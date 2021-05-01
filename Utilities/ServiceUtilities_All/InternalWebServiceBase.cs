/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading;
using BCommonUtilities;
using BWebServiceUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ServiceUtilities.All
{
    public abstract class InternalWebServiceBase : BppWebServiceBase
    {
        protected readonly string InternalCallPrivateKey;
        public InternalWebServiceBase(string _InternalCallPrivateKey)
        {
            InternalCallPrivateKey = _InternalCallPrivateKey;
        }
        private InternalWebServiceBase() { }

        protected override BWebServiceResponse OnRequestPP(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null)
        {
            if (UrlParameters.ContainsKey("secret") && UrlParameters["secret"] == InternalCallPrivateKey)
            {
                return Process(_Context, _ErrorMessageAction);
            }
            return BWebResponse.Forbidden("You are trying to access to a private service.");
        }

        protected abstract BWebServiceResponse Process(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null);
    }

    public abstract class InternalWebServiceBaseWebhook : BppWebServiceBase
    {
        protected readonly string InternalCallPrivateKey;
        public InternalWebServiceBaseWebhook(string _InternalCallPrivateKey)
        {
            InternalCallPrivateKey = _InternalCallPrivateKey;
        }

        protected override BWebServiceResponse OnRequestPP(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null)
        {
            _ErrorMessageAction?.Invoke($"InternalWebServiceBaseWebhook->OnRequest: Message received. HttpMethod: {_Context.Request.HttpMethod}, Headers: {_Context.Request.Headers.ToString()}");

            // Cloud Event Schema v1.0
            // https://github.com/cloudevents/spec/blob/v1.0/http-webhook.md#4-abuse-protection
            if (_Context.Request.HttpMethod == "OPTIONS")
            {
                var WebhookRequestCallbak = _Context.Request.Headers.Get("webhook-request-callback");
                var WebhookRequestOrigin = _Context.Request.Headers.Get("webhook-request-origin");

                BTaskWrapper.Run(() =>
                {
                    Thread.CurrentThread.IsBackground = true;

                    Thread.Sleep(3000);
                    using var Handler = new HttpClientHandler
                    {
                        SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls,
                        ServerCertificateCustomValidationCallback = (a, b, c, d) => true
                    };
                    using var Client = new HttpClient(Handler);
                    Client.DefaultRequestHeaders.TryAddWithoutValidation("WebHook-Allowed-Origin", WebhookRequestOrigin);
                    Client.DefaultRequestHeaders.TryAddWithoutValidation("WebHook-Allowed-Rate", "120");
                    using (var RequestTask = Client.GetAsync(WebhookRequestCallbak))
                    {
                        RequestTask.Wait();
                        using var Response = RequestTask.Result;
                        using var ResponseContent = Response.Content;

                        using var ReadResponseTask = ResponseContent.ReadAsStringAsync();
                        ReadResponseTask.Wait();

                        var ResponseString = ReadResponseTask.Result;
                        var ResponseStatusCode = (int)Response.StatusCode;
                        _ErrorMessageAction?.Invoke($"InternalWebServiceBaseWebhook->WebhookRequestResult: Request origin is {WebhookRequestOrigin}. Request url is {WebhookRequestCallbak}. Response: {ResponseString}, Code: {ResponseStatusCode}");

                        if (!Response.IsSuccessStatusCode)
                        {
                            _ErrorMessageAction?.Invoke("Error: InternalWebServiceBaseWebhook->WebhookRequestResult: Request returned error. Code: " + Response.StatusCode + ", message: " + ResponseString);
                        }
                    }
                });

                return BWebResponse.StatusOK("OK.");
            }

            if (UrlParameters.ContainsKey("secret") && UrlParameters["secret"] == InternalCallPrivateKey)
            {
                return Process(_Context, _ErrorMessageAction);
            }

            return BWebResponse.Forbidden("You are trying to access to a private service.");
        }

        protected abstract BWebServiceResponse Process(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null);
    }
}
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
        private string WebhookRequestCallbak = null;
        private string WebhookRequestOrigin = null;

        public InternalWebServiceBase(string _InternalCallPrivateKey)
        {
            InternalCallPrivateKey = _InternalCallPrivateKey;
        }
        private InternalWebServiceBase() { }

        protected override BWebServiceResponse OnRequestPP(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null)
        {
            // Cloud Event Schema v1.0
            // https://github.com/cloudevents/spec/blob/v1.0/http-webhook.md#4-abuse-protection
            if (_Context.Request.HttpMethod == "OPTIONS")
            {
                WebhookRequestCallbak = _Context.Request.Headers.Get("WebHook-Request-Callback");
                WebhookRequestOrigin = _Context.Request.Headers.Get("WebHook-Request-Origin");

                if (WebhookRequestCallbak != null && WebhookRequestOrigin != null)
                {
                    BTaskWrapper.Run(() =>
                    {
                        Thread.CurrentThread.IsBackground = true;

                        Thread.Sleep(1000);

                        SendWebhookValidationRequest(WebhookRequestOrigin, WebhookRequestCallbak, _ErrorMessageAction);
                    });

                    return BWebResponse.StatusOK("OK.");
                }

                return BWebResponse.BadRequest("WebHook-Request-Callback and WebHook-Request-Origin must be included in the request.");
            }

            if (UrlParameters.ContainsKey("secret") && UrlParameters["secret"] == InternalCallPrivateKey)
            {
                return Process(_Context, _ErrorMessageAction);
            }
            return BWebResponse.Forbidden("You are trying to access to a private service.");
        }

        private void SendWebhookValidationRequest(string _WebhookRequestOrigin, string _WebhookRequestCallbak, Action<string> _ErrorMessageAction)
        {
            using var Handler = new HttpClientHandler
            {
                SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls,
                ServerCertificateCustomValidationCallback = (a, b, c, d) => true
            };
            using var Client = new HttpClient(Handler);
            Client.DefaultRequestHeaders.TryAddWithoutValidation("WebHook-Allowed-Origin", _WebhookRequestOrigin);
            Client.DefaultRequestHeaders.TryAddWithoutValidation("WebHook-Allowed-Rate", "*");
            using (var RequestTask = Client.GetAsync(_WebhookRequestCallbak))
            {
                RequestTask.Wait();
                using var Response = RequestTask.Result;
                using var ResponseContent = Response.Content;

                using var ReadResponseTask = ResponseContent.ReadAsStringAsync();
                ReadResponseTask.Wait();

                var ResponseString = ReadResponseTask.Result;
                var ResponseStatusCode = (int)Response.StatusCode;
                var ResponseSuccessString = Response.IsSuccessStatusCode ? "OK" : "ERROR";
                _ErrorMessageAction?.Invoke($"InternalWebServiceBaseWebhook->ValidationResponse: From '{_WebhookRequestOrigin}', Result {ResponseSuccessString} ({ResponseStatusCode}): '{ResponseString}'");
            }
        }

        protected abstract BWebServiceResponse Process(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null);
    }
}
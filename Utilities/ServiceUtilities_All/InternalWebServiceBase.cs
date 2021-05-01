/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
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
            _ErrorMessageAction?.Invoke($"InternalWebServiceBaseWebhook->OnRequest: Message received.");

            string FoundedHeaderValue = "None";
            bool bIsSubscriptionValidation =
                BWebUtilities.DoesContextContainHeader(out List<string> HeaderValues, out string _, _Context, "aeg-event-type")
                && BUtility.CheckAndGetFirstStringFromList(HeaderValues, out FoundedHeaderValue);

            if (bIsSubscriptionValidation)
            {
                _ErrorMessageAction?.Invoke($"InternalWebServiceBaseWebhook->OnRequest: SubscriptionValidation is {bIsSubscriptionValidation}, FoundedHeaderValue is: {FoundedHeaderValue}");
            }

            if (UrlParameters.ContainsKey("secret") && UrlParameters["secret"] == InternalCallPrivateKey)
            {
                return Process(_Context, _ErrorMessageAction);
            }
            else
            {
                string ValidationCode = null;
                using (var InputStream = _Context.Request.InputStream)
                {
                    using (var Reader = new StreamReader(InputStream))
                    {
                        var JsonMessage = Reader.ReadToEnd();
                        try
                        {
                            dynamic ParsedArray = JsonConvert.DeserializeObject(JsonMessage);
                            foreach (var Parsed in ParsedArray)
                            {
                                if (Parsed.ContainsKey("data"))
                                {
                                    var Data = (JObject)Parsed["data"];
                                    if (Data.ContainsKey("validationCode"))
                                    {
                                        ValidationCode = (string)Data["validationCode"];
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            _ErrorMessageAction?.Invoke("InternalWebServiceBaseWebhook->OnRequest: Webhook data object parse error: " + e.Message + ", trace: " + e.StackTrace + ", payload is: " + JsonMessage);
                            return BWebResponse.BadRequest("Webhook data object parsing has failed for accessing validationCode.");
                        }

                        if (ValidationCode != null)
                        {
                            var Success_Object = new JObject()
                            {
                                ["validationResponse"] = ValidationCode
                            };
                            return new BWebServiceResponse(BWebResponse.Status_OK_Code, new BStringOrStream(Success_Object.ToString()), BWebResponse.Status_Success_ContentType);
                        }
                        _ErrorMessageAction?.Invoke("InternalWebServiceBaseWebhook->OnRequest: ValidationCode is null, payload is: " + JsonMessage);
                    }
                }
            }

            return BWebResponse.Forbidden("You are trying to access to a private service.");
        }

        protected abstract BWebServiceResponse Process(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null);
    }
}
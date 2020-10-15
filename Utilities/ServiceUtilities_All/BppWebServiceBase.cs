/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;
using System.Net;
using BCommonUtilities;
using BWebServiceUtilities;
using Newtonsoft.Json.Linq;

namespace ServiceUtilities.All
{
    //Use this instead of using BWebServiceBase as base class; this checks if it is an internal call; if so; it dumps error messages to the response
    public abstract class BppWebServiceBase : BWebServiceBase
    {
        public BppWebServiceBase() { }

        public override BWebServiceResponse OnRequest(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null)
        {
            bool bIsInternalCall =
                BWebUtilities.DoesContextContainHeader(out List<string> ICHVs, out string _, _Context, "internal-call-secret")
                && BUtility.CheckAndGetFirstStringFromList(ICHVs, out string _);

            if (bIsInternalCall)
            {
                var ErrorMessages = new JArray();

                var Result = OnRequestPP(_Context, (string _Message) =>
                {
                    _ErrorMessageAction?.Invoke(_Message);
                    ErrorMessages.Add(_Message);
                });

                try
                {
                    if (Result.ResponseContent.Type == EBStringOrStreamEnum.String)
                    {
                        var Parsed = JObject.Parse(Result.ResponseContent.String);
                        Parsed["internalCallErrorMessages"] = ErrorMessages;
                        Result = new BWebServiceResponse(Result.StatusCode, Result.Headers, new BStringOrStream(Parsed.ToString()), Result.ResponseContentType);
                    }
                }
                catch (Exception) {}

                return Result;
            }
            return OnRequestPP(_Context, _ErrorMessageAction);
        }

        protected abstract BWebServiceResponse OnRequestPP(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null);
    }
}
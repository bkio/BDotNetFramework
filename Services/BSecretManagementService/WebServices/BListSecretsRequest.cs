/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;
using System.Net;
using BCloudServiceUtilities;
using BCommonUtilities;
using BWebServiceUtilities;
using Newtonsoft.Json.Linq;

namespace BSecretManagementService.WebServices
{
    public class BListSecretsRequest : BWebServiceBase
    {
        private readonly IBFileServiceInterface FileService;
        private readonly string SecretsStorageBucket;

        public BListSecretsRequest(IBFileServiceInterface _FileService, string _SecretsStorageBucket)
        {
            FileService = _FileService;
            SecretsStorageBucket = _SecretsStorageBucket;
        }

        public override BWebServiceResponse OnRequest(HttpListenerContext Context, Action<string> _ErrorMessageAction = null)
        {
            GetTracingService()?.On_FromServiceToService_Received(Context, _ErrorMessageAction);

            var Result = OnRequest_Internal(Context, _ErrorMessageAction);

            GetTracingService()?.On_FromServiceToService_Sent(Context, _ErrorMessageAction);

            return Result;
        }
        
        private BWebServiceResponse OnRequest_Internal(HttpListenerContext Context, Action<string> _ErrorMessageAction = null)
        {
            if (Context.Request.HttpMethod != "GET")
            {
                _ErrorMessageAction?.Invoke("BListSecretsRequest: GET method is accepted. But received request method:  " + Context.Request.HttpMethod);
                return BWebResponse.MethodNotAllowed("GET method is accepted. But received request method: " + Context.Request.HttpMethod);
            }

            if (!FileService.ListAllFilesInBucket(
                SecretsStorageBucket,
                out List<string> KeysFound,
                (string Message) =>
                {
                    _ErrorMessageAction?.Invoke("BListSecretsRequest->Error-> " + Message);
                }))
            {
                return BWebResponse.InternalError("List all files operation has failed.`");
            }

            var ResultObject = new JObject
            {
                ["keys"] = new JArray()
            };
            foreach (var Key in KeysFound)
            {
                ((JArray)ResultObject["keys"]).Add(Key);
            }

            return new BWebServiceResponse(
                BWebResponse.Status_OK_Code,
                new BStringOrStream(ResultObject.ToString()),
                EBResponseContentType.JSON);
        }
    }
}
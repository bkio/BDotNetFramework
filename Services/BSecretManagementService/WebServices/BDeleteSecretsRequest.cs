/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Threading;
using BCloudServiceUtilities;
using BCommonUtilities;
using BWebServiceUtilities;
using Newtonsoft.Json.Linq;

namespace BSecretManagementService.WebServices
{
    public class BDeleteSecretsRequest : BWebServiceBase
    {
        private readonly IBFileServiceInterface FileService;
        private readonly string SecretsStorageBucket;

        public BDeleteSecretsRequest(IBFileServiceInterface _FileService, string _SecretsStorageBucket)
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
            if (Context.Request.HttpMethod != "POST" && Context.Request.HttpMethod != "DELETE")
            {
                _ErrorMessageAction?.Invoke("BDeleteSecretsRequest: POST/DELETE method is accepted. But received request method:  " + Context.Request.HttpMethod);
                return BWebResponse.MethodNotAllowed("POST/DELETE method is accepted. But received request method: " + Context.Request.HttpMethod);
            }

            JObject ParsedBody;
            using var ResponseReader = new StreamReader(Context.Request.InputStream);
            try
            {
                ParsedBody = JObject.Parse(ResponseReader.ReadToEnd());
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BDeleteSecretsRequest-> Read request body stage has failed. Exception: " + e.Message + ", Trace: " + e.StackTrace);
                return BWebResponse.BadRequest("Malformed request body. Request must be a valid json form.");
            }

            if (!ParsedBody.ContainsKey("keys"))
            {
                _ErrorMessageAction?.Invoke("BDeleteSecretsRequest-> Request does not have keys field.");
                return BWebResponse.BadRequest("Request does not have keys field.");
            }

            var ToBeDeletedKeys = (JArray)ParsedBody["keys"];
            if (ToBeDeletedKeys == null || ToBeDeletedKeys.Count == 0)
            {
                _ErrorMessageAction?.Invoke("BDeleteSecretsRequest-> Request does not have keys array or elements.");
                return BWebResponse.BadRequest("Request does not have keys array.");
            }

            var CompletionStateStack = new ConcurrentStack<object>();
            for (int i = 0; i < ToBeDeletedKeys.Count; i++)
                CompletionStateStack.Push(new object());

            var WaitUntilSignal = new ManualResetEvent(false);

            var SucceedQueue = new ConcurrentQueue<string>();
            var FailedQueue = new ConcurrentQueue<string>();

            foreach (string ToBeDeletedKey in ToBeDeletedKeys)
            {
                BTaskWrapper.Run(() =>
                {
                    if (FileService.DeleteFile(
                        SecretsStorageBucket,
                        ToBeDeletedKey,
                        (string Message) =>
                        {
                            _ErrorMessageAction?.Invoke("BDeleteSecretsRequest->Error-> " + Message);
                        }))
                    {
                        SucceedQueue.Enqueue(ToBeDeletedKey);
                    }
                    else
                    {
                        FailedQueue.Enqueue(ToBeDeletedKey);
                    }

                    CompletionStateStack.TryPop(out object Ignore);
                    if (CompletionStateStack.Count == 0)
                    {
                        WaitUntilSignal.Set();
                    }
                });
            }

            WaitUntilSignal.WaitOne();

            var ResultObject = new JObject
            {
                ["succeed"] = new JArray(),
                ["failed"] = new JArray()
            };

            while (SucceedQueue.TryDequeue(out string SucceedKey))
            {
                ((JArray)ResultObject["succeed"]).Add(SucceedKey);
            }
            while (FailedQueue.TryDequeue(out string FailedKey))
            {
                ((JArray)ResultObject["failed"]).Add(FailedKey);
            }

            return new BWebServiceResponse(
                BWebResponse.Status_OK_Code,
                new BStringOrStream(ResultObject.ToString()),
                EBResponseContentType.JSON);
        }
    }
}

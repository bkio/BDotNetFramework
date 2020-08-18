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
    public class BGetSecretsRequest : BWebServiceBase
    {
        private readonly IBFileServiceInterface FileService;
        private readonly string SecretsStorageBucket;

        public BGetSecretsRequest(IBFileServiceInterface _FileService, string _SecretsStorageBucket)
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
                _ErrorMessageAction?.Invoke("BGetSecretsRequest: GET method is accepted. But received request method:  " + Context.Request.HttpMethod);
                return BWebResponse.MethodNotAllowed("GET method is accepted. But received request method: " + Context.Request.HttpMethod);
            }

            JObject ParsedBody;
            using var InputStream = Context.Request.InputStream;
            using var ResponseReader = new StreamReader(InputStream);
            try
            {
                ParsedBody = JObject.Parse(ResponseReader.ReadToEnd());
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BGetSecretsRequest-> Read request body stage has failed. Exception: " + e.Message + ", Trace: " + e.StackTrace);
                return BWebResponse.BadRequest("Malformed request body. Request must be a valid json form.");
            }

            if (!ParsedBody.ContainsKey("keys"))
            {
                _ErrorMessageAction?.Invoke("BGetSecretsRequest-> Request does not have keys field.");
                return BWebResponse.BadRequest("Request does not have keys field.");
            }

            var RequestedKeys = (JArray)ParsedBody["keys"];
            if (RequestedKeys == null || RequestedKeys.Count == 0)
            {
                _ErrorMessageAction?.Invoke("BGetSecretsRequest-> Request does not have keys array or elements.");
                return BWebResponse.BadRequest("Request does not have keys array.");
            }

            var CompletionStateStack = new ConcurrentStack<object>();
            for (int i = 0; i < RequestedKeys.Count; i++)
                CompletionStateStack.Push(new object());

            var WaitUntilSignal = new ManualResetEvent(false);

            var SucceedQueue = new ConcurrentQueue<Tuple<string, string>>();
            var FailedQueue = new ConcurrentQueue<string>();

            foreach (string RequestedKey in RequestedKeys)
            {
                BTaskWrapper.Run(() =>
                {
                    using var MemStream = new MemoryStream();

                    if (FileService.DownloadFile(SecretsStorageBucket, RequestedKey, new BStringOrStream(MemStream, MemStream.Length),
                        (string Message) =>
                        {
                            _ErrorMessageAction?.Invoke("BGetSecretsRequest->Error-> " + Message);
                        }))
                    {
                        try
                        {
                            using var MemReader = new StreamReader(MemStream);
                            SucceedQueue.Enqueue(new Tuple<string, string>(RequestedKey, MemReader.ReadToEnd()));
                        }
                        catch (Exception e)
                        {
                            _ErrorMessageAction?.Invoke("BGetSecretsRequest-> Exception during secret retrieval from file service: " + e.Message + ", Trace: " + e.StackTrace);
                        }
                    }
                    else
                    {
                        FailedQueue.Enqueue(RequestedKey);
                    }

                    CompletionStateStack.TryPop(out object Ignore);
                    if (CompletionStateStack.Count == 0)
                    {
                        try
                        {
                            WaitUntilSignal.Set();
                        }
                        catch (Exception) { }
                    }
                });
            }

            try
            {
                WaitUntilSignal.WaitOne();
                WaitUntilSignal.Close();
            }
            catch (Exception) { }

            var ResultObject = new JObject
            {
                ["succeed"] = new JObject(),
                ["failed"] = new JArray()
            };

            while (SucceedQueue.TryDequeue(out Tuple<string, string> Pair))
            {
                ResultObject["succeed"][Pair.Item1] = Pair.Item2;
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
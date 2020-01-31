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
    public class BPutSecretsRequest : BWebServiceBase
    {
        private readonly IBFileServiceInterface FileService;
        private readonly string SecretsStorageBucket;

        public BPutSecretsRequest(IBFileServiceInterface _FileService, string _SecretsStorageBucket)
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
            if (Context.Request.HttpMethod != "POST" && Context.Request.HttpMethod != "PUT")
            {
                _ErrorMessageAction?.Invoke("BPutSecretsRequest: POST/PUT methods are accepted. But received request method:  " + Context.Request.HttpMethod);
                return BWebResponse.MethodNotAllowed("POST/PUT methods are accepted. But received request method: " + Context.Request.HttpMethod);
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
                _ErrorMessageAction?.Invoke("BPutSecretsRequest-> Read request body stage has failed. Exception: " + e.Message + ", Trace: " + e.StackTrace);
                return BWebResponse.BadRequest("Malformed request body. Request must be a valid json form.");
            }

            if (ParsedBody.Count == 0)
            {
                _ErrorMessageAction?.Invoke("BPutSecretsRequest-> Request does not have any secret field.");
                return BWebResponse.BadRequest("Request does not have secret field.");
            }

            var WaitUntilSignal = new ManualResetEvent(false);

            var CompletionStateStack = new ConcurrentStack<object>();
            for (int i = 0; i < ParsedBody.Count; i++)
                CompletionStateStack.Push(new object());

            var SucceedQueue = new ConcurrentQueue<string>();
            var FailedQueue = new ConcurrentQueue<string>();

            foreach (var Pair in ParsedBody)
            {
                BTaskWrapper.Run(() =>
                {
                    using var MemStream = new MemoryStream();
                    using var MemWriter = new StreamWriter(MemStream);
                    MemWriter.Write(Pair.Value);
                    MemWriter.Flush();
                    MemStream.Position = 0;

                    if (FileService.UploadFile(
                        new BStringOrStream(MemStream, MemStream.Length),
                        SecretsStorageBucket,
                        Pair.Key,
                        EBRemoteFileReadPublicity.AuthenticatedRead,
                        null,
                        (string Message) =>
                        {
                            _ErrorMessageAction?.Invoke("BPutSecretsRequest->Error-> " + Message);
                        }))
                    {
                        SucceedQueue.Enqueue(Pair.Key);
                    }
                    else
                    {
                        FailedQueue.Enqueue(Pair.Key);
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

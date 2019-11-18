# BDotNetFramework

BDotNetFramework is a microservice framework for .NET Core 3 and .NET Framework 4.7+ that abstracts many cloud features such as;

 - Cloud specific managed no-sql database access for
	 - Amazon: Dynamodb
	 - Google Cloud: Datastore
 - Cloud storage access for
	 - Amazon: S3
	 - Google: GCS
 - Cloud logging service access for
	 - Amazon: Cloud Watch
	 - Google: Stackdriver
 - Pub/Sub service access for
	 - Amazon: SQS
	 - Google: Google Pub/Sub
	 - Generic: Redis Pub/Sub
 - In-memory service access for:
	 - Generic: Redis
 - Cloud e-mail service access for
	 - Sendgrid
 - Tracing service access for
	 - Zipkin

The framework does not use any http library such as ASP.NET or OWIN, it uses the basic HttpListener by abstracting wildcard path parsing to make the usage even easier and increase the compatibility with different platforms.

The framework also has WebSocket server functionality and many utility functions.

# Getting started
It is as easy as creating a new .NET (Core or Framework) project and adding desired dependencies to the project. For instance, in case only the file service access is desired;

Add .NET standard projects called:

 1. BCommonUtilities
 2. BWebServiceUtilities
 3. BCloudServiceUtilities
 4. BCloudServiceUtilities-BFileService-AWS
 5. BCloudServiceUtilities-BFileService-GC

Add shared projects called:

 1. BServiceUtilities
 2. BServiceUtilities-FileService

Set environment variables for your application:

 1. **PROGRAM_ID**: Program Unique ID, Uniqueness for the sake of tracing service's easy service segregation, it does not have to be unique for each instance of the same application.
 2. **CLOUD_PROVIDER**: AWS, GC (Supported ones right now)
 3. **TRACING_SERVER_IP**: Only Zipkin is supported at the moment
 4. **TRACING_SERVER_PORT**
 5. **PORT**: HTTP Port for listening requests

Then it is as easy as:

    BServiceInitializer.Initialize(out BServiceInitializer ServInit);
    ServInit.WithFileService();
    var WebServiceEndpoints = new List<BWebPrefixStructure>()
    {
        new BWebPrefixStructure(new string[] { "/api/endpoint" }, () => new YourCustomRequestHandler(ServInit.FileService))
    };
    var WebService = new BWebService(WebServiceEndpoints.ToArray(), ServInit.ServerPort, ServInit.TracingService);
    WebService.Run((string Message) =>
    {
        ServInit.LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Info, Message), ServInit.ProgramID, "WebService");
    });

And of course implementing the handler class that extends **BWebServiceBase** like:

    
    public class YourCustomRequestHandler : BWebServiceBase
    {
	    private IBFileServiceInterface FileService;
	    
	    public YourCustomRequestHandler(IBFileServiceInterface _FileService)
        {
            FileService = _FileService;
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
			//Add your logic here

            return new BWebServiceResponse(
                BWebResponseStatus.Status_OK_Code,
                new BStringOrStream(ResultObject.ToString()),
                EBResponseContentType.JSON);
        }
    }


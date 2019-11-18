/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;
using System.Threading;
using BCloudServiceUtilities;
using BSecretManagementService.WebServices;
using BServiceUtilities;
using BWebServiceUtilities;

namespace BSecretManagementService
{
    /// <summary>
    /// 
    /// <para>Check BServiceUtilities.BServiceInitializer for required common environment variables.</para>
    /// 
    /// <para>Service specific environment variables:</para>
    /// 
    /// <para>1) SECRETS_STORAGE_BUCKET:                    Bucket address that holds secrets</para>
    /// 
    /// </summary>
    public class BProgram
    {
        static void Main()
        {
            /*
            * Common initialization step
            */
            if (!BServiceInitializer.Initialize(out BServiceInitializer ServInit, Console.WriteLine, 
                new string[]
                {
                    "SECRETS_STORAGE_BUCKET"
                }))
                return;

            //File service is required
            if (!ServInit.WithFileService()) return;

            var SecretsBucketName = ServInit.RequiredEnvironmentVariables["SECRETS_STORAGE_BUCKET"];

            /*
            * Web-http service initialization
            */
            var WebServiceEndpoints = new List<BWebPrefixStructure>()
            {
                new BWebPrefixStructure(new string[] { "/api/private/secrets/get" }, () => new BGetSecretsRequest(ServInit.FileService, SecretsBucketName)),
                new BWebPrefixStructure(new string[] { "/api/private/secrets/put" }, () => new BPutSecretsRequest(ServInit.FileService, SecretsBucketName)),
                new BWebPrefixStructure(new string[] { "/api/private/secrets/delete" }, () => new BDeleteSecretsRequest(ServInit.FileService, SecretsBucketName)),
                new BWebPrefixStructure(new string[] { "/api/private/secrets/list" }, () => new BListSecretsRequest(ServInit.FileService, SecretsBucketName))
            };
            var BWebService = new BWebService(WebServiceEndpoints.ToArray(), ServInit.ServerPort, ServInit.TracingService);
            BWebService.Run((string Message) =>
            {
                ServInit.LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Info, Message), ServInit.ProgramID, "WebService");
            });

            /*
            * Make main thread sleep forever
            */
            Thread.Sleep(Timeout.Infinite);
        }
    }
}
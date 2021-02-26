/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;
using BCloudServiceUtilities.DatabaseServices;
using BCloudServiceUtilities.FileServices;
using BCloudServiceUtilities.LoggingServices;
using BCloudServiceUtilities.MailServices;
using BCloudServiceUtilities.MemoryServices;
using BCloudServiceUtilities.PubSubServices;
using BCloudServiceUtilitiesTest.Tests;
using BCommonUtilities;

namespace BCloudServiceUtilitiesTest
{
    /// <summary>
    /// 
    /// <para>Required environment variables:</para>
    /// 
    /// <para>APPINSIGHTS_INSTRUMENTATIONKEY</para>
    /// <para>GC_PROJECT_ID</para>
    /// <para>AWS_ACCESS_KEY</para>
    /// <para>AWS_SECRET_KEY</para>
    /// <para>AWS_REGION</para>
    /// <para>SENDGRID_API_KEY</para>
    /// <para>REDIS_ENDPOINT</para>
    /// <para>REDIS_PORT</para>
    /// <para>REDIS_PASSWORD</para>
    /// <para>FILESERVICE_BUCKET</para>
    /// <para>FILESERVICE_REMOTE_PATH</para>
    /// <para>FILESERVICE_TEST_FILE_LOCAL_PATH</para>
    /// 
    /// </summary>
    public static class BRunTests
    {
        private class BRelativeTestsResultComparator
        {
            private readonly List<string> Results = new List<string>();
            private readonly List<string> Results_Old = new List<string>();

            private readonly Action<string> PrintAction;

            public BRelativeTestsResultComparator(Action<string> _PrintAction)
            {
                PrintAction = _PrintAction;
            }

            public void Next()
            {
                Results_Old.Clear();
                foreach (var _Result in Results)
                {
                    Results_Old.Add(_Result);
                }
                Results.Clear();
            }

            public void AddLine(string _Result)
            {
                PrintAction?.Invoke(_Result);
                Results.Add(_Result);
            }

            public bool Compare()
            {
                if (Results.Count != Results_Old.Count)
                {
                    PrintAction?.Invoke("BTestResultComparator->Compare has failed. Count mismatch. Results:" + Results.Count + " != Results_Old:" + Results_Old.Count);
                    return false;
                }

                for (int i = 0; i < Results.Count; i++)
                {
                    if (Results[i] != Results_Old[i])
                    {
                        PrintAction?.Invoke("BTestResultComparator->Compare has failed. Result mismatch. Results[" + i + "]: " + Results[i] + " != Results_Old[" + i + "]: " + Results_Old[i]);
                        return false;
                    }
                }
                return true;
            }
        }

        public static bool RunTests()
        {
            if (!BUtility.GetEnvironmentVariables(out Dictionary<string, string> RequiredEnvVars,
                new string[][]
                {
                    new string[] { "GC_PROJECT_ID" }
                },
                Console.WriteLine)) return false;

            /*
             * Logging Services tests
             */
            var LoggingTests_GC = new BLoggingServiceTest(
                new BLoggingServiceGC(
                    RequiredEnvVars["GC_PROJECT_ID"],
                    Console.WriteLine),
                Console.WriteLine);
            if (!LoggingTests_GC.Start()) return false;

            var LoggingTests_AWS = new BLoggingServiceTest(
                new BLoggingServiceAWS(
                    RequiredEnvVars["AWS_ACCESS_KEY"],
                    RequiredEnvVars["AWS_SECRET_KEY"],
                    RequiredEnvVars["AWS_REGION"],
                    Console.WriteLine),
                Console.WriteLine);
            if (!LoggingTests_AWS.Start()) return false;

            var LoggingTests_Azure = new BLoggingServiceTest(
                new BLoggingServiceAzure(
                    RequiredEnvVars["APPINSIGHTS_INSTRUMENTATIONKEY"],
                    Console.WriteLine),
                Console.WriteLine);
            if (!LoggingTests_Azure.Start()) return false;

            /*
             * E-mail Services tests
             */
            var Comparator = new BRelativeTestsResultComparator(Console.WriteLine);

            var MailTests_SendGrid = new BEmailServicesTest(
                new BMailServiceSendGrid(
                    RequiredEnvVars["SENDGRID_API_KEY"],
                    "btest@btest.com",
                    "BTest",
                    Console.WriteLine),
                Comparator.AddLine);
            if (!MailTests_SendGrid.Start()) return false;

            /*
             * Database Services tests
             */
            Comparator = new BRelativeTestsResultComparator(Console.WriteLine);

            var DBTests_GC = new BDatabaseServicesTest(
                new BDatabaseServiceGC(
                    RequiredEnvVars["GC_PROJECT_ID"],
                    Console.WriteLine),
                "BTest", 
                "TestKey",
                Comparator.AddLine);
            if (!DBTests_GC.Start()) return false;

            Comparator.Next();

            var DBTests_AWS = new BDatabaseServicesTest(
                new BDatabaseServiceAWS(
                    RequiredEnvVars["AWS_ACCESS_KEY"],
                    RequiredEnvVars["AWS_SECRET_KEY"],
                    RequiredEnvVars["AWS_REGION"],
                    Console.WriteLine),
                "BTest",
                "TestKey",
                Comparator.AddLine);
            if (!DBTests_AWS.Start()) return false;

            Comparator.Next();

            var DBTests_MongoDB = new BDatabaseServicesTest(
                new BDatabaseServiceMongoDB(
                    RequiredEnvVars["MONGO_DB_CONNECTION_STRING"],
                    RequiredEnvVars["MONGO_DB_DATABASE"],
                    Console.WriteLine),
                "BTest",
                "TestKey",
                Comparator.AddLine);
            if (!DBTests_MongoDB.Start()) return false;

            if (!Comparator.Compare()) return false;

            /*
             * Memory and Pub/Sub Services tests
             */
            Comparator = new BRelativeTestsResultComparator(Console.WriteLine);

            var MemTests_WithRedisPubSub = new BMemoryPubSubServiceTest(
                new BMemoryServiceRedis(
                    RequiredEnvVars["REDIS_ENDPOINT"],
                    int.Parse(RequiredEnvVars["REDIS_PORT"]),
                    RequiredEnvVars["REDIS_PASSWORD"],
                    new BPubSubServiceRedis(
                        RequiredEnvVars["REDIS_ENDPOINT"],
                        int.Parse(RequiredEnvVars["REDIS_PORT"]),
                        RequiredEnvVars["REDIS_PASSWORD"],
                        true,
                        Console.WriteLine),
                    true,
                    Console.WriteLine),
                Comparator.AddLine);
            if (!MemTests_WithRedisPubSub.Start()) return false;

            Comparator.Next();

            var MemTests_WithGCPubSub = new BMemoryPubSubServiceTest(
                new BMemoryServiceRedis(
                    RequiredEnvVars["REDIS_ENDPOINT"],
                    int.Parse(RequiredEnvVars["REDIS_PORT"]),
                    RequiredEnvVars["REDIS_PASSWORD"],
                    new BPubSubServiceGC(
                        RequiredEnvVars["GC_PROJECT_ID"],
                        Console.WriteLine),
                    true,
                    Console.WriteLine),
                Comparator.AddLine);
            if (!MemTests_WithGCPubSub.Start()) return false;

            if (!Comparator.Compare()) return false;

            Comparator.Next();

            var MemTests_WithAWSPubSub = new BMemoryPubSubServiceTest(
                new BMemoryServiceRedis(
                    RequiredEnvVars["REDIS_ENDPOINT"],
                    int.Parse(RequiredEnvVars["REDIS_PORT"]),
                    RequiredEnvVars["REDIS_PASSWORD"],
                    new BPubSubServiceAWS(
                        RequiredEnvVars["AWS_ACCESS_KEY"],
                        RequiredEnvVars["AWS_SECRET_KEY"],
                        RequiredEnvVars["AWS_REGION"],
                        Console.WriteLine),
                    true,
                    Console.WriteLine),
                Comparator.AddLine);
            if (!MemTests_WithAWSPubSub.Start()) return false;

            if (!Comparator.Compare()) return false;

            Comparator.Next();

            var MemTests_WithAzurePubSub = new BMemoryPubSubServiceTest(
                new BMemoryServiceRedis(
                    RequiredEnvVars["REDIS_ENDPOINT"],
                    int.Parse(RequiredEnvVars["REDIS_PORT"]),
                    RequiredEnvVars["REDIS_PASSWORD"],
                    new BPubSubServiceAzure(
                        RequiredEnvVars["AZURE_CLIENT_ID"],
                        RequiredEnvVars["AZURE_CLIENT_SECRET"],
                        RequiredEnvVars["AZURE_TENANT_ID"],
                        RequiredEnvVars["AZURE_NAMESPACE_ID"],
                        RequiredEnvVars["AZURE_NAMESPACE_CONNSTR"],
                        Console.WriteLine),
                    true,
                    Console.WriteLine),
                Comparator.AddLine);
            if (!MemTests_WithAWSPubSub.Start()) return false;

            if (!Comparator.Compare()) return false;

            /*
             * File Services tests
             */
            Comparator = new BRelativeTestsResultComparator(Console.WriteLine);

            var FSTests_GC = new BFileServiceTest(
                new BFileServiceGC(
                    RequiredEnvVars["GC_PROJECT_ID"],
                    Console.WriteLine),
                RequiredEnvVars["FILESERVICE_BUCKET"],
                RequiredEnvVars["FILESERVICE_REMOTE_PATH"],
                RequiredEnvVars["FILESERVICE_TEST_FILE_LOCAL_PATH"],
                Comparator.AddLine);
            if (!FSTests_GC.Start()) return false;

            Comparator.Next();

            var FSTests_AWS = new BFileServiceTest(
                new BFileServiceAWS(
                    RequiredEnvVars["AWS_ACCESS_KEY"],
                    RequiredEnvVars["AWS_SECRET_KEY"],
                    RequiredEnvVars["AWS_REGION"],
                    Console.WriteLine),
                RequiredEnvVars["FILESERVICE_BUCKET"],
                RequiredEnvVars["FILESERVICE_REMOTE_PATH"],
                RequiredEnvVars["FILESERVICE_TEST_FILE_LOCAL_PATH"],
                Comparator.AddLine);
            if (!FSTests_AWS.Start()) return false;

            Comparator.Next();

            var FSTests_AZ = new BFileServiceTest(
                new BFileServiceAZ(
                    RequiredEnvVars["AZ_STORAGE_SERVICE"],
                    RequiredEnvVars["AZ_STORAGE_ACCOUNT"],
                    RequiredEnvVars["AZ_STORAGE_ACCOUNT_KEY"],
                    RequiredEnvVars["AZ_STORAGE_RESOURCE_GROUP"],
                    RequiredEnvVars["AZ_STORAGE_MANAGEMENT_APP_ID"],
                    RequiredEnvVars["AZ_STORAGE_MANAGEMENT_SECRET"],
                    RequiredEnvVars["AZ_SUBSCRIPTION_ID"],
                    RequiredEnvVars["AZ_TENANT_ID"],
                    RequiredEnvVars["AZ_STORAGE_LOCATION"],
                    Console.WriteLine),
                RequiredEnvVars["FILESERVICE_BUCKET"],
                RequiredEnvVars["FILESERVICE_REMOTE_PATH"],
                RequiredEnvVars["FILESERVICE_TEST_FILE_LOCAL_PATH"],
                Comparator.AddLine);
            if (!FSTests_AWS.Start()) return false;

            if (!Comparator.Compare()) return false;

            return true;
        }
    }
}
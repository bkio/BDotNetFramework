/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using BCloudServiceUtilities;
using BCommonUtilities;
using BWebServiceUtilities;
using Newtonsoft.Json.Linq;

namespace ServiceUtilities.Common
{
    public struct AuthorizedRequester
    {
        public bool bParseSuccessful;
        public string ParseErrorMessage;

        public string UserID;
        public string UserName;
        public string UserEmail;
        public string AuthMethodKey;
    }

    public class Methods
    {
        public static AuthorizedRequester GetAuthorizedRequester(HttpListenerContext _Context, Action<string> _ErrorMessageAction)
        {
            if (!BWebUtilities.DoesContextContainHeader(out List<string> AuthorizedUserID, out string _, _Context, "authorized-u-id") ||
                !BWebUtilities.DoesContextContainHeader(out List<string> AuthorizedUserName, out string _, _Context, "authorized-u-name") ||
                !BWebUtilities.DoesContextContainHeader(out List<string> AuthorizedUserEmail, out string _, _Context, "authorized-u-email") ||
                !BWebUtilities.DoesContextContainHeader(out List<string> AuthorizedUserAuthMethodKey, out string _, _Context, "authorized-u-auth-key"))
            {
                _ErrorMessageAction?.Invoke("Error: Request headers do not contain authorized-u-* headers.");
                return new AuthorizedRequester()
                {
                    bParseSuccessful = false,
                    ParseErrorMessage = "Expected headers could not be found."
                };
            }
            return new AuthorizedRequester()
            {
                bParseSuccessful = true,
                UserID = AuthorizedUserID[0],
                UserEmail = AuthorizedUserEmail[0],
                UserName = AuthorizedUserName[0],
                AuthMethodKey = AuthorizedUserAuthMethodKey[0]
            };
        }

        public static string GetSelfPublicIP()
        {
            using var Handler = new HttpClientHandler
            {
                SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls,
                ServerCertificateCustomValidationCallback = (a, b, c, d) => true
            };

            using var Client = new HttpClient(Handler);
            try
            {
                using var RequestTask = Client.GetAsync("https://api.ipify.org");
                RequestTask.Wait();

                using var Response = RequestTask.Result;
                using var Content = Response.Content;
                using var ReadResponseTask = Content.ReadAsStringAsync();

                ReadResponseTask.Wait();
                return ReadResponseTask.Result;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public static bool GenerateNonExistentUniqueID(
            WebServiceBaseTimeoutable _Request,
            IBDatabaseServiceInterface _DatabaseService,
            string _TableName,
            string _TableKeyName,
            string[] _TableEntryMustHaveProperties,
            EGetClearance _GetClearance,
            out string _GeneratedUniqueID,
            out BWebServiceResponse _FailureResponse,
            Action<string> _ErrorMessageAction = null)
        {
            _GeneratedUniqueID = null;
            _FailureResponse = BWebResponse.InternalError("");

            int ExistenceTrial = 0;

            while (_GeneratedUniqueID == null && ExistenceTrial < 3)
            {
                if (!BUtility.CalculateStringMD5(BUtility.RandomString(32, false), out _GeneratedUniqueID, _ErrorMessageAction))
                {
                    _FailureResponse = BWebResponse.InternalError("Hashing operation has failed.");
                    return false;
                }

                if (_GetClearance == EGetClearance.Yes && !Controller_AtomicDBOperation.Get().GetClearanceForDBOperation(_Request.InnerProcessor, _TableName, _GeneratedUniqueID, _ErrorMessageAction))
                {
                    _FailureResponse = BWebResponse.InternalError("Atomic operation control has failed.");
                    return false;
                }

                if (!_DatabaseService.GetItem(
                    _TableName,
                    _TableKeyName,
                    new BPrimitiveType(_GeneratedUniqueID),
                    _TableEntryMustHaveProperties,
                    out JObject ExistenceCheck,
                    _ErrorMessageAction))
                {
                    _FailureResponse = BWebResponse.InternalError("Database existence check operation has failed.");
                    return false;
                }
                if (ExistenceCheck != null)
                {
                    if (_GetClearance == EGetClearance.Yes)
                    {
                        Controller_AtomicDBOperation.Get().SetClearanceForDBOperationForOthers(_Request.InnerProcessor, _TableName, _GeneratedUniqueID, _ErrorMessageAction);
                    }

                    _GeneratedUniqueID = null;
                    ExistenceTrial++;
                }
                else break;
            }
            if (_GeneratedUniqueID == null)
            {
                _FailureResponse = BWebResponse.InternalError("Unique model ID generation operation has failed.");
                return false;
            }
            return true;
        }

        public static string GetNowAsLongDateAndTimeString()
        {
            return DateTime.UtcNow.ToLongDateString() + " - " + DateTime.UtcNow.ToLongTimeString();
        }
    }
}
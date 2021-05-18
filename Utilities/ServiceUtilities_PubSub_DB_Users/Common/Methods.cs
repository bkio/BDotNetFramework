/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using Newtonsoft.Json.Linq;
using BCloudServiceUtilities;
using BCommonUtilities;
using BWebServiceUtilities;

namespace ServiceUtilities.Common
{
    public partial class Methods
    {
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
    }
}

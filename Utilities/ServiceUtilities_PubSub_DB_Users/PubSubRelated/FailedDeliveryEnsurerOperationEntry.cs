/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using Newtonsoft.Json;

namespace ServiceUtilities
{
    //DB Table entry
    //KeyName = KEY_NAME_USER_ID
    public class FailedDeliveryEnsurerOperationEntry
    {
        //Service name shall be appended
        public const string DBSERVICE_FAILED_DELIVERY_ENSURER_OPERATIONS_TABLE_PREFIX = "failed-de-ops-";

        public const string KEY_NAME_OPERATION_TIMESTAMP_ID = "operationTimestamp";

        public const string OPERATION_PROPERTY = "operation";

        public static bool GenerateOperationTimestampID(out string _NewOperationID, Action<string> _ErrorMessageAction)
        {
            try
            {
                _NewOperationID = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds().ToString();
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("FailedDeliveryEnsurerOperationEntry->GenerateOperationTimestampID failed with: " + e.Message + ", trace: " + e.StackTrace);
                _NewOperationID = null;
                return false;
            }
            return true;
        }

        [JsonProperty(OPERATION_PROPERTY)]
        public string OperationStringified;
    }
}
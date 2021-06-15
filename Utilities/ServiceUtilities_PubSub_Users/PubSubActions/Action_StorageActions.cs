/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace ServiceUtilities
{
    public abstract class Action_StorageNotification : Action
    {
    }

    /// https://cloud.google.com/storage/docs/json_api/v1/objects#resource-representations
    public class Action_StorageFileUploaded : Action_StorageNotification
    {
        [JsonProperty("bucket")]
        public string BucketName;

        [JsonProperty("name")]
        public string RelativeUrl;

        [JsonProperty("size")]
        public ulong Size;

        [JsonProperty("md5Hash")]
        public string MD5Hash;

        [JsonProperty("crc32c")]
        public string CRC32C;

        [JsonProperty("etag")]
        public string ETag;

        [JsonProperty("contentType")]
        public string ContentType;

        public static bool IsMatch(JObject _Input)
        {
            return _Input.ContainsKey("bucket")
                && _Input.ContainsKey("name")
                && _Input.ContainsKey("size");
        }

        public override Actions.EAction GetActionType()
        {
            return Actions.EAction.ACTION_STORAGE_FILE_UPLOADED;
        }

        //Default Instance
        public static readonly Action_StorageFileUploaded DefaultInstance = new Action_StorageFileUploaded();
        protected override Action GetStaticDefaultInstance() { return DefaultInstance; }
    }

    public class Action_StorageFileDeleted : Action_StorageNotification
    {
        [JsonProperty("bucket")]
        public string BucketName;

        [JsonProperty("name")]
        public string RelativeUrl;

        public static bool IsMatch(JObject _Input)
        {
            return _Input.ContainsKey("bucket")
                && _Input.ContainsKey("name");
        }

        public override Actions.EAction GetActionType()
        {
            return Actions.EAction.ACTION_STORAGE_FILE_DELETED;
        }

        //Default Instance
        public static readonly Action_StorageFileDeleted DefaultInstance = new Action_StorageFileDeleted();
        protected override Action GetStaticDefaultInstance() { return DefaultInstance; }
    }

    public class Action_StorageFileUploaded_CloudEventSchemaV1_0 : Action_StorageNotification
    {
        [JsonProperty("api")]
        public string DataApi;

        [JsonProperty("url")]
        public string RelativeUrl;

        public string CompleteUrl;

        [JsonProperty("contentLength")]
        public ulong Size;

        [JsonProperty("eTag")]
        public string ETag;

        [JsonProperty("contentType")]
        public string ContentType;

        public static bool IsMatch(JObject _Input)
        {
            return _Input.ContainsKey("url")
                && _Input.ContainsKey("api") && _Input.GetValue("api").ToString().StartsWith("Put")
                && _Input.ContainsKey("contentLength");
        }

        public override Actions.EAction GetActionType()
        {
            return Actions.EAction.ACTION_STORAGE_FILE_UPLOADED_CLOUDEVENT;
        }

        public void ConvertUrlToRelativeUrl(string _ServiceEndpointPart)
        {
            CompleteUrl = new string(RelativeUrl);
            if (RelativeUrl.StartsWith(_ServiceEndpointPart))
            {
                RelativeUrl = RelativeUrl.Replace(_ServiceEndpointPart, "");
            }
        }

        //Default Instance
        public static readonly Action_StorageFileUploaded_CloudEventSchemaV1_0 DefaultInstance = new Action_StorageFileUploaded_CloudEventSchemaV1_0();
        protected override Action GetStaticDefaultInstance() { return DefaultInstance; }
    }

    public class Action_StorageFileDeleted_CloudEventSchemaV1_0 : Action_StorageNotification
    {
        [JsonProperty("api")]
        public string DataApi;

        [JsonProperty("url")]
        public string RelativeUrl;

        public string CompleteUrl;

        public static bool IsMatch(JObject _Input)
        {
            return _Input.ContainsKey("api") && _Input.GetValue("api").ToString().StartsWith("Delete")
                && _Input.ContainsKey("url");
        }

        public override Actions.EAction GetActionType()
        {
            return Actions.EAction.ACTION_STORAGE_FILE_DELETED_CLOUDEVENT;
        }

        public void ConvertUrlToRelativeUrl(string _ServiceEndpointPart)
        {
            CompleteUrl = new string(RelativeUrl);
            if (RelativeUrl.StartsWith(_ServiceEndpointPart))
            {
                RelativeUrl = RelativeUrl.Replace(_ServiceEndpointPart, "");
            }
        }

        //Default Instance
        public static readonly Action_StorageFileDeleted_CloudEventSchemaV1_0 DefaultInstance = new Action_StorageFileDeleted_CloudEventSchemaV1_0();
        protected override Action GetStaticDefaultInstance() { return DefaultInstance; }
    }
}

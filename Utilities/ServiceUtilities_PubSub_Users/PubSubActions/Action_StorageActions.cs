/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
}

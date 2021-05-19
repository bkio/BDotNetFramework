/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using Newtonsoft.Json;

namespace ServiceUtilities.Common
{
    public class HistoryRecord
    {
        public const string RECORD_DATE_PROPERTY = "recordDate";
        public const string RECORD_PROCESS_STAGE_PROPERTY = "recordProcessStage";
        public const string RECORD_PROCESS_INFO_PROPERTY = "recordProcessInfo";

        [JsonProperty(RECORD_DATE_PROPERTY)]
        public string RecordDate = Methods.GetUtcNowShortDateAndLongTimeString();

        [JsonProperty(RECORD_PROCESS_STAGE_PROPERTY)]
        public int RecordProcessStage = (int)EProcessStage.Stage0_FileUpload;

        [JsonProperty(RECORD_PROCESS_INFO_PROPERTY)]
        public string ProcessInfo = "";
    }
}

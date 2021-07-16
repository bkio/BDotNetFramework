/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using Newtonsoft.Json;

namespace ServiceUtilities.Common
{
    public class GenericFilter
    {
        public const string FILTER_KEY_PROPERTY = "filterKey";
        public const string FILTER_VALUE_PROPERTY = "filterValue";

        [JsonProperty(FILTER_KEY_PROPERTY)]
        public string FilterKey = "";

        [JsonProperty(FILTER_VALUE_PROPERTY)]
        public string FilterValue = "";
    }
}

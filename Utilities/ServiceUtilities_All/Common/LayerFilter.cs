/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using Newtonsoft.Json;

namespace ServiceUtilities_All.Common
{
    public class LayerFilter
    {
        public const string FILTER_TYPE_PROPERTY = "filterType";

        [JsonProperty(FILTER_TYPE_PROPERTY)]
        public string FilterType = "";
    }
}

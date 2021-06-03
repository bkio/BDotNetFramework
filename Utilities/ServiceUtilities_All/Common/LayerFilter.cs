/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using Newtonsoft.Json;

namespace ServiceUtilities.Common
{
    public class LayerFilter
    {
        public const string FILTER_TYPE_PROPERTY = "filterType";
        public const string FILTER_VALUE_PROPERTY = "filterValue";
        public const string LAYER_NAME_PROPERTY = "layerName";

        [JsonProperty(FILTER_TYPE_PROPERTY)]
        public string FilterType = "";

        [JsonProperty(FILTER_VALUE_PROPERTY)]
        public string FilterValue = "";

        [JsonProperty(LAYER_NAME_PROPERTY)]
        public string LayerName = "";
    }
}

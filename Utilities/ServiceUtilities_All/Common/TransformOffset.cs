/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using Newtonsoft.Json;

namespace ServiceUtilities.Common
{
    public class TransformOffset
    {
        public const string LOCATION_OFFSET_X_PROPERTY = "locationOffsetX";
        public const string LOCATION_OFFSET_Y_PROPERTY = "locationOffsetY";
        public const string LOCATION_OFFSET_Z_PROPERTY = "locationOffsetZ";
        public const string ROTATION_OFFSET_X_PROPERTY = "rotationOffsetX";
        public const string ROTATION_OFFSET_Y_PROPERTY = "rotationOffsetY";
        public const string ROTATION_OFFSET_Z_PROPERTY = "rotationOffsetZ";
        public const string UNIFORM_SCALE_PROPERTY = "uniformScale";

        [JsonProperty(LOCATION_OFFSET_X_PROPERTY)]
        public float LocationOffsetX = 0;

        [JsonProperty(LOCATION_OFFSET_Y_PROPERTY)]
        public float LocationOffsetY = 0;

        [JsonProperty(LOCATION_OFFSET_Z_PROPERTY)]
        public float LocationOffsetZ = 0;

        [JsonProperty(ROTATION_OFFSET_X_PROPERTY)]
        public float RotationOffsetX = 0;

        [JsonProperty(ROTATION_OFFSET_Y_PROPERTY)]
        public float RotationOffsetY = 0;

        [JsonProperty(ROTATION_OFFSET_Z_PROPERTY)]
        public float RotationOffsetZ = 0;

        [JsonProperty(UNIFORM_SCALE_PROPERTY)]
        public float UniformScale = 0;
    }
}

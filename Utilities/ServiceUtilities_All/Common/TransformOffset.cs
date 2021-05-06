/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using Newtonsoft.Json;

namespace ServiceUtilities_All.Common
{
    public class TransformOffset
    {
        public const string LOCATION_OFFSET_X_PROPERTY = "locationOffsetX";
        public const string LOCATION_OFFSET_Y_PROPERTY = "locationOffsetY";
        public const string LOCATION_OFFSET_Z_PROPERTY = "locationOffsetZ";
        public const string QUATERNION_ROTATION_OFFSET_X_PROPERTY = "quaternionRotationOffsetX";
        public const string QUATERNION_ROTATION_OFFSET_Y_PROPERTY = "quaternionRotationOffsetY";
        public const string QUATERNION_ROTATION_OFFSET_Z_PROPERTY = "quaternionRotationOffsetZ";
        public const string QUATERNION_ROTATION_OFFSET_W_PROPERTY = "quaternionRotationOffsetW";
        public const string SCALE_X_PROPERTY = "scaleX";
        public const string SCALE_Y_PROPERTY = "scaleY";
        public const string SCALE_Z_PROPERTY = "scaleZ";

        [JsonProperty(LOCATION_OFFSET_X_PROPERTY)]
        public float LocationOffsetX = 0;

        [JsonProperty(LOCATION_OFFSET_Y_PROPERTY)]
        public float LocationOffsetY = 0;

        [JsonProperty(LOCATION_OFFSET_Z_PROPERTY)]
        public float LocationOffsetZ = 0;

        [JsonProperty(QUATERNION_ROTATION_OFFSET_X_PROPERTY)]
        public float QuaternionRotationOffsetX = 0;

        [JsonProperty(QUATERNION_ROTATION_OFFSET_Y_PROPERTY)]
        public float QuaternionRotationOffsetY = 0;

        [JsonProperty(QUATERNION_ROTATION_OFFSET_Z_PROPERTY)]
        public float QuaternionRotationOffsetZ = 0;

        [JsonProperty(QUATERNION_ROTATION_OFFSET_W_PROPERTY)]
        public float QuaternionRotationOffsetW = 0;

        [JsonProperty(SCALE_X_PROPERTY)]
        public float ScaleX = 0;

        [JsonProperty(SCALE_Y_PROPERTY)]
        public float ScaleY = 0;

        [JsonProperty(SCALE_Z_PROPERTY)]
        public float ScaleZ = 0;
    }
}

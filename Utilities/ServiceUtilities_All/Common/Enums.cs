namespace ServiceUtilities_All.Common
{
    public enum EVMStatus
    {
        Available,
        Busy,
        NotResponding
    }

    public enum EProcessStage
    {
        Stage0_FileUploaded,
        Stage1_PullingData,
        Stage2_ExtractingGeometryInfo,
        Stage3_OptimizingModel,
        Stage4_FilteringModel,
        Stage5_CustomPlatformConvertion,
        Stage6_UnrealEngineConvertion,
        Stage7_Completed
    }
    public enum EProcessStatus
    {
        Idle,
        Processing,
        Canceled,
        Failed,
        Completed
    }
    public enum EUploadProcessStage : int
    {
        NotUploaded = 0,
        Uploaded_Processing = 1,
        Uploaded_ProcessFailed = 2,
        Uploaded_Processed = 3
    }
    public enum EOptimizationPreset
    {
        Default,
        LargeFiles_WideOpenFactory_WithUniformCompactness,
        MediumFiles_WideOpenFactory_WithUniformCompactness,
        WideArea_WithCompactModels,
        VeryDenseVessel,
        TopsideOilRig
    }
}

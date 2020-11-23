/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ServiceUtilities
{
    public class ZipAndUploadModelToCdf
    {
        public ZipAndUploadModelToCdf() { }

        [JsonProperty("downloadUrl")]
        public string DownloadURL = "";

        [JsonProperty("modelId")]
        public string ModelID = "";

        [JsonProperty("revisionIndex")]
        public int RevisionIndex = 0;

        [JsonProperty("fileEntryName")]
        public string FileEntryName = "";

        [JsonProperty("fileEntryFileType")]
        public string FileEntryFileType = "";

        [JsonProperty("facilityNameIfExists")]
        public string FacilityName_IfExists = "";

        [JsonProperty("dataSource")]
        public string DataSource = "NULL";

        public override bool Equals(object _Other)
        {
            return _Other is ZipAndUploadModelToCdf Casted &&
                    DownloadURL == Casted.DownloadURL &&
                    ModelID == Casted.ModelID &&
                    RevisionIndex == Casted.RevisionIndex &&
                    FileEntryName == Casted.FileEntryName &&
                    FileEntryFileType == Casted.FileEntryFileType &&
                    FacilityName_IfExists == Casted.FacilityName_IfExists &&
                    DataSource == Casted.DataSource;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(HashCode.Combine(DownloadURL, ModelID, RevisionIndex), HashCode.Combine(FileEntryName, FileEntryFileType, FacilityName_IfExists, DataSource));
        }
        public static bool operator ==(ZipAndUploadModelToCdf x, ZipAndUploadModelToCdf y)
        {
            return x.Equals(y);
        }
        public static bool operator !=(ZipAndUploadModelToCdf x, ZipAndUploadModelToCdf y)
        {
            return !x.Equals(y);
        }
    }

    public class Action_ZipAndUploadModelsToCdf : Action
    {
        [JsonProperty("models")]
        public List<ZipAndUploadModelToCdf> Models = new List<ZipAndUploadModelToCdf>();

        [JsonProperty("projectName")]
        public string ProjectName = "";

        public Action_ZipAndUploadModelsToCdf() { }
        public Action_ZipAndUploadModelsToCdf(string _ProjectName, List<ZipAndUploadModelToCdf> _Models)
        {
            ProjectName = _ProjectName;
            Models = _Models;
        }

        public override bool Equals(object _Other)
        {
            return _Other is Action_ZipAndUploadModelsToCdf Casted &&
                    ProjectName == Casted.ProjectName &&
                    Models == Casted.Models;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ProjectName, Models);
        }

        public override Actions.EAction GetActionType()
        {
            return Actions.EAction.ACTION_ZIP_AND_UPLOAD_MODELS_TO_CDF;
        }

        //Default Instance
        public static readonly Action_ZipAndUploadModelsToCdf DefaultInstance = new Action_ZipAndUploadModelsToCdf();
        protected override Action GetStaticDefaultInstance() { return DefaultInstance; }
    }
}
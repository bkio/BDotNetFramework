/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Storage.v1;
using Google.Cloud.Storage.V1;
using BCommonUtilities;
using System.Threading.Tasks;

namespace BCloudServiceUtilities.FileServices
{
    public class BFileServiceGC : IBFileServiceInterface
    {
        /// <summary>Google Storage Client that is responsible to serve to this object</summary>
        private StorageClient GSClient;

        /// <summary>Holds initialization success</summary>
        private readonly bool bInitializationSucceed;

        private readonly GoogleCredential Credential;
        private readonly ServiceAccountCredential CredentialScoped;

        private readonly string ProjectID;

        /// <summary>
        ///
        /// <para>BFileServiceGC: Parametered Constructor:</para>
        /// 
        /// <para><paramref name="_ProjectID"/>                       GC Project ID</para>
        /// <para><paramref name="_ErrorMessageAction"/>              Error messages will be pushed to this action</para>
        ///
        /// </summary>
        public BFileServiceGC(
            string _ProjectID,
            Action<string> _ErrorMessageAction = null)
        {
            try
            {
                string ApplicationCredentials = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
                string ApplicationCredentialsPlain = Environment.GetEnvironmentVariable("GOOGLE_PLAIN_CREDENTIALS");
                if (ApplicationCredentials == null && ApplicationCredentialsPlain == null)
                {
                    _ErrorMessageAction?.Invoke("BFileServiceGC->Constructor: GOOGLE_APPLICATION_CREDENTIALS (or GOOGLE_PLAIN_CREDENTIALS) environment variable is not defined.");
                    bInitializationSucceed = false;
                }
                else
                {
                    ProjectID = _ProjectID;

                    if (ApplicationCredentials == null)
                    {
                        if (!BUtility.HexDecode(out ApplicationCredentialsPlain, ApplicationCredentialsPlain, _ErrorMessageAction))
                        {
                            throw new Exception("Hex decode operation for application credentials plain has failed.");
                        }
                        Credential = GoogleCredential.FromJson(ApplicationCredentialsPlain);
                        CredentialScoped = Credential.CreateScoped(
                                new string[]
                                {
                                    StorageService.Scope.DevstorageReadWrite
                                })
                                .UnderlyingCredential as ServiceAccountCredential;
                    }
                    else
                    {
                        using (var Stream = new FileStream(ApplicationCredentials, FileMode.Open, FileAccess.Read))
                        {
                            Credential = GoogleCredential.FromStream(Stream);
                            CredentialScoped = Credential.CreateScoped(
                                    new string[]
                                    {
                                        StorageService.Scope.DevstorageReadWrite
                                    })
                                    .UnderlyingCredential as ServiceAccountCredential;
                        }
                    }

                    if (Credential != null)
                    {
                        GSClient = StorageClient.Create(Credential);

                        bInitializationSucceed = GSClient != null;
                    }
                    else
                    {
                        bInitializationSucceed = false;
                    }
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BFileServiceGC->Constructor: " + e.Message + ", Trace: " + e.StackTrace);
                bInitializationSucceed = false;
            }
        }
        ~BFileServiceGC()
        {
            GSClient?.Dispose();
        }

        /// <summary>
        ///
        /// <para>HasInitializationSucceed:</para>
        /// 
        /// <para>Check <seealso cref="IBFileServiceInterface.HasInitializationSucceed"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool HasInitializationSucceed()
        {
            return bInitializationSucceed;
        }

        /// <summary>
        ///
        /// <para>CopyFile:</para>
        ///
        /// <para>Copy a file from a bucket and relative location to another in File Service, caller thread will be blocked before it is done</para>
        ///
        /// <para>Check <seealso cref="IBFileServiceInterface.CopyFile"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool CopyFile(
            string _SourceBucketName,
            string _SourceKeyInBucket,
            string _DestinationBucketName,
            string _DestinationKeyInBucket,
            EBRemoteFileReadPublicity _RemoteFileReadAccess = EBRemoteFileReadPublicity.AuthenticatedRead,
            Action<string> _ErrorMessageAction = null)
        {
            if (GSClient == null)
            {
                _ErrorMessageAction?.Invoke("BFileServiceGC->CopyFile: GSClient is null.");
                return false;
            }
            
            PredefinedObjectAcl CloudTypePublicity;
            if (_RemoteFileReadAccess == EBRemoteFileReadPublicity.PublicRead)
            {
                CloudTypePublicity = PredefinedObjectAcl.PublicRead;
            }
            else if (_RemoteFileReadAccess == EBRemoteFileReadPublicity.ProjectWideProtectedRead)
            {
                CloudTypePublicity = PredefinedObjectAcl.ProjectPrivate;
            }
            else
            {
                CloudTypePublicity = PredefinedObjectAcl.AuthenticatedRead;
            }

            try
            {
                var CopiedObject = GSClient.CopyObject(_SourceBucketName, _SourceKeyInBucket, _DestinationBucketName, _DestinationKeyInBucket, new CopyObjectOptions
                {
                    DestinationPredefinedAcl = CloudTypePublicity
                });

                if (CopiedObject == null)
                {
                    _ErrorMessageAction?.Invoke("BFileServiceGC->CopyFile: Operation has failed.");
                    return false;
                }

                CopiedObject.CacheControl = CloudTypePublicity == PredefinedObjectAcl.PublicRead ? "public" : "private";
                GSClient.PatchObject(CopiedObject, null);
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BFileServiceGC->CopyFile: " + e.Message + ", Trace: " + e.StackTrace);
                return false;
            }
            return true;
        }

        /// <summary>
        ///
        /// <para>DeleteFile:</para>
        ///
        /// <para>Deletes a file from File Service, caller thread will be blocked before it is done</para>
        ///
        /// <para>Check <seealso cref="IBFileServiceInterface.DeleteFile"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool DeleteFile(
            string _BucketName,
            string _KeyInBucket,
            Action<string> _ErrorMessageAction = null)
        {
            if (GSClient == null)
            {
                _ErrorMessageAction?.Invoke("BFileServiceGC->DeleteFile: GSClient is null.");
                return false;
            }

            try
            {
                GSClient.DeleteObject(_BucketName, _KeyInBucket);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        ///
        /// <para>DeleteFolder:</para>
        ///
        /// <para>Deletes a folder from File Service, caller thread will be blocked before it is done</para>
        ///
        /// <para>Check <seealso cref="IBFileServiceInterface.DeleteFile"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool DeleteFolder(
            string _BucketName,
            string _Folder,
            Action<string> _ErrorMessageAction = null)
        {
            if (GSClient == null)
            {
                _ErrorMessageAction?.Invoke("BFileServiceGC->DeleteFolder: GSClient is null.");
                return false;
            }

            try
            {
                var ListResult = GSClient.ListObjects(_BucketName, _Folder);
                if (ListResult == null) return true;

                foreach (var Current in ListResult)
                {
                    GSClient.DeleteObject(Current);
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        ///
        /// <para>DownloadFile:</para>
        ///
        /// <para>Downloads a file from File Service and stores locally/or to stream, caller thread will be blocked before it is done</para>
        ///
        /// <para>Check <seealso cref="IBFileServiceInterface.DownloadFile"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool DownloadFile(
            string _BucketName,
            string _KeyInBucket,
            BStringOrStream _Destination,
            Action<string> _ErrorMessageAction = null,
            UInt64 _StartIndex = 0,
            UInt64 _Size = 0)
        {
            if (GSClient == null)
            {
                _ErrorMessageAction?.Invoke("BFileServiceGC->DownloadFile: GSClient is null.");
                return false;
            }

            if (!CheckFileExistence(
                _BucketName,
                _KeyInBucket,
                out bool bExists,
                _ErrorMessageAction
                ))
            {
                _ErrorMessageAction?.Invoke("BFileServiceGC->DownloadFile: CheckFileExistence failed.");
                return false;
            }

            if (!bExists)
            {
                _ErrorMessageAction?.Invoke("BFileServiceGC->DownloadFile: File does not exist in the File Service.");
                return true;
            }

            DownloadObjectOptions DOO = null;
            if (_Size > 0)
            {
                DOO = new DownloadObjectOptions()
                {
                    Range = new System.Net.Http.Headers.RangeHeaderValue((long)_StartIndex, (long)(_StartIndex + _Size))
                };
            }

            try
            {
                if (_Destination.Type == EBStringOrStreamEnum.String)
                {
                    using (FileStream FS = File.Create(_Destination.String))
                    {
                        GSClient.DownloadObject(_BucketName, _KeyInBucket, FS, DOO);
                    }

                    if (!BUtility.DoesFileExist(
                        _Destination.String,
                        out bool bLocalFileExists,
                        _ErrorMessageAction))
                    {
                        _ErrorMessageAction?.Invoke("BFileServiceGC->DownloadFile: DoesFileExist failed.");
                        return false;
                    }

                    if (!bLocalFileExists)
                    {
                        _ErrorMessageAction?.Invoke("BFileServiceGC->DownloadFile: Download finished, but still file does not locally exist.");
                        return false;
                    }
                }
                else
                {
                    if (_Destination.Stream == null)
                    {
                        _ErrorMessageAction?.Invoke("BFileServiceGC->DownloadFile: Destination stream is null.");
                        return false;
                    }

                    GSClient.DownloadObject(_BucketName, _KeyInBucket, _Destination.Stream, DOO);
                    try
                    {
                        _Destination.Stream.Position = 0;
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BFileServiceGC->DownloadFile: " + e.Message + ", Trace: " + e.StackTrace);
                return false;
            }
            return true;
        }
        /// <summary>
        ///
        /// <para>CreateSignedURLForUpload:</para>
        ///
        /// <para>Creates signed url for uploading a file</para>
        ///
        /// <para>Check <seealso cref="IBFileServiceInterface.CreateSignedURLForUpload"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool CreateSignedURLForUpload(
            out string _SignedUrl,
            string _BucketName,
            string _KeyInBucket,
            string _ContentType,
            int _URLValidForMinutes = 60,
            Action<string> _ErrorMessageAction = null)
        {
            var ContentHeaders = new Dictionary<string, IEnumerable<string>>
            {
                { "Content-Type", new string[] { _ContentType } }
            };

            try
            {                  
                UrlSigner Signer = UrlSigner.FromServiceAccountCredential(CredentialScoped);
                _SignedUrl = Signer.Sign(_BucketName, _KeyInBucket, TimeSpan.FromMinutes(_URLValidForMinutes), HttpMethod.Put, null, ContentHeaders);
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BFileServiceGC->CreateSignedURLForUpload: " + e.Message + ", Trace: " + e.StackTrace);
                _SignedUrl = null;
                return false;
            }
            return true;
        }

        /// <summary>
        ///
        /// <para>CreateSignedURLForDownload:</para>
        /// 
        /// <para>Creates signed url for downloading a file</para>
        ///
        /// <para>Check <seealso cref="IBFileServiceInterface.CreateSignedURLForDownload"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool CreateSignedURLForDownload(
            out string _SignedUrl,
            string _BucketName,
            string _KeyInBucket,
            int _URLValidForMinutes = 1,
            Action<string> _ErrorMessageAction = null)
        {
            try
            {
                UrlSigner Signer = UrlSigner.FromServiceAccountCredential(CredentialScoped);
                _SignedUrl = Signer.Sign(_BucketName, _KeyInBucket, TimeSpan.FromMinutes(_URLValidForMinutes), HttpMethod.Get);
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BFileServiceGC->CreateSignedURLForDownload: " + e.Message + ", Trace: " + e.StackTrace);
                _SignedUrl = null;
                return false;
            }
            return true;
        }

        /// <summary>
        ///
        /// <para>GetFileSize:</para>
        ///
        /// <para>Gets size of a file in bytes from File Service, caller thread will be blocked before it is done</para>
        ///
        /// <para>Check <seealso cref="IBFileServiceInterface.GetFileSize"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool GetFileSize(
            string _BucketName,
            string _KeyInBucket,
            out ulong _FileSize,
            Action<string> _ErrorMessageAction = null)
        {
            _FileSize = 0;

            if (GSClient == null)
            {
                _ErrorMessageAction?.Invoke("BFileServiceGC->GetFileSize: GSClient is null.");
                return false;
            }

            try
            {
                var ResultObject = GSClient.GetObject(_BucketName, _KeyInBucket);
                if (ResultObject == null || !ResultObject.Size.HasValue)
                {
                    _ErrorMessageAction?.Invoke("BFileServiceGC->GetFileSize: GetObject Response is null or Size object does not have value.");
                    return false;
                }
                _FileSize = ResultObject.Size.Value;
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BFileServiceGC->GetFileSize: " + e.Message + ", Trace: " + e.StackTrace);
                return false;
            }
            return true;
        }

        /// <summary>
        ///
        /// <para>GetFileChecksum:</para>
        ///
        /// <para>Gets MD5 checksum of a file from File Service, caller thread will be blocked before it is done</para>
        ///
        /// <para>Check <seealso cref="IBFileServiceInterface.GetFileChecksum"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool GetFileChecksum(
            string _BucketName,
            string _KeyInBucket,
            out string _Checksum,
            Action<string> _ErrorMessageAction = null)
        {
            _Checksum = null;

            if (GSClient == null)
            {
                _ErrorMessageAction?.Invoke("BFileServiceGC->GetFileChecksum: GSClient is null.");
                return false;
            }

            try
            {
                var ResultObject = GSClient.GetObject(_BucketName, _KeyInBucket);
                
                if (ResultObject == null || ResultObject.Md5Hash == null)
                {
                    _ErrorMessageAction?.Invoke("BFileServiceGC->GetFileChecksum: GetObject Response is null or Size object does not have value.");
                    return false;
                }
                _Checksum = BitConverter.ToString(Convert.FromBase64String(ResultObject.Md5Hash)).Replace("-", string.Empty).ToLower();
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BFileServiceGC->GetFileChecksum: " + e.Message + ", Trace: " + e.StackTrace);
                return false;
            }
            return true;
        }

        /// <summary>
        ///
        /// <para>GetFileTags:</para>
        ///
        /// <para>Gets the tags of the file from the file service</para>
        ///
        /// <para>Check <seealso cref="IBFileServiceInterface.GetFileTags"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool GetFileTags(
            string _BucketName,
            string _KeyInBucket,
            out List<Tuple<string, string>> _Tags,
            Action<string> _ErrorMessageAction = null)
        {
            _Tags = new List<Tuple<string, string>>();

            if (GSClient == null)
            {
                _ErrorMessageAction?.Invoke("BFileServiceGC->GetFileTags: GSClient is null.");
                return false;
            }

            try
            {
                var ResultObject = GSClient.GetObject(_BucketName, _KeyInBucket);
                if (ResultObject == null || ResultObject.Metadata == null)
                {
                    _ErrorMessageAction?.Invoke("BFileServiceGC->GetFileTags: GetObject Response or Metadata object is null.");
                    return false;
                }

                foreach (var CurrentMetadata in ResultObject.Metadata)
                {
                    _Tags.Add(new Tuple<string, string>(CurrentMetadata.Key, CurrentMetadata.Value));
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BFileServiceGC->GetFileTags: " + e.Message + ", Trace: " + e.StackTrace);
                return false;
            }
            return true;
        }

        /// <summary>
        ///
        /// <para>SetFileTags:</para>
        ///
        /// <para>Sets the tags of the file in the file service, existing tags for the file in the cloud will be deleted</para>
        ///
        /// <para>Check <seealso cref="IBFileServiceInterface.SetFileTags"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool SetFileTags(
            string _BucketName,
            string _KeyInBucket,
            Tuple<string, string>[] _Tags,
            Action<string> _ErrorMessageAction = null)
        {
            if (GSClient == null)
            {
                _ErrorMessageAction?.Invoke("BFileServiceGC->SetFileTags: GSClient is null.");
                return false;
            }

            if (_Tags == null || _Tags.Length == 0)
            {
                _ErrorMessageAction?.Invoke("BFileServiceGC->SetFileTags: Tags parameter is null or empty.");
                return false;
            }

            try
            {
                var ExistingObject = GSClient.GetObject(_BucketName, _KeyInBucket);
                if (ExistingObject == null)
                {
                    _ErrorMessageAction?.Invoke("BFileServiceGC->SetFileTags: Response of GetObject for existing object is null.");
                    return false;
                }

                Dictionary<string, string> NewMetadata = new Dictionary<string, string>();
                foreach (Tuple<string, string> CurrentTag in _Tags)
                {
                    NewMetadata.Add(CurrentTag.Item1, CurrentTag.Item2);
                }
                ExistingObject.Metadata = NewMetadata;

                var FileName = _KeyInBucket;
                var FileNameIndex = _KeyInBucket.LastIndexOf("/") + 1;
                if (FileNameIndex > 0)
                {
                    FileName = _KeyInBucket.Substring(FileNameIndex, _KeyInBucket.Length - FileNameIndex);
                }
                ExistingObject.ContentDisposition = "inline; filename=" + FileName;

                var UpdatedObject = GSClient.UpdateObject(ExistingObject);
                if (UpdatedObject == null)
                {
                    _ErrorMessageAction?.Invoke("BFileServiceGC->SetFileTags: Response of GetObject for updated object is null.");
                    return false;
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BFileServiceGC->SetFileTags: " + e.Message + ", Trace: " + e.StackTrace);
                return false;
            }
            return true;
        }

        /// <summary>
        ///
        /// <para>CheckFileExistence:</para>
        ///
        /// <para>Checks existence of an object in File Service, caller thread will be blocked before it is done</para>
        ///
        /// <para>Check <seealso cref="IBFileServiceInterface.CheckFileExistence"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool CheckFileExistence(
            string _BucketName,
            string _KeyInBucket,
            out bool _bExists,
            Action<string> _ErrorMessageAction = null)
        {
            _bExists = false;

            if (GSClient == null)
            {
                _ErrorMessageAction?.Invoke("BFileServiceGC->CheckFileExistence: GSClient is null.");
                return false;
            }

            try
            {
                if (GSClient.GetObject(_BucketName, _KeyInBucket) != null)
                {
                    _bExists = true;
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BFileServiceGC->CheckFileExistence: " + e.Message + ", Trace: " + e.StackTrace);
                return false;
            }
            return true;
        }

        /// <summary>
        ///
        /// <para>UploadFile:</para>
        ///
        /// <para>Uploads a local file to File Service, caller thread will be blocked before it is done</para>
        ///
        /// <para>Check <seealso cref="IBFileServiceInterface.UploadFile"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool UploadFile(
            BStringOrStream _LocalFileOrStream,
            string _BucketName,
            string _KeyInBucket,
            EBRemoteFileReadPublicity _RemoteFileReadAccess = EBRemoteFileReadPublicity.AuthenticatedRead,
            Tuple<string, string>[] _FileTags = null,
            Action<string> _ErrorMessageAction = null)
        {
            if (GSClient == null)
            {
                _ErrorMessageAction?.Invoke("BFileServiceGC->UploadFile: GSClient is null.");
                return false;
            }

            PredefinedObjectAcl CloudTypePublicity;
            if (_RemoteFileReadAccess == EBRemoteFileReadPublicity.PublicRead)
            {
                CloudTypePublicity = PredefinedObjectAcl.PublicRead;
            }
            else if (_RemoteFileReadAccess == EBRemoteFileReadPublicity.ProjectWideProtectedRead)
            {
                CloudTypePublicity = PredefinedObjectAcl.ProjectPrivate;
            }
            else
            {
                CloudTypePublicity = PredefinedObjectAcl.AuthenticatedRead;
            }

            Google.Apis.Storage.v1.Data.Object UploadedObject = null;

            if (_LocalFileOrStream.Type == EBStringOrStreamEnum.String)
            {
                if (!BUtility.DoesFileExist(
                    _LocalFileOrStream.String,
                    out bool bLocalFileExists,
                    _ErrorMessageAction))
                {
                    _ErrorMessageAction?.Invoke("BFileServiceGC->UploadFile: DoesFileExist failed.");
                    return false;
                }

                if (!bLocalFileExists)
                {
                    _ErrorMessageAction?.Invoke("BFileServiceGC->UploadFile: Local file does not exist.");
                    return false;
                }

                using (FileStream FS = new FileStream(_LocalFileOrStream.String, FileMode.Open, FileAccess.Read))
                {
                    try
                    {
                        UploadedObject = GSClient.UploadObject(_BucketName, _KeyInBucket, null, FS, new UploadObjectOptions
                        {
                            PredefinedAcl = CloudTypePublicity
                        });

                        if (UploadedObject == null)
                        {
                            _ErrorMessageAction?.Invoke("BFileServiceGC->UploadFile: Operation has failed.");
                            return false;
                        }
                    }
                    catch (Exception e)
                    {
                        _ErrorMessageAction?.Invoke("BFileServiceGC->UploadFile: " + e.Message + ", Trace: " + e.StackTrace);
                        return false;
                    }
                }
            }
            else
            {
                try
                {
                    UploadedObject = GSClient.UploadObject(_BucketName, _KeyInBucket, null, _LocalFileOrStream.Stream, new UploadObjectOptions
                    {
                        PredefinedAcl = CloudTypePublicity
                    });

                    if (UploadedObject == null)
                    {
                        _ErrorMessageAction?.Invoke("BFileServiceGC->UploadFile: Operation has failed.");
                        return false;
                    }
                }
                catch (Exception e)
                {
                    _ErrorMessageAction?.Invoke("BFileServiceGC->UploadFile: " + e.Message + ", Trace: " + e.StackTrace);
                    return false;
                }
            }

            if (UploadedObject != null)
            {
                UploadedObject.CacheControl = CloudTypePublicity == PredefinedObjectAcl.PublicRead ? "public" : "private";

                if (_FileTags != null && _FileTags.Length > 0)
                {
                    Dictionary<string, string> NewMetadata = new Dictionary<string, string>();
                    foreach (Tuple<string, string> CurrentTag in _FileTags)
                    {
                        NewMetadata.Add(CurrentTag.Item1, CurrentTag.Item2);
                    }
                    UploadedObject.Metadata = NewMetadata;
                }

                var FileName = _KeyInBucket;
                var FileNameIndex = _KeyInBucket.LastIndexOf("/") + 1;
                if (FileNameIndex > 0)
                {
                    FileName = _KeyInBucket.Substring(FileNameIndex, _KeyInBucket.Length - FileNameIndex);
                }
                UploadedObject.ContentDisposition = "inline; filename=" + FileName;
                GSClient.PatchObject(UploadedObject, null);
            }
            return true;
        }

        /// <summary>
        ///
        /// <para>SetFileAccessibility:</para>
        ///
        /// <para>Changes accessibility of a file in the File Service</para>
        ///
        /// <para>Check <seealso cref="IBFileServiceInterface.SetFileAccessibility"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool SetFileAccessibility(
            string _BucketName,
            string _KeyInBucket,
            EBRemoteFileReadPublicity _RemoteFileReadAccess = EBRemoteFileReadPublicity.AuthenticatedRead,
            Action<string> _ErrorMessageAction = null)
        {
            if (GSClient == null)
            {
                _ErrorMessageAction?.Invoke("BFileServiceGC->SetFileAccessibility: GSClient is null.");
                return false;
            }

            PredefinedObjectAcl CloudTypePublicity;
            if (_RemoteFileReadAccess == EBRemoteFileReadPublicity.PublicRead)
            {
                CloudTypePublicity = PredefinedObjectAcl.PublicRead;
            }
            else if (_RemoteFileReadAccess == EBRemoteFileReadPublicity.ProjectWideProtectedRead)
            {
                CloudTypePublicity = PredefinedObjectAcl.ProjectPrivate;
            }
            else
            {
                CloudTypePublicity = PredefinedObjectAcl.AuthenticatedRead;
            }

            try
            {
                var ExistingObject = GSClient.GetObject(_BucketName, _KeyInBucket);
                if (ExistingObject == null)
                {
                    _ErrorMessageAction?.Invoke("BFileServiceGC->SetFileAccessibility: Response of GetObject for existing object is null.");
                    return false;
                }

                ExistingObject.CacheControl = CloudTypePublicity == PredefinedObjectAcl.PublicRead ? "public" : "private";

                var UpdatedObject = GSClient.UpdateObject(ExistingObject, new UpdateObjectOptions
                {
                    PredefinedAcl = CloudTypePublicity
                });
                if (UpdatedObject == null)
                {
                    _ErrorMessageAction?.Invoke("BFileServiceGC->SetFileAccessibility: Response of GetObject for updated object is null.");
                    return false;
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BFileServiceGC->SetFileAccessibility: " + e.Message + ", Trace: " + e.StackTrace);
                return false;
            }
            return true;
        }

        /// <summary>
        ///
        /// <para>ListAllFilesInBucket:</para>
        ///
        /// <para>Lists keys of all files</para>
        ///
        /// <para>Check <seealso cref="IBFileServiceInterface.ListAllFilesInBucket"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool ListAllFilesInBucket(
             string _BucketName,
             out List<string> _FileKeys,
             Action<string> _ErrorMessageAction = null)
        {
            _FileKeys = null;

            var AllObjects = new List<string>();

            try
            {
                foreach (var StorageObject in GSClient.ListObjects(_BucketName))
                {
                    if (StorageObject != null)
                    {
                        AllObjects.Add(StorageObject.Name);
                    }
                }
            }
            catch (Exception)
            {
                AllObjects = null;
                _ErrorMessageAction?.Invoke("BFileServiceGC->ListUsers: Google Cloud Error");
                return false;
            }

            if (AllObjects.Count > 0)
            {
                _FileKeys = AllObjects;
            }
            return true;
        }

        /// <summary>
        ///
        /// <para>CreateFilePubSubNotification:</para>
        ///
        /// <para>Creates file based pub/sub notification</para>
        ///
        /// <para>Check <seealso cref="IBFileServiceInterface.CreateFilePubSubNotification"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool CreateFilePubSubNotification(string _BucketName, string _TopicName, string _PathPrefixToListen, List<EBFilePubSubNotificationEventType> _EventsToListen, Action<string> _ErrorMessageAction = null)
        {
            var EventTypes = new List<string>();
            if (_EventsToListen.Contains(EBFilePubSubNotificationEventType.Uploaded))
                EventTypes.Add("OBJECT_FINALIZE");
            if (_EventsToListen.Contains(EBFilePubSubNotificationEventType.Deleted))
                EventTypes.Add("OBJECT_DELETE");

            try
            {
                var Created = GSClient.CreateNotification(_BucketName, new Google.Apis.Storage.v1.Data.Notification()
                {
                    PayloadFormat = "JSON_API_V1",
                    Topic = "//pubsub.googleapis.com/projects/" + ProjectID + "/topics/" + _TopicName,
                    EventTypes = EventTypes,
                    ObjectNamePrefix = _PathPrefixToListen
                });
                if (Created == null)
                {
                    _ErrorMessageAction?.Invoke("BFileServiceGC->CreateFilePubSubNotification: Notification could not be created.");
                    return false;
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BFileServiceGC->CreateFilePubSubNotification: " + e.Message + ", trace: " + e.StackTrace);
                return false;
            }
            return true;
        }

        /// <summary>
        ///
        /// <para>DeleteAllFilePubSubNotifications:</para>
        ///
        /// <para>Deletes all file pub/sub notifications for a bucket/topic</para>
        ///
        /// <para>Parameters:</para>
        /// <para><paramref name="_BucketName"/>                  Name of the Bucket</para>
        /// <para><paramref name="_TopicName"/>                   Optional topic name to be pushed; if null; all will be deleted</para>
        /// <para><paramref name="_ErrorMessageAction"/>          Error messages will be pushed to this action</para>
        ///
        /// <returns>                                             Returns: Operation success </returns>  
        ///
        /// </summary>
        public bool DeleteAllFilePubSubNotifications(
            string _BucketName,
            string _TopicName = null,
            Action<string> _ErrorMessageAction = null)
        {
            if (_TopicName != null)
            {
                _TopicName = "//pubsub.googleapis.com/projects/" + ProjectID + "/topics/" + _TopicName;
            }
            try
            {
                var Created = GSClient.ListNotifications(_BucketName);
                if (Created == null || Created.Count == 0)
                {
                    return true;
                }

                foreach (var Current in Created)
                {
                    if (_TopicName == null || Current.Topic == _TopicName)
                    {
                        //Async cannot be used to run in parallel; throws "The project exceeded the rate limit for creating and deleting buckets"
                        GSClient.DeleteNotification(_BucketName, Current.Id);
                    }
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BFileServiceGC->DeleteAllFilePubSubNotifications: " + e.Message + ", trace: " + e.StackTrace);
                return false;
            }
            return true;
        }
    }
}
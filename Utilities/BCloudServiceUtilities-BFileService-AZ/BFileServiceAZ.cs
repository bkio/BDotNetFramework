using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using BCloudServiceUtilities;
using BCommonUtilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace BCloudServiceUtilities_BFileService_AZ
{
    public class BFileServiceAZ : IBFileServiceInterface
    {
        private readonly BlobServiceClient AServiceClient;
        private readonly bool bInitializationSucceed;

        public BFileServiceAZ(string _ConnectionString, Action<string> _ErrorMessageAction = null)
        {
            try
            {
                AServiceClient = new BlobServiceClient(_ConnectionString);
                bInitializationSucceed = true;
            }
            catch (Exception ex)
            {
                _ErrorMessageAction?.Invoke($"BFileServiceAZ Initialization failed : {ex.Message}\n{ex.StackTrace}");
                bInitializationSucceed = false;
            }
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
        public bool CheckFileExistence(string _BucketName, string _KeyInBucket, out bool _bExists, Action<string> _ErrorMessageAction = null)
        {
            try
            {
                BlobContainerClient ContainerClient = AServiceClient.GetBlobContainerClient(_BucketName);
                BlobClient Blob = ContainerClient.GetBlobClient(_KeyInBucket);

                //Will throw exception on failure
                Response<bool> Response = Blob.Exists();

                _bExists = Response.Value;
                return true;
            }
            catch (Exception ex)
            {
                _ErrorMessageAction?.Invoke($"BFileServiceAZ -> CheckFileExistence : {ex.Message}\n{ex.StackTrace}");
                _bExists = false;
                return false;
            }
        }

        /// <summary>
        ///
        /// <para>CopyFile:</para>
        ///
        /// <para>Copy a file from a bucket and relative location to another in File Service, caller thread will be blocked before it is done</para>
        /// 
        /// <para>EBRemoteFileReadPublicity does nothing as Azure does not support per object authentication, only per container</para>
        ///
        /// <para>Check <seealso cref="IBFileServiceInterface.CopyFile"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool CopyFile(string _SourceBucketName, string _SourceKeyInBucket, string _DestinationBucketName, string _DestinationKeyInBucket, EBRemoteFileReadPublicity _RemoteFileReadAccess = EBRemoteFileReadPublicity.AuthenticatedRead, Action<string> _ErrorMessageAction = null)
        {

            try
            {
                BlobContainerClient SourceClient = AServiceClient.GetBlobContainerClient(_SourceBucketName);
                BlobClient SourceBlob = SourceClient.GetBlobClient(_SourceKeyInBucket);

                //Will throw exception on failure
                Response<bool> ExistsResponse = SourceBlob.Exists();

                if (ExistsResponse.Value)
                {
                    BlobContainerClient DestClient = AServiceClient.GetBlobContainerClient(_DestinationBucketName);
                    BlobClient DestBlob = SourceClient.GetBlobClient(_DestinationKeyInBucket);

                    CopyFromUriOperation CopyOp = DestBlob.StartCopyFromUri(SourceBlob.Uri, new BlobCopyFromUriOptions());

                    CopyOp.WaitForCompletionAsync();
                    CopyOp.UpdateStatus();

                    return CopyOp.HasCompleted;
                }
                else
                {
                    _ErrorMessageAction?.Invoke($"BFileServiceAZ -> CopyFile : Source file does not exist");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _ErrorMessageAction?.Invoke($"BFileServiceAZ -> CopyFile : {ex.Message}\n{ex.StackTrace}");
                return false;
            }
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
        public bool CreateSignedURLForDownload(out string _SignedUrl, string _BucketName, string _KeyInBucket, int _URLValidForMinutes = 1, Action<string> _ErrorMessageAction = null)
        {
            try
            {
                BlobContainerClient ContainerClient = AServiceClient.GetBlobContainerClient(_BucketName);
                BlobClient Blob = ContainerClient.GetBlobClient(_KeyInBucket);

                Uri U = Blob.GenerateSasUri(BlobSasPermissions.Read, new DateTimeOffset(DateTime.Now, new TimeSpan(0, _URLValidForMinutes, 0)));
                _SignedUrl = U.ToString();

                return true;
            }
            catch (Exception ex)
            {
                _ErrorMessageAction?.Invoke($"BFileServiceAZ -> CreateSignedURLForDownload : {ex.Message}\n{ex.StackTrace}");
                _SignedUrl = null;

                return false;
            }
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
        public bool CreateSignedURLForUpload(out string _SignedUrl, string _BucketName, string _KeyInBucket, string _ContentType, int _URLValidForMinutes = 60, Action<string> _ErrorMessageAction = null)
        {
            try
            {
                BlobContainerClient ContainerClient = AServiceClient.GetBlobContainerClient(_BucketName);
                BlobClient Blob = ContainerClient.GetBlobClient(_KeyInBucket);
                Blob.SetHttpHeaders(new BlobHttpHeaders() { ContentType = _ContentType });

                Uri U = Blob.GenerateSasUri(BlobSasPermissions.Create, new DateTimeOffset(DateTime.Now, new TimeSpan(0, _URLValidForMinutes, 0)));

                _SignedUrl = U.ToString();

                return true;
            }
            catch (Exception ex)
            {
                _ErrorMessageAction?.Invoke($"BFileServiceAZ -> CreateSignedURLForUpload : {ex.Message}\n{ex.StackTrace}");
                _SignedUrl = null;

                return false;
            }
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
        public bool DeleteFile(string _BucketName, string _KeyInBucket, Action<string> _ErrorMessageAction = null)
        {
            try
            {
                BlobContainerClient ContainerClient = AServiceClient.GetBlobContainerClient(_BucketName);
                ContainerClient.DeleteBlobIfExists(_KeyInBucket);

                return true;
            }
            catch (Exception ex)
            {
                _ErrorMessageAction?.Invoke($"BFileServiceAZ -> DeleteFile : {ex.Message}\n{ex.StackTrace}");

                return false;
            }
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
        public bool DeleteFolder(string _BucketName, string _Folder, Action<string> _ErrorMessageAction = null)
        {
            try
            {
                BlobContainerClient ContainerClient = AServiceClient.GetBlobContainerClient(_BucketName);
                Pageable<BlobHierarchyItem> BlobsResponse = ContainerClient.GetBlobsByHierarchy(BlobTraits.None, BlobStates.None, null, _Folder);

                foreach (BlobHierarchyItem Item in BlobsResponse)
                {
                    if (Item.IsBlob)
                    {
                        ContainerClient.DeleteBlobIfExists(Item.Blob.Name);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _ErrorMessageAction?.Invoke($"BFileServiceAZ -> DeleteFolder : {ex.Message}\n{ex.StackTrace}");
                return false;
            }

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
        public bool DownloadFile(string _BucketName, string _KeyInBucket, BStringOrStream _Destination, Action<string> _ErrorMessageAction = null, ulong _StartIndex = 0, ulong _Size = 0)
        {
            BlobContainerClient ContainerClient = AServiceClient.GetBlobContainerClient(_BucketName);
            BlobClient Blob = ContainerClient.GetBlobClient(_KeyInBucket);
            Response<BlobProperties> Response = Blob.GetProperties();

            if (AServiceClient == null)
            {
                _ErrorMessageAction?.Invoke("BFileServiceAZ->DownloadFile: AServiceClient is null.");
                return false;
            }

            if (!CheckFileExistence(
                _BucketName,
                _KeyInBucket,
                out bool bExists,
                _ErrorMessageAction
                ))
            {
                _ErrorMessageAction?.Invoke("BFileServiceAZ->DownloadFile: CheckFileExistence failed.");
                return false;
            }

            if (!bExists)
            {
                _ErrorMessageAction?.Invoke("BFileServiceAZ->DownloadFile: File does not exist in the File Service.");
                return true;
            }

            HttpRange Range = default(HttpRange);
            if (_Size > 0)
            {
                Range = new HttpRange((long)_StartIndex, (long)(_StartIndex + _Size));
            }

            try
            {
                if (_Destination.Type == EBStringOrStreamEnum.String)
                {
                    using (FileStream FS = File.Create(_Destination.String))
                    {

                        BlobDownloadInfo DlInfo = Blob.Download(Range).Value;
                        DlInfo.Content.CopyTo(FS);

                        DlInfo.Dispose();
                    }

                    if (!BUtility.DoesFileExist(
                        _Destination.String,
                        out bool bLocalFileExists,
                        _ErrorMessageAction))
                    {
                        _ErrorMessageAction?.Invoke("BFileServiceAZ->DownloadFile: DoesFileExist failed.");
                        return false;
                    }

                    if (!bLocalFileExists)
                    {
                        _ErrorMessageAction?.Invoke("BFileServiceAZ->DownloadFile: Download finished, but still file does not locally exist.");
                        return false;
                    }
                }
                else
                {
                    if (_Destination.Stream == null)
                    {
                        _ErrorMessageAction?.Invoke("BFileServiceAZ->DownloadFile: Destination stream is null.");
                        return false;
                    }


                    BlobDownloadInfo DlInfo = Blob.Download(Range).Value;
                    DlInfo.Content.CopyTo(_Destination.Stream);
                    DlInfo.Dispose();

                    try
                    {
                        _Destination.Stream.Position = 0;
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BFileServiceAZ->DownloadFile: " + e.Message + ", Trace: " + e.StackTrace);
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
        public bool GetFileChecksum(string _BucketName, string _KeyInBucket, out string _Checksum, Action<string> _ErrorMessageAction = null)
        {
            try
            {
                BlobContainerClient ContainerClient = AServiceClient.GetBlobContainerClient(_BucketName);
                BlobClient Blob = ContainerClient.GetBlobClient(_KeyInBucket);
                Response<BlobProperties> Response = Blob.GetProperties();

                if (Response.Value != null)
                {
                    _Checksum = Response.Value.ETag.ToString().Trim('"').ToLower();
                    return true;
                }
                else
                {
                    _ErrorMessageAction?.Invoke($"BFileServiceAZ -> GetFileChecksum : Service response was empty");
                    _Checksum = null;
                    return false;
                }
            }
            catch (Exception ex)
            {
                _ErrorMessageAction?.Invoke($"BFileServiceAZ -> GetFileChecksum : {ex.Message}\n{ex.StackTrace}");
                _Checksum = null;
                return false;
            }
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
        public bool GetFileSize(string _BucketName, string _KeyInBucket, out ulong _FileSize, Action<string> _ErrorMessageAction = null)
        {
            try
            {
                BlobContainerClient ContainerClient = AServiceClient.GetBlobContainerClient(_BucketName);
                BlobClient Blob = ContainerClient.GetBlobClient(_KeyInBucket);
                Response<BlobProperties> Response = Blob.GetProperties();

                if (Response.Value != null)
                {
                    _FileSize = (ulong)Response.Value.ContentLength;
                    return true;
                }
                else
                {
                    _ErrorMessageAction?.Invoke($"BFileServiceAZ -> GetFileSize : Service response was empty");
                    _FileSize = 0;
                    return false;
                }
            }
            catch (Exception ex)
            {
                _ErrorMessageAction?.Invoke($"BFileServiceAZ -> GetFileSize : {ex.Message}\n{ex.StackTrace}");
                _FileSize = 0;

                return false;
            }
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
        public bool GetFileTags(string _BucketName, string _KeyInBucket, out List<Tuple<string, string>> _Tags, Action<string> _ErrorMessageAction = null)
        {
            try
            {
                BlobContainerClient ContainerClient = AServiceClient.GetBlobContainerClient(_BucketName);
                BlobClient Blob = ContainerClient.GetBlobClient(_KeyInBucket);

                Response<BlobProperties> TagResult = Blob.GetProperties();

                if (TagResult.Value != null)
                {
                    BlobProperties TagsObj = TagResult.Value;
                    KeyValuePair<string, string>[] TagsArr = TagsObj.Metadata.ToArray();
                    List<Tuple<string, string>> Tags = new List<Tuple<string, string>>(TagsArr.Length);

                    for (int i = 0; i < TagsArr.Length; ++i)
                    {
                        Tags.Add(new Tuple<string, string>(TagsArr[i].Key, TagsArr[i].Value));
                    }

                    _Tags = Tags;
                    return true;
                }
                else
                {
                    _ErrorMessageAction?.Invoke($"BFileServiceAZ -> GetFileTags : Service response was empty");
                    _Tags = null;
                    return false;
                }

            }
            catch (Exception ex)
            {
                _ErrorMessageAction?.Invoke($"BFileServiceAZ -> GetFileTags : {ex.Message}\n{ex.StackTrace}");
                _Tags = null;

                return false;
            }
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
        /// <para>ListAllFilesInBucket:</para>
        ///
        /// <para>Lists keys of all files</para>
        ///
        /// <para>Check <seealso cref="IBFileServiceInterface.ListAllFilesInBucket"/> for detailed documentation</para>
        /// 
        /// </summary>
        public bool ListAllFilesInBucket(string _BucketName, out List<string> _FileKeys, Action<string> _ErrorMessageAction = null)
        {
            try
            {
                List<string> Filenames = new List<string>();
                BlobContainerClient ContainerClient = AServiceClient.GetBlobContainerClient(_BucketName);

                Pageable<BlobItem> Blobs = ContainerClient.GetBlobs();

                foreach (BlobItem Blob in Blobs)
                {
                    Filenames.Add(Blob.Name);
                }

                _FileKeys = Filenames;
                return true;
            }
            catch (Exception ex)
            {
                _ErrorMessageAction?.Invoke($"BFileServiceAZ -> ListAllFilesInBucket : {ex.Message}\n{ex.StackTrace}");
                _FileKeys = null;
                return false;
            }
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
        public bool SetFileTags(string _BucketName, string _KeyInBucket, Tuple<string, string>[] _Tags, Action<string> _ErrorMessageAction = null)
        {
            try
            {
                BlobContainerClient ContainerClient = AServiceClient.GetBlobContainerClient(_BucketName);
                BlobClient Blob = ContainerClient.GetBlobClient(_KeyInBucket);

                Dictionary<string, string> TagDictionary = new Dictionary<string, string>();

                foreach (var Item in _Tags)
                {
                    TagDictionary.Add(Item.Item1, Item.Item1);
                }

                Response<BlobInfo> Resp = Blob.SetMetadata(TagDictionary);
                
                return true;
            }
            catch (Exception ex)
            {
                _ErrorMessageAction?.Invoke($"BFileServiceAZ -> SetFileTags : {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }


        /// <summary>
        ///
        /// <para>UploadFile:</para>
        ///
        /// <para>Uploads a local file to File Service, caller thread will be blocked before it is done</para>
        /// 
        /// <para>EBRemoteFileReadPublicity does nothing as Azure does not support per object authentication, only per container</para>
        ///
        /// <para>Check <seealso cref="IBFileServiceInterface.UploadFile"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool UploadFile(BStringOrStream _LocalFileOrStream, string _BucketName, string _KeyInBucket, EBRemoteFileReadPublicity _RemoteFileReadAccess = EBRemoteFileReadPublicity.AuthenticatedRead, Tuple<string, string>[] _FileTags = null, Action<string> _ErrorMessageAction = null)
        {

            if (AServiceClient == null)
            {
                _ErrorMessageAction?.Invoke("BFileServiceAZ->UploadFile: GSClient is null.");
                return false;
            }

            BlobContainerClient ContainerClient = AServiceClient.GetBlobContainerClient(_BucketName);
            BlobClient Blob = ContainerClient.GetBlobClient(_KeyInBucket);

            Response<BlobContentInfo> Response = null;

            if (_LocalFileOrStream.Type == EBStringOrStreamEnum.String)
            {
                if (!BUtility.DoesFileExist(
                    _LocalFileOrStream.String,
                    out bool bLocalFileExists,
                    _ErrorMessageAction))
                {
                    _ErrorMessageAction?.Invoke("BFileServiceAZ->UploadFile: DoesFileExist failed.");
                    return false;
                }

                if (!bLocalFileExists)
                {
                    _ErrorMessageAction?.Invoke("BFileServiceAZ->UploadFile: Local file does not exist.");
                    return false;
                }

                using (FileStream FS = new FileStream(_LocalFileOrStream.String, FileMode.Open, FileAccess.Read))
                {
                    try
                    {
                        Response = Blob.Upload(FS);
                        
                        if (Response.Value == null)
                        {
                            _ErrorMessageAction?.Invoke("BFileServiceAZ->UploadFile: Operation has failed.");
                            return false;
                        }
                    }
                    catch (Exception e)
                    {
                        _ErrorMessageAction?.Invoke("BFileServiceAZ->UploadFile: " + e.Message + ", Trace: " + e.StackTrace);
                        return false;
                    }
                }
            }
            else
            {
                try
                {
                    Response = Blob.Upload(_LocalFileOrStream.Stream);

                    if (Response.Value == null)
                    {
                        _ErrorMessageAction?.Invoke("BFileServiceAZ->UploadFile: Operation has failed.");
                        return false;
                    }
                }
                catch (Exception e)
                {
                    _ErrorMessageAction?.Invoke("BFileServiceAZ->UploadFile: " + e.Message + ", Trace: " + e.StackTrace);
                    return false;
                }
            }

            if (Response != null)
            {
                if (_FileTags != null && _FileTags.Length > 0)
                {
                    Dictionary<string, string> NewMetadata = new Dictionary<string, string>();
                    foreach (Tuple<string, string> CurrentTag in _FileTags)
                    {
                        NewMetadata.Add(CurrentTag.Item1, CurrentTag.Item2);
                    }

                    try
                    {
                        Blob.SetMetadata(NewMetadata);
                    }
                    catch(Exception ex)
                    {
                        _ErrorMessageAction?.Invoke($"BFileServiceAZ->UploadFile: {ex.Message}, Trace: {ex.StackTrace}");
                        return false;
                    }
                }

                var FileName = _KeyInBucket;
                var FileNameIndex = _KeyInBucket.LastIndexOf("/") + 1;
                if (FileNameIndex > 0)
                {
                    FileName = _KeyInBucket.Substring(FileNameIndex, _KeyInBucket.Length - FileNameIndex);
                }
                BlobHttpHeaders Header = new BlobHttpHeaders()
                {
                    ContentDisposition = $"inline; filename={FileName}"
                };
                try
                {
                    Blob.SetHttpHeaders(Header);
                }
                catch(Exception ex)
                {
                    _ErrorMessageAction?.Invoke($"BFileServiceAZ->UploadFile: {ex.Message}, Trace: {ex.StackTrace}");
                    return false;
                }
            }
            return true;
        }


        /// <summary>
        /// Azure blob storage does not support per object permissions. 
        /// Calling this throws NotSupportedException
        /// </summary>
        public bool SetFileAccessibility(string _BucketName, string _KeyInBucket, EBRemoteFileReadPublicity _RemoteFileReadAccess = EBRemoteFileReadPublicity.AuthenticatedRead, Action<string> _ErrorMessageAction = null)
        {
            // Azure blob storage does not support per object permissions.
            throw new NotSupportedException();
        }

        /// <summary>
        /// All events on blob storage are active by default with a fixed name so you can only subscribe to them. You cannot create a custom topic on storage directly.
        /// If you want a custom topic name you'll have to subscribe to the fixed events and map them to your topic names.
        /// Calling this throws NotSupportedException
        /// </summary>
        public bool CreateFilePubSubNotification(string _BucketName, string _TopicName, string _PathPrefixToListen, List<EBFilePubSubNotificationEventType> _EventsToListen, Action<string> _ErrorMessageAction = null)
        {
            throw new NotSupportedException();
        }
    }
}

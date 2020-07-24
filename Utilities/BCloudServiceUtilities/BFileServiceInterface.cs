/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;
using BCommonUtilities;

namespace BCloudServiceUtilities
{
    /// <summary>
    /// <para>After uploading or copying a file, defines accessibility to the new file location</para>
    /// </summary>
    public enum EBRemoteFileReadPublicity
    {
        AuthenticatedRead,
        ProjectWideProtectedRead,
        PublicRead
    };

    /// <summary>
    /// <para>After uploading or copying a file, defines accessibility to the new file location</para>
    /// </summary>
    public enum EBFilePubSubNotificationEventType
    {
        Uploaded,
        Deleted
    };

    /// <summary>
    /// <para>Interface for abstracting File Services to make it usable with multiple cloud solutions</para>
    /// </summary>
    public interface IBFileServiceInterface
    {
        /// <summary>
        ///
        /// <para>HasInitializationSucceed:</para>
        /// 
        /// <returns>     Returns: Initialization succeed or failed </returns>
        ///
        /// </summary>
        bool HasInitializationSucceed();

        /// <summary>
        ///
        /// <para>UploadFile:</para>
        ///
        /// <para>Uploads a local file to File Service, caller thread will be blocked before it is done</para>
        ///
        /// <para>Parameters:</para>
        /// <para><paramref name="_LocalFileOrStream"/>           Full file path or stream</para>
        /// <para><paramref name="_BucketName"/>                  Key to remote file relative to bucket</para>
        /// <para><paramref name="_KeyInBucket"/>                 Name of the file in the remote location</para>
        /// <para><paramref name="_RemoteFileReadAccess"/>        Defines accessibility to the new file location</para>
        /// <para><paramref name="_FileTags"/>                    File tags, will be discarded if it is null or empty</para>
        /// <para><paramref name="_ErrorMessageAction"/>          Error messages will be pushed to this action</para>
        ///
        /// <returns>                                             Returns: Operation success </returns>
        ///
        /// </summary>
        bool UploadFile(
            BStringOrStream _LocalFileOrStream, 
            string _BucketName,
            string _KeyInBucket,
            EBRemoteFileReadPublicity _RemoteFileReadAccess = EBRemoteFileReadPublicity.AuthenticatedRead,
            Tuple<string, string>[] _FileTags = null, 
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        ///
        /// <para>CreateSignedURLForUpload:</para>
        ///
        /// <para>Creates signed url for uploading a file</para>
        ///
        /// <para>Parameters:</para>
        /// <para><paramref name="_SignedUrl"/>                   Url result</para>
        /// <para><paramref name="_BucketName"/>                  Key to remote file relative to bucket</para>
        /// <para><paramref name="_KeyInBucket"/>                 Key to remote file relative to bucket</para>
        /// <para><paramref name="_ContentType"/>                 Http content type of the file</para>
        /// <para><paramref name="_URLValidForMinutes"/>          For how long url will remain valid</para>
        /// <para><paramref name="_ErrorMessageAction"/>          Error messages will be pushed to this action</para>
        ///
        /// <returns>                                             Returns: Operation success </returns>
        ///
        /// </summary>
        bool CreateSignedURLForUpload(
            out string _SignedUrl,
            string _BucketName,
            string _KeyInBucket,
            string _ContentType,
            int _URLValidForMinutes = 60,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        ///
        /// <para>DownloadFile:</para>
        ///
        /// <para>Downloads a file from File Service and stores locally/or to stream, caller thread will be blocked before it is done</para>
        ///
        /// <para>Parameters:</para>
        /// <para><paramref name="_BucketName"/>                     Name of the Bucket to be used to store file in</para>
        /// <para><paramref name="_KeyInBucket"/>                    Key to remote file relative to bucket</para>
        /// <para><paramref name="_Destination"/>                    Destination full file path or stream</para>
        /// <para><paramref name="_ErrorMessageAction"/>             Error messages will be pushed to this action</para>
        /// <para><paramref name="_StartIndex"/>                     Start Index</para>
        /// <para><paramref name="_Size"/>                           Size</para>
        ///
        /// <returns>                                                Returns: Operation success </returns>
        ///
        /// </summary>
        bool DownloadFile(
            string _BucketName,
            string _KeyInBucket,
            BStringOrStream _Destination,
            Action<string> _ErrorMessageAction = null,
            UInt64 _StartIndex = 0,
            UInt64 _Size = 0);

        /// <summary>
        ///
        /// <para>CreateSignedURLForDownload:</para>
        ///
        /// <para>Creates signed url for downloading a file</para>
        ///
        /// <para>Parameters:</para>
        /// <para><paramref name="_SignedUrl"/>                   Url result</para>
        /// <para><paramref name="_BucketName"/>                  Key to remote file relative to bucket</para>
        /// <para><paramref name="_KeyInBucket"/>                 Key to remote file relative to bucket</para>
        /// <para><paramref name="_URLValidForMinutes"/>          For how long url will remain valid</para>
        /// <para><paramref name="_ErrorMessageAction"/>          Error messages will be pushed to this action</para>
        ///
        /// <returns>                                             Returns: Operation success </returns>
        ///
        /// </summary>
        bool CreateSignedURLForDownload(
            out string _SignedUrl,
            string _BucketName,
            string _KeyInBucket,
            int _URLValidForMinutes = 1,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        ///
        /// <para>GetFileSize:</para>
        ///
        /// <para>Gets size of a file in bytes from File Service, caller thread will be blocked before it is done</para>
        ///
        /// <para>Parameters:</para>
        /// <para><paramref name="_BucketName"/>                 Name of the Bucket to be used to store file in</para>
        /// <para><paramref name="_KeyInBucket"/>                Key to remote file relative to bucket</para>
        /// <para><paramref name="_FileSize"/>                   File size in bytes</para>
        /// <para><paramref name="_ErrorMessageAction"/>         Error messages will be pushed to this action</para>
        ///
        /// <returns>                                            Returns: Operation success </returns>
        ///
        /// </summary>
        bool GetFileSize(
            string _BucketName,
            string _KeyInBucket,
            out ulong _FileSize,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        ///
        /// <para>GetFileChecksum:</para>
        ///
        /// <para>Gets MD5 checksum of a file from File Service, caller thread will be blocked before it is done</para>
        ///
        /// <para>Parameters:</para>
        /// <para><paramref name="_BucketName"/>                  Name of the Bucket to be used to store file in</para>
        /// <para><paramref name="_KeyInBucket"/>                 Key to remote file relative to bucket</para>
        /// <para><paramref name="_Checksum"/>                    Checksum</para>
        /// <para><paramref name="_ErrorMessageAction"/>          Error messages will be pushed to this action</para>
        ///
        /// <returns>                                             Returns: Operation success </returns>
        ///
        /// </summary>
        bool GetFileChecksum(
            string _BucketName, 
            string _KeyInBucket, 
            out string _Checksum,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        ///
        /// <para>GetFileTags:</para>
        ///
        /// <para>Gets the tags of the file from the file service</para>
        ///
        /// <para>Parameters:</para>
        /// <para><paramref name="_BucketName"/>                  Name of the Bucket to be used to store file in</para>
        /// <para><paramref name="_KeyInBucket"/>                 Key to remote file relative to bucket</para>
        /// <para><paramref name="_Tags"/>                        File tags</para>
        /// <para><paramref name="_ErrorMessageAction"/>          Error messages will be pushed to this action</para>
        ///
        /// <returns>                                             Returns: Operation success </returns>
        ///
        /// </summary>
        bool GetFileTags(
            string _BucketName,
            string _KeyInBucket,
            out System.Collections.Generic.List<Tuple<string, string>> _Tags,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        ///
        /// <para>SetFileTags:</para>
        ///
        /// <para>Sets the tags of the file in the file service, existing tags for the file in the cloud will be deleted</para>
        ///
        /// <para>Parameters:</para>
        /// <para><paramref name="_BucketName"/>          Name of the Bucket to be used to store file in</para>
        /// <para><paramref name="_KeyInBucket"/>         Key to remote file relative to bucket</para>
        /// <para><paramref name="_Tags"/>                File tags</para>
        /// <para><paramref name="_ErrorMessageAction"/>  Error messages will be pushed to this action</para>
        ///
        /// <returns>                                     Returns: Operation success </returns>
        ///
        /// </summary>
        bool SetFileTags(
            string _BucketName,
            string _KeyInBucket,
            Tuple<string, string>[] _Tags,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        ///
        /// <para>CheckFileExistence:</para>
        ///
        /// <para>Checks existence of an object in File Service, caller thread will be blocked before it is done</para>
        ///
        /// <para>Parameters:</para>
        /// <para><paramref name="_BucketName"/>                  Name of the Bucket to be used to store file in</para>
        /// <para><paramref name="_KeyInBucket"/>                 Key to remote file relative to bucket</para>
        /// <para><paramref name="_bExists"/>                     Existence of the object</para>
        /// <para><paramref name="_ErrorMessageAction"/>          Error messages will be pushed to this action</para>
        ///
        /// <returns>                                             Returns: Operation success </returns>
        ///
        /// </summary>
        bool CheckFileExistence(
            string _BucketName,
            string _KeyInBucket,
            out bool _bExists,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        ///
        /// <para>DeleteFile:</para>
        ///
        /// <para>Deletes a file from File Service, caller thread will be blocked before it is done</para>
        ///
        /// <para>Parameters:</para>
        /// <para><paramref name="_BucketName"/>                  Name of the Bucket</para>
        /// <para><paramref name="_KeyInBucket"/>                 Key to remote file relative to bucket</para>
        /// <para><paramref name="_ErrorMessageAction"/>          Error messages will be pushed to this action</para>
        ///
        /// <returns>                                             Returns: Operation success </returns>
        ///
        /// </summary>
        bool DeleteFile(
            string _BucketName,
            string _KeyInBucket,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        ///
        /// <para>CopyFile:</para>
        ///
        /// <para>Copy a file from a bucket and relative location to another in File Service, caller thread will be blocked before it is done</para>
        ///
        /// <para>Parameters:</para>
        /// <para><paramref name="_SourceBucketName"/>            Name of the Source Bucket</para>
        /// <para><paramref name="_SourceKeyInBucket"/>           Key to remote file relative to source bucket</para>
        /// <para><paramref name="_DestinationBucketName"/>       Name of the Destination Bucket</para>
        /// <para><paramref name="_DestinationKeyInBucket"/>      Key to remote file relative to destination bucket</para>
        /// <para><paramref name="_RemoteFileReadAccess"/>        Defines accessibility to the new file location</para>
        /// <para><paramref name="_ErrorMessageAction"/>          Error messages will be pushed to this action</para>
        ///
        /// <returns>                                             Returns: Operation success </returns>
        ///
        /// </summary>
        bool CopyFile(
            string _SourceBucketName,
            string _SourceKeyInBucket,
            string _DestinationBucketName,
            string _DestinationKeyInBucket,
            EBRemoteFileReadPublicity _RemoteFileReadAccess = EBRemoteFileReadPublicity.AuthenticatedRead,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        ///
        /// <para>SetFileAccessibility:</para>
        ///
        /// <para>Changes accessibility of a file in the File Service</para>
        ///
        /// <para>Parameters:</para>
        /// <para><paramref name="_BucketName"/>                  Name of the Bucket</para>
        /// <para><paramref name="_KeyInBucket"/>                 Key to remote file relative to bucket</para>
        /// <para><paramref name="_RemoteFileReadAccess"/>        Defines accessibility to the new file location</para>
        /// <para><paramref name="_ErrorMessageAction"/>          Error messages will be pushed to this action</para>
        ///
        /// <returns>                                             Returns: Operation success </returns>
        ///
        /// </summary>
        bool SetFileAccessibility(
            string _BucketName,
            string _KeyInBucket,
            EBRemoteFileReadPublicity _RemoteFileReadAccess = EBRemoteFileReadPublicity.AuthenticatedRead,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        ///
        /// <para>ListAllFilesInBucket:</para>
        ///
        /// <para>Lists keys of all files</para>
        ///
        /// <para>Parameters:</para>
        /// <para><paramref name="_BucketName"/>                  Name of the Bucket</para>
        /// <para><paramref name="_FileKeys"/>                    All file keys in the given bucket</para>
        /// <para><paramref name="_ErrorMessageAction"/>          Error messages will be pushed to this action</para>
        ///
        /// <returns>                                             Returns: Operation success </returns>
        ///
        /// </summary>
        bool ListAllFilesInBucket(
            string _BucketName,
            out System.Collections.Generic.List<string> _FileKeys,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        ///
        /// <para>CreateFilePubSubNotification:</para>
        ///
        /// <para>Creates file based pub/sub notification</para>
        ///
        /// <para>Parameters:</para>
        /// <para><paramref name="_BucketName"/>                  Name of the Bucket</para>
        /// <para><paramref name="_TopicName"/>                   Topic name to be pushed</para>
        /// <para><paramref name="_PathPrefixToListen"/>          Path prefix to listen</para>
        /// <para><paramref name="_EventsToListen"/>              Events to listen</para>
        /// <para><paramref name="_ErrorMessageAction"/>          Error messages will be pushed to this action</para>
        ///
        /// <returns>                                             Returns: Operation success </returns>
        ///
        /// </summary>
        bool CreateFilePubSubNotification(
            string _BucketName,
            string _TopicName,
            string _PathPrefixToListen,
            List<EBFilePubSubNotificationEventType> _EventsToListen,
            Action<string> _ErrorMessageAction = null);
    }
}
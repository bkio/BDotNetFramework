/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;

namespace BCloudServiceUtilities
{
    public struct BMailServicerReceiverStruct
    {
        public readonly string ReceiverEmail;
        public readonly string ReceiverName;

        public BMailServicerReceiverStruct(
            string _ReceiverEmail,
            string _ReceiverName)
        {
            ReceiverEmail = _ReceiverEmail;
            ReceiverName = _ReceiverName;
        }
    }

    public struct BMailServiceMailStruct
    {
        public readonly BMailServicerReceiverStruct Receiver;
        public readonly string Subject;
        public readonly string PlainText;
        public readonly string HtmlText;

        public BMailServiceMailStruct(
            BMailServicerReceiverStruct _Receiver,
            string _Subject,
            string _PlainText,
            string _HtmlText)
        {
            Receiver = _Receiver;
            Subject = _Subject;
            PlainText = _PlainText;
            HtmlText = _HtmlText;
        }
    }

    public interface IBMailServiceInterface
    {
        /// <summary>
        /// 
        /// <para>HasInitializationSucceed:</para>
        /// 
        /// <returns>Returns: Initialization succeed or failed</returns>
        /// 
        /// </summary>
        bool HasInitializationSucceed();

        /// <summary>
        /// 
        /// <para>SendEmail</para>
        /// 
        /// <para>Sends e-mails to given addresses with given bodies</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Messages"/>                  List of messages to be sent</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                  Operation success</returns>
        /// 
        /// </summary>
        bool SendEmails(
            List<BMailServiceMailStruct> _Messages,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>BroadcastEmail</para>
        /// 
        /// <para>Sends an e-mail to given addresses with given body</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Receivers"/>                 List of receivers</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                  Operation success</returns>
        /// 
        /// </summary>
        bool BroadcastEmail(
            List<BMailServicerReceiverStruct> _Receivers,
            string _Subject,
            string _PlainText,
            string _HtmlText,
            Action<string> _ErrorMessageAction = null);
    }
}

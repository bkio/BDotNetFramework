/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Net;

namespace BCloudServiceUtilities
{
    public interface IBTracingServiceInterface
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
        /// <para>On_FromClientToGateway_Received</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Context"/>                       Http context</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// </summary>
        void On_FromClientToGateway_Received(
            HttpListenerContext _Context,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>On_FromServiceToService_Sent</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Context"/>                       Http context</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// </summary>
        void On_FromServiceToService_Sent(
            HttpListenerContext _Context,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>On_FromGatewayToClient_Sent</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Context"/>                       Http context</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// </summary>
        void On_FromGatewayToClient_Sent(
            HttpListenerContext _Context,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>On_FromServiceToService_Received</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Context"/>                       Http context</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// </summary>
        void On_FromServiceToService_Received(
            HttpListenerContext _Context,
            Action<string> _ErrorMessageAction = null);
    }
}
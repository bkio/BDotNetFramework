/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Net;
using BCommonUtilities;

namespace BWebServiceUtilities
{
    public class BMovedPermanently : BWebServiceBase
    {
        private readonly string ServerName;
        private readonly string RedirectToHTMLEndpoint;

        public BMovedPermanently(string _ServerName, string _RedirectToHTMLEndpoint)
        {
            ServerName = _ServerName;
            RedirectToHTMLEndpoint = _RedirectToHTMLEndpoint;
        }

        public override BWebServiceResponse OnRequest(HttpListenerContext Context, Action<string> _ErrorMessageAction = null)
        {
            string NewLocation = ServerName + RedirectToHTMLEndpoint;

            return new BWebServiceResponse(
                BWebResponse.From_Internal_To_Gateway_Moved_Permanently_Code,
                new BStringOrStream(BWebResponse.From_Internal_To_Gateway_Moved_Permanently_String(NewLocation)),
                BWebResponse.From_Internal_To_Gateway_Moved_Permanently_ContentType);
        }
    }
}
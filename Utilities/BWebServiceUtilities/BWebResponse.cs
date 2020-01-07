/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using BCommonUtilities;

namespace BWebServiceUtilities
{
    public static class BWebResponse
    {
        public static EBResponseContentType GetResponseContentTypeFromFailCode(int _Code)
        {
            switch (_Code)
            {
                case 501:
                    return Error_BadRequest_ContentType;
                case 500:
                    return Error_InternalError_ContentType;
                case 405:
                    return Error_MethodNotAllowed_ContentType;
                case 404:
                    return Error_NotFound_ContentType;
                case 403:
                    return Error_Forbidden_ContentType;
                case 401:
                    return Error_Unauthorized_ContentType;
                case 400:
                    return Error_BadRequest_ContentType;
                default:
                    return EBResponseContentType.None;
            }
        }
        public static string GetErrorStringWithFailCode(string _Message, int _Code)
        {
            switch (_Code)
            {
                case 501:
                    return Error_NotImplemented_String(_Message);
                case 500:
                    return Error_InternalError_String(_Message);
                case 405:
                    return Error_MethodNotAllowed_String(_Message);
                case 404:
                    return Error_NotFound_String(_Message);
                case 403:
                    return Error_Forbidden_String(_Message);
                case 401:
                    return Error_Unauthorized_String(_Message);
                case 400:
                    return Error_BadRequest_String(_Message);
                default:
                    return null;
            }
        }

        public static readonly EBResponseContentType Error_NotImplemented_ContentType = EBResponseContentType.JSON;
        public static string Error_NotImplemented_String(string Message) { return "{\"result\":\"failure\",\"message\":\"Not Implemented. " + Message + "\"}"; }
        public static readonly int Error_NotImplemented_Code = 501;
        public static BWebServiceResponse NotImplemented(string Message) { return new BWebServiceResponse(Error_NotImplemented_Code, new BStringOrStream(Error_NotImplemented_String(Message)), Error_NotImplemented_ContentType); }

        //

        public static readonly EBResponseContentType Error_InternalError_ContentType = EBResponseContentType.JSON;
        public static string Error_InternalError_String(string Message) { return "{\"result\":\"failure\",\"message\":\"Internal Server Error. " + Message + "\"}"; }
        public static readonly int Error_InternalError_Code = 500;
        public static BWebServiceResponse InternalError(string Message) { return new BWebServiceResponse(Error_InternalError_Code, new BStringOrStream(Error_InternalError_String(Message)), Error_InternalError_ContentType); }

        //

        public static readonly EBResponseContentType Error_MethodNotAllowed_ContentType = EBResponseContentType.JSON;
        public static string Error_MethodNotAllowed_String(string Message) { return "{\"result\":\"failure\",\"message\":\"Method Not Allowed. " + Message + "\"}"; }
        public static readonly int Error_MethodNotAllowed_Code = 405;
        public static BWebServiceResponse MethodNotAllowed(string Message) { return new BWebServiceResponse(Error_MethodNotAllowed_Code, new BStringOrStream(Error_MethodNotAllowed_String(Message)), Error_MethodNotAllowed_ContentType); }

        //

        public static readonly EBResponseContentType Error_NotFound_ContentType = EBResponseContentType.JSON;
        public static string Error_NotFound_String(string Message) { return "{\"result\":\"failure\",\"message\":\"Not Found. " + Message + "\"}"; }
        public static readonly int Error_NotFound_Code = 404;
        public static BWebServiceResponse NotFound(string Message) { return new BWebServiceResponse(Error_NotFound_Code, new BStringOrStream(Error_NotFound_String(Message)), Error_NotFound_ContentType); }

        //

        public static readonly EBResponseContentType Error_Forbidden_ContentType = EBResponseContentType.JSON;
        public static string Error_Forbidden_String(string Message) { return "{\"result\":\"failure\",\"message\":\"Forbidden. " + Message + "\"}"; }
        public static readonly int Error_Forbidden_Code = 403;
        public static BWebServiceResponse Forbidden(string Message) { return new BWebServiceResponse(Error_Forbidden_Code, new BStringOrStream(Error_Forbidden_String(Message)), Error_Forbidden_ContentType); }

        //

        public static readonly EBResponseContentType Error_Unauthorized_ContentType = EBResponseContentType.JSON;
        public static string Error_Unauthorized_String(string Message, string RedirectTo = null) { return "{\"result\":\"failure\",\"message\":\"Unauthorized. " + Message + "\"" + (RedirectTo != null ? (",\"RedirectTo\":\"" + RedirectTo + "\"") : "") + "}"; }
        public static readonly int Error_Unauthorized_Code = 401;
        public static BWebServiceResponse Unauthorized(string Message) { return new BWebServiceResponse(Error_Unauthorized_Code, new BStringOrStream(Error_Unauthorized_String(Message)), Error_Unauthorized_ContentType); }

        //

        public static readonly EBResponseContentType Error_BadRequest_ContentType = EBResponseContentType.JSON;
        public static string Error_BadRequest_String(string Message) { return "{\"result\":\"failure\",\"message\":\"Bad Request. " + Message + "\"}"; }
        public static readonly int Error_BadRequest_Code = 400;
        public static BWebServiceResponse BadRequest(string Message) { return new BWebServiceResponse(Error_BadRequest_Code, new BStringOrStream(Error_BadRequest_String(Message)), Error_BadRequest_ContentType); }

        //

        public static readonly EBResponseContentType Moved_Permanently_ContentType = EBResponseContentType.JSON;
        public static string Moved_Permanently_String(string Message = "") { return "{\"result\":\"failure\",\"message\":\"Moved Permanently. " + Message + "\"}"; }
        public static readonly int Moved_Permanently_Code = 301;
        public static BWebServiceResponse MovedPermanently(string Message) { return new BWebServiceResponse(Moved_Permanently_Code, new BStringOrStream(Moved_Permanently_String(Message)), Moved_Permanently_ContentType); }

        //

        public static readonly EBResponseContentType From_Internal_To_Gateway_Moved_Permanently_ContentType = EBResponseContentType.JSON;
        public static string From_Internal_To_Gateway_Moved_Permanently_String(string To) { return "{\"redirectTo\":\"" + To + "\"}"; }
        public static readonly int From_Internal_To_Gateway_Moved_Permanently_Code = 418;

        //

        public static readonly int Status_OK_Code = 200;
    }
}
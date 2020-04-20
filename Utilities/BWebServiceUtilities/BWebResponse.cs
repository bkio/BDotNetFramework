/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using BCommonUtilities;
using Newtonsoft.Json.Linq;

namespace BWebServiceUtilities
{
    public static class BWebResponse
    {
        public static EBResponseContentType GetResponseContentTypeFromFailCode(int _Code)
        {
            switch (_Code)
            {
                case 501:
                    return Error_NotImplemented_ContentType;
                case 500:
                    return Error_InternalError_ContentType;
                case 415:
                    return Error_UnsupportedMediaType_ContentType;
                case 409:
                    return Error_Conflict_ContentType;
                case 406:
                    return Error_NotAcceptable_ContentType;
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
                case 415:
                    return Error_UnsupportedMediaType_String(_Message);
                case 409:
                    return Error_Conflict_String(_Message);
                case 406:
                    return Error_NotAcceptable_String(_Message);
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
        public static string Error_NotImplemented_String(string _Message) { return "{\"result\":\"failure\",\"message\":\"Not Implemented. " + _Message + "\"}"; }
        public static readonly int Error_NotImplemented_Code = 501;
        public static BWebServiceResponse NotImplemented(string _Message) { return new BWebServiceResponse(Error_NotImplemented_Code, new BStringOrStream(Error_NotImplemented_String(_Message)), Error_NotImplemented_ContentType); }

        //

        public static readonly EBResponseContentType Error_InternalError_ContentType = EBResponseContentType.JSON;
        public static string Error_InternalError_String(string _Message) { return "{\"result\":\"failure\",\"message\":\"Internal Server Error. " + _Message + "\"}"; }
        public static readonly int Error_InternalError_Code = 500;
        public static BWebServiceResponse InternalError(string _Message) { return new BWebServiceResponse(Error_InternalError_Code, new BStringOrStream(Error_InternalError_String(_Message)), Error_InternalError_ContentType); }

        //

        public static readonly EBResponseContentType Error_UnsupportedMediaType_ContentType = EBResponseContentType.JSON;
        public static string Error_UnsupportedMediaType_String(string _Message) { return "{\"result\":\"failure\",\"message\":\"Unsupported media type. " + _Message + "\"}"; }
        public static readonly int Error_UnsupportedMediaType_Code = 415;
        public static BWebServiceResponse UnsupportedMediaType(string _Message) { return new BWebServiceResponse(Error_UnsupportedMediaType_Code, new BStringOrStream(Error_UnsupportedMediaType_String(_Message)), Error_UnsupportedMediaType_ContentType); }

        //

        public static readonly EBResponseContentType Error_Conflict_ContentType = EBResponseContentType.JSON;
        public static string Error_Conflict_String(string _Message) { return "{\"result\":\"failure\",\"message\":\"Conflict. " + _Message + "\"}"; }
        public static readonly int Error_Conflict_Code = 409;
        public static BWebServiceResponse Conflict(string _Message) { return new BWebServiceResponse(Error_Conflict_Code, new BStringOrStream(Error_Conflict_String(_Message)), Error_Conflict_ContentType); }

        //

        public static readonly EBResponseContentType Error_NotAcceptable_ContentType = EBResponseContentType.JSON;
        public static string Error_NotAcceptable_String(string _Message) { return "{\"result\":\"failure\",\"message\":\"Not acceptable. " + _Message + "\"}"; }
        public static readonly int Error_NotAcceptable_Code = 406;
        public static BWebServiceResponse NotAcceptable(string _Message) { return new BWebServiceResponse(Error_NotAcceptable_Code, new BStringOrStream(Error_NotAcceptable_String(_Message)), Error_NotAcceptable_ContentType); }

        //

        public static readonly EBResponseContentType Error_MethodNotAllowed_ContentType = EBResponseContentType.JSON;
        public static string Error_MethodNotAllowed_String(string _Message) { return "{\"result\":\"failure\",\"message\":\"Method Not Allowed. " + _Message + "\"}"; }
        public static readonly int Error_MethodNotAllowed_Code = 405;
        public static BWebServiceResponse MethodNotAllowed(string _Message) { return new BWebServiceResponse(Error_MethodNotAllowed_Code, new BStringOrStream(Error_MethodNotAllowed_String(_Message)), Error_MethodNotAllowed_ContentType); }

        //

        public static readonly EBResponseContentType Error_NotFound_ContentType = EBResponseContentType.JSON;
        public static string Error_NotFound_String(string _Message) { return "{\"result\":\"failure\",\"message\":\"Not Found. " + _Message + "\"}"; }
        public static readonly int Error_NotFound_Code = 404;
        public static BWebServiceResponse NotFound(string _Message) { return new BWebServiceResponse(Error_NotFound_Code, new BStringOrStream(Error_NotFound_String(_Message)), Error_NotFound_ContentType); }

        //

        public static readonly EBResponseContentType Error_Forbidden_ContentType = EBResponseContentType.JSON;
        public static string Error_Forbidden_String(string _Message) { return "{\"result\":\"failure\",\"message\":\"Forbidden. " + _Message + "\"}"; }
        public static readonly int Error_Forbidden_Code = 403;
        public static BWebServiceResponse Forbidden(string _Message) { return new BWebServiceResponse(Error_Forbidden_Code, new BStringOrStream(Error_Forbidden_String(_Message)), Error_Forbidden_ContentType); }

        //

        public static readonly EBResponseContentType Error_Unauthorized_ContentType = EBResponseContentType.JSON;
        public static string Error_Unauthorized_String(string _Message) { return "{\"result\":\"failure\",\"message\":\"Unauthorized. " + _Message + "\"}"; }
        public static readonly int Error_Unauthorized_Code = 401;
        public static BWebServiceResponse Unauthorized(string _Message) { return new BWebServiceResponse(Error_Unauthorized_Code, new BStringOrStream(Error_Unauthorized_String(_Message)), Error_Unauthorized_ContentType); }

        //

        public static readonly EBResponseContentType Error_BadRequest_ContentType = EBResponseContentType.JSON;
        public static string Error_BadRequest_String(string _Message) { return "{\"result\":\"failure\",\"message\":\"Bad Request. " + _Message + "\"}"; }
        public static readonly int Error_BadRequest_Code = 400;
        public static BWebServiceResponse BadRequest(string _Message) { return new BWebServiceResponse(Error_BadRequest_Code, new BStringOrStream(Error_BadRequest_String(_Message)), Error_BadRequest_ContentType); }

        //
        public static readonly int Status_OK_Code = 200;
        public static BWebServiceResponse StatusOK(string _Message, JObject _AdditionalFields = null) { return new BWebServiceResponse(Status_OK_Code, new BStringOrStream(Status_Success_String(_Message, _AdditionalFields)), Status_Success_ContentType); }

        //

        public static readonly int Status_Created_Code = 201;
        public static BWebServiceResponse StatusCreated(string _Message, JObject _AdditionalFields = null) { return new BWebServiceResponse(Status_Created_Code, new BStringOrStream(Status_Success_String(_Message, _AdditionalFields)), Status_Success_ContentType); }

        //

        public static readonly int Status_Accepted_Code = 202;
        public static BWebServiceResponse StatusAccepted(string _Message, JObject _AdditionalFields = null) { return new BWebServiceResponse(Status_Accepted_Code, new BStringOrStream(Status_Success_String(_Message, _AdditionalFields)), Status_Success_ContentType); }

        // Success common

        public static readonly EBResponseContentType Status_Success_ContentType = EBResponseContentType.JSON;
        public static string Status_Success_String(string _Message, JObject _AdditionalFields = null)
        {
            return Status_Success_JObject(_Message, _AdditionalFields).ToString();
        }
        public static JObject Status_Success_JObject(string _Message, JObject _AdditionalFields = null)
        {
            return new JObject()
            {
                ["result"] = "success",
                ["message"] = _Message
            }.MergeJObjects(_AdditionalFields);
        }
        private static JObject MergeJObjects(this JObject _Input_1, JObject _Input_2)
        {
            if (_Input_2 != null)
            {
                _Input_1.Merge(_Input_2);
            }
            return _Input_1;
        }
    }
}
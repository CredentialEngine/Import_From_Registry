using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;

using workIT.Utilities;

namespace CredentialFinderWeb.Controllers 
{
    public class RegistrySearchController : ApiController
    {
        string thisClassName = "RegistrySearchController";
        [HttpPost, Route( "credential/format" )]
        public ApiResponse Search( SearchRequest request )
        {
            bool isValid = true;
            List<string> messages = new List<string>();
            var response = new ApiResponse();
            string statusMessage = "";
            RequestHelper helper = new RequestHelper();

            try
            {
                if ( request == null )
                {
                    response.Messages.Add( "Error - please provide a valid search request." );
                    return response;
                }

                helper.OwnerCtid = request.OrganizationIdentifier;
                if ( !ValidateRequest( helper, ref statusMessage ) )
                {
                    response.Messages.Add( statusMessage );
                } else
                {

                    //do the search request

                    //response.Payload = helper.Payload;
                    response.Successful = isValid;

                    if ( isValid )
                    {
                        if ( helper.Messages.Count > 0 )
                            response.Messages = helper.GetAllMessages();


                    }
                    else
                    {
                        //if not valid, could return the payload as reference?
                        //response.Messages = messages;
                        response.Messages = helper.GetAllMessages();
                    }
                }
            } catch (Exception ex)
            {
                response.Messages.Add( ex.Message );
                response.Successful = false;
            }

            return response;
        }

        //TBD


        /// <summary>
        /// The actual validation will be via a call to the accounts api
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="statusMessage"></param>
        /// <returns></returns>
        public static bool ValidateRequest( RequestHelper helper, ref string statusMessage, bool isDeleteRequest = false )
        {
            bool isValid = true;
            string clientIdentifier = "";
            bool isTokenRequired = UtilityManager.GetAppKeyValue( "requiringHeaderToken", true );
            if ( isDeleteRequest )
                isTokenRequired = true;

            //api key will be passed in the header
            string apiToken = "";
            if ( IsAuthTokenValid( isTokenRequired, ref apiToken, ref clientIdentifier, ref statusMessage ) == false )
            {
                return false;
            }
            helper.ApiKey = apiToken;
            helper.ClientIdentifier = clientIdentifier ?? "";

            if ( isTokenRequired &&
                ( string.IsNullOrWhiteSpace( helper.OwnerCtid ) ||
                 !helper.OwnerCtid.ToLower().StartsWith( "ce-" ) ||
                 helper.OwnerCtid.Length != 39 )
                )
            {
                if ( clientIdentifier.ToLower().StartsWith( "cerpublisher" ) == false )
                {
                    statusMessage = "Error - a valid CTID for the related organization must be provided.";
                    return false;
                }
            }
            return isValid;
        }

        public static bool IsAuthTokenValid( bool isTokenRequired, ref string token, ref string clientIdentifier, ref string message )
        {
            bool isValid = true;
            //need to handle both ways. So if a token, and ctid are provided, then use them!
            //bool isTokenRequired = UtilityManager.GetAppKeyValue( "requiringHeaderToken", true );

            try
            {
                HttpContext httpContext = HttpContext.Current;
                clientIdentifier = httpContext.Request.Headers["Proxy-Authorization"];
                string authHeader = httpContext.Request.Headers["Authorization"];
                //registry API uses ApiToken rather than Basic
                if ( !string.IsNullOrWhiteSpace( authHeader ) )
                {
                    LoggingHelper.DoTrace( 4, "$$$$$$$$ Found an authorization header." + authHeader );
                    if ( authHeader.ToLower().StartsWith( "apitoken" ) )
                    {
                        //Extract credentials
                        authHeader = authHeader.ToLower();
                        token = authHeader.Substring( "apitoken ".Length ).Trim();
                    }
                }
            }
            catch ( Exception ex )
            {
                if ( isTokenRequired )
                {
                    LoggingHelper.LogError( ex, "Exception encountered attempting to get API key from request header. " );
                    isValid = false;
                }
            }

            if ( isTokenRequired && string.IsNullOrWhiteSpace( token ) )
            {
                if ( !string.IsNullOrWhiteSpace( clientIdentifier ) )
                {
                    if ( clientIdentifier.ToLower().StartsWith( "cerpublisher" ) )
                        return true;
                }
                message = "Error a valid API key must be provided in the header";
                isValid = false;
            }

            return isValid;
        }
    }
    public class SearchRequest
    {
        public string OrganizationIdentifier { get; set; }

        public string SearchFilter { get; set; }
    }
    public class RequestHelper
    {
        public RequestHelper()
        {
            Messages = new List<RequestMessage>();
            HasErrors = false;
        }
        public string ApiKey { get; set; } = "";
        public string OwnerCtid { get; set; } = "";
        public string ClientIdentifier { get; set; } = "";

        public string SerializedInput { get; set; }

        public List<RequestMessage> Messages { get; set; }
        public bool HasErrors { get; set; }
        public void AddError( string message )
        {
            Messages.Add( new RequestMessage() { Message = message } );
            HasErrors = true;
        }
        public void AddWarning( string message )
        {
            Messages.Add( new RequestMessage() { Message = message, IsWarning = true } );
        }

        public List<string> GetAllMessages()
        {
            List<string> messages = new List<string>();
            string prefix = "";
            foreach ( RequestMessage msg in Messages.OrderBy( m => m.IsWarning ) )
            {
                if ( msg.IsWarning )
                    prefix = "Warning - ";
                else
                    prefix = "Error - ";
                messages.Add( prefix + msg.Message );
            }


            return messages;
        }

        public void SetMessages( List<string> messages )
        {
            //just treat all as errors for now
            string prefix = "";
            foreach ( string msg in messages )
            {
                AddError( msg );
            }

        }
    }

    public class RequestMessage
    {
        public string Message { get; set; }
        public bool IsWarning { get; set; }
    }
    public class ApiResponse
    {
        public ApiResponse()
        {
            Messages = new List<string>();
            Payload = "";
        }
        public bool Successful { get; set; }

        public List<string> Messages { get; set; }

        public string CTID { get; set; }

        /// <summary>
        /// Identifier for the registry envelope that contains the document just add/updated
        /// </summary>
        public string RegistryEnvelopeIdentifier { get; set; }

        /// <summary>
        /// Payload of request to registry, containing properties formatted as CTDL - JSON-LD
        /// </summary>
        public string Payload { get; set; }
    }
}

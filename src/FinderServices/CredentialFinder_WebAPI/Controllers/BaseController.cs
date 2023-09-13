using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using CredentialFinderWebAPI.Models;

using workIT.Utilities;

namespace CredentialFinderWebAPI.Controllers
{
    public class BaseController : ApiController
    {
		public void SendResponse( string message, string contentType = "application/json" )
		{
			SendResponseWithHeaders( message, contentType );
		}
		public void SendDebuggingResponse( ApiResponse response, JObject debugInfo )
		{
			var data = JObject.FromObject( response );
			data.Add( "DebugInfo", debugInfo );
			SendResponse( data );
		}
		public void SendResponse( JObject message )
		{
			SendResponseWithHeaders( message.ToString( Formatting.None ) );
		}
		public void SendResponse( List<string> messages )
		{
			string message = JsonConvert.SerializeObject( messages, JsonHelper.GetJsonSettings() );
			SendResponseWithHeaders( message );
		}
		public void SendResponse( ApiResponse response )
		{
			string message = JsonConvert.SerializeObject( response, JsonHelper.GetJsonSettingsAll() );
			SendResponseWithHeaders( message );
		}
        public void SendResponse( PathwayApiResponse response )
        {
            string message = JsonConvert.SerializeObject( response);
            SendResponseWithHeaders( message );
        }

        public void BuildAndSendAPIResponse( object resultData, bool successful = true, List<string> messages = null )
		{
			var response = new ApiResponse()
			{
				Result = resultData,
				Successful = successful,
				Messages = messages,
			};

			SendResponse( response );
		}

		public void SendResponseWithHeaders( object response, string contentType = "application/json" )
		{
			try { 
			HttpContext.Current.Response.Clear();
			HttpContext.Current.Response.BufferOutput = true;
			HttpContext.Current.Response.AppendHeader( "Access-Control-Allow-Origin", "*" );
			HttpContext.Current.Response.ContentType = contentType;
			HttpContext.Current.Response.ContentEncoding = contentType == "application/json" ? Encoding.UTF8 : HttpContext.Current.Response.ContentEncoding;
			HttpContext.Current.Response.Write( response );
			//21-04-01 skipping Response.End to see if fixes
			//			-Server cannot set status after HTTP headers have been sent.
			//			DID NOT WORK, SO ADDED BACK
			HttpContext.Current.Response.End();
			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, "API-BaseController.SendResponseWithHeaders()." + ex.Message );
				

			}
		}
        //public System.Web.Mvc.JsonResult JsonResponse( object data, bool valid, string status, object extra )
        //{
        //	return new System.Web.Mvc.JsonResult() { Data = new { data = data, valid = valid, status = status, extra = extra }, JsonRequestBehavior = System.Web.Mvc.JsonRequestBehavior.AllowGet, MaxJsonLength = int.MaxValue };
        //}


    }
}

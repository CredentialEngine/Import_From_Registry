using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using CredentialFinderWebAPI.Models;

using workIT.Utilities;

namespace CredentialFinderWebAPI.Controllers
{
    public class BaseController : ApiController
    {
		public void SendResponse( string message )
		{
			HttpContext.Current.Response.Clear();
			HttpContext.Current.Response.AppendHeader( "Access-Control-Allow-Origin", "*" );
			HttpContext.Current.Response.ContentType = "application/json";
			HttpContext.Current.Response.Write( message );
			HttpContext.Current.Response.End();
		}

		public void SendResponse( JObject message )
		{
			HttpContext.Current.Response.Clear();
			HttpContext.Current.Response.AppendHeader( "Access-Control-Allow-Origin", "*" );
			HttpContext.Current.Response.ContentType = "application/json";
			HttpContext.Current.Response.Write( message );
			HttpContext.Current.Response.End();
		}
		public void SendResponse( List<string> messages )
		{
			string message = JsonConvert.SerializeObject( messages, JsonHelper.GetJsonSettings() );

			HttpContext.Current.Response.Clear();
			HttpContext.Current.Response.AppendHeader( "Access-Control-Allow-Origin", "*" );
			HttpContext.Current.Response.ContentType = "application/json";
			HttpContext.Current.Response.Write( message );
			HttpContext.Current.Response.End();
		}
		public void SendResponse( ApiResponse response )
		{
			string message = JsonConvert.SerializeObject( response, JsonHelper.GetJsonSettingsAll() );

			HttpContext.Current.Response.Clear();
			HttpContext.Current.Response.AppendHeader( "Access-Control-Allow-Origin", "*" );
			HttpContext.Current.Response.ContentType = "application/json";
			HttpContext.Current.Response.Write( message );
			HttpContext.Current.Response.End();
		}
	}
}

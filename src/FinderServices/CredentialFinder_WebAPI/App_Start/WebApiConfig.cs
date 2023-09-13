using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;

using workIT.Utilities;

namespace CredentialFinderWebAPI
{
	public static class WebApiConfig
	{
		public static void Register( HttpConfiguration config )
		{
			// Web API configuration and services

			// Web API routes
			config.MapHttpAttributeRoutes();
			//enable CORS
			//https://docs.microsoft.com/en-us/aspnet/web-api/overview/security/enabling-cross-origin-requests-in-web-api
			var corsSiteURL = UtilityManager.GetAppKeyValue( "corsSiteURL" ).TrimEnd( '/' );
			var cors = new EnableCorsAttribute( "*", "*", "*" );//apparantly will use asterisk for now, verify OK, then plan for perhaps using corsSiteURL in production. 
			config.EnableCors( cors );
			//
			config.Routes.MapHttpRoute(
				name: "DefaultApi",
				routeTemplate: "api/{controller}/{id}",
				defaults: new { id = RouteParameter.Optional }
			);

		}
	}
}

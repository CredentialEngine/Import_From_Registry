using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

using workIT.Utilities;
namespace CredentialFinderWebAPI
{
	public class WebApiApplication : System.Web.HttpApplication
	{
		protected void Application_Start()
		{
			AreaRegistration.RegisterAllAreas();
			GlobalConfiguration.Configure( WebApiConfig.Register );
			FilterConfig.RegisterGlobalFilters( GlobalFilters.Filters );
			RouteConfig.RegisterRoutes( RouteTable.Routes );
			BundleConfig.RegisterBundles( BundleTable.Bundles );
		}
		void Session_Start( object sender, EventArgs e )
		{
		}
		protected void Application_Error( object sender, EventArgs e )
		{
			Exception exception = Server.GetLastError();
			Response.Clear();
			if ( exception.Message.IndexOf( "Server cannot set status after HTTP headers" ) == -1 )
			{
				
			}
			bool is404 = false;
			HttpException httpException = exception as HttpException;
			if ( httpException != null )
			{
				RouteData routeData = new RouteData();
				routeData.Values.Add( "controller", "Error" );

				switch ( httpException.GetHttpCode() )
				{
					case 404:
						// page not found
						routeData.Values.Add( "action", "HttpError404" );
						is404 = true;
						break;
					//case 400:
					//    // page not found
					//    routeData.Values.Add( "action", "HttpError404" );
					//    break;
					case 500:
						// server error
						routeData.Values.Add( "action", "HttpError500" );
						break;
					default:
						routeData.Values.Add( "action", "General" );
						break;
				}
				routeData.Values.Add( "error", exception );
				string ipAddress = this.GetUserIPAddress();
				bool loggingError = true;
				if ( exception.Message.IndexOf( "Server cannot set status after HTTP headers" ) > -1
					|| ipAddress.IndexOf( "66.24" ) == 0
					|| is404 )
				{
					loggingError = false;
				}
				string url = "MISSING";
				try
				{
					HttpContext con = HttpContext.Current;
					url = con.Request.Url.ToString();
					url = url.Trim();
				}
				catch
				{
					//skip
				}

				string lRefererPage = GetUserReferrer();
				string currentUser = "unknown";

				if ( url.EndsWith( "&" ) || url.EndsWith( ";" ) )
				{
					LoggingHelper.DoTrace( 4, string.Format( "Application_Error. url: {0} referer: {1}, ipAddress: {2}, message: {3}", url.Trim(), lRefererPage, ipAddress, exception.Message ) );
					url = url.TrimEnd( '&' ).Trim();
					url = url.TrimEnd( ';' ).Trim();
					//do we want to do this? Could append a marker parm and then check 
					Response.Redirect( url, true );
				}
				else
				{
					if ( loggingError )
					{
						//messages will probably be different
						//if ( IsARepeatError( ipAddress, exception.Message ) )
						//	return;
						//StoreLastError( ipAddress, exception.Message );
						//may not want constant emails. Could check for existing message or ip address?
						//LoggingHelper.LogError( exception, string.Format( "Application HttpException. url: {0}, Referer: {1}, ipAddress: {2}", url, lRefererPage, ipAddress ), false );
						LoggingHelper.DoTrace( 1, string.Format( "Application HttpException. url: {0}, Referer: {1}, ipAddress: {2}, Message: {3}", url, lRefererPage, ipAddress, exception.Message ), false );
					}
				}
				// clear error on server ==> this would hide the error in dev as well
				//Server.ClearError();

				// at this point how to properly pass route data to error controller?
			}
			else
			{
				string url = "MISSING";
				try
				{
					HttpContext con = HttpContext.Current;
					url = con.Request.Url.ToString();
					url = url.Trim();
				}
				catch
				{
					//skip
				}
				string lRefererPage = "unknown";
				try
				{
					if ( Request.UrlReferrer != null )
					{
						lRefererPage = Request.UrlReferrer.ToString();
					}
				}
				catch ( Exception ex )
				{
					//skip
				}
				LoggingHelper.LogError( exception, string.Format( "Application Exception. (missing initial httpException) url: {0}, Referer: {1}", url, lRefererPage ) );
			}
		}
		private string GetUserReferrer()
		{
			string lRefererPage = "unknown";
			try
			{
				if ( Request.UrlReferrer != null )
				{
					lRefererPage = Request.UrlReferrer.ToString();
					//check for link to us parm
					//??
				}
			}
			catch ( Exception ex )
			{
				lRefererPage = ex.Message;
			}

			return lRefererPage;
		} //
		private string GetUserIPAddress()
		{
			string ip = "unknown";
			try
			{
				ip = Request.ServerVariables[ "HTTP_X_FORWARDED_FOR" ];
				if ( ip == null || ip == "" || ip.ToLower() == "unknown" )
				{
					ip = Request.ServerVariables[ "REMOTE_ADDR" ];
				}
			}
			catch ( Exception ex )
			{

			}

			return ip;
		} //
	}
}


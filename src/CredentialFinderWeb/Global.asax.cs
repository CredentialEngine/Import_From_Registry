using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Helpers;
using System.Web.Http;
using System.Web.Routing;

using workIT.Models;
using workIT.Services;
using workIT.Utilities;

namespace CredentialFinderWeb
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            WebApiConfig.Register( GlobalConfiguration.Configuration );

            AntiForgeryConfig.SuppressXFrameOptionsHeader = true;
        }
		/// <summary>
		/// Prototype to force to https - doesn't work well
		/// </summary>
		//protected void Application_BeginRequest()
		//{
		//	if ( !Context.Request.IsSecureConnection
		//		&& !Context.Request.IsLocal // to avoid switching to https when local testing
		//		)
		//	{
		//		Response.Clear();
		//		Response.Status = "301 Moved Permanently";
		//		Response.AddHeader( "Location", Context.Request.Url.ToString().Insert( 4, "s" ) );
		//		Response.End();
		//	}
		//}
		protected void Application_Error( object sender, EventArgs e )
		{
			Exception exception = Server.GetLastError();
			Response.Clear();

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
				bool loggingError = true;
				if ( exception.Message.IndexOf( "Server cannot set status after HTTP headers;" ) > -1 )
					loggingError = false;
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
				var user = AccountServices.GetCurrentUser();
				string currentUser = "unknown";
				if ( user != null && user.Id > 0 )
				{
					currentUser = string.Format( "User: {0} ({1})", user.FullName(), user.Id );
				}
				if ( url.EndsWith( "&" ) || url.EndsWith( ";" ) )
				{
					LoggingHelper.DoTrace( 4, string.Format( "Application_Error. url: {0} referer: {1}, currentUser: {2}, message: {3}", url.Trim(), lRefererPage, currentUser, exception.Message ) );
					url = url.TrimEnd( '&' ).Trim();
					url = url.TrimEnd( ';' ).Trim();
					Response.Redirect( url, true );
				}
				else
				{
					if ( loggingError )
						LoggingHelper.LogError( exception, string.Format( "Application HttpException. url: {0}, Referer: {1}, currentUser: {2}", url, lRefererPage, currentUser ) );
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
				var user = AccountServices.GetUserFromSession();
				LoggingHelper.LogError( exception, string.Format( "Application Exception. url: {0}, Referer: {1}, currentUser: {2}", url, lRefererPage, user ) );
			}
		}

		void Session_Start( object sender, EventArgs e )
        {
            // Code that runs when a new session is started
            //apparantly can prevent error:
            /*
			Session state has created a session id, but cannot save it because the response was already flushed by the application
			*/
            string sessionId = Session.SessionID;
            try
            {
                //Do we want to track the referer somehow??
                string lRefererPage = GetUserReferrer();
                bool isBot = false;
                string ipAddress = this.GetUserIPAddress();
                //check for bots
                //use common method
                string agent = GetUserAgent( ref isBot );
                string url = "MISSING";
                try
                {
                    HttpContext con = HttpContext.Current;
                    url = con.Request.Url.ToString();
                }
                catch
                {
                    //skip
                }
                if ( isBot == false )
                {
                    AppUser user = new AppUser();
                    //not always helpful
                    if ( User.Identity.IsAuthenticated )
                        user = AccountServices.GetCurrentUser( User.Identity.Name );
                    string userState = user.Id > 0 ? string.Format( "User: {0}", user.FullName() ) : "none";

                    LoggingHelper.DoTrace( 6, string.Format( "Session_Start. referrer: {0}, starting Url: {1} agent: {2}, IP Address: {3}, User?: {4}", lRefererPage, url, agent, ipAddress, userState ) );

                    string startMsg = "Session Started. SessionID: " + sessionId;

                    startMsg += ", IP Address: " + ipAddress;

                    startMsg += ", User: " + userState;
                    startMsg += ", Agent: " + agent;
                    ActivityServices.SessionStartActivity( startMsg, sessionId, ipAddress, lRefererPage, isBot );

                }
                else
                {
                    LoggingHelper.DoBotTrace( 8, string.Format( "Session_Start. Skipping bot: referrer: {0}, agent: {1}", lRefererPage, agent ) );
                }
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, "Session_Start. =================" );
            }

        } //
        private static string GetUserAgent( ref bool isBot )
        {
            string agent = "";
            isBot = false;
            try
            {
                if ( HttpContext.Current.Request.UserAgent != null )
                {
                    agent = HttpContext.Current.Request.UserAgent;
                }

                if ( agent.ToLower().IndexOf( "bot" ) > -1
                    || agent.ToLower().IndexOf( "spider" ) > -1
                    || agent.ToLower().IndexOf( "slurp" ) > -1
                    || agent.ToLower().IndexOf( "crawl" ) > -1
                    || agent.ToLower().IndexOf( "addthis.com" ) > -1
                    )
                    isBot = true;
                if ( isBot )
                {
                    //what should happen? Skip completely? Should add attribute to track
                    //user agent may NOT be available in this context
                }
            }
            catch ( Exception ex )
            {
                //agent = ex.Message;
            }

            return agent;
        } //

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

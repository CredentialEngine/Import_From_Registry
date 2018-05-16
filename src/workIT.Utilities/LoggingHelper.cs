using System;
using System.IO;
using System.Web;

namespace workIT.Utilities
{
    public class LoggingHelper
    {
        const string thisClassName = "LoggingHelper";

        public LoggingHelper() { }


        #region Error Logging ================================================
        /// <summary>
        /// Format an exception and message, and then log it
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <param name="message">Additional message regarding the exception</param>
        public static void LogError( Exception ex, string message, string subject = "Credential Finder Exception encountered" )
        {
            bool notifyAdmin = false;
            LogError( ex, message, notifyAdmin, subject );
        }

        /// <summary>
        /// Format an exception and message, and then log it
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <param name="message">Additional message regarding the exception</param>
        /// <param name="notifyAdmin">If true, an email will be sent to admin</param>
        public static void LogError( Exception ex, string message, bool notifyAdmin, string subject = "Credential Finder Exception encountered" )
        {

            //string userId = "";
            string sessionId = "unknown";
            string remoteIP = "unknown";
            string path = "unknown";
            string queryString = "unknown";
            string url = "unknown";
            string parmsString = "";
            string lRefererPage= "";

            try
            {
                if ( UtilityManager.GetAppKeyValue( "notifyOnException", "no" ).ToLower() == "yes" )
                    notifyAdmin = true;

                sessionId = HttpContext.Current.Session.SessionID.ToString();
                remoteIP = HttpContext.Current.Request.ServerVariables[ "REMOTE_HOST" ];

                if ( HttpContext.Current.Request.UrlReferrer != null )
                {
                    lRefererPage = HttpContext.Current.Request.UrlReferrer.ToString();
                }
                string serverName = UtilityManager.GetAppKeyValue( "serverName", HttpContext.Current.Request.ServerVariables[ "LOCAL_ADDR" ] );
                path = serverName + HttpContext.Current.Request.Path;

                if ( FormHelper.IsValidRequestString() == true )
                {
                    queryString = HttpContext.Current.Request.Url.AbsoluteUri.ToString();
                    //url = GetPublicUrl( queryString );

                    url = HttpContext.Current.Server.UrlDecode( queryString );
                    //if ( url.IndexOf( "?" ) > -1 )
                    //{
                    //    parmsString = url.Substring( url.IndexOf( "?" ) + 1 );
                    //    url = url.Substring( 0, url.IndexOf( "?" ) );
                    //}
                }
                else
                {
                    url = "suspicious url encountered!!";
                }
                //????
                //userId = WUM.GetCurrentUserid();
            }
            catch
            {
                //eat any additional exception
            }

            try
            {
                string errMsg = message +
                    "\r\nType: " + ex.GetType().ToString() + ";" + 
                    "\r\nSession Id - " + sessionId + "____IP - " + remoteIP +
                    "\r\rReferrer: " + lRefererPage + ";" +
                    "\r\nException: " + ex.Message.ToString() + ";" + 
                    "\r\nStack Trace: " + ex.StackTrace.ToString() +
                    "\r\nServer\\Template: " + path +
                    "\r\nUrl: " + url;

				if ( ex.InnerException != null && ex.InnerException.Message != null )
				{
					errMsg += "\r\n****Inner exception: " + ex.InnerException.Message;

					if ( ex.InnerException.InnerException != null && ex.InnerException.InnerException.Message != null )
						errMsg += "\r\n@@@@@@Inner-Inner exception: " + ex.InnerException.InnerException.Message;
				}

                if ( parmsString.Length > 0 )
                    errMsg += "\r\nParameters: " + parmsString;

                LoggingHelper.LogError( errMsg, notifyAdmin, subject );
            }
            catch
            {
                //eat any additional exception
            }

        } //

		/// <summary>
		/// Format an exception handling inner exceptions as well
		/// </summary>
		/// <param name="ex"></param>
		/// <returns></returns>
		public static string FormatExceptions( Exception ex )
		{
			string message = ex.Message;

			if ( ex.InnerException != null )
			{
				message += "; \r\nInnerException: " + ex.InnerException.Message;
				if ( ex.InnerException.InnerException != null )
				{
					message += "; \r\nInnerException2: " + ex.InnerException.InnerException.Message;
				}
			}

			return message;
		}

		/// <summary>
		/// Write the message to the log file.
		/// </summary>
		/// <remarks>
		/// The message will be appended to the log file only if the flag "logErrors" (AppSetting) equals yes.
		/// The log file is configured in the web.config, appSetting: "error.log.path"
		/// </remarks>
		/// <param name="message">Message to be logged.</param>
		public static void LogError( string message, string subject = "Credential Finder Exception encountered" )
        {

            if ( UtilityManager.GetAppKeyValue( "notifyOnException", "no" ).ToLower() == "yes" )
            {
                LogError( message, true, subject );
            }
            else
            {
                LogError( message, false, subject );
            }

        } //
        /// <summary>
        /// Write the message to the log file.
        /// </summary>
        /// <remarks>
        /// The message will be appended to the log file only if the flag "logErrors" (AppSetting) equals yes.
        /// The log file is configured in the web.config, appSetting: "error.log.path"
        /// </remarks>
        /// <param name="message">Message to be logged.</param>
        /// <param name="notifyAdmin"></param>
        public static void LogError( string message, bool notifyAdmin, string subject = "Credential Finder Exception encountered" )
        {
            if ( UtilityManager.GetAppKeyValue( "logErrors" ).ToString().Equals( "yes" ) )
            {
                try
                {
                    //would like to limit number, just need a means to overwrite the first time used in a day
                    //- check existance, then if for a previous day, overwrite
                    string datePrefix1 = System.DateTime.Today.ToString("u").Substring(0, 10);
                    string datePrefix = System.DateTime.Today.ToString("yyyy-dd");
                    string logFile = UtilityManager.GetAppKeyValue("path.error.log", "");
                    if (!string.IsNullOrWhiteSpace(logFile))
                    {
                        string outputFile = logFile.Replace("[date]", datePrefix);
                        if (File.Exists(outputFile))
                        {
                            if (File.GetLastWriteTime(outputFile).Month != DateTime.Now.Month)
                                File.Delete(outputFile);
                        }

                        StreamWriter file = File.AppendText(outputFile);
                        file.WriteLine(DateTime.Now + ": " + message);
                        file.WriteLine("---------------------------------------------------------------------");
                        file.Close();

                        if (notifyAdmin)
                        {
                            if (ShouldMessagesBeSkipped(message) == false)
                                EmailManager.NotifyAdmin(subject, message);
                        }
                    }
                }
                catch ( Exception ex )
                {
                    //eat any additional exception
                    DoTrace( 5, thisClassName + ".LogError(string message, bool notifyAdmin). Exception: " + ex.Message );
                }
            }
        } //
		private static bool ShouldMessagesBeSkipped(string message)
		{

			if ( message.IndexOf( "Server cannot set status after HTTP headers have been sent" ) > 0 )
				return true;

			return false;
		}

        #endregion


        #region === Application Trace Methods ===
        /// <summary>
        /// IsTestEnv - determines if the current environment is a testing/development
        /// </summary>
        /// <returns>True if localhost - implies testing</returns>
        //public static bool IsTestEnv()
        //{
        //    string host = HttpContext.Current.Request.Url.Host.ToString();

        //    if ( host.ToLower() == "localhost" )
        //        return true;
        //    else
        //        return false;

        //} //

        /// <summary>
        /// Handle trace requests - typically during development, but may be turned on to track code flow in production.
        /// </summary>
        /// <param name="label">Label control that will display a trace message</param>
        /// <param name="message">The message to be sent to the trace log as well as to the trace control</param>
        //public static void DoTrace( System.Web.UI.WebControls.Label label, string message )
        //{
        //    try
        //    {
        //        label.Text += message + "<br>";

        //        label.Visible = true;

        //        DoTrace( message );
        //    }
        //    catch
        //    {
        //        // ignore error for now - future to log it
        //    }

        //} // end

        /// <summary>
        /// Handle trace requests - typically during development, but may be turned on to track code flow in production.
        /// </summary>
        /// <param name="message">Trace message</param>
        /// <remarks>This is a helper method that defaults to a trace level of 10</remarks>
        public static void DoTrace( string message )
        {
            //default level to 8
            int appTraceLevel = UtilityManager.GetAppKeyValue( "appTraceLevel", 8 );
            if ( appTraceLevel < 8 )
                appTraceLevel = 8;
            DoTrace( appTraceLevel, message, true );
        }

        /// <summary>
        /// Handle trace requests - typically during development, but may be turned on to track code flow in production.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="message"></param>
        public static void DoTrace( int level, string message )
        {
            DoTrace( level, message, true );
        }

        /// <summary>
        /// Handle trace requests - typically during development, but may be turned on to track code flow in production.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="message"></param>
        /// <param name="showingDatetime">If true, precede message with current date-time, otherwise just the message> The latter is useful for data dumps</param>
        public static void DoTrace( int level, string message, bool showingDatetime )
        {
            //TODO: Future provide finer control at the control level
            string msg = "";
            int appTraceLevel = 0;
            //bool useBriefFormat = true;

            try
            {
                appTraceLevel = UtilityManager.GetAppKeyValue( "appTraceLevel", 6 );

                //Allow if the requested level is <= the application thresh hold
                if ( level <= appTraceLevel )
                {
                    if ( showingDatetime )
                        msg = "\n " + System.DateTime.Now.ToString() + " - " + message;
                    else
                        msg = "\n " + message;


                    string datePrefix1 = System.DateTime.Today.ToString("u").Substring(0, 10);
                    string datePrefix = System.DateTime.Today.ToString("yyyy-dd");
                    string logFile = UtilityManager.GetAppKeyValue( "path.trace.log", "" );
                    if (!string.IsNullOrWhiteSpace(logFile))
                    {
                        string outputFile = logFile.Replace("[date]", datePrefix);
                        if (File.Exists(outputFile))
                        {
                            if (File.GetLastWriteTime(outputFile).Month != DateTime.Now.Month)
                                File.Delete(outputFile);
                        }

                        StreamWriter file = File.AppendText(outputFile);

                        file.WriteLine(msg);
                        file.Close();
                    }
                }
            }
            catch
            {
                //ignore errors
            }

        }
		public static void WriteLogFile( int level, string filename, string message, 
			string datePrefixOverride = "", 
			bool appendingText = true )
		{
			int appTraceLevel = 0;

			try
			{
				appTraceLevel = UtilityManager.GetAppKeyValue( "appTraceLevel", 6 );

				//Allow if the requested level is <= the application thresh hold
				if ( level <= appTraceLevel )
				{					
					string datePrefix = System.DateTime.Today.ToString( "u" ).Substring( 0, 10 );
					if ( !string.IsNullOrWhiteSpace( datePrefixOverride ) )
						datePrefix = datePrefixOverride;

					string logFile = UtilityManager.GetAppKeyValue( "path.log.file", "C:\\LOGS.txt" );
					string outputFile = logFile.Replace( "[date]", datePrefix ).Replace( "[filename]", filename );
					if ( outputFile.IndexOf( "json.txt" ) > 1 )
						outputFile = outputFile.Replace( "json.txt", "json" );

					if ( appendingText )
					{
						StreamWriter file = File.AppendText( outputFile );

						file.WriteLine( message );
						file.Close();
					}
					else
					{
						//FileStream file = File.Create( outputFile );

						//file.( message );
						//file.Close();
						File.WriteAllText( outputFile, message );
					}
				}
			}
			catch
			{
				//ignore errors
			}

		}
		public static void DoBotTrace( int level, string message )
		{
			string msg = "";
			int appTraceLevel = 0;

			try
			{
				appTraceLevel = UtilityManager.GetAppKeyValue( "botTraceLevel", 5 );

				//Allow if the requested level is <= the application thresh hold
				if ( level <= appTraceLevel )
				{
					msg = "\n " + System.DateTime.Now.ToString() + " - " + message;

                    string datePrefix1 = System.DateTime.Today.ToString( "u" ).Substring( 0, 10 );
                    string datePrefix = System.DateTime.Today.ToString( "yyyy-dd" );
                    string logFile = UtilityManager.GetAppKeyValue( "path.botTrace.log", "" );
                    if (!string.IsNullOrWhiteSpace( logFile ))
                    {
                        string outputFile = logFile.Replace( "[date]", datePrefix );
                        if (File.Exists( outputFile ))
                        {
                            if (File.GetLastWriteTime( outputFile ).Month != DateTime.Now.Month)
                                File.Delete( outputFile );
                        }

                        StreamWriter file = File.AppendText( outputFile );

                        file.WriteLine( msg );
                        file.Close();
                    }
                }
			}
			catch
			{
				//ignore errors
			}

		}
        /// <summary>
        /// Record a page visit, either to file or to the database 
        /// </summary>
        /// <param name="sessionId">Session Id</param>
        /// <param name="isPostBack">Was this a page postback</param>
        /// <param name="template">MCMS Template</param>
        /// <param name="queryString">Request URL</param>
        /// <param name="parmString">Request Parameters (if any)</param>
        /// <param name="userid">Userid of current user (guest if not logged in)</param>
        /// <param name="partner">Partner name</param>
        /// <param name="comment">Comment</param>
        /// <param name="remoteIP">client IP address</param>
        /// <remarks>06/09/15 contactUs - added remoteIP</remarks>
		public static void LogPageVisit( string sessionId, string template, string queryString, string parmString, bool isPostBack, string userid, string partner, string comment, string remoteIP, string lwia )
        {
            System.DateTime visitDate = System.DateTime.Now;

            string pathway = "";
            string lang = "";
            string mainChannel = "";
           // string currentZip = "";
            //skip startup records
            if ( sessionId.ToLower().IndexOf( "worknet" ) > -1
                || comment.ToLower().StartsWith( "session " ) )
            { //skip these records

            }
            else
            {
                try
                {
                    //pathway = GetPathTitle();
                    //lang = getLanguage();
                    //mainChannel = getPathChannel();
                    //currentZip = GetDefaultZipcode();
                }
                catch
                {
                    //ignore
                }
            }

            string logEntry = sessionId + ","
                + visitDate.ToString() + ","
                + pathway + ","
                + lang + ","
                + mainChannel + ","
                + template + ","
                + queryString + ",'"
                + parmString + "',"
                + isPostBack + ","
                + userid + ","
                + partner
                + ",Lwia:" + lwia
                + ",'" + comment + "',"
                + remoteIP + "";

            LogPageVisit( logEntry );
            

        } //

        /// <summary>
        /// Log a page visit. Output is to a file.
        /// </summary>
        /// <param name="message"></param>
        private static void LogPageVisit( string message )
        {

            string msg = "";
            string outputPath = "";
			string logFileAppKey = "path.trace.log"; //"path.visitor.log"
            try
            {
				msg = "**** Visitor.log: " + message;

                string datePrefix = System.DateTime.Today.ToString( "u" ).Substring( 0, 10 );

				string logFile = UtilityManager.GetAppKeyValue( logFileAppKey, "C:\\VOS_LOGS.txt" );

                string outputFile = logFile.Replace( "[date]", datePrefix );

                outputPath = outputFile;

                StreamWriter file = File.AppendText( outputPath );
                file.WriteLine( msg );
                file.Close();

            }
            catch ( Exception ex )
            {
                //ignore errors
                LogError( "UtilityManager.LogPageVisit: " + ex.ToString(), false );
            }

        } //
        private static string GetServerPath( string fileName )
        {
            System.Web.HttpApplication swh = new System.Web.HttpApplication();
            return swh.Server.MapPath( fileName );

        } //


        #endregion
    }
}

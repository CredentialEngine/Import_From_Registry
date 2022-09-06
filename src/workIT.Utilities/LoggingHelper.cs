using System;
using System.IO;
using System.Threading;
using System.Web;

using DBEntity = workIT.Data.Tables.MessageLog;
using EntityContext = workIT.Data.Tables.workITEntities;
using ThisEntity = workIT.Models.MessageLog;

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
        public static void LogError( Exception ex, string message, string subject = "" )
        {
			var application = UtilityManager.GetAppKeyValue( "application", "Credential Finder" );
			if (string.IsNullOrWhiteSpace( subject ) )
            {
				subject = application + " Exception encountered";
			}
			bool notifyAdmin = false;
            LogError( ex, message, notifyAdmin, subject );
		}

		/// <summary>
		/// Format an exception and message, and then log it
		/// </summary>
		/// <param name="ex">Exception</param>
		/// <param name="message">Additional message regarding the exception</param>
		/// <param name="notifyAdmin">If true, an email will be sent to admin</param>
		public static void LogError( Exception ex, string activity, string summary, bool notifyAdmin, string subject = "" )
		{
			var application = UtilityManager.GetAppKeyValue( "application", "Credential Finder" );
			if ( string.IsNullOrWhiteSpace( subject ) )
			{
				subject = application + " Exception encountered";
			}
			if ( UtilityManager.GetAppKeyValue( "loggingErrorsToDatabase", false ) )
			{
				var message = FormatExceptions( ex );
				//check for duplicates
				var mcheck = activity + "-" + summary + "-" + ex.Message;
				if ( IsADuplicateRequest( mcheck ) )
				{
					//just write to file
					LoggingHelper.LogError( mcheck, false );
					return;
				}
					
				var messageLog = new ThisEntity()
				{
					Application = application,
					Activity = activity,
					Message = summary,
					Description = message + ex.StackTrace.ToString()
				};
				AddMessage( messageLog );
				StoreLastError( mcheck );
			}
			else
				LoggingHelper.LogError( ex, activity + "--"+ summary, notifyAdmin, subject );
		}

		public static void LogError( string activity, string summary, string message, string details, bool notifyAdmin = false )
		{
			var application = UtilityManager.GetAppKeyValue( "application", "Credential Finder" );

			if ( UtilityManager.GetAppKeyValue( "loggingErrorsToDatabase", false ) )
			{
				//check for duplicates
				var mcheck = activity + "-" + summary + "-" + message;
				if ( IsADuplicateRequest( mcheck ) )
				{
					//just write to file
					mcheck += "\r\n" + details ?? "";
					LoggingHelper.LogError( mcheck, false );
					return;
				}

				var messageLog = new ThisEntity()
				{
					Application = application,
					Activity = activity,
					Message = message,
					Description = details ?? ""
				};
				AddMessage( messageLog );
				StoreLastError( mcheck );
			}
			else
			{
				var mcheck = activity + "-" + summary + "-" + message;
				mcheck += "\r\n" + details ?? "";
				LoggingHelper.LogError( mcheck );
			}
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

				if ( HttpContext.Current != null )
				{
					if ( HttpContext.Current.Session != null )
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

				if ( UtilityManager.GetAppKeyValue( "loggingErrorsToDatabase", false ) )
				{
					var messageLog = new ThisEntity()
					{
						Activity = message,
						Message = ex.Message,
						Description = errMsg
					};
					AddMessage( messageLog );
					//also log to file system without notify
					LoggingHelper.LogError( errMsg, false, subject );
				}
				else 
					LoggingHelper.LogError( errMsg, notifyAdmin, subject );
            }
            catch
            {
                //eat any additional exception
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
					if ( !string.IsNullOrWhiteSpace( logFile ) )
					{
						string outputFile = logFile.Replace( "[date]", datePrefix );
						if ( File.Exists( outputFile ) )
						{
							if ( File.GetLastWriteTime( outputFile ).Month != DateTime.Now.Month )
								File.Delete( outputFile );
						}
						else
						{
							System.IO.FileInfo f = new System.IO.FileInfo( outputFile );
							f.Directory.Create(); // If the directory already exists, this method does nothing.
						}

						StreamWriter file = File.AppendText( outputFile );
						file.WriteLine( DateTime.Now + ": " + message );
						file.WriteLine( "---------------------------------------------------------------------" );
						file.Close();
					}

					if (notifyAdmin)
                    {
                        if (ShouldMessagesBeSkipped(message) == false && !IsADuplicateRequest( message ) )
                            EmailManager.NotifyAdmin(subject, message);

						StoreLastError( message );
					}
                   
                }
                catch ( Exception ex )
                {
                    //eat any additional exception
                    DoTrace( 5, thisClassName + ".LogError(string message, bool notifyAdmin). Exception while logging. \r\nException: " + ex.Message + ".\r\n Original message: " + message );
                }
            }
        } //


		private static int AddMessage( ThisEntity entity )
		{
			int count = 0;
			string truncateMsg = "";
			DBEntity efEntity = new DBEntity();
			MapToDB( entity, efEntity );

			//================================
			//if ( IsADuplicateRequest( efEntity.Description ) )
			//	return 0;

			//StoreLastRequest( efEntity.Description );

			//----------------------------------------------

			if ( efEntity.RelatedUrl != null && efEntity.RelatedUrl.Length > 600 )
			{
				truncateMsg += string.Format( "RelatedUrl overflow: {0}; ", efEntity.RelatedUrl.Length );
				efEntity.RelatedUrl = efEntity.RelatedUrl.Substring( 0, 600 );
			}


			//the following should not be necessary but getting null related exceptions
			if ( efEntity.ActionByUserId == null )
				efEntity.ActionByUserId = 0;


			using ( var context = new EntityContext() )
			{
				try
				{
					efEntity.Created = System.DateTime.Now;
					if ( string.IsNullOrEmpty(efEntity.MessageType) || efEntity.MessageType.Length < 5 )
						efEntity.MessageType = "Error";

					context.MessageLog.Add( efEntity );

					// submit the change to database
					count = context.SaveChanges();
					if ( count > 0 )
					{
						return efEntity.Id;
					}
					else
					{
						//?no info on error
						return 0;
					}
				}
				catch ( Exception ex )
				{
					var message = FormatExceptions( ex );
					LoggingHelper.LogError( thisClassName + ".MessageLogAdd(MessageLog) Exception: \n\r" + message + "\n\r" + ex.StackTrace.ToString(), false );

					return count;
				}
			}
		} //
		private static void MapToDB( ThisEntity input, DBEntity output )
		{
			output.Id = input.Id;

			output.Application = input.Application;
			output.Activity = input.Activity;
			if ( output.Activity.Length > 200 )
			{
				output.Activity = output.Activity.Substring( 0, 194 ) + "  ...";
			}
			output.MessageType = input.MessageType;
			output.Message = input.Message;
			output.Description = input.Description;

			output.ActionByUserId = input.ActionByUserId;
			output.ActivityObjectId = input.ActivityObjectId;
			output.Tags = input.Tags;

			output.RelatedUrl = input.RelatedUrl;
			output.SessionId = input.SessionId;
			output.IPAddress = input.IPAddress;

			if ( output.SessionId == null || output.SessionId.Length < 10 )
				output.SessionId = GetCurrentSessionId();

			if ( output.IPAddress != null && output.IPAddress.Length > 50 )
				output.IPAddress = output.IPAddress.Substring( 0, 50 );
		}
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
		private static bool ShouldMessagesBeSkipped(string message)
		{

			if ( message.IndexOf( "Server cannot set status after HTTP headers have been sent" ) > -1 )
				return true;

			return false;
		}
		public static void StoreLastError( string message )
		{
			string sessionKey = GetCurrentSessionId() + "_lastError";

			try
			{
				if ( HttpContext.Current != null && HttpContext.Current.Session != null )
				{
					HttpContext.Current.Session[ sessionKey ] = message;
				}
			}
			catch
			{
			}

		} //
		public static bool IsADuplicateRequest( string message )
		{
			string sessionKey = GetCurrentSessionId() + "_lastError";
			bool isDup = false;
			try
			{
				if ( HttpContext.Current != null && HttpContext.Current.Session != null )
				{
					string lastAction = HttpContext.Current.Session[ sessionKey ]?.ToString();
					if ( lastAction?.ToLower() == message.ToLower() )
					{
						LoggingHelper.DoTrace( 7, "ActivityServices. Duplicate message: " + message );
						return true;
					}
				}
				else
				{
					
				}
			}
			catch
			{

			}
			return isDup;
		}
		public static string GetCurrentSessionId()
		{
			string sessionId = "unknown";

			try
			{
				//NOTE: ignore Object not found exception when called from a batch process
				if ( HttpContext.Current != null && HttpContext.Current.Session != null )
				{
					sessionId = HttpContext.Current.Session.SessionID;
				}
			
			}
			catch
			{
			}
			return sessionId;
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
        /// Handle trace requests - with addition of a log prefix to enable a custom log file
        /// </summary>
        /// <param name="level"></param>
        /// <param name="message"></param>
        /// <param name="logPrefix">If present, will be prefixed to logfile name after the date.</param>
        public static void DoTrace(int level, string message, string logPrefix)
        {

            DoTrace( level, message, true, logPrefix );
        }


		/// <summary>
		/// Handle trace requests - typically during development, but may be turned on to track code flow in production.
		/// </summary>
		/// <param name="level"></param>
		/// <param name="message"></param>
		/// <param name="showingDatetime">If true, precede message with current date-time, otherwise just the message> The latter is useful for data dumps</param>
		public static void DoTrace( int level, string message, bool showingDatetime = true, string logPrefix = "")
		{
			//TODO: Future provide finer control at the control level
			string msg = "";
			int appTraceLevel = UtilityManager.GetAppKeyValue( "appTraceLevel", 6 );
			//bool useBriefFormat = true;
			const int NumberOfRetries = 4;
			const int DelayOnRetry = 1000;
			if ( showingDatetime )
				msg = "\n " + System.DateTime.Now.ToString() + " - " + message;
			else
				msg = "\n " + message;
			string datePrefix1 = System.DateTime.Today.ToString( "u" ).Substring( 0, 10 );
			string datePrefix = System.DateTime.Today.ToString( "yyyy-dd" );
			string logFile = UtilityManager.GetAppKeyValue( "path.trace.log", "" );
			if ( !string.IsNullOrWhiteSpace( logPrefix ) )
				datePrefix += "_" + logPrefix;
			//Allow if the requested level is <= the application thresh hold
			if ( string.IsNullOrWhiteSpace( logFile ) || level > appTraceLevel )
			{
				return;
			}
			string outputFile = "";
            if (message.IndexOf( "UPPER CASE URI" ) > -1 || message.IndexOf( "GRAPH URI" ) > -1 )
            {
                logFile = logFile.Replace( "[date]", "[date]_URI_ISSUES" );
            }
			//added retries where log file is in use
			for ( int i = 1; i <= NumberOfRetries; ++i )
			{
				try
				{
					outputFile = logFile.Replace( "[date]", datePrefix + ( i < 3 ? "" : "_" + i.ToString() ) );

					if ( File.Exists( outputFile ) )
					{
						if ( File.GetLastWriteTime( outputFile ).Month != DateTime.Now.Month )
							File.Delete( outputFile );
					}
					else
					{
						System.IO.FileInfo f = new System.IO.FileInfo( outputFile );
						f.Directory.Create(); // If the directory already exists, this method does nothing.
											 
					}

					StreamWriter file = File.AppendText( outputFile );

					file.WriteLine( msg );
					file.Close();
                    if ( level > 0)
					    Console.WriteLine( msg );
					break;
				}
				catch ( IOException e ) when ( i <= NumberOfRetries )
				{
					// You may check error code to filter some exceptions, not every error
					// can be recovered.
					Thread.Sleep( DelayOnRetry );
				}
				catch ( Exception ex )
				{
					//ignore errors
				}
			}
		}
		public static void WriteLogFileForReason( int level, string reason, string filename, string message,
			string datePrefixOverride = "", bool appendingText = true, int cacheForHours = 2 )
		{
			if ( !LoggingHelper.IsMessageInCache( reason ) )
			{
				AddMessageToCache( reason, cacheForHours );
				WriteLogFile( level, filename, message, datePrefixOverride, appendingText );
				if ( reason.Length > 10 )
					LoggingHelper.LogError( reason, true, "Finder: CredentialSearch Elastic error" );
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
					else if ( datePrefixOverride == " " )
						datePrefix = "";
					filename = filename.Replace( ":", "-" );
					string logFile = UtilityManager.GetAppKeyValue( "path.log.file", "C:\\LOGS.txt" );
					string outputFile = logFile.Replace( "[date]", datePrefix ).Replace( "[filename]", filename );
                    if ( outputFile.IndexOf( "csv.txt" ) > 1 )
                        outputFile = outputFile.Replace( "csv.txt", "csv" );
                    else if ( outputFile.IndexOf( "csv.json" ) > 1 )
                        outputFile = outputFile.Replace( "csv.json", "csv" );
					else if ( outputFile.IndexOf( "html.json" ) > 1 )
						outputFile = outputFile.Replace( "html.json", "html" );
					else if ( outputFile.IndexOf( "json.txt" ) > 1 )
                        outputFile = outputFile.Replace( "json.txt", "json" );
                    else if ( outputFile.IndexOf( "json.json" ) > 1 )
                        outputFile = outputFile.Replace( "json.json", "json" );
                    else if ( outputFile.IndexOf( "txt.json" ) > 1 )
                        outputFile = outputFile.Replace( "txt.json", "txt" );

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
		public static bool IsMessageInCache( string message )
		{
			if ( string.IsNullOrWhiteSpace( message ) )
				return false;
			string key = UtilityManager.GenerateMD5String( message );
			try
			{
				if ( HttpRuntime.Cache[ key ] != null )
				{
					var msgExists = ( string )HttpRuntime.Cache[ key ];
					if ( msgExists.ToLower() == message.ToLower() )
					{
						LoggingHelper.DoTrace( 7, "LoggingHelper. Duplicate message: " + message );
						return true;
					}
				}
			}
			catch
			{
			}

			return false;
		}
		public static void AddMessageToCache( string message, int cacheForHours = 2 )
		{
			string key = UtilityManager.GenerateMD5String( message );
			
			if ( HttpContext.Current != null )
			{
				if ( HttpContext.Current.Cache[ key ] != null )
				{
					HttpRuntime.Cache.Remove( key );
				}
				else
				{
					System.Web.HttpRuntime.Cache.Insert( key, message, null, DateTime.Now.AddHours( cacheForHours ), TimeSpan.Zero );
				}
			}		
		}
		public static bool IsADuplicateRecentSessionMessage( string message )
		{
			string sessionKey = UtilityManager.GenerateMD5String(message);
			bool isDup = false;
			try
			{
				if ( HttpContext.Current != null )
				{
					if ( HttpContext.Current.Session != null )
					{
						string msgExists = HttpContext.Current.Session[ sessionKey ].ToString();
						if ( msgExists.ToLower() == message.ToLower() )
						{
							LoggingHelper.DoTrace( 7, "LoggingHelper. Duplicate message: " + message );
							return true;
						}
					}
				}
			}
			catch
			{

			}
			return isDup;
		}
		public static void StoreSessionMessage( string message )
		{
			string sessionKey = UtilityManager.GenerateMD5String( message );

			try
			{
				if ( HttpContext.Current != null )
				{

					if ( HttpContext.Current.Session != null )
					{
						HttpContext.Current.Session[ sessionKey ] = message;
					}
				}
			}
			catch
			{
			}

		} //
		//public static void DoBotTrace( int level, string message )
		//{
		//	string msg = "";
		//	int appTraceLevel = 0;

		//	try
		//	{
		//		appTraceLevel = UtilityManager.GetAppKeyValue( "botTraceLevel", 5 );

		//		//Allow if the requested level is <= the application thresh hold
		//		if ( level <= appTraceLevel )
		//		{
		//			msg = "\n " + System.DateTime.Now.ToString() + " - " + message;

  //                  string datePrefix1 = System.DateTime.Today.ToString( "u" ).Substring( 0, 10 );
  //                  string datePrefix = System.DateTime.Today.ToString( "yyyy-dd" );
  //                  string logFile = UtilityManager.GetAppKeyValue( "path.botTrace.log", "" );
  //                  if (!string.IsNullOrWhiteSpace( logFile ))
  //                  {
  //                      string outputFile = logFile.Replace( "[date]", datePrefix );
  //                      if (File.Exists( outputFile ))
  //                      {
  //                          if (File.GetLastWriteTime( outputFile ).Month != DateTime.Now.Month)
  //                              File.Delete( outputFile );
  //                      }
		//				else
		//				{
		//					System.IO.FileInfo f = new System.IO.FileInfo( outputFile );
		//					f.Directory.Create(); // If the directory already exists, this method does nothing.
		//				}

		//				StreamWriter file = File.AppendText( outputFile );

  //                      file.WriteLine( msg );
  //                      file.Close();
  //                  }
  //              }
		//	}
		//	catch
		//	{
		//		//ignore errors
		//	}
  //      } //


        #endregion
    }
	public class CachedItem
	{
		public CachedItem()
		{
			lastUpdated = DateTime.Now;
		}
		public DateTime lastUpdated { get; set; }

	}
	public class RecentMessage : CachedItem
	{
		public string Message { get; set; }

	}
}

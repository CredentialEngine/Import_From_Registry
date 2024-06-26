using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Web;

using DBResource = workIT.Data.Tables.MessageLog;
using EntityContext = workIT.Data.Tables.workITEntities;
using ThisResource = workIT.Models.MessageLog;

namespace workIT.Utilities
{
    public class LoggingHelper
    {
        const string thisClassName = "LoggingHelper";
		//default specialTrace to high. Code can be called to lower it
		public static int appSpecialTraceLevel = UtilityManager.GetAppKeyValue( "appSpecialTraceLevel", 9 );
		//using this means show with whatever the trace level is
		public static int appTraceLevel = UtilityManager.GetAppKeyValue( "appTraceLevel", 5 );
		public static int appDebuggingTraceLevel = UtilityManager.GetAppKeyValue( "appDebuggingTraceLevel", 7 );
		public static int appMethodEntryTraceLevel = UtilityManager.GetAppKeyValue( "appMethodEntryTraceLevel", 7 );
		public static int appMethodExitTraceLevel = UtilityManager.GetAppKeyValue( "appMethodExitTraceLevel", 8 );
		public static int appSectionDurationTraceLevel = UtilityManager.GetAppKeyValue( "appSectionDurationTraceLevel", 8 );
		public LoggingHelper() { }


        #region Error Logging ================================================

		/// <summary>
		/// Format an exception and message, and then log it
		/// </summary>
		/// <param name="ex">Exception</param>
		/// <param name="message">Additional message regarding the exception</param>
		/// <param name="notifyAdmin">If true, an email will be sent to admin</param>
		public static void LogError( Exception ex, string activity, string summary )
		{
			var application = UtilityManager.GetAppKeyValue( "application", "CredentialFinder" );
			
			if ( UtilityManager.GetAppKeyValue( "loggingErrorsToDatabase", false ) )
			{
				var message = FormatExceptions( ex );
				//check for duplicates
				var mcheck = activity + "-" + summary + "-" + ex.Message;
				if ( IsADuplicateRequest( mcheck ) )
				{
					//just write to file
					LoggingHelper.LogError( mcheck );
					return;
				}
					
				var messageLog = new ThisResource()
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
				LoggingHelper.LogError( ex, activity + "--"+ summary );
		}

		/// <summary>
		/// A custom error with detail useful for an email
		/// </summary>
		/// <param name="activity">Often the source class</param>
		/// <param name="summary">Method or activity in the class</param>
		/// <param name="message"></param>
		/// <param name="details"></param>
		/// <param name="notifyAdmin"></param>
		public static void LogError( string activity, string summary, string message, string details, bool notifyAdmin = false )
		{
			var application = UtilityManager.GetAppKeyValue( "application", "CredentialFinder" );

			if ( UtilityManager.GetAppKeyValue( "loggingErrorsToDatabase", false ) )
			{
				//check for duplicates
				var mcheck = activity + "-" + summary + "-" + message;
				if ( IsADuplicateRequest( mcheck ) )
				{
					//just write to file
					mcheck += "\r\n" + details ?? string.Empty;
					LoggingHelper.LogError( mcheck );
					return;
				}

				var messageLog = new ThisResource()
				{
					Application = application,
					Activity = activity + "-" + summary,
					Message = message,
					Description = details ?? string.Empty
				};
				AddMessage( messageLog );
				StoreLastError( mcheck );
			}
			else
			{
				var mcheck = activity + "-" + summary + "-" + message;
				mcheck += "\r\n" + details ?? string.Empty;
				LoggingHelper.LogError( mcheck );
			}
		}

		/// <summary>
		/// Format an exception and message, and then log it
		/// </summary>
		/// <param name="ex">Exception</param>
		/// <param name="message">Additional message regarding the exception</param>
		/// <param name="notifyAdmin">If true, an email will be sent to admin</param>
		public static void LogError( Exception ex, string message )
        {
			var application = UtilityManager.GetAppKeyValue( "application", "CredentialFinder" );

			//string userId = string.Empty;
			string sessionId = "unknown";
            string remoteIP = "unknown";
            string path = "unknown";
            string url = "unknown";
            string lRefererPage= string.Empty;

            try
            {
				if ( HttpContext.Current != null )
				{
					if ( HttpContext.Current.Session != null )
						sessionId = HttpContext.Current.Session.SessionID;
					remoteIP = HttpContext.Current.Request.ServerVariables[ "REMOTE_HOST" ];

					if ( HttpContext.Current.Request.UrlReferrer != null )
					{
						lRefererPage = HttpContext.Current.Request.UrlReferrer.AbsoluteUri;
					}
					string serverName = UtilityManager.GetAppKeyValue( "serverName", HttpContext.Current.Request.ServerVariables[ "LOCAL_ADDR" ] );
					path = serverName + HttpContext.Current.Request.Path;
                    url = HttpContext.Current.Request.Url.AbsoluteUri;
				}
			}
            catch (Exception e)
            {
				LogError(e, "Unexpected exception");
            }

			string errMsg = message;
			
            try
            {
                var exMsg = FormatExceptions( ex );
				errMsg +=
					"\r\nType: " + ex.GetType().ToString() + ";" +
					"\r\nSession Id - " + sessionId + "____IP - " + remoteIP +
					"\r\rReferrer: " + lRefererPage + " ";
				errMsg +=
					"\r\nException: " + exMsg + " ";
                errMsg +=
                    "\r\nStack Trace: " + ex.StackTrace?.ToString() +
                    "\r\nServer\\Template: " + path +
                    "\r\nUrl: " + url;
            }
            catch (Exception e)
            {
                LogError(e, "Unexpected exception");
            }

            try
            {
                if ( UtilityManager.GetAppKeyValue( "loggingErrorsToDatabase", false ) )
                {
                    var messageLog = new ThisResource()
                    {
                        Application = application,
                        Activity = message,
                        Message = ex.Message,
                        Description = errMsg
                    };
                    AddMessage( messageLog );
                    //also log to file system without notify
                    LoggingHelper.LogError(errMsg);
                }
                else
                    LoggingHelper.LogError( errMsg );
            }
            catch (Exception e)
            {
                LogError(e, "Unexpected exception");
            }
        }

        /// <summary>
        /// Write the message to the log file.
        /// </summary>
        /// <remarks>
        /// The message will be appended to the log file only if the flag "logErrors" (AppSetting) equals yes.
        /// The log file is configured in the web.config, appSetting: "error.log.path"
        /// </remarks>
        /// <param name="message">Message to be logged.</param>
        public static void LogError( string message )
        {
            if ( UtilityManager.GetAppKeyValue( "logErrors" ).ToString().Equals( "yes" ) )
            {
                try
                {
                    //would like to limit number, just need a means to overwrite the first time used in a day
                    //- check existance, then if for a previous day, overwrite
                    string datePrefix1 = DateTime.Today.ToString("u").Substring(0, 10);
                    string datePrefix = DateTime.Today.ToString("yyyy-dd");
                    string logFile = UtilityManager.GetAppKeyValue("path.error.log", string.Empty);
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
							FileInfo f = new FileInfo( outputFile );
							f.Directory.Create(); // If the directory already exists, this method does nothing.
						}

						StreamWriter file = File.AppendText( outputFile );
						file.WriteLine( DateTime.Now + ": " + message );
						Trace.TraceError(message);
						file.WriteLine( "---------------------------------------------------------------------" );
						file.Close();
					}
                }
                catch ( Exception ex )
                {
                    //eat any additional exception
                    DoTrace( 5, thisClassName + ".LogError(string message, bool notifyAdmin). Exception while logging. \r\nException: " + ex.Message + ".\r\n Original message: " + message );
                }
            }
        }

		private static int AddMessage( ThisResource entity )
		{
			int count = 0;
			string truncateMsg = string.Empty;
			DBResource efEntity = new DBResource();
			MapToDB( entity, efEntity );

			if ( efEntity.RelatedUrl != null && efEntity.RelatedUrl.Length > 600 )
			{
				truncateMsg += string.Format( "RelatedUrl overflow: {0}; ", efEntity.RelatedUrl.Length );
				efEntity.RelatedUrl = efEntity.RelatedUrl.Substring( 0, 600 );
			}


			//the following should not be necessary but getting null related exceptions
			if (efEntity.ActionByUserId == null)
			{
				efEntity.ActionByUserId = 0;
			}

			using ( var context = new EntityContext() )
			{
				try
				{
					efEntity.Created = DateTime.Now;
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
					LoggingHelper.LogError( thisClassName + ".MessageLogAdd(MessageLog) Exception: \n\r" + message + "\n\r" + ex.StackTrace.ToString() );

					return count;
				}
			}
		}

		private static void MapToDB( ThisResource input, DBResource output )
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
			{
				output.SessionId = GetCurrentSessionId();
			}

			if ( output.IPAddress != null && output.IPAddress.Length > 50 )
			{
				output.IPAddress = output.IPAddress.Substring( 0, 50 );
			}
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
                    if ( ex.InnerException.InnerException.InnerException != null )
                    {
                        message += "; \r\nInnerException3: " + ex.InnerException.InnerException.InnerException.Message;
                    }
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

		public static void DoTrace( int level, List<string> message )
		{
			//
			foreach ( var item in message )
			{
				DoTrace( level, item );
			}
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
			Trace.TraceInformation(message);

            //TODO: Future provide finer control at the control level
            string msg = string.Empty;
			int appTraceLevel = UtilityManager.GetAppKeyValue( "appTraceLevel", 6 );
			const int NumberOfRetries = 4;
			const int DelayOnRetry = 1000;
			if ( showingDatetime )
			{
				msg = "\n " + DateTime.Now.ToString() + " - " + message;
			}
			else
			{
				msg = "\n " + message;
			}

			string datePrefix1 = DateTime.Today.ToString( "u" ).Substring( 0, 10 );
			string datePrefix = DateTime.Today.ToString( "yyyy-dd" );
			string logFile = UtilityManager.GetAppKeyValue( "path.trace.log", string.Empty );
			if ( !string.IsNullOrWhiteSpace( logPrefix ) )
			{
				datePrefix += "_" + logPrefix;
			}

			//Allow if the requested level is <= the application thresh hold
			if ( string.IsNullOrWhiteSpace( logFile ) || level > appTraceLevel )
			{
				return;
			}
			
			string outputFile = string.Empty;
            if (message.IndexOf( "UPPER CASE URI" ) > -1 || message.IndexOf( "GRAPH URI" ) > -1 )
            {
                logFile = logFile.Replace( "[date]", "[date]_URI_ISSUES" );
            }

			//added retries where log file is in use
			for ( int i = 1; i <= NumberOfRetries; ++i )
			{
				try
				{
					outputFile = logFile.Replace( "[date]", datePrefix + ( i < 3 ? string.Empty : "_" + i.ToString() ) );

					if ( File.Exists( outputFile ) )
					{
						if ( File.GetLastWriteTime( outputFile ).Month != DateTime.Now.Month )
						{ 
							File.Delete( outputFile );
						}
					}
					else
					{
						FileInfo f = new FileInfo( outputFile );
						f.Directory.Create(); // If the directory already exists, this method does nothing.											 
					}

					StreamWriter file = File.AppendText( outputFile );
					file.WriteLine( msg );
					file.Close();
                    if ( level > 0)
					{
					    Console.WriteLine( msg );
					}
					break;
				}
				catch ( IOException e ) when ( i <= NumberOfRetries )
				{
                    // You may check error code to filter some exceptions, not every error
                    // can be recovered.
                    LogError(e, string.Format("Unexpected exception. Delaying retry by {0}ms", DelayOnRetry) );
                    Task.Delay( DelayOnRetry ).Wait();
				}
				catch ( Exception ex )
				{
					LogError(ex, "Unexpected exception");
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
                    LoggingHelper.LogError(reason);
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
					string datePrefix = DateTime.Today.ToString( "u" ).Substring( 0, 10 );
					if ( !string.IsNullOrWhiteSpace( datePrefixOverride ) )
						datePrefix = datePrefixOverride;
					else if ( datePrefixOverride == " " )
						datePrefix = string.Empty;
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
					HttpRuntime.Cache.Insert( key, message, null, DateTime.Now.AddHours( cacheForHours ), TimeSpan.Zero );
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
		}

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

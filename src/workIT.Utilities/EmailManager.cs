using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using System.Net.Mail;
using Newtonsoft.Json;
using System.Net;

namespace workIT.Utilities
{
	/// <summary>
	/// Helper class, provides email services
	/// </summary>
	public class EmailManager 
	{
		//: BaseUtilityManager
		/// <summary>
		/// Default constructor for EmailManager
		/// </summary>
		public EmailManager()
		{ }

		public static string FormatEmailAddress( string address, string userName )
		{
			MailAddress fAddress;
			fAddress = new MailAddress( address, userName );

			return fAddress.ToString();
		} //

		/// <summary>
		/// Send a email created with the parameters
		/// The destination is assumed to be the site contact, and from defaults to the system value
		/// </summary>
		/// <remarks>
		/// Use the SMTP server configured in the web.config as smtpEmail
		/// </remarks>
		/// <param name="subject">Email subject</param>
		/// <param name="message">Message Text</param>
		/// <returns></returns>
		public static bool SendSiteEmail( string subject, string message )
		{
			string toEmail = UtilityManager.GetAppKeyValue( "contactUsMailTo", "cwd.contactUs@ad.siu.edu" );
			string fromEmail = UtilityManager.GetAppKeyValue( "contactUsMailFrom", "contactUs@test.com" );
			return SendEmail( toEmail, fromEmail, subject, message, "", "" );

		} //

		public static bool SendEmail( string toEmail, string subject, string message )
		{
			string fromEmail = UtilityManager.GetAppKeyValue( "contactUsMailFrom", "contactUs@test.com" );
			return SendEmail( toEmail, fromEmail, subject, message, "", "" );

		} //

		public static bool SendEmail( string toEmail, string fromEmail, string subject, string message )
		{
			return SendEmail( toEmail, fromEmail, subject, message, "", "" );

		} //
		  /// <summary>
		  /// Send an e-mail using a formatted EmailNotice
		  /// - assumes the Message property contains the formatted e-mail - allows for not HTML variety
		  /// </summary>
		  /// <param name="toEmail"></param>
		  /// <param name="notice"></param>
		  /// <returns></returns>
		//public static bool SendEmail( string toEmail, EmailNotice notice )
		//{

		//	return SendEmail( toEmail, notice.FromEmail, notice.Subject, notice.Message, notice.CcEmail, notice.BccEmail );

		//} //

		/// <summary>
		/// Send a email created with the parameters
		/// </summary>
		/// <param name="toEmail"></param>
		/// <param name="fromEmail"></param>
		/// <param name="subject"></param>
		/// <param name="message"></param>
		/// <param name="CC"></param>
		/// <param name="BCC"></param>
		/// <returns></returns>
		public static bool SendEmail( string toEmail, string fromEmail, string subject, string message, string CC, string BCC )
		{
			char[] delim = new char[ 1 ];
			delim[ 0 ] = ',';
            MailMessage email = new MailMessage();
            string appEmail = UtilityManager.GetAppKeyValue( "contactUsMailFrom", "contactUs@test.com" );
			string systemAdminEmail = UtilityManager.GetAppKeyValue( "systemAdminEmail", "contactUs@test.com" );
			if ( string.IsNullOrWhiteSpace( BCC ) )
				BCC = systemAdminEmail;
			else
				BCC += ", " + systemAdminEmail;

			try
			{
				MailAddress maFrom;
				if ( fromEmail.Trim().Length == 0 )
                    fromEmail = appEmail;

				//10-04-06 contactUs - with our new mail server, we can't send emails where the from address is anything but @test.com
				//									- also try to handle the aliases 
				if ( fromEmail.IndexOf( "_siuccwd.com" ) > -1 )
					fromEmail = HandleProxyEmails( fromEmail.Trim() );

				if ( fromEmail.ToLower().IndexOf( "@test.com" ) == -1 )
				{
					//not this site so set to DoNotReply@ and switch
					string orig = fromEmail;
                    fromEmail = appEmail;
					//insert as first
                    //TODO - need a method parm to determine when should add to CC if at all. Should only show note on orginal sender
                    //if ( CC.Trim().Length > 0 )
                    //    CC = orig + "; " + CC;
                    //else
                    //    CC = orig;

				}


                maFrom = new MailAddress( fromEmail );

				//check for overrides on the to email 
				if ( UtilityManager.GetAppKeyValue( "usingTempOverrideEmail", "no" ) == "yes" )
				{
                    if ( toEmail.ToLower().IndexOf( "illinoisworknet.com" ) < 0 && toEmail.ToLower().IndexOf( "test.com" ) < 0 )
					{
						toEmail = UtilityManager.GetAppKeyValue( "systemAdminEmail", "contactUs@test.com" );
					}
				}

				if ( toEmail.Trim().EndsWith( ";" ) )
					toEmail = toEmail.TrimEnd( Char.Parse( ";" ), Char.Parse( " " ) );


				email.From = maFrom;
				//use the add format to handle multiple email addresses - not sure what delimiters are allowed
				toEmail = toEmail.Replace( ";", "," );
				//email.To.Add( toEmail );
				string[] toSplit = toEmail.Trim().Split( delim );
				foreach ( string item in toSplit )
				{
					if ( item.Trim() != "" )
					{
						string addr = HandleProxyEmails( item.Trim() );
						MailAddress ma = new MailAddress( addr );
						email.To.Add( ma );

					}
				}

				//email.To = FormatEmailAddresses( toEmail );


				if ( CC.Trim().Length > 0 )
				{
					CC = CC.Replace( ";", "," );
					//email.CC.Add( CC );

					string[] ccSplit = CC.Trim().Split( delim );
					foreach ( string item in ccSplit )
					{
						if ( item.Trim() != "" )
						{
							string addr = HandleProxyEmails( item.Trim() );
							MailAddress ma = new MailAddress( addr );
							email.CC.Add( ma );

						}
					}
				}
				if ( BCC.Trim().Length > 0 )
				{
					BCC = BCC.Replace( ";", "," );
					//email.Bcc.Add( BCC );

					string[] bccSplit = BCC.Trim().Split( delim );
					foreach ( string item in bccSplit )
					{
						if ( item.Trim() != "" )
						{
							string addr = HandleProxyEmails( item.Trim() );
							MailAddress ma = new MailAddress( addr );
							email.Bcc.Add( ma );
						}
					}
				}

				email.Subject = subject;
				email.Body = message;
				
				
				email.IsBodyHtml = true;
				//email.BodyFormat = MailFormat.Html;
				if ( UtilityManager.GetAppKeyValue( "sendEmailFlag", "false" ) == "TRUE" )
				{
					DoSendEmail( email);
				}

				if ( UtilityManager.GetAppKeyValue( "logAllEmail", "no" ) == "yes" )
				{
					LogEmail( 1, email );
				}
				return true;
			} catch ( Exception exc )
			{
                if ( UtilityManager.GetAppKeyValue( "logAllEmail", "no" ) == "yes" )
                {
                    LogEmail( 1, email );
                }
				LoggingHelper.LogError( "UtilityManager.sendEmail(): Error while attempting to send:"
					+ "\r\nFrom:" + fromEmail + "   To:" + toEmail
					+ "\r\nCC:(" + CC + ") BCC:(" + BCC + ") "
					+ "\r\nSubject:" + subject
					+ "\r\nMessage:" + message
					+ "\r\nError message: " + exc.ToString() );
			}

			return false;
		} //
		private static void DoSendEmail( MailMessage emailMsg )
		{
			string emailService = UtilityManager.GetAppKeyValue( "emailService", "smtp" );

            if ( emailService == "mailgun" )
                SendEmailViaMailgun( emailMsg );
            else if ( emailService == "serviceApi" )
                SendEmailViaApi( emailMsg );
			else if ( emailService == "smtp" )
			{
				SmtpClient smtp = new SmtpClient( UtilityManager.GetAppKeyValue( "SmtpHost" ) );
				smtp.UseDefaultCredentials = false;
				smtp.Credentials = new NetworkCredential( "contactUs", "SomeNewCreds" );

				smtp.Send( emailMsg );
				//SmtpMail.Send(email);
			}
			//else if ( emailService == "sendGrid" )
			//{
			//	EmailViaSendGrid( emailMsg );
			//}
			else
			{
				//no service api
				LoggingHelper.DoTrace( 2, "***** EmailManager.SendEmail - UNHANDLED EMAIL SERVICE ENCOUNTERED ******");
				return;
			}
			
		}

		private static void SendEmailViaApi( MailMessage emailMsg )
		{
			string emailServiceUri = UtilityManager.GetAppKeyValue( "SendEmailService" );
			if ( string.IsNullOrWhiteSpace( emailServiceUri ) )
			{
				//no service api
				LoggingHelper.DoTrace( 2, "***** EmailManager.SendEmail - no email service has been configured" );
				return;
			}
			//else if ( emailServiceUri.ToLower().Equals( "sendgrid" ) )
			//{
			//	EmailViaSendGrid( emailMsg );
			//	return;
			//} 
			//else if ( emailServiceUri.ToLower().Equals( "smtp" ) )
			//{
			//	EmailViaSmtp( emailMsg );
			//	return;
			//}


			var email = new Email()
			{
				Message = emailMsg.Body,
				From = emailMsg.From.Address,
				To = emailMsg.To.ToString(),
				CC = emailMsg.CC.ToString(),
				BCC = emailMsg.Bcc.ToString(),
				IsHtml = true,
				Subject = emailMsg.Subject
			};

			var postBody = JsonConvert.SerializeObject( email );

			PostRequest( postBody, emailServiceUri );
		}
		private static bool PostRequest( string postBody, string serviceUri )
		{

			try
			{
				using ( var client = new HttpClient() )
				{
					client.DefaultRequestHeaders.
						Accept.Add( new MediaTypeWithQualityHeaderValue( "application/json" ) );


					var task = client.PostAsync( serviceUri,
						new StringContent( postBody, Encoding.UTF8, "application/json" ) );
					task.Wait();
					var response = task.Result;

					return response.IsSuccessStatusCode;

				}
			}
			catch ( Exception exc )
			{
				LoggingHelper.LogError( exc, "Factories.EmailManager.PostRequest" );
				return false;

			}
}
        private static void SendEmailViaMailgun( MailMessage emailMsg )
        {
            bool valid = true;
            string status = "";
            var sendingDomain = System.Configuration.ConfigurationManager.AppSettings[ "MailgunSendingDomainName" ];
            var apiKey = System.Configuration.ConfigurationManager.AppSettings[ "MailgunSecretAPIKey" ];
            if ( string.IsNullOrWhiteSpace( sendingDomain ) )
            {
                //no service api
                LoggingHelper.DoTrace( 2, "***** EmailManager.SendEmailViaMailgun - no email service has been configured" );
                return;
            }
            var apiKeyEncoded = Convert.ToBase64String( UTF8Encoding.UTF8.GetBytes( "api" + ":" + apiKey ) );
            var url = "https://api.mailgun.net/v3/" + sendingDomain + "/messages";
            //
            var email = new MailgunEmail()
            {
                BodyHtml = emailMsg.Body,
                From = emailMsg.From.Address,
                BCC = emailMsg.Bcc.ToString(),
                Subject = emailMsg.Subject
            };
            email.To.Add( emailMsg.To.ToString() );
            email.CC.Add( emailMsg.CC.ToString() );
            var parameters = email.GetContent();

            using ( var client = new HttpClient() )
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue( "Basic", apiKeyEncoded );
                var result = client.PostAsync( url, parameters ).Result;
                valid = result.IsSuccessStatusCode;
                status = valid ? result.Content.ReadAsStringAsync().Result : result.ReasonPhrase;
            }
            if ( !valid )
            {
                LoggingHelper.DoTrace( 2, "***** EmailManager.SendEmailViaMailgun - error on send: " + status );
            }
        }
        private static bool EmailViaSendGrid( MailMessage emailMsg )
		{
			bool isValid = true;

			return isValid;
		}

		/// <summary>
		/// Handles multiple addresses in the passed email part
		/// </summary>
		/// <param name="address">String of one or more Email message</param>
		/// <returns>MailAddressCollection</returns>
		public static MailAddressCollection FormatEmailAddresses( string address )
		{
			char[] delim = new char[ 1 ];
			delim[ 0 ] = ',';
			MailAddressCollection collection = new MailAddressCollection();

			address = address.Replace( ";", "," );
			string[] split = address.Trim().Split( delim );
			foreach ( string item in split )
			{
				if ( item.Trim() != "" )
				{
					string addr = HandleProxyEmails( item );
					MailAddress copy = new MailAddress( addr );
					collection.Add( copy );
				}
			}

			return collection;

		} //

		/// <summary>
		/// Handle 'proxy' email addresses - wn address used for testing that include a number to make unique
		/// Can also handle any emails where the @:
		///		- Is followed by underscore, 
		///		- then any characters
		///		- then two underscore characters
		///		- followed with the valid domain name
		/// </summary>
		/// <param name="address">Email address</param>
		/// <returns>translated email address as needed</returns>
		private static string HandleProxyEmails( string address )
		{
			string newAddr = address;

			int atPos = address.IndexOf( "@" );
			int wnPos = address.IndexOf( "_siuccwd.com" );
			if ( wnPos > atPos )
			{
				newAddr = address.Substring( 0, atPos + 1 ) + address.Substring( wnPos + 1 );
			} else
			{
				//check for others with format:
				//	someName@_ ??? __realDomain.com
				atPos = address.IndexOf( "@_" );
				if ( atPos > 1 )
				{
					wnPos = address.IndexOf( "__", atPos );
					if ( wnPos > atPos )
					{
						newAddr = address.Substring( 0, atPos + 1 ) + address.Substring( wnPos + 2 );
					}
				}
			}

			return newAddr;
		} //

		/// <summary>
		/// Sends an email message to the system administrator
		/// </summary>
		/// <param name="subject">Email subject</param>
		/// <param name="message">Email message</param>
		/// <returns>True id message was sent successfully, otherwise false</returns>
		public static bool NotifyAdmin( string subject, string message )
		{
			string emailTo = UtilityManager.GetAppKeyValue( "systemAdminEmail", "contactUs@test.com" );	

			return  NotifyAdmin( emailTo, subject, message );
        } 

		/// <summary>
		/// Sends an email message to the system administrator
		/// </summary>
		/// <param name="emailTo">admin resource responsible for exceptions</param>
		/// <param name="subject">Email subject</param>
		/// <param name="message">Email message</param>
		/// <returns>True id message was sent successfully, otherwise false</returns>
		public static bool NotifyAdmin( string emailTo, string subject, string message )
		{
			char[] delim = new char[ 1 ];
			delim[ 0 ] = ',';
			string emailFrom = UtilityManager.GetAppKeyValue( "systemNotifyFromEmail", "TheWatcher@test.com" );
			string cc = UtilityManager.GetAppKeyValue( "systemAdminEmail", "contactUs@test.com" );
            if ( emailTo == "" )
            {
                emailTo = cc;
                cc = "";
            }
			//avoid infinite loop by ensuring this method didn't generate the exception
			if ( message.IndexOf( "EmailManager.NotifyAdmin" ) > -1 )
			{
				//skip may be error on send
				return true;

			} else
			{
				if ( emailTo.ToLower() == cc.ToLower() )
					cc = "";

				message = message.Replace("\r", "<br/>");

				MailMessage email = new MailMessage( emailFrom, emailTo );

				//try to make subject more specific
				//if: workNet Exception encountered, try to insert type
				if ( subject.IndexOf( "workNet Exception" ) > -1 )
				{
					subject = FormatExceptionSubject( subject, message );
				}
				email.Subject = subject;

				if ( message.IndexOf( "Type:" ) > 0 )
				{
					int startPos = message.IndexOf( "Type:" );
					int endPos = message.IndexOf( "Error Message:" );
					if ( endPos > startPos )
					{
						subject += " - " + message.Substring( startPos, endPos - startPos );
					}
				}
				if ( cc.Trim().Length > 0 )
				{
					cc = cc.Replace( ";", "," );
					//email.CC.Add( CC );

					string[] ccSplit = cc.Trim().Split( delim );
					foreach ( string item in ccSplit )
					{
						if ( item.Trim() != "" )
						{
							string addr = HandleProxyEmails( item.Trim() );
							MailAddress ma = new MailAddress( addr );
							email.CC.Add( ma );
						}
					}
				}

				email.Body = DateTime.Now + "<br>" + message.Replace( "\n\r", "<br>" );
				email.Body = email.Body.Replace( "\r\n", "<br>" );
				email.Body = email.Body.Replace( "\n", "<br>" );
				email.Body = email.Body.Replace( "\r", "<br>" );
				email.IsBodyHtml = true;
				//email.BodyFormat = MailFormat.Html;
				try
				{
					//The trace was a just in case, if the send fails, a LogError call will be made anyway. Set to a high level so not shown in prod
					LoggingHelper.DoTrace( 11, "EmailManager.NotifyAdmin: - Admin email was requested:\r\nSubject:" + subject + "\r\nMessage:" + message );
					if ( UtilityManager.GetAppKeyValue( "sendEmailFlag", "false" ) == "TRUE" )
					{
						DoSendEmail( email );
					}

					if ( UtilityManager.GetAppKeyValue( "logAllEmail", "no" ) == "yes" )
					{
						LogEmail( 1, email );
					}
					return true;
				} catch ( Exception exc )
				{
					LoggingHelper.LogError( "EmailManager.NotifyAdmin(): Error while attempting to send:"
						+ "\r\nSubject:" + subject + "\r\nMessage:" + message
						+ "\r\nError message: " + exc.ToString(), false );
				}
			}

			return false;
		} //

		/// <summary>
		/// Attempt to format a more meaningful subject for an exception related email
		/// </summary>
		/// <param name="subject"></param>
		/// <param name="message"></param>
		public static string FormatExceptionSubject( string subject, string message )
		{
			string work = "";

			try
			{
                int start = message.IndexOf( "Exception:" );
                int end = message.IndexOf( "Stack Trace:" );
                if ( end == -1)
                    end = message.IndexOf( ";",start );

				if ( start > -1 && end > start )
				{
					work = message.Substring( start, end - start );
					//remove line break
					work = work.Replace( "\r\n", "" );
					work = work.Replace( "<br>", "" );
					work = work.Replace( "Type:", "Exception:" );
					if ( message.IndexOf( "Caught in Application_Error event" ) > -1 )
					{
						work = work.Replace( "Exception:", "Unhandled Exception:" );
					}

				}
				if ( work.Length == 0 )
				{
					work = subject;
				}
			} catch
			{
				work = subject;
			}

			return work;
		} //

		/// <summary>
		/// Log email message - for future resend/reviews
		/// </summary>
		/// <param name="level"></param>
		/// <param name="email"></param>
		public static void LogEmail( int level, MailMessage email )
		{

			string msg = "";
			int appTraceLevel = 0;
			try
			{
				appTraceLevel = UtilityManager.GetAppKeyValue( "appTraceLevel", 1 );

				//Allow if the requested level is <= the application thresh hold
				if ( level <= appTraceLevel )
				{

					msg = "\n=============================================================== ";
					msg += "\nDate:	" + System.DateTime.Now.ToString();
					msg += "\nFrom:	" + email.From.ToString();
					msg += "\nTo:		" + email.To.ToString();
					msg += "\nCC:		" + email.CC.ToString();
					msg += "\nBCC:  " + email.Bcc.ToString();
					msg += "\nSubject: " + email.Subject.ToString();
					msg += "\nMessage: " + email.Body.ToString();
					msg += "\n=============================================================== ";

                    string datePrefix1 = System.DateTime.Today.ToString( "u" ).Substring( 0, 10 );
                    string datePrefix = System.DateTime.Today.ToString( "yyyy-dd" );
                    string logFile = UtilityManager.GetAppKeyValue( "path.email.log", "" );
                    if ( !string.IsNullOrWhiteSpace( logFile ) )
                    {
                        string outputFile = logFile.Replace( "[date]", datePrefix );
                        if ( File.Exists( outputFile ) )
                        {
                            if ( File.GetLastWriteTime( outputFile ).Month != DateTime.Now.Month )
                                File.Delete( outputFile );
                        }
                        StreamWriter file = File.AppendText( outputFile );

                        file.WriteLine( msg );
                        file.Close();
                    }

                }
			} catch
      {
				//ignore errors
			}

		}


        /// <summary>
        /// Get an email snippet
        /// The email name is the file name under AppData   without the .txt extension.
        /// The email name is also the key used for storing it in the cache.
        /// </summary>
        /// <param name="emailName"></param>
        /// <returns></returns>
        public static string GetEmailText(string emailName)
        {

            string body = HttpContext.Current.Cache[emailName] as string;

            if (string.IsNullOrEmpty(body))
            {
                string file = System.Web.HttpContext.Current.Server.MapPath(string.Format("~/App_Data/email/{0}.txt",emailName));
                body = File.ReadAllText(file);

                HttpContext.Current.Cache[emailName] = body;
            }

            return body;
        }

		public class Email
		{
			public string Message { get; set; }
			public string From { get; set; }
			public string To { get; set; }
			public string CC { get; set; }
			public string BCC { get; set; }
			public string Subject { get; set; }
			public bool IsHtml { get; set; }
		}
        public class MailgunEmail
        {
            public MailgunEmail()
            {
                To = new List<string>();
                CC = new List<string>();
                BCC = "";
            }

            public string From { get; set; }
            public List<string> To { get; set; }
            public List<string> CC { get; set; }
            public string BCC { get; set; }
            public string Subject { get; set; }
            public string BodyHtml { get; set; }
            public string BodyText { get; set; }

            public FormUrlEncodedContent GetContent()
            {
                var data = new List<KeyValuePair<string, string>>();
                Add( data, "from", From );
                Add( data, "subject", Subject );
                Add( data, "html", BodyHtml );
                Add( data, "text", BodyText );
                foreach ( var item in To )
                {
                    Add( data, "to", HandleProxyEmails( item ) );
                }
                foreach ( var item in CC )
                {
                    Add( data, "cc", HandleProxyEmails( item ) );
                }
                if ( !string.IsNullOrWhiteSpace( BCC ) )
                {
                    Add( data, "bcc", BCC );
                }
                return new FormUrlEncodedContent( data );
            }

            private void Add( List<KeyValuePair<string, string>> data, string key, string value )
            {
                if ( !string.IsNullOrWhiteSpace( value ) )
                {
                    data.Add( new KeyValuePair<string, string>( key, value ) );
                }
            }
        }
    }
}

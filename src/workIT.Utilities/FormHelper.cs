using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.SessionState;

namespace workIT.Utilities
{
    /// <summary>
    /// A utility class containing methods commonly used in forms/user controls
    /// </summary>
    public class FormHelper
	{
        static string thisClassName = "FormHelper";

		/// <summary>
		/// Default constructor for FormHelper
		/// </summary>
		public FormHelper()
		{ }

		#region Related constants
		/// <summary>
		/// property used to control access to a channel
		/// Allowed values:
		/// </summary>
		const string ErrorMessageStyle = "errorMessage";
        public static string DEFAULT_GUID = "00000000-0000-0000-0000-000000000000";
		#endregion

		#region application messaging methods

		/// <summary>
		/// Set Application Message using passed resource manager, resource string and placeholder 
		/// </summary>
		/// <param name="msgString">Message to be displayed</param>
		/// <param name="msgPlaceholder">PlaceHolder where message control will be loaded</param>
		public static void SetApplicationMessage( string msgString, PlaceHolder msgPlaceholder )
		{

			SetSessionItem( "appMessage", msgString );
			//
			//assume error style for now
			string css = UtilityManager.GetAppKeyValue( "errorMessageCss", "errorMessage" );

			FormMessage formMessage = new FormMessage();
			formMessage.Text = msgString;
			formMessage.Title = "Application Error";
			formMessage.CssClass = css;
			formMessage.ShowPopup = false;
			if ( msgString.IndexOf( "&lt;" ) > -1 || msgString.IndexOf( "</" ) > -1 )
			{
				formMessage.IsFormatted = true;
			}

			HttpContext.Current.Session[ "FormMessage" ] = formMessage;

		} //

		public static void SetApplicationMessage( string title, string msgString, string cssClass, bool showPopup )
		{
			SetSessionItem( "appMessage", msgString );
			//
			FormMessage formMessage = new FormMessage();
			formMessage.Text = msgString;
			formMessage.Title = title;
			formMessage.CssClass = cssClass;
			formMessage.ShowPopup = showPopup;
			if ( msgString.IndexOf( "&lt;" ) > -1 || msgString.IndexOf( "</" ) > -1 )
			{
				formMessage.IsFormatted = true;
			}

			HttpContext.Current.Session[ "FormMessage" ] = formMessage;

		} //

		/// <summary>
		/// Determine if an application message needs to be formatted. if so format using passed HTML container
		/// </summary>
		/// <param name="usingFormMessage">Set to Yes to use the FormMessage entity</param>
		/// <param name="container">The HTML control that will present the message, if present</param>
		public static void HandleAppMessage( string usingFormMessage, HtmlGenericControl container )
		{
			//bool usingFormMessage = true;
			if ( usingFormMessage == "yes" )
			{
				FormMessage message = new FormMessage();
				message = ( FormMessage ) HttpContext.Current.Session[ "FormMessage" ];
				if ( message != null )
				{

					if ( message.ShowPopup )
					{
						DisplayMsgBox( message.Title, message.Text );
					}

					//if (message.CssClass.Length > 0)
					//  container. = message.CssClass;

					if ( message.IsFormatted )
					{
						if ( message.Title.Length > 0 )
							container.InnerHtml += message.Title;

						if (message.Text.StartsWith("<p")) 
							container.InnerHtml += message.Text;
						else
							container.InnerHtml += "<p>" + message.Text + "</p>";
					} else
					{
						if ( message.Title.Length > 0 )
							container.InnerHtml += "<h2>" + message.Title + "</h2>";

						container.InnerHtml += "<p>" + message.Text + "</p>";
					}
					container.Visible = true;

					//now clear out message
					HttpContext.Current.Session.Remove( "FormMessage" );

				}
			} else
			{
				string appMessage = GetSessionItem( "appMessage", "" );

				if ( appMessage.Length > 0 )
				{
					container.InnerHtml = appMessage;
					container.Visible = true;
					//now clear out message
					HttpContext.Current.Session.Remove( "appMessage" );
				}

			}

		}//

		public static void HandleAppMessage( string usingFormMessage, Label container )
		{
			//bool usingFormMessage = true;
			if ( usingFormMessage == "yes" )
			{
				FormMessage message = new FormMessage();
				message = ( FormMessage ) HttpContext.Current.Session[ "FormMessage" ];
				if ( message != null )
				{

					if ( message.ShowPopup )
					{
						DisplayMsgBox( message.Title, message.Text );
					}

					if (message.CssClass.Length > 0)
					  container.CssClass = message.CssClass;

					if ( message.IsFormatted )
					{
						if ( message.Title.Length > 0 )
							container.Text += message.Title;

						if ( message.Text.StartsWith( "<p" ) )
							container.Text += message.Text;
						else
							container.Text += "<p>" + message.Text + "</p>";
					} else
					{
						if ( message.Title.Length > 0 )
							container.Text += "<h2>" + message.Title + "</h2>";

						container.Text += "<p>" + message.Text + "</p>";
					}
					container.Visible = true;

					//now clear out message
					HttpContext.Current.Session.Remove( "FormMessage" );

				}
			} else
			{
				string appMessage = GetSessionItem( "appMessage", "" );

				if ( appMessage.Length > 0 )
				{
					container.Text = appMessage;
					container.Visible = true;
					//now clear out message
					HttpContext.Current.Session.Remove( "appMessage" );
				}

			}

		}//		

		/// <summary>
		/// Format javascript to display a message box on next page load
		/// </summary>
		/// <param name="message">Display message</param>
		public static void DisplayMsgBox( string message )
		{
			string newMessage = UnformatHtml( message );

			System.Web.HttpContext.Current.Response.Write( "<SCRIPT  type='text/javascript' language='javascript'>" );
			System.Web.HttpContext.Current.Response.Write( "alert('" + newMessage + "')" );
			System.Web.HttpContext.Current.Response.Write( "</SCRIPT>" );
		}
		/// <summary>
		/// Format javascript to display a message box on next page load
		/// </summary>
		/// <param name="title">Title of message</param>
		/// <param name="message">Display message</param>
		public static void DisplayMsgBox( string title, string message )
		{
			if ( title.Length == 0 )
				title = "Note";
			string newMessage = UnformatHtml( message );
			string newtitle = UnformatHtml( title );

			System.Web.HttpContext.Current.Response.Write( "<SCRIPT  type='text/javascript' language='javascript'>" );
			System.Web.HttpContext.Current.Response.Write( "alert('" + newtitle + "\\n\\n" + newMessage + "')" );
			System.Web.HttpContext.Current.Response.Write( "</SCRIPT>" );
		}//
		public static string UnformatHtml( string message )
		{
			string newMessage;
			newMessage = message.Replace( "<p>", "\\n" );
			newMessage = newMessage.Replace( "</p>", "" );
			newMessage = newMessage.Replace( "<br>", "\\n" );
			newMessage = newMessage.Replace( "<br/>", "\\n" );
			newMessage = newMessage.Replace( "<br />", "\\n" );
			newMessage = newMessage.Replace( "<h2>", "" );
			newMessage = newMessage.Replace( "</h2>", "\\n" );

			return newMessage;
		}
		#endregion

		#region === Request handler methods ===
		/// <summary>
		/// Strip tags from user input.
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
        //public static String CleanText( String text )
        //{
        //    return CleanText( text );
        //}
        /// <summary>
        /// Strip tags from user input.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        /// 


        public static String CleanText(String text)
        {
            return CleanText(text, false);
        }

        public static String CleanText(String text, bool allowingHtmlPosts)
        {
            if ( String.IsNullOrEmpty( text.Trim() ) )
                return String.Empty;

            String output = String.Empty;
            try
            {
                if (allowingHtmlPosts == false)
                {
                    String rxPattern = "<(?>\"[^\"]*\"|'[^']*'|[^'\">])*>";
                    Regex rx = new Regex(rxPattern);
                    output = rx.Replace(text, String.Empty);
                }
                else
                {
                    output = text;
                }
                if ( output.ToLower().IndexOf( "<script" ) > -1
                    || output.ToLower().IndexOf( "javascript" ) > -1 )
                {
                    output = "";
                }
				//line breaks? - would not want arbitrarily remove these
                //one last??
                //output = FilterText( output );
            }
            catch 
            {

            }

            return output;
        }
        //private static String FilterText( String userInput )
        //{
        //    Regex re = new Regex( "([^A-Za-z0-9@.,' _-]+)" );
        //    String filtered = re.Replace( userInput, "_" );
        //    return filtered;
        //}
      

		/// <summary>
		/// do checks for possible invalid parameters in the request string. return false if suspect
		/// </summary>
		/// <returns></returns>
		public static bool IsValidRequestString()
		{
			//move to utility classes (FormHelper?)
			bool isValid = true;
			try
			{
				string request = HttpContext.Current.Request.QueryString.ToString();

				//create a default method for the following checks to make more easily available
				if ( request.ToLower().IndexOf( "<script" ) > -1
					|| request.ToLower().IndexOf( "javascript" ) > -1 )
				{
					//or just do a redirect???
					return false;
				}

				//other checks

			} catch ( Exception ex )
			{

			}

			return isValid;

		} // end

		/// <summary>
		/// Retrieve a particular parameter from the HTTP Request querystring.
		/// </summary>
		/// <param name="parameter">Parameter name to return from the Request QueryString</param>
		/// <param name="defaultParm">A default value to return in the event the requested parameter doesn't exist</param>
		/// <returns>The value for the requested parameter or the default value if the parameter was not found</returns>
		public static int GetRequestKeyValue( string parameter, int defaultParm )
		{
            if ( IsValidRequestString() == false )
            {
                return defaultParm;
            }
			string request = HttpContext.Current.Request.QueryString.Get( parameter );
			if ( request == null ) request = "";

			if ( IsInteger( request ) )
			{
				return int.Parse( request );
			} else
			{
				return defaultParm;
			}

		} // end

        public static bool GetRequestKeyValue( string parameter, bool defaultParm )
        {
            if ( IsValidRequestString() == false )
            {
                return defaultParm;
            }

            bool result = false;
            string request = HttpContext.Current.Request.QueryString.Get( parameter );
            if ( request == null ) request = defaultParm.ToString();

            if (bool.TryParse( request, out result) )
            {
                return result;
            }
            else
            {
                return defaultParm;
            }

        } // end
		/// <summary>
		/// Retrieve a particular parameter from the HTTP Request querystring.
		/// </summary>
		/// <param name="parameter">Parameter name to return from the Request QueryString</param>
		/// <returns>The value for the requested parameter or blank if the parameter was not found</returns>
		public static string GetRequestKeyValue( string parameter )
		{

			return GetRequestKeyValue( parameter, "" );

		} // end

		/// <summary>
		/// Retrieve a particular parameter from the HTTP Request querystring.
		/// </summary>
		/// <param name="parameter">Parameter name to return from the Request QueryString</param>
		/// <param name="defaultParm">A default value to return in the event the requested parameter doesn't exist</param>
		/// <returns>The value for the requested parameter or the default value if the parameter was not found</returns>
		public static string GetRequestKeyValue( string parameter, string defaultParm )
		{
			if ( IsValidRequestString() == false )
			{
				return defaultParm;
			}
			string request = HttpContext.Current.Request.QueryString.Get( parameter );

			if ( request == null )
			{
				return defaultParm;
			} else
			{
                request = CleanText( request );
                request = HttpUtility.UrlDecode(request);

                //if (request.IndexOf("';") > -1)
                //{
                //  request = request.Substring(0, request.IndexOf("';"));
                //}
                //if (request.IndexOf(";") > -1)
                //{
                //  request = request.Substring(0, request.IndexOf(";"));
                //}
				return request;
			}
        } // end

        /// <summary>
        /// Get a route parameter.
        /// If found, the variable will be 'cleaned'.
        /// If not found, return the default value of blank
        /// </summary>
        /// <param name="page"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public static string GetRouteKeyValue( Page page, string parameter )
        {
            return GetRouteKeyValue(page, parameter, "");
		} // end

        /// <summary>
        /// Get a route parameter.
        /// If found, the variable will be 'cleaned'.
        /// If not found, a second check will be done using GetRequestKeyValue 
        /// </summary>
        /// <param name="page"></param>
        /// <param name="parameter"></param>
        /// <param name="defaultParm"></param>
        /// <returns></returns>
        public static string GetRouteKeyValue( Page page, string parameter, string defaultParm )
        {
            string request = "";
            if ( IsValidRequestString() == false )
            {
                return defaultParm;
            }
            if ( page.RouteData.Values.Count > 0
              && page.RouteData.Values.ContainsKey( parameter ) )
            {
                request = page.RouteData.Values[ parameter ].ToString();
                request = CleanText( request );
                return request;
            }
            //check the http parms
            return GetRequestKeyValue( parameter, defaultParm );
            
        } // end


        /// <summary>
        /// Get a route parameter.
        /// If found, the variable will be 'cleaned'.
        /// If not found, a second check will be done using GetRequestKeyValue
        /// </summary>
        /// <param name="page"></param>
        /// <param name="parameter"></param>
        /// <param name="defaultParm"></param>
        /// <returns></returns>
        public static int GetRouteKeyValue( Page page, string parameter, int defaultParm )
        {
            if ( IsValidRequestString() == false )
            {
                return defaultParm;
            }

            if ( page.RouteData.Values.Count > 0
              && page.RouteData.Values.ContainsKey( parameter ) )
            {
                string request = page.RouteData.Values[ parameter ].ToString();
                if ( IsInteger( request ) )
                {
                    return int.Parse( request );
                }
                else
                {
                    return defaultParm;
                }  
            }
            //check the http parms
            return GetRequestKeyValue( parameter, defaultParm );
            
        } // end
		#endregion
		#region === Session handler methods ===
		/// <summary>
		/// Gets an item from the session as a string.
		/// This method is menat to hide the actual session implementation. In the event that, say SQL Server is used to 
		/// handle session data, then just this method chgs, no application code
		/// </summary>
		/// <remarks>This property is explicitly thread safe.</remarks>
		public static string GetSessionItem( string sessionKey )
		{
			string sessionValue = "";

			try
			{
				sessionValue = HttpContext.Current.Session[ sessionKey ].ToString();

			} catch
			{
				sessionValue = "";
			} finally
			{

			}
			return sessionValue;
		} //
		/// <summary>
		/// Gets an item from the session as a string.
		/// This method is menat to hide the actual session implementation. In the event that, say SQL Server is used to  handle session data, then just this method chgs, no application code
		/// </summary>
		/// <param name="sessionKey">Key name to retrieve from session</param> 
		/// <param name="defaultValue">Default value to use in not found in session</param> 
		/// <remarks>This property is explicitly thread safe.</remarks>
		public static string GetSessionItem( string sessionKey, string defaultValue )
		{
			string sessionValue = "";

			try
			{
				sessionValue = HttpContext.Current.Session[ sessionKey ].ToString();
			} catch
			{
				sessionValue = defaultValue;
			}

			return sessionValue;

		} //	
		/// <summary>
		/// Assigns a value to a session key
		/// </summary>
		/// <param name="sessionKey">String represnting the name of the session key</param>
		/// <param name="sessionValue">Value to be assigned to the session key</param>
		public static void SetSessionItem( string sessionKey, string sessionValue )
		{
			SetSessionItem( HttpContext.Current.Session, sessionKey, sessionValue );

		} //

		public static void SetSessionItem( HttpSessionState session, string sessionKey, string sessionValue )
		{
			session[ sessionKey ] = sessionValue;

		} //

		/// <summary>
		/// Get the ID for the current sessoin
		/// </summary>
		/// <returns></returns>
		public static string GetSessionId()
		{
			string id = "";

			try
			{
				id = HttpContext.Current.Session.SessionID.ToString();

			} catch
			{
				id = "";
			} finally
			{

			}
			return id;
		} //

		/// <summary>
		/// Format and return javascript that will be used to "keep alive" a session.
		/// Requires the followig support page to exist:
		///			/vos_portal/support/reconnect.aspx
		/// reference:
		/// http://www.codeproject.com/KB/session/Reconnect.aspx
		/// </summary>
		/// <param name="sessionTimeout">Current session timeout</param>
		/// <param name="maximumReconnects">Maximum Reconnects allowed (0 = 999)</param>
		/// <returns>Formatted javascript to be added to the calling page with a statement like the following:
		///					this.Page.RegisterClientScriptBlock("Reconnect", str_Script);
		/// </returns>
		public static string AddKeepAliveScript( int sessionTimeout, int maximumReconnects )
		{
			//int milliSecondsTimeOut = (this.Session.Timeout * 60000) - 30000;
			int milliSecondsTimeOut = (sessionTimeout * 60000) - 30000;
			if ( maximumReconnects == 0 ) maximumReconnects = 999;

			string str_Script = @"
			<script language='javascript' type='text/javascript'>
			//Number of Reconnects
			var count=0;
			//Maximum reconnects setting
			var max = " + maximumReconnects.ToString() + @";
			function Reconnect(){
				count++;
				if (count < max)
				{
					window.status = 'Link to Server Refreshed ' + count.toString()+' time(s)' ;
					var img = new Image(1,1);
					img.src = '/vos_portal/support/Reconnect.aspx';
				}
			}

			window.setInterval('Reconnect()'," + milliSecondsTimeOut.ToString()+ @"); //Set to length required

			</script>
			";

			//this.Page.RegisterClientScriptBlock("Reconnect", str_Script);

			return str_Script;
		}

		#endregion

		#region Control Helper methods

		/// <summary>
		///	Loops through the controls on the page and adds attibutes to the help hyperlinks.	
		/// </summary>
		/// <remarks>Created on 06-10-05 by twright</remarks>
		/// <remarks>ASP:Hyperlinks must have an id with a prefix of 'contextHelp'</remarks>
		public static void FormatHelpLinksStyle( Control parent )
		{
			try
			{
				foreach ( Control c in parent.Controls )
				{
					//LoggingHelper.DoTrace( 9, " @@@FormatHelpLinksStyle. Control Type: " + c.GetType().ToString() );
					if ( c.GetType().ToString().Equals( "System.Web.UI.WebControls.HyperLink" ) )
					{
						if ( GetControlId( c ).Length > 10 )
						{
							if ( c.ID.Substring( 0, 11 ) == "contextHelp" )
							{
								( ( HyperLink ) c ).Attributes.Add( "onBlur", "ChangeColor(this.id,'Off')" );
								( ( HyperLink ) c ).Attributes.Add( "onFocus", "ChangeColor(this.id,'On')" );
								( ( HyperLink ) c ).Attributes.Add( "style", "border-width:0px;border-style:Solid;" );
							}
						}
					} else
					{
						//check for nested containers and do recursive loop
						if ( c.Controls.Count > 0 )
						{
							FormatHelpLinksStyle( c );
						}
					}
				}
			} catch ( Exception ex )
			{
				//report error and continue
				LoggingHelper.LogError( ex, thisClassName + ".FormatHelpLinksStyle exception encountered" );
			}
		}//		
		private static string GetControlId( Control c )
		{
			string controlId = "";
			try
			{
				controlId = c.ID.ToString();
			} catch
			{
				controlId = "";
			}
			return controlId;
		}//
		#endregion

		#region ===== validation ===============================

		/// <summary>
		/// IsDate - test if passed string is a valid date
		/// </summary>
		/// <param name="stringToTest"></param>
		/// <returns></returns>
        public static bool IsDate( string stringToTest )
		{

			DateTime newDate;
			bool result = false;
			try
			{
				newDate = System.DateTime.Parse( stringToTest );
				result = true;
			} catch
			{

				result = false;
			}
			return result;

		} //end
		/// <summary>
		/// IsInteger - test if passed string is an integer
		/// </summary>
		/// <param name="stringToTest"></param>
		/// <returns></returns>
		public static bool IsInteger( string stringToTest )
		{
			int newVal;
			bool result = false;
			try
			{
				newVal = Int32.Parse( stringToTest );

				// If we get here, then number is an integer
				result = true;
			} catch
			{

				result = false;
			}
			return result;

		}

		/// <summary>
		/// IsNumeric - test if passed string is numeric
		/// </summary>
		/// <param name="stringToTest"></param>
		/// <returns></returns>
        public static bool IsNumeric( string stringToTest )
		{
			double newVal;
			bool result = false;
			try
			{
				result = double.TryParse( stringToTest, NumberStyles.Any,
					NumberFormatInfo.InvariantInfo, out newVal );
			} catch
			{

				result = false;
			}
			return result;

		}

        /// <summary>
        /// IsValidRowId - test if passed string is a valid guid
        /// </summary>
        /// <param name="rowId"></param>
        /// <returns></returns>
        public static bool IsValidRowId( string rowId )
        {
            if ( rowId == null
                || rowId.Trim() == ""
                || rowId.Trim().Length != 36
                || rowId.ToString() == DEFAULT_GUID )
            {
                return false;
            }
            else
            {
                return true;
            }
        }
		#endregion
        /// <summary>
        /// Search for a control within a starting control - typically the current Page
        /// </summary>
        /// <param name="startingControl"></param>
        /// <param name="controlId"></param>
        /// <returns></returns>
        public static Control FindChildControl( Control startingControl, string controlId )
        {
            try
            {
                if ( startingControl != null )
                {
                    Control foundControl;
                    foundControl = startingControl.FindControl( controlId );
                    if ( foundControl == null && startingControl.ID == controlId )
                    {
                        foundControl = startingControl;
                    }
                    if ( foundControl != null )
                    {
                        return foundControl;
                    }

                    foreach ( Control c in startingControl.Controls )
                    {
                        foundControl = FindChildControl( c, controlId );
                        if ( foundControl != null )
                        {
                            return foundControl;
                        }
                    }
                }

            }
            catch
            {
                return null;
            }
            return null;
        }
	}//class

	public class FormMessage
	{

		public string Text { get; set; }
		public string Title { get; set; }
		public string CssClass { get; set; }
		public bool ShowPopup { get; set; }
		public bool IsFormatted { get; set; }

	}
}

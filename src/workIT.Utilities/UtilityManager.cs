using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Resources;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace workIT.Utilities
{
    public class UtilityManager
    {
        const string thisClassName = "UtilityManager";


        /// <summary>
        /// Default constructor for UtilityManager
        /// </summary>
        public UtilityManager()
        {
            //
            // TODO: Add constructor logic here
            //
        }


        #region === Application Keys Methods ===

        /// <summary>
        /// Gets the value of an application key from web.config. Returns blanks if not found
        /// </summary>
        /// <remarks>This property is explicitly thread safe.</remarks>
        public static string GetAppKeyValue( string keyName )
        {

            return GetAppKeyValue( keyName, "" );
        } //

        /// <summary>
        /// Gets the value of an application key from web.config. Returns the default value if not found
        /// </summary>
        /// <remarks>This property is explicitly thread safe.</remarks>
        public static string GetAppKeyValue( string keyName, string defaultValue )
        {
            string appValue = "";

            try
            {
                appValue = System.Configuration.ConfigurationManager.AppSettings[ keyName ];
                if ( appValue == null )
                    appValue = defaultValue;
            }
            catch
            {
                appValue = defaultValue;
				if ( HasMessageBeenPreviouslySent( keyName ) == false )
					LoggingHelper.LogError( string.Format( "@@@@ Error on appKey: {0},  using default of: {1}", keyName, defaultValue ) );
            }

            return appValue;
        } //
        public static int GetAppKeyValue( string keyName, int defaultValue )
        {
            int appValue = -1;

            try
            {
                appValue = Int32.Parse( System.Configuration.ConfigurationManager.AppSettings[ keyName ] );

                // If we get here, then number is an integer, otherwise we will use the default
            }
            catch
            {
                appValue = defaultValue;
				if ( HasMessageBeenPreviouslySent( keyName ) == false )
					LoggingHelper.LogError( string.Format( "@@@@ Error on appKey: {0},  using default of: {1}", keyName, defaultValue ) );
            }

            return appValue;
        } //
        public static bool GetAppKeyValue( string keyName, bool defaultValue )
        {
            bool appValue = false;

            try
            {
                appValue = bool.Parse( System.Configuration.ConfigurationManager.AppSettings[ keyName ] );
            }
            catch (Exception ex)
            {
                appValue = defaultValue;
				if ( HasMessageBeenPreviouslySent( keyName ) == false )
					LoggingHelper.LogError( string.Format( "@@@@ Error on appKey: {0},  using default of: {1}", keyName, defaultValue ) );
            }

            return appValue;
        } //
		public static bool HasMessageBeenPreviouslySent( string keyName )
		{

			string key = "appkey_" + keyName;
			//check cache for keyName
			if ( HttpRuntime.Cache[ key ] != null )
			{
				return true;
			}
			else
			{
				//not really much to store
				HttpRuntime.Cache.Insert( key, keyName );
			}

			return false;
		}
		#endregion

		#region === Security related Methods ===

		/// <summary>
		/// Encrypt the text using MD5 crypto service
		/// This is used for one way encryption of a user password - it can't be decrypted
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static string Encrypt( string data )
        {
            byte[] byDataToHash = ( new UnicodeEncoding() ).GetBytes( data );
            byte[] bytHashValue = new MD5CryptoServiceProvider().ComputeHash( byDataToHash );
            return BitConverter.ToString( bytHashValue );
        }

        /// <summary>
        /// Encrypt the text using the provided password (key) and CBC CipherMode
        /// </summary>
        /// <param name="text"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static string Encrypt_CBC( string text, string password )
        {
            RijndaelManaged rijndaelCipher = new RijndaelManaged();

            rijndaelCipher.Mode = CipherMode.CBC;
            rijndaelCipher.Padding = PaddingMode.PKCS7;
            rijndaelCipher.KeySize = 128;
            rijndaelCipher.BlockSize = 128;

            byte[] pwdBytes = System.Text.Encoding.UTF8.GetBytes( password );

            byte[] keyBytes = new byte[ 16 ];

            int len = pwdBytes.Length;

            if ( len > keyBytes.Length ) len = keyBytes.Length;

            System.Array.Copy( pwdBytes, keyBytes, len );

            rijndaelCipher.Key = keyBytes;
            rijndaelCipher.IV = keyBytes;

            ICryptoTransform transform = rijndaelCipher.CreateEncryptor();

            byte[] plainText = Encoding.UTF8.GetBytes( text );

            byte[] cipherBytes = transform.TransformFinalBlock( plainText, 0, plainText.Length );

            return Convert.ToBase64String( cipherBytes );

        }

        /// <summary>
        /// Decrypt the text using the provided password (key) and CBC CipherMode
        /// </summary>
        /// <param name="text"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static string Decrypt_CBC( string text, string password )
        {
            RijndaelManaged rijndaelCipher = new RijndaelManaged();

            rijndaelCipher.Mode = CipherMode.CBC;
            rijndaelCipher.Padding = PaddingMode.PKCS7;
            rijndaelCipher.KeySize = 128;
            rijndaelCipher.BlockSize = 128;

            byte[] encryptedData = Convert.FromBase64String( text );

            byte[] pwdBytes = System.Text.Encoding.UTF8.GetBytes( password );

            byte[] keyBytes = new byte[ 16 ];

            int len = pwdBytes.Length;

            if ( len > keyBytes.Length ) len = keyBytes.Length;

            System.Array.Copy( pwdBytes, keyBytes, len );

            rijndaelCipher.Key = keyBytes;
            rijndaelCipher.IV = keyBytes;

            ICryptoTransform transform = rijndaelCipher.CreateDecryptor();

            byte[] plainText = transform.TransformFinalBlock( encryptedData, 0, encryptedData.Length );

            return Encoding.UTF8.GetString( plainText );

        }

        /// <summary>
        /// Encode a passed URL while first checking if already encoded
        /// </summary>
        /// <param name="url">A web Address</param>
        /// <returns>Encoded URL</returns>
        public static string EncodeUrl( string url )
        {
            string encodedUrl = "";

            if ( url.Length > 0 )
            {
                //check if already encoded

                if ( url.ToLower().IndexOf( "%3a" ) > -1
                    //|| url.ToLower().IndexOf( "&amp;" ) > -1
                )
                {
                    encodedUrl = url;
                }
                else
                {
                    encodedUrl = HttpUtility.UrlEncode( url );
                    //fix potential encode errors:
                    encodedUrl = encodedUrl.Replace( "%26amp%3b", "%26" );
                }
            }

            return encodedUrl;
        }

        #endregion

        #region === Path related Methods ===
       
        /// <summary>
        /// FormatAbsoluteUrl an absolute URL - equivalent to Url.Content()
        /// </summary>
        /// <param name="path"></param>
        /// <param name="uriScheme"></param>
        /// <returns></returns>
        public static string FormatAbsoluteUrl( string path, string uriScheme = null )
        {
            uriScheme = uriScheme ?? HttpContext.Current.Request.Url.Scheme; //allow overriding http or https

            var environment = System.Configuration.ConfigurationManager.AppSettings[ "envType" ]; //Use port number only on localhost because https redirecting to a port on production screws this up
            var host = environment == "dev" ? HttpContext.Current.Request.Url.Authority : HttpContext.Current.Request.Url.Host;

            return uriScheme + "://" +
                ( host + "/" + HttpContext.Current.Request.ApplicationPath + path.Replace( "~/", "/" ) )
                .Replace( "///", "/" )
                .Replace( "//", "/" );
        }
        /// <summary>
        /// Format a relative, internal URL as a full URL, with http or https depending on the environment. 
        /// Determines the current host and then calls overloaded method to complete the formatting
        /// </summary>
        /// <param name="relativeUrl">Internal URL, usually beginning with /vos_portal/</param>
        /// <param name="isSecure">If the URL is to be formatted as a secure URL, set this value to true.</param>
        /// <returns>Formatted URL</returns>
        public static string FormatAbsoluteUrl( string relativeUrl, bool isSecure )
        {
            string host = "";
            try
            {
                //14-10-10 mp - change to explicit value from web.config
                host = GetAppKeyValue( "siteHostName" );
                if ( host == "" )
                {
                    // doing it this way so as to not break anything - HttpContext doesn't exist in a WCF web service
                    // so if this doesn't work we go get the FQDN another way.
                    host = HttpContext.Current.Request.ServerVariables[ "HTTP_HOST" ];
                    //need to handle ports!!
                }
            }
            catch ( Exception ex )
            {
                host = Dns.GetHostEntry( "localhost" ).HostName;
                // Fix up name with www instead of webX
                Regex hostEx = new Regex( @"web.?" );
                Match match = hostEx.Match( host );
                if ( match.Index > -1 )
                {
                    if (match.Value.Length > 0)
                        host = host.Replace( match.Value, "www" );
                }
            }

            return FormatAbsoluteUrl( relativeUrl, host, isSecure );
        }

        /// <summary>
        /// Format a relative, internal URL as a full URL, with http or https depending on the environment.
        /// </summary>
        /// <param name="relativeUrl">Internal URL, usually beginning with /vos_portal/</param>
        /// <param name="host">name of host (e.g. localhost, edit.illinoisworknet.com, www.illinoisworknet.com)</param>
        /// <param name="isSecure">If the URL is to be formatted as a secure URL, set this value to true.</param>
        /// <returns>Formatted URL</returns>
        public static string FormatAbsoluteUrl( string relativeUrl, string host, bool isSecure )
        {
            string url = "";
            if ( string.IsNullOrEmpty( relativeUrl ) )
                return "";
            if ( string.IsNullOrEmpty( host ) )
                return "";
            //ensure not already an absolute
            if ( relativeUrl.ToLower().StartsWith( "http" ) )
                return relativeUrl;
			//
			if ( isSecure && GetAppKeyValue( "usingSSL", false ))
            {
                url = "https://" + host + relativeUrl;
            }
            else
            {
                url = "http://" + host + relativeUrl;
            }
            return url;
        }
        /// <summary>
        /// get current url, including query parameters
        /// </summary>
        /// <returns></returns>
        public static string GetCurrentUrl()
        {
            string url = GetPublicUrl( HttpContext.Current.Request.QueryString.ToString() );

            //url = "http://" + HttpContext.Current.Request.ServerVariables[ "HTTP_HOST" ] +  url ;

            url = HttpUtility.UrlDecode( url );

            return url;
        }//

        /// <summary>
        /// Return the public version of the current MCMS url - removes MCMS specific parameters
        /// </summary>
        public static string GetPublicUrl( string url )
        {
            string publicUrl = "";

            //just take everything??
            publicUrl = url;
          
            publicUrl = publicUrl.Replace( "%2f", "/" );
            publicUrl = publicUrl.Replace( "%2e", "." );
            publicUrl = publicUrl.Replace( "%3a", ":" );
            return publicUrl;
        } //


        #endregion

        /// <summary>
        /// Format a title (such as for a library) to be url friendly
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        public static string UrlFriendlyTitle( string title )
        {
            if ( title == null || title.Trim().Length == 0 )
                return "";

            title = title.Trim();

            string encodedTitle = title.Replace( " - ", "-" );
            encodedTitle = encodedTitle.Replace( " ", "_" );
            //encodedTitle = encodedTitle.Replace( ".", "-" );
            encodedTitle = encodedTitle.Replace( "'", "" );
            encodedTitle = encodedTitle.Replace( "&", "-" );
            encodedTitle = encodedTitle.Replace( "#", "" );
            encodedTitle = encodedTitle.Replace( "$", "S" );
            encodedTitle = encodedTitle.Replace( "%", "percent" );
            encodedTitle = encodedTitle.Replace( "^", "" );
            encodedTitle = encodedTitle.Replace( "*", "" );
            encodedTitle = encodedTitle.Replace( "+", "_" );
            encodedTitle = encodedTitle.Replace( "~", "_" );
            encodedTitle = encodedTitle.Replace( "`", "_" );
            encodedTitle = encodedTitle.Replace( ":", "-" );
            encodedTitle = encodedTitle.Replace( ";", "" );
            encodedTitle = encodedTitle.Replace( "?", "" );
            encodedTitle = encodedTitle.Replace( "\"", "_" );
            encodedTitle = encodedTitle.Replace( "\\", "_" );
            encodedTitle = encodedTitle.Replace( "<", "_" );
            encodedTitle = encodedTitle.Replace( ">", "_" );
            encodedTitle = encodedTitle.Replace( "__", "_" );
            encodedTitle = encodedTitle.Replace( "__", "_" );

            if ( encodedTitle.EndsWith( "." ) )
                encodedTitle = encodedTitle.Substring( 0, encodedTitle.Length - 1 );

            return encodedTitle;
        } //

		#region string helpers
		/// <summary>
		/// Retrieve a string item from the current cache
		/// - assumes a default value of blank
		/// </summary>
		/// <param name="cacheKeyName"></param>
		/// <returns></returns>
		public static string GetCacheItem( string cacheKeyName )
        {
            string defaultValue = "";
            return GetCacheItem( cacheKeyName, defaultValue );

        }//
        /// <summary>
        /// Retrieve a string item from the current cache
        /// </summary>
        /// <param name="cacheKeyName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static string GetCacheItem( string cacheKeyName, string defaultValue )
        {
            string cacheItem = defaultValue;
            try
            {
                cacheItem = HttpContext.Current.Cache[ cacheKeyName ] as string;

                if ( string.IsNullOrEmpty( cacheItem ) )
                {
                    //assuming keyname is same as file name in app_Data - or should the ext also be part of the key?
                    string dataLoc = String.Format( "~/App_Data/{0}.txt", cacheKeyName );
                    string file = System.Web.HttpContext.Current.Server.MapPath( dataLoc );

                    cacheItem = File.ReadAllText( file );
                    //save in cache for future
                    HttpContext.Current.Cache[ cacheKeyName ] = cacheItem;
                }
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, thisClassName + ".GetCacheItem( string cacheKeyName, string defaultValue ). Error retrieving item key: " + cacheKeyName );
                cacheItem = defaultValue;
            }
            return cacheItem;
        }//

		/// <summary>
		/// extract value of a particular named parameter from passed string (Assumes and equal sign is used)
		/// ex: for string:		
		///			string searchString = "key1=value1;key2=value2;key3=value3;";
		/// To retrieve the value for key2 use:
		///			value = ExtractNameValue( searchString, "key2", ";");
		/// </summary>
		/// <param name="sourceString">String to search</param>
		/// <param name="name">Name of "parameter" in string</param>
		/// <param name="endDelimiter">End Delimeter. A character used to indicate the end of value in the string (often a semi-colon)</param>
		/// <returns>The value associated with the passed name</returns>
		public static string ExtractNameValue( string sourceString, string name, string endDelimiter )
		{
			string assignDelimiter = "=";

			return ExtractNameValue( sourceString, name, assignDelimiter, endDelimiter );
		}//

		/// <summary>
		/// extract value of a particular named parameter from passed string. The assign delimiter
		/// ex: for string:		
		///			string radioButtonId = "Radio_q_4_c_15_";
		/// To retrieve the value for question # use:
		///			qNbr = ExtractNameValue( radioButtonId, "q", "_", "_");
		/// To retrieve the value for choiceId use:
		///			choiceId = ExtractNameValue( radioButtonId, "c", "_", "_");
		/// </summary>
		/// <param name="sourceString">String to search</param>
		/// <param name="name">Name of "parameter" in string</param>
		/// <param name="assignDelimiter">Assigned delimiter. Typically an equal sign (=), but could be any defining character</param>
		/// <param name="endDelimiter">End Delimeter. A character used to indicate the end of value in the string (often a semi-colon)</param>
		/// <returns></returns>
		public static string ExtractNameValue( string sourceString, string name, string assignDelimiter, string endDelimiter )
		{
			int pos = sourceString.IndexOf( name + assignDelimiter );

			if ( pos == -1 )
				return "";

			string value = sourceString.Substring( pos + name.Length + 1 );
			int pos2 = value.IndexOf( endDelimiter );
			if ( pos2 > -1 )
				value = value.Substring( 0, pos2 );

			return value;
		}//

		#endregion


    }
}

using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Resources;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Runtime.Caching;
using System.Net.Http;
using System.Web.Configuration;
//using System.CodeDom;
using System.Linq;

using workIT.Models.Common;

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

        //Auto-convert one class to another based on matching properties by name and type
        public static T SimpleMap<T>( object input ) where T : new()
        {
            if ( input == null )
            {
                return default( T );
            }

            var result = new T();
            SimpleUpdate( input, result, true );

            return result;
        }
        //Auto-map properties whose name and type matches
        //Useful for converting between database models and project models
        public static void SimpleUpdate( object source, object destination, bool allowOverwritingSkippableValues = false )
        {
            if ( source == null || destination == null )
            {
                return;
            }

            //Get the properties, and any skippable properties
            var sourceProperties = source.GetType().GetProperties();
            var destinationProperties = destination.GetType().GetProperties();
            var skippableSourceProperties = CoreObject.GetSkippableProperties( source );

            //Do the mapping
            foreach ( var prop in destinationProperties )
            {
                try
                {
                    //Find the matching source property
                    var match = sourceProperties.FirstOrDefault( m => m.Name.ToLower() == prop.Name.ToLower() );
                    if ( match != null )
                    {
                        //If we aren't allowing skippable values (e.g. Id, RowId, Created, etc.) to be overwritten on update, and
                        //if the source property has an UpdateAttribute with a SkipPropertyOnUpdateValue of "true", skip the property
                        if ( !allowOverwritingSkippableValues && skippableSourceProperties.Contains( match ) )
                        {
                            continue;
                        }

                        //Attempt to map embedded objects if they can't be directly mapped...
                        if ( prop.PropertyType.IsClass && prop.PropertyType != match.PropertyType )
                        {
                            var item = match.GetValue( source );
                            var holder = prop.GetValue( destination );
                            SimpleUpdate( item, holder );
                        }
                        //Otherwise, attempt to map property directly
                        else
                        {
                            prop.SetValue( destination, match.GetValue( source, null ), null );
                        }
                    }
                }
                catch ( Exception ex ) { }
            }
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
                {
                    //HACK - had changed to use environment, but this affects any partners that had already downloaded the import program
                    if ( keyName == "environment" )
                    {
                        keyName = "envType";
                        appValue = System.Configuration.ConfigurationManager.AppSettings[keyName];
                    }
                
                    if ( appValue == null )
                        appValue = defaultValue;
                }
            }
            catch
            {
                //HACK - had changed to use environment, but this affects any partners that had already downloaded the import program
                if ( keyName == "environment" )
                {
                    return GetAppKeyValue( "envType", "" );
                }
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
        public static decimal GetAppKeyValue(string keyName, decimal defaultValue)
        {
            decimal appValue = -1;

            try
            {
                var dv = GetAppKeyValue( keyName, "" );
                appValue = decimal.Parse( dv );

                // If we get here, then number is an decimal, otherwise we will use the default
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

        public static string GetConnectionStringValue( string connectionName = "workIT_RO" )
        {
            string connString = "";

            try
            {
                connString = WebConfigurationManager.ConnectionStrings[connectionName].ConnectionString;
                //error
                if ( connString == null )
                {
                    throw new Exception(String.Format("There requested connection string is missing for: '{0}.'", connectionName) );
                }
            }
            catch
            {
                throw new Exception( String.Format( "There requested connection string is missing for: '{0}.'", connectionName ) );
            }

            return connString;
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

		/// <summary>
		/// Generate an MD5 hash of a string
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public static string GenerateMD5String( string input, bool returnAsLowerCase = true )
		{
			// Use input string to calculate MD5 hash
			using ( System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create() )
			{
				byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes( input );
				byte[] hashBytes = md5.ComputeHash( inputBytes );

				// Convert the byte array to hexadecimal string
				StringBuilder sb = new StringBuilder();
				for ( int i = 0; i < hashBytes.Length; i++ )
				{
					sb.Append( hashBytes[ i ].ToString( "X2" ) );
				}
				if ( returnAsLowerCase )
					return sb.ToString().ToLower();
				else
					return sb.ToString();
			}
		}
        #endregion

        #region === Path related Methods ===

        /// <summary>
        /// FormatAbsoluteUrl an absolute URL - equivalent to Url.Content()
        /// </summary>
        /// <param name="path"></param>
        /// <param name="uriScheme">http/https VERY unlikely call will 'know' what to provide</param>
        /// <returns></returns>
        public static string FormatAbsoluteUrl( string path )
        {
            var url = "";
            var uriScheme = "";
            bool isSecure = UtilityManager.GetAppKeyValue( "usingSSL", true );
            try
            {
                //need to handle where called from batch!!!!. Maybe just default to https?
                if ( HttpContext.Current != null )
                {
                    uriScheme = HttpContext.Current.Request.Url.Scheme;
                    var environment = System.Configuration.ConfigurationManager.AppSettings["environment"]; //Use port number only on localhost because https redirecting to a port on production screws this up
                    var host = environment == "development" ? HttpContext.Current.Request.Url.Authority : HttpContext.Current.Request.Url.Host;
                    return uriScheme + "://" +
                            ( host + "/" + HttpContext.Current.Request.ApplicationPath + path.Replace( "~/", "/" ) )
                            .Replace( "///", "/" )
                            .Replace( "//", "/" );
                }
                else
                {
                    var domainName = GetAppKeyValue( "oldCredentialFinderSite" );
                    url = FormatAbsoluteUrl( path, domainName, isSecure );
                }
            } catch
            {
                var domainName = GetAppKeyValue( "oldCredentialFinderSite" );
                url = FormatAbsoluteUrl( path, domainName, isSecure );
            }

            return url;

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
                host = GetAppKeyValue( "oldCredentialFinderSite" );
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
            LoggingHelper.DoTrace(7, string.Format( "FormatAbsoluteUrl start: relativeUrl:{0}, host:{1}", relativeUrl, host ) );
            string url = "";
            if ( string.IsNullOrEmpty( relativeUrl ) || relativeUrl == "~/" || relativeUrl == "/" )
                relativeUrl="";
            //ensure not already an absolute
            if ( relativeUrl.ToLower().StartsWith( "http" ) )
                return relativeUrl;

            relativeUrl = relativeUrl.Replace( "~/", "/" );
            if ( relativeUrl=="/" )
                relativeUrl = "";


            if ( string.IsNullOrEmpty( host ) )
                return "";

            if ( host.ToLower().StartsWith( "http" ) || host.ToLower().StartsWith( "//" ) )
            {
                //handle where host ends in '/' and relativeURL begins with '/'
                if ( host.EndsWith( "/" ) && relativeUrl.StartsWith( "/" ) )
                    relativeUrl = relativeUrl.TrimStart( '/' );
                //
                url = ( host + relativeUrl );

                LoggingHelper.DoTrace( 5, string.Format( "FormatAbsoluteUrl formatted: Url:{0}", url ) );

                return url;
            }
            //caller will not know if secure or not!
            if ( isSecure )
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
        //public static string GetCurrentUrl()
        //{
        //    string url = GetPublicUrl( HttpContext.Current.Request.QueryString.ToString() );

        //    //url = "http://" + HttpContext.Current.Request.ServerVariables[ "HTTP_HOST" ] +  url ;

        //    url = HttpUtility.UrlDecode( url );

        //    return url;
        //}//


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
        public static string ExtractDelimitedValue( string sourceString, string beginDelimiter = "(", string endDelimiter = ")")
        {
            int pos = sourceString.IndexOf( beginDelimiter );

            if ( pos == -1 )
                return "";

            string value = sourceString.Substring( pos + 1 );
            int pos2 = value.IndexOf( endDelimiter );
            if ( pos2 > -1 )
                value = value.Substring( 0, pos2 );

            return value.Trim();
        }//
        #endregion

        #region HTTP Request Helpers

        public static URLResult GetHttpData( string url, bool getFromCacheIfPossible = true, bool addToCache = true, int cacheMinutes = 30 )
		{
			var data = ( URLResult ) MemoryCache.Default[ url ];
			if ( data == null || !getFromCacheIfPossible )
			{
				data = new URLResult();
				try
				{
					System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
					var rawResult = new HttpClient().GetAsync( url ).Result;

					data.IsSuccessStatusCode = rawResult.IsSuccessStatusCode;
					data.ReasonPhrase = rawResult.ReasonPhrase ?? "";
					data.StatusCode = rawResult.StatusCode.ToString();

					if ( rawResult.IsSuccessStatusCode )
					{
						data.Content = rawResult.Content.ReadAsStringAsync().Result;
					}

					if ( addToCache )
					{
						MemoryCache.Default.Remove( url );
						MemoryCache.Default.Add( url, data, DateTime.Now.AddMinutes( cacheMinutes ) );
					}
				}
				catch ( Exception ex )
				{
					return new URLResult()
					{
						Content = null,
						IsSuccessStatusCode = false,
						ReasonPhrase = ex.Message,
						StatusCode = "999"
					};
				}
			}
			return data;
		}

		[Serializable]
		public class URLResult
		{
			public string Content { get; set; }
			public bool IsSuccessStatusCode { get; set; }
			public string ReasonPhrase { get; set; }
			public string StatusCode { get; set; }
		}
		//

		#endregion
	}
}

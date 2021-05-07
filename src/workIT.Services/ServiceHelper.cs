using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.SessionState;

using workIT.Utilities;
using MC = workIT.Models.Common;
using MD = workIT.Models.API;
using ME = workIT.Models.Elastic;
using MPM = workIT.Models.ProfileModels;
using MSR = workIT.Models.Search;


namespace workIT.Services
{
	public class ServiceHelper
	{
		static string DEFAULT_GUID = "00000000-0000-0000-0000-000000000000";

		//
		/// <summary>
		/// Session variable for message to display in the system console
		/// </summary>
		public const string SYSTEM_CONSOLE_MESSAGE = "SystemConsoleMessage";

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

			if ( len > keyBytes.Length )
				len = keyBytes.Length;

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

			if ( len > keyBytes.Length )
				len = keyBytes.Length;

			System.Array.Copy( pwdBytes, keyBytes, len );

			rijndaelCipher.Key = keyBytes;
			rijndaelCipher.IV = keyBytes;

			ICryptoTransform transform = rijndaelCipher.CreateDecryptor();

			byte[] plainText = transform.TransformFinalBlock( encryptedData, 0, encryptedData.Length );

			return Encoding.UTF8.GetString( plainText );

		}

		#endregion

		
		#region Helpers and validaton
		public static bool IsLocalHost()
		{
			return IsTestEnv();
		}
		public static bool IsTestEnv()
		{
			string host = HttpContext.Current.Request.Url.Host.ToString();
			return ( host.Contains( "localhost" ) || host.Contains( "209.175.164.200" ) );
		}
		public static int StringToInt( string value, int defaultValue )
		{
			int returnValue = defaultValue;
			if ( Int32.TryParse( value, out returnValue ) == true )
				return returnValue;
			else
				return defaultValue;
		}


		public static bool StringToDate( string value, ref DateTime returnValue )
		{
			if ( System.DateTime.TryParse( value, out returnValue ) == true )
				return true;
			else
				return false;
		}

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
			}
			catch
			{

				result = false;
			}
			return result;

		}


		/// <summary>
		/// IsDate - test if passed string is a valid date
		/// </summary>
		/// <param name="stringToTest"></param>
		/// <returns></returns>
		public static bool IsDate( string stringToTest, bool doingReasonableCheck = true )
		{

			DateTime newDate;
			bool result = false;
			try
			{
				newDate = System.DateTime.Parse( stringToTest );
				result = true;
				//check if reasonable
				if ( doingReasonableCheck && newDate < new DateTime( 1900, 1, 1 ) )
					result = false;
			}
			catch
			{

				result = false;
			}
			return result;

		} //end
        public static bool IsValidCtid( string ctid, ref List<string> messages, bool isRequired = false, bool skippingErrorMessages = true )
        {
            bool isValid = true;

            if ( string.IsNullOrWhiteSpace( ctid ) )
            {
                if ( isRequired )
                {
                    messages.Add( "Error - A valid CTID property must be entered." );
                }
                return false;
            }

            ctid = ctid.ToLower();
            if ( ctid.Length != 39 )
            {
                if ( !skippingErrorMessages )
                    messages.Add( "Error - Invalid CTID format. The proper format is ce-UUID. ex. ce-84365AEA-57A5-4B5A-8C1C-EAE95D7A8C9B" );
                return false;
            }

            if ( !ctid.StartsWith( "ce-" ) )
            {
                if ( !skippingErrorMessages )
                    messages.Add( "Error - The CTID property must begin with ce-" );
                return false;
            }
            //now we have the proper length and format, the remainder must be a valid guid
            if ( !IsValidGuid( ctid.Substring( 3, 36 ) ) )
            {
                if ( !skippingErrorMessages )
                    messages.Add( "Error - Invalid CTID format. The proper format is ce-UUID. ex. ce-84365AEA-57A5-4B5A-8C1C-EAE95D7A8C9B" );
                return false;
            }

            return isValid;
        }

        public static bool IsValidGuid( Guid field )
		{
			if ( ( field == null || field == Guid.Empty ) )
				return false;
			else
				return true;
		}
		public static bool IsValidGuid( string field )
		{
			Guid guidOutput;
			if ( ( field == null || field.ToString() == DEFAULT_GUID ) )
				return false;
			else if ( !Guid.TryParse( field, out guidOutput ) )
				return false;
			else 
				return true;
		}
		/// <summary>
		/// Check if the passed dataset is indicated as one containing an error message (from a web service)
		/// </summary>
		/// <param name="wsDataset">DataSet for a web service method</param>
		/// <returns>True if dataset contains an error message, otherwise false</returns>
		public static bool HasErrorMessage( DataSet wsDataset )
		{

			if ( wsDataset.DataSetName == "ErrorMessage" )
				return true;
			else
				return false;

		} //

		/// <summary>
		/// Convert a comma-separated list (as a string) to a list of integers
		/// </summary>
		/// <param name="csl">A comma-separated list of integers</param>
		/// <returns>A List of integers. Returns an empty list on error.</returns>
		public static List<int> CommaSeparatedListToIntegerList( string csl )
		{
			try
			{
				return CommaSeparatedListToStringList( csl ).Select( int.Parse ).ToList();
			}
			catch
			{
				return new List<int>();
			}

		}

		/// <summary>
		/// Convert a comma-separated list (as a string) to a list of strings
		/// </summary>
		/// <param name="csl">A comma-separated list of strings</param>
		/// <returns>A List of strings. Returns an empty list on error.</returns>
		public static List<string> CommaSeparatedListToStringList( string csl )
		{
			try
			{
				return csl.Trim().Split( new string[] { "," }, StringSplitOptions.RemoveEmptyEntries ).ToList();
			}
			catch
			{
				return new List<string>();
			}
		}

		#endregion

		#region === Dataset helper Methods ===
		/// <summary>
		/// Check is dataset is valid and has at least one table with at least one row
		/// </summary>
		/// <param name="ds"></param>
		/// <returns></returns>
		public static bool DoesDataSetHaveRows( DataSet ds )
		{

			try
			{
				if ( ds != null && ds.Tables.Count > 0 && ds.Tables[ 0 ].Rows.Count > 0 )
					return true;
				else
					return false;
			}
			catch
			{

				return false;
			}
		}//

		/// <summary>
		/// Helper method to retrieve a string column from a row but will ignore missing columns (unlike GetRowColumn)
		/// </summary>
		/// <param name="row"></param>
		/// <param name="column"></param>
		/// <returns></returns>
		public static string GetRowPossibleColumn( DataRow row, string column )
		{
			string colValue = "";

			try
			{
				colValue = row[ column ].ToString();

			}
			catch ( System.FormatException fex )
			{
				//Assuming FormatException means null or invalid value, so can ignore
				colValue = "";

			}
			catch ( Exception ex )
			{
				//this method will ignore not found
				colValue = "";
			}
			return colValue;

		} // end method
		public static int GetRowPossibleColumn( DataRow row, string column, int defaultValue )
		{
			int colValue = 0;

			try
			{
				colValue = Int32.Parse( row[ column ].ToString() );

			}
			catch ( System.FormatException fex )
			{
				//Assuming FormatException means null or invalid value, so can ignore
				colValue = defaultValue;

			}
			catch ( Exception ex )
			{
				//this method will ignore not found
				colValue = defaultValue;
			}
			return colValue;

		} // end method
		/// <summary>
		/// Helper method to retrieve a string column from a row while handling invalid values
		/// </summary>
		/// <param name="row">DataRow</param>
		/// <param name="column">Column Name</param>
		/// <returns></returns>
		public static string GetRowColumn( DataRow row, string column )
		{
			return GetRowColumn( row, column, "" );
		} // end method

		/// <summary>
		/// Helper method to retrieve a string column from a row while handling invalid values
		/// </summary>
		/// <param name="row">DataRow</param>
		/// <param name="column">Column Name</param>
		/// <param name="defaultValue">Default value to return if column data is invalid</param>
		/// <returns></returns>
		public static string GetRowColumn( DataRow row, string column, string defaultValue )
		{
			string colValue = "";

			try
			{
				colValue = row[ column ].ToString();

			}
			catch ( System.FormatException fex )
			{
				//Assuming FormatException means null or invalid value, so can ignore
				colValue = defaultValue;

			}
			catch ( Exception ex )
			{
				if ( column.IndexOf( "CUSTOMER_STATUS" ) > -1 )
				{
					//ignore

				}
				else
				{

					string exType = ex.GetType().ToString();
					LoggingHelper.LogError( exType + " Exception in GetRowColumn( DataRow row, string column, string defaultValue ) for column: " + column + ". \r\n" + ex.Message.ToString(), true );
				}
				colValue = defaultValue;
			}
			return colValue;

		} // end method
		/// <summary>
		/// Helper method to retrieve an int column from a row while handling invalid values
		/// </summary>
		/// <param name="row">DataRow</param>
		/// <param name="column">Column Name</param>
		/// <param name="defaultValue">Default value to return if column data is invalid</param>
		/// <returns></returns>
		public static int GetRowColumn( DataRow row, string column, int defaultValue )
		{
			int colValue = 0;

			try
			{
				colValue = Int32.Parse( row[ column ].ToString() );

			}
			catch ( System.FormatException fex )
			{
				//Assuming FormatException means null or invalid value, so can ignore
				colValue = defaultValue;

			}
			catch ( Exception ex )
			{


				LoggingHelper.LogError( "Exception in GetRowColumn( DataRow row, string column, int defaultValue ) for column: " + column + ". \r\n" + ex.Message.ToString(), true );
				colValue = defaultValue;
				//throw ex;
			}
			return colValue;

		} // end method

		/// <summary>
		/// Helper method to retrieve a bool column from a row while handling invalid values
		/// </summary>
		/// <param name="row">DataRow</param>
		/// <param name="column">Column Name</param>
		/// <param name="defaultValue">Default value to return if column data is invalid</param>
		/// <returns></returns>
		public static bool GetRowColumn( DataRow row, string column, bool defaultValue )
		{
			bool colValue;

			try
			{
				colValue = Boolean.Parse( row[ column ].ToString() );

			}
			catch ( System.FormatException fex )
			{
				//Assuming FormatException means null or invalid value, so can ignore
				colValue = defaultValue;

			}
			catch ( Exception ex )
			{

				LoggingHelper.LogError( "Exception in GetRowColumn( DataRow row, string column, bool defaultValue ) for column: " + column + ". \r\n" + ex.Message.ToString(), true );
				colValue = defaultValue;
				//throw ex;
			}
			return colValue;

		} // end method

		/// <summary>
		/// Helper method to retrieve a DateTime column from a row while handling invalid values
		/// </summary>
		/// <param name="row">DataRow</param>
		/// <param name="column">Column Name</param>
		/// <param name="defaultValue">Default value to return if column data is invalid</param>
		/// <returns></returns>      
		public static System.DateTime GetRowColumn( DataRow row, string column, System.DateTime defaultValue )
		{
			System.DateTime colValue;

			try
			{
				colValue = System.DateTime.Parse( row[ column ].ToString() );
			}
			catch ( System.FormatException fex )
			{
				//Assuming FormatException means null or invalid value, so can ignore
				colValue = defaultValue;

			}
			catch ( Exception ex )
			{

				LoggingHelper.LogError( "Exception in GetRowColumn( DataRow row, string column, System.DateTime defaultValue ) for column: " + column + ". \r\n" + ex.Message.ToString(), true );
				colValue = defaultValue;
			}
			return colValue;

		} // end method


		/// <summary>
		/// Helper method to retrieve a column from a row while handling invalid values
		/// </summary>
		/// <param name="row">DataRow</param>
		/// <param name="column">Column Name</param>
		/// <param name="defaultValue">Default value to return if column data is invalid</param>
		/// <returns></returns>
		public static decimal GetRowColumn( DataRow row, string column, decimal defaultValue )
		{
			decimal colValue = 0;

			try
			{
				colValue = Convert.ToDecimal( row[ column ].ToString() );

			}
			catch ( System.FormatException fex )
			{
				//Assuming FormatException means null or invalid value, so can ignore
				colValue = defaultValue;

			}
			catch ( Exception ex )
			{

				LoggingHelper.LogError( "Exception in GetRowColumn( DataRow row, string column, decimal defaultValue ) for column: " + column + ". \r\n" + ex.Message.ToString(), true );
				colValue = defaultValue;
			}
			return colValue;

		} // end method

		public static string GetRowColumn( DataRowView row, string column )
		{
			return GetRowColumn( row, column, "" );
		} // end method

		/// <summary>
		/// Helper method to retrieve a string column from a row while handling invalid values
		/// </summary>
		/// <param name="row">DataRow</param>
		/// <param name="column">Column Name</param>
		/// <param name="defaultValue">Default value to return if column data is invalid</param>
		/// <returns></returns>
		public static string GetRowColumn( DataRowView row, string column, string defaultValue )
		{
			string colValue = "";

			try
			{
				colValue = row[ column ].ToString();

			}
			catch ( System.FormatException fex )
			{
				//Assuming FormatException means null or invalid value, so can ignore
				colValue = defaultValue;

			}
			catch ( Exception ex )
			{

				string exType = ex.GetType().ToString();
				LoggingHelper.LogError( exType + " Exception in GetRowColumn( DataRowView row, string column, string defaultValue ) for column: " + column + ". \r\n" + ex.Message.ToString(), true );
				colValue = defaultValue;
			}
			return colValue;

		} // end method

		/// <summary>
		/// Helper method to retrieve a string column from a row while handling invalid values
		/// </summary>
		/// <param name="row">DataRowView</param>
		/// <param name="column">Column Name</param>
		/// <param name="defaultValue">Default value to return if column data is invalid</param>
		/// <returns></returns>
		public static int GetRowColumn( DataRowView row, string column, int defaultValue )
		{
			int colValue = 0;

			try
			{
				colValue = Int32.Parse( row[ column ].ToString() );

			}
			catch ( System.FormatException fex )
			{
				//Assuming FormatException means null or invalid value, so can ignore
				colValue = defaultValue;

			}
			catch ( Exception ex )
			{

				LoggingHelper.LogError( "Exception in GetRowColumn( DataRowView row, string column, int defaultValue ) for column: " + column + ". \r\n" + ex.Message.ToString(), true );
				colValue = defaultValue;
				//throw ex;
			}
			return colValue;

		} // end method

		/// <summary>
		/// Helper method to retrieve a bool column from a row while handling invalid values
		/// </summary>
		/// <param name="row"></param>
		/// <param name="column"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public static bool GetRowColumn( DataRowView row, string column, bool defaultValue )
		{
			bool colValue;

			try
			{
				colValue = Boolean.Parse( row[ column ].ToString() );

			}
			catch ( System.FormatException fex )
			{
				//Assuming FormatException means null or invalid value, so can ignore
				colValue = defaultValue;

			}
			catch ( Exception ex )
			{

				LoggingHelper.LogError( "Exception in GetRowColumn( DataRowView row, string column, bool defaultValue ) for column: " + column + ". \r\n" + ex.Message.ToString(), true );
				colValue = defaultValue;
				//throw ex;
			}
			return colValue;

		} // end method

		/// <summary>
		/// Helper method to retrieve a column from a row while handling invalid values
		/// </summary>
		/// <param name="row">DataRowView</param>
		/// <param name="column">Column Name</param>
		/// <param name="defaultValue">Default value to return if column data is invalid</param>
		/// <returns></returns>
		public static decimal GetRowColumn( DataRowView row, string column, decimal defaultValue )
		{
			decimal colValue = 0;

			try
			{
				colValue = Convert.ToDecimal( row[ column ].ToString() );

			}
			catch ( System.FormatException fex )
			{
				//Assuming FormatException means null or invalid value, so can ignore
				colValue = defaultValue;

			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( "Exception in GetRowColumn( DataRowView row, string column, decimal defaultValue ) for column: " + column + ". \r\n" + ex.Message.ToString(), true );
				colValue = defaultValue;
			}
			return colValue;

		} // end method

		#endregion

		#region Common Utility Methods
		public static string HandleApostrophes( string strValue )
		{

			if ( strValue.IndexOf( "'" ) > -1 )
			{
				strValue = strValue.Replace( "'", "''" );
			}
			if ( strValue.IndexOf( "''''" ) > -1 )
			{
				strValue = strValue.Replace( "''''", "''" );
			}

			return strValue;
		}
		public static String CleanText( String text )
		{
			if ( String.IsNullOrEmpty( text.Trim() ) )
				return String.Empty;

			String output = String.Empty;
			try
			{
				String rxPattern = "<(?>\"[^\"]*\"|'[^']*'|[^'\">])*>";
				Regex rx = new Regex( rxPattern );
				output = rx.Replace( text, String.Empty );
				if ( output.ToLower().IndexOf( "<script" ) > -1
					|| output.ToLower().IndexOf( "javascript" ) > -1 )
				{
					output = "";
				}
			}
			catch ( Exception ex )
			{

			}

			return output;
		}
		/// <summary>
		/// Format a string item for a search string (where)
		/// </summary>
		/// <param name="sqlWhere"></param>
		/// <param name="colName"></param>
		/// <param name="colValue"></param>
		/// <param name="booleanOperator"></param>
		/// <returns></returns>
		public static string FormatSearchItem( string sqlWhere, string colName, string colValue, string booleanOperator )
		{
			string item = "";
			string boolean = " ";

			if ( colValue.Length == 0 )
				return "";

			if ( sqlWhere.Length > 0 )
			{
				boolean = " " + booleanOperator + " ";
			}
			//allow asterisks
			colValue = colValue.Replace( "*", "%" );

			if ( colValue.IndexOf( "%" ) > -1 )
			{
				item = boolean + " (" + colName + " like '" + colValue + "') ";
			}
			else
			{
				item = boolean + " (" + colName + " = '" + colValue + "') ";
			}

			return item;

		}	// End method

		/// <summary>
		/// Format an integer item for a search string (where)
		/// </summary>
		/// <param name="sqlWhere"></param>
		/// <param name="colName"></param>
		/// <param name="colValue"></param>
		/// <param name="booleanOperator"></param>
		/// <returns></returns>
		public static string FormatSearchItem( string sqlWhere, string colName, int colValue, string booleanOperator )
		{
			string item = "";
			string boolean = " ";

			if ( sqlWhere.Length > 0 )
			{
				boolean = " " + booleanOperator + " ";
			}

			item = boolean + " (" + colName + " = " + colValue + ") ";

			return item;

		}	// End method

		/// <summary>
		/// Format an item for a search string (where)
		/// </summary>
		/// <param name="sqlWhere"></param>
		/// <param name="filter"></param>
		/// <param name="booleanOperator"></param>
		/// <returns></returns>
		public static string FormatSearchItem( string sqlWhere, string filter, string booleanOperator )
		{
			string item = "";
			string boolean = " ";

			if ( filter.Trim().Length == 0 )
				return "";

			if ( sqlWhere.Length > 0 )
			{
				boolean = " " + booleanOperator + " ";
			}

			item = boolean + " (" + filter + ") ";

			return item;

		}	// End method

		#endregion


		#region HttpSessionState Methods
		public static void SessionSet( string key, System.Object sysObject )
		{
			if ( HttpContext.Current.Session != null )
			{
				SessionSet( HttpContext.Current.Session, key, sysObject );
			}

		} //
		/// <summary>
		/// Helper Session method - future use if required to chg to another session provider such as SQL Server 
		/// </summary>
		/// <param name="session"></param>
		/// <param name="key"></param>
		/// <param name="sysObject"></param>
		public static void SessionSet( HttpSessionState session, string key, System.Object sysObject )
		{

			session[ key ] = sysObject;

		} //
		/// <summary>
		/// Get a key from a session, default to blank if not found
		/// </summary>
		/// <param name="key">Key for session</param>
		/// <returns>string</returns>
		public static string SessionGet( string key )
		{
			if ( HttpContext.Current.Session != null )
			{
				return SessionGet( HttpContext.Current.Session, key, "" );
			}
			else
				return null;
		} //

		public static string SessionGet( string key, string defaultValue )
		{
			if ( HttpContext.Current.Session != null )
			{
				return SessionGet( HttpContext.Current.Session, key, defaultValue );
			}
			else
				return null;
		} //
		/// <summary>
		/// Get a key from a session, default to blank if not found
		/// </summary>
		/// <param name="session">HttpSessionState</param>
		/// <param name="key">Key for session</param>
		/// <returns>string</returns>
		public static string SessionGet( HttpSessionState session, string key, string defaultValue )
		{

			string value = "";
			try
			{
				if ( session[ key ] != null )
					value = session[ key ].ToString();
				else
					value = defaultValue;

			}
			catch ( Exception ex )
			{
				value = defaultValue;
			}


			return value;
		} //
		#endregion
	}
}

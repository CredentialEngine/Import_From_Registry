using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
//using System.Net.Http;
//using System.Net.Http.Headers;
using System.IO;
using Newtonsoft.Json;
using Import.Services;
//using CTI.Import.classes;

using workIT.Utilities;

namespace CTI.Import
{
	public class RegistryImport
	{

		public static int maxExceptions = UtilityManager.GetAppKeyValue( "maxExceptions", 500 );

		public static List<ReadEnvelope> GetLatest( string type, string startingDate, string endingDate, int pageNbr, int pageSize, ref int pTotalRows, ref string statusMessage )
		{
			string document = "";
			string filter = "";
			//includes the question mark
			string serviceUri = UtilityManager.GetAppKeyValue( "credentialRegistrySearch" );
			//from=2016-08-22T00:00:00&until=2016-08-31T23:59:59
			//resource_type=credential
			if ( !string.IsNullOrWhiteSpace( type ) )
				filter = string.Format( "resource_type={0}", type.ToLower() );

			SetPaging( pageNbr, pageSize, ref filter );
			SetDateFilters( startingDate, endingDate, ref filter );

			serviceUri += filter.Length > 0 ? filter : "";

			List<ReadEnvelope> list = new List<ReadEnvelope>();
			ReadEnvelope envelope = new ReadEnvelope();


			try
			{

				// Create a request for the URL.         
				WebRequest request = WebRequest.Create( serviceUri );

				// If required by the server, set the credentials.
				request.Credentials = CredentialCache.DefaultCredentials;

				//Get the response.
				HttpWebResponse response = ( HttpWebResponse ) request.GetResponse();

				// Get the stream containing content returned by the server.
				Stream dataStream = response.GetResponseStream();

				// Open the stream using a StreamReader for easy access.
				StreamReader reader = new StreamReader( dataStream );
				// Read the content.
				document = reader.ReadToEnd();

				// Cleanup the streams and the response.

				reader.Close();
				dataStream.Close();
				response.Close();

				//Link contains links for paging
				var hdr = response.GetResponseHeader( "Link" );
				Int32.TryParse( response.GetResponseHeader( "Total" ), out pTotalRows );

				//map to the default envelope
				list = JsonConvert.DeserializeObject<List<ReadEnvelope>>( document );

				
			}
			catch ( Exception exc )
			{
				LoggingHelper.LogError( exc, "RegistryServices.GetLatest" );
			}
			return list;
		}

		public static List<ReadEnvelope> GetDeleted( string type, string startingDate, string endingDate, int pageNbr, int pageSize, ref int pTotalRows, ref string statusMessage )
		{
			string document = "";
			string filter = "include_deleted=only";
			string serviceUri = UtilityManager.GetAppKeyValue( "credentialRegistrySearch" );
			//from=2016-08-22T00:00:00&until=2016-08-31T23:59:59
			//resource_type=credential
			if ( !string.IsNullOrWhiteSpace( type ) )
				filter += string.Format( "&resource_type={0}", type.ToLower() );

			SetPaging( pageNbr, pageSize, ref filter );
			SetDateFilters( startingDate, endingDate, ref filter );

			serviceUri += filter.Length > 0 ? filter : "";

			List<ReadEnvelope> list = new List<ReadEnvelope>();
			ReadEnvelope envelope = new ReadEnvelope();

			try
			{

				// Create a request for the URL.         
				WebRequest request = WebRequest.Create( serviceUri );

				// If required by the server, set the credentials.
				request.Credentials = CredentialCache.DefaultCredentials;

				//Get the response.
				HttpWebResponse response = ( HttpWebResponse ) request.GetResponse();

				// Get the stream containing content returned by the server.
				Stream dataStream = response.GetResponseStream();

				// Open the stream using a StreamReader for easy access.
				StreamReader reader = new StreamReader( dataStream );
				// Read the content.
				document = reader.ReadToEnd();

				// Cleanup the streams and the response.

				reader.Close();
				dataStream.Close();
				response.Close();

				//Link contains links for paging
				var hdr = response.GetResponseHeader( "Link" );
				Int32.TryParse( response.GetResponseHeader( "Total" ), out pTotalRows );

				//map to the default envelope
				list = JsonConvert.DeserializeObject<List<ReadEnvelope>>( document );

			}
			catch ( Exception exc )
			{
				LoggingHelper.LogError( exc, "RegistryServices.GetDeleted" );
			}
			return list;
		}
		private static void SetPaging( int pageNbr, int pageSize, ref string where )
		{
			string AND = "";
			if ( where.Length > 0 )
				AND = "&";

			if ( pageNbr > 0 )
			{
				where = where + AND + string.Format( "page={0}", pageNbr );
				AND = "&";
			}
			if ( pageSize > 0 )
			{
				where = where + AND + string.Format( "per_page={0}", pageSize );
				AND = "&";
			}
		}
		private static void SetSortOrder( ref string where )
		{
			string AND = "";
			if ( where.Length > 0 )
				AND = "&";
			where = where + AND + "sort_by=updated_at&sort_order=asc";
		}

		private static void SetDateFilters( string startingDate, string endingDate, ref string where )
		{
			string AND = "";
			if ( where.Length > 0 )
				AND = "&";

			string date = FormatDateFilter( startingDate );
			if ( !string.IsNullOrWhiteSpace( date ) )
			{
				where = where + AND + string.Format( "from={0}", startingDate );
				AND = "&";
			}

			date = FormatDateFilter( endingDate );
			if ( !string.IsNullOrWhiteSpace( date ) )
			{
				where = where + AND + string.Format( "until={0}", endingDate );
				AND = "&";
			}
			//if ( !string.IsNullOrWhiteSpace( endingDate ) && endingDate.Length == 10 )
			//{
			//	where = where + AND + string.Format( "until={0}T23:59:59", endingDate );
			//}
		}
		private static string FormatDateFilter( string date )
		{
			string formatedDate = "";
			if ( string.IsNullOrWhiteSpace( date ) )
				return "";

			//start by checking for just properly formatted date
			if ( !string.IsNullOrWhiteSpace( date ) && date.Length == 10 )
			{
				formatedDate = string.Format( "{0}T00:00:00", date );
			}
			else if ( !string.IsNullOrWhiteSpace( date ) )
			{
				//check if in proper format - perhaps with time provided
				if ( date.IndexOf( "T" ) > 8 )
				{
					formatedDate = string.Format( "{0}", date );
				}
				else
				{
					//not sure how to handle unexpected date except to ignore
					//might be better to send actual DateTime field
				}
			}

			return formatedDate;
		}
	}

	public class ImportMonitor
	{
		public ImportMonitor()
		{
			ImportSectionStatus = new List<string>();
			DownloadOnly = false;
		}

		public List<string> ImportSectionStatus { get; set; }

		public string StartingDate { get; set; }
		public string EndingDate { get; set; }
		public int MaxRecord { get; set; }

		public bool DownloadOnly { get; set; }
	}
}

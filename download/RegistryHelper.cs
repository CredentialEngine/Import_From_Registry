using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

using Download.Models;
using Download.Services;
using System.Net;
using System.IO;

namespace Download
{
	public class RegistryHelper
	{
		static string thisClassName = "RegistryImport";
		public RegistryHelper( string community )
		{
			Community = community;
		}
		public string Community { get; set; }

		public string Retrieve( string registryEntityType, int entityTypeId, string startingDate, string endingDate, int maxRecords, bool downloadOnly, ref int recordsImported, string sortOrder = "asc" )
		{

			bool importingThisType = UtilityManager.GetAppKeyValue( "importing_" + registryEntityType, true );
			if ( !importingThisType )
			{
				LoggingHelper.DoTrace( 1, string.Format( "===  *****************  Skipping import of {0}  ***************** ", registryEntityType ) );
				return "Skipped import of " + registryEntityType;
			}
			LoggingHelper.DoTrace( 1, string.Format( "===  *****************  Importing {0}  ***************** ", registryEntityType ) );
			//JsonEntity input = new JsonEntity();
			ReadEnvelope envelope = new ReadEnvelope();
			List<ReadEnvelope> list = new List<ReadEnvelope>();

			int pageNbr = 1;
			int pageSize = UtilityManager.GetAppKeyValue( "importPageSize", 100 );
			string importError = "";
			string importResults = "";
			string importNote = "";
			//ThisEntity output = new ThisEntity();
			List<string> messages = new List<string>();

			int cntr = 0;
			int pTotalRows = 0;

			int exceptionCtr = 0;
			string statusMessage = "";
			bool isComplete = false;
			bool importSuccessfull = true;
			int newImportId = 0;
			
			//will need to handle multiple calls - watch for time outs
			while ( pageNbr > 0 && !isComplete )
			{
				//19-09-22 chg to use RegistryServices to remove duplicate services
				list = Search( registryEntityType, startingDate, endingDate, pageNbr, pageSize, ref pTotalRows, ref statusMessage, Community, sortOrder );

				if ( list == null || list.Count == 0 )
				{
					isComplete = true;
					if ( pageNbr == 1 )
					{
						LoggingHelper.DoTrace( 4, registryEntityType + ": No records where found for date range. " );
					}
					break;
				}
				if ( pageNbr == 1 )
					LoggingHelper.DoTrace( 2, string.Format( "Import {0} Found {1} records to process.", registryEntityType, pTotalRows ) );

				foreach ( ReadEnvelope item in list )
				{
					cntr++;

					string envelopeIdentifier = item.EnvelopeIdentifier;
					string ctid = item.EnvelopeCetermsCtid;
					string payload = item.DecodedResource.ToString();
					LoggingHelper.DoTrace( 2, string.Format( "{0}. {1} ctid {2} ", cntr, registryEntityType, ctid ) );

					string ctdlType = RegistryServices.GetResourceType( payload );
					string envelopeUrl = RegistryHelper.GetEnvelopeUrl( envelopeIdentifier );

					//LoggingHelper.DoTrace( 5, "		envelopeUrl: " + envelopeUrl );
					//to overwrite an existing file, suppress the date prefix (" "), or use an alternate prefix
					LoggingHelper.WriteLogFile( 1, registryEntityType + "_" + ctid, payload, "", false );

					if ( maxRecords > 0 && cntr >= maxRecords )
					{
						break;
					}
				} //end foreach 

				pageNbr++;
				if ( ( maxRecords > 0 && cntr >= maxRecords ) )
				{
					isComplete = true;
					LoggingHelper.DoTrace( 2, string.Format( "Import {2} EARLY EXIT. Completed {0} records out of a total of {1} for {2} ", cntr, pTotalRows, registryEntityType ) );

				}
				else if ( cntr >= pTotalRows )
				{
					isComplete = true;
				}
			}
			importResults = string.Format( "Import {0} - Processed {1} records, with {2} exceptions. \r\n", registryEntityType, cntr, exceptionCtr );
			LoggingHelper.DoTrace( 2, importResults );
			if ( !string.IsNullOrWhiteSpace( importNote ) )
				importResults += importNote;

			recordsImported += cntr;

			return importResults;
		}

		/// <summary>
		/// Get list of deleted records for the requested time period.
		/// </summary>
		/// <param name="community"></param>
		/// <param name="type"></param>
		/// <param name="startingDate">Date must be in UTC</param>
		/// <param name="endingDate">Date must be in UTC</param>
		/// <param name="pageNbr"></param>
		/// <param name="pageSize"></param>
		/// <param name="pTotalRows"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public static List<ReadEnvelope> GetDeleted( string community, string type, string startingDate, string endingDate, int pageNbr, int pageSize, ref int pTotalRows, ref string statusMessage )
		{
			string document = "";
			string filter = "include_deleted=only";
			string serviceUri = RegistryHelper.GetRegistrySearchUrl( community );
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
				HttpWebResponse response = ( HttpWebResponse )request.GetResponse();
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
		public static string DisplayMessages( string message )
		{
			LoggingHelper.DoTrace( 1, message );
			//Console.WriteLine( message );

			return message;
		}
		#region Registry search
		public static List<ReadEnvelope> Search( string type, string startingDate, string endingDate, int pageNbr, int pageSize, ref int pTotalRows, ref string statusMessage, string community, string sortOrder = "asc" )
		{
			string credentialEngineAPIKey = UtilityManager.GetAppKeyValue( "MyCredentialEngineAPIKey" );
			string document = "";
			string filter = "";
			//includes the question mark
			string serviceUri = GetRegistrySearchUrl( community );
			//from=2016-08-22T00:00:00&until=2016-08-31T23:59:59
			//resource_type=credential
			if ( !string.IsNullOrWhiteSpace( type ) )
				filter = string.Format( "resource_type={0}", type.ToLower() );

			SetPaging( pageNbr, pageSize, ref filter );
			SetDateFilters( startingDate, endingDate, ref filter );
			SetSortOrder( ref filter, sortOrder );

			serviceUri += filter.Length > 0 ? filter : "";
			//future proof

			List<ReadEnvelope> list = new List<ReadEnvelope>();
			try
			{
				WebRequest request = WebRequest.Create( serviceUri );

				// If required by the server, set the credentials.
				request.Credentials = CredentialCache.DefaultCredentials;

				var hdr = new WebHeaderCollection
				{
					{ "Authorization", "Token  " + credentialEngineAPIKey }
				};
				request.Headers.Add( hdr );

				HttpWebResponse response = ( HttpWebResponse )request.GetResponse();
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
				var hdr2 = response.GetResponseHeader( "Link" );
				Int32.TryParse( response.GetResponseHeader( "Total" ), out pTotalRows );
				//20-07-02 mp - seems the header name is now X-Total
				if ( pTotalRows == 0 )
				{
					Int32.TryParse( response.GetResponseHeader( "X-Total" ), out pTotalRows );
				}
				int totalPages = 0;
				Int32.TryParse( response.GetResponseHeader( "X-Total-Pages" ), out totalPages );
				//map to the list
				list = JsonConvert.DeserializeObject<List<ReadEnvelope>>( document );

			}
			catch ( Exception exc )
			{
				LoggingHelper.LogError( exc, "RegistryServices.Search" );
			}
			return list;
		}
		public static string GetRegistrySearchUrl( string community = "" )
		{
			if ( string.IsNullOrWhiteSpace( community ) )
			{
				community = UtilityManager.GetAppKeyValue( "defaultCommunity" );
			}
			string serviceUri = UtilityManager.GetAppKeyValue( "credentialRegistrySearch" );
			serviceUri = string.Format( serviceUri, community );
			return serviceUri;
		}
		public static string GetEnvelopeUrl( string envelopeId, string community = "" )
		{
			if ( string.IsNullOrWhiteSpace( community ) )
			{
				community = UtilityManager.GetAppKeyValue( "defaultCommunity" );
			}
			string serviceUri = UtilityManager.GetAppKeyValue( "cerGetEnvelope" );

			string registryEnvelopeUrl = string.Format( serviceUri, community, envelopeId );
			return registryEnvelopeUrl;
		}
		public static string GetResourceUrl( string ctid, string community = "" )
		{
			if ( string.IsNullOrWhiteSpace( community ) )
			{
				community = UtilityManager.GetAppKeyValue( "defaultCommunity" );
			}
			string serviceUri = UtilityManager.GetAppKeyValue( "credentialRegistryResource" );

			string registryUrl = string.Format( serviceUri, community, ctid );

			return registryUrl;
		}
		//public static string GetRegistryEnvelopeUrl(string community)
		//{
		//	//the app key should be changed to be more meaningful!!
		//	string serviceUri = UtilityManager.GetAppKeyValue( "cerGetEnvelope" );
		//	serviceUri = string.Format( serviceUri, community );
		//	return serviceUri;
		//}
		public static string GetRegistryUrl( string appKey, string community )
		{
			//requires all urls to have a parameter?
			//or, check if the default community exists
			//also have to handle an absence community
			string serviceUri = UtilityManager.GetAppKeyValue( appKey );
			serviceUri = string.Format( serviceUri, community );
			return serviceUri;
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
		private static void SetSortOrder( ref string where, string sortOrder = "asc" )
		{
			string AND = "";
			if ( where.Length > 0 )
				AND = "&";
			if ( string.IsNullOrWhiteSpace( sortOrder ) )
				sortOrder = "asc";
			//this is the default anyway - maybe not, seems like dsc is the default
			where = where + AND + "sort_by=updated_at&sort_order=" + sortOrder;
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
				//apparently this is not necessary!!
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
		#endregion
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

using Download.Models;
using Download.Services;

using Newtonsoft.Json;

namespace Download
{
	public class RegistryHelper
	{
		/// <summary>
		/// class name (used in generic messages)
		/// </summary>
		static string thisClassName = "RegistryImport";

		public string StartingDate = "";

		public string EndingDate = "";
		/// <summary>
		/// global filter for owning organizaion
		/// </summary>
		public string OwningOrganizationCTID = "";

		/// <summary>
		/// default filter for publishing organization
		/// </summary>
		public string PublishingOrganizationCTID = "";

		/// <summary>
		/// Establish the default community
		/// </summary>
		/// <param name="community"></param>
		public RegistryHelper( string community )
		{
			Community = community;
		}
		public string Community { get; set; }

		public void Retrieve( string registryEntityType, int entityTypeId, int maxRecords,  ref int recordsImported, ref List<string> importSummary, string sortOrder = "asc" )
		{

			bool importingThisType = UtilityManager.GetAppKeyValue( "importing_" + registryEntityType, true );
			if ( !importingThisType )
			{
				LoggingHelper.DoTrace( 1, string.Format( "===  *****************  Skipping import of {0}  ***************** ", registryEntityType ) );
				importSummary.Add( "Skipped import of " + registryEntityType);
				return;
			}
			LoggingHelper.DoTrace( 1, string.Format( "===  *****************  Importing {0}  ***************** ", registryEntityType ) );

			//bool downloadOnly
			//
			ReadEnvelope envelope = new ReadEnvelope();
			List<ReadEnvelope> list = new List<ReadEnvelope>();

			int pageNbr = 1;
			int pageSize = UtilityManager.GetAppKeyValue( "importPageSize", 100 );
			string importResults = "";
			string importNote = "";
			//ThisEntity output = new ThisEntity();
			List<string> messages = new List<string>();

			int cntr = 0;
			int pTotalRows = 0;

			int exceptionCtr = 0;
			string statusMessage = "";
			bool isComplete = false;
			//var request = new SearchRequest()
			//{
			//	StartingDate = startingDate,
			//	EndingDate = endingDate,
			//	OwningOrganizationCTID = owningOrganizationCTID,
			//	PublishingOrganizationCTID = publishingOrganizationCTID,
			//	DownloadOnly = downloadOnly
			//};
			//will need to handle multiple calls - watch for time outs
			while ( pageNbr > 0 && !isComplete )
			{
				//19-09-22 chg to use RegistryServices to remove duplicate services
				list = Search( registryEntityType, StartingDate, EndingDate, pageNbr, pageSize, ref pTotalRows, ref statusMessage, Community, OwningOrganizationCTID, PublishingOrganizationCTID, sortOrder );

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
					string ctid = item.EnvelopeCtid;
					string payload = item.DecodedResource.ToString();
					DateTime createDate = new DateTime();
					DateTime envelopeUpdateDate = new DateTime();
					if ( DateTime.TryParse( item.NodeHeaders.CreatedAt.Replace( "UTC", "" ).Trim(), out createDate ) )
					{
						//status.SetEnvelopeCreated( createDate );
					}
					if ( DateTime.TryParse( item.NodeHeaders.UpdatedAt.Replace( "UTC", "" ).Trim(), out envelopeUpdateDate ) )
					{
						//status.SetEnvelopeUpdated( envelopeUpdateDate );
					}
					//payload contains the graph from DecodedResource
					//var ctdlType = RegistryServices.GetResourceType( payload );
					LoggingHelper.DoTrace( 2, string.Format( "{0}. {1} ctid {2}, lastUpdated: {3} ", cntr, registryEntityType, ctid, envelopeUpdateDate ) );

					//existing files will be overridden. , suppress the date prefix (" "), or use an alternate prefix
					LoggingHelper.WriteLogFile( 1, registryEntityType + "_" + ctid, payload, "", false );

					#region future: define process to generic record to a database.
					//TODO - add optional save to a database
					//		- will need entity type, ctid, name, description (maybe), created and lastupdated from envelope,payload
					//		- only doing adds, allows for history, user can choose to do updates
					if ( UtilityManager.GetAppKeyValue( "savingDocumentToDatabase", false ) )
					{
						var graphMainResource = RegistryServices.GetGraphMainResource( payload );
						var resource = new CredentialRegistryResource()
						{
							EntityType = graphMainResource.Type,
							CTID = ctid,
							DownloadDate = DateTime.Now,
							Created = createDate,
							LastUpdated = envelopeUpdateDate,
							CredentialRegistryGraph = payload
						};
						if ( entityTypeId == 10 )
						{
							resource.Name = graphMainResource.CeasnName.ToString();
						}
						else
						{
							resource.Name = graphMainResource.Name.ToString();
							resource.Description = graphMainResource.Description.ToString();
							resource.SubjectWebpage = graphMainResource.SubjectWebpage;
						}
						statusMessage = "";
						//optionally save record to a database
						if ( new DatabaseServices().Add( resource, ref statusMessage ) == 0 )
						{
							//error handling
						}
					}
					#endregion

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
			importSummary.Add( importResults );
			recordsImported += cntr;

			//return importResults;
		}

		/// <summary>
		/// Handle deleted records for the requested time period
		/// Save to file systems with prefix of Deleted
		/// </summary>
		/// <param name="community"></param>
		/// <param name="maxRecords"></param>
		/// <param name="recordsDeleted"></param>
		/// <returns></returns>
		public string HandleDeletes( string community, int maxRecords, ref int recordsDeleted )
		{
			int pageNbr = 1;
			int pageSize = 50;
			//string importError = "";
			//may want to just do all types!
			string type = "";
			List<string> messages = new List<string>();
			List<ReadEnvelope> list = new List<ReadEnvelope>();
			SaveStatus status = new SaveStatus();
			int pTotalRows = 0;
			int cntr = 0;
			bool isComplete = false;
			int exceptionCtr = 0;
			string statusMessage = "";
			string importResults = "";
			string importNote = "";

			LoggingHelper.DoTrace( 1, string.Format( "===  DELETE Check for: '{0}' to '{1}' ===", StartingDate, EndingDate ) );
			//registryImport.StartingDate = "2017-10-29T00:00:00";
			try
			{
				while ( pageNbr > 0 && !isComplete )
				{
					list = GetDeleted( community, type, StartingDate, EndingDate, pageNbr, pageSize, ref pTotalRows, ref statusMessage );

					if ( list == null || list.Count == 0 )
					{
						isComplete = true;
						if ( pageNbr == 1 )
						{
							importNote = "Deletes: No records where found for date range ";
							//Console.WriteLine( thisClassName + ".HandleDeletes() - " + importNote );
							LoggingHelper.DoTrace( 1, string.Format( "Download.Main.HandleDeletes() Community: {0} - ", community ) + importNote );
						}
						break;
					}
					foreach ( ReadEnvelope item in list )
					{
						cntr++;
						string payload = item.DecodedResource.ToString();

						string ctdlType = RegistryServices.GetResourceType( payload );
						string ctid = item.EnvelopeCtid;

						LoggingHelper.DoTrace( 6, string.Format( "{0}. ctdlType: {1} ctid: {2} ", cntr, ctdlType, ctid ) );
						try
						{
							//only need the envelopeId and type
							//so want a full delete, or set EntityStateId to 0 - just as a precaution
							messages = new List<string>();
							//action - place in a deleted folder?
							LoggingHelper.WriteLogFile( 1, "Deleted_" + ctdlType + "_" + ctid, payload, "", false );

						}
						catch ( Exception ex )
						{
							LoggingHelper.LogError( ex, string.Format( "Exception encountered in envelopeId: {0}", item.EnvelopeIdentifier ), false, "CredentialFinder Import exception" );
							//importError = ex.Message;

						}

						if ( maxRecords > 0 && cntr > maxRecords )
						{
							break;
						}
					} //foreach ( ReadEnvelope item in list )

					pageNbr++;
					if ( ( maxRecords > 0 && cntr > maxRecords ) || cntr > pTotalRows )
					{
						isComplete = true;
						DisplayMessages( string.Format( "Delete EARLY EXIT. Completed {0} records out of a total of {1} ", cntr, pTotalRows ) );

					}
				} //while
				  //delete from elastic
				if ( cntr > 0 )
				{
					messages = new List<string>();
				}

				importResults = string.Format( "HandleDeletes - Processed {0} records, with {1} exceptions. \r\n", cntr, exceptionCtr );
				if ( !string.IsNullOrWhiteSpace( importNote ) )
					importResults += importNote;
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "Import.HandleDeletes" );
			}
			//actually only attepted at this time, need to account for errors!
			recordsDeleted = cntr;
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
		public List<ReadEnvelope> GetDeleted( string community, string type, string startingDate, string endingDate, int pageNbr, int pageSize, ref int pTotalRows, ref string statusMessage )
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
				//20-07-02 mp - seems the header name is now X-Total
				if ( pTotalRows == 0 )
				{
					Int32.TryParse( response.GetResponseHeader( "X-Total" ), out pTotalRows );
				}
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
		public static List<ReadEnvelope> Search( string type, string startingDate, string endingDate, int pageNbr, int pageSize, ref int pTotalRows, ref string statusMessage, string community, string owningOrganizationCTID = "", string publishingOrganizationCTID = "", string sortOrder = "asc" )
		{
			string credentialEngineAPIKey = UtilityManager.GetAppKeyValue( "MyCredentialEngineAPIKey" );
			string environment = UtilityManager.GetAppKeyValue( "envType" );
			if ( environment == "sandbox" && credentialEngineAPIKey == "PROVIDE YOUR ACCOUNTS API KEY" )
			{
				credentialEngineAPIKey = "";
				LoggingHelper.DoTrace( 1, "NOTE: an API key was not provided for the search. This is allowed for the sandbox, but not for production." );
			}
			string document = "";
			string filter = "";
			//includes the question mark
			string serviceUri = GetRegistrySearchUrl( community );
			//from=2016-08-22T00:00:00&until=2016-08-31T23:59:59
			//resource_type=credential
			if ( !string.IsNullOrWhiteSpace( type ) )
				filter = string.Format( "resource_type={0}", type.ToLower() );

			//
			SetOwningOrganizationFilters( owningOrganizationCTID, ref filter );
			SetPublishingOrganizationFilters( publishingOrganizationCTID, ref filter );
			//
			SetPaging( pageNbr, pageSize, ref filter );
			SetDateFilters( startingDate, endingDate, ref filter );
			SetSortOrder( ref filter, sortOrder );

			serviceUri += filter.Length > 0 ? filter : "";
			//
			List<ReadEnvelope> list = new List<ReadEnvelope>();
			try
			{
				WebRequest request = WebRequest.Create( serviceUri );

				// If required by the server, set the credentials.
				request.Credentials = CredentialCache.DefaultCredentials;
				if ( !string.IsNullOrWhiteSpace( credentialEngineAPIKey ) )
				{
					var hdr = new WebHeaderCollection
					{
						{ "Authorization", "Token  " + credentialEngineAPIKey }
					};
					request.Headers.Add( hdr );
				}
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

			string serviceUri = UtilityManager.GetAppKeyValue( "assistantCredentialRegistrySearch" );
			if ( !string.IsNullOrWhiteSpace( community ) )
				serviceUri += "community=" + community + "&";

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

		/// <summary>
		/// To limit the search results to a single organization, provide the CTID of the organization. 
		/// </summary>
		/// <param name="owningOrganizationCTID"></param>
		/// <param name="where"></param>
		private static void SetOwningOrganizationFilters( string owningOrganizationCTID, ref string where )
		{
			string AND = "";
			if ( where.Length > 0 )
				AND = "&";

			if ( !string.IsNullOrWhiteSpace( owningOrganizationCTID ) )
			{
				where = where + AND + string.Format( "owned_by={0}", owningOrganizationCTID );
				AND = "&";
			}

		}
		/// <summary>
		/// To limit the search results to those by a third party publisher, provide the CTID of the publisher (typically done in the app.config. 
		/// NOTE: where the publisher and the owner are the same, there is no need to provide both the owning and publishing org filters, just pick one. 
		/// </summary>
		/// <param name="publishingOrganizationCTID"></param>
		/// <param name="where"></param>
		private static void SetPublishingOrganizationFilters( string publishingOrganizationCTID, ref string where )
		{
			string AND = "";
			if ( where.Length > 0 )
				AND = "&";

			if ( !string.IsNullOrWhiteSpace( publishingOrganizationCTID ) )
			{
				where = where + AND + string.Format( "published_by={0}", publishingOrganizationCTID );
				AND = "&";
			}

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
	public class SearchRequest
	{
		public SearchRequest()
		{
		}
		public SearchRequest( int entityTypeId )
		{
			switch (entityTypeId)
			{
				case 1:
					RegistryEntityType = "credential";
					break;
				case 2:
					RegistryEntityType = "organization";
					break;
				case 3:
					RegistryEntityType = "assessment_profile";
					break;
				case 7:
					RegistryEntityType = "learning_opportunity_profile";
					break;
				case 8:
					RegistryEntityType = "pathway";
					break;
				case 10:
					RegistryEntityType = "competency_framework";
					break;
				case 11:
					RegistryEntityType = "concept_scheme";
					break;
				case 19:
					RegistryEntityType = "condition_manifest_schema";
					break;
				case 20:
					RegistryEntityType = "cost_manifest_schema";
					break;
				case 26:
					RegistryEntityType = "transfer_value_profile";
					break;
				case 31:
					RegistryEntityType = "qdata_dataset_profile";
					break;
			}
		}
		public string RegistryEntityType { get; set; }
		public int EntityTypeId { get; set; }
		public string StartingDate { get; set; }
		public string EndingDate { get; set; }
		public string OwningOrganizationCTID { get; set; }
		public string PublishingOrganizationCTID { get; set; }
		public string SortOrder { get; set; } = "asc";
	}
}

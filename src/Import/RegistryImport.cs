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

using workIT.Factories;
using workIT.Models;
using workIT.Utilities;

namespace CTI.Import
{
	public class RegistryImport
	{
        static string thisClassName = "RegistryImport";
		public RegistryImport( string community)
		{
			Community = community;
		}
		public string Community { get; set; }
        ImportCredential credImportMgr = new ImportCredential();
        ImportOrganization orgImportMgr = new ImportOrganization();
        ImportAssessment asmtImportMgr = new ImportAssessment();
        ImportLearningOpportunties loppImportMgr = new ImportLearningOpportunties();
        ImportConditionManifests cndManImportMgr = new ImportConditionManifests();
        ImportCostManifests cstManImportMgr = new ImportCostManifests();

        public static int maxExceptions = UtilityManager.GetAppKeyValue( "maxExceptions", 500 );

        public string Import( string registryEntityType, int entityTypeId, string startingDate, string endingDate, int maxRecords, bool downloadOnly, ref int recordsImported )
        {

			bool importingThisType = UtilityManager.GetAppKeyValue( "importing_" + registryEntityType, true );
			if (!importingThisType )
			{
				LoggingHelper.DoTrace( 1, string.Format( "===  *****************  Skipping import of {0}  ***************** ", registryEntityType ) );
				return "Skipped import of " + registryEntityType;
			}
			LoggingHelper.DoTrace( 1, string.Format( "===  *****************  Importing {0}  ***************** ", registryEntityType ) );
			//JsonEntity input = new JsonEntity();
			ReadEnvelope envelope = new ReadEnvelope();
            List<ReadEnvelope> list = new List<ReadEnvelope>();
            //EntityServices mgr = new EntityServices();
            ImportServiceHelpers importMgr = new ImportServiceHelpers();
            string entityType = registryEntityType;
            CodeItem ci = CodesManager.Codes_EntityType_Get( entityTypeId );
            if ( ci != null && ci.Id > 0 )
                entityType = ci.Title;
            int pageNbr = 1;
            int pageSize = 25;
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
            SaveStatus status = new SaveStatus();

            //will need to handle multiple calls - watch for time outs
            while ( pageNbr > 0 && !isComplete )
            {
				//19-09-22 chg to use RegistryServices to remove duplicate services
                list = RegistryServices.Search( registryEntityType, startingDate, endingDate, pageNbr, pageSize, ref pTotalRows, ref statusMessage, Community );

				//list = RegistryImport.GetLatest( registryEntityType, startingDate, endingDate, pageNbr, pageSize, ref pTotalRows, ref statusMessage, Community );

				if ( list == null || list.Count == 0 )
                {
                    isComplete = true;
                    if ( pageNbr == 1 )
                    {
                        //importNote = registryEntityType + ": No records where found for date range ";

                        //Console.WriteLine( thisClassName + importNote );
                        LoggingHelper.DoTrace( 4, registryEntityType + ": No records where found for date range. " );
                    }
                    break;
                }

                foreach ( ReadEnvelope item in list )
                {
                    cntr++;
                    string payload = item.DecodedResource.ToString();
                    newImportId = 0;
                    LoggingHelper.DoTrace( 2, string.Format( "{0}. {1} EnvelopeIdentifier {2} ", cntr, registryEntityType, item.EnvelopeIdentifier ) );
                    messages = new List<string>();
                    status = new SaveStatus();
                    status.DoingDownloadOnly = downloadOnly;
                    status.ValidationGroup = string.Format( "{0} Import", registryEntityType );
                    importError = "";
                    importSuccessfull = false;

                    try
                    {
                        //Console.WriteLine( string.Format( "{0}. {1} EnvelopeIdentifier {2} ", cntr, registryEntityType, item.EnvelopeIdentifier ) );
                        if ( payload.IndexOf( "@graphXXX" ) > 0 )
                        {
                            DisplayMessages( string.Format( "{0}. {1} Import Encountered an envelope using @graph which is not handled yet: {2} ", cntr, entityTypeId, item.EnvelopeIdentifier ) );
                        }
                        else
                        {
                            switch ( entityTypeId )
                            {

                                case 1:
                                    importSuccessfull = credImportMgr.ProcessEnvelope( item, status );
                                    break;
                                case 2:
                                    importSuccessfull = orgImportMgr.ProcessEnvelope( item, status );
                                    break;
                                case 3:
                                    importSuccessfull = asmtImportMgr.ProcessEnvelope( item, status );
                                    break;
                                case 7:
                                    importSuccessfull = loppImportMgr.ProcessEnvelope( item, status );
                                    break;
                                case 19:
                                    importSuccessfull = cndManImportMgr.ProcessEnvelope( item, status );
                                    break;
                                case 20:
                                    importSuccessfull = cstManImportMgr.ProcessEnvelope( item, status );
                                    break;
                                default:
                                    DisplayMessages( string.Format( "{0}. Invalid Entity type encountered: {1} ", cntr, entityTypeId ) );
                                    break;
                            }
                        }

                    }
                    catch ( Exception ex )
                    {
                        if ( ex.Message.IndexOf( "Path '@context', line 1" ) > 0 )
                        {
                            importError = "The referenced registry document is using an old schema. Please republish it with the latest schema!";
                            status.AddError( importError );
                        }
                        else
                        {
                            LoggingHelper.LogError( ex, string.Format( entityType + " Exception encountered in envelopeId: {0}", item.EnvelopeIdentifier ), false, "CredentialFinder Import exception" );
                            status.AddError( ex.Message );
                            importError = ex.Message;
                        }

                        //make continue on exceptions an option
                        exceptionCtr++;
                        if ( maxExceptions > 0 && exceptionCtr > maxExceptions )
                        {
                            //arbitrarily stop if large number of exceptions
                            importNote = string.Format( thisClassName + " - {0} Many exceptions ({1}) were encountered during import - abandoning.", entityType, exceptionCtr );
                            //Console.WriteLine( importNote );
                            LoggingHelper.DoTrace( 1, importNote );
                            LoggingHelper.LogError( importNote, true, thisClassName + "- many exceptions" );
                            isComplete = true;
                            break;
                        }
                    }
                    finally
                    {
                        if ( !importSuccessfull )
                        {
                            if ( string.IsNullOrWhiteSpace( importError ) )
                            {
                                importError = string.Join( "\r\n", status.GetAllMessages().ToArray() );
                            }
                        }
                        //store document
                        //add indicator of success
                        newImportId = importMgr.Add( item, entityTypeId, status.Ctid, importSuccessfull, importError, ref messages );
                        if ( newImportId > 0 && status.Messages != null && status.Messages.Count > 0 )
                        {
                            //add indicator of current recored
                            string msg = string.Format( "========= Messages for {4}, EnvelopeIdentifier: {0}, ctid: {1}, Id: {2}, rowId: {3} =========", item.EnvelopeIdentifier, status.Ctid, status.DocumentId, status.DocumentRowId, registryEntityType );
                            importMgr.AddMessages( newImportId, status, ref messages );
                        }

                    } //finally

                    if ( maxRecords > 0 && cntr > maxRecords )
                    {
                        break;
                    }
                } //end foreach 

                pageNbr++;
                if ( ( maxRecords > 0 && cntr > maxRecords ) || cntr > pTotalRows )
                {
                    isComplete = true;
                    LoggingHelper.DoTrace( 2, string.Format( "Import {2} EARLY EXIT. Completed {0} records out of a total of {1} ", cntr, pTotalRows, registryEntityType ) );

                }
                //if ( pageNbr * pageSize < pTotalRows )
                //	pageNbr++;
                //else
                //	isComplete = true;
            }
            importResults = string.Format( "Import {0} - Processed {1} records, with {2} exceptions. \r\n", registryEntityType, cntr, exceptionCtr );
            if ( !string.IsNullOrWhiteSpace( importNote ) )
                importResults += importNote;

			recordsImported += cntr;

			return importResults;
        }
        public static string DisplayMessages( string message )
        {
            LoggingHelper.DoTrace( 1, message );
            //Console.WriteLine( message );

            return message;
        }

		//actually may want to pass community, to allow multiple calls
        public static List<ReadEnvelope> GetLatest( string type, string startingDate, string endingDate, int pageNbr, int pageSize, ref int pTotalRows, ref string statusMessage, string community)
		{
			string document = "";
			string filter = "";
			//includes the question mark
			string serviceUri = RegistryServices.GetRegistrySearchUrl( community );
			//from=2016-08-22T00:00:00&until=2016-08-31T23:59:59
			//resource_type=credential
			if ( !string.IsNullOrWhiteSpace( type ) )
				filter = string.Format( "resource_type={0}", type.ToLower() );

			SetPaging( pageNbr, pageSize, ref filter );
			SetDateFilters( startingDate, endingDate, ref filter );

			serviceUri += filter.Length > 0 ? filter : "";

			List<ReadEnvelope> list = new List<ReadEnvelope>();
			//ReadEnvelope envelope = new ReadEnvelope();

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

				//map to the list
				list = JsonConvert.DeserializeObject<List<ReadEnvelope>>( document );

				
			}
			catch ( Exception exc )
			{
				LoggingHelper.LogError( exc, "RegistryServices.GetLatest" );
			}
			return list;
		}


		public static List<ReadEnvelope> GetDeleted( string community, string type, string startingDate, string endingDate, int pageNbr, int pageSize, ref int pTotalRows, ref string statusMessage )
		{
			string document = "";
			string filter = "include_deleted=only";
			string serviceUri = RegistryServices.GetRegistrySearchUrl( community );
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
				HttpWebResponse response = ( HttpWebResponse ) request.GetResponse();
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
}

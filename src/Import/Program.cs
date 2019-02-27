using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Utilities;
using workIT.Factories;
using Import.Services;
using workIT.Models;
using workIT.Services;
namespace CTI.Import
{
	class Program
	{
		static string thisClassName = "Program";
        public static int maxExceptions = UtilityManager.GetAppKeyValue( "maxExceptions", 500 );

        ImportCredential credImportMgr = new ImportCredential();
        ImportOrganization orgImportMgr = new ImportOrganization();
        ImportAssessment asmtImportMgr = new ImportAssessment();
        ImportLearningOpportunties loppImportMgr = new ImportLearningOpportunties();
        ImportConditionManifests cndManImportMgr = new ImportConditionManifests();
        ImportCostManifests cstManImportMgr = new ImportCostManifests();

        static void Main( string[] args )
		{
			//NOTE: consider the IOER approach that all candidate records are first downloaded, and then a separate process does the import

			RegistryImport registryImport = new RegistryImport();
			LoggingHelper.DoTrace( 1, "======================= STARTING IMPORT =======================" );
            TimeZone zone = TimeZone.CurrentTimeZone;
            // Demonstrate ToLocalTime and ToUniversalTime.
            DateTime local = zone.ToLocalTime( DateTime.Now );
            DateTime universal = zone.ToUniversalTime( DateTime.Now );
			LoggingHelper.DoTrace( 1, "Local time: " + local );
			LoggingHelper.DoTrace( 1, "Universal time: " + universal );

            //need to determine how to get last start date
            //may be run multiple times during day, so use a schedule type
            string scheduleType = UtilityManager.GetAppKeyValue( "scheduleType", "daily" );
            int deleteAction = UtilityManager.GetAppKeyValue( "deleteAction", 0 );
            bool doingDownloadOnly = UtilityManager.GetAppKeyValue( "DoingDownloadOnly", false );

            #region  Import Type/Arguments
            if ( args != null && args.Length == 1 )
			{
				scheduleType = args[ 0 ];
			}
			string startingDate = DateTime.Now.AddDays( -1 ).ToString();
            //typically will want this as registry server is UTC (+6 hours from central)
            bool usingUTC_ForTime = UtilityManager.GetAppKeyValue( "usingUTC_ForTime", true );
            

            string endingDate = "";
			string importResults = "";

			//could ignore end date until a special scedule type of adhoc is used, then read the dates from config
			importResults = DisplayMessages( string.Format( " - Schedule type: {0} ", scheduleType ) );
			int minutes = 0;
			
			if ( Int32.TryParse( scheduleType, out minutes ) )
			{
				//minutes
				//may want more flexibility and use input parms
				if (minutes < 1 || minutes > 1440) //doesn't really matter
				{
					DisplayMessages( string.Format( "invalid value encountered for Minutes option: {0} - defaulting to 60.", scheduleType) );
					minutes = 60;
				}
                if ( usingUTC_ForTime )
                {
                    //UTC is +6 hours (360 minutes), so subtract entered minutes and add to current time
                    // ex: If -60, want 5 hours (360 - 60)
                    minutes = minutes * -1;
                    //startingDate = DateTime.Now.AddMinutes( minutes ).ToString( "yyyy-MM-ddTHH:mm:ss" );
                    startingDate = zone.ToUniversalTime( DateTime.Now.AddMinutes( minutes ) ).ToString( "yyyy-MM-ddTHH:mm:ss" );
                    //no end date?
                    endingDate = "";
                }
                else
                {
                    startingDate = DateTime.Now.AddMinutes( -minutes ).ToString( "yyyy-MM-ddTHH:mm:ss" );
                    //the server date is UTC, so if we leave enddate open, we will get the same stuff all day, so setting an endate to the current hour
                    endingDate = DateTime.Now.ToString( "yyyy-MM-ddTHH:mm:ss" );
                }
                importResults = importResults + "<br/>" + DisplayMessages( string.Format( " - Updates since: {0} {1}", startingDate, usingUTC_ForTime ? " (UTC)" : "" ) );
            } 
            else if( scheduleType == "sinceLastRun" )
			{
                SiteActivity lastRun = ActivityServices.GetLastImport();
                if (usingUTC_ForTime)
                {
                    startingDate = zone.ToUniversalTime( lastRun.Created ).ToString( "yyyy-MM-ddTHH:mm:ss" );
                }
                else
                {
                    startingDate = lastRun.Created.ToString( "yyyy-MM-ddTHH:mm:ss" );
                }
                endingDate = "";
                importResults = importResults + "<br/>" + DisplayMessages( string.Format( " - Updates since: {0} {1}", startingDate, usingUTC_ForTime ? " (UTC)" : "" ) );
            }
            else if (scheduleType == "adhoc")
            {
                startingDate = UtilityManager.GetAppKeyValue( "startingDate", "" );
                endingDate = UtilityManager.GetAppKeyValue( "endingDate", "" );
				DateTime dtcheck = System.DateTime.Now;				//LoggingHelper.DoTrace( 1, string.Format( " - Updates from: {0} to {1} ", startingDate, endingDate ) );
				
				if ( usingUTC_ForTime )
				{
					if ( DateTime.TryParse( startingDate, out dtcheck ) )
					{
						startingDate = zone.ToUniversalTime( dtcheck ).ToString( "yyyy-MM-ddTHH:mm:ss" );
					}
					if ( DateTime.TryParse( endingDate, out dtcheck ) )
					{
						endingDate = zone.ToUniversalTime( dtcheck ).ToString( "yyyy-MM-ddTHH:mm:ss" );
					}
					//no end date?
					//endingDate = "";
				}
				importResults = importResults + "<br/>" + DisplayMessages( string.Format( " - Updates from: {0} to {1} ", startingDate, endingDate ) );
            }
            else if ( scheduleType == "hourly" )
			{
                if ( usingUTC_ForTime )
                {
                    //6 hour diff, so add 5 hours, equiv to +6 hours - 1 hour
                    startingDate = zone.ToUniversalTime( DateTime.Now.AddHours( -1 )).ToString( "yyyy-MM-ddTHH:mm:ss" );
                }
                else
                {
                    startingDate = DateTime.Now.AddHours( -1 ).ToString( "yyyy-MM-ddTHH:mm:ss" );
                    //format into: 2016-08-01T23:59:59
                    //the server date is UTC, so if we leave enddate open, we will get the same stuff all day, so setting an endate to the current hour
                    //HOWEVER - THIS COULD RESULT IN BEING 6 HOURS BEHIND
                    endingDate = DateTime.Now.ToString( "yyyy-MM-ddTHH:mm:ss" );
                }
                //LoggingHelper.DoTrace( 1, string.Format( " - Updates since: {0} ", startingDate ) );
                importResults = importResults + "<br/>" + DisplayMessages( string.Format( " - Updates since: {0} {1}", startingDate, usingUTC_ForTime ? " (UTC)" : "" ) );
			}
            else
			{
				//assume daily
				startingDate = DateTime.Now.AddDays( -1 ).ToString( "yyyy-MM-ddTHH:mm:ss" );
				//format into: 2016-08-01T23:59:59
				endingDate = "";
				//LoggingHelper.DoTrace( 1, string.Format( " - Updates since: {0} ", startingDate ) );
				importResults = importResults + "<br/>" + DisplayMessages( string.Format( " - Updates since: {0} ", startingDate ) );
			}
            #endregion
            //===================================================================================================
            LogStart();
            //set to zero to handle all, or a number to limit records to process
            //partly for testing
            //although once can sort by date, we can use this, and update the start date
            int maxImportRecords = UtilityManager.GetAppKeyValue( "maxImportRecords", 50 );

			//NOTE - NEED TO REBUILD CACHE TABLES BEFORE BUILDING ELASTIC INDICES

			//Actions: 0-normal; 1-DeleteOnly; 2-SkipDelete 
			int recordsDeleted = 0;
            if ( deleteAction < 2 )
            {
                //handle deleted records
                importResults = importResults + "<br/>" + HandleDeletes( startingDate, endingDate, maxImportRecords, ref recordsDeleted );
            }
			int recordsImported = 0;
            if ( deleteAction != 1 )
            {
                //do manifests 
                //importResults = importResults + "<br/>" + new ConditionManifestImport().Import( startingDate, endingDate, maxImportRecords, doingDownloadOnly );
                importResults = importResults + "<br/>" + registryImport.Import( "condition_manifest_schema", CodesManager.ENTITY_TYPE_CONDITION_MANIFEST, startingDate, endingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );
                //importResults = importResults + "<br/>" + new CostManifestImport().Import( startingDate, endingDate, maxImportRecords, doingDownloadOnly );
                importResults = importResults + "<br/>" + registryImport.Import( "cost_manifest_schema", CodesManager.ENTITY_TYPE_COST_MANIFEST, startingDate, endingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );

                //handle credentials
                //importResults = importResults + "<br/>" + new CredentialsImport().Import( startingDate, endingDate, maxImportRecords, doingDownloadOnly );
                importResults = importResults + "<br/>" + registryImport.Import( "credential", CodesManager.ENTITY_TYPE_CREDENTIAL, startingDate, endingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );
                //handle assessments
                //importResults = importResults + "<br/>" + new AssessmentsImport().Import( startingDate, endingDate, maxImportRecords, doingDownloadOnly );
                importResults = importResults + "<br/>" + registryImport.Import( "assessment_profile", CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, startingDate, endingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );

                //handle learning opps
                //importResults = importResults + "<br/>" + new LearningOpportuniesImport().Import( startingDate, endingDate, maxImportRecords, doingDownloadOnly );
                importResults = importResults + "<br/>" + registryImport.Import( "learning_opportunity_profile", CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, startingDate, endingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );

                importResults = importResults + "<br/>" + new CompetencyFramesworksImport().Import( startingDate, endingDate, maxImportRecords, doingDownloadOnly );

                //handle organizations
                //might be better to do last, then can populate placeholders, try first
                //importResults = importResults + "<br/>" + new OrganizationsImport().Import( startingDate, endingDate, maxImportRecords, doingDownloadOnly );
                importResults = importResults + "<br/>" + registryImport.Import( "organization", 2, startingDate, endingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );

				if ( !doingDownloadOnly && recordsImported > 0 )
				{
                    //==============================================================
                    //import pending
                    string pendingStatus = new RegistryServices().ImportPending();

                    importResults = importResults + "<br/>TODO: add stats from ImportPending.";
                }
            }

            //===================================================================================================
            if ( !doingDownloadOnly && recordsImported > 0 )
            {
                //update elastic if not included - probably will always delay elastic, due to multiple possible updates
                //may want to move this to services for use by other process, including adhoc imports
                if ( UtilityManager.GetAppKeyValue( "delayingAllCacheUpdates", true ) )
                {
                    ElasticServices.UpdateElastic(true);
                    //procs have been updated to use the 
                    //new CacheManager().PopulateAllCaches();
                    ////
                    //ElasticServices.HandlePendingReindexRequests();
                }

                //update code table counts
                new CacheManager().UpdateCodeTableCounts();
                //set all resolved records in Import_EntityResolution to be resolved.
                new ImportManager().SetAllResolvedEntities();
                //send summary email 
                string message = string.Format( "<h2>Import Results</h2><p>{0}</p>", importResults );
                EmailManager.NotifyAdmin( "Credential Finder Import Results", message );
            }

			//summary, and logging
			LoggingHelper.DoTrace( 1, "======================= all done ==============================" );
		}

        public static string DisplayMessages( string message )
		{
			LoggingHelper.DoTrace( 1, message );
			//Console.WriteLine( message );

			return message;
		}
        public static void LogStart( )
        {
            new ActivityServices().AddActivity( new SiteActivity()
                { ActivityType = "System", Activity = "Import", Event = "Start" } 
            );

        }
        public static string HandleDeletes( string startingDate, string endingDate, int maxRecords, ref int recordsDeleted )
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
		
			LoggingHelper.DoTrace( 1, string.Format( "===                   DELETES                   ===", thisClassName ) );
			//startingDate = "2017-10-29T00:00:00";
			try
			{
				while ( pageNbr > 0 && !isComplete )
				{
					list = RegistryImport.GetDeleted( type, startingDate, endingDate, pageNbr, pageSize, ref pTotalRows, ref statusMessage );

					if ( list == null || list.Count == 0 )
					{
						isComplete = true;
						if ( pageNbr == 1 )
						{
							importNote = "Deletes: No records where found for date range ";
							//Console.WriteLine( thisClassName + ".HandleDeletes() - " + importNote );
							LoggingHelper.DoTrace( 1, thisClassName + ".HandleDeletes() - " + importNote );
						}
						break;
					}
					foreach ( ReadEnvelope item in list )
					{
						cntr++;
						string payload = item.DecodedResource.ToString();

						string ctdlType = RegistryServices.GetResourceType( payload );
						string ctid = RegistryServices.GetCtidFromUnknownEnvelope( item );
						//may not be available in database, may want to use ctid
						string envelopeIdentifier = item.EnvelopeIdentifier;
						string envelopeUrl = RegistryServices.GetEnvelopeUrl( item.EnvelopeIdentifier );
						LoggingHelper.DoTrace( 5, "		envelopeUrl: " + envelopeUrl );

						LoggingHelper.DoTrace( 6, string.Format( "{0}. EnvelopeIdentifier: {1} ", cntr, item.EnvelopeIdentifier ) );
						try
						{
							//only need the envelopeId and type
							//so want a full delete, or set EntityStateId to 4 or greater - just as a precaution
							messages = new List<string>();
							status = new SaveStatus();
							status.ValidationGroup = "Deletes";
							//importError = "";
							//each delete method will add an entry to SearchPendingReindex.
							//at the end of the process, call method to handle all the deletes
							switch ( ctdlType.ToLower() )
							{
								case "credentialorganization":
								case "qacredentialorganization":
								case "organization":
									DisplayMessages( string.Format( "{0}. Deleting {3} by EnvelopeIdentifier/ctid: {1}/{2} ", cntr, item.EnvelopeIdentifier, ctid, ctdlType ) );
									if ( !new OrganizationManager().Delete( envelopeIdentifier, ctid, ref statusMessage ) )
										DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
									break;

								case "assessmentprofile":
									DisplayMessages( string.Format( "{0}. Deleting Assessment by EnvelopeIdentifier/ctid: {1}/{2} ", cntr, item.EnvelopeIdentifier, ctid ) );
									if ( !new AssessmentManager().Delete( envelopeIdentifier, ctid, ref statusMessage ) )
										DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
									break;
								case "learningopportunityprofile":
									DisplayMessages( string.Format( "{0}. Deleting LearningOpportunity by EnvelopeIdentifier/ctid: {1}/{2} ", cntr, item.EnvelopeIdentifier, ctid ) );
									if ( !new LearningOpportunityManager().Delete( envelopeIdentifier, ctid, ref statusMessage ) )
										DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
									break;
								case "conditionmanifest":
									DisplayMessages( string.Format( "{0}. Deleting ConditionManifest by EnvelopeIdentifier/ctid: {1}/{2} ", cntr, item.EnvelopeIdentifier, ctid ) );
									if ( !new ConditionManifestManager().Delete( envelopeIdentifier, ctid, ref statusMessage ) )
									{
										DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
									}
									break;
								case "costmanifest":
									DisplayMessages( string.Format( "{0}. Deleting CostManifest by EnvelopeIdentifier/ctid: {1}/{2} ", cntr, item.EnvelopeIdentifier, ctid ) );
									if ( !new CostManifestManager().Delete( envelopeIdentifier, ctid, ref statusMessage ) )
										DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
									break;
								default:
									//default to credential
									DisplayMessages( string.Format( "{0}. Deleting Credential ({2}) by EnvelopeIdentifier/ctid: {1}/{3} ", cntr, item.EnvelopeIdentifier, ctdlType, ctid ) );
									if ( !new CredentialManager().Delete( envelopeIdentifier, ctid, ref statusMessage ) )
										DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
									break;
							}
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
					ElasticServices.HandlePendingDeletes( ref messages );
				}

				importResults = string.Format( "HandleDeletes - Processed {0} records, with {1} exceptions. \r\n", cntr, exceptionCtr );
				if ( !string.IsNullOrWhiteSpace( importNote ) )
					importResults += importNote;
			}
			catch (Exception ex)
			{
				LoggingHelper.LogError( ex, "Import.HandleDeletes" );
			}
			//actually only attepted at this time, need to account for errors!
			recordsDeleted= cntr;
			return importResults;
		}
	}
}

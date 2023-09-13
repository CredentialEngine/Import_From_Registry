using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Script.Serialization;

using Import.Services;

using workIT.Factories;
using workIT.Models;
using workIT.Services;
using workIT.Utilities;

using ElasticHelper = workIT.Services.ElasticServices;
using RegistryServices = Import.Services.RegistryServices;
namespace CTI.Import
{
	class Program
	{
		static string thisClassName = "Program";
		public static int maxExceptions = UtilityManager.GetAppKeyValue( "maxExceptions", 500 );
		public static string envType = UtilityManager.GetAppKeyValue( "environment" );
		//

		static void Main( string[] args )
		{

			LoggingHelper.DoTrace( 1, "======================= STARTING IMPORT =======================" );
			bool usingImportPendingProcess = UtilityManager.GetAppKeyValue( "usingImportPendingProcess", false );
			if ( usingImportPendingProcess )
			{
				int stopAfterMinutes = UtilityManager.GetAppKeyValue( "stopAfterMinutes", 14 );
				var cycleMinutes = "";
				//consider parameters to override using importPending - especially for deletes
				if ( args != null )
				{
					//consider using args with qualifiers: scheduleType:10, community:xxxx, deleteOnly:true 
					//		or flags -c community -s schedule -d deleteOnly
					if ( args.Length >= 1 )
					{
						cycleMinutes = args[ 0 ];
						if ( Int32.TryParse( cycleMinutes, out int minutes ) )
						{
							stopAfterMinutes = minutes;
						}
					}
				}
				new ImportPendingRequests().Main( stopAfterMinutes );
			} else
			{
				DoImport( args );
				LoggingHelper.DoTrace( 1, "======================= all done ==============================" );
			}


		}

		public static void DoImport( string[] args )
		{
			TimeZone zone = TimeZone.CurrentTimeZone;
			// Demonstrate ToLocalTime and ToUniversalTime.
			DateTime local = zone.ToLocalTime( DateTime.Now );
			DateTime universal = zone.ToUniversalTime( DateTime.Now );
			LoggingHelper.DoTrace( 1, "Local time: " + local );
			LoggingHelper.DoTrace( 1, "Universal time: " + universal );

			//valdate current setup
			if ( !ValidateSetup() )
			{
				//should have already displayed a message.
				LoggingHelper.DoTrace( 1, "********************* ERROR ********************* " );
				return;
			}

			string importResults = "";
			int recordsDeleted = 0;
			
			var credentialRegistryUrl = UtilityManager.GetAppKeyValue( "credentialRegistryUrl" );
			LoggingHelper.DoTrace( 1, string.Format("********* DOING IMPORT FROM {0} *********", credentialRegistryUrl) );

			string connectionString =BaseFactory.DBConnectionRO();
			if ( !string.IsNullOrWhiteSpace( credentialRegistryUrl ) )
            {
				var parts = credentialRegistryUrl.Split( ';' );
				var db = parts.FirstOrDefault( s => s.Contains("database"));
				if ( db != null )
					LoggingHelper.DoTrace( 1, string.Format( "********* TARGET DATABASE {0} *********", db ) );
			}

			//typically will want this as registry server is UTC (+6 hours from central)
			bool usingUTC_ForTime = UtilityManager.GetAppKeyValue( "usingUTC_ForTime", true );
			
			string defaultCommunity = UtilityManager.GetAppKeyValue( "defaultCommunity" );
			string additionalCommunity = UtilityManager.GetAppKeyValue( "additionalCommunity" );
			var registryImport = new RegistryImport( defaultCommunity );

			//set to zero to handle all, or a number to limit records to process
			//partly for testing
			//although once can sort by date, we can use this, and update the start date
			registryImport.MaxImportRecords = UtilityManager.GetAppKeyValue( "maxImportRecords", 0 );

			//just call a search to ensure database exists and user has access

			//need to determine how to get last start date
			//may be run multiple times during day, so use a schedule type
			string scheduleType = UtilityManager.GetAppKeyValue( "scheduleType", "daily" );
			registryImport.StartingDate = DateTime.Now.AddDays( -1 ).ToString();
			registryImport.EndingDate = "";
			//===============================================================================
			int deleteAction = UtilityManager.GetAppKeyValue( "deleteAction", 0 );
			registryImport.DoingDownloadOnly = UtilityManager.GetAppKeyValue( "DoingDownloadOnly", false );
			if ( registryImport.DoingDownloadOnly )
				LoggingHelper.DoTrace( 1, "********* DOING DOWNLOAD ONLY *********" );


			#region	check for use of a graph search filter
            string searchToolType = UtilityManager.GetAppKeyValue( "searchToolType", "fullTextSearch" );
			if ( searchToolType == "graphSearch" )
			{
				//NOTE: will still need an option for handling deletes!
				ImportUsingGraphSearch( defaultCommunity, registryImport.DoingDownloadOnly );
				
				if ( deleteAction < 2 )
				{
					//handle deleted records
					importResults = importResults + "<br/>" + HandleDeletes( defaultCommunity, registryImport.StartingDate, registryImport.EndingDate, registryImport.MaxImportRecords, ref recordsDeleted );
				}

				return;
			}
            #endregion


			#region  Import Type/ schedule Arguments
			//consider parameters to override using importPending - especially for deletes
			if ( args != null )
			{
				//consider using args with qualifiers: scheduleType:10, community:xxxx, deleteOnly:true 
				//		or flags -c community -s schedule -d deleteOnly
				if ( args.Length >= 1 )
					scheduleType = args[ 0 ];

				if ( args.Length == 2 )
				{
					//
					var altCommunity = args[ 1 ];
					if ( !string.IsNullOrWhiteSpace( altCommunity ) && altCommunity.ToLower() == additionalCommunity.ToLower() )
					{
						//has to match additional to be valid
						defaultCommunity = additionalCommunity;
					}
				}
			}			
			

			//could ignore end date until a special scedule type of adhoc is used, then read the dates from config
			importResults = RegistryImport.DisplayMessages( string.Format( " - Schedule type: {0} ", scheduleType ) );
			int minutes = 0;

			if ( Int32.TryParse( scheduleType, out minutes ) )
			{
				//minutes
				//may want more flexibility and use input parms
				if ( minutes < 1 || minutes > 1440 ) //doesn't really matter
				{
					RegistryImport.DisplayMessages( string.Format( "invalid value encountered for Minutes option: {0} - defaulting to 60.", scheduleType ) );
					minutes = 60;
				}
				if ( usingUTC_ForTime )
				{
					//registry is UTC, so make adjustments
					minutes = minutes * -1;
					//registryImport.StartingDate = DateTime.Now.AddMinutes( minutes ).ToString( "yyyy-MM-ddTHH:mm:ss" );
					registryImport.StartingDate = zone.ToUniversalTime( DateTime.Now.AddMinutes( minutes ) ).ToString( "yyyy-MM-ddTHH:mm:ss" );
					//no end date?
					registryImport.EndingDate = "";
				}
				else
				{
					registryImport.StartingDate = DateTime.Now.AddMinutes( -minutes ).ToString( "yyyy-MM-ddTHH:mm:ss" );
					//the server date is UTC, so if we leave enddate open, we will get the same stuff all day, so setting an endate to the current hour
					registryImport.EndingDate = DateTime.Now.ToString( "yyyy-MM-ddTHH:mm:ss" );
				}
				importResults = importResults + "<br/>" + RegistryImport.DisplayMessages( string.Format( " - Community: {0}, Updates since: {1} {2}", defaultCommunity, registryImport.StartingDate, usingUTC_ForTime ? " (UTC)" : "" ) );
			}
			else if ( scheduleType == "sinceLastRun" )
			{
				SiteActivity lastRun = ActivityServices.GetLastImport();
				if ( usingUTC_ForTime )
				{
					registryImport.StartingDate = zone.ToUniversalTime( lastRun.Created ).ToString( "yyyy-MM-ddTHH:mm:ss" );
				}
				else
				{
					registryImport.StartingDate = lastRun.Created.ToString( "yyyy-MM-ddTHH:mm:ss" );
				}
				registryImport.EndingDate = "";
				importResults = importResults + "<br/>" + RegistryImport.DisplayMessages( string.Format( " - Updates since: {0} {1}", registryImport.StartingDate, usingUTC_ForTime ? " (UTC)" : "" ) );
			}
			else if ( scheduleType == "adhoc" )
			{
				registryImport.StartingDate = UtilityManager.GetAppKeyValue( "startingDate", "" );
				registryImport.EndingDate = UtilityManager.GetAppKeyValue( "endingDate", "" );
				DateTime dtcheck = System.DateTime.Now;             //LoggingHelper.DoTrace( 1, string.Format( " - Updates from: {0} to {1} ", registryImport.StartingDate, registryImport.EndingDate ) );

				if ( usingUTC_ForTime )
				{
					if ( DateTime.TryParse( registryImport.StartingDate, out dtcheck ) )
					{
						registryImport.StartingDate = zone.ToUniversalTime( dtcheck ).ToString( "yyyy-MM-ddTHH:mm:ss" );
					}
					if ( DateTime.TryParse( registryImport.EndingDate, out dtcheck ) )
					{
						registryImport.EndingDate = zone.ToUniversalTime( dtcheck ).ToString( "yyyy-MM-ddTHH:mm:ss" );
					}
					//no end date?
					//registryImport.EndingDate = "";
				}
				importResults = importResults + "<br/>" + RegistryImport.DisplayMessages( string.Format( " - Updates from: {0} to {1} for community: {2}", registryImport.StartingDate, registryImport.EndingDate, defaultCommunity ) );
			}
			else if ( scheduleType == "hourly" )
			{
				if ( usingUTC_ForTime )
				{
					//6 hour diff, so add 5 hours, equiv to +6 hours - 1 hour
					registryImport.StartingDate = zone.ToUniversalTime( DateTime.Now.AddHours( -1 ) ).ToString( "yyyy-MM-ddTHH:mm:ss" );
				}
				else
				{
					registryImport.StartingDate = DateTime.Now.AddHours( -1 ).ToString( "yyyy-MM-ddTHH:mm:ss" );
					//format into: 2016-08-01T23:59:59
					//the server date is UTC, so if we leave enddate open, we will get the same stuff all day, so setting an endate to the current hour
					//HOWEVER - THIS COULD RESULT IN BEING 6 HOURS BEHIND
					registryImport.EndingDate = DateTime.Now.ToString( "yyyy-MM-ddTHH:mm:ss" );
				}
				//LoggingHelper.DoTrace( 1, string.Format( " - Updates since: {0} ", registryImport.StartingDate ) );
				importResults = importResults + "<br/>" + RegistryImport.DisplayMessages( string.Format( " - Updates since: {0} {1}, community: {2}", registryImport.StartingDate, usingUTC_ForTime ? " (UTC)" : "", defaultCommunity ) );
			}
			else
			{
				//assume daily
				registryImport.StartingDate = DateTime.Now.AddDays( -1 ).ToString( "yyyy-MM-ddTHH:mm:ss" );
				//format into: 2016-08-01T23:59:59
				registryImport.EndingDate = "";
				//LoggingHelper.DoTrace( 1, string.Format( " - Updates since: {0} ", registryImport.StartingDate ) );
				importResults = importResults + "<br/>" + RegistryImport.DisplayMessages( string.Format( " - Updates since: {0} ", registryImport.StartingDate ) );
			}
			#endregion
			//===================================================================================================
			if ( !registryImport.DoingDownloadOnly )
				LogStart();
			List<string> importSummary = new List<string>();

			//NOTE - NEED TO REBUILD CACHE TABLES BEFORE BUILDING ELASTIC INDICES


			//establish common filters
			//NEW - filter sets
			var filterSets = UtilityManager.GetAppKeyValue( "filterSets" );
			//if you always only want to download documents for a particular set of organizations, provide the CTIDs for 'owningOrganizationCTID' in the app.config.
			registryImport.OwningOrganizationCTID = UtilityManager.GetAppKeyValue( "owningOrganizationCTID" );

			//if you want to download documents published by a third party publisher, provide the CTID for 'publishingOrganizationCTID' in the app.config. 
			//NOTE: where the publisher and the owner are the same, there is no need to provide both the owning and publishing org filters, just pick one.
			registryImport.PublishingOrganizationCTID = UtilityManager.GetAppKeyValue( "publishingOrganizationCTID" );
			//
			registryImport.ResourceCTIDList = UtilityManager.GetAppKeyValue( "resourcesCTIDList" );

			var hasCustomRun = false;
			if ( registryImport.PublishingOrganizationCTID.Length > 0 || registryImport.OwningOrganizationCTID.Length > 0 || registryImport.ResourceCTIDList.Length > 0 )
			{
				hasCustomRun = true;
			}
			//Actions: 0-normal; 1-DeleteOnly; 2-SkipDelete 
			if ( deleteAction < 2 )
			{
				//handle deleted records
				importResults = importResults + "<br/>" + HandleDeletes( defaultCommunity, registryImport.StartingDate, registryImport.EndingDate, registryImport.MaxImportRecords, ref recordsDeleted );
			}
			//int recordsImported = 0;
			if ( deleteAction == 1 )
				return;

			//****NOTE for the appKey of importing_entity_type, the entity_type must match the resource type in the registry -	NO plurals
			//																													**********
			//
			//22-04-25 mp - move to a similar approach to the download
			//
			var resourceTypeList = GetRequestedResourceTypes();
			//
			#region ==== Option for a list of data publishers or data owners
			//TODO - first check for filter sets.
			//	- for generic processing, if no filter set, treat the default publisher and owner filters like filters and add to filter set
			PublisherRelatedImport( registryImport, registryImport.PublishingOrganizationCTID, resourceTypeList );

			DataOwnerRelatedImport( registryImport, registryImport.OwningOrganizationCTID, resourceTypeList );
			//
			AdhocResourceListImport( registryImport, registryImport.ResourceCTIDList );

			#endregion
			//take approach that if either the publisher or owner was provided, then the general method should be skipped.
			//Will need to update this with filter sets
			if ( !hasCustomRun )
			{
				//22-04-12 NEW - just use a list from app.config
				//TBD - need to make this customizable - simply check for non blank. Although blank could be considered to be all?
				if ( UtilityManager.GetAppKeyValue( "usingListOfResourceTypes", false ) )
				{
					int entityTypeId = 0;
					foreach ( var resourceType in resourceTypeList )
					{
						registryImport.ImportNew( resourceType, entityTypeId );
						//ImportNew( string registryEntityType, int entityTypeId, int maxRecords, bool downloadOnly, ref int recordsImported, ref List<string> importSummary, string sortOrder = "asc" )
					}
				}
				else
				{
					//OR
					OldWordyImport( registryImport, registryImport.MaxImportRecords, registryImport.DoingDownloadOnly, ref importResults );
				}
			}
				
				
			if ( !registryImport.DoingDownloadOnly && registryImport.RecordsImported > 0 )
			{
				//TODO - will partners want a handling of pending? 
				//	==> should be handled by regular downloads?
				if ( UtilityManager.GetAppKeyValue( "processingPendingRecords", true ) )
				{
					//==============================================================
					//import pending
					string pendingStatus = new RegistryServices().ImportPending();

					importResults = importResults + "<br/>TODO: add stats from ImportPending.";
				}

			}

			//===================================================================================================
			if ( !registryImport.DoingDownloadOnly )
			{
				if ( registryImport.RecordsImported > 0 || recordsDeleted > 0 )
				{
					//2023 - need to update addresses first (geocode) or elastic will be off
					if ( UtilityManager.GetAppKeyValue( "doingGeoCodingImmediately", false ) == false && UtilityManager.GetAppKeyValue( "skippingGeoCodingCompletely", false ) == false )
					{
						ProfileServices.HandleAddressGeoCoding();
					}
					//update elastic if not included - probably will always delay elastic, due to multiple possible updates
					//may want to move this to services for use by other process, including adhoc imports
					if ( UtilityManager.GetAppKeyValue( "delayingAllCacheUpdates", true ) )
					{
						//update elastic if a elasticSearchUrl exists
						if ( UtilityManager.GetAppKeyValue( "elasticSearchUrl" ) != "" && UtilityManager.GetAppKeyValue( "updateCredIndexAction", 0 ) > 0 )
						{
							LoggingHelper.DoTrace( 1, string.Format( "===  *****************  UpdateElastic  ***************** " ) );
							ElasticHelper.UpdateElastic( false, true );
						}
					}


					//send email to accounts admin
					//harder to do here to the adhoc options
					//ImportPendingRequests.SendSummaryEmail( null, registryImport.StartingDate, registryImport.EndingDate );

					if ( registryImport.RecordsImported > 0 )
					{
						//set all resolved records in Import_EntityResolution to be resolved.
						LoggingHelper.DoTrace( 1, string.Format( "===  *****************  SetAllResolvedEntities  ***************** " ) );
						new ImportManager().SetAllResolvedEntities();
					}

					//update code table counts - maybe
					LoggingHelper.DoTrace( 1, string.Format( "===  *****************  UpdateCodeTableCounts  ***************** " ) );
					if ( UtilityManager.GetAppKeyValue( "doingPropertyCounts", false ) )
						new CacheManager().UpdateCodeTableCounts();

					//send summary email 
					string message = string.Format( "<h2>Import Results</h2><p>{0}</p>", importResults );
					EmailManager.NotifyAdmin( string.Format( "Credential Finder Import Results ({0})", envType ), message );
					new ActivityServices().AddActivity( new SiteActivity()
					{ ActivityType = "System", Activity = "Import", Event = "End", Comment = string.Format( "Summary: {0} records were imported, {1} records were deleted.", registryImport.RecordsImported, recordsDeleted ), SessionId = "batch job", IPAddress = "local" } );
				}
				else
				{
					new ActivityServices().AddActivity( new SiteActivity()
					{ ActivityType = "System", Activity = "Import", Event = "End", Comment = "No data was found to import", SessionId = "batch job", IPAddress = "local" } );
				}

			}

			
		}
		public static void OldWordyImport( RegistryImport registryImport, int maxImportRecords, bool doingDownloadOnly, ref string importResults )
		{
			int recordsImported = 0;
			//handle organizations
			//might be better to do last, then can populate placeholders, try first
			//
			if ( UtilityManager.GetAppKeyValue( "importing_organization", true ) )
				importResults = importResults + "<br/>" + registryImport.Import( "organization", 2, registryImport.StartingDate, registryImport.EndingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );

			if ( UtilityManager.GetAppKeyValue( "importing_competency_framework", true ) )
			{
				//🛺🛺🛺importResults = importResults + "<br/>" + new CompetencyFramesworksImport().Import( registryImport.StartingDate, registryImport.EndingDate, maxImportRecords, defaultCommunity, doingDownloadOnly );
				//
				importResults = importResults + "<br/>" + registryImport.Import( "competency_framework", CodesManager.ENTITY_TYPE_COMPETENCY_FRAMEWORK, registryImport.StartingDate, registryImport.EndingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );
			}
			//
			if ( UtilityManager.GetAppKeyValue( "importing_concept_scheme", true ) )
			{
				importResults = importResults + "<br/>" + registryImport.Import( "concept_scheme", CodesManager.ENTITY_TYPE_CONCEPT_SCHEME, registryImport.StartingDate, registryImport.EndingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );
			}
			if ( UtilityManager.GetAppKeyValue( "importing_collection", true ) )
			{
				importResults = importResults + "<br/>" + registryImport.Import( "collection", CodesManager.ENTITY_TYPE_COLLECTION, registryImport.StartingDate, registryImport.EndingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );
			}
			//TVP 
			if ( UtilityManager.GetAppKeyValue( "importing_transfer_value_profile", true ) )
			{
				string sortOrder = "asc";//WHY
				importResults = importResults + "<br/>" + registryImport.Import( "transfer_value_profile", CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, registryImport.StartingDate, registryImport.EndingDate, maxImportRecords, doingDownloadOnly, ref recordsImported, sortOrder );
			}
			if ( UtilityManager.GetAppKeyValue( "importing_transfer_intermediary", true ) )
			{
				string sortOrder = "asc";//WHY
				importResults = importResults + "<br/>" + registryImport.Import( "transfer_intermediary", CodesManager.ENTITY_TYPE_TRANSFER_INTERMEDIARY, registryImport.StartingDate, registryImport.EndingDate, maxImportRecords, doingDownloadOnly, ref recordsImported, sortOrder );
			}

			//pathways 
			//should we try to combine pathways and pathway sets?
			if ( UtilityManager.GetAppKeyValue( "importing_pathway", true ) )
			{
				importResults = importResults + "<br/>" + registryImport.Import( "pathway", CodesManager.ENTITY_TYPE_PATHWAY, registryImport.StartingDate, registryImport.EndingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );
			}
			//
			if ( UtilityManager.GetAppKeyValue( "importing_pathway_set", true ) )
			{
				//can't do this until registry fixture is updated.
				importResults = importResults + "<br/>" + registryImport.Import( "pathway_set", CodesManager.ENTITY_TYPE_PATHWAY_SET, registryImport.StartingDate, registryImport.EndingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );
			}

			//do manifests 
			if ( UtilityManager.GetAppKeyValue( "importing_condition_manifest_schema", true ) )
				importResults = importResults + "<br/>" + registryImport.Import( "condition_manifest_schema", CodesManager.ENTITY_TYPE_CONDITION_MANIFEST, registryImport.StartingDate, registryImport.EndingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );
			//
			if ( UtilityManager.GetAppKeyValue( "importing_cost_manifest_schema", true ) )
				importResults = importResults + "<br/>" + registryImport.Import( "cost_manifest_schema", CodesManager.ENTITY_TYPE_COST_MANIFEST, registryImport.StartingDate, registryImport.EndingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );

			//handle assessments
			//
			if ( UtilityManager.GetAppKeyValue( "importing_assessment_profile", true ) )
				importResults = importResults + "<br/>" + registryImport.Import( "assessment_profile", CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, registryImport.StartingDate, registryImport.EndingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );

			//handle learning opps
			//21-08-09 include course and learning program
			if ( UtilityManager.GetAppKeyValue( "importing_learning_opportunity_profile", true ) )
			{
				importResults = importResults + "<br/>" + registryImport.Import( "learning_opportunity_profile", CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, registryImport.StartingDate, registryImport.EndingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );
				//
				importResults = importResults + "<br/>" + registryImport.Import( "learning_program", CodesManager.ENTITY_TYPE_LEARNING_PROGRAM, registryImport.StartingDate, registryImport.EndingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );
				//
				importResults = importResults + "<br/>" + registryImport.Import( "course", CodesManager.ENTITY_TYPE_COURSE, registryImport.StartingDate, registryImport.EndingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );
			}
			//datasetprofile
			if ( UtilityManager.GetAppKeyValue( "importing_qdata_dataset_profile", true ) )
			{
				//can't do this until registry fixture is updated.
				importResults = importResults + "<br/>" + registryImport.Import( "qdata_dataset_profile", CodesManager.ENTITY_TYPE_DATASET_PROFILE, registryImport.StartingDate, registryImport.EndingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );
			}

			//handle credentials
			//
			if ( UtilityManager.GetAppKeyValue( "importing_credential", true ) )
				importResults = importResults + "<br/>" + registryImport.Import( "credential", CodesManager.ENTITY_TYPE_CREDENTIAL, registryImport.StartingDate, registryImport.EndingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );
			//
			if ( UtilityManager.GetAppKeyValue( "importing_occupation_profile", true ) )
				importResults = importResults + "<br/>" + registryImport.Import( "occupation_profile", CodesManager.ENTITY_TYPE_OCCUPATIONS_PROFILE, registryImport.StartingDate, registryImport.EndingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );
			//
			if ( UtilityManager.GetAppKeyValue( "importing_job_profile", true ) )
				importResults = importResults + "<br/>" + registryImport.Import( "job_profile", CodesManager.ENTITY_TYPE_JOB_PROFILE, registryImport.StartingDate, registryImport.EndingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );
		}

		public static void PublisherRelatedImport( RegistryImport registryImport, string publishersList, List<string> resourceTypeList )
		{
			var currentTotal = registryImport.RecordsImported;

			var publishedByList = new List<string>();
			if ( publishersList?.Length > 0 )
			{
				if ( publishersList.IndexOf( "," ) > 0 )
				{
					//get list
					var requestlist = publishersList.Split( ',' );
					foreach ( var item in requestlist )
					{
						if ( !string.IsNullOrWhiteSpace( item ) )
							publishedByList.Add( item.Trim() );
					}
				}
				else
				{
					publishedByList.Add( publishersList );
				}
				//process
				//TBD
				int eTypeId = 0;
				foreach ( var item in publishedByList )
				{
					LoggingHelper.DoTrace( 1, string.Format( "===  *****************  Downloading all recent recources for Publishing Org: {0}  ***************** ", item ) );
					registryImport.PublishingOrganizationCTID = item;
					//WARNING - IF NO RESOURCE TYPE IS INCLUDED WILL GET ceasn:Competency as well. This is not terrible as these will result in competency framework file being overwritten (so only one), but slows the process
					//may want to get the list of target resources. We would eventually have similar issues with pathway components;
					foreach ( var resourceType in resourceTypeList )
					{
						registryImport.ImportNew( resourceType, eTypeId );
					}
				}

				LoggingHelper.DoTrace( 1, string.Format( "Completed download request. Resources:{0}", registryImport.RecordsImported - currentTotal ) );
				//clear publisher - THIS WILL AFFECT STOPPING
				registryImport.PublishingOrganizationCTID = "";
			}
		}

		public static void DataOwnerRelatedImport( RegistryImport registryImport, string ownersList, List<string> resourceTypeList )
		{
			var importList = new List<string>();
			var currentTotal = registryImport.RecordsImported;
			if ( ownersList?.Length > 0 )
			{
				if ( ownersList.IndexOf( "," ) > 0 )
				{
					//get list
					var requestlist = ownersList.Split( ',' );
					foreach ( var item in requestlist )
					{
						if ( !string.IsNullOrWhiteSpace( item ) )
							importList.Add( item.Trim() );
					}
				}
				else
				{
					importList.Add( ownersList );
				}
				//process
				//TBD
				int eTypeId = 0;
				int cntr = 0;
				foreach ( var item in importList )
				{
					cntr++;
					LoggingHelper.DoTrace( 1, string.Format( "===  *****************  Downloading all recent selected recources for DataOwner Org: {0}  ***************** ", item ) );
					registryImport.OwningOrganizationCTID = item;
					foreach ( var resourceType in resourceTypeList )
					{
						registryImport.ImportNew( resourceType, eTypeId );
					}
				}
				//may want summaries for each filter set?
				LoggingHelper.DoTrace( 1, string.Format( "Completed DataOwnerRelated download request. Resources:{0}", registryImport.RecordsImported - currentTotal ) );
				//clear owner - THIS WILL AFFECT STOPPING
				registryImport.OwningOrganizationCTID = "";
			}
		}
		//

		public static void AdhocResourceListImport( RegistryImport registryImport, string resourceList )
		{
			var importList = new List<string>();
			var currentTotal = registryImport.RecordsImported;
			if ( resourceList == null || resourceList.Trim().Length == 0 )
				return;

			if ( resourceList.IndexOf( "," ) > 0 )
			{
				//get list
				var requestlist = resourceList.Split( ',' );
				foreach ( var item in requestlist )
				{
					if ( !string.IsNullOrWhiteSpace( item ) )
						importList.Add( item.Trim() );
				}
			}
			else
			{
				importList.Add( resourceList );
			}
			//process
			string statusMessage = "";
			string ctdlType = "";
			string community = "";
			int cntr = 0;
			foreach ( var item in importList )
			{
				cntr++;
				LoggingHelper.DoTrace( 1, string.Format( "{0}.  *****************  Downloading CTID: {1}  ***************** ", cntr, item ) );
				
				//get the envelope
				var envelope = RegistryServices.GetEnvelopeByCtid( item, ref statusMessage, ref ctdlType, community );
				var entityTypeId = MappingHelperV3.GetEntityTypeId( envelope.EnvelopeCtdlType );
				var registryEntityType = ctdlType;
				registryImport.ProcessEnvelope( envelope, registryEntityType, cntr, false );
			}
			//may want summaries for each filter set?
			LoggingHelper.DoTrace( 1, string.Format( "Completed Adhoc resource download request. Resources:{0}", cntr ) );
			//clear source - THIS WILL AFFECT STOPPING
			registryImport.ResourceCTIDList = "";
			
		}


		public static List<string> GetRequestedResourceTypes()
		{
			var resourceTypeList = new List<string>();
			var resourceTypeSelections = UtilityManager.GetAppKeyValue( "resourceTypeList", "" );
			if ( string.IsNullOrWhiteSpace( resourceTypeSelections ) )
				return resourceTypeList;
			//
			var list = resourceTypeSelections.Split( ',' );
			foreach ( var item in list )
			{
				if ( !string.IsNullOrWhiteSpace( item ) )
					resourceTypeList.Add( item.Trim() );
			}
			return resourceTypeList;
		}
		public static void ImportUsingGraphSearch( string defaultCommunity, bool doingDownloadOnly )
		{
			//require a query file name. Or other alternatives?
			string queryFileName  = UtilityManager.GetAppKeyValue( "queryFileName", "" );
			if ( string.IsNullOrWhiteSpace( queryFileName ) )
			{
				//must have at least one file. More TBD
				LoggingHelper.DoTrace( 1, "********************* ERROR ********************* " );
				LoggingHelper.DoTrace( 1, "* When selecting the graph search option, a query file name (path/file) that contains the graph query) must be provided. *" );
				LoggingHelper.DoTrace( 1, "********************* ERROR ********************* " );
				return;
			}

			var registryImport = new RegistryImport( defaultCommunity );
			//***add constructor or assign here
			registryImport.GraphSearchQuery = queryFileName;

			LoggingHelper.DoTrace( 1, "********************* WARNING  ********************* " );
			LoggingHelper.DoTrace( 1, "* The graph search option is still under construction.  *" );
			LoggingHelper.DoTrace( 1, "********************* WARNING ********************* " );
			//TODO - could provide this in each call. Then can use the current import method but call a different method if a queryFileName is provided?
			//could add to the constructor for RegistryImport, and then don't have to add to each call to the Import?? 

			if ( !File.Exists( queryFileName ) )
			{
				LoggingHelper.DoTrace( 1, "********************* QUERY FILE NOT FOUND	********************* " );
				LoggingHelper.DoTrace( 1, "* The provided query file was not found: " + queryFileName );
				LoggingHelper.DoTrace( 1, "*********************		EXITING			********************* " );
			}

			// Open the file to read from.
			string jsonQuery = File.ReadAllText( queryFileName );
			if ( string.IsNullOrWhiteSpace( jsonQuery ))
			{
				LoggingHelper.DoTrace( 1, "********************* QUERY FILE IS EMPTY	********************* " );
				LoggingHelper.DoTrace( 1, "* The provided query file was found but contains no data: " + queryFileName );
				LoggingHelper.DoTrace( 1, "*********************		EXITING			********************* " );
			}
			registryImport.GraphSearchQuery = jsonQuery;

			int skip = 0;
			int take = 50;
			int pTotalRows = 0;
			string statusMessage = "";
			string community = "";
			string sortOrder = "asc";
			var importResults = "";
			var maxImportRecords = 0;
			var recordsImported = 0;
			//var list = GraphSearchByTemplate( jsonQuery, skip, take, ref pTotalRows, ref statusMessage, community, sortOrder );
			importResults = importResults + "<br/>" + registryImport.ImportByGraphSearch( maxImportRecords, doingDownloadOnly, ref recordsImported );

		}
		public static void LogStart()
		{
			new ActivityServices().AddActivity( new SiteActivity()
			{ ActivityType = "System", Activity = "Import", Event = "Start", SessionId = "batch job", IPAddress = "local" }
			);

		}
		/// <summary>
		/// Handle deleted records for the requested time period
		/// </summary>
		/// <param name="community"></param>
		/// <param name="startingDate">Date must be in UTC</param>
		/// <param name="registryImport.EndingDate">Date must be in UTC</param>
		/// <param name="maxRecords"></param>
		/// <param name="recordsDeleted"></param>
		/// <returns></returns>
		public static string HandleDeletes( string community, string startingDate, string endingDate, int maxRecords, ref int recordsDeleted )
		{
			int pageNbr = 1;
			int pageSize = 100;
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
			var deleteService = new ImportUtilities();
			LoggingHelper.DoTrace( 1, string.Format( "===  DELETE Check for: '{0}' to '{1}' ===", startingDate, endingDate ) );
			//startingDate = "2017-10-29T00:00:00";
			try
			{
				while ( pageNbr > 0 && !isComplete )
				{
					list = RegistryServices.GetDeleted( community, type, startingDate, endingDate, pageNbr, pageSize, ref pTotalRows, ref statusMessage );

					if ( list == null || list.Count == 0 )
					{
						isComplete = true;
						if ( pageNbr == 1 )
						{
							importNote = "Deletes: No records where found for date range ";
							//Console.WriteLine( thisClassName + ".HandleDeletes() - " + importNote );
							LoggingHelper.DoTrace( 1, thisClassName + string.Format( ".HandleDeletes() Community: {0} - ", community ) + importNote );
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
						//LoggingHelper.DoTrace( 5, "		envelopeUrl: " + envelopeUrl );

						//LoggingHelper.DoTrace( 6, string.Format( "{0}. EnvelopeIdentifier: {1} ", cntr, item.EnvelopeIdentifier ) );
						try
						{
							//only need the envelopeId and type
							//so want a full delete, or set EntityStateId to 0 - just as a precaution
							messages = new List<string>();
							status = new SaveStatus();
							status.ValidationGroup = "Deletes";

							//
							//Program.HandleDeleteRequest( cntr, item.EnvelopeIdentifier, ctid, ctdlType, ref statusMessage );
							deleteService.HandleDeleteRequest( cntr, ctid, ctdlType, ref statusMessage );

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

					if ( ( maxRecords > 0 && cntr > maxRecords ) || cntr > pTotalRows )
					{
						isComplete = true;
						RegistryImport.DisplayMessages( string.Format( "Delete EARLY EXIT. Completed {0} records out of a total of {1} ", cntr, pTotalRows ) );
					}
					pageNbr++;
				} //while
				  //delete from elastic
				if ( cntr > 0 )
				{
					messages = new List<string>();
					ElasticHelper.HandlePendingDeletes( ref messages );
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
		/// Moved to ProfileServices for better solution access
		/// TBD or maybe better to have a clone here for github project?
		/// </summary>
		/// <param name="cntr"></param>
		/// <param name="envelopeIdentifier"></param>
		/// <param name="ctid"></param>
		/// <param name="ctdlType"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		[Obsolete]
		public static bool HandleDeleteRequest( int cntr, string envelopeIdentifier, string ctid, string ctdlType, ref string statusMessage )
		{
			statusMessage = "";
			List<string> messages = new List<string>();

			bool isValid = true;
			RegistryImport.DisplayMessages( string.Format( "{0}. Deleting {1} by ctid: {2} ", cntr, ctdlType, ctid ) );

			switch ( ctdlType.ToLower() )
			{
				case "credentialorganization":
				case "qacredentialorganization":
				case "organization":
					if ( !new OrganizationManager().Delete( ctid, ref statusMessage ) )
						RegistryImport.DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;

				case "assessmentprofile":
				case "assessment":
					if ( !new AssessmentManager().Delete( ctid, ref statusMessage ) )
						RegistryImport.DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
				case "learningopportunityprofile":
				case "learningopportunity":
				case "learningprogram":
				case "course":
					if ( !new LearningOpportunityManager().Delete( ctid, ref statusMessage ) )
						RegistryImport.DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
				case "conditionmanifest":
					if ( !new ConditionManifestManager().Delete( ctid, ref statusMessage ) )
					{
						RegistryImport.DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					}
					break;
				case "costmanifest":
					if ( !new CostManifestManager().Delete( ctid, ref statusMessage ) )
						RegistryImport.DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
				case "collection":
				case "ceterms:collection":
					if ( !new CollectionManager().Delete( ctid, ref statusMessage ) )
						RegistryImport.DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;

				case "competencyframework": //CompetencyFramework
					if ( !new CompetencyFrameworkManager().Delete( ctid, ref statusMessage ) )
						RegistryImport.DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
				case "conceptscheme":
				case "skos:conceptscheme":
				case "progressionmodel":
				case "asn:progressionmodel":
					if ( !new ConceptSchemeManager().Delete( ctid, ref statusMessage ) )
						RegistryImport.DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
				//
				case "datasetprofile":
				case "qdata:datasetprofile":
					if ( !new DataSetProfileManager().Delete( ctid, ref messages ) )
						RegistryImport.DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
				//
				case "pathway":
					if ( !new PathwayManager().Delete( ctid, ref statusMessage ) )
						RegistryImport.DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
				case "pathwayset":
					if ( !new PathwaySetManager().Delete( ctid, ref statusMessage ) )
						RegistryImport.DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
				case "transfervalueprofile":
					if ( !new TransferValueProfileManager().Delete( ctid, ref statusMessage ) )
						RegistryImport.DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
				case "transferintermediary":
					if ( !new TransferIntermediaryManager().Delete( ctid, ref statusMessage ) )
						RegistryImport.DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
				case "job":
					if ( !new JobManager().Delete( ctid, ref statusMessage ) )
						RegistryImport.DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
				case "occupation":
					if ( !new OccupationManager().Delete( ctid, ref statusMessage ) )
						RegistryImport.DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
				case "task":
					if ( !new TaskManager().Delete( ctid, ref statusMessage ) )
						RegistryImport.DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
				case "workrole":
					if ( !new WorkRoleManager().Delete( ctid, ref statusMessage ) )
						RegistryImport.DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
				default:
					//default to credential
					//RegistryImport.DisplayMessages( string.Format( "{0}. Deleting Credential ({1}) by ctid: {2} ", cntr, ctdlType, ctid ) );
					if ( !new CredentialManager().Delete( ctid, ref statusMessage ) )
						RegistryImport.DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
			}

			if ( statusMessage.Length > 0 )
				isValid = false;

			return isValid;
		}

		#region Validation
		public static bool ValidateSetup()
		{
			//allow user to turn off validation step once all confirmed
			if ( !UtilityManager.GetAppKeyValue( "validatingSetup", true ) )
				return true;
			bool isValid = true;
			LoggingHelper.DoTrace( 1, " ========== Validating Setup ========== ");
			//Api key should always be required
			var apiKey = UtilityManager.GetAppKeyValue( "MyCredentialEngineAPIKey", "" );
			if ( string.IsNullOrWhiteSpace( apiKey ) || apiKey.Length != 36 )
			{
				LoggingHelper.DoTrace( 1, "********************* ERROR ********************* " );
				LoggingHelper.DoTrace( 1, string.Format( "* A valid Api key must be present in the app.config file for key: MyCredentialEngineAPIKey, current value: '{0}'", apiKey ) );
				LoggingHelper.DoTrace( 1, "********************* ERROR ********************* " );
				isValid = false;
			}
			else
			{
				//validate the api key. Consider how to avoid always doing this step?
				string ctid = "";
				List<string> messages = new List<string>();
				if ( !GetAccountOrganizationByApiKey( apiKey, ref ctid, ref messages ) )
				{
					//should have already displayed a message.
					LoggingHelper.DoTrace( 1, "********************* ERROR ********************* " );
					isValid = false;
				}
			}
			//check databases
			if ( !ValidateDatabaseAccess() )
			{
				//should have already displayed a message.
				LoggingHelper.DoTrace( 1, "********************* ERROR ********************* " );
				isValid = false;
			}
			if ( isValid ) {
				LoggingHelper.DoTrace( 1, "********************* VALIDATION SUCCESSFUL ********************* " );
				LoggingHelper.DoTrace( 1, "* Validation of setup completed succesfully. You can now set the appKey: validatingSetup to false * " );
			}
			return isValid;
		}
		/// <summary>
		/// Check if databases are properly set up
		/// </summary>
		/// <returns></returns>
		public static bool ValidateDatabaseAccess()
		{
			string filter = string.Format( "" );
			int pageNbr = 1;
			int pageSize = 100;
			int pTotalRows = 0;
			bool isValid = true;

			var list = CredentialManager.Search( filter, "", pageNbr, pageSize, ref pTotalRows );
			if ( list != null && list.Count == 1 )
			{
				if ( list[ 0 ].Name == "EXCEPTION ENCOUNTERED" )
				{
					LoggingHelper.DoTrace( 1, "Database check failed: " + list[ 0 ].Description );
					isValid = false;
				}
			} else
            {
				LoggingHelper.DoTrace( 1, String.Format("Database check using Credential search was successful. Total records: {0} ", pTotalRows) );
			}

			var status = "";
			if ( UtilityManager.GetAppKeyValue( "writingToDownloadResourceDatabase", false ) )
			{				
				if (!ExternalServices.VerifyDownloadDatabaseExists( ref status))
				{
					LoggingHelper.DoTrace( 1, "CredentialRegistryDownload Database check failed: " + status );
					isValid = false;
				}
			}
			status = "";
			if ( !ExternalServices.VerifyCEExternalExists( ref status ) )
			{
				LoggingHelper.DoTrace( 1, "CE_ExternalData Database check failed: " + status );
				isValid = false;
			} else
            {
				LoggingHelper.DoTrace( 1, String.Format( "The database 'CE_ExternalData' appears to exist" ) );
			}
			return isValid;
		} 
		
		public static bool GetAccountOrganizationByApiKey( string apiKey, ref string ctid, ref List<string> messages )
		{
			//
			bool isValid = true;	
			//validate org apikey
			var getUrl = string.Format( UtilityManager.GetAppKeyValue( "ceAccountValidateOrganizationApiKey" ), apiKey );
			try
			{
				var rawData = new HttpClient().PostAsync( getUrl, null ).Result.Content.ReadAsStringAsync().Result;

				if ( rawData == null || rawData.IndexOf( "The resource cannot be found" ) > 0
				|| rawData.IndexOf( "\"PublishingApiKey\"" ) == -1 )
				{
					messages.Add( "Error: invalid apiKey: " + apiKey );
					LoggingHelper.DoTrace( 2, string.Format( "ceAccountValidateOrganizationApiKey. Error attempting to call method to return an org for apiKey: {0}. \r\n{1}", apiKey, rawData.ToString() ) );
					return false;
				}

				var results = new JavaScriptSerializer().Deserialize<GetOrgResult>( rawData );
				if ( results != null && results.data != null && results.data.CTID != null )
				{
					ctid = results.data.CTID.ToLower();
					if ( results.data.ApprovedPublishingRoles.Contains( "publishRole:TrustedPartner" ) )
					{
						//should already be set
						//doesn't matter for the finder
						//LoggingHelper.DoTrace( 5, "ceAccountValidateOrganizationApiKey() Found a trusted partner: " + results.data.Name );
					}
					//
				}
				else
				{
					messages.Add( "Error: was not able to find an organization for the provided apiKey: " + apiKey );
					LoggingHelper.DoTrace( 5, string.Format( thisClassName + ".ceAccountValidateOrganizationApiKey FAILED. NO ORG RETURNED! Organization apiKey: {0}", apiKey ) );
					return false;
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".ceAccountValidateOrganizationApiKey: " + apiKey );
				string message = LoggingHelper.FormatExceptions( ex );
				LoggingHelper.DoTrace( 5, message );
				messages.Add( message );
				return false;
			}


			return isValid;
		}  //


		#endregion
	}

	[Serializable]
	public class GetOrgResult
	{
		public AccountOrganization data { get; set; }
	}
	[Serializable]
	public class AccountOrganization
	{
		public string CTID { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public bool CanPublish { get; set; }
		public bool IsTrustedPartner { get; set; }
		public List<string> ApprovedPublishingMethods { get; set; } = new List<string>();
		public List<string> ApprovedPublishingRoles { get; set; } = new List<string>();
		public List<string> ApprovedConsumingMethods { get; set; } = new List<string>();

		public string PublicPrivateKey { get; set; }
		public string PublishingApiKey { get; set; }

	}

	[Serializable]
	public class StringCache
	{
		public StringCache()
		{
			lastUpdated = DateTime.Now;
		}
		public DateTime lastUpdated { get; set; }
		public string Item { get; set; }

	}
}

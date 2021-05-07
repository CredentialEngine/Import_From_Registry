using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Utilities;
using workIT.Factories;
using Import.Services;
using RegistryServices = Import.Services.RegistryServices;
using workIT.Models;
using workIT.Services;
using ElasticHelper = workIT.Services.ElasticServices;
namespace CTI.Import
{
	class Program
	{
		static string thisClassName = "Program";
        public static int maxExceptions = UtilityManager.GetAppKeyValue( "maxExceptions", 500 );
		public static string envType = UtilityManager.GetAppKeyValue( "envType");
		//
		//ImportCredential credImportMgr = new ImportCredential();
  //      ImportOrganization orgImportMgr = new ImportOrganization();
  //      ImportAssessment asmtImportMgr = new ImportAssessment();
  //      ImportLearningOpportunties loppImportMgr = new ImportLearningOpportunties();
  //      ImportConditionManifests cndManImportMgr = new ImportConditionManifests();
  //      ImportCostManifests cstManImportMgr = new ImportCostManifests();

        static void Main( string[] args )
		{
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
			if ( doingDownloadOnly )
				LoggingHelper.DoTrace( 1, "********* DOING DOWNLOAD ONLY *********" );

			string defaultCommunity = UtilityManager.GetAppKeyValue( "defaultCommunity" );
			string additionalCommunity = UtilityManager.GetAppKeyValue( "additionalCommunity" );

			#region  Import Type/Arguments
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


			RegistryImport registryImport = new RegistryImport( defaultCommunity );

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
				if ( minutes < 1 || minutes > 1440 ) //doesn't really matter
				{
					DisplayMessages( string.Format( "invalid value encountered for Minutes option: {0} - defaulting to 60.", scheduleType ) );
					minutes = 60;
				}
				if ( usingUTC_ForTime )
				{
					//registry is UTC, so make adjustments
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
				importResults = importResults + "<br/>" + DisplayMessages( string.Format( " - Community: {0}, Updates since: {1} {2}", defaultCommunity, startingDate, usingUTC_ForTime ? " (UTC)" : "" ) );
			}
			else if ( scheduleType == "sinceLastRun" )
			{
				SiteActivity lastRun = ActivityServices.GetLastImport();
				if ( usingUTC_ForTime )
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
			else if ( scheduleType == "adhoc" )
			{
				startingDate = UtilityManager.GetAppKeyValue( "startingDate", "" );
				endingDate = UtilityManager.GetAppKeyValue( "endingDate", "" );
				DateTime dtcheck = System.DateTime.Now;             //LoggingHelper.DoTrace( 1, string.Format( " - Updates from: {0} to {1} ", startingDate, endingDate ) );

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
				importResults = importResults + "<br/>" + DisplayMessages( string.Format( " - Updates from: {0} to {1} for community: {2}", startingDate, endingDate, defaultCommunity ) );
			}
			else if ( scheduleType == "hourly" )
			{
				if ( usingUTC_ForTime )
				{
					//6 hour diff, so add 5 hours, equiv to +6 hours - 1 hour
					startingDate = zone.ToUniversalTime( DateTime.Now.AddHours( -1 ) ).ToString( "yyyy-MM-ddTHH:mm:ss" );
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
				importResults = importResults + "<br/>" + DisplayMessages( string.Format( " - Updates since: {0} {1}, community: {2}", startingDate, usingUTC_ForTime ? " (UTC)" : "", defaultCommunity ) );
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
			if ( !doingDownloadOnly )
				LogStart();
			//set to zero to handle all, or a number to limit records to process
			//partly for testing
			//although once can sort by date, we can use this, and update the start date
			int maxImportRecords = UtilityManager.GetAppKeyValue( "maxImportRecords", 0 );

			//NOTE - NEED TO REBUILD CACHE TABLES BEFORE BUILDING ELASTIC INDICES

			//Actions: 0-normal; 1-DeleteOnly; 2-SkipDelete 
			int recordsDeleted = 0;
			if ( deleteAction < 2 )
			{
				//handle deleted records
				importResults = importResults + "<br/>" + HandleDeletes( defaultCommunity, startingDate, endingDate, maxImportRecords, ref recordsDeleted );
			}
			int recordsImported = 0;
			if ( deleteAction != 1 )
			{
				//****NOTE for the appKey of importing_entity_type, the entity_type must match the resource type in the registry -	NO plurals
				//																													**********
				//


				//handle organizations
				//might be better to do last, then can populate placeholders, try first
				//
				if ( UtilityManager.GetAppKeyValue( "importing_organization", true ) )
					importResults = importResults + "<br/>" + registryImport.Import( "organization", 2, startingDate, endingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );

				if ( UtilityManager.GetAppKeyValue( "importing_competency_framework", true ) )
				{
					//🛺🛺🛺importResults = importResults + "<br/>" + new CompetencyFramesworksImport().Import( startingDate, endingDate, maxImportRecords, defaultCommunity, doingDownloadOnly );
					//
					importResults = importResults + "<br/>" + registryImport.Import( "competency_framework", CodesManager.ENTITY_TYPE_COMPETENCY_FRAMEWORK, startingDate, endingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );
				}
				//
				if ( UtilityManager.GetAppKeyValue( "importing_concept_scheme", true ) )
				{
					//
					importResults = importResults + "<br/>" + registryImport.Import( "concept_scheme", CodesManager.ENTITY_TYPE_CONCEPT_SCHEME, startingDate, endingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );
				}
				//TVP
				if ( UtilityManager.GetAppKeyValue( "importing_transfer_value_profile", true ) )
				{
					string sortOrder = "asc";
					//if ( UtilityManager.GetAppKeyValue("envType") == "development" && System.DateTime.Now.ToString( "yyyy-MM-dd" ) == "2020-07-20" )
					//{
					//	maxImportRecords = 50;
					//	sortOrder = "dsc";
					//}
					importResults = importResults + "<br/>" + registryImport.Import( "transfer_value_profile", CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, startingDate, endingDate, maxImportRecords, doingDownloadOnly, ref recordsImported, sortOrder );
				}


				//pathways 
				//should we try to combine pathways and pathway sets?
				if ( UtilityManager.GetAppKeyValue( "importing_pathway", true ) )
				{
					importResults = importResults + "<br/>" + registryImport.Import( "pathway", CodesManager.ENTITY_TYPE_PATHWAY, startingDate, endingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );
				}
				//
				if ( UtilityManager.GetAppKeyValue( "importing_pathway_set", true ) )
				{
					//can't do this until registry fixture is updated.
					importResults = importResults + "<br/>" + registryImport.Import( "pathway_set", CodesManager.ENTITY_TYPE_PATHWAY_SET, startingDate, endingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );
				}

				//do manifests 
				if ( UtilityManager.GetAppKeyValue( "importing_condition_manifest_schema", true ) )
					importResults = importResults + "<br/>" + registryImport.Import( "condition_manifest_schema", CodesManager.ENTITY_TYPE_CONDITION_MANIFEST, startingDate, endingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );
				//
				if ( UtilityManager.GetAppKeyValue( "importing_cost_manifest_schema", true ) )
					importResults = importResults + "<br/>" + registryImport.Import( "cost_manifest_schema", CodesManager.ENTITY_TYPE_COST_MANIFEST, startingDate, endingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );

				//handle assessments
				//
				if ( UtilityManager.GetAppKeyValue( "importing_assessment_profile", true ) )
					importResults = importResults + "<br/>" + registryImport.Import( "assessment_profile", CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, startingDate, endingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );

				//handle learning opps
				//
				if ( UtilityManager.GetAppKeyValue( "importing_learning_opportunity_profile", true ) )
					importResults = importResults + "<br/>" + registryImport.Import( "learning_opportunity_profile", CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, startingDate, endingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );


				//handle credentials
				//
				if ( UtilityManager.GetAppKeyValue( "importing_credential", true ) )
					importResults = importResults + "<br/>" + registryImport.Import( "credential", CodesManager.ENTITY_TYPE_CREDENTIAL, startingDate, endingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );


				if ( !doingDownloadOnly && recordsImported > 0 )
				{
					if ( UtilityManager.GetAppKeyValue( "processingPendingRecords", true ) )
					{
						//==============================================================
						//import pending
						string pendingStatus = new RegistryServices().ImportPending();

						importResults = importResults + "<br/>TODO: add stats from ImportPending.";
					}

				}

				//===================================================================================================
				if ( !doingDownloadOnly )
				{
					if ( recordsImported > 0 || recordsDeleted > 0 )
					{
						//update elastic if not included - probably will always delay elastic, due to multiple possible updates
						//may want to move this to services for use by other process, including adhoc imports
						if ( UtilityManager.GetAppKeyValue( "delayingAllCacheUpdates", true ) )
						{
							//update elastic if a elasticSearchUrl exists
							if ( UtilityManager.GetAppKeyValue( "elasticSearchUrl" ) != "" )
							{
								LoggingHelper.DoTrace( 1, string.Format( "===  *****************  UpdateElastic  ***************** " ) );
								ElasticHelper.UpdateElastic( false, true );
							}
						}
						if ( !UtilityManager.GetAppKeyValue( "doingGeoCodingImmediately", false ) )
						{
							//have check in case want to skip geocoding sometimes
							if ( !UtilityManager.GetAppKeyValue( "skippingGeoCodingCompletely", false ) )
								ProfileServices.HandleAddressGeoCoding();
						}


						if ( recordsImported > 0 )
						{
							//set all resolved records in Import_EntityResolution to be resolved.
							LoggingHelper.DoTrace( 1, string.Format( "===  *****************  SetAllResolvedEntities  ***************** " ) );
							new ImportManager().SetAllResolvedEntities();
						}

						//update code table counts - maybe
						LoggingHelper.DoTrace( 1, string.Format( "===  *****************  UpdateCodeTableCounts  ***************** " ) );
						if ( UtilityManager.GetAppKeyValue( "doingPropertyCounts", true ) )
							new CacheManager().UpdateCodeTableCounts();

						//send summary email 
						string message = string.Format( "<h2>Import Results</h2><p>{0}</p>", importResults );
						EmailManager.NotifyAdmin( string.Format( "Credential Finder Import Results ({0})", envType ), message );
						new ActivityServices().AddActivity( new SiteActivity()
						{ ActivityType = "System", Activity = "Import", Event = "End", Comment = string.Format( "Summary: {0} records were imported, {1} records were deleted.", recordsImported, recordsDeleted ), SessionId = "batch job", IPAddress = "local" } );
					}
					else
					{
						new ActivityServices().AddActivity( new SiteActivity()
						{ ActivityType = "System", Activity = "Import", Event = "End", Comment = "No data was found to import", SessionId = "batch job", IPAddress = "local" } );
					}

				}

			}
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
                { ActivityType = "System", Activity = "Import", Event = "Start", SessionId="batch job", IPAddress= "local" } 
            );

        }
		/// <summary>
		/// Handle deleted records for the requested time period
		/// </summary>
		/// <param name="community"></param>
		/// <param name="startingDate">Date must be in UTC</param>
		/// <param name="endingDate">Date must be in UTC</param>
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
			bool usingNewDeleteProcess = UtilityManager.GetAppKeyValue( "usingNewDeleteProcess", false );

			LoggingHelper.DoTrace( 1, string.Format("===  DELETE Check for: '{0}' to '{1}' ===", startingDate, endingDate ) );
			//startingDate = "2017-10-29T00:00:00";
			try
			{
				while ( pageNbr > 0 && !isComplete )
				{
					list = RegistryImport.GetDeleted( community, type, startingDate, endingDate, pageNbr, pageSize, ref pTotalRows, ref statusMessage );

					if ( list == null || list.Count == 0 )
					{
						isComplete = true;
						if ( pageNbr == 1 )
						{
							importNote = "Deletes: No records where found for date range ";
							//Console.WriteLine( thisClassName + ".HandleDeletes() - " + importNote );
							LoggingHelper.DoTrace( 1, thisClassName + string.Format(".HandleDeletes() Community: {0} - ", community) + importNote );
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
							if ( usingNewDeleteProcess ) {
								Program.HandleDeleteRequest( cntr, item.EnvelopeIdentifier, ctid, ctdlType, ref statusMessage );
								continue;
							}
							//21-02-22 the old and new delete process seem to be the same, at lease the same xxxManager().Delete methods are being called. 
						
							//
							//importError = "";
							//each delete method will add an entry to SearchPendingReindex.
							//at the end of the process, call method to handle all the deletes
							switch ( ctdlType.ToLower() )
							{
								case "credentialorganization":
								case "qacredentialorganization":
								case "organization":
									DisplayMessages( string.Format( "{0}. Deleting {1} by ctid: {2} ", cntr, ctdlType, ctid ) );
									if ( !new OrganizationManager().Delete( ctid, ref statusMessage ) )
										DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
									break;

								case "assessmentprofile":
									DisplayMessages( string.Format( "{0}. Deleting {1} by ctid: {2} ", cntr, ctdlType, ctid ) );
									if ( !new AssessmentManager().Delete(  ctid, ref statusMessage ) )
										DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
									break;
								case "learningopportunityprofile":
									DisplayMessages( string.Format( "{0}. Deleting {1} by ctid: {2} ", cntr, ctdlType, ctid ) );
									if ( !new LearningOpportunityManager().Delete( ctid, ref statusMessage ) )
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
								case "competencyframework": //CompetencyFramework
									DisplayMessages( string.Format( "{0}. Deleting CompetencyFramework by EnvelopeIdentifier/ctid: {1}/{2} ", cntr, item.EnvelopeIdentifier, ctid ) );
									if ( !new CompetencyFrameworkManager().Delete( ctid, ref statusMessage ) )
										DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
									break;
								case "conceptscheme": //
								case "skos:conceptscheme":
									DisplayMessages( string.Format( "{0}. Deleting ConceptScheme by EnvelopeIdentifier/ctid: {1}/{2} ", cntr, item.EnvelopeIdentifier, ctid ) );
									if ( !new ConceptSchemeManager().Delete( envelopeIdentifier, ctid, ref statusMessage ) )
										DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
									break;
								case "pathway":
									DisplayMessages( string.Format( "{0}. Deleting Pathway by EnvelopeIdentifier/ctid: {1}/{2} ", cntr, item.EnvelopeIdentifier, ctid ) );
									if ( !new PathwayManager().Delete( envelopeIdentifier, ctid, ref statusMessage ) )
										DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
									break;
								case "pathwayset":
									DisplayMessages( string.Format( "{0}. Deleting PathwaySet by EnvelopeIdentifier/ctid: {1}/{2} ", cntr, item.EnvelopeIdentifier, ctid ) );
									if ( !new PathwaySetManager().Delete( envelopeIdentifier, ctid, ref statusMessage ) )
										DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
									break;
								case "transfervalueprofile":
									DisplayMessages( string.Format( "{0}. Deleting transfervalue by Ctid: {1} ", cntr, ctid ) );
									if ( !new TransferValueProfileManager().Delete( ctid, ref statusMessage ) )
										DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
									break;
								default:
									//default to credential
									DisplayMessages( string.Format( "{0}. Deleting Credential ({2}) by EnvelopeIdentifier/ctid: {1}/{3} ", cntr, item.EnvelopeIdentifier, ctdlType, ctid ) );
									if ( !new CredentialManager().Delete( ctid, ref statusMessage ) )
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

					if ( ( maxRecords > 0 && cntr > maxRecords ) || cntr > pTotalRows )
					{
						isComplete = true;
						DisplayMessages( string.Format( "Delete EARLY EXIT. Completed {0} records out of a total of {1} ", cntr, pTotalRows ) );
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
			catch (Exception ex)
			{
				LoggingHelper.LogError( ex, "Import.HandleDeletes" );
			}
			//actually only attepted at this time, need to account for errors!
			recordsDeleted= cntr;
			return importResults;
		}
		public static bool HandleDeleteRequest( int cntr, string envelopeIdentifier, string ctid, string ctdlType, ref string statusMessage)
		{
			statusMessage = "";

			bool isValid = true;
			switch ( ctdlType.ToLower() )
			{
				case "credentialorganization":
				case "qacredentialorganization":
				case "organization":
					DisplayMessages( string.Format( "{0}. Deleting {1} by ctid: {2} ", cntr, ctdlType, ctid ) );
					if ( !new OrganizationManager().Delete( ctid, ref statusMessage ) )
						DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;

				case "assessmentprofile":
				case "assessment":
					DisplayMessages( string.Format( "{0}. Deleting {1} by ctid: {2} ", cntr, ctdlType, ctid ) );
					if ( !new AssessmentManager().Delete( ctid, ref statusMessage ) )
						DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
				case "learningopportunityprofile":
				case "learningopportunity":
					DisplayMessages( string.Format( "{0}. Deleting {1} by ctid: {2} ", cntr, ctdlType, ctid ) );
					if ( !new LearningOpportunityManager().Delete( ctid, ref statusMessage ) )
						DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
				case "conditionmanifest":
					DisplayMessages( string.Format( "{0}. Deleting ConditionManifest by EnvelopeIdentifier/ctid: {1}/{2} ", cntr, envelopeIdentifier, ctid ) );
					if ( !new ConditionManifestManager().Delete( envelopeIdentifier, ctid, ref statusMessage ) )
					{
						DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					}
					break;
				case "costmanifest":
					DisplayMessages( string.Format( "{0}. Deleting CostManifest by EnvelopeIdentifier/ctid: {1}/{2} ", cntr, envelopeIdentifier, ctid ) );
					if ( !new CostManifestManager().Delete( envelopeIdentifier, ctid, ref statusMessage ) )
						DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
				case "competencyframework": //CompetencyFramework
					DisplayMessages( string.Format( "{0}. Deleting CompetencyFramework by ctid: '{1}' ", cntr, ctid ) );
					if ( !new CompetencyFrameworkManager().Delete( ctid, ref statusMessage ) )
						DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
				case "conceptscheme":
				case "skos:conceptscheme":
					DisplayMessages( string.Format( "{0}. Deleting ConceptScheme by EnvelopeIdentifier/ctid: {1}/{2} ", cntr, envelopeIdentifier, ctid ) );
					if ( !new ConceptSchemeManager().Delete( envelopeIdentifier, ctid, ref statusMessage ) )
						DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
				case "pathway":
					DisplayMessages( string.Format( "{0}. Deleting Pathway by EnvelopeIdentifier/ctid: {1}/{2} ", cntr, envelopeIdentifier, ctid ) );
					if ( !new PathwayManager().Delete( envelopeIdentifier, ctid, ref statusMessage ) )
						DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
				case "pathwayset":
					DisplayMessages( string.Format( "{0}. Deleting PathwaySet by EnvelopeIdentifier/ctid: {1}/{2} ", cntr, envelopeIdentifier, ctid ) );
					if ( !new PathwaySetManager().Delete( envelopeIdentifier, ctid, ref statusMessage ) )
						DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
				case "transfervalueprofile":
					DisplayMessages( string.Format( "{0}. Deleting TransferValueProfile by ctid: {1} ", cntr, ctid ) );
					if ( !new TransferValueProfileManager().Delete( ctid, ref statusMessage ) )
						DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
				case "occupation":
					DisplayMessages( string.Format( "{0}. Deleting Occupation by ctid: {1} ", cntr, ctid ) );
					if ( !new OccupationManager().Delete( ctid, ref statusMessage ) )
						DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
				default:
					//default to credential
					DisplayMessages( string.Format( "{0}. Deleting Credential ({1}) by ctid: {2} ", cntr, ctdlType, ctid ) );
					if ( !new CredentialManager().Delete( ctid, ref statusMessage ) )
						DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
			}

			if ( statusMessage.Length > 0 ) 
				isValid = false;

			return isValid;
		}
	}
}

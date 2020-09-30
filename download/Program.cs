using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Download.Models;
using Download.Services;

namespace Download
{
	class Program
	{
		static void Main( string[] args )
		{

			TimeZone zone = TimeZone.CurrentTimeZone;
			// Demonstrate ToLocalTime and ToUniversalTime.
			DateTime local = zone.ToLocalTime( DateTime.Now );
			LoggingHelper.DoTrace( 1, "Local time: " + local );

			//need to determine how to get last start date
			//may be run multiple times during day, so use a schedule type
			string scheduleType = UtilityManager.GetAppKeyValue( "scheduleType", "daily" );
			int deleteAction = UtilityManager.GetAppKeyValue( "deleteAction", 0 );
			bool doingDownloadOnly = true;

			string defaultCommunity = UtilityManager.GetAppKeyValue( "defaultCommunity" );
			string additionalCommunity = UtilityManager.GetAppKeyValue( "additionalCommunity" );

			#region  Retrieve Type/Arguments
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


			var registryImport = new RegistryHelper( defaultCommunity );

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
			string sortOrder = "asc";
			if ( deleteAction != 1 )
			{
				//
				importResults = importResults + "<br/>" + registryImport.Retrieve( "competency_framework", CodesManager.ENTITY_TYPE_COMPETENCY_FRAMEWORK, startingDate, endingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );

				//Coming soon TransferValue
				importResults = importResults + "<br/>" + registryImport.Retrieve( "transfer_value_profile", CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, startingDate, endingDate, maxImportRecords, doingDownloadOnly, ref recordsImported, sortOrder );


				//pathways 
				//should we try to combine pathways and pathway sets?
				if ( UtilityManager.GetAppKeyValue( "importing_pathway", true ) )
				{
					importResults = importResults + "<br/>" + registryImport.Retrieve( "pathway", CodesManager.ENTITY_TYPE_PATHWAY, startingDate, endingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );
				}
				//
				if ( UtilityManager.GetAppKeyValue( "importing_pathwayset", true ) )
				{
					//can't do this until registry fixture is updated.
					importResults = importResults + "<br/>" + registryImport.Retrieve( "pathway_set", CodesManager.ENTITY_TYPE_PATHWAY_SET, startingDate, endingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );
				}

				//do manifests 
				importResults = importResults + "<br/>" + registryImport.Retrieve( "condition_manifest_schema", CodesManager.ENTITY_TYPE_CONDITION_MANIFEST, startingDate, endingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );
				//
				importResults = importResults + "<br/>" + registryImport.Retrieve( "cost_manifest_schema", CodesManager.ENTITY_TYPE_COST_MANIFEST, startingDate, endingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );

				//handle assessments
				importResults = importResults + "<br/>" + registryImport.Retrieve( "assessment_profile", CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, startingDate, endingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );

				//handle learning opps
				//
				importResults = importResults + "<br/>" + registryImport.Retrieve( "learning_opportunity_profile", CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, startingDate, endingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );


				//handle credentials
				//
				importResults = importResults + "<br/>" + registryImport.Retrieve( "credential", CodesManager.ENTITY_TYPE_CREDENTIAL, startingDate, endingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );


				//handle organizations
				//might be better to do last, then can populate placeholders, try first
				//
				importResults = importResults + "<br/>" + registryImport.Retrieve( "organization", CodesManager.ENTITY_TYPE_ORGANIZATION, startingDate, endingDate, maxImportRecords, doingDownloadOnly, ref recordsImported );

				TimeSpan duration = DateTime.Now.Subtract( local );
				LoggingHelper.DoTrace( 1, string.Format( "********* COMPLETED {0:c} minutes *********", duration.TotalMinutes ));
			}
		}

		public static string DisplayMessages( string message )
		{
			LoggingHelper.DoTrace( 1, message );
			//Console.WriteLine( message );

			return message;
		}

		/// <summary>
		/// Handle deleted records for the requested time period
		/// Save to file systems with prefix of Deleted
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

			LoggingHelper.DoTrace( 1, string.Format( "===  DELETE Check for: '{0}' to '{1}' ===", startingDate, endingDate ) );
			//startingDate = "2017-10-29T00:00:00";
			try
			{
				while ( pageNbr > 0 && !isComplete )
				{
					list = RegistryHelper.GetDeleted( community, type, startingDate, endingDate, pageNbr, pageSize, ref pTotalRows, ref statusMessage );

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
						string ctid = item.EnvelopeCetermsCtid;

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
	}
}

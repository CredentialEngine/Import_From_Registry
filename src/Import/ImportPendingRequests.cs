using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Import.Services;
using workIT.Factories;
using workIT.Models;
using workIT.Services;
using workIT.Utilities;
using ElasticHelper = workIT.Services.ElasticServices;
using RegistryServices = Import.Services.RegistryServices;

namespace CTI.Import
{
    /// <summary>
    /// Import using Import.PendingRequest - populated from ctdlEditor.RegistryPublishingHistory
    /// No date checks needed, just get all that have not been imported.
    /// Will be a partner to the full import. This may be used during prime time, and the full import from 7-8pm to 6am.
    /// 
    /// </summary>
    public class ImportPendingRequests
	{

		static string thisClassName = "ImportPendingRequests";
		public static string envType = UtilityManager.GetAppKeyValue( "environment" );
		public static string defaultCommunity = UtilityManager.GetAppKeyValue( "defaultCommunity" );
		public static string additionalCommunity = UtilityManager.GetAppKeyValue( "additionalCommunity" );
		public static int competencyFrameworksHandled = 0;
		//totals
		public int totalImports = 0;
		public int addedRecords = 0;
		public int updatedRecords = 0;
		public int deletedRecords = 0;
		/* key points
		 * - Will need to check the environment for community
		 * _ get all except org
		 * - then get orgs
		 * - after completion start over
		 * - if none found, pause for 60 seconds, then loop
		 * - stop at 7pm (proposed)
		 * - implications for long running batch?
		 * - need to watch for overlap with batch import
		 * - DELETES WILL HAVE TO BE HANDLED SEPARATELY - UNLESS THIS ARE ALSO ADDED TO THE PENDING QUEUE WHICH IS PROBABLY PREFERRED!
		 *		- could add an action to the history table
		 */
		public void Main( int stopAfterMinutes )
		{
			TimeZone zone = TimeZone.CurrentTimeZone;
			DateTime start = DateTime.Now;
			
			bool pendingElasticUpdates = false;
			bool dataFoundDuringThisPeriod = false;
			int deleteAction = UtilityManager.GetAppKeyValue( "deleteAction", 0 );
			bool doingDownloadOnly = UtilityManager.GetAppKeyValue( "DoingDownloadOnly", false );
			//
			if ( stopAfterMinutes == 0)
				stopAfterMinutes = UtilityManager.GetAppKeyValue( "stopAfterMinutes", 14 );
			//	
			int primeTimeSleepSeconds = UtilityManager.GetAppKeyValue( "primeTimeSleepSeconds", 60 ) * 1000;
			if ( primeTimeSleepSeconds < 1 )
				primeTimeSleepSeconds = 10000;
			int offHoursSleepSeconds = UtilityManager.GetAppKeyValue( "offHoursSleepSeconds", 300 ) * 1000;
			if ( offHoursSleepSeconds < 1 )
				offHoursSleepSeconds = 1800000; //3 min

			DateTime lastTimeDataFound = DateTime.Now;
			DateTime lastPendingUpdatesCheck = DateTime.Now;
			DateTime lastCodeTableCountsCheck = DateTime.Now;

			
			//default minutes for checking for deletes
			//force check on first loop - assuming schedule will relate to stopAfterMinutes
			int deleteInterval = UtilityManager.GetAppKeyValue( "deleteInterval", 14 );
			DateTime lastDeletesCheck = DateTime.Now.AddMinutes( deleteInterval * -1 );
			//
			var startDate = DateTime.Now;
			//string startingDate = DateTime.Now.AddHours( -1 ).ToString();

			//
			int loopsWithoutData = 0;
			int dataForPeriod = 0;
			bool haveDataToImport = true;
			LoggingHelper.DoTrace( 1, string.Format( "===  *****************  ImportPendingRequests  ***************** " ) );
			LoggingHelper.DoTrace( 1, string.Format( "===  *****************  Stop After:		{0} minutes  ***************** ", stopAfterMinutes ) );
			LoggingHelper.DoTrace( 1, string.Format( "===  *****************  Pause interval:	{0} seconds  ***************** ", primeTimeSleepSeconds/1000 ) );
			LoggingHelper.DoTrace( 1, string.Format( "===  *****************  Delete Interval:	{0} minutes  ***************** ", deleteInterval ) );
			var summary = new ImportSummary();
			/* Process:
			 * uses the table: Import.PendingRequest to check for any pending requests to import. The latter is populated through a trigger on the ctdlEditor.RegistryPublishingHistory table
			 * - runs on a schedule or with batch parameters to indicate for how long to run (stopAfterMinutes)
			 * - if there is no data found, the process will sleep (primeTimeSleepSeconds) for a bit and try again
			 * - until the stop period is reached.
			 * 
			 * Steps
			 * - start by checking for any delete requests
			 * - next check for all non-organization requests
			 * - finally check for organizations
			 * 
			 * - if any data was imported, 
			 *		- a flag is set to indicate elastic should be updated.
			 *		- then start over checking for deletes/data
			 *	- if no data in this cycle
			 *		- check for pending elastic updates. 
			 *		- If true and , call the elastic method
			 */
			do
			{
				if ( loopsWithoutData > 0 && loopsWithoutData % 10 == 0 )
					LoggingHelper.DoTrace( 7, string.Format( "Next loop. loopsWithoutData: {0}", loopsWithoutData ) );
				int foundData = 0;
				int foundOrgData = 0;
				int didDeletes = 0;
				
				CheckForDeletes( ref didDeletes );
				deletedRecords += didDeletes;
				summary.RecordsDeleted += didDeletes;				

				//may want some numbers, or use class with actual numbers by type
				//Warning: if there is a high number could have conflict with next job.
				//21-03-07 mp - updated to do 100 at a time
				ImportAllExceptOrganizations( ref foundData );
				summary.RecordsImported += foundData;

				ImportOrganizations( ref foundOrgData );
				summary.OrganizationsImported += foundOrgData;

				if ( foundData > 0 || foundOrgData > 0 || didDeletes > 0 )
				{
					//if any data was found, just loop again immediately
					lastTimeDataFound = DateTime.Now;
					pendingElasticUpdates = true;
					dataFoundDuringThisPeriod = true;
					loopsWithoutData = 0;
					dataForPeriod += foundData + foundOrgData + didDeletes;
					//20-08-11 mp - if there is a very busy period, then can run past the cycle minutes,and run into the next job?
					if ( DateTime.Now.Subtract( startDate ).TotalMinutes >= stopAfterMinutes )
					{
						haveDataToImport = false;
						break;
					}

					//what to add to the activity history?
					//maybe an hourly summary?
					//new ActivityServices().AddActivity( new SiteActivity()
					//{ ActivityType = "System", Activity = "Import", Event = "End", Comment = string.Format( "Summary: {0} records were imported, {1} records were deleted.", recordsImported, recordsDeleted ), SessionId = "batch job", IPAddress = "local" } );
				}
				else
				{
					loopsWithoutData++;
					//want to update elastic etc. but only if there has been actual imports since the last time this section hit
					if ( pendingElasticUpdates )
					{
						/*--False - will update caches, and elastic on a per record basis,
							True -store requests in the SearchPendingReindex table, and handle at end of import.	*/
						if ( UtilityManager.GetAppKeyValue( "delayingAllCacheUpdates", true )
                            //if doingScheduledElasticUpdates is true, then the updates are done by an external job, so don't do here
                            && UtilityManager.GetAppKeyValue( "doingScheduledElasticUpdates", false ) == false )
                        {
							//update elastic if a elasticSearchUrl exists
							//May NOT want to do this every minute
							if ( UtilityManager.GetAppKeyValue( "elasticSearchUrl" ) != "" )
							{
								//2023 - need to update addresses first (geocode) or elastic will be off
								if ( UtilityManager.GetAppKeyValue( "doingGeoCodingImmediately", false ) == false && UtilityManager.GetAppKeyValue( "skippingGeoCodingCompletely", false ) == false )
								{
									//ThreadPool.QueueUserWorkItem( HandleAddressGeoCoding, Guid.NewGuid() );
									ProfileServices.HandleAddressGeoCoding();
								}
								LoggingHelper.DoTrace( 1, string.Format( "===  *****************  Updating Elastic  ***************** " ) );
								//22-08-22 mp - perhaps we need to be populating the caches here - rather than the populate cache proc running every few minutes
								ElasticHelper.UpdateElastic( false, true );
							}
						}

						//may want to do this less frequently
						//this won't work if the job is restarting every 25 minutes. Done at the end of each cycle now
						if ( lastTimeDataFound.Subtract( lastPendingUpdatesCheck ).TotalHours > 2 )
						{
							//set all resolved records in Import_EntityResolution to be resolved.
							LoggingHelper.DoTrace( 1, string.Format( "===  *****************  SetAllResolvedEntities  ***************** " ) );
							new ImportManager().SetAllResolvedEntities();

							lastPendingUpdatesCheck = DateTime.Now;
						}

						//do even less frequently - unless there was a large import?
						//this won't work if the job is restarting every 25 minutes. Done at the end of each cycle now
						//update code table counts
						if ( DateTime.Now.Subtract( lastCodeTableCountsCheck ).TotalHours > 1 || dataForPeriod > 10 )
						{
							//LoggingHelper.DoTrace( 1, string.Format( "===  *****************  UpdateCodeTableCounts  ***************** " ) );
							//maybe do this async? - will miss next group until done, but should catch up?
							//20-12-16 mp - getting lots of errors like: 'Cannot insert duplicate key row in object 'dbo.Counts.SiteTotals'. Chg to only do at end of cycle
							//ThreadPool.QueueUserWorkItem( UpdateCodeTableCounts, Guid.NewGuid() );

							lastCodeTableCountsCheck = DateTime.Now;
							dataForPeriod = 0;
						}
					}
					pendingElasticUpdates = false;
					//check for break
					if ( DateTime.Now.Subtract( startDate ).TotalMinutes > stopAfterMinutes )
					{
						haveDataToImport = false;
						break;
					}

					//if no data found, pause and try again
					if ( DateTime.Now.Hour < 6 || DateTime.Now.Hour >= 19 )
					{
						LoggingHelper.DoTrace( 1, string.Format( "**** ImportPendingRequests - sleeping for {0} seconds. ****", offHoursSleepSeconds / 1000 ) );
						//WARNING - COULD RUN INTO THE NEXT SCHEDULE CAUSING A CONFLIC
						//this may be too long if using a shorter schedule
						Thread.Sleep( offHoursSleepSeconds );// 5+ minutes
					}
					else if ( DateTime.Now.Hour >= 6 && DateTime.Now.Hour < 19 )
					{
						LoggingHelper.DoTrace( 1, string.Format("**** ImportPendingRequests - sleeping for {0} seconds. ****", primeTimeSleepSeconds/1000) );

						Thread.Sleep( primeTimeSleepSeconds ); // prime time default 60 seconds
					}
					else //now this will not be hit!
						Thread.Sleep( offHoursSleepSeconds );//

					if ( DateTime.Now.DayOfYear - start.DayOfYear != 0 )
					{
						//stop, assumming that another job will start
						haveDataToImport = false;
					}
				}

			} while ( haveDataToImport );

			//========================================================================
			//completed cycle
			//check for pending (could be necessary becuase of an end due to period time)
			//21-04-01 mp - always check on end of cycle, just in case

			LoggingHelper.DoTrace( 1, "**** ImportPendingRequests - End of period cleanup ****" );
			//Ahh same issue will the geocode step stop once the console job stops
			//2023 - IF GEOCODING IS DONE AFTER ELASTIC, THEN ELASTIC WILL BE OUT OF DATE
			//	Sigh-actually updated the entity.address process to add a pending reindex record in 2022-08. So why are there still issues?
			if ( UtilityManager.GetAppKeyValue( "doingGeoCodingImmediately", false ) == false && UtilityManager.GetAppKeyValue( "skippingGeoCodingCompletely", false ) == false )
			{
				//ThreadPool.QueueUserWorkItem( HandleAddressGeoCoding, Guid.NewGuid() );
				ProfileServices.HandleAddressGeoCoding();
			}
			if ( UtilityManager.GetAppKeyValue( "elasticSearchUrl" ) != "" )
			{
				LoggingHelper.DoTrace( 1, string.Format( "===  *****************  Updating Elastic  ***************** " ) );
				//only do the populate cache if we know that we have data - should not really affect the website though
				ElasticHelper.UpdateElastic( false, pendingElasticUpdates );
			}


			//set all resolved records in Import_EntityResolution to be resolved.
			//do we want this? What if a new record was added after end of cycle and this step resets it?
			//could be date based
			LoggingHelper.DoTrace( 1, string.Format( "===  *****************  SetAllResolvedEntities  ***************** " ) );
			new ImportManager().SetAllResolvedEntities();


			if ( dataFoundDuringThisPeriod )
			{
				//send email to accounts admin
				SendSummaryEmail( summary, startDate, DateTime.Now );
				//20-05-05 mparsons - now done once per period.
				//21-04-02 mparsons - queueing doesn't work here, do direct
				//ThreadPool.QueueUserWorkItem( HandlePendingRequests, Guid.NewGuid() );
				HandlePendingRequests( Guid.NewGuid() );


				//maybe do this async? - will miss next group until done, but should catch up?
				//21-03-30 mp - now doing via a schedule proc - as process could end before proces completes
				//if ( UtilityManager.GetAppKeyValue( "doingPropertyCounts", false ) )
				//{
				//	//this will now be done once per period, at end of period
				//	LoggingHelper.DoTrace( 1, string.Format( "===  *****************  UpdateCodeTableCounts  ***************** " ) );
				//	//hmm - will this just stop once console job ends?
				//	ThreadPool.QueueUserWorkItem( UpdateCodeTableCounts, Guid.NewGuid() );
				//}


			}
			//if any frameworks encountered during period, or always ?
			//20-11-11 mp - change to always do
			//if ( competencyFrameworksHandled > 0 )
			//{
			if ( UtilityManager.GetAppKeyValue( "updateCompetencyFrameworkReportTotals", false ) == true )
			{
				//could check if db count is different from the Codes_EntityTypes count
				LoggingHelper.DoTrace( 1, "@@@@ Import calling UpdateCompetencyFrameworkReportTotals @@@@" );

				CompetencyFrameworkServicesV2.UpdateCompetencyFrameworkReportTotals( true, false );
			}
			//}

			LoggingHelper.DoTrace( 1, "**** Completion of ImportPendingRequests ****" );
		}

		public static void SendSummaryEmail( ImportSummary summary, DateTime startDate, DateTime endingDate )
		{
			bool hasSummaryData = true;
			bool hasDetailedData = false;
			string subject = "Credential Finder Import Summary";
			string table = "";
			string finderSite = UtilityManager.GetAppKeyValue( "oldCredentialFinderSite" );
			if ( summary.RecordsImported == 0 && summary.OrganizationsImported == 0 && summary.RecordsDeleted == 0 )
				hasSummaryData=false;
			//
			try
			{
				string emailBody = string.Format( "<p>Summary of Credential Finder import from {0} to {1}</p>", startDate.ToString( "MMM d, yyyy HH:mm" ), endingDate.ToString( "MMM d, yyyy HH:mm" ) );
				if ( summary.RecordsImported == 0 && summary.OrganizationsImported == 0 && summary.RecordsDeleted == 0 )
				{
					hasSummaryData = false;
				}
				string notificationsEmail = UtilityManager.GetAppKeyValue( "accountNotifications", "mparsons@credentialengine.org" );
				if ( hasSummaryData )
				{
					//emailBody += string.Format( "<p>Organizations Imported: {0}</p>", summary.OrganizationsImported );
					//emailBody += string.Format( "<p>Records Imported:		{0}</p>", summary.RecordsImported );
					//emailBody += string.Format( "<p>Records Deleted:		{0}</p>", summary.RecordsDeleted );
				}
				List<CodeItem> output = new List<CodeItem>();
				string message = "";
				ImportManager.ImportPeriodSummary( startDate.ToString( "yyyy-MM-dd hh:mm" ), DateTime.Now.ToString( "yyyy-MM-dd hh:mm" ), ref output, ref message );
				if ( output?.Any() ?? false )
				{
					hasDetailedData = true;
					table = "<table><tr><th style='width:350px;text-align: left;'>Entity</th><th style='width:300px;text-align: left;'>Activity</th><th style='width:50px;text-align: left;'>Totals</th></tr>";
					foreach ( CodeItem item in output )
					{
						table += $"<tr><td>{item.EntityType}</td><td>{item.Description}</td><td>{item.Totals}</td></tr>";
					}
					table += "</table>";
					emailBody += table;
				}
				if ( output?.Any() ?? false )
				{
					emailBody += $"<p><a href='" + finderSite + "Admin/Activity/'>View Activity Report</a></p>";
					emailBody += $"<p>Have a great day.</p>";
					if ( hasDetailedData || hasSummaryData )
					{
						EmailManager.SendEmail( notificationsEmail, subject, emailBody );
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError(ex, "Import");
			}

			string pFilter = string.Format( "convert(varchar(16),Created,120) >= '{0}'", startDate.ToString( "yyyy-MM-dd hh:mm" ) );
			int pTotalRows = 0;
			try
			{
				List<MessageLog> list = MessageManager.Search( pFilter, "", 1, 5, ref pTotalRows );
				if ( !( list?.Any() ?? false ) )
				{
					return;
				}
				string appKeyValue3 = UtilityManager.GetAppKeyValue( "systemAdminEmail", "mparsons@credentialengine.org" );
				subject = $"Finder Import: {pTotalRows} errors have been encountered recently";
				var emailBody = $"<p>Summary of Errors: {list.Count} of {pTotalRows}.</p>";
				table = "<table><tr><th style='width:30%;text-align: left;'>Activity</th><th style='width:50%;text-align: left;'>Message</th></tr>";
				foreach ( MessageLog item2 in list )
				{
					table += $"<tr><td>{item2.Activity}</td><td>{item2.Message}</td></tr>";
				}
				table += "</table>";
				//
				emailBody += table;
				emailBody += $"<p><a href='" + finderSite + "Admin/messagelog/'>View Messages Log</a></p>";
				emailBody += $"<p>Have a lucky day.</p>";
				EmailManager.SendEmail( appKeyValue3, subject, emailBody );
			}
			catch ( Exception ex2 )
			{
				LoggingHelper.LogError( ex2, "Import", "Send Message Log Summary" );
			}
		}

		static void UpdateCodeTableCounts(Object rowId)
		{
			if ( UtilityManager.GetAppKeyValue( "doingPropertyCounts", false ) )
				new CacheManager().UpdateCodeTableCounts();

		}
		/// <summary>
		/// attempt to resolve records in pending (EntityStateId = 1) state
		/// NEED NEW NAME TO DISTINGUISH THIS FROM Import.PendingRequest
		/// </summary>
		/// <param name="rowId"></param>
		static void HandlePendingRequests(Object rowId)
		{
			if ( UtilityManager.GetAppKeyValue( "processingPendingRecords", true ) )
			{
				//==============================================================
				//import pending
				string pendingStatus = new RegistryServices().ImportPending();
			}

		}

		//public static void HandleAddressGeoCoding( Object rowId )
		//{
		//	//should we do all?
		//	int maxRecords = 0;
		//	LoggingHelper.DoTrace( 5, thisClassName + string.Format( ".HandleAddressGeoCoding - maxRecords: {0}", maxRecords ) );
		//	DateTime started = DateTime.Now;
		//	string report = "";
		//	string messages = "";
		//	var list = new Entity_AddressManager().ResolveMissingGeodata( ref messages, maxRecords );

		//	var saveDuration = DateTime.Now.Subtract( started );
		//	LoggingHelper.DoTrace( 5, thisClassName + string.Format( ".NormalizeAddresses - Completed - seconds: {0}", saveDuration.Seconds ) );
		//	if ( !string.IsNullOrWhiteSpace( messages ) )
		//		report = string.Format( "<p>Normalize Addresses. Duration: {0} seconds <br/>", saveDuration.Seconds ) + messages + "</p>";

		//	foreach ( var address in list )
		//	{
		//		string msg = string.Format( " - Unable to resolve address: Id: {0}, address1: {1}, city: {2}, region: {3}, postalCode: {4}, country: {5} ", address.Id, address.StreetAddress, address.AddressLocality, address.AddressRegion, address.PostalCode, address.AddressCountry );
		//		LoggingHelper.DoTrace( 2, msg );
		//		report += System.Environment.NewLine + msg;
		//	}
		//	//no reporting of successes here 

		//}
		/// <summary>
		/// Get all pending records except organizations
		/// NOTE: for snhu the trgRegistryPublishingHistoryAfterInsert trigger in sandbox_ctdlEditor will create in a separate database
		///		An alternate would be to use a default community here and filter the method: SelectAllPendingExceptList with community
		/// 20-08-11 mparsons - should we have a limit on the number?
		/// 21-03-07 mparsons - added code to process 100 at a time
		///						there could be an issue with a large number where the stays inside here beyond the 55 minutes. And the next job could start before this finishes
		/// </summary>
		/// <param name="foundData"></param>
		public static void ImportAllExceptOrganizations(ref int foundData)
		{
			ImportManager imanager = new ImportManager();
			//RegistryImport regImporter = new RegistryImport( defaultCommunity );
			foundData = 0;
			var maxPendingImportRecords = UtilityManager.GetAppKeyValue( "maxPendingImportRecords", 100 );

			//this approach would likely only have one type. However there may be a need to customize the except option so a full import could exclude TVP
			var pendingProcessResourceTypeList = UtilityManager.GetAppKeyValue( "pendingProcessResourceTypeList" );
			var pendingProcessExcludeResourceTypeList = UtilityManager.GetAppKeyValue( "pendingProcessExcludeResourceTypeList" );

			//TODO - if the set being imported could be 'tagged', then multiple programs could run. Or split up project and add an async process. or parallel processing
			var list = ImportManager.SelectAllPendingExceptList( "Organization", maxPendingImportRecords );
			if ( list != null && list.Count() > 0 )
			{
				LoggingHelper.DoTrace( 1, string.Format( "@@@@  ImportAllExceptOrganizations. Processing: {0} records  ", list.Count()) );

				foundData = list.Count();
				ProcessList( list );

				//pageNbr++;
			}
			
		}
		public static void ImportOrganizations(ref int foundData)
		{
			ImportManager imanager = new ImportManager();
			foundData = 0;
			var maxPendingImportRecords = UtilityManager.GetAppKeyValue( "maxPendingImportRecords", 100 );

			//only get orgs
			var list = ImportManager.SelectPendingList( "Organization", maxPendingImportRecords );
			if ( list != null && list.Count() > 0 )
			{
				LoggingHelper.DoTrace( 1, string.Format( "@@@@  Import Pending: Organizations. Processing: {0} records  ", list.Count() ) );

				foundData = list.Count();
				ProcessList( list );
			}
		}
		public static void ImportCredentials( ref int foundData )
		{
			ImportManager imanager = new ImportManager();
			foundData = 0;
			var maxPendingImportRecords = UtilityManager.GetAppKeyValue( "maxPendingImportRecords", 100 );

			//only get orgs
			var list = ImportManager.SelectPendingList( "Credential", maxPendingImportRecords );
			if ( list != null && list.Count() > 0 )
			{
				LoggingHelper.DoTrace( 1, string.Format( "@@@@  Import Pending: Credentials. Processing: {0} records  ", list.Count() ) );

				foundData = list.Count();
				ProcessList( list );
			}
		}
		public static void  ProcessList(List<Import_PendingRequest> list)
		{
			ImportManager imanager = new ImportManager();
			RegistryImport regImporter = new RegistryImport( defaultCommunity );
			//var list = ImportManager.SelectAllPendingExceptList( "Organization" );
			if ( list != null && list.Count() > 0 )
			{
				int cntr = 0;
				
				foreach (var item in list)
				{
					bool isValid = true;
					cntr++;	//this may not be meaningful given could be running all day!
					var statusMessage = "";
					var ctdlType = "";
					var community = defaultCommunity;
					//check for community
					//if (item.Environment.IndexOf(".") > 0)
					//{
					//	//not the best approach, may want to add community to the history
					//	//21-10-29 - going forward, environment will just contain the community (or perhaps blank for the default community) 
					//	community = item.Environment.Substring( item.Environment.IndexOf( "." ) + 1 );
					//}

					regImporter = new RegistryImport( community );
					//
					int entityTypeId = MappingHelperV3.GetEntityTypeId( item.PublishingEntityType );
					if ( entityTypeId > 0 )
					{
						//20-12-07 mp - chg to use ctid now that we can get the envelope by ctid.Note that deletes will not have an envelope id
						//21-10-30 - need alter process to check if record is equal to default or alternate community
						if ( !string.IsNullOrWhiteSpace(item.EntityCtid) && item.EntityCtid.Length == 39 )
						{
							ReadEnvelope envelope = new ReadEnvelope();
							//23-12-01 if item contains envelopeURL, use it
							bool usedEnvelopeURL = false;
							if ( !string.IsNullOrWhiteSpace( item.EnvelopeURL ) )
							{
								envelope = RegistryServices.GetEnvelopeByURL( item.EnvelopeURL, ref statusMessage, ref ctdlType );
								usedEnvelopeURL=true;
							} else 
							{
								envelope = RegistryServices.GetEnvelope( item.EntityCtid, ref statusMessage, ref ctdlType, community );
							}

							if ( envelope == null || envelope.DecodedResource == null )
							{
								//shouldn't happen in proper environment
								LoggingHelper.DoTrace( 1, $"!!! Encountered record that has a CTID that was not found. This is unlikely outside of test? Possibly the record has been deleted. EntityTypeId: {entityTypeId}, EntityName: {item.EntityName},  CTID: {item.EntityCtid}, usedEnvelopeURL: {usedEnvelopeURL} \r\nmessage: {statusMessage}. Setting to handled to avoid endless messages!" );
								//set handled
								imanager.SetImport_PendingRequestHandled( item.Id, false );
							}
							else
							{
								envelope.documentPublishedBy = envelope.documentPublishedBy != null ? envelope.documentPublishedBy : item.PublisherCTID;
								envelope.documentOwnedBy = envelope.documentOwnedBy != null ? envelope.documentOwnedBy : item.DataOwnerCTID;
								//will we want a download option here? Unlikely
								isValid = regImporter.ProcessEnvelope( envelope, item.PublishingEntityType, entityTypeId, cntr, false );
								if ( entityTypeId == 10 || entityTypeId == 9 )
									competencyFrameworksHandled++;

								//may need to set completed regardless or we get loops
								//maybe need a qualifier to indicate unsuccessful
								//if ( isValid )
								{
									imanager.SetImport_PendingRequestHandled( item.Id, isValid );
								}
							}
						}
						else
						{
							//shouldn't happen in proper environment
							LoggingHelper.DoTrace( 1, string.Format( " Encountered record that is missing an envelopeId. This is unlikely outside of test? EntityName: {0},  EntityCtid: {1}. Setting to handled to avoid endless messages!", item.EntityName, item.EntityCtid ) );
							//
							imanager.SetImport_PendingRequestHandled( item.Id, false );
						}
					} else
					{
						LoggingHelper.DoTrace( 1, string.Format( " Encountered a publishing type that is not handled. This can happen with Navy data. EntityName: {0},  EntityCtid: {1}, Type: {2}. Setting to handled to avoid endless messages!", item.EntityName, item.EntityCtid, item.PublishingEntityType ) );
						imanager.SetImport_PendingRequestHandled( item.Id, false );
					}
				}
			}			
		}

		public static void CheckForDeletes( ref int foundData )
		{
			ImportManager imanager = new ImportManager();
			RegistryImport regImporter = new RegistryImport( defaultCommunity );
			var deleteService = new ImportUtilities();
			foundData = 0;
			int cntr = 0;
			//TODO - may need to add paging to handle hundreds/thousands
			

			var list = ImportManager.SelectAllPendingDeletes();
			if ( list != null && list.Count() > 0 )
			{
				foundData = list.Count();
				LoggingHelper.DoTrace( 1, string.Format( "Found: {0} deletes to process.", foundData ) );
				foreach ( var item in list )
				{
					cntr++;
					string statusMessage = "";
					var community = defaultCommunity;
					//check for community
					if ( item.Environment.IndexOf( "." ) > 0 )
					{
						//not the best approach, may want to add community to the history
						//may not matter at this point
						community = item.Environment.Substring( item.Environment.IndexOf( "." ) + 1 );
					}
					if ( string.IsNullOrWhiteSpace( item.EntityCtid ) )
					{
						var entityMsg = $"CheckForDeletes. Found request without a CTID. Id: {item.Id}, Date: {item.Created}, Method: {item.PublishMethodURI} ";
						continue;
					}
					if (item.PublishMethodURI == BaseFactory.REGISTRY_ACTION_PURGE_ALL)
					{
						/*process
						 * - could use the registry fts search to get and delete
						 *	- no can't, as there is no envelopes after purge all
						 * - or method to use: Import.PendingRequest
						 * - or use stored proc - easier set based approach
						 * 
						 * 
						 */
					}
					//else if ( Program.HandleDeleteRequest( cntr, "", item.EntityCtid, item.PublishingEntityType, ref statusMessage ) )
					else if ( deleteService.HandleDeleteRequest( cntr, item.EntityCtid, item.PublishingEntityType, ref statusMessage ) )
					{
						imanager.SetImport_PendingRequestHandled( item.Id, true );
						if ( item.PublishingEntityType.IndexOf( "Competency" ) > -1 )
							competencyFrameworksHandled++;
					}
					else
					{
						var entity = EntityManager.EntityCacheGetByCTID( item.EntityCtid );
						var entityMsg = "";
						if ( entity != null && entity.Id > 0 )
						{
							//22-03-11 mp - why?
							//entityMsg = entity.ToString();
							entityMsg = string.Format( "Found EntityType: {0},  EntityCtid: {1}", entity.EntityType, item.EntityCtid );
						}
						else
							entityMsg = string.Format( "Didn't find EntityType: {0},  EntityCtid: {1}", item.PublishingEntityType, item.EntityCtid );

						LoggingHelper.DoTrace( 1, string.Format( "CheckForDeletes. Error encountered during delete attempt. Entity: {0}, \r\nmessage: {1}. Setting to handled to avoid endless messages!", entityMsg, statusMessage ) );
						LoggingHelper.LogError( string.Format( "Error encountered during delete attempt. Entity: {0}, \r\nmessage: {1}. Setting to handled to avoid endless messages!", entityMsg, statusMessage ) );

						imanager.SetImport_PendingRequestHandled( item.Id, false );
					}
				}
			}

			if ( cntr > 0 )
			{
				var messages = new List<string>();
				ElasticHelper.HandlePendingDeletes( ref messages );
			}
			//21-03-17 also need to handle a purge all event
			list = ImportManager.SelectAllPendingPurgeAll();
			if ( list != null && list.Count() > 0 )
			{
				//now what
			}
		}

		//public static void ImportOrganizationsOLD(ref bool foundData)
		//{
		//	ImportManager imanager = new ImportManager();
		//	RegistryImport regImporter = new RegistryImport( defaultCommunity );
		//	//only get orgs
		//	var list = ImportManager.SelectPendingList( "Organization" );
		//	if ( list != null && list.Count() > 0 )
		//	{
		//		foundData = true;
		//		int cntr = 0;

		//		foreach ( var item in list )
		//		{
		//			bool isValid = true;
		//			cntr++; //this may not be meaningful given could be running all day!
		//			var statusMessage = "";
		//			var ctdlType = "";
		//			var community = defaultCommunity;
		//			//check for community
		//			if ( item.Environment.IndexOf( "." ) > 0 )
		//			{
		//				//not the best approach, may want to add community to the history
		//				community = item.Environment.Substring( item.Environment.IndexOf( "." ) + 1 );
		//			}

		//			regImporter = new RegistryImport( community );

		//			int entityTypeId = MappingHelperV3.GetEntityTypeId( item.PublishingEntityType );

		//			//var envelope = RegistryServices.GetEnvelope( item.EnvelopeId, ref statusMessage, ref ctdlType, community );

		//			////will we want a download option here? Unlikely
		//			//isValid = regImporter.ProcessEnvelope( envelope, item.PublishingEntityType, entityTypeId, cntr, false );

		//			//if ( isValid )
		//			//{
		//			//	imanager.SetImport_PendingRequestHandled( item.Id );
		//			//}

		//			if ( item.EnvelopeId != null && item.EnvelopeId.Length == 36 )
		//			{
		//				var envelope = RegistryServices.GetEnvelope( item.EnvelopeId, ref statusMessage, ref ctdlType, community );
		//				if ( envelope == null || envelope.DecodedResource == null )
		//				{
		//					//shouldn't happen in proper environment
		//					LoggingHelper.DoTrace( 1, string.Format( " Encountered record that has an envelopeId that was not found. This is unlikely outside of test? Possibly the record has been deleted. EntityName: {0},  EntityCtid: {1}, \r\nmessage: {2}. Setting to handled to avoid endless messages!", item.EntityName, item.EntityCtid, statusMessage ) );
		//					//set handled
		//					imanager.SetImport_PendingRequestHandled( item.Id );
		//				}
		//				else
		//				{
		//					//will we want a download option here? Unlikely
		//					isValid = regImporter.ProcessEnvelope( envelope, item.PublishingEntityType, entityTypeId, cntr, false );

		//					if ( isValid )
		//					{
		//						imanager.SetImport_PendingRequestHandled( item.Id );
		//					}
		//				}
		//			}
		//			else
		//			{
		//				//shouldn't happen in proper environment
		//				LoggingHelper.DoTrace( 1, string.Format( " Encountered record that is missing an envelopeId. This is unlikely outside of test? EntityName: {0},  EntityCtid: {1}. Setting to handled to avoid endless messages!", item.EntityName, item.EntityCtid ) );
		//				//
		//				imanager.SetImport_PendingRequestHandled( item.Id );
		//			}
		//		}

		//	}
		//	else
		//		foundData = false;
		//}
	}
	public class ImportSummary
	{
		public int OrganizationsImported { get; set; }
		public int RecordsImported { get; set; }
		public int RecordsDeleted { get; set; }
	}
}

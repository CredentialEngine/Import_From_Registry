﻿using System;
using System.Collections.Generic;
//using System.Net.Http;
//using System.Net.Http.Headers;
using Import.Services;
using workIT.Factories;
using workIT.Models;
using workIT.Utilities;

namespace CTI.Import
{
    public class RegistryImport
	{
		static string thisClassName = "RegistryImport";
        #region Import Config
        public RegistryImport( string community )
		{
			Community = community;
		}
		public string Community { get; set; }
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
		/// Adhoc list of CTIDs to download
		/// </summary>
		public string ResourceCTIDList = "";

		public List<string> ImportSummary = new List<string>();

		public bool DoingDownloadOnly = false;

		/// <summary>
		/// Set to other than zero to limit the number of records to download
		/// </summary>
		public int MaxImportRecords = 0;
		public int RecordsImported = 0;
		/// <summary>
		/// Graph query in properly formed JSON and CTDL syntax
		/// </summary>
		public string GraphSearchQuery { get; set; }

		public static int maxExceptions = UtilityManager.GetAppKeyValue( "maxExceptions", 500 );
        #endregion

        ImportServiceHelpers importMgr = new ImportServiceHelpers();

		ImportCredential credImportMgr = new ImportCredential();
		ImportOrganization orgImportMgr = new ImportOrganization();
		ImportAssessment asmtImportMgr = new ImportAssessment();
		ImportLearningOpportunties loppImportMgr = new ImportLearningOpportunties();
		ImportConditionManifests cndManImportMgr = new ImportConditionManifests();
		ImportCostManifests cstManImportMgr = new ImportCostManifests();
		ImportCollections cltnImportMgr = new ImportCollections();
		ImportCompetencyFrameworks cfImportMgr = new ImportCompetencyFrameworks();
		ImportPathways pathwayImportMgr = new ImportPathways();
		ImportTransferValueProfile tvpImportMgr = new ImportTransferValueProfile();
		ImportTransferIntermediary transIntermediaryImportMgr = new ImportTransferIntermediary();

		
		public string ImportNew( string registryEntityType, int entityTypeId, string sortOrder = "asc" )
		{
			var resourceTypeDisplay = "Custom";
			if ( !string.IsNullOrWhiteSpace( registryEntityType ) )
			{
				resourceTypeDisplay = registryEntityType;
				//now that we are using an explicit list, no reason to check here
				//bool importingThisType = UtilityManager.GetAppKeyValue( "importing_" + registryEntityType, true );
				//if ( !importingThisType )
				//{
				//	LoggingHelper.DoTrace( 1, string.Format( "===  *****************  Skipping import of {0}  ***************** ", registryEntityType ) );
				//	return "Skipped import of " + registryEntityType;
				//}
				LoggingHelper.DoTrace( 1, string.Format( "===  *****************  Importing {0}  ***************** ", registryEntityType ) );
			}
			
			ReadEnvelope envelope = new ReadEnvelope();
			List<ReadEnvelope> list = new List<ReadEnvelope>();

			string entityType = registryEntityType;
			//CodeItem ci = CodesManager.Codes_EntityType_Get( entityTypeId );
			//if ( ci != null && ci.Id > 0 )
			//	entityType = ci.Title;
			int pageNbr = 1;
			int pageSize = UtilityManager.GetAppKeyValue( "importPageSize", 100 );
			string importResults = "";
			string importNote = "";
			//ThisEntity output = new ThisEntity();
			List<string> messages = new List<string>();

			int cntr = 0;
			int actualTotalRows = 0;
			int pTotalRows = 0;
			int skip = 0;
			int exceptionCtr = 0;
			string statusMessage = "";
			bool isComplete = false;
			bool importSuccessfull = true;
			var usingParallelProcessing = UtilityManager.GetAppKeyValue( "usingParallelProcessing", false );
			//TBD on some tracking 
			Guid transactionGUID = Guid.NewGuid();
			//Update the Progress Tracker with the new count and total (this will reset the progress bar client-side) and include a message indicating that this phase has been reached
			ProgressTrackingManager.UpdateProgressTracker( transactionGUID, true, 50000, tracker =>
			{
				tracker.Messages.Add( "Starting import..." );
				tracker.ProcessedItems = 0;
				tracker.TotalItems = 50000; //unknown and not important at this time
			} );

			//will need to handle multiple calls - watch for time outs
			while ( pageNbr > 0 && !isComplete )
			{
				//19-09-22 chg to use RegistryServices to remove duplicate services

				//***TODO - check for a GraphSearchQuery value and call alternate search
				if ( !string.IsNullOrWhiteSpace( GraphSearchQuery ) )
				{
					skip = ( pageNbr - 1 ) * pageSize;
					list = RegistryServices.GraphSearchByTemplate( GraphSearchQuery, skip, pageSize, ref pTotalRows, ref statusMessage, Community, sortOrder );
				}
				else
				{
					list = RegistryServices.Search( registryEntityType, StartingDate, EndingDate, pageNbr, pageSize, ref pTotalRows, ref statusMessage, Community, OwningOrganizationCTID, PublishingOrganizationCTID, sortOrder );
				}

				if ( list == null || list.Count == 0 )
				{
					isComplete = true;
					if ( !string.IsNullOrWhiteSpace( statusMessage ) )
					{
						LoggingHelper.DoTrace( 1, "Error: " + statusMessage );
						return statusMessage;
					}
					else if ( pageNbr == 1 )
					{
						//importNote = registryEntityType + ": No records where found for date range ";
						LoggingHelper.DoTrace( 4, registryEntityType + ": No records where found for the selected filters. " );
					}
					else if ( cntr < actualTotalRows )
					{
						//if no data found and have not processed actual rows, could have been an issue with the search.
						//perhaps should be an error to ensure followup
						LoggingHelper.DoTrace( 2, string.Format( "**************** WARNING -Import for '{0}' didn't find data on this pass, but has only processed {1} of an expected {2} records.", registryEntityType, cntr, actualTotalRows ) );
						LoggingHelper.LogError( string.Format( "**************** WARNING -Import for '{0}' didn't find data on this pass, but has only processed {1} of an expected {2} records.", registryEntityType, cntr, actualTotalRows ) );
					}
					break;
				}
				if ( pageNbr == 1 )
				{
					actualTotalRows = pTotalRows;
					LoggingHelper.DoTrace( 2, string.Format( "Import {0} Found {1} records to process.", registryEntityType, pTotalRows ) );
				}
				//
				if ( usingParallelProcessing )
				{
					//consider reducing page size?
					//Process the items
					ProgressTrackingManager.ProcessInParallelAndTrack( transactionGUID, list, ( request, tracker, index ) =>
					{
						cntr++;
						//rowStart = DateTime.Now;
						//TimeSpan rowDuration = new TimeSpan();
						// NOTE: found that older envelopes may not have envelope_ctdl_type,
						entityTypeId = MappingHelperV3.GetEntityTypeId( request.EnvelopeCtdlType );
						if ( entityTypeId == 1 && registryEntityType == "qdata_dataset_profile" )
						{
							//skip
						}
						else
						{
							var returnedTask = ProcessEnvelope( request, registryEntityType, entityTypeId, cntr, DoingDownloadOnly );

							//Update the tracker
							if ( cntr % 100 == 0 )
							{
								//TimeSpan currentDuration = DateTime.Now.Subtract( importStart );
								//var currentAvgRecordsPerSecond = CurrentRowNbr / currentDuration.TotalSeconds;
								//var currentAvgSecondsPerRecord = currentDuration.TotalSeconds / CurrentRowNbr;
								//var remainingSeconds = ( ( mTotalRecords - CurrentRowNbr ) * currentAvgSecondsPerRecord ) + 5; //add a fudge factor
								//LoggingHelper.DoTrace( 1, $"{thisClassName}.ImportInParallel. Record: {CurrentRowNbr} of {mTotalRecords}. Avg Records Per Second: {currentAvgRecordsPerSecond:N2}, Avg Seconds Per Record: {currentAvgSecondsPerRecord:N2}, remainingSeconds: {remainingSeconds:N0} " );
								//if ( remainingSeconds < 120 )
								//	tracker.Messages.Add( $"Estimated remaining: {remainingSeconds:N0} seconds." );
								//else
								//{
								//	Decimal minutes = ( decimal ) ( ( remainingSeconds / 60 ) + 1 );
								//	tracker.Messages.Add( $"Estimated remaining: {minutes:N2} minutes" );
								//}
							}
							tracker.ProcessedItems++;
						}
					}, ( request, tracker, index, ex ) =>
					{
						//Get the item name and index
						var itemName = ( string.IsNullOrWhiteSpace( request?.EnvelopeCtid ) ? "Unknown CTID" : request.EnvelopeCtid ) + "/Org: " +
							( string.IsNullOrWhiteSpace( request?.documentOwnedBy ) ? "Unknown" : request.documentOwnedBy );

						//Update the tracker
						tracker.Errors.Add( "Error processing item at index #" + index + " (" + itemName + "): " + ex?.Message + ( !string.IsNullOrWhiteSpace( ex?.InnerException?.Message ) ? "; " + ex.InnerException.Message : "" ) );

					}, ( tracker ) => { 
						//Handle cancelation
					}, false );

				}
				else
				{
					foreach ( ReadEnvelope item in list )
					{
						cntr++;
						//NOTE: found that older envelopes may not have envelope_ctdl_type, 
						entityTypeId = MappingHelperV3.GetEntityTypeId( item.EnvelopeCtdlType );
						if ( entityTypeId == 1 )
						{
							if ( registryEntityType == "qdata_dataset_profile" )
							{
								continue;
							}
						}
						importSuccessfull = ProcessEnvelope( item, registryEntityType, entityTypeId, cntr, DoingDownloadOnly );

						if ( MaxImportRecords > 0 && cntr >= MaxImportRecords )
						{
							break;
						}
					} //end foreach 
				}
				pageNbr++;
				if ( ( MaxImportRecords > 0 && cntr >= MaxImportRecords ) )
				{
					isComplete = true;
					LoggingHelper.DoTrace( 2, string.Format( "Import {2} EARLY EXIT. Completed {0} records out of a total of {1} for {2} ", cntr, pTotalRows, registryEntityType ) );

				}
				else if ( cntr >= actualTotalRows )
				{
					isComplete = true;
					//LoggingHelper.DoTrace( 2, string.Format( "Completed {0} records out of a total of {1} for {2}", cntr, pTotalRows, registryEntityType ) );

				}
				//if ( pageNbr * pageSize < pTotalRows )
				//	pageNbr++;
				//else
				//	isComplete = true;
			}
			importResults = string.Format( "Import {0} - Processed {1} records, with {2} exceptions. \r\n", registryEntityType, cntr, exceptionCtr );
			LoggingHelper.DoTrace( 2, importResults );
			if ( !string.IsNullOrWhiteSpace( importNote ) )
				importResults += importNote;
			//this is not really being used. Was intended for an email 
			ImportSummary.Add( importResults );
			RecordsImported += cntr;

			return importResults;
		}

		public string ImportOld( string registryEntityType, int entityTypeId, string startingDate, string endingDate, int maxRecords, bool downloadOnly, ref int recordsImported, string sortOrder = "asc" )
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

			string entityType = registryEntityType;
			CodeItem ci = CodesManager.Codes_EntityType_Get( entityTypeId );
			if ( ci != null && ci.Id > 0 )
				entityType = ci.Title;
			int pageNbr = 1;
			int pageSize = UtilityManager.GetAppKeyValue( "importPageSize", 100 );
			string importResults = "";
			string importNote = "";
			//ThisEntity output = new ThisEntity();
			List<string> messages = new List<string>();

			int cntr = 0;
			int actualTotalRows = 0;
			int pTotalRows = 0;
			int skip = 0;
			int exceptionCtr = 0;
			string statusMessage = "";
			bool isComplete = false;
			bool importSuccessfull = true;

			//will need to handle multiple calls - watch for time outs
			while ( pageNbr > 0 && !isComplete )
			{
				//19-09-22 chg to use RegistryServices to remove duplicate services

				//***TODO - check for a GraphSearchQuery value and call alternate search
				if ( !string.IsNullOrWhiteSpace( GraphSearchQuery ) )
				{
					skip = (pageNbr - 1) * pageSize;
					list = RegistryServices.GraphSearchByTemplate( GraphSearchQuery, skip, pageSize, ref pTotalRows, ref statusMessage, Community, sortOrder );
				}
				else
				{
					list = RegistryServices.Search( registryEntityType, startingDate, endingDate, pageNbr, pageSize, ref pTotalRows, ref statusMessage, Community, OwningOrganizationCTID, PublishingOrganizationCTID, sortOrder );
				}

				if ( list == null || list.Count == 0 )
				{
					isComplete = true;
					if ( !string.IsNullOrWhiteSpace( statusMessage ) )
					{
						LoggingHelper.DoTrace( 1, "Error: " + statusMessage );
						return statusMessage;
					}
					else if ( pageNbr == 1 )
					{
						//importNote = registryEntityType + ": No records where found for date range ";
						LoggingHelper.DoTrace( 4, registryEntityType + ": No records where found for date range. " );
					}
					else if ( cntr < actualTotalRows )
					{
						//if no data found and have not processed actual rows, could have been an issue with the search.
						//perhaps should be an error to ensure followup
						LoggingHelper.DoTrace( 2, string.Format( "**************** WARNING -Import for '{0}' didn't find data on this pass, but has only processed {1} of an expected {2} records.", registryEntityType, cntr, actualTotalRows ) );
						LoggingHelper.LogError(string.Format("**************** WARNING -Import for '{0}' didn't find data on this pass, but has only processed {1} of an expected {2} records.", registryEntityType, cntr, actualTotalRows));
					}
					break;
				}
				if ( pageNbr == 1 )
				{
					actualTotalRows = pTotalRows;
					LoggingHelper.DoTrace( 2, string.Format( "Import {0} Found {1} records to process.", registryEntityType, pTotalRows ) );
				}

				foreach ( ReadEnvelope item in list )
				{
					cntr++;

					importSuccessfull = ProcessEnvelope( item, registryEntityType, entityTypeId, cntr, downloadOnly );

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
				else if ( cntr >= actualTotalRows )
				{
					isComplete = true;
					//LoggingHelper.DoTrace( 2, string.Format( "Completed {0} records out of a total of {1} for {2}", cntr, pTotalRows, registryEntityType ) );

				}
				//if ( pageNbr * pageSize < pTotalRows )
				//	pageNbr++;
				//else
				//	isComplete = true;
			}
			importResults = string.Format( "Import {0} - Processed {1} records, with {2} exceptions. \r\n", registryEntityType, cntr, exceptionCtr );
			LoggingHelper.DoTrace( 2, importResults );
			if ( !string.IsNullOrWhiteSpace( importNote ) )
				importResults += importNote;

			recordsImported += cntr;

			return importResults;
		}

		public string ImportByGraphSearch( int maxRecords, bool downloadOnly, ref int recordsImported, string sortOrder = "asc" )
		{

			LoggingHelper.DoTrace( 1, string.Format( "===  *****************  ImportByGraphSearch  ***************** " ) );
			LoggingHelper.DoTrace( 1, $"Query:\n {GraphSearchQuery}" );
			//JsonEntity input = new JsonEntity();
			ReadEnvelope envelope = new ReadEnvelope();
			List<ReadEnvelope> list = new List<ReadEnvelope>();
			int pageNbr = 1;
			int pageSize = UtilityManager.GetAppKeyValue( "importPageSize", 100 );
			string importResults = "";
			string importNote = "";
			//ThisEntity output = new ThisEntity();
			List<string> messages = new List<string>();

			int cntr = 0;
			int actualTotalRows = 0;
			int pTotalRows = 0;
			int skip = 0;
			int exceptionCtr = 0;
			string statusMessage = "";
			bool isComplete = false;
			bool importSuccessfull = true;
			var registryEntityType = "";
			int entityTypeId = 0;
			//Q and D test filter
			//GraphSearchQuery="{\"@type\": \"ceasn:CompetencyFramework\"}";
			//will need to handle multiple calls - watch for time outs
			while ( pageNbr > 0 && !isComplete )
			{
				skip = ( pageNbr - 1 ) * pageSize;
				list = RegistryServices.GraphSearch( GraphSearchQuery, skip, pageSize, ref pTotalRows, ref statusMessage, Community, sortOrder );

				if ( list == null || list.Count == 0 )
				{
					isComplete = true;
					if ( !string.IsNullOrWhiteSpace( statusMessage ) )
					{
						LoggingHelper.DoTrace( 1, "Error: " + statusMessage );
						return statusMessage;
					}
					else if ( pageNbr == 1 )
					{
						//importNote = registryEntityType + ": No records where found for date range ";
						LoggingHelper.DoTrace( 4, " No records where found for provided query. " );
					}
					else if ( cntr < actualTotalRows )
					{
						//if no data found and have not processed actual rows, could have been an issue with the search.
						//perhaps should be an error to ensure followup
						LoggingHelper.DoTrace( 2, string.Format( "**************** WARNING -ImportByGraphSearch didn't find data on this pass, but has only processed {0} of an expected {1} records.", cntr, actualTotalRows ) );
				
					}
					break;
				}
				if ( pageNbr == 1 )
				{
					//not handled yet
					actualTotalRows = pTotalRows;
					LoggingHelper.DoTrace( 2, string.Format( "ImportByGraphSearch Found {0} records to process.", pTotalRows ) );
				}

				foreach ( ReadEnvelope item in list )
				{
					cntr++;
					if ( item == null || item.DecodedResource == null )
					{
						//message ................
						continue;
					}

					string payload = item.DecodedResource.ToString();
					registryEntityType = RegistryServices.GetResourceType( payload, true );
					entityTypeId = MappingHelperV3.GetEntityTypeId( registryEntityType );
					importSuccessfull = ProcessEnvelope( item, registryEntityType, entityTypeId, cntr, downloadOnly );

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
				else if ( cntr >= actualTotalRows )
				{
					isComplete = true;
					//LoggingHelper.DoTrace( 2, string.Format( "Completed {0} records out of a total of {1} for {2}", cntr, pTotalRows, registryEntityType ) );

				}
				//if ( pageNbr * pageSize < pTotalRows )
				//	pageNbr++;
				//else
				//	isComplete = true;
			}

			//there could be multiple types
			importResults = string.Format( "ImportByGraphSearch {0} - Processed {1} records, with {2} exceptions. \r\n", registryEntityType, cntr, exceptionCtr );
			LoggingHelper.DoTrace( 2, importResults );
			if ( !string.IsNullOrWhiteSpace( importNote ) )
				importResults += importNote;

			recordsImported += cntr;

			return importResults;
		}



		public bool ProcessEnvelope( ReadEnvelope item, string registryEntityType, int cntr, bool doingDownloadOnly = false )
		{
			int entityTypeId = MappingHelperV3.GetEntityTypeId( item.EnvelopeCtdlType );

			return ProcessEnvelope( item, registryEntityType, entityTypeId, cntr, DoingDownloadOnly );
		}



		public bool ProcessEnvelope( ReadEnvelope item, string registryEntityType, int entityTypeId, int cntr, bool doingDownloadOnly = false )
		{
			bool importSuccessfull = false;
			if ( item == null || item.DecodedResource == null )
				return false;

			int newImportId = 0;
			var status = new SaveStatus
			{
				DoingDownloadOnly = doingDownloadOnly,
				ValidationGroup = string.Format( "{0} Import", registryEntityType ),
				OnlyImportIfNewerThanExisting = UtilityManager.GetAppKeyValue( "OnlyImportIfNewerThanExisting", false ),
			};

			DateTime started = DateTime.Now;
			DateTime envelopeUpdateDate = new DateTime();
			DateTime createDate = new DateTime();
			if ( DateTime.TryParse( item.NodeHeaders.CreatedAt.Replace( "UTC", "" ).Trim(), out createDate ) )
			{
				status.SetEnvelopeCreated( createDate );
			}
			if ( DateTime.TryParse( item.NodeHeaders.UpdatedAt.Replace( "UTC", "" ).Trim(), out envelopeUpdateDate ) )
			{
				status.SetEnvelopeUpdated( envelopeUpdateDate );
			}
			status.DocumentOwnedBy = item.documentOwnedBy;
			if ( item.documentPublishedBy != null )
			{
				if ( item.documentOwnedBy == null || ( item.documentPublishedBy != item.documentOwnedBy ) )
					status.DocumentPublishedBy = item.documentPublishedBy;
			}
			else
			{
				//will need to check elsewhere
				//OR as part of import check if existing one had 3rd party publisher
			}
			LoggingHelper.DoTrace( 2, string.Format( "{0}. {1} CTID {2}, Updated: {3} ", cntr, registryEntityType, item.EnvelopeCtid, envelopeUpdateDate.ToString() ) );
			var messages = new List<string>();

			string importError = "";
			importSuccessfull = false;
			//var documentPublishedBy = item.documentPublishedBy ?? ""
			//======================================================	
			//21-01-28 mp - moving common code back here to improve maintenance
			if ( item.documentPublishedBy != null )
			{
				//only providing DocumentPublishedBy where apparantly a 3PP
				if ( item.documentOwnedBy == null || ( item.documentPublishedBy != item.documentOwnedBy ) )
					status.DocumentPublishedBy = item.documentPublishedBy;
			}
			else
			{
				//will need to check elsewhere
				//OR as part of import check if existing one had 3rd party publisher
			}

			string payload = item.DecodedResource.ToString();
			status.EnvelopeId = item.EnvelopeIdentifier;
			var ctid = item.EnvelopeCtid;
			var envelopeCtdlType = item.EnvelopeCtdlType;
			string ctdlType = RegistryServices.GetResourceType( payload );
			//string envelopeUrl = RegistryServices.GetEnvelopeUrl( envelopeIdentifier );
			LoggingHelper.WriteLogFile( UtilityManager.GetAppKeyValue( "logFileTraceLevel", 5 ), item.EnvelopeCtid + "_" + ctdlType, payload, "", false );
			//
			if ( UtilityManager.GetAppKeyValue( "savingEnvelopeToFileSystem", false ) )
			{
				string envelope = item.ToString();
				LoggingHelper.WriteLogFile( 1, $"{item.EnvelopeCtid}_{ctdlType}_envelope", envelope, "", false );
			}
			//
			try
			{
				switch ( entityTypeId )
				{

					case 1:
						//importSuccessfull = credImportMgr.ProcessEnvelope( item, status );
						if ( ctdlType.IndexOf( "Organization" ) > -1 || ctdlType.IndexOf( "LearningOpportunity" ) > -1 || ctdlType.IndexOf( "Assessment" ) > -1 )
						{
							//how can this happen????
							//- expected as the FTS search will also search blank nodes
							importError = string.Format( "*****WHILE DOING A CREDENTIAL IMPORT (#{0}), A RECORD OF TYPE: '{1}', CTID: '{2}' WAS ENCOUNTERED! *********************", cntr, ctdlType, ctid );
							//status.AddError( importError );
							//LoggingHelper.DoTrace( 1, importError );
						}
						//else if ( ctdlType != envelopeCtdlType )
						//{
						//	LoggingHelper.DoTrace( 1, "___skipping blank node" );
						//}
						else
						{
							//TODO - indicating skipped?
							importSuccessfull = credImportMgr.ImportV3( payload, status );
						}

						break;
					case 2:
					case 13:
					case 14:
						//importSuccessfull = orgImportMgr.ProcessEnvelope( item, status );
						if ( ctdlType.IndexOf( "Organization" ) > -1 )
						{
							importSuccessfull = orgImportMgr.ImportV3( payload, status );
						}
						else
						{
							//how can this happen????
							importError = string.Format( "*****WHILE DOING AN ORGANIZATION IMPORT (#{0}), A RECORD OF TYPE: '{1}', CTID: '{2}' WAS ENCOUNTERED! *********************", cntr, ctdlType, ctid );
							status.AddError( importError );
							LoggingHelper.DoTrace( 1, importError );
						}
						break;
					case 3:
						if ( ctdlType.IndexOf( "Assessment" ) > -1 )
						{
							importSuccessfull = asmtImportMgr.ProcessEnvelope( item, status );
						}
						else
						{
							//how can this happen????
							importError = string.Format( "*****WHILE DOING AN Assessment IMPORT (#{0}), A RECORD OF TYPE: '{1}', CTID: '{2}' WAS ENCOUNTERED! *********************", cntr, ctdlType, ctid );
							status.AddError( importError );
							LoggingHelper.DoTrace( 1, importError );
						}

						break;
					case 7:
					case 36:
					case 37:
						if ( ctdlType.IndexOf( "LearningOpportunity" ) > -1 || ctdlType.IndexOf( "LearningProgram" ) > -1 || ctdlType.IndexOf( "Course" ) > -1 )
						{
							importSuccessfull = loppImportMgr.ProcessEnvelope( item, status );
						}
						else
						{
							//how can this happen????
							//importError = string.Format( "*****WHILE DOING A LearningOpportunity IMPORT (#{0}), A RECORD OF TYPE: '{1}', CTID: '{2}' WAS ENCOUNTERED! *********************", cntr, ctdlType, ctid );
							//status.AddError( importError );
							//LoggingHelper.DoTrace( 1, importError );
						}
						break;
					case 8:    //
						if ( ctdlType.IndexOf( "Pathway" ) > -1 )
							importSuccessfull = pathwayImportMgr.ProcessEnvelope( item, status );
						else
						{
							//how can this happen????
							//importError = string.Format( "*****WHILE DOING A Pathway IMPORT (#{0}), A RECORD OF TYPE: '{1}', CTID: '{2}' WAS ENCOUNTERED! *********************", cntr, ctdlType, ctid );
							//status.AddError( importError );
							//LoggingHelper.DoTrace( 1, importError );
						}
						break;
					case 9:    //
						if ( ctdlType == "Collection" || ctdlType == "ceterms:Collection" )
							importSuccessfull = cltnImportMgr.ProcessEnvelope( item, status );

						break;
					case 10:
						if ( ctdlType.IndexOf( "CompetencyFramework" ) > -1 )
							importSuccessfull = cfImportMgr.ProcessEnvelope( item, status );
						else
						{
							//how can this happen????
							//importError = string.Format( "*****WHILE DOING A CompetencyFramework IMPORT (#{0}), A RECORD OF TYPE: '{1}', CTID: '{2}' WAS ENCOUNTERED! *********************", cntr, ctdlType, ctid );
							//status.AddError( importError );
							//LoggingHelper.DoTrace( 1, importError );
						}

						break;
					//what about competency??? ==> use 17. don't have competency only imports, but should have a concrete entry for competencies
					case 11:    //concept scheme
						importSuccessfull = new ImportConceptSchemes().ProcessEnvelope( item, status );
						return true;
					case 12:    //progression model
						importSuccessfull = new ImportProgressionModels().ProcessEnvelope( item, status );
						return true;
					case 15:
						importSuccessfull = new ImportScheduledOfferings().ProcessEnvelope( item, status );
						break;
					case 19:
						importSuccessfull = cndManImportMgr.ProcessEnvelope( item, status );
						break;
					case 20:
						importSuccessfull = cstManImportMgr.ProcessEnvelope( item, status );
						break;
					case 22:
						importSuccessfull = new ImportCredentialingAction().ProcessEnvelope( item, status );
						break;
					case 23:
						importSuccessfull = new ImportPathwaySets().ProcessEnvelope( item, status );
						break;
					case 26:
						importSuccessfull = tvpImportMgr.ProcessEnvelope( item, status );
						break;
					case 28:
						importSuccessfull = transIntermediaryImportMgr.ProcessEnvelope( item, status );
						break;
					case 31:
						importSuccessfull = new ImportDataSetProfile().ProcessEnvelope( item, status );
						break;
					case 32:
						importSuccessfull = new ImportJob().ProcessEnvelope( item, status );
						break;
					case 33:
						//DisplayMessages( string.Format( "{0}. TaskProfiles ({1}) are not handled at this time. ", cntr, entityTypeId ) );

						importSuccessfull = new ImportTask().ProcessEnvelope( item, status );
						break;
					case 34:
						importSuccessfull = new ImportWorkRole().ProcessEnvelope( item, status );
						break;
					case 35:
						importSuccessfull = new ImportOccupation().ProcessEnvelope( item, status );
						break;
					case 38:
						importSuccessfull = new ImportSupportService().ProcessEnvelope( item, status );
						break;
					case 39:
						importSuccessfull = new ImportRubric().ProcessEnvelope( item, status );
						break;
					case 41:
						importSuccessfull = new ImportVerificationService().ProcessEnvelope( item, status );
						break;

					default:
						DisplayMessages( string.Format( "{0}. RegistryImport. Unhandled Entity type encountered: entityTypeId: {1} ", cntr, entityTypeId ) );
						break;
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
					LoggingHelper.LogError( ex, string.Format( registryEntityType + " Exception encountered in ctid: {0}", ctid ) );
					status.AddError( ex.Message );
					importError = ex.Message;
				}

				//make continue on exceptions an option
				//exceptionCtr++;
				//if ( maxExceptions > 0 && exceptionCtr > maxExceptions )
				//{
				//    //arbitrarily stop if large number of exceptions
				//    importNote = string.Format( thisClassName + " - {0} Many exceptions ({1}) were encountered during import - abandoning.", entityType, exceptionCtr );
				//    //Console.WriteLine( importNote );
				//    LoggingHelper.DoTrace( 1, importNote );
				//    LoggingHelper.LogError( importNote, true, thisClassName + "- many exceptions" );
				//    isComplete = true;
				//    break;
				//}
			}
			finally
			{
				if ( !importSuccessfull )
				{
					if ( string.IsNullOrWhiteSpace( importError ) )
					{
						importError = string.Join( "\r\n", status.GetAllMessages().ToArray() );
					}
				} else
				{
					//TODO - consider a check for in import pending and set processed if found!
				}
				//store document
				//add indicator of success
				if ( status.RecordWasSkipped == false )
				{
					newImportId = importMgr.Add( item, entityTypeId, status.Ctid, importSuccessfull, importError, ref messages );
					if ( newImportId > 0 && status.Messages != null && status.Messages.Count > 0 )
					{
						//add indicator of current recored
						string msg = string.Format( "========= Messages for {4}, EnvelopeIdentifier: {0}, ctid: {1}, Id: {2}, rowId: {3} =========", item.EnvelopeIdentifier, status.Ctid, status.DocumentId, status.DocumentRowId, registryEntityType );
						//ensure status has info on the current context, so can be include in messages. Or N/A. The message has the Import.Staging record as the parent 
						importMgr.AddMessages( newImportId, status, ref messages );
					}

					TimeSpan duration = DateTime.Now.Subtract( started );
					LoggingHelper.DoTrace( 2, string.Format( "         Total Duration: {0:N2} seconds ", duration.TotalSeconds ) );
				}
			} //finally


			return importSuccessfull;
		}
		public static string DisplayMessages( string message )
		{
			LoggingHelper.DoTrace( 1, message );
			//Console.WriteLine( message );

			return message;
		}
	
	}
}

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
		/// <summary>
		/// Start process to download documents from the credential registry
		/// NOTE: MAY NEED TO TURN OFF ANTIVIRUS/SPAM PROGRAM OTHERWISE EACH FILE WILL BE CHECKED BEFORE SAVE.
		/// </summary>
		/// <param name="args"></param>
		static void Main( string[] args )
		{

			TimeZone zone = TimeZone.CurrentTimeZone;
			// Demonstrate ToLocalTime and ToUniversalTime.
			DateTime local = zone.ToLocalTime( DateTime.Now );
			LoggingHelper.DoTrace( 1, "Local time: " + local );
			var importSummary = new List<string>();
			//

			//verify if api key was provided
			string credentialEngineAPIKey = UtilityManager.GetAppKeyValue( "MyCredentialEngineAPIKey" );
			string environment = UtilityManager.GetAppKeyValue( "envType" );
			if ( environment == "sandbox" && credentialEngineAPIKey == "PROVIDE YOUR ACCOUNTS API KEY" )
			{
				credentialEngineAPIKey = "";
				LoggingHelper.DoTrace( 1, "NOTE: an API key was not provided for the search. This is allowed for the sandbox, but not for production." );
			}
			if ( environment == "production" && ( string.IsNullOrWhiteSpace( credentialEngineAPIKey ) || credentialEngineAPIKey == "PROVIDE YOUR ACCOUNTS API KEY" ) )
			{
				LoggingHelper.DoTrace( 1, "NOTE: an API key was not provided for the search. This is required for production." );
				return;
			}
			//Get the schedule type
			string scheduleType = UtilityManager.GetAppKeyValue( "scheduleType", "daily" );
			//get the delete action - typical is 2 to skip deletes
			int deleteAction = UtilityManager.GetAppKeyValue( "deleteAction", 0 );

			string defaultCommunity = UtilityManager.GetAppKeyValue( "defaultCommunity" );
			string additionalCommunity = UtilityManager.GetAppKeyValue( "additionalCommunity" );

			var registryImport = new RegistryHelper( defaultCommunity );

			//
			registryImport.SavingDocumentToFileSystem = UtilityManager.GetAppKeyValue( "savingDocumentToFileSystem", true );
			registryImport.SavingDocumentToDatabase = UtilityManager.GetAppKeyValue( "savingDocumentToDatabase", false );
			if ( !registryImport.SavingDocumentToFileSystem && !registryImport.SavingDocumentToDatabase )
			{
				LoggingHelper.DoTrace( 1, string.Format( "*****************  ERROR ***************** " ) );
				LoggingHelper.DoTrace( 1, string.Format( "You must have at least one of: 'savingDocumentToFileSystem' OR 'savingDocumentToDatabase'  set to true, or you will have no results for your download! " ) );
				LoggingHelper.DoTrace( 1, string.Format( "****************************************** " ) );
				return;
			}

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



			//establish common filters
			//if you always only want to download documents for a particular organization, provide the CTID for 'owningOrganizationCTID' in the app.config.
			//TODO - update to handle a list
			registryImport.OwningOrganizationCTID = UtilityManager.GetAppKeyValue( "owningOrganizationCTID" );

			//if you want to download documents published by a third party publisher, provide the CTID for 'publishingOrganizationCTID' in the app.config. 
			//NOTE: where the publisher and the owner are the same, there is no need to provide both the owning and publishing org filters, just pick one.
			registryImport.PublishingOrganizationCTID = UtilityManager.GetAppKeyValue( "publishingOrganizationCTID" );

			registryImport.StartingDate = DateTime.Now.AddDays( -1 ).ToString();
			//typically will want this as registry server is UTC (+6 hours from central)
			bool usingUTC_ForTime = UtilityManager.GetAppKeyValue( "usingUTC_ForTime", true );


			//could ignore end date until a special scedule type of adhoc is used, then read the dates from config
			importSummary.Add( DisplayMessages( string.Format( " - Schedule type: {0} ", scheduleType ) ) );
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
				DisplayMessages( string.Format( " - Community: {0}, Updates since: {1} {2}", defaultCommunity, registryImport.StartingDate, usingUTC_ForTime ? " (UTC)" : "" ) );
			}
			else if ( scheduleType == "adhoc" )
			{
				registryImport.StartingDate = UtilityManager.GetAppKeyValue( "startingDate", "" );
				registryImport.EndingDate = UtilityManager.GetAppKeyValue( "endingDate", "" );
				DateTime dtcheck = System.DateTime.Now;             
				//LoggingHelper.DoTrace( 1, string.Format( " - Updates from: {0} to {1} ", registryImport.StartingDate, registryImport.EndingDate ) );

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
				DisplayMessages( string.Format( " - Updates from: {0} to {1} for community: {2}", registryImport.StartingDate, registryImport.EndingDate, defaultCommunity ) );
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
				DisplayMessages( string.Format( " - Updates since: {0} {1}, community: {2}", registryImport.StartingDate, usingUTC_ForTime ? " (UTC)" : "", defaultCommunity ) );
			}
			else
			{
				//assume daily
				registryImport.StartingDate = DateTime.Now.AddDays( -1 ).ToString( "yyyy-MM-ddTHH:mm:ss" );
				//format into: 2016-08-01T23:59:59
				registryImport.EndingDate = "";
				//LoggingHelper.DoTrace( 1, string.Format( " - Updates since: {0} ", registryImport.StartingDate ) );
				DisplayMessages( string.Format( " - Updates since: {0} ", registryImport.StartingDate ) );
			}
			#endregion

			//===================================================================================================

			//set to zero to handle all, or a number to limit records to process
			//Typically a limit would be used for testing
			//Alternately, run several times with different date ranges
			int maxImportRecords = UtilityManager.GetAppKeyValue( "maxImportRecords", 0 );


			//Actions: 0-normal; 1-DeleteOnly; 2-SkipDelete (default/recommended)
			int recordsDeleted = 0;
			if ( deleteAction < 2 )
			{
				//handle deleted records
				registryImport.HandleDeletes( defaultCommunity, maxImportRecords, ref recordsDeleted );
			}
			int recordsImported = 0;
			string sortOrder = "asc";
			//
			var resourceTypeList = GetRequestedResourceTypes();
			//
			#region Option for a list of data owners
			var ownedByList = new List<string>();
			if ( registryImport.OwningOrganizationCTID?.Length > 0 )
			{
				if ( registryImport.OwningOrganizationCTID.IndexOf( "," ) > 0 )
				{
					//get list
					var ownerslist = registryImport.OwningOrganizationCTID.Split( ',' );
					foreach ( var item in ownerslist )
					{
						if ( !string.IsNullOrWhiteSpace( item ) )
							ownedByList.Add( item.Trim() );
					}
					//
				}
				else
				{
					ownedByList.Add( registryImport.OwningOrganizationCTID );
				}
				//process
				foreach ( var item in ownedByList )
				{
					LoggingHelper.DoTrace( 1, string.Format( "===  *****************  Downloading all recent recources for Org: {0}  ***************** ", item ) );
					registryImport.OwningOrganizationCTID = item;
					//WARNING - IF NO RESOURCE TYPE IS INCLUDED WILL GET ceasn:Competency as well. This is not terrible as these will result in competency framework file being overwritten (so only one), but slows the process
					//may want to get the list of target resources. We would eventually have similar issues with pathway components;
					foreach ( var resourceType in resourceTypeList )
					{
						registryImport.Retrieve( resourceType, maxImportRecords, ref recordsImported, ref importSummary );

					}
				}
			
				LoggingHelper.DoTrace( 1, string.Format("Completed download request. Resources:{0}",recordsImported) );
				return;
			}
			
			
            #endregion

            
			if ( deleteAction != 1 )
			{
				//22-04-12 NEW - just use a list from app.config
				foreach ( var resourceType in resourceTypeList )
				{
					registryImport.Retrieve( resourceType, maxImportRecords, ref recordsImported, ref importSummary );

				}
				/*
				//do manifests 
				registryImport.Retrieve( "condition_manifest_schema", maxImportRecords, ref recordsImported, ref importSummary );
				//
				registryImport.Retrieve( "cost_manifest_schema", maxImportRecords, ref recordsImported, ref importSummary );

				//handle assessments
				registryImport.Retrieve( "assessment_profile", maxImportRecords, ref recordsImported, ref importSummary );

				//handle learning opps
				registryImport.Retrieve( "learning_opportunity_profile", maxImportRecords, ref recordsImported, ref importSummary );

				//handle credentials
				registryImport.Retrieve( "credential", maxImportRecords, ref recordsImported, ref importSummary );

				//competency frameworks
				registryImport.Retrieve( "competency_framework", maxImportRecords, ref recordsImported, ref importSummary );

				//TVP
				registryImport.Retrieve( "transfer_value_profile", maxImportRecords, ref recordsImported, ref importSummary, sortOrder );

				//pathways 
				if ( UtilityManager.GetAppKeyValue( "importing_pathway", true ) )
				{
					registryImport.Retrieve( "pathway", maxImportRecords, ref recordsImported, ref importSummary );
				}
				//
				if ( UtilityManager.GetAppKeyValue( "importing_pathwayset", true ) )
				{
					registryImport.Retrieve( "pathway_set", maxImportRecords, ref recordsImported, ref importSummary );
				}

				//handle organizations last
				registryImport.Retrieve( "organization",  maxImportRecords, ref recordsImported, ref importSummary );
				*/
				//
				TimeSpan duration = DateTime.Now.Subtract( local );
				LoggingHelper.DoTrace( 1, string.Format( "********* COMPLETED {0:c} minutes *********", duration.TotalMinutes ) );
			}
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

		public static string DisplayMessages( string message )
		{
			LoggingHelper.DoTrace( 1, message );
			//Console.WriteLine( message );

			return message;
		}

	}
}

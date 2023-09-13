using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Utilities;
using workIT.Factories;
using workIT.Models;
using workIT.Services;
using ElasticHelper = workIT.Services.ElasticServices;
using System.ComponentModel.Design;

namespace ElasticIndexBuild
{
	class Program
	{
		static void Main( string[] args )
		{

			//************ make sure using proper database and elastic collections ***************

			if ( UtilityManager.GetAppKeyValue( "populatingAllCaches", true ) )
			{
				DisplayMessages( "Populating All Caches" );
				//determine if doing arbitrarily
				//if true, rebuilds all caches
				new CacheManager().PopulateAllCaches( true );
			} else
				DisplayMessages( "Skipping Populating of All Caches" );


			//if true, delete the index first and add, or if false just do updates
			bool deletingIndexBeforeRebuild = UtilityManager.GetAppKeyValue( "deletingIndexBeforeRebuild", true );

			if ( UtilityManager.GetAppKeyValue( "processCustomFiltersRequests", false ) )
			{
				HandleRequestsCustomFiltersRequests();
				return;
			}


			//set the related appKey to empty, to skip one of the loads
			if ( args != null && args.Count() > 0 )
			{
				var requestType = args[0];
				if ( requestType == "processPendingRequests" )		
				{
					var buildTypeId = "0";
					if (args.Count() > 0)
                    {
						buildTypeId = args[1];
					}
					HandlePendingRequests( buildTypeId );
				}
				else
				{
					//expect 1-5 for cred, org, asmt, lopp, cf
					if ( Int32.TryParse( args[0], out int requestId ) )
					{
						CustomBuildRequest( requestId, deletingIndexBeforeRebuild );
					}
					else
					{
						DisplayMessages( string.Format( "ElasticIndexBuild. Invalid argument encountered: {0}. only the values 1-6 are valid. Ending", args[0] ) );
					}
				}
			}
			else
			{

				if ( UtilityManager.GetAppKeyValue( "processPendingRequests", false ) == true )
				{
					HandlePendingRequests( "0" );
					return;
				}

				AdhocBuildRequest( deletingIndexBeforeRebuild );

			}
		}

		public static void CustomBuildRequest( int requestId, bool deletingIndexBeforeRebuild )
		{
			LoggingHelper.DoTrace( 1, "ElasticIndexBuild.CustomBuildRequest Started" );
			switch ( requestId )
			{
				case 1:
					LoadCredentialsIndex( deletingIndexBeforeRebuild );
					break;
				case 2:
					LoadOrganizationIndex( deletingIndexBeforeRebuild );
					break;
				case 3:
					LoadAssessmentIndex( deletingIndexBeforeRebuild );
					break;
				case 4:
					LoadLearningOpportunityIndex( deletingIndexBeforeRebuild );
					break;
				case 5:
					LoadCompetencyFrameworkIndex( deletingIndexBeforeRebuild );
					break;
				case 6:
					LoadCommonIndex( deletingIndexBeforeRebuild );
					break;
				//ase 7:
				//	LoadCompetencyFrameworkIndex( deletingIndexBeforeRebuild );
				//	break;c
				default:
					DisplayMessages( string.Format( "ElasticIndexBuild.CustomBuildRequest Unhandled argument request identifier encountered: {0}, ending", requestId ) );
					break;
			}
		}

		/// <summary>
		/// Assume that will never delete the index for a custom filter request
		/// </summary>
		public static void HandleRequestsCustomFiltersRequests()
		{
			LoggingHelper.DoTrace( 1, "ElasticIndexBuild.HandleRequestsCustomFiltersRequests Started" );
			int processed = 0;

			var filter = UtilityManager.GetAppKeyValue( "credentialsCustomFilter", "" );
			if ( !string.IsNullOrWhiteSpace( filter ) )
			{
				processed = 0;
				ElasticServices.Credential_UpdateIndex( filter, ref processed );
			}

			filter = UtilityManager.GetAppKeyValue( "organizationsCustomFilter", "" );
			if ( !string.IsNullOrWhiteSpace( filter ) )
			{
				processed = 0;
				ElasticServices.Organization_UpdateIndex( filter, ref processed );
			}

			filter = UtilityManager.GetAppKeyValue( "loppCustomFilter", "" );
			if ( !string.IsNullOrWhiteSpace( filter ) )
			{
				processed = 0;
				ElasticServices.LearningOpp_UpdateIndex( filter, ref processed );
			}

			filter = UtilityManager.GetAppKeyValue( "asmtCustomFilter", "" );
			if ( !string.IsNullOrWhiteSpace( filter ) )
			{
				processed = 0;
				ElasticServices.Assessment_UpdateIndex( filter, ref processed );
			}
		}

		public static void AdhocBuildRequest( bool deletingIndexBeforeRebuild )
		{
			if ( !string.IsNullOrWhiteSpace( UtilityManager.GetAppKeyValue( "commonCollection", "" ) ) )
				LoadCommonIndex( deletingIndexBeforeRebuild );
			if ( !string.IsNullOrWhiteSpace( UtilityManager.GetAppKeyValue( "pathwayCollection", "" ) ) )
				LoadPathwayIndex( deletingIndexBeforeRebuild );

			if ( !string.IsNullOrWhiteSpace( UtilityManager.GetAppKeyValue( "organizationCollection", "" ) ) )
				LoadOrganizationIndex( deletingIndexBeforeRebuild );

			if ( !string.IsNullOrWhiteSpace( UtilityManager.GetAppKeyValue( "competencyFrameworkCollection", "" ) ) )
				LoadCompetencyFrameworkIndex( deletingIndexBeforeRebuild );

			if ( !string.IsNullOrWhiteSpace( UtilityManager.GetAppKeyValue( "assessmentCollection", "" ) ) )
				LoadAssessmentIndex( deletingIndexBeforeRebuild );

			if ( !string.IsNullOrWhiteSpace( UtilityManager.GetAppKeyValue( "learningOppCollection", "" ) ) )
				LoadLearningOpportunityIndex( deletingIndexBeforeRebuild );

			//NOTE if credentialCollection is empty, will get: "Dispatching IndicesExists() from NEST into to Elasticsearch.NET" (no default index name)
			//so using a custom appKey instead
			if ( UtilityManager.GetAppKeyValue( "buildingCredentialIndex", true ) )
				LoadCredentialsIndex( deletingIndexBeforeRebuild );
		}

		public static void HandlePendingRequests( string entityRequestTypeId )
		{
			LoggingHelper.DoTrace( 1, "ElasticIndexBuild.HandlePendingRequests Started" );
			List<String> messages = new List<string>();
			if ( Int32.TryParse( entityRequestTypeId, out int entityTypeId ) )
			{
				switch ( entityTypeId )
				{
					case 0:	// all
						ElasticServices.HandlePendingReindexRequests( ref messages, 0 );
						break;
					case 1:
					case 2:
					case 3:
					case 7:
					case 36:
					case 37:
					case 8:
					case 9:		//collection:
					case 10:    //framework
					case 11:    //concept scheme			
					case 12:
					case 13:
					case 14:
						ElasticServices.HandlePendingReindexRequests( ref messages, entityTypeId );
						break;
					case 23:    //pathwaySet
					case 26:    //TVP

						//General is not ready. It should do both whenver requested 
						ElasticServices.HandlePendingReindexRequests( ref messages, entityTypeId );
						break;
					
					default:
						DisplayMessages( string.Format( "ElasticIndexBuild.HandlePendingRequests. Unhandled request identifier encountered: {0}, ending", entityRequestTypeId ) );
						break;
				}
			}
		}
		 
		public static void LoadCredentialsIndex( bool deletingIndexBeforeRebuild )
		{
            DisplayMessages( "Starting LoadCredentialsIndex: " + UtilityManager.GetAppKeyValue( "credentialCollection", "missing credential" ) );
			//TODO
			//	- add custom filters
			//	- enable starting pending search index values
			try
            {
				DateTime start = DateTime.Now;
				ElasticHelper.Credential_BuildIndex( deletingIndexBeforeRebuild, true );
				DateTime end = DateTime.Now;
				var elasped = end.Subtract( start ).TotalSeconds;

				DisplayMessages( string.Format("___Completed LoadCredentialsIndex. Elapsed Seconds: {0}", elasped ) );
			} catch (Exception ex)
            {
                LoggingHelper.LogError( ex, "LoadCredentialsIndex Failed", "ElasticIndex Build Exception" );
            }
		}


		public static void LoadOrganizationIndex( bool deletingIndexBeforeRebuild )
		{
            DisplayMessages( "Starting LoadOrganizationIndex: " + UtilityManager.GetAppKeyValue( "organizationCollection", "missing organizationCollection" ) );
			try
            {
				DateTime start = DateTime.Now;
				ElasticHelper.Organization_BuildIndex( deletingIndexBeforeRebuild, true );
				DateTime end = DateTime.Now;
				var elasped = end.Subtract( start ).TotalSeconds;

				DisplayMessages( string.Format( "___Completed LoadOrganizationIndex. Elapsed Seconds: {0}", elasped ) );
			}
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, "LoadOrganizationIndex Failed", "ElasticIndex Build Exception" );
            }
        }


		public static void LoadAssessmentIndex( bool deletingIndexBeforeRebuild )
		{
            DisplayMessages( "Starting LoadAssessmentIndex: " + UtilityManager.GetAppKeyValue( "assessmentCollection", "missing assessmentCollection" ) );
			try
            {
				DateTime start = DateTime.Now;
				ElasticHelper.Assessment_BuildIndex( deletingIndexBeforeRebuild, true );
				DateTime end = DateTime.Now;
				var elasped = end.Subtract( start ).TotalSeconds;

				DisplayMessages( string.Format( "___Completed LoadAssessmentIndex. Elapsed Seconds: {0}", elasped ) );
				
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, "LoadAssessmentIndex Failed", "ElasticIndex Build Exception" );
            }
        }


		public static void LoadLearningOpportunityIndex( bool deletingIndexBeforeRebuild )
		{
            DisplayMessages( "Starting LoadLearningOpportunityIndex: " + UtilityManager.GetAppKeyValue( "learningOppCollection", "missing learningOppCollection" ) );

			try
            {
				DateTime start = DateTime.Now;
				ElasticHelper.LearningOpp_BuildIndex( deletingIndexBeforeRebuild, true );
				DateTime end = DateTime.Now;
				var elasped = end.Subtract( start ).TotalSeconds;

				DisplayMessages( string.Format( "___Completed LoadLearningOpportunityIndex. Elapsed Seconds: {0}", elasped ) );
				
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, "LoadLearningOpportunityIndex Failed", "ElasticIndex Build Exception" );
            }
        }


		public static void LoadCompetencyFrameworkIndex(bool deletingIndexBeforeRebuild)
		{
			DisplayMessages( "Starting LoadCompetencyFrameworkIndex: " + UtilityManager.GetAppKeyValue( "competencyFrameworkCollection", "missing competencyFrameworkCollection" ) );

			try
			{
				DateTime start = DateTime.Now;
				ElasticHelper.CompetencyFramework_BuildIndex( deletingIndexBeforeRebuild, true );
				DateTime end = DateTime.Now;
				var elasped = end.Subtract( start ).TotalSeconds;

				DisplayMessages( string.Format( "___Completed LoadCompetencyFrameworkIndex. Elapsed Seconds: {0}", elasped ) );

			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "LoadCompetencyFrameworkIndex Failed", "ElasticIndex Build Exception" );
			}
		}
		public static void LoadCommonIndex( bool deletingIndexBeforeRebuild )
		{
			DisplayMessages( "Starting LoadCommonIndex: " + UtilityManager.GetAppKeyValue( "commonCollection", "missing commonCollection" ) );

			try
			{
				//int processed = 0;
				DateTime start = DateTime.Now;
				DateTime end = DateTime.Now;
				var includeTransferValueInBuild= UtilityManager.GetAppKeyValue( "includeTransferValueInBuild", true );
				var includeTransferIntermediaryInBuild = UtilityManager.GetAppKeyValue( "includeTransferIntermediaryInBuild", true );
				var includeCollectionInBuild = UtilityManager.GetAppKeyValue( "includeCollectionInBuild", true );
                var includePathwaySetInBuild = UtilityManager.GetAppKeyValue( "includePathwaySetInBuild", true );

                //	TODO - just define a direct method to delete and recreate 
                if ( deletingIndexBeforeRebuild )
                {
					ElasticHelper.GeneralIndex_Reset();
				}
				deletingIndexBeforeRebuild = false;
				if ( !includeTransferValueInBuild || !includeTransferIntermediaryInBuild || !includeCollectionInBuild )
                {
					//if any are excluded, delete should be turned off
					
					
				}

				//NO - pathways has its own index now
				//do delete  for first one, but not second
				/*
				DisplayMessages( "=========== Starting Pathway ===========" );
				ElasticHelper.Pathway_BuildIndex( deletingIndexBeforeRebuild, true );
				DateTime end = DateTime.Now;
				var elasped = end.Subtract( start ).TotalSeconds;
				DisplayMessages( string.Format( "___Completed LoadCommonIndex for pathway. Elapsed Seconds: {0}", elasped ) );
				deletingIndexBeforeRebuild = false;
				*/
				//next

				if ( includeTransferValueInBuild )
				{
					DisplayMessages( "=========== Starting Transfer Value Profile ===========" );
					start = DateTime.Now;
					ElasticHelper.General_BuildIndexForTVP( deletingIndexBeforeRebuild, true );
					DateTime end2 = DateTime.Now;
					var tvpElasped = end2.Subtract( start ).TotalSeconds;

					DisplayMessages( string.Format( "___Completed LoadCommonIndex for transfer value. Elapsed Seconds: {0}", tvpElasped ) );
				}
				deletingIndexBeforeRebuild = false;
				//
				if ( includeTransferIntermediaryInBuild )
				{
					DisplayMessages( "=========== Starting Transfer Intermediary ===========" );
					start = DateTime.Now;
					ElasticHelper.General_BuildIndexForTransferIntermediary( false, true );
					end = DateTime.Now;
					var tiElasped = end.Subtract( start ).TotalSeconds;
					DisplayMessages( string.Format( "___Completed LoadCommonIndex for transfer intermediary. Elapsed Seconds: {0}", tiElasped ) );
				}
                if ( UtilityManager.GetAppKeyValue( "includePathwaySetInBuild", true ) )
                {
                    ElasticHelper.General_BuildIndexForPathwaySet();
                }
                //
                if ( UtilityManager.GetAppKeyValue( "includeOccupationInBuild", true ) )
                {
					ElasticHelper.General_BuildIndexForOccupation();
				}
				//
				if ( UtilityManager.GetAppKeyValue( "includeJobInBuild", true ) )
				{
					ElasticHelper.General_BuildIndexForJob(true);
				}
				//
				if ( UtilityManager.GetAppKeyValue( "includeTaskInBuild", true ) )
				{
					ElasticHelper.General_BuildIndexForTask();
				}
				//
				if ( UtilityManager.GetAppKeyValue( "includeWorkRoleInBuild", true ) )
				{
					ElasticHelper.General_BuildIndexForWorkRole();
				}
				//
				if ( includeCollectionInBuild )
				{
					DisplayMessages( " =========== Starting Collection ===========" );
					start = DateTime.Now;
					ElasticHelper.General_BuildIndexForCollection( false, true );
					end = DateTime.Now;
					var colElasped = end.Subtract( start ).TotalSeconds;

					DisplayMessages( string.Format( "___Completed LoadCommonIndex for Collection. Elapsed Seconds: {0}", colElasped ) );
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "LoadCommonIndex Failed", "ElasticIndex Build Exception" );
			}
		}
		public static void LoadPathwayIndex( bool deletingIndexBeforeRebuild )
		{
			DisplayMessages( "Starting LoadPathwayIndex: " + UtilityManager.GetAppKeyValue( "pathwayCollection", "missing PathwayCollection" ) );

			try
			{
				//int processed = 0;
				DateTime start = DateTime.Now;
				//do delete  for first one, but not second
				DisplayMessages( "Starting Pathway" );
				ElasticHelper.Pathway_BuildIndex( deletingIndexBeforeRebuild, true );
				DateTime end = DateTime.Now;
				var elasped = end.Subtract( start ).TotalSeconds;
				DisplayMessages( string.Format( "___Completed LoadPathwayIndex for pathway. Elapsed Seconds: {0}", elasped ) );
				deletingIndexBeforeRebuild = false;


			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "LoadPathwayIndex Failed", "ElasticIndex Build Exception" );
			}
		}
		public static string DisplayMessages( string message )
        {
            LoggingHelper.DoTrace( 1, message );
            //Console.WriteLine( message );

            return message;
        }
    }
}

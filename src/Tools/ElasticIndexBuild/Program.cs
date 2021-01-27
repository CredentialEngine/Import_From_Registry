using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Utilities;
using workIT.Factories;
using workIT.Models;
using workIT.Services;
using System.ComponentModel.Design;

namespace ElasticIndexBuild
{
	class Program
	{
		static void Main( string[] args )
		{

			//************ make sure using proper database and elastic collections ***************

            if ( UtilityManager.GetAppKeyValue("populatingAllCaches", true) )
            {
                DisplayMessages("Populating All Caches");
                //determine if doing arbitrarily
                new CacheManager().PopulateAllCaches(true);
            } else
                DisplayMessages("Skipping Populating of All Caches");


			//set the related appKey to empty, to skip one of the loads
			bool deletingIndexBeforeRebuild = UtilityManager.GetAppKeyValue( "deletingIndexBeforeRebuild", true );

			if ( args != null && args.Count() > 0 )
			{
				//expect 1-5 for cred, org, asmt, lopp, cf
				if ( Int32.TryParse( args[ 0 ], out int requestId ) )
				{
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
							DisplayMessages( string.Format("ElasticIndexBuild. Unhandled argument request identifier encountered: {0}, ending", requestId) ); 
							break;
					}
				} else
				{
					DisplayMessages( string.Format( "ElasticIndexBuild. Invalid argument encountered: {0}. only the values 1-5 are valid. Ending", args[ 0 ] ) ); 
				}			
			}
			else
			{
				if ( !string.IsNullOrWhiteSpace( UtilityManager.GetAppKeyValue( "commonCollection", "" ) ) )
					LoadCommonIndex( deletingIndexBeforeRebuild );

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

		}
		 
		public static void LoadCredentialsIndex( bool deletingIndexBeforeRebuild )
		{
            DisplayMessages( "Starting LoadCredentialsIndex: " + UtilityManager.GetAppKeyValue( "credentialCollection", "missing credential" ) );
			
			try
            {
				DateTime start = DateTime.Now;
				ElasticServices.Credential_BuildIndex( deletingIndexBeforeRebuild, true );
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
				ElasticServices.Organization_BuildIndex( deletingIndexBeforeRebuild, true );
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
				ElasticServices.Assessment_BuildIndex( deletingIndexBeforeRebuild, true );
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
				ElasticServices.LearningOpp_BuildIndex( deletingIndexBeforeRebuild, true );
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
				ElasticServices.CompetencyFramework_BuildIndex( deletingIndexBeforeRebuild, true );
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
				//do delete  for first one, but not second
				DisplayMessages( "Starting Pathway" );
				ElasticServices.Pathway_BuildIndex( deletingIndexBeforeRebuild, true );
				DateTime end = DateTime.Now;
				var elasped = end.Subtract( start ).TotalSeconds;
				DisplayMessages( string.Format( "___Completed LoadCommonIndex for pathway. Elapsed Seconds: {0}", elasped ) ); 
				deletingIndexBeforeRebuild = false;
				 //next
				 //processed = 0;
				DisplayMessages( "Starting Transfer Value Profile" );
				start = DateTime.Now;
				ElasticServices.General_BuildIndexForTVP( deletingIndexBeforeRebuild, true );
				//ElasticServices.General_UpdateIndexForTVP( "", ref processed ); 
				DateTime end2 = DateTime.Now;
				var tvpElasped = end2.Subtract( start ).TotalSeconds;

				DisplayMessages( string.Format( "___Completed LoadCommonIndex for transfer value. Elapsed Seconds: {0}", tvpElasped ) );

			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "LoadCommonIndex Failed", "ElasticIndex Build Exception" );
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

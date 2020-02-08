using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Utilities;
using workIT.Factories;
using workIT.Models;
using workIT.Services;

namespace ElasticIndexBuild
{
	class Program
	{
		static void Main( string[] args )
		{
            if ( UtilityManager.GetAppKeyValue("populatingAllCaches", true) )
            {
                DisplayMessages("Populating All Caches");
                //determine if doing arbitrarily
                new CacheManager().PopulateAllCaches(true);
            } else
                DisplayMessages("Skipping Populating of All Caches");

			//set the related appKey to empty, to skip one of the loads
			bool deletingIndexBeforeRebuild = UtilityManager.GetAppKeyValue( "deletingIndexBeforeRebuild", true );

			if (!string.IsNullOrWhiteSpace(UtilityManager.GetAppKeyValue( "assessmentCollection", "")))
                LoadAssessmentIndex( deletingIndexBeforeRebuild );

            if ( !string.IsNullOrWhiteSpace( UtilityManager.GetAppKeyValue( "learningOppCollection", "" ) ) )
                LoadLearningOpportunityIndex( deletingIndexBeforeRebuild );

            if ( !string.IsNullOrWhiteSpace( UtilityManager.GetAppKeyValue( "organizationCollection", "" ) ) )
                LoadOrganizationIndex( deletingIndexBeforeRebuild );

			if ( !string.IsNullOrWhiteSpace( UtilityManager.GetAppKeyValue( "credentialCollection", "" ) ) )
				 LoadCredentialsIndex( deletingIndexBeforeRebuild );


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

        public static string DisplayMessages( string message )
        {
            LoggingHelper.DoTrace( 1, message );
            //Console.WriteLine( message );

            return message;
        }
    }
}

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
            if (!string.IsNullOrWhiteSpace(UtilityManager.GetAppKeyValue( "credentialCollection", "")))
			    LoadCredentialsIndex();


            if (!string.IsNullOrWhiteSpace(UtilityManager.GetAppKeyValue( "assessmentCollection", "")))
                LoadAssessmentIndex();

            if ( !string.IsNullOrWhiteSpace( UtilityManager.GetAppKeyValue( "learningOppCollection", "" ) ) )
                LoadLearningOpportunityIndex();

            if ( !string.IsNullOrWhiteSpace( UtilityManager.GetAppKeyValue( "organizationCollection", "" ) ) )
                LoadOrganizationIndex();

        }

        public static void LoadCredentialsIndex()
		{
            DisplayMessages( "Starting LoadCredentialsIndex" );
            try
            {


                ElasticServices.Credential_BuildIndex( true );
            } catch (Exception ex)
            {
                LoggingHelper.LogError( ex, "LoadCredentialsIndex Failed", "ElasticIndex Build Exception" );
            }
		}


		public static void LoadOrganizationIndex()
		{
            DisplayMessages( "Starting LoadOrganizationIndex" );
            try
            {
                ElasticServices.Organization_BuildIndex( true );
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, "LoadOrganizationIndex Failed", "ElasticIndex Build Exception" );
            }
        }


		public static void LoadAssessmentIndex()
		{
            DisplayMessages( "Starting LoadAssessmentIndex" );
            try
            {
                ElasticServices.Assessment_BuildIndex( true );
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, "LoadAssessmentIndex Failed", "ElasticIndex Build Exception" );
            }
        }


		public static void LoadLearningOpportunityIndex()
		{
            DisplayMessages( "Starting LoadLearningOpportunityIndex" );

            try
            {
                ElasticServices.LearningOpp_BuildIndex( true );
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, "LoadLearningOpportunityIndex Failed", "ElasticIndex Build Exception" );
            }
        }

        public static string DisplayMessages( string message )
        {
            LoggingHelper.DoTrace( 1, System.DateTime.Now.ToString() + " - " + message );
            Console.WriteLine( message );

            return message;
        }
    }
}

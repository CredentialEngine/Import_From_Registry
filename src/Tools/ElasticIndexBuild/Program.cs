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
            //determine if doing arbitrarily
            new CacheManager().PopulateAllCaches();

            //set the related appKey to empty, to skip one of the loads
            if (!string.IsNullOrWhiteSpace(UtilityManager.GetAppKeyValue( "credentialCollection", "")))
			    LoadCredentialsIndex();

            if (!string.IsNullOrWhiteSpace(UtilityManager.GetAppKeyValue( "organizationCollection", "")))
                LoadOrganizationIndex();


            if (!string.IsNullOrWhiteSpace(UtilityManager.GetAppKeyValue( "assessmentCollection", "")))
                LoadAssessmentIndex();


            if ( !string.IsNullOrWhiteSpace( UtilityManager.GetAppKeyValue( "learningOppCollection", "" ) ) )
                LoadLearningOpportunityIndex();
        }

		public static void LoadCredentialsIndex()
		{
            DisplayMessages( "Starting LoadCredentialsIndex" );
            try
            {


                ElasticServices.BuildCredentialIndex( true );
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
                ElasticServices.BuildOrganizationIndex( true );
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
                ElasticServices.BuildAssessmentIndex( true );
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
                ElasticServices.BuildLearningOppIndex( true );
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, "LoadLearningOpportunityIndex Failed", "ElasticIndex Build Exception" );
            }
        }

        public static string DisplayMessages( string message )
        {
            LoggingHelper.DoTrace( 1, message );
            Console.WriteLine( message );

            return message;
        }
    }
}

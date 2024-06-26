using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Factories;
using workIT.Models;
using workIT.Models.Common;
using workIT.Utilities;

namespace Import.Services
{
    public class ImportUtilities
    {
		static string thisClassName = "Import.Services.ImportUtilities";
		public bool HandleDeleteRequest( int cntr, string ctid, string ctdlType, ref string statusMessage )
		{
			statusMessage = "";
			List<string> messages = new List<string>();

			bool isValid = true;
            if ( string.IsNullOrWhiteSpace( ctid ) )
            {
				statusMessage = "Error: The Delete Request CTID is required.";
            }
            if (string.IsNullOrWhiteSpace( ctdlType ) )
			{
				var resource = EntityManager.EntityCacheGetByCTID( ctid );
				if ( resource != null && resource.Id > 0 )
				{
					ctdlType = resource.EntityType;
				}
			}
			ctdlType = ctdlType.Replace( "ceterms:", "" );

            DisplayMessages( string.Format( "{0}. Deleting {1} by ctid: {2} ", cntr, ctdlType, ctid ) );

			switch ( ctdlType.ToLower() )
			{
				case "credentialorganization":
				case "qacredentialorganization":
				case "organization":
					if ( !new OrganizationManager().Delete( ctid, ref statusMessage ) )
						DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;

				case "assessmentprofile":
				case "assessment":
					if ( !new AssessmentManager().Delete( ctid, ref statusMessage ) )
						DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
				case "learningopportunityprofile":
				case "learningopportunity":
				case "learningprogram":
				case "course":
					if ( !new LearningOpportunityManager().Delete( ctid, ref statusMessage ) )
						DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
				case "conditionmanifest":
					if ( !new ConditionManifestManager().Delete( ctid, ref statusMessage ) )
					{
						DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					}
					break;
				case "costmanifest":
					if ( !new CostManifestManager().Delete( ctid, ref statusMessage ) )
						DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
				case "collection":
				case "ceterms:collection":
					if ( !new CollectionManager().Delete( ctid, ref statusMessage ) )
						DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
				case "competencyframework": //CompetencyFramework
                case "ceasn:competencyframework ": 
                    if ( !new CompetencyFrameworkManager().Delete( ctid, ref statusMessage ) )
						DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
				case "conceptscheme":
				case "skos:conceptscheme":
				case "progressionmodel":
				case "asn:progressionmodel":
					if ( !new ConceptSchemeManager().Delete( ctid, ref statusMessage ) )
						DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
				//
				case "datasetprofile":
				case "qdata:datasetprofile":
					if ( !new DataSetProfileManager().Delete( ctid, ref messages ) )
						DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
				//
				case "pathway":
                    if ( !new PathwayManager().Delete( ctid, ref statusMessage ) )
						DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
				case "pathwayset":
					if ( !new PathwaySetManager().Delete( ctid, ref statusMessage ) )
						DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
				case "transfervalueprofile":
				case "transfervalue":
					if ( !new TransferValueProfileManager().Delete( ctid, ref statusMessage ) )
						DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
				case "transferintermediary":
					if ( !new TransferIntermediaryManager().Delete( ctid, ref statusMessage ) )
						DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
				case "job":
					if ( !new JobManager().Delete( ctid, ref statusMessage ) )
						DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
				case "occupation":
					if ( !new OccupationManager().Delete( ctid, ref statusMessage ) )
						DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
				case "task":
					if ( !new TaskManager().Delete( ctid, ref statusMessage ) )
						DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
				case "workrole":
					if ( !new WorkRoleManager().Delete( ctid, ref statusMessage ) )
						DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
                case "scheduledoffering":
                    if ( !new ScheduledOfferingManager().Delete( ctid, ref messages ) )
                        DisplayMessages( string.Format( "  Delete failed: {0} ", string.Join( "", messages.ToArray() ) ) );
                    break;
                case "supportservice":
                    if ( !new SupportServiceManager().Delete( ctid, ref messages ) )
                        DisplayMessages( string.Format( "  Delete failed: {0} ", string.Join( "", messages.ToArray() ) ) );
                    break;
				case "rubric":
					if ( !new RubricManager().Delete( ctid, ref statusMessage ) )
						DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
				case "accreditaction":
				case "advancedstandingaction":
				case "approveaction":
				case "credentialingaction":
				case "offeraction":
				case "recognizeaction":
				case "regulateaction":
				case "renewaction":
				case "revokeaction":
				case "rightsaction":
				case "workforcedemandaction":
					var msg = $"Delete of Credential Actions ({ctdlType}) are not handled yet. This should be logged";
					DisplayMessages( msg );
					LoggingHelper.LogError( thisClassName + $".HandleDeleteRequest({ctdlType} / {ctid}): " + msg );
					break;
				default:
					//default to credential
					//DisplayMessages( string.Format( "{0}. Deleting Credential ({1}) by ctid: {2} ", cntr, ctdlType, ctid ) );
					if ( !new CredentialManager().Delete( ctid, ref statusMessage ) )
						DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
					break;
			}

			if ( statusMessage.Length > 0 )
				isValid = false;

			return isValid;
		}
		/// <summary>
		/// simple helper, retained for where methods called from console app.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		public static string DisplayMessages( string message )
		{
			LoggingHelper.DoTrace( 1, message );

			return message;
		}
	}
}

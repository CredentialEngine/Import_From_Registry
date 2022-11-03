using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elasticsearch.Net;

using workIT.Factories;
using workIT.Models;
using workIT.Models.Common;
using workIT.Utilities;

namespace workIT.Services
{
	public class ProfileServices
	{
		public static string thisClassName = "ProfileServices";
		public static List<TopLevelObject> ResolveToTopLevelObject( List<Guid> input, string property, ref SaveStatus status )
		{
			var list = new List<TopLevelObject>();
			foreach(var item in input )
			{
				var tlo = GetEntityAsTopLevelObject( item );
				if ( tlo != null && tlo.Id > 0 )
					list.Add( tlo );
				else
				{
					status.AddError( string.Format( "ProfileServicesError.ResolveToTopLevelObject: For property: '{0}' unable to resolve GUID: '{1}' to a top level object.", property, item.ToString() ) );
				}
			}
			//may be common to want the output sorted by entity type? If so do before returning

			return list;
		}
		public static TopLevelObject GetEntityAsTopLevelObject(Guid uid)
		{
			TopLevelObject tlo = new TopLevelObject();

			var entity = EntityManager.GetEntity( uid, false );
			if ( entity == null || entity.Id == 0 )
				return null;
			//
			if (entity.EntityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL)
			{
				//actually should return some type info
				tlo = CredentialManager.GetBasic( entity.EntityBaseId );
				tlo.EntityTypeId = entity.EntityTypeId;
			}
			else if ( entity.EntityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION )
			{
				tlo = OrganizationManager.GetBasics( entity.EntityUid );
				tlo.EntityTypeId = entity.EntityTypeId;
			}
			else if ( entity.EntityTypeId == CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE )
			{
				tlo = AssessmentManager.GetBasic( entity.EntityBaseId );
				tlo.EntityTypeId = entity.EntityTypeId;
			}
			else if ( entity.EntityTypeId == CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE )
			{
				tlo = LearningOpportunityManager.GetBasic( entity.EntityBaseId );
				tlo.EntityTypeId = entity.EntityTypeId;
			}
			else if ( entity.EntityTypeId == CodesManager.ENTITY_TYPE_PATHWAY )
			{
				tlo = PathwayManager.GetBasic( entity.EntityBaseId );
				tlo.EntityTypeId = entity.EntityTypeId;
			}
			else if ( entity.EntityTypeId == CodesManager.ENTITY_TYPE_PATHWAY_COMPONENT )
			{
				tlo = PathwayComponentManager.Get( entity.EntityBaseId );
				tlo.EntityTypeId = entity.EntityTypeId;
			}
			else if ( entity.EntityTypeId == CodesManager.ENTITY_TYPE_PATHWAY_SET )
			{
				tlo = PathwaySetManager.Get( entity.EntityBaseId, false );
				tlo.EntityTypeId = entity.EntityTypeId;
			}
			else if ( entity.EntityTypeId == CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE )
			{
				//these need to be light versions
				tlo = TransferValueProfileManager.Get( entity.EntityBaseId, false );
				tlo.EntityTypeId = entity.EntityTypeId;
			}
			else if ( entity.EntityTypeId == CodesManager.ENTITY_TYPE_OCCUPATIONS_PROFILE )
			{
				tlo = OccupationManager.GetBasic( entity.EntityBaseId );
				tlo.EntityTypeId = entity.EntityTypeId;
			}
			else if ( entity.EntityTypeId == CodesManager.ENTITY_TYPE_JOB_PROFILE )
			{
				tlo = JobManager.GetBasic( entity.EntityBaseId );
				tlo.EntityTypeId = entity.EntityTypeId;
			}
			return tlo;
		}


        #region addresses
        public static void HandleAddressGeoCoding()
		{
			//should we do all?
			int maxRecords = 0;
			LoggingHelper.DoTrace( 5, thisClassName + string.Format( ".HandleAddressGeoCoding - maxRecords: {0}", maxRecords ) );
			DateTime started = DateTime.Now;
			string report = "";
			string messages = "";
			var list = new Entity_AddressManager().ResolveMissingGeodata( ref messages, maxRecords );

			var saveDuration = DateTime.Now.Subtract( started );
			LoggingHelper.DoTrace( 5, thisClassName + string.Format( ".NormalizeAddresses - Completed - seconds: {0}", saveDuration.Seconds ) );
			if ( !string.IsNullOrWhiteSpace( messages ) )
				report = string.Format( "<p>Normalize Addresses. Duration: {0} seconds <br/>", saveDuration.Seconds ) + messages + "</p>";

			foreach ( var address in list )
			{
				string msg = string.Format( " - Unable to resolve address: Id: {0}, address1: {1}, city: {2}, region: {3}, postalCode: {4}, country: {5} ", address.Id, address.StreetAddress, address.AddressLocality, address.AddressRegion, address.PostalCode, address.AddressCountry );
				LoggingHelper.DoTrace( 2, msg );
				report += System.Environment.NewLine + msg;
			}
			//no reporting of successes here 

		}
		public void NormalizeAddressesExternal( string authorization, int maxRecords, ref string message )
		{
			LoggingHelper.DoTrace( 5, thisClassName + string.Format( ".NormalizeAddressesExternal - starting maxRecords: {0}", maxRecords ) );
			//do validation
			if ( ( authorization ?? "" ).ToLower() != "bca5a70f-cf0d-4b27-8566-9f874d88741e" )
			{
				message= "You are not authorized to invoke NormalizeAddressesExternal.";
				return;
			}
			//may be OK from API
			if ( UtilityManager.GetAppKeyValue( "environment" ) == "development" )
			{
				//message="Sorry the NormalizeAddresses process is not available in the development environment (not allowed by Google)." ;
				//return;
			}

			int addressesFixed = 0;
			int addressRemaining = 0;
			DateTime started = DateTime.Now;
			
			string messages = "";
			List<Address> list = new Entity_AddressManager().ResolveMissingGeodata( ref messages, ref addressesFixed, ref addressRemaining, maxRecords );

			var saveDuration = DateTime.Now.Subtract( started );
			LoggingHelper.DoTrace( 5, thisClassName + string.Format( ".NormalizeAddressesExternal - Completed - seconds: {0}", saveDuration.Seconds ) );
			int addressesNotFixed = list.Count();
			message = string.Format( "Normalize Addresses. Duration: {0} seconds, Addresses Fixed: {1}, Not Fixed: {2} Remaining: {3}", saveDuration.Seconds, addressesFixed, addressesNotFixed, addressRemaining ) ;
			
			//where called externally, don't need details, just count. Could be used in a loop.
			//foreach ( var address in list )
			//{
			//	if ( !string.IsNullOrWhiteSpace( address.StreetAddress ) )
			//	{
			//		string msg = string.Format( " - Unable to resolve address: Id: {0}, address1: {1}, city: {2}, region: {3}, postalCode: {4}, country: {5} ", address.Id, address.StreetAddress, address.AddressLocality, address.AddressRegion, address.PostalCode, address.AddressCountry );
			//		LoggingHelper.DoTrace( 2, msg );
			//		report += System.Environment.NewLine + msg;
			//	}
			//}
		}

		#endregion

		//public bool HandleDeleteRequest( int cntr, string ctid, string ctdlType, ref string statusMessage )
		//{
		//	statusMessage = "";
		//	List<string> messages = new List<string>();

		//	bool isValid = true;
		//	DisplayMessages( string.Format( "{0}. Deleting {1} by ctid: {2} ", cntr, ctdlType, ctid ) );

		//	switch ( ctdlType.ToLower() )
		//	{
		//		case "credentialorganization":
		//		case "qacredentialorganization":
		//		case "organization":
		//			if ( !new OrganizationManager().Delete( ctid, ref statusMessage ) )
		//				DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
		//			break;

		//		case "assessmentprofile":
		//		case "assessment":
		//			if ( !new AssessmentManager().Delete( ctid, ref statusMessage ) )
		//				DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
		//			break;
		//		case "learningopportunityprofile":
		//		case "learningopportunity":
		//		case "learningprogram":
		//		case "course":
		//			if ( !new LearningOpportunityManager().Delete( ctid, ref statusMessage ) )
		//				DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
		//			break;
		//		case "conditionmanifest":
		//			if ( !new ConditionManifestManager().Delete( ctid, ref statusMessage ) )
		//			{
		//				DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
		//			}
		//			break;
		//		case "costmanifest":
		//			if ( !new CostManifestManager().Delete( ctid, ref statusMessage ) )
		//				DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
		//			break;
		//		case "collection":
		//		case "ceterms:collection":
		//			if ( !new CollectionManager().Delete( ctid, ref statusMessage ) )
		//				DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
		//			break;
		//		case "competencyframework": //CompetencyFramework
		//			if ( !new CompetencyFrameworkManager().Delete( ctid, ref statusMessage ) )
		//				DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
		//			break;
		//		case "conceptscheme":
		//		case "skos:conceptscheme":
		//			if ( !new ConceptSchemeManager().Delete( ctid, ref statusMessage ) )
		//				DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
		//			break;
		//		//
		//		case "datasetprofile":
		//		case "qdata:datasetprofile":
		//			if ( !new DataSetProfileManager().Delete( ctid, ref messages ) )
		//				DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
		//			break;
		//		//
		//		case "pathway":
		//			if ( !new PathwayManager().Delete( ctid, ref statusMessage ) )
		//				DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
		//			break;
		//		case "pathwayset":
		//			if ( !new PathwaySetManager().Delete( ctid, ref statusMessage ) )
		//				DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
		//			break;
		//		case "transfervalueprofile":
		//			if ( !new TransferValueProfileManager().Delete( ctid, ref statusMessage ) )
		//				DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
		//			break;
		//		case "transferintermediary":
		//			if ( !new TransferIntermediaryManager().Delete( ctid, ref statusMessage ) )
		//				DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
		//			break;
		//		case "job":
		//			if ( !new JobManager().Delete( ctid, ref statusMessage ) )
		//				DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
		//			break;
		//		case "occupation":
		//			if ( !new OccupationManager().Delete( ctid, ref statusMessage ) )
		//				DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
		//			break;
		//		case "task":
		//			if ( !new TaskManager().Delete( ctid, ref statusMessage ) )
		//				DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
		//			break;
		//		case "workrole":
		//			if ( !new WorkRoleManager().Delete( ctid, ref statusMessage ) )
		//				DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
		//			break;
		//		default:
		//			//default to credential
		//			//DisplayMessages( string.Format( "{0}. Deleting Credential ({1}) by ctid: {2} ", cntr, ctdlType, ctid ) );
		//			if ( !new CredentialManager().Delete( ctid, ref statusMessage ) )
		//				DisplayMessages( string.Format( "  Delete failed: {0} ", statusMessage ) );
		//			break;
		//	}

		//	if ( statusMessage.Length > 0 )
		//		isValid = false;

		//	return isValid;
		//}
		///// <summary>
		///// simple helper, retained for where methods called from console app.
		///// </summary>
		///// <param name="message"></param>
		///// <returns></returns>
		//public static string DisplayMessages( string message )
		//{
		//	LoggingHelper.DoTrace( 1, message );

		//	return message;
		//}
	}
}

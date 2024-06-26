using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elasticsearch.Net;
using Newtonsoft.Json;
using APIServices = workIT.Services.API;
using workIT.Factories;
using workIT.Models;
using workIT.Models.Common;
using workIT.Utilities;
//using System.Web.UI.WebControls;

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

		/// <summary>
		/// Get any top level resource with a CTID as type TopLevelResource.
		/// TODO - there is a lot of chafe with this. 
		/// </summary>
		/// <param name="uid"></param>
		/// <returns></returns>
		public static TopLevelObject GetEntityAsTopLevelObject( Guid uid )
		{
			TopLevelObject tlo = new TopLevelObject();

			//
			//TODO - make this lighter
			//	can entity cache be used?
			var resource = EntityManager.EntityCacheGetByGuid( uid );
			if ( resource != null && resource.Id > 0)
			{
				tlo = new TopLevelObject()
				{
					EntityType = resource.EntityType,	
					EntityTypeId = resource.EntityTypeId,
					Id = resource.BaseId,
					Name = resource.Name,
					Description = resource.Description,
					CTID = resource.CTID,
					EntityLastUpdated = resource.LastUpdated,
					EntityStateId = resource.EntityStateId,
				};
				return tlo;
			}


			var entity = EntityManager.GetEntity( uid, false );
			if ( entity == null || entity.Id == 0 )
				return null;
			switch ( entity.EntityTypeId )
			{
				case 1:
					//actually should return some type info
					tlo = CredentialManager.GetBasic( entity.EntityBaseId );
					tlo.EntityTypeId = entity.EntityTypeId;
					break;
				case 2:
				case 12:
				case 13:
					tlo = OrganizationManager.GetBasics( entity.EntityUid );
					tlo.EntityTypeId = entity.EntityTypeId;
					break;
				case 3:
					tlo = AssessmentManager.GetBasic( entity.EntityBaseId );
					tlo.EntityTypeId = entity.EntityTypeId;
					break;
				case 7:
				case 36:
				case 37:
					tlo = LearningOpportunityManager.GetBasic( entity.EntityBaseId );
					tlo.EntityTypeId = entity.EntityTypeId;
					break;
				case 8:
					tlo = PathwayManager.GetBasic( entity.EntityBaseId );
					tlo.EntityTypeId = entity.EntityTypeId;
					break;
				case 9:
					tlo = CollectionManager.Get( entity.EntityBaseId, false );
					tlo.EntityTypeId = entity.EntityTypeId;
					break;
				case 10:
					tlo = CompetencyFrameworkManager.Get( entity.EntityBaseId );
					tlo.EntityTypeId = entity.EntityTypeId;
					break;
				case 15:
					tlo = ScheduledOfferingManager.GetBasic( entity.EntityBaseId );
					tlo.EntityTypeId = CodesManager.ENTITY_TYPE_SCHEDULED_OFFERING;
					break;
				case 22:
					tlo = CredentialingActionManager.GetBasic( entity.EntityBaseId);
					tlo.EntityTypeId = entity.EntityTypeId;
					break;
				case 23:
					tlo = PathwaySetManager.Get( entity.EntityBaseId, false );
					tlo.EntityTypeId = entity.EntityTypeId;
					break;
				case 24:
					tlo = PathwayComponentManager.Get( entity.EntityBaseId );
					tlo.EntityTypeId = entity.EntityTypeId;
					break;
				
				case 26:
					//these need to be light versions
					tlo = TransferValueProfileManager.Get( entity.EntityBaseId, false );
					tlo.EntityTypeId = entity.EntityTypeId;
					break;
				case 28:
					tlo = TransferIntermediaryManager.Get( entity.EntityBaseId, false );
					tlo.EntityTypeId = entity.EntityTypeId;
					break;


				case 31:
					tlo = DataSetProfileManager.Get( entity.EntityBaseId, false );
					tlo.EntityTypeId = entity.EntityTypeId;
					break;
				case 32:
					tlo = JobManager.GetBasic( entity.EntityBaseId );
					tlo.EntityTypeId = entity.EntityTypeId;
					break;
				case 33:
					tlo = TaskManager.GetBasic( entity.EntityBaseId );
					tlo.EntityTypeId = entity.EntityTypeId;
					break;
				case 34:
					tlo = WorkRoleManager.GetBasic( entity.EntityBaseId );
					tlo.EntityTypeId = entity.EntityTypeId;
					break;
				case 35:
					tlo = OccupationManager.GetBasic( entity.EntityBaseId );
					tlo.EntityTypeId = entity.EntityTypeId;
					break;
				case 38:
					tlo = SupportServiceManager.GetBasic( entity.EntityBaseId );
					tlo.EntityTypeId = CodesManager.ENTITY_TYPE_SUPPORT_SERVICE;
					break;
				case 39:
					tlo = RubricManager.GetBasic( entity.EntityBaseId );
					tlo.EntityTypeId = CodesManager.ENTITY_TYPE_RUBRIC;
					break;
			} 
					
			

			return tlo;
		}

		#region Helper for indexing reference objects
		/// <summary>
		/// Method to prepare and index reference resources 
		/// </summary>
		/// <param name="list"></param>
		/// <param name="status"></param>
		public void IndexPrepForReferenceResource( List<TopLevelObject> list, ref SaveStatus status )
		{
			if ( list == null || !list.Any() )
				return;

			var eManager = new EntityManager();
			foreach ( var item in list )
			{
				//NOTE need to build the entity.cache.ResourceDetail before updating the index pending request
				var reindexId = item.EntityTypeId;

				var statusMsg = "";
				var messages = new List<string>();
				//do we need a check for CTID? That is, if present skip?
				switch ( item.EntityTypeId )
				{
					case 1:
						var cred = APIServices.CredentialServices.GetDetailForAPI( item.Id, true );
						if ( cred != null && cred.Meta_Id > 0 )
						{
							var resourceDetail = JsonConvert.SerializeObject( cred, JsonHelper.GetJsonSettings( false ) );
							if ( eManager.EntityCacheUpdateResourceDetail( item.RowId, resourceDetail, ref statusMsg ) == 0 )
							{
								status.AddError( $"{thisClassName}.IndexPrepForReferenceResource-EntityCacheUpdateResourceDetail for: {item.EntityTypeId} failed:" + statusMsg );
							}
							if ( eManager.EntityCacheUpdateAgentRelationshipsForCredential( cred.Meta_RowId.ToString(), ref statusMsg ) == false )
							{
								status.AddError( $"{thisClassName}.IndexPrepForReferenceResource-EntityCacheUpdateAgentRelationshipsForCredential for: {item.EntityTypeId} failed:" + statusMsg );
							}
						}
						//new SearchPendingReindexManager().Add( item.EntityTypeId, item.Id, 1, ref messages );
						////what about owner? would have to ensure that it is present.
						//if ( item.PrimaryOrganizationId > 0 )
						//	new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, item.PrimaryOrganizationId, 1, ref messages );
						break;
					case 2:
						var org = APIServices.OrganizationServices.GetDetailForAPI( item.Id, true );
						if ( org != null && org.Meta_Id > 0 )
						{
							var resourceDetail = JsonConvert.SerializeObject( org, JsonHelper.GetJsonSettings( false ) );
							if ( eManager.EntityCacheUpdateResourceDetail( item.RowId, resourceDetail, ref statusMsg ) == 0 )
							{
								status.AddError( $"{thisClassName}.IndexPrepForReferenceResource-EntityCacheUpdateResourceDetail for: {item.EntityTypeId} failed:" + statusMsg );
							}
						}
						//new SearchPendingReindexManager().Add( item.EntityTypeId, item.Id, 1, ref messages );
						//others?????
						break;
					case 3:
						var asmt = APIServices.AssessmentServices.GetDetailForAPI( item.Id, true );
						if ( asmt != null && asmt.Meta_Id > 0 )
						{
							var resourceDetail = JsonConvert.SerializeObject( asmt, JsonHelper.GetJsonSettings( false ) );
							if ( eManager.EntityCacheUpdateResourceDetail( item.RowId, resourceDetail, ref statusMsg ) == 0 )
							{
								status.AddError( $"{thisClassName}.IndexPrepForReferenceResource-EntityCacheUpdateResourceDetail for: {item.EntityTypeId} failed:" + statusMsg );
							}
							if ( eManager.EntityCacheUpdateAgentRelationshipsForAssessment( asmt.Meta_RowId.ToString(), ref statusMsg ) == false )
							{
								status.AddError( $"{thisClassName}.IndexPrepForReferenceResource-EntityCacheUpdateAgentRelationshipsForAssessment for: {item.EntityTypeId} failed:" + statusMsg );
							}
						}
						//new SearchPendingReindexManager().Add( item.EntityTypeId, item.Id, 1, ref messages );
						//if ( item.PrimaryOrganizationId > 0 )
						//	new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, item.PrimaryOrganizationId, 1, ref messages );
						break;
					case 7:
					case 36:
					case 37:
						reindexId = 7;
						var lopp = APIServices.LearningOpportunityServices.GetDetailForAPI( item.Id, true );
						if ( lopp != null && lopp.Meta_Id > 0 )
						{
							var resourceDetail = JsonConvert.SerializeObject( lopp, JsonHelper.GetJsonSettings( false ) );
							if ( eManager.EntityCacheUpdateResourceDetail( item.RowId, resourceDetail, ref statusMsg ) == 0 )
							{
								status.AddError( $"{thisClassName}.IndexPrepForReferenceResource-EntityCacheUpdateResourceDetail for: {item.EntityTypeId} failed:" + statusMsg );
							}
							if ( eManager.EntityCacheUpdateAgentRelationshipsForLopp( lopp.Meta_RowId.ToString(), ref statusMsg ) == false )
							{
								status.AddError( $"{thisClassName}.IndexPrepForReferenceResource-EntityCacheUpdateAgentRelationshipsForLopp for: {item.EntityTypeId} failed:" + statusMsg );
							}
						}
						//new SearchPendingReindexManager().Add( 7, item.Id, 1, ref messages );
						//if ( item.PrimaryOrganizationId > 0 )
						//	new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, item.PrimaryOrganizationId, 1, ref messages );

						
						break;

					case 35:
						var occupation = APIServices.OccupationServices.GetDetailForAPI( item.Id, true );
						if ( occupation != null && occupation.Meta_Id > 0 )
						{
							var resourceDetail = JsonConvert.SerializeObject( occupation, JsonHelper.GetJsonSettings( false ) );
							if ( eManager.EntityCacheUpdateResourceDetail( item.RowId, resourceDetail, ref statusMsg ) == 0 )
							{
								status.AddError( $"{thisClassName}.IndexPrepForReferenceResource-EntityCacheUpdateResourceDetail for: {item.EntityTypeId} failed:" + statusMsg );
							}
							
							//new SearchPendingReindexManager().Add( item.EntityTypeId, item.Id, 1, ref messages );
							//if ( item.PrimaryOrganizationId > 0 )
							//	new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, item.PrimaryOrganizationId, 1, ref messages );

						}
						break;
					default:
						status.AddError( $"{thisClassName}.IndexPrepForReferenceResource Unhandled EntityTypeId encountered: {item.EntityTypeId}." );
						break;
				}

				
				new SearchPendingReindexManager().Add( reindexId, item.Id, 1, ref messages );
				//what about owner? would have to ensure that it is present.
				//just in case exclude entitytypeId of 2
				if ( reindexId !=2 && item.PrimaryOrganizationId > 0 )
					new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, item.PrimaryOrganizationId, 1, ref messages );
			}

		}
		public void IndexPrepForReferenceResource (int entityTypeId)
		{


		}
		#endregion
		#region addresses
		/// <summary>
		/// Handle addresses missing lat/lng
		///  NEW: may need to reindex the parent resource (should be handled if from import)
		/// </summary>
		public static void HandleAddressGeoCoding()
		{
			//should we do all?
			int maxRecords = 0;
			LoggingHelper.DoTrace( CodesManager.appMethodEntryTraceLevel, thisClassName + string.Format( ".HandleAddressGeoCoding - maxRecords: {0}", maxRecords ) );
			DateTime started = DateTime.Now;
			string report = "";
			string messages = "";
			var failedList = new Entity_AddressManager().ResolveMissingGeodata( ref messages, maxRecords );

			var saveDuration = DateTime.Now.Subtract( started );
			LoggingHelper.DoTrace( 5, thisClassName + string.Format( ".NormalizeAddresses - Completed - seconds: {0}", saveDuration.Seconds ) );
			if ( !string.IsNullOrWhiteSpace( messages ) )
				report = string.Format( "<p>Normalize Addresses. Duration: {0} seconds <br/>", saveDuration.Seconds ) + messages + "</p>";

			foreach ( var address in failedList )
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
			var failedList = new Entity_AddressManager().ResolveMissingGeodata( ref messages, ref addressesFixed, ref addressRemaining, maxRecords );

			var saveDuration = DateTime.Now.Subtract( started );
			LoggingHelper.DoTrace( 5, thisClassName + string.Format( ".NormalizeAddressesExternal - Completed - seconds: {0}", saveDuration.Seconds ) );
			int addressesNotFixed = failedList.Count();
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

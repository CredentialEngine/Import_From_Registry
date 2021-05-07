using System;
using System.Collections.Generic;
using System.Web;

using workIT.Services;
using workIT.Models;
using workIT.Models.Common;
using workIT.Factories;
using workIT.Utilities;
using EM = workIT.Data.Tables;

namespace Import.Services
{
	public class ResolutionServices
	{

		public static Guid ResolveEntityByRegistryAtIdToGuid( string property, string referencedAtId, int entityTypeId, ref SaveStatus status, ref bool isResolved )
		{
			Guid entityUid = new Guid();
			string ctid = "";
			int newEntityId = 0;
			if ( string.IsNullOrWhiteSpace( referencedAtId ) )
				return entityUid;

			List<string> messages = new List<string>();
			//test direct, and fall back to by ctid??
			//should only handle Uri's for now
			if ( referencedAtId.Trim().ToLower().IndexOf( "http" ) == 0 )
			{
				//should probably ensure a registry url
				ctid = ExtractCtid( referencedAtId.Trim() );
				if ( ctid == "ce-fa6c139f-0615-401f-9920-6ec8c445baca" )
				{

				}
				EM.Import_EntityResolution item = ImportManager.Import_EntityResolution_GetById( referencedAtId );

				if ( item != null && item.Id > 0 )
				{
					isResolved = item.IsResolved != null ? ( bool ) item.IsResolved : false;
					//need to make sure valid
					//actually should always be valid
					//if ( BaseFactory.IsGuidValid( item.EntityUid ) )
					//20-07-30 mparsons - why is EntityUid returned here?
					//check this
					return ( Guid )item.EntityUid;
				}
				else
				{
					if ( IsCtidValid( ctid, ref messages ) )
					{
						item = ImportManager.Import_EntityResolution_GetByCtid( ctid );
						if ( item != null && item.Id > 0 )
						{
							isResolved = item.IsResolved != null ? ( bool ) item.IsResolved : false;
							return ( Guid ) item.EntityUid;
						}
					}
				}

			}
			else
			{
				ctid = ExtractCtid( referencedAtId.Trim() );
				if ( IsCtidValid( ctid, ref messages ) )
				{
					EM.Import_EntityResolution item2 = ImportManager.Import_EntityResolution_GetByCtid( ctid );
					if ( item2 != null && item2.Id > 0 )
					{
						isResolved = item2.IsResolved != null ? ( bool ) item2.IsResolved : false;
						return ( Guid ) item2.EntityUid;
					}
				}

			}
			//add an import entry
			ImportManager importManager = new ImportManager();
			entityUid = Guid.NewGuid();
			string statusMsg = "";
			if ( entityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL )
			{
				newEntityId = new CredentialManager().AddPendingRecord( entityUid, ctid, referencedAtId, ref statusMsg );
				if ( newEntityId == 0 )
				{
					//need to log, and reset 
					status.AddError( "Credential Add Pending failed for: " + property + ". " + statusMsg );
					entityUid = new Guid();
				}
			}
			else if ( entityTypeId == CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE )
			{
				newEntityId = new AssessmentManager().AddPendingRecord( entityUid, ctid, referencedAtId, ref statusMsg );
				if ( newEntityId == 0 )
				{
					//need to log, and reset 
					status.AddError( "Assessment Add Pending failed for: " + property + ". " + statusMsg );
					entityUid = new Guid();
				}
			}
			else if ( entityTypeId == CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE )
			{
				newEntityId = new LearningOpportunityManager().AddPendingRecord( entityUid, ctid, referencedAtId, ref statusMsg );
				if ( newEntityId == 0 )
				{
					//need to log, and reset 
					status.AddError( "Learning Opportunity Add Pending failed for: " + property + ". " + statusMsg );
					entityUid = new Guid();
				}
			}
			else if ( entityTypeId == CodesManager.ENTITY_TYPE_COST_MANIFEST )
			{
				//should know the parent org, add to this method
				newEntityId = new CostManifestManager().AddPendingRecord( entityUid, ctid, referencedAtId, ref statusMsg );
				if ( newEntityId == 0 )
				{
					//need to log, and reset 
					status.AddError( "CostManifest Add Pending failed for: " + property + ". " + statusMsg );
					entityUid = new Guid();
				}
			}
			else if ( entityTypeId == CodesManager.ENTITY_TYPE_CONDITION_MANIFEST )
			{
				newEntityId = new ConditionManifestManager().AddPendingRecord( entityUid, ctid, referencedAtId, ref statusMsg );
				if ( newEntityId == 0 )
				{
					//need to log, and reset 
					status.AddError( "ConditionManifest Add Pending failed for: " + property + ". " + statusMsg );
					entityUid = new Guid();
				}
			}
			else if ( entityTypeId == CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE )
			{
				//not sure we will ever have a reference to a TVP?
				newEntityId = new TransferValueProfileManager().AddPendingRecord( entityUid, ctid, referencedAtId, ref statusMsg );
				if ( newEntityId == 0 )
				{
					//need to log, and reset 
					status.AddError( "TransferValue Add Pending failed for: " + property + ". " + statusMsg );
					entityUid = new Guid();
				}
			}
			else if ( entityTypeId == CodesManager.ENTITY_TYPE_PATHWAY )
			{
				newEntityId = new PathwayManager().AddPendingRecord( entityUid, ctid, referencedAtId, ref statusMsg );
				if ( newEntityId == 0 )
				{
					//need to log, and reset 
					status.AddError( "Pathway Add Pending failed for: " + property + ". " + statusMsg );
					entityUid = new Guid();
				}
			}
			else if ( entityTypeId == CodesManager.ENTITY_TYPE_PATHWAY_COMPONENT )
			{
				if ( ctid == "ce-fa6c139f-0615-401f-9920-6ec8c445baca" )
				{

				}
				//need pathwayCTID for this ce-abcb5fe0-8fde-4f06-9d70-860cd5bdc763
				newEntityId = new PathwayComponentManager().AddPendingRecord( entityUid, ctid, referencedAtId, ref statusMsg );
				if ( newEntityId == 0 )
				{
					//need to log, and reset 
					status.AddError( statusMsg );
					//need to know what property would need to be fixed - really shouldn't happen
					entityUid = new Guid();
				}
			}
			else
			{
				//for properties like organization.Offers, we don't know what the entity type is.
				//SO.....
			}

			if ( BaseFactory.IsGuidValid( entityUid) )
			{
				int id = importManager.Import_EntityResolutionAdd( referencedAtId,
						ctid,
						entityTypeId,
						entityUid,
						newEntityId,
						false,
						ref messages );
				//need to create a placeholder
				if ( id == 0 )
				{
					status.AddError( "Error - failed to add Import_EntityResolution for " + referencedAtId );
					status.AddWarningRange( messages );
					//return 0;
				}
			}


			return entityUid;
		}
		public static int ResolveEntityByRegistryAtId( string referencedAtId, int entityTypeId, ref SaveStatus status, ref bool isResolved )
		{
			Guid entityUid = Guid.NewGuid();
			int newEntityId = 0;
			string ctid = "";
			List<string> messages = new List<string>();
			//test direct, and fall back to by ctid??
			//should only handle Uri's for now
			if ( referencedAtId.Trim().ToLower().IndexOf( "http" ) == 0 )
			{
				//should probably ensure a registry url
				ctid = ExtractCtid( referencedAtId.Trim() );
				LoggingHelper.DoTrace( 7, string.Format( "ResolutionServices.ResolveEntityByRegistryAtId: EntityTypeId: {0}, referencedAtId: {1} ", entityTypeId, referencedAtId ) );

				EM.Import_EntityResolution item = ImportManager.Import_EntityResolution_GetById( referencedAtId );

				if ( item != null && item.Id > 0 && (item.EntityBaseId ?? 0) > 0 )
				{
					isResolved = item.IsResolved != null ? ( bool ) item.IsResolved : false;
					//need to make sure valid
					//actually should always be valid
					//if ( BaseFactory.IsGuidValid( item.EntityUid ) )
					return (int)item.EntityBaseId;
				}
				else
				{
					LoggingHelper.DoTrace( 6, string.Format( "ResolutionServices. **FAILED** ResolveEntityByRegistryAtId: EntityTypeId: {0}, target.CtdlId: {1}. Trying with CTID: {2}", entityTypeId, referencedAtId, ctid ) );
					if ( IsCtidValid( ctid, ref messages ) )
					{
						item = ImportManager.Import_EntityResolution_GetByCtid( ctid );
						if ( item != null && item.Id > 0 && ( item.EntityBaseId ?? 0 ) > 0 )
						{
							isResolved = item.IsResolved != null ? ( bool ) item.IsResolved : false;
							return ( int ) item.EntityBaseId;
						}
					}
				}

			}
			else
			{
				
				ctid = ExtractCtid( referencedAtId.Trim() );
				LoggingHelper.DoTrace( 7, string.Format( "ResolutionServices.ResolveEntityByRegistryAtId. referencedAtId appears to be a ctid EntityTypeId: {0}, referencedAtId: {1}, ctid: {2} ", entityTypeId, referencedAtId, ctid ) );
				if ( IsCtidValid( ctid, ref messages ) )
				{
					EM.Import_EntityResolution item2 = ImportManager.Import_EntityResolution_GetByCtid( ctid );
					if ( item2 != null && item2.Id > 0 && ( item2.EntityBaseId ?? 0 ) > 0 )
					{
						isResolved = item2.IsResolved != null ? ( bool ) item2.IsResolved : false;
						return ( int ) item2.EntityBaseId;
					} else
					{
						LoggingHelper.DoTrace( 5, string.Format( "ResolutionServices.ResolveEntityByRegistryAtId. DID NOT RESOLVE VIA CTID referencedAtId appears to be a ctid EntityTypeId: {0}, ctid: {2} ", entityTypeId, referencedAtId, ctid ) );
					}
				}

			}
			//add an import entry - need to do the base first
			ImportManager importManager = new ImportManager();
			
			string statusMsg = "";
			if ( entityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL )
			{
				newEntityId = new CredentialManager().AddPendingRecord( entityUid, ctid, referencedAtId, ref statusMsg );
				if ( newEntityId == 0 )
				{
					//need to log, and reset 
					status.AddError( statusMsg );
					//need to know what property would need to be fixed - really shouldn't happen
					entityUid = new Guid();
				}
			}
			else if ( entityTypeId == CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE )
			{
				newEntityId = new AssessmentManager().AddPendingRecord( entityUid, ctid, referencedAtId, ref statusMsg );
				if ( newEntityId == 0 )
				{
					//need to log, and reset 
					status.AddError( statusMsg );
					//need to know what property would need to be fixed - really shouldn't happen
					entityUid = new Guid();
				}
			}
			else if ( entityTypeId == CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE )
			{
				newEntityId = new LearningOpportunityManager().AddPendingRecord( entityUid, ctid, referencedAtId, ref statusMsg );
				if ( newEntityId == 0 )
				{
					//need to log, and reset 
					status.AddError( statusMsg );
					//need to know what property would need to be fixed - really shouldn't happen
					entityUid = new Guid();
				}
			}
			else if ( entityTypeId == CodesManager.ENTITY_TYPE_COST_MANIFEST )
			{
				newEntityId = new CostManifestManager().AddPendingRecord( entityUid, ctid, referencedAtId, ref statusMsg );
				if ( newEntityId == 0 )
				{
					//need to log, and reset 
					status.AddError( statusMsg );
					//need to know what property would need to be fixed - really shouldn't happen
					entityUid = new Guid();
				}
			}
			else if ( entityTypeId == CodesManager.ENTITY_TYPE_CONDITION_MANIFEST )
			{
				newEntityId = new ConditionManifestManager().AddPendingRecord( entityUid, ctid, referencedAtId, ref statusMsg );
				if ( newEntityId == 0 )
				{
					//need to log, and reset 
					status.AddError( statusMsg );
					//need to know what property would need to be fixed - really shouldn't happen
					entityUid = new Guid();
				}
			}
			else if ( entityTypeId == CodesManager.ENTITY_TYPE_PATHWAY )
			{
				newEntityId = new PathwayManager().AddPendingRecord( entityUid, ctid, referencedAtId, ref statusMsg );
				if ( newEntityId == 0 )
				{
					//need to log, and reset 
					status.AddError( statusMsg );
					//need to know what property would need to be fixed - really shouldn't happen
					entityUid = new Guid();
				}
			}
			else if ( entityTypeId == CodesManager.ENTITY_TYPE_PATHWAY_COMPONENT )
			{
				newEntityId = new PathwayComponentManager().AddPendingRecord( entityUid, ctid, referencedAtId, ref statusMsg );
				if ( newEntityId == 0 )
				{
					//need to log, and reset 
					status.AddError( statusMsg );
					//need to know what property would need to be fixed - really shouldn't happen
					entityUid = new Guid();
				}
			}
			else if ( entityTypeId == CodesManager.ENTITY_TYPE_COMPETENCY_FRAMEWORK )
			{
				//actually should not happen - confirm the cf must exist or will be rejected by API
				newEntityId = new CompetencyFrameworkManager().AddPendingRecord( entityUid, ctid, referencedAtId, ref statusMsg );
				if ( newEntityId == 0 )
				{
					//need to log, and reset 
					status.AddError( statusMsg );
					//need to know what property would need to be fixed - really shouldn't happen
					entityUid = new Guid();
				}
			}
			else if ( entityTypeId == CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE )
			{
				//actually should not happen - confirm the tvp must exist or will be rejected by API
				newEntityId = new TransferValueProfileManager().AddPendingRecord( entityUid, ctid, referencedAtId, ref statusMsg );
				if ( newEntityId == 0 )
				{
					//need to log, and reset 
					status.AddError( statusMsg );
					//need to know what property would need to be fixed - really shouldn't happen
					entityUid = new Guid();
				}
			}
			//
			if ( newEntityId  > 0)
			{
				int id = importManager.Import_EntityResolutionAdd( referencedAtId,
						ctid,
						entityTypeId,
						entityUid,
						newEntityId,
						false,
						ref messages, true );
				//need to create a placeholder
				if ( id == 0 )
				{
					status.AddError( "Error - failed to add Import_EntityResolution for " + referencedAtId );
					status.AddWarningRange( messages );
					//this may be wiping out newEntityId
					//return 0;
				}
			}

			return newEntityId;
		}

		/// <summary>
		/// Attempt to resolve an organization reference by either a registry URI or CTID
		/// </summary>
		/// <param name="referencedAtId">Registry URI or CTID</param>
		/// <param name="status"></param>
		/// <param name="isResolved"></param>
		/// <returns></returns>
		public static Guid ResolveOrgByRegistryAtId( string referencedAtId, ref SaveStatus status, ref bool isResolved )
		{
			Guid entityUid = new Guid();
			string ctid = "";
			List<string> messages = new List<string>();
			//test direct, and fall back to by ctid??
			//should only handle Uri's for now
			if ( referencedAtId.Trim().ToLower().IndexOf( "http" ) == 0 )
			{
				//should probably ensure a registry url
				ctid = ExtractCtid( referencedAtId.Trim() );

				EM.Import_EntityResolution item = ImportManager.Import_EntityResolution_GetById( referencedAtId );

				if ( item != null && item.Id > 0 )
				{
					isResolved = item.IsResolved != null ? ( bool ) item.IsResolved : false;
					//need to make sure valid
					//actually should always be valid
					//if ( BaseFactory.IsGuidValid( item.EntityUid ) )
						return ( Guid ) item.EntityUid;

					//add activity or error
					//return entityUid;
				}
				else
				{
					if ( IsCtidValid( ctid, ref messages ) )
					{
						item = ImportManager.Import_EntityResolution_GetByCtid( ctid );
						if ( item != null && item.Id > 0 )
						{
							isResolved = item.IsResolved != null ? ( bool ) item.IsResolved : false;
							return ( Guid ) item.EntityUid;
						}
					}
				}
					
			} else
			{
				ctid = ExtractCtid( referencedAtId.Trim() );
				if ( IsCtidValid( ctid, ref messages ) )
				{
					EM.Import_EntityResolution item2 = ImportManager.Import_EntityResolution_GetByCtid( ctid );
					if ( item2 != null && item2.Id > 0 )
					{
						isResolved = item2.IsResolved != null ? ( bool ) item2.IsResolved : false;
						return ( Guid ) item2.EntityUid;
					}
				}

			}
			//add an import entry
			ImportManager importManager = new ImportManager();
			entityUid = Guid.NewGuid();
			
			string statusMsg = "";
			int orgId = new OrganizationManager().AddPendingRecord( entityUid, ctid, referencedAtId, ref statusMsg );
			if ( orgId  == 0)
			{
				//need to log, and reset 
				status.AddError( statusMsg );
				//need to know what property would need to be fixed - really shouldn't happen
				entityUid = new Guid();
			} else
			{
				int id = importManager.Import_EntityResolutionAdd( referencedAtId,
						ctid,
						CodesManager.ENTITY_TYPE_ORGANIZATION,
						entityUid,
						orgId,
						false,
						ref messages );
				//need to create a placeholder
				if ( id == 0 )
				{
					status.AddError( "Error - failed to add Import_EntityResolution for " + referencedAtId );
					status.AddWarningRange( messages );
					return new Guid();
				}
			}

			return entityUid;
		}

		/// <summary>
		/// May want to make more generic, to reduce methods
		/// in some cases, may just want the Guid
		/// </summary>
		/// <param name="registryId"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public static Organization ResolveAgentByRegistryId(string registryId, ref List<string> messages)
		{
			if ( string.IsNullOrWhiteSpace( registryId ) )
				return null;
			if ( registryId.Trim().ToLower().IndexOf( "http" ) == 0 )
			{
				string ctid = ExtractCtid( registryId.Trim() );
				//could validate result
				if (IsCtidValid(ctid, ref messages))
					return ResolveAgentByCtid( ctid );
			}
			//else ?

			return null;
		}

		public static Organization ResolveAgentByCtid( string ctid )
		{
			Organization entity = OrganizationServices.GetSummaryByCtid( ctid );
			if ( entity != null && entity.Id > 0)
			{

			}

			return entity;
		}
		public static Credential ResolveCredentialByCtid( string ctid )
		{
			Credential entity = CredentialServices.GetMinimumByCtid( ctid );
			if ( entity != null && entity.Id > 0 )
			{

			}

			return entity;
		}

		/// <summary>
		/// Extract the ctid from a properly formatted registry URI
		/// </summary>
		/// <param name="registryURL"></param>
		/// <returns></returns>
		public static string ExtractCtid( string registryURL )
		{
			string ctid = "";
			if ( string.IsNullOrWhiteSpace( registryURL ) )
				return "";

			if ( registryURL.Length == 39 && registryURL.ToLower().IndexOf( "ce-" ) == 0 )
				return registryURL;

			int pos = registryURL.ToLower().IndexOf( "/graph/ce-" );
			if ( pos > 1 )
			{
				ctid = registryURL.Substring( pos + 7 );
			}
			else
			{
				pos = registryURL.ToLower().IndexOf( "/resources/ce-" );
				if ( pos > 1 )
					ctid = registryURL.Substring( pos + 11 );
				else
				{
					//shouldn't happen, once all fixed. In case was published without ce-
					pos = registryURL.ToLower().IndexOf( "/resources/" );
					if ( pos > 10 )
					{
						ctid = "ce-" + registryURL.Substring( pos + 11 );
					}
					else
					{
						pos = registryURL.ToLower().IndexOf( "/ce-" );
						if ( pos > -1 )
							ctid = registryURL.Substring( pos + 1 );
					}
				}
			}

			return ctid;
		}
		public static bool IsCtidValid( string ctid, ref List<string> messages )
		{
			bool isValid = true;

			if ( string.IsNullOrWhiteSpace( ctid ) )
			{
				messages.Add( "Error - A CTID property must be entered." );
				return false;
			}
			//just in case, handle old formats
			ctid = ctid.Replace( "urn:ctid:", "ce-" );
			if ( ctid.Length != 39 )
			{
				messages.Add( "Error - Invalid CTID format. The proper format is ce-UUID. ex. ce-84365AEA-57A5-4B5A-8C1C-EAE95D7A8C9B" );
				return false;
			}

			if ( !ctid.StartsWith( "ce-" ) )
			{
				//actually we could add this if missing - but maybe should NOT
				messages.Add( "Error - The CTID property must begin with ce-." );
				return false;
			}
			//now we have the proper length and format, the remainder must be a valid guid
			if ( !ServiceHelper.IsValidGuid( ctid.Substring( 3, 36 ) ) )
			{
				//actually we could add this if missing - but maybe should NOT
				messages.Add( "Error - Invalid CTID format. The proper format is ce-UUID. ex. ce-84365AEA-57A5-4B5A-8C1C-EAE95D7A8C9B" );
				return false;
			}

			return isValid;
		}

		/// <summary>
		/// Retrieve a resource from the registry by ctid
		/// </summary>
		/// <param name="ctid"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public static int GetEntityTypeIdFromResource( string ctid )
		{
			string statusMessage = "";
			string ctdlType = "";

			string r = RegistryServices.GetResourceByCtid( ctid, ref ctdlType, ref statusMessage );

			switch ( ctdlType.ToLower() )
			{
				case "credentialorganization":
					return 2;
				//break;
				case "qacredentialorganization":
					//what distinctions do we need for QA orgs?
					return 2;
				//break;
				case "organization":
					return 2;
				case "AssessmentProfile":
					return 3;
				//break;
				case "learningopportunityprofile":
					return 7;
				//break;
				case "conditionmanifest":
					return 19;
				//break;
				case "costmanifest":
					return 20;
				//break;
				default:
					//default to credential
					return 1;
					//break;
			}
			
		}
		public void LookupCtdlType(string ctdl)
		{
			List<CodeItem> list = EntityTypes_GetAll();

			//CodeItem item = list.FirstOrDefault.Where( s => s.SchemaName == ctdl );

			//return value;
		}
		public List<CodeItem> EntityTypes_GetAll()
		{
			List<CodeItem> list = new List<CodeItem>();
			//check if in cache
			string key = "entityCodes";
			//check cache for list
			if ( HttpRuntime.Cache[ key ] != null )
			{
				list = ( List<CodeItem> ) HttpRuntime.Cache[ key ];
				return list;
			}

			CodeItem value = new CodeItem();
			list = CodesManager.Codes_EntityTypes_GetAll();
			HttpRuntime.Cache.Insert( key, list );

			return list;
		}
	}
}

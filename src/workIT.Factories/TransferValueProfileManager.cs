using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Newtonsoft.Json;
using workIT.Models;
using workIT.Models.Common;
using workIT.Models.ProfileModels;
using workIT.Utilities;
using DBEntity = workIT.Data.Tables.TransferValueProfile;
using EntityContext = workIT.Data.Tables.workITEntities;
using ThisResource = workIT.Models.Common.TransferValueProfile;

namespace workIT.Factories
{
    public class TransferValueProfileManager : BaseFactory
	{
		static string thisClassName = "TransferValueProfileManager";
		static string EntityType = "TransferValue";
		static int tvpNameMaxLength = UtilityManager.GetAppKeyValue( "maxResourceNameLength", 800 );

		#region Persistance ===================


		/// <summary>
		/// Add/Update a TransferValueProfile
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public bool Save( ThisResource entity, ref SaveStatus status )
		{
			bool isValid = true;
			int count = 0;

			DBEntity efEntity = new DBEntity();
			try
			{
				using ( var context = new EntityContext() )
				{

					if ( ValidateProfile( entity, ref status ) == false )
					{
						return false;
					}

					if ( entity.Id == 0 )
					{
						//add
						efEntity = new DBEntity();
						MapToDB( entity, efEntity );
						efEntity.EntityStateId = entity.EntityStateId = 3;

						if ( IsValidDate( status.EnvelopeCreatedDate ) )
						{
							efEntity.Created = status.LocalCreatedDate;
							efEntity.LastUpdated = status.LocalCreatedDate;
						}
						else
						{
							efEntity.Created = System.DateTime.Now;
							efEntity.LastUpdated = System.DateTime.Now;
						}

						if ( IsValidGuid( entity.RowId ) )
							efEntity.RowId = entity.RowId;
						else
							efEntity.RowId = Guid.NewGuid();

						context.TransferValueProfile.Add( efEntity );

						count = context.SaveChanges();

						entity.Id = efEntity.Id;
						entity.RowId = efEntity.RowId;
						if ( count == 0 )
						{
							status.AddWarning( string.Format( " Unable to add Profile: {0} <br\\> ", string.IsNullOrWhiteSpace( entity.Name ) ? "no description" : entity.Name ) );
						}
						else
						{
							entity.RowId = efEntity.RowId;
							entity.Created = efEntity.Created.Value;
							entity.LastUpdated = efEntity.LastUpdated.Value;
							entity.Id = efEntity.Id;
							UpdateEntityCache( entity, ref status );

							//add log entry
							SiteActivity sa = new SiteActivity()
							{
								ActivityType = "TransferValueProfile",
								Activity = "Import",
								Event = "Add",
								Comment = string.Format( "New Transfer Value Profile was found by the import. Name: {0}, URI: {1}", entity.Name, entity.SubjectWebpage ),
								ActivityObjectId = entity.Id
							};
							new ActivityManager().SiteActivityAdd( sa );

							if ( !UpdateParts( entity, ref status ) )
								isValid = false;
						}
					}
					else
					{

						efEntity = context.TransferValueProfile.FirstOrDefault( s => s.Id == entity.Id );
						if ( efEntity != null && efEntity.Id > 0 )
						{
							entity.RowId = efEntity.RowId;
							//update
							MapToDB( entity, efEntity );
                            efEntity.EntityStateId = entity.EntityStateId = 3;
                            if ( IsValidDate( status.EnvelopeCreatedDate ) && status.LocalCreatedDate < efEntity.Created )
							{
								efEntity.Created = status.LocalCreatedDate;
							}
							if ( IsValidDate( status.EnvelopeUpdatedDate ) && status.LocalUpdatedDate != efEntity.LastUpdated )
							{
								efEntity.LastUpdated = status.LocalUpdatedDate;
							}
							//has changed?
							if ( HasStateChanged( context ) )
							{
								if ( IsValidDate( status.EnvelopeUpdatedDate ) )
									efEntity.LastUpdated = status.LocalUpdatedDate;
								else
									efEntity.LastUpdated = DateTime.Now;

								count = context.SaveChanges();
								//
								entity.LastUpdated = efEntity.LastUpdated.Value;
								UpdateEntityCache( entity, ref status );
								//add log entry
								SiteActivity sa = new SiteActivity()
								{
									ActivityType = "TransferValueProfile",
									Activity = "Import",
									Event = "Update",
									Comment = string.Format( "Updated Transfer Value Profile found by the import. Name: {0}, URI: {1}", entity.Name, entity.SubjectWebpage ),
									ActivityObjectId = entity.Id
								};
								new ActivityManager().SiteActivityAdd( sa );

							}
							if ( !UpdateParts( entity, ref status ) )
								isValid = false;
						}
					}
				}
			}
			catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
			{
				string message = HandleDBValidationError( dbex, thisClassName + $".Save. id: {entity.Id}, CTID: {entity.CTID}", EntityType );
				LoggingHelper.LogError( dbex, thisClassName, string.Format( "Save for id: {0}, Name: {1}, CTID:{2}", entity.Id, entity.Name, entity.CTID ) );
				status.AddError( thisClassName + ".Save(). Error - the save was not successful. DbEntityValidationException. " + message );
				isValid = false;
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, $"TransferValueProfileManager.Save() CTID: {entity.CTID}, Name: {entity.Name}" );
			}

			return isValid;
		}

		public bool UpdateParts( ThisResource entity, ref SaveStatus status )
		{
			bool isAllValid = true;
			Entity_AgentRelationshipManager eamgr = new Entity_AgentRelationshipManager();
			Entity relatedEntity = EntityManager.GetEntity( entity.RowId );
			if ( relatedEntity == null || relatedEntity.Id == 0 )
			{
				status.AddError( "Error - the related Entity was not found." );
				return false;
			}
			eamgr.DeleteAll( relatedEntity, ref status );
			eamgr.SaveList( relatedEntity.Id, Entity_AgentRelationshipManager.ROLE_TYPE_OWNER, entity.OwnedBy, ref status );
			//
			eamgr.SaveList( relatedEntity.Id, Entity_AgentRelationshipManager.ROLE_TYPE_PUBLISHEDBY, entity.PublishedBy, ref status );

			//consider storing the class properties as Json!

			//derived from
			//where to store this? It commonly require Entity.TransferValueProfile
			var etvlMgr = new Entity_TransferValueProfileManager();

			//*****NOTE: API does check to ensure derived from is not same as the current
			etvlMgr.SaveList( entity.DerivedFromForImport, entity.RowId, ref status );

			Entity_AssertionManager eaMgr = new Entity_AssertionManager();
			eaMgr.DeleteAll( relatedEntity, ref status );

			//delete all Entity.Lopp, .Cred, and .Assessment relationships, and then add?
			//would be convenient if a delete wasn't necessary
			//NOTE: this will leave orphan reference objects. Will need to clean up. 
			//could check if target is a reference. If so delete, or check if there are other references
			//NOTE: this should have been done in TransferValueServices.HandlingExistingEntity - when is done corrently, remove this
			Entity_CredentialManager ecm = new Entity_CredentialManager();
			ecm.DeleteAll( relatedEntity, ref status );
			//
			var eam = new Entity_AssessmentManager();
			eam.DeleteAll( relatedEntity, ref status );
			//
			var elom = new Entity_LearningOpportunityManager();
			elom.DeleteAll( relatedEntity, ref status );

			//NEW start using hasResource
			var hasResourceMgr = new Entity_HasResourceManager();
			hasResourceMgr.DeleteAll( relatedEntity, ref status );

			//
			var etvp = new Entity_TransferValueProfileManager();
			etvp.DeleteAll( relatedEntity, ref status );
			//
			foreach ( var item in entity.TransferValueFromImport )
			{
				int newId = 0;
				var tvpFromEntity = EntityManager.GetEntity( item, false );
				if ( tvpFromEntity == null || tvpFromEntity.Id == 0 )
				{
					status.AddError( string.Format( "{0}.UpdateParts - TransferValueFromImport. TVP: {1}. An entity was not found for GUID: {2}", thisClassName, entity.Id, item ) );
					continue;
				}
				//may only need the hasResource, not sure the others are being used? 
				//NOTE*** OLDER TVPs will not have HasResource ****
				newId = hasResourceMgr.Add( relatedEntity, tvpFromEntity.EntityTypeId, tvpFromEntity.EntityBaseId, Entity_HasResourceManager.HAS_RESOURCE_TYPE_TransferValueFrom, ref status );
				if ( newId > 0 )
				{
					entity.PendingReindexList.Add( new CodeItem()
					{
						EntityTypeId = tvpFromEntity.EntityTypeId,
						Id = tvpFromEntity.EntityBaseId
					} );
				}

				///==========================='
				//TODO - make obsolete
				if ( tvpFromEntity.EntityTypeId == 1 )
				{
					//may need to designate for and fromEntity later
					ecm.Add( entity.RowId, tvpFromEntity.EntityBaseId, BaseFactory.RELATIONSHIP_TYPE_IS_PART_OF, ref newId, ref status );
					if ( newId > 0 )
					{
						//what happens with CredentialIds ==> these will be added to elastic pending index in the tvp services 
						//entity.CredentialIds.Add( from.EntityBaseId );
						entity.PendingReindexList.Add( new CodeItem()
						{
							EntityTypeId = tvpFromEntity.EntityTypeId,
							Id = tvpFromEntity.EntityBaseId
						} );
					}
				}
				else if ( tvpFromEntity.EntityTypeId == 3 )
				{
					newId = eam.Add( entity.RowId, tvpFromEntity.EntityBaseId, BaseFactory.RELATIONSHIP_TYPE_IS_PART_OF, true, ref status );
					if ( newId > 0 )
					{
						//entity.AssessmentIds.Add( fromEntity.EntityBaseId );
						entity.PendingReindexList.Add( new CodeItem()
						{
							EntityTypeId = tvpFromEntity.EntityTypeId,
							Id = tvpFromEntity.EntityBaseId
						} );
					}
				}
				else if ( tvpFromEntity.EntityTypeId == 7 )
				{
					newId = elom.Add( entity.RowId, tvpFromEntity.EntityBaseId, BaseFactory.RELATIONSHIP_TYPE_IS_PART_OF, true, ref status );
					if ( newId > 0 )
					{
						//entity.LearningOpportunityIds.Add( fromEntity.EntityBaseId );
						entity.PendingReindexList.Add( new CodeItem()
						{
							EntityTypeId = tvpFromEntity.EntityTypeId,
							Id = tvpFromEntity.EntityBaseId
						} );
					}
				}
				//else if ( tvpFromEntity.EntityTypeId == CodesManager.ENTITY_TYPE_JOB_PROFILE )
				//{
				//	//need equiv of RELATIONSHIP_TYPE_IS_PART_OF
				//	newId = hasResMgr.Add( relatedEntity, CodesManager.ENTITY_TYPE_JOB_PROFILE, tvpFromEntity.EntityBaseId, Entity_HasResourceManager.HAS_RESOURCE_TYPE_TransferValueFrom, ref status );
				//	if ( newId > 0 )
				//	{
				//		entity.PendingReindexList.Add( new CodeItem()
				//		{
				//			EntityTypeId = CodesManager.ENTITY_TYPE_JOB_PROFILE,
				//			Id = tvpFromEntity.EntityBaseId
				//		} );
				//	}
				//}
				//else if ( tvpFromEntity.EntityTypeId == CodesManager.ENTITY_TYPE_OCCUPATIONS_PROFILE )
				//{
				//	newId = hasResMgr.Add( relatedEntity, CodesManager.ENTITY_TYPE_OCCUPATIONS_PROFILE, tvpFromEntity.EntityBaseId, Entity_HasResourceManager.HAS_RESOURCE_TYPE_TransferValueFrom, ref status );
				//	if ( newId > 0 )
				//	{
				//		entity.PendingReindexList.Add( new CodeItem()
				//		{
				//			EntityTypeId = CodesManager.ENTITY_TYPE_OCCUPATIONS_PROFILE,
				//			Id = tvpFromEntity.EntityBaseId
				//		} );
				//	}
				//}
			}

			foreach ( var item in entity.TransferValueForImport )
			{
				int newId = 0;
				var tvpForEntity = EntityManager.GetEntity( item, false );
				if ( tvpForEntity == null || tvpForEntity.Id == 0 )
				{
					//??
					status.AddError( string.Format( "{0}.UpdateParts - TransferValueForImport. TVP: {1}. An entity was not found for GUID: {2}", thisClassName, entity.Id, item ) );
					continue;
				}
				//may only need the hasResource, not sure the others are being used? 
				newId = hasResourceMgr.Add( relatedEntity, tvpForEntity.EntityTypeId, tvpForEntity.EntityBaseId, Entity_HasResourceManager.HAS_RESOURCE_TYPE_TransferValueFor, ref status );
				if ( newId > 0 )
				{
					entity.PendingReindexList.Add( new CodeItem()
					{
						EntityTypeId = tvpForEntity.EntityTypeId,
						Id = tvpForEntity.EntityBaseId
					} );
				}

				//============================================
				//TODO - make obsolete
				if ( tvpForEntity.EntityTypeId == 1 )
				{
					ecm.Add( entity.RowId, tvpForEntity.EntityBaseId, BaseFactory.RELATIONSHIP_TYPE_HAS_PART, ref newId, ref status );
					if ( newId > 0 )
					{
						//entity.CredentialIds.Add( tvpForEntity.EntityBaseId );
						entity.PendingReindexList.Add( new CodeItem()
						{
							EntityTypeId = tvpForEntity.EntityTypeId,
							Id = tvpForEntity.EntityBaseId
						} );
					}

				}
				else if ( tvpForEntity.EntityTypeId == 3 )
				{
					newId = eam.Add( entity.RowId, tvpForEntity.EntityBaseId, BaseFactory.RELATIONSHIP_TYPE_HAS_PART, true, ref status );
					if ( newId > 0 )
					{
						//entity.AssessmentIds.Add( tvpForEntity.EntityBaseId );
						entity.PendingReindexList.Add( new CodeItem()
						{
							EntityTypeId = tvpForEntity.EntityTypeId,
							Id = tvpForEntity.EntityBaseId
						} );
					}
				}
				else if ( tvpForEntity.EntityTypeId == 7 )
				{
					newId = elom.Add( entity.RowId, tvpForEntity.EntityBaseId, BaseFactory.RELATIONSHIP_TYPE_HAS_PART, true, ref status );
					if ( newId > 0 )
					{
						//entity.LearningOpportunityIds.Add( tvpForEntity.EntityBaseId );
						entity.PendingReindexList.Add( new CodeItem()
						{
							EntityTypeId = tvpForEntity.EntityTypeId,
							Id = tvpForEntity.EntityBaseId
						} );
					}

				}
				//else if ( tvpForEntity.EntityTypeId == CodesManager.ENTITY_TYPE_JOB_PROFILE || tvpForEntity.EntityTypeId == CodesManager.ENTITY_TYPE_OCCUPATIONS_PROFILE )
				//{
				//	newId = hasResMgr.Add( relatedEntity, tvpForEntity.EntityTypeId, tvpForEntity.EntityBaseId, Entity_HasResourceManager.HAS_RESOURCE_TYPE_TransferValueFor, ref status );
				//	if ( newId > 0 )
				//	{
				//		entity.PendingReindexList.Add( new CodeItem()
				//		{
				//			EntityTypeId = tvpForEntity.EntityTypeId,
				//			Id = tvpForEntity.EntityBaseId
				//		} );
				//	}
				//	//newId = hasResMgr.Add( relatedEntity, CodesManager.ENTITY_TYPE_JOB_PROFILE, tvpForEntity.EntityBaseId, Entity_HasResourceManager.HAS_RESOURCE_TYPE_TransferValueFor, ref status );
				//	//if ( newId > 0 )
				//	//{
				//	//	entity.PendingReindexList.Add( new CodeItem()
				//	//	{
				//	//		EntityTypeId = CodesManager.ENTITY_TYPE_JOB_PROFILE,
				//	//		Id = tvpForEntity.EntityBaseId
				//	//	} );
				//	//}
				//}
				//else if ( tvpForEntity.EntityTypeId == CodesManager.ENTITY_TYPE_OCCUPATIONS_PROFILE )
				//{
				//	newId = hasResMgr.Add( relatedEntity, CodesManager.ENTITY_TYPE_OCCUPATIONS_PROFILE, tvpForEntity.EntityBaseId, Entity_HasResourceManager.HAS_RESOURCE_TYPE_TransferValueFor, ref status );
				//	if ( newId > 0 )
				//	{
				//		entity.PendingReindexList.Add( new CodeItem()
				//		{
				//			EntityTypeId = CodesManager.ENTITY_TYPE_OCCUPATIONS_PROFILE,
				//			Id = tvpForEntity.EntityBaseId
				//		} );
				//	}
				//}
			}

			foreach ( var item in entity.DerivedFromForImport )
			{
				var from = Get(item );
				if ( from == null || from.Id == 0 )
				{
					//??
					status.AddError( string.Format( "{0}.UpdateParts - DerivedFromForImport. TVP: {1}. A TVP was not found for ID: {2}", thisClassName, entity.Id, item ) );
					continue;
				}
				//check that not the same as current TVP
				if (from.Id == entity.Id)
				{
					status.AddError( string.Format( "{0}.UpdateParts - DerivedFromForImport. TVP: {1}. The DerivedFrom TVP Id ({2}) is the same as the current TVP ID", thisClassName, entity.Id, item ) );
					continue;
				}
				etvp.Add( entity.RowId, item, ref status );
			}

			//ProcessProfile
			Entity_ProcessProfileManager ppm = new Factories.Entity_ProcessProfileManager();
			ppm.DeleteAll( relatedEntity, ref status );
			try
			{
				//24-02-29 mp - TVP doesn't have an admin process profile
				//ppm.SaveList( entity.AdministrationProcess, Entity_ProcessProfileManager.ADMIN_PROCESS_TYPE, entity.RowId, ref status );
				ppm.SaveList( entity.DevelopmentProcess, Entity_ProcessProfileManager.DEV_PROCESS_TYPE, entity.RowId, ref status );
			}
			catch ( Exception ex )
			{
				string message = FormatExceptions( ex );
				LoggingHelper.LogError( ex, thisClassName + string.Format( ".AddProfiles() - ProcessProfiles. id: {0}", entity.Id ) );
				status.AddWarning( thisClassName + ".AddProfiles(). Exceptions encountered handling ProcessProfiles. " + message );
			}
			return isAllValid;
		}

		public int AddPendingRecord( Guid entityUid, string ctid, string registryAtId, ref SaveStatus status )
		{
			DBEntity efEntity = new DBEntity();
			try
			{
				using ( var context = new EntityContext() )
				{
					if ( !IsValidGuid( entityUid ) )
					{
						status.AddError( thisClassName + " - A valid GUID must be provided to create a pending entity" );
						return 0;
					}
					//quick check to ensure not existing
					ThisResource entity = GetByCtid( ctid );
					if ( entity != null && entity.Id > 0 )
						return entity.Id;

					//only add DB required properties
					//NOTE - an entity will be created via trigger
					efEntity.Name = "Placeholder until full document is downloaded";
					efEntity.Description = "Placeholder until full document is downloaded";
					efEntity.EntityStateId = 1;
					efEntity.RowId = entityUid;
					//watch that Ctid can be  updated if not provided now!!
					efEntity.CTID = ctid;
					efEntity.SubjectWebpage = registryAtId;

					efEntity.Created = System.DateTime.Now;
					efEntity.LastUpdated = System.DateTime.Now;

					context.TransferValueProfile.Add( efEntity );
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						SiteActivity sa = new SiteActivity()
						{
							ActivityType = EntityType,
							Activity = "Import",
							Event = string.Format( "Add Pending {0}", EntityType ),
							Comment = string.Format( "Pending {0} was added by the import. ctid: {1}, registryAtId: {2}", EntityType, ctid, registryAtId ),
							ActivityObjectId = efEntity.Id
						};
						new ActivityManager().SiteActivityAdd( sa );
						//Question should this be in the EntityCache?
						entity.Id = efEntity.Id;
						entity.RowId = efEntity.RowId;
						entity.CTID = efEntity.CTID;
						entity.EntityStateId = 1;
						entity.Name = efEntity.Name;
						entity.Description = efEntity.Description;
						entity.SubjectWebpage = efEntity.SubjectWebpage;
						entity.Created = ( DateTime )efEntity.Created;
						entity.LastUpdated = ( DateTime )efEntity.LastUpdated;
						UpdateEntityCache( entity, ref status );
						return efEntity.Id;
					}

					status.AddError( thisClassName + " Error - the save was not successful, but no message provided. " );
				}
			}
			catch ( Exception ex )
			{
				string message = FormatExceptions( ex );
				LoggingHelper.LogError( ex, thisClassName + string.Format( ".AddPendingRecord. entityUid:  {0}, ctid: {1}", entityUid, ctid ) );
				status.AddError( thisClassName + " Error - the save was not successful. " + message );

			}
			return 0;
		}

		public void UpdateEntityCache( ThisResource document, ref SaveStatus status )
		{
			EntityCache ec = new EntityCache()
			{
				EntityTypeId = CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE,
				EntityType = EntityType,
				EntityStateId = document.EntityStateId,
				EntityUid = document.RowId,
				BaseId = document.Id,
				Description = document.Description,
				SubjectWebpage = document.SubjectWebpage,
				CTID = document.CTID,
				Created = document.Created,
				LastUpdated = document.LastUpdated,
				//ImageUrl = document.ImageUrl,
				Name = document.Name,
				OwningAgentUID = document.PrimaryAgentUID,
				OwningOrgId = document.OrganizationId
			};
            //var defStatus = CodesManager.Codes_PropertyValue_GetBySchema( CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS_ACTIVE );
            var ceasedStatus = CodesManager.GetLifeCycleStatus( CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS_CEASED );
            if ( document.LifeCycleStatusTypeId > 0 && document.LifeCycleStatusTypeId == ceasedStatus.Id )
            {
                ec.IsActive = false;
            }
            var statusMessage = string.Empty;
			if ( new EntityManager().EntityCacheSave( ec, ref statusMessage ) == 0 )
			{
				status.AddError( thisClassName + string.Format( ".UpdateEntityCache for '{0}' ({1}) failed: {2}", document.Name, document.Id, statusMessage ) );
			}
		}

		/// <summary>
		/// Do VIRTUAL delete based on import of deleted documents
		/// </summary>
		/// <param name="credentialRegistryId">NOT CURRENTLY HANDLED</param>
		/// <param name="ctid"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool Delete( string ctid, ref string statusMessage )
		{
			bool isValid = true;
			if ( string.IsNullOrWhiteSpace( ctid ) )
			{
				statusMessage = thisClassName + ".Delete() Error - a valid CTID must be provided";
				return false;
			}
			int orgId = 0;
			Guid orgUid = new Guid();
			using ( var context = new EntityContext() )
			{
				try
				{
					context.Configuration.LazyLoadingEnabled = false;
					var efEntity = context.TransferValueProfile
								.FirstOrDefault( s => s.CTID == ctid );

					if ( efEntity != null && efEntity.Id > 0 )
					{
						//TODO - may need a check for existing alignments
						Guid rowId = efEntity.RowId;
						if ( IsValidGuid( efEntity.OwningAgentUid ) )
						{
							Organization org = OrganizationManager.GetBasics( ( Guid ) efEntity.OwningAgentUid );
							orgId = org.Id;
							orgUid = org.RowId;
						}
						//need to remove from Entity.
						//-using before delete trigger - verify won't have RI issues
						string msg = string.Format( " TransferValueProfile. Id: {0}, Name: {1}, Ctid: {2}", efEntity.Id, efEntity.Name, efEntity.CTID );
						//leaving as virtual?
						//need to check for in use.
						//context.TransferValueProfile.Remove( efEntity );
						efEntity.EntityStateId = 0;
						efEntity.LastUpdated = System.DateTime.Now;

						int count = context.SaveChanges();
						if ( count >= 0 )
						{
							new ActivityManager().SiteActivityAdd( new SiteActivity()
							{
								ActivityType = "TransferValueProfile",
								Activity = "Import",
								Event = "Delete",
								Comment = msg
							} );
							isValid = true;
							//delete cache
							new EntityManager().EntityCacheDelete( rowId, ref statusMessage );

							//add pending request 
							List<String> messages = new List<string>();
							new SearchPendingReindexManager().AddDeleteRequest( CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, efEntity.Id, ref messages );

							//mark owning org for updates (actually should be covered by ReindexAgentForDeletedArtifact
							new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, orgId, 1, ref messages );
						}
					}
					else
					{
						statusMessage = thisClassName + ".Delete() Warning No action taken, as the record was not found.";
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + ".Delete(envelopeId)" );
					statusMessage = FormatExceptions( ex );
					isValid = false;
					if ( statusMessage.ToLower().IndexOf( "the delete statement conflicted with the reference constraint" ) > -1 )
					{
						statusMessage = thisClassName + "Error: this record cannot be deleted as it is being referenced by other items, such as roles or credentials. These associations must be removed before this assessment can be deleted.";
					}
				}
			}
			return isValid;
		}
		public bool ValidateProfile( ThisResource profile, ref SaveStatus status )
		{
			status.HasSectionErrors = false;

			if ( string.IsNullOrWhiteSpace( profile.Name ) )
			{
				status.AddWarning( "An Transfer Value Profile name must be entered" );
			}

			//
			var defStatus = CodesManager.GetLifeCycleStatus( CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS_ACTIVE );
			if ( profile.LifeCycleStatusType == null || profile.LifeCycleStatusType.Items == null || profile.LifeCycleStatusType.Items.Count == 0 )
			{
				//23-10-06 - no longer defaulting
				//profile.LifeCycleStatusTypeId = defStatus.Id;
			}
			else
			{
				var schemaName = profile.LifeCycleStatusType.GetFirstItem().SchemaName;
				CodeItem ci = CodesManager.GetLifeCycleStatus( CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, schemaName );
				if ( ci == null || ci.Id < 1 )
				{
					//while this should never happen, should have a default
					status.AddError( string.Format( "A valid LifeCycleStatusType must be included. Invalid: {0}", schemaName ) );
					//profile.LifeCycleStatusTypeId = defStatus.Id;
				}
				else
					profile.LifeCycleStatusTypeId = ci.Id;
			}


			return status.WasSectionValid;
		}

		#endregion
		#region  retrieval ==================

		/// <summary>
		/// Get a record
		/// </summary>
		/// <param name="profileId"></param>
		/// <returns></returns>
		public static ThisResource Get( int profileId, bool gettingAll = true )
		{
			ThisResource entity = new ThisResource();
			if ( profileId == 0 )
				return entity;
			try
			{
				using ( var context = new EntityContext() )
				{
					DBEntity item = context.TransferValueProfile
							.FirstOrDefault( s => s.Id == profileId && s.EntityStateId == 3 );

					if ( item != null && item.Id > 0 )
					{
						if ( item.EntityStateId == 0 )
						{
							LoggingHelper.DoTrace( 1, string.Format( thisClassName + " Error: encountered request for detail for a deleted record. Name: {0}, CTID:{1}", item.Name, item.CTID ) );
							entity.Name = "Record was not found.";
							entity.CTID = item.CTID;
							return entity;
						}
						MapFromDB( item, entity, gettingAll );
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".Get" );
			}
			return entity;
		}//


		public static ThisResource GetByUrl( string SubjectWebpage )
		{
			ThisResource entity = new ThisResource();
			if ( string.IsNullOrWhiteSpace( SubjectWebpage ) )
				return entity;
			try
			{
				using ( var context = new EntityContext() )
				{
					//lookup by SubjectWebpage, or SourceUrl
					DBEntity item = context.TransferValueProfile
							.FirstOrDefault( s =>
								( s.SubjectWebpage != null && s.SubjectWebpage.ToLower() == SubjectWebpage.ToLower() && s.EntityStateId == 3 )
							);

					if ( item != null && item.Id > 0 )
					{
						MapFromDB( item, entity );
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetByUrl" );
			}
			return entity;
		}//

		public static ThisResource GetByCtid( string ctid, bool gettingAll = false )
		{
			ThisResource entity = new ThisResource();
			if ( string.IsNullOrWhiteSpace( ctid ) )
				return entity;
			try
			{
				using ( var context = new EntityContext() )
				{
					//lookup by SubjectWebpage, or SourceUrl
					DBEntity item = context.TransferValueProfile
							.FirstOrDefault( s => s.CTID.ToLower() == ctid.ToLower() && s.EntityStateId == 3 );

					if ( item != null && item.Id > 0 )
					{
						MapFromDB( item, entity, gettingAll );
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetByCtid: " + ctid );
			}
			return entity;
		}//
		public static int Count_ForOwningOrg( Guid orgUid )
		{
			int totalRecords = 0;

			using ( var context = new EntityContext() )
			{
				var results = context.TransferValueProfile
							.Where( s => s.OwningAgentUid == orgUid && s.EntityStateId == 3 && s.EntityStateId == 3 )
							.ToList();

				if ( results != null && results.Count > 0 )
				{
					totalRecords = results.Count();
				}
			}
			return totalRecords;
		}

		public static List<Credential> GetAllCredentials( int topParentTypeId, int topParentEntityBaseId )
		{
			var list = new List<Credential>();
			Entity parent = EntityManager.GetEntity( topParentTypeId, topParentEntityBaseId );
			//Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				return list;
			}

			try
			{
				//only need minimum
				list = Entity_CredentialManager.GetAll( parent.EntityUid, 0 );
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetAllCredentials" );
			}
			return list;
		}//
		public static List<Credential> GetAllCredentials( int topParentEntityTypeId, int topParentEntityBaseId, int relationshipTypeId )
		{
			var list = new List<Credential>();
			Entity parent = EntityManager.GetEntity( topParentEntityTypeId, topParentEntityBaseId );
			if ( parent == null || parent.Id == 0 )
			{
				return list;
			}

			try
			{
				//TODO - start using hasResource 
				var newList = Entity_HasResourceManager.GetAllForEntityType( parent, 1, relationshipTypeId );
				if ( newList != null && newList.Any() )
				{
					foreach ( var item in newList )
					{
						var record = new Credential()
						{
							Id = item.Id,
							Name = item.Name,
							CTID = item.CTID,
							Description = item.Description,
							NamePlusOrganization = item.Name
						};
						if ( item.ResourcePrimaryOrganizationName != null )
							record.NamePlusOrganization += " (" + item.ResourcePrimaryOrganizationName + ")";
						list.Add( record );
					}
				}

			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + $".GetAllCredentials, topParentEntityTypeId: {topParentEntityTypeId}" );
			}
			return list;
		}//
		public static List<AssessmentProfile> GetAllAssessments( int topParentTypeId, int topParentEntityBaseId )
		{
			var list = new List<AssessmentProfile>();
			Entity parent = EntityManager.GetEntity( topParentTypeId, topParentEntityBaseId );
			//Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				return list;
			}

			try
			{
				//only need minimum
				list = Entity_AssessmentManager.GetAll( parent.EntityUid, 0 );
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetAllAssessments" );
			}
			return list;
		}//
		public static List<AssessmentProfile> GetAllAssessments( int topParentEntityTypeId, int topParentEntityBaseId, int relationshipTypeId )
		{
			var list = new List<AssessmentProfile>();
			Entity parent = EntityManager.GetEntity( topParentEntityTypeId, topParentEntityBaseId );
			if ( parent == null || parent.Id == 0 )
			{
				return list;
			}

			try
			{
				//TODO - start using hasResource 
				var newList = Entity_HasResourceManager.GetAllForEntityType( parent, 3, relationshipTypeId );
				if ( newList == null || !newList.Any() )
				{
					//back up for older ones?
					newList = Entity_HasResourceManager.GetAllForEntityType( parent, 3 );
				}
				if ( newList != null && newList.Any() )
				{
					foreach ( var item in newList )
					{
						var record = new AssessmentProfile()
						{
							Id = item.Id,
							Name = item.Name,
							CTID = item.CTID,
							Description = item.Description,
							NamePlusOrganization = item.Name
						};
						if ( item.ResourcePrimaryOrganizationName != null )
							record.NamePlusOrganization += " (" + item.ResourcePrimaryOrganizationName + ")";
						list.Add( record );
					}
				}

			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + $".GetAllCredentials, topParentEntityTypeId: {topParentEntityTypeId}" );
			}
			return list;
		}//
		public static List<LearningOpportunityProfile> GetAllLearningOpportunities( int topParentEntityTypeId, int topParentEntityBaseId )
		{
			var list = new List<LearningOpportunityProfile>();
			Entity parent = EntityManager.GetEntity( topParentEntityTypeId, topParentEntityBaseId );
			if ( parent == null || parent.Id == 0 )
			{
				return list;
			}

			try
			{
				//TODO - start using hasResource 
				var newList = Entity_HasResourceManager.GetAll( parent );
				list = Entity_LearningOpportunityManager.TargetResource_GetAll( parent.EntityUid, true, false,0 );
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetAllLopps" );
			}
			return list;
		}//
		public static List<LearningOpportunityProfile> GetAllLearningOpportunities( int topParentEntityTypeId, int topParentEntityBaseId, int relationshipTypeId )
		{
			var list = new List<LearningOpportunityProfile>();
			Entity parent = EntityManager.GetEntity( topParentEntityTypeId, topParentEntityBaseId );
			if ( parent == null || parent.Id == 0 )
			{
				return list;
			}

			try
			{
				//TODO - start using hasResource 
				var newList = Entity_HasResourceManager.GetAllForEntityType( parent, 7, relationshipTypeId );
				if ( newList != null && newList.Any() )
				{
					foreach ( var item in newList )
					{
						var record = new LearningOpportunityProfile() 
						{ 
							Id = item.Id,
							Name = item.Name,
							CTID = item.CTID,
							Description = item.Description,
							NamePlusOrganization = item.Name
						};
						if ( item.ResourcePrimaryOrganizationName != null )
							record.NamePlusOrganization += " (" + item.ResourcePrimaryOrganizationName + ")";
						list.Add( record );
					}
				} 
				//else
				//{
				//	var altList = Entity_HasResourceManager.GetAllForEntityType( parent, 7 );
				//}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + $".GetAllLearningOpportunities, topParentEntityTypeId: {topParentEntityTypeId}" );
			}
			return list;
		}//
		public static void MapToDB( ThisResource input, DBEntity output )
		{
			//want to ensure fields from create are not wiped
			//to.Id = from.Id;
			output.Name = AssignLimitedString( input.Name, tvpNameMaxLength - 5 );
			if ( input.Name.Length > tvpNameMaxLength )
			{
				//input.Name = input.Name.Substring( 0, tvpNameMaxLength );
				//log?
				LoggingHelper.LogError( $"{thisClassName}.MapToDB. CTID:{input.CTID}, Name length is greater than the max." );
			}
			
			//TODO - can't have pending or references
			output.EntityStateId = 3;
			output.Description = input.Description;
			output.CTID = input.CTID;
			output.SubjectWebpage = input.SubjectWebpage ?? string.Empty;
			output.CredentialRegistryId = input.CredentialRegistryId ?? string.Empty;
			output.OwningAgentUid = input.PrimaryAgentUID;
			//being set in validate profile method
			output.LifeCycleStatusTypeId = input.LifeCycleStatusTypeId;
			//need to handle a partial date
			if ( !string.IsNullOrWhiteSpace( input.StartDate ) )
			{
				output.StartDate = input.StartDate;
				if ( ( output.StartDate ?? string.Empty ).Length > 20 )
				{
					output.StartDate = output.StartDate.Substring( 0, 10 );
				}
			}
			else
				output.StartDate = null;
			//
			if ( !string.IsNullOrWhiteSpace( input.EndDate ) )
			{
				output.EndDate = input.EndDate;
				if ( ( output.EndDate ?? string.Empty ).Length > 20 )
				{
					output.EndDate = output.EndDate.Substring( 0, 10 );
				}
			}
			else
				output.EndDate = null;

            //
            output.SupersededBy = input.SupersededBy;
            output.Supersedes = input.Supersedes;
			output.InCatalog = GetUrlData( input.InCatalog );
			//output.CodedNotation = input.CodedNotation;

			//just store the json
			output.IdentifierJson = input.IdentifierJson;
			output.TransferValueJson = input.TransferValueJson;
			output.TransferValueFromJson = input.TransferValueFromJson;
			output.TransferValueForJson = input.TransferValueForJson;
			//
			output.LatestVersion = GetUrlData( input.LatestVersion, null );
			output.PreviousVersion = GetUrlData( input.PreviousVersion, null );
			output.NextVersion = GetUrlData( input.NextVersion, null );
			output.VersionIdentifier = input.VersionIdentifierJson;
			//output.ProfileGraph = input.ProfileGraph;
			//ensure we don't reset the graph
			//if ( !string.IsNullOrWhiteSpace( input.ProfileGraph ) )
			//	output.ProfileGraph = input.ProfileGraph;
			//else
			//{

			//}
		} //
		//we don't have to store the complete object, such as assessment, lopp, etc.
		public static string TransferValueActionToJson( List<TopLevelObject> input )
		{
			string json = string.Empty;


			return json;
		} //

		public static void MapFromDB( DBEntity input, ThisResource output, bool gettingAll = true )
		{
			output.Id = input.Id;
			output.RowId = input.RowId;
			output.EntityStateId = input.EntityStateId;
			output.Name = input.Name;
			output.FriendlyName = FormatFriendlyTitle( input.Name );

			output.Description = input.Description;
			output.CTID = input.CTID;

			if ( input.Created != null )
				output.Created = ( DateTime )input.Created;
			if ( input.LastUpdated != null )
				output.LastUpdated = ( DateTime )input.LastUpdated;
			if ( IsGuidValid( input.OwningAgentUid ) )
			{
				output.PrimaryAgentUID = ( Guid )input.OwningAgentUid;
				output.PrimaryOrganization = OrganizationManager.GetForSummary( output.PrimaryAgentUID );

				//get roles
				OrganizationRoleProfile orp = Entity_AgentRelationshipManager.AgentEntityRole_GetAsEnumerationFromCSV( output.RowId, output.PrimaryAgentUID );
				output.OwnerRoles = orp.AgentRole;
			}
			//
			output.OrganizationRole = Entity_AgentRelationshipManager.AgentEntityRole_GetAll_ToEnumeration( output.RowId, true );
			//

			//get related ....
			//var relatedEntity = EntityManager.GetEntity( output.RowId, false );
			//if ( relatedEntity != null && relatedEntity.Id > 0 )
			//	output.EntityLastUpdated = relatedEntity.LastUpdated;
			//NOTE: EntityLastUpdated should really be the last registry update now. Check how LastUpdated is assigned on import
			output.EntityLastUpdated = output.LastUpdated;
			//
			output.SubjectWebpage = input.SubjectWebpage;
			output.CredentialRegistryId = input.CredentialRegistryId ?? string.Empty;
			output.InCatalog = GetUrlData( input.InCatalog );
			//22-07-10 - LifeCycleStatusTypeId is now on the credential directly
			output.LifeCycleStatusTypeId = input.LifeCycleStatusTypeId;
			if ( output.LifeCycleStatusTypeId > 0 )
			{
				CodeItem ct = CodesManager.GetLifeCycleStatus( output.LifeCycleStatusTypeId );
				if ( ct != null && ct.Id > 0 )
				{
					output.LifeCycleStatus = ct.Title;
				}
				//retain example using an Enumeration for by other related tableS??? - old detail page?
				output.LifeCycleStatusType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS );
				output.LifeCycleStatusType.Items.Add( new EnumeratedItem() { Id = output.LifeCycleStatusTypeId, Name = ct.Name, SchemaName = ct.SchemaName } );
			}
			else
			{
				//OLD
				//output.LifeCycleStatusType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS );
				//EnumeratedItem statusItem = output.LifeCycleStatusType.GetFirstItem();
				//if ( statusItem != null && statusItem.Id > 0 && statusItem.Name != "Active" )
				//{

				//}
			}

            //output.CodedNotation = input.CodedNotation;
            //20-12-16 changed to a string as partial dates are possible
            if ( !string.IsNullOrWhiteSpace( input.StartDate ) )
				output.StartDate = input.StartDate;
			else
				output.StartDate = string.Empty;
			//
			if ( !string.IsNullOrWhiteSpace( input.EndDate ) )
				output.EndDate = input.EndDate;
			else
				output.EndDate = string.Empty;
	
			//derived from ....

			//get json and expand
			output.IdentifierJson = input.IdentifierJson;
			output.TransferValueJson = input.TransferValueJson;
			output.TransferValueFromJson = input.TransferValueFromJson;
			output.TransferValueForJson = input.TransferValueForJson;
			//
			if ( !string.IsNullOrWhiteSpace( output.IdentifierJson ) )
				output.Identifier = JsonConvert.DeserializeObject<List<Entity_IdentifierValue>>( output.IdentifierJson );
			if ( !string.IsNullOrWhiteSpace( output.TransferValueJson ) )
				output.TransferValue = JsonConvert.DeserializeObject<List<ValueProfile>>( output.TransferValueJson );

			//
			output.LatestVersion = input.LatestVersion;
			output.PreviousVersion = input.PreviousVersion;
			output.NextVersion = input.NextVersion;
			if ( !string.IsNullOrWhiteSpace( input.VersionIdentifier ) )
			{
				output.VersionIdentifier = JsonConvert.DeserializeObject<List<IdentifierValue>>( input.VersionIdentifier );
			}
			if ( !gettingAll )
				return;
			//
			output.SupersededBy = input.SupersededBy;
			output.Supersedes = input.Supersedes;

			output.LatestVersionResource = GetResourceFromURL( output.LatestVersion );
			output.PreviousVersionResource = GetResourceFromURL( output.PreviousVersion );
			output.NextVersionResource = GetResourceFromURL( output.NextVersion );

			//the top level object may not be enough. First need to confirm if reference lopps and asmts can have detail pages. => Yes they can
			// the json could be considered a convenience, but should get data from db?
			//24-02-18 mp - TODO - can we just use Entity.HasResource? Much cleaner
			var getAllHasResources = Entity_HasResourceManager.GetAll( output.RowId );
			if ( getAllHasResources != null && getAllHasResources.Count > 0 )
			{
				//TBD if used
				//var tvpForAsmts = getAllHasResources.Where( r => r.EntityTypeId == CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE && r.RelationshipTypeId == Entity_HasResourceManager.HAS_RESOURCE_TYPE_TransferValueFor ).ToList();
				//var tvpFromAsmts = getAllHasResources.Where( r => r.EntityTypeId == CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE && r.RelationshipTypeId == Entity_HasResourceManager.HAS_RESOURCE_TYPE_TransferValueFrom ).ToList();
				////
				//var tvpForCreds = getAllHasResources.Where( r => r.EntityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL && r.RelationshipTypeId == Entity_HasResourceManager.HAS_RESOURCE_TYPE_TransferValueFor ).ToList();
				//var tvpFromCreds = getAllHasResources.Where( r => r.EntityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL && r.RelationshipTypeId == Entity_HasResourceManager.HAS_RESOURCE_TYPE_TransferValueFrom ).ToList();
				////
				//var tvpForLopps = getAllHasResources.Where( r => r.EntityTypeId == CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE && r.RelationshipTypeId == Entity_HasResourceManager.HAS_RESOURCE_TYPE_TransferValueFor ).ToList();
				//var tvpFromLopps = getAllHasResources.Where( r => r.EntityTypeId == CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE && r.RelationshipTypeId == Entity_HasResourceManager.HAS_RESOURCE_TYPE_TransferValueFrom ).ToList();
				//occupations

				//jobs
			}

			if ( !string.IsNullOrWhiteSpace( output.TransferValueFromJson ) )
			{
				output.TransferValueFrom = JsonConvert.DeserializeObject<List<TopLevelObject>>( output.TransferValueFromJson );
				output.TransferValueFromLopp = new List<LearningOpportunityProfile>();
				var lopps = output.TransferValueFrom.Where( s => s.EntityTypeId == 7 || s.EntityTypeId == 36  || s.EntityTypeId == 37 ).ToList();
				foreach( var item in lopps)
				{
					//should exist, but need to handle where it was deleted
					var lopp =LearningOpportunityManager.GetBasic( item.Id );
					if (lopp != null && lopp.Id > 0) 
						output.TransferValueFromLopp.Add( lopp );
				}
				var assmts = output.TransferValueFrom.Where( s => s.EntityTypeId == 3 ).ToList();
				foreach ( var item in assmts )
				{
					var asmt = AssessmentManager.GetBasic( item.Id );
					if ( asmt != null && asmt.Id > 0 )
						output.TransferValueFromAsmt.Add( asmt );
				}

				var creds = output.TransferValueFrom.Where( s => s.EntityTypeId == 1 ).ToList();
				foreach ( var item in creds )
				{
					//no this should not be for detail - especially if full object?
					//the detail was probably getting the TVP = infinate loop
					//output.TransferValueFromCredential.Add( CredentialManager.GetForDetail( item.Id ) );
					var cred = CredentialManager.GetBasic( item.Id );
					if ( cred != null && cred.Id > 0 )
						output.TransferValueFromCredential.Add( cred );
				}

				var comps = output.TransferValueFrom.Where( s => s.EntityTypeId == CodesManager.ENTITY_TYPE_COMPETENCY ).ToList();
				foreach ( var item in comps )
				{
					//??
					var comp = CompetencyFrameworkCompetencyManager.Get( item.Id );
					if ( comp != null && comp.Id > 0 )
						output.TransferValueFromCompetency.Add( comp );
				}
			}			
			//
			if ( !string.IsNullOrWhiteSpace( output.TransferValueForJson ) )
			{
				output.TransferValueFor = JsonConvert.DeserializeObject<List<TopLevelObject>>( output.TransferValueForJson );
				var lopps = output.TransferValueFor.Where( s => s.EntityTypeId == 7 || s.EntityTypeId == 36 || s.EntityTypeId == 37 ).ToList();
				foreach ( var item in lopps )
				{
					var lopp = LearningOpportunityManager.GetBasic( item.Id );
					if ( lopp != null && lopp.Id > 0 )
						output.TransferValueForLopp.Add( lopp );
				}
				var assmts = output.TransferValueFor.Where( s => s.EntityTypeId == 3 ).ToList();
				foreach ( var item in assmts )
				{
					var asmt = AssessmentManager.GetBasic( item.Id );
					if ( asmt != null && asmt.Id > 0 )
						output.TransferValueForAsmt.Add( asmt );
				}

				var creds = output.TransferValueFor.Where( s => s.EntityTypeId == 1 ).ToList();
				foreach ( var item in creds )
				{
					var cred = CredentialManager.GetBasic( item.Id );
					if ( cred != null && cred.Id > 0 )
						output.TransferValueForCredential.Add( cred );
				}
				var comps = output.TransferValueFor.Where( s => s.EntityTypeId == CodesManager.ENTITY_TYPE_COMPETENCY ).ToList();
				foreach ( var item in comps )
				{
					//??
					var comp = CompetencyFrameworkCompetencyManager.Get( item.Id );
					if ( comp != null && comp.Id > 0 )
						output.TransferValueForCompetency.Add( comp );
				}
			}
			//check for presence in transfer intermediaries
			//
			output.HasTransferIntermediary = TransferIntermediaryTransferValueManager.GetAllTransferIntermediariesForTVP( output.Id );
			//this should be a summary level, not the full TVP
			output.DerivedFrom = Entity_TransferValueProfileManager.GetAll( output.RowId );

			//
			List<ProcessProfile> processes = Entity_ProcessProfileManager.GetAll( output.RowId );
			foreach ( ProcessProfile item in processes )
			{
				//if ( item.ProcessTypeId == Entity_ProcessProfileManager.ADMIN_PROCESS_TYPE )
				//	output.AdministrationProcess.Add( item );

				//else 
				if ( item.ProcessTypeId == Entity_ProcessProfileManager.DEV_PROCESS_TYPE )
					output.DevelopmentProcess.Add( item );

				else
				{
					//unexpected
				}
			}

			//with the addition of ProvidesTransferValueFor and ReceivesTransferValueFrom, a resource can reference a TVP. This is to get the resources
			var getAll = Entity_HasResourceManager.GetParentsForTVPResourceId( output.Id );
			foreach(var item in getAll )
            {
                if ( item.EntityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL )
                {
					output.RelatedCredential.Add( item );
                }
				if ( item.EntityTypeId == CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE )
				{
					output.RelatedAssessment.Add( item );
				}
				if ( item.EntityTypeId == CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE )
				{
					output.RelatedLearningOpp.Add( item );
				}
				if ( item.EntityTypeId == CodesManager.ENTITY_TYPE_JOB_PROFILE )
				{
					output.RelatedJob.Add( item );
				}
				if ( item.EntityTypeId == CodesManager.ENTITY_TYPE_OCCUPATIONS_PROFILE )
				{
					output.RelatedOccupation.Add( item );
				}
				if ( item.EntityTypeId == CodesManager.ENTITY_TYPE_COMPETENCY )
				{
					output.RelatedCompetency.Add( item );
				}
			}


		}
		public static TopLevelObject GetResourceFromURL( string someURL)
		{
			if ( string.IsNullOrWhiteSpace( someURL ))
				return null;
			//temp
			var ctid = ExtractCtid( someURL );
			if ( !IsValidCtid( ctid ) )
				return null;

			var resource = EntityManager.EntityCacheGetByCTID( ctid );	
			if ( resource == null || resource.Id == 0 ) 
				return null;
			TopLevelObject output = new TopLevelObject()
			{
				Id = resource.BaseId,
				EntityType = resource.EntityType,
				Name = resource.Name,
				FriendlyName = FormatFriendlyTitle(resource.Name),
				Description = resource.Description,
				SubjectWebpage = resource.SubjectWebpage,
				CTID = ctid,
			};
			return output;
		}
        #endregion

        //public static int Count_ForOwningOrg( string orgCtid )
        //{
        //	int totalRecords = 0;
        //	if ( string.IsNullOrWhiteSpace( orgCtid ) || orgCtid.Trim().Length != 39 )
        //		return totalRecords;

        //	using ( var context = new EntityContext() )
        //	{
        //		var query = ( from entity in context.TransferValueProfile
        //					  join org in context.Organization on entity.OrganizationCTID equals org.CTID
        //					  where entity.OrganizationCTID.ToLower() == orgCtid.ToLower()
        //						   && org.EntityStateId > 1 && entity.EntityStateId == 3
        //					  select new
        //					  {
        //						  entity.CTID
        //					  } );
        //		//until ed frameworks is cleaned up, need to prevent dups != 39
        //		var results = query.Select( s => s.CTID ).Distinct()
        //			.ToList();

        //		if ( results != null && results.Count > 0 )
        //		{
        //			totalRecords = results.Count();

        //		}
        //	}

        //	return totalRecords;
        //}

        public static List<string> Autocomplete( string pFilter, int pageNumber, int pageSize, ref int pTotalRows )
        {
            bool autocomplete = true;
            var results = new List<string>();
            //
            List<ThisResource> list = Search( pFilter, string.Empty, pageNumber, pageSize, ref pTotalRows, autocomplete );
            bool appendingOrgNameToAutocomplete = UtilityManager.GetAppKeyValue( "appendingOrgNameToAutocomplete", false );
            string prevName = string.Empty;
            foreach ( var item in list )
            {
                //note excluding duplicates may have an impact on selected max terms
                if ( string.IsNullOrWhiteSpace( item.OrganizationName ) || !appendingOrgNameToAutocomplete )
                {
                    if ( item.Name.ToLower() != prevName )
                        results.Add( item.Name );
                }
                else
                {
                    results.Add( item.Name + " ('" + item.OrganizationName + "')" );
                }

                prevName = item.Name.ToLower();
            }
            return results;
        }


        public static List<ThisResource> Search( string pFilter, string pOrderBy, int pageNumber, int pageSize, ref int pTotalRows, bool autocomplete = false )
		{
			string connectionString = DBConnectionRO();
			var item = new ThisResource();
			var list = new List<ThisResource>();
			var result = new DataTable();

			using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				c.Open();

				if ( string.IsNullOrEmpty( pFilter ) )
				{
					pFilter = string.Empty;
				}

				using ( SqlCommand command = new SqlCommand( "[TransferValue.ElasticSearch]", c ) )
				{
					command.CommandType = CommandType.StoredProcedure;
					command.Parameters.Add( new SqlParameter( "@Filter", pFilter ) );
					command.Parameters.Add( new SqlParameter( "@SortOrder", pOrderBy ) );
					command.Parameters.Add( new SqlParameter( "@StartPageIndex", pageNumber ) );
					command.Parameters.Add( new SqlParameter( "@PageSize", pageSize ) );
					command.CommandTimeout = 300;

					SqlParameter totalRows = new SqlParameter( "@TotalRows", pTotalRows );
					totalRows.Direction = ParameterDirection.Output;
					command.Parameters.Add( totalRows );

					using ( SqlDataAdapter adapter = new SqlDataAdapter() )
					{
						adapter.SelectCommand = command;
						adapter.Fill( result );
					}
					string rows = command.Parameters[ 4 ].Value.ToString();
					try
					{
						pTotalRows = Int32.Parse( rows );
					}
					catch
					{
						pTotalRows = 0;
					}
				}

				foreach ( DataRow dr in result.Rows )
				{
					item = new ThisResource();
					item.Id = GetRowColumn( dr, "Id", 0 );
					item.CTID = GetRowColumn( dr, "CTID", string.Empty );
					item.Name = GetRowColumn( dr, "Name", "???" );
                    //for autocomplete, only need name
                    if ( autocomplete )
                    {
                        list.Add( item );
                        continue;
                    }
                    item.PrimaryOrganizationName = GetRowColumn( dr, "OrganizationName", string.Empty );
					item.Description = GetRowColumn( dr, "Description", string.Empty );
					item.SubjectWebpage = GetRowColumn( dr, "SubjectWebpage", string.Empty );
					//item.CodedNotation = GetRowColumn( dr, "CodedNotation", string.Empty );
					item.SearchTagName = item.Name;
					item.StartDate = GetRowColumn( dr, "StartDate", string.Empty );
					item.EndDate = GetRowColumn( dr, "EndDate", string.Empty );
					if ( !string.IsNullOrWhiteSpace(item.StartDate ) && !string.IsNullOrWhiteSpace(item.EndDate))
					{
						item.SearchTagName += $" (Start Date: {item.StartDate}, End Date: {item.EndDate})";
					}
					item.IdentifierJson = GetRowColumn( dr, "IdentifierJson", string.Empty );
					item.TransferValueJson = GetRowColumn( dr, "TransferValueJson", string.Empty );
					item.TransferValueFromJson = GetRowColumn( dr, "TransferValueFromJson", string.Empty );
					item.TransferValueForJson = GetRowColumn( dr, "TransferValueForJson", string.Empty );
					//
					item.EntityLastUpdated = GetRowColumn( dr, "EntityLastUpdated", System.DateTime.MinValue );
					item.Created = GetRowColumn( dr, "Created", System.DateTime.MinValue );
					//item.LastUpdated = GetRowColumn( dr, "LastUpdated", System.DateTime.MinValue );

					list.Add( item );
				}

				return list;

			}
		} //

		/// <summary>
		/// Search for TransferIntermediary
		/// </summary>
		/// <param name="pFilter"></param>
		/// <param name="pOrderBy"></param>
		/// <param name="pageNumber"></param>
		/// <param name="pageSize"></param>
		/// <param name="pTotalRows"></param>
		/// <returns></returns>
		public static List<ThisResource> DoTransferIntermediarySearch( string pFilter, string pOrderBy, int pageNumber, int pageSize, ref int pTotalRows )
		{
			string connectionString = DBConnectionRO();
			var item = new ThisResource();
			var list = new List<ThisResource>();
			var result = new DataTable();

			using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				c.Open();

				if ( string.IsNullOrEmpty( pFilter ) )
				{
					pFilter = string.Empty;
				}

				using ( SqlCommand command = new SqlCommand( "[TransferValue.ElasticSearch]", c ) )
				{
					command.CommandType = CommandType.StoredProcedure;
					command.Parameters.Add( new SqlParameter( "@Filter", pFilter ) );
					command.Parameters.Add( new SqlParameter( "@SortOrder", pOrderBy ) );
					command.Parameters.Add( new SqlParameter( "@StartPageIndex", pageNumber ) );
					command.Parameters.Add( new SqlParameter( "@PageSize", pageSize ) );

					SqlParameter totalRows = new SqlParameter( "@TotalRows", pTotalRows );
					totalRows.Direction = ParameterDirection.Output;
					command.Parameters.Add( totalRows );

					using ( SqlDataAdapter adapter = new SqlDataAdapter() )
					{
						adapter.SelectCommand = command;
						adapter.Fill( result );
					}
					string rows = command.Parameters[4].Value.ToString();
					try
					{
						pTotalRows = Int32.Parse( rows );
					}
					catch
					{
						pTotalRows = 0;
					}
				}

				foreach ( DataRow dr in result.Rows )
				{
					item = new ThisResource();
					item.Id = GetRowColumn( dr, "Id", 0 );
					item.CTID = GetRowColumn( dr, "CTID", string.Empty );
					item.Name = GetRowColumn( dr, "Name", "???" );
					item.PrimaryOrganizationName = GetRowColumn( dr, "OrganizationName", string.Empty );
					item.Description = GetRowColumn( dr, "Description", string.Empty );
					item.SubjectWebpage = GetRowColumn( dr, "SubjectWebpage", string.Empty );
					//item.CodedNotation = GetRowColumn( dr, "CodedNotation", string.Empty );
					item.StartDate = GetRowColumn( dr, "StartDate", string.Empty );
					item.EndDate = GetRowColumn( dr, "EndDate", string.Empty );

					item.IdentifierJson = GetRowColumn( dr, "IdentifierJson", string.Empty );
					item.TransferValueJson = GetRowColumn( dr, "TransferValueJson", string.Empty );
					item.TransferValueFromJson = GetRowColumn( dr, "TransferValueFromJson", string.Empty );
					item.TransferValueForJson = GetRowColumn( dr, "TransferValueForJson", string.Empty );
					//
					item.EntityLastUpdated = GetRowColumn( dr, "EntityLastUpdated", System.DateTime.MinValue );
					item.Created = GetRowColumn( dr, "Created", System.DateTime.MinValue );
					//item.LastUpdated = GetRowColumn( dr, "LastUpdated", System.DateTime.MinValue );

					list.Add( item );
				}

				return list;

			}
		} //

	}


}

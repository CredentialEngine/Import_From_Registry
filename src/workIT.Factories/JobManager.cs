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
using DBEntity = workIT.Data.Tables.JobProfile;
using EntityContext = workIT.Data.Tables.workITEntities;
using ReferenceFrameworkItemsManager = workIT.Factories.Reference_FrameworkItemManager;
using ThisResource = workIT.Models.Common.Job;
namespace workIT.Factories
{
    public class JobManager : BaseFactory
	{
		static readonly string thisClassName = "JobManager";
		static string EntityType = "Job";
		static int EntityTypeId = CodesManager.ENTITY_TYPE_JOB_PROFILE;
        static string Entity_Label = "Job";
        static string Entities_Label = "Jobs";


        #region Job - persistance ==================
        /// <summary>
        /// Update a Job
        /// - base only, caller will handle parts?
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public bool Save( ThisResource entity, ref SaveStatus status )
		{
			bool isValid = true;
			int count = 0;
			try
			{
				using ( var context = new EntityContext() )
				{
					if ( ValidateProfile( entity, ref status ) == false )
						return false;

					if ( entity.Id > 0 )
					{
						//TODO - consider if necessary, or interferes with anything
						context.Configuration.LazyLoadingEnabled = false;
						DBEntity efEntity = context.JobProfile
								.SingleOrDefault( s => s.Id == entity.Id );

						if ( efEntity != null && efEntity.Id > 0 )
						{
							//fill in fields that may not be in entity
							entity.RowId = efEntity.RowId;

							MapToDB( entity, efEntity );

							//19-05-21 mp - should add a check for an update where currently is deleted
							if ( ( efEntity.EntityStateId ) == 0 )
							{
								SiteActivity sa = new SiteActivity()
								{
									ActivityType = "JobProfile",
									Activity = "Import",
									Event = "Reactivate",
									Comment = string.Format( "Job had been marked as deleted, and was reactivted by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ),
									ActivityObjectId = entity.Id
								};
								new ActivityManager().SiteActivityAdd( sa );
							}
                            //assume and validate, that if we get here we have a full record
                            if ( efEntity.EntityStateId != 2 )
                                efEntity.EntityStateId = 3;
                            entity.EntityStateId = efEntity.EntityStateId;

                            if ( IsValidDate( status.EnvelopeCreatedDate ) && status.LocalCreatedDate < efEntity.Created )
							{
								efEntity.Created = status.LocalCreatedDate;
							}
							if ( IsValidDate( status.EnvelopeUpdatedDate ) && status.LocalUpdatedDate != efEntity.LastUpdated )
							{
								efEntity.LastUpdated = status.LocalUpdatedDate;
							}
							if ( HasStateChanged( context ) )
							{
								if ( IsValidDate( status.EnvelopeUpdatedDate ) )
									efEntity.LastUpdated = status.LocalUpdatedDate;
								else
									efEntity.LastUpdated = DateTime.Now;
								//NOTE efEntity.EntityStateId is set to 0 in delete method )
								count = context.SaveChanges();
								//can be zero if no data changed
								if ( count >= 0 )
								{
									entity.LastUpdated = efEntity.LastUpdated.Value;
									UpdateEntityCache( entity, ref status );
									isValid = true;
								}
								else
								{
									//?no info on error
									isValid = false;
									string message = string.Format( thisClassName + ".Save Failed", "Attempted to update a Job. The process appeared to not work, but was not an exception, so we have no message, or no clue. Job: {0}, Id: {1}", entity.Name, entity.Id );
									status.AddError( "Error - the update was not successful. " + message );
									EmailManager.NotifyAdmin( thisClassName + ".Save Failed Failed", message );
								}

							}
							else
							{
								//update entity.LastUpdated - assuming there has to have been some change in related data
								new EntityManager().UpdateModifiedDate( entity.RowId, ref status, efEntity.LastUpdated );
							}
							if ( isValid )
							{
								if ( !UpdateParts( entity, ref status ) )
									isValid = false;

								SiteActivity sa = new SiteActivity()
								{
									ActivityType = "JobProfile",
									Activity = "Import",
									Event = "Update",
									Comment = string.Format( "Job was updated by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ),
									ActivityObjectId = entity.Id
								};
								new ActivityManager().SiteActivityAdd( sa );
							}
						}
						else
						{
							status.AddError( "Error - update failed, as record was not found." );
						}
					}
					else
					{
						//add
						int newId = Add( entity, ref status );
						if ( newId == 0 || status.HasErrors )
							isValid = false;
					}
				}
			}
			catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
			{
				string message = HandleDBValidationError( dbex, thisClassName + string.Format( ".Save. id: {0}, Name: {1}", entity.Id, entity.Name ), "Job" );
				status.AddError( thisClassName + ".Save(). Error - the save was not successful. " + message );
			}
			catch ( Exception ex )
			{
				string message = FormatExceptions( ex );
				LoggingHelper.LogError( ex, thisClassName + string.Format( ".Save. id: {0}, Name: {1}", entity.Id, entity.Name ) );
				status.AddError( thisClassName + ".Save(). Error - the save was not successful. " + message );
				isValid = false;
			}


			return isValid;
		}

		/// <summary>
		/// add a Job
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		private int Add( ThisResource entity, ref SaveStatus status )
		{
			DBEntity efEntity = new DBEntity();
			using ( var context = new EntityContext() )
			{
				try
				{
					MapToDB( entity, efEntity );

					if ( IsValidGuid( entity.RowId ) )
						efEntity.RowId = entity.RowId;
					else
						efEntity.RowId = Guid.NewGuid();
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
					context.JobProfile.Add( efEntity );

					// submit the change to database
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						entity.RowId = efEntity.RowId;
						entity.Created = efEntity.Created.Value;
						entity.LastUpdated = efEntity.LastUpdated.Value;
						entity.Id = efEntity.Id;
						UpdateEntityCache( entity, ref status );
						//add log entry
						SiteActivity sa = new SiteActivity()
						{
							ActivityType = "JobProfile",
							Activity = "Import",
							Event = "Add",
							Comment = string.Format( "Full Job was added by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ),
							ActivityObjectId = entity.Id
						};
						new ActivityManager().SiteActivityAdd( sa );
						if ( UpdateParts( entity, ref status ) == false )
						{
						}

						return efEntity.Id;
					}
					else
					{
						//?no info on error

						string message = thisClassName + string.Format( ". Add Failed", "Attempted to add a Job. The process appeared to not work, but was not an exception, so we have no message, or no clue. Job: {0}, ctid: {1}", entity.Name, entity.CTID );
						status.AddError( thisClassName + ". Error - the add was not successful. " + message );
						EmailManager.NotifyAdmin( "JobManager. Add Failed", message );
					}
				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", "Job" );
					status.AddError( thisClassName + ".Add(). Error - the save was not successful. " + message );

					LoggingHelper.LogError(dbex, message);
				}
				catch ( Exception ex )
				{
					string message = FormatExceptions( ex );
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Add(), Name: {0}\r\n", efEntity.Name ) );
					status.AddError( thisClassName + ".Add(). Error - the save was not successful. \r\n" + message );
				}
			}

			return efEntity.Id;
		}
		public int AddReference( ThisResource entity, ref SaveStatus status )
		{
			DBEntity efEntity = new DBEntity();
			try
			{
				using ( var context = new EntityContext() )
				{
					if ( entity == null || string.IsNullOrWhiteSpace( entity.Name ))
					{
						status.AddError( thisClassName + ". AddBaseReference() The Job is incomplete" );
						return 0;
					}

					//only add DB required properties
					//NOTE - an entity will be created via trigger
					//23-10-23 - ahh - now reference resources can have a lot of info. maybe just use 
					MapToDB( entity, efEntity );
					efEntity.EntityStateId = entity.EntityStateId = 2;
                    

					//
					if ( IsValidGuid( entity.RowId ) )
						efEntity.RowId = entity.RowId;
					else
						efEntity.RowId = Guid.NewGuid();
					//set to return, just in case
					entity.RowId = efEntity.RowId;
					//

					//
					efEntity.Created = System.DateTime.Now;
					efEntity.LastUpdated = System.DateTime.Now;

					context.JobProfile.Add( efEntity );
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						entity.Id = efEntity.Id;
						entity.RowId = efEntity.RowId;
						entity.Created = efEntity.Created.Value;
						entity.LastUpdated = efEntity.LastUpdated.Value;
						UpdateEntityCache( entity, ref status );
						UpdateParts( entity, ref status );

						return efEntity.Id;
					}

					status.AddError( thisClassName + ". AddBaseReference() Error - the save was not successful, but no message provided. " );
				}
			}
			catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
			{
				status.AddError( HandleDBValidationError( dbex, thisClassName + ".AddBaseReference() ", "Job" ) );
				LoggingHelper.LogError( dbex, thisClassName + string.Format( ".Add(), Name: {0}", entity.Name) );
			}
			catch ( Exception ex )
			{
				string message = FormatExceptions( ex );
				LoggingHelper.LogError( ex, thisClassName + string.Format( ".AddBaseReference. Name:  {0}, SubjectWebpage: {1}", entity.Name, entity.SubjectWebpage ) );
				status.AddError( thisClassName + ". AddBaseReference() Error - the save was not successful. " + message );

			}
			return 0;
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
					var entity = GetMinimumByCtid( ctid );
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

					context.JobProfile.Add( efEntity );
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
				EntityTypeId = EntityTypeId,
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
		public bool ValidateProfile( ThisResource profile, ref SaveStatus status )
		{
			status.HasSectionErrors = false;


			if ( string.IsNullOrWhiteSpace( profile.Description ) )
			{
				//status.AddWarning( "An Job Description must be entered" );
			}


			if ( string.IsNullOrWhiteSpace( profile.SubjectWebpage ) )
			{
				//status.AddWarning( "Error - A Subject Webpage name must be entered" );
			}
			else if ( !IsUrlValid( profile.SubjectWebpage, ref commonStatusMessage ) )
			{
				status.AddWarning( "The Job Subject Webpage is invalid. " + commonStatusMessage );
			}
			var defStatus = CodesManager.GetLifeCycleStatus( CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS_ACTIVE );
			if ( profile.LifeCycleStatusType == null || profile.LifeCycleStatusType.Items == null || profile.LifeCycleStatusType.Items.Count == 0 )
			{
				profile.LifeCycleStatusTypeId = defStatus.Id;
			}
			else
			{
				var schemaName = profile.LifeCycleStatusType.GetFirstItem().SchemaName;
				CodeItem ci = CodesManager.GetLifeCycleStatus( CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, schemaName );
				if ( ci == null || ci.Id < 1 )
				{
					//while this should never happen, should have a default
					status.AddError( string.Format( "A valid LifeCycleStatusType must be included. Invalid: {0}", schemaName ) );
					profile.LifeCycleStatusTypeId = defStatus.Id;
				}
				else
					profile.LifeCycleStatusTypeId = ci.Id;
			}


			return status.WasSectionValid;
		}


		/// <summary>
		/// Delete an Job, and related Entity
		/// </summary>
		/// <param name="id"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool Delete( int id, ref string statusMessage )
		{
			bool isValid = false;
			if ( id == 0 )
			{
				statusMessage = "Error - missing an identifier for the Job";
				return false;
			}

			using ( var context = new EntityContext() )
			{
				try
				{
					context.Configuration.LazyLoadingEnabled = false;
					DBEntity efEntity = context.JobProfile
								.SingleOrDefault( s => s.Id == id );

					if ( efEntity != null && efEntity.Id > 0 )
					{

						//need to remove from Entity.
						//could use a pre-delete trigger?
						//what about roles

						context.JobProfile.Remove( efEntity );
						int count = context.SaveChanges();
						if ( count > 0 )
						{
							isValid = true;
							//add pending delete request 
							List<String> messages = new List<string>();
							new SearchPendingReindexManager().AddDeleteRequest( CodesManager.ENTITY_TYPE_OCCUPATIONS_PROFILE, id, ref messages );
							//
							//new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, orgId, 1, ref messages );
							//also check for any relationships
							//new Entity_AgentRelationshipManager().ReindexAgentForDeletedArtifact( orgUid );
						}
					}
					else
					{
						statusMessage = "Error - Job_Delete failed, as record was not found.";
					}
				}
				catch ( Exception ex )
				{
					statusMessage = FormatExceptions( ex );
					LoggingHelper.LogError( ex, thisClassName + ".Job_Delete()" );

					if ( statusMessage.ToLower().IndexOf( "the delete statement conflicted with the reference constraint" ) > -1 )
					{
						statusMessage = "Error: this Job cannot be deleted as it is being referenced by other items, such as roles or credentials. These associations must be removed before this Job can be deleted.";
					}
				}
			}
			return isValid;
		}

		public bool Delete( string ctid, ref string statusMessage )
		{
			bool isValid = true;
			if ( string.IsNullOrWhiteSpace( ctid ) )
			{
				statusMessage = thisClassName + ".Delete() Error - a valid CTID must be provided";
				return false;
			}
			if ( string.IsNullOrWhiteSpace( ctid ) )
				ctid = "SKIP ME";

			using ( var context = new EntityContext() )
			{
				try
				{
					context.Configuration.LazyLoadingEnabled = false;
					DBEntity efEntity = context.JobProfile
								.FirstOrDefault( s => ( s.CTID == ctid )
								);

					if ( efEntity != null && efEntity.Id > 0 )
					{
						Guid rowId = efEntity.RowId;

						//need to remove from Entity.
						//-using before delete trigger - verify won't have RI issues
						string msg = string.Format( " Job. Id: {0}, Name: {1}, Ctid: {2}.", efEntity.Id, efEntity.Name, efEntity.CTID );
						//18-04-05 mparsons - change to set inactive, and notify - seems to have been some incorrect deletes
						//context.JobProfile.Remove( efEntity );
						efEntity.EntityStateId = 0;
						efEntity.LastUpdated = System.DateTime.Now;
						int count = context.SaveChanges();
						if ( count > 0 )
						{
							new ActivityManager().SiteActivityAdd( new SiteActivity()
							{
								ActivityType = "JobProfile",
								Activity = "Import",
								Event = "Delete",
								Comment = msg,
								ActivityObjectId = efEntity.Id
							} );
							isValid = true;
							//delete cache
							new EntityManager().EntityCacheDelete( rowId, ref statusMessage );
							//add pending request 
							List<String> messages = new List<string>();
							new SearchPendingReindexManager().AddDeleteRequest( CodesManager.ENTITY_TYPE_OCCUPATIONS_PROFILE, efEntity.Id, ref messages );
							//mark owning org for updates (actually should be covered by ReindexAgentForDeletedArtifact
							//new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, orgId, 1, ref messages );

							//delete all relationships
							workIT.Models.SaveStatus status = new SaveStatus();
							Entity_AgentRelationshipManager earmgr = new Entity_AgentRelationshipManager();
							earmgr.DeleteAll( rowId, ref status );
							//also check for any relationships
							//There could be other orgs from relationships to be reindexed as well!

							//also check for any relationships
							//new Entity_AgentRelationshipManager().ReindexAgentForDeletedArtifact( orgUid );
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
					isValid = false;
					statusMessage = FormatExceptions( ex );
					if ( statusMessage.ToLower().IndexOf( "the delete statement conflicted with the reference constraint" ) > -1 )
					{
						statusMessage = "Error: this Job cannot be deleted as it is being referenced by other items, such as roles or credentials. These associations must be removed before this Job can be deleted.";
					}
				}
			}
			return isValid;
		}

		#region Job properties ===================
		public bool UpdateParts( ThisResource resource, ref SaveStatus status )
		{
			bool isAllValid = true;
			Entity relatedEntity = EntityManager.GetEntity( resource.RowId );
			if ( relatedEntity == null || relatedEntity.Id == 0 )
			{
				status.AddError( "Error - the related Entity was not found." );
				return false;
			}

			if ( UpdateProperties( resource, relatedEntity, ref status ) == false )
			{
				isAllValid = false;
			}
			Entity_AgentRelationshipManager eamgr = new Entity_AgentRelationshipManager();
			eamgr.SaveList( relatedEntity.Id, Entity_AgentRelationshipManager.ROLE_TYPE_OFFERED_BY, resource.OfferedByList, ref status );
			eamgr.SaveList( relatedEntity.Id, Entity_AgentRelationshipManager.ROLE_TYPE_PUBLISHEDBY, resource.PublishedBy, ref status );

			Entity_ReferenceFrameworkManager erfm = new Entity_ReferenceFrameworkManager();
            erfm.DeleteAll( relatedEntity, ref status );

            if ( erfm.SaveList( relatedEntity.Id, CodesManager.PROPERTY_CATEGORY_SOC, resource.Occupations, ref status ) == false )
                isAllValid = false;
            if ( erfm.SaveList( relatedEntity.Id, CodesManager.PROPERTY_CATEGORY_NAICS, resource.Industries, ref status ) == false )
                isAllValid = false;

            //ConditionProfile =======================================
            Entity_ConditionProfileManager emanager = new Entity_ConditionProfileManager();
            //emanager.DeleteAll( relatedEntity, ref status );
            emanager.SaveList( resource.Requires, Entity_ConditionProfileManager.ConnectionProfileType_Requirement, resource.RowId, ref status );

            //Entity_HasResource
            var eHasResourcesMgr = new Entity_HasResourceManager();
			//no, doing delete in save method
			//Ohh, this would be fine if all entity types are known in advance. With KSA, there can be a mix
			eHasResourcesMgr.DeleteAll( relatedEntity, ref status );

			if ( eHasResourcesMgr.SaveList( relatedEntity, CodesManager.ENTITY_TYPE_WORKROLE_PROFILE, resource.HasWorkRoleIds, ref status, Entity_HasResourceManager.HAS_RESOURCE_TYPE_HasResource ) == false )
				isAllValid = false;
            if ( eHasResourcesMgr.SaveList( relatedEntity, CodesManager.ENTITY_TYPE_OCCUPATIONS_PROFILE, resource.HasOccupationIds, ref status, Entity_HasResourceManager.HAS_RESOURCE_TYPE_HasResource ) == false )
                isAllValid = false;
            if ( eHasResourcesMgr.SaveList( relatedEntity, CodesManager.ENTITY_TYPE_TASK_PROFILE, resource.HasTaskIds, ref status, Entity_HasResourceManager.HAS_RESOURCE_TYPE_HasResource ) == false )
                isAllValid = false;

			//KSA
			//The entityTypeId from the list will be used with Entity.HasResource
			if ( eHasResourcesMgr.SaveList( relatedEntity, resource.AbilityEmbodied, ref status, Entity_HasResourceManager.HAS_RESOURCE_TYPE_AbilityEmbodied ) == false )
				isAllValid = false;
			if ( eHasResourcesMgr.SaveList( relatedEntity, resource.KnowledgeEmbodied, ref status, Entity_HasResourceManager.HAS_RESOURCE_TYPE_KnowledgeEmbodied ) == false )
				isAllValid = false;
			if ( eHasResourcesMgr.SaveList( relatedEntity, resource.SkillEmbodied, ref status, Entity_HasResourceManager.HAS_RESOURCE_TYPE_SkillEmbodied ) == false )
				isAllValid = false;

			//concepts
			//- Hmm should these be treated as hasResource? Rather than concrete concepts/codes.PropertyValue?
			//	so far YES
			if ( eHasResourcesMgr.SaveList( relatedEntity, resource.PerformanceLevelType, ref status, Entity_HasResourceManager.HAS_RESOURCE_TYPE_PerformanceLevelType ) == false )
				isAllValid = false;
			if ( eHasResourcesMgr.SaveList( relatedEntity, resource.PhysicalCapabilityType, ref status, Entity_HasResourceManager.HAS_RESOURCE_TYPE_PhysicalCapabilityType ) == false )
				isAllValid = false;
			if ( eHasResourcesMgr.SaveList( relatedEntity, resource.SensoryCapabilityType, ref status, Entity_HasResourceManager.HAS_RESOURCE_TYPE_SensoryCapabiltyType ) == false )
				isAllValid = false;
			if ( eHasResourcesMgr.SaveList( relatedEntity, resource.EnvironmentalHazardType, ref status, Entity_HasResourceManager.HAS_RESOURCE_TYPE_EnvironmentalHazardType ) == false )
				isAllValid = false;

			if ( eHasResourcesMgr.SaveList( relatedEntity, resource.Classification, ref status, Entity_HasResourceManager.HAS_RESOURCE_TYPE_Classification ) == false )
				isAllValid = false;

			// Transfer Value 
			if ( eHasResourcesMgr.SaveList( relatedEntity, CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, resource.ProvidesTVForIds, ref status, Entity_HasResourceManager.HAS_RESOURCE_TYPE_ProvidesTransferValueFor ) == false )
				isAllValid = false;
			if ( eHasResourcesMgr.SaveList( relatedEntity, CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, resource.ReceivesTVFromIds, ref status, Entity_HasResourceManager.HAS_RESOURCE_TYPE_ReceivesTransferValueFrom ) == false )
				isAllValid = false;
			//
			if ( eHasResourcesMgr.SaveList( relatedEntity, CodesManager.ENTITY_TYPE_RUBRIC, resource.HasRubricIds, ref status, Entity_HasResourceManager.HAS_RESOURCE_TYPE_HasResource ) == false )
				isAllValid = false;
			var ehssMgr = new Entity_HasSupportServiceManager();
            ehssMgr.Update( resource.HasSupportServiceIds, relatedEntity, ref status );

			//hasRubric - TBD

			return isAllValid;
		}


		public bool UpdateProperties( ThisResource entity, Entity relatedEntity, ref SaveStatus status )
		{
			bool isAllValid = true;
			EntityPropertyManager mgr = new EntityPropertyManager();
			//first clear all propertiesd
			//mgr.DeleteAll( relatedEntity, ref status );
			//Entity_ReferenceManager erm = new Entity_ReferenceManager();
			//already did a deleteAll in UpdateParts

			return isAllValid;
		}




		#endregion

		#endregion

		#region == Retrieval =======================
		public static ThisResource GetMinimumByCtid( string ctid )
		{
			ThisResource entity = new ThisResource();
			using ( var context = new EntityContext() )
			{
				DBEntity from = context.JobProfile
						.FirstOrDefault( s => s.CTID.ToLower() == ctid.ToLower() );

				if ( from != null && from.Id > 0 )
				{
					entity.RowId = from.RowId;
					entity.Id = from.Id;
					entity.EntityStateId = from.EntityStateId;
					entity.Name = from.Name;
					entity.Description = from.Description;
					entity.SubjectWebpage = from.SubjectWebpage;
					entity.CTID = from.CTID;
				}
			}

			return entity;
		}
		public static ThisResource Get( Guid profileUid )
		{
			ThisResource entity = new ThisResource();
			if ( !IsGuidValid( entity.RowId ) )
				return null;
			try
			{
				using ( var context = new EntityContext() )
				{
					DBEntity item = context.JobProfile
							.SingleOrDefault( s => s.RowId == profileUid );

					if ( item != null && item.Id > 0 )
					{
						MapFromDB( item, entity, false );
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".Get" );
			}
			return entity;
		}//
		public static ThisResource GetBasic( int id )
		{
			ThisResource entity = new ThisResource();
			using ( var context = new EntityContext() )
			{
				DBEntity item = context.JobProfile
						.SingleOrDefault( s => s.Id == id );

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity, false );
				}
			}

			return entity;
		}

		
		public static ThisResource GetForDetail( int id )
		{
			ThisResource entity = new ThisResource();
			using ( var context = new EntityContext() )
			{
				DBEntity item = context.JobProfile
						.SingleOrDefault( s => s.Id == id );

				if ( item != null && item.Id > 0 )
				{
					//check for virtual deletes
					if ( item.EntityStateId == 0 )
					{
						LoggingHelper.DoTrace( 1, string.Format( thisClassName + " Error: encountered request for detail for a deleted record. Name: {0}, CTID:{1}", item.Name, item.CTID ) );
						entity.Name = "Record was not found.";
						entity.CTID = item.CTID;
						return entity;
					}

					MapFromDB( item, entity,
							true //includingProperties
							);
				}
			}

			return entity;
		}

        /// <summary>
        /// typically used with blank node resolution
        /// </summary>
        /// <param name="name"></param>
        /// <param name="swp"></param>
        /// <returns></returns>
        public static ThisResource GetByName_SubjectWebpage( string name, string swp )
        {
            var entity = new ThisResource();
            if ( string.IsNullOrWhiteSpace( swp ) )
                return null;
            if ( swp.IndexOf( "//" ) == -1 )
                return null;
            bool hasHttps = false;
            if ( swp.ToLower().IndexOf( "https:" ) > -1 )
                hasHttps = true;

            //swp = swp.Substring( swp.IndexOf( "//" ) + 2 );
            //swp = swp.ToLower().TrimEnd( '/' );
            var host = new Uri( swp ).Host;
            var domain = host.Substring( host.LastIndexOf( '.', host.LastIndexOf( '.' ) - 1 ) + 1 );
            using ( var context = new EntityContext() )
            {
                //s.Name.ToLower() == name.ToLower() && 
                context.Configuration.LazyLoadingEnabled = false;
                var list = context.JobProfile
                        .Where( s => s.SubjectWebpage.ToLower().Contains( domain ) && s.EntityStateId > 1 )
                        .OrderByDescending( s => s.EntityStateId )
                        .ThenBy( s => s.Name )
                        .ToList();
                int cntr = 0;

                ActivityManager amgr = new ActivityManager();

                foreach ( var from in list )
                {
                    cntr++;
                    //any way to check further?
                    //the full org will be returned first
                    //may want a secondary check and send notifications if additional full orgs found, or even if multiples are found.
                    if ( from.Name.ToLower().Contains( name.ToLower() )
                    || name.ToLower().Contains( from.Name.ToLower() )
                    )
                    {
                        //OK, take me
                        if ( cntr == 1 || entity.Id == 0 )
                        {
                            //hmmm if input was https and found http, and a reference, should update to https!
                            if ( hasHttps && from.SubjectWebpage.StartsWith( "http:" ) )
                            {

                            }
                            //
                            MapFromDB( from, entity, false );
                        }
                        else
                        {
                            if ( from.EntityStateId == 3 )
                            {
                                //could log warning conditions to activity log, and then report out at end of an import?
                                amgr.SiteActivityAdd( new SiteActivity()
                                {
                                    ActivityType = "System",
                                    Activity = "Import",
                                    Event = $"{EntityType} Reference Check",
                                    Comment = $"{Entity_Label} Get by Name and subject webpage. Found additional full {EntityType} for name: {name}, swp: {swp}. First {EntityType}: {entity.Name} ({entity.Id})"
                                } );

                            }
                            MapFromDB( from, entity, false );
                            break;
                        }
                    }
                }
            }

            return entity;
        }

		
        public static ThisResource GetByNameAndDescription( string name, string description )
        {
            var entity = new ThisResource();
            using ( var context = new EntityContext() )
            {
                context.Configuration.LazyLoadingEnabled = false;
                var list = context.JobProfile
                        .Where( s => s.Name.ToLower() == name.ToLower() && s.EntityStateId > 1 
							&& !string.IsNullOrWhiteSpace(s.Description) )
                        .OrderByDescending( s => s.EntityStateId )
                        .ThenBy( s => s.Name )
                        .ToList();
                int cntr = 0;
                foreach ( var from in list )
                {
                    cntr++;
					//if only one take it. 
					if ( list.Count == 1 )
					{
						MapFromDB( from, entity, false );
						break;
					}
                    //just start with an exact match on the desc. The key is having one
                    if ( from.Description.ToLower() == description.ToLower() )
					{
                        MapFromDB( from, entity, false );
                        break;
                    }
                }
            }

            return entity;
        }
        /// <summary>
        /// look up by name for blank node resolution.
        /// Need an exact match with this limited data. Should be the last resort
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static ThisResource GetByName( string name )
        {
            var entity = new ThisResource();
            using ( var context = new EntityContext() )
            {
                //s.Name.ToLower() == name.ToLower() && 
                context.Configuration.LazyLoadingEnabled = false;
                var list = context.JobProfile
                        .Where( s => s.Name.ToLower() == name.ToLower() && s.EntityStateId > 1 )
                        .OrderByDescending( s => s.EntityStateId )
                        .ThenBy( s => s.Name )
                        .ToList();
                int cntr = 0;
                foreach ( var from in list )
                {
                    cntr++;
                    //just take first one
                    MapFromDB( from, entity, false );

                    break;
                }
            }

            return entity;
        }

        public static int Count_ForOwningOrg( Guid orgUid )
        {
            int totalRecords = 0;

            using ( var context = new EntityContext() )
            {
                var results = context.JobProfile
                            .Where( s => s.PrimaryAgentUID == orgUid && s.EntityStateId == 3 )
                            .ToList();

                if ( results != null && results.Count > 0 )
                {
                    totalRecords = results.Count();
                }
            }
            return totalRecords;
        }
        public static List<object> Autocomplete( string pFilter, int pageNumber, int pageSize, ref int pTotalRows )
		{
			bool autocomplete = true;
			List<object> results = new List<object>();
			List<string> competencyList = new List<string>();
			//ref competencyList, 
			List<ThisResource> list = Search( pFilter, string.Empty, pageNumber, pageSize, ref pTotalRows, autocomplete );
			bool appendingOrgNameToAutocomplete = UtilityManager.GetAppKeyValue( "appendingOrgNameToAutocomplete", false );
			string prevName = string.Empty;
			foreach ( var item in list )
			{
				//note excluding duplicates may have an impact on selected max terms
				if ( string.IsNullOrWhiteSpace( item.OrganizationName )
	|| !appendingOrgNameToAutocomplete )
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
			ThisResource item = new ThisResource();
			List<ThisResource> list = new List<ThisResource>();
			var result = new DataTable();
			string temp = string.Empty;
			string org = string.Empty;
			int orgId = 0;

			using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				c.Open();

				if ( string.IsNullOrEmpty( pFilter ) )
				{
					pFilter = string.Empty;
				}

				using ( SqlCommand command = new SqlCommand( "[Job_Search]", c ) )
				{
					command.CommandType = CommandType.StoredProcedure;
					command.Parameters.Add( new SqlParameter( "@Filter", pFilter ) );
					command.Parameters.Add( new SqlParameter( "@SortOrder", pOrderBy ) );
					command.Parameters.Add( new SqlParameter( "@StartPageIndex", pageNumber ) );
					command.Parameters.Add( new SqlParameter( "@PageSize", pageSize ) );

					SqlParameter totalRows = new SqlParameter( "@TotalRows", pTotalRows );
					totalRows.Direction = ParameterDirection.Output;
					command.Parameters.Add( totalRows );
					try
					{
						using ( SqlDataAdapter adapter = new SqlDataAdapter() )
						{
							adapter.SelectCommand = command;
							adapter.Fill( result );
						}
						string rows = command.Parameters[ command.Parameters.Count - 1 ].Value.ToString();

						pTotalRows = Int32.Parse( rows );
					}
					catch ( Exception ex )
					{
						pTotalRows = 0;
						LoggingHelper.LogError( ex, thisClassName + string.Format( ".Search() - Execute proc, Message: {0} \r\n Filter: {1} \r\n", ex.Message, pFilter ) );

						item = new Job();
						item.Name = "Unexpected error encountered. System administration has been notified. Please try again later. ";
						item.Description = ex.Message;

						list.Add( item );
						return list;
					}
				}

				foreach ( DataRow dr in result.Rows )
				{
					item = new ThisResource();
					item.Id = GetRowColumn( dr, "Id", 0 );
					item.Name = GetRowColumn( dr, "Name", "missing" );
					item.FriendlyName = FormatFriendlyTitle( item.Name );
					//for autocomplete, only need name
					if ( autocomplete )
					{
						list.Add( item );
						continue;
					}

					item.Description = GetRowColumn( dr, "Description", string.Empty );
					string rowId = GetRowColumn( dr, "RowId" );
					item.RowId = new Guid( rowId );

					item.SubjectWebpage = GetRowColumn( dr, "SubjectWebpage", string.Empty );
					item.CTID = GetRowPossibleColumn( dr, "CTID", string.Empty );
					item.CredentialRegistryId = GetRowPossibleColumn( dr, "CredentialRegistryId", string.Empty );



					//org = GetRowPossibleColumn( dr, "Organization", string.Empty );
					//orgId = GetRowPossibleColumn( dr, "OrgId", 0 );
					//if ( orgId > 0 )
					//	item.OwningOrganization = new Organization() { Id = orgId, Name = org };

					//
					//temp = GetRowColumn( dr, "DateEffective", string.Empty );
					//if ( IsValidDate( temp ) )
					//	item.DateEffective = DateTime.Parse( temp ).ToString("yyyy-MM-dd");
					//else
					//	item.DateEffective = string.Empty;

					item.Created = GetRowColumn( dr, "Created", System.DateTime.MinValue );
					item.LastUpdated = GetRowColumn( dr, "LastUpdated", System.DateTime.MinValue );

					list.Add( item );
				}

				return list;

			}
		} //

		public static void MapToDB( ThisResource input, DBEntity output )
		{

			//want output ensure fields input create are not wiped
			if ( output.Id == 0 )
			{
				output.CTID = input.CTID;
			}
			//if ( !string.IsNullOrWhiteSpace( input.CredentialRegistryId ) )
			//	output.CredentialRegistryId = input.CredentialRegistryId;

			output.Id = input.Id;
			output.Name = GetData( input.Name );
			output.Description = GetData( input.Description );
			output.EntityStateId = input.EntityStateId;
            output.PrimaryAgentUID = input.PrimaryAgentUID;

            output.SubjectWebpage = GetUrlData( input.SubjectWebpage );
			output.InCatalog = GetUrlData( input.InCatalog );
			output.AlternateName = FormatListAsDelimitedString( input.AlternateName, "|" );
			output.Keyword = FormatListAsDelimitedString( input.Keyword, "|" );
			output.SameAs = FormatListAsDelimitedString( input.SameAs, "|" );

			output.CodedNotation = GetData( input.CodedNotation );
			output.Comment = GetData( input.CommentJson );
			output.Identifier = input.IdentifierJson;
			output.VersionIdentifier = input.VersionIdentifierJson;

			output.LifeCycleStatusTypeId = input.LifeCycleStatusTypeId;
			output.TargetCompetency = FormatCAOListAsDelimitedString( input.TargetCompetency, "|" );

			if ( IsValidDate( input.LastUpdated ) )
				output.LastUpdated = input.LastUpdated;
		}

		public static void MapFromDB( DBEntity input, ThisResource output,
				bool includingProperties )
		{

			output.Id = input.Id;
			output.RowId = input.RowId;
			output.EntityStateId = input.EntityStateId;

			//
			output.Name = input.Name;
			output.FriendlyName = FormatFriendlyTitle( input.Name );

			output.Description = input.Description ?? string.Empty;
			output.CTID = input.CTID;
            if ( IsGuidValid( input.PrimaryAgentUID ) )
            {
                output.PrimaryAgentUID = ( Guid ) input.PrimaryAgentUID;
                output.PrimaryOrganization = OrganizationManager.GetBasics( ( Guid ) input.PrimaryAgentUID );
				output.OfferedBy = new List<Organization>()
				{
					output.PrimaryOrganization
				};
            }
			//this would get offered by. Should exclude the primary or ????
            output.OrganizationRole = Entity_AgentRelationshipManager.AgentEntityRole_GetAll_ToEnumeration( output.RowId, true );
			if ( input.LifeCycleStatusTypeId != null )
			output.LifeCycleStatusTypeId = (int)input.LifeCycleStatusTypeId;
			output.InCatalog = GetUrlData( input.InCatalog );
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
				output.LifeCycleStatusType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS );
				EnumeratedItem statusItem = output.LifeCycleStatusType.GetFirstItem();
				if ( statusItem != null && statusItem.Id > 0 && statusItem.Name != "Active" )
				{

				}
			}
			if ( !string.IsNullOrWhiteSpace( input.TargetCompetency ) )
			{
				output.TargetCompetency = GetTargetCompetency( SplitDelimitedStringToList( input.TargetCompetency, '|' ) );
			}
			output.SubjectWebpage = input.SubjectWebpage;
			output.CodedNotation = GetData( input.CodedNotation );
			output.AlternateName = SplitDelimitedStringToList( input.AlternateName, '|' );
			output.Keyword = SplitDelimitedStringToList( input.Keyword, '|' );
			output.SameAs = SplitDelimitedStringToList( input.SameAs, '|' );
			if ( IsValidDate( input.Created ) )
				output.Created = ( DateTime )input.Created;
			if ( IsValidDate( input.LastUpdated ) )
				output.LastUpdated = ( DateTime )input.LastUpdated;

			if ( !string.IsNullOrWhiteSpace( input.Comment ) )
			{
				output.Comment = JsonConvert.DeserializeObject<List<string>>( input.Comment );
			}
			if ( !string.IsNullOrWhiteSpace( input.Identifier ) )
			{
				output.Identifier = JsonConvert.DeserializeObject<List<IdentifierValue>>( input.Identifier );
			}
			output.OccupationType = ReferenceFrameworkItemsManager.FillCredentialAlignmentObject( output.RowId, CodesManager.PROPERTY_CATEGORY_SOC );
			output.IndustryType = ReferenceFrameworkItemsManager.FillCredentialAlignmentObject( output.RowId, CodesManager.PROPERTY_CATEGORY_NAICS );

            if ( !string.IsNullOrWhiteSpace( input.VersionIdentifier ) )
			{
				output.VersionIdentifier = JsonConvert.DeserializeObject<List<IdentifierValue>>( input.VersionIdentifier );
			}
			//=====
			var relatedEntity = EntityManager.GetEntity( output.RowId, false );
            //if ( relatedEntity != null && relatedEntity.Id > 0 )
            output.EntityLastUpdated = output.LastUpdated;  // relatedEntity.LastUpdated;

            //get condition profiles
            List<ConditionProfile> list = Entity_ConditionProfileManager.GetAll( output.RowId, false, false );
            if ( list != null && list.Count > 0 )
            {
				output.Requires = new List<ConditionProfile>();
                foreach ( ConditionProfile item in list )
                {
                    if ( item.ConditionSubTypeId != Entity_ConditionProfileManager.ConditionSubType_Basic )
                    {
                        //should not happen
                    }
                    else if ( item.ConnectionProfileTypeId == Entity_ConditionProfileManager.ConnectionProfileType_Requirement )
                        output.Requires.Add( item );
                    else
                    {
                        EmailManager.NotifyAdmin( $"Unexpected Condition Profile for {Entity_Label}", string.Format( "recordId: {0}, ConditionProfileTypeId: {1}", output.Id, item.ConnectionProfileTypeId ) );
                    }
                }
            }
			//
			output.HasSupportService = Entity_HasSupportServiceManager.GetAllAsResourceSummary( relatedEntity );
            //actually more effecient to get all and then split out
			var getAll = Entity_HasResourceManager.GetAll( relatedEntity);
			if (getAll != null && getAll.Count > 0 )
			{
				output.HasOccupation = getAll.Where( r => r.EntityTypeId == CodesManager.ENTITY_TYPE_OCCUPATIONS_PROFILE ).ToList();
                output.HasTask = getAll.Where( r => r.EntityTypeId == CodesManager.ENTITY_TYPE_TASK_PROFILE ).ToList();
                output.HasWorkRole = getAll.Where( r => r.EntityTypeId == CodesManager.ENTITY_TYPE_WORKROLE_PROFILE ).ToList();
				output.HasRubric = getAll.Where( r => r.EntityTypeId == CodesManager.ENTITY_TYPE_RUBRIC ).ToList();
				output.ProvidesTransferValueFor = getAll.Where( r => r.RelationshipTypeId == Entity_HasResourceManager.HAS_RESOURCE_TYPE_ProvidesTransferValueFor && ( r.EntityTypeId == CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE ) ).ToList();
				output.ReceivesTransferValueFrom = getAll.Where( r => r.RelationshipTypeId == Entity_HasResourceManager.HAS_RESOURCE_TYPE_ReceivesTransferValueFrom && ( r.EntityTypeId == CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE ) ).ToList();
				var pathwayComponents = getAll.Where( r => r.EntityTypeId == CodesManager.ENTITY_TYPE_PATHWAY_COMPONENT ).ToList();
				if ( pathwayComponents != null &&  pathwayComponents.Count > 0 )
				{
					foreach ( var component in pathwayComponents )
					{
						var pathway = PathwayManager.GetByCtid( component.CTID );
						if ( pathway != null && pathway.Id > 0)
						{
							output.TargetPathway.Add( pathway );
						}
					}
				}
				output.AbilityEmbodiedOutput = MapResourceSummaryToCAOFramework( getAll.Where( r => r.RelationshipTypeId == 5 && ( r.EntityTypeId == CodesManager.ENTITY_TYPE_COMPETENCY || r.EntityTypeId == CodesManager.ENTITY_TYPE_COLLECTION_COMPETENCY ) ).ToList() );
				output.KnowledgeEmbodiedOutput = MapResourceSummaryToCAOFramework( getAll.Where( r => r.RelationshipTypeId == 6 && ( r.EntityTypeId == CodesManager.ENTITY_TYPE_COMPETENCY || r.EntityTypeId == CodesManager.ENTITY_TYPE_COLLECTION_COMPETENCY ) ).ToList() );
				output.SkillEmbodiedOutput = MapResourceSummaryToCAOFramework( getAll.Where( r => r.RelationshipTypeId == 7 && ( r.EntityTypeId == CodesManager.ENTITY_TYPE_COMPETENCY || r.EntityTypeId == CodesManager.ENTITY_TYPE_COLLECTION_COMPETENCY ) ).ToList() );
				output.PerformanceLevelType = UpdateConceptResourceSummary( getAll.Where( r => r.RelationshipTypeId == Entity_HasResourceManager.HAS_RESOURCE_TYPE_PerformanceLevelType && r.EntityTypeId == CodesManager.ENTITY_TYPE_CONCEPT ).ToList() );
				output.PhysicalCapabilityType = UpdateConceptResourceSummary( getAll.Where( r => r.RelationshipTypeId == Entity_HasResourceManager.HAS_RESOURCE_TYPE_PhysicalCapabilityType && r.EntityTypeId == CodesManager.ENTITY_TYPE_CONCEPT ).ToList() );
				output.SensoryCapabilityType = UpdateConceptResourceSummary( getAll.Where( r => r.RelationshipTypeId == Entity_HasResourceManager.HAS_RESOURCE_TYPE_SensoryCapabiltyType && r.EntityTypeId == CodesManager.ENTITY_TYPE_CONCEPT ).ToList() );
				output.EnvironmentalHazardType = UpdateConceptResourceSummary( getAll.Where( r => r.RelationshipTypeId == Entity_HasResourceManager.HAS_RESOURCE_TYPE_EnvironmentalHazardType && r.EntityTypeId == CodesManager.ENTITY_TYPE_CONCEPT ).ToList() );
				output.Classification = UpdateConceptResourceSummary( getAll.Where( r => r.RelationshipTypeId == Entity_HasResourceManager.HAS_RESOURCE_TYPE_Classification && r.EntityTypeId == CodesManager.ENTITY_TYPE_CONCEPT ).ToList() );
			}
			var parentsgetAll = Entity_HasResourceManager.GetParentsForResourceId( output.Id, output.EntityTypeId );
			foreach ( var item in parentsgetAll )
			{
				if ( item.EntityTypeId == CodesManager.ENTITY_TYPE_TASK_PROFILE )
				{
					output.RelatedTask.Add( item );
				}
				if ( item.EntityTypeId == CodesManager.ENTITY_TYPE_OCCUPATIONS_PROFILE )
				{
					output.RelatedOccupation.Add( item );
				}
				if ( item.EntityTypeId == CodesManager.ENTITY_TYPE_WORKROLE_PROFILE )
				{
					output.RelatedWorkRole.Add( item );
				}
			}
			output.RelatedCollection = CollectionMemberManager.GetMemberOfCollections( output.CTID );
			//output.HasOccupation = Entity_HasResourceManager.GetAllEntityType( relatedEntity, CodesManager.ENTITY_TYPE_OCCUPATIONS_PROFILE );
			//output.HasWorkRole = Entity_HasResourceManager.GetAllEntityType( relatedEntity, CodesManager.ENTITY_TYPE_TASK_PROFILE);
			//output.HasTask = Entity_HasResourceManager.GetAllEntityType( relatedEntity, CodesManager.ENTITY_TYPE_WORKROLE_PROFILE );
		} //

		#endregion

	}
}

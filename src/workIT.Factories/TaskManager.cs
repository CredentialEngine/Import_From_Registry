﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Newtonsoft.Json;
using workIT.Models;
using workIT.Models.Common;
using workIT.Utilities;
using DBEntity = workIT.Data.Tables.TaskProfile;
using EntityContext = workIT.Data.Tables.workITEntities;
using ThisResource = workIT.Models.Common.Task;

namespace workIT.Factories
{
    public class TaskManager : BaseFactory
	{
		static readonly string thisClassName = "TaskManager";
		static string EntityType = "Task";
		static int EntityTypeId = CodesManager.ENTITY_TYPE_TASK_PROFILE;
        static string Entity_Label = "Task";
        static string Entities_Label = "Tasks";

		#region Task - persistance ==================
		/// <summary>
		/// Update a Task
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
						DBEntity efEntity = context.TaskProfile
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
									ActivityType = "Task",
									Activity = "Import",
									Event = "Reactivate",
									Comment = string.Format( "Task had been marked as deleted, and was reactivted by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ),
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
									string message = string.Format( thisClassName + ".Save Failed", "Attempted to update a Task. The process appeared to not work, but was not an exception, so we have no message, or no clue. Task: {0}, Id: {1}", entity.Name, entity.Id );
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
									ActivityType = "Task",
									Activity = "Import",
									Event = "Update",
									Comment = string.Format( "Task was updated by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ),
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
				string message = HandleDBValidationError( dbex, thisClassName + string.Format( ".Save. id: {0}, Name: {1}", entity.Id, entity.Name ), "Task" );
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
		/// add a Task
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
					context.TaskProfile.Add( efEntity );

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
							ActivityType = "Task",
							Activity = "Import",
							Event = "Add",
							Comment = string.Format( "Full Task was added by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ),
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

						string message = thisClassName + string.Format( ". Add Failed", "Attempted to add a Task. The process appeared to not work, but was not an exception, so we have no message, or no clue. Task: {0}, ctid: {1}", entity.Name, entity.CTID );
						status.AddError( thisClassName + ". Error - the add was not successful. " + message );
						EmailManager.NotifyAdmin( "TaskManager. Add Failed", message );
					}
				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", "Task" );
					status.AddError( thisClassName + ".Add(). Error - the save was not successful. " + message );

					LoggingHelper.LogError( dbex, message );
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
					if ( entity == null ||
						( string.IsNullOrWhiteSpace( entity.Name ) )
						//||                        string.IsNullOrWhiteSpace( entity.SubjectWebpage )) 
						)
					{
						status.AddError( thisClassName + ". AddBaseReference() The Task is incomplete" );
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

					context.TaskProfile.Add( efEntity );
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
				status.AddError( HandleDBValidationError( dbex, thisClassName + ".AddBaseReference() ", "Task" ) );
				LoggingHelper.LogError( dbex, thisClassName + string.Format( ".Add(), Name: {0}, SubjectWebpage: {1}", entity.Name, entity.SubjectWebpage ) );


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

					efEntity.Created = System.DateTime.Now;
					efEntity.LastUpdated = System.DateTime.Now;

					context.TaskProfile.Add( efEntity );
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
						entity.Created = ( DateTime ) efEntity.Created;
						entity.LastUpdated = ( DateTime ) efEntity.LastUpdated;
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
				//status.AddWarning( "An Task Description must be entered" );
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

			//if ( string.IsNullOrWhiteSpace( profile.SubjectWebpage ) )
			//	status.AddWarning( "Error - A Subject Webpage name must be entered" );

			//else if ( !IsUrlValid( profile.SubjectWebpage, ref commonStatusMessage ) )
			//{
			//	status.AddWarning( "The Task Subject Webpage is invalid. " + commonStatusMessage );
			//}


			return status.WasSectionValid;
		}


		/// <summary>
		/// Delete an Task, and related Entity
		/// </summary>
		/// <param name="id"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool Delete( int id, ref string statusMessage )
		{
			bool isValid = false;
			if ( id == 0 )
			{
				statusMessage = "Error - missing an identifier for the Task";
				return false;
			}

			using ( var context = new EntityContext() )
			{
				try
				{
					context.Configuration.LazyLoadingEnabled = false;
					DBEntity efEntity = context.TaskProfile
								.SingleOrDefault( s => s.Id == id );

					if ( efEntity != null && efEntity.Id > 0 )
					{

						//need to remove from Entity.
						//could use a pre-delete trigger?
						//what about roles

						context.TaskProfile.Remove( efEntity );
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
						statusMessage = "Error - Task_Delete failed, as record was not found.";
					}
				}
				catch ( Exception ex )
				{
					statusMessage = FormatExceptions( ex );
					LoggingHelper.LogError( ex, thisClassName + ".Task_Delete()" );

					if ( statusMessage.ToLower().IndexOf( "the delete statement conflicted with the reference constraint" ) > -1 )
					{
						statusMessage = "Error: this Task cannot be deleted as it is being referenced by other items, such as roles or credentials. These associations must be removed before this Task can be deleted.";
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
					DBEntity efEntity = context.TaskProfile
								.FirstOrDefault( s => ( s.CTID == ctid )
								);

					if ( efEntity != null && efEntity.Id > 0 )
					{
						Guid rowId = efEntity.RowId;

						//need to remove from Entity.
						//-using before delete trigger - verify won't have RI issues
						string msg = string.Format( " Task. Id: {0}, Name: {1}, Ctid: {2}.", efEntity.Id, efEntity.Name, efEntity.CTID );
						//18-04-05 mparsons - change to set inactive, and notify - seems to have been some incorrect deletes
						//context.TaskProfile.Remove( efEntity );
						efEntity.EntityStateId = 0;
						efEntity.LastUpdated = System.DateTime.Now;
						int count = context.SaveChanges();
						if ( count > 0 )
						{
							new ActivityManager().SiteActivityAdd( new SiteActivity()
							{
								ActivityType = "Task",
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
						statusMessage = "Error: this Task cannot be deleted as it is being referenced by other items, such as roles or credentials. These associations must be removed before this Task can be deleted.";
					}
				}
			}
			return isValid;
		}

		#region Task properties ===================
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
			eamgr.SaveList( relatedEntity.Id, Entity_AgentRelationshipManager.ROLE_TYPE_PUBLISHEDBY, resource.PublishedBy, ref status );
			eamgr.SaveList( relatedEntity.Id, Entity_AgentRelationshipManager.ROLE_TYPE_AssertedBy, resource.AssertedByList, ref status );
			eamgr.SaveList( relatedEntity.Id, Entity_AgentRelationshipManager.ROLE_TYPE_OFFERED_BY, resource.OfferedBy, ref status );

			//Entity_ReferenceFrameworkManager erfm = new Entity_ReferenceFrameworkManager();
			//erfm.DeleteAll( relatedEntity, ref status );

			//if ( erfm.SaveList( relatedEntity.Id, CodesManager.PROPERTY_CATEGORY_SOC, entity.Tasks, ref status ) == false )
			//	isAllValid = false;
			//if ( erfm.SaveList( relatedEntity.Id, CodesManager.PROPERTY_CATEGORY_NAICS, entity.Industries, ref status ) == false )
			//	isAllValid = false;


			//Entity_ReferenceManager erm = new Entity_ReferenceManager();
			//erm.DeleteAll( relatedEntity, ref status );
			//if ( erm.Add( entity.Subject, entity.RowId, CodesManager.ENTITY_TYPE_Task_PROFILE, ref status, CodesManager.PROPERTY_CATEGORY_SUBJECT, false ) == false )
			//	isAllValid = false;

			//if ( erm.Add( entity.Keyword, entity.RowId, CodesManager.ENTITY_TYPE_Task_PROFILE, ref status, CodesManager.PROPERTY_CATEGORY_KEYWORD, false ) == false )
			//	isAllValid = false;


			var eHasResourcesMgr = new Entity_HasResourceManager();
			//no, doing delete in save method
			//Ohh, this would be fine if all entity types are known in advance. With KSA, there can be a mix
			eHasResourcesMgr.DeleteAll( relatedEntity, ref status );
			//
			if ( eHasResourcesMgr.SaveList( relatedEntity, CodesManager.ENTITY_TYPE_TASK_PROFILE, resource.HasChildIds, ref status, Entity_HasResourceManager.HAS_RESOURCE_TYPE_HasChild ) == false )
				isAllValid = false;
			//NOT SURE this should happen??
			if ( eHasResourcesMgr.SaveList( relatedEntity, CodesManager.ENTITY_TYPE_TASK_PROFILE, resource.IsChildOfIds, ref status, Entity_HasResourceManager.HAS_RESOURCE_TYPE_IsChildOf ) == false )
				isAllValid = false;
			if ( eHasResourcesMgr.SaveList( relatedEntity, CodesManager.ENTITY_TYPE_WORKROLE_PROFILE, resource.HasWorkRoleIds, ref status, Entity_HasResourceManager.HAS_RESOURCE_TYPE_HasResource ) == false )
				isAllValid = false;
			if ( eHasResourcesMgr.SaveList( relatedEntity, CodesManager.ENTITY_TYPE_OCCUPATIONS_PROFILE, resource.HasOccupationIds, ref status, Entity_HasResourceManager.HAS_RESOURCE_TYPE_HasResource ) == false )
				isAllValid = false;
			if ( eHasResourcesMgr.SaveList( relatedEntity, CodesManager.ENTITY_TYPE_JOB_PROFILE, resource.HasJobIds, ref status, Entity_HasResourceManager.HAS_RESOURCE_TYPE_HasResource ) == false )
				isAllValid = false;
			//
			if ( eHasResourcesMgr.SaveList( relatedEntity, CodesManager.ENTITY_TYPE_RUBRIC, resource.HasRubricIds, ref status, Entity_HasResourceManager.HAS_RESOURCE_TYPE_HasResource ) == false )
				isAllValid = false;
			//KSA
			if ( eHasResourcesMgr.SaveList( relatedEntity, resource.AbilityEmbodied, ref status, Entity_HasResourceManager.HAS_RESOURCE_TYPE_AbilityEmbodied ) == false )
				isAllValid = false;
			if ( eHasResourcesMgr.SaveList( relatedEntity, resource.KnowledgeEmbodied, ref status, Entity_HasResourceManager.HAS_RESOURCE_TYPE_KnowledgeEmbodied ) == false )
				isAllValid = false;
			if ( eHasResourcesMgr.SaveList( relatedEntity, resource.SkillEmbodied, ref status, Entity_HasResourceManager.HAS_RESOURCE_TYPE_SkillEmbodied ) == false )
				isAllValid = false;
			//concepts
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
				DBEntity from = context.TaskProfile
						.FirstOrDefault( s => s.CTID.ToLower() == ctid.ToLower() );

				if ( from != null && from.Id > 0 )
				{
					entity.RowId = from.RowId;
					entity.Id = from.Id;
					entity.EntityStateId = from.EntityStateId;
					entity.Name = from.Name;
					entity.Description = from.Description;
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
					DBEntity item = context.TaskProfile
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
				DBEntity item = context.TaskProfile
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
				DBEntity item = context.TaskProfile
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

        public static ThisResource GetByNameAndDescription( string name, string description )
        {
            var entity = new ThisResource();
            using ( var context = new EntityContext() )
            {
                context.Configuration.LazyLoadingEnabled = false;
                var list = context.TaskProfile
                        .Where( s => s.Name.ToLower() == name.ToLower() && s.EntityStateId > 1
                            && !string.IsNullOrWhiteSpace( s.Description ) )
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
                context.Configuration.LazyLoadingEnabled = false;
                var list = context.TaskProfile
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
                var results = context.TaskProfile
                            .Where( s => s.PrimaryAgentUid == orgUid && s.EntityStateId == 3 )
                            .ToList();

                if ( results != null && results.Count > 0 )
                {
                    totalRecords = results.Count();
                }
            }
            return totalRecords;
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

				using ( SqlCommand command = new SqlCommand( "[Task_Search]", c ) )
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
						string rows = command.Parameters[command.Parameters.Count - 1].Value.ToString();

						pTotalRows = Int32.Parse( rows );
					}
					catch ( Exception ex )
					{
						pTotalRows = 0;
						LoggingHelper.LogError( ex, thisClassName + string.Format( ".Search() - Execute proc, Message: {0} \r\n Filter: {1} \r\n", ex.Message, pFilter ) );

						item = new ThisResource();
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
            output.PrimaryAgentUid = input.PrimaryAgentUID;

            output.CodedNotation = GetData( input.CodedNotation );  
			output.Comment = GetData( input.CommentJson );
			output.Identifier = input.IdentifierJson;
			output.VersionIdentifier = input.VersionIdentifierJson;
			output.ListID = input.ListId;
			output.InCatalog = GetUrlData( input.InCatalog );
			output.LifeCycleStatusTypeId = input.LifeCycleStatusTypeId;
			output.TargetCompetency = FormatCAOListAsDelimitedString( input.TargetCompetency, "|" );

			if ( IsValidDate( input.LastUpdated ) )
				output.LastUpdated = input.LastUpdated;
		}

		public static void MapFromDB( DBEntity input, ThisResource output, bool includingProperties )
		{

			output.Id = input.Id;
			output.RowId = input.RowId;
			output.EntityStateId = input.EntityStateId;

			//
			output.Name = !string.IsNullOrEmpty(input.Name)? input.Name:input.Description.Length> 150? input.Description.Substring(0,150):input.Description;
			output.FriendlyName = FormatFriendlyTitle( input.Name );

			output.Description = input.Description == null ? string.Empty : input.Description;
			output.CTID = input.CTID;
			output.CodedNotation = input.CodedNotation;
			output.ListId = input.ListID;
            if ( IsGuidValid( input.PrimaryAgentUid ) )
            {
                output.PrimaryAgentUID = ( Guid ) input.PrimaryAgentUid;
                output.PrimaryOrganization = OrganizationManager.GetBasics( ( Guid ) input.PrimaryAgentUid );
            }
			output.OrganizationRole = Entity_AgentRelationshipManager.AgentEntityRole_GetAll_ToEnumeration( output.RowId, true );
			if ( IsValidDate( input.Created ) )
				output.Created = ( DateTime ) input.Created;
			if ( IsValidDate( input.LastUpdated ) )
				output.LastUpdated = ( DateTime ) input.LastUpdated;

			if ( !string.IsNullOrWhiteSpace( input.Comment ) )
			{
				output.Comment = JsonConvert.DeserializeObject<List<string>>( input.Comment );
			}
			if ( !string.IsNullOrWhiteSpace( input.Identifier ) )
			{
				output.Identifier = JsonConvert.DeserializeObject<List<IdentifierValue>>( input.Identifier );
			}

			if ( !string.IsNullOrWhiteSpace( input.VersionIdentifier ) )
			{
				output.VersionIdentifier = JsonConvert.DeserializeObject<List<IdentifierValue>>( input.VersionIdentifier );
			}
			output.InCatalog = GetUrlData( input.InCatalog );
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
			//=====
			var relatedEntity = EntityManager.GetEntity( output.RowId, false );
			//if ( relatedEntity != null && relatedEntity.Id > 0 )
			//	output.EntityLastUpdated = relatedEntity.LastUpdated;
			//NOTE: EntityLastUpdated should really be the last registry update now. Check how LastUpdated is assigned on import
			output.EntityLastUpdated = output.LastUpdated;

			//get all with relationshipId = 1 - yes need to chg, well maybe
			var getAll = Entity_HasResourceManager.GetAll( relatedEntity );
			//var getAll = Entity_HasResourceManager.GetAllEntityType( relatedEntity, CodesManager.ENTITY_TYPE_OCCUPATIONS_PROFILE, HasSpecializationRelationshipId );
			if ( getAll != null && getAll.Count > 0 )
			{
				output.HasChild = getAll.Where( r => r.EntityTypeId == CodesManager.ENTITY_TYPE_TASK_PROFILE && r.RelationshipTypeId== Entity_HasResourceManager.HAS_RESOURCE_TYPE_HasChild ).ToList();
				output.IsChildOf = getAll.Where( r => r.EntityTypeId == CodesManager.ENTITY_TYPE_TASK_PROFILE && r.RelationshipTypeId == Entity_HasResourceManager.HAS_RESOURCE_TYPE_IsChildOf ).ToList();

				output.HasOccupation = getAll.Where( r => r.EntityTypeId == CodesManager.ENTITY_TYPE_OCCUPATIONS_PROFILE ).ToList();
				output.HasJob = getAll.Where( r => r.EntityTypeId == CodesManager.ENTITY_TYPE_JOB_PROFILE ).ToList();
				output.HasWorkRole = getAll.Where( r => r.EntityTypeId == CodesManager.ENTITY_TYPE_WORKROLE_PROFILE ).ToList();
				output.HasRubric = getAll.Where( r => r.EntityTypeId == CodesManager.ENTITY_TYPE_RUBRIC ).ToList();
				output.AbilityEmbodiedOutput = MapResourceSummaryToCAOFramework( getAll.Where( r => r.RelationshipTypeId == 5 && ( r.EntityTypeId == CodesManager.ENTITY_TYPE_COMPETENCY || r.EntityTypeId == CodesManager.ENTITY_TYPE_COLLECTION_COMPETENCY ) ).ToList() );
				output.KnowledgeEmbodiedOutput = MapResourceSummaryToCAOFramework( getAll.Where( r => r.RelationshipTypeId == 6 && ( r.EntityTypeId == CodesManager.ENTITY_TYPE_COMPETENCY || r.EntityTypeId == CodesManager.ENTITY_TYPE_COLLECTION_COMPETENCY ) ).ToList() );
				output.SkillEmbodiedOutput = MapResourceSummaryToCAOFramework( getAll.Where( r => r.RelationshipTypeId == 7 && ( r.EntityTypeId == CodesManager.ENTITY_TYPE_COMPETENCY || r.EntityTypeId == CodesManager.ENTITY_TYPE_COLLECTION_COMPETENCY ) ).ToList() );

				output.PerformanceLevelType = UpdateConceptResourceSummary( getAll.Where( r => r.RelationshipTypeId == Entity_HasResourceManager.HAS_RESOURCE_TYPE_PerformanceLevelType && r.EntityTypeId == CodesManager.ENTITY_TYPE_CONCEPT ).ToList() );
				output.PhysicalCapabilityType = UpdateConceptResourceSummary( getAll.Where( r => r.RelationshipTypeId == Entity_HasResourceManager.HAS_RESOURCE_TYPE_PhysicalCapabilityType && r.EntityTypeId == CodesManager.ENTITY_TYPE_CONCEPT ).ToList() );
				output.SensoryCapabilityType = UpdateConceptResourceSummary( getAll.Where( r => r.RelationshipTypeId == Entity_HasResourceManager.HAS_RESOURCE_TYPE_SensoryCapabiltyType && r.EntityTypeId == CodesManager.ENTITY_TYPE_CONCEPT ).ToList() );
				output.EnvironmentalHazardType = UpdateConceptResourceSummary( getAll.Where( r => r.RelationshipTypeId == Entity_HasResourceManager.HAS_RESOURCE_TYPE_EnvironmentalHazardType && r.EntityTypeId == CodesManager.ENTITY_TYPE_CONCEPT ).ToList() );
				output.Classification = UpdateConceptResourceSummary( getAll.Where( r => r.RelationshipTypeId == Entity_HasResourceManager.HAS_RESOURCE_TYPE_Classification && r.EntityTypeId == CodesManager.ENTITY_TYPE_CONCEPT ).ToList() );
				//output.AbilityEmbodied = getAll.Where( r => r.RelationshipTypeId == Entity_HasResourceManager.HAS_RESOURCE_TYPE_AbilityEmbodied && r.EntityTypeId == CodesManager.ENTITY_TYPE_COMPETENCY ).ToList();
				//output.KnowledgeEmbodied = getAll.Where( r => r.RelationshipTypeId == Entity_HasResourceManager.HAS_RESOURCE_TYPE_KnowledgeEmbodied && r.EntityTypeId == CodesManager.ENTITY_TYPE_COMPETENCY ).ToList();
				//output.SkillEmbodied = getAll.Where( r => r.RelationshipTypeId == Entity_HasResourceManager.HAS_RESOURCE_TYPE_SkillEmbodied && r.EntityTypeId == CodesManager.ENTITY_TYPE_COMPETENCY ).ToList();
			}
			//getAll = Entity_HasResourceManager.GetAllEntityType( relatedEntity, CodesManager.ENTITY_TYPE_TASK_PROFILE, IsChildOf );
			//if ( getAll != null && getAll.Count > 0 )
			//{
			//	//need to qualify
			//	//output.IsChildOf = getAll.Where( r => r.EntityTypeId == CodesManager.ENTITY_TYPE_TASK_PROFILE ).ToList();
			//}
			var parentsgetAll = Entity_HasResourceManager.GetParentsForResourceId( output.Id, output.EntityTypeId );
			foreach ( var item in parentsgetAll )
			{
				if ( item.EntityTypeId == CodesManager.ENTITY_TYPE_JOB_PROFILE )
				{
					output.RelatedJob.Add( item );
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


		} //

		#endregion

	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using workIT.Models;
using workIT.Models.Common;
using workIT.Models.ProfileModels;

using workIT.Utilities;

using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;

using ThisEntity = workIT.Models.Common.Entity;
using DBentity = workIT.Data.Tables.Entity;

using ViewContext = workIT.Data.Views.workITViews;
using EntityContext = workIT.Data.Tables.workITEntities;

namespace workIT.Factories
{
	/// <summary>
	/// manager for entities
	/// NOTE: May 7, 2016 contactUs - using after insert triggers to create the entity related a new created major entities like:
	/// - Credential
	/// - Organization
	/// - Assessment
	/// - ConnectionProfile
	/// - LearningOpportunity
	/// However, the issue will be not having the EntityId for the entity child components
	/// </summary>
	public class EntityManager : BaseFactory
	{
		string thisClassName = "EntityManager";
		#region 
		/// <summary>
		/// Resetting an entity by first deleting it, and then readding.
		/// The purpose of the delete is to remove all children relationships.
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool ResetEntity(Entity entity, ref string statusMessage)
		{
			bool isValid = true; 
			if (Delete(entity.EntityUid, ref statusMessage) == false)
			{
				//major issue
				return false;
			}

			if ( Add( entity.EntityUid, entity.EntityBaseId, entity.EntityTypeId, entity.EntityBaseName, ref statusMessage ) > 0 )
			{
				//add log entry
				SiteActivity sa = new SiteActivity()
				{
					ActivityType = "Import",
					Activity = entity.EntityType,
					Event = "Reset Entity",
					Comment = string.Format( "Entity was reset due to import of {0} [{1}]", entity.EntityType, entity.EntityBaseId ),
					ActivityObjectId = entity.EntityBaseId
				};
				//skip adds
				//new ActivityManager().SiteActivityAdd( sa );
			}
			else
				isValid = false;

			return isValid;
		}
		/// <summary>
		/// Add an Entity mirror
		/// NOTE: ALL ENTITY ADDS WOULD NORMALLY BE DONE VIA TRIGGERS. 
		/// However, as on import, we want to delete all the child entities for a top level entity like credential. The latter is accomplished by deleting the Entity. We then need to re- add the Entity.
		/// </summary>
		/// <param name="entityUid">RowId of the base Object</param>
		/// <param name="baseId">Integer PK of the base object</param>
		/// <param name="entityTypeId"></param>
		/// <param name="baseName"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		private int Add( Guid entityUid, int baseId, int entityTypeId, string baseName, ref string statusMessage )
		{

			DBentity efEntity = new DBentity();
			using ( var context = new EntityContext() )
			{
				try
				{
					efEntity.EntityUid = entityUid;
					efEntity.EntityBaseId = baseId;
					efEntity.EntityTypeId = entityTypeId;
					efEntity.EntityBaseName = baseName;
					efEntity.Created = efEntity.LastUpdated = System.DateTime.Now;

					context.Entities.Add( efEntity );

					// submit the change to database
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						statusMessage = "successful";

						return efEntity.Id;
					}
					else
					{
						//?no info on error
						statusMessage = "Error - the add was not successful. ";
						string message = thisClassName + string.Format( ". Add Failed", "Attempted to add an Entity. The process appeared to not work, but was not an exception, so we have no message, or no clue. entityUid: {0}, entityTypeId: {1}", entityUid.ToString(), entityTypeId );
						EmailManager.NotifyAdmin( "AssessmentManager. Assessment_Add Failed", message );
						return 0;
					}
				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", "Entity" );
					statusMessage = "Error - the save was not successful. " + message ;
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Add(). entityUid: {0}, entityTypeId: {1}", entityUid.ToString(), entityTypeId ) );
				}
			}

			return 0;
		}

		/// <summary>
		/// Delete an Entity
		/// This should be handled by triggers as well, or at least with the child entity
		/// </summary>
		/// <param name="entityUid"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool Delete( Guid entityUid, ref string statusMessage )
		{
			bool isValid = false;
			if ( !IsValidGuid(entityUid))
			{
				statusMessage = "Error - missing a valid identifier for the Entity";
				return false;
			}
			using ( var context = new EntityContext() )
			{
				DBentity efEntity = context.Entities
							.FirstOrDefault( s => s.EntityUid == entityUid );

				if ( efEntity != null && efEntity.Id > 0 )
				{
					int entityTypeId = efEntity.EntityTypeId;
					//string entityType = efEntity.Codes_EntityType.Title;

					context.Entities.Remove( efEntity );
					int count = context.SaveChanges();
					if ( count >= 0 )
					{
						isValid = true;
					}
				}
				else
				{
					statusMessage = "Error - Entity delete unnecessary, as record was not found.";
					LoggingHelper.LogError( thisClassName + string.Format( ".Delete - WIERD - delete failed, as record was not found. entityUid: {0}", entityUid ), true );
				}
			}

			return isValid;
		}
		#endregion 
		#region retrieval


		public static Entity GetEntity( Guid entityUid )
		{
			Entity entity = new Entity();
			using ( var context = new EntityContext() )
			{
				DBentity item = context.Entities
						.FirstOrDefault( s => s.EntityUid == entityUid );

				if ( item != null && item.Id > 0 )
				{
					entity.Id = item.Id;
					entity.EntityTypeId = item.EntityTypeId;

					//entity.EntityType = item.Codes_EntityType.Title;

					entity.EntityUid = item.EntityUid;
					entity.EntityBaseId = item.EntityBaseId ?? 0;
					entity.EntityBaseName = item.EntityBaseName;
					entity.Created = (DateTime)item.Created;

				}
				return entity;
			}


		}

		public static Entity GetEntity( int entityId)
		{
			Entity entity = new Entity();
			using ( var context = new EntityContext() )
			{
				DBentity item = context.Entities
						.FirstOrDefault( s => s.Id == entityId);

				if ( item != null && item.Id > 0 )
				{
					entity.Id = item.Id;
					entity.EntityTypeId = item.EntityTypeId;
					//entity.EntityType = item.Codes_EntityType.Title;
					entity.EntityUid = item.EntityUid;
					entity.EntityBaseId = item.EntityBaseId ?? 0;
					entity.EntityBaseName = item.EntityBaseName;
					entity.Created = ( DateTime ) item.Created;

				}
				return entity;
			}
		}
		public static Entity GetEntity( int entityTypeId, int entityBaseId )
		{
			Entity entity = new Entity();
			using ( var context = new EntityContext() )
			{
				DBentity item = context.Entities
						.FirstOrDefault( s => s.EntityTypeId == entityTypeId
							&& s.EntityBaseId == entityBaseId );

				if ( item != null && item.Id > 0 )
				{
					entity.Id = item.Id;
					entity.EntityTypeId = item.EntityTypeId;
					//entity.EntityType = item.Codes_EntityType.Title;
					entity.EntityUid = item.EntityUid;
					entity.EntityBaseId = item.EntityBaseId ?? 0;
					entity.EntityBaseName = item.EntityBaseName;
					entity.Created = ( DateTime ) item.Created;

				}
				return entity;
			}


		}

		//Entity_Cache
		public static Entity GetEntity_FromCache( int entityId )
		{
			Entity entity = new Entity();
			using ( var context = new EntityContext() )
			{
				EM.Entity_Cache item = context.Entity_Cache
						.SingleOrDefault( s => s.Id == entityId );

				if ( item != null && item.Id > 0 )
				{
					entity.Id = item.Id;
					entity.EntityTypeId = item.EntityTypeId;
					entity.EntityType = item.EntityType;
					entity.EntityUid = item.EntityUid;
					entity.EntityBaseId = item.BaseId;
					entity.EntityBaseName = item.Name;
					entity.Created = ( DateTime ) item.Created;
					entity.LastUpdated = ( DateTime ) item.LastUpdated;
					if (item.parentEntityId > 0)
					{
						//NOTE	- can use the included Entity to get more info
						//		- although may want to turn off lazy loading
						entity.ParentEntity = new ThisEntity();
						entity.ParentEntity.Id = item.parentEntityId ?? 0;
						entity.ParentEntity.EntityTypeId = item.parentEntityTypeId ?? 0;
						entity.ParentEntity.EntityType = item.parentEntityType;
						entity.ParentEntity.EntityUid = (Guid)item.parentEntityUid;
					}
				}
				return entity;
			}
		}

		/// <summary>
		/// Look up for resolving a third party entity
		/// </summary>
		/// <param name="entityTypeId"></param>
		/// <param name="name"></param>
		/// <param name="subjectWebpage"></param>
		/// <returns></returns>
		public static Entity Entity_Cache_Get( int entityTypeId, string name, string subjectWebpage )
		{
			Entity entity = new Entity();
			using ( var context = new EntityContext() )
			{
				EM.Entity_Cache item = context.Entity_Cache
						.FirstOrDefault( s => s.EntityTypeId == entityTypeId
						 && s.Name.ToLower() == name.ToLower()
						 && s.SubjectWebpage == subjectWebpage.ToLower() );

				if ( item != null && item.Id > 0 )
				{
					entity.Id = item.Id;
					entity.EntityTypeId = item.EntityTypeId;
					entity.EntityType = item.EntityType;
					entity.EntityUid = item.EntityUid;
					entity.EntityBaseId = item.BaseId;
					entity.EntityBaseName = item.Name;
					entity.Created = ( DateTime ) item.Created;
					entity.LastUpdated = ( DateTime ) item.LastUpdated;
					if ( item.parentEntityId > 0 )
					{
						//NOTE	- can use the included Entity to get more info
						//		- although may want to turn off lazy loading
						entity.ParentEntity = new ThisEntity();
						entity.ParentEntity.Id = item.parentEntityId ?? 0;
						entity.ParentEntity.EntityTypeId = item.parentEntityTypeId ?? 0;
						entity.ParentEntity.EntityType = item.parentEntityType;
						entity.ParentEntity.EntityUid = ( Guid ) item.parentEntityUid;
					}
				}
				return entity;
			}
		}
		/*
		/// <summary>
		/// Get Entity_Summary
		/// NOTE: work to minimize use of this method - slow
		/// </summary>
		/// <param name="entityUid"></param>
		/// <returns></returns>
		public static Views.Entity_Summary GetDBEntity( Guid entityUid )
		{
			using ( var context = new ViewContext() )
			{
				Views.Entity_Summary item = context.Entity_Summary
						.FirstOrDefault( s => s.EntityUid == entityUid );

				if ( item != null && item.Id > 0 )
				{
					
				}
				return item;
			}
		}

		/// <summary>
		/// Get an Entity Summary object by entityId 
		/// </summary>
		/// <param name="entityId"></param>
		/// <returns></returns>
		public static EntitySummary GetEntitySummary( int entityId)
		{
			EntitySummary entity = new EntitySummary();
			using ( var context = new ViewContext() )
			{
				Views.Entity_Summary item = context.Entity_Summary
							.FirstOrDefault( s => s.Id == entityId );
			
				if ( item != null && item.Id > 0 )
				{
					entity.Id = item.Id;
					entity.EntityTypeId = item.EntityTypeId;
					entity.EntityType = item.EntityType;
					entity.EntityUid = item.EntityUid;
					entity.Name = item.Name;
					entity.BaseId = item.BaseId;
					entity.Description = item.Description;
					entity.StatusId = ( int ) ( item.StatusId ?? 1 );
					if ( IsValidDate( item.Created ) )
						entity.Created = ( DateTime ) item.Created;
					entity.CreatedById = item.CreatedById ?? 0;

					entity.ManagingOrgId = (int) (item.ManagingOrgId ?? 0);

					entity.parentEntityId = item.parentEntityId;
					entity.parentEntityUid = item.parentEntityUid;
					entity.parentEntityType = item.parentEntityType;
					entity.parentEntityTypeId = item.parentEntityTypeId;
				}
				return entity;
			}
		}


		/// <summary>
		/// Get the top level entity for a entity component. 
		/// This currently means one of:
		/// 1 - credential
		/// 2 - Organization
		/// 3 - Assessment
		/// 4 - Learning Opp
		/// </summary>
		/// <param name="entityId"></param>
		/// <returns></returns>
		public static Views.Entity_Summary GetTopLevelParentEntity( Guid entityUid)
		{
			Views.Entity_Summary topParent = new Views.Entity_Summary();

			using ( var context = new ViewContext() )
			{
				while ( topParent != null )
				{
					topParent = context.Entity_Summary
						.FirstOrDefault( s => s.EntityUid == entityUid );

					if ( topParent == null && topParent.Id == 0 )
					{
						//should not happen, so ???

						break;
					}
				}

				return topParent;
			}
		}
		*/
		#endregion

    }
}

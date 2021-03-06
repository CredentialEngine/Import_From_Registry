﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
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
using DBEntityCache = workIT.Data.Tables.Entity_Cache;
using ViewContext = workIT.Data.Views.workITViews;
using EntityContext = workIT.Data.Tables.workITEntities;

namespace workIT.Factories
{
	/// <summary>
	/// manager for entities
	/// NOTE: May 7, 2016 mparsons - using after insert triggers to create the entity related a new created major entities like:
	/// - Credential
	/// - Organization
	/// - Assessment
	/// - ConnectionProfile
	/// - LearningOpportunity
	/// However, the issue will be not having the EntityId for the entity child components
	/// </summary>
	public class EntityManager : BaseFactory
	{
		static string thisClassName = "EntityManager";
        #region persistance
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
			if (Delete(entity.EntityUid, string.Format("type: {0}, Id: {1}",entity.EntityTypeId, entity.EntityBaseId), ref statusMessage, 2) == false)
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

					context.Entity.Add( efEntity );

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

		public bool UpdateModifiedDate(Guid entityUid, ref SaveStatus status)
		{
			DateTime someDate = DateTime.Now;
			return UpdateModifiedDate( entityUid, ref status, someDate );
		}

		public bool UpdateModifiedDate( Guid entityUid, ref SaveStatus status, DateTime? modifiedDate )
		{
			DateTime date = (DateTime)modifiedDate;
			return UpdateModifiedDate( entityUid, ref status, date );
		}

        public bool UpdateModifiedDate( Guid entityUid, ref SaveStatus status, DateTime modifiedDate  )
        {
            bool isValid = false;
            if ( !IsValidGuid( entityUid ) )
            {
                status.AddError( thisClassName + ".UpdateModifiedDate(). Error - missing a valid identifier for the Entity" );
                return false;
            }
			if ( modifiedDate == null || modifiedDate < new DateTime(2017, 1, 1) )
				modifiedDate = DateTime.Now;

            using ( var context = new EntityContext() )
            {
                DBentity efEntity = context.Entity
                            .FirstOrDefault( s => s.EntityUid == entityUid );

                if ( efEntity != null && efEntity.Id > 0 )
                {
                    efEntity.LastUpdated = modifiedDate;
					//don't allowing setting to before created
					if ( efEntity.LastUpdated >= efEntity.Created )
					{
						int count = context.SaveChanges();
						if ( count >= 0 )
						{
							isValid = true;
							LoggingHelper.DoTrace( 7, thisClassName + string.Format( ".UpdateModifiedDate - update last updated for TypeId: {0}, BaseId: {1}", efEntity.EntityTypeId, efEntity.EntityBaseId ) );
						}
					}
                }
                else
                {
                    status.AddError( thisClassName + ".UpdateModifiedDate(). Error - Entity  was not found.");
                    LoggingHelper.LogError( thisClassName + string.Format( ".UpdateModifiedDate - record was not found. entityUid: {0}", entityUid ), true );
                }
            }

            return isValid;
		}///

		 /// <summary>
		 /// Delete an Entity
		 /// This should be handled by triggers as well, or at least with the child entity
		 /// </summary>
		 /// <param name="entityUid"></param>
		 /// <param name="forTableIdentifer">Table identifer for tracing</param>
		 /// <param name="statusMessage"></param>
		 /// <returns></returns>
		public bool Delete( Guid entityUid, string forTableIdentifer, ref string statusMessage, int attemptsRemaining = 0 )
        {
            bool isValid = false;
            statusMessage = "";
            if ( !IsValidGuid(entityUid) )
            {
                statusMessage = "Error - missing a valid identifier for the Entity";
                return false;
            }
            try
            {
                using ( var context = new EntityContext() )
                {
                    DBentity efEntity = context.Entity
                                .FirstOrDefault(s => s.EntityUid == entityUid);

                    if ( efEntity != null && efEntity.Id > 0 )
                    {
                        int entityTypeId = efEntity.EntityTypeId;
                        //string entityType = efEntity.Codes_EntityTypes.Title;

                        context.Entity.Remove(efEntity);
                        int count = context.SaveChanges();
                        if ( count > 0 )
                        {
                            isValid = true;
                        } else
                        {
                            statusMessage = string.Format( "Entity delete returned count of zero - potential inconsistant state. entityTypeId: {0}, entityUid: {1}", entityTypeId, entityUid );
                            LoggingHelper.LogError( thisClassName + string.Format( ".Delete - Entity delete returned count of zero - potential inconsistant state. entityTypeId: {0}, entityUid: {1}", entityTypeId, entityUid ), true );
                        }
                    }
                    else
                    {
                        statusMessage = "Error - Entity delete unnecessary, as record was not found.";
                        LoggingHelper.DoTrace( 1, thisClassName + string.Format(".Delete - WIERD - delete failed, as record was not found. entityUid: {0} for {1}.", entityUid, forTableIdentifer ) );
                    }
                }
            }
            catch (SqlException sex)
            {
                statusMessage = FormatExceptions(sex);
                if ( statusMessage.ToLower().IndexOf("was deadlocked on lock resources with another process") > -1 )
                {
                    LoggingHelper.DoTrace(4, thisClassName + string.Format(".Delete(). Attempt to delete entity: {0} failed with deadlock. Retrying {1} more times.", entityUid, attemptsRemaining));
                    if ( attemptsRemaining > 0 )
                    {
                        attemptsRemaining--;
                        return Delete(entityUid, forTableIdentifer, ref statusMessage, attemptsRemaining);
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch ( Exception ex )
            {
                statusMessage = FormatExceptions(ex);
            }

            return isValid;
        }
		#endregion 
		#region retrieval


		public static Entity GetEntity( Guid entityUid, bool includingAllChildren = true)
		{
			Entity entity = new Entity();
			using ( var context = new EntityContext() )
			{
				if ( includingAllChildren == false )
					context.Configuration.LazyLoadingEnabled = false;
				DBentity item = context.Entity
						.FirstOrDefault( s => s.EntityUid == entityUid );

				if ( item != null && item.Id > 0 )
				{
					entity.Id = item.Id;
					entity.EntityTypeId = item.EntityTypeId;
					//20-12-18 mp - the following was commented, not sure why - probably related to lazy loading. Change to do conditionally
					//entity.EntityType = item.Codes_EntityTypes.Title;
					if ( item.Codes_EntityTypes != null )
					{
						entity.EntityType = item.Codes_EntityTypes.Title;
					} else
					{
						entity.EntityType = GetEntityType( entity.EntityTypeId );
					}

					entity.EntityUid = item.EntityUid;
					entity.EntityBaseId = item.EntityBaseId ?? 0;
					entity.EntityBaseName = item.EntityBaseName;
					entity.Created = (DateTime)item.Created;
					entity.LastUpdated = item.LastUpdated != null ?( DateTime )item.LastUpdated : entity.Created;
				}
				return entity;
			}
		}

		public static string GetEntityType( int entityTypeId )
		{
			string entityType = "error";
			switch ( entityTypeId )
			{
				case 1:
					entityType="Credential";
					break;
				case 2:
					entityType = "Organization";
					break;
				case 3:
					entityType = "AssessmentProfile";
					break;
				case 4:
					entityType = "ConditionProfile";
					break;
				case 5:
					entityType = "CostProfile";
					break;
				case 6:
					entityType = "CostProfileItem";
					break;
				case 7:
					entityType = "LearningOpportunity";
					break;
				case 8:  
					entityType = "Pathway";
					break;
				case 9:    //
					entityType = "Rubric";
					break;
				case 10:
				case 17:
					entityType = "CompetencyFramework";
					break;
				case 11:   
					entityType = "ConceptScheme";
					break;
				//
				case 12:
					entityType = "RevocationProfile";
					break;
				case 13:
					entityType = "VerificationProfile";
					break;
				case 14:
					entityType = "ProcessProfile";
					break;
				case 15:
					entityType = "ContactPoint";
					break;
				case 16:
					entityType = "Address Profile";
					break;
				case 18:
					entityType = "JurisdictionProfile";
					break;
				case 19:
					entityType = "ConditionManifest";
					break;
				case 20:
					entityType = "CostManifest";
					break;
				case 21:
					entityType = "FinancialAssistanceProfile";
					break;
				case 22:
					entityType = "Accredit Action";
					break;
				case 23:
					entityType = "PathwaySet";
					break;
				case 24:
					entityType = "Pathway Component";
					break;
				case 25:
					entityType = "Component Condition";
					break;
				case 26:
					entityType = "TransferValueProfile";
					break;
				case 28:
					entityType = "HoldersProfile";
					break;
				case 29:
					entityType = "EarningsProfile";
					break;
				case 30:
					entityType = "EmploymentOutcomeProfile";
					break;
				case 31:
					entityType = "DataSet Profile";
					break;
				case 32:
					entityType = "JobProfile";
					break;
				case 33:
					entityType = "TaskProfile";
					break;
				case 34:
					entityType = "Work Role";
					break;
				case 35:
					entityType = "Occupation";
					break;
				default:
					LoggingHelper.LogError( string.Format( "{0}.GetEntityType. Invalid Entity type encountered: {1} ", thisClassName, entityTypeId ) );
					break;
			}

			return entityType;
		}

		public static Entity GetEntity( int entityId)
		{
			Entity entity = new Entity();
			using ( var context = new EntityContext() )
			{
				DBentity item = context.Entity
						.FirstOrDefault( s => s.Id == entityId);

				if ( item != null && item.Id > 0 )
				{
					entity.Id = item.Id;
					entity.EntityTypeId = item.EntityTypeId;
					//not why this was commented. Reusing with care
					if ( item.Codes_EntityTypes != null )
						entity.EntityType = item.Codes_EntityTypes.Title;
					else
					{
						entity.EntityType = GetEntityType( entity.EntityTypeId );
					}
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
				DBentity item = context.Entity
						.FirstOrDefault( s => s.EntityTypeId == entityTypeId
							&& s.EntityBaseId == entityBaseId );

				if ( item != null && item.Id > 0 )
				{
					entity.Id = item.Id;
					entity.EntityTypeId = item.EntityTypeId;
					//not why this was commented. Reusing with care
					if(item.Codes_EntityTypes!=null)
						entity.EntityType = item.Codes_EntityTypes.Title;
					else
					{
						entity.EntityType = GetEntityType( entity.EntityTypeId );
					}
					entity.EntityUid = item.EntityUid;
					entity.EntityBaseId = item.EntityBaseId ?? 0;
					entity.EntityBaseName = item.EntityBaseName;
					entity.Created = ( DateTime ) item.Created;

				}
				return entity;
			}


		}

		//
		#endregion
		#region Entity_Cache
		public int EntityCacheSave( EntityCache input, ref string statusMessage )
		{
			LoggingHelper.DoTrace( 6, string.Format( "EntityCacheSave entered. EntityTypeId:{0}, CTID: '{1}', BaseId: {2}, Name: {3}", input.EntityTypeId, input.CTID, input.BaseId, input.Name) );

			DBEntityCache efEntity = new DBEntityCache();
			if ( input.Id == 0 )
			{
				var ec = GetEntity( input.EntityTypeId, input.BaseId );
				if (ec != null && ec.Id > 0)
				{
					input.Id = ec.Id;
					input.EntityType = ec.EntityType;
					input.EntityTypeId = ec.EntityTypeId;
					input.EntityUid = ec.EntityUid;
					input.BaseId = ec.EntityBaseId;
				}
			}
			if (input.OwningOrgId==0)
			{
				var org = OrganizationManager.GetBasics( input.OwningAgentUID );
				if ( org != null )
					input.OwningOrgId = org.Id;
			}
			using ( var context = new EntityContext() )
			{
				try
				{
					if ( input.Id == 0)
					{
						//may need a look up just in case
						efEntity.Id = input.Id;//entityId
						efEntity.EntityTypeId = input.EntityTypeId;
						efEntity.EntityType = input.EntityType;
						efEntity.EntityUid = input.EntityUid;
						//
						efEntity.BaseId = input.BaseId;
						efEntity.CTID = input.CTID;
						efEntity.Name = input.Name;
						efEntity.EntityStateId = input.EntityStateId;
						efEntity.Description = input.Description ?? "";
						efEntity.SubjectWebpage = input.SubjectWebpage ?? "";
						efEntity.ImageUrl = input.ImageUrl ?? "";
						efEntity.SubjectWebpage = input.SubjectWebpage ?? "";
						efEntity.OwningOrgId = input.OwningOrgId;
						//not sure we really need this
						efEntity.parentEntityId = input.parentEntityId;
						efEntity.parentEntityType = input.parentEntityType;
						efEntity.parentEntityTypeId = input.parentEntityTypeId;
						efEntity.parentEntityUid = input.parentEntityUid;

						//
						efEntity.Created = efEntity.LastUpdated = input.Created;
						efEntity.CacheDate = System.DateTime.Now;

						context.Entity_Cache.Add( efEntity );
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
							string message = thisClassName + string.Format( ". Add Failed", "Attempted to add an Entity. The process appeared to not work, but was not an exception, so we have no message, or no clue. Entity_Cache: Type: {0}, Name: {1}, BaseId: {2}", input.EntityType, input.Name, input.BaseId );
							EmailManager.NotifyAdmin( "AssessmentManager. Assessment_Add Failed", message );
							return 0;
						}
					} else
					{
						efEntity = context.Entity_Cache
									.FirstOrDefault( s => s.Id == input.Id );

						if ( efEntity != null && efEntity.Id > 0 )
						{
							//these cannot be updated
							//efEntity.EntityTypeId = input.EntityTypeId;
							//efEntity.EntityType = input.EntityType;
							//efEntity.EntityUid = input.EntityUid;
							//
							//efEntity.BaseId = input.BaseId;
							//maybe if was a pending, 
							efEntity.CTID = input.CTID;
							efEntity.Name = input.Name;
							efEntity.EntityStateId = input.EntityStateId;
							efEntity.Description = input.Description ?? "";
							efEntity.SubjectWebpage = input.SubjectWebpage ?? "";
							efEntity.ImageUrl = input.ImageUrl ?? "";
							efEntity.SubjectWebpage = input.SubjectWebpage ?? "";
							efEntity.OwningOrgId = input.OwningOrgId;
							//
							efEntity.parentEntityId = input.parentEntityId;
							efEntity.parentEntityType = input.parentEntityType;
							efEntity.parentEntityTypeId = input.parentEntityTypeId;
							efEntity.parentEntityUid = input.parentEntityUid;

							//
							efEntity.LastUpdated = input.LastUpdated;
							if ( HasStateChanged( context ) )
							{
								efEntity.CacheDate = System.DateTime.Now;

								//NOTE efEntity.EntityStateId is set to 0 in delete method )
								int count = context.SaveChanges();
								//can be zero if no data changed
								if ( count >= 0 )
								{
									return efEntity.Id;
								}
								else
								{
									//?no info on error
									string message = string.Format( thisClassName + ".Save Failed", "Attempted to update an Entity_Cache. The process appeared to not work, but was not an exception, so we have no message, or no clue. Entity_Cache: Type: {0}, Name: {1}, BaseId: {2}", input.EntityType, input.Name, input.BaseId );
									EmailManager.NotifyAdmin( thisClassName + ".Save Failed Failed", message );
								}
							}
							
						} else
						{
							
						}
					}					
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".EntityCacheSave(). Entity_Cache: Type: {0}, Name: {1}, BaseId: {2}", input.EntityType, input.Name, input.BaseId ) );
				}
			}

			return 0;
		}
		//public static Entity GetEntity_FromCache( int entityId )
		//{
		//	Entity entity = new Entity();
		//	using ( var context = new EntityContext() )
		//	{
		//		EM.Entity_Cache item = context.Entity_Cache
		//				.SingleOrDefault( s => s.Id == entityId );

		//		if ( item != null && item.Id > 0 )
		//		{
		//			entity.Id = item.Id;
		//			entity.EntityTypeId = item.EntityTypeId;
		//			entity.EntityType = item.EntityType;
		//			entity.EntityUid = item.EntityUid;
		//			entity.EntityBaseId = item.BaseId;
		//			entity.EntityBaseName = item.Name;
		//			entity.Created = ( DateTime ) item.Created;
		//			entity.LastUpdated = ( DateTime ) item.LastUpdated;
		//			if (item.parentEntityId > 0)
		//			{
		//				//NOTE	- can use the included Entity to get more info
		//				//		- although may want to turn off lazy loading
		//				entity.ParentEntity = new ThisEntity();
		//				entity.ParentEntity.Id = item.parentEntityId ?? 0;
		//				entity.ParentEntity.EntityTypeId = item.parentEntityTypeId ?? 0;
		//				entity.ParentEntity.EntityType = item.parentEntityType;
		//				entity.ParentEntity.EntityUid = (Guid)item.parentEntityUid;
		//			}
		//		}
		//		return entity;
		//	}
		//}
		public static Entity EntityGetFromEntityCache( string ctid )
		{
			Entity entity = new Entity();
			using ( var context = new EntityContext() )
			{
				EM.Entity_Cache item = context.Entity_Cache
						.FirstOrDefault( s => s.CTID == ctid.ToLower() );

				if ( item != null && item.Id > 0 )
				{
					entity.Id = item.Id;
					entity.EntityTypeId = item.EntityTypeId;
					entity.EntityType = item.EntityType;
					entity.EntityUid = item.EntityUid;
					entity.EntityBaseId = item.BaseId;
					entity.EntityBaseName = item.Name;
					entity.CTID = item.CTID ?? "";

					entity.Created = ( DateTime )item.Created;
					entity.LastUpdated = ( DateTime )item.LastUpdated;
					if ( item.parentEntityId > 0 )
					{
						//NOTE	- can use the included Entity to get more info
						//		- although may want to turn off lazy loading
						entity.ParentEntity = new ThisEntity();
						entity.ParentEntity.Id = item.parentEntityId ?? 0;
						entity.ParentEntity.EntityTypeId = item.parentEntityTypeId ?? 0;
						entity.ParentEntity.EntityType = item.parentEntityType;
						entity.ParentEntity.EntityUid = ( Guid )item.parentEntityUid;
					}
				}
				return entity;
			}
		}

		/// <summary>
		/// Look up for resolving a third party entity
		/// NOTE: entityTypeId will often be zero (as unknown at time), 
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
				//20-12-16 NOTE: the swp can be null now. It could be a risk to allow just a match on name and type. Could add whether a reference
				var item = context.Entity_Cache
						.FirstOrDefault( s => s.EntityTypeId == entityTypeId
						 && s.Name.ToLower() == (name ?? "").ToLower()
						 && s.SubjectWebpage == subjectWebpage );

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

		public static Entity EntityCacheGet( int entityTypeId, int entityBaseId )
		{
			Entity entity = new Entity();
			using ( var context = new EntityContext() )
			{
				var item = context.Entity_Cache
						.FirstOrDefault( s => s.EntityTypeId == entityTypeId
						 && s.BaseId == entityBaseId );

				if ( item != null && item.Id > 0 )
				{
					entity.Id = item.Id;
					entity.EntityTypeId = item.EntityTypeId;
					entity.EntityType = item.EntityType;
					entity.EntityUid = item.EntityUid;
					entity.EntityBaseId = item.BaseId;
					entity.EntityBaseName = item.Name;
					entity.Created = ( DateTime )item.Created;
					entity.LastUpdated = ( DateTime )item.LastUpdated;
					if ( item.parentEntityId > 0 )
					{
						//NOTE	- can use the included Entity to get more info
						//		- although may want to turn off lazy loading
						entity.ParentEntity = new ThisEntity();
						entity.ParentEntity.Id = item.parentEntityId ?? 0;
						entity.ParentEntity.EntityTypeId = item.parentEntityTypeId ?? 0;
						entity.ParentEntity.EntityType = item.parentEntityType;
						entity.ParentEntity.EntityUid = ( Guid )item.parentEntityUid;
					}
				}
				return entity;
			}
		}

		#endregion

	}
}

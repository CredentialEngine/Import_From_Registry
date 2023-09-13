using System;
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

using ThisResource = workIT.Models.Common.Entity;
using DBResource = workIT.Data.Tables.Entity;
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

			DBResource efEntity = new DBResource();
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
                DBResource efEntity = context.Entity
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
                    DBResource efEntity = context.Entity
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
				DBResource item = context.Entity
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
        /// <summary>
        /// TBD - this should be a table look up?
        /// NEED TO SYNC WITH:
		///		TopLevelObject.EntityTypeId 
		///		Entity.EntityTypeId 
		///		
		/// NOT GOOD
        /// </summary>
        /// <param name="entityTypeId"></param>
        /// <returns></returns>
        //		- need to only have one method to do this resolution!!!!!
        public static string GetEntityType( int entityTypeId )
		{
			string entityType = "error";
			switch ( entityTypeId )
			{
				case 1:
					entityType="Credential";
					break;
                //org types
                case 2:
					entityType = "CredentialOrganization";
					break;
                case 13:
                    entityType = "QACredentialOrganization";
                    break;
                case 14:
                    entityType = "Organization";
                    break;
                //
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
				//lopp types
				case 7:
					entityType = "LearningOpportunity";
					break;
				case 36:
					entityType = "LearningProgram";
					break;
				case 37:
					entityType = "Course";
					break;
				//
				case 8:  
					entityType = "Pathway";
					break;
				case 9:    //
					entityType = "Collection";
					break;
				case 10:
					entityType = "CompetencyFramework";
					break;
				case 11:   
					entityType = "ConceptScheme";
					break;
                case 12:
                    entityType = "ProgressionModel";
                    break;
				//case 13:
				//case 14: 

                case 15:
                    entityType = "ScheduledOffering";
                    break;
                case 16:
					entityType = "Address Profile";
					break;
                case 17:
                    entityType = "Competency";
                    break;
                case 18:
					entityType = "JurisdictionProfile";
					break;
				case 19:
					entityType = "ConditionManifest";
					break;
				//
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
				case 27:
					entityType = "AggregateDataProfile";
					break;
				case 28:
					entityType = "TransferIntermediary";
					break;
                case 29:
                    entityType = "Concept";
                    break;
				//
                case 30:
                    entityType = "ProgressionLevel";
                    break;
                case 31:
					entityType = "DataSet Profile";
					break;
				case 32:
					entityType = "Job";
					break;
				case 33:
					entityType = "Task";
					break;
				case 34:
					entityType = "Work Role";
					break;
				case 35:
					entityType = "Occupation";
					break;
                //
                case 38:
                    entityType = "SupportService";
                    break;
                //
                case 40:
                    entityType = "RevocationProfile";
                    break;
                case 41:
                    entityType = "VerificationProfile";
                    break;
                case 42:
                    entityType = "ProcessProfile";
                    break;
                case 43:
                    entityType = "ContactPoint";
                    break;
                case 58:
                    entityType = "Rubric";
                    break;
                //obsolete/not used
                case 55:
					entityType = "EarningsProfile";
					break;
				case 56:
					entityType = "EmploymentOutcomeProfile";
					break;
				case 57:
					entityType = "HoldersProfile";
					break;

				default:
					LoggingHelper.LogError( string.Format( "{0}.GetEntityType. Invalid Entity type encountered: {1} ", thisClassName, entityTypeId ) );
					break;
			}

			return entityType;
		}
        public static int GetEntityTypeId( string entityType, bool getTopLevelType = true )
        {
            int entityTypeId = 0;

            switch ( entityType.Replace( " ", "" ).ToLower() )
            {
                case "credential":
                    entityTypeId = 1;
                    break;
                case "ceterms:credentialorganization":
                case "ceterms:qacredentialorganization":        //inconsistent with lopp where latter has separate entityTypeIds
                case "ceterms:organization":
                case "organization":
                case "credentialorganization":
                    entityTypeId = 2;
                    break;
                case "ceterms:assessmentprofile":
                case "assessmentprofile":
                case "assessment":
                    entityTypeId = 3;
                    break;
                case "ceterms:conditionprofile":
                case "conditionprofile":
                    entityTypeId = 4;
                    break;
                case "ceterms:costprofile":
                case "costprofile":
                    entityTypeId = 5;
                    break;
                case "ceterms:costprofileitem":
                case "costprofileitem":
                    entityTypeId = 6;
                    break;
                case "ceterms:learningopportunityprofile":
                case "learningopportunityprofile":
                case "learningopportunity":
                    entityTypeId = 7;
                    break;
                case "ceterms:course":
                case "course":
                    entityTypeId = 37;      //TBD if context should always be lopp
                    break;
                case "ceterms:learningprogram":
                case "learningprogram":
                    entityTypeId = 36;
                    break;
                case "ceterms:pathway":
                case "pathway":
                    entityTypeId = 8;
                    break;
                case "ceterms:collection":
                case "collection":
                    entityTypeId = 9;
                    break;
                case "ceasn:competencyframework":
                case "competencyframework":
                    //ISSUE - still have references to 17 in places for CaSS competencies
                    entityTypeId = 10;
                    break;
                case "skos:conceptscheme":
                case "conceptscheme":
                    entityTypeId = 11;
                    break;
                case "asn:progressionmodel":
                case "progressionmodel":
                    entityTypeId = CodesManager.ENTITY_TYPE_PROGRESSION_MODEL;
                    break;
                case "ceterms:scheduledoffering":
                case "scheduledoffering":
                    entityTypeId = CodesManager.ENTITY_TYPE_SCHEDULED_OFFERING;
                    break;
                case "ceterms:conditionmanifest":
                case "conditionmanifest":
                    entityTypeId = 19;
                    break;
                case "ceterms:costmanifest":
                case "costmanifest":
                    entityTypeId = 20;
                    break;
                case "ceterms:pathwayset":
                case "pathwayset":
                    entityTypeId = 23;
                    break;
                case "ceterms:pathwaycomponent":
                case "pathwaycomponent":
                    entityTypeId = CodesManager.ENTITY_TYPE_PATHWAY_COMPONENT;
                    break;
                case "ceterms:transfervalueprofile":
                case "transfervalueprofile":
                case "transfervalue":
                    entityTypeId = 26;
                    break;
                case "ceterms:transferintermediary":
                case "transferintermediary":
                    entityTypeId = 28;
                    break;
                case "qdata:datasetprofile":
                case "datasetprofile":
                    entityTypeId = 31;
                    break;
                case "ceterms:job":
                case "job":
                    entityTypeId = 32;
                    break;
                case "ceterms:task":
                case "task":
                    entityTypeId = 33;
                    break;
                case "ceterms:workrole":
                case "workrole":
                    entityTypeId = 34;
                    break;
                case "ceterms:occupation":
                case "occupation":
                    entityTypeId = 35;
                    break;
                case "ceterms:supportservice":
                case "supportservice":
                    entityTypeId = 38;
                    break;
                case "asn:rubric":
                case "rubric":
                    entityTypeId = CodesManager.ENTITY_TYPE_RUBRIC;
                    break;
                default:
                    //no default
                    entityTypeId = 0;
                    break;
            }
            return entityTypeId;
        }

        public static Entity GetEntity( int entityId)
		{
			Entity entity = new Entity();
			using ( var context = new EntityContext() )
			{
				DBResource item = context.Entity
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
				DBResource item = context.Entity
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
		/// <summary>
		/// Add record to Entity.cache
		/// </summary>
		/// <param name="input"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public int EntityCacheSave( EntityCache input, ref string statusMessage )
		{
			LoggingHelper.DoTrace( 6, string.Format( "EntityCacheSave entered. EntityTypeId:{0}, CTID: '{1}', BaseId: {2}, Name: {3}", input.EntityTypeId, input.CTID, input.BaseId, input.Name) );

			DBEntityCache efEntity = new DBEntityCache();
			var isActive = true;

			//check the related Entity.Id
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
				} else
				{
					//if not found, there is no entity, so can't continue
					statusMessage = string.Format( "Error - could not find an entity for EntityTypeId:{0}, BaseId:{1} ", input.EntityTypeId, input.BaseId );
					return 0;
				}
			}
			if (input.OwningOrgId==0)
			{
				var org = OrganizationManager.GetBasics( input.OwningAgentUID );
				if (org != null && org.Id > 0)
				{
					input.OwningOrgId = org.Id;
					input.OwningAgentUID = org.RowId;
				}
			} 
			
			if ( !IsGuidValid(input.OwningAgentUID))
            {
                var org = OrganizationManager.GetForSummary( input.OwningOrgId );
                if (org != null && org.Id > 0)
                    input.OwningAgentUID = org.RowId;
            }
            using ( var context = new EntityContext() )
			{
				try
				{
					efEntity = context.Entity_Cache.FirstOrDefault( s => s.Id == input.Id || s.EntityUid == input.EntityUid );
					if ( efEntity == null ||  efEntity.Id == 0)
					{
						efEntity = new DBEntityCache
						{
							Id = input.Id,//entityId
							EntityTypeId = input.EntityTypeId,
							EntityType = input.EntityType,
							EntityUid = input.EntityUid,
							//
							BaseId = input.BaseId,
							CTID = input.CTID,
							Name = input.Name,
							EntityStateId = input.EntityStateId,
							IsActive  = input.IsActive,		//caller will set to false for ceased/deprecated resource
							Description = input.Description ?? "",
							SubjectWebpage = input.SubjectWebpage ?? "",
							ImageUrl = input.ImageUrl ?? ""
						};
						efEntity.SubjectWebpage = input.SubjectWebpage ?? "";
						efEntity.OwningOrgId = input.OwningOrgId;
						//PublishedByOrganizationId = input.PublishedByOrganizationId;
						//not sure we really need this
						efEntity.parentEntityId = input.ParentEntityId;
						efEntity.parentEntityType = input.ParentEntityType;
						efEntity.parentEntityTypeId = input.ParentEntityTypeId;
						efEntity.parentEntityUid = input.ParentEntityUid;
						//unlikely on initial add, but may have a utility to populate
                        efEntity.ResourceDetail = input.ResourceDetail;
                        //
                        if ( input.Created > new DateTime( 1900, 1, 1 ) )
							efEntity.Created = efEntity.LastUpdated = input.Created;
						else
							efEntity.Created = efEntity.LastUpdated = DateTime.Now;
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
						//these cannot be updated - unless null?
						//efEntity.EntityTypeId = input.EntityTypeId;
						//efEntity.EntityType = input.EntityType;
						//efEntity.EntityUid = input.EntityUid;
						//
						//efEntity.BaseId = input.BaseId;
						//maybe if was a pending, 
						if(!string.IsNullOrWhiteSpace( input.CTID ) && input.CTID.Length == 39 && input.Name.IndexOf( "Placeholder until full document is downloaded" ) == -1 )
						{
							input.EntityStateId = 3;
						}
						efEntity.CTID = input.CTID;
						
						efEntity.Name = input.Name;
						efEntity.EntityStateId = input.EntityStateId;
						efEntity.IsActive = input.IsActive;

						efEntity.Description = input.Description ?? "";
						efEntity.SubjectWebpage = input.SubjectWebpage ?? "";
						efEntity.ImageUrl = input.ImageUrl ?? "";
						efEntity.OwningOrgId = input.OwningOrgId;
						//don't over write
						//efEntity.PublishedByOrganizationId = input.PublishedByOrganizationId > 0 ? input.PublishedByOrganizationId : efEntity.PublishedByOrganizationId;
						//
						efEntity.parentEntityId = input.ParentEntityId;
						efEntity.parentEntityType = input.ParentEntityType;
						efEntity.parentEntityTypeId = input.ParentEntityTypeId;
						efEntity.parentEntityUid = input.ParentEntityUid;
                        //retain resourceDetail if not provided 
                        if ( !string.IsNullOrWhiteSpace( input.ResourceDetail) )
						{
							efEntity.ResourceDetail = input.ResourceDetail;
						}
                        //
                        if ( input.LastUpdated > new DateTime( 1900, 1, 1 ) )
							efEntity.LastUpdated = input.LastUpdated;
						else
							efEntity.LastUpdated = DateTime.Now;
						//
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
								string message = statusMessage = string.Format( thisClassName + ".Save Failed", "Attempted to update an Entity_Cache. The process appeared to not work, but was not an exception, so we have no message, or no clue. Entity_Cache: Type: {0}, Name: {1}, BaseId: {2}", input.EntityType, input.Name, input.BaseId );
								EmailManager.NotifyAdmin( thisClassName + ".Save Failed Failed", message );
							}
						}
							
						
						return efEntity.Id;
					}					
				}
				catch ( Exception ex )
				{
					statusMessage = FormatExceptions( ex );
					LoggingHelper.LogError( ex, string.Format( thisClassName + ".EntityCacheSave(). Entity_Cache: Type: {0}, Name: {1}, BaseId: {2}", input.EntityType, input.Name, input.BaseId ), statusMessage, false );
				}
			}

			return 0;
		}

		/// <summary>
		/// Update Entity.Cache LastUpdatedDate base on the envelope last verified date.
		/// </summary>
		/// <param name="ctid"></param>
		/// <param name="lastVerifiedDate"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool EntityCacheUpdateLastUpdatedDate( string ctid, DateTime lastVerifiedDate, ref string statusMessage )
		{

			DBEntityCache efEntity = new DBEntityCache();
			if ( string.IsNullOrWhiteSpace( ctid ) )
			{
				statusMessage = $"EntityCacheUpdateLastUpdatedDate Error: a valid CTID ({ctid}) was not provided.";
				return false;
			}
			if ( lastVerifiedDate== null || lastVerifiedDate == System.DateTime.MinValue )
			{
				statusMessage = $"EntityCacheUpdateLastUpdatedDate Error: for CTID ({ctid}) a valid date ({lastVerifiedDate}) was not provided.";
				return false;
			}

			TimeZone zone = TimeZone.CurrentTimeZone;
			// Demonstrate ToLocalTime and ToUniversalTime.
			var localDate = zone.ToLocalTime( lastVerifiedDate );

			using ( var context = new EntityContext() )
			{
				try
				{
					efEntity = context.Entity_Cache.FirstOrDefault( s => s.CTID.ToLower() == ctid.ToLower() );
					if ( efEntity == null || efEntity.Id == 0 )
					{
						//error
						statusMessage = $"EntityCacheUpdateLastUpdatedDate Error: An Entity.Cache record was not found for CTID: '{ctid}'.";
						return false;
					}
					else
					{
						//
						efEntity.LastUpdated = localDate;
						//
						if ( HasStateChanged( context ) )
						{
							//not sure if we want to update this date? It relates to the import
							//efEntity.CacheDate = System.DateTime.Now;

							int count = context.SaveChanges();
							if ( count == 0 )
							{
								//?no info on error
								string message = statusMessage = string.Format( thisClassName + ".EntityCacheUpdateLastUpdatedDate Failed", "Attempted to update an Entity_Cache.LastUpdated. The process appeared to not work, but was not an exception, so we have no message, or no clue. Type: {0}, Name: {1}, BaseId: {2}", efEntity.EntityType, efEntity.Name, efEntity.BaseId );
								EmailManager.NotifyAdmin( thisClassName + ".EntityCacheUpdateLastUpdatedDate Failed Failed", message );
								return false;
							}
						}
					}
				}
				//catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				//{
				//	string message = HandleDBValidationError( dbex, thisClassName + ".EntityCacheUpdateLastUpdatedDate() ", "Entity" );
				//	statusMessage = "Error - the save was not successful. " + message;
				//	return false;
				//}
				catch ( Exception ex )
				{
					statusMessage = FormatExceptions( ex );
					LoggingHelper.LogError( ex, string.Format( thisClassName + ".EntityCacheUpdateLastUpdatedDate(). Type: {0}, Name: {1}, BaseId: {2}", efEntity.EntityType, efEntity.Name, efEntity.BaseId ), statusMessage, false );
					return false;
				}
			}
			return true;	
		}

		public int EntityCacheUpdateResourceDetail( string ctid, string input, ref string statusMessage )
        {           

            DBEntityCache efEntity = new DBEntityCache();
            if ( string.IsNullOrWhiteSpace(ctid) )
            {
				statusMessage = $"EntityCacheUpdateResourceDetail Error: a valid CTID ({ctid}) was not provided.";
				return 0;
            }
            if ( string.IsNullOrWhiteSpace( input ) )
            {
                statusMessage = $"EntityCacheUpdateResourceDetail Error: for CTID ({ctid}) no ResourceDetail data was provided.";
                return 0;
            }
            using ( var context = new EntityContext() )
            {
                try
                {
                    efEntity = context.Entity_Cache.FirstOrDefault( s => s.CTID.ToLower() == ctid.ToLower() );
                    if ( efEntity == null || efEntity.Id == 0 )
                    {
                        //error
                        statusMessage = $"EntityCacheUpdateResourceDetail Error: An Entity.Cache record was not found for CTID: '{ctid}'.";
                        return 0;
                    }
                    else
                    {
						//
                        efEntity.ResourceDetail = input;
                        //
                        if ( HasStateChanged( context ) )
                        {
							//not sure if we want to update this date? It relates to the import
                            //efEntity.CacheDate = System.DateTime.Now;

                            int count = context.SaveChanges();
                            if ( count >= 0 )
                            {
                                return efEntity.Id;
                            }
                            else
                            {
                                //?no info on error
                                string message = statusMessage = string.Format( thisClassName + ".Save Failed", "Attempted to update an Entity_Cache.ResourceDetail. The process appeared to not work, but was not an exception, so we have no message, or no clue. Type: {0}, Name: {1}, BaseId: {2}", efEntity.EntityType, efEntity.Name, efEntity.BaseId );
                                EmailManager.NotifyAdmin( thisClassName + ".Save Failed Failed", message );
                            }
                        }
                        return efEntity.Id;
                    }
                }
                catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
                {
                    string message = HandleDBValidationError( dbex, thisClassName + ".EntityCacheUpdateResourceDetail() ", "Entity" );
                    statusMessage = "Error - the save was not successful. " + message;
                }
                catch ( Exception ex )
                {
                    statusMessage = FormatExceptions( ex );
                    LoggingHelper.LogError( ex, string.Format( thisClassName + ".EntityCacheUpdateResourceDetail(). Type: {0}, Name: {1}, BaseId: {2}", efEntity.EntityType, efEntity.Name, efEntity.BaseId ), statusMessage, false );
                }
            }
            return 0;
        }
        /// <summary>
        /// /// Update Entity_Cache.AgentRelationshipsForEntity for credential
        /// </summary>
        /// <param name="ctid"></param>
        /// <param name="statusMessage"></param>
        /// <returns></returns>
        public bool EntityCacheUpdateCredentialAgentRelationships( string ctid,  ref string statusMessage )
        {

            string connectionString = MainConnection();
            try
            {
                using ( SqlConnection c = new SqlConnection( connectionString ) )
                {
                    c.Open();

                    using ( SqlCommand command = new SqlCommand( "[Entity_Cache_PopulateCredentialAgentRelationships]", c ) )
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.Add( new SqlParameter( "@ResourceCTID", ctid ) );
                        command.CommandTimeout = 300;
                        command.ExecuteNonQuery();
                        command.Dispose();
                        c.Close();
                    }
                }
				return true;
            }
            catch ( Exception ex )
            {
				statusMessage = FormatExceptions( ex );
                LoggingHelper.LogError( ex, "EntityCacheUpdateCredentialAgentRelationships", false );
				return false;
            }
        }
        /// <summary>
        /// Update Entity_Cache.AgentRelationshipsForEntity for learning opp
        /// </summary>
        /// <param name="ctid"></param>
        /// <param name="statusMessage"></param>
        /// <returns></returns>
        public bool EntityCacheUpdateLoppAgentRelationships( string ctid, ref string statusMessage )
        {

            string connectionString = MainConnection();
            try
            {
                using ( SqlConnection c = new SqlConnection( connectionString ) )
                {
                    c.Open();

                    using ( SqlCommand command = new SqlCommand( "[Entity_Cache_PopulateLearningOppAgentRelationships]", c ) )
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.Add( new SqlParameter( "@ResourceCTID", ctid ) );
                        command.CommandTimeout = 300;
                        command.ExecuteNonQuery();
                        command.Dispose();
                        c.Close();
                    }
                }
                return true;
            }
            catch ( Exception ex )
            {
                statusMessage = FormatExceptions( ex );
                LoggingHelper.LogError( ex, "EntityCacheUpdateCredentialAgentRelationships", false );
                return false;
            }
        }
        public bool EntityCacheDelete( int entityTypeId, int recordId, ref string statusMessage )
		{

			DBEntityCache efEntity = new DBEntityCache();
			//check the related Entity.Id
			if ( entityTypeId == 0 || recordId == 0 )
			{
				statusMessage = thisClassName + string.Format( ".EntityCacheDelete. Invalid request, missing parameters. entityTypeId: {0}, recordId: {1}", entityTypeId, recordId );
				return false;
			}

			using ( var context = new EntityContext() )
			{
				try
				{
					efEntity = context.Entity_Cache.FirstOrDefault( s => s.EntityTypeId == entityTypeId && s.BaseId == recordId );
					if ( efEntity == null || efEntity.Id == 0 )
					{
						statusMessage = thisClassName + string.Format( ".EntityCacheDelete. Rerquested record was not found. EntityTypeId: {0}, recordId: {1}", entityTypeId, recordId );
						return false;
					}
					else
					{
						//update or delete?
						bool doingUpdate = false;
						if ( doingUpdate )
						{
							efEntity.EntityStateId = 0;
							efEntity.LastUpdated = DateTime.Now;
							efEntity.CacheDate = System.DateTime.Now;
							int count = context.SaveChanges();
							//can be zero if no data changed
							if ( count >= 0 )
							{
								return true;
							}
							else
							{
								//?no info on error
								statusMessage = string.Format( thisClassName + ".Virtual delete Failed", "Attempted to delete an Entity_Cache. The process appeared to not work, but was not an exception, so we have no message, or no clue. Entity_Cache: EntityTypeId: {0}, recordId: {1}", entityTypeId, recordId );
								
								return false;
							}
							//

						} else
						{
							context.Entity_Cache.Remove( efEntity );
							var count = context.SaveChanges();
							if ( count >= 0 )
							{
								return true;
							}
							else
							{
								//?no info on error
								statusMessage = string.Format( thisClassName + ".Delete Failed", "Attempted to delete an Entity_Cache. The process appeared to not work, but was not an exception, so we have no message, or no clue. Entity_Cache: EntityTypeId: {0}, recordId: {1}", entityTypeId, recordId );

								return false;
							}
						}
						//
						
					}
				}
				catch ( Exception ex )
				{
					statusMessage = FormatExceptions( ex );
					LoggingHelper.LogError( ex, string.Format( thisClassName + ".Delete(). Entity_Cache: EntityTypeId: {0}, recordId: {1}", entityTypeId, recordId ), statusMessage, false );
				}
			}

			return false;
		}

        public bool EntityCacheDelete( Guid entityUid, ref string statusMessage )
        {

            DBEntityCache efEntity = new DBEntityCache();
            using ( var context = new EntityContext() )
            {
                try
                {
                    efEntity = context.Entity_Cache.FirstOrDefault( s => s.EntityUid == entityUid );
                    if ( efEntity == null || efEntity.Id == 0 )
                    {
                        statusMessage = thisClassName + string.Format( ".EntityCacheDelete. Rerquested record was not found. EntityUid: {0}", entityUid );
                        return false;
                    }
                    else
                    {
                        //update or delete?
                        bool doingUpdate = false;
                        if ( doingUpdate )
                        {
                            efEntity.EntityStateId = 0;
                            efEntity.LastUpdated = DateTime.Now;
                            efEntity.CacheDate = System.DateTime.Now;
                            int count = context.SaveChanges();
                            //can be zero if no data changed
                            if ( count >= 0 )
                            {
                                return true;
                            }
                            else
                            {
                                //?no info on error
                                statusMessage = string.Format( thisClassName + ".Virtual delete Failed", "Attempted to delete an Entity_Cache. The process appeared to not work, but was not an exception, so we have no message, or no clue. Entity_Cache: EntityUid: {0}", entityUid );

                                return false;
                            }
                            //

                        }
                        else
                        {
                            context.Entity_Cache.Remove( efEntity );
                            var count = context.SaveChanges();
                            if ( count >= 0 )
                            {
                                return true;
                            }
                            else
                            {
                                //?no info on error
                                statusMessage = string.Format( thisClassName + ".Delete Failed", "Attempted to delete an Entity_Cache. The process appeared to not work, but was not an exception, so we have no message, or no clue. Entity_Cache: EntityUid: {0}", entityUid );

                                return false;
                            }
                        }
                        //

                    }
                }
                catch ( Exception ex )
                {
                    statusMessage = FormatExceptions( ex );
                    LoggingHelper.LogError( ex, string.Format( thisClassName + ".Delete(). Entity_Cache: EntityUid: {0}", entityUid ), statusMessage, false );
                }
            }

            return false;
        }

        public static EntityCache EntityCacheGetByCTID( string ctid )
		{
			var entity = new EntityCache();
			using ( var context = new EntityContext() )
			{
				//doing a left join just in case the org is missing (most likely in testing)
                var results = ( from record in context.Entity_Cache
                                join org in context.Organization on record.OwningOrgId equals org.Id into gj
                                from subOrg in gj.DefaultIfEmpty()
                                where record.parentEntityId == 0
                                && record.CTID == ctid
                                //&& subOrg.EntityStateId >= 1 && record.EntityStateId == 3
                                select new
                                {
                                    record.Id,
                                    record.CTID,
                                    record.BaseId,
                                    record.EntityType,
                                    record.EntityTypeId,
                                    record.EntityUid,
                                    record.Name,
                                    record.EntityStateId,
                                    record.OwningOrgId,
                                    //TBD
                                    //Organization = subOrg.Name ?? "",
                                    OrganizationCTID = subOrg.CTID ?? "",
                                    record.LastUpdated,
                                } ).ToList();

                if ( results != null && results.Count > 0 )
                {
                    foreach ( var ec in results )
                    {
                        entity = new EntityCache()
                        {
                            //Organization = ec.Organization,
                            OwningOrgCTID = ec.OrganizationCTID,

                            Id = ec.Id,
                            CTID = ec.CTID,
                            EntityType = ec.EntityType,
                            EntityTypeId = ec.EntityTypeId,
                            EntityStateId = ( int ) ec.EntityStateId,
                            BaseId = ec.BaseId,
                            Name = ec.Name,
                            OwningOrgId = ec.OwningOrgId ?? 0,
                            EntityUid = ec.EntityUid,
                            LastUpdated = ( DateTime ) ec.LastUpdated
                        };
                        break;
                    }
                }
                return entity;
			}
		}
		public static EntityCache EntityCacheGetByGuid( Guid id )
		{
			var ec = new EntityCache();
			using ( var context = new EntityContext() )
			{
				EM.Entity_Cache item = context.Entity_Cache
						.FirstOrDefault( s => s.EntityUid == id);

				if ( item != null && item.Id > 0 )
				{
					ec.Id = item.Id;
					ec.EntityTypeId = item.EntityTypeId;
					ec.EntityStateId = item.EntityStateId ==  null ? 1 : (int) item.EntityStateId;
					ec.EntityType = item.EntityType;
					ec.EntityUid = item.EntityUid;
					ec.BaseId = item.BaseId;
					ec.Name = item.Name;
					ec.CTID = item.CTID ?? "";

					ec.Created = ( DateTime )item.Created;
					ec.LastUpdated = ( DateTime )item.LastUpdated;
					if ( (item.parentEntityId ?? 0) > 0 )
					{
						//NOTE	- can use the included Entity to get more info
						//		- although may want to turn off lazy loading
						//entity.ParentEntity = new ThisResource();
						ec.ParentEntityId = item.parentEntityId ?? 0;
						ec.ParentEntityTypeId = item.parentEntityTypeId ?? 0;
						ec.ParentEntityType = item.parentEntityType;
						ec.ParentEntityUid = ( Guid )item.parentEntityUid;
					}
				}
				return ec;
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
						entity.ParentEntity = new ThisResource();
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
						entity.ParentEntity = new ThisResource();
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

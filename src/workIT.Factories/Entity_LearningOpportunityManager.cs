using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using workIT.Models;
using workIT.Models.Common;
using ThisEntity = workIT.Models.ProfileModels.LearningOpportunityProfile;
using DBEntity = workIT.Data.Tables.Entity_LearningOpportunity;
using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;

using workIT.Utilities;
using CM = workIT.Models.Common;

using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;

namespace workIT.Factories
{
	public class Entity_LearningOpportunityManager : BaseFactory
	{
		static string thisClassName = "Entity_LearningOpportunityManager";


		#region Entity LearningOpp Persistance ===================
		public bool SaveList( List<int> list, Guid parentUid, ref SaveStatus status, int relationshipTypeId = 1 )
		{
			if ( list == null || list.Count == 0 )
				return true;
			int newId = 0;

			bool isAllValid = true;
			foreach ( int item in list )
			{
				newId = Add( parentUid, item, relationshipTypeId, false, ref status );
				if ( newId == 0 )
					isAllValid = false;
			}

			return isAllValid;
		}
		/// <summary>
		/// Add a learning opp to a parent (typically a stub was created, so can be associated before completing the full profile)
		/// </summary>
		/// <param name="parentUid"></param>
		/// <param name="learningOppId">The just create lopp</param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public int Add( Guid parentUid, 
					int learningOppId, int relationshipTypeId,
					bool allowMultiples,
					ref SaveStatus status,
					bool warnOnDuplicate = true
			)
		{
			int id = 0;
			int count = 0;
			if ( learningOppId == 0 )
			{
				status.AddError( string.Format( "A valid Learning Opportunity identifier was not provided to the {0}.Add method.", thisClassName ) );
				return 0;
			}
			if ( relationshipTypeId == 0 )
				relationshipTypeId = 1;

			Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				status.AddError( "Error - the parent entity was not found.");
				return 0;
			}

			using ( var context = new EntityContext() )
			{
				DBEntity efEntity = new DBEntity();
				try
				{
					//first check for duplicates
					efEntity = context.Entity_LearningOpportunity
							.FirstOrDefault( s => s.EntityId == parent.Id && s.LearningOpportunityId == learningOppId && s.RelationshipTypeId == relationshipTypeId );

					if ( efEntity != null && efEntity.Id > 0 )
					{
						if ( warnOnDuplicate )
						{
							status.AddWarning( string.Format( "Error - this Learning Opportunity has already been added to this profile.", thisClassName ) );
						}
						id = efEntity.Id;
						return id;
					}

					efEntity = new DBEntity();
					efEntity.EntityId = parent.Id;
					efEntity.LearningOpportunityId = learningOppId;
					efEntity.RelationshipTypeId = relationshipTypeId > 0 ? relationshipTypeId : 1;
					efEntity.Created = System.DateTime.Now;

					context.Entity_LearningOpportunity.Add( efEntity );

					// submit the change to database
					count = context.SaveChanges();
					if ( count > 0 )
					{
						id = efEntity.Id;
						return efEntity.Id;
					}
					else
					{
						//?no info on error
						
						string message = thisClassName + string.Format( ".Add Failed", "Attempted to add a LearningOpp for a connection profile. The process appeared to not work, but there was no exception, so we have no message, or no clue. Parent Profile: {0}, learningOppId: {1}", parent.Id, learningOppId );
						status.AddError( thisClassName + ". Error - the add was not successful. " + message );
						EmailManager.NotifyAdmin( thisClassName + ".Add Failed", message );
					}
				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", "Entity_LearningOpp" );
					status.AddError( thisClassName + ". Error - the Add was not successful. " + message );
					LoggingHelper.LogError( dbex, thisClassName + string.Format( ".Save(), Parent: {0} ({1})", parent.EntityBaseName, parent.EntityBaseId ) );

				}
				catch ( Exception ex )
				{
					string message = FormatExceptions( ex );
					status.AddError( thisClassName + "Error - the Add was not successful. " + message );
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Add(), Parent: {0} ({1})", parent.EntityBaseName, parent.EntityBaseId ) );
				}


			}
			return id;
		}

       /// <summary>
	   /// Delete all relationships for parent
	   /// NOTE: there should be a check for reference entities, and delete if no other references.
	   /// OR: have a clean up process to delete orphans. 
	   /// </summary>
	   /// <param name="parent"></param>
	   /// <param name="status"></param>
	   /// <returns></returns>
        public bool DeleteAll( Entity parent, ref SaveStatus status )
        {
            bool isValid = true;
			int count = 0;
            if ( parent == null || parent.Id == 0 )
            {
                status.AddError( thisClassName + ". Error - the provided target parent entity was not provided." );
                return false;
            }
			using ( var context = new EntityContext() )
			{
				//check if target is a reference object and is only in use here
				var results = context.Entity_LearningOpportunity
							.Where( s => s.EntityId == parent.Id )
							.OrderBy( s => s.LearningOpportunity.Name )
							.ToList();
				if ( results == null || results.Count == 0 )
				{
					return true;
				}

				foreach ( var item in results )
				{
					//if a reference, delete actual LearningOpportunity if not used elsewhere
					if ( item.LearningOpportunity != null && item.LearningOpportunity.EntityStateId == 2 )
					{
						//do a getall. If only one, delete it.
						var exists = context.Entity_LearningOpportunity
							.Where( s => s.LearningOpportunityId == item.LearningOpportunityId )
							.ToList();
						if ( exists != null && exists.Count() == 1 )
						{
							var statusMsg = "";
							//this method will also add pending request to remove from elastic.
							//20-12-18 mp - Only done for a reference lopp but what about a full lopp that may now be an orphan? We are not allowing lopps without parent, but will still exist in registry!!!
							//actually this delete will probably also delete the Entity_LearningOpportunity
							//new LearningOpportunityManager().Delete( item.LearningOpportunityId, ref statusMsg );
							//continue;
						}
					}
					context.Entity_LearningOpportunity.Remove( item );
					count = context.SaveChanges();
					if ( count > 0 )
					{ 

					}
				}
            }

            return isValid;
        }
		#endregion

		public static List<ThisEntity> GetAllSummary( Guid parentUid, int relationshipTypeId )
		{
			//note even the summary should include indicator of competencies
			return LearningOpps_GetAll( parentUid, false, false,  relationshipTypeId );
		}
		/// <summary>
		/// Get all learning opportunties for the parent
		/// Uses the parent Guid to retrieve the related Entity, then uses the EntityId to retrieve the child objects.
		/// </summary>
		/// <param name="parentUid"></param>
		/// <param name="forProfilesList"></param>
		/// <returns></returns>
		public static List<ThisEntity> LearningOpps_GetAll( Guid parentUid,
					bool forProfilesList,
					bool isForCredentialDetails = false, 
					int relationshipTypeId = 1 )
		{
			List<ThisEntity> list = new List<ThisEntity>();
			ThisEntity entity = new ThisEntity();

			Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				return list;
			}
			//TODO - this was left over from the publisher, needs to be cleaned up
			bool includingProperties = isForCredentialDetails;
			bool includingProfiles = false;
			if ( isForCredentialDetails )
			{
				//don't want this when called from a condition profile!
				includingProperties = true;
				includingProfiles = true;
			}
			LoggingHelper.DoTrace( 7, string.Format( "Entity_LearningOpps_GetAll: parentUid:{0} entityId:{1}, e.EntityTypeId:{2}", parentUid, parent.Id, parent.EntityTypeId ) );
			try
			{
				using ( var context = new EntityContext() )
				{
					List<DBEntity> results = context.Entity_LearningOpportunity
							.Where( s => s.EntityId == parent.Id && ( relationshipTypeId == 0 || s.RelationshipTypeId == relationshipTypeId ) )
							.OrderBy( s => s.LearningOpportunity.Name )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( DBEntity item in results )
						{
							entity = new ThisEntity();
                            if ( item.LearningOpportunity != null && item.LearningOpportunity.EntityStateId > 1 )
                            {
                                if ( forProfilesList || isForCredentialDetails )
                                {

									LearningOpportunityManager.MapFromDB_Basic( item.LearningOpportunity, entity );

                                    if ( isForCredentialDetails )
                                    {
										entity.EstimatedDuration = DurationProfileManager.GetAll( entity.RowId );
										LearningOpportunityManager.MapFromDB_HasPart( entity, false );
                                        LearningOpportunityManager.MapFromDB_Competencies( entity );
                                    }
                                    list.Add( entity );

                                }
                                else
                                {
                                    if ( CacheManager.IsLearningOpportunityAvailableFromCache( item.LearningOpportunityId, ref entity ) )
                                    {
                                        list.Add( entity );
                                    }
                                    else
                                    {
                                        //TODO - is this section used??
										//to determine minimum needed for a or detail page
                                        LearningOpportunityManager.MapFromDB( item.LearningOpportunity, entity,
                                            includingProperties,
                                            includingProfiles,
                                            false //includeWhereUsed
                                            );
                                        list.Add( entity );
                                        if ( entity.HasPart.Count > 0 )
                                        {
                                            CacheManager.AddLearningOpportunityToCache( entity );
                                        }
                                    }
                                }
                            }
						}
					}
					return list;
				}


			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + string.Format(".LearningOpps_GetAll. Guid: {0}, parentType: {1} ({2}), ", parentUid, parent.EntityType, parent.EntityBaseId) );
			}
			return list;
		}
		/// <summary>
		/// Get all learning opportunties where the source learning opportunity is a part
		/// Steps: 1 use the learning opportunity Id to get All the Entity_LearningOpp, use the entity Uses the parent Guid to retrieve the related Entity, then uses the EntityId to retrieve the child objects.
		/// </summary>
		/// <param name="parentUid"></param>
		/// <returns></returns>
		public static List<ThisEntity> LearningOpps_GetAll_IsPart( int learningOpportunityId, int parentTypeId )
		{
			List<ThisEntity> list = new List<ThisEntity>();
			ThisEntity entity = new ThisEntity();

			try
			{
				using ( var context = new ViewContext() )
				{
					var results = context.Entity_LearningOpportunity_IsPartOfSummary
							.Where( s => s.LearningOpportunityId == learningOpportunityId
								&& s.EntityTypeId == parentTypeId )
							.OrderBy( s => s.EntityTypeId ).ThenBy( s => s.ParentName )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( Views.Entity_LearningOpportunity_IsPartOfSummary item in results )
						{
							entity = new ThisEntity();
							//LearningOpportunityManager.Entity_ToMap( item.LearningOpportunity, entity, false, false );


							list.Add( entity );
						}
					}
					return list;
				}


			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + string.Format( ".LearningOpps_GetAll_IsPart. learningOpportunityId: {0}, parentTypeId: {1}. ", learningOpportunityId, parentTypeId ) );

			}
			return list;
		}
	}
}

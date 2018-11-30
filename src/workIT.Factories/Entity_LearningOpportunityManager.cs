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
		/// <summary>
		/// Add a learning opp to a parent (typically a stub was created, so can be associated before completing the full profile)
		/// </summary>
		/// <param name="parentUid"></param>
		/// <param name="learningOppId">The just create lopp</param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public int Add( Guid parentUid, 
					int learningOppId, 
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
							.SingleOrDefault( s => s.EntityId == parent.Id && s.LearningOpportunityId == learningOppId );

					if ( efEntity != null && efEntity.Id > 0 )
					{
						if ( warnOnDuplicate )
						{
							status.AddWarning( string.Format( "Error - this Learning Opportunity has already been added to this profile.", thisClassName ) );
						}
						return 0;
					}

					if ( allowMultiples == false )
					{
						//check if one exists, and replace if found
						efEntity = context.Entity_LearningOpportunity
							.FirstOrDefault( s => s.EntityId == parent.Id );
						if ( efEntity != null && efEntity.Id > 0 )
						{
							efEntity.LearningOpportunityId = learningOppId;

							count = context.SaveChanges();

							return efEntity.Id;
						}
					}
					efEntity = new DBEntity();
					efEntity.EntityId = parent.Id;
					efEntity.LearningOpportunityId = learningOppId;
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
					status.AddError( "Error - the Add was not successful. " + message );
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Add(), Parent: {0} ({1})", parent.EntityBaseName, parent.EntityBaseId ) );
				}


			}
			return id;
		}

       
        public bool DeleteAll( Entity parent, ref SaveStatus status )
        {
            bool isValid = true;
            //Entity parent = EntityManager.GetEntity( parentUid );
            if ( parent == null || parent.Id == 0 )
            {
                status.AddError( thisClassName + ". Error - the provided target parent entity was not provided." );
                return false;
            }
            using ( var context = new EntityContext() )
            {
                context.Entity_LearningOpportunity.RemoveRange( context.Entity_LearningOpportunity.Where( s => s.EntityId == parent.Id ) );
                int count = context.SaveChanges();
                if ( count > 0 )
                {
                    isValid = true;
                }
                else
                {
                    //if doing a delete on spec, may not have been any properties
                }
            }

            return isValid;
        }
        #endregion


        /// <summary>
        /// Get all learning opportunties for the parent
        /// Uses the parent Guid to retrieve the related Entity, then uses the EntityId to retrieve the child objects.
        /// </summary>
        /// <param name="parentUid"></param>
        /// <param name="forProfilesList"></param>
        /// <returns></returns>
        public static List<ThisEntity> LearningOpps_GetAll( Guid parentUid,
					bool forProfilesList,
					bool isForCredentialDetails = false )
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
				includingProperties = true;
				includingProfiles = true;
			}
			LoggingHelper.DoTrace( 7, string.Format( "Entity_LearningOpps_GetAll: parentUid:{0} entityId:{1}, e.EntityTypeId:{2}", parentUid, parent.Id, parent.EntityTypeId ) );
			try
			{
				using ( var context = new EntityContext() )
				{
					List<DBEntity> results = context.Entity_LearningOpportunity
							.Where( s => s.EntityId == parent.Id )
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

									LearningOpportunityManager.MapFromDB_Basic( item.LearningOpportunity, entity,
								true );

									//entity.Id = item.LearningOpportunityId;
         //                           entity.RowId = item.LearningOpportunity.RowId;

         //                           entity.Name = item.LearningOpportunity.Name;
         //                           entity.Description = item.LearningOpportunity.Description == null ? "" : item.LearningOpportunity.Description;
									//entity.EntityStateId = ( int )( item.LearningOpportunity.EntityStateId ?? 1 );
									//entity.SubjectWebpage = item.LearningOpportunity.SubjectWebpage;
         //                           entity.CTID = item.LearningOpportunity.CTID;
                                    //also get costs - really only need the profile list view 
                                    //entity.EstimatedCost = CostProfileManager.GetAllForList( entity.RowId );
                                    //entity.CommonCosts = Entity_CommonCostManager.GetAll( entity.RowId );
                                    //get durations - need this for search and compare
                                    //entity.EstimatedDuration = DurationProfileManager.GetAll( entity.RowId );
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
				LoggingHelper.LogError( ex, thisClassName + ".LearningOpps_GetAll" );
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
					List<Views.Entity_LearningOpportunity_IsPartOfSummary> results = context.Entity_LearningOpportunity_IsPartOfSummary
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
				LoggingHelper.LogError( ex, thisClassName + ".LearningOpps_GetAll" );
			}
			return list;
		}
	}
}

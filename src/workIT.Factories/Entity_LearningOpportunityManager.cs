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

		/// <summary>
		/// Delete a learning opportunity from a parent entity
		/// </summary>
		/// <param name="parentId"></param>
		/// <param name="entityTypeId"></param>
		/// <param name="learningOppId"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		//public bool Delete( Guid parentUid, int learningOppId, ref string statusMessage )
		//{
		//	bool isValid = false;
		//	if ( learningOppId == 0 )
		//	{
		//		statusMessage = "Error - missing an identifier for the LearningOpp to remove";
		//		return false;
		//	}
		//	//need to get Entity.Id 
		//	Entity parent = EntityManager.GetEntity( parentUid );
		//	if ( parent == null || parent.Id == 0 )
		//	{
		//		statusMessage = "Error - the parent entity was not found.";
		//		return false;
		//	}
		//	using ( var context = new EntityContext() )
		//	{
		//		DBEntity efEntity = context.Entity_LearningOpportunity
		//						.SingleOrDefault( s => s.EntityId == parent.Id && s.LearningOpportunityId == learningOppId );

		//		if ( efEntity != null && efEntity.Id > 0 )
		//		{
		//			context.Entity_LearningOpportunity.Remove( efEntity );
		//			int count = context.SaveChanges();
		//			if ( count > 0 )
		//			{
		//				isValid = true;
		//			}
		//		}
		//		else
		//		{
		//			statusMessage = "Warning - the record was not found - probably because the target had been previously deleted";
		//			isValid = true;
		//		}
		//	}

		//	return isValid;
		//}

		#endregion


		/// <summary>
		/// Get all learning opportunties for the parent
		/// Uses the parent Guid to retrieve the related Entity, then uses the EntityId to retrieve the child objects.
		/// </summary>
		/// <param name="parentUid"></param>
		/// <param name="forEditView"></param>
		/// <param name="forProfilesList"></param>
		/// <returns></returns>
		public static List<ThisEntity> LearningOpps_GetAll( Guid parentUid,
					bool forEditView,
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

			bool includingProperties = isForCredentialDetails;
			bool includingProfiles = false;
			if ( !forEditView )
			{
				includingProperties = true;
				includingProfiles = true;
			}
			LoggingHelper.DoTrace( 5, string.Format( "Entity_LearningOpps_GetAll: parentUid:{0} entityId:{1}, e.EntityTypeId:{2}", parentUid, parent.Id, parent.EntityTypeId ) );
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
                                    entity.Id = item.LearningOpportunityId;
                                    entity.RowId = item.LearningOpportunity.RowId;

                                    entity.Name = item.LearningOpportunity.Name;
                                    entity.Description = item.LearningOpportunity.Description == null ? "" : item.LearningOpportunity.Description;

                                    entity.SubjectWebpage = item.LearningOpportunity.SubjectWebpage;
                                    entity.CTID = item.LearningOpportunity.CTID;
                                    //also get costs - really only need the profile list view 
                                    entity.EstimatedCost = CostProfileManager.GetAllForList( entity.RowId );

                                    //get durations - need this for search and compare
                                    entity.EstimatedDuration = DurationProfileManager.GetAll( entity.RowId );
                                    if ( isForCredentialDetails )
                                    {
                                        LearningOpportunityManager.MapFromDB_HasPart( entity, false, false );
                                        LearningOpportunityManager.MapFromDB_Competencies( entity );
                                    }
                                    list.Add( entity );

                                }
                                else
                                {
                                    if ( !forEditView
                                      && CacheManager.IsLearningOpportunityAvailableFromCache( item.LearningOpportunityId, ref entity ) )
                                    {
                                        list.Add( entity );
                                    }
                                    else
                                    {
                                        //to determine minimum needed for a or detail page
                                        LearningOpportunityManager.MapFromDB( item.LearningOpportunity, entity,
                                            includingProperties,
                                            includingProfiles,
                                            forEditView, //forEditView
                                            false //includeWhereUsed
                                            );
                                        list.Add( entity );
                                        if ( !forEditView && entity.HasPart.Count > 0 )
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

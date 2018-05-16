using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;
using ThisEntity = workIT.Models.ProfileModels.Entity_Assessment;
using DBEntity = workIT.Data.Tables.Entity_Assessment;
using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;

using workIT.Utilities;
using CM = workIT.Models.Common;
using workIT.Models.ProfileModels;
using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;

namespace workIT.Factories
{
	public class Entity_AssessmentManager : BaseFactory
	{
		static string thisClassName = "Entity_AssessmentManager";
		/// <summary>
		/// Get all assessments for the provided entity
		/// The returned entities are just the base
		/// </summary>
		/// <param name="parentUid"></param>
		/// <returns></returnsThisEntity
		public static List<AssessmentProfile> GetAll( Guid parentUid)
		{
			List<AssessmentProfile> list = new List<AssessmentProfile>();
			AssessmentProfile entity = new AssessmentProfile();

			Entity parent = EntityManager.GetEntity( parentUid );
			LoggingHelper.DoTrace( 5, string.Format( "EntityAssessments_GetAll: parentUid:{0} entityId:{1}, e.EntityTypeId:{2}", parentUid, parent.Id, parent.EntityTypeId ) );

			try
			{
				using ( var context = new EntityContext() )
				{
					List<DBEntity> results = context.Entity_Assessment
							.Where( s => s.EntityId == parent.Id )
							.OrderBy( s => s.Assessment.Name )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( DBEntity item in results )
						{
							entity = new AssessmentProfile();

                            //need to distinguish between on a detail page for conditions and assessment detail
                            //would usually only want basics here??
                            //17-05-26 mp- change to MapFromDB_Basic
                            if ( item.Assessment != null && item.Assessment.EntityStateId > 1 )
                            {
                                AssessmentManager.MapFromDB_Basic( item.Assessment, entity,
                                true );//includingCosts-not sure
                                       //add competencies
                                AssessmentManager.MapFromDB_Competencies( entity );
                                list.Add( entity );
                            }
						}
					}
					return list;
				}


			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".EntityAssessments_GetAll" );
			}
			return list;
		}

		public static ThisEntity Get( int parentId, int assessmentId )
		{
			ThisEntity entity = new ThisEntity();
			if ( parentId < 1 || assessmentId < 1 )
			{
				return entity;
			}
			try
			{
				using ( var context = new EntityContext() )
				{
					EM.Entity_Assessment from = context.Entity_Assessment
							.SingleOrDefault( s => s.AssessmentId == assessmentId && s.EntityId == parentId );

					if ( from != null && from.Id > 0 )
					{
						entity.Id = from.Id;
						entity.AssessmentId = from.AssessmentId;
						entity.EntityId = from.EntityId;

						entity.ProfileSummary = from.Assessment.Name;
						//to.Credential = from.Credential;
						entity.Assessment = new AssessmentProfile();
						AssessmentManager.MapFromDB_Basic( from.Assessment, entity.Assessment,
								false //includeCosts - propose to use for credential editor
								);

						if ( IsValidDate( from.Created ) )
							entity.Created = ( DateTime ) from.Created;
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".Get" );
			}
			return entity;
		}//

		#region Entity Assessment Persistance ===================
	
		/// <summary>
		/// Add an Entity assessment
		/// </summary>
		/// <param name="parentUid"></param>
		/// <param name="assessmentId"></param>
		/// <param name="allowMultiples">If false, check if an assessment exists. If found, do an update</param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public int Add( Guid parentUid, 
					int assessmentId,
					bool allowMultiples,
					ref SaveStatus status )
		{
			int id = 0;
			int count = 0;
			if ( assessmentId == 0 )
			{
				status.AddError( string.Format( "A valid Assessment identifier was not provided to the {0}.EntityAssessment_Add method.", thisClassName ) );
				return 0;
			}


			Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				status.AddError( "Error - the parent entity was not found." );
				return 0;
			}
			using ( var context = new EntityContext() )
			{
				DBEntity efEntity = new DBEntity();
				try
				{
					//first check for duplicates
					efEntity = context.Entity_Assessment
							.SingleOrDefault( s => s.EntityId == parent.Id && s.AssessmentId == assessmentId );

					if ( efEntity != null && efEntity.Id > 0 )
					{
						//status.AddError( string.Format( "Error - this Assessment has already been added to this profile.", thisClassName ) );
						return 0;
					}

					if ( allowMultiples == false )
					{
						//check if one exists, and replace if found
						efEntity = context.Entity_Assessment
							.FirstOrDefault( s => s.EntityId == parent.Id  );
						if ( efEntity != null && efEntity.Id > 0 )
						{
							efEntity.AssessmentId = assessmentId;

							count = context.SaveChanges();
							return efEntity.Id;
						}
					}
					efEntity = new DBEntity();
					efEntity.EntityId = parent.Id;
					efEntity.AssessmentId = assessmentId;
					efEntity.Created = System.DateTime.Now;

					context.Entity_Assessment.Add( efEntity );

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
						status.AddError( "Error - the add was not successful." );
						string message = thisClassName + string.Format( ".Add Failed", "Attempted to add a Assessment for a profile. The process appeared to not work, but there was no exception, so we have no message, or no clue. Parent Profile: {0}, Type: {1}, assessmentId: {2}", parentUid, parent.EntityType, assessmentId );
						EmailManager.NotifyAdmin( thisClassName + ".Add Failed", message );
					}
				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", "Entity_Assessment" );
					status.AddError( "Error - the save was not successful. " + message );
					LoggingHelper.LogError( dbex, thisClassName + string.Format( ".Save(), Parent: {0} ({1})", parent.EntityBaseName, parent.EntityBaseId ) );

				}
				catch ( Exception ex )
				{
					string message = FormatExceptions( ex );
					status.AddError( "Error - the save was not successful. " + message );
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Save(), Parent: {0} ({1})", parent.EntityBaseName, parent.EntityBaseId ) );
				}


			}
			return id;
		}
		//public bool Delete( Guid parentUid, int assessmentId, ref string statusMessage )
		//{
		//	bool isValid = false;
		//	if ( assessmentId == 0 )
		//	{
		//		statusMessage = "Error - missing an identifier for the Assessment to remove";
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
		//		DBEntity efEntity = context.Entity_Assessment
		//						.SingleOrDefault( s => s.EntityId == parent.Id && s.AssessmentId == assessmentId );

		//		if ( efEntity != null && efEntity.Id > 0 )
		//		{
		//			context.Entity_Assessment.Remove( efEntity );
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
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;
using WPM = workIT.Models.ProfileModels;
using ThisEntity = workIT.Models.ProfileModels.Entity_CompetencyFramework;
using DBEntity = workIT.Data.Tables.Entity_CompetencyFramework;
using EntityContext = workIT.Data.Tables.workITEntities;

using workIT.Utilities;
//using CM = workIT.Models.Common;
//using EM = workIT.Data.Tables;

namespace workIT.Factories
{
	public class Entity_CompetencyFrameworkManager : BaseFactory
	{
		static string thisClassName = "Entity_CompetencyFrameworkManager";
		public static int RelationshipType_HasPart = 1;
		#region Entity CompetencyFramework Persistance ===================

		public bool SaveList( List<int> list, Guid parentUid, ref SaveStatus status )
		{
			if ( list == null || list.Count == 0 )
				return true;
			int newId = 0;

			bool isAllValid = true;
			foreach ( int item in list )
			{
				newId = Add( parentUid, item, ref status );
				if ( newId == 0 )
					isAllValid = false;
			}

			return isAllValid;
		}

		/// <summary>
		/// Add an Entity CompetencyFramework
		/// </summary>
		/// <param name="parentUid"></param>
		/// <param name="CompetencyFrameworkId"></param>
		/// <param name="relationshipTypeId"></param>
		/// <param name="allowMultiples">If false, check if an CompetencyFramework exists. If found, do an update</param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public int Add( Guid parentUid, int CompetencyFrameworkId, ref SaveStatus status )
		{
			int id = 0;
			int count = 0;
			if ( CompetencyFrameworkId == 0 )
			{
				status.AddError( string.Format( "A valid CompetencyFramework identifier was not provided to the {0}.EntityCompetencyFramework_Add method.", thisClassName ) );
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
					efEntity = context.Entity_CompetencyFramework
							.FirstOrDefault( s => s.EntityId == parent.Id && s.CompetencyFrameworkId == CompetencyFrameworkId );

					if ( efEntity != null && efEntity.Id > 0 )
					{
						//status.AddError( string.Format( "Error - this CompetencyFramework has already been added to this profile.", thisClassName ) );
						return 0;
					}

					efEntity = new DBEntity();
					efEntity.EntityId = parent.Id;
					efEntity.CompetencyFrameworkId = CompetencyFrameworkId;
					efEntity.Created = System.DateTime.Now;

					context.Entity_CompetencyFramework.Add( efEntity );

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
						status.AddError( thisClassName + "Error - the add was not successful." );
						string message = thisClassName + string.Format( ".Add Failed", "Attempted to add a CompetencyFramework for a profile. The process appeared to not work, but there was no exception, so we have no message, or no clue. Parent Profile: {0}, Type: {1}, CompetencyFrameworkId: {2}", parentUid, parent.EntityType, CompetencyFrameworkId );
						EmailManager.NotifyAdmin( thisClassName + ".Add Failed", message );
					}
				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", "Entity_CompetencyFramework" );
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

		/// <summary>
		/// Delete all relationships for parent - FOR REFERENCES, DELETE ACTUAL CompetencyFramework AS WELL
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
				var results = context.Entity_CompetencyFramework
							.Where( s => s.EntityId == parent.Id )
							.OrderBy( s => s.CompetencyFramework.Name )
							.ToList();
				if ( results == null || results.Count == 0 )
				{
					return true;
				}

				foreach ( var item in results )
				{
					//if a reference, delete actual CompetencyFramework if not used elsewhere
					if ( item.CompetencyFramework != null && item.CompetencyFramework.EntityStateId == 2 )
					{
						//do a getall for the current CompetencyFrameworkId. If the CompetencyFramework is only referenced once, delete the CompetencyFramework as well.
						var exists = context.Entity_CompetencyFramework
							.Where( s => s.CompetencyFrameworkId == item.CompetencyFrameworkId )
							.ToList();
						if ( exists != null && exists.Count() == 1 )
						{
							var statusMsg = "";
							//this method will also add pending reques to remove from elastic.
							//20-11-11 mp - BE CLEAR - ONLY DONE FOR A REFERENCE
							//actually this delete will probably also delete the Entity_CompetencyFramework
							//new CompetencyFrameworkManager().Delete( item.CompetencyFrameworkId, ref statusMsg );
							//continue;
						}
					}
					context.Entity_CompetencyFramework.Remove( item );
					count = context.SaveChanges();
					if ( count > 0 )
					{

					}
				}
				//context.Entity_CompetencyFramework.RemoveRange( context.Entity_CompetencyFramework.Where( s => s.EntityId == parent.Id ) );
				//            int count = context.SaveChanges();
				//            if ( count > 0 )
				//            {
				//                isValid = true;
				//            }
				//            else
				//            {
				//                //if doing a delete on spec, may not have been any properties
				//            }
			}

            return isValid;
        }
		#endregion


		/// <summary>
		/// Get all CompetencyFrameworks for the provided entity
		/// The returned entities are just the base
		/// </summary>
		/// <param name="parentUid"></param>
		/// <returns></returnsThisEntity
		public static List<WPM.CompetencyFramework> GetAll( Guid parentUid)
		{
			var list = new List<WPM.CompetencyFramework>();
			var entity = new WPM.CompetencyFramework();

			Entity parent = EntityManager.GetEntity( parentUid );
			LoggingHelper.DoTrace( 7, string.Format( "EntityCompetencyFrameworks_GetAll: parentUid:{0} entityId:{1}, e.EntityTypeId:{2}", parentUid, parent.Id, parent.EntityTypeId ) );

			try
			{
				using ( var context = new EntityContext() )
				{
					List<DBEntity> results = context.Entity_CompetencyFramework
							.Where( s => s.EntityId == parent.Id  )
							.OrderBy( s => s.CompetencyFramework.Name )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( DBEntity item in results )
						{
							entity = new WPM.CompetencyFramework();
                            if ( item.CompetencyFramework != null && item.CompetencyFramework.EntityStateId > 1 )
                            {
                                CompetencyFrameworkManager.MapFromDB( item.CompetencyFramework, entity );
                                list.Add( entity );
                            }
						}
					}
					return list;
				}


			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + string.Format( ".EntityCompetencyFrameworks_GetAll. Guid: {0}, parentType: {1} ({2}), ", parentUid, parent.EntityType, parent.EntityBaseId ) );

			}
			return list;
		}

		public static ThisEntity Get( int parentId, int CompetencyFrameworkId )
		{
			ThisEntity entity = new ThisEntity();
			if ( parentId < 1 || CompetencyFrameworkId < 1 )
			{
				return entity;
			}
			try
			{
				using ( var context = new EntityContext() )
				{
					var from = context.Entity_CompetencyFramework
							.SingleOrDefault( s => s.CompetencyFrameworkId == CompetencyFrameworkId && s.EntityId == parentId );

					if ( from != null && from.Id > 0 )
					{
						entity.Id = from.Id;
						entity.CompetencyFrameworkId = from.CompetencyFrameworkId;
						entity.EntityId = from.EntityId;
						entity.ProfileSummary = from.CompetencyFramework.Name;
						//to.Credential = from.Credential;
						entity.CompetencyFramework = new WPM.CompetencyFramework();
						CompetencyFrameworkManager.MapFromDB( from.CompetencyFramework, entity.CompetencyFramework	);

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

		
	}
}

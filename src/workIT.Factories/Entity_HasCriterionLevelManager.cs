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
using ThisResource = workIT.Models.Common.Entity_HasCriterionLevel;
using DBResource = workIT.Data.Tables.Entity_HasCriterionLevel;
using EntityContext = workIT.Data.Tables.workITEntities;


namespace workIT.Factories
{
	public class Entity_HasCriterionLevelManager : BaseFactory
	{
		/// <summary>
		/// if true, return an error message if the HasCriterionLevel is already associated with the parent
		/// </summary>
		private bool ReturningErrorOnDuplicate { get; set; }
		public Entity_HasCriterionLevelManager()
		{
			ReturningErrorOnDuplicate = false;
		}
		public Entity_HasCriterionLevelManager( bool returnErrorOnDuplicate )
		{
			ReturningErrorOnDuplicate = returnErrorOnDuplicate;
		}
		static string thisClassName = "Entity_HasCriterionLevelManager";
		/// <summary>
		/// Get all components for the provided entity
		/// The returned entities are just the base
		/// </summary>
		/// <param name="parentUid"></param>
		/// <returns></returns>
		public static List<CriterionLevel> GetAll( int parentId, bool includingComponents = false )
		{
			List<CriterionLevel> list = new List<CriterionLevel>();
			CriterionLevel entity = new CriterionLevel();


			try
			{
				using ( var context = new EntityContext() )
				{
					List<DBResource> results = context.Entity_HasCriterionLevel
							.Where( s => s.EntityId == parentId )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( DBResource item in results )
						{
							//actually the relationship type is not applicable in the component
							entity = new CriterionLevel();
							//not sure if we will have variances in what is returned
							entity = RubricCriterionLevelManager.GetBasic( item.CriterionLevelId );

							list.Add( entity );
						}
					}
					return list;
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".Entity_HasCriterionLevel_GetAll" );
			}
			return list;
		}

		#region Entity_HasCriteronLevel Persistance ===================

		/// <summary>
		/// Add an Entity_HasCriteronLevel
		/// </summary>
		/// <param name="parentUid"></param>
		/// <param name="criterionLevelId"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public int Add( int parentId, int criterionLevelId, ref List<string> messages )
		{
			int id = 0;
			int count = messages.Count();
			if ( criterionLevelId == 0 )
			{
				messages.Add( string.Format( "A valid Entity_HasCriteronLevel identifier was not provided to the {0}.Add method.", thisClassName ) );
			}
			if ( messages.Count > count )
				return 0;

			using ( var context = new EntityContext() )
			{
				DBResource efEntity = new DBResource();
				try
				{
					//first check for duplicates
					efEntity = context.Entity_HasCriterionLevel
							.FirstOrDefault( s => s.EntityId == parentId && s.CriterionLevelId == criterionLevelId );

					if ( efEntity != null && efEntity.Id > 0 )
					{
						if ( ReturningErrorOnDuplicate )
							messages.Add( string.Format( "Error - this Entity_HasCriterionLevel has already been added to this profile.", thisClassName ) );

						return efEntity.Id;
					}

					efEntity = new DBResource();
					efEntity.EntityId = parentId;
					efEntity.CriterionLevelId = criterionLevelId;
					efEntity.RowId = Guid.NewGuid();
					efEntity.Created = System.DateTime.Now;

					context.Entity_HasCriterionLevel.Add( efEntity );

					// submit the change to database
					count = context.SaveChanges();
					if ( count > 0 )
					{
						//messages.Add( "Successful" );
						id = efEntity.Id;
						return efEntity.Id;
					}
					else
					{
						//?no info on error
						messages.Add( "Error - the add was not successful." );
						string message = thisClassName + string.Format( ".Add Failed", "Attempted to add a Entity_HasCriterionLevel for a profile. The process appeared to not work, but there was no exception, so we have no message, or no clue. Parent Profile: learningOppId: {0}", criterionLevelId );
						EmailManager.NotifyAdmin( thisClassName + ".Add Failed", message );
					}
				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", "Entity_HasCriterionLevel" );
					messages.Add( "Error - the save was not successful. " + message );
					LoggingHelper.LogError( dbex, thisClassName + string.Format( ".Save(), Parent: {0})", parentId ) );

				}
				catch ( Exception ex )
				{
					string message = FormatExceptions( ex );
					messages.Add( "Error - the save was not successful. " + message );
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Save(), Parent: {0} )", parentId ) );
				}


			}
			return id;
		}
		public bool SaveList( int parentId, List<int> list, ref List<string> messages, int relationshipTypeId = 1 )
		{
			if ( parentId == 0 )
			{
				messages.Add( thisClassName + ".SaveList. Error - the provided target parent entity was not provided." );
				return false;
			}

			bool isAllValid = true;
			if ( list == null || list.Count == 0 )
			{
				DeleteAll( parentId, ref messages );
				return true;
			}

			foreach ( var item in list )
			{
				Add( parentId, item, ref messages );
			}

			return isAllValid;
		}
		public bool Delete( Guid parentUid, int recordId, ref string statusMessage )
		{
			bool isValid = false;
			if ( recordId == 0 )
			{
				statusMessage = "Error - missing an identifier for the Entity_HasCriterionLevel to remove";
				return false;
			}
			//need to get Entity.Id 
			Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				statusMessage = "Error - Entity_HasCriterionLevel.Delete: The parent entity was not found: " + parentUid.ToString();
				return false;
			}

			using ( var context = new EntityContext() )
			{
				DBResource efEntity = context.Entity_HasCriterionLevel
								.FirstOrDefault( s => s.EntityId == parent.Id && s.CriterionLevelId == recordId );

				if ( efEntity != null && efEntity.Id > 0 )
				{
					context.Entity_HasCriterionLevel.Remove( efEntity );
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						isValid = true;
					}
				}
				else
				{
					statusMessage = "Warning - the record was not found - probably because the target had been previously deleted";
					isValid = true;
				}
			}

			return isValid;
		}
		public bool DeleteAll( int parentId, ref List<string> messages )
		{
			bool isValid = true;
			if ( parentId == 0 )
			{
				messages.Add( "Error - EntityHasCriterionLevelManager.DeleteAll: The parent entity was not found: " + parentId.ToString() );
				return false;
			}
			using ( var context = new EntityContext() )
			{
				context.Entity_HasCriterionLevel.RemoveRange( context.Entity_HasCriterionLevel.Where( s => s.EntityId == parentId ) );
				int count = context.SaveChanges();
				if ( count > 0 )
				{
					isValid = true;
					messages.Add( string.Format( "removed {0} related relationships.", count ) );
				}
			}
			return isValid;

		}

		/// <summary>
		/// Delete all records that are not in the provided list. 
		/// This method is typically called from bulk upload, and want to remove any records not in the current list to upload.
		/// </summary>
		/// <param name="parentUid"></param>
		/// <param name="list"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public bool DeleteNotInList( Guid parentUid, List<Rubric> list, ref List<string> messages )
		{
			bool isValid = true;
			if ( !list.Any() )
			{
				return true;
			}
			Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				messages.Add( thisClassName + string.Format( ".DeleteNotInList() Error - the parent entity for [{0}] was not found.", parentUid ) );
				return false;
			}

			using ( var context = new EntityContext() )
			{
				var existing = context.Entity_HasCriterionLevel.Where( s => s.EntityId == parent.Id ).ToList();
				var inputIds = list.Select( x => x.Id ).ToList();

				//delete records which are not selected 
				var notExisting = existing.Where( x => !inputIds.Contains( x.CriterionLevelId ) ).ToList();
				foreach ( var item in notExisting )
				{
					context.Entity_HasCriterionLevel.Remove( item );
					context.SaveChanges();
				}

			}
			return isValid;

		}
		#endregion
	}
}


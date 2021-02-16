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

using ThisEntity = workIT.Models.Common.Entity_HasPathway;
using DBEntity = workIT.Data.Tables.Entity_HasPathway;
using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;

using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;

namespace workIT.Factories
{
	public class Entity_PathwayManager : BaseFactory
	{
		/// <summary>
		/// if true, return an error message if the HasPathway is already associated with the parent
		/// </summary>
		private bool ReturningErrorOnDuplicate { get; set; }
		public Entity_PathwayManager()
		{
			ReturningErrorOnDuplicate = false;
		}
		public Entity_PathwayManager( bool returnErrorOnDuplicate )
		{
			ReturningErrorOnDuplicate = returnErrorOnDuplicate;
		}
		static string thisClassName = "Entity_PathwayManager";
		/// <summary>
		/// Get all components for the provided entity
		/// The returned entities are just the base
		/// </summary>
		/// <param name="parentUid"></param>
		/// <returns></returns>
		public static List<Pathway> GetAll( Guid parentUid, bool includingComponents = false )
		{
			List<Pathway> list = new List<Pathway>();
			Pathway entity = new Pathway();

			Entity parent = EntityManager.GetEntity( parentUid );
			LoggingHelper.DoTrace( 7, string.Format( "Entity_Pathway_GetAll: parentUid:{0} entityId:{1}, e.EntityTypeId:{2}", parentUid, parent.Id, parent.EntityTypeId ) );

			try
			{
				using ( var context = new EntityContext() )
				{
					List<DBEntity> results = context.Entity_HasPathway
							.Where( s => s.EntityId == parent.Id )
							.OrderBy( s => s.Pathway.Name ) //not suire
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( DBEntity item in results )
						{
							//actually the relationship type is not applicable in the component
							entity = new Pathway() { PathwayRelationshipTypeId = item.PathwayRelationshipTypeId };
							//not sure if we will have variances in what is returned
							PathwayManager.MapFromDB( item.Pathway, entity, includingComponents );

							list.Add( entity );
						}
					}
					return list;
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".Entity_Pathway_GetAll" );
			}
			return list;
		}


		public static List<int> GetAllIds( Guid parentUid )
		{
			List<int> list = new List<int>();
			Pathway entity = new Pathway();

			Entity parent = EntityManager.GetEntity( parentUid );
			LoggingHelper.DoTrace( 7, string.Format( "Entity_Pathway_GetAll: parentUid:{0} entityId:{1}, e.EntityTypeId:{2}", parentUid, parent.Id, parent.EntityTypeId ) );

			try
			{
				using ( var context = new EntityContext() )
				{
					List<DBEntity> results = context.Entity_HasPathway
							.Where( s => s.EntityId == parent.Id )
							.OrderBy( s => s.Pathway.Name ) //not sure
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( DBEntity item in results )
						{
							list.Add( item.PathwayId );
						}
					}
					return list;
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".Entity_Pathway_GetAll" );
			}
			return list;
		}
		/// <summary>
		/// Get list of particular relationships, like hasChild
		/// May just want the component since getting a particulary relationship
		/// </summary>
		/// <param name="parentId"></param>
		/// <param name="PathwayId"></param>
		/// <returns></returns>
		public static List<ThisEntity> GetAll( int parentEntityId, int pathwayRelationshipTypeId )
		{
			var entity = new ThisEntity();
			var list = new List<ThisEntity>();
			if ( parentEntityId < 1 || pathwayRelationshipTypeId < 1 )
			{
				return list;
			}
			try
			{
				using ( var context = new EntityContext() )
				{
					//there could be multiple relationships for a parent and component
					List<DBEntity> results = context.Entity_HasPathway
						.Where( s => s.EntityId == parentEntityId && s.PathwayRelationshipTypeId == pathwayRelationshipTypeId )
						.OrderBy( s => s.PathwayRelationshipTypeId )
						.ThenBy( s => s.Pathway.Name ) //
						.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( var from in results )
						{
							if ( from != null && from.Id > 0 )
							{
								entity.Id = from.Id;
								entity.EntityId = from.EntityId;
								entity.PathwayId = from.PathwayId;
								entity.PathwayRelationshipTypeId = from.PathwayRelationshipTypeId;
								if ( IsValidDate( from.Created ) )
									entity.Created = ( DateTime )from.Created;

								entity.PathwayName = from.Pathway.Name;
								entity.Pathway = new Pathway();
								PathwayManager.MapFromDB( from.Pathway, entity.Pathway, false );
							}
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetAll" );
			}
			return list;
		}//

		/// <summary>
		/// Or just get all and let caller sort the data
		/// </summary>
		/// <param name="parentId"></param>
		/// <returns></returns>
		public static List<ThisEntity> GetAll( int parentId )
		{
			var entity = new ThisEntity();
			var list = new List<ThisEntity>();
			if ( parentId < 1 )
			{
				return list;
			}
			try
			{
				using ( var context = new EntityContext() )
				{
					//there could be multiple relationships for a parent and component
					List<DBEntity> results = context.Entity_HasPathway
						.Where( s => s.EntityId == parentId )
						.OrderBy( s => s.PathwayRelationshipTypeId )
						.ThenBy( s => s.Pathway.Name ) //
						.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( var from in results )
						{
							if ( from != null && from.Id > 0 )
							{
								entity.Id = from.Id;
								entity.EntityId = from.EntityId;
								entity.PathwayId = from.PathwayId;
								entity.PathwayRelationshipTypeId = from.PathwayRelationshipTypeId > 0 ? from.PathwayRelationshipTypeId : 0;
								if ( IsValidDate( from.Created ) )
									entity.Created = ( DateTime )from.Created;

								entity.PathwayName = from.Pathway.Name;
								//to.Credential = from.Credential;
								entity.Pathway = new Pathway();
								PathwayManager.MapFromDB( from.Pathway, entity.Pathway, false );
							}
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetAll" );
			}
			return list;
		}//

		#region Entity_HasPathway Persistance ===================

		/// <summary>
		/// Add an Entity HasPathway
		/// </summary>
		/// <param name="parentUid"></param>
		/// <param name="pathwayId"></param>
		/// <param name="pathwayRelationshipTypeId">May not be relevent, default to 1</param>
		/// <param name="userId"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public int Add( Guid parentUid,
					int pathwayId,
					int pathwayRelationshipTypeId,
					int userId,
					ref List<string> messages )
		{
			int id = 0;
			int count = messages.Count();
			if ( pathwayId == 0 )
			{
				messages.Add( string.Format( "A valid Entity_HasPathway identifier was not provided to the {0}.Add method.", thisClassName ) );
			}
			if ( pathwayRelationshipTypeId == 0 )
			{
				pathwayRelationshipTypeId = 1;
				//messages.Add( string.Format( "A valid Entity_HasPathway PathwayRelationshipTypeId was not provided to the {0}.Add method.", thisClassName ) );
			}
			if ( messages.Count > count )
				return 0;

			Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				messages.Add( "Error - the parent entity was not found." );
				return 0;
			}
			using ( var context = new EntityContext() )
			{
				DBEntity efEntity = new DBEntity();
				try
				{
					//first check for duplicates
					efEntity = context.Entity_HasPathway
							.FirstOrDefault( s => s.EntityId == parent.Id && s.PathwayId == pathwayId && s.PathwayRelationshipTypeId == pathwayRelationshipTypeId );

					if ( efEntity != null && efEntity.Id > 0 )
					{
						if ( ReturningErrorOnDuplicate )
							messages.Add( string.Format( "Error - this Entity_HasPathway has already been added to this profile.", thisClassName ) );

						return efEntity.Id;
					}

					efEntity = new DBEntity();
					efEntity.EntityId = parent.Id;
					efEntity.PathwayId = pathwayId;
					efEntity.PathwayRelationshipTypeId = pathwayRelationshipTypeId;

					efEntity.Created = System.DateTime.Now;

					context.Entity_HasPathway.Add( efEntity );

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
						string message = thisClassName + string.Format( ".Add Failed", "Attempted to add a Entity_HasPathway for a profile. The process appeared to not work, but there was no exception, so we have no message, or no clue. Parent Profile: {0}, Type: {1}, learningOppId: {2}, createdById: {3}", parentUid, parent.EntityType, pathwayId, userId );
						EmailManager.NotifyAdmin( thisClassName + ".Add Failed", message );
					}
				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", "Entity_Pathway" );
					messages.Add( "Error - the save was not successful. " + message );
					LoggingHelper.LogError( dbex, thisClassName + string.Format( ".Save(), Parent: {0} ({1})", parent.EntityBaseName, parent.EntityBaseId ) );

				}
				catch ( Exception ex )
				{
					string message = FormatExceptions( ex );
					messages.Add( "Error - the save was not successful. " + message );
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Save(), Parent: {0} ({1})", parent.EntityBaseName, parent.EntityBaseId ) );
				}


			}
			return id;
		}
		/// <summary>
		/// Update: check for existing. Any existing pathways that were not included in the input will be removed.
		/// </summary>
		/// <param name="parentUid"></param>
		/// <param name="pathwayRelationshipTypeId"></param>
		/// <param name="inputIds"></param>
		/// <param name="userId"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public bool Replace( Guid parentUid, int pathwayRelationshipTypeId, List<int> inputIds, ref SaveStatus status )
		{
			if ( inputIds == null || !inputIds.Any() )
			{
				return true;
			}
			status.HasSectionErrors = false;
			Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				status.AddError( "Error - the parent entity was not found." );
				return false;
			}

			int count = 0;
			DBEntity efEntity = new DBEntity();
			try
			{
				using ( var context = new EntityContext() )
				{
					var existing = context.Entity_HasPathway.Where( s => s.EntityId == parent.Id && s.PathwayRelationshipTypeId == pathwayRelationshipTypeId ).ToList();
					//var inputIds = profiles.Select( x => x.Id ).ToList();
					var existingIds = existing.Select( x => x.PathwayId ).ToList();

					//delete records which are not selected 
					var notExisting = existing.Where( x => !inputIds.Contains( x.PathwayId ) ).ToList();
					foreach ( var item in notExisting )
					{
						context.Entity_HasPathway.Remove( item );
						context.SaveChanges();
					}
					//only get profiles where not existing
					var newProfiles = inputIds.Where( x => !existingIds.Contains( x ) ).ToList();
					if ( existing != null && existing.Count() > 0 && inputIds.Count() > 0 )
					{
						LoggingHelper.DoTrace( 6, string.Format( thisClassName + ".Replace. Existing: {0}, input: {1}, Not existing(to delete): {2}, newProfiles: {3}", existing.Count(), inputIds.Count(), notExisting.Count(), newProfiles.Count() ) );

						if ( existing.Count() != inputIds.Count() )
						{

						}
					}
					foreach ( var pathwayId in newProfiles )
					{
						//if there are no existing, optimize by not doing check. What about duplicates?
						efEntity = new DBEntity
						{
							EntityId = parent.Id,
							PathwayRelationshipTypeId = pathwayRelationshipTypeId,
							PathwayId = pathwayId,
							Created = DateTime.Now,
						};
						context.Entity_HasPathway.Add( efEntity );
						count = context.SaveChanges();

					} //foreach
				}

			}
			catch ( Exception ex )
			{
				string message = FormatExceptions( ex );
				status.AddError( message );
				LoggingHelper.LogError( ex, "Entity_FrameworkItemManager.Replace()" );
			}
			return status.WasSectionValid;
		} //

		/// <summary>
		/// Delete all pathways for a parent, typically a pathwaySet
		/// </summary>
		/// <param name="parentUid"></param>
		/// <param name="recordId"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool DeleteAll( Guid parentUid, ref SaveStatus status )
		{
			bool isValid = false;

			//need to get Entity.Id 
			Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				status.AddError( "Error - the parent entity was not found." );
				return false;
			}
			try
			{
				using ( var context = new EntityContext() )
				{
					context.Entity_HasPathway.RemoveRange( context.Entity_HasPathway.Where( s => s.EntityId == parent.Id ) );
					int count = context.SaveChanges();
					if ( count >= 0 )
					{
						isValid = true;
					}
					else
					{
						//may not be any?
						//may be should to a read check first?
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".DeleteAll( Guid parentUid, ref SaveStatus status )" );
			}
			return isValid;
		}
		public bool Delete( Guid parentUid, int recordId, ref string statusMessage )
		{
			bool isValid = false;
			if ( recordId == 0 )
			{
				statusMessage = "Error - missing an identifier for the Entity_HasPathway to remove";
				return false;
			}
			//need to get Entity.Id 
			Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				statusMessage = "Error - the parent entity was not found.";
				return false;
			}

			using ( var context = new EntityContext() )
			{
				DBEntity efEntity = context.Entity_HasPathway
								.FirstOrDefault( s => s.EntityId == parent.Id && s.PathwayId == recordId );

				if ( efEntity != null && efEntity.Id > 0 )
				{
					context.Entity_HasPathway.Remove( efEntity );
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


		/// <summary>
		/// Delete all records that are not in the provided list. 
		/// </summary>
		/// <param name="parentUid"></param>
		/// <param name="list"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public bool DeleteNotInList( Guid parentUid, List<Pathway> list, ref List<string> messages )
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
				var existing = context.Entity_HasPathway.Where( s => s.EntityId == parent.Id ).ToList();
				var inputIds = list.Select( x => x.Id ).ToList();

				//delete records which are not selected 
				var notExisting = existing.Where( x => !inputIds.Contains( x.PathwayId ) ).ToList();
				foreach ( var item in notExisting )
				{
					context.Entity_HasPathway.Remove( item );
					context.SaveChanges();
				}

			}
			return isValid;

		}
		#endregion
	}
}

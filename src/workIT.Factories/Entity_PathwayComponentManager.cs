using System;
using System.Collections.Generic;
using System.Linq;
using workIT.Models;
using workIT.Models.Common;
using workIT.Utilities;
using DBEntity = workIT.Data.Tables.Entity_HasPathwayComponent;
using EntityContext = workIT.Data.Tables.workITEntities;
using ThisEntity = workIT.Models.Common.Entity_HasPathwayComponent;

namespace workIT.Factories
{
	public class Entity_PathwayComponentManager : BaseFactory
	{
        #region Relationships 
        /*
		Id	Title						InverseTitle
		1	Has Destination Component	Is Destination Component Of
		2	Is Child Of					Has Child
		3	Has Child					Is Child Of
		4	Precedes					Preceded By
		5	Preceded					By	Precedes
		6	Target Component			Is Target Component Of
		7	Has Part					Is Part Of
		*/
        public static int Relationship_IsDestinationComponent = 1;
        public static int Relationship_IsChildOf = 2;
        public static int Relationship_HasChild = 3;
        #endregion
        /// <summary>
        /// if true, return an error message if the HasPathwayComponent is already associated with the parent
        /// </summary>
        private bool ReturningErrorOnDuplicate { get; set; }
		public Entity_PathwayComponentManager()
		{
			ReturningErrorOnDuplicate = false;
		}
		public Entity_PathwayComponentManager( bool returnErrorOnDuplicate )
		{
			ReturningErrorOnDuplicate = returnErrorOnDuplicate;
		}
		static string thisClassName = "Entity_PathwayComponentManager";

		/// <summary>
		/// Get all target components for a condition
		/// </summary>
		/// <param name="parentUid"></param>
		/// <param name="componentRelationshipTypeId"></param>
		/// <param name="childComponentsAction">0-none; 1-summary; 2-deep </param>
		/// <returns></returns>
		public static List<PathwayComponent> GetAll( Guid parentUid, int componentRelationshipTypeId, int childComponentsAction = 1 )
		{
			List<PathwayComponent> list = new List<PathwayComponent>();
			PathwayComponent entity = new PathwayComponent();

			Entity parent = EntityManager.GetEntity( parentUid );
			LoggingHelper.DoTrace( 7, string.Format( "Entity_PathwayComponent_GetAll: parentUid:{0} entityId:{1}, e.EntityTypeId:{2}, componentRelationshipTypeId: {3}", parentUid, parent.Id, parent.EntityTypeId, componentRelationshipTypeId ) );

			try
			{
				using ( var context = new EntityContext() )
				{
					List<DBEntity> results = context.Entity_HasPathwayComponent
							.Where( s => s.EntityId == parent.Id && s.ComponentRelationshipTypeId == componentRelationshipTypeId )
							.OrderBy( s => s.Created)
							.ThenBy( s => s.PathwayComponent.Name ) //not sure
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( DBEntity item in results )
						{
							//actually the relationship type is not applicable in the component
							entity = new PathwayComponent() { ComponentRelationshipTypeId = item.ComponentRelationshipTypeId };
							//not sure if we will have variances in what is returned
							PathwayComponentManager.MapFromDB( item.PathwayComponent, entity, childComponentsAction );
							list.Add( entity );
						}
					}
					return list;
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".Entity_PathwayComponent_GetAll" );
			}
			return list;
		}

        /// <summary>
        /// Get all components for the provided entity
        /// The returned entities are just the base
        /// </summary>
        /// <param name="parentUid"></param>
        /// <returns></returns>
        public static List<PathwayComponent> GetAll( Guid parentUid, int childComponentsAction = 1 )
		{
			List<PathwayComponent> list = new List<PathwayComponent>();
			PathwayComponent entity = new PathwayComponent();

			Entity parent = EntityManager.GetEntity( parentUid );
			LoggingHelper.DoTrace( 7, string.Format( "Entity_PathwayComponent_GetAll: parentUid:{0} entityId:{1}, e.EntityTypeId:{2}", parentUid, parent.Id, parent.EntityTypeId ) );
			/*
			 * ideal order
			 * - get destination component
			 *	 - get all children
			 *	 - children of children?
			 * - get hasChild
			 *	 - if different from destination component, get children
			 */
			//20-08-19 - try displaying in same order as read and see what we have
			try
			{
				using ( var context = new EntityContext() )
				{
					List<DBEntity> results = context.Entity_HasPathwayComponent
							.Where( s => s.EntityId == parent.Id )
							.OrderBy( s => s.Created )
							.OrderBy( s => s.ComponentRelationshipTypeId )		//????
							.ThenBy( s => s.PathwayComponent.Name )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( DBEntity item in results )
						{
							//actually the relationship type is not applicable in the component
							entity = new PathwayComponent() { ComponentRelationshipTypeId = item.ComponentRelationshipTypeId };
							//not sure if we will have variances in what is returned
							PathwayComponentManager.MapFromDB( item.PathwayComponent, entity, childComponentsAction );
							int index = list.FindIndex( a => a.CTID == entity.CTID );
							//20-07-27 shouldn't we be doing an exists here
							//if ( index == -1 )
							//{
							list.Add( entity );
							//}
						}
					}
					return list;
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".Entity_PathwayComponent_GetAll" );
			}
			return list;
		}

        public static List<PathwayComponent> GetDestinationComponent( Guid parentUid, int childComponentsAction = 1 )
        {
            List<PathwayComponent> list = new List<PathwayComponent>();
            PathwayComponent entity = new PathwayComponent();

            Entity parent = EntityManager.GetEntity( parentUid );

            try
            {
                using (var context = new EntityContext())
                {
                    //should only be one. 
                    List<DBEntity> results = context.Entity_HasPathwayComponent
                            .Where( s => s.EntityId == parent.Id && s.ComponentRelationshipTypeId == Relationship_IsDestinationComponent )
                            .OrderBy( s => s.Created )
                            .ThenBy( s => s.PathwayComponent.Name ) //not sure
                            .ToList();

                    if (results != null && results.Count > 0)
                    {
                        foreach (DBEntity item in results)
                        {
                            //actually the relationship type is not applicable in the component
                            entity = new PathwayComponent() { ComponentRelationshipTypeId = item.ComponentRelationshipTypeId };
                            //not sure if we will have variances in what is returned
                            PathwayComponentManager.MapFromDB( item.PathwayComponent, entity, childComponentsAction );
                            list.Add( entity );
                        }
                    }
                    return list;
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError( ex, thisClassName + ".Entity_PathwayComponent_GetAll" );
            }
            return list;
        }

        /// <summary>
        /// Get list of particular relationships, like hasChild
        /// May just want the component since getting a particulary relationship
        /// </summary>
        /// <param name="parentEntityId"></param>
        /// <param name="pathwayComponentId"></param>
        /// <returns></returns>
        public static List<ThisEntity> GetAll( int parentEntityId, int componentRelationshipTypeId, int childComponentsAction = 1 )
		{
			var entity = new ThisEntity();
			var list = new List<ThisEntity>();
			if ( parentEntityId < 1 || componentRelationshipTypeId < 1 )
			{
				return list;
			}
			try
			{
				using ( var context = new EntityContext() )
				{
					//there could be multiple relationships for a parent and component
					List<DBEntity> results = context.Entity_HasPathwayComponent
						.Where( s => s.EntityId == parentEntityId && s.ComponentRelationshipTypeId == componentRelationshipTypeId )
						.OrderBy( s => s.ComponentRelationshipTypeId )
						.ThenBy( s => s.PathwayComponent.Name ) //
						.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( var from in results )
						{
							if ( from != null && from.Id > 0 )
							{
								entity.Id = from.Id;
								entity.EntityId = from.EntityId;
								entity.PathwayComponentId = from.PathwayComponentId;
								entity.ComponentRelationshipTypeId = from.ComponentRelationshipTypeId;
								if ( IsValidDate( from.Created ) )
									entity.Created = ( DateTime )from.Created;

								entity.PathwayComponentName = from.PathwayComponent.Name;
								//to.Credential = from.Credential;
								entity.PathwayComponent = new PathwayComponent();
								PathwayComponentManager.MapFromDB( from.PathwayComponent, entity.PathwayComponent, childComponentsAction );
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

		public static List<Pathway> GetPathwayForComponent( int pathwayComponentId, int componentRelationshipTypeId )
		{
			/*
			 * 
			*/
			var entity = new Pathway();
			var list = new List<Pathway>();
			if ( pathwayComponentId < 1 || componentRelationshipTypeId < 1 )
			{
				return list;
			}
			try
			{
				using ( var context = new EntityContext() )
				{
					//there should be only one pathway-component for the passed relationship - typically ispartof
					List<DBEntity> results = context.Entity_HasPathwayComponent
						.Where( s => s.PathwayComponentId == pathwayComponentId && s.ComponentRelationshipTypeId == componentRelationshipTypeId
						&& s.Entity.EntityTypeId == CodesManager.ENTITY_TYPE_PATHWAY )
						.OrderBy( s => s.ComponentRelationshipTypeId )
						.ThenBy( s => s.Entity.EntityBaseName ) //
						.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( var from in results )
						{
							if ( from != null && from.Id > 0 )
							{
								if ( from.Entity != null && from.Entity.EntityBaseId != null )
								{
									//is basic enough?
									entity = PathwayManager.GetBasic( ( int )from.Entity.EntityBaseId );
								}

								list.Add( entity );
							}
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetPathwayForComponent" );
			}
			return list;
		}//

		/// <summary>
		/// Or just get all and let caller sort the data
		/// </summary>
		/// <param name="parentId"></param>
		/// <returns></returns>
		public static List<ThisEntity> GetAll( int parentId, int childComponentsAction = 1 )
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
					List<DBEntity> results = context.Entity_HasPathwayComponent
						.Where( s => s.EntityId == parentId )
						.OrderBy( s => s.ComponentRelationshipTypeId )
						.ThenBy( s => s.PathwayComponent.Name ) //
						.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( var from in results )
						{
							if ( from != null && from.Id > 0 )
							{
								entity.Id = from.Id;
								entity.EntityId = from.EntityId;
								entity.PathwayComponentId = from.PathwayComponentId;
								entity.ComponentRelationshipTypeId = from.ComponentRelationshipTypeId;
								if ( IsValidDate( from.Created ) )
									entity.Created = ( DateTime )from.Created;

								entity.PathwayComponentName = from.PathwayComponent.Name;
								//to.Credential = from.Credential;
								entity.PathwayComponent = new PathwayComponent();
								PathwayComponentManager.MapFromDB( from.PathwayComponent, entity.PathwayComponent, childComponentsAction );
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
		/// May not have a direct get. If we do, will likely need to include ComponentRelationshipTypeId
		/// </summary>
		/// <param name="parentId"></param>
		/// <param name="pathwayComponentId"></param>
		/// <returns></returns>
		public static ThisEntity Get( int parentId, int pathwayComponentId, int childComponentsAction = 1 )
		{
			ThisEntity entity = new ThisEntity();
			if ( parentId < 1 || pathwayComponentId < 1 )
			{
				return entity;
			}
			try
			{
				using ( var context = new EntityContext() )
				{
					//there could be multiple relationships for a parent and component
					var from = context.Entity_HasPathwayComponent
							.FirstOrDefault( s => s.PathwayComponentId == pathwayComponentId && s.EntityId == parentId );

					if ( from != null && from.Id > 0 )
					{
						entity.Id = from.Id;
						entity.EntityId = from.EntityId;
						entity.PathwayComponentId = from.PathwayComponentId;
						entity.ComponentRelationshipTypeId = from.ComponentRelationshipTypeId;
						if ( IsValidDate( from.Created ) )
							entity.Created = ( DateTime )from.Created;

						entity.PathwayComponentName = from.PathwayComponent.Name;
						//to.Credential = from.Credential;
						entity.PathwayComponent = new PathwayComponent();
						PathwayComponentManager.MapFromDB( from.PathwayComponent, entity.PathwayComponent, childComponentsAction );
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".Get" );
			}
			return entity;
		}//

		#region Entity_HasPathwayComponent Persistance ===================

		/// <summary>
		/// Add an Entity HasPathwayComponent
		/// </summary>
		/// <param name="parentUid">Parent Guid. Could be pathway for isPartOf, component for hasChild or ComponentCondition for TargetComponent.</param>
		/// <param name="recordId"></param>
		/// <param name="userId"></param>
		/// <param name="allowMultiples">If false, check if an HasPathwayComponent exists. If found, do an update</param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public int Add( Guid parentUid,
					int pathwayComponentId,
					int componentRelationshipTypeId,
					ref SaveStatus status )
		{
			int id = 0;
			status.HasSectionErrors = false;
			if ( pathwayComponentId == 0 )
			{
				status.AddError( string.Format( "A valid Entity_HasPathwayComponent identifier was not provided to the {0}.Add method.", thisClassName ) );
			}
			if ( componentRelationshipTypeId == 0 )
			{
				status.AddError( string.Format( "A valid Entity_HasPathwayComponent componentRelationshipTypeId was not provided to the {0}.Add method.", thisClassName ) );
			}
			if ( status.HasSectionErrors )
				return 0;

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
					efEntity = context.Entity_HasPathwayComponent
							.FirstOrDefault( s => s.EntityId == parent.Id && s.PathwayComponentId == pathwayComponentId && s.ComponentRelationshipTypeId == componentRelationshipTypeId );

					if ( efEntity != null && efEntity.Id > 0 )
					{
						if ( ReturningErrorOnDuplicate )
							status.AddError( string.Format( "Error - this Entity_HasPathwayComponent has already been added to this profile.", thisClassName ) );

						return efEntity.Id;
					}

					efEntity = new DBEntity();
					efEntity.EntityId = parent.Id;
					efEntity.PathwayComponentId = pathwayComponentId;
					efEntity.ComponentRelationshipTypeId = componentRelationshipTypeId;
					efEntity.Created = System.DateTime.Now;

					context.Entity_HasPathwayComponent.Add( efEntity );

					// submit the change to database
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						//status.AddError( "Successful" );
						id = efEntity.Id;
						return efEntity.Id;
					}
					else
					{
						//?no info on error
						status.AddError( thisClassName + "Error - the add was not successful." );
						string message = thisClassName + string.Format( ".Add Failed", "Attempted to add a Entity_HasPathwayComponent for a profile. The process appeared to not work, but there was no exception, so we have no message, or no clue. Parent Profile: {0}, Type: {1}, learningOppId: {2}", parentUid, parent.EntityType, pathwayComponentId);
						EmailManager.NotifyAdmin( thisClassName + ".Add Failed", message );
					}
				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", "Entity_PathwayComponent" );
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
		public bool Replace( Guid parentUid, int componentRelationshipTypeId, List<PathwayComponent> profiles, ref SaveStatus status )
		{
			//23-04-28 mp - need to remove existing not in the current import
			//if ( profiles == null || !profiles.Any() )
			//{
   //             return true;
			//}
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
					if ( profiles == null || !profiles.Any() )
					{
                        //check for existing that should be removed
                        var existing = context.Entity_HasPathwayComponent.Where( s => s.EntityId == parent.Id && s.ComponentRelationshipTypeId == componentRelationshipTypeId ).ToList();
						if ( existing != null && existing.Any() )
							foreach ( var item in existing )
							{
								context.Entity_HasPathwayComponent.Remove( item );
								context.SaveChanges();
							}
                    }
					else
					{
						var existing = context.Entity_HasPathwayComponent.Where( s => s.EntityId == parent.Id && s.ComponentRelationshipTypeId == componentRelationshipTypeId );
						var inputIds = profiles.Select( x => x.Id ).ToList();
						var existingIds = existing.Select( x => x.PathwayComponentId ).ToList();

						//delete records which are not selected 
						var notExisting = existing.Where( x => !inputIds.Contains( x.PathwayComponentId ) ).ToList();
						foreach ( var item in notExisting )
						{
							context.Entity_HasPathwayComponent.Remove( item );
							context.SaveChanges();
						}
						//only get profiles where not existing
						var newProfiles = profiles.Where( x => !existingIds.Contains( x.Id ) ).ToList();
						if ( existing != null && existing.Count() > 0 && profiles.Count() > 0 )
						{
							LoggingHelper.DoTrace( 6, string.Format( thisClassName + ".Replace. Existing: {0}, input: {1}, Not existing(to delete): {2}, newProfiles: {3}", existing.Count(), profiles.Count(), notExisting.Count(), newProfiles.Count() ) );

							if ( existing.Count() != profiles.Count() )
							{

							}
						}
						foreach ( var entity in newProfiles )
						{
							//if there are no existing, optimize by not doing check. What about duplicates?
							efEntity = new DBEntity
							{
								EntityId = parent.Id,
								ComponentRelationshipTypeId = componentRelationshipTypeId,
								PathwayComponentId = entity.Id,
								Created = DateTime.Now,
							};
							context.Entity_HasPathwayComponent.Add( efEntity );
							count = context.SaveChanges();

						} //foreach

					}
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

		public bool Delete( Guid parentUid, int recordId, ref string statusMessage )
		{
			bool isValid = false;
			if ( recordId == 0 )
			{
				statusMessage = "Error - missing an identifier for the Entity_HasPathwayComponent to remove";
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
				DBEntity efEntity = context.Entity_HasPathwayComponent
								.FirstOrDefault( s => s.EntityId == parent.Id && s.PathwayComponentId == recordId );

				if ( efEntity != null && efEntity.Id > 0 )
				{
					context.Entity_HasPathwayComponent.Remove( efEntity );
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
		public bool DeleteAll( Guid parentUid, SaveStatus status )
		{
			bool isValid = true;
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
					//var results = context.Entity_HasPathwayComponent.Where( s => s.EntityId == parent.Id ).ToList();
					//if ( results == null || results.Count == 0 )
					//	return true;
					//foreach ( var item in results )
					//{
					//	string statusMessage = "";
					//	//we have a trigger for this
					//	new EntityManager().Delete( item.RowId, ref statusMessage );

					//	context.Entity_HasPathwayComponent.Remove( item );
					//	int count = context.SaveChanges();
					//	if ( count > 0 )
					//	{
					//		isValid = true;
					//	}
					//	else
					//	{
					//		//if doing a delete on spec, may not have been any properties
					//	}
					//}

				context.Entity_HasPathwayComponent.RemoveRange( context.Entity_HasPathwayComponent.Where( s => s.EntityId == parent.Id ) );
				int count = context.SaveChanges();
				if ( count > 0 )
				{
					isValid = true;
					//status.AddError( string.Format( "removed {0} related relationships.", count ) );
				}
			}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".DeleteAll( Guid parentUid, ref SaveStatus status )" );
			}
			return isValid;

		}
		public bool DeleteNotInList( string pathwayCTID, List<PathwayComponent> list, ref SaveStatus status )
		{
			bool isValid = true;
			if ( !list.Any() )
			{
				return true;
			}

			using ( var context = new EntityContext() )
			{
				var existing = context.Entity_HasPathwayComponent.Where( s => s.PathwayComponent.PathwayCTID == pathwayCTID ).ToList();
				var inputIds = list.Select( x => x.CTID ).ToList();

				//delete records which are not selected 
				var notExisting = existing.Where( x => !inputIds.Contains( x.PathwayComponent.CTID ) ).ToList();
				foreach ( var item in notExisting )
				{
					context.Entity_HasPathwayComponent.Remove( item );
					context.SaveChanges();
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
		public bool DeleteNotInList( Guid parentUid, List<PathwayComponent> list, ref SaveStatus status )
		{
			bool isValid = true;
			if ( !list.Any() )
			{
				return true;
			}
			Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				status.AddError( thisClassName + string.Format( ".DeleteNotInList() Error - the parent entity for [{0}] was not found.", parentUid ) );
				return false;
			}

			using ( var context = new EntityContext() )
			{
				var existing = context.Entity_HasPathwayComponent.Where( s => s.EntityId == parent.Id ).ToList();
				var inputIds = list.Select( x => x.Id ).ToList();

				//delete records which are not selected 
				var notExisting = existing.Where( x => !inputIds.Contains( x.PathwayComponentId ) ).ToList();
				foreach ( var item in notExisting )
				{
					context.Entity_HasPathwayComponent.Remove( item );
					context.SaveChanges();
				}

			}
			return isValid;

		}
		#endregion
	}
}

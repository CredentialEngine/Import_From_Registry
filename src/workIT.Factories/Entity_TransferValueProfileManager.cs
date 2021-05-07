using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;
using ThisEntity = workIT.Models.Common.Entity_TransferValueProfile;
using DBEntity = workIT.Data.Tables.Entity_TransferValueProfile;
using EntityContext = workIT.Data.Tables.workITEntities;

using workIT.Utilities;
using CM = workIT.Models.Common;
using EM = workIT.Data.Tables;

namespace workIT.Factories
{
	public class Entity_TransferValueProfileManager : BaseFactory
	{
		static string thisClassName = "Entity_TransferValueProfileManager";
		public static int RelationshipType_HasPart = 1;
		#region Entity TransferValueProfile Persistance ===================

		public bool SaveList( List<int> list, Guid parentUid, ref SaveStatus status )
		{
			if ( list == null || list.Count == 0 )
				return true;
			int newId = 0;

			bool isAllValid = true;
			foreach ( var item in list )
			{
				newId = Add( parentUid, item, ref status );
				if ( newId == 0 )
					isAllValid = false;
			}

			return isAllValid;
		}

		/// <summary>
		/// Add an Entity TransferValueProfile
		/// </summary>
		/// <param name="parentUid"></param>
		/// <param name="transferValueProfileId"></param>
		/// <param name="relationshipTypeId"></param>
		/// <param name="allowMultiples">If false, check if an TransferValueProfile exists. If found, do an update</param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public int Add( Guid parentUid, int transferValueProfileId, ref SaveStatus status )
		{
			int id = 0;
			int count = 0;
			if ( transferValueProfileId < 1 )
			{
				status.AddError( string.Format( "A valid TransferValueProfile identifier ({0}) was not provided to the {1}.EntityTransferValueProfile_Add method.", transferValueProfileId, thisClassName ) );
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
					efEntity = context.Entity_TransferValueProfile
							.FirstOrDefault( s => s.EntityId == parent.Id && s.TransferValueProfileId == transferValueProfileId );

					if ( efEntity != null && efEntity.Id > 0 )
					{
						return 0;
					}

					efEntity = new DBEntity();
					efEntity.EntityId = parent.Id;
					efEntity.TransferValueProfileId = transferValueProfileId;
					efEntity.Created = System.DateTime.Now;

					context.Entity_TransferValueProfile.Add( efEntity );

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
						string message = thisClassName + string.Format( ".Add Failed", "Attempted to add a TransferValueProfile for a profile. The process appeared to not work, but there was no exception, so we have no message, or no clue. Parent Profile: {0}, Type: {1}, TransferValueProfileId: {2}", parentUid, parent.EntityType, transferValueProfileId );
						EmailManager.NotifyAdmin( thisClassName + ".Add Failed", message );
					}
				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", "Entity_TransferValueProfile" );
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
		/// Delete all relationships for parent - FOR REFERENCES, DELETE ACTUAL TransferValueProfile AS WELL
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
				var results = context.Entity_TransferValueProfile
							.Where( s => s.EntityId == parent.Id )
							.OrderBy( s => s.TransferValueProfile.Name )
							.ToList();
				if ( results == null || results.Count == 0 )
				{
					return true;
				}

				foreach ( var item in results )
				{
					//if a reference, delete actual TransferValueProfile if not used elsewhere
					if ( item.TransferValueProfile != null && item.TransferValueProfile.EntityStateId == 2 )
					{
						//do a getall for the current TransferValueProfileId. If the TransferValueProfile is only referenced once, delete the TransferValueProfile as well.
						var exists = context.Entity_TransferValueProfile
							.Where( s => s.TransferValueProfileId == item.TransferValueProfileId )
							.ToList();
						if ( exists != null && exists.Count() == 1 )
						{
							var statusMsg = "";
							//this method will also add pending reques to remove from elastic.
							//20-11-11 mp - BE CLEAR - ONLY DONE FOR A REFERENCE
							//actually this delete will probably also delete the Entity_TransferValueProfile
							//new TransferValueProfileManager().Delete( item.TransferValueProfileId, ref statusMsg );
							//continue;
						}
					}
					context.Entity_TransferValueProfile.Remove( item );
					count = context.SaveChanges();
					if ( count > 0 )
					{

					}
				}
			}

            return isValid;
        }
		#endregion


		/// <summary>
		/// Get all TransferValueProfiles for the provided entity
		/// The returned entities are just the base
		/// </summary>
		/// <param name="parentUid"></param>
		/// <returns></returnsThisEntity
		public static List<TransferValueProfile> GetAll( Guid parentUid )
		{
			List<TransferValueProfile> list = new List<TransferValueProfile>();
			TransferValueProfile entity = new TransferValueProfile();

			Entity parent = EntityManager.GetEntity( parentUid );
			LoggingHelper.DoTrace( 7, string.Format( "EntityTransferValueProfiles_GetAll: parentUid:{0} entityId:{1}, e.EntityTypeId:{2}", parentUid, parent.Id, parent.EntityTypeId ) );

			try
			{
				using ( var context = new EntityContext() )
				{
					List<DBEntity> results = context.Entity_TransferValueProfile
							.Where( s => s.EntityId == parent.Id  )
							.OrderBy( s => s.TransferValueProfile.Name )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( DBEntity item in results )
						{
							entity = new TransferValueProfile();

                            //need to distinguish between on a detail page for conditions and TransferValueProfile detail
                            //would usually only want basics here??
                            //17-05-26 mp- change to MapFromDB_Basic
                            if ( item.TransferValueProfile != null && item.TransferValueProfile.EntityStateId > 1 )
                            {
                                TransferValueProfileManager.MapFromDB( item.TransferValueProfile, entity, false);
                                list.Add( entity );
                            }
						}
					}
					return list;
				}


			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + string.Format( ".EntityTransferValueProfiles_GetAll. Guid: {0}, parentType: {1} ({2}), ", parentUid, parent.EntityType, parent.EntityBaseId ) );

			}
			return list;
		}

		public static ThisEntity Get( int parentId, int TransferValueProfileId )
		{
			ThisEntity entity = new ThisEntity();
			if ( parentId < 1 || TransferValueProfileId < 1 )
			{
				return entity;
			}
			try
			{
				using ( var context = new EntityContext() )
				{
					EM.Entity_TransferValueProfile from = context.Entity_TransferValueProfile
							.SingleOrDefault( s => s.TransferValueProfileId == TransferValueProfileId && s.EntityId == parentId );

					if ( from != null && from.Id > 0 )
					{
						entity.Id = from.Id;
						entity.TransferValueProfileId = from.TransferValueProfileId;
						entity.EntityId = from.EntityId;
				
						entity.TransferValueProfile = new TransferValueProfile();
						TransferValueProfileManager.MapFromDB( from.TransferValueProfile, entity.TransferValueProfile );

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

using System;
using System.Collections.Generic;
using System.Linq;

using workIT.Models;
using workIT.Models.Common;
using workIT.Utilities;

using DBEntity = workIT.Data.Tables.Entity_HoldersProfile;
using EntityContext = workIT.Data.Tables.workITEntities;

namespace workIT.Factories
{
	public class Entity_HoldersProfileManager : BaseFactory
	{
		static string thisClassName = "Entity_HoldersProfileManager";
		public static int RelationshipType_HasPart = 1;
		#region Entity Holders Persistance ===================

		/// <summary>
		/// Add an Entity_HoldersProfile
		/// </summary>
		/// <param name="parentUid"></param>
		/// <param name="HoldersProfileId"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public int Add( Guid parentUid,
					int holdersProfileId, 
					ref SaveStatus status )
		{
			int id = 0;
			int count = 0;
			if ( holdersProfileId == 0 )
			{
				status.AddError( string.Format( "A valid Holders identifier was not provided to the {0}.EntityHolders_Add method.", thisClassName ) );
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
					efEntity = context.Entity_HoldersProfile
							.FirstOrDefault( s => s.EntityId == parent.Id && s.HoldersProfileId == holdersProfileId );

					if ( efEntity != null && efEntity.Id > 0 )
					{
						return 0;
					}

					efEntity = new DBEntity();
					efEntity.EntityId = parent.Id;
					efEntity.HoldersProfileId = holdersProfileId;
					efEntity.Created = System.DateTime.Now;

					context.Entity_HoldersProfile.Add( efEntity );

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
						string message = thisClassName + string.Format( ".Add Failed", "Attempted to add an Entity_HoldersProfile. The process appeared to not work, but there was no exception, so we have no message, or no clue. Parent Profile: {0}, Type: {1}, HoldersId: {2}", parentUid, parent.EntityType, holdersProfileId );
						EmailManager.NotifyAdmin( thisClassName + ".Add Failed", message );
					}
				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", "Entity_Holders" );
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
		/// Delete all HoldersProfiles for parent
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public bool DeleteAll( Guid parentUid, ref SaveStatus status )
		{
			bool isValid = true;
			int count = 0;
			Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				status.AddError( thisClassName + ".DeleteAll Error - the provided target parent entity was not provided." );
				return false;
			}
		
			using ( var context = new EntityContext() )
			{
				//check if target is a reference object and is only in use here
				var results = context.Entity_HoldersProfile
							.Where( s => s.EntityId == parent.Id )
							.OrderBy( s => s.Created )
							.ToList();
				if ( results == null || results.Count == 0 )
				{
					return true;
				}

				foreach ( var item in results )
				{
					if ( item.HoldersProfile != null && item.HoldersProfile.EntityStateId > 0 )
					{
						var messages = new List<string>();
						new HoldersProfileManager().Delete( item.Id, ref messages );
						if ( messages.Any() )
							status.AddErrorRange( messages );
						continue;
					}
					context.Entity_HoldersProfile.Remove( item );
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
		/// Get all HoldersProfile for the provided entity
		/// The returned entities are just the base
		/// </summary>
		/// <param name="parentUid"></param>
		/// <returns></returnsThisEntity
		public static List<HoldersProfile> GetAll( Entity parent, bool includingParts = true )
		{
			var list = new List<HoldersProfile>();
			var entity = new HoldersProfile();

			//Entity parent = EntityManager.GetEntity( parentUid );
			LoggingHelper.DoTrace( 7, string.Format( thisClassName + ".GetAll: parentUid:{0} entityId:{1}, e.EntityTypeId:{2}", parent.EntityUid, parent.Id, parent.EntityTypeId ) );

			try
			{
				using ( var context = new EntityContext() )
				{
					List<DBEntity> results = context.Entity_HoldersProfile
							.Where( s => s.EntityId == parent.Id )
							.OrderBy( s => s.Created )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( DBEntity item in results )
						{
							entity = new HoldersProfile();

							//need to distinguish between on a detail page for conditions and Holders detail
							//would usually only want basics here??
							//17-05-26 mp- change to MapFromDB_Basic
							if ( item.HoldersProfile != null && item.HoldersProfile.EntityStateId > 1 )
							{
								HoldersProfileManager.MapFromDB( item.HoldersProfile, entity, includingParts );
								list.Add( entity );
							}
						}
					}
					return list;
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetAll" );
			}
			return list;
		}
		/// <summary>
		/// Format a summary of the HoldersProfile for use in search and gray boxes
		/// </summary>
		/// <param name="parentUid"></param>
		/// <returns></returns>
		public static string GetSummary( Guid parentUid )
		{
			var list = new List<HoldersProfile>();
			var entity = new HoldersProfile();

			Entity parent = EntityManager.GetEntity( parentUid );
			LoggingHelper.DoTrace( 7, string.Format( thisClassName + ".GetAll: parentUid:{0} entityId:{1}, e.EntityTypeId:{2}", parentUid, parent.Id, parent.EntityTypeId ) );
			var summary = "";
			var lineBreak = "";
			try
			{
				using ( var context = new EntityContext() )
				{
					List<DBEntity> results = context.Entity_HoldersProfile
							.Where( s => s.EntityId == parent.Id )
							.OrderBy( s => s.Created )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( DBEntity item in results )
						{
							entity = new HoldersProfile();
							if ( item.HoldersProfile != null && item.HoldersProfile.EntityStateId > 1 )
							{
								if ( !string.IsNullOrWhiteSpace(item.HoldersProfile.Name) )
									summary = item.HoldersProfile.Name + lineBreak;
								else if ( !string.IsNullOrWhiteSpace( item.HoldersProfile.Description) )
								{
									summary = item.HoldersProfile.Description.Length < 200 ? item.HoldersProfile.Description : item.HoldersProfile.Description.Substring(0, 200) + "  ... " + lineBreak;
								}
								else
								{

								}
								if ( item.HoldersProfile.NumberAwarded > 0 )
									summary += string.Format( " Number awarded: {0}", item.HoldersProfile.NumberAwarded );
							}
							lineBreak = "<\br>";
						}
					}
					return summary;
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetSummary" );
			}
			return summary;
		}
		//unlikely to use
		//public static ThisEntity Get( int parentId, int holdersProfileId )
		//{
		//	ThisEntity entity = new ThisEntity();
		//	if ( parentId < 1 || holdersProfileId < 1 )
		//	{
		//		return entity;
		//	}
		//	try
		//	{
		//		using ( var context = new EntityContext() )
		//		{
		//			EM.Entity_HoldersProfile from = context.Entity_HoldersProfile
		//					.SingleOrDefault( s => s.HoldersProfileId == holdersProfileId && s.EntityId == parentId );

		//			if ( from != null && from.Id > 0 )
		//			{
		//				entity.Id = from.Id;
		//				entity.HoldersProfileId = from.HoldersProfileId;
		//				entity.EntityId = from.EntityId;
		//				//to.Credential = from.Credential;
		//				entity.HoldersProfile = new HoldersProfile();
		//				HoldersProfileManager.MapFromDB( from.HoldersProfile, entity.HoldersProfile,
		//						false //don't include parts?
		//						);

		//				if ( IsValidDate( from.Created ) )
		//					entity.Created = ( DateTime )from.Created;
		//			}
		//		}
		//	}
		//	catch ( Exception ex )
		//	{
		//		LoggingHelper.LogError( ex, thisClassName + ".Get" );
		//	}
		//	return entity;
		//}//


	}
}

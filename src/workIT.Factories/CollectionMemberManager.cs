using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.ProfileModels;
using workIT.Utilities;

using ApiFramework = workIT.Models.API.Collection;
using DBResource = workIT.Data.Tables.Collection_CollectionMember;
using EntityContext = workIT.Data.Tables.workITEntities;
using ThisResource = workIT.Models.Common.CollectionMember;



namespace workIT.Factories
{
	public class CollectionMemberManager : BaseFactory
	{
		static string thisClassName = "CollectionMemberManager";

		#region Persistance ===================

		public bool SaveList( int collectionId, List<ThisResource> list, ref SaveStatus status )
		{
			var addedMembers = new List<string>();
			//will need to do a delete all or take the approach for entity address:
			//- read
			//- look up by CTID
			//- update last updated
			//- at end delete records with an older last updated date
			DateTime updateDate = DateTime.Now;
			if ( IsValidDate( status.EnvelopeUpdatedDate ) )
			{
				//an individual member doesn't have a modified date, for purposes of deletes, maybe this should always be the current date
				updateDate = status.LocalUpdatedDate;
			}
			bool isAllValid = true;

			foreach ( var entity in list )
			{
				entity.CollectionId = collectionId;
				Save( entity, updateDate, ref status, ref addedMembers );
			}
			//prep for a bulk means to update pending index
			if ( addedMembers.Count > 0 )
            {

            }
			var usingDateCheck = false;
			//delete any records with last updated less than updateDate
			//actually the LastUpdated may not change?
			if ( !usingDateCheck )
			{
				var requestedList = list.Select( x => ( x.ProxyFor ?? "" ).ToLower() ).Distinct().ToList();
				//TODO - may want to get the type as well, in order to reindex. Although will need to get the Int Id. Can use entity_cache
				var existing = GetAllCTIDs( collectionId );
				//find existing not in requested
				//delete records which are not selected - must reindex as well
				var notExisting = existing.Where( x => !requestedList.Contains( x ) ).ToList();
				if ( notExisting.Any() )
				{
					DeleteAll( collectionId, notExisting, ref status );
				}
			}
			else
			{
				//OR
				DeleteAll( collectionId, ref status, updateDate );
			}
			return isAllValid;
		}

		/// <summary>
		/// Add/Update a CollectionMember
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public bool Save( ThisResource entity, DateTime updateDate, ref SaveStatus status, ref List<string> addedMembers )
		{
			bool isValid = true;
			int count = 0;

			DBResource efEntity = new DBResource();
			try
			{
				using ( var context = new EntityContext() )
				{

					if ( ValidateProfile( entity, ref status ) == false )
					{
						//return false;
					}
					//just in case
					if ( entity.Id == 0 )
					{
						//mostly likely will be zero, so do a look up
						efEntity = context.Collection_CollectionMember.FirstOrDefault( s => s.CollectionId == entity.CollectionId && s.ProxyFor.ToLower() == entity.ProxyFor.ToLower() );

						if ( efEntity != null && efEntity.Id > 0 )
						{
							//could have a change in date, etc, so update Id and continue to the update
							entity.Id = efEntity.Id;
						}
					}
					if ( entity.Id == 0 )
					{
						//add
						efEntity = new DBResource();
						MapToDB( entity, efEntity );

						if ( IsValidDate( status.EnvelopeCreatedDate ) )
							efEntity.Created = status.LocalCreatedDate;
						else
							efEntity.Created = DateTime.Now;
						//this is not the competency last updated
						efEntity.LastUpdated = entity.LastUpdated = updateDate;
						//future handling of last modified
						//if ( IsValidDate( entity.DateModified ) )
						//	efEntity.DateModified = (DateTime) entity.DateModified;
						//not sure we need this?
						efEntity.RowId = Guid.NewGuid();

						context.Collection_CollectionMember.Add( efEntity );

						count = context.SaveChanges();

						entity.Id = efEntity.Id;
						//entity.RowId = efEntity.RowId;
						if ( count == 0 )
						{
							status.AddWarning( string.Format( "Collection Id: {0}. Unable to add collection member: {1} <br\\> ", entity.CollectionId, entity.ProxyFor ) );
						}
						else
						{
							//TODO - a bulk method? Also a method to reindex using CTID
							addedMembers.Add( entity.ProxyFor );
							//perhaps collect and do at the end?
							//also may want to initiate the update after a certain number, or doing thousands could be unwieldly?
							var messages = new List<string>();
							if ( !string.IsNullOrWhiteSpace( entity.ProxyFor ))
								new SearchPendingReindexManager().Add( entity.ProxyFor, 1, ref messages );
						}
                    }
					else
					{
						if ( efEntity == null || efEntity.Id == 0 )
							efEntity = context.Collection_CollectionMember.FirstOrDefault( s => s.Id == entity.Id );
						if ( efEntity != null && efEntity.Id > 0 )
						{
							//entity.RowId = efEntity.RowId;
							//update
							MapToDB( entity, efEntity );

							//need to do the date check here, or may not be updated
							if ( IsValidDate( entity.Created ) )
								efEntity.Created = ( DateTime ) entity.Created;

							//if ( IsValidDate( status.EnvelopeCreatedDate ) && status.LocalCreatedDate < efEntity.Created )
							//{
							//	efEntity.Created = status.LocalCreatedDate;
							//}
							//future handling of last
							//if ( IsValidDate( entity.DateModified ) )
							//	efEntity.DateModified = (DateTime) entity.DateModified;

							efEntity.LastUpdated = entity.LastUpdated = updateDate;
							//has changed?	 - should always chg based on the latter statement
							//TBD: would be nice not to take the hit on 3K updates???
							if ( HasStateChanged( context ) )
							{
								//if ( IsValidDate( status.EnvelopeUpdatedDate ) )
								//	efEntity.LastUpdated = status.LocalUpdatedDate;
								//else
								//	efEntity.LastUpdated = DateTime.Now;

								count = context.SaveChanges();

                            }
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".Save()" );
			}

			return isValid;
		}
		public bool DeleteAll( int collectionId, List<string> input, ref SaveStatus status )
		{
			bool isValid = true;
			//Entity parent = EntityManager.GetEntity( parentUid );
			if ( collectionId == 0 )
			{
				status.AddError( thisClassName + ". Error - the parent collectionId was not provided." );
				return false;
			}
			var messages = new List<string>();
			try
			{
				using ( var context = new EntityContext() )
				{			
					foreach ( var item in input )
					{
						//hold on - also have to reindex anything affected by the delete
						//but don't have the Id. if proxyFor is the ctid, we can use entity.cache
						//var record = EntityManager.EntityCacheGetByCTID( item );
						//if ( record != null )
						//	new SearchPendingReindexManager().Add( record.EntityTypeId, record.Id, 1, ref messages );
						if ( !string.IsNullOrWhiteSpace( item ) )
							new SearchPendingReindexManager().Add( item, 1, ref messages );
						//now remove
						var effEntity = context.Collection_CollectionMember
							.FirstOrDefault( s => s.CollectionId == collectionId && s.ProxyFor.ToLower() == item.ToLower() );
						context.Collection_CollectionMember.Remove( effEntity );
						var count = context.SaveChanges();
						if ( count > 0 )
						{

						}
					}
				}
			}
			catch ( Exception ex )
			{
				var msg = BaseFactory.FormatExceptions( ex );
				LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DeleteAll. collectionId: {0},  exception: {1}", collectionId, msg ) );
			}
			return isValid;
		}

		/// <summary>
		/// Watch the performance on this. May be better to have a statusId/entityStateId and set all to zero, and delete later. 
		/// </summary>
		/// <param name="collectionId"></param>
		/// <param name="status"></param>
		/// <param name="lastUpdated"></param>
		/// <returns></returns>
		public bool DeleteAll( int collectionId, ref SaveStatus status, DateTime lastUpdated)
		{
			bool isValid = true;
			//Entity parent = EntityManager.GetEntity( parentUid );
			if ( collectionId == 0 )
			{
				status.AddError( thisClassName + ". Error - the parent collectionId was not provided." );
				return false;
			}
			int expectedDeleteCount = 0;
			var messages = new List<string>();
			try
			{
				using ( var context = new EntityContext() )
				{

					var results = context.Collection_CollectionMember.Where( s => s.CollectionId == collectionId && ( s.LastUpdated < lastUpdated ) )
				.ToList();
					if ( results == null || results.Count == 0 )
						return true;
					expectedDeleteCount = results.Count;

					foreach ( var item in results )
					{
						//hold on - also have to reindex anything affected by the delete
						//but don't have the Id. if proxyFor is the ctid, we can use entity.cache
						//var record = EntityManager.EntityCacheGetByCTID( item.ProxyFor );
						//if ( record != null )
						//	new SearchPendingReindexManager().Add( record.EntityTypeId, record.Id, 1, ref messages );
						if ( !string.IsNullOrWhiteSpace( item.ProxyFor ) )
							new SearchPendingReindexManager().Add( item.ProxyFor, 1, ref messages );
						//now remove
						context.Collection_CollectionMember.Remove( item );
						var count = context.SaveChanges();
						if ( count > 0 )
						{

						}
					}
				}
			}
			catch ( Exception ex )
			{
				var msg = BaseFactory.FormatExceptions( ex );
				LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DeleteAll. collectionId: {0},  exception: {1}", collectionId, msg ) );
			}
			return isValid;
		}
		//

		public bool ValidateProfile( ThisResource profile, ref SaveStatus status )
		{
			status.HasSectionErrors = false;

			//not much to do?

			return status.WasSectionValid;
		}

		#endregion
		#region  retrieval ==================
		/// <summary>
		/// get all collection members for the provided CTID
		/// </summary>
		/// <param name="ctid"></param>
		/// <returns></returns>
		public static List<ThisResource> GetMemberOfCollections( string ctid )
		{
			List<ThisResource> output = new List<ThisResource>();
			ThisResource entity = new ThisResource();
			if ( string.IsNullOrWhiteSpace( ctid ) )
				return output;

			try
			{
				using ( var context = new EntityContext() )
				{
					//find all collections for CTID
					var list = context.Collection_CollectionMember
							.Where( s => s.ProxyFor.ToLower() == ctid.ToLower() ).ToList();
					if ( list != null && list.Count > 0 )
					{
						foreach( var item in list )
                        {
							entity = new ThisResource();
							if ( item.Collection != null )
							{
								entity.Collection = new Collection();
								CollectionManager.MapFromDB( item.Collection, entity.Collection );
								entity.CollectionName = item.Collection.Name;
								//do we want type as well!
								entity.CollectionType = EntityPropertyManager.FillEnumeration( item.Collection.RowId, CodesManager.PROPERTY_CATEGORY_COLLECTION_CATEGORY );
							}

							MapFromDB( item, entity );
							output.Add( entity );
						}
					}					
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetCollections" );
			}
			return output;
		}//

		public static List<TopLevelObject> GetMemberOfCollections( string entityType, int recordId )
		{
			var output = new List<TopLevelObject>();
			var entity = new TopLevelObject();
			if ( string.IsNullOrWhiteSpace( entityType ) || recordId < 1 )
				return output;

			try
			{
				using ( var context = new EntityContext() )
				{
					var query = from cm in context.Collection_CollectionMember
								join e in context.Entity_Cache on cm.ProxyFor equals e.CTID
								where ( e.BaseId == recordId && e.EntityType.ToLower() == entityType.ToLower() )
								select new { Collection = cm.Collection.Name, Id = cm.CollectionId, CTID = cm.Collection.CTID};

					var list = query.OrderBy( m => m.Collection ).ToList();
					if ( list != null && list.Count > 0 )
					{
						foreach ( var item in list )
						{
							entity = new TopLevelObject()
							{
								Name = item.Collection,
								CTID = item.CTID,
								Id = item.Id,
								FriendlyName = FormatFriendlyTitle( item.Collection )
						};
							output.Add( entity );
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetCollections" );
			}
			return output;
		}//


		public static List<string> GetAllCTIDs( int collectionId )
		{
			var existing = GetAll( collectionId );
			if ( existing == null || existing.Count == 0 )
				return new List<string>();

			var output = existing.Select( x => ( x.ProxyFor ?? "" ).ToLower() ).Distinct().ToList();
			return output;
		}//

		public static List<ThisResource> GetAll( int collectionId )
		{
			var output = new List<ThisResource>();
			ThisResource entity = new ThisResource();

			try
			{
				using ( var context = new EntityContext() )
				{
					//
					var list = context.Collection_CollectionMember
							.Where( s => s.CollectionId == collectionId ).ToList();
					foreach ( var item in list )
					{
						if ( item != null && item.Id > 0 )
						{
							if ( item.Collection != null )
							{
								entity.CollectionName = item.Collection.Name;
								//do we want type as well!
								entity.CollectionType = EntityPropertyManager.FillEnumeration( item.Collection.RowId, CodesManager.PROPERTY_CATEGORY_COLLECTION_CATEGORY );
							}
							//may only want CTID in some cases?
							MapFromDB( item, entity );
							output.Add(entity);
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetAll( int collectionId)" );
			}
			return output;
		}//

		public static List<CodeItem> GetCollectionMemberTypeTotal( int collectionId )
		{
			var output = new List<CodeItem>();
			var codeItem = new CodeItem();

			try
			{
				using ( var context = new EntityContext() )
				{
					//make sure indexed
					var query = from cm in context.Collection_CollectionMember
								join e in context.Codes_EntityTypes on cm.EntityTypeId equals e.Id
								where ( cm.CollectionId == collectionId && cm.EntityTypeId != null )
								group new { cm, e } by new { cm.CollectionId, cm.EntityTypeId, e.Title }
									into cmgrp
								select new { CollectionId = cmgrp.Key.CollectionId, EntityTypeId = cmgrp.Key.EntityTypeId, EntityType = cmgrp.Key.Title, Count = cmgrp.Count() };

					var result = query.OrderBy( m => m.CollectionId ).ThenBy( m => m.EntityType ).ToList();

					if ( result != null && result.Count() > 0 )
					{
						foreach ( var item in result )
						{
							codeItem = new CodeItem()
							{
								EntityType = item.EntityType,
								EntityTypeId = item.EntityTypeId ?? 0,
								Totals = item.Count
							};
							output.Add( codeItem );
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetCollectionMemberTypeTotal( int collectionId)" );
			}
			return output;
		}//


		/// <summary>
		/// Retrieve by collectionId and ProxyFor
		/// **assuming for now there can not be duplicates**
		/// </summary>
		/// <param name="collectionId"></param>
		/// <param name="ctid"></param>
		/// <returns></returns>
		public static ThisResource Get( int collectionId, string ctid )
		{
			ThisResource entity = new ThisResource();
			if ( string.IsNullOrWhiteSpace( ctid ) )
				return entity;
			try
			{
				using ( var context = new EntityContext() )
				{
					//
					DBResource item = context.Collection_CollectionMember
							.FirstOrDefault( s => s.CollectionId == collectionId && s.ProxyFor.ToLower() == ctid.ToLower() );

					if ( item != null && item.Id > 0 )
					{
						if ( item.Collection != null )
						{
							entity.CollectionName = item.Collection.Name;
							//do we want type as well!
							entity.CollectionType = EntityPropertyManager.FillEnumeration( item.Collection.RowId, CodesManager.PROPERTY_CATEGORY_COLLECTION_CATEGORY );
						}
						MapFromDB( item, entity );
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".Get( int collectionId, string ctid )" );
			}
			return entity;
		}//
		public static ThisResource Get( int profileId )
		{
			ThisResource entity = new ThisResource();
			if ( profileId == 0 )
				return entity;
			try
			{
				using ( var context = new EntityContext() )
				{
					DBResource item = context.Collection_CollectionMember
							.SingleOrDefault( s => s.Id == profileId );

					if ( item != null && item.Id > 0 )
					{
						if ( item.Collection != null )
						{
							entity.CollectionName = item.Collection.Name;
							//do we want type as well!
							entity.CollectionType = EntityPropertyManager.FillEnumeration( item.Collection.RowId, CodesManager.PROPERTY_CATEGORY_COLLECTION_CATEGORY );
						}
						MapFromDB( item, entity );
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".Get" );
			}
			return entity;
		}//
		/// <summary>
		/// Check if there are any collection members for the provided entityTypeId
		/// </summary>
		/// <param name="entityTypeId"></param>
		/// <returns></returns>
		public static bool HasAnyForEntityType( int entityTypeId )
		{
			if ( entityTypeId == 0 )
				return false;
			try
			{
				using ( var context = new EntityContext() )
				{
					DBResource item = context.Collection_CollectionMember
							.FirstOrDefault( s => s.EntityTypeId == entityTypeId );

					if ( item != null && item.Id > 0 )
					{
						return true;
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".HasAnyEntityType" );
			}
			return false;
		}//

		public static void MapToDB( ThisResource input, DBResource output )
		{
			//want to ensure fields from create are not wiped
			if ( output.Id == 0 )
			{
				//look up entityType
				var cacheItem = EntityManager.EntityCacheGetByCTID( input.ProxyFor );
				if ( cacheItem != null && cacheItem.Id > 0)
                {
					output.EntityTypeId = cacheItem.EntityTypeId;
                } else
                {
					//log that resource was not found
					LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".MapToDB. For CollectionId: '{0}', a resource was not found for ProxyFor: '{1}'.", input.CollectionId, input.ProxyFor ) );
                }
			}
			output.Id = input.Id;

			output.CollectionId = input.CollectionId;
			output.Name = input.Name;
			output.Description = input.Description;
			output.ProxyFor = input.ProxyFor.ToLower();

			if ( IsValidDate( input.StartDate ) )
				output.StartDate = DateTime.Parse( input.StartDate );
			else //
				output.StartDate = null;

			if ( IsValidDate( input.EndDate ) )
				output.EndDate = DateTime.Parse( input.EndDate );
			else //handle reset
				output.EndDate = null;
		
			

		} //

		public static void MapFromDB( DBResource input, ThisResource output )
		{
			output.Id = input.Id;

			output.CollectionId = input.CollectionId;
			output.Name = input.Name;
			output.Description = input.Description;
			output.ProxyFor = input.ProxyFor;
			if ( input.EntityTypeId != null )
			{
				output.EntityTypeId = ( int ) input.EntityTypeId;
				//to avoid doing this 3K+ times, use a cache
				var entityType = CodesManager.Codes_EntityType_Get( output.EntityTypeId);
			}
			if ( IsValidDate( input.StartDate ) )
				output.StartDate = (( DateTime ) input.StartDate).ToString( "yyyy-MM-dd" );
			else //
				output.StartDate = null;

			if ( IsValidDate( input.EndDate ) )
				output.EndDate = (( DateTime ) input.EndDate).ToString("yyyy-MM-dd");
			else //handle reset
				output.EndDate = null;

			if ( input.Created != null )
				output.Created = ( DateTime ) input.Created;
			if ( input.LastUpdated != null )
				output.LastUpdated = ( DateTime ) input.LastUpdated;
		}

		#endregion


	}
}

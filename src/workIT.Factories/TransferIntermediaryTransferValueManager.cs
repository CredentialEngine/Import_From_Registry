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
using DBEntity = workIT.Data.Tables.TransferIntermediary_TransferValue;
using EntityContext = workIT.Data.Tables.workITEntities;
using ThisEntity = workIT.Models.Common.TransferIntermediaryTransferValue;



namespace workIT.Factories
{
	public class TransferIntermediaryTransferValueManager : BaseFactory
	{
		static string thisClassName = "TransferIntermediaryTransferValueManager";

		#region Persistance ===================

		/// <summary>
		/// Save list using TopLevelObjects (as used by the import)
		/// </summary>
		/// <param name="transferIntermediaryId"></param>
		/// <param name="list"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public bool SaveList( int transferIntermediaryId, List<TopLevelObject> list, ref SaveStatus status )
		{
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
				Save( transferIntermediaryId, entity, ref status );
			}

			var usingDateCheck = false;
			//delete any records with last updated less than updateDate
			//actually the LastUpdated may not change?
			if ( !usingDateCheck )
			{
				var requestedList = list.Select( x => ( x.CTID ?? "" ).ToLower() ).Distinct().ToList();
				var existing = GetAllTVPCTIDs( transferIntermediaryId );
				//find existing not in requested
				//delete records which are not selected 
				var notExisting = existing.Where( x => !requestedList.Contains( x ) ).ToList();
				if ( notExisting.Any() )
				{
					DeleteAll( transferIntermediaryId, notExisting, ref status );
				}
			}
			//else
			//{
			//	//OR
			//	DeleteAll( transferIntermediaryId, ref status, updateDate );
			//}
			return isAllValid;
		}

		/// <summary>
		/// Add a TransferIntermediary.TransferValue
		/// </summary>
		/// <param name="transferIntermediaryId"></param>
		/// <param name="entity"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public bool Save( int transferIntermediaryId, TopLevelObject entity, ref SaveStatus status )
		{
			bool isValid = true;
			int count = 0;

			DBEntity efEntity = new DBEntity();
			try
			{
				using ( var context = new EntityContext() )
				{

					//hmmm, do need the Entity.Id?
					var exists = Get( transferIntermediaryId, entity.Id );
					if (exists.Id > 0 )
						return true;

					//add
					efEntity = new DBEntity()
					{
						TransferIntermediaryId = transferIntermediaryId,
						TransferValueProfileId = entity.Id
					};

					if ( IsValidDate( status.EnvelopeCreatedDate ) )
						efEntity.Created = status.LocalCreatedDate;
					else
						efEntity.Created = DateTime.Now;

					context.TransferIntermediary_TransferValue.Add( efEntity );

					count = context.SaveChanges();

					entity.Id = efEntity.Id;
					//entity.RowId = efEntity.RowId;
					if ( count == 0 )
					{
						status.AddWarning( string.Format( "TransferIntermediaryId Id: {0}. Unable to add TransferIntermediary.TransferValueId: {1}.", transferIntermediaryId, entity.Id ) );
					}		
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".Save()" );
			}
			return isValid;
		}
		/// <summary>
		/// Delete all using a list of TVP CTIDs
		/// </summary>
		/// <param name="transferIntermediaryId"></param>
		/// <param name="input">list of TVP CTIDs</param>
		/// <param name="status"></param>
		/// <returns></returns>
		public bool DeleteAll( int transferIntermediaryId, List<string> input, ref SaveStatus status )
		{
			bool isValid = true;
			//Entity parent = EntityManager.GetEntity( parentUid );
			if ( transferIntermediaryId == 0 )
			{
				status.AddError( thisClassName + ". Error - the parent transferIntermediaryId was not provided." );
				return false;
			}
			var messages = new List<string>();
			try
			{
				using ( var context = new EntityContext() )
				{
					foreach ( var item in input )
					{
						//Also have to reindex anything affected by the delete
						//but don't have the Id. if proxyFor is the ctid, we can use entity.cache
						var record = EntityManager.EntityCacheGetByCTID( item );
						if ( record != null )
							new SearchPendingReindexManager().Add( record.EntityTypeId, record.Id, 1, ref messages );
						//now remove
						var effEntity = context.TransferIntermediary_TransferValue
							.FirstOrDefault( s => s.TransferIntermediaryId == transferIntermediaryId && s.TransferValueProfileId == record.Id);
						context.TransferIntermediary_TransferValue.Remove( effEntity );
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
				LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DeleteAll. transferIntermediaryId: {0},  exception: {1}", transferIntermediaryId, msg ) );
			}
			return isValid;
		}

		///// <summary>
		///// Can't use this approach, no lastupdated date.
		///// </summary>
		///// <param name="transferIntermediaryId"></param>
		///// <param name="status"></param>
		///// <param name="lastUpdated"></param>
		///// <returns></returns>
		//public bool DeleteAll( int transferIntermediaryId, ref SaveStatus status, DateTime lastUpdated )
		//{
		//	bool isValid = true;
		//	//Entity parent = EntityManager.GetEntity( parentUid );
		//	if ( transferIntermediaryId == 0 )
		//	{
		//		status.AddError( thisClassName + ". Error - the parent transferIntermediaryId was not provided." );
		//		return false;
		//	}
		//	int expectedDeleteCount = 0;
		//	var messages = new List<string>();
		//	try
		//	{
		//		using ( var context = new EntityContext() )
		//		{

		//			var results = context.TransferIntermediary_TransferValue.Where( s => s.TransferIntermediaryId == transferIntermediaryId && ( s.LastUpdated < lastUpdated ) )
		//		.ToList();
		//			if ( results == null || results.Count == 0 )
		//				return true;
		//			expectedDeleteCount = results.Count;

		//			foreach ( var item in results )
		//			{
		//				//hold on - also have to reindex anything affected by the delete
		//				//but don't have the Id. if proxyFor is the ctid, we can use entity.cache
		//				var record = EntityManager.EntityCacheGetByCTID( item.ProxyFor );
		//				if ( record != null )
		//					new SearchPendingReindexManager().Add( record.EntityTypeId, record.Id, 1, ref messages );
		//				//now remove
		//				context.TransferIntermediary_TransferValue.Remove( item );
		//				var count = context.SaveChanges();
		//				if ( count > 0 )
		//				{

		//				}
		//			}
		//		}
		//	}
		//	catch ( Exception ex )
		//	{
		//		var msg = BaseFactory.FormatExceptions( ex );
		//		LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DeleteAll. transferIntermediaryId: {0},  exception: {1}", transferIntermediaryId, msg ) );
		//	}
		//	return isValid;
		//}
		//


		#endregion
		#region  retrieval ==================

		public static List<string> GetAllTVPCTIDs( int transferIntermediaryId )
		{
			var existing = GetAll( transferIntermediaryId );
			if ( existing == null || existing.Count == 0 )
				return new List<string>();

			var output = existing.Select( x => ( x.TransferValueProfileCTID ?? "" ).ToLower() ).Distinct().ToList();
			return output;
		}//

		/// <summary>
		/// Get all transfer values for a transfer intermediary
		/// </summary>
		/// <param name="transferIntermediaryId"></param>
		/// <returns></returns>
		public static List<ThisEntity> GetAll( int transferIntermediaryId )
		{
			var output = new List<ThisEntity>();
			ThisEntity entity = new ThisEntity();

			try
			{
				using ( var context = new EntityContext() )
				{
					//
					var list = context.TransferIntermediary_TransferValue
							.Where( s => s.TransferIntermediaryId == transferIntermediaryId ).ToList();
					foreach ( var item in list )
					{
                        entity = new ThisEntity();
                        if ( item != null && item.Id > 0 )
						{
							if ( item.TransferValueProfile != null )
							{
								entity.TransferIntermediaryName = item.TransferValueProfile.Name;
							}
							//may only want CTID in some cases?
							MapFromDB( item, entity );
							output.Add( entity );
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetAll( int transferIntermediaryId)" );
			}
			return output;
		}//

		/// <summary>
		/// Get all transfer intermediaries for a transfer value
		/// </summary>
		/// <param name="transferValueProfileId"></param>
		/// <returns></returns>
		public static List<TopLevelObject> GetAllTransferIntermediariesForTVP( int transferValueProfileId )
		{
			var output = new List<TopLevelObject>();
			TopLevelObject entity = new TopLevelObject();

			try
			{
				using ( var context = new EntityContext() )
				{
					//
					var list = context.TransferIntermediary_TransferValue
							.Where( s => s.TransferValueProfileId == transferValueProfileId ).ToList();
					foreach ( var item in list )
					{
						if ( item != null && item.Id > 0 )
						{
							if ( item.TransferIntermediary != null )
							{
								entity = new TopLevelObject()
								{
									Id = item.TransferIntermediary.Id,
									Name = item.TransferIntermediary.Name,
									Description = item.TransferIntermediary.Description,
									CTID = item.TransferIntermediary.CTID,
									SubjectWebpage = item.TransferIntermediary.SubjectWebpage,
									EntityTypeId = CodesManager.ENTITY_TYPE_TRANSFER_INTERMEDIARY,
									EntityType = "TransferIntermediary"
									
								};
								output.Add( entity );
							}
							else
                            {
								//this of course should never happen.
                            }							
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetAllTransferIntermediariesForTVP()" );
			}
			return output;
		}//


		/// <summary>
		/// Retrieve by transferIntermediaryId and TransferValueId
		/// </summary>
		/// <param name="transferIntermediaryId"></param>
		/// <param name="ctid"></param>
		/// <returns></returns>
		public static ThisEntity Get( int transferIntermediaryId, int transferValueId )
		{
			ThisEntity entity = new ThisEntity();
			//can't happen
			if ( transferIntermediaryId == 0  || transferValueId == 0 )
				return entity;
			try
			{
				using ( var context = new EntityContext() )
				{
					//
					DBEntity item = context.TransferIntermediary_TransferValue
							.FirstOrDefault( s => s.TransferIntermediaryId == transferIntermediaryId && s.TransferValueProfileId == transferValueId );

					if ( item != null && item.Id > 0 )
					{
						if ( item.TransferIntermediary != null )
						{
							entity.TransferIntermediaryName = item.TransferIntermediary.Name;
							entity.TransferIntermediaryId = item.TransferIntermediary.Id;
						}
						MapFromDB( item, entity );
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".Get( int transferIntermediaryId, int transferValueId )" );
			}
			return entity;
		}//
		//so simple, not used yet
		public static void MapToDB( ThisEntity input, DBEntity output )
		{
			//want to ensure fields from create are not wiped
			if ( output.Id == 0 )
			{

			}
			output.Id = input.Id;

			output.TransferIntermediaryId = input.TransferIntermediaryId;
			output.TransferValueProfileId = input.TransferValueProfileId;

		} //

		public static void MapFromDB( DBEntity input, ThisEntity output )
		{
			output.Id = input.Id;

			output.TransferIntermediaryId = input.TransferIntermediaryId;
			output.TransferValueProfileId = input.TransferValueProfileId;

			if ( input.TransferIntermediary != null )
			{
				output.TransferIntermediaryName = input.TransferIntermediary.Name;
				output.TransferIntermediaryId = input.TransferIntermediary.Id;
			}

			if ( input.TransferValueProfile != null )
			{
				output.TransferIntermediaryName = input.TransferValueProfile.Name;
				output.TransferIntermediaryId = input.TransferValueProfile.Id;
				output.TransferValueProfileCTID = input.TransferValueProfile.CTID;
				output.TransferValueProfileDescription = input.TransferValueProfile.Description;
			}
			if ( input.Created != null )
				output.Created = ( DateTime ) input.Created;
		}

		#endregion


	}
}

using System.Collections.Generic;

using workIT.Factories;
using workIT.Models;
using workIT.Models.Common;
using workIT.Models.Search;
using workIT.Utilities;

using ElasticHelper = workIT.Services.ElasticServices;
using EntityMgr = workIT.Factories.DataSetProfileManager;
using MP = workIT.Models.Common;
using ThisEntity = workIT.Models.QData.DataSetProfile;


namespace workIT.Services
{
	public class DataSetProfileServices
	{
		string thisClassName = "DataSetProfileServices";
		int classEntityTypeId = CodesManager.ENTITY_TYPE_DATASET_PROFILE;
		ActivityServices activityMgr = new ActivityServices();
		public List<string> messages = new List<string>();


		#region import

		public bool Import( ThisEntity entity, ref SaveStatus status )
		{

			bool isValid = new EntityMgr().Save( entity, null, ref status );
			List<string> messages = new List<string>();
			if ( entity.Id > 0 )
			{

				if ( UtilityManager.GetAppKeyValue( "delayingAllCacheUpdates", false ) == false )
				{
					//update cache - not applicable yet

					//update Elastic
					if ( Utilities.UtilityManager.GetAppKeyValue( "updatingElasticIndexImmediately", false ) )
					{
						//ElasticHelper.DataSetProfileProfile_UpdateIndex( entity.Id );
					}
					else
					{
						new SearchPendingReindexManager().Add( classEntityTypeId, entity.Id, 1, ref messages );
						if ( messages.Count > 0 )
							status.AddWarningRange( messages );
					}
					//also update related org
					//????????????
					if ( entity.DataProvider != null && entity.DataProvider.Id > 0 )
						new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_ORGANIZATION, entity.DataProvider.Id, 1, ref messages );
				}
				else
				{
					new SearchPendingReindexManager().Add( classEntityTypeId, entity.Id, 1, ref messages );
					if ( entity.DataProvider!= null && entity.DataProvider.Id > 0 )
						new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_ORGANIZATION, entity.DataProvider.Id, 1, ref messages );
					if ( messages.Count > 0 )
						status.AddWarningRange( messages );
				}
				//no caching needed yet
				//CacheManager.RemoveItemFromCache( "cframework", entity.Id );
			}

			return isValid;
		}
		public static ThisEntity HandlingExistingEntity( string ctid, ref SaveStatus status )
		{
			var entity = new ThisEntity();
			//warning- 
			entity = DataSetProfileManager.GetByCtid( ctid );
			if ( entity != null && entity.Id > 0 )
			{
				Entity relatedEntity = EntityManager.GetEntity( entity.RowId );
				if ( relatedEntity == null || relatedEntity.Id == 0 )
				{
					status.AddError( string.Format( "Error - the related Entity for DataSetProfile: '{0}' ({1}), was not found.", entity.Name, entity.Id ) );
					return entity;
				}
				//any clean up?



			}
			return entity;
		}
		#endregion

		public static ThisEntity GetByCtid( string ctid )
		{
			ThisEntity entity = new ThisEntity();
			entity = DataSetProfileManager.GetByCtid( ctid );
			return entity;
		}
	}
}

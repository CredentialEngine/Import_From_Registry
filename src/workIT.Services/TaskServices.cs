using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.Search;
using ElasticHelper = workIT.Services.ElasticServices;

using ThisEntity = workIT.Models.Common.Task;
using EntityMgr = workIT.Factories.TaskManager;
using workIT.Utilities;
using workIT.Factories;

namespace workIT.Services
{
	public class TaskServices
	{
		string thisClassName = "TaskServices";
		ActivityServices activityMgr = new ActivityServices();
		public List<string> messages = new List<string>();

		#region import
		public static ThisEntity GetByCtid( string ctid )
		{
			ThisEntity entity = new ThisEntity();
			if ( string.IsNullOrWhiteSpace( ctid ) )
				return entity;

			return EntityMgr.GetByCtid( ctid );
		}
		public bool Import( ThisEntity entity, ref SaveStatus status )
		{
			bool isValid = new EntityMgr().Save( entity, ref status );
			if ( entity.Id > 0 )
			{
				//update cache
				new CacheManager().PopulateEntityRelatedCaches( entity.RowId );

				//TODO - will need to update related elastic indices
				//	NOTE: not sure if there is an organization
				//new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, entity.OrganizationId, 1, ref messages );
			}

			return isValid;
		}

		#endregion


		#region Task Profile

		public static ThisEntity GetDetail( int profileId )
		{
			var profile = TaskManager.GetForDetail( profileId );

			return profile;
		}

		public static ThisEntity GetBasic( int profileId )
		{
			var profile = TaskManager.GetBasic( profileId );

			return profile;
		}

		public static List<ThisEntity> Search( string filter, int pageNumber, int pageSize, ref int pTotalRows )
		{
			var list = TaskManager.Search( filter, "", pageNumber, pageSize, ref pTotalRows );
			return list;
		}

		#endregion


	}
}

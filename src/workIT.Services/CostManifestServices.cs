using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.Search;

using ThisEntity = workIT.Models.Common.CostManifest;
using EntityMgr = workIT.Factories.CostManifestManager;
using workIT.Utilities;
using workIT.Factories;

namespace workIT.Services
{
	public class CostManifestServices
	{
		string thisClassName = "CostManifestServices";
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
                new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_ORGANIZATION, entity.OrganizationId, 1, ref messages );
            }

			return isValid;
		}

		#endregion


		#region CostManifest Profile

		public static CostManifest GetDetail( int profileId, AppUser user )
		{
			CostManifest profile = CostManifestManager.Get( profileId );
			
			return profile;
		}
		
		public static CostManifest GetBasic( int profileId )
		{
			CostManifest profile = CostManifestManager.GetBasic( profileId );

			return profile;
		}

		public static List<CostManifest> Search( int orgId, int pageNumber, int pageSize, ref int pTotalRows )
		{
			List<CostManifest> list = CostManifestManager.Search( orgId, pageNumber, pageSize, ref pTotalRows );
			return list;
		}

		#endregion

		#region Entity_CommonCost
		/// <summary>
		/// Add a Entity_CommonCost to a profile
		/// </summary>
		/// <param name="parentUid"></param>
		/// <param name="CostManifestId"></param>
		/// <param name="user"></param>
		/// <param name="valid"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public int Entity_CommonCost_Add( Guid parentUid, int CostManifestId, AppUser user, ref bool valid, ref string status, bool allowMultiples = true )
		{
			int id = 0;
			try
			{
				Entity parent = EntityManager.GetEntity( parentUid );
				if ( parent == null || parent.Id == 0 )
				{
					status = "Error - the parent entity was not found.";
					valid = false;
					return 0;
				}

				//id = new Entity_CommonCostManager().Add( parentUid, CostManifestId, user.Id, ref messages );

				if ( id > 0 )
				{
					//if valid, status contains the cred id, category, and codeId
					activityMgr.AddActivity( "Entity_CommonCost", "Add Entity_CommonCost", string.Format( "{0} added Entity_CommonCost {1} to {3} EntityId: {2}", user.FullName(), CostManifestId, parent.Id, parent.EntityType ), user.Id, 0, CostManifestId );
					status = "";

				}
				else
				{
					valid = false;
					status += string.Join( "<br/>", messages.ToArray() );
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".Entity_CommonCost_Add" );
				status = ex.Message;
				valid = false;
			}

			return id;
		}

		#endregion

	}
}

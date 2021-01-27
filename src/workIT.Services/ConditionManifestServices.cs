using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.Search;

using ThisEntity = workIT.Models.Common.ConditionManifest;

using EntityMgr = workIT.Factories.ConditionManifestManager;
using workIT.Utilities;
using workIT.Factories;

namespace workIT.Services
{
	public class ConditionManifestServices
	{
		string thisClassName = "ConditionManifestServices";
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


		#region ConditionManifest Profile

		public static ConditionManifest GetDetail( int profileId, AppUser user )
		{
			ConditionManifest profile = ConditionManifestManager.Get( profileId );
		
			return profile;
		}


		public static ConditionManifest GetBasic( int profileId )
		{
			ConditionManifest profile = ConditionManifestManager.GetBasic( profileId );

			return profile;
		}
		//public bool Save( ConditionManifest entity, Guid parentUid, string action, AppUser user, ref string status, bool isQuickCreate = false )
		//{
		//	bool isValid = true;
		//	List<String> messages = new List<string>();
		//	//parent is the org - not sure if int or guid yet
		//	if ( entity == null || !BaseFactory.IsGuidValid( parentUid ) )
		//	{
		//		messages.Add( "Error - missing an identifier for the ConditionManifest Profile" );
		//		return false;
		//	}

		//	try
		//	{
		//		Entity e = EntityManager.GetEntity( parentUid );
		//		//remove this if properly passed from client
		//		//plus need to migrate to the use of EntityId
		//		entity.ParentId = e.Id;
		//		entity.CreatedById = entity.LastUpdatedById = user.Id;

		//		if ( new ConditionManifestManager().Save( entity, parentUid, user.Id, ref messages ) )
		//		{

		//			//if valid, status contains the cred id, category, and codeId
		//			status = "Successfully Saved Condition Manifest";
		//			activityMgr.AddActivity( "Condition Manifest", action, string.Format( "{0} added/updated Condition Manifest profile: {1}", user.FullName(), entity.ProfileName ), user.Id, 0, entity.Id );

		//		}
		//		else
		//		{
		//			status += string.Join( "<br/>", messages.ToArray() );
		//			return false;
		//		}
		//	}
		//	catch ( Exception ex )
		//	{
		//		LoggingHelper.LogError( ex, thisClassName + ".ConditionManifest_Save" );
		//		status = ex.Message;
		//		isValid = false;
		//	}

		//	return isValid;
		//}


		public static List<ConditionManifest> Search( int orgId, int pageNumber, int pageSize, ref int pTotalRows )
		{
			List<ConditionManifest> list = ConditionManifestManager.Search( orgId, pageNumber, pageSize, ref pTotalRows );
			return list;
		}
		

		#endregion

		#region Entity_CommonCondition
		/// <summary>
		/// Add a Entity_CommonCondition to a profile
		/// </summary>
		/// <param name="parentUid"></param>
		/// <param name="conditionManifestId"></param>
		/// <param name="user"></param>
		/// <param name="valid"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public int Entity_CommonCondition_Add( Guid parentUid, int conditionManifestId, AppUser user, ref bool valid, ref string status, bool allowMultiples = true )
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

				//id = new Entity_CommonConditionManager().Add( parentUid, conditionManifestId, user.Id,  ref messages );

				if ( id > 0 )
				{
					//if valid, status contains the cred id, category, and codeId
					activityMgr.AddActivity( "Entity_CommonCondition", "Add Entity_CommonCondition", string.Format( "{0} added Entity_CommonCondition {1} to {3} EntityId: {2}", user.FullName(), conditionManifestId, parent.Id, parent.EntityType ), user.Id, 0, conditionManifestId );
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
				LoggingHelper.LogError( ex, thisClassName + ".Entity_CommonCondition_Add" );
				status = ex.Message;
				valid = false;
			}

			return id;
		}


		#endregion

	}
}

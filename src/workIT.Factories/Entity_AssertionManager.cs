using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.ProfileModels;
//using MN = workIT.Models.Node;

using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;
using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;

using workIT.Utilities;

using DBEntity = workIT.Data.Tables.Entity_Assertion;
using ThisEntity = workIT.Models.Common.OrganizationAssertion;

namespace workIT.Factories
{
	public class Entity_AssertionManager : BaseFactory
	{
		string thisClassName = "Entity_AssertionManager";

		public bool SaveList( int parentId, int roleId, List<Guid> targetUids, ref SaveStatus status )
		{

			if ( targetUids == null || targetUids.Count == 0 || roleId < 1 )
				return true;
            Entity parentEntity = EntityManager.GetEntity( parentId );
            Entity_AgentRelationshipManager emgr = new Entity_AgentRelationshipManager();

            bool isAllValid = true;
			foreach ( Guid targetUid in targetUids )
			{
                Entity targetEntity = EntityManager.GetEntity( targetUid );

                Save( parentId, targetUid, roleId, ref status );

                //check for add to AgentRelationship, if present
                //we don't know what the target is, so can't create a pending record!!!
                if (targetEntity.Id > 0)
                    emgr.Save( targetEntity.Id, parentEntity.EntityUid, roleId, ref status );
            }
			return isAllValid;
		} //

		public int Save( int entityId, Guid targetUid, int roleId
					, ref SaveStatus status )
		{
			int newId = 0;
			//assume if all empty, then ignore
			if ( entityId == 0 || !IsValidGuid( targetUid ) || roleId < 1 )
			{
				status.AddError( thisClassName + string.Format( ".Save() Error: invalid request, please provide a valid entityId: {0}, RoleId: {1}, and AgenUtid: {2}.", entityId, roleId, targetUid ) );
				return newId;
			}

			//TODO - update this method
            //a role could exist
			if ( AgentEntityRoleExists( entityId, targetUid, roleId ) )
			{
				//status.AddError( "Error: the selected relationship already exists!" );
				return 0;
			}

			EM.Entity_Assertion efEntity = new EM.Entity_Assertion();

			//TODO - need to get entity and type, otherwise set pending
			Entity targetEntity = EntityManager.GetEntity( targetUid );
			if ( targetEntity == null || targetEntity.Id == 0 )
			{
                //don't show in messages, no action possible
				//status.AddWarning( thisClassName + ".Save() Warning: the selected entity was not found yet. Need to check later. Setting to pending" );
				efEntity.IsPending = true;
				//may want to log an activity - would allow for queries, or the equivalent to the search reindex
				LoggingHelper.DoTrace( 5, thisClassName + string.Format( ".Entity_AgentRole_Add the agent was not found, for entityId: {0}, AgentId:{1}, RoleId: {2}", entityId, targetUid, roleId ) );
				return 0;
			} else
			{
				efEntity.TargetEntityTypeId = targetEntity.EntityTypeId;
				efEntity.IsPending = false;
			}

			using ( var context = new EntityContext() )
			{
				//add
				

				efEntity.EntityId = entityId;
				efEntity.TargetEntityUid = targetUid;
				efEntity.AssertionTypeId = roleId;
				efEntity.IsInverseRole = false; //should remove this, as never inverse

				efEntity.Created = System.DateTime.Now;

				context.Entity_Assertion.Add( efEntity );

				// submit the change to database
				int count = context.SaveChanges();
				newId = efEntity.Id;
			}

			return newId;
		}


		private static bool AgentEntityRoleExists( int entityId, Guid targetEntityUid, int roleId )
		{
			EntityAgentRelationship item = new EntityAgentRelationship();
			using ( var context = new EntityContext() )
			{
				EM.Entity_Assertion entity = context.Entity_Assertion.FirstOrDefault( s => s.EntityId == entityId
						&& s.TargetEntityUid == targetEntityUid
						&& s.AssertionTypeId == roleId );
				if ( entity != null && entity.Id > 0 )
				{
					return true;
				}
			}
			return false;
		}
	}
}

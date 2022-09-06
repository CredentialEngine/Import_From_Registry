using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;
using WMP= workIT.Models.ProfileModels;

using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;
using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;

using workIT.Utilities;

using DBEntity = workIT.Data.Tables.Entity_HasKSA;
//using DBEntitySummary = workIT.Data.Views.Entity_HasKSA_Summary;
using ThisEntity = workIT.Models.Common.KnowledgeSkillsAbilities;

namespace workIT.Factories
{
	public class Entity_HasKSAManager : BaseFactory
	{
		string thisClassName = "Entity_HasKSAManager";
		/*
		 * Occupation -> Entity -> Entity.HasKSA (a)
		 *								a.TargetEntityUid -> Entity -> concrete object
		 *	- use of entity.Cache?												
		 * 
		 */
		#region Persistance
		/// <summary>
		/// Save list of assertions
		/// TODO - need to handle change of owner. The old owner is not being deleted. 
		/// </summary>
		/// <param name="parentId"></param>
		/// <param name="roleId"></param>
		/// <param name="targetUids"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public bool SaveList( int parentId, int ksaTypeId, List<Guid> targetUids, ref SaveStatus status )
		{

			if ( targetUids == null || targetUids.Count == 0 || ksaTypeId < 1 )
				return true;
			Entity parentEntity = EntityManager.GetEntity( parentId );
			var searchPendingReindexManager = new SearchPendingReindexManager();
			var messages = new List<string>();
			bool isAllValid = true;

			foreach ( Guid targetUid in targetUids )
			{
				//we don't know what the target is, so can't create a pending record!!!
				Entity targetEntity = EntityManager.GetEntity( targetUid );
				if ( targetEntity == null || targetEntity.Id < 1 )
				{
					status.AddError( thisClassName + string.Format( ".SaveList() Error: invalid request, an existing entity was not found for the Identifier: {0}, for parentId: {1}.", targetUid, parentId ) );
					return false;
				}
				Save( parentId, ksaTypeId, targetEntity, ref status );
				
				//21-02-02 mp - need to add all targets to be reindexed.
				if ( targetEntity.Id > 0 )
				{
					//add items to be reindexed
					//- that is: thing is target of this
					searchPendingReindexManager.Add( targetEntity.EntityTypeId, targetEntity.EntityBaseId, 1, ref messages );
				};
			}
			return isAllValid;
		} //


		public int Save( int entityId, int ksaTypeId, Entity targetEntity, ref SaveStatus status )
		{
			int newId = 0;

			//TODO - update this method
			//==> not where a delete all first occurs?
			if ( EntityHasKSAExists( entityId, ksaTypeId, targetEntity.EntityUid, ref newId ) )
			{
				//status.AddError( "Error: the selected relationship already exists!" );
				return newId;
			}

			var efEntity = new EM.Entity_HasKSA();

			using ( var context = new EntityContext() )
			{
				//add
				efEntity.EntityId = entityId;
				efEntity.TargetEntityUid = targetEntity.EntityUid;
				efEntity.TargetEntityTypeId = targetEntity.EntityTypeId;
				efEntity.KSATypeId = ksaTypeId;
				efEntity.Created = System.DateTime.Now;

				context.Entity_HasKSA.Add( efEntity );

				// submit the change to database
				int count = context.SaveChanges();
				newId = efEntity.Id;
			}

			return newId;
		}


		private static bool EntityHasKSAExists( int entityId, int ksaTypeId, Guid targetEntityUid, ref int recordId )
		{
			using ( var context = new EntityContext() )
			{
				var entity = context.Entity_HasKSA.FirstOrDefault( s => s.EntityId == entityId
						&& s.TargetEntityUid == targetEntityUid
						&& s.KSATypeId == ksaTypeId );
				if ( entity != null && entity.Id > 0 )
				{
					recordId = entity.Id;
					return true;
				}
			}
			return false;
		}
		public bool DeleteAll( Entity parent, ref SaveStatus status )
		{
			bool isValid = true;
			//Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				status.AddError( thisClassName + ". Error - the provided target parent entity was not provided." );
				return false;
			}
			try
			{
				using ( var context = new EntityContext() )
				{
					var results = context.Entity_HasKSA.Where( s => s.EntityId == parent.Id )
					.ToList();
					if ( results == null || results.Count == 0 )
						return true;
					foreach ( var item in results )
					{
						context.Entity_HasKSA.Remove( item );
						int count = context.SaveChanges();
						if ( count > 0 )
						{
							isValid = true;
						}
						else
						{
							//if doing a delete on spec, may not have been any properties
						}
					}

				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".DeleteAll( Entity parent, ref SaveStatus status )" );
			}
			return isValid;
		}
		#endregion

		#region Retrieval 
		
		//
		public static RelatedKSA GetAll( int parentEntityId, int ksaTypeId, int maxRecords = 10 )
		{
			var output = new RelatedKSA();
			EnumeratedItem eitem = new EnumeratedItem();
			int records = maxRecords * 2;

			using ( var context = new EntityContext() )
			{
				var resources = context.Entity_HasKSA.Where( s => s.EntityId == parentEntityId
						&& s.KSATypeId == ksaTypeId )
						.OrderBy(s => s.KSATypeId)
						.ThenBy(s => s.TargetEntityTypeId)
						.ThenBy(s => s.TargetEntityUid).ToList();
				if ( resources != null && resources.Any() )
				{

					foreach ( var item in resources )
					{
						if (item.TargetEntityTypeId == CodesManager.ENTITY_TYPE_COMPETENCY )
						{
							var entity = CompetencyFrameworkCompetencyManager.Get( item.TargetEntityUid );
							output.Competencies.Add( new WMP.Competency()
							{
								CompetencyFrameworkId = entity.CompetencyFrameworkId,
								CompetencyText = entity.CompetencyText,
								CtdlId = entity.CtdlId,
								CTID = entity.CTID,
								CompetencyCategory = entity.CompetencyCategory,
								CompetencyLabel = entity.CompetencyLabel
							} );

						} else if ( item.TargetEntityTypeId == CodesManager.ENTITY_TYPE_JOB_PROFILE )
						{
							var entity = JobManager.Get( item.TargetEntityUid );
						}
						else if ( item.TargetEntityTypeId == CodesManager.ENTITY_TYPE_OCCUPATIONS_PROFILE )
						{
							var entity = OccupationManager.Get( item.TargetEntityUid );

						}
						else if ( item.TargetEntityTypeId == CodesManager.ENTITY_TYPE_TASK_PROFILE )
						{

						}
						else if ( item.TargetEntityTypeId == CodesManager.ENTITY_TYPE_WORKROLE_PROFILE )
						{

						}
					}
				}


			}
			return output;
		}

		#endregion
	}
}

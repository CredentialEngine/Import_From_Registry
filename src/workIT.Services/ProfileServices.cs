using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elasticsearch.Net;

using workIT.Factories;
using workIT.Models;
using workIT.Models.Common;

namespace workIT.Services
{
	public class ProfileServices
	{

		public static List<TopLevelObject> ResolveToTopLevelObject( List<Guid> input, string property, ref SaveStatus status )
		{
			var list = new List<TopLevelObject>();
			foreach(var item in input )
			{
				var tlo = GetEntityAsTopLevelObject( item );
				if ( tlo != null && tlo.Id > 0 )
					list.Add( tlo );
				else
				{
					status.AddError( string.Format( "ProfileServicesError.ResolveToTopLevelObject: For property: '{0}' unable to resolve GUID: '{1}' to a top level object.", property, item.ToString() ) );
				}
			}
			//may be common to want the output sorted by entity type? If so do before returning

			return list;
		}
		public static TopLevelObject GetEntityAsTopLevelObject(Guid uid)
		{
			TopLevelObject tlo = new TopLevelObject();

			var entity = EntityManager.GetEntity( uid, false );
			if ( entity == null || entity.Id == 0 )
				return null;
			//
			if (entity.EntityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL)
			{
				//actually should return some type info
				tlo = CredentialManager.GetBasic( entity.EntityBaseId );
				tlo.EntityTypeId = entity.EntityTypeId;
			}
			else if ( entity.EntityTypeId == CodesManager.ENTITY_TYPE_ORGANIZATION )
			{
				tlo = OrganizationManager.GetBasics( entity.EntityUid );
				tlo.EntityTypeId = entity.EntityTypeId;
			}
			else if ( entity.EntityTypeId == CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE )
			{
				tlo = AssessmentManager.GetBasic( entity.EntityBaseId );
				tlo.EntityTypeId = entity.EntityTypeId;
			}
			else if ( entity.EntityTypeId == CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE )
			{
				tlo = LearningOpportunityManager.GetBasic( entity.EntityBaseId );
				tlo.EntityTypeId = entity.EntityTypeId;
			}
			else if ( entity.EntityTypeId == CodesManager.ENTITY_TYPE_PATHWAY )
			{
				tlo = PathwayManager.GetBasic( entity.EntityBaseId );
				tlo.EntityTypeId = entity.EntityTypeId;
			}
			else if ( entity.EntityTypeId == CodesManager.ENTITY_TYPE_PATHWAY_COMPONENT )
			{
				tlo = PathwayComponentManager.Get( entity.EntityBaseId );
				tlo.EntityTypeId = entity.EntityTypeId;
			}
			else if ( entity.EntityTypeId == CodesManager.ENTITY_TYPE_PATHWAY_SET )
			{
				tlo = PathwaySetManager.Get( entity.EntityBaseId );
				tlo.EntityTypeId = entity.EntityTypeId;
			}
			else if ( entity.EntityTypeId == CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE )
			{
				tlo = TransferValueProfileManager.Get( entity.EntityBaseId );
				tlo.EntityTypeId = entity.EntityTypeId;
			}
			return tlo;
		}
	}
}

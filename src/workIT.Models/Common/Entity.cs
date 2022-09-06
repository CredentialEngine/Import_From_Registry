using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Common
{
    [Serializable]
    public class Entity : BaseObject
	{
		public Entity ()
		{
			//can't initialize here, as will cause infinite loop
			ParentEntity = null;	//			new Entity();
		}
		public System.Guid EntityUid { get; set; }
		public int EntityTypeId { get; set; }
		public string EntityType { get; set; }

		public int EntityBaseId { get; set; }
		public string EntityBaseName { get; set; }

		public Entity ParentEntity { get; set; }
		//only available when populated from Entity_Cache
		public string CTID { get; set; }
		public override string ToString()
		{
			if ( Id == 0 )
				return "";
			else
			{
				return string.Format( "EntityTypeId: {0}, EntityBaseName: {1}, EntityBaseId: {2}, CTID: {3}", EntityTypeId, ( EntityBaseName ?? "none" ), EntityBaseId, CTID ?? "" );
			}
		}
		//derived
		public int EntityStateId { get; set; }
	}

	public class TopLevelEntityReference
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string CTID { get; set; }
		public string Description { get; set; }
		public string SubjectWebpage { get; set; }
		public string DetailURL { get; set; }

		//public System.Guid RowId { get; set; }
		private int _entityTypeId { get; set; }
		public int EntityTypeId
		{
			get { return _entityTypeId; }
			set
			{
				_entityTypeId = value;
				switch ( _entityTypeId )
				{
					case 1:
						EntityType = "Credential";
						break;
					case 2:
						EntityType = "Organization";
						break;
					case 3:
						EntityType = "Assessment";
						break;
					case 7:
						EntityType = "LearningOpportunity";
						break;
					case 8:
						EntityType = "Pathway";
						break;
					case 9:
						EntityType = "Collection";
						break;
					case 10:
					case 17:
						EntityType = "CompetencyFramework";
						break;
					case 11:
						EntityType = "ConceptScheme";
						break;
					case 19:
						EntityType = "ConditionManifest";
						break;
					case 20:
						EntityType = "CostManifest";
						break;
					case 21:
						EntityType = "FinancialAssistance";
						break;
					case 23:
						EntityType = "PathwaySet";
						break;
					case 24:
						EntityType = "PathwayComponent";
						break;
					case 26:
						EntityType = "TransferValue";
						break;
					case 28:
						EntityType = "EarningsProfile";
						break;
					case 29:
						EntityType = "HoldersProfile";
						break;
					case 30:
						EntityType = "EmploymentOutcomeProfile";
						break;
					case 31:
						EntityType = "DataSetProfile";
						break;
					case 32:
						EntityType = "JobProfile";
						break;
					case 33:
						EntityType = "TaskProfile";
						break;
					case 34:
						EntityType = "WorkRoleProfile";
						break;
					case 35:
						EntityType = "OccupationProfile";
						break;
					default:
						EntityType = string.Format( "Unexpected EntityTypeId of {0}", _entityTypeId );
						break;
				}
			}
		}
		public string EntityType { get; set; }
		public int EntityStateId { get; set; } = 3;
		public string Image { get; set; }
	}

	public class EntityCache
	{
		/// <summary>
		/// From related Entity.Id
		/// </summary>
		public int Id { get; set; }
		public int EntityTypeId { get; set; }
		public string EntityType { get; set; }
		public int EntityStateId { get; set; }
		public System.Guid EntityUid { get; set; }
		public int BaseId { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public string CTID { get; set; }

		public int OwningOrgId { get; set; }
		//
		public int PublishedByOrganizationId { get; set; }
		/// <summary>
		/// Use OwningAgentUID to get OwningOrgId if not available
		/// </summary>
		public System.Guid OwningAgentUID { get; set; }
		public string OwningOrgCTID { get; set; }
		public string SubjectWebpage { get; set; }
		public string ImageUrl { get; set; }
		public int parentEntityId { get; set; }
		public System.Guid parentEntityUid { get; set; }
		public string parentEntityType { get; set; }
		public int parentEntityTypeId { get; set; }

		public System.DateTime Created { get; set; }

		public System.DateTime LastUpdated { get; set; }
		public System.DateTime CacheDate { get; set; }



	}
}

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
	}

	public class TopLevelEntityReference
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string CTID { get; set; }
		public string Description { get; set; }
		public string SubjectWebpage { get; set; }
		public bool IsValid { get; set; }

		public System.Guid RowId { get; set; }
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
						EntityType = "Rubric";
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
					default:
						EntityType = string.Format( "Unexpected EntityTypeId of {0}", _entityTypeId );
						break;
				}
			}
		}
		public string EntityType { get; set; }
	}
}

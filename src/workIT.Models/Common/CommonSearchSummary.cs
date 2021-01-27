using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Common
{
	public class CommonSearchSummary
	{
		public int ResultNumber { get; set; }
		public int EntityTypeId { get; set; }
		public string EntityType { get; set; }
		public int Id { get; set; }
		public Guid RowId { get; set; }
		public string CTID { get; set; }
		public int EntityStateId { get; set; }
		public string CredentialRegistryId { get; set; }

		public string Name { get; set; }
		//TBD
		public string NameAlphanumericOnly { get; set; }
		//
		public string FriendlyName { get; set; }
		public string Description { get; set; }
		public string SubjectWebpage { get; set; }

		public DateTime Created { get; set; }
		public DateTime LastUpdated { get; set; }
		//===================================
		public int PrimaryOrganizationId { get; set; }

		public string PrimaryOrganizationName { get; set; }

		public string PrimaryOrganizationCTID { get; set; }
		public List<int> AgentRelationships { get; set; } = new List<int>();
		//public List<AgentRelationshipForEntity> AgentRelationshipsForEntity { get; set; } = new List<AgentRelationshipForEntity>();
		//=======================================
		//
		//
		public bool HasOccupations { get; set; }
		public bool HasIndustries { get; set; }
		public CodeItemResult IndustryResults { get; set; } = new CodeItemResult();
		public CodeItemResult OccupationResults { get; set; } = new CodeItemResult();
		public CodeItemResult InstructionalProgramClassification { get; set; } = new CodeItemResult();

		//public List<IndexReferenceFramework> Industries { get; set; } = new List<IndexReferenceFramework>();
		//public List<IndexReferenceFramework> Occupations { get; set; } = new List<IndexReferenceFramework>();
		//public List<IndexReferenceFramework> InstructionalPrograms { get; set; } = new List<IndexReferenceFramework>();
		public List<string> Industry { get; set; } = new List<string>();
		public List<string> Occupation { get; set; } = new List<string>();
		public List<string> InstructionalProgram { get; set; } = new List<string>();

		public List<string> Subjects { get; set; } = new List<string>();
		//
		public List<int> ReportFilters { get; set; } = new List<int>();
		//will need more, for each of the types of list:
		//provider, QA, entity list
		//although could just handle lists for now
		public List<int> ResourceForWidget { get; set; } = new List<int>();
		//OR
		//public List<IndexWidgetTag> WidgetTags { get; set; } = new List<IndexWidgetTag>();
		
	}
}

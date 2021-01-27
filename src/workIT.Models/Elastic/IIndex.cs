using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Elastic
{
    public interface IIndex
    {
        int Id { get; set; }
		string Name { get; set; }
		string Description{ get; set; }
		string SubjectWebpage { get; set; }

		//string ListTitle { get; set; }
		int OwnerOrganizationId { get; set; }
		string PrimaryOrganizationName { get; set; }
		int EntityStateId { get; set; }

		bool IsAvailableOnline { get; }

        DateTime Created { get; set; }
		DateTime LastUpdated { get; set; }
		List<string> QualityAssurancePhrase { get; set; }
		List<string> Keyword { get; set; }
		List<AgentRelationshipForEntity> AgentRelationshipsForEntity { get; set; }
		//List<QualityAssurancePerformed> QualityAssurancePerformed { get; set; }
		List<IndexReferenceFramework> Occupations { get; set; }
		List<IndexReferenceFramework> Industries { get; set; }
		//20-10-29 renamed from Classifications
		List<IndexReferenceFramework> InstructionalPrograms { get; set; }
		List<string> Industry { get; set; } 

		List<string> Occupation { get; set; }
		List<string> InstructionalProgram { get; set; } 
		//
		List<Address> Addresses { get; set; }
		//List<string> Locations { get; set; }
		List<string> Cities { get; set; } 
		List<string> Regions { get; set; } 
		List<string> Countries { get; set; } 

		List<string> TextValues { get; set; }
		List<string> SubjectAreas { get; set; }
		List<string> PremiumValues { get; set; }
		List<int> ReportFilters { get; set; }
		List<int> ResourceForWidget { get; set; }
		List<IndexSubject> Subjects { get; set; }
	}
}

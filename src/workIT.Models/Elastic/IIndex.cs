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

		List<Address> Addresses { get; set; }
        DateTime Created { get; set; }
        List<string> Keyword { get; set; }
		List<AgentRelationshipForEntity> AgentRelationshipsForEntity { get; set; }
		//List<QualityAssurancePerformed> QualityAssurancePerformed { get; set; }
		List<IndexReferenceFramework> Occupations { get; set; }
		List<IndexReferenceFramework> Industries { get; set; }
		List<IndexReferenceFramework> Classifications { get; set; } 
		List<string> TextValues { get; set; }
		List<string> SubjectAreas { get; set; }
		List<string> PremiumValues { get; set; }
		List<int> ReportFilters { get; set; }
	}
}

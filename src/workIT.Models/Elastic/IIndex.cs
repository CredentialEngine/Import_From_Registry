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
        int OwnerOrganizationId { get; set; }
        int EntityStateId { get; set; }
        List<Address> Addresses { get; set; }
        DateTime Created { get; set; }
        List<string> Keyword { get; set; }
		List<IndexQualityAssurance> QualityAssurance { get; set; }
		List<IndexReferenceFramework> Occupations { get; set; }
		List<IndexReferenceFramework> Industries { get; set; }
		List<IndexReferenceFramework> Classifications { get; set; } 
		List<string> TextValues { get; set; } 
		List<string> PremiumValues { get; set; } 
	}
}

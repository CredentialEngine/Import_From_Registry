using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.Common;

using ME = workIT.Models.Elastic;
using WMS = workIT.Models.Search;

namespace workIT.Models.API
{

	public class RevocationProfile 
	{
		public RevocationProfile()
		{
			CTDLTypeLabel = "Revocation Profile";
		}
		public string CTDLTypeLabel { get; set; }
				
		public string DateEffective { get; set; }
		
		public string Description { get; set; }
		public List<ME.JurisdictionProfile> Jurisdiction { get; set; } 
		public string RevocationCriteriaUrl { get; set; }
		public string RevocationCriteriaDescription { get; set; }

		//public List<Credential> CredentialProfiled { get; set; } //holds values of RequiredCredential

		public List<ME.JurisdictionProfile> Region { get; set; } 
	}
}

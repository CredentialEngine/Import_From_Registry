using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.Common;

namespace workIT.Models.ProfileModels
{
	public class VerificationServiceProfile : BaseProfile
	{
		public VerificationServiceProfile()
		{
			EstimatedCost = new List<CostProfile>();
			ClaimType = new Enumeration();
			TargetCredential = new List<Credential>();
		}

		public string SubjectWebpage { get; set; }
		public string VerificationServiceUrl { get; set; }

		public bool? HolderMustAuthorize { get; set; }
		public List<CostProfile> EstimatedCost { get; set; }
		//public List<CostProfile> EstimatedCosts
		//{ get { return EstimatedCost; } set { EstimatedCost = value; }
		//} //Convenience for publishing


		//for import 
		public List<int> TargetCredentialIds { get; set; }
		//for display
		public List<Credential> TargetCredential { get; set; }

		//public Credential RelevantCredential { get; set; } //Workaround

		public Enumeration ClaimType { get; set; }

		public Guid OfferedByAgentUid { get; set; }
		/// <summary>
		/// Inflate OfferedByAgentUid for display 
		/// </summary>
		public Organization OfferedByAgent { get; set; }
		 
		public string VerificationDirectory { get; set; }


		public string VerificationMethodDescription { get; set; }

		public List<JurisdictionProfile> Region { get; set; }
		public List<JurisdictionProfile> JurisdictionAssertions { get; set; }



	}


}

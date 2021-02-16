using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MC = workIT.Models.Common;

using ME = workIT.Models.Elastic;

namespace workIT.Models.Detail
{
	[Serializable]
	public class VerificationServiceProfile 
	{
		public VerificationServiceProfile()
		{
		}

		public string DateEffective { get; set; }
		public string Description { get; set; }
		public List<ME.CostProfile> EstimatedCost { get; set; }
		public bool? HolderMustAuthorize { get; set; }

		public List<ME.JurisdictionProfile> Jurisdiction { get; set; } = new List<ME.JurisdictionProfile>();

		public MC.TopLevelEntityReference OfferedBy { get; set; } 
		public List<ME.JurisdictionProfile> OfferedIn { get; set; } = new List<ME.JurisdictionProfile>();
		public List<ME.JurisdictionProfile> Region { get; set; }
		public string SubjectWebpage { get; set; }

		//
		public List<MC.TopLevelEntityReference> TargetCredential { get; set; }
		// URL
		public string VerificationDirectory { get; set; }
		public string VerificationMethodDescription { get; set; }
		//URL
		public string VerificationService { get; set; }

		public List<LabelLink> VerifiedClaimType { get; set; } = new List<LabelLink>();

	}

}

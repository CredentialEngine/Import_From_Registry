using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MC = workIT.Models.Common;

using ME = workIT.Models.Elastic;
using WMS = workIT.Models.Search;

namespace workIT.Models.API
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

		public WMS.AJAXSettings OfferedBy { get; set; } 
		public List<ME.JurisdictionProfile> OfferedIn { get; set; } = new List<ME.JurisdictionProfile>();
		public List<ME.JurisdictionProfile> Region { get; set; }
		public string SubjectWebpage { get; set; }

		//
		public WMS.AJAXSettings TargetCredential { get; set; }
		// URL
		public string VerificationDirectory { get; set; }
		public string VerificationMethodDescription { get; set; }
		//URL
		public string VerificationService { get; set; }

		public List<LabelLink> VerifiedClaimType { get; set; } = new List<LabelLink>();

	}

}

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
	[Serializable]
	public class ProcessProfile 
	{
		public ProcessProfile()
		{
			//ProcessTypeId = 1;
		}
		//only relevent if combining all process profiles 
		//public int ProcessTypeId { get; set; }
		//public string ProcessProfileType { get; set; }

		public string Description { get; set; }
		//only include where different from owner-rare
		public WMS.AJAXSettings ProcessingAgent { get; set; }

		public List<LabelLink> DataCollectionMethodType { get; set; } = new List<LabelLink>();

		public List<LabelLink> ExternalInputType { get; set; } = new List<LabelLink>();

		public string DateEffective { get; set; }

		// Interval of process occurence.
		public string ProcessFrequency { get; set; }
		//URL
		public LabelLink ProcessMethod { get; set; }
		public string ProcessMethodDescription { get; set; }
		//URL
		public LabelLink ProcessStandards { get; set; }
		public string ProcessStandardsDescription { get; set; }
		public string ScoringMethodDescription { get; set; }
		//URL
		public LabelLink ScoringMethodExample { get; set; }
		public string ScoringMethodExampleDescription { get; set; }
		public string SubjectWebpage { get; set; }

		public string VerificationMethodDescription { get; set; }

		public List<ME.JurisdictionProfile> Jurisdiction { get; set; } = new List<ME.JurisdictionProfile>();
		/// <summary>
		/// A geo-political area of the described resource.
		/// </summary>
		public List<ME.JurisdictionProfile> Region { get; set; }= new List<ME.JurisdictionProfile>();

		public WMS.AJAXSettings TargetAssessment { get; set; }
		public WMS.AJAXSettings TargetCredential { get; set; }

		public WMS.AJAXSettings TargetLearningOpportunity { get; set; }
		public WMS.AJAXSettings TargetCompetencyFramework { get; set; }
		public int Meta_Id { get; set; }

		//public string ProcessType
		//{
		//	get
		//	{
		//		if ( ProcessTypeId == 2 )
		//			return "Appeal Process ";
		//		else if ( ProcessTypeId == 3 )
		//			return "Complaint Process ";
		//		else if ( ProcessTypeId == 4 )
		//			return "Criteria Process ";
		//		else if ( ProcessTypeId == 5 )
		//			return "Review Process ";
		//		else if ( ProcessTypeId == 6 )
		//			return "Revoke Process ";
		//		else
		//			return "Process Profile ";
		//	}
		//}
	}
	//
}

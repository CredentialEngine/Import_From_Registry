using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.Common;


namespace workIT.Models.Detail
{
	[Serializable]
	public class ProcessProfile : BaseDisplay
	{
		public ProcessProfile()
		{
			ProcessTypeId = 1;
		}
		//only relevent if combining all process profiles 
		public int ProcessTypeId { get; set; }
		public string ProcessProfileType { get; set; }

		//only include where different from owner-rare
		public TopLevelEntityReference ProcessingAgent { get; set; } = new TopLevelEntityReference();

		public List<LabelLink> ExternalInput { get; set; } = new List<LabelLink>();

		public string DateEffective { get; set; }


		public string ProcessFrequency { get; set; }
		//URL
		public string ProcessMethod { get; set; }
		public string ProcessMethodDescription { get; set; }
		//URL
		public string ProcessStandards { get; set; }
		public string ProcessStandardsDescription { get; set; }
		public string ScoringMethodDescription { get; set; }
		//URL
		public string ScoringMethodExample { get; set; }
		public string ScoringMethodExampleDescription { get; set; }
		public string VerificationMethodDescription { get; set; }
		//public string SubjectWebpage { get; set; }

		/// <summary>
		/// A geo-political area of the described resource.
		/// </summary>
		public List<JurisdictionProfile> Region { get; set; }= new List<JurisdictionProfile>();

		public List<TopLevelEntityReference> TargetAssessment { get; set; } = new List<TopLevelEntityReference>();
		public List<TopLevelEntityReference> TargetLearningOpportunity { get; set; } = new List<TopLevelEntityReference>();
		public List<TopLevelEntityReference> TargetCredential { get; set; } = new List<TopLevelEntityReference>();
		public List<TopLevelEntityReference> TargetCompetencyFramework { get; set; } = new List<TopLevelEntityReference>();

		public string ProcessType
		{
			get
			{
				if ( ProcessTypeId == 2 )
					return "Appeal Process ";
				else if ( ProcessTypeId == 3 )
					return "Complaint Process ";
				else if ( ProcessTypeId == 4 )
					return "Criteria Process ";
				else if ( ProcessTypeId == 5 )
					return "Review Process ";
				else if ( ProcessTypeId == 6 )
					return "Revoke Process ";
				else
					return "Process Profile ";
			}
		}
	}
	//
}

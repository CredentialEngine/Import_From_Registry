using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.Common;

namespace workIT.Models.ProfileModels
{

    [Serializable]
    public class ProcessProfile : BaseProfile
	{
		public ProcessProfile()
		{
			ProcessingAgent = new Organization();
			//ExternalInput = new Enumeration();
			ProcessTypeId = 1;

			//TargetAssessment = new List<AssessmentProfile>();
			//TargetCredential = new List<Credential>();
			//TargetLearningOpportunity = new List<LearningOpportunityProfile>();

			Region = new List<JurisdictionProfile>();
		}

		public int ProcessTypeId { get; set; }
		public string ProcessProfileType { get; set; }

		public Guid ProcessingAgentUid { get; set; }
		/// <summary>
		/// Inflate ProcessingAgentUid for display 
		/// </summary>
		public Organization ProcessingAgent { get; set; }

		public Enumeration DataCollectionMethodType { get; set; } = new Enumeration();
		/// <summary>
		/// Alias used for publishing
		/// </summary>
		public Enumeration ExternalInputType { get; set; } = new Enumeration();

		public string ProcessFrequency { get; set; }

		public string ProcessMethod { get; set; }
		public string ProcessMethodDescription { get; set; }
		public string ProcessStandards { get; set; }
		public string ProcessStandardsDescription { get; set; }
		public string ScoringMethodDescription { get; set; }
		public string ScoringMethodExample { get; set; }
		public string ScoringMethodExampleDescription { get; set; }
		public string VerificationMethodDescription { get; set; }
		public string SubjectWebpage { get; set; }

		/// <summary>
		/// A geo-political area of the described resource.
		/// </summary>
		public List<JurisdictionProfile> Region { get; set; }


		public List<AssessmentProfile> TargetAssessment { get; set; }
		public List<CompetencyFramework> TargetCompetencyFramework { get; set; } = new List<CompetencyFramework>();

		public List<Credential> TargetCredential { get; set; }

		public List<LearningOpportunityProfile> TargetLearningOpportunity { get; set; }


		#region Prperties for Import
		//public List<Guid> TargetCredentialUids { get; set; }
		//public List<Guid> TargetAssessmentUids { get; set; }
		//public List<Guid> TargetLearningOpportunityUids { get; set; }


		public List<int> TargetAssessmentIds { get; set; }
		public List<int> TargetCompetencyFrameworkIds { get; set; }

		public List<int> TargetCredentialIds { get; set; }
		public List<int> TargetLearningOpportunityIds { get; set; }

		#endregion

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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MC = workIT.Models.Common;
using WMA = workIT.Models.API;
using ME = workIT.Models.Elastic;
//using MPM =workIT.Models.ProfileModels;
using WMS = workIT.Models.Search;

namespace workIT.Models.API
{
	[JsonObject( ItemNullValueHandling = NullValueHandling.Ignore )]
	public class AssessmentDetail : BaseAPIType
	{
		public AssessmentDetail() 
		{
			EntityTypeId = 3;
			BroadType = "Assessment";
			CTDLType = "ceterms:AssessmentProfile";
			CTDLTypeLabel = "Assessment";
		}
		//helper where referenced by something else
		public string URL { get; set; }
		//public string RecordLanguage { get; set; } = "en-US";
		public bool IsReferenceVersion { get; set; }
		public List<LabelLink> OwnerRoles { get; set; }

		public List<LabelLink> AudienceLevelType { get; set; }
		public List<LabelLink> AudienceType { get; set; } = new List<LabelLink>();
		public List<LabelLink> Connections { get; set; } = new List<LabelLink>();
		public List<LabelLink> DeliveryType { get; set; } = new List<LabelLink>();
		public string DeliveryTypeDescription { get; set; }
		public List<string> AlternateName { get; set; }

		public List<LabelLink> AssessmentUseType { get; set; } = new List<LabelLink>();
		public List<LabelLink> AssessmentMethodType { get; set; } = new List<LabelLink>();
		
		public string AssessmentMethodDescription { get; set; }
		public string AssessmentOutput { get; set; }
		
		public LabelLink AssessmentExample { get; set; }
        //AssessmentExampleDescription is output with AssessmentExample
        //public string AssessmentExampleDescription { get; set; }
        public List<Address> AvailableAt { get; set; } = new List<Address>();
		public List<string> AvailableOnlineAt { get; set; } = new List<string>();

		public List<string> AvailabilityListing { get; set; }

		public List<WMA.CostManifest> CommonCosts { get; set; }

		public List<ValueProfile> CreditValue { get; set; }
		public string CreditUnitTypeDescription { get; set; }
		public List<ME.CostProfile> EstimatedCost { get; set; }

		public List<WMA.DurationProfile> EstimatedDuration { get; set; }
		public string DateEffective { get; set; }
		public string ExpirationDate { get; set; }
		public string ExternalResearch { get; set; }
		public List<FinancialAssistanceProfile> FinancialAssistance { get; set; } = new List<FinancialAssistanceProfile>();
		public string LearningMethodDescription { get; set; }
		//public LabelLink LifeCycleStatusType { get; set; }

		//
		public List<WMA.ConditionManifest> CommonConditions { get; set; }
		public List<WMA.ConditionProfile> Corequisite { get; set; }
		public List<WMA.ConditionProfile> CoPrerequisite { get; set; }

		public List<WMA.ConditionProfile> EntryCondition { get; set; }
		public List<WMA.ConditionProfile> Recommends { get; set; }
		public List<WMA.ConditionProfile> Requires { get; set; }
		//connections
		public List<WMA.ConditionProfile> AdvancedStandingFrom { get; set; }
		public List<WMA.ConditionProfile> IsAdvancedStandingFor { get; set; }
		public List<WMA.ConditionProfile> PreparationFrom { get; set; }
		public List<WMA.ConditionProfile> IsPreparationFor { get; set; }
		public List<WMA.ConditionProfile> IsRequiredFor { get; set; }
		public List<WMA.ConditionProfile> IsRecommendedFor { get; set; }
		//
		public List<Outline> Collections { get; set; }

		public List<LabelLink> ScheduleTimingType { get; set; }
		public List<LabelLink> ScheduleFrequencyType { get; set; } = new List<LabelLink>();
		public List<LabelLink> OfferFrequencyType { get; set; } = new List<LabelLink>();

		/// <summary>
		/// Is Non-Credit
		/// Resource carries or confers no official academic credit towards a program or a credential.
		/// </summary>
		public bool? IsNonCredit { get; set; }
		//
		public bool? HasGroupEvaluation { get; set; }
		public bool? HasGroupParticipation { get; set; }
		public bool? IsProctored { get; set; }
		public List<IdentifierValue> Identifier { get; set; } = new List<IdentifierValue>();
        //not sure of approach 
        public WMS.AJAXSettings HasSupportService { get; set; }

        public WMS.AJAXSettings HasTransferValue { get; set; }

		public List<Outline> QAReceived { get; set; } = new List<Outline>();

		public List<Outline> OwnerQAReceived { get; set; } = new List<Outline>();

		public List<ReferenceFramework> IndustryType { get; set; } = new List<ReferenceFramework>();
		public List<LabelLink> OccupationTypeOld { get; set; } = new List<LabelLink>(); 
		public List<ReferenceFramework> OccupationType { get; set; } = new List<ReferenceFramework>();

        public List<ReferenceFramework> InstructionalProgramType { get; set; } = new List<ReferenceFramework>();
		public List<LabelLink> Keyword { get; set; } = new List<LabelLink>();


		public LabelLink ProcessStandards { get; set; }
		//public string ProcessStandardsDescription { get; set; }
		public List<string> SameAs { get; set; }

		public string ScoringMethodDescription { get; set; }
		public LabelLink ScoringMethodExample { get; set; }
		//public string ScoringMethodExampleDescription { get; set; }
		public List<LabelLink> ScoringMethodType { get; set; } = new List<LabelLink>();



		public List<LabelLink> Subject { get; set; } = new List<LabelLink>();
        public string Supersedes { get; set; }
        public string SupersededBy { get; set; }
        public WMS.AJAXSettings TargetPathway { get; set; }
		public List<string> TargetLearningResource { get; set; }
		public List<IdentifierValue> VersionIdentifier { get; set; }
		public WMS.AJAXSettings AssessesCompetencies { get; set; }
		public WMS.AJAXSettings RequiresCompetencies { get; set; }

		public List<AggregateDataProfile> AggregateData { get; set; }

		public List<DataSetProfile> ExternalDataSetProfiles { get; set; }

		#region Jurisdiction
		//in base class
		//public List<ME.JurisdictionProfile> Jurisdiction { get; set; } = new List<ME.JurisdictionProfile>();
		//Propose use JurisdictionAssertion for all assertedIn data
		//JurisdictionAssertion

		//public List<ME.JurisdictionProfile> JurisdictionAssertion { get; set; } = new List<ME.JurisdictionProfile>();

		public List<ME.JurisdictionProfile> AccreditedIn { get; set; } = new List<ME.JurisdictionProfile>();
		public List<ME.JurisdictionProfile> ApprovedIn { get; set; } = new List<ME.JurisdictionProfile>();
		public List<ME.JurisdictionProfile> OfferedIn { get; set; } = new List<ME.JurisdictionProfile>();
		public List<ME.JurisdictionProfile> RecognizedIn { get; set; } = new List<ME.JurisdictionProfile>();
		public List<ME.JurisdictionProfile> RegulatedIn { get; set; } = new List<ME.JurisdictionProfile>();
		#endregion
		#region Process Profiles
		//TBD
		public List<WMS.AJAXSettings> ProcessProfiles { get; set; }

		public WMS.AJAXSettings AdministrationProcess { get; set; }
		public WMS.AJAXSettings DevelopmentProcess { get; set; }
		public WMS.AJAXSettings MaintenanceProcess { get; set; }

		#endregion


	}
}

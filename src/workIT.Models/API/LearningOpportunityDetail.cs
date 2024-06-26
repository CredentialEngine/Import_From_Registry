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
using WMS = workIT.Models.Search;

namespace workIT.Models.API
{
	[JsonObject( ItemNullValueHandling = NullValueHandling.Ignore )]
	public class LearningOpportunityDetail : BaseAPIType
	{
		public LearningOpportunityDetail()
		{
			EntityTypeId = 7;
			BroadType = "LearningOpportunity";
			CTDLType = "ceterms:LearningOpportunityProfile";
			CTDLTypeLabel = "Learning Opportunity";
		}
		public int LearningEntityTypeId { get; set; }
		public bool IsReferenceVersion { get; set; }
		//helper where referenced by something else
		public string URL { get; set; }

		//public List<LabelLink> OwnerRoles { get; set; }
		public List<AggregateDataProfile> AggregateData { get; set; }
		public List<string> AlternateName { get; set; }

		public List<LabelLink> AudienceLevelType { get; set; }
		public List<LabelLink> AudienceType { get; set; } = new List<LabelLink>();
		public List<LabelLink> AssessmentMethodType { get; set; } = new List<LabelLink>();
		public string AssessmentMethodDescription { get; set; }
		public List<LabelLink> AssessmentUseType { get; set; } = new List<LabelLink>();
		public List<LabelLink> DeliveryType { get; set; } = new List<LabelLink>();
		public string DeliveryTypeDescription { get; set; }

		public List<LabelLink> Connections { get; set; } = new List<LabelLink>();

		public List<Address> AvailableAt { get; set; } 
		public List<string> AvailableOnlineAt { get; set; } = new List<string>();

		public List<string> AvailabilityListing { get; set; }
		public string InCatalog { get; set; }
		public string CodedNotation { get; set; }
		public string SCED { get; set; }
		public List<ValueProfile> CreditValue { get; set; }
		public string CreditUnitTypeDescription { get; set; }
		public string DateEffective { get; set; }
		//
		public List<Outline> Collections { get; set; }
		//public List<string> Collections { get; set; } = new List<string>();
		//
		public List<LabelLink> DegreeConcentration { get; set; }
		public List<FinancialAssistanceProfile> FinancialAssistance { get; set; }
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
		public List<WMA.CostManifest> CommonCosts { get; set; }
		public List<ME.CostProfile> EstimatedCost { get; set; } 
		public List<WMA.DurationProfile> EstimatedDuration { get; set; }
		public string ExpirationDate { get; set; }

		public List<IdentifierValue> Identifier { get; set; } 


		public WMS.AJAXSettings HasPart { get; set; }
        //not sure of approach 
        public WMS.AJAXSettings HasSupportService { get; set; }

        public WMS.AJAXSettings HasTransferValue { get; set; }
		/// <summary>
		/// Is Non-Credit
		/// Resource carries or confers no official academic credit towards a program or a credential.
		/// </summary>
		public bool? IsNonCredit { get; set; }

		public WMS.AJAXSettings IsPartOf { get; set; }
		public List<LabelLink> LearningMethodType { get; set; } = new List<LabelLink>();
		public string LearningMethodDescription { get; set; }
		//public LabelLink LifeCycleStatusType { get; set; }

		public List<LabelLink> ScheduleTimingType { get; set; } = new List<LabelLink>();
		public List<LabelLink> ScheduleFrequencyType { get; set; } = new List<LabelLink>();
		public List<LabelLink> OfferFrequencyType { get; set; } = new List<LabelLink>();

		public WMS.AJAXSettings Prerequisite { get; set; }
		public List<Outline> QAReceived { get; set; } = new List<Outline>();
		public List<Outline> OwnerQAReceived { get; set; } = new List<Outline>();

		public List<ReferenceFramework> IndustryType { get; set; } = new List<ReferenceFramework>();
		//public List<LabelLink> OccupationTypeOld { get; set; } = new List<LabelLink>();
		public List<ReferenceFramework> OccupationType { get; set; } = new List<ReferenceFramework>();

		public List<ReferenceFramework> InstructionalProgramType { get; set; } = new List<ReferenceFramework>();


		public List<LabelLink> Keyword { get; set; } = new List<LabelLink>();
		public List<string> SameAs { get; set; }
		public List<LabelLink> Subject { get; set; } = new List<LabelLink>();
        public string Supersedes { get; set; }
        public string SupersededBy { get; set; }
        public WMS.AJAXSettings TargetAssessment { get; set; }
		public WMS.AJAXSettings TargetLearningOpportunity { get; set; }
		public WMS.AJAXSettings TargetPathway { get; set; }
		public List<string> TargetLearningResource { get; set; }
		public List<IdentifierValue> VersionIdentifier { get; set; }

		//
		public WMS.AJAXSettings AssessesCompetencies { get; set; }
		public WMS.AJAXSettings RequiresCompetencies { get; set; }
		public WMS.AJAXSettings TeachesCompetencies { get; set; }

		public List<DataSetProfile> ExternalDataSetProfiles { get; set; }

		#region Jurisdiction
		//in base class
		//public List<ME.JurisdictionProfile> Jurisdiction { get; set; } = new List<ME.JurisdictionProfile>();
		//Propose use JurisdictionAssertion for all assertedIn data
		//JurisdictionAssertion

		//public List<ME.JurisdictionProfile> JurisdictionAssertion { get; set; } = new List<ME.JurisdictionProfile>();

		public List<ME.JurisdictionProfile> AccreditedIn { get; set; } = new List<ME.JurisdictionProfile>();
		public List<ME.JurisdictionProfile> ApprovedIn { get; set; } = new List<ME.JurisdictionProfile>();

		public List<ME.JurisdictionProfile> RecognizedIn { get; set; } = new List<ME.JurisdictionProfile>();
		public List<ME.JurisdictionProfile> RegulatedIn { get; set; } = new List<ME.JurisdictionProfile>();
		#endregion


		public WMS.AJAXSettings ProvidesTransferValueFor { get; set; }
		public WMS.AJAXSettings ReceivesTransferValueFrom { get; set; }
		public WMS.AJAXSettings ObjectOfAction { get; set; }
		public WMS.AJAXSettings HasRubric { get; set; }
		public WMS.AJAXSettings RelatedActions { get; set; }
	}
}

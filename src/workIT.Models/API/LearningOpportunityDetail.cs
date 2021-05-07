using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MC = workIT.Models.Common;
using WMA = workIT.Models.API;
using ME = workIT.Models.Elastic;
using WMS = workIT.Models.Search;

namespace workIT.Models.API
{
	public class LearningOpportunityDetail : BaseDisplay
	{
		public LearningOpportunityDetail()
		{
			EntityTypeId = 7;
			BroadType = "LearningOpportunity";
			CTDLType = "ceterms:LearningOpportunityProfile";
			CTDLTypeLabel = "Learning Opportunity";
		}
		//public string CTDLType { get; set; }
		//public string RecordLanguage { get; set; } = "en-US";
		public bool IsReferenceVersion { get; set; }

		public List<LabelLink> OwnerRoles { get; set; }

		public List<LabelLink> AudienceLevelType { get; set; }
		public List<LabelLink> AudienceType { get; set; } = new List<LabelLink>();
		public List<LabelLink> AssessmentMethodType { get; set; } = new List<LabelLink>();
		public string AssessmentMethodDescription { get; set; }
		public List<LabelLink> AssessmentUseType { get; set; } = new List<LabelLink>();
		public List<LabelLink> DeliveryType { get; set; } = new List<LabelLink>();
		public string DeliveryTypeDescription { get; set; }

		public List<LabelLink> Connections { get; set; } = new List<LabelLink>();

		public List<Address> AvailableAt { get; set; } = new List<Address>();
		public List<string> AlternateName { get; set; } = new List<string>();
		public List<string> AvailableOnlineAt { get; set; } = new List<string>();

		public List<string> AvailabilityListing { get; set; }

		public string CredentialId { get; set; }
		public string CodedNotation { get; set; }
		public List<ValueProfile> CreditValue { get; set; }
		public string CreditUnitTypeDescription { get; set; }
		public string DateEffective { get; set; }
		//
		public List<FinancialAssistanceProfile> FinancialAssistance { get; set; } = new List<FinancialAssistanceProfile>();
		//
		public List<WMA.ConditionManifest> CommonConditions { get; set; }
		public List<WMA.ConditionProfile> Corequisite { get; set; } = new List<WMA.ConditionProfile>();
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
		public List<ME.CostProfile> EstimatedCost { get; set; } = new List<ME.CostProfile>();
		public List<WMA.DurationProfile> EstimatedDuration { get; set; }
		public string ExpirationDate { get; set; }

		public List<IdentifierValue> Identifier { get; set; } = new List<IdentifierValue>();


		public List<Outline> HasPart { get; set; }
		public List<Outline> IsPartOf { get; set; }
		public List<LabelLink> LearningMethodType { get; set; } = new List<LabelLink>();
		public string LearningMethodDescription { get; set; }

		public List<Outline> QAReceived { get; set; } = new List<Outline>();
		public List<Outline> OwnerQAReceived { get; set; } = new List<Outline>();

		public List<LabelLink> IndustryType { get; set; } = new List<LabelLink>();
		public List<LabelLink> OccupationType { get; set; } = new List<LabelLink>();
		public List<LabelLink> InstructionalProgramType { get; set; } = new List<LabelLink>();
		public List<LabelLink> Keyword { get; set; } = new List<LabelLink>();
		public List<string> SameAs { get; set; }
		public List<LabelLink> Subject { get; set; } = new List<LabelLink>();
		public List<IdentifierValue> VersionIdentifier { get; set; }

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


	}
}

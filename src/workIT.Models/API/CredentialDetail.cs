using System.Collections.Generic;

using Newtonsoft.Json;

using ME = workIT.Models.Elastic;
using MPM = workIT.Models.ProfileModels;
using WMA = workIT.Models.API;
using WMS = workIT.Models.Search;

namespace workIT.Models.API
{
	[JsonObject( ItemNullValueHandling = NullValueHandling.Ignore )]
	public class CredentialDetail : BaseAPIType
	{
		public CredentialDetail()
		{
			EntityTypeId = 1;
			BroadType = "Credential";
		}
		public bool IsReferenceVersion { get; set; }
		//helper where referenced by something else
		public string URL { get; set; }

		public List<LabelLink> OwnerRoles { get; set; }
		//public List<OrganizationRoleProfile> OwnedBy { get; set; } = new List<OrganizationRoleProfile>();
		//public List<OrganizationRoleProfile> OfferedBy { get; set; } = new List<OrganizationRoleProfile>();


		public List<LabelLink> AudienceLevelType { get; set; }
		public List<LabelLink> AudienceType { get; set; } 
		public List<LabelLink> AssessmentDeliveryType { get; set; } 
		public List<LabelLink> LearningDeliveryType { get; set; } 
		public List<LabelLink> Connections { get; set; } 
		public LabelLink CredentialStatusType { get; set; }
		public LabelLink CredentialType { get; set; }

		public List<Address> AvailableAt { get; set; } 
		public List<string> AlternateName { get; set; } 
		public List<string> AvailableOnlineAt { get; set; } 

		public List<string> AvailabilityListing { get; set; }
		public string InCatalog { get; set; }
		public string CredentialId { get; set; }
		public string CodedNotation { get; set; }
		public List<Outline> Collections { get; set; }

		public List<LabelLink> DegreeConcentration { get; set; }
		public List<LabelLink> DegreeMajor { get; set; }
		public List<LabelLink> DegreeMinor { get; set; }
		//
		public List<WMA.DurationProfile> EstimatedDuration { get; set; }
		public WMS.AJAXSettings CredentialEstimatedDuration { get; set; }
		public WMS.AJAXSettings AssessmentEstimatedDuration { get; set; }
		public WMS.AJAXSettings LearningOpportunityEstimatedDuration { get; set; }
		//always single, 
		public string RenewalFrequency { get; set; }
		//
		/// <summary>
		/// Is Non-Credit
		/// Resource carries or confers no official academic credit towards a program or a credential.
		/// </summary>
		public bool? IsNonCredit { get; set; }
		//
		//public List<WMA.Outline> HasPart2 { get; set; }
		public WMS.AJAXSettings HasPart { get; set; }
		public WMS.AJAXSettings IsPartOf { get; set; }
		//public List<WMA.Outline> IsPartOf2 { get; set; }

		//
		public List<WMA.ConditionManifest> CommonConditions { get; set; }
		public List<WMA.ConditionProfile> Corequisite { get; set; } 
		public List<WMA.ConditionProfile> CoPrerequisite { get; set; }

		public List<WMA.ConditionProfile> Recommends { get; set; }
		public List<WMA.ConditionProfile> Renewal { get; set; }
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
		public List<ME.CostProfile> AssessmentCost { get; set; }
		public List<ME.CostProfile> LearningOpportunityCost { get; set; }
		public string DateEffective { get; set; }
		public string ExpirationDate { get; set; }
		

		//
		public List<MPM.CostProfile> AssessmentEstimatedCosts { get; set; }
		public List<MPM.CostProfile> LearningOpportunityEstimatedCosts { get; set; }
		public List<FinancialAssistanceProfile> FinancialAssistance { get; set; } = new List<FinancialAssistanceProfile>();
		public List<IdentifierValue> Identifier { get; set; } = new List<IdentifierValue>();
		public List<Outline> EmbeddedCredentials { get; set; } //bundled/sub-credentials
		public List<Outline> ETPLCredentials { get; set; }
		public List<Outline> ETPLAssessments { get; set; } 
		public List<Outline> ETPLLearningOpportunities { get; set; }
		//
		public List<AggregateDataProfile> AggregateData { get; set; }
		public List<DataSetProfile> ExternalDataSetProfiles { get; set; } 

		public List<Outline> CopyrightHolder { get; set; }
		public string ISICV4 { get; set; }
		public string Image { get; set; } //image URL
		public string Meta_Icon { get; set; } //image URL
		public bool? Meta_HasVerificationBadge { get; set; }

		public List<Outline> QAReceived { get; set; } = new List<Outline>();
		public List<Outline> OwnerQAReceived { get; set; } = new List<Outline>();
		public WMS.AJAXSettings RenewedBy { get; set; }
		public WMS.AJAXSettings RevokedBy { get; set; }
        //not sure of approach 
        public WMS.AJAXSettings HasSupportService { get; set; }

        public WMS.AJAXSettings HasTransferValue { get; set; }
		public WMS.AJAXSettings Revocation { get; set; }

		public List<string> SameAs { get; set; }

		public List<ReferenceFramework> IndustryType { get; set; } = new List<ReferenceFramework>();
		public List<LabelLink> OccupationTypeOld { get; set; } = new List<LabelLink>();
		public List<ReferenceFramework> OccupationType { get; set; } = new List<ReferenceFramework>();

		public List<ReferenceFramework> InstructionalProgramType { get; set; } = new List<ReferenceFramework>();
		public List<LabelLink> NavyRating { get; set; }
		public List<LabelLink> Keyword { get; set; } = new List<LabelLink>();
		public List<LabelLink> Subject { get; set; } = new List<LabelLink>();
		public LabelLink LatestVersion { get; set; }
		public LabelLink NextVersion { get; set; } //URL

		public LabelLink PreviousVersion { get; set; }
		public List<IdentifierValue> VersionIdentifier { get; set; }

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
		public List<ME.JurisdictionProfile> RenewedIn { get; set; } = new List<ME.JurisdictionProfile>();
		public List<ME.JurisdictionProfile> RevokedIn { get; set; } = new List<ME.JurisdictionProfile>();
		#endregion
		#region Process Profiles
		//why are these AjaxSettings and condition profiles are just the latter?
		public List<WMS.AJAXSettings> ProcessProfiles { get; set; }

		public WMS.AJAXSettings AdministrationProcess { get; set; }
		public WMS.AJAXSettings AppealProcess { get; set; }
		public WMS.AJAXSettings ComplaintProcess { get; set; }
		public WMS.AJAXSettings DevelopmentProcess { get; set; }
		public WMS.AJAXSettings MaintenanceProcess { get; set; }
		public WMS.AJAXSettings ReviewProcess { get; set; }
		public WMS.AJAXSettings RevocationProcess { get; set; }
		#endregion

		public LabelLink ProcessStandards { get; set; }

		//public string ProcessStandardsDescription { get; set; }
		public LabelLink SupersededBy { get; set; } //URL
		public LabelLink Supersedes { get; set; } //URL

		public WMS.AJAXSettings TargetPathway { get; set; }

		public WMS.AJAXSettings RequiresCompetencies { get; set; }
		public WMS.AJAXSettings TeachesCompetencies { get; set; }
		public WMS.AJAXSettings AssessesCompetencies { get; set; }
		public WMS.AJAXSettings ProvidesTransferValueFor { get; set; }
		public WMS.AJAXSettings ReceivesTransferValueFrom { get; set; }
		public WMS.AJAXSettings HasRubric { get; set; }
		public WMS.AJAXSettings RelatedActions { get; set; }
	}
}

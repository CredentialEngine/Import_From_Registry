using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MC = workIT.Models.Common;
using WMA = workIT.Models.API;
using ME = workIT.Models.Elastic;
using MPM = workIT.Models.ProfileModels;
using WMS = workIT.Models.Search;
using MQD = workIT.Models.QData;

namespace workIT.Models.API
{
	public class CredentialDetail : BaseDisplay
	{
		public CredentialDetail()
		{
			EntityTypeId = 1;
			BroadType = "Credential";
		}
		//public string CTDLType { get; set; }
		//public string CTDLType { get; set; }
		//public string RecordLanguage { get; set; } = "en-US";
		public bool IsReferenceVersion { get; set; }
		public List<LabelLink> OwnerRoles { get; set; }
		//public List<OrganizationRoleProfile> OwnedBy { get; set; } = new List<OrganizationRoleProfile>();
		//public List<OrganizationRoleProfile> OfferedBy { get; set; } = new List<OrganizationRoleProfile>();


		public List<LabelLink> AudienceLevelType { get; set; }
		public List<LabelLink> AudienceType { get; set; } = new List<LabelLink>();
		public List<LabelLink> AssessmentDeliveryType { get; set; } = new List<LabelLink>();
		public List<LabelLink> LearningDeliveryType { get; set; } = new List<LabelLink>();
		public List<LabelLink> Connections { get; set; } = new List<LabelLink>();
		public LabelLink CredentialStatusType { get; set; }
		public LabelLink CredentialType { get; set; }

		public List<Address> AvailableAt { get; set; } = new List<Address>();
		public List<string> AlternateName { get; set; } = new List<string>();
		public List<string> AvailableOnlineAt { get; set; } = new List<string>();

		public List<string> AvailabilityListing { get; set; }

		public string CredentialId { get; set; }
		public string CodedNotation { get; set; }
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
		public List<WMA.Outline> HasPart2 { get; set; }
		public WMS.AJAXSettings HasPart { get; set; }
		public WMS.AJAXSettings IsPartOf { get; set; }
		public List<WMA.Outline> IsPartOf2 { get; set; }

		//
		public List<WMA.ConditionManifest> CommonConditions { get; set; }
		public List<WMA.ConditionProfile> Corequisite { get; set; } = new List<WMA.ConditionProfile>();
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


		public List<Outline> CopyrightHolder { get; set; }
		public List<LabelLink> CopyrightHolder2 { get; set; }
		public string ISICV4 { get; set; }
		public string Image { get; set; } //image URL
		public string Meta_Icon { get; set; } //image URL

		public List<Outline> QAReceived { get; set; } = new List<Outline>();
		public List<Outline> OwnerQAReceived { get; set; } = new List<Outline>();
		public List<Outline> RenewedBy2 { get; set; } = new List<Outline>();
		public WMS.AJAXSettings RenewedBy { get; set; }
		public WMS.AJAXSettings RevokedBy { get; set; }
		public List<Outline> RevokedBy2 { get; set; } = new List<Outline>();

		public WMS.AJAXSettings Revocation { get; set; }

		public List<string> SameAs { get; set; }

		public List<LabelLink> IndustryType { get; set; } = new List<LabelLink>();
		public List<LabelLink> OccupationType { get; set; } = new List<LabelLink>();
		public List<LabelLink> InstructionalProgramType { get; set; } = new List<LabelLink>();
		public List<LabelLink> NavyRating { get; set; }
		public List<LabelLink> Keyword { get; set; } = new List<LabelLink>();
		public List<LabelLink> Subject { get; set; } = new List<LabelLink>();
		public LabelLink LatestVersion { get; set; }

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
		//TBD
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
		public LabelLink NextVersion { get; set; } //URL
		public LabelLink SupersededBy { get; set; } //URL
		public LabelLink Supersedes { get; set; } //URL

		public WMS.AJAXSettings TargetPathway { get; set; }
	}
}

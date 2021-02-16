using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MC = workIT.Models.Common;
using MD = workIT.Models.Detail;
using ME = workIT.Models.Elastic;
using MPM = workIT.Models.ProfileModels;

namespace workIT.Models.Detail
{
	[Serializable]

	public class OrganizationDetail : BaseDisplay
	{
		//TBD
		public List<Address> Address { get; set; } = new List<Address>();

		/// <summary>
		/// Credentialing Organization or QA Credentialing Organization
		/// </summary>
		public string CTDLType { get; set; }
		public string RecordLanguage { get; set; } = "en-US";
		//URL
		public string AgentPurpose { get; set; }
		public string AgentPurposeDescription { get; set; }
		public List<LabelLink> AgentType { get; set; } = new List<LabelLink>();
		public List<LabelLink> AgentSectorType { get; set; } = new List<LabelLink>();
		public List<string> AlternateName { get; set; } = new List<string>();

		public List<string> AvailabilityListing { get; set; } = new List<string>();

		//TBD
		public List<ContactPoint> ContactPoint { get; set; } = new List<ContactPoint>();
		public List<string> Email { get; set; } = new List<string>();
		public string FoundingDate { get; set; }
		//
		public List<ME.ConditionManifest> HasConditionManifest { get; set; }
		public List<ME.CostManifest> HasCostManifest { get; set; }
		//TBD
		public List<MPM.Entity_IdentifierValue> Identifier { get; set; } = new List<MPM.Entity_IdentifierValue>();
		//URL
		public string Image { get; set; }
		//Note: the industry type link is set up to just to a keyword search. This should probably change
		public List<LabelLink> IndustryType { get; set; } = new List<LabelLink>();
		public List<LabelLink> Keyword { get; set; } = new List<LabelLink>();
		//URL
		public string MissionAndGoalsStatement { get; set; }
		public string MissionAndGoalsStatementDescription { get; set; }
		public List<string> SameAs { get; set; }
		//this should be part of the contactPoints
		public List<string> SocialMedia { get; set; }
		public List<LabelLink> ServiceType { get; set; } = new List<LabelLink>();
		//
		public List<MC.TopLevelEntityReference> ParentOrganization { get; set; }
		public List<MC.TopLevelEntityReference> Department { get; set; }
		public List<MC.TopLevelEntityReference> SubOrganization { get; set; }
		/// <summary>
		/// Webpage or online document that defines or explains the nature of transfer value handled by the organization.
		/// URI
		/// </summary>
		public string TransferValueStatement { get; set; }
		/// <summary>
		/// Description of the nature of transfer value handled by the organization.
		/// </summary>
		public string TransferValueStatementDescription { get; set; }

		//codes
		public string DUNS { get; set; }
		public string FEIN { get; set; }
		public string IPEDSID { get; set; }
		public string OPEID { get; set; }
		public string LEICode { get; set; }
		public string ISICV4 { get; set; }
		public string NECS { get; set; }

		//searches
		//these could be a list of search LabelLink
		public List<LabelLink> Connections{ get; set; } = new List<LabelLink>();

		//public LabelLink CredentialsSearch { get; set; } = new LabelLink();
		//public LabelLink AssessmentsSearch { get; set; } = new LabelLink();
		//public LabelLink LearningOpportunitySearch { get; set; } = new LabelLink();
		//public LabelLink PathwaySetsSearch { get; set; } = new LabelLink();
		//public LabelLink PathwaySearch { get; set; } = new LabelLink();
		//public LabelLink TransferValueProfilesSearch { get; set; } = new LabelLink();

		//QA
		public List<LabelLink> QAPerformed { get; set; } = new List<LabelLink>();

		public List<OrganizationRoleProfile> QAReceived { get; set; } = new List<OrganizationRoleProfile>();

		#region Jurisdiction
		//in base class
		//public List<ME.JurisdictionProfile> Jurisdiction { get; set; } = new List<ME.JurisdictionProfile>();
		//Propose use JurisdictionAssertion for all assertedIn data
		//JurisdictionAssertion
		public List<ME.JurisdictionProfile> JurisdictionAssertion { get; set; } = new List<ME.JurisdictionProfile>();

		public List<ME.JurisdictionProfile> AccreditedIn { get; set; } = new List<ME.JurisdictionProfile>();
		public List<ME.JurisdictionProfile> ApprovedIn { get; set; } = new List<ME.JurisdictionProfile>();

		public List<ME.JurisdictionProfile> RecognizedIn { get; set; } = new List<ME.JurisdictionProfile>();
		public List<ME.JurisdictionProfile> RegulatedIn { get; set; } = new List<ME.JurisdictionProfile>();
		#endregion
		#region Process Profiles
		//TBD
		//public List<ProcessProfileGroup> ProcessProfiles { get; set; } = new List<ProcessProfileGroup>();
		public List<MD.ProcessProfile> AdministrationProcess { get; set; } = new List<MD.ProcessProfile>();
		public List<MD.ProcessProfile> AppealProcess { get; set; } = new List<MD.ProcessProfile>();
		public List<MD.ProcessProfile> ComplaintProcess { get; set; } = new List<MD.ProcessProfile>();
		public List<MD.ProcessProfile> DevelopmentProcess { get; set; } = new List<MD.ProcessProfile>();
		public List<MD.ProcessProfile> MaintenanceProcess { get; set; } = new List<MD.ProcessProfile>();
		public List<MD.ProcessProfile> ReviewProcess { get; set; } = new List<MD.ProcessProfile>();
		public List<MD.ProcessProfile> RevocationProcess { get; set; } = new List<MD.ProcessProfile>();
		#endregion

		//TBD
		public List<MD.VerificationServiceProfile> VerificationServiceProfiles { get; set; }

	}
}

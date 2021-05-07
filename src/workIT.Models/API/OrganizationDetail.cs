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

namespace workIT.Models.API
{
	[Serializable]

	public class OrganizationDetail : BaseDisplay
	{
		public OrganizationDetail()
		{
			EntityTypeId = 2;
			BroadType = "Organization";
			CTDLType = "ceterms:CredentialOrganization";
			CTDLTypeLabel = "Organization";
		}
		//TBD
		public List<Address> Address { get; set; } = new List<Address>();

		/// <summary>
		/// Credentialing Organization or QA Credentialing Organization
		/// </summary>
		//public string CTDLType { get; set; }
		//public string RecordLanguage { get; set; } = "en-US";
		public bool IsReferenceVersion { get; set; }
		//URL
		public LabelLink AgentPurpose { get; set; }
		public string AgentPurposeDescription { get; set; }
		public List<LabelLink> AgentType { get; set; } = new List<LabelLink>();
		//will only ever be one value, using an array for consistency
		public List<LabelLink> AgentSectorType { get; set; } = new List<LabelLink>();
		public List<string> AlternateName { get; set; } = new List<string>();

		public List<string> AvailabilityListing { get; set; } = new List<string>();

		//TBD
		public List<ContactPoint> ContactPoint { get; set; } = new List<ContactPoint>();
		public List<string> Email { get; set; } = new List<string>();
		public string FoundingDate { get; set; }
		//
		public WMS.AJAXSettings HasConditionManifest { get; set; } 
		public List<WMA.ConditionManifest> HasConditionManifest2 { get; set; }
		public WMS.AJAXSettings HasCostManifest { get; set; }
		public List<WMA.CostManifest> HasCostManifest2 { get; set; }
		//TBD
		public List<IdentifierValue> Identifier { get; set; } = new List<IdentifierValue>();
		//URL
		public string Image { get; set; }
		//Note: the industry type link is set up to just to a keyword search. This should probably change
		public List<LabelLink> IndustryType { get; set; } = new List<LabelLink>();
		public List<LabelLink> Keyword { get; set; } = new List<LabelLink>();
		//URL
		public LabelLink MissionAndGoalsStatement { get; set; }
		//public string MissionAndGoalsStatementDescription { get; set; }
		public List<string> SameAs { get; set; }
		//this should be part of the contactPoints
		public List<string> SocialMedia { get; set; }
		public List<LabelLink> ServiceType { get; set; } = new List<LabelLink>();
		//should only be one, so should this be an Outline
		public Outline ParentOrganizationOutline { get; set; }
		public WMS.AJAXSettings ParentOrganization { get; set; }
		public WMS.AJAXSettings Department { get; set; }
		public WMS.AJAXSettings SubOrganization { get; set; }
		/// <summary>
		/// Webpage or online document that defines or explains the nature of transfer value handled by the organization.
		/// URI
		/// </summary>
		public LabelLink TransferValueStatement { get; set; }
		/// <summary>
		/// Description of the nature of transfer value handled by the organization.
		/// </summary>
		//public string TransferValueStatementDescription { get; set; }

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

		public List<Outline> QAReceived { get; set; } = new List<Outline>();

		#region Jurisdiction
		//in base class
		//public List<ME.JurisdictionProfile> Jurisdiction { get; set; } = new List<ME.JurisdictionProfile>();
		//Propose use JurisdictionAssertion for all assertedIn data
		//JurisdictionAssertion
		//public List<ME.JurisdictionProfile> JurisdictionAssertion { get; set; } 

		public List<ME.JurisdictionProfile> AccreditedIn { get; set; } = new List<ME.JurisdictionProfile>();
		public List<ME.JurisdictionProfile> ApprovedIn { get; set; } = new List<ME.JurisdictionProfile>();

		public List<ME.JurisdictionProfile> RecognizedIn { get; set; } = new List<ME.JurisdictionProfile>();
		public List<ME.JurisdictionProfile> RegulatedIn { get; set; } = new List<ME.JurisdictionProfile>();
		#endregion
		#region Process Profiles
		//TBD
		public List<WMS.AJAXSettings> ProcessProfiles { get; set; } 

		//public List<ProcessProfileGroup> ProcessProfiles { get; set; } = new List<ProcessProfileGroup>();
		public WMS.AJAXSettings AdministrationProcess { get; set; } 
		public WMS.AJAXSettings AppealProcess { get; set; } 
		public WMS.AJAXSettings ComplaintProcess { get; set; } 
		public WMS.AJAXSettings DevelopmentProcess { get; set; } 
		public WMS.AJAXSettings MaintenanceProcess { get; set; } 
		public WMS.AJAXSettings ReviewProcess { get; set; } 
		public WMS.AJAXSettings RevocationProcess { get; set; } 
		#endregion

		//TBD
		public WMS.AJAXSettings HasVerificationService { get; set; }
		public List<WMA.VerificationServiceProfile> HasVerificationServiceTemp { get; set; }

	}
}

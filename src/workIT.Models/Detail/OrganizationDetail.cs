using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MC = workIT.Models.Common;
using MPM = workIT.Models.ProfileModels;

namespace workIT.Models.Detail
{
	[Serializable]

	public class OrganizationDetail : BaseDisplay
	{

		public List<Address> Address { get; set; } = new List<Address>();

		/// <summary>
		/// Organization or QAOrganization
		/// </summary>
		public string AgentClassType { get; set; }

		public string AgentPurpose { get; set; }
		public string AgentPurposeDescription { get; set; }
		public List<LabelLink> AgentType { get; set; } = new List<LabelLink>();
		public List<LabelLink> AgentSectorType { get; set; } = new List<LabelLink>();
		public List<string> AlternateName { get; set; } = new List<string>();

		public List<string> AvailabilityListing { get; set; } = new List<string>();

		public List<ContactPoint> ContactPoint { get; set; } = new List<ContactPoint>();
		public List<string> Email { get; set; } = new List<string>();
		public string FoundingDate { get; set; }
		public List<MPM.Entity_IdentifierValue> Identifier { get; set; } = new List<MPM.Entity_IdentifierValue>();
		public string ImageUrl { get; set; }
		public List<LabelLink> IndustryType { get; set; } = new List<LabelLink>();
		public List<LabelLink> Keyword { get; set; } = new List<LabelLink>();
		public string MissionAndGoalsStatement { get; set; }
		public string MissionAndGoalsStatementDescription { get; set; }
		public List<LabelLink> ServiceType { get; set; } = new List<LabelLink>();
		/// <summary>
		/// Webpage or online document that defines or explains the nature of transfer value handled by the organization.
		/// URI
		/// </summary>
		public string TransferValueStatement { get; set; }
		/// <summary>
		/// Description of the nature of transfer value handled by the organization.
		/// </summary>
		public string TransferValueStatementDescription { get; set; }

		public List<MPM.VerificationStatus> VerificationStatus { get; set; }
		//codes
		public string ID_DUNS { get; set; }
		public string ID_FEIN { get; set; }
		public string ID_IPEDSID { get; set; }
		public string ID_OPEID { get; set; }
		public string ID_LEICode { get; set; }
		public string ID_ISICV4 { get; set; }
		public string ID_NECS { get; set; }

		//searches
		//these could be a list of search LabelLink
		public List<LabelLink> Connections{ get; set; } = new List<LabelLink>();

		public LabelLink CredentialsSearch { get; set; } = new LabelLink();
		public LabelLink AssessmentsSearch { get; set; } = new LabelLink();
		public LabelLink LearningOpportunitySearch { get; set; } = new LabelLink();
		public LabelLink PathwaySetsSearch { get; set; } = new LabelLink();
		public LabelLink PathwaySearch { get; set; } = new LabelLink();
		public LabelLink TransferValueProfilesSearch { get; set; } = new LabelLink();

		#region Process Profiles
		public List<MPM.ProcessProfile> AppealProcess { get; set; } = new List<MPM.ProcessProfile>();
		public List<MPM.ProcessProfile> ComplaintProcess { get; set; } = new List<MPM.ProcessProfile>();
		public List<MPM.ProcessProfile> ReviewProcess { get; set; } = new List<MPM.ProcessProfile>();
		public List<MPM.ProcessProfile> RevocationProcess { get; set; } = new List<MPM.ProcessProfile>();

		public List<MPM.ProcessProfile> AdministrationProcess { get; set; } = new List<MPM.ProcessProfile>();
		public List<MPM.ProcessProfile> DevelopmentProcess { get; set; } = new List<MPM.ProcessProfile>();
		public List<MPM.ProcessProfile> MaintenanceProcess { get; set; } = new List<MPM.ProcessProfile>();
		#endregion
	}
}

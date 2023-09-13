using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Common
{
	public class CredentialingAction
	{
		/* One of
		ceterms:AccreditAction
		ceterms:AdvancedStandingAction
		ceterms:ApproveAction
		ceterms:CredentialingAction
		ceterms:OfferAction
		ceterms:RecognizeAction
		ceterms:RegulateAction
		ceterms:RenewAction
		ceterms:RevokeAction
		ceterms:RightsAction
		21-09-07 perhaps also:
		ceterms:WorkforceDemandAction
	 */
		public string Type { get; set; } = "ceterms:CredentialingAction";

		/// <summary>
		/// Action Status
		/// Types of current status of an action.
		/// Available statuses include ActiveActionStatus, CompletedActionStatus, FailedActionStatus, PotentialActionStatus.
		/// <see cref="https://credreg.net/ctdl/terms/ActionStatus"/>
		/// </summary>
		public string ActionStatusType { get; set; }

		/// <summary>
		/// Acting Agent
		/// Provide the CTID for a participant in the Credential Registry or provide minimum data where not in the registry.
		/// </summary>
		public List<ResourceSummary> ActingAgent { get; set; }

		/// <summary>
		/// Accredit Action Description
		/// REQUIRED
		/// </summary>
		public string Description { get; set; }


		/// <summary>
		/// Evidence of Action
		/// Entity that proves that the action occured or that the action continues to be valid.
		/// URI
		/// </summary>
		public string EvidenceOfAction { get; set; }

		/// <summary>
		/// Instrument
		/// Object that helped the agent perform the action. e.g. John wrote a book with a pen.
		/// A credential or other instrument whose criteria was applied in executing the action.
		/// Provide the CTID for a credential in the Credential Registry or provide minimum data for a credential not in the registry.
		/// </summary>
		public List<ResourceSummary> Instrument { get; set; }

		/// <summary>
		/// Object
		/// Object upon which the action is carried out, whose state is kept intact or changed.
		/// An EntityReference for Credentials, AssessmentProfile, or LearningOpportunity Profile
		/// </summary>
		public ResourceSummary Object { get; set; }

		/// <summary>
		/// Participant
		/// Co-agents that participated in the action indirectly.
		/// Provide the CTID for a participant in the Credential Registry or provide minimum data where not in the registry.
		/// LIST????
		/// </summary>
		public List<ResourceSummary> Participant { get; set; }

		/// <summary>
		/// Resulting Award
		/// Awarded credential resulting from an action.
		/// Domain: RenewAction, AccreditAction
		/// Range: ceterms:CredentialAssertion - Representation of a credential awarded to a person.
		/// ???		https://www.imsglobal.org/sites/default/files/Badges/OBv2p0Final/index.html#Assertion
		/// SO A URI???
		/// </summary>
		public string ResultingAward { get; set; }

		/// <summary>
		/// Date this action starts.
		/// </summary>
		public string StartDate { get; set; }

		/// <summary>
		/// Date this action ends.
		/// </summary>
		public string EndDate { get; set; }

		public List<JurisdictionProfile> Jurisdiction { get; set; }
	}
}

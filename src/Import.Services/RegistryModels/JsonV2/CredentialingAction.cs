using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
	public class CredentialingAction : BaseResourceDocument
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
		[JsonProperty( "@type" )]
		public string Type { get; set; } = "ceterms:CredentialingAction";

		/// <summary>
		/// Action Status
		/// Types of current status of an action.
		/// Available statuses include ActiveActionStatus, CompletedActionStatus, FailedActionStatus, PotentialActionStatus.
		/// <see cref="https://credreg.net/ctdl/terms/ActionStatus"/>
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:actionStatusType" )]
		public CredentialAlignmentObject ActionStatusType { get; set; }

		/// <summary>
		/// Acting Agent
		/// Provide the CTID for a participant in the Credential Registry or provide minimum data where not in the registry.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:actingAgent" )]
		public List<string> ActingAgent { get; set; }

		/// <summary>
		/// Accredit Action Description
		/// REQUIRED
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:description" )]
		public LanguageMap Description { get; set; } = new LanguageMap();


		/// <summary>
		/// Evidence of Action
		/// Entity that proves that the action occured or that the action continues to be valid.
		/// URI
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:evidenceOfAction" )]
		public string EvidenceOfAction { get; set; }

		/// <summary>
		/// Instrument
		/// Object that helped the agent perform the action. e.g. John wrote a book with a pen.
		/// A credential or other instrument whose criteria was applied in executing the action.
		/// Provide the CTID for a credential in the Credential Registry or provide minimum data for a credential not in the registry.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:instrument" )]
		public List<string> Instrument { get; set; }

		/// <summary>
		/// Object
		/// Object upon which the action is carried out, whose state is kept intact or changed.
		/// An EntityReference for Credentials, AssessmentProfile, or LearningOpportunity Profile
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:object" )]
		public string Object { get; set; }

		/// <summary>
		/// Participant
		/// Co-agents that participated in the action indirectly.
		/// Provide the CTID for a participant in the Credential Registry or provide minimum data where not in the registry.
		/// LIST????
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:participant" )]
		public List<string> Participant { get; set; }

		/// <summary>
		/// Resulting Award
		/// Awarded credential resulting from an action.
		/// Domain: RenewAction, AccreditAction
		/// Range: ceterms:CredentialAssertion - Representation of a credential awarded to a person.
		/// ???		https://www.imsglobal.org/sites/default/files/Badges/OBv2p0Final/index.html#Assertion
		/// SO A URI???
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:resultingAward" )]
		public string ResultingAward { get; set; }

		/// <summary>
		/// Date this action starts.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:startDate" )]
		public string StartDate { get; set; }

		/// <summary>
		/// Date this action ends.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:endDate" )]
		public string EndDate { get; set; }

        [JsonProperty( PropertyName = "ceterms:jurisdiction" )]
        public List<JurisdictionProfile> Jurisdiction { get; set; }
    }
}

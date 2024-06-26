using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.Common;
using WMP = workIT.Models.ProfileModels;

namespace workIT.Models.Common
{
	/// <summary>
	/// Description of transfer value for a resource.
	/// </summary>
	public class TransferValueProfile : TopLevelObject
	{
		/*
		 * 
			ceterms:name
			ceterms:description
			ceterms:ctid
			ceterms:subjectWebpage
			ceterms:startDate
			ceterms:endDate
			ceterms:identifier
			ceterms:ownedBy
			ceasn:derivedFrom
			ceterms:transferValue
			ceterms:transferValueFrom
			ceterms:transferValueFor
			--------------
			ceterms:learningMethodDescription
			ceterms:assessmentMethodDescription
			ceterms:recognizes
			ceterms:owns
		 * 
		 */
		public TransferValueProfile()
		{
			EntityTypeId = 26;
		}
		#region Required 


		/// <summary>
		/// Organization(s) that owns this resource
		/// </summary>
		//public List<Organization> OwnedBy { get; set; } = new List<Organization>();

		public Enumeration OwnerRoles { get; set; } = new Enumeration();
		//??
		public List<WMP.OrganizationRoleProfile> OrganizationRole { get; set; } = new List<WMP.OrganizationRoleProfile>();
		#endregion

		/// <summary>
		/// Identifier
		/// Definition:	Alphanumeric token that identifies this resource and information about the token's originating context or scheme.
		/// </summary>	
		public List<WMP.Entity_IdentifierValue> Identifier { get; set; } = new List<WMP.Entity_IdentifierValue>();
		//or could store this as json
		public string IdentifierJson { get; set; }
		//OR
		//public string CodedNotation { get; set; }

		public List<TransferValueProfile> DerivedFrom { get; set; } = new List<TransferValueProfile>();

		public List<ResourceSummary> RelatedAssessment { get; set; } = new List<ResourceSummary>();
		public List<ResourceSummary> RelatedLearningOpp { get; set; } = new List<ResourceSummary>();
		public List<ResourceSummary> RelatedCredential { get; set; } = new List<ResourceSummary>();
		public List<ResourceSummary> RelatedJob { get; set; } = new List<ResourceSummary>();
		public List<ResourceSummary> RelatedOccupation { get; set; } = new List<ResourceSummary>();
		public List<ResourceSummary> RelatedCompetency { get; set; } = new List<ResourceSummary>();
		//
		//public List<WMP.ProcessProfile> AdministrationProcess { get; set; } = new List<WMP.ProcessProfile>();

		public List<WMP.ProcessProfile> DevelopmentProcess { get; set; } = new List<WMP.ProcessProfile>();

		public string SearchTagName { get; set; }

		/// <summary>
		/// Date the validity or usefulness of the information in this resource begins.
		/// </summary>
		public string StartDate { get; set; }

		/// <summary>
		/// Date this assertion ends.
		/// </summary>
		public string EndDate { get; set; }
		public string InCatalog { get; set; }

		/// <summary>
		/// Type of official status of the TransferProfile; select from an enumeration of such types.
		/// Provide the string value. API will format correctly. The name space of lifecycle doesn't have to be included
		/// lifecycle:Developing, lifecycle:Active", lifecycle:Suspended, lifecycle:Ceased
		/// </summary>
		public Enumeration LifeCycleStatusType { get; set; } = new Enumeration();
		public string LifeCycleStatus { get; set; }
		public int LifeCycleStatusTypeId { get; set; }

		/// <summary>
		/// A suggested or articulated credit- or point-related transfer value.
		/// OR COULD STORE AS JSON
		/// </summary>
		//public List<QuantitativeValue> TransferValue { get; set; } = new List<QuantitativeValue>();
		public List<ValueProfile> TransferValue { get; set; } = new List<ValueProfile>();
		public string TransferValueJson { get; set; }
		/// <summary>
		///  Resource that provides the transfer value described by this resource, according to the entity providing this resource.
		///  OR COULD STORE AS JSON
		/// <see cref="https://credreg.net/registry/assistant#EntityReference"/>
		/// </summary>
		public List<TopLevelObject> TransferValueFrom { get; set; } = new List<TopLevelObject>();
		public string TransferValueFromJson { get; set; }

		public List<WMP.AssessmentProfile> TransferValueFromAsmt { get; set; } = new List<WMP.AssessmentProfile>();
		public List<Credential> TransferValueFromCredential { get; set; } = new List<Credential>();
		public List<WMP.LearningOpportunityProfile> TransferValueFromLopp { get; set; } = new List<WMP.LearningOpportunityProfile>();
		public List<WMP.Competency> TransferValueFromCompetency { get; set; } = new List<WMP.Competency>();

		/// <summary>
		///  Resource that accepts the transfer value described by this resource, according to the entity providing this resource.
		///  OR COULD STORE AS JSON
		/// <see cref="https://credreg.net/registry/assistant#EntityReference"/>
		/// </summary>
		public List<TopLevelObject> TransferValueFor { get; set; } = new List<TopLevelObject>();
		public string TransferValueForJson { get; set; }
		public List<WMP.AssessmentProfile> TransferValueForAsmt { get; set; } = new List<WMP.AssessmentProfile>();
		public List<Credential> TransferValueForCredential { get; set; } = new List<Credential>();
		public List<WMP.LearningOpportunityProfile> TransferValueForLopp { get; set; } = new List<WMP.LearningOpportunityProfile>();
		public List<WMP.Competency> TransferValueForCompetency { get; set; } = new List<WMP.Competency>();

		public List<TopLevelObject> HasTransferIntermediary { get; set; } = new List<TopLevelObject>();
		///// <summary>
		///// The profile graph can be used to store properties like TransferValue, CodedNotation, the dates, and profile lists for TransferValueFrom and For.
		///// However, at some point we will want to count these so - nevermind
		///// </summary>
		////public string ProfileGraph { get; set; }
		#region version related
		public string LatestVersion { get; set; }

		public string PreviousVersion { get; set; }
		public string NextVersion { get; set; }

		public TopLevelObject LatestVersionResource { get; set; }
		public TopLevelObject PreviousVersionResource { get; set; }
		public TopLevelObject NextVersionResource { get; set; }

		public string SupersededBy { get; set; } //URL
		public string Supersedes { get; set; } //URL

		public List<IdentifierValue> VersionIdentifier { get; set; }
		public string VersionIdentifierJson { get; set; }
		#endregion
		#region Import
		public List<int> DerivedFromForImport { get; set; } 

		public List<Guid> OwnedBy { get; set; } = new List<Guid>();
		public List<Guid> TransferValueForImport { get; set; } = new List<Guid>();
		public List<Guid> TransferValueFromImport { get; set; } = new List<Guid>();

		/// <summary>
		/// 24-02-16 - changed to use one property - in progress. May not use this approach now
		/// </summary>
		public List<CodeItem> PendingReindexList { get; set; } = new List<CodeItem>();

		public List<int> AssessmentIds { get; set; } = new List<int>();
		public List<int> CredentialIds { get; set; } = new List<int>();
		public List<int> LearningOpportunityIds { get; set; } = new List<int>();
		#endregion
	}

	public class Entity_TransferValueProfile
	{
		public int Id { get; set; }
		public int EntityId { get; set; }
		public int TransferValueProfileId { get; set; }
		public System.DateTime Created { get; set; }

		public TransferValueProfile TransferValueProfile { get; set; } = new TransferValueProfile();

	}
}

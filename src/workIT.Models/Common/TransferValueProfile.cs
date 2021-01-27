using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.ProfileModels;
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
			ceterms:statusType
			ceterms:identifier
			ceterms:ownedBy
			ceterms:derivedFrom
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

		#region Required 


		/// <summary>
		/// Organization(s) that owns this resource
		/// </summary>
		//public List<Organization> OwnedBy { get; set; } = new List<Organization>();

		public Enumeration OwnerRoles { get; set; } = new Enumeration();
		//??
		public List<OrganizationRoleProfile> OrganizationRole { get; set; } = new List<OrganizationRoleProfile>();
		#endregion

		/// <summary>
		/// Identifier
		/// Definition:	Alphanumeric token that identifies this resource and information about the token's originating context or scheme.
		/// </summary>	
		public List<Entity_IdentifierValue> Identifier { get; set; } = new List<Entity_IdentifierValue>();
		//or could store this as json
		public string IdentifierJson { get; set; }
		//OR
		//public string CodedNotation { get; set; }

		public List<TransferValueProfile> DerivedFrom { get; set; } = new List<TransferValueProfile>();

		public List<ProcessProfile> DevelopmentProcess { get; set; } = new List<ProcessProfile>();

		/// <summary>
		/// Date the validity or usefulness of the information in this resource begins.
		/// </summary>
		public string StartDate { get; set; }

		/// <summary>
		/// Date this assertion ends.
		/// </summary>
		public string EndDate { get; set; }

		/// <summary>
		/// Type of official status of the TransferProfile; select from an enumeration of such types.
		/// Provide the string value. API will format correctly. The name space of lifecycle doesn't have to be included
		/// lifecycle:Developing, lifecycle:Active", lifecycle:Suspended, lifecycle:Ceased
		/// </summary>
		public string LifecycleStatusType { get; set; }
		//public Enumeration LifecycleStatusType { get; set; } = new Enumeration();

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

		public List<AssessmentProfile> TransferValueFromAsmt { get; set; } = new List<AssessmentProfile>();
		public List<Credential> TransferValueFromCredential { get; set; } = new List<Credential>();
		public List<LearningOpportunityProfile> TransferValueFromLopp { get; set; } = new List<LearningOpportunityProfile>();

		/// <summary>
		///  Resource that accepts the transfer value described by this resource, according to the entity providing this resource.
		///  OR COULD STORE AS JSON
		/// <see cref="https://credreg.net/registry/assistant#EntityReference"/>
		/// </summary>
		public List<TopLevelObject> TransferValueFor { get; set; } = new List<TopLevelObject>();
		public string TransferValueForJson { get; set; }
		public List<AssessmentProfile> TransferValueForAsmt { get; set; } = new List<AssessmentProfile>();
		public List<Credential> TransferValueForCredential { get; set; } = new List<Credential>();
		public List<LearningOpportunityProfile> TransferValueForLopp { get; set; } = new List<LearningOpportunityProfile>();
		/// <summary>
		/// The profile graph can be used to store properties like TransferValue, CodedNotation, the dates, and profile lists for TransferValueFrom and For.
		/// However, at some point we will want to count these so - nevermind
		/// </summary>
		//public string ProfileGraph { get; set; }

		#region Import
		public List<Guid> DerivedFromForImport { get; set; } = new List<Guid>();

		public List<Guid> OwnedBy { get; set; } = new List<Guid>();
		public List<Guid> TransferValueForImport { get; set; } = new List<Guid>();
		public List<Guid> TransferValueFromImport { get; set; } = new List<Guid>();

		#endregion
	}
}

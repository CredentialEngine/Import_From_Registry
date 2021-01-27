using System.Collections.Generic;

using Newtonsoft.Json;
namespace RA.Models.JsonV2
{
	/// <summary>
	/// TBD - perhaps we need an IBlankNode Interface. Then have Org and entity base implement the latter, rather than jamming all the properties in one class!
	/// </summary>
	public class BlankNode
	{

		/// <summary>
		/// An identifier for use with blank nodes, to minimize duplicates
		/// </summary>
		[JsonProperty( "@id" )]
		public string BNodeId { get; set; }

		/// <summary>
		/// the type of the entity must be provided. examples
		/// ceterms:AssessmentProfile
		/// ceterms:LearningOpportunityProfile
		/// ceterms:ConditionManifest
		/// ceterms:CostManifest
		/// or the many credential subclasses!!
		/// </summary>
		[JsonProperty( "@type" )]
		public string Type { get; set; }

		/// <summary>
		/// Name of the entity (required)
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:name" )]
		public LanguageMap Name { get; set; } = new LanguageMap();

		//Purpose? upcoming addition?
		[JsonProperty( PropertyName = "rdfs:label" )]
		public LanguageMap Label { get; set; } = new LanguageMap();

		/// <summary>
		/// Description of the entity (optional)
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:description" )]
		public LanguageMap Description { get; set; } = new LanguageMap();

		/// <summary>
		/// Subject webpage of the entity
		/// </summary> (required)
		[JsonProperty( PropertyName = "ceterms:subjectWebpage" )]
		public string SubjectWebpage { get; set; }


		#region Properties for an Organization related blank node
		//
		[JsonProperty( PropertyName = "ceterms:address" )]
		public List<Place> Address { get; set; }
		//
		[JsonProperty( PropertyName = "ceterms:availabilityListing" )]
		public List<string> AvailabilityListing { get; set; }
		//
		[JsonProperty( PropertyName = "ceterms:email" )]
		public List<string> Email { get; set; }
		//
		[JsonProperty( PropertyName = "ceterms:socialMedia" )]
		public List<string> SocialMedia { get; set; } = null;


		#endregion

		#region Properties for an Entity related blank node

		//assessment only
		[JsonProperty( PropertyName = "ceterms:assesses" )]
		public List<CredentialAlignmentObject> Assesses { get; set; }

		//
		[JsonProperty( PropertyName = "ceterms:assessmentMethodType" )]
		public List<CredentialAlignmentObject> AssessmentMethodType { get; set; }

		[JsonProperty( PropertyName = "ceterms:assessmentMethodDescription" )]
		public LanguageMap AssessmentMethodDescription { get; set; }
		//
		[JsonProperty( PropertyName = "ceterms:availableAt" )]
		public List<Place> AvailableAt { get; set; }
		//
		[JsonProperty( PropertyName = "ceterms:codedNotation" )]
		public string CodedNotation { get; set; }
		//
		[JsonProperty( PropertyName = "ceterms:creditValue" )]
		public List<ValueProfile> CreditValue { get; set; } = null;

		//for learning opportunity only
		[JsonProperty( PropertyName = "ceterms:dateEffective" )]
		public string DateEffective { get; set; }

		[JsonProperty( PropertyName = "ceterms:deliveryType" )]
		public List<CredentialAlignmentObject> DeliveryType { get; set; }

		//for learning opportunity only
		[JsonProperty( PropertyName = "ceterms:expirationDate" )]
		public string ExpirationDate { get; set; }
		//
		[JsonProperty( PropertyName = "ceterms:estimatedDuration" )]
		public List<DurationProfile> EstimatedDuration { get; set; }
		//
		[JsonProperty( PropertyName = "ceterms:identifierValue" )]
		public List<IdentifierValue> Identifier { get; set; }

		[JsonProperty( PropertyName = "ceterms:industryType" )]
		public List<CredentialAlignmentObject> IndustryType { get; set; }
		//
		[JsonProperty( PropertyName = "ceterms:keyword" )]
		public LanguageMapList Keyword { get; set; }
		//
		[JsonProperty( PropertyName = "ceterms:learningMethodDescription" )]
		public LanguageMap LearningMethodDescription { get; set; }
		//for learning opportunity only
		[JsonProperty( PropertyName = "ceterms:learningMethodType" )]
		public List<CredentialAlignmentObject> LearningMethodType { get; set; }

		[JsonProperty( PropertyName = "ceterms:occupationType" )]
		public List<CredentialAlignmentObject> OccupationType { get; set; }
		//
		[JsonProperty( PropertyName = "ceterms:requires" )]
		public List<ConditionProfile> Requires { get; set; } = null;

		[JsonProperty( PropertyName = "ceterms:corequisite" )]
		public List<ConditionProfile> Corequisite { get; set; } = null;

		[JsonProperty( PropertyName = "ceterms:recommends" )]
		public List<ConditionProfile> Recommends { get; set; } = null;

		[JsonProperty( PropertyName = "ceterms:entryCondition" )]
		public List<ConditionProfile> EntryCondition { get; set; } = null;
		//
		[JsonProperty( PropertyName = "ceterms:offeredBy" )]
		public List<string> OfferedBy { get; set; } = null;
		//
		[JsonProperty( PropertyName = "ceterms:ownedBy" )]
		public List<string> OwnedBy { get; set; } = null;
		//
		[JsonProperty( PropertyName = "ceterms:sameAs" )]
		public List<string> SameAs { get; set; } = null;

		//
		[JsonProperty( PropertyName = "ceterms:subject" )]
		public List<CredentialAlignmentObject> Subject { get; set; }
		//for learning opportunity only
		[JsonProperty( PropertyName = "ceterms:teaches" )]
		public List<CredentialAlignmentObject> Teaches { get; set; }

		#endregion
	}
}

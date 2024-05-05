using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
	public class LearningOpportunityProfile : BaseResourceDocument
	{
		[JsonIgnore]
		public static string classType = "ceterms:LearningOpportunityProfile";
		public LearningOpportunityProfile()
		{
			Type = "ceterms:LearningOpportunityProfile";
		}

		/// <summary>
		/// Learning Opportunity Class type
		/// </summary>
		[JsonProperty( "@type" )]
		public string Type { get; set; }

		/// <summary>
		/// Resource Locator
		/// </summary>
		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }

		/// <summary>
		/// Name or title of the resource.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:name" )]
		public LanguageMap Name { get; set; }

		/// <summary>
		/// Globally unique Credential Transparency Identifier (CTID) by which the creator, owner or provider of a resource recognizes it in transactions with the external environment (e.g., in verifiable claims involving the resource).
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:ctid" )]
		public string CTID { get; set; }

		/// <summary>
		/// Statement, characterization or account of the entity.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:description" )]
		public LanguageMap Description { get; set; }

		/// <summary>
		/// The primary language or languages of the entity, even if it makes use of other languages; e.g., a course offered in English to teach Spanish would have an inLanguage of English, while a credential in Quebec could have an inLanguage of both French and English.
		/// List of language codes. ex: en, es
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:inLanguage" )]
		//public string InLanguage { get; set; }
		public List<string> InLanguage { get; set; }

		/// <summary>
		/// Keyword or key phrase describing relevant aspects of an entity.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:keyword" )]
		public LanguageMapList Keyword { get; set; }

		//
		/// <summary>
		/// Another source of information about the entity being described.
		/// List of URIs
		/// ceterms:sameAs
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:sameAs" )]
		public List<string> SameAs { get; set; }

		[JsonProperty( PropertyName = "ceterms:subject" )]
		public List<CredentialAlignmentObject> Subject { get; set; }

		/// <summary>
		/// Webpage that describes this entity.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:subjectWebpage" )]
		public string SubjectWebpage { get; set; } //URL

		/// <summary>
		/// The status type of this LearningOpportunityProfile. 
		/// The default is Active. 
		/// ConceptScheme: ceterms:LifeCycleStatus
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:lifeCycleStatusType" )]
		public CredentialAlignmentObject LifeCycleStatusType { get; set; }

		[JsonProperty( PropertyName = "ceterms:codedNotation" )]
		public string CodedNotation { get; set; }

		/// <summary>
		/// Start Date of this resource
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:dateEffective" )]
		public string DateEffective { get; set; }

		/// <summary>
		/// End date of the learning opportunity if applicable
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:expirationDate" )]
		public string ExpirationDate { get; set; }
		//

		[JsonProperty( PropertyName = "ceterms:aggregateData" )]
		public List<AggregateDataProfile> AggregateData { get; set; }

		/// <summary>
		/// List of Alternate Names for this learning opportunity
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:alternateName" )]
		public LanguageMapList AlternateName { get; set; }

		/// <summary>
		/// Physical location where the learning opportunity can be pursued.
		/// Place
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:availableAt" )]
		public List<Place> AvailableAt { get; set; }

		/// <summary>
		/// Listing of online and/or physical locations where a resource can be pursued.
		/// URL
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:availabilityListing" )]
		public List<string> AvailabilityListing { get; set; } //URL
		/// <summary>
		/// Online location where the learning opportunity can be pursued.
		/// URL
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:availableOnlineAt" )] //URL
		public List<string> AvailableOnlineAt { get; set; }

		[JsonProperty( PropertyName = "ceterms:audienceLevelType" )]
		public List<CredentialAlignmentObject> AudienceLevelType { get; set; }

		[JsonProperty( PropertyName = "ceterms:audienceType" )]
		public List<CredentialAlignmentObject> AudienceType { get; set; }

		[JsonProperty( PropertyName = "ceterms:learningMethodType" )]
		public List<CredentialAlignmentObject> LearningMethodType { get; set; }

		/// <summary>
		///  Competency evaluated through the learning opportunity.		  
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:assesses" )]
		public List<CredentialAlignmentObject> Assesses { get; set; }

		/// <summary>
		/// Assessment Method Description 
		/// Description of the assessment methods for a resource.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:assessmentMethodDescription" )]
		public LanguageMap AssessmentMethodDescription { get; set; }

		[JsonProperty( PropertyName = "ceterms:assessmentMethodType" )]
		public List<CredentialAlignmentObject> AssessmentMethodType { get; set; }

		/// <summary>
		/// Learning Method Description 
		///  Description of the learning methods for a resource.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:learningMethodDescription" )]
		public LanguageMap LearningMethodDescription { get; set; }

		/// <summary>
		/// Type of means by which a learning opportunity or assessment is delivered to credential seekers and by which they interact; select from an existing enumeration of such types.
		/// deliveryType:BlendedDelivery deliveryType:InPerson deliveryType:OnlineOnly
		/// <see href="https://credreg.net/ctdl/terms/Delivery"></see>
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:deliveryType" )]
		public List<CredentialAlignmentObject> DeliveryType { get; set; }

		[JsonProperty( PropertyName = "ceterms:deliveryTypeDescription" )]
		public LanguageMap DeliveryTypeDescription { get; set; }

		[JsonProperty( PropertyName = "ceterms:estimatedDuration" )]
		public List<DurationProfile> EstimatedDuration { get; set; }

		/// <summary>
		/// Estimated cost of a learning opportunity.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:estimatedCost" )]
		public List<CostProfile> EstimatedCost { get; set; }
		//
		//20-10-31 CreditValue is now of type ValueProfile
		[JsonProperty( PropertyName = "ceterms:creditValue" )]
		public List<ValueProfile> CreditValue { get; set; } = null;
		//

		[JsonProperty( PropertyName = "ceterms:creditUnitTypeDescription" )]
		public LanguageMap CreditUnitTypeDescription { get; set; }

		/// <summary>
		/// Valid only for a Learning Program
		/// Focused plan of study within a college or university degree such as a concentration in Aerospace Engineering within an Engineering degree.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:degreeConcentration" )]
		public List<CredentialAlignmentObject> DegreeConcentration { get; set; }

		/// <summary>
		/// OccupationType
		/// Type of occupation; select from an existing enumeration of such types.
		///  For U.S. credentials, best practice is to identify an occupation using a framework such as the O*Net. 
		///  Other credentials may use any framework of the class ceterms:OccupationClassification, such as the EU's ESCO, ISCO-08, and SOC 2010.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:occupationType" )]
		public List<CredentialAlignmentObject> OccupationType { get; set; }

		[JsonProperty( PropertyName = "ceterms:industryType" )]
		public List<CredentialAlignmentObject> IndustryType { get; set; }

		[JsonProperty( PropertyName = "ceterms:instructionalProgramType" )]
		public List<CredentialAlignmentObject> InstructionalProgramType { get; set; }
		//
		/// <summary>
		/// Is Non-Credit
		/// Will be null unless true
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:isNonCredit" )]
		public bool? IsNonCredit { get; set; }

		[JsonProperty( PropertyName = "ceterms:teaches" )]
		public List<CredentialAlignmentObject> Teaches { get; set; }


		[JsonProperty( PropertyName = "ceterms:hasPart" )]
		public List<string> HasPart { get; set; }

		[JsonProperty( PropertyName = "ceterms:hasProxy" )]
		public string HasProxy { get; set; } //URL

		/// <summary>
		/// Offering of a Learning Opportunity or Assessment with a schedule associated with a specified location or modality.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:hasOffering" )]
		public List<string> HasOffering { get; set; }

		/// <summary>
		/// Rubric related to this resource.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:hasRubric" )]
		public List<string> HasRubric { get; set; }

		/// <summary>
		/// Reference to a relevant support service available for this resource.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:hasSupportService" )]
		public List<string> HasSupportService { get; set; }

		/// <summary>
		/// Alphanumeric token that identifies this resource and information about the token's originating context or scheme.
		/// <see href="https://purl.org/ctdl/terms/identifier"></see>
		/// ceterms:identifier
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:identifier" )]
		public List<IdentifierValue> Identifier { get; set; }

		/// <summary>
		/// An inventory or listing of resources that includes this resource.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:inCatalog" )]
		public string InCatalog { get; set; }

		[JsonProperty( PropertyName = "ceterms:isPartOf" )]
		public List<string> IsPartOf { get; set; }

		[JsonProperty( PropertyName = "ceterms:ownedBy" )]
		public List<string> OwnedBy { get; set; }

		/// <summary>
		/// Agent that offers the resource.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:offeredBy" )]

		public List<string> OfferedBy { get; set; }
		/// <summary>
		/// Only allowed for a course on a course
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:prerequisite" )]
		public List<string> Prerequisite { get; set; }



		/// <summary>
		/// This resource provides transfer value for the referenced Transfer Value Profile.
		/// Refer to the referenced Transfer Value Profile for more information. Other resources may be included for the full value.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:providesTransferValueFor" )]
		public List<string> ProvidesTransferValueFor { get; set; }

		/// <summary>
		/// This resource receives transfer value from the referenced Transfer Value Profile.
		/// Refer to the referenced Transfer Value Profile for more information. Other resources may be included for the full value.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:receivesTransferValueFrom" )]
		public List<string> ReceivesTransferValueFrom { get; set; }

		/// <summary>
		/// Action carried out upon this resource.
		/// Refer to the referenced Action for more information. Other resources may be included for the full value.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:objectOfAction" )]
		public List<string> ObjectOfAction { get; set; }

		#region -- Quality Assurance BY --
		/// <summary>
		/// List of Organizations that accredit this resource
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:accreditedBy" )]
		public List<string> AccreditedBy { get; set; }

		/// <summary>
		/// List of Organizations that approve this resource
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:approvedBy" )]
		public List<string> ApprovedBy { get; set; }

		/// <summary>
		/// List of Organizations that recognize this resource
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:recognizedBy" )]
		public List<string> RecognizedBy { get; set; }

		/// <summary>
		/// List of Organizations that regulate this resource
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:regulatedBy" )]
		public List<string> RegulatedBy { get; set; }

		/// <summary>
		/// Agent with whom an apprenticeship is registered.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:registeredBy" )]
		public List<string> RegisteredBy { get; set; }
		#endregion

		#region Quality Assurance IN - Jurisdiction based Quality Assurance  (INs)
		/// <summary>
		/// List of Organizations that accredit this learning opportunity in a specific Jurisdiction. 
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:accreditedIn" )]
		public List<JurisdictionProfile> AccreditedIn { get; set; }

		/// <summary>
		/// List of Organizations that approve this learning opportunity in a specific Jurisdiction. 
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:approvedIn" )]
		public List<JurisdictionProfile> ApprovedIn { get; set; }

		/// <summary>
		/// List of Organizations that offer this learning opportunity in a specific Jurisdiction. 
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:offeredIn" )]
		public List<JurisdictionProfile> OfferedIn { get; set; }

		/// <summary>
		/// List of Organizations that recognize this learning opportunity in a specific Jurisdiction. 
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:recognizedIn" )]
		public List<JurisdictionProfile> RecognizedIn { get; set; }

		/// <summary>
		/// List of Organizations that regulate this learning opportunity in a specific Jurisdiction. 
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:regulatedIn" )]
		public List<JurisdictionProfile> RegulatedIn { get; set; }

		/// <summary>
		/// List of Organizations that revoke this learning opportunity in a specific Jurisdiction. 
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:revokedIn" )]
		public List<JurisdictionProfile> RevokedIn { get; set; }

		#endregion


		[JsonProperty( PropertyName = "ceterms:requires" )]
		public List<ConditionProfile> Requires { get; set; }

		[JsonProperty( PropertyName = "ceterms:corequisite" )]
		public List<ConditionProfile> Corequisite { get; set; }

		[JsonProperty( PropertyName = "ceterms:coPrerequisite" )]
		public List<ConditionProfile> CoPrerequisite { get; set; }

		[JsonProperty( PropertyName = "ceterms:recommends" )]
		public List<ConditionProfile> Recommends { get; set; }

		[JsonProperty( PropertyName = "ceterms:entryCondition" )]
		public List<ConditionProfile> EntryCondition { get; set; }

		/// <summary>
		/// List of CTIDs or full URLs for a ConditionManifest published by the owning organization
		/// Set constraints, prerequisites, entry conditions, or requirements that are shared across an organization, organizational subdivision, set of credentials, or category of entities and activities.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:commonConditions" )]
		public List<string> CommonConditions { get; set; }

		/// <summary>
		/// List of CTIDs (recommended) or full URLs for a CostManifest published by the owning organization.
		/// Set of costs maintained at an organizational or sub-organizational level, which apply to this learning opportunity.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:commonCosts" )]
		public List<string> CommonCosts { get; set; }


		[JsonProperty( PropertyName = "ceterms:jurisdiction" )]
		public List<JurisdictionProfile> Jurisdiction { get; set; }


		[JsonProperty( PropertyName = "ceterms:advancedStandingFrom" )]
		public List<ConditionProfile> AdvancedStandingFrom { get; set; }

		[JsonProperty( PropertyName = "ceterms:isAdvancedStandingFor" )]
		public List<ConditionProfile> IsAdvancedStandingFor { get; set; }

		[JsonProperty( PropertyName = "ceterms:preparationFrom" )]
		public List<ConditionProfile> PreparationFrom { get; set; }

		[JsonProperty( PropertyName = "ceterms:isPreparationFor" )]
		public List<ConditionProfile> IsPreparationFor { get; set; }

		[JsonProperty( PropertyName = "ceterms:isRecommendedFor" )]
		public List<ConditionProfile> IsRecommendedFor { get; set; }

		[JsonProperty( PropertyName = "ceterms:isRequiredFor" )]
		public List<ConditionProfile> IsRequiredFor { get; set; }

		/// <summary>
		/// Entity that describes financial assistance that is offered or available.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:financialAssistance" )]
		public List<FinancialAssistanceProfile> FinancialAssistance { get; set; }

		/// <summary>
		///  Resource that replaces this resource.
		///  full URL OR CTID (recommended)
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:supersededBy" )]
		public string SupersededBy { get; set; } //URL

		/// <summary>
		/// Resource that this resource replaces.
		/// full URL OR CTID (recommended)
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:supersedes" )]
		public string Supersedes { get; set; } //URL

		/// <summary>
		/// Assessment that provides direct, indirect, formative or summative evaluation or estimation of the nature, ability, or quality for an entity.
		/// </summary>//
		[JsonProperty( PropertyName = "ceterms:targetAssessment" )]
		public List<string> TargetAssessment { get; set; }

		/// <summary>
		/// Learning opportunity that is the focus of a condition, process or another learning opportunity.
		/// This is an inverse property and would not be published with this resource.
		/// BUT may be allowing direct use??
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:targetLearningOpportunity" )]
		public List<string> TargetLearningOpportunity { get; set; }

		/// <summary>
		/// Learning object or resource that is used as part of an learning activity.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:targetLearningResource" )]
		public List<string> TargetLearningResource { get; set; }

		///// <summary>
		///// Pathway in which this resource is a potential component.
		///// This is an inverse property and would not be published with this resource
		///// </summary>
		//[JsonProperty( PropertyName = "ceterms:targetPathway" )]
		//public List<string> TargetPathway { get; set; }

		[JsonProperty( PropertyName = "ceterms:versionIdentifier" )]
		public List<IdentifierValue> VersionIdentifier { get; set; }

		[JsonProperty( PropertyName = "ceterms:offerFrequencyType" )]
		public List<CredentialAlignmentObject> OfferFrequencyType { get; set; }

		[JsonProperty( PropertyName = "ceterms:scheduleFrequencyType" )]
		public List<CredentialAlignmentObject> ScheduleFrequencyType { get; set; }

		[JsonProperty( PropertyName = "ceterms:scheduleTimingType" )]
		public List<CredentialAlignmentObject> ScheduleTimingType { get; set; }

		//COURSE ONLY
		[JsonProperty( PropertyName = "ceterms:sced" )]
		public string SCED { get; set; }
	}
}

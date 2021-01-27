using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
    public class LearningOpportunityProfile : JsonLDDocument
    {
		[JsonIgnore]
		public static string classType = "ceterms:LearningOpportunityProfile";
		public LearningOpportunityProfile()
        {
			Type = "ceterms:LearningOpportunityProfile";
			InLanguage = new List<string>();
            //Keyword = new List<string>();

			AudienceLevelType = new List<CredentialAlignmentObject>();
            AudienceType = new List<CredentialAlignmentObject>();
            AvailabilityListing = new List<string>();
            Subject = new List<CredentialAlignmentObject>();
            AvailableOnlineAt = new List<string>();
			LearningMethodType = new List<CredentialAlignmentObject>();
			DeliveryType = new List<CredentialAlignmentObject>();
            EstimatedDuration = new List<DurationProfile>();
            EstimatedCost = new List<CostProfile>();
            //CreditUnitType = new CredentialAlignmentObject();
            
            Requires = new List<ConditionProfile>();
            Corequisite = new List<ConditionProfile>();
            Recommends = new List<ConditionProfile>();
            EntryCondition = new List<ConditionProfile>();
			Teaches = new List<CredentialAlignmentObject>();

			OwnedBy = null;
			AccreditedBy = null;
            ApprovedBy = null;
            OfferedBy = null;
            RegulatedBy = null;
            RecognizedBy = null;

			AccreditedIn = null;
			ApprovedIn = null;
			OfferedIn = null;
			RecognizedIn = null;
			RegulatedIn = null;
			RevokedIn = null;

			Jurisdiction = new List<JurisdictionProfile>();
			AvailableAt = new List<Place>();

			AdvancedStandingFrom = new List<ConditionProfile>();
			IsAdvancedStandingFor = new List<ConditionProfile>();
			IsPreparationFor = new List<ConditionProfile>();
			IsRecommendedFor = new List<ConditionProfile>();
			IsRequiredFor = new List<ConditionProfile>();
			PreparationFrom = new List<ConditionProfile>();

            CommonConditions = new List<string>();
            CommonCosts = new List<string>();
          //  FinancialAssistanceOLD = new List<FinancialAlignmentObject>();

			HasPart = new List<string>();
			IsPartOf = new List<string>();
			VersionIdentifier = new List<IdentifierValue>();
		}

		/// <summary>
		/// Need a custom mapping to @type based on input value
		/// ceterms:CredentialOrganization, oR
		/// ceterms:QACredentialOrganization
		/// </summary>
		[JsonProperty( "@type" )]
		public string Type { get; set; }


		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }

		[JsonProperty( PropertyName = "ceterms:name" )]
        public LanguageMap Name { get; set; }

		[JsonProperty( PropertyName = "ceterms:ctid" )]
		public string Ctid { get; set; }

		[JsonProperty( PropertyName = "ceterms:description" )]
        public LanguageMap Description { get; set; }

        [JsonProperty( PropertyName = "ceterms:inLanguage" )]
		//public string InLanguage { get; set; }
		public List<string> InLanguage { get; set; }

		[JsonProperty( PropertyName = "ceterms:keyword" )]
        public LanguageMapList Keyword { get; set; }

        [JsonProperty( PropertyName = "ceterms:subject" )]
        public List<CredentialAlignmentObject> Subject { get; set; }

        [JsonProperty( PropertyName = "ceterms:subjectWebpage" )]
        public string SubjectWebpage { get; set; } //URL

		/// <summary>
		/// The status type of this LearningOpportunityProfile. 
		/// The default is Active. 
		/// ConceptScheme: ceterms:StatusCategory
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:lifecycleStatusType" )]
		public CredentialAlignmentObject LifecycleStatusType { get; set; }
		
		[JsonProperty( PropertyName = "ceterms:codedNotation" )]
        public string CodedNotation { get; set; }

		[JsonProperty( PropertyName = "ceterms:dateEffective" )]
        public string DateEffective { get; set; }

		[JsonProperty( PropertyName = "ceterms:expirationDate" )]
		public string ExpirationDate { get; set; }
		//


        //[JsonProperty( PropertyName = "ceterms:verificationMethodDescription" )] 
        //public LanguageMap VerificationMethodDescription { get; set; }
        
        [JsonProperty( PropertyName = "ceterms:availabilityListing" )]
        public List<string> AvailabilityListing { get; set; } //URL

        [JsonProperty( PropertyName = "ceterms:availableOnlineAt" )] //URL
        public List<string> AvailableOnlineAt { get; set; }
		[JsonProperty( PropertyName = "ceterms:audienceLevelType" )]
        public List<CredentialAlignmentObject> AudienceLevelType { get; set; }

        [JsonProperty(PropertyName = "ceterms:audienceType")]
        public List<CredentialAlignmentObject> AudienceType { get; set; }

        [JsonProperty( PropertyName = "ceterms:learningMethodType" )]
		public List<CredentialAlignmentObject> LearningMethodType { get; set; }


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
		///  Description of the learning methods for a resource.		/// 
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:learningMethodDescription" )]
		public LanguageMap LearningMethodDescription { get; set; }

		[JsonProperty( PropertyName = "ceterms:deliveryType" )]
        public List<CredentialAlignmentObject> DeliveryType { get; set; }
      
        [JsonProperty( PropertyName = "ceterms:deliveryTypeDescription" )]
        public LanguageMap DeliveryTypeDescription { get; set; }

        [JsonProperty( PropertyName = "ceterms:estimatedDuration" )]
        public List<DurationProfile> EstimatedDuration { get; set; }

        [JsonProperty( PropertyName = "ceterms:estimatedCost" )]
        public List<CostProfile> EstimatedCost { get; set; }
		//
		//[JsonProperty( PropertyName = "ceterms:creditValue" )]
		//public List<QuantitativeValue> CreditValue { get; set; } = null;
		//20-10-31 CreditValue is now of type ValueProfile
		[JsonProperty( PropertyName = "ceterms:creditValue" )]
		public List<ValueProfile> CreditValue { get; set; } = null;
		//
		//[JsonProperty( PropertyName = "ceterms:creditHourType" )]
		//      public LanguageMap CreditHourType { get; set; }

		//[JsonProperty( PropertyName = "ceterms:creditUnitType" )]
		//public CredentialAlignmentObject CreditUnitType { get; set; }

		//[JsonProperty( PropertyName = "ceterms:creditHourValue" )]
		//public decimal CreditHourValue { get; set; }

		//[JsonProperty( PropertyName = "ceterms:creditUnitValue" )]
		//public decimal CreditUnitValue { get; set; }

		[JsonProperty( PropertyName = "ceterms:creditUnitTypeDescription" )]
        public LanguageMap CreditUnitTypeDescription { get; set; }

		//frameworks
		[JsonProperty( PropertyName = "ceterms:occupationType" )]
		public List<CredentialAlignmentObject> OccupationType { get; set; } = new List<CredentialAlignmentObject>();

		[JsonProperty( PropertyName = "ceterms:alternativeOccupationType" )]
		public LanguageMapList AlternativeOccupationType { get; set; } = new LanguageMapList();

		[JsonProperty( PropertyName = "ceterms:industryType" )]
		public List<CredentialAlignmentObject> IndustryType { get; set; } = new List<CredentialAlignmentObject>();

		//[JsonProperty( PropertyName = "ceterms:naics" )]
		//public List<string> Naics { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "ceterms:alternativeIndustryType" )]
		public LanguageMapList AlternativeIndustryType { get; set; } = new LanguageMapList();

		[JsonProperty( PropertyName = "ceterms:instructionalProgramType" )]
		public List<CredentialAlignmentObject> InstructionalProgramType { get; set; } = new List<CredentialAlignmentObject>();
		//
		//[JsonProperty( PropertyName = "ceterms:alternativeInstructionalProgramType" )]
		//public LanguageMapList AlternativeInstructionalProgramType { get; set; } = new LanguageMapList();
		//


		[JsonProperty( PropertyName = "ceterms:teaches" )]
		public List<CredentialAlignmentObject> Teaches { get; set; }

		[JsonProperty( PropertyName = "ceterms:hasPart" )]
		public List<string> HasPart { get; set; }

		/// <summary>
		/// Identifier
		/// Definition:	Alphanumeric Identifier value.
		/// List of URIs 
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:identifierValue" )]
		public List<IdentifierValue> Identifier { get; set; }

		[JsonProperty( PropertyName = "ceterms:isPartOf" )]
		public List<string> IsPartOf { get; set; }

		[JsonProperty( PropertyName = "ceterms:ownedBy" )]
		public List<string> OwnedBy { get; set; }

		[JsonProperty( PropertyName = "ceterms:accreditedBy" )]
        public List<string> AccreditedBy { get; set; }

        [JsonProperty( PropertyName = "ceterms:approvedBy" )]
        public List<string> ApprovedBy { get; set; }

        [JsonProperty( PropertyName = "ceterms:offeredBy" )]
        public List<string> OfferedBy { get; set; }

        [JsonProperty( PropertyName = "ceterms:recognizedBy" )]
        public List<string> RecognizedBy { get; set; }

        [JsonProperty( PropertyName = "ceterms:regulatedBy" )]
        public List<string> RegulatedBy { get; set; }

		#region Ins

		[JsonProperty( PropertyName = "ceterms:accreditedIn" )]
		public List<JurisdictionProfile> AccreditedIn { get; set; }

		[JsonProperty( PropertyName = "ceterms:approvedIn" )]
		public List<JurisdictionProfile> ApprovedIn { get; set; }

		[JsonProperty( PropertyName = "ceterms:offeredIn" )]
		public List<JurisdictionProfile> OfferedIn { get; set; }

		[JsonProperty( PropertyName = "ceterms:recognizedIn" )]
		public List<JurisdictionProfile> RecognizedIn { get; set; }

		[JsonProperty( PropertyName = "ceterms:regulatedIn" )]
		public List<JurisdictionProfile> RegulatedIn { get; set; }

		[JsonProperty( PropertyName = "ceterms:revokedIn" )]
		public List<JurisdictionProfile> RevokedIn { get; set; }

		#endregion


		[JsonProperty( PropertyName = "ceterms:requires" )]
        public List<ConditionProfile> Requires { get; set; }

        [JsonProperty( PropertyName = "ceterms:corequisite" )]
        public List<ConditionProfile> Corequisite { get; set; }

        [JsonProperty( PropertyName = "ceterms:recommends" )]
        public List<ConditionProfile> Recommends { get; set; }

        [JsonProperty( PropertyName = "ceterms:entryCondition" )]
        public List<ConditionProfile> EntryCondition { get; set; }

		[JsonProperty( PropertyName = "ceterms:commonConditions" )]
		public List<string> CommonConditions { get; set; }

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

		[JsonProperty( PropertyName = "ceterms:availableAt" )]
		public List<Place> AvailableAt { get; set; }

		//[JsonIgnore]
		//[JsonProperty( PropertyName = "ceterms:financialAssistanceOLD" )]
  //      public List<FinancialAlignmentObject> FinancialAssistanceOLD { get; set; }

		[JsonProperty( PropertyName = "ceterms:financialAssistance" )]
		public List<FinancialAssistanceProfile> FinancialAssistance { get; set; }

		//
		[JsonProperty( PropertyName = "ceterms:targetLearningResource" )]
		public List<string> TargetLearningResource { get; set; }

		[JsonProperty( PropertyName = "ceterms:versionIdentifier" )]
		public List<IdentifierValue> VersionIdentifier { get; set; }

	}
}

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
    public class AssessmentProfile : JsonLDDocument
    {
		[JsonIgnore]
		public static string classType = "ceterms:AssessmentProfile";
		public AssessmentProfile()
        {
			Type = "ceterms:AssessmentProfile";

			//Keyword = new List<string>();

            AudienceType = new List<CredentialAlignmentObject>();
            AvailabilityListing = new List<string>();
            AvailableOnlineAt = new List<string>();
			AvailableAt = new List<Place>();
			DeliveryType = new List<CredentialAlignmentObject>();
            AssessmentMethodType = new List<CredentialAlignmentObject>();
            EstimatedCost = new List<CostProfile>();
            EstimatedDuration = new List<DurationProfile>();
            ScoringMethodType = new List<CredentialAlignmentObject>();

			Requires = new List<ConditionProfile>();
            Corequisite = new List<ConditionProfile>();
            Recommends = new List<ConditionProfile>();
            EntryCondition = new List<ConditionProfile>();
			//CreditUnitType = new CredentialAlignmentObject();
			Assesses = new List<CredentialAlignmentObject>();
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
		
			InLanguage = new List<string>();
			AdministrationProcess = new List<ProcessProfile>();
			DevelopmentProcess = new List<ProcessProfile>();
			MaintenanceProcess = new List<ProcessProfile>();

			AdvancedStandingFrom = new List<ConditionProfile>();
			IsAdvancedStandingFor = new List<ConditionProfile>();
			IsPreparationFor = new List<ConditionProfile>();
			IsRecommendedFor = new List<ConditionProfile>();
			IsRequiredFor = new List<ConditionProfile>();
			PreparationFrom = new List<ConditionProfile>();
			Jurisdiction = new List<JurisdictionProfile>();
            ExternalResearch = new List<string>();
			
            CommonConditions = new List<string>();
            CommonCosts = new List<string>();
            //FinancialAssistanceOLD = new List<FinancialAlignmentObject>();

			VersionIdentifier = new List<IdentifierValue>();
		}

		/// <summary>
		///  type
		/// </summary>
		[JsonProperty( "@type" )]
		public string Type { get; set; }

		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }

		[JsonProperty( PropertyName = "ceterms:ctid" )]
		public string CTID { get; set; }

		/// <summary>
		/// Name
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:name" )]
        public LanguageMap Name { get; set; }

		/// <summary>
		/// Description
		/// </summary>
        [JsonProperty( PropertyName = "ceterms:description" )]
        public LanguageMap Description { get; set; }

        [JsonProperty( PropertyName = "ceterms:inLanguage" )]
		//public string InLanguage { get; set; }
		public List<string> InLanguage { get; set; }

		[JsonProperty( PropertyName = "ceterms:keyword" )]
        public LanguageMapList Keyword { get; set; }

        [JsonProperty( PropertyName = "ceterms:subject" )]
        public List<CredentialAlignmentObject> Subject { get; set; } 

		//*** pending schema changes

        [JsonProperty( PropertyName = "ceterms:subjectWebpage" )]
        public string SubjectWebpage { get; set; } //URL


		/// <summary>
		/// Assessment Method Description 
		/// Description of the assessment methods for a resource.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:assessmentMethodDescription" )]
		public LanguageMap AssessmentMethodDescription { get; set; }

		/// <summary>
		/// Learning Method Description 
		///  Description of the learning methods for a resource.		/// 
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:learningMethodDescription" )]
		public LanguageMap LearningMethodDescription { get; set; }


		[JsonProperty( PropertyName = "ceterms:assessmentMethodType" )]
        public List<CredentialAlignmentObject> AssessmentMethodType { get; set; }

		[JsonProperty( PropertyName = "ceterms:assesses" )]
		public List<CredentialAlignmentObject> Assesses { get; set; }



        [JsonProperty( PropertyName = "ceterms:assessmentExample" )] //URL
        public string AssessmentExample { get; set; } //URL

        [JsonProperty( PropertyName = "ceterms:assessmentExampleDescription" )]
        public LanguageMap AssessmentExampleDescription { get; set; }

        [JsonProperty( PropertyName = "ceterms:assessmentOutput" )]
        public LanguageMap AssessmentOutput { get; set; }

        [JsonProperty( PropertyName = "ceterms:assessmentUseType" )]
        public List<CredentialAlignmentObject> AssessmentUseType { get; set; }

		[JsonProperty( PropertyName = "ceterms:availableAt" )]
		public List<Place> AvailableAt { get; set; }

		[JsonProperty( PropertyName = "ceterms:availabilityListing" )]
        public List<string> AvailabilityListing { get; set; } //URL


        [JsonProperty( PropertyName = "ceterms:availableOnlineAt" )] //URL
        public List<string> AvailableOnlineAt { get; set; }

		[JsonProperty( PropertyName = "ceterms:audienceLevelType" )]
		public List<CredentialAlignmentObject> AudienceLevelType { get; set; }

		[JsonProperty(PropertyName = "ceterms:audienceType")]
        public List<CredentialAlignmentObject> AudienceType { get; set; }


		//D

		[JsonProperty( PropertyName = "ceterms:codedNotation" )]
		public string CodedNotation { get; set; }
		//

		[JsonProperty( PropertyName = "ceterms:commonConditions" )]
		public List<string> CommonConditions { get; set; }

		[JsonProperty( PropertyName = "ceterms:commonCosts" )]
		public List<string> CommonCosts { get; set; }
		//
		//[JsonProperty( PropertyName = "ceterms:creditValue" )]
		//public List<QuantitativeValue> CreditValue { get; set; } = null;
		//20-10-31 CreditValue is now of type ValueProfile
		[JsonProperty( PropertyName = "ceterms:creditValue" )]
		public List<ValueProfile> CreditValue { get; set; } = null;

		[JsonProperty( PropertyName = "ceterms:creditUnitTypeDescription" )]
		public LanguageMap CreditUnitTypeDescription { get; set; }

		[JsonProperty( PropertyName = "ceterms:dateEffective" )]
		public string DateEffective { get; set; }

		[JsonProperty( PropertyName = "ceterms:expirationDate" )]
		public string ExpirationDate { get; set; }


		[JsonProperty( PropertyName = "ceterms:deliveryType" )]
        public List<CredentialAlignmentObject> DeliveryType { get; set; }


		[JsonProperty( PropertyName = "ceterms:deliveryTypeDescription" )]
		public LanguageMap DeliveryTypeDescription { get; set; }


		[JsonProperty( PropertyName = "ceterms:estimatedDuration" )]
		public List<DurationProfile> EstimatedDuration { get; set; }

		[JsonProperty( PropertyName = "ceterms:estimatedCost" )]
		public List<CostProfile> EstimatedCost { get; set; }


		[JsonProperty( PropertyName = "ceterms:externalResearch" )]
		public List<string> ExternalResearch { get; set; }


		[JsonProperty( PropertyName = "ceterms:financialAssistance" )]
		public List<FinancialAssistanceProfile> FinancialAssistance { get; set; }

		//  H
		[JsonProperty( PropertyName = "ceterms:hasGroupEvaluation", DefaultValueHandling = DefaultValueHandling.Include )]
        public bool? HasGroupEvaluation { get; set; }

        [JsonProperty( PropertyName = "ceterms:hasGroupParticipation", DefaultValueHandling = DefaultValueHandling.Include )]
        public bool? HasGroupParticipation { get; set; }

		/// <summary>
		/// Identifier
		/// Definition:	Alphanumeric Identifier value.
		/// List of URIs 
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:identifierValue" )]
		public List<IdentifierValue> Identifier { get; set; }

		[JsonProperty( PropertyName = "ceterms:isProctored", DefaultValueHandling = DefaultValueHandling.Include )]
		public bool? IsProctored { get; set; }

		[JsonProperty( PropertyName = "ceterms:jurisdiction" )]
		public List<JurisdictionProfile> Jurisdiction { get; set; }


		#region Conditions 
		[JsonProperty( PropertyName = "ceterms:requires" )]
		public List<ConditionProfile> Requires { get; set; }

		[JsonProperty( PropertyName = "ceterms:corequisite" )]
		public List<ConditionProfile> Corequisite { get; set; }

		[JsonProperty( PropertyName = "ceterms:recommends" )]
		public List<ConditionProfile> Recommends { get; set; }

		[JsonProperty( PropertyName = "ceterms:entryCondition" )]
		public List<ConditionProfile> EntryCondition { get; set; }
		#endregion

		#region Connections 

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
		#endregion

		#region Process profiles
		[JsonProperty( PropertyName = "ceterms:administrationProcess" )]
		public List<ProcessProfile> AdministrationProcess { get; set; }

		[JsonProperty( PropertyName = "ceterms:developmentProcess" )]
		public List<ProcessProfile> DevelopmentProcess { get; set; }

		[JsonProperty( PropertyName = "ceterms:maintenanceProcess" )]
		public List<ProcessProfile> MaintenanceProcess { get; set; }
		#endregion
		//QA ======================================
		#region  QA BY
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

		#endregion

		#region QA INs

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

		#endregion


		//V
		[JsonProperty( PropertyName = "ceterms:processStandardsDescription" )]
        public LanguageMap ProcessStandardsDescription { get; set; }


		[JsonProperty( PropertyName = "ceterms:processStandards" )]
		public string ProcessStandards { get; set; } //URL
													 //

		//S
		[JsonProperty( PropertyName = "ceterms:scoringMethodDescription" )]
        public LanguageMap ScoringMethodDescription { get; set; }
        [JsonProperty( PropertyName = "ceterms:scoringMethodExample" )]
        public string ScoringMethodExample { get; set; } //URL

        [JsonProperty( PropertyName = "ceterms:scoringMethodExampleDescription" )]
        public LanguageMap ScoringMethodExampleDescription { get; set; }

        [JsonProperty( PropertyName = "ceterms:scoringMethodType" )]
        public List<CredentialAlignmentObject> ScoringMethodType { get; set; }

		/// <summary>
		/// Type of official status of the Assessment; select from an enumeration of such types.
		/// URI to a concept
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:lifecycleStatusType" )]
		public CredentialAlignmentObject LifecycleStatusType { get; set; }

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
		[JsonProperty( PropertyName = "ceterms:alternativeInstructionalProgramType" )]
		public LanguageMapList AlternativeInstructionalProgramType { get; set; } = new LanguageMapList();

		//
		[JsonProperty( PropertyName = "ceterms:targetLearningResource" )]
		public List<string> TargetLearningResource { get; set; }

		[JsonProperty( PropertyName = "ceterms:versionIdentifier" )]
		public List<IdentifierValue> VersionIdentifier { get; set; }
	}
}


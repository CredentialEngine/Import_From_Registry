using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RA.Models.Json
{
    public class LearningOpportunityProfile : JsonLDDocument
    {
		[JsonIgnore]
		public static string classType = "ceterms:LearningOpportunityProfile";
		public LearningOpportunityProfile()
        {
			Type = "ceterms:LearningOpportunityProfile";
			InLanguage = new List<string>();
            Keyword = new List<string>();
        
            AvailabilityListing = new List<string>();
            Subject = new List<CredentialAlignmentObject>();
            AvailableOnlineAt = new List<string>();
			LearningMethodType = new List<CredentialAlignmentObject>();
			DeliveryType = new List<CredentialAlignmentObject>();
            InstructionalProgramType = new List<CredentialAlignmentObject>();
            EstimatedDuration = new List<DurationProfile>();
            EstimatedCost = new List<CostProfile>();
            CreditUnitType = new CredentialAlignmentObject();
            
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
			AvailableAt = new List<Json.Place>();

			AdvancedStandingFrom = new List<ConditionProfile>();
			IsAdvancedStandingFor = new List<ConditionProfile>();
			IsPreparationFor = new List<ConditionProfile>();
			IsRecommendedFor = new List<ConditionProfile>();
			IsRequiredFor = new List<ConditionProfile>();
			PreparationFrom = new List<ConditionProfile>();

            CommonConditions = new List<string>();
            CommonCosts = new List<string>();
            FinancialAssistance = new List<Json.FinancialAlignmentObject>();

			HasPart = new List<EntityBase>();
			IsPartOf = new List<EntityBase>();
			VersionIdentifier = new List<IdentifierValue>();
		}

        [JsonProperty( PropertyName = "ceterms:name" )]
        public string Name { get; set; }

        [JsonProperty( PropertyName = "ceterms:description" )]
        public string Description { get; set; }

        [JsonProperty( PropertyName = "ceterms:inLanguage" )]
		//public string InLanguage { get; set; }
		public List<string> InLanguage { get; set; }

		[JsonProperty( PropertyName = "ceterms:keyword" )]
        public List<string> Keyword { get; set; }

        [JsonProperty( PropertyName = "ceterms:subject" )]
        public List<CredentialAlignmentObject> Subject { get; set; }

        [JsonProperty( PropertyName = "ceterms:subjectWebpage" )]
        public string SubjectWebpage { get; set; } //URL
		//public string SubjectWebpage { get; set; } //URL

		[JsonProperty( PropertyName = "ceterms:codedNotation" )]
        public string CodedNotation { get; set; }
		//public string CodedNotation { get; set; }

		[JsonProperty( PropertyName = "ceterms:dateEffective" )]
        public string DateEffective { get; set; }

        /// <summary>
        /// Need a custom mapping to @type based on input value
        /// ceterms:CredentialOrganization, oR
        /// ceterms:QACredentialOrganization
        /// </summary>
        [JsonProperty( "@type" )]
        public string Type { get; set; }

        [JsonProperty( PropertyName = "ceterms:ctid" )]
        public string Ctid { get; set; }

        [JsonProperty( "@id" )]
        public string CtdlId { get; set; }

        [JsonProperty( PropertyName = "ceterms:verificationMethodDescription" )] 
        public string VerificationMethodDescription { get; set; }
        
        [JsonProperty( PropertyName = "ceterms:availabilityListing" )]
        public List<string> AvailabilityListing { get; set; } //URL

        [JsonProperty( PropertyName = "ceterms:availableOnlineAt" )] //URL
        public List<string> AvailableOnlineAt { get; set; }


		[JsonProperty( PropertyName = "ceterms:learningMethodType" )]
		public List<CredentialAlignmentObject> LearningMethodType { get; set; }


		[JsonProperty( PropertyName = "ceterms:deliveryType" )]
        public List<CredentialAlignmentObject> DeliveryType { get; set; }
      
        [JsonProperty( PropertyName = "ceterms:deliveryTypeDescription" )]
        public string DeliveryTypeDescription { get; set; }

        [JsonProperty( PropertyName = "ceterms:audienceType" )]
        public List<CredentialAlignmentObject> AudienceType { get; set; }

        [JsonProperty( PropertyName = "ceterms:estimatedDuration" )]
        public List<DurationProfile> EstimatedDuration { get; set; }

        [JsonProperty( PropertyName = "ceterms:estimatedCost" )]
        public List<CostProfile> EstimatedCost { get; set; }

        [JsonProperty( PropertyName = "ceterms:creditHourType" )]
        public string CreditHourType { get; set; }

        [JsonProperty( PropertyName = "ceterms:creditUnitType" )]
        public CredentialAlignmentObject CreditUnitType { get; set; }

        [JsonProperty( PropertyName = "ceterms:creditHourValue" )]
        public decimal CreditHourValue { get; set; }

        [JsonProperty( PropertyName = "ceterms:creditUnitValue" )]
        public decimal CreditUnitValue { get; set; }

        [JsonProperty( PropertyName = "ceterms:creditUnitTypeDescription" )]
        public string CreditUnitTypeDescription { get; set; }

        [JsonProperty( PropertyName = "ceterms:instructionalProgramType" )]
        public List<CredentialAlignmentObject> InstructionalProgramType { get; set; }


		[JsonProperty( PropertyName = "ceterms:teaches" )]
		public List<CredentialAlignmentObject> Teaches { get; set; }

		[JsonProperty( PropertyName = "ceterms:hasPart" )]
		public List<EntityBase> HasPart { get; set; }

		[JsonProperty( PropertyName = "ceterms:isPartOf" )]
		public List<EntityBase> IsPartOf { get; set; }

		[JsonProperty( PropertyName = "ceterms:ownedBy" )]
		public List<OrganizationBase> OwnedBy { get; set; }

		[JsonProperty( PropertyName = "ceterms:accreditedBy" )]
        public List<OrganizationBase> AccreditedBy { get; set; }

        [JsonProperty( PropertyName = "ceterms:approvedBy" )]
        public List<OrganizationBase> ApprovedBy { get; set; }

        [JsonProperty( PropertyName = "ceterms:offeredBy" )]
        public List<OrganizationBase> OfferedBy { get; set; }

        [JsonProperty( PropertyName = "ceterms:recognizedBy" )]
        public List<OrganizationBase> RecognizedBy { get; set; }

        [JsonProperty( PropertyName = "ceterms:regulatedBy" )]
        public List<OrganizationBase> RegulatedBy { get; set; }

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

        [JsonProperty( PropertyName = "ceterms:financialAssistance" )]
        public List<FinancialAlignmentObject> FinancialAssistance { get; set; }

		[JsonProperty( PropertyName = "ceterms:versionIdentifier" )]
		public List<IdentifierValue> VersionIdentifier { get; set; }

	}
}

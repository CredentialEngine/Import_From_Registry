using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace RA.Models.JsonV2
{

    public class Credential : BaseResourceDocument
    {
		[JsonIgnore]
		public static string classType = "ceterms:Credential";
		public Credential()
        {
			Subject = new List<CredentialAlignmentObject>();
            SubjectWebpage = null;
			HasPart = null;
			IsPartOf = null;
			AudienceLevelType = new List<CredentialAlignmentObject>();
            AudienceType = new List<CredentialAlignmentObject>();
            AvailableOnlineAt = new List<string>();
            AvailabilityListing = new List<string>();
			//AlternateName = new List<string>();
			//Image = new List<string>();
			InLanguage = new List<string>();
			
			VersionIdentifier = new List<IdentifierValue>();
			DegreeConcentration = new List<CredentialAlignmentObject>();
            DegreeMajor = new List<CredentialAlignmentObject>();
            DegreeMinor = new List<CredentialAlignmentObject>();
            EstimatedCost = new List<CostProfile>();
            Requires = new List<ConditionProfile>();
            Recommends = new List<ConditionProfile>();

			OwnedBy = null;
			AccreditedBy = null;
			ApprovedBy = null;
			OfferedBy = null;
			RecognizedBy = null;
            RegulatedBy = null;
			RevokedBy = null;
			RenewedBy = null;

			AccreditedIn = null;
			ApprovedIn = null;
			OfferedIn = null;
			RecognizedIn = null;
			RegulatedIn = null;
			RevokedIn = null;
            RenewedIn = null;

			AdministrationProcess = new List<ProcessProfile>();
			DevelopmentProcess = new List<ProcessProfile>();
			MaintenanceProcess = new List<ProcessProfile>();
			AppealProcess = new List<ProcessProfile>();
			ComplaintProcess = new List<ProcessProfile>();
			ReviewProcess = new List<ProcessProfile>();
			RevocationProcess = new List<ProcessProfile>();
			
			//FinancialAssistanceOLD = new List<FinancialAlignmentObject>();
            CredentialStatusType = new CredentialAlignmentObject();
            AdvancedStandingFrom = new List<ConditionProfile>();
            IsAdvancedStandingFor = new List<ConditionProfile>();
            IsPreparationFor = new List<ConditionProfile>();
            IsRecommendedFor = new List<ConditionProfile>();
            IsRequiredFor = new List<ConditionProfile>();
            PreparationFrom = new List<ConditionProfile>();
			Jurisdiction = new List<JurisdictionProfile>();
			AvailableAt = new List<Place>();

            CommonConditions = new List<string>();
            CommonCosts = new List<string>();
            Revocation = new List<RevocationProfile>();
            Renewal = new List<ConditionProfile>();
        }

        /// <summary>
        /// Need a custom mapping to @type based on input value
        /// </summary>
        [JsonProperty( "@type" )]
        public string CredentialType { get; set; }

        [JsonProperty( "@id" )]
        public string CtdlId { get; set; }

		[JsonProperty( PropertyName = "ceterms:ctid" )]
		public string CTID { get; set; }

		[JsonProperty( PropertyName = "ceterms:name" )]
        public LanguageMap Name { get; set; } = new LanguageMap();

        [JsonProperty( PropertyName = "ceterms:description" )]
        public LanguageMap Description { get; set; } = new LanguageMap();

		[JsonProperty( PropertyName = "ceterms:aggregateData" )]
		public List<AggregateDataProfile> AggregateData { get; set; }


		[JsonProperty( PropertyName = "ceterms:alternateName" )]
		public LanguageMapList AlternateName { get; set; } 


		[JsonProperty( PropertyName = "ceterms:assessmentDeliveryType" )]
		public List<CredentialAlignmentObject> AssessmentDeliveryType { get; set; }


		[JsonProperty( PropertyName = "ceterms:audienceLevelType" )]
		public List<CredentialAlignmentObject> AudienceLevelType { get; set; }

		[JsonProperty( PropertyName = "ceterms:audienceType" )]
		public List<CredentialAlignmentObject> AudienceType { get; set; }


		[JsonProperty( PropertyName = "ceterms:availableAt" )]
		public List<Place> AvailableAt { get; set; }


		[JsonProperty( PropertyName = "ceterms:availabilityListing" )]
		public List<string> AvailabilityListing { get; set; } //URL

		[JsonProperty( PropertyName = "ceterms:availableOnlineAt" )]
		public List<string> AvailableOnlineAt { get; set; }

		/// <summary>
		/// The salary value or range associated with this credential.
		/// </summary>
		[JsonProperty( PropertyName = "schema:baseSalary" )]
		public MonetaryAmount BaseSalary { get; set; }


		//obsolete
		[JsonProperty( PropertyName = "ceterms:codedNotation" )]
		public string CodedNotation { get; set; }
		//public string CodedNotation { get; set; }

		[JsonProperty( PropertyName = "ceterms:copyrightHolder" )]
		public List<string> CopyrightHolder { get; set; } = null;

		[JsonProperty( PropertyName = "ceterms:credentialStatusType" )]
		public CredentialAlignmentObject CredentialStatusType { get; set; }

		[JsonProperty( PropertyName = "ceterms:dateEffective" )]
        public string DateEffective { get; set; }

		[JsonProperty( PropertyName = "ceterms:credentialId" )]
		public string CredentialId { get; set; }

		[JsonProperty( PropertyName = "ceterms:creditValue" )]
		public List<ValueProfile> CreditValue { get; set; } = null;

		[JsonProperty( PropertyName = "ceterms:degreeConcentration" )]
		public List<CredentialAlignmentObject> DegreeConcentration { get; set; }

		[JsonProperty( PropertyName = "ceterms:degreeMajor" )]
		public List<CredentialAlignmentObject> DegreeMajor { get; set; }

		[JsonProperty( PropertyName = "ceterms:degreeMinor" )]
		public List<CredentialAlignmentObject> DegreeMinor { get; set; }

		[JsonProperty( PropertyName = "ceterms:estimatedDuration" )]
		public List<DurationProfile> EstimatedDuration { get; set; }

		[JsonProperty( PropertyName = "ceterms:expirationDate" )]
		public string ExpirationDate { get; set; }

		[JsonProperty( PropertyName = "ceterms:financialAssistance" )]
		public List<FinancialAssistanceProfile> FinancialAssistance { get; set; }



		/// <summary>
		/// HasETPLResource
		/// Only valid for a QualityAssuranceCredential
		/// List of entities that are members of (essentially approved by the owner of the) the QACredential
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:hasETPLResource" )]
		public List<string> HasETPLResource { get; set; }

		[JsonProperty( PropertyName = "ceterms:hasPart" )]
		public List<string> HasPart { get; set; }

		/// <summary>
		/// HasRating
		/// Rating related to this resource.
		/// URI to a Rating
		/// </summary>
		[JsonProperty( PropertyName = "navy:hasRating" )]
		public List<string> HasRating { get; set; }


		/// <summary>
		/// Identifier
		/// Means of identifying a resource, typically consisting of an alphanumeric token and a context or scheme from which that token originates.
		/// List of IdentifierValue 
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:identifier" )]
		public List<IdentifierValue> Identifier { get; set; }


		//Image URL
		[JsonProperty( PropertyName = "ceterms:image" )]
		public string Image { get; set; }


		[JsonProperty( PropertyName = "ceterms:inLanguage" )]
		//public string InLanguage { get; set; }
		public List<string> InLanguage { get; set; }



		/// <summary>
		/// ISIC Revision 4 Code
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:isicV4" )]
		public string ISICV4 { get; set; }


		/// <summary>
		/// Is Non-Credit
		/// Will be null unless true
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:isNonCredit" )]
		public bool? IsNonCredit { get; set; }

		[JsonProperty( PropertyName = "ceterms:isPartOf" )]
		public List<string> IsPartOf { get; set; }


		[JsonProperty( PropertyName = "ceterms:jurisdiction" )]
		public List<JurisdictionProfile> Jurisdiction { get; set; }
		//
		[JsonProperty( PropertyName = "ceterms:keyword" )]
		public LanguageMapList Keyword { get; set; }



		[JsonProperty( PropertyName = "ceterms:learningDeliveryType" )]
		public List<CredentialAlignmentObject> LearningDeliveryType { get; set; }


		[JsonProperty( PropertyName = "ceterms:processStandards" )]
		public string ProcessStandards { get; set; } //URL

		[JsonProperty( PropertyName = "ceterms:processStandardsDescription" )]
		public LanguageMap ProcessStandardsDescription { get; set; } = new LanguageMap();

		/// <summary>
		/// Action related to the credential
		/// This may end up being a list of URIs?
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:relatedAction" )]
		public List<CredentialingAction> RelatedAction { get; set; }


		[JsonProperty( PropertyName = "ceterms:renewalFrequency" )]
		public string RenewalFrequency { get; set; } //duration item


		[JsonProperty( PropertyName = "ceterms:revocation" )]
		public List<RevocationProfile> Revocation { get; set; }

		/// <summary>
		/// Another source of information about the entity being described.
		/// List of URIs
		/// ceterms:sameAs
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:sameAs" )]
		public List<string> SameAs { get; set; }

		[JsonProperty( PropertyName = "ceterms:subject" )]
		public List<CredentialAlignmentObject> Subject { get; set; }

		[JsonProperty( PropertyName = "ceterms:subjectWebpage" )]
        public string SubjectWebpage { get; set; }

		// <summary>
		// NOTE: the TargetPathway is an inverse property for a credential. That is, it will not be published with the credential, and is instead derived via a PathwayComponent
		// </summary>
		//[JsonProperty( PropertyName = "ceterms:targetPathway" )]
		//public List<string> TargetPathway { get; set; }

		[JsonProperty( PropertyName = "ceterms:versionIdentifier" )]
        public List<IdentifierValue> VersionIdentifier { get; set; }

		#region Versions
		[JsonProperty( PropertyName = "ceterms:latestVersion" )]
        public string LatestVersion { get; set; } //URL

        [JsonProperty( PropertyName = "ceterms:previousVersion" )]
        public string PreviousVersion { get; set; } //URL

		[JsonProperty( PropertyName = "ceterms:nextVersion" )]
		public string NextVersion { get; set; } //URL

		[JsonProperty( PropertyName = "ceterms:supersededBy" )]
		public string SupersededBy { get; set; } //URL

		[JsonProperty( PropertyName = "ceterms:supersedes" )]
		public string Supersedes { get; set; } //URL
        #endregion
        //

		#region Occupation, Industry, Program
		[JsonProperty( PropertyName = "ceterms:occupationType" )]
		public List<CredentialAlignmentObject> OccupationType { get; set; } = new List<CredentialAlignmentObject>();

		//[JsonProperty( PropertyName = "ceterms:alternativeOccupationType" )]
		//public LanguageMapList AlternativeOccupationType { get; set; } = new LanguageMapList();

		[JsonProperty( PropertyName = "ceterms:industryType" )]
		public List<CredentialAlignmentObject> IndustryType { get; set; } = new List<CredentialAlignmentObject>();

		[JsonProperty( PropertyName = "ceterms:naics" )]
		public List<string> Naics { get; set; } = new List<string>();

		//[JsonProperty( PropertyName = "ceterms:alternativeIndustryType" )]
		//public LanguageMapList AlternativeIndustryType { get; set; } = new LanguageMapList();
		[JsonProperty( PropertyName = "ceterms:instructionalProgramType" )]
		public List<CredentialAlignmentObject> InstructionalProgramType { get; set; } = new List<CredentialAlignmentObject>();
		#endregion


		#region costs
		[JsonProperty( PropertyName = "ceterms:estimatedCost" )]
        public List<CostProfile> EstimatedCost { get; set; }


		[JsonProperty( PropertyName = "ceterms:commonCosts" )]
		public List<string> CommonCosts { get; set; }

		#endregion


		#region Condition Profiles
		[JsonProperty( PropertyName = "ceterms:requires" )]
        public List<ConditionProfile> Requires { get; set; }

        [JsonProperty( PropertyName = "ceterms:corequisite" )]
        public List<ConditionProfile> Corequisite { get; set; }

		[JsonProperty( PropertyName = "ceterms:coPrerequisite" )]
		public List<ConditionProfile> CoPrerequisite { get; set; }

		[JsonProperty( PropertyName = "ceterms:recommends" )]
        public List<ConditionProfile> Recommends { get; set; }


		[JsonProperty( PropertyName = "ceterms:renewal" )]
		public List<ConditionProfile> Renewal { get; set; }

		[JsonProperty( PropertyName = "ceterms:commonConditions" )]
		public List<string> CommonConditions { get; set; }

		#endregion

		#region process profiles
		[JsonProperty( PropertyName = "ceterms:administrationProcess" )]
        public List<ProcessProfile> AdministrationProcess { get; set; }

        [JsonProperty( PropertyName = "ceterms:developmentProcess" )]
        public List<ProcessProfile> DevelopmentProcess { get; set; }

        [JsonProperty( PropertyName = "ceterms:maintenanceProcess" )]
        public List<ProcessProfile> MaintenanceProcess { get; set; }

        [JsonProperty( PropertyName = "ceterms:appealProcess" )]
        public List<ProcessProfile> AppealProcess { get; set; }

        [JsonProperty( PropertyName = "ceterms:complaintProcess" )]
        public List<ProcessProfile> ComplaintProcess { get; set; }

        [JsonProperty( PropertyName = "ceterms:reviewProcess" )]
        public List<ProcessProfile> ReviewProcess { get; set; }

        [JsonProperty( PropertyName = "ceterms:revocationProcess" )]
        public List<ProcessProfile> RevocationProcess { get; set; }
		#endregion


		#region Connections
		[JsonProperty( PropertyName = "ceterms:advancedStandingFrom" )]
        public List<ConditionProfile> AdvancedStandingFrom { get; set; }

        [JsonProperty( PropertyName = "ceterms:isAdvancedStandingFor" )]
        public List<ConditionProfile> IsAdvancedStandingFor { get; set; }

        [JsonProperty( PropertyName = "ceterms:isPreparationFor" )]
        public List<ConditionProfile> IsPreparationFor { get; set; }

        [JsonProperty( PropertyName = "ceterms:isRecommendedFor" )]
        public List<ConditionProfile> IsRecommendedFor { get; set; }

        [JsonProperty( PropertyName = "ceterms:isRequiredFor" )]
        public List<ConditionProfile> IsRequiredFor { get; set; }

        [JsonProperty( PropertyName = "ceterms:preparationFrom" )]
        public List<ConditionProfile> PreparationFrom { get; set; }
		#endregion


		#region BYs
		/// <summary>
		/// OwnedBy
		/// Will either by an Id array, or a thin organization array
		/// </summary>
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

		[JsonProperty( PropertyName = "ceterms:revokedBy" )]
		public List<string> RevokedBy { get; set; }

		[JsonProperty( PropertyName = "ceterms:renewedBy" )]
		public List<string> RenewedBy { get; set; }
		#endregion

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

		[JsonProperty( PropertyName = "ceterms:renewedIn" )]
		public List<JurisdictionProfile> RenewedIn { get; set; }

		[JsonProperty( PropertyName = "ceterms:revokedIn" )]
		public List<JurisdictionProfile> RevokedIn { get; set; }

		#endregion


		#region OBSOLETE: Holder, earnings, EmploymentOutcome


		//TODO - this may change to URIs
		//		- in this case, the earningProfile should have a credential reference like
		[JsonProperty( PropertyName = "ceterms:earningsList" )]
		public List<EarningsProfile> EarningsList { get; set; }

		[JsonProperty( PropertyName = "ceterms:earnings" )]
		public List<string> Earnings { get; set; }

		//TODO - this may change to URIs
		//		- in this case, the employmentOutcomeProfile should have a credential reference like employmentOutcomeProfileFor
		[JsonProperty( PropertyName = "ceterms:employmentOutcomeList" )]
		public List<EmploymentOutcomeProfile> EmploymentOutcomeList { get; set; }

		[JsonProperty( PropertyName = "ceterms:employmentOutcome" )]
		public List<string> EmploymentOutcome { get; set; }


		[JsonProperty( PropertyName = "ceterms:holdersList" )]
		public List<HoldersProfile> HoldersList { get; set; }

		[JsonProperty( PropertyName = "ceterms:holders" )]
		public List<string> Holders { get; set; }

		#endregion

	}

	public class RevocationProfile
    {
        public RevocationProfile()
        {
			Type = "ceterms:RevocationProfile";
			Jurisdiction = new List<JurisdictionProfile>();
           // RevocationCriteria = new List<string>();
            //CredentialProfiled = new List<string>();
        }

        [JsonProperty( "@type" )]
        public string Type { get; set; }

        [JsonProperty( PropertyName = "ceterms:description" )]
        public LanguageMap Description { get; set; }

        [JsonProperty( PropertyName = "ceterms:dateEffective" )]
        public string DateEffective { get; set; }

		[JsonProperty( PropertyName = "ceterms:jurisdiction" )]
        public List<JurisdictionProfile> Jurisdiction { get; set; }

        [JsonProperty( PropertyName = "ceterms:revocationCriteria" )]
        public string RevocationCriteria { get; set; } //URL

        [JsonProperty( PropertyName = "ceterms:revocationCriteriaDescription" )]
        public LanguageMap RevocationCriteriaDescription { get; set; }

	}
}

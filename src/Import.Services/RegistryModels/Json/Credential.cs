using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RA.Models.Json;

namespace RA.Models.Json
{
    public class Credential : JsonLDDocument
    {
		[JsonIgnore]
		public static string classType = "ceterms:Credential";
		public Credential()
        {
			Subject = new List<CredentialAlignmentObject>();
			OccupationType = new List<CredentialAlignmentObject>();
            IndustryType = new List<CredentialAlignmentObject>();
			Naics = new List<string>();
			Keyword = new List<string>();
            SubjectWebpage = null;
			HasPart = null;
			IsPartOf = null;
			AudienceLevel = new List<CredentialAlignmentObject>();
            AvailableOnlineAt = new List<string>();
            AvailabilityListing = new List<string>();
			CopyrightHolder = new List<OrganizationBase>();
			AlternateName = new List<string>();
			//Image = new List<string>();
			InLanguage = new List<string>();
			
			VersionIdentifier = new List<IdentifierValue>();
			DegreeConcentration = new List<CredentialAlignmentObject>();
            DegreeMajor = new List<CredentialAlignmentObject>();
            DegreeMinor = new List<CredentialAlignmentObject>();
            EstimatedCost = new List<CostProfile>();
            Requires = new List<ConditionProfile>();
            Corequisite = new List<ConditionProfile>();
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
			
			FinancialAssistance = new List<FinancialAlignmentObject>();
            CredentialStatusType = new CredentialAlignmentObject();
            AdvancedStandingFrom = new List<ConditionProfile>();
            IsAdvancedStandingFor = new List<ConditionProfile>();
            IsPreparationFor = new List<ConditionProfile>();
            IsRecommendedFor = new List<ConditionProfile>();
            IsRequiredFor = new List<ConditionProfile>();
            PreparationFrom = new List<ConditionProfile>();
			Jurisdiction = new List<JurisdictionProfile>();
			AvailableAt = new List<Json.Place>();

            CommonConditions = new List<string>();
            CommonCosts = new List<string>();
            Revocation = new List<RevocationProfile>();
            Renewal = new List<ConditionProfile>();
        }

        [JsonProperty( "@id" )]
        public string CtdlId { get; set; }

        /// <summary>
        /// Need a custom mapping to @type based on input value
        /// </summary>
        [JsonProperty( "@type" )]
        public string CredentialType { get; set; }


        [JsonProperty( PropertyName = "ceterms:name" )]
        public string Name { get; set; }

        [JsonProperty( PropertyName = "ceterms:description" )]
        public string Description { get; set; }


		[JsonProperty( PropertyName = "ceterms:dateEffective" )]
        public string DateEffective { get; set; }

        [JsonProperty( PropertyName = "ceterms:ctid" )]
        public string Ctid { get; set; }

        [JsonProperty( PropertyName = "ceterms:alternateName" )]
        public List<string> AlternateName { get; set; }

        [JsonProperty( PropertyName = "ceterms:image" )]
        public string Image { get; set; } //Image URL

		[JsonProperty( PropertyName = "ceterms:subjectWebpage" )]
        public string SubjectWebpage { get; set; }

		[JsonProperty( PropertyName = "ceterms:codedNotation" )]
		public string CodedNotation { get; set; }
		//public string CodedNotation { get; set; }

		[JsonProperty( PropertyName = "ceterms:hasPart" )]
		public List<EntityBase> HasPart { get; set; }


		[JsonProperty( PropertyName = "ceterms:isPartOf" )]
		public List<EntityBase> IsPartOf { get; set; }

		[JsonProperty( PropertyName = "ceterms:availabilityListing" )]
        public List<string> AvailabilityListing { get; set; } //URL

        [JsonProperty( PropertyName = "ceterms:availableOnlineAt" )]
        public List<string> AvailableOnlineAt { get; set; }

        [JsonProperty( PropertyName = "ceterms:credentialId" )]
        public string CredentialId { get; set; }

        [JsonProperty( PropertyName = "ceterms:versionIdentifier" )]
        public List<IdentifierValue> VersionIdentifier { get; set; }


		[JsonProperty( PropertyName = "ceterms:inLanguage" )]
		//public string InLanguage { get; set; }
		public List<string> InLanguage { get; set; }

		[JsonProperty( PropertyName = "ceterms:processStandards" )]
        public string ProcessStandards { get; set; } //URL

        [JsonProperty( PropertyName = "ceterms:processStandardsDescription" )]
        public string ProcessStandardsDescription { get; set; }

        [JsonProperty( PropertyName = "ceterms:latestVersion" )]
        public string LatestVersion { get; set; } //URL

        [JsonProperty( PropertyName = "ceterms:previousVersion" )]
        public string PreviousVersion { get; set; } //URL

        [JsonProperty( PropertyName = "ceterms:subject" )]
        public List<CredentialAlignmentObject> Subject { get; set; }

        [JsonProperty( PropertyName = "ceterms:occupationType" )]
        public List<CredentialAlignmentObject> OccupationType { get; set; }

        [JsonProperty( PropertyName = "ceterms:industryType" )]
        public List<CredentialAlignmentObject> IndustryType { get; set; }

		[JsonProperty( PropertyName = "ceterms:naics" )]
		public List<string> Naics { get; set; }

		[JsonProperty( PropertyName = "ceterms:keyword" )]
        public List<string> Keyword { get; set; }


		[JsonProperty( PropertyName = "ceterms:jurisdiction" )]
		public List<JurisdictionProfile> Jurisdiction { get; set; }

		[JsonProperty( PropertyName = "ceterms:copyrightHolder" )]
		public List<OrganizationBase> CopyrightHolder { get; set; }
		
		[JsonProperty( PropertyName = "ceterms:audienceLevelType" )]
        public List<CredentialAlignmentObject> AudienceLevel { get; set; }

        [JsonProperty( PropertyName = "ceterms:audienceType" )]
        public List<CredentialAlignmentObject> AudienceType { get; set; }

        [JsonProperty( PropertyName = "ceterms:degreeConcentration" )]
        public List<CredentialAlignmentObject> DegreeConcentration { get; set; }

        [JsonProperty( PropertyName = "ceterms:degreeMajor" )]
        public List<CredentialAlignmentObject> DegreeMajor { get; set; }

        [JsonProperty( PropertyName = "ceterms:degreeMinor" )]
        public List<CredentialAlignmentObject> DegreeMinor { get; set; }
		#region costs
		[JsonProperty( PropertyName = "ceterms:estimatedCost" )]
        public List<CostProfile> EstimatedCost { get; set; }


		[JsonProperty( PropertyName = "ceterms:commonCosts" )]
		public List<string> CommonCosts { get; set; }

		#endregion
		[JsonProperty( PropertyName = "ceterms:estimatedDuration" )]
        public List<DurationProfile> EstimatedDuration { get; set; }

		[JsonProperty( PropertyName = "ceterms:renewalFrequency" )]
		public string RenewalFrequency { get; set; }


		#region Condition Profiles
		[JsonProperty( PropertyName = "ceterms:requires" )]
        public List<ConditionProfile> Requires { get; set; }

        [JsonProperty( PropertyName = "ceterms:corequisite" )]
        public List<ConditionProfile> Corequisite { get; set; }

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

		[JsonProperty( PropertyName = "ceterms:financialAssistance" )]
        public List<FinancialAlignmentObject> FinancialAssistance { get; set; }

        [JsonProperty( PropertyName = "ceterms:credentialStatusType" )]
        public CredentialAlignmentObject CredentialStatusType { get; set; }

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

		[JsonProperty( PropertyName = "ceterms:availableAt" )]
		public List<Place> AvailableAt { get; set; }

        [JsonProperty( PropertyName = "ceterms:revocation" )]
        public List<RevocationProfile> Revocation { get; set; }


		#region BYs
		/// <summary>
		/// OwnedBy
		/// Will either by an Id array, or a thin organization array
		/// </summary>
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

		[JsonProperty( PropertyName = "ceterms:revokedBy" )]
		public List<OrganizationBase> RevokedBy { get; set; }

		[JsonProperty( PropertyName = "ceterms:renewedBy" )]
		public List<OrganizationBase> RenewedBy { get; set; }
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
        public string Description { get; set; }

        [JsonProperty( PropertyName = "ceterms:dateEffective" )]
        public string DateEffective { get; set; }

        [JsonProperty( PropertyName = "ceterms:jurisdiction" )]
        public List<JurisdictionProfile> Jurisdiction { get; set; }

        [JsonProperty( PropertyName = "ceterms:revocationCriteria" )]
        public string RevocationCriteria { get; set; }

        [JsonProperty( PropertyName = "ceterms:revocationCriteriaDescription" )]
        public string RevocationCriteriaDescription { get; set; }
    }
}

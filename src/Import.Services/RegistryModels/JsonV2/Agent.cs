using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
	public class Agent : JsonLDDocument
	{
		[JsonIgnore]
		public static string classType = "ceterms:CredentialOrganization";

		public Agent()
        {			
			Type = "ceterms:CredentialOrganization";
			IndustryType = new List<CredentialAlignmentObject>();
			Naics = new List<string>();
			//Keyword = new List<string>();
			SubjectWebpage = null;//new List<string>();
           // MissionAndGoalsStatement = new List<string>();
            //Image = new List<string>();
            AgentType = new List<CredentialAlignmentObject>();
            AgentSectorType = new List<CredentialAlignmentObject>();
            //AgentPurpose = new List<string>();
            ServiceType = new List<CredentialAlignmentObject>();
            AvailabilityListing = new List<string>();
			SameAs = new List<string>();
			SocialMedia = new List<string>();
            Address = new List<Place>();
            //ContactPoint = new List<Json.ContactPoint>();

			HasConditionManifest = new List<string>();
			HasCostManifest = new List<string>();

			AccreditedBy = null;
            ApprovedBy = null;
            RegulatedBy = null;
            RecognizedBy = null;

            Accredits = null;
			Approves = null;
			Offers = null;
			Owns = null;
			Renews = null;
			Revokes = null;
			Recognizes = null;
            Regulates = null;

            AccreditedIn = null;
            ApprovedIn = null;
            RecognizedIn = null;
            RegulatedIn = null;
            VerificationServiceProfiles = new List<VerificationServiceProfile>();

			ParentOrganization = null;
			Department = new List<string>();
            SubOrganization = new List<string>();
			Jurisdiction = new List<JurisdictionProfile>();
        }

		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }

		/// <summary>
		/// The type of organization is one of :
		/// - CredentialOrganization
		/// - QACredentialOrganization
		/// </summary>

		[JsonProperty( "@type" )]
		public string Type { get; set; }

        [DefaultValue("")]
		[JsonProperty( "ceterms:name" )]
		public LanguageMap Name { get; set; }
		[JsonProperty( "ceterms:description" )]
		public LanguageMap Description { get; set; }


		[JsonProperty( PropertyName = "ceterms:ctid" )]
		public string Ctid { get; set; }

		[JsonProperty( PropertyName = "ceterms:subjectWebpage" )]
		public string SubjectWebpage { get; set; } //URL

		/// <summary>
		/// The status type of this Organization. 
		/// The default is Active. 
		/// ConceptScheme: ceterms:StatusCategory
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:lifecycleStatusType" )]
		public CredentialAlignmentObject LifecycleStatusType { get; set; }
		
		//INs
		[JsonProperty( PropertyName = "ceterms:accreditedIn" )]
        public List<JurisdictionProfile> AccreditedIn { get; set; }

        [JsonProperty( PropertyName = "ceterms:approvedIn" )]
        public List<JurisdictionProfile> ApprovedIn { get; set; }

        [JsonProperty( PropertyName = "ceterms:recognizedIn" )]
        public List<JurisdictionProfile> RecognizedIn { get; set; }

        [JsonProperty( PropertyName = "ceterms:regulatedIn" )]
        public List<JurisdictionProfile> RegulatedIn { get; set; }

		//
        [JsonProperty( PropertyName = "ceterms:sameAs" )]
		public List<string> SameAs { get; set; } //URL

		[JsonProperty( PropertyName = "ceterms:alternateName" )]
		public LanguageMapList AlternateName { get; set; }

		[JsonProperty( PropertyName = "ceterms:socialMedia" )]
		public List<string> SocialMedia { get; set; }

		/// <summary>
		/// Identifier
		/// Definition:	Alphanumeric Identifier value.
		/// List of URIs 
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:identifierValue" )]
		public List<IdentifierValue> Identifier { get; set; }

		[JsonProperty( PropertyName = "ceterms:image" )]
		public string Image { get; set; } //Image URL

		[JsonProperty( PropertyName = "ceterms:foundingDate" )]
		public string FoundingDate { get; set; }

		[JsonProperty( PropertyName = "ceterms:agentType" )]
		public List<CredentialAlignmentObject> AgentType { get; set; }


		[JsonProperty( PropertyName = "ceterms:agentSectorType" )]
		public List<CredentialAlignmentObject> AgentSectorType { get; set; }


		[JsonProperty( PropertyName = "ceterms:industryType" )]
        public List<CredentialAlignmentObject> IndustryType { get; set; }

		[JsonProperty( PropertyName = "ceterms:alternativeIndustryType" )]
		public LanguageMapList AlternativeIndustryType { get; set; } = new LanguageMapList();

		[JsonProperty( PropertyName = "ceterms:naics" )]
		public List<string> Naics { get; set; }

		[JsonProperty( PropertyName = "ceterms:keyword" )]
        public LanguageMapList Keyword { get; set; }

		//20-10-31 - replace by Identifier
		//[JsonProperty( PropertyName = "ceterms:alternativeIdentifier" )]
		//public List<IdentifierValue> AlternativeIdentifier { get; set; }

		[JsonProperty( PropertyName = "ceterms:missionAndGoalsStatement" )]
		public string MissionAndGoalsStatement { get; set; } //URL

		[JsonProperty( PropertyName = "ceterms:missionAndGoalsStatementDescription" )]
		public LanguageMap MissionAndGoalsStatementDescription { get; set; }

		[JsonProperty( PropertyName = "ceterms:agentPurpose" )]
		public string AgentPurpose { get; set; } //URL

		[JsonProperty( PropertyName = "ceterms:agentPurposeDescription" )]
		public LanguageMap AgentPurposeDescription { get; set; }

		[JsonProperty( PropertyName = "ceterms:duns" )]
		public string DUNS { get; set; }


		[JsonProperty( PropertyName = "ceterms:fein" )]
		public string FEIN { get; set; }


		[JsonProperty( PropertyName = "ceterms:ipedsID" )]
		public string IpedsID { get; set; }

		[JsonProperty( PropertyName = "ceterms:opeID" )]
		public string OPEID { get; set; }

        [JsonProperty( PropertyName = "ceterms:leiCode" )]
        public string LEICode { get; set; }

		[JsonProperty( PropertyName = "ceterms:isicv4" )]
		public string ISICV4 { get; set; }

		[JsonProperty( PropertyName = "ceterms:ncesID" )]
		public string NcesID { get; set; }
		//
		[JsonProperty( PropertyName = "ceterms:email" )]
		public List<string> Email { get; set; }


        [JsonProperty( PropertyName = "ceterms:availabilityListing" )]
        public List<string> AvailabilityListing { get; set; }


		/// <summary>
		/// Webpage or online document that defines or explains the nature of transfer value handled by the organization.
		/// URI
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:transferValueStatement" )]
		public string TransferValueStatement { get; set; }

		[JsonProperty( PropertyName = "ceterms:transferValueStatementDescription" )]
		public LanguageMap TransferValueStatementDescription { get; set; }

		[JsonProperty( PropertyName = "ceterms:serviceType" )]
        public List<CredentialAlignmentObject> ServiceType { get; set; }

        //public Jurisdiction Jurisdiction { get; set; }
        [JsonProperty( PropertyName = "ceterms:address" )]
        public  List<Place> Address { get; set; }

        //[JsonProperty( PropertyName = "ceterms:targetContactPoint" )]
        //public List<ContactPoint> ContactPoint { get; set; }

		[JsonProperty( PropertyName = "ceterms:jurisdiction" )]
		public List<JurisdictionProfile> Jurisdiction { get; set; }

		[JsonProperty( PropertyName = "ceterms:accreditedBy" )]
        public List<string> AccreditedBy { get; set; }

        [JsonProperty( PropertyName = "ceterms:approvedBy" )]
        public List<string> ApprovedBy { get; set; }

        [JsonProperty( PropertyName = "ceterms:recognizedBy" )]
        public List<string> RecognizedBy { get; set; }

        [JsonProperty( PropertyName = "ceterms:regulatedBy" )]
        public List<string> RegulatedBy { get; set; }

        [JsonProperty( PropertyName = "ceterms:accredits" )]
        public List<string> Accredits { get; set; }

        [JsonProperty( PropertyName = "ceterms:approves" )]
		public List<string> Approves { get; set; }

		[JsonProperty( PropertyName = "ceterms:offers" )]
		public List<string> Offers { get; set; }

		[JsonProperty( PropertyName = "ceterms:owns" )]
		public List<string> Owns { get; set; }

		[JsonProperty( PropertyName = "ceterms:renews" )]
		public List<string> Renews { get; set; }

		[JsonProperty( PropertyName = "ceterms:revokes" )]
		public List<string> Revokes { get; set; }

		[JsonProperty( PropertyName = "ceterms:recognizes" )]
		public List<string> Recognizes { get; set; }
        [JsonProperty( PropertyName = "ceterms:regulates" )]
        public List<string> Regulates { get; set; }

        [JsonProperty( PropertyName = "ceterms:hasConditionManifest" )]
		public List<string> HasConditionManifest { get; set; }

		[JsonProperty( PropertyName = "ceterms:hasCostManifest" )]
		public List<string> HasCostManifest { get; set; }

		[JsonProperty( PropertyName = "ceterms:hasVerificationService" )]
        public List<VerificationServiceProfile> VerificationServiceProfiles { get; set; }

        [JsonProperty( PropertyName = "ceterms:administrationProcess", NullValueHandling = NullValueHandling.Ignore )]
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

		[JsonProperty( PropertyName = "ceterms:parentOrganization" )]
		public List<string> ParentOrganization { get; set; }

		[JsonProperty( PropertyName = "ceterms:department" )]
        public List<string> Department { get; set; }

        [JsonProperty( PropertyName = "ceterms:subOrganization" )]
        public List<string> SubOrganization { get; set; }
    }
}

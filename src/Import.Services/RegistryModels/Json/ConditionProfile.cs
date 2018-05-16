using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RA.Models.Json
{
    public class ConditionProfile
    {
        public ConditionProfile()
        {
			AssertedBy = null;
			//AssertedByList = new List<OrganizationBase>();
			// AssertedBy = new List<OrganizationBase>();
			EstimatedCosts = new List<CostProfile>();
            ResidentOf = new List<JurisdictionProfile>();
			SubjectWebpage = null; 
            AudienceLevelType = new List<CredentialAlignmentObject>();
            AudienceType = new List<CredentialAlignmentObject>();
            //AudienceLevel = new List<string>();
            Condition = new List<string>();
			SubmissionOf = new List<string>();
			CreditUnitType = new CredentialAlignmentObject();
            //ApplicableAudienceType = new List<string>();
            AlternativeCondition = new List<ConditionProfile>();
            TargetAssessment = new List<EntityBase>();
            TargetCredential = new List<EntityBase>();
            TargetLearningOpportunity = new List<EntityBase>();
			TargetCompetency = new List<CredentialAlignmentObject>();

			Type = "ceterms:ConditionProfile";
			Jurisdiction = new List<JurisdictionProfile>();
			ResidentOf = new List<JurisdictionProfile>();
           // Renewal = new List<ConditionProfile>();
		}

        [JsonProperty( "@type" )]
        public string Type { get; set; }

        [JsonProperty( PropertyName = "ceterms:name" )]
        public string Name { get; set; }

        [JsonProperty( PropertyName = "ceterms:description" )]
        public string Description { get; set; }

        [JsonProperty( PropertyName = "ceterms:subjectWebpage" )]
		public string SubjectWebpage { get; set; }

		//TODO - alter from enumeration
		[JsonProperty( PropertyName = "ceterms:audienceLevelType" )]
        public List<CredentialAlignmentObject> AudienceLevelType { get; set; }

        [JsonProperty( PropertyName = "ceterms:audienceType" )]
        public List<CredentialAlignmentObject> AudienceType { get; set; }

        //[JsonProperty( PropertyName = "ceterms:audienceLevel" )]
        //public List<string> AudienceLevel { get; set; }

        [JsonProperty( PropertyName = "ceterms:dateEffective" )]
        public string DateEffective { get; set; }

        [JsonProperty( PropertyName = "ceterms:condition" )]
        public List<string> Condition { get; set; }

        [JsonProperty( PropertyName = "ceterms:submissionOf" )]
        public List<string> SubmissionOf { get; set; }

		/// <summary>
		/// Organization that asserts this condition
		/// NOTE: It must be serialized to a List
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:assertedBy" )]
		public object AssertedBy { get; set; }
		//public List<OrganizationBase> AssertedBy { get; set; }


		[JsonProperty( PropertyName = "ceterms:experience" )]
        public string Experience { get; set; }

        [JsonProperty( PropertyName = "ceterms:minimumAge" )]
        public int MinimumAge { get; set; }

        [JsonProperty( PropertyName = "ceterms:yearsOfExperience" )]
        public decimal YearsOfExperience { get; set; }

        [JsonProperty( PropertyName = "ceterms:weight" )]
        public decimal Weight { get; set; }

        [JsonProperty( PropertyName = "ceterms:creditHourType" )]
        public string CreditHourType { get; set; }

        [JsonProperty( PropertyName = "ceterms:creditHourValue" )]
        public decimal CreditHourValue { get; set; }

        [JsonProperty( PropertyName = "ceterms:creditUnitType" )]
        public CredentialAlignmentObject CreditUnitType { get; set; } //Used for publishing

        [JsonProperty( PropertyName = "ceterms:creditUnitTypeDescription" )]
        public string CreditUnitTypeDescription { get; set; }

        [JsonProperty( PropertyName = "ceterms:creditUnitValue" )]
        public decimal CreditUnitValue { get; set; }


		[JsonProperty( PropertyName = "ceterms:targetCompetency" )]
		public List<CredentialAlignmentObject> TargetCompetency { get; set; }

		//external classes =====================================
		[JsonProperty( PropertyName = "ceterms:estimatedCost" )]
        public List<CostProfile> EstimatedCosts { get; set; }

        [JsonProperty( PropertyName = "ceterms:jurisdiction" )]
        public List<JurisdictionProfile> Jurisdiction { get; set; }

        [JsonProperty( PropertyName = "ceterms:residentOf" )]
        public List<JurisdictionProfile> ResidentOf { get; set; }

        [JsonProperty( PropertyName = "ceterms:targetAssessment" )]
        public List<EntityBase> TargetAssessment { get; set; }

        [JsonProperty( PropertyName = "ceterms:targetCredential" )]
        public List<EntityBase> TargetCredential { get; set; }

        [JsonProperty( PropertyName = "ceterms:targetLearningOpportunity" )]
        public List<EntityBase> TargetLearningOpportunity { get; set; }

        [JsonProperty( PropertyName = "ceterms:alternativeCondition" )]
        public List<ConditionProfile> AlternativeCondition { get; set; }



    }
}

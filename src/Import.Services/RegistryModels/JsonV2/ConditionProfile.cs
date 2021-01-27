using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RA.Models.JsonV2
{
    public class ConditionProfile
    {
        public ConditionProfile()
        {
			AssertedBy = null;
			//AssertedByList = new List<string>();
			// AssertedBy = new List<string>();
			EstimatedCost = new List<CostProfile>();
            ResidentOf = new List<JurisdictionProfile>();
			SubjectWebpage = null; 
            AudienceLevelType = new List<CredentialAlignmentObject>();
            AudienceType = new List<CredentialAlignmentObject>();
            //AudienceLevel = new List<string>();
            //Condition = new List<string>();
			//SubmissionOf = new List<string>();
			CreditUnitType = new CredentialAlignmentObject();
            //ApplicableAudienceType = new List<string>();
            AlternativeCondition = new List<ConditionProfile>();
            TargetAssessment = new List<string>();
            TargetCredential = new List<string>();
            TargetLearningOpportunity = new List<string>();
			TargetCompetency = new List<CredentialAlignmentObject>();

			Type = "ceterms:ConditionProfile";
			Jurisdiction = new List<JurisdictionProfile>();
			ResidentOf = new List<JurisdictionProfile>();
           // Renewal = new List<ConditionProfile>();
		}

        [JsonProperty( "@type" )]
        public string Type { get; set; }

        [JsonProperty( PropertyName = "ceterms:name" )]
        public LanguageMap Name { get; set; }

        [JsonProperty( PropertyName = "ceterms:description" )]
        public LanguageMap Description { get; set; }

        [JsonProperty( PropertyName = "ceterms:subjectWebpage" )]
		public string SubjectWebpage { get; set; }

		//TODO - alter from enumeration
		[JsonProperty( PropertyName = "ceterms:audienceLevelType" )]
        public List<CredentialAlignmentObject> AudienceLevelType { get; set; }

        [JsonProperty( PropertyName = "ceterms:audienceType" )]
        public List<CredentialAlignmentObject> AudienceType { get; set; }

        [JsonProperty( PropertyName = "ceterms:dateEffective" )]
        public string DateEffective { get; set; }

        [JsonProperty( PropertyName = "ceterms:condition" )]
        public LanguageMapList Condition { get; set; }

        [JsonProperty( PropertyName = "ceterms:submissionOf" )]
        public List<string> SubmissionOf { get; set; }
		//public object SubmissionOf { get; set; }

		[JsonIgnore]
		public LanguageMapList SubmissionOfOld { get; set; }

		[JsonProperty( PropertyName = "ceterms:submissionOfDescription" )]
		public LanguageMap SubmissionOfDescription { get; set; }
		/// <summary>
		/// Organization that asserts this condition
		/// NOTE: It must be serialized to a List
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:assertedBy" )]
		public List<string> AssertedBy { get; set; }

		[JsonProperty( PropertyName = "ceterms:experience" )]
        public string Experience { get; set; }

        [JsonProperty( PropertyName = "ceterms:minimumAge" )]
        public int MinimumAge { get; set; }

        [JsonProperty( PropertyName = "ceterms:yearsOfExperience" )]
        public decimal YearsOfExperience { get; set; }

        [JsonProperty( PropertyName = "ceterms:weight" )]
        public decimal Weight { get; set; }
		//20-10-31 CreditValue is now of type ValueProfile
		[JsonProperty( PropertyName = "ceterms:creditValue" )]
		public List<ValueProfile> CreditValue { get; set; } = null;
		//
		//[JsonProperty( PropertyName = "ceterms:creditHourType" )]
  //      public LanguageMap CreditHourType { get; set; }

  //      [JsonProperty( PropertyName = "ceterms:creditHourValue" )]
  //      public decimal CreditHourValue { get; set; }

        [JsonProperty( PropertyName = "ceterms:creditUnitType" )]
        public CredentialAlignmentObject CreditUnitType { get; set; } //Used for publishing

        [JsonProperty( PropertyName = "ceterms:creditUnitTypeDescription" )]
        public LanguageMap CreditUnitTypeDescription { get; set; }

        [JsonProperty( PropertyName = "ceterms:creditUnitValue" )]
        public decimal CreditUnitValue { get; set; }


		[JsonProperty( PropertyName = "ceterms:targetCompetency" )]
		public List<CredentialAlignmentObject> TargetCompetency { get; set; }

		//external classes =====================================
		[JsonProperty( PropertyName = "ceterms:estimatedCost" )]
        public List<CostProfile> EstimatedCost { get; set; }

        [JsonProperty( PropertyName = "ceterms:jurisdiction" )]
        public List<JurisdictionProfile> Jurisdiction { get; set; }

        [JsonProperty( PropertyName = "ceterms:residentOf" )]
        public List<JurisdictionProfile> ResidentOf { get; set; }

        [JsonProperty( PropertyName = "ceterms:targetAssessment" )]
        public List<string> TargetAssessment { get; set; }

        [JsonProperty( PropertyName = "ceterms:targetCredential" )]
        public List<string> TargetCredential { get; set; }

        [JsonProperty( PropertyName = "ceterms:targetLearningOpportunity" )]
        public List<string> TargetLearningOpportunity { get; set; }

        [JsonProperty( PropertyName = "ceterms:alternativeCondition" )]
        public List<ConditionProfile> AlternativeCondition { get; set; }



    }
}

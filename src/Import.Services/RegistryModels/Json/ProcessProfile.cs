using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.ComponentModel;

namespace RA.Models.Json
{
    /// <summary>
    /// Common input class for all condition profiles
    /// </summary>
    public class ProcessProfile
    {
        public ProcessProfile()
        {
			ProcessingAgent = new List<OrganizationBase>();
			ProcessMethod = null;
			ProcessStandards = null;
			ScoringMethodExample = null;
			SubjectWebpage = null;

			TargetAssessment = new List<EntityBase>();
			TargetCredential = new List<EntityBase>();
			TargetLearningOpportunity = new List<EntityBase>();

			TargetCompetencyFramework = null;

			Jurisdiction = new List<JurisdictionProfile>();
           // Region = new List<GeoCoordinates>();
            Type = "ceterms:ProcessProfile";
            ExternalInputType = new List<CredentialAlignmentObject>();
        }

        [JsonProperty( "@type" )]
        public string Type { get; set; }

        [JsonProperty( PropertyName = "ceterms:description" )]
        public string Description { get; set; }


        [JsonProperty( PropertyName = "ceterms:dateEffective" )]
        public string DateEffective { get; set; }

        [JsonProperty( PropertyName = "ceterms:externalInputType" )]
        public List<CredentialAlignmentObject> ExternalInputType { get; set; }

        [JsonProperty( PropertyName = "ceterms:processFrequency" )]
        public string ProcessFrequency { get; set; }

        [JsonProperty( PropertyName = "ceterms:processingAgent" )]
        public List<OrganizationBase> ProcessingAgent { get; set; }

        [JsonProperty( PropertyName = "ceterms:processMethod" )]
        public string ProcessMethod { get; set; }

        [JsonProperty( PropertyName = "ceterms:processMethodDescription" )]
        public string ProcessMethodDescription { get; set; }

        [JsonProperty( PropertyName = "ceterms:processStandards", NullValueHandling = NullValueHandling.Ignore )]
        public string ProcessStandards { get; set; }

        [JsonProperty( PropertyName = "ceterms:processStandardsDescription" )]
        public string ProcessStandardsDescription { get; set; }

        [JsonProperty( PropertyName = "ceterms:scoringMethodDescription" )]
        public string ScoringMethodDescription { get; set; }

        [JsonProperty( PropertyName = "ceterms:scoringMethodExample" )]
        public string ScoringMethodExample { get; set; }

        [JsonProperty( PropertyName = "ceterms:scoringMethodExampleDescription" )]
        public string ScoringMethodExampleDescription { get; set; }

        [JsonProperty( PropertyName = "ceterms:subjectWebpage" )]
		public string SubjectWebpage { get; set; } //URL


		[JsonProperty( PropertyName = "ceterms:verificationMethodDescription" )]
        public string VerificationMethodDescription { get; set; }

        [JsonProperty( PropertyName = "ceterms:jurisdiction" )]
        public List<JurisdictionProfile> Jurisdiction { get; set; }

		//[JsonProperty( PropertyName = "ceterms:region" )]
		//public List<GeoCoordinates> Region { get; set; }


		[JsonProperty( PropertyName = "ceterms:targetCredential" )]
		public List<EntityBase> TargetCredential { get; set; }

		[JsonProperty( PropertyName = "ceterms:targetAssessment" )]
		public List<EntityBase> TargetAssessment { get; set; }

		[JsonProperty( PropertyName = "ceterms:targetLearningOpportunity" )]
		public List<EntityBase> TargetLearningOpportunity { get; set; }

		[JsonProperty( PropertyName = "ceterms:targetCompetencyFramework" )]
		public List<EntityBase> TargetCompetencyFramework { get; set; }
	}
}




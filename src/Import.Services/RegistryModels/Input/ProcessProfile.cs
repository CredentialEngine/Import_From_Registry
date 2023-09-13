using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RA.Models.Input
{
    /// <summary>
    /// Common input class for all process profiles 
    /// 2018-09-02 Where LanguageMap alternates are available, only enter one. The system will check the string version first. 
    /// </summary>
    public class ProcessProfile
    {
        public ProcessProfile()
        {
            ExternalInputType = new List<string>();
			
			TargetCredential = new List<EntityReference>();
			TargetAssessment = new List<EntityReference>();
			TargetLearningOpportunity = new List<EntityReference>();
			//Region = new List<GeoCoordinates>();
		}
		/// <summary>
		/// Process Profile Description
		/// REQUIRED
		/// </summary>
		public string Description { get; set; }
        /// <summary>
        /// Alternately can provide a language map
        /// </summary>
        public LanguageMap Description_Map { get; set; } = new LanguageMap();

		/// <summary>
		/// Effective date of the content of this profile
		/// ceterms:dateEffective
		/// </summary>
		public string DateEffective { get; set; }

		/// <summary>
		/// Webpage that describes this entity.
		/// </summary>
		public string SubjectWebpage { get; set; }
		//public List<CredentialAlignmentObject> ExternalInputType { get; set; }

		/// <summary>
		/// Types of external stakeholders that provide input to an entity's processes or resources; select from an existing enumeration of such types.
		/// ConceptScheme
		/// https://credreg.net/ctdl/terms/ExternalInput
		/// inputType:Associations, inputType:Business, inputType:BusinessAssociation, inputType:Consumers, inputType:EducationAdministrators, inputType:Educators, inputType:Experts, inputType:Governments, inputType:Guardians, inputType:InternationalBodies, inputType:Practitioners, inputType:Public inputType:Students
		/// </summary>
		public List<string> ExternalInputType { get; set; }
		/// <summary>
		///  Interval of process occurence.
		/// </summary>
		public string ProcessFrequency { get; set; }
		/// <summary>
		/// Alternately use a LanguageMap
		/// </summary>
        public LanguageMap ProcessFrequency_Map { get; set; } = new LanguageMap();
		/// <summary>
		///  Organization or person performing the process
		/// </summary>
		public List<OrganizationReference> ProcessingAgent { get; set; } = new List<OrganizationReference>();
		/// <summary>
		/// Webpage or online document that describes the process methods.
		/// URL
		/// </summary>
		public string ProcessMethod { get; set; }
		/// <summary>
		/// Textual description of the process methods.
		/// </summary>
		public string ProcessMethodDescription { get; set; }
		/// <summary>
		/// Alternately use a LanguageMap
		/// </summary>
		public LanguageMap ProcessMethodDescription_Map { get; set; } = new LanguageMap();
		/// <summary>
		/// Webpage or online document that describes the criteria, standards, and/or requirements used with a process.
		/// </summary>
		public string ProcessStandards { get; set; }
		/// <summary>
		///  Textual description of the criteria, standards, and/or requirements used with a process.
		/// </summary>
		public string ProcessStandardsDescription { get; set; }
		/// <summary>
		/// Alternately use a LanguageMap
		/// </summary>
		public LanguageMap ProcessStandardsDescription_Map { get; set; } = new LanguageMap();
		/// <summary>
		/// Textual description of the method used to score the assessment.
		/// </summary>
		public string ScoringMethodDescription { get; set; }
		/// <summary>
		/// Alternately use a LanguageMap
		/// </summary>
		public LanguageMap ScoringMethodDescription_Map { get; set; } = new LanguageMap();
		/// <summary>
		/// Webpage or online document providing an example of the method or tool used to score the assessment.
		/// </summary>
		public string ScoringMethodExample { get; set; }
		/// <summary>
		/// Textual example of the method or tool used to score the assessment.
		/// </summary>
		public string ScoringMethodExampleDescription { get; set; }
		/// <summary>
		/// Alternately use a LanguageMap
		/// </summary>
		public LanguageMap ScoringMethodExampleDescription_Map { get; set; } = new LanguageMap();

		/// <summary>
		/// Assessment that provides direct, indirect, formative or summative evaluation or estimation of the nature, ability, or quality for an entity.
		/// </summary>
		public List<EntityReference> TargetAssessment { get; set; }
		/// <summary>
		/// Credential that is a focus or target of the condition, process or verification service.
		/// </summary>
		public List<EntityReference> TargetCredential { get; set; }
		/// <summary>
		/// Learning opportunity that is the focus of a condition, process or another learning opportunity.
		/// </summary>
		public List<EntityReference> TargetLearningOpportunity { get; set; }
		/// <summary>
		///  Competency framework relevant to the process being described.
		///  List of URL or CTIDs (recommended) that must exist in the registry
		/// </summary>
		public List<string> TargetCompetencyFramework{ get; set; }

		/// <summary>
		/// Textual description of the methods used to evaluate an assessment, learning opportunity, process or verificaiton service for validity or reliability.
		/// </summary>
		public string VerificationMethodDescription { get; set; }
		/// <summary>
		/// Alternately use a LanguageMap
		/// </summary>
		public LanguageMap VerificationMethodDescription_Map { get; set; } = new LanguageMap();
		/// <summary>
		/// Jurisdiction Profile
		/// Geo-political information about applicable geographic areas and their exceptions.
		/// <see cref="https://credreg.net/ctdl/terms/JurisdictionProfile"/>
		/// </summary>
		public List<Jurisdiction> Jurisdiction { get; set; } = new List<Jurisdiction>();
		//public List<GeoCoordinates> Region { get; set; }

		/// <summary>
		/// Data Collection Method Type
		/// Type of method by which the data was collected.
		/// Concept
		/// CER Target Scheme:	qdata:CollectionMethod
		/// <see cref="https://credreg.net/qdata/terms/CollectionMethod"/>
		/// collectionMethod:AdministrativeRecordMatching
		/// collectionMethod:CredentialHolderReporting 
		/// collectionMethod:CredentialHolderSurvey 
		/// collectionMethod:SupplementalMethod 
		/// collectionMethod:SupplementalSource
		/// </summary>
		public List<string> DataCollectionMethodType { get; set; } = new List<string>();
	}
}




using System.Collections.Generic;
//using System.Text.Json;
//using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
	/// <summary>
	/// Competency class
	/// </summary>
	public class Competency //: JsonLDDocument
	{
		//required": [ "@type", "@id", "ceasn:competencyText", "ceasn:inLanguage", "ceasn:isPartOf", "ceterms:ctid" ]

		//[STJ.JsonIgnore]
		//public static string classType = "ceasn:Competency";
		public Competency()
		{
			//Type = classType;
		}

		[JsonProperty( "@type" )]
		public string Type { get; set; } = "ceasn:Competency";

		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }

		[JsonProperty( "ceterms:ctid" )]
		public string CTID { get; set; }

		[JsonProperty( "ceasn:publicationStatusType" )]
		public string publicationStatusType { get; set; }

		/// <summary>
		/// Enduring attributes of the individual that influence performance are embodied either directly or indirectly in this resource.
		/// The abilityEmbodied property may referenced a defined ability in an ontology such as O*NET or an existing competency defined in a competency framework.
		/// URI
		/// </summary>
		[JsonProperty( "ceasn:abilityEmbodied" )]
		public List<string> abilityEmbodied { get; set; }
		/// <summary>
		/// Competency uris
		/// </summary>
		[JsonProperty( "ceasn:alignFrom" )]
		public List<string> alignFrom { get; set; } 

		/// <summary>
		/// Competency uris
		/// </summary>
		[JsonProperty( "ceasn:alignTo" )]
		public List<string> alignTo { get; set; } 

		[JsonProperty( "ceasn:altCodedNotation" )]
		public List<string> altCodedNotation { get; set; } 

		[JsonProperty( "ceasn:author" )]
		public string author { get; set; } 

		[JsonProperty( "ceasn:codedNotation" )]
		public string codedNotation { get; set; }


		[JsonProperty( "ceasn:comment" )]
		public LanguageMapList comment { get; set; }


		[JsonProperty( "ceasn:competencyCategory" )]
		public LanguageMap competencyCategory { get; set; }

		[JsonProperty( "ceasn:competencyText" )]
		public LanguageMap competencyText { get; set; }

		[JsonProperty( "ceasn:competencyLabel" )]
		public LanguageMap competencyLabel { get; set; }

		[JsonProperty( "ceasn:complexityLevel" )]
		public List<string> complexityLevel { get; set; } 
		//public List<ProficiencyScale> complexityLevel { get; set; } = new List<ProficiencyScale>();

		[JsonProperty( "ceasn:comprisedOf" )]
		public List<string> comprisedOf { get; set; } 

		[JsonProperty( "ceasn:conceptKeyword" )]
		public LanguageMapList conceptKeyword { get; set; }

		[JsonProperty( "ceasn:conceptTerm" )]
		public List<string> conceptTerm { get; set; } 

		[JsonProperty( "ceasn:creator" )]
		public List<string> creator { get; set; } 


		[JsonProperty( "ceasn:dateCreated" )]
		public string dateCreated { get; set; }


		[JsonProperty( "ceasn:dateModified" )]
		public string dateModified { get; set; }
		/// <summary>
		/// A version of the entity being referenced that has been modified in meaning through editing, extension or refinement.
		/// 23-03-22 - change to a list, sigh
		/// </summary>
		[JsonProperty( "ceasn:derivedFrom" )]
        public object derivedFrom { get; set; }
        //public List<string> derivedFrom { get; set; }

        /// <summary>
        /// Education Level Type
        /// Concept URI
        /// </summary>
        [JsonProperty( "ceasn:educationLevelType" )]
		//public List<string> educationLevelType { get; set; }
		public List<string> educationLevelType { get; set; }


		[JsonProperty( "ceasn:hasChild" )]
		public List<string> hasChild { get; set; } 

		[JsonProperty( "ceasn:identifier" )]
		public List<string> identifier { get; set; } 


		/// <summary>
		/// Competency deduced or arrive at by reasoning on the competency being described.
		/// List of URIs (CTIDs recommended) to competencies
		/// </summary>
		[JsonProperty( "ceasn:inferredCompetency" )]
		public List<string> inferredCompetency { get; set; }


		/// <summary>
		/// Is Child Of
		/// The referenced competency is higher in some arbitrary hierarchy than this competency.
		/// List of URIs (CTIDs recommended) to competenciesenvironment.
		/// </summary>
		[JsonProperty( "ceasn:isChildOf" )]
		public List<string> isChildOf { get; set; }

		/// <summary>
		/// URI to the framework that this competency is part of. 
		/// Will not be present for a member of a collection.
		/// </summary>
		[JsonProperty( "ceasn:isPartOf" )]
		public string isPartOf { get; set; }
		//public List<string> isPartOf { get; set; } 

		[JsonProperty( "ceasn:isTopChildOf" )]
		public string isTopChildOf { get; set; }

		/// <summary>
		/// A related competency of which this competency is a version, edition, or adaptation.
		/// </summary>
		[JsonProperty( "ceasn:isVersionOf" )]
		public string isVersionOf { get; set; }

		/// <summary>
		/// Concept in a ProgressionModel concept scheme
		/// </summary>
		[JsonProperty( PropertyName = "asn:hasProgressionLevel" )]
		public string HasProgressionLevel { get; set; }

        [JsonProperty( PropertyName = "ceterms:keyword" )]
        public LanguageMapList Keyword { get; set; }

        [JsonProperty( "ceasn:listID" )]
		public string ListID { get; set; }

		[JsonProperty( "ceasn:localSubject" )]
		//public LanguageMapList LocalSubject { get; set; }
		public object localSubject { get; set; }
		#region alignments
		/// <summary>
		/// Competency uris
		/// </summary>
		[JsonProperty( "ceasn:broadAlignment" )]
		public List<string> broadAlignment { get; set; }

		/// <summary>
		/// A relationship between this competency and a competency in a separate competency framework.
		/// Range Includes: ceasn:Competency
		/// </summary>
		[JsonProperty( "ceasn:crossSubjectReference" )]
		public List<string> crossSubjectReference { get; set; }

		/// <summary>
		/// Competency uris
		/// This should be a list of URIs. The data type is object to handle receiving a string, which will be converted to a list of strings
		/// </summary>
		[JsonProperty( "ceasn:exactAlignment" )]
		//public List<string> exactAlignment { get; set; } 
		public object exactAlignment { get; set; }

		/// <summary>
		/// Competency uris
		/// </summary>
		[JsonProperty( "ceasn:majorAlignment" )]
		public List<string> majorAlignment { get; set; } 

		/// <summary>
		/// Competency uris
		/// </summary>
		[JsonProperty( "ceasn:minorAlignment" )]
		public List<string> minorAlignment { get; set; } 

		/// <summary>
		/// Competency uris
		/// </summary>
		[JsonProperty( "ceasn:narrowAlignment" )]
		public List<string> narrowAlignment { get; set; } 

		/// <summary>
		/// This competency is a prerequisite to the referenced competency.
		/// Uri to a competency
		/// </summary>
		[JsonProperty( "ceasn:prerequisiteAlignment" )]
		public List<string> prerequisiteAlignment { get; set; } 
		#endregion

		/// <summary>
		/// Body of information embodied either directly or indirectly in this competency.
		/// URI
		/// </summary>
		[JsonProperty( "ceasn:knowledgeEmbodied" )]
		public List<string> KnowledgeEmbodied { get; set; } 

		/// <summary>
		/// Specifically defined piece of work embodied either directly or indirectly in this competency.
		/// URI
		/// </summary>
		[JsonProperty( "ceasn:taskEmbodied" )]
		public List<string> TaskEmbodied { get; set; }

		[JsonProperty( "ceterms:occupationType" )]
		public List<CredentialAlignmentObject> OccupationType { get; set; }

		[JsonProperty( "ceterms:industryType" )]
		public List<CredentialAlignmentObject> IndustryType { get; set; }

		[JsonProperty( PropertyName = "ceterms:instructionalProgramType" )]
		public List<CredentialAlignmentObject> InstructionalProgramType { get; set; } = new List<CredentialAlignmentObject>();


		[JsonProperty( "ceterms:hasWorkforceDemand" )]
		public List<string> HasWorkforceDemand { get; set; }


		/// <summary>
		/// An asserted measurement of the weight, degree, percent, or strength of a recommendation, requirement, or comparison.
		/// Float
		/// </summary>
		[JsonProperty( "ceasn:weight" )]
		public string Weight { get; set; }


		/// <summary>
		/// Task related to this resource.
		/// </summary>
		[JsonProperty( "ceterms:hasTask" )]
		public List<string> HasTask { get; set; }

		#region Navy terms
		/// <summary>
		/// Has Source Identifier
		///  A collection of identifiers related to this resource.
		/// </summary>
		[JsonProperty( "navy:hasSourceIdentifier" )]
		public List<string> HasSourceIdentifier { get; set; }

		//[JsonProperty( "navy:hasMaintenanceTask" )]
		//public List<string> HasMaintenanceTask { get; set; }


		//[JsonProperty( "navy:hasTrainingTask" )]
		//public List<string> HasTrainingTask { get; set; }
		#endregion

		//New 2021-09-30

		/// <summary>
		/// Type of condition in the physical work performance environment that entails risk exposures requiring mitigating processes; select from an existing enumeration of such types.
		/// Collection only
		/// </summary>
		[JsonProperty( "ceasn:environmentalHazardType" )]
		public List<string> EnvironmentalHazardType { get; set; }

		//only for collection context
		[JsonProperty( "ceasn:inLanguage" )]
		public List<string> InLanguage { get; set; }

		///// <summary>
		///// Collection to which this resource belongs.
		///// This is really an inverse property so would not be published with the competency
		///// Collection ONLY
		///// </summary>
		//[JsonProperty( "ceasn:isMemberOf" )]
		//public string IsMemberOf { get; set; }

		/// <summary>
		/// A legal document giving official permission to do something with this resource.
		/// Collections only
		/// </summary>
		[JsonProperty( "ceasn:license" )]
		public string License { get; set; }

		/// <summary>
		/// Type of required or expected performance level for a resource; 
		/// There is no concept scheme for this. Must allow any URI.
		/// </summary>
		[JsonProperty( "ceasn:performanceLevelType" )]
		public List<string> PerformanceLevelType { get; set; }

		/// <summary>
		/// Type of physical activity required or expected in performance;
		/// There is no concept scheme for this. Must allow any URI.
		/// </summary>
		[JsonProperty( "ceasn:physicalCapabilityType" )]
		public List<string> PhysicalCapabilityType { get; set; }

		/// <summary>
		/// Type of required or expected sensory capability; 
		/// There is no concept scheme for this. Must allow any URI.
		/// </summary>
		[JsonProperty( "ceasn:sensoryCapabilityType" )]
		public List<string> SensoryCapabilityType { get; set; }

		/// <summary>
		/// Indicates whether correlators should or should not assign the competency during correlation.
		/// </summary>
		[JsonProperty( "ceasn:shouldIndex" )]
		public bool? ShouldIndex { get; set; }


		/// <summary>
		/// Cognitive, affective, and psychomotor skills directly or indirectly embodied in this competency.
		/// URI
		/// </summary>
		[JsonProperty( "ceasn:skillEmbodied" )]
		public List<string> SkillEmbodied { get; set; }


		/// <summary>
		/// Human-readable information resource other than a competency framework from which this competency was generated or derived by humans or machines.
		/// URI
		/// </summary>
		[JsonProperty( "ceasn:sourceDocumentation" )]
		public List<string> SourceDocumentation { get; set; }

		/// <summary>
		/// Aspects of the referenced Competency Framework provide some justification that the resource being described is useful.
		/// </summary>
		[JsonProperty( "ceasn:substantiatingCompetencyFramework" )]
		public List<string> SubstantiatingCompetencyFramework { get; set; }

		/// <summary>
		/// Aspects of the referenced Credential provide some justification that the resource being described is useful.
		/// </summary>
		[JsonProperty( "ceasn:substantiatingCredential" )]
		public List<string> SubstantiatingCredential { get; set; }

		/// <summary>
		/// Aspects of the referenced Job provide some justification that the resource being described is useful.
		/// </summary>
		[JsonProperty( "ceasn:substantiatingJob" )]
		public List<string> SubstantiatingJob { get; set; }

		/// <summary>
		/// Aspects of the referenced Occupation provide some justification that the resource being described is useful.
		/// </summary>
		[JsonProperty( "ceasn:substantiatingOccupation" )]
		public List<string> SubstantiatingOccupation { get; set; }

		/// <summary>
		/// Aspects of the referenced Organization provide some justification that the resource being described is useful.
		/// </summary>
		[JsonProperty( "ceasn:substantiatingOrganization" )]
		public List<string> SubstantiatingOrganization { get; set; }

		/// <summary>
		/// Aspects of the referenced resource provide some justification that the resource being described is useful.
		/// </summary>
		[JsonProperty( "ceasn:substantiatingResource" )]
		public List<string> SubstantiatingResource { get; set; }

		/// <summary>
		/// Referenced Task attests to some level of achievement/mastery of the competency being described.
		/// </summary>
		[JsonProperty( "ceasn:substantiatingTask" )]
		public List<string> SubstantiatingTask { get; set; }

		/// <summary>
		/// Referenced Workrole attests to some level of achievement/mastery of the competency being described.
		/// </summary>
		[JsonProperty( "ceasn:substantiatingWorkrole" )]
		public List<string> SubstantiatingWorkrole { get; set; }




		//--------------- helpers ---------------------------------------
		/// <summary>
		/// CIP List is a helper when publishing from a graph. It will not be published
		/// </summary>
		[JsonProperty( "cipList" )]
		public List<string> CIPList { get; set; }
		/// <summary>
		/// SOC List is a helper when publishing from a graph. It will not be published
		/// </summary>
		[JsonProperty( "socList" )]
		public List<string> SOCList { get; set; }

		/// NAICS List is a helper when publishing from a graph. It will not be published
		[JsonProperty( "naicsList" )]
		public List<string> NaicsList { get; set; }
		//temp??
		/// <summary>
		/// Only used where part of a Collection
		/// </summary>
		[JsonProperty( "ceterms:isMemberOf" )]
		public List<string> isMemberOf { get; set; }

	}
	public class CompetencyPlain : Competency
	{
		
		public CompetencyPlain()
		{
			//Type = classType;
		}


		[JsonProperty( "ceasn:comment" )]
		public new List<string> comment { get; set; } = new List<string>();


		[JsonProperty( "ceasn:competencyCategory" )]
		public new string competencyCategory { get; set; }

		[JsonProperty( "ceasn:competencyText" )]
		public new string competencyText { get; set; }

		[JsonProperty( "ceasn:competencyLabel" )]
		public new string competencyLabel { get; set; }

		[JsonProperty( "ceasn:conceptKeyword" )]
		public new List<string> conceptKeyword { get; set; } = new List<string>();

		[JsonProperty( "ceasn:localSubject" )]
		public new List<string> localSubject { get; set; } = new List<string>();


	}

	///// <summary>
	///// The class of structured profiles describing discrete levels of expertise and performance mastery.
	///// </summary>
	////	public class ProficiencyScale
	////	{
	////      "ceasn:ProficiencyScale": {
	////      "type": "object",
	////      "properties": { "@type": { "enum": [ "ceasn:ProficiencyScale" ]
	////	}
	////},
	////      "required": [ "@type" ],
	////      "additionalProperties": false
	////    },
	////        [JsonProperty( "@id" )]
	////        public string CtdlId { get; set; }
	////    }


}

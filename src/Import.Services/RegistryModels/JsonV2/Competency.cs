using System.Collections.Generic;
//using System.Text.Json;
//using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
	
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
		public string Ctid { get; set; }

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
		public List<string> author { get; set; } 

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

		/// <summary>
		/// A relationship between this competency and a competency in a separate competency framework.
		/// Competency uris
		/// </summary>
		//[JsonProperty( "ceasn:crossSubjectReference" )]
		//public List<string> crossSubjectReference { get; set; } 

		[JsonProperty( "ceasn:dateCreated" )]
		public string dateCreated { get; set; }


		[JsonProperty( "ceasn:dateModified" )]
		public string dateModified { get; set; }
		/// <summary>
		/// The URI of a competency from which this competency has been derived.
		/// </summary>
		[JsonProperty( "ceasn:derivedFrom" )]
		public string derivedFrom { get; set; }
		//public List<string> derivedFrom { get; set; } 

		[JsonProperty( "ceasn:educationLevelType" )]
		public List<string> educationLevelType { get; set; } 


		[JsonProperty( "ceasn:hasChild" )]
		public List<string> hasChild { get; set; } 

		[JsonProperty( "ceasn:identifier" )]
		public List<string> identifier { get; set; } 

		[JsonProperty( "ceasn:isChildOf" )]
		public List<string> isChildOf { get; set; } 

		[JsonProperty( "ceasn:isPartOf" )]
		public string isPartOf { get; set; }
		//public List<string> isPartOf { get; set; } 

		[JsonProperty( "ceasn:isTopChildOf" )]
		public string isTopChildOf { get; set; }
		//public List<string> isTopChildOf { get; set; } 

		/// <summary>
		/// Competency uri
		/// </summary>
		[JsonProperty( "ceasn:isVersionOf" )]
		public string isVersionOf { get; set; }

		[JsonProperty( "ceasn:listID" )]
		public string listID { get; set; }

		//[JsonProperty( "ceasn:localSubject" )]
		//public LanguageMapList localSubject { get; set; }

		#region alignments
		/// <summary>
		/// Competency uris
		/// </summary>
		[JsonProperty( "ceasn:broadAlignment" )]
		public List<string> broadAlignment { get; set; } 

		/// <summary>
		/// Resource being described includes, comprehends or encompass, in whole or in part, the meaning, nature or importance of the resource being referenced.
		/// Range Includes: ceasn:Competency, ceasn:Concept
		/// </summary>
		[JsonProperty( "ceasn:encompasses" )]
		public List<string> encompasses { get; set; } 

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
		/// Cognitive, affective, and psychomotor skills directly or indirectly embodied in this competency.
		/// URI
		/// </summary>
		[JsonProperty( "ceasn:skillEmbodied" )]
		public List<string> skillEmbodied { get; set; } 

		/// <summary>
		/// Body of information embodied either directly or indirectly in this competency.
		/// URI
		/// </summary>
		[JsonProperty( "ceasn:knowledgeEmbodied" )]
		public List<string> knowledgeEmbodied { get; set; } 

		/// <summary>
		/// Specifically defined piece of work embodied either directly or indirectly in this competency.
		/// URI
		/// </summary>
		[JsonProperty( "ceasn:taskEmbodied" )]
		public List<string> taskEmbodied { get; set; } 

		/// <summary>
		/// An asserted measurement of the weight, degree, percent, or strength of a recommendation, requirement, or comparison.
		/// Float
		/// </summary>
		[JsonProperty( "ceasn:weight" )]
		public string weight { get; set; }

		/// <summary>
		/// Has Source Identifier
		///  A collection of identifiers related to this resource.
		/// </summary>
		[JsonProperty( "navy:hasSourceIdentifier" )]
		public List<string> hasSourceIdentifier { get; set; }

		[JsonProperty( "navy:hasMaintenanceTask" )]
		public List<string> hasMaintenanceTask { get; set; }


		[JsonProperty( "navy:hasTrainingTask" )]
		public List<string> hasTrainingTask { get; set; }
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

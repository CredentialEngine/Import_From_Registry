using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RA.Models.JsonV2
{

    /// <summary>
    /// Variation of Competency that uses string instead of IdProperty
    /// </summary>
  //  public class CompetencyInput 
  //  {
  //      //required": [ "@type", "@id", "ceasn:competencyText", "ceasn:inLanguage", "ceasn:isPartOf", "ceterms:ctid" ]

  //      [JsonIgnore]
  //      public static string classType = "ceasn:Competency";
  //      public CompetencyInput()
  //      {
  //          Type = classType;
  //      }

  //      [JsonProperty( "@type" )]
  //      public string Type { get; set; }

  //      [JsonProperty( "@id" )]
  //      public string CtdlId { get; set; }

  //      [JsonProperty( PropertyName = "ceterms:ctid" )]
  //      public string Ctid { get; set; }

  //      /// <summary>
  //      /// Competency uris
  //      /// </summary>
  //      [JsonProperty( PropertyName = "ceasn:alignFrom" )]
  //      public List<string> alignFrom { get; set; } = new List<string>();

  //      /// <summary>
  //      /// Competency uris
  //      /// </summary>
  //      [JsonProperty( PropertyName = "ceasn:alignTo" )]
  //      public List<string> alignTo { get; set; } = new List<string>();

  //      [JsonProperty( PropertyName = "ceasn:altCodedNotation" )]
  //      public List<string> altCodedNotation { get; set; } = new List<string>();

  //      [JsonProperty( "ceasn:author" )]
  //      public List<string> author { get; set; } = new List<string>();

  //      [JsonProperty( PropertyName = "ceasn:codedNotation" )]
  //      public string codedNotation { get; set; }


  //      [JsonProperty( PropertyName = "ceasn:comment" )]
  //      public LanguageMapList comment { get; set; }


  //      [JsonProperty( PropertyName = "ceasn:competencyCategory" )]
  //      public LanguageMap competencyCategory { get; set; }

  //      [JsonProperty( PropertyName = "ceasn:competencyText" )]
  //      public LanguageMap competencyText { get; set; }

  //      [JsonProperty( PropertyName = "ceasn:complexityLevel" )]
  //      public List<ProficiencyScale> complexityLevel { get; set; } = new List<ProficiencyScale>();

  //      [ JsonProperty( PropertyName = "ceasn:comprisedOf" )]
  //      public List<string> comprisedOf { get; set; } = new List<string>();

  //      [JsonProperty( PropertyName = "ceasn:conceptKeyword" )]
		//public LanguageMapList conceptKeyword { get; set; }

		//[JsonProperty( PropertyName = "ceasn:conceptTerm" )]
  //      public List<string> conceptTerm { get; set; } = new List<string>();

  //      [JsonProperty( PropertyName = "ceasn:creator" )]
  //      public List<string> creator { get; set; } = new List<string>();

  //      /// <summary>
  //      /// A relationship between this competency and a competency in a separate competency framework.
  //      /// Competency uris
  //      /// </summary>
  //      [JsonProperty( PropertyName = "ceasn:crossSubjectReference" )]
  //      public List<string> crossSubjectReference { get; set; } = new List<string>();

  //      [JsonProperty( PropertyName = "ceasn:dateCreated" )]
  //      public string dateCreated { get; set; }

  //      /// <summary>
  //      /// The URI of a competency from which this competency has been derived.
  //      /// </summary>
  //      [JsonProperty( PropertyName = "ceasn:derivedFrom" )]
  //      public List<string> derivedFrom { get; set; } = new List<string>();

  //      [JsonProperty( PropertyName = "ceasn:educationLevelType" )]
  //      public List<string> educationLevelType { get; set; } = new List<string>();


  //      [JsonProperty( PropertyName = "ceasn:hasChild" )]
  //      public List<string> hasChild { get; set; } = new List<string>();

  //      [JsonProperty( PropertyName = "ceasn:identifier" )]
  //      public List<string> identifier { get; set; } = new List<string>();

  //      [JsonProperty( PropertyName = "ceasn:inLanguage" )]
  //      public List<string> inLanguage { get; set; } = new List<string>();

  //      [JsonProperty( PropertyName = "ceasn:isChildOf" )]
  //      public List<string> isChildOf { get; set; } = new List<string>();

  //      [JsonProperty( PropertyName = "ceasn:isPartOf" )]
		//public string isPartOf { get; set; }
		////public List<string> isPartOf { get; set; } = new List<string>();

		//[JsonProperty( PropertyName = "ceasn:isTopChildOf" )]
		//public List<string> isTopChildOf { get; set; } = new List<string>();

		///// <summary>
		///// Competency uri
		///// </summary>
		//[JsonProperty( PropertyName = "ceasn:isVersionOf" )]
		//public string isVersionOf { get; set; }

  //      [JsonProperty( PropertyName = "ceasn:listID" )]
  //      public string listID { get; set; }

  //      [JsonProperty( PropertyName = "ceasn:localSubject" )]
  //      public LanguageMapList localSubject { get; set; }

  //      #region alignments
  //      /// <summary>
  //      /// Competency uris
  //      /// </summary>
  //      [JsonProperty( PropertyName = "ceasn:broadAlignment" )]
  //      public List<string> broadAlignment { get; set; } = new List<string>();

  //      /// <summary>
  //      /// Competency uris
  //      /// </summary>
  //      [JsonProperty( PropertyName = "ceasn:exactAlignment" )]
  //      public List<string> exactAlignment { get; set; } = new List<string>();

  //      /// <summary>
  //      /// Competency uris
  //      /// </summary>
  //      [JsonProperty( PropertyName = "ceasn:majorAlignment" )]
  //      public List<string> majorAlignment { get; set; } = new List<string>();

  //      /// <summary>
  //      /// Competency uris
  //      /// </summary>
  //      [JsonProperty( PropertyName = "ceasn:minorAlignment" )]
  //      public List<string> minorAlignment { get; set; } = new List<string>();

  //      /// <summary>
  //      /// Competency uris
  //      /// </summary>
  //      [JsonProperty( PropertyName = "ceasn:narrowAlignment" )]
  //      public List<string> narrowAlignment { get; set; } = new List<string>();

  //      /// <summary>
  //      /// This competency is a prerequisite to the referenced competency.
  //      /// Uri to a competency
  //      /// </summary>
  //      [JsonProperty( PropertyName = "ceasn:prerequisiteAlignment" )]
  //      public List<string> prerequisiteAlignment { get; set; } = new List<string>();
  //      #endregion

  //      /// <summary>
  //      /// Cognitive, affective, and psychomotor skills directly or indirectly embodied in this competency.
  //      /// URI
  //      /// </summary>
  //      [JsonProperty( PropertyName = "ceasn:skillEmbodied" )]
  //      public List<string> skillEmbodied { get; set; } = new List<string>();

  //      /// <summary>
  //      /// An asserted measurement of the weight, degree, percent, or strength of a recommendation, requirement, or comparison.
  //      /// Float
  //      /// </summary>
  //      [JsonProperty( PropertyName = "ceasn:weight" )]
  //      public decimal weight { get; set; }

  //  }


    public class Competency : JsonLDDocument
    {
        //required": [ "@type", "@id", "ceasn:competencyText", "ceasn:inLanguage", "ceasn:isPartOf", "ceterms:ctid" ]

        [JsonIgnore]
        public static string classType = "ceasn:Competency";
        public Competency()
        {
            Type = classType;
        }

		[JsonProperty( "@type" )]
		public string Type { get; set; }

		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }

		[JsonProperty( PropertyName = "ceterms:ctid" )]
		public string Ctid { get; set; }

		/// <summary>
		/// Competency uris
		/// </summary>
		[JsonProperty( PropertyName = "ceasn:alignFrom" )]
		public List<string> alignFrom { get; set; } = new List<string>();

		/// <summary>
		/// Competency uris
		/// </summary>
		[JsonProperty( PropertyName = "ceasn:alignTo" )]
		public List<string> alignTo { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "ceasn:altCodedNotation" )]
		public List<string> altCodedNotation { get; set; } = new List<string>();

		[JsonProperty( "ceasn:author" )]
		public List<string> author { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "ceasn:codedNotation" )]
		public string codedNotation { get; set; }


		[JsonProperty( PropertyName = "ceasn:comment" )]
		public LanguageMapList comment { get; set; }


		[JsonProperty( PropertyName = "ceasn:competencyCategory" )]
		public LanguageMap competencyCategory { get; set; }

		[JsonProperty( PropertyName = "ceasn:competencyText" )]
		public LanguageMap competencyText { get; set; }

		[JsonProperty( PropertyName = "ceasn:complexityLevel" )]
		public List<string> complexityLevel { get; set; } = new List<string>();
		//public List<ProficiencyScale> complexityLevel { get; set; } = new List<ProficiencyScale>();

		[JsonProperty( PropertyName = "ceasn:comprisedOf" )]
		public List<string> comprisedOf { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "ceasn:conceptKeyword" )]
		public LanguageMapList conceptKeyword { get; set; }

		[JsonProperty( PropertyName = "ceasn:conceptTerm" )]
		public List<string> conceptTerm { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "ceasn:creator" )]
		public List<string> creator { get; set; } = new List<string>();

		/// <summary>
		/// A relationship between this competency and a competency in a separate competency framework.
		/// Competency uris
		/// </summary>
		//[JsonProperty( PropertyName = "ceasn:crossSubjectReference" )]
		//public List<string> crossSubjectReference { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "ceasn:dateCreated" )]
		public string dateCreated { get; set; }


		[JsonProperty( PropertyName = "ceasn:dateModified" )]
		public string dateModified { get; set; }
		/// <summary>
		/// The URI of a competency from which this competency has been derived.
		/// </summary>
		[JsonProperty( PropertyName = "ceasn:derivedFrom" )]
		public string derivedFrom { get; set; }
		//public List<string> derivedFrom { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "ceasn:educationLevelType" )]
		public List<string> educationLevelType { get; set; } = new List<string>();


		[JsonProperty( PropertyName = "ceasn:hasChild" )]
		public List<string> hasChild { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "ceasn:identifier" )]
		public List<string> identifier { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "ceasn:isChildOf" )]
		public List<string> isChildOf { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "ceasn:isPartOf" )]
		public string isPartOf { get; set; }
		//public List<string> isPartOf { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "ceasn:isTopChildOf" )]
		public string isTopChildOf { get; set; }
		//public List<string> isTopChildOf { get; set; } = new List<string>();

		/// <summary>
		/// Competency uri
		/// </summary>
		[JsonProperty( PropertyName = "ceasn:isVersionOf" )]
		public string isVersionOf { get; set; }

		[JsonProperty( PropertyName = "ceasn:listID" )]
		public string listID { get; set; }

		//[JsonProperty( PropertyName = "ceasn:localSubject" )]
		//public LanguageMapList localSubject { get; set; }

		#region alignments
		/// <summary>
		/// Competency uris
		/// </summary>
		[JsonProperty( PropertyName = "ceasn:broadAlignment" )]
		public List<string> broadAlignment { get; set; } = new List<string>();

		/// <summary>
		/// Resource being described includes, comprehends or encompass, in whole or in part, the meaning, nature or importance of the resource being referenced.
		/// Range Includes: ceasn:Competency, ceasn:Concept
		/// </summary>
		[JsonProperty( PropertyName = "ceasn:encompasses" )]
		public List<string> encompasses { get; set; } = new List<string>();

		/// <summary>
		/// Competency uris
		/// </summary>
		[JsonProperty( PropertyName = "ceasn:exactAlignment" )]
		public List<string> exactAlignment { get; set; } = new List<string>();

		/// <summary>
		/// Competency uris
		/// </summary>
		[JsonProperty( PropertyName = "ceasn:majorAlignment" )]
		public List<string> majorAlignment { get; set; } = new List<string>();

		/// <summary>
		/// Competency uris
		/// </summary>
		[JsonProperty( PropertyName = "ceasn:minorAlignment" )]
		public List<string> minorAlignment { get; set; } = new List<string>();

		/// <summary>
		/// Competency uris
		/// </summary>
		[JsonProperty( PropertyName = "ceasn:narrowAlignment" )]
		public List<string> narrowAlignment { get; set; } = new List<string>();

		/// <summary>
		/// This competency is a prerequisite to the referenced competency.
		/// Uri to a competency
		/// </summary>
		[JsonProperty( PropertyName = "ceasn:prerequisiteAlignment" )]
		public List<string> prerequisiteAlignment { get; set; } = new List<string>();
		#endregion

		/// <summary>
		/// Cognitive, affective, and psychomotor skills directly or indirectly embodied in this competency.
		/// URI
		/// </summary>
		[JsonProperty( PropertyName = "ceasn:skillEmbodied" )]
		public List<string> skillEmbodied { get; set; } = new List<string>();

		/// <summary>
		/// Body of information embodied either directly or indirectly in this competency.
		/// URI
		/// </summary>
		[JsonProperty( PropertyName = "ceasn:knowledgeEmbodied" )]
		public List<string> knowledgeEmbodied { get; set; } = new List<string>();
		
		/// <summary>
		/// Specifically defined piece of work embodied either directly or indirectly in this competency.
		/// URI
		/// </summary>
		[JsonProperty( PropertyName = "ceasn:taskEmbodied" )]
		public List<string> taskEmbodied { get; set; } = new List<string>();
		
		/// <summary>
		/// An asserted measurement of the weight, degree, percent, or strength of a recommendation, requirement, or comparison.
		/// Float
		/// </summary>
		[JsonProperty( PropertyName = "ceasn:weight" )]
		public string weight { get; set; }

		/// <summary>
		/// Has Source Identifier
		///  A collection of identifiers related to this resource.
		/// </summary>
		[JsonProperty( PropertyName = "navy:hasSourceIdentifier" )]
		public List<string> hasSourceIdentifier { get; set; } 

	}

    /// <summary>
    /// The class of structured profiles describing discrete levels of expertise and performance mastery.
    /// </summary>
    public class ProficiencyScale
    {
//      "ceasn:ProficiencyScale": {
//      "type": "object",
//      "properties": { "@type": { "enum": [ "ceasn:ProficiencyScale" ]
//    }
//      },
//      "required": [ "@type" ],
//      "additionalProperties": false
//    },
        [JsonProperty( "@id" )]
        public string CtdlId { get; set; }
    }


}

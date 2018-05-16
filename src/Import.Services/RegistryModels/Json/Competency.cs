using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RA.Models.Json
{

    /// <summary>
    /// Variation of Competency that uses string instead of IdProperty
    /// </summary>
    public class CompetencyInput 
    {
        //required": [ "@type", "@id", "ceasn:competencyText", "ceasn:inLanguage", "ceasn:isPartOf", "ceterms:ctid" ]

        [JsonIgnore]
        public static string classType = "ceasn:Competency";
        public CompetencyInput()
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
        public LanguageMap comment { get; set; }


        [JsonProperty( PropertyName = "ceasn:competencyCategory" )]
        public List<LanguageMap> competencyCategory { get; set; }



        [JsonProperty( PropertyName = "ceasn:competencyText" )]
        public LanguageMap competencyText { get; set; }

        [JsonProperty( PropertyName = "ceasn:complexityLevel" )]
        public List<ProficiencyScale> complexityLevel { get; set; } = new List<ProficiencyScale>();

        [ JsonProperty( PropertyName = "ceasn:comprisedOf" )]
        public List<string> comprisedOf { get; set; } = new List<string>();

        [JsonProperty( PropertyName = "ceasn:conceptKeyword" )]
        public List<LanguageMap> conceptKeyword { get; set; }

        [JsonProperty( PropertyName = "ceasn:conceptTerm" )]
        public List<string> conceptTerm { get; set; } = new List<string>();



        [JsonProperty( PropertyName = "ceasn:creator" )]
        public List<string> creator { get; set; } = new List<string>();

        /// <summary>
        /// A relationship between this competency and a competency in a separate competency framework.
        /// Competency uris
        /// </summary>
        [JsonProperty( PropertyName = "ceasn:crossSubjectReference" )]
        public List<string> crossSubjectReference { get; set; } = new List<string>();

        [JsonProperty( PropertyName = "ceasn:dateCreated" )]
        public string dateCreated { get; set; }

        /// <summary>
        /// The URI of a competency from which this competency has been derived.
        /// </summary>
        [JsonProperty( PropertyName = "ceasn:derivedFrom" )]
        public List<string> derivedFrom { get; set; } = new List<string>();

        [JsonProperty( PropertyName = "ceasn:educationLevelType" )]
        public List<string> educationLevelType { get; set; } = new List<string>();

       


        /// <summary>
        /// NOTE: temporary, will be changed to exactAlignment
        /// </summary>
        [JsonProperty( PropertyName = "ceasn:exactMatch" )]
        public List<string> exactMatch
        {
            get { return exactAlignment; }
            set { exactAlignment = value; }
        }

        [JsonProperty( PropertyName = "ceasn:hasChild" )]
        public List<string> hasChild { get; set; } = new List<string>();

        [JsonProperty( PropertyName = "ceasn:identifier" )]
        public List<string> identifier { get; set; } = new List<string>();

        [JsonProperty( PropertyName = "ceasn:inLanguage" )]
        public List<string> inLanguage { get; set; } = new List<string>();

        [JsonProperty( PropertyName = "ceasn:isChildOf" )]
        public List<string> isChildOf { get; set; } = new List<string>();

        [JsonProperty( PropertyName = "ceasn:isPartOf" )]
        public List<string> isPartOf { get; set; } = new List<string>();

        /// <summary>
        /// Competency uri
        /// </summary>
        [JsonProperty( PropertyName = "ceasn:isVersionOf" )]
        public string isVersionOf { get; set; }

        [JsonProperty( PropertyName = "ceasn:listID" )]
        public string listID { get; set; }

        [JsonProperty( PropertyName = "ceasn:localSubject" )]
        public List<LanguageMap> localSubject { get; set; }

        #region alignments
        /// <summary>
        /// Competency uris
        /// </summary>
        [JsonProperty( PropertyName = "ceasn:broadAlignment" )]
        public List<string> broadAlignment { get; set; } = new List<string>();

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
        /// An asserted measurement of the weight, degree, percent, or strength of a recommendation, requirement, or comparison.
        /// Float
        /// </summary>
        [JsonProperty( PropertyName = "ceasn:weight" )]
        public decimal weight { get; set; }

    }


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

        [JsonProperty( PropertyName = "ceasn:alignFrom" )]
        public List<IdProperty> alignFrom { get; set; } = new List<IdProperty>();

        [ JsonProperty( PropertyName = "ceasn:alignTo" )]
        public List<IdProperty> alignTo { get; set; } = new List<IdProperty>();

        [JsonProperty( PropertyName = "ceasn:altCodedNotation" )]
        public List<string> altCodedNotation { get; set; } = new List<string>();

        [JsonProperty( "@author" )]
        public List<string> author { get; set; } = new List<string>();

        [JsonProperty( PropertyName = "ceasn:broadAlignment" )]
        public List<IdProperty> broadAlignment { get; set; } = new List<IdProperty>();

        [JsonProperty( PropertyName = "ceasn:codedNotation" )]
        public string codedNotation { get; set; }


        [JsonProperty( PropertyName = "ceasn:comment" )]
        public LanguageMap comment { get; set; }


        [JsonProperty( PropertyName = "ceasn:competencyCategory" )]
        public List<LanguageMap> competencyCategory { get; set; }
        


        [JsonProperty( PropertyName = "ceasn:competencyText" )]
        public LanguageMap competencyText { get; set; }

        [JsonProperty( PropertyName = "ceasn:complexityLevel" )]
        public List<ProficiencyScale> complexityLevel { get; set; } = new List<ProficiencyScale>();

        [JsonProperty( PropertyName = "ceasn:comprisedOf" )]
        public List<IdProperty> comprisedOf { get; set; } = new List<IdProperty>();

        [JsonProperty( PropertyName = "ceasn:conceptKeyword" )]
        public List<LanguageMap> conceptKeyword { get; set; }

        [JsonProperty( PropertyName = "ceasn:conceptTerm" )]
        public List<IdProperty> conceptTerm { get; set; } = new List<IdProperty>();



        [JsonProperty( PropertyName = "ceasn:creator" )]
        public List<IdProperty> creator { get; set; } = new List<IdProperty>();

        [JsonProperty( PropertyName = "ceasn:crossSubjectReference" )]
        public List<IdProperty> crossSubjectReference { get; set; } = new List<IdProperty>();

        [JsonProperty( PropertyName = "ceasn:dateCreated" )]
        public string dateCreated { get; set; }

        [JsonProperty( PropertyName = "ceasn:derivedFrom" )]
        public List<IdProperty> derivedFrom { get; set; } = new List<IdProperty>();

        [JsonProperty( PropertyName = "ceasn:educationLevelType" )]
        public List<IdProperty> educationLevelType { get; set; } = new List<IdProperty>();

        [JsonProperty( PropertyName = "ceasn:exactAlignment" )]
        public List<IdProperty> exactAlignment { get; set; } = new List<IdProperty>();


        /// <summary>
        /// NOTE: temporary, will be changed to exactAlignment
        /// </summary>
        [JsonProperty( PropertyName = "ceasn:exactMatch" )]
        public List<IdProperty> exactMatch {
            get { return exactAlignment; }
            set { exactAlignment = value; }
        } 

        [JsonProperty( PropertyName = "ceasn:hasChild" )]
        public List<IdProperty> hasChild { get; set; } = new List<IdProperty>();

        [JsonProperty( PropertyName = "ceasn:identifier" )]
        public List<IdProperty> identifier { get; set; } = new List<IdProperty>();

        [JsonProperty( PropertyName = "ceasn:inLanguage" )]
        public List<string> inLanguage { get; set; } = new List<string>();

        [JsonProperty( PropertyName = "ceasn:isChildOf" )]
        public List<IdProperty> isChildOf { get; set; } = new List<IdProperty>();

        [JsonProperty( PropertyName = "ceasn:isPartOf" )]
        public List<IdProperty> isPartOf { get; set; } = new List<IdProperty>();

        [JsonProperty( PropertyName = "ceasn:isVersionOf" )]
        public IdProperty isVersionOf { get; set; }

        [JsonProperty( PropertyName = "ceasn:listID" )]
        public string listID { get; set; }

        [JsonProperty( PropertyName = "ceasn:localSubject" )]
        public List<LanguageMap> localSubject { get; set; }

        [JsonProperty( PropertyName = "ceasn:majorAlignment" )]
        public List<string> majorAlignment { get; set; } = new List<string>();

        [JsonProperty( PropertyName = "ceasn:minorAlignment" )]
        public List<IdProperty> minorAlignment { get; set; } = new List<IdProperty>();

        [JsonProperty( PropertyName = "ceasn:narrowAlignment" )]
        public List<IdProperty> narrowAlignment { get; set; } = new List<IdProperty>();

        [JsonProperty( PropertyName = "ceasn:prerequisiteAlignment" )]
        public List<IdProperty> prerequisiteAlignment { get; set; } = new List<IdProperty>();

        [JsonProperty( PropertyName = "ceasn:skillEmbodied" )]
        public List<IdProperty> skillEmbodied { get; set; } = new List<IdProperty>();


        [JsonProperty( PropertyName = "ceasn:weight" )]
        public string weight { get; set; }

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

    /// <summary>
    /// Language Map is an object holds multiple 
    /// </summary>
    public class LanguageMap
    {
        [JsonProperty( "en" )]
        public string English { get; set; }
        [JsonProperty( "en-us" )]
        public string English_US { get; set; }

        [JsonProperty( "en-gb" )]
        public string English_GB { get; set; }

        [JsonProperty( "es" )]
        public string Spanish { get; set; }

        [JsonProperty( "pl" )]
        public string Polish { get; set; }
        [JsonProperty( "de" )]
        public string German { get; set; }
        [JsonProperty( "ru" )]
        public string Russian { get; set; }
    }
}

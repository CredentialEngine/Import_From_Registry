using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RA.Models.Json
{
    public class CompetencyFrameworksGraph
    {
        [JsonIgnore]
        public static string classType = "ceasn:CompetencyFramework";
        public CompetencyFrameworksGraph()
        {
            Type = classType;
            Context = "http://credreg.net/ctdlasn/schema/context/json";
        }
        [JsonProperty( "@context" )]
        public string Context { get; set; }


        [JsonProperty( "@id" )]
        public string CtdlId { get; set; }

        /// <summary>
        /// Main graph object
        /// </summary>
        [ JsonProperty( "@graph" )]
        public object Graph { get; set; }
        //public object Graph { get; set;  }

        [JsonProperty( "@type" )]
        public string Type { get; set; }

        [JsonProperty( "ceterms:ctid" )]
        public string CTID { get; set; }




    }
    public class CompetencyFramework : JsonLDDocument
    {
        [JsonIgnore]
        public static string classType = "ceasn:CompetencyFramework";
        public CompetencyFramework()
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

        [JsonProperty( PropertyName = "ceasn:alignTo" )]
        public List<IdProperty> alignTo { get; set; } = new List<IdProperty>();

        [JsonProperty( "@author" )]
        public List<string> author { get; set; } 

        

        [JsonProperty( PropertyName = "ceasn:conceptKeyword" )]
        public List<LanguageMap> conceptKeyword { get; set; }

        [JsonProperty( PropertyName = "ceasn:conceptTerm" )]
        public List<IdProperty> conceptTerm { get; set; } = new List<IdProperty>();

        [JsonProperty( PropertyName = "ceasn:creator" )]
        public List<IdProperty> creator { get; set; } = new List<IdProperty>();

        [JsonProperty( PropertyName = "ceasn:dateCopyrighted" )]
        public string dateCopyrighted { get; set; }

        [JsonProperty( PropertyName = "ceasn:dateCreated" )]
        public string dateCreated { get; set; }


        [JsonProperty( PropertyName = "ceasn:dateValidFrom" )]
        public string dateValidFrom { get; set; }

        [JsonProperty( PropertyName = "ceasn:dateValidUntil" )]
        public string dateValidUntil { get; set; }

        [JsonProperty( PropertyName = "ceasn:derivedFrom" )]
        public List<IdProperty> derivedFrom { get; set; } = new List<IdProperty>();

        //???language map??
        [JsonProperty( PropertyName = "ceasn:description" )]
        public LanguageMap description { get; set; } = new LanguageMap();

        [ JsonProperty( PropertyName = "ceasn:educationLevelType" )]
        public List<IdProperty> educationLevelType { get; set; } = new List<IdProperty>();

        [JsonProperty( PropertyName = "ceasn:hasTopChild" )]
        public List<IdProperty> hasTopChild { get; set; } = new List<IdProperty>();

        [JsonProperty( PropertyName = "ceasn:identifier" )]
        public List<IdProperty> identifier { get; set; } = new List<IdProperty>();

        [JsonProperty( PropertyName = "ceasn:inLanguage" )]
        public List<string> inLanguage { get; set; } = new List<string>();

        [JsonProperty( PropertyName = "ceasn:license" )]
        public IdProperty license { get; set; }

        [JsonProperty( PropertyName = "ceasn:localSubject" )]
        public List<LanguageMap> localSubject { get; set; } = new List<LanguageMap>();


        [JsonProperty( PropertyName = "ceasn:name" )]
        public LanguageMap name { get; set; } = new LanguageMap();

        [ JsonProperty( PropertyName = "ceasn:publicationStatusType" )]
        public List<IdProperty> publicationStatusType { get; set; } = new List<IdProperty>();

        [JsonProperty( PropertyName = "ceasn:publisher" )]
        public List<IdProperty> publisher { get; set; } = new List<IdProperty>();

        [JsonProperty( PropertyName = "ceasn:publisherName" )]
        public List<LanguageMap> publisherName { get; set; } = new List<LanguageMap>();
        //

        [JsonProperty( PropertyName = "ceasn:repositoryDate" )]
        public string repositoryDate { get; set; }

        [JsonProperty( PropertyName = "ceasn:rights" )]
        public IdProperty rights { get; set; }

        [JsonProperty( PropertyName = "ceasn:rightsHolder" )]
        public IdProperty rightsHolder { get; set; }

        [JsonProperty( PropertyName = "ceasn:source" )]
        public List<IdProperty> source { get; set; } = new List<IdProperty>();
   
        //
        [JsonProperty( PropertyName = "ceasn:tableOfContents" )]
        public List<LanguageMap> tableOfContents { get; set; } = new List<LanguageMap>();
    }

    public class CompetencyFrameworkInput 
    {
        [JsonIgnore]
        public static string classType = "ceasn:CompetencyFramework";
        public CompetencyFrameworkInput()
        {
            Type = classType;
        }
        [JsonProperty( "@type" )]
        public string Type { get; set; }

        [JsonProperty( "@id" )]
        public string CtdlId { get; set; }

        [JsonProperty( PropertyName = "ceterms:ctid" )]
        public string CTID { get; set; }



        [JsonProperty( PropertyName = "ceasn:alignFrom" )]
        public List<string> alignFrom { get; set; } = new List<string>();

        [JsonProperty( PropertyName = "ceasn:alignTo" )]
        public List<string> alignTo { get; set; } = new List<string>();

        /// <summary>
        /// A person or organization chiefly responsible for the intellectual or artistic content of this competency framework or competency.
        /// </summary>
        [JsonProperty( "ceasn:author" )]
        public List<string> author { get; set; }


        /// <summary>
        /// A word or phrase used by the promulgating agency to refine and differentiate individual competencies contextually.
        /// </summary>
        [JsonProperty( PropertyName = "ceasn:conceptKeyword" )]
        public List<LanguageMap> conceptKeyword { get; set; }

        /// <summary>
        /// A term drawn from a controlled vocabulary used by the promulgating agency to refine and differentiate individual competencies contextually.
        /// </summary>
        [JsonProperty( PropertyName = "ceasn:conceptTerm" )]
        public List<string> conceptTerm { get; set; } = new List<string>();

        [JsonProperty( PropertyName = "ceasn:creator" )]
        public List<string> creator { get; set; } = new List<string>();

        [JsonProperty( PropertyName = "ceasn:dateCopyrighted" )]
        public string dateCopyrighted { get; set; }

        [JsonProperty( PropertyName = "ceasn:dateCreated" )]
        public string dateCreated { get; set; }


        [JsonProperty( PropertyName = "ceasn:dateValidFrom" )]
        public string dateValidFrom { get; set; }

        [JsonProperty( PropertyName = "ceasn:dateValidUntil" )]
        public string dateValidUntil { get; set; }

        [JsonProperty( PropertyName = "ceasn:derivedFrom" )]
        public List<string> derivedFrom { get; set; } = new List<string>();

        //???language map??
        [JsonProperty( PropertyName = "ceasn:description" )]
        public LanguageMap description { get; set; } = new LanguageMap();

        [JsonProperty( PropertyName = "ceasn:educationLevelType" )]
        public List<string> educationLevelType { get; set; } = new List<string>();

        [JsonProperty( PropertyName = "ceasn:hasTopChild" )]
        public List<string> hasTopChild { get; set; } = new List<string>();

        [JsonProperty( PropertyName = "ceasn:identifier" )]
        public List<string> identifier { get; set; } = new List<string>();

        [JsonProperty( PropertyName = "ceasn:inLanguage" )]
        public List<string> inLanguage { get; set; } = new List<string>();

        [JsonProperty( PropertyName = "ceasn:license" )]
        public string license { get; set; }

        [JsonProperty( PropertyName = "ceasn:localSubject" )]
        public List<LanguageMap> localSubject { get; set; } = new List<LanguageMap>();


        [JsonProperty( PropertyName = "ceasn:name" )]
        public LanguageMap name { get; set; } = new LanguageMap();

        [JsonProperty( PropertyName = "ceasn:publicationStatusType" )]
        public List<string> publicationStatusType { get; set; } = new List<string>();

        [JsonProperty( PropertyName = "ceasn:publisher" )]
        public List<string> publisher { get; set; } = new List<string>();

        [JsonProperty( PropertyName = "ceasn:publisherName" )]
        public List<LanguageMap> publisherName { get; set; } = new List<LanguageMap>();
        //

        [JsonProperty( PropertyName = "ceasn:repositoryDate" )]
        public string repositoryDate { get; set; }

        [JsonProperty( PropertyName = "ceasn:rights" )]
        public string rights { get; set; }

        [JsonProperty( PropertyName = "ceasn:rightsHolder" )]
        public string rightsHolder { get; set; }

        [JsonProperty( PropertyName = "ceasn:source" )]
        public List<string> source { get; set; } = new List<string>();

        //
        [JsonProperty( PropertyName = "ceasn:tableOfContents" )]
        public List<LanguageMap> tableOfContents { get; set; } = new List<LanguageMap>();
    }
}

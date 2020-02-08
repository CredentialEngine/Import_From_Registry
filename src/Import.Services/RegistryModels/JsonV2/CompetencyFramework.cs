using System.Collections.Generic;
using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
	public class CompetencyFrameworksGraph
    {
        [JsonIgnore]
        public static string classType = "ceasn:CompetencyFramework";
        public CompetencyFrameworksGraph()
        {
            Type = classType;
            Context = "https://credreg.net/ctdlasn/schema/context/json";
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


        [JsonIgnore]
        [JsonProperty( "@type" )]
        public string Type { get; set; }

        [JsonIgnore]
        [JsonProperty( "ceterms:ctid" )]
        public string CTID { get; set; }

    }
    public class CompetencyFramework : JsonLDDocument
    {
        [JsonIgnore]
        public static string classType = "ceasn:CompetencyFramework";
		[JsonIgnore]
		public static string thisContext = "https://credreg.net/ctdlasn/schema/context/json";
		public CompetencyFramework()
        {
            Type = classType;
			//Context = thisContext;	//
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

		[JsonProperty( PropertyName = "ceasn:altIdentifier" )]
		public List<string> altIdentifier { get; set; }

		[JsonProperty( "@author" )]
        public List<string> author { get; set; } 

        
        [JsonProperty( PropertyName = "ceasn:conceptKeyword" )]
        public LanguageMapList conceptKeyword { get; set; }

        [JsonProperty( PropertyName = "ceasn:conceptTerm" )]
        public List<string> conceptTerm { get; set; } = new List<string>();

        [JsonProperty( PropertyName = "ceasn:creator" )]
        public List<string> creator { get; set; } = new List<string>();

        [JsonProperty( PropertyName = "ceasn:dateCopyrighted" )]
        public string dateCopyrighted { get; set; }

        [JsonProperty( PropertyName = "ceasn:dateCreated" )]
        public string dateCreated { get; set; }

		[JsonProperty( PropertyName = "ceasn:dateModified" )]
		public string dateModified { get; set; }

		[JsonProperty( PropertyName = "ceasn:dateValidFrom" )]
        public string dateValidFrom { get; set; }

        [JsonProperty( PropertyName = "ceasn:dateValidUntil" )]
        public string dateValidUntil { get; set; }

        //single per https://github.com/CredentialEngine/CompetencyFrameworks/issues/66
        [JsonProperty( PropertyName = "ceasn:derivedFrom" )]
        public string derivedFrom { get; set; } 

        //???language map??
        [JsonProperty( PropertyName = "ceasn:description" )]
        public LanguageMap description { get; set; } = new LanguageMap();

        [ JsonProperty( PropertyName = "ceasn:educationLevelType" )]
        public List<string> educationLevelType { get; set; } = new List<string>();

		/// <summary>
		/// Uris to external frameworks
		/// </summary>
		//[JsonProperty( PropertyName = "ceasn:exactAlignment" )]
		//public List<string> exactAlignment { get; set; } = new List<string>();

		/// <summary>
		/// Top-level child competency of a competency framework.
		/// </summary>
		[JsonProperty( PropertyName = "ceasn:hasTopChild" )]
        public List<string> hasTopChild { get; set; } = new List<string>();

        [JsonProperty( PropertyName = "ceasn:identifier" )]
        public List<string> identifier { get; set; } = new List<string>();

        [JsonProperty( PropertyName = "ceasn:inLanguage" )]
        public List<string> inLanguage { get; set; } = new List<string>();

        [JsonProperty( PropertyName = "ceasn:license" )]
        public string license { get; set; }

        [JsonProperty( PropertyName = "ceasn:localSubject" )]
        public LanguageMapList localSubject { get; set; } = new LanguageMapList();


        [JsonProperty( PropertyName = "ceasn:name" )]
        public LanguageMap name { get; set; } = new LanguageMap();

        [ JsonProperty( PropertyName = "ceasn:publicationStatusType" )]
        public string publicationStatusType { get; set; }// = new List<IdProperty>();

        [JsonProperty( PropertyName = "ceasn:publisher" )]
        public List<string> publisher { get; set; } = new List<string>();

        [JsonProperty( PropertyName = "ceasn:publisherName" )]
        public LanguageMapList publisherName { get; set; } = new LanguageMapList();
        //

        [JsonProperty( PropertyName = "ceasn:repositoryDate" )]
        public string repositoryDate { get; set; }

		/// <summary>
		/// 19-01-18 Changed to a language string
		/// Hide until changed in CaSS
		/// </summary>
		[JsonProperty( PropertyName = "ceasn:rights" )]
		public LanguageMap rights { get; set; } = new LanguageMap();
		//public object rights { get; set; }
		//public List<string> rights { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "ceasn:rightsHolder" )]
        public string rightsHolder { get; set; }

        [JsonProperty( PropertyName = "ceasn:source" )]
        public List<string> source { get; set; } = new List<string>();
   
        //
        [JsonProperty( PropertyName = "ceasn:tableOfContents" )]
        public LanguageMap tableOfContents { get; set; } = new LanguageMap();

		[JsonProperty( PropertyName = "ceterms:occupationType" )]
		public List<CredentialAlignmentObject> OccupationType { get; set; } = new List<CredentialAlignmentObject>();

		[JsonProperty( PropertyName = "ceterms:industryType" )]
		public List<CredentialAlignmentObject> IndustryType { get; set; } = new List<CredentialAlignmentObject>();

	}


}

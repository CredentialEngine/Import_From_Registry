using System.Collections.Generic;
//using System.Text.Json;
//using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
    public class GraphContainer
    {
		
		public GraphContainer()
        {
            Context = "https://credreg.net/ctdlasn/schema/context/json";
        }
        [JsonProperty( "@context" )]
        public string Context { get; set; }

        [JsonProperty( "@id" )]
        public string CtdlId { get; set; }

        /// <summary>
        /// Main graph object
        /// </summary>
        [JsonProperty( "@graph" )]
        public List<object> Graph { get; set; } = new List<object>();

/* should not be used for a graph*/
        [Newtonsoft.Json.JsonIgnore]
        [JsonProperty( "@type" )]
        public string Type { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        [JsonProperty( "ceterms:ctid" )]
        public string CTID { get; set; }

    }

    /// <summary>
    /// A class for use where the type of a payload is not known.
    /// </summary>
    public class GraphMainResource
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public GraphMainResource()
        {
        }

        [JsonProperty( "@id" )]
        public string CtdlId { get; set; }

        [JsonProperty( "@type" )]
        public string Type { get; set; }

        [JsonProperty( "ceterms:ctid" )]
        public string CTID { get; set; }

        [JsonProperty( PropertyName = "ceterms:name" )]
        public LanguageMap Name { get; set; } = new LanguageMap();

        [JsonProperty( PropertyName = "ceterms:description" )]
        public LanguageMap Description { get; set; } = new LanguageMap();


        [JsonProperty( "ceasn:name" )]
        public LanguageMap FrameworkName { get; set; } = new LanguageMap();

        [JsonProperty( PropertyName = "ceasn:description" )]
        public LanguageMap FrameworkDescription { get; set; } = new LanguageMap();

        [JsonProperty( "skos:prefLabel" )]
        public LanguageMap PrefLabel { get; set; } = new LanguageMap();

        [JsonProperty( "ceasn:competencyText" )]
        public LanguageMap CompetencyText { get; set; } = new LanguageMap();

        [JsonProperty( "ceterms:subjectWebpage" )]
        public string SubjectWebpage { get; set; }


        /// <summary>
        /// OwnedBy
        /// List of URIs
        /// </summary>
        [JsonProperty( PropertyName = "ceterms:ownedBy" )]
        public List<string> OwnedBy { get; set; }


        [JsonProperty( PropertyName = "ceterms:offeredBy" )]
        public List<string> OfferedBy { get; set; }


        public string ResourceName
        {
            get
            {
                if ( Name != null )
                    return Name.ToString();
                else if ( FrameworkName != null )
                    return FrameworkName.ToString();
                else if ( CompetencyText != null )
                    return CompetencyText.ToString();
                else if ( PrefLabel != null )
                    return PrefLabel.ToString();
                else
                    return "";
            }
        }
        public string ResourceDescription
        {
            get
            {
                if ( Description != null )
                    return Description.ToString();
                else if ( FrameworkDescription != null )
                    return FrameworkDescription.ToString();

                else
                    return "";
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;


namespace Download.Models
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

    public class GraphMainResource
    {
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

        [JsonProperty( "ceasn:name" )]
        public LanguageMap CeasnName { get; set; } = new LanguageMap();


        [JsonProperty( "ceterms:subjectWebpage" )]
        public string SubjectWebpage { get; set; }

        [JsonProperty( PropertyName = "ceterms:description" )]
        public LanguageMap Description { get; set; } = new LanguageMap();
        /// <summary>
        /// OwnedBy
        /// List of URIs
        /// </summary>
        [JsonProperty( PropertyName = "ceterms:ownedBy" )]
        public List<string> OwnedBy { get; set; }


        [JsonProperty( PropertyName = "ceterms:offeredBy" )]
        public List<string> OfferedBy { get; set; }
    }
}

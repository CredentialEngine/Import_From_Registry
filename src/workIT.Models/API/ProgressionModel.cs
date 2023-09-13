using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace workIT.Models.API
{
    [JsonObject( ItemNullValueHandling = NullValueHandling.Ignore )]
    public class ProgressionModel : BaseAPIType
    {
        public ProgressionModel()
        {
            EntityTypeId = 12;
            BroadType = "ProgressionModel";
            CTDLType = "ceasn:ProgressionModel";
            CTDLTypeLabel = "Progression Model";
        }
        public string Source { get; set; }
    }
    public class ProgressionLevel
    {
        /// <summary>
        /// CTID - identifier for concept. 
        /// Format: ce-UUID (lowercase)
        /// example: ce-a044dbd5-12ec-4747-97bd-a8311eb0a042
        /// </summary>
        public string CTID { get; set; }

        /// <summary>
        /// Concept 
        /// Required
        /// </summary>
        public string PrefLabel { get; set; }

        /// <summary>
        /// Concetpt description 
        /// Required
        /// </summary>
        public string Definition { get; set; }

        public bool IsTopConcept { get; set; }

    }
}

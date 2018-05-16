using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RA.Models.Json
{
    public class DirectCostType
    {
        public DirectCostType()
        {
            Type = "ceterms:CredentialAlignmentObject";

        }

        [JsonProperty( "@type" )]
        public string Type { get; set; }

        [JsonProperty( PropertyName = "ceterms:targetNode" )]
        public string TargetNode { get; set; }

        [JsonProperty( PropertyName = "ceterms:targetNodeDescription" )]
        public string TargetNodeDescription { get; set; }

        [JsonProperty( PropertyName = "targetNodeDescription" )]
        public string TargetNodeName { get; set; }
    }
}

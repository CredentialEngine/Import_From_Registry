﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RA.Models.Json
{
    /// <summary>
    /// Common input class for all verification profiles
    /// </summary>
    public class FinancialAlignmentObject
    {
        public FinancialAlignmentObject()
        {
            CodedNotation = null;
            Type = "ceterms:FinancialAlignmentObject";
        }
        [JsonProperty( "@type" )]
        public string Type { get; set; }

        [JsonProperty( PropertyName = "ceterms:alignmentType" )]
        public string AlignmentType { get; set; }

        [JsonProperty( PropertyName = "ceterms:codedNotation" )]
        public string CodedNotation { get; set; }
		//public List<string> CodedNotation { get; set; }

		[JsonProperty( PropertyName = "ceterms:targetNode" )]
        public string TargetNode { get; set; }

        [JsonProperty( PropertyName = "ceterms:targetNodeDescription" )]
        public string TargetNodeDescription { get; set; }

        [JsonProperty( PropertyName = "ceterms:targetNodeName" )]
        public string TargetNodeName { get; set; }

        [JsonProperty( PropertyName = "ceterms:framework" )]
        public string Framework { get; set; }

        [JsonProperty( PropertyName = "ceterms:frameworkName" )]
        public string FrameworkName { get; set; }

        [JsonProperty( PropertyName = "ceterms:weight" )]
        public decimal Weight { get; set; }

        [JsonProperty( PropertyName = "ceterms:alignmentDate" )]
        public string AlignmentDate { get; set; }
    }
}




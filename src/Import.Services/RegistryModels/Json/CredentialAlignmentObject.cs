using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RA.Models.Json
{
	/// <summary>
	/// Credential Alignment Object
	/// Modifications
	/// 2017-10-17 TargetNodeName is now required!
	/// </summary>
	public class CredentialAlignmentObject
    {
        public CredentialAlignmentObject()
        {
            Type = "ceterms:CredentialAlignmentObject";
			//CodedNotation = new List<string>();
			CodedNotation = null;
			AlignmentDate = null;
			//Weight = null;
			AlignmentType = null;

			
		}
        /// <summary>
        /// Need a custom mapping to @type based on input value
        /// </summary>
        [JsonProperty( "@type" )]
        public string Type { get; set; }


        /// <summary>
        /// Alignment Date
        /// The date the alignment was made.
        /// </summary>
        [JsonProperty( PropertyName = "ceterms:alignmentDate" )]
        public string AlignmentDate { get; set; }

        /// <summary>
        /// Alignment Type
        /// A category of alignment between the learning resource and the framework node.
        /// </summary>
        [JsonProperty( PropertyName = "ceterms:alignmentType" )]
        public string AlignmentType { get; set; }

        /// <summary>
        /// Coded Notation
        /// A short set of alpha-numeric symbols that uniquely identifies a resource and supports its discovery.
        /// </summary>
        [JsonProperty( PropertyName = "ceterms:codedNotation" )]
        public string CodedNotation { get; set; }
		//public List<string> CodedNotation { get; set; }
		/// <summary>
		/// Coded Notation
		/// A short set of alpha-numeric symbols that uniquely identifies a resource and supports its discovery.
		///// </summary>
		//[JsonProperty( PropertyName = "ceterms:codedNotation" )]
		//public string CodedNotation2 { get; set; }


		/// <summary>
		/// Framework URL
		/// The framework to which the resource being described is aligned.Must be a valid URL.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:framework" )]
        public string Framework { get; set; }

		/// <summary>
		/// Framework Name
		/// The name of the framework to which the resource being described is aligned. 
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:frameworkName" )]
        public string FrameworkName { get; set; }

        /// <summary>
        /// Target Node
        /// The node of a framework targeted by the alignment. Must be a valid URL.
        /// </summary>
        [JsonProperty( PropertyName = "ceterms:targetNode" )]
        public string TargetNode { get; set; }

        /// <summary>
        /// Target Description
        /// The description of a node in an established educational framework.
        /// </summary>
        [JsonProperty( PropertyName = "ceterms:targetNodeDescription" )]
        public string TargetNodeDescription { get; set; }

        /// <summary>
        /// Target Node Name
        /// The name of a node in an established educational framework.
        /// </summary>
        [JsonProperty( PropertyName = "ceterms:targetNodeName" )]
        public string TargetNodeName { get; set; }

        /// <summary>
        /// Weight
        /// An asserted measurement of the weight, degree, percent, or strength of a recommendation, requirement, or comparison.
        /// </summary>
        [JsonProperty( PropertyName = "ceterms:weight" )]
        public decimal Weight { get; set; }

        //have to handle weight and alignmentdate 
    }
}

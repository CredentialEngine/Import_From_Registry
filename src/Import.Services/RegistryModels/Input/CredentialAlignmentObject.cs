using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RA.Models.Input
{
    /// <summary>
    /// 2018-09-02 Where LanguageMap alternates are available, only enter one. The system will check the string version first. 
    /// </summary>
    public class CredentialAlignmentObject
	{
        /// <summary>
        /// If the target exists in the registry, just provide the CTID. 
        /// When present and valid, the rest of the properties will be ignored. 
        /// </summary>
        public string CTID { get; set; }

        /// <summary>
        /// Set of alpha-numeric symbols as defined by the body responsible for this resource that uniquely identifies this resource and supports its discovery and use.
        /// </summary>
        public string CodedNotation { get; set; }

        /// <summary>
        /// Could be a registry URL or external, typically expect a framework URL.
        /// URL
        /// </summary>
        public string Framework { get; set; }
        /// <summary>
        /// Formal name of the framework.
        /// </summary>
		public string FrameworkName { get; set; }
        /// <summary>
        /// Name of the framework - using LanguageMap
        /// </summary>
        public LanguageMap FrameworkName_Map { get; set; } = new LanguageMap();

        /// <summary>
        /// Individual entry in a formally defined framework such as a competency or an industry, instructional program, or occupation code.
        /// xsd:anyURI
        /// </summary>
        public string TargetNode { get; set; }
        /// <summary>
        /// Textual description of an individual concept or competency in a formally defined framework.
        /// </summary>
		public string TargetNodeDescription { get; set; }
        /// <summary>
        /// Alternately provide description using LanguageMap
        /// </summary>
        public LanguageMap TargetNodeDescription_Map { get; set; } = new LanguageMap();

        /// <summary>
        /// Name of an individual concept or competency in a formally defined framework
        /// </summary>
        public string TargetNodeName { get; set; }
        /// <summary>
        /// Name of an individual concept or competency in a formally defined framework using languageMap
        /// </summary>
        public LanguageMap TargetNodeName_Map { get; set; } = new LanguageMap();

        /// <summary>
        /// Measurement of the weight, degree, percent, or strength of a recommendation, requirement, or comparison.
        /// </summary>
        public decimal Weight { get; set; }
	}
}

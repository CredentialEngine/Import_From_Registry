using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using workIT.Models.ProfileModels;

namespace workIT.Models.Common
{
	/// <summary>
	/// Profession, trade, or career field that may involve training and/or a formal qualification.
	/// </summary>
	public class Job : BaseEmploymentObject
	{
		/*
		ceasn:abilityEmbodied
		ceasn:comment
		ceasn:knowledgeEmbodied
		ceasn:skillEmbodied
		ceterms:alternateName
		ceterms:classification
		ceterms:codedNotation
		ceterms:ctid
		ceterms:description
		ceterms:environmentalHazardType
		ceterms:hasOccupation
		ceterms:hasTask
		ceterms:hasWorkRole
		ceterms:identifier
		ceterms:industryType
		ceterms:isMemberOf
		ceterms:keyword
		ceterms:name
		ceterms:occupationType
		ceterms:offeredBy
		ceterms:performanceLevelType
		ceterms:physicalCapabilityType
		ceterms:requires
		ceterms:sameAs
		ceterms:sensoryCapabilityType
		ceterms:subjectWebpage
		ceterms:versionIdentifier
		*/
		public Job()
		{
			EntityTypeId = 32;
		}
		/// <summary>
		///  type
		/// </summary>
		public string Type { get; set; } = "ceterms:Job";

			/// <summary>
		/// IndustryType
		/// Type of industry; select from an existing enumeration of such types such as the SIC, NAICS, and ISIC classifications.
		/// Best practice in identifying industries for U.S. credentials is to provide the NAICS code using the ceterms:naics property. 
		/// Other credentials may use the ceterms:industrytype property and any framework of the class ceterms:IndustryClassification.
		/// ceterms:industryType
		/// </summary>
		public List<CredentialAlignmentObjectProfile> IndustryType { get; set; } = new List<CredentialAlignmentObjectProfile>();

		/// <summary>
		/// Keyword or key phrase describing relevant aspects of an entity.
		/// ceterms:keyword
		/// </summary>
		//public List<string> Keyword { get; set; }
		public List<string> Keyword { get; set; }

        public List<ResourceSummary> HasOccupation { get; set; } = new List<ResourceSummary>();
        //public List<ResourceSummary> HasSupportService { get; set; } = new List<ResourceSummary>();
        public List<ResourceSummary> HasWorkRole { get; set; } = new List<ResourceSummary>();
        public List<ResourceSummary> HasTask{ get; set; } = new List<ResourceSummary>();
		public List<ResourceSummary> ProvidesTransferValueFor { get; set; } = new List<ResourceSummary>();
		public List<ResourceSummary> ReceivesTransferValueFrom { get; set; } = new List<ResourceSummary>();


		/// <summary>
		/// OccupationType
		/// Type of occupation; select from an existing enumeration of such types.
		///  For U.S. credentials, best practice is to identify an occupation using a framework such as the O*Net. 
		///  Other credentials may use any framework of the class ceterms:OccupationClassification, such as the EU's ESCO, ISCO-08, and SOC 2010.
		/// </summary>
		public List<CredentialAlignmentObjectProfile> OccupationType { get; set; } = new List<CredentialAlignmentObjectProfile>();

		/// <summary>
		/// Organization(s) that offer this resource
		/// </summary>
		public List<Organization> OfferedBy { get; set; }

		public List<ConditionProfile> Requires { get; set; } = new List<ConditionProfile>();

		/// <summary>
		/// Another source of information about the entity being described.
		/// List of URIs
		/// ceterms:sameAs
		/// </summary>
		//public List<string> SameAs { get; set; }
		public List<string> SameAs { get; set; } = new List<string>();

		public List<Pathway> TargetPathway { get; set; } = new List<Pathway>();

		#region import
		/// <summary>
		/// Occupation related to this resource.
		/// </summary>
		public List<int> HasOccupationIds { get; set; }

        /// <summary>
        /// Task related to this resource.
        /// <see cref="https://credreg.net/ctdl/terms/hasTask"/>
        /// ceterms:hasSpecialization
        /// </summary>
        public List<int> HasTaskIds { get; set; }

        /// <summary>
        /// Work Role related to this resource.
        /// List of URIs for an existing WorkRole
        /// ceterms:hasWorkRole
        /// </summary>
        public List<int> HasWorkRoleIds { get; set; }
        //public List<int> HasSupportServiceIds { get; set; } = new List<int>();
        public List<Guid> OfferedByList { get; set; }
		public List<int> ProvidesTVForIds { get; set; }
		public List<int> ReceivesTVFromIds { get; set; }
		public List<CredentialAlignmentObjectProfile> Occupations { get; set; }
		public List<CredentialAlignmentObjectProfile> Industries { get; set; }
        #endregion
     
    }
}

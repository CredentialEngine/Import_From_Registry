using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using WMP=workIT.Models.ProfileModels;

namespace workIT.Models.Common
{
	/// <summary>
	/// Profession, trade, or career field that may involve training and/or a formal qualification.
	/// </summary>
	public class OccupationProfile : BaseEmploymentObject
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
			ceterms:hasJob					*
			ceterms:hasSpecialization		*
			ceterms:hasWorkforceDemand		*
			ceterms:hasWorkRole				*	
			ceterms:identifier
			ceterms:industryType			**
			ceterms:isSpecializationOf		*
			ceterms:keyword					**
			ceterms:name
			ceterms:occupationType			**
			ceterms:requires				_*
			ceterms:sameAs					**
			ceterms:subjectWebpage			**
			ceterms:versionIdentifier
		*/
		public OccupationProfile()
		{
			EntityTypeId = 35;
		}
		/// <summary>
		///  type
		/// </summary>
		public string Type { get; set; } = "ceterms:Occupation";

        /// <summary>
        /// Job related to this resource.
        /// CTID for an existing Job
        /// ceterms:hasJob
        /// </summary>
        public List<ResourceSummary> HasJob { get; set; } = new List<ResourceSummary>();

        /// <summary>
        /// More specialized profession, trade, or career field that is encompassed by the one being described.
        /// List of URIs for an existing Occupation
        /// <see cref="https://credreg.net/ctdl/terms/hasSpecialization"/>
        /// ceterms:hasSpecialization
        public List<ResourceSummary> HasSpecialization { get; set; } = new List<ResourceSummary>();

		public List<ResourceSummary> HasTask { get; set; } = new List<ResourceSummary>();



		/// <summary>
		/// Work Role related to this resource.
		/// List of URIs for an existing WorkRole
		/// ceterms:hasWorkRole
		/// </summary>
		public List<ResourceSummary> HasWorkRole { get; set; } = new List<ResourceSummary>();

		public List<ResourceSummary> ProvidesTransferValueFor { get; set; } = new List<ResourceSummary>();
		public List<ResourceSummary> ReceivesTransferValueFrom { get; set; } = new List<ResourceSummary>();


		/// <summary>
		/// IndustryType
		/// Type of industry; select from an existing enumeration of such types such as the SIC, NAICS, and ISIC classifications.
		/// Best practice in identifying industries for U.S. credentials is to provide the NAICS code using the ceterms:naics property. 
		/// Other credentials may use the ceterms:industrytype property and any framework of the class ceterms:IndustryClassification.
		/// ceterms:industryType
		/// </summary>
		//public Enumeration IndustryType { get; set; }
		public List<CredentialAlignmentObjectProfile> IndustryType { get; set; }
        /// <summary>
        /// Less specialized profession, trade, or career field that encompasses the one being described.
        /// List of URIs for an existing Occupation
        /// ceterms:isSpecializationOf
        /// </summary>
        public List<ResourceSummary> IsSpecializationOf { get; set; } = new List<ResourceSummary>();


        /// <summary>
        /// Keyword or key phrase describing relevant aspects of an entity.
        /// ceterms:keyword
        /// </summary>
        public List<string> Keyword { get; set; }

		/// <summary>
		/// OccupationType
		/// Type of occupation; select from an existing enumeration of such types.
		///  For U.S. credentials, best practice is to identify an occupation using a framework such as the O*Net. 
		///  Other credentials may use any framework of the class ceterms:OccupationClassification, such as the EU's ESCO, ISCO-08, and SOC 2010.
		public Enumeration OccupationType { get; set; }
        public List<CredentialAlignmentObjectProfile> OccupationTypes { get; set; }

        public List<WMP.ConditionProfile> Requires { get; set; }

		/// <summary>
		/// Another source of information about the entity being described.
		/// List of URIs
		/// ceterms:sameAs
		/// </summary>
		public List<string> SameAs { get; set; }


		#region import
		public List<Guid> AssertedByList { get; set; } = new List<Guid>();

		public List<Guid> TasksIds { get; set; } = new List<Guid>();
        public List<int> HasWorkRoleIds { get; set; }
        public List<int> HasJobIds { get; set; }
		public List<int> HasTaskIds { get; set; }
		public List<int> ProvidesTVForIds { get; set; }
		public List<int> ReceivesTVFromIds { get; set; }

		public List<int> HasSpecializationIds { get; set; }
        public List<int> IsSpecializationOfIds { get; set; }
        //
        public List<CredentialAlignmentObjectProfile> Occupations { get; set; }
		public List<CredentialAlignmentObjectProfile> Industries { get; set; }
		#endregion
	}

	public class RelatedKSA
	{
		public List<WMP.Competency> Competencies { get; set; } = new List<WMP.Competency>();
		public List<Job> Jobs { get; set; } = new List<Job>();
		public List<OccupationProfile> Occupations { get; set; }
		public List<Task> Tasks { get; set; } = new List<Task>();

		public List<WorkRole> WorkRoles { get; set; } = new List<WorkRole>();
	}
}

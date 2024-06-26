using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ApiEntity = workIT.Models.API.CompetencyFramework;
using workIT.Models.Elastic;
using workIT.Models.Common;

namespace workIT.Models.ProfileModels
{
	/// <summary>
	/// CompetencyFramework used by Import, etc.
	/// </summary>
	public class CompetencyFramework : TopLevelObject
	{
		public CompetencyFramework()
		{
		}

		//public int EntityStateId { get; set; }
		//public string Name { get; set; }
  //      [Obsolete]
		//public string FrameworkUrl { get; set; }
		//public string RepositoryUri { get; set; }
		public bool ExistsInRegistry { get; set; }
		//public string CredentialRegistryId { get; set; }
		//public string CTID { get; set; }
        public string FrameworkUri { get; set; }
		//
        public string Source { get; set; }
		//handle alias for use in common methods
		public new string SubjectWebpage
		{
			get { return Source; }
			set { this.Source = value; }
		}
        //public System.Guid RowId { get; set; }
		//CTID for owning organization, where origin is registry
		public string OrganizationCTID { get; set; }
		//public int OrganizationId { get; set; }
		//public List<Guid> PublishedBy { get; set; } = new List<Guid>();
		//can we use just one list and include a property type (owner, creator, publisher, etc)
		//public List<OrganizationReference> Creators = new List<OrganizationReference>()
		//public List<OrganizationReference> Publisher = new List<OrganizationReference>()
		public string CompetencyFrameworkGraph { get; set; }
		/// <summary>
		/// NOT stored yet
		/// </summary>
		public int TotalCompetencies { get; set; }
		/// <summary>
		/// A summary version of Compentencies for use in Elastic.
		/// Not clear if this will be used. 
		/// </summary>
		public string ElasticCompentenciesStore { get; set; }
		//TBD
		public string APIFramework { get; set; }
		public ApiEntity ApiFramework { get; set; }

		//[JsonProperty( "ceasn:alignFrom" )]
		public List<string> AlignFrom { get; set; }

		public List<string> AlignTo { get; set; }

		public List<string> AltIdentifier { get; set; }

		public List<string> Author { get; set; }


		//public LanguageMapList conceptKeyword { get; set; }

		public List<string> ConceptTerm { get; set; }

		public List<string> Creator { get; set; }

		public string DateCopyrighted { get; set; }

		/// <summary>
		/// Only allow date (yyyy-mm-dd), no time
		/// </summary>
		public string DateCreated { get; set; }

		public string DateModified { get; set; }

		public string DateValidFrom { get; set; }

		public string DateValidUntil { get; set; }

		//single per https://github.com/CredentialEngine/CompetencyFrameworks/issues/66
		public string DerivedFrom { get; set; }

		public List<string> educationLevelType { get; set; }

		/// <summary>
		/// Top-level child competency of a competency framework.
		/// </summary>
		public List<string> HasTopChild { get; set; }

		public List<string> Identifier { get; set; } = new List<string>();

		public List<string> inLanguage { get; set; }

		//[JsonProperty( "ceasn:license" )]
		public string License { get; set; }

		//public LanguageMapList localSubject { get; set; }

		public string PublicationStatusType { get; set; }//
		public List<string> Publisher { get; set; }

		//public LanguageMapList publisherName { get; set; }
		//

		public string RepositoryDate { get; set; }

		/// <summary>
		/// 19-01-18 Changed to a language string
		/// Hide until changed in CaSS
		/// </summary>
		//[JsonProperty( "ceasn:rights" )]
		//public LanguageMap rights { get; set; }
		//public object rights { get; set; }
		//public List<string> rights { get; set; } 

		//[JsonProperty( "ceasn:rightsHolder" )]
		public string RightsHolder { get; set; }
		public List<Entity_IdentifierValue> VersionIdentifier { get; set; }
        public string LatestVersion { get; set; }
        public string PreviousVersion { get; set; }
        public string NextVersion { get; set; }
        //public List<string> source { get; set; }

        //
        //public LanguageMap tableOfContents { get; set; }

        //public List<CredentialAlignmentObject> OccupationType { get; set; }

        //public List<CredentialAlignmentObject> IndustryType { get; set; }
        public List<IndexCompetency> Competencies { get; set; } = new List<IndexCompetency>();

		#region import 
		public List<Guid> OwnedByIds { get; set; } = new List<Guid>();
		public Guid AuthorId { get; set; }
		public List<Guid> CreatedByIds { get; set; } = new List<Guid>();
		public List<Guid> PublisherIds { get; set; } = new List<Guid>();

		public List<Competency> ImportCompetencies { get; set; } = new List<Competency>();

		#endregion
	}

	public class CompetencyFrameworkSummary : CompetencyFramework
	{
		public CompetencyFrameworkSummary()
		{

		}
		//public string FriendlyName { get; set; }
		//public string OrganizationName { get; set; }

		public int ReferencedByAssessments { get; set; }
		public int ReferencedByCredentials { get; set; }
		public int ReferencedByLearningOpportunities { get; set; }
		//public int ResultNumber { get; set; }
	}

	public class Competency
	{
		public Competency()
		{
		}

		//public string Type { get; set; }
		public int Id { get; set; }
		public int FrameworkId { get; set; }
		public string CtdlId { get; set; }
		public string CTID { get; set; }
		public Guid RowId { get; set; }

		public string CompetencyText { get; set; }

		public string CompetencyCategory { get; set; }
		public string CompetencyLabel { get; set; }
		public bool? IsTopChildOf { get; set; }
		//
		public DateTime? DateModified { get; set; }
		public DateTime? Created { get; set; }
		public DateTime? LastUpdated { get; set; }
		public string CreatedDisplay { get { return Created == null ? "" : ((DateTime)Created).ToShortDateString(); } }
		public string LastUpdatedDisplay { get { return LastUpdated == null ? "" : ( ( DateTime ) LastUpdated ).ToShortDateString(); } }
		//public DateTime EntityLastUpdated { get; set; }

		/// <summary>
		/// Environmental Hazard Type
		/// Type of condition in the physical work performance environment that entails risk exposures requiring mitigating processes; 
		/// select from an existing enumeration of such types.
		/// skos:Concept
		/// Blank nodes!
		/// </summary>
		public List<ResourceSummary> EnvironmentalHazardType { get; set; } = new List<ResourceSummary>();

		/// <summary>
		/// Type of required or expected human performance level; select from an existing enumeration of such types.
		/// skos:Concept
		/// Blank nodes!
		/// </summary>
		public List<ResourceSummary> PerformanceLevelType { get; set; } = new List<ResourceSummary>();

		/// <summary>
		/// Type of physical activity required or expected in performance; select from an existing enumeration of such types.
		/// skos:Concept
		/// Blank nodes!
		/// </summary>
		public List<ResourceSummary> PhysicalCapabilityType { get; set; } = new List<ResourceSummary>();

		/// <summary>
		/// Type of required or expected sensory capability; select from an existing enumeration of such types.
		/// skos:Concept
		/// Blank nodes!
		/// </summary>
		public List<ResourceSummary> SensoryCapabilityType { get; set; } = new List<ResourceSummary>();

		public List<Entity_IdentifierValue> VersionIdentifier { get; set; }

		public CompetencyDetail CompetencyDetail { get; set; }

		public string CompetencyDetailJson { get; set; }

		#region Import

		public List<int> SubstantiatingCredentialIds { get; set; }
		public List<int> SubstantiatingCompetencyFrameworkIds { get; set; }
		public List<int> SubstantiatingJobIds { get; set; }
		public List<int> SubstantiatingOccupationIds { get; set; }
		public List<int> SubstantiatingOrganizationIds { get; set; }
		public List<int> SubstantiatingTaskIds { get; set; }
		/// <summary>
		/// these can be anything, so would not be an int. Probably a guid? Or as may use HasResource, would need the type and Id?
		/// </summary>
		public List<int> SubstantiatingResourceIds { get; set; }
		public List<int> SubstantiatingWorkroleIds { get; set; }

		#endregion
	}
	public class CompetencyDetail
	{

		public List<string> AlignFrom { get; set; } 

		public List<string> AlignTo { get; set; } 

		public List<string> AltCodedNotation { get; set; } 

		public List<string> Author { get; set; } 

		public List<string> BroadAlignment { get; set; } 

		public string CodedNotation { get; set; }

		public LanguageMap Comment { get; set; }

			

		///ProficiencyScale??
		public List<string> ComplexityLevel { get; set; } 

		public List<string> ComprisedOf { get; set; } 

		public List<LanguageMap> ConceptKeyword { get; set; }

		public List<string> ConceptTerm { get; set; } 

		public List<string> Creator { get; set; } 

		public List<string> CrossSubjectReference { get; set; }

		public string DateCreated { get; set; }
		public string DateModfied { get; set; }
		public List<string> DerivedFrom { get; set; } 

		public List<string> EducationLevelType { get; set; } 

		public List<string> ExactAlignment { get; set; } 

		public List<Competency> HasChild { get; set; } 

		public List<string> Identifier { get; set; } 

		public List<string> InLanguage { get; set; } 

		public List<string> IsChildOf { get; set; } 

		public List<string> IsPartOf { get; set; } 

		public string IsVersionOf { get; set; }

		public string ListID { get; set; }

		public List<LanguageMap> LocalSubject { get; set; }

		public List<string> MajorAlignment { get; set; } 

		public List<string> MinorAlignment { get; set; } 

		public List<string> NarrowAlignment { get; set; }

		public List<string> PrerequisiteAlignment { get; set; } 

		public List<string> SkillEmbodied { get; set; } 

		public string Weight { get; set; }

	}
	//
}

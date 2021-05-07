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
        public string SourceUrl { get; set; }

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
		public string CompentenciesStore { get; set; }
		//TBD
		public string APIFramework { get; set; }
		public ApiEntity ApiFramework { get; set; }

		//[JsonProperty( "ceasn:alignFrom" )]
		public List<string> alignFrom { get; set; }

		//[JsonProperty( "ceasn:alignTo" )]
		public List<string> alignTo { get; set; }

		//[JsonProperty( "ceasn:altIdentifier" )]
		public List<string> altIdentifier { get; set; }

		//[JsonProperty( "@author" )]
		public List<string> author { get; set; }


		//[JsonProperty( "ceasn:conceptKeyword" )]
		//public LanguageMapList conceptKeyword { get; set; }

		//[JsonProperty( "ceasn:conceptTerm" )]
		public List<string> conceptTerm { get; set; }

		//[JsonProperty( "ceasn:creator" )]
		public List<string> creator { get; set; }

		//[JsonProperty( "ceasn:dateCopyrighted" )]
		public string dateCopyrighted { get; set; }

		/// <summary>
		/// Only allow date (yyyy-mm-dd), no time
		/// </summary>
		//[JsonProperty( "ceasn:dateCreated" )]
		public string dateCreated { get; set; }

		//[JsonProperty( "ceasn:dateModified" )]
		public string dateModified { get; set; }

		//[JsonProperty( "ceasn:dateValidFrom" )]
		public string dateValidFrom { get; set; }

		//[JsonProperty( "ceasn:dateValidUntil" )]
		public string dateValidUntil { get; set; }

		//single per https://github.com/CredentialEngine/CompetencyFrameworks/issues/66
		//[JsonProperty( "ceasn:derivedFrom" )]
		public string derivedFrom { get; set; }

		//[JsonProperty( "ceasn:educationLevelType" )]
		public List<string> educationLevelType { get; set; }

		/// <summary>
		/// Top-level child competency of a competency framework.
		/// </summary>
		//[JsonProperty( "ceasn:hasTopChild" )]
		public List<string> hasTopChild { get; set; }

		//[JsonProperty( "ceasn:identifier" )]
		public List<string> Identifier { get; set; } = new List<string>();

		//[JsonProperty( "ceasn:inLanguage" )]
		public List<string> inLanguage { get; set; }

		//[JsonProperty( "ceasn:license" )]
		public string license { get; set; }

		//[JsonProperty( "ceasn:localSubject" )]
		//public LanguageMapList localSubject { get; set; }

		//[JsonProperty( "ceasn:publicationStatusType" )]
		public string publicationStatusType { get; set; }//
		//[JsonProperty( "ceasn:publisher" )]
		public List<string> publisher { get; set; }

		//[JsonProperty( "ceasn:publisherName" )]
		//public LanguageMapList publisherName { get; set; }
		//

		//[JsonProperty( "ceasn:repositoryDate" )]
		public string repositoryDate { get; set; }

		/// <summary>
		/// 19-01-18 Changed to a language string
		/// Hide until changed in CaSS
		/// </summary>
		//[JsonProperty( "ceasn:rights" )]
		//public LanguageMap rights { get; set; }
		//public object rights { get; set; }
		//public List<string> rights { get; set; } 

		//[JsonProperty( "ceasn:rightsHolder" )]
		public string rightsHolder { get; set; }

		//[JsonProperty( "ceasn:source" )]
		public List<string> source { get; set; }

		//
		//[JsonProperty( "ceasn:tableOfContents" )]
		//public LanguageMap tableOfContents { get; set; }

		////[JsonProperty( "ceterms:occupationType" )]
		//public List<CredentialAlignmentObject> OccupationType { get; set; }

		////[JsonProperty( "ceterms:industryType" )]
		//public List<CredentialAlignmentObject> IndustryType { get; set; }
		public List<IndexCompetency> Competencies { get; set; } = new List<IndexCompetency>();
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
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.ProfileModels;
using ImportCompetency = workIT.Models.ProfileModels.Competency;
namespace workIT.Models.Common
{
	public class Collection : TopLevelObject
	{
		public string Type { get; set; } = "ceterms:Collection";


		/// <summary>
		/// Category or classification of this resource.
		/// List of URIs that point to a concept
		/// </summary>
		//[JsonProperty( "ceterms:classification" )]
		public List<ResourceSummary> Classification { get; set; } = new List<ResourceSummary>();

		public string CodedNotation { get; set; }

		/// <summary>
		/// Resource in a Collection.
		/// </summary>
		public List<string> HasMember { get; set; } = new List<string>();
        public List<TopLevelObject> HasSupportService { get; set; } = new List<TopLevelObject>();


        //The primary language used in or by this resource.
        //public List<string> InLanguage { get; set; } = new List<string>();
        public List<TextValueProfile> InLanguageCodeList { get; set; } = new List<TextValueProfile>();


		/// <summary>
		/// Type of occupation; select from an existing enumeration of such types.
		/// </summary>
		public Enumeration Occupation { get; set; } = new Enumeration();
		//import
		public List<CredentialAlignmentObjectProfile> OccupationType { get; set; } = new List<CredentialAlignmentObjectProfile>();


		//public List<TextValueProfile> AlternativeOccupations { get; set; } = new List<TextValueProfile>();

		/// <summary>
		/// Type of industry; select from an existing enumeration of such types such as the SIC, NAICS, and ISIC classifications.
		/// </summary>
		public Enumeration Industry { get; set; } = new Enumeration();
		public List<CredentialAlignmentObjectProfile> IndustryType { get; set; } = new List<CredentialAlignmentObjectProfile>();
		//public List<TextValueProfile> AlternativeIndustries { get; set; } = new List<TextValueProfile>();

		/// <summary>
		/// Type of instructional program; select from an existing enumeration of such types.
		/// </summary>
		public List<CredentialAlignmentObjectProfile> InstructionalProgramType { get; set; }
		public Enumeration InstructionalProgramTypes { get; set; } = new Enumeration();

		//public List<string> Keyword { get; set; } = new List<string>();
		public List<TextValueProfile> Keyword { get; set; } = new List<TextValueProfile>();
		/// <summary>
		/// Type of official status of the record; select from an enumeration of such types.
		/// URI to a concept
		/// </summary>
		public Enumeration LifeCycleStatusType { get; set; } = new Enumeration();
		public string LifeCycleStatus { get; set; }
		public int LifeCycleStatusTypeId { get; set; }
		/// <summary>
		/// A legal document giving official permission to do something with this resource.
		/// </summary>
		public string License { get; set; }


		//public List<string> CollectionType { get; set; } = new List<string>();
		public Enumeration CollectionType { get; set; } = new Enumeration();
		public string CollectionGraph { get; set; }

		public List<CollectionMember> CollectionMember { get; set; } = new List<CollectionMember>();
		public List<CodeItem> CollectionMemberCounts { get; set; } = new List<CodeItem>();
		/// <summary>
		/// Conditions for collection membership
		/// </summary>
		public List<ConditionProfile> MembershipCondition { get; set; } = new List<ConditionProfile>();

		public List<ImportCompetency> ImportCompetencies { get; set; } = new List<ImportCompetency>();


		//public List<string> OwnedBy { get; set; } = new List<string>();
		public List<Guid> OwnedBy { get; set; } = new List<Guid>();
		/// <summary>
		/// Subjects
		/// </summary>
		public List<TextValueProfile> Subject { get; set; } = new List<TextValueProfile>();

		/// <summary>
		/// Webpage that describes this entity.
		/// </summary>
		public new string SubjectWebpage { get; set; }
		public string InCatalog { get; set; }

        /// <summary>
        /// Alphanumeric identifier of the version of the resource that is unique within the organizational context of its owner.
        /// </summary>
        public List<Entity_IdentifierValue> VersionIdentifier { get; set; }
        public string LatestVersion { get; set; }
        public string PreviousVersion { get; set; }
        public string NextVersion { get; set; }
        #region Import
        public List<Guid> HasMemberImport { get; set; } = new List<Guid>();
        public List<int> HasSupportServiceIds { get; set; } = new List<int>();
		/// <summary>
		/// Category or classification of this resource.
		/// List of URIs that point to a concept
		/// NO, the actual concept needs to have been imported and stored appropriately. 
		/// </summary>
		public List<string> ClassificationImport { get; set; } = new List<string>();
		/// <summary>
		/// Type of industry; select from an existing enumeration of such types such as the SIC, NAICS, and ISIC classifications.
		/// </summary>
		public List<CredentialAlignmentObjectProfile> IndustryTypeList { get; set; }

		/// <summary>
		/// Type of instructional program; select from an existing enumeration of such types.
		/// </summary>
		public List<CredentialAlignmentObjectProfile> InstructionalProgramTypeList { get; set; }

		/// <summary>
		/// Type of occupation; select from an existing enumeration of such types.
		/// </summary>
		public List<CredentialAlignmentObjectProfile> OccupationTypeLIst { get; set; }

		public List<CredentialAlignmentObjectProfile> SubjectList { get; set; }

		#endregion
	}

	public class CollectionMember
	{
		public int Id { get; set; }
		/// <summary>
		/// Default type for a collection member
		/// </summary>
		public string Type { get; set; } = "ceterms:CollectionMember";

		public string CollectionName { get; set; }
		public Enumeration CollectionType { get; set; } = new Enumeration();

		public int CollectionId { get; set; }	
		/// <summary>
		/// An identifier for use with blank nodes, to minimize duplicates
		/// </summary>
		public string BNodeId { get; set; }

		/// <summary>
		/// The name or title of this resource.
		/// </summary>
		public string Name { get; set; } 

		/// <summary>
		/// A short description of this resource.
		/// </summary>
		public string Description { get; set; }

		///// <summary>
		///// Will be FK to [actual entity].RowId or Entity.EntityUID, or Entity.Cache.EntityUID
		///// NOT USING, using ProxyFor
		///// </summary>
		//public Guid MemberUID { get; set; }

		public string StartDate { get; set; }

		public string EndDate { get; set; }

		/// <summary>
		/// URI to the resource that this member describes
		/// Likely will be the CTID from the import
		/// </summary>
		public string ProxyFor { get; set; }
		//may want to get the entity type immediately
		public int EntityTypeId { get; set; }
		public string EntityType { get; set; }

		public Nullable<System.DateTime> Created { get; set; }
		public Nullable<System.DateTime> LastUpdated { get; set; }

		public Collection Collection { get; set; }
	}
}

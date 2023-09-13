using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
	public class Collection
	{
		[JsonProperty( "@type" )]
		public string Type { get; set; } = "ceterms:Collection";

		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }  

		[JsonProperty( "ceterms:ctid" )]
		public string CTID { get; set; }

		[JsonProperty( PropertyName = "ceterms:alternateName" )]
		public LanguageMapList AlternateName { get; set; } 

		/// <summary>
		/// Category or classification of this resource.
		/// List of URIs that point to a concept
		/// </summary>
		[JsonProperty( "ceterms:classification" )]
        public List<string> Classification { get; set; }

        [JsonProperty( PropertyName = "ceterms:codedNotation" )]
		public string CodedNotation { get; set; }

		[JsonProperty( "ceterms:dateEffective" )]
		public string DateEffective { get; set; }

		[JsonProperty( "ceterms:expirationDate" )]
		public string ExpirationDate { get; set; }

		/// <summary>
		/// A short description of this resource.
		/// </summary>
		[JsonProperty( "ceterms:description" )]
		public LanguageMap Description { get; set; }

		/// <summary>
		/// Resource in a Collection.
		/// </summary>
		[JsonProperty( "ceterms:hasMember" )]
		public List<string> HasMember { get; set; }


        /// <summary>
        /// Reference to a relevant support service available for this resource.
        /// </summary>
        [JsonProperty( PropertyName = "ceterms:hasSupportService" )]
        public List<string> HasSupportService { get; set; }

        //The primary language used in or by this resource.
        [JsonProperty( "ceterms:inLanguage" )]
		public List<string> InLanguage { get; set; }

		/// <summary>
		/// Type of industry; select from an existing enumeration of such types such as the SIC, NAICS, and ISIC classifications.
		/// </summary>
		[JsonProperty( "ceterms:industryType" )]
		public List<CredentialAlignmentObject> IndustryType { get; set; }

		/// <summary>
		/// Type of instructional program; select from an existing enumeration of such types.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:instructionalProgramType" )]
		public List<CredentialAlignmentObject> InstructionalProgramType { get; set; }

        [JsonProperty( PropertyName = "ceterms:keyword" )]
		public object Keyword { get; set; }
		//public LanguageMapList Keyword { get; set; }

		/// <summary>
		/// Type of official status of the record; select from an enumeration of such types.
		/// URI to a concept
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:lifeCycleStatusType" )]
		public CredentialAlignmentObject LifeCycleStatusType { get; set; }

		/// <summary>
		/// A legal document giving official permission to do something with this resource.
		/// </summary>
		[JsonProperty( "ceasn:license" )]
		public string License { get; set; }

		/// <summary>
		/// Type of collection, list, set, or other grouping of resources; select from an existing enumeration of such types.
		/// ConceptScheme: CollectionCategory 
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:collectionType" )]
		//public object CollectionType { get; set; }
		public List<CredentialAlignmentObject> CollectionType { get; set; }
		/// <summary>
		/// The name or title of this resource.
		/// </summary>
		[JsonProperty( "ceterms:name" )]
		public LanguageMap Name { get; set; } = new LanguageMap();

		/// <summary>
		/// Conditions for collection membership
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:membershipCondition" )]
		public List<ConditionProfile> MembershipCondition { get; set; }

		/// <summary>
		/// Type of occupation; select from an existing enumeration of such types.
		/// </summary>
		[JsonProperty( "ceterms:occupationType" )]
		public List<CredentialAlignmentObject> OccupationType { get; set; }

		[JsonProperty( PropertyName = "ceterms:ownedBy" )]
		public List<string> OwnedBy { get; set; }

		/// <summary>
		/// Subjects
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:subject" )]
		public List<CredentialAlignmentObject> Subject { get; set; }

        /// <summary>
        /// Webpage(s) that describes this entity.
        /// </summary>
        [JsonProperty( PropertyName = "ceterms:subjectWebpage" )]
        public string SubjectWebpage { get; set; }

        //[JsonProperty( PropertyName = "ceterms:subjectWebpage" )]
        //public List<string> SubjectWebpage2 { get; set; }

        //*** Helper properties where publishing input is a graph. These will not be published
        /// <summary>
        /// CIP List is a helper when publishing from a graph. It will not be published
        /// </summary>
        [JsonProperty( "cipList" )]
		public List<string> CIPList { get; set; }
		/// <summary>
		/// SOC List is a helper when publishing from a graph. It will not be published
		/// </summary>
		[JsonProperty( "socList" )]
		public List<string> SOCList { get; set; }

		/// NAICS List is a helper when publishing from a graph. It will not be published
		[JsonProperty( "naicsList" )]
		public List<string> NaicsList { get; set; }
	}

	public class CollectionMember
	{
		/// <summary>
		/// Default type for a collection member
		/// </summary>
		[JsonProperty( "@type" )]
		public string Type { get; set; } = "ceterms:CollectionMember";

		/// <summary>
		/// An identifier for use with blank nodes, to minimize duplicates
		/// </summary>
		[JsonProperty( "@id" )]
		public string BNodeId { get; set; }

		//[JsonProperty( "ceterms:ctid" )]
		//public string CTID { get; set; }

		/// <summary>
		/// The name or title of this resource.
		/// </summary>
		[JsonProperty( "ceterms:name" )]
		public LanguageMap Name { get; set; } = new LanguageMap();

		/// <summary>
		/// A short description of this resource.
		/// </summary>
		[JsonProperty( "ceterms:description" )]
		public LanguageMap Description { get; set; }

		/// <summary>
		/// URI to the resource that this member describes
		/// </summary>
		[JsonProperty( "ceterms:proxyFor" )]
		public string ProxyFor { get; set; }

		[JsonProperty( "ceterms:startDate" )]
		public string StartDate { get; set; }

		[JsonProperty( "ceterms:endDate" )]
		public string EndDate { get; set; }

		[JsonProperty( PropertyName = "ceterms:alternateName" )]
		public LanguageMapList AlternateName { get; set; }
	}
}

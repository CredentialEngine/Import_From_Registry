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
	public class OccupationProfile : TopLevelObject
	{
		public OccupationProfile()
		{
			EntityTypeId = 35;
		}
		/// <summary>
		///  type
		/// </summary>
		public string Type { get; set; } = "ceterms:Occupation";

		/// <summary>
		/// URI
		/// </summary>
		public string CtdlId { get; set; }
		/// <summary>
		/// Globally unique Credential Transparency Identifier (CTID) by which the creator, owner or provider of a resource recognizes it in transactions with the external environment (e.g., in verifiable claims involving the resource).
		/// required
		/// <see cref="https://credreg.net/ctdl/terms/ctid"/>
		/// </summary>
		//public string CTID { get; set; }

		/// <summary>
		/// Name
		/// </summary>
		//public string Name { get; set; }

		/// <summary>
		/// Description
		/// </summary>
		//public string Description { get; set; }


		/// <summary>
		/// AbilityEmbodied
		/// Enduring attributes of the individual that influence performance are embodied either directly or indirectly in this resource.
		/// This can be one of five types. Using an approach like Entity.Assertation
		/// Any one of: Competency (most likely?), Job, Occupation. Task, WorkRole
		/// ceasn:abilityEmbodied
		/// </summary>
		public List<string> AbilityEmbodied { get; set; }
		//public Enumeration AbilityEmbodied { get; set; } 

		public string CodedNotation { get; set; }


		/// <summary>
		/// Category or classification of this resource.
		/// Where a more specific property exists, such as ceterms:naics, ceterms:isicV4, ceterms:credentialType, etc., use that property instead of this one.
		/// URI to a Concept
		/// ceterms:classification
		/// </summary>
		public List<string> ClassificationList { get; set; }
		public Enumeration Classification { get; set; }

		/// <summary>
		/// Comment
		/// Definition:	en-US: Supplemental text provided by the promulgating body that clarifies the nature, scope or use of this competency.
		/// ceasn:comment
		/// </summary>
		public List<string> Comment { get; set; } = new List<string>();

		/// <summary>
		/// Job related to this resource.
		/// CTID for an existing Job
		/// ceterms:hasJob
		/// </summary>
		public List<string> HasJob { get; set; }

		/// <summary>
		/// More specialized profession, trade, or career field that is encompassed by the one being described.
		/// List of URIs for an existing Occupation
		/// <see cref="https://credreg.net/ctdl/terms/hasSpecialization"/>
		/// ceterms:hasSpecialization
		[JsonProperty( PropertyName = "ceterms:hasSpecialization" )]
		public List<string> HasSpecialization { get; set; }

		/// <summary>
		/// Work Role related to this resource.
		/// List of URIs for an existing WorkRole
		/// ceterms:hasWorkRole
		/// </summary>
		public List<string> HasWorkRole { get; set; }

		/// <summary>
		/// Alphanumeric token that identifies this resource and information about the token's originating context or scheme.
		/// <see cref="https://purl.org/ctdl/terms/identifier"/>
		/// </summary>
		public List<IdentifierValue> Identifier { get; set; }
		public string IdentifierJson { get; set; }

		/// <summary>
		/// IndustryType
		/// Type of industry; select from an existing enumeration of such types such as the SIC, NAICS, and ISIC classifications.
		/// Best practice in identifying industries for U.S. credentials is to provide the NAICS code using the ceterms:naics property. 
		/// Other credentials may use the ceterms:industrytype property and any framework of the class ceterms:IndustryClassification.
		/// ceterms:industryType
		/// </summary>
		public Enumeration IndustryType { get; set; }

		/// <summary>
		/// Less specialized profession, trade, or career field that encompasses the one being described.
		/// List of URIs for an existing Occupation
		/// ceterms:isSpecializationOf
		/// </summary>
		public List<string> IsSpecializationOf { get; set; }

		/// <summary>
		/// Body of information embodied either directly or indirectly in this resource.
		/// List of URIs for a competency
		/// ceasn:knowledgeEmbodied
		/// </summary>
		public List<string> KnowledgeEmbodied { get; set; }


		/// <summary>
		/// Keyword or key phrase describing relevant aspects of an entity.
		/// ceterms:keyword
		/// </summary>
		//public List<string> Keyword { get; set; }
		public List<WMP.TextValueProfile> Keyword { get; set; }

		/// <summary>
		/// OccupationType
		/// Type of occupation; select from an existing enumeration of such types.
		///  For U.S. credentials, best practice is to identify an occupation using a framework such as the O*Net. 
		///  Other credentials may use any framework of the class ceterms:OccupationClassification, such as the EU's ESCO, ISCO-08, and SOC 2010.
		public Enumeration OccupationType { get; set; }

		public List<WMP.ConditionProfile> Requires { get; set; }

		/// <summary>
		/// Another source of information about the entity being described.
		/// List of URIs
		/// ceterms:sameAs
		/// </summary>
		//public List<string> SameAs { get; set; }
		public List<WMP.TextValueProfile> SameAs { get; set; } = new List<WMP.TextValueProfile>();

		/// <summary>
		///Ability to apply knowledge and use know-how to complete tasks and solve problems including types or categories of developed proficiency or dexterity in mental operations and physical processes is embodied either directly or indirectly in this resource.
		/// </summary>
		public List<string> SkillEmbodied { get; set; }

		/// <summary>
		/// Subject Webpage
		/// URL
		/// Required
		/// </summary>
		//public string SubjectWebpage { get; set; } //URL

		/// <summary>
		/// Alphanumeric identifier of the version of the credential that is unique within the organizational context of its owner.
		/// ceterms:versionIdentifier
		/// </summary>
		public List<IdentifierValue> VersionIdentifier { get; set; }
		public string VersionIdentifierJson { get; set; }


		#region import
		public List<int> AbilitiesIds { get; set; }
		public List<Guid> AbilityEmbodiedIds { get; set; } = new List<Guid>();
		public List<Guid> KnowledgeUIDs { get; set; } = new List<Guid>();
		public List<Guid> TasksIds { get; set; } = new List<Guid>();
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

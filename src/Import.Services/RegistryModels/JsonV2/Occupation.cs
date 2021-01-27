using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
	/// <summary>
	/// Profession, trade, or career field that may involve training and/or a formal qualification.
	/// </summary>
	public class Occupation
	{
		/// <summary>
		///  type
		/// </summary>
		[JsonProperty( "@type" )]
		public string Type { get; set; } = "ceterms:Occupation";

		/// <summary>
		/// URI
		/// </summary>
		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }
		/// <summary>
		/// Globally unique Credential Transparency Identifier (CTID) by which the creator, owner or provider of a resource recognizes it in transactions with the external environment (e.g., in verifiable claims involving the resource).
		/// required
		/// <see cref="https://credreg.net/ctdl/terms/ctid"/>
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:ctid" )]
		public string CTID { get; set; }

		/// <summary>
		/// Name
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:name" )]
		public LanguageMap Name { get; set; }

		/// <summary>
		/// Description
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:description" )]
		public LanguageMap Description { get; set; }


		/// <summary>
		/// AbilityEmbodied
		/// Enduring attributes of the individual that influence performance are embodied either directly or indirectly in this resource.
		/// ceasn:abilityEmbodied
		/// </summary>
		[JsonProperty( PropertyName = "ceasn:abilityEmbodied" )]
		public List<string> AbilityEmbodied { get; set; }
		//public List<CredentialAlignmentObject> AbilityEmbodied { get; set; } 

		/// <summary>
		/// Category or classification of this resource.
		/// Where a more specific property exists, such as ceterms:naics, ceterms:isicV4, ceterms:credentialType, etc., use that property instead of this one.
		/// URI to a competency
		/// ceterms:classification
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:classification" )]
		public List<string> Classification { get; set; }
		//public List<CredentialAlignmentObject> Classification { get; set; }

		/// <summary>
		/// Comment
		/// Definition:	en-US: Supplemental text provided by the promulgating body that clarifies the nature, scope or use of this competency.
		/// ceasn:comment
		/// </summary>
		[JsonProperty( PropertyName = "ceasn:comment" )]
		public LanguageMapList Comment { get; set; } = new LanguageMapList();

		/// <summary>
		/// Job related to this resource.
		/// CTID for an existing Job
		/// ceterms:hasJob
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:hasJob" )]
		public List<string> HasJob { get; set; } 

		/// <summary>
		/// More specialized profession, trade, or career field that is encompassed by the one being described.
		/// List of URIs for an existing Occupation
		/// <see cref="https://credreg.net/ctdl/terms/hasSpecialization"/>
		/// ceterms:hasSpecialization
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:hasSpecialization" )]
		public List<string> HasSpecialization { get; set; }

		/// <summary>
		/// Work Role related to this resource.
		/// List of URIs for an existing WorkRole
		/// ceterms:hasWorkRole
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:hasWorkRole" )]
		public List<string> HasWorkRole { get; set; } 

		/// <summary>
		/// Alphanumeric token that identifies this resource and information about the token's originating context or scheme.
		/// <see cref="https://purl.org/ctdl/terms/identifier"/>
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:identifier" )]
		public List<IdentifierValue> Identifier { get; set; } 

		/// <summary>
		/// IndustryType
		/// Type of industry; select from an existing enumeration of such types such as the SIC, NAICS, and ISIC classifications.
		/// Best practice in identifying industries for U.S. credentials is to provide the NAICS code using the ceterms:naics property. 
		/// Other credentials may use the ceterms:industrytype property and any framework of the class ceterms:IndustryClassification.
		/// ceterms:industryType
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:industryType" )]
		public List<CredentialAlignmentObject> IndustryType { get; set; }

		/// <summary>
		/// Less specialized profession, trade, or career field that encompasses the one being described.
		/// List of URIs for an existing Occupation
		/// ceterms:isSpecializationOf
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:isSpecializationOf" )]
		public List<string> IsSpecializationOf { get; set; } 

		/// <summary>
		/// Body of information embodied either directly or indirectly in this resource.
		/// List of URIs for a competency
		/// ceasn:knowledgeEmbodied
		/// </summary>
		[JsonProperty( PropertyName = "ceasn:knowledgeEmbodied" )]
		public List<string> KnowledgeEmbodied { get; set; }
		//public List<CredentialAlignmentObject> KnowledgeEmbodied { get; set; } 


		/// <summary>
		/// Keyword or key phrase describing relevant aspects of an entity.
		/// ceterms:keyword
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:keyword" )]
		public LanguageMapList Keyword { get; set; }


		/// <summary>
		/// OccupationType
		/// Type of occupation; select from an existing enumeration of such types.
		///  For U.S. credentials, best practice is to identify an occupation using a framework such as the O*Net. 
		///  Other credentials may use any framework of the class ceterms:OccupationClassification, such as the EU's ESCO, ISCO-08, and SOC 2010.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:occupationType" )]
		public List<CredentialAlignmentObject> OccupationType { get; set; } 


		/// <summary>
		/// Another source of information about the entity being described.
		/// List of URIs
		/// ceterms:sameAs
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:sameAs" )]
		public List<string> SameAs { get; set; } 

		/// <summary>
		///Ability to apply knowledge and use know-how to complete tasks and solve problems including types or categories of developed proficiency or dexterity in mental operations and physical processes is embodied either directly or indirectly in this resource.
		/// </summary>
		[JsonProperty( PropertyName = "ceasn:skillEmbodied" )]
		public List<string> SkillEmbodied { get; set; }
		//public List<CredentialAlignmentObject> SkillEmbodied { get; set; } 

		/// <summary>
		/// Subject Webpage
		/// URL
		/// Required
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:subjectWebpage" )]
		public string SubjectWebpage { get; set; } //URL

		/// <summary>
		/// Alphanumeric identifier of the version of the credential that is unique within the organizational context of its owner.
		/// ceterms:versionIdentifier
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:versionIdentifier" )]
		public List<IdentifierValue> VersionIdentifier { get; set; } 

	}
}

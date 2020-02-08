using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using RA.Models.JsonV2;

namespace RA.Models.Navy.Json
{
	public class Rating //: JsonLDDocument
	{
		public Rating()
		{
			Type = "navy:Rating";
		}
		/// <summary>
		///  type
		/// </summary>
		[JsonProperty( "@type" )]
		public string Type { get; set; }

		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }

		[JsonProperty( PropertyName = "ceasn:inLanguage" )]
		public List<string> InLanguage { get; set; } = new List<string>();

		/// <summary>
		/// CodedNotation
		/// An alphanumeric notation or ID code as defined by the promulgating body to identify this resource.
		/// </summary>
		[JsonProperty( "ceasn:codedNotation" )]
		public string CodedNotation { get; set; }

		/// <summary>
		/// Comment
		/// Supplemental text provided by the promulgating body that clarifies the nature, scope or use of this competency.
		/// </summary>
		[JsonProperty( "ceasn:comment" )]
		public LanguageMapList Comment { get; set; }

		/// <summary>
		/// Ctid
		/// Globally unique Credential Transparency Identifier (CTID) by which the creator, owner or provider of a resource recognizes it in transactions with the external environment (e.g., in verifiable claims involving the resource).
		/// </summary>
		[JsonProperty( "ceterms:ctid" )]
		public string CTID { get; set; }

		/// <summary>
		/// DatePublished
		/// Date when this resource was published.
		/// </summary>
		[JsonProperty( "schema:datePublished" )]
		public string DatePublished { get; set; }

		/// <summary>
		/// Description
		/// Description of the resource.
		/// </summary>
		[JsonProperty( "schema:description" )]
		public LanguageMap Description { get; set; }
		//
		/// <summary>
		/// HasCredential
		/// HasCredentialCredential related to this resource
		/// List of URIs
		/// </summary>
		[JsonProperty( "navy:hasCredential" )]
		public List<string> HasCredential { get; set; } 

		/// <summary>
		/// HasDoDOccupationType
		/// Type of occupation as categorized by the U.S. Department of Defense.
		/// Concept
		/// </summary>
		[JsonProperty( "navy:hasDoDOccupationType" )]
		public List<string> HasDoDOccupationType { get; set; }

		/// <summary>
		/// HasOccupationType
		/// Type of occupation.
		/// Concept
		/// </summary>
		[JsonProperty( "navy:hasOccupationType" )]
		public List<string> HasOccupationType { get; set; }

		/// <summary>
		/// Name
		/// Name of the resource.
		/// </summary>
		[JsonProperty( "schema:name" )]
		public LanguageMap Name { get; set; }

		/// <summary>
		/// UploadDate
		/// Date when this resource was uploaded.
		/// </summary>
		[JsonProperty( "schema:uploadDate" )]
		public string UploadDate { get; set; }

		/// <summary>
		/// Version
		/// Version of this resource.
		/// </summary>
		[JsonProperty( "schema:version" )]
		public string Version { get; set; }
	}
}



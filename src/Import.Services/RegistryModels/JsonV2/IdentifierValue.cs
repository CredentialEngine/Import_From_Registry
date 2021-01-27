
using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
	public class IdentifierValue
	{
		public IdentifierValue()
		{
			Type = "ceterms:IdentifierValue";
		}

		[JsonProperty( "@type" )]
		public string Type { get; set; }

		/// <summary>
		/// Formal name or acronym of the framework, scheme, type, or other organizing principle of this identifier, such as ISBN or ISSN.
		/// </summary>
		[JsonProperty( "ceterms:identifierTypeName" )]
		public LanguageMap IdentifierTypeName { get; set; }

		//[JsonProperty( "ceterms:description" )]
		//public LanguageMap Description { get; set; }

		/// <summary>
		/// Framework, scheme, type, or other organizing principle of this identifier.
		/// URI
		/// </summary>
		[JsonProperty( "ceterms:identifierType" )]
		public string IdentifierType { get; set; }

		/// <summary>
		/// Alphanumeric string identifier of the entity.
		/// Where a formal identification system exists for the identifier, recommended best practice is to use a string conforming to that system.
		/// </summary>
		[JsonProperty( "ceterms:identifierValueCode" )]
		public string IdentifierValueCode { get; set; }
	}
}

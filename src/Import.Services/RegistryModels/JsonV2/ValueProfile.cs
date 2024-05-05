using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
	public class ValueProfile
	{
		public ValueProfile()
		{
			Type = "ceterms:ValueProfile";
		}
		[JsonProperty( "@type" )]
		public string Type { get; set; }

		/// <summary>
		/// Provide a valid concept from the CreditUnitType concept scheme, with or without the namespace. For example:
		/// creditUnit:DegreeCredit or ContinuingEducationUnit
		/// <see cref="https://credreg.net/ctdl/terms/creditUnitType"/> 
		/// If this object is a monetary purpose, the UnitText would typically be the related currency for the value (example: "USD")
		/// </summary>
		[JsonProperty( "ceterms:creditUnitType" )]
		public List<CredentialAlignmentObject> CreditUnitTypeOLD { get; set; }

		[JsonIgnore]
		[JsonProperty( "ceterms:creditUnitTypeNew" )]
		public object CreditUnitType { get; set; }

		/// <summary>
		/// The level of credit associated with the credit awarded or required.
		/// Concept
		/// CoceptScheme - assumes audienceLevel, as no means to provide altenatives
		/// </summary>
		[JsonProperty( "ceterms:creditLevelType" )]
		public List<CredentialAlignmentObject> CreditLevelTypeOLD { get; set; }

		[JsonIgnore]
		[JsonProperty( "ceterms:creditLevelTypeNew" )]
		public object CreditLevelType { get; set; }

		/// <summary>
		/// Optional description of the value, using either a string value or as a language map
		/// </summary>
		[JsonProperty( "schema:description" )]
		public LanguageMap Description { get; set; }

		/// <summary>
		/// Minimum value for this purpose.
		/// </summary>
		[JsonProperty( "schema:minValue" )]
		public decimal? MinValue { get; set; }

		/// <summary>
		/// Maximum value for this purpose.
		/// </summary>
		[JsonProperty( "schema:maxValue" )]
		public decimal? MaxValue { get; set; }

		/// <summary>
		/// A percentage for this purpose. 
		/// Do not use if providing value
		/// qdata:percentage
		/// </summary>
		[JsonProperty( "qdata:percentage" )]
		public decimal? Percentage { get; set; }

		[JsonProperty( PropertyName = "ceterms:subject" )]
		public List<CredentialAlignmentObject> Subject { get; set; }

		/// <summary>
		/// A single value for this purpose. 
		/// </summary>
		[JsonProperty( "schema:value" )]
		public decimal? Value { get; set; }
	}
}

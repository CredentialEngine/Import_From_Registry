using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
	public class QuantitativeValue
	{
		public QuantitativeValue()
		{
			Type = "schema:QuantitativeValue";
			Value = null;
			Percentage = null;
		}
		[JsonProperty( "@type" )]
		public string Type { get; set; }

		[JsonProperty( "schema:unitText" )]
		public CredentialAlignmentObject UnitText { get; set; }

		[JsonProperty( "schema:value" )]
		public decimal? Value { get; set; }


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
		/// Expected input is as a percentage of 100 (and can be greater, or negative). Examples: 95.2, 11, etc.
		/// Do not use if providing any of value, minimum and maximum value.
		/// qdata:percentage
		/// </summary>
		[JsonProperty( "qdata:percentage" )]
		public decimal? Percentage { get; set; }

		[JsonProperty( "schema:description" )]
		public LanguageMap Description { get; set; }

		/// <summary>
		/// Type of suppression, masking, or other modification made to the data to protect the identities of its subjects.
		/// URI to a concept from qdata:DataWithholdingCategory
		/// </summary>
		[JsonProperty( "qdata:dataWithholdingType" )]
		public string DataWithholdingType { get; set; }
	}
}

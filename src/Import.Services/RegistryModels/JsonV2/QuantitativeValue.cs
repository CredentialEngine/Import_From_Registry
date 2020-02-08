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
		}
		[JsonProperty( "@type" )]
		public string Type { get; set; }

		[JsonProperty( "schema:unitText" )]
		public CredentialAlignmentObject UnitText { get; set; }

		[JsonProperty( "schema:value" )]
		public decimal Value { get; set; }
		[JsonProperty( "schema:minValue" )]
		public decimal MinValue { get; set; }
		[JsonProperty( "schema:maxValue" )]
		public decimal MaxValue { get; set; }

		[JsonProperty( "schema:description" )]
		public LanguageMap Description { get; set; }
	}
}

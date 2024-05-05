using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
	public class MonetaryAmount
	{
		public MonetaryAmount()
		{
			Type = "schema:MonetaryAmount";
		}
		[JsonProperty( "@type" )]
		public string Type { get; set; }

		/// <summary>
		/// Currency abbreviation (e.g., USD).
		/// </summary>
		[JsonProperty( "schema:currency" )]
		public string Currency { get; set; }

		/// <summary>
		/// Value of a monetary amount or a quantitative value.
		/// </summary>
		[JsonProperty( "schema:value" )]
		public decimal? Value { get; set; }

		/// <summary>
		/// Lower value of some characteristic or property.
		/// </summary>
		[JsonProperty( "schema:minValue" )]
		public decimal? MinValue { get; set; }

		/// <summary>
		/// Upper value of some characteristic or property.
		/// </summary>
		[JsonProperty( "schema:maxValue" )]
		public decimal? MaxValue { get; set; }

		[JsonProperty( "schema:description" )]
		public LanguageMap Description { get; set; }

		/// <summary>
		/// Word or phrase indicating the unit of measure - mostly if the use of Currency is not sufficient
		/// </summary>
		[JsonProperty( "schema:unitText" )]
		public string UnitText { get; set; }

		/// <summary>
		/// Type of suppression, masking, or other modification made to the data to protect the identities of its subjects.
		/// URI to a concept from qdata:DataWithholdingCategory
		/// </summary>
		[JsonProperty( "qdata:dataWithholdingType" )]
		public string DataWithholdingType { get; set; }
	}

	/// <summary>
	/// Statistical distribution of monetary amounts.
	/// https://credreg.net/qdata/terms/MonetaryAmountDistribution#MonetaryAmountDistribution
	/// </summary>
	public class MonetaryAmountDistribution
	{
		public MonetaryAmountDistribution()
		{
			Type = "schema:MonetaryAmountDistribution";
		}

		[JsonProperty( "@type" )]
		public string Type { get; set; }

		/// <summary>
		/// Currency abbreviation (e.g., USD).
		/// </summary>
		[JsonProperty( "schema:currency" )]
		public string Currency { get; set; }

		/// <summary>
		/// Median salary value.
		/// </summary>
		[JsonProperty( "qdata:median" )]
		public decimal? Median { get; set; }

		/// <summary>
		/// 10th percentile salary value.
		/// </summary>
		[JsonProperty( "qdata:percentile10" )]
		public decimal? Percentile10 { get; set; }

		/// <summary>
		/// 25th percentile salary value.
		/// </summary>
		[JsonProperty( "qdata:percentile25" )]
		public decimal? Percentile25 { get; set; }

		/// <summary>
		/// 75th percentile salary value.
		/// </summary>
		[JsonProperty( "qdata:percentile75" )]
		public decimal? Percentile75 { get; set; }

		/// <summary>
		/// 90th percentile salary value.
		/// </summary>
		[JsonProperty( "qdata:percentile90" )]
		public decimal? Percentile90 { get; set; }

		/// <summary>
		/// Type of suppression, masking, or other modification made to the data to protect the identities of its subjects.
		/// URI to a concept from qdata:DataWithholdingCategory
		/// </summary>
		[JsonProperty( "qdata:dataWithholdingType" )]
		public string DataWithholdingType { get; set; }
	}
}

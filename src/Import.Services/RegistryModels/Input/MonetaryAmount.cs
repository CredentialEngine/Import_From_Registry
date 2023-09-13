using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RA.Models.Input
{
	/// <summary>
	/// Monetary value or range.
	/// </summary>
	public class MonetaryAmount
	{
		/// <summary>
		/// Currency abbreviation (e.g., USD).
		/// </summary>
		public string Currency { get; set; }

		/// <summary>
		/// Value of a monetary amount or a quantitative value.
		/// </summary>
		public decimal Value { get; set; }

		/// <summary>
		/// Lower value of some characteristic or property.
		/// </summary>
		public decimal MinValue { get; set; }

		/// <summary>
		/// Upper value of some characteristic or property.
		/// </summary>
		public decimal MaxValue { get; set; }

		/// <summary>
		/// Description of this record
		/// </summary>
		public string Description { get; set; }
		/// <summary>
		/// Language map for description
		/// </summary>
		public LanguageMap Description_Map { get; set; }

		/// <summary>
		/// Word or phrase indicating the unit of measure - mostly if the use of Currency is not sufficient
		/// </summary>
		public string UnitText { get; set; }
	}

	/// <summary>
	/// Statistical distribution of monetary amounts.
	/// https://credreg.net/qdata/terms/MonetaryAmountDistribution#MonetaryAmountDistribution
	/// </summary>
	public class MonetaryAmountDistribution
	{
		/// <summary>
		/// Currency abbreviation (e.g., USD).
		/// </summary>
		public string Currency { get; set; }

		/// <summary>
		/// Median salary value.
		/// </summary>
		public decimal Median { get; set; }

		/// <summary>
		/// 10th percentile salary value.
		/// </summary>
		public decimal Percentile10 { get; set; }

		/// <summary>
		/// 25th percentile salary value.
		/// </summary>
		public decimal Percentile25 { get; set; }

		/// <summary>
		/// 75th percentile salary value.
		/// </summary>
		public decimal Percentile75 { get; set; }

		/// <summary>
		/// 90th percentile salary value.
		/// </summary>
		public decimal Percentile90 { get; set; }

	}
}

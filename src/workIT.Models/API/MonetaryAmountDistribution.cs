using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.API
{
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
		public string CurrencySymbol { get; set; }

		/// <summary>
		/// Median salary value.
		/// </summary>
		public decimal? Median { get; set; }

		/// <summary>
		/// 10th percentile salary value.
		/// </summary>
		public decimal? Percentile10 { get; set; }

		/// <summary>
		/// 25th percentile salary value.
		/// </summary>
		public decimal? Percentile25 { get; set; }

		/// <summary>
		/// 75th percentile salary value.
		/// </summary>
		public decimal? Percentile75 { get; set; }

		/// <summary>
		/// 90th percentile salary value.
		/// </summary>
		public decimal? Percentile90 { get; set; }

		public bool HasData()
		{
			if ( Median > 0 || Percentile10 > 0 || Percentile25 > 0 || Percentile75 > 0 || Percentile90 > 0 )
			{
				return true;
			}

			return false;
		}

	}
}

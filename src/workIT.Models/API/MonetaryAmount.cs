using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.API
{
	[Serializable]
	public class MonetaryAmount
	{
		/// <summary>
		/// Currency abbreviation (e.g., USD).
		/// </summary>
		public string Currency { get; set; }
		public string CurrencySymbol { get; set; }
		public string Description { get; set; }
		public decimal? Value { get; set; }
		public decimal? MinValue { get; set; }
		public decimal? MaxValue { get; set; }

		/// <summary>
		/// Provide a valid concept from the CreditUnitType concept scheme, with or without the namespace. For example:
		/// creditUnit:ContinuingEducationUnit or ContinuingEducationUnit
		/// <see cref="https://credreg.net/ctdl/terms/creditUnitType"/> 
		/// </summary>
		public string UnitText { get; set; }

		public bool HasData()
		{
			if ( Value > 0 || MinValue > 0 || MaxValue > 0
				|| ( Description ?? "" ).Length > 2 )
			{
				return true;
			}

			return false;
		}
	}

	
}

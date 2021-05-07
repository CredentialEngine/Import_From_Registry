using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.API
{
	public class QuantitativeValue
	{
		public string UnitText { get; set; }
		public string Label { get; set; }
		public decimal? Value { get; set; }
		public decimal? MinValue { get; set; }
		public decimal? MaxValue { get; set; }
		public decimal? Percentage { get; set; }

		public string CurrencySymbol { get; set; }
		public string Description { get; set; }
		//helper
		public bool IsRange
		{
			get
			{
				if ( MinValue > 0 && MaxValue > 0 )
					return true;
				else
					return false;
			}
		}

		public bool HasData()
		{
			if ( Value != 0 || MinValue != 0 || MaxValue != 0 || Percentage != 0
				|| ( Description ?? "" ).Length > 2 )
			{
				return true;
			}

			return false;
		}
		public string Summary()
		{
			if ( !HasData() )
				return "";

			if ( Percentage > 0 )
			{
				return string.Format( "{0}% {1}", Percentage, Description ?? "" );
			}
			else if ( Value > 0 )
			{
				//check if integer
				return string.Format( "{0} {1}", Value, Description ?? "" );
			}
			else if ( MinValue != 0 && MaxValue != 0 )
			{
				//check if integer
				return string.Format( "{0} to {1} {2}", MinValue, MaxValue, Description ?? "" );
			}
			else
				return Description;

		}
	}
}

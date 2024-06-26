using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.API
{
	public class DurationProfile 
	{
		public DurationProfile()
		{
			//MinimumDuration = new DurationItem();
			//MaximumDuration = new DurationItem();
			//ExactDuration = new DurationItem();
		}
		public string DurationSummary { get; set; }

		public string Description { get; set; }
		public string TimeRequired { get; set; }
	}

	public class DurationItem
	{
		public decimal Years { get; set; }
		public decimal Months { get; set; }
		public decimal Weeks { get; set; }
		public decimal Days { get; set; }
		public decimal Hours { get; set; }
		public decimal Minutes { get; set; }
		public bool HasValue { get { return Years + Months + Weeks + Days + Hours + Minutes > 0; } }

		public string Display()
		{
			var parts = new List<string>();
			if ( Years > 0 ) { parts.Add( Years + " year" + ( Years == 1 ? "" : "s" ) ); }
			if ( Months > 0 ) { parts.Add( Months + " month" + ( Months == 1 ? "" : "s" ) ); }
			if ( Weeks > 0 ) { parts.Add( Weeks + " week" + ( Weeks == 1 ? "" : "s" ) ); }
			if ( Days > 0 ) { parts.Add( Days + " day" + ( Days == 1 ? "" : "s" ) ); }
			if ( Hours > 0 ) { parts.Add( Hours + " hour" + ( Hours == 1 ? "" : "s" ) ); }

			if ( Minutes > 0 ) { parts.Add( Minutes + " minute" + ( Minutes == 1 ? "" : "s" ) ); }

			return string.Join( ", ", parts );
		}
	}
	//

}

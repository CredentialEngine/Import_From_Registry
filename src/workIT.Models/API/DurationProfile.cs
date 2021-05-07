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

		//public DurationItem MinimumDuration { get; set; }
		//public DurationItem MaximumDuration { get; set; }
		//public DurationItem ExactDuration { get; set; }
		//public bool HasData
		//{
		//	get
		//	{
		//		return (!string.IsNullOrWhiteSpace( DurationSummary )
		//	  || !string.IsNullOrWhiteSpace( Description ) );
		//	}
		//}
		//public bool HasData
		//{
		//	get
		//	{
		//		return (
		//			this.MinimumDuration != null  && this.MaximumDuration != null
		//	  && ( this.MinimumDuration.HasValue || this.MaximumDuration.HasValue )
		//	  )
		//	  || !string.IsNullOrWhiteSpace( DurationSummary )
		//	  || (ExactDuration.HasValue || !string.IsNullOrWhiteSpace(Description));
		//	}
		//}
		//public bool IsRange
		//{
		//	get
		//	{
		//		return this.MinimumDuration != null
		//	  && this.MaximumDuration != null
		//	  && ( this.MinimumDuration.HasValue || this.MaximumDuration.HasValue );
		//	}
		//}
	}

	public class DurationItem
	{
		public int Years { get; set; }
		public int Months { get; set; }
		public int Weeks { get; set; }
		public int Days { get; set; }
		public int Hours { get; set; }
		public int Minutes { get; set; }
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

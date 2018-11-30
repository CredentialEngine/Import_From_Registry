using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using workIT.Models.Common;

namespace workIT.Models.ProfileModels
{

	public class DurationProfile : BaseProfile
	{
		public DurationProfile()
		{
			MinimumDuration = new DurationItem();
			MaximumDuration = new DurationItem();
			ExactDuration = new DurationItem();
			ProfileName = "";
		}

		public int EntityId { get; set; }
		public Guid ParentUid { get; set; }
		public int ParentTypeId { get; set; }
		public string ParentType { get; set; }
		public string Conditions { get { return Description; } set { Description = value; } }

        /// <summary>
		/// 1 - Exact Estimated Duration
		/// 2 - Range Estimated Duration
		/// 3 - Renewal frequency
		/// </summary>
		public int DurationProfileTypeId { get; set; }
        public DurationItem MinimumDuration { get; set; }
		public DurationItem MaximumDuration { get; set; }
		public DurationItem ExactDuration { get; set; }

		public int MinimumMinutes { get; set; }
		public string MinimumDurationISO8601 { get; set; }
		public int MaximumMinutes { get; set; }
		public string MaximumDurationISO8601 { get; set; }
		public string ExactDurationISO8601 { get; set; }

		public bool IsRange { 
			get { return this.MinimumDuration != null 
				&& this.MaximumDuration != null 
				&& ( this.MinimumDuration.HasValue || this.MaximumDuration.HasValue );
			} }
	}
	//

	public class DurationItem
	{
		public string DurationISO8601 { get; set; }
		public int Years { get; set; }
		public int Months { get; set; }
		public int Weeks { get; set; }
		public int Days { get; set; }
		public int Hours { get; set; }
		public int Minutes { get; set; }
		public bool HasValue { get { return Years + Months + Weeks + Days + Hours + Minutes > 0; } }

		public string Print()
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

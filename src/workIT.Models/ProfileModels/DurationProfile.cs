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
			DurationSummary = "";
		}

		public int EntityId { get; set; }
		public Guid ParentUid { get; set; }
		public int ParentTypeId { get; set; }
		public string ParentType { get; set; }

		///// <summary>
		///// Why using Conditions?
		///// </summary>
		////public string Conditions { get { return Description; } set { Description = value; } }

        /// <summary>
		/// 1 - Exact Estimated Duration
		/// 2 - Range Estimated Duration
		/// 3 - Renewal frequency
		/// </summary>
		public int DurationProfileTypeId { get; set; }
        public DurationItem MinimumDuration { get; set; }
		public DurationItem MaximumDuration { get; set; }
		public DurationItem ExactDuration { get; set; }

		//can make this simple, so don't really need DurationItem
		public DurationItem TimeRequiredImport { get; set; }
		public string TimeRequired { get; set; }
		public decimal TimeAmount { get; set; }
		public string TimeUnit { get; set; }

		public string DurationSummary { get; set; } 
		//public int MinimumMinutes { get; set; }
		public string MinimumDurationISO8601 { get; set; }
		//public int MaximumMinutes { get; set; }
		public string MaximumDurationISO8601 { get; set; }
		public string ExactDurationISO8601 { get; set; }
		public bool HasData
		{
			get
			{
				return ( this.MinimumDuration != null && this.MaximumDuration != null && ( this.MinimumDuration.HasValue || this.MaximumDuration.HasValue ) )
							|| ( (this.ExactDuration != null && ExactDuration.HasValue) )
							|| ( ( this.TimeRequiredImport != null && TimeRequiredImport.HasValue ) )
							|| !string.IsNullOrWhiteSpace( Description );
			}
		}
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
        public decimal Years { get; set; }
        public decimal Months { get; set; }
        public decimal Weeks { get; set; }
        public decimal Days { get; set; }
        public decimal Hours { get; set; }
        public decimal Minutes { get; set; }

		public decimal DurationAmount { get; set; }
		public string DurationUnit { get; set; }

		//public string TimeRequired { get; set; }
		//public decimal TimeAmount { get; set; }
		//public string TimeUnit { get; set; }

		public bool HasValue { get { return Years + Months + Weeks + Days + Hours + Minutes > 0; } }

        public string Print()
        {
            var parts = new List<string>();
            if (Years > 0) { parts.Add( Years.ToString( "G29" ) + " year" + (Years == 1 ? "" : "s") ); }
            if (Months > 0) { parts.Add( Months.ToString( "G29" ) + " month" + (Months == 1 ? "" : "s") ); }
            if (Weeks > 0) { parts.Add( Weeks.ToString( "G29" ) + " week" + (Weeks == 1 ? "" : "s") ); }
            if (Days > 0) { parts.Add( Days.ToString( "G29" ) + " day" + (Days == 1 ? "" : "s") ); }
            if (Hours > 0) { parts.Add( Hours.ToString( "G29" ) + " hour" + (Hours == 1 ? "" : "s") ); }
            if (Minutes > 0) { parts.Add( Minutes.ToString( "G29" ) + " minute" + (Minutes == 1 ? "" : "s") ); }

            if (parts.Count > 0)
                return string.Join( ", ", parts );
            else
                return string.Empty;
        }
    }
    public class DurationItemDecimal
	{
		public string DurationISO8601 { get; set; }
		public decimal Years { get; set; }
		public decimal Months { get; set; }
		public decimal Weeks { get; set; }
		public decimal Days { get; set; }
		public decimal Hours { get; set; }
		public decimal Minutes { get; set; }
		public bool HasValue { get { return Years + Months + Weeks + Days + Hours + Minutes > 0; } }

		public string Print()
		{
			var parts = new List<string>();
			if ( Years > 0 )	{ parts.Add( Years + " year" + ( Years == 1 ? "" : "s" ) ); }
			if ( Months > 0 )	{ parts.Add( Months + " month" + ( Months == 1 ? "" : "s" ) ); }
			if ( Weeks > 0 )	{ parts.Add( Weeks + " week" + ( Weeks == 1 ? "" : "s" ) ); }
			if ( Days > 0 )		{ parts.Add( Days + " day" + ( Days == 1 ? "" : "s" ) ); }
			if ( Hours > 0 )	{ parts.Add( Hours + " hour" + ( Hours == 1 ? "" : "s" ) ); }
			if ( Minutes > 0 )	{ parts.Add( Minutes + " minute" + ( Minutes == 1 ? "" : "s" ) ); }

			if ( parts .Count > 0)
				return string.Join( ", ", parts );
			else 
				return string.Empty;
		}
	}
	//

}

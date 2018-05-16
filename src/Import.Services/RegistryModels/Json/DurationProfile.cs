using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RA.Models.Json
{
	public class DurationProfile
	{
		public DurationProfile()
		{
			MinimumDuration = null;
			MaximumDuration = null;
			ExactDuration = null;
			//MinimumDuration = new DurationItem();
			//MaximumDuration = new DurationItem();
			//ExactDuration = new DurationItem();
			Type = "ceterms:DurationProfile";
		}

        [JsonProperty( "@type" )]
        public string Type { get; set; }

        [JsonProperty( PropertyName = "ceterms:description" )]
        public string Description { get; set; }

        //[JsonIgnore]        
        //public DurationItem MinimumDuration { get; set; }

        [JsonProperty( PropertyName = "ceterms:minimumDuration" )]
		public string MinimumDuration { get; set; }
		// public string Minimum { get { return AsSchemaDuration( MinimumDuration ); } }

		//[JsonIgnore]
		//public DurationItem MaximumDuration { get; set; }

		[JsonProperty( PropertyName = "ceterms:maximumDuration" )]
		public string MaximumDuration { get; set; }
		// public string Maximum { get { return AsSchemaDuration( MaximumDuration ); } }

		//[JsonIgnore]        
		//public DurationItem ExactDuration { get; set; }

		[JsonProperty( PropertyName = "ceterms:exactDuration" )]
		public string ExactDuration { get; set; }
		//public string Exact { get { return AsSchemaDuration( ExactDuration ); } }

		//[JsonIgnore]
  //      public bool IsRange
		//{
		//	get
		//	{
		//		return this.MinimumDuration != null && this.MaximumDuration != null && ( this.MinimumDuration.HasValue && this.MaximumDuration.HasValue );
		//	}
		//}

        //public static string AsSchemaDuration( DurationItem entity )
        //{
        //    string duration = "";

        //    if ( entity.Years > 0 ) duration += entity.Years.ToString() + "Y";
        //    if ( entity.Months > 0 ) duration += entity.Months.ToString() + "M";
        //    if ( entity.Weeks > 0 ) duration += entity.Weeks.ToString() + "W";
        //    if ( entity.Days > 0 ) duration += entity.Days.ToString() + "D";
        //    if ( entity.Hours > 0 || entity.Minutes > 0 ) duration += "T";
        //    if ( entity.Hours > 0 ) duration += entity.Hours.ToString() + "H";
        //    if ( entity.Minutes > 0 ) duration += entity.Minutes.ToString() + "M";

        //    if ( !string.IsNullOrEmpty( duration ) ) duration = "P" + duration;
        //    return duration;
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
	}
}

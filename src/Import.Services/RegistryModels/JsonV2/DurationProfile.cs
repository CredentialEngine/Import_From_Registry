using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
namespace RA.Models.JsonV2
{
	public class DurationProfile
	{
		public DurationProfile()
		{
			MinimumDuration = null;
			MaximumDuration = null;
			ExactDuration = null;
			TimeRequired = null;
			Type = "ceterms:DurationProfile";
		}

        [JsonProperty( "@type" )]
        public string Type { get; set; }

        [JsonProperty( PropertyName = "ceterms:description" )]
        public LanguageMap Description { get; set; }

        [JsonProperty( PropertyName = "ceterms:minimumDuration" )]
		public string MinimumDuration { get; set; }

		[JsonProperty( PropertyName = "ceterms:maximumDuration" )]
		public string MaximumDuration { get; set; }

		[JsonProperty( PropertyName = "ceterms:exactDuration" )]
		public string ExactDuration { get; set; }


		[JsonProperty( PropertyName = "ceterms:timeRequired" )]
		public string TimeRequired { get; set; }

	}

	//public class DurationItem
	//{
	//	public int Years { get; set; }
	//	public int Months { get; set; }
	//	public int Weeks { get; set; }
	//	public int Days { get; set; }
	//	public int Hours { get; set; }
	//	public int Minutes { get; set; }
	//	public bool HasValue { get { return Years + Months + Weeks + Days + Hours + Minutes > 0; } }
	//}
}

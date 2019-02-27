using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
namespace RA.Models.JsonV2
{
	public class JurisdictionProfile
	{
        public JurisdictionProfile()
        {
            Type = "ceterms:JurisdictionProfile";
			//MainJurisdiction = null;
			MainJurisdiction = new List<Place>();
			JurisdictionException = new List<Place>();
			//AssertedBy = new List<string>();
			AssertedBy = null;
		}

        [JsonProperty( "@type" )]
        public string Type { get; set; }

		[JsonProperty( PropertyName = "ceterms:globalJurisdiction" , DefaultValueHandling = DefaultValueHandling.Include)]        
		public bool? GlobalJurisdiction { get; set; }

		[JsonProperty( PropertyName = "ceterms:description" )]
		public LanguageMap Description { get; set; }

		/// <summary>
		/// The main jurisdiction, commonly a country name.
		/// The schema is defined as an array. However, the RA, and editor only allow a single MainJurisdiction
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:mainJurisdiction" )]
		public List<Place> MainJurisdiction { get; set; }

		//[JsonProperty( PropertyName = "ceterms:jurisdictionException" )]
		//public List<GeoCoordinates> JurisdictionException { get; set; }

		[JsonProperty( PropertyName = "ceterms:jurisdictionException" )]
		public List<Place> JurisdictionException { get; set; }

		/// <summary>
		/// Asserted by is typically only explicitly entered for jurisdiction assertions the INs
		/// NOTE: It must be serialized to a List
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:assertedBy" )]
		public List<string> AssertedBy { get; set; }

	}
	

}

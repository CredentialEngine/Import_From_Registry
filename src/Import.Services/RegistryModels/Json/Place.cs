using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace RA.Models.Json
{
	public class Place
	{
		public Place()
		{
			Type = "ceterms:Place";
			ContactPoint = new List<Json.ContactPoint>();
		}
		[JsonProperty( "@type" )]
		public string Type { get; set; }

		[JsonProperty( "ceterms:geoURI" )]
		public string GeoURI { get; set; }

		[JsonProperty( PropertyName = "ceterms:name" )]
		public string Name { get; set; }

		[JsonProperty( PropertyName = "ceterms:description" )]
		public string Description { get; set; }

		[JsonProperty( "ceterms:streetAddress" )]
		public string StreetAddress { get; set; }

		[JsonProperty( "ceterms:postOfficeBoxNumber" )]
		public string PostOfficeBoxNumber { get; set; }

		[JsonProperty( "ceterms:addressLocality" )]
		public string City { get; set; }

		[JsonProperty( "ceterms:addressRegion" )]
		public string AddressRegion { get; set; }
		[JsonProperty( "ceterms:postalCode" )]
		public string PostalCode { get; set; }

		[JsonProperty( "ceterms:addressCountry" )]
		public string Country { get; set; }

		[JsonProperty( PropertyName = "ceterms:latitude" )]
		public double Latitude { get; set; }

		[JsonProperty( PropertyName = "ceterms:longitude" )]
		public double Longitude { get; set; }


		[JsonProperty( "ceterms:targetContactPoint" )]
		public List<ContactPoint> ContactPoint { get; set; }
	}
}

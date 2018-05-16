using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace RA.Models.Json
{
	public class AvailableAt: Place
	{
	}

	public class AvailableAt2
	{
		public AvailableAt2()
		{
			ContactPoint = new List<ContactPoint>();
			Type = "ceterms:PostalAddress";
		}
		[JsonProperty( "@type" )]
		public string Type { get; set; }
		[JsonProperty( "ceterms:name" )]
		public string Name { get; set; }

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

		[JsonProperty( "ceterms:targetContactPoint" )]
		public List<ContactPoint> ContactPoint { get; set; }
	}

	public class AvailableAtOLD
	{
		public AvailableAtOLD()
		{
			//Type = "ceterms:PostalAddress";
			Address = new List<Json.Address>();
		}
		//[ JsonProperty( "@type" )]
		//public string Type { get; set; }

		//[JsonProperty( "ceterms:name" )]
		//public string Name { get; set; }


		[JsonProperty( "ceterms:address" )]
		public List<Address> Address { get; set; }
	}
}

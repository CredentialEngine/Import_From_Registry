using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
///Entity describing a mailing address.
	public class PostalAddress
	{
		public PostalAddress()
		{
			Type = "ceterms:PostalAddress";
		}
		[JsonProperty( "@type" )]
		public string Type { get; set; }


		[JsonProperty( PropertyName = "ceterms:name" )]
		public LanguageMap Name { get; set; }


		[JsonProperty( "ceterms:streetAddress" )]
		public LanguageMap StreetAddress { get; set; }

		[JsonProperty( "ceterms:postOfficeBoxNumber" )]
		public string PostOfficeBoxNumber { get; set; }

		[JsonProperty( "ceterms:addressLocality" )]
		public LanguageMap City { get; set; }

		[JsonProperty( "ceterms:addressRegion" )]
		public LanguageMap AddressRegion { get; set; }

		//
		[JsonProperty( "ceterms:postalCode" )]
		public string PostalCode { get; set; }

		[JsonProperty( "ceterms:addressCountry" )]
		public LanguageMap Country { get; set; }


		[JsonProperty( PropertyName = "ceterms:alternateName" )]
		public LanguageMapList AlternateName { get; set; } 

	}
}

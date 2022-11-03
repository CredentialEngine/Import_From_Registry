﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
	public class Place
	{
		public Place()
		{
			Type = "ceterms:Place";
			ContactPoint = new List<ContactPoint>();
		}
		[JsonProperty( "@type" )]
		public string Type { get; set; }

		[JsonProperty( "ceterms:geoURI" )]
		public string GeoURI { get; set; }

		[JsonProperty( PropertyName = "ceterms:name" )]
		public LanguageMap Name { get; set; }

		//2020-12-15 - adding back use of description
		[JsonProperty( PropertyName = "ceterms:description" )]
		public LanguageMap Description { get; set; }

		[JsonProperty( "ceterms:streetAddress" )]
		public LanguageMap StreetAddress { get; set; }

		[JsonProperty( "ceterms:postOfficeBoxNumber" )]
		public string PostOfficeBoxNumber { get; set; }

		[JsonProperty( "ceterms:addressLocality" )]
		public LanguageMap City { get; set; }

		[JsonProperty( "ceterms:addressRegion" )]
		public LanguageMap AddressRegion { get; set; }

		/// <summary>
		/// Named area or division within a region, such as a county in the U.S. or Canada.
		/// </summary>
		[JsonProperty( "ceterms:addressSubRegion" )]
		public LanguageMap SubRegion { get; set; }
		//
		[JsonProperty( "ceterms:postalCode" )]
		public string PostalCode { get; set; }

		[JsonProperty( "ceterms:addressCountry" )]
		public LanguageMap Country { get; set; }
		/// <summary>
		/// Identifier
		/// Definition:	Alphanumeric Identifier value.
		/// List of URIs 
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:identifier" )]
		public List<IdentifierValue> Identifier { get; set; }

		[JsonProperty( PropertyName = "ceterms:latitude" )]
		public double? Latitude { get; set; } = null;

		[JsonProperty( PropertyName = "ceterms:longitude" )]
		public double? Longitude { get; set; } = null;


		[JsonProperty( "ceterms:targetContactPoint" )]
		public List<ContactPoint> ContactPoint { get; set; }
	}
}

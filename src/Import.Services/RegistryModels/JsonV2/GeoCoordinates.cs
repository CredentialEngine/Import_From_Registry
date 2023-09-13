using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace RA.Models.Input
{

	public class GeoCoordinates
	{
		public GeoCoordinates()
		{
		}
		/// <summary>
		///  type
		/// </summary>
		[JsonProperty( "@type" )]
		public string Type { get; set; } = "ceterms:GeoCoordinates";

		/// <summary>
		/// Name
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:name" )]
		public LanguageMap Name { get; set; }

		[JsonProperty( PropertyName = "ceterms:alternateName" )]
		public LanguageMapList AlternateName { get; set; }

		[JsonProperty( PropertyName = "ceterms:iatitude " )]

		public double Latitude { get; set; }
		[JsonProperty( PropertyName = "ceterms:longitude " )]
		public double Longitude { get; set; }


		/// <summary>
		/// Geographic URI
		/// Entity that describes the longitude, latitude and other location details of a place.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:geoURI" )]
		public string GeoUri { get; set; }

	}

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Search.ThirdPartyApiModels
{
	public class GeoNames
	{
		//Returned by GeoNames API
		public class SearchResultRaw
		{
			public SearchResultRaw()
			{
				bbox = new BoundingBox();
			}
			public int geonameId { get; set; } //GeoNames ID
			public string name { get; set; } //Name of the place
			public string toponymName { get; set; } //Alternative name (?)
			public string adminName1 { get; set; } //State/province name
			public string adminCode1 { get; set; } //State/province abbreviation
			public string countryName { get; set; } //Name of the country
			public string countryCode { get; set; } //Country abbreviation
			public string lat { get; set; } //Latitude
			public string lng { get; set; } //Longitude
			public long population { get; set; } //Population Size
			public string countryId { get; set; } //GeoNames country ID. Appears to be an integer, but is returned as a string
			public string fclName { get; set; } //Type of place (e.g., "city, village...")
			public string fcl { get; set; } //Unknown
			public string fcode { get; set; } //Unknown
			public string fcodeName { get; set; } //Unknown
			public BoundingBox bbox { get; set; } //Bounding Box
		}
		//

		//Returned by GeoNames API
		public class SearchResultsRaw
		{
			public int totalResultsCount { get; set; }
			public List<SearchResultRaw> geonames { get; set; }
		}
		//

		public class BoundingBox
		{
			public decimal north { get; set; }
			public decimal south { get; set; }
			public decimal east { get; set; }
			public decimal west { get; set; }
		}
		//

	}

	public class GoogleGeocoding
	{
		//Result set returned by Google. Usually only one result is returned.
		public class Results
		{
			public Results()
			{
				results = new List<Result>();
			}
			public List<Result> results { get; set; }
			public string status { get; set; }

			//Shortcut
			public Location GetLocation()
			{
				try
				{
					return results.First().geometry.location;
				}
				catch
				{
					return new Location();
				}
			}
		}
		//

		//Individual result within the set.
		public class Result
		{
			public Result()
			{
				address_components = new List<AddressComponent>();
				geometry = new Geometry();
				types = new List<string>();
			}

			public List<AddressComponent> address_components { get; set; }
			public string formatted_address { get; set; }
			public Geometry geometry { get; set; }
			public bool partial_match { get; set; }
			public string place_id { get; set; }
			public List<string> types { get; set; }
		}
		//

		//Address item - seems to contain miscellaneous address information about the location including type of location, parent locations all the way up to country level, and zip code info
		public class AddressComponent
		{
			public AddressComponent()
			{
				types = new List<string>();
			}

			public string long_name { get; set; }
			public string short_name { get; set; }
			public List<string> types { get; set; }
		}
		//

		//Geometry information - the "location" field in here is usually what we're interested in
		public class Geometry
		{
			public Geometry()
			{
				location = new Location();
				viewport = new Viewport();
			}
			public Location location { get; set; }
			public string location_type { get; set; }
			public Viewport viewport { get; set; }
		}
		//

		//Location item, used in a few places
		public class Location
		{
			public double lat { get; set; }
			public double lng { get; set; }
		}
		//

		//Viewport data for setting a specific viewport over a location - may not be necessary client-side since the map can be centered over an arbitrary position with an arbitrary zoom level
		public class Viewport
		{
			public Viewport()
			{
				northeast = new Location();
				southwest = new Location();
			}
			public Location northeast { get; set; }
			public Location southwest { get; set; }
		}
		//

	}
}

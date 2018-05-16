using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;

using workIT.Models.Search.ThirdPartyApiModels;

namespace workIT.Utilities
{
	public class GeoServices
	{

		#region Google Maps

		public static string GetGoogleMapsApiKey()
		{
			return Utilities.ConfigHelper.GetApiKey( "GoogleMapsApiKey", "" );
		}
		//

		public static string GetGoogleGeocodingServerApiKey()
		{
			return Utilities.ConfigHelper.GetApiKey( "GoogleGeocodingServerApiKey", "" );
		}
		//

		public static GoogleGeocoding.Results GeocodeAddress( string address )
		{
			var key = GetGoogleGeocodingServerApiKey();
			var url = "https://maps.googleapis.com/maps/api/geocode/json?key=" + key + "&address=" + HttpUtility.UrlEncode( address );
			var rawData = MakeRequest( url );
			var results = new JavaScriptSerializer().Deserialize<GoogleGeocoding.Results>( rawData );
			return results;
		}
		//

		#endregion

		/// <summary>
		/// Generic method to make a request to a URL and return the raw response.
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		public static string MakeRequest( string url )
		{
			var getter = new HttpClient();
			var response = getter.GetAsync( url ).Result;
			var responseData = response.Content.ReadAsStringAsync().Result;

			return responseData;
		}
		//
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Web.Script.Serialization;

using System.Net.Http;
using workIT.Models.Search;
using workIT.Models.Common;
using workIT.Models.Helpers;
using workIT.Models.Search.ThirdPartyApiModels;
using workIT.Utilities;
//using workIT.Models.Helpers.Cass;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Serialization;
//using workIT.Utilities;

namespace workIT.Services
{
	public class ThirdPartyApiServices
	{

		#region GeoNames
		protected string allGeoNamesPlaces = "CONT,ADMD,ADM1,ADM2,PCL*,PPL*";

		/// <summary>
		/// Search GeoNames for a list of places that roughly match the query. Returns the raw response from GeoNames API.
		/// </summary>
		/// <param name="query"></param>
		/// <returns></returns>
		public string GeoNamesSearch( string query, bool includeBoundingBox = false )
		{
			return GeoNamesSearch( query, allGeoNamesPlaces.Split( ',' ).ToList(), 1, 5, includeBoundingBox );
		}
		//

		/// <summary>
		/// Search GeoNames for a list of places that roughly match the query. Returns the raw response from GeoNames API.
		/// References feature codes found at http://www.geonames.org/export/codes.html
		/// </summary>
		/// <param name="query"></param>
		/// <param name="locationType"></param>
		/// <param name="pageNumber"></param>
		/// <param name="pageSize"></param>
		/// <param name="includeBoundingBox"></param>
		/// <returns></returns>
		public string GeoNamesSearch( string query, List<string> locationType, int pageNumber, int pageSize, bool includeBoundingBox )
		{
			var featureCodes = "";
			if ( locationType == null )
			{
				locationType = allGeoNamesPlaces.Split( ',' ).ToList();
			}
			foreach ( var item in locationType )
			{
				featureCodes = featureCodes + "&featureCode=" + item;
			}
			//var username = workIT.Utilities.ConfigHelper.GetApiKey( "GeoNamesUserName", "" );
			var username = UtilityManager.GetAppKeyValue( "GeoNamesUserName", "" );
			//if ( string.IsNullOrWhiteSpace( username ) )
			//	username = UtilityManager.GetAppKeyValue( "GeoNamesUserName" );
			var text = HttpUtility.UrlEncode( query );
			var url = "http://api.geonames.org/searchJSON?q=" + text + "&username=" + username + "&fuzzy=0.7&maxRows=" + pageSize + "&startRow=" + ((pageNumber -1) * pageSize) + "&countryBias=US" + featureCodes + (includeBoundingBox ? "&inclBbox=true" : "");

			return MakeRequest( url );
		}
		//

		/// <summary>
		/// Search GeoNames for a list of places that roughly match the query. Returns a list of GeoCoordinates and sets a reference variable for total results.
		/// </summary>
		/// <param name="query"></param>
		/// <param name="pageNumber"></param>
		/// <param name="pageSize"></param>
		/// <param name="totalResults"></param>
		/// <returns></returns>
		public List<GeoCoordinates> GeoNamesSearch( string query, int pageNumber, int pageSize, List<string> locationType, ref int totalResults, bool includeBoundingBox )
		{
			var output = new List<GeoCoordinates>();
			var rawData = GeoNamesSearch( query, locationType, pageNumber, pageSize, includeBoundingBox );
			var data = new JavaScriptSerializer().Deserialize<GeoNames.SearchResultsRaw>( rawData );

			totalResults = data.totalResultsCount;
            if ( data.geonames == null )
                return output;

			foreach ( var result in data.geonames )
			{
				var newResult = new GeoCoordinates
				{
					GeoNamesId = result.geonameId,
					Name = result.name,
					ToponymName = result.toponymName,
					Region = result.adminName1,
					Country = result.countryName,
					Latitude = double.Parse( result.lat ),
					Longitude = double.Parse( result.lng ),
					GeoURI = "http://geonames.org/" + result.geonameId + "/"
				};
				if ( includeBoundingBox )
				{
					try
					{
						newResult.Bounds = new BoundingBox()
						{
							North = result.bbox.north,
							East = result.bbox.east,
							West = result.bbox.west,
							South = result.bbox.south
						};
					}
					catch { }
				}

				output.Add( newResult );
			}

			return output;
		}
		//
		public static GeoCoordinates GeoNamesGet( string geoNamesId, bool includeBoundingBox = false )
		{
			////the import would not have access to keys.config, so always use UtilityManager.GetAppKey
			var username = UtilityManager.GetAppKeyValue( "GeoNamesUserName" );
			//var username = workIT.Utilities.ConfigHelper.GetApiKey( "GeoNamesUserName", "" );
			
			//if ( string.IsNullOrWhiteSpace( username ) )
			//	username = UtilityManager.GetAppKeyValue( "GeoNamesUserName" );

			var output = new GeoCoordinates();
			var url = "http://api.geonames.org/getJSON?geonameId=" + geoNamesId + "&username=" + username ;

			string rawData = MakeRequest( url );
			GeoNames.SearchResultRaw data = new JavaScriptSerializer().Deserialize<GeoNames.SearchResultRaw>( rawData );

			if ( data == null || data.name == null )
				return output;

			var newResult = new GeoCoordinates
			{
				GeoNamesId = data.geonameId,
				Name = data.name,
				ToponymName = data.toponymName,
				Region = data.adminName1,
				Country = data.countryName,
				Latitude = double.Parse( data.lat ),
				Longitude = double.Parse( data.lng ),
				GeoURI = "http://geonames.org/" + data.geonameId + "/"
			};
			if ( includeBoundingBox )
			{
				try
				{
					newResult.Bounds = new BoundingBox()
					{
						North = data.bbox.north,
						East = data.bbox.east,
						West = data.bbox.west,
						South = data.bbox.south
					};
				}
				catch { }
			}


			return newResult;
		}
		#endregion

		#region Google Maps

		public string GetGoogleMapsApiKey()
		{
			//return workIT.Utilities.ConfigHelper.GetApiKey( "GoogleMapsApiKey", "" );
			return UtilityManager.GetAppKeyValue( "GoogleMapsApiKey", "" );
		}
		//

		public string GetGoogleGeocodingServerApiKey()
		{
			//return workIT.Utilities.ConfigHelper.GetApiKey( "GoogleGeocodingServerApiKey", "" );
			return UtilityManager.GetAppKeyValue( "GoogleGeocodingServerApiKey", "" );
		}
		//

		public GoogleGeocoding.Results GeocodeAddress( string address )
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
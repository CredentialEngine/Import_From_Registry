using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using CredentialFinderWebAPI.Models;

using workIT.Models.Helpers;
using workIT.Models.Search;

using workIT.Services;
using workIT.Utilities;
using System.Web;

namespace CredentialFinderWebAPI.Controllers
{
	public class SearchController : ApiController
	{
		SearchServices searchService = new SearchServices();
		bool valid = true;
		string status = "";

		/// <summary>
		/// blind credential search
		/// </summary>
		[HttpGet, Route( "Search" )]
		public void Index()
		{
			MainSearchInput query = new MainSearchInput()
			{
				SearchType = "credential",
				StartPage = 1,
				PageSize = 25,
				SortOrder = "newest"
			};

			var results = searchService.MainSearch( query, ref valid, ref status );

			var finalResult = JObject.FromObject( new { data = results, valid = valid, status = status } );

			//var result = MainSearch( query );
			HttpContext.Current.Response.Clear();
			HttpContext.Current.Response.ContentType = "application/json";
			HttpContext.Current.Response.Write( finalResult );
			HttpContext.Current.Response.End();
		}


		[HttpGet, Route( "Search/{searchType}" )]
		public void Search2( string searchType = "" )
		{
			if ( string.IsNullOrWhiteSpace( searchType ) )
				searchType = "credential";
			MainSearchInput query = new MainSearchInput()
			{
				SearchType = searchType,
				StartPage = 1,
				PageSize = 25,
				SortOrder = "newest"
			};

			var results = searchService.MainSearch( query, ref valid, ref status );

			var finalResult = JObject.FromObject( new { data = results, valid = valid, status = status } );

			//var result = MainSearch( query );
			HttpContext.Current.Response.Clear();
			HttpContext.Current.Response.ContentType = "application/json";
			HttpContext.Current.Response.Write( finalResult );
			HttpContext.Current.Response.End();
		}


		[HttpPost, Route( "Search/query" )]
		public void MainSearch( MainSearchInput searchQuery )
		{
			var response = new ApiResponse();
			try
			{
				var results = searchService.MainSearch( searchQuery, ref valid, ref status );

				var finalResult = JObject.FromObject( new { data = results, valid = valid, status = status } );

				HttpContext.Current.Response.Clear();
				HttpContext.Current.Response.ContentType = "application/json";
				HttpContext.Current.Response.Write( finalResult );
				HttpContext.Current.Response.End();
			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, "CredentialFinderWebAPI.Controllers. Exception: " + ex.Message );
				//return something
				HttpContext.Current.Response.Clear();
				HttpContext.Current.Response.ContentType = "application/json";
				HttpContext.Current.Response.Write( ex.Message );
				HttpContext.Current.Response.End();
			}

		}
		//[HttpPost, Route( "Search/main2" )]
		//public ApiResponse MainSearch2( MainSearchInput query )
		//{
		//	var response = new ApiResponse();
		//	try
		//	{
		//		var results = searchService.MainSearch( query, ref valid, ref status );

		//		var finalResult = JObject.FromObject( new { data = results, valid = valid, status = status } );
		//		//Response.ContentType = "application/json";
		//		//Response.ContentEncoding = Encoding.UTF8;
		//		response.Result = finalResult.ToString( Formatting.None );
		//		response.Successful = true;
		//	}
		//	catch ( Exception ex )
		//	{
		//		LoggingHelper.DoTrace( 1, "CredentialFinderWebAPI.Controllers. Exception: " + ex.Message );
		//		//return something
		//		response.Messages.Add( ex.Message );
		//	}

		//	return response;
		//}

		//Do a MicroSearch
		//public JsonResult DoMicroSearch( MicroSearchInputV2 query )
		//{
		//	var totalResults = 0;
		//	var data = MicroSearchServicesV2.DoMicroSearch( query, ref totalResults, ref valid, ref status );

		//	return JsonHelper.GetJsonWithWrapper( data, valid, status, totalResults );
		//}
	}
}

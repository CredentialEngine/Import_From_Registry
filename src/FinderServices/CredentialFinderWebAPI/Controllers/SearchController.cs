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
	public class SearchController : BaseController
	{
		SearchServices searchService = new SearchServices();
		bool valid = true;
		string status = "";

		[HttpGet, Route( "Search/Initialize/{searchType}" )]
		public void SearchInitialize( string searchType = "", bool getAll=false )
		{
			if ( string.IsNullOrWhiteSpace( searchType ) )
				searchType = "credential";
			List<string> messages = new List<string>();
			var response = new ApiResponse();

			var results = new JObject(); 
			switch ( searchType.ToLower() )
			{
				case "credential": 
					//this needs to be a generic class
					var credentialFilters =SearchServices.GetCredentialFilters( getAll );
					response.Result = credentialFilters;
					break;
				case "organization":
				{
					var filters = SearchServices.GetOrganizationFilters( getAll );
					response.Result = filters;
					break;
				}
				case "assessment":
					//this needs to be a generic class
					var asmtFilters = SearchServices.GetAssessmentFilters( getAll );
					response.Result = asmtFilters;
					break;
				case "learningopportunity":
				{
					var loppFilters = SearchServices.GetLearningOppFilters( getAll );
					response.Result = loppFilters;
					break;
				}
				case "pathway":
				{
					var pwFilters = SearchServices.GetPathwayFilters( getAll );
					response.Result = pwFilters;
					break;
				}
				case "competencyframework":
				{
					var pwFilters = SearchServices.GetNoFilters( "Competency Framework" );
					response.Result = pwFilters;
					break;
				}
				case "transfervalue":
				{
					var pwFilters = SearchServices.GetNoFilters( "Transfer Value" );
					response.Result = pwFilters;
					break;
				}
				default:
				{
					valid = false;
					messages.Add( "Unknown search mode: " + searchType);
					break;
				}
			}
			if ( messages.Any() )
			{
				SendResponse( messages );
			}
			else
			{
				response.Successful = true;
				//response.Result = results;
				//var finalResult = JObject.FromObject( new { data = results, valid = valid, status = status } );
				SendResponse( response );
			}
			

		}


		//Do an autocomplete
		[HttpGet, Route( "Search/autocomplete/{searchType}/{context}/{text}" )]

		public void AutoComplete( string searchType, string context, string text, int widgetId = 0 )
		{
			var response = new ApiResponse();
			var status = "";
			var results = SearchServices.DoAutoComplete( searchType, context, text, widgetId );

			var autoCompleteResults = results.Select( m => new FilterItem() { Label = m.ToString() } ).ToList();

			//return JsonHelper.GetJsonWithWrapper( data, true, "", null );
			//var finalResult = JObject.FromObject( new { data = results, valid = valid, status = status } );
			//SendResponse( finalResult );

			response.Successful = true;
			response.Result = autoCompleteResults;
			SendResponse( response );
		}
		//
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

			SendResponse( finalResult );

			//HttpContext.Current.Response.Clear();
			//HttpContext.Current.Response.ContentType = "application/json";
			//HttpContext.Current.Response.Write( finalResult );
			//HttpContext.Current.Response.End();
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
			SendResponse( finalResult );

		}


		[HttpPost, Route( "Search/query" )]
		public void MainSearch( MainSearchInput searchQuery )
		{
			var response = new ApiResponse();
			try
			{
				var results = searchService.MainSearch( searchQuery, ref valid, ref status );

				var finalResult = JObject.FromObject( new { data = results, valid = valid, status = status } );
				SendResponse( finalResult );

			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, "CredentialFinderWebAPI.Controllers. Exception: " + ex.Message );
				//return something - may want to wrap up pretty
				SendResponse( "Error encountered: " +  ex.Message );

				//HttpContext.Current.Response.Clear();
				//HttpContext.Current.Response.ContentType = "application/json";
				//HttpContext.Current.Response.Write( ex.Message );
				//HttpContext.Current.Response.End();
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

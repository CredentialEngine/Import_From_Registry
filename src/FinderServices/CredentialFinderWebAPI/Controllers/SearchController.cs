using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Http;

using CredentialFinderWebAPI.Models;

using Newtonsoft.Json.Linq;

using workIT.Models.Search;
using workIT.Services;
using workIT.Utilities;

using API = workIT.Services.API;
using APIModels = workIT.Models.API;


namespace CredentialFinderWebAPI.Controllers
{
	public class SearchController : BaseController
	{
		SearchServices searchService = new SearchServices();
		bool valid = true;
		string status = "";


		#region Initialize
		[HttpPost, Route( "Search/load" )]
		public void Load( SearchInitialize query )
		{
			if ( query == null )
			{
				query = new SearchInitialize() { SearchType = "credential" };
			}

			if ( string.IsNullOrWhiteSpace( query.SearchType ) )
				query.SearchType = "credential";

			DoInitialize( query );
		}

		[HttpGet, Route( "Search/Initialize/{searchType}" )]
		public void SearchInitialize( string searchType = "", bool getAll = false )
		{
			if ( string.IsNullOrWhiteSpace( searchType ) )
				searchType = "credential";

			SearchInitialize query = new SearchInitialize() { SearchType = searchType };
			DoInitialize( query );
			

		}

		private void DoInitialize( SearchInitialize query )
		{
			if ( query == null )
			{
				query = new SearchInitialize() { SearchType = "credential" };
			}

			if ( string.IsNullOrWhiteSpace( query.SearchType ) )
				query.SearchType = "credential";
			List<string> messages = new List<string>();
			var response = new ApiResponse();
			var searchType = query.SearchType.ToLower();
			var results = new JObject();
			switch ( searchType.ToLower() )
			{
				case "credential":
					//this needs to be a generic class
					var credentialFilters = API.SearchServices.GetCredentialFilters( query.GetAll );
					response.Result = credentialFilters;
					break;
				case "organization":
				{
					var filters = API.SearchServices.GetOrganizationFilters( query.GetAll );
					response.Result = filters;
					break;
				}
				case "assessment":
					//this needs to be a generic class
					var asmtFilters = API.SearchServices.GetAssessmentFilters( query.GetAll );
					response.Result = asmtFilters;
					break;
				case "learningopportunity":
				{
					var loppFilters = API.SearchServices.GetLearningOppFilters( query.GetAll );
					response.Result = loppFilters;
					break;
				}
				case "pathway":
				{
					var pwFilters = API.SearchServices.GetPathwayFilters( query.GetAll );
					response.Result = pwFilters;
					break;
				}
				case "competencyframework":
				{
					var pwFilters = API.SearchServices.GetNoFilters( "Competency Framework" );
					response.Result = pwFilters;
					break;
				}
				case "conceptscheme":
				{
					var pwFilters = API.SearchServices.GetNoFilters( "Concept Scheme" );
					response.Result = pwFilters;
					break;
				}
				case "transfervalue":
				{
					var pwFilters = API.SearchServices.GetNoFilters( "Transfer Value" );
					response.Result = pwFilters;
					break;
				}
				case "pathwayset":
				{
					var pwFilters = API.SearchServices.GetNoFilters( "Pathway Set" );
					response.Result = pwFilters;
					break;
				}
				default:
				{
					valid = false;
					messages.Add( "Unknown search mode: " + searchType );
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
		#endregion

		#region Autocomplete
		//Do an autocomplete
		[HttpPost, Route( "search/autocomplete" )]
		public ApiResponse AutoCompletePost( AutoCompleteQuery query )
		{
			return DoAutoComplete( query );
		}
		[HttpGet, Route( "search/autocomplete/{searchType}/{context}/{text}" )]
		public ApiResponse AutoCompleteGet( string searchType, string context, string text, int widgetId = 0 )
		{
			return DoAutoComplete( new AutoCompleteQuery() { SearchType = searchType, FilterURI = context, Text = text, WidgetId = widgetId } );
		}

		private ApiResponse DoAutoComplete( AutoCompleteQuery query )
		{
			query = query ?? new AutoCompleteQuery() { SearchType = "credential" };

			try
			{
				//HttpContext.Current.Response.Clear();
				//HttpContext.Current.Response.BufferOutput = true;
				//HttpContext.Current.Response.AppendHeader( "Access-Control-Allow-Origin", "*" );
				//string contentType = "application/json";
				//HttpContext.Current.Response.ContentType = contentType;
				//HttpContext.Current.Response.ContentEncoding = contentType == "application/json" ? Encoding.UTF8 : HttpContext.Current.Response.ContentEncoding;

				var results = SearchServices.DoAutoComplete( query.SearchType, query.FilterURI, query.Text, query.WidgetId );
				var acr = new AutoCompleteResponse();
				acr.Items = results.Select( m => new FilterItem() { Label = m.ToString(), Text = m.ToString(), URI =query.FilterURI, InterfaceType = "interfaceType:Text" } ).ToList();
				return new ApiResponse( acr, true, null );
			}
			catch( Exception ex )
			{
				return new ApiResponse( null, false, new List<string>() { "Error processing autocomplete request: " + ex.Message } );
			}
		}
		#endregion
		//


		[HttpPost, Route( "Search/" )]
		public ApiResponse Search( MainQuery query )
		{
			var debug = new JObject();
			try
			{
				var translatedQuery = API.SearchServices.TranslateMainQueryToMainSearchInput( query );
				var results = searchService.MainSearch( translatedQuery, ref valid, ref status, debug );
				var translatedResults = API.SearchServices.TranslateMainSearchResultsToAPIResults( results, debug );
				return new ApiResponse( translatedResults, true, null );
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "CredentialFinderWebAPI.Search. " + ex.Message + "\n\n" + debug.ToString() );
				return new ApiResponse( debug, false, new List<string>() { string.Format( "Error encountered returning data. {0} ", ex.Message ), "See debug object for details." } );
			}

		}

		[HttpGet, Route( "Search/{query}" )]
		public void Index( MainQuery query )
		{
			Search( query );
		}
		[HttpPost, Route( "Search/query" )]
		public void MainSearchOld( MainSearchInput searchQuery )
		{
			var response = new ApiResponse();
			try
			{
				var results = searchService.MainSearch( searchQuery, ref valid, ref status );

				//var finalResult = JObject.FromObject( new { data = results, valid = valid, status = status } );
				response.Successful = true;
				response.Result = results;
				SendResponse( response );

			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "CredentialFinderWebAPI.MainSearchOld. " + ex.Message );
				response.Messages.Add( string.Format( "Error encountered returning data. {0} ", ex.Message ) );
				response.Successful = false;
				SendResponse( response );
			}

		}

		/// <summary>
		/// blind credential search
		/// </summary>
		[HttpGet, Route( "SearchOld" )]
		public void SearchOld()
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

		[HttpPost, Route( "Search/GetTagSetItems" )]
		public ApiResponse GetTagSetItems( APIModels.TagSetRequest request )
		{
			var tagSetItems = API.SearchServices.GetTagSetItems( request );
			return new ApiResponse( tagSetItems, true, null );
		}
		//

	}

	public class SearchInitialize
	{
		public string SearchType { get; set; }
		public bool GetAll { get; set; } = false;
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http;

using CredentialFinderWebAPI.Models;

using Newtonsoft.Json;
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
			//TODO - add config to get all if there is a problem with the code table counts 
			if ( UtilityManager.GetAppKeyValue( "searchDisplayingAllFilters", false ) )
				query.GetAll = true;
			DoInitialize( query );
		}

		[HttpGet, Route( "Search/Initialize/{searchType}" )]
		public void SearchInitialize( string searchType = "", string widgetId = "" )
		{
			if ( string.IsNullOrWhiteSpace( searchType ) )
				searchType = "credential";

			SearchInitialize query = new SearchInitialize() { SearchType = searchType };
			DoInitialize( query, widgetId );
			

		}

		private void DoInitialize( SearchInitialize query, string widgetIdParm = "" )
		{
			string widgetId = "";
			if ( !string.IsNullOrWhiteSpace( widgetIdParm) )
				widgetId = widgetIdParm;

			if ( query == null )
			{
				query = new SearchInitialize() { SearchType = "credential" };
			}
			//test - probably need to hit the legacy site
			//var user = AccountServices.GetCurrentUser();

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
                case "learningprogram":
                case "course":
                case "trainingprogram":
                    {
					var loppFilters = API.SearchServices.GetLearningOppFilters( query.GetAll, widgetId );
					response.Result = loppFilters;
					break;
				}
				case "pathway":
				{
					var pwFilters = API.SearchServices.GetPathwayFilters( query.GetAll );
					response.Result = pwFilters;
					break;
				}
				case "collection":
					{
						var pwFilters = API.SearchServices.GetCollectionFilters( query.GetAll, widgetId );
						response.Result = pwFilters;
						break;
					}
				case "competencyframework":
				{
					var filters = API.SearchServices.GetNoFilters( "Competency Framework" );
					response.Result = filters;
					break;
				}
				case "conceptscheme":
				{
					var pwFilters = API.SearchServices.GetNoFilters( "Concept Scheme" );
					response.Result = pwFilters;
					break;
				}
				case "outcomedata":
				{
					//var filters = API.SearchServices.GetOutcomeDataFilters( query.GetAll );
					var filters = API.SearchServices.GetNoFilters( searchType );
					response.Result = filters;
					break;
				}
				case "scheduledoffering":
                    {
                        var filters = API.SearchServices.GetScheduledOfferingFilters( query.GetAll );
                        response.Result = filters;
                        break;
                    }
                case "supportservice":
                    {
                        var filters = API.SearchServices.GetSupportServiceFilters( query.GetAll );
                        response.Result = filters;
                        break;
                    }
                case "transferintermediary":
					{
						var filters = API.SearchServices.GetNoFilters( "Transfer Intermediary" );
						response.Result = filters;
						break;
					}
				case "transfervalue":
				{
					var tvFilters = API.SearchServices.GetTransferValueFilters( query.GetAll );
					response.Result = tvFilters;
					break;
				}
				case "pathwayset":
				{
					var pwFilters = API.SearchServices.GetNoFilters( "Pathway Set" );
					response.Result = pwFilters;
					break;
				}
				case "progressionmodel":
					{
						var pwFilters = API.SearchServices.GetNoFilters( "Progression Model" );
						response.Result = pwFilters;
						break;
					}
				case "job":
				case "occupation":
				case "rubric":
				case "task":
				case "workrole":
					{
						var pwFilters = API.SearchServices.GetNoFilters( searchType );
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
		public ApiResponse AutoCompletePost()
		{
			HttpContent requestContent = Request.Content;
			string jsonContent = requestContent.ReadAsStringAsync().Result;

			//determine format of request
			if (jsonContent.IndexOf( "\"MainFilters\"" ) > 0)
            {
				var request = JsonConvert.DeserializeObject<MainQuery>( jsonContent );
				return DoAutoComplete( request );
			} else
            {
				var request = JsonConvert.DeserializeObject<AutoCompleteQuery>( jsonContent );
				return DoAutoCompleteOld( request );
			}
			
		}

		//[HttpPost, Route( "search/autocomplete" )]
		//public ApiResponse AutoCompletePost( MainQuery query )
		//{
		//	HttpContent requestContent = Request.Content;
		//	string jsonContent = requestContent.ReadAsStringAsync().Result;

		//	//determine format of request
		//	if ( jsonContent.IndexOf( "" ) > 0 )
		//	{
		//		var request = JsonConvert.DeserializeObject<MainQuery>( jsonContent );
		//		return DoAutoComplete( query );
		//	}
		//	else
		//	{
		//		var request = JsonConvert.DeserializeObject<AutoCompleteQuery>( jsonContent );
		//		return DoAutoCompleteOld( query );
		//	}

		//}
		private ApiResponse DoAutoComplete( MainQuery query )
		{
			query = query ?? new MainQuery() { SearchType = "credential" };
			var debug = new JObject();
			bool valid = true;
			string status = "";
			var queryFilterURI = query.AutocompleteContext;
			try
			{
				var translatedQuery = API.SearchServices.TranslateMainQueryToMainSearchInput( query, debug );

				var results = new SearchServices().DoAutoComplete( translatedQuery, ref valid, ref status );
				var acr = new AutoCompleteResponse();

				acr.Items = results.Select( m => new FilterItem() { Id = m.Id, Label = m.Label, Text = m.Text, URI = queryFilterURI, InterfaceType = "interfaceType:Text" } ).ToList();

				return new ApiResponse( acr, true, null );
			}
			catch ( Exception ex )
			{
				return new ApiResponse( null, false, new List<string>() { "Error processing autocomplete request: " + ex.Message } );
			}
		}


		[HttpPost, Route( "search/autocompleteold" )]
		public ApiResponse AutoCompletePost( AutoCompleteQuery query )
		{
			return DoAutoCompleteOld( query );
		}
		//[HttpGet, Route( "search/autocomplete/{searchType}/{context}/{text}" )]
		//public ApiResponse AutoCompleteGet( string searchType, string context, string text, int widgetId = 0 )
		//{
		//	return DoAutoCompleteOld( new AutoCompleteQuery() { SearchType = searchType, FilterURI = context, Text = text, WidgetId = widgetId } );
		//}

		private ApiResponse DoAutoCompleteOld( AutoCompleteQuery query )
		{
			query = query ?? new AutoCompleteQuery() { SearchType = "credential" };

			try
			{
				if ( query.FilterItemURI == null)
					query.FilterItemURI = "interfaceType:Text";
				query.Text = string.IsNullOrWhiteSpace( query.Text ) ? "" : query.Text;
				if (query.FilterURI == "mainsearch")
                {

                } else if ( query.FilterURI == null )
                {
					
					if (query.FilterURI == null )
                    {
						//guess while working on bug
						if (query.SearchType == "learningopportunity" )
                        {
							query.FilterURI = "filter:occupationtype";
                        }
                    }

				}
				var results = new SearchServices().DoAutoCompleteOLD( query.SearchType, query.FilterURI, query.Text, query.WidgetId );
				var acr = new AutoCompleteResponse();
				acr.Items = results.Select( m => new FilterItem() { Id = m.Id, Label = m.Label, Text = m.Text, URI = query.FilterURI, InterfaceType = "interfaceType:Text" } ).ToList();
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
                if ( query.PageSize == 0 )
                    query.PageSize = 25;
				var translatedQuery = API.SearchServices.TranslateMainQueryToMainSearchInput( query, debug );
				var results = searchService.MainSearch( translatedQuery, ref valid, ref status, debug );
				var translatedResults = API.SearchServices.TranslateMainSearchResultsToAPIResults( results, debug );
				translatedResults.Debug = new JObject()
				{
					{ "General Debug", debug },
					{ "Raw Results Debug", results != null ? results.Debug : null },
					{ "Translated Results Debug", translatedResults.Debug }
				};

				return new ApiResponse( translatedResults, true, null );
			}
			catch ( Exception ex )
			{
				string jsoninput = JsonConvert.SerializeObject( query, JsonHelper.GetJsonSettings() );

				LoggingHelper.LogError( ex, "CredentialFinderWebAPI.Search. " + ex.Message + "\nQuery\n" + jsoninput + "\n\n" + debug.ToString() );
				return new ApiResponse( debug, false, new List<string>() { string.Format( "Error encountered returning data. {0} ", ex.Message ), "See debug object for details." } );
			}

		}
		//

		//Find Locations
		[HttpPost, Route( "Search/Location" )]
		public ApiResponse FindLocations( MapQuery query )
		{
			var total = 0;
			var data = new ThirdPartyApiServices().GeoNamesSearch( query.Text, 1, 15, null, ref total, true );
			var translated = API.SearchServices.TranslateGeoCoordinatesListToMapFilterList( data );
			return new ApiResponse( translated, true, null );
		}


		/// <summary>
		/// Get version for quick testing
		/// 
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		[HttpGet, Route( "Location/{text}" )]
		public ApiResponse FindLocation( string text )
		{
			var total = 0;
			var data = new ThirdPartyApiServices().GeoNamesSearch( text, 1, 15, null, ref total, true );
			var translated = API.SearchServices.TranslateGeoCoordinatesListToMapFilterList( data );
			return new ApiResponse( translated, true, null );
		}

		[HttpPost, Route( "Search/GetTagSetItems" )]
		public ApiResponse GetTagSetItems( APIModels.TagSetRequest request )
		{
			var debug = new JObject();
			try
			{
				var tagSetItems = API.SearchServices.GetTagSetItems( request, 10, debug );
				return new ApiResponse( tagSetItems, true, null );
			}
			catch ( Exception ex )
			{
				return new ApiResponse( null, false, new List<string>()
				{
					"Error processing request: " + ex.Message,
					"Inner Exception: " + ex.InnerException?.Message,
					"Debugging: " + debug.ToString( Formatting.None )
				} );
			}
		}
		//

		#region prototypes/ old search
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


		//[HttpGet, Route( "Search/{searchType}" )]
		//public void Search2( string searchType = "" )
		//{
		//	if ( string.IsNullOrWhiteSpace( searchType ) )
		//		searchType = "credential";
		//	MainSearchInput query = new MainSearchInput()
		//	{
		//		SearchType = searchType,
		//		StartPage = 1,
		//		PageSize = 25,
		//		SortOrder = "newest"
		//	};

		//	var results = searchService.MainSearch( query, ref valid, ref status );

		//	var finalResult = JObject.FromObject( new { data = results, valid = valid, status = status } );
		//	SendResponse( finalResult );

		//}


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

		#endregion
	}

	public class SearchInitialize
	{
		public string SearchType { get; set; }
		public bool GetAll { get; set; } = false;
	}
}

using System;
using System.Collections.Generic;
using System.Web.Mvc;
using workIT.Models.Search;
using workIT.Services;
using workIT.Models.Helpers;

//using Nest;
//using workIT.Factories;
//using System.Linq;
//using workIT.Models;
//using workIT.Models.Common;
using workIT.Utilities;

using Newtonsoft.Json.Linq;
using System.Text;

namespace CredentialFinderWeb.Controllers
{
    public class SearchController : Controller
    {
        SearchServices searchService = new SearchServices();
        bool valid = true;
        string status = "";

        //Main Search Page
        public ActionResult Index()
        {
            //if ( Request.Params[ "oldSearch" ] == "yes" )
            //{
            //    return View( "~/Views/Search/Index.cshtml" );
            //}
            //else
                return View( "~/Views/Search/SearchV2.cshtml" );
        }

		/// <summary>
		/// Endpoint friendly urls like /search/credentials
		/// </summary>
		/// <param name="searchType"></param>
		/// <returns></returns>
		public ActionResult Index2( string searchType )
		{
			if ( string.IsNullOrWhiteSpace( searchType ) )
				searchType = "credential";
			ViewBag.SearchType = searchType;

			return View( "~/Views/Search/SearchV2.cshtml" );

		}

		public JsonResult DoAutoComplete( string text, string context, string searchType, int widgetId = 0 )
        {
            var data = SearchServices.DoAutoComplete( text, context, searchType, widgetId );

            return JsonHelper.GetJsonWithWrapper( data, true, "", null );
        }

        //Original
		[HttpPost]
        public string MainSearch( MainSearchInput query )
        {
            //DateTime start = DateTime.Now;
            //LoggingHelper.DoTrace( 6, string.Format( "$$$$SearchController.MainSearch === Started: " ) );
            if ( Request.Params[ "useSql" ] == "true" )
            {
                //query.useSql = true;
                //query.useElastic = false;
            }

            //if ( Request.Params["elastic"] == "true" ) query.ElasticSearch = true;


            var results = searchService.MainSearch( query, ref valid, ref status );

			//TimeSpan timeDifference = start.Subtract( DateTime.Now );
			//LoggingHelper.DoTrace( 6, string.Format( "$$$$SearchController.MainSearch === Ended - Elapsed: {0}", timeDifference.TotalSeconds ) );

			//Use a different return structure here because the default serialization method screws up the internal JObjects/JArrays/etc
			var finalResult = JObject.FromObject( new { data = results, valid = valid, status = status } );
			Response.ContentType = "application/json";
			Response.ContentEncoding = Encoding.UTF8;
			return finalResult.ToString();

        }

        //public ActionResult Filter( string keywords )
        //{
        //    var response = ElasticClient.Search<MainSearchResult>( body => body.Query( query => query.QueryString( qs => qs.Query( keywords ) ) ) );
        //    return Json( response.Documents, JsonRequestBehavior.AllowGet );
        //}

        //Do a MicroSearch
        public JsonResult DoMicroSearch( MicroSearchInputV2 query )
        {
            var totalResults = 0;
            var data = MicroSearchServicesV2.DoMicroSearch( query, ref totalResults, ref valid, ref status );

            return JsonHelper.GetJsonWithWrapper( data, valid, status, totalResults );
        }
        //

        //Find Locations
        public JsonResult FindLocations( string text )
        {
            var total = 0;
            var data = new ThirdPartyApiServices().GeoNamesSearch( text, 1, 5, null, ref total, true );

            return JsonHelper.GetJsonWithWrapper( data, valid, status, total );
        }
        //

        //Get Tag Item data
        public JsonResult GetTagItemData( string searchType, string entityType, int recordID, int maxRecords = 10 )
        {
            try
            {
                var data = SearchServices.GetTagSet( searchType, ( SearchServices.TagTypes )Enum.Parse( typeof( SearchServices.TagTypes ), entityType, true ), recordID, maxRecords );
                return JsonHelper.GetJsonWithWrapper( data, true, "", null );
            }
            catch
            {
                return JsonHelper.GetJsonWithWrapper( null, false, "Invalid entity type", null );
            }
        }

        //Get Tag Item Data (Used with V2 tag items)
        public JsonResult GetTagsV2Data( string AjaxQueryName, int RecordId, string SearchType, string TargetEntityType, int MaxRecords = 10 )
        {
            try
            {
                var tagType = ( SearchServices.TagTypes )Enum.Parse( typeof( SearchServices.TagTypes ), TargetEntityType, true );
                var data = SearchServices.GetTagSet( SearchType, tagType, RecordId, MaxRecords );
                var items = new List<SearchTagItem>();

                switch ( AjaxQueryName )
                {
                    case "GetSearchResultCompetencies":
                    {
                        items = data.Items.ConvertAll( m => new SearchTagItem()
                        {
                            Display = string.IsNullOrWhiteSpace( m.Description ) ? m.Label : "<b>" + m.Label + "</b>" + System.Environment.NewLine + m.Description,
                            QueryValues = new Dictionary<string, object>()
                                {
                                    { "SchemaName", m.Schema },
                                    { "CodeId", m.CodeId },
                                    { "TextValue", m.Label },
                                    { "TextDescription", m.Description }
                                }
                        } );
                        break;
                    }
                    case "GetSearchResultCosts":
                    {
                        items = data.CostItems.ConvertAll( m => new SearchTagItem()
                        {
                            Display = m.CostType + ": " + m.CurrencySymbol + m.Price + " ( " + ( m.SourceEntity ?? "direct" ) + " )",
                            QueryValues = new Dictionary<string, object>()
                                {
                                    { "CurrencySymbol", m.CurrencySymbol },
                                    { "Price", m.Price },
                                    { "CostType", m.CostType }
                                }
                            //Something that probably looks like that -^
                        } );
                        break;
                    }
                    case "GetSearchResultPerformed":
                    {
                        items = data.QAItems.ConvertAll( m => new SearchTagItem()
                        {
                            Display = "<b>" + m.AgentToTargetRelationship + "</b> <b>" + m.TargetEntityType + "</b> " + m.TargetEntityName,
                            QueryValues = new Dictionary<string, object>()
                                {
                                    { "SearchType", m.TargetEntityType.ToLower() },
                                    { "RecordId", m.TargetEntityBaseId },
                                    { "TargetEntityType", m.TargetEntityType },
                                    { "IsReference", m.IsReference },
                                    {"SubjectWebpage", m.TargetEntitySubjectWebpage },
                                    {"TargetEntityTypeId", m.TargetEntityTypeId },
                                    {"AssertionTypeId",m.AssertionTypeId }
                                }
                        } );
                        break;
                    }

                    case "CredentialConnections":
                    {
                        items = data.CostItems.ConvertAll( m => new SearchTagItem()
                        {
                            Display = m.CostType + ": " + m.CurrencySymbol + m.Price + " ( " + ( m.SourceEntity ?? "direct" ) + " )",
                            QueryValues = new Dictionary<string, object>()
                                {
                                    { "CurrencySymbol", m.CurrencySymbol },
                                    { "Price", m.Price },
                                    { "CostType", m.CostType }
                                }
                            //Something that probably looks like that -^
                        } );
                        break;
                    }
                    default:
                        break;
                }
                return JsonHelper.GetJsonWithWrapper( items, true, "", null );
            }
            catch ( Exception ex )
            {
                return JsonHelper.GetJsonWithWrapper( null, false, ex.Message, null );
            }
        }

        /// <summary>
        /// Get a summary of QA role for this entity
        /// </summary>
        /// <param name="searchType"></param>
        /// <param name="entityId"></param>
        /// <param name="maxRecords"></param>
        /// <returns></returns>
        public JsonResult GetEntityRoles( string searchType, int entityId, int maxRecords = 10 )
        {
            var data = SearchServices.EntityQARolesList( searchType, entityId, maxRecords );

            return JsonHelper.GetJsonWithWrapper( data, true, "", null );
        }
        //

        [HttpPost]
        public ActionResult LoadPartial( string partialName, Dictionary<string, object> queryValues )
        {
            return View( "~/Views/Search/ResultPartials/" + partialName + ".cshtml", queryValues );
        }
        //
    }

}
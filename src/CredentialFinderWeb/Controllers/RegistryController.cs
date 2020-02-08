using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Web.Mvc;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.Helpers.CompetencyFrameworkHelpers;
using workIT.Services;
using workIT.Utilities;

using Newtonsoft.Json;

namespace CredentialFinderWeb.Controllers
{
	public class RegistryController : BaseController
    {
		private bool IsAuthorized()
		{
			if ( AccountServices.IsUserSiteStaff() )
				return true;
			else
				return false;
		}

		// GET: Registry
		public ActionResult Index()
        {
            return View( "Search" );
        }

        public ActionResult Search()
        {
            return View();
        }
		//


		public JsonResult GetResource(string type, string value)
        {
            var credentialRegistryUrl = UtilityManager.GetAppKeyValue("credentialRegistryUrl");

            try
            {
                var url = "";
                var mode = "";
                switch (type.ToLower())
                {
                    case "ctid":
                        {
                            url = credentialRegistryUrl + "resources/" + value;
                            var rawResult = new HttpClient().GetAsync(url).Result;
                            var result = rawResult.Content.ReadAsStringAsync().Result;
                            mode = "ctid";
                            if (rawResult.StatusCode == System.Net.HttpStatusCode.NotFound || result.Contains("No matching resource found")) //Last Resort
                            {
                                url = credentialRegistryUrl + "ce-registry/search?ceterms%3Actid=" + value;
                                result = new HttpClient().GetAsync(url).Result.Content.ReadAsStringAsync().Result;
                                mode = "search";
                            }
                            return JsonResponse(result, true, "", new { url = url, mode = mode });
                        }
                    case "envelopeid":
                        {
                            url = credentialRegistryUrl + "ce-registry/envelopes/" + value;
                            var result = new HttpClient().GetAsync(url).Result.Content.ReadAsStringAsync().Result;
                            mode = "envelopeid";
                            return JsonResponse(result, true, "", new { url = url, mode = mode });
                        }
                    default:
                        {
                            return JsonResponse(null, false, "Unable to determine type", null);
                        }
                }

            }
            catch (Exception ex)
            {
                return JsonResponse(null, false, ex.Message, null);
            }
        }
        //
        public JsonResult ProxyQuery(string query)
        {
            var credentialRegistryUrl = UtilityManager.GetAppKeyValue("credentialRegistryUrl");
            var queryBasis = credentialRegistryUrl + "ce-registry/search?"; //Should get this from web.config

            var data = new HttpClient().GetAsync(queryBasis + query).Result;
            var headers = new Dictionary<string, object>();
            foreach (var header in data.Headers)
            {
                var value = header.Value.Count() == 1 ? (object)header.Value.FirstOrDefault() : (object)header.Value;
                try { value = int.Parse(header.Value.FirstOrDefault()); } catch { }
                headers.Add(header.Key, value);
            }
            var body = data.Content.ReadAsStringAsync().Result;
            return JsonResponse(body, true, "", new { headers = headers });
        }
		//

		/* Update 2019 - Competency Framework handling */
		public JsonResult GetRegistryDataList( List<string> ctids = null, List<string> uris = null )
		{
			ctids = (ctids ?? new List<string>()).Where( m => !string.IsNullOrWhiteSpace( m ) ).Distinct().ToList();
			uris = (uris ?? new List<string>()).Where( m => !string.IsNullOrWhiteSpace( m ) ).Distinct().ToList();
			var results = new Dictionary<string, string>();
			var urlPrefix = ConfigHelper.GetConfigValue( "credentialRegistryUrl", "https://credentialengineregistry.org/" ) + "graph/";
			WaitCallback getMethod = MakeHttpGet;
			var ctidSet = new AsyncItemSet<RegistryGetItem>() { Items = ctids.ConvertAll( m => new RegistryGetItem() { Identifier = m, Url = urlPrefix + m, IsInProgress = true } ) };
			var uriSet = new AsyncItemSet<RegistryGetItem>() { Items = uris.ConvertAll( m => new RegistryGetItem() { Identifier = m, Url = m, IsInProgress = true } ) };

			foreach( var ctidItem in ctidSet.Items )
			{
				ThreadPool.QueueUserWorkItem( MakeHttpGet, ctidItem );
			}

			foreach( var uriItem in uriSet.Items )
			{
				ThreadPool.QueueUserWorkItem( MakeHttpGet, uriItem );
			}

			ctidSet.WaitUntilAllAreFinished();
			uriSet.WaitUntilAllAreFinished();

			foreach( var ctidItem in ctidSet.Items )
			{
				Append(results, ctidItem.Identifier, ctidItem.Result );
			}

			foreach(var uriItem in uriSet.Items )
			{
				Append(results, uriItem.Identifier, uriItem.Result );
			}

			return JsonResponse( results, true, "", null );
		}
		private static void MakeHttpGet( object registryGetItem )
		{
			var item = ( RegistryGetItem ) registryGetItem;
			item.Result = new HttpClient().GetAsync( item.Url ).Result.Content.ReadAsStringAsync().Result;
			item.IsInProgress = false;
		}
		private void Append<T>( Dictionary<string, T> container, string key, T value )
		{
			if ( container.ContainsKey( key ) )
			{
				container[ key ] = value;
			}
			else
			{
				container.Add( key, value );
			}
		}
		//

		public JsonResult GetCredentialsForFrameworks( List<FrameworkSearchItem> ctidMap )
		{
			//Deduplicate map and set processing method
			var results = DoThreadedFrameworkSearch( ctidMap, CompetencyFrameworkServices.GetCredentialsForCompetencies );
			return JsonResponse( results, true, "", null );
		}
		//

		public JsonResult GetAssessmentsForFrameworks( List<FrameworkSearchItem> ctidMap )
		{
			//Deduplicate map and set processing method
			var results = DoThreadedFrameworkSearch( ctidMap, CompetencyFrameworkServices.GetAssessmentsForCompetencies );
			return JsonResponse( results, true, "", null );
		}
		//

		public JsonResult GetLearningOpportunitiesForFrameworks( List<FrameworkSearchItem> ctidMap )
		{
			//Deduplicate map and set processing method
			var results = DoThreadedFrameworkSearch( ctidMap, CompetencyFrameworkServices.GetLearningOpportunitiesForCompetencies );
			return JsonResponse( results, true, "", null );
		}
		//

		private List<FrameworkResultSet> DoThreadedFrameworkSearch( List<FrameworkSearchItem> items, FrameworkSearchMethod searchMethod, int skipResults = 0, int takeResults = 50 )
		{
			//Denullify
			items = items ?? new List<FrameworkSearchItem>();
			//Hold filtered items
			var preparedItems = new List<FrameworkSearchItem>();
			//Process items
			foreach( var item in items )
			{
				//Filter out duplicate frameworks by CTID and assign the search method
				if( preparedItems.FirstOrDefault( m => m.FrameworkCTID == item.FrameworkCTID ) == null )
				{
					item.ProcessMethod = searchMethod;
					item.SkipResults = skipResults;
					item.TakeResults = takeResults;
					item.ClientIP = Request.UserHostAddress;
					preparedItems.Add( item );
				}
			}
			//Do the search
			var searchResults = CompetencyFrameworkServices.ThreadedFrameworkSearch( preparedItems );
			//Convert the results and return them
			return searchResults.ConvertAll( m => new FrameworkResultSet( m ) );
		}
		//

		public class FrameworkResultSet
		{
			public FrameworkResultSet( FrameworkSearchItem data )
			{
				FrameworkCTID = data.FrameworkCTID;
				Results = data.Results.ConvertAll( m => m.ToString( Newtonsoft.Json.Formatting.None ) );
				TotalResults = data.TotalResults;
			}
			public string FrameworkCTID { get; set; }
			public List<string> Results { get; set; }
			public int TotalResults { get; set; }
		}
		//

		[Route("registry/ctdltoeocred/script/{ctid}")]
		public string CTDLtoEOCredScript ( string ctid, string onload = "", string mergeType = "MergeAndReplace" )
		{
			if ( string.IsNullOrWhiteSpace( ctid ) )
			{
				return "console.log('Error: You must include a valid CTID to use this script.');";
			}

			try
			{
				var content = new HttpClient().GetAsync( "https://credreg.net/registry/convertctdltoschema?ctid=" + ctid + "&mergetype=" + mergeType ).Result.Content.ReadAsStringAsync().Result;
				var lines = "(function(){\n" + //Use a closure to avoid any chance of conflict with window-level variables
					"var tag = document.createElement('script');\n" +
					"var content = " + content + ";\n" +
					"tag.type = 'application/ld+json';\n" +
					"tag.innerHTML = JSON.stringify(content);\n" +
					"document.head.appendChild(tag);\n";
					

				if ( !string.IsNullOrWhiteSpace( onload ) )
				{
					lines += onload + "('" + ctid + "', content, tag);\n";
				}

				//Finish the closure
				lines += "})()";

				Response.ContentType = "text/javascript";
				return lines;
			}
			catch ( Exception ex )
			{
				return "console.log('Error: There was an error retrieving your data: " + ex.Message + "');";
			}

		}
		//

		[Route("registry/ctdltoeocred/json/{ctid}")]
		public string CTDLtoEOCredJSON( string ctid, string mergeType = "MergeAndReplace" )
		{
			Response.ContentType = "application/json";
			try
			{
				var content = new HttpClient().GetAsync( "https://credreg.net/registry/convertctdltoschema?ctid=" + ctid + "&mergetype=" + mergeType ).Result.Content.ReadAsStringAsync().Result;
				return content;
			}
			catch ( Exception ex )
			{
				return JsonConvert.SerializeObject( new { Error = "There was an error processing your request" } );
			}
		}
		//

	}
}
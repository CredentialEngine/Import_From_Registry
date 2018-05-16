using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Mvc;
using workIT.Utilities;

namespace WorkIT.Web.Controllers
{
    public class RegistryController : BaseController
    {
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
    }
}
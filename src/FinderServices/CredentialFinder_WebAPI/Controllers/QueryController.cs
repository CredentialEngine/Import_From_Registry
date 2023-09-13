using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
//using System.Web.Mvc;
using System.Web.Http;

using workIT.Utilities;
using workIT.Models.API;
using API = workIT.Services.API;
using workIT.Models.Services.Reports;
using MPM = workIT.Models.ProfileModels;
using Mgr = workIT.Factories.QueryManager;

namespace CredentialFinderWebAPI.Controllers
{
    public class QueryController : BaseController
    {
        // GET: Query
        [HttpGet, Route( "query/DuplicateCredentialsNameDescSWP" )]
        public SearchResponse DuplicateCredentialsNameDescSWP( Query query )
        {
            SearchResponse response = new SearchResponse();
           
            response.Result = Mgr.DuplicateCredentialsNameDescSWP( query );
            response.TotalRecords = query.TotalRows;
            response.Successful = true;
            return response;
        }
    }
    public class SearchResponse
    {
        public SearchResponse()
        {
            Messages = new List<string>();
        }
        public List<QuerySummary> Result { get; set; }
        public int TotalRecords { get; set; }
        public bool Successful { get; set; }

        public List<string> Messages { get; set; }

    }
}
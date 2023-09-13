using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Http.Description;
using System.Web.Mvc;

using workIT.Models;
using workIT.Services;
using workIT.Utilities;

using PB = workIT.Models.PathwayBuilder;

namespace CredentialFinderWebAPI.Controllers
{
    [CORSActionFilter]
    public class PathwayDisplayController : BaseController
    {
        static string thisClassName = "PathwayDisplayController";

        public ActionResult Index()
        {
            var results = "There is no data at this endpoint";
            var response = new PB.ApiResponse()
            {
                Data = results
            };


            return JsonApiResponse( response );
        }

        // GET: Pathway
        /// <summary>
        /// get pathway and all components
        /// </summary>
        /// <param name="id"></param>
        [AcceptVerbs( "OPTIONS", "GET" ), Route( "PathwayGraph/{id}" )]
        public ActionResult PathwayWrapperGet( string id )
        {
            int recordId = 0;
            var label = "Pathway";
            var resource = new PB.PathwayWrapper();
      
            AppUser user = AccountServices.GetCurrentUser();
            List<string> messages = new List<string>();
      
            try
            {
                
                if ( int.TryParse( id, out recordId ) )
                {
                    resource = PathwayServices.PathwayGraphGet( recordId );
                }
                else if ( ServiceHelper.IsValidCtid( id, ref messages ) )
                {
                    resource = PathwayServices.PathwayGraphGetByCtid( id );
                }
                else
                {
                    messages.Add( "ERROR - Invalid request. Either provide a CTID which starts with 'ce-' or the number. " );
                    return JsonApiResponse( resource, !messages.Any(), messages );
                }
                //HttpContext.Server.ScriptTimeout = 300;

                if ( resource == null || resource.Pathway == null || resource.Pathway.Id == 0 )
                {
                    messages.Add( string.Format( "ERROR - Invalid request - the {0} was not found. ", label ) );
                }
                else
                {
                    //map to the wrapper
                    //this should be done a services method
                    recordId = resource.Pathway.Id;
                }
              

                return JsonApiResponse( resource, !messages.Any(), messages, null, recordId );
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, thisClassName + ".Pathway.Id: " + id );
                messages.Add( string.Format( "Error encountered returning data. {0} ", ex.Message ) );

                return JsonApiResponse( null, false, messages );
            }
        }

        #region Schema information

        [HttpGet, Route( "PathwayGraph/Schema/PathwayComponent" )]
        public ActionResult SchemaPathwayComponent()
        {
            return ReturnDataOrError( () => { return PathwayServices.GetPathwayComponentConcepts(); }, thisClassName + ".SchemaPathwayComponent" );
        }
        //
        [HttpGet, Route( "PathwayGraph/Schema/CredentialType" )]
        public ActionResult CredentialType()
        {
            return ReturnDataOrError( () => { return PathwayServices.GetCredentialTypeConcepts(); }, thisClassName + ".SchemaCredentialType" );
        }
        //

        [HttpGet, Route( "PathwayDisplay/Schema/LogicalOperator" )]
        public ActionResult SchemaLogicalOperator()
        {
            return ReturnDataOrError( () => { return PathwayServices.GetLogicalOperatorConcepts(); }, thisClassName + ".SchemaLogicalOperator" );
        }
        //

        [HttpGet, Route( "PathwayGraph/Schema/Comparator" )]
        public ActionResult Comparator()
        {
            return ReturnDataOrError( () => { return PathwayServices.GetComparatorConcepts(); }, thisClassName + ".SchemaComparator" );
        }
        //

        [HttpGet, Route( "PathwayDisplay/Schema/ArrayOperation" )]
        public ActionResult ArrayOperation()
        {
            return ReturnDataOrError( () => { return PathwayServices.GetArrayOperationConcepts(); }, thisClassName + ".SchemaArrayOperation" );
        }
        //
        [HttpGet, Route( "PathwayGraph/Schema/CreditUnitType" )]
        public ActionResult CreditUnits()
        {
            return ReturnDataOrError( () => { return PathwayServices.GetCreditUnitTypeConcepts(); }, thisClassName + ".SchemaCreditUnit" );
        }
        //
        [HttpGet, Route( "PathwayDisplay/Schema/CreditLevelType" )]
        public ActionResult CreditLevelTypes()
        {
            return ReturnDataOrError( () => { return PathwayServices.GetCreditLevelTypeConcepts(); }, thisClassName + ".SchemaCreditUnit" );
        }
        //

        #endregion

        //Generic way to call a method and return whatever it returns, or return an error
        private ActionResult ReturnDataOrError( Func<object> GetDataMethod, string errorLoggingHelper )
        {
            try
            {
                var output = GetDataMethod();

                return JsonApiResponse( output, true );
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, errorLoggingHelper );

                return JsonApiResponse( null, false, new List<string>() { string.Format( "Error encountered returning data. {0} ", ex.Message ) } );
            }
        }
        //
        //Default method to send a JSON response for PB API
        public ActionResult JsonApiResponse( object data, bool valid, List<string> messages = null, object extra = null, int pathwayId = 0 )
        {
            var response = new PB.ApiResponse( data, valid, messages, extra );
            if ( pathwayId > 0 )
                response.PathwayId = pathwayId;
            return JsonApiResponse( response );
        }
        public ActionResult JsonApiResponse( PB.ApiResponse response )
        {
            //??
            //Response.AppendHeader( "Access-Control-Allow-Origin", "*" );
            return new ContentResult()
            {
                Content = JsonConvert.SerializeObject(
                    response,
                    Formatting.None,
                    new JsonSerializerSettings()
                    {
                        DefaultValueHandling = DefaultValueHandling.Ignore,
                        NullValueHandling = NullValueHandling.Ignore
                    }
                ),
                ContentEncoding = Encoding.UTF8,
                ContentType = "application/json"
            };
        }
        public class CORSActionFilterAttribute : ActionFilterAttribute
        {
            public override void OnActionExecuting( ActionExecutingContext filterContext )
            {
                if ( filterContext.HttpContext.Request.HttpMethod == "OPTIONS" )
                {
                    filterContext.HttpContext.Response.Clear();
                    filterContext.HttpContext.Response.Headers.Add( "Access-Control-Allow-Origin", "*" );
                    filterContext.HttpContext.Response.Headers.Add( "Access-Control-Allow-Headers", "*" );
                    filterContext.HttpContext.Response.Headers.Add( "Access-Control-Allow-Methods", "GET, POST, OPTIONS" );
                    filterContext.HttpContext.Response.StatusCode = 204;
                    filterContext.Result = new EmptyResult(); //Causes further execution to be cancelled, so that methods aren't executed twice (once for options, once for get/post)
                }

                base.OnActionExecuting( filterContext );
            }
        }
        //

    }
}
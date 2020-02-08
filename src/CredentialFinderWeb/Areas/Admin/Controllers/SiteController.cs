using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

using CM = workIT.Models.Common;
using workIT.Factories;
using workIT.Services;
using workIT.Utilities;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Web.Script.Serialization;

namespace CredentialFinderWeb.Areas.Admin.Controllers
{
    public class SiteController : CredentialFinderWeb.Controllers.BaseController
	{
        // GET: Admin/Site
        public ActionResult Index()
        {
            return View();
        }

		public ActionResult LinkChecker()
		{
			return View();
		}
		/// <summary>
		/// Publishing LinkChecker Search
		/// </summary>
		/// <returns></returns>
		public ActionResult LinkCheckerSearch()
		{
			var result = new JsonResult();

			try
			{
				#region According to Datatables.net, Server side parameters

				//global search
				var search = Request.Form[ "search[value]" ];
				//what is draw??
				var draw = Request.Form[ "draw" ];

				var orderBy = string.Empty;
				//column index
				var order = int.Parse( Request.Form[ "order[0][column]" ] );
				//sort direction
				var orderDir = Request.Form[ "order[0][dir]" ];

				int startRec = int.Parse( Request.Form[ "start" ] );
				int pageSize = int.Parse( Request.Form[ "length" ] );
				int pageNbr = ( startRec / pageSize ) + 1;
				#endregion

				#region Where filter

				//individual column wise search
				var columnSearch = new List<string>();
				var globalSearch = new List<string>();
				DateTime dt = new DateTime();

				//Get all keys starting with columns    
				foreach ( var index in Request.Form.AllKeys.Where( x => Regex.Match( x, @"columns\[(\d+)]" ).Success ).Select( x => int.Parse( Regex.Match( x, @"\d+" ).Value ) ).Distinct().ToList() )
				{
					//get individual columns search value
					var value = Request.Form[ string.Format( "columns[{0}][search][value]", index ) ];
					if ( !string.IsNullOrWhiteSpace( value.Trim() ) )
					{
						value = value.Trim();
						string colName = Request.Form[ string.Format( "columns[{0}][data]", index ) ];
						if ( colName == "DisplayDate" || colName == "CheckDate" )
						{
							if ( DateTime.TryParse( value, out dt ) )
							{
								columnSearch.Add( string.Format( " (convert(varchar(10),CheckDate,120) = '{0}') ", dt.ToString( "yyyy-MM-dd" ) ) );
							}
						}
						else
						{
							if ( value.Length > 1 && value.IndexOf( "!" ) == 0 )
								columnSearch.Add( string.Format( "({0} NOT LIKE '%{1}%')", Request.Form[ string.Format( "columns[{0}][data]", index ) ], value.Substring( 1 ) ) );
							else
							{
								//check for OR, or ||
								//should watch for incomplete typing
								if ( value.IndexOf( " OR " ) > 0 )
								{
									var itemList = value.Split( new string[] { " OR " }, StringSplitOptions.None );
									string filter = "";
									string OR = "";
									foreach ( var item in itemList )
									{
										filter = OR + string.Format( "({0} LIKE '%{1}%')", Request.Form[ string.Format( "columns[{0}][data]", index ) ], item );
										OR = " OR ";
									}
									columnSearch.Add( filter );
								}
								else if ( value.IndexOf( "||" ) > 0 )
								{
									var itemList = value.Split( new string[] { "||" }, StringSplitOptions.None );
									string filter = "";
									string OR = "";
									foreach ( var item in itemList )
									{
										filter = OR + string.Format( "({0} LIKE '%{1}%')", Request.Form[ string.Format( "columns[{0}][data]", index ) ], item );
										OR = " OR ";
									}
									columnSearch.Add( filter );
								}
								else
								{
									columnSearch.Add( string.Format( "({0} LIKE '%{1}%')", Request.Form[ string.Format( "columns[{0}][data]", index ) ], value ) );
								}
							}
						}
					}
					//get column filter for global search
					if ( !string.IsNullOrWhiteSpace( search ) )
					{
						if ( index > 0 ) //skip date
						{
							if ( Request.Form[ string.Format( "columns[{0}][data]", index ) ] != "Totals" )
								globalSearch.Add( string.Format( "({0} LIKE '%{1}%')", Request.Form[ string.Format( "columns[{0}][data]", index ) ], search ) );
						}
					}

					//get order by from order index
					if ( order == index )
						orderBy = Request.Form[ string.Format( "columns[{0}][data]", index ) ];
				}

				var where = string.Empty;
				//concat all filters for global search
				if ( globalSearch.Any() )
					where = globalSearch.Aggregate( ( current, next ) => current + " OR " + next );

				if ( columnSearch.Any() )
					if ( !string.IsNullOrEmpty( where ) )
						where = string.Format( "({0}) AND ({1})", where, columnSearch.Aggregate( ( current, next ) => current + " AND " + next ) );
					else
						where = columnSearch.Aggregate( ( current, next ) => current + " AND " + next );

				#endregion
				if ( orderBy == "DisplayDate" )
					orderBy = "CheckDate";

				SearchRequest parms = new SearchRequest()
				{
					OrderBy = orderBy,
					OrderDescending = orderDir == "desc" ? true : false,
					PageNumber = pageNbr,
					PageSize = pageSize
				};
				parms.Filter = where;
				var totalRecords = 0;
				//var list = ActivityServices.PublishHistorySearch( parms, ref totalRecords );
				var list = new List<LinkCheckActivity>();
				parms.Token = UtilityManager.GetAppKeyValue( "apiPublisherIdentifier", "" );
				var getUrl = UtilityManager.GetAppKeyValue( "ceServicesLinkCheckerSearch" );

				SearchResponse response = new SearchResponse();
				try
				{
					var content = new StringContent( Newtonsoft.Json.JsonConvert.SerializeObject( parms ), System.Text.Encoding.UTF8, "application/json" );

					var rawData = new HttpClient().PostAsync( getUrl, content ).Result.Content.ReadAsStringAsync().Result;

					if ( rawData == null || rawData.IndexOf( "The resource cannot be found" ) > 0
					)
					{
						//messages.Add( "Error: the concepts schemes were not found using fallback?????? " );
						LoggingHelper.DoTrace( 2, string.Format( "LinkCheckerSearch. ??????" ) );

						
						//return false;
					}
					else
					{
						response = new JavaScriptSerializer().Deserialize<SearchResponse>( rawData );
					}


					if ( response != null && response.History != null && response.History.Count > 0 )
					{
						list = response.History;
						
					}
					else
					{
						
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, ".LinkCheckerSearch" );
					string message = LoggingHelper.FormatExceptions( ex );

					list.Add( new LinkCheckActivity() { OrganizationName = "Error encountered.", OrganizationCTID = message } );
				}

				result = Json( new { data = list, draw = int.Parse( draw ), recordsTotal = response.TotalRecords, recordsFiltered = response }, JsonRequestBehavior.AllowGet );
			}
			catch ( Exception ex )
			{
			}

			return result;
		}
		// fix addresses missing lat/lng, and normalize
		//[Authorize( Roles = "Administrator, Site Staff" )]
		public ActionResult NormalizeAddresses( int maxRecords = 100 )
        {
            var user = AccountServices.GetCurrentUser();
            if ( user != null && 
                ( 
                    user.Email == "email@yourDomain.com" ||
                    user.Email == "cwd-nathan.argo@yourDomain.com"
                  )
                )
            {
                //next
            } else 
            //if ( !User.Identity.IsAuthenticated
            //    || ( User.Identity.Name != "mparsons"
            //    && User.Identity.Name != "email@yourDomain.com"
            //    && User.Identity.Name != "cwd-nathan.argo@yourDomain.com" )
            //    )
            {
                SetSystemMessage( "Unauthorized Action", "You are not authorized to invoke NormalizeAddresses." );
                return RedirectToAction( "Index", "Message" );
            }
            string report = "";
            string messages = "";
            List<CM.Address> list = new Entity_AddressManager().ResolveMissingGeodata( ref messages, maxRecords: maxRecords );

            if ( !string.IsNullOrWhiteSpace( messages ) )
                report = "<p>Normalize Addresses: <br/>" + messages + "</p>"  ;

            foreach ( var address in list )
            {
                string msg = string.Format( " - Unable to resolve address: Id: {0}, address1: {1}, city: {2}, region: {3}, postalCode: {4}, country: {5} ", address.Id, address.Address1, address.City, address.AddressRegion, address.PostalCode, address.Country );
                LoggingHelper.DoTrace(2, msg );
                report += System.Environment.NewLine + msg;
            }

            SetSystemMessage( "Normalize Addresses", report );

            return RedirectToAction( "Index", "Message", new { area = "" } );
        }

        public ActionResult HandlePendingDeletes()
        {
            List<String> messages = new List<string>();
            ElasticServices.HandlePendingDeletes(ref messages);
            string report = string.Join( "<br/>", messages.ToArray() ); 
            SetSystemMessage( "Handling Pending Deletes From Elastic", report );

            return RedirectToAction( "Index", "Message", new { area = "" } );
        }
        public ActionResult HandlePendingUpdates()
        {
            List<String> messages = new List<string>();
            ElasticServices.HandlePendingReindexRequests( ref messages );
            string report = string.Join( "<br/>", messages.ToArray() );
            SetSystemMessage( "Handling Pending Updates From Elastic", report );

            return RedirectToAction( "Index", "Message", new { area = "" } );
        }

		public ActionResult UpdateCompetencyFrameworkTotals( int maxRecords = 100 )
		{
			new CompetencyFrameworkServices().UpdateCompetencyFrameworkReportTotals();

			SetSystemMessage( "Completed task", "Check the reports page.");

			return RedirectToAction( "Index", "Message", new { area = "" } );
		}
	}

	public class SearchRequest
	{
		public SearchRequest()
		{
		}

		public string Filter { get; set; }
		public string OrderBy { get; set; }
		public bool OrderDescending { get; set; }
		public int PageNumber { get; set; }
		public int PageSize { get; set; }
		public string Token { get; set; }
	}
	public class SearchResponse
	{
		public SearchResponse()
		{
			Messages = new List<string>();
		}
		public List<LinkCheckActivity> History { get; set; }
		public int TotalRecords { get; set; }
		public bool Successful { get; set; }

		public List<string> Messages { get; set; }

	}
	public class LinkCheckActivity
	{
		public int Id { get; set; }
		public System.DateTime CheckDate { get; set; }
		public string DisplayDate
		{
			get
			{
				if ( CheckDate != null )
				{
					return this.CheckDate.ToString( "yyyy-MM-dd HH.mm.ss" );
				}
				else
					return "";
			}
		}
		public string OrganizationCTID { get; set; }
		public string OrganizationName { get; set; }
		public int EntityTypeId { get; set; }
		public string EntityType { get; set; }
		public string EntityCTID { get; set; }
		public string EntityName { get; set; }
		public string FinderUrl { get; set; }
		public string Property { get; set; }
		public string URL { get; set; }
		public string Status { get; set; }
		public string StatusCode { get; set; }
	}
}
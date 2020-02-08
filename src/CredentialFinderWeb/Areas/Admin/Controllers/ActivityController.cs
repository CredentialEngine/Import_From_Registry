using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

using Newtonsoft.Json;

using CredentialFinderWeb;
using workIT.Models;
using workIT.Services;
using workIT.Utilities;
using workIT.Factories;
using workIT.Models.Search;

namespace CredentialFinderWeb.Areas.Admin.Controllers
{
    public class ActivityController : Controller
    {
        // GET: Admin/Activity
        public ActionResult Index()
        {
            return View();
            //return Search();
        }
        /// <summary>
        /// Current Activity Search
        /// </summary>
        /// <returns></returns>
        public ActionResult DoSearch()
        {
            var result = new JsonResult();

            try
            {
                #region According to Datatables.net, Server side parameters

                //global search
                var search = Request.Form["search[value]"];
                //what is draw??
                var draw = Request.Form["draw"];

                var orderBy = string.Empty;
                //column index
                var order = int.Parse( Request.Form["order[0][column]"] );
                //sort direction
                var orderDir = Request.Form["order[0][dir]"];

                int startRec = int.Parse( Request.Form["start"] );
                int pageSize = int.Parse( Request.Form["length"] );
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
                    var value = Request.Form[string.Format( "columns[{0}][search][value]", index )];
                    if ( !string.IsNullOrWhiteSpace( value ) )
                    {
                        value = value.Trim();
                        string colName = Request.Form[string.Format( "columns[{0}][data]", index )];
                        if ( colName == "DisplayDate" || colName == "Created" )
                        {
                            if ( DateTime.TryParse( value, out dt ) )
                            {
                                columnSearch.Add( string.Format( " (convert(varchar(10),CreatedDate,120) = '{0}') ", dt.ToString( "yyyy-MM-dd" ) ) );
                            }
                        }
                        else if ( colName == "ParentRecordId" || colName == "ParentEntityTypeId" || colName == "ActivityObjectId" || colName == "ActionByUserId" )
                        {
                            columnSearch.Add( string.Format( " ( {0} = {1} ) ", colName, value ) );
                        }
                        else
                        {
                            if ( value.Length > 1 && value.IndexOf( "!" ) == 0 )
                                columnSearch.Add( string.Format( "({0} NOT LIKE '%{1}%')", Request.Form[string.Format( "columns[{0}][data]", index )], value.Substring( 1 ) ) );
                            else
                                columnSearch.Add( string.Format( "({0} LIKE '%{1}%')", Request.Form[string.Format( "columns[{0}][data]", index )], value ) );
                        }
                    }
					//get column filter for global search
					if ( !string.IsNullOrWhiteSpace( search ) )
					{
						if ( index > 0 ) //skip date
							globalSearch.Add( string.Format( "({0} LIKE '%{1}%')", Request.Form[ string.Format( "columns[{0}][data]", index ) ], search ) );
					}


                    //get order by from order index
                    if ( order == index )
                        orderBy = Request.Form[string.Format( "columns[{0}][data]", index )];
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


                BaseSearchModel parms = new BaseSearchModel()
                {
                    Keyword = "",
                    OrderBy = orderBy,
                    IsDescending = orderDir == "desc" ? true : false,
                    PageNumber = pageNbr,
                    PageSize = pageSize
                };
                parms.Filter = where;
                var totalRecords = 0;
                var list = ActivityServices.Search( parms, ref totalRecords );

                result = Json( new { data = list, draw = int.Parse( draw ), recordsTotal = totalRecords, recordsFiltered = totalRecords }, JsonRequestBehavior.AllowGet );
            }
            catch ( Exception ex )
            {
            }

            return result;
        }
        [HttpGet]
		public ActionResult Today()
		{
			return View();
		}
		public ActionResult ActivityToday( string sidx, string sord, int page, int rows
			, bool _search
			, string Activity
			, string Event, string comment, string ActionByUser, string DisplayDate
			, string filters )
		{

			int pTotalRows = 0;

			List<SiteActivity> list = ActivityServices.SearchToday( "", sidx, sord, page, rows, ref pTotalRows );

			int pageIndex = Convert.ToInt32( page ) - 1;
			int pageSize = rows;
			int totalPages = ( int ) Math.Ceiling( ( float ) pTotalRows / ( float ) pageSize );


			var jsonData = new
			{
				total = totalPages,
				page = page,
				records = pTotalRows,
				rows = list
			};

			return Json( jsonData, JsonRequestBehavior.AllowGet );
		}

		[HttpGet]
		public ActionResult Search()
		{
			return View();
		}
		public ActionResult GetActivity( string sidx, string sord, int page, int rows
				, bool _search
				, string Activity
				, string Event, string comment, string ActionByUser, string DisplayDate, string ActivityObjectId
				, string filters )
		{
			//, string filters = "", string Event:registra
			int pTotalRows = 0;
			string where = "";

			if ( ( filters ?? "" ).Length > 20 )
			{
				GridFilter data = JsonConvert.DeserializeObject<GridFilter>( filters, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore } );

				foreach ( Rule r in data.rules )
				{
					SetRuleFilter( r.field, r.op, r.data, data.groupOp, ref where );
				}
			}

			BaseSearchModel parms = new BaseSearchModel()
			{
				Keyword = "",
				OrderBy = sidx,
				IsDescending = sord == "desc" ? true : false,
				PageNumber = page,
				PageSize = rows
			};

			if ( ( filters ?? "" ) == "" )
			{
				//need an operator
				SetKeywordFilter( "Activity", Activity, ref where );
				SetKeywordFilter( "Event", Event, ref where );
				SetKeywordFilter( "ActionByUser", ActionByUser, ref where );
				SetKeywordFilter( "comment", comment, ref where );
			}

			parms.Filter = where;
			List<SiteActivity> list = ActivityServices.Search( parms, ref pTotalRows );

			int pageIndex = Convert.ToInt32( page ) - 1;
			int pageSize = rows;
			int totalPages = ( int ) Math.Ceiling( ( float ) pTotalRows / ( float ) pageSize );


			var jsonData = new
			{
				total = totalPages,
				page = page,
				records = pTotalRows,
				rows = list
			};

			return Json( jsonData, JsonRequestBehavior.AllowGet );
		}

		public ActionResult Search( string sidx, string sord, int page, int rows
				, bool _search
				, string Activity
				, string Event, string comment, string ActionByUser, string DisplayDate
				, string filters )
		{
			//, string filters = "", string Event:registra
			int pTotalRows = 0;
			string where = "";

			if ( (filters ?? "").Length > 20)
			{
				GridFilter data = JsonConvert.DeserializeObject<GridFilter>( filters, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore } );

				foreach ( Rule r in data.rules)
				{
					SetRuleFilter( r.field, r.op, r.data, data.groupOp, ref where );
				}
			}
			
			BaseSearchModel parms = new BaseSearchModel()
			{
				Keyword = "",
				OrderBy = sidx,
				IsDescending = sord == "desc" ? true : false,
				PageNumber = page,
				PageSize = rows
			};
			
			if (!_search )
			{
				//need an operator
				SetKeywordFilter( "Activity", Activity, ref where );
				SetKeywordFilter( "Event", Event, ref where );
				SetKeywordFilter( "ActionByUser", ActionByUser, ref where );
				SetKeywordFilter( "comment", comment, ref where );
			}
			
			parms.Filter = where;
			List<SiteActivity> list = ActivityServices.Search( parms, ref pTotalRows );

			int pageIndex = Convert.ToInt32( page ) - 1;
			int pageSize = rows;
			int totalPages = ( int ) Math.Ceiling( ( float ) pTotalRows / ( float ) pageSize );


			var jsonData = new
			{
				total = totalPages,
				page = page,
				records = pTotalRows,
				rows = list
			};

			return Json( jsonData, JsonRequestBehavior.AllowGet );
		}
		private static void SetKeywordFilter( string column, string value, ref string where )
		{
			if ( string.IsNullOrWhiteSpace( value ) )
				return;
			string text = " ({0} like '{1}' ) ";

			string AND = "";
			if ( where.Length > 0 )
				AND = " AND ";
			//
			value = ServiceHelper.HandleApostrophes( value );
			if ( value.IndexOf( "%" ) == -1 )
				value = "%" + value.Trim() + "%";

			where = where + AND + string.Format( " ( " + text + " ) ", column, value );

		}
		private static void SetRuleFilter( string column, string filterType, string value, string groupOp, ref string where )
		{
			//may need a type, to properly format equals, etc
			if ( string.IsNullOrWhiteSpace( value ) )
				return;

			string GROUP_OP = "";
			if ( where.Length > 0 )
				GROUP_OP = string.Format( " {0} ", groupOp );

			value = ServiceHelper.HandleApostrophes( value );
			if ( column == "id" || column == "ActivityObjectId" )
			{
				if ( filterType == "eq" )
				{
					where = where + GROUP_OP + string.Format( " ( {0} = {1}  ) ", column, value );
				}
				else if ( filterType == "le" )
				{
					where = where + GROUP_OP + string.Format( " ( {0} <= {1}  ) ", column, value );
				}
				else if ( filterType == "ge" )
				{
					where = where + GROUP_OP + string.Format( " ( {0} >= {1}  ) ", column, value );
				}

			}
			else if ( column == "DisplayDate" )
			{
				if ( value.ToLower() == "today" )
					value = DateTime.Now.ToString( "yyyy-MM-dd" );
				if ( filterType == "bw" )
					where = where + GROUP_OP + string.Format( " ( convert(varchar(10),CreatedDate, 120) = '{0}'  ) ", value );
				else if ( filterType == "eq" )
				{
					where = where + GROUP_OP + string.Format( " ( convert(varchar(10),CreatedDate, 120) = '{0}'  ) ", value );
				}
				else
					where = where + GROUP_OP + string.Format( " ( CreatedDate > '{0}'  ) ", value );
			} else
			{
				//for strings
				if ( filterType == "eq" )
				{
					where = where + GROUP_OP + string.Format( " ( {0} = '{1}'  ) ", column, value );
				}
				else if ( filterType == "bw" ) //begins with
				{
					if ( value.IndexOf( "%" ) == -1 )
						value = value.Trim() + "%";
					where = where + GROUP_OP + string.Format( " ( {0} like '{1}' ) ", column, value );
				}
				else if ( filterType == "bn" )  //does not begin with
				{
					if ( value.IndexOf( "%" ) == -1 )
						value = value.Trim() + "%";
					where = where + GROUP_OP + string.Format( " ( {0} NOT like '{1}' ) ", column, value );
				}
				else if ( filterType == "cn" )  //contains
				{
					if ( value.IndexOf( "%" ) == -1 )
						value = "%" + value.Trim() + "%";
					where = where + GROUP_OP + string.Format( " ( {0} like '{1}' ) ", column, value );
				}
				else if ( filterType == "nc" )  //does not contain
				{
					if ( value.IndexOf( "%" ) == -1 )
						value = "%" + value.Trim() + "%";
					where = where + GROUP_OP + string.Format( " ( {0} NOT like '{1}' ) ", column, value );
				}
				else if ( filterType == "ew" ) //ends with
				{
					if ( value.IndexOf( "%" ) == -1 )
						value = "%" + value.Trim();
					where = where + GROUP_OP + string.Format( " ( {0} like '{1}' ) ", column, value );
				}
				else if ( filterType == "en" )  //does not ends with
				{
					if ( value.IndexOf( "%" ) == -1 )
						value = "%" + value.Trim();
					where = where + GROUP_OP + string.Format( " ( {0} NOT like '{1}' ) ", column, value );
				}
			}
		

		} //

		private static void SetKeywordFilter( string keywords, ref string where )
		{
			if ( string.IsNullOrWhiteSpace( keywords ) )
				return;
			string text = " (FirstName like '{0}' OR LastName like '{0}'  OR Email like '{0}'  ) ";

			string AND = "";
			if ( where.Length > 0 )
				AND = " AND ";
			//
			keywords = ServiceHelper.HandleApostrophes( keywords );
			if ( keywords.IndexOf( "%" ) == -1 )
				keywords = "%" + keywords.Trim() + "%";

			where = where + AND + string.Format( " ( " + text + " ) ", keywords );


		}

		[HttpGet]
		public ActionResult SiteTotals()
		{

			return View();
		}

    }


	public class GridFilter
	{
		public string groupOp { get; set; }
		public List<Rule> rules { get; set; }
	}
	public class Rule
	{
		public string field { get; set; }
		public string op { get; set; }
		public string data { get; set; }
	}
}
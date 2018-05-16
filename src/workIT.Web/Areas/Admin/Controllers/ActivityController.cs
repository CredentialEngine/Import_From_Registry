using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using Newtonsoft.Json;

using WorkIT.Web;
using workIT.Models;
using workIT.Services;
using workIT.Utilities;
using workIT.Factories;
using workIT.Models.Search;

namespace WorkIT.Web.Areas.Admin.Controllers
{
    public class ActivityController : Controller
    {
        // GET: Admin/Activity
        public ActionResult Index()
        {
            return Search();
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
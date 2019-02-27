using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using workIT.Services;
using workIT.Utilities;

namespace workIT.Web.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            bool showingSearchOnHome = Utilities.UtilityManager.GetAppKeyValue( "showingSearchOnHome", true );
            if ( Utilities.UtilityManager.GetAppKeyValue( "showingSearchOnHome", true ) )
            {
                if ( Request.Params[ "oldSearch" ] == "yes" )
                {
                    return View( "~/Views/Search/Index.cshtml" );
                }
                else
                    return View( "~/Views/Search/SearchV2.cshtml" );
            }
            else
                return View( "Index" );
        }
        public ActionResult ShowWidget( string widgetAlias )
        {
            if ( !string.IsNullOrWhiteSpace( widgetAlias ) )
            {
                var widget = WidgetServices.GetByAlias( widgetAlias );
                //if not found, display message somewhere - console message?
                if ( widget == null || widget.Id == 0 )
                {
                    workIT.Models.Common.SiteMessage msg = new workIT.Models.Common.SiteMessage() { Title = "Invalid Widget Request", Message = "ERROR - the requested Widget record was not found ", MessageType = "error" };
                    Session[ "SystemMessage" ] = msg;
                    return RedirectToAction( "Index", "Message" );
                }
                else
                {
                    string message = "";
                    //may already be in session, so remove and readd
                    //don't want this any longer
                    //if ( !WidgetServices.Activate( widget, message ) )
                    //{
                    //    workIT.Models.Common.SiteMessage msg = new workIT.Models.Common.SiteMessage() { Title = "Invalid Widget Request", Message = message, MessageType = "error" };
                    //    Session[ "SystemMessage" ] = msg;
                    //    return RedirectToAction( "Index", "Message" );
                    //}

                    var vm = Newtonsoft.Json.JsonConvert.DeserializeObject<workIT.Models.Common.WidgetV2>( widget.CustomStyles ?? "{}" );
                    return View( "~/views/widget/searchwidget.cshtml", vm );
                    //return View( "~/Views/Search/SearchV2.cshtml" );

                }
            }

            return RedirectToAction( "Index", "Home" );
        }
        public ActionResult About()
        {
            //if ( !AccountServices.CanUserViewSite() )
            {
                //ConsoleMessageHelper.SetConsoleErrorMessage( "This site is not currently open to the public. You must be logged in and authorized in order to use this site.", "", true );
            }
            return View( "About" );
            //return Index();
        }
		//
	}
}
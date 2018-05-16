using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using workIT.Utilities;

namespace WorkIT.Web.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            //string envType = Utilities.UtilityManager.GetAppKeyValue("envType", "dev");

            return View( "Index" );
            //return V2();
        }

        public ActionResult About()
        {
            string pageMessage = "";

            if ( Session["siteMessage"] != null )
            {
                pageMessage = Session["siteMessage"].ToString();
                //setting console message doesn't work when switching to a different controller
                ConsoleMessageHelper.SetConsoleErrorMessage( pageMessage, "", true );
                Session.Remove( "siteMessage" );
            }
			else if ( Session[ "SystemMessage" ] != null )
			{
				//should we assume message will include a title
				workIT.Models.Common.SiteMessage msg = ( workIT.Models.Common.SiteMessage ) Session[ "SystemMessage" ];
				//pageHeading = msg.Title;
				//pageMessage = msg.Message;
				ConsoleMessageHelper.SetConsoleErrorMessage( msg.Message, "", true );
				Session.Remove( "SystemMessage" );
			}
			else
            {
                //if ( !AccountServices.CanUserViewSite() )
                {
                    ConsoleMessageHelper.SetConsoleErrorMessage( "This site is not currently open to the public. You must be logged in and authorized in order to use this site.", "", true );
                }
            }
            return Index();
        }

    }
}
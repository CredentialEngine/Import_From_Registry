using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace workIT.Web.Controllers
{
    public class WidgetController : Controller
    {
		//Load page to configure a widget
		public ActionResult Configure()
		{
			return View( "~/Views/Widget/Configure.cshtml" );
		}
		//

		//Take widget parameters via GET parameters
		//TODO - remove widget from session when user visits the homepage
		public ActionResult Apply()
		{
			//Apply widget to session


			//Redirect to start page
			return Redirect( "~/search" );
		}
		//
    }
}
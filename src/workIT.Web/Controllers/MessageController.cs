using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using workIT.Utilities;

namespace workIT.Web.Controllers
{
    public class MessageController : Controller
    {
        // GET: Message
        public ActionResult Index()
        {
			string pageMessage = "";

			if ( Session[ "siteMessage" ] != null )
			{
				pageMessage = Session[ "siteMessage" ].ToString();
				//setting console message doesn't work when switching to a different controller
				//so will handle in home
			}
			return View();
        }

		public ActionResult NotAuthorized()
		{
			return View();
		}
    }
}
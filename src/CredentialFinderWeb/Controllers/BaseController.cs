using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using workIT.Models.Common;
using workIT.Utilities;

namespace CredentialFinderWeb.Controllers
{
	public class BaseController : Controller
	{
       protected InMemoryCache cacheProvider = new InMemoryCache();

        public bool DoesViewExist(string path)
        {
            return System.IO.File.Exists(Server.MapPath(path));
        }

        public ActionResult ViewPage(string path, string redirectFallback)
        {
            if (DoesViewExist(path))
            {
                return View(path);
            }
            else
            {
                return RedirectToAction(redirectFallback);
            }
        }

        // GET: Base
        //public ActionResult Index()
        //{
        //	return View();
        //}

        protected void SetSystemMessage( string title, string message, string messageType = "success" )
		{
			SiteMessage msg = new SiteMessage() { Title = title, Message = message, MessageType = messageType };
			Session[ "SystemMessage" ] = msg;
		}

		protected void SetPopupMessage( string message, string messageType = "info" )
		{
			SiteMessage msg = new SiteMessage() { Message = message, MessageType = messageType };
			Session[ "popupMessage" ] = msg;
		}
		protected void SetPopupSuccessMessage( string message )
		{
			SiteMessage msg = new SiteMessage() { Message = message, MessageType = "success" };
			Session[ "popupMessage" ] = msg;
		}
		protected void SetPopupErrorMessage( string message )
		{
			SiteMessage msg = new SiteMessage() { Message = message, MessageType = "error" };
			Session[ "popupMessage" ] = msg;
		}

        //public JsonResult JsonResponse(object data, bool valid, string status, object extra)
        //{
        //    return new JsonResult() { Data = new { data = data, valid = valid, status = status, extra = extra } };
        //}
        public JsonResult JsonResponse(object data, bool valid, string status, object extra)
        {
            return new JsonResult() { Data = new { data = data, valid = valid, status = status, extra = extra }, JsonRequestBehavior = JsonRequestBehavior.AllowGet, MaxJsonLength = int.MaxValue };
        }

   

    }
}
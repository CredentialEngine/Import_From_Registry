using System.Web.Mvc;

namespace CredentialFinderWeb.Controllers
{
    public class StyleController : Controller
  {
		public ActionResult CommonV2()
		{
			Response.ContentType = "text/css";
			return View( "~/Views/Style/commonV2.cshtml" );
		}
		//

		public ActionResult AccountBox()
		{
			Response.ContentType = "text/css";
			return View( "~/Views/Style/account.cshtml" );
		}
		//

	}
}
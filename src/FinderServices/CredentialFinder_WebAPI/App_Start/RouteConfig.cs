using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace CredentialFinderWebAPI
{
	public class RouteConfig
	{
		public static void RegisterRoutes( RouteCollection routes )
		{
			routes.IgnoreRoute( "{resource}.axd/{*pathInfo}" );
			//enable routing in a controller
			routes.MapMvcAttributeRoutes();
			//
			routes.MapRoute(
			  name: "DetailProcessProfile",
			  url: "detail/processprofile/{guid}/{processTypeId}",
			  defaults: new { controller = "Detail", action = "processprofile", guid = UrlParameter.Optional, processTypeId = UrlParameter.Optional }
			);
            routes.MapRoute(
				name: "ProgressionModelDetails",
				url: "progressionmodel/{id}/{name}",
				defaults: new { controller = "Detail", action = "ConceptScheme", name = UrlParameter.Optional }
			);
            //
            routes.MapRoute(
				name: "Default",
				url: "{controller}/{action}/{id}",
				defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
			);
		}
	}
}

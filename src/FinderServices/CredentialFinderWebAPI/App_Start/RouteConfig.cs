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
				name: "Default",
				url: "{controller}/{action}/{id}",
				defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
			);
		}
	}
}

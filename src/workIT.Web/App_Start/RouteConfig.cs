using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace WorkIT.Web
{
    public class RouteConfig
    {
        public static void RegisterRoutes( RouteCollection routes )
        {
            routes.IgnoreRoute( "{resource}.axd/{*pathInfo}" );

			routes.MapRoute(
				name: "ImportByEnvelopeId",
				url: "import/ByEnvelopeId/{envelopeId}",
				defaults: new { controller = "import", action = "ByEnvelopeId", envelopeId = UrlParameter.Optional }
			);
			routes.MapRoute(
				name: "ImportByCtid",
				url: "import/ByCtid/{ctid}",
				defaults: new { controller = "import", action = "ByCtid", ctid = UrlParameter.Optional }
			);

			routes.MapRoute(
				name: "Credentials",
				url: "credential/{id}/{name}",
				defaults: new { controller = "Detail", action = "Credential", name = UrlParameter.Optional }
			);

			routes.MapRoute(
				name: "Organizations",
				url: "organization/{id}/{name}",
				defaults: new { controller = "Detail", action = "Organization", name = UrlParameter.Optional }
			);

			routes.MapRoute(
				name: "Assessments",
				url: "assessment/{id}/{name}",
				defaults: new { controller = "Detail", action = "Assessment", name = UrlParameter.Optional }
			);

			routes.MapRoute(
				name: "LearningOpps",
				url: "learningOpportunity/{id}/{name}",
				defaults: new { controller = "Detail", action = "LearningOpportunity", name = UrlParameter.Optional }
			);

			routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}

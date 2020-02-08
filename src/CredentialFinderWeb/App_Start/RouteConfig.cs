using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace CredentialFinderWeb
{
    public class RouteConfig
    {
        public static void RegisterRoutes( RouteCollection routes )
        {
            routes.IgnoreRoute( "{resource}.axd/{*pathInfo}" );

			//Enable attribute routing in controllers
			routes.MapMvcAttributeRoutes();

			//??? adding this resulted in a duplicate route
			//AreaRegistration.RegisterAllAreas();

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
				name: "FormImport",
				url: "import/doimport/{model}",
				defaults: new { controller = "import", action = "doimport", model = UrlParameter.Optional }
			);
			routes.MapRoute(
                name: "friendlyWidget",
                url: "w/{widgetAlias}",
                defaults: new { controller = "Home", action = "ShowWidget", widgetAlias = UrlParameter.Optional }
            );
            routes.MapRoute(
                name: "ActivateWidget",
                url: "widget/activate/{widgetId}",
                defaults: new { controller = "widget", action = "Activate", widgetId = UrlParameter.Optional }
            );
            routes.MapRoute(
                name: "RemoveWidget",
                url: "widget/remove/",
                defaults: new { controller = "widget", action = "Remove" }
            );
            routes.MapRoute(
				name: "CredentialDetail",
				url: "credential/{id}/{name}",
				defaults: new { controller = "Detail", action = "Credential", name = UrlParameter.Optional }
			);
            
            routes.MapRoute(
				name: "OrganizationDetail",
				url: "organization/{id}/{name}",
				defaults: new { controller = "Detail", action = "Organization", name = UrlParameter.Optional }
			);
            

            routes.MapRoute(
				name: "AssessmentDetail",
				url: "assessment/{id}/{name}",
				defaults: new { controller = "Detail", action = "Assessment", name = UrlParameter.Optional }
			);
            

            routes.MapRoute(
				name: "LearningOppDetail",
				url: "learningOpportunity/{id}/{name}",
				defaults: new { controller = "Detail", action = "LearningOpportunity", name = UrlParameter.Optional }
			);

			routes.MapRoute(
				name: "CompetencyFrameworkDetail",
				url: "competencyframework/{id}/{name}",
				defaults: new { controller = "Detail", action = "CompetencyFramework", name = UrlParameter.Optional }
			);
			routes.MapRoute(
				name: "credentialsSearch",
				url: "credentials",
				defaults: new { controller = "Search", action = "Index2", searchType = "credential" }
			);
			routes.MapRoute(
				name: "orgSearch",
				url: "organizations",
				defaults: new { controller = "Search", action = "Index2", searchType = "organization" }
			);
			routes.MapRoute(
				name: "asmtSearch",
				url: "assessments",
				defaults: new { controller = "Search", action = "Index2", searchType = "assessment" }
			);
			routes.MapRoute(
				name: "loppSearch",
				url: "learningopportunities",
				defaults: new { controller = "Search", action = "Index2", searchType = "learningopportunity" }
			);
			routes.MapRoute(
				name: "loppSearch2",
				url: "lopps",
				defaults: new { controller = "Search", action = "Index2", searchType = "learningopportunity" }
			);
			routes.MapRoute(
				name: "CompetencyFrameworkSearch",
				url: "competencyframeworks",
				defaults: new { controller = "Search", action = "Index2", searchType = "competencyframework" }
			);
			routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );

        }

    }
}

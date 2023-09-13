using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using CredentialFinderWebAPI.Models;

using WMA = workIT.Models.API;
using ServiceHelper = workIT.Services.API.ServiceHelper;
using workIT.Services;
using workIT.Utilities;

namespace CredentialFinderWebAPI.Controllers
{
	public class HomeController : Controller
	{
		public ActionResult Index()
		{
			ViewBag.Title = "Home Page";

			return View();
		}

		[HttpGet, Route( "FinderHome/" )]

		public void FinderHome()
		{
			var response = new ApiResponse();
			List<string> messages = new List<string>();

			List<WMA.LabelLink> output = new List<WMA.LabelLink>();
			var endpoint = "search/";
			try
			{
				List<int> excludeList = new List<int>() { 11, 19, 20 };
				var homePageEntities = UtilityManager.GetAppKeyValue( "entitiesForHomePageCounts" );
				var list = ReportServices.MainEntityTotals( false, homePageEntities );
				foreach ( var item in list )
				{
					var skipURL = false;
					if ( item.Id == 19 || item.Id == 20 || item.Id == 36 || item.Id == 37 )
						skipURL = true;
					output.Add( new WMA.LabelLink()
					{
						Label = item.Description, //description should contain the display friendly label
						Total = item.Totals,
						//try without base
						//URL = ServiceHelper.reactFinderSiteURL + endpoint + item.Name.Replace( " ", "" ),
						//skip: 19,20, 31, for 36/37, set to lopp?
						URL = skipURL ? "" : endpoint + item.Name.Replace( " ", "" ),
						TestURL = ServiceHelper.oldCredentialFinderSite + endpoint + item.Name.Replace( " ", "" )
					} );
				}
				//TODO - how to handle add ons like creds with outcomes, and lopps with outcomes?

				response.Result = output;

				if ( messages.Any() )
				{
					new BaseController().SendResponse( messages );
				}
				else
				{
					response.Successful = true;
					//response.Result = results;
					//var finalResult = JObject.FromObject( new { data = results, valid = valid, status = status } );
					new BaseController().SendResponse( response );
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "API-Home.Index" );
				response.Messages.Add( string.Format( "Error encountered returning data. {0} ", ex.Message ) );
				response.Successful = false;
				new BaseController().SendResponse( response );

			}
		}
	}
}

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
				var list = ReportServices.MainEntityTotals( false );
				foreach ( var item in list )
				{
					output.Add( new WMA.LabelLink()
					{
						Label = item.Name,
						Total = item.Totals,
						URL = ServiceHelper.externalFinderSiteURL + endpoint + item.Name.Replace( " ", "" ),
						TestURL = ServiceHelper.baseFinderSiteURL + endpoint + item.Name.Replace( " ", "" )
					} );
				}
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

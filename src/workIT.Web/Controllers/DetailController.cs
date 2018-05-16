using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using System.Net.Http;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.ProfileModels;
using workIT.Services;
using workIT.Utilities;

using ImportHelpers;
using workIT.Services;

namespace workIT.Web.Controllers
{
	public class DetailController : WorkIT.Web.Controllers.BaseController
	{
		//AppUser user = new AppUser();
		string status = "";
		bool valid = true;

		public ActionResult Credential( int id, string name = "" )
		{
			Credential entity = new Credential();

			//HttpContext.Server.ScriptTimeout = 300;
			string refresh = Request.Params[ "refresh" ];
			bool skippingCache = FormHelper.GetRequestKeyValue( "skipCache", false );

			var vm = CredentialServices.GetDetail( id, skippingCache );

			if ( id > 0 && vm.Id == 0 )
			{
				SetPopupErrorMessage( "ERROR - the requested Credential record was not found " );
				return RedirectToAction( "Index", "Home" );
			}
            ActivityServices.SiteActivityAdd( "Credential", "View", string.Format("User viewed Credential: {0} ({1})",vm.Name, id), 0, 0, id );

			return View( "~/Views/Detail/Detail.cshtml", vm );
		}
		//

		public ActionResult Organization( int id, string name = "" )
		{
			bool skippingCache = FormHelper.GetRequestKeyValue( "skipCache", false );

			Organization vm = OrganizationServices.GetDetail( id, skippingCache );

			if ( id > 0 && vm.Id == 0 )
			{
				SetPopupErrorMessage( "ERROR - the requested organization record was not found " );
				return RedirectToAction( "Index", "Home" );
			}
            ActivityServices.SiteActivityAdd( "Organization", "View", string.Format( "User viewed Organization: {0} ({1})", vm.Name, id ), 0, 0, id );
            return View( "~/Views/Detail/Detail.cshtml", vm );
        }
		//
		public ActionResult QAOrganization( int id, string name = "" )
		{
			return Organization( id, name );
			/*
			if ( User.Identity.IsAuthenticated )
				user = AccountServices.GetCurrentUser( User.Identity.Name );

			Organization vm = new Organization();
			//check if can view the org
			//method returns the org as well
			//17-03-08 mp - so far no difference in call for a QA org. Appropriate data will be returned for view to handle
			if ( !OrganizationServices.CanUserViewQAOrganization( id, user, ref vm ) )
			{
				if ( vm.Id > 0 )
					Session[ "SystemMessage" ] = msg;
				else
					Session[ "SystemMessage" ] = notFoundMsg;
				return RedirectToAction( "Index", "Message" );
			}

			//var vm = OrganizationServices.GetOrganizationDetail( id, user );

			if ( id > 0 && vm.Id == 0 )
			{
				SetPopupErrorMessage( "ERROR - the requested organization record was not found " );
				return RedirectToAction( "Index", "Home" );
			}

			if ( Request.Params[ "v2" ] == "true" )
			{
				return View( "~/Views/V2/Detail/Index.cshtml", vm );
			}

			if ( Request.Params[ "v3" ] == "true" )
			{
				return View( "~/Views/V2/DetailV3/Detail.cshtml", vm );
			}

			return View( "~/Views/Detail/Index.cshtml", vm );
			*/
		}
		//

		public ActionResult Assessment( int id, string name = "" )
		{

			AssessmentProfile vm = AssessmentServices.GetDetail( id );

			if ( id > 0 && vm.Id == 0 )
			{
				SetPopupErrorMessage( "ERROR - the requested Assessment record was not found " );
				return RedirectToAction( "Index", "Home" );
			}
            ActivityServices.SiteActivityAdd( "Assessment", "View", string.Format( "User viewed Assessment: {0} ({1})", vm.Name, id ), 0, 0, id );
            return View( "~/Views/Detail/Detail.cshtml", vm );
        }
		//

		public ActionResult LearningOpportunity( int id, string name = "" )
		{

			var vm = LearningOpportunityServices.GetDetail( id );


			if ( id > 0 && vm.Id == 0 )
			{
				SetPopupErrorMessage( "ERROR - the requested Learning Opportunity record was not found " );
				return RedirectToAction( "Index", "Home" );
			}
            ActivityServices.SiteActivityAdd( "LearningOpportunity", "View", string.Format( "User viewed LearningOpportunity: {0} ({1})", vm.Name, id ), 0, 0, id );
            return View( "~/Views/Detail/Detail.cshtml", vm );
        }

		//Used because HttpClient doesn't work in views for some reason
		public static string MakeHttpGet( string url )
		{
			return new HttpClient().GetAsync( url ).Result.Content.ReadAsStringAsync().Result;
		}
		//

	}
}
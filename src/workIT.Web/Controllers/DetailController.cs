using System;
using System.Collections.Generic;
using System.Linq;
//using SW=System.Web;
using System.Web.Mvc;
using System.Text;
using System.Net.Http;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.ProfileModels;
using workIT.Services;
using workIT.Utilities;

using ImportHelpers;


namespace workIT.Web.Controllers
{
    public class DetailController : workIT.Web.Controllers.BaseController
    {
        //AppUser user = new AppUser();
        string status = "";
        bool valid = true;

        public ActionResult Credential( string id, string name = "" )
        {
            //Credential entity = new Credential();
            int credId = 0;
            var entity = new Credential();
            string refresh = Request.Params[ "refresh" ];
            bool skippingCache = FormHelper.GetRequestKeyValue( "skipCache", false );
            List<string> messages = new List<string>();
            if ( int.TryParse(id,out credId) )
            {
                entity = CredentialServices.GetDetail( credId, skippingCache );
            }
            else if( ServiceHelper.IsValidCtid(id,ref messages ))
            {
                entity = CredentialServices.GetDetailByCtid( id, skippingCache );  
            }
            else
            {
                SetPopupErrorMessage( "ERROR - Enter the ctid which starts with 'ce' or Enter the id " );
                return RedirectToAction( "Index", "Home" );
            }
            //HttpContext.Server.ScriptTimeout = 300;
          
            if (  entity.Id == 0 )
            {
                SetPopupErrorMessage( "ERROR - the requested Credential record was not found " );
                return RedirectToAction( "Index", "Home" );
            }
            ActivityServices.SiteActivityAdd( "Credential", "View", string.Format( "User viewed Credential: {0} ({1})", entity.Name, entity.Id ), 0, 0, credId );

            return View( "~/Views/Detail/Detail.cshtml", entity );
        }
        //
        public ActionResult CredentialByCtid( string ctid, string name = "" )
        {
            Credential entity = new Credential();

            //HttpContext.Server.ScriptTimeout = 300;
            string refresh = Request.Params[ "refresh" ];
            bool skippingCache = FormHelper.GetRequestKeyValue( "skipCache", false );

            var vm = CredentialServices.GetDetailByCtid( ctid, skippingCache );

            if (  vm.Id == 0 )
            {
                SetPopupErrorMessage( "ERROR - the requested Credential record was not found " );
                return RedirectToAction( "Index", "Home" );
            }
            ActivityServices.SiteActivityAdd( "Credential", "View", string.Format( "User viewed Credential: {0} ({1})", vm.Name, vm.Id ), 0, 0, vm.Id );

            return View( "~/Views/Detail/Detail.cshtml", vm );
        }
        //

        public ActionResult Organization( string id, string name = "" )
        {
            //Organization entity = new Organization();
            int orgId = 0;
            var entity = new Organization();
            string refresh = Request.Params[ "refresh" ];
            bool skippingCache = FormHelper.GetRequestKeyValue( "skipCache", false );
            List<string> messages = new List<string>();
                if ( int.TryParse( id, out orgId ) )
            {
                entity = OrganizationServices.GetDetail( orgId, skippingCache );
            }
            else if ( ServiceHelper.IsValidCtid( id, ref messages ) )
            {
                entity = OrganizationServices.GetDetailByCtid( id, skippingCache );
            }
            else
            {
                SetPopupErrorMessage( "ERROR - Enter the ctid which starts with 'ce' or Enter the id " );
                return RedirectToAction( "Index", "Home" );
            }
            if ( entity.Id == 0 )
            {
                SetPopupErrorMessage( "ERROR - the requested Organization record was not found " );
                return RedirectToAction( "Index", "Home" );
            }
            ActivityServices.SiteActivityAdd( "Organization", "View", string.Format( "User viewed Organization: {0} ({1})", entity.Name, entity.Id ), 0, 0, orgId );

            return View( "~/Views/Detail/Detail.cshtml", entity );

        }
        //
        public ActionResult QAOrganization( string id, string name = "" )
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

        public ActionResult Assessment( string id, string name = "" )
        {
            //AssessmentProfile entity = new AssessmentProfile();
            int assmId = 0;
            var entity = new AssessmentProfile();
            string refresh = Request.Params[ "refresh" ];
            bool skippingCache = FormHelper.GetRequestKeyValue( "skipCache", false );
            List<string> messages = new List<string>();
            if ( int.TryParse( id, out assmId ) )
            {
                entity = AssessmentServices.GetDetail( assmId, skippingCache );
            }
            else if ( ServiceHelper.IsValidCtid( id, ref messages ) )
            {
                entity = AssessmentServices.GetDetailByCtid( id, skippingCache );
            }
            else
            {
                SetPopupErrorMessage( "ERROR - Enter the ctid which starts with 'ce' or Enter the id " );
                return RedirectToAction( "Index", "Home" );
            }
            //HttpContext.Server.ScriptTimeout = 300;

            if ( entity.Id == 0 )
            {
                SetPopupErrorMessage( "ERROR - the requested Assessment record was not found " );
                return RedirectToAction( "Index", "Home" );
            }
            ActivityServices.SiteActivityAdd( "AssessmentProfile", "View", string.Format( "User viewed Assessment: {0} ({1})", entity.Name, entity.Id ), 0, 0, assmId );

            return View( "~/Views/Detail/Detail.cshtml", entity );
        }
        //

        public ActionResult LearningOpportunity( string id, string name = "" )
        {

            //LearningOpportunityProfile entity = new LearningOpportunityProfile();
            int loppId = 0;
            var entity = new LearningOpportunityProfile();
            string refresh = Request.Params[ "refresh" ];
            bool skippingCache = FormHelper.GetRequestKeyValue( "skipCache", false );
            List<string> messages = new List<string>();
            if ( int.TryParse( id, out loppId ) )
            {
                entity = LearningOpportunityServices.GetDetail( loppId, skippingCache );
            }
            else if ( ServiceHelper.IsValidCtid( id, ref messages ) )
            {
                entity = LearningOpportunityServices.GetDetailByCtid( id, skippingCache );
            }
            else
            {
                SetPopupErrorMessage( "ERROR - Enter the ctid which starts with 'ce' or Enter the id " );
                return RedirectToAction( "Index", "Home" );
            }
            //HttpContext.Server.ScriptTimeout = 300;

            if ( entity.Id == 0 )
            {
                SetPopupErrorMessage( "ERROR - the requested LearningOpportunity record was not found " );
                return RedirectToAction( "Index", "Home" );
            }
            ActivityServices.SiteActivityAdd( "LearningOpportunity", "View", string.Format( "User viewed LearningOpportunity: {0} ({1})", entity.Name, entity.Id ), 0, 0, loppId );

            return View( "~/Views/Detail/Detail.cshtml", entity );
        }

        //Used because HttpClient doesn't work in views for some reason
        public static string MakeHttpGet( string url )
        {
            return new HttpClient().GetAsync( url ).Result.Content.ReadAsStringAsync().Result;
        }
        //
    }
}
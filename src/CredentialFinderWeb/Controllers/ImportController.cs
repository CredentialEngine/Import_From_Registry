using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.ProfileModels;
using workIT.Services;
using workIT.Utilities;
using ImportHelpers;

namespace CredentialFinderWeb.Controllers
{

	public class ImportController : BaseController
    {
		SiteMessage msg = new SiteMessage();
		bool isAuthRequiredForImport = UtilityManager.GetAppKeyValue( "isAuthRequiredForImport", false );
		private bool IsAuthorized()
		{
			if ( !isAuthRequiredForImport )
				return true;

			if ( AccountServices.IsUserSiteStaff() )
				return true;
			else
				return false;
		}
		// Show view to enter ctid or envelopeid
		//[Authorize]
		public ActionResult Index()
        {
			SaveStatus status = new SaveStatus();

			return View( status );
        }

		//[Authorize]
		[ HttpGet, Route( "Import/ByEnvelopeId/{envelopeId}" )]
		public ActionResult ByEnvelopeId( string envelopeId)
		{
			if ( !IsAuthorized() )
			{
				msg.Title = "ERROR - you are not authorized for this action.";
				msg.Message = "<a href='/Account/Login'>Please log in</a> with an account that has sufficient authority.";
				Session[ "siteMessage" ] = msg.Message;
				return RedirectToAction( "About", "Home" );
			}
			ImportRequest mgr = new ImportRequest();
			SaveStatus status = new SaveStatus();
			
			if ( !string.IsNullOrWhiteSpace( envelopeId )  )
			{
				LoggingHelper.DoTrace( 4, "ImportController. Starting ByEnvelopeId: " + envelopeId );
				status = mgr.ImportByEnvelopeId( envelopeId );
               // if ( !status.HasErrors )
                    ElasticServices.UpdateElastic(false);
            }
			else
			{
				SetPopupErrorMessage( "ERROR - provide a valid envelopeId " );
				msg.Title = "ERROR - provide a valid CTID or envelopeId";
				msg.Message = "Either a valid CTID, or a valid registry envelope identifier must be provided.";
				Session[ "siteMessage" ] = msg;
				return View();

				//return RedirectToAction( "Index", "Home" );
			}

			return View( "Index", status );
		}

		//[Authorize]
		[HttpGet, Route( "Import/byctid/{ctid}" )]
		public ActionResult ByCtid( string ctid )
		{
			if ( !IsAuthorized() )
			{
				if ( UtilityManager.GetAppKeyValue( "envType" ) != "production" )
				{
					//allow, and log
					string referer = GetUserReferrer();
					string ipAddress = this.GetUserIPAddress();
					LoggingHelper.DoTrace( 4, string.Format( "ImportController.ByCtid() NON-AUTHENTICATED REQUEST. For Ctid: {0}, referer: {1}, ipAddress: {2}", ctid, referer, ipAddress ) );
				}
				else
				{
					msg.Title = "ERROR - you are not authorized for this action.";
					msg.Message = "<a href='/Account/Login'>Please log in</a> with an account that has sufficient authority.";
					Session[ "siteMessage" ] = msg.Message;
					return RedirectToAction( "Index", "Home" );
				}
			}
			ImportRequest mgr = new ImportRequest();
			SaveStatus status = new SaveStatus();

			if ( !string.IsNullOrWhiteSpace( ctid ) )
			{
				LoggingHelper.DoTrace( 4, "ImportController. Starting ByCtid: " + ctid );
				status = mgr.ImportByCtid( ctid );
                //if ( !status.HasErrors )
                    ElasticServices.UpdateElastic( false );
            }
			else
			{
				SetPopupErrorMessage( "ERROR - provide a valid ctid " );
				msg.Title = "ERROR - provide a valid CTID or envelopeId";
				msg.Message = "Either a valid CTID, or a valid registry envelope identifier must be provided.";
				Session[ "siteMessage" ] = msg;
				return View( "index", status );
			}

			return View( "index", status );
		}

		//[Authorize]
		//[HttpPost, Route( "Import/doimport/{model}" )]
		public ActionResult DoImport( SaveStatus model )
		{
			ImportRequest mgr = new ImportRequest();
			SaveStatus status = new SaveStatus();
			status.Community = status.Community ?? "";
			if ( !string.IsNullOrWhiteSpace( model.Ctid ) && model.Ctid.Length == 39 )
			{
				status = mgr.ImportByCtid( model.Ctid );
                //if ( !status.HasErrors )
                    ElasticServices.UpdateElastic( false );

            }
			else if ( !string.IsNullOrWhiteSpace( model.EnvelopeId ) && model.EnvelopeId.Length == 36 )
			{
				status = mgr.ImportByEnvelopeId( model.EnvelopeId );
                //if ( !status.HasErrors )
                    ElasticServices.UpdateElastic( false );

            }
			else
			{
				SetPopupErrorMessage( "ERROR - provide a valid CTID or envelopeId " );
				msg.Title = "ERROR - provide a valid CTID or envelopeId";
				msg.Message = "Either a valid CTID, or a valid registry envelope identifier must be provided.";
				Session[ "siteMessage" ] = msg;
				return View( "index" );

				//return RedirectToAction( "Index", "Home" );
			}
            if (status.Messages.Count == 0 && string.IsNullOrWhiteSpace(status.DetailPageUrl))
            {
                status.Messages.Add( new StatusMessage() { Message = "Import was successful, there is no detail page for this document type.", IsWarning = true } );
            }
			return View( "Index", status );
		}

		//[Authorize]
		public JsonResult Reimport( ReimportClass context )
		{
            //Check permission - maybe later
            //if ( !IsAuthorized() )
            //{
            //	return JsonHelper.GetJsonWithWrapper( null, false, "You are not authorized for this request type.", null );
            //}
            bool valid = true;

			ImportRequest mgr = new ImportRequest();
			SaveStatus status = new SaveStatus();
			//Do the register
			switch ( context.TypeName )
			{
				case "credential":
				case "CredentialProfile":
					if ( !string.IsNullOrWhiteSpace( context.Ctid ) )
						status = mgr.ImportCredentialByCtid( context.Ctid );
					else
					if (!string.IsNullOrWhiteSpace(context.CredentialRegistryId))
						status = mgr.ImportCredential( context.CredentialRegistryId );
					else
						status.AddError( "Must provide either an valid CTID or envelopeId" );
					break;

				case "organization":
				case "QAOrganization":
					if ( !string.IsNullOrWhiteSpace( context.Ctid ) )
						status = mgr.ImportOrganizationByCtid( context.Ctid );
					else if( !string.IsNullOrWhiteSpace( context.CredentialRegistryId ) )
						status = mgr.ImportOrganization( context.CredentialRegistryId );
					else
						status.AddError( "Must provide either an valid CTID or envelopeId" );
					break;
				case "AssessmentProfile":
				case "assessment":
					if ( !string.IsNullOrWhiteSpace( context.Ctid ) )
						status = mgr.ImportAssessmentByCtid( context.Ctid );
					else if( !string.IsNullOrWhiteSpace( context.CredentialRegistryId ) )
						status = mgr.ImportAssessment( context.CredentialRegistryId );
					else
						status.AddError( "Must provide either an valid CTID or envelopeId" );

					break;
				case "LearningOpportunityProfile":
				case "learningopportunity":
					if ( !string.IsNullOrWhiteSpace( context.Ctid ) )
						status = mgr.ImportLearningOpportuntyByCtid( context.Ctid );
					else if( !string.IsNullOrWhiteSpace( context.CredentialRegistryId ) )
						status = mgr.ImportLearningOpportunty( context.CredentialRegistryId );
					else
						status.AddError( "Must provide either an valid CTID or envelopeId" );
					
					break;
				default:
					valid = false;
					status.AddError( "Profile not handled" );
					break;
			}
			string returnStatus = "";
			if ( status.HasErrors )
			{
				valid = false;
				returnStatus = string.Join( "<br/>", status.GetAllMessages().ToArray() );
			} else if (status.GetAllMessages().Count > 0)
			{
				returnStatus = string.Join( "<br/>", status.GetAllMessages().ToArray() );
			} else
			{
				returnStatus = "Seems to have worked??";
                ElasticServices.UpdateElastic( true );
            }
			
			
			//Return the result
			return JsonHelper.GetJsonWithWrapper( null, valid, returnStatus, null );
		}

        public JsonResult Reindex( ReimportClass context )
        {
            //Check permission 
            if ( !IsAuthorized() )
            {
                return JsonHelper.GetJsonWithWrapper( null, false, "You are not authorized for this request type.", null );
            }
            bool valid = true;

            string filter = string.Format( " base.CTID = '{0}'", context.Ctid );
            int processed = 0;
            string returnStatus = "";
            //Do the request
            switch ( context.TypeName )
            {
                case "credential":
                case "CredentialProfile":
                    ElasticServices.Credential_UpdateIndex( filter, ref processed );
                    break;

                case "organization":
                case "QAOrganization":
                    ElasticServices.Organization_UpdateIndex( filter, ref processed );
                    break;
                case "AssessmentProfile":
                case "assessment":
                    ElasticServices.Assessment_UpdateIndex( filter, ref processed );
                    break;
                case "LearningOpportunityProfile":
                case "learningopportunity":
                    ElasticServices.LearningOpp_UpdateIndex( filter, ref processed );
                    break;
                default:
                    valid = false;
                    returnStatus = "Profile not handled";
                    return JsonHelper.GetJsonWithWrapper( null, valid, returnStatus, null );
                    //break;
            }

            if ( processed > 0 )
                returnStatus = "Seems to have worked, try searching again.";
            else
            {
                valid = false;
                returnStatus = "Hmmm - didn't seem to work.";
            }

            //Return the result
            return JsonHelper.GetJsonWithWrapper( null, valid, returnStatus, null );
        }
		private string GetUserReferrer()
		{
			string lRefererPage = "unknown";
			try
			{
				if ( Request.UrlReferrer != null )
				{
					lRefererPage = Request.UrlReferrer.ToString();
					//check for link to us parm
					//??

				}
			}
			catch ( Exception ex )
			{
				lRefererPage = ex.Message;
			}

			return lRefererPage;
		} //
		private string GetUserIPAddress()
		{
			string ip = "unknown";
			try
			{
				ip = Request.ServerVariables[ "HTTP_X_FORWARDED_FOR" ];
				if ( ip == null || ip == "" || ip.ToLower() == "unknown" )
				{
					ip = Request.ServerVariables[ "REMOTE_ADDR" ];
				}
			}
			catch ( Exception ex )
			{

			}

			return ip;
		} //
		public class ReimportClass
		{
			public int Id { get; set; }
			public string Ctid { get; set; }
			public string CredentialRegistryId { get; set; }
			public string TypeName { get; set; } //Profile being targeted/worked on directly

		}
	}
}
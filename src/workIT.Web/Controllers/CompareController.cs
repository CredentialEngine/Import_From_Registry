using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using workIT.Utilities;
using workIT.Models.Helpers;
using workIT.Services;

namespace workIT.Web.Controllers
{
    public class CompareController : Controller
    {
        string sessionKey = "compare";

        //Load Compare page
        public ActionResult Index()
        {
            return V3();
   //         var vm = new CompareItemSummary();

   //         var lists = GetSessionItems();
   //         foreach ( var item in lists )
   //         {
   //             switch ( item.Type )
   //             {
			//		case "credential":
			//			vm.Credentials.Add( CredentialServices.GetCredentialForCompare( item.Id ) );
			//			break;
			//		case "organization":
			//			vm.Organizations.Add( OrganizationServices.GetDetail( item.Id ) );
			//			break;
			//		case "assessment":
			//			vm.Assessments.Add( AssessmentServices.GetDetail( item.Id ) );
			//			break;
			//		case "learningopportunity":
			//			vm.LearningOpportunities.Add( LearningOpportunityServices.GetDetail( item.Id ) );
			//			break;
			//		default:
			//			break;
			//	}
   //         }

			//return View( "CompareV3", vm );
		}

        public ActionResult V3()
        {
            var vm = new CompareItemSummary();
            string credentialCompare = "";
            string orgCompare = "";
            string asmtCompare = "";
            string loppCompare = "";

            var lists = GetSessionItems();
            foreach ( var item in lists )
            {
                switch ( item.Type )
                {
                    case "credential":
                        var entity = CredentialServices.GetCredentialForCompare( item.Id );
                        vm.Credentials.Add( entity );
                        ActivityServices.SiteActivityAdd( "Credential", "Compare", string.Format( "User doing compare on Credential: {0} ({1})", entity.Name, entity.Id ), 0, 0, entity.Id );
                        break;
                    case "organization":
                        var org = OrganizationServices.GetDetail( item.Id );
                        vm.Organizations.Add( org );
                        ActivityServices.SiteActivityAdd( "Organization", "Compare", string.Format( "User doing compare on Organization: {0} ({1})", org.Name, org.Id ), 0, 0, org.Id );
                        break;
                    case "assessment":
                        var asmt = AssessmentServices.GetDetail( item.Id );
                        vm.Assessments.Add( asmt );
                        ActivityServices.SiteActivityAdd( "Assessment", "Compare", string.Format( "User doing compare on Assessment: {0} ({1})", asmt.Name, asmt.Id ), 0, 0, asmt.Id );
                        break;
                    case "learningopportunity":
                        var lopp = LearningOpportunityServices.GetDetail( item.Id );
                        vm.LearningOpportunities.Add( lopp );
                        ActivityServices.SiteActivityAdd( "LearningOpportunity", "Compare", string.Format( "User doing compare on Learning opportunity: {0} ({1})", lopp.Name, lopp.Id ), 0, 0, lopp.Id );
                        break;
                    default:
                        break;
                }
            }

            return View( "CompareV3", vm );
        }
        //

        //Store a compare item
        public JsonResult AddItem( CompareItem input )
        {
            var items = GetSessionItems();
            var existing = items.FirstOrDefault( m => m.Id == input.Id && m.Type == input.Type );
            if ( existing == null )
            {
                //Don't allow too many items to be compared
                if ( items.Count() >= 10 )
                {
                    return JsonHelper.GetJsonWithWrapper( null, false, "You can only compare up to 10 items. Please remove one or more items and try again.", null );
                }

                //Add the item
                items.Add( new CompareItem()
                {
                    Id = input.Id,
                    Type = input.Type.ToLower(),
                    Title = input.Title
                } );

                UpdateSessionItems( items );

                return JsonHelper.GetJsonWithWrapper( items );
            }
            else
            {
                return JsonHelper.GetJsonWithWrapper( null, false, "That item is already in the list of items to compare!", null );
            }
        }
        //

        //Remove a compare item
        public JsonResult RemoveItem( CompareItem input )
        {
            var items = GetSessionItems();
            var existing = items.FirstOrDefault( m => m.Id == input.Id && m.Type == input.Type );
            if ( existing != null )
            {
                items.Remove( existing );
                UpdateSessionItems( items );
                return JsonHelper.GetJsonWithWrapper( items );
            }
            else
            {
                return JsonHelper.GetJsonWithWrapper( null, false, "Item not found!", null );
            }
        }
        //

        //Get compare items
        public JsonResult GetItems()
        {
            return JsonHelper.GetJsonWithWrapper( GetSessionItems() );
        }
        //

        //Dump all current compare items
        public void DumpItems( string type, int id )
        {
            UpdateSessionItems( new List<CompareItem>() );
        }
        //

        //Update session items
        private void UpdateSessionItems( List<CompareItem> items )
        {
            try
            {
                new HttpSessionStateWrapper( System.Web.HttpContext.Current.Session ).Contents[sessionKey] = items;
            }
            catch
            {
                //
            }
        }
        //

        //Get session items
        private List<CompareItem> GetSessionItems()
        {
            try
            {
                var items = new HttpSessionStateWrapper( System.Web.HttpContext.Current.Session ).Contents[sessionKey] as List<CompareItem>;
                if ( items == null )
                {
                    return new List<CompareItem>();
                }
                else
                {
                    return items;
                }
            }
            catch
            {
                return new List<CompareItem>();
            }
        }
        //

    }
}
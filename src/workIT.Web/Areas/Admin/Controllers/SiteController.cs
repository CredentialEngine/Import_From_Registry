using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

using CM = workIT.Models.Common;
using workIT.Factories;
using workIT.Services;
using workIT.Utilities;


namespace workIT.Web.Areas.Admin.Controllers
{
    public class SiteController : workIT.Web.Controllers.BaseController
	{
        // GET: Admin/Site
        public ActionResult Index()
        {
            return View();
        }


        // fix addresses missing lat/lng, and normalize
        //[Authorize( Roles = "Administrator, Site Staff" )]
        public ActionResult NormalizeAddresses( int maxRecords = 100 )
        {
            var user = AccountServices.GetCurrentUser();
            if ( user != null && 
                ( 
                    user.Email == "email@email.com" ||
                    user.Email == "email@email.com"
                  )
                )
            {
                //next
            } else 
            //if ( !User.Identity.IsAuthenticated
            //    || ( User.Identity.Name != "mparsons"
            //    && User.Identity.Name != "email@email.com" )
            //    )
            {
                SetSystemMessage( "Unauthorized Action", "You are not authorized to invoke NormalizeAddresses." );
                return RedirectToAction( "Index", "Message" );
            }
            string report = "";
            string messages = "";
            List<CM.Address> list = new Entity_AddressManager().ResolveMissingGeodata( ref messages, maxRecords: maxRecords );

            if ( !string.IsNullOrWhiteSpace( messages ) )
                report = "<p>Normalize Addresses: <br/>" + messages + "</p>"  ;

            foreach ( var address in list )
            {
                string msg = string.Format( " - Unable to resolve address: Id: {0}, address1: {1}, city: {2}, region: {3}, postalCode: {4}, country: {5} ", address.Id, address.Address1, address.City, address.AddressRegion, address.PostalCode, address.Country );
                LoggingHelper.DoTrace(2, msg );
                report += System.Environment.NewLine + msg;
            }

            SetSystemMessage( "Normalize Addresses", report );

            return RedirectToAction( "Index", "Message", new { area = "" } );
        }

        public ActionResult HandlePendingDeletes( int maxRecords = 100 )
        {
            List<String> messages = new List<string>();
            ElasticServices.HandlePendingDeletes(ref messages);
            string report = string.Join( "<br/>", messages.ToArray() ); 
            SetSystemMessage( "Handling Pending Deletes From Elastic", report );

            return RedirectToAction( "Index", "Message", new { area = "" } );
        }
        public ActionResult HandlePendingUpdates( int maxRecords = 100 )
        {
            List<String> messages = new List<string>();
            ElasticServices.HandlePendingReindexRequests( ref messages );
            string report = string.Join( "<br/>", messages.ToArray() );
            SetSystemMessage( "Handling Pending Updates From Elastic", report );

            return RedirectToAction( "Index", "Message", new { area = "" } );
        }
    }
}
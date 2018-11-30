using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.SessionState;
using Newtonsoft.Json;
using workIT.Models;
using workIT.Models.Common;
using Manager = workIT.Factories.WidgetManager;
namespace workIT.Services
{
    public class WidgetServices
    {
        Manager mgr = new Manager();

        public bool Save( Widget item, ref List<string> messages )
        {
            AppUser user = AccountServices.GetCurrentUser();
            if ( user == null || user.Id == 0 )
            {
                messages.Add( string.Format( "You must be logged in to: '{0}'.", "Save a widget" ) );
                return false;
            }
            if ( !AccountServices.IsUserSiteStaff( user ) )
            {
                //check if user is part of organization related to widget
                var exists = user.Organizations.FirstOrDefault( a => a.CTID.ToLower() == item.OrgCTID.ToLower() );
                if ( exists == null || string.IsNullOrWhiteSpace( exists.CTID ) )
                {
                    messages.Add( string.Format( "You are not associated with the organization for this widget: {0}.", item.OrganizationName ) );
                    return false;
                }
            }

            if ( !mgr.Save( item, user.Id, ref messages ) )
            {
                //message handling, or leave to caller
                return false;
            }
            return true;
        }

        public bool Delete( int id, ref string message )
        {
            bool isValid = true;
            AppUser user = AccountServices.GetCurrentUser();
            if ( user == null || user.Id == 0 )
            {
                message = string.Format( "You must be logged in to: '{0}'.", "Delete a widget" );
            }
            isValid = mgr.Delete( id, ref message );
            return isValid;
        }

        public static Widget Get( int id )
        {
            Widget item = new Widget();

            item = Manager.Get( id );

            return item;
        }
        public static Widget GetByAlias( string alias )
        {
            Widget item = Manager.GetByAlias( alias );

            return item;
        }
        public static Widget GetForUser( int userId )
        {
            Widget item = new Widget();
            item = Manager.GetFirstWidgetForUser( userId );

            return item;
        }

        public static List<Widget> GetWidgetsForOrganization( string ctid )
        {
            return Manager.GetWidgetsForOrganization( ctid );
        }
        public static List<Organization> GetAllOrganizationsWithWidgets()
        {
            return Manager.GetAllOrganizationsWithWidgets( );
        }
        //[Obsolete]
        //public static bool Activate( int widgetId, string message )
        //{

        //    var widget = WidgetServices.Get( widgetId );
        //    if ( widget == null || widget.Id == 0 )
        //    {
        //        message = "ERROR - the requested Widget record was not found ";
        //        return false;
        //    }
        //    else
        //    {
        //        if ( HttpContext.Current != null && HttpContext.Current.Session != null )
        //        {
        //            //may already be in session, so remove and read
        //            if ( HttpContext.Current.Session[ "currentWidget" + widget.Id ] != null )
        //            {
        //                HttpContext.Current.Session.Remove( "currentWidget" + widget.Id );
        //            }
        //            HttpContext.Current.Session[ "currentWidget" + widget.Id ] = widget;
        //            return true;
        //        }
        //    }
        //    message = "Unable to acquire the current session. Widget not activated.";
        //    return false;
        //} //
        //public static bool Activate( Widget widget, string message )
        //{
        //    if ( HttpContext.Current != null && HttpContext.Current.Session != null )
        //    {
        //        //may already be in session, so remove and readd
        //        if ( HttpContext.Current.Session[ "currentWidget" + widget.Id ] != null )
        //        {
        //            HttpContext.Current.Session.Remove( "currentWidget" + widget.Id );
        //        }
        //        HttpContext.Current.Session[ "currentWidget" + widget.Id ] = widget;
        //        return true;
        //    }
        //    message = "Unable to acquire the current session. Widget not activated.";
        //    return false;
        //} //
        public static bool IsWidgetMode()
        {
            if ( HttpContext.Current != null && HttpContext.Current.Session != null )
            {
                var widgetId = Convert.ToInt32( HttpContext.Current.Request.Params[ "widgetId" ] );
                var widget = GetCurrentWidget( HttpContext.Current.Session, widgetId );
                if ( widget == null || widget.Id == 0 )
                    return false;
                else
                    return true;
            }
            else
                return false;
        } //
        public static Widget GetCurrentWidget( int widgetId )
        {
            if ( HttpContext.Current != null && HttpContext.Current.Session != null )
            {

                return GetCurrentWidget( HttpContext.Current.Session, widgetId );
            }
            else
                return null;
        } //
        public static Widget GetCurrentWidget( HttpSessionState session, int widgetId )
        {
            Widget item = new Widget();
            //may already be in session, so remove and readd
            if ( session[ "currentWidget" + widgetId ] != null )
            {
                item = ( Widget )session[ "currentWidget" + widgetId ];

            }
            else
            {
                item = Get( widgetId );
                session[ "currentWidget" + widgetId ] = item;
            }

            return item;
        }

        public static Widget RemoveCurrentWidget()
        {
            if ( HttpContext.Current != null && HttpContext.Current.Session != null )
            {
                return RemoveCurrentWidget( HttpContext.Current.Session );
            }
            else
                return null;
        } //
        public static Widget RemoveCurrentWidget( HttpSessionState session )
        {
            Widget item = new Widget();
            //may already be in session, so remove and readd
            if ( session[ "currentWidget" ] != null )
            {
                session.Remove( "currentWidget" );
            }

            return item;
        }
    }
}

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
		public static List<Widget> GetAllWidgets()
		{
			return Manager.GetAllWidgets();
		}
		public static List<Widget> GetWidgetsForOrganization( string ctid )
        {
            return Manager.GetWidgetsForOrganization( ctid );
        }
        public static List<Organization> GetAllOrganizationsWithWidgets()
        {
            return Manager.GetAllOrganizationsWithWidgets( );
        }

		/// <summary>
		/// Add Widget.Selection
		/// </summary>
		/// <param name="widgetId"></param>
		/// <param name="purpose"></param>
		/// <param name="entityTypeId"></param>
		/// <param name="recordId"></param>
		public bool AddWidgetSelection( string index, int widgetId, int entityTypeId, string widgetSection, string widgetProperty, int recordId, string resourceName, ref List<string> messages )
		{
			//AddWidgetSelection( index, widgetID, entityTypeID, widgetSection, widgetProperty, recordID, resourceName, ref messages );
			//may need to record whether already exists and perhaps? skip the elastic update. Or have elastic also do an exists check
			bool alreadyExists = false;
			var status = "";
			if ( mgr.WidgetSelectionAdd( widgetId, widgetSection, entityTypeId, recordId, resourceName, ref messages, ref alreadyExists ) )
			{
				switch ( entityTypeId )
				{
					case 1:
						if (!new ElasticServices().CredentialResourceAddWidgetId( index, widgetId, widgetProperty, recordId.ToString(), alreadyExists, ref status ))
						{
							messages.Add( status );
							return false;
						}
						return true;
					default:
						messages.Add( "Error there is no elastic method to add a resource of type: " + entityTypeId.ToString() );
						return false;
				}

			} else
			{
				return false;
			}
			
		}

		public bool RemoveWidgetSelection( string index, int widgetId, int entityTypeId, string widgetSection, string widgetProperty,  int recordId, ref List<string> messages )
		{
			var status = "";

			if ( mgr.WidgetSelectionDelete( widgetId, widgetSection, entityTypeId, recordId, ref messages ) )
			{
				switch ( entityTypeId )
				{
					case 1:
						if (!new ElasticServices().CredentialResourceRemoveWidgetId( index, widgetId, widgetProperty, recordId.ToString(), ref status ))
						{
							messages.Add( status );
							return false;
						}
						return true;
					default:
						messages.Add( "Error there is no elastic method to remove a resource of type: " + entityTypeId.ToString() );
						return false;
				}
				
			}
			else
			{
				return false;
			}
		}

		#region Mostly obsolete 
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

		#endregion 
	}
}

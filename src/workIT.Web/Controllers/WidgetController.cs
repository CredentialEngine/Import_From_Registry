﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using workIT.Models.Common;

using workIT.Utilities;
using workIT.Services;
using workIT.Models.Node;
using workIT.Models.Search;
using workIT.Models;
using System.Configuration;
using System.IO;
using Newtonsoft.Json;

namespace workIT.Web.Controllers
{
    public class WidgetController : BaseController
    {
        bool valid = true;
        string status = "";
        SearchServices searchServices = new SearchServices();

        public ActionResult Index()
        {
            return View();
            //return Configure();

        }
        public ActionResult Guidance()
        {
            return View();
        }
        //public ActionResult Show( string widgetAlias )
        //{
        //    if ( !string.IsNullOrWhiteSpace( widgetAlias) )
        //    {
        //        var widget = WidgetServices.GetByAlias( widgetAlias );
        //        //if not found, display message somewhere - console message?
        //        if ( widget == null || widget.Id == 0 )
        //        {
        //            workIT.Models.Common.SiteMessage msg = new workIT.Models.Common.SiteMessage() { Title = "Invalid Widget Request", Message = "ERROR - the requested Widget record was not found ", MessageType = "error" };
        //            Session[ "SystemMessage" ] = msg;
        //            return RedirectToAction( "Index", "Message" );
        //        }
        //        else
        //        {
        //            string message = "";
        //            //may already be in session, so remove and readd
        //            //don't want this any longer
        //            if ( !WidgetServices.Activate( widget, message ) )
        //            {
        //                workIT.Models.Common.SiteMessage msg = new workIT.Models.Common.SiteMessage() { Title = "Invalid Widget Request", Message = message, MessageType = "error" };
        //                Session[ "SystemMessage" ] = msg;
        //                return RedirectToAction( "Index", "Message" );
        //            }

        //            return RedirectToAction( "Index", "Home" );
        //        }
        //    }

        //    return RedirectToAction( "Index", "Home" );
        //}
        //public ActionResult Activate( int widgetId )
        //{
        //    if ( widgetId > 0 )
        //    {
        //        var widget = WidgetServices.Get( widgetId );
        //        //if not found, display message somewhere - console message?
        //        if ( widget == null || widget.Id == 0 )
        //        {
        //            workIT.Models.Common.SiteMessage msg = new workIT.Models.Common.SiteMessage() { Title = "Invalid Widget Request", Message = "ERROR - the requested Widget record was not found ", MessageType = "error" };
        //            Session[ "SystemMessage" ] = msg;
        //            return RedirectToAction( "Index", "Message" );
        //        }
        //        else
        //        {
        //            string message = "";
        //            //may already be in session, so remove and readd
        //            if ( !WidgetServices.Activate( widget, message ) )
        //            {
        //                workIT.Models.Common.SiteMessage msg = new workIT.Models.Common.SiteMessage() { Title = "Invalid Widget Request", Message = message, MessageType = "error" };
        //                Session[ "SystemMessage" ] = msg;
        //                return RedirectToAction( "Index", "Message" );
        //            }

        //            return RedirectToAction( "Index", "Home" );
        //        }
        //    }

        //    return RedirectToAction( "Index", "Home" );
        //}
        public ActionResult Remove()
        {
            WidgetServices.RemoveCurrentWidget();
            return RedirectToAction( "Index", "Home" );
        }
        public ActionResult TestWidget( int widgetId )
        {
            ViewBag.widgetid = widgetId;
            return View();
        }

        //Load page to configure a widget
        public ActionResult Configure()
        {
            AppUser user = AccountServices.GetCurrentUser();
            if ( user == null || user.Id == 0 )
            {
                SiteMessage msg = new SiteMessage();
                msg.Title = "ERROR - you are not authorized for this action.";
                msg.Message = "<a href='/Account/Login'>Please log in</a>to enable managing widgets.";
                Session[ "siteMessage" ] = msg.Message;

                return RedirectToAction( "About", "Home" );
            }
            if ( user.Organizations != null && ( user.Organizations.Count > 0 || AccountServices.IsUserSiteStaff(user) ))
            {
                return View( "~/Views/Widget/Configure.cshtml", new Widget() );
            }
            else 
            {
                SiteMessage msg = new SiteMessage() { Title = "Not Authorized to Access Widgets", Message = "You must be associated with an approved Credential Engine organization in order to create Widgets. Please ensure your organization is registered with the Credential Engine accounts site, and it is approved to create widgets. " };
                Session[ "SystemMessage" ] = msg;
                return RedirectToAction( "Index", "Message" );
            }
            
        }

        [HttpPost]
        public JsonResult SaveWidget()
        {
            bool isFileUploaded = false;
            List<string> messages = new List<string>();
            Widget widgetData = new Widget();
            WidgetServices widgetService = new WidgetServices();

            string fileUploadStatus = "";
            var w1 = Request.Params[ "widgetData" ];
            if ( w1 != null )
            {
                LoggingHelper.DoTrace( 6, "WidgetController.SaveWidget. Entered." );

                widgetData = JsonConvert.DeserializeObject<Widget>( Request.Params[ "widgetData" ] );
                if ( !string.IsNullOrWhiteSpace( widgetData.WidgetAlias ) )
                {
                    var widgetExisting = WidgetServices.GetByAlias( widgetData.WidgetAlias );
                    if ( widgetExisting != null && widgetExisting.Id > 0 && widgetExisting.Id != widgetData.Id ) {
                        messages.Add( "Widget Alias already exist" );
                        return JsonResponse( widgetData, false, "failure", messages );
                    }
                }

                if ( !widgetService.Save( widgetData, ref messages ) )
                {
                    LoggingHelper.DoTrace( 5, "WidgetController.SaveWidget. Errors on save: " + string.Join("\n\r>",messages) );
                    return JsonResponse( widgetData, false, "", messages );
                }

                //don't call activate
                //WidgetServices.Activate( widgetData, string.Empty );

                //WHY the check for Files?
                //Need a check for actual entered data
                //skip for now
                if ( string.IsNullOrEmpty( widgetData.WidgetStylesUrl ) && Request.Files.Count == 0 )
                {
                    //TODO - if no style changes, should delete any existing stylesheet
                    if ( widgetData.WidgetStyles != null
                        && widgetData.WidgetStyles.HasChanged() )
                    {
                        //if ( this.ParseAndCreateUserStyleTemplate( widgetData, this.ControllerContext, Server.MapPath( "~/" ), "UserStyleTemplate", ref messages ) )
                        //    widgetService.Save( widgetData, ref messages );
                        //else
                        //{
                        //    //for now ignore errors?
                        //    return JsonResponse( widgetData, false, "failure", messages );
                        //}
                    }
                }
                //plan to chg this to get the text and pass as a string
                //if ( Request.Files.Count > 0 )
                //{
                //    LoggingHelper.DoTrace( 4, "WidgetController.SaveWidget. Have widget data and found files" );
                //    isFileUploaded = this.UploadStyle( Request.Files[ 0 ], widgetData );

                //    widgetService.Save( widgetData, ref messages );

                //    fileUploadStatus = isFileUploaded ? "Style template upload success" : fileUploadStatus;
                //    if ( !string.IsNullOrWhiteSpace( fileUploadStatus ) )
                //    {
                //        messages.Add( fileUploadStatus );
                //    }
                //}
            }
            else
            {
                //plan to chg this to get the text and pass as a string
                //if ( Request.Files.Count > 0 )
                //{
                //    LoggingHelper.DoTrace( 4, "WidgetController.SaveWidget. No widget data but found files" );
                //    isFileUploaded = this.UploadStyle( Request.Files[ 0 ], widgetData );

                //    widgetService.Save( widgetData, ref messages );

                //    fileUploadStatus = isFileUploaded ? "Style template upload success" : fileUploadStatus;
                //    if ( !string.IsNullOrWhiteSpace( fileUploadStatus ) )
                //    {
                //        messages.Add( fileUploadStatus );
                //    }
                //} else
                {
                    messages.Add( "Error - no widget data was sent to the server." );
                    return JsonResponse( widgetData, false, "failure", messages );
                }
            }

            LoggingHelper.DoTrace( 6, "WidgetController.SaveWidget. Regular exit." );
            return JsonResponse( widgetData, true, "success", new { isFileUploaded = isFileUploaded, fileUploadStatus = fileUploadStatus } );
        }


        [HttpPost]
        public ActionResult DeleteWidget( int id )
        {
            WidgetServices widgetService = new WidgetServices();
            List<string> messages = new List<string>();
            string message = string.Empty;
            if ( !widgetService.Delete( id, ref message ) )
            {
                messages.Add( message );
                return JsonResponse( new object(), false, "", messages );
            }

            return JsonResponse( new object(), true, "success", null );
        }

        [HttpGet]
        public JsonResult GetWidget( int widgetId )
        {
            Widget widget = WidgetServices.Get( widgetId );
            if ( !string.IsNullOrEmpty( widget.CustomStyles ) )
            {
                //this should be done in factory manager
                //widget.WidgetStyles = JsonConvert.DeserializeObject<WidgetStyles>( widget.CustomStyles );
            }
            else
            {
                widget.WidgetStyles = new WidgetStyles();
            }
            List<string> messages = new List<string>();
            if ( widget == null )
            {
                messages.Add( "Widget not found" );
                return JsonResponse( new Widget(), false, "", messages );
            }
            return JsonResponse( widget, true, "success", null );
        }

        [HttpGet]
        public JsonResult GetUserOrganizations()
        {
            var user = AccountServices.GetUserFromSession();
            List<string> messages = new List<string>();
            if ( user == null )
            {
                messages.Add( "User not found" );
                return JsonResponse( null, false, "", messages );
            }
            var organizations = user.Organizations;
            if ( organizations == null || organizations.Count <= 0 )
            {
                messages.Add( "User not found" );
                return JsonResponse( null, false, "", messages );
            }

            return JsonResponse( organizations, true, "success", null );
        }


		[HttpGet]
        public JsonResult GetOrganizationWidgets( string orgId )
        {
            var widgets = WidgetServices.GetWidgetsForOrganization( orgId );
            List<string> messages = new List<string>();
            if ( widgets == null || widgets.Count == 0 )
            {
                messages.Add( "Widgets not found" );
                return JsonResponse( null, false, "", messages );
            }

            return JsonResponse( widgets, true, "success", null );
        }


        //Take widget parameters via GET parameters
        //TODO - remove widget from session when user visits the homepage
        public ActionResult Apply()
        {
            //Apply widget to session


            //Redirect to start page
            return Redirect( "~/search" );
        }
        //

        //
        public JsonResult OrgSearch( MainSearchInput query )
        {
            query.IncludingReferenceObjects = true;
            MainSearchResults results = searchServices.MainSearch( query, ref valid, ref status );

            return JsonHelper.GetJsonWithWrapper( results, valid, status, null );
        }
        //Do a MicroSearch
        public JsonResult DoMicroSearch( MicroSearchInputV2 query )
        {
            var totalResults = 0;
            List<MicroProfile> data = MicroSearchServicesV2.DoMicroSearch( query, ref totalResults, ref valid, ref status );
            return JsonHelper.GetJsonWithWrapper( data, valid, status, new { TotalResults = totalResults } );
        }
        //

        public JsonResult GetOrganizations( List<int> organizationIDs )
        {
            //Get the data
            var organizations = new List<MicroProfile>();
            organizations = OrganizationServices.GetMicroProfile( organizationIDs );
            return JsonHelper.GetJsonWithWrapper( organizations, true, "", null );
        }
        //

        /// <summary>
        /// Return list as CodeItems - only Region is available, no Id of any sort
        /// </summary>
        /// <param name="country"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult GetRegionsForCountry( string country )
        {
            List<CodeItem> regionList = ( new EnumerationServices() ).GetExistingRegionsForCountry( country );

            List<string> messages = new List<string>();
            if ( regionList == null || regionList.Count == 0 )
            {
                messages.Add( "Regions not found" );
                return JsonResponse( null, false, "", messages );
            }

            return JsonResponse( regionList, true, "success", null );
        }
        /// <summary>
        /// Return list as CodeItems - only Region is available, no Id of any sort
        /// </summary>
        /// <param name="country"></param>
        /// <param name="region"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult GetExistingCitiesForRegion( string country, string region )
        {
            List<CodeItem> regionList = ( new EnumerationServices() ).GetExistingCitiesForRegion( country, region );

            List<string> messages = new List<string>();
            if ( regionList == null || regionList.Count == 0 )
            {
                messages.Add( "No cities were found" );
                return JsonResponse( null, false, "", messages );
            }

            return JsonResponse( regionList, true, "success", null );
        }



        private bool UploadStyle( HttpPostedFileBase file, Widget widget )
        {
            try
            {
                if ( file != null && file.ContentLength > 0 && Path.GetExtension( file.FileName ).ToLowerInvariant() == ".css" )
                {
                    int widgetId = 0;
                    if ( widget != null )
                    {
                        widgetId = widget.Id;
                    }
                    var styleUploadPath = ConfigurationManager.AppSettings[ "styleUploadPath" ] + "widget" + widgetId
                        + Path.GetExtension( file.FileName );

                    file.SaveAs( styleUploadPath );
                    widget.CustomStylesFileName = Path.GetFileName( styleUploadPath );

                    return true;
                }

                return false;
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex.Message, "Upload file style css failed" );
                return false;
            }
        }

        public bool ParseAndCreateUserStyleTemplate( Widget widget, ControllerContext controller, string tempFilePath, string template, ref List<string> messages )
        {
            LoggingHelper.DoTrace( 6, "WidgetController.ParseAndCreateUserStyleTemplate." );
            try
            {
                var sb = new StringWriter();
                ViewDataDictionary viewData = new ViewDataDictionary();
                Dictionary<string, string> pathMapDictionary = new Dictionary<string, string>();

                pathMapDictionary.Add( tempFilePath, template ); // Add template to dictionary which virtual path provider will access

                var styleUploadPath = ConfigurationManager.AppSettings[ "styleUploadPath" ] + "widget" + widget.Id + ".css";

                var tempData = new TempDataDictionary();
                viewData.Model = widget.WidgetStyles;
                var razor = new RazorView( controller, "~/UserStyleTemplate.cshtml", null, false, null );
                var viewContext = new ViewContext( controller, razor, viewData, tempData, sb );
                razor.Render( viewContext, sb );
                System.IO.File.WriteAllText( styleUploadPath, sb.ToString() );
                widget.CustomStylesFileName = Path.GetFileName( styleUploadPath );
                return true;
            }
            catch ( Exception ex )
            {
                messages.Add( ex.Message );
                LoggingHelper.LogError( ex, "WidgetController.ParseAndCreateUserStyleTemplate" );
                return false;
            }
        }


		#region Widget V2 Methods

		//Used for testing the new widgetized search page
		public ActionResult SearchWidget( int widgetID = 0 )
		{
			var widget = WidgetServices.Get( widgetID ) ?? new Widget();
			var vm = JsonConvert.DeserializeObject<WidgetV2>( widget.CustomStyles ?? "{}", new JsonSerializerSettings() { Error = IgnoreDeserializationErrors } );
			return View( "~/views/widget/searchwidget.cshtml", vm );
		}
		//

		//Used for testing the new configure page
		public ActionResult ConfigureV2()
		{
			return View( "~/views/widget/configurev2.cshtml" );
		}
		//

		//Get all of the widgets for an organization
		public JsonResult GetWidgetsForOrganization( string organizationCTID )
		{
			//Get data
			var widgets = WidgetServices.GetWidgetsForOrganization( organizationCTID );
			//Extract
			var results = new List<WidgetV2>();
			foreach( var widget in widgets )
			{
				var result = new WidgetV2();
				try
				{
					//Extract the JSON data if it is V2 data
					result = JsonConvert.DeserializeObject<WidgetV2>( widget.CustomStyles, new JsonSerializerSettings() { Error = IgnoreDeserializationErrors } );
					if( result.Id == 0 )
					{
						throw new Exception();
					}
				}
				catch
				{
					//Try to convert old data to new format
					result = V1toV2( widget );
				}

				results.Add( result );
			}

			return JsonResponse( results, true, "okay", null );
		}
		//

		//Search for organizations related to some entity type
		public JsonResult OrganizationConnectionSearch( ConnectionQuery query )
		{
			/* Temporary */
			//TODO - Determine whether or not this needs to be replaced with a method that cares about the Relationship type (perhaps limit results to QA orgs?)
			return OrgSearch( new MainSearchInput() { SearchType = "organization", StartPage = query.PageNumber, PageSize = query.PageSize, Keywords = query.Keywords } );

			/* End Temporary */
		}
		public class ConnectionQuery
		{
			public enum RelationshipTypes { OfferedBy, AccreditedBy, ApprovedBy, RegulatedBy }
			public string Keywords { get; set; }
			public string RelatedTo { get; set; }
			public RelationshipTypes Relationship { get; set; }
			public int PageSize { get; set; }
			public int PageNumber { get; set; }
		}
		//

		public JsonResult GetOrganizationDataForSelectedMicrosearchItems( List<int> organizationIDs )
		{
			var rawResults = new List<OrganizationSummary>();
			var results = new List<MainSearchResult>();
			foreach( var id in organizationIDs )
			{
				rawResults.Add( SimpleMap<OrganizationSummary>( OrganizationServices.GetBasic( id ) ) );
			}
			results = new SearchServices().ConvertOrganizationResults( rawResults, 0, "organization" ).Results;
			return JsonResponse( results, true, "", null );
		}
		//

		public JsonResult SaveWidgetV2( WidgetV2 data )
		{
			//Convert
			var toSave = V2toV1( data );

			//Save the data
			var result = new WidgetV2();
			var messages = new List<string>();
			if ( !string.IsNullOrWhiteSpace( toSave.WidgetAlias ) )
			{
				var widgetExisting = WidgetServices.GetByAlias( toSave.WidgetAlias );
				if ( widgetExisting != null && widgetExisting.Id > 0 && widgetExisting.Id != toSave.Id )
				{
					messages.Add( "Widget Alias already exist" );
					return JsonResponse( null, false, "error", messages );
				}
			}

			if ( !new WidgetServices().Save( toSave, ref messages ) )
			{
				LoggingHelper.DoTrace( 5, "WidgetController.SaveWidgetV2. Errors on save: " + string.Join( "\n\r>", messages ) );
				return JsonResponse( null, false, "error", messages );
			}

			//Copy key properties from newly-saved widget (such as ID) to the V2 widget
			var saved = WidgetServices.Get( toSave.Id );
			result = JsonConvert.DeserializeObject<WidgetV2>( saved.CustomStyles );
			SimpleUpdate( saved, result );
			result.UrlName = saved.WidgetAlias;
			
			//Save again to ensure the V2 data has the correct key properties
			saved.CustomStyles = JsonConvert.SerializeObject( result );
			new WidgetServices().Save( saved, ref messages );

			return JsonResponse( result, true, "okay", null );
		}
		//

		public JsonResult PreviewWithoutSaving( WidgetV2 data )
		{
			Session.Remove( "PreviewSearchWidget" );
			Session.Add( "PreviewSearchWidget", JsonConvert.SerializeObject( data ) );
			return JsonResponse( null, true, "okay", null );
		}
		//

		public ActionResult PreviewSearchWidget()
		{
			var vm = JsonConvert.DeserializeObject<WidgetV2>( ( string ) Session[ "PreviewSearchWidget" ] ?? "{}" );
			return View( "~/views/widget/searchwidget.cshtml", vm );
		}
		//

		private Widget V2toV1( WidgetV2 v2 )
		{
			var v1 = SimpleMap<Widget>( v2 );
			v1.WidgetAlias = v2.UrlName;
			v1.OrgCTID = v2.OrganizationCTID;
			v1.OrganizationName = v2.OrganizationName;
			v1.CountryFilters = string.Join( ",", v2.Locations.Countries ?? new List<string>() );
			v1.RegionFilters = string.Join( ",", v2.Locations.Regions ?? new List<string>() );
			v1.CityFilters = string.Join( ",", v2.Locations.Cities ?? new List<string>() );
			v1.OwningOrganizationIds = string.Join( ",", v2.CredentialFilters.OwnedBy );
			v1.IncludeIfAvailableOnline = v2.Locations.IsAvailableOnline;
			v1.WidgetStylesUrl = v2.CustomCssUrl;
			v1.SearchFilters = v2.CustomJSON;

			//Important, and one-way
			v1.CustomStyles = JsonConvert.SerializeObject( v2 );

			return v1;
		}

		private WidgetV2 V1toV2( Widget v1 )
		{
			var v2 = SimpleMap<WidgetV2>( v1 );
			/*  //These changes may no longer be desired
			v2.UrlName = v1.WidgetAlias;
			v2.OrganizationCTID = v1.OrgCTID;
			v2.Locations.Countries = SplitString( v1.CountryFilters );
			v2.Locations.Regions = SplitString( v1.RegionFilters );
			v2.Locations.Cities = SplitString( v1.CityFilters );
			v2.Locations.IsAvailableOnline = v1.IncludeIfAvailableOnline;
			v2.CredentialFilters.OwnedBy = SplitString( v1.OwningOrganizationIds ).ConvertAll( m => new WidgetV2.Organization() { Id = int.Parse( m ) } ).ToList();
			v2.CustomCssUrl = v1.WidgetStylesUrl;
			v2.CustomJSON = v1.SearchFilters;
			*/

			//Do not map CustomStyles to the widget here

			return v2;
		}

		private List<string> SplitString(string text )
		{
			return (text ?? "").Split( new string[] { "," }, StringSplitOptions.RemoveEmptyEntries ).ToList().ConvertAll( m => m.Trim() ).Where( m => m.Length > 0 ).ToList();
		}


		private T SimpleMap<T>( object input ) where T : new()
		{
			var result = new T();
			SimpleUpdate( input, result );
			return result;
		}
		//

		private void SimpleUpdate( object input, object output )
		{
			var obj1Properties = input.GetType().GetProperties();
			var obj2Properties = output.GetType().GetProperties();
			foreach ( var property in obj2Properties )
			{
				try
				{
					var match = obj1Properties.FirstOrDefault( m => m.Name == property.Name );
					if ( match != null )
					{
						property.SetValue( output, match.GetValue( input ) );
					}
				}
				catch { }
			}
		}
		private void IgnoreDeserializationErrors( object sender, Newtonsoft.Json.Serialization.ErrorEventArgs e )
		{
			e.ErrorContext.Handled = true;
		}

		#endregion
	}
}
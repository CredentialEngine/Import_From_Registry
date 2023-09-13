using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http;

using CredentialFinderWebAPI.Models;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using workIT.Models.Common;
using workIT.Services;
using workIT.Utilities;

using API = workIT.Services.API;
using WMA = workIT.Models.API;
using MPM = workIT.Models.ProfileModels;
using workIT.Factories;

using workIT.Models;
using PB = workIT.Models.PathwayBuilder;

namespace CredentialFinderWebAPI.Controllers
{
	//[EnableCors( origins: "http://mywebclient.azurewebsites.net", headers: "*", methods: "*" )]
	public class DetailController : BaseController
	{
		static string thisClassName = "DetailController";
        #region Generic Handling
        private DetailResult<T> GetDetail<T>( string label, string idOrCTID, Func<int, bool, T> getDetailByIDForAPI, Func<string, bool, T> getDetailByCTIDForAPI ) where T : WMA.BaseAPIType, new()
		{
			var entity = new T();
			var messages = new List<string>();
			var skippingCache = FormHelper.GetRequestKeyValue( "skipCache", false );
			var widgetMsg = GetWidgetMessage();

			try
			{
				var recordID = 0;
				if( int.TryParse( idOrCTID, out recordID ) )
				{
					entity = getDetailByIDForAPI( recordID, skippingCache );
				}
				else if( ServiceHelper.IsValidCtid( idOrCTID, ref messages ) )
				{
					entity = getDetailByCTIDForAPI( idOrCTID, skippingCache );
				}
				else
				{
					messages.Add( "ERROR - Invalid request. Either provide a CTID which starts with 'ce-' or the number. " );
				}

				if ( entity.Meta_Id == 0 )
				{
					messages.Add( string.Format( "ERROR - Invalid request - the {0} was not found. ", label ) );
				}

				if ( messages.Any() )
				{
					return new DetailResult<T>( null, false, messages );
				}
				else
				{
					ActivityServices.SiteActivityAdd( label, "View", "Detail", string.Format( "User viewed {0}: {1} ({2}). {3}", label, entity.Name, entity.Meta_Id, widgetMsg ), 0, 0, entity.Meta_Id ?? 0 );
					return new DetailResult<T>( entity, true, messages );
				}
			}
			catch( Exception ex )
			{
				LoggingHelper.LogError( ex, "API - " + label + " Detail exception. Id: " + idOrCTID );
				messages.Add( string.Format( "Error encountered returning data. {0} ", ex.Message ) );
				return new DetailResult<T>( null, false, messages );
			}
		}
		public class DetailResult<T>
		{
			public DetailResult( T data, bool successful, List<string> messages ) 
			{
				Data = data;
				Successful = successful;
				Messages = messages;
			}
			public T Data { get; set; }
			public bool Successful { get; set; }
			public List<string> Messages { get; set; }

			public ApiResponse ToApiResponse()
			{
				return new ApiResponse( Data, Successful, Messages );
			}
		}
		//

		public void CredentialSimplified( string id )
		{
			var detail = GetDetail( "Credential", id, API.CredentialServices.GetDetailForAPI, API.CredentialServices.GetDetailByCtidForAPI );
			SendResponse( detail.ToApiResponse() );
		}

		public void OrganizationSimplified( string id )
		{
			var timeStart = DateTime.Now;
			bool includingAll = FormHelper.GetRequestKeyValue( "includeAllData", false );
			var detail = GetDetail( "Organization", id, delegate( int recordID, bool skip ) { return API.OrganizationServices.GetDetailForAPI( recordID, skip, includingAll ); }, API.OrganizationServices.GetDetailByCtidForAPI );
			if ( detail.Successful )
			{
				var elapsed = DateTime.Now.Subtract( timeStart );
				LoggingHelper.DoTrace( 6, string.Format( "         Organization: '{1}' detail Duration: {0:N2} seconds", elapsed.TotalSeconds, detail.Data.Name ) );
			}

			SendResponse( detail.ToApiResponse() );
		}

		public void AssessmentSimplified( string id )
		{
			var detail = GetDetail( "AssessmentProfile", id, API.AssessmentServices.GetDetailForAPI, API.AssessmentServices.GetDetailByCtidForAPI );

			SendResponse( detail.ToApiResponse() );
		}

		public void LearningOpportunitySimplified( string id )
		{
			int widgetId = FormHelper.GetRequestKeyValue( "widgetId", 0 );

			var detail = GetDetail( "LearningOpportunity", id, API.LearningOpportunityServices.GetDetailForAPI, API.LearningOpportunityServices.GetDetailByCtidForAPI );
			SendResponse( detail.ToApiResponse() );
		}

		public void PathwaySimplified( string id )
		{
			var detail = GetDetail( "Pathway", id, API.PathwayServices.GetDetailForAPI, API.PathwayServices.GetDetailByCtidForAPI );
			SendResponse( detail.ToApiResponse() );
		}

		public void TransferValueSimplified( string id )
		{
			var detail = GetDetail( "TransferValue", id, API.TransferValueServices.GetDetailForAPI, API.TransferValueServices.GetDetailByCtidForAPI );
			SendResponse( detail.ToApiResponse() );
		}

		public void CompetencyFrameworkSimplified( string id )
		{
			var detail = GetDetail( "CompetencyFramework", id, API.CompetencyFrameworkServices.GetDetailForAPI, API.CompetencyFrameworkServices.GetDetailByCtidForAPI );
			SendResponse( detail.ToApiResponse() );
		}
		//public void Competency( string id )
		//{
		//	var detail = GetDetail( "Competency", id, API.CompetencyFrameworkServices.GetCompetencyDetailForAPI, API.CompetencyFrameworkServices.GetCompetencyDetailByCtidForAPI );
		//	SendResponse( detail.ToApiResponse() );
		//}
		//
		[HttpGet, Route( "Collection/{id}" )]
		public void Collection( string id )
		{
			var detail = GetDetail( "Collection", id, API.CollectionServices.GetDetailForAPI, API.CollectionServices.GetDetailByCtidForAPI );
			SendResponse( detail.ToApiResponse() );
		}
		#endregion

		[HttpGet, Route( "Credential/{id}" )]
		public void Credential( string id )
		{
			int recordId = 0;
			var label = "Credential";
			var response = new ApiResponse();
			var entity = new WMA.CredentialDetail();
			bool skippingCache = FormHelper.GetRequestKeyValue( "skipCache", false );
			int widgetId = FormHelper.GetRequestKeyValue( "widgetId", 0 );
			var widgetMsg = GetWidgetMessage();

			List<string> messages = new List<string>();
			try
			{
				if ( int.TryParse( id, out recordId ) )
				{
					entity = API.CredentialServices.GetDetailForAPI( recordId, skippingCache );
				}
				else if ( ServiceHelper.IsValidCtid( id, ref messages ) )
				{
					entity = API.CredentialServices.GetDetailByCtidForAPI( id, skippingCache );
				}
				else
				{
					messages.Add( "ERROR - Invalid request. Either provide a CTID which starts with 'ce-' or the number. " );
				}
				//HttpContext.Server.ScriptTimeout = 300;

				if ( entity.Meta_Id == 0 )
				{
					messages.Add( string.Format( "ERROR - Invalid request - the {0} was not found. ", label ) );
				}

				if ( messages.Any() )
				{
					//anyway??
					response.Result = entity;
					response.Successful = false;
					response.Messages.AddRange( messages );
					SendResponse( response );
				}
				else
				{
					ActivityServices.SiteActivityAdd( "Credential", "View", "Detail", $"User viewed: {entity.Name} ({entity.Meta_Id}).{widgetMsg} ", 0, 0, recordId );

					//string jsonoutput = JsonConvert.SerializeObject( entity, JsonHelper.GetJsonSettings() );
					//SendResponse( jsonoutput );
					response.Successful = true;
					response.Result = entity;
					SendResponse( response );
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "API-CredentialDetail. Id" + id );
				response.Messages.Add( string.Format( "Error encountered returning data. {0} ", ex.Message ) );
				response.Successful = false;
				SendResponse( response );

			}
		}


        #region Organization 
        [HttpGet, Route( "CredentialOrganization/{id}" )]
        public void CredentialOrganization( string id )
        {
			Organization( id );
        }

        [HttpGet, Route( "Organization/{id}" )]
		public void Organization( string id )
		{
			int recordId = 0;
			var label = "Organization";
			var response = new ApiResponse();
			DateTime overall = DateTime.Now;

			var entity = new WMA.OrganizationDetail();
			bool skippingCache = FormHelper.GetRequestKeyValue( "skipCache", false );
			bool includingAll = FormHelper.GetRequestKeyValue( "includeAllData", false );
            var widgetMsg = GetWidgetMessage();

            List<string> messages = new List<string>();
			try
			{
				if ( int.TryParse( id, out recordId ) )
				{
					entity = API.OrganizationServices.GetDetailForAPI( recordId, skippingCache, includingAll );
				}
				else if ( ServiceHelper.IsValidCtid( id, ref messages ) )
				{
					entity = API.OrganizationServices.GetDetailByCtidForAPI( id, skippingCache );
				}
				else
				{
					messages.Add( "ERROR - Invalid request. Either provide a CTID which starts with 'ce-' or the integer identifier of an existing organization. " );
				}
				//HttpContext.Server.ScriptTimeout = 300;

				if ( entity.Meta_Id == 0 && !messages.Any() )
				{
					messages.Add( string.Format( "ERROR - Invalid request - the {0} was not found. ", label ) );
				}
				if ( messages.Any() )
				{
					response.Successful = false;
					response.Messages.AddRange( messages );
					SendResponse( response );
				}
				else
				{
					var saveDuration = DateTime.Now.Subtract( overall );
					//if ( saveDuration.TotalSeconds > 1 )
					LoggingHelper.DoTrace( 6, string.Format( "         Organization: '{1}' detail Duration: {0:N2} seconds", saveDuration.TotalSeconds, entity.Name ) );
					//map to new 
					ActivityServices.SiteActivityAdd( label, "View", "Detail", string.Format( "User viewed {0}: {1} ({2}). {3}", label, entity.Name, entity.Meta_Id, widgetMsg ), 0, 0, recordId );

					response.Successful = true;
					response.Result = entity;
					SendResponse( response );
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "API-OrganizationDetail. Id" + id );
				response.Messages.Add( string.Format( "Error encountered returning data. {0} ", ex.Message ) );
				response.Successful = false;
				SendResponse( response );

			}
		}

        [HttpGet, Route( "OrganizationOld/{id}" )]
		public void OrganizationOld( string id )
		{
			int recordId = 0;
			var entity = new Organization();
			bool skippingCache = FormHelper.GetRequestKeyValue( "skipCache", false );
			List<string> messages = new List<string>();
			if ( int.TryParse( id, out recordId ) )
			{
				entity = OrganizationServices.GetDetail( recordId, skippingCache );
			}
			else if ( ServiceHelper.IsValidCtid( id, ref messages ) )
			{
				entity = OrganizationServices.GetDetailByCtid( id, skippingCache );
			}
			else
			{
				messages.Add( "ERROR - Invalid request. Either provide a CTID which starts with 'ce-' or the number. " );
			}
			//HttpContext.Server.ScriptTimeout = 300;

			if ( entity.Id == 0 )
			{
				messages.Add( "ERROR - Invalid request - the record was not found. " );
			}
			if ( messages.Any() )
			{
				SendResponse( messages );
				//HttpContext.Current.Response.Clear();
				//HttpContext.Current.Response.AppendHeader( "Access-Control-Allow-Origin", "*" );
				//HttpContext.Current.Response.ContentType = "application/json";
				//HttpContext.Current.Response.Write( messages );
				//HttpContext.Current.Response.End();
			}
			else
			{
				//map to new 
				ActivityServices.SiteActivityAdd( "Organization", "View", "Detail", string.Format( "User viewed Organization: {0} ({1})", entity.Name, entity.Id ), 0, 0, recordId );

				string jsonoutput = JsonConvert.SerializeObject( entity, JsonHelper.GetJsonSettings() );

				SendResponse( jsonoutput );
				//HttpContext.Current.Response.Clear();
				//HttpContext.Current.Response.AppendHeader( "Access-Control-Allow-Origin", "*" );
				//HttpContext.Current.Response.ContentType = "application/json";
				//HttpContext.Current.Response.Write( jsonoutput );
				//HttpContext.Current.Response.End();
			}
		}

		[HttpGet, Route( "Organization2/{id}" )]
		public string Organization2( string id )
		{
			int recordId = 0;
			var entity = new Organization();
			bool skippingCache = FormHelper.GetRequestKeyValue( "skipCache", false );
			List<string> messages = new List<string>();
			if ( int.TryParse( id, out recordId ) )
			{
				entity = OrganizationServices.GetDetail( recordId, skippingCache );
			}
			else if ( ServiceHelper.IsValidCtid( id, ref messages ) )
			{
				entity = OrganizationServices.GetDetailByCtid( id, skippingCache );
			}
			else
			{
				return "ERROR - Invalid request. Either provide a CTID which starts with 'ce-' or the number. ";
			}
			//HttpContext.Server.ScriptTimeout = 300;

			if ( entity.Id == 0 )
			{
				return "ERROR - Invalid request - the record was not found. ";

			}
			ActivityServices.SiteActivityAdd( "Organization", "View", "Detail", string.Format( "User viewed Organization: {0} ({1})", entity.Name, entity.Id ), 0, 0, recordId );

			string jsonoutput = JsonConvert.SerializeObject( entity, JsonHelper.GetJsonSettings() );
			HttpContext.Current.Response.ContentType = "application/json";
			HttpContext.Current.Response.ContentEncoding = Encoding.UTF8;
			return jsonoutput;
		}

		#endregion

		#region Assessment

		[HttpGet, Route( "AssessmentProfile/{id}" )]
		public void AssessmentProfile( string id )
		{
			Assessment( id );
		}
		[HttpGet, Route( "Assessment/{id}" )]
		public void Assessment( string id )
		{
			int recordId = 0;
			var label = "AssessmentProfile";
			var response = new ApiResponse();

			var entity = new WMA.AssessmentDetail();
			bool skippingCache = FormHelper.GetRequestKeyValue( "skipCache", false );
            var widgetMsg = GetWidgetMessage();

            List<string> messages = new List<string>();
			try
			{
				if ( int.TryParse( id, out recordId ) )
				{
					entity = API.AssessmentServices.GetDetailForAPI( recordId, skippingCache );
				}
				else if ( ServiceHelper.IsValidCtid( id, ref messages ) )
				{
					entity = API.AssessmentServices.GetDetailByCtidForAPI( id, skippingCache );
				}
				else
				{
					messages.Add( "ERROR - Invalid request. Either provide a CTID which starts with 'ce-' or the number. " );
				}
				//HttpContext.Server.ScriptTimeout = 300;

				if ( entity.Meta_Id == 0 )
				{
					messages.Add( string.Format( "ERROR - Invalid request - the {0} was not found. ", label ) );
				}
				if ( messages.Any() )
				{
					response.Successful = false;
					response.Messages.AddRange( messages );
					SendResponse( response );
				}
				else
				{
					ActivityServices.SiteActivityAdd( label, "View", "Detail", string.Format( "User viewed {0}: {1} ({2}). {3}", label, entity.Name, entity.Meta_Id, widgetMsg ), 0, 0, recordId );

					response.Successful = true;
					response.Result = entity;
					SendResponse( response );
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "API {0} Detail exception. Id: {1}", label, id ) );
				response.Successful = false;
				response.Messages.Add( ex.Message );
				SendResponse( response );

			}
		}

        #endregion

        #region Lopp
        [HttpGet, Route( "TrainingProgram/{id}" )]
        public void TrainingProgram( string id )
        {
            var detail = GetDetail( "TrainingProgram", id, API.LearningOpportunityServices.GetDetailForAPI, API.LearningOpportunityServices.GetDetailByCtidForAPI );
            SendResponse( detail.ToApiResponse() );

            //OR
            //HandleLearningOpportunity( id, "TrainingProgram" );
        }
        [HttpGet, Route( "Course/{id}" )]
        public void Course( string id )
        {
            var detail = GetDetail( "Course", id, API.LearningOpportunityServices.GetDetailForAPI, API.LearningOpportunityServices.GetDetailByCtidForAPI );
            SendResponse( detail.ToApiResponse() );
        }
        private string GetWidgetMessage()
        {
            int widgetId = FormHelper.GetRequestKeyValue( "widgetId", 0 );
			if ( widgetId == 0 )
				widgetId = FormHelper.GetRequestKeyValue( "widgetid", 0 );

			var widgetMsg = "";
            if (widgetId > 0)
                widgetMsg = $" Current WidgetId:{widgetId}";
			return widgetMsg;
        }
        [HttpGet, Route( "LearningOpportunity/{id}" )]
        public void LearningOpportunity( string id )
        {
			HandleLearningOpportunity( id );
        }
		private void HandleLearningOpportunity( string id, string label = "LearningOpportunity" )
		{
			int recordId = 0;
			//var label = "LearningOpportunity";
			var response = new ApiResponse();

			var entity = new WMA.LearningOpportunityDetail();
			bool skippingCache = FormHelper.GetRequestKeyValue( "skipCache", false );
            var widgetMsg = GetWidgetMessage();

            WMA.DetailRequest request = new WMA.DetailRequest()
			{
				SkippingCache = skippingCache,
				IsAPIRequest = true
			};
			List<string> messages = new List<string>();
			try
			{
				if ( int.TryParse( id, out recordId ) )
				{
					request.Id = recordId;
					entity = API.LearningOpportunityServices.GetDetailForAPI( request );
				}
				else if ( ServiceHelper.IsValidCtid( id, ref messages ) )
				{
					entity = API.LearningOpportunityServices.GetDetailByCtidForAPI( id, skippingCache );
				}
				else
				{
					messages.Add( "ERROR - Invalid request. Either provide a CTID which starts with 'ce-' or the number. " );
				}
				//HttpContext.Server.ScriptTimeout = 300;

				if ( entity.Meta_Id == 0 )
				{
					messages.Add( string.Format( "ERROR - Invalid request - the {0} was not found. ", label ) );
				}
				if ( messages.Any() )
				{
					response.Successful = false;
					response.Messages.AddRange( messages );
					SendResponse( response );
				}
				else
				{
					//map to new 
					ActivityServices.SiteActivityAdd( label, "View", "Detail", string.Format( "User viewed {0}: {1} ({2}). {3}", label, entity.Name, entity.Meta_Id, widgetMsg ), 0, 0, recordId );

					response.Successful = true;
					response.Result = entity;
					SendResponse( response );
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "API {0} Detail exception. Id: {1}", label, id ) );
				response.Successful = false;
				response.Messages.Add( ex.Message );
				SendResponse( response );

			}
		}
		#endregion

		#region Pathway
		[HttpGet, Route( "Pathway/{id}" )]
		public void Pathway( string id )
		{
			int recordId = 0;
			var label = "Pathway";
			var response = new ApiResponse();

			var entity = new WMA.Pathway();
			bool skippingCache = FormHelper.GetRequestKeyValue( "skipCache", false );
			List<string> messages = new List<string>();
			try
			{
				if ( int.TryParse( id, out recordId ) )
				{
					entity = API.PathwayServices.GetDetailForAPI( recordId, skippingCache );
				}
				else if ( ServiceHelper.IsValidCtid( id, ref messages ) )
				{
					entity = API.PathwayServices.GetDetailByCtidForAPI( id, skippingCache );
				}
				else
				{
					messages.Add( "ERROR - Invalid request. Either provide a CTID which starts with 'ce-' or the number. " );
				}

				if ( entity.Meta_Id == 0 )
				{
					messages.Add( string.Format( "ERROR - Invalid request - the {0} was not found. ", label ) );
				}
				if ( messages.Any() )
				{
					response.Successful = false;
					response.Messages.AddRange( messages );
					SendResponse( response );
				}
				else
				{
					//map to new 
					ActivityServices.SiteActivityAdd( label, "View", "Detail", string.Format( "User viewed {0}: {1} ({2})", label, entity.Name, entity.Meta_Id ), 0, 0, recordId );

                    response.Successful = true;
					response.Result = entity;
					SendResponse( response );
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "API {0} Detail exception. Id: {1}", label, id ) );
				response.Successful = false;
				response.Messages.Add( ex.Message );
				SendResponse( response );

			}
		}

        [HttpGet, Route( "PathwayComponent/{id}" )]
        public void PathwayComponent( string id )
        {
            //var label = "Pathway Component";
            //var response = new ApiResponse();

            //WMA.PathwayComponent detail = GetDetail( label, id, API.PathwayServices.PathwayComponentGetDetailForAPI, API.PathwayServices.PathwayComponentGetDetailByCtidForAPI );

            //SendResponse( detail.ToApiResponse() );


            int recordId = 0;
            var label = "Pathway Component";
            var response = new ApiResponse();

            var entity = new WMA.PathwayComponent();
            List<string> messages = new List<string>();
            try
            {
                if (int.TryParse( id, out recordId ))
                {
                    entity = API.PathwayServices.PathwayComponentGetDetailForAPI( recordId );
                }
                else if (ServiceHelper.IsValidCtid( id, ref messages ))
                {
                    entity = API.PathwayServices.PathwayComponentGetDetailByCtidForAPI( id );
                }
                else
                {
                    messages.Add( "ERROR - Invalid request. Either provide a CTID which starts with 'ce-' or the number. " );
                }

                if (entity.Meta_Id == 0)
                {
                    messages.Add( string.Format( "ERROR - Invalid request - the {0} was not found. ", label ) );
                }
                if (messages.Any())
                {
                    response.Successful = false;
                    response.Messages.AddRange( messages );
                    SendResponse( response );
                }
                else
                {
                    //map to new 
                    ActivityServices.SiteActivityAdd( label, "View", "Detail", string.Format( "User viewed {0}: {1} ({2})", label, entity.Name, entity.Meta_Id ), 0, 0, recordId );

                    response.Successful = true;
                    response.Result = entity;
                    SendResponse( response );
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError( ex, string.Format( "API {0} Detail exception. Id: {1}", label, id ) );
                response.Successful = false;
                response.Messages.Add( ex.Message );
                SendResponse( response );

            }
        }
        [HttpGet, Route( "PathwaySet/{id}" )]
		public void PathwaySet( string id )
		{
			int recordId = 0;
			var label = "PathwaySet";
			var response = new ApiResponse();

			var entity = new WMA.PathwaySet();
			bool skippingCache = FormHelper.GetRequestKeyValue( "skipCache", false );
			List<string> messages = new List<string>();
			try
			{
				if ( int.TryParse( id, out recordId ) )
				{
					entity = API.PathwaySetServices.GetDetailForAPI( recordId, skippingCache );
				}
				else if ( ServiceHelper.IsValidCtid( id, ref messages ) )
				{
					entity = API.PathwaySetServices.GetDetailByCtidForAPI( id, skippingCache );
				}
				else
				{
					messages.Add( "ERROR - Invalid request. Either provide a CTID which starts with 'ce-' or the number. " );
				}

				if ( entity.Meta_Id == 0 )
				{
					messages.Add( string.Format( "ERROR - Invalid request - the {0} was not found. ", label ) );
				}
				if ( messages.Any() )
				{
					response.Successful = false;
					response.Messages.AddRange( messages );
					SendResponse( response );
				}
				else
				{
					//map to new 
					ActivityServices.SiteActivityAdd( label, "View", "Detail", string.Format( "User viewed {0}: {1} ({2})", label, entity.Name, entity.Meta_Id ), 0, 0, recordId );

					response.Successful = true;
					response.Result = entity;
					SendResponse( response );
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "API {0} Detail exception. Id: {1}", label, id ) );
				response.Successful = false;
				response.Messages.Add( ex.Message );
				SendResponse( response );

			}
		}
        #endregion

        #region PathwayBuilder/Display


        //[AcceptVerbs( "OPTIONS", "GET" ), Route( "PathwayDisplay/{id}" )]
        [HttpGet, Route( "PathwayDisplay/{id}" )]
        public void PathwayWrapperGet( string id )
        {
            int recordId = 0;
            var label = "Pathway";
            var resource = new PB.PathwayWrapper();
            var response = new PathwayApiResponse();
            AppUser user = AccountServices.GetCurrentUser();
            List<string> messages = new List<string>();
			LoggingHelper.DoTrace( BaseFactory.appMethodEntryTraceLevel, $"PathwayDisplay: {id} started." );
            try
            {
                bool skippingCache = FormHelper.GetRequestKeyValue( "skipCache", false );

                if ( int.TryParse( id, out recordId ) )
                {
                    resource = PathwayServices.PathwayGraphGet( recordId );
                }
                else if ( ServiceHelper.IsValidCtid( id, ref messages ) )
                {
                    resource = PathwayServices.PathwayGraphGetByCtid( id );
                }
                else
                {
                    messages.Add( "ERROR - Invalid request. Either provide a CTID which starts with 'ce-' or the number. " );
                    response.Valid = false;
                    response.Messages.AddRange( messages );
                    SendResponse( response );
                }
                //HttpContext.Server.ScriptTimeout = 300;

                if ( resource == null || resource.Pathway == null || resource.Pathway.Id == 0 )
                {
                    messages.Add( string.Format( "ERROR - Invalid request - the {0} was not found. ", label ) );
                }

                else
                {
                    //map to the wrapper
                    //this should be done a services method
                    recordId = resource.Pathway.Id;

                    try
                    {
                        //really only need to do once per day. OR not, as can just use the finderAPI URL to see current contents
                        var pathwayLabel = resource.Pathway.FriendlyName + string.Format( " (ID:{0})", resource.Pathway.Id);
                        string datePrefix = System.DateTime.Now.ToString( "yyyy-dd" );
                        string payload = JsonConvert.SerializeObject( resource, JsonHelper.GetJsonSettings() );
                        var fileLabel = string.Format( "{0}PathwayWrapperGet.json", pathwayLabel.Replace( " ", "." ).Replace( ":", "" ) );
                        LoggingHelper.WriteLogFile( 7, label, payload, datePrefix, false );
                    }
                    catch ( Exception ex )
                    {
                        //ignore?
                        LoggingHelper.LogError( ex, thisClassName + ".PathwayWrapperGet", "Error saving payload", false );
                    }
                }

                response.Valid = true;
                response.Data = resource;
                SendResponse( response );
                //return JsonApiResponse( resource, !messages.Any(), messages, null, recordId );
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, thisClassName + ".Pathway.Id: " + id );
                messages.Add( string.Format( "Error encountered returning data. {0} ", ex.Message ) );
                response.Valid = false;
                response.Messages.Add( ex.Message );
                SendResponse( response );
            }
        }

        [HttpGet, Route( "PathwayDisplay/Schema/PathwayComponent" )]
        public void SchemaPathwayComponent()
        {
            var results = PathwayServices.GetPathwayComponentConcepts();
            var response = new PathwayApiResponse
            {
                Valid = true,
                Data = results
            };
            SendResponse( response );
        }
        //
        [HttpGet, Route( "PathwayDisplay/Schema/CredentialType" )]
        public void CredentialType()
        {
			
            //ReturnDataOrError( () => { return PathwayServices.GetCredentialTypeConcepts(); }, thisClassName + ".SchemaCredentialType" );
			//var results = PathwayServices.GetCredentialTypeConcepts();
   //         string postBody = JsonConvert.SerializeObject( results, JsonHelper.GetJsonSettings(false ) );
   //         return new PathwayApiResponse( postBody, true, null );

            var results = PathwayServices.GetCredentialTypeConcepts();
            var response = new PathwayApiResponse
            {
                Valid = true,
                Data = results
            };
            SendResponse( response );
        }
        [HttpGet, Route( "PathwayDisplay/Schema/LogicalOperator" )]
        public void LogicalOperator()
        {
            var results = PathwayServices.GetLogicalOperatorConcepts();
            var response = new PathwayApiResponse
            {
                Valid = true,
                Data = results
            };

            SendResponse( response );
        }
        [HttpGet, Route( "PathwayDisplay/Schema/Comparator" )]
		public void Comparator()
		{
			var results = PathwayServices.GetComparatorConcepts();
			var response = new PathwayApiResponse
			{
				Valid = true,
				Data = results
			};

            SendResponse( response );
        }
        //
        [HttpGet, Route( "PathwayDisplay/Schema/ArrayOperation" )]
        public void ArrayOperation()
        {
            var results = PathwayServices.GetArrayOperationConcepts();
            var response = new PathwayApiResponse
            {
                Valid = true,
                Data = results
            };

            SendResponse( response );
        }
        //
        [HttpGet, Route( "PathwayDisplay/Schema/CreditUnitType" )]
        public void CreditUnitType()
        {
            var results = PathwayServices.GetCreditUnitTypeConcepts();
            var response = new PathwayApiResponse
            {
                Valid = true,
                Data = results
            };

            SendResponse( response );
        }
        //
        [HttpGet, Route( "PathwayDisplay/Schema/CreditLevelType" )]
        public void CreditLevelType()
        {
            var results = PathwayServices.GetCreditLevelTypeConcepts();
            var response = new PathwayApiResponse
            {
                Valid = true,
                Data = results
            };

            SendResponse( response );
        }
        #endregion
        #region Transfer Value
        [HttpGet, Route( "TransferValueProfile/{id}" )]
		public void TransferValueProfile( string id )
		{
			TransferValue( id );
		}
		[HttpGet, Route( "TransferValue/{id}" )]
		public void TransferValue( string id )
		{
			int recordId = 0;
			var label = "TransferValue";
			var response = new ApiResponse();

			var entity = new WMA.TransferValueProfile();
			bool skippingCache = FormHelper.GetRequestKeyValue( "skipCache", false );
			List<string> messages = new List<string>();
			try
			{
				if ( int.TryParse( id, out recordId ) )
				{
					entity = API.TransferValueServices.GetDetailForAPI( recordId, skippingCache );
				}
				else if ( ServiceHelper.IsValidCtid( id, ref messages ) )
				{
					entity = API.TransferValueServices.GetDetailByCtidForAPI( id, skippingCache );
				}
				else
				{
					messages.Add( "ERROR - Invalid request. Either provide a CTID which starts with 'ce-' or the number. " );
				}

				if ( entity.Meta_Id == 0 )
				{
					messages.Add( string.Format( "ERROR - Invalid request - the {0} was not found. ", label ) );
				}
				if ( messages.Any() )
				{
					response.Successful = false;
					response.Messages.AddRange( messages );
					SendResponse( response );
				}
				else
				{
					//map to new 
					ActivityServices.SiteActivityAdd( label, "View", "Detail", string.Format( "User viewed {0}: {1} ({2})", label, entity.Name, entity.Meta_Id ), 0, 0, recordId );

					response.Successful = true;
					response.Result = entity;
					SendResponse( response );
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "API {0} Detail exception. Id: {1}", label, id ) );
				response.Successful = false;
				response.Messages.Add( ex.Message );
				SendResponse( response );

			}
		}

		/// <summary>
		/// TI Get
		/// </summary>
		/// <param name="id"></param>
		[HttpGet, Route( "TransferIntermediary/{id}" )]
		public void TransferIntermediary( string id )
		{
			int recordId = 0;
			var label = "TransferIntermediary";
			var response = new ApiResponse();

			var entity = new WMA.TransferIntermediary();
			bool skippingCache = FormHelper.GetRequestKeyValue( "skipCache", false );
			List<string> messages = new List<string>();
			try
			{
				if ( int.TryParse( id, out recordId ) )
				{
					entity = API.TransferIntermediaryServices.GetDetailForAPI( recordId, skippingCache );
				}
				else if ( ServiceHelper.IsValidCtid( id, ref messages ) )
				{
					entity = API.TransferIntermediaryServices.GetDetailByCtidForAPI( id, skippingCache );
				}
				else
				{
					messages.Add( "ERROR - Invalid request. Either provide a CTID which starts with 'ce-' or the number. " );
				}

				if ( entity.Meta_Id == 0 )
				{
					messages.Add( string.Format( "ERROR - Invalid request - the {0} was not found. ", label ) );
				}
				if ( messages.Any() )
				{
					response.Successful = false;
					response.Messages.AddRange( messages );
					SendResponse( response );
				}
				else
				{
					//map to new 
					ActivityServices.SiteActivityAdd( label, "View", "Detail", string.Format( "User viewed {0}: {1} ({2})", label, entity.Name, entity.Meta_Id ), 0, 0, recordId );

					response.Successful = true;
					response.Result = entity;
					SendResponse( response );
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "API {0} Detail exception. Id: {1}", label, id ) );
				response.Successful = false;
				response.Messages.Add( ex.Message );
				SendResponse( response );

			}
		}
        #endregion



        [HttpGet, Route( "ConceptScheme/{id}" )]
        public void ConceptScheme( string id )
        {
            var detail = GetDetail( "ConceptScheme", id, API.ConceptSchemeServices.GetConceptSchemeOnlyByID, API.ConceptSchemeServices.GetConceptSchemeOnlyByCTID );
            SendResponse( detail.ToApiResponse() );
        }

        [HttpGet, Route( "ProgressionModel/{id}" )]
        public void ProgressionModel( string id )
        {
            var detail = GetDetail( "ProgressionModel", id, API.ProgressionModelServices.GetProgressionModelOnlyByID, API.ProgressionModelServices.GetProgressionModelOnlyByCTID );
            SendResponse( detail.ToApiResponse() );
        }

        [HttpGet, Route( "OutcomeData/{id}" )]
        public void OutcomeData( string id )
        {
            var detail = GetDetail( "OutcomeData", id, API.OutcomeDataServices.GetDetailForAPI, API.OutcomeDataServices.GetDetailByCtidForAPI );
            SendResponse( detail.ToApiResponse() );
        }
        [HttpGet, Route( "DataSetProfile/{id}" )]
        public void DataSetProfile( string id )
        {
            var detail = GetDetail( "DataSetProfile", id, API.OutcomeDataServices.GetDetailForAPI, API.OutcomeDataServices.GetDetailByCtidForAPI );
            SendResponse( detail.ToApiResponse() );
        }

        [HttpGet, Route( "Occupation/{id}" )]
        public void Occupation( string id )
        {
            var detail = GetDetail( "Occupation", id, API.OccupationServices.GetDetailForAPI, API.OccupationServices.GetDetailByCtidForAPI );
            SendResponse( detail.ToApiResponse() );
        }
		/*	*/
        [HttpGet, Route( "Job/{id}" )]
        public void Job( string id )
        {
            var detail = GetDetail( "Job", id, API.JobServices.GetDetailForAPI, API.JobServices.GetDetailByCtidForAPI );
            SendResponse( detail.ToApiResponse() );
        }


        [HttpGet, Route( "Task/{id}" )]
        public void Task( string id )
        {
            var detail = GetDetail( "Task", id, API.TaskServices.GetDetailForAPI, API.TaskServices.GetDetailByCtidForAPI );
            SendResponse( detail.ToApiResponse() );
        }


        [HttpGet, Route( "WorkRole/{id}" )]
        public void WorkRole( string id )
        {
            var detail = GetDetail( "WorkRole", id, API.WorkRoleServices.GetDetailForAPI, API.WorkRoleServices.GetDetailByCtidForAPI );
            SendResponse( detail.ToApiResponse() );
        }
	
        [HttpGet, Route( "ScheduledOffering/{id}" )]
        public void ScheduledOffering( string id )
        {
            var detail = GetDetail( "ScheduledOffering", id, API.ScheduledOfferingServices.GetDetailForAPI, API.ScheduledOfferingServices.GetDetailByCtidForAPI );
            SendResponse( detail.ToApiResponse() );
        }


        [HttpGet, Route( "SupportService/{id}" )]
        public void SupportService( string id )
        {
            var detail = GetDetail( "SupportService", id, API.SupportServiceServices.GetDetailForAPI, API.SupportServiceServices.GetDetailByCtidForAPI );
            SendResponse( detail.ToApiResponse() );
        }
        /// <summary>
        /// Resolve a CTID and return an Outline
        /// </summary>
        /// <param name="id"></param>
        [HttpGet, Route( "Resources/{id}" )]
		public void Resources( string id )
		{
			
			var response = new ApiResponse();
			var label = "Resources";
			List<string> messages = new List<string>();
			var output = new WMA.BaseAPIType();
			//options-
			var outline = new WMA.Outline();
			try
			{
				if ( !ServiceHelper.IsValidCtid( id, ref messages ) )
				{
					messages.Add( string.Format( "ERROR - Invalid CTID: '{0}'. Please enter a valid CTID.", id ) );
				}
				else
				{
					var entity = SearchServices.GetEntityByCTID( id );
					if(entity != null && entity.Id > 0)
					{
						//quick fix
						if ( entity.EntityTypeId == 3 )
							entity.EntityType = "Assessment";
						else if ( entity.EntityTypeId == 26 )
							entity.EntityType = "TransferValue";
						else if ( entity.EntityTypeId == 7 )
							entity.EntityType = "LearningOpportunity";
						//really only need enough data to redirect
						//probably should provide the URL
						output = new WMA.BaseAPIType()
						{
							Meta_StateId = entity.EntityStateId,
							Meta_Id = entity.EntityBaseId,
							Name = entity.EntityBaseName,
							BroadType = entity.EntityType,
							//
							//CTDLType = entity.EntityType,
							CTID = entity.CTID
						};

						//or use Outline - less data
						outline = new WMA.Outline()
						{
							Label = entity.EntityBaseName,
							OutlineType	 = entity.EntityType,
							//return relative for now
							URL = "" + entity.EntityType.Replace( " ", "" ) + "/" + entity.EntityBaseId.ToString(),
							Meta_Id = entity.EntityBaseId,
						};
					} else
					{
						messages.Add( "Error a resource was not found using the provided CTID." );
					}

				}
				if ( messages.Any() )
				{
					response.Successful = false;
					response.Messages.AddRange( messages );
					SendResponse( response );
					//return response;
				}
				else
				{
					response.Successful = true;
					var jo =JObject.FromObject( outline, new JsonSerializer() { NullValueHandling = NullValueHandling.Ignore } );
					response.Result = jo;
					SendResponse( response );
					//return response;
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "API {0} Detail exception. Id: {1}", label, id ) );
				response.Successful = false;
				response.Messages.Add( ex.Message );

				SendResponse( response );
				//return response;

			}
		}
		#region Manifests Profiles
		/// <summary>
		/// Get all condition manifests for an organization
		/// TBD: consider getting all on first call? Depends on performance
		/// </summary>
		/// <param name="organizationId">Guid for the parent entity</param>
		[HttpGet, Route( "detail/ConditionManifest/{organizationId}" )]
		public void ConditionManifest( int organizationId )
		{
			var response = new ApiResponse();

			var output = new List<WMA.ConditionManifest>();
			List<string> messages = new List<string>();
			try
			{
				if ( organizationId > 0 )
				{
					output = API.OrganizationServices.GetConditionManifests( organizationId );
				}
				else
				{
					messages.Add( "ERROR - Invalid request. Please provide a valid organization identifier." );
				}

				if ( messages.Any() )
				{
					response.Successful = false;
					response.Messages.AddRange( messages );
					SendResponse( response );
					//return response;

				}
				else
				{
					response.Successful = true;
					response.Result = output;
					SendResponse( response );
					//return response;
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "API-Detail.ConditionManifest(organizationId:{0}.", organizationId ), false );
				response.Messages.Add( string.Format( "Error encountered returning data. {0} ", ex.Message ) );
				response.Successful = false;
				SendResponse( response );
				//return response;
			}
		}
		/// <summary>
		/// Get all cost manifests for an organization
		/// TBD: consider getting all on first call? Depends on performance
		/// </summary>
		/// <param name="organizationId">Id for the parent entity</param>
		[HttpGet, Route( "detail/CostManifest/{organizationId}" )]
		public void CostManifest( int organizationId )
		{
			var response = new ApiResponse();

			var output = new List<WMA.CostManifest>();
			List<string> messages = new List<string>();
			try
			{
				if ( organizationId > 0 )
				{
					output = API.OrganizationServices.GetCostManifests( organizationId );
				}
				else
				{
					messages.Add( "ERROR - Invalid request. Please provide a valid organization identifier." );
				}

				if ( messages.Any() )
				{
					response.Successful = false;
					response.Messages.AddRange( messages );
					SendResponse( response );
					//return response;

				}
				else
				{
					response.Successful = true;
					response.Result = output;
					SendResponse( response );
					//return response;
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "API-Detail.CostManifest(organizationId:{0}.", organizationId ), false );
				response.Messages.Add( string.Format( "Error encountered returning data. {0} ", ex.Message ) );
				response.Successful = false;
				SendResponse( response );
				//return response;
			}
		}
		#endregion

		#region Process Profiles
		/// <summary>
		/// Get all process profiles of a type
		/// TBD: consider getting all on first call? Depends on performance
		/// </summary>
		/// <param name="id">Guid for the parent entity</param>
		/// <param name="processTypeId">Identifier (1-8) for the type of process profiles to retrieve.</param>
		[HttpGet, Route( "detail/ProcessProfile/{id}/{processTypeId}" )]
		public void ProcessProfile( string id, int processTypeId )
		{
			var response = new ApiResponse();
			var output = new List<WMA.ProcessProfile>();
			List<string> messages = new List<string>();
			LoggingHelper.DoTrace( 5, string.Format( "API.DetailController called with: id:{0}, processTypeId:{1})", id, processTypeId) );
			try
			{
				if ( processTypeId < 1 || processTypeId > 8 )
				{
					processTypeId = 1;
				}
				if ( ServiceHelper.IsValidGuid( id ) )
				{
					var guid = new Guid( id );
					output = API.ProfileServices.HandleProcessProfiles( guid, processTypeId );
				}
				else
				{
					messages.Add( "ERROR - Invalid request. Please provide the Guid for the target entity (organization, etc.) and the process profile type id (1-8)" );
				}

				if ( messages.Any() )
				{
					response.Successful = false;
					response.Messages.AddRange( messages );
					SendResponse( response );
					//return response;

				}
				else
				{
					response.Successful = true;
					response.Result = output;
					SendResponse( response );
					//return response;
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "API-Detail.ProcessProfile(Guid:{0}, processTypeId: {1}).", id, processTypeId ), false );
				response.Messages.Add( string.Format( "Error encountered returning data. {0} ", ex.Message ) );
				response.Successful = false;
				SendResponse( response );
				//return response;
			}
		}
		#endregion

		#region Verification Service Profiles
		/// <summary>
		/// Get all VerificationServiceProfile for an organization
		/// </summary>
		/// <param name="guid"></param>
		[HttpGet, Route( "detail/VerificationService/{guid}" )]
		public void VerificationServices( Guid guid )
		{
			var response = new ApiResponse();

			var output = new List<WMA.VerificationServiceProfile>();
			List<string> messages = new List<string>();
			try
			{
				
				if ( ServiceHelper.IsValidGuid( guid ) )
				{
					output = API.OrganizationServices.GetVerificationServiceProfiles( guid );
				}
				else
				{
					messages.Add( "ERROR - Invalid request. Please provide the Guid for the target organization. " );
				}

				if ( messages.Any() )
				{
					response.Successful = false;
					response.Messages.AddRange( messages );
					SendResponse( response );
					//return response;
				}
				else
				{
					response.Successful = true;
					response.Result = output;
					SendResponse( response );
					//return response;
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "API-Detail.ProcessProfile(Guid:{0}).", guid ), false );
				response.Messages.Add( string.Format( "Error encountered returning data. {0} ", ex.Message ) );
				response.Successful = false;
				SendResponse( response );
				//return response;
			}
		}
		#endregion

		#region Competency Data for Something Else's Detail Page

		[HttpGet, Route( "detail/competencyalignmentset/credential/{id}" )]
		public void GetCredentialCompetencyAlignmentSet( int id, bool includeDebug = false )
		{
			var request = new CredentialRequest() { IncludingConnectionProfiles = true };
			var conditionProfiles = CredentialServices.GetDetail( id, request, false ).Requires.ToList();
			var competencyData = API.ServiceHelper.GetAllCompetencies( conditionProfiles, true );
			var result = new WMA.CompetencyAlignmentSet();
			result.Debug = includeDebug ? new List<JObject>() : null;

			result.RequiresCompetencies = competencyData.RequiresByFramework.Select( m => API.CompetencyFrameworkServices.ConvertCAOFrameworkProfileToFinderAPICompetencyFrameworkWithOnlyReferencedCompetenciesAndTheirParents( m, result.Debug ) ).ToList();
			result.AssessesCompetencies = competencyData.AssessesByFramework.Select( m => API.CompetencyFrameworkServices.ConvertCAOFrameworkProfileToFinderAPICompetencyFrameworkWithOnlyReferencedCompetenciesAndTheirParents( m, result.Debug ) ).ToList();
			result.TeachesCompetencies = competencyData.TeachesByFramework.Select( m => API.CompetencyFrameworkServices.ConvertCAOFrameworkProfileToFinderAPICompetencyFrameworkWithOnlyReferencedCompetenciesAndTheirParents( m, result.Debug ) ).ToList();
			
			SendResponse( new ApiResponse( result, true, null ) );
		}
		//

		[HttpGet, Route( "detail/competencyalignmentset/assessment/{id}" )]
		public void GetAssessmentCompetencyAlignmentSet( int id, bool includeDebug = false )
		{
			var rawDetail = AssessmentServices.GetDetail( id, false );
			var result = new WMA.CompetencyAlignmentSet();
			result.Debug = includeDebug ? new List<JObject>() : null;

			result.AssessesCompetencies = rawDetail.AssessesCompetenciesFrameworks.Select( m => API.CompetencyFrameworkServices.ConvertCAOFrameworkProfileToFinderAPICompetencyFrameworkWithOnlyReferencedCompetenciesAndTheirParents( m, result.Debug ) ).ToList();

			SendResponse( new ApiResponse( result, true, null ) );
		}
		//

		[HttpGet, Route( "detail/competencyalignmentset/learningopportunity/{id}" )]
		public void GetLearningOpportunityCompetencyAlignmentSet( int id, bool includeDebug = false )
		{
			var rawDetail = LearningOpportunityServices.GetDetail( id, false );
			var result = new WMA.CompetencyAlignmentSet();
			result.Debug = includeDebug ? new List<JObject>() : null;

			result.TeachesCompetencies = rawDetail.TeachesCompetenciesFrameworks.Select( m => API.CompetencyFrameworkServices.ConvertCAOFrameworkProfileToFinderAPICompetencyFrameworkWithOnlyReferencedCompetenciesAndTheirParents( m, result.Debug ) ).ToList();

			SendResponse( new ApiResponse( result, true, null ) );
		}
		//


		#endregion

		#region Registry Description Set Detail Page Loaders
		private T GetByIDorCTID<T>( string id, Func<int, T> GetByID, Func<string, T> GetByCTID )
		{
			try
			{
				return GetByID( int.Parse( id ) );
			}
			catch { }

			try
			{
				return GetByCTID( id );
			}
			catch { }

			return default(T);
		}

		[HttpGet, Route("Temporary/Graph/ConceptScheme/{id}")] //Temporary workaround for SNHU until the registry supports community description sets
		public void TemporaryConceptSchemeGraph( string id )
		{
			//Get the graph data for the default registry
			var data = GetByIDorCTID( id, ConceptSchemeServices.Get, ConceptSchemeServices.GetByCtid );
			if ( data == null || data.Id == 0 || string.IsNullOrWhiteSpace( data.CTID ) )
			{
				SendResponse( new ApiResponse( null, false, new List<string>() { "Unable to find Registry data for Concept Scheme: " + id } ) );
				return;
			}

			var result = RegistryServicesV2.TemporaryDoNotUseMe_GetRegistryGraph( data.CTID );
			SendResponse( new ApiResponse( result.RawData, result.Successful, null ) );
		}
		//

		[HttpGet, Route( "Temporary/Graph/CompetencyFramework/{id}" )] //Temporary workaround for SNHU until the registry supports community description sets
		public void TemporaryCompetencyFrameworkGraph( string id )
		{
			//Get the graph data for the default registry
			var data = GetByIDorCTID( id, CompetencyFrameworkServices.Get, CompetencyFrameworkServices.GetCompetencyFrameworkByCtid );
			if ( data == null || data.Id == 0 || string.IsNullOrWhiteSpace( data.CTID ) )
			{
				SendResponse( new ApiResponse( null, false, new List<string>() { "Unable to find Registry data for Competency Framework: " + id } ) );
				return;
			}

			var result = RegistryServicesV2.TemporaryDoNotUseMe_GetRegistryGraph( data.CTID );
			SendResponse( new ApiResponse( result.RawData, result.Successful, null ) );
		}
		//

		[HttpGet, Route( "Temporary/Graph/Pathway/{id}" )] //Temporary workaround for SNHU until the registry supports community description sets
		public void TemporaryPathwayGraph( string id )
		{
			//Get the graph data for the default registry
			var data = GetByIDorCTID( id, PathwayServices.GetBasic, PathwayServices.GetByCtid );
			if ( data == null || data.Id == 0 || string.IsNullOrWhiteSpace( data.CTID ) )
			{
				SendResponse( new ApiResponse( null, false, new List<string>() { "Unable to find Registry data for Pathway: " + id } ) );
				return;
			}

			var result = RegistryServicesV2.TemporaryDoNotUseMe_GetRegistryGraph( data.CTID );
			SendResponse( new ApiResponse( result.RawData, result.Successful, null ) );
		}
		//

		[HttpGet, Route( "DSP_ConceptScheme/{id}" )]
		public void DSP_ConceptScheme( string id, bool includeRelatedResources = false, int perBranchLimit = 10, bool includeGraphData = true, bool includeMetadata = true )
		{
			GetDetailPageDescriptionSetFromCTIDorIDString( id, ConceptSchemeServices.GetCTIDFromID, includeRelatedResources, perBranchLimit, includeGraphData, includeMetadata );
		}
        //
        [HttpGet, Route( "DSP_ProgressionModel/{id}" )]
        public void DSP_ProgressionModel( string id, bool includeRelatedResources = false, int perBranchLimit = 10, bool includeGraphData = true, bool includeMetadata = true )
        {
            GetDetailPageDescriptionSetFromCTIDorIDString( id, ProgressionModelServices.GetCTIDFromID, includeRelatedResources, perBranchLimit, includeGraphData, includeMetadata );
        }
        //
        [HttpGet, Route( "DSP_CompetencyFramework/{id}" )]
		public void DSP_CompetencyFramework( string id, bool includeRelatedResources = false, int perBranchLimit = 10, bool includeGraphData = true, bool includeMetadata = true )
		{
			GetDetailPageDescriptionSetFromCTIDorIDString( id, CompetencyFrameworkServices.GetCTIDFromID, includeRelatedResources, perBranchLimit, includeGraphData, includeMetadata );
		}
		//

		[HttpGet, Route( "DSP_Pathway/{id}" )]
		public void DSP_Pathway( string id, bool includeRelatedResources = false, int perBranchLimit = 10, bool includeGraphData = true, bool includeMetadata = true )
		{
			GetDetailPageDescriptionSetFromCTIDorIDString( id, PathwayServices.GetCTIDFromID, includeRelatedResources, perBranchLimit, includeGraphData, includeMetadata );
		}
		//

		[HttpGet, Route( "DSP_Collection/{id}" )]
		public void DSP_Collection( string id, bool includeRelatedResources = false, int perBranchLimit = 10, bool includeGraphData = false, bool includeMetadata = true )
		{
			GetDetailPageDescriptionSetFromCTIDorIDString( id, CollectionServices.GetCTIDFromID, includeRelatedResources, perBranchLimit, includeGraphData, includeMetadata );
		}
		//

		[HttpGet, Route("DSP_Competency/{id}")]
		public void DSP_Competency( string id, bool includeRelatedResources = false, int perBranchLimit = 10, bool includeGraphData = false, bool includeMetadata = true )
		{
			GetDetailPageDescriptionSetFromCTIDorIDString( id, parsedID => 
				CompetencyFrameworkServices.GetCompetencyCTIDFromCompetencyID( parsedID ) ?? 
				CollectionServices.GetCompetencyCTIDFromCompetencyID( parsedID ), 
			includeRelatedResources, perBranchLimit, includeGraphData, includeMetadata );
		}
		//

		[HttpGet, Route("Detail/DescriptionSetByCTID/{ctid}")]
		public void DescriptionSetByCTID( string ctid, bool includeRelatedResources = false, int perBranchLimit = 10, bool includeGraphData = false, bool includeMetadata = false )
		{
			var result = RegistryServicesV2.GetDetailPageDescriptionSet( ctid, includeRelatedResources, perBranchLimit, includeGraphData, includeMetadata );
			SendResponse( new ApiResponse( result, true, null ) );
		}
		//

		private void GetDetailPageDescriptionSetFromCTIDorIDString( string id, Func<int, string> GetCTIDFromIntegerIDMethod, bool includeRelatedResources, int perBranchLimit, bool includeGraphData, bool includeMetadata )
		{
			var ctid = "";
			if( id.IndexOf("ce-") == 0 )
			{
				ctid = id;
			}
			else
			{
				try
				{
					ctid = GetCTIDFromIntegerIDMethod( int.Parse( id ) );
					if ( string.IsNullOrWhiteSpace( ctid ) )
					{
						throw new Exception();
					}
				}
				catch 
				{
					SendResponse( new ApiResponse( null, false, new List<string>() { "Unable to find CTID for ID: " + id } ) );
				}
			}

			var result = RegistryServicesV2.GetDetailPageDescriptionSet( ctid, includeRelatedResources, perBranchLimit, includeGraphData, includeMetadata );
			SendResponse( new ApiResponse( result, true, null ) );
		}
		//

		#endregion

		[HttpPost, Route("Detail/RegistrySearch")]
		public void RegistrySearch( RegistryServicesV2.RegistryQuery query )
		{
			query.Take = query.Take > 50 ? 50 : query.Take;
			if( query.Query != null )
			{
				var result = RegistryServicesV2.MakeDirectRegistryRequest( "ctdl?skip=" + query.Skip + "&take=" + query.Take, true, true, query.Query.ToString( Formatting.None ) );
				if ( result.Successful )
				{
					SendResponse( new ApiResponse( result.RawData, true ) );
				}
				else
				{
					SendResponse( new ApiResponse( result.DebugInfo, false ) );
				}
			}
		}
        //

        //

    }
}

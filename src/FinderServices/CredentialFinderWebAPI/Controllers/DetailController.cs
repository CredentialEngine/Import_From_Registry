using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http;

using CredentialFinderWebAPI.Models;

using Newtonsoft.Json;

using workIT.Models.Common;
using workIT.Services;
using workIT.Utilities;

using API = workIT.Services.API;
using MD = workIT.Models.API;

namespace CredentialFinderWebAPI.Controllers
{
	//[EnableCors( origins: "http://mywebclient.azurewebsites.net", headers: "*", methods: "*" )]
	public class DetailController : BaseController
	{
		[HttpGet, Route( "Credential/{id}" )]
		public void Credential( string id )
		{
			int recordId = 0;
			var label = "Credential";
			var response = new ApiResponse();
			var entity = new MD.CredentialDetail();
			bool skippingCache = FormHelper.GetRequestKeyValue( "skipCache", false );
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
					response.Successful = false;
					response.Messages.AddRange( messages );
					SendResponse( response );
				}
				else
				{
					ActivityServices.SiteActivityAdd( "Credential", "View", "Detail", string.Format( "User viewed Credential: {0} ({1})", entity.Name, entity.Meta_Id ), 0, 0, recordId );

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
		/*
		[HttpGet, Route( "CredentialOld/{id}" )]
		public void CredentialOld( string id )
		{
			int recordId = 0;
			var entity = new Credential();
			bool skippingCache = FormHelper.GetRequestKeyValue( "skipCache", false );
			List<string> messages = new List<string>();
			if ( int.TryParse( id, out recordId ) )
			{
				entity = CredentialServices.GetDetail( recordId, skippingCache );
			}
			else if ( ServiceHelper.IsValidCtid( id, ref messages ) )
			{
				entity = CredentialServices.GetDetailByCtid( id, skippingCache );
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
			}
			else
			{
				ActivityServices.SiteActivityAdd( "Credential", "View", "Detail", string.Format( "User viewed Credential: {0} ({1})", entity.Name, entity.Id ), 0, 0, recordId );

				string jsonoutput = JsonConvert.SerializeObject( entity, JsonHelper.GetJsonSettings() );
				SendResponse( jsonoutput );
			}
		}
		*/

		#region organization 
		[HttpGet, Route( "Organization/{id}" )]
		public void Organization( string id )
		{
			int recordId = 0;
			var label = "Organization";
			var response = new ApiResponse();
			DateTime overall = DateTime.Now;

			var entity = new MD.OrganizationDetail();
			bool skippingCache = FormHelper.GetRequestKeyValue( "skipCache", false );
			bool includingAll = FormHelper.GetRequestKeyValue( "includeAllData", false );
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
					ActivityServices.SiteActivityAdd( label, "View", "Detail", string.Format( "User viewed {0}: {1} ({2})", label, entity.Name, entity.Meta_Id ), 0, 0, recordId );

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

			var entity = new MD.AssessmentDetail();
			bool skippingCache = FormHelper.GetRequestKeyValue( "skipCache", false );
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

		#region Lopp
		[HttpGet, Route( "LearningOpportunity/{id}" )]
		public void LearningOpportunity( string id )
		{
			int recordId = 0;
			var label = "LearningOpportunity";
			var response = new ApiResponse();

			var entity = new MD.LearningOpportunityDetail();
			bool skippingCache = FormHelper.GetRequestKeyValue( "skipCache", false );
			List<string> messages = new List<string>();
			try
			{
				if ( int.TryParse( id, out recordId ) )
				{
					entity = API.LearningOpportunityServices.GetDetailForAPI( recordId, skippingCache );
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

		#region Pathway
		[HttpGet, Route( "Pathway/{id}" )]
		public void Pathway( string id )
		{
			int recordId = 0;
			var label = "Pathway";
			var response = new ApiResponse();

			var entity = new MD.Pathway();
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

			var entity = new MD.TransferValueProfile();
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
		#endregion


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

			var output = new List<MD.ConditionManifest>();
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

			var output = new List<MD.CostManifest>();
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
		/// <param name="guid">Guid for the parent entity</param>
		/// <param name="processTypeId">Identifier (1-8) for the type of process profiles to retrieve.</param>
		[HttpGet, Route( "detail/ProcessProfile/{id}/{processTypeId}" )]
		public void ProcessProfile( string id, int processTypeId )
		{
			var response = new ApiResponse();

			var output = new List<MD.ProcessProfile>();
			List<string> messages = new List<string>();
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


		#region VerificationServices Profiles
		/// <summary>
		/// Get all VerificationServiceProfile for an organization
		/// </summary>
		/// <param name="guid"></param>
		[HttpGet, Route( "detail/VerificationService/{guid}" )]
		public void VerificationServices( Guid guid )
		{
			var response = new ApiResponse();

			var output = new List<MD.VerificationServiceProfile>();
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


		//

	}
}

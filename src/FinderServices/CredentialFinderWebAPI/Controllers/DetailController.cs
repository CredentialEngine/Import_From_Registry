using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
//using System.Web.Http.Cors;

using CredentialFinderWebAPI.Models;
using workIT.Models;
using workIT.Models.Common;
using MD=workIT.Models.Detail;
using workIT.Models.ProfileModels;
using workIT.Services;
using workIT.Utilities;
using Newtonsoft.Json;
using System.Web;
using Newtonsoft.Json.Linq;

namespace CredentialFinderWebAPI.Controllers
{
	//[EnableCors( origins: "http://mywebclient.azurewebsites.net", headers: "*", methods: "*" )]
	public class DetailController : BaseController
	{
		[HttpGet, Route( "Credential/{id}" )]
		public void Credential( string id )
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
				messages.Add( "ERROR - Invalid request. Either provided a CTID which starts with 'ce-' or the number. " );
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
		[HttpGet, Route( "Organization/{id}" )]
		public void Organization( string id )
		{
			int recordId = 0;
			var label = "Organization";
			var response = new ApiResponse();

			var entity = new MD.OrganizationDetail();
			bool skippingCache = FormHelper.GetRequestKeyValue( "skipCache", false );
			List<string> messages = new List<string>();
			if ( int.TryParse( id, out recordId ) )
			{
				entity = OrganizationServices.GetDetailForAPI( recordId, skippingCache );
			}
			else if ( ServiceHelper.IsValidCtid( id, ref messages ) )
			{
				entity = OrganizationServices.GetDetailByCtidForApi( id, skippingCache );
			}
			else
			{
				messages.Add( "ERROR - Invalid request. Either provide a CTID which starts with 'ce-' or the integer identifier of an existing organization. " );
			}
			//HttpContext.Server.ScriptTimeout = 300;

			if ( entity.Id == 0 && !messages.Any() )
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
				ActivityServices.SiteActivityAdd( label, "View", "Detail", string.Format( "User viewed {0}: {1} ({2})", label, entity.Name, entity.Id ), 0, 0, recordId );

				//string jsonoutput = JsonConvert.SerializeObject( entity, JsonHelper.GetJsonSettings() );
				//SendResponse( jsonoutput );
				//OR
				response.Successful = true;
				response.Result = entity;
				SendResponse( response );
				//var finalResult = JObject.FromObject( new { data = jsonoutput, valid = true, status = status } );

				//SendResponse( jsonoutput );
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
				messages.Add( "ERROR - Invalid request. Either provided a CTID which starts with 'ce-' or the number. ");
			}
			//HttpContext.Server.ScriptTimeout = 300;

			if ( entity.Id == 0 )
			{
				messages.Add( "ERROR - Invalid request - the record was not found. ");
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
				return "ERROR - Invalid request. Either provided a CTID which starts with 'ce-' or the number. ";
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

		[HttpGet, Route( "Assessment/{id}" )]
		public void Assessment( string id )
		{
			int recordId = 0;
			var label = "AssessmentProfile";
			var entity = new AssessmentProfile();
			bool skippingCache = FormHelper.GetRequestKeyValue( "skipCache", false );
			List<string> messages = new List<string>();
			if ( int.TryParse( id, out recordId ) )
			{
				entity = AssessmentServices.GetDetail( recordId, skippingCache );
			}
			else if ( ServiceHelper.IsValidCtid( id, ref messages ) )
			{
				entity = AssessmentServices.GetDetailByCtid( id, skippingCache );
			}
			else
			{
				messages.Add( "ERROR - Invalid request. Either provided a CTID which starts with 'ce-' or the number. " );
			}
			//HttpContext.Server.ScriptTimeout = 300;

			if ( entity.Id == 0 )
			{
				messages.Add( string.Format("ERROR - Invalid request - the {0} was not found. ",label) );
			}
			if ( messages.Any() )
			{
				HttpContext.Current.Response.Clear();
				HttpContext.Current.Response.ContentType = "application/json";
				HttpContext.Current.Response.Write( messages );
				HttpContext.Current.Response.End();
			}
			else
			{
				ActivityServices.SiteActivityAdd( label, "View", "Detail", string.Format( "User viewed {0}: {1} ({2})", label, entity.Name, entity.Id ), 0, 0, recordId );

				string jsonoutput = JsonConvert.SerializeObject( entity, JsonHelper.GetJsonSettings() );

				HttpContext.Current.Response.Clear();
				HttpContext.Current.Response.ContentType = "application/json";
				HttpContext.Current.Response.Write( jsonoutput );
				HttpContext.Current.Response.End();
			}
		}

		[HttpGet, Route( "LearningOpportunity/{id}" )]
		public void LearningOpportunity( string id )
		{
			int recordId = 0;
			var label = "LearningOpportunity";

			var entity = new LearningOpportunityProfile();
			bool skippingCache = FormHelper.GetRequestKeyValue( "skipCache", false );
			List<string> messages = new List<string>();
			if ( int.TryParse( id, out recordId ) )
			{
				entity = LearningOpportunityServices.GetDetail( recordId, skippingCache );
			}
			else if ( ServiceHelper.IsValidCtid( id, ref messages ) )
			{
				entity = LearningOpportunityServices.GetDetailByCtid( id, skippingCache );
			}
			else
			{
				messages.Add( "ERROR - Invalid request. Either provided a CTID which starts with 'ce-' or the number. " );
			}
			//HttpContext.Server.ScriptTimeout = 300;

			if ( entity.Id == 0 )
			{
				messages.Add( string.Format( "ERROR - Invalid request - the {0} was not found. ", label ) );
			}
			if ( messages.Any() )
			{
				SendResponse( messages );
			}
			else
			{
				//map to new 
				ActivityServices.SiteActivityAdd( label, "View", "Detail", string.Format( "User viewed {0}: {1} ({2})", label, entity.Name, entity.Id ), 0, 0, recordId );

				string jsonoutput = JsonConvert.SerializeObject( entity, JsonHelper.GetJsonSettings() );

				SendResponse( jsonoutput );
			}
		}

	}
}

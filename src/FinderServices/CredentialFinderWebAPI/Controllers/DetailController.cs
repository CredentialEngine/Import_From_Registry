using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.ProfileModels;
using workIT.Services;
using workIT.Utilities;
using Newtonsoft.Json;
using System.Web;

namespace CredentialFinderWebAPI.Controllers
{
    public class DetailController : ApiController
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
				HttpContext.Current.Response.Clear();
				HttpContext.Current.Response.ContentType = "application/json";
				HttpContext.Current.Response.Write( messages );
				HttpContext.Current.Response.End();
			}
			else
			{
				ActivityServices.SiteActivityAdd( "Credential", "View", "Detail", string.Format( "User viewed Credential: {0} ({1})", entity.Name, entity.Id ), 0, 0, recordId );

				string jsonoutput = JsonConvert.SerializeObject( entity, JsonHelper.GetJsonSettings() );

				HttpContext.Current.Response.Clear();
				HttpContext.Current.Response.ContentType = "application/json";
				HttpContext.Current.Response.Write( jsonoutput );
				HttpContext.Current.Response.End();
			}
		}

		[HttpGet, Route( "Organization/{id}" )]
		public void Organization( string id )
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
				HttpContext.Current.Response.Clear();
				HttpContext.Current.Response.ContentType = "application/json";
				HttpContext.Current.Response.Write( messages );
				HttpContext.Current.Response.End();
			}
			else
			{
				ActivityServices.SiteActivityAdd( "Organization", "View", "Detail", string.Format( "User viewed Organization: {0} ({1})", entity.Name, entity.Id ), 0, 0, recordId );

				string jsonoutput = JsonConvert.SerializeObject( entity, JsonHelper.GetJsonSettings() );

				HttpContext.Current.Response.Clear();
				HttpContext.Current.Response.ContentType = "application/json";
				HttpContext.Current.Response.Write( jsonoutput );
				HttpContext.Current.Response.End();
			}
		}

		[HttpGet, Route( "Assessment/{id}" )]
		public void Assessment( string id )
		{
			int recordId = 0;
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
				messages.Add( "ERROR - Invalid request - the record was not found. " );
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
				ActivityServices.SiteActivityAdd( "Assessment", "View", "Detail", string.Format( "User viewed Assessment: {0} ({1})", entity.Name, entity.Id ), 0, 0, recordId );

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
				messages.Add( "ERROR - Invalid request - the record was not found. " );
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
				ActivityServices.SiteActivityAdd( "LearningOpportunity", "View", "Detail", string.Format( "User viewed LearningOpportunity: {0} ({1})", entity.Name, entity.Id ), 0, 0, recordId );

				string jsonoutput = JsonConvert.SerializeObject( entity, JsonHelper.GetJsonSettings() );

				HttpContext.Current.Response.Clear();
				HttpContext.Current.Response.ContentType = "application/json";
				HttpContext.Current.Response.Write( jsonoutput );
				HttpContext.Current.Response.End();
			}
		}
	}
}

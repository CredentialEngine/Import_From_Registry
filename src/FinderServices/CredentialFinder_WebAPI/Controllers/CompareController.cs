using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;

using Newtonsoft.Json.Linq;

using API = workIT.Services.API;
using CredentialFinderWebAPI.Models;
using workIT.Utilities;
using workIT.Services;

namespace CredentialFinderWebAPI.Controllers
{
	//Methods to get data for the compare page
	//Needed in order to ensure reports and such reference the right kind of data load (ie not triggering a detail page report for a compare load)
    public class CompareController : BaseController
    {
		private T GetCompareData<T>( string idOrCTID, Func<int, bool, T> GetByID, Func<string, bool, T> GetByCTID, JObject debug )
		{
			try
			{
				var id = 0;
				var skippingCache = FormHelper.GetRequestKeyValue( "skipCache", false );
				return int.TryParse( idOrCTID, out id ) ? GetByID( id, skippingCache ) : GetByCTID( idOrCTID, skippingCache );
			}
			catch ( Exception ex )
			{
				debug.Add( "Error for ID", idOrCTID );
				debug.Add( "Exception", ex.Message );
				debug.Add( "Inner Exception", ex.InnerException?.Message );
				return default(T);
			}
		}
		//

		private void SendCompareData<T>( string idOrCTID, T data, bool isValid, string activityLabel, int activityID, List<string> messages = null, JObject debug = null )
		{
			if ( isValid )
			{
				ActivityServices.SiteActivityAdd( activityLabel, "Compare", "Compare Page", "User loaded data for compare page: " + activityLabel + " " + idOrCTID, 0, 0, activityID );
				SendResponse( new ApiResponse( data, true ) );
			}
			else
			{
				messages = messages ?? new List<string>();
				messages.Add( "Error loading data for " + idOrCTID );
				SendDebuggingResponse( new ApiResponse( null, false, messages ), debug );
			}
		}
		//

		[HttpGet, Route( "compare/credential/{id}" )]
		public void CompareCredential( string id )
		{
			var debug = new JObject();
			var data = GetCompareData( id, API.CredentialServices.GetDetailForAPI, API.CredentialServices.GetDetailByCtidForAPI, debug );
			SendCompareData( id, data, (data?.Meta_Id ?? 0) > 0, "Credential", data?.Meta_Id ?? 0, null, debug );
		}
		//

		[HttpGet, Route( "compare/organization/{id}" )]
		public void CompareOrganization( string id )
		{
			var debug = new JObject();
			bool includingAll = FormHelper.GetRequestKeyValue( "includeAllData", false );
			var data = GetCompareData( id, delegate ( int recordID, bool skip ) { return API.OrganizationServices.GetDetailForAPI( recordID, skip, includingAll ); }, API.OrganizationServices.GetDetailByCtidForAPI, debug );
			SendCompareData( id, data, ( data?.Meta_Id ?? 0 ) > 0, "Organization", data?.Meta_Id ?? 0, null, debug );
		}
		//

		[HttpGet, Route( "compare/assessment/{id}" )]
		public void CompareAssessmentProfile( string id )
		{
			var debug = new JObject();
			var data = GetCompareData( id, API.AssessmentServices.GetDetailForAPI, API.AssessmentServices.GetDetailByCtidForAPI, debug );
			SendCompareData( id, data, ( data?.Meta_Id ?? 0 ) > 0, "AssessmentProfile", data?.Meta_Id ?? 0, null, debug );
		}
		//

		[HttpGet, Route( "compare/learningopportunity/{id}" )]
		public void CompareLearningOpportunityProfile( string id )
		{
			var debug = new JObject();
			var data = GetCompareData( id, API.LearningOpportunityServices.GetDetailForAPI, API.LearningOpportunityServices.GetDetailByCtidForAPI, debug );
			SendCompareData( id, data, ( data?.Meta_Id ?? 0 ) > 0, "LearningOpportunityProfile", data?.Meta_Id ?? 0, null, debug );
		}
		//

		[HttpGet, Route( "compare/transfervalue/{id}" )]
		public void CompareTransferValueProfile( string id )
		{
			var debug = new JObject();
			var data = GetCompareData( id, API.TransferValueServices.GetDetailForAPI, API.TransferValueServices.GetDetailByCtidForAPI, debug );
			SendCompareData( id, data, ( data?.Meta_Id ?? 0 ) > 0, "TransferValueProfile", data?.Meta_Id ?? 0, null, debug );
		}
		//

		[HttpGet, Route( "compare/pathway/{id}" )]
		public void ComparePathway( string id )
		{
			var debug = new JObject();
			var data = GetCompareData( id, API.PathwayServices.GetDetailForAPI, API.PathwayServices.GetDetailByCtidForAPI, debug );
			SendCompareData( id, data, ( data?.Meta_Id ?? 0 ) > 0, "Pathway", data?.Meta_Id ?? 0, null, debug );
		}
		//

		[HttpGet, Route( "compare/competencyframework/{id}" )]
		public void CompareCompetencyFramework( string id )
		{
			var debug = new JObject();
			var data = GetCompareData( id, API.CompetencyFrameworkServices.GetDetailForAPI, API.CompetencyFrameworkServices.GetDetailByCtidForAPI, debug );
			SendCompareData( id, data, ( data?.Meta_Id ?? 0 ) > 0, "CompetencyFramework", data?.Meta_Id ?? 0, null, debug );
		}
		//

		[HttpGet, Route( "compare/collection/{id}" )]
		public void CompareCollection( string id )
		{
			var debug = new JObject();
			var data = GetCompareData( id, API.CollectionServices.GetDetailForAPI, API.CollectionServices.GetDetailByCtidForAPI, debug );
			SendCompareData( id, data, ( data?.Meta_Id ?? 0 ) > 0, "Collection", data?.Meta_Id ?? 0, null, debug );
		}
		//

		[HttpGet, Route( "compare/conceptscheme/{id}" )]
		public void CompareConceptScheme( string id )
		{
			var debug = new JObject();
			var data = GetCompareData( id, API.ConceptSchemeServices.GetConceptSchemeOnlyByID, API.ConceptSchemeServices.GetConceptSchemeOnlyByCTID, debug );
			SendCompareData( id, data, ( data?.Meta_Id ?? 0 ) > 0, "ConceptScheme", data?.Meta_Id ?? 0, null, debug );
		}
		//

	}
}
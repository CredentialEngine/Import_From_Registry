using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

using CredentialFinderWebAPI.Models;
using CredentialFinderWebAPI.Services;

using workIT.Utilities;

namespace CredentialFinderWebAPI.Controllers
{
    public class ValidationController : ApiController
    {
		//[ApiExplorerSettings( IgnoreApi = true )]
		[HttpGet, Route( "Validation/AssessmentConnection/{ctid}" )]
		public ApiResponse ValidateAssessmentConnection( string ctid )
		{
			bool isValid = true;
			List<string> messages = new List<string>();
			var response = new ApiResponse();
			//some validation?
			
			string apiKey = "";
			//can the api validation and request validation be done at the same time to minimize 
			//if ( !AuthorizationServices.IsAuthTokenValid( true, ref apiKey, ref statusMessage ) )
			//{
			//	response.Messages.Add( "Error: A valid Apikey was not provided in an Authorization Header. " + statusMessage );
			//	return response;
			//}
			if ( string.IsNullOrWhiteSpace( ctid ) || ctid.Length != 39 )
			{
				response.Messages.Add( "A valid Assessment CTID must be provided." );
				return response;
			}
			//
			string status = "";
			if ( ConnectionServices.DoesAssessmentHaveCredentialConnection( ctid, ref status ))
			{
				response.Successful = true;
			}
			else
			{
				LoggingHelper.DoTrace( 6, string.Format( "CredentialFinderWebAPI.ValidationController.ValidateAssessmentConnection FAILED for assessmentCTID:{0}", ctid ) );
				response.Messages.Add( "The Assessment is not connected to any credentials." );
			}
			return response; ;

		}

		[HttpGet, Route( "Validation/LearningOpportunityConnection/{ctid}" )]
		public ApiResponse ValidateLearningOpportunityConnection( string ctid )
		{
			bool isValid = true;
			List<string> messages = new List<string>();
			var response = new ApiResponse();
			//some validation?

			string apiKey = "";
			//can the api validation and request validation be done at the same time to minimize 
			//if ( !AuthorizationServices.IsAuthTokenValid( true, ref apiKey, ref statusMessage ) )
			//{
			//	response.Messages.Add( "Error: A valid Apikey was not provided in an Authorization Header. " + statusMessage );
			//	return response;
			//}
			if ( string.IsNullOrWhiteSpace( ctid ) || ctid.Length != 39 )
			{
				response.Messages.Add( "A valid LearningOpportunity CTID must be provided." );
				return response;
			}
			//
			string status = "";
			if ( ConnectionServices.DoesLearningOpportunityHaveCredentialConnection( ctid, ref status ) )
			{
				response.Successful = true;
			}
			else
			{
				LoggingHelper.DoTrace( 6, string.Format( "CredentialFinderWebAPI.ValidationController.ValidateLearningOpportunityConnection FAILED for LearningOpportunityCTID:{0}", ctid ) );
				response.Messages.Add( "The LearningOpportunity is not connected to any credentials." );
			}
			return response; ;

		}

	}
}

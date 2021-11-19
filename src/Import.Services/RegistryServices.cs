using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RA.Models.JsonV2;

using workIT.Models.API.RegistrySearchAPI;
using workIT.Utilities;

namespace Import.Services
{
	public class RegistryServices
	{
		public static string credentialEngineAPIKey = UtilityManager.GetAppKeyValue( "MyCredentialEngineAPIKey" );

		public static string REGISTRY_ACTION_DELETE = "Registry Delete";
		public static string REGISTRY_ACTION_PURGE = "Registry Purge";
		public static string REGISTRY_ACTION_TRANSFER = "Transfer of Owner";
		public static string REGISTRY_ACTION_REMOVE_ORG = "RemoveOrganization";

		public RegistryServices()
		{
			//Community
		}
		public string Community { get; set; }

		#region Registry search

		public static List<ReadEnvelope> Search( string resourceType, string startingDate, string endingDate, int pageNbr, int pageSize, ref int pTotalRows, ref string statusMessage, string community, string sortOrder = "asc" )
		{

			string document = "";
			string filter = "";
			//includes the question mark
			string serviceUri = GetRegistrySearchUrl( community );

			//from=2016-08-22T00:00:00&until=2016-08-31T23:59:59
			//resource_type=credential
			if ( !string.IsNullOrWhiteSpace( resourceType ) )
				filter = string.Format( "resource_type={0}", resourceType.ToLower() );

			SetPaging( pageNbr, pageSize, ref filter );
			SetDateFilters( startingDate, endingDate, ref filter );
			SetSortOrder( ref filter, sortOrder );

			serviceUri += filter.Length > 0 ? filter : "";
			List<ReadEnvelope> list = new List<ReadEnvelope>();

			try
			{
				using ( var client = new HttpClient() )
				{
					System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

					client.DefaultRequestHeaders.
						Accept.Add( new MediaTypeWithQualityHeaderValue( "application/json" ) );
					if ( !string.IsNullOrWhiteSpace( credentialEngineAPIKey ) )
					{
						client.DefaultRequestHeaders.Add( "Authorization", "Token " + credentialEngineAPIKey );
					}
					var task = client.GetAsync( serviceUri );
					task.Wait();
					var response = task.Result;
					document = task.Result.Content.ReadAsStringAsync().Result;
					if ( response.IsSuccessStatusCode == false )
					{
						statusMessage = response.StatusCode + ": " + document;
						list = null;
						return list;
					}

					//just in case check 
					if ( !string.IsNullOrWhiteSpace( document ) )
					{
						if ( document.IndexOf( "Invalid" ) == 0
							|| document.IndexOf( "Error: " ) > -1
							|| document.IndexOf( "An API Key is required " ) > -1
							)
						{
							statusMessage = document;
							list = null;
							return list;
						}
					}
					//map to the list
					list = JsonConvert.DeserializeObject<List<ReadEnvelope>>( document );
					//
					string total = response.Headers.GetValues( "X-Total" ).FirstOrDefault();
					Int32.TryParse( total, out pTotalRows );
					string totalPages = response.Headers.GetValues( "X-Total-Pages" ).FirstOrDefault();

				}
			}
			catch ( Exception exc )
			{
				LoggingHelper.LogError( exc, "RegistryServices.Search. Using: " + serviceUri, false );
				statusMessage = exc.Message;
			}
			return list;
		}

		[Obsolete]
		public static List<ReadEnvelope> SearchOld( string resourceType, string startingDate, string endingDate, int pageNbr, int pageSize, ref int pTotalRows, ref string statusMessage, string community, string sortOrder = "asc" )
		{

			string document = "";
			string filter = "";
			//includes the question mark
			string serviceUri = GetRegistrySearchUrl( community );

			//from=2016-08-22T00:00:00&until=2016-08-31T23:59:59
			//resource_type=credential
			if ( !string.IsNullOrWhiteSpace( resourceType ) )
				filter = string.Format( "resource_type={0}", resourceType.ToLower() );

			SetPaging( pageNbr, pageSize, ref filter );
			SetDateFilters( startingDate, endingDate, ref filter );
			SetSortOrder( ref filter, sortOrder );

			serviceUri += filter.Length > 0 ? filter : "";
			//future proof

			List<ReadEnvelope> list = new List<ReadEnvelope>();
			try
			{
				WebRequest request = WebRequest.Create( serviceUri );

				// If required by the server, set the credentials.
				request.Credentials = CredentialCache.DefaultCredentials;

				var hdr = new WebHeaderCollection
				{
					{ "Authorization", "Token  " + credentialEngineAPIKey }
				};
				request.Headers.Add( hdr );

				HttpWebResponse response = ( HttpWebResponse )request.GetResponse();
				Stream dataStream = response.GetResponseStream();

				// Open the stream using a StreamReader for easy access.
				StreamReader reader = new StreamReader( dataStream );
				// Read the content.
				document = reader.ReadToEnd();

				// Cleanup the streams and the response.
				reader.Close();
				dataStream.Close();
				response.Close();
				//check document for an error message
				//also check the http status - have to leave as OK, or would be an exception?
				if ( !string.IsNullOrWhiteSpace( document ) )
				{
					if ( document.IndexOf( "Invalid" ) == 0
						|| document.IndexOf( "Error: " ) > -1
						|| document.IndexOf( "An API Key is required " ) > -1
						)
					{
						statusMessage = document;
						list = null;
						return list;
					}
				}

				//Link contains links for paging
				var hdr2 = response.GetResponseHeader( "Link" );
				Int32.TryParse( response.GetResponseHeader( "Total" ), out pTotalRows );
				//20-07-02 mp - seems the header name is now X-Total
				if ( pTotalRows == 0 )
				{
					Int32.TryParse( response.GetResponseHeader( "X-Total" ), out pTotalRows );
				}
				//map to the list
				list = JsonConvert.DeserializeObject<List<ReadEnvelope>>( document );

			}
			catch ( Exception exc )
			{
				LoggingHelper.LogError( exc, "RegistryServices.Search. Using: " + serviceUri, false );
				statusMessage = exc.Message;
			}
			return list;
		}

		public static string GetRegistrySearchUrl( string community = "" )
		{
			if ( string.IsNullOrWhiteSpace( community ) )
			{
				community = UtilityManager.GetAppKeyValue( "defaultCommunity" );
			}
			//
			if ( UtilityManager.GetAppKeyValue( "usingAssistantRegistrySearch", false ) )
			{
				string serviceUri = UtilityManager.GetAppKeyValue( "assistantCredentialRegistrySearch" );
				if ( !string.IsNullOrWhiteSpace( community ) )
					serviceUri += "community=" + community + "&";

				return serviceUri;
			}
			else
			{
				string serviceUri = UtilityManager.GetAppKeyValue( "credentialRegistrySearch" );
				serviceUri = string.Format( serviceUri, community );
				return serviceUri;
			}
			//
			//string serviceUri = UtilityManager.GetAppKeyValue( "credentialRegistrySearch" );
			//serviceUri = string.Format( serviceUri, community );
			//return serviceUri;
		}
		public static string GetEnvelopeUrl( string envelopeId, string community = "" )
		{
			if ( string.IsNullOrWhiteSpace( community ) )
			{
				community = UtilityManager.GetAppKeyValue( "defaultCommunity" );
			}
			string serviceUri = UtilityManager.GetAppKeyValue( "cerGetEnvelope" );

			string registryEnvelopeUrl = string.Format( serviceUri, community, envelopeId );
			return registryEnvelopeUrl;
		}
		public static string GetResourceUrl( string ctid, string community = "" )
		{
			if ( string.IsNullOrWhiteSpace( community ) )
			{
				community = UtilityManager.GetAppKeyValue( "defaultCommunity" );
			}
			string serviceUri = UtilityManager.GetAppKeyValue( "credentialRegistryResource" );

			string registryUrl = string.Format( serviceUri, community, ctid );

			return registryUrl;
		}
		//public static string GetRegistryEnvelopeUrl(string community)
		//{
		//	//the app key should be changed to be more meaningful!!
		//	string serviceUri = UtilityManager.GetAppKeyValue( "cerGetEnvelope" );
		//	serviceUri = string.Format( serviceUri, community );
		//	return serviceUri;
		//}
		public static string GetRegistryUrl( string appKey, string community )
		{
			//requires all urls to have a parameter?
			//or, check if the default community exists
			//also have to handle an absence community
			string serviceUri = UtilityManager.GetAppKeyValue( appKey );
			serviceUri = string.Format( serviceUri, community );
			return serviceUri;
		}

		private static void SetPaging( int pageNbr, int pageSize, ref string where )
		{
			string AND = "";
			if ( where.Length > 0 )
				AND = "&";

			if ( pageNbr > 0 )
			{
				where = where + AND + string.Format( "page={0}", pageNbr );
				AND = "&";
			}
			if ( pageSize > 0 )
			{
				where = where + AND + string.Format( "per_page={0}", pageSize );
				AND = "&";
			}
		}
		private static void SetSortOrder( ref string where, string sortOrder = "asc" )
		{
			string AND = "";
			if ( where.Length > 0 )
				AND = "&";
			if ( string.IsNullOrWhiteSpace( sortOrder ) )
				sortOrder = "asc";
			//this is the default anyway - maybe not, seems like dsc is the default
			where = where + AND + "sort_by=updated_at&sort_order=" + sortOrder;
		}

		private static void SetDateFilters( string startingDate, string endingDate, ref string where )
		{
			string AND = "";
			if ( where.Length > 0 )
				AND = "&";

			string date = FormatDateFilter( startingDate );
			if ( !string.IsNullOrWhiteSpace( date ) )
			{
				where = where + AND + string.Format( "from={0}", date );
				AND = "&";
			}

			date = FormatDateFilter( endingDate );
			if ( !string.IsNullOrWhiteSpace( date ) )
			{
				where = where + AND + string.Format( "until={0}", date );
				AND = "&";
			}
			//if ( !string.IsNullOrWhiteSpace( endingDate ) && endingDate.Length == 10 )
			//{
			//	where = where + AND + string.Format( "until={0}T23:59:59", endingDate );
			//}
		}
		private static string FormatDateFilter( string date )
		{
			string formatedDate = "";
			if ( string.IsNullOrWhiteSpace( date ) )
				return "";

			//start by checking for just properly formatted date
			if ( !string.IsNullOrWhiteSpace( date ) && date.Length == 10 )
			{
				//apparently this is not necessary!!
				formatedDate = string.Format( "{0}T00:00:00", date );
			}
			else if ( !string.IsNullOrWhiteSpace( date ) )
			{
				//check if in proper format - perhaps with time provided
				if ( date.IndexOf( "T" ) > 8 )
				{
					formatedDate = string.Format( "{0}", date );
				}
				else
				{
					//not sure how to handle unexpected date except to ignore
					//might be better to send actual DateTime field
				}
			}

			return formatedDate;
		}
		#endregion


		#region Registry Graph Search

		public static List<ReadEnvelope> GraphSearchByTemplate( string queryName, int skip, int take, ref int pTotalRows, ref string statusMessage, string community, string sortOrder = "asc" )
		{

			var jsonQuery = "";
			return GraphSearch( jsonQuery, skip, take, ref pTotalRows, ref statusMessage, community, sortOrder);
		}
		public static List<ReadEnvelope> GraphSearch( string jsonQuery, int skip, int take, ref int pTotalRows, ref string statusMessage, string community, string sortOrder = "asc" )
		{
			//Hold the query
			var apiQuery = new SearchQuery()
			{
				Skip = skip,
				Take = take
			};


			//Hold the response
			var results = new List<ReadEnvelope>();

			//...
			//Do the query
			try
			{
				var jquery = JObject.Parse( jsonQuery );
				apiQuery.Query = jquery;
				var rawResults = DoRegistrySearchAPIQuery( apiQuery );

				if ( !rawResults.valid )
				{
					LoggingHelper.DoTrace(1, string.Format( "RegistryServices.Error Performing Search: {0}", rawResults.status ));
					return results;
				}

				//Compose the results
				//TODO - what do we get, we want the full graph json
				//WARNING - JUST THE RESOURCE, NO BLANK NODES, CHECK DESCRIPTION SET
				//loop thru and call API to get the envelope
				var ctid = "";
				var ctdlType = "";
				foreach ( var item in rawResults.data )
				{
					//item is a JObject. Serialize to ?? and get the ctid
					//GraphMainResource
					var resource = GetGraphMainResource( item.ToString() );
					statusMessage = "";
					var envelope = GetEnvelopeByCtid( resource.CTID, ref statusMessage, ref ctdlType, community );
					//if OK
					results.Add( envelope );
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, "error: " + ex.Message );
			}
			

			//Return the results
			return results;
		}
		//TODO - this should probably be move to work.IT.Services.RegistryServices
		private static SearchResponse DoRegistrySearchAPIQuery( SearchQuery query )
		{
			var response = new SearchResponse();

			//Get API key and URL
			var apiKey = UtilityManager.GetAppKeyValue( "MyCredentialEngineAPIKey", "" );
			var apiURL = ConfigHelper.GetConfigValue( "AssistantCTDLJSONSearchAPIUrl", "" );

			//Format the request
			var queryJSON = JsonConvert.SerializeObject( query, new JsonSerializerSettings() { Formatting = Formatting.None, NullValueHandling = NullValueHandling.Ignore } );

			//Do the request
			var client = new HttpClient();
			client.DefaultRequestHeaders.TryAddWithoutValidation( "Authorization", "ApiToken " + apiKey );
			client.Timeout = new TimeSpan( 0, 10, 0 );
			System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
			var rawResult = client.PostAsync( apiURL, new StringContent( queryJSON, Encoding.UTF8, "application/json" ) ).Result;

			//Process the response
			if ( !rawResult.IsSuccessStatusCode )
			{
				response.valid = false;
				response.status = rawResult.ReasonPhrase;
				return response;
			}

			try
			{
				response = JsonConvert.DeserializeObject<SearchResponse>( rawResult.Content.ReadAsStringAsync().Result, new JsonSerializerSettings() { DateParseHandling = DateParseHandling.None } );
			}
			catch ( Exception ex )
			{
				response.valid = false;
				response.status = "Error parsing response: " + ex.Message + ( ex.InnerException != null ? " " + ex.InnerException.Message : "" );
			}

			return response;
		}
		
		#endregion


		#region Registry Gets

		/*
		/// <summary>
		/// Retrieve an envelop from the registry and do import
		/// Custom version for Import from Finder.Import
		/// TODO - THIS IS SOMEWHAT HIDDEN HERE - easy to forget when adding new classes
		/// </summary>
		/// <param name="envelopeId"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		[Obsolete]
		public bool ImportByEnvelopeId( string envelopeId, SaveStatus status )
        {
            //this is currently specific, assumes envelop contains a credential
            //can use the hack for GetResourceType to determine the type, and then call the appropriate import method

            if ( string.IsNullOrWhiteSpace( envelopeId ) )
            {
                status.AddError( "ImportByEnvelope - a valid envelope id must be provided" );
                return false;
            }

            string statusMessage = "";
            string ctdlType = "";
            try
            {
                ReadEnvelope envelope = RegistryServices.GetEnvelope( envelopeId, ref statusMessage, ref ctdlType );
				if ( envelope == null || string.IsNullOrWhiteSpace(envelope.EnvelopeType))
				{
					string defCommunity = UtilityManager.GetAppKeyValue( "defaultCommunity" );
					string community = UtilityManager.GetAppKeyValue( "additionalCommunity" );
					if (defCommunity != community)
						envelope = RegistryServices.GetEnvelope( envelopeId, ref statusMessage, ref ctdlType, community );
				}

				if ( envelope != null && !string.IsNullOrWhiteSpace( envelope.EnvelopeIdentifier ) )
                {
                    LoggingHelper.DoTrace( 4, string.Format( "RegistryServices.ImportByEnvelopeId ctdlType: {0}, EnvelopeId: {1} ", ctdlType, envelopeId ) );
                    ctdlType = ctdlType.Replace( "ceterms:", "" );

                    switch ( ctdlType.ToLower() )
                    {
                        case "credentialorganization":
                        case "qacredentialorganization": //what distinctions do we need for QA orgs?
                        case "organization":
                            return new ImportOrganization().CustomProcessEnvelope( envelope, status );
                        //break;CredentialOrganization
                        case "assessmentprofile":
                            return new ImportAssessment().CustomProcessEnvelope( envelope, status );
                        //break;
                        case "learningopportunityprofile":
                            return new ImportLearningOpportunties().CustomProcessEnvelope( envelope, status );
                        //break;
                        case "conditionmanifest":
                            return new ImportAssessment().CustomProcessEnvelope( envelope, status );
                        //break;
                        case "costmanifest":
                            return new ImportLearningOpportunties().CustomProcessEnvelope( envelope, status );
						case "competencyframework":
							return new ImportCompetencyFramesworks().CustomProcessEnvelope( envelope, status );
						case "conceptscheme":
							return new ImportConceptSchemes().CustomProcessEnvelope( envelope, status );
						case "pathway":
							return new ImportPathways().CustomProcessEnvelope( envelope, status );
						case "pathwaysset":
							return new ImportPathwaySets().CustomProcessEnvelope( envelope, status );
						case "rating":
						{
							LoggingHelper.DoTrace( 1, string.Format( "ImportByEnvelopeId. Ratings ({0}-{1}) are not handled at this time. ", envelope.EnvelopeCtdlType, envelope.EnvelopeCetermsCtid ) );
							return false;
						}
						case "rubric":
						{
							LoggingHelper.DoTrace( 1, string.Format( "ImportByEnvelopeId. Rubrics ({0}-{1}) are not handled at this time. ", envelope.EnvelopeCtdlType, envelope.EnvelopeCetermsCtid ) );
							return false;
						}
						case "transfervalueprofile":
							return new ImportTransferValue().CustomProcessEnvelope( envelope, status );
						//break;
						default:
                            //default to credential
                            return new ImportCredential().CustomProcessRequest( envelope, status );
                            //break;
                    }
                }
                else
                    return false;
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, string.Format( "RegistryServices`.ImportByEnvelopeId(). ctdlType: {0}", ctdlType ) );
                status.AddError( ex.Message );
                if ( ex.Message.IndexOf( "Path '@context', line 1" ) > 0 )
                {
                    status.AddWarning( "The referenced registry document is using an old schema. Please republish it with the latest schema!" );
                }
                return false;
            }
        }
		[Obsolete]
        public bool ImportByCtid( string ctid, SaveStatus status )
        {
            //this is currently specific, assumes envelop contains a credential
            //can use the hack fo GetResourceType to determine the type, and then call the appropriate import method

            if ( string.IsNullOrWhiteSpace( ctid ) )
            {
                status.AddError( "ImportByCtid - a valid ctid must be provided" );
                return false;
            }

            string statusMessage = "";
            string ctdlType = "";
            //string payload = "";
            try
            {
				//TODO - should get envelope by ctid in order to get envelope dates
				ReadEnvelope envelope = RegistryServices.GetEnvelopeByCtid( ctid, ref statusMessage, ref ctdlType );
				if ( envelope == null || string.IsNullOrWhiteSpace( envelope.EnvelopeType ) )
				{
					string defCommunity = UtilityManager.GetAppKeyValue( "defaultCommunity" );
					string community = UtilityManager.GetAppKeyValue( "additionalCommunity" );
					if ( defCommunity != community )
						envelope = RegistryServices.GetEnvelopeByCtid( ctid, ref statusMessage, ref ctdlType, community );
				}
				if ( envelope != null && !string.IsNullOrWhiteSpace( envelope.EnvelopeIdentifier ) )
				{
					LoggingHelper.DoTrace( 4, string.Format( "RegistryServices.ImportByCtid ctdlType: {0}, CTID: {1} ", ctdlType, ctid ) );
					ctdlType = ctdlType.Replace( "ceterms:", "" );

					switch ( ctdlType.ToLower() )
					{
						case "credentialorganization":
						case "qacredentialorganization": //what distinctions do we need for QA orgs?
						case "organization":
							return new ImportOrganization().CustomProcessEnvelope( envelope, status );
						//break;CredentialOrganization
						case "assessmentprofile":
							return new ImportAssessment().CustomProcessEnvelope( envelope, status );
						//break;
						case "learningopportunityprofile":
							return new ImportLearningOpportunties().CustomProcessEnvelope( envelope, status );
						//break;
						case "conditionmanifest":
							return new ImportAssessment().CustomProcessEnvelope( envelope, status );
						//break;
						case "costmanifest":
							return new ImportLearningOpportunties().CustomProcessEnvelope( envelope, status );
						case "competencyframework":
							return new ImportCompetencyFramesworks().CustomProcessEnvelope( envelope, status );
						case "pathway":
							return new ImportPathways().CustomProcessEnvelope( envelope, status );
						case "transfervalueprofile":
							return new ImportTransferValue().CustomProcessEnvelope( envelope, status );
						//break;
						default:
							//default to credential
							return new ImportCredential().CustomProcessRequest( envelope, status );
							//break;
					}
				}
				else
					return false;
				
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, string.Format( "ImportCredential.ImportByCtid(). ctdlType: {0}", ctdlType ) );
                status.AddError( ex.Message );
                if ( ex.Message.IndexOf( "Path '@context', line 1" ) > 0 )
                {
                    status.AddWarning( "The referenced registry document is using an old schema. Please republish it with the latest schema!" );
                }
                return false;
            }
        }
		*/

		/// <summary>
		/// Retrieve an envelope from the registry - using either envelopeId or CTID (same format)
		/// </summary>
		/// <param name="envelopeId">20-12-07 - sometimes CTID will be passed as will work as expected.</param>
		/// <param name="statusMessage"></param>
		/// <param name="ctdlType"></param>
		/// <returns></returns>
		public static ReadEnvelope GetEnvelope( string envelopeId, ref string statusMessage, ref string ctdlType, string community = "" )
        {
			//need to pass in an override community - eventually
			if (string.IsNullOrWhiteSpace( community ) )
				community = UtilityManager.GetAppKeyValue( "defaultCommunity" );
			string serviceUri = GetEnvelopeUrl( envelopeId, community );
            //
			serviceUri = string.Format( serviceUri, envelopeId );
            //LoggingHelper.DoTrace( 5, string.Format( "RegistryServices.GetEnvelope envelopeId: {0}, serviceUri: {1} ", envelopeId, serviceUri ) );
			
			return GetEnvelopeByURL( serviceUri, ref statusMessage, ref ctdlType );

		}
		public static ReadEnvelope GetEnvelopeByCtid( string ctid, ref string statusMessage, ref string ctdlType, string community = "" )
		{
			
			//need to pass in an override community - eventually
			if ( string.IsNullOrWhiteSpace( community ) )
				community = UtilityManager.GetAppKeyValue( "defaultCommunity" );
			string serviceUri = GetEnvelopeUrl( ctid, community );
			//
			serviceUri = string.Format( serviceUri, ctid );
			//LoggingHelper.DoTrace( 5, string.Format( "RegistryServices.GetEnvelope ctid: {0}, serviceUri: {1} ", ctid, serviceUri ) );

			return GetEnvelopeByURL( serviceUri, ref statusMessage, ref ctdlType );

		}

		public static ReadEnvelope GetEnvelopeByURL( string envelopeUrl, ref string statusMessage, ref string ctdlType )
		{
			string document = "";
			LoggingHelper.DoTrace( 5, string.Format( "RegistryServices.GetEnvelope envelopeUrl: {0} ", envelopeUrl ) );
			ReadEnvelope envelope = new ReadEnvelope();

			try
			{
				// Create a request for the URL.         
				WebRequest request = WebRequest.Create( envelopeUrl );
				// If required by the server, set the credentials.
				request.Credentials = CredentialCache.DefaultCredentials;
				var hdr = new WebHeaderCollection
				{
					{ "Authorization", "Token  " + credentialEngineAPIKey }
				};
				request.Headers.Add( hdr );

				//Get the response.
				HttpWebResponse response = ( HttpWebResponse )request.GetResponse();

				// Get the stream containing content returned by the server.
				Stream dataStream = response.GetResponseStream();

				// Open the stream using a StreamReader for easy access.
				StreamReader reader = new StreamReader( dataStream );
				// Read the content.
				document = reader.ReadToEnd();

				// Cleanup the streams and the response.

				reader.Close();
				dataStream.Close();
				response.Close();
				//check document for an error message
				//also check the http status - have to leave as OK, or would be an exception?
				if ( !string.IsNullOrWhiteSpace( document ) )
				{
					if ( document.IndexOf( "Invalid" ) == 0
						|| document.IndexOf( "Error: " ) > -1
						|| document.IndexOf( "An API Key is required " ) > -1
						)
					{
						statusMessage = document;
						return null;
					}
				}
				//map to the default envelope
				envelope = JsonConvert.DeserializeObject<ReadEnvelope>( document );

				if ( envelope != null && !string.IsNullOrWhiteSpace( envelope.EnvelopeIdentifier ) )
				{
					string payload = envelope.DecodedResource.ToString();
					ctdlType = RegistryServices.GetResourceType( payload );

					//return ProcessProxy( mgr, item, status );
				}
			}
			catch ( Exception exc )
			{
				if ( exc.Message.IndexOf( "(404) Not Found" ) > 0 )
				{
					statusMessage = string.Format( "RegistryServices.GetEnvelope. Not found for {0}", envelopeUrl );
					LoggingHelper.DoTrace( 1, statusMessage );
				}
				else
				{
					LoggingHelper.LogError( exc, "RegistryServices.GetEnvelope: " + envelopeUrl );
					statusMessage = exc.Message;
				}
				return null;
			}
			return envelope;
		}

		/// <summary>
		/// Use search to get the envelope for a ctid
		/// </summary>
		/// <param name="ctid"></param>
		/// <param name="statusMessage"></param>
		/// <param name="ctdlType"></param>
		/// <returns></returns>
		[Obsolete]
		public static ReadEnvelope GetEnvelopeByCtidSearch( string ctid, ref string statusMessage, ref string ctdlType, string community = "")
        {
            string document = "";
			//perhaps this should be done in the caller. It could check the default or previous import source
			if ( string.IsNullOrWhiteSpace( community ) )
				community = UtilityManager.GetAppKeyValue( "defaultCommunity" );
			string additionalCommunity = UtilityManager.GetAppKeyValue( "additionalCommunity" );

			//

			string searchUrl = GetRegistrySearchUrl( community );
			searchUrl = searchUrl + "ctid=" + ctid.ToLower();

            LoggingHelper.DoTrace( 5, string.Format( "RegistryServices.ImportByCtid ctid: {0}, searchUrl: {1} ", ctid, searchUrl ) );
            ReadEnvelope envelope = new ReadEnvelope();
            List<ReadEnvelope> list = new List<ReadEnvelope>();
            try
            {

                // Create a request for the URL.         
                WebRequest request = WebRequest.Create( searchUrl );
                request.Credentials = CredentialCache.DefaultCredentials;

				var hdr = new WebHeaderCollection
				{
					{ "Authorization", "Token  " + credentialEngineAPIKey }
				};
				request.Headers.Add( hdr );

				HttpWebResponse response = ( HttpWebResponse )request.GetResponse();
                Stream dataStream = response.GetResponseStream();

                // Open the stream using a StreamReader for easy access.
                StreamReader reader = new StreamReader( dataStream );
                // Read the content.
                document = reader.ReadToEnd();

                // Cleanup the streams and the response.

                reader.Close();
                dataStream.Close();
                response.Close();

                //map to list
                list = JsonConvert.DeserializeObject<List<ReadEnvelope>>( document );
                //only expecting one
                if ( list != null && list.Count > 0 )
                {
                    envelope = list[ 0 ];

                    if ( envelope != null && !string.IsNullOrWhiteSpace( envelope.EnvelopeIdentifier ) )
                    {
                        string payload = envelope.DecodedResource.ToString();
                        ctdlType = RegistryServices.GetResourceType( payload );
                    }
                }
            }
            catch ( Exception exc )
            {
				if ( exc.Message.IndexOf( "(404) Not Found" ) > 0 )
					LoggingHelper.DoTrace( 1, string.Format( "RegistryServices.GetEnvelopeByCtid. Not found for envelopeId: {0}", ctid ) );
				else
					LoggingHelper.LogError( exc, "RegistryServices.GetEnvelopeByCtid for " + ctid );
                statusMessage = exc.Message;
            }
            return envelope;
        }


        /// <summary>
        /// Retrieve a resource from the registry by ctid
        /// </summary>
        /// <param name="ctid"></param>
        /// <param name="statusMessage"></param>
        /// <returns></returns>
        public static string GetResourceByCtid( string ctid, ref string ctdlType, ref string statusMessage, string community = "")
        {
            string resourceIdUrl = GetResourceUrl( ctid, community );
            return GetResourceByUrl( resourceIdUrl, ref ctdlType, ref statusMessage );
        }

        public static string GetResourceGraphByCtid(string ctid, ref string ctdlType, ref string statusMessage, string community = "")
        {
            string registryUrl = GetResourceUrl( ctid, community );
            //not sure about this anymore
            //actually dependent on the purpose. If doing an import, then need graph
            //here will always want graph
            registryUrl = registryUrl.Replace( "/resources/", "/graph/" );         

            return GetResourceByUrl( registryUrl, ref ctdlType, ref statusMessage );
        }
        /// <summary>
        /// Retrieve a resource from the registry by resourceId
        /// </summary>
        /// <param name="resourceId">Url to a resource in the registry</param>
        /// <param name="statusMessage"></param>
        /// <returns></returns>
        public static string GetResourceByUrl( string resourceUrl, ref string ctdlType, ref string statusMessage )
        {
            string payload = "";
            //NOTE - getting by ctid means no envelopeid
            try
            {
                // Create a request for the URL.         
                WebRequest request = WebRequest.Create( resourceUrl );

                // If required by the server, set the credentials.
                request.Credentials = CredentialCache.DefaultCredentials;
				var hdr = new WebHeaderCollection
				{
					{ "Authorization", "Token  " + credentialEngineAPIKey }
				};
				request.Headers.Add( hdr );

				//Get the response.
				HttpWebResponse response = ( HttpWebResponse )request.GetResponse();

                // Get the stream containing content returned by the server.
                Stream dataStream = response.GetResponseStream();

                // Open the stream using a StreamReader for easy access.
                StreamReader reader = new StreamReader( dataStream );
                // Read the content.
                payload = reader.ReadToEnd();

                // Cleanup the streams and the response.

                reader.Close();
                dataStream.Close();
                response.Close();

                ctdlType = RegistryServices.GetResourceType( payload );
            }
            catch ( Exception exc )
            {
                if ( exc.Message.IndexOf( "(404) Not Found" ) > 0 )
                {
                    //need to surface these better
                    statusMessage = "ERROR - resource was not found in registry: " + resourceUrl;
                }
                else
                {
                    LoggingHelper.LogError( exc, "RegistryServices.GetResourceByUrl" );
                    statusMessage = exc.Message;
                }
            }
            return payload;
        }

        public static string GetCtidFromUnknownEnvelope( ReadEnvelope item )
        {
            string ctid = "";
            //string envelopeId = "";
            try
            {
                RegistryObject ro = new RegistryObject( item.DecodedResource.ToString() );
                ctid = ro.Ctid;

                //TODO - this will have to change for type of @graph
                //envelopeId = item.EnvelopeIdentifier;
                //ctid = item.EnvelopeCetermsCtid ?? "";
                //if ( !string.IsNullOrWhiteSpace( ctid ) )
                //    return ctid;

                //string payload = item.DecodedResource.ToString();
                //if ( payload.IndexOf( "@graph" ) > -1 )
                //{
                //    UnknownPayload input = JsonConvert.DeserializeObject<UnknownPayload>( item.DecodedResource.ToString() );
                //    //extract from the @id
                //    if ( !string.IsNullOrWhiteSpace( input.Ctid ) )
                //    {
                //        ctid = input.Ctid;
                //    }
                //    else if ( !string.IsNullOrWhiteSpace( input.CtdlId ) )
                //    {
                //        int pos = input.CtdlId.LastIndexOf( "/" );
                //        ctid = input.CtdlId.Substring( pos );
                //    }
                //}
                //else
                //{
                //    UnknownPayload input = JsonConvert.DeserializeObject<UnknownPayload>( item.DecodedResource.ToString() );
                //    ctid = input.Ctid;
                //}
            }
            catch ( Exception ex )
            {
                LoggingHelper.DoTrace( 2, "GetCtidFromUnknownEnvelope - unable to extract ctid from envelope" );
            }

            return ctid;
        }

        public static string GetResourceType( string payload )
        {
            string ctdlType = "";
            RegistryObject ro = new RegistryObject( payload );
            ctdlType = ro.CtdlType;
            //ctdlType = ctdlType.Replace( "ceterms:", "" );
            return ctdlType;
        }
		/// <summary>
		/// Get the primary type object for a graph from a Decoded resource (from an envelope)
		/// </summary>
		/// <param name="json"></param>
		/// <returns></returns>
		public static string GetGraphPrimaryType( string json )
		{
			if ( string.IsNullOrWhiteSpace( json ) )
				return null; //??
			var type = "";
			var resourceOutline = GetGraphMainResource( json );
			if ( resourceOutline != null )
				return resourceOutline.Type;
			else
				return type;
		}
		/// <summary>
		/// Get the main resource object for a graph from a Decoded resource (from an envelope)
		/// Should to handle the decodedResource which contains th @graph, or just the contents of the a payload (i.e. stuff inside a graph)
		/// </summary>
		/// <param name="json"></param>
		/// <returns></returns>
		public static GraphMainResource GetGraphMainResource( string json )
		{
			if ( string.IsNullOrWhiteSpace( json ) )
				return null; //??
			var graphMainResource = new GraphMainResource();
			Dictionary<string, object> dictionary = JsonToDictionary( json );
			if ( json.IndexOf( "@graph" ) > -1 )
			{
				object graph = dictionary[ "@graph" ];
				var glist = JsonConvert.SerializeObject( graph );
				//parse graph in to list of objects
				JArray graphList = JArray.Parse( glist );

				if ( graphList != null && graphList.Any() )
				{
					var main = graphList[ 0 ].ToString();
					graphMainResource = JsonConvert.DeserializeObject<GraphMainResource>( main );
				}
			} else if ( json.IndexOf( "@type" ) > -1 )
			{
				graphMainResource = JsonConvert.DeserializeObject<GraphMainResource>( json );
			}
			return graphMainResource;
		}
		/// <summary>
		/// Handle import of records with a 'pending' state (entityStateId=1)
		/// </summary>
		/// <returns></returns>
		public string ImportPending()
        {
            string status = "";
            LoggingHelper.DoTrace( 1, "Import.Services.RegistryServices.ImportPending - start" );
            new ImportCredential().ImportPendingRecords();

            new ImportOrganization().ImportPendingRecords();

            new ImportAssessment().ImportPendingRecords();

            new ImportLearningOpportunties().ImportPendingRecords();

            LoggingHelper.DoTrace( 1, "Import.Services.RegistryServices.ImportPending - completed" );
            return status;
        }



		/// <summary>
		/// Get list of objects in a JSON-LD graph
		/// </summary>
		/// <param name="json"></param>
		/// <returns>JArray</returns>
		public static JArray GetGraphList( string json )
		{
			if ( string.IsNullOrWhiteSpace( json ) )
				return null; //??

			Dictionary<string, object> dictionary = JsonToDictionary( json );
			object graph = dictionary[ "@graph" ];
			var glist = JsonConvert.SerializeObject( graph );
			//parse graph in to list of objects
			JArray graphList = JArray.Parse( glist );

			return graphList;
		}

		/// <summary>
		/// Generic handling of Json object - especially for unexpected types
		/// </summary>
		/// <param name="json"></param>
		/// <returns></returns>
		public static Dictionary<string, object> JsonToDictionary( string json )
        {
            var result = new Dictionary<string, object>();
            var obj = JObject.Parse( json );
            foreach ( var property in obj )
            {
                result.Add( property.Key, JsonToObject( property.Value ) );
            }
            return result;
        }
        public static object JsonToObject( JToken token )
        {
            switch ( token.Type )
            {
                case JTokenType.Object:
                {
                    return token.Children<JProperty>().ToDictionary( property => property.Name, property => JsonToObject( property.Value ) );
                }
                case JTokenType.Array:
                {
                    var result = new List<object>();
                    foreach ( var obj in token )
                    {
                        result.Add( JsonToObject( obj ) );
                    }
                    return result;
                }
                default:
                {
                    return ( ( JValue )token ).Value;
                }
            }
        }
		#endregion
	}


	public class RegistryObject
    {
        public RegistryObject( string payload )
        {
            if ( !string.IsNullOrWhiteSpace( payload ) )
            {
                dictionary = RegistryServices.JsonToDictionary( payload );
                if ( payload.IndexOf( "@graph" ) > 0 && payload.IndexOf( "@graph\": null") == -1 )
                {
                    IsGraphObject = true;
                    //get the graph object
                    object graph = dictionary[ "@graph" ];
                    //serialize the graph object
                    var glist = JsonConvert.SerializeObject( graph );
                    //parse graph in to list of objects
                    JArray graphList = JArray.Parse( glist );

                    var main = graphList[ 0 ].ToString();
                    BaseObject = JsonConvert.DeserializeObject<RegistryBaseObject>( main );
                    CtdlType = BaseObject.CdtlType;
                    Ctid = BaseObject.Ctid;
                    //not important to fully resolve yet
                    if ( BaseObject.Name != null )
                    {
                        Name = BaseObject.Name.ToString();
                        if (Name.IndexOf("{") > -1 && Name.IndexOf( ":" ) > 1)
                        {
                            int pos = Name.IndexOf( "\"", Name.IndexOf( ":" ) );
                            int endpos = Name.IndexOf( "\"", pos+1 );
                            if ( pos > 1 && endpos > pos )
                            {
                                Name = Name.Substring( pos + 1, endpos - ( pos + 1 ) );
                            }
                        }
                        if ( BaseObject.Name is LanguageMap )
                        {

                        }
                    }
                    else if ( CtdlType == "ceasn:CompetencyFramework" )
                    {
                        Name = ( BaseObject.CompetencyFrameworkName ?? "" ).ToString();
                    }
                    else
                        Name = "?????";

                    //if ( BaseObject.Name.GetType())
                    //{

                    //}
				}
                else
                {
                    //check if old resource or standalone resource
                    BaseObject = JsonConvert.DeserializeObject<RegistryBaseObject>( payload );
                    CtdlType = BaseObject.CdtlType;
                    Ctid = BaseObject.Ctid;
                    Name = BaseObject.Name.ToString();
                    if ( Name.IndexOf( "{" ) > -1 && Name.IndexOf( ":" ) > 1 )
                    {
                        int pos = Name.IndexOf( "\"", Name.IndexOf( ":" ) );
                        int endpos = Name.IndexOf( "\"", pos + 1 );
                        if ( pos > 1 && endpos > pos )
                        {
                            Name = Name.Substring( pos + 1, endpos - ( pos + 1 ) );
                        }
                    }
                }
                CtdlType = CtdlType.Replace( "ceterms:", "" );
				CtdlType = CtdlType.Replace( "ceasn:", "" );
			}
        }

        Dictionary<string, object> dictionary = new Dictionary<string, object>();

        public bool IsGraphObject { get; set; }
        public RegistryBaseObject BaseObject { get; set; } = new RegistryBaseObject();
        public string CtdlType { get; set; } = "";
        public string CtdlId { get; set; } = "";
        public string Ctid { get; set; } = "";
        public string Name { get; set;  }
    }

    public class RegistryBaseObject
    {
        [JsonProperty( "@id" )]
        public string CtdlId { get; set; }

        /// <summary>
        /// Type  of CTDL object
        /// </summary>
        [JsonProperty( "@type" )]
        public string CdtlType { get; set; }

        [JsonProperty( PropertyName = "ceterms:ctid" )]
        public string Ctid { get; set; }

        [JsonProperty( PropertyName = "ceterms:name" )]
        public object Name { get; set; }

		[JsonProperty( PropertyName = "ceterms:description" )]
		public object Description { get; set; }

		[JsonProperty( PropertyName = "ceasn:name" )]
		public object CompetencyFrameworkName { get; set; }

		[JsonProperty( PropertyName = "ceasn:description" )]
		public object FrameworkDescription { get; set; }


		[JsonProperty( PropertyName = "ceterms:subjectWebpage" )]
        public string SubjectWebpage { get; set; }

    }
}

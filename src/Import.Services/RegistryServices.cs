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
using Import.Services.RegistryModels;
using workIT.Factories;
using workIT.Models.API.RegistrySearchAPI;
using workIT.Utilities;

namespace Import.Services
{
	public class RegistryServices
	{
		public static string thisClassName = "Import.Services.RegistryServices";
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

		/// <summary>
		/// Do a registry search
		/// </summary>
		/// <param name="resourceType"></param>
		/// <param name="startingDate"></param>
		/// <param name="endingDate"></param>
		/// <param name="pageNbr"></param>
		/// <param name="pageSize"></param>
		/// <param name="pTotalRows"></param>
		/// <param name="statusMessage"></param>
		/// <param name="community"></param>
		/// <param name="owningOrganizationCTID"></param>
		/// <param name="publishingOrganizationCTID"></param>
		/// <param name="sortOrder"></param>
		/// <returns></returns>
		public static List<ReadEnvelope> Search( string resourceType, string startingDate, string endingDate, int pageNbr, int pageSize, ref int pTotalRows, ref string statusMessage, string community, string owningOrganizationCTID = "", string publishingOrganizationCTID = "", string sortOrder = "asc" )
		{
            //other
            //	metadata_only - true: Whether omit envelopes’ payloads
            string document = "";
			string filter = "";
			//includes the question mark
			string serviceUri = GetRegistrySearchUrl( community );

			//from=2016-08-22T00:00:00&until=2016-08-31T23:59:59
			//resource_type=credential
			if ( !string.IsNullOrWhiteSpace( resourceType ) )
			{
				if ( resourceType.StartsWith("ceterms:"))
					filter = string.Format( "envelope_ctdl_type={0}", resourceType );
				else
					filter = string.Format( "resource_type={0}", resourceType.ToLower() );
			}

			//
			SetOwningOrganizationFilters( owningOrganizationCTID, ref filter );
			SetPublishingOrganizationFilters( publishingOrganizationCTID, ref filter );

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
				LoggingHelper.LogError( exc, "RegistryServices.Search. Using: " + serviceUri );
				statusMessage = exc.Message;
			}
			return list;
		}
		/*
		[Obsolete]
		public static List<ReadEnvelope> SearchOld( string resourceType, string startingDate, string endingDate, int pageNbr, int pageSize, ref int pTotalRows, ref string statusMessage, string community, string sortOrder = "asc" )
		{

			string document = "";
			string filter = "";
			//includes the question mark
			string serviceUri = GetRegistrySearchUrl( community );
			if ( !string.IsNullOrWhiteSpace( resourceType ) )
				filter = string.Format( "resource_type={0}", resourceType.ToLower() );

			//from=2016-08-22T00:00:00&until=2016-08-31T23:59:59
			//resource_type=credential

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
		*/
		public static string GetRegistrySearchUrl( string community = "" )
		{
			if ( string.IsNullOrWhiteSpace( community ) )
			{
				community = UtilityManager.GetAppKeyValue( "defaultCommunity" );
			}
			//
			if ( UtilityManager.GetAppKeyValue( "usingAssistantRegistrySearch", true ) )
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
			//NOTE: got an error trying to get a resource using the RegistryURL, but worked using the Assistant equivalent?
			string serviceUri = UtilityManager.GetAppKeyValue( "cerEnvelopeURL" );

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
		//	string serviceUri = UtilityManager.GetAppKeyValue( "cerEnvelopeURL" );
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


		/// <summary>
		/// Get list of deleted records for the requested time period.
		/// </summary>
		/// <param name="community"></param>
		/// <param name="type"></param>
		/// <param name="startingDate">Date must be in UTC</param>
		/// <param name="endingDate">Date must be in UTC</param>
		/// <param name="pageNbr"></param>
		/// <param name="pageSize"></param>
		/// <param name="pTotalRows"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public static List<ReadEnvelope> GetDeleted( string community, string type, string startingDate, string endingDate, int pageNbr, int pageSize, ref int pTotalRows, ref string statusMessage )
		{
			string document = "";
			string filter = "include_deleted=only";
			string serviceUri = GetRegistrySearchUrl( community );
			//from=2016-08-22T00:00:00&until=2016-08-31T23:59:59
			//resource_type=credential
			if ( !string.IsNullOrWhiteSpace( type ) )
				filter += string.Format( "&resource_type={0}", type.ToLower() );

			SetPaging( pageNbr, pageSize, ref filter );
			SetDateFilters( startingDate, endingDate, ref filter );

			serviceUri += filter.Length > 0 ? filter : "";

			List<ReadEnvelope> list = new List<ReadEnvelope>();
			ReadEnvelope envelope = new ReadEnvelope();
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
				LoggingHelper.LogError( exc, "RegistryServices.GetDeleted. Using: " + serviceUri );
				statusMessage = exc.Message;
			}

			return list;
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

		/// <summary>
		/// To limit the search results to a single organization, provide the CTID of the organization. 
		/// </summary>
		/// <param name="owningOrganizationCTID"></param>
		/// <param name="where"></param>
		private static void SetOwningOrganizationFilters( string owningOrganizationCTID, ref string where )
		{
			string AND = "";
			if ( where.Length > 0 )
				AND = "&";

			if ( !string.IsNullOrWhiteSpace( owningOrganizationCTID ) )
			{
				where = where + AND + string.Format( "owned_by={0}", owningOrganizationCTID );
				AND = "&";
			}

		}
		/// <summary>
		/// To limit the search results to those by a third party publisher, provide the CTID of the publisher (typically done in the app.config. 
		/// NOTE: where the publisher and the owner are the same, there is no need to provide both the owning and publishing org filters, just pick one. 
		/// </summary>
		/// <param name="publishingOrganizationCTID"></param>
		/// <param name="where"></param>
		private static void SetPublishingOrganizationFilters( string publishingOrganizationCTID, ref string where )
		{
			string AND = "";
			if ( where.Length > 0 )
				AND = "&";

			if ( !string.IsNullOrWhiteSpace( publishingOrganizationCTID ) )
			{
				where = where + AND + string.Format( "published_by={0}", publishingOrganizationCTID );
				AND = "&";
			}

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

		public static List<ReadEnvelope> GraphSearchByTemplate( string queryFilePath, int skip, int take, ref int pTotalRows, ref string statusMessage, string community, string sortOrder = "asc" )
		{

			var jsonQuery = queryFilePath;
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
			apiQuery.UseBetaAPI = true;

			//Hold the response
			var results = new List<ReadEnvelope>();

			//...
			//Do the query
			SearchResponse rawResults = new SearchResponse();
			try
			{
				var jquery = JObject.Parse( jsonQuery );
				apiQuery.Query = jquery;
				rawResults = DoRegistrySearchAPIQuery( apiQuery );

				if ( !rawResults.valid )
				{
					statusMessage = rawResults.status;
					LoggingHelper.DoTrace(1, string.Format( "RegistryServices.Error Performing Search: {0}", rawResults.status ));
					return results;
				}
				pTotalRows = rawResults.extra.TotalResults;
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
					//unfortunately we need the envelope, so doing the extra get
					var envelope = GetEnvelopeByCtid( resource.CTID, ref statusMessage, ref ctdlType, community );
					if ( envelope == null || envelope.DecodedResource == null )
					{
						LoggingHelper.DoTrace( 1, $"{thisClassName}.GraphSearch. Envelope not found for CTID: {resource.CTID} " );

						continue;
					}
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
			try
			{
				//Format the request
				var queryJSON = JsonConvert.SerializeObject( query, new JsonSerializerSettings() { Formatting = Formatting.None, NullValueHandling = NullValueHandling.Ignore } );

				//Do the request
				using ( var client = new HttpClient() )
				{
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
						var message = BaseFactory.FormatExceptions( ex );
						response.status = "DoRegistrySearchAPIQuery. Error parsing response: " + message;
					}

					//Process the response
					if ( !rawResult.IsSuccessStatusCode )
					{
						response.valid = false;
						response.status = "DoRegistrySearchAPIQuery. Unsuccessful: " + rawResult.ReasonPhrase;
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
				}
			}
			catch ( Exception ex )
			{
				response.valid = false;
				var message = BaseFactory.FormatExceptions( ex );
				LoggingHelper.LogError( ex, $"{thisClassName}.DoRegistrySearchAPIQuery. Exception " );
				response.status = "DoRegistrySearchAPIQuery. Exception: " + message;
			}
		
			return response;
		}
		
		#endregion


		#region Registry Gets

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
            LoggingHelper.DoTrace( CodesManager.appDebuggingTraceLevel, $"{thisClassName}.GetEnvelope envelopeId: {envelopeId}, serviceUri: {serviceUri}" );
			
			return GetEnvelopeByURL( serviceUri, ref statusMessage, ref ctdlType );

		}
		public static ReadEnvelope GetEnvelopeByCtid( string ctid, ref string statusMessage, ref string ctdlType, string community = "" )
		{
			
			//need to pass in an override community - eventually
			if ( string.IsNullOrWhiteSpace( community ) )
				community = UtilityManager.GetAppKeyValue( "defaultCommunity" );
			ctid = ctid.Trim();
			if ( ctid.Length > 39 )
			{
				ctid = ctid.Substring( 0, 39 );
			}
			string serviceUri = GetEnvelopeUrl( ctid, community );

			return GetEnvelopeByURL( serviceUri, ref statusMessage, ref ctdlType );

		}

		public static ReadEnvelope GetEnvelopeByURL( string envelopeUrl, ref string statusMessage, ref string ctdlType )
		{
			string document = "";
			LoggingHelper.DoTrace( 7, string.Format( "RegistryServices.GetEnvelopeByURL envelopeUrl: {0} ", envelopeUrl ) );
			ReadEnvelope envelope = new ReadEnvelope();
			if (System.DateTime.Now.Day == 4  && UtilityManager.GetAppKeyValue( "environment" ) == "development")
            {
				//if (envelopeUrl.IndexOf( "credentialengineregistry.org" ) > 0)
    //            {
				//	envelopeUrl = envelopeUrl.Replace( "credentialengineregistry.org/ce-registry/envelopes", "credentialengine.org/assistant/envelopes" );
    //            }
            }
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
					var task = client.GetAsync( envelopeUrl );
					task.Wait();
					var getResponse = task.Result;
					document = task.Result.Content.ReadAsStringAsync().Result;
					if ( getResponse.IsSuccessStatusCode == false )
					{
						statusMessage = getResponse.StatusCode + ": " + document;
						return null;
					}
					//
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


			}
			catch ( Exception exc )
			{
				if ( exc.Message.IndexOf( "(404) Not Found" ) > 0 )
				{
					statusMessage = $"{thisClassName}.GetEnvelopeByURL. Not found for " + envelopeUrl;
					LoggingHelper.DoTrace( 1, statusMessage );
				}
				else
				{
					LoggingHelper.LogError( exc, $"{thisClassName}.GetEnvelopeByURL: " + envelopeUrl );
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

            LoggingHelper.DoTrace( 6, string.Format( "RegistryServices.ImportByCtid ctid: {0}, searchUrl: {1} ", ctid, searchUrl ) );
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
		/// Get a registry resource by URL
		/// NOTE: this only handles registry URLs. Need to reject non:/resources, /envelopes, /graph (ex: nocti pdfs)
		/// </summary>
		/// <param name="resourceUrl"></param>
		/// <param name="ctdlType"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public static string GetResourceByUrl( string resourceUrl, ref string ctdlType, ref string statusMessage)
		{
			string payload = "";
			statusMessage = "";
			ctdlType = "";
			if ( resourceUrl.ToLower().IndexOf( "credentialengineregistry.org/" ) == -1
				)
			{
				statusMessage = "Error: the provided URL is not for the credential registry: " + resourceUrl;
				return "";
			}
			try
			{
				using ( var client = new HttpClient() )
				{
					client.DefaultRequestHeaders.
						Accept.Add( new MediaTypeWithQualityHeaderValue( "application/json" ) );
					
					client.DefaultRequestHeaders.Add( "Authorization", "Token " + credentialEngineAPIKey );
				
					var task = client.GetAsync( resourceUrl );
					task.Wait();
					var response1 = task.Result;
					payload = task.Result.Content.ReadAsStringAsync().Result;

					//just in case, likely the caller knows the context
					if ( !string.IsNullOrWhiteSpace( payload )
							&& payload.Length > 100
							//&& payload.IndexOf("\"errors\":") == -1
							)
					{
						//this doesn't work for an envelope
						ctdlType = RegistryServices.GetResourceType( payload );
					}
					else
					{
						//nothing found, or error/not found
						if ( payload.IndexOf( "401 Unauthorized" ) > -1 )
						{
							LoggingHelper.DoTrace( 1, "RegistryServices.GetResourceByUrl. Not authorized to view: " + resourceUrl );
							statusMessage = "This organization is not authorized to view: " + resourceUrl;
						}
						else
						{
							LoggingHelper.DoTrace( 1, "RegistryServices.GetResourceByUrl. Did not find: " + resourceUrl );
							statusMessage = "The referenced resource was not found in the credential registry: " + resourceUrl;
						}
						payload = "";
					}
					//

				}
			}
			catch ( AggregateException ae )
			{
				//not sure if this is a red herring?
				var msg = LoggingHelper.FormatExceptions( ae );
				LoggingHelper.DoTrace( 1, thisClassName + string.Format( ".GetResourceByUrl AggregateException. URL: {0}, msg: {1}", resourceUrl, msg ) );
			}
			catch ( Exception exc )
			{
				if ( exc.Message.IndexOf( "(404) Not Found" ) > 0 )
				{
					//need to surface these better
					statusMessage = "The referenced resource was not found in the credential registry: " + resourceUrl;
				}
				else
				{
					var msg = LoggingHelper.FormatExceptions( exc );
					if ( msg.IndexOf( "remote name could not be resolved: 'sandbox.credentialengineregistry.org'" ) > 0 )
					{
						//retry?
						statusMessage = "retry";
					}
					else if ( msg.IndexOf( "The underlying connection was closed" ) > 0 )
					{
						//retry?
						statusMessage = "The underlying connection was closed: An unexpected error occurred on a send.";
					}
					else
					{
						LoggingHelper.LogError( exc, "RegistryServices.GetResourceByUrl: " + resourceUrl );
						statusMessage = exc.Message;
					}
				}
			}
			return payload;
		}

		public static string GetNonRegistryResourceByUrl( string resourceUrl, ref string ctdlType, ref string statusMessage )
		{
			string payload = "";
			statusMessage = "";
			ctdlType = "";
			//if ( resourceUrl.ToLower().IndexOf( "credentialengineregistry.org/" ) > -1
			//	)
			//{
			//	// no reason to reject
			//	statusMessage = "Error: the provided URL is not for the credential registry: " + resourceUrl;
			//	return "";
			//}
			try
			{
				using ( var client = new HttpClient() )
				{
					client.DefaultRequestHeaders.
						Accept.Add( new MediaTypeWithQualityHeaderValue( "application/json" ) );

					client.DefaultRequestHeaders.Add( "Authorization", "Token " + credentialEngineAPIKey );

					var task = client.GetAsync( resourceUrl );
					task.Wait();
					var response1 = task.Result;
					payload = task.Result.Content.ReadAsStringAsync().Result;

					//just in case, likely the caller knows the context
					if ( !string.IsNullOrWhiteSpace( payload )
							&& payload.Length > 100
							//&& payload.IndexOf("\"errors\":") == -1
							)
					{
						//this doesn't work for an envelope
						ctdlType = RegistryServices.GetResourceType( payload );
					}
					else
					{
						//nothing found, or error/not found
						if ( payload.IndexOf( "401 Unauthorized" ) > -1 )
						{
							LoggingHelper.DoTrace( 1, "RegistryServices.GetResourceByUrl. Not authorized to view: " + resourceUrl );
							statusMessage = "This organization is not authorized to view: " + resourceUrl;
						}
						else
						{
							LoggingHelper.DoTrace( 1, "RegistryServices.GetResourceByUrl. Did not find: " + resourceUrl );
							statusMessage = "The referenced resource was not found in the credential registry: " + resourceUrl;
						}
						payload = "";
					}
					//

				}
			}
			catch ( AggregateException ae )
			{
				//not sure if this is a red herring?
				var msg = LoggingHelper.FormatExceptions( ae );
				LoggingHelper.DoTrace( 1, thisClassName + string.Format( ".GetResourceByUrl AggregateException. URL: {0}, msg: {1}", resourceUrl, msg ) );
			}
			catch ( Exception exc )
			{
				if ( exc.Message.IndexOf( "(404) Not Found" ) > 0 )
				{
					//need to surface these better
					statusMessage = "The referenced resource was not found in the credential registry: " + resourceUrl;
				}
				else
				{
					var msg = LoggingHelper.FormatExceptions( exc );
					if ( msg.IndexOf( "remote name could not be resolved: 'sandbox.credentialengineregistry.org'" ) > 0 )
					{
						//retry?
						statusMessage = "retry";
					}
					else if ( msg.IndexOf( "The underlying connection was closed" ) > 0 )
					{
						//retry?
						statusMessage = "The underlying connection was closed: An unexpected error occurred on a send.";
					}
					else
					{
						LoggingHelper.LogError( exc, "RegistryServices.GetResourceByUrl: " + resourceUrl );
						statusMessage = exc.Message;
					}
				}
			}
			return payload;
		}

		///// <summary>
		///// Retrieve a resource from the registry by resourceId
		///// </summary>
		///// <param name="resourceId">Url to a resource in the registry</param>
		///// <param name="statusMessage"></param>
		///// <returns></returns>
		////[Obsolete]
		//public static string GetResourceByUrl( string resourceUrl, ref string ctdlType, ref string statusMessage )
		//      {
		//          string payload = "";
		//          //NOTE - getting by ctid means no envelopeid
		//          try
		//          {
		//              // Create a request for the URL.         
		//              WebRequest request = WebRequest.Create( resourceUrl );

		//              // If required by the server, set the credentials.
		//              request.Credentials = CredentialCache.DefaultCredentials;
		//		var hdr = new WebHeaderCollection
		//		{
		//			{ "Authorization", "Token  " + credentialEngineAPIKey }
		//		};
		//		request.Headers.Add( hdr );

		//		//Get the response.
		//		HttpWebResponse response = ( HttpWebResponse )request.GetResponse();

		//              // Get the stream containing content returned by the server.
		//              Stream dataStream = response.GetResponseStream();

		//              // Open the stream using a StreamReader for easy access.
		//              StreamReader reader = new StreamReader( dataStream );
		//              // Read the content.
		//              payload = reader.ReadToEnd();

		//              // Cleanup the streams and the response.

		//              reader.Close();
		//              dataStream.Close();
		//              response.Close();

		//              ctdlType = RegistryServices.GetResourceType( payload );
		//          }
		//          catch ( Exception exc )
		//          {
		//              if ( exc.Message.IndexOf( "(404) Not Found" ) > 0 )
		//              {
		//                  //need to surface these better
		//                  statusMessage = "ERROR - resource was not found in registry: " + resourceUrl;
		//              }
		//              else
		//              {
		//                  LoggingHelper.LogError( exc, "RegistryServices.GetResourceByUrl" );
		//                  statusMessage = exc.Message;
		//              }
		//          }
		//          return payload;
		//      }

		public static string GetCtidFromUnknownEnvelope( ReadEnvelope item )
        {
            string ctid = "";
            //string envelopeId = "";
            try
            {
                RegistryObject ro = new RegistryObject( item.DecodedResource.ToString() );
                ctid = ro.CTID;

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

		/// <summary>
		/// this method can handle a graph,resource or an envelope
		/// </summary>
		/// <param name="payload"></param>
		/// <param name="removeNameSpace"></param>
		/// <returns></returns>
        public static string GetResourceType( string payload, bool removeNameSpace = false )
        {
            string ctdlType = "";
            RegistryObject ro = new RegistryObject( payload );
            ctdlType = ro.CtdlType;
			if ( removeNameSpace )
			{
				ctdlType = ctdlType.Replace( "ceterms:", "" );
				ctdlType = ctdlType.Replace( "ceasn:", "" );
				ctdlType = ctdlType.Replace( "asn:", "" );
			}
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

		public static string GetFirstItemValue( LanguageMap map )
		{
			if ( map == null || map.Count == 0 )
				return "";
			string output = "";
			foreach ( var item in map )
			{
				output = item.Value;
				break;
			}

			return output;
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

		#region Purge methods
		public bool PurgeRequest( string CTID, ref string dataOwnerCtid, ref string entityType, ref string message, string community = "" )
		{
			//needs owner
			var entity = EntityManager.EntityCacheGetByCTIDWithOrganization( CTID );
			if (entity == null || entity.Id == 0)
			{
				message = "Error: a valid CTID must be provided for this function. ";
				return false;
            }
            if ( string.IsNullOrWhiteSpace( entity.OwningOrgCTID ))
            {
                //hmm this could mean the owner has been deleted?
                //	unlikely outside of the sandbox. Should be able to use CE? or some override parameter.
                entity.OwningOrgCTID = UtilityManager.GetAppKeyValue( "credentialEngineCTID" );
				//may want to log this or return a warning?
				//message = $"Error: An owning organization is not associated with the CTID ({CTID}) in the Entity.Cache table. The purge request needs to included the correct owner.";
				//return false;
            }
			entityType = entity.EntityType;
			dataOwnerCtid = entity.OwningOrgCTID;
            var CTIDList = new List<string>() { CTID };
			return PurgeRequest( CTIDList, entity.OwningOrgCTID, entity.EntityType, ref message, community );
		}

		public bool PurgeRequest( List<string> CTIDList, string dataOwnerCtid, string entityType, ref string message, string community = "" )
		{
			var raResponse = new RegistryAssistantResponse();
			//"https://sandbox.credentialengine.org/assistant/"
			string serviceUri = UtilityManager.GetAppKeyValue( "registryAssistantApi" );
			if ( DateTime.Now.Day == 01 && UtilityManager.GetAppKeyValue( "environment" ) == "development" )
			{
				//serviceUri = "https://localhost:44312/";
			}
            if ( entityType.ToLower() == "course" || entityType.ToLower() == "learningprogram" )
                entityType = "learningopportunity";
            else if ( entityType.ToLower() == "credentialorganization" || entityType.ToLower() == "qacredentialorganization" )
                entityType = "organization";

            //might use one delete endpoint, as adding code to handle this.
            //check if the single endpoint is viable to simplify
            string endpointUrl = serviceUri + "admin/purge";

			//RAResponse response = new RAResponse();
			string credentialEngineCTID = UtilityManager.GetAppKeyValue( "credentialEngineCTID" );
			string apiPublisherIdentifier = UtilityManager.GetAppKeyValue( "MyCredentialEngineAPIKey" );
			DeleteRequest dr = new DeleteRequest()
			{
				CTIDList = CTIDList,
				PublishForOrganizationIdentifier = credentialEngineCTID
			};
			dr.Community = GetCommunity( community );
			var label = "";
			if ( CTIDList.Count == 1 )
				label = CTIDList[0];
			else
				label = string.Format( "Request for {0} CTIDs.", CTIDList.Count );

			foreach (var ctid in CTIDList)
            {
				//format the payload
				string postBody = JsonConvert.SerializeObject( dr, JsonHelper.GetJsonSettings() );
				try
				{
					using ( var client = new System.Net.Http.HttpClient() )
					{
						client.DefaultRequestHeaders.
							Accept.Add( new MediaTypeWithQualityHeaderValue( "application/json" ) );

						//if ( UtilityManager.GetAppKeyValue( "environment" ) == "development" )
						//{
						client.Timeout = new TimeSpan( 0, 30, 0 );
						//}

						if ( !string.IsNullOrWhiteSpace( apiPublisherIdentifier ) )
						{
							client.DefaultRequestHeaders.Add( "Authorization", "ApiToken " + apiPublisherIdentifier );
						}

						HttpRequestMessage hrm = new HttpRequestMessage
						{
							Content = new StringContent( postBody, Encoding.UTF8, "application/json" ),
							Method = HttpMethod.Delete,
							RequestUri = new Uri( endpointUrl )
						};
						var task = client.SendAsync( hrm );
						task.Wait();
						var result = task.Result;
						string response = JsonConvert.SerializeObject( result );
						var contents = task.Result.Content.ReadAsStringAsync().Result;
						//
						if ( result.IsSuccessStatusCode == false )
						{
							//logging???
							//response = contents.Result;
							LoggingHelper.LogError( "RegistryServices.PurgeRequest Failed\n\r" + response + "\n\rError: " + JsonConvert.SerializeObject( contents ) );

							RegistryResponseContent contentsJson = JsonConvert.DeserializeObject<RegistryResponseContent>( contents );
							message = string.Join( "<br/>", contentsJson.Errors.ToArray() );
						}
						else
						{
							raResponse = JsonConvert.DeserializeObject<RegistryAssistantResponse>( contents );
							//note for list, could be false even if one fails.
							if ( raResponse.Successful )
							{
								LoggingHelper.DoTrace( 5, string.Format( "PurgeRequest sucessful for requestType:{0}.  CTID: {1}, dataOwnerCtid: {2} ", entityType, ctid, dataOwnerCtid ) );
							}
							else
							{
								//message = string.Join("", raResponse.Messages );
								message += string.Join( ",", raResponse.Messages.ToArray() ) + "; ";
								//this will be displayed by delete step
								LoggingHelper.DoTrace( 1, thisClassName + " PurgeRequest FAILED. result: " + message );

								//return false;
							}

						}
						//return result.IsSuccessStatusCode;
					}
				}
				catch ( Exception exc )
				{
					LoggingHelper.LogError( exc, string.Format( "PurgeRequest. RequestType:{0}, CTID: {1}", entityType, label ) );
					message = LoggingHelper.FormatExceptions( exc );

					return false;

				}
			}
			if ( message.Length > 0 )
				return false;
			else
				return true;
		}

		public static string GetCommunity( string community = "" )
		{
			//var community = "";
			if ( string.IsNullOrWhiteSpace( community ) )
			{
				var requestedCommunity = UtilityManager.GetAppKeyValue( "requestedCommunity", "" );
				if ( !string.IsNullOrWhiteSpace( requestedCommunity ) &&
					UtilityManager.GetAppKeyValue( "defaultCommunity", "" ) != requestedCommunity )
					community = requestedCommunity;
			}
			return community;
		}

		#endregion

		#region Delete methods
		public bool DeleteRequest( string CTID, ref string message, ref string entityType, string community = "" )
		{
            //needs owner
            var entity = EntityManager.EntityCacheGetByCTIDWithOrganization( CTID );
            if ( entity == null || entity.Id == 0 )
            {
                message = "Error: a valid CTID must be provided for this function. ";
                return false;
            }
            if ( string.IsNullOrWhiteSpace( entity.OwningOrgCTID ) )
            {
                //hmm this could mean the owner has been deleted?
                //	unlikely outside of the sandbox. Should be able to use CE? or some override parameter.
                entity.OwningOrgCTID = UtilityManager.GetAppKeyValue( "credentialEngineCTID" );
                //may want to log this or return a warning?
                //message = $"Error: An owning organization is not associated with the CTID ({CTID}) in the Entity.Cache table. The purge request needs to included the correct owner.";
                //return false;
            }
            entityType = entity.EntityType;

            var CTIDList = new List<string>() { CTID };
			return DeleteRequest( CTIDList, entity.OwningOrgCTID, entity.EntityType, ref message, community );
		}

		public bool DeleteRequest( List<string> CTIDList, string dataOwnerCtid, string entityType, ref string message, string community = "" )
		{
			var raResponse = new RegistryAssistantResponse();
			//"https://sandbox.credentialengine.org/assistant/"
			string serviceUri = UtilityManager.GetAppKeyValue( "registryAssistantApi" );
			if ( DateTime.Now.Day == 08 && UtilityManager.GetAppKeyValue( "environment" ) == "development" )
			{
				//serviceUri = "https://localhost:44312/";
			}
			if ( entityType.ToLower() == "course" || entityType.ToLower() == "learningprogram" )
				entityType = "learningopportunity";
			else if ( entityType.ToLower() == "credentialorganization" || entityType.ToLower() == "qacredentialorganization" )
                entityType = "organization";
            //might use one delete endpoint, as adding code to handle this.
            //check if the single endpoint is viable to simplify
            string endpointUrl = serviceUri + entityType + "/Delete";

			//RAResponse response = new RAResponse();
			string credentialEngineCTID = UtilityManager.GetAppKeyValue( "credentialEngineCTID" );
			string apiPublisherIdentifier = UtilityManager.GetAppKeyValue( "MyCredentialEngineAPIKey" );
			//specific methods like lopp/delete don't check for a list
			DeleteRequest dr = new DeleteRequest()
			{
				//CTIDList = CTIDList,
				PublishForOrganizationIdentifier = credentialEngineCTID
			};
			dr.Community = GetCommunity( community );
			var label = "";
			if ( CTIDList.Count == 1 )
				label = CTIDList[0];
			else
				label = string.Format( "Request for {0} CTIDs.", CTIDList.Count );

			foreach ( var ctid in CTIDList )
			{
				dr = new DeleteRequest()
				{
					CTID = ctid,
					PublishForOrganizationIdentifier = credentialEngineCTID
				};
				//format the payload
				string postBody = JsonConvert.SerializeObject( dr, JsonHelper.GetJsonSettings() );
				try
				{
					using ( var client = new System.Net.Http.HttpClient() )
					{
						client.DefaultRequestHeaders.
							Accept.Add( new MediaTypeWithQualityHeaderValue( "application/json" ) );

						//if ( UtilityManager.GetAppKeyValue( "environment" ) == "development" )
						//{
						client.Timeout = new TimeSpan( 0, 30, 0 );
						//}

						if ( !string.IsNullOrWhiteSpace( apiPublisherIdentifier ) )
						{
							client.DefaultRequestHeaders.Add( "Authorization", "ApiToken " + apiPublisherIdentifier );
						}

						HttpRequestMessage hrm = new HttpRequestMessage
						{
							Content = new StringContent( postBody, Encoding.UTF8, "application/json" ),
							Method = HttpMethod.Delete,
							RequestUri = new Uri( endpointUrl )
						};
						var task = client.SendAsync( hrm );
						task.Wait();
						var result = task.Result;
						string response = JsonConvert.SerializeObject( result );
						var contents = task.Result.Content.ReadAsStringAsync().Result;
						//
						if ( result.IsSuccessStatusCode == false )
						{
							//logging???
							//response = contents.Result;
							LoggingHelper.LogError( "RegistryServices.DeleteRequest Failed\n\r" + response + "\n\rError: " + JsonConvert.SerializeObject( contents ) );

							RegistryResponseContent contentsJson = JsonConvert.DeserializeObject<RegistryResponseContent>( contents );
							message = string.Join( "<br/>", contentsJson.Errors.ToArray() );
						}
						else
						{
							raResponse = JsonConvert.DeserializeObject<RegistryAssistantResponse>( contents );
							//note for list, could be false even if one fails.
							if ( raResponse.Successful )
							{
								LoggingHelper.DoTrace( 5, string.Format( "DeleteRequest sucessful for requestType:{0}.  CTID: {1}, dataOwnerCtid: {2} ", entityType, ctid, dataOwnerCtid ) );
							}
							else
							{
								//message = string.Join("", raResponse.Messages );
								message += string.Join( ",", raResponse.Messages.ToArray() ) + "; ";
								//this will be displayed by delete step
								LoggingHelper.DoTrace( 1, thisClassName + " DeleteRequest FAILED. result: " + message );

								//return false;
							}

						}
						//return result.IsSuccessStatusCode;
					}
				}
				catch ( Exception exc )
				{
					LoggingHelper.LogError( exc, string.Format( "DeleteRequest. RequestType:{0}, CTID: {1}", entityType, label ) );
					message = LoggingHelper.FormatExceptions( exc );

					return false;

				}
			}
			if ( message.Length > 0 )
				return false;
			else
				return true;
		}


		#endregion


		#region Verify methods
		/// <summary>
		/// Request to set the last_verified_on date to today.
		/// </summary>
		/// <param name="CTID"></param>
		/// <param name="message"></param>
		/// <param name="entityType"></param>
		/// <param name="community"></param>
		/// <returns></returns>
		public bool SetVerifiedRequest( string CTID, ref string message, ref string entityType, string community = "" )
		{
			//needs owner
			var entity = EntityManager.EntityCacheGetByCTIDWithOrganization( CTID );
			if ( entity == null || entity.Id == 0 )
			{
				message = "Error: a valid CTID must be provided for this function. ";
				return false;
			}
			if ( string.IsNullOrWhiteSpace( entity.OwningOrgCTID ) )
			{
				//hmm this could mean the owner has been deleted?
				//	unlikely outside of the sandbox. Should be able to use CE? or some override parameter.
				entity.OwningOrgCTID = UtilityManager.GetAppKeyValue( "credentialEngineCTID" );
			}
			entityType = entity.EntityType;

			var CTIDList = new List<string>() { CTID };
			return SetVerifiedRequest( CTIDList, entity.OwningOrgCTID, entity.EntityType, ref message, community );
		}

		/// <summary>
		/// Request to set the last_verified_on date to today for a list of resources
		/// </summary>
		/// <param name="CTIDList"></param>
		/// <param name="dataOwnerCtid"></param>
		/// <param name="entityType"></param>
		/// <param name="message"></param>
		/// <param name="community"></param>
		/// <returns></returns>
		public bool SetVerifiedRequest( List<string> CTIDList, string dataOwnerCtid, string entityType, ref string message, string community = "" )
		{
			var raResponse = new ValidateResponse();
			//
			string serviceUri = UtilityManager.GetAppKeyValue( "registryAssistantApi" );
			if ( DateTime.Now.Day == 08 && UtilityManager.GetAppKeyValue( "environment" ) == "development" )
			{
				//serviceUri = "https://localhost:44312/";
			}
	
			//might use one delete endpoint, as adding code to handle this.
			//check if the single endpoint is viable to simplify
			string endpointUrl = serviceUri + "manage/VerifyResource";

			//RAResponse response = new RAResponse();
			string credentialEngineCTID = UtilityManager.GetAppKeyValue( "credentialEngineCTID" );
			string apiPublisherIdentifier = UtilityManager.GetAppKeyValue( "MyCredentialEngineAPIKey" );
			//
			var dr = new VerifyRequest()
			{
				//CTIDList = CTIDList,
				PublishForOrganizationIdentifier = dataOwnerCtid
			};
			community = GetCommunity( community );
			var label = "";
			if ( CTIDList.Count == 1 )
				label = CTIDList[0];
			else
				label = string.Format( "Request for {0} CTIDs.", CTIDList.Count );

			foreach ( var ctid in CTIDList )
			{
				dr = new VerifyRequest()
				{
					CTID = ctid,
					PublishForOrganizationIdentifier = dataOwnerCtid,
					Community = community,
				};
				//format the payload
				string postBody = JsonConvert.SerializeObject( dr, JsonHelper.GetJsonSettings() );
				try
				{
					using ( var client = new System.Net.Http.HttpClient() )
					{
						client.DefaultRequestHeaders.
							Accept.Add( new MediaTypeWithQualityHeaderValue( "application/json" ) );
						client.Timeout = new TimeSpan( 0, 30, 0 );
						client.DefaultRequestHeaders.Add( "Authorization", "ApiToken " + apiPublisherIdentifier );

						var task = client.PostAsync( endpointUrl, new StringContent( postBody, Encoding.UTF8, "application/json" ) );
						task.Wait();
						var result = task.Result;
						var responseContents = task.Result.Content.ReadAsStringAsync().Result;
						//
						if ( result.IsSuccessStatusCode == false )
						{
							var contentsJson = JsonConvert.DeserializeObject<ValidateResponse>( responseContents );
							message = string.Join( "\n", contentsJson.Messages.ToArray() );
						}
						else
						{
							raResponse = JsonConvert.DeserializeObject<ValidateResponse>( responseContents );
							//note for list, could be false even if one fails.
							if ( raResponse.Successful )
							{
								LoggingHelper.DoTrace( 5, string.Format( "SetVerifiedRequest successful for requestType:{0}.  CTID: {1}, dataOwnerCtid: {2} ", entityType, ctid, dataOwnerCtid ) );
							}
							else
							{
								//message = string.Join("", raResponse.Messages );
								message += string.Join( ",", raResponse.Messages.ToArray() ) + "; ";
								//this will be displayed by delete step
								LoggingHelper.DoTrace( 1, thisClassName + " SetVerifiedRequest FAILED. result: " + message );

								//return false;
							}

						}
					}
				}
				catch ( Exception exc )
				{
					LoggingHelper.LogError( exc, string.Format( "SetVerifiedRequest. RequestType:{0}, CTID: {1}", entityType, label ) );
					message = LoggingHelper.FormatExceptions( exc );

					return false;

				}
			}
			if ( message.Length > 0 )
				return false;
			else
				return true;
		}


		#endregion

		public bool SetOrganizationToCeased( string ctid, ref string message, string community = "" )
		{
			var raResponse = new RegistryAssistantResponse();
			//"https://sandbox.credentialengine.org/assistant/"
			string serviceUri = UtilityManager.GetAppKeyValue( "registryAssistantApi" );
			if ( DateTime.Now.Day == 08 && UtilityManager.GetAppKeyValue( "environment" ) == "development" )
			{
				//serviceUri = "https://localhost:44312/";
			}


			//might use one delete endpoint, as adding code to handle this.
			//check if the single endpoint is viable to simplify
			string endpointUrl = serviceUri + "organization/cease";

			//RAResponse response = new RAResponse();
			string credentialEngineCTID = UtilityManager.GetAppKeyValue( "credentialEngineCTID" );
			string apiPublisherIdentifier = UtilityManager.GetAppKeyValue( "MyCredentialEngineAPIKey" );
			DeleteRequest dr = new DeleteRequest()
			{
				CTID = ctid,
				PublishForOrganizationIdentifier = credentialEngineCTID
			};
			dr.Community = GetCommunity( community );
			//var label = string.Format( "Request for {0} CTIDs.", CTIDList.Count );

			
			//format the payload
			string postBody = JsonConvert.SerializeObject( dr, JsonHelper.GetJsonSettings() );
			try
			{
				using ( var client = new System.Net.Http.HttpClient() )
				{
					client.DefaultRequestHeaders.
						Accept.Add( new MediaTypeWithQualityHeaderValue( "application/json" ) );
					client.Timeout = new TimeSpan( 0, 30, 0 );

					if ( !string.IsNullOrWhiteSpace( apiPublisherIdentifier ) )
					{
						client.DefaultRequestHeaders.Add( "Authorization", "ApiToken " + apiPublisherIdentifier );
					}

					HttpRequestMessage hrm = new HttpRequestMessage
					{
						Content = new StringContent( postBody, Encoding.UTF8, "application/json" ),
						Method = HttpMethod.Post,
						RequestUri = new Uri( endpointUrl )
					};
					var task = client.SendAsync( hrm );
					task.Wait();
					var result = task.Result;
					string response = JsonConvert.SerializeObject( result );
					var contents = task.Result.Content.ReadAsStringAsync().Result;
					//
					if ( result.IsSuccessStatusCode == false )
					{
						//logging???
						//response = contents.Result;
						LoggingHelper.LogError( "RegistryServices.PurgeRequest Failed\n\r" + response + "\n\rError: " + JsonConvert.SerializeObject( contents ) );

						RegistryResponseContent contentsJson = JsonConvert.DeserializeObject<RegistryResponseContent>( contents );
						message = string.Join( "<br/>", contentsJson.Errors.ToArray() );
					}
					else
					{
						raResponse = JsonConvert.DeserializeObject<RegistryAssistantResponse>( contents );
						//note for list, could be false even if one fails.
						if ( raResponse.Successful )
						{
							LoggingHelper.DoTrace( 5, string.Format( "SetOrganizationToCeased sucessful for:  CTID: {0}. ", ctid) );
						}
						else
						{
							//message = string.Join("", raResponse.Messages );
							message += string.Join( ",", raResponse.Messages.ToArray() ) + "; ";
							//this will be displayed by delete step
							LoggingHelper.DoTrace( 1, thisClassName + " SetOrganizationToCeased FAILED. result: " + message );

							return false;
						}

					}
					return result.IsSuccessStatusCode;
				}
			}
			catch ( Exception exc )
			{
				LoggingHelper.LogError( exc, string.Format( "SetOrganizationToCeased.  CTID: {0}", ctid ) );
				message = LoggingHelper.FormatExceptions( exc );

				return false;

			}

		}

	}


	public class RegistryObject
    {
        public RegistryObject( string payload )
        {
			if ( !string.IsNullOrWhiteSpace( payload ) )
			{
				dictionary = RegistryServices.JsonToDictionary( payload );
				//handle envelope
				if ( payload.IndexOf( "decoded_resource" ) > 0 )
				{
					object ctdlType = dictionary["envelope_ctdl_type"];
					CtdlType = ctdlType.ToString();
					object decoded_resource = dictionary["decoded_resource"];
					var drlist = JsonConvert.SerializeObject( decoded_resource );
					var drDictionary = RegistryServices.JsonToDictionary( drlist );
					object graph = drDictionary["@graph"];
					//serialize the graph object
					var glist = JsonConvert.SerializeObject( graph );
					//parse graph in to list of objects
					JArray graphList = JArray.Parse( glist );

					var main = graphList[0].ToString();
					BaseObject = JsonConvert.DeserializeObject<RegistryBaseObject>( main );
					CtdlType = BaseObject.CdtlType;
					CTID = BaseObject.Ctid;
					CtdlId = BaseObject.CtdlId;
					//not important to fully resolve yet
					if ( BaseObject.Name != null )
					{
						//Name = BaseObject.Name.ToString();
						Name = RegistryServices.GetFirstItemValue( BaseObject.Name );
					}
					else if ( CtdlType == "ceasn:CompetencyFramework" || CtdlType == "asn:ProgressionModel" || CtdlType == "skos:ConceptScheme" )
					{
						if ( BaseObject.CeasnName != null )
						{
							//Name = BaseObject.Name.ToString();
							Name = RegistryServices.GetFirstItemValue( BaseObject.CeasnName );
							//Name = ( BaseObject.CompetencyFrameworkName ?? "" ).ToString();
						}
					}
					else if ( CtdlType == "skos:Concept" || CtdlType == "asn:ProgressionLevel" )
					{
						if ( BaseObject.PrefLabel != null )
						{
							Name = RegistryServices.GetFirstItemValue( BaseObject.PrefLabel );
						}
					}
					else if ( CtdlType == "ceasn:Competency" )
					{
						if ( BaseObject.CompetencyText != null )
						{
							Name = RegistryServices.GetFirstItemValue( BaseObject.CompetencyText );
						}
					}
					else
					{
						//object graph = dictionary["@graph"];
						//var glist = JsonConvert.SerializeObject( decoded_resource );
						Name = "Not handled for an Envelope yet.";
					}


				}
				else if ( payload.IndexOf( "@graph" ) > 0 && payload.IndexOf( "@graph\": null" ) == -1 )
				{
					IsGraphObject = true;
					//get the graph object
					object graph = dictionary["@graph"];
					//serialize the graph object
					var glist = JsonConvert.SerializeObject( graph );
					//parse graph in to list of objects
					JArray graphList = JArray.Parse( glist );

					var main = graphList[0].ToString();
					BaseObject = JsonConvert.DeserializeObject<RegistryBaseObject>( main );
					CtdlType = BaseObject.CdtlType;
					CTID = BaseObject.Ctid;
					CtdlId = BaseObject.CtdlId;
					//not important to fully resolve yet
					if ( BaseObject.Name != null )
					{
						if ( BaseObject.Name is LanguageMap )
						{
							Name = RegistryServices.GetFirstItemValue( BaseObject.Name );
						}
						else
						{
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
					}
					else if ( CtdlType == "ceasn:CompetencyFramework" || CtdlType == "asn:ProgressionModel" || CtdlType == "skos:ConceptScheme" )
					{
						if ( BaseObject.CeasnName is LanguageMap )
						{
							Name = RegistryServices.GetFirstItemValue( BaseObject.CeasnName );

						}
						else
						{
							Name = BaseObject.CeasnName.ToString();
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
					}
					else if ( CtdlType == "skos:Concept" || CtdlType == "asn:ProgressionLevel" )
					{
						if ( BaseObject.PrefLabel != null )
						{
							Name = RegistryServices.GetFirstItemValue( BaseObject.PrefLabel );
						}
					}
					else if ( CtdlType == "ceasn:Competency" )
					{
						if ( BaseObject.CompetencyText != null )
						{
							Name = RegistryServices.GetFirstItemValue( BaseObject.CompetencyText );
						}
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
					CTID = BaseObject.Ctid;
					CtdlId = BaseObject.CtdlId;
					Name = BaseObject.Name != null ? BaseObject.Name.ToString() : "";
					if ( BaseObject.Name != null )
					{
						//Name = BaseObject.Name.ToString();
						Name = RegistryServices.GetFirstItemValue( BaseObject.Name );
						Description = RegistryServices.GetFirstItemValue( BaseObject.Description );

					}
					else if ( CtdlType == "ceasn:CompetencyFramework" || CtdlType == "asn:ProgressionModel" || CtdlType == "skos:ConceptScheme" )
					{
						if ( BaseObject.CeasnName != null )
						{
							Name = RegistryServices.GetFirstItemValue( BaseObject.CeasnName );
						}
						if ( BaseObject.CeasnDescription != null )
						{
							Description = RegistryServices.GetFirstItemValue( BaseObject.CeasnDescription );
						}
					}
					else if ( CtdlType == "skos:Concept" || CtdlType == "asn:ProgressionLevel" )
					{
						if ( BaseObject.PrefLabel != null )
						{
							Name = RegistryServices.GetFirstItemValue( BaseObject.PrefLabel );
						}
						if ( BaseObject.Definition != null )
						{
							Description = RegistryServices.GetFirstItemValue( BaseObject.Definition );
						}
					}
					else if ( CtdlType == "ceasn:Competency" )
					{
						if ( BaseObject.CompetencyText != null )
						{
							Name = RegistryServices.GetFirstItemValue( BaseObject.CompetencyText );
						}
					}
					else if ( CtdlType == "ceasn:Rubric" || CtdlType == "ceasn:RubricCriterion" || CtdlType == "ceasn:RubricLevel" )
					{
						if ( BaseObject.CeasnName != null )
						{
							Name = RegistryServices.GetFirstItemValue( BaseObject.CeasnName );
						}
					}
					else if ( CtdlType == "ceasn:CriterionLevel" )
					{
						//for now, may not really need this
						Name = "CriterionLevel";
					}
					else
					{

					}

				}
				if ( !string.IsNullOrWhiteSpace( CtdlType ) )
				{
					CtdlType = CtdlType.Replace( "ceterms:", "" );
					CtdlType = CtdlType.Replace( "ceasn:", "" );
					CtdlType = CtdlType.Replace( "asn:", "" );
					CtdlType = CtdlType.Replace( "skos:", "" );

				}
			}
        }

        Dictionary<string, object> dictionary = new Dictionary<string, object>();

        public bool IsGraphObject { get; set; }
        public RegistryBaseObject BaseObject { get; set; } = new RegistryBaseObject();
        public string CtdlType { get; set; } = "";
		//this will be significany in blank nodes as the alternative CTID
        public string CtdlId { get; set; } = "";
        public string CTID { get; set; } = "";
        public string Name { get; set;  }
		public string Description { get; set; } = "";
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
        public LanguageMap Name { get; set; }

		[JsonProperty( PropertyName = "ceterms:description" )]
		public LanguageMap Description { get; set; }

		[JsonProperty( PropertyName = "ceasn:name" )]
		public LanguageMap CeasnName { get; set; }

		[JsonProperty( PropertyName = "ceasn:description" )]
		public LanguageMap CeasnDescription { get; set; }

		[JsonProperty( PropertyName = "skos:prefLabel" )]
		public LanguageMap PrefLabel { get; set; }
		[JsonProperty( PropertyName = "skos:definition" )]
		public LanguageMap Definition { get; set; }

		[JsonProperty( PropertyName = "ceasn:CompetencyText" )]
		public LanguageMap CompetencyText { get; set; }

		//22-02-12 mp - this will now be a problem with Collection having a list of SWP
		[JsonProperty( PropertyName = "ceterms:subjectWebpage" )]
		//public string SubjectWebpage { get; set; }
		public object SubjectWebpage { get; set; }
	}

	public class VerifyRequest
	{
		/// <summary>
		/// Identifier for Organization which Owns the data being verified
		/// </summary>
		public string PublishForOrganizationIdentifier { get; set; }

		/// <summary>
		/// The CTID of the resource to be verified.
		/// </summary>
		public string CTID { get; set; }

		/// <summary>
		/// The community/private registry where the resource to be verified is located.
		/// Optional. If not provided, the default registry will be used. 
		/// </summary>
		public string Community { get; set; }
	}

	public class RegistryAssistantDeleteResponse
	{
		public RegistryAssistantDeleteResponse()
		{
			Messages = new List<string>();
		}
		/// <summary>
		/// True if delete was successfull, otherwise false
		/// </summary>
		public bool Successful { get; set; }

		/// <summary>
		/// List of error or warning messages
		/// </summary>
		public List<string> Messages { get; set; }

	}
	public class ValidateResponse : RegistryAssistantDeleteResponse
	{

	}
}

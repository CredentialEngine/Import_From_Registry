using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using workIT.Utilities;
using workIT.Models.API.RegistrySearchAPI;


namespace workIT.Services
{
	public class RegistryServicesV2
	{
		public static RawRegistryResponse GetDataByCTID( string ctid, bool asGraphData = false )
		{
			return MakeDirectRegistryRequest( ( asGraphData ? "graph" : "resources" ) + "/" + ctid, false, true );
		}
		public static RawRegistryResponse GetDataByCTID( List<string> ctids, bool asGraphData = false )
		{
			//Setup the request
			var requestURLPartAfterDomain = ( asGraphData ? "graph" : "resources" ) + "/search";
			var requestData = new JObject()
			{
				{ "ctids", JArray.FromObject( ctids ) }
			};

			//Make the request
			var result = MakeDirectRegistryRequest( requestURLPartAfterDomain, true, true, requestData.ToString( Formatting.None ) );

			//Return the result
			return result;
		}
		//

		public static DetailPageDescriptionSet GetDetailPageDescriptionSet( string ctid, bool includeRelatedResources = true, int perBranchLimit = 10, bool includeGraphData = true, bool includeMetadata = true )
		{
			var result = new DetailPageDescriptionSet();

			//Get the description set
			var requestData = new JObject()
			{
				{ "ctids", JArray.FromObject( new List<string>() { ctid } ) },
				{ "include_resources", includeRelatedResources },
				{ "include_graph_data", includeGraphData },
				{ "include_results_metadata", includeMetadata }
			};

			//If -1, don't limit the number of URLs returned
			if( perBranchLimit > -1 )
			{
				requestData.Add( "per_branch_limit", perBranchLimit );
			}

			//Do the request
			var descriptionSetData = MakeDirectRegistryRequest( "description_sets", true, true, requestData.ToString( Formatting.None ) );
			if ( !descriptionSetData.Successful )
			{
				result.DebugInfo.Add( "Error getting description set data", descriptionSetData.DebugInfo );
				return result;
			}

			//Process the data
			try
			{
				//Resource
				if( descriptionSetData.RawData["data"] != null )
				{
					result.Resource = ( ( JArray ) descriptionSetData.RawData[ "data" ] ).Select( m => ( JObject ) m ).FirstOrDefault();
				}

				//Related Items
				if ( descriptionSetData.RawData[ "description_set_resources" ] != null )
				{
					result.RelatedItems = ( ( JArray ) descriptionSetData.RawData[ "description_set_resources" ] ).Select( m => ( JObject ) m ).ToList();
				}

				//Related Items Map
				foreach ( JObject item in ( JArray ) descriptionSetData.RawData[ "description_sets" ] )
				{
					foreach ( JObject subItem in ( JArray ) item[ "description_set" ] )
					{
						var path = new RelatedItemsPath()
						{
							Path = subItem[ "path" ].ToString(),
							TotalURIs = int.Parse( subItem[ "total" ].ToString() ),
							URIs = ( ( JArray ) subItem[ "uris" ] ).Select( m => m.ToString() ).ToList()
						};

						result.RelatedItemsMap.Add( path );
					}
				}

				//Metadata
				var resultMetadata = descriptionSetData.RawData[ "results_metadata" ]?.Where( m => m[ "resource_uri" ]?.ToString() == ctid ).Select( m => ( JObject ) m ).FirstOrDefault();
				if( resultMetadata != null )
				{
					result.Metadata = new JObject()
					{
						{ "DateCreated", resultMetadata["created_at"] },
						{ "DateModified", resultMetadata["updated_at"] },
						{ "RecordOwnedBy", resultMetadata["owned_by"] },
						{ "RecordPublishedBy", resultMetadata["published_by"] ?? resultMetadata["owned_by"] }
					};
				}
			}
			catch (Exception ex )
			{
				result.DebugInfo.Add( "Error converting description set data", new JObject()
				{
					{ "Exception", ex.Message },
					{ "Raw Data", descriptionSetData.RawData }
				} );
			}

			//Return the data
			return result;
		}
		//

		public static ComposedSearchResultSet GetDescriptionSetsByCTID( List<string> ctids, bool includeRelatedResources = false, int perBranchLimit = 10, bool includeGraphData = false )
		{
			//Hold the response
			var result = new ComposedSearchResultSet();

			//Get the resources
			var resourcesData = GetDataByCTID( ctids, false );
			if( !resourcesData.Successful )
			{
				result.DebugInfo.Add( "Error getting resources", resourcesData.DebugInfo );
				return result;
			}
			var resources = ( ( JArray ) resourcesData.RawData ).Select( m => ( JObject ) m ).ToList();

			//Get the description set data
			var requestData = new JObject()
			{
				{ "ctids", JArray.FromObject( ctids ) },
				{ "include_resources", includeRelatedResources },
				{ "per_branch_limit", perBranchLimit },
				{ "include_graph_data", includeGraphData }
			};

			//Get the data
			var descriptionSetData = MakeDirectRegistryRequest( "description_sets", true, true, requestData.ToString( Formatting.None ) );
			if ( !descriptionSetData.Successful )
			{
				result.DebugInfo.Add( "Error getting description set data", descriptionSetData.DebugInfo );
				return result;
			}

			//Process the data
			//Registry uses a slightly different class structure than our API does, so account for that
			var baseURL = ConfigHelper.GetConfigValue( "credentialRegistryUrl", "" );
			var relatedItemsMap = new List<RelatedItemsWrapper>();
			var relatedItems = new List<JObject>();
			try
			{
				if( descriptionSetData.RawData["description_set_resources"] != null )
				{
					relatedItems = ( ( JArray ) descriptionSetData.RawData[ "description_set_resources" ] ).Select( m => ( JObject ) m ).ToList();
				}

				foreach ( JObject item in (JArray) descriptionSetData.RawData[ "description_sets" ] )
				{
					var wrapper = new RelatedItemsWrapper();
					wrapper.ResourceURI = baseURL + item[ "ctid" ].ToString();

					foreach ( JObject subItem in (JArray) item[ "description_set" ] )
					{
						var path = new RelatedItemsPath()
						{
							Path = subItem[ "path" ].ToString(),
							TotalURIs = int.Parse( subItem[ "total" ].ToString() ),
							URIs = ( ( JArray ) subItem[ "uris" ] ).Select( m => m.ToString() ).ToList()
						};

						wrapper.RelatedItems.Add( path );
					}

					relatedItemsMap.Add( wrapper );
				}
			}
			catch( Exception ex )
			{
				result.DebugInfo.Add( "Error converting description set data", new JObject()
				{
					{ "Exception", ex.Message },
					{ "Raw Data", descriptionSetData.RawData }
				} );
			}

			//Compose the results
			ComposeResults( resources, relatedItems, relatedItemsMap, null, ctids.Count(), result );

			//Return the data
			return result;
		}
		//

		//Predetermine which things go with which other things in a result set
		//Except for the related items, so that we don't duplicate data
		public static ComposedSearchResultSet ComposeResults( List<JObject> results, List<JObject> relatedItems, List<RelatedItemsWrapper> relatedItemsMap, List<JObject> resultsMetadata, int totalResults, ComposedSearchResultSet container = null )
		{
			container = container ?? new ComposedSearchResultSet();

			try
			{
				container.TotalResults = totalResults;
				container.RelatedItems = relatedItems;
				foreach ( var result in results )
				{
					var composed = new ComposedSearchResult();
					composed.Data = result;

					var uri = result[ "@id" ].ToString();
					composed.RelatedItemsMap = relatedItemsMap?.Where( m => m.ResourceURI == uri ).SelectMany( m => m.RelatedItems ).OrderBy( m => m.Path ).ToList();
					composed.Metadata = resultsMetadata?.FirstOrDefault( m => m[ "ResourceURI" ] != null && m[ "ResourceURI" ].ToString() == uri );

					container.Results.Add( composed );
				}
			}
			catch ( Exception ex )
			{
				container.DebugInfo.Add( "Error Composing Results", ex.Message );
			}

			return container;
		}
		//

		public static RawRegistryResponse TemporaryDoNotUseMe_GetRegistryGraph( string ctid )
		{
			return MakeDirectRegistryRequest( "graph/" + ctid, false, true, null, UtilityManager.GetAppKeyValue( "MyCredentialEngineAPIKey" ) );
		}
		//

		public static RawRegistryResponse MakeDirectRegistryRequest( string requestURLPartAfterDomain, bool useHTTPPOST, bool forceAuthenticatedRequest, string jsonData = "", string temporaryDoNotUseMe_overrideKey = "" )
		{
			//Configure the HTTP Client
			var client = new HttpClient();
			System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

			//Get community and key
			var community = UtilityManager.GetAppKeyValue( "defaultCommunity", "" );
			community = ( string.IsNullOrWhiteSpace( community ) || community == "ce-registry" ) ? "" : ( community + "/" );

			//Add the key header if needed
			if( !string.IsNullOrWhiteSpace( community ) || forceAuthenticatedRequest )
			{
				//var credentialEngineAPIKey = UtilityManager.GetAppKeyValue( "MyCredentialEngineAPIKey" );
				//client.DefaultRequestHeaders.TryAddWithoutValidation( "Authorization", "ApiToken " + credentialEngineAPIKey );
				var registryKey = UtilityManager.GetAppKeyValue( "CredentialRegistryAuthorizationToken", "" );
				var token = !string.IsNullOrWhiteSpace( temporaryDoNotUseMe_overrideKey ) ? temporaryDoNotUseMe_overrideKey : registryKey;
				client.DefaultRequestHeaders.TryAddWithoutValidation( "Authorization", "Token " + token );
			}

			//Setup the URL
			var requestURL = ConfigHelper.GetConfigValue( "credentialRegistryUrl", "" ) + community + requestURLPartAfterDomain;

			//Process the request
			var result = new RawRegistryResponse();
			if ( useHTTPPOST )
			{
				var content = new StringContent( jsonData, Encoding.UTF8, "application/json" );
				var response = client.PostAsync( requestURL, content ).Result;
				result = ParseHttpResponse( response );
			}
			else
			{
				var response = client.GetAsync( requestURL ).Result;
				result = ParseHttpResponse( response );
			}

			result.DebugInfo.Add( "Request URL", requestURL );
			return result;
		}
		//

		public static RawRegistryResponse ParseHttpResponse( HttpResponseMessage response )
		{
			//Hold the output
			var result = new RawRegistryResponse();

			//Handle HTTP Errors
			if ( !response.IsSuccessStatusCode )
			{
				result.DebugInfo.Add( "Http Request Error", new JObject()
				{
					{ "Status Code", response.StatusCode.ToString() },
					{ "Reason Phrase", response.ReasonPhrase }
				} );
				return result;
			}

			//Handle JSON Parsing errors
			result.RawContent = response.Content.ReadAsStringAsync().Result;
			try
			{
				result.RawData = ParseJSONWithoutParsingDates<JToken>( result.RawContent );
			}
			catch ( Exception ex )
			{
				result.DebugInfo.Add( "Error parsing JSON", new JObject()
				{
					{ "Exception", ex.Message },
					{ "Raw Data", result.RawContent }
				} );
				return result;
			}

			//Return the result
			result.Successful = true;
			return result;
		}
		//

		//Work around JSON.NET doing that extremely stupid thing where it defaults to parsing date-looking strings into DateTime and you can't turn it off without writing your own reader method like this one
		public static T ParseJSONWithoutParsingDates<T>( string json ) where T : JToken
		{
			JToken result;
			using ( var reader = new JsonTextReader( new StringReader( json ) ) { DateParseHandling = DateParseHandling.None } )
			{
				result = JToken.Load( reader );
			}
			return ( T ) result;
		}
		//

		public class RegistryQuery
		{
			public JObject Query { get; set; }
			public int Skip { get; set; }
			public int Take { get; set; }
			public bool UseBetaAPI { get; set; }
		}
		//
	}
}

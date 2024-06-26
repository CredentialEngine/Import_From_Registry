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
				container.DebugInfo.Add( "Results are null?", results == null );
				container.DebugInfo.Add( "Tried to compose results", JArray.FromObject( results ?? new List<JObject>() ) );
			}

			return container;
		}
		//

		/// <summary>
		/// Get a normalized text value from a property. Always returns a LanguageMapValue even if the data is null. This is primarily intended for language maps, but can handle simple string values as well. Note: this will stringify numbers/bools/etc.
		/// </summary>
		/// <param name="value">The JToken to process, e.g. someJObjectData["some:value"]</param>
		/// <param name="defaultValue">Default string value to return if the data would otherwise be null. If this is null, too, then the SingleValue will be null and the MultiValue will be an empty List.</param>
		/// <param name="overrideLanguageCode">Language codes to look for, in order of preference. Case-sensitive. Defaults to en-US, en-us, en</param>
		/// <param name="returnFirstValueIfMatchingLanguageNotFound">For language maps, if no value is found for the desired langauges, this will use the first value (from any language) if set to true.</param>
		/// <param name="HandleArrayValue">Custom handler to translate array values to the SingleValue property. Defaults to joining strings with " | ".</param>
		/// <returns></returns>
		public static LanguageMapValue GetTextValue( JToken value, string defaultValue, List<string> overrideLanguageCode = null, bool returnFirstValueIfMatchingLanguageNotFound = true, Func<List<string>, string> HandleArrayValue = null )
		{
			//Always return some kind of result
			var result = new LanguageMapValue()
			{
				LanguageCode = "N/A",
				SingleValue = defaultValue,
				MultiValue = new List<string>() { defaultValue }
			};

			//Ensure a handler is set
			HandleArrayValue = HandleArrayValue != null ? HandleArrayValue : values => string.Join( " | ", values );

			//If the value is not null...
			if( value != null ) 
			{
				//Handle Array values
				//e.g. { "sample": [ ... ] }
				if ( value.Type == JTokenType.Array ) 
				{
					result.MultiValue = ( ( JArray ) value ).Where( m => m != null && !string.IsNullOrWhiteSpace( m.ToString() ) ).Select( m => m.ToString() ).ToList();
					result.SingleValue = HandleArrayValue( result.MultiValue );
				}
				//Handle Object values - these should only ever be language strings unless something goes very wrong
				//e.g. { "sample": { "en": "abc", "fr": [ "def", "ghi" ] } }
				if( value.Type == JTokenType.Object )
				{
					//Get the properties of the object
					var properties = ( ( JObject ) value ).Properties().Where( m => m.Value != null && !string.IsNullOrWhiteSpace( m.Value?.ToString() ) ).ToList();

					//Determine which language codes to look for
					var languages = overrideLanguageCode ?? new List<string>() { "en-US", "en-us", "en" };

					//Find the first key in the object that matches one of the languages
					var match = properties.FirstOrDefault( m => languages.Contains( m.Name ) ) ?? ( returnFirstValueIfMatchingLanguageNotFound ? properties.FirstOrDefault() : null );

					//If a match is found...
					if( match != null )
					{
						//Track which language code matched
						result.LanguageCode = match.Name;
						
						//If the value is an array...
						//e.g. { "en": [ "one", "two" ] }
						if( match.Value.Type == JTokenType.Array )
						{
							result.MultiValue = match.Value.Where( m => m != null && !string.IsNullOrWhiteSpace( m.ToString() ) ).Select( m => m.ToString() ).ToList();
							result.SingleValue = HandleArrayValue( result.MultiValue );
						}
						//If the value is anything else
						//e.g. { "en": "some text" }
						else
						{
							result.SingleValue = match.Value.ToString();
							result.MultiValue = new List<string>() { result.SingleValue };
						}
					}
				}
				//Handle any other kind of value
				//e.g. { "sample": "text" }
				else
				{
					result.SingleValue = value.ToString();
					result.MultiValue = new List<string>() { result.SingleValue };
				}
			}

			//Ensure the multi-value array doesn't have empty/null things
			result.MultiValue = result.MultiValue.Where( m => !string.IsNullOrWhiteSpace( m ) ).ToList();

			//Return the value
			return result;
		}
		//

		public static ComposedSearchResultSet DoRegistrySearch( JToken typeFilter, List<JObject> filterItems, int skip, int take, string sort, string loggingSource, bool includeMetadata = false, bool includeDebugInfo = false, bool asDescriptionSet = false, int descriptionSetPerBranchLimit = 10 )
		{
			//Hold the response
			var resultSet = new ComposedSearchResultSet();

			//Hold the query
			var outerQuery = new SearchQuery()
			{
				Skip = skip,
				Take = take,
				Sort = sort,
				IncludeResultsMetadata = includeMetadata,
				IncludeDebugInfo = includeDebugInfo,
				DescriptionSetType = asDescriptionSet ? SearchQuery.DescriptionSetTypes.Resource_RelatedURIs_RelatedData : SearchQuery.DescriptionSetTypes.Resource,
				DescriptionSetRelatedURIsLimit = 10,
				Community = ConfigHelper.GetConfigValue( "defaultCommunity", "" ),
				ExtraLoggingInfo = new JObject() { 
					{ "Source", string.IsNullOrWhiteSpace( loggingSource ) ? "Finder/Search" : loggingSource }, 
					{ "ClientIP", System.Web.HttpContext.Current?.Request?.UserHostAddress ?? "Unknown" } 
				}
			};

			//Construct the outer wrapper
			outerQuery.Query = new JObject()
			{
				{ "@type", typeFilter },
				{ "search:termGroup", new JObject() {
					{ "search:value", JArray.FromObject( filterItems ) },
					{ "search:operator", "search:andTerms" }
				} }
			};

			//Debugging Info
			resultSet.DebugInfo.Add( "Raw Query", JObject.FromObject( outerQuery ) );

			//Do the search via the Accounts System (so that it gets logged)
			var apiKey = UtilityManager.GetAppKeyValue( "MyCredentialEngineAPIKey", "" );
			var apiURL = UtilityManager.GetAppKeyValue( "AssistantCTDLJSONSearchAPIUrl", "" );
			var rawResponse = new SearchResponse();
			try
			{
				//Format the request
				var queryJSON = JsonConvert.SerializeObject( outerQuery, new JsonSerializerSettings() { Formatting = Formatting.None, NullValueHandling = NullValueHandling.Ignore } );

				//Do the request
				var client = new HttpClient();
				client.DefaultRequestHeaders.TryAddWithoutValidation( "Authorization", "ApiToken " + apiKey );
				client.Timeout = new TimeSpan( 0, 10, 0 );
				System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
				var rawResult = client.PostAsync( apiURL, new StringContent( queryJSON, Encoding.UTF8, "application/json" ) ).Result;

				//Process the response
				if ( !rawResult.IsSuccessStatusCode )
				{
					rawResponse.valid = false;
					rawResponse.status = rawResult.ReasonPhrase;
					resultSet.DebugInfo.Add( "Error Performing Search", JObject.FromObject( rawResponse ) );
					return resultSet;
				}

				try
				{
					rawResponse = JsonConvert.DeserializeObject<SearchResponse>( rawResult.Content.ReadAsStringAsync().Result, new JsonSerializerSettings() { DateParseHandling = DateParseHandling.None } );
					if ( !rawResponse.valid )
					{
						//TODO: float the error up to the surface
						resultSet.DebugInfo.Add( "CE API Layer Error", JObject.FromObject( rawResponse ) );
						return resultSet;
					}
				}
				catch ( Exception ex )
				{
					rawResponse.valid = false;
					rawResponse.status = "Error parsing response: " + ex.Message + ( ex.InnerException != null ? " " + ex.InnerException.Message : "" ) + " (from URL: " + apiURL + ")";
					resultSet.DebugInfo.Add( "Error Performing Search", JObject.FromObject( rawResponse ) );
					return resultSet;
				}
			}
			catch ( Exception ex )
			{
				rawResponse.valid = false;
				rawResponse.status = "Error on search: " + ex.Message + ( ex.InnerException != null ? " " + ex.InnerException.Message : "" ) + " (from URL: " + apiURL + ")";
				resultSet.DebugInfo.Add( "Error Performing Search", JObject.FromObject( rawResponse ) );
			}

			//Compose the results
			ComposeResults( rawResponse.data, rawResponse.extra.RelatedItems, rawResponse.extra.RelatedItemsMap, rawResponse.extra.ResultsMetadata, rawResponse.extra.TotalResults, resultSet );

			//Return the results
			return resultSet;
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

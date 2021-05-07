using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net.Http;
using System.Web;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using workIT.Models.Search;
using workIT.Models.API.RegistrySearchAPI;
using workIT.Utilities;

namespace workIT.Services
{
	public class CompetencyFrameworkServicesV2
	{
		public static ComposedSearchResultSet SearchViaRegistry( MainSearchInput sourceQuery, bool asDescriptionSet = false, int descriptionSetPerBranchLimit = 10 )
		{
			//Hold the query
			var apiQuery = new SearchQuery();

			//Hold the response
			var results = new ComposedSearchResultSet();

			//Handle blind searches
			sourceQuery.Keywords = ( sourceQuery.Keywords ?? "" ).Trim();
			if ( string.IsNullOrWhiteSpace( sourceQuery.Keywords ) )
			{
				apiQuery.SkipLogging = true; //No need to log the blind searches that auto-happen when a user visits the search page
				apiQuery.Query = new JObject()
				{
					{ "@type", "ceasn:CompetencyFramework" }
				};
			}
			//Otherwise, look for the value in various places
			else
			{
				//Normalize the text
				var normalized = Regex.Replace( Regex.Replace( sourceQuery.Keywords.ToLower().Trim(), "[^a-z0-9-\" ]", " " ), " +", " " );
				var ctids = Regex.Matches( normalized, @"\b(ce-[a-f0-9-]{0,36})\b" ).Cast<Match>().Select( m => m.Value ).ToList();
				var wordsAndPhrases = normalized.Split( ' ' ).Where( m => !ctids.Contains( m.Replace( "\"", "" ) ) ).ToList();
				var keywords = string.Join( " ", wordsAndPhrases );

				//Basic skeleton
				apiQuery.Query = new JObject()
				{
					{ "@type", "ceasn:CompetencyFramework" },
					{ "search:termGroup", new JObject()
					{
						{ "search:operator", "search:orTerms" },
						{ "ceasn:creator", new JObject() },
						{ "ceasn:publisher", new JObject() },
						{ "ceasn:isPartOf", new JObject() }
					} }
				};

				//Add CTID-based query data
				if ( ctids.Count() > 0 )
				{
					var ctidList = JArray.FromObject( ctids );
					apiQuery.Query[ "search:termGroup" ][ "ceterms:ctid" ] = ctidList;
					apiQuery.Query[ "search:termGroup" ][ "ceasn:isPartOf" ][ "ceterms:ctid" ] = ctidList;
					apiQuery.Query[ "search:termGroup" ][ "ceasn:creator" ][ "ceterms:ctid" ] = ctidList;
					apiQuery.Query[ "search:termGroup" ][ "ceasn:publisher" ][ "ceterms:ctid" ] = ctidList;
				}

				//Add non-CTID-based query data
				if ( keywords.Count() > 0 )
				{
					//Framework
					apiQuery.Query[ "search:termGroup" ][ "ceasn:name" ] = keywords;
					apiQuery.Query[ "search:termGroup" ][ "ceasn:description" ] = keywords;
					apiQuery.Query[ "search:termGroup" ][ "ceasn:source" ] = keywords;
					//Creator
					apiQuery.Query[ "search:termGroup" ][ "ceasn:creator" ][ "ceterms:name" ] = keywords;
					apiQuery.Query[ "search:termGroup" ][ "ceasn:creator" ][ "ceterms:subjectWebpage" ] = keywords;
					apiQuery.Query[ "search:termGroup" ][ "ceasn:creator" ][ "search:operator" ] = "search:orTerms";
					//Publisher
					apiQuery.Query[ "search:termGroup" ][ "ceasn:publisher" ][ "ceterms:name" ] = keywords;
					apiQuery.Query[ "search:termGroup" ][ "ceasn:publisher" ][ "ceterms:subjectWebpage" ] = keywords;
					apiQuery.Query[ "search:termGroup" ][ "ceasn:publisher" ][ "search:operator" ] = "search:orTerms";
					//Competencies
					apiQuery.Query[ "search:termGroup" ][ "ceasn:isPartOf" ][ "ceasn:competencyLabel" ] = keywords;
					apiQuery.Query[ "search:termGroup" ][ "ceasn:isPartOf" ][ "ceasn:competencyText" ] = keywords;
					apiQuery.Query[ "search:termGroup" ][ "ceasn:isPartOf" ][ "ceasn:comment" ] = keywords;
					apiQuery.Query[ "search:termGroup" ][ "ceasn:isPartOf" ][ "search:operator" ] = "search:orTerms";
				}

			}

			//Handle paging
			apiQuery.Skip = sourceQuery.PageSize * ( sourceQuery.StartPage - 1 );
			apiQuery.Take = sourceQuery.PageSize;

			//Include metadata so that we can display when the record was created/updated in the registry
			apiQuery.IncludeResultsMetadata = true;

			//Include debug info for now
			apiQuery.IncludeDebugInfo = true;

			//Handle sort order
			apiQuery.Sort =
				sourceQuery.SortOrder == "alpha" ? "ceasn:name" :
				sourceQuery.SortOrder == "newest" ? "search:recordUpdated" : //Use ceasn:dateModified instead if we want to see newest according to that rather than the Registry record date
				sourceQuery.SortOrder == "relevance" ? (string) null : 
				null;

			//Handle description set
			if ( asDescriptionSet )
			{
				apiQuery.DescriptionSetType = SearchQuery.DescriptionSetTypes.Resource_RelatedURIs_RelatedData;
				apiQuery.DescriptionSetRelatedURIsLimit = descriptionSetPerBranchLimit; //Use -1 for no limit
			}
			else
			{
				apiQuery.DescriptionSetType = SearchQuery.DescriptionSetTypes.Resource;
			}

			//Help with logging
			apiQuery.ExtraLoggingInfo = new JObject();
			apiQuery.ExtraLoggingInfo.Add( "Source", "Finder/Search/CompetencyFramework" );
			apiQuery.ExtraLoggingInfo.Add( "ClientIP", HttpContext.Current?.Request?.UserHostAddress ?? "unknown" );
			results.DebugInfo.Add( "Raw Query", JObject.FromObject( apiQuery ) );

			//Do the query
			var rawResults = DoRegistrySearchAPIQuery( apiQuery );
			results.DebugInfo.Add( "Raw Results", JObject.FromObject( rawResults ) );
			if ( !rawResults.valid )
			{
				results.DebugInfo.Add( "Error Performing Search", rawResults.status );
				return results;
			}

			//Compose the results
			try
			{
				results.TotalResults = rawResults.extra.TotalResults;
				results.RelatedItems = rawResults.extra.RelatedItems;
				results.DebugInfo.Add( "Query Debug", rawResults.extra.DebugInfo );
				foreach( var result in rawResults.data )
				{
					var composed = new ComposedSearchResult();
					composed.Data = result;

					var uri = result[ "@id" ].ToString();
					composed.RelatedItemsMap = rawResults.extra.RelatedItemsMap?.Where( m => m.ResourceURI == uri ).SelectMany( m => m.RelatedItems ).OrderBy( m => m.Path ).ToList();
					composed.Metadata = rawResults.extra.ResultsMetadata?.FirstOrDefault( m => m[ "ResourceURI" ] != null && m[ "ResourceURI" ].ToString() == uri );

					results.Results.Add( composed );
				}
			}
			catch( Exception ex )
			{
				results.DebugInfo.Add( "Error Composing Results", ex.Message );
				return results;
			}

			//Return the results
			return results;
		}
		//

		public static SearchResponse DoRegistrySearchAPIQuery( SearchQuery query )
		{
			var response = new SearchResponse();

			//Get API key and URL
			var apiKey = ConfigHelper.GetConfigValue( "MyCredentialEngineAPIKey", "" );
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
			catch( Exception ex )
			{
				response.valid = false;
				response.status = "Error parsing response: " + ex.Message + ( ex.InnerException != null ? " " + ex.InnerException.Message : "" );
			}

			return response;
		}
		//

		public static string GetEnglishString( JToken languageMap, string defaultValue )
		{
			return GetEnglishToken( languageMap )?.ToString() ?? defaultValue;
		}
		public static List<string> GetEnglishList( JToken languageMap, List<string> defaultValue )
		{
			return GetEnglishToken( languageMap )?.Where( m => m != null && m.Type == JTokenType.String ).Select( m => ( m ?? "" ).ToString() ).ToList() ?? defaultValue;
		}
		public static JToken GetEnglishToken( JToken languageMap )
		{
			try
			{
				return ( languageMap[ "en" ] ?? languageMap[ "en-us" ] ?? languageMap[ "en-US" ] ?? languageMap[ ( ( JObject ) languageMap ).Properties().FirstOrDefault().Name ] );
			}
			catch
			{
				return null;
			}
		}
		//

	}
}

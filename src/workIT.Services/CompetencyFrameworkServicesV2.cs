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

using workIT.Factories;
using workIT.Models.Search;
using workIT.Models.API.RegistrySearchAPI;
using workIT.Utilities;

namespace workIT.Services
{
	public class CompetencyFrameworkServicesV2
	{
		public static ComposedSearchResultSet SearchViaRegistryV2( MainSearchInput query, bool asDescriptionSet = false, int descriptionSetPerBranchLimit = 10 )
		{
			//Handle keywords, with special handling if the keywords are really a CTID
			var keywords = Regex.Replace( Regex.Replace( query.Keywords.ToLower().Trim(), "[^a-z0-9-\" ]", " " ), " +", " " );
			var searchBranches = new List<JObject>();
			if( keywords.IndexOf( "ce-" ) == 0 && keywords.Length == 39 )
			{
				var ctidPart = new JProperty( "ceterms:ctid", new JObject() { { "search:value", keywords }, { "search:matchType", "search:exactMatch" } } );
				var ctidFilter = new JObject() {
					{ "search:operator", "search:orTerms" },
					ctidPart,
					{ "^ceasn:isPartOf", new JObject(){
						ctidPart
					} },
					{ "ceasn:creator", new JObject(){
						ctidPart
					} },
					{ "ceasn:publisher", new JObject(){
						ctidPart
					} }
				};
				searchBranches.Add( ctidFilter );
			}
			else
			{
				var keywordFilter = new JObject() {
					{ "search:operator", "search:orTerms" },
					{ "^ceasn:isPartOf", new JObject(){
						{ "search:operator", "search:orTerms" },
						{ "ceasn:competencyLabel", keywords },
						{ "ceasn:competencyText", keywords },
						{ "ceasn:comment", keywords },
						{ "ceasn:conceptKeyword", keywords },
						{ "ceasn:codedNotation", keywords },
					} },
					{ "ceasn:creator", new JObject(){
						{ "ceterms:subjectWebpage", keywords }
					} },
					{ "ceasn:publisher", new JObject(){
						{ "ceterms:subjectWebpage", keywords }
					} },
					{ "ceasn:name", keywords },
					{ "ceasn:description", keywords },
					{ "ceasn:source", keywords },
					{ "ceasn:conceptKeyword", keywords },
				};
				searchBranches.Add( keywordFilter );
			}

			//Handle role filters
			var roleWrapper = new JObject()
			{
				{ "search:operator", "search:orTerms" }
			};
			var roleOptions = new List<JObject>();
			var roleFilters = query.FiltersV2?.Where( m => m.Name.ToLower() == "organizationroles" && m.TranslationHelper != null ).ToList();
			var publisherRoleCTIDs = roleFilters.Select( m => m.TranslationHelper?[ "> ceasn:publisher > ceterms:ctid" ]?.ToString() ).Where( m => !string.IsNullOrWhiteSpace( m ) ).ToList();
			if ( publisherRoleCTIDs.Count() > 0 )
			{
				roleWrapper.Add( "ceasn:publisher", new JObject() { { "ceterms:ctid", new JObject() { { "search:value", JArray.FromObject( publisherRoleCTIDs ) }, { "search:matchType", "search:exactMatch" } } } } );
			}
			var creatorRoleCTIDs = roleFilters.Select( m => m.TranslationHelper?[ "> ceasn:creator > ceterms:ctid" ]?.ToString() ).Where( m => !string.IsNullOrWhiteSpace( m ) ).ToList();
			if ( creatorRoleCTIDs.Count() > 0 )
			{
				roleWrapper.Add( "ceasn:creator", new JObject() { { "ceterms:ctid", new JObject() { { "search:value", JArray.FromObject( publisherRoleCTIDs ) }, { "search:matchType", "search:exactMatch" } } } } );
			}
			if( publisherRoleCTIDs.Count() > 0 || creatorRoleCTIDs.Count() > 0 )
			{
				searchBranches.Add( roleWrapper );
			}

			//Handle Provider filter
			var providerFilterCTIDs = query.FiltersV2.Where( m => m.Name.ToLower() == "filter:provider" ).Select( m => m.Values ).Where( m => m.ContainsKey( "TextValue" ) ).Select( m => m["TextValue"] ).ToList();
			if( providerFilterCTIDs.Count() > 0 )
			{
				searchBranches.Add( new JObject()
				{
					{ "search:operator", "search:orTerms" },
					{ "ceasn:creator", new JObject(){ { "ceterms:ctid", new JObject() { { "search:value", JArray.FromObject( providerFilterCTIDs ) }, { "search:matchType", "search:exactMatch" } } } } },
					{ "ceasn:publisher", new JObject(){ { "ceterms:ctid", new JObject() { { "search:value", JArray.FromObject( providerFilterCTIDs ) }, { "search:matchType", "search:exactMatch" } } } } },
				} );
			}

			//Handle contains competency text filter
			var competencyTextFilter = query.FiltersV2.Where( m => m.Name.ToLower() == "filter:hascompetencywithtext").Select( m => m.Values ).Where( m => m.ContainsKey( "TextValue" ) ).Select( m => m[ "TextValue" ] ).ToList();
			if( competencyTextFilter.Count() > 0 )
			{
				searchBranches.Add( new JObject()
				{
					{ "^ceasn:isPartOf", new JObject()
					{
						{ "search:operator", "search:orTerms" },
						{ "ceasn:competencyLabel", JArray.FromObject( competencyTextFilter ) },
						{ "ceasn:competencyText", JArray.FromObject( competencyTextFilter ) },
						{ "ceasn:comment", JArray.FromObject( competencyTextFilter ) },
						{ "ceasn:conceptKeyword", JArray.FromObject( competencyTextFilter ) },
						{ "ceasn:codedNotation", JArray.FromObject( competencyTextFilter ) },
					} }
				} );
			}

			//Handle skip, take, and sort
			var skip = query.PageSize * ( query.StartPage - 1 );
			var take = query.PageSize;
			var sortOrder =
				query.SortOrder == "alpha" ? "ceasn:name" :
				query.SortOrder == "zalpha" ? "^ceasn:name" :
				query.SortOrder == "newest" ? "^search:recordUpdated" : //Use ceasn:dateModified instead if we want to see newest according to that rather than the Registry record date
				query.SortOrder == "oldest" ? "search:recordUpdated" :
				query.SortOrder == "relevance" ? "^search:relevance" :
				null;

			return RegistryServicesV2.DoRegistrySearch( "ceasn:CompetencyFramework", searchBranches, skip, take, sortOrder, "Finder/Search/CompetencyFramework", true, true, asDescriptionSet, descriptionSetPerBranchLimit );
		}
		//

		public static ComposedSearchResultSet SearchViaRegistry( MainSearchInput sourceQuery, bool asDescriptionSet = false, int descriptionSetPerBranchLimit = 10 )
		{
			//Hold the query
			var apiQuery = new SearchQuery();

			//Hold the response
			var results = new ComposedSearchResultSet();

			//Handle blind searches
			sourceQuery.Keywords = ( sourceQuery.Keywords ?? "" ).Trim();
			if ( string.IsNullOrWhiteSpace( sourceQuery.Keywords ) && sourceQuery.FiltersV2.Count() == 0 )
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
						{ "^ceasn:isPartOf", new JObject() }
					} }
				};

				//Add CTID-based query data
				if ( ctids.Count() > 0 )
				{
					var ctidList = JArray.FromObject( ctids );
					apiQuery.Query[ "search:termGroup" ][ "ceterms:ctid" ] = ctidList;
					apiQuery.Query[ "search:termGroup" ][ "^ceasn:isPartOf" ][ "ceterms:ctid" ] = ctidList;
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
					apiQuery.Query[ "search:termGroup" ][ "ceasn:conceptKeyword" ] = keywords;
					//Creator
					apiQuery.Query[ "search:termGroup" ][ "ceasn:creator" ][ "ceterms:name" ] = keywords;
					apiQuery.Query[ "search:termGroup" ][ "ceasn:creator" ][ "ceterms:subjectWebpage" ] = keywords;
					apiQuery.Query[ "search:termGroup" ][ "ceasn:creator" ][ "search:operator" ] = "search:orTerms";
					//Publisher
					apiQuery.Query[ "search:termGroup" ][ "ceasn:publisher" ][ "ceterms:name" ] = keywords;
					apiQuery.Query[ "search:termGroup" ][ "ceasn:publisher" ][ "ceterms:subjectWebpage" ] = keywords;
					apiQuery.Query[ "search:termGroup" ][ "ceasn:publisher" ][ "search:operator" ] = "search:orTerms";
					//Competencies
					apiQuery.Query[ "search:termGroup" ][ "^ceasn:isPartOf" ][ "ceasn:competencyLabel" ] = keywords;
					apiQuery.Query[ "search:termGroup" ][ "^ceasn:isPartOf" ][ "ceasn:competencyText" ] = keywords;
					apiQuery.Query[ "search:termGroup" ][ "^ceasn:isPartOf" ][ "ceasn:comment" ] = keywords;
					apiQuery.Query[ "search:termGroup" ][ "^ceasn:isPartOf" ][ "ceasn:conceptKeyword" ] = keywords;
					apiQuery.Query[ "search:termGroup" ][ "^ceasn:isPartOf" ][ "search:operator" ] = "search:orTerms";
				}

				//Handle filters sent from the new finder
				foreach( var sourceFilter in sourceQuery.FiltersV2 ?? new List<MainSearchFilterV2>() )
				{
					//Organization Roles queries
					if( sourceFilter.Name == "organizationroles" && sourceFilter.TranslationHelper != null )
					{
						AddFilterItem( sourceFilter.TranslationHelper, "> ceasn:publisher > ceterms:ctid", ( JObject ) apiQuery.Query[ "search:termGroup" ][ "ceasn:publisher" ], "ceterms:ctid" );
						AddFilterItem( sourceFilter.TranslationHelper, "> ceasn:creator > ceterms:ctid", ( JObject ) apiQuery.Query[ "search:termGroup" ][ "ceasn:creator" ], "ceterms:ctid" );
					}
				}

				//If it was just an organization search query and there is no "isPartOf" data, remove it
				if(((JObject) apiQuery.Query["search:termGroup"]["^ceasn:isPartOf"]).Properties().Count() == 0 )
				{
					apiQuery.Query[ "search:termGroup" ][ "^ceasn:isPartOf" ].Parent.Remove();
				}
			}

			//Handle paging
			apiQuery.Skip = sourceQuery.PageSize * ( sourceQuery.StartPage - 1 );
			apiQuery.Take = sourceQuery.PageSize;

			//Include metadata so that we can display when the record was created/updated in the registry
			apiQuery.IncludeResultsMetadata = true;

			//Include debug info for now
			apiQuery.IncludeDebugInfo = true;

			//Use the beta search API
			apiQuery.UseBetaAPI = true;

			//Handle sort order
			apiQuery.Sort =
				sourceQuery.SortOrder == "alpha" ? "ceasn:name" :
				sourceQuery.SortOrder == "zalpha" ? "^ceasn:name" :
				sourceQuery.SortOrder == "newest" ? "^search:recordUpdated" : //Use ceasn:dateModified instead if we want to see newest according to that rather than the Registry record date
				sourceQuery.SortOrder == "oldest" ? "search:recordUpdated" :
				sourceQuery.SortOrder == "relevance" ? "^search:relevance" : 
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

			//Include community if it isn't vanilla
			var community = ConfigHelper.GetConfigValue( "defaultCommunity", "" );
			if( !string.IsNullOrWhiteSpace(community) && community.ToLower() != "ce-registry" )
			{
				apiQuery.Community = community;
			}

			//Help with logging
			apiQuery.ExtraLoggingInfo = new JObject();
			apiQuery.ExtraLoggingInfo.Add( "Source", "Finder/Search/CompetencyFramework" );
			apiQuery.ExtraLoggingInfo.Add( "ClientIP", HttpContext.Current?.Request?.UserHostAddress ?? "unknown" );
			results.DebugInfo.Add( "Raw Query", JObject.FromObject( apiQuery ) );

			//Do the query
			var rawResults = DoRegistrySearchAPIQuery( apiQuery );
			results.DebugInfo.Add( "Raw Response", JObject.FromObject( rawResults ) );
			if ( !rawResults.valid )
			{
				results.DebugInfo.Add( "Error Performing Search", rawResults.status );
				return results;
			}

			//Compose the results
			RegistryServicesV2.ComposeResults( rawResults.data, rawResults.extra.RelatedItems, rawResults.extra.RelatedItemsMap, rawResults.extra.ResultsMetadata, rawResults.extra.TotalResults, results );

			//Return the results
			return results;
		}
		private static void AddFilterItem( JObject source, string sourcePath, JObject destination, string destinationProperty )
		{
			if( source?[ sourcePath ] != null )
			{
				destination[ destinationProperty ] = destination[ destinationProperty ] ?? new JArray();
				( ( JArray ) destination[ destinationProperty ] ).Add( source[ sourcePath ] );
			}
		}
		//

		public static SearchResponse DoRegistrySearchAPIQuery( SearchQuery query )
		{
			var response = new SearchResponse();

			//Get API key and URL
			var apiKey = UtilityManager.GetAppKeyValue( "MyCredentialEngineAPIKey", "" );
			var apiURL = UtilityManager.GetAppKeyValue( "AssistantCTDLJSONSearchAPIUrl", "" );
			try
			{
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
					response.status = "Error parsing response: " + ex.Message + ( ex.InnerException != null ? " " + ex.InnerException.Message : "" ) + " (from URL: " + apiURL + ")";
				}
			}
			catch ( Exception ex )
			{
				response.valid = false;
				response.status = "Error on search: " + ex.Message + ( ex.InnerException != null ? " " + ex.InnerException.Message : "" ) + " (from URL: " + apiURL + ")";
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
				if ( languageMap == null )
					return null;

				return ( languageMap[ "en" ] ?? languageMap[ "en-us" ] ?? languageMap[ "en-US" ] ?? languageMap[ ( ( JObject ) languageMap ).Properties().FirstOrDefault().Name ] );
			}
			catch
			{
				return null;
			}
		}
		//

		/// <summary>
		/// Update totals related to Competency Frameworks
		/// </summary>
		/// <param name="usingCFTotals">If true, the totals will be retrieved using { "@type","ceasn:CompetencyFramework" }, otherwise use { "@type","ceasn:Competency" } in searches</param>
		public static void UpdateCompetencyFrameworkReportTotals( bool usingCFTotals = true, bool includingRelationships = true )
		{
			var loggingClassName = "CompetencyFrameworkServicesV2";
			LoggingHelper.DoTrace( 5, loggingClassName + ".UpdateCompetencyFrameworkReportTotals started" );
			var mgr = new CodesManager();

			try
			{
				var total = GetCompetencyFrameworkTermTotal( null );
				if ( total > 0 )
				{
					mgr.UpdateEntityTypes( 10, total, false );
				}

				var competencyTotals = GetCompetencyTermTotal( null );
                mgr.UpdateEntityStatistic( 10, "frameworkReport:Competencies", competencyTotals, false );
                //23-04-07 also update Codes.EntityTypes
                mgr.UpdateEntityTypes( 17, competencyTotals, false );

                if ( includingRelationships )
				{
					if ( usingCFTotals )
					{
						mgr.UpdateEntityStatistic( 10, "frameworkReport:HasEducationLevels", GetCompetencyFrameworkTermTotal( "ceasn:educationLevelType" ) );
						mgr.UpdateEntityStatistic( 10, "frameworkReport:HasOccupationType", GetCompetencyFrameworkTermTotal( "ceterms:occupationType" ) );
						mgr.UpdateEntityStatistic( 10, "frameworkReport:HasIndustryType", GetCompetencyFrameworkTermTotal( "ceterms:industryType" ) );
						mgr.UpdateEntityStatistic( 10, "frameworkReport:HasAlignFrom", GetCompetencyFrameworksWithCompetencyTermTotal( "ceasn:alignFrom" ) );
						mgr.UpdateEntityStatistic( 10, "frameworkReport:HasAlignTo", GetCompetencyFrameworksWithCompetencyTermTotal( "ceasn:alignTo" ) );
						mgr.UpdateEntityStatistic( 10, "frameworkReport:HasBroadAlignment", GetCompetencyFrameworksWithCompetencyTermTotal( "ceasn:broadAlignment" ) );
						mgr.UpdateEntityStatistic( 10, "frameworkReport:HasExactAlignment", GetCompetencyFrameworksWithCompetencyTermTotal( "ceasn:exactAlignment" ) );
						mgr.UpdateEntityStatistic( 10, "frameworkReport:HasMajorAlignment", GetCompetencyFrameworksWithCompetencyTermTotal( "ceasn:majorAlignment" ) );
						mgr.UpdateEntityStatistic( 10, "frameworkReport:HasMinorAlignment", GetCompetencyFrameworksWithCompetencyTermTotal( "ceasn:minorAlignment" ) );
						mgr.UpdateEntityStatistic( 10, "frameworkReport:HasNarrowAlignment", GetCompetencyFrameworksWithCompetencyTermTotal( "ceasn:narrowAlignment" ) );
						mgr.UpdateEntityStatistic( 10, "frameworkReport:HasPrerequisiteAlignment", GetCompetencyFrameworksWithCompetencyTermTotal( "ceasn:prerequisiteAlignment" ) );
					}
					else
					{
						mgr.UpdateEntityStatistic( 10, "frameworkReport:HasEducationLevels", GetCompetencyTermTotal( "ceasn:educationLevelType" ) );
						mgr.UpdateEntityStatistic( 10, "frameworkReport:HasAlignFrom", GetCompetencyTermTotal( "ceasn:alignFrom" ) );
						mgr.UpdateEntityStatistic( 10, "frameworkReport:HasAlignTo", GetCompetencyTermTotal( "ceasn:alignTo" ) );
						mgr.UpdateEntityStatistic( 10, "frameworkReport:HasBroadAlignment", GetCompetencyTermTotal( "ceasn:broadAlignment" ) );
						mgr.UpdateEntityStatistic( 10, "frameworkReport:HasExactAlignment", GetCompetencyTermTotal( "ceasn:exactAlignment" ) );
						mgr.UpdateEntityStatistic( 10, "frameworkReport:HasMajorAlignment", GetCompetencyTermTotal( "ceasn:majorAlignment" ) );
						mgr.UpdateEntityStatistic( 10, "frameworkReport:HasMinorAlignment", GetCompetencyTermTotal( "ceasn:minorAlignment" ) );
						mgr.UpdateEntityStatistic( 10, "frameworkReport:HasNarrowAlignment", GetCompetencyTermTotal( "ceasn:narrowAlignment" ) );
						mgr.UpdateEntityStatistic( 10, "frameworkReport:HasPrerequisiteAlignment", GetCompetencyTermTotal( "ceasn:prerequisiteAlignment" ) );
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "Services.UpdateCompetencyFrameworkReportTotals" );
			}

			LoggingHelper.DoTrace( 5, loggingClassName + ".UpdateCompetencyFrameworkReportTotals completed" );
		}
		//

		public static int GetTermTotal( JObject coreQuery, string addSearchTerm, string extraLoggingSourceHelper, bool skipLogging )
		{
			var apiQuery = new SearchQuery()
			{
				Query = coreQuery,
				ExtraLoggingInfo = new JObject()
				{
					{ "Source", string.IsNullOrWhiteSpace(extraLoggingSourceHelper) ? "Finder/GetTermTotal" + ( string.IsNullOrWhiteSpace( addSearchTerm ) ? "" : "/" + addSearchTerm ) : extraLoggingSourceHelper }
				},
				SkipLogging = skipLogging,
				UseBetaAPI = true
			};

			if ( !string.IsNullOrWhiteSpace( addSearchTerm ) )
			{
				apiQuery.Query.Add( addSearchTerm, "search:anyValue" );
			}

			var result = DoRegistrySearchAPIQuery( apiQuery );
			return result.extra.TotalResults;
		}

		public static int GetCompetencyFrameworkTermTotal( string searchTerm )
		{
			return GetTermTotal( new JObject() { { "@type", "ceasn:CompetencyFramework" } }, searchTerm, "Finder/GetCompetencyFrameworkTermTotal" + ( string.IsNullOrWhiteSpace( searchTerm ) ? "" : "/" + searchTerm ), false ); //Probably only need to log totals for types
		}
		//

		public static int GetCompetencyTermTotal( string searchTerm )
		{
			return GetTermTotal( new JObject() { { "@type", "ceasn:Competency" } }, searchTerm, "Finder/GetCompetencyTermTotal" + ( string.IsNullOrWhiteSpace( searchTerm ) ? "" : "/" + searchTerm ), false ); //Probably only need to log totals for types
		}
		//

		public static int GetCompetencyFrameworksWithCompetencyTermTotal( string searchTerm )
		{
			var query = new JObject() 
			{ 
				{ "@type", "ceasn:CompetencyFramework" },
				{ "^ceasn:isPartOf", new JObject() {
					{ searchTerm ?? "none", "search:anyValue" }
				} }
			};

			return string.IsNullOrWhiteSpace( searchTerm ) ? 0 : GetTermTotal( query, null, "Finder/GetCompetencyFrameworksWithCompetencyTermTotal" + ( string.IsNullOrWhiteSpace( searchTerm ) ? "" : "/" + searchTerm ), false ); //Probably don't need to log these(?)
		}
		//

	}
}

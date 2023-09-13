using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Caching;
using System.Text;
using System.Threading;
using System.Web;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

using workIT.Factories;
using workIT.Models;
using workIT.Models.Common;
using workIT.Models.Helpers;
using workIT.Models.Elastic;
using ElasticHelper = workIT.Services.ElasticServices;

using workIT.Models.Helpers.CompetencyFrameworkHelpers;
using workIT.Models.Search;
using workIT.Utilities;

using EntityMgr = workIT.Factories.CompetencyFrameworkManager;
using MPM = workIT.Models.ProfileModels;
using ThisEntity = workIT.Models.ProfileModels.CompetencyFramework;

namespace workIT.Services
{
	public class CompetencyFrameworkServices
	{
		string thisClassName = "CompetencyFrameworkServices";
		ActivityServices activityMgr = new ActivityServices();
		public List<string> messages = new List<string>();

		#region import

		public bool Import( ThisEntity entity, ref SaveStatus status )
		{
			LoggingHelper.DoTrace( 5, thisClassName + "Import entered. " + entity.Name );
			//do a get, and add to cache before updating
			if ( entity.Id > 0 )
			{
				//note could cause problems verifying after an import (i.e. shows cached version. Maybe remove from cache after completion.
				//var detail = GetDetail( entity.Id );
			}
			bool isValid = new EntityMgr().Save( entity, ref status, true );
			List<string> messages = new List<string>();
			if ( entity.Id > 0 )
			{
				if ( UtilityManager.GetAppKeyValue( "delayingAllCacheUpdates", false ) == false )
				{
					//update cache - not applicable yet
					//new CacheManager().PopulateEntityRelatedCaches( entity.RowId );
					//update Elastic
					if ( Utilities.UtilityManager.GetAppKeyValue( "updatingElasticIndexImmediately", false ) )
						ElasticHelper.CompetencyFramework_UpdateIndex( entity.Id );
					else
					{
						new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_COMPETENCY_FRAMEWORK, entity.Id, 1, ref messages );
						if ( messages.Count > 0 )
							status.AddWarningRange( messages );
					}
					new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, entity.OrganizationId, 1, ref messages );
				}
				else
				{
					new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_COMPETENCY_FRAMEWORK, entity.Id, 1, ref messages );
					new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, entity.OrganizationId, 1, ref messages );
					if ( messages.Count > 0 )
						status.AddWarningRange( messages );
				}
				//no caching needed yet
				//CacheManager.RemoveItemFromCache( "cframework", entity.Id );
			}

			return isValid;
		}

		#endregion
		#region retrieval
		public static MPM.CompetencyFramework GetCompetencyFrameworkByCtid( string ctid )
		{
			MPM.CompetencyFramework entity = new MPM.CompetencyFramework();
			entity = CompetencyFrameworkManager.GetByCtid( ctid );
			return entity;
		}

		public static MPM.CompetencyFramework Get( int id )
		{
			MPM.CompetencyFramework entity = new MPM.CompetencyFramework();
			entity = CompetencyFrameworkManager.Get( id );
			return entity;
		}
		//

		public static string GetCTIDFromID( int id )
		{
			return CompetencyFrameworkManager.GetCTIDFromID( id );
		}
		//

		public static string GetCompetencyCTIDFromCompetencyID( int id )
		{
			return CompetencyFrameworkManager.GetCompetencyCTIDFromCompetencyID( id );
		}
		//

		public static List<MPM.CompetencyFrameworkSummary> CompetencyFrameworkSearch( MainSearchInput data, ref int pTotalRows )
		{
			if ( UtilityManager.GetAppKeyValue( "usingElasticCompetencyFrameworkSearch", false ) )
			{
				return ElasticHelper.CompetencyFrameworkSearch( data, ref pTotalRows );
			}
			else
			{
				var results = new List<MPM.CompetencyFrameworkSummary>();
				var list = DoFrameworksSearch( data, ref pTotalRows );
				//var list = ElasticManager.CompetencyFramework_SearchForElastic( data.fil, data.StartPage, data.PageSize, ref pTotalRows );
				foreach ( var item in list )
				{
					results.Add( new MPM.CompetencyFrameworkSummary()
					{
						Id = item.Id,
						Name = item.Name,
						Description = item.Description,
						Source = item.SourceUrl,
						CTID = item.CTID,
						PrimaryOrganization = item.PrimaryOrganizationId == 0 ? null : new Organization() { Id = item.PrimaryOrganizationId, Name=item.PrimaryOrganizationName} 
						//EntityTypeId = CodesManager.ENTITY_TYPE_COMPETENCY_FRAMEWORK,
						//EntityType = "CompetencyFramework"
					} );
				}
				return results;
			}

		}//

		public static List<CompetencyFrameworkIndex> DoFrameworksSearch( MainSearchInput data, ref int pTotalRows )
		{
			string where = "";

			//only target full entities
			where = " ( base.EntityStateId = 3 ) ";

			//need to create a new category id for custom filters
			//SearchServices.HandleCustomFilters( data, 61, ref where );

			SetKeywordFilter( data.Keywords, false, ref where );
			//SearchServices.SetSubjectsFilter( data, CodesManager.ENTITY_TYPE_COMPETENCY_FRAMEWORK, ref where );

			//SetPropertiesFilter( data, ref where );
			SearchServices.SetRolesFilter( data, ref where );
			SearchServices.SetBoundariesFilter( data, ref where );

			LoggingHelper.DoTrace( 6, "CompetencyFrameworkServices.DoFrameworksSearch(). Filter: " + where );
			//return EntityMgr.Search( where, data.SortOrder, data.StartPage, data.PageSize, ref totalRows );
			return ElasticManager.CompetencyFramework_SearchForElastic( where, data.StartPage, data.PageSize, ref pTotalRows );
		}
		//
		private static void SetKeywordFilter( string keywords, bool isBasic, ref string where )
		{
			if ( string.IsNullOrWhiteSpace( keywords ) )
				return;
			//trim trailing (org)
			if ( keywords.IndexOf( "('" ) > 0 )
				keywords = keywords.Substring( 0, keywords.IndexOf( "('" ) );

			//OR base.Description like '{0}' 
			string text = " (base.name like '{0}' OR base.SourceUrl like '{0}'  OR base.OrganizationName like '{0}'  ) ";
			bool isCustomSearch = false;
			//use Entity.SearchIndex for all
			//string indexFilter = " OR (base.Id in (SELECT c.id FROM [dbo].[Entity.SearchIndex] a inner join Entity b on a.EntityId = b.Id inner join TransferValue c on b.EntityUid = c.RowId where (b.EntityTypeId = 3 AND ( a.TextValue like '{0}' OR a.[CodedNotation] like '{0}' ) ))) ";

			//for ctid, needs a valid ctid or guid
			if ( keywords.IndexOf( "ce-" ) > -1 && keywords.Length == 39 )
			{
				text = " ( CTID = '{0}' ) ";
				isCustomSearch = true;
			}
			else if ( ServiceHelper.IsValidGuid( keywords ) )
			{
				text = " ( CTID = 'ce-{0}' ) ";
				isCustomSearch = true;
			}
			else if ( keywords.ToLower() == "[hascredentialregistryid]" )
			{
				text = " ( len(Isnull(CredentialRegistryId,'') ) = 36 ) ";
				isCustomSearch = true;
			}


			string AND = "";
			if ( where.Length > 0 )
				AND = " AND ";

			keywords = ServiceHelper.HandleApostrophes( keywords );
			if ( keywords.IndexOf( "%" ) == -1 && !isCustomSearch )
			{
				keywords = SearchServices.SearchifyWord( keywords );
			}

			//skip url  OR base.Url like '{0}' 
			if ( isBasic || isCustomSearch )
				where = where + AND + string.Format( " ( " + text + " ) ", keywords );
			else
				where = where + AND + string.Format( " ( " + text + " ) ", keywords );

		}
		#endregion
		#region Methods using registry search index
		//Get a Competency Framework description set
		public static CTDLAPICompetencyFrameworkResultSet GetCompetencyFrameworkDescriptionSet( string ctid )
		{
			var queryData = new JObject()
			{
				{ "@type", "ceterms:CompetencyFramework" },
				{ "ceterms:ctid", ctid }
			};

			var clientIP = "unknown";
			try
			{
				clientIP = HttpContext.Current.Request.UserHostAddress;
			}
			catch { }

			var resultData = DoQuery( queryData, 0, 1, "", true, "https://credentialfinder.org/Finder/SearchViaRegistry/", clientIP, "CompetencyFramework" );

			var resultSet = new CTDLAPICompetencyFrameworkResultSet()
			{
				Results = ParseResults<CTDLAPICompetencyFrameworkResult>( resultData.data ),
				RelatedItems = resultData.extra.RelatedItems,
				TotalResults = resultData.extra.TotalResults
			};

			return resultSet;
		}
		//

		//Temporary workaround while Mike is trying to fix issues with the Assistant API
		//This functionality should instead be handled by the assistant API so that logging and whatnot can be handled there
		//Although not having that step in the middle does make this go faster...


		//TODO: Add caching


		public static CTDLAPICompetencyFrameworkResultSet GetCompetencyFrameworkDescriptionSetsTemporary( List<string> ctids, int relatedItemsLimit = 10 )
		{
			//Get keys
			var key = UtilityManager.GetAppKeyValue( "CredentialRegistryAuthorizationToken" );
			var url = UtilityManager.GetAppKeyValue( "GetDescriptionSetByCTIDEndpoint" );
			var baseURL = UtilityManager.GetAppKeyValue( "credentialRegistryUrl" );
			var debug = new JObject();

			//Create clients
			var authorizedClient = new HttpClient();
			authorizedClient.DefaultRequestHeaders.TryAddWithoutValidation( "Authorization", "Token " + key );
			var regularClient = new HttpClient();

			//Create intermediate containers
			var relatedItemMaps = new List<JObject>();
			var allRelatedURIs = new List<string>();
			var fetchGraphs = new List<string>();
			var retrievedResources = new List<JObject>();

			//For each CTID, get the description set summaries
			foreach ( var ctid in ctids )
			{
				try
				{
					var requestURL = url + ctid + ( relatedItemsLimit > 0 ? "?limit=" + relatedItemsLimit : "" );

					var httpResult = authorizedClient.GetAsync( requestURL ).Result;
					var httpResultBody = httpResult.Content.ReadAsStringAsync().Result;
					var resultData = JArray.Parse( httpResultBody );

					var itemMap = new JObject();
					itemMap[ "ResourceURI" ] = baseURL + "/resources/" + ctid;
					var relatedItems = new List<JObject>();

					foreach ( JObject rawMap in resultData )
					{
						var uris = ( ( JArray ) rawMap[ "uris" ] ).Select( m => m.ToString() ).ToList();
						allRelatedURIs.AddRange( uris );
						relatedItems.Add( new JObject()
						{
							{ "Path", rawMap[ "path" ].ToString() },
							{  "URIs", JArray.FromObject( uris ) },
							{  "TotalURIs", ( int ) rawMap[ "total" ] }
						} );

						if( uris.Any( m => m.IndexOf( "_:" ) == 0 ) )
						{
							fetchGraphs.Add( baseURL + "/graph/" + ctid );
						}
					}
					itemMap[ "RelatedItems" ] = JArray.FromObject( relatedItems );

					relatedItemMaps.Add( itemMap );
				}
				catch ( Exception ex )
				{
					debug[ "Error getting related data for " + ctid ] = ex.Message;
				}
			}

			//Remove duplicates in the tracking of all related URIs
			allRelatedURIs = allRelatedURIs.Distinct().ToList();

			//For each item above that involves a bnode, get the @graph data in order to get the bnode data
			//Also hang onto any non-bnode data that happens to be in the list of related URIs
			foreach( var uri in fetchGraphs )
			{
				try
				{
					var rawResult = regularClient.GetAsync( uri ).Result;
					var rawData = rawResult.Content.ReadAsStringAsync().Result;
					var resultData = JObject.Parse( rawData );

					var items = ( JArray ) resultData[ "@graph" ];
					retrievedResources.AddRange( items.Where( m => allRelatedURIs.Contains( m[ "@id" ].ToString() ) ).Select( m => ( JObject ) m ).ToList() );
				}
				catch ( Exception ex )
				{
					debug[ "Error getting graph for " + uri ] = ex.Message;
				}
			}

			//Figure out what hasn't been gotten yet
			var loadedURIs = retrievedResources.Select( m => m[ "@id" ].ToString() ).ToList();
			var remainingRelatedURIs = allRelatedURIs.Where( m => !loadedURIs.Contains( m ) ).ToList();
			var remainingRelatedCTIDs = remainingRelatedURIs.Where( m => m.Contains( "/ce-" ) ).Select( m => "ce-" + m.Split( new string[] { "/ce-" }, StringSplitOptions.RemoveEmptyEntries ).Last() ).ToList();

			//Then go get it
			if( remainingRelatedCTIDs.Count() > 0 )
			{
				try
				{
					var query = new JObject()
					{
						{ "ctids", JArray.FromObject( remainingRelatedCTIDs ) }
					};
					var fetchedResourcesResult = authorizedClient.PostAsync( baseURL + "/resources/search", new StringContent( query.ToString( Formatting.None ) ) ).Result;
					var fetchedResourcesData = fetchedResourcesResult.Content.ReadAsStringAsync().Result;
					var fetchedResources = JArray.Parse( fetchedResourcesData );
					retrievedResources.AddRange( fetchedResources.Select( m => ( JObject ) m ).ToList() );
				}
				catch ( Exception ex )
				{
					debug[ "Error getting related data by CTIDs" ] = ex.Message;
				}
			}

			//Construct the result set
			var resultSet = new CTDLAPICompetencyFrameworkResultSet()
			{
				Results = ctids.Select( m => new CTDLAPICompetencyFrameworkResult() { CTID = m } ).ToList(),
				RelatedItemsMap = JArray.FromObject( relatedItemMaps ),
				RelatedItems = JArray.FromObject( retrievedResources ),
				TotalResults = ctids.Count(),
				Debug = debug
			};

			//Return the data
			return resultSet;
		}
		//

		public static CTDLAPICompetencyFrameworkResultSet GetCompetencyFrameworkDescriptionSetsByCTIDs( List<string> ctids, bool includeRelatedData = true, int relatedURIsLimit = 10, int relatedItemsLimit = 10 )
		{
			var result = new CTDLAPICompetencyFrameworkResultSet();
			try
			{
				//Get the data
				var descriptionSet = DescriptionSetServices.GetDescriptionSetsByCTIDs( ctids, includeRelatedData, relatedURIsLimit, relatedItemsLimit );
				var registryURL = ConfigHelper.GetConfigValue( "credentialRegistryUrl", "" );

				//Convert the data to use the old format
				result.Results = ctids.Select( m => new CTDLAPICompetencyFrameworkResult()
				{
					_ID = registryURL + "resources/" + m,
					Name = new LanguageMap( "" ),
					Description = new LanguageMap( "" ),
					_Type = "",
					CTID = m,
					RawData = new JObject() {
							{ "@id", registryURL + "resources/" + m },
							{ "ceterms:ctid", m }
						}.ToString( Formatting.None )
				} ).ToList();

				result.RelatedItems = JArray.FromObject( descriptionSet.RelatedItems );
				result.RelatedItemsMap = JArray.FromObject( descriptionSet.RelatedItemsMap );
				result.TotalResults = ctids.Count();
				result.Debug = descriptionSet.DebugInfo;
			}
			catch ( Exception ex )
			{
				result.Debug[ "Error Getting Description Sets" ] = ex.Message;
			}

			return result;
		}

		public static CTDLAPICompetencyFrameworkResultSet GetCompetencyFrameworkDescriptionSetsFaster( List<string> ctids, bool includeRelatedData = true, int relatedItemsLimit = 10 )
		{
			var debug = new JObject();
			try
			{
				var request = new JObject()
				{
					{ "DescriptionSetCTIDs", JArray.FromObject(ctids) },
					{ "DescriptionSetRelatedItemsLimit", relatedItemsLimit },
					{ "DescriptionSetIncludeData", includeRelatedData }
				};

				var clientIP = "unknown";
				try
				{
					clientIP = HttpContext.Current.Request.UserHostAddress;
				}
				catch { }

				//Get API key and URL
				var apiKey = ConfigHelper.GetConfigValue( "MyCredentialEngineAPIKey", "" );
				var apiURL = ConfigHelper.GetConfigValue( "GetDescriptionSetsByCTIDsEndpoint", "" );
				var registryURL = ConfigHelper.GetConfigValue( "credentialRegistryUrl", "" );

				//Get the data
				var client = new HttpClient();
				client.DefaultRequestHeaders.TryAddWithoutValidation( "Authorization", "ApiToken " + apiKey );
				client.DefaultRequestHeaders.Referrer = new Uri( "https://credentialfinder.org/Finder/GetCompetencyFrameworkDescriptionSets/" );
				client.Timeout = new TimeSpan( 0, 10, 0 );
				debug[ "API URL" ] = apiURL;
				var rawResult = client.PostAsync( apiURL, new StringContent( request.ToString( Formatting.None ), Encoding.UTF8, "application/json" ) ).Result;
				var rawResultContent = rawResult.Content.ReadAsStringAsync().Result;
				debug[ "Raw Result Text" ] = rawResultContent;
				debug[ "Raw Result Status" ] = rawResult.StatusCode.ToString();
				var parsedResult = JObject.Parse( rawResultContent );
				var parsedResultData = parsedResult[ "data" ];

				debug[ "Inner Debug" ] = parsedResultData[ "Debug" ];
				var resultSet = new CTDLAPICompetencyFrameworkResultSet()
				{
					Results = ctids.Select( m => new CTDLAPICompetencyFrameworkResult()
					{
						_ID = registryURL + "resources/" + m,
						Name = new LanguageMap( "" ),
						Description = new LanguageMap( "" ),
						_Type = "",
						CTID = m,
						RawData = new JObject() { 
							{ "@id", registryURL + "resources/" + m },
							{ "ceterms:ctid", m }
						}.ToString( Formatting.None )
					} ).ToList(),
					RelatedItems = ( JArray ) parsedResultData[ "RelatedItems" ],
					RelatedItemsMap = ( JArray ) parsedResultData[ "RelatedItemsMap" ],
					TotalResults = ctids.Count(),
					Debug = debug
				};

				return resultSet;
			}
			catch ( Exception ex )
			{
				debug[ "Error Getting Description Sets" ] = ex.Message;
				return new CTDLAPICompetencyFrameworkResultSet() { Debug = debug };
			}
		}
		//

		//Get multiple description sets using new methods
		public static CTDLAPICompetencyFrameworkResultSet GetCompetencyFrameworkDescriptionSets( List<string> ctids, int relatedItemsLimit = 10 )
		{
			var queryData = new JObject()
			{
				{ "@type", "ceasn:CompetencyFramework" },
				{ "ceterms:ctid", JArray.FromObject( ctids ) }
			};

			var clientIP = "unknown";
			try
			{
				clientIP = HttpContext.Current.Request.UserHostAddress;
			}
			catch { }

			var resultData = DoQuery( queryData, 0, ctids.Count(), "", true, "https://credentialfinder.org/Finder/SearchViaRegistry/", clientIP, "Resource_RelatedURIs_Graph_RelatedData", true, relatedItemsLimit );

			var resultSet = new CTDLAPICompetencyFrameworkResultSet()
			{
				Results = ParseResults<CTDLAPICompetencyFrameworkResult>( resultData.data ),
				RelatedItems = resultData.extra.RelatedItems,
				RelatedItemsMap = resultData.extra.RelatedItemsMap,
				TotalResults = resultData.extra.TotalResults,
				Debug = resultData.extra.DebugInfo
			};

			return resultSet;
		}
		//

		//Bypass SPARQL and get multiple raw description sets using the new-new methods
		//Not sure if this needs to go through the accounts system somehow? Maybe logging at the end? We don't currently log the existing framework queries done via the finder.
		public static RegistryDescriptionSetResponse GetCompetencyFrameworkDescriptionSetsDirectlyFromRegistryAPI( List<string> ctids, bool includeResources = false, int relatedItemsLimit = 10, bool allowCache = true )
		{
			var result = new RegistryDescriptionSetResponse();
			result.Debug[ "CTIDs" ] = JArray.FromObject( ctids );
			var cacheURI = string.Join( ",", ctids.OrderBy( m => m ).ToList() );
			if ( allowCache )
			{
				//Try to get the data from the cache
				//TODO: Once the CTID bug is fixed, do this by CTID: https://github.com/CredentialEngine/CredentialRegistry/issues/399#issuecomment-782138617
				//For now just mash them all together
				var cachedData = MemoryCache.Default.Get( cacheURI );
				if ( cachedData != null )
				{
					result.Data = JObject.Parse( ( string ) cachedData ).ToObject<RegistryDescriptionSet>();
					result.Valid = true;
					return result;
				}
			}

			//Setup the request
			var requestData = new JObject()
			{
				{ "ctids", JArray.FromObject( ctids ) },
				{ "include_resources", includeResources }
			};

			//Indicate whether or not to include related resources for each branch of the description set
			//Use 0 to return empty arrays; useful if you just want the totals
			//Use -1 to return everything
			if( relatedItemsLimit > -1 )
			{
				requestData[ "per_branch_limit" ] = relatedItemsLimit;
			}

			//Setup the client
			var apiKey = ConfigHelper.GetConfigValue( "CredentialRegistryAuthorizationToken", "" );
			var directAPIendpoint = ConfigHelper.GetConfigValue( "GetRawDescriptionSetsByCTIDsDirectlyFromRegistryEndpoint", "" );
			var client = new HttpClient();
			client.DefaultRequestHeaders.TryAddWithoutValidation( "Authorization", "ApiToken " + apiKey );

			//Do the request
			var requestJSON = requestData.ToString( Formatting.None );
			var httpResult = client.PostAsync( directAPIendpoint, new StringContent( requestJSON, Encoding.UTF8, "application/json" ) ).Result;
			if ( httpResult.IsSuccessStatusCode )
			{
				//Read and store the raw result
				var rawResult = httpResult.Content.ReadAsStringAsync().Result;
				result.Debug[ "Raw Response" ] = rawResult;

				try
				{
					//Parse the result
					result.Data = JObject.Parse( rawResult ).ToObject<RegistryDescriptionSet>();
					result.Valid = true;

					//Cache the success
					MemoryCache.Default.Remove( cacheURI );
					MemoryCache.Default.Add( cacheURI, rawResult, DateTime.Now.AddMinutes( 15 ) );
				}
				catch( Exception e )
				{
					result.Valid = false;
					result.Messages.Add( "Error parsing response: " + e.Message );
				}
			}
			else
			{
				result.Valid = false;
				result.Messages.Add( "Error loading data: " + httpResult.ReasonPhrase );
				result.Messages.Add( "Error code: " + httpResult.StatusCode.ToString() );
			}

			return result;
		}

		//Use the Credential Registry to search for competency frameworks
		public static CTDLAPICompetencyFrameworkResultSet SearchViaRegistry( MainSearchInput data, bool asDescriptionSet = false )
		{
			data.Keywords = ( data.Keywords ?? "" ).Trim();

			//Handle blind searches
			var queryData = new JObject()
			{
				{ "@type", "ceasn:CompetencyFramework" }
			};

			//Otherwise, look for the value in various places
			if ( !string.IsNullOrWhiteSpace( data.Keywords ) )
			{
				//Normalize the text
				var normalized = Regex.Replace( Regex.Replace( data.Keywords.ToLower().Trim(), "[^a-z0-9-\" ]", " " ), " +", " " );
				var ctids = Regex.Matches( normalized, @"\b(ce-[a-f0-9-]{0,36})\b" ).Cast<Match>().Select( m => m.Value ).ToList();
				var wordsAndPhrases = normalized.Split( ' ' ).Where( m => !ctids.Contains( m.Replace( "\"", "" ) ) ).ToList();
				var keywords = string.Join( " ", wordsAndPhrases );

				//Basic skeleton
				queryData = new JObject()
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
				if( ctids.Count() > 0 )
				{
					var ctidList = JArray.FromObject( ctids );
					queryData[ "search:termGroup" ][ "ceterms:ctid" ] = ctidList;
					queryData[ "search:termGroup" ][ "ceasn:isPartOf" ][ "ceterms:ctid" ] = ctidList;
					queryData[ "search:termGroup" ][ "ceasn:creator" ][ "ceterms:ctid" ] = ctidList;
					queryData[ "search:termGroup" ][ "ceasn:publisher" ][ "ceterms:ctid" ] = ctidList;
				}

				//Add non-CTID-based query data
				if( keywords.Count() > 0 )
				{
					//Framework
					queryData[ "search:termGroup" ][ "ceasn:name" ] = keywords;
					queryData[ "search:termGroup" ][ "ceasn:description" ] = keywords;
					queryData[ "search:termGroup" ][ "ceasn:source" ] = keywords;
					//Creator
					queryData[ "search:termGroup" ][ "ceasn:creator" ][ "ceterms:name" ] = keywords;
					//queryData[ "search:termGroup" ][ "ceasn:creator" ][ "ceterms:subjectWebpage" ] = keywords;
					queryData[ "search:termGroup" ][ "ceasn:creator" ][ "search:operator" ] = "search:orTerms";
					//Publisher
					queryData[ "search:termGroup" ][ "ceasn:publisher" ][ "ceterms:name" ] = keywords;
					//queryData[ "search:termGroup" ][ "ceasn:publisher" ][ "ceterms:subjectWebpage" ] = keywords;
					queryData[ "search:termGroup" ][ "ceasn:publisher" ][ "search:operator" ] = "search:orTerms";
					//Competencies
					queryData[ "search:termGroup" ][ "ceasn:isPartOf" ][ "ceasn:competencyLabel" ] = keywords;
					queryData[ "search:termGroup" ][ "ceasn:isPartOf" ][ "ceasn:competencyText" ] = keywords;
					//queryData[ "search:termGroup" ][ "ceasn:isPartOf" ][ "ceasn:comment" ] = keywords;
					queryData[ "search:termGroup" ][ "ceasn:isPartOf" ][ "search:operator" ] = "search:orTerms";
				}

			}

			var skip = data.PageSize * (data.StartPage - 1);
			var take = data.PageSize;

			var clientIP = "unknown";
			try
			{
				clientIP = HttpContext.Current.Request.UserHostAddress;
			}
			catch { }

			var orderBy = "";
			var orderDescending = true;
			TranslateSortOrder(data.SortOrder, ref orderBy, ref orderDescending);

			var resultData = DoQuery(queryData, skip, take, orderBy, orderDescending, "https://credentialfinder.org/Finder/SearchViaRegistry/", clientIP, asDescriptionSet ? ( data.UseSPARQL ? "Resource" : "CompetencyFramework" ) : "", data.UseSPARQL);

			if (resultData.valid)
			{
				var resultSet = new CTDLAPICompetencyFrameworkResultSet()
				{
					Results = ParseResults<CTDLAPICompetencyFrameworkResult>( resultData.data ),
					RelatedItems = resultData.extra.RelatedItems,
					TotalResults = resultData.extra.TotalResults,
					RelatedItemsMap = resultData.extra.RelatedItemsMap,
					Debug = resultData.extra.DebugInfo
				};
				if ( !data.UseSPARQL )
				{
					try
					{
						var cacheSuccess = false;
						var cacheident = "";
						resultSet.PerResultRelatedItems = GetRelatedItemsForResults( resultData.data, resultData.extra.RelatedItems, data.Keywords == "search:anyValue", ref cacheSuccess, ref cacheident );
						resultSet.Debug = new JObject()
						{
							{ "Keywords", data.Keywords },
							{ "Use Cache", data.Keywords == "search:anyValue" },
							{ "Cache Succss", cacheSuccess },
							{ "Cache ID", cacheident }
						};
					}
					catch ( Exception ex )
					{
						resultSet.PerResultRelatedItems = new List<CTDLAPICompetencyFrameworkRelatedItemSetForSearchResult>()
						{
							new CTDLAPICompetencyFrameworkRelatedItemSetForSearchResult()
							{
								Competencies = new CTDLAPIRelatedItemForSearchResult()
								{
									Label = "Error loading competencies: " + ex.Message,
									Samples = new List<JObject>()
									{
										JObject.FromObject(ex)
									}
								}
							}
						};
					}
				}

				return resultSet;
			}
			else
			{
				var list = new List<CTDLAPICompetencyFrameworkResult>();
				var result = new CTDLAPICompetencyFrameworkResult()
				{
					Name = new LanguageMap("Error encountered"),
					Description = string.IsNullOrWhiteSpace(resultData.status) ? new LanguageMap("Sorry no useable message was returned.") : new LanguageMap(resultData.status)
				};
				list.Add(result);
				return new CTDLAPICompetencyFrameworkResultSet()
				{
					Results = list,
					RelatedItems = null,
					TotalResults = 0
				};
			}
		}
		//

		public static List<FrameworkSearchItem> ThreadedFrameworkSearch(List<FrameworkSearchItem> searchItems)
		{
			var itemSet = new FrameworkSearchItemSet() { Items = searchItems };
			//Trigger the threads
			foreach (var searchItem in itemSet.Items)
			{
				//Set this here to avoid any potential race conditions with the WaitUntiLAllAreFinished method
				searchItem.IsInProgress = true;
				WaitCallback searchMethod = StartFrameworkSearchThread;
				ThreadPool.QueueUserWorkItem(searchMethod, searchItem);
			}
			//Wait for them all to finish
			itemSet.WaitUntilAllAreFinished();

			//Return results
			return itemSet.Items;
		}
		private static void StartFrameworkSearchThread(object frameworkSearchItem)
		{
			//Cast the type and do the search
			var searchItem = (FrameworkSearchItem)frameworkSearchItem;
			try
			{
				var total = 0;
				searchItem.Results = searchItem.ProcessMethod.Invoke(searchItem.CompetencyCTIDs, searchItem.SkipResults, searchItem.TakeResults, ref total, searchItem.ClientIP);
				searchItem.TotalResults = total;
			}
			catch { }
			//When finished, set the variable that will be checked by the FrameworkSearchItemSet.WaitUntilAllAreFinished method
			searchItem.IsInProgress = false;
		}
		//

		public static List<JObject> GetCredentialsForCompetencies(List<string> competencyCTIDs, int skip, int take, ref int totalResults, string clientIP = null)
		{
			var competencies = new JArray(competencyCTIDs.ToArray());
			var queryData = new JObject()
			{
				//TODO: may need to include the list of credential types here (as a parameter) - probably not necessary?
				//Find anything that requires...
				{ "ceterms:requires", new JObject()
				{
					//A target competency with a CTID that matches, or
					{ "ceterms:targetCompetency", new JObject()
					{
						{ "ceterms:targetNode", new JObject() {
							{ "ceterms:ctid", competencies }
						} }
					} },
					//A target assessment that assesses a competency with a CTID that matches, or
					{ "ceterms:targetAssessment", new JObject()
					{
						{ "ceterms:assesses", new JObject()
						{
							{ "ceterms:targetNode", new JObject()
							{
								{ "ceterms:ctid", competencies }
							} }
						} }
					} },
					//A target learning opportunity that teaches a competency with a CTID that matches
					{ "ceterms:targetLearningOpportunity", new JObject()
					{
						{ "ceterms:teaches", new JObject()
						{
							{ "ceterms:targetNode", new JObject()
							{
								{ "ceterms:ctid", competencies }
							} }
						} }
					} },
					{ "search:operator", "search:orTerms" }
				} }
			};

			return DoSimpleQuery(queryData, skip, take, "", true, ref totalResults, "https://credentialfinder.org/Finder/GetCredentialsForCompetencies/", clientIP);
		}
		//

		public static List<JObject> GetAssessmentsForCompetencies(List<string> competencyCTIDs, int skip, int take, ref int totalResults, string clientIP = null)
		{
			var competencies = new JArray(competencyCTIDs.ToArray());
			var queryData = new JObject()
			{
				{ "ceterms:assesses", new JObject()
				{
					{ "ceterms:targetNode", new JObject()
					{
						{ "ceterms:ctid", competencies }
					} }
				} }
			};

			return DoSimpleQuery(queryData, skip, take, "", true, ref totalResults, "https://credentialfinder.org/Finder/GetAssessmentsForCompetencies/", clientIP);
		}
		//

		public static List<JObject> GetLearningOpportunitiesForCompetencies(List<string> competencyCTIDs, int skip, int take, ref int totalResults, string clientIP = null)
		{
			var competencies = new JArray(competencyCTIDs.ToArray());
			var queryData = new JObject()
			{
				{ "ceterms:teaches", new JObject()
				{
					{ "ceterms:targetNode", new JObject()
					{
						{ "ceterms:ctid", competencies }
					} }
				} }
			};

			return DoSimpleQuery(queryData, skip, take, "", true, ref totalResults, "https://credentialfinder.org/Finder/GetLearningOpportunitiesForCompetencies/", clientIP);
		}
		//

		/*
		/// <summary>
		/// Update totals related to competency frameworks
		/// </summary>
		/// <param name="usingCFTotals">If true, the totals will be retrieved using "@type","ceasn:CompetencyFramework", otherwise use "@type","ceasn:Competency" in searches</param>
		public void UpdateCompetencyFrameworkReportTotals(bool usingCFTotals = true, bool includingRelationships = true)
		{
			LoggingHelper.DoTrace( 5, thisClassName + ".UpdateCompetencyFrameworkReportTotals started" );
			var mgr = new CodesManager();
			//bool usingCFTotals = true;
			try
			{
				var total = GetCompetencyFrameworkTermTotal( null );
				if ( total > 0 )
				{
					mgr.UpdateEntityTypes( 10, total, false );
				}
				else
				{

				}
				mgr.UpdateEntityStatistic( 10, "frameworkReport:Competencies", GetCompetencyTermTotal( null ), false );

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
			catch (Exception ex)
			{
				LoggingHelper.LogError(ex, "Services.UpdateCompetencyFrameworkReportTotals");
			}

			LoggingHelper.DoTrace( 5, thisClassName + ".UpdateCompetencyFrameworkReportTotals completed" );
		}
		//
		public int GetCompetencyFrameworkTermTotal(string searchTerm)
		{
			bool useSparQL = UtilityManager.GetAppKeyValue( "usingSparQLForSearch", false );
			var queryData = new JObject()
			{
				//Get competency frameworks...
                { "@type","ceasn:CompetencyFramework" },
			};
			if (!string.IsNullOrWhiteSpace(searchTerm))
			{
				queryData.Add(searchTerm, "search:anyValue");
			}

			var resultData = DoQuery(queryData, 0, 1, "", true, "https://credentialfinder.org/Finder/GetCompetencyTermTotal/", null, null, useSparQL);
			return resultData.extra.TotalResults;
		}

		//
		public int GetCompetencyTermTotal(string searchTerm)
		{
			var queryData = new JObject()
			{
				//Get competency frameworks...
                { "@type","ceasn:Competency" },
			};
			if (!string.IsNullOrWhiteSpace(searchTerm))
			{
				queryData.Add(searchTerm, "search:anyValue");
			}

			var resultData = DoQuery(queryData, 0, 1, "", true, "https://credentialfinder.org/Finder/GetCompetencyTermTotal/", null, null, true);
			return resultData.extra.TotalResults;
		}
		//
		public int GetCompetencyFrameworksWithCompetencyTermTotal(string searchTerm)
		{
			if (string.IsNullOrWhiteSpace(searchTerm))
				return 0;
			//var termData = new JObject()
			//{
			//	//TODO: may need to include the list of credential types here (as a parameter) - probably not necessary?
			//	//Find anything that requires...
			//	{ "ceasn:isPartOf", new JObject()
			//		{
			//			{ searchTerm, "search:anyValue" }
			//		}
			//	}
			//};
			var queryData = new JObject()
			{
				//Get competency frameworks...
                { "@type","ceasn:CompetencyFramework" },
			};
			queryData.Add("ceasn:isPartOf", new JObject()
					{
						{ searchTerm, "search:anyValue" }
					});


			//string query = "@type\":\"ceasn:CompetencyFramework\",\"ceasn:isPartOf\": {\"" + searchTerm + "\": \"search:anyValue\"}";

			var resultData = DoQuery(queryData, 0, 1, "", true, "https://credentialfinder.org/Finder/GetCompetencyTermTotal/");
			return resultData.extra.TotalResults;
		}
		//
		*/

		private static List<JObject> DoSimpleQuery(JObject queryData, int skip, int take, string orderBy, bool orderDescending, ref int totalResults, string referrer = null, string clientIP = null)
		{
			take = take == 0 ? 20 : take;
			var resultData = DoQuery(queryData, skip, take, orderBy, orderDescending, "https://credentialfinder.org/Finder/GetLearningOpportunitiesForCompetencies/", clientIP);
			try
			{
				totalResults = resultData.extra.TotalResults;
				return resultData.data.ToObject<List<JObject>>();
			}
			catch
			{
				return new List<JObject>();
			}
		}
		//

		private static void Log( string text )
		{
			try
			{
				//System.IO.File.AppendAllText( "C:/@logs/finderlogtemp.txt", text + "\r\n" );
			}
			catch { }
		}
		private static CTDLAPIJSONResponse DoQuery(JObject query, int skip, int take, string orderBy, bool orderDescending, string referrer = null, string clientIP = null, string descriptionSetType = null, bool useSPARQL = false, int relatedItemsLimit = 10)
		{
			var testGUID = Guid.NewGuid().ToString();
			var queryWrapper = new JObject()
			{
				{ "Query", query },
				{ "Skip", skip },
				{ "Take", take },
				{ "OrderBy", orderBy },
				{ "OrderDescending", orderDescending },
				{ "IncludeDebugInfo", true }
			};
			if (!string.IsNullOrWhiteSpace(descriptionSetType))
			{
				queryWrapper["DescriptionSetType"] = descriptionSetType;
			}
			if( relatedItemsLimit > 0 ) //Enable using 0 to turn off the limit
			{
				queryWrapper[ "DescriptionSetRelatedURIsLimit" ] = relatedItemsLimit;
				queryWrapper[ "DescriptionSetRelatedItemsLimit" ] = relatedItemsLimit;
			}

			//Get API key and URL
			var apiKey = ConfigHelper.GetConfigValue("MyCredentialEngineAPIKey", "");
			var apiURL = ConfigHelper.GetConfigValue("AssistantCTDLJSONSearchAPIUrl", "");

			Log( "-----" );
			//Testing the SPARQL query stuff
			if ( useSPARQL )
			{
				apiURL = apiURL.Replace( "/ctdl", "/ctdltosparql" );
				Log( "Test 1" );
				Log( apiURL );
				Log( apiKey );
			}

			//Make it a little easier to track the source of the requests
			referrer = (string.IsNullOrWhiteSpace(referrer) ? "https://credentialfinder.org/Finder/" : referrer);
			try
			{
				referrer = referrer + "?ClientIP=" + (string.IsNullOrWhiteSpace(clientIP) ? HttpContext.Current.Request.UserHostAddress : clientIP);
			}
			catch
			{
				referrer = referrer + "?ClientIP=unknown"; //It seems HttpContext.Current.Request.UserHostAddress might only be available if passed in from the calling thread?
			}

			//Do the query
			Log( "Query" );
			Log( queryWrapper.ToString() );
			Log( "" );
			Log( "Test 2" );
			var queryJSON = JsonConvert.SerializeObject(queryWrapper);
			var client = new HttpClient();
			client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "ApiToken " + apiKey);
			client.DefaultRequestHeaders.Referrer = new Uri(referrer);
			client.Timeout = new TimeSpan( 0, 10, 0 );
			var result = client.PostAsync(apiURL, new StringContent(queryJSON, Encoding.UTF8, "application/json")).Result;
			var rawResultData = result.Content.ReadAsStringAsync().Result ?? "{}";

			Log( "Raw result:" );
			Log( rawResultData );
			var resultData = JsonConvert.DeserializeObject<CTDLAPIJSONResponse>(rawResultData, new JsonSerializerSettings()
			{
				//Ignore errors
				Error = delegate (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs e) {
					Log( "Error:" );
					Log( e.ToString() );
					e.ErrorContext.Handled = true;
				}
			}) ?? new CTDLAPIJSONResponse();

			return resultData;
		}
		//


		private static List<T> ParseResults<T>(JArray items) where T : new()
		{
			var properties = typeof(T).GetProperties();
			var result = new List<T>();
			if (items == null || items.Count == 0)
				return result;

			foreach (var item in items)
			{
				try
				{
					var converted = item.ToObject<T>();
					try
					{
						properties.FirstOrDefault(m => m.Name == "RawData").SetValue(converted, item.ToString(Formatting.None));
					}
					catch { }
					result.Add(converted);
				}
				catch { }
			}
			return result;
		}
		//

		private static void TranslateSortOrder(string searchSortOrder, ref string orderBy, ref bool orderDescending)
		{
			switch (searchSortOrder)
			{
				case "alpha":
				{
					orderBy = "name";
					orderDescending = false;
					break;
				}
				case "newest":
				{
					orderBy = "updated";
					orderDescending = true;
					break;
				}
				case "relevance":
				{
					orderBy = "relevance";
					orderDescending = true;
					break;
				}
				default:
				{
					orderBy = "";
					orderDescending = true;
					break;
				}
			}
		}

		private class CTDLAPIJSONResponse
		{
			public CTDLAPIJSONResponse()
			{
				data = new JArray();
				extra = new CTDLAPIJsonResponseExtra();
			}
			public JArray data { get; set; }
			public CTDLAPIJsonResponseExtra extra { get; set; }
			public bool valid { get; set; }
			public string status { get; set; }
		}
		private class CTDLAPIJsonResponseExtra
		{
			public CTDLAPIJsonResponseExtra()
			{
				RelatedItems = new JArray();
			}
			public int TotalResults { get; set; }
			public JArray RelatedItems { get; set; }
			public JArray RelatedItemsMap { get; set; }
			public JObject DebugInfo { get; set; }
		}
		//
		#endregion



		public static List<CTDLAPICompetencyFrameworkRelatedItemSetForSearchResult> GetRelatedItemsForResults(JArray rawResults, JArray rawRelatedItems, bool useCache, ref bool cacheSuccess, ref string cacheident)
		{

			//Get List<JObject> for the two arrays
			var relatedItemSets = new List<CTDLAPICompetencyFrameworkRelatedItemSetForSearchResult>();
			var results = rawResults.ToList().ConvertAll(m => (JObject)m).ToList();

			//Skip the hard part if possible
			var cacheID = "CompetencyFrameworkCache_" + string.Join(",", results.Select(m => (string)m["ceterms:ctid"] ?? "").ToList());
			cacheident = cacheID;
			cacheSuccess = false;
			var cache = MemoryCache.Default;
			if (useCache)
			{
				var data = cache[cacheID];
				if (data != null)
				{
					cacheSuccess = true;
					return JsonConvert.DeserializeObject<List<CTDLAPICompetencyFrameworkRelatedItemSetForSearchResult>>((string)data);
				}
			}

			var relatedItems = rawRelatedItems.ToList().ConvertAll(m => (JObject)m).ToList();

			//Hold onto these for later
			var credentialTypes = new List<string>() { "ceterms:ApprenticeshipCertificate", "ceterms:AssociateDegree", "ceterms:BachelorDegree", "ceterms:Badge", "ceterms:Certificate", "ceterms:Certification", "ceterms:Degree", "ceterms:DigitalBadge", "ceterms:Diploma", "ceterms:DoctoralDegree", "ceterms:GeneralEducationDevelopment", "ceterms:JourneymanCertificate", "ceterms:License", "ceterms:MasterCertificate", "ceterms:MasterDegree", "ceterms:MicroCredential", "ceterms:OpenBadge", "ceterms:ProfessionalDoctorate", "ceterms:QualityAssuranceCredential", "ceterms:ResearchDoctorate", "ceterms:SecondarySchoolDiploma" }; //Should probably retrieve this dynamically
			var connectionProperties = new List<string>() { "ceasn:creator", "ceasn:publisher", "ceasn:isPartOf", "ceasn:abilityEmbodied", "ceasn:skillEmbodied", "knowledgeEmbodied", "skos:inScheme", "ceterms:targetNode", "ceterms:targetCredential", "ceterms:targetAssessment", "ceterms:targetLearningOpportunity" };
			var alignmentProperties = new List<string>() { "ceasn:alignFrom", "ceasn:alignTo", "ceasn:broadAlignment", "ceasn:exactAlignment", "ceasn:narrowAlignment", "ceasn:majorAlignment", "ceasn:minorAlignment", "ceasn:prerequisiteAlignment" };
			var conceptProperties = new List<string>() { "ceasn:conceptTerm", "ceasn:complexityLevel" };
			var associativeProperties = connectionProperties.Concat(alignmentProperties).Concat(conceptProperties).ToList();
			var tripleProperties = typeof(RDFTriple).GetProperties();
			var subjectURIProperty = tripleProperties.FirstOrDefault(m => m.Name == "SubjectURI");
			var objectURIProperty = tripleProperties.FirstOrDefault(m => m.Name == "ObjectURI");

			//Build triples
			var allItems = results.Concat(relatedItems).ToList();
			var triples = new List<RDFTriple>();
			foreach (var item in allItems)
			{
				var label = GetEnglish(item["ceterms:name"] ?? item["ceasn:name"] ?? item["ceasn:competencyLabel"] ?? item["ceasn:competencyText"] ?? item["skos:prefLabel"] ?? "Item");
				foreach (var property in item.Properties())
				{
					var path = new List<string>() { property.Name };
					RecursivelyExtractTriples(property.Name, property.Value, path, (string)item["@type"], (string)item["@id"], label, (string)item["ceterms:ctid"], triples, associativeProperties);
				}
			}

			//Handle results
			var allCredentialTriples = triples.Where(m => credentialTypes.Contains(m.SubjectType)).ToList();
			var allConceptSchemeTriples = triples.Where(m => m.SubjectType == "skos:ConceptScheme").ToList();
			foreach (var result in results)
			{
				var debug = "";

				//Triples for the Framework
				var resultURI = result["@id"].ToString();
				var outgoingTriples = triples.Where(m => m.SubjectURI == resultURI).ToList();
				var incomingTriples = triples.Where(m => m.ObjectURI == resultURI).ToList();
				debug += "Outgoing Triples: " + outgoingTriples.Count() + "\n";
				debug += "Incoming Triples: " + incomingTriples.Count() + "\n";

				//Competencies and triples for the Competencies
				var competencyURIs = incomingTriples.Where(m => m.Path.Contains("ceasn:isPartOf")).Select(m => m.SubjectURI).ToList();
				var competencies = relatedItems.Where(m => competencyURIs.Contains((string)m["@id"] ?? "")).ToList();
				var outgoingCompetencyTriples = triples.Where(m => competencyURIs.Contains(m.SubjectURI)).ToList();
				var incomingCompetencyTriples = triples.Where(m => competencyURIs.Contains(m.ObjectURI)).ToList();
				debug += "Outgoing Competency Triples: " + outgoingCompetencyTriples.Count() + "\n";
				debug += "Incoming Competency Triples: " + incomingCompetencyTriples.Count() + "\n";

				//Framework-level relationships
				var publishers = FindRelated(objectURIProperty, outgoingTriples, delegate (RDFTriple m) { return m.Path.Contains("ceasn:publisher"); }, relatedItems);
				var creators = FindRelated(objectURIProperty, outgoingTriples, delegate (RDFTriple m) { return m.Path.Contains("ceasn:creator"); }, relatedItems);
				var outAlignedFrameworks = FindRelated(objectURIProperty, outgoingTriples, delegate (RDFTriple m) { return m.Path.Intersect(alignmentProperties).Count() > 0; }, relatedItems);
				var inAlignedFrameworks = FindRelated(objectURIProperty, incomingTriples, delegate (RDFTriple m) { return m.SubjectType == "ceasn:CompetencyFramework"; }, relatedItems);

				//Competency-level relationships
				var assessments = FindRelated(subjectURIProperty, incomingCompetencyTriples, delegate (RDFTriple m) { return m.SubjectType == "ceterms:AssessmentProfile"; }, relatedItems);
				var assessmentURIs = assessments.Select(m => (string)m["@id"] ?? "").ToList();
				debug += "Assessments: " + assessments.Count() + "\n";
				debug += "Subject URI Property: " + subjectURIProperty.Name + "\n";
				debug += "Related Items: " + relatedItems.Count();
				debug += "Test: " + FindRelated(subjectURIProperty, incomingCompetencyTriples, delegate (RDFTriple m) { return true; }, relatedItems).Count();
				var learningOpportunities = FindRelated(subjectURIProperty, incomingCompetencyTriples, delegate (RDFTriple m) { return m.SubjectType == "ceterms:LearningOpportunityProfile"; }, relatedItems);
				var learningOpportunityURIs = learningOpportunities.Select(m => (string)m["@id"] ?? "").ToList();
				var credentials = FindRelated(subjectURIProperty, incomingCompetencyTriples, delegate (RDFTriple m) { return credentialTypes.Contains(m.SubjectType); }, relatedItems);
				var credentialsViaAssessments = FindRelated(subjectURIProperty, allCredentialTriples, delegate (RDFTriple m) { return assessmentURIs.Contains(m.ObjectURI); }, relatedItems);
				var credentialsViaLearningOpportunities = FindRelated(subjectURIProperty, allCredentialTriples, delegate (RDFTriple m) { return learningOpportunityURIs.Contains(m.ObjectURI); }, relatedItems);
				var concepts = FindRelated(objectURIProperty, outgoingCompetencyTriples, delegate (RDFTriple m) { return m.Path.Intersect(conceptProperties).Count() > 0; }, relatedItems);
				var conceptSchemeURIs = concepts.Select(m => (string)m["skos:inScheme"] ?? "").Distinct().ToList();
				var conceptSchemes = FindRelated(subjectURIProperty, allConceptSchemeTriples, delegate (RDFTriple m) { return conceptSchemeURIs.Contains(m.SubjectURI); }, relatedItems);
				var outAlignedCompetencies = FindRelated(objectURIProperty, outgoingCompetencyTriples, delegate (RDFTriple m) { return m.Path.Intersect(alignmentProperties).Count() > 0; }, relatedItems);
				var inAlignedCompetencies = FindRelated(subjectURIProperty, incomingCompetencyTriples, delegate (RDFTriple m) { return m.Path.Intersect(alignmentProperties).Count() > 0; }, relatedItems);

				//Store the data
				var dataSet = new CTDLAPICompetencyFrameworkRelatedItemSetForSearchResult()
				{
					RelatedItemsForCTID = (string)result["ceterms:ctid"],
					Publishers = new CTDLAPIRelatedItemForSearchResult("ceasn:publisher", "# Publishers", publishers),
					Creators = new CTDLAPIRelatedItemForSearchResult("ceasn:creator", "# Creators", creators),
					Owners = new CTDLAPIRelatedItemForSearchResult("meta:owner", "# Owners", GetDistinctObjects(publishers.Concat(creators).ToList())),
					Competencies = new CTDLAPIRelatedItemForSearchResult("ceasn:Competency", "# Competencies", competencies),
					Credentials = new CTDLAPIRelatedItemForSearchResult("ceterms:Credential", "# Credentials", GetDistinctObjects(credentials.Concat(credentialsViaAssessments).Concat(credentialsViaLearningOpportunities).ToList())),
					Assessments = new CTDLAPIRelatedItemForSearchResult("ceterms:Assessment", "# Assessments", assessments),
					LearningOpportunities = new CTDLAPIRelatedItemForSearchResult("ceterms:LearningOpportunity", "# Learning Opportunities", learningOpportunities),
					ConceptSchemes = new CTDLAPIRelatedItemForSearchResult("skos:ConceptScheme", "# Concept Schemes", conceptSchemes),
					Concepts = new CTDLAPIRelatedItemForSearchResult("skos:Concept", "# Concepts", concepts),
					AlignedFrameworks = new CTDLAPIRelatedItemForSearchResult("meta:AlignedFramework", "# Aligned Frameworks", GetDistinctObjects(outAlignedFrameworks.Concat(inAlignedFrameworks).ToList())),
					AlignedCompetencies = new CTDLAPIRelatedItemForSearchResult("meta:AlignedCompetency", "# Aligned Competencies", GetDistinctObjects(outAlignedCompetencies.Concat(inAlignedCompetencies).ToList()))
				};
				relatedItemSets.Add(dataSet);
			}

			//Skip the hard part next time, if applicable
			if (useCache)
			{
				cache.Remove(cacheID);
				cache.Add(cacheID, JsonConvert.SerializeObject(relatedItemSets), new CacheItemPolicy() { AbsoluteExpiration = DateTime.Now.AddMinutes(15) });
			}

			return relatedItemSets;
		}
		private static void RecursivelyExtractTriples(string name, JToken value, List<string> path, string _type, string _id, string label, string ctid, List<RDFTriple> triples, List<string> associativeProperties)
		{
			if (value.Type == JTokenType.Array)
			{
				foreach (var itemValue in ((JArray)value))
				{
					RecursivelyExtractTriples(name, itemValue, path, _type, _id, label, ctid, triples, associativeProperties);
				}
			}
			else if (value.Type == JTokenType.Object)
			{
				foreach (var property in ((JObject)value).Properties())
				{
					var newPath = path.Concat(new List<string>() { property.Name }).ToList(); //Don't overwrite the original path, since it needs to branch for different parts of the recursion
					RecursivelyExtractTriples(property.Name, property.Value, newPath, _type, _id, label, ctid, triples, associativeProperties);
				}
			}
			else
			{
				if (associativeProperties.Contains(name))
				{
					triples.Add(new RDFTriple()
					{
						SubjectType = _type,
						SubjectURI = _id,
						SubjectLabel = label,
						SubjectCTID = ctid,
						Path = path,
						ObjectURI = value.ToString()
					});
				}
			}
		}
		private static List<JObject> FindRelated(PropertyInfo desiredURIProperty, List<RDFTriple> lookIn, Func<RDFTriple, bool> matchFunction, List<JObject> relatedItems)
		{
			var matchingURIs = lookIn.Where(m => matchFunction(m)).Select(m => desiredURIProperty.GetValue(m).ToString()).ToList();
			return relatedItems.Where(m => matchingURIs.Contains((string)m["@id"] ?? "")).ToList();
		}
		private static List<JObject> GetDistinctObjects(List<JObject> items)
		{
			var results = new List<JObject>();
			var uniqueURIs = items.Select(m => (string)m["@id"] ?? "").Distinct().ToList();
			foreach (var uri in uniqueURIs)
			{
				results.Add(items.FirstOrDefault(m => ((string)m["@id"] ?? "") == uri));
			}
			return results;
		}
		public static string GetEnglish(JToken data)
		{
			if (data == null)
			{
				return "";
			}
			else if (data.Type == JTokenType.String)
			{
				return data.ToString();
			}
			else if (data.Type == JTokenType.Object)
			{
				return (string)(data["en"] ?? data["en-us"] ?? data["en-US"]) ?? "";
			}
			else
			{
				return "";
			}
		}
		public class RDFTriple
		{
			public string SubjectType { get; set; }
			public string SubjectURI { get; set; }
			public string SubjectLabel { get; set; }
			public string SubjectCTID { get; set; }
			public List<string> Path { get; set; }
			public string ObjectURI { get; set; }
		}
	}
}

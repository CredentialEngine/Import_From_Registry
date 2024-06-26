using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
//using System.Web.UI;
//using System.Xml;
using Elasticsearch.Net;
using Nest;
using Nest.JsonNetSerializer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using workIT.Factories;
using workIT.Models;
using workIT.Models.Common;
using workIT.Models.Elastic;
using workIT.Models.ProfileModels;
using workIT.Models.Search;
using workIT.Utilities;
using PM = workIT.Models.ProfileModels;

namespace workIT.Services
{
    public class ElasticServices
	{
		public static string thisClassName = "ElasticServices";
		public static string enviroment = UtilityManager.GetAppKeyValue( "environment" );
        public static int nonCredentialPageSize = UtilityManager.GetAppKeyValue( "nonCredentialPageSize", 250 );
        private static double negativeBooster = .2d;
		JavaScriptSerializer serializer = new JavaScriptSerializer();
		//common queries - remove these as being static can make them 'sticky'
		//static QueryContainer createdFromQuery = null;
		//static QueryContainer createdToQuery = null;
		//static QueryContainer historyFromQuery = null;
		//static QueryContainer historyToQuery = null;



		#region Elastic related properties
		public static string CommonIndex
		{
			get { return UtilityManager.GetAppKeyValue( "commonCollection", "common" ); }
		}
		public static string CredentialIndex
		{
			get { return UtilityManager.GetAppKeyValue( "credentialCollection", "credentials" ); }
		}

		public static string OrganizationIndex
		{
			//putting all in the same index
			get { return UtilityManager.GetAppKeyValue( "organizationCollection", "organizations" ); }

		}
		public static string AssessmentIndex
		{
			get { return UtilityManager.GetAppKeyValue( "assessmentCollection", "assessments" ); }
		}
		public static string LearningOppIndex
		{
			get { return UtilityManager.GetAppKeyValue( "learningOppCollection", "learningopps" ); }
		}

		public static string CompetencyFrameworkIndex
		{
			get { return UtilityManager.GetAppKeyValue( "competencyFrameworkCollection", "competency_frameworks" ); }
		}

		public static string PathwayIndex
		{
			//get { return UtilityManager.GetAppKeyValue( "pathwayCollection", "pathway" ); }
			get { return UtilityManager.GetAppKeyValue( "pathwayCollection", "pathway_index" ); }
		}
		private static ElasticClient EC
		{
			get
			{
				var url = UtilityManager.GetAppKeyValue( "elasticSearchUrl", "http://localhost:9207" );
				var uri = new Uri( url );
				var pool = new SingleNodeConnectionPool( new Uri( url ) );
				//OLD
				var settingsOld = new ConnectionSettings( uri ).DefaultIndex( CredentialIndex );
				//     Ensures the response bytes are always available on the Elasticsearch.Net.ElasticsearchResponse`1
				//     IMPORTANT: Depending on the registered serializer, this may cause the response
				//     to be buffered in memory first, potentially affecting performance.
				settingsOld.DisableDirectStreaming(); //defaults to true
				var clientOld = new ElasticClient( settingsOld );
				//23-02-15 new to enable index with JObject
				var connectionSettings = new ConnectionSettings( pool, JsonNetSerializer.Default );
				//TBD The following seems to result in not being able to see the elastic query in search.DebugInformation
				//23-03-08 setting to true. Can see the query again. 
				//connectionSettings.DisableDirectStreaming( false );
				connectionSettings.DisableDirectStreaming( true );
				var client = new ElasticClient( connectionSettings );
				return client;
			}
		}

		//public static QueryContainer countryQuery { get; private set; }

		//static ElasticClient client;

		//private static ElasticClient GetClient()
		//{
		//	if ( client == null )
		//	{

		//		var url = UtilityManager.GetAppKeyValue( "elasticSearchUrl", "http://localhost:9200" );
		//		var uri = new Uri( url );
		//		var settings = new ConnectionSettings( uri ).DefaultIndex( CredentialIndex );
		//		settings.DisableDirectStreaming();
		//		client = new ElasticClient( settings );
		//	}

		//	return client;
		//}
		#endregion


		#region General/Common Search

		public static List<string> GeneralAutoComplete( int entityTypeId, string entityType, MainSearchInput query, int maxTerms, ref int pTotalRows )
		{
			if ( UtilityManager.GetAppKeyValue( "includingAllFiltersWithAutocomplete", false ) )
			{
				List<string> autocompleteList = new List<string>();

				//test just calling the related main search and returning the terms
				GeneralSearch( entityTypeId, entityType, query, ref pTotalRows, ref autocompleteList, true );
				return autocompleteList;
			}

			var keywords = query.Keywords;
			var search = EC.Search<GenericIndex>( i => i.Index( CommonIndex ).Query( q => q.MultiMatch( m => m
						   .Fields( f => f
							   .Field( ff => ff.Name )
							   .Field( ff => ff.Description )
						   )
						   //.Operator( Operator.Or )
						   .Type( TextQueryType.PhrasePrefix )
						   .Query( keywords )
						   .MaxExpansions( 10 ) ) ).Size( maxTerms * 2 ) );

			pTotalRows = ( int ) search.Total;
			var list = ( List<GenericIndex> ) search.Documents;
			//why was this using object?
			//return list.Select( x => x.Name ).Distinct().Take( maxTerms ).ToList().Cast<object>().ToList();
			return list.Select( m => m.ListTitle ).Distinct().Take( maxTerms ).ToList();
		}
		/// <summary>
		/// Proxy for full search
		/// </summary>
		/// <param name="query"></param>
		/// <param name="pTotalRows"></param>
		/// <returns></returns>
		public static List<CommonSearchSummary> GeneralSearch( int entityTypeId, string entityType, MainSearchInput query, ref int pTotalRows )
		{
			List<string> autocompleteList = new List<string>();
			bool requestIsAutocomplete = false;
			return GeneralSearch( entityTypeId, entityType, query, ref pTotalRows, ref autocompleteList, requestIsAutocomplete );
		}
		public static List<CommonSearchSummary> GeneralSearch( int entityTypeId, string entityType, MainSearchInput query, ref int pTotalRows, ref List<string> autocompleteList, bool requestIsAutocomplete = false )
		{
			//? actually cannot do here, as would have to build the complete common index, or should call General_BuildIndexForCredentialingAction
			//would need a means to query for any data, or always assume done in deug mode?
			//user would set false to true
			General_BuildIndexForCredentialingAction( false );
			//General_BuildIndex();

			var list = new List<CommonSearchSummary>();
			QueryContainer accommodationTypeQuery = null;
			QueryContainer deliveryTypeQuery = null;
			QueryContainer supportServiceCategoryQuery = null;
			QueryContainer collectionCategoryTypeQuery = null;
			QueryContainer classificationsQuery = null;
			QueryContainer entityTypeQuery = null;
			QueryContainer industriesQuery = null;
			QueryContainer keywordsQuery = null;
			QueryContainer lifeCycleStatusTypeQuery = null;
			QueryContainer occupationsQuery = null;
			QueryContainer ownedByQuery = null;
			QueryContainer qaFilterQuery = null;
			QueryContainer commonOrgRolesBYQuery = null;
			QueryContainer publishedBYQuery = null;
			QueryContainer reportsQuery = null;
			QueryContainer subjectsQuery = null;
			QueryContainer transferIntermediaryIdQuery = null;
			QueryContainer hasTransferValueForQuery = null;
			QueryContainer hasTransferValueFromQuery = null;
			QueryContainer ctidQuery = null;
			//QueryContainer widgetIdQuery = null;
			QueryContainer widgetOccupationsQuery = null;
			QueryContainer windustriesQuery = null;
			//QueryContainer wsubjectsQuery = null;
			HistoryQueryFilters history = new HistoryQueryFilters();
			var accommondationTypeIds = new List<int>();
			var deliveryTypeIds = new List<int>();
			var lifeCycleStatusTypeIds = new List<int>();
			var supportServiceCategoryIds = new List<int>();
			var relationshipTypeIds = new List<int>();
			QueryContainer competenciesQuery = null;
			QueryContainer commonLeftRailOrgRolesBYQuery = null;


			if ( entityTypeId > 0 )
				entityTypeQuery = Query<GenericIndex>.Match( ts => ts.Field( f => f.EntityTypeId ).Query( entityTypeId.ToString() ) );

			if ( entityTypeId == 26 )
			{
				//test with hard code
				var orgId = 1314;
				//				ownedByQuery = Query<GenericIndex>.Match( ts => ts.Field( f => f.PrimaryOrganizationId ).Query( orgId.ToString() ) );
			}


			if ( query.StartPage == 1 )
			{

			}

			//
			#region properties
			if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CODE ) )
			{
				//collection: 85,
				//TVP: ??
				//TI: ??
				//conceptScheme: ??
				//
				var collectionCategoryTypeIds = new List<int>();
				var reportIds = new List<int>();

				//TODO watch for relationshipIds being passed for two filters
				foreach ( var filter in query.FiltersV2.Where( m => m.Type == MainSearchFilterV2Types.CODE ).ToList() )
				{
					var item = filter.AsCodeItem();
					if ( filter.Name == "reports" || filter.Name == "otherfilters" || filter.Name == "filter:OtherFilters" )
					{
						reportIds.Add( item.Id ); //can probably continue after here?
						continue;
					}
					else if ( filter.Name == "" && filter.Values != null && filter.Values.Count == 3 )
					{
						//HACK test while determine why no filter.name
						//var filterItem = filter.AsDefaultCodeItem();
						//if ( item.CategoryId == 1 || item.CategoryId == 58 )
						//{
						//	reportIds.Add( item.Id ); //can probably continue after here?
						//	continue;
						//}
					}

					if ( filter.Name == "history" )
					{
						continue;
					}

					if ( item == null || item.CategoryId < 1 )
					{
						//check for text only 
						var text = filter.AsText();
						if ( !string.IsNullOrWhiteSpace( text ) )
						{
							//what? if no keywords, then append. Or append?
							if ( string.IsNullOrWhiteSpace( query.Keywords ) )
								query.Keywords = text;
							else
								query.Keywords += " " + text;
						}
						continue;
					}

					//if ( credSearchCategories.Contains( item.CategoryId ) )
					//{ }
					if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_COLLECTION_CATEGORY )
						collectionCategoryTypeIds.Add( item.Id );

					else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS )
						lifeCycleStatusTypeIds.Add( item.Id );
					else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_ACCOMMODATION )
						accommondationTypeIds.Add( item.Id );
					else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE )
						deliveryTypeIds.Add( item.Id );
					else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_SUPPORT_SERVICE_CATEGORY )
						supportServiceCategoryIds.Add( item.Id );
					//no not here
					//else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE )
					//{
					//	//will need to watch 
					//	if ( filter.Name == "filter:organizationnonqaroles" )
					//	{
					//		//what name do we have?
					//		relationshipTypeIds.Add( item.Id );
					//	} 
					//	else if ( filter.Name == "filter:transferValueFor" )
					//	{
					//		//what name do we have?
					//		tvpForRelationshipTypeIds.Add( item.Id );
					//	}
					//	else if ( filter.Name == "filter:transferValueFrom" )
					//	{
					//		//what name do we have?
					//		tvpFromRelationshipTypeIds.Add( item.Id );
					//	}
					//}
				}

				CommonHistoryFilter<GenericIndex>( query, ref history );

				if ( collectionCategoryTypeIds.Any() )
				{
					collectionCategoryTypeQuery = Query<GenericIndex>.Terms( ts => ts.Field( f => f.CollectionCategoryTypeIds ).Terms( collectionCategoryTypeIds.ToArray() ) );
				}

				if ( lifeCycleStatusTypeIds.Any() )
					lifeCycleStatusTypeQuery = Query<GenericIndex>.Terms( ts => ts.Field( f => f.LifeCycleStatusTypeId ).Terms( lifeCycleStatusTypeIds.ToArray() ) );
				if ( accommondationTypeIds.Any() )
					accommodationTypeQuery = Query<GenericIndex>.Terms( ts => ts.Field( f => f.AccommodationTypeIds ).Terms( accommondationTypeIds.ToArray() ) );

				if ( deliveryTypeIds.Any() )
					deliveryTypeQuery = Query<GenericIndex>.Terms( ts => ts.Field( f => f.DeliveryMethodTypeIds ).Terms( deliveryTypeIds.ToArray() ) );

				if ( supportServiceCategoryIds.Any() )
					supportServiceCategoryQuery = Query<GenericIndex>.Terms( ts => ts.Field( f => f.SupportServiceCategoryIds ).Terms( supportServiceCategoryIds.ToArray() ) );

				if ( reportIds.Any() )
				{
					reportsQuery = Query<GenericIndex>.Terms( ts => ts
								.Field( f => f
									.ReportFilters )
										.Terms<int>( reportIds.ToArray() ) );
				}

			}
			#endregion

			#region Competencies
			competenciesQuery = CommonCompetencies<GenericIndex>( query );

			#endregion

			#region General potentialresults list
			QueryContainer potentialResultsQuery = null;
			if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CUSTOM ) )
			{
				var idsList = new List<int>();
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "potentialresults" ).ToList() )
				{
					idsList.AddRange( JsonConvert.DeserializeObject<List<int>>( filter.CustomJSON ) );
				}

				if ( idsList.Any() )
				{
					idsList.ForEach( x =>
					{
						potentialResultsQuery |= Query<GenericIndex>.Terms( ts => ts.Field( f => f.Id ).Terms( x ) );
					} );
				}
			}
			#endregion

			commonOrgRolesBYQuery = CommonOrgRolesByFilter<GenericIndex>( query );
			publishedBYQuery = PublishedByFilter<GenericIndex>( query );
			//24-02-19 mp - relationshipTypeIds should not be QA roles!!
			var commonNonQAOrgRolesQuery = CommonNonQAOrgRolesFilter<GenericIndex>( query, relationshipTypeIds );
			//
			commonLeftRailOrgRolesBYQuery = CommonNonQAOrgRolesLeftRailFilter<GenericIndex>( query, new List<int>() { 3, 7 } );

			if ( entityTypeId == CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE )
			{
				//probably not used
				var hasTVPEntityTypeIds = new List<int>();
				hasTransferValueForQuery = TVPOrgsWithHasTVPForFilter( query, hasTVPEntityTypeIds );
				hasTransferValueFromQuery = TVPOrgsWithHasTVPFromFilter( query, hasTVPEntityTypeIds );

				if ( query.TransferIntermediaryId > 0 )
				{
					transferIntermediaryIdQuery = Query<GenericIndex>.Terms( ts => ts.Field( f => f.TransferIntermediariesFor ).Terms( query.TransferIntermediaryId ) );
				}
				//or
				//var transIntermediaryQuery = CommonPartOfTransferIntermediaryFilters<GenericIndex>( query );

				ownedByQuery = null;
			}

			#region Subject Areas.keywords
			//from widget and from search filters
			if ( query.FiltersV2.Any( x => x.Name == "subjects" ) )
			{
				subjectsQuery = HandleSubjects<GenericIndex>( query );
			}
			//keywords from widget

			if ( BaseFactory.IsValidCtid( query.Keywords ) )
			{
				ctidQuery = Query<GenericIndex>.Match( ts => ts.Field( f => f.CTID ).Query( query.Keywords ) );
			}
			else
			{
				if ( query.FiltersV2.Any( x => x.Name == "keywords" ) )
				{
					keywordsQuery = HandleWidgetKeywords<GenericIndex>( query );
				}
			}
			#endregion
			//NOTE: this is only referenced after clicking on a gray box or on detail, not from the search page
			//		==> actually now used by search widget => type = organizationroles
			//NOT WORKING YET
			//qaFilterQuery = CommonQualityAssurance<GenericIndex>( query );



			#region Occupations, industries
			occupationsQuery = CommonOccupations<GenericIndex>( query, ref widgetOccupationsQuery );
			classificationsQuery = CommonCip<GenericIndex>( query );
			industriesQuery = CommonIndustries<GenericIndex>( query, ref windustriesQuery );
			#endregion

			#region Query

			var sort = new SortDescriptor<GenericIndex>();

			var sortOrder = query.SortOrder;
			if ( sortOrder == "alpha" )
				sort.Ascending( s => s.Name.Suffix( "keyword" ) );
			else if ( sortOrder == "zalpha" )
				sort.Descending( s => s.Name.Suffix( "keyword" ) );
			else if ( sortOrder == "newest" )
				sort.Field( f => f.LastUpdated, SortOrder.Descending );
			else if ( sortOrder == "oldest" )
				sort.Field( f => f.LastUpdated, SortOrder.Ascending );
			else if ( sortOrder == "relevance" )
				sort.Descending( SortSpecialField.Score );
			//
			QueryContainer phrasePrefixQuery = SetPhrasePrefixQuery<GenericIndex>( query );
			QueryContainer bestFieldsQuery = SetBestFieldsQuery<GenericIndex>( query );
			QueryContainer autoCompleteQuery = SetAutocompleteQuery<GenericIndex>( query );


			var pageSize = query.PageSize;
			if ( requestIsAutocomplete )
			{
				pageSize = pageSize * 2;
				phrasePrefixQuery = null;
				//bestFieldsQuery = null;
			}
			else
			{
				autoCompleteQuery = null;
				//remove for TI testing
				//bestFieldsQuery = null;
			}
			if ( query.StartPage < 1 )
				query.StartPage = 1;

			var search = EC.Search<GenericIndex>( body => body
				   .Index( CommonIndex )
				
				   .Query( q =>
						entityTypeQuery
					  && ownedByQuery
					  && potentialResultsQuery
					  && transferIntermediaryIdQuery
					  && subjectsQuery
					  //&& wsubjectsQuery         //widget specific!!!
					  && keywordsQuery
					  && occupationsQuery && industriesQuery && classificationsQuery
					  && commonOrgRolesBYQuery && publishedBYQuery
					   && ctidQuery
					  && collectionCategoryTypeQuery && lifeCycleStatusTypeQuery
					  && supportServiceCategoryQuery && accommodationTypeQuery && deliveryTypeQuery
					  && commonNonQAOrgRolesQuery && commonLeftRailOrgRolesBYQuery
					  && hasTransferValueForQuery && hasTransferValueFromQuery
					  && qaFilterQuery
					  && history.CreatedFromQuery && history.CreatedToQuery && history.HistoryFromQuery && history.HistoryToQuery
					  && reportsQuery
					  && competenciesQuery
					  && ( phrasePrefixQuery || autoCompleteQuery || bestFieldsQuery )
				   )
				   .TrackTotalHits( true )
				   .Sort( s => sort )
				   .From( query.StartPage > 0 ? query.StartPage - 1 : 1 )
				   .Skip( ( query.StartPage - 1 ) * query.PageSize )
				 
				   .Size( pageSize ) );


			#endregion

			var debug = search.DebugInformation;
			pTotalRows = ( int ) search.Total;
			if ( requestIsAutocomplete )
			{
				var requestList = ( List<GenericIndex> ) search.Documents;
				autocompleteList = requestList.Select( x => x.ListTitle ).Distinct().Take( query.PageSize ).ToList();
				return null;
			}
			if ( query.StartPage == 1 && ( query.Filters.Count > 0 || query.FiltersV2.Count > 0 || !string.IsNullOrWhiteSpace( query.Keywords ) ) )
			{
				//actually extract the request: between #request and #response
				var elasticQuery = UtilityManager.ExtractNameValue( debug, "# Request", ":", "# Response:" );
				LoggingHelper.DoTrace( 6, string.Format( "ElasticServices.Search: {0}, Results: {1}, ElasticLog: \r\n", "GENERAL", pTotalRows ) + elasticQuery, "ElasticQuery" );
			}
			if ( pTotalRows > 0 )
			{
				list = ElasticManager.CommonIndex_MapFromElastic( ( List<GenericIndex> ) search.Documents, entityTypeId, query.StartPage, query.PageSize );

				LoggingHelper.DoTrace( 6, $"ElasticServices.GeneralSearch. Search type: {entityTypeId} found: {pTotalRows} records" );
			}
			//stats
			query.Results = pTotalRows;
			string jsoninput = JsonConvert.SerializeObject( query, JsonHelper.GetJsonSettings() );
			string searchType = "blind";
			if ( query.Filters.Count > 0 || query.FiltersV2.Count > 0 || !string.IsNullOrWhiteSpace( query.Keywords ) )
			{
				searchType = "filters selected";
			}
			if ( query.StartPage > 1 )
				searchType += " - paging";
			new ActivityServices().AddActivity( new SiteActivity()
			{ ActivityType = entityType, Activity = "Search", Event = searchType, Comment = jsoninput }
			);
			return list;
		}
		#region Build/update index
		#region Collection
		/// <summary>
		/// NOTE: would never do delete index in these individual methods, otherwise th
		/// </summary>
		/// <param name="deleteIndexFirst"></param>
		/// <param name="updatingIndexRegardless"></param>
		public static void General_BuildIndexForCollection( bool deleteIndexFirst = false, bool updatingIndexRegardless = false )
		{
			var list = new List<GenericIndex>();
			bool indexInitialized = false;
			//would have to have more control if sharing an index
			if ( deleteIndexFirst && EC.Indices.Exists( CommonIndex ).Exists )
			{
				//note if we want to handle multiple types, then either don't do a delete of everything, and then call each related build method
				//	or don't do a delete and provide info on which general index resources to rebuild
				LoggingHelper.DoTrace( 1, " General_BuildIndexForCollection - deleting CommonIndex" );
				EC.Indices.Delete( CommonIndex );
			}
			if ( !EC.Indices.Exists( CommonIndex ).Exists )
			{
				General_InitializeIndex();
				indexInitialized = true;
			}

			if ( indexInitialized || updatingIndexRegardless )
			{
				LoggingHelper.DoTrace( 1, "General_BuildIndexForCollection" );
				int minEntityStateId = UtilityManager.GetAppKeyValue( "minAsmtEntityStateId", 3 );
				try
				{
					new ActivityServices().AddActivity( new SiteActivity()
					{ ActivityType = "Collection", Activity = "Elastic", Event = "Build Index for Collection" }
					);
					int processed = 0;
					string filter = "( base.EntityStateId >= 2 )";
					//20-12-28 - update to be like credentials
					General_UpdateIndexForCollection( filter, ref processed );
					if ( processed > 0 )
					{
						var refreshResults = EC.Indices.Refresh( CommonIndex );
						new ActivityServices().AddActivity( new SiteActivity()
						{
							ActivityType = "Collection",
							Activity = "Elastic",
							Event = "Build Index Completed",
							Comment = string.Format( "Completed rebuild of CommonIndex-for Collection for {0} records.", processed )
						} );
					}
					else
					{
						LoggingHelper.DoTrace( 1, " General_BuildIndexForCollection - no data was found" );

					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, "General_BuildIndexForCollection" );
				}
				finally
				{

				}
			}

		}

		public static void General_UpdateIndexForCollection( int recordId )
		{
			if ( recordId < 1 )
				return;
			string filter = string.Format( " ( base.Id = {0} ) ", recordId );
			int processed = 0;
			General_UpdateIndexForCollection( filter, ref processed );

		} //

		/// <summary>
		/// Pass a filter to use for updating the index
		/// </summary>
		/// <param name="filter"></param>
		public static void General_UpdateIndexForCollection( string filter, ref int processed )
		{
			if ( string.IsNullOrWhiteSpace( filter ) )
				return;
			int action = UtilityManager.GetAppKeyValue( "updateCredIndexAction", 0 );
			if ( action == 0 )
				return;
			string methodName = "General_UpdateIndexForCollection";
			string IndexName = CommonIndex;
			int pageSize = nonCredentialPageSize;
			int pageNbr = 1;
			int totalRows = 0;
			bool isComplete = false;
			int cntr = 0; ;
			try
			{
				while ( pageNbr > 0 && !isComplete )
				{
					var list = ElasticManager.Collection_SearchForElastic( filter, pageSize, pageNbr, ref totalRows );
					if ( list != null && list.Count > 0 )
					{
						if ( list.Count == 1 && list[0].Name.ToLower().IndexOf( "exception" ) > -1 )
						{
							LoggingHelper.DoTrace( 2, string.Format( "{0}: NOTE Error encountered during search for data: {1}", methodName, list[0].Description ) );
							isComplete = true;
							break;
						}
						processed = totalRows;
						if ( action == 1 )
						{
							foreach ( var item in list )
							{
								cntr++;
								var res = EC.Index( item, idx => idx.Index( IndexName ) );
								Console.WriteLine( res.Result );
							}
						}
						else if ( action == 2 )
						{
							cntr = cntr + list.Count;
							//22-12-16 NEW using EntityId as the PK, otherwise Id in one type overwrites the Id from another type
							var results = EC.Bulk( b => b.IndexMany( list, ( d, record ) => d.Index( IndexName ).Document( record ).Id( record.EntityId.ToString() ) ) );
							if ( results.ToString().IndexOf( "Valid NEST response built from a successful" ) == -1 )
							{
								Console.WriteLine( results.ToString() );
								LoggingHelper.DoTrace( 1, string.Format( " Issue building {0}: ", IndexName ) + results.DebugInformation.Substring( 0, 2000 ) );
							}
						}
					}
					else
					{
						if ( list != null && list.Count == 1 && totalRows == -1 )
						{
							LoggingHelper.LogError( string.Format( "{0}: Error in search. {1}", methodName, list[0].Description ) );

						}
						else if ( pageNbr == 1 )
						{
							if ( string.IsNullOrWhiteSpace( filter ) )
							{
								LoggingHelper.LogError( string.Format( "{0}: entered with no filter, but no results were returned from search.", methodName ) );
							}
							LoggingHelper.DoTrace( 2, string.Format( "{0}: NOTE no data returned for filter: {1}", methodName, filter ) );
						}
						isComplete = true;
						break;
					}
					LoggingHelper.DoTrace( 4, string.Format( "{0}. Page: {1}, Records Indexed: {2} 0f {3} records.", methodName, pageNbr, cntr, totalRows ) );
					if ( cntr >= totalRows )
					{
						isComplete = true;
					}

					pageNbr++;
				} //loop
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "{0} failed for filter: {1}", methodName, filter ) );
			}
		}

		#endregion
		#region OutcomeData
		public static void General_BuildIndexForOutcomeData( bool updatingIndexRegardless = true )
		{
			var list = new List<GenericIndex>();

			if ( updatingIndexRegardless )
			{
				LoggingHelper.DoTrace( 1, "General- BuildIndexForOutcomeData" );
				try
				{
					new ActivityServices().AddActivity( new SiteActivity()
					{ ActivityType = "OutcomeData", Activity = "Elastic", Event = "Build Index for OutcomeData" }
					);
					int processed = 0;
					string filter = "( base.EntityStateId = 3)";
					//
					General_UpdateIndexForOutcomeData( filter, ref processed );
					if ( processed > 0 )
					{
						var refreshResults = EC.Indices.Refresh( CommonIndex );
						new ActivityServices().AddActivity( new SiteActivity()
						{
							ActivityType = "OutcomeData",
							Activity = "Elastic",
							Event = "Build Index Completed",
							Comment = string.Format( "Completed rebuild of CommonIndex-for OutcomeData for {0} records.", processed )
						} );
					}
					else
					{
						LoggingHelper.DoTrace( 1, " General_BuildIndexForOutcomeData - no data was found" );
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, "General_BuildIndexForOutcomeData" );
				}
				finally
				{

				}
			}
		}


		/// <summary>
		/// Pass a filter to use for updating the index
		/// </summary>
		/// <param name="filter"></param>
		public static void General_UpdateIndexForOutcomeData( string filter, ref int processed )
		{
			if ( string.IsNullOrWhiteSpace( filter ) )
				return;
			int action = UtilityManager.GetAppKeyValue( "updateCredIndexAction", 0 );
			if ( action == 0 )
				return;
			string methodName = "General_UpdateIndexForOutcomeData";
			string IndexName = CommonIndex;
			int pageSize = nonCredentialPageSize;
			int pageNbr = 1;
			int totalRows = 0;
			bool isComplete = false;
			int cntr = 0; ;
			try
			{
				while ( pageNbr > 0 && !isComplete )
				{
					var list = ElasticManager.OutcomeData_SearchForElastic( filter, pageSize, pageNbr, ref totalRows );
					if ( list != null && list.Count > 0 )
					{
						if ( list.Count == 1 && list[0].Name.ToLower().IndexOf( "exception" ) > -1 )
						{
							LoggingHelper.DoTrace( 2, string.Format( "{0}: NOTE Error encountered during search for data: {1}", methodName, list[0].Description ) );
							isComplete = true;
							break;
						}
						processed = totalRows;
						if ( action == 1 )
						{
							foreach ( var item in list )
							{
								cntr++;
								var res = EC.Index( item, idx => idx.Index( IndexName ) );
								Console.WriteLine( res.Result );
							}
						}
						else if ( action == 2 )
						{
							cntr = cntr + list.Count;
							var results = EC.Bulk( b => b.IndexMany( list, ( d, record ) => d.Index( IndexName ).Document( record ).Id( record.EntityId.ToString() ) ) );
							if ( results.ToString().IndexOf( "Valid NEST response built from a successful" ) == -1 )
							{
								Console.WriteLine( results.ToString() );
								LoggingHelper.DoTrace( 1, string.Format( " Issue building {0}: ", IndexName ) + results.DebugInformation.Substring( 0, 2000 ) );
							}
						}
					}
					else
					{
						if ( list != null && list.Count == 1 && totalRows == -1 )
						{
							LoggingHelper.LogError( string.Format( "{0}: Error in search. {1}", methodName, list[0].Description ) );

						}
						else if ( pageNbr == 1 )
						{
							if ( string.IsNullOrWhiteSpace( filter ) )
							{
								LoggingHelper.LogError(string.Format("{0}: entered with no filter, but no results were returned from search.", methodName));
							}
							LoggingHelper.DoTrace( 2, string.Format( "{0}: NOTE no data returned for filter: {1}", methodName, filter ) );
						}
						isComplete = true;
						break;
					}
					LoggingHelper.DoTrace( 4, string.Format( "{0}. Page: {1}, Records Indexed: {2} 0f {3} records.", methodName, pageNbr, cntr, totalRows ) );

					if ( cntr >= totalRows )
					{
						isComplete = true;
					}

					pageNbr++;
				} //loop
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "{0} failed for filter: {1}", methodName, filter ) );
			}
		}


		#endregion
		#region PathwaySet
		public static void General_BuildIndexForPathwaySet( bool deleteIndexFirst = false, bool updatingIndexRegardless = false )
		{
			var list = new List<GenericIndex>();
			bool indexInitialized = false;
			//would have to have more control if sharing an index
			if ( deleteIndexFirst && EC.Indices.Exists( CommonIndex ).Exists )
			{
				//note if we want to handle multiple types, then either don't do a delete of everything, and then call each related build method
				//	or don't do a delete and provide info on which general index resources to rebuild
				LoggingHelper.DoTrace( 1, " General_BuildIndexForPathwaySet - deleting CommonIndex" );
				EC.Indices.Delete( CommonIndex );
			}
			if ( !EC.Indices.Exists( CommonIndex ).Exists )
			{
				General_InitializeIndex();
				indexInitialized = true;
			}

			if ( indexInitialized || updatingIndexRegardless )
			{
				LoggingHelper.DoTrace( 1, "General_BuildIndexForPathwaySet" );
				int minEntityStateId = UtilityManager.GetAppKeyValue( "minAsmtEntityStateId", 3 );
				try
				{
					new ActivityServices().AddActivity( new SiteActivity()
					{ ActivityType = "PathwaySet", Activity = "Elastic", Event = "Build Index for PathwaySet" }
					);
					int processed = 0;
					string filter = "( base.EntityStateId >= 2 )";
					//20-12-28 - update to be like credentials
					General_UpdateIndexForPathwaySet( filter, ref processed );
					if ( processed > 0 )
					{
						var refreshResults = EC.Indices.Refresh( CommonIndex );
						new ActivityServices().AddActivity( new SiteActivity()
						{
							ActivityType = "PathwaySet",
							Activity = "Elastic",
							Event = "Build Index Completed",
							Comment = string.Format( "Completed rebuild of CommonIndex-for PathwaySet for {0} records.", processed )
						} );
					}
					else
					{
						LoggingHelper.DoTrace( 1, " General_BuildIndexForPathwaySet - no data was found" );

					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, "General_BuildIndexForPathwaySet" );
				}
				finally
				{

				}
			}
		}

		public static void General_UpdateIndexForPathwaySet( int recordId )
		{
			if ( recordId < 1 )
				return;
			string filter = string.Format( " ( base.Id = {0} ) ", recordId );
			int processed = 0;
			General_UpdateIndexForPathwaySet( filter, ref processed );

		} //

		/// <summary>
		/// Pass a filter to use for updating the index
		/// </summary>
		/// <param name="filter"></param>
		public static void General_UpdateIndexForPathwaySet( string filter, ref int processed )
		{
			if ( string.IsNullOrWhiteSpace( filter ) )
				return;
			int action = UtilityManager.GetAppKeyValue( "updateCredIndexAction", 0 );
			if ( action == 0 )
				return;
			string methodName = "General_UpdateIndexForPathwaySet";
			string IndexName = CommonIndex;
			int pageSize = nonCredentialPageSize;
			int pageNbr = 1;
			int totalRows = 0;
			bool isComplete = false;
			int cntr = 0; ;
			try
			{
				while ( pageNbr > 0 && !isComplete )
				{
					var list = ElasticManager.PathwaySet_SearchForElastic( filter, pageSize, pageNbr, ref totalRows );
					if ( list != null && list.Count > 0 )
					{
						if ( list.Count == 1 && list[0].Name.ToLower().IndexOf( "exception" ) > -1 )
						{
							LoggingHelper.DoTrace( 2, string.Format( "{0}: NOTE Error encountered during search for data: {1}", methodName, list[0].Description ) );
							isComplete = true;
							break;
						}
						processed = totalRows;
						if ( action == 1 )
						{
							foreach ( var item in list )
							{
								cntr++;
								var res = EC.Index( item, idx => idx.Index( IndexName ) );
								Console.WriteLine( res.Result );
							}
						}
						else if ( action == 2 )
						{
							cntr = cntr + list.Count;
							var results = EC.Bulk( b => b.IndexMany( list, ( d, record ) => d.Index( IndexName ).Document( record ).Id( record.EntityId.ToString() ) ) );
							if ( results.ToString().IndexOf( "Valid NEST response built from a successful" ) == -1 )
							{
								Console.WriteLine( results.ToString() );
								LoggingHelper.DoTrace( 1, string.Format( " Issue building {0}: ", IndexName ) + results.DebugInformation.Substring( 0, 2000 ) );
							}
						}
					}
					else
					{
						if ( list != null && list.Count == 1 && totalRows == -1 )
						{
							LoggingHelper.LogError( string.Format( "{0}: Error in search. {1}", methodName, list[0].Description ) );

						}
						else if ( pageNbr == 1 )
						{
							if ( string.IsNullOrWhiteSpace( filter ) )
							{
								LoggingHelper.LogError(string.Format("{0}: entered with no filter, but no results were returned from search.", methodName));
							}
							LoggingHelper.DoTrace( 2, string.Format( "{0}: NOTE no data returned for filter: {1}", methodName, filter ) );
						}
						isComplete = true;
						break;
					}
					LoggingHelper.DoTrace( 4, string.Format( "{0}. Page: {1}, Records Indexed: {2} 0f {3} records.", methodName, pageNbr, cntr, totalRows ) );
					if ( cntr >= totalRows )
					{
						isComplete = true;
					}

					pageNbr++;
				} //loop
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "{0} failed for filter: {1}", methodName, filter ) );
			}
		}

		#endregion

		#region Transfer Value
		public static void General_BuildIndexForTVP( bool deleteIndexFirst = false, bool updatingIndexRegardless = false )
		{
			var list = new List<GenericIndex>();
			bool indexInitialized = false;
			//would have to have more control if sharing an index
			if ( deleteIndexFirst && EC.Indices.Exists( CommonIndex ).Exists )
			{
				//NOTE: a delete of the general index should only be done when building all
				LoggingHelper.DoTrace( 1, " General_BuildIndexForTVP - deleting CommonIndex" );
				EC.Indices.Delete( CommonIndex );
			}
			if ( !EC.Indices.Exists( CommonIndex ).Exists )
			{
				General_InitializeIndex();
				indexInitialized = true;
			}

			if ( indexInitialized || updatingIndexRegardless )
			{
				LoggingHelper.DoTrace( 1, "General- BuildIndexForTVP" );
				int minEntityStateId = UtilityManager.GetAppKeyValue( "minAsmtEntityStateId", 3 );
				try
				{
					new ActivityServices().AddActivity( new SiteActivity()
					{ ActivityType = "TransferValue", Activity = "Elastic", Event = "Build Index for TVP" }
					);
					int processed = 0;
					string filter = "( base.EntityStateId >= 2 )";
					//20-12-28 - update to be like credentials
					General_UpdateIndexForTVP( filter, ref processed );
					if ( processed > 0 )
					{
						var refreshResults = EC.Indices.Refresh( CommonIndex );
						new ActivityServices().AddActivity( new SiteActivity()
						{
							ActivityType = "TransferValue",
							Activity = "Elastic",
							Event = "Build Index Completed",
							Comment = string.Format( "Completed rebuild of CommonIndex-for TransferValue for {0} records.", processed )
						} );
					}
					else
					{
						LoggingHelper.DoTrace( 1, " General_BuildIndexForTVP - no data was found" );

					}
					//list = ElasticManager.TransferValue_SearchForElastic( string.Format( "( base.EntityStateId >= {0} )", minEntityStateId ) );
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, "General_BuildIndexForTVP" );
				}
				finally
				{

				}
			}

		}
		//NOTE: this is only used when updating elastic immediately which we are not, so don't replicate this method for new classes
		public static void General_UpdateIndexForTVP( int recordId )
		{
			if ( recordId < 1 )
				return;
			try
			{
				string filter = string.Format( " ( base.Id = {0} ) ", recordId );
				int processed = 0;
				General_UpdateIndexForTVP( filter, ref processed );

			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "General_UpdateIndex" );
			}
		} //


		/// <summary>
		/// Pass a filter to use for updating the index
		/// </summary>
		/// <param name="filter"></param>
		public static void General_UpdateIndexForTVP( string filter, ref int processed )
		{
			if ( string.IsNullOrWhiteSpace( filter ) )
				return;
			int action = UtilityManager.GetAppKeyValue( "updateCredIndexAction", 0 );
			if ( action == 0 )
				return;
			string methodName = "General_UpdateIndexForTVP";
			LoggingHelper.DoTrace( CodesManager.appDebuggingTraceLevel, $"{thisClassName}.{methodName} filter: {filter}" );

			string IndexName = CommonIndex;
			int pageSize = nonCredentialPageSize;
			int pageNbr = 1;
			int totalRows = 0;
			bool isComplete = false;
			int cntr = 0; ;
			try
			{
				while ( pageNbr > 0 && !isComplete )
				{
					var list = ElasticManager.TransferValue_SearchForElastic( filter, pageSize, pageNbr, ref totalRows );
					if ( list != null && list.Count > 0 )
					{
						if ( list.Count == 1 && list[0].Name.ToLower().IndexOf( "exception" ) > -1 )
						{
							LoggingHelper.DoTrace( 2, string.Format( "{0}: NOTE Error encountered during search for data: {1}", methodName, list[0].Description ) );
							isComplete = true;
							break;
						}
						processed = totalRows;
						if ( action == 1 )
						{
							foreach ( var item in list )
							{
								cntr++;
								var res = EC.Index( item, idx => idx.Index( IndexName ) );
								Console.WriteLine( res.Result );
							}
						}
						else if ( action == 2 )
						{
							cntr = cntr + list.Count;
							var results = EC.Bulk( b => b.IndexMany( list, ( d, record ) => d.Index( IndexName ).Document( record ).Id( record.EntityId.ToString() ) ) );
							if ( results.ToString().IndexOf( "Valid NEST response built from a successful" ) == -1 )
							{
								Console.WriteLine( results.ToString() );
								LoggingHelper.DoTrace( 1, string.Format( " Issue building {0}: ", IndexName ) + results.DebugInformation.Substring( 0, 2000 ) );
							}
						}
					}
					else
					{
						//LoggingHelper.DoTrace( 2, string.Format( "TransferValue_UpdateIndex failed, no data returned for filter: {0}", filter ) );
						if ( list != null && list.Count == 1 && totalRows == -1 )
						{
							LoggingHelper.LogError( string.Format( "{0}: Error in search. {1}", methodName, list[0].Description ) );

						}
						else if ( pageNbr == 1 )
						{
							if ( string.IsNullOrWhiteSpace( filter ) )
							{
								LoggingHelper.LogError(string.Format("{0}: entered with no filter, but no results were returned from search.", methodName));
							}
							LoggingHelper.DoTrace( 2, string.Format( "{0}: NOTE no data returned for filter: {1}", methodName, filter ) );
						}
						isComplete = true;
						break;
					}
					LoggingHelper.DoTrace( 4, string.Format( "{0}. Page: {1}, Records Indexed: {2} 0f {3} records.", methodName, pageNbr, cntr, totalRows ) );
					if ( cntr >= totalRows )
					{
						isComplete = true;
					}

					pageNbr++;
				} //loop
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "{0} failed for filter: {1}", methodName, filter ) );
			}
		}


		#endregion
		#region Transfer Intermediary
		public static void General_BuildIndexForTransferIntermediary( bool deleteIndexFirst = false, bool updatingIndexRegardless = false )
		{
			var list = new List<GenericIndex>();
			bool indexInitialized = false;
			//would have to have more control if sharing an index
			if ( deleteIndexFirst && EC.Indices.Exists( CommonIndex ).Exists )
			{
				LoggingHelper.DoTrace( 1, " General_BuildIndexForTransferIntermediary - deleting CommonIndex" );
				EC.Indices.Delete( CommonIndex );
			}
			if ( !EC.Indices.Exists( CommonIndex ).Exists )
			{
				General_InitializeIndex();
				indexInitialized = true;
			}

			if ( indexInitialized || updatingIndexRegardless )
			{
				LoggingHelper.DoTrace( 1, "General- BuildIndexForTransferIntermediary" );
				int minEntityStateId = UtilityManager.GetAppKeyValue( "minAsmtEntityStateId", 3 );
				try
				{
					new ActivityServices().AddActivity( new SiteActivity()
					{ ActivityType = "TransferIntermediary", Activity = "Elastic", Event = "Build Index for TransferIntermediary" }
					);
					int processed = 0;
					string filter = "( base.EntityStateId >= 2 )";
					//20-12-28 - update to be like credentials
					General_UpdateIndexForTransferIntermediary( filter, ref processed );
					if ( processed > 0 )
					{
						var refreshResults = EC.Indices.Refresh( CommonIndex );
						new ActivityServices().AddActivity( new SiteActivity()
						{
							ActivityType = "TransferIntermediary",
							Activity = "Elastic",
							Event = "Build Index Completed",
							Comment = string.Format( "Completed rebuild of CommonIndex-for TransferIntermediary for {0} records.", processed )
						} );
					}
					else
					{
						LoggingHelper.DoTrace( 1, " General_BuildIndexForTransferIntermediary - no data was found" );

					}
					//list = ElasticManager.TransferValue_SearchForElastic( string.Format( "( base.EntityStateId >= {0} )", minEntityStateId ) );
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, "General_BuildIndexForTransferIntermediary" );
				}
				finally
				{
					//if ( list != null && list.Count > 0 )
					//{
					//	var results = EC.Bulk( b => b.IndexMany( list, ( d, document ) => d.Index( CommonIndex ).Document( document ).Id( document.Id.ToString() ) ) );
					//	if ( results.ToString().IndexOf( "Valid NEST response built from a successful" ) == -1 )
					//	{
					//		Console.WriteLine( results.ToString() );
					//		LoggingHelper.DoTrace( 1, " Issue building General index: " + results.DebugInformation.Substring( 0, 2000 ) );
					//	}

					//	EC.Indices.Refresh( CommonIndex );
					//} else
					//{
					//	LoggingHelper.DoTrace( 1, " General_BuildIndexForTVP - no data was found" );
					//}
				}
			}

		}

		public static void General_UpdateIndexForTransferIntermediary( int recordId )
		{
			if ( recordId < 1 )
				return;
			try
			{
				string filter = string.Format( " ( base.Id = {0} ) ", recordId );
				int processed = 0;
				General_UpdateIndexForTransferIntermediary( filter, ref processed );

			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, String.Format( "General_UpdateIndexForTrIntermediary( {0})", recordId ) );
			}
		} //

		public static void General_UpdateIndexForTransferIntermediary( string filter, ref int processed )
		{
			if ( string.IsNullOrWhiteSpace( filter ) )
				return;
			int action = UtilityManager.GetAppKeyValue( "updateCredIndexAction", 0 );
			if ( action == 0 )
				return;
			string methodName = "General_UpdateIndexForTransferIntermediary";
			string IndexName = CommonIndex;
			int pageSize = nonCredentialPageSize;
			int pageNbr = 1;
			int totalRows = 0;
			bool isComplete = false;
			int cntr = 0; ;
			try
			{
				while ( pageNbr > 0 && !isComplete )
				{
					var list = ElasticManager.TransferIntermediary_SearchForElastic( filter, pageSize, pageNbr, ref totalRows );
					if ( list != null && list.Count > 0 )
					{
						if ( list.Count == 1 && list[0].Name.ToLower().IndexOf( "exception" ) > -1 )
						{
							LoggingHelper.DoTrace( 2, string.Format( "{0}: NOTE Error encountered during search for data: {1}", methodName, list[0].Description ) );
							isComplete = true;
							break;
						}
						processed = totalRows;
						if ( action == 1 )
						{
							foreach ( var item in list )
							{
								cntr++;
								var res = EC.Index( item, idx => idx.Index( IndexName ) );
								Console.WriteLine( res.Result );
							}
						}
						else if ( action == 2 )
						{
							cntr = cntr + list.Count;
							var results = EC.Bulk( b => b.IndexMany( list, ( d, record ) => d.Index( IndexName ).Document( record ).Id( record.EntityId.ToString() ) ) );
							if ( results.ToString().IndexOf( "Valid NEST response built from a successful" ) == -1 )
							{
								Console.WriteLine( results.ToString() );
								LoggingHelper.DoTrace( 1, string.Format( " Issue building {0}: ", IndexName ) + results.DebugInformation.Substring( 0, 2000 ) );
							}
						}
					}
					else
					{
						if ( list != null && list.Count == 1 && totalRows == -1 )
						{
							LoggingHelper.LogError( string.Format( "{0}: Error in search. {1}", methodName, list[0].Description ) );

						}
						else if ( pageNbr == 1 )
						{
							if ( string.IsNullOrWhiteSpace( filter ) )
							{
								LoggingHelper.LogError( string.Format( "{0}: entered with no filter, but no results were returned from search.", methodName ) );
							}
							LoggingHelper.DoTrace( 2, string.Format( "{0}: NOTE no data returned for filter: {1}", methodName, filter ) );
						}
						isComplete = true;
						break;
					}
					LoggingHelper.DoTrace( 4, string.Format( "{0}. Page: {1}, Records Indexed: {2} 0f {3} records.", methodName, pageNbr, cntr, totalRows ) );
					if ( cntr >= totalRows )
					{
						isComplete = true;
					}

					pageNbr++;
				} //loop
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "{0} failed for filter: {1}", methodName, filter ) );
			}
		}


		#endregion
		#region ConceptScheme/Progression model
		//Currently mostly N/A as search is done thru the registry
		public static void General_UpdateIndexForConceptScheme( bool updatingIndexRegardless = true )
		{
			var list = new List<GenericIndex>();
			var EntityType = "ConceptScheme";
			if ( updatingIndexRegardless )
			{
				LoggingHelper.DoTrace( 1, $"General- General_UpdateIndexFor{EntityType}" );
				try
				{
					new ActivityServices().AddActivity( new SiteActivity()
					{ ActivityType = EntityType, Activity = "Elastic", Event = $"Build Index for {EntityType}" }
					);
					int processed = 0;
					string filter = "( base.EntityStateId >= 2 )";
					//
					General_UpdateIndexForConceptScheme( filter, ref processed );
					if ( processed > 0 )
					{
						var refreshResults = EC.Indices.Refresh( CommonIndex );
						new ActivityServices().AddActivity( new SiteActivity()
						{
							ActivityType = EntityType,
							Activity = "Elastic",
							Event = "Build Index Completed",
							Comment = $"Completed rebuild of CommonIndex-for {EntityType} for {processed} records."
						} );
					}
					else
					{
						LoggingHelper.DoTrace( 1, $" General_BuildIndexFor{EntityType} - no data was found" );
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, $"General_BuildIndexFor{EntityType}" );
				}
				finally
				{

				}
			}
		}

		public static void General_UpdateIndexForConceptScheme( string filter, ref int processed )
		{
			if ( string.IsNullOrWhiteSpace( filter ) )
				return;
			int action = UtilityManager.GetAppKeyValue( "updateCredIndexAction", 0 );
			if ( action == 0 )
				return;
			string methodName = "General_UpdateIndexForConceptScheme";
			string IndexName = CommonIndex;
			int pageSize = nonCredentialPageSize;
			int pageNbr = 1;
			int totalRows = 0;
			bool isComplete = false;
			int cntr = 0; ;
			try
			{
				while ( pageNbr > 0 && !isComplete )
				{
					var list = ElasticManager.ConceptScheme_SearchForElastic( filter, pageSize, pageNbr, ref totalRows );
					if ( list != null && list.Count > 0 )
					{
						if ( list.Count == 1 && list[0].Name.ToLower().IndexOf( "exception" ) > -1 )
						{
							LoggingHelper.DoTrace( 2, string.Format( "{0}: NOTE Error encountered during search for data: {1}", methodName, list[0].Description ) );
							isComplete = true;
							break;
						}
						processed = totalRows;
						if ( action == 1 )
						{
							foreach ( var item in list )
							{
								cntr++;
								var res = EC.Index( item, idx => idx.Index( IndexName ) );
								Console.WriteLine( res.Result );
							}
						}
						else if ( action == 2 )
						{
							cntr = cntr + list.Count;
							var results = EC.Bulk( b => b.IndexMany( list, ( d, record ) => d.Index( IndexName ).Document( record ).Id( record.EntityId.ToString() ) ) );
							if ( results.ToString().IndexOf( "Valid NEST response built from a successful" ) == -1 )
							{
								Console.WriteLine( results.ToString() );
								LoggingHelper.DoTrace( 1, string.Format( " Issue building {0}: ", IndexName ) + results.DebugInformation.Substring( 0, 2000 ) );
							}
						}
					}
					else
					{
						if ( list != null && list.Count == 1 && totalRows == -1 )
						{
							LoggingHelper.LogError( string.Format( "{0}: Error in search. {1}", methodName, list[0].Description ) );

						}
						else if ( pageNbr == 1 )
						{
							if ( string.IsNullOrWhiteSpace( filter ) )
							{
								LoggingHelper.LogError(string.Format("{0}: entered with no filter, but no results were returned from search.", methodName));
							}
							LoggingHelper.DoTrace( 2, string.Format( "{0}: NOTE no data returned for filter: {1}", methodName, filter ) );
						}
						isComplete = true;
						break;
					}
					LoggingHelper.DoTrace( 4, string.Format( "{0}. Page: {1}, Records Indexed: {2} 0f {3} records.", methodName, pageNbr, cntr, totalRows ) );
					if ( cntr >= totalRows )
					{
						isComplete = true;
					}

					pageNbr++;
				} //loop
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "{0} failed for filter: {1}", methodName, filter ) );
			}
		}
		public static void General_UpdateIndexForProgressionModel( bool updatingIndexRegardless = true )
		{
			var list = new List<GenericIndex>();
			var EntityType = "ProgressionModel";
			if ( updatingIndexRegardless )
			{
				LoggingHelper.DoTrace( 1, $"General- General_UpdateIndexFor{EntityType}" );
				try
				{
					new ActivityServices().AddActivity( new SiteActivity()
					{ ActivityType = EntityType, Activity = "Elastic", Event = $"Build Index for {EntityType}" }
					);
					int processed = 0;
					string filter = "( base.EntityStateId >= 2 )";
					//
					General_UpdateIndexForProgressionModel( filter, ref processed );
					if ( processed > 0 )
					{
						var refreshResults = EC.Indices.Refresh( CommonIndex );
						new ActivityServices().AddActivity( new SiteActivity()
						{
							ActivityType = EntityType,
							Activity = "Elastic",
							Event = "Build Index Completed",
							Comment = $"Completed rebuild of CommonIndex-for {EntityType} for {processed} records."
						} );
					}
					else
					{
						LoggingHelper.DoTrace( 1, $" General_BuildIndexFor{EntityType} - no data was found" );
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, $"General_BuildIndexFor{EntityType}" );
				}
				finally
				{

				}
			}
		}

		//
		public static void General_UpdateIndexForProgressionModel( string filter, ref int processed )
		{
			if ( string.IsNullOrWhiteSpace( filter ) )
				return;
			int action = UtilityManager.GetAppKeyValue( "updateCredIndexAction", 0 );
			if ( action == 0 )
				return;
			string methodName = "General_UpdateIndexForProgressionModel";
			string IndexName = CommonIndex;
			int pageSize = nonCredentialPageSize;
			int pageNbr = 1;
			int totalRows = 0;
			bool isComplete = false;
			int cntr = 0; ;
			try
			{
				while ( pageNbr > 0 && !isComplete )
				{
					var list = ElasticManager.ProgressionModel_SearchForElastic( filter, pageSize, pageNbr, ref totalRows );
					if ( list != null && list.Count > 0 )
					{
						if ( list.Count == 1 && list[0].Name.ToLower().IndexOf( "exception" ) > -1 )
						{
							LoggingHelper.DoTrace( 2, string.Format( "{0}: NOTE Error encountered during search for data: {1}", methodName, list[0].Description ) );
							isComplete = true;
							break;
						}
						processed = totalRows;
						if ( action == 1 )
						{
							foreach ( var item in list )
							{
								cntr++;
								var res = EC.Index( item, idx => idx.Index( IndexName ) );
								Console.WriteLine( res.Result );
							}
						}
						else if ( action == 2 )
						{
							cntr = cntr + list.Count;
							var results = EC.Bulk( b => b.IndexMany( list, ( d, record ) => d.Index( IndexName ).Document( record ).Id( record.EntityId.ToString() ) ) );
							if ( results.ToString().IndexOf( "Valid NEST response built from a successful" ) == -1 )
							{
								Console.WriteLine( results.ToString() );
								LoggingHelper.DoTrace( 1, string.Format( " Issue building {0}: ", IndexName ) + results.DebugInformation.Substring( 0, 2000 ) );
							}
						}
					}
					else
					{
						if ( list != null && list.Count == 1 && totalRows == -1 )
						{
							LoggingHelper.LogError( string.Format( "{0}: Error in search. {1}", methodName, list[0].Description ) );

						}
						else if ( pageNbr == 1 )
						{
							if ( string.IsNullOrWhiteSpace( filter ) )
							{
								LoggingHelper.LogError( string.Format( "{0}: entered with no filter, but no results were returned from search.", methodName ) );
							}
							LoggingHelper.DoTrace( 2, string.Format( "{0}: NOTE no data returned for filter: {1}", methodName, filter ) );
						}
						isComplete = true;
						break;
					}
					LoggingHelper.DoTrace( 4, string.Format( "{0}. Page: {1}, Records Indexed: {2} 0f {3} records.", methodName, pageNbr, cntr, totalRows ) );
					if ( cntr >= totalRows )
					{
						isComplete = true;
					}

					pageNbr++;
				} //loop
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "{0} failed for filter: {1}", methodName, filter ) );
			}
		}

		#endregion
		#region CredentialingAction
		public static void General_BuildIndexForCredentialingAction( bool updatingIndexRegardless = true )
		{
			var list = new List<GenericIndex>();

			if ( updatingIndexRegardless )
			{
				LoggingHelper.DoTrace( 1, "General- BuildIndexForCredentialingAction" );
				try
				{
					new ActivityServices().AddActivity( new SiteActivity()
					{ ActivityType = "CredentialingAction", Activity = "Elastic", Event = "Build Index for CredentialingAction" }
					);
					int processed = 0;
					string filter = "( base.EntityStateId >= 2 )";
					//
					General_UpdateIndexForCredentialingAction( filter, ref processed );
					if ( processed > 0 )
					{
						var refreshResults = EC.Indices.Refresh( CommonIndex );
						new ActivityServices().AddActivity( new SiteActivity()
						{
							ActivityType = "CredentialingAction",
							Activity = "Elastic",
							Event = "Build Index Completed",
							Comment = string.Format( "Completed rebuild of CommonIndex-for CredentialingAction for {0} records.", processed )
						} );
					}
					else
					{
						LoggingHelper.DoTrace( 1, " General_BuildIndexForCredentialingAction - no data was found" );

					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, "General_BuildIndexForCredentialingAction" );
				}
				finally
				{

				}
			}
		}


		/// <summary>
		/// Pass a filter to use for updating the index
		/// </summary>
		/// <param name="filter"></param>
		public static void General_UpdateIndexForCredentialingAction( string filter, ref int processed )
		{
			if ( string.IsNullOrWhiteSpace( filter ) )  //wouldn't this mean reindex all?
				return;
			int action = UtilityManager.GetAppKeyValue( "updateCredIndexAction", 0 );
			if ( action == 0 )
				return;
			string methodName = "General_UpdateIndexForCredentialingAction";
			string IndexName = CommonIndex;
            int pageSize = nonCredentialPageSize;
			int pageNbr = 1;
			int totalRows = 0;
			bool isComplete = false;
			int cntr = 0; ;
			try
			{
				while ( pageNbr > 0 && !isComplete )
				{
					var list = ElasticManager.CredentialingAction_SearchForElastic( filter, pageSize, pageNbr, ref totalRows );
					if ( list != null && list.Count > 0 )
					{
						if ( list.Count == 1 && list[0].Name.ToLower().IndexOf( "exception" ) > -1 )
						{
							LoggingHelper.DoTrace( 2, string.Format( "{0}: NOTE Error encountered during search for data: {1}", methodName, list[0].Description ) );
							isComplete = true;
							break;
						}
						processed = totalRows;
						if ( action == 1 )
						{
							foreach ( var item in list )
							{
								cntr++;
								var res = EC.Index( item, idx => idx.Index( IndexName ) );
								Console.WriteLine( res.Result );
							}
						}
						else if ( action == 2 )
						{
							cntr = cntr + list.Count;
							var results = EC.Bulk( b => b.IndexMany( list, ( d, record ) => d.Index( IndexName ).Document( record ).Id( record.EntityId.ToString() ) ) );
							if ( results.ToString().IndexOf( "Valid NEST response built from a successful" ) == -1 )
							{
								Console.WriteLine( results.ToString() );
								LoggingHelper.DoTrace( 1, string.Format( " Issue building {0}: ", IndexName ) + results.DebugInformation.Substring( 0, 2000 ) );
							}
						}
					}
					else
					{
						if ( list != null && list.Count == 1 && totalRows == -1 )
						{
							LoggingHelper.LogError( string.Format( "{0}: Error in search. {1}", methodName, list[0].Description ) );

						}
						else if ( pageNbr == 1 )
						{
							if ( string.IsNullOrWhiteSpace( filter ) )
							{
								LoggingHelper.LogError(string.Format("{0}: entered with no filter, but no results were returned from search.", methodName));
							}
							LoggingHelper.DoTrace( 2, string.Format( "{0}: NOTE no data returned for filter: {1}", methodName, filter ) );
						}
						isComplete = true;
						break;
					}
					LoggingHelper.DoTrace( 4, string.Format( "{0}. Page: {1}, Records Indexed: {2} 0f {3} records.", methodName, pageNbr, cntr, totalRows ) );
					if ( cntr >= totalRows )
					{
						isComplete = true;
					}

					pageNbr++;
				} //loop
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "{0} failed for filter: {1}", methodName, filter ) );
			}
		}


		#endregion
		#region SupportService
		public static void General_BuildIndexForSupportService( bool updatingIndexRegardless = true )
		{
			var list = new List<GenericIndex>();

			if ( updatingIndexRegardless )
			{
				LoggingHelper.DoTrace( 1, "General- BuildIndexForSupportService" );
				int minEntityStateId = UtilityManager.GetAppKeyValue( "minAsmtEntityStateId", 3 );
				try
				{
					new ActivityServices().AddActivity( new SiteActivity()
					{ ActivityType = "SupportService", Activity = "Elastic", Event = "Build Index for SupportService" }
					);
					int processed = 0;
					string filter = "( base.EntityStateId >= 2 )";
					//
					processed = General_UpdateIndexForSupportService( filter );
					if ( processed > 0 )
					{
						var refreshResults = EC.Indices.Refresh( CommonIndex );
						new ActivityServices().AddActivity( new SiteActivity()
						{
							ActivityType = "SupportService",
							Activity = "Elastic",
							Event = "Build Index Completed",
							Comment = string.Format( "Completed rebuild of CommonIndex-for SupportService for {0} records.", processed )
						} );
					}
					else
					{
						LoggingHelper.DoTrace( 1, " General_BuildIndexForSupportService - no data was found" );
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, "General_BuildIndexForSupportService" );
				}
				finally
				{

				}
			}
		}


		/// <summary>
		/// Pass a filter to use for updating the index
		/// </summary>
		/// <param name="filter"></param>
		public static int General_UpdateIndexForSupportService( string filter )
		{
			int processed = 0;
			if ( string.IsNullOrWhiteSpace( filter ) )
				return processed;
			int action = UtilityManager.GetAppKeyValue( "updateCredIndexAction", 0 );
			if ( action == 0 )
				return processed;
			string methodName = "General_UpdateIndexForSupportService";
			string IndexName = CommonIndex;
			int pageSize = nonCredentialPageSize;
			int pageNbr = 1;
			int totalRows = 0;
			bool isComplete = false;
			int cntr = 0; ;
			try
			{
				while ( pageNbr > 0 && !isComplete )
				{
					var list = ElasticManager.SupportService_SearchForElastic( filter, pageSize, pageNbr, ref totalRows );
					if ( list != null && list.Count > 0 )
					{
						if ( list.Count == 1 && list[0].Name.ToLower().IndexOf( "exception" ) > -1 )
						{
							LoggingHelper.DoTrace( 2, string.Format( "{0}: NOTE Error encountered during search for data: {1}", methodName, list[0].Description ) );
							isComplete = true;
							break;
						}
						processed = totalRows;
						if ( action == 1 )
						{
							foreach ( var item in list )
							{
								cntr++;
								var res = EC.Index( item, idx => idx.Index( IndexName ) );
								Console.WriteLine( res.Result );
							}
						}
						else if ( action == 2 )
						{
							cntr = cntr + list.Count;
							var results = EC.Bulk( b => b.IndexMany( list, ( d, record ) => d.Index( IndexName ).Document( record ).Id( record.EntityId.ToString() ) ) );
							if ( results.ToString().IndexOf( "Valid NEST response built from a successful" ) == -1 )
							{
								Console.WriteLine( results.ToString() );
								LoggingHelper.DoTrace( 1, string.Format( " Issue building {0}: ", IndexName ) + results.DebugInformation.Substring( 0, 2000 ) );
							}
						}
					}
					else
					{
						if ( list != null && list.Count == 1 && totalRows == -1 )
						{
							LoggingHelper.LogError( string.Format( "{0}: Error in search. {1}", methodName, list[0].Description ) );

						}
						else if ( pageNbr == 1 )
						{
							if ( string.IsNullOrWhiteSpace( filter ) )
							{
								LoggingHelper.LogError(string.Format("{0}: entered with no filter, but no results were returned from search.", methodName));
							}
							LoggingHelper.DoTrace( 2, string.Format( "{0}: NOTE no data returned for filter: {1}", methodName, filter ) );
						}
						isComplete = true;
						break;
					}
					LoggingHelper.DoTrace( 4, string.Format( "{0}. Page: {1}, Records Indexed: {2} 0f {3} records.", methodName, pageNbr, cntr, totalRows ) );
					if ( cntr >= totalRows )
					{
						isComplete = true;
					}

					pageNbr++;
				} //loop
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "{0} failed for filter: {1}", methodName, filter ) );
			}
			return processed;
		}


		#endregion
		#region ScheduledOffering
		public static void General_BuildIndexForScheduledOffering( bool updatingIndexRegardless = true )
		{
			var list = new List<GenericIndex>();

			if ( updatingIndexRegardless )
			{
				LoggingHelper.DoTrace( 1, "General- BuildIndexForScheduledOffering" );
				try
				{
					new ActivityServices().AddActivity( new SiteActivity()
					{ ActivityType = "ScheduledOffering", Activity = "Elastic", Event = "Build Index for ScheduledOffering" }
					);
					int processed = 0;
					//no pending, could have virtual deletes at some point
					string filter = "( base.EntityStateId >= 2 )";
					//
					General_UpdateIndexForScheduledOffering( filter, ref processed );
					if ( processed > 0 )
					{
						var refreshResults = EC.Indices.Refresh( CommonIndex );
						new ActivityServices().AddActivity( new SiteActivity()
						{
							ActivityType = "ScheduledOffering",
							Activity = "Elastic",
							Event = "Build Index Completed",
							Comment = string.Format( "Completed rebuild of CommonIndex-for ScheduledOffering for {0} records.", processed )
						} );
					}
					else
					{
						LoggingHelper.DoTrace( 1, " General_BuildIndexForScheduledOffering - no data was found" );
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, "General_BuildIndexForScheduledOffering" );
				}
				finally
				{

				}
			}
		}


		/// <summary>
		/// Pass a filter to use for updating the index
		/// </summary>
		/// <param name="filter"></param>
		public static void General_UpdateIndexForScheduledOffering( string filter, ref int processed )
		{
			if ( string.IsNullOrWhiteSpace( filter ) )
				return;
			int action = UtilityManager.GetAppKeyValue( "updateCredIndexAction", 0 );
			if ( action == 0 )
				return;
			string methodName = "General_UpdateIndexForScheduledOffering";
			string IndexName = CommonIndex;
			int pageSize = nonCredentialPageSize;
			int pageNbr = 1;
			int totalRows = 0;
			bool isComplete = false;
			int cntr = 0; ;
			try
			{
				while ( pageNbr > 0 && !isComplete )
				{
					var list = ElasticManager.ScheduledOffering_SearchForElastic( filter, pageSize, pageNbr, ref totalRows );
					if ( list != null && list.Count > 0 )
					{
						if ( list.Count == 1 && list[0].Name.ToLower().IndexOf( "exception" ) > -1 )
						{
							LoggingHelper.DoTrace( 2, string.Format( "{0}: NOTE Error encountered during search for data: {1}", methodName, list[0].Description ) );
							isComplete = true;
							break;
						}
						processed = totalRows;
						if ( action == 1 )
						{
							foreach ( var item in list )
							{
								cntr++;
								var res = EC.Index( item, idx => idx.Index( IndexName ) );
								Console.WriteLine( res.Result );
							}
						}
						else if ( action == 2 )
						{
							cntr = cntr + list.Count;
							//22-12-16 NEW using **EntityId** as the PK, otherwise Id in one type overwrites the Id from another type
							var results = EC.Bulk( b => b.IndexMany( list, ( d, record ) => d.Index( IndexName ).Document( record ).Id( record.EntityId.ToString() ) ) );
							if ( results.ToString().IndexOf( "Valid NEST response built from a successful" ) == -1 )
							{
								Console.WriteLine( results.ToString() );
								LoggingHelper.DoTrace( 1, string.Format( " Issue building {0}: ", IndexName ) + results.DebugInformation.Substring( 0, 2000 ) );
							}
						}
					}
					else
					{
						if ( list != null && list.Count == 1 && totalRows == -1 )
						{
							LoggingHelper.LogError( string.Format( "{0}: Error in search. {1}", methodName, list[0].Description ) );

						}
						else if ( pageNbr == 1 )
						{
							if ( string.IsNullOrWhiteSpace( filter ) )
							{
								LoggingHelper.LogError( string.Format( "{0}: entered with no filter, but no results were returned from search.", methodName ) );
							}
							LoggingHelper.DoTrace( 2, string.Format( "{0}: NOTE no data returned for filter: {1}", methodName, filter ) );
						}
						isComplete = true;
						break;
					}
					LoggingHelper.DoTrace( 4, string.Format( "{0}. Page: {1}, Records Indexed: {2} 0f {3} records.", methodName, pageNbr, cntr, totalRows ) );
					if ( cntr >= totalRows )
					{
						isComplete = true;
					}

					pageNbr++;
				} //loop
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "{0} failed for filter: {1}", methodName, filter ) );
			}
		}


		#endregion
		#region Job
		public static void General_BuildIndexForJob( bool updatingIndexRegardless = true )
		{
			var list = new List<GenericIndex>();

			if ( updatingIndexRegardless )
			{
				LoggingHelper.DoTrace( 1, "General- BuildIndexForJob" );
				int minEntityStateId = UtilityManager.GetAppKeyValue( "minAsmtEntityStateId", 3 );
				try
				{
					new ActivityServices().AddActivity( new SiteActivity()
					{ ActivityType = "Job", Activity = "Elastic", Event = "Build Index for Job" }
					);
					int processed = 0;
					string filter = "( base.EntityStateId >= 2 )";
					//
					General_UpdateIndexForJob( filter, ref processed );
					if ( processed > 0 )
					{
						var refreshResults = EC.Indices.Refresh( CommonIndex );
						new ActivityServices().AddActivity( new SiteActivity()
						{
							ActivityType = "Job",
							Activity = "Elastic",
							Event = "Build Index Completed",
							Comment = string.Format( "Completed rebuild of CommonIndex-for Job for {0} records.", processed )
						} );
					}
					else
					{
						LoggingHelper.DoTrace( 1, " General_BuildIndexForJob - no data was found" );

					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, "General_BuildIndexForJob" );
				}
				finally
				{

				}
			}
		}


		/// <summary>
		/// Pass a filter to use for updating the index
		/// </summary>
		/// <param name="filter"></param>
		public static void General_UpdateIndexForJob( string filter, ref int processed )
		{
			if ( string.IsNullOrWhiteSpace( filter ) )
				return;
			int action = UtilityManager.GetAppKeyValue( "updateCredIndexAction", 0 );
			if ( action == 0 )
				return;
			string methodName = "General_UpdateIndexForJob";
			string IndexName = CommonIndex;
			int pageSize = nonCredentialPageSize;
			int pageNbr = 1;
			int totalRows = 0;
			bool isComplete = false;
			int cntr = 0; ;
			try
			{
				while ( pageNbr > 0 && !isComplete )
				{
					var list = ElasticManager.Job_SearchForElastic( filter, pageSize, pageNbr, ref totalRows );
					if ( list != null && list.Count > 0 )
					{
						if ( list.Count == 1 && list[0].Name.ToLower().IndexOf( "exception" ) > -1 )
						{
							LoggingHelper.DoTrace( 2, string.Format( "{0}: NOTE Error encountered during search for data: {1}", methodName, list[0].Description ) );
							isComplete = true;
							break;
						}
						processed = totalRows;
						if ( action == 1 )
						{
							foreach ( var item in list )
							{
								cntr++;
								var res = EC.Index( item, idx => idx.Index( IndexName ) );
								Console.WriteLine( res.Result );
							}
						}
						else if ( action == 2 )
						{
							cntr = cntr + list.Count;
							var results = EC.Bulk( b => b.IndexMany( list, ( d, record ) => d.Index( IndexName ).Document( record ).Id( record.EntityId.ToString() ) ) );
							if ( results.ToString().IndexOf( "Valid NEST response built from a successful" ) == -1 )
							{
								Console.WriteLine( results.ToString() );
								LoggingHelper.DoTrace( 1, string.Format( " Issue building {0}: ", IndexName ) + results.DebugInformation.Substring( 0, 2000 ) );
							}
						}
					}
					else
					{
						if ( list != null && list.Count == 1 && totalRows == -1 )
						{
							LoggingHelper.LogError( string.Format( "{0}: Error in search. {1}", methodName, list[0].Description ) );

						}
						else if ( pageNbr == 1 )
						{
							if ( string.IsNullOrWhiteSpace( filter ) )
							{
								LoggingHelper.LogError( string.Format( "{0}: entered with no filter, but no results were returned from search.", methodName ) );
							}
							LoggingHelper.DoTrace( 2, string.Format( "{0}: NOTE no data returned for filter: {1}", methodName, filter ) );
						}
						isComplete = true;
						break;
					}
					LoggingHelper.DoTrace( 4, string.Format( "{0}. Page: {1}, Records Indexed: {2} 0f {3} records.", methodName, pageNbr, cntr, totalRows ) );
					if ( cntr >= totalRows )
					{
						isComplete = true;
					}

					pageNbr++;
				} //loop
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "{0} failed for filter: {1}", methodName, filter ) );
			}
		}


		#endregion

		#region Occupation
		public static void General_BuildIndexForOccupation()
		{
			var list = new List<GenericIndex>();

			LoggingHelper.DoTrace( 1, "General- BuildIndexForOccupation" );
			int minEntityStateId = UtilityManager.GetAppKeyValue( "minAsmtEntityStateId", 3 );
			try
			{
				new ActivityServices().AddActivity( new SiteActivity()
				{ ActivityType = "Occupation", Activity = "Elastic", Event = "Build Index for Occupation" }
				);
				int processed = 0;
				string filter = "( base.EntityStateId >= 2 )";
				//
				General_UpdateIndexForOccupation( filter, ref processed );
				if ( processed > 0 )
				{
					var refreshResults = EC.Indices.Refresh( CommonIndex );
					new ActivityServices().AddActivity( new SiteActivity()
					{
						ActivityType = "Occupation",
						Activity = "Elastic",
						Event = "Build Index Completed",
						Comment = string.Format( "Completed rebuild of CommonIndex-for Occupation for {0} records.", processed )
					} );
				}
				else
				{
					LoggingHelper.DoTrace( 1, " General_BuildIndexForOccupation - no data was found" );

				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "General_BuildIndexForOccupation" );
			}
			finally
			{

			}

		}


		/// <summary>
		/// Pass a filter to use for updating the index
		/// </summary>
		/// <param name="filter"></param>
		public static void General_UpdateIndexForOccupation( string filter, ref int processed )
		{
			if ( string.IsNullOrWhiteSpace( filter ) )
				return;
			int action = UtilityManager.GetAppKeyValue( "updateCredIndexAction", 0 );
			if ( action == 0 )
				return;
			string methodName = "General_UpdateIndexForOccupation";
			string IndexName = CommonIndex;
			int pageSize = nonCredentialPageSize;
			int pageNbr = 1;
			int totalRows = 0;
			bool isComplete = false;
			int cntr = 0; ;
			try
			{
				while ( pageNbr > 0 && !isComplete )
				{
					var list = ElasticManager.Occupation_SearchForElastic( filter, pageSize, pageNbr, ref totalRows );
					if ( list != null && list.Count > 0 )
					{
						if ( list.Count == 1 && list[0].Name.ToLower().IndexOf( "exception" ) > -1 )
						{
							LoggingHelper.DoTrace( 2, string.Format( "{0}: NOTE Error encountered during search for data: {1}", methodName, list[0].Description ) );
							isComplete = true;
							break;
						}
						processed = totalRows;
						if ( action == 1 )
						{
							foreach ( var item in list )
							{
								cntr++;
								var res = EC.Index( item, idx => idx.Index( IndexName ) );
								Console.WriteLine( res.Result );
							}
						}
						else if ( action == 2 )
						{
							cntr = cntr + list.Count;
							var results = EC.Bulk( b => b.IndexMany( list, ( d, record ) => d.Index( IndexName ).Document( record ).Id( record.EntityId.ToString() ) ) );
							if ( results.ToString().IndexOf( "Valid NEST response built from a successful" ) == -1 )
							{
								Console.WriteLine( results.ToString() );
								LoggingHelper.DoTrace( 1, string.Format( " Issue building {0}: ", IndexName ) + results.DebugInformation.Substring( 0, 2000 ) );
							}
						}
					}
					else
					{
						if ( list != null && list.Count == 1 && totalRows == -1 )
						{
							LoggingHelper.LogError( string.Format( "{0}: Error in search. {1}", methodName, list[0].Description ) );

						}
						else if ( pageNbr == 1 )
						{
							if ( string.IsNullOrWhiteSpace( filter ) )
							{
								LoggingHelper.LogError(string.Format("{0}: entered with no filter, but no results were returned from search.", methodName));
							}
							LoggingHelper.DoTrace( 2, string.Format( "{0}: NOTE no data returned for filter: {1}", methodName, filter ) );
						}
						isComplete = true;
						break;
					}
					LoggingHelper.DoTrace( 4, string.Format( "{0}. Page: {1}, Records Indexed: {2} 0f {3} records.", methodName, pageNbr, cntr, totalRows ) );
					if ( cntr >= totalRows )
					{
						isComplete = true;
					}

					pageNbr++;
				} //loop
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "{0} failed for filter: {1}", methodName, filter ) );
			}
		}


		#endregion
		#region Task
		public static void General_BuildIndexForTask( bool updatingIndexRegardless = true )
		{
			var list = new List<GenericIndex>();

			if ( updatingIndexRegardless )
			{
				LoggingHelper.DoTrace( 1, "General- BuildIndexForTask" );
				int minEntityStateId = UtilityManager.GetAppKeyValue( "minAsmtEntityStateId", 3 );
				try
				{
					new ActivityServices().AddActivity( new SiteActivity()
					{ ActivityType = "Task", Activity = "Elastic", Event = "Build Index for Task" }
					);
					int processed = 0;
					string filter = "( base.EntityStateId >= 2 )";
					//
					General_UpdateIndexForTask( filter, ref processed );
					if ( processed > 0 )
					{
						var refreshResults = EC.Indices.Refresh( CommonIndex );
						new ActivityServices().AddActivity( new SiteActivity()
						{
							ActivityType = "Task",
							Activity = "Elastic",
							Event = "Build Index Completed",
							Comment = string.Format( "Completed rebuild of CommonIndex-for Task for {0} records.", processed )
						} );
					}
					else
					{
						LoggingHelper.DoTrace( 1, " General_BuildIndexForTask - no data was found" );

					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, "General_BuildIndexForTask" );
				}
				finally
				{

				}
			}
		}


		/// <summary>
		/// Pass a filter to use for updating the index
		/// </summary>
		/// <param name="filter"></param>
		public static void General_UpdateIndexForTask( string filter, ref int processed )
		{
			if ( string.IsNullOrWhiteSpace( filter ) )
				return;
			int action = UtilityManager.GetAppKeyValue( "updateCredIndexAction", 0 );
			if ( action == 0 )
				return;
			string methodName = "General_UpdateIndexForTask";
			string IndexName = CommonIndex;
			int pageSize = nonCredentialPageSize;
			int pageNbr = 1;
			int totalRows = 0;
			bool isComplete = false;
			int cntr = 0; ;
			try
			{
				while ( pageNbr > 0 && !isComplete )
				{
					var list = ElasticManager.Task_SearchForElastic( filter, pageSize, pageNbr, ref totalRows );
					if ( list != null && list.Count > 0 )
					{
						if ( list.Count == 1 && list[0].Name.ToLower().IndexOf( "exception" ) > -1 )
						{
							LoggingHelper.DoTrace( 2, string.Format( "{0}: NOTE Error encountered during search for data: {1}", methodName, list[0].Description ) );
							isComplete = true;
							break;
						}
						processed = totalRows;
						if ( action == 1 )
						{
							foreach ( var item in list )
							{
								cntr++;
								var res = EC.Index( item, idx => idx.Index( IndexName ) );
								Console.WriteLine( res.Result );
							}
						}
						else if ( action == 2 )
						{
							cntr = cntr + list.Count;
							var results = EC.Bulk( b => b.IndexMany( list, ( d, record ) => d.Index( IndexName ).Document( record ).Id( record.EntityId.ToString() ) ) );
							if ( results.ToString().IndexOf( "Valid NEST response built from a successful" ) == -1 )
							{
								Console.WriteLine( results.ToString() );
								LoggingHelper.DoTrace( 1, string.Format( " Issue building {0}: ", IndexName ) + results.DebugInformation.Substring( 0, 2000 ) );
							}
						}
					}
					else
					{
						if ( list != null && list.Count == 1 && totalRows == -1 )
						{
							LoggingHelper.LogError( string.Format( "{0}: Error in search. {1}", methodName, list[0].Description ) );

						}
						else if ( pageNbr == 1 )
						{
							if ( string.IsNullOrWhiteSpace( filter ) )
							{
								LoggingHelper.LogError(string.Format("{0}: entered with no filter, but no results were returned from search.", methodName));
							}
							LoggingHelper.DoTrace( 2, string.Format( "{0}: NOTE no data returned for filter: {1}", methodName, filter ) );
						}
						isComplete = true;
						break;
					}
					LoggingHelper.DoTrace( 4, string.Format( "{0}. Page: {1}, Records Indexed: {2} 0f {3} records.", methodName, pageNbr, cntr, totalRows ) );
					if ( cntr >= totalRows )
					{
						isComplete = true;
					}

					pageNbr++;
				} //loop
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "{0} failed for filter: {1}", methodName, filter ) );
			}
		}


		#endregion
		#region WorkRole
		public static void General_BuildIndexForWorkRole( bool updatingIndexRegardless = true )
		{
			var list = new List<GenericIndex>();

			if ( updatingIndexRegardless )
			{
				LoggingHelper.DoTrace( 1, "General- BuildIndexForWorkRole" );
				int minEntityStateId = UtilityManager.GetAppKeyValue( "minAsmtEntityStateId", 3 );
				try
				{
					new ActivityServices().AddActivity( new SiteActivity()
					{ ActivityType = "WorkRole", Activity = "Elastic", Event = "Build Index for WorkRole" }
					);
					int processed = 0;
					string filter = "( base.EntityStateId >= 2 )";
					//
					General_UpdateIndexForWorkRole( filter, ref processed );
					if ( processed > 0 )
					{
						var refreshResults = EC.Indices.Refresh( CommonIndex );
						new ActivityServices().AddActivity( new SiteActivity()
						{
							ActivityType = "WorkRole",
							Activity = "Elastic",
							Event = "Build Index Completed",
							Comment = string.Format( "Completed rebuild of CommonIndex-for WorkRole for {0} records.", processed )
						} );
					}
					else
					{
						LoggingHelper.DoTrace( 1, " General_BuildIndexForWorkRole - no data was found" );

					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, "General_BuildIndexForWorkRole" );
				}
				finally
				{

				}
			}
		}


		/// <summary>
		/// Pass a filter to use for updating the index
		/// </summary>
		/// <param name="filter"></param>
		public static void General_UpdateIndexForWorkRole( string filter, ref int processed )
		{
			if ( string.IsNullOrWhiteSpace( filter ) )
				return;
			int action = UtilityManager.GetAppKeyValue( "updateCredIndexAction", 0 );
			if ( action == 0 )
				return;
			string methodName = "General_UpdateIndexForWorkRole";
			string IndexName = CommonIndex;
			int pageSize = nonCredentialPageSize;
			int pageNbr = 1;
			int totalRows = 0;
			bool isComplete = false;
			int cntr = 0; ;
			try
			{
				while ( pageNbr > 0 && !isComplete )
				{
					var list = ElasticManager.WorkRole_SearchForElastic( filter, pageSize, pageNbr, ref totalRows );
					if ( list != null && list.Count > 0 )
					{
						if ( list.Count == 1 && list[0].Name.ToLower().IndexOf( "exception" ) > -1 )
						{
							LoggingHelper.DoTrace( 2, string.Format( "{0}: NOTE Error encountered during search for data: {1}", methodName, list[0].Description ) );
							isComplete = true;
							break;
						}
						processed = totalRows;
						if ( action == 1 )
						{
							foreach ( var item in list )
							{
								cntr++;
								var res = EC.Index( item, idx => idx.Index( IndexName ) );
								Console.WriteLine( res.Result );
							}
						}
						else if ( action == 2 )
						{
							cntr = cntr + list.Count;
							var results = EC.Bulk( b => b.IndexMany( list, ( d, record ) => d.Index( IndexName ).Document( record ).Id( record.EntityId.ToString() ) ) );
							if ( results.ToString().IndexOf( "Valid NEST response built from a successful" ) == -1 )
							{
								Console.WriteLine( results.ToString() );
								LoggingHelper.DoTrace( 1, string.Format( " Issue building {0}: ", IndexName ) + results.DebugInformation.Substring( 0, 2000 ) );
							}
						}
					}
					else
					{
						if ( list != null && list.Count == 1 && totalRows == -1 )
						{
							LoggingHelper.LogError( string.Format( "{0}: Error in search. {1}", methodName, list[0].Description ) );

						}
						else if ( pageNbr == 1 )
						{
							if ( string.IsNullOrWhiteSpace( filter ) )
							{
								LoggingHelper.LogError(string.Format("{0}: entered with no filter, but no results were returned from search.", methodName));
							}
							LoggingHelper.DoTrace( 2, string.Format( "{0}: NOTE no data returned for filter: {1}", methodName, filter ) );
						}
						isComplete = true;
						break;
					}
					LoggingHelper.DoTrace( 4, string.Format( "{0}. Page: {1}, Records Indexed: {2} 0f {3} records.", methodName, pageNbr, cntr, totalRows ) );
					if ( cntr >= totalRows )
					{
						isComplete = true;
					}

					pageNbr++;
				} //loop
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "{0} failed for filter: {1}", methodName, filter ) );
			}
		}


		#endregion
		#region Rubric
		public static void General_BuildIndexForRubric( bool updatingIndexRegardless = true )
		{
			var list = new List<GenericIndex>();

			if ( updatingIndexRegardless )
			{
				LoggingHelper.DoTrace( 1, "General- BuildIndexForRubric" );
				int minEntityStateId = UtilityManager.GetAppKeyValue( "minAsmtEntityStateId", 3 );
				try
				{
					new ActivityServices().AddActivity( new SiteActivity()
					{ ActivityType = "Rubric", Activity = "Elastic", Event = "Build Index for Rubric" }
					);
					int processed = 0;
					string filter = "( base.EntityStateId >= 2 )";
					//
					General_UpdateIndexForRubric( filter, ref processed );
					if ( processed > 0 )
					{
						var refreshResults = EC.Indices.Refresh( CommonIndex );
						new ActivityServices().AddActivity( new SiteActivity()
						{
							ActivityType = "Rubric",
							Activity = "Elastic",
							Event = "Build Index Completed",
							Comment = string.Format( "Completed rebuild of CommonIndex-for Rubric for {0} records.", processed )
						} );
					}
					else
					{
						LoggingHelper.DoTrace( 1, " General_BuildIndexForRubric - no data was found" );
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, "General_BuildIndexForRubric" );
				}
				finally
				{

				}
			}
		}

		/// <summary>
		/// Pass a filter to use for updating the index
		/// </summary>
		/// <param name="filter"></param>
		public static void General_UpdateIndexForRubric( string filter, ref int processed )
		{
			if ( string.IsNullOrWhiteSpace( filter ) )
				return;
			int action = UtilityManager.GetAppKeyValue( "updateCredIndexAction", 0 );
			if ( action == 0 )
				return;
			string methodName = "General_UpdateIndexForRubric";
			string IndexName = CommonIndex;
			int pageSize = nonCredentialPageSize;
			int pageNbr = 1;
			int totalRows = 0;
			bool isComplete = false;
			int cntr = 0; ;
			try
			{
				while ( pageNbr > 0 && !isComplete )
				{
					var list = ElasticManager.Rubric_SearchForElastic( filter, pageSize, pageNbr, ref totalRows );
					if ( list != null && list.Count > 0 )
					{
						if ( list.Count == 1 && list[0].Name.ToLower().IndexOf( "exception" ) > -1 )
						{
							LoggingHelper.DoTrace( 2, string.Format( "{0}: NOTE Error encountered during search for data: {1}", methodName, list[0].Description ) );
							isComplete = true;
							break;
						}
						processed = totalRows;
						if ( action == 1 )
						{
							foreach ( var item in list )
							{
								cntr++;
								var res = EC.Index( item, idx => idx.Index( IndexName ) );
								Console.WriteLine( res.Result );
							}
						}
						else if ( action == 2 )
						{
							cntr = cntr + list.Count;
							var results = EC.Bulk( b => b.IndexMany( list, ( d, record ) => d.Index( IndexName ).Document( record ).Id( record.EntityId.ToString() ) ) );
							if ( results.ToString().IndexOf( "Valid NEST response built from a successful" ) == -1 )
							{
								Console.WriteLine( results.ToString() );
								LoggingHelper.DoTrace( 1, string.Format( " Issue building {0}: ", IndexName ) + results.DebugInformation.Substring( 0, 2000 ) );
							}
						}
					}
					else
					{
						if ( list != null && list.Count == 1 && totalRows == -1 )
						{
							LoggingHelper.LogError( string.Format( "{0}: Error in search. {1}", methodName, list[0].Description ) );

						}
						else if ( pageNbr == 1 )
						{
							if ( string.IsNullOrWhiteSpace( filter ) )
							{
								LoggingHelper.LogError( string.Format( "{0}: entered with no filter, but no results were returned from search.", methodName ) );
							}
							LoggingHelper.DoTrace( 2, string.Format( "{0}: NOTE no data returned for filter: {1}", methodName, filter ) );
						}
						isComplete = true;
						break;
					}
					LoggingHelper.DoTrace( 4, string.Format( "{0}. Page: {1}, Records Indexed: {2} 0f {3} records.", methodName, pageNbr, cntr, totalRows ) );
					if ( cntr >= totalRows )
					{
						isComplete = true;
					}

					pageNbr++;
				} //loop
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "{0} failed for filter: {1}", methodName, filter ) );
			}
		}


		#endregion

		/// <summary>
		/// Delete and rebuild the general index
		/// </summary>
		public static void GeneralIndex_Reset()
		{

			if ( EC.Indices.Exists( CommonIndex ).Exists )
			{
				LoggingHelper.DoTrace( 1, " GeneralIndex_Reset - deleting CommonIndex" );
				EC.Indices.Delete( CommonIndex );
			}
			if ( !EC.Indices.Exists( CommonIndex ).Exists )
			{
				General_InitializeIndex();
			}
		}
		//
		/// <summary>
		/// If requested, must include all resource types under general. Currently:
		/// - transfer value
		/// - transfer intermediary
		/// - collection
		/// </summary>
		/// <param name="deleteIndexFirst"></param>
		/// <param name="updatingIndexRegardless"></param>
		public static void General_BuildIndex( bool deleteIndexFirst = false, bool updatingIndexRegardless = false )
		{
			var list = new List<GenericIndex>();

			bool indexInitialized = false;
			if ( deleteIndexFirst && EC.Indices.Exists( CommonIndex ).Exists )
			{
				LoggingHelper.DoTrace( 1, " General_BuildIndex - deleting CommonIndex" );
				EC.Indices.Delete( CommonIndex );
			}
			if ( !EC.Indices.Exists( CommonIndex ).Exists )
			{
				General_InitializeIndex();
				indexInitialized = true;
			}
			if ( DateTime.Now.ToString( "yyyy-MM-dd" ) == "2023-11-21" && UtilityManager.GetAppKeyValue( "environment" ) == "development" )
			{
				//General_BuildIndexForCredentialingAction();
			}

			if ( indexInitialized || updatingIndexRegardless )
			{
				LoggingHelper.DoTrace( 1, "CommonIndex- Building Index" );
				int minEntityStateId = UtilityManager.GetAppKeyValue( "minAsmtEntityStateId", 3 );
				try
				{
					int processed = 0;
					string filter = "( base.EntityStateId >= 2 )";
					//*************pathways ****************************************

					//new ActivityServices().AddActivity( new SiteActivity()
					//{ ActivityType = "Pathway", Activity = "Elastic", Event = "Build Index" }
					//);

					//
					//Pathway_UpdateIndex( filter, ref processed );
					//{
					//	var refreshResults = EC.Indices.Refresh( PathwayIndex );
					//	new ActivityServices().AddActivity( new SiteActivity()
					//	{
					//		ActivityType = "Pathway",
					//		Activity = "Elastic",
					//		Event = "Build Index Completed",
					//		Comment = string.Format( "Completed rebuild of PathwayIndex for {0} records.", processed )
					//	} );
					//}

					//*************transfer value ****************************************
					processed = 0;
					filter = "( base.EntityStateId >= 2 )";
					//20-12-28 - update to be like credentials
					General_UpdateIndexForTVP( filter, ref processed );
					var totalCount = processed;

					processed = 0;
					General_UpdateIndexForTransferIntermediary( filter, ref processed );
					totalCount += processed;

					processed = 0;
					//something wrong with this, wipes out transfer intermediary!
					//23-06-15 mp - this should be fixed now?
					General_UpdateIndexForCollection( filter, ref processed );
					totalCount += processed;
					//
					processed = General_UpdateIndexForSupportService( filter );
					totalCount += processed;
					//
					General_UpdateIndexForOutcomeData( filter, ref processed );
					totalCount += processed;

					processed = 0;
					General_UpdateIndexForScheduledOffering( filter, ref processed );
					totalCount += processed;

					processed = 0;
					General_UpdateIndexForConceptScheme( filter, ref processed );
					totalCount += processed;

					processed = 0;
					General_UpdateIndexForOccupation( filter, ref processed );
					totalCount += processed;

					processed = 0;
					General_UpdateIndexForProgressionModel( filter, ref processed );
					totalCount += processed;

					processed = 0;
					General_UpdateIndexForJob( filter, ref processed );
					totalCount += processed;

					processed = 0;
					General_UpdateIndexForTask( filter, ref processed );
					totalCount += processed;

					processed = 0;
					General_UpdateIndexForWorkRole( filter, ref processed );
					totalCount += processed;

					processed = 0;
					General_UpdateIndexForCredentialingAction( filter, ref processed );
					totalCount += processed;

					processed = 0;
					//
					if ( totalCount > 0 )
					{
						//??is this done in Assessment_UpdateIndex
						var refreshResults = EC.Indices.Refresh( CommonIndex );
						new ActivityServices().AddActivity( new SiteActivity()
						{
							ActivityType = "Common Index",
							Activity = "Elastic",
							Event = "Build Common Index Completed for TVP, TI, and Collection",
							Comment = string.Format( "Completed rebuild of CommonIndex for TVP, TI, and Collection: {0} records.", totalCount )
						} );
					}

					//

				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, "General_BuildIndex-Pathway" );
				}
				finally
				{
					//if ( list != null && list.Count > 0 )
					//{
					//	var results = EC.Bulk( b => b.IndexMany( list, ( d, document ) => d.Index( CommonIndex ).Document( document ).Id( document.Id.ToString() ) ) );
					//	if ( results.ToString().IndexOf( "Valid NEST response built from a successful low level cal" ) == -1 )
					//	{
					//		Console.WriteLine( results.ToString() );
					//		LoggingHelper.DoTrace( 1, " Issue building Pathway/General index: " + results.DebugInformation.Substring( 0, 2000 ) );
					//	}

					//	EC.Indices.Refresh( CommonIndex );
					//}
				}
			}
		}

		public static void General_InitializeIndex( bool deleteIndexFirst = true )
		{
			if ( !EC.Indices.Exists( CommonIndex ).Exists )
			{

				var results = EC.Indices.Create( CommonIndex, c => new CreateIndexDescriptor( CommonIndex )
				 .Settings( st => st
						 .Analysis( an => an.TokenFilters( tf => tf.Stemmer( "custom_english_stemmer", ts => ts.Language( "english" ) ) ).Analyzers( anz => anz.Custom( "custom_lowercase_stemmed", cc => cc.Tokenizer( "standard" ).Filters( new List<string> { "lowercase", "custom_english_stemmer" } ) ) ) 
						 )
						.Setting( UpdatableIndexSettings.MaxResultWindow, 100000 )  //not sure of implications?

						 )
						//.Mappings( ms => ms
						.Map<GenericIndex>( m => m
							.AutoMap()
							.Properties( p => p
								.Text( s => s.Name( n => n.Description ).Analyzer( "custom_lowercase_stemmed" ) )
								.Nested<IndexReferenceFramework>( n => n
									.Name( nn => nn.Industries )
									.AutoMap()
								)
								.Nested<IndexReferenceFramework>( n => n
									.Name( nn => nn.Occupations )
									.AutoMap()
								)
								.Nested<IndexReferenceFramework>( n => n
									.Name( nn => nn.InstructionalPrograms )
									.AutoMap()
								)
								.Nested<Models.Elastic.Address>( n => n
									.Name( nn => nn.Addresses )
									.AutoMap()
								) //AgentRelationshipForEntity will replace IndexQualityAssurance
								 .Nested<Models.Elastic.AgentRelationshipForEntity>( n => n
									.Name( nn => nn.AgentRelationshipsForEntity )
									.AutoMap()
								)
								 .Nested<Models.Elastic.IndexEntityReference>( n => n
									.Name( nn => nn.TransferValueFor )
									.AutoMap()
								)
								 .Nested<Models.Elastic.IndexEntityReference>( n => n
									.Name( nn => nn.TransferValueFrom )
									.AutoMap()
								)
								.Object<JObject>( o => o
									.Name( n => n.ResourceDetail )
									.Enabled( false ) )
							)
						)
				//)	//.Mappings
				);
			}
		}
		#endregion


		#endregion

		#region Credentials
		#region Build/update index
		public static void Credential_BuildIndex( bool deleteIndexFirst = false, bool updatingIndexRegardless = false )
		{
			try
			{

				bool indexInitialized = false;
				if ( deleteIndexFirst && EC.Indices.Exists( CredentialIndex ).Exists )
				{
					EC.Indices.Delete( CredentialIndex );
				}
				if ( !EC.Indices.Exists( CredentialIndex ).Exists )
				{
					//if(UtilityManager.GetAppKeyValue( "environment" ) == "development" )
					CredentialInitializeIndex7();
					//else 
					//	CredentialInitializeIndex(); 
					indexInitialized = true;
				}

				if ( indexInitialized || updatingIndexRegardless )
				{
					LoggingHelper.DoTrace( 1, "Credential - Building Index" );
					new ActivityServices().AddActivity( new SiteActivity()
					{ ActivityType = "Credential", Activity = "Elastic", Event = "Build Index" }
					);
					//redentialInitializeIndex();
					int minEntityStateId = UtilityManager.GetAppKeyValue( "minEntityStateId", 3 );
					int processed = 0;
					string filter = string.Format( "base.EntityStateId >= {0}", minEntityStateId );
					Credential_UpdateIndex( filter, ref processed );
					if ( processed > 0 )
					{
						var refreshResults = EC.Indices.Refresh( CredentialIndex );
						new ActivityServices().AddActivity( new SiteActivity()
						{
							ActivityType = "Credential",
							Activity = "Elastic",
							Event = "Build Index Completed",
							Comment = string.Format( "Completed rebuild of Credential Index for {0} records.", processed )
						} );
					}
					else
					{
						//ISSUE
						LoggingHelper.LogError("BuildCredentialIndex: no results were returned from Credential_SearchForElastic method.");
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "BuildCredentialIndex" );
			}
		}
		public static void Credential_UpdateIndex( int recordId )
		{
			if ( recordId < 1 )
				return;
			int action = UtilityManager.GetAppKeyValue( "updateCredIndexAction", 0 );
			if ( action == 0 )
				return;
			try
			{
				string filter = string.Format( " ( base.Id = {0} ) ", recordId );
				int processed = 0;
				Credential_UpdateIndex( filter, ref processed );

			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "Credential_UpdateIndex failed for id: {0}", recordId ) );
			}
		}

		/// <summary>
		/// Pass a filter to use for updating the index
		/// </summary>
		/// <param name="filter"></param>
		public static void Credential_UpdateIndex( string filter, ref int processed )
		{
			processed = 0;
			if ( string.IsNullOrWhiteSpace( filter ) )
				return;
			int action = UtilityManager.GetAppKeyValue( "updateCredIndexAction", 0 );
			if ( action == 0 )
				return;

			string methodName = "Credential_UpdateIndex";
			string IndexName = CredentialIndex;
			LoggingHelper.DoTrace( 6, $"{methodName}. Enter. Index: {IndexName} Filter: '" + filter + "' " );



			try
			{
				//actually this doesn't help unless we use an upating filter to target specific data!
				int pageSize = UtilityManager.GetAppKeyValue( "credentialRebuildPageSize", 999 );
				int pageNbr = 1;
				int totalRows = 0;
				bool isComplete = false;
				int cntr = 0; ;
				while ( pageNbr > 0 && !isComplete )
				{
					LoggingHelper.DoTrace( 5, $"Credential_UpdateIndex. PageNbr: {pageNbr}, PageSize: {pageSize}" );
					var list = ElasticManager.Credential_SearchForElastic( filter, pageSize, pageNbr, ref totalRows );
					if ( list != null && list.Count > 0 && totalRows > 0 )
					{
						//if error encountered, first record can be an error message. 
						processed += list.Count;
						if ( action == 1 )
						{
							foreach ( var item in list )
							{
								cntr++;
								var res = EC.Index( item, idx => idx.Index( CredentialIndex ) );
								Console.WriteLine( res.Result );
							}

						}
						else if ( action == 2 )
						{
							cntr = cntr + list.Count;
							var result = EC.Bulk( b => b.IndexMany( list, ( d, credential ) => d.Index( CredentialIndex ).Document( credential ).Id( credential.Id.ToString() ) ) );
							if ( result.ToString().IndexOf( "Valid NEST response built from a successful" ) == -1 )
							{
								//Console.WriteLine( result.ToString() );
								var msg = "";
								foreach ( var item in result.Items.Where( m => m.Error != null ).ToList() )
								{
									if ( item.Error != null )
										msg += item.Error.ToString() + "\r\n";
								}
								LoggingHelper.DoTrace( 1, "***** Issue building credential index with filter: '" + filter + "' == " + msg );
								LoggingHelper.LogError( $"Credential_UpdateIndex: page: {pageNbr}. Error in IndexBuild. {msg}" );
							}
						}
					}
					else
					{
						if ( list != null && list.Count == 1 && totalRows == -1 )
						{
							LoggingHelper.LogError( string.Format( "Credential_UpdateIndex: Error in search. Filter: {0}, message: {1}", filter, list[0].Description ) );

						}
						else if ( pageNbr == 1 )
						{
							if ( string.IsNullOrWhiteSpace( filter ) )
							{
								LoggingHelper.LogError( "Credential_UpdateIndex: entered with no filter, but no results were returned from credential search." );
							}
							LoggingHelper.DoTrace( 2, string.Format( "Credential_UpdateIndex. NOTE no data returned for filter: {0}", filter ) );
						}
						isComplete = true;
						break;
					}

					LoggingHelper.DoTrace( 4, string.Format( "{0}. Page: {1}, Records Indexed: {2} of {3}", methodName, pageNbr, processed, totalRows ) );
					pageNbr++;
					if ( cntr >= totalRows )
					{
						isComplete = true;
					}

				} //loop
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "Credential_UpdateIndex failed for filter: {0}", filter ) );
			}
		}

		public static object CredentialDeleteDocument( int documentId )
		{
			//ex: var response = EsClient.Delete<Employee>(2, d => d.Index("employee").Type("myEmployee"));
			var response = EC.Delete<CredentialIndex>( documentId, d => d.Index( CredentialIndex ) );

			//bulk deletes. Will want a bulk delete when using SearchPendingReindex!
			//https://stackoverflow.com/questions/31028839/how-to-delete-several-documents-by-id-in-one-operation-using-elasticsearch-nest/31029136
			/*
             * To use esClient.DeleteMany(..) you have to pass collection of objects to delete.

            var objectsToDelete = new List<YourType> {.. };
            var bulkResponse = client.DeleteMany<YourType>(objectsToDelete);
            
            *You can get around this by using following code:

            var ids = new List<string> {"1", "2", "3"};
            var bulkResponse = client.DeleteMany<YourType>(ids.Select(x => new YourType { Id = x }));
            
            *Third option, use bulk delete:

            var bulkResponse = client.Bulk(new BulkRequest
            {
                Operations = ids.Select(x => new BulkDeleteOperation<YourType>(x)).Cast<IBulkOperation>().ToList()
            });
             * 
             */
			return response;
		}

		//public static void CredentialInitializeIndex()
		//{
		//	if ( !EC.Indices.Exists( CredentialIndex ).Exists )
		//	{
		//		// .String(s => s.Index(FieldIndexOption.NotAnalyzed).Name(n => n.Name))
		//		EC.Indices.Create( CredentialIndex, c => new CreateIndexDescriptor( CredentialIndex )
		//			  .Settings( st => st
		//				 .Analysis( an => an.TokenFilters( tf => tf.Stemmer( "custom_english_stemmer", ts => ts.Language( "english" ) ) )
		//					.Analyzers( anz => anz
		//						.Custom( "custom_lowercase_stemmed", cc => cc.Tokenizer( "standard" ).Filters( new List<string> { "lowercase", "custom_english_stemmer" } ) )
		//						)
		//					) ) //Settings
		//						//21-05-31 mparsons - as Mappings is obsolete in e7x, changed to use Map and commented the first instance of Map
		//						//		backed out - only 10000 records would appear in search=> still only 10000 using original search
		//				  .Mappings( ms => ms
		//					.Map<CredentialIndex>( m => m
		//						.AutoMap()
		//						.Properties( p => p
		//							.Text( s => s.Name( n => n.Description ).Analyzer( "custom_lowercase_stemmed" )
		//						 )
		//						//.Text( s => s.Name( n => n.Name ).Analyzer( "my_analyzer" )) .Fields( f=> f.Text (t => t.Analyzer( "english_exav" ) )
		//						.Nested<IndexSubject>( n => n
		//							.Name( nn => nn.Subjects )
		//							.AutoMap()
		//						)
		//						.Nested<IndexReferenceFramework>( n => n
		//							.Name( nn => nn.Industries )
		//							.AutoMap()
		//						)
		//						.Nested<IndexReferenceFramework>( n => n
		//							.Name( nn => nn.Occupations )
		//							.AutoMap()
		//						)
		//						.Nested<IndexReferenceFramework>( n => n
		//							.Name( nn => nn.InstructionalPrograms )
		//							.AutoMap()
		//						)
		//						.Nested<IndexCompetency>( n => n
		//							.Name( nn => nn.Competencies )
		//							.AutoMap()
		//						)
		//						//.GeoPoint( g => g.Name( n => n.AddressLocations ) )
		//						.Nested<Models.Elastic.Address>( n => n
		//							.Name( nn => nn.Addresses )
		//							.AutoMap()
		//							) //AgentRelationshipForEntity will replace IndexQualityAssurance
		//						.Nested<Models.Elastic.AgentRelationshipForEntity>( n => n
		//							.Name( nn => nn.AgentRelationshipsForEntity )
		//							.AutoMap()
		//							)
		//						.Nested<Models.Elastic.AgentRelationshipForEntity>( n => n
		//							.Name( nn => nn.OutcomeProvidersForEntity )
		//							.AutoMap()
		//						)
		//						//.Nested<IndexQualityAssurance>( n => n
		//						//	.Name( nn => nn.QualityAssurance )
		//						//	.AutoMap()
		//						//)
		//						//.Nested<QualityAssurancePerformed>( n => n
		//						//	.Name( nn => nn.QualityAssurancePerformed )
		//						//	.AutoMap()
		//						//)

		//						.Nested<Connection>( n => n
		//							.Name( nn => nn.Connections )
		//							.AutoMap()
		//						)
		//						.Nested<Models.Common.IdentifierValue>( n => n
		//							.Name( nn => nn.RegionIdentifier )
		//							.AutoMap()
		//						)
		//						)
		//					)
		//				)
		//		);
		//	}
		//}
		public static void Credential_ManageIndex( bool deleteIndexFirst = false )
		{
			if ( deleteIndexFirst && EC.Indices.Exists( CredentialIndex ).Exists )
			{
				EC.Indices.Delete( CredentialIndex );
			}
			if ( !EC.Indices.Exists( CredentialIndex ).Exists )
			{
				CredentialInitializeIndex7();
			}
		}
		public static void CredentialInitializeIndex7()
		{
			if ( !EC.Indices.Exists( CredentialIndex ).Exists )
			{
				EC.Indices.Create( CredentialIndex, c => new CreateIndexDescriptor( CredentialIndex )
					  .Settings( st => st
						 .Analysis( an => an.TokenFilters( tf => tf.Stemmer( "custom_english_stemmer", ts => ts.Language( "english" ) ) )
							.Analyzers( anz => anz
								.Custom( "custom_lowercase_stemmed", cc => cc.Tokenizer( "standard" )
									.Filters( new List<string> { "lowercase", "custom_english_stemmer" } ) )
								)
							)
						 .Setting( UpdatableIndexSettings.MaxResultWindow, 100000 )  //not sure of implications?
						 )

					//Settings
					//21 - 05 - 31 mparsons - as Mappings is obsolete in e7x, changed to use Map and commented the first instance of Map
					//		backed out -only 10000 records would appear in search => still only 10000 using original search
					//.Mappings( ms => ms
					.Map<CredentialIndex>( m => m
						.AutoMap()
						.Properties( p => p
							.Text( s => s.Name( n => n.Description ).Analyzer( "custom_lowercase_stemmed" ) )
						//.Keyword( s => s.Name( n => n.Name ) ) //22-09-12 mp - not sure if this is why we are getting an error on sorting of credentials, so commented to match asmt
						//.Text( s => s.Name( n => n.Name ).Analyzer( "my_analyzer" )) .Fields( f=> f.Text (t => t.Analyzer( "english_exav" ) )
						.Nested<IndexSubject>( n => n
							.Name( nn => nn.Subjects )
							.AutoMap()
						)
						.Nested<IndexReferenceFramework>( n => n
							.Name( nn => nn.Industries )
							.AutoMap()
						)
						.Nested<IndexReferenceFramework>( n => n
							.Name( nn => nn.Occupations )
							.AutoMap()
						)
						.Nested<IndexReferenceFramework>( n => n
							.Name( nn => nn.InstructionalPrograms )
							.AutoMap()
						)
						.Nested<IndexCompetency>( n => n
							.Name( nn => nn.Competencies )
							.AutoMap()
						)
						.Nested<Models.Elastic.Address>( n => n
							.Name( nn => nn.Addresses )
							.AutoMap()
							) 
						//AgentRelationshipForEntity will replace IndexQualityAssurance
						.Nested<Models.Elastic.AgentRelationshipForEntity>( n => n
							.Name( nn => nn.AgentRelationshipsForEntity )
							.AutoMap()
							)
						//owns/offers etc ==> N/A can just use AgentRelationshipsForEntity
						//.Nested<Models.Elastic.AgentRelationshipForEntity>( n => n
						//	.Name( nn => nn.PrimaryAgentRelationshipsForEntity )
						//	.AutoMap()
						//	)
						.Nested<Models.Elastic.AgentRelationshipForEntity>( n => n
							.Name( nn => nn.OutcomeProvidersForEntity )
							.AutoMap()
						)

						.Nested<Connection>( n => n
							.Name( nn => nn.Connections )
							.AutoMap()
						)
						.Nested<Models.Common.IdentifierValue>( n => n
							.Name( nn => nn.RegionIdentifier )
							.AutoMap()
						)
						.Object<JObject>( o => o
							.Name( n => n.ResourceDetail )
							.Enabled( false )
							)
						)//Properties
					)//Map

				);
			}
		}
		#endregion

		#region Search
		/// <summary>
		/// Confirm usage
		/// 1. from widget config RelatedEntitySearch to list currently selected items
		/// TBD - in this case, it appears we should be using where credential doesn't have the widget
		/// </summary>
		/// <param name="query"></param>
		/// <param name="pTotalRows"></param>
		/// <returns></returns>
		public static List<CredentialSummary> CredentialSimpleSearch( MainSearchInput query, ref int pTotalRows )
		{
			List<CredentialSummary> list = new List<CredentialSummary>();

			if ( query.FiltersV2.Any( x => x.Name == "keywords" ) )
			{
				//20-05-21 ?? query.Keywords already has the keywords?
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "keywords" ) )
				{
					//need to handle or remove double quotes
					var text = filter.AsText();
					if ( string.IsNullOrWhiteSpace( text ) )
						continue;
					//this doesn't work:
					//temp
					query.Keywords += " " + text;
				}
			}

			//here we want to exclude a record if already tagged in widget
			//only do this if called from widget.Configure potential results
			QueryContainer widgetIdQuery = null;
			if ( query.WidgetId > 0 )
			{
				if ( query.MustHaveWidget )
				{
					widgetIdQuery = Query<CredentialIndex>.Terms( ts => ts.Field( f => f.ResourceForWidget ).Terms( query.WidgetId ) );
				}
				else if ( query.MustNotHaveWidget )
				{
					widgetIdQuery = Query<CredentialIndex>.Bool( b => b
							 .MustNot( mn => mn
								 .Terms( ts => ts.Field( f => f.ResourceForWidget ).Terms( query.WidgetId ) )
								 )
							);
				}
			}
			//this a whack requirement
			double nameBoost = 100;
			double orgNameBoost = 80;
			double descriptionBoost = 45;
			double keywordsBoost = 60;
			if ( query.CustomSearchInFields.Count() > 0 && query.CustomSearchInFields.Count() < 4 )
			{
				if ( !query.CustomSearchInFields.Contains( "Name" ) )
					nameBoost = 0;
				if ( !query.CustomSearchInFields.Contains( "OrganizationName" ) )
					orgNameBoost = 0;
				if ( !query.CustomSearchInFields.Contains( "Description" ) )
					descriptionBoost = 0;
				if ( !query.CustomSearchInFields.Contains( "Keywords" ) )
					keywordsBoost = 0;
			} else
			{

			}
			//var firstSearchResponse = client.Search<CredentialIndex>( s => s
			//	 .Query( q => !q
			//		  .Term( p => p.Name, "x" )
			//	 )
			//);
			var sort = new SortDescriptor<CredentialIndex>();

			var sortOrder = query.SortOrder;
			if ( sortOrder == "alpha" )
				sort.Ascending( s => s.Name.Suffix( "keyword" ) );
			else if ( sortOrder == "zalpha" )
				sort.Descending( s => s.Name.Suffix( "keyword" ) );
			else if ( sortOrder == "newest" )
				sort.Field( f => f.LastUpdated, SortOrder.Descending );
			else if ( sortOrder == "oldest" )
				sort.Field( f => f.LastUpdated, SortOrder.Ascending );
			else if ( sortOrder == "relevance" )
				sort.Descending( SortSpecialField.Score );
			else
				sort.Ascending( s => s.Name );
			if ( query.StartPage < 1 )
				query.StartPage = 1;
			var search = EC.Search<CredentialIndex>( i => i.Index( CredentialIndex )
				.Query( q =>
					widgetIdQuery &&
					 q.MultiMatch( m => m
									 .Fields( f => f
										  .Field( ff => ff.Name, nameBoost )
										  .Field( ff => ff.PrimaryOrganizationName, orgNameBoost )
										  .Field( ff => ff.SubjectWebpage, 60 )
										  .Field( ff => ff.Description, descriptionBoost )
										  .Field( ff => ff.Keyword, keywordsBoost )
										//.Field( ff => ff.SubjectAreas, 60 )
										)
						 .Type( TextQueryType.PhrasePrefix )
						 .Query( query.Keywords )
						 .MaxExpansions( 10 )
						 )
					 )
					 .TrackTotalHits( true )
					 .Sort( s => sort )
					 //.From( query.StartPage - 1 )
					 .From( query.StartPage > 0 ? query.StartPage - 1 : 1 )
					 .Skip( ( query.StartPage - 1 ) * query.PageSize )
					 .Size( query.PageSize ) );

			var debug = search.DebugInformation;
			pTotalRows = ( int ) search.Total;
			if ( query.StartPage == 1 && ( query.Filters.Count > 0 || query.FiltersV2.Count > 0 || !string.IsNullOrWhiteSpace( query.Keywords ) ) )
			{
				//actually extract the request: between #request and #response
				var elasticQuery = UtilityManager.ExtractNameValue( debug, "# Request", ":", "# Response:" );
				LoggingHelper.DoTrace( 6, string.Format( "ElasticServices.Search: {0}, Results: {1}, ElasticLog: \r\n", "Credential SIMPLE", pTotalRows ) + elasticQuery, "ElasticQuery" );
			}
			if ( pTotalRows > 0 )
			{
				//map results
				list = ElasticManager.Credential_MapFromElastic( ( List<CredentialIndex> ) search.Documents, query.StartPage, query.PageSize );

				LoggingHelper.DoTrace( 6, string.Format( "ElasticServices.CredentialSearch. found: {0} records", pTotalRows ) );
			}
			//stats
			query.Results = pTotalRows;
			return list;
		}


		/// <summary>
		/// Credential auto complete
		/// This must be updated to handle use with a widget, or should the simple version be used?
		/// </summary>
		/// <param name="keyword"></param>
		/// <param name="maxTerms"></param>
		/// <param name="pTotalRows"></param>
		/// <returns></returns>
		public List<string> CredentialAutoComplete( MainSearchInput query, int maxTerms, ref int pTotalRows )
		{
			if ( UtilityManager.GetAppKeyValue( "includingAllFiltersWithAutocomplete", false ) )
			{
				List<string> autocompleteList = new List<string>();

				//test just calling the related main search and returning the terms
				Credential_Search( query, ref pTotalRows, ref autocompleteList, true );
				return autocompleteList;
			}
			var keywords = query.Keywords;
			QueryContainer widgetIdQuery = null;
			QueryContainer commonOrgRolesBYQuery = null;
			QueryContainer publishedBYQuery = null;
			QueryContainer credentialStatusTypeQuery = null;

			QueryContainer entityStateQuery = null;
			//bool hasNewPotentialResults = false;
			//if ( query.WidgetId > 0 && query.HasCredentialPotentialResults )
			//{
			//	hasNewPotentialResults = true;
			//	widgetIdQuery = Query<CredentialIndex>.Terms( ts => ts.Field( f => f.ResourceForWidget ).Terms( query.WidgetId ) );
			//}
			//
			List<int> targetOrgIds = new List<int>();
			var relationshipTypeIds = GetAnyRelationships( query, ref targetOrgIds );
			if ( relationshipTypeIds?.Count > 0 )
			{
				//seems poorly names
				commonOrgRolesBYQuery = CommonOrgRolesByFilter<CredentialIndex>( query );
			}
			publishedBYQuery = PublishedByFilter<CredentialIndex>( query );

			//20-04-16 mparsons - set a default value for credentialStatusTypeQuery to exclude deprecated. Will be overridden if any credential status are provided
			if ( UtilityManager.GetAppKeyValue( "hidingDeprecatedStatus", false ) )
			{
				var defStatus = CodesManager.Property_GetValues( CodesManager.PROPERTY_CATEGORY_CREDENTIAL_STATUS_TYPE, "", false );
				var exists = defStatus.Where( s => s.Title.Contains( "Deprecated" ) == false ).ToList();
				if ( exists != null && exists.Count > 0 )
				{
					//var defaultStatusTypeIds = defStatus.Where( s => s.Title.Contains( "Ceased" ) == false ).Select( s => s.Id ).ToList();
					var defaultStatusTypeIds = exists.Select( s => s.Id ).ToList();
					//var defCredentialStatusTypeIds = defStatus.Where( s => s.Title.Contains( "Deprecated" ) == false ).Select( s => s.Id ).ToList();
					credentialStatusTypeQuery = Query<CredentialIndex>.Terms( ts => ts.Field( f => f.CredentialStatusId ).Terms( defaultStatusTypeIds.ToArray() ) );
				}
			}

			//boost references lower
			//or maybe always downgrade?
			if ( UtilityManager.GetAppKeyValue( "usingEntityStateIdBoosting", false ) )
			{
				entityStateQuery = Query<CredentialIndex>
						.Boosting( bq => bq
							.Positive( pq => pq
								.Terms( ts => ts
									.Field( f => f.EntityStateId ).Terms<int>( 3 ) )
								)
							.Negative( nq => nq
								.Terms( ts => ts
									.Field( f => f.EntityStateId ).Terms<int>( 2 ) )
								)
							.NegativeBoost( negativeBooster )
							);
			}
			//Phrase Prefix is looking for matches to what a user types in (i.e., the phrase)"
			QueryContainer multiMatchPhrasePrefixQuery = Query<CredentialIndex>.MultiMatch( m => m
				.Fields( f => f
					.Field( ff => ff.NameOrganizationKey, 100 )      //??
					.Field( ff => ff.PrimaryOrganizationName, 90 )
				)
				.Type( TextQueryType.PhrasePrefix )
				.Query( keywords )
			//.MaxExpansions( 10 )
			//.Analyzer( "standard" )
			);
			QueryContainer queryStringQuery = null;
			//get more than the max terms to allow returning max after getting distinct list
			//var search = EC.Search<CredentialIndex>( i => i.Index( CredentialIndex )
			//	.Query( q =>
			//		 q.MultiMatch( m => m
			//						 .Fields( f => f
			//						  .Field( ff => ff.NameOrganizationKey, 100 )
			//						  .Field( ff => ff.OwnerOrganizationName, 80 )
			//		 )
			//		.Type( TextQueryType.PhrasePrefix )
			//		.Query( keyword )
			//		.MaxExpansions( 10 ) ) 
			//	 )
			//	.Size( maxTerms * 2 ) );
			//pTotalRows = ( int )search.Total;

			//pTotalRows == 0 && 
			if ( keywords.IndexOf( " " ) == -1 && keywords.IndexOf( "*" ) == -1 )
			{
				keywords = "*" + keywords; //prefix and suffix?, or just prefix
				queryStringQuery = Query<CredentialIndex>.QueryString( m => m
									.Fields( f => f
										.Field( ff => ff.NameOrganizationKey, 90 )
										.Field( ff => ff.PrimaryOrganizationName, 25 )
									 )
									.Query( keywords )
									.Analyzer( "standard" )
									.QuoteAnalyzer( "keyword" ) //??
									.AllowLeadingWildcard()
									.FuzzyPrefixLength( 0 ) //Number of beginning characters left unchanged for fuzzy matching. Defaults to 0
									.FuzzyMaxExpansions( 20 )
									.FuzzyRewrite( MultiTermQueryRewrite.ConstantScore )
									.Rewrite( MultiTermQueryRewrite.ConstantScore )
									.Fuzziness( Fuzziness.Auto )
									.TieBreaker( .5 )       //got error with 1.2, maybe should be between 0 and 1?
															//.MinimumShouldMatch( 3 )
									.DefaultOperator( Operator.Or )
									.Lenient()
									.AnalyzeWildcard( true ) //???? - may only be pertinent if asterisk included 
									.AutoGenerateSynonymsPhraseQuery( false ) //??
								 );
			}
			var search = EC.Search<CredentialIndex>( body => body
			 .Index( CredentialIndex )
				 .Query( q =>
					( multiMatchPhrasePrefixQuery || queryStringQuery )
					&& entityStateQuery
					&& commonOrgRolesBYQuery && publishedBYQuery
					&& credentialStatusTypeQuery
				 )
				.TrackTotalHits( true )
				.Size( maxTerms * 2 )
			);
			//21-08-11 mp - search.Total is the total that would be returned. search.Documents is the actual to be returned 
			pTotalRows = ( int ) search.Total;

			var list = ( List<CredentialIndex> ) search.Documents;
			return list.Select( m => m.ListTitle ).Distinct().Take( maxTerms ).ToList();
			//var output = list.Select( m => new FilterItem() { Id = m.Id, Label = m.ListTitle, Text = m.ListTitle } ).Distinct().Take(maxTerms).ToList();
			//return list.Select( m => m.ListTitle ).Distinct().ToList();
			//return output;
		}

        public List<string> CredentialGet( MainSearchInput query, int maxTerms, ref int pTotalRows )
        {
            bool hasNewPotentialResults = false;
            QueryContainer widgetIdQuery = null;
            QueryContainer credentialStatusTypeQuery = null;
            QueryContainer credentialIdQuery = null;
            QueryContainer entityStateQuery = null;
            //
            var credIds = new List<int>();
            foreach ( var filter in query.FiltersV2.Where( m => m.Name == "potentialresults" ).ToList() )
            {
                credIds.AddRange( JsonConvert.DeserializeObject<List<int>>( filter.CustomJSON ) );
            }

            if ( credIds.Any() )
            {
                if ( !hasNewPotentialResults )
                {
                    LoggingHelper.DoTrace( 6, string.Format( "ElasticServices.Credential_Search - Found Credential Ids list. Count: {0}", credIds.Count() ) );
                    credIds.ForEach( x =>
                    {
                        credentialIdQuery |= Query<CredentialIndex>.Terms( ts => ts.Field( f => f.Id ).Terms( x ) );
                    } );
                }
                else
                {
                    LoggingHelper.DoTrace( 6, string.Format( "ElasticServices.Credential_Search - Found Credential Ids list. Count: {0}. BUT SKIPPING USE AS there is are query.HasCredentialPotentialResults", credIds.Count() ) );
                }
            }

            var search = EC.Search<CredentialIndex>( body => body
             .Index( CredentialIndex )
                 .Query( q =>
                    ( credentialIdQuery )
                    && entityStateQuery
                    && credentialStatusTypeQuery
                 )
                .TrackTotalHits( true )
                .Size( maxTerms * 2 )
            );
            //21-08-11 mp - search.Total is the total that would be returned. search.Documents is the actual to be returned 
            pTotalRows = ( int ) search.Total;

            var list = ( List<CredentialIndex> ) search.Documents;
            return list.Select( m => m.ListTitle ).Distinct().Take( maxTerms ).ToList();
            //var output = list.Select( m => new FilterItem() { Id = m.Id, Label = m.ListTitle, Text = m.ListTitle } ).Distinct().Take(maxTerms).ToList();
            //return list.Select( m => m.ListTitle ).Distinct().ToList();
            //return output;
        }


        /// <summary>
        /// from main search autocomplete just need the relationships.
        /// from occupations autocomplete, probably just need the orgId? Or only orgIds with a rel of 30
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static List<int> GetAnyRelationships( MainSearchInput query, ref List<int> targetOrgIds )
		{
			List<int> relationshipTypeIds = new List<int>();
			//targetOrgIds = new List<int>();
			//NOTE the same method in elastic searches uses CODE!!!!
			foreach ( var filter in query.FiltersV2.Where( m => m.Type == MainSearchFilterV2Types.CUSTOM ).ToList() )
			{
				var item = filter.AsOrgRolesItem();
				//no category. set to 
				if ( item.CategoryId < 1 )
					item.CategoryId = CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE;
				if ( item == null || item.CategoryId < 1 )
					continue;

				if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE )
				{
					if ( filter.Name == "organizationroles" )
					{
						//item.Id is the orgId
						relationshipTypeIds.AddRange( item.IdsList );
						targetOrgIds.Add( item.Id );
					}
				}
			}

			return relationshipTypeIds;
		}
		//public static List<object> CredentialAutoCompleteOLD( string keyword, int maxTerms, ref int pTotalRows )
		//{
		//	//get more than the max terms to allow returning max after getting distinct list
		//	var search = EC.Search<CredentialIndex>( i => i.Index( CredentialIndex ).Query( q =>
		//		 q.MultiMatch( m => m
		//						 .Fields( f => f
		//						  .Field( ff => ff.NameOrganizationKey, 100 )
		//						  .Field( ff => ff.OwnerOrganizationName, 80 )
		//		 )
		//		.Type( TextQueryType.PhrasePrefix )
		//		.Query( keyword )
		//		.MaxExpansions( 10 ) ) )
		//		.Size( maxTerms * 2 ) );
		//	//21-08-11 mp - search.Total is the total that would be returned. search.Documents is the actual to be returned 

		//	pTotalRows = ( int ) search.Total;

		//	var list = ( List<CredentialIndex> ) search.Documents;
		//	return list.Select( x => x.ListTitle ).Distinct().Take( maxTerms ).ToList().Cast<object>().ToList();

		//}
		public static void CustomAnalyzer( string keyword )
		{
			var indexSettings = new IndexSettings();

			var customAnalyzer = new CustomAnalyzer();
			customAnalyzer.Tokenizer = "keyword";
			customAnalyzer.Filter = new List<string>();
			//customAnalyzer.Filter.Add( "lowercase" );
			//indexSettings.Analysis.Analyzers.Add( "custom_lowercase_analyzer", customAnalyzer );

			//var analyzerRes = EC.Indices.Create( CredentialIndex, ci => ci
			//	.Index( "my_third_index" )
			//	.InitializeUsing( indexSettings )
			//	.AddMapping( m => m.MapFromAttributes() )
			//	.AddMapping( m => m.MapFromAttributes() ) );

			//Console.WriteLine( analyzerRes.RequestInformation.Success );
		}

		public static ICollection<string> SelectAllNamesDistinct<T>( string keyword ) where T : class, IIndex
		{
			var searchResponse =
				EC.Search<T>( s => s
					 .Size( 0 )
					 .Aggregations( agg => agg
						  .Terms( "name", t => t
							   .Field( "name.keyword" )

						  )
					 )
				);

			var aggregation = searchResponse.Aggregations.Values;
			var listOfNames = new List<string>();

			if ( searchResponse.Aggregations.Values.FirstOrDefault().GetType() == typeof( BucketAggregate ) )
			{
				foreach ( IBucket bucket in ( ( BucketAggregate ) aggregation.FirstOrDefault() ).Items )
				{
					if ( bucket.GetType() == typeof( KeyedBucket<object> ) )
					{
						var valueKey = ( ( KeyedBucket<object> ) bucket ).Key;
						listOfNames.Add( valueKey.ToString() );
					}
				}
			}

			return listOfNames.OrderBy( c => c ).ToList();
		}

		/// <summary>
		/// Proxy for full search
		/// </summary>
		/// <param name="query"></param>
		/// <param name="pTotalRows"></param>
		/// <returns></returns>
		public List<CredentialSummary> Credential_Search( MainSearchInput query, ref int pTotalRows )
		{
			List<string> autocompleteList = new List<string>();
			bool requestIsAutocomplete = false;
			return Credential_Search( query, ref pTotalRows, ref autocompleteList, requestIsAutocomplete );
		}
		public List<CredentialSummary> Credential_Search( MainSearchInput query, ref int pTotalRows, ref List<string> autocompleteList, bool requestIsAutocomplete = false )
		{
			//
			if ( UtilityManager.GetAppKeyValue( "usingCredentialPrototypingSearch", false ) )
			{
				return Credential_SearchPROTOTYPING( query, ref pTotalRows );
			}
			LoggingHelper.DoTrace( 7, "ElasticServices.Credential_Search - entered" );
			List<CredentialSummary> list = new List<CredentialSummary>();
			Credential_BuildIndex();

			QueryContainer collectionIdQuery = null;
			QueryContainer collectionsQuery = null;
			QueryContainer credentialTypeQuery = null;
			QueryContainer credentialIdQuery = null;
			QueryContainer widgetIdQuery = null;
			QueryContainer credentialStatusTypeQuery = null;
			QueryContainer audienceLevelTypeQuery = null;
			QueryContainer audienceTypeQuery = null;
			QueryContainer competenciesQuery = null;
			//QueryContainer relationshipIdQuery = null;
			//probably will have same problem with ORs with subjects entered in widget, and in search box
			QueryContainer subjectsQuery = null;

			QueryContainer widgetKeywordsQuery = null;//may want to use this for keyword entered in widget??
			QueryContainer wLWIAQuery = null;
			QueryContainer connectionsQuery = null;
			QueryContainer occupationsQuery = null;
			QueryContainer industriesQuery = null;
			QueryContainer boundariesQuery = null;
			QueryContainer commonOrgRolesBYQuery = null;
			QueryContainer commonLeftRailOrgRolesBYQuery = null; 
			QueryContainer publishedBYQuery = null;

			QueryContainer qaFilterQuery = null;

			QueryContainer languagesQuery = null;
			QueryContainer reportsQuery = null;
			QueryContainer classificationsQuery = null;
			QueryContainer asmntDeliveryTypesQuery = null;
			QueryContainer learningDeliveryTypesQuery = null;
			QueryContainer widgetOccupationsQuery = null;
			QueryContainer windustriesQuery = null;
			QueryContainer wsubjectsQuery = null;
			HistoryQueryFilters history = new HistoryQueryFilters();
			QueryContainer ctidQuery = null;
			//set a default, and use when needed
			QueryContainer entityStateQuery = null;// Query<CredentialIndex>.Terms( ts => ts.Field( f => f.EntityStateId ).Terms<int>( 3 ) );

			//QueryContainer createdFromQuery = null;
			//QueryContainer createdToQuery = null;
			//QueryContainer historyFromQuery = null;
			//QueryContainer historyToQuery = null;
			LocationQueryFilters locationQueryFilters = new LocationQueryFilters();
			var qaRelationshipTypeIds = new List<int>();
			// this is used to collection custom resource relationships ( ex 6,7 or 1,2,10,etc)
			var relationshipTypeIds = new List<int>();
			var credentialStatusTypeIds = new List<int>();
			//custom orgIds for searching 
			var orgFilterIds = new List<int>();
			//20-04-16 mparsons - set a default value for credentialStatusTypeQuery to exclude deprecated. Will be overridden if any credential status are provided
			if ( UtilityManager.GetAppKeyValue( "hidingDeprecatedStatus", false ) )
			{
				var defStatus = CodesManager.Property_GetValues( CodesManager.PROPERTY_CATEGORY_CREDENTIAL_STATUS_TYPE, "", false );
				var exists = defStatus.Where( s => s.Title.Contains( "Deprecated" ) == false ).ToList();
				if ( exists != null && exists.Count > 0 )
				{
					//var defaultStatusTypeIds = defStatus.Where( s => s.Title.Contains( "Ceased" ) == false ).Select( s => s.Id ).ToList();
					var defaultStatusTypeIds = exists.Select( s => s.Id ).ToList();
					//var defCredentialStatusTypeIds = defStatus.Where( s => s.Title.Contains( "Deprecated" ) == false ).Select( s => s.Id ).ToList();
					credentialStatusTypeQuery = Query<CredentialIndex>.Terms( ts => ts.Field( f => f.CredentialStatusId ).Terms( defaultStatusTypeIds.ToArray() ) );
				}
			}
			try
			{
				if ( UtilityManager.GetAppKeyValue( "usingEntityStateIdBoosting", false ) )
				{

					if ( query.HasFilters )
					{
						//TODO: filter to watch for "Is A nnn Reference" (and maybe "Is a Registered nnn")
						if ( query.FiltersV2.Any( filter => filter.Name == "reports" ) )
						{
							//skip for now
						}
						else
						{
							//new - only show  top level objects if no keywords (what others to consider)
							if ( query.HasKeywords )
							{
								//entityStateQuery = null;
								//or see how to use boosting
								//back to a TODO - skipping for now
								/*
								entityStateQuery = Query<CredentialIndex>
									.Boosting( bq => bq
										.Positive( pq => pq
											.Terms( ts => ts
												.Field( f => f.EntityStateId ).Terms<int>( 3 ) )
											)
										.Negative( nq => nq
											.Terms( ts => ts
												.Field( f => f.EntityStateId ).Terms<int>( 2 ) )
											)
										.NegativeBoost( negativeBooster ) //what if skipped?
										);
								*/
							}
							else
							{
								entityStateQuery = Query<CredentialIndex>.Terms( ts => ts.Field( f => f.EntityStateId ).Terms<int>( 3 ) );
							}
						}
					}
					else
					{
						entityStateQuery = Query<CredentialIndex>.Terms( ts => ts.Field( f => f.EntityStateId ).Terms<int>( 3 ) );
					}

				}
				#region cred properties/Categories
				//20-09-09 mparsons - in preparation of implementation of this filter, make it mutually exclusive with the current filter
				bool hasNewPotentialResults = false;
				//quick hack for miLEAP
				if ( ( query.WidgetId > 0 && query.HasCredentialPotentialResults )
					|| ( query.WidgetId == 68 || query.WidgetId == 70 ) )
				{
					hasNewPotentialResults = true;
					widgetIdQuery = Query<CredentialIndex>.Terms( ts => ts.Field( f => f.ResourceForWidget ).Terms( query.WidgetId ) );
				}

				//21-10-26 WATCH - Has Any Subjects might be missed if selected from the Subject Areas filters, vs Other Filters
				if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CODE ) )
				{
					//2,4,14,18,21,39,
					//string searchCategories = UtilityManager.GetAppKeyValue( "credSearchCategories", "" );
					//var credSearchCategories = new List<int>();
					//foreach ( var s in searchCategories.Split( ',' ) )
					//	if ( !string.IsNullOrEmpty( s ) )
					//		credSearchCategories.Add( int.Parse( s ) );

					//var credentialIds = new List<int>();
					var credentialTypeIds = new List<int>();

					var audienceLevelTypeIds = new List<int>();
					var audienceTypeIds = new List<int>();

					var validConnections = new List<string>();
					var connectionFilters = new List<string>();
					var asmntdeliveryTypeIds = new List<int>();
					var learningdeliveryTypeIds = new List<int>();
					var reportIds = new List<int>();
					//var resourceForWidget = new List<int>();

					if ( query.FiltersV2.Any( x => x.AsCodeItem().CategoryId == CodesManager.PROPERTY_CATEGORY_CONNECTION_PROFILE_TYPE ) )
					{
						//LoggingHelper.DoTrace( 7, "Credential search, filters exist for connections" );
						//this will include is part/has part
						//Enumeration entity = CodesManager.GetCredentialsConditionProfileTypes();
						Enumeration entity = CodesManager.GetConnectionTypes( 1, false );
						validConnections = entity.Items.Select( s => s.SchemaName.ToLower() ).ToList();
					}
					//TODO watch for relationshipIds being passed for two filters
					foreach ( var filter in query.FiltersV2.Where( m => m.Type == MainSearchFilterV2Types.CODE ).ToList() )
					{
						var item = filter.AsCodeItem();
						if ( filter.Name == "reports" || filter.Name == "otherfilters" || filter.Name == "filter:OtherFilters" )
						{
							reportIds.Add( item.Id ); //can probably continue after here?
							continue;
						} else if ( filter.Name == "" && filter.Values != null && filter.Values.Count == 3 )
						{
							//HACK test while determine why no filter.name
							//var filterItem = filter.AsDefaultCodeItem();
							if ( item.CategoryId == 1 || item.CategoryId == 58 )
							{
								reportIds.Add( item.Id ); //can probably continue after here?
								continue;
							}
						}
						//else if ( item.Name == "" && filter.Values != null && item.SchemaName.IndexOf("credReport") ==0 )
						//{
						//	//we have a report from the non-Other filters section Do a lookup
						//	var propertyId = 0;
						//	if ( ElasticManager.GetPropertyId( 58, item.SchemaName, ref propertyId ) )
						//	{
						//		reportIds.Add( propertyId );
						//		continue;
						//	}
						//}

						if ( filter.Name == "history" )
						{
							continue;
						}
						//if ( filter.Name == "widget" )
						//{
						//	resourceForWidget.Add( item.Id );
						//	continue;
						//}
						if ( item == null || item.CategoryId < 1 )
						{
							//check for text only 
							var text = filter.AsText();
							if ( !string.IsNullOrWhiteSpace( text ) )
							{
								//what? if no keywords, then append. Or append?
								if ( string.IsNullOrWhiteSpace( query.Keywords ) )
									query.Keywords = text;
								else
									query.Keywords += " " + text;
							}
							continue;
						}

						//if ( credSearchCategories.Contains( item.CategoryId ) )
						//{ }
						if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_CREDENTIAL_TYPE )
							credentialTypeIds.Add( item.Id );
						else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_CREDENTIAL_STATUS_TYPE )
							credentialStatusTypeIds.Add( item.Id );
						else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL )
							audienceLevelTypeIds.Add( item.Id );
						else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE )
							audienceTypeIds.Add( item.Id );
						else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_ASMT_DELIVERY_TYPE )
							asmntdeliveryTypeIds.Add( item.Id );
						else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE )
							learningdeliveryTypeIds.Add( item.Id );
						else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE )
						{
							if ( filter.Name == "qualityassurance" || filter.Name == "filter:QAReceived" )
							{
								//24-02-20 mp - the orgId has been added to relationships? Need a better way to distinguish this? Obviously a problem with an org id under 15
								if ( item.Id < 15)
									qaRelationshipTypeIds.Add( item.Id );
							}
							else
							{
								//confirm if these are the custom rel types from a detail page link
								if ( item.Id < 35 )
									relationshipTypeIds.Add( item.Id );
							}
						}
						else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_ORG_OWNS_OFFERS )
						{
							//should not need the name check?
							if ( filter.Name == "filter:organizationnonqaroles" )
							{
								//what name do we have? Not needed. The common method will do this
								//orgFilterIds.Add( item.Id );
							}
						}
						else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_CONNECTION_PROFILE_TYPE )
						{
							if ( validConnections.Contains( item.SchemaName.ToLower() ) )
								connectionFilters.Add( item.SchemaName.Replace( "ceterms:", "" ) );
						}
					}

					CommonHistoryFilter<CredentialIndex>( query, ref history );

					if ( credentialTypeIds.Any() )
					{
						//21-05-13 mparsons - check if a badge type - actually only check if from new interface.
						//					- probably better to check prior to here (in translation)
						credentialTypeQuery = Query<CredentialIndex>.Terms( ts => ts.Field( f => f.CredentialTypeId ).Terms( credentialTypeIds.ToArray() ) );
					}

					if ( credentialStatusTypeIds.Any() )
						credentialStatusTypeQuery = Query<CredentialIndex>.Terms( ts => ts.Field( f => f.CredentialStatusId ).Terms( credentialStatusTypeIds.ToArray() ) );

					if ( audienceLevelTypeIds.Any() )
						audienceLevelTypeQuery = Query<CredentialIndex>.Terms( ts => ts.Field( f => f.AudienceLevelTypeIds ).Terms<int>( audienceLevelTypeIds ) );

					if ( audienceTypeIds.Any() )
						audienceTypeQuery = Query<CredentialIndex>.Terms( ts => ts.Field( f => f.AudienceTypeIds ).Terms<int>( audienceTypeIds ) );

					if ( relationshipTypeIds.Any() )
					{
						//no longer an action here, done in common method: CommonQualityAssuranceFilter
					}
					if ( asmntdeliveryTypeIds.Any() )
						asmntDeliveryTypesQuery = Query<CredentialIndex>.Terms( ts => ts.Field( f => f.AsmntDeliveryMethodTypeIds ).Terms<int>( asmntdeliveryTypeIds ) );

					if ( learningdeliveryTypeIds.Any() )
						learningDeliveryTypesQuery = Query<CredentialIndex>.Terms( ts => ts.Field( f => f.LearningDeliveryMethodTypeIds ).Terms<int>( learningdeliveryTypeIds ) );

					if ( reportIds.Any() )
					{
						reportsQuery = Query<CredentialIndex>.Terms( ts => ts
									.Field( f => f
										.ReportFilters )
											.Terms<int>( reportIds.ToArray() ) );
					}

					if ( connectionFilters.Any() )
					{
						connectionFilters.ForEach( x =>
						{
							//LoggingHelper.DoTrace( 6, "Credential search, checking for connections. x=" + x );
							if ( x == "requires" )
								connectionsQuery |= Query<CredentialIndex>.Range( r => r.Field( f => f.RequiresCount ).GreaterThan( 0 ) );
							if ( x == "recommends" )
								connectionsQuery |= Query<CredentialIndex>.Range( r => r.Field( f => f.RecommendsCount ).GreaterThan( 0 ) );
							if ( x == "isRequiredFor" )
								connectionsQuery |= Query<CredentialIndex>.Range( r => r.Field( f => f.RequiredForCount ).GreaterThan( 0 ) );
							if ( x == "isRecommendedFor" )
								connectionsQuery |= Query<CredentialIndex>.Range( r => r.Field( f => f.IsRecommendedForCount ).GreaterThan( 0 ) );
							if ( x == "isAdvancedStandingFor" )
								connectionsQuery |= Query<CredentialIndex>.Range( r => r.Field( f => f.IsAdvancedStandingForCount ).GreaterThan( 0 ) );
							if ( x == "advancedStandingFrom" )
								connectionsQuery |= Query<CredentialIndex>.Range( r => r.Field( f => f.AdvancedStandingFromCount ).GreaterThan( 0 ) );
							if ( x == "isPreparationFor" )
								connectionsQuery |= Query<CredentialIndex>.Range( r => r.Field( f => f.PreparationForCount ).GreaterThan( 0 ) );
							if ( x == "isPreparationFrom" )
								connectionsQuery |= Query<CredentialIndex>.Range( r => r.Field( f => f.PreparationFromCount ).GreaterThan( 0 ) );
							if ( x == "isPartOf" )
								connectionsQuery |= Query<CredentialIndex>.Range( r => r.Field( f => f.IsPartOfCount ).GreaterThan( 0 ) );
							if ( x == "hasPart" )
								connectionsQuery |= Query<CredentialIndex>.Range( r => r.Field( f => f.HasPartCount ).GreaterThan( 0 ) );
							if ( x == "entryCondition" )
								connectionsQuery |= Query<CredentialIndex>.Range( r => r.Field( f => f.EntryConditionCount ).GreaterThan( 0 ) );
						} );

						//LoggingHelper.DoTrace( 6, "Credential search, AFTER checking for connections" );
					}
				}
				#endregion
				#region Widget. Credential Ids list
				if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CUSTOM ) )
				{
					var exists = query.FiltersV2.Where( m => m.Name == "potentialresults" ).ToList();
					if ( exists != null && exists.Any() )
					{
						var credIds = new List<int>();
						foreach ( var filter in query.FiltersV2.Where( m => m.Name == "potentialresults" ).ToList() )
						{
							credIds.AddRange( JsonConvert.DeserializeObject<List<int>>( filter.CustomJSON ) );
						}

						if ( credIds.Any() )
						{
							if ( !hasNewPotentialResults )
							{
								LoggingHelper.DoTrace( 6, string.Format( "ElasticServices.Credential_Search - Found Credential Ids list. Count: {0}", credIds.Count() ) );
								credIds.ForEach( x =>
								{
									credentialIdQuery |= Query<CredentialIndex>.Terms( ts => ts.Field( f => f.Id ).Terms( x ) );
								} );
							}
							else
							{
								LoggingHelper.DoTrace( 6, string.Format( "ElasticServices.Credential_Search - Found Credential Ids list. Count: {0}. BUT SKIPPING USE AS there is are query.HasCredentialPotentialResults", credIds.Count() ) );
							}
						}
					}
					//prototype - additional credentials (in addition to other filters) 
					//	not simple, as a single query object would have to be created that included doing an OR with this option
					exists = query.FiltersV2.Where( m => m.Name == "additionalresults" ).ToList();
					if ( exists != null && exists.Any() )
					{
					}
				}

				#endregion

				#region collections
				//if ( query.CollectionId > 0 )       //???
				//{
				//	collectionIdQuery = Query<CredentialIndex>.Terms( ts => ts.Field( f => f.ResourceForCollection ).Terms( query.CollectionId ) );
				//}
				if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CUSTOM ) )
				{
					var idsList = new List<int>();
					foreach ( var filter in query.FiltersV2.Where( m => m.Name == "collectionresults" ).ToList() )
					{
						//filter.CustomJSON is populated in workIT.Services.API.SearchServicesTranslateMainQueryFiltersToMainSearchInputFilters
						//	TBD if should use something different
						idsList.AddRange( JsonConvert.DeserializeObject<List<int>>( filter.CustomJSON ) );
					}

					if ( idsList.Any() )
					{
						collectionIdQuery = Query<CredentialIndex>.Terms( ts => ts.Field( f => f.ResourceForCollection ).Terms<int>( idsList.ToArray() ) );
					}
				}
				collectionsQuery = CommonInCollectionByIdFilters<CredentialIndex>( query );
				if ( collectionsQuery == null )
					collectionsQuery = CommonPartOfCollectionFilters<CredentialIndex>( query );
				#endregion
				#region Handle Location queries
				bool usingRegionHack = false;
				//int regionBoost = 10;
				LocationFilter<CredentialIndex>( query, locationQueryFilters, ref usingRegionHack );
				//custom filter for Illinois LWIAs
				if ( query.FiltersV2.Count > 0 )
				{
					wLWIAQuery = LocationSubregionFilter<CredentialIndex>( query );
				}
				#endregion

				#region QualityAssurance, with owned and offered by
				//****document which is used by the detail page searches for owns/offers

				//NOTE: this is only referenced after clicking on a gray box or on detail, not from the search page
				//		==> actually now used by search widget => type = organizationroles
				//20-11-20 - the QA performed search from org detail page uses this. Updated. Note that the QA target will need to be reindexed
				//21-06-01	ALSO the owns/offers searches from the detail pages
				//23-07-21 we had a conflict where the widget over writes the organizationroles filter from an org detail page. TBD will we get two filters, or do we need to create two methods
				//23-08-14 To address the latter, we had changed the query to use ANDs. BUT, this is an issue with multiple owners in a widget. 

				commonOrgRolesBYQuery = CommonOrgRolesByFilter<CredentialIndex>( query );
				//
				commonLeftRailOrgRolesBYQuery = CommonNonQAOrgRolesLeftRailFilter<CredentialIndex>( query, new List<int>() { 6, 7 } );
				//
				publishedBYQuery = PublishedByFilter<CredentialIndex>( query );

				//USED BY QUALITY ASSURANCE FILTER check boxes and org list
				//21-03-29 mp - while adding organization roles filter, noted that this method may produce a filter using relationshipTypeIds. However, will this work with an org filter????
				//need to ensure relationshipTypeIds are not applied to the wrong filter type
				qaFilterQuery = CommonQualityAssuranceFilter<CredentialIndex>( query, qaRelationshipTypeIds );

				//24-02-19 mp - relationshipTypeIds should not be QA roles!!
				var commonNonQAOrgRolesQuery = CommonNonQAOrgRolesFilter<CredentialIndex>( query, relationshipTypeIds );
				//
				var commonOutcomeProvidersQuery = CommonOutcomeProviders<CredentialIndex>( query );


				#endregion

				if ( BaseFactory.IsValidCtid( query.Keywords ) )
				{
					ctidQuery = Query<CredentialIndex>.Match( ts => ts.Field( f => f.CTID ).Query( query.Keywords ) );
				}

				#region Competencies
				competenciesQuery = CommonCompetencies<CredentialIndex>( query );


				#endregion


				#region Occupations, Industries, Programs

				occupationsQuery = CommonOccupations<CredentialIndex>( query, ref widgetOccupationsQuery );

				industriesQuery = CommonIndustries<CredentialIndex>( query, ref windustriesQuery );

				classificationsQuery = CommonCip<CredentialIndex>( query );

				#endregion

				#region Subjects

				if ( query.FiltersV2.Any( x => x.Name == "subjects" ) )
				{
					//LoggingHelper.DoTrace( 6, "ElasticServices.Credential_Search - subjects" );
					subjectsQuery = HandleSubjects<CredentialIndex>( query );
				}

				//keywords from widget 
				if ( query.FiltersV2.Any( x => x.Name == "keywords" ) )
				{
					//LoggingHelper.DoTrace( 6, "ElasticServices.Credential_Search - keywords" );
					widgetKeywordsQuery = HandleWidgetKeywords<CredentialIndex>( query );
				}
				#endregion

				#region Languages
				var languageFilters = new List<string>();
				if ( query.FiltersV2.Any( x => x.Name == "languages" ) )
				{
					foreach ( var filter in query.FiltersV2.Where( m => m.Name == "languages" ) )
					{
						var text = filter.GetValueOrDefault( "CodeText", "" );
						if ( string.IsNullOrWhiteSpace( text ) )
							continue;
						languageFilters.Add( text.ToLower() );
					}

					//QueryContainer qc = null;
					languageFilters.ForEach( x =>
					{
						languagesQuery |= Query<CredentialIndex>.Terms( ts => ts.Field( f => f.InLanguage ).Terms( x ) );
						//qc |= Query<CredentialIndex>.Terms( ts => ts.Field( f => f.InLanguage ).Terms( languages ) );
					} );
					//languagesQuery = Query<CredentialIndex>.Nested( n => n.Path( p => p.InLanguage ).Query( q => qc ).IgnoreUnmapped() );
				}
				#endregion

				#region Boundaries

				boundariesQuery = HandleLocationQuery<CredentialIndex>( query );

				#endregion

				#region Query
				LoggingHelper.DoTrace( 7, "ElasticServices.Credential_Search - starting query" );
				//var tag = string.Format( "*{0}*", query.Keywords.ToLower() );
				//var search = EC.Search<CredentialIndex>( i => i.Index( CredentialIndex ).Query( q => q.Wildcard( w => w.Field( f => f.Name ).Value( query ) ) || q.Wildcard( w => w.Field( f => f.OoName ).Value( query ) ) ) );
				var sort = new SortDescriptor<CredentialIndex>();

				var sortOrder = query.SortOrder;
				if ( sortOrder == "alpha" )
					sort.Ascending( s => s.Name.Suffix( "keyword" ) );
				else if ( sortOrder == "zalpha" )
					sort.Descending( s => s.Name.Suffix( "keyword" ) );
				else if ( sortOrder == "newest" )
					sort.Field( f => f.LastUpdated, SortOrder.Descending );
				else if ( sortOrder == "oldest" )
					sort.Field( f => f.LastUpdated, SortOrder.Ascending );
				else if ( sortOrder == "relevance" )
					sort.Descending( SortSpecialField.Score );
				else
					sort.Ascending( s => s.Name );
				//	.Field( ff => ff.Description )

				if ( query.StartPage < 1 )
					query.StartPage = 1;

				//this could truncate a legitimate phrase, so change to square brackets
				//NO - if the org is removed, then the selected record (keyword + org will likely not be the first search result.
				//WWGD-what would google do - will get millions of results with 
				//if ( UtilityManager.GetAppKeyValue( "stripOrganizationSuffixFromSearch", false ) && !string.IsNullOrWhiteSpace(query.Keywords) )
				//{		
				//var pos1 = query.Keywords.LastIndexOf( "(" );
				//var pos2 = query.Keywords.LastIndexOf( ")" );
				//if ( pos1 > 0 && pos2 > pos1 )
				//{
				//	query.Keywords = query.Keywords.Substring( 0, query.Keywords.LastIndexOf( "(" ) );
				//}
				//}
				//

				//try to implement an exact match?

				query.Keywords = query.Keywords.Trim();
				var pos1 = query.Keywords.IndexOf( "\"" );
				var pos2 = query.Keywords.LastIndexOf( "\"" );
				if ( pos1 == 0 && pos2 > pos1 )
				{
					//now what - could force the use of MatchPhrasePrefix. 
					//Works partially, but not for a whole string that is part of another string "account" would return account and account management
					//usingOnlyMatchPhrasePrefix = true;
				}
				//21-07-30 changed from f.Name to f.NameOrganizationKey - to test NOCTI
				//TODO  -try best fields
				//QueryContainer matchPhrasePrefixQuery = null;
				//matchPhrasePrefixQuery = Query<CredentialIndex>.MatchPhrasePrefix( mp => mp
				//				.Field( f => f.NameOrganizationKey )
				//				.Query( query.Keywords )
				//				);
				//
				//QueryContainer matchPhraseBestFieldsQuery = null;
				//matchPhraseBestFieldsQuery = Query<CredentialIndex>.MultiMatch( mp => mp
				//				.Fields( f => f
				//					.Field( ff => ff.NameOrganizationKey, 120 )     //??
				//				)
				//				.Type( TextQueryType.BestFields )
				//				.Query( query.Keywords )
				//				);
				//
				//Phrase Prefix is looking for matches to what a user types in (i.e., the phrase)"
				QueryContainer multiMatchPhrasePrefixQuery = Query<CredentialIndex>.MultiMatch( m => m
					.Fields( f => f
						.Field( ff => ff.NameOrganizationKey, 90 )      //??
						.Field( ff => ff.ListTitle, 90 )
						.Field( ff => ff.PrimaryOrganizationName, 90 )
						.Field( ff => ff.Description, 30 )
						.Field( ff => ff.AlternateNames, 35 )
						.Field( ff => ff.TextValues, 50 )       //no longer being populated? 23-06-06 - chould CTIDs be added to these?
																//.Field( ff => ff.PremiumValues, 50 )
						.Field( ff => ff.Occupation, 30 )
						.Field( ff => ff.Industry, 30 )
						.Field( ff => ff.InstructionalProgram, 30 )
						.Field( ff => ff.Keyword, 60 )
					//.Field( ff => ff.CodedNotation, 30 )  //
					//.Field( ff => ff.TextValues, 30 )	//mostly obsolete, esp if excluding occ/ind/programs
					)
					.Type( TextQueryType.PhrasePrefix )
					.Query( query.Keywords )
				//.MaxExpansions( 10 )
				//.Analyzer( "standard" )
				);
				QueryContainer autocompletePhrasePrefixQuery = Query<CredentialIndex>.MultiMatch( m => m
				.Fields( f => f
					.Field( ff => ff.NameOrganizationKey, 100 )      //??
					.Field( ff => ff.PrimaryOrganizationName, 90 )
				)
				.Type( TextQueryType.PhrasePrefix )
				.Query( query.Keywords )
				//.MaxExpansions( 10 )
				//.Analyzer( "standard" )
				);
				//21-08-31 - not clear of the advantage of best fields
				//ALSO same as matchPhraseBestFieldsQuery
				QueryContainer multiMatchBestFieldsQuery = Query<CredentialIndex>.MultiMatch( m => m
					.Fields( f => f
						.Field( ff => ff.Name, 90 )     //??
						.Field( ff => ff.ListTitle, 90 )
						.Field( ff => ff.PrimaryOrganizationName, 90 )
						.Field( ff => ff.Description, 45 )
						.Field( ff => ff.AlternateNames, 35 )
						.Field( ff => ff.TextValues, 50 )
						//.Field( ff => ff.PremiumValues, 50 )
						.Field( ff => ff.Occupation, 30 )
						.Field( ff => ff.Industry, 30 )
						.Field( ff => ff.InstructionalProgram, 30 )
						.Field( ff => ff.Keyword, 60 )  //
					)
					.Type( TextQueryType.BestFields )
					.Query( query.Keywords )
				//.MaxExpansions( 10 )
				//.Analyzer( "standard" )
				);
				//
				//Cross Fields looks for each term in any of the fields, which is useful for queries like "nursing ohio"
				QueryContainer crossFieldsQuery = null;
				crossFieldsQuery = Query<CredentialIndex>.MultiMatch( m => m
								.Fields( f => f
									.Field( ff => ff.NameOrganizationKey, 90 )
									.Field( ff => ff.CredentialType, 25 )
									.Field( ff => ff.Regions, 75 )
									.Field( ff => ff.Cities, 75 )
									.Field( ff => ff.Countries, 75 )
								 )
								.Type( TextQueryType.CrossFields )
								.Query( query.Keywords )
								);
				//Returns documents that contain terms similar to the search term, as measured by a Levenshtein edit distance.
				//not sure if will help with searching for imbedded letters (eader from leader)
				//QueryContainer fuzzyQuery = null;
				//if ( !string.IsNullOrWhiteSpace( query.Keywords ) )
				//{
				//	fuzzyQuery = Query<CredentialIndex>.Fuzzy( m => m
				//						.Boost( 2.1 )
				//						.Field( ff => ff.NameOrganizationKey )
				//						.Fuzziness( Fuzziness.Auto )
				//						.Value( query.Keywords )
				//						.MaxExpansions( 10 ) //High values in the max_expansions parameter can cause poor performance due to the high number of variations examined.
				//						.PrefixLength( 3 )
				//						.Rewrite( MultiTermQueryRewrite.ConstantScore )
				//						.Transpositions()
				//					 );
				//}

				//SimpleQueryString
				//https://www.elastic.co/guide/en/elasticsearch/reference/current/query-dsl-simple-query-string-query.html
				/*
				 * The simple_query_string query supports the following operators:
				 * https://www.elastic.co/guide/en/elasticsearch/reference/current/query-dsl-simple-query-string-query.html#supported-flags
					+ signifies AND operation
					| signifies OR operation
					- negates a single token
					" wraps a number of tokens to signify a phrase for searching
					* at the end of a term signifies a prefix query
					( and ) signify precedence
					~N after a word signifies edit distance (fuzziness)
					~N after a phrase signifies slop amount
					To use one of these characters literally, escape it with a preceding backslash (\).
				*/
				//QueryContainer simpleQuery = null;
				//if ( !string.IsNullOrWhiteSpace( query.Keywords ) )
				//{
				//	simpleQuery = Query<CredentialIndex>.SimpleQueryString( m => m
				//						.Fields( f => f
				//							.Field( ff => ff.NameOrganizationKey, 90 )
				//							.Field( ff => ff.Description, 25 )
				//						 )
				//						.Query( query.Keywords )
				//						.Analyzer( "standard" )
				//						.DefaultOperator( Operator.Or )
				//						.Flags( SimpleQueryStringFlags.All)
				//						.Lenient()
				//						.AnalyzeWildcard(true) //???? - may only be pertinent if asterisk included 
				//						//.MinimumShouldMatch( 3 )
				//						.FuzzyPrefixLength( 0 )
				//						.FuzzyMaxExpansions( 10 )
				//						.FuzzyTranspositions()
				//						.AutoGenerateSynonymsPhraseQuery( false )
				//					 );
				//}
				//only do for first word. maybe only do with a single phrase
				//OR, only do if no results were found
				QueryContainer queryStringQuery = null;
				if ( !string.IsNullOrWhiteSpace( query.Keywords ) && query.Keywords.IndexOf( "*" ) > -1 )
				{
					queryStringQuery = Query<CredentialIndex>.QueryString( m => m
										.Fields( f => f
											.Field( ff => ff.NameOrganizationKey, 90 )
											.Field( ff => ff.Description, 25 )
										 )
										.Query( query.Keywords )
										.Analyzer( "standard" )
										.QuoteAnalyzer( "keyword" ) //??
										.AllowLeadingWildcard()
										.FuzzyPrefixLength( 0 )
										.FuzzyMaxExpansions( 20 )
										.FuzzyRewrite( MultiTermQueryRewrite.ConstantScore )
										.Rewrite( MultiTermQueryRewrite.ConstantScore )
										.Fuzziness( Fuzziness.Auto )
										.TieBreaker( .5 )       //got error with 1.2, maybe should be between 0 and 1?
										.MinimumShouldMatch( 0 )
										.DefaultOperator( Operator.Or )
										.Lenient()
										.AnalyzeWildcard( true ) //???? - may only be pertinent if asterisk included 
										.AutoGenerateSynonymsPhraseQuery( false ) //??
									 );
				}
				//
				QueryContainer regionQuery = null;
				if ( usingRegionHack )
				{
					regionQuery = locationQueryFilters.RegionQuery;
					locationQueryFilters.RegionQuery = null;
				}

				var pageSize = query.PageSize;
				if ( requestIsAutocomplete )
				{
					pageSize = pageSize * 2;
					crossFieldsQuery = null;
					multiMatchBestFieldsQuery = null;
					//retaining bestFields
				} else if ( ctidQuery != null )
				{
					multiMatchPhrasePrefixQuery = null;
					multiMatchBestFieldsQuery = null;
					crossFieldsQuery = null;
					autocompletePhrasePrefixQuery = null;
				}
				else
				{
					//??
					//autocompletePhrasePrefixQuery = null;

				}
				//
				var search = EC.Search<CredentialIndex>( body => body
						 .Index( CredentialIndex )
						 //.TrackTotalHits = true
						 .Query( q =>
							//q.Term( t => t.Field( f => f.EntityStateId ).Value( 3 ) )
							credentialTypeQuery
							&& credentialIdQuery
							&& entityStateQuery         //testing
							&& widgetIdQuery
							&& collectionIdQuery && collectionsQuery
							&& credentialStatusTypeQuery
							&& connectionsQuery
							&& audienceLevelTypeQuery
							&& audienceTypeQuery
							&& competenciesQuery
							&& ctidQuery
							//&& relationshipIdQuery
							&& subjectsQuery
							&& widgetKeywordsQuery        //special used by widget search only
							&& occupationsQuery
							&& industriesQuery
							&& classificationsQuery
							&& asmntDeliveryTypesQuery
							&& learningDeliveryTypesQuery
							&& boundariesQuery
							&& commonNonQAOrgRolesQuery
							&& commonOutcomeProvidersQuery
							&& qaFilterQuery
							&& commonOrgRolesBYQuery && commonLeftRailOrgRolesBYQuery && publishedBYQuery
							&& languagesQuery
							&& locationQueryFilters.RegionQuery
							&& locationQueryFilters.CountryQuery
							&& locationQueryFilters.CityQuery
							&& wLWIAQuery
							&& history.CreatedFromQuery && history.CreatedToQuery && history.HistoryFromQuery && history.HistoryToQuery
							&& reportsQuery
							&&
							(
								 queryStringQuery || autocompletePhrasePrefixQuery
								//	simpleQuery || matchPhrasePrefixQuery ||	matchPhraseBestFieldsQuery

								|| regionQuery
								|| crossFieldsQuery
								|| multiMatchPhrasePrefixQuery
								|| multiMatchBestFieldsQuery
							)
						 )
						.TrackTotalHits( true )
						.Sort( s => sort )
						//.From( query.StartPage - 1 )
						.From( query.StartPage > 0 ? query.StartPage - 1 : 1 )
						.Skip( ( query.StartPage - 1 ) * query.PageSize )
						.Size( pageSize )
				);


				#endregion
				var debug = search.DebugInformation;
				pTotalRows = ( int ) search.Total;
				if ( requestIsAutocomplete )
				{
					var requestList = ( List<CredentialIndex> ) search.Documents;
					autocompleteList = requestList.Select( x => x.ListTitle ).Distinct().Take( query.PageSize ).ToList();
					return null;
				}
				//TBD
				if ( pTotalRows <= 0 && !string.IsNullOrWhiteSpace( query.Keywords ) )
				{
					//may do a wild card query string search now?
				}
				//trace to custom log file of elasticDebug 
				//this will be messy for multiple searches
				//only do for first page and where has filters
				if ( query.StartPage == 1 )
				{
					try
					{
						//OR to avoid excessive logging could look for a special character 
						if ( query.Filters.Count > 0 || query.FiltersV2.Count > 0 || !string.IsNullOrWhiteSpace( query.Keywords ) )
						{
							//actually extract the request: between #request and #response
							var elasticQuery = UtilityManager.ExtractNameValue( debug, "# Request", ":", "# Response:" );
							LoggingHelper.DoTrace( 6, string.Format( "ElasticServices.Search: {0}, Results: {1}, ElasticLog: \r\n", "Credential", pTotalRows ) + elasticQuery, "ElasticQuery" );

						}
						else if ( query.Filters.Count == 0 && query.FiltersV2.Count == 0 && string.IsNullOrWhiteSpace( query.Keywords ) && query.WidgetId == 0 )
						{
							//if ( UtilityManager.GetAppKeyValue( "environment" ) == "production" && pTotalRows < 23000 )
							//{
							//	//actually extract the request: between #request and #response
							//	var elasticQuery = UtilityManager.ExtractNameValue( debug, "# Request", ":", "# Response:" );
							//	LoggingHelper.DoTrace( 6, string.Format( "ElasticServices.Search: {0}, Results: {1}, ElasticLog: \r\n", "Credential", pTotalRows ) + elasticQuery, "ElasticQuery" );
							//	//TODO - need to be sure not alarmist?
							//	//23-08-20 for example the latter were empty but there was a filter on occupations. It could be related to where a search is done twice
							//	var acctsemail = UtilityManager.GetAppKeyValue( "accountNotifications" );
							//	//so skip accounts for now
							//	//TODO - needs to consider source is FL
							//	var credentialFinderMainSite = UtilityManager.GetAppKeyValue( "credentialFinderMainSite" ) + "search?searchType=credential";
								
							//	//EmailManager.NotifyAdmin( "", "WARNING Credential Blind Search Result Low", $"A <a href='{credentialFinderMainSite}'>Credential blind search</a> only returned {pTotalRows} records. This seems to be unexpected. Full query:\n {elasticQuery}" );
							//}
						}
					}
					catch ( Exception ex )
					{
						var msg = BaseFactory.FormatExceptions( ex );
						LoggingHelper.DoTrace( 3, string.Format( "ElasticServices.Search: {0}. Exception encountered extracting elastic summary. Results: {1}, ElasticLog: \r\n", "Credential", pTotalRows ) + msg, "ElasticQuery" );
					}
				}
				//
				if ( debug.IndexOf( "Invalid NEST response built" ) == 0 )
				{
					string errPhrase = "";
					int causePos = debug.IndexOf( "\"caused_by\"" );
					if ( causePos > -1 )
					{
						errPhrase = UtilityManager.ExtractNameValue( debug.Substring( causePos ), "\"caused_by\":", "{", "}" );
					}
					//don't want to log excessively
					LoggingHelper.WriteLogFileForReason( 5, errPhrase, "Credential_ElasticSearch_debugLog", debug, "", false );
					//include in trace with as much info as possible. Unfortunately, the IPaddress will be the finder.
					LoggingHelper.DoTrace( 1, string.Format( "ElasticServices.CredentialSearch. Encountered 'Invalid NEST response built': \r\n{0}\r\nKeywords: {1}", errPhrase, query.Keywords ) );
					//return invalid search query, try again
					list.Add( new CredentialSummary()
					{
						Name = "NO DATA - You entered an invalid search query.",
						Description = "The information entered for the query DOES NOT COMPUTE. Please try again with a better effort.",
						CredentialType = "", CredentialTypeSchema = "", LastUpdated = DateTime.Now,
						CTID = ""
					} );
					return list;
				}
				//OR try a file
				//LoggingHelper.WriteLogFile( 6, "CredentialElasticSearch_debugLog", debug, "", true);
				//

				LoggingHelper.DoTrace( 7, string.Format( "ElasticServices.CredentialSearch. After search, before mapping: {0} records", pTotalRows ) );
				//	
				if ( pTotalRows > 0 )
				{
					//map results
					list = ElasticManager.Credential_MapFromElastic( ( List<CredentialIndex> ) search.Documents, query.StartPage, query.PageSize );

					//LoggingHelper.DoTrace( 7, string.Format( "ElasticServices.CredentialSearch. found: {0} records", pTotalRows ) );
				}

				//stats
				query.Results = pTotalRows;
				string jsoninput = JsonConvert.SerializeObject( query, JsonHelper.GetJsonSettings() );
				string searchType = "blind";
				if ( query.Filters.Count > 0 || query.FiltersV2.Count > 0 || !string.IsNullOrWhiteSpace( query.Keywords ) )
				{
					searchType = "filters selected";
				}
				if ( query.StartPage > 1 )
					searchType += " - paging";

				//LoggingHelper.DoTrace( 6, "ElasticServices.CredentialSearch. Adding activity" );
				//may want to queue this?
				var activity = new SiteActivity()
				{
					ActivityType = "Credential",
					Activity = "Search",
					Event = searchType,
					Comment = jsoninput
				};

				//ThreadPool.QueueUserWorkItem( AddActivity, activity );
				new ActivityServices().AddActivity( activity );
				//new ActivityServices().AddActivity( new SiteActivity() { ActivityType = "Credential", Activity = "Search", Event = searchType, Comment = jsoninput });
			}
			catch ( Exception ex )
			{
				list.Add( new CredentialSummary()
				{
					Name = "Exception encountered during search",
					Description = ex.Message
				} );
				LoggingHelper.DoTrace( 1, string.Format( "ElasticServices.CredentialSearch. Exception: {0}\r\nKeywords: {1}", ex.Message, query.Keywords ) );

			}
			return list;
		} //

		public List<CredentialSummary> Credential_SearchPROTOTYPING( MainSearchInput query, ref int pTotalRows )
		{
			LoggingHelper.DoTrace( 7, "ElasticServices.Credential_SearchPROTOTYPING - entered" );
			List<CredentialSummary> list = new List<CredentialSummary>();
			Credential_BuildIndex();

			QueryContainer credentialTypeQuery = null;
			QueryContainer credentialIdQuery = null;
			QueryContainer widgetIdQuery = null;
			QueryContainer credentialStatusTypeQuery = null;
			QueryContainer audienceLevelTypeQuery = null;
			QueryContainer audienceTypeQuery = null;
			QueryContainer competenciesQuery = null;
			//QueryContainer relationshipIdQuery = null;
			//probably will have same problem with ORs with subjects entered in widget, and in search box
			QueryContainer subjectsQuery = null;
			//may want to use this for keyword entered in widget??
			QueryContainer keywordsQuery = null;
			QueryContainer connectionsQuery = null;
			QueryContainer occupationsQuery = null;
			QueryContainer industriesQuery = null;
			QueryContainer boundariesQuery = null;
			QueryContainer commonOrgRolesBYQuery = null;
			QueryContainer publishedBYQuery = null;

			QueryContainer qaFilterQuery = null;
			QueryContainer widgetOccupationsQuery = null;
			QueryContainer windustriesQuery = null;

			QueryContainer languagesQuery = null;
			QueryContainer reportsQuery = null;
			QueryContainer classificationsQuery = null;
			QueryContainer asmntDeliveryTypesQuery = null;
			QueryContainer learningDeliveryTypesQuery = null;
			HistoryQueryFilters history = new HistoryQueryFilters();
			//QueryContainer createdFromQuery = null;
			//QueryContainer createdToQuery = null;
			//QueryContainer historyFromQuery = null;
			//QueryContainer historyToQuery = null;
			LocationQueryFilters locationQueryFilters = new LocationQueryFilters();
			var qaRelationshipTypeIds = new List<int>();
			var relationshipTypeIds = new List<int>();
			var credentialStatusTypeIds = new List<int>();
			//20-04-16 mparsons - set a default value for credentialStatusTypeQuery to exclude deprecated. Will be overridden if any credential status are provided
			if ( UtilityManager.GetAppKeyValue( "hidingDeprecatedStatus", false ) )
			{
				var defStatus = CodesManager.Property_GetValues( CodesManager.PROPERTY_CATEGORY_CREDENTIAL_STATUS_TYPE, "", false );
				var defCredentialStatusTypeIds = defStatus.Where( s => s.Title.Contains( "Deprecated" ) == false ).Select( s => s.Id ).ToList();
				credentialStatusTypeQuery = Query<CredentialIndex>.Terms( ts => ts.Field( f => f.CredentialStatusId ).Terms( defCredentialStatusTypeIds.ToArray() ) );
			}
			try
			{

				#region credSearchCategories
				//20-09-09 mparsons - in preparation of implementation of this filter, make it mutual exclusive with the current filter
				bool hasNewPotentialResults = false;
				if ( query.WidgetId > 0 && query.HasCredentialPotentialResults )
				{
					hasNewPotentialResults = true;
					widgetIdQuery = Query<CredentialIndex>.Terms( ts => ts.Field( f => f.ResourceForWidget ).Terms( query.WidgetId ) );

					//QueryContainer widgetIdQuery2 = null;
					//widgetIdQuery = Query<CredentialIndex>.Bool( b => b
					//	 .MustNot( mn => mn
					//		 .Terms( ts => ts.Field( f => f.ResourceForWidget ).Terms( query.WidgetId ) )
					//		 )
					//	);
				}
				if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CODE ) )
				{
					//2,4,14,18,21,39,
					//string searchCategories = UtilityManager.GetAppKeyValue( "credSearchCategories", "" );
					//var credSearchCategories = new List<int>();
					//foreach ( var s in searchCategories.Split( ',' ) )
					//	if ( !string.IsNullOrEmpty( s ) )
					//		credSearchCategories.Add( int.Parse( s ) );

					//var credentialIds = new List<int>();
					var credentialTypeIds = new List<int>();

					var audienceLevelTypeIds = new List<int>();
					var audienceTypeIds = new List<int>();

					var validConnections = new List<string>();
					var connectionFilters = new List<string>();
					var asmntdeliveryTypeIds = new List<int>();
					var learningdeliveryTypeIds = new List<int>();
					var reportIds = new List<int>();
					//var resourceForWidget = new List<int>();

					if ( query.FiltersV2.Any( x => x.AsCodeItem().CategoryId == CodesManager.PROPERTY_CATEGORY_CONNECTION_PROFILE_TYPE ) )
					{
						//LoggingHelper.DoTrace( 7, "Credential search, filters exist for connections" );
						//this will include is part/has part
						//Enumeration entity = CodesManager.GetCredentialsConditionProfileTypes();
						Enumeration entity = CodesManager.GetConnectionTypes( 1, false );
						validConnections = entity.Items.Select( s => s.SchemaName.ToLower() ).ToList();
					}
					//TODO watch for relationshipIds being passed for two filters
					foreach ( var filter in query.FiltersV2.Where( m => m.Type == MainSearchFilterV2Types.CODE ).ToList() )
					{
						var item = filter.AsCodeItem();
						if ( filter.Name == "reports" || filter.Name == "otherfilters" || filter.Name == "filter:OtherFilters" )
						{
							reportIds.Add( item.Id ); //can probably continue after here?
							continue;
						}
						else if ( filter.Name == "" && filter.Values != null && filter.Values.Count == 3 )
						{
							//HACK test while determine why no filter.name
							//var filterItem = filter.AsDefaultCodeItem();
							if ( item.CategoryId == 1 || item.CategoryId == 58 )
							{
								reportIds.Add( item.Id ); //can probably continue after here?
								continue;
							}
						}
						//else if ( item.Name == "" && filter.Values != null && item.SchemaName.IndexOf( "credReport" ) == 0 )
						//{
						//	//we have a report from the non-Other filters section Do a lookup
						//	var propertyId = 0;
						//	if ( ElasticManager.GetPropertyId( 58, item.SchemaName, ref propertyId ) )
						//	{
						//		reportIds.Add( propertyId );
						//	}
						//}

						if ( filter.Name == "history" )
						{
							continue;
						}
						//if ( filter.Name == "widget" )
						//{
						//	resourceForWidget.Add( item.Id );
						//	continue;
						//}
						if ( item == null || item.CategoryId < 1 )
							continue;

						//if ( credSearchCategories.Contains( item.CategoryId ) )
						//{ }
						if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_CREDENTIAL_TYPE )
							credentialTypeIds.Add( item.Id );
						else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_CREDENTIAL_STATUS_TYPE )
							credentialStatusTypeIds.Add( item.Id );
						else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL )
							audienceLevelTypeIds.Add( item.Id );
						else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE )
							audienceTypeIds.Add( item.Id );
						else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_ASMT_DELIVERY_TYPE )
							asmntdeliveryTypeIds.Add( item.Id );
						else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE )
							learningdeliveryTypeIds.Add( item.Id );
						else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE )
						{
							if ( filter.Name == "qualityassurance" || filter.Name == "filter:QAReceived" )
								qaRelationshipTypeIds.Add( item.Id );
							else
								relationshipTypeIds.Add( item.Id );
						}

						else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_CONNECTION_PROFILE_TYPE )
						{
							if ( validConnections.Contains( item.SchemaName.ToLower() ) )
								connectionFilters.Add( item.SchemaName.Replace( "ceterms:", "" ) );
						}
					}

					CommonHistoryFilter<CredentialIndex>( query, ref history );

					if ( credentialTypeIds.Any() )
					{
						//21-05-13 mparsons - check if a badge type - actually only check if from new interface.
						//					- probably better to check prior to here (in translation)
						credentialTypeQuery = Query<CredentialIndex>.Terms( ts => ts.Field( f => f.CredentialTypeId ).Terms( credentialTypeIds.ToArray() ) );
					}

					if ( credentialStatusTypeIds.Any() )
						credentialStatusTypeQuery = Query<CredentialIndex>.Terms( ts => ts.Field( f => f.CredentialStatusId ).Terms( credentialStatusTypeIds.ToArray() ) );

					if ( audienceLevelTypeIds.Any() )
						audienceLevelTypeQuery = Query<CredentialIndex>.Terms( ts => ts.Field( f => f.AudienceLevelTypeIds ).Terms<int>( audienceLevelTypeIds ) );

					if ( audienceTypeIds.Any() )
						audienceTypeQuery = Query<CredentialIndex>.Terms( ts => ts.Field( f => f.AudienceTypeIds ).Terms<int>( audienceTypeIds ) );

					if ( relationshipTypeIds.Any() )
					{
						//no longer an action here, done in common method: CommonQualityAssuranceFilter
					}
					if ( asmntdeliveryTypeIds.Any() )
						asmntDeliveryTypesQuery = Query<CredentialIndex>.Terms( ts => ts.Field( f => f.AsmntDeliveryMethodTypeIds ).Terms<int>( asmntdeliveryTypeIds ) );

					if ( learningdeliveryTypeIds.Any() )
						learningDeliveryTypesQuery = Query<CredentialIndex>.Terms( ts => ts.Field( f => f.LearningDeliveryMethodTypeIds ).Terms<int>( learningdeliveryTypeIds ) );

					if ( reportIds.Any() )
					{
						reportsQuery = Query<CredentialIndex>.Terms( ts => ts.Field( f => f.ReportFilters ).Terms<int>( reportIds.ToArray() ) );
					}

					if ( connectionFilters.Any() )
					{
						connectionFilters.ForEach( x =>
						{
							//LoggingHelper.DoTrace( 6, "Credential search, checking for connections. x=" + x );
							if ( x == "requires" )
								connectionsQuery |= Query<CredentialIndex>.Range( r => r.Field( f => f.RequiresCount ).GreaterThan( 0 ) );
							if ( x == "recommends" )
								connectionsQuery |= Query<CredentialIndex>.Range( r => r.Field( f => f.RecommendsCount ).GreaterThan( 0 ) );
							if ( x == "isRequiredFor" )
								connectionsQuery |= Query<CredentialIndex>.Range( r => r.Field( f => f.RequiredForCount ).GreaterThan( 0 ) );
							if ( x == "isRecommendedFor" )
								connectionsQuery |= Query<CredentialIndex>.Range( r => r.Field( f => f.IsRecommendedForCount ).GreaterThan( 0 ) );
							if ( x == "isAdvancedStandingFor" )
								connectionsQuery |= Query<CredentialIndex>.Range( r => r.Field( f => f.IsAdvancedStandingForCount ).GreaterThan( 0 ) );
							if ( x == "advancedStandingFrom" )
								connectionsQuery |= Query<CredentialIndex>.Range( r => r.Field( f => f.AdvancedStandingFromCount ).GreaterThan( 0 ) );
							if ( x == "isPreparationFor" )
								connectionsQuery |= Query<CredentialIndex>.Range( r => r.Field( f => f.PreparationForCount ).GreaterThan( 0 ) );
							if ( x == "isPreparationFrom" )
								connectionsQuery |= Query<CredentialIndex>.Range( r => r.Field( f => f.PreparationFromCount ).GreaterThan( 0 ) );
							if ( x == "isPartOf" )
								connectionsQuery |= Query<CredentialIndex>.Range( r => r.Field( f => f.IsPartOfCount ).GreaterThan( 0 ) );
							if ( x == "hasPart" )
								connectionsQuery |= Query<CredentialIndex>.Range( r => r.Field( f => f.HasPartCount ).GreaterThan( 0 ) );
							if ( x == "entryCondition" )
								connectionsQuery |= Query<CredentialIndex>.Range( r => r.Field( f => f.EntryConditionCount ).GreaterThan( 0 ) );
						} );

						//LoggingHelper.DoTrace( 6, "Credential search, AFTER checking for connections" );
					}
				}
				#endregion
				#region Widget. Credential Ids list
				if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CUSTOM ) )
				{
					var credIds = new List<int>();
					foreach ( var filter in query.FiltersV2.Where( m => m.Name == "potentialresults" ).ToList() )
					{
						credIds.AddRange( JsonConvert.DeserializeObject<List<int>>( filter.CustomJSON ) );
					}

					if ( credIds.Any() )
					{
						if ( !hasNewPotentialResults )
						{
							LoggingHelper.DoTrace( 6, string.Format( "ElasticServices.Credential_Search - Found Credential Ids list. Count: {0}", credIds.Count() ) );
							credIds.ForEach( x =>
							{
								credentialIdQuery |= Query<CredentialIndex>.Terms( ts => ts.Field( f => f.Id ).Terms( x ) );
							} );
						}
						else
						{
							LoggingHelper.DoTrace( 6, string.Format( "ElasticServices.Credential_Search - Found Credential Ids list. Count: {0}. BUT SKIPPING USE AS there is are query.HasCredentialPotentialResults", credIds.Count() ) );
						}
					}
				}

				#endregion

				#region Handle Location queries
				bool usingRegionHack = false;
				LocationFilter<CredentialIndex>( query, locationQueryFilters, ref usingRegionHack );

				#endregion

				#region QualityAssurance, with owned and offered by
				//****document which is used by the detail page searches for owns/offers

				//NOTE: this is only referenced after clicking on a gray box or on detail, not from the search page
				//		==> actually now used by search widget => type = organizationroles
				//20-11-20 - the QA performed search from org detail page uses this. Updated. Note that the QA target will need to be reindexed
				//21-06-01	ALSO the owns/offers searches from the detail pages
				commonOrgRolesBYQuery = CommonOrgRolesByFilter<CredentialIndex>( query );
				publishedBYQuery = PublishedByFilter<CredentialIndex>( query );

				//USED BY QUALITY ASSURANCE FILTER check boxes and org list
				//21-03-29 mp - while adding organization roles filter, noted that this method may produce a filter using relationshipTypeIds. However, will this work with an org filter????
				//need to ensure relationshipTypeIds are not applied to the wrong filter type
				qaFilterQuery = CommonQualityAssuranceFilter<CredentialIndex>( query, qaRelationshipTypeIds );

				var commonOrgQARolesQuery = CommonNonQAOrgRolesFilter<CredentialIndex>( query, relationshipTypeIds );
				//
				var commonOutcomeProvidersQuery = CommonOutcomeProviders<CredentialIndex>( query );


				#endregion



				#region Competencies
				if ( query.FiltersV2.Any( x => x.Name == "competencies" ) )
				{
					var competencies = new List<string>();
					var competencySchemas = new List<string>();
					foreach ( var filter in query.FiltersV2.Where( m => m.Name == "competencies" ) )
					{
						var codeItem = filter.HasAnyValue();
						if ( codeItem.AnyValue || !string.IsNullOrWhiteSpace( codeItem.SchemaName ) )
						{
							competencySchemas.Add( codeItem.SchemaName );
						}
						else
						{
							var text = filter.AsText();
							try
							{
								if ( text.IndexOf( " - " ) > -1 )
									text = text.Substring( text.IndexOf( " -- " ) + 4 );
							}
							catch { }

							if ( text.Trim().Length > 2 )
							{
								text = SearchServices.SearchifyWord( text );
								competencies.Add( text.Trim() );
							}
						}
					}
					if ( competencySchemas.Any() )
					{
						var ids = CodesManager.GetEntityStatisticBySchema( competencySchemas );
						if ( ids.Any() )
						{
							competenciesQuery = Query<CredentialIndex>.Terms( ts => ts.Field( f => f.ReportFilters ).Terms<int>( ids.ToArray() ) );
						}
					}

					//21-05-10 mparsons - the competency search no longer works!!!!!
					//				- assessment seems to, coping from the latter.
					//competencies.ForEach( x =>
					//{
					//	//Should eventually change once the Competencies have proper inputs.
					//	competenciesQuery |= Query<CredentialIndex>.Nested( n => n
					//			.Path( p => p.Competencies )
					//			.Query( q => q
					//				.Bool( mm => mm
					//					.Must( mu => mu
					//						.MultiMatch( m => m
					//							.Fields( mf => mf.Field( f => f.Competencies.First().Name, 70 ) ).Type( TextQueryType.PhrasePrefix ).Query( x ) ) ||
					//	mu.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Competencies.First().Name, 70 ) ).Type( TextQueryType.BestFields ).Query( x ) ) ) ) ).IgnoreUnmapped() );
					//} );
					if ( competencies.Any() )
					{
						competencies.ForEach( x =>
						{
							//Temporary fix
							competenciesQuery |= Query<CredentialIndex>.MultiMatch( m => m
								.Fields( mf => mf
									.Field( f => f.Competencies.First().Name, 70 ) )
										.Type( TextQueryType.PhrasePrefix )
											.Query( x ) );
							//add BestFields
							competenciesQuery |= Query<CredentialIndex>
								.MultiMatch( m => m
									.Fields( mf => mf
										.Field( f => f.Competencies.First().Name, 70 ) )
											.Type( TextQueryType.BestFields ).Query( x ) );
						} );
						if ( competenciesQuery != null )
							competenciesQuery = Query<CredentialIndex>.Nested( n => n.Path( p => p.Competencies ).Query( q => competenciesQuery ).IgnoreUnmapped() );
					}
				}
				#endregion

				#region Occupations, Industries, programs

				occupationsQuery = CommonOccupations<CredentialIndex>( query, ref widgetOccupationsQuery );

				industriesQuery = CommonIndustries<CredentialIndex>( query, ref windustriesQuery );

				classificationsQuery = CommonCip<CredentialIndex>( query );

				#endregion

				#region Subjects

				if ( query.FiltersV2.Any( x => x.Name == "subjects" ) )
				{
					//LoggingHelper.DoTrace( 6, "ElasticServices.Credential_Search - subjects" );
					subjectsQuery = HandleSubjects<CredentialIndex>( query );
					//var subjects = new List<string>();
					//foreach ( var filter in query.FiltersV2.Where( m => m.Name == "subjects" ) )
					//{
					//	//var text = ServiceHelper.HandleApostrophes( filter.AsText() );
					//	var text = filter.AsText();
					//	if ( string.IsNullOrWhiteSpace( text ) )
					//		continue;
					//	subjects.Add( text.ToLower() );
					//	//fnext += OR + string.Format( titleTemplate, SearchifyWord( text ) );
					//}

					//QueryContainer qc = null;
					//subjects.ForEach( x =>
					//{
					//	qc |= Query<CredentialIndex>.MatchPhrase( mp => mp.Field( f => f.Subjects.First().Name ).Query( x ).Boost( 60 ) );
					//} );
					//if ( qc != null )
					//	subjectsQuery = Query<CredentialIndex>.Nested( n => n.Path( p => p.Subjects ).Query( q => qc ).IgnoreUnmapped() );
				}

				//keywords from widget 
				if ( query.FiltersV2.Any( x => x.Name == "keywords" ) )
				{
					//LoggingHelper.DoTrace( 6, "ElasticServices.Credential_Search - keywords" );
					keywordsQuery = HandleWidgetKeywords<CredentialIndex>( query );
					/*
					var tags = new List<string>();
					foreach ( var filter in query.FiltersV2.Where( m => m.Name == "keywords" ) )
					{
						var text = filter.AsText();
						if ( string.IsNullOrWhiteSpace( text ) )
							continue;
						//this doesn't work, get ORs need ANDs:
						//temp
						//query.Keywords += " " + text;
						tags.Add( text.ToLower() );
					}
					QueryContainer qc = null;
					tags.ForEach( x =>
					{
						//keywordsQuery = Query<CredentialIndex>.MatchPhrase( ts => ts.Field( f => f.Keyword ).Terms( tags ) );
						qc |= Query<CredentialIndex>.MatchPhrase( mp => mp.Field( f => f.Keyword ).Query( x ) );
						//keywordsQuery = Query<CredentialIndex>.Terms( ts => ts.Field( f => f.Keyword ).Terms( tags ) );
						//qc |= Query<CredentialIndex>.MatchPhrase( mp => mp.Field( f => f.Keyword ).Query( x ) );
					} );
					if ( qc != null )
					{
						//keywordsQuery = Query<CredentialIndex>.MatchPhrase( n => n.( p => p.Keyword ).Query( q => qc ).IgnoreUnmapped() );

						keywordsQuery = Query<CredentialIndex>.MultiMatch( m => m
									 .Fields( f => f
										.Field( ff => ff.Name, 90 )
										.Field( ff => ff.ListTitle, 90 )
										.Field( ff => ff.OwnerOrganizationName, 90 )
										.Field( ff => ff.Description, 45 )
										.Field( ff => ff.SubjectWebpage, 25 )
										.Field( ff => ff.AlternateNames, 35 )
										.Field( ff => ff.TextValues, 50 )
										.Field( ff => ff.Keyword, 60 )
									 )
									 //.Slop(2)
									 //.Operator( Operator.And )
									 .Type( TextQueryType.PhrasePrefix )
									 .Query( string.Join( "", tags.ToList() ) )
								//.MaxExpansions( 10 )
								//.Analyzer( "standard" )
								)
								|| Query<CredentialIndex>.MultiMatch( m => m
								.Fields( f => f
									.Field( ff => ff.Name, 90 )
									.Field( ff => ff.ListTitle, 90 )
									 .Field( ff => ff.Description, 45 )
									 .Field( ff => ff.SubjectWebpage, 25 )
									 .Field( ff => ff.OwnerOrganizationName, 90 )
									 .Field( ff => ff.AlternateNames, 35 )
									 .Field( ff => ff.TextValues, 50 )
									 .Field( ff => ff.Keyword, 60 )
								 )
								.Type( TextQueryType.BestFields )
								.Query( string.Join( "", tags.ToList() ) )
								);
					}
					*/
				}
				#endregion

				#region Languages
				var languageFilters = new List<string>();
				if ( query.FiltersV2.Any( x => x.Name == "languages" ) )
				{
					foreach ( var filter in query.FiltersV2.Where( m => m.Name == "languages" ) )
					{
						var text = filter.GetValueOrDefault( "CodeText", "" );
						if ( string.IsNullOrWhiteSpace( text ) )
							continue;
						languageFilters.Add( text.ToLower() );
					}

					//QueryContainer qc = null;
					languageFilters.ForEach( x =>
					{
						languagesQuery |= Query<CredentialIndex>.Terms( ts => ts.Field( f => f.InLanguage ).Terms( x ) );
						//qc |= Query<CredentialIndex>.Terms( ts => ts.Field( f => f.InLanguage ).Terms( languages ) );
					} );
					//languagesQuery = Query<CredentialIndex>.Nested( n => n.Path( p => p.InLanguage ).Query( q => qc ).IgnoreUnmapped() );
				}
				#endregion

				#region Boundaries

				boundariesQuery = HandleLocationQuery<CredentialIndex>( query );
				/*
				var boundaries = SearchServices.GetBoundaries( query, "bounds" );
				if ( boundaries.IsDefined )
				{
					boundariesQuery = Query<CredentialIndex>
						  .Nested( n => n.Path( p => p.Addresses )
						  .Query( q => Query<CredentialIndex>.Range( r => r.Field( f => f.Addresses.First().Longitude ).LessThan( ( double ) boundaries.East ).GreaterThan( ( double ) boundaries.West ) ) && Query<CredentialIndex>.Range( r => r.Field( f => f.Addresses.First().Latitude ).LessThan( ( double ) boundaries.North ).GreaterThan( ( double ) boundaries.South ) ) ).IgnoreUnmapped() );
				}
				*/

				#endregion

				#region Query
				LoggingHelper.DoTrace( 7, "ElasticServices.Credential_Search - starting query" );
				//var tag = string.Format( "*{0}*", query.Keywords.ToLower() );
				//var search = EC.Search<CredentialIndex>( i => i.Index( CredentialIndex ).Query( q => q.Wildcard( w => w.Field( f => f.Name ).Value( query ) ) || q.Wildcard( w => w.Field( f => f.OoName ).Value( query ) ) ) );
				var sort = new SortDescriptor<CredentialIndex>();

				var sortOrder = query.SortOrder;
				if ( sortOrder == "alpha" )
					sort.Ascending( s => s.Name.Suffix( "keyword" ) );
				else if ( sortOrder == "zalpha" )
					sort.Descending( s => s.Name.Suffix( "keyword" ) );
				else if ( sortOrder == "newest" )
					sort.Field( f => f.LastUpdated, SortOrder.Descending );
				else if ( sortOrder == "oldest" )
					sort.Field( f => f.LastUpdated, SortOrder.Ascending );
				else if ( sortOrder == "relevance" )
					sort.Descending( SortSpecialField.Score );
				else
					sort.Ascending( s => s.Name );
				//	.Field( ff => ff.Description )

				if ( query.StartPage < 1 )
					query.StartPage = 1;

				//this could truncate a legitimate phrase, so change to square brackets
				//NO - if the org is removed, then the selected record (keywork + org will likely not be the first search result.
				//WWGD-what would google do - will get millions of results with 
				//if ( UtilityManager.GetAppKeyValue( "stripOrganizationSuffixFromSearch", false ) && !string.IsNullOrWhiteSpace(query.Keywords) )
				//{		
				//var pos1 = query.Keywords.LastIndexOf( "(" );
				//var pos2 = query.Keywords.LastIndexOf( ")" );
				//if ( pos1 > 0 && pos2 > pos1 )
				//{
				//	query.Keywords = query.Keywords.Substring( 0, query.Keywords.LastIndexOf( "(" ) );
				//}
				//}
				//

				//try implement an exact match?
				bool usingOnlyMatchPhrasePrefix = false;
				query.Keywords = query.Keywords.Trim();
				var pos1 = query.Keywords.IndexOf( "\"" );
				var pos2 = query.Keywords.LastIndexOf( "\"" );
				if ( pos1 == 0 && pos2 > pos1 )
				{
					//now what - could force the use of MatchPhrasePrefix. 
					//Works partially, but not for a whole string that is part of another string "account" would return account and account management
					//usingOnlyMatchPhrasePrefix = true;
				}

				var elasticConfiguration = new ElasticConfiguration();

				//this stuff should be cached once completed evaluation
				bool usingMatchPhrasePrefixQuery = UtilityManager.GetAppKeyValue( "usingMatchPhrasePrefixQuery", false );
				bool usingPhrasePrefixQuery = UtilityManager.GetAppKeyValue( "usingPhrasePrefixQuery", false );
				bool usingBestFieldsQuery = UtilityManager.GetAppKeyValue( "usingBestFieldsQuery", true );
				bool usingCrossFieldsQuery = UtilityManager.GetAppKeyValue( "usingCrossFieldsQuery", true );
				//
				if ( enviroment != "production" && query.ElasticConfigs != null && query.ElasticConfigs.Any() )
				{
					//update elasticConfiguration from query
					ConfiguationCredentialElastic( query.ElasticConfigs, ref elasticConfiguration );
				}
				else
				{
					//default to web.config settings
					elasticConfiguration.PhrasePrefix.ExcludeQuery = !usingPhrasePrefixQuery;
					elasticConfiguration.BestFields.ExcludeQuery = !usingBestFieldsQuery;
					elasticConfiguration.CrossFields.ExcludeQuery = !usingCrossFieldsQuery;
					elasticConfiguration.MatchPhrasePrefix.ExcludeQuery = !usingMatchPhrasePrefixQuery;
				}

				//
				QueryContainer matchPhrasePrefixQuery = null;
				//matchPhrasePrefixQuery = Query<CredentialIndex>.MatchPhrasePrefix( mp => mp
				//				.Field( f => f.NameOrganizationKey )
				//				.Query( query.Keywords )
				//				);
				if ( elasticConfiguration.MatchPhrasePrefix.ExcludeQuery )
					matchPhrasePrefixQuery = null;
				// 
				QueryContainer nameAlphaQuery = null;
				if ( !string.IsNullOrWhiteSpace( query.Keywords ) )
				{
					//Regex rgx = new Regex( "[^a-zA-Z0-9 -]" );
					//var kw = rgx.Replace( query.Keywords, "" ).Replace( " ", "" ).Replace( "-", "" );
					//nameAlphaQuery = Query<CredentialIndex>.MultiMatch( mp => mp
					//.Fields( f => f
					//	.Field( ff => ff.NameAlphanumericOnly, 100 )
					//)
					//.Type( TextQueryType.BestFields )
					//.Query( kw )
					//);
				}
				//Phrase Prefix is looking for matches to what a user types in (i.e., the phrase)"
				QueryContainer phrasePrefixQuery = Query<CredentialIndex>.MultiMatch( m => m
					.Fields( f => f
					.Field( ff => ff.NameOrganizationKey, elasticConfiguration.PhrasePrefix.NameOrganization )
					//.Field( ff => ff.NameAlphanumericOnly, elasticConfiguration.PhrasePrefix.NameAlphanumericOnly )
					//.Field( ff => ff.OwnerOrganizationName, elasticConfiguration.PhrasePrefix.OwnerOrganizationName )
					.Field( ff => ff.Description, elasticConfiguration.PhrasePrefix.Description )
					.Field( ff => ff.AlternateNames, elasticConfiguration.PhrasePrefix.AlternateNames )
					//.Field( ff => ff.PremiumValues, 50 )
					.Field( ff => ff.Occupation, elasticConfiguration.PhrasePrefix.Occupation )
					.Field( ff => ff.Industry, elasticConfiguration.PhrasePrefix.Industry )
					.Field( ff => ff.InstructionalProgram, elasticConfiguration.PhrasePrefix.InstructionalPrograms )
					.Field( ff => ff.Keyword, elasticConfiguration.PhrasePrefix.Keyword )  //
																						   //.Field( ff => ff.CodedNotation, 30 )  //
																						   //.Field( ff => ff.TextValues, 30 )	//mostly obsolete, esp if excluding occ/ind/programs
					)
					.Type( TextQueryType.PhrasePrefix )
					.Query( query.Keywords )
			//.MaxExpansions( 10 )
			//.Analyzer( "standard" )
			);
				if ( elasticConfiguration.PhrasePrefix.ExcludeQuery )
					phrasePrefixQuery = null;
				//Best Fields ignores the order of the phrase and results are based on  anything with the words in the phrase.
				//20-12-28 mp - separate credential name and org name
				//21-05-04 mp - remove separated queries, and use the combined name org
				QueryContainer bestFieldsQuery = Query<CredentialIndex>.MultiMatch( m => m
							.Fields( f => f
								.Field( ff => ff.NameOrganizationKey, elasticConfiguration.BestFields.NameOrganization )
								.Field( ff => ff.CTID, 100 )
								//.Field( ff => ff.OwnerOrganizationName, elasticConfiguration.BestFields.OwnerOrganizationName )
								.Field( ff => ff.Description, elasticConfiguration.BestFields.Description )
								.Field( ff => ff.Occupation, elasticConfiguration.BestFields.Occupation )
								.Field( ff => ff.Industry, elasticConfiguration.BestFields.Industry )
								.Field( ff => ff.InstructionalProgram, elasticConfiguration.BestFields.InstructionalPrograms )
								.Field( ff => ff.AlternateNames, elasticConfiguration.BestFields.AlternateNames )
								.Field( ff => ff.QualityAssurancePhrase, elasticConfiguration.BestFields.Industry )
								.Field( ff => ff.Keyword, elasticConfiguration.BestFields.Keyword )
								)
							.Type( TextQueryType.BestFields )
							.Query( query.Keywords )
							);
				if ( elasticConfiguration.BestFields.ExcludeQuery )
					bestFieldsQuery = null;
				//
				QueryContainer credBestFieldsQuery = null;
				//credBestFieldsQuery = Query<CredentialIndex>.MultiMatch( m => m
				//			.Fields( f => f
				//				.Field( ff => ff.NameOrganizationKey, 100 )
				//				)
				//			.Type( TextQueryType.BestFields )
				//			.Query( query.Keywords )
				//			);
				QueryContainer orgBestFieldsQuery = null;
				//orgBestFieldsQuery = Query<CredentialIndex>.MultiMatch( m => m
				//			.Fields( f => f
				//				.Field( ff => ff.OwnerOrganizationName, 90 )
				//				)
				//			.Type( TextQueryType.BestFields )
				//			.Query( query.Keywords )
				//			); 
				//Cross Fields looks for each term in any of the fields, which is useful for queries like "nursing ohio"
				QueryContainer crossFieldsQuery = Query<CredentialIndex>.MultiMatch( m => m
								.Fields( f => f
									.Field( ff => ff.NameOrganizationKey, elasticConfiguration.CrossFields.Name )
									.Field( ff => ff.CredentialType, elasticConfiguration.CrossFields.CredentialType )
									.Field( ff => ff.Regions, elasticConfiguration.CrossFields.Region )
									.Field( ff => ff.Cities, elasticConfiguration.CrossFields.City )
									.Field( ff => ff.Countries, elasticConfiguration.CrossFields.Country )
								 )
								.Type( TextQueryType.CrossFields )
								.Query( query.Keywords )
								);
				if ( elasticConfiguration.CrossFields.ExcludeQuery )
					crossFieldsQuery = null;
				//
				if ( query.ElasticConfigs != null && query.ElasticConfigs.Any() )
				{
					var hasMatchPhrasePrefix = query.ElasticConfigs.Where( s => s.QueryType == "MatchPhrasePrefix" ).ToList();
					if ( hasMatchPhrasePrefix == null || !hasMatchPhrasePrefix.Any() )
					{
						matchPhrasePrefixQuery = null; ;
					}
					else
					{
						//no boosts for this yet
					}
					var hasPhrasePrefix = query.ElasticConfigs.Where( s => s.QuerySubType == "PhrasePrefix" ).ToList();
					if ( hasPhrasePrefix == null || !hasPhrasePrefix.Any() )
					{
						phrasePrefixQuery = null; ;
					}
					else
					{
						//no boosts for this yet
					}
					var hasBestFields = query.ElasticConfigs.Where( s => s.QuerySubType == "BestFields" ).ToList();
					if ( hasBestFields == null || !hasBestFields.Any() )
					{
						bestFieldsQuery = null; ;
					}
					else
					{
						//no boosts for this yet
					}

					var hasCrossFields = query.ElasticConfigs.Where( s => s.QuerySubType == "CrossFields" ).ToList();
					if ( hasCrossFields == null || !hasCrossFields.Any() )
					{
						crossFieldsQuery = null; ;
					}
					else
					{
						//no boosts for this yet
					}
				}
				// 
				if ( usingOnlyMatchPhrasePrefix )
				{
					bestFieldsQuery = null;
					crossFieldsQuery = null;
					phrasePrefixQuery = null; ;
				}
				//
				var search = EC.Search<CredentialIndex>( body => body
						 .Index( CredentialIndex )
						 .Query( q =>
							//q.Term( t => t.Field( f => f.EntityStateId ).Value( 3 ) )
							credentialTypeQuery
							&& credentialIdQuery
							&& widgetIdQuery
							&& credentialStatusTypeQuery
							&& connectionsQuery
							&& audienceLevelTypeQuery
							&& audienceTypeQuery
							&& competenciesQuery
							//&& relationshipIdQuery
							&& subjectsQuery
							&& keywordsQuery        //special used by widget search only
							&& occupationsQuery
							&& industriesQuery
							&& classificationsQuery
							&& asmntDeliveryTypesQuery
							&& learningDeliveryTypesQuery
							&& boundariesQuery
							&& commonOutcomeProvidersQuery
							&& commonOrgQARolesQuery
							&& qaFilterQuery && commonOrgRolesBYQuery && publishedBYQuery
							&& languagesQuery
							&& locationQueryFilters.RegionQuery
							&& locationQueryFilters.CountryQuery
							&& locationQueryFilters.CityQuery
							&& history.CreatedFromQuery && history.CreatedToQuery && history.HistoryFromQuery && history.HistoryToQuery
							&& reportsQuery
							&& (
									//
									//q.MatchPhrasePrefix( mp => mp
									//.Field( f => f.Name )
									//.Query( query.Keywords )
									//)
									matchPhrasePrefixQuery
								|| phrasePrefixQuery
								|| crossFieldsQuery
								|| credBestFieldsQuery || orgBestFieldsQuery || bestFieldsQuery
								|| nameAlphaQuery

							//PhrasePrefix behave just like best_fields, but they use a match_phrase_prefix query instead of a match query.
							//|| q.MultiMatch( m => m
							//	 .Fields( f => f
							//		.Field( ff => ff.Name, 100 )
							//		//.Field( ff => ff.ListTitle, 90 )
							//		.Field( ff => ff.OwnerOrganizationName, 90 )
							//		.Field( ff => ff.Description, 50 )
							//		.Field( ff => ff.AlternateNames, 75 ) 
							//		.Field( ff => ff.Keyword, 60 )  //note Keyword not populated!!! 19-04-26 mp => actually is being populated, 
							//		.Field( ff => ff.TextValues, 50 )
							//		//.Field( ff => ff.CodedNotation )
							//		//.Field( ff => ff.Competencies ) //test how long it takes 
							//	 )
							//	 //.Slop(2)
							//	 //.Operator( Operator.And )
							//	 .Type( TextQueryType.PhrasePrefix )
							//	 .Query( query.Keywords )
							////.MaxExpansions( 10 )
							////.Analyzer( "standard" )
							//) //end q.MultiMatch
							//best fields searches for any words regardless of order
							//|| q.MultiMatch( m => m
							//	.Fields( f => f
							//		.Field( ff => ff.Name, 10 )
							//		.Field( ff => ff.CTID, 100 )
							//		 .Field( ff => ff.Description, 5 )
							//		 .Field( ff => ff.OwnerOrganizationName )
							//		 .Field( ff => ff.AlternateNames )
							//		 .Field( ff => ff.TextValues )
							//		 .Field( ff => ff.Keyword )

							//	 )
							//	.Type( TextQueryType.BestFields )
							//	.Query( query.Keywords )
							//	) //end q.MultiMatch
							)//end keyword related queries
						 )  //Query end
						 .TrackTotalHits( true )
						 .Sort( s => sort )
						 //.From( query.StartPage - 1 )
						 .From( query.StartPage > 0 ? query.StartPage - 1 : 1 )
						 .Skip( ( query.StartPage - 1 ) * query.PageSize )
						 .Size( query.PageSize )
					);


				#endregion
				var debug = search.DebugInformation;
				pTotalRows = ( int ) search.Total;
				//trace to custom log file of elasticDebug 
				//this will be messy for multiple searches
				//only do for first page and where has filters
				if ( query.StartPage == 1 )
				{
					//OR to avoid excessive logging could look for a special character 
					if ( query.Filters.Count > 0 || query.FiltersV2.Count > 0 || !string.IsNullOrWhiteSpace( query.Keywords ) )
					{
						//actually extract the request: between #request and #response
						var elasticQuery = UtilityManager.ExtractNameValue( debug, "# Request", ":", "# Response:" );
						LoggingHelper.DoTrace( 6, string.Format( "ElasticServices.Search: {0}, Results: {1}, ElasticLog: \r\n", "Credential", pTotalRows ) + elasticQuery, "ElasticQuery" );

					}
					else if ( query.Filters.Count == 0 && query.FiltersV2.Count == 0 && string.IsNullOrWhiteSpace( query.Keywords ) && query.WidgetId == 0 )
					{
						if ( UtilityManager.GetAppKeyValue( "environment" ) == "production" && pTotalRows < 23000 )
						{
							//actually extract the request: between #request and #response
							var elasticQuery = UtilityManager.ExtractNameValue( debug, "# Request", ":", "# Response:" );
							LoggingHelper.DoTrace( 6, string.Format( "ElasticServices.Search: {0}, Results: {1}, ElasticLog: \r\n", "Credential", pTotalRows ) + elasticQuery, "ElasticQuery" );
							//var acctsemail = UtilityManager.GetAppKeyValue( "accountNotifications" );
							//EmailManager.NotifyAdmin( acctsemail, "WARNING Credential Blind Search Result Low", string.Format( "A Credential blind search only returned {0} records. This seems to be unexpected.", pTotalRows ) );
						}
					}
				}
				//
				if ( debug.IndexOf( "Invalid NEST response built" ) == 0 )
				{
					string errPhrase = "";
					int causePos = debug.IndexOf( "\"caused_by\"" );
					if ( causePos > -1 )
					{
						errPhrase = UtilityManager.ExtractNameValue( debug.Substring( causePos ), "\"caused_by\":", "{", "}" );
					}
					//don't want to log excessively
					LoggingHelper.WriteLogFileForReason( 5, errPhrase, "Credential_ElasticSearch_debugLog", debug, "", false );
					//if ( !LoggingHelper.IsMessageInCache( errPhrase ) )
					//{
					//	LoggingHelper.WriteLogFile( 5, "Credential_ElasticSearch_debugLog", debug, "", true );
					//	if ( errPhrase .Length > 10)
					//		LoggingHelper.LogError( errPhrase, true, "Finder: CredentialSearch Elastic error" );
					//}
				}
				//OR try a file
				//LoggingHelper.WriteLogFile( 6, "CredentialElasticSearch_debugLog", debug, "", true);
				//

				LoggingHelper.DoTrace( 7, string.Format( "ElasticServices.CredentialSearch. After search, before mapping: {0} records", pTotalRows ) );
				//	
				if ( pTotalRows > 0 )
				{
					//map results
					list = ElasticManager.Credential_MapFromElastic( ( List<CredentialIndex> ) search.Documents, query.StartPage, query.PageSize );

					//LoggingHelper.DoTrace( 7, string.Format( "ElasticServices.CredentialSearch. found: {0} records", pTotalRows ) );
				}

				//stats
				query.Results = pTotalRows;
				string jsoninput = JsonConvert.SerializeObject( query, JsonHelper.GetJsonSettings() );
				string searchType = "blind";
				if ( query.Filters.Count > 0 || query.FiltersV2.Count > 0 || !string.IsNullOrWhiteSpace( query.Keywords ) )
				{
					searchType = "filters selected";
				}
				if ( query.StartPage > 1 )
					searchType += " - paging";

				//LoggingHelper.DoTrace( 6, "ElasticServices.CredentialSearch. Adding activity" );
				//may want to queue this?
				var activity = new SiteActivity()
				{
					ActivityType = "Credential",
					Activity = "Search",
					Event = searchType,
					Comment = jsoninput
				};

				//ThreadPool.QueueUserWorkItem( AddActivity, activity );
				new ActivityServices().AddActivity( activity );
				//new ActivityServices().AddActivity( new SiteActivity() { ActivityType = "Credential", Activity = "Search", Event = searchType, Comment = jsoninput });
			}
			catch ( Exception ex )
			{
				list.Add( new CredentialSummary()
				{
					Name = "Exception encountered during search",
					Description = ex.Message
				} );
			}
			return list;
		} //

		//
		public static void ConfiguationCredentialElastic( List<ElasticSearchFilterConfig> input, ref ElasticConfiguration elasticConfiguration )
		{
			elasticConfiguration.PhrasePrefix.Name = GetPropertyBoost( input, "MultiMatch", "PhrasePrefix", "Name", elasticConfiguration.PhrasePrefix.Name );
			elasticConfiguration.PhrasePrefix.OwnerOrganizationName = GetPropertyBoost( input, "MultiMatch", "PhrasePrefix", "OwnerOrganizationName", elasticConfiguration.PhrasePrefix.OwnerOrganizationName );
			elasticConfiguration.PhrasePrefix.Description = GetPropertyBoost( input, "MultiMatch", "PhrasePrefix", "Description", elasticConfiguration.PhrasePrefix.Description );
			elasticConfiguration.PhrasePrefix.AlternateNames = GetPropertyBoost( input, "MultiMatch", "PhrasePrefix", "AlternateNames", elasticConfiguration.PhrasePrefix.AlternateNames );
			elasticConfiguration.PhrasePrefix.Industry = GetPropertyBoost( input, "MultiMatch", "PhrasePrefix", "Industry", elasticConfiguration.PhrasePrefix.Industry );
			elasticConfiguration.PhrasePrefix.InstructionalPrograms = GetPropertyBoost( input, "MultiMatch", "PhrasePrefix", "InstructionalPrograms", elasticConfiguration.PhrasePrefix.InstructionalPrograms );
			elasticConfiguration.PhrasePrefix.Keyword = GetPropertyBoost( input, "MultiMatch", "PhrasePrefix", "Keyword", elasticConfiguration.PhrasePrefix.Keyword );
			elasticConfiguration.PhrasePrefix.NameAlphanumericOnly = GetPropertyBoost( input, "MultiMatch", "PhrasePrefix", "NameAlphanumericOnly", elasticConfiguration.PhrasePrefix.NameAlphanumericOnly );
			elasticConfiguration.PhrasePrefix.Occupation = GetPropertyBoost( input, "MultiMatch", "PhrasePrefix", "Occupation", elasticConfiguration.PhrasePrefix.Occupation );

			//BestFields
			elasticConfiguration.BestFields.Name = GetPropertyBoost( input, "MultiMatch", "BestFields", "Name", elasticConfiguration.BestFields.Name );
			elasticConfiguration.BestFields.OwnerOrganizationName = GetPropertyBoost( input, "MultiMatch", "BestFields", "OwnerOrganizationName", elasticConfiguration.BestFields.OwnerOrganizationName );
			elasticConfiguration.BestFields.Description = GetPropertyBoost( input, "MultiMatch", "BestFields", "Description", elasticConfiguration.BestFields.Description );
			elasticConfiguration.BestFields.AlternateNames = GetPropertyBoost( input, "MultiMatch", "BestFields", "AlternateNames", elasticConfiguration.BestFields.AlternateNames );
			elasticConfiguration.BestFields.Industry = GetPropertyBoost( input, "MultiMatch", "BestFields", "Industry", elasticConfiguration.BestFields.Industry );
			elasticConfiguration.BestFields.InstructionalPrograms = GetPropertyBoost( input, "MultiMatch", "BestFields", "InstructionalPrograms", elasticConfiguration.BestFields.InstructionalPrograms );
			elasticConfiguration.BestFields.Keyword = GetPropertyBoost( input, "MultiMatch", "BestFields", "Keyword", elasticConfiguration.BestFields.Keyword );
			elasticConfiguration.BestFields.Occupation = GetPropertyBoost( input, "MultiMatch", "BestFields", "Occupation", elasticConfiguration.BestFields.Occupation );

			//CrossFields
			elasticConfiguration.CrossFields.Name = GetPropertyBoost( input, "MultiMatch", "CrossFields", "Name", elasticConfiguration.CrossFields.Name );
			elasticConfiguration.CrossFields.CredentialType = GetPropertyBoost( input, "MultiMatch", "CrossFields", "CredentialType", elasticConfiguration.CrossFields.CredentialType );
			//elasticConfiguration.CrossFields.Description = GetPropertyBoost( input, "MultiMatch", "CrossFields", "Description", elasticConfiguration.CrossFields.Description );
			elasticConfiguration.CrossFields.City = GetPropertyBoost( input, "MultiMatch", "CrossFields", "City", elasticConfiguration.CrossFields.City );
			elasticConfiguration.CrossFields.Region = GetPropertyBoost( input, "MultiMatch", "CrossFields", "Region", elasticConfiguration.CrossFields.Region );
			elasticConfiguration.CrossFields.Country = GetPropertyBoost( input, "MultiMatch", "CrossFields", "Country", elasticConfiguration.CrossFields.Country );

			//MatchPhrasePrefix
			elasticConfiguration.MatchPhrasePrefix.Name = GetPropertyBoost( input, "MatchPhrasePrefix", "", "Name", elasticConfiguration.MatchPhrasePrefix.Name );
			elasticConfiguration.MatchPhrasePrefix.Keywords = GetPropertyBoost( input, "MatchPhrasePrefix", "", "Keywords", elasticConfiguration.MatchPhrasePrefix.Keywords );

		} //

		//
		public static int GetPropertyBoost( List<ElasticSearchFilterConfig> input, string queryType, string querySubType, string property, int defaultValue = 0 )
		{
			int boost = 0;
			//var boostValue1 = input.FirstOrDefault( s => s.QueryType == queryType && s.QuerySubType == querySubType && s.FieldName == property ).Boost;
			if ( querySubType == "" )
				querySubType = null;
			var configQuery = input.FirstOrDefault( s => s.QueryType == queryType && ( s.QuerySubType == querySubType ) && s.FieldName == property );
			if ( configQuery != null && configQuery.FieldName != null )
				boost = configQuery.Boost;
			else
				boost = defaultValue;
			return boost;
		} //

		//
		public static void AddActivity( object entity )
		{
			if ( entity.GetType() != typeof( workIT.Models.SiteActivity ) )
				return;
			var activity = ( entity as workIT.Models.SiteActivity );
			new ActivityServices().AddActivity( activity );
		} //

		//test - keep for a while
		public static List<CredentialSummary> CredentialSearchTest( MainSearchInput query )
		{
			List<CredentialSummary> list = new List<CredentialSummary>();

			var search = EC.Search<CredentialIndex>( i => i.Index( CredentialIndex ).Query( q =>
				q.MultiMatch( m => m
					.Fields( f => f
						.Field( ff => ff.Name )
						.Field( ff => ff.PrimaryOrganizationName )
					)
					.Type( TextQueryType.PhrasePrefix )
					.Query( query.Keywords )
				)
				||
				q.MultiMatch( m => m
					.Fields( f => f
						.Field( ff => ff.Name )
						.Field( ff => ff.PrimaryOrganizationName )
					)
					.Type( TextQueryType.BestFields )
					.Query( query.Keywords )
				)
			)
			.From( query.StartPage > 0 ? query.StartPage - 1 : 1 )
			.Skip( ( query.StartPage - 1 ) * query.PageSize )
			.Size( query.PageSize ) );

			//var search = EC.Search<CredentialIndex>( i => i.Index( CredentialIndex ).Query( q => q.Term( t => t.Field( f => f.Subjects.First() ).Value( new[] { "Anatomy" } ) ) ) );

			var pTotalRows = ( int ) search.Total;

			//list = JsonConvert.DeserializeObject<List<CredentialSummary>>( results );
			//var results = CredentialServices.Search( data, ref totalResults );
			//results = searchService.ConvertCredentialResults( list.Documents.ToList(), totalResults, searchType );

			return list;
		} //
		#endregion

		#endregion

		#region Organizations

		public static void Organization_BuildIndex( bool deleteIndexFirst = false, bool updatingIndexRegardless = false )
		{
			bool indexInitialized = false;
			try
			{
				if ( deleteIndexFirst && EC.Indices.Exists( OrganizationIndex ).Exists )
					EC.Indices.Delete( OrganizationIndex );

				if ( !EC.Indices.Exists( OrganizationIndex ).Exists )
				{
					Organization_InitializeIndex();
					indexInitialized = true;
				}

				if ( indexInitialized || updatingIndexRegardless )
				{
					LoggingHelper.DoTrace( 1, "Organization - Building Index" );

					new ActivityServices().AddActivity( new SiteActivity()
					{ ActivityType = "Organization", Activity = "Elastic", Event = "Build Index" }
					);

					int processed = 0;
					string filter = "( base.EntityStateId >= 2 )";
					//20-12-28 - update to be like credentials
					Organization_UpdateIndex( filter, ref processed );
					if ( processed > 0 )
					{
						var refreshResults = EC.Indices.Refresh( OrganizationIndex );
						new ActivityServices().AddActivity( new SiteActivity()
						{
							ActivityType = "Organization",
							Activity = "Elastic",
							Event = "Build Index Completed",
							Comment = string.Format( "Completed rebuild of OrganizationIndex for {0} records.", processed )
						} );
					}
					else
					{
						//ISSUE
						LoggingHelper.LogError( "Build OrganizationIndex: no results were returned from Organization_SearchForElastic method." );
					}
					//var list = ElasticManager.Organization_SearchForElastic( filter );

					//var results = EC.Bulk( b => b.IndexMany( list, ( d, organization ) => d.Index( OrganizationIndex ).Document( organization ).Id( organization.Id.ToString() ) ) );
					////need to first identify a error phrase or will get all error entries displayed
					//if ( results.ToString().IndexOf( "Valid NEST response built from a successful low level cal" ) == -1 )
					//{
					//	Console.WriteLine( results.ToString() );
					//	LoggingHelper.DoTrace( 1, " Issue building organization index: " + results );
					//}
					////??????
					//EC.Indices.Refresh( OrganizationIndex );
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "Organization_BuildIndex" );
			}
		}

		public static void Organization_ManageIndex( bool deleteIndexFirst = false )
		{
			if ( deleteIndexFirst && EC.Indices.Exists( OrganizationIndex ).Exists )
			{
				EC.Indices.Delete( OrganizationIndex );
			}
			if ( !EC.Indices.Exists( OrganizationIndex ).Exists )
			{
				Organization_InitializeIndex();
			}
		}

		public static void Organization_InitializeIndex()
		{
			if ( !EC.Indices.Exists( OrganizationIndex ).Exists )
			{
				EC.Indices.Create( OrganizationIndex, c => new CreateIndexDescriptor( OrganizationIndex )
				.Settings( st => st
						 .Analysis( an => an.TokenFilters( tf => tf.Stemmer( "custom_english_stemmer", ts => ts.Language( "english" ) ) ).Analyzers( anz => anz.Custom( "custom_lowercase_stemmed", cc => cc.Tokenizer( "standard" ).Filters( new List<string> { "lowercase", "custom_english_stemmer" } ) ) ) )
						 .Setting( UpdatableIndexSettings.MaxResultWindow, 50000 )  //not sure of implications?
						 )
						//.Mappings( ms => ms
						.Map<OrganizationIndex>( m => m
							.AutoMap()
							.Properties( p => p
							.Text( s => s.Name( n => n.Description ).Analyzer( "custom_lowercase_stemmed" ) )
								//.Text( s => s.Index( true ).Name( n => n.Name ).Analyzer( "lowercase_analyzer" ) )
								.Text( s => s.Index( true ).Name( n => n.TextValues ) )

								.Nested<IndexReferenceFramework>( n => n
									.Name( nn => nn.Industries )
									.AutoMap( 5 )
								) //AgentRelationshipForEntity will replace IndexQualityAssurance
								 .Nested<Models.Elastic.AgentRelationshipForEntity>( n => n
									.Name( nn => nn.AgentRelationshipsForEntity )
									.AutoMap()
								)
								// .Nested<Models.Elastic.IndexQualityAssurance>( n => n
								//	.Name( nn => nn.QualityAssurance )
								//	.AutoMap()
								//)
								.Nested<Models.Elastic.QualityAssurancePerformed>( n => n
									.Name( nn => nn.QualityAssurancePerformed )
									.AutoMap()
								)
								.Nested<Models.Elastic.IndexProperty>( n => n
									.Name( nn => nn.OrganizationClaimTypes )
									.AutoMap()
								)
								 .Nested<Models.Elastic.Address>( n => n
									.Name( nn => nn.Addresses )
									.AutoMap( 5 )
								)
								 .Nested<Models.Common.IdentifierValue>( n => n
									.Name( nn => nn.RegionIdentifier )
									.AutoMap()
								)
								.Object<JObject>( o => o
									.Name( n => n.ResourceDetail )
									.Enabled( false )
								)
							)
						)

				//)	//.Mappings
				);
			}

			//EC.Indices.Create( OrganizationIndex, c => c.Mappings( m => m.Map<OrganizationIndex>( d => d.AutoMap() ) ) );
		}
		public static void Organization_UpdateIndex( int recordId )
		{
			if ( recordId < 1 )
				return;
			try
			{
				string filter = string.Format( " ( base.Id = {0} ) ", recordId );
				int processed = 0;
				Organization_UpdateIndex( filter, ref processed );
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "Organization_UpdateIndex" );
			}
		} //


		/// <summary>
		/// Pass a filter to use for updating the index
		/// </summary>
		/// <param name="filter"></param>
		public static void Organization_UpdateIndex( string filter, ref int processed )
		{
			processed = 0;
			if ( string.IsNullOrWhiteSpace( filter ) )
				return;
			int action = UtilityManager.GetAppKeyValue( "updateCredIndexAction", 0 );
			if ( action == 0 )
				return;
			string methodName = "Organization_UpdateIndex";
			string IndexName = OrganizationIndex;
			int pageSize = nonCredentialPageSize;
			int pageNbr = 1;
			int totalRows = 0;
			bool isComplete = false;
			int cntr = 0; ;
			try
			{
				while ( pageNbr > 0 && !isComplete )
				{
					//NOTE: where filter is pendingReindex, the number of entries will be reduced on page 2+ 
					totalRows = 0;
					var list = ElasticManager.Organization_SearchForElastic( filter, pageSize, pageNbr, ref totalRows );
					if ( list != null && list.Count > 0 )
					{
						processed += list.Count;
						if ( action == 1 )
						{
							foreach ( var item in list )
							{
								cntr++;
								var res = EC.Index( item, idx => idx.Index( IndexName ) );
								Console.WriteLine( res.Result );
							}
						}
						else if ( action == 2 )
						{
							cntr = cntr + list.Count;
							var results = EC.Bulk( b => b.IndexMany( list, ( d, record ) => d.Index( IndexName ).Document( record ).Id( record.Id.ToString() ) ) );
							if ( results.ToString().IndexOf( "Valid NEST response built from a successful" ) == -1 )
							{
								Console.WriteLine( results.ToString() );
								LoggingHelper.DoTrace( 1, string.Format( " Issue building {0}: ", IndexName ) + results.DebugInformation.Substring( 0, 2000 ) );
							}
						}
						LoggingHelper.DoTrace( 4, string.Format( "{0}. Page: {1}, Records Indexed: {2} of {3}", methodName, pageNbr, processed, totalRows ) );

					}
					else
					{
						//LoggingHelper.DoTrace( 2, string.Format( "Organization_UpdateIndex failed, no data returned for filter: {0}", filter ) );
						if ( list != null && list.Count == 1 && totalRows == -1 )
						{
							LoggingHelper.LogError( string.Format( "{0}: Error in search. {1}", methodName, list[0].Description ) );
						}
						else if ( pageNbr == 1 )
						{
							if ( string.IsNullOrWhiteSpace( filter ) )
							{
								LoggingHelper.LogError( string.Format( "{0}: entered with no filter, but no results were returned from search.", methodName ) );
							}
							LoggingHelper.DoTrace( 2, string.Format( "{0}: NOTE no data returned for filter: {1}", methodName, filter ) );
						}
						isComplete = true;
						break;
					}

					if ( !isComplete )
						pageNbr++;
					if ( cntr >= totalRows )
					{
						isComplete = true;
					}

				} //loop
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "{0} failed for filter: {1}", methodName, filter ) );
			}

		}
		public static List<OrganizationSummary> OrganizationSimpleSearch( MainSearchInput query, ref int pTotalRows )
		{
			var list = new List<OrganizationSummary>();
			if ( query.FiltersV2.Any( x => x.Name == "keywords" ) )
			{
				//where is called from ???
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "keywords" ) )
				{
					var text = filter.AsText();
					if ( string.IsNullOrWhiteSpace( text ) )
						continue;
					//this doesn't work:
					//temp
					query.Keywords += " " + text;

				}

			}
			//here we want to exclude a record if already tagged in widget
			//only do this if called from widget.Configure potential results
			QueryContainer widgetIdQuery = null;
			if ( query.WidgetId > 0 )
			{
				if ( query.MustHaveWidget )
				{
					widgetIdQuery = Query<OrganizationIndex>.Terms( ts => ts.Field( f => f.ResourceForWidget ).Terms( query.WidgetId ) );
				}
				else if ( query.MustNotHaveWidget )
				{
					widgetIdQuery = Query<OrganizationIndex>.Bool( b => b
							 .MustNot( mn => mn
								 .Terms( ts => ts.Field( f => f.ResourceForWidget ).Terms( query.WidgetId ) )
								 )
							);
				}
			}
			var sort = new SortDescriptor<OrganizationIndex>();

			var sortOrder = query.SortOrder;
			if ( sortOrder == "alpha" )
				sort.Ascending( s => s.Name.Suffix( "keyword" ) );
			else if ( sortOrder == "zalpha" )
				sort.Descending( s => s.Name.Suffix( "keyword" ) );
			else if ( sortOrder == "newest" )
				sort.Field( f => f.LastUpdated, SortOrder.Descending );
			else if ( sortOrder == "oldest" )
				sort.Field( f => f.LastUpdated, SortOrder.Ascending );
			else if ( sortOrder == "relevance" )
				sort.Descending( SortSpecialField.Score );
			else
				sort.Ascending( s => s.Name );
			if ( query.StartPage < 1 )
				query.StartPage = 1;
			var search = EC.Search<OrganizationIndex>( i => i.Index( OrganizationIndex )
				.Query( q =>
				 ( q.MultiMatch( m => m
								.Fields( f => f
								   .Field( ff => ff.Name, 90 )
								   .Field( ff => ff.SubjectWebpage, 60 )
								   .Field( ff => ff.Description, 45 )
								   .Field( ff => ff.Keyword, 60 )
							)
					  .Type( TextQueryType.PhrasePrefix )
					  .Query( query.Keywords )
					  .MaxExpansions( 10 )
					 )
					 || q.MultiMatch( m => m
							.Fields( f => f
							   .Field( ff => ff.Name, 90 )
							   .Field( ff => ff.Description, 75 )
							   .Field( ff => ff.SubjectWebpage, 25 )
							)
							.Type( TextQueryType.BestFields )
							.Query( query.Keywords )
							)
					)
				 )
				 .TrackTotalHits( true )
				 .Sort( s => sort )
					 //.From( query.StartPage - 1 )
					 .From( query.StartPage > 0 ? query.StartPage - 1 : 1 )
					 .Skip( ( query.StartPage - 1 ) * query.PageSize )
					 .Size( query.PageSize ) );

			var debug = search.DebugInformation;
			pTotalRows = ( int ) search.Total;


			if ( pTotalRows > 0 )
			{
				//map results
				list = ElasticManager.Organization_MapFromElastic( ( List<OrganizationIndex> ) search.Documents, query.StartPage, query.PageSize );

				LoggingHelper.DoTrace( 6, string.Format( "ElasticServices.OrganizationSearch. found: {0} records", pTotalRows ) );
			}
			//stats
			query.Results = pTotalRows;
			return list;
		}

		/// <summary>
		/// Organization autocomplete.
		/// Unlike others, the orgId needs to be returned as well 
		/// </summary>
		/// <param name="keyword"></param>
		/// <param name="maxTerms"></param>
		/// <param name="pTotalRows"></param>
		/// <returns></returns>
		public List<FilterItem> OrganizationAutoComplete( MainSearchInput query, int maxTerms, ref int pTotalRows, bool includingAllFiltersWithAutocomplete= false )
		{
			//this should only be done if the autocomplete is for the org search, vs an ownedBy search
			if ( includingAllFiltersWithAutocomplete && UtilityManager.GetAppKeyValue( "includingAllFiltersWithAutocomplete", false ) )
			{
				List<FilterItem> autocompleteList = new List<FilterItem>();

				//test just calling the related main search and returning the terms
				OrganizationSearch( query, ref pTotalRows, ref autocompleteList, true );
				return autocompleteList;
			}

			string keyword = query.Keywords;
			QueryContainer organizationEntityStateQuery = null;
			QueryContainer lifeCycleStatusTypeQuery = null;

			//organizationEntityStateQuery = Query<OrganizationIndex>.Terms( ts => ts.Field( f => f.EntityStateId ).Terms<int>( 3 ) );

			if ( UtilityManager.GetAppKeyValue( "usingEntityStateIdBoosting", false ) )
			{
				//this can be a problem for the autocomplete, a ref org may not appear
				organizationEntityStateQuery = Query<OrganizationIndex>
						.Boosting( bq => bq
							.Positive( pq => pq
								.Terms( ts => ts
									.Field( f => f.EntityStateId ).Terms<int>( 3 ) )
								)
							.Negative( nq => nq
								.Terms( ts => ts
									.Field( f => f.EntityStateId ).Terms<int>( 2 ) )
								)
							.NegativeBoost( negativeBooster )
							);
			}
			if ( UtilityManager.GetAppKeyValue( "hidingCeasedStatus", false ) )
			{
				var defStatus = CodesManager.Property_GetValues( CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, "", false );
				var exists = defStatus.Where( s => s.Title.Contains( "Ceased" ) == false ).ToList();
				if ( exists != null && exists.Count > 0 )
				{
					//var defaultStatusTypeIds = defStatus.Where( s => s.Title.Contains( "Ceased" ) == false ).Select( s => s.Id ).ToList();
					var defaultStatusTypeIds = exists.Select( s => s.Id ).ToList();
					lifeCycleStatusTypeQuery = Query<OrganizationIndex>.Terms( ts => ts.Field( f => f.LifeCycleStatusTypeId ).Terms( defaultStatusTypeIds.ToArray() ) );
				}
			}
			var search = EC.Search<OrganizationIndex>( i => i
							.Index( OrganizationIndex )
							.Query( q =>
								organizationEntityStateQuery
								&& lifeCycleStatusTypeQuery
								&& q.MultiMatch( m => m
								   .Fields( f => f
									  .Field( ff => ff.Name, 100 )
									  .Field( ff => ff.Description )
									  //.Field( ff => ff.SubjectWebpage )
									  )
								  .Type( TextQueryType.PhrasePrefix )
								  .Query( keyword )
								  .MaxExpansions( 10 )
								)
							)
						   .Size( maxTerms * 2 )
						   );

			//Need to be look for other approaches            

			var debug = search.DebugInformation;
			pTotalRows = ( int ) search.Total;
			var list = ( List<OrganizationIndex> ) search.Documents;
			//TODO - should we just add the suffix in elastic?
			var distinctList = list.Distinct().Take( maxTerms ).Select( x => new FilterItem() { Label = !string.IsNullOrWhiteSpace( x.CTID ) ? x.Name : x.Name.IndexOf("[reference]") == -1 ? x.Name + " [reference]" : x.Name, Text = x.Name, Id = x.Id } ).ToList();
			return distinctList; //Needs to have Id in order to work properly
		}

		public static List<FilterItem> OrganizationQAAutoComplete( string keyword, int entityTypeId )
		{
			int maxTerms = 20;
			QueryContainer HasQaQuery = null;
			if ( entityTypeId == 1 )
				HasQaQuery = Query<OrganizationIndex>.Term( t => t.Field( f => f.HasCredentialsQAPerformed ).Value( true ) );
			else if ( entityTypeId == 2 )
				HasQaQuery = Query<OrganizationIndex>.Term( t => t.Field( f => f.HasOrganizationsQAPerformed ).Value( true ) );
			else if ( entityTypeId == 3 )
				HasQaQuery = Query<OrganizationIndex>.Term( t => t.Field( f => f.HasAssessmentsQAPerformed ).Value( true ) );
			else if ( entityTypeId == 7 )
				HasQaQuery = Query<OrganizationIndex>.Term( t => t.Field( f => f.HasLoppsQAPerformed ).Value( true ) );
			else
				HasQaQuery = Query<OrganizationIndex>.Term( t => t.Field( f => f.HasCredentialsQAPerformed ).Value( true ) );

			keyword = keyword.Trim();
			var search = EC.Search<OrganizationIndex>( i => i
							.Index( OrganizationIndex )
							.Query( q =>
							HasQaQuery &&
								 q.MultiMatch( m => m
									.Fields( f => f
									   .Field( ff => ff.Name )
									   )
								   //.Operator( Operator.Or )
								   .Type( TextQueryType.PhrasePrefix )
								   .Query( keyword )
								   .MaxExpansions( 10 )
									)
								)
						   .Size( maxTerms * 2 )
						   );
			var debug = search.DebugInformation;
			//Need to be look for other approaches            
			var list = ( List<OrganizationIndex> ) search.Documents;
			return list.Distinct().Take( maxTerms ).Select( x => new FilterItem() { Label = x.Name, Text = x.Name, Id = x.Id } ).ToList(); //Needs to have Id in order to work properly
		}

		/// <summary>
		/// Proxy for full search
		/// </summary>
		/// <param name="query"></param>
		/// <param name="pTotalRows"></param>
		/// <returns></returns>
		public List<OrganizationSummary> OrganizationSearch( MainSearchInput query, ref int pTotalRows )
		{
			var autocompleteList = new List<FilterItem>();
			bool requestIsAutocomplete = false;
			return OrganizationSearch( query, ref pTotalRows, ref autocompleteList, requestIsAutocomplete );
		}
		public List<OrganizationSummary> OrganizationSearch( MainSearchInput query, ref int pTotalRows, ref List<FilterItem> autocompleteList, bool requestIsAutocomplete = false )
		{
			LoggingHelper.DoTrace( 7, thisClassName + "OrganizationSearch Started" );
			Organization_BuildIndex();

			List<OrganizationSummary> list = new List<OrganizationSummary>();

			QueryContainer organizationEntityStateQuery = null;
			QueryContainer organizationTypeQuery = null;
			QueryContainer orgClassTypeQuery = null;

			QueryContainer HasQaQuery = null;
			QueryContainer OrgIdQuery = null;
			QueryContainer organizationServiceQuery = null;
			QueryContainer sectorTypeQuery = null;
			QueryContainer claimTypeQuery = null;
			//QueryContainer relationshipIdQuery = null;
			QueryContainer commonOrgRolesBYQuery = null;
			QueryContainer publishedBYQuery = null;

			QueryContainer qaFilterQuery = null;
			QueryContainer industriesQuery = null;
			QueryContainer boundariesQuery = null;
			QueryContainer keywordsQuery = null;
			QueryContainer lifeCycleStatusTypeQuery = null;
			QueryContainer wLWIAQuery = null;
			QueryContainer windustriesQuery = null;

			QueryContainer qualityAssurancePerformedTypesQuery = null;
			QueryContainer qualityAssurancePerformedAgentQuery = null;
			HistoryQueryFilters history = new HistoryQueryFilters();
			//QueryContainer createdFromQuery = null;
			//QueryContainer createdToQuery = null;
			//QueryContainer historyFromQuery = null;
			//QueryContainer historyToQuery = null;

			QueryContainer reportsQuery = null;
			WidgetQueryFilters widgetQuery = new WidgetQueryFilters();
			LocationQueryFilters locationQueryFilters = new LocationQueryFilters();
			var relationshipTypeIds = new List<int>();
			List<int> LifeCycleStatusTypeIds = new List<int>();

			if ( !query.IncludingReferenceObjects )
			{
				//organizationEntityStateQuery = Query<OrganizationIndex>.Terms( ts => ts.Field( f => f.EntityStateId ).Terms<int>( 3 ) );
			}

			if ( UtilityManager.GetAppKeyValue( "usingEntityStateIdBoosting", false ) )
			{
				if ( query.HasFilters )
				{
					//TODO: filter to watch for "Is A nnn Reference" (and maybe "Is a Registered nnn")
					if ( query.FiltersV2.Any( filter => filter.Name == "reports" ) )
					{
						//skip for now
					}
					else
					{
						//new - only show  top level objects if no keywords (what others to consider)
						if ( query.HasKeywords )
						{
							//entityStateQuery = null;
							//or see how to use boosting
							//back to a TODO - skipping for now
							/*
							entityStateQuery = Query<OrganizationIndex>
								.Boosting( bq => bq
									.Positive( pq => pq
										.Terms( ts => ts
											.Field( f => f.EntityStateId ).Terms<int>( 3 ) )
										)
									.Negative( nq => nq
										.Terms( ts => ts
											.Field( f => f.EntityStateId ).Terms<int>( 2 ) )
										)
									.NegativeBoost( negativeBooster ) //what if skipped?
									);
							*/
						}
						else
						{
							organizationEntityStateQuery = Query<OrganizationIndex>.Terms( ts => ts.Field( f => f.EntityStateId ).Terms<int>( 3 ) );
						}
					}
				}
				else
				{
					organizationEntityStateQuery = Query<OrganizationIndex>.Terms( ts => ts.Field( f => f.EntityStateId ).Terms<int>( 3 ) );
				}

			}

			//20-04-16 mparsons - set a default value for LifeCycleStatusTypeQuery to exclude deprecated. Will be overridden if any LifeCycleStatusType status are provided. might be better to have simple property of IsActive (non ceased)
			if ( UtilityManager.GetAppKeyValue( "hidingCeasedStatus", false ) )
			{
				var defStatus = CodesManager.Property_GetValues( CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, "", false );
				var exists = defStatus.Where( s => s.Title.Contains( "Ceased" ) == false ).ToList();
				if ( exists != null && exists.Count > 0 )
				{
					//var defaultStatusTypeIds = defStatus.Where( s => s.Title.Contains( "Ceased" ) == false ).Select( s => s.Id ).ToList();
					var defaultStatusTypeIds = exists.Select( s => s.Id ).ToList();
					lifeCycleStatusTypeQuery = Query<OrganizationIndex>.Terms( ts => ts.Field( f => f.LifeCycleStatusTypeId ).Terms( defaultStatusTypeIds.ToArray() ) );
				}
			}

			#region orgSearchCategories
			if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CODE ) )
			{
				string searchCategories = UtilityManager.GetAppKeyValue( "orgSearchCategories", "7,8,9,30," );
				var orgSearchCategories = new List<int>();
				var orgClassTypeIds = new List<int>();

				foreach ( var s in searchCategories.Split( ',' ) )
					if ( !string.IsNullOrEmpty( s ) )
						orgSearchCategories.Add( int.Parse( s ) );

				var organizationTypeIds = new List<int>();
				var organizationServiceIds = new List<int>();
				var sectorTypeIds = new List<int>();
				var claimTypeIds = new List<int>();

				var qualityAssurancePerformedIds = new List<int>();
				var reportIds = new List<int>();

				foreach ( var filter in query.FiltersV2.Where( m => m.Type == MainSearchFilterV2Types.CODE ).ToList() )
				{
					var item = filter.AsCodeItem();
					if ( filter.Name == "reports" || filter.Name == "otherfilters" || filter.Name == "filter:OtherFilters" )
					{
						reportIds.Add( item.Id );
						continue;
					}
					else if ( item.Name == "" && filter.Values != null && item.SchemaName.IndexOf( "orgReport" ) == 0 )
					{
						//TBD - ensure no related problems
						//we have a report from the non-Other filters section Do a lookup
						var propertyId = 0;
						if ( ElasticManager.GetPropertyId( 59, item.SchemaName, ref propertyId ) )
						{
							reportIds.Add( propertyId );
							continue;
						}
					}
					//history
					//if ( filter.Name == "history" || filter.Name == "filter:History" )
					//{
					//	var dateFilter = filter.AsDateItem();
					//	if ( dateFilter != null && !string.IsNullOrWhiteSpace( dateFilter.Name ) && !string.IsNullOrWhiteSpace( dateFilter.Code ) )
					//	{
					//		if ( dateFilter.Name == "lastUpdatedFrom" && BaseFactory.IsValidDate( dateFilter.Code ) )
					//		{
					//			historyFromQuery = Query<OrganizationIndex>.DateRange( c => c
					//				  .Boost( 1.1 )
					//				  .Field( p => p.LastUpdated )
					//				  .GreaterThanOrEquals( dateFilter.Code )
					//				  .Format( "MM/dd/yyyy||yyyy" )
					//			);
					//		}
					//		if ( dateFilter.Name == "lastUpdatedTo" && BaseFactory.IsValidDate( dateFilter.Code ) )
					//		{
					//			historyToQuery = Query<OrganizationIndex>.DateRange( c => c
					//				  .Boost( 1.1 )
					//				  .Field( p => p.LastUpdated )
					//				  .LessThanOrEquals( dateFilter.Code )
					//				  .Format( "MM/dd/yyyy||yyyy" )
					//			);
					//		}
					//		//
					//		if ( dateFilter.Name == "createdFrom" && BaseFactory.IsValidDate( dateFilter.Code ) )
					//		{
					//			createdFromQuery = Query<OrganizationIndex>.DateRange( c => c
					//				  .Boost( 1.1 )
					//				  .Field( p => p.Created )
					//				  .GreaterThanOrEquals( dateFilter.Code )
					//				  .Format( "MM/dd/yyyy||yyyy" )
					//			);
					//		}
					//		if ( dateFilter.Name == "createdTo" && BaseFactory.IsValidDate( dateFilter.Code ) )
					//		{
					//			createdToQuery = Query<OrganizationIndex>.DateRange( c => c
					//				  .Boost( 1.1 )
					//				  .Field( p => p.Created )
					//				  .LessThanOrEquals( dateFilter.Code )
					//				  .Format( "MM/dd/yyyy||yyyy" )
					//			);
					//		}
					//	}

					//}

					//Filters - OrganizationTypes, ServiceTypes
					//CategoryId typically relates to the Codes.PropertyCategory.Id
					if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_ORGANIZATION_TYPE )
						organizationTypeIds.Add( item.Id );
					//
					else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_ORGANIZATION_CLASS_TYPE )
						orgClassTypeIds.Add( item.Id );
					else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_ORG_SERVICE )
						organizationServiceIds.Add( item.Id );
					//Filters - Sector Types 
					else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_ORGANIZATION_SECTORTYPE )
						sectorTypeIds.Add( item.Id );
					//Filters - Claim Types 
					else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_CLAIM_TYPE )
						claimTypeIds.Add( item.Id );
					else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS )
						LifeCycleStatusTypeIds.Add( item.Id );


					//Filters - Quality Assurance Performed
					if ( filter.Name == "qaperformed" )
						qualityAssurancePerformedIds.Add( item.Id );
					//Filters - Quality Assurance
					else if ( item.CategoryId == 13 )
						relationshipTypeIds.Add( item.Id );

				}
				//history
				CommonHistoryFilter<OrganizationIndex>( query, ref history );

				if ( reportIds.Any() )
				{
					reportsQuery = Query<OrganizationIndex>.Terms( ts => ts.Field( f => f.ReportFilters ).Terms<int>( reportIds.ToArray() ) );
				}

				if ( organizationTypeIds.Any() )
					organizationTypeQuery = Query<OrganizationIndex>.Terms( ts => ts.Field( f => f.OrganizationTypeIds ).Terms( organizationTypeIds.ToArray() ) );
				//
				if ( orgClassTypeIds.Any() )
					orgClassTypeQuery = Query<OrganizationIndex>.Terms( ts => ts.Field( f => f.EntityTypeId ).Terms( orgClassTypeIds.ToArray() ) );
				//
				if ( organizationServiceIds.Any() )
					organizationServiceQuery = Query<OrganizationIndex>.Terms( ts => ts.Field( f => f.OrganizationServiceTypeIds ).Terms( organizationServiceIds.ToArray() ) );

				if ( sectorTypeIds.Any() )
					sectorTypeQuery = Query<OrganizationIndex>.Terms( ts => ts.Field( f => f.OrganizationSectorTypeIds ).Terms<int>( sectorTypeIds ) );

				if ( claimTypeIds.Any() )
					claimTypeQuery = Query<OrganizationIndex>.Terms( ts => ts.Field( f => f.OrganizationClaimTypeIds ).Terms<int>( claimTypeIds ) );
				if ( LifeCycleStatusTypeIds.Any() )
					lifeCycleStatusTypeQuery = Query<OrganizationIndex>.Terms( ts => ts.Field( f => f.LifeCycleStatusTypeId ).Terms( LifeCycleStatusTypeIds.ToArray() ) );

				if ( relationshipTypeIds.Any() )
				{
					//relationshipIdQuery = Query<OrganizationIndex>.Nested( n => n.Path( p => p.AgentRelationshipsForEntity ).Query( q => q.Terms( t => t.Field( f => f.AgentRelationshipsForEntity.First().RelationshipTypeIds ).Terms<int>( relationshipTypeIds.ToArray() ) ) ) );
					if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.TEXT ) && query.FiltersV2.Where( m => m.Name == "qualityassurance" ).Count() > 0 )
					{
						//save relationships for use with the QA orgs
					}
					else
					{
						//relationshipIdQuery = Query<OrganizationIndex>.Nested( n => n.Path( p => p.AgentRelationshipsForEntity )
						//		.Query( q =>
						//			q.Terms( t =>
						//				t.Field( f => f.AgentRelationshipsForEntity.First().RelationshipTypeIds )
						//					.Terms<int>( relationshipTypeIds.ToArray() ) ) )
						//				);
					}
				}

				if ( qualityAssurancePerformedIds.Any() )
					qualityAssurancePerformedIds.ForEach( x =>
					{
						qualityAssurancePerformedTypesQuery |= Query<OrganizationIndex>.Nested( n => n.Path( p => p.QualityAssurancePerformed ).Query( q => q.Bool( mm => mm.Must( mu => mu.Match( m => m.Field( f => f.QualityAssurancePerformed.First().AssertionTypeIds ).Query( x.ToString() ) ) ) ) ) );
					} );
			}

			#endregion

			#region Handle Widget Mode queries
			//WidgetFilter<OrganizationIndex>( widgetQuery, query.WidgetId );
			//keywords from widget
			if ( query.FiltersV2.Any( x => x.Name == "keywords" ) )
			{
				keywordsQuery = HandleWidgetKeywords<OrganizationIndex>( query );
				//foreach ( var filter in query.FiltersV2.Where( m => m.Name == "keywords" ) )
				//{
				//	//append to any other keywords
				//	var text = filter.AsText();
				//	if ( string.IsNullOrWhiteSpace( text ) )
				//		continue;
				//	//query.Keywords += " " + text;
				//}
			}
			#endregion

			#region Handle Location queries
			bool usingRegionHack = false;
			LocationFilter<OrganizationIndex>( query, locationQueryFilters, ref usingRegionHack );
			//custom filter for Illinois LWIAs
			if ( query.FiltersV2.Count > 0 )
			{
				wLWIAQuery = LocationSubregionFilter<OrganizationIndex>( query );
			}
			#endregion

			#region Industries
			industriesQuery = CommonIndustries<OrganizationIndex>( query, ref windustriesQuery );
			#endregion

			#region QualityAssurance
			//NOTE: this is only referenced after clicking on a gray box, not from the search page
			//		==> actually now used by search widget => CONFIRM THIS
			//20-11-20 - the QA performed search from org detail page uses this. Updated. Note that the QA target will need to be reindexed
			commonOrgRolesBYQuery = CommonOrgRolesByFilter<OrganizationIndex>( query );
			publishedBYQuery = PublishedByFilter<OrganizationIndex>( query );

			//USED BY QUALITY ASSURANCE FILTER check boxes and org list
			qaFilterQuery = CommonQualityAssuranceFilter<OrganizationIndex>( query, relationshipTypeIds );
			#endregion

			#region QualityAssurancePerformed
			qualityAssurancePerformedAgentQuery = CommonQualityAssurancePerformed<OrganizationIndex>( query );
			//if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CUSTOM ) )
			//         {
			//             var assurances = new List<CodeItem>();
			//             foreach 
			//		( var filter in query.FiltersV2.Where( m => m.Name == "qualityassuranceperformed" ).ToList() )
			//             {
			//                 assurances.Add( filter.AsQapItem() );
			//             }
			//             if ( assurances.Any() )
			//                 assurances.ForEach( x =>
			//                 {
			//                     qualityAssurancePerformedQuery |= Query<OrganizationIndex>.Nested( n => n.Path( p => p.QualityAssurancePerformed ).Query( q => q.Bool( mm => mm.Must( mu => mu.Match( m => m.Field( f => f.QualityAssurancePerformed.First().AssertionTypeIds ).Query( x.AssertionId.ToString() ) ) && mu.Match( m => m.Field( f => f.QualityAssurancePerformed.First().TargetEntityBaseId ).Query( x.Id.ToString() ) ) ) ) ) );
			//                 } );
			//         }
			#endregion

			#region Organization Ids list
			if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CUSTOM ) )
			{
				var orgIds = new List<int>();
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "potentialresults" ).ToList() )
				{
					orgIds.AddRange( JsonConvert.DeserializeObject<List<int>>( filter.CustomJSON ) );
				}

				if ( orgIds.Any() )
					orgIds.ForEach( x =>
					{
						OrgIdQuery |= Query<OrganizationIndex>.Terms( ts => ts.Field( f => f.Id ).Terms( x ) );
					} );
			}
			#endregion

			#region Boundaries

			boundariesQuery = HandleLocationQuery<OrganizationIndex>( query );
			/*
			var boundaries = SearchServices.GetBoundaries( query, "bounds" );
			if ( boundaries.IsDefined )
			{
				boundariesQuery = Query<OrganizationIndex>
					.Nested( n => n.Path( p => p.Addresses )
					.Query( q => Query<OrganizationIndex>.Range( r => r.Field( f => f.Addresses.First().Longitude ).LessThan( ( double ) boundaries.East ).GreaterThan( ( double ) boundaries.West ) ) && Query<OrganizationIndex>.Range( r => r.Field( f => f.Addresses.First().Latitude ).LessThan( ( double ) boundaries.North ).GreaterThan( ( double ) boundaries.South ) ) ).IgnoreUnmapped() );
			}
			*/
			#endregion

			#region Query

			var sort = new SortDescriptor<OrganizationIndex>();

			var sortOrder = query.SortOrder;
			if ( sortOrder == "alpha" )
				sort.Ascending( s => s.Name.Suffix( "keyword" ) );
			else if ( sortOrder == "zalpha" )
				sort.Descending( s => s.Name.Suffix( "keyword" ) );
			else if ( sortOrder == "newest" )
				sort.Field( f => f.LastUpdated, SortOrder.Descending );
			else if ( sortOrder == "oldest" )
				sort.Field( f => f.LastUpdated, SortOrder.Ascending );
			else if ( sortOrder == "relevance" )
				sort.Descending( SortSpecialField.Score );
			else
				sort.Ascending( s => s.Name );
			//
			QueryContainer phrasePrefixQuery = Query<OrganizationIndex>
				.MultiMatch( m => m
					.Fields( f => f
						.Field( ff => ff.Name, 100 )      //??
						.Field( ff => ff.Description, 55 )
						.Field( ff => ff.SubjectWebpage, 25 )
						.Field( ff => ff.AlternateNames, 50 )
						.Field( ff => ff.TextValues, 45 )
						.Field( ff => ff.Industry, 30 )
						.Field( ff => ff.Keyword, 50 )
					)
				.Type( TextQueryType.PhrasePrefix )
				.Query( query.Keywords )
				.MaxExpansions( 10 )
			//.Analyzer( "standard" )
			);
			//
			QueryContainer bestFieldsQuery = Query<OrganizationIndex>
				.MultiMatch( m => m
					.Fields( f => f
							   .Field( ff => ff.Name, 100 )
							   .Field( ff => ff.Description, 55 )
							   .Field( ff => ff.SubjectWebpage, 25 )
							   .Field( ff => ff.AlternateNames, 35 )
							   .Field( ff => ff.TextValues, 45 )
							   .Field( ff => ff.Keyword, 50 )
							)
							.Type( TextQueryType.BestFields )
							.Query( query.Keywords )
			);
			//
			//QueryContainer autoCompleteQuery = SetAutocompleteQuery<OrganizationIndex>( query );
			QueryContainer autoCompleteQuery = Query<OrganizationIndex>
				.MultiMatch( m => m
					.Fields( f => f
						.Field( ff => ff.Name, 100 )      //??
						.Field( ff => ff.Description )
				)
				.Type( TextQueryType.PhrasePrefix )
				.Query( query.Keywords )
				.MaxExpansions( 10 )
			//.Analyzer( "standard" )
			);
			var pageSize = query.PageSize;
			if ( requestIsAutocomplete )
			{
				pageSize = pageSize * 2;
				phrasePrefixQuery = null;
				//bestFieldsQuery = null;
			} else
			{
				autoCompleteQuery = null;
			}

			if ( query.StartPage < 1 )
				query.StartPage = 1;

			var search = EC.Search<OrganizationIndex>( body => body
				   .Index( OrganizationIndex )
				   .Query( q =>
					  //q.Term( t => t.Field( f => f.EntityTypeId ).Value( 3 ) )
					  organizationTypeQuery
					  && OrgIdQuery
					  && organizationEntityStateQuery
					  && HasQaQuery
					  && widgetQuery.OwningOrgsQuery
					  && organizationServiceQuery
					  && sectorTypeQuery
					  && keywordsQuery
					  && orgClassTypeQuery
					  && industriesQuery
					  && boundariesQuery
					  && lifeCycleStatusTypeQuery
					  && qaFilterQuery && commonOrgRolesBYQuery && publishedBYQuery
					  && qualityAssurancePerformedAgentQuery
					  && qualityAssurancePerformedTypesQuery
					  && reportsQuery
					  && locationQueryFilters.RegionQuery
					  && locationQueryFilters.CountryQuery
					  && locationQueryFilters.CityQuery
					  && widgetQuery.KeywordQuery
					  //&& history.CreatedFromQuery && history.CreatedToQuery && history.HistoryFromQuery && history.HistoryToQuery
					  && claimTypeQuery
					  && ( autoCompleteQuery || phrasePrefixQuery || bestFieldsQuery )
					 )
				   .TrackTotalHits( true )
				   .Sort( s => sort )
				   .From( query.StartPage > 0 ? query.StartPage - 1 : 1 )
				   .Skip( ( query.StartPage - 1 ) * query.PageSize )
				   .Size( pageSize )
				   );


			#endregion

			var debug = search.DebugInformation;
			pTotalRows = ( int ) search.Total;

			if ( query.StartPage == 1 && ( query.Filters.Count > 0 || query.FiltersV2.Count > 0 || !string.IsNullOrWhiteSpace( query.Keywords ) ) )
			{
				//actually extract the request: between #request and #response
				var elasticQuery = UtilityManager.ExtractNameValue( debug, "# Request", ":", "# Response:" );
				LoggingHelper.DoTrace( 6, string.Format( "ElasticServices.Search: {0}, requestIsAutocomplete: {1}, Results: {2}, ElasticLog: \r\n", "Organization", requestIsAutocomplete, pTotalRows ) + elasticQuery, "ElasticQuery" );

			}
			//
			if ( requestIsAutocomplete )
			{
				var requestList = ( List<OrganizationIndex> ) search.Documents;
				autocompleteList = requestList.Distinct().Take( query.PageSize ).Select( x => new FilterItem() { Label = x.Name, Text = x.Name, Id = x.Id } ).ToList(); //Needs to have Id in
				return null;
			}
			if ( pTotalRows > 0 )
			{
				list = ElasticManager.Organization_MapFromElastic( ( List<OrganizationIndex> ) search.Documents, query.StartPage, query.PageSize );
				LoggingHelper.DoTrace( 6, string.Format( "ElasticServices.OrganizationSearch. found: {0} records", pTotalRows ) );
				if ( list.Count == 0 )
					pTotalRows = 0;
			}

			//stats
			query.Results = pTotalRows;
			string jsoninput = JsonConvert.SerializeObject( query, JsonHelper.GetJsonSettings() );

			if ( query.LoggingActivity )
			{
				string searchType = "blind";
				if ( query.Filters.Count > 0 || query.FiltersV2.Count > 0 || !string.IsNullOrWhiteSpace( query.Keywords ) )
				{
					searchType = "filters selected";
				}
				if ( query.StartPage > 1 )
					searchType += " - paging";
				new ActivityServices().AddActivity( new SiteActivity()
				{ ActivityType = "Organization", Activity = "Search", Event = searchType, Comment = jsoninput }
				);
			}
			return list;
		}

		#endregion

		#region Assessments

		#region Build/update index
		public static void Assessment_BuildIndex( bool deleteIndexFirst = false, bool updatingIndexRegardless = false )
		{
			var list = new List<AssessmentIndex>();
			bool indexInitialized = false;
			if ( deleteIndexFirst && EC.Indices.Exists( AssessmentIndex ).Exists )
			{
				EC.Indices.Delete( AssessmentIndex );
			}
			if ( !EC.Indices.Exists( AssessmentIndex ).Exists )
			{
				//if ( UtilityManager.GetAppKeyValue( "environment" ) == "development" )
				Assessment_InitializeIndex7();
				//else
				//	AssessmentInitializeIndex();
				indexInitialized = true;
			}

			if ( indexInitialized || updatingIndexRegardless )
			{
				LoggingHelper.DoTrace( 1, "Assessment- Building Index" );
				int minEntityStateId = UtilityManager.GetAppKeyValue( "minAsmtEntityStateId", 3 );
				try
				{
					new ActivityServices().AddActivity( new SiteActivity()
					{ ActivityType = "AssessmentProfile", Activity = "Elastic", Event = "Build Index" }
					);
					int processed = 0;

					string filter = "( base.EntityStateId >= 2 )";
					//20-12-28 - update to be like credentials
					Assessment_UpdateIndex( filter, ref processed );
					//list = ElasticManager.Assessment_SearchForElastic( string.Format( "( base.EntityStateId >= {0} )", minEntityStateId ) );
					if ( processed > 0 )
					{
						var refreshResults = EC.Indices.Refresh( AssessmentIndex );
						new ActivityServices().AddActivity( new SiteActivity()
						{
							ActivityType = "Assessment",
							Activity = "Elastic",
							Event = "Build Index Completed",
							Comment = string.Format( "Completed rebuild of AssessmentIndex for {0} records.", processed )
						} );
					}
					else
					{
						//ISSUE
						LoggingHelper.LogError( "Assessment_BuildIndex: no results were returned from Assessment_UpdateIndex method." );
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, "Assessment_BuildIndex" );
				}
				finally
				{
					//if ( list != null && list.Count > 0 )
					//{
					//	var results = EC.Bulk( b => b.IndexMany( list, ( d, document ) => d.Index( AssessmentIndex ).Document( document ).Id( document.Id.ToString() ) ) );
					//	if ( results.ToString().IndexOf( "Valid NEST response built from a successful low level cal" ) == -1 )
					//	{
					//		Console.WriteLine( results.ToString() );
					//		LoggingHelper.DoTrace( 1, " Issue building assessment index: " + results );
					//	}

					//	EC.Indices.Refresh( AssessmentIndex );
					//}
				}
			}

		}
		#endregion

		public static void Assessment_ManageIndex( bool deleteIndexFirst = false )
		{
			if ( deleteIndexFirst && EC.Indices.Exists( AssessmentIndex ).Exists )
			{
				EC.Indices.Delete( AssessmentIndex );
			}
			if ( !EC.Indices.Exists( AssessmentIndex ).Exists )
			{
				Assessment_InitializeIndex7();
			}
		}
		public static void Assessment_InitializeIndex7( bool deleteIndexFirst = true )
		{
			if ( !EC.Indices.Exists( AssessmentIndex ).Exists )
			{
				var tChars = new List<string> { "letter", "digit", "punctuation", "symbol" };

				EC.Indices.Create( AssessmentIndex, c => new CreateIndexDescriptor( AssessmentIndex )
				 .Settings( st => st
						 .Analysis( an => an.TokenFilters( tf => tf.Stemmer( "custom_english_stemmer", ts => ts.Language( "english" ) ) )
							.Analyzers( anz => anz
								.Custom( "custom_lowercase_stemmed", cc => cc.Tokenizer( "standard" )
									.Filters( new List<string> { "lowercase", "custom_english_stemmer" } ) ) ) 
							)
						 .Setting( UpdatableIndexSettings.MaxResultWindow, 50000 )  //not sure of implications?
						 )
						//.Mappings( ms => ms
						.Map<AssessmentIndex>( m => m
							.AutoMap()
							.Properties( p => p
								.Text( s => s.Name( n => n.Description ).Analyzer( "custom_lowercase_stemmed" ) )
								//.Keyword( s => s.Name( n => n.Name ) ) //will this allow searches of strings within strings

								.Nested<IndexCompetency>( n => n
									.Name( nn => nn.Competencies ) //21-08-31 chg to use combined competencies
									.AutoMap()
								)
								.Nested<IndexReferenceFramework>( n => n
									.Name( nn => nn.Industries )
									.AutoMap()
								)
								.Nested<IndexReferenceFramework>( n => n
									.Name( nn => nn.Occupations )
									.AutoMap()
								)
								.Nested<IndexReferenceFramework>( n => n
									.Name( nn => nn.InstructionalPrograms )
									.AutoMap()
								)
								.Nested<Models.Elastic.Address>( n => n
									.Name( nn => nn.Addresses )
									.AutoMap()
								) //AgentRelationshipForEntity will replace IndexQualityAssurance
								 .Nested<Models.Elastic.AgentRelationshipForEntity>( n => n
									.Name( nn => nn.AgentRelationshipsForEntity )
									.AutoMap()
								)
								.Object<JObject>( o => o
									.Name( n => n.ResourceDetail )
									.Enabled( false ) )
							)
						)
				//)
				);
			}
		}

		public static void Assessment_UpdateIndex( int recordId )
		{
			if ( recordId < 1 )
				return;
			try
			{
				string filter = string.Format( " ( base.Id = {0} ) ", recordId );
				int processed = 0;
				Assessment_UpdateIndex( filter, ref processed );

			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "Assessment_UpdateIndex" );
			}
		} //


		/// <summary>
		/// Pass a filter to use for updating the index
		/// </summary>
		/// <param name="filter"></param>
		public static void Assessment_UpdateIndex( string filter, ref int processed )
		{
			processed = 0;
			if ( string.IsNullOrWhiteSpace( filter ) )
				return;
			int action = UtilityManager.GetAppKeyValue( "updateCredIndexAction", 0 );
			if ( action == 0 )
				return;
			string methodName = "Assessment_UpdateIndex";
			string IndexName = AssessmentIndex;
			int pageSize = nonCredentialPageSize;
			int pageNbr = 1;
			int totalRows = 0;
			bool isComplete = false;
			int cntr = 0; ;
			try
			{
				while ( pageNbr > 0 && !isComplete )
				{
					var list = ElasticManager.Assessment_SearchForElastic( filter, pageSize, pageNbr, ref totalRows );
					if ( list != null && list.Count > 0 )
					{
						processed += list.Count;
						if ( action == 1 )
						{
							foreach ( var item in list )
							{
								cntr++;
								var res = EC.Index( item, idx => idx.Index( IndexName ) );
								Console.WriteLine( res.Result );
							}
						}
						else if ( action == 2 )
						{
							cntr = cntr + list.Count;
							var results = EC.Bulk( b => b.IndexMany( list, ( d, record ) => d.Index( IndexName ).Document( record ).Id( record.Id.ToString() ) ) );
							if ( results.ToString().IndexOf( "Valid NEST response built from a successful" ) == -1 )
							{
								Console.WriteLine( results.ToString() );
								LoggingHelper.DoTrace( 1, string.Format( " Issue building {0}: ", IndexName ) + results.DebugInformation.Substring( 0, 2000 ) );
							}
						}
					}
					else
					{
						if ( list != null && list.Count == 1 && totalRows == -1 )
						{
							LoggingHelper.LogError( string.Format( "{0}: Error in search. {1}", methodName, list[0].Description ) );

						}
						else if ( pageNbr == 1 )
						{
							if ( string.IsNullOrWhiteSpace( filter ) )
							{
								LoggingHelper.LogError( string.Format( "{0}: entered with no filter, but no results were returned from search.", methodName ) );
							}
							LoggingHelper.DoTrace( 2, string.Format( "{0}: NOTE no data returned for filter: {1}", methodName, filter ) );
						}
						isComplete = true;
						break;
					}
					LoggingHelper.DoTrace( 4, string.Format( "{0}. Page: {1}, Records Indexed: {2} of {3}", methodName, pageNbr, processed, totalRows ) );
					pageNbr++;
					if ( cntr >= totalRows )
					{
						isComplete = true;
					}

				} //loop
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "{0} failed for filter: {1}", methodName, filter ) );
			}
		}

		public static List<PM.AssessmentProfile> AssessmentSimpleSearch( MainSearchInput query, ref int pTotalRows )
		{
			var list = new List<PM.AssessmentProfile>();
			if ( query.FiltersV2.Any( x => x.Name == "keywords" ) )
			{

				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "keywords" ) )
				{
					var text = filter.AsText();
					if ( string.IsNullOrWhiteSpace( text ) )
						continue;
					//this doesn't work:
					//temp
					query.Keywords += " " + text;

				}

			}
			//here we want to exclude a record if already tagged in widget
			//only do this if called from widget.Configure potential results
			QueryContainer widgetIdQuery = null;
			if ( query.WidgetId > 0 )
			{
				if ( query.MustHaveWidget )
				{
					widgetIdQuery = Query<AssessmentIndex>.Terms( ts => ts.Field( f => f.ResourceForWidget ).Terms( query.WidgetId ) );
				}
				else if ( query.MustNotHaveWidget )
				{
					widgetIdQuery = Query<AssessmentIndex>.Bool( b => b
							 .MustNot( mn => mn
								 .Terms( ts => ts.Field( f => f.ResourceForWidget ).Terms( query.WidgetId ) )
								 )
							);
				}
			}
			var sort = new SortDescriptor<AssessmentIndex>();

			var sortOrder = query.SortOrder;
			if ( sortOrder == "alpha" )
				sort.Ascending( s => s.Name.Suffix( "keyword" ) );
			else if ( sortOrder == "zalpha" )
				sort.Descending( s => s.Name.Suffix( "keyword" ) );
			else if ( sortOrder == "newest" )
				sort.Field( f => f.LastUpdated, SortOrder.Descending );
			else if ( sortOrder == "oldest" )
				sort.Field( f => f.LastUpdated, SortOrder.Ascending );
			else if ( sortOrder == "relevance" )
				sort.Descending( SortSpecialField.Score );
			else
				sort.Ascending( s => s.Name );
			if ( query.StartPage < 1 )
				query.StartPage = 1;
			var search = EC.Search<AssessmentIndex>( i => i.Index( AssessmentIndex ).Query( q =>
				 q.MultiMatch( m => m
								 .Fields( f => f
								  .Field( ff => ff.Name, 90 )
								  .Field( ff => ff.PrimaryOrganizationName, 80 )
								  .Field( ff => ff.SubjectWebpage, 60 )
								  .Field( ff => ff.Description, 45 )
								  .Field( ff => ff.Keyword, 60 )
								  .Field( ff => ff.SubjectAreas, 60 )
				 )
				 .Type( TextQueryType.PhrasePrefix )
				 .Query( query.Keywords )
				 .MaxExpansions( 10 ) ) )
				 .TrackTotalHits( true )
				 .Sort( s => sort )
					 //.From( query.StartPage - 1 )
					 .From( query.StartPage > 0 ? query.StartPage - 1 : 1 )
					 .Skip( ( query.StartPage - 1 ) * query.PageSize )
					 .Size( query.PageSize ) );

			var debug = search.DebugInformation;
			pTotalRows = ( int ) search.Total;
			if ( query.StartPage == 1 && ( query.Filters.Count > 0 || query.FiltersV2.Count > 0 || !string.IsNullOrWhiteSpace( query.Keywords ) ) )
			{
				//actually extract the request: between #request and #response
				var elasticQuery = UtilityManager.ExtractNameValue( debug, "# Request", ":", "# Response:" );
				LoggingHelper.DoTrace( 6, string.Format( "ElasticServices.Search: {0}, Results: {1}, ElasticLog: \r\n", "Assessment Simple Search", pTotalRows ) + elasticQuery, "ElasticQuery" );
			}
			if ( pTotalRows > 0 )
			{
				//map results
				list = ElasticManager.Assessment_MapFromElastic( ( List<AssessmentIndex> ) search.Documents, query.StartPage, query.PageSize );

				LoggingHelper.DoTrace( 6, string.Format( "ElasticServices.AssessmentSearch. found: {0} records", pTotalRows ) );
			}
			//stats
			query.Results = pTotalRows;
			return list;
		}
		/// <summary>
		/// Autocomplete search for Assessments
		/// </summary>
		/// <param name="query"></param>
		/// <param name="maxTerms"></param>
		/// <param name="pTotalRows"></param>
		/// <returns></returns>
		public List<string> AssessmentAutoComplete( MainSearchInput query, int maxTerms, ref int pTotalRows )
		{
			if ( UtilityManager.GetAppKeyValue( "includingAllFiltersWithAutocomplete", false ) )
			{
				List<string> autocompleteList = new List<string>();

				//test just calling the related main search and returning the terms
				AssessmentSearch( query, ref pTotalRows, ref autocompleteList, true );
				return autocompleteList;
			}
			QueryContainer lifeCycleStatusTypeQuery = null;

			//20-04-16 mparsons - set a default value for LifeCycleStatusTypeQuery to exclude Ceased. Will be overridden if any LifeCycleStatusType status are provided. might be better to have simple property of IsActive (non ceased)
			if ( UtilityManager.GetAppKeyValue( "hidingCeasedStatus", false ) )
			{
				var defStatus = CodesManager.Property_GetValues( CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, "", false );
				var defaultStatusTypeIds = defStatus.Where( s => s.Title.Contains( "Ceased" ) == false ).Select( s => s.Id ).ToList();
				lifeCycleStatusTypeQuery = Query<AssessmentIndex>.Terms( ts => ts.Field( f => f.LifeCycleStatusTypeId ).Terms( defaultStatusTypeIds.ToArray() ) );
			}


			var search = EC.Search<AssessmentIndex>( i => i
						.Index( AssessmentIndex )
							.Query( q => 
								q.MultiMatch( m => m
								   .Fields( f => f
									   .Field( ff => ff.Name )
									   .Field( ff => ff.Description )
								   )
								   //.Operator( Operator.Or )
								   .Type( TextQueryType.PhrasePrefix )
								   .Query( query.Keywords )
								   .MaxExpansions( 10 ) )
								&& lifeCycleStatusTypeQuery
							)
							.Size( maxTerms * 2 ) );

			pTotalRows = ( int ) search.Total;
			var list = ( List<AssessmentIndex> ) search.Documents;
			return list.Select( x => x.ListTitle ).Distinct().Take( maxTerms ).ToList();
		}
		/// <summary>
		/// Proxy for full search
		/// </summary>
		/// <param name="query"></param>
		/// <param name="pTotalRows"></param>
		/// <returns></returns>
		public List<AssessmentProfile> AssessmentSearch( MainSearchInput query, ref int pTotalRows )
		{
			List<string> autocompleteList = new List<string>();
			bool requestIsAutocomplete = false;
			return AssessmentSearch( query, ref pTotalRows, ref autocompleteList, requestIsAutocomplete );
		}
		public List<AssessmentProfile> AssessmentSearch( MainSearchInput query, ref int pTotalRows, ref List<string> autocompleteList, bool requestIsAutocomplete = false )
		{
			Assessment_BuildIndex();

			var list = new List<PM.AssessmentProfile>();

			QueryContainer audienceTypeQuery = null;
			QueryContainer audienceLevelTypeQuery = null;
			QueryContainer asmtMethodTypesQuery = null;
			QueryContainer asmtUseTypesQuery = null;
			QueryContainer collectionIdQuery = null;
			QueryContainer collectionsQuery = null;
			QueryContainer competenciesQuery = null;
			QueryContainer connectionsQuery = null;
			QueryContainer deliveryTypesQuery = null;
			QueryContainer subjectsQuery = null;
			QueryContainer keywordsQuery = null;
			QueryContainer scoringMethodsQuery = null;
			QueryContainer occupationsQuery = null;
			QueryContainer industriesQuery = null;
			QueryContainer classificationsQuery = null;
			//QueryContainer relationshipIdQuery = null;
			QueryContainer commonOrgRolesBYQuery = null;
			QueryContainer publishedBYQuery = null;

			QueryContainer qaFilterQuery = null;
			QueryContainer languagesQuery = null;
			QueryContainer boundariesQuery = null;
			QueryContainer lifeCycleStatusTypeQuery = null;
			QueryContainer reportsQuery = null;
			//WidgetQueryFilters widgetQuery = new WidgetQueryFilters();
			QueryContainer widgetOccupationsQuery = null;
			QueryContainer windustriesQuery = null;
			QueryContainer wsubjectsQuery = null;
			QueryContainer wLWIAQuery = null;
			HistoryQueryFilters history = new HistoryQueryFilters();
			LocationQueryFilters locationQueryFilters = new LocationQueryFilters();
			var relationshipTypeIds = new List<int>();
			var qaRelationshipTypeIds = new List<int>();
			List<int> LifeCycleStatusTypeIds = new List<int>();

			//20-04-16 mparsons - set a default value for LifeCycleStatusTypeQuery to exclude Ceased. Will be overridden if any LifeCycleStatusType status are provided. might be better to have simple property of IsActive (non ceased)
			if ( UtilityManager.GetAppKeyValue( "hidingCeasedStatus", false ) )
			{
				var defStatus = CodesManager.Property_GetValues( CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, "", false );
				var defaultStatusTypeIds = defStatus.Where( s => s.Title.Contains( "Ceased" ) == false ).Select( s => s.Id ).ToList();
				lifeCycleStatusTypeQuery = Query<AssessmentIndex>.Terms( ts => ts.Field( f => f.LifeCycleStatusTypeId ).Terms( defaultStatusTypeIds.ToArray() ) );
			}
			#region Handle Location queries
			bool usingRegionHack = false;
			LocationFilter<AssessmentIndex>( query, locationQueryFilters, ref usingRegionHack );
			//custom filter for Illinois LWIAs
			if ( query.FiltersV2.Count > 0 )
			{
				wLWIAQuery = LocationSubregionFilter<AssessmentIndex>( query );
			}
			#endregion

			#region Competencies
			competenciesQuery = CommonCompetencies<AssessmentIndex>( query );

			#endregion

			#region Subject Areas.keywords
			//from widget and from search filters
			if ( query.FiltersV2.Any( x => x.Name == "subjects" ) )
			{
				subjectsQuery = HandleSubjects<AssessmentIndex>( query );
			}
			//keywords from widget
			if ( query.FiltersV2.Any( x => x.Name == "keywords" ) )
			{
				keywordsQuery = HandleWidgetKeywords<AssessmentIndex>( query );
			}
			#endregion

			#region Properties

			if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CODE ) )
			{
				//string searchCategories = UtilityManager.GetAppKeyValue( "asmtSearchCategories", "21,37," );
				//var categoryIds = new List<int>();
				//foreach ( var s in searchCategories.Split( ',' ) )
				//	if ( !string.IsNullOrEmpty( s ) )
				//		categoryIds.Add( int.Parse( s ) );

				var asmtMethodsIds = new List<int>();
				var asmtUseIds = new List<int>();
				var reportIds = new List<int>();
				var deliveryTypeIds = new List<int>();
				var scoringMethodIds = new List<int>();

				var validConnections = new List<string>();
				var connectionFilters = new List<string>();
				var audienceTypeIds = new List<int>();
				var audienceLevelTypeIds = new List<int>();

				if ( query.FiltersV2.Any( x => x.AsCodeItem().CategoryId == CodesManager.PROPERTY_CATEGORY_CONNECTION_PROFILE_TYPE ) )
				{
					//this will include is part/has part
					Enumeration entity = CodesManager.GetCredentialsConditionProfileTypes();
					validConnections = entity.Items.Select( s => s.SchemaName.ToLower() ).ToList();
				}

				foreach ( var filter in query.FiltersV2.Where( m => m.Type == MainSearchFilterV2Types.CODE ).ToList() )
				{
					var item = filter.AsCodeItem();
					if ( filter.Name == "reports" || filter.Name == "otherfilters" || filter.Name == "filter:OtherFilters" )
					{
						reportIds.Add( item.Id ); //can probably continue after here?
						continue;
					}
					else if ( filter.Name == "" && filter.Values != null && filter.Values.Count == 3 )
					{
						//HACK test while determine why no filter.name
						//var filterItem = filter.AsDefaultCodeItem();
						if ( item.CategoryId == 1 || item.CategoryId == 58 )
						{
							reportIds.Add( item.Id ); //can probably continue after here?
							continue;
						}
					}
					//else if ( item.Name == "" && filter.Values != null && item.SchemaName.IndexOf( "asmtReport" ) == 0 )
					//{
					//	//we have a report from the non-Other filters section Do a lookup
					//	var propertyId = 0;
					//	if ( ElasticManager.GetPropertyId( 60, item.SchemaName, ref propertyId ) )
					//	{
					//		reportIds.Add( propertyId );
					//		continue;
					//	}
					//}
					if ( filter.Name == "history" )
						continue;
					if ( item == null || item.CategoryId < 1 )
					{
						//check for text only 
						var text = filter.AsText();
						if ( !string.IsNullOrWhiteSpace( text ) )
						{
							//what? if no keywords, then append. Or append?
							if ( string.IsNullOrWhiteSpace( query.Keywords ) )
								query.Keywords = text;
							else
								query.Keywords += " " + text;
						}
						continue;
					}
					//if ( categoryIds.Contains( item.CategoryId ) ) propertyValueIds.Add( item.Id );
					if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_Assessment_Method_Type )
						asmtMethodsIds.Add( item.Id );
					else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_ASSESSMENT_USE_TYPE )
						asmtUseIds.Add( item.Id );
					else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE )
						deliveryTypeIds.Add( item.Id );
					else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_Scoring_Method )
						scoringMethodIds.Add( item.Id );
					else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE )
					{
						if ( filter.Name == "qualityassurance" || filter.Name == "filter:QAReceived" )
							qaRelationshipTypeIds.Add( item.Id );
						else
							relationshipTypeIds.Add( item.Id );
					}
					else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL )
						audienceLevelTypeIds.Add( item.Id );
					else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE )
						audienceTypeIds.Add( item.Id );
					else if ( item.CategoryId == 15 )
					{
						if ( validConnections.Contains( item.SchemaName.ToLower() ) )
							connectionFilters.Add( item.SchemaName.Replace( "ceterms:", "" ) );
					}
					else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS )
						LifeCycleStatusTypeIds.Add( item.Id );
				}
				//
				CommonHistoryFilter<AssessmentIndex>( query, ref history );

				if ( asmtMethodsIds.Any() )
					asmtMethodTypesQuery = Query<AssessmentIndex>.Terms( ts => ts.Field( f => f.AssessmentMethodTypeIds ).Terms<int>( asmtMethodsIds.ToArray() ) );
				if ( asmtUseIds.Any() )
					asmtUseTypesQuery = Query<AssessmentIndex>.Terms( ts => ts.Field( f => f.AssessmentUseTypeIds ).Terms<int>( asmtUseIds.ToArray() ) );
				if ( deliveryTypeIds.Any() )
					deliveryTypesQuery = Query<AssessmentIndex>.Terms( ts => ts.Field( f => f.DeliveryMethodTypeIds ).Terms<int>( deliveryTypeIds.ToArray() ) );
				if ( scoringMethodIds.Any() )
					scoringMethodsQuery = Query<AssessmentIndex>.Terms( ts => ts.Field( f => f.ScoringMethodTypeIds ).Terms<int>( scoringMethodIds.ToArray() ) );
				if ( audienceTypeIds.Any() )
					audienceTypeQuery = Query<AssessmentIndex>.Terms( ts => ts.Field( f => f.AudienceTypeIds ).Terms<int>( audienceTypeIds ) );
				if ( audienceLevelTypeIds.Any() )
					audienceLevelTypeQuery = Query<AssessmentIndex>.Terms( ts => ts.Field( f => f.AudienceLevelTypeIds ).Terms<int>( audienceLevelTypeIds ) );
				if ( LifeCycleStatusTypeIds.Any() )
					lifeCycleStatusTypeQuery = Query<AssessmentIndex>.Terms( ts => ts.Field( f => f.LifeCycleStatusTypeId ).Terms( LifeCycleStatusTypeIds.ToArray() ) );
				//
				if ( relationshipTypeIds.Any() )
				{
					if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.TEXT ) && query.FiltersV2.Where( m => m.Name == "qualityassurance" ).Count() > 0 )
					{
						//save relationships for use with the QA orgs
					}
					else
					{
						//relationshipIdQuery = Query<AssessmentIndex>.Nested( n => n.Path( p => p.AgentRelationshipsForEntity )
						//		.Query( q =>
						//			q.Terms( t =>
						//				t.Field( f => f.AgentRelationshipsForEntity.First().RelationshipTypeIds )
						//					.Terms<int>( relationshipTypeIds.ToArray() ) ) )
						//				);
					}
				}

				if ( reportIds.Any() )
				{
					reportsQuery = Query<AssessmentIndex>.Terms( ts => ts.Field( f => f.ReportFilters ).Terms<int>( reportIds.ToArray() ) );
				}

				if ( connectionFilters.Any() )
				{
					connectionFilters.ForEach( x =>
					{
						if ( x == "requires" )
							connectionsQuery |= Query<AssessmentIndex>.Range( r => r.Field( f => f.RequiresCount ).GreaterThan( 0 ) );
						if ( x == "recommends" )
							connectionsQuery |= Query<AssessmentIndex>.Range( r => r.Field( f => f.RecommendsCount ).GreaterThan( 0 ) );
						if ( x == "isRequiredFor" )
							connectionsQuery |= Query<AssessmentIndex>.Range( r => r.Field( f => f.IsRequiredForCount ).GreaterThan( 0 ) );
						if ( x == "isRecommendedFor" )
							connectionsQuery |= Query<AssessmentIndex>.Range( r => r.Field( f => f.IsRecommendedForCount ).GreaterThan( 0 ) );
						if ( x == "isAdvancedStandingFor" )
							connectionsQuery |= Query<AssessmentIndex>.Range( r => r.Field( f => f.IsAdvancedStandingForCount ).GreaterThan( 0 ) );
						if ( x == "advancedStandingFrom" )
							connectionsQuery |= Query<AssessmentIndex>.Range( r => r.Field( f => f.AdvancedStandingFromCount ).GreaterThan( 0 ) );
						if ( x == "isPreparationFor" )
							connectionsQuery |= Query<AssessmentIndex>.Range( r => r.Field( f => f.IsPreparationForCount ).GreaterThan( 0 ) );
						if ( x == "isPreparationFrom" )
							connectionsQuery |= Query<AssessmentIndex>.Range( r => r.Field( f => f.PreparationFromCount ).GreaterThan( 0 ) );
					} );
				}
			}

			#endregion

			#region QualityAssurance
			//NOTE: this is only referenced after clicking on a gray box, not from the search page
			//		==> actually now used by search widget => using organizationroles
			commonOrgRolesBYQuery = CommonOrgRolesByFilter<AssessmentIndex>( query );
			publishedBYQuery = PublishedByFilter<AssessmentIndex>( query );

			//USED BY QUALITY ASSURANCE FILTER check boxes and org list
			qaFilterQuery = CommonQualityAssuranceFilter<AssessmentIndex>( query, qaRelationshipTypeIds );

			var commonOrgQARolesQuery = CommonNonQAOrgRolesFilter<AssessmentIndex>( query, relationshipTypeIds );
			#endregion

			#region Assessment Ids list
			QueryContainer recordIdListQuery = null;
			if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CUSTOM ) )
			{
				var idsList = new List<int>();
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "potentialresults" ).ToList() )
				{
					idsList.AddRange( JsonConvert.DeserializeObject<List<int>>( filter.CustomJSON ) );
				}

				if ( idsList.Any() )
				{
					idsList.ForEach( x =>
					{
						recordIdListQuery |= Query<AssessmentIndex>.Terms( ts => ts.Field( f => f.Id ).Terms( x ) );
					} );
				}
			}
			#endregion
			//collections
			if ( query.CollectionId > 0 )       //???
			{
				collectionIdQuery = Query<AssessmentIndex>.Terms( ts => ts.Field( f => f.ResourceForCollection ).Terms( query.CollectionId ) );
			}
			collectionsQuery = CommonInCollectionByIdFilters<AssessmentIndex>( query );
			if ( collectionsQuery == null )
				collectionsQuery = CommonPartOfCollectionFilters<AssessmentIndex>( query );

			#region Occupations, Industries, Programs
			occupationsQuery = CommonOccupations<AssessmentIndex>( query, ref widgetOccupationsQuery );

			industriesQuery = CommonIndustries<AssessmentIndex>( query, ref windustriesQuery );

			classificationsQuery = CommonCip<AssessmentIndex>( query );
			#endregion

			#region Languages
			var languageFilters = new List<string>();
			if ( query.FiltersV2.Any( x => x.Name == "languages" ) )
			{
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "languages" ) )
				{
					var text = filter.GetValueOrDefault( "CodeText", "" );
					if ( string.IsNullOrWhiteSpace( text ) )
						continue;
					languageFilters.Add( text.ToLower() );
				}
				languageFilters.ForEach( x =>
				{
					languagesQuery = Query<AssessmentIndex>.Terms( ts => ts.Field( f => f.InLanguage ).Terms( x ) );
				} );
			}
			#endregion

			#region Boundaries

			boundariesQuery = HandleLocationQuery<CredentialIndex>( query );
			/*
			var boundaries = SearchServices.GetBoundaries( query, "bounds" );
			if ( boundaries.IsDefined )
			{

				boundariesQuery = Query<AssessmentIndex>
					.Nested( n => n.Path( p => p.Addresses )
					.Query( q => Query<AssessmentIndex>.Range( r => r.Field( f => f.Addresses.First().Longitude ).LessThan( ( double ) boundaries.East ).GreaterThan( ( double ) boundaries.West ) ) && Query<AssessmentIndex>.Range( r => r.Field( f => f.Addresses.First().Latitude ).LessThan( ( double ) boundaries.North ).GreaterThan( ( double ) boundaries.South ) ) ).IgnoreUnmapped() );
			}
			*/
			#endregion

			#region Query

			var sort = new SortDescriptor<AssessmentIndex>();

			var sortOrder = query.SortOrder;
			if ( sortOrder == "alpha" )
				sort.Ascending( s => s.Name.Suffix( "keyword" ) );
			else if ( sortOrder == "zalpha" )
				sort.Descending( s => s.Name.Suffix( "keyword" ) );
			else if ( sortOrder == "newest" )
				sort.Field( f => f.LastUpdated, SortOrder.Descending );
			else if ( sortOrder == "oldest" )
				sort.Field( f => f.LastUpdated, SortOrder.Ascending );
			else if ( sortOrder == "relevance" )
				sort.Descending( SortSpecialField.Score );
			//
			//
			QueryContainer phrasePrefixQuery = SetPhrasePrefixQuery<AssessmentIndex>( query );
			QueryContainer bestFieldsQuery = SetBestFieldsQuery<AssessmentIndex>( query );
			QueryContainer autoCompleteQuery = SetAutocompleteQuery<AssessmentIndex>( query );
			//QueryContainer phrasePrefixQuery = Query<AssessmentIndex>
			//	.MultiMatch( m => m
			//		.Fields( f => f
			//			.Field( ff => ff.Name, 100 )
			//			 .Field( ff => ff.ListTitle, 90 )
			//			.Field( ff => ff.Description, 55 )
			//			.Field( ff => ff.SubjectWebpage, 25 )
			//			.Field( ff => ff.OwnerOrganizationName, 80 )
			//			.Field( ff => ff.TextValues, 45 )
			//			.Field( ff => ff.Industry, 30 )
			//			.Field( ff => ff.Keyword, 50 )
			//			.Field( ff => ff.SubjectAreas, 50 ) //??
			//		)
			//	.Type( TextQueryType.PhrasePrefix )
			//	.Query( query.Keywords )
			////.MaxExpansions( 10 )
			////.Analyzer( "standard" )
			//);
			////
			//QueryContainer bestFieldsQuery = Query<AssessmentIndex>
			//	.MultiMatch( m => m
			//		.Fields( f => f
			//				   .Field( ff => ff.Name, 100 )
			//			 .Field( ff => ff.ListTitle, 90 )
			//			.Field( ff => ff.Description, 55 )
			//			.Field( ff => ff.SubjectWebpage, 25 )
			//			.Field( ff => ff.OwnerOrganizationName, 80 )
			//			.Field( ff => ff.TextValues, 45 )
			//			.Field( ff => ff.Industry, 30 )
			//			.Field( ff => ff.Keyword, 50 )
			//			.Field( ff => ff.SubjectAreas, 50 ) //??
			//				)
			//				.Type( TextQueryType.BestFields )
			//				.Query( query.Keywords )
			//);
			////
			//QueryContainer autoCompleteQuery = Query<AssessmentIndex>
			//	.MultiMatch( m => m
			//		.Fields( f => f
			//			.Field( ff => ff.Name, 100 )      //??
			//			.Field( ff => ff.Description, 30 )
			//	)
			//	.Type( TextQueryType.PhrasePrefix )
			//	.Query( query.Keywords )
			//	.MaxExpansions( 10 )
			////.Analyzer( "standard" )
			//);
			var pageSize = query.PageSize;
			if ( requestIsAutocomplete )
			{
				pageSize = pageSize * 2;
				phrasePrefixQuery = null;
				bestFieldsQuery = null;
			}
			else
			{
				autoCompleteQuery = null;
			}
			if ( query.StartPage < 1 )
				query.StartPage = 1;

			var search = EC.Search<AssessmentIndex>( body => body
				   .Index( AssessmentIndex )
				   .Query( q =>
						competenciesQuery
					  //&& widgetQuery.OwningOrgsQuery  //?????
					  && recordIdListQuery
					  && subjectsQuery
					  //&& wsubjectsQuery         //widget specific!!!
					  && keywordsQuery
					  && collectionIdQuery && collectionsQuery
					  && asmtMethodTypesQuery
					  && asmtUseTypesQuery
					  && audienceLevelTypeQuery
					  && audienceTypeQuery
					  && deliveryTypesQuery
					  && scoringMethodsQuery
					  && connectionsQuery
					  && lifeCycleStatusTypeQuery
					  && occupationsQuery
					  && widgetOccupationsQuery     //widget specific!!!
					  && industriesQuery
					  && windustriesQuery       //widget specific!!!
					  && classificationsQuery
					  && commonOrgQARolesQuery
					  && qaFilterQuery && commonOrgRolesBYQuery && publishedBYQuery
					  && languagesQuery
					  && boundariesQuery
					  && locationQueryFilters.RegionQuery
					  && locationQueryFilters.CountryQuery
					  && locationQueryFilters.CityQuery
					 && history.CreatedFromQuery && history.CreatedToQuery && history.HistoryFromQuery && history.HistoryToQuery
					  //&& widgetQuery.KeywordQuery
					  && reportsQuery
					  && ( phrasePrefixQuery || autoCompleteQuery || bestFieldsQuery )
				   )
				   .TrackTotalHits( true )
				   .Sort( s => sort )
				   .From( query.StartPage > 0 ? query.StartPage - 1 : 1 )
				   .Skip( ( query.StartPage - 1 ) * query.PageSize )
				   .Size( pageSize ) );


			#endregion

			var debug = search.DebugInformation;
			pTotalRows = ( int ) search.Total;
			if ( requestIsAutocomplete )
			{
				var requestList = ( List<AssessmentIndex> ) search.Documents;
				autocompleteList = requestList.Select( x => x.ListTitle ).Distinct().Take( query.PageSize ).ToList();
				return null;
			}
			if ( query.StartPage == 1 && ( query.Filters.Count > 0 || query.FiltersV2.Count > 0 || !string.IsNullOrWhiteSpace( query.Keywords ) ) )
			{
				//actually extract the request: between #request and #response
				var elasticQuery = UtilityManager.ExtractNameValue( debug, "# Request", ":", "# Response:" );
				LoggingHelper.DoTrace( 6, string.Format( "ElasticServices.Search: {0}, Results: {1}, ElasticLog: \r\n", "Assessment", pTotalRows ) + elasticQuery, "ElasticQuery" );
			}
			if ( pTotalRows > 0 )
			{
				list = ElasticManager.Assessment_MapFromElastic( ( List<AssessmentIndex> ) search.Documents, query.StartPage, query.PageSize );

				LoggingHelper.DoTrace( 6, string.Format( "ElasticServices.AssessmentSearch. found: {0} records", pTotalRows ) );
			}
			//stats
			query.Results = pTotalRows;
			string jsoninput = JsonConvert.SerializeObject( query, JsonHelper.GetJsonSettings() );
			string searchType = "blind";
			if ( query.Filters.Count > 0 || query.FiltersV2.Count > 0 || !string.IsNullOrWhiteSpace( query.Keywords ) )
			{
				searchType = "filters selected";
			}
			if ( query.StartPage > 1 )
				searchType += " - paging";
			new ActivityServices().AddActivity( new SiteActivity()
			{ ActivityType = "AssessmentProfile", Activity = "Search", Event = searchType, Comment = jsoninput }
			);
			return list;
		}
		#endregion

		#region Learning Opportunities
		public static void LearningOpp_BuildIndex( bool deleteIndexFirst = false, bool updatingIndexRegardless = false )
		{
			try
			{
				bool indexInitialized = false;
				if ( deleteIndexFirst && EC.Indices.Exists( LearningOppIndex ).Exists )
					EC.Indices.Delete( LearningOppIndex );

				if ( !EC.Indices.Exists( LearningOppIndex ).Exists )
				{
					//if ( UtilityManager.GetAppKeyValue( "environment" ) == "development" )
					LearningOpp_InitializeIndex7();
					//else
					//	LearningOppInitializeIndex();
					indexInitialized = true;
				}

				if ( indexInitialized || updatingIndexRegardless )
				{
					LoggingHelper.DoTrace( 1, "LOPP- Building Index" );
					int minEntityStateId = UtilityManager.GetAppKeyValue( "minLoppEntityStateId", 3 );

					new ActivityServices().AddActivity( new SiteActivity()
					{ ActivityType = "LearningOpportunity", Activity = "Elastic", Event = "Build Index" }
					);
					int processed = 0;
					string filter = "( base.EntityStateId >= 2 )";
					//20-12-28 - update to be like credentials
					LearningOpp_UpdateIndex( filter, ref processed );
					if ( processed > 0 )
					{
						var refreshResults = EC.Indices.Refresh( LearningOppIndex );
						new ActivityServices().AddActivity( new SiteActivity()
						{
							ActivityType = "LearningOpportunity",
							Activity = "Elastic",
							Event = "Build Index Completed",
							Comment = string.Format( "Completed rebuild of LearningOpportunityIndex for {0} records.", processed )
						} );
					}
					else
					{
						//ISSUE
						LoggingHelper.LogError( "Build LearningOpportunityIndex: no results were returned from LearningOpportunity_SearchForElastic method." );
					}
					//var list = ElasticManager.LearningOpp_SearchForElastic( string.Format( "( base.EntityStateId >= {0} )", minEntityStateId ) );
					//if ( list.Count() > 0 )
					//{
					//	var results = EC.Bulk( b => b.IndexMany( list, ( d, document ) => d.Index( LearningOppIndex ).Document( document ).Id( document.Id.ToString() ) ) );
					//	if ( results.ToString().IndexOf( "Valid NEST response built from a successful low level cal" ) == -1 )
					//	{
					//		Console.WriteLine( results.ToString() );
					//		LoggingHelper.DoTrace( 1, " Issue building learning opportunity index: " + results );
					//	}
					//}
					//EC.Indices.Refresh( LearningOppIndex );
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "LearningOpp_BuildIndex" );
			}
		}
		public static void LearningOpp_ManageIndex( bool deleteIndexFirst = false )
		{
			if ( deleteIndexFirst && EC.Indices.Exists( LearningOppIndex ).Exists )
			{
				EC.Indices.Delete( LearningOppIndex );
			}
			if ( !EC.Indices.Exists( LearningOppIndex ).Exists )
			{
				LearningOpp_InitializeIndex7();
			}
		}
		//public static void LearningOppInitializeIndex( bool deleteIndexFirst = true )
		//{
		//	if ( !EC.Indices.Exists( LearningOppIndex ).Exists )
		//	{
		//		EC.Indices.Create( LearningOppIndex, c => new CreateIndexDescriptor( LearningOppIndex )
		//		 .Settings( st => st
		//				 .Analysis( an => an.TokenFilters( tf => tf.Stemmer( "custom_english_stemmer", ts => ts.Language( "english" ) ) ).Analyzers( anz => anz.Custom( "custom_lowercase_stemmed", cc => cc.Tokenizer( "standard" ).Filters( new List<string> { "lowercase", "custom_english_stemmer" } ) ) ) ) )
		//			.Mappings( ms => ms
		//				.Map<LearningOppIndex>( m => m
		//					.AutoMap()
		//					.Properties( p => p
		//						.Text( s => s.Name( n => n.Description ).Analyzer( "custom_lowercase_stemmed" ) )
		//						//.Text( s => s.Index( true ).Name( n => n.Name ).Fielddata( true ).Analyzer( "lowercase_analyzer" ) )
		//						.Nested<IndexCompetency>( n => n
		//							.Name( nn => nn.Competencies )  //21-08-31 chg to use general competencies
		//							.AutoMap()
		//						)
		//						.Nested<IndexReferenceFramework>( n => n
		//							.Name( nn => nn.Industries )
		//							.AutoMap()
		//						)
		//						.Nested<IndexReferenceFramework>( n => n
		//							.Name( nn => nn.Occupations )
		//							.AutoMap()
		//						)
		//						.Nested<IndexReferenceFramework>( n => n
		//							.Name( nn => nn.InstructionalPrograms )
		//							.AutoMap()
		//					   )
		//						.Nested<Models.Elastic.Address>( n => n
		//							.Name( nn => nn.Addresses )
		//							.AutoMap()
		//						) //AgentRelationshipForEntity will replace IndexQualityAssurance
		//						 .Nested<Models.Elastic.AgentRelationshipForEntity>( n => n
		//							.Name( nn => nn.AgentRelationshipsForEntity )
		//							.AutoMap()
		//						)
		//						 .Nested<Models.Common.IdentifierValue>( n => n
		//							.Name( nn => nn.RegionIdentifier )
		//							.AutoMap()
		//						)
		//					)
		//				)
		//			)
		//		);
		//	}
		//}
		public static void LearningOpp_InitializeIndex7( bool deleteIndexFirst = true )
		{
			if ( !EC.Indices.Exists( LearningOppIndex ).Exists )
			{
				EC.Indices.Create( LearningOppIndex, c => new CreateIndexDescriptor( LearningOppIndex )
				 .Settings( st => st
						 .Analysis( an => an.TokenFilters( tf => tf.Stemmer( "custom_english_stemmer", ts => ts.Language( "english" ) ) ).Analyzers( anz => anz.Custom( "custom_lowercase_stemmed", cc => cc.Tokenizer( "standard" ).Filters( new List<string> { "lowercase", "custom_english_stemmer" } ) ) ) 
						 )
						 .Setting( UpdatableIndexSettings.MaxResultWindow, 50000 )  //not sure of implications?

						 )
						//.Mappings( ms => ms
						.Map<LearningOppIndex>( m => m
							.AutoMap()
							.Properties( p => p
								.Text( s => s.Name( n => n.Description ).Analyzer( "custom_lowercase_stemmed" ) )
								//.Text( s => s.Index( true ).Name( n => n.Name ).Fielddata( true ).Analyzer( "lowercase_analyzer" ) )
								.Nested<IndexCompetency>( n => n
									.Name( nn => nn.Competencies )  //21-08-31 chg to use general competencies
									.AutoMap()
								)
								.Nested<IndexReferenceFramework>( n => n
									.Name( nn => nn.Industries )
									.AutoMap()
								)
								.Nested<IndexReferenceFramework>( n => n
									.Name( nn => nn.Occupations )
									.AutoMap()
								)
								.Nested<IndexReferenceFramework>( n => n
									.Name( nn => nn.InstructionalPrograms )
									.AutoMap()
							   )
								.Nested<Models.Elastic.Address>( n => n
									.Name( nn => nn.Addresses )
									.AutoMap()
								) //AgentRelationshipForEntity will replace IndexQualityAssurance
								 .Nested<Models.Elastic.AgentRelationshipForEntity>( n => n
									.Name( nn => nn.AgentRelationshipsForEntity )
									.AutoMap()
								)
								 .Nested<Models.Common.IdentifierValue>( n => n
									.Name( nn => nn.RegionIdentifier )
									.AutoMap()
								)
								 //override the automap inferred mapping for ResourceDetail
								 .Object<JObject>( o => o
									.Name( n => n.ResourceDetail )
									.Enabled( false )
									)

							//.Object<object>( o => o
							//.Name( n => n.ResourceDetail2 )
							//.Enabled( false ) )
							//.Object<object>( o => o
							//   .Name( n => n.ResourceDetail3 )
							//   .Enabled( false ) )
							) //Properties
						)
				//)
				);
			}
		}

		public static void LearningOpp_UpdateIndex( int recordId )
		{
			if ( recordId < 1 )
				return;
			string filter = string.Format( " ( base.Id = {0} ) ", recordId );
			int processed = 0;
			LearningOpp_UpdateIndex( filter, ref processed );

		} //


		/// <summary>
		/// Pass a filter to use for updating the index
		/// </summary>
		/// <param name="filter"></param>
		public static void LearningOpp_UpdateIndex( string filter, ref int processed )
		{
			processed = 0;

			if ( string.IsNullOrWhiteSpace( filter ) )
				return;

			int action = UtilityManager.GetAppKeyValue( "updateCredIndexAction", 0 );
			if ( action == 0 )
				return;

			//        if ( DateTime.Now.ToString("yyyy-MM-dd") == "2023-02-16" )
			//        {
			////ADHOC DELETE AND RE-INIT
			//            EC.Indices.Delete( LearningOppIndex );
			//            LearningOppInitializeIndex7();
			//        }
			string methodName = "LearningOpp_UpdateIndex";
			string IndexName = LearningOppIndex;
			int pageSize = nonCredentialPageSize;
			LoggingHelper.DoTrace( 6, $"{methodName}. Enter. Index: {IndexName} Filter: '" + filter + "' " );

			int pageNbr = 1;
			int totalRows = 0;
			bool isComplete = false;
			int cntr = 0;
			//
			var manager = new LearningOpportunityServices();
			try
			{
				while ( pageNbr > 0 && !isComplete )
				{
					var list = new List<LearningOppIndex>();
					//

					list = ElasticManager.LearningOpp_SearchForElastic( filter, pageSize, pageNbr, ref totalRows );
					if ( list != null && list.Count > 0 )
					{
						processed += list.Count;
						if ( action == 1 )
						{
							foreach ( var item in list )
							{
								cntr++;
								var res = EC.Index( item, idx => idx.Index( IndexName ) );
								Console.WriteLine( res.Result );
							}
						}
						else if ( action == 2 )
						{
							cntr = cntr + list.Count;
							var results = EC.Bulk( b => b.IndexMany( list, ( d, record ) => d.Index( IndexName ).Document( record ).Id( record.Id.ToString() ) ) );
							if ( results.ToString().IndexOf( "Valid NEST response built from a successful" ) == -1 )
							{
								Console.WriteLine( results.ToString() );
								LoggingHelper.DoTrace( 1, string.Format( " Issue building {0}: ", IndexName ) + results.DebugInformation.Substring( 0, 2000 ) );
							}
						}
					}
					else
					{
						//LoggingHelper.DoTrace( 2, string.Format( "LearningOpp_UpdateIndex failed, no data returned for filter: {0}", filter ) );
						if ( list != null && list.Count == 1 && totalRows == -1 )
						{
							LoggingHelper.LogError( string.Format( "{0}: Error in search. {1}", methodName, list[0].Description ) );

						}
						else if ( pageNbr == 1 )
						{
							if ( string.IsNullOrWhiteSpace( filter ) )
							{
								LoggingHelper.LogError(string.Format("{0}: entered with no filter, but no results were returned from search.", methodName));
							}
							LoggingHelper.DoTrace( 2, string.Format( "{0}: NOTE no data returned for filter: {1}", methodName, filter ) );
						}
						isComplete = true;
						break;
					}
					LoggingHelper.DoTrace( 4, string.Format( "{0}. Page: {1}, Records Indexed: {2} of {3}", methodName, pageNbr, processed, totalRows ) );
					pageNbr++;
					if ( cntr >= totalRows )
					{
						isComplete = true;
					}
				} //loop
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "{0} failed for filter: {1}", methodName, filter ) );
			}
		}

		public static List<PM.LearningOpportunityProfile> LearningOppSimpleSearch( MainSearchInput query, ref int pTotalRows, string searchType = "learningopportunity" )
		{
			var list = new List<PM.LearningOpportunityProfile>();
			if ( query.FiltersV2.Any( x => x.Name == "keywords" ) )
			{

				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "keywords" ) )
				{
					var text = filter.AsText();
					if ( string.IsNullOrWhiteSpace( text ) )
						continue;
					//this doesn't work:
					//temp
					query.Keywords += " " + text;

				}

			}
			//here we want to exclude a record if already tagged in widget
			//only do this if called from widget.Configure potential results
			QueryContainer widgetIdQuery = null;
			if ( query.WidgetId > 0 )
			{
				if ( query.MustHaveWidget )
				{
					widgetIdQuery = Query<LearningOppIndex>.Terms( ts => ts.Field( f => f.ResourceForWidget ).Terms( query.WidgetId ) );
				}
				else if ( query.MustNotHaveWidget )
				{
					widgetIdQuery = Query<LearningOppIndex>.Bool( b => b
							 .MustNot( mn => mn
								 .Terms( ts => ts.Field( f => f.ResourceForWidget ).Terms( query.WidgetId ) )
								 )
							);
				}
			}
			var sort = new SortDescriptor<LearningOppIndex>();

			var sortOrder = query.SortOrder;
			if ( sortOrder == "alpha" )
				sort.Ascending( s => s.Name.Suffix( "keyword" ) );
			else if ( sortOrder == "zalpha" )
				sort.Descending( s => s.Name.Suffix( "keyword" ) );
			else if ( sortOrder == "newest" )
				sort.Field( f => f.LastUpdated, SortOrder.Descending );
			else if ( sortOrder == "oldest" )
				sort.Field( f => f.LastUpdated, SortOrder.Ascending );
			else if ( sortOrder == "relevance" )
				sort.Descending( SortSpecialField.Score );
			else
				sort.Ascending( s => s.Name );
			if ( query.StartPage < 1 )
				query.StartPage = 1;
			var search = EC.Search<LearningOppIndex>( i => i.Index( LearningOppIndex ).Query( q =>
				 q.MultiMatch( m => m
						.Fields( f => f
								  .Field( ff => ff.Name, 90 )
								  .Field( ff => ff.PrimaryOrganizationName, 80 )
								  .Field( ff => ff.SubjectWebpage, 60 )
								  .Field( ff => ff.Description, 45 )
								  .Field( ff => ff.Keyword, 60 )
								  .Field( ff => ff.SubjectAreas, 60 )
						   )
				 .Type( TextQueryType.PhrasePrefix )
				 .Query( query.Keywords )
				 .MaxExpansions( 10 ) ) )
				 .TrackTotalHits( true )
				 .Sort( s => sort )
					 //.From( query.StartPage - 1 )
					 .From( query.StartPage > 0 ? query.StartPage - 1 : 1 )
					 .Skip( ( query.StartPage - 1 ) * query.PageSize )
					 .Size( query.PageSize ) );

			pTotalRows = ( int ) search.Total;

			if ( pTotalRows > 0 )
			{
				//map results
				list = ElasticManager.LearningOpp_MapFromElastic( ( List<LearningOppIndex> ) search.Documents, query.StartPage, query.PageSize );

				LoggingHelper.DoTrace( 6, string.Format( "ElasticServices.OrganizationSearch. found: {0} records", pTotalRows ) );
			}
			//stats
			query.Results = pTotalRows;
			return list;
		}
		//need to include the filters related to a widget here
		public List<string> LearningOppAutoComplete( MainSearchInput query, int maxTerms, ref int pTotalRows )
		{
			if ( UtilityManager.GetAppKeyValue( "includingAllFiltersWithAutocomplete", false ) )
			{
				List<string> autocompleteList = new List<string>();

				//test just calling the related main search and returning the terms
				LearningOppSearch( query, ref pTotalRows, ref autocompleteList, true );
				return autocompleteList;
			}

			//new need to call a method to get all applicable query containers. Or just start with the widget org relationship
			QueryContainer commonOrgQARolesQuery = null;
			QueryContainer wLWIAQuery = null;
			QueryContainer entityStateQuery = null;
			QueryContainer lifeCycleStatusTypeQuery = null;

			List<int> targetOrgIds = new List<int>();
			var relationshipTypeIds = GetAnyRelationships( query, ref targetOrgIds );
			if ( relationshipTypeIds?.Count > 0 ) //not working
				commonOrgQARolesQuery = CommonOrgRolesByFilter<LearningOppIndex>( query );

			if ( UtilityManager.GetAppKeyValue( "usingEntityStateIdBoosting", false ) )
			{
				entityStateQuery = Query<LearningOppIndex>
						.Boosting( bq => bq
							.Positive( pq => pq
								.Terms( ts => ts
									.Field( f => f.EntityStateId ).Terms<int>( 3 ) )
								)
							.Negative( nq => nq
								.Terms( ts => ts
									.Field( f => f.EntityStateId ).Terms<int>( 2 ) )
								)
							.NegativeBoost( negativeBooster )
							);
			}
			//
			if ( UtilityManager.GetAppKeyValue( "hidingCeasedStatus", false ) )
			{
				var defStatus = CodesManager.Property_GetValues( CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, "", false );
				var exists = defStatus.Where( s => s.Title.Contains( "Ceased" ) == false ).ToList();
				if ( exists != null && exists.Count > 0 )
				{
					//var defaultStatusTypeIds = defStatus.Where( s => s.Title.Contains( "Ceased" ) == false ).Select( s => s.Id ).ToList();
					var defaultStatusTypeIds = exists.Select( s => s.Id ).ToList();
					lifeCycleStatusTypeQuery = Query<OrganizationIndex>.Terms( ts => ts.Field( f => f.LifeCycleStatusTypeId ).Terms( defaultStatusTypeIds.ToArray() ) );
				}
			}
			wLWIAQuery = LocationSubregionFilter<LearningOppIndex>( query );
			var search = EC.Search<LearningOppIndex>( i => i
				.Index( LearningOppIndex )
				.Query( q =>
					q.MultiMatch( m => m
					  .Fields( f => f
						  .Field( ff => ff.Name )
						  .Field( ff => ff.Description )
						   )
					   //.Operator( Operator.Or )
					   .Type( TextQueryType.PhrasePrefix )
					   .Query( query.Keywords )
					   .MaxExpansions( 10 )
					 )
					&& commonOrgQARolesQuery
					&& lifeCycleStatusTypeQuery
					&& entityStateQuery
					&& wLWIAQuery
				)
				.Size( maxTerms * 2 ) );

			var debug = search.DebugInformation;
			pTotalRows = ( int ) search.Total;
			var list = ( List<LearningOppIndex> ) search.Documents;
			return list.Select( x => x.ListTitle ).Distinct().Take( maxTerms ).ToList();

		}

		public List<string> LearningOppAutoCompleteOld( string keyword, int maxTerms, ref int pTotalRows )
		{
			var search = EC.Search<LearningOppIndex>( i => i.Index( LearningOppIndex ).Query( q => q.MultiMatch( m => m
			  .Fields( f => f
			  .Field( ff => ff.Name )
			  .Field( ff => ff.Description )
			   )
			   //.Operator( Operator.Or )
			   .Type( TextQueryType.PhrasePrefix )
			   .Query( keyword )
			   .MaxExpansions( 10 ) ) ).Size( maxTerms * 2 ) );

			pTotalRows = ( int ) search.Total;
			var list = ( List<LearningOppIndex> ) search.Documents;
			return list.Select( x => x.ListTitle ).Distinct().Take( maxTerms ).ToList();
		}

		//in progress: use to get by Id to check elastic content, or in elastic
		public List<string> LearningOpp_Get( int id, int maxTerms, ref int pTotalRows )
		{
			//or do a range of numbers, or a get all and order by Id?
			 var query = Query<LearningOppIndex>.Match( ts => ts.Field( f => f.Id ).Query( id.ToString() ) );
			var query2 = Query<LearningOppIndex>.Match( ts => ts.Field( f => f.Id ).Query( id.ToString() ) );
			var search = EC.Search<LearningOppIndex>( i => i.Index( LearningOppIndex )
				.Query( q => query )
			   );

			pTotalRows = ( int ) search.Total;
			var list = ( List<LearningOppIndex> ) search.Documents;
			return list.Select( x => x.ListTitle ).Distinct().Take( maxTerms ).ToList();
		}
		/// <summary>
		/// Proxy for full search
		/// </summary>
		/// <param name="query"></param>
		/// <param name="pTotalRows"></param>
		/// <returns></returns>
		public List<PM.LearningOpportunityProfile> LearningOppSearch( MainSearchInput query, ref int pTotalRows )
		{
			List<string> autocompleteList = new List<string>();
			bool requestIsAutocomplete = false;
			return LearningOppSearch( query, ref pTotalRows, ref autocompleteList, requestIsAutocomplete );
		}

		public List<PM.LearningOpportunityProfile> LearningOppSearch( MainSearchInput query, ref int pTotalRows, ref List<string> autocompleteList, bool requestIsAutocomplete = false )
		{
			LearningOpp_BuildIndex();

			var list = new List<PM.LearningOpportunityProfile>();


			QueryContainer competenciesQuery = null;
			QueryContainer learningObjectTypeQuery = null;
			QueryContainer widgetIdQuery = null;
			QueryContainer wLWIAQuery = null;
			QueryContainer widgetOccupationsQuery = null;
			QueryContainer windustriesQuery = null;
			QueryContainer wsubjectsQuery = null;


			QueryContainer subjectsQuery = null;
			QueryContainer keywordsQuery = null;
			QueryContainer audienceTypeQuery = null;
			QueryContainer audienceLevelTypeQuery = null;
			QueryContainer collectionIdQuery = null;
			QueryContainer collectionsQuery = null;
			QueryContainer connectionsQuery = null;
			QueryContainer methodTypesQuery = null;
			QueryContainer deliveryTypesQuery = null;
			QueryContainer occupationsQuery = null;
			QueryContainer industriesQuery = null;
			QueryContainer classificationsQuery = null;
			//QueryContainer relationshipIdQuery = null;
			QueryContainer qaFilterQuery = null;
			QueryContainer commonOrgRolesBYQuery = null;
			QueryContainer publishedBYQuery = null;

			QueryContainer languagesQuery = null;
			QueryContainer LifeCycleStatusTypeQuery = null;
			QueryContainer boundariesQuery = null;
			QueryContainer reportsQuery = null;
			HistoryQueryFilters history = new HistoryQueryFilters();
			LocationQueryFilters locationQueryFilters = new LocationQueryFilters();
			QueryContainer entityStateQuery = null;

			var relationshipTypeIds = new List<int>();
			var qaRelationshipTypeIds = new List<int>();
			List<int> lifeCycleStatusTypeIds = new List<int>();

			//22-07-07 mparsons - set a default value for LifeCycleStatusTypeQuery to exclude deprecated. Will be overridden if any LifeCycleStatusType status are provided. might be better to have simple property of IsActive (non ceased)
			//					- however, this will result in hiding the references. Need to set references to an active status!
			//		ALSO		- for filters of owned by, need to also remove this filter
			if ( UtilityManager.GetAppKeyValue( "hidingCeasedStatus", false ) )
			{
				var defStatus = CodesManager.Property_GetValues( CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, "", false );
				var exists = defStatus.Where( s => s.Title.Contains( "Ceased" ) == false ).ToList();
				if ( exists != null && exists.Count > 0 )
				{
					//var defaultStatusTypeIds = defStatus.Where( s => s.Title.Contains( "Ceased" ) == false ).Select( s => s.Id ).ToList();
					var defaultStatusTypeIds = exists.Select( s => s.Id ).ToList();
					LifeCycleStatusTypeQuery = Query<LearningOppIndex>.Terms( ts => ts.Field( f => f.LifeCycleStatusTypeId ).Terms( defaultStatusTypeIds.ToArray() ) );
				}
			}
			if ( UtilityManager.GetAppKeyValue( "usingEntityStateIdBoosting", false ) )
			{
				if ( query.HasFilters )
				{
					//TODO: filter to watch for "Is A nnn Reference" (and maybe "Is a Registered nnn")
					if ( query.FiltersV2.Any( filter => filter.Name == "reports" ) )
					{
						//skip for now
					}
					else
					{
						//new - only show  top level objects if no keywords (what others to consider)
						if ( query.HasKeywords )
						{
							//entityStateQuery = null;
							//or see how to use boosting
							//back to a TODO - skipping for now
							/*
							entityStateQuery = Query<LearningOppIndex>
								.Boosting( bq => bq
									.Positive( pq => pq
										.Terms( ts => ts
											.Field( f => f.EntityStateId ).Terms<int>( 3 ) )
										)
									.Negative( nq => nq
										.Terms( ts => ts
											.Field( f => f.EntityStateId ).Terms<int>( 2 ) )
										)
									.NegativeBoost( negativeBooster ) //what if skipped?
									);
							*/
						}
						else
						{
							entityStateQuery = Query<LearningOppIndex>.Terms( ts => ts.Field( f => f.EntityStateId ).Terms<int>( 3 ) );
						}
					}
				}
				else
				{
					entityStateQuery = Query<LearningOppIndex>.Terms( ts => ts.Field( f => f.EntityStateId ).Terms<int>( 3 ) );
				}
			}
			#region Handle Location queries
			bool usingRegionHack = false;
			LocationFilter<LearningOppIndex>( query, locationQueryFilters, ref usingRegionHack );
			//custom filter for Illinois LWIAs
			if ( query.FiltersV2.Count > 0 )
			{
				wLWIAQuery = LocationSubregionFilter<LearningOppIndex>( query );
			}
			#endregion


			#region Competencies
			competenciesQuery = CommonCompetencies<LearningOppIndex>( query );

			#endregion

			#region Subject Areas
			subjectsQuery = HandleSubjects<LearningOppIndex>( query );

			//keywords from widget
			if ( query.FiltersV2.Any( x => x.Name == "keywords" ) )
			{
				keywordsQuery = HandleWidgetKeywords<LearningOppIndex>( query );
			}
			#endregion

			#region Properties, Connections

			if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CODE ) )
			{
				//string searchCategories = UtilityManager.GetAppKeyValue( "loppSearchCategories", "21,37," );
				//var categoryIds = new List<int>();
				//foreach ( var s in searchCategories.Split( ',' ) )
				//	if ( !string.IsNullOrEmpty( s ) )
				//		categoryIds.Add( int.Parse( s ) );

				var learningMethodTypesIds = new List<int>();
				var learningObjectTypesIds = new List<int>();
				var deliveryTypeIds = new List<int>();
				var audienceTypeIds = new List<int>();
				var audienceLevelTypeIds = new List<int>();

				var reportIds = new List<int>();
				var validConnections = new List<string>();
				var connectionFilters = new List<string>();

				if ( query.FiltersV2.Any( x => x.AsCodeItem().CategoryId == CodesManager.PROPERTY_CATEGORY_CONNECTION_PROFILE_TYPE ) )
				{
					//this will include is part/has part
					Enumeration entity = CodesManager.GetCredentialsConditionProfileTypes();
					validConnections = entity.Items.Select( s => s.SchemaName.ToLower() ).ToList();
				}

				foreach ( var filter in query.FiltersV2.Where( m => m.Type == MainSearchFilterV2Types.CODE ).ToList() )
				{
					var item = filter.AsCodeItem();
					if ( filter.Name == "reports" || filter.Name == "otherfilters" || filter.Name == "filter:OtherFilters" )
					{
						reportIds.Add( item.Id ); //can probably continue after here?
						continue;
					}
					else if ( filter.Name == "" && filter.Values != null && filter.Values.Count == 3 )
					{
						//HACK test while determine why no filter.name
						//var filterItem = filter.AsDefaultCodeItem();
						if ( item.CategoryId == 1 || item.CategoryId == 58 )
						{
							reportIds.Add( item.Id ); //can probably continue after here?
							continue;
						}
					}
					//else if ( item.Name == "" && filter.Values != null && item.SchemaName.IndexOf( "loppReport" ) == 0 )
					//{
					//	//we have a report from the non-Other filters section Do a lookup
					//	var propertyId = 0;
					//	if ( ElasticManager.GetPropertyId( 61, item.SchemaName, ref propertyId ) )
					//	{
					//		reportIds.Add( propertyId );
					//		continue;
					//	}
					//}

					if ( filter.Name == "history" )
						continue;
					if ( item == null || item.CategoryId < 1 )
					{
						//check for text only 
						var text = filter.AsText();
						if ( !string.IsNullOrWhiteSpace( text ) )
						{
							//what? if no keywords, then append. Or append?
							if ( string.IsNullOrWhiteSpace( query.Keywords ) )
								query.Keywords = text;
							else
								query.Keywords += " " + text;
						}
						continue;
					}
					if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_Learning_Method_Type )
						learningMethodTypesIds.Add( item.Id );
					//this doesn't seem to be used? In Codes.PropertyCategory, but no data in Codes.PropertyValue??
					//is this meant to be for lopp, course, learning program?
					else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_LEARNING_OBJECT_TYPE )
						learningObjectTypesIds.Add( item.Id );
					else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE )
						deliveryTypeIds.Add( item.Id );
					else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL )
						audienceLevelTypeIds.Add( item.Id );
					else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE )
						audienceTypeIds.Add( item.Id );
					else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE )
					{
						if ( filter.Name == "qualityassurance" || filter.Name == "filter:QAReceived" )
							qaRelationshipTypeIds.Add( item.Id );
						else
							relationshipTypeIds.Add( item.Id );
					}
					//else if ( item.CategoryId == 13 )
					//	relationshipTypeIds.Add( item.Id );
					else if ( item.CategoryId == 15 )
					{
						if ( validConnections.Contains( item.SchemaName.ToLower() ) )
							connectionFilters.Add( item.SchemaName.Replace( "ceterms:", "" ) );
					}
					else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS )
						lifeCycleStatusTypeIds.Add( item.Id );
				}
				//
				CommonHistoryFilter<LearningOppIndex>( query, ref history );

				if ( learningMethodTypesIds.Any() )
				{
					methodTypesQuery = Query<LearningOppIndex>.Terms( ts => ts.Field( f => f.LearningMethodTypeIds ).Terms<int>( learningMethodTypesIds.ToArray() ) );
				}

				if ( deliveryTypeIds.Any() )
				{
					deliveryTypesQuery = Query<LearningOppIndex>.Terms( ts => ts.Field( f => f.DeliveryMethodTypeIds ).Terms<int>( deliveryTypeIds.ToArray() ) );
				}

				if ( learningObjectTypesIds.Any() )
					learningObjectTypeQuery = Query<LearningOppIndex>.Terms( ts => ts.Field( f => f.EntityTypeId ).Terms( learningObjectTypesIds.ToArray() ) );

				if ( lifeCycleStatusTypeIds.Any() )
					LifeCycleStatusTypeQuery = Query<LearningOppIndex>.Terms( ts => ts.Field( f => f.LifeCycleStatusTypeId ).Terms( lifeCycleStatusTypeIds.ToArray() ) );
				if ( relationshipTypeIds.Any() )
				{
					//relationshipIdQuery = Query<LearningOppIndex>.Nested( n => n.Path( p => p.AgentRelationshipsForEntity ).Query( q => q.Terms( t => t.Field( f => f.AgentRelationshipsForEntity.First().RelationshipTypeIds ).Terms<int>( relationshipTypeIds.ToArray() ) ) ) );
					if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.TEXT ) && query.FiltersV2.Where( m => m.Name == "qualityassurance" ).Count() > 0 )
					{
						//save relationships for use with the QA orgs
					}
					else
					{
						//relationshipIdQuery = Query<LearningOppIndex>.Nested( n => n.Path( p => p.AgentRelationshipsForEntity )
						//		.Query( q =>
						//			q.Terms( t =>
						//				t.Field( f => f.AgentRelationshipsForEntity.First().RelationshipTypeIds )
						//					.Terms<int>( relationshipTypeIds.ToArray() ) ) )
						//				);
					}
				}

				if ( audienceTypeIds.Any() )
					audienceTypeQuery = Query<LearningOppIndex>.Terms( ts => ts.Field( f => f.AudienceTypeIds ).Terms<int>( audienceTypeIds ) );

				if ( audienceLevelTypeIds.Any() )
					audienceLevelTypeQuery = Query<LearningOppIndex>.Terms( ts => ts.Field( f => f.AudienceLevelTypeIds ).Terms<int>( audienceLevelTypeIds ) );

				if ( reportIds.Any() )
				{
					reportsQuery = Query<LearningOppIndex>.Terms( ts => ts.Field( f => f.ReportFilters ).Terms<int>( reportIds.ToArray() ) );
				}

				if ( connectionFilters.Any() )
				{
					connectionFilters.ForEach( x =>
					{
						if ( x == "requires" )
							connectionsQuery |= Query<LearningOppIndex>.Range( r => r.Field( f => f.RequiresCount ).GreaterThan( 0 ) );
						if ( x == "recommends" )
							connectionsQuery |= Query<LearningOppIndex>.Range( r => r.Field( f => f.RecommendsCount ).GreaterThan( 0 ) );
						if ( x == "isRequiredFor" )
							connectionsQuery |= Query<LearningOppIndex>.Range( r => r.Field( f => f.IsRequiredForCount ).GreaterThan( 0 ) );
						if ( x == "isRecommendedFor" )
							connectionsQuery |= Query<LearningOppIndex>.Range( r => r.Field( f => f.IsRecommendedForCount ).GreaterThan( 0 ) );
						if ( x == "isAdvancedStandingFor" )
							connectionsQuery |= Query<LearningOppIndex>.Range( r => r.Field( f => f.IsAdvancedStandingForCount ).GreaterThan( 0 ) );
						if ( x == "advancedStandingFrom" )
							connectionsQuery |= Query<LearningOppIndex>.Range( r => r.Field( f => f.AdvancedStandingFromCount ).GreaterThan( 0 ) );
						if ( x == "isPreparationFor" )
							connectionsQuery |= Query<LearningOppIndex>.Range( r => r.Field( f => f.IsPreparationForCount ).GreaterThan( 0 ) );
						if ( x == "isPreparationFrom" )
							connectionsQuery |= Query<LearningOppIndex>.Range( r => r.Field( f => f.PreparationFromCount ).GreaterThan( 0 ) );
					} );
				}
			}

			#endregion

			#region QualityAssurance
			//NOTE: this is only referenced after clicking on a gray box, not from the search page
			//		==> actually now used by search widget => CONFIRM THIS
			commonOrgRolesBYQuery = CommonOrgRolesByFilter<LearningOppIndex>( query );
			publishedBYQuery = PublishedByFilter<LearningOppIndex>( query );

			//USED BY QUALITY ASSURANCE FILTER check boxes and org list
			qaFilterQuery = CommonQualityAssuranceFilter<LearningOppIndex>( query, qaRelationshipTypeIds );

			var commonOrgQARolesQuery = CommonNonQAOrgRolesFilter<LearningOppIndex>( query, relationshipTypeIds );

			//remove lifecycle query if any ownedBy/offeredBy or publishedBy
			if (publishedBYQuery != null )
			{
				//TBD - need to ensure no selected items
				//LifeCycleStatusTypeQuery = null;
			}
			#endregion


			#region Lopp Ids list
			QueryContainer potentialResultsQuery = null;
			if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CUSTOM ) )
			{
				var idsList = new List<int>();
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "potentialresults" ).ToList() )
				{
					idsList.AddRange( JsonConvert.DeserializeObject<List<int>>( filter.CustomJSON ) );
				}

				if ( idsList.Any() )
				{
					idsList.ForEach( x =>
					{
						potentialResultsQuery |= Query<LearningOppIndex>.Terms( ts => ts.Field( f => f.Id ).Terms( x ) );
					} );
				}
			}
			#endregion
			//collections. TBD on how the widget side will work. Probably not a collection Id
			//if ( query.CollectionId > 0 )		//???
			//{
			//	collectionIdQuery = Query<LearningOppIndex>.Terms( ts => ts.Field( f => f.ResourceForCollection ).Terms( query.CollectionId ) );
			//}
			//
			if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CUSTOM ) )
			{
				var idsList = new List<int>();
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "collectionresults" ).ToList() )
				{
					//filter.CustomJSON is populated in workIT.Services.API.SearchServicesTranslateMainQueryFiltersToMainSearchInputFilters
					//	TBD if should use something different
					idsList.AddRange( JsonConvert.DeserializeObject<List<int>>( filter.CustomJSON ) );
					//var f = filter.AsRecordIdList();
					//if ( f != null && f.IdsList?.Count > 0 )
					//	idsList.AddRange( f.IdsList );
				}

				if ( idsList.Any() )
				{
					//or
					collectionIdQuery = Query<LearningOppIndex>.Terms( ts => ts.Field( f => f.ResourceForCollection ).Terms<int>( idsList.ToArray() ) );

					//idsList.ForEach( x =>
					//{
					//	collectionIdQuery |= Query<LearningOppIndex>.Terms( ts => ts.Field( f => f.Id ).Terms( x ) );

					//} );
				}
			}


			collectionsQuery = CommonInCollectionByIdFilters<LearningOppIndex>( query );
			if ( collectionsQuery == null )
				collectionsQuery = CommonPartOfCollectionFilters<LearningOppIndex>( query );

			#region Occupations, Industries, Programs

			occupationsQuery = CommonOccupations<LearningOppIndex>( query, ref widgetOccupationsQuery );

			industriesQuery = CommonIndustries<LearningOppIndex>( query, ref windustriesQuery );

			classificationsQuery = CommonCip<LearningOppIndex>( query );
			#endregion

			#region Languages

			var languageFilters = new List<string>();
			if ( query.FiltersV2.Any( x => x.Name == "languages" ) )
			{
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "languages" ) )
				{
					var text = filter.GetValueOrDefault( "CodeText", "" );
					if ( string.IsNullOrWhiteSpace( text ) )
						continue;
					languageFilters.Add( text.ToLower() );
				}
				languageFilters.ForEach( x =>
				{
					languagesQuery = Query<LearningOppIndex>.Terms( ts => ts.Field( f => f.InLanguage ).Terms( x ) );
				} );
			}
			#endregion

			#region Boundaries

			boundariesQuery = HandleLocationQuery<CredentialIndex>( query );
			/*
			var boundaries = SearchServices.GetBoundaries( query, "bounds" );
			if ( boundaries.IsDefined )
			{

				boundariesQuery = Query<LearningOppIndex>
					.Nested( n => n.Path( p => p.Addresses )
					.Query( q => Query<LearningOppIndex>.Range( r => r.Field( f => f.Addresses.First().Longitude ).LessThan( ( double ) boundaries.East ).GreaterThan( ( double ) boundaries.West ) ) && Query<LearningOppIndex>.Range( r => r.Field( f => f.Addresses.First().Latitude ).LessThan( ( double ) boundaries.North ).GreaterThan( ( double ) boundaries.South ) ) ).IgnoreUnmapped() );
			}
			*/
			#endregion

			#region Query

			var sort = new SortDescriptor<LearningOppIndex>();

			var sortOrder = query.SortOrder;
			if ( sortOrder == "alpha" )
				sort.Ascending( s => s.Name.Suffix( "keyword" ) );
			else if ( sortOrder == "zalpha" )
				sort.Descending( s => s.Name.Suffix( "keyword" ) );
			else if ( sortOrder == "newest" )
				sort.Field( f => f.LastUpdated, SortOrder.Descending );
			else if ( sortOrder == "oldest" )
				sort.Field( f => f.LastUpdated, SortOrder.Ascending );
			else if ( sortOrder == "relevance" )
				sort.Descending( SortSpecialField.Score );
			//
			QueryContainer phrasePrefixQuery = SetPhrasePrefixQuery<LearningOppIndex>( query );
			QueryContainer bestFieldsQuery = SetBestFieldsQuery<LearningOppIndex>( query );
			QueryContainer crossFieldsQuery = SetCrossFieldsQuery<LearningOppIndex>( query );
			QueryContainer autoCompleteQuery = SetAutocompleteQuery<LearningOppIndex>( query );

			//


			try
			{

				//if ( query.Keywords.LastIndexOf( "[" ) > 0 )
				//	query.Keywords = query.Keywords.Substring( 0, query.Keywords.LastIndexOf( "[" ) );

				if ( query.StartPage < 1 )
					query.StartPage = 1;
				var pageSize = query.PageSize;
				if ( requestIsAutocomplete )
				{
					pageSize = pageSize * 2;
					phrasePrefixQuery = null;
					//??
					crossFieldsQuery = null;
					//bestFieldsQuery = null;
				}
				else
				{
					//autoCompleteQuery = null;

				}

				var search = EC.Search<LearningOppIndex>( body => body
					   .Index( LearningOppIndex )
					   .Query( q =>
						  competenciesQuery
						  //&& widgetQuery.OwningOrgsQuery //??
						  && potentialResultsQuery
						  && collectionIdQuery
						  && learningObjectTypeQuery
						  && LifeCycleStatusTypeQuery
						  && entityStateQuery
						  && collectionsQuery
						  && subjectsQuery
						  && keywordsQuery
						  && methodTypesQuery
						  && deliveryTypesQuery
						  && occupationsQuery
						  && industriesQuery
						  && classificationsQuery
						  && connectionsQuery
						  && audienceLevelTypeQuery
						  && audienceTypeQuery
						  && commonOrgQARolesQuery
						  && qaFilterQuery && commonOrgRolesBYQuery && publishedBYQuery
						  && languagesQuery
						  && boundariesQuery
						  && locationQueryFilters.RegionQuery
						  && locationQueryFilters.CountryQuery
						  && locationQueryFilters.CityQuery
						  && wLWIAQuery
						  && history.CreatedFromQuery && history.CreatedToQuery && history.HistoryFromQuery && history.HistoryToQuery
						  //&& widgetQuery.KeywordQuery
						  && reportsQuery
						  && ( phrasePrefixQuery || autoCompleteQuery || bestFieldsQuery || crossFieldsQuery )
					   //&& ( q.MultiMatch( m => m
					   // .Fields( f => f
					   //  .Field( ff => ff.Name, 90 )
					   //  .Field( ff => ff.ListTitle, 90 )
					   //  .Field( ff => ff.Description, 75 )
					   //  .Field( ff => ff.OwnerOrganizationName, 80 )
					   //  .Field( ff => ff.TextValues, 45 )
					   //  .Field( ff => ff.SubjectAreas, 50 ) //??
					   // )
					   // .Operator( Operator.Or )
					   // .Type( TextQueryType.BestFields )
					   // .Query( query.Keywords )
					   // .MaxExpansions( 10 )

					   //)
					   //these appear to be mostly the same??
					   // || q.MultiMatch( m => m
					   //		.Fields( f => f
					   //		.Field( ff => ff.Name, 90 )
					   //		.Field( ff => ff.Description, 50 )
					   //		.Field( ff => ff.OwnerOrganizationName, 80 )
					   //		.Field( ff => ff.ListTitle, 95 )
					   //		)
					   //		.Operator( Operator.Or )
					   //		.Type( TextQueryType.PhrasePrefix )
					   //		.Query( query.Keywords )
					   //	)
					   //)
					   )
					   .TrackTotalHits( true )
					   .Sort( s => sort )
					   .From( query.StartPage > 0 ? query.StartPage - 1 : 1 )
					   .Skip( ( query.StartPage - 1 ) * query.PageSize )
					   .Size( pageSize ) );


				#endregion

				var debug = search.DebugInformation;
				pTotalRows = ( int ) search.Total;
				if ( requestIsAutocomplete )
				{
					var requestList = ( List<LearningOppIndex> ) search.Documents;
					autocompleteList = requestList.Select( x => x.ListTitle ).Distinct().Take( query.PageSize ).ToList();
					return null;
				}
				if ( query.StartPage == 1 )
				{
					try
					{
						//OR to avoid excessive logging could look for a special character 
						if ( query.Filters.Count > 0 || query.FiltersV2.Count > 0 || !string.IsNullOrWhiteSpace( query.Keywords ) )
						{
							//actually extract the request: between #request and #response
							var elasticQuery = UtilityManager.ExtractNameValue( debug, "# Request", ":", "# Response:" );
							LoggingHelper.DoTrace( 6, string.Format( "ElasticServices.Search: {0}, Results: {1}, ElasticLog: \r\n", "LearningOpportunity", pTotalRows ) + elasticQuery, "ElasticQuery" );

						}
						else if ( query.Filters.Count == 0 && query.FiltersV2.Count == 0 && string.IsNullOrWhiteSpace( query.Keywords ) && query.WidgetId == 0 )
						{
							if ( UtilityManager.GetAppKeyValue( "environment" ) == "production" && pTotalRows < 1000 )
							{
								//actually extract the request: between #request and #response
								var elasticQuery = UtilityManager.ExtractNameValue( debug, "# Request", ":", "# Response:" );
								LoggingHelper.DoTrace( 6, string.Format( "ElasticServices.Search: {0}, Results: {1}, ElasticLog: \r\n", "LearningOpportunity", pTotalRows ) + elasticQuery, "ElasticQuery" );

								//var acctsemail = UtilityManager.GetAppKeyValue( "accountNotifications" );
								//EmailManager.NotifyAdmin( acctsemail, "WARNING LearningOpportunity blind Search Result Low", string.Format( "A LearningOpportunity blind search only returned {0} records. This seems to be unexpected.", pTotalRows ) );
							}
						}
					} catch ( Exception ex )
					{
						var msg = BaseFactory.FormatExceptions( ex );
						LoggingHelper.DoTrace( 3, string.Format( "ElasticServices.Search: {0}. Exception encountered extracting elastic summary. Results: {1}, ElasticLog: \r\n", "LearningOpportunity", pTotalRows ) + msg, "ElasticQuery" );

					}
				}
				if ( pTotalRows > 0 )
				{
					list = ElasticManager.LearningOpp_MapFromElastic( ( List<LearningOppIndex> ) search.Documents, query.StartPage, query.PageSize );
					LoggingHelper.DoTrace( 6, string.Format( "ElasticServices.LearningOppSearch. found: {0} records", pTotalRows ) );
					if ( list.Count == 0 )
						pTotalRows = 0;
				}

				//stats
				query.Results = pTotalRows;
				string jsoninput = JsonConvert.SerializeObject( query, JsonHelper.GetJsonSettings() );
				string searchType = "blind";
				if ( query.Filters.Count > 0 || query.FiltersV2.Count > 0 || !string.IsNullOrWhiteSpace( query.Keywords ) )
				{
					searchType = "filters selected";
				}
				if ( query.StartPage > 1 )
					searchType += " - paging";
				new ActivityServices().AddActivity( new SiteActivity()
				{ ActivityType = "LearningOpportunity", Activity = "Search", Event = searchType, Comment = jsoninput }
				);

			}
			catch ( Exception ex )
			{
				list.Add( new PM.LearningOpportunityProfile()
				{
					Name = "Exception encountered during search",
					Description = BaseFactory.FormatExceptions( ex ),
				} );
				LoggingHelper.DoTrace( 1, string.Format( "ElasticServices.LearningOpportunitySearch. Exception: {0}", ex.Message ) );

			}
			return list;
		}

		#endregion

		#region Competency frameworks
		#region Build/update index
		public static void CompetencyFramework_BuildIndex( bool deleteIndexFirst = false, bool updatingIndexRegardless = false )
		{
			var list = new List<CompetencyFrameworkIndex>();
			bool indexInitialized = false;
			if ( deleteIndexFirst && EC.Indices.Exists( CompetencyFrameworkIndex ).Exists )
			{
				EC.Indices.Delete( CompetencyFrameworkIndex );
			}
			if ( !EC.Indices.Exists( CompetencyFrameworkIndex ).Exists )
			{
				CompetencyFrameworkInitializeIndex();
				indexInitialized = true;
			}

			if ( indexInitialized || updatingIndexRegardless )
			{
				LoggingHelper.DoTrace( 1, "CompetencyFramework_BuildIndex - starting" );
				int minEntityStateId = UtilityManager.GetAppKeyValue( "minAsmtEntityStateId", 3 );
				try
				{
					new ActivityServices().AddActivity( new SiteActivity()
					{ ActivityType = "CompetencyFramework", Activity = "Elastic", Event = "Build Index" }
					);

					list = ElasticManager.CompetencyFramework_SearchForElastic( string.Format( "( base.EntityStateId >= {0} )", minEntityStateId ) );
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, "CompetencyFramework_BuildIndex" );
				}
				finally
				{
					if ( list != null && list.Count > 0 )
					{
						LoggingHelper.DoTrace( 1, string.Format( "CompetencyFramework_BuildIndex - doing bulk load of {0} records.", list.Count ) );
						var results = EC.Bulk( b => b.IndexMany( list, ( d, document ) => d.Index( CompetencyFrameworkIndex ).Document( document ).Id( document.Id.ToString() ) ) );
						if ( results.ToString().IndexOf( "Valid NEST response built from a successful" ) == -1 )
						{
							Console.WriteLine( results.ToString() );
							LoggingHelper.DoTrace( 1, " Issue building CompetencyFramework index: " + results );
						}

						EC.Indices.Refresh( CompetencyFrameworkIndex );
					}
				}
			}

		}

		public static void CompetencyFrameworkInitializeIndex( bool deleteIndexFirst = true )
		{
			if ( !EC.Indices.Exists( CompetencyFrameworkIndex ).Exists )
			{

				EC.Indices.Create( CompetencyFrameworkIndex, c => new CreateIndexDescriptor( CompetencyFrameworkIndex )

				 .Settings( st => st
						 .Analysis( an => an.TokenFilters( tf => tf.Stemmer( "custom_english_stemmer", ts => ts.Language( "english" ) ) ).Analyzers( anz => anz.Custom( "custom_lowercase_stemmed", cc => cc.Tokenizer( "standard" ).Filters( new List<string> { "lowercase", "custom_english_stemmer" } ) ) ) ) )
						//.Mappings( ms => ms
						.Map<CompetencyFrameworkIndex>( m => m
							.AutoMap()
							.Properties( p => p
								.Text( s => s.Name( n => n.Description ).Analyzer( "custom_lowercase_stemmed" ) )
								.Nested<IndexCompetency>( n => n
									.Name( nn => nn.Competencies )
									.AutoMap()
								)
								.Nested<Models.Elastic.AgentRelationshipForEntity>( n => n
									.Name( nn => nn.AgentRelationshipsForEntity )
									.AutoMap()
								)
							)
						)
				//)
				);
			}
		}

		public static void CompetencyFramework_UpdateIndex( int recordId )
		{
			if ( recordId < 1 )
				return;
			string filter = string.Format( " ( base.Id = {0} ) ", recordId );
			int processed = 0;
			CompetencyFramework_UpdateIndex( filter, ref processed );

		} //
		/// <summary>
		/// Pass a filter to use for updating the index
		/// </summary>
		/// <param name="filter"></param>
		public static void CompetencyFramework_UpdateIndex( string filter, ref int processed )
		{
			if ( string.IsNullOrWhiteSpace( filter ) )
				return;
			int action = UtilityManager.GetAppKeyValue( "updateCredIndexAction", 0 );
			if ( action == 0 )
				return;
			try
			{

				var list = ElasticManager.CompetencyFramework_SearchForElastic( filter );
				if ( list != null && list.Count > 0 )
				{
					processed = list.Count;
					if ( action == 1 )
					{
						foreach ( var item in list )
						{
							var res = EC.Index( item, idx => idx.Index( CompetencyFrameworkIndex ) );
							Console.WriteLine( res.Result );
						}

					}
					else if ( action == 2 )
					{
						var results = EC.Bulk( b => b.IndexMany( list, ( d, entity ) => d.Index( CompetencyFrameworkIndex ).Document( entity ).Id( entity.Id.ToString() ) ) );
						if ( results.ToString().IndexOf( "Valid NEST response built from a successful" ) == -1 )
						{
							Console.WriteLine( results.ToString() );
							LoggingHelper.DoTrace( 1, " Issue building CompetencyFramework index: " + results );
						}
					}
				}
				else
				{
					LoggingHelper.DoTrace( 2, string.Format( "CompetencyFramework_UpdateIndex. No data returned for filter: {0}", filter ) );
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "CompetencyFramework_UpdateIndex failed for filter: {0}", filter ) );
			}

		}
		#endregion
		public static List<string> CompetencyFrameworkAutoComplete( string keyword, int maxTerms, ref int pTotalRows )
		{
			var query = string.Format( "*{0}*", keyword.ToLower() );
			var search = EC.Search<CompetencyFrameworkIndex>( i => i.Index( CompetencyFrameworkIndex ).Query( q => q.MultiMatch( m => m
			  .Fields( f => f
			  .Field( ff => ff.Name )
			  .Field( ff => ff.Description )
			   )
			   //.Operator( Operator.Or )
			   .Type( TextQueryType.PhrasePrefix )
			   .Query( keyword )
			   .MaxExpansions( 10 ) ) ).Size( maxTerms * 2 ) );

			pTotalRows = ( int ) search.Total;
			var list = ( List<CompetencyFrameworkIndex> ) search.Documents;
			return list.Select( x => x.Name ).Distinct().Take( maxTerms ).ToList();
		}
		/// <summary>
		/// Proxy for full search
		/// </summary>
		/// <param name="query"></param>
		/// <param name="pTotalRows"></param>
		/// <returns></returns>
		public static List<CompetencyFrameworkSummary> CompetencyFrameworkSearch( MainSearchInput query, ref int pTotalRows )
		{
			List<string> autocompleteList = new List<string>();
			bool requestIsAutocomplete = false;
			return CompetencyFrameworkSearch( query, ref pTotalRows, ref autocompleteList, requestIsAutocomplete );
		}
		public static List<CompetencyFrameworkSummary> CompetencyFrameworkSearch( MainSearchInput query, ref int pTotalRows, ref List<string> autocompleteList, bool requestIsAutocomplete = false )
		{
			CompetencyFramework_BuildIndex();

			var list = new List<PM.CompetencyFrameworkSummary>();

			QueryContainer competenciesQuery = null;
			QueryContainer commonOrgRolesBYQuery = null;
			QueryContainer publishedBYQuery = null;

			QueryContainer keywordsQuery = null;

			QueryContainer connectionsQuery = null;
			QueryContainer languagesQuery = null;
			QueryContainer reportsQuery = null;

			#region Competencies

			if ( query.FiltersV2.Any( x => x.Name == "competencies" ) )
			{
				var competencies = new List<string>();
				var competencySchemas = new List<string>();
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "competencies" ) )
				{
					var codeItem = filter.HasAnyValue();
					if ( codeItem.AnyValue )
					{
						competencySchemas.Add( codeItem.SchemaName );
					}
					else
					{
						var text = filter.AsText();
						try
						{
							if ( text.IndexOf( " - " ) > -1 )
								text = text.Substring( text.IndexOf( " -- " ) + 4 );
						}
						catch { }

						if ( text.Trim().Length > 2 )
						{
							text = SearchServices.SearchifyWord( text );
							competencies.Add( text.Trim() );
						}
					}
				}
				if ( competencySchemas.Any() )
				{
					var ids = CodesManager.GetEntityStatisticBySchema( competencySchemas );
					if ( ids.Any() )
					{
						competenciesQuery = Query<CompetencyFrameworkIndex>.Terms( ts => ts.Field( f => f.ReportFilters ).Terms<int>( ids.ToArray() ) );
					}
				}
				if ( competencies.Any() )
				{
					competencies.ForEach( x =>
					{
						competenciesQuery |= Query<CompetencyFrameworkIndex>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Competencies.First().Name, 70 ) ).Type( TextQueryType.PhrasePrefix ).Query( x ) );
						competenciesQuery |= Query<CompetencyFrameworkIndex>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Competencies.First().Name, 70 ) ).Type( TextQueryType.BestFields ).Query( x ) );
					} );
					if ( competenciesQuery != null )
						competenciesQuery = Query<CompetencyFrameworkIndex>.Nested( n => n.Path( p => p.Competencies ).Query( q => competenciesQuery ).IgnoreUnmapped() );
				}
			}

			#endregion

			#region custom
			if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CODE ) )
			{
				var reportIds = new List<int>();

				foreach ( var filter in query.FiltersV2.Where( m => m.Type == MainSearchFilterV2Types.CODE ).ToList() )
				{
					var item = filter.AsCodeItem();
					if ( filter.Name == "reports" || filter.Name == "otherfilters" || filter.Name == "filter:OtherFilters" )
					{
						reportIds.Add( item.Id ); //can probably continue after here?
					}
					if ( item == null || item.CategoryId < 1 )
						continue;

				}

				if ( reportIds.Any() )
				{
					reportsQuery = Query<CompetencyFrameworkIndex>.Terms( ts => ts.Field( f => f.ReportFilters ).Terms<int>( reportIds.ToArray() ) );
				}

			}
			#endregion
			#region keywords

			//keywords from widget
			if ( query.FiltersV2.Any( x => x.Name == "keywords" ) )
			{
				//keywordsQuery = HandleKeywords<CompetencyFrameworkIndex>( query );

				var tags = new List<string>();
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "keywords" ) )
				{
					var text = filter.AsText();
					if ( string.IsNullOrWhiteSpace( text ) )
						continue;
					//this doesn't work, get ORs need ANDs:
					//temp
					//query.Keywords += " " + text;
					tags.Add( text.ToLower() );
				}
				QueryContainer qc = null;
				tags.ForEach( x =>
				{
					qc |= Query<CompetencyFrameworkIndex>.MatchPhrase( mp => mp.Field( f => f.Keyword ).Query( x ) );
				} );
				if ( qc != null )
				{
					keywordsQuery = Query<CompetencyFrameworkIndex>.MultiMatch( m => m
								 .Fields( f => f
									.Field( ff => ff.Name, 90 )
									.Field( ff => ff.PrimaryOrganizationName, 90 )
									.Field( ff => ff.Description, 45 )
									.Field( ff => ff.SourceUrl, 25 )
									.Field( ff => ff.TextValues, 50 )
									.Field( ff => ff.Keyword, 60 )
								 )
								 //.Slop(2)
								 //.Operator( Operator.And )
								 .Type( TextQueryType.PhrasePrefix )
								 .Query( string.Join( "", tags.ToList() ) )
							//.MaxExpansions( 10 )
							//.Analyzer( "standard" )
							)
							|| Query<CompetencyFrameworkIndex>.MultiMatch( m => m
							.Fields( f => f
								.Field( ff => ff.Name, 90 )
								 .Field( ff => ff.Description, 45 )
								 .Field( ff => ff.SourceUrl, 25 )
								 .Field( ff => ff.PrimaryOrganizationName, 90 )
								 .Field( ff => ff.TextValues, 50 )
								 .Field( ff => ff.Keyword, 60 )
							 )
							.Type( TextQueryType.BestFields )
							.Query( string.Join( "", tags.ToList() ) )
							);
				}
			}
			#endregion

			#region Languages

			var languageFilters = new List<string>();
			if ( query.FiltersV2.Any( x => x.Name == "languages" ) )
			{
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "languages" ) )
				{
					var text = filter.GetValueOrDefault( "CodeText", "" );
					if ( string.IsNullOrWhiteSpace( text ) )
						continue;
					languageFilters.Add( text.ToLower() );
				}
				languageFilters.ForEach( x =>
				{
					languagesQuery = Query<CompetencyFrameworkIndex>.Terms( ts => ts.Field( f => f.InLanguage ).Terms( x ) );
				} );
			}
			#endregion

			/*
			The type 'workIT.Models.Elastic.CompetencyFrameworkIndex' cannot be used as type parameter 'T' in the generic type or method 'ElasticServices.CommonQualityAssurance<T>(MainSearchInput)'. There is no implicit reference conversion from 'workIT.Models.Elastic.CompetencyFrameworkIndex' to 'workIT.Models.Elastic.IIndex'.
			*/
			commonOrgRolesBYQuery = CommonOrgRolesByFilter<CompetencyFrameworkIndex>( query );
			publishedBYQuery = PublishedByFilter<CompetencyFrameworkIndex>( query );
			//
			var commonLeftRailOrgRolesBYQuery = CommonNonQAOrgRolesLeftRailFilter<CompetencyFrameworkIndex>( query, new List<int>() { 6, 7 } );
			#region Query

			var sort = new SortDescriptor<CompetencyFrameworkIndex>();

			var sortOrder = query.SortOrder;
			if ( sortOrder == "alpha" )
				sort.Ascending( s => s.Name.Suffix( "keyword" ) );
			else if ( sortOrder == "zalpha" )
				sort.Descending( s => s.Name.Suffix( "keyword" ) );
			else if ( sortOrder == "newest" )
				sort.Field( f => f.LastUpdated, SortOrder.Descending );
			else if ( sortOrder == "oldest" )
				sort.Field( f => f.LastUpdated, SortOrder.Ascending );
			else if ( sortOrder == "relevance" )
				sort.Descending( SortSpecialField.Score );

			if ( query.StartPage < 1 )
				query.StartPage = 1;

			var search = EC.Search<CompetencyFrameworkIndex>( body => body
				   .Index( CompetencyFrameworkIndex )
				   .Query( q =>
					  competenciesQuery
					  //&& widgetQuery.OwningOrgsQuery //??

					  && keywordsQuery
					  && connectionsQuery
					  && languagesQuery
					  && commonOrgRolesBYQuery && publishedBYQuery && commonLeftRailOrgRolesBYQuery
					  && reportsQuery
					  && ( q.MultiMatch( m => m
						  .Fields( f => f
							  .Field( ff => ff.Name, 90 )
							  .Field( ff => ff.Description, 75 )
							  .Field( ff => ff.CTID, 100 )
							  .Field( ff => ff.OwnerOrganizationCTID, 100 )
							  .Field( ff => ff.SourceUrl, 25 )
							  .Field( ff => ff.PrimaryOrganizationName, 80 )
							  .Field( ff => ff.TextValues, 45 )
						  )
						  .Operator( Operator.Or )
						  .Type( TextQueryType.BestFields )
						  .Query( query.Keywords )
						  .MaxExpansions( 10 )
					  )

					  || q.MultiMatch( m => m
						 .Fields( f => f
							  .Field( ff => ff.Name, 90 )
							  .Field( ff => ff.CTID, 100 )
							  .Field( ff => ff.OwnerOrganizationCTID, 100 )
							  .Field( ff => ff.Description, 50 )
							  .Field( ff => ff.SourceUrl, 25 )
							  .Field( ff => ff.PrimaryOrganizationName, 80 )
							  .Field( ff => ff.TextValues, 60 )
						 )
						 .Operator( Operator.Or )
						 .Type( TextQueryType.BestFields )
						 .Query( query.Keywords )
						)
					 )
				   )
				   .TrackTotalHits( true )
				   .Sort( s => sort )
				   .From( query.StartPage > 0 ? query.StartPage - 1 : 1 )
				   .Skip( ( query.StartPage - 1 ) * query.PageSize )
				   .Size( query.PageSize ) );

			#endregion

			var debug = search.DebugInformation;
			pTotalRows = ( int ) search.Total;
			if ( requestIsAutocomplete )
			{
				var requestList = ( List<CompetencyFrameworkIndex> ) search.Documents;
				autocompleteList = requestList.Select( x => x.Name ).Distinct().Take( query.PageSize ).ToList();
				return null;
			}

			if ( query.StartPage == 1 && ( query.Filters.Count > 0 || query.FiltersV2.Count > 0 || !string.IsNullOrWhiteSpace( query.Keywords ) ) )
			{
				//actually extract the request: between #request and #response
				var elasticQuery = UtilityManager.ExtractNameValue( debug, "# Request", ":", "# Response:" );
				LoggingHelper.DoTrace( 6, string.Format( "ElasticServices.Search: {0}, Results: {1}, ElasticLog: \r\n", "CompetencyFrameworks", pTotalRows ) + elasticQuery, "ElasticQuery" );
			}
			if ( pTotalRows > 0 )
			{
				list = ElasticManager.CompetencyFramework_MapFromElastic( ( List<CompetencyFrameworkIndex> ) search.Documents, query.StartPage, query.PageSize );
				LoggingHelper.DoTrace( 6, string.Format( "ElasticServices.CompetencyFrameworkSearch. found: {0} records", pTotalRows ) );
				if ( list.Count == 0 )
					pTotalRows = 0;
			}

			//stats
			query.Results = pTotalRows;
			string jsoninput = JsonConvert.SerializeObject( query, JsonHelper.GetJsonSettings() );
			string searchType = "blind";
			if ( query.Filters.Count > 0 || query.FiltersV2.Count > 0 || !string.IsNullOrWhiteSpace( query.Keywords ) )
			{
				searchType = "filters selected";
			}
			if ( query.StartPage > 1 )
				searchType += " - paging";
			new ActivityServices().AddActivity( new SiteActivity()
			{ ActivityType = "CompetencyFramework", Activity = "Search", Event = searchType, Comment = jsoninput }
			);
			return list;
		}


		#endregion
		#region Common 
		/// <summary>
		/// Handle keyworks where entered in the filters, not query.Keywords
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="query"></param>
		/// <returns></returns>
		public static QueryContainer HandleWidgetKeywords<T>( MainSearchInput query ) where T : class, IIndex
		{
			QueryContainer results = null;
			if ( query.FiltersV2.Any( x => x.Name == "keywords" ) )
			{
				QueryContainer qc = null;
				var tags = new List<string>();
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "keywords" ) )
				{
					var text = filter.AsText();
					if ( string.IsNullOrWhiteSpace( text ) )
						continue;
					tags.Add( text.ToLower() );
				}
				//not sure if we need this approach
				tags.ForEach( x =>
				{
					qc |= Query<T>.MatchPhrase( mp => mp.Field( f => f.Keyword ).Query( x ) );
				} );
				if ( qc != null )
				{
					//keywordsQuery = Query<CredentialIndex>.MatchPhrase( n => n.( p => p.Keyword ).Query( q => qc ).IgnoreUnmapped() );

					results = Query<T>.MultiMatch( m => m
								 .Fields( f => f
									.Field( ff => ff.Name, 90 )
									//.Field( ff => ff.ListTitle, 90 )
									.Field( ff => ff.PrimaryOrganizationName, 90 )
									.Field( ff => ff.Description, 45 )
									.Field( ff => ff.SubjectWebpage, 25 )
									//.Field( ff => ff.AlternateNames, 35 )
									.Field( ff => ff.TextValues, 50 )
									.Field( ff => ff.Keyword, 60 )
								 )
								 //.Slop(2)
								 //.Operator( Operator.And )
								 .Type( TextQueryType.PhrasePrefix )
								 .Query( string.Join( " ", tags.ToList() ) )
							//.MaxExpansions( 10 )
							//.Analyzer( "standard" )
							)
							|| Query<T>.MultiMatch( m => m
							.Fields( f => f
								.Field( ff => ff.Name, 90 )
								 //.Field( ff => ff.ListTitle, 90 )
								 .Field( ff => ff.Description, 45 )
								 .Field( ff => ff.SubjectWebpage, 25 )
								 .Field( ff => ff.PrimaryOrganizationName, 90 )
								 //.Field( ff => ff.AlternateNames, 35 )
								 .Field( ff => ff.TextValues, 50 )
								 .Field( ff => ff.Keyword, 60 )
							 )
							.Type( TextQueryType.BestFields )
							.Query( string.Join( " ", tags.ToList() ) )
							);
				}
			}
			return results;
		}
		/// <summary>
		/// Subject search using SubjectIndex (nested)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="query"></param>
		/// <returns></returns>
		public static QueryContainer HandleSubjects<T>( MainSearchInput query ) where T : class, IIndex
		{
			QueryContainer thisQuery = null;
			if ( query.FiltersV2.Any( x => x.Name == "subjects" || x.Name == "filter:Subjects" ) )
			{
				QueryContainer qc = null;
				var reportSchemas = new List<string>();
				var tags = new List<string>();
				foreach ( var filter in query.FiltersV2.Where( x => x.Name == "subjects" || x.Name == "filter:Subjects" ) )
				{
					//handle where the hasAny check was clicked
					var codeItem = filter.HasAnyValue();
					if ( codeItem.AnyValue || ( !string.IsNullOrWhiteSpace( codeItem.SchemaName ) && codeItem.SchemaName != "filter:Subjects" ) )
					{
						reportSchemas.Add( codeItem.SchemaName );
					}
					else
					{
						var text = filter.AsText();
						if ( string.IsNullOrWhiteSpace( text ) )
							continue;
						tags.Add( text.ToLower() );
					}
				}//
				if ( reportSchemas.Any() )
				{
					var ids = CodesManager.GetEntityStatisticBySchema( reportSchemas );
					if ( ids.Any() )
					{
						thisQuery = Query<T>.Terms( ts => ts.Field( f => f.ReportFilters ).Terms<int>( ids.ToArray() ) );
					}
				}
				//
				if ( tags.Any() )
				{
					if ( reportSchemas.Any() ) //if there are concrete subjects, remove the Has any
						thisQuery = null;
					tags.ForEach( x =>
					{
						qc |= Query<T>.MatchPhrase( mp => mp.Field( f => f.Subjects.First().Name ).Query( x ).Boost( 60 ) );
					} );
					if ( qc != null )
					{
						thisQuery = Query<T>.Nested( n => n.Path( p => p.Subjects ).Query( q => qc ).IgnoreUnmapped() );
					}
				}
			}

			//TBD - need to review use with widgets
			//22-04-14 mp - can't have two QueryContainers returned, as will result in an AND that may conflict.
			//			If changed to an OR, could be confusing. --disable for now
			if ( query.FiltersV2.Any( x => x.Name == "wsubjects" ) )
			{
				/*
				QueryContainer qc = null;
				var tags = new List<string>();
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "wsubjects" ) )
				{
					var text = filter.AsText();
					if ( string.IsNullOrWhiteSpace( text ) )
						continue;
					tags.Add( text.ToLower() );
				}
				tags.ForEach( x =>
				{
					qc |= Query<T>.MatchPhrase( mp => mp.Field( f => f.SubjectAreas.First() ).Query( x ).Boost( 60 ) );
				} );

				if ( qc != null )
				{
					wsubjectsQuery = Query<T>.Nested( n => n.Path( p => p.SubjectAreas ).Query( q => qc ).IgnoreUnmapped() );
				}
				*/
			}
			return thisQuery;
		}
		/// <summary>
		/// Subject search using list of strings
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="query"></param>
		/// <returns></returns>
		public static QueryContainer HandleSubjectAreas<T>( MainSearchInput query ) where T : class, IIndex
		{
			QueryContainer results = null;
			if ( query.FiltersV2.Any( x => x.Name == "subjects" || x.Name == "filter:Subjects" ) )
			{
				QueryContainer qc = null;
				var tags = new List<string>();
				foreach ( var filter in query.FiltersV2.Where( x => x.Name == "subjects" || x.Name == "filter:Subjects" ) )
				{
					var text = filter.AsText();
					if ( string.IsNullOrWhiteSpace( text ) )
						continue;
					tags.Add( text.ToLower() );
				}
				tags.ForEach( x =>
				{
					qc |= Query<T>.MatchPhrase( mp => mp.Field( f => f.SubjectAreas ).Query( x ) );
				} );
				if ( qc != null )
				{
					return qc;
					//results = Query<T>.Nested( n => n.Path( p => p.Subjects ).Query( q => qc ).IgnoreUnmapped() );
				}

			}

			return results;
		}
		//
		public static QueryContainer CommonCompetencies<T>( MainSearchInput query ) where T : class, IIndex
		{
			QueryContainer thisQuery = null;
			if ( !query.FiltersV2.Any( x => x.Name == "competencies" ) )
				return null;
			//
			//if any competency text entered, should ignore the has any Competencies
			bool hasAnyRequested = false;
			var competencies = new List<string>();
			var competencySchemas = new List<string>();
			foreach ( var filter in query.FiltersV2.Where( m => m.Name == "competencies" ) )
			{
				var codeItem = filter.HasAnyValue();
				if ( codeItem.AnyValue || !string.IsNullOrWhiteSpace( codeItem.SchemaName ) )
				{
					competencySchemas.Add( codeItem.SchemaName );
					hasAnyRequested = true;
				}
				else
				{
					var text = filter.AsText();
					try
					{
						if ( text.IndexOf( " - " ) > -1 )
							text = text.Substring( text.IndexOf( " -- " ) + 4 );
					}
					catch { }

					if ( text.Trim().Length > 2 )
					{
						text = SearchServices.SearchifyWord( text );
						competencies.Add( text.Trim() );
					}
				}
			}//
			 //this should be ignored if there was any text! NOTE: Done later.
			if ( competencySchemas.Any() )
			{
				var ids = CodesManager.GetEntityStatisticBySchema( competencySchemas );
				if ( ids.Any() )
				{
					thisQuery = Query<T>.Terms( ts => ts.Field( f => f.ReportFilters ).Terms<int>( ids.ToArray() ) );
				}
			}

			//21-05-10 mparsons - the competency search no longer works!!!!!
			//				- assessment seems to, coping from the latter.
			//competencies.ForEach( x =>
			//{
			//	//Should eventually change once the Competencies have proper inputs.
			//	competenciesQuery |= Query<T>.Nested( n => n
			//			.Path( p => p.Competencies )
			//			.Query( q => q
			//				.Bool( mm => mm
			//					.Must( mu => mu
			//						.MultiMatch( m => m
			//							.Fields( mf => mf.Field( f => f.Competencies.First().Name, 70 ) ).Type( TextQueryType.PhrasePrefix ).Query( x ) ) ||
			//	mu.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Competencies.First().Name, 70 ) ).Type( TextQueryType.BestFields ).Query( x ) ) ) ) ).IgnoreUnmapped() );
			//} );
			if ( competencies.Any() )
			{
				if ( hasAnyRequested )
					thisQuery = null;

				competencies.ForEach( x =>
				{
					//Temporary fix
					thisQuery |= Query<T>.MultiMatch( m => m
						.Fields( mf => mf
							.Field( f => f.Competencies.First().Name, 70 ) )
								.Type( TextQueryType.PhrasePrefix )
									.Query( x ) );
					thisQuery |= Query<T>.MultiMatch( m => m
						.Fields( mf => mf
							.Field( f => f.Competencies.First().Name, 60 ) )
								.Type( TextQueryType.Phrase )
									.Query( x ) );
					//add BestFields -> only includes full words - so use a higher weight
					thisQuery |= Query<T>
						.MultiMatch( m => m
							.Fields( mf => mf
								.Field( f => f.Competencies.First().Name, 100 ) )
									.Type( TextQueryType.BestFields ).Query( x ) );
				} );
				if ( thisQuery != null )
					thisQuery = Query<T>.Nested( n => n.Path( p => p.Competencies ).Query( q => thisQuery ).IgnoreUnmapped() );
			}

			return thisQuery;
		}
		//
		public static QueryContainer SetPhrasePrefixQuery<T>( MainSearchInput query ) where T : class, IIndex
		{
			QueryContainer phrasePrefixQuery = Query<T>
				.MultiMatch( m => m
					.Fields( f => f
						.Field( ff => ff.NameOrganizationKey, 90 )      //??
						.Field( ff => ff.ListTitle, 90 )
						.Field( ff => ff.PrimaryOrganizationName, 90 )
						.Field( ff => ff.Description, 50 )
						.Field( ff => ff.SubjectAreas, 50 ) //??
						.Field( ff => ff.Occupation, 30 )
						.Field( ff => ff.Industry, 30 )
						.Field( ff => ff.InstructionalProgram, 30 )
						.Field( ff => ff.Keyword, 60 )  //
					)
			.Type( TextQueryType.PhrasePrefix )
			.Query( query.Keywords )
			.MaxExpansions( 10 )
			//.Analyzer( "standard" )
			);

			return phrasePrefixQuery;
		}
		//
		public static QueryContainer SetCrossFieldsQuery<T>( MainSearchInput query ) where T : class, IIndex
		{
			QueryContainer qc = Query<T>
				.MultiMatch( m => m
					.Fields( f => f
						.Field( ff => ff.NameOrganizationKey, 90 )      //??
																		//.Field( ff => ff.Description, 50 )
						.Field( ff => ff.Occupation, 30 )
						.Field( ff => ff.Industry, 30 )
						.Field( ff => ff.InstructionalProgram, 30 )
						.Field( ff => ff.Keyword, 60 )  //
					)
				.Type( TextQueryType.CrossFields )
				.Query( query.Keywords )
				.MaxExpansions( 10 )
			//.Analyzer( "standard" )
			);

			return qc;
		}
		//
		public static QueryContainer SetBestFieldsQuery<T>( MainSearchInput query ) where T : class, IIndex
		{
			QueryContainer bestFieldsQuery = Query<T>
				.MultiMatch( m => m
					.Fields( f => f
						.Field( ff => ff.Name, 90 )      //??
						.Field( ff => ff.ListTitle, 90 )
						.Field( ff => ff.PrimaryOrganizationName, 90 )
						.Field( ff => ff.Description, 50 )
						.Field( ff => ff.SubjectAreas, 50 ) //??
						.Field( ff => ff.Occupation, 30 )
						.Field( ff => ff.Industry, 30 )
						.Field( ff => ff.InstructionalProgram, 30 )
						.Field( ff => ff.Keyword, 60 )  //
					)
				.Type( TextQueryType.BestFields )
				.Query( query.Keywords )
				.MaxExpansions( 10 )
			//.Analyzer( "standard" )
			);

			return bestFieldsQuery;
		}
		//
		public static QueryContainer SetAutocompleteQuery<T>( MainSearchInput query ) where T : class, IIndex
		{
			QueryContainer autoCompleteQuery = Query<T>
				.MultiMatch( m => m
					.Fields( f => f
						.Field( ff => ff.NameOrganizationKey, 90 )      //??
						.Field( ff => ff.PrimaryOrganizationName, 90 )
					//.Field( ff => ff.Description, 50 )
					)
				.Type( TextQueryType.PhrasePrefix )
				.Query( query.Keywords )
			//.MaxExpansions( 10 )
			//.Analyzer( "standard" )
			);

			return autoCompleteQuery;
		}

		/// <summary>
		/// Handle search using link from a detail page for members of a collection
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="query"></param>
		/// <returns></returns>
		public static QueryContainer CommonInCollectionByIdFilters<T>( MainSearchInput query ) where T : class, IIndex
		{
			QueryContainer thisQuery = null;
			var results = new List<CodeItem>();
			var list = new List<int>();
			if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CUSTOM ) )
			{
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "partofcollection" ).ToList() )
				{
					var f = filter.AsRecordId();
					list.Add( f.Id );
					results.Add( filter.AsRecordId() );
				}

				if ( results.Any() )
					results.ForEach( x =>
					{
						thisQuery = Query<T>.Terms( ts => ts.Field( f => f.ResourceForCollection ).Terms<int>( list.ToArray() ) );
					} );
			}

			return thisQuery;
		}

		/// <summary>
		/// Handle left rail filters of: In any collection, and search by name
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="query"></param>
		/// <returns></returns>
		public static QueryContainer CommonPartOfCollectionFilters<T>( MainSearchInput query ) where T : class, IIndex
		{
			//, ref QueryContainer widgetQuery
			QueryContainer thisQuery = null;
			QueryContainer qc = null;
			var tags = new List<string>();
			var reportSchemas = new List<string>();
			List<int> ids = new List<int>();

			//check for entered filters

			//In any collection check:
			if ( query.FiltersV2.Any( x => x.Name == "collections" || x.Name == "filter:InCollection" ) )
			{
				foreach ( var filter in query.FiltersV2.Where( x => x.Name == "collections" || x.Name == "filter:InCollection" ) )
				{
					var codeItem = filter.HasAnyValue();
					if ( codeItem.AnyValue || ( !string.IsNullOrWhiteSpace( codeItem.SchemaName ) && codeItem.SchemaName != "filter:InCollection" ) )
					{
						reportSchemas.Add( codeItem.SchemaName );
					}
					else
					{
						//obsolete approach?. Will the filter for a specific collection fall thru to here?
						string text = filter.AsText();
						if ( string.IsNullOrWhiteSpace( text ) )
							continue;
						tags.Add( text );
					}
				}
				if ( reportSchemas.Any() )
				{
					ids = CodesManager.GetEntityStatisticBySchema( reportSchemas );
					if ( ids.Any() )
					{
						thisQuery = Query<T>.Terms( ts => ts.Field( f => f.ReportFilters ).Terms<int>( ids.ToArray() ) );
					}
				}
			}


			if ( tags.Any() )
			{
				if ( reportSchemas.Any() ) //if there are concrete results, remove the Has any
					thisQuery = null;
				tags.ForEach( name =>
				{
					qc |= Query<T>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Collection.First(), 70 ) ).Type( TextQueryType.BestFields ).Query( name ) );
				} );
				//??
				if ( qc != null )
				{
					//???
					//thisQuery = Query<T>.Nested( n => n.Path( p => p.Collection ).Query( q => qc ).IgnoreUnmapped() );
					thisQuery = qc;
				}
			}

			//TBD
			if ( query.FiltersV2.Any( x => x.Name == "wcollection" ) )
			{

			}
			return thisQuery;
		}

		public static QueryContainer CommonPartOfTransferIntermediaryFilters<T>( MainSearchInput query ) where T : class, IIndex
		{
			//, ref QueryContainer widgetQuery
			QueryContainer thisQuery = null;
			QueryContainer qc = null;
			var tags = new List<string>();
			var reportSchemas = new List<string>();
			List<int> ids = new List<int>();

			//check for entered filters

			//In any collection check:
			if ( query.FiltersV2.Any( x => x.Name == "transfervalues" || x.Name == "filter:TransferValue" ) )
			{
				foreach ( var filter in query.FiltersV2.Where( x => x.Name == "transfervalues" || x.Name == "filter:TransferValue" ) )
				{
					var codeItem = filter.HasAnyValue();
					if ( codeItem.AnyValue || ( !string.IsNullOrWhiteSpace( codeItem.SchemaName ) && codeItem.SchemaName != "filter:TransferValue" ) )
					{
						reportSchemas.Add( codeItem.SchemaName );
					}
					else
					{
						//obsolete approach?. Will the filter for a specific collection fall thru to here?
						var text = filter.AsText();
						if ( string.IsNullOrWhiteSpace( text ) )
							continue;
						tags.Add( text );
					}
				}
				if ( reportSchemas.Any() )      //???
				{
					ids = CodesManager.GetEntityStatisticBySchema( reportSchemas );
					if ( ids.Any() )
					{
						thisQuery = Query<T>.Terms( ts => ts.Field( f => f.ReportFilters ).Terms<int>( ids.ToArray() ) );
					}
				}
			}


			if ( tags.Any() )
			{
				if ( reportSchemas.Any() ) //if there are concrete results, remove the Has any
					thisQuery = null;
				tags.ForEach( name =>
				{
					qc |= Query<T>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Collection.First(), 70 ) ).Type( TextQueryType.BestFields ).Query( name ) );
				} );
				//??
				if ( qc != null )
				{
					//???
					//thisQuery = Query<T>.Nested( n => n.Path( p => p.Collection ).Query( q => qc ).IgnoreUnmapped() );
					thisQuery = qc;
				}
			}

			return thisQuery;
		}

		public static QueryContainer CommonOccupations<T>( MainSearchInput query, ref QueryContainer widgetOccupationsQuery ) where T : class, IIndex
		{
			QueryContainer thisQuery = null;
			QueryContainer qc = null;
			var tags = new List<string>();
			var reportSchemas = new List<string>();
			List<int> ids = new List<int>();

			//check for entered filters
			if ( query.FiltersV2.Any( x => x.Name == "occupations" || x.Name == "filter:OccupationType" ) )
			{
				foreach ( var filter in query.FiltersV2.Where( x => x.Name == "occupations" || x.Name == "filter:OccupationType" ) )
				{
					var codeItem = filter.HasAnyValue();
					if ( codeItem.AnyValue || ( !string.IsNullOrWhiteSpace( codeItem.SchemaName ) && codeItem.SchemaName != "filter:OccupationType" ) )
					{
						reportSchemas.Add( codeItem.SchemaName );
					}
					else
					{
						var text = filter.AsText();
						if ( string.IsNullOrWhiteSpace( text ) ) continue;
						tags.Add( text );
					}
				}
				if ( reportSchemas.Any() )
				{
					ids = CodesManager.GetEntityStatisticBySchema( reportSchemas );
					if ( ids.Any() )
					{
						thisQuery = Query<T>.Terms( ts => ts.Field( f => f.ReportFilters ).Terms<int>( ids.ToArray() ) );
					}
				}
			}
			else if ( query.FiltersV2.Any( x => x.Name == "" && x.Values.Count > 0 ) )
			{
				foreach ( var filter in query.FiltersV2.Where( x => x.Name == "" && x.Values.Count > 0 ) )
				{
					var codeItem = filter.AsCodeItem();
					if ( codeItem.CategoryId == 11 )
					{
						if ( ( !string.IsNullOrWhiteSpace( codeItem.SchemaName ) && codeItem.SchemaName != "filter:OccupationType" ) )
						{
							reportSchemas.Add( codeItem.SchemaName );
						}
						else
						{
							var text = filter.AsText();
							if ( string.IsNullOrWhiteSpace( text ) )
								continue;
							tags.Add( text );
						}
					}
				}
				if ( reportSchemas.Any() )
				{
					ids = CodesManager.GetEntityStatisticBySchema( reportSchemas );
					if ( ids.Any() )
					{
						thisQuery = Query<T>.Terms( ts => ts.Field( f => f.ReportFilters ).Terms<int>( ids.ToArray() ) );
					}
				}
			}
			if ( !tags.Any() )
			{
				if ( UtilityManager.GetAppKeyValue( "occupationsUsingCodedNotationGuessing", false ) )
				{
					//TODO - with the addition of codedNotation to the top level occupations list, may not need this - only for specific codes?
					//if no occupation filters, check if can inject something from keywords )
					//loop thru. Note, if what appears to be a SOC is found, should it be removed from Keywords as not to affect the keyword search
					if ( query.FiltersV2 == null )
						query.FiltersV2 = new List<MainSearchFilterV2>();

					var modifiedKeywords = query.Keywords;
					//look for codedNotation specifically. Then occupation name in general
					//note the occupation search sends the text + code: Computer User Support Specialists (15-1151.00)
					foreach ( var s in query.Keywords.Split( ' ' ) )
					{
						//hack for testing
						if ( s.Trim().ToLower().IndexOf( "soc:" ) == 0 && s.IndexOf( "-" ) == 6 && s.Length == 11 )
						{
							//remove from keywords - only if full length
							modifiedKeywords = modifiedKeywords.Replace( s, "" );
							//codeList.Add( string.Format( "{0}-{1}", s.Substring( 4, 2 ), s.Substring( 6, 4 ) ) );
							tags.Add( string.Format( "{0}", s.Substring( 4, 7 ) ) );
						}
						else
						{
							//TODO - should matches be removed from keywords? what if will want to do the same for naics or cip
							if ( s.Length == 6 && ServiceHelper.IsInteger( s ) )
							{
								tags.Add( string.Format( "{0}-{1}", s.Substring( 0, 2 ), s.Substring( 2, 4 ) ) );
								modifiedKeywords = modifiedKeywords.Replace( s, "" );
							}
							//else if ( s.Length == 7 && s.IndexOf( "-" ) == 2 )
							//{
							//	tags.Add( s );
							//	modifiedKeywords = modifiedKeywords.Replace( s, "" );
							//}
							else if ( s.IndexOf( "-" ) == 2 && s.Length >= 7 && s.Length < 11 )
							{
								tags.Add( s );
								modifiedKeywords = modifiedKeywords.Replace( s, "" );
							}
							else
							{
								//value in individual words, vs whole phrase?
							}
						}
					}
					query.Keywords = modifiedKeywords;
					//could just throw the whole phrase?, if no codes
					//NO this could result in a problem, as would result in an AND with the others keyword filter
					//if ( tags.Count == 0 )
					//	tags.Add( query.Keywords.Trim() );
				}
			}

			if ( tags.Any() )
			{
				if ( reportSchemas.Any() ) //if there are concrete occupations, remove the Has any
					thisQuery = null;
				tags.ForEach( name =>
				{
					//21-07-30 mp - added specific codedNotation check with a high boost, started with PhrasePrefix
					//22-04-21 mp - does this handle where no dash included?
					var notation = UtilityManager.ExtractDelimitedValue( name );
					if ( !string.IsNullOrWhiteSpace( notation ) )
					{
						qc |= Query<T>.MultiMatch( m => m
						.Fields( mf => mf
							.Field( f => f
								.Occupations
									.First().CodedNotation, 100 ) )
										.Type( TextQueryType.PhrasePrefix )
											.Query( notation ) );
					}
					//qc |= Query<T>.MultiMatch( m => m
					//	.Fields( mf => mf
					//		.Field( f => f
					//			.Occupations
					//				.First().CodedNotation, 100 ) )
					//					.Type( TextQueryType.PhrasePrefix )
					//						.Query( name ) );
					qc |= Query<T>.MultiMatch( m => m
						.Fields( mf => mf
							.Field( f => f
								.Occupations
									.First().CodeTitle, 70 ) )
										.Type( TextQueryType.PhrasePrefix )
											.Query( name ) );
					qc |= Query<T>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Occupations.First().Name, 70 ) ).Type( TextQueryType.BestFields ).Query( name ) );
				} );
				if ( qc != null )
					thisQuery = Query<T>.Nested( n => n.Path( p => p.Occupations ).Query( q => qc ).IgnoreUnmapped() );
			}

			//TBD
			if ( query.FiltersV2.Any( x => x.Name == "woccupations" ) )
			{
				QueryContainer wqc = null;
				var occupationNames = new List<string>();
				var occupationSchemas = new List<string>();
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "woccupations" ) )
				{
					var codeItem = filter.HasAnyValue();
					if ( codeItem.AnyValue || !string.IsNullOrWhiteSpace( codeItem.SchemaName ) )
					{
						occupationSchemas.Add( codeItem.SchemaName );
					}
					else
					{
						var text = filter.AsText();
						if ( string.IsNullOrWhiteSpace( text ) )
							continue;
						occupationNames.Add( text );
					}
				}
				if ( occupationSchemas.Any() )
				{
					ids = CodesManager.GetEntityStatisticBySchema( occupationSchemas );
					if ( ids.Any() )
					{
						widgetOccupationsQuery = Query<T>.Terms( ts => ts.Field( f => f.ReportFilters ).Terms<int>( ids.ToArray() ) );
					}
				}
				occupationNames.ForEach( name =>
				{
					wqc |= Query<T>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Occupations.First().CodeTitle, 70 ) ).Type( TextQueryType.PhrasePrefix ).Query( name ) );
					wqc |= Query<T>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Occupations.First().Name, 70 ) ).Type( TextQueryType.BestFields ).Query( name ) );
				} );
				if ( wqc != null )
					widgetOccupationsQuery = Query<T>.Nested( n => n.Path( p => p.Occupations ).Query( q => qc ).IgnoreUnmapped() );
			}
			return thisQuery;
		}

		/// <summary>
		/// Not sure of the planned purpose of this, as not used?
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="query"></param>
		/// <param name="widgetOccupationsQuery"></param>
		/// <returns></returns>
		public static QueryContainer GetDistinctOccupations<T>( MainSearchInput query, ref QueryContainer widgetOccupationsQuery ) where T : class, IIndex
		{
			QueryContainer thisQuery = null;
			QueryContainer qc = null;
			var tags = new List<string>();
			var reportSchemas = new List<string>();
			var ids = new List<int>();
			//check for entered filters
			//if a full soc code nn-nnnn, then would only have one resule?
			if ( query.FiltersV2.Any( x => x.Name == "occupations" || x.Name == "filter:OccupationType" ) )
			{
				foreach ( var filter in query.FiltersV2.Where( x => x.Name == "occupations" || x.Name == "filter:OccupationType" ) )
				{
					var codeItem = filter.HasAnyValue();
					if ( codeItem.AnyValue || ( !string.IsNullOrWhiteSpace( codeItem.SchemaName ) && codeItem.SchemaName != "filter:OccupationType" ) )
					{
						reportSchemas.Add( codeItem.SchemaName );
					}
					else
					{
						var text = filter.AsText();
						if ( string.IsNullOrWhiteSpace( text ) ) continue;
						tags.Add( text );
					}
				}
				if ( reportSchemas.Any() )
				{
					ids = CodesManager.GetEntityStatisticBySchema( reportSchemas );
					if ( ids.Any() )
					{
						thisQuery = Query<T>.Terms( ts => ts.Field( f => f.ReportFilters ).Terms<int>( ids.ToArray() ) );
					}
				}
			}



			//if ( !tags.Any() )
			//{
			//	if ( UtilityManager.GetAppKeyValue( "occupationsUsingCodedNotationGuessing", false ) )
			//	{
			//		//TODO - with the addition of codedNotation to the top level occupations list, may not need this - only for specific codes?
			//		//if no occupation filters, check if can inject something from keywords )
			//		//loop thru. Note, if what appears to be a SOC is found, should it be removed from Keywords as not to affect the keyword search
			//		if ( query.FiltersV2 == null )
			//			query.FiltersV2 = new List<MainSearchFilterV2>();

			//		var modifiedKeywords = query.Keywords;
			//		//look for codedNotation specifically. Then occupation name in general
			//		//note the occupation search sends the text + code: Computer User Support Specialists (15-1151.00)
			//		foreach ( var s in query.Keywords.Split( ' ' ) )
			//		{
			//			//hack for testing
			//			if ( s.Trim().ToLower().IndexOf( "soc:" ) == 0 && s.IndexOf( "-" ) == 6 && s.Length == 11 )
			//			{
			//				//remove from keywords - only if full length
			//				modifiedKeywords = modifiedKeywords.Replace( s, "" );
			//				//codeList.Add( string.Format( "{0}-{1}", s.Substring( 4, 2 ), s.Substring( 6, 4 ) ) );
			//				tags.Add( string.Format( "{0}", s.Substring( 4, 7 ) ) );
			//			}
			//			else
			//			{
			//				//TODO - should matches be removed from keywords? what if will want to do the same for naics or cip
			//				if ( s.Length == 6 && ServiceHelper.IsInteger( s ) )
			//				{
			//					tags.Add( string.Format( "{0}-{1}", s.Substring( 0, 2 ), s.Substring( 2, 4 ) ) );
			//					modifiedKeywords = modifiedKeywords.Replace( s, "" );
			//				}
			//				//else if ( s.Length == 7 && s.IndexOf( "-" ) == 2 )
			//				//{
			//				//	tags.Add( s );
			//				//	modifiedKeywords = modifiedKeywords.Replace( s, "" );
			//				//}
			//				else if ( s.IndexOf( "-" ) == 2 && s.Length >= 7 && s.Length < 11 )
			//				{
			//					tags.Add( s );
			//					modifiedKeywords = modifiedKeywords.Replace( s, "" );
			//				}
			//				else
			//				{
			//					//value in individual words, vs whole phrase?
			//				}
			//			}
			//		}
			//		query.Keywords = modifiedKeywords;
			//		//could just throw the whole phrase?, if no codes
			//		//NO this could result in a problem, as would result in an AND with the others keyword filter
			//		//if ( tags.Count == 0 )
			//		//	tags.Add( query.Keywords.Trim() );
			//	}
			//}

			if ( tags.Any() )
			{
				if ( reportSchemas.Any() ) //if there are concrete occupations, remove the Has any
					thisQuery = null;
				tags.ForEach( name =>
				{
					//21-07-30 mp - added specific codedNotation check with a high boost, started with PhrasePrefix
					//22-04-21 mp - does this handle where no dash included?
					var notation = UtilityManager.ExtractDelimitedValue( name );
					if ( !string.IsNullOrWhiteSpace( notation ) )
					{
						qc |= Query<T>.MultiMatch( m => m
						.Fields( mf => mf
							.Field( f => f
								.Occupations
									.First().CodedNotation, 100 ) )
										.Type( TextQueryType.PhrasePrefix )
											.Query( notation ) );
					}

					qc |= Query<T>.MultiMatch( m => m
						.Fields( mf => mf
							.Field( f => f
								.Occupations
									.First().CodeTitle, 70 ) )
										.Type( TextQueryType.PhrasePrefix )
											.Query( name ) );
					qc |= Query<T>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Occupations.First().Name, 70 ) ).Type( TextQueryType.BestFields ).Query( name ) );
				} );
				if ( qc != null )
					thisQuery = Query<T>.Nested( n => n.Path( p => p.Occupations ).Query( q => qc ).IgnoreUnmapped() );
			}

			//TBD
			if ( query.FiltersV2.Any( x => x.Name == "woccupations" ) )
			{
				QueryContainer wqc = null;
				var occupationNames = new List<string>();
				var occupationSchemas = new List<string>();
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "woccupations" ) )
				{
					var codeItem = filter.HasAnyValue();
					if ( codeItem.AnyValue || !string.IsNullOrWhiteSpace( codeItem.SchemaName ) )
					{
						occupationSchemas.Add( codeItem.SchemaName );
					}
					else
					{
						var text = filter.AsText();
						if ( string.IsNullOrWhiteSpace( text ) )
							continue;
						occupationNames.Add( text );
					}
				}
				if ( occupationSchemas.Any() )
				{
					ids = CodesManager.GetEntityStatisticBySchema( occupationSchemas );
					if ( ids.Any() )
					{
						widgetOccupationsQuery = Query<T>.Terms( ts => ts.Field( f => f.ReportFilters ).Terms<int>( ids.ToArray() ) );
					}
				}
				occupationNames.ForEach( name =>
				{
					wqc |= Query<T>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Occupations.First().CodeTitle, 70 ) ).Type( TextQueryType.PhrasePrefix ).Query( name ) );
					wqc |= Query<T>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Occupations.First().Name, 70 ) ).Type( TextQueryType.BestFields ).Query( name ) );
				} );
				if ( wqc != null )
					widgetOccupationsQuery = Query<T>.Nested( n => n.Path( p => p.Occupations ).Query( q => qc ).IgnoreUnmapped() );
			}
			return thisQuery;
		}

		public static QueryContainer CommonIndustries<T>( MainSearchInput query, ref QueryContainer windustriesQuery ) where T : class, IIndex
		{
			QueryContainer thisQuery = null;
			var tags = new List<string>();
			List<int> ids = new List<int>();

			QueryContainer qc = null;
			var reportSchemas = new List<string>();
			if ( query.FiltersV2.Any( x => x.Name == "industries" || x.Name == "filter:IndustryType" ) )
			{

				foreach ( var filter in query.FiltersV2.Where( x => x.Name == "industries" || x.Name == "filter:IndustryType" ) )
				{
					var codeItem = filter.HasAnyValue();
					if ( codeItem.AnyValue || ( !string.IsNullOrWhiteSpace( codeItem.SchemaName ) && codeItem.SchemaName != "filter:IndustryType" ) )
					{
						reportSchemas.Add( codeItem.SchemaName );
					}
					else
					{
						var text = filter.AsText();
						if ( string.IsNullOrWhiteSpace( text ) )
							continue;
						tags.Add( text );
					}
				}
				if ( reportSchemas.Any() )
				{
					ids = CodesManager.GetEntityStatisticBySchema( reportSchemas );
					if ( ids.Any() )
					{
						thisQuery = Query<T>.Terms( ts => ts.Field( f => f.ReportFilters ).Terms<int>( ids.ToArray() ) );
					}
				}
			}
			else if ( query.FiltersV2.Any( x => x.Name == "" && x.Values.Count > 0 ) )
			{
				foreach ( var filter in query.FiltersV2.Where( x => x.Name == "" && x.Values.Count > 0 ) )
				{
					var codeItem = filter.AsCodeItem();
					if ( codeItem.CategoryId == 10 )
					{
						if ( ( !string.IsNullOrWhiteSpace( codeItem.SchemaName ) && codeItem.SchemaName != "filter:IndustryType" ) )
						{
							reportSchemas.Add( codeItem.SchemaName );
						}
						else
						{
							var text = filter.AsText();
							if ( string.IsNullOrWhiteSpace( text ) )
								continue;
							tags.Add( text );
						}
					}
				}
				if ( reportSchemas.Any() )
				{
					ids = CodesManager.GetEntityStatisticBySchema( reportSchemas );
					if ( ids.Any() )
					{
						thisQuery = Query<T>.Terms( ts => ts.Field( f => f.ReportFilters ).Terms<int>( ids.ToArray() ) );
					}
				}
			}
			//
			if ( tags.Any() )
			{
				if ( reportSchemas.Any() ) //if there are concrete tags, remove the Has any
					thisQuery = null;

				tags.ForEach( name =>
				{
					var notation = UtilityManager.ExtractDelimitedValue( name );
					if ( !string.IsNullOrWhiteSpace( notation ) )
					{
						qc |= Query<T>.MultiMatch( m => m
						.Fields( mf => mf
							.Field( f => f
								.Industries
									.First().CodedNotation, 100 ) )
										.Type( TextQueryType.PhrasePrefix )
											.Query( notation ) );
					}
					qc |= Query<T>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Industries.First().CodeTitle, 70 ) ).Type( TextQueryType.PhrasePrefix ).Query( name ) );
					qc |= Query<T>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Industries.First().Name, 70 ) ).Type( TextQueryType.BestFields ).Query( name ) );
				} );

				if ( qc != null )
					thisQuery = Query<T>.Nested( n => n.Path( p => p.Industries ).Query( q => qc ).IgnoreUnmapped() );
			}
			//TBD
			if ( query.FiltersV2.Any( x => x.Name == "windustries" ) )
			{
				QueryContainer wqc = null;
				var industryNames = new List<string>();
				var industrySchemas = new List<string>();
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "windustries" ) )
				{
					var codeItem = filter.HasAnyValue();
					if ( codeItem.AnyValue || !string.IsNullOrWhiteSpace( codeItem.SchemaName ) )
					{
						industrySchemas.Add( codeItem.SchemaName );
					}
					else
					{
						var text = filter.AsText();
						if ( string.IsNullOrWhiteSpace( text ) )
							continue;
						industryNames.Add( text );
					}
				}
				if ( industrySchemas.Any() )
				{
					ids = CodesManager.GetEntityStatisticBySchema( industrySchemas );
					if ( ids.Any() )
					{
						thisQuery = Query<T>.Terms( ts => ts.Field( f => f.ReportFilters ).Terms<int>( ids.ToArray() ) );
					}
				}
				industryNames.ForEach( name =>
				{
					wqc |= Query<T>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Industries.First().CodeTitle, 70 ) ).Type( TextQueryType.PhrasePrefix ).Query( name ) );
					wqc |= Query<T>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Industries.First().Name, 70 ) ).Type( TextQueryType.BestFields ).Query( name ) );
				} );

				if ( wqc != null )
					windustriesQuery = Query<T>.Nested( n => n.Path( p => p.Industries ).Query( q => wqc ).IgnoreUnmapped() );
			}
			return thisQuery;
		}
		public static QueryContainer CommonCip<T>( MainSearchInput query ) where T : class, IIndex
		{

			QueryContainer thisQuery = null;
			QueryContainer qc = null;
			var tags = new List<string>();
			List<int> ids = new List<int>();
			var reportSchemas = new List<string>();
			if ( query.FiltersV2.Any( x => x.Name == "instructionalprogramtypes" || x.Name == "filter:InstructionalProgramType" ) )
			{
				foreach ( var filter in query.FiltersV2.Where( x => x.Name == "instructionalprogramtypes" || x.Name == "filter:InstructionalProgramType" ) )
				{
					var codeItem = filter.HasAnyValue();
					if ( codeItem.AnyValue || ( !string.IsNullOrWhiteSpace( codeItem.SchemaName ) && codeItem.SchemaName != "filter:InstructionalProgramType" ) )
					{
						reportSchemas.Add( codeItem.SchemaName );
					}
					else
					{
						var text = filter.AsText();
						if ( string.IsNullOrWhiteSpace( text ) )
							continue;
						tags.Add( text );
					}
				}
				if ( reportSchemas.Any() )
				{
					ids = CodesManager.GetEntityStatisticBySchema( reportSchemas );
					if ( ids.Any() )
					{
						thisQuery = Query<T>.Terms( ts => ts.Field( f => f.ReportFilters ).Terms<int>( ids.ToArray() ) );
					}
				}


			}
			else if ( query.FiltersV2.Any( x => x.Name == "" && x.Values.Count > 0 ) )
			{
				foreach ( var filter in query.FiltersV2.Where( x => x.Name == "" && x.Values.Count > 0 ) )
				{
					var codeItem = filter.AsCodeItem();
					if ( codeItem.CategoryId == 23 )
					{
						if ( ( !string.IsNullOrWhiteSpace( codeItem.SchemaName ) && codeItem.SchemaName != "filter:InstructionalProgramType" ) )
						{
							reportSchemas.Add( codeItem.SchemaName );
						}
						else
						{
							var text = filter.AsText();
							if ( string.IsNullOrWhiteSpace( text ) )
								continue;
							tags.Add( text );
						}
					}
				}
				if ( reportSchemas.Any() )
				{
					ids = CodesManager.GetEntityStatisticBySchema( reportSchemas );
					if ( ids.Any() )
					{
						thisQuery = Query<T>.Terms( ts => ts.Field( f => f.ReportFilters ).Terms<int>( ids.ToArray() ) );
					}
				}
			}

			if ( !tags.Any() )
			{
				if ( UtilityManager.GetAppKeyValue( "cipUsingCodedNotationGuessing", false ) )
				{

					//if no occupation filters, check if can inject something from keywords )
					//loop thru. Note, if what appears to be a SOC is found, should it be removed from Keywords as not to affect the keyword search
					if ( query.FiltersV2 == null )
						query.FiltersV2 = new List<MainSearchFilterV2>();

					var modifiedKeywords = query.Keywords;
					//look for codedNotation specifically. 
					foreach ( var s in query.Keywords.Split( ' ' ) )
					{
						//hack for testing. assume no dash for cip
						if ( s.Trim().ToLower().IndexOf( "cip:" ) == 0 && s.Length == 10 )
						{
							//remove from keywords - only if full length
							modifiedKeywords = modifiedKeywords.Replace( s, "" );
							tags.Add( string.Format( "{0}", s.Substring( 4, 6 ) ) );
						}
						else
						{
							//TODO - should matches be removed from keywords? whatif will want to do the same for naics or cip
							//if ( s.Length == 6 && ServiceHelper.IsInteger( s ) )
							//{
							//	tags.Add( string.Format( "{0}-{1}", s.Substring( 0, 2 ), s.Substring( 2, 4 ) ) );
							//	//modifiedKeywords = modifiedKeywords.Replace( s, "" );
							//}
							//else if ( s.IndexOf( "-" ) == 2 && s.Length >= 7 && s.Length < 11 )
							//{
							//	tags.Add( s );
							//	//modifiedKeywords = modifiedKeywords.Replace( s, "" );
							//}
						}
					}
					query.Keywords = modifiedKeywords;
				}
			}

			//
			if ( tags.Any() )
			{
				if ( reportSchemas.Any() ) //if there are concrete tags, remove the Has any
					thisQuery = null;

				tags.ForEach( name =>
				{
					var notation = UtilityManager.ExtractDelimitedValue( name );
					if ( !string.IsNullOrWhiteSpace( notation ) )
					{
						qc |= Query<T>.MultiMatch( m => m
						.Fields( mf => mf
							.Field( f => f
								.InstructionalPrograms
									.First().CodedNotation, 100 ) )
										.Type( TextQueryType.PhrasePrefix )
											.Query( notation ) );
					}
					qc |= Query<T>.MultiMatch( m => m
						.Fields( mf => mf
							.Field( f => f
								.InstructionalPrograms.First().CodeTitle, 70 ) )
									.Type( TextQueryType.PhrasePrefix )
										.Query( name ) );
					qc |= Query<T>.MultiMatch( m => m
						.Fields( mf => mf
							.Field( f => f.InstructionalPrograms.First().Name, 70 ) )
								.Type( TextQueryType.BestFields )
									.Query( name ) );

				} );

				if ( qc != null )
					thisQuery = Query<T>.Nested( n => n.Path( p => p.InstructionalPrograms ).Query( q => qc ).IgnoreUnmapped() );
			}
			return thisQuery;
		}

		public static QueryContainer CommonOutcomeProviders<T>( MainSearchInput query ) where T : class, IIndex
		{
			QueryContainer thisQuery = null;
			if ( query.FiltersV2 == null || query.FiltersV2.Count == 0 )
			{
				return null;
			}
			var selected = new List<CodeItem>();
			if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CUSTOM ) )
			{
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "outcomeproviders" ).ToList() )
				{
					//should be OK using this method to get the orgId
					selected.Add( filter.AsOrgRolesItem() );
				}

				if ( selected.Any() )
					selected.ForEach( x =>
					{
						thisQuery |= Query<T>.Nested( n => n
							   .Path( p => p.OutcomeProvidersForEntity )
								.Query( q => q
									.Bool( mm => mm
										 .Must( mu => mu.Terms( m => m.Field( f => f.OutcomeProvidersForEntity.First().OrgId ).Terms( x.Id )
											   )
											   )//Must
										   )//Bool
								   ) //Query
							   );
					} );
			}

			return thisQuery;
		}

		public static QueryContainer CommonHasSupportService<T>( MainSearchInput query ) where T : class, IIndex
		{
			QueryContainer thisQuery = null;
			if ( query.FiltersV2 == null || query.FiltersV2.Count == 0 )
			{
				return null;
			}
			var selected = new List<CodeItem>();
			if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CUSTOM ) )
			{
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "outcomeproviders" ).ToList() )
				{
					//should be OK using this method to get the orgId
					selected.Add( filter.AsOrgRolesItem() );
				}

				if ( selected.Any() )
					selected.ForEach( x =>
					{
						thisQuery |= Query<T>.Nested( n => n
							   .Path( p => p.OutcomeProvidersForEntity )
								.Query( q => q
									.Bool( mm => mm
										 .Must( mu => mu.Terms( m => m.Field( f => f.OutcomeProvidersForEntity.First().OrgId ).Terms( x.Id )
											   )
											   )//Must
										   )//Bool
								   ) //Query
							   );
					} );
			}

			return thisQuery;
		}

		#region Common method for organization filtering
		/// <summary>
		/// the various org filters are a bit murky. Setting up new specifically for left rail filters, and then see if we can merge others
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="query"></param>
		/// <param name="relationshipTypeIds">Not sure how these will be used, as the new left rail query will not have a rel type</param>
		/// <returns></returns>
		public static QueryContainer CommonNonQAOrgRolesLeftRailFilter<T>( MainSearchInput query, List<int> relationshipTypeIds = null  ) where T : class, IIndex
		{
			QueryContainer thisQuery = null;
			if ( query.FiltersV2 == null || query.FiltersV2.Count == 0 )
			{
				return null;
			}
			var roles = new List<CodeItem>();

			if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CODE ) )
			{
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "filter:organizationnonqaroles" ).ToList() )
				{
					CodeItem ci = filter.AsCodeItem();
					if ( ci != null && ci.Id > 0 )
						roles.Add( filter.AsCodeItem() );    //codeIds
				}
				//

				if ( roles.Any() )
				{
					if ( relationshipTypeIds == null || relationshipTypeIds.Count() == 0 )
					{
						//ensure only targets Non-QA roles	(then why is this method named: CommonOrgQARolesFilter)
						relationshipTypeIds.AddRange( new List<int>() { 6, 7, Entity_AgentRelationshipManager.ROLE_TYPE_Revokes, Entity_AgentRelationshipManager.ROLE_TYPE_Renews } );
					}

					roles.ForEach( x =>
					{
						thisQuery |= Query<T>.Nested( n => n
							   .Path( p => p.AgentRelationshipsForEntity )
								.Query( q => q
									.Bool( mm => mm
										 .Must( mu => mu.Terms( m => m.Field( f => f.AgentRelationshipsForEntity.First().RelationshipTypeIds ).Terms( relationshipTypeIds )
													)
												&& mu.Terms( m => m.Field( f => f.AgentRelationshipsForEntity.First().OrgId ).Terms( x.Id )
												)
										)//Must
									) //Bool
								) //Query
							); //Nested
					} );
				}

			}

			return thisQuery;
		}


		/// <summary>
		/// query orgs that have resources that have transfer value for or from
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="query"></param>
		/// <param name="entityTypeIds">Future for to filter on the individual target entity types</param>
		/// <returns></returns>
		public static QueryContainer TVPOrgsWithHasTVPForFilter( MainSearchInput query, List<int> entityTypeIds ) 
		{
			QueryContainer thisQuery = null;
			if ( query.FiltersV2 == null || query.FiltersV2.Count == 0 )
			{
				return null;
			}
			var filters = new List<CodeItem>();
			List<int> includedEntityTypeIds = new List<int>();
			if ( query.FiltersV2.Any( x => x.Name == "filter:transferValueFor" ) )
			{
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "filter:transferValueFor" ).ToList() )
				{
					CodeItem ci = filter.AsCodeItem();
					if ( ci != null && ci.Id > 0 )
					{
						filters.Add( ci );    //codeIds
						if (ci.EntityTypeId > 0)	//future to add list of entity types to UI
							includedEntityTypeIds.Add(ci.EntityTypeId );
					}
				}

				if ( filters.Any() )
					if ( includedEntityTypeIds.Any() )
					{
						filters.ForEach( x =>
						{
							thisQuery |= Query<GenericIndex>.Nested( n => n
								   .Path( p => p.TransferValueFor )
									.Query( q => q
										.Bool( mm => mm
											 .Must( mu => mu.Terms( m => m.Field( f => f.TransferValueFor.First().EntityTypeId ).Terms( includedEntityTypeIds )
														)
													&& mu.Terms( m => m.Field( f => f.TransferValueFor.First().OrganizationId ).Terms( x.Id )
													)
											)//Must
										) //Bool
									) //Query
								); //Nested
						} );
					}
					else
					{
						filters.ForEach( x =>
						{
							thisQuery |= Query<GenericIndex>.Nested( n => n
								   .Path( p => p.TransferValueFor )
									.Query( q => q
										.Bool( mm => mm
											 .Must( mu => mu.Terms( m => m.Field( f => f.TransferValueFor.First().OrganizationId ).Terms( x.Id )
													)
											)//Must
										) //Bool
									) //Query
								); //Nested
						} );
					}
			}

		

			return thisQuery;
		}
		public static QueryContainer TVPOrgsWithHasTVPFromFilter( MainSearchInput query, List<int> entityTypeIds )
		{
			QueryContainer thisQuery = null;
			if ( query.FiltersV2 == null || query.FiltersV2.Count == 0 )
			{
				return null;
			}
			var filters = new List<CodeItem>();
			List<int> includedEntityTypeIds = new List<int>();

			if ( query.FiltersV2.Any( x => x.Name == "filter:transferValueFrom" ) )
			{
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "filter:transferValueFrom" ).ToList() )
				{
					CodeItem ci = filter.AsCodeItem();
					if ( ci != null && ci.Id > 0 )
					{
						filters.Add( ci );    //codeIds
						if ( ci.EntityTypeId > 0 )
							includedEntityTypeIds.Add( ci.EntityTypeId );
					}
				}

				if ( filters.Any() )
				{
					//these might be part of the filter, not separate?
					if ( entityTypeIds == null || entityTypeIds.Count() == 0 )
					{
						//include all expected targets
						//24-04-09 MP - not sure EntityTypeId is being populated
						//		added zero for testing
						entityTypeIds.AddRange( new List<int>() { 0, 1, 3, 7, 36, 37, CodesManager.ENTITY_TYPE_COMPETENCY } );
					}

					if ( includedEntityTypeIds.Any() )
					{
						filters.ForEach( x =>
						{
							thisQuery |= Query<GenericIndex>.Nested( n => n
								   .Path( p => p.TransferValueFrom )
									.Query( q => q
										.Bool( mm => mm
											 .Must( mu => mu.Terms( m => m.Field( f => f.TransferValueFrom.First().EntityTypeId ).Terms( includedEntityTypeIds )
														)
													&& mu.Terms( m => m.Field( f => f.TransferValueFrom.First().OrganizationId ).Terms( x.Id )
													)
											)//Must
										) //Bool
									) //Query
								); //Nested
						} );
					} else
					{
						filters.ForEach( x =>
						{
							thisQuery |= Query<GenericIndex>.Nested( n => n
								   .Path( p => p.TransferValueFrom )
									.Query( q => q
										.Bool( mm => mm
											 .Must( mu => mu.Terms( m => m.Field( f => f.TransferValueFrom.First().OrganizationId ).Terms( x.Id )
													)
											)//Must
										) //Bool
									) //Query
								); //Nested
						} );
					}
				}

			}

			return thisQuery;
		}

		/// <summary>
		/// Document this
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="query"></param>
		/// <param name="relationshipTypeIds"></param>
		/// <returns></returns>
		public static QueryContainer CommonNonQAOrgRolesFilter<T>( MainSearchInput query, List<int> relationshipTypeIds ) where T : class, IIndex
		{
			QueryContainer thisQuery = null;
			if ( query.FiltersV2 == null || query.FiltersV2.Count == 0 || query.FiltersV2.Any( m => m.Name == "organizationnonqaroles" || m.Name == "filter:nonQARoles" ) == false )
			{
				return null;
			}
			var roles = new List<CodeItem>();

			//TODO - document this path
			if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.TEXT ) )
			{
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "organizationnonqaroles" || m.Name == "filter:nonQARoles" ).ToList() )
				{
					CodeItem ci = filter.AsCodeItem();
					if ( ci != null && ci.Id > 0 )
						roles.Add( filter.AsCodeItem() );    //codeIds
				}

				if ( roles.Any() )
				{
					if ( relationshipTypeIds == null || relationshipTypeIds.Count() == 0 )
					{
						//ensure only targets Non-QA roles	(then why is this method named: CommonOrgQARolesFilter)
						relationshipTypeIds.AddRange( new List<int>() { 6, 7, 8, Entity_AgentRelationshipManager.ROLE_TYPE_Revokes, Entity_AgentRelationshipManager.ROLE_TYPE_Renews } );
					}

					roles.ForEach( x =>
					{
						thisQuery |= Query<T>.Nested( n => n
							   .Path( p => p.AgentRelationshipsForEntity )
								.Query( q => q
									.Bool( mm => mm
										 .Must( mu => mu.Terms( m => m.Field( f => f.AgentRelationshipsForEntity.First().RelationshipTypeIds ).Terms( relationshipTypeIds )
													)
												&& mu.Terms( m => m.Field( f => f.AgentRelationshipsForEntity.First().OrgId ).Terms( x.Id )
												)
										)//Must
									) //Bool
								) //Query
							); //Nested
					} );
				}

			}
			else if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CODE ) )
			{
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "filter:organizationnonqaroles" ).ToList() )
				{
					CodeItem ci = filter.AsCodeItem();
					//TODO - handle partial name??
					if ( ci != null && (ci.Id > 0 )|| !string.IsNullOrWhiteSpace(ci.Name) )
						roles.Add( filter.AsCodeItem() );    //codeIds
				}
				//

				if ( roles.Any() )
				{
					if ( relationshipTypeIds == null || relationshipTypeIds.Count() == 0 )
					{
						//ensure only targets Non-QA roles	(then why is this method named: CommonOrgQARolesFilter)
						//24-03-12 mp - added provides (8) 
						relationshipTypeIds.AddRange( new List<int>() { 6, 7, 8, Entity_AgentRelationshipManager.ROLE_TYPE_Revokes, Entity_AgentRelationshipManager.ROLE_TYPE_Renews } );
					}

					roles.ForEach( x =>
					{
						if ( x.Id > 0 )
						{
							thisQuery |= Query<T>.Nested( n => n
								   .Path( p => p.AgentRelationshipsForEntity )
									.Query( q => q
										.Bool( mm => mm
											 .Must( mu => mu.Terms( m => m.Field( f => f.AgentRelationshipsForEntity.First().RelationshipTypeIds ).Terms( relationshipTypeIds )
														)
													&& mu.Terms( m => m.Field( f => f.AgentRelationshipsForEntity.First().OrgId ).Terms( x.Id )
													)
											)//Must
										) //Bool
									) //Query
								); //Nested
						}
						else if ( !string.IsNullOrWhiteSpace( x.Name ) )
						{
							thisQuery |= Query<T>.Nested( n => n
								   .Path( p => p.AgentRelationshipsForEntity )
									.Query( q => q
										.Bool( mm => mm
											 .Must( mu => mu.Terms( m => m.Field( f => f.AgentRelationshipsForEntity.First().RelationshipTypeIds ).Terms( relationshipTypeIds )
														)
													&& mu.Terms( m => m.Field( f => f.AgentRelationshipsForEntity.First().AgentName ).Terms( x.Name )
													)
											)//Must
										) //Bool
									) //Query
								); //Nested
						}
					} );
				}

			}
			//**** should only be used in widget mode confirm WidgetId has been populated
			//MAYBE NOT => also via the owns/offers link from a detail page
			else if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CUSTOM ) && query?.WidgetId > 0 )
			{
				/*
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "organizationroles" ).ToList() )
				{
					CodeItem ci = filter.AsOrgRolesItem();
					if ( ci != null && ci.Id > 0 )
						roles.Add( filter.AsCodeItem() );    //codeIds
				}

				if ( roles.Any() )
				{
					//no defaults here, or could be 30
					//if ( relationshipTypeIds == null || relationshipTypeIds.Count() == 0 )
					//{
					//	//ensure only targets Non-QA roles
					//	relationshipTypeIds.AddRange( new List<int>() { 6, 7, Entity_AgentRelationshipManager.ROLE_TYPE_Revokes, Entity_AgentRelationshipManager.ROLE_TYPE_Renews } );
					//}

					roles.ForEach( x =>
					{
						thisQuery |= Query<T>.Nested( n => n
							   .Path( p => p.AgentRelationshipsForEntity )
								.Query( q => q
									.Bool( mm => mm
										 .Must( mu => mu.Terms( m => m.Field( f => f.AgentRelationshipsForEntity.First().RelationshipTypeIds ).Terms( relationshipTypeIds )
													)
												&& mu.Terms( m => m.Field( f => f.AgentRelationshipsForEntity.First().OrgId ).Terms( x.Id )
												)
										)//Must
									) //Bool
								) //Query
							); //Nested
					} );
				}
				*/
			}
			else
			{
				//make sure relationships are for this query type
				var exists = query.FiltersV2.Where( m => m.Name == "organizationnonqaroles" ).ToList();
				if ( exists != null && exists.Any() )
				{
					//if no TEXT filter, then just use the relationship codes
					thisQuery = Query<T>.Nested( n => n.Path( p => p.AgentRelationshipsForEntity )
								.Query( q =>
									q.Terms( t =>
										t.Field( f => f.AgentRelationshipsForEntity.First().RelationshipTypeIds )
											.Terms<int>( relationshipTypeIds.ToArray() ) ) )
										);
				}
			}
			return thisQuery;
		}

		/// <summary>
		/// Used primarily from a gray box search such as show where this qa org does this QA
		/// AND for searches from the detail page (owns/offers and publishedby)
		/// 2019-April - also used for widget filters 
		/// </summary>
		/// <typeparam name="T">Current Elastic Index</typeparam>
		/// <param name="query"></param>
		/// <returns></returns>
		public static QueryContainer CommonOrgRolesByFilter<T>( MainSearchInput query ) where T : class, IIndex
		{
			QueryContainer thisQuery = null;
			if ( query.FiltersV2.Any( m => m.Name == "organizationroles" || m.Name == "filter:organizationnonqaroles" ) == false )
			{
				return null;
			}
			var assurances = new List<CodeItem>();
			var publishedBy = new List<CodeItem>();

			//via the owns/offers link from a detail page
			if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CUSTOM ) )
			{
				//TBD on handling  || m.Name == "filter:nonQARoles" here
				var targetQuery = query.FiltersV2.Where( m => m.Name == "organizationroles" || m.Name == "filter:organizationnonqaroles" ).ToList();

				foreach ( var filter in targetQuery )
				{
					var orgFilter = filter.AsOrgRolesItem();
					//maybe better to just use a separate query for publisher
					if ( orgFilter.IdsList.Contains( 30 ) )
					{
						//23-08-14 mp - now will basically ignore publishedBy here
						publishedBy.Add( orgFilter );
					}
					else
					{
						assurances.Add( orgFilter );
					}
				}

				if ( assurances.Any() )
					assurances.ForEach( x =>
					{
						//need to do ANDs. 23-08-14 - back to an OR
						thisQuery |= Query<T>.Nested( n => n
							   .Path( p => p.AgentRelationshipsForEntity )
								.Query( q => q
									.Bool( mm => mm
										 .Must( mu => mu.Terms( m => m.Field( f => f.AgentRelationshipsForEntity.First().RelationshipTypeIds ).Terms( x.IdsList )
												   )
											   && mu.Terms( m => m.Field( f => f.AgentRelationshipsForEntity.First().OrgId ).Terms( x.Id )
											   )
											   )//Must
										   )//Bool
								   ) //Query
							   );
					} );
			}

			return thisQuery;
		}

		public static QueryContainer PublishedByFilter<T>( MainSearchInput query ) where T : class, IIndex
		{
			QueryContainer thisQuery = null;
			var publishedBy = new List<CodeItem>();
			if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CUSTOM ) )
			{
				var targetQuery = query.FiltersV2.Where( m => m.Name == "organizationroles" ).ToList();

				foreach ( var filter in targetQuery )
				{
					var orgFilter = filter.AsOrgRolesItem();
					//hmm actually what if more than 30?
					if ( orgFilter.IdsList.Contains( 30 ) )
					{
						publishedBy.Add( orgFilter );
					}
				}

				if ( publishedBy.Any() )
					publishedBy.ForEach( x =>
					{
						//need to do ANDs. 23-08-14
						thisQuery &= Query<T>.Nested( n => n
							   .Path( p => p.AgentRelationshipsForEntity )
								.Query( q => q
									.Bool( mm => mm
										 .Must( mu => mu.Terms( m => m.Field( f => f.AgentRelationshipsForEntity.First().RelationshipTypeIds ).Terms( x.IdsList )
												   )
											   && mu.Terms( m => m.Field( f => f.AgentRelationshipsForEntity.First().OrgId ).Terms( x.Id )
											   )
											   )//Must
										   )//Bool
								   ) //Query
							   );
					} );
			}
			return thisQuery;
		}

		/// <summary>
		/// Used from the Quality Assurance filter section.
		/// 24-04-15 mp - not working where just selecting a QA type. Seems to only work if an orgId was included
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="query"></param>
		/// <param name="relationshipTypeIds">If this has data, should signal to filter even if no orgId</param>
		/// <returns></returns>
		public static QueryContainer CommonQualityAssuranceFilter<T>( MainSearchInput query, List<int> relationshipTypeIds ) where T : class, IIndex
		{
			QueryContainer thisQuery = null;
			if ( query.FiltersV2.Any( m => m.Name == "qualityassurance" || m.Name == "filter:QAReceived" ) == false )
			{
				return null;
			}
			var assurances = new List<CodeItem>();
			//21-08-30 mp - check if this will work with a mix of with an orgId and without! It does seem so, although not sure until FTS is working. 
			if ( relationshipTypeIds == null || relationshipTypeIds.Count() == 0 )
			{
				//ensure only targets QA roles
				relationshipTypeIds.AddRange( new List<int>() { 1, 2, Entity_AgentRelationshipManager.ROLE_TYPE_RecognizedBy, Entity_AgentRelationshipManager.ROLE_TYPE_RegulatedBy } );
			}
			//var foundQAFilter = false;
			if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.TEXT ) )
			{
				bool hasId = false;
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "qualityassurance" || m.Name == "filter:QAReceived" ).ToList() )
				{
					CodeItem ci = filter.AsQaText();
					if ( ci != null && ( ci.Id > 0 || !string.IsNullOrWhiteSpace( ci.Name ) ) )
					{
						if ( ci.Id > 0 )
							hasId = true;
						//temp HACK have to handle: { label = Higher Learning Commission (HLC), value = 960 }
						if ( ci.Name.IndexOf( "{ label =" ) > -1 )
						{
							var value = UtilityManager.ExtractNameValue( ci.Name, "value ", "=", "}" );
							if ( !string.IsNullOrWhiteSpace( value ) )
							{
								if ( int.TryParse( value, out int cid ) )
								{
									//ci.Id = cid;
									filter.Values["Id"] = cid.ToString();
									hasId = true;
								}
							}
							//
							var name = UtilityManager.ExtractNameValue( ci.Name, "{ label ", "=", "value" );
							if ( !string.IsNullOrWhiteSpace( name ) )
							{
								filter.Values["TextValue"] = name.Trim().TrimEnd( ',' );
								//ci.Name = name.Trim().TrimEnd( ',' );
							}

						}
						assurances.Add( filter.AsQaText() );    //getting orgIds
					}
				}

				if ( assurances.Any() )
				{
					//21-08-30 mp - check if this will work with a mix of with an orgId and without! It does seem so, although not sure until FTS is working. 
					//if ( relationshipTypeIds == null || relationshipTypeIds.Count() == 0 )
					//{
					//	//ensure only targets QA roles
					//	relationshipTypeIds.AddRange( new List<int>() { 1, 2, Entity_AgentRelationshipManager.ROLE_TYPE_RecognizedBy, Entity_AgentRelationshipManager.ROLE_TYPE_RegulatedBy } );
					//}

					if ( hasId )
					{
						assurances.ForEach( x =>
						{
							thisQuery |= Query<T>.Nested( n => n
								   .Path( p => p.AgentRelationshipsForEntity )
									.Query( q => q
										.Bool( mm => mm
											 .Must( mu => mu
												.Terms( m => m
													.Field( f => f.AgentRelationshipsForEntity.First().RelationshipTypeIds ).Terms( relationshipTypeIds )
														)
													&& mu.Terms( m => m.Field( f => f.AgentRelationshipsForEntity.First().OrgId ).Terms( x.Id )
													)
											)//Must
										) //Bool
									) //Query
								); //Nested
						} );
					} else
					{
						//21-05-10 mparsons - search on name only not working. Try modelling after competencies
						//			LATER - for now a name must be selected
						assurances.ForEach( x =>
						{
							thisQuery |= Query<T>.Nested( n => n
								   .Path( p => p.AgentRelationshipsForEntity )
									.Query( q => q
										.Bool( mm => mm
											 .Must( mu => mu
												.Terms( m => m
													.Field( f => f.AgentRelationshipsForEntity.First().RelationshipTypeIds ).Terms( relationshipTypeIds ) )
													//&& mu.Terms( m => m.Field( f => f.AgentRelationshipsForEntity.First().AgentName ).Terms( x.Name )
													//)
													&& mu.MultiMatch( m => m
														.Fields( mf => mf
															.Field( f => f.AgentRelationshipsForEntity.First().AgentName, 70 ) ).Type( TextQueryType.Phrase ).Query( x.Name ) )
											)//Must
										) //Bool
									) //Query
								); //Nested
						} );
						//assurances.ForEach( x =>
						//{
						//	thisQuery |= Query<T>.Nested( n => n
						//		   .Path( p => p.AgentRelationshipsForEntity )
						//			.Query( q => q
						//				.Bool( mm => mm
						//					 .Must( mu => mu
						//						.Terms( m => m
						//							.Field( f => f.AgentRelationshipsForEntity.First().RelationshipTypeIds ).Terms( relationshipTypeIds ) )
						//					&& mu
						//						.Terms( m => m.Field( f => f.AgentRelationshipsForEntity.First().AgentName ).Terms( x.Name )
						//							)
						//					)//Must
						//				) //Bool
						//			) //Query
						//		); //Nested
						//} );
					}
				}
				else
				{
					//do we need a case where there was a TEXT filter, but no qualityassurance, and we have relationships?
				}
			}
			//24-02-20 mparsons - the left rail QA filter uses CODE, how could this have ever worked (where the org text is included?
			//	issue: have type code with one qa relationship, but no org, so don't want this step
			else if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CODE ) )
			{
				bool hasOrgId = false;
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "qualityassurance" || m.Name == "filter:QAReceived" ).ToList() )
				{
					//the purpose of this is to get an orgId. If the Id < 15, skip
					CodeItem ci = filter.AsQaText();
					if ( ci != null && ( ci.Id > 0 || !string.IsNullOrWhiteSpace( ci.Name ) ) )
					{
						//for left rail filter, we are checking for an orgId, skip anything that looks like a QA relationship
						if ( ci.Id > 15 )
							hasOrgId = true;
						//temp HACK have to handle: { label = Higher Learning Commission (HLC), value = 960 }
						if ( ci.Name.IndexOf( "{ label =" ) > -1 )
						{
							var value = UtilityManager.ExtractNameValue( ci.Name, "value ", "=", "}" );
							if ( !string.IsNullOrWhiteSpace( value ) )
							{
								if ( int.TryParse( value, out int cid ) )
								{
									if ( cid > 15 )
									{
										//ci.Id = cid;
										filter.Values["Id"] = cid.ToString();
										hasOrgId = true;
									}
								}
							}
							//
							var name = UtilityManager.ExtractNameValue( ci.Name, "{ label ", "=", "value" );
							if ( !string.IsNullOrWhiteSpace( name ) )
							{
								filter.Values["TextValue"] = name.Trim().TrimEnd( ',' );
								//ci.Name = name.Trim().TrimEnd( ',' );
							}

						}
						//if ( hasId )
						if ( ci.Id > 15 )
							assurances.Add( filter.AsQaText() );    //getting orgIds
					}
				}

				if ( assurances.Any() )
				{
					if ( hasOrgId )
					{
						assurances.ForEach( x =>
						{
							thisQuery |= Query<T>.Nested( n => n
								   .Path( p => p.AgentRelationshipsForEntity )
									.Query( q => q
										.Bool( mm => mm
											 .Must( mu => mu
												.Terms( m => m
													.Field( f => f.AgentRelationshipsForEntity.First().RelationshipTypeIds ).Terms( relationshipTypeIds )
														)
													&& mu.Terms( m => m.Field( f => f.AgentRelationshipsForEntity.First().OrgId ).Terms( x.Id )
													)
											)//Must
										) //Bool
									) //Query
								); //Nested
						} );
					}
					else
					{
						//21-05-10 mparsons - search on name only not working. Try modelling after competencies
						//			LATER
						assurances.ForEach( x =>
						{
							thisQuery |= Query<T>.Nested( n => n
								   .Path( p => p.AgentRelationshipsForEntity )
									.Query( q => q
										.Bool( mm => mm
											 .Must( mu => mu
												.Terms( m => m
													.Field( f => f.AgentRelationshipsForEntity.First().RelationshipTypeIds ).Terms( relationshipTypeIds ) )
													//&& mu.Terms( m => m.Field( f => f.AgentRelationshipsForEntity.First().AgentName ).Terms( x.Name )
													//)
													&& mu.MultiMatch( m => m
														.Fields( mf => mf
															.Field( f => f.AgentRelationshipsForEntity.First().AgentName, 70 ) ).Type( TextQueryType.Phrase ).Query( x.Name ) )
											)//Must
										) //Bool
									) //Query
								); //Nested
						} );
					}
				}
				else
				{
					//do we need a case where there was a TEXT filter, but no qualityassurance, and we have relationships?
					//24-04-15 mp - adding check for just ids
					if ( relationshipTypeIds.Any() )
					{
						thisQuery = Query<T>.Nested( n => n.Path( p => p.AgentRelationshipsForEntity )
									.Query( q =>
										q.Terms( t =>
											t.Field( f => f.AgentRelationshipsForEntity.First().RelationshipTypeIds )
												.Terms<int>( relationshipTypeIds.ToArray() ) ) )
											);
					}
				}
			}
			else
			{
				//make sure relationships are for this query type
				var exists = query.FiltersV2.Where( m => m.Name == "qualityassurance" || m.Name == "filter:QAReceived" ).ToList();
				if ( exists != null && exists.Any() )
				{
					//if no TEXT filter, then just use the relationship codes
					thisQuery = Query<T>.Nested( n => n.Path( p => p.AgentRelationshipsForEntity )
								.Query( q =>
									q.Terms( t =>
										t.Field( f => f.AgentRelationshipsForEntity.First().RelationshipTypeIds )
											.Terms<int>( relationshipTypeIds.ToArray() ) ) )
										);
				}
			}
			return thisQuery;
		}

		#endregion
		#region Common QualityAssurancePerformed
		public static QueryContainer CommonQualityAssurancePerformed<T>( MainSearchInput query ) where T : class, IIndex
		{
			QueryContainer qualityAssurancePerformedQuery = null;
			if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CUSTOM ) )
			{
				var assurances = new List<CodeItem>();
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "qualityassuranceperformed" || m.Name == "filter:QAPerformed" ).ToList() )
				{
					assurances.Add( filter.AsQapItem() );
				}
				if ( assurances.Any() )
					assurances.ForEach( x =>
					{
						qualityAssurancePerformedQuery |= Query<OrganizationIndex>.Nested( n => n.Path( p => p.QualityAssurancePerformed ).Query( q => q.Bool( mm => mm.Must( mu => mu.Match( m => m.Field( f => f.QualityAssurancePerformed.First().AssertionTypeIds ).Query( x.AssertionId.ToString() ) ) && mu.Match( m => m.Field( f => f.QualityAssurancePerformed.First().TargetEntityBaseId ).Query( x.Id.ToString() ) ) ) ) ) );
					} );
			}
			return qualityAssurancePerformedQuery;
		}
		#endregion
		//public static QueryContainer CommonQualityAssurance_OLD<T>( MainSearchInput query ) where T : class, IIndex
		//{
		//	QueryContainer qualityAssurancesQuery = null;
		//	if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CUSTOM ) )
		//	{
		//		var assurances = new List<CodeItem>();
		//		foreach ( var filter in query.FiltersV2.Where( m => m.Name == "qualityassurance" ).ToList() )
		//		{
		//			assurances.Add( filter.AsQaItem() );
		//		}

		//		if ( assurances.Any() )
		//			assurances.ForEach( x =>
		//			{
		//				qualityAssurancesQuery |= Query<CredentialIndex>.Nested( n => n
		//					   .Path( p => p.QualityAssurance )
		//						.Query( q => q
		//							.Bool( mm => mm
		//								 .Must( mu => mu.Match( m => m.Field( f => f
		//										.QualityAssurance.First().RelationshipTypeId )
		//										   .Query( x.RelationshipId.ToString() ) )
		//										  && mu.Match( m => m.Field( f => f.QualityAssurance.First().AgentRelativeId )
		//											   .Query( x.Id.ToString() ) ) ) ) ) );
		//			} );
		//	}
		//	return qualityAssurancesQuery;
		//}


		public static QueryContainer HandleLocationQuery<T>( MainSearchInput query ) where T : class, IIndex
		{
			var boundariesQuery = new QueryContainer();

			//Get map filter data, in new or old flavors as applicable
			var mapFilter = query.FiltersV2.FirstOrDefault( m => !string.IsNullOrWhiteSpace( m.Map_PositionType ) );
			var boundaries = SearchServices.GetBoundaries( query, "bounds" );
			var newMapFilter = query.OriginalQuery?.MapFilter;
			if ( newMapFilter != null && newMapFilter.PositionType != null )
			{
				mapFilter = new MainSearchFilterV2()
				{
					Map_PositionType = newMapFilter.PositionType ?? "positionType:In", //Sometimes React doesn't assign this property!
					Map_Country = newMapFilter.Country,
					Map_Region = newMapFilter.Region,
					Map_Locality = newMapFilter.Label == newMapFilter.Region ? null : newMapFilter.Label
				};
				boundaries = new workIT.Models.Common.BoundingBox()
				{
					North = ( decimal ) newMapFilter.BBoxNorth,
					East = ( decimal ) newMapFilter.BBoxEast,
					South = ( decimal ) newMapFilter.BBoxSouth,
					West = ( decimal ) newMapFilter.BBoxWest
				};
			}

			//Skip if no location data
			//if ( !boundaries.IsDefined && string.IsNullOrWhiteSpace( mapFilter.Map_Country ) )
			if ( !boundaries.IsDefined && ( mapFilter == null || string.IsNullOrWhiteSpace( mapFilter.Map_Country ) ) )
			{
				return null;
			}

			//Only look for results that are within the specified area
			if ( mapFilter != null && mapFilter.Map_PositionType == "positionType:In" && !string.IsNullOrWhiteSpace( mapFilter.Map_Country ) && !string.IsNullOrWhiteSpace( mapFilter.Map_Region ) )
			{
				if ( string.IsNullOrWhiteSpace( mapFilter.Map_Locality ) )
				{
					boundariesQuery = Query<CredentialIndex>
						.Nested( n => n.Path( p => p.Addresses )
						.Query( q => Query<CredentialIndex>.Bool( b =>
							b.Must(
								m => m.Match( mm => mm.Field( f => f.Addresses.First().AddressRegion ).Query( mapFilter.Map_Region ) )
							)
							.Should(
								m => m.Match( mm => mm.Field( f => f.Addresses.First().AddressCountry ).Query( mapFilter.Map_Country ) )
							)
						) ).IgnoreUnmapped() );
				}
				else
				{
					boundariesQuery = Query<CredentialIndex>
						.Nested( n => n.Path( p => p.Addresses )
						.Query( q => Query<CredentialIndex>.Bool( b =>
							b.Must(
								m => m.Match( mm => mm.Field( f => f.Addresses.First().AddressRegion ).Query( mapFilter.Map_Region ) ),
								m => m.Match( mm => mm.Field( f => f.Addresses.First().AddressLocality ).Query( mapFilter.Map_Locality ) )
							)
							.Should(
								m => m.Match( mm => mm.Field( f => f.Addresses.First().AddressCountry ).Query( mapFilter.Map_Country ) )
							)
						) ).IgnoreUnmapped() );
				}
			}
			//Do a bounding box search but prioritize results that are in the specified area
			else if ( mapFilter != null && mapFilter.Map_PositionType.IndexOf( "positionType:Near" ) == 0 )
			{
				var latitude25Unit = 0.362d; //One degree of latitude is 69 miles, so this represents 25 miles of latitude
				var longitude25Unit = 0.463d; //One degree of longitude is a little over 54 miles, so this represents 25 miles of longitude
				var latitudeRadius = latitude25Unit;
				var longitudeRadius = longitude25Unit;
				var latitudeCenter = newMapFilter?.BBoxCenterLatitude ?? ( ( double ) boundaries.North + ( double ) boundaries.South ) / 2d;
				var longitudeCenter = newMapFilter?.BBoxCenterLongitude ?? ( ( double ) boundaries.East + ( double ) boundaries.West ) / 2d;
				switch ( mapFilter.Map_PositionType )
				{
					case "positionType:Near_25": latitudeRadius = latitude25Unit * 1; longitudeRadius = longitude25Unit * 1; break;
					case "positionType:Near_50": latitudeRadius = latitude25Unit * 2; longitudeRadius = longitude25Unit * 2; break;
					case "positionType:Near_100": latitudeRadius = latitude25Unit * 3; longitudeRadius = longitude25Unit * 3; break;
					default: break;
				}

				if ( latitudeCenter != 0 && longitudeCenter != 0 )
				{
					boundariesQuery = Query<CredentialIndex>
						.Nested( n => n.Path( p => p.Addresses )
						.Query( q => Query<CredentialIndex>.Bool( b =>
							b.Must(
								//m => m.Range( r => r.Field( f => f.Addresses.First().Longitude ).LessThan( ( double ) boundaries.East ).GreaterThan( ( double ) boundaries.West ) ),
								//m => m.Range( r => r.Field( f => f.Addresses.First().Latitude ).LessThan( ( double ) boundaries.North ).GreaterThan( ( double ) boundaries.South ) ),
								m => m.Range( r => r.Field( f => f.Addresses.First().Longitude ).LessThan( longitudeCenter + longitudeRadius ).GreaterThan( longitudeCenter - longitudeRadius ) ),
								m => m.Range( r => r.Field( f => f.Addresses.First().Latitude ).LessThan( latitudeCenter + latitudeRadius ).GreaterThan( latitudeCenter - latitudeRadius ) )
							)
						//.Should(
						//	m => m.Match( mm => mm.Field( f => f.Addresses.First().AddressRegion ).Query( mapFilter.Map_Region ) ),
						//	m => m.Match( mm => mm.Field( f => f.Addresses.First().AddressCountry ).Query( mapFilter.Map_Country ) )
						//)
						) ).IgnoreUnmapped() );
				}
				else
				{
					boundariesQuery = Query<CredentialIndex>
						.Nested( n => n.Path( p => p.Addresses )
						.Query( q => Query<CredentialIndex>.Bool( b =>
							b.Must(
								m => m.Range( r => r.Field( f => f.Addresses.First().Longitude ).LessThan( ( double ) boundaries.East ).GreaterThan( ( double ) boundaries.West ) ),
								m => m.Range( r => r.Field( f => f.Addresses.First().Latitude ).LessThan( ( double ) boundaries.North ).GreaterThan( ( double ) boundaries.South ) )
							)
						//.Should(
						//	m => m.Match( mm => mm.Field( f => f.Addresses.First().AddressRegion ).Query( mapFilter.Map_Region ) ),
						//	m => m.Match( mm => mm.Field( f => f.Addresses.First().AddressLocality ).Query( mapFilter.Map_Locality ) ),
						//	m => m.Match( mm => mm.Field( f => f.Addresses.First().AddressCountry ).Query( mapFilter.Map_Country ) )
						//)
						) ).IgnoreUnmapped() );
				}
			}
			//Just do a classic bounding box search
			else
			{
				if ( boundaries.IsDefined )
				{
					boundariesQuery = Query<CredentialIndex>
						.Nested( n => n.Path( p => p.Addresses )
						.Query( q =>
							Query<CredentialIndex>.Range( r => r.Field( f => f.Addresses.First().Longitude ).LessThan( ( double ) boundaries.East ).GreaterThan( ( double ) boundaries.West ) ) &&
							Query<CredentialIndex>.Range( r => r.Field( f => f.Addresses.First().Latitude ).LessThan( ( double ) boundaries.North ).GreaterThan( ( double ) boundaries.South ) )
						).IgnoreUnmapped() );
				}

			}

			return boundariesQuery;

		}
		//


		private static void CommonHistoryFilter<T>( MainSearchInput query, ref HistoryQueryFilters history ) where T : class, IIndex
		{
			foreach ( var filter in query.FiltersV2.Where( m => m.Type == MainSearchFilterV2Types.CODE ).ToList() )
			{
				if ( filter.Name != "history" )
				{
					continue;
				}
				var dateFilter = filter.AsDateItem();
				if ( dateFilter != null && !string.IsNullOrWhiteSpace( dateFilter.Name ) && !string.IsNullOrWhiteSpace( dateFilter.Code ) )
				{
					if ( dateFilter.Name == "lastUpdatedFrom" && BaseFactory.IsValidDate( dateFilter.Code ) )
					{
						history.HistoryFromQuery = Query<T>.DateRange( c => c
							  .Boost( 1.1 )
							  .Field( p => p.LastUpdated )
							  .GreaterThanOrEquals( dateFilter.Code )
							  .Format( "MM/dd/yyyy||yyyy" )
						);
					}
					if ( dateFilter.Name == "lastUpdatedTo" && BaseFactory.IsValidDate( dateFilter.Code ) )
					{
						history.HistoryToQuery = Query<T>.DateRange( c => c
							  .Boost( 1.1 )
							  .Field( p => p.LastUpdated )
							  .LessThanOrEquals( dateFilter.Code )
							  .Format( "MM/dd/yyyy||yyyy" )
						);
					}
					//
					if ( dateFilter.Name == "createdFrom" && BaseFactory.IsValidDate( dateFilter.Code ) )
					{
						history.CreatedFromQuery = Query<T>.DateRange( c => c
							  .Boost( 1.1 )
							  .Field( p => p.Created )
							  .GreaterThanOrEquals( dateFilter.Code )
							  .Format( "MM/dd/yyyy||yyyy" )
						);
					}
					if ( dateFilter.Name == "createdTo" && BaseFactory.IsValidDate( dateFilter.Code ) )
					{
						history.CreatedToQuery = Query<T>.DateRange( c => c
							  .Boost( 1.1 )
							  .Field( p => p.Created )
							  .LessThanOrEquals( dateFilter.Code )
							  .Format( "MM/dd/yyyy||yyyy" )
						);
					}
				}

			}
		}

		/// <summary>
		/// Handle location related filters - for a widget
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="query"></param>
		/// <param name="locationQueryFilters"></param>
		/// <param name="usingRegionHack"></param>
		private void LocationFilter<T>( MainSearchInput query, LocationQueryFilters locationQueryFilters, ref bool usingRegionHack ) where T : class, IIndex
		{
			//TODO - try to include a Region search with keywords. 
			//	The problem here could be only a few letters were entered before search start. Use a low priority
			var hasLocationFilters = false;
			int regionBoost = 20;
			if ( query.FiltersV2.Count > 0 )
			{
				var locationFiltersExists = query.FiltersV2.Where( m => m.Type == MainSearchFilterV2Types.CODE && m.Name?.ToLower() == "locationset" ).ToList();
				if ( locationFiltersExists != null && locationFiltersExists.Count > 0 )
				{
					hasLocationFilters = true;
				}
			}

			//check for custom keywords starting with region:
			if ( !string.IsNullOrWhiteSpace( query.Keywords ) && query.Keywords.Trim().ToLower().IndexOf( "region:" ) == 0 )
			{
				//if present, skip location filters
				//also check for presense of a locationset filter
				if ( query.FiltersV2.Count > 0 )
				{
					//var exists = query.FiltersV2.Where( m => m.Type == MainSearchFilterV2Types.CODE && m.Name == "locationset" ).ToList();
					//if ( exists != null && exists.Count > 0 )
					if ( !hasLocationFilters )
					{
						if ( query.FiltersV2.Count > 1 )
						{
							var newQuery = new List<MainSearchFilterV2>();
							foreach ( var filter in query.FiltersV2.Where( m => m.Type == MainSearchFilterV2Types.CODE ).ToList() )
							{
								if ( filter.Name != "locationset" )
								{
									newQuery.Add( filter );
								}
							}
							query.FiltersV2 = newQuery;
						}
						else
						{
							query.FiltersV2 = new List<MainSearchFilterV2>();
						}
						locationQueryFilters = new LocationQueryFilters();
					}
				}

				var region = query.Keywords.Substring( query.Keywords.IndexOf( "region:" ) + 7 );
				if ( !string.IsNullOrWhiteSpace( region ) )
				{
					//first try to use existing code
					var rfilter = new MainSearchFilterV2()
					{
						Name = "locationset",
						Type = MainSearchFilterV2Types.CODE
					};
					rfilter.Values = new Dictionary<string, object>();
					rfilter.Values.Add( "Countries[0]", "United States" );
					rfilter.Values.Add( "Regions[0]", region.Trim() );
					query.FiltersV2.Add( rfilter );
					//fall thru
					//LocationFilter<CredentialIndex>( query, locationQueryFilters );

				}
				query.Keywords = "";
			}
			else
			{
				//try a low weighted region query. Minimum of ? letters, and max as well
				//also no spaces. However checking individual words. Or use last word - OK now what about New york, new jersey, etc.?
				//TODO make this an OR with the keyword query
				//NO -_ removing - this is an AND, unless can make an OR, skip
				if ( !hasLocationFilters && !string.IsNullOrWhiteSpace( query.Keywords ) )
				{
					var region = query.Keywords.Trim().ToLower();
					if ( IsUSRegion( region, ref regionBoost ) && UtilityManager.GetAppKeyValue( "searchAllowingRegionGuessing", false ) )
					{
						//if ( region.Length > 3 && region.Length < 15 && region.IndexOf( " " ) == -1 )
						//{
						var rfilter = new MainSearchFilterV2()
						{
							Name = "locationset",
							Type = MainSearchFilterV2Types.CODE
						};
						rfilter.Values = new Dictionary<string, object>
						{
							{ "Countries[0]", "United States" },
							{ "Regions[0]", region }
						};
						query.FiltersV2.Add( rfilter );
						//can leave at 20 until able to do ORs
						//TODO -make this larger if matches full region
						//regionBoost = 20;
						//now should query.Keywords be cleared? No, we are guessing here
						usingRegionHack = true;
						//query.Keywords = "";
					}
				}
			}

			//What is locationset? - related to filters selected in the widget config
			foreach ( var filter in query.FiltersV2.Where( m => m.Type == MainSearchFilterV2Types.CODE ).ToList() )
			{
				if ( filter.Name?.ToLower() != "locationset" )
				{
					continue;
				}
				//TODO not sure if can use the same process
				//var locationSet = filter.AsLocationSet();
				var locationSet = query.OriginalQuery.MainFilters?.FirstOrDefault( m => m.Items?.FirstOrDefault( n => n.URI?.ToLower() == "locationset" ) != null )?.Items.FirstOrDefault( m => m.URI?.ToLower() == "locationset" ).Values?.ToObject<WidgetV2.LocationSet>() ?? new WidgetV2.LocationSet();
				if ( locationSet.Regions.Count() > 0 )
				{
					if ( locationSet.Regions.Count() == 1 && locationSet.Cities.Count() > 0 && !string.IsNullOrEmpty( locationSet.Cities[0] ) )
					{
						string region = locationSet.Regions[0];
						//need to check cities with regions, maybe country, but latter may not exist in index
						locationSet.Cities.ForEach( x =>
						{
							if ( !string.IsNullOrEmpty( x ) )
							{
								locationQueryFilters.CityQuery |= Query<T>.Nested( n => n.Path( p => p.Addresses )
								.Query( q => q.Bool( mm => mm.Must( mu => mu
									.MultiMatch( m => m.Fields( mf => mf
									.Field( f => f.Addresses.First().AddressLocality, 70 ) )
									.Type( TextQueryType.PhrasePrefix )
									.Query( x ) ) ) ) ).IgnoreUnmapped() );
							}
						} );
					}
					else
					{
						locationSet.Regions.ForEach( x =>
						{
							if ( !string.IsNullOrEmpty( x ) )
							{
								if ( locationSet.IsAvailableOnline )
								{
									locationQueryFilters.RegionQuery |= Query<T>.Term( t => t.Field( f => f.IsAvailableOnline ).Value( true ) ) ||
									Query<T>.Nested( n => n.Path( p => p.Addresses )
									.Query( q => q.Bool( mm => mm.Must( mu => mu
									.MultiMatch( m => m.Fields( mf => mf
									.Field( f => f.Addresses.First().AddressRegion, regionBoost ) )
									.Type( TextQueryType.PhrasePrefix )
									.Query( x ) ) ) ) ).IgnoreUnmapped() );
								}
								else
								{
									locationQueryFilters.RegionQuery |= Query<T>.Nested
									( n => n.Path( p => p.Addresses )
										.Query
										( q => q.Bool
											( mm => mm.Must
												( mu => mu.MultiMatch
													( m => m.Fields( mf => mf
														.Field( f => f.Addresses.First().AddressRegion, 20 ) )
														.Type( TextQueryType.PhrasePrefix )
														.Query( x )
													)
												)
											)
										).IgnoreUnmapped()
									);
								}
							}
						} );
					}

				}
				else
				{
					//country may not always be present
					locationSet.Countries.ForEach( x =>
					{
						if ( !string.IsNullOrEmpty( x ) )
						{
							locationQueryFilters.CountryQuery |= Query<T>.Nested( n => n.Path( p => p.Addresses )
								.Query( q => q.Bool( mm => mm.Must( mu => mu.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Addresses.First().AddressCountry, 10 ) )
								.Type( TextQueryType.PhrasePrefix )
								.Query( x ) ) ) ) )
								.IgnoreUnmapped() );
						}
					} );

				}

			}
		}

		private static QueryContainer LocationSubregionFilter<T>( MainSearchInput query ) where T : class, IIndex
		{
			QueryContainer thisQuery = null;
			QueryContainer qc = null;
			QueryContainer qc2 = null;
			var identifiers = new List<CodeItem>();
			var lwiaList = new List<string>();
			bool hasId = false;
			if ( query.FiltersV2.Count == 0 )
				return null;

			var lwiaFiltersExists = query.FiltersV2.Where( m => m.Type == MainSearchFilterV2Types.CODE && ( m.Name == "filter:IllinoisEDR" || m.Name == "filter:IllinoisLWIA" ) ).ToList();
			if ( lwiaFiltersExists?.Count == 0 )
			{
				return null;
			}

			//
			foreach ( var filter in query.FiltersV2.Where( m => m.Name == "filter:IllinoisLWIA" ).ToList() )
			{
				CodeItem ci = filter.AsLWIAText();
				if ( ci != null && ( ci.Id > 0 || !string.IsNullOrWhiteSpace( ci.Name ) ) )
				{
					if ( ci.Id > 0 )
					{
						hasId = true;
						lwiaList.Add( ci.Id.ToString() );
					}

					identifiers.Add( filter.AsQaText() );    //getting orgIds

				}
			}
			if ( hasId )
			{
				//identifiers.ForEach( x =>
				//{
				//	qc |= Query<T>.MatchPhrase( mp => mp
				//		.Field( f => f.RegionIdentifier.First().IdentifierValueCode ).Query( x.Id.ToString() ).Boost( 60 ) );
				//} );

				qc2 |= Query<T>.Terms( ts => ts.Field( f => f.LWIAList ).Terms( lwiaList.ToArray() ) );
				//identifiers.ForEach( x =>
				//{
				//	qc |= Query<T>
				//		.Bool ( mm => mm
				//			.Must ( mu => mu
				//				.Term( t => t.RegionIdentifier.Find ) 

				//				.MatchPhrase( mp => mp
				//		.Field( f => f.RegionIdentifier.First().IdentifierValueCode ).Query( x.Id.ToString() ).Boost( 60 ) );
				//} );

				/*
				 * identifiers.ForEach( x =>
				{
					qc |= Query<T>.MatchPhrase( mp => mp
						.Field( f => f.RegionIdentifier.First().IdentifierValueCode ).Query( x.Id.ToString() ).Boost( 60 )
						.Field( f => f.RegionIdentifier.First().IdentifierTypeName) .Query ("LWIA"));
				} );
				*/
				if ( qc != null )
				{
					thisQuery = Query<T>.Nested( n => n.Path( p => p.RegionIdentifier ).Query( q => qc ).IgnoreUnmapped() );
				} else if ( qc2 != null )
				{
					thisQuery = qc2;
				}

			}
			//
			return thisQuery;
		}
		public static bool IsUSRegion( string region, ref int boostLevel )
		{
			//if ( region.Length == 2 )
			//{
			//	//don't think the 2 digit codes are stored
			//	if ( "AK AL AR AZ CA CO CT DC DE FL GA HI IA ID IL IN KS KY LA MA MD ME MI MN MO MS MT NC ND NE NH NJ NM NV NY OH OK OR PA RI SC SD TN TX UT VA VT WA WI WV WY".IndexOf( region.ToUpper() ) > -1 )
			//		return true;
			//}
			//else
			{

				//test duration
				if ( UtilityManager.GetAppKeyValue( "useDatabaseForRegionGuessing", false ) )
				{
					var fullRegion = "";
					if ( CodesManager.Codes_IsState( region, ref fullRegion, ref boostLevel ) )
					{
						//if region = full region then use a higher boost
						region = fullRegion;
					}
				} else
				{
					if ( "alabama alaska arizona arkansas california colorado connecticut delaware district of columbia florida georgia hawaii idaho illinois indiana iowa kansas kentucky louisiana maine maryland massachusetts michigan minnesota mississippi missouri montana nebraska nevada new hampshire new jersey new mexico new york north carolina north dakota ohio oklahoma oregon pennsylvania rhode island south carolina south dakota tennessee texas utah vermont virginia washington west virginia wisconsin wyoming".IndexOf( region.ToLower() ) > -1 )
						return true;
				}
			}

			return false;
		}
		#endregion

		#region Pathway

		#region Build/update index --21-02-11 - using PathwayIndex now
		public static void Pathway_BuildIndex( bool deleteIndexFirst = false, bool updatingIndexRegardless = false )
		{
			List<PathwayIndex> list = new List<Models.Elastic.PathwayIndex>();
			bool indexInitialized = false;
			if ( deleteIndexFirst && EC.Indices.Exists( PathwayIndex ).Exists )
			{
				EC.Indices.Delete( PathwayIndex );
			}
			if ( !EC.Indices.Exists( PathwayIndex ).Exists )
			{
				//GeneralInitializeIndex();
				PathwayInitializeIndex();
				indexInitialized = true;
			}

			if ( indexInitialized || updatingIndexRegardless )
			{
				LoggingHelper.DoTrace( 1, "Pathway- Building Index" );
				int minEntityStateId = UtilityManager.GetAppKeyValue( "minAsmtEntityStateId", 3 );
				try
				{
					new ActivityServices().AddActivity( new SiteActivity()
					{ ActivityType = "Pathway", Activity = "Elastic", Event = "Build Index" }
					);

					int processed = 0;
					string filter = "( base.EntityStateId >= 2 )";
					//20-12-28 - update to be like credentials
					Pathway_UpdateIndex( filter, ref processed );
					if ( processed > 0 )
					{
						var refreshResults = EC.Indices.Refresh( PathwayIndex );
						new ActivityServices().AddActivity( new SiteActivity()
						{
							ActivityType = "Pathway",
							Activity = "Elastic",
							Event = "Build Index Completed",
							Comment = string.Format( "Completed rebuild of PathwayIndex for {0} records.", processed )
						} );
					}
					else
					{
						//ISSUE-not necessarily
						LoggingHelper.DoTrace( 1, "Build PathwayIndex: no results were returned from SearchForElastic method." );
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, "Pathway_BuildIndex" );
				}
				finally
				{
					if ( list != null && list.Count > 0 )
					{
						var results = EC.Bulk( b => b.IndexMany( list, ( d, document ) => d.Index( PathwayIndex ).Document( document ).Id( document.Id.ToString() ) ) );
						if ( results.ToString().IndexOf( "Valid NEST response built from a successful" ) == -1 )
						{
							Console.WriteLine( results.ToString() );
							LoggingHelper.DoTrace( 1, " Issue building Pathway/General index: " + results.DebugInformation.Substring( 0, 2000 ) );
						}

						EC.Indices.Refresh( PathwayIndex );
					}
				}
			}

		}
		#endregion

		public static void PathwayInitializeIndex( bool deleteIndexFirst = true )
		{
			if ( !EC.Indices.Exists( PathwayIndex ).Exists )
			{

				EC.Indices.Create( PathwayIndex, c => new CreateIndexDescriptor( PathwayIndex )

				 .Settings( st => st
						 .Analysis( an => an.TokenFilters( tf => tf.Stemmer( "custom_english_stemmer", ts => ts.Language( "english" ) ) ).Analyzers( anz => anz.Custom( "custom_lowercase_stemmed", cc => cc.Tokenizer( "standard" ).Filters( new List<string> { "lowercase", "custom_english_stemmer" } ) ) ) ) )
						//.Mappings( ms => ms
						.Map<PathwayIndex>( m => m
							.AutoMap()
							.Properties( p => p
								.Text( s => s.Name( n => n.Description ).Analyzer( "custom_lowercase_stemmed" ) )
								.Nested<IndexReferenceFramework>( n => n
									.Name( nn => nn.Industries )
									.AutoMap()
								)
								.Nested<IndexReferenceFramework>( n => n
									.Name( nn => nn.Occupations )
									.AutoMap()
								)
								 //AgentRelationshipForEntity will replace IndexQualityAssurance
								 .Nested<Models.Elastic.AgentRelationshipForEntity>( n => n
									.Name( nn => nn.AgentRelationshipsForEntity )
									.AutoMap()
								)

							)
						)
				//)
				);
			}
		}

		public static void Pathway_UpdateIndex( int recordId )
		{
			if ( recordId < 1 )
				return;
			try
			{
				string filter = string.Format( " ( base.Id = {0} ) ", recordId );
				int processed = 0;
				Pathway_UpdateIndex( filter, ref processed );

			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "Pathway_UpdateIndex" );
			}
		} //


		/// <summary>
		/// Pass a filter to use for updating the index
		/// </summary>
		/// <param name="filter"></param>
		public static void Pathway_UpdateIndex( string filter, ref int processed )
		{
			if ( string.IsNullOrWhiteSpace( filter ) )
				return;
			int action = UtilityManager.GetAppKeyValue( "updateCredIndexAction", 2 );
			if ( action == 0 )
				return;
			string methodName = "Pathway_UpdateIndex";
			string IndexName = PathwayIndex;
			int pageSize = nonCredentialPageSize;
			int pageNbr = 1;
			int totalRows = 0;
			bool isComplete = false;
			int cntr = 0; ;
			try
			{
				while ( pageNbr > 0 && !isComplete )
				{
					//var list = ElasticManager.Pathway_SearchForElastic( filter );
					var list = ElasticManager.Pathway_SearchForElastic( filter, pageSize, pageNbr, ref totalRows );
					if ( list != null && list.Count > 0 )
					{
						processed = list.Count;
						if ( action == 1 )
						{
							foreach ( var item in list )
							{
								cntr++;
								var res = EC.Index( item, idx => idx.Index( IndexName ) );
								Console.WriteLine( res.Result );
							}
						}
						else if ( action == 2 )
						{
							cntr = cntr + list.Count;
							var results = EC.Bulk( b => b.IndexMany( list, ( d, record ) => d.Index( IndexName ).Document( record ).Id( record.Id.ToString() ) ) );
							if ( results.ToString().IndexOf( "Valid NEST response built from a successful" ) == -1 )
							{
								Console.WriteLine( results.ToString() );
								LoggingHelper.DoTrace( 1, string.Format( " Issue building {0}: ", IndexName ) + results.DebugInformation.Substring( 0, 2000 ) );
							}
						}
					}
					else
					{
						//LoggingHelper.DoTrace( 2, string.Format( "Pathway_UpdateIndex failed, no data returned for filter: {0}", filter ) );
						if ( list != null && list.Count == 1 && totalRows == -1 )
						{
							LoggingHelper.LogError( string.Format( "{0}: Error in search. {1}", methodName, list[0].Description ) );

						}
						else if ( pageNbr == 1 )
						{
							if ( string.IsNullOrWhiteSpace( filter ) )
							{
								LoggingHelper.LogError( string.Format( "{0}: entered with no filter, but no results were returned from search.", methodName ) );
							}
							LoggingHelper.DoTrace( 2, string.Format( "{0}: NOTE no data returned for filter: {1}", methodName, filter ) );
						}
						isComplete = true;
						break;
					}
					LoggingHelper.DoTrace( 4, string.Format( "{0}. Page: {1}, Records Indexed: {2} 0f {3} records.", methodName, pageNbr, cntr, totalRows ) );
					pageNbr++;
					if ( cntr >= totalRows )
					{
						isComplete = true;
					}
				} //loop
			}
			catch ( Exception ex )
			{
				var msg = BaseFactory.FormatExceptions( ex );
				LoggingHelper.DoTrace( 1, string.Format( " Issue building {0}: ", IndexName ) + msg );
				LoggingHelper.LogError( ex, string.Format( "{0} failed for filter: {1}", methodName, filter ) );
			}
		}

		public static List<string> PathwayAutoComplete( MainSearchInput query, int maxTerms, ref int pTotalRows )
		{

			List<string> autocompleteList = new List<string>();

			//test just calling the related main search and returning the terms
			PathwaySearch( query, ref pTotalRows, ref autocompleteList, true );
			return autocompleteList;

			//var search = EC.Search<PathwayIndex>( i => i.Index( PathwayIndex ).Query( q => q.MultiMatch( m => m
			//			   .Fields( f => f
			//				   .Field( ff => ff.Name )
			//				   .Field( ff => ff.Description )
			//			   )
			//			   //.Operator( Operator.Or )
			//			   .Type( TextQueryType.PhrasePrefix )
			//			   .Query( keyword )
			//			   .MaxExpansions( 10 ) ) ).Size( maxTerms * 2) );

			//pTotalRows = ( int )search.Total;
			//var list = ( List<PathwayIndex> )search.Documents;
			//return list.Select( x => x.Name).Distinct().Take( maxTerms ).ToList().Cast<object>().ToList();
		}

		/// <summary>
		/// Update Index for PathwaySet
		/// </summary>
		/// <param name="filter"></param>
		/// <param name="processed"></param>
		public static void PathwaySet_UpdateIndex( string filter, ref int processed )
		{
			if ( string.IsNullOrWhiteSpace( filter ) )
				return;
			int action = UtilityManager.GetAppKeyValue( "updateCredIndexAction", 2 );
			if ( action == 0 )
				return;
			string methodName = "PathwaySet_UpdateIndex";
			string IndexName = CommonIndex;
			int pageSize = nonCredentialPageSize; ;
			int pageNbr = 1;
			int totalRows = 0;
			bool isComplete = false;
			int cntr = 0; ;
			try
			{
				while ( pageNbr > 0 && !isComplete )
				{
					//var list = ElasticManager.PathwaySet_SearchForElastic( filter );
					var list = ElasticManager.PathwaySet_SearchForElastic( filter, pageSize, pageNbr, ref totalRows );
					if ( list != null && list.Count > 0 )
					{
						processed = list.Count;
						if ( action == 1 )
						{
							foreach ( var item in list )
							{
								cntr++;
								var res = EC.Index( item, idx => idx.Index( IndexName ) );
								Console.WriteLine( res.Result );
							}
						}
						else if ( action == 2 )
						{
							cntr = cntr + list.Count;
							var results = EC.Bulk( b => b.IndexMany( list, ( d, record ) => d.Index( IndexName ).Document( record ).Id( record.Id.ToString() ) ) );
							if ( results.ToString().IndexOf( "Valid NEST response built from a successful" ) == -1 )
							{
								Console.WriteLine( results.ToString() );
								LoggingHelper.DoTrace( 1, string.Format( " Issue building {0}: ", IndexName ) + results.DebugInformation.Substring( 0, 2000 ) );
							}
						}
					}
					else
					{
						//LoggingHelper.DoTrace( 2, string.Format( "PathwaySet_UpdateIndex failed, no data returned for filter: {0}", filter ) );
						if ( list != null && list.Count == 1 && totalRows == -1 )
						{
							LoggingHelper.LogError( string.Format( "{0}: Error in search. {1}", methodName, list[0].Description ) );

						}
						else if ( pageNbr == 1 )
						{
							if ( string.IsNullOrWhiteSpace( filter ) )
							{
								LoggingHelper.LogError( string.Format( "{0}: entered with no filter, but no results were returned from search.", methodName ) );
							}
							LoggingHelper.DoTrace( 2, string.Format( "{0}: NOTE no data returned for filter: {1}", methodName, filter ) );
						}
						isComplete = true;
						break;
					}
					LoggingHelper.DoTrace( 4, string.Format( "{0}. Page: {1}, Records Indexed: {2} 0f {3} records.", methodName, pageNbr, cntr, totalRows ) );
					pageNbr++;
					if ( cntr >= totalRows )
					{
						isComplete = true;
					}
				} //loop
			}
			catch ( Exception ex )
			{
				var msg = BaseFactory.FormatExceptions( ex );
				LoggingHelper.DoTrace( 1, string.Format( " Issue building {0}: ", IndexName ) + msg );
				LoggingHelper.LogError( ex, string.Format( "{0} failed for filter: {1}", methodName, filter ) );
			}
		}
		public static List<CommonSearchSummary> PathwaySetSearch( MainSearchInput query, ref int pTotalRows )
		{
			return GeneralSearch( CodesManager.ENTITY_TYPE_PATHWAY_SET, "PathwaySet", query, ref pTotalRows );

		}

		/// <summary>
		/// Proxy for full search
		/// </summary>
		/// <param name="query"></param>
		/// <param name="pTotalRows"></param>
		/// <returns></returns>
		public static List<CommonSearchSummary> PathwaySearch( MainSearchInput query, ref int pTotalRows )
		{
			List<string> autocompleteList = new List<string>();
			bool requestIsAutocomplete = false;
			return PathwaySearch( query, ref pTotalRows, ref autocompleteList, requestIsAutocomplete );
		}
		public static List<CommonSearchSummary> PathwaySearch( MainSearchInput query, ref int pTotalRows, ref List<string> autocompleteList, bool requestIsAutocomplete = false )
		{
			//any index rebuild will occur from GeneralSearch
			Pathway_BuildIndex();

			//return GeneralSearch( CodesManager.ENTITY_TYPE_PATHWAY, "Pathway", query, ref pTotalRows );



			var list = new List<CommonSearchSummary>();
			//
			QueryContainer collectionsQuery = null;
			QueryContainer entityTypeQuery = null;
			//QueryContainer LifeCycleStatusTypeQuery = null;
			QueryContainer keywordsQuery = null;
			QueryContainer industriesQuery = null;
			QueryContainer occupationsQuery = null;
			QueryContainer qaFilterQuery = null;
			QueryContainer subjectsQuery = null;
			QueryContainer reportsQuery = null;
			QueryContainer commonOrgRolesBYQuery = null;
			QueryContainer publishedBYQuery = null;

			//QueryContainer ownedByQuery = null;
			QueryContainer widgetOccupationsQuery = null;
			QueryContainer windustriesQuery = null;
			HistoryQueryFilters history = new HistoryQueryFilters();
			var relationshipTypeIds = new List<int>();
			//List<int> LifeCycleStatusTypeIds = new List<int>();

			int entityTypeId = 8;
			if ( entityTypeId > 0 )
				entityTypeQuery = Query<PathwayIndex>.Match( ts => ts.Field( f => f.EntityTypeId ).Query( entityTypeId.ToString() ) );


			//20-04-16 mparsons - set a default value for LifeCycleStatusTypeQuery to exclude deprecated. Will be overridden if any LifeCycleStatusType status are provided. might be better to have simple property of IsActive (non ceased)
			//if ( UtilityManager.GetAppKeyValue( "hidingCeasedStatus", false ) )
			//{
			//	var defStatus = CodesManager.Property_GetValues( CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, "", false );
			//	var defaultStatusTypeIds = defStatus.Where( s => s.Title.Contains( "Ceased" ) == false ).Select( s => s.Id ).ToList();
			//	LifeCycleStatusTypeQuery = Query<PathwayIndex>.Terms( ts => ts.Field( f => f.LifeCycleStatusTypeId ).Terms( defaultStatusTypeIds.ToArray() ) );
			//}


			#region Subject Areas.keywords
			//from widget and from search filters
			if ( query.FiltersV2.Any( x => x.Name == "subjects" ) )
			{
				subjectsQuery = HandleSubjectAreas<PathwayIndex>( query );
			}
			//keywords from widget
			if ( query.FiltersV2.Any( x => x.Name == "keywords" ) )
			{
				keywordsQuery = HandleWidgetKeywords<PathwayIndex>( query );
			}
			#endregion

			#region Properties

			if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CODE ) )
			{
				var asmtUseIds = new List<int>();
				var reportIds = new List<int>();

				foreach ( var filter in query.FiltersV2.Where( m => m.Type == MainSearchFilterV2Types.CODE ).ToList() )
				{
					var item = filter.AsCodeItem();
					if ( filter.Name == "reports" || filter.Name == "otherfilters" || filter.Name == "filter:OtherFilters" )
						reportIds.Add( item.Id );
				}

				if ( reportIds.Any() )
				{
					reportsQuery = Query<PathwayIndex>.Terms( ts => ts.Field( f => f.ReportFilters ).Terms<int>( reportIds.ToArray() ) );
				}
			}

			#endregion

			commonOrgRolesBYQuery = CommonOrgRolesByFilter<PathwayIndex>( query );
			publishedBYQuery = PublishedByFilter<PathwayIndex>( query );

			//if ( query.FiltersV2.Count > 0 )
			//{
			//	//NOTE: not used - so purpose? should be part of the ownedByQuery?
			//	var assurances = new List<CodeItem>();
			//	//will only be one owner
			//	var orgId = 0;
			//	if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CUSTOM ) )
			//	{
			//		foreach ( var filter in query.FiltersV2.Where( m => m.Name == "organizationroles" ).ToList() )
			//		{
			//			var cc = filter.AsOrgRolesItem();
			//			orgId = cc.Id;

			//			assurances.Add( filter.AsOrgRolesItem() );
			//			break;
			//		}
			//	}
			//	if ( orgId > 0 )
			//		ownedByQuery = Query<PathwayIndex>.Match( ts => ts.Field( f => f.OwnerOrganizationId ).Query( orgId.ToString() ) );
			//}
			#region Pathway Ids list
			QueryContainer recordIdListQuery = null;
			if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CUSTOM ) )
			{
				var idsList = new List<int>();
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "potentialresults" ).ToList() )
				{
					idsList.AddRange( JsonConvert.DeserializeObject<List<int>>( filter.CustomJSON ) );
				}

				if ( idsList.Any() )
				{
					idsList.ForEach( x =>
					{
						recordIdListQuery |= Query<PathwayIndex>.Terms( ts => ts.Field( f => f.Id ).Terms( x ) );
					} );
				}
			}
			#endregion


			collectionsQuery = CommonInCollectionByIdFilters<PathwayIndex>( query );
			if ( collectionsQuery == null )
				collectionsQuery = CommonPartOfCollectionFilters<PathwayIndex>( query );
			#region Occupations, industries
			occupationsQuery = CommonOccupations<PathwayIndex>( query, ref widgetOccupationsQuery );

			industriesQuery = CommonIndustries<PathwayIndex>( query, ref windustriesQuery );
			#endregion

			#region Query

			var sort = new SortDescriptor<PathwayIndex>();

			var sortOrder = query.SortOrder;
			if ( sortOrder == "alpha" )
				sort.Ascending( s => s.Name.Suffix( "keyword" ) );
			else if ( sortOrder == "zalpha" )
				sort.Descending( s => s.Name.Suffix( "keyword" ) );
			else if ( sortOrder == "newest" )
				sort.Field( f => f.LastUpdated, SortOrder.Descending );
			else if ( sortOrder == "oldest" )
				sort.Field( f => f.LastUpdated, SortOrder.Ascending );
			else if ( sortOrder == "relevance" )
				sort.Descending( SortSpecialField.Score );
			//
			QueryContainer phrasePrefixQuery = SetPhrasePrefixQuery<PathwayIndex>( query );
			QueryContainer bestFieldsQuery = SetBestFieldsQuery<PathwayIndex>( query );
			QueryContainer autoCompleteQuery = SetAutocompleteQuery<PathwayIndex>( query );
			//
			var pageSize = query.PageSize;
			if ( requestIsAutocomplete )
			{
				pageSize = pageSize * 2;
				phrasePrefixQuery = null;
				bestFieldsQuery = null;
			}
			else
			{
				autoCompleteQuery = null;
			}
			//
			if ( query.StartPage < 1 )
				query.StartPage = 1;

			var search = EC.Search<PathwayIndex>( body => body
				   .Index( PathwayIndex )
				   .Query( q =>
					  recordIdListQuery
					  //&& ownedByQuery
					  && subjectsQuery
					  && collectionsQuery
					  //&& wsubjectsQuery         //widget specific!!!
					  && keywordsQuery
					  && occupationsQuery
					  && industriesQuery
					  && commonOrgRolesBYQuery && publishedBYQuery
					  //&& LifeCycleStatusTypeQuery
					  && qaFilterQuery
					  && history.CreatedFromQuery && history.CreatedToQuery && history.HistoryFromQuery && history.HistoryToQuery
					  && reportsQuery
					  && ( phrasePrefixQuery || autoCompleteQuery || bestFieldsQuery )
				   )
				.TrackTotalHits( true )
				.Sort( s => sort )
				.From( query.StartPage > 0 ? query.StartPage - 1 : 1 )
				.Skip( ( query.StartPage - 1 ) * query.PageSize )
				.Size( pageSize ) );


			#endregion

			var debug = search.DebugInformation;
			pTotalRows = ( int ) search.Total;
			if ( requestIsAutocomplete )
			{
				var requestList = ( List<PathwayIndex> ) search.Documents;
				autocompleteList = requestList.Select( x => x.ListTitle ).Distinct().Take( query.PageSize ).ToList();
				return null;
			}
			if ( query.StartPage == 1 && ( query.Filters.Count > 0 || query.FiltersV2.Count > 0 || !string.IsNullOrWhiteSpace( query.Keywords ) ) )
			{
				//actually extract the request: between #request and #response
				var elasticQuery = UtilityManager.ExtractNameValue( debug, "# Request", ":", "# Response:" );
				LoggingHelper.DoTrace( 6, string.Format( "ElasticServices.Search: {0}, Results: {1}, ElasticLog: \r\n", "Pathway", pTotalRows ) + elasticQuery, "ElasticQuery" );
			}
			if ( pTotalRows > 0 )
			{
				list = ElasticManager.Pathway_MapFromElastic( ( List<PathwayIndex> ) search.Documents );

				LoggingHelper.DoTrace( 6, string.Format( "ElasticServices.PathwaySearch. found: {0} records", pTotalRows ) );
			}
			//stats
			query.Results = pTotalRows;
			string jsoninput = JsonConvert.SerializeObject( query, JsonHelper.GetJsonSettings() );
			string searchType = "blind";
			if ( query.Filters.Count > 0 || query.FiltersV2.Count > 0 || !string.IsNullOrWhiteSpace( query.Keywords ) )
			{
				searchType = "filters selected";
			}
			if ( query.StartPage > 1 )
				searchType += " - paging";
			new ActivityServices().AddActivity( new SiteActivity()
			{ ActivityType = "Pathway", Activity = "Search", Event = searchType, Comment = jsoninput }
			);
			return list;
		}
		#endregion
		#region updates and deletes
		/// <summary>
		/// This method is only effective where documents requiring an elastic update are added to the SearchPendingReindex table.
		/// So if handlingPendingRequests is false why call, we should do nothing
		/// </summary>
		/// <param name="doingAll"></param>
		/// <param name="populatingCachesFirst"></param>
		public static void UpdateElastic( bool doingAll, bool populatingCachesFirst = false )
		{
			//procs have been updated to use the 
			if ( populatingCachesFirst )
				new CacheManager().PopulateAllCaches( doingAll );
			List<String> messages = new List<string>();
			//
			//messages are not used here
			HandlePendingReindexRequests( ref messages );
			if ( messages.Count > 0 )
			{
				var errorMsgs = messages.Where( m => m.IndexOf( "Reindexed" ) == -1 ).ToList();
				//No, there are successful messages in the list
				if ( errorMsgs.Any() )
				{
					LoggingHelper.DoTrace( 1, $"{thisClassName}.UpdateElastic. Messages were encountered while handling Pending requests" );
					LoggingHelper.DoTrace( 1, messages );
				}
			}
		}
		public static void HandlePendingReindexRequests( ref List<String> messages, int entityTypeId = 0 )
		{

			LoggingHelper.DoTrace( 1, "ElasticServices.HandlePendingReindexRequests - start" );
			//could centralize cache updates here
			//could possibly use a similar filter approach as below
			int processedCnt = 0;
			string filter = "";
			messages = new List<string>();
			var nextEntityTypeId = 0;
			List<SearchPendingReindex> expected = new List<SearchPendingReindex>();
			var nextLabel = "Credential(s)";
			//or could do bulk
			// 
			//will need process to update statusId after completion
			//will need some status from the methods to ensure ok
			if ( entityTypeId == 0 || entityTypeId == 1 )
			{
				expected = SearchPendingReindexManager.GetAllPendingReindex( ref messages, 1 );
				if ( expected != null && expected.Count > 0 )
				{
					LoggingHelper.DoTrace( 1, $"ElasticServices.HandlePendingReindexRequests. Found {expected.Count} {nextLabel} to reindex." );

					filter = "(base.Id in (SELECT [RecordId]  FROM [dbo].[SearchPendingReindex] where [EntityTypeId]= 1 And [StatusId] = 1 AND IsUpdateOrDeleteTypeId= 1 ) )";
					Credential_UpdateIndex( filter, ref processedCnt );
					//check and reset as needed
					ResetSearchPendingReindex( 1, expected.Count, processedCnt, nextLabel, ref messages );
					//if ( processedCnt > 0 )
					//{
					//	LoggingHelper.DoTrace( 1, $"Reindexed {processedCnt} credentials." );
					//	if ( processedCnt == expected.Count() )
					//	{
					//		messages.Add( $"Reindexed {processedCnt} credentials." );
					//		new SearchPendingReindexManager().UpdateAll( 1, ref messages, 1 );
					//	}
					//	else
					//	{
					//		//only skip if there was a small number processed
					//		var diff = expected.Count() - processedCnt;
					//		if ( diff < 10 && processedCnt > 100 )
					//		{
					//			messages.Add( $"Expected to reindex {expected.Count()} credentials, but only did: {processedCnt}. Regardless, do the update of SearchPendingReindex!!" );
					//			new SearchPendingReindexManager().UpdateAll( 1, ref messages, 1 );
					//		}
					//		else
					//		{
					//			messages.Add( $"Expected to reindex {expected.Count()} credentials, but only did: {processedCnt}. So skipped the update of SearchPendingReindex!!" );
					//			//new SearchPendingReindexManager().UpdateAll( 1, ref messages, 1 );
					//		}
					//	}
					//} else
					//{
					//	messages.Add( $"Expected to reindex {expected.Count()} credentials, but only did: {processedCnt}. INVESTIGATE!!" );
					//}
				}
			}

			//org
			processedCnt = 0;
			if ( entityTypeId == 0 || entityTypeId == 2 || entityTypeId == 13 || entityTypeId == 14 )
			{
				nextLabel = "Organization(s)";
				var entityIds = new List<int>() { 2, 13, 14 };
				expected = SearchPendingReindexManager.GetAllPendingReindex( ref messages, entityIds );
				if ( expected != null && expected.Count > 0 )
				{
					LoggingHelper.DoTrace( 1, $"ElasticServices.HandlePendingReindexRequests. Found {expected.Count} {nextLabel} to reindex." );

					filter = "(base.Id in (SELECT [RecordId]  FROM [dbo].[SearchPendingReindex] where [EntityTypeId] in ( 2, 13, 14) And [StatusId] = 1 AND IsUpdateOrDeleteTypeId= 1 ) )";
					Organization_UpdateIndex( filter, ref processedCnt );
					//check and reset as needed
					ResetSearchPendingReindex( 0, expected.Count, processedCnt, nextLabel, ref messages, entityIds );
				}
			}

			//asmts
			processedCnt = 0;
			nextEntityTypeId = CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE;
			if ( entityTypeId == 0 || entityTypeId == nextEntityTypeId )
			{
				expected = SearchPendingReindexManager.GetAllPendingReindex( ref messages, nextEntityTypeId );
				if ( expected != null && expected.Count > 0 )
				{
					nextLabel = "Assessments(s)";
					filter = $"(base.Id in (SELECT [RecordId]  FROM [dbo].[SearchPendingReindex] where [EntityTypeId]= {nextEntityTypeId} And [StatusId] = 1 AND IsUpdateOrDeleteTypeId= 1 ) )";
					Assessment_UpdateIndex( filter, ref processedCnt );
					//check and reset as needed
					ResetSearchPendingReindex( nextEntityTypeId, expected.Count, processedCnt, nextLabel, ref messages );
				}
			}

			//lopps
			processedCnt = 0;
			nextLabel = "Learning Opportunties";

			if ( entityTypeId == 0 || entityTypeId == 7 || entityTypeId == 36 || entityTypeId == 37 )
			{
				var entityIds = new List<int>() { 7, 36, 37 };
				expected = SearchPendingReindexManager.GetAllPendingReindex( ref messages, entityIds );
				if ( expected != null && expected.Count > 0 )
				{
					filter = "(base.Id in (SELECT [RecordId]  FROM [dbo].[SearchPendingReindex] where [EntityTypeId] in ( 7, 36, 37) And [StatusId] = 1 AND IsUpdateOrDeleteTypeId= 1 ) )";
					LearningOpp_UpdateIndex( filter, ref processedCnt );
					//check and reset as needed
					ResetSearchPendingReindex( 0, expected.Count, processedCnt, nextLabel, ref messages, entityIds );
				}
			}
			nextLabel = "Competency Frameworks";
			processedCnt = 0;
			if ( entityTypeId == 0 || entityTypeId == 10 )
			{
				expected = SearchPendingReindexManager.GetAllPendingReindex( ref messages, 10 );
				if ( expected != null && expected.Count > 0 )
				{
					filter = "(base.Id in (SELECT [RecordId]  FROM [dbo].[SearchPendingReindex] where [EntityTypeId]= 10 And [StatusId] = 1 AND IsUpdateOrDeleteTypeId= 1 ) )";
					CompetencyFramework_UpdateIndex( filter, ref processedCnt );
					//check and reset as needed
					ResetSearchPendingReindex( 10, expected.Count, processedCnt, nextLabel, ref messages );
				}
			}
			//collections
			nextLabel = "Collections";
			processedCnt = 0;
			if ( entityTypeId == 0 || entityTypeId == CodesManager.ENTITY_TYPE_COLLECTION )
			{
				expected = SearchPendingReindexManager.GetAllPendingReindex( ref messages, CodesManager.ENTITY_TYPE_COLLECTION );
				if ( expected != null && expected.Count > 0 )
				{
					filter = "(base.Id in (SELECT [RecordId]  FROM [dbo].[SearchPendingReindex] where [EntityTypeId]= 9 And [StatusId] = 1 AND IsUpdateOrDeleteTypeId= 1 ) )";
					General_UpdateIndexForCollection( filter, ref processedCnt );
					//check and reset as needed
					ResetSearchPendingReindex( CodesManager.ENTITY_TYPE_COLLECTION, expected.Count, processedCnt, nextLabel, ref messages );
				}
			}
			//outcome data
			processedCnt = 0;
			nextEntityTypeId = CodesManager.ENTITY_TYPE_DATASET_PROFILE;
			nextLabel = "DataSetProfile(s)";
			if ( entityTypeId == 0 || entityTypeId == nextEntityTypeId )
			{

				expected = SearchPendingReindexManager.GetAllPendingReindex( ref messages, nextEntityTypeId );
				if ( expected != null && expected.Count > 0 )
				{
					filter = $"(base.Id in (SELECT [RecordId]  FROM [dbo].[SearchPendingReindex] where [EntityTypeId]= {nextEntityTypeId} And [StatusId] = 1 AND IsUpdateOrDeleteTypeId= 1 ) )";
					//
					General_UpdateIndexForOutcomeData( filter, ref processedCnt );
					//check and reset as needed
					ResetSearchPendingReindex( nextEntityTypeId, expected.Count, processedCnt, nextLabel, ref messages );
				}
			}
			//TVP
			processedCnt = 0;
			nextEntityTypeId = CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE;
			nextLabel = "TransferValueProfile(s)";
			if ( entityTypeId == 0 || entityTypeId == nextEntityTypeId )
			{
				expected = SearchPendingReindexManager.GetAllPendingReindex( ref messages, nextEntityTypeId );
				if ( expected != null && expected.Count > 0 )
				{
					filter = $"(base.Id in (SELECT [RecordId]  FROM [dbo].[SearchPendingReindex] where [EntityTypeId]= {nextEntityTypeId} And [StatusId] = 1 AND IsUpdateOrDeleteTypeId= 1 ) )";
					//
					General_UpdateIndexForTVP( filter, ref processedCnt );
					//check and reset as needed
					ResetSearchPendingReindex( nextEntityTypeId, expected.Count, processedCnt, nextLabel, ref messages );
				}
			}
			//transferIntermediary
			processedCnt = 0;
			nextEntityTypeId = CodesManager.ENTITY_TYPE_TRANSFER_INTERMEDIARY;
			nextLabel = "TransferIntermediaries";
			if ( entityTypeId == 0 || entityTypeId == nextEntityTypeId )
			{
				expected = SearchPendingReindexManager.GetAllPendingReindex( ref messages, nextEntityTypeId );
				if ( expected != null && expected.Count > 0 )
				{
					filter = $"(base.Id in (SELECT [RecordId]  FROM [dbo].[SearchPendingReindex] where [EntityTypeId]= {nextEntityTypeId} And [StatusId] = 1 AND IsUpdateOrDeleteTypeId= 1 ) )";
					General_UpdateIndexForTransferIntermediary( filter, ref processedCnt );
					//check and reset as needed
					ResetSearchPendingReindex( nextEntityTypeId, expected.Count, processedCnt, nextLabel, ref messages );
				}
			}
			//pathway
			processedCnt = 0;
			nextEntityTypeId = CodesManager.ENTITY_TYPE_PATHWAY;
			nextLabel = "Pathway(s)";
			if ( entityTypeId == 0 || entityTypeId == nextEntityTypeId )
			{
				expected = SearchPendingReindexManager.GetAllPendingReindex( ref messages, nextEntityTypeId );
				if ( expected != null && expected.Count > 0 )
				{
					filter = $"(base.Id in (SELECT [RecordId]  FROM [dbo].[SearchPendingReindex] where [EntityTypeId]= {nextEntityTypeId} And [StatusId] = 1 AND IsUpdateOrDeleteTypeId= 1 ) )";
					Pathway_UpdateIndex( filter, ref processedCnt );
					//check and reset as needed
					ResetSearchPendingReindex( nextEntityTypeId, expected.Count, processedCnt, nextLabel, ref messages );
				}
			}
			//pathwaySet
			//not sure this is implemented. It was to use the common index
			processedCnt = 0;
			nextEntityTypeId = CodesManager.ENTITY_TYPE_PATHWAY_SET;
			nextLabel = "Pathway Set(s)";
			if ( entityTypeId == 0 || entityTypeId == nextEntityTypeId )
			{
				expected = SearchPendingReindexManager.GetAllPendingReindex( ref messages, nextEntityTypeId );
				if ( expected != null && expected.Count > 0 )
				{
					filter = $"(base.Id in (SELECT [RecordId]  FROM [dbo].[SearchPendingReindex] where [EntityTypeId]= {nextEntityTypeId} And [StatusId] = 1 AND IsUpdateOrDeleteTypeId= 1 ) )";
					//
					General_UpdateIndexForPathwaySet( filter, ref processedCnt );
					//check and reset as needed
					ResetSearchPendingReindex( nextEntityTypeId, expected.Count, processedCnt, nextLabel, ref messages );
				}
			}
			//support service
			processedCnt = 0;
			nextEntityTypeId = CodesManager.ENTITY_TYPE_SUPPORT_SERVICE;
			nextLabel = "Support Service";
			if ( entityTypeId == 0 || entityTypeId == nextEntityTypeId )
			{
				expected = SearchPendingReindexManager.GetAllPendingReindex( ref messages, nextEntityTypeId );
				if ( expected != null && expected.Count > 0 )
				{
					filter = $"(base.Id in (SELECT [RecordId]  FROM [dbo].[SearchPendingReindex] where [EntityTypeId]= {nextEntityTypeId} And [StatusId] = 1 AND IsUpdateOrDeleteTypeId= 1 ) )";
					processedCnt = General_UpdateIndexForSupportService( filter );
					//check and reset as needed
					ResetSearchPendingReindex( nextEntityTypeId, expected.Count, processedCnt, nextLabel, ref messages );
				}
			}
			//ScheduledOffering
			processedCnt = 0;
			nextEntityTypeId = CodesManager.ENTITY_TYPE_SCHEDULED_OFFERING;
			nextLabel = "Scheduled Offering";
			if ( entityTypeId == 0 || entityTypeId == nextEntityTypeId )
			{
				expected = SearchPendingReindexManager.GetAllPendingReindex( ref messages, nextEntityTypeId );
				if ( expected != null && expected.Count > 0 )
				{
					filter = $"(base.Id in (SELECT [RecordId]  FROM [dbo].[SearchPendingReindex] where [EntityTypeId]= {nextEntityTypeId} And [StatusId] = 1 AND IsUpdateOrDeleteTypeId= 1 ) )";
					General_UpdateIndexForScheduledOffering( filter, ref processedCnt );
					//check and reset as needed
					ResetSearchPendingReindex( nextEntityTypeId, expected.Count, processedCnt, nextLabel, ref messages );
				}
			}
			//ProgressionModel
			processedCnt = 0;
			nextEntityTypeId = CodesManager.ENTITY_TYPE_PROGRESSION_MODEL;
			nextLabel = "ProgressionModel";
			if ( entityTypeId == 0 || entityTypeId == nextEntityTypeId )
			{
				expected = SearchPendingReindexManager.GetAllPendingReindex( ref messages, nextEntityTypeId );
				if ( expected != null && expected.Count > 0 )
				{
					filter = $"(base.Id in (SELECT [RecordId]  FROM [dbo].[SearchPendingReindex] where [EntityTypeId]= {nextEntityTypeId} And [StatusId] = 1 AND IsUpdateOrDeleteTypeId= 1 ) )";
					General_UpdateIndexForProgressionModel( filter, ref processedCnt );
					//check and reset as needed
					ResetSearchPendingReindex( nextEntityTypeId, expected.Count, processedCnt, nextLabel, ref messages );
				}
			}
			//ConceptScheme
			processedCnt = 0;
			nextEntityTypeId = CodesManager.ENTITY_TYPE_CONCEPT_SCHEME;
			nextLabel = "ConceptScheme(s)";
			if ( entityTypeId == 0 || entityTypeId == nextEntityTypeId )
			{
				expected = SearchPendingReindexManager.GetAllPendingReindex( ref messages, nextEntityTypeId );
				if ( expected != null && expected.Count > 0 )
				{
					filter = $"(base.Id in (SELECT [RecordId]  FROM [dbo].[SearchPendingReindex] where [EntityTypeId]= {nextEntityTypeId} And [StatusId] = 1 AND IsUpdateOrDeleteTypeId= 1 ) )";
					General_UpdateIndexForConceptScheme( filter, ref processedCnt );
					//check and reset as needed
					ResetSearchPendingReindex( nextEntityTypeId, expected.Count, processedCnt, nextLabel, ref messages );
				}
			}
			//Occupations
			processedCnt = 0;
			nextEntityTypeId = CodesManager.ENTITY_TYPE_OCCUPATIONS_PROFILE;
			nextLabel = "Occupations";
			if ( entityTypeId == 0 || entityTypeId == nextEntityTypeId )
			{
				expected = SearchPendingReindexManager.GetAllPendingReindex( ref messages, nextEntityTypeId );
				if ( expected != null && expected.Count > 0 )
				{
					filter = $"(base.Id in (SELECT [RecordId]  FROM [dbo].[SearchPendingReindex] where [EntityTypeId]= {nextEntityTypeId} And [StatusId] = 1 AND IsUpdateOrDeleteTypeId= 1 ) )";
					General_UpdateIndexForOccupation( filter, ref processedCnt );
					//check and reset as needed
					ResetSearchPendingReindex( nextEntityTypeId, expected.Count, processedCnt, nextLabel, ref messages );
				}
			}
			//Jobs
			processedCnt = 0;
			nextEntityTypeId = CodesManager.ENTITY_TYPE_JOB_PROFILE;
			nextLabel = "Jobs";
			if ( entityTypeId == 0 || entityTypeId == nextEntityTypeId )
			{
				expected = SearchPendingReindexManager.GetAllPendingReindex( ref messages, nextEntityTypeId );
				if ( expected != null && expected.Count > 0 )
				{
					filter = $"(base.Id in (SELECT [RecordId]  FROM [dbo].[SearchPendingReindex] where [EntityTypeId]= {nextEntityTypeId} And [StatusId] = 1 AND IsUpdateOrDeleteTypeId= 1 ) )";
					//					******
					General_UpdateIndexForJob( filter, ref processedCnt );
					//check and reset as needed
					ResetSearchPendingReindex( nextEntityTypeId, expected.Count, processedCnt, nextLabel, ref messages );
				}
			}
			//WorkRole
			processedCnt = 0;
			nextEntityTypeId = CodesManager.ENTITY_TYPE_WORKROLE_PROFILE;
			nextLabel = "WorkRoles";
			if ( entityTypeId == 0 || entityTypeId == nextEntityTypeId )
			{
				expected = SearchPendingReindexManager.GetAllPendingReindex( ref messages, nextEntityTypeId );
				if ( expected != null && expected.Count > 0 )
				{
					filter = $"(base.Id in (SELECT [RecordId]  FROM [dbo].[SearchPendingReindex] where [EntityTypeId]= {nextEntityTypeId} And [StatusId] = 1 AND IsUpdateOrDeleteTypeId= 1 ) )";
					//					******
					General_UpdateIndexForWorkRole( filter, ref processedCnt );
					//check and reset as needed
					ResetSearchPendingReindex( nextEntityTypeId, expected.Count, processedCnt, nextLabel, ref messages );
				}
			}
			//Task
			processedCnt = 0;
			nextEntityTypeId = CodesManager.ENTITY_TYPE_TASK_PROFILE;
			nextLabel = "Tasks";
			if ( entityTypeId == 0 || entityTypeId == nextEntityTypeId )
			{
				expected = SearchPendingReindexManager.GetAllPendingReindex( ref messages, nextEntityTypeId );
				if ( expected != null && expected.Count > 0 )
				{
					filter = $"(base.Id in (SELECT [RecordId]  FROM [dbo].[SearchPendingReindex] where [EntityTypeId]= {nextEntityTypeId} And [StatusId] = 1 AND IsUpdateOrDeleteTypeId= 1 ) )";
					//					******
					General_UpdateIndexForTask( filter, ref processedCnt );
					//check and reset as needed
					ResetSearchPendingReindex( nextEntityTypeId, expected.Count, processedCnt, nextLabel, ref messages );
				}
			}
			//CredentialingAction
			processedCnt = 0;
			nextEntityTypeId = CodesManager.ENTITY_TYPE_CREDENTIALING_ACTION;
			nextLabel = "Quality Assurance Actions";
			if ( entityTypeId == 0 || entityTypeId == nextEntityTypeId )
			{
				expected = SearchPendingReindexManager.GetAllPendingReindex( ref messages, nextEntityTypeId );
				if ( expected != null && expected.Count > 0 )
				{
					filter = $"(base.Id in (SELECT [RecordId]  FROM [dbo].[SearchPendingReindex] where [EntityTypeId]= {nextEntityTypeId} And [StatusId] = 1 AND IsUpdateOrDeleteTypeId= 1 ) )";
					//					******
					General_UpdateIndexForCredentialingAction( filter, ref processedCnt );
					//check and reset as needed
					ResetSearchPendingReindex( nextEntityTypeId, expected.Count, processedCnt, nextLabel, ref messages );
				}
			}

			//rubric
			processedCnt = 0;
			nextEntityTypeId = CodesManager.ENTITY_TYPE_RUBRIC;
			nextLabel = "Rubrics";
			if ( entityTypeId == 0 || entityTypeId == nextEntityTypeId )
			{
				expected = SearchPendingReindexManager.GetAllPendingReindex( ref messages, nextEntityTypeId );
				if ( expected != null && expected.Count > 0 )
				{
					filter = $"(base.Id in (SELECT [RecordId]  FROM [dbo].[SearchPendingReindex] where [EntityTypeId]= {nextEntityTypeId} And [StatusId] = 1 AND IsUpdateOrDeleteTypeId= 1 ) )";
					//					******
					General_UpdateIndexForRubric( filter, ref processedCnt );
					//check and reset as needed
					ResetSearchPendingReindex( nextEntityTypeId, expected.Count, processedCnt, nextLabel, ref messages );
				}
			}
			//

			if ( messages.Count == 0 )
				messages.Add( "There were no records found for pending re-indexing." );

			//set all in pending to resolved - not ideal - don't want to delete in case updates failed!
			//new SearchPendingReindexManager().UpdateAll( 1, ref messages );
		}

		/*
        public static void HandlePendingReindex<T>( string entityType, int nextEntityTypeId, 
				Func<string, ref int, T> General_UpdateIndexForEntity, 
				ref List<string> messages )
        {
			var processedCnt = 0;

            var expected = SearchPendingReindexManager.GetAllPendingReindex( ref messages, nextEntityTypeId );
            if (expected != null && expected.Count > 0)
            {
                var filter = $"(base.Id in (SELECT [RecordId]  FROM [dbo].[SearchPendingReindex] where [EntityTypeId]= {nextEntityTypeId} And [StatusId] = 1 AND IsUpdateOrDeleteTypeId= 1 ) )";
                //					******
                General_UpdateIndexForEntity( filter, ref processedCnt );
                if (processedCnt > 0)
                {
                    messages.Add( $"Reindexed {processedCnt} {entityType}." );
                    LoggingHelper.DoTrace( 1, $"Reindexed {processedCnt} {entityType}." );
                    new SearchPendingReindexManager().UpdateAll( 1, ref messages, nextEntityTypeId );
                }
            }
        }

		*/
		//a little late but a method with a passed Func would make the PendingIndex updates very easy. 
		public delegate void UpdateIndexMethodDelegate( string filter, ref int processedCount );
		public static void HandlePendingReindex_GeneralIndex( int entityTypeId, string label, Func<string, int> generalUpdateIndexMethod, ref List<String> messages )
		{

			//UpdateIndexMethodDelegate myDelegate = generalUpdateIndexMethod;
			int processedCount = 0;
			var expected = SearchPendingReindexManager.GetAllPendingReindex( ref messages, entityTypeId );
			if ( expected != null && expected.Count > 0 )
			{
				var filter = $"(base.Id in (SELECT [RecordId]  FROM [dbo].[SearchPendingReindex] where [EntityTypeId]= {entityTypeId} And [StatusId] = 1 AND IsUpdateOrDeleteTypeId= 1 ) )";
				processedCount = generalUpdateIndexMethod( filter );
				//or could some how do with this
				//myDelegate( filter, ref processedCount );
				//check and reset as needed
				ResetSearchPendingReindex( entityTypeId, expected.Count, processedCount, label, ref messages );
			}
		}

		public static void ResetSearchPendingReindex( int entityTypeId, int expectedCount, int processedCount, string label, ref List<String> messages, List<int> entityTypeIds = null )
		{
			if ( entityTypeIds == null || !entityTypeIds.Any())
			{
				entityTypeIds = new List<int>() { entityTypeId };
			}
			if ( processedCount > 0 )
			{
				LoggingHelper.DoTrace( 1, $"Reindexed {processedCount} {label}." );
				if ( processedCount == expectedCount )
				{
					messages.Add( $"Reindexed {processedCount} {label}." );
					if (!new SearchPendingReindexManager().UpdateAll( 1, ref messages, entityTypeIds ))
					{
						//this approach will result in messages being repeated!
						//LoggingHelper.DoTrace( CodesManager.appDefaultTraceLevel, messages );
					}
				}
				else
				{
					//only skip if there was a small number processed
					var diff = expectedCount - processedCount;
					if ( diff < 10 && processedCount > 100 )
					{
						messages.Add( $"Expected to reindex {expectedCount} {label}, but only did: {processedCount}. Regardless, did the update of SearchPendingReindex!!" );
						if (!new SearchPendingReindexManager().UpdateAll( 1, ref messages, entityTypeIds ))
						{

						}
					}
					else
					{
						messages.Add( $"Expected to reindex {expectedCount} {label}, but only did: {processedCount}. So skipped the update of SearchPendingReindex!!" );
					}
				}
			}
			else
			{
				messages.Add( $"Expected to reindex {expectedCount} {label}, but only did: {processedCount}. INVESTIGATE!!" );
			}
		}
		public static void HandlePendingDeletes( ref List<String> messages )
		{
			SearchPendingReindexManager mgr = new SearchPendingReindexManager();
			//List<String> messages = new List<string>();
			bool resettingPendingRecordImmediately = true;
			List<SearchPendingReindex> list = SearchPendingReindexManager.GetAllPendingDeletes();
			if ( list == null || list.Count == 0 )
			{
				messages.Add( "HandlePendingDeletes: No pending delete requests were found" );
				return;
			}
			int cntr = 0;
			foreach ( var item in list )
			{
				cntr++; //could have separate counters by etype
				switch ( item.EntityTypeId )
				{
					case 1:
						{
							var response = EC.Delete<CredentialIndex>( item.RecordId, d => d.Index( CredentialIndex ) );
							messages.Add( string.Format( "Removed credential #{0} from elastic index", item.RecordId ) );
						}
						break;
					case 2:
						{
							var response = EC.Delete<OrganizationIndex>( item.RecordId, d => d.Index( OrganizationIndex ) );
							messages.Add( string.Format( "Removed organization #{0} from elastic index", item.RecordId ) );
						}
						break;
					case 3:
						{
							var response = EC.Delete<AssessmentIndex>( item.RecordId, d => d.Index( AssessmentIndex ) );
							messages.Add( string.Format( "Removed assessment #{0} from elastic index", item.RecordId ) );
						}
						break;
					case 7:
						{
							var response = EC.Delete<LearningOppIndex>( item.RecordId, d => d.Index( LearningOppIndex ) );
							messages.Add( string.Format( "Removed lopp #{0} from elastic index", item.RecordId ) );
						}
						break;
					case 8:
					{
						//Problem, can't use record ID for the common index, could delete the wrong record
						//		 DELETE: /common_index/genericindex/178
						//var response = EC.Delete<PathwayIndex>( item.RecordId, d => d.Index( PathwayIndex ).Type( "genericindex" ) );

						//this works in the head plugin - to return the record
						//YES - did delete
						var response5 = EC.DeleteByQuery<PathwayIndex>( q => q.Query( rq => rq
								  .Bool( mm => mm
										 .Must( mu =>
													mu.Terms( m => m.Field( f => f.EntityTypeId ).Terms( "8" ) )
												&&
													mu.Terms( m => m.Field( f => f.Id ).Terms( item.RecordId )
												)
										)//Must
									) //Bool
							 ).Index( PathwayIndex )
						);

						messages.Add( string.Format( "Removed pathway #{0} from elastic index", item.RecordId ) );
					}
					break;
					case 9:
						{

							//Problem, can't use record ID for the common index, could delete the wrong record
							//var response = EC.Delete<GenericIndex>( item.RecordId, d => d.Index( CommonIndex ).Type( "genericindex" ) );

							var response = EC.DeleteByQuery<GenericIndex>( q => q.Query( rq => rq
									  .Bool( mm => mm
											 .Must( mu =>
														mu.Terms( m => m.Field( f => f.EntityTypeId ).Terms( "9" ) )
													&&
														mu.Terms( m => m.Field( f => f.Id ).Terms( item.RecordId )
													)
											)//Must
										) //Bool
								 ).Index( CommonIndex )
							);
							messages.Add( string.Format( "Removed Collection #{0} from elastic index", item.RecordId ) );
						}
						break;
					case 10:
					{
						var response = EC.Delete<CompetencyFrameworkIndex>( item.RecordId, d => d.Index( CompetencyFrameworkIndex) ); ;
						messages.Add( string.Format( "Removed Competency framework #{0} from elastic index", item.RecordId ) );
					}
					break;
					case 11:
                        {
                            var response = EC.DeleteByQuery<GenericIndex>( q => q.Query( rq => rq
                                   .Bool( mm => mm
                                          .Must( mu =>
                                                     mu.Terms( m => m.Field( f => f.EntityTypeId ).Terms( "11" ) ) //???
                                                 &&
                                                     mu.Terms( m => m.Field( f => f.Id ).Terms( item.RecordId )
                                                 )
                                         )//Must
                                     ) //Bool
                              ).Index( CommonIndex )
                         );
                            messages.Add( string.Format( "Removed Concept scheme #{0} from elastic index", item.RecordId ) );
                        }
                        break;
                    case 12:
                        {
                            var response = EC.DeleteByQuery<GenericIndex>( q => q.Query( rq => rq
                                   .Bool( mm => mm
                                          .Must( mu =>
                                                     mu.Terms( m => m.Field( f => f.EntityTypeId ).Terms( "12" ) ) //???
                                                 &&
                                                     mu.Terms( m => m.Field( f => f.Id ).Terms( item.RecordId )
                                                 )
                                         )//Must
                                     ) //Bool
                              ).Index( CommonIndex )
                         );
                            messages.Add( string.Format( "Removed Progression Model #{0} from elastic index", item.RecordId ) );
                        }
                        break;
                    case 15:
                        {
                            var response = EC.DeleteByQuery<GenericIndex>( q => q.Query( rq => rq
                                   .Bool( mm => mm
                                          .Must( mu =>
                                                     mu.Terms( m => m.Field( f => f.EntityTypeId ).Terms( "15" ) ) //???
                                                 &&
                                                     mu.Terms( m => m.Field( f => f.Id ).Terms( item.RecordId )
                                                 )
                                         )//Must
                                     ) //Bool
                              ).Index( CommonIndex )
                         );
                            messages.Add( string.Format( "Removed Scheduled Offering #{0} from elastic index", item.RecordId ) );
                        }
                        break;
                    case 26:
					{
						//Problem, can't use record ID for the common index, could delete the wrong record
						//var response = EC.Delete<GenericIndex>( item.RecordId, d => d.Index( CommonIndex ).Type( "genericindex" ) );

						var response = EC.DeleteByQuery<GenericIndex>( q => q.Query( rq => rq
								  .Bool( mm => mm
										 .Must( mu =>
													mu.Terms( m => m.Field( f => f.EntityTypeId ).Terms( "26" ) )
												&&
													mu.Terms( m => m.Field( f => f.Id ).Terms( item.RecordId )
												)
										)//Must
									) //Bool
							 ).Index( CommonIndex )
						);
						messages.Add( string.Format( "Removed TVP #{0} from elastic index", item.RecordId ) );
					}
					break;
					case 28:
						{
							//Problem, can't use record ID for the common index, could delete the wrong record
							//var response = EC.Delete<GenericIndex>( item.RecordId, d => d.Index( CommonIndex ).Type( "genericindex" ) );

							var response = EC.DeleteByQuery<GenericIndex>( q => q.Query( rq => rq
									  .Bool( mm => mm
											 .Must( mu =>
														mu.Terms( m => m.Field( f => f.EntityTypeId ).Terms( "28" ) )
													&&
														mu.Terms( m => m.Field( f => f.Id ).Terms( item.RecordId )
													)
											)//Must
										) //Bool
								 ).Index( CommonIndex )
							);
							messages.Add( string.Format( "Removed Transfer Intermediary #{0} from elastic index", item.RecordId ) );
						}
						break;
                    case 31:
                    case 32:
                    case 33:
					case 34:
                    case 35:
                        {
                            //Problem, can't use record ID for the common index, could delete the wrong record
                            //var response = EC.Delete<GenericIndex>( item.RecordId, d => d.Index( CommonIndex ).Type( "genericindex" ) );

                            var response = EC.DeleteByQuery<GenericIndex>( q => q.Query( rq => rq
                                      .Bool( mm => mm
                                             .Must( mu =>
                                                        mu.Terms( m => m.Field( f => f.EntityTypeId ).Terms( item.EntityTypeId.ToString() ) )
                                                    &&
                                                        mu.Terms( m => m.Field( f => f.Id ).Terms( item.RecordId )
                                                    )
                                            )//Must
                                        ) //Bool
                                 ).Index( CommonIndex )
                            );
                            messages.Add( $"Removed {item.EntityTypeId} #{item.RecordId} from elastic index" );
                        }
                        break;
                    case 38:
                        {
                            //Problem, can't use record ID for the common index, could delete the wrong record
                            //var response = EC.Delete<GenericIndex>( item.RecordId, d => d.Index( CommonIndex ).Type( "genericindex" ) );

                            var response = EC.DeleteByQuery<GenericIndex>( q => q.Query( rq => rq
                                      .Bool( mm => mm
                                             .Must( mu =>
                                                        mu.Terms( m => m.Field( f => f.EntityTypeId ).Terms( "38" ) )
                                                    &&
                                                        mu.Terms( m => m.Field( f => f.Id ).Terms( item.RecordId )
                                                    )
                                            )//Must
                                        ) //Bool
                                 ).Index( CommonIndex )
                            );
                            messages.Add( string.Format( "Removed Support Service #{0} from elastic index", item.RecordId ) );
                        }
                        break;
                }

				if ( resettingPendingRecordImmediately )
				{
					item.StatusId = 2;
					mgr.Update( item, ref messages );
				}

			}
			messages.Add( string.Format( "HandlePendingDeletes: Removed {0} documents from elastic index", cntr ) );
			//reset pending deletes, or do one at a time
			//should be sure that all deletes were successful
			if ( !resettingPendingRecordImmediately )
			{
				mgr.UpdateAll( 2, ref messages );
			}


			//bulk deletes. Will want a bulk delete when using SearchPendingReindex!
			//https://stackoverflow.com/questions/31028839/how-to-delete-several-documents-by-id-in-one-operation-using-elasticsearch-nest/31029136
			/*
             * To use esClient.DeleteMany(..) you have to pass collection of objects to delete.

            var objectsToDelete = new List<YourType> {.. };
            var bulkResponse = client.DeleteMany<YourType>(objectsToDelete);

            *You can get around this by using following code:

            var ids = new List<string> {"1", "2", "3"};
            var bulkResponse = client.DeleteMany<YourType>(ids.Select(x => new YourType { Id = x }));

            *Third option, use bulk delete:

            var bulkResponse = client.Bulk(new BulkRequest
            {
                Operations = ids.Select(x => new BulkDeleteOperation<YourType>(x)).Cast<IBulkOperation>().ToList()
            });
             * 
             */

			}


			public static bool Search( List<string> field, List<string> list )
		{
			return list.Any( x => field.Any( y => y == x ) );
		}
		private static QueryContainer TermAny<T>( QueryContainerDescriptor<T> descriptor, Field field, object[] values ) where T : class
		{
			QueryContainer q = new QueryContainer();
			foreach ( var value in values )
				q |= descriptor.Term( t => t.Field( field ).Value( value ) );
			return q;
		}
		#endregion

		#region Adhoc updates

		//public void AppendField( string index, string recordId, string fieldName, int[] item, bool checkExists )
		//{
		//	AppendItem( index, recordId, fieldName, item, checkExists );
		//}

		/// <summary>
		/// Do a direct elastic update, to add widgetId to property: 
		/// </summary>
		/// <param name="index"></param>
		/// <param name="widgetId"></param>
		/// <param name="widgetProperty"></param>
		/// <param name="recordId"></param>
		/// <param name="checkExists"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public bool CredentialResourceAddWidgetId( string index, int widgetId, string widgetProperty, string recordId,  bool checkExists, ref string status )
		{
			//if (ctx._source.resourceForWidget == null) {ctx._source.resourceForWidget = new ArrayList();  ctx._source.resourceForWidget.add( params.widget_id )}
			//{ "inline", "ctx._source.resourceForWidget.add( params.widget_id )" },
			//try generic
			try
			{
				var method = string.Format( "if (ctx._source.{0} == null) ctx._source.{0} = new ArrayList();  if (!ctx._source.{0}.contains(params.widget_id)) ctx._source.{0}.add( params.widget_id )", widgetProperty );
				var query = new JObject() 
				{
					{ "script", new JObject()  {
						{ "inline",method },
						{ "lang", "painless" },
						{ "params", new JObject()
						{
							{ "widget_id", widgetId }
						}}
					} }
				};
				//var query = new JObject() {
				//	{ "script", new JObject()  {
				//		{ "inline", "if (ctx._source.resourceForWidget == null) ctx._source.resourceForWidget = new ArrayList();  if (!ctx._source.resourceForWidget.contains(params.widget_id)) ctx._source.resourceForWidget.add( params.widget_id )" },
				//		{ "lang", "painless" },
				//		{ "params", new JObject()
				//		{
				//			{ "widget_id", widgetId }
				//		}}
				//	} }
				//};

				var q1 = JsonConvert.SerializeObject( query );
				var q2 = JsonConvert.SerializeObject( query ).Replace( @"\u0027", "'" );

				//need to hard code the type, so may use specific methods by index
				//var elasticVersion = UtilityManager.GetAppKeyValue( "elasticVersion", "6.x" );
				//23-02-01 mp - note the index property is the actual name of the index from the web.config file. 
				var endpoint = FormatEndpoint( index, "CredentialIndex", recordId );

				return ContactServer( "POST", q2, endpoint, ref status );
			}
			catch ( Exception ex )
			{
				status = ex.Message;
				LoggingHelper.LogError( ex, "ElasticServices.CredentialResourceAddWidgetId" );
				return false;
			}
		}

		public bool CredentialResourceRemoveWidgetId( string index, int widgetId, string widgetProperty, string recordId, ref string status )
		{
			var method = string.Format( "if (ctx._source.{0}.contains(params.widget_id)) {1}", widgetProperty, string.Format( "ctx._source.{0}.remove(ctx._source.{0}.indexOf(params.widget_id)) ", widgetProperty) );
			try
			{
				//should have an existance check just in case. 
				var query = new JObject() {
					{ "script", new JObject()  {
						{ "inline", method },
						{ "lang", "painless" },
						{ "params", new JObject()
							{
								{ "widget_id", widgetId }
							}
						}
					} }
				};
				////var query = new JObject() {
				////	{ "script", new JObject()  {
				////		{ "inline", "if (ctx._source.resourceForWidget.contains(params.widget_id)) { ctx._source.resourceForWidget.remove(ctx._source.resourceForWidget.indexOf(params.widget_id)) }" },
				////		{ "lang", "painless" },
				////		{ "params", new JObject()
				////			{
				////				{ "widget_id", widgetId }
				////			}
				////		}
				////	} }
				////};

				var q1 = JsonConvert.SerializeObject( query );
				var endpoint = FormatEndpoint( index, "credentialindex", recordId );
				
				return ContactServer( "POST", q1, endpoint, ref status );
				//Dictionary<string, object> dictionary = RegistryServices.JsonToDictionary( result );
			}
			catch (Exception ex )
			{
				status = ex.Message;
				LoggingHelper.LogError( ex, "ElasticServices.CredentialRemoveWidgetId" );
				return false;
			}
			//ContactServer( "POST", serializer.Serialize( query ).Replace( @"\u0027", "'" ), index + "/_update/" + recordId );
		}
		public string FormatEndpoint( string index, string type, string recordId )
		{
			var elasticVersion = UtilityManager.GetAppKeyValue( "elasticVersion", "6.x" );
			var endpoint = "";
			var endpoint5x = string.Format( "{0}/{1}/{2}/_update", index, type, recordId );
			var endpoint6x = string.Format( "{0}/{1}/{2}/_update", index, type, recordId );
			var endpoint7x = string.Format( "{0}/_update/{1}", index, recordId ); //No "types" in 7x
			if ( elasticVersion == "6.x" )
				endpoint = endpoint6x;
			else if ( elasticVersion == "7.x" )
				endpoint = endpoint7x;
			else
				endpoint = endpoint5x;

			return endpoint;
		}
		//private void AddPropertyAppendItem( string index, string recordId, string fieldName, object data, bool checkExists )
		//{
		//	dynamic query = new Dictionary<string, object>();
		//	dynamic parameters = new Dictionary<string, object>();
		//	parameters.Add( "info", data );
		//	if ( checkExists )
		//	{
		//		query.Add( "script", "ctx._source." + fieldName + ".contains(info) ? (ctx.op = 'none') : (ctx._source." + fieldName + " += info)" );
		//	}
		//	else
		//	{
		//		query.Add( "script", "ctx._source." + fieldName + " += info;" );
		//	}
		//	query.Add( "params", parameters );
		//	var result = ContactServer( "POST", serializer.Serialize( query ).Replace( @"\u0027", "'" ), index + "/_update/" + recordId );
		//}
		//public void RemoveFromWidgetTags( string index, string recordId, int collectionID )
		//{
		//	TrimField( index, recordId, "resourceForWidget", collectionID );
		//}
		//private void TrimField( string index, string recordId, string fieldName, object data )
		//{
		//	List<int> versionIDs = GetVersionIDFromrecordId( int.Parse( recordId ) );
		//	foreach ( int versionID in versionIDs )
		//	{
		//		TrimItem( index, versionID, fieldName, data );
		//		Thread.Sleep( 1000 );
		//	}
		//}
		private void TrimItem( string index, string recordId, string fieldName, object data )
		{
			string removeString = "";
			if ( data is int )
			{
				removeString = ".remove((Object) " + data + ")";
			}
			else
			{
				removeString = ".remove('" + data + "')";
			}
			var outerQuery = new
			{
				query = new
				{
					script = "ctx._source." + fieldName + removeString
				}
			};
			var result = "";
			ContactServer( "POST", serializer.Serialize( outerQuery ), index + "/_update/" + recordId, ref result );
		}


		protected bool ContactServer( string method, string json, string urlAddendum, ref string result )
		{
			int maxAttempts = 4;
			bool success = false;
			result = "";
			int attemptNbr = 0;
			string wsUrl = UtilityManager.GetAppKeyValue( "elasticSearchUrl" ) + urlAddendum;
			while ( attemptNbr < maxAttempts && !success )
			{
				try
				{
					//string wsUrl = "http://localhost:9200/" + collection + urlAddendum;
					HttpWebRequest request = ( HttpWebRequest )WebRequest.Create( wsUrl );
					request.Method = method;

					if ( method == "POST" || method == "PUT" || json.Length > 0 )
					{
						request.ContentType = "application/json; charset=utf-8";
						byte[] byteData = Encoding.UTF8.GetBytes( json );
						request.ContentLength = byteData.Length;
						Stream requestStream = request.GetRequestStream();
						requestStream.Write( byteData, 0, byteData.Length );
						request.Timeout = 15000;
					}
					HttpWebResponse response = ( HttpWebResponse )request.GetResponse();
					success = true;
					//Console.WriteLine("Server contact successful");

					StreamReader reader = new StreamReader( response.GetResponseStream() );
					result= reader.ReadToEnd();
					return true;
				}
				catch ( TimeoutException tex )
				{
					LoggingHelper.DoTrace( "ElasticSearch Timeout Encountered encountered: " + tex.ToString() );

					//LogToStaging( "Timeout Encountered at " + DateTime.Now + ": " + tex.ToString(), true );
					result = "Timeout Error: " + tex.ToString();
					return false;
				}
				catch ( Exception ex )
				{
					if ( ex.Message.IndexOf( "(404) Not Found" ) > 0 )
					{
						LoggingHelper.DoTrace( "ElasticSearch Exception encountered: " + ex.ToString() + " for " + urlAddendum );
						attemptNbr++;
						result = "Record not found: " + urlAddendum;
						return false;
					}
					else
					{
						LoggingHelper.DoTrace( "ElasticSearch Exception encountered: " + ex.ToString() + " for " + urlAddendum );
						attemptNbr++;
						result = "General Error: " + ex.ToString();
						return false;
					}

				}
			}
			return success;
		}
		#endregion
	}
	/*
	public class ElasticConfiguration
	{
		public BestFields BestFields { get; set; } = new BestFields();
		public CrossFields CrossFields { get; set; } = new CrossFields();
		public PhrasePrefix PhrasePrefix { get; set; } = new PhrasePrefix();
		public MatchPhrasePrefix MatchPhrasePrefix { get; set; } = new MatchPhrasePrefix();
	}
	/// <summary>
	/// Best Fields ignores the order of the phrase and results are based on anything with the words in the phrase.
	/// </summary>
	public class BestFields
	{
		public bool ExcludeQuery { get; set; }
		public int Name { get; set; } = 10;
		public int OwnerOrganizationName { get; set; } = 0;
		public int Description { get; set; } = 5;
		public int AlternateNames { get; set; } = 0;
		public int Keyword { get; set; } = 0;
		public int Occupation { get; set; } = 0;
		public int Industry { get; set; } = 0;
		public int InstructionalPrograms { get; set; } = 25;
		public int QualityAssurancePhrase { get; set; } = 25;
		
	}

	/// <summary>
	/// Cross Fields looks for each term in any of the fields, which is useful for queries like "nursing ohio"
	/// </summary>
	public class CrossFields
	{
		public bool ExcludeQuery { get; set; }
		public int Name { get; set; } = 5;
		public int CredentialType { get; set; } = 30;
		public int City { get; set; } = 5;
		public int Region { get; set; } = 5;
		public int Country { get; set; } = 0;
	}

	/// <summary>
	/// Phrase Prefix is looking for matches to what a user types in (i.e., the phrase)"
	/// </summary>
	public class PhrasePrefix
	{
		public bool ExcludeQuery { get; set; }
		public int Name { get; set; } = 100;
		public int NameAlphanumericOnly { get; set; } = 10;
		//
		public int OwnerOrganizationName { get; set; } = 90;
		public int Description { get; set; } = 50;
		public int AlternateNames { get; set; } = 75;
		public int Keyword { get; set; } = 20;
		public int Occupation { get; set; } = 0;
		public int Industry { get; set; } = 0;
		public int InstructionalPrograms { get; set; } = 25;
	}

	public class MatchPhrasePrefix
	{
		public bool ExcludeQuery { get; set; }
		public int Name { get; set; } = 0;
		public int Keywords { get; set; } = 0;

	}
	*/
}

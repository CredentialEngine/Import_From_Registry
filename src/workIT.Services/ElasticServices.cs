using Nest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
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
		#region Elastic related properties
		private static string CredentialIndex
		{
			get { return UtilityManager.GetAppKeyValue( "credentialCollection", "credentials" ); }
		}

		private static string OrganizationIndex
		{
			//putting all in the same index
			get { return UtilityManager.GetAppKeyValue( "organizationCollection", "organizations" ); }

		}
		private static string AssessmentIndex
		{
			get { return UtilityManager.GetAppKeyValue( "assessmentCollection", "assessments" ); }
		}
		private static string LearningOppIndex
		{
			get { return UtilityManager.GetAppKeyValue( "learningOppCollection", "learningopps" ); }
		}

		private static ElasticClient EC
		{
			get
			{
				var url = UtilityManager.GetAppKeyValue( "elasticSearchUrl", "http://localhost:9200" );
				var uri = new Uri( url );
				var settings = new ConnectionSettings( uri ).DefaultIndex( CredentialIndex );
				settings.DisableDirectStreaming();
				return new ElasticClient( settings );
			}
		}

		//public static QueryContainer countryQuery { get; private set; }

		static ElasticClient client;

		private static ElasticClient GetClient()
		{
			if ( client == null )
			{

				var url = UtilityManager.GetAppKeyValue( "elasticSearchUrl", "http://localhost:9200" );
				var uri = new Uri( url );
				var settings = new ConnectionSettings( uri ).DefaultIndex( CredentialIndex );
				settings.DisableDirectStreaming();
				client = new ElasticClient( settings );
			}

			return client;
		}
		#endregion

		static QueryContainer widgetOccupationsQuery = null;
		static QueryContainer windustriesQuery = null;
		static QueryContainer wsubjectsQuery = null;
		#region Credentials
		#region Build/update index
		public static void Credential_BuildIndex( bool deleteIndexFirst = false, bool updatingIndexRegardless = false )
		{
			try
			{

				bool indexInitialized = false;
				if ( deleteIndexFirst && EC.IndexExists( CredentialIndex ).Exists )
				{
					EC.DeleteIndex( CredentialIndex );
				}
				if ( !EC.IndexExists( CredentialIndex ).Exists )
				{
					CredentialInitializeIndex();
					indexInitialized = true;
				}

				if ( indexInitialized || updatingIndexRegardless )
				{

					new ActivityServices().AddActivity( new SiteActivity()
					{ ActivityType = "Credential", Activity = "Elastic", Event = "Build Index" }
					);
					//redentialInitializeIndex();
					int minEntityStateId = UtilityManager.GetAppKeyValue( "minEntityStateId", 3 );
					int processed = 0;
					string filter = string.Format( "base.EntityStateId >= {0}", minEntityStateId);
					Credential_UpdateIndex( filter, ref processed );
					if ( processed > 0 )
					{
						var refreshResults = EC.Refresh( CredentialIndex );
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
						LoggingHelper.LogError( "BuildCredentialIndex: no results were returned from Credential_SearchForElastic method.", true, "BuildCredentialIndex ISSUE: zero records loaded" );
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
				LoggingHelper.LogError( ex, string.Format( "UpdateCredentialIndex failed for id: {0}", recordId ), false );
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

			LoggingHelper.DoTrace( 6, "Credential_UpdateIndex. Enter. Filter: '" + filter + "' " );
			try
			{
				//actually this doesn't help unless we use an upating filter to target specific data!
				int pageSize = UtilityManager.GetAppKeyValue( "credentialRebuildPageSize", 1500 );
				int pageNbr = 1;
				int totalRows = 0;
				bool isComplete = false;
				int cntr = 0; ;
				while ( pageNbr > 0 && !isComplete )
				{
					LoggingHelper.DoTrace( 5, "Credential_UpdateIndex. Page: " + pageNbr.ToString() );
					var list = ElasticManager.Credential_SearchForElastic( filter, pageSize, pageNbr, ref totalRows );
					if ( list != null && list.Count > 0 && totalRows > 0 )
					{
						//if error encountered, first record can be an error message. 
						processed = processed + list.Count;
						if ( action == 1 )
						{
							foreach ( CredentialIndex item in list )
							{
								cntr++;
								var res = EC.Index( item, idx => idx.Index( CredentialIndex ) );
								Console.WriteLine( res.Result );
							}

						}
						else if ( action == 2 )
						{
							//maybe consider breaking this into a loop?
							cntr = cntr + list.Count;
							var result = EC.Bulk( b => b.IndexMany( list, ( d, credential ) => d.Index( CredentialIndex ).Document( credential ).Id( credential.Id.ToString() ) ) );
							if ( result.ToString().IndexOf( "Valid NEST response built from a successful low level cal" ) == -1 )
							{
								//Console.WriteLine( result.ToString() );
								LoggingHelper.DoTrace( 1, "***** Issue building credential index with filter: '" + filter + "' == " + result );
							}
						}
					}
					else
					{
						if ( list != null && list.Count == 1 && totalRows == -1 )
						{
							LoggingHelper.LogError( string.Format("UpdateCredentialIndex: Error in credential search. {0}", list[0].Description), true, "UpdateCredentialIndex EXCEPTION" );

						} else if ( pageNbr == 1 )
						{
							if ( string.IsNullOrWhiteSpace( filter ) )
							{
								LoggingHelper.LogError( "UpdateCredentialIndex: entered with no filter, but no results were returned from credential search.", true, "UpdateCredentialIndex ISSUE: zero records returned" );
							}
							LoggingHelper.DoTrace( 2, string.Format( "UpdateCredentialIndex. NOTE no data returned for filter: {0}", filter ) );
						}
						isComplete = true;
						break;
					}

					pageNbr++;
					if ( cntr >= totalRows )
					{
						isComplete = true;
					}
					LoggingHelper.DoTrace( 4, "Credential_UpdateIndex. Credentials Indexed: " + processed.ToString() );
				} //loop
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "UpdateCredentialIndex failed for filter: {0}", filter ), true );
			}

		}

		public static object CredentialDeleteDocument( int documentId )
		{
			//ex: var response = EsClient.Delete<Employee>(2, d => d.Index("employee").Type("myEmployee"));
			var response = EC.Delete<CredentialIndex>( documentId, d => d.Index( CredentialIndex ).Type( "credentialindex" ) );

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

		public static void CredentialInitializeIndex()
		{
			if ( !EC.IndexExists( CredentialIndex ).Exists )
			{
				// .String(s => s.Index(FieldIndexOption.NotAnalyzed).Name(n => n.Name))
				EC.CreateIndex( CredentialIndex, c => new CreateIndexDescriptor( CredentialIndex )
					  .Settings( st => st
						 .Analysis( an => an.TokenFilters( tf => tf.Stemmer( "custom_english_stemmer", ts => ts.Language( "english" ) ) ).Analyzers( anz => anz.Custom( "custom_lowercase_stemmed", cc => cc.Tokenizer( "standard" ).Filters( new List<string> { "lowercase", "custom_english_stemmer" } ) ) ) ) )
					  .Mappings( ms => ms
						.Map<CredentialIndex>( m => m
							.AutoMap()
							.Properties( p => p
							 .Text( s => s.Name( n => n.Description ).Analyzer( "custom_lowercase_stemmed" ) )
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
									.Name( nn => nn.Classifications )
									.AutoMap()
								)
								.Nested<IndexCompetency>( n => n
									.Name( nn => nn.Competencies )
									.AutoMap()
								)
								 //.GeoPoint( g => g.Name( n => n.AddressLocations ) )
								 .Nested<Models.Elastic.Address>( n => n
									.Name( nn => nn.Addresses )
									.AutoMap()
								) //AgentRelationshipForEntity will replace IndexQualityAssurance
								 .Nested<Models.Elastic.AgentRelationshipForEntity>( n => n
									.Name( nn => nn.AgentRelationshipsForEntity )
									.AutoMap()
								)
								//.Nested<IndexQualityAssurance>( n => n
								//	.Name( nn => nn.QualityAssurance )
								//	.AutoMap()
								//)
								//.Nested<QualityAssurancePerformed>( n => n
								//	.Name( nn => nn.QualityAssurancePerformed )
								//	.AutoMap()
								//)

								.Nested<Connection>( n => n
									.Name( nn => nn.Connections )
									.AutoMap()
								)
							)
						)
					)
				);
			}
		}
		#endregion

		#region Search
		public static List<CredentialSummary> CredentialSimpleSearch( MainSearchInput query, ref int pTotalRows )
		{
			List<CredentialSummary> list = new List<CredentialSummary>();
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
			var sort = new SortDescriptor<CredentialIndex>();

			var sortOrder = query.SortOrder;
			if ( sortOrder == "alpha" )
				sort.Ascending( s => s.Name.Suffix( "keyword" ) );
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
			var search = EC.Search<CredentialIndex>( i => i.Index( CredentialIndex ).Query( q =>
				 q.MultiMatch( m => m
								 .Fields( f => f
								  .Field( ff => ff.Name )
								  .Field( ff => ff.OwnerOrganizationName )
				 )
				 .Type( TextQueryType.PhrasePrefix )
				 .Query( query.Keywords )
				 .MaxExpansions( 10 ) ) )
				 .Sort( s => sort )
					 //.From( query.StartPage - 1 )
					 .From( query.StartPage > 0 ? query.StartPage - 1 : 1 )
					 .Skip( ( query.StartPage - 1 ) * query.PageSize )
					 .Size( query.PageSize ) );

			pTotalRows = ( int ) search.Total;

			if ( pTotalRows > 0 )
			{
				//map results
				list = ElasticManager.Credential_MapFromElastic( ( List<CredentialIndex> ) search.Documents );

				LoggingHelper.DoTrace( 6, string.Format( "ElasticServices.CredentialSearch. found: {0} records", pTotalRows ) );
			}
			//stats
			query.Results = pTotalRows;
			return list;
		}
		public static List<object> CredentialAutoComplete( string keyword, int maxTerms, ref int pTotalRows )
		{

			var search = EC.Search<CredentialIndex>( i => i.Index( CredentialIndex ).Query( q =>
				 q.MultiMatch( m => m
								 .Fields( f => f
								  .Field( ff => ff.Name )
								  .Field( ff => ff.OwnerOrganizationName )
				 )
				 .Type( TextQueryType.PhrasePrefix )
				 .Query( keyword )
				 .MaxExpansions( 10 ) ) ).Size( maxTerms ) );
			//var search = EC.Search<CredentialIndex>( i => i.Index( CredentialIndex ).Query( q => q.Term( t => t.Field( f => f.Subjects.First() ).Value( new[] { "Anatomy" } ) ) ) );

			pTotalRows = ( int ) search.Total;

			var list = ( List<CredentialIndex> ) search.Documents;
			return list.Select( x => x.ListTitle ).Distinct().ToList().Cast<object>().ToList();
		}

		#region Common 
		public static QueryContainer HandleKeywords<T>( MainSearchInput query ) where T : class, IIndex
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
					//keywordsQuery = Query<CredentialIndex>.MatchPhrase( ts => ts.Field( f => f.Keyword ).Terms( tags ) );
					qc |= Query<T>.MatchPhrase( mp => mp.Field( f => f.Keyword ).Query( x ) );
					//keywordsQuery = Query<CredentialIndex>.Terms( ts => ts.Field( f => f.Keyword ).Terms( tags ) );
					//qc |= Query<CredentialIndex>.MatchPhrase( mp => mp.Field( f => f.Keyword ).Query( x ) );
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
		public static QueryContainer HandleSubjects<T>( MainSearchInput query ) where T : class, IIndex
		{
			QueryContainer results = null;
			if ( query.FiltersV2.Any( x => x.Name == "subjects" ) )
			{
				QueryContainer qc = null;
				var tags = new List<string>();
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "subjects" ) )
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
					results = Query<T>.Nested( n => n.Path( p => p.SubjectAreas ).Query( q => qc ).IgnoreUnmapped() );
				}

			}

			if ( query.FiltersV2.Any( x => x.Name == "wsubjects" ) )
			{
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

			}
			return results;
		}
		public static QueryContainer CommonOccupations<T>( MainSearchInput query ) where T : class, IIndex
		{
			QueryContainer occupationsQuery = null;
			if ( query.FiltersV2.Any( x => x.Name == "occupations" ) )
			{
				QueryContainer qc = null;
				var occupationNames = new List<string>();
				var occupationSchemas = new List<string>();
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "occupations" ) )
				{
					var codeItem = filter.HasAnyValue();
					if ( codeItem.AnyValue )
					{
						occupationSchemas.Add( codeItem.SchemaName );						
					}
					else
					{
						var text = filter.AsText();
						if ( string.IsNullOrWhiteSpace( text ) ) continue;
						occupationNames.Add( text );
					}
				}
				if ( occupationSchemas.Any() )
				{
					var ids = CodesManager.GetEntityStatisticBySchema( occupationSchemas );
					if ( ids.Any() )
					{
						occupationsQuery = Query<T>.Terms( ts => ts.Field( f => f.ReportFilters ).Terms<int>( ids.ToArray() ) );
					}
				}
				occupationNames.ForEach ( name => 
				{
					qc |= Query<T>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Occupations.First().CodeTitle, 70 ) ).Type( TextQueryType.PhrasePrefix ).Query( name ) );
					qc |= Query<T>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Occupations.First().Name, 70 ) ).Type( TextQueryType.BestFields ).Query( name ) );
				} );
				if ( qc != null )
					occupationsQuery = Query<T>.Nested( n => n.Path( p => p.Occupations ).Query( q => qc ).IgnoreUnmapped() );
			}

			if ( query.FiltersV2.Any( x => x.Name == "woccupations" ) )
			{
				QueryContainer qc = null;
				var occupationNames = new List<string>();
				var occupationSchemas = new List<string>();
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "woccupations" ) )
				{
					var codeItem = filter.HasAnyValue();
					if ( codeItem.AnyValue )
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
					var ids = CodesManager.GetEntityStatisticBySchema( occupationSchemas );
					if ( ids.Any() )
					{
						widgetOccupationsQuery = Query<T>.Terms( ts => ts.Field( f => f.ReportFilters ).Terms<int>( ids.ToArray() ) );
					}
				}
				occupationNames.ForEach( name =>
				{
					qc |= Query<T>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Occupations.First().CodeTitle, 70 ) ).Type( TextQueryType.PhrasePrefix ).Query( name ) );
					qc |= Query<T>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Occupations.First().Name, 70 ) ).Type( TextQueryType.BestFields ).Query( name ) );
				} );
				if ( qc != null )
					widgetOccupationsQuery = Query<T>.Nested( n => n.Path( p => p.Occupations ).Query( q => qc ).IgnoreUnmapped() );
			}
			return occupationsQuery;
		}
		public static QueryContainer CommonIndustries<T>( MainSearchInput query ) where T : class, IIndex
		{
			QueryContainer industriesQuery = null;
			if ( query.FiltersV2.Any( x => x.Name == "industries" ) )
			{
				QueryContainer qc = null;
				var industryNames = new List<string>();
				var industrySchemas = new List<string>();
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "industries" ) )
				{
					var codeItem = filter.HasAnyValue();
					if ( codeItem.AnyValue )
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
					var ids = CodesManager.GetEntityStatisticBySchema( industrySchemas );
					if ( ids.Any() )
					{
						industriesQuery = Query<T>.Terms( ts => ts.Field( f => f.ReportFilters ).Terms<int>( ids.ToArray() ) );
					}
				}
				industryNames.ForEach( name =>
				{
					qc |= Query<T>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Industries.First().CodeTitle, 70 ) ).Type( TextQueryType.PhrasePrefix ).Query( name ) );
					qc |= Query<T>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Industries.First().Name, 70 ) ).Type( TextQueryType.BestFields ).Query( name ) );
				} );

				if ( qc != null )
					industriesQuery = Query<T>.Nested( n => n.Path( p => p.Industries ).Query( q => qc ).IgnoreUnmapped() );
			}

			if ( query.FiltersV2.Any( x => x.Name == "windustries" ) )
			{
				QueryContainer qc = null;
				var industryNames = new List<string>();
				var industrySchemas = new List<string>();
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "windustries" ) )
				{
					var codeItem = filter.HasAnyValue();
					if ( codeItem.AnyValue )
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
					var ids = CodesManager.GetEntityStatisticBySchema( industrySchemas );
					if ( ids.Any() )
					{
						industriesQuery = Query<T>.Terms( ts => ts.Field( f => f.ReportFilters ).Terms<int>( ids.ToArray() ) );
					}
				}
				industryNames.ForEach( name =>
				{
					qc |= Query<T>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Industries.First().CodeTitle, 70 ) ).Type( TextQueryType.PhrasePrefix ).Query( name ) );
					qc |= Query<T>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Industries.First().Name, 70 ) ).Type( TextQueryType.BestFields ).Query( name ) );
				} );

				if ( qc != null )
					windustriesQuery = Query<T>.Nested( n => n.Path( p => p.Industries ).Query( q => qc ).IgnoreUnmapped() );
			}
			return industriesQuery;
		}
		public static QueryContainer CommonCip<T>( MainSearchInput query ) where T : class, IIndex
		{
			
			QueryContainer classificationsQuery = null;
			if ( query.FiltersV2.Any( x => x.Name == "instructionalprogramtypes" ) )
			{
				QueryContainer qc = null;
				var CIPNames = new List<string>();
				var CIPSchemas = new List<string>();
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "instructionalprogramtypes" ) )
				{
					var codeItem = filter.HasAnyValue();
					if ( codeItem.AnyValue )
					{
						CIPSchemas.Add( codeItem.SchemaName );
					}
					else
					{
						var text = filter.AsText();
						if ( string.IsNullOrWhiteSpace( text ) )
							continue;
						CIPNames.Add( text );
					}
				}
				if ( CIPSchemas.Any() )
				{
					var ids = CodesManager.GetEntityStatisticBySchema( CIPSchemas );
					if ( ids.Any() )
					{
						classificationsQuery = Query<T>.Terms( ts => ts.Field( f => f.ReportFilters ).Terms<int>( ids.ToArray() ) );
					}
				}
				CIPNames.ForEach( name =>
				{
					qc |= Query<T>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Classifications.First().CodeTitle, 70 ) ).Type( TextQueryType.PhrasePrefix ).Query( name ) );
					qc |= Query<T>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Classifications.First().Name, 70 ) ).Type( TextQueryType.BestFields ).Query( name ) );

				} );

				if ( qc != null )
					classificationsQuery = Query<T>.Nested( n => n.Path( p => p.Classifications ).Query( q => qc ).IgnoreUnmapped() );
			}
			return classificationsQuery;
		}
		#endregion

		/// <summary>
		/// Used primarily from a gray box search such as show where this qa org does this QA
		/// 2019-April - also used for widget filters
		/// </summary>
		/// <typeparam name="T">Current Elastic Index</typeparam>
		/// <param name="query"></param>
		/// <returns></returns>
		public static QueryContainer CommonQualityAssurance<T>( MainSearchInput query ) where T : class, IIndex
		{
			QueryContainer thisQuery = null;
			var assurances = new List<CodeItem>();
			if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CUSTOM ) )
			{
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "organizationroles" ).ToList() )
				{
					assurances.Add( filter.AsOrgRolesItem() );
				}

				if ( assurances.Any() )
					assurances.ForEach( x =>
					 {
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
			//if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.TEXT ) )
			//{				
			//	foreach ( var filter in query.FiltersV2.Where( m => m.Name == "qualityassurance" ).ToList() )
			//	{
			//		CodeItem ci = filter.AsQaText();
			//		if (ci != null && ci.Id > 0)
			//			assurances.Add( filter.AsQaText() );	//getting orgIds
			//	}

			//	if ( assurances.Any() )
			//	{
			//		assurances.ForEach( x =>
			//		{
			//			thisQuery |= Query<T>.Nested( n => n
			//				   .Path( p => p.AgentRelationshipsForEntity )
			//					.Query( q => q
			//						.Bool( mm => mm
			//							 .Must( mu => mu.Terms( m => m.Field( f => f.AgentRelationshipsForEntity.First().OrgId ).Terms( x.Id ) ) ) ) ) );
			//		} );
			//	}
			//}
			return thisQuery;
		}

		/// <summary>
		/// Used from the Quality Assurance section
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="query"></param>
		/// <param name="relationshipTypeIds"></param>
		/// <returns></returns>
		public static QueryContainer CommonQualityAssuranceFilter<T>( MainSearchInput query, List<int> relationshipTypeIds ) where T : class, IIndex
		{
			QueryContainer thisQuery = null;
			var assurances = new List<CodeItem>();

			if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.TEXT ) )
			{
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "qualityassurance" ).ToList() )
				{
					CodeItem ci = filter.AsQaText();
					if ( ci != null && ci.Id > 0 )
						assurances.Add( filter.AsQaText() );    //getting orgIds
				}

				if ( assurances.Any() )
				{
					if ( relationshipTypeIds == null || relationshipTypeIds.Count() == 0 )
					{
						//ensure only targets QA roles
						relationshipTypeIds.AddRange( new List<int>() { 1, 2, Entity_AgentRelationshipManager.ROLE_TYPE_RecognizedBy, Entity_AgentRelationshipManager.ROLE_TYPE_RegulatedBy } );
					}

					assurances.ForEach( x =>
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
				} else
				{
					//do we need a case where there was a TEXT filter, but no qualityassurance, and we have relationships?
				}
			} else
			{
				//if no TEXT filter, then just use the relationship codes
				thisQuery = Query<T>.Nested( n => n.Path( p => p.AgentRelationshipsForEntity )
								.Query( q =>
									q.Terms( t =>
										t.Field( f => f.AgentRelationshipsForEntity.First().RelationshipTypeIds )
											.Terms<int>( relationshipTypeIds.ToArray() ) ) )
										);
			}
			return thisQuery;
		}
		#region Common QualityAssurancePerformed
		public static QueryContainer CommonQualityAssurancePerformed<T>( MainSearchInput query ) where T : class, IIndex
		{
			QueryContainer qualityAssurancePerformedQuery = null;
			if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CUSTOM ) )
			{
				var assurances = new List<CodeItem>();
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "qualityassuranceperformed" ).ToList() )
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


		private static void LocationFilter<T>( MainSearchInput query, LocationQueryFilters locationQueryFilters ) where T : class, IIndex
		{
			foreach ( var filter in query.FiltersV2.Where( m => m.Type == MainSearchFilterV2Types.CODE ).ToList() )
			{
				if ( filter.Name != "locationset" )
				{
					continue;
				}

				var locationSet = filter.AsLocationSet();
				if ( locationSet.Regions.Count() > 0 )
				{
					if ( locationSet.Regions.Count() == 1 && locationSet.Cities.Count() > 0 && !string.IsNullOrEmpty( locationSet.Cities[ 0 ] ) )
					{
						string region = locationSet.Regions[ 0 ];
						//need to check cities with regions, maybe country, but latter may not exist in index
						locationSet.Cities.ForEach( x =>
						{
							if ( !string.IsNullOrEmpty( x ) )
							{
								locationQueryFilters.CityQuery |= Query<T>.Nested( n => n.Path( p => p.Addresses )
								.Query( q => q.Bool( mm => mm.Must( mu => mu
								.MultiMatch( m => m.Fields( mf => mf
								.Field( f => f.Addresses.First().City, 70 ) )
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
									locationQueryFilters.LocationQuery |= Query<T>.Term( t => t.Field( f => f.IsAvailableOnline ).Value( true ) ) ||
									Query<T>.Nested( n => n.Path( p => p.Addresses )
									.Query( q => q.Bool( mm => mm.Must( mu => mu
									.MultiMatch( m => m.Fields( mf => mf
									.Field( f => f.Addresses.First().AddressRegion, 20 ) )
									.Type( TextQueryType.PhrasePrefix )
									.Query( x ) ) ) ) ).IgnoreUnmapped() );
								}
								else
								{
									locationQueryFilters.LocationQuery |= Query<T>.Nested
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
								 .Query( q => q.Bool( mm => mm.Must( mu => mu.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Addresses.First().Country, 10 ) )
								 .Type( TextQueryType.PhrasePrefix )
								 .Query( x ) ) ) ) )
								 .IgnoreUnmapped() );
						 }
					 } );

				}

			}
		}

		public static List<CredentialSummary> Credential_Search( MainSearchInput query, ref int pTotalRows )
		{
			List<CredentialSummary> list = new List<CredentialSummary>();
			Credential_BuildIndex();


			QueryContainer credentialTypeQuery = null;
			QueryContainer credentialIdQuery = null;
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
			QueryContainer qualityAssuranceSearchQuery = null;
			QueryContainer qaFilterQuery = null;

			QueryContainer languagesQuery = null;
			QueryContainer reportsQuery = null;
			QueryContainer classificationsQuery = null;
			QueryContainer asmntDeliveryTypesQuery = null;
			QueryContainer learningDeliveryTypesQuery = null;
			
			LocationQueryFilters locationQueryFilters = new LocationQueryFilters();
			var relationshipTypeIds = new List<int>();
			try
			{

			
			#region credSearchCategories
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
				var credentialStatusTypeIds = new List<int>();
				var audienceLevelTypeIds = new List<int>();
				var audienceTypeIds = new List<int>();
				
				var validConnections = new List<string>();
				var connectionFilters = new List<string>();
				var asmntdeliveryTypeIds = new List<int>();
				var learningdeliveryTypeIds = new List<int>();
				var reportIds = new List<int>();


				if ( query.FiltersV2.Any( x => x.AsCodeItem().CategoryId == CodesManager.PROPERTY_CATEGORY_CONNECTION_PROFILE_TYPE ) )
				{
					LoggingHelper.DoTrace( 6, "Credential search, filters exist for connections" );
					//this will include is part/has part
					//Enumeration entity = CodesManager.GetCredentialsConditionProfileTypes();
					Enumeration entity = CodesManager.GetConnectionTypes( 1, false );
					validConnections = entity.Items.Select( s => s.SchemaName.ToLower() ).ToList();
				}

				foreach ( var filter in query.FiltersV2.Where( m => m.Type == MainSearchFilterV2Types.CODE ).ToList() )
				{
					var item = filter.AsCodeItem();
					if ( filter.Name == "reports" || filter.Name == "otherfilters" )
					{
						reportIds.Add( item.Id ); //can probably continue after here?
					}
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
						relationshipTypeIds.Add( item.Id );

					else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_CONNECTION_PROFILE_TYPE )
					{
						if ( validConnections.Contains( item.SchemaName.ToLower() ) )
							connectionFilters.Add( item.SchemaName.Replace( "ceterms:", "" ) );
					}
				}

				if ( credentialTypeIds.Any() )
					credentialTypeQuery = Query<CredentialIndex>.Terms( ts => ts.Field( f => f.CredentialTypeId ).Terms( credentialTypeIds.ToArray() ) );
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
						LoggingHelper.DoTrace( 6, "Credential search, checking for connections. x=" + x );
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

					LoggingHelper.DoTrace( 6, "Credential search, AFTER checking for connections" );
				}
			}
			#endregion


			#region Handle Location queries
			LocationFilter<CredentialIndex>( query, locationQueryFilters );

			#endregion

			#region QualityAssurance, with owned and offered by
			//NOTE: this is only referenced after clicking on a gray box, not from the search page
			//		==> actually now used by search widget => type = organizationroles
			qualityAssuranceSearchQuery = CommonQualityAssurance<CredentialIndex>( query );

			//USED BY QUALITY ASSURANCE FILTER check boxes and org list
			qaFilterQuery = CommonQualityAssuranceFilter<CredentialIndex>( query, relationshipTypeIds );
			#endregion

			#region Credential Ids list
			if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CUSTOM ) )
			{
				var credIds = new List<int>();
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "potentialresults" ).ToList() )
				{
					credIds.AddRange( JsonConvert.DeserializeObject<List<int>>( filter.CustomJSON ) );
				}

				if ( credIds.Any() )
				{
					credIds.ForEach( x =>
					{
						credentialIdQuery |= Query<CredentialIndex>.Terms( ts => ts.Field( f => f.Id ).Terms( credIds ) );
						//.Nested( n => n.Path( p => p.Id ).Query( q => q.Bool( mm => mm.Must( mu => mu.Match( m => m.Field( f => f.Id ) ) ) ) ) );
					} );
				}
			}
			#endregion

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
						competenciesQuery = Query<CredentialIndex>.Terms( ts => ts.Field( f => f.ReportFilters ).Terms<int>( ids.ToArray() ) );
					}
				}
				competencies.ForEach( x =>
					{
						//Should eventually change once the Competencies have proper inputs.
						competenciesQuery |= Query<CredentialIndex>.Nested( n => n.Path( p => p.Competencies )
						.Query( q => q.Bool( mm => mm.Must( mu =>
						mu.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Competencies.First().Name, 70 ) ).Type( TextQueryType.PhrasePrefix ).Query( x ) ) ||
						mu.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Competencies.First().Name, 70 ) ).Type( TextQueryType.BestFields ).Query( x ) ) ) ) ).IgnoreUnmapped() );
					} );
			}
			#endregion

			#region Occupations, Industries, programs

			occupationsQuery = CommonOccupations<CredentialIndex>( query );

			industriesQuery = CommonIndustries<CredentialIndex>( query );

			classificationsQuery = CommonCip<CredentialIndex>( query );

			#endregion

			#region Subjects

			if ( query.FiltersV2.Any( x => x.Name == "subjects" ) )
			{
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
				keywordsQuery = HandleKeywords<CredentialIndex>( query );
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
					languagesQuery = Query<CredentialIndex>.Terms( ts => ts.Field( f => f.InLanguage ).Terms( languageFilters ) );
					//qc |= Query<CredentialIndex>.Terms( ts => ts.Field( f => f.InLanguage ).Terms( languages ) );
				} );
				//languagesQuery = Query<CredentialIndex>.Nested( n => n.Path( p => p.InLanguage ).Query( q => qc ).IgnoreUnmapped() );
			}
			#endregion

			#region Boundaries

			var boundaries = SearchServices.GetBoundaries( query, "bounds" );
			if ( boundaries.IsDefined )
			{
				boundariesQuery = Query<CredentialIndex>
					  .Nested( n => n.Path( p => p.Addresses )
					  .Query( q => Query<CredentialIndex>.Range( r => r.Field( f => f.Addresses.First().Longitude ).LessThan( ( double ) boundaries.East ).GreaterThan( ( double ) boundaries.West ) ) && Query<CredentialIndex>.Range( r => r.Field( f => f.Addresses.First().Latitude ).LessThan( ( double ) boundaries.North ).GreaterThan( ( double ) boundaries.South ) ) ).IgnoreUnmapped() );
			}

			#endregion

			#region Query

			//var tag = string.Format( "*{0}*", query.Keywords.ToLower() );
			//var search = EC.Search<CredentialIndex>( i => i.Index( CredentialIndex ).Query( q => q.Wildcard( w => w.Field( f => f.Name ).Value( query ) ) || q.Wildcard( w => w.Field( f => f.OoName ).Value( query ) ) ) );
			var sort = new SortDescriptor<CredentialIndex>();

			var sortOrder = query.SortOrder;
			if ( sortOrder == "alpha" )
				sort.Ascending( s => s.Name.Suffix( "keyword" ) );
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
			//if ( query.Keywords.LastIndexOf( "[" ) > 0 )
			//	query.Keywords = query.Keywords.Substring( 0, query.Keywords.LastIndexOf( "[" ) );

			var search = EC.Search<CredentialIndex>( body => body
					 .Index( CredentialIndex )
					 .Query( q =>
						//q.Term( t => t.Field( f => f.EntityStateId ).Value( 3 ) )
						credentialTypeQuery
						&& credentialIdQuery
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
						&& qaFilterQuery
						&& qualityAssuranceSearchQuery
						&& languagesQuery
						&& locationQueryFilters.LocationQuery
						&& locationQueryFilters.CountryQuery
						&& locationQueryFilters.CityQuery
						&& reportsQuery
						&& (
						q.MatchPhrasePrefix( mp => mp
						.Field( f => f.Name )
						.Query( query.Keywords ) )
						||
						q.MultiMatch( m => m
							 .Fields( f => f
								 .Field( ff => ff.Name, 90 )
								 .Field( ff => ff.ListTitle, 90 )
								 .Field( ff => ff.OwnerOrganizationName, 90 )
								 .Field( ff => ff.Description, 45 )
								 .Field( ff => ff.SubjectWebpage, 40 )
								 .Field( ff => ff.AlternateNames, 35 )
								 .Field( ff => ff.TextValues, 50 )
							 //.Field( ff => ff.Subject, 50 )
							  .Field( ff => ff.Keyword, 60 )  //note Keyword not populated!!! 19-04-26 mp => actually is being populated, 
							 //.Field( ff => ff.CodedNotation )
							 //.Field( ff => ff.Competencies ) //test how long it takes 
							 )
							 //.Slop(2)
							 //.Operator( Operator.And )
							 .Type( TextQueryType.PhrasePrefix )
							 .Query( query.Keywords )
						//.MaxExpansions( 10 )
						//.Analyzer( "standard" )
						)
						|| q.MultiMatch( m => m
							.Fields( f => f
								.Field( ff => ff.Name, 90 )
								.Field( ff => ff.ListTitle, 90 )
								 .Field( ff => ff.Description, 45 )
								 .Field( ff => ff.SubjectWebpage, 40 )
								 .Field( ff => ff.OwnerOrganizationName, 90 )
								 .Field( ff => ff.AlternateNames, 35 )
								 .Field( ff => ff.TextValues, 50 )
								 .Field( ff => ff.Keyword, 60 )
							 //.Field( ff => ff.Subject, 50 )

							 )
							.Type( TextQueryType.BestFields )
							.Query( query.Keywords )
							)
						)
					 )
					 .Sort( s => sort )
					 //.From( query.StartPage - 1 )
					 .From( query.StartPage > 0 ? query.StartPage - 1 : 1 )
					 .Skip( ( query.StartPage - 1 ) * query.PageSize )
					 .Size( query.PageSize ) );

			#endregion
			var debug = search.DebugInformation;
			pTotalRows = ( int ) search.Total;
			if ( pTotalRows > 0 )
			{
				//map results
				list = ElasticManager.Credential_MapFromElastic( ( List<CredentialIndex> ) search.Documents );

				LoggingHelper.DoTrace( 6, string.Format( "ElasticServices.CredentialSearch. found: {0} records", pTotalRows ) );
			} else
			{

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
			{ ActivityType = "Credential", Activity = "Search", Event = searchType, Comment = jsoninput }
			);
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

		//test - keep for a while
		public static List<CredentialSummary> Search( MainSearchInput query )
		{
			bool valid = true;
			string status = "";
			List<CredentialSummary> list = new List<CredentialSummary>();

			var search = EC.Search<CredentialIndex>( i => i.Index( CredentialIndex ).Query( q =>
				q.MultiMatch( m => m
					.Fields( f => f
						.Field( ff => ff.Name )
						.Field( ff => ff.OwnerOrganizationName )
					)
					.Type( TextQueryType.PhrasePrefix )
					.Query( query.Keywords )
				)
				||
				q.MultiMatch( m => m
					.Fields( f => f
						.Field( ff => ff.Name )
						.Field( ff => ff.OwnerOrganizationName )
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
				if ( deleteIndexFirst && EC.IndexExists( OrganizationIndex ).Exists )
					EC.DeleteIndex( OrganizationIndex );

				if ( !EC.IndexExists( OrganizationIndex ).Exists )
				{
					OrganizationInitializeIndex();
					indexInitialized = true;
				}

				if ( indexInitialized || updatingIndexRegardless )
				{
					new ActivityServices().AddActivity( new SiteActivity()
					{ ActivityType = "Organization", Activity = "Elastic", Event = "Build Index" }
					);

					string filter = "( base.EntityStateId >= 2 )";
					var list = ElasticManager.Organization_SearchForElastic( filter );

					var results = EC.Bulk( b => b.IndexMany( list, ( d, organization ) => d.Index( OrganizationIndex ).Document( organization ).Id( organization.Id.ToString() ) ) );
					//need to first identify a error phrase or will get all error entries displayed
					if ( results.ToString().IndexOf( "Valid NEST response built from a successful low level cal" ) == -1 )
					{
						Console.WriteLine( results.ToString() );
						LoggingHelper.DoTrace( 1, " Issue building organization index: " + results );
					}
					//??????
					EC.Refresh( OrganizationIndex );
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "Organization_BuildIndex" );
			}
		}



		public static void OrganizationInitializeIndex()
		{
			if ( !EC.IndexExists( OrganizationIndex ).Exists )
			{
				EC.CreateIndex( OrganizationIndex, c => new CreateIndexDescriptor( OrganizationIndex )
				.Settings( st => st
						 .Analysis( an => an.TokenFilters( tf => tf.Stemmer( "custom_english_stemmer", ts => ts.Language( "english" ) ) ).Analyzers( anz => anz.Custom( "custom_lowercase_stemmed", cc => cc.Tokenizer( "standard" ).Filters( new List<string> { "lowercase", "custom_english_stemmer" } ) ) ) ) )
					.Mappings( ms => ms
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
								 .Nested<Models.Elastic.IndexQualityAssurance>( n => n
									.Name( nn => nn.QualityAssurance )
									.AutoMap()
								)
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
							)
						)

					)
				);
			}

			//EC.CreateIndex( OrganizationIndex, c => c.Mappings( m => m.Map<OrganizationIndex>( d => d.AutoMap() ) ) );
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
				LoggingHelper.LogError( ex, "Organization_UpdateIndex", false );
			}
		} //


		/// <summary>
		/// Pass a filter to use for updating the index
		/// </summary>
		/// <param name="filter"></param>
		public static void Organization_UpdateIndex( string filter, ref int processed )
		{
			if ( string.IsNullOrWhiteSpace( filter ) )
				return;
			int action = UtilityManager.GetAppKeyValue( "updateCredIndexAction", 0 );
			if ( action == 0 )
				return;
			try
			{
				var list = ElasticManager.Organization_SearchForElastic( filter );
				if ( list != null && list.Count > 0 )
				{
					processed = list.Count;
					if ( action == 1 )
					{
						foreach ( OrganizationIndex item in list )
						{
							var res = EC.Index( item, idx => idx.Index( OrganizationIndex ) );
							Console.WriteLine( res.Result );
						}

					}
					else if ( action == 2 )
					{
						EC.Bulk( b => b.IndexMany( list, ( d, organization ) => d.Index( OrganizationIndex ).Document( organization ).Id( organization.Id.ToString() ) ) );
					}
				}
				else
				{
					LoggingHelper.DoTrace( 2, string.Format( "Organization_UpdateIndex failed, no data returned for filter: {0}", filter ) );
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "Organization_UpdateIndex failed for filter: {0}", filter ), false );
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
			var sort = new SortDescriptor<OrganizationIndex>();

            var sortOrder = query.SortOrder;
            if ( sortOrder == "alpha" )
                sort.Ascending( s => s.Name.Suffix( "keyword" ) );
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
                 (q.MultiMatch( m => m
                               .Fields( f => f
                               .Field( ff => ff.Name )
                               .Field( ff => ff.Description )
                               .Field( ff => ff.SubjectWebpage )
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
                 .Sort( s => sort )
                     //.From( query.StartPage - 1 )
                     .From( query.StartPage > 0 ? query.StartPage - 1 : 1 )
                     .Skip( ( query.StartPage - 1 ) * query.PageSize )
					 .Size( query.PageSize ) );

			pTotalRows = ( int ) search.Total;

			if ( pTotalRows > 0 )
			{
				//map results
				list = ElasticManager.Organization_MapFromElastic( ( List<OrganizationIndex> ) search.Documents );

				LoggingHelper.DoTrace( 6, string.Format( "ElasticServices.OrganizationSearch. found: {0} records", pTotalRows ) );
			}
			//stats
			query.Results = pTotalRows;
			return list;
		}
		public static List<Object> OrganizationAutoComplete( string keyword, int maxTerms, ref int pTotalRows )
		{
			QueryContainer organizationEntityStateQuery = null;
			organizationEntityStateQuery = Query<OrganizationIndex>.Terms( ts => ts.Field( f => f.EntityStateId ).Terms<int>( 3 ) );

			var search = EC.Search<OrganizationIndex>( i => i
							.Index( OrganizationIndex )
							.Query( q =>
								organizationEntityStateQuery
								&&  q.MultiMatch( m => m
									.Fields( f => f
									   .Field( ff => ff.Name )
									   .Field( ff => ff.Description )
									   .Field( ff => ff.SubjectWebpage )
									   )
								   //.Operator( Operator.Or )
								   .Type( TextQueryType.PhrasePrefix )
								   .Query( keyword )
								   .MaxExpansions( 10 )
									)
							)
						   .Size( maxTerms )
						   );

			//Need to be look for other approaches            

			pTotalRows = ( int ) search.Total;
			var list = ( List<OrganizationIndex> ) search.Documents;
			return list.Select( x => x.Name ).Distinct().ToList().Cast<object>().ToList();
		}

		public static List<object> OrganizationQAAutoComplete( string keyword, int entityTypeId )
		{
			int maxTerms = 20;
			QueryContainer HasQaQuery = null;
			if ( entityTypeId  == 2)
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
						   .Size( maxTerms )
						   );
			var debug = search.DebugInformation;
			//Need to be look for other approaches            
			var list = ( List<OrganizationIndex> ) search.Documents;			
			return list.Select( x => new { label = x.Name, value = x.Id } ).Distinct().ToList().Cast<object>().ToList();
		}

		public static List<OrganizationSummary> OrganizationSearch( MainSearchInput query, ref int pTotalRows )
		{
			Organization_BuildIndex();

			List<OrganizationSummary> list = new List<OrganizationSummary>();

			QueryContainer organizationEntityStateQuery = null;
			QueryContainer organizationTypeQuery = null;
			QueryContainer HasQaQuery = null;
			QueryContainer OrgIdQuery = null;
			QueryContainer organizationServiceQuery = null;
			QueryContainer sectorTypeQuery = null;
			QueryContainer claimTypeQuery = null;
			//QueryContainer relationshipIdQuery = null;
			QueryContainer qualityAssuranceSearchQuery = null;
			QueryContainer qaFilterQuery = null;
			QueryContainer industriesQuery = null;
			QueryContainer boundariesQuery = null;
			QueryContainer keywordsQuery = null;

			QueryContainer qualityAssurancePerformedTypesQuery = null;
			QueryContainer qualityAssurancePerformedAgentQuery = null;
			QueryContainer reportsQuery = null;
			WidgetQueryFilters widgetQuery = new WidgetQueryFilters();
			LocationQueryFilters locationQueryFilters = new LocationQueryFilters();
			var relationshipTypeIds = new List<int>();

			if ( !query.IncludingReferenceObjects )
			{
				//organizationEntityStateQuery = Query<OrganizationIndex>.Terms( ts => ts.Field( f => f.EntityStateId ).Terms<int>( 3 ) );
			}

			#region orgSearchCategories
			if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CODE ) )
			{
				string searchCategories = UtilityManager.GetAppKeyValue( "orgSearchCategories", "7,8,9,30," );
				var orgSearchCategories = new List<int>();
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
					if ( filter.Name == "reports" || filter.Name == "otherfilters" )
						reportIds.Add( item.Id );
					//Filters - OrganizationTypes, ServiceTypes
					if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_ORGANIZATION_TYPE )
						organizationTypeIds.Add( item.Id );
					if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_ORG_SERVICE )
						organizationServiceIds.Add( item.Id );
					//Filters - Sector Types 
					if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_ORGANIZATION_SECTORTYPE )
						sectorTypeIds.Add( item.Id );
					//Filters - Claim Types 
					if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_CLAIM_TYPE )
						claimTypeIds.Add( item.Id );


					//Filters - Quality Assurance Performed
					if ( filter.Name == "qaperformed" )
						qualityAssurancePerformedIds.Add( item.Id );
					//Filters - Quality Assurance
					else if ( item.CategoryId == 13 )
						relationshipTypeIds.Add( item.Id );
				}

				if ( reportIds.Any() )
				{
					reportsQuery = Query<OrganizationIndex>.Terms( ts => ts.Field( f => f.ReportFilters ).Terms<int>( reportIds.ToArray() ) );
				}

				if ( organizationTypeIds.Any() )
					organizationTypeQuery = Query<OrganizationIndex>.Terms( ts => ts.Field( f => f.OrganizationTypeIds ).Terms( organizationTypeIds.ToArray() ) );

				if ( organizationServiceIds.Any() )
					organizationServiceQuery = Query<OrganizationIndex>.Terms( ts => ts.Field( f => f.OrganizationServiceTypeIds ).Terms( organizationServiceIds.ToArray() ) );

				if ( sectorTypeIds.Any() )
					sectorTypeQuery = Query<OrganizationIndex>.Terms( ts => ts.Field( f => f.OrganizationSectorTypeIds ).Terms<int>( sectorTypeIds ) );

				if ( claimTypeIds.Any() )
					claimTypeQuery = Query<OrganizationIndex>.Terms( ts => ts.Field( f => f.OrganizationClaimTypeIds ).Terms<int>( claimTypeIds ) );

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
				keywordsQuery = HandleKeywords<OrganizationIndex>( query );
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
			LocationFilter<OrganizationIndex>( query, locationQueryFilters );

			#endregion

			#region Industries
			industriesQuery = CommonIndustries<OrganizationIndex>( query );
			#endregion

			#region QualityAssurance
			//NOTE: this is only referenced after clicking on a gray box, not from the search page
			//		==> actually now used by search widget => CONFIRM THIS
			qualityAssuranceSearchQuery = CommonQualityAssurance<OrganizationIndex>( query );

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
						OrgIdQuery |= Query<OrganizationIndex>.Terms( ts => ts.Field( f => f.Id ).Terms( orgIds ) );
					} );
			}
			#endregion

			#region Boundaries

			var boundaries = SearchServices.GetBoundaries( query, "bounds" );
			if ( boundaries.IsDefined )
			{
				boundariesQuery = Query<OrganizationIndex>
					.Nested( n => n.Path( p => p.Addresses )
					.Query( q => Query<OrganizationIndex>.Range( r => r.Field( f => f.Addresses.First().Longitude ).LessThan( ( double ) boundaries.East ).GreaterThan( ( double ) boundaries.West ) ) && Query<OrganizationIndex>.Range( r => r.Field( f => f.Addresses.First().Latitude ).LessThan( ( double ) boundaries.North ).GreaterThan( ( double ) boundaries.South ) ) ).IgnoreUnmapped() );
			}
			#endregion

			#region Query

			var sort = new SortDescriptor<OrganizationIndex>();

            var sortOrder = query.SortOrder;
            if ( sortOrder == "alpha" )
                sort.Ascending( s => s.Name.Suffix( "keyword" ) );
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
					  //&& relationshipIdQuery
					  && industriesQuery
					  && boundariesQuery
					  && qaFilterQuery && qualityAssuranceSearchQuery
					  && qualityAssurancePerformedAgentQuery
					  && qualityAssurancePerformedTypesQuery
					  && reportsQuery
					  && locationQueryFilters.LocationQuery
					  && locationQueryFilters.CountryQuery
					  && locationQueryFilters.CityQuery
					  && widgetQuery.KeywordQuery
					  && claimTypeQuery
					  && ( q.MultiMatch( m => m
						   .Fields( f => f
							   .Field( ff => ff.Name, 100 )
							   .Field( ff => ff.Description, 55 )
							   .Field( ff => ff.SubjectWebpage, 25 )
							   .Field( ff => ff.AlternateNames, 35 )
							   .Field( ff => ff.TextValues, 45 )
							   .Field( ff => ff.Keyword, 50 )
						   )
						   // .Operator( Operator.Or )
						   .Type( TextQueryType.PhrasePrefix )
						   .Query( query.Keywords )
					  // .MaxExpansions( 10 )
					  )
					   || q.MultiMatch( m => m
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
							)
						)
					 )
				   .Sort( s => sort )
				   .From( query.StartPage > 0 ? query.StartPage - 1 : 1 )
				   .Skip( ( query.StartPage - 1 ) * query.PageSize )
				   .Size( query.PageSize )
				   );


			#endregion

			var debug = search.DebugInformation;
			pTotalRows = ( int ) search.Total;
			if ( pTotalRows > 0 )
			{
				list = ElasticManager.Organization_MapFromElastic( ( List<OrganizationIndex> ) search.Documents );
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
			List<AssessmentIndex> list = new List<Models.Elastic.AssessmentIndex>();
			bool indexInitialized = false;
			if ( deleteIndexFirst && EC.IndexExists( AssessmentIndex ).Exists )
			{
				EC.DeleteIndex( AssessmentIndex );
			}
			if ( !EC.IndexExists( AssessmentIndex ).Exists )
			{
				AssessmentInitializeIndex();
				indexInitialized = true;
			}

			if ( indexInitialized || updatingIndexRegardless )
			{
				int minEntityStateId = UtilityManager.GetAppKeyValue( "minAsmtEntityStateId", 3 );
				try
				{
					new ActivityServices().AddActivity( new SiteActivity()
					{ ActivityType = "AssessmentProfile", Activity = "Elastic", Event = "Build Index" }
					);

					list = ElasticManager.Assessment_SearchForElastic( string.Format("( base.EntityStateId >= {0} )", minEntityStateId) );
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, "Assessment_BuildIndex" );
				}
				finally
				{
					if ( list != null && list.Count > 10 )
					{
						var results = EC.Bulk( b => b.IndexMany( list, ( d, document ) => d.Index( AssessmentIndex ).Document( document ).Id( document.Id.ToString() ) ) );
						if ( results.ToString().IndexOf( "Valid NEST response built from a successful low level cal" ) == -1 )
						{
							Console.WriteLine( results.ToString() );
							LoggingHelper.DoTrace( 1, " Issue building assessment index: " + results );
						}

						EC.Refresh( AssessmentIndex );
					}
				}
			}

		}
		#endregion

		public static void AssessmentInitializeIndex( bool deleteIndexFirst = true )
		{
			if ( !EC.IndexExists( AssessmentIndex ).Exists )
			{
				var tChars = new List<string> { "letter", "digit", "punctuation", "symbol" };

				EC.CreateIndex( AssessmentIndex, c => new CreateIndexDescriptor( AssessmentIndex )
				 //.Settings( s => s.Analysis( a => a.TokenFilters( t => t.Stop( "my_stop", st => st.RemoveTrailing() ).Snowball( "my_snowball", st => st.Language( SnowballLanguage.English ) ) ).Analyzers( aa => aa.Custom( "my_analyzer", sa => sa.Tokenizer( "standard" ).Filters( "lowercase", "my_stop", "my_snowball" ) ) ) ) )
				 //.Settings( s => s.Analysis( a => a.Analyzers( aa => aa.Standard( "snowball", sa => sa.StopWords( "_english_" ) ) ) ) )
				 // .Settings( s => s.Analysis( a => a.Tokenizers (aa => aa.EdgeNGram( "my_ngram_tokenizer" ,   )

				 .Settings( st => st
						 .Analysis( an => an.TokenFilters( tf => tf.Stemmer( "custom_english_stemmer", ts => ts.Language( "english" ) ) ).Analyzers( anz => anz.Custom( "custom_lowercase_stemmed", cc => cc.Tokenizer( "standard" ).Filters( new List<string> { "lowercase", "custom_english_stemmer" } ) ) ) ) )
					.Mappings( ms => ms
						.Map<AssessmentIndex>( m => m
							.AutoMap()
							.Properties( p => p
								.Text( s => s.Name( n => n.Description ).Analyzer( "custom_lowercase_stemmed" ) )
								.Nested<IndexCompetency>( n => n
									.Name( nn => nn.AssessesCompetencies )
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
									.Name( nn => nn.Classifications )
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
							)
						)
					)
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
				LoggingHelper.LogError( ex, "Assessment_UpdateIndex", false );
			}
		} //


		/// <summary>
		/// Pass a filter to use for updating the index
		/// </summary>
		/// <param name="filter"></param>
		public static void Assessment_UpdateIndex( string filter, ref int processed )
		{
			if ( string.IsNullOrWhiteSpace( filter ) )
				return;
			int action = UtilityManager.GetAppKeyValue( "updateCredIndexAction", 0 );
			if ( action == 0 )
				return;
			try
			{

				var list = ElasticManager.Assessment_SearchForElastic( filter );
				if ( list != null && list.Count > 0 )
				{
					processed = list.Count;
					if ( action == 1 )
					{
						foreach ( AssessmentIndex item in list )
						{
							var res = EC.Index( item, idx => idx.Index( AssessmentIndex ) );
							Console.WriteLine( res.Result );
						}

					}
					else if ( action == 2 )
					{
						EC.Bulk( b => b.IndexMany( list, ( d, entity ) => d.Index( AssessmentIndex ).Document( entity ).Id( entity.Id.ToString() ) ) );
					}
				}
				else
				{
					//can be empty when called after import
					LoggingHelper.DoTrace( 2, string.Format( "Assessment_UpdateIndex. no data returned for filter: {0}", filter ) );
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "Assessment_UpdateIndex failed for filter: {0}", filter ), false );
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
			var sort = new SortDescriptor<AssessmentIndex>();

			var sortOrder = query.SortOrder;
			if ( sortOrder == "alpha" )
				sort.Ascending( s => s.Name.Suffix( "keyword" ) );
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
								  .Field( ff => ff.Name )
								  .Field( ff => ff.OwnerOrganizationName )
				 )
				 .Type( TextQueryType.PhrasePrefix )
				 .Query( query.Keywords )
				 .MaxExpansions( 10 ) ) )
				 .Sort( s => sort )
					 //.From( query.StartPage - 1 )
					 .From( query.StartPage > 0 ? query.StartPage - 1 : 1 )
					 .Skip( ( query.StartPage - 1 ) * query.PageSize )
					 .Size( query.PageSize ) );

			pTotalRows = ( int ) search.Total;

			if ( pTotalRows > 0 )
			{
				//map results
				list = ElasticManager.Assessment_MapFromElastic( ( List<AssessmentIndex> ) search.Documents );

				LoggingHelper.DoTrace( 6, string.Format( "ElasticServices.AssessmentSearch. found: {0} records", pTotalRows ) );
			}
			//stats
			query.Results = pTotalRows;
			return list;
		}
		public static List<object> AssessmentAutoComplete( string keyword, int maxTerms, ref int pTotalRows )
		{

			var search = EC.Search<AssessmentIndex>( i => i.Index( AssessmentIndex ).Query( q => q.MultiMatch( m => m
						   .Fields( f => f
							   .Field( ff => ff.Name )
							   .Field( ff => ff.Description )
						   )
						   //.Operator( Operator.Or )
						   .Type( TextQueryType.PhrasePrefix )
						   .Query( keyword )
						   .MaxExpansions( 10 ) ) ).Size( maxTerms ) );

			pTotalRows = ( int ) search.Total;
			var list = ( List<AssessmentIndex> ) search.Documents;
			return list.Select( x => x.ListTitle ).Distinct().ToList().Cast<object>().ToList();
		}

		public static List<AssessmentProfile> AssessmentSearch( MainSearchInput query, ref int pTotalRows )
		{
			Assessment_BuildIndex();

			var list = new List<PM.AssessmentProfile>();

			QueryContainer competenciesQuery = null;
			QueryContainer subjectsQuery = null;
			QueryContainer keywordsQuery = null;
			QueryContainer connectionsQuery = null;
			QueryContainer audienceTypeQuery = null;
			QueryContainer audienceLevelTypeQuery = null;
			QueryContainer asmtMethodTypesQuery = null;
			QueryContainer asmtUseTypesQuery = null;
			QueryContainer deliveryTypesQuery = null;
			QueryContainer scoringMethodsQuery = null;
			QueryContainer occupationsQuery = null;
			QueryContainer industriesQuery = null;
			QueryContainer classificationsQuery = null;
			//QueryContainer relationshipIdQuery = null;
			QueryContainer qualityAssuranceSearchQuery = null;
			QueryContainer qaFilterQuery = null;
			QueryContainer languagesQuery = null;
			QueryContainer boundariesQuery = null;
			QueryContainer reportsQuery = null;
			//WidgetQueryFilters widgetQuery = new WidgetQueryFilters();
			LocationQueryFilters locationQueryFilters = new LocationQueryFilters();
			var relationshipTypeIds = new List<int>();

			#region Handle Location queries
			LocationFilter<AssessmentIndex>( query, locationQueryFilters );

			#endregion

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
							competenciesQuery = Query<AssessmentIndex>.Terms( ts => ts.Field( f => f.ReportFilters ).Terms<int>( ids.ToArray() ) );
						}
					}

				if ( competencies.Any() )
				{
					competencies.ForEach( x =>
					{
						//Temporary fix
						competenciesQuery |= Query<AssessmentIndex>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.AssessesCompetencies.First().Name, 70 ) ).Type( TextQueryType.PhrasePrefix ).Query( x ) );
						competenciesQuery |= Query<AssessmentIndex>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.AssessesCompetencies.First().Name, 70 ) ).Type( TextQueryType.BestFields ).Query( x ) );
					} );
					if ( competenciesQuery != null )
						competenciesQuery = Query<AssessmentIndex>.Nested( n => n.Path( p => p.AssessesCompetencies ).Query( q => competenciesQuery ).IgnoreUnmapped() );
				}
			}

			#endregion

			#region Subject Areas.keywords
			//from widget and from search filters
			if ( query.FiltersV2.Any( x => x.Name == "subjects" ) )
			{
				subjectsQuery = HandleSubjects<AssessmentIndex>( query );
				//var subjects = new List<string>();
				//foreach ( var filter in query.FiltersV2.Where( m => m.Name == "subjects" ) )
				//{
				//	var text = ServiceHelper.HandleApostrophes( filter.AsText() );
				//	if ( string.IsNullOrWhiteSpace( text ) )
				//		continue;
				//	subjects.Add( text.ToLower() );
				//}

				////QueryContainer qc = null;
				////need to be able to add a weight here!
				//subjects.ForEach( x =>
				//{
				//	subjectsQuery |= Query<AssessmentIndex>.Match( m => m.Field( f => f.SubjectAreas ).Query( x ).Boost(60) );
				//} );

			}
			//keywords from widget
			if ( query.FiltersV2.Any( x => x.Name == "keywords" ) )
			{
				keywordsQuery = HandleKeywords<AssessmentIndex>( query );
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
					if ( filter.Name == "reports" || filter.Name == "otherfilters" )
						reportIds.Add( item.Id );

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
						relationshipTypeIds.Add( item.Id );
					else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL )
						audienceLevelTypeIds.Add( item.Id );
					else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE )
						audienceTypeIds.Add( item.Id );
					else if ( item.CategoryId == 15 )
					{
						if ( validConnections.Contains( item.SchemaName.ToLower() ) )
							connectionFilters.Add( item.SchemaName.Replace( "ceterms:", "" ) );
					}
				}

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
				//if ( relationshipTypeIds.Any() )
				//	relationshipIdQuery = Query<AssessmentIndex>.Nested( n => n.Path( p => p.AgentRelationshipsForEntity ).Query( q => q.Terms( t => t.Field( f => f.AgentRelationshipsForEntity.First().RelationshipTypeIds ).Terms<int>( relationshipTypeIds.ToArray() ) ) ) );
				if ( relationshipTypeIds.Any() )
				{
					//relationshipIdQuery = Query<OrganizationIndex>.Nested( n => n.Path( p => p.AgentRelationshipsForEntity ).Query( q => q.Terms( t => t.Field( f => f.AgentRelationshipsForEntity.First().RelationshipTypeIds ).Terms<int>( relationshipTypeIds.ToArray() ) ) ) );
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
			qualityAssuranceSearchQuery = CommonQualityAssurance<AssessmentIndex>( query );

			//USED BY QUALITY ASSURANCE FILTER check boxes and org list
			qaFilterQuery = CommonQualityAssuranceFilter<AssessmentIndex>( query, relationshipTypeIds );
			#endregion

			#region Assessment Ids list
			QueryContainer asmtIdListQuery = null;
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
						asmtIdListQuery |= Query<AssessmentIndex>.Terms( ts => ts.Field( f => f.Id ).Terms( idsList ) );
					} );
				}
			}
			#endregion
			#region Occupations, industries, programs
			occupationsQuery = CommonOccupations<AssessmentIndex>( query );

			industriesQuery = CommonIndustries<AssessmentIndex>( query );

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
					languagesQuery = Query<AssessmentIndex>.Terms( ts => ts.Field( f => f.InLanguage ).Terms( languageFilters ) );
				} );
			}
			#endregion

			#region Boundaries
			var boundaries = SearchServices.GetBoundaries( query, "bounds" );
			if ( boundaries.IsDefined )
			{

				boundariesQuery = Query<AssessmentIndex>
					.Nested( n => n.Path( p => p.Addresses )
					.Query( q => Query<AssessmentIndex>.Range( r => r.Field( f => f.Addresses.First().Longitude ).LessThan( ( double ) boundaries.East ).GreaterThan( ( double ) boundaries.West ) ) && Query<AssessmentIndex>.Range( r => r.Field( f => f.Addresses.First().Latitude ).LessThan( ( double ) boundaries.North ).GreaterThan( ( double ) boundaries.South ) ) ).IgnoreUnmapped() );
			}
			#endregion

			#region Query

			var sort = new SortDescriptor<AssessmentIndex>();

			var sortOrder = query.SortOrder;
			if ( sortOrder == "alpha" )
				sort.Ascending( s => s.Name.Suffix( "keyword" ) );
			else if ( sortOrder == "newest" )
				sort.Field( f => f.LastUpdated, SortOrder.Descending );
			else if ( sortOrder == "oldest" )
				sort.Field( f => f.LastUpdated, SortOrder.Ascending );
			else if ( sortOrder == "relevance" )
				sort.Descending( SortSpecialField.Score );

			if ( query.StartPage < 1 )
				query.StartPage = 1;

			var search = EC.Search<AssessmentIndex>( body => body
				   .Index( AssessmentIndex )
				   .Query( q =>
						competenciesQuery
					  //&& widgetQuery.OwningOrgsQuery  //?????
					  && asmtIdListQuery
					  && subjectsQuery
					  && wsubjectsQuery			//widget specific!!!
					  && keywordsQuery 
					  && asmtMethodTypesQuery
					  && asmtUseTypesQuery
					  && audienceLevelTypeQuery
					  && audienceTypeQuery
					  && deliveryTypesQuery
					  && scoringMethodsQuery
					  && connectionsQuery
					  && occupationsQuery
					  && widgetOccupationsQuery     //widget specific!!!
					  && industriesQuery
					  && windustriesQuery       //widget specific!!!
					  && classificationsQuery
					  //&& relationshipIdQuery
					  && qaFilterQuery && qualityAssuranceSearchQuery
					  && languagesQuery
					  && boundariesQuery
					  && locationQueryFilters.LocationQuery
					  && locationQueryFilters.CountryQuery
					  && locationQueryFilters.CityQuery
					  //&& widgetQuery.KeywordQuery
					  && reportsQuery
					  && ( q.MultiMatch( m => m
						   .Fields( f => f
							   .Field( ff => ff.Name, 90 )
							   .Field( ff => ff.ListTitle, 90 )
							   .Field( ff => ff.Description, 75 )
							   .Field( ff => ff.SubjectWebpage, 25 )
							   .Field( ff => ff.OwnerOrganizationName, 80 )
							   .Field( ff => ff.TextValues, 45 )
							   .Field( ff => ff.CodedNotation, 40 )
							   .Field( ff => ff.SubjectAreas, 50 ) //??

						   )
						   //.Operator( Operator.Or )
						   .Type( TextQueryType.PhrasePrefix )
						   .Query( query.Keywords )
					  //.MaxExpansions( 10 )
					  )
					  || q.MultiMatch( m => m
						  .Fields( f => f
							   .Field( ff => ff.Name, 90 )
							   .Field( ff => ff.ListTitle, 90 )
							   .Field( ff => ff.Description, 75 )
							   .Field( ff => ff.SubjectWebpage, 25 )
							   .Field( ff => ff.OwnerOrganizationName, 80 )
							   .Field( ff => ff.TextValues, 45 )
							   .Field( ff => ff.CodedNotation, 40 )
							   .Field( ff => ff.SubjectAreas, 50 ) //??
						  )
						  //.Operator( Operator.Or )
						  .Type( TextQueryType.BestFields )
						  .Query( query.Keywords )
						  .MaxExpansions( 10 )
						)
					  )
				   )
				   .Sort( s => sort )
				   .From( query.StartPage > 0 ? query.StartPage - 1 : 1 )
				   .Skip( ( query.StartPage - 1 ) * query.PageSize )
				   .Size( query.PageSize ) );


			#endregion

			var debug = search.DebugInformation;
			pTotalRows = ( int ) search.Total;
			if ( pTotalRows > 0 )
			{
				list = ElasticManager.Assessment_MapFromElastic( ( List<AssessmentIndex> ) search.Documents );

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
				if ( deleteIndexFirst && EC.IndexExists( LearningOppIndex ).Exists )
					EC.DeleteIndex( LearningOppIndex );

				if ( !EC.IndexExists( LearningOppIndex ).Exists )
				{
					LearningOppInitializeIndex();
					indexInitialized = true;
				}

				if ( indexInitialized || updatingIndexRegardless )
				{
					int minEntityStateId = UtilityManager.GetAppKeyValue( "minLoppEntityStateId", 3 );

					new ActivityServices().AddActivity( new SiteActivity()
					{ ActivityType = "LearningOpportunity", Activity = "Elastic", Event = "Build Index" }
					);

					var list = ElasticManager.LearningOpp_SearchForElastic( string.Format( "( base.EntityStateId >= {0} )", minEntityStateId ) );

					var results = EC.Bulk( b => b.IndexMany( list, ( d, document ) => d.Index( LearningOppIndex ).Document( document ).Id( document.Id.ToString() ) ) );
					if ( results.ToString().IndexOf( "Valid NEST response built from a successful low level cal" ) == -1 )
					{
						Console.WriteLine( results.ToString() );
						LoggingHelper.DoTrace( 1, " Issue building learning opportunity index: " + results );
					}

					EC.Refresh( LearningOppIndex );
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "LearningOpp_BuildIndex" );
			}
		}

		public static void LearningOppInitializeIndex( bool deleteIndexFirst = true )
		{
			if ( !EC.IndexExists( LearningOppIndex ).Exists )
			{
				EC.CreateIndex( LearningOppIndex, c => new CreateIndexDescriptor( LearningOppIndex )
				 .Settings( st => st
						 .Analysis( an => an.TokenFilters( tf => tf.Stemmer( "custom_english_stemmer", ts => ts.Language( "english" ) ) ).Analyzers( anz => anz.Custom( "custom_lowercase_stemmed", cc => cc.Tokenizer( "standard" ).Filters( new List<string> { "lowercase", "custom_english_stemmer" } ) ) ) ) )
					.Mappings( ms => ms
						.Map<LearningOppIndex>( m => m
							.AutoMap()
							.Properties( p => p
								.Text( s => s.Name( n => n.Description ).Analyzer( "custom_lowercase_stemmed" ) )
								//.Text( s => s.Index( true ).Name( n => n.Name ).Fielddata( true ).Analyzer( "lowercase_analyzer" ) )
								.Nested<IndexCompetency>( n => n
									.Name( nn => nn.TeachesCompetencies )
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
									.Name( nn => nn.Classifications )
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
							)
						)
					)
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
			if ( string.IsNullOrWhiteSpace( filter ) )
				return;
			int action = UtilityManager.GetAppKeyValue( "updateCredIndexAction", 0 );
			if ( action == 0 )
				return;
			try
			{

				var list = ElasticManager.LearningOpp_SearchForElastic( filter );
				if ( list != null && list.Count > 0 )
				{
					processed = list.Count;
					if ( action == 1 )
					{
						foreach ( LearningOppIndex item in list )
						{
							var res = EC.Index( item, idx => idx.Index( LearningOppIndex ) );
							Console.WriteLine( res.Result );
						}

					}
					else if ( action == 2 )
					{
						EC.Bulk( b => b.IndexMany( list, ( d, entity ) => d.Index( LearningOppIndex ).Document( entity ).Id( entity.Id.ToString() ) ) );
					}
				}
				else
				{
					LoggingHelper.DoTrace( 2, string.Format( "LearningOpp_UpdateIndex. No data returned for filter: {0}", filter ) );
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "LearningOpp_UpdateIndex failed for filter: {0}", filter ), false );
			}

		}

		public static List<PM.LearningOpportunityProfile> LearningOppSimpleSearch( MainSearchInput query, ref int pTotalRows )
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
			var sort = new SortDescriptor<LearningOppIndex>();

			var sortOrder = query.SortOrder;
			if ( sortOrder == "alpha" )
				sort.Ascending( s => s.Name.Suffix( "keyword" ) );
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
							   .Field( ff => ff.Name )
							   .Field( ff => ff.Description )
							   .Field( ff => ff.SubjectWebpage )
						   )
				 .Type( TextQueryType.PhrasePrefix )
				 .Query( query.Keywords )
				 .MaxExpansions( 10 ) ) )
				 .Sort( s => sort )
					 //.From( query.StartPage - 1 )
					 .From( query.StartPage > 0 ? query.StartPage - 1 : 1 )
					 .Skip( ( query.StartPage - 1 ) * query.PageSize )
					 .Size( query.PageSize ) );

			pTotalRows = ( int ) search.Total;

			if ( pTotalRows > 0 )
			{
				//map results
				list = ElasticManager.LearningOpp_MapFromElastic( ( List<LearningOppIndex> ) search.Documents );

				LoggingHelper.DoTrace( 6, string.Format( "ElasticServices.OrganizationSearch. found: {0} records", pTotalRows ) );
			}
			//stats
			query.Results = pTotalRows;
			return list;
		}
		public static List<object> LearningOppAutoComplete( string keyword, int maxTerms, ref int pTotalRows )
		{
			var query = string.Format( "*{0}*", keyword.ToLower() );
			var search = EC.Search<LearningOppIndex>( i => i.Index( LearningOppIndex ).Query( q => q.MultiMatch( m => m
			  .Fields( f => f
			  .Field( ff => ff.Name )
			  .Field( ff => ff.Description )
			   )
			   //.Operator( Operator.Or )
			   .Type( TextQueryType.PhrasePrefix )
			   .Query( keyword )
			   .MaxExpansions( 10 ) ) ).Size( maxTerms ) );

			pTotalRows = ( int ) search.Total;
			var list = ( List<LearningOppIndex> ) search.Documents;
			return list.Select( x => x.ListTitle ).Distinct().ToList().Cast<object>().ToList();
		}


		public static List<PM.LearningOpportunityProfile> LearningOppSearch( MainSearchInput query, ref int pTotalRows )
		{
			LearningOpp_BuildIndex();

			var list = new List<PM.LearningOpportunityProfile>();

			QueryContainer competenciesQuery = null;
			QueryContainer subjectsQuery = null;
			QueryContainer keywordsQuery = null;
			QueryContainer audienceTypeQuery = null;
			QueryContainer audienceLevelTypeQuery = null;
			QueryContainer connectionsQuery = null;
			QueryContainer methodTypesQuery = null;
			QueryContainer deliveryTypesQuery = null;
			QueryContainer occupationsQuery = null;
			QueryContainer industriesQuery = null;
			QueryContainer classificationsQuery = null;
			//QueryContainer relationshipIdQuery = null;
			QueryContainer qaFilterQuery = null;
			QueryContainer qualityAssuranceSearchQuery = null;
			QueryContainer languagesQuery = null;
			QueryContainer boundariesQuery = null;
			QueryContainer reportsQuery = null;
			//WidgetQueryFilters widgetQuery = new WidgetQueryFilters();
			LocationQueryFilters locationQueryFilters = new LocationQueryFilters();
			var relationshipTypeIds = new List<int>();

			#region Handle Location queries
			LocationFilter<LearningOppIndex>( query, locationQueryFilters );

			#endregion


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
							competenciesQuery = Query<AssessmentIndex>.Terms( ts => ts.Field( f => f.ReportFilters ).Terms<int>( ids.ToArray() ) );
						}
					}
					if ( competencies.Any() )
					{ 
						competencies.ForEach( x =>
						{
							competenciesQuery |= Query<LearningOppIndex>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.TeachesCompetencies.First().Name, 70 ) ).Type( TextQueryType.PhrasePrefix ).Query( x ) );
							competenciesQuery |= Query<LearningOppIndex>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.TeachesCompetencies.First().Name, 70 ) ).Type( TextQueryType.BestFields ).Query( x ) );
						} );
						if ( competenciesQuery != null )
						competenciesQuery = Query<LearningOppIndex>.Nested( n => n.Path( p => p.TeachesCompetencies ).Query( q => competenciesQuery ).IgnoreUnmapped() );
					}
			}

			#endregion

			#region Subject Areas

			if ( query.FiltersV2.Any( x => x.Name == "subjects" ) )
			{
				var subjects = new List<string>();
				foreach ( var filter in query.FiltersV2.Where( m => m.Name == "subjects" ) )
				{
					var text = ServiceHelper.HandleApostrophes( filter.AsText() );
					if ( string.IsNullOrWhiteSpace( text ) )
						continue;
					subjects.Add( text.ToLower() );
				}

				subjects.ForEach( x =>
				{
					subjectsQuery |= Query<LearningOppIndex>.Match( m => m.Field( f => f.SubjectAreas ).Query( x ).Boost( 60 ) );
				} );
			}
			//keywords from widget
			if ( query.FiltersV2.Any( x => x.Name == "keywords" ) )
			{
				keywordsQuery = HandleKeywords<LearningOppIndex>( query );
				//var tags = new List<string>();
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

			#region MethodTypes, QualityAssurance, Connections

			if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CODE ) )
			{
				//string searchCategories = UtilityManager.GetAppKeyValue( "loppSearchCategories", "21,37," );
				//var categoryIds = new List<int>();
				//foreach ( var s in searchCategories.Split( ',' ) )
				//	if ( !string.IsNullOrEmpty( s ) )
				//		categoryIds.Add( int.Parse( s ) );

				var learningMethodTypesIds = new List<int>();
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
					if ( filter.Name == "reports" || filter.Name == "otherfilters" )
						reportIds.Add( item.Id );

					if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_Learning_Method_Type )
						learningMethodTypesIds.Add( item.Id );
					else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE )
						deliveryTypeIds.Add( item.Id );
					else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL )
						audienceLevelTypeIds.Add( item.Id );
					else if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE )
						audienceTypeIds.Add( item.Id );
					else if ( item.CategoryId == 13 )
						relationshipTypeIds.Add( item.Id );
					else if ( item.CategoryId == 15 )
					{
						if ( validConnections.Contains( item.SchemaName.ToLower() ) )
							connectionFilters.Add( item.SchemaName.Replace( "ceterms:", "" ) );
					}
				}

				if ( learningMethodTypesIds.Any() )
				{
					methodTypesQuery = Query<LearningOppIndex>.Terms( ts => ts.Field( f => f.LearningMethodTypeIds ).Terms<int>( learningMethodTypesIds.ToArray() ) );
				}

				if ( deliveryTypeIds.Any() )
				{
					deliveryTypesQuery = Query<LearningOppIndex>.Terms( ts => ts.Field( f => f.DeliveryMethodTypeIds ).Terms<int>( deliveryTypeIds.ToArray() ) );
				}

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
			qualityAssuranceSearchQuery = CommonQualityAssurance<LearningOppIndex>( query );

			//USED BY QUALITY ASSURANCE FILTER check boxes and org list
			qaFilterQuery = CommonQualityAssuranceFilter<LearningOppIndex>( query, relationshipTypeIds );
			#endregion


			#region Lopp Ids list
			QueryContainer loppIdListQuery = null;
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
						loppIdListQuery |= Query<LearningOppIndex>.Terms( ts => ts.Field( f => f.Id ).Terms( idsList ) );
					} );
				}
			}
			#endregion

			#region Occupations, Industries, Classifications

			occupationsQuery = CommonOccupations<LearningOppIndex>( query );

			industriesQuery = CommonIndustries<LearningOppIndex>( query );

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
					languagesQuery = Query<LearningOppIndex>.Terms( ts => ts.Field( f => f.InLanguage ).Terms( languageFilters ) );
				} );
			}
			#endregion

			#region Boundaries
			var boundaries = SearchServices.GetBoundaries( query, "bounds" );
			if ( boundaries.IsDefined )
			{

				boundariesQuery = Query<LearningOppIndex>
					.Nested( n => n.Path( p => p.Addresses )
					.Query( q => Query<LearningOppIndex>.Range( r => r.Field( f => f.Addresses.First().Longitude ).LessThan( ( double ) boundaries.East ).GreaterThan( ( double ) boundaries.West ) ) && Query<LearningOppIndex>.Range( r => r.Field( f => f.Addresses.First().Latitude ).LessThan( ( double ) boundaries.North ).GreaterThan( ( double ) boundaries.South ) ) ).IgnoreUnmapped() );
			}
			#endregion

			#region Query

			var sort = new SortDescriptor<LearningOppIndex>();

			var sortOrder = query.SortOrder;
			if ( sortOrder == "alpha" )
				sort.Ascending( s => s.Name.Suffix( "keyword" ) );
			else if ( sortOrder == "newest" )
				sort.Field( f => f.LastUpdated, SortOrder.Descending );
			else if ( sortOrder == "oldest" )
				sort.Field( f => f.LastUpdated, SortOrder.Ascending );
			else if ( sortOrder == "relevance" )
				sort.Descending( SortSpecialField.Score );

			//if ( query.Keywords.LastIndexOf( "[" ) > 0 )
			//	query.Keywords = query.Keywords.Substring( 0, query.Keywords.LastIndexOf( "[" ) );

			if ( query.StartPage < 1 )
                query.StartPage = 1;

			var search = EC.Search<LearningOppIndex>( body => body
				   .Index( LearningOppIndex )
				   .Query( q =>
					  competenciesQuery
					  //&& widgetQuery.OwningOrgsQuery //??
					  && loppIdListQuery
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
					  //&& relationshipIdQuery
					  && qaFilterQuery && qualityAssuranceSearchQuery
					  && languagesQuery
					  && boundariesQuery
					  && locationQueryFilters.LocationQuery
					  && locationQueryFilters.CountryQuery
					  && locationQueryFilters.CityQuery
					  //&& widgetQuery.KeywordQuery
					  && reportsQuery
					  && ( q.MultiMatch( m => m
						  .Fields( f => f
							  .Field( ff => ff.Name, 90 )
							  .Field( ff => ff.ListTitle, 90 )
							  .Field( ff => ff.Description, 75 )
							  .Field( ff => ff.SubjectWebpage, 25 )
							  .Field( ff => ff.OwnerOrganizationName, 80 )
							  .Field( ff => ff.TextValues, 45 )
							  .Field( ff => ff.SubjectAreas, 50 ) //??
						  )
						  .Operator( Operator.Or )
						  .Type( TextQueryType.BestFields )
						  .Query( query.Keywords )
						  .MaxExpansions( 10 )

					  )

					  || q.MultiMatch( m => m
						 .Fields( f => f
							  .Field( ff => ff.Name, 90 )
							  .Field( ff => ff.Description, 50 )
							  .Field( ff => ff.SubjectWebpage, 25 )
							  .Field( ff => ff.OwnerOrganizationName, 80 )
							  .Field( ff => ff.TextValues, 60 )
							  .Field( ff => ff.SubjectAreas, 50 ) //??
							  .Field( ff => ff.ListTitle, 95 )
						 )
						 .Operator( Operator.Or )
						 .Type( TextQueryType.BestFields )
						 .Query( query.Keywords )
						)
					 )
				   )
				   .Sort( s => sort )
				   .From( query.StartPage > 0 ? query.StartPage - 1 : 1 )
				   .Skip( ( query.StartPage - 1 ) * query.PageSize )
				   .Size( query.PageSize ) );

			#endregion

			var debug = search.DebugInformation;
			pTotalRows = ( int ) search.Total;
			if ( pTotalRows > 0 )
			{
				list = ElasticManager.LearningOpp_MapFromElastic( ( List<LearningOppIndex> ) search.Documents );
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
			return list;
		}

		#endregion

		#region updates and deletes
		public static void UpdateElastic( bool doingAll )
		{
			//procs have been updated to use the 
			new CacheManager().PopulateAllCaches( doingAll );
			List<String> messages = new List<string>();
			//messages are not used here
			HandlePendingReindexRequests( ref messages );
		}
		public static void HandlePendingReindexRequests( ref List<String> messages )
		{
			LoggingHelper.DoTrace( 1, "ElasticServices.HandlePendingReindexRequests - start" );
			//could centralize cache updates here
			//could possibly use a similar filter approach as below
			int processedCnt = 0;
			string filter = "";
			messages = new List<string>();
			//or could do bulk
			// 
			//will need process to update statusId after completion
			//will need some status from the methods to ensure ok
			var expected = SearchPendingReindexManager.GetAllPendingReindex( ref messages, 1 );
			if ( expected != null && expected.Count > 0)
			{
				filter = "(base.Id in (SELECT [RecordId]  FROM [dbo].[SearchPendingReindex] where [EntityTypeId]= 1 And [StatusId] = 1 AND IsUpdateOrDeleteTypeId= 1 ) )";
				Credential_UpdateIndex( filter, ref processedCnt );
				if ( processedCnt > 0 )
				{

					LoggingHelper.DoTrace( 1, string.Format( "Reindexed {0} credentials.", processedCnt ) );
					//may be dangerous to do an arbitary updateAll?
					//need an indicator of expectations
					if ( processedCnt == expected.Count() )
					{
						messages.Add( string.Format( "Reindexed {0} credentials.", processedCnt ) );
						new SearchPendingReindexManager().UpdateAll( 1, ref messages, 1 );
					}
					else
					{
						//this can happen if there are any pending records in reindex list
						messages.Add( string.Format( "Expected to reindex {0} credentials, but only did: {1}. So skipped the update of SearchPendingReindex!!", expected.Count(), processedCnt ) );
						//do anyway
						new SearchPendingReindexManager().UpdateAll( 1, ref messages, 1 );
					}
				}
			}
			

			processedCnt = 0;
			expected = SearchPendingReindexManager.GetAllPendingReindex( ref messages, 2 );
			if ( expected != null && expected.Count > 0 )
			{
				filter = "(base.Id in (SELECT [RecordId]  FROM [dbo].[SearchPendingReindex] where [EntityTypeId]= 2 And [StatusId] = 1 AND IsUpdateOrDeleteTypeId= 1 ) )";
				Organization_UpdateIndex( filter, ref processedCnt );
				if ( processedCnt > 0 )
				{
					messages.Add( string.Format( "Reindexed {0} organizations.", processedCnt ) );
					LoggingHelper.DoTrace( 1, string.Format( "Reindexed {0} organizations.", processedCnt ) );
					new SearchPendingReindexManager().UpdateAll( 1, ref messages, 2 );
				}
			}
				

			processedCnt = 0;
			expected = SearchPendingReindexManager.GetAllPendingReindex( ref messages, 3 );
			if ( expected != null && expected.Count > 0 )
			{
				filter = "(base.Id in (SELECT [RecordId]  FROM [dbo].[SearchPendingReindex] where [EntityTypeId]= 3 And [StatusId] = 1 AND IsUpdateOrDeleteTypeId= 1 ) )";
				Assessment_UpdateIndex( filter, ref processedCnt );
				if ( processedCnt > 0 )
				{
					messages.Add( string.Format( "Reindexed {0} assessments.", processedCnt ) );
					LoggingHelper.DoTrace( 1, string.Format( "Reindexed {0} assessments.", processedCnt ) );
					new SearchPendingReindexManager().UpdateAll( 1, ref messages, 3 );
				}
			}
			

			processedCnt = 0;

			expected = SearchPendingReindexManager.GetAllPendingReindex( ref messages, 7 );
			if ( expected != null && expected.Count > 0 )
			{
				filter = "(base.Id in (SELECT [RecordId]  FROM [dbo].[SearchPendingReindex] where [EntityTypeId]= 7 And [StatusId] = 1 AND IsUpdateOrDeleteTypeId= 1 ) )";
				LearningOpp_UpdateIndex( filter, ref processedCnt );
				if ( processedCnt > 0 )
				{
					messages.Add( string.Format( "Reindexed {0} learning opportunties.", processedCnt ) );
					LoggingHelper.DoTrace( 1, string.Format( "Reindexed {0} learning opportunties.", processedCnt ) );
					new SearchPendingReindexManager().UpdateAll( 1, ref messages, 7 );
				}
			}
			if (messages.Count == 0)
				messages.Add( "There were no records found for pending re-indexing." );

			//set all in pending to resolved - not ideal - don't want to delete in case updates failed!
			//new SearchPendingReindexManager().UpdateAll( 1, ref messages );

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
							var response = EC.Delete<CredentialIndex>( item.RecordId, d => d.Index( CredentialIndex ).Type( "credentialindex" ) );
							messages.Add( string.Format( "Removed credential #{0} from elastic index", item.RecordId ) );
						}
						break;
					case 2:
						{
							var response = EC.Delete<OrganizationIndex>( item.RecordId, d => d.Index( OrganizationIndex ).Type( "organizationindex" ) );
							messages.Add( string.Format( "Removed organization #{0} from elastic index", item.RecordId ) );
						}
						break;
					case 3:
						{
							var response = EC.Delete<AssessmentIndex>( item.RecordId, d => d.Index( AssessmentIndex ).Type( "assessmentindex" ) );
							messages.Add( string.Format( "Removed assessment #{0} from elastic index", item.RecordId ) );
						}
						break;
					case 7:
						{
							var response = EC.Delete<LearningOppIndex>( item.RecordId, d => d.Index( LearningOppIndex ).Type( "learningoppindex" ) );
							messages.Add( string.Format( "Removed lopp #{0} from elastic index", item.RecordId ) );
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
	}
}

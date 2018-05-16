
using System;
using System.Collections.Generic;
using System.IO;

using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Elasticsearch.Net;
using Nest;
using Newtonsoft.Json;

using workIT.Factories;
using workIT.Models;
using workIT.Models.Common;
using workIT.Models.Elastic;
using workIT.Models.ProfileModels;
using workIT.Models.Search;
using workIT.Utilities;
using PM = workIT.Models.ProfileModels;
using ThisSearchEntity = workIT.Models.Common.CredentialSummary;

namespace workIT.Services
{
    public class ElasticServices
    {
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

        #region Credentials
        #region Build/update index
        public static void BuildCredentialIndex( bool deleteIndexFirst = false )
        {
            try
            {
                if ( deleteIndexFirst && EC.IndexExists( CredentialIndex ).Exists )
                    EC.DeleteIndex( CredentialIndex );

                //was correct before, with latter code in InitCredential??
                if ( !EC.IndexExists( CredentialIndex ).Exists )
                {
                    CredentialInitializeIndex();
                    int pageSize = 0;
                    var list = ElasticManager.Credential_SearchForElastic( "base.EntityStateId = 3", pageSize );

                    var results = EC.Bulk( b => b.IndexMany( list, ( d, credential ) => d.Index( CredentialIndex ).Document( credential ).Id( credential.Id.ToString() ) ) );
                    if ( results.ToString().IndexOf( "Valid NEST response built from a successful low level cal" ) == -1 )
                    {
                        Console.WriteLine( results.ToString() );
                        LoggingHelper.DoTrace( 1, " Issue building credential index: " + results );
                    }

                    //??????
                    var refreshResults = EC.Refresh( CredentialIndex );

                }
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, "BuildCredentialIndex" );
            }
        }
        public static void UpdateCredentialIndex( int recordId )
        {
            if ( recordId < 1 )
                return;
            int action = UtilityManager.GetAppKeyValue( "updateCredIndexAction", 0 );
            if ( action == 0 )
                return;
            try
            {
                string filter = string.Format( " ( base.Id = {0} ) ", recordId );
                var list = ElasticManager.Credential_SearchForElastic( filter );
                if ( list != null && list.Count > 0 )
                {
                    if ( action == 1 )
                    {
                        foreach ( CredentialIndex item in list )
                        {
                            var res = EC.Index( item, idx => idx.Index( CredentialIndex ) );
                            Console.WriteLine( res.Result );
                        }

                    }
                    else if ( action == 2 )
                    {
                        EC.Bulk( b => b.IndexMany( list, ( d, credential ) => d.Index( CredentialIndex ).Document( credential ).Id( credential.Id.ToString() ) ) );
                    }
                }
                else
                {
                    LoggingHelper.DoTrace( 2, string.Format( "UpdateCredentialIndex failed, no data returned for id: {0}", recordId ) );
                }
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
        public static void UpdateCredentialIndex( string filter )
        {
            if ( string.IsNullOrWhiteSpace( filter ) )
                return;
            int action = UtilityManager.GetAppKeyValue( "updateCredIndexAction", 0 );
            if ( action == 0 )
                return;
            try
            {

                var list = ElasticManager.Credential_SearchForElastic( filter );
                if ( list != null && list.Count > 0 )
                {
                    if ( action == 1 )
                    {
                        foreach ( CredentialIndex item in list )
                        {
                            var res = EC.Index( item, idx => idx.Index( CredentialIndex ) );
                            Console.WriteLine( res.Result );
                        }

                    }
                    else if ( action == 2 )
                    {
                        EC.Bulk( b => b.IndexMany( list, ( d, credential ) => d.Index( CredentialIndex ).Document( credential ).Id( credential.Id.ToString() ) ) );
                    }
                }
                else
                {
                    LoggingHelper.DoTrace( 2, string.Format( "UpdateCredentialIndex. NOTE no data returned for filter: {0}", filter ) );
                }
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, string.Format( "UpdateCredentialIndex failed for filter: {0}", filter ), false );
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
                    .Mappings( ms => ms
                        .Map<CredentialIndex>( m => m
                            .AutoMap()
                            .Properties( p => p
                            //.Text( s => s.Index( true ).Name( n => n.Name ).Fielddata( true ).Analyzer( "lowercase_analyzer" ) )
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
                                .Nested<IndexCompetency>( n => n
                                    .Name( nn => nn.Competencies )
                                    .AutoMap()
                                )
                                 //.GeoPoint( g => g.Name( n => n.AddressLocations ) )
                                 .Nested<Models.Elastic.Address>( n => n
                                    .Name( nn => nn.Addresses )
                                    .AutoMap()
                                )
                                .Nested<IndexQualityAssurance>( n => n
                                    .Name( nn => nn.QualityAssurance )
                                    .AutoMap()
                                )
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
        public static List<string> CredentialAutoComplete( string keyword, int maxTerms, ref int pTotalRows )
        {
            var search = EC.Search<CredentialIndex>( i => i.Index( CredentialIndex ).Query( q => q.MultiMatch( m => m
              .Fields( f => f
              .Field( ff => ff.Name )
              .Field( ff => ff.OwnerOrganizationName )
              )
                 //.Operator( Operator.Or )
                 .Type( TextQueryType.PhrasePrefix )
                 .Query( keyword )
                 .MaxExpansions( 10 ) ) ).Size( maxTerms ) );

            //var search = EC.Search<CredentialIndex>( i => i.Index( CredentialIndex ).Query( q => q.Term( t => t.Field( f => f.Subjects.First() ).Value( new[] { "Anatomy" } ) ) ) );

            pTotalRows = ( int )search.Total;

            var list = ( List<CredentialIndex> )search.Documents;
            return list.Select( x => x.ListTitle ).Distinct().ToList();
        }


        public static List<ThisSearchEntity> CredentialSearch( MainSearchInput query, ref int pTotalRows )
        {
            List<ThisSearchEntity> list = new List<CredentialSummary>();

            BuildCredentialIndex();

            QueryContainer credentialTypeQuery = null;
            QueryContainer audienceLevelTypeQuery = null;
            QueryContainer competenciesQuery = null;
            QueryContainer rolesFilterQuery = null;
            QueryContainer subjectsQuery = null;
            QueryContainer connectionsQuery = null;
            QueryContainer occupationsQuery = null;
            QueryContainer industriesQuery = null;
            QueryContainer boundariesQuery = null;
            QueryContainer qualityAssurancesQuery = null;
            QueryContainer reportsQuery = null;

            #region credSearchCategories
            if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CODE ) )
            {
                string searchCategories = UtilityManager.GetAppKeyValue( "credSearchCategories", "21,37," );
                var credSearchCategories = new List<int>();
                foreach ( var s in searchCategories.Split( ',' ) )
                    if ( !string.IsNullOrEmpty( s ) )
                        credSearchCategories.Add( int.Parse( s ) );

                var credentialTypeIds = new List<int>();
                var audienceLevelTypeIds = new List<int>();
                var relationshipTypes = new List<int>();
                var validConnections = new List<string>();
                var connectionFilters = new List<string>();
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
                    if ( filter.Name == "reports" ) reportIds.Add( item.Id );
                    if ( credSearchCategories.Contains( item.CategoryId ) )
                        if ( item.CategoryId == 2 )
                            credentialTypeIds.Add( item.Id );
                        else
                            audienceLevelTypeIds.Add( item.Id );
                    else if ( item.CategoryId == 13 )
                        relationshipTypes.Add( item.Id );
                    else if ( item.CategoryId == 15 )
                    {
                        if ( validConnections.Contains( item.SchemaName.ToLower() ) )
                            connectionFilters.Add( item.SchemaName.Replace( "ceterms:", "" ) );
                    }
                }

                if ( credentialTypeIds.Any() )
                    credentialTypeQuery = Query<CredentialIndex>.Terms( ts => ts.Field( f => f.CredentialTypeId ).Terms( credentialTypeIds.ToArray() ) );

                if ( audienceLevelTypeIds.Any() )
                    audienceLevelTypeQuery = Query<CredentialIndex>.Terms( ts => ts.Field( f => f.AudienceLevelTypeIds ).Terms<int>( audienceLevelTypeIds ) );

                if ( relationshipTypes.Any() )
                    rolesFilterQuery = Query<CredentialIndex>.Terms( ts => ts.Field( f => f.RelationshipTypes ).Terms<int>( relationshipTypes ) );

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

            #region QualityAssurance
            if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CUSTOM ) )
            {
                var assurances = new List<CodeItem>();
                foreach ( var filter in query.FiltersV2.Where( m => m.Name == "qualityassurance" ).ToList() )
                {
                    assurances.Add( filter.AsQaItem() );
                }

                if ( assurances.Any() )
                    assurances.ForEach( x =>
                    {
                        qualityAssurancesQuery |= Query<CredentialIndex>.Nested( n => n.Path( p => p.QualityAssurance ).Query( q => q.Bool( mm => mm.Must( mu => mu.Match( m => m.Field( f => f.QualityAssurance.First().RelationshipTypeId ).Query( x.RelationshipId.ToString() ) ) && mu.Match( m => m.Field( f => f.QualityAssurance.First().AgentRelativeId ).Query( x.Id.ToString() ) ) ) ) ) );

                        //qualityAssurancesQuery |= Query<CredentialIndex>.Nested( n => n.Path( p => p.QualityAssurance ).Query( q => q.Bool( mm => mm.Must( mu => mu.Match( m => m.Field( f => f.QualityAssurance.First().AgentRelativeId ).Query( x.EntityTypeId.ToString() ))))));

                        //qualityAssurancesQuery |= Query<CredentialIndex>.Nested( n => n.Path( p => p.QualityAssurance ).Query( q => q.MultiMatch( mm =>
                        //      mm.Fields( fs => fs.Field( f => f.QualityAssurance.First().RelationshipTypeId ) ).Query( x.EntityTypeId.ToString() )
                        //      &&
                        //      mm.Fields( fs => fs.Field( f => f.QualityAssurance.First().AgentRelativeId ) ).Query( x.Id.ToString() ) ) ) );

                        //qualityAssurancesQuery |= Query<CredentialIndex>.Nested( n => n.Path( p => p.QualityAssurance ).Query( q => q.MatchPhrasePrefix( mp => mp.Field( f => f.QualityAssurance.First().AgentName ).Query( x ) ) ).IgnoreUnmapped() );
                    } );
            }
            #endregion

            #region competencies
            if ( query.FiltersV2.Any( x => x.Name == "competencies" ) )
            {
                var competencies = new List<string>();
                foreach ( var filter in query.FiltersV2.Where( m => m.Name == "competencies" ) )
                {
                    var text = filter.AsText();
                    try
                    {
                        if ( text.IndexOf( " - " ) > -1 )
                            text = text.Substring( text.IndexOf( " -- " ) + 4 );
                    }
                    catch { }

                    if ( text.Trim().Length > 2 )
                        competencies.Add( text.Trim() );
                }

                competencies.ForEach( x =>
                    {
                        //Should eventually change once the Competencies have proper inputs.
                        competenciesQuery |= Query<CredentialIndex>.Nested( n => n.Path( p => p.Competencies ).Query( q => q.Bool( mm => mm.Must( mu => mu.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Competencies.First().Name, 70 ) ).Type( TextQueryType.PhrasePrefix ).Query( x ) ) || mu.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Competencies.First().Name, 70 ) ).Type( TextQueryType.BestFields ).Query( x ) ) ) ) ).IgnoreUnmapped() );

                        // competenciesQuery |= Query<CredentialIndex>.Nested( n => n.Path( p => p.Competencies ).Query( q => q.MultiMatch( mm => mm.Fields( fs => fs.Field( f => f.Competencies.First().Name ).Field( f => f.Competencies.First().Description ) ).Type( TextQueryType.BestFields ).Query( x ) ) ).IgnoreUnmapped() );

                    } );
                //if ( qc != null ) competenciesQuery = Query<CredentialIndex>.Nested( n => n.Path( p => p.Competencies ).Query( q => qc ).IgnoreUnmapped() );
                //competenciesQuery = Query<CredentialIndex>.Nested( n => n.Path( p => p.Competencies ).Query( q => q.Terms( t => t.Field( f => f.Competencies.First().Name ).Terms( competencies ) ) || q.Terms( t => t.Field( f => f.Competencies.First().Description ).Terms( competencies ) ) ).IgnoreUnmapped() );

            }
            #endregion

            #region Occupations
            if ( query.FiltersV2.Any( x => x.Name == "occupations" ) )
            {
                var occupationNames = new List<string>();
                foreach ( var filter in query.FiltersV2.Where( m => m.Name == "occupations" ) )
                {
                    var text = filter.AsText();
                    if ( string.IsNullOrWhiteSpace( text ) ) continue;
                    occupationNames.Add( text );
                }

                QueryContainer qc = null;
                occupationNames.ForEach( name =>
                {
                    qc |= Query<CredentialIndex>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Occupations.First().CodeTitle, 70 ) ).Type( TextQueryType.PhrasePrefix ).Query( name ) );
                    qc |= Query<CredentialIndex>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Industries.First().Name, 70 ) ).Type( TextQueryType.BestFields ).Query( name ) );
                } );

                if ( qc != null ) occupationsQuery = Query<CredentialIndex>.Nested( n => n.Path( p => p.Occupations ).Query( q => qc ).IgnoreUnmapped() );
            }
            #endregion

            #region Industries
            if ( query.FiltersV2.Any( x => x.Name == "industries" ) )
            {
                var industryNames = new List<string>();
                foreach ( var filter in query.FiltersV2.Where( m => m.Name == "industries" ) )
                {
                    var text = filter.AsText();
                    if ( string.IsNullOrWhiteSpace( text ) ) continue;
                    industryNames.Add( text );
                }

                QueryContainer qc = null;
                industryNames.ForEach( name =>
                {
                    qc |= Query<CredentialIndex>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Industries.First().CodeTitle, 70 ) ).Type( TextQueryType.PhrasePrefix ).Query( name ) );
                    qc |= Query<CredentialIndex>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Industries.First().Name, 70 ) ).Type( TextQueryType.BestFields ).Query( name ) );
                } );

                if ( qc != null ) industriesQuery = Query<CredentialIndex>.Nested( n => n.Path( p => p.Industries ).Query( q => qc ).IgnoreUnmapped() );
            }
            #endregion

            #region Subjects

            if ( query.FiltersV2.Any( x => x.Name == "subjects" ) )
            {
                var subjects = new List<string>();
                foreach ( var filter in query.FiltersV2.Where( m => m.Name == "subjects" ) )
                {
                    //var text = ServiceHelper.HandleApostrophes( filter.AsText() );
                    var text = filter.AsText();
                    if ( string.IsNullOrWhiteSpace( text ) )
                        continue;
                    subjects.Add( text.ToLower() );
                    //fnext += OR + string.Format( titleTemplate, SearchifyWord( text ) );
                }

                QueryContainer qc = null;
                subjects.ForEach( x =>
                {
                    qc |= Query<CredentialIndex>.MatchPhrase( mp => mp.Field( f => f.Subjects.First().Name ).Query( x ) );
                } );
                subjectsQuery = Query<CredentialIndex>.Nested( n => n.Path( p => p.Subjects ).Query( q => qc ).IgnoreUnmapped() );
            }
            #endregion

            #region Boundaries

            var boundaries = SearchServices.GetBoundaries( query, "bounds" );
            if ( boundaries.IsDefined )
            {
                boundariesQuery = Query<CredentialIndex>
                      .Nested( n => n.Path( p => p.Addresses )
                      .Query( q => Query<CredentialIndex>.Range( r => r.Field( f => f.Addresses.First().Longitude ).LessThan( ( double )boundaries.East ).GreaterThan( ( double )boundaries.West ) ) && Query<CredentialIndex>.Range( r => r.Field( f => f.Addresses.First().Latitude ).LessThan( ( double )boundaries.North ).GreaterThan( ( double )boundaries.South ) ) ).IgnoreUnmapped() );
            }

            #endregion

            #region Query

            //var tag = string.Format( "*{0}*", query.Keywords.ToLower() );
            //var search = EC.Search<CredentialIndex>( i => i.Index( CredentialIndex ).Query( q => q.Wildcard( w => w.Field( f => f.Name ).Value( query ) ) || q.Wildcard( w => w.Field( f => f.OoName ).Value( query ) ) ) );
            var sort = new SortDescriptor<CredentialIndex>();

            var sortOrder = query.SortOrder;
            if ( sortOrder == "alpha" ) sort.Ascending( s => s.Name.Suffix( "keyword" ) );
            else if ( sortOrder == "newest" ) sort.Field( f => f.LastUpdated, SortOrder.Descending );
            else if ( sortOrder == "oldest" ) sort.Field( f => f.LastUpdated, SortOrder.Ascending );
            else if ( sortOrder == "relevance" ) sort.Descending( SortSpecialField.Score );
            else sort.Ascending( s => s.Name );
            //	.Field( ff => ff.Description )

            if ( query.StartPage < 1 ) query.StartPage = 1;

            var search = EC.Search<CredentialIndex>( body => body
                     .Index( CredentialIndex )
                     .Query( q =>
                        //q.Term( t => t.Field( f => f.EntityStateId ).Value( 3 ) )
                        credentialTypeQuery
                        && connectionsQuery
                        && audienceLevelTypeQuery
                        && competenciesQuery
                        && rolesFilterQuery
                        && subjectsQuery
                        && occupationsQuery
                        && industriesQuery
                        && boundariesQuery
                        && qualityAssurancesQuery
                        && reportsQuery
                        && (
                        //q.Nested( n => n.Path( p => p.TextValues ).Query( qq => qq.Match( m => m.Field( f => f.TextValues.First() ).Query( query.Keywords.Replace( "-", "" ).Replace( " ", "" ) ) ) ).IgnoreUnmapped() )
                        //q.Match( m => m.Field(f=> f.TextValues.First()).Query( query.Keywords.Replace( "-", "" ).Replace( " ", "" ) ) )
                        //||
                        q.MatchPhrasePrefix( mp => mp
                        .Field( f => f.Name )
                        .Query( query.Keywords ) )
                        ||
                        q.MultiMatch( m => m
                             .Fields( f => f
                                 .Field( ff => ff.Name, 90 )
                                 .Field( ff => ff.ListTitle, 90 )
                                 .Field( ff => ff.Description, 75 )
                                 .Field( ff => ff.SubjectWebpage, 25 )
                                 .Field( ff => ff.OwnerOrganizationName, 60 )
                                 .Field( ff => ff.AlternateNames, 35 )
                                 .Field( ff => ff.TextValues, 45 )
                             //.Field( ff => ff.Subject, 50 )

                             // .Field( ff => ff.Keyword )  //note Keyword not populated!!!
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
                                 .Field( ff => ff.Description, 75 )
                                 .Field( ff => ff.SubjectWebpage, 25 )
                                 .Field( ff => ff.OwnerOrganizationName, 60 )
                                 .Field( ff => ff.AlternateNames, 35 )
                                 .Field( ff => ff.TextValues, 45 )
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
            var debug = search.DebugInformation;

            //  var debug = search.DebugInformation;
            #endregion

            pTotalRows = ( int )search.Total;
            if ( pTotalRows > 0 )
            {
                //map results
                list = ElasticManager.Credential_MapFromElastic( ( List<CredentialIndex> )search.Documents );

                LoggingHelper.DoTrace( 6, string.Format( "ElasticServices.CredentialSearch. found: {0} records", pTotalRows ) );
            }
            //return ( List<CredentialIndex> ) search.Documents;

            return list;
        } //


        //test - keep for a while
        public static List<ThisSearchEntity> Search( MainSearchInput query )
        {
            bool valid = true;
            string status = "";
            List<ThisSearchEntity> list = new List<CredentialSummary>();

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

            var pTotalRows = ( int )search.Total;

            //list = JsonConvert.DeserializeObject<List<ThisSearchEntity>>( results );
            //var results = CredentialServices.Search( data, ref totalResults );
            //results = searchService.ConvertCredentialResults( list.Documents.ToList(), totalResults, searchType );

            return list;
        } //
        #endregion
        #endregion

        #region Organizations

        public static void BuildOrganizationIndex( bool deleteIndexFirst = false )
        {
            try
            {
                if ( deleteIndexFirst && EC.IndexExists( OrganizationIndex ).Exists )
                    EC.DeleteIndex( OrganizationIndex );

                //was correct before, with latter code in InitCredential??
                if ( !EC.IndexExists( OrganizationIndex ).Exists )
                {
                    OrganizationInitializeIndex();

                    string filter = "( base.EntityStateId = 3 )";
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
                LoggingHelper.LogError( ex, "BuildOrganizationIndex" );
            }
        }

        public async void CreateIndex()
        {
            //using ( var httpClient = new HttpClient() )
            //{
            //    using ( var request = new HttpRequestMessage( HttpMethod.Put, new Uri( "http://elastic_server_ip/your_index_name" ) ) )
            //    {
            //        var content = @"{ ""settings"" : { ""number_of_shards"" : 1 } }";
            //        request.Content = new StringContent( content );
            //        var response = await httpClient.SendAsync( request );
            //    }
            //}
        }

        public static void OrganizationInitializeIndexTest()
        {
            ElasticClient client = GetClient();
            string indexMappingFile = "";//get the mapping file
            //PostData<T> obj = new PostData<T>(indexMappingFile);

            //var response = client.LowLevel.IndexPut<CredentialIndex>( "credentials", "CredentialIndex2", obj);
            //if ( !response.Success || response.HttpStatusCode != 200 )
            //{
            //    //throw new ElasticsearchServerException( response.ServerError );
            //}

        }

        public static void OrganizationInitializeIndex()
        {
            if ( !EC.IndexExists( OrganizationIndex ).Exists )
            {
                EC.CreateIndex( OrganizationIndex, c => new CreateIndexDescriptor( OrganizationIndex )
                    .Mappings( ms => ms
                        .Map<OrganizationIndex>( m => m
                            .AutoMap()
                            .Properties( p => p
                                //.Text( s => s.Index( true ).Name( n => n.Name ).Analyzer( "lowercase_analyzer" ) )
                                .Text( s => s.Index( true ).Name( n => n.TextValues ) )

                                .Nested<IndexReferenceFramework>( n => n
                                    .Name( nn => nn.ReferenceFrameworks )
                                    .AutoMap( 5 )
                                )
                                 .Nested<Models.Elastic.IndexQualityAssurance>( n => n
                                    .Name( nn => nn.QualityAssurance )
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
                Organization_UpdateIndex( filter );
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
        public static void Organization_UpdateIndex( string filter )
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

        public static List<string> OrganizationAutoComplete( string keyword, int maxTerms, ref int pTotalRows )
        {
            var search = EC.Search<OrganizationIndex>( i => i.Index( OrganizationIndex ).Query( q => q.MultiMatch( m => m
                           .Fields( f => f
                               .Field( ff => ff.Name )
                               .Field( ff => ff.Description )
                               .Field( ff => ff.SubjectWebpage )
                           )
                           //.Operator( Operator.Or )
                           .Type( TextQueryType.PhrasePrefix )
                           .Query( keyword )
                           .MaxExpansions( 10 ) ) ).Size( maxTerms ) );

            //Need to be look for other approaches            

            pTotalRows = ( int )search.Total;
            var list = ( List<OrganizationIndex> )search.Documents;
            return list.Select( x => x.Name ).Distinct().ToList();
        }

        public static List<OrganizationSummary> OrganizationSearch( MainSearchInput query, ref int pTotalRows )
        {
            BuildOrganizationIndex();

            List<OrganizationSummary> list = new List<OrganizationSummary>();

            QueryContainer organizationTypeQuery = null;
            QueryContainer organizationServiceQuery = null;
            QueryContainer sectorTypeQuery = null;
            QueryContainer claimTypeQuery = null;
            QueryContainer qualityAssuranceQuery = null;
            QueryContainer industriesQuery = null;
            QueryContainer boundariesQuery = null;
            QueryContainer qualityAssurancesQuery = null;
            QueryContainer reportsQuery = null;

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
                var qualityAssuranceIds = new List<int>();
                var reportIds = new List<int>();

                foreach ( var filter in query.FiltersV2.Where( m => m.Type == MainSearchFilterV2Types.CODE ).ToList() )
                {
                    var item = filter.AsCodeItem();
                    if ( filter.Name == "reports" ) reportIds.Add( item.Id );
                    //Filters - OrganizationTypes, ServiceTypes
                    if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_ORGANIZATION_TYPE ) organizationTypeIds.Add( item.Id );
                    if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_ORG_SERVICE ) organizationServiceIds.Add( item.Id );
                    //Filters - Sector Types 
                    if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_ORGANIZATION_SECTORTYPE )
                        sectorTypeIds.Add( item.Id );
                    //Filters - Claim Types 
                    if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_CLAIM_TYPE )
                        claimTypeIds.Add( item.Id );

                    //Filters - Quality Assurance
                    if ( item.CategoryId == 13 ) qualityAssuranceIds.Add( item.Id );
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

                //claimTypeIds.ForEach( x =>
                //{
                //    //claimTypeQuery |= Query<OrganizationIndex>.Nested( n => n.Path( p => p.OrganizationClaimTypes ).Query( q => q.Bool( mm => mm.Must( mu => mu.Match( m => m.Field( f => f.OrganizationClaimTypes.First().SchemaName ).Query( x.SchemaName ) ) && mu.Match( m => m.Field( f => f.OrganizationClaimTypes.First().Id ).Query( x.Id.ToString() ) ) ) ) ) );
                //    //claimTypeQuery |= Query<OrganizationIndex>.Nested( n => n.Path( p => p.OrganizationClaimTypes ).Query( q => q.Bool( mm => mm.Must( mu => mu.Match( m => m.Field( f => f.OrganizationClaimTypes.First().Id ).Query( x.Id.ToString() ) ) ) ) ) );
                //} );

                if ( claimTypeIds.Any() )
                    claimTypeQuery = Query<OrganizationIndex>.Terms( ts => ts.Field( f => f.OrganizationClaimTypeIds ).Terms<int>( claimTypeIds ) );

                if ( qualityAssuranceIds.Any() )
                    qualityAssuranceQuery = Query<OrganizationIndex>.Terms( ts => ts.Field( f => f.AgentRelationships ).Terms<int>( qualityAssuranceIds.ToArray() ) );
            }

            #endregion

            #region Industries
            if ( query.FiltersV2.Any( x => x.Name == "industries" ) )
            {
                var industryNames = new List<string>();
                foreach ( var filter in query.FiltersV2.Where( m => m.Name == "industries" ) )
                {
                    var text = filter.AsText();
                    if ( string.IsNullOrWhiteSpace( text ) )
                        continue;
                    industryNames.Add( text.ToLower() );
                }

                QueryContainer qc = null;
                industryNames.ForEach( name =>
                {
                    qc |= Query<OrganizationIndex>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.ReferenceFrameworks.First().CodeTitle, 70 ) ).Type( TextQueryType.PhrasePrefix ).Query( name ) );

                    qc |= Query<CredentialIndex>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Industries.First().Name, 70 ) ).Type( TextQueryType.BestFields ).Query( name ) );
                } );

                if ( qc != null ) industriesQuery = Query<OrganizationIndex>.Nested( n => n.Path( p => p.ReferenceFrameworks ).Query( q => qc ).IgnoreUnmapped() );
            }
            #endregion

            #region QualityAssurance
            if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CUSTOM ) )
            {
                var assurances = new List<CodeItem>();

                foreach ( var filter in query.FiltersV2.Where( m => m.Name == "qa" ).ToList() )
                {
                    assurances.Add( filter.AsQaItem() );
                }

                if ( assurances.Any() )
                    assurances.ForEach( x =>
                    {
                        qualityAssurancesQuery |= Query<OrganizationIndex>.Nested( n => n.Path( p => p.QualityAssurance ).Query( q => q.Bool( mm => mm.Must( mu => mu.Match( m => m.Field( f => f.QualityAssurance.First().RelationshipTypeId ).Query( x.RelationshipId.ToString() ) ) && mu.Match( m => m.Field( f => f.QualityAssurance.First().AgentRelativeId ).Query( x.Id.ToString() ) ) ) ) ) );
                    } );
            }
            #endregion

            #region Boundaries

            var boundaries = SearchServices.GetBoundaries( query, "bounds" );
            if ( boundaries.IsDefined )
            {
                boundariesQuery = Query<OrganizationIndex>
                    .Nested( n => n.Path( p => p.Addresses )
                    .Query( q => Query<OrganizationIndex>.Range( r => r.Field( f => f.Addresses.First().Longitude ).LessThan( ( double )boundaries.East ).GreaterThan( ( double )boundaries.West ) ) && Query<OrganizationIndex>.Range( r => r.Field( f => f.Addresses.First().Latitude ).LessThan( ( double )boundaries.North ).GreaterThan( ( double )boundaries.South ) ) ).IgnoreUnmapped() );
            }
            #endregion

            #region Query

            var sort = new SortDescriptor<OrganizationIndex>();

            var sortOrder = query.SortOrder;
            if ( sortOrder == "alpha" ) sort.Ascending( s => s.Name.Suffix( "keyword" ) );
            else if ( sortOrder == "newest" ) sort.Field( f => f.LastUpdated, SortOrder.Descending );
            else if ( sortOrder == "oldest" ) sort.Field( f => f.LastUpdated, SortOrder.Ascending );
            else if ( sortOrder == "relevance" ) sort.Descending( SortSpecialField.Score );
            else sort.Ascending( s => s.Name );
            //								.Field( ff => ff.Description )
            if ( query.StartPage < 1 ) query.StartPage = 1;

            var search = EC.Search<OrganizationIndex>( body => body
                   .Index( OrganizationIndex )
                   .Query( q =>
                      //q.Term( t => t.Field( f => f.EntityTypeId ).Value( 3 ) )
                      organizationTypeQuery
                      && organizationServiceQuery
                      && sectorTypeQuery
                      && qualityAssuranceQuery
                      && industriesQuery
                      && boundariesQuery
                      && qualityAssurancesQuery
                      && reportsQuery
                      && claimTypeQuery
                      && ( q.MultiMatch( m => m
                           .Fields( f => f
                               .Field( ff => ff.Name, 90 )
                               .Field( ff => ff.Description, 75 )
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
                               .Field( ff => ff.Name, 90 )
                               .Field( ff => ff.Description, 75 )
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
                   .Size( query.PageSize ) );

            var debug = search.DebugInformation;
            #endregion

            pTotalRows = ( int )search.Total;
            if ( pTotalRows > 0 )
            {
                list = ElasticManager.Organization_MapFromElastic( ( List<OrganizationIndex> )search.Documents );
                LoggingHelper.DoTrace( 6, string.Format( "ElasticServices.OrganizationSearch. found: {0} records", pTotalRows ) );
            }

            return list;
        }

        #endregion

        #region Assessments

        #region Build/update index
        public static void BuildAssessmentIndex( bool deleteIndexFirst = false )
        {
            List<AssessmentIndex> list = new List<Models.Elastic.AssessmentIndex>();
            if ( deleteIndexFirst && EC.IndexExists( AssessmentIndex ).Exists )
                EC.DeleteIndex( AssessmentIndex );
            if ( !EC.IndexExists( AssessmentIndex ).Exists )
            {
                try
                {
                    AssessmentInitializeIndex();

                    list = ElasticManager.Assessment_SearchForElastic( "( base.EntityStateId = 3 )" );
                }
                catch ( Exception ex )
                {
                    LoggingHelper.LogError( ex, "BuildAssessmentIndex" );
                }
                finally
                {
                    if ( list != null && list.Count > 100 )
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
                EC.CreateIndex( AssessmentIndex, c => new CreateIndexDescriptor( AssessmentIndex )
                    .Mappings( ms => ms
                        .Map<AssessmentIndex>( m => m
                            .AutoMap()
                            .Properties( p => p
                                //.Text( s => s.Index( true ).Name( n => n.Name ).Analyzer( "lowercase_analyzer" ) )
                                .Nested<IndexCompetency>( n => n
                                    .Name( nn => nn.AssessesCompetencies )
                                    .AutoMap()
                                )
                                .Nested<IndexReferenceFramework>( n => n
                                    .Name( nn => nn.Classifications )
                                    .AutoMap()
                                )
                                .Nested<Models.Elastic.Address>( n => n
                                    .Name( nn => nn.Addresses )
                                    .AutoMap()
                                )
                                .Nested<IndexQualityAssurance>( n => n
                                    .Name( nn => nn.QualityAssurance )
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
                Assessment_UpdateIndex( filter );
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
        public static void Assessment_UpdateIndex( string filter )
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


        public static List<string> AssessmentAutoComplete( string keyword, int maxTerms, ref int pTotalRows )
        {
            var query = string.Format( "*{0}*", keyword.ToLower() );
            var search = EC.Search<AssessmentIndex>( i => i.Index( AssessmentIndex ).Query( q => q.MultiMatch( m => m
                           .Fields( f => f
                               .Field( ff => ff.Name )
                               .Field( ff => ff.Description )
                           )
                           //.Operator( Operator.Or )
                           .Type( TextQueryType.PhrasePrefix )
                           .Query( keyword )
                           .MaxExpansions( 10 ) ) ).Size( maxTerms ) );

            pTotalRows = ( int )search.Total;
            var list = ( List<AssessmentIndex> )search.Documents;
            return list.Select( x => x.ListTitle ).Distinct().ToList();
        }

        public static List<AssessmentProfile> AssessmentSearch( MainSearchInput query, ref int pTotalRows )
        {
            BuildAssessmentIndex();

            var list = new List<PM.AssessmentProfile>();

            QueryContainer competenciesQuery = null;
            QueryContainer subjectsQuery = null;
            QueryContainer connectionsQuery = null;
            QueryContainer asmtMethodTypesQuery = null;
            QueryContainer asmtUseTypesQuery = null;
            QueryContainer deliveryTypesQuery = null;
            QueryContainer scoringMethodsQuery = null;
            QueryContainer classificationsQuery = null;
            QueryContainer relationshipIdQuery = null;
            QueryContainer qualityAssurancesQuery = null;
            QueryContainer boundariesQuery = null;
            QueryContainer reportsQuery = null;

            #region Competencies

            if ( query.FiltersV2.Any( x => x.Name == "competencies" ) )
            {
                var competencies = new List<string>();
                foreach ( var filter in query.FiltersV2.Where( m => m.Name == "competencies" ) )
                {
                    var text = filter.AsText();
                    try
                    {
                        if ( text.IndexOf( " - " ) > -1 ) text = text.Substring( text.IndexOf( " -- " ) + 4 );
                    }
                    catch { }

                    if ( text.Trim().Length > 2 )
                        competencies.Add( text.Trim() );
                }

                if ( competencies.Any() )
                    competencies.ForEach( x =>
                    {
                        //Temporary fix
                        competenciesQuery |= Query<AssessmentIndex>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.AssessesCompetencies.First().Name, 70 ) ).Type( TextQueryType.PhrasePrefix ).Query( x ) );
                        competenciesQuery |= Query<AssessmentIndex>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.AssessesCompetencies.First().Name, 70 ) ).Type( TextQueryType.BestFields ).Query( x ) );
                    } );
                if ( competenciesQuery != null ) competenciesQuery = Query<AssessmentIndex>.Nested( n => n.Path( p => p.AssessesCompetencies ).Query( q => competenciesQuery ).IgnoreUnmapped() );
            }

            #endregion  

            #region Subject Areas

            if ( query.FiltersV2.Any( x => x.Name == "subjects" ) )
            {
                var subjects = new List<string>();
                foreach ( var filter in query.FiltersV2.Where( m => m.Name == "subjects" ) )
                {
                    var text = ServiceHelper.HandleApostrophes( filter.AsText() );
                    if ( string.IsNullOrWhiteSpace( text ) ) continue;
                    subjects.Add( text.ToLower() );
                }

                //QueryContainer qc = null;
                subjects.ForEach( x =>
                {
                    subjectsQuery |= Query<AssessmentIndex>.Match( m => m.Field( f => f.SubjectAreas ).Query( x ) );
                } );
                //subjectsQuery = Query<AssessmentIndex>.Nested( n => n.Path( p => p.SubjectAreas ).Query( q => qc ).IgnoreUnmapped() );
            }

            #endregion

            #region Properties

            if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CODE ) )
            {
                string searchCategories = UtilityManager.GetAppKeyValue( "asmtSearchCategories", "21,37," );
                var categoryIds = new List<int>();
                foreach ( var s in searchCategories.Split( ',' ) )
                    if ( !string.IsNullOrEmpty( s ) )
                        categoryIds.Add( int.Parse( s ) );

                var asmtMethodsIds = new List<int>();
                var asmtUseIds = new List<int>();
                var reportIds = new List<int>();
                var deliveryTypeIds = new List<int>();
                var scoringMethodIds = new List<int>();
                var relationshipTypeIds = new List<int>();
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
                    if ( filter.Name == "reports" ) reportIds.Add( item.Id );

                    //if ( categoryIds.Contains( item.CategoryId ) ) propertyValueIds.Add( item.Id );
                    if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_Assessment_Method_Type ) asmtMethodsIds.Add( item.Id );
                    if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_ASSESSMENT_USE_TYPE ) asmtUseIds.Add( item.Id );
                    if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE ) deliveryTypeIds.Add( item.Id );
                    if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_Scoring_Method ) scoringMethodIds.Add( item.Id );
                    if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE ) relationshipTypeIds.Add( item.Id );
                    if ( item.CategoryId == 15 )
                    {
                        if ( validConnections.Contains( item.SchemaName.ToLower() ) )
                            connectionFilters.Add( item.SchemaName.Replace( "ceterms:", "" ) );
                    }
                }

                if ( asmtMethodsIds.Any() )
                {
                    asmtMethodTypesQuery = Query<AssessmentIndex>.Terms( ts => ts.Field( f => f.AssessmentMethodTypeIds ).Terms<int>( asmtMethodsIds.ToArray() ) );

                }
                if ( asmtUseIds.Any() )
                    asmtUseTypesQuery = Query<AssessmentIndex>.Terms( ts => ts.Field( f => f.AssessmentUseTypeIds ).Terms<int>( asmtUseIds.ToArray() ) );
                if ( deliveryTypeIds.Any() )
                    deliveryTypesQuery = Query<AssessmentIndex>.Terms( ts => ts.Field( f => f.DeliveryMethodTypeIds ).Terms<int>( deliveryTypeIds.ToArray() ) );
                if ( scoringMethodIds.Any() )
                    scoringMethodsQuery = Query<AssessmentIndex>.Terms( ts => ts.Field( f => f.ScoringMethodTypeIds ).Terms<int>( scoringMethodIds.ToArray() ) );
                if ( relationshipTypeIds.Any() )
                    //qualityAssuranceQuery = Query<AssessmentIndex>.Terms( ts => ts.Field( f => f.QualityAssurances ).Terms<int>( relationshipTypeIds.ToArray() ) );
                    relationshipIdQuery = Query<AssessmentIndex>.Nested( n => n.Path( p => p.QualityAssurance ).Query( q => q.Terms( t => t.Field( f => f.QualityAssurance.First().RelationshipTypeId ).Terms<int>( relationshipTypeIds.ToArray() ) ) ) );

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
            if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CUSTOM ) )
            {
                var assurances = new List<CodeItem>();
                foreach ( var filter in query.FiltersV2.Where( m => m.Name == "qualityassurance" ).ToList() )
                {
                    assurances.Add( filter.AsQaItem() );
                }

                if ( assurances.Any() )
                    assurances.ForEach( x =>
                    {
                        qualityAssurancesQuery |= Query<AssessmentIndex>.Nested( n => n.Path( p => p.QualityAssurance ).Query( q => q.Bool( mm => mm.Must( mu => mu.Match( m => m.Field( f => f.QualityAssurance.First().RelationshipTypeId ).Query( x.RelationshipId.ToString() ) ) && mu.Match( m => m.Field( f => f.QualityAssurance.First().AgentRelativeId ).Query( x.Id.ToString() ) ) ) ) ).IgnoreUnmapped() );
                    } );
            }
            #endregion

            #region Classifications
            if ( query.FiltersV2.Any( x => x.Name == "instructionalprogramtype" ) )
            {
                var CIPNames = new List<string>();
                foreach ( var filter in query.FiltersV2.Where( m => m.Name == "instructionalprogramtype" ) )
                {
                    var text = filter.AsText();
                    if ( string.IsNullOrWhiteSpace( text ) ) continue;
                    CIPNames.Add( text );
                }

                QueryContainer qc = null;
                CIPNames.ForEach( name =>
                {
                    qc |= Query<AssessmentIndex>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Classifications.First().CodeTitle, 70 ) ).Type( TextQueryType.PhrasePrefix ).Query( name ) );
                    qc |= Query<AssessmentIndex>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Classifications.First().Name, 70 ) ).Type( TextQueryType.BestFields ).Query( name ) );

                } );

                if ( qc != null ) classificationsQuery = Query<AssessmentIndex>.Nested( n => n.Path( p => p.Classifications ).Query( q => qc ).IgnoreUnmapped() );
            }
            #endregion


            #region Boundaries
            var boundaries = SearchServices.GetBoundaries( query, "bounds" );
            if ( boundaries.IsDefined )
            {

                boundariesQuery = Query<AssessmentIndex>
                    .Nested( n => n.Path( p => p.Addresses )
                    .Query( q => Query<AssessmentIndex>.Range( r => r.Field( f => f.Addresses.First().Longitude ).LessThan( ( double )boundaries.East ).GreaterThan( ( double )boundaries.West ) ) && Query<AssessmentIndex>.Range( r => r.Field( f => f.Addresses.First().Latitude ).LessThan( ( double )boundaries.North ).GreaterThan( ( double )boundaries.South ) ) ).IgnoreUnmapped() );
            }
            #endregion

            #region Query

            var sort = new SortDescriptor<AssessmentIndex>();

            var sortOrder = query.SortOrder;
            if ( sortOrder == "alpha" ) sort.Ascending( s => s.Name.Suffix("keyword") );
            else if ( sortOrder == "newest" ) sort.Field( f => f.LastUpdated, SortOrder.Descending );
            else if ( sortOrder == "oldest" ) sort.Field( f => f.LastUpdated, SortOrder.Ascending );
            else if ( sortOrder == "relevance" ) sort.Descending( SortSpecialField.Score );

            if ( query.StartPage < 1 ) query.StartPage = 1;

            var search = EC.Search<AssessmentIndex>( body => body
                   .Index( AssessmentIndex )
                   .Query( q =>
                        competenciesQuery
                      && subjectsQuery
                      && asmtMethodTypesQuery
                      && asmtUseTypesQuery
                      && deliveryTypesQuery
                      && scoringMethodsQuery
                      && connectionsQuery
                      && classificationsQuery
                      && relationshipIdQuery
                      && qualityAssurancesQuery
                      && boundariesQuery
                      && reportsQuery
                      && ( q.MultiMatch( m => m
                           .Fields( f => f
                               .Field( ff => ff.Name, 90 )
                               .Field( ff => ff.ListTitle, 90 )
                               .Field( ff => ff.Description, 75 )
                               .Field( ff => ff.SubjectWebpage, 25 )
                               .Field( ff => ff.Organization, 60 )
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
                               .Field( ff => ff.Organization, 60 )
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

            pTotalRows = ( int )search.Total;
            if ( pTotalRows > 0 )
            {
                list = ElasticManager.Assessment_MapFromElastic( ( List<AssessmentIndex> )search.Documents );

                LoggingHelper.DoTrace( 6, string.Format( "ElasticServices.AssessmentSearch. found: {0} records", pTotalRows ) );
            }
            return list;
        }
        #endregion

        #region Learning Opportunities
        public static void BuildLearningOppIndex( bool deleteIndexFirst = false )
        {
            try
            {
                if ( deleteIndexFirst && EC.IndexExists( LearningOppIndex ).Exists ) EC.DeleteIndex( LearningOppIndex );

                if ( !EC.IndexExists( LearningOppIndex ).Exists )
                {
                    LearningOppInitializeIndex();

                    var list = ElasticManager.LearningOpp_SearchForElastic( "( base.EntityStateId = 3 )" );

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
                LoggingHelper.LogError( ex, "BuildLearningOppIndex" );
            }
        }

        public static void LearningOppInitializeIndex( bool deleteIndexFirst = true )
        {
            if ( !EC.IndexExists( LearningOppIndex ).Exists )
            {
                EC.CreateIndex( LearningOppIndex, c => new CreateIndexDescriptor( LearningOppIndex )
                    .Mappings( ms => ms
                        .Map<LearningOppIndex>( m => m
                            .AutoMap()
                            .Properties( p => p
                                //.Text( s => s.Index( true ).Name( n => n.Name ).Fielddata( true ).Analyzer( "lowercase_analyzer" ) )
                                .Nested<IndexCompetency>( n => n
                                    .Name( nn => nn.TeachesCompetencies )
                                    .AutoMap()
                                )
                                .Nested<IndexReferenceFramework>( n => n
                                    .Name( nn => nn.Classifications )
                                    .AutoMap()
                               )
                                .Nested<Models.Elastic.Address>( n => n
                                    .Name( nn => nn.Addresses )
                                    .AutoMap()
                                )
                                .Nested<IndexQualityAssurance>( n => n
                                    .Name( nn => nn.QualityAssurance )
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
            LearningOpp_UpdateIndex( filter );
        } //


        /// <summary>
        /// Pass a filter to use for updating the index
        /// </summary>
        /// <param name="filter"></param>
        public static void LearningOpp_UpdateIndex( string filter )
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


        public static List<string> LearningOppAutoComplete( string keyword, int maxTerms, ref int pTotalRows )
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

            pTotalRows = ( int )search.Total;
            var list = ( List<LearningOppIndex> )search.Documents;
            return list.Select( x => x.ListTitle ).Distinct().ToList();
        }


        public static List<PM.LearningOpportunityProfile> LearningOppSearch( MainSearchInput query, ref int pTotalRows )
        {
            BuildLearningOppIndex();

            var list = new List<PM.LearningOpportunityProfile>();

            QueryContainer competenciesQuery = null;
            QueryContainer subjectsQuery = null;
            QueryContainer connectionsQuery = null;
            QueryContainer methodTypesQuery = null;
            QueryContainer deliveryTypesQuery = null;
            QueryContainer classificationsQuery = null;
            QueryContainer relationshipIdQuery = null;
            QueryContainer qualityAssurancesQuery = null;
            QueryContainer boundariesQuery = null;
            QueryContainer reportsQuery = null;

            #region Competencies

            if ( query.FiltersV2.Any( x => x.Name == "competencies" ) )
            {
                var competencies = new List<string>();
                foreach ( var filter in query.FiltersV2.Where( m => m.Name == "competencies" ) )
                {
                    var text = filter.AsText();
                    try
                    {
                        if ( text.IndexOf( " - " ) > -1 ) text = text.Substring( text.IndexOf( " -- " ) + 4 );
                    }
                    catch { }

                    if ( text.Trim().Length > 2 )
                        competencies.Add( text.Trim() );
                }
                if ( competencies.Any() )
                    competencies.ForEach( x =>
                    {
                        //Temporary fix
                        competenciesQuery |= Query<LearningOppIndex>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.TeachesCompetencies.First().Name, 70 ) ).Type( TextQueryType.PhrasePrefix ).Query( x ) );
                        competenciesQuery |= Query<LearningOppIndex>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.TeachesCompetencies.First().Name, 70 ) ).Type( TextQueryType.BestFields ).Query( x ) );
                    } );
                if ( competenciesQuery != null ) competenciesQuery = Query<LearningOppIndex>.Nested( n => n.Path( p => p.TeachesCompetencies ).Query( q => competenciesQuery ).IgnoreUnmapped() );
            }

            #endregion

            #region Subject Areas

            if ( query.FiltersV2.Any( x => x.Name == "subjects" ) )
            {
                var subjects = new List<string>();
                foreach ( var filter in query.FiltersV2.Where( m => m.Name == "subjects" ) )
                {
                    var text = ServiceHelper.HandleApostrophes( filter.AsText() );
                    if ( string.IsNullOrWhiteSpace( text ) ) continue;
                    subjects.Add( text.ToLower() );
                }

                subjects.ForEach( x =>
                {
                    subjectsQuery |= Query<LearningOppIndex>.Match( m => m.Field( f => f.SubjectAreas ).Query( x ) );
                } );
            }

            #endregion

            #region MethodTypes, QualityAssurance, Connections

            if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CODE ) )
            {
                string searchCategories = UtilityManager.GetAppKeyValue( "loppSearchCategories", "21,37," );
                var categoryIds = new List<int>();
                foreach ( var s in searchCategories.Split( ',' ) )
                    if ( !string.IsNullOrEmpty( s ) )
                        categoryIds.Add( int.Parse( s ) );

                var learningMethodTypesIds = new List<int>();
                var deliveryTypeIds = new List<int>();
                var relationshipTypeIds = new List<int>();
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
                    if ( filter.Name == "reports" ) reportIds.Add( item.Id );

                    if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_Learning_Method_Type ) learningMethodTypesIds.Add( item.Id );
                    if ( item.CategoryId == CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE ) deliveryTypeIds.Add( item.Id );

                    if ( item.CategoryId == 13 ) relationshipTypeIds.Add( item.Id );
                    if ( item.CategoryId == 15 )
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
                    relationshipIdQuery = Query<LearningOppIndex>.Nested( n => n.Path( p => p.QualityAssurance ).Query( q => q.Terms( t => t.Field( f => f.QualityAssurance.First().RelationshipTypeId ).Terms<int>( relationshipTypeIds.ToArray() ) ) ) );
                }

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
            if ( query.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CUSTOM ) )
            {
                var assurances = new List<CodeItem>();
                foreach ( var filter in query.FiltersV2.Where( m => m.Name == "qualityassurance" ).ToList() )
                {
                    assurances.Add( filter.AsQaItem() );
                }

                if ( assurances.Any() )
                    assurances.ForEach( x =>
                    {
                        qualityAssurancesQuery |= Query<LearningOppIndex>.Nested( n => n.Path( p => p.QualityAssurance ).Query( q => q.Bool( mm => mm.Must( mu => mu.Match( m => m.Field( f => f.QualityAssurance.First().RelationshipTypeId ).Query( x.RelationshipId.ToString() ) ) && mu.Match( m => m.Field( f => f.QualityAssurance.First().AgentRelativeId ).Query( x.Id.ToString() ) ) ) ) ) );
                    } );
            }
            #endregion

            #region Classifications
            if ( query.FiltersV2.Any( x => x.Name == "instructionalprogramtype" ) )
            {
                var CIPNames = new List<string>();
                foreach ( var filter in query.FiltersV2.Where( m => m.Name == "instructionalprogramtype" ) )
                {
                    var text = filter.AsText();
                    if ( string.IsNullOrWhiteSpace( text ) ) continue;
                    CIPNames.Add( text );
                }

                QueryContainer qc = null;
                CIPNames.ForEach( name =>
                {
                    qc |= Query<LearningOppIndex>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Classifications.First().CodeTitle ) ).Type( TextQueryType.PhrasePrefix ).Query( name ) );

                    qc |= Query<LearningOppIndex>.MultiMatch( m => m.Fields( mf => mf.Field( f => f.Classifications.First().Name ) ).Type( TextQueryType.BestFields ).Query( name ) );
                } );

                if ( qc != null ) classificationsQuery = Query<LearningOppIndex>.Nested( n => n.Path( p => p.Classifications ).Query( q => qc ).IgnoreUnmapped() );
            }
            #endregion

            #region Boundaries
            var boundaries = SearchServices.GetBoundaries( query, "bounds" );
            if ( boundaries.IsDefined )
            {

                boundariesQuery = Query<LearningOppIndex>
                    .Nested( n => n.Path( p => p.Addresses )
                    .Query( q => Query<LearningOppIndex>.Range( r => r.Field( f => f.Addresses.First().Longitude ).LessThan( ( double )boundaries.East ).GreaterThan( ( double )boundaries.West ) ) && Query<LearningOppIndex>.Range( r => r.Field( f => f.Addresses.First().Latitude ).LessThan( ( double )boundaries.North ).GreaterThan( ( double )boundaries.South ) ) ).IgnoreUnmapped() );
            }
            #endregion

            #region Query

            var sort = new SortDescriptor<LearningOppIndex>();

            var sortOrder = query.SortOrder;
            if ( sortOrder == "alpha" ) sort.Ascending( s => s.Name.Suffix( "keyword" ) );
            else if ( sortOrder == "newest" ) sort.Field( f => f.LastUpdated, SortOrder.Descending );
            else if ( sortOrder == "oldest" ) sort.Field( f => f.LastUpdated, SortOrder.Ascending );
            else if ( sortOrder == "relevance" ) sort.Descending( SortSpecialField.Score );

            if ( query.StartPage < 1 ) query.StartPage = 1;

            var search = EC.Search<LearningOppIndex>( body => body
                   .Index( LearningOppIndex )
                   .Query( q =>
                      competenciesQuery
                      && subjectsQuery
                      && methodTypesQuery
                      && deliveryTypesQuery
                      && classificationsQuery
                      && connectionsQuery
                      && relationshipIdQuery
                      && qualityAssurancesQuery
                      && boundariesQuery
                      && reportsQuery
                      && ( q.MultiMatch( m => m
                          .Fields( f => f
                              .Field( ff => ff.Name, 90 )
                              .Field( ff => ff.ListTitle, 90 )
                              .Field( ff => ff.Description, 75 )
                              .Field( ff => ff.SubjectWebpage, 25 )
                              .Field( ff => ff.Organization, 60 )
                              .Field( ff => ff.TextValues, 45 )
                              .Field( ff => ff.Subject, 50 )
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
                              .Field( ff => ff.Organization, 60 )
                              .Field( ff => ff.TextValues, 60 )
                              .Field( ff => ff.Subject, 50 )
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

            pTotalRows = ( int )search.Total;
            if ( pTotalRows > 0 )
            {
                list = ElasticManager.LearningOpp_MapFromElastic( ( List<LearningOppIndex> )search.Documents );
                LoggingHelper.DoTrace( 6, string.Format( "ElasticServices.LearningOppSearch. found: {0} records", pTotalRows ) );
            }
            return list;
        }

        #endregion


        public static void UpdateElastic()
        {
            //procs have been updated to use the 
            new CacheManager().PopulateAllCaches();
            //
            HandlePendingReindexRequests();
        }
        public static void HandlePendingReindexRequests()
        {
            //could centralize cache updates here
            //could possibly use a similar filter approach as below

            //or could do bulk
            // 
            //will need process to update statusId after completion
            //will need some status from the methods to ensure ok
            string filter = "(base.Id in (SELECT [RecordId]  FROM [dbo].[SearchPendingReindex] where [EntityTypeId]= 1 And [StatusId] = 1) )";
            UpdateCredentialIndex( filter );

            filter = "(base.Id in (SELECT [RecordId]  FROM [dbo].[SearchPendingReindex] where [EntityTypeId]= 2 And [StatusId] = 1) )";
            Organization_UpdateIndex( filter );

            filter = "(base.Id in (SELECT [RecordId]  FROM [dbo].[SearchPendingReindex] where [EntityTypeId]= 3 And [StatusId] = 1) )";
            Assessment_UpdateIndex( filter );

            filter = "(base.Id in (SELECT [RecordId]  FROM [dbo].[SearchPendingReindex] where [EntityTypeId]= 7 And [StatusId] = 1) )";
            LearningOpp_UpdateIndex( filter );

            List<String> messages = new List<string>();
            new SearchPendingReindexManager().UpdateAll( 1, ref messages );

            //List<SearchPendingReindex> list = SearchPendingReindexManager.GetAllPendingReindex();
            //if ( list == null || list.Count == 0 )
            //    return;
            //foreach ( var item in list )
            //{
            //    switch ( item.EntityTypeId )
            //    {
            //        case 1:
            //            {

            //            }
            //            break;
            //        case 2:
            //            {

            //            }
            //            break;
            //        case 3:
            //            {

            //            }
            //            break;
            //        case 7:
            //            {

            //            }
            //            break;
            //    }


            //}

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
                            var response = EC.Delete<OrganizationIndex>( item.RecordId, d => d.Index( "organizationindex" ).Type( "organizationindex" ) );
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

    }
}

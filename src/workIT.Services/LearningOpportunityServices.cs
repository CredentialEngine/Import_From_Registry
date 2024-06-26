using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Web;

using workIT.Factories;
using workIT.Models;
using workIT.Models.Elastic;
using workIT.Models.Search;
using workIT.Utilities;

using ElasticHelper = workIT.Services.ElasticServices;
using EntityMgr = workIT.Factories.LearningOpportunityManager;
using ThisResource = workIT.Models.ProfileModels.LearningOpportunityProfile;
using WMA = workIT.Models.API;
using FAPI = workIT.Services.API;


namespace workIT.Services
{
    public class LearningOpportunityServices
    {
        static string thisClassName = "LearningOpportunityServices";
		static int ThisResourceEntityTypeId = CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE;
		#region import
		/// <summary>
		/// Handle import of record.
		/// On success, populate the entity_cache properties: resource detail and AgentRelationshipsForEntity
		/// </summary>
		/// <param name="resource"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public bool Import( ThisResource resource, ref SaveStatus status )
        {
            LoggingHelper.DoTrace( 7, thisClassName + ".Import - entered" );
            //do a get, and add to cache before updating
            if ( resource.Id > 0 )
            {
                //note could cause problems verifying after an import (i.e. shows cached version. Maybe remove from cache after completion.
                if ( UtilityManager.GetAppKeyValue( "learningOppCacheMinutes", 0 ) > 0 )
                {
                    //if ( System.DateTime.Now.Hour > 7 && System.DateTime.Now.Hour < 18 )
                    //    GetDetail( resource.Id, true );
                }
                string key = "lopp_" + resource.Id.ToString();
                ServiceHelper.ClearCacheEntity( key );
            }
            bool isValid = new EntityMgr().Save( resource, ref status );
            List<string> messages = new List<string>();
            if ( resource.Id > 0 )
            {
                if ( UtilityManager.GetAppKeyValue( "learningOppCacheMinutes", 0 ) > 0 )
                    CacheManager.RemoveItemFromCache( "lopp_", resource.Id );

				//start storing the finder api ready version
				var detail = FAPI.LearningOpportunityServices.GetDetailForAPI( resource.Id, true );
				var resourceDetail = JsonConvert.SerializeObject( detail, JsonHelper.GetJsonSettings( false ) );

				var statusMsg = "";
				var eManager = new EntityManager();
				if ( eManager.EntityCacheUpdateResourceDetail( resource.RowId, resourceDetail, ref statusMsg ) == 0 )
				{
					status.AddError( "EntityCacheUpdateResourceDetail Error: " + statusMsg );
				}
				//realistically, don't have to do this every time
				if ( eManager.EntityCacheUpdateAgentRelationshipsForLopp( resource.RowId.ToString(), ref statusMsg ) == false )
				{
					status.AddError( "EntityCacheUpdateAgentRelationshipsForLopp Error: " + statusMsg );
				}

				if ( UtilityManager.GetAppKeyValue( "delayingAllCacheUpdates", false ) == false )
                {
                    //update cache
                    ThreadPool.QueueUserWorkItem( UpdateCaches, resource );
                    //new CacheManager().PopulateEntityRelatedCaches( entity.RowId );
                    //add owning org to reindex queue

                }
                else
                {
					UpdatePendingReindex( resource, ref status );

                }
            }

            return isValid;
        }
		public void UpdatePendingReindex( ThisResource resource, ref SaveStatus status )
		{
			List<string> messages = new List<string>();
			new SearchPendingReindexManager().Add( ThisResourceEntityTypeId, resource.Id, 1, ref messages );
			int orgId = resource.OwningOrganizationId;

			if ( orgId == 0 )
			{
				var org = OrganizationManager.GetBasics( resource.PrimaryAgentUID, false );
				if ( org != null && org.Id > 0 )
					orgId = org.Id;

			}
			if ( orgId > 0 )
				new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, orgId, 1, ref messages );
			if ( messages.Count > 0 )
				status.AddWarningRange( messages );
		}
		static void UpdateCaches( Object entity )
        {
            if ( entity.GetType() != typeof( Models.ProfileModels.LearningOpportunityProfile ) )
                return;
            var document = ( entity as Models.ProfileModels.LearningOpportunityProfile );
            //EntityCache ec = new EntityCache()
            //{
            //	EntityTypeId = 7,
            //	EntityType = "LearningOpportunity",
            //	EntityStateId = document.EntityStateId,
            //	EntityUid = document.RowId,
            //	BaseId = document.Id,
            //	Description = document.Description,
            //	SubjectWebpage = document.SubjectWebpage,
            //	CTID = document.CTID,
            //	Created = document.Created,
            //	LastUpdated = document.LastUpdated,
            //	//ImageUrl = document.ImageUrl,
            //	Name = document.Name,
            //	OwningAgentUID = document.OwningAgentUid,
            //	OwningOrgId = document.OrganizationId
            //};

            //var statusMessage = "";
            //new EntityManager().EntityCacheSave( ec, ref statusMessage );


            new CacheManager().PopulateEntityRelatedCaches( document.RowId );
            //update Elastic
            List<string> messages = new List<string>();

            if ( Utilities.UtilityManager.GetAppKeyValue( "updatingElasticIndexImmediately", false ) )
                ElasticHelper.LearningOpp_UpdateIndex( document.Id );
            else
                new SearchPendingReindexManager().Add( 7, document.Id, 1, ref messages );

            new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, document.OwningOrganizationId, 1, ref messages );
        }
        #endregion
        #region retrievals
        public static ThisResource GetByCtid( string ctid )
        {
            ThisResource entity = new ThisResource();
            if ( string.IsNullOrWhiteSpace( ctid ) )
                return entity;

            return EntityMgr.GetByCtid( ctid );
        }
        public static ThisResource GetDetailByCtid( string ctid, bool skippingCache = false )
        {
            ThisResource entity = new ThisResource();
            if ( string.IsNullOrWhiteSpace( ctid ) )
                return entity;
            var learningOpportunity = EntityMgr.GetByCtid( ctid );

            return GetDetail( learningOpportunity.Id, skippingCache );
        }
        public static ThisResource GetBasic( int id, bool forEditView = false )
        {
            ThisResource entity = EntityMgr.GetBasic( id );

            return entity;
        }
        public static ThisResource GetDetail( int id, bool skippingCache = false, bool isAPIRequest = false )
        {
            WMA.DetailRequest request = new WMA.DetailRequest()
            {
                Id = id,
                SkippingCache = skippingCache,
                IsAPIRequest = isAPIRequest
            };
            return GetDetail( request );
        }
        public static ThisResource GetDetailForElastic( int id, bool isAPIRequest = false )
        {
            WMA.DetailRequest request = new WMA.DetailRequest()
            {
                Id = id,
                SkippingCache = true,
                IsAPIRequest = isAPIRequest
            };
            return GetDetail( request );
        }
        public static ThisResource GetDetail( WMA.DetailRequest request )
        {
            int cacheMinutes = UtilityManager.GetAppKeyValue( "learningOppCacheMinutes", 0 );
            DateTime maxTime = DateTime.Now.AddMinutes( cacheMinutes * -1 );
            string key = "lopp_" + request.Id.ToString();

            if ( request.SkippingCache == false
                && HttpRuntime.Cache[key] != null && cacheMinutes > 0 )
            {
                var cache = ( CachedLopp ) HttpRuntime.Cache[key];
                try
                {
                    if ( cache.lastUpdated > maxTime )
                    {
                        LoggingHelper.DoTrace( 6, string.Format( thisClassName + ".GetDetail === Using cached version of Lopp, Id: {0}, {1}", cache.Item.Id, cache.Item.Name ) );

                        return cache.Item;
                    }
                }
                catch ( Exception ex )
                {
                    LoggingHelper.DoTrace( 6, thisClassName + ".GetDetail === exception " + ex.Message );
                }
            }
            else
            {
                LoggingHelper.DoTrace( 8, thisClassName + string.Format( ".GetDetail === Retrieving full version of Lopp, Id: {0}", request.Id ) );
            }

            DateTime start = DateTime.Now;

            ThisResource entity = EntityMgr.GetForDetail( request.Id, request.IsAPIRequest );

            DateTime end = DateTime.Now;
            int elasped = ( end - start ).Seconds;
            //Cache the output if more than specific seconds,
            //NOTE need to be able to force it for imports
            //&& elasped > 2
            if ( key.Length > 0 && cacheMinutes > 0 && elasped > 7 )
            {
                var newCache = new CachedLopp()
                {
                    Item = entity,
                    lastUpdated = DateTime.Now
                };
                if ( HttpContext.Current != null )
                {
                    if ( HttpContext.Current.Cache[key] != null )
                    {
                        HttpRuntime.Cache.Remove( key );
                        HttpRuntime.Cache.Insert( key, newCache );

                        LoggingHelper.DoTrace( 5, string.Format( "==={0}.GetDetail $$$ Updating cached version of Lopp, Id: {1}, {2}", thisClassName, entity.Id, entity.Name ) );
                    }
                    else
                    {
                        LoggingHelper.DoTrace( 5, string.Format( "==={0}.GetDetail ****** Inserting new cached version of Lopp, Id: {1}, {2}", thisClassName, entity.Id, entity.Name ) );

                        System.Web.HttpRuntime.Cache.Insert( key, newCache, null, DateTime.Now.AddMinutes( cacheMinutes ), TimeSpan.Zero );
                    }
                }
            }
            else
            {
                LoggingHelper.DoTrace( 7, string.Format( "==={0}.GetDetail $$$$$$ skipping caching of Lopp, Id: {1}, {2}, elasped:{3}", thisClassName, entity.Id, entity.Name, elasped ) );
            }

            return entity;
        }

        #endregion


        #region Searches
        public List<string> Autocomplete( MainSearchInput data, int widgetId = 0 )
        {
            string where = "";
            int totalRows = 0;


            if ( UtilityManager.GetAppKeyValue( "usingElasticLearningOppSearch", false ) )
            {
                return new ElasticHelper().LearningOppAutoComplete( data, data.PageSize, ref totalRows );
            }
            else
            {
                string keywords = ServiceHelper.HandleApostrophes( data.Keywords );
                if ( keywords.IndexOf( "%" ) == -1 )
                    keywords = "%" + keywords.Trim() + "%";
                where = string.Format( " (base.name like '{0}') ", keywords );

                SetKeywordFilter( keywords, true, ref where );

                return EntityMgr.Autocomplete( where, 1, data.PageSize, ref totalRows );
            }
        }
        public List<string> AutocompleteOld( string keyword, int maxTerms, int widgetId = 0 )
        {
            string where = "";
            int totalRows = 0;

            string keywords = ServiceHelper.HandleApostrophes( keyword );
            if ( keywords.IndexOf( "%" ) == -1 )
                keywords = "%" + keywords.Trim() + "%";


            if ( UtilityManager.GetAppKeyValue( "usingElasticLearningOppSearch", false ) )
            {
                return new ElasticHelper().LearningOppAutoCompleteOld( keyword, maxTerms, ref totalRows );
            }
            else
            {
                where = string.Format( " (base.name like '{0}') ", keywords );
                SetKeywordFilter( keyword, true, ref where );

                return EntityMgr.Autocomplete( where, 1, maxTerms, ref totalRows );
            }
        }


        public List<ThisResource> Search( MainSearchInput data, ref int pTotalRows, string searchType = "learningopportunity" )
        {
            if ( UtilityManager.GetAppKeyValue( "usingElasticLearningOppSearch", false ) )
            {
                return new ElasticHelper().LearningOppSearch( data, ref pTotalRows );
            }
            else
            {
                return DoSearch( data, ref pTotalRows, searchType );
            }

        }

        private List<ThisResource> DoSearch( MainSearchInput data, ref int totalRows, string searchType = "learningopportunity" )
        {
            string where = "";
            List<string> competencies = new List<string>();

            //only target full entities
            where = " ( base.EntityStateId = 3 ) ";

            SearchServices.HandleCustomFilters( data, 61, ref where );

            SetKeywordFilter( data.Keywords, false, ref where );
            SearchServices.SetSubjectsFilter( data, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, ref where );

            SetPropertiesFilter( data, ref where );
            SearchServices.SetRolesFilter( data, ref where );
            SearchServices.SetBoundariesFilter( data, ref where );
            //SetBoundariesFilter( data, ref where );

            //CIP
            SetFrameworksFilter( data, ref where );

            //Competencies
            SetCompetenciesFilter( data, ref where, ref competencies );

            LoggingHelper.DoTrace( 5, "LearningOpportunityServices.Search(). Filter: " + where );
            return EntityMgr.Search( where, data.SortOrder, data.StartPage, data.PageSize, ref totalRows, ref competencies );
        }
        private static void SetKeywordFilter( string keywords, bool isBasic, ref string where )
        {
            if ( string.IsNullOrWhiteSpace( keywords ) )
                return;
            //trim trailing (org)
            if ( keywords.IndexOf( "('" ) > 0 )
                keywords = keywords.Substring( 0, keywords.IndexOf( "('" ) );

            //OR base.Description like '{0}' 
            string text = " (base.name like '{0}' OR base.SubjectWebpage like '{0}'  OR base.Organization like '{0}'  ) ";
            bool isCustomSearch = false;
            //use Entity.SearchIndex for all
            string indexFilter = " OR (base.Id in (SELECT c.id FROM [dbo].[Entity.SearchIndex] a inner join Entity b on a.EntityId = b.Id inner join LearningOpportunity c on b.EntityUid = c.RowId where (b.EntityTypeId = 3 AND ( a.TextValue like '{0}' OR a.[CodedNotation] like '{0}' ) ))) ";

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
            string subjectsEtc = " OR (base.Id in (SELECT c.id FROM [dbo].[Entity.Reference] a inner join Entity b on a.EntityId = b.Id inner join LearningOpportunity c on b.EntityUid = c.RowId where [CategoryId] in (34 ,35) and a.TextValue like '{0}' )) ";

            string frameworkItems = " OR (RowId in (SELECT EntityUid FROM [dbo].[Entity.FrameworkItemSummary] a where CategoryId= 23 and entityTypeId = 7 AND  a.title like '{0}' ) ) ";

            //string otherFrameworkItems = " OR (RowId in (SELECT EntityUid FROM [dbo].[Entity_Reference_Summary] a where  a.TextValue like '{0}' ) ) ";

            //string competencies = " OR ( base.Id in (SELECT LearningOpportunityId FROM [dbo].LearningOpportunity_Competency_Summary  where [Description] like '{0}' ) ) ";
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
                where = where + AND + string.Format( " ( " + text + indexFilter + " ) ", keywords );
            //where = where + AND + string.Format( " ( " + text + subjectsEtc + frameworkItems + otherFrameworkItems + competencies + " ) ", keywords );


        }

        private static void SetCompetenciesAutocompleteFilter( string keywords, ref string where )
        {
            List<string> competencies = new List<string>();
            MainSearchInput data = new MainSearchInput();
            MainSearchFilter filter = new MainSearchFilter() { Name = "competencies", CategoryId = 29 };
            filter.Items.Add( keywords );
            SetCompetenciesFilter( data, ref where, ref competencies );

        }
        private static void SetCompetenciesFilter( MainSearchInput data, ref string where, ref List<string> competencies )
        {
            string AND = "";
            string OR = "";
            string template = " ( base.Id in (SELECT distinct LearningOpportunityId FROM [dbo].LearningOpportunity_Competency_Summary  where AlignmentType = 'teaches' AND ({0}) ) )";
            string phraseTemplate = " ([Name] like '%{0}%' OR [Description] like '%{0}%') ";
            //

            //Updated to use FiltersV2
            string next = "";
            if ( where.Length > 0 )
                AND = " AND ";
            foreach ( var filter in data.FiltersV2.Where( m => m.Name == "competencies" ) )
            {
                var text = filter.AsText();

                //No idea what this is supposed to do
                try
                {
                    if ( text.IndexOf( " - " ) > -1 )
                    {
                        text = text.Substring( text.IndexOf( " -- " ) + 4 );
                    }
                }
                catch { }

                competencies.Add( text.Trim() );
                next += OR + string.Format( phraseTemplate, text.Trim() );
                OR = " OR ";

            }
            if ( !string.IsNullOrWhiteSpace( next ) )
            {
                where = where + AND + string.Format( template, next );
            }

        }
        //

        private static void SetPropertiesFilter( MainSearchInput data, ref string where )
        {

            string searchCategories = UtilityManager.GetAppKeyValue( "loppSearchCategories", "21,37," );
            SearchServices.SetPropertiesFilter( data, 1, searchCategories, ref where );

        }
        private static void SetFrameworksFilter( MainSearchInput data, ref string where )
        {
            string AND = "";
            //string codeTemplate2 = "  (base.Id in (SELECT c.id FROM [dbo].[Entity.FrameworkItemSummary] a inner join Entity b on a.EntityId = b.Id inner join LearningOpportunity c on b.EntityUid = c.RowId where [CategoryId] = {0} and ([FrameworkGroup] in ({1})  OR ([CodeId] in ({2}) )  ))  ) ";

            string codeTemplate = " (base.Id in (SELECT c.id FROM [dbo].[Entity_ReferenceFramework_Summary] a inner join Entity b on a.EntityId = b.Id inner join LearningOpportunity c on b.EntityUid = c.RowId where [CategoryId] = {0} and ([CodeGroup] in ({1})  OR ([ReferenceFrameworkId] in ({2}) )  )) ) ";
            //Updated to use FiltersV2
            string next = "";
            string groups = "";
            if ( where.Length > 0 )
                AND = " AND ";
            var targetCategoryID = 23;
            foreach ( var filter in data.FiltersV2.Where( m => m.Type == MainSearchFilterV2Types.FRAMEWORK ) )
            {
                var item = filter.AsCodeItem();
                var isTopLevel = filter.GetValueOrDefault<bool>( "IsTopLevel", false );
                if ( item.CategoryId == targetCategoryID )
                {
                    if ( isTopLevel )
                        groups += item.Id + ",";
                    else
                        next += item.Id + ",";
                }
            }
            if ( next.Length > 0 )
                next = next.Trim( ',' );
            else
                next = "''";
            if ( groups.Length > 0 )
                groups = groups.Trim( ',' );
            else
                groups = "''";
            if ( groups != "''" || next != "''" )
            {
                where = where + AND + string.Format( codeTemplate, targetCategoryID, groups, next );
            }

        }
        #endregion

        #region Elastic related
        /// <summary>
        /// temp for testing new approach for using detial data
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageNumber"></param>
        /// <param name="totalRows"></param>
        /// <returns></returns>
        public List<LearningOppIndex> LearningOpp_SearchForElastic( string filter, int pageSize, int pageNumber, ref int totalRows )
        {
            DateTime started = DateTime.Now;

            var list = ElasticManager.LearningOpp_SearchForElastic( filter, pageSize, pageNumber, ref totalRows );
            DateTime searchProcCompleted = DateTime.Now;
            var emgrDuration = searchProcCompleted.Subtract( started ).TotalSeconds;
            //this is displayed in elastic manager
            //LoggingHelper.DoTrace( 2, string.Format( thisClassName + ".LearningOpp_SearchForElastic - Call to elasticManager: page #{0}, took {1} seconds", pageNumber, emgrDuration ) );

            //reset for just this step
            var detailStarted = DateTime.Now;
            var cntr = 0;
            var currentId = 0;
            try
            {

                foreach ( var index in list )
                {
                    cntr++;
                    currentId = index.Id;
                    if ( cntr % 100 == 0 )
                        LoggingHelper.DoTrace( 2, string.Format( "      .... LearningOpp_SearchForElastic processing record: {0}", cntr ) );


                    //==========================
                    //TBD. just time initially if there are outcomes?

                    DateTime getDetailStarted = DateTime.Now;
                    //actually an alternate may be to populate from factory and then call API mapping method
                    var lopp = API.LearningOpportunityServices.GetDetailForAPI( index.Id, true );
                    DateTime getDetailCompleted = DateTime.Now;
                    var getDuration = getDetailCompleted.Subtract( getDetailStarted ).TotalSeconds;
                    
                    LoggingHelper.DoTrace( 7, string.Format( "LearningOpp_SearchForElastic - Page: {0}, #{1} Retrieved API Detail '{2}' record, in {3} seconds", pageNumber, cntr, currentId, getDuration ) );
                    //try to remove null properties before saving?
                    //var lopp2 = JsonConvert.SerializeObject( lopp, JsonHelper.GetJsonSettings( false ) );
                    //var lopp3= JsonConvert.DeserializeObject<ThisResource>( lopp2 );
                    //actually can we then just the string version here?
                    //      NO
                    //index.ResourceDetail = JObject.FromObject( lopp3 );
                    index.ResourceDetail = JObject.FromObject( lopp );

                   // index.ResourceDetail3 = JObject.FromObject( lopp ).ToString( Formatting.None );

                    //index.ResourceDetail2 = lopp;

                }
            }
            catch ( Exception ex )
            {
                var msg = BaseFactory.FormatExceptions( ex );
                LoggingHelper.DoTrace( 2, string.Format( "LearningOpp_SearchForElastic. Last Row: {0}, LoppId: {1} Exception: \r\n{2}", cntr, currentId, msg ) );
            }
            finally
            {
                DateTime completed = DateTime.Now;
                //
                var detailDuration = completed.Subtract( detailStarted ).TotalSeconds;
                var totalLoaded = (pageNumber -1 ) * pageSize + cntr;
                var totalDuration = completed.Subtract( started ).TotalSeconds;
                LoggingHelper.DoTrace( 2, $"LearningOpp_SearchForElastic - Page: {pageNumber}. Processed {cntr} records. Loaded: {totalLoaded} out of {totalRows}. Total duration: {totalDuration} seconds, emanagerDuration: {emgrDuration}, detailDuration: {detailDuration}" );
            }
            return list;
        }

        #endregion

    }


    public class CachedLopp
    {
        public CachedLopp()
        {
            lastUpdated = DateTime.Now;
        }
        public DateTime lastUpdated { get; set; }
        public ThisResource Item { get; set; }

    }
}

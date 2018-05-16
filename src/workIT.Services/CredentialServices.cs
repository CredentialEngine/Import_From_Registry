using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Web;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.Search;

using ThisEntity = workIT.Models.Common.Credential;
using ThisSearchEntity = workIT.Models.Common.CredentialSummary;
using EntityMgr = workIT.Factories.CredentialManager;

using workIT.Utilities;
using workIT.Factories;

namespace workIT.Services
{
    public class CredentialServices
    {
        static string thisClassName = "CredentialServices";
        ActivityServices activityMgr = new ActivityServices();

        public List<string> messages = new List<string>();

        public CredentialServices()
        {
        }

        #region import
        public static ThisEntity GetByCtid( string ctid )
        {
            ThisEntity entity = new ThisEntity();
            if ( string.IsNullOrWhiteSpace( ctid ) )
                return entity;

            return EntityMgr.GetByCtid( ctid );
        }

        public bool Import( ThisEntity entity, ref SaveStatus status )
        {
            bool isValid = new EntityMgr().Save( entity, ref status );
            List<string> messages = new List<string>();
            if ( entity.Id > 0 )
            {
                if ( UtilityManager.GetAppKeyValue( "delayingAllCacheUpdates", false ) == false )
                {
                    //update cache
                    new CacheManager().PopulateEntityRelatedCaches( entity.RowId );
                    //update Elastic
                    if ( Utilities.UtilityManager.GetAppKeyValue( "usingElasticCredentialSearch", false ) )
                        ElasticServices.UpdateCredentialIndex( entity.Id );
                    else
                    {
                        ElasticServices.UpdateCredentialIndex( entity.Id );

                        new SearchPendingReindexManager().Add( 1, entity.Id, 1, ref messages );
                        if ( messages.Count > 0 )
                            status.AddWarningRange( messages );
                    }
                } else
                {
                    new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL, entity.Id, 1, ref messages );
                    new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_ORGANIZATION, entity.OwningOrganizationId, 1, ref messages );
                    //check for embedded items
                    //has part
                    AddCredentialsToPendingReindex( entity.HasPartIds );
                    AddCredentialsToPendingReindex( entity.IsPartOfIds );

                    if ( messages.Count > 0 )
                        status.AddWarningRange( messages );
                }
            }

            return isValid;
        }
        public void AddCredentialsToPendingReindex(List<Credential> list)
        {
            List<string> messages = new List<string>();
            foreach (var item in list)
            {
                new SearchPendingReindexManager().Add( 1, item.Id, 1, ref messages );
            }
        }
        public void AddCredentialsToPendingReindex( List<int> list )
        {
            List<string> messages = new List<string>();
            foreach (var item in list)
            {
                new SearchPendingReindexManager().Add( 1, item, 1, ref messages );
            }
        }
        #endregion

        #region search 
        /// <summary>
        /// Credential autocomplete
        /// Needs to check authorization level for credential
        /// </summary>
        /// <param name="keyword"></param>
        /// <param name="maxTerms"></param>
        /// <returns></returns>
        public static List<string> Autocomplete( string keyword, int maxTerms = 25 )
        {
            int userId = 0;
            string where = "";
            int pTotalRows = 0;
            AppUser user = AccountServices.GetCurrentUser();
            if ( user != null && user.Id > 0 )
                userId = user.Id;
            SetAuthorizationFilter( user, ref where );

            SetKeywordFilter( keyword, true, ref where );

            if ( UtilityManager.GetAppKeyValue( "usingElasticCredentialSearch", false ) )
            {
                return ElasticServices.CredentialAutoComplete( keyword, maxTerms, ref pTotalRows );
            }
            else
            {
                return CredentialManager.Autocomplete( where, 1, maxTerms, ref pTotalRows );
            }
            // return new List<string>();
        }
        public static List<string> AutocompleteCompetencies( string keyword, int maxTerms = 25 )
        {
            int userId = 0;
            string where = "";

            AppUser user = AccountServices.GetCurrentUser();
            if ( user != null && user.Id > 0 )
                userId = user.Id;
            SetAuthorizationFilter( user, ref where );

            SetCompetenciesAutocompleteFilter( keyword, ref where );

            //return CredentialManager.Autocomplete( where, 1, maxTerms, userId, ref pTotalRows );
            return new List<string>();
        }


        /// <summary>
        /// Full credentials search
        /// </summary>
        /// <param name="data"></param>
        /// <param name="pTotalRows"></param>
        /// <returns></returns>
        public static List<ThisSearchEntity> Search( MainSearchInput data, ref int pTotalRows )
        {
            if ( UtilityManager.GetAppKeyValue( "usingElasticCredentialSearch", false ) || data.Elastic )
            {
                return ElasticServices.CredentialSearch( data, ref pTotalRows );
            }
            else
            {
                return DoSearch( data, ref pTotalRows );
            }
        }

        /// <summary>
        /// Full credentials search
        /// </summary>
        /// <param name="data"></param>
        /// <param name="pTotalRows"></param>
        /// <returns></returns>
        public static List<ThisSearchEntity> DoSearch( MainSearchInput data, ref int pTotalRows )
        {
            string where = "";
            DateTime start = DateTime.Now;
            //Stopwatch stopwatch = new Stopwatch();
            //stopwatch.Start();
            LoggingHelper.DoTrace( 6, string.Format( "===CredentialServices.Search === Started: {0}", start ) );
            int userId = 0;
            List<string> competencies = new List<string>();

            AppUser user = AccountServices.GetCurrentUser();
            if ( user != null && user.Id > 0 )
                userId = user.Id;
            //only target full entities
            where = " ( base.EntityStateId = 3 ) ";

            SetKeywordFilter( data.Keywords, false, ref where );
            where = where.Replace( "[USERID]", user.Id.ToString() );

            SearchServices.SetSubjectsFilter( data, CodesManager.ENTITY_TYPE_CREDENTIAL, ref where );

            //SetAuthorizationFilter( user, ref where );

            SearchServices.HandleCustomFilters( data, 58, ref where );

            //Should probably move this to its own method?
            string agentRoleTemplate = " ( id in (SELECT [CredentialId] FROM [dbo].[CredentialAgentRelationships_Summary] where RelationshipTypeId = {0} and OrgId = {1})) ";
            int roleId = 0;
            int orgId = 0;
            string AND = "";
            if ( where.Length > 0 )
                AND = " AND ";

            //Updated to use FilterV2
            foreach ( var filter in data.FiltersV2.Where( m => m.Name == "qualityAssuranceBy" ).ToList() )
            {
                roleId = filter.GetValueOrDefault( "RoleId", 0 );
                orgId = filter.GetValueOrDefault( "AgentId", 0 );
                where = where + AND + string.Format( agentRoleTemplate, roleId, orgId );
                AND = " AND ";
            }

            /* //Retained for reference
			foreach ( MainSearchFilter filter in data.Filters.Where( s => s.Name == "qualityAssuranceBy" ) )
			{
				if ( filter.Data.ContainsKey( "RoleId" ) )
					roleId = (int)filter.Data[ "RoleId" ];
				if ( filter.Data.ContainsKey( "AgentId" ) )
					orgId = ( int ) filter.Data[ "AgentId" ];
				where = where + AND + string.Format( agentRoleTemplate, roleId, orgId );
			}
			*/

            SetPropertiesFilter( data, ref where );

            SearchServices.SetRolesFilter( data, ref where );
            SearchServices.SetBoundariesFilter( data, ref where );
            //need to fix rowId

            //naics, ONET
            SetFrameworksFilter( data, ref where );
            //Competencies
            SetCompetenciesFilter( data, ref where, ref competencies );
            SetCredCategoryFilter( data, ref where ); //Not updated for FiltersV2 - I don't think we're using this anymore - NA 5/11/2017
            SetConnectionsFilter( data, ref where );

            TimeSpan timeDifference = start.Subtract( DateTime.Now );
            LoggingHelper.DoTrace( 5, thisClassName + string.Format( ".Search(). Filter: {0}, elapsed: {1} ", where, timeDifference.TotalSeconds ) );

            List<ThisSearchEntity> list = EntityMgr.Search( where, data.SortOrder, data.StartPage, data.PageSize, ref pTotalRows );

            //stopwatch.Stop();
            timeDifference = start.Subtract( DateTime.Now );
            LoggingHelper.DoTrace( 6, string.Format( "===CredentialServices.Search === Ended: {0}, Elapsed: {1}, Filter: {2}", DateTime.Now, timeDifference.TotalSeconds, where ) );
            return list;
        }

        private static void SetKeywordFilter( string keywords, bool isBasic, ref string where )
        {
            if ( string.IsNullOrWhiteSpace( keywords ) || string.IsNullOrWhiteSpace( keywords.Trim() ) )
                return;

            //trim trailing (org)
            if ( keywords.IndexOf( "('" ) > 0 )
                keywords = keywords.Substring( 0, keywords.IndexOf( "('" ) );

            //OR CreatorOrgs like '{0}' 
            bool isCustomSearch = false;
            //OR base.Description like '{0}'  
            string text = " (base.name like '{0}' OR base.SubjectWebpage like '{0}' OR base.AlternateName like '{0}' OR OwningOrganization like '{0}'  ) ";
            //for ctid, needs a valid ctid or guid
            if ( keywords.IndexOf( "ce-" ) > -1 && keywords.Length == 39 )
            {
                text = " ( CTID = '{0}' ) ";
                isCustomSearch = true;
            }
            else if ( keywords.IndexOf( "in (" ) > -1 )
            {
                text = " base.Id  " + keywords;
                isCustomSearch = true;
            }
            else if ( ServiceHelper.IsValidGuid( keywords ) )
            {
                text = " ( CTID = 'ce-{0}' ) ";
                isCustomSearch = true;
            }
            else if ( ServiceHelper.IsInteger( keywords ) )
            {
                text = " ( Id = '{0}' ) ";
                isCustomSearch = true;
            }
            else if ( keywords.ToLower() == "[hascredentialregistryid]" )
            {
                text = " ( len(Isnull(CredentialRegistryId,'') ) = 36 ) ";
                isCustomSearch = true;
            }

            //use Entity.SearchIndex for all
            string indexFilter = " OR (base.Id in (SELECT c.id FROM [dbo].[Entity.SearchIndex] a inner join Entity b on a.EntityId = b.Id inner join Credential c on b.EntityUid = c.RowId where (b.EntityTypeId = 1 AND ( a.TextValue like '{0}' OR a.[CodedNotation] like '{0}' ) ))) ";

            //removed 10,11 as part of the frameworkItemSummary
            //string keywordsFilter = " OR (base.Id in (SELECT c.id FROM [dbo].[Entity.Reference] a inner join Entity b on a.EntityId = b.Id inner join Credential c on b.EntityUid = c.RowId where [CategoryId] = 35 and a.TextValue like '{0}' )) ";

            //  string subjects = " OR  (base.EntityUid in (SELECT EntityUid FROM [Entity_Subjects] a where EntityTypeId = 1 AND a.Subject like '{0}' )) ";

            //string frameworkItems = " OR (EntityUid in (SELECT EntityUid FROM [dbo].[Entity.FrameworkItemSummary_ForCredentials] a where  a.title like '{0}' ) ) ";

            // string otherFrameworkItems = " OR (EntityUid in (SELECT EntityUid FROM [dbo].[Entity_Reference_Summary] a where  a.TextValue like '{0}' ) ) ";
            string AND = "";
            if ( where.Length > 0 )
                AND = " AND ";
            //
            keywords = ServiceHelper.HandleApostrophes( keywords );
            if ( keywords.IndexOf( "%" ) == -1 && !isCustomSearch )
            {
                keywords = SearchServices.SearchifyWord( keywords );
                //keywords = "%" + keywords.Trim() + "%";
                //keywords = keywords.Replace( "&", "%" ).Replace( " and ", "%" ).Replace( " in ", "%" ).Replace( " of ", "%" );
                //keywords = keywords.Replace( " - ", "%" );
                //keywords = keywords.Replace( " % ", "%" );
            }

            //skip url  OR base.Url like '{0}' 
            if ( isBasic || isCustomSearch )
                where = where + AND + string.Format( " ( " + text + " ) ", keywords );
            else
                where = where + AND + string.Format( " ( " + text + indexFilter + " ) ", keywords );
            //where = where + AND + string.Format( " ( " + text + keywordsFilter + subjects + frameworkItems + otherFrameworkItems + " ) ", keywords );

        }

        /// <summary>
        /// determine which results a user may view, and eventually edit
        /// </summary>
        /// <param name="data"></param>
        /// <param name="user"></param>
        /// <param name="where"></param>
        private static void SetAuthorizationFilter( AppUser user, ref string where )
        {
            //string AND = "";

            //if ( where.Length > 0 )
            //    AND = " AND ";
            //if ( user == null || user.Id == 0 )
            //{
            //	//public only records
            //	where = where + AND + string.Format( " (base.StatusId = {0}) ", CodesManager.ENTITY_STATUS_PUBLISHED );
            //	return;
            //}

            //if ( AccountServices.IsUserSiteStaff( user )
            //  || AccountServices.CanUserViewAllContent( user) )
            //{
            //	//can view all, edit all
            //	return;
            //}

            ////can only view where status is published, or associated with the org
            //where = where + AND + string.Format( "((base.StatusId = {0}) OR (base.Id in (SELECT cs.Id FROM [dbo].[Organization.Member] om inner join [Credential_Summary] cs on om.ParentOrgId = cs.ManagingOrgId where userId = {1}) ))", CodesManager.ENTITY_STATUS_PUBLISHED, user.Id );

        }
        private static void SetPropertiesFilter( MainSearchInput data, ref string where )
        {
            string AND = "";
            string searchCategories = UtilityManager.GetAppKeyValue( "credSearchCategories", "21,37," );
            SearchServices.SetPropertiesFilter( data, 1, searchCategories, ref where );
            //string template1 = " ( base.Id in ( SELECT  [EntityBaseId] FROM [dbo].[EntityProperty_Summary] where EntityTypeId= 1 AND [PropertyValueId] in ({0}))) ";
            //string template = " ( base.Id in ( SELECT  [EntityBaseId] FROM [dbo].[EntityProperty_Summary] where EntityTypeId= 1 AND {0} )) ";
            //string credTypes = " ( base.CredentialTypeId in ({0}) ) ";

            //string properyListTemplate = " ( [PropertyValueId] in ({0}) ) ";
            //string filterList = "";
            //int prevCategoryId = 0;
            ////Updated to use FiltersV2
            //string next = "";
            //string typesFilter = "";
            //if ( where.Length > 0 )
            //    AND = " AND ";

            //var credSearchCategories = new List<int>();
            //foreach ( var s in searchCategories.Split( ',' ) )
            //    if ( !string.IsNullOrEmpty( s ) )
            //        credSearchCategories.Add( int.Parse( s ) );

            //foreach ( var filter in data.FiltersV2.Where( m => m.Type == MainSearchFilterV2Types.CODE ).ToList() )
            //{
            //    var item = filter.AsCodeItem();
            //    //if ( searchCategories.Contains( item.CategoryId.ToString() ) )
            //    if ( credSearchCategories.Contains( item.CategoryId ) )
            //    {
            //        if ( item.CategoryId == 2 )
            //        {
            //            typesFilter += item.Id + ",";
            //        }
            //        else
            //        {
            //            //18-03-27 mp - these are all property values, so using an AND with multiple categories will always fail - removing prevCategoryId check
            //            //if (item.CategoryId != prevCategoryId)
            //            //{
            //            //    if (prevCategoryId > 0)
            //            //    {
            //            //        next = next.Trim(',');
            //            //        filterList += (filterList.Length > 0 ? " AND " : "") + string.Format(properyListTemplate, next);
            //            //    }
            //            //    prevCategoryId = item.CategoryId;
            //            //    next = "";
            //            //}
            //            next += item.Id + ",";
            //        }
            //    }
            //}
            //next = next.Trim( ',' );
            //typesFilter = typesFilter.Trim( ',' );
            //if ( !string.IsNullOrWhiteSpace( next ) )
            //{
            //    //where = where + AND + string.Format( template, next );
            //    filterList += ( filterList.Length > 0 ? " AND " : "" ) + string.Format( properyListTemplate, next );
            //    where = where + AND + string.Format( template, filterList );
            //    AND = " AND ";
            //}
            //if ( !string.IsNullOrWhiteSpace( typesFilter ) )
            //{
            //    where = where + AND + string.Format( credTypes, typesFilter );
            //    AND = " AND ";
            //}
            /* //Retained for reference
			foreach ( MainSearchFilter filter in data.Filters)
			{
				if ( searchCategories.IndexOf( filter.CategoryId.ToString() ) > -1 )
				{
					string next = "";
					if ( where.Length > 0 )
						AND = " AND ";
					foreach ( string item in filter.Items )
					{
						next += item + ",";
					}
					next = next.Trim( ',' );
					where = where + AND + string.Format( template, next );
				}
			}
			*/
        }
        private static void SetFrameworksFilter( MainSearchInput data, ref string where )
        {
            string AND = "";
            //string codeTemplate = " (base.Id in (SELECT c.id FROM [dbo].[Entity_ReferenceFramework_Summary] a inner join Entity b on a.EntityId = b.Id inner join Credential c on b.EntityUid = c.RowId where [CategoryId] = {0} and ([CodeGroup] in ({1})  OR ([Name] in ({2}) )  )) ) ";

            string codeTemplate = " (base.Id in (SELECT c.id FROM [dbo].[Entity_ReferenceFramework_Summary] a inner join Entity b on a.EntityId = b.Id inner join Credential c on b.EntityUid = c.RowId where [CategoryId] = {0} and ([CodeGroup] in ({1})  OR ([ReferenceFrameworkId] in ({2}) )  )) ) ";

            //string codeTemplate2 = "  (base.Id in (SELECT c.id FROM [dbo].[Entity.FrameworkItemSummary] a inner join Entity b on a.EntityId = b.Id inner join Credential c on b.EntityUid = c.RowId where [CategoryId] = {0} and ([CodeGroup] in ({1})  OR ([CodedNotation] in ({2}) )  )) ) ";

            //string codeTemplate1 = "  (base.Id in (SELECT c.id FROM [dbo].[Entity.FrameworkItemSummary] a inner join Entity b on a.EntityId = b.Id inner join Credential c on b.EntityUid = c.RowId where [CategoryId] = {0} and [CodeId] in ({1}))  ) ";

            //Updated to use FiltersV2
            string next = "";
            string groups = "";
            if ( where.Length > 0 )
                AND = " AND ";
            var categoryID = 0;
            foreach ( var filter in data.FiltersV2.Where( m => m.Type == MainSearchFilterV2Types.FRAMEWORK ) )
            {
                var item = filter.AsCodeItem();
                var isTopLevel = filter.GetValueOrDefault<bool>( "IsTopLevel", false );
                if ( item.CategoryId == 10 || item.Name == "industries" )
                {
                    categoryID = item.CategoryId;
                    if ( isTopLevel )
                        groups += item.Id + ",";
                    else
                    {
                        next += item.Id + ",";
                        // next += "'" + item.Code + "',";
                    }
                }
                else if ( item.CategoryId == 11 || item.Name == "occupations" )
                {
                    categoryID = item.CategoryId;
                    if ( isTopLevel )
                        groups += item.Id + ",";
                    else
                    {
                        next += item.Id + ",";
                        //can't use code here, need to use codednotation?
                        //next += "'" + item.Code + "',";
                    }
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
                where = where + AND + string.Format( codeTemplate, categoryID, groups, next );
            }

            /* //Retained for reference
			foreach ( MainSearchFilter filter in data.Filters.Where( s => s.CategoryId == 10 || s.CategoryId == 11 ) )
			{
				string next = "";
				if ( where.Length > 0 )
					AND = " AND ";
				foreach ( string item in filter.Items )
				{
					next += item + ",";
				}
				next = next.Trim( ',' );
				where = where + AND + string.Format( codeTemplate, filter.CategoryId, next );
			}
			*/
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
            string keyword = "";
            //just learning opps
            //string template = " ( base.Id in (SELECT distinct  CredentialId FROM [dbo].[ConditionProfile_Competencies_Summary]  where AlignmentType in ('teaches', 'assesses') AND ({0}) ) ) ";
            //learning opps and asmts:
            string template = " ( base.Id in (SELECT distinct  CredentialId FROM [dbo].[ConditionProfile_Competencies_Summary]  where ({0}) ) ) ";
            //
            string phraseTemplate = " ([Name] like '%{0}%' OR [TargetNodeDescription] like '%{0}%') ";
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
        private static void SetCredCategoryFilter( MainSearchInput data, ref string where )
        {
            string AND = "";
            //check for org category (credentially, or QA). Only valid if one item
            var qaSettings = data.GetFilterValues_Strings( "qualityAssurance" );
            if ( qaSettings.Count == 1 )
            {
                //ignore unless one filter
                string item = qaSettings[0];
                if ( where.Length > 0 )
                    AND = " AND ";
                if ( item == "includeNormal" ) //IsAQAOrganization = false
                    where = where + AND + " ( base.CredentialTypeSchema <> 'qualityAssurance') ";
                else if ( item == "includeQualityAssurance" )  //IsAQAOrganization = true
                    where = where + AND + " ( base.CredentialTypeSchema = 'qualityAssurance') ";
            }
        }

        public static void SetConnectionsFilter( MainSearchInput data, ref string where )
        {
            string AND = "";
            string OR = "";
            if ( where.Length > 0 )
                AND = " AND ";


            ////Should probably get this from the database
            Enumeration entity = CodesManager.GetCredentialsConditionProfileTypes();

            var validConnections = new List<string>();
            //{ 
            //	"requires", 
            //	"recommends", 
            //	"requiredFor", 
            //	"isRecommendedFor", 
            //	//"renewal", //Not a connection type
            //	"isAdvancedStandingFor", 
            //	"advancedStandingFrom", 
            //	"preparationFor", 
            //	"preparationFrom", 
            //	"isPartOf", 
            //	"hasPart"	
            //};
            //validConnections = validConnections.ConvertAll( m => m.ToLower() ); //Makes comparisons easier when combined with the .ToLower() below
            validConnections = entity.Items.Select( s => s.SchemaName.ToLower() ).ToList();

            string conditionTemplate = " {0}Count > 0 ";

            //Updated for FiltersV2
            string next = "";
            string condition = "";
            if ( where.Length > 0 )
                AND = " AND ";
            foreach ( var filter in data.FiltersV2.Where( m => m.Type == MainSearchFilterV2Types.CODE ) )
            {
                var item = filter.AsCodeItem();
                if ( item.CategoryId != 15 )
                {
                    continue;
                }

                //Prevent query hijack attacks
                if ( validConnections.Contains( item.SchemaName.ToLower() ) )
                {
                    condition = item.SchemaName;
                    next += OR + string.Format( conditionTemplate, condition );
                    OR = " OR ";
                }
            }
            next = next.Trim();
            next = next.Replace( "ceterms:", "" );
            if ( !string.IsNullOrWhiteSpace( next ) )
            {
                where = where + AND + "(" + next + ")";
            }

        }

        #endregion

        #region Retrievals

        /// <summary>
        /// Get a minimal credential - typically for a link, or need just basic properties
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static ThisEntity GetBasic( int credentialId )
        {
            return CredentialManager.GetBasic( credentialId );
        }

        /// <summary>
        /// Get a minimal credential - typically for a link, or need just basic properties
        /// </summary>
        /// <param name="rowId"></param>
        /// <returns></returns>
        //public static ThisEntity GetBasicCredentialAsLink( Guid rowId )
        //{
        //    return CredentialManager.GetBasic( rowId, false, true );
        //}
        //public static ThisEntity GetBasicCredential( Guid rowId )
        //{
        //    return CredentialManager.GetBasic( rowId, false, false );
        //}

        /// <summary>
        /// Get a credential for detailed display
        /// </summary>
        /// <param name="id"></param>
        /// <param name="user"></param>
        /// <param name="skippingCache">If true, do not use the cached version</param>
        /// <returns></returns>
        public static ThisEntity GetDetail( int id, bool skippingCache = false )
        {
            //
            string statusMessage = "";
            int cacheMinutes = UtilityManager.GetAppKeyValue( "credentialCacheMinutes", 0 );
            DateTime maxTime = DateTime.Now.AddMinutes( cacheMinutes * -1 );

            string key = "credential_" + id.ToString();

            if ( skippingCache == false
                && HttpRuntime.Cache[key] != null && cacheMinutes > 0 )
            {
                var cache = ( CachedCredential )HttpRuntime.Cache[key];
                try
                {
                    if ( cache.lastUpdated > maxTime )
                    {
                        LoggingHelper.DoTrace( 6, string.Format( "===CredentialServices.GetCredentialDetail === Using cached version of Credential, Id: {0}, {1}", cache.Item.Id, cache.Item.Name ) );

                        return cache.Item;
                    }
                }
                catch ( Exception ex )
                {
                    LoggingHelper.DoTrace( 6, "===CredentialServices.GetCredentialDetail === exception " + ex.Message );
                }
            }
            else
            {
                LoggingHelper.DoTrace( 8, string.Format( "****** CredentialServices.GetCredentialDetail === Retrieving full version of credential, Id: {0}", id ) );
            }

            DateTime start = DateTime.Now;

            CredentialRequest cr = new CredentialRequest();
            cr.IsDetailRequest();

            ThisEntity entity = CredentialManager.GetForDetail( id, cr );

            DateTime end = DateTime.Now;
            int elasped = ( end - start ).Seconds;
            //Cache the output if more than 3? seconds
            if ( key.Length > 0 && cacheMinutes > 0 && elasped > 3 )
            {
                var newCache = new CachedCredential()
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

                        LoggingHelper.DoTrace( 5, string.Format( "===CredentialServices.GetCredentialDetail $$$ Updating cached version of credential, Id: {0}, {1}", entity.Id, entity.Name ) );

                    }
                    else
                    {
                        LoggingHelper.DoTrace( 5, string.Format( "===CredentialServices.GetCredentialDetail ****** Inserting new cached version of credential, Id: {0}, {1}", entity.Id, entity.Name ) );

                        System.Web.HttpRuntime.Cache.Insert( key, newCache, null, DateTime.Now.AddHours( cacheMinutes ), TimeSpan.Zero );
                    }
                }
            }
            else
            {
                LoggingHelper.DoTrace( 7, string.Format( "===CredentialServices.GetCredentialDetail $$$$$$ skipping caching of credential, Id: {0}, {1}, elasped:{2}", entity.Id, entity.Name, elasped ) );
            }

            return entity;
        }

        /// <summary>
        /// Retrieve Credential for compare purposes
        /// - name, description, cred type, education level, 
        /// - industries, occupations
        /// - owner role
        /// - duration
        /// - estimated costs
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static ThisEntity GetCredentialForCompare( int id )
        {
            //not clear if checks necessary, as interface only allows selection of those to which the user has access.
            AppUser user = AccountServices.GetCurrentUser();

            LoggingHelper.DoTrace( 2, string.Format( "GetCredentialForCompare - using new compare get for cred: {0}", id ) );

            //================================================
            string statusMessage = "";
            string key = "credentialCompare_" + id.ToString();

            ThisEntity entity = new ThisEntity();

            if ( CacheManager.IsCredentialAvailableFromCache( id, key, ref entity ) )
            {
                //check if user can update the object
                string status = "";
                //if ( !CanUserUpdateCredential( id, user, ref status ) ) 
                //    entity.CanEditRecord = false;
                return entity;
            }

            CredentialRequest cr = new CredentialRequest();
            cr.IsCompareRequest();


            DateTime start = DateTime.Now;

            entity = CredentialManager.GetForCompare( id, cr );

            //if ( CanUserUpdateCredential( entity, user, ref statusMessage ) )
            //    entity.CanUserEditEntity = true;

            DateTime end = DateTime.Now;
            int elasped = ( end - start ).Seconds;
            if ( elasped > 1 )
                CacheManager.AddCredentialToCache( entity, key );

            return entity;
        } //

        //private void RemoveCredentialFromCache( int credentialId )
        //{
        //    CacheManager.RemoveItemFromCache( "credential", credentialId );
        //    CacheManager.RemoveItemFromCache( "credentialCompare", credentialId );
        //} //

        #endregion

        /*
		
		#region === add/update/delete =============
		
		/// <summary>
		/// Save a credential - vai new editor
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="user"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public bool Credential_Save( ThisEntity entity, AppUser user, ref string status )
		{
			//entity.IsNewVersion = true;
			return Credential_Update( entity, user, ref status );
		}

		public bool Credential_Update( ThisEntity entity, AppUser user, ref string status )
		{
			entity.LastUpdatedById = user.Id;
			LoggingHelper.DoTrace( 5, string.Format( thisClassName + ".Credential_Update. CredentialId: {0}, userId: {1}", entity.Id, entity.LastUpdatedById ) );
			Mgr mgr = new Mgr();
			bool valid = true;
			if ( !ValidateCredential( entity, false, ref messages ) )
			{
				status = string.Join( "<br/>", messages.ToArray() );
				return false;
			}
			try
			{
				if ( entity.ManagingOrgId == 0 )
					entity.ManagingOrgId = OrganizationManager.GetPrimaryOrganizationId( user.Id );

				valid = mgr.Update( entity, ref status );
				if ( valid )
				{
					ConsoleMessageHelper.SetConsoleInfoMessage( "Successfully Updated Credential" );
					activityMgr.AddActivity( "Credential", "Update", string.Format( "{0} updated credential (or parts of): {1}", user.FullName(), entity.Name ), user.Id, 0, entity.Id );

					//remove from cache
					//RemoveFromCache( "credential",entity.Id );
					RemoveCredentialFromCache( entity.Id );
					
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".Credential_Update" );
				valid = false;
				status = ex.Message;
			}

			return valid;
		}

		//public bool Credential_Delete( int credentialId, AppUser user )
		//{
		//	var isOK = false;
		//	var statusMessage = "";
		//	return Credential_Delete( credentialId, user,ref isOK, ref statusMessage );
		//}
		public bool Credential_Delete( int credentialId, AppUser user, ref string status )
		{
			Mgr mgr = new Mgr();
			bool valid = true;
			try
			{
				ThisEntity entity = new ThisEntity();
				if (CanUserUpdateCredential( credentialId, user, ref status, ref entity ) == false) 
				{
					status = "You do not have authorization to delete this credential";
					valid = false;
					return false;
				}
				valid = mgr.Credential_Delete( credentialId, user.Id, ref status );
				if ( valid )
				{
					activityMgr.AddActivity( "Credential", "Deactivate", string.Format( "{0} deactivated credential: {1} (id: {2})", user.FullName(), entity.Name, entity.Id ), user.Id, 0, credentialId );
					status = "";
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".Credential_Delete" );
				status = ex.Message;
				valid = false;
			}

			return valid;
		}
		
		#endregion

		

		#region Duration Profiles
		/// <summary>
		/// Get all Duration profiles for a parent
		/// </summary>
		/// <param name="parentId"></param>
		/// <returns></returns>
		//public static List<DurationProfile> DurationProfile_GetAll( Guid parentId )
		//{
		//	List<DurationProfile> list = DurationProfileManager.GetAll( parentId );
		//	return list;
		//}

		/// <summary>
		/// Get a Duration Profile By integer Id
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public static DurationProfile DurationProfile_Get( int id )
		{
			DurationProfile profile = DurationProfileManager.Get( id );
			return profile;
		}


		public bool DurationProfile_Update( DurationProfile entity, Guid contextParentUid, Guid contextMainUid, int userId, ref string statusMessage )
		{
			//LoggingHelper.DoTrace( 2, string.Format( "CredentialServices.DurationProfile_Update. contextParentUid: {0} contextMainUid: {1} ", contextParentUid.ToString(), contextMainUid.ToString() ) 				);

			List<String> messages = new List<string>();
			if ( entity == null || !BaseFactory.IsGuidValid( contextParentUid ) )
			{
				messages.Add( "Error - missing an identifier for the DurationProfile" );
				return false;
			}
			//validate credential and access
			//==> not just from a credential
			//ThisEntity credential = GetCredential()
			//if (CanUserUpdateCredential( contextMainUid, userId, ref statusMessage ) == false) 
			//{
			//	messages.Add( "Error - missing credential identifier" );
			//	return false;
			//}
			//CanUser update entity?
			MC.Entity e = EntityManager.GetEntity( contextParentUid );

			//remove this if properly passed from client
			//plus need to migrate to the use of EntityId
			//entity.ParentUid = parentUid;
			entity.EntityId = e.Id;
			entity.CreatedById = entity.LastUpdatedById = userId;

			//if an add, the new id will be returned in the entity
			bool isValid = new DurationProfileManager().DurationProfileUpdate( entity, userId, ref messages );
			statusMessage = string.Join( "<br/>", messages.ToArray() );
			return isValid;

		}

		public bool DurationProfile_Delete( int profileID, ref string status )
		{
			bool valid = false;
			AppUser user = AccountServices.GetCurrentUser();
			if ( user == null || user.Id == 0 )
			{
				status = "You must be logged and authorized to perform this action.";
				return false;
			}
			try
			{
				DurationProfile profile = DurationProfileManager.Get( profileID );
				//ensure has access

				valid = new DurationProfileManager().DurationProfile_Delete( profileID, ref status );
				if ( valid )
				{
					//if valid, status contains the cred name and id
					activityMgr.AddActivity( "DurationProfile", "Delete", string.Format( "{0} deleted {1}", user.FullName(), status ), user.Id, 0, profileID );
					status = "";
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".DurationProfile_Delete" );
				status = ex.Message;
				valid = false;
			}

			return valid;
		}

		#endregion

		
		#region Credential Revocation Profile
		public static RevocationProfile RevocationProfile_GetForEdit( int profileId,
				bool forEditView = true )
		{
			
			RevocationProfile profile = Entity_RevocationProfileManager.Get( profileId );

			return profile;
		}

		public bool RevocationProfile_Save( RevocationProfile entity, Guid credentialUid, string action, AppUser user, ref string status, bool isQuickCreate = false )
		{
			bool valid = true;
			status = "";
			List<string> messages = new List<string>();
			Entity_RevocationProfileManager mgr = new Entity_RevocationProfileManager();
			try
			{
				ThisEntity credential = GetBasicCredentialAsLink( credentialUid );

				int count = 0;
				//entity.IsNewVersion = true;
				if ( mgr.Save( entity, credential, user.Id, ref messages ) == false )
				{
					valid = false;
					status = string.Join( "<br/>", messages.ToArray() );
				}
				else 
				{
					if ( isQuickCreate )
					{
						status = "Created an initial Profile. Please provide a meaningful name, and fill out the remainder of the profile";
					}
					else
					{
						status = "Successful";
						activityMgr.AddActivity( "RevocationProfile", "Modify", string.Format( "{0} added/updated Revocation Profiles under credential: {1}, count:{2}", user.FullName(), credential.Name, count ), user.Id, 0, entity.Id );
					}
				}

			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".RevocationProfile_Save" );
				valid = false;
				status = ex.Message;
			}
			return valid;
		}
		/// <summary>
		/// Delete a revocation Profile ??????????????
		/// TODO - ensure current user has access to the credential
		/// </summary>
		/// <param name="credenialId"></param>
		/// <param name="profileId"></param>
		/// <param name="user"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public bool RevocationProfile_Delete( int credentialId, int profileId, AppUser user, ref string status )
		{
			bool valid = true;

			try
			{
				valid = new Entity_RevocationProfileManager().Delete( profileId, ref status );

				if ( valid )
				{
					//if valid, status contains the cred id, category, and codeId
					activityMgr.AddActivity( "Revocation Profile", "Delete", string.Format( "{0} deleted Revocation Profile: {1} from Credential: {2}", user.FullName(), profileId, credentialId ), user.Id, 0, profileId );
					status = "";
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".RevocationProfile_Delete" );
				status = ex.Message;
				valid = false;
			}

			return valid;
		}

		#endregion
		*/
    }

    public class CachedCredential
    {
        public CachedCredential()
        {
            lastUpdated = DateTime.Now;
        }
        public DateTime lastUpdated { get; set; }
        public Credential Item { get; set; }

    }
}

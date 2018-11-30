using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.Search;

using ThisEntity = workIT.Models.ProfileModels.AssessmentProfile;
using ThisSearcvhEntity = workIT.Models.ProfileModels.AssessmentProfile;
using EntityMgr = workIT.Factories.AssessmentManager;
using workIT.Utilities;
using workIT.Factories;

namespace workIT.Services
{
    public class AssessmentServices
    {
		static string thisClassName = "AssessmentServices";

		#region import

		public bool Import( ThisEntity entity, ref SaveStatus status )
        {
            //do a get, and add to cache before updating
            if ( entity.Id > 0 )
            {
                //need to force caching here
                var detail = GetDetail( entity.Id );
            }
            bool isValid = new EntityMgr().Save( entity, ref status );
            List<string> messages = new List<string>();
            if ( entity.Id > 0 )
            {
				CacheManager.RemoveItemFromCache( "asmt", entity.Id );

				if ( UtilityManager.GetAppKeyValue( "delayingAllCacheUpdates", false ) == false )
                {
                    //update cache
                    new CacheManager().PopulateEntityRelatedCaches( entity.RowId );
                    //update Elastic
                    if ( UtilityManager.GetAppKeyValue( "usingElasticAssessmentSearch", false ) )
                        ElasticServices.Assessment_UpdateIndex( entity.Id );
                    else
                    {
                        new SearchPendingReindexManager().Add( 3, entity.Id, 1, ref messages );
                        if ( messages.Count > 0 )
                            status.AddWarningRange( messages );
                    }
                }
                else
                {
                    new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, entity.Id, 1, ref messages );
                    new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_ORGANIZATION, entity.OwningOrganizationId, 1, ref messages );
                    if ( messages.Count > 0 )
                        status.AddWarningRange( messages );
                }
            }

            return isValid;
        }

        #endregion


        #region Searches 
        //	public static List<CodeItem> SearchAsCodeItem( string keyword, int startingPageNbr, int pageSize, ref int totalRows )
        //	{
        //		List<ThisEntity> list = Search( keyword, startingPageNbr, pageSize, ref totalRows );
        //		List<CodeItem> codes = new List<CodeItem>();
        //		foreach (ThisEntity item in list) 
        //		{
        //			codes.Add(new CodeItem() {
        //				Id = item.Id,
        //				Name = item.Name,
        //				Description = item.Description
        //			});
        //		}
        //		return codes;
        //}
        public static List<string> Autocomplete( string keyword, int maxTerms = 25, int widgetId = 0 )
        {

            string where = "";
            int totalRows = 0;

            //SetKeywordFilter( keyword, true, ref where );
            string keywords = ServiceHelper.HandleApostrophes( keyword );
            if ( keywords.IndexOf( "%" ) == -1 )
                keywords = "%" + keywords.Trim() + "%";
            where = string.Format( " (base.name like '{0}') ", keywords );


            if ( UtilityManager.GetAppKeyValue( "usingElasticAssessmentSearch", false ) )
            {
                return ElasticServices.AssessmentAutoComplete( keyword, maxTerms, ref totalRows, widgetId );
            }
            else
            {
                SetKeywordFilter( keyword, true, ref where );
                return EntityMgr.Autocomplete( where, 1, maxTerms, ref totalRows );
            }
        }
        //public static List<ThisEntity> Search( string keywords, int pageNumber, int pageSize, ref int totalRows )
        //{
        //	string pOrderBy = "";
        //	string filter = "";
        //	int userId = 0;
        //	AppUser user = AccountServices.GetCurrentUser();
        //	if ( user != null && user.Id > 0 )
        //		userId = user.Id;

        //	SetKeywordFilter( keywords, true, ref filter );
        //	//SetAuthorizationFilter( user, ref filter );

        //	return EntityMgr.Search( filter, pOrderBy, pageNumber, pageSize, ref totalRows );
        //}

        public static List<ThisEntity> Search( MainSearchInput data, ref int pTotalRows )
        {
            if ( UtilityManager.GetAppKeyValue( "usingElasticAssessmentSearch", false ) )
            {
                return ElasticServices.AssessmentSearch( data, ref pTotalRows );
            }
            else
            {
                return DoSearch( data, ref pTotalRows );
            }

        }
        public static List<ThisEntity> DoSearch( MainSearchInput data, ref int totalRows )
        {
            string where = "";
            List<string> competencies = new List<string>();
            int userId = 0;
            AppUser user = AccountServices.GetCurrentUser();
            if ( user != null && user.Id > 0 )
                userId = user.Id;

            //only target full entities
            where = " ( base.EntityStateId = 3 ) ";

            SetKeywordFilter( data.Keywords, false, ref where );

            SearchServices.SetSubjectsFilter( data, CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, ref where );

            SearchServices.HandleCustomFilters( data, 60, ref where );

            SetPropertiesFilter( data, ref where );
            SearchServices.SetRolesFilter( data, ref where );
            SearchServices.SetBoundariesFilter( data, ref where );
            //CIP
            SetFrameworksFilter( data, ref where );
            //Competencies
            SetCompetenciesFilter( data, ref where, ref competencies );

            LoggingHelper.DoTrace( 5, "AssessmentServices.Search(). Filter: " + where );
            return EntityMgr.Search( where, data.SortOrder, data.StartPage, data.PageSize, ref totalRows );
        }

        private static void SetKeywordFilter( string keywords, bool isBasic, ref string where )
        {
            if ( string.IsNullOrWhiteSpace( keywords ) )
                return;

            //trim trailing (org)
            if ( keywords.IndexOf( "('" ) > 0 )
                keywords = keywords.Substring( 0, keywords.IndexOf( "('" ) );

            //OR base.Description like '{0}'  
            string text = " (base.name like '{0}' OR base.SubjectWebpage like '{0}' OR base.Organization like '{0}'  ) ";

            bool isCustomSearch = false;
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
            //use Entity.SearchIndex for all
            string indexFilter = " OR (base.Id in (SELECT c.id FROM [dbo].[Entity.SearchIndex] a inner join Entity b on a.EntityId = b.Id inner join Assessment c on b.EntityUid = c.RowId where (b.EntityTypeId = 3 AND ( a.TextValue like '{0}' OR a.[CodedNotation] like '{0}' ) ))) ";

            string subjectsEtc = " OR (base.Id in (SELECT c.id FROM [dbo].[Entity.Reference] a inner join Entity b on a.EntityId = b.Id inner join Assessment c on b.EntityUid = c.RowId where [CategoryId] in (34 ,35) and a.TextValue like '{0}' )) ";

            string frameworkItems = " OR (RowId in (SELECT EntityUid FROM [dbo].[Entity.FrameworkItemSummary] a where CategoryId= 23 and entityTypeId = 3 AND  a.title like '{0}' ) ) ";

            string otherFrameworkItems = " OR (RowId in (SELECT EntityUid FROM [dbo].[Entity_Reference_Summary] a where  a.TextValue like '{0}' ) ) ";

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
            //where = where + AND + string.Format( " ( " + text + subjectsEtc + frameworkItems + otherFrameworkItems + " ) ", keywords );

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
            string template = " ( base.Id in (SELECT distinct  AssessmentId FROM [dbo].Assessment_Competency_Summary  where AlignmentType = 'assesses' AND ({0}) ) )";
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
            string AND = "";
            string searchCategories = UtilityManager.GetAppKeyValue( "asmtSearchCategories", "21,37," );
            SearchServices.SetPropertiesFilter( data, 1, searchCategories, ref where );
            //string template = " ( base.Id in ( SELECT  [EntityBaseId] FROM [dbo].[EntityProperty_Summary] where EntityTypeId= 3 AND {0} )) ";
            //string template1 = " ( base.Id in ( SELECT  [EntityBaseId] FROM [dbo].[EntityProperty_Summary] where EntityTypeId= 3 AND [PropertyValueId] in ({0}) )) ";
            //string properyListTemplate = " ( [PropertyValueId] in ({0}) ) ";
            //string filterList = "";
            //int prevCategoryId = 0;

            ////Updated to use FiltersV2
            //string next = "";
            //if (where.Length > 0)
            //    AND = " AND ";
            //foreach (var filter in data.FiltersV2.Where(m => m.Type == MainSearchFilterV2Types.CODE))
            //{
            //    var item = filter.AsCodeItem();
            //    if (searchCategories.Contains(item.CategoryId.ToString()))
            //    {
            //        //18-03-27 mp - these are all property values, so using an AND with multiple categories will always fail - removing prevCategoryId check
            //        //if (item.CategoryId != prevCategoryId)
            //        //{
            //        //    if (prevCategoryId > 0)
            //        //    {
            //        //        next = next.Trim(',');
            //        //        filterList += (filterList.Length > 0 ? " AND " : "") + string.Format(properyListTemplate, next);
            //        //    }
            //        //    prevCategoryId = item.CategoryId;
            //        //    next = "";
            //        //}
            //        next += item.Id + ",";
            //    }
            //}
            //next = next.Trim(',');
            //if (!string.IsNullOrWhiteSpace(next))
            //{
            //    //where = where + AND + string.Format( template, next );
            //    filterList += (filterList.Length > 0 ? " AND " : "") + string.Format(properyListTemplate, next);
            //    where = where + AND + string.Format(template, filterList);
            //}

        } //


        private static void SetFrameworksFilter( MainSearchInput data, ref string where )
        {
            string AND = "";
            string codeTemplate2 = "  (base.Id in (SELECT c.id FROM [dbo].[Entity.FrameworkItemSummary] a inner join Entity b on a.EntityId = b.Id inner join Assessment c on b.EntityUid = c.RowId where [CategoryId] = {0} and ([FrameworkGroup] in ({1})  OR ([CodeId] in ({2}) )  ))  ) ";
            string codeTemplate = " (base.Id in (SELECT c.id FROM [dbo].[Entity_ReferenceFramework_Summary] a inner join Entity b on a.EntityId = b.Id inner join Assessment c on b.EntityUid = c.RowId where [CategoryId] = {0} and ([CodeGroup] in ({1})  OR ([ReferenceFrameworkId] in ({2}) )  )) ) ";
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

        #region Retrievals
        public static ThisEntity GetByCtid( string ctid )
        {
            ThisEntity entity = new ThisEntity();
            if ( string.IsNullOrWhiteSpace( ctid ) )
                return entity;

            return EntityMgr.GetByCtid( ctid );
        }
        public static ThisEntity GetDetailByCtid( string ctid, bool skippingCache = false )
        {
            ThisEntity entity = new ThisEntity();
            if ( string.IsNullOrWhiteSpace( ctid ) )
                return entity;
            var assessment = EntityMgr.GetByCtid( ctid );

            return GetDetail( assessment.Id, skippingCache );
        }
        public static ThisEntity GetBasic( int id )
        {
            ThisEntity entity = EntityMgr.GetBasic( id );
            return entity;
        }
        public static ThisEntity GetDetail( int id , bool skippingCache = false )
        {
			int cacheMinutes = UtilityManager.GetAppKeyValue( "learningOppCacheMinutes", 0 );
			DateTime maxTime = DateTime.Now.AddMinutes( cacheMinutes * -1 );
			string key = "asmt_" + id.ToString();

			if ( skippingCache == false
				&& HttpRuntime.Cache[ key ] != null 
				&& cacheMinutes > 0 )
			{
				var cache = ( CachedAssessment )HttpRuntime.Cache[ key ];
				try
				{
					if ( cache.lastUpdated > maxTime )
					{
						LoggingHelper.DoTrace( 6, string.Format( thisClassName + ".GetDetail === Using cached version of Asmt, Id: {0}, {1}", cache.Item.Id, cache.Item.Name ) );
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
				LoggingHelper.DoTrace( 8, thisClassName + string.Format( ".GetDetail === Retrieving full version of Asmt, Id: {0}", id ) );
			}

			DateTime start = DateTime.Now;
			ThisEntity entity = EntityMgr.GetDetails( id);

			DateTime end = DateTime.Now;
			int elasped = ( end - start ).Seconds;
			//Cache the output if more than specific seconds,
			//NOTE need to be able to force it for imports
			//&& elasped > 2
			if ( key.Length > 0 && cacheMinutes > 0  )
			{
				var newCache = new CachedAssessment()
				{
					Item = entity,
					lastUpdated = DateTime.Now
				};
				if ( HttpContext.Current != null )
				{
					if ( HttpContext.Current.Cache[ key ] != null )
					{
						HttpRuntime.Cache.Remove( key );
						HttpRuntime.Cache.Insert( key, newCache );

						LoggingHelper.DoTrace( 6, string.Format( "==={0}.GetDetail $$$ Updating cached version of Asmt, Id: {1}, {2}", thisClassName, entity.Id, entity.Name ) );
					}
					else
					{
						LoggingHelper.DoTrace( 6, string.Format( "==={0}.GetDetail ****** Inserting new cached version of Asmt, Id: {1}, {2}", thisClassName, entity.Id, entity.Name ) );

						System.Web.HttpRuntime.Cache.Insert( key, newCache, null, DateTime.Now.AddHours( cacheMinutes ), TimeSpan.Zero );
					}
				}
			}
			else
			{
				LoggingHelper.DoTrace( 7, string.Format( "==={0}.GetDetail $$$$$$ skipping caching of Asmt, Id: {1}, {2}, elasped:{3}", thisClassName, entity.Id, entity.Name, elasped ) );
			}
			return entity;
        }



        #endregion

    }
	public class CachedAssessment
	{
		public CachedAssessment()
		{
			lastUpdated = DateTime.Now;
		}
		public DateTime lastUpdated { get; set; }
		public ThisEntity Item { get; set; }

	}
}

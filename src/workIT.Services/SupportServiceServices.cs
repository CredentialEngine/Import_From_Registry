using System;
using System.Collections.Generic;

using workIT.Factories;
using workIT.Models;
using workIT.Models.Common;
using workIT.Models.Search;
using workIT.Utilities;

using ElasticHelper = workIT.Services.ElasticServices;
using APIResourceServices = workIT.Services.API.SupportServiceServices;
using Newtonsoft.Json;
using ResourceManager = workIT.Factories.SupportServiceManager;
using ThisResource = workIT.Models.Common.SupportService;

namespace workIT.Services
{
    public class SupportServiceServices
    {
        static string thisClassName = "SupportServiceServices";
        public static string ThisEntityType = "SupportService";
        public static int ThisEntityTypeId = CodesManager.ENTITY_TYPE_SUPPORT_SERVICE;
        static string usingElasticSearch = "usingElasticSupportServiceSearch";

        #region import
        public static ThisResource GetMinimumByCtid( string ctid )
        {
            ThisResource resource = new ThisResource();
            if ( string.IsNullOrWhiteSpace( ctid ) )
                return resource;

            return ResourceManager.GetMinimumByCtid( ctid );
        }

        public bool Import( ThisResource resource, ref SaveStatus status )
        {
            LoggingHelper.DoTrace( BaseFactory.appMethodEntryTraceLevel, thisClassName + ".Import - entered" );

            bool isValid = new ResourceManager().Save( resource, ref status );
            if ( resource.Id > 0 )
            {
                List<string> messages = new List<string>();
				var apiDetail = APIResourceServices.GetDetailForAPI( resource.Id, true );
				if ( apiDetail != null && apiDetail.Meta_Id > 0 )
				{
					var resourceDetail = JsonConvert.SerializeObject( apiDetail, JsonHelper.GetJsonSettings( false ) );
					var statusMsg = "";
					if ( new EntityManager().EntityCacheUpdateResourceDetail( resource.CTID, resourceDetail, ref statusMsg ) == 0 )
					{
						status.AddError( statusMsg );
					}
				}
				if ( resource.OwningOrganizationId == 0 )
                {
                    resource = ResourceManager.GetBasic( resource.Id );
                }
                new SearchPendingReindexManager().Add( ThisEntityTypeId, resource.Id, 1, ref messages );
                new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, resource.PrimaryOrganizationId, 1, ref messages );
                if ( messages.Count > 0 )
                    status.AddWarningRange( messages );
            }

            return isValid;
        }
        #endregion

        public static ThisResource GetDetail( int id, bool skippingCache = false )
        {
            var entity = ResourceManager.GetForDetail( id );
            if ( entity.EntityStateId == 0 )
                return null;


            return entity;
        }
        public static ThisResource GetDetailByCtid( string ctid, bool skippingCache = false )
        {
            var entity = new ThisResource();
            if ( string.IsNullOrWhiteSpace( ctid ) )
                return entity;
            var resource = ResourceManager.GetMinimumByCtid( ctid );

            return GetDetail( resource.Id, skippingCache );
        }

        //
        public static List<ThisResource> GetResourceHasSupportServices( string searchType, int recordID, int maxRecords = 10 )
        {
            //get an entity type for the search type
            var entityTypeId = SearchServices.GetEntityTypeIdFromSearchType( searchType );
            return GetResourceHasSupportServices( entityTypeId, recordID, maxRecords );
        }

        //
        public static List<ThisResource> GetResourceHasSupportServices( int topParentTypeId, int topParentEntityBaseId, int maxRecords = 0 )
        {
            var list = new List<ThisResource>();
            Entity parent = EntityManager.GetEntity( topParentTypeId, topParentEntityBaseId );
            //Entity parent = EntityManager.GetEntity( parentUid );
            if ( parent == null || parent.Id == 0 )
            {
                return list;
            }

            try
            {
                list = Entity_HasSupportServiceManager.GetAll( parent, 1, maxRecords );
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, thisClassName + ".GetResourceHasSupportServices" );
            }
            return list;
        }//

        #region search
        public static List<string> Autocomplete( string keywords, int maxTerms, ref int totalRows )
        {
            MainSearchInput query = new MainSearchInput()
            {
                Keywords = keywords ?? "",
                StartPage = 1,
                PageSize = maxTerms,
            };
            string where = "";
            totalRows = 0;

            if ( UtilityManager.GetAppKeyValue( usingElasticSearch, true ) )
            {
                return ElasticHelper.GeneralAutoComplete( ThisEntityTypeId, ThisEntityType, query, maxTerms, ref totalRows );
            }
            else
            {
                keywords = ServiceHelper.HandleApostrophes( query.Keywords );
                if ( keywords.IndexOf( "%" ) == -1 )
                    keywords = "%" + keywords.Trim() + "%";
                where = string.Format( " (base.name like '{0}') ", keywords );

                SetKeywordFilter( keywords, true, ref where );
                return ResourceManager.Autocomplete( where, 1, maxTerms, ref totalRows );
            }
        }
        public static List<CommonSearchSummary> Search( MainSearchInput data, ref int pTotalRows )
        {
            if ( UtilityManager.GetAppKeyValue( usingElasticSearch, true ) )
            {
                return ElasticHelper.GeneralSearch( ThisEntityTypeId, ThisEntityType, data, ref pTotalRows );
            }
            else
            {
                var results = new List<CommonSearchSummary>();
                var list = DoSearch( data, ref pTotalRows );
                foreach ( var item in list )
                {
                    results.Add( new CommonSearchSummary()
                    {
                        Id = item.Id,
                        Name = item.Name,
                        Description = item.Description,
                        SubjectWebpage = item.SubjectWebpage,
                        PrimaryOrganizationName = item.PrimaryOrganizationName,
                        CTID = item.CTID,
                        EntityTypeId = ThisEntityTypeId,
                        EntityType = ThisEntityType
                    } );
                }
                return results;
            }

        }//

        public static List<ThisResource> DoSearch( MainSearchInput data, ref int totalRows )
        {
            string where = "";

            //only target full entities
            where = " ( base.EntityStateId = 3 ) ";
            //need to create a new category id for custom filters
            //SearchServices.HandleCustomFilters( data, 61, ref where );

            SetKeywordFilter( data.Keywords, false, ref where );

            //SetPropertiesFilter( data, ref where );
            SearchServices.SetRolesFilter( data, ref where );
            SearchServices.SetBoundariesFilter( data, ref where );

            LoggingHelper.DoTrace( 5, $"{thisClassName}.Search(). Filter: " + where );
            return ResourceManager.Search( where, data.SortOrder, data.StartPage, data.PageSize, ref totalRows );
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
    }
}

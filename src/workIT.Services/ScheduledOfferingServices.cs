using System;
using System.Collections.Generic;

using workIT.Factories;
using workIT.Models;
using workIT.Models.Common;
using workIT.Models.Search;
using workIT.Utilities;

using ElasticHelper = workIT.Services.ElasticServices;
using ResourceManager = workIT.Factories.ScheduledOfferingManager;
using ThisResource = workIT.Models.Common.ScheduledOffering;

namespace workIT.Services
{
    public class ScheduledOfferingServices
    {
        static string thisClassName = "ScheduledOfferingServices";
        public static string ThisEntityType = "ScheduledOffering";
        public static int ThisEntityTypeId = CodesManager.ENTITY_TYPE_SCHEDULED_OFFERING;
        #region import
        public static ThisResource GetByCtid( string ctid )
        {
            ThisResource resource = new ThisResource();
            if ( string.IsNullOrWhiteSpace( ctid ) )
                return resource;

            return ResourceManager.GetMinimumByCtid( ctid );
        }

        public bool Import( ThisResource resource, ref SaveStatus status )
        {
            LoggingHelper.DoTrace( 7, thisClassName + ".Import - entered" );

            bool isValid = new ResourceManager().Save( resource, ref status );
            List<string> messages = new List<string>();
            if ( resource.Id > 0 )
            {
                if (resource.OwningOrganizationId == 0)
                {
                    resource = ResourceManager.GetBasic( resource.Id );
                }
                new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_SCHEDULED_OFFERING, resource.Id, 1, ref messages );
                new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, resource.OwningOrganizationId, 1, ref messages );
                if ( messages.Count > 0 )
                    status.AddWarningRange( messages );
            }

            return isValid;
        }
        #endregion

        public static List<CommonSearchSummary> Search( MainSearchInput data, ref int pTotalRows )
        {
            if ( UtilityManager.GetAppKeyValue( "usingElasticScheduledOfferingSearch", true ) )
            {
                return ElasticHelper.GeneralSearch( ThisEntityTypeId, ThisEntityType, data, ref pTotalRows );
            }
            else
            {
                List<CommonSearchSummary> results = new List<CommonSearchSummary>();
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
            string text = " (base.name like '{0}' OR base.SubjectWebpage like '{0}'  OR base.OrganizationName like '{0}'  ) ";
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

        public static ThisResource GetDetail( int id, bool skippingCache = false )
        {
            ThisResource entity = ResourceManager.GetForDetail( id );
            if ( entity.EntityStateId == 0 )
                return null;


            return entity;
        }

        public static ThisResource GetDetailByCtid( string ctid, bool skippingCache = false )
        {
            ThisResource entity = new ThisResource();
            if ( string.IsNullOrWhiteSpace( ctid ) )
                return entity;
            var resource = ResourceManager.GetMinimumByCtid( ctid );

            return GetDetail( resource.Id, skippingCache );
        }

    }
}

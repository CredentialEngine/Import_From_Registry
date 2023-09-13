using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.Search;
using ElasticHelper = workIT.Services.ElasticServices;

using ThisResource = workIT.Models.Common.WorkRole;
using ResourceManager = workIT.Factories.WorkRoleManager;
using workIT.Utilities;
using workIT.Factories;

namespace workIT.Services
{
	public class WorkRoleServices
	{
        static string thisClassName = "WorkRoleServices";
        public static string ThisEntityType = "WorkRole";
        public static int ThisEntityTypeId = CodesManager.ENTITY_TYPE_WORKROLE_PROFILE;
        static string usingElasticSearch = "usingElasticWorkRoleSearch";

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
			bool isValid = new ResourceManager().Save( resource, ref status );
			if ( resource.Id > 0 )
            {
                List<string> messages = new List<string>();

                //TODO - will need to update related elastic indices
                new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_WORKROLE_PROFILE, resource.Id, 1, ref messages );

                //	NOTE: not sure if there is an organization
                new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, resource.PrimaryOrganizationId, 1, ref messages );
            }

            return isValid;
		}

        #endregion


        #region Retrieval 

        public static ThisResource GetDetail( int profileId )
        {
            var profile = ResourceManager.GetForDetail( profileId );

            return profile;
        }

        public static ThisResource GetDetailByCtid( string ctid, bool skippingCache = false )
        {
            ThisResource entity = new ThisResource();
            if ( string.IsNullOrWhiteSpace( ctid ) )
                return entity;
            var resource = ResourceManager.GetMinimumByCtid( ctid );

            return GetDetail( resource.Id );
        }

        #endregion


        #region search
        public static List<string> Autocomplete( MainSearchInput query, int maxTerms = 25 )
        {

            string where = "";
            int totalRows = 0;

            //if ( UtilityManager.GetAppKeyValue( usingElasticSearch, true ) )
            //{
            var keywords = query.Keywords;
            return ElasticHelper.GeneralAutoComplete( ThisEntityTypeId, ThisEntityType, query, maxTerms, ref totalRows );
            //}
            //else
            //{
            //    string keywords = ServiceHelper.HandleApostrophes( query.Keywords );
            //    if ( keywords.IndexOf( "%" ) == -1 )
            //        keywords = "%" + keywords.Trim() + "%";
            //    where = string.Format( " (base.name like '{0}') ", keywords );

            //    SetKeywordFilter( keywords, true, ref where );
            //    return ResourceManager.Autocomplete( where, 1, maxTerms, ref totalRows );
            //}
        }
        public static List<CommonSearchSummary> Search( MainSearchInput data, ref int pTotalRows )
        {
            if ( UtilityManager.GetAppKeyValue( usingElasticSearch, true ) )
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
            string text = " (base.name like '{0}' OR base.SubjectWebpage like '{0}'  OR base.PrimaryOrganizationName like '{0}'  ) ";
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

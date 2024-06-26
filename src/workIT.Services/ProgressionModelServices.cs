using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

using workIT.Factories;
using workIT.Models;
using workIT.Models.Common;
using workIT.Models.Search;
using workIT.Utilities;
using APIResourceServices = workIT.Services.API.ProgressionModelServices;
using ResourceManager = workIT.Factories.ProgressionModelManager;
using ThisResource = workIT.Models.Common.ProgressionModel;
using ThisResourceSummary = workIT.Models.Common.ProgressionModelSummary;
namespace workIT.Services
{
	public class ProgressionModelServices
	{
		string thisClassName = "ProgressionModelServices";
		ActivityServices activityMgr = new ActivityServices();
		public List<string> messages = new List<string>();
        public int EntityTypeId = CodesManager.ENTITY_TYPE_PROGRESSION_MODEL;
        #region import

        public bool Import( ThisResource input, ref SaveStatus status )
        {
            LoggingHelper.DoTrace( 5, thisClassName + "Import entered. " + input.Name );
            //do a get, and add to cache before updating
            if (input.Id > 0)
            {
                //note could cause problems verifying after an import (i.e. shows cached version. Maybe remove from cache after completion.
                //var detail = GetDetail( entity.Id );
            }
            bool isValid = new ResourceManager().Save( input, ref status, true );
            List<string> messages = new List<string>();
            if (input.Id > 0)
            {
				//populate entity.cache - need to do before elastic processes
				var detail = APIResourceServices.GetDetailForAPI( input.Id, true );
				if ( detail != null && detail.Meta_Id > 0 )
				{
					var resourceDetail = JsonConvert.SerializeObject( detail, JsonHelper.GetJsonSettings( false ) );
					var statusMsg = "";
					if ( new EntityManager().EntityCacheUpdateResourceDetail( input.CTID, resourceDetail, ref statusMsg ) == 0 )
					{
						status.AddError( statusMsg );
					}
				}

                new SearchPendingReindexManager().Add( EntityTypeId, input.Id, 1, ref messages );
                new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, input.OrganizationId, 1, ref messages );
                if (messages.Count > 0)
                    status.AddWarningRange( messages );
             

            }

            return isValid;
        }

        #endregion

        #region retrieval
        public static ProgressionModel GetByCtid( string ctid )
		{
			ProgressionModel entity = new ProgressionModel();
			entity = ResourceManager.GetByCtid( ctid );
			return entity;
		}

		public static ProgressionModel Get( int id )
		{
			ProgressionModel entity = new ProgressionModel();
			entity = ResourceManager.Get( id );
			return entity;
		}
		//

		public static string GetCTIDFromID( int id )
		{
			return ResourceManager.GetCTIDFromID( id );
		}
		//

		public static List<ThisResourceSummary> Autocomplete( string keywords, int maxRows, ref int totalRows )
		{

			string where = "";
			SetKeywordFilter( keywords, false, ref where );

			LoggingHelper.DoTrace( 7, "ProgressionModelServices.Autocomplete(). Filter: " + where );

			return ResourceManager.Search( where, "", 1, maxRows, ref totalRows );
	
		}

		public static List<ThisResourceSummary> Search( MainSearchInput data, ref int totalRows )
		{

			//if ( UtilityManager.GetAppKeyValue( "usingElasticProgressionModelSearch", false ) )
			//{
			//	//return ElasticServices.ProgressionModelSearch( data, ref totalRows );
			//}
			//else
			{

				string where = "";
				List<string> messages = new List<string>();
				List<string> competencies = new List<string>();
				//int userId = 0;
				//AppUser user = AccountServices.GetCurrentUser();
				//if ( user != null && user.Id > 0 )
				//	userId = user.Id;
				//only target records with a ctid
				//21-09-14 mp - all will have ctids at this point
				where = "";//" (len(Isnull(base.Ctid,'')) = 39) ";

				SetKeywordFilter( data.Keywords, false, ref where );
				//SearchServices.SetLanguageFilter( data, EntityTypeId, ref where );

				////
				//SearchServices.SetAuthorizationFilter( user, "ProgressionModel_Summary", ref where );

				//SearchServices.HandleCustomFilters( data, 60, ref where );
				//

				//can this be replaced by following
				SearchServices.SetRolesFilter( data, ref where );

				//owned/offered
				//SearchServices.SetOrgRolesFilter( data, 3, ref where );
				//probably N/A
				SearchServices.SetBoundariesFilter( data, ref where );

				LoggingHelper.DoTrace( 5, "ProgressionModelServices.Search(). Filter: " + where );

				return ResourceManager.Search( where, data.SortOrder, data.StartPage, data.PageSize, ref totalRows );
			}
		}

		private static void SetKeywordFilter( string keywords, bool isBasic, ref string where )
		{
			if ( string.IsNullOrWhiteSpace( keywords ) || string.IsNullOrWhiteSpace( keywords.Trim() ) )
				return;


			//trim trailing (org)
			if ( keywords.IndexOf( "('" ) > 0 )
				keywords = keywords.Substring( 0, keywords.IndexOf( "('" ) );

			//OR base.Description like '{0}'  
			var text = " (base.name like '{0}' OR base.OrganizationName like '{0}' OR base.Description like '{0}' ) ";

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
			else if ( ServiceHelper.IsInteger( keywords ) )
			{
				text = " ( Id = '{0}' ) ";
				isCustomSearch = true;
			}
			else if ( keywords.ToLower().IndexOf( "orgid:" ) == 0 )
			{
				string[] parts = keywords.Split( ':' );
				if ( parts.Count() > 1 )
				{
					if ( ServiceHelper.IsInteger( parts[ 1 ] ) )
					{
						text = string.Format( " ( OwningOrganizationId={0} ) ", parts[ 1 ].Trim() );
						isCustomSearch = true;
					}
				}
			}


			string AND = "";
			if ( where.Length > 0 )
				AND = " AND ";

			keywords = ServiceHelper.HandleApostrophes( keywords );
			if ( keywords.IndexOf( "%" ) == -1 && !isCustomSearch )
			{
				keywords = SearchServices.SearchifyWord( keywords, false );
			}

			//skip url  OR base.Url like '{0}' 
			if ( isBasic || isCustomSearch )
			{
				//if ( !includingFrameworkItemsInKeywordSearch )
				//	where = where + AND + string.Format( " ( " + text + " ) ", keywords );
				//else
				where = where + AND + string.Format( " ( " + text + " ) ", keywords );
			}
			else
			{
				//if ( using_EntityIndexSearch )
				//	where = where + AND + string.Format( " ( " + text + indexFilter + " ) ", keywords );
				//else
				where = where + AND + string.Format( " ( " + text + " ) ", keywords );
			}

		}

		/*
		public static List<CompetencyFrameworkSummary> Search( MainSearchInput data, ref int pTotalRows )
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
						SourceUrl = item.SourceUrl,
						OrganizationName = item.PrimaryOrganizationName,
						CTID = item.CTID
						//EntityTypeId = EntityTypeId,
						//EntityType = "ProgressionModel"
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
			//SearchServices.SetSubjectsFilter( data, EntityTypeId, ref where );

			//SetPropertiesFilter( data, ref where );
			SearchServices.SetRolesFilter( data, ref where );
			SearchServices.SetBoundariesFilter( data, ref where );

			LoggingHelper.DoTrace( 6, "CompetencyFrameworkServices.DoFrameworksSearch(). Filter: " + where );
			//return ResourceManager.Search( where, data.SortOrder, data.StartPage, data.PageSize, ref totalRows );
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


		*/
		#endregion
	}
}

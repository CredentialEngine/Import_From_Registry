using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;

using workIT.Utilities;
using workIT.Models;
using workIT.Models.Node;
using workIT.Models.Common;
using MCD=workIT.Models.API;
using ME = workIT.Models.Elastic;
using workIT.Models.Search;
using ElasticHelper = workIT.Services.ElasticServices;

using ThisEntity = workIT.Models.Common.Organization;
using EntityMgr = workIT.Factories.OrganizationManager;
using CM = workIT.Models.Common;
using Mgr = workIT.Factories.OrganizationManager;
using workIT.Factories;

namespace workIT.Services
{
	public class OrganizationServices
	{
		static string thisClassName = "OrganizationServices";

		#region import
		/// <summary>
		/// Get by CTID - will return pending records
		/// </summary>
		/// <param name="ctid"></param>
		/// <returns></returns>
		public static ThisEntity GetSummaryByCtid( string ctid )
		{
			ThisEntity entity = new ThisEntity();
			if ( string.IsNullOrWhiteSpace( ctid ) )
				return entity;
			return EntityMgr.GetSummaryByCtid( ctid );
		}



		public bool Import( ThisEntity entity, ref SaveStatus status )
		{
			//do a get, and add to cache before updating
			if ( entity.Id > 0 )
			{
				if ( UtilityManager.GetAppKeyValue( "organizationCacheMinutes", 0 ) > 0 )
				{
					if ( System.DateTime.Now.Hour > 7 && System.DateTime.Now.Hour < 18 )
						GetDetail( entity.Id );
				}
			}
			bool isValid = new EntityMgr().Save( entity, ref status );
			List<string> messages = new List<string>();
			if ( entity.Id > 0 )
			{
				if ( UtilityManager.GetAppKeyValue( "organizationCacheMinutes", 0 ) > 0 )
					CacheManager.RemoveItemFromCache( "organization", entity.Id );

				if ( UtilityManager.GetAppKeyValue( "delayingAllCacheUpdates", false ) == false )
				{
					//update cache
					ThreadPool.QueueUserWorkItem( UpdateCaches, entity );

				}
				else
				{
					new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_ORGANIZATION, entity.Id, 1, ref messages );
					//add all credential in a verification profile
					if ( entity.VerificationServiceProfiles != null && entity.VerificationServiceProfiles.Count > 0 )
					{
						foreach ( var profile in entity.VerificationServiceProfiles )
						{
							if ( profile.TargetCredential != null
								&& profile.TargetCredential.Count > 0 )
							{
								new CredentialServices().AddCredentialsToPendingReindex( profile.TargetCredential );
							}
						}
					}
					//20-11-20 mp re: QA performed
					//				- may have to reindex all orgs etc that have QA performed by a QA org!!!
					if ( messages.Count > 0 )
						status.AddWarningRange( messages );
				}
			}

			return isValid;
		}
		static void UpdateCaches( Object entity )
		{
			if ( entity.GetType() != typeof( Models.Common.Organization ) )
				return;
			var document = ( entity as Models.Common.Organization );
			EntityCache ec = new EntityCache()
			{
				EntityTypeId = 2,
				EntityType = "Organization",
				EntityStateId = document.EntityStateId,
				EntityUid = document.RowId,
				BaseId = document.Id,
				Description = document.Description,
				SubjectWebpage = document.SubjectWebpage,
				CTID = document.CTID,
				Created = document.Created,
				LastUpdated = document.LastUpdated,
				ImageUrl = document.Image,
				Name = document.Name,
				OwningOrgId = document.OrganizationId
			};
			var statusMessage = "";
			new EntityManager().EntityCacheSave( ec, ref statusMessage );


			new CacheManager().PopulateEntityRelatedCaches( document.RowId );
			//may need to update elastic for creds, etc
			List<string> messages = new List<string>();
			//update Elastic
			if ( Utilities.UtilityManager.GetAppKeyValue( "updatingElasticIndexImmediately", false ) )
				ElasticHelper.Organization_UpdateIndex( document.Id );
			else
				new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_ORGANIZATION, document.Id, 1, ref messages );

		}
		public static ThisEntity GetBySubjectWebpage( string swp )
		{
			ThisEntity entity = new ThisEntity();
			if ( string.IsNullOrWhiteSpace( swp ) )
				return entity;
			return EntityMgr.GetBySubjectWebpage( swp );
		}
		#endregion

		#region Search
		public static List<CM.Organization> MicroSearch( MicroSearchInputV2 query, int pageNumber, int pageSize, ref int pTotalRows )
		{
			string pOrderBy = "";
			string filter = "";
			int userId = 0;
			string keywords = query.GetFilterValueString( "Keywords" );
			string orgMbrs = query.GetFilterValueString( "OrgFilters" );

			//user is used to determine if can edit results
			AppUser user = new AppUser();
			user = AccountServices.GetCurrentUser();
			if ( user != null && user.Id > 0 )
				userId = user.Id;
			//this is an option on micro searches to only target orgs associated to the user
			//if ( orgMbrs == "myOrgs" )
			//    SetAuthorizationFilter( user, ref filter, true, true );
			//else
			//    SetAuthorizationFilter( user, ref filter, true, false );

			SetKeywordFilter( keywords, true, ref filter );

			return Mgr.Search( filter, pOrderBy, pageNumber, pageSize, ref pTotalRows );
		}
		public static List<object> Autocomplete( string keyword = "", int maxTerms = 25, int widgetId = 0 )
		{
			int userId = 0;
			string where = "";
			int totalRows = 0;
			AppUser user = AccountServices.GetCurrentUser();
			if ( user != null && user.Id > 0 )
				userId = user.Id;
			//SetAuthorizationFilter( user, ref where );


			if ( UtilityManager.GetAppKeyValue( "usingElasticOrganizationSearch", false ) )
			{
				return ElasticHelper.OrganizationAutoComplete( keyword, maxTerms, ref totalRows );
			}
			else
			{
				SetKeywordFilter( keyword, true, ref where );
				//string keywords = ServiceHelper.HandleApostrophes( keyword );
				//if ( keywords.IndexOf( "%" ) == -1 )
				//	keywords = "%" + keywords.Trim() + "%";
				//where = string.Format( " (base.name like '{0}') ", keywords );

				return EntityMgr.Autocomplete( where, 1, maxTerms, userId, ref totalRows );
			}
		}
		public static List<MicroProfile> GetMicroProfile( List<int> organizationIDs )
		{

			List<MicroProfile> list = new List<MicroProfile>();

			if ( organizationIDs != null && organizationIDs.Count() > 0 )
			{
				string orgList = "";
				foreach ( var item in organizationIDs )
				{
					orgList += item + ",";
				}
				string filter = string.Format( " base.Id in ({0})", orgList.Trim( ',' ) );
				int pTotalRows = 0;
				List<OrganizationSummary> orgs = OrganizationManager.MainSearch( filter, "", 1, 500, ref pTotalRows );
				if ( orgs != null )
				{
					foreach ( var item in orgs )
					{
						list.Add( new MicroProfile() { Id = item.Id, Name = item.Name, RowId = item.RowId } );
					}
				}
			}

			return list;
		}

		public static List<OrganizationSummary> Search( MainSearchInput data, ref int pTotalRows )
		{
			if ( UtilityManager.GetAppKeyValue( "usingElasticOrganizationSearch", false ) )
			{
				return ElasticHelper.OrganizationSearch( data, ref pTotalRows );
			}
			else
			{
				return DoSearch( data, ref pTotalRows );
			}
		}
		/// <summary>
		/// Main search
		/// </summary>
		/// <param name="data"></param>
		/// <param name="pTotalRows"></param>
		/// <returns></returns>
		public static List<OrganizationSummary> DoSearch( MainSearchInput data, ref int pTotalRows )
		{
			string where = "";
			int userId = 0;

			AppUser user = AccountServices.GetCurrentUser();
			if ( user != null && user.Id > 0 )
				userId = user.Id;

			//only target full entities
			where = " ( base.EntityStateId = 3 ) ";

			SetKeywordFilter( data.Keywords, true, ref where );

			//SetAuthorizationFilter( user, ref where );
			SearchServices.HandleCustomFilters( data, 59, ref where );

			SetPropertiesFilter( data, ref where );
			SearchServices.SetRolesFilter( data, ref where );

			SetBoundariesFilter( data, ref where );
			SetFrameworksFilter( data, ref where );

			SetOrgServicesFilter( data, ref where );

			//check for org category (credentially, or QA). Only valid if one item
			//SetOrgCategoryFilter( data, ref where ); //Not updated - I'm not sure we're still using this. - NA 5/12/2017

			LoggingHelper.DoTrace( 5, thisClassName + ".Search(). Filter: " + where );
			return EntityMgr.MainSearch( where, data.SortOrder, data.StartPage, data.PageSize, ref pTotalRows );
		}

		private static void SetBoundariesFilter( MainSearchInput data, ref string where )
		{
			string AND = "";
			if ( where.Length > 0 )
				AND = " AND ";
			string template = " ( base.Id in ( SELECT  [EntityBaseId] FROM [dbo].[Entity_AddressSummary] where EntityTypeId = 2 AND  [Longitude] < {0} and [Longitude] > {1} and [Latitude] < {2} and [Latitude] > {3} ) ) ";

			var boundaries = SearchServices.GetBoundaries( data, "bounds" );
			if ( boundaries.IsDefined )
			{
				where = where + AND + string.Format( template, boundaries.East, boundaries.West, boundaries.North, boundaries.South );
			}
		}
		/// <summary>
		/// determine which results a user may view, and eventually edit
		/// </summary>
		/// <param name="data"></param>
		/// <param name="user"></param>
		/// <param name="where"></param>

		private static void SetPropertiesFilter( MainSearchInput data, ref string where )
		{
			string searchCategories = UtilityManager.GetAppKeyValue( "orgSearchCategories", "7,8,9,30," );
			SearchServices.SetPropertiesFilter( data, 1, searchCategories, ref where );

		}
		private static void SetFrameworksFilter( MainSearchInput data, ref string where )
		{
			string AND = "";
			//string codeTemplate2 = "  (base.Id in (SELECT c.id FROM [dbo].[Entity.FrameworkItemSummary] a inner join Entity b on a.EntityId = b.Id inner join Organization c on b.EntityUid = c.RowId where [CategoryId] = {0} and ([CodeGroup] in ({1})  OR ([CodeId] in ({2}) )  )) ) ";

			string codeTemplate = " (base.Id in (SELECT c.id FROM [dbo].[Entity_ReferenceFramework_Summary] a inner join Entity b on a.EntityId = b.Id inner join Organization c on b.EntityUid = c.RowId where [CategoryId] = {0} and ([CodeGroup] in ({1})  OR ([ReferenceFrameworkId] in ({2}) )  )) ) ";

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
				if ( item.CategoryId == 10 || item.CategoryId == 11 )
				{
					categoryID = item.CategoryId;
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
		private static void SetKeywordFilter( string keywords, bool isBasic, ref string where )
		{
			if ( string.IsNullOrWhiteSpace( keywords ) )
				return;
			//OR base.Description like '{0}'  
			string text = " (base.name like '{0}'  OR base.SubjectWebpage like '{0}' OR base.id in ( select EntityBaseId from Organization_AlternatesNames where TextValue like '{0}') ) ";

			string orgDepts = "( base.Id in (SELECT o.Id FROM dbo.Entity e INNER JOIN dbo.[Entity.AgentRelationship] ear ON e.Id = ear.EntityId INNER JOIN dbo.Organization o ON e.EntityUid = o.RowId WHERE ear.RelationshipTypeId = {0} AND o.StatusId < 4) )";
			bool isCustomSearch = false;
			//use Entity.SearchIndex for all
			string indexFilter = " OR (base.Id in (SELECT c.id FROM [dbo].[Entity.SearchIndex] a inner join Entity b on a.EntityId = b.Id inner join Organization c on b.EntityUid = c.RowId where (b.EntityTypeId = 2 AND ( a.TextValue like '{0}' OR a.[CodedNotation] like '{0}' ) ))) ";

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
			else if ( keywords.ToLower() == "has subsidiary" )
			{
				text = string.Format( orgDepts, 21 );
				isCustomSearch = true;
			}
			else if ( keywords.ToLower() == "has department" )
			{
				text = string.Format( orgDepts, 20 );
				isCustomSearch = true;
			}
			string AND = "";
			if ( where.Length > 0 )
				AND = " AND ";

			keywords = ServiceHelper.HandleApostrophes( keywords );
			if ( keywords.IndexOf( "%" ) == -1 && !isCustomSearch )
			{
				keywords = SearchServices.SearchifyWord( keywords );
				//keywords = "%" + keywords.Trim() + "%";
				//keywords = keywords.Replace( "&", "%" ).Replace( " and ", "%" ).Replace( " in ", "%" ).Replace( " of ", "%" );
				//keywords = keywords.Replace( " - ", "%" );
				//keywords = keywords.Replace( " % ", "%" );
			}

			//same for now, but will chg
			if ( isBasic || isCustomSearch )
				where = where + AND + string.Format( " ( " + text + " ) ", keywords );
			else
				where = where + AND + string.Format( " ( " + text + indexFilter + " ) ", keywords );
		}

		/// <summary>
		/// Note these are now properties!!!
		/// Changed the view to use the properties view, and proper category
		/// </summary>
		/// <param name="data"></param>
		/// <param name="where"></param>
		private static void SetOrgServicesFilter( MainSearchInput data, ref string where )
		{
			string AND = "";
			string template = " ( base.Id in ( SELECT  [OrganizationId] FROM [dbo].[Organization.ServiceSummary]  where [CodeId] in ({0}))) ";
			//don't really need categoryId - yet

			//Updated to use FiltersV2
			string next = "";
			if ( where.Length > 0 )
				AND = " AND ";
			foreach ( var filter in data.FiltersV2.Where( m => m.Type == MainSearchFilterV2Types.CODE ) )
			{
				var item = filter.AsCodeItem();
				if ( item.CategoryId == 6 )
				{
					next += item.Id + ",";
				}
			}
			next = next.Trim( ',' );
			if ( !string.IsNullOrWhiteSpace( next ) )
			{
				where = where + AND + string.Format( template, next );
			}

		}
		private static void SetOrgCategoryFilter( MainSearchInput data, ref string where )
		{
			string AND = "";
			//check for org category (credentially, or QA). Only valid if one item
			var qaSettings = data.GetFilterValues_Strings( "qualityAssurance" );
			if ( qaSettings.Count == 1 )
			{
				//ignore unless one filter
				string item = qaSettings[ 0 ];
				if ( where.Length > 0 )
					AND = " AND ";
				if ( item == "includeNormal" ) //IsAQAOrganization = false
					where = where + AND + " ([IsAQAOrganization] = 0 OR [CredentialCount] > 0) ";
				else if ( item == "includeQualityAssurance" )  //IsAQAOrganization = true
					where = where + AND + " ([IsAQAOrganization] = 1) ";
			}
		}
		#endregion
		
		public static CM.Organization GetBasic( int id )
        {
            CM.Organization entity = Mgr.GetForSummary( id );
            return entity;

        }
        public static CM.Organization GetForSummaryWithRoles( int id )
        {
            return Mgr.GetForSummary( id, true );
		}
		
        public static CM.Organization GetDetail( int id, bool skippingCache = false )
        {
            int cacheMinutes = UtilityManager.GetAppKeyValue( "organizationCacheMinutes", 0 );
            DateTime maxTime = DateTime.Now.AddMinutes( cacheMinutes * -1 );

            string key = "organization_" + id.ToString();

            if ( skippingCache == false
                && HttpRuntime.Cache[ key ] != null && cacheMinutes > 0 )
            {
                var cache = new CachedOrganization();
                try
                {
					cache = ( CachedOrganization )HttpRuntime.Cache[ key ];
                    if ( cache.lastUpdated > maxTime )
                    {
                        LoggingHelper.DoTrace( 6, string.Format( "===OrganizationServices.GetDetail === Using cached version of Organization, Id: {0}, {1}", cache.Item.Id, cache.Item.Name ) );

                        return cache.Item;
                    }
                }
                catch ( Exception ex )
                {
                    LoggingHelper.DoTrace( 6, thisClassName + ".GetDetail. Get OrganizationCache === exception " + ex.Message );
                }
            }
            else
            {
                LoggingHelper.DoTrace( 8, thisClassName + string.Format( ".GetDetail === Retrieving full version of Organization, Id: {0}", id ) );
            }

            DateTime start = DateTime.Now;

            CM.Organization entity = Mgr.GetDetail( id );

            DateTime end = DateTime.Now;
            int elasped = ( end - start ).Seconds;
			//Cache the output if more than specific seconds,
			//NOTE need to be able to force it for imports
			//&& elasped > 2
			if ( key.Length > 0 && cacheMinutes > 0 )
			{
				try
				{
					var newCache = new CachedOrganization()
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

							LoggingHelper.DoTrace( 5, string.Format( "===OrganizationServices.GetDetail $$$ Updating cached version of Organization, Id: {0}, {1}", entity.Id, entity.Name ) );

						}
						else
						{
							LoggingHelper.DoTrace( 5, string.Format( "===OrganizationServices.GetDetail ****** Inserting new cached version of Organization, Id: {0}, {1}", entity.Id, entity.Name ) );

							System.Web.HttpRuntime.Cache.Insert( key, newCache, null, DateTime.Now.AddMinutes( cacheMinutes ), TimeSpan.Zero );
						}
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.DoTrace( 6, thisClassName + ".GetDetail. Updating OrganizationCache === exception " + ex.Message );
				}
			}
			else
            {
                LoggingHelper.DoTrace( 7, string.Format( "===OrganizationServices.GetDetail $$$$$$ skipping caching of Organization, Id: {0}, {1}, elasped:{2}", entity.Id, entity.Name, elasped ) );
            }

            return entity;
        }


		public static ThisEntity GetDetailByCtid( string ctid, bool skippingCache = false )
		{
			ThisEntity entity = new ThisEntity();
			if ( string.IsNullOrWhiteSpace( ctid ) )
				return entity;
			//21-03-06 mp - there is too much extra work doing this (would be OK if just the entity)
			var organization = EntityMgr.GetSummaryByCtid( ctid );
			return GetDetail( organization.Id, skippingCache );
		}
	}
    public class CachedOrganization
    {
        public CachedOrganization()
        {
            lastUpdated = DateTime.Now;
        }
        public DateTime lastUpdated { get; set; }
        public Organization Item { get; set; }

    }
}

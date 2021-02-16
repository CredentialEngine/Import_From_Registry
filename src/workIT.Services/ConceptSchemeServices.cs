using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Caching;
using System.Text;
using System.Threading;
using System.Web;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

using workIT.Factories;
using Manager = workIT.Factories.ConceptSchemeManager;
using workIT.Models;
using workIT.Models.Common;
using workIT.Models.Elastic;
using ElasticHelper = workIT.Services.ElasticServices;

using workIT.Models.Helpers.CompetencyFrameworkHelpers;
using workIT.Models.Search;
using workIT.Utilities;

using ThisEntity = workIT.Models.Common.ConceptScheme;
namespace workIT.Services
{
	public class ConceptSchemeServices
	{
		string thisClassName = "ConceptSchemeServices";
		ActivityServices activityMgr = new ActivityServices();
		public List<string> messages = new List<string>();

		#region import

		public bool Import( ThisEntity entity, ref SaveStatus status )
		{
			LoggingHelper.DoTrace( 5, thisClassName + "Import entered. " + entity.Name );
			//do a get, and add to cache before updating
			if ( entity.Id > 0 )
			{
				//note could cause problems verifying after an import (i.e. shows cached version. Maybe remove from cache after completion.
				//var detail = GetDetail( entity.Id );
			}
			bool isValid = new Manager().Save( entity, ref status, true );
			List<string> messages = new List<string>();
			if ( entity.Id > 0 )
			{
				if ( UtilityManager.GetAppKeyValue( "delayingAllCacheUpdates", false ) == false )
				{
					//update cache - not applicable yet
					//new CacheManager().PopulateEntityRelatedCaches( entity.RowId );
					//update Elastic
					if ( Utilities.UtilityManager.GetAppKeyValue( "updatingElasticIndexImmediately", false ) )
						ElasticHelper.CompetencyFramework_UpdateIndex( entity.Id );
					else
					{
						new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CONCEPT_SCHEME, entity.Id, 1, ref messages );
						if ( messages.Count > 0 )
							status.AddWarningRange( messages );
					}
					new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_ORGANIZATION, entity.OrganizationId, 1, ref messages );
				}
				else
				{
					new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CONCEPT_SCHEME, entity.Id, 1, ref messages );
					new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_ORGANIZATION, entity.OrganizationId, 1, ref messages );
					if ( messages.Count > 0 )
						status.AddWarningRange( messages );
				}
				//no caching needed yet
				//CacheManager.RemoveItemFromCache( "cframework", entity.Id );
			}

			return isValid;
		}

		#endregion

		#region retrieval
		public static ConceptScheme GetByCtid( string ctid )
		{
			ConceptScheme entity = new ConceptScheme();
			entity = Manager.GetByCtid( ctid );
			return entity;
		}

		public static ConceptScheme Get( int id )
		{
			ConceptScheme entity = new ConceptScheme();
			entity = Manager.Get( id );
			return entity;
		}
		//
		public static List<ConceptSchemeSummary> Search( MainSearchInput data, ref int totalRows )
		{
			string where = "";
			List<string> messages = new List<string>();
			List<string> competencies = new List<string>();
			//int userId = 0;
			//AppUser user = AccountServices.GetCurrentUser();
			//if ( user != null && user.Id > 0 )
			//	userId = user.Id;
			//only target records with a ctid
			where = " (len(Isnull(base.Ctid,'')) = 39) ";

			SetKeywordFilter( data.Keywords, false, ref where );
			//SearchServices.SetLanguageFilter( data, CodesManager.ENTITY_TYPE_CONCEPT_SCHEME, ref where );

			////
			//SearchServices.SetAuthorizationFilter( user, "ConceptScheme_Summary", ref where );

			//SearchServices.HandleCustomFilters( data, 60, ref where );
			//

			//can this be replaced by following
			SearchServices.SetRolesFilter( data, ref where );

			//owned/offered
			//SearchServices.SetOrgRolesFilter( data, 3, ref where );
			//probably N/A
			SearchServices.SetBoundariesFilter( data, ref where );

			LoggingHelper.DoTrace( 5, "ConceptSchemeServices.Search(). Filter: " + where );

			return Manager.Search( where, data.SortOrder, data.StartPage, data.PageSize, ref totalRows );
		}

		private static void SetKeywordFilter( string keywords, bool isBasic, ref string where )
		{
			if ( string.IsNullOrWhiteSpace( keywords ) || string.IsNullOrWhiteSpace( keywords.Trim() ) )
				return;


			//trim trailing (org)
			if ( keywords.IndexOf( "('" ) > 0 )
				keywords = keywords.Substring( 0, keywords.IndexOf( "('" ) );

			//OR base.Description like '{0}'  
			var text = " (base.name like '{0}' OR base.Organization like '{0}' OR base.Description like '{0}' ) ";

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
				keywords = SearchServices.SearchifyWord( keywords );
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
						//EntityTypeId = CodesManager.ENTITY_TYPE_CONCEPT_SCHEME,
						//EntityType = "ConceptScheme"
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
			//SearchServices.SetSubjectsFilter( data, CodesManager.ENTITY_TYPE_CONCEPT_SCHEME, ref where );

			//SetPropertiesFilter( data, ref where );
			SearchServices.SetRolesFilter( data, ref where );
			SearchServices.SetBoundariesFilter( data, ref where );

			LoggingHelper.DoTrace( 6, "CompetencyFrameworkServices.DoFrameworksSearch(). Filter: " + where );
			//return Manager.Search( where, data.SortOrder, data.StartPage, data.PageSize, ref totalRows );
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

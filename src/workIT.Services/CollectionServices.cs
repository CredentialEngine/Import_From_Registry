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
using workIT.Models;
using workIT.Models.Common;
using workIT.Models.Helpers;
using workIT.Models.Elastic;
using ElasticHelper = workIT.Services.ElasticServices;

using workIT.Models.Search;
using workIT.Utilities;

using EntityMgr = workIT.Factories.CollectionManager;
using MPM = workIT.Models.ProfileModels;
using ThisEntity = workIT.Models.Common.Collection;
using ThisEntitySummary = workIT.Models.Common.Collection;

namespace workIT.Services
{
	public class CollectionServices
	{
		string thisClassName = "CollectionServices";
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
			bool isValid = new EntityMgr().Save( entity, ref status );
			List<string> messages = new List<string>();
			if ( entity.Id > 0 )
			{
				if ( UtilityManager.GetAppKeyValue( "delayingAllCacheUpdates", false ) == false )
				{
					//update cache - not applicable yet
					//update Elastic
					if ( Utilities.UtilityManager.GetAppKeyValue( "updatingElasticIndexImmediately", false ) )
						ElasticHelper.General_UpdateIndexForTVP( entity.Id );
					else
					{
						new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_COLLECTION, entity.Id, 1, ref messages );
						if ( messages.Count > 0 )
							status.AddWarningRange( messages );
					}
					new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, entity.OrganizationId, 1, ref messages );
				}
				else
				{
					new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_COLLECTION, entity.Id, 1, ref messages );
					new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, entity.OrganizationId, 1, ref messages );
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
		public static ThisEntity GetByCtid( string ctid )
		{
			ThisEntity entity = new ThisEntity();
			entity = CollectionManager.GetByCTID( ctid );
			return entity;
		}

		public static ThisEntity Get( int id )
		{
			ThisEntity entity = new ThisEntity();
			entity = CollectionManager.Get( id );
			return entity;
		}
		//

		//public static string GetCTIDFromID( int id )
		//{
		//	return CollectionManager.GetCTIDFromID( id );
		//}
		//

		public static List<CommonSearchSummary> CollectionSearch( MainSearchInput data, ref int pTotalRows )
		{
			if ( UtilityManager.GetAppKeyValue( "usingElasticCollectionSearch", true ) )
			{
				return ElasticHelper.GeneralSearch( CodesManager.ENTITY_TYPE_COLLECTION, "Collection", data, ref pTotalRows );
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
						FriendlyName = BaseFactory.FormatFriendlyTitle( item.Name),
						Description = item.Description,
						SubjectWebpage = item.SubjectWebpage, //?.Count > 0 ? item.SubjectWebpage[0] : "",
						PrimaryOrganizationName = item.PrimaryOrganizationName,
						PrimaryOrganizationFriendlyName = BaseFactory.FormatFriendlyTitle( item.PrimaryOrganizationName),
						PrimaryOrganizationId = item.PrimaryOrganizationId,
						CTID = item.CTID,
						ResultNumber = item.ResultNumber,
						Created = item.Created,
						LastUpdated = item.LastUpdated,
						EntityTypeId = CodesManager.ENTITY_TYPE_COLLECTION,
						EntityType = "Collection"
					} );
				}
				return results;
			}

		}//

		public static List<ThisEntity> DoSearch( MainSearchInput data, ref int totalRows )
		{
			string where = "";
			var output = new List<CommonSearchSummary>();
			//only target full entities
			where = " ( base.EntityStateId = 3 ) ";

			var idsList = new List<int>();
			foreach ( var filter in data.FiltersV2.Where( m => m.Name == "potentialresults" ).ToList() )
			{
				idsList.AddRange( JsonConvert.DeserializeObject<List<int>>( filter.CustomJSON ) );
			}
			if ( idsList.Any() )
			{
				var potentialQuery = string.Format( "(base.Id in ({0})) ", string.Join( ",", idsList ));
			}

			//need to create a new category id for custom filters
			//SearchServices.HandleCustomFilters( data, 61, ref where );

			SetKeywordFilter( data.Keywords, false, ref where );
			//SearchServices.SetSubjectsFilter( data, CodesManager.ENTITY_TYPE_COMPETENCY_FRAMEWORK, ref where );

			//SetPropertiesFilter( data, ref where );
			SearchServices.SetRolesFilter( data, ref where );
			//org addresses will be added to elastic
			SearchServices.SetBoundariesFilter( data, ref where );

			LoggingHelper.DoTrace( 6, "CollectionServices.Search(). Filter: " + where );
			//TODO 
			return EntityMgr.Search( where, data.SortOrder, data.StartPage, data.PageSize, ref totalRows );

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
			string text = " (base.name like '{0}'  OR base.OrganizationName like '{0}'  ) ";
			bool isCustomSearch = false;
			List<string> messages = new List<string>();
			//use Entity.SearchIndex for all
			//string indexFilter = " OR (base.Id in (SELECT c.id FROM [dbo].[Entity.SearchIndex] a inner join Entity b on a.EntityId = b.Id inner join TransferValue c on b.EntityUid = c.RowId where (b.EntityTypeId = 3 AND ( a.TextValue like '{0}' OR a.[CodedNotation] like '{0}' ) ))) ";

			//for ctid, needs a valid ctid or guid
			//if ( keywords.IndexOf( "ce-" ) > -1 && keywords.Length == 39 )
			//{
			//	text = " ( CTID = '{0}' ) ";
			//	isCustomSearch = true;
			//}
			//else 
			if ( ServiceHelper.IsValidCtid( keywords, ref messages ) )
			{
				//for this context assume owned by -OrganizationCTID
				//OR may want to hide this, as there will be the agentRelationship check
				//text = string.Format( " ( base.OrganizationCTID = '{0}' ) ", keywords );
				//isCustomSearch = true;
				keywords = "";

			}
			else if ( ServiceHelper.IsValidGuid( keywords ) )
			{
				text =string.Format( " ( CTID = 'ce-{0}' ) ", keywords);
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
				keywords = SearchServices.SearchifyWord( keywords, false );
			}

			//skip url  OR base.Url like '{0}' 
			if ( isBasic || isCustomSearch )
				where = where + AND + string.Format( " ( " + text + " ) ", keywords );
			else
				where = where + AND + string.Format( " ( " + text + " ) ", keywords );

		}
		#endregion
		#region Methods using registry search index


		//
		#endregion


	}
}

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

using workIT.Factories;
using workIT.Models;
using workIT.Models.Common;


using workIT.Models.Search;
using workIT.Utilities;

using EntityMgr = workIT.Factories.TransferValueProfileManager;
using MP = workIT.Models.Common;
using ME = workIT.Models.Elastic;
using ThisEntity = workIT.Models.Common.TransferValueProfile;

namespace workIT.Services
{
	public class TransferValueServices
	{
		string thisClassName = "TransferValueServices";
		ActivityServices activityMgr = new ActivityServices();
		public List<string> messages = new List<string>();


		#region import

		public bool Import( ThisEntity entity, ref SaveStatus status )
		{

			bool isValid = new EntityMgr().Save( entity, ref status );
			List<string> messages = new List<string>();
			if ( entity.Id > 0 )
			{

				if ( UtilityManager.GetAppKeyValue( "delayingAllCacheUpdates", false ) == false )
				{
					//update cache - not applicable yet

					//update Elastic
					if ( Utilities.UtilityManager.GetAppKeyValue( "updatingElasticIndexImmediately", false ) )
					{
						//ElasticServices.TransferValueProfile_UpdateIndex( entity.Id );
					}
					else
					{
						new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, entity.Id, 1, ref messages );
						if ( messages.Count > 0 )
							status.AddWarningRange( messages );
					}
					//also update related org
					new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_ORGANIZATION, entity.OwningOrganizationId, 1, ref messages );
				}
				else
				{
					new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, entity.Id, 1, ref messages );
					if ( entity.OwningOrganizationId > 0 )
						new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_ORGANIZATION, entity.OwningOrganizationId, 1, ref messages );
					if ( messages.Count > 0 )
						status.AddWarningRange( messages );
				}
				//no caching needed yet
				//CacheManager.RemoveItemFromCache( "cframework", entity.Id );
			}

			return isValid;
		}
		public static MP.TransferValueProfile HandlingExistingEntity( string ctid, ref SaveStatus status )
		{
			MP.TransferValueProfile entity = new MP.TransferValueProfile();
			entity = TransferValueProfileManager.GetByCtid( ctid );
			if ( entity != null && entity.Id > 0 )
			{
				Entity relatedEntity = EntityManager.GetEntity( entity.RowId );
				if ( relatedEntity == null || relatedEntity.Id == 0 )
				{
					status.AddError( string.Format( "Error - the related Entity for transfer value: '{0}' ({1}), was not found.", entity.Name, entity.Id ) );
					return entity;
				}
				//we know for this type, there will entity.learningopp, entity.assessment and entity.credential relationships, and quick likely blank nodes.
				//delete related entity if a reference
				//there are for and from relationships!! - OK in this case it will be all
				new Entity_AssessmentManager().DeleteAll( relatedEntity, ref status );
				new Entity_CredentialManager().DeleteAll( relatedEntity, ref status );
				new Entity_LearningOpportunityManager().DeleteAll( relatedEntity, ref status );
				//also
				entity.TransferValueFor = new List<TopLevelObject>();
				entity.TransferValueFrom = new List<TopLevelObject>();

			}
			return entity;
		}
		#endregion

		public static MP.TransferValueProfile GetByCtid( string ctid )
		{
			MP.TransferValueProfile entity = new MP.TransferValueProfile();
			entity = TransferValueProfileManager.GetByCtid( ctid );
			return entity;
		}

		public static MP.TransferValueProfile Get( int id )
		{
			MP.TransferValueProfile entity = new MP.TransferValueProfile();
			entity = TransferValueProfileManager.Get( id );
			return entity;
		}
		public static List<CommonSearchSummary> Search( MainSearchInput data, ref int pTotalRows )
		{
			if ( UtilityManager.GetAppKeyValue( "usingElasticTransferValueSearch", false ) )
			{
				//var results = ElasticServices.GeneralSearch( CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, data, ref pTotalRows );
				return ElasticServices.GeneralSearch( CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, "TransferValue", data, ref pTotalRows );
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
						EntityTypeId = CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE,
						EntityType = "TransferValueProfile"
					} );
				}
				return results;
			}

		}//

		public static List<ThisEntity> DoSearch( MainSearchInput data, ref int totalRows )
		{
			string where = "";
			List<string> competencies = new List<string>();

			//only target full entities
			where = " ( base.EntityStateId = 3 ) ";
			//need to create a new category id for custom filters
			//SearchServices.HandleCustomFilters( data, 61, ref where );

			SetKeywordFilter( data.Keywords, false, ref where );
			//SearchServices.SetSubjectsFilter( data, CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, ref where );

			//SetPropertiesFilter( data, ref where );
			SearchServices.SetRolesFilter( data, ref where );
			SearchServices.SetBoundariesFilter( data, ref where );

			//Competencies
			//SetCompetenciesFilter( data, ref where, ref competencies );

			LoggingHelper.DoTrace( 5, "TransferValueServices.Search(). Filter: " + where );
			return EntityMgr.Search( where, data.SortOrder, data.StartPage, data.PageSize, ref totalRows );
		}

		public static List<ThisEntity> GetTVPOwnedByOrg( int orgId, int maxRecords )
		{
			string where = "";
			List<string> competencies = new List<string>();
			int totalRows = 0;
			//only target full entities
			where = " ( base.EntityStateId = 3 ) ";

			if ( orgId > 0 )
			{
				where = where + " AND " + string.Format( " ( base.OrganizationId = {0} ) ", orgId );
			}

			LoggingHelper.DoTrace( 5, "TransferValueServices.Search(). Filter: " + where );
			return EntityMgr.Search( where, "", 1, maxRecords, ref totalRows );
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

	}
}

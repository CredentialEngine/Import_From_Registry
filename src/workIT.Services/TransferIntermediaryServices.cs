using System.Collections.Generic;

using workIT.Factories;
using workIT.Models;
using workIT.Models.Common;
using workIT.Models.Search;
using workIT.Utilities;

using ElasticHelper = workIT.Services.ElasticServices;
using ThisEntityMgr = workIT.Factories.TransferIntermediaryManager;
using MP = workIT.Models.Common;
using ThisEntity = workIT.Models.Common.TransferIntermediary;

namespace workIT.Services
{
	public class TransferIntermediaryServices
	{
		string thisClassName = "TransferIntermediaryServices";
		ActivityServices activityMgr = new ActivityServices();
		public List<string> messages = new List<string>();
		public static int entityTypeId = CodesManager.ENTITY_TYPE_TRANSFER_INTERMEDIARY;

		#region import

		public bool Import( ThisEntity entity, ref SaveStatus status )
		{

			bool isValid = new ThisEntityMgr().Save( entity, ref status );
			List<string> messages = new List<string>();
			if ( entity.Id > 0 )
			{

				if ( UtilityManager.GetAppKeyValue( "delayingAllCacheUpdates", false ) == false )
				{
					new SearchPendingReindexManager().Add( entityTypeId, entity.Id, 1, ref messages );
					if ( messages.Count > 0 )
						status.AddWarningRange( messages );
					//also update related org
					new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, entity.OwningOrganizationId, 1, ref messages );
				}
				else
				{
					new SearchPendingReindexManager().Add( entityTypeId, entity.Id, 1, ref messages );
					if ( entity.OwningOrganizationId > 0 )
						new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, entity.OwningOrganizationId, 1, ref messages );
					//Will probably have to update the TVPs to indicate part of a TI
					if ( entity.IntermediaryFor.Count > 0 )
					{
						foreach ( var item in entity.IntermediaryFor )
						{
							new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, item.Id, 1, ref messages );
						}
					}
					
					if ( messages.Count > 0 )
						status.AddWarningRange( messages );
				}
				//no caching needed yet
				//CacheManager.RemoveItemFromCache( "cframework", entity.Id );
			}

			return isValid;
		}

		/// <summary>
		/// Check if TVP exists
		/// </summary>
		/// <param name="ctid"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public static ThisEntity HandlingExistingEntity( string ctid, ref SaveStatus status )
		{
			var entity = new ThisEntity();
			//warning- 
			entity = ThisEntityMgr.GetByCtid( ctid );
			if ( entity != null && entity.Id > 0 )
			{
				Entity relatedEntity = EntityManager.GetEntity( entity.RowId );
				if ( relatedEntity == null || relatedEntity.Id == 0 )
				{
					status.AddError( string.Format( "Error - the related Entity for transfer Intermediary: '{0}' ({1}), was not found.", entity.Name, entity.Id ) );
					return entity;
				}
				//other classes might do deletes here. However, if the import fails, then we have an inconsistency

			}
			return entity;
		}
		#endregion

		public static ThisEntity GetByCtid( string ctid )
		{
			ThisEntity entity = new ThisEntity();
			entity = ThisEntityMgr.GetByCtid( ctid );
			return entity;
		}

		public static ThisEntity Get( int id )
		{
			ThisEntity entity = new ThisEntity();
			entity = ThisEntityMgr.Get( id );
			return entity;
		}
		public static List<CommonSearchSummary> Search( MainSearchInput data, ref int pTotalRows )
		{
			if ( UtilityManager.GetAppKeyValue( "usingElasticTransferIntermediarySearch", false ) )
			{
				return ElasticHelper.GeneralSearch( entityTypeId, "TransferIntermediary", data, ref pTotalRows );
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
						EntityTypeId = entityTypeId,
						EntityType = "TransferIntermediary"
					} );
				}
				return results;
			}

		}//

		public static List<ThisEntity> DoSearch( MainSearchInput data, ref int totalRows )
		{
			string where = "";

			//only target full entities
			where = " ( base.EntityStateId = 3 ) ";
			//need to create a new category id for custom filters
			//SearchServices.HandleCustomFilters( data, 61, ref where );

			SetKeywordFilter( data.Keywords, false, ref where );
			//SearchServices.SetSubjectsFilter( data, entityTypeId, ref where );

			//SetPropertiesFilter( data, ref where );
			SearchServices.SetRolesFilter( data, ref where );
			SearchServices.SetBoundariesFilter( data, ref where );

			LoggingHelper.DoTrace( 5, "TransferIntermediaryServices.Search(). Filter: " + where );
			return ThisEntityMgr.Search( where, data.SortOrder, data.StartPage, data.PageSize, ref totalRows );
		}

		public static List<ThisEntity> GetOwnedByOrg( int orgId, int maxRecords )
		{
			string where = "";
			int totalRows = 0;
			//only target full entities
			where = " ( base.EntityStateId = 3 ) ";

			if ( orgId > 0 )
			{
				where = where + " AND " + string.Format( " ( base.OrganizationId = {0} ) ", orgId );
			}

			LoggingHelper.DoTrace( 5, "TransferIntermediaryServices.GetTVPOwnedByOrg(). Filter: " + where );
			return ThisEntityMgr.Search( where, "", 1, maxRecords, ref totalRows );
		}
		//
		//Get tvp for entity
		public static List<ThisEntity> GetEntityHasTransferIntermediary( string searchType, int recordId, int maxRecords )
		{
			string where = "";
			int totalRows = 0;
			if ( recordId == 0 || string.IsNullOrWhiteSpace( searchType ) )
				return new List<ThisEntity>();

			//only target full entities
			switch ( searchType.ToLower() )
			{
				case "credential":
					where = string.Format( " (  base.id in (Select a.Id from [TransferIntermediary] a inner join entity b on a.RowId = b.EntityUid inner join [Entity.Credential] c ON b.Id = c.EntityId  where c.CredentialId = {0}) ) ", recordId );
					break;
				case "assessment":
					where = string.Format( " (  base.id in (Select a.Id from [TransferIntermediary] a inner join entity b on a.RowId = b.EntityUid inner join[Entity.Assessment] c ON b.Id = c.EntityId  where c.AssessmentId = {0}) ) ", recordId );
					break;
				case "learningopportunity":
					where = string.Format( " (  base.id in (Select a.Id from [TransferIntermediary] a inner join entity b on a.RowId = b.EntityUid inner join[Entity.learningopportunity] c ON b.Id = c.EntityId  where c.LearningOpportunityId = {0}) ) ", recordId );
					break;
			}

			LoggingHelper.DoTrace( 5, "TransferIntermediaryServices.GetEntityHasTVP(). Filter: " + where );
			return ThisEntityMgr.Search( where, "", 1, maxRecords, ref totalRows );
		}
		//
		public static List<ThisEntity> GetEntitiesForTVP( string searchType, int recordId, int maxRecords )
		{
			string where = "";
			int totalRows = 0;
			if ( recordId == 0 || string.IsNullOrWhiteSpace( searchType ) )
				return new List<ThisEntity>();

			//only target full entities
			switch ( searchType.ToLower() )
			{
				case "credential":
					var list = TransferIntermediaryManager.GetAllCredentials( entityTypeId, recordId );

					break;
				case "assessment":
					where = string.Format( " (  base.id in (Select a.Id from [TransferIntermediary] a inner join entity b on a.RowId = b.EntityUid inner join[Entity.Assessment] c ON b.Id = c.EntityId  where c.CredentialId = {0}) ) ", recordId );
					break;
				case "learningopportunity":
					where = string.Format( " (  base.id in (Select a.Id from [TransferIntermediary] a inner join entity b on a.RowId = b.EntityUid inner join[Entity.learningopportunity] c ON b.Id = c.EntityId  where c.CredentialId = {0}) ) ", recordId );
					break;
			}

			LoggingHelper.DoTrace( 5, "TransferIntermediaryServices.GetEntityHasTVP(). Filter: " + where );
			return ThisEntityMgr.Search( where, "", 1, maxRecords, ref totalRows );
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
			//string indexFilter = " OR (base.Id in (SELECT c.id FROM [dbo].[Entity.SearchIndex] a inner join Entity b on a.EntityId = b.Id inner join TransferIntermediary c on b.EntityUid = c.RowId where (b.EntityTypeId = 3 AND ( a.TextValue like '{0}' OR a.[CodedNotation] like '{0}' ) ))) ";

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

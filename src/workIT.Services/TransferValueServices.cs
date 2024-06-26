using System.Collections.Generic;

using workIT.Factories;
using workIT.Models;
using workIT.Models.Common;
using workIT.Models.Search;
using workIT.Utilities;
using APIResourceServices = workIT.Services.API.TransferValueServices;

using ElasticHelper = workIT.Services.ElasticServices;
using ResourceManager = workIT.Factories.TransferValueProfileManager;
using MP = workIT.Models.Common;
using ThisResource = workIT.Models.Common.TransferValueProfile;
using Newtonsoft.Json;

namespace workIT.Services
{
	public class TransferValueServices
	{
		static string thisClassName = "TransferValueServices";
		static string ThisEntityType = "TransferValue";
        static int ThisResourceEntityTypeId = CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE;

        ActivityServices activityMgr = new ActivityServices();
		public List<string> messages = new List<string>();


		#region import

		public bool Import( ThisResource resource, ref SaveStatus status )
		{

			bool isValid = new ResourceManager().Save( resource, ref status );
			List<string> messages = new List<string>();
			if ( resource.Id > 0 )
			{
				var statusMsg = "";
				var eManager = new EntityManager();
				//prep cache properties
				var detail = APIResourceServices.GetDetailForAPI( resource.Id, true );
				if ( detail != null && detail.Meta_Id > 0 )
				{
					var resourceDetail = JsonConvert.SerializeObject( detail, JsonHelper.GetJsonSettings( false ) );
					if ( eManager.EntityCacheUpdateResourceDetail( resource.RowId, resourceDetail, ref statusMsg ) == 0 )
					{
						status.AddError( statusMsg );
					}
					//realistically, don't have to do this every time
					//TODO: should now do in JSON so don't need the stored proc. OR see what can be derived from resourceDetail

					if ( eManager.EntityCacheUpdateAgentRelationshipsForTransferValueProfile( resource.RowId.ToString(), ref statusMsg ) == false )
					{
						status.AddError( statusMsg );
					}
				}
				
				new SearchPendingReindexManager().Add( ThisResourceEntityTypeId, resource.Id, 1, ref messages );
				if ( resource.OwningOrganizationId  > 0)
					new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, resource.OwningOrganizationId, 1, ref messages );
				/*
					* now don't do this, use common method
				if ( resource.CredentialIds.Count > 0 )
				{
					foreach ( var item in resource.CredentialIds )
					{
						new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL, item, 1, ref messages );
					}
				}
				if ( resource.AssessmentIds.Count > 0 )
				{
					foreach ( var item in resource.AssessmentIds )
					{
						new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, item, 1, ref messages );
					}
				}
				if ( resource.LearningOpportunityIds.Count > 0 )
				{
					foreach ( var item in resource.LearningOpportunityIds )
					{
						new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, item, 1, ref messages );
					}
				}
				*/
				if ( messages.Count > 0 )
					status.AddWarningRange( messages );
				
				//no caching needed yet
				//CacheManager.RemoveItemFromCache( "cframework", entity.Id );
			}

			return isValid;
		}
		public void UpdatePendingReindex( ThisResource resource, ref SaveStatus status )
		{
			List<string> messages = new List<string>();
			new SearchPendingReindexManager().Add( ThisResourceEntityTypeId, resource.Id, 1, ref messages );
			int orgId = resource.OwningOrganizationId;

			if ( orgId == 0 )
			{
				var org = OrganizationManager.GetBasics( resource.PrimaryAgentUID, false );
				if ( org != null && org.Id > 0 )
					orgId = org.Id;

			}
			if ( orgId > 0 )
				new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, orgId, 1, ref messages );
			if ( messages.Count > 0 )
				status.AddWarningRange( messages );
		}

		/// <summary>
		/// Check if TVP exists
		/// </summary>
		/// <param name="ctid"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public static ThisResource HandlingExistingEntity( string ctid, ref SaveStatus status )
		{
			ThisResource entity = new ThisResource();
			//warning- 
			entity = ResourceManager.GetByCtid( ctid );
			if ( entity != null && entity.Id > 0 )
			{
				Entity relatedEntity = EntityManager.GetEntity( entity.RowId );
				if ( relatedEntity == null || relatedEntity.Id == 0 )
				{
					status.AddError( string.Format("Error - the related Entity for transfer value: '{0}' ({1}), was not found.", entity.Name, entity.Id) );
					return entity;
				}
				//we know for this type, there will entity.learningopp, entity.assessment and entity.credential relationships, and quick likely blank nodes.
				//delete related entity if a reference
				//there are for and from relationships!! 
				//		- OK in this case it will be all
				//NOTE: delete all is messy. Should just do Replace
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

		public static ThisResource GetByCtid( string ctid, bool gettingAll = false )
		{
			ThisResource entity = new ThisResource();
			entity = ResourceManager.GetByCtid( ctid, gettingAll );
			return entity;
		}

		public static ThisResource Get( int id )
		{
			ThisResource entity = new ThisResource();
			entity = ResourceManager.Get( id );
			return entity;
		}

        public static List<string> Autocomplete( MainSearchInput query, int maxTerms = 25 )
        {

            string where = "";
            int totalRows = 0;

            if ( UtilityManager.GetAppKeyValue( "usingElasticSupportServiceSearch", false ) )
            {
                var keywords = query.Keywords;
                return ElasticHelper.GeneralAutoComplete( ThisResourceEntityTypeId, ThisEntityType, query, maxTerms, ref totalRows );
            }
            else
            {
                string keywords = ServiceHelper.HandleApostrophes( query.Keywords );
                if ( keywords.IndexOf( "%" ) == -1 )
                    keywords = "%" + keywords.Trim() + "%";
                where = string.Format( " (base.name like '{0}') ", keywords );

                SetKeywordFilter( keywords, true, ref where );
                return ResourceManager.Autocomplete( where, 1, maxTerms, ref totalRows );
            }
        }
        public static List<CommonSearchSummary> Search( MainSearchInput data, ref int pTotalRows )
		{
			if ( UtilityManager.GetAppKeyValue( "usingElasticTransferValueSearch", false ) )
			{
				//var results = ElasticHelper.GeneralSearch( ThisResourceEntityTypeId, data, ref pTotalRows );
				return ElasticHelper.GeneralSearch( ThisResourceEntityTypeId, ThisEntityType, data, ref pTotalRows );
			}
			else
			{
				List<CommonSearchSummary> results = new List<CommonSearchSummary>();
				var list = DoSearch( data, ref pTotalRows );
				foreach (var item in list)
				{
					results.Add( new CommonSearchSummary()
					{
						Id = item.Id,
						Name = item.Name,
						Description = item.Description,
						SubjectWebpage = item.SubjectWebpage,
						PrimaryOrganizationName = item.PrimaryOrganizationName,
						CTID = item.CTID,
						EntityTypeId = ThisResourceEntityTypeId,
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
			//SearchServices.SetSubjectsFilter( data, ThisResourceEntityTypeId, ref where );

			//SetPropertiesFilter( data, ref where );
			SearchServices.SetRolesFilter( data, ref where );
			SearchServices.SetBoundariesFilter( data, ref where );

			LoggingHelper.DoTrace( 5, $"{thisClassName}.Search(). Filter: " + where );
			return ResourceManager.Search( where, data.SortOrder, data.StartPage, data.PageSize, ref totalRows );
		}

		public static List<ThisResource> DoTransferIntermediarySearch( MainSearchInput data, ref int totalRows )
		{
			string where = "";

			//only target full entities
			where = " ( base.EntityStateId = 3 ) ";
			//need to create a new category id for custom filters
			//SearchServices.HandleCustomFilters( data, 61, ref where );

			SetKeywordFilter( data.Keywords, false, ref where );
			//SearchServices.SetSubjectsFilter( data, ThisResourceEntityTypeId, ref where );

			//SetPropertiesFilter( data, ref where );
			SearchServices.SetRolesFilter( data, ref where );
			SearchServices.SetBoundariesFilter( data, ref where );

			LoggingHelper.DoTrace( 5, "TransferValueServices.DoTransferIntermediarySearch(). Filter: " + where );
			return ResourceManager.DoTransferIntermediarySearch( where, data.SortOrder, data.StartPage, data.PageSize, ref totalRows );
		}

		public static List<ThisResource> GetTVPOwnedByOrg( int orgId, int maxRecords )
		{
			string where = "";
			int totalRows = 0;
			//only target full entities
			where = " ( base.EntityStateId = 3 ) ";

			if ( orgId > 0 )
			{
				where = where + " AND " + string.Format( " ( base.OrganizationId = {0} ) ", orgId );
			}

			LoggingHelper.DoTrace( 5, "TransferValueServices.GetTVPOwnedByOrg(). Filter: " + where );
			return ResourceManager.Search( where, "", 1, maxRecords, ref totalRows );
		}
		//
		//Get tvp for entity
		public static List<ThisResource> GetEntityHasTVP( string searchType, int recordId, int maxRecords )
		{
			string where = "";
			int totalRows = 0;
			if ( recordId == 0 || string.IsNullOrWhiteSpace( searchType ))
				return new List<ThisResource>();

			//only target full entities
			switch (searchType.ToLower())
			{
				case "credential":
					where = string.Format( " (  base.id in (Select a.Id from [TransferValueProfile] a inner join entity b on a.RowId = b.EntityUid inner join [Entity.Credential] c ON b.Id = c.EntityId  where c.CredentialId = {0}) ) ", recordId );
					break;
				case "assessment":
					where = string.Format( " (  base.id in (Select a.Id from [TransferValueProfile] a inner join entity b on a.RowId = b.EntityUid inner join[Entity.Assessment] c ON b.Id = c.EntityId  where c.AssessmentId = {0}) ) ", recordId );
					break;
				case "learningopportunity":
					where = string.Format( " (  base.id in (Select a.Id from [TransferValueProfile] a inner join entity b on a.RowId = b.EntityUid inner join[Entity.learningopportunity] c ON b.Id = c.EntityId  where c.LearningOpportunityId = {0}) ) ", recordId );
					break;
			}

			LoggingHelper.DoTrace( 5, "TransferValueServices.GetEntityHasTVP(). Filter: " + where );
			return ResourceManager.Search( where, "", 1, maxRecords, ref totalRows );
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

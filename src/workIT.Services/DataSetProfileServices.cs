using System.Collections.Generic;

using workIT.Factories;
using workIT.Models;
using workIT.Models.Common;
using workIT.Models.Search;
using workIT.Utilities;

using ElasticHelper = workIT.Services.ElasticServices;
using ResourceManager = workIT.Factories.DataSetProfileManager;
using MP = workIT.Models.Common;
using ThisResource = workIT.Models.QData.DataSetProfile;
using ThisResourceSummary = workIT.Models.QData.DataSetProfileSummary;

namespace workIT.Services
{
	public class DataSetProfileServices
	{
		static string thisClassName = "DataSetProfileServices";
		static string ThisEntityType = "DataSetProfile";
		static int ThisEntityTypeId = CodesManager.ENTITY_TYPE_DATASET_PROFILE;


		ActivityServices activityMgr = new ActivityServices();
		public List<string> messages = new List<string>();


		#region import

		public bool Import( ThisResource entity, ref SaveStatus status )
		{

			bool isValid = new ResourceManager().Save( entity, null, ref status );
			List<string> messages = new List<string>();
			if ( entity.Id > 0 )
			{

				if ( UtilityManager.GetAppKeyValue( "delayingAllCacheUpdates", false ) == false )
				{
					//update cache - not applicable yet

					//update Elastic
					if ( Utilities.UtilityManager.GetAppKeyValue( "updatingElasticIndexImmediately", false ) )
					{
						//ElasticHelper.DataSetProfileProfile_UpdateIndex( entity.Id );
					}
					else
					{
						new SearchPendingReindexManager().Add( ThisEntityTypeId, entity.Id, 1, ref messages );
						if ( messages.Count > 0 )
							status.AddWarningRange( messages );
						//also need to reindex the 'About' resource
					}
					//also update related org
					//????????????
					if ( entity.DataProviderOld != null && entity.DataProviderOld.Id > 0 )
						new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, entity.DataProviderOld.Id, 1, ref messages );
				}
				else
				{
					//at this time dataset profiles are not in elastic.
					//new SearchPendingReindexManager().Add( ThisEntityTypeId, entity.Id, 1, ref messages );

					//use about to get any credentials to reindex
					//var dsp = ResourceManager.Get( entity.Id, false );
					//if (dsp.AboutOLD != null && dsp.AboutOLD.Count > 0)
					//{
					//	foreach (var item in dsp.AboutOLD )
					//	{
					//		new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL, item.Id, 1, ref messages );
					//	}
					//}
					if (entity.CredentialIds.Count > 0)
					{
						foreach ( var item in entity.CredentialIds )
						{
							new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL, item, 1, ref messages );
						}
					}
					if ( entity.AssessmentIds.Count > 0 )
					{
						foreach ( var item in entity.AssessmentIds )
						{
							new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, item, 1, ref messages );
						}
					}
					if ( entity.LearningOpportunityIds.Count > 0 )
					{
						foreach ( var item in entity.LearningOpportunityIds )
						{
							new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, item, 1, ref messages );
						}
					}

					if ( entity.DataProviderOld!= null && entity.DataProviderOld.Id > 0 )
						new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, entity.DataProviderOld.Id, 1, ref messages );
					if ( messages.Count > 0 )
						status.AddWarningRange( messages );
				}
				//no caching needed yet
				//CacheManager.RemoveItemFromCache( "cframework", entity.Id );
			}

			return isValid;
		}
		public static ThisResource HandlingExistingEntity( string ctid, ref SaveStatus status )
		{
			var entity = new ThisResource();
			//only need the base 
			entity = DataSetProfileManager.GetByCtid( ctid, false );
			if ( entity != null && entity.Id > 0 )
			{
				//
				Entity relatedEntity = EntityManager.GetEntity( entity.RowId );
				if ( relatedEntity == null || relatedEntity.Id == 0 )
				{
					status.AddError( string.Format( "Error - the related Entity for DataSetProfile: '{0}' ({1}), was not found.", entity.Name, entity.Id ) );
					return entity;
				}
				//any clean up?



			}
			return entity;
		}
		#endregion

		public static ThisResource GetByCtid( string ctid, bool skippingCache = false )
		{
			ThisResource entity = new ThisResource();
			entity = ResourceManager.GetByCtid( ctid, true, false );
			return entity;
		}


		public static ThisResource Get( int id, bool skippingCache = false)
		{
			ThisResource entity = new ThisResource();
			entity = ResourceManager.Get( id );
			return entity;
		}
        public static ThisResource GetDetail( int id, bool skippingCache = false )
        {
            ThisResource entity = ResourceManager.Get( id, true );
            if ( entity.EntityStateId == 0 )
                return null;


            return entity;
        }
        public static ThisResource GetDetailByCtid( string ctid, bool skippingCache = false )
        {
            ThisResource entity = new ThisResource();
            if ( string.IsNullOrWhiteSpace( ctid ) )
                return entity;
            var resource = ResourceManager.GetByCtid( ctid, false );

            return GetDetail( resource.Id, skippingCache );
        }
        public static List<TopLevelObject> GetDSPCredentialsForOrg( int orgId, int maxRecords )
		{
			return ResourceManager.GetAllDataSetCredentials( orgId, maxRecords );
		}
        public static List<ThisResourceSummary> Autocomplete( string keywords, int maxRows, ref int totalRows )
        {

            string where = "";
            SetKeywordFilter( keywords, false, ref where );

            LoggingHelper.DoTrace( 7, "ConceptSchemeServices.Autocomplete(). Filter: " + where );

            return ResourceManager.Search( where, "", 1, maxRows, ref totalRows );

        }
        public static List<CommonSearchSummary> Search( MainSearchInput data, ref int pTotalRows )
		{
			if ( UtilityManager.GetAppKeyValue( "usingElasticOutcomeDataSearch", false ) )
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
						PrimaryOrganizationName = item.DataProviderName,
						CTID = item.CTID,
						EntityTypeId = ThisEntityTypeId,
						EntityType = ThisEntityType
					} );
				}
				return results;
			}

		}//
		public static List<ThisResourceSummary> DoSearch( MainSearchInput data, ref int totalRows )
		{
			string where = "";

			//only target full entities
			where = " ( base.EntityStateId = 3 ) ";

			//need to create a new category id for custom filters
			//SearchServices.HandleCustomFilters( data, 61, ref where );

			SetKeywordFilter( data.Keywords, false, ref where );
			SearchServices.SetSubjectsFilter( data, CodesManager.ENTITY_TYPE_PATHWAY, ref where );

			//SetPropertiesFilter( data, ref where );
			SearchServices.SetRolesFilter( data, ref where );
			SearchServices.SetBoundariesFilter( data, ref where );


			LoggingHelper.DoTrace( 5, thisClassName + ".DoSearch(). Filter: " + where );
			return ResourceManager.Search( where, data.SortOrder, data.StartPage, data.PageSize, ref totalRows );
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
			//OR base.SubjectWebpage like '{0}' 
			string text = " (base.name like '{0}'  OR base.DataProviderName like '{0}'  ) ";
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
			//else if ( keywords.ToLower() == "[hascredentialregistryid]" )
			//{
			//	text = " ( len(Isnull(CredentialRegistryId,'') ) = 36 ) ";
			//	isCustomSearch = true;
			//}


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
		//
	}
}

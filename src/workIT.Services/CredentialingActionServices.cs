using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.Search;
using ElasticHelper = workIT.Services.ElasticServices;
using APIResourceServices = workIT.Services.API.CredentialingActionServices;

using ThisResource = workIT.Models.Common.CredentialingAction;
using ResourceManager = workIT.Factories.CredentialingActionManager;
using workIT.Utilities;
using workIT.Factories;
using Newtonsoft.Json;

namespace workIT.Services
{
	public class CredentialingActionServices
	{
		static string thisClassName = "CredentialingActionServices";
		public static string ThisEntityType = "CredentialingAction";
		public static int ThisEntityTypeId = CodesManager.ENTITY_TYPE_CREDENTIALING_ACTION;
		static string usingElasticSearch = "usingElasticCredentialingActionSearch";

		#region import
		public static ThisResource GetMinimumByCtid( string ctid )
		{
			ThisResource resource = new ThisResource();
			if ( string.IsNullOrWhiteSpace( ctid ) )
				return resource;

			return ResourceManager.GetMinimumByCtid( ctid );
		}
		public bool Import( ThisResource input, ref SaveStatus status )
		{
			bool isValid = new ResourceManager().Save( input, ref status );
			if ( input.Id > 0 )
			{
				List<string> messages = new List<string>();
				var resource = APIResourceServices.GetDetailForElastic( input.Id, true );
				if ( resource != null && resource.Meta_Id > 0 )
				{
					var resourceDetail = JsonConvert.SerializeObject( resource, JsonHelper.GetJsonSettings( false ) );
					var statusMsg = "";
					if ( new EntityManager().EntityCacheUpdateResourceDetail( input.CTID, resourceDetail, ref statusMsg ) == 0 )
					{
						status.AddError( statusMsg );
					}
				}

				//TODO - will need to update related elastic indices
				new SearchPendingReindexManager().Add( ThisEntityTypeId, input.Id, 1, ref messages );

				//tag org to reindex
				new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, input.PrimaryOrganizationId, 1, ref messages );
			}

			return isValid;
		}

		#endregion


		#region CredentialingAction Profile

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
			int totalRows = 0;

			var keywords = query.Keywords;
			return ElasticHelper.GeneralAutoComplete( ThisEntityTypeId, ThisEntityType, query, maxTerms, ref totalRows );

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

			//
			string text = " (base.name like '{0}' OR base.Description like '{0}'  OR base.PrimaryOrganizationName like '{0}'  ) ";
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

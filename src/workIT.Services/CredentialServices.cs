using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Web;

using workIT.Models;
using workIT.Models.Common;
using MCD=workIT.Models.API;
using workIT.Models.Search;
using ElasticHelper = workIT.Services.ElasticServices;
using ThisEntity = workIT.Models.Common.Credential;
using ThisSearchEntity = workIT.Models.Common.CredentialSummary;
using EntityMgr = workIT.Factories.CredentialManager;

using workIT.Utilities;
using workIT.Factories;

namespace workIT.Services
{
	public class CredentialServices
	{
		static string thisClassName = "CredentialServices";

		public List<string> messages = new List<string>();

		public CredentialServices()
		{
		}

		#region import
		public bool Import( ThisEntity entity, ref SaveStatus status )
		{
			//do a get, and add to cache before updating
			if ( entity.Id > 0 )
			{
				//no need to get and cache if called from batch import - maybe during day, but likelihood of issues is small?
				if ( UtilityManager.GetAppKeyValue( "credentialCacheMinutes", 0 ) > 0 )
				{
					if ( System.DateTime.Now.Hour > 7 && System.DateTime.Now.Hour < 18 )
						GetDetail( entity.Id );
				}
                string key = "credentialapi_" + entity.Id.ToString();
                ServiceHelper.ClearCacheEntity( key );
            }
			bool isValid = new EntityMgr().Save( entity, ref status );
			List<string> messages = new List<string>();

			if ( entity.Id > 0 )
			{
				if ( UtilityManager.GetAppKeyValue( "credentialCacheMinutes", 0 ) > 0 )
					CacheManager.RemoveItemFromCache( "credential", entity.Id );

				if ( UtilityManager.GetAppKeyValue( "delayingAllCacheUpdates", false ) == false )
				{
					ThreadPool.QueueUserWorkItem( UpdateCaches, entity );

					new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, entity.OwningOrganizationId, 1, ref messages );
					if ( messages.Count > 0 )
						status.AddWarningRange( messages );
				}
				else
				{
					//only update elatic if has apparent relevent changes
					if (status.UpdateElasticIndex)
						new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL, entity.Id, 1, ref messages );
					new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, entity.OwningOrganizationId, 1, ref messages );
					//check for embedded items
					//has part
					AddCredentialsToPendingReindex( entity.HasPartIds );
					AddCredentialsToPendingReindex( entity.IsPartOfIds );

					if ( messages.Count > 0 )
						status.AddWarningRange( messages );
				}
			}

			return isValid;
		}
		static void UpdateCaches( Object entity )
		{
			if ( entity.GetType() != typeof( Models.Common.Credential ) )
				return;
			var document = ( entity as Models.Common.Credential );
			//EntityCache ec = new EntityCache()
			//{
			//	EntityTypeId = 1,
			//	EntityType = "credential",
			//	EntityStateId = document.EntityStateId,
			//	EntityUid = document.RowId,
			//	BaseId = document.Id,
			//	Description = document.Description,
			//	SubjectWebpage = document.SubjectWebpage,
			//	CTID = document.CTID,
			//	Created = document.Created,
			//	LastUpdated = document.LastUpdated,
			//	ImageUrl = document.Image,
			//	Name = document.Name,
			//	OwningAgentUID = document.OwningAgentUid,
			//	OwningOrgId = document.OrganizationId
			//};
			//var statusMessage = "";
			//new EntityManager().EntityCacheSave( ec, ref statusMessage );

			new CacheManager().PopulateEntityRelatedCaches( document.RowId );
			//update Elastic
			List<string> messages = new List<string>();

			if ( Utilities.UtilityManager.GetAppKeyValue( "updatingElasticIndexImmediately", false ) )
				ElasticHelper.Credential_UpdateIndex( document.Id );
			else
				new SearchPendingReindexManager().Add( 1, document.Id, 1, ref messages );

		}
		public void AddCredentialsToPendingReindex( List<Credential> list )
		{
			List<string> messages = new List<string>();
			foreach ( var item in list )
			{
				new SearchPendingReindexManager().Add( 1, item.Id, 1, ref messages );
			}
		}
		public void AddCredentialsToPendingReindex( List<int> list )
		{
			List<string> messages = new List<string>();
			foreach ( var item in list )
			{
				new SearchPendingReindexManager().Add( 1, item, 1, ref messages );
			}
		}
		#endregion

		#region search 
		/// <summary>
		/// Credential autocomplete
		/// Needs to check authorization level for credential
		/// </summary>
		/// <param name="keyword"></param>
		/// <param name="maxTerms"></param>
		/// <returns></returns>
		public List<string> Autocomplete( MainSearchInput query, int maxTerms = 25 )
		{
			int userId = 0;
			string where = " base.EntityStateId = 3 ";
			string AND = "";
			int pTotalRows = 0;
			AppUser user = AccountServices.GetCurrentUser();
			if ( user != null && user.Id > 0 )
				userId = user.Id;

			if ( UtilityManager.GetAppKeyValue( "usingElasticCredentialAutocomplete", false ) )
			{
				return new ElasticHelper().CredentialAutoComplete( query, maxTerms, ref pTotalRows );
			}
			else
			{
				var keywords = query.Keywords;
				bool usingLinqAutocomplete = true;
				if ( usingLinqAutocomplete )
				{
					return CredentialManager.AutocompleteInternal( keywords, 1, maxTerms, ref pTotalRows );
				}
				else
				{
					string text = " (base.name like '{0}'  OR base.AlternateName like '{0}' OR OwningOrganization like '{0}'  ) ";
					//SetKeywordFilter( keywords, true, ref where );
					keywords = ServiceHelper.HandleApostrophes( keywords );
					if ( keywords.IndexOf( "%" ) == -1 )
					{
						keywords = SearchServices.SearchifyWord( keywords );
					}
					if ( where.Length > 0 )
						AND = " AND ";
					where = where + AND + string.Format( " ( " + text + " ) ", keywords );

					return CredentialManager.AutocompleteDB( where, 1, maxTerms, ref pTotalRows );
				}
				// return new List<string>();
			}
		}
	
		public List<string> AutocompleteCompetencies( string keyword, int maxTerms = 25 )
		{
			int userId = 0;
			string where = "";

			AppUser user = AccountServices.GetCurrentUser();
			if ( user != null && user.Id > 0 )
				userId = user.Id;

			SetCompetenciesAutocompleteFilter( keyword, ref where );

			//return CredentialManager.Autocomplete( where, 1, maxTerms, userId, ref pTotalRows );
			return new List<string>();
		}


		/// <summary>
		/// Full credentials search
		/// </summary>
		/// <param name="data"></param>
		/// <param name="pTotalRows"></param>
		/// <returns></returns>
		public static List<ThisSearchEntity> Search( MainSearchInput data, ref int pTotalRows )
		{
			if ( UtilityManager.GetAppKeyValue( "usingElasticCredentialSearch", false ) || data.Elastic )
			{
				return new ElasticHelper().Credential_Search( data, ref pTotalRows );
			}
			else
			{
				return DoSearch( data, ref pTotalRows );
			}
		}

		/// <summary>
		/// Full credentials search
		/// </summary>
		/// <param name="data"></param>
		/// <param name="pTotalRows"></param>
		/// <returns></returns>
		private static List<ThisSearchEntity> DoSearch( MainSearchInput data, ref int pTotalRows )
		{
			string where = "";
			DateTime start = DateTime.Now;
			//Stopwatch stopwatch = new Stopwatch();
			//stopwatch.Start();
			LoggingHelper.DoTrace( 6, string.Format( "===CredentialServices.Search === Started: {0}", start ) );
			int userId = 0;
			List<string> competencies = new List<string>();

			AppUser user = AccountServices.GetCurrentUser();
			if ( user != null && user.Id > 0 )
				userId = user.Id;
			//only target full entities
			where = " ( base.EntityStateId = 3 ) ";

			SetKeywordFilter( data.Keywords, false, ref where );
			where = where.Replace( "[USERID]", user.Id.ToString() );

			SearchServices.SetSubjectsFilter( data, CodesManager.ENTITY_TYPE_CREDENTIAL, ref where );

			SearchServices.HandleCustomFilters( data, 58, ref where );

			//Should probably move this to its own method?
			string agentRoleTemplate = " ( id in (SELECT [CredentialId] FROM [dbo].[CredentialAgentRelationships_Summary] where RelationshipTypeId = {0} and OrgId = {1})) ";
			int roleId = 0;
			int orgId = 0;
			string AND = "";
			if ( where.Length > 0 )
				AND = " AND ";

			//Updated to use FilterV2
			foreach ( var filter in data.FiltersV2.Where( m => m.Name == "qualityAssuranceBy" ).ToList() )
			{
				roleId = filter.GetValueOrDefault( "RoleId", 0 );
				orgId = filter.GetValueOrDefault( "AgentId", 0 );
				where = where + AND + string.Format( agentRoleTemplate, roleId, orgId );
				AND = " AND ";
			}

			/* //Retained for reference
			foreach ( MainSearchFilter filter in data.Filters.Where( s => s.Name == "qualityAssuranceBy" ) )
			{
				if ( filter.Data.ContainsKey( "RoleId" ) )
					roleId = (int)filter.Data[ "RoleId" ];
				if ( filter.Data.ContainsKey( "AgentId" ) )
					orgId = ( int ) filter.Data[ "AgentId" ];
				where = where + AND + string.Format( agentRoleTemplate, roleId, orgId );
			}
			*/

			SetPropertiesFilter( data, ref where );

			SearchServices.SetRolesFilter( data, ref where );
			SearchServices.SetBoundariesFilter( data, ref where );
			//need to fix rowId

			//naics, ONET
			SetFrameworksFilter( data, ref where );
			//Competencies
			SetCompetenciesFilter( data, ref where, ref competencies );
			SetCredCategoryFilter( data, ref where ); //Not updated for FiltersV2 - I don't think we're using this anymore - NA 5/11/2017
			SetConnectionsFilter( data, ref where );

			TimeSpan timeDifference = start.Subtract( DateTime.Now );
			LoggingHelper.DoTrace( 5, thisClassName + string.Format( ".Search(). Filter: {0}, elapsed: {1} ", where, timeDifference.TotalSeconds ) );

			List<ThisSearchEntity> list = EntityMgr.Search( where, data.SortOrder, data.StartPage, data.PageSize, ref pTotalRows );

			//stopwatch.Stop();
			timeDifference = start.Subtract( DateTime.Now );
			LoggingHelper.DoTrace( 6, string.Format( "===CredentialServices.Search === Ended: {0}, Elapsed: {1}, Filter: {2}", DateTime.Now, timeDifference.TotalSeconds, where ) );
			return list;
		}

		private static void SetKeywordFilter( string keywords, bool isBasic, ref string where )
		{
			if ( string.IsNullOrWhiteSpace( keywords ) || string.IsNullOrWhiteSpace( keywords.Trim() ) )
				return;

			//trim trailing (org)
			if ( keywords.LastIndexOf( "(" ) > 0 )
				keywords = keywords.Substring( 0, keywords.LastIndexOf( "(" ) );

			//OR CreatorOrgs like '{0}' 
			bool isCustomSearch = false;
			//OR base.Description like '{0}'  OR base.SubjectWebpage like '{0}'
			string text = " (base.name like '{0}'  OR base.AlternateName like '{0}' OR OwningOrganization like '{0}'  ) ";
			//for ctid, needs a valid ctid or guid
			if ( keywords.IndexOf( "ce-" ) > -1 && keywords.Length == 39 )
			{
				text = " ( CTID = '{0}' ) ";
				isCustomSearch = true;
			}
			else if ( keywords.IndexOf( "in (" ) > -1 )
			{
				text = " base.Id  " + keywords;
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
			else if ( keywords.ToLower() == "[hascredentialregistryid]" )
			{
				text = " ( len(Isnull(CredentialRegistryId,'') ) = 36 ) ";
				isCustomSearch = true;
			}

			//use Entity.SearchIndex for all
			string indexFilter = " OR (base.Id in (SELECT c.id FROM [dbo].[Entity.SearchIndex] a inner join Entity b on a.EntityId = b.Id inner join Credential c on b.EntityUid = c.RowId where (b.EntityTypeId = 1 AND ( a.TextValue like '{0}') ))) ";

			//removed 10,11 as part of the frameworkItemSummary
			//string keywordsFilter = " OR (base.Id in (SELECT c.id FROM [dbo].[Entity.Reference] a inner join Entity b on a.EntityId = b.Id inner join Credential c on b.EntityUid = c.RowId where [CategoryId] = 35 and a.TextValue like '{0}' )) ";

			//  string subjects = " OR  (base.EntityUid in (SELECT EntityUid FROM [Entity_Subjects] a where EntityTypeId = 1 AND a.Subject like '{0}' )) ";

			//string frameworkItems = " OR (EntityUid in (SELECT EntityUid FROM [dbo].[Entity.FrameworkItemSummary_ForCredentials] a where  a.title like '{0}' ) ) ";

			// string otherFrameworkItems = " OR (EntityUid in (SELECT EntityUid FROM [dbo].[Entity_Reference_Summary] a where  a.TextValue like '{0}' ) ) ";
			string AND = "";
			if ( where.Length > 0 )
				AND = " AND ";
			//
			keywords = ServiceHelper.HandleApostrophes( keywords );
			if ( keywords.IndexOf( "%" ) == -1 && !isCustomSearch )
			{
				keywords = SearchServices.SearchifyWord( keywords );
				//keywords = "%" + keywords.Trim() + "%";
				//keywords = keywords.Replace( "&", "%" ).Replace( " and ", "%" ).Replace( " in ", "%" ).Replace( " of ", "%" );
				//keywords = keywords.Replace( " - ", "%" );
				//keywords = keywords.Replace( " % ", "%" );
			}

			//skip url  OR base.Url like '{0}' 
			if ( isBasic || isCustomSearch )
				where = where + AND + string.Format( " ( " + text + " ) ", keywords );
			else
				where = where + AND + string.Format( " ( " + text + indexFilter + " ) ", keywords );
			//where = where + AND + string.Format( " ( " + text + keywordsFilter + subjects + frameworkItems + otherFrameworkItems + " ) ", keywords );

		}

		private static void SetPropertiesFilter( MainSearchInput data, ref string where )
		{
			string AND = "";
			string searchCategories = UtilityManager.GetAppKeyValue( "credSearchCategories", "21,37," );
			SearchServices.SetPropertiesFilter( data, 1, searchCategories, ref where );
			//string template1 = " ( base.Id in ( SELECT  [EntityBaseId] FROM [dbo].[EntityProperty_Summary] where EntityTypeId= 1 AND [PropertyValueId] in ({0}))) ";
			//string template = " ( base.Id in ( SELECT  [EntityBaseId] FROM [dbo].[EntityProperty_Summary] where EntityTypeId= 1 AND {0} )) ";
			//string credTypes = " ( base.CredentialTypeId in ({0}) ) ";

			//string properyListTemplate = " ( [PropertyValueId] in ({0}) ) ";
			//string filterList = "";
			//int prevCategoryId = 0;
			////Updated to use FiltersV2
			//string next = "";
			//string typesFilter = "";
			//if ( where.Length > 0 )
			//    AND = " AND ";

			//var credSearchCategories = new List<int>();
			//foreach ( var s in searchCategories.Split( ',' ) )
			//    if ( !string.IsNullOrEmpty( s ) )
			//        credSearchCategories.Add( int.Parse( s ) );

			//foreach ( var filter in data.FiltersV2.Where( m => m.Type == MainSearchFilterV2Types.CODE ).ToList() )
			//{
			//    var item = filter.AsCodeItem();
			//    //if ( searchCategories.Contains( item.CategoryId.ToString() ) )
			//    if ( credSearchCategories.Contains( item.CategoryId ) )
			//    {
			//        if ( item.CategoryId == 2 )
			//        {
			//            typesFilter += item.Id + ",";
			//        }
			//        else
			//        {
			//            //18-03-27 mp - these are all property values, so using an AND with multiple categories will always fail - removing prevCategoryId check
			//            //if (item.CategoryId != prevCategoryId)
			//            //{
			//            //    if (prevCategoryId > 0)
			//            //    {
			//            //        next = next.Trim(',');
			//            //        filterList += (filterList.Length > 0 ? " AND " : "") + string.Format(properyListTemplate, next);
			//            //    }
			//            //    prevCategoryId = item.CategoryId;
			//            //    next = "";
			//            //}
			//            next += item.Id + ",";
			//        }
			//    }
			//}
			//next = next.Trim( ',' );
			//typesFilter = typesFilter.Trim( ',' );
			//if ( !string.IsNullOrWhiteSpace( next ) )
			//{
			//    //where = where + AND + string.Format( template, next );
			//    filterList += ( filterList.Length > 0 ? " AND " : "" ) + string.Format( properyListTemplate, next );
			//    where = where + AND + string.Format( template, filterList );
			//    AND = " AND ";
			//}
			//if ( !string.IsNullOrWhiteSpace( typesFilter ) )
			//{
			//    where = where + AND + string.Format( credTypes, typesFilter );
			//    AND = " AND ";
			//}
			/* //Retained for reference
			foreach ( MainSearchFilter filter in data.Filters)
			{
				if ( searchCategories.IndexOf( filter.CategoryId.ToString() ) > -1 )
				{
					string next = "";
					if ( where.Length > 0 )
						AND = " AND ";
					foreach ( string item in filter.Items )
					{
						next += item + ",";
					}
					next = next.Trim( ',' );
					where = where + AND + string.Format( template, next );
				}
			}
			*/
		}
		private static void SetFrameworksFilter( MainSearchInput data, ref string where )
		{
			string AND = "";
			//string codeTemplate = " (base.Id in (SELECT c.id FROM [dbo].[Entity_ReferenceFramework_Summary] a inner join Entity b on a.EntityId = b.Id inner join Credential c on b.EntityUid = c.RowId where [CategoryId] = {0} and ([CodeGroup] in ({1})  OR ([Name] in ({2}) )  )) ) ";

			string codeTemplate = " (base.Id in (SELECT c.id FROM [dbo].[Entity_ReferenceFramework_Summary] a inner join Entity b on a.EntityId = b.Id inner join Credential c on b.EntityUid = c.RowId where [CategoryId] = {0} and ([CodeGroup] in ({1})  OR ([ReferenceFrameworkId] in ({2}) )  )) ) ";

			//string codeTemplate2 = "  (base.Id in (SELECT c.id FROM [dbo].[Entity.FrameworkItemSummary] a inner join Entity b on a.EntityId = b.Id inner join Credential c on b.EntityUid = c.RowId where [CategoryId] = {0} and ([CodeGroup] in ({1})  OR ([CodedNotation] in ({2}) )  )) ) ";

			//string codeTemplate1 = "  (base.Id in (SELECT c.id FROM [dbo].[Entity.FrameworkItemSummary] a inner join Entity b on a.EntityId = b.Id inner join Credential c on b.EntityUid = c.RowId where [CategoryId] = {0} and [CodeId] in ({1}))  ) ";

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
				if ( item.CategoryId == 10 || item.Name == "industries" )
				{
					categoryID = item.CategoryId;
					if ( isTopLevel )
						groups += item.Id + ",";
					else
					{
						next += item.Id + ",";
						// next += "'" + item.Code + "',";
					}
				}
				else if ( item.CategoryId == 11 || item.Name == "occupations" )
				{
					categoryID = item.CategoryId;
					if ( isTopLevel )
						groups += item.Id + ",";
					else
					{
						next += item.Id + ",";
						//can't use code here, need to use codednotation?
						//next += "'" + item.Code + "',";
					}
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
		private static void SetCompetenciesAutocompleteFilter( string keywords, ref string where )
		{
			List<string> competencies = new List<string>();
			MainSearchInput data = new MainSearchInput();
			MainSearchFilter filter = new MainSearchFilter() { Name = "competencies", CategoryId = 29 };
			filter.Items.Add( keywords );
			SetCompetenciesFilter( data, ref where, ref competencies );

		}
		private static void SetCompetenciesFilter( MainSearchInput data, ref string where, ref List<string> competencies )
		{
			string AND = "";
			string OR = "";
			string keyword = "";
			//just learning opps
			//string template = " ( base.Id in (SELECT distinct  CredentialId FROM [dbo].[ConditionProfile_Competencies_Summary]  where AlignmentType in ('teaches', 'assesses') AND ({0}) ) ) ";
			//learning opps and asmts:
			string template = " ( base.Id in (SELECT distinct  CredentialId FROM [dbo].[ConditionProfile_Competencies_Summary]  where ({0}) ) ) ";
			//
			string phraseTemplate = " ([Name] like '%{0}%' OR [TargetNodeDescription] like '%{0}%') ";
			//

			//Updated to use FiltersV2
			string next = "";
			if ( where.Length > 0 )
				AND = " AND ";
			foreach ( var filter in data.FiltersV2.Where( m => m.Name == "competencies" ) )
			{
				var text = filter.AsText();

				//No idea what this is supposed to do
				try
				{
					if ( text.IndexOf( " - " ) > -1 )
					{
						text = text.Substring( text.IndexOf( " -- " ) + 4 );
					}
				}
				catch { }

				competencies.Add( text.Trim() );
				next += OR + string.Format( phraseTemplate, text.Trim() );
				OR = " OR ";

			}
			if ( !string.IsNullOrWhiteSpace( next ) )
			{
				where = where + AND + string.Format( template, next );
			}


		}
		//
		private static void SetCredCategoryFilter( MainSearchInput data, ref string where )
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
					where = where + AND + " ( base.CredentialTypeSchema <> 'qualityAssurance') ";
				else if ( item == "includeQualityAssurance" )  //IsAQAOrganization = true
					where = where + AND + " ( base.CredentialTypeSchema = 'qualityAssurance') ";
			}
		}

		public static void SetConnectionsFilter( MainSearchInput data, ref string where )
		{
			string AND = "";
			string OR = "";
			if ( where.Length > 0 )
				AND = " AND ";


			////Should probably get this from the database
			Enumeration entity = CodesManager.GetCredentialsConditionProfileTypes();

			var validConnections = new List<string>();
			//{ 
			//	"requires", 
			//	"recommends", 
			//	"requiredFor", 
			//	"isRecommendedFor", 
			//	//"renewal", //Not a connection type
			//	"isAdvancedStandingFor", 
			//	"advancedStandingFrom", 
			//	"preparationFor", 
			//	"preparationFrom", 
			//	"isPartOf", 
			//	"hasPart"	
			//};
			//validConnections = validConnections.ConvertAll( m => m.ToLower() ); //Makes comparisons easier when combined with the .ToLower() below
			validConnections = entity.Items.Select( s => s.SchemaName.ToLower() ).ToList();

			string conditionTemplate = " {0}Count > 0 ";

			//Updated for FiltersV2
			string next = "";
			string condition = "";
			if ( where.Length > 0 )
				AND = " AND ";
			foreach ( var filter in data.FiltersV2.Where( m => m.Type == MainSearchFilterV2Types.CODE ) )
			{
				var item = filter.AsCodeItem();
				if ( item.CategoryId != 15 )
				{
					continue;
				}

				//Prevent query hijack attacks
				if ( validConnections.Contains( item.SchemaName.ToLower() ) )
				{
					condition = item.SchemaName;
					next += OR + string.Format( conditionTemplate, condition );
					OR = " OR ";
				}
			}
			next = next.Trim();
			next = next.Replace( "ceterms:", "" );
			if ( !string.IsNullOrWhiteSpace( next ) )
			{
				where = where + AND + "(" + next + ")";
			}

		}

		#endregion

		#region Retrievals
		public static ThisEntity GetMinimumByCtid( string ctid )
        {
            ThisEntity entity = new ThisEntity();
            if ( string.IsNullOrWhiteSpace( ctid ) )
                return entity;

            return EntityMgr.GetMinimumByCtid( ctid );
        }
        public static ThisEntity GetDetailByCtid( string ctid, bool skippingCache = false )
        {
            ThisEntity entity = new ThisEntity();
            if ( string.IsNullOrWhiteSpace( ctid ) )
                return entity;
            var credential = EntityMgr.GetMinimumByCtid( ctid );
            
            return GetDetail( credential.Id, skippingCache );
            
        }

		/// <summary>
		/// Get a credential for detailed display
		/// </summary>
		/// <param name="id"></param>
		/// <param name="user"></param>
		/// <param name="skippingCache">If true, do not use the cached version</param>
		/// <returns></returns>
		public static ThisEntity GetDetail( int id, bool skippingCache = false )
		{
			CredentialRequest cr = new CredentialRequest();
			cr.IsDetailRequest();

			return GetDetail( id, cr, skippingCache );
		}



        /// <summary>
        /// Get a credential for detailed display
        /// </summary>
        /// <param name="id"></param>
        /// <param name="user"></param>
        /// <param name="skippingCache">If true, do not use the cached version</param>
        /// <returns></returns>
        public static ThisEntity GetDetail( int id, CredentialRequest cr, bool skippingCache = false )
        {
            //
            string statusMessage = "";
            int cacheMinutes = UtilityManager.GetAppKeyValue( "credentialCacheMinutes", 0 );
            DateTime maxTime = DateTime.Now.AddMinutes( cacheMinutes * -1 );

            string key = "credential_" + id.ToString();

            if ( skippingCache == false && cacheMinutes > 0 
                && HttpRuntime.Cache[ key ] != null)
            {
                var cache = ( CachedCredential )HttpRuntime.Cache[ key ];
                try
                {
                    if ( cache.LastUpdated > maxTime )
                    {
                        LoggingHelper.DoTrace( 6, string.Format( "===CredentialServices.GetCredentialDetail === Using cached version of Credential, Id: {0}, {1}", cache.Item.Id, cache.Item.Name ) );

                        return cache.Item;
                    }
                }
                catch ( Exception ex )
                {
                    LoggingHelper.DoTrace( 6, "===CredentialServices.GetCredentialDetail === exception " + ex.Message );
                }
            }
            else
            {
                LoggingHelper.DoTrace( 8, string.Format( "****** CredentialServices.GetCredentialDetail === Retrieving full version of credential, Id: {0}", id ) );
            }

            DateTime start = DateTime.Now;

            //CredentialRequest cr = new CredentialRequest();
            //cr.IsDetailRequest();

            ThisEntity entity = CredentialManager.GetForDetail( id, cr );

            DateTime end = DateTime.Now;
			var elasped = end.Subtract( start ).TotalSeconds;
			//Cache the output if more than specific seconds,
			//NOTE need to be able to force it for imports
			//&& elasped > 2
			if ( key.Length > 0 && cacheMinutes > 0 )
			{
                var newCache = new CachedCredential()
                {
                    Item = entity,
                    LastUpdated = DateTime.Now
                };
                if ( HttpContext.Current != null )
                {
                    if ( HttpContext.Current.Cache[ key ] != null )
                    {
                        HttpRuntime.Cache.Remove( key );
                        HttpRuntime.Cache.Insert( key, newCache );

                        LoggingHelper.DoTrace( 5, string.Format( "===CredentialServices.GetCredentialDetail $$$ Updating cached version of credential, Id: {0}, {1}", entity.Id, entity.Name ) );

                    }
                    else
                    {
                        LoggingHelper.DoTrace( 5, string.Format( "===CredentialServices.GetCredentialDetail ****** Inserting new cached version of credential, Id: {0}, {1}", entity.Id, entity.Name ) );

                        System.Web.HttpRuntime.Cache.Insert( key, newCache, null, DateTime.Now.AddMinutes( cacheMinutes ), TimeSpan.Zero );
                    }
                }
            }
            else
            {
                LoggingHelper.DoTrace( 7, string.Format( "===CredentialServices.GetCredentialDetail $$$$$$ skipping caching of credential, Id: {0}, {1}, elasped:{2}", entity.Id, entity.Name, elasped ) );
            }

            return entity;
        }

        /// <summary>
        /// Retrieve Credential for compare purposes
        /// - name, description, cred type, education level, 
        /// - industries, occupations
        /// - owner role
        /// - duration
        /// - estimated costs
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static ThisEntity GetCredentialForCompare( int id )
        {
            //not clear if checks necessary, as interface only allows selection of those to which the user has access.
            AppUser user = AccountServices.GetCurrentUser();

            LoggingHelper.DoTrace( 2, string.Format( "GetCredentialForCompare - using new compare get for cred: {0}", id ) );

            //================================================
            string statusMessage = "";
            string key = "credentialCompare_" + id.ToString();

            ThisEntity entity = new ThisEntity();

            if ( CacheManager.IsCredentialAvailableFromCache( id, key, ref entity ) )
            {
                //check if user can update the object
                string status = "";
                //if ( !CanUserUpdateCredential( id, user, ref status ) ) 
                //    entity.CanEditRecord = false;
                return entity;
            }

            CredentialRequest cr = new CredentialRequest();
            cr.IsCompareRequest();


            DateTime start = DateTime.Now;

            entity = CredentialManager.GetForCompare( id, cr );

            //if ( CanUserUpdateCredential( entity, user, ref statusMessage ) )
            //    entity.CanUserEditEntity = true;

            DateTime end = DateTime.Now;
            int elasped = ( end - start ).Seconds;
            if ( elasped > 1 )
                CacheManager.AddCredentialToCache( entity, key );

            return entity;
        } //


		#endregion

	}

	public class CachedCredential
    {
        public CachedCredential()
        {
            LastUpdated = DateTime.Now;
        }
        public DateTime LastUpdated { get; set; }
        public Credential Item { get; set; }

    }
}

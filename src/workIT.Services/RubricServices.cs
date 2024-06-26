using System;
using System.Collections.Generic;

using workIT.Factories;
using workIT.Models;
using workIT.Models.Common;
using workIT.Models.Search;
using workIT.Utilities;
using APIResourceServices = workIT.Services.API.RubricServices;
using Newtonsoft.Json;
using ElasticHelper = workIT.Services.ElasticServices;
using ResourceManager = workIT.Factories.RubricManager;
using ThisResource = workIT.Models.Common.Rubric;

namespace workIT.Services
{
	public class RubricServices
	{
		static string thisClassName = "RubricServices";
		public static string ThisEntityType = "Rubric";
		public static int ThisEntityTypeId = CodesManager.ENTITY_TYPE_RUBRIC;
		static string usingElasticSearch = "usingElasticRubricSearch";

		#region import
		public static ThisResource GetMinimumByCtid( string ctid )
		{
			ThisResource resource = new ThisResource();
			if ( string.IsNullOrWhiteSpace( ctid ) )
				return resource;

			return ResourceManager.GetMinimumByCtid( ctid );
		}
		public bool Import( ThisResource resource, ref SaveStatus status )
		{
			bool isValid = new ResourceManager().Save( resource, ref status );
			if ( resource.Id > 0 )
			{
				HandleComponents( resource, ref status );
				List<string> messages = new List<string>();
				//start storing the finder api ready version

				var apiDetail = APIResourceServices.GetDetailForAPI( resource.Id, true );
				if ( apiDetail != null && apiDetail.Meta_Id > 0 )
				{
					var resourceDetail = JsonConvert.SerializeObject( apiDetail, JsonHelper.GetJsonSettings( false ) );
					var statusMsg = "";
					if ( new EntityManager().EntityCacheUpdateResourceDetail( resource.CTID, resourceDetail, ref statusMsg ) == 0 )
					{
						status.AddError( statusMsg );
					}
				}
				//TODO - will need to update related elastic indices
				new SearchPendingReindexManager().Add( ThisEntityTypeId, resource.Id, 1, ref messages );

				//	NOTE: not sure if there is an organization
				new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, resource.PrimaryOrganizationId, 1, ref messages );
			}

			return isValid;
		}

		/// <summary>
		/// TODO
		/// </summary>
		/// <param name="resource"></param>
		/// <param name="status"></param>
		public void HandleComponents( ThisResource resource, ref SaveStatus status )
		{
			try
			{
				//components
				//parts saving these first as we need the criterion levels for Rubric Criterion and Rubric Level
				if ( resource.CriterionLevel?.Count > 0 )
				{
					List<string> messages = new List<string>();
					//deleting existing ones first
					new RubricCriterionLevelManager().DeleteAllForRubric( resource.Id, ref messages );
					foreach ( var item in resource.CriterionLevel )
					{
						var criterionLevel = new CriterionLevel();
						var recordExists = false;
						if ( HandleCriterionLevel( item, resource, ref criterionLevel, ref recordExists, ref status ) < 1 )
						{
							status.RecordsFailed++;
							continue;
						}
						else
						{
							if ( recordExists )
								status.RecordsUpdated++;
							else
								status.RecordsAdded++;
						}
					}
				}
				//
				if ( resource.RubricCriterion?.Count > 0 )
				{
					List<string> messages = new List<string>();
					new RubricCriterionManager().DeleteAllForRubric( resource.Id, ref messages );//deleting existing ones first
					foreach ( var item in resource.RubricCriterion )
					{
						var rubricCriterion = new RubricCriterion();
						var recordExists = false;
						if ( HandleRubricCriterion( item, resource, ref rubricCriterion, ref recordExists, ref status ) < 1 )
						{
							status.RecordsFailed++;
							continue;
						}
						else
						{
							if ( recordExists )
								status.RecordsUpdated++;
							else
								status.RecordsAdded++;
						}

					}
				}
				//
				if ( resource.RubricLevel?.Count > 0 )
				{
					List<string> messages = new List<string>();
					new RubricLevelManager().DeleteAllForRubric( resource.Id, ref messages );//deleting existing ones first
					foreach ( var item in resource.RubricLevel )
					{
						var rubricLevel = new RubricLevel();
						var recordExists = false;
						if ( HandleRubricLevel( item, resource, ref rubricLevel, ref recordExists, ref status ) < 1 )
						{
							status.RecordsFailed++;
							continue;
						}
						else
						{
							if ( recordExists )
								status.RecordsUpdated++;
							else
								status.RecordsAdded++;
						}
					}
				}
			}
			catch ( Exception ex )
			{
				var msg = $"{thisClassName}.HandleComponents(). Exception encountered. {ThisEntityType}: {resource.Name} ({resource.Id}), Message: {ex.Message}";
				LoggingHelper.DoTrace( 1, msg );
				//only fail current, and allow to continue
				status.AddError( msg );

				LoggingHelper.LogError( ex, msg );
			}
		}

		private int HandleRubricCriterion( RubricCriterion input, Rubric rubric, ref RubricCriterion output, ref bool recordExists, ref SaveStatus status )
		{
			List<string> messages = new List<string>();
			////the record may be ready to save??
			////
			//if ( DoesComponentExist( input, rubric.Id, ref output ) )
			//{
			//	input.Id = output.Id;
			//	input.RowId = output.RowId;
			//	input.RubricId = rubric.Id;
			//	recordExists = true;
			//	//delete any existing conditions
			//	//new Entity_ComponentConditionManager().DeleteAll( input, ref status );
			//}
			//else
			//{
			//if ( BaseFactory.IsGuidValid( input.RowId ) )
			//	output.RowId = input.RowId;
			//}

			//Always add as the old ones are deleted
			input.RubricId = rubric.Id;
			input.PrimaryAgentUID = rubric.PrimaryAgentUID;

			new RubricCriterionManager().Save( input, ref status );
			var ehclm = new Entity_HasCriterionLevelManager();
			var entity = EntityManager.GetEntity( input.RowId );
			//will prefer a replacement method, but start with the destructive version
			ehclm.DeleteAll( entity.Id, ref messages );
			var CriterionLevelIds = new List<int>();
			if ( input.HasCriterionLevelUids.Count > 0 )
			{
				foreach ( var criterionLevel in input.HasCriterionLevelUids )
				{
					if ( Guid.TryParse( criterionLevel.StartsWith( "_:" ) ? criterionLevel.Substring( 2 ):criterionLevel, out Guid rowId ) )//if it starts with _: replace it if not just use the criterionlevel
					{
						var CriterionLevel = RubricCriterionLevelManager.Get( rowId );// this should work as the criterion levels are added first
						if ( CriterionLevel.Id > 0 )
						{
							CriterionLevelIds.Add( CriterionLevel.Id );
						}
						else
						{
							messages.Add( string.Format( "CriterionLevel {0} is not found for Rubric Criterion", rowId, input.Name ) );
						}
					}
				}
				ehclm.SaveList( entity.Id, CriterionLevelIds, ref messages );
			}


			return input.Id;
		}
		private int HandleRubricLevel( RubricLevel input, Rubric rubric, ref RubricLevel output, ref bool recordExists, ref SaveStatus status )
		{
			List<string> messages = new List<string>();
			//the record may be ready to save??
			//
			//if ( DoesLevelExist( input, rubric.Id, ref output ) )
			//{
			//	input.Id = output.Id;
			//	input.RowId = output.RowId;
			//	input.RubricId = rubric.Id;
			//	recordExists = true;
			//	//delete any existing conditions
			//	//new Entity_ComponentConditionManager().DeleteAll( input, ref status );
			//}
			//else
			//{
			//if ( BaseFactory.IsGuidValid( input.RowId ) )
			//	output.RowId = input.RowId;
			//}

			//always an add as they are deleted
			input.RubricId = rubric.Id;
			input.PrimaryAgentUID = rubric.PrimaryAgentUID;

			new RubricLevelManager().Save( input, ref status );
			var ehclm = new Entity_HasCriterionLevelManager();
			var entity = EntityManager.GetEntity( input.RowId );
			//will prefer a replacement method, but start with the destructive version
			ehclm.DeleteAll( entity.Id, ref messages );
			var CriterionLevelIds = new List<int>();
			if ( input.HasCriterionLevelUids.Count > 0 )
			{
				foreach ( var rubricLevel in input.HasCriterionLevelUids )
				{
					if ( Guid.TryParse( rubricLevel.StartsWith( "_:" ) ? rubricLevel.Substring( 2 ) : rubricLevel, out Guid rowId ) )//if it starts with _: replace it if not just use the criterionlevel
					{
						var CriterionLevel = RubricCriterionLevelManager.Get( rowId );
						if ( CriterionLevel.Id > 0 )
						{
							CriterionLevelIds.Add( CriterionLevel.Id );
						}
						else
						{
							messages.Add( string.Format( "RubricLevel {0} is not found for Rubric Criterion", rowId, input.Name ) );
						}
					}
				}
				ehclm.SaveList( entity.Id, CriterionLevelIds, ref messages );
			}

			return input.Id;
		}
		private int HandleCriterionLevel( CriterionLevel input, Rubric rubric, ref CriterionLevel output, ref bool recordExists, ref SaveStatus status )
		{
			List<string> messages = new List<string>();
			//the record may be ready to save??
			//
			//always an add
				input.RubricId = rubric.Id;
				if ( BaseFactory.IsGuidValid( input.RowId ) )
					output.RowId = input.RowId;
			input.PrimaryAgentUID = rubric.PrimaryAgentUID;

			new RubricCriterionLevelManager().Save( input, ref status );


			return input.Id;
		}
		private bool DoesComponentExist( RubricCriterion input, int Id, ref RubricCriterion record )
		{
			bool isFound = false;

			var exists = RubricCriterionManager.GetByCTID( input.CTID );
			if ( exists != null && exists.Id > 0 )
			{
				record = exists;
				isFound = true;
			}

			return isFound;
		}
		private bool DoesLevelExist( RubricLevel input, int Id, ref RubricLevel record )
		{
			bool isFound = false;

			var exists = RubricLevelManager.GetByNameAndDescription( input.Name, input.Description );
			if ( exists != null && exists.Id > 0 )
			{
				record = exists;
				isFound = true;
			}

			return isFound;
		}
		#endregion


		#region Retrieval 

		public static ThisResource GetDetail( int profileId, bool skippingCache = false )
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

			//if ( UtilityManager.GetAppKeyValue( usingElasticSearch, true ) )
			//{
			var keywords = query.Keywords;
			return ElasticHelper.GeneralAutoComplete( ThisEntityTypeId, ThisEntityType, query, maxTerms, ref totalRows );
			//}
			//else
			//{
			//    string keywords = ServiceHelper.HandleApostrophes( query.Keywords );
			//    if ( keywords.IndexOf( "%" ) == -1 )
			//        keywords = "%" + keywords.Trim() + "%";
			//    where = string.Format( " (base.name like '{0}') ", keywords );

			//    SetKeywordFilter( keywords, true, ref where );
			//    return ResourceManager.Autocomplete( where, 1, maxTerms, ref totalRows );
			//}
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

			//OR base.Description like '{0}' 
			string text = " (base.name like '{0}' OR base.SubjectWebpage like '{0}'  OR base.PrimaryOrganizationName like '{0}'  ) ";
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

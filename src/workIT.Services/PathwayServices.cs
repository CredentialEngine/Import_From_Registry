using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using workIT.Models;
using workIT.Models.Common;
using ME = workIT.Models.Elastic;
using workIT.Models.Search;

using ThisEntity = workIT.Models.Common.Pathway;
using PathwayComponent = workIT.Models.Common.PathwayComponent;

using EntityMgr = workIT.Factories.PathwayManager;
using workIT.Utilities;
using workIT.Factories;

namespace workIT.Services
{
	public class PathwayServices
	{
		static string thisClassName = "PathwayServices";
		PathwayComponentConditionManager pccm = new PathwayComponentConditionManager();
		ActivityManager activityMgr = new ActivityManager();
		Entity_PathwayComponentManager epcmgr = new Entity_PathwayComponentManager();
		#region import

		public bool Import( ThisEntity entity, ref SaveStatus status )
		{
			//do a get, and add to cache before updating
			if ( entity.Id > 0 )
			{
				//need to force caching here
				//var detail = GetDetail( entity.Id );
			}
			bool isValid = new EntityMgr().Save( entity, ref status );
			List<string> messages = new List<string>();
			if ( entity.Id > 0 )
			{
				HandleComponents( entity, ref status );

				CacheManager.RemoveItemFromCache( "pathway", entity.Id );

				if ( UtilityManager.GetAppKeyValue( "delayingAllCacheUpdates", false ) == false )
				{
					//update cache
					new CacheManager().PopulateEntityRelatedCaches( entity.RowId );
					//update Elastic
					if ( UtilityManager.GetAppKeyValue( "usingElasticPathwaySearch", false ) )
						ElasticServices.Pathway_UpdateIndex( entity.Id );
					else
					{
						new SearchPendingReindexManager().Add( 8, entity.Id, 1, ref messages );
						if ( messages.Count > 0 )
							status.AddWarningRange( messages );
					}
				}
				else
				{
					new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_PATHWAY, entity.Id, 1, ref messages );
					new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_ORGANIZATION, entity.OwningOrganizationId, 1, ref messages );
					if ( messages.Count > 0 )
						status.AddWarningRange( messages );
				}
			}

			return isValid;
		}

		public void HandleComponents( ThisEntity pathway, ref SaveStatus status )
		{
			try
			{
				//components
				//delete all not in current list
				new Entity_PathwayComponentManager().DeleteNotInList( pathway.CTID, pathway.HasPart, ref status );
				//

				//TBD - should we do a fresh get of the pathway with components - or clear all?
				//handle components
				foreach ( var item in pathway.HasPart )
				{
					var component = new PathwayComponent();
					//handle each component
					//add to pathway HasParts on conclusion (with existance checking
					var recordExists = false;
					if ( HandlePathwayComponent( item, pathway, ref component, ref recordExists, ref status ) < 1 )
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

					//add pathway HasPart for component
					//?do we need has part in the finder?
					//will be useful to retrieve data for the detail page
					epcmgr.Add( pathway.RowId, component.Id, PathwayComponent.PathwayComponentRelationship_HasPart, ref status );

				}

				//handle conditions
				var candidates = pathway.HasPart.Where( s => s.HasCondition != null && s.HasCondition.Count() > 0 ).ToList();
				foreach ( var pc in candidates )
				{
					foreach ( var item in pc.HasCondition )
					{
						//get parent component
						var component = PathwayComponentManager.GetByCtid( pc.CTID );
						if ( component == null || component.Id == 0 )
						{
							//shouldn't happen here - although the add attempt could have failed?
							status.AddError( string.Format( "The parent pathway component: {0} for ConditionComponent: {1} was not found. This could have been due the an issue adding the component - which should have resulted in an earlier error message.", pc.Name, item.Name ) );
							continue;
						}
						var condition = new PathwayComponentCondition();
						//add to pathway component Entity.HasPathwayComponent on conclusion 

						if ( HandleComponentCondition( item, pathway, component, ref status ) < 1 )
						{
							status.RecordsFailed++;
							//could continue if have an id (i.e. failed after saved)?
							continue;
						}
					}
				}

				//now handle relationships
				int cntr = 0;
				foreach ( var item in pathway.HasPart )
				{
					cntr++;
					var component = PathwayComponentManager.GetByCtid( item.CTID, PathwayComponentManager.componentActionOfNone );
					//handle each component
					//add to pathway HasParts on conclusion (with existance checking
					ReplacePathwayComponentRelationships( cntr, component.RowId, item.HasChildList, pathway, PathwayComponent.PathwayComponentRelationship_HasChild, "PathwayComponent.HasChild", ref status );

					ReplacePathwayComponentRelationships( cntr, component.RowId, item.HasPrerequisiteList, pathway, PathwayComponent.PathwayComponentRelationship_Prerequiste, "PathwayComponent.Prerequisite", ref status );
					//
					ReplacePathwayComponentRelationships( cntr, component.RowId, item.HasPreceedsList, pathway, PathwayComponent.PathwayComponentRelationship_Preceeds, "PathwayComponent.Preceeds", ref status );

				}

				//these may have to been done after processing components
				//================ destination component
				ReplacePathwayToPathwayComponentRelationships( pathway.HasDestinationList, pathway, PathwayComponent.PathwayComponentRelationship_HasDestinationComponent, "Pathway.HasDestinationComponent", ref status );
				//
				ReplacePathwayToPathwayComponentRelationships( pathway.HasChildList, pathway, PathwayComponent.PathwayComponentRelationship_HasChild, "Pathway.HasChild", ref status );
			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".HandleComponents. Pathway: {0} ({1}) Exception encountered: {2}", pathway.Name, pathway.Id, ex.Message ) );
				//only fail current, and allow to continue
				status.AddError( string.Format( "Exception encountered. Pathway: {0}, Message: {1}", pathway.Name, ex.Message ) );

				LoggingHelper.LogError( ex, string.Format( thisClassName + ".HandleComponents. Pathway: {0} ({1}) Exception encountered",  pathway.Name, pathway.Id ) );
			}
		}

		private int HandlePathwayComponent( PathwayComponent input, Pathway pathway, ref PathwayComponent output, ref bool recordExists, ref SaveStatus status )
		{
			List<string> messages = new List<string>();
			//the record may be ready to save??
			//
			if ( DoesComponentExist( input, pathway.CTID, ref output ) )
			{
				input.Id = output.Id;
				recordExists = true;
				//delete any existing conditions
				new PathwayComponentConditionManager().DeleteAll( input.Id, status );
			}
			else
			{
				output.PathwayCTID = pathway.CTID;
				//if ( BaseFactory.IsGuidValid( input.RowId ) )
				//	output.RowId = input.RowId;
			}

			new PathwayComponentManager().Save( input, ref status );


			return input.Id;
		}
		private int HandleComponentCondition( PathwayComponentCondition input, Pathway pathway, PathwayComponent component, ref  SaveStatus status )
		{

			int newId = 0;
			List<string> messages = new List<string>();
			string statusMessage = "";
			input.ParentComponentId = component.Id;
			if ( pccm.Save( input, ref messages ) )
			{
				newId = input.Id;
				activityMgr.SiteActivityAdd( new SiteActivity()
				{
					ActivityType = "PathwayComponent",
					Activity = "Import",
					Event = "Add",
					Comment = string.Format( "Added PathwayComponentCondition via Import: '{0}' for Component: '{1}'", input.Name, component.Name ),
					ActivityObjectId = newId,

				} );
			}
			else
			{
				status.AddErrorRange( messages );
			}

			if ( newId == 0 || ( !string.IsNullOrWhiteSpace( statusMessage ) && statusMessage != "successful" ) )
			{
				status.AddError( string.Format( "Row: Issue encountered updating pathway ComponentCondition: {0} for Component: '{1}': {2}", input.Name, component.Name, statusMessage ) );
				return 0;
			}
			//==================================================


			//handle target components - better organization to move this to HandleComponentCondition since all components should now exist
			List<PathwayComponent> profiles = new List<PathwayComponent>();
			messages = new List<string>();
			foreach ( var tc in input.HasTargetComponentList )
			{
				var targetComponent = PathwayComponentManager.Get( tc);
				if ( targetComponent == null || targetComponent.Id == 0 )
				{
					//shouldn't happen here - although the add attempt could have failed?
					status.AddError( string.Format( "The target pathway component: {0} for ConditionComponent: {1} was not found. This could have been due the an issue adding the component - which should have resulted in an earlier error message.", tc, input.Name ) );
					continue;
				}
				profiles.Add( targetComponent );
			}
			//now replace relationships
			if ( !epcmgr.Replace( input.RowId, PathwayComponent.PathwayComponentRelationship_TargetComponent, profiles, ref status ) )
			{
				//status.AddErrorRange( messages );
			}

			return newId;
		}

		private void ReplacePathwayComponentRelationships( int componentNbr, Guid parentComponentUid, List<Guid> input, Pathway pathway, int pathwayComponentRelationship, string property,  ref SaveStatus status )
		{
			var pclist = new List<PathwayComponent>();
			foreach ( var pcGuid in input )
			{
				//look up component
				var pc = PathwayComponentManager.Get( pcGuid, PathwayComponentManager.componentActionOfNone );
				if ( pc != null && pc.Id > 0 )
				{
					pclist.Add( pc );
				}
				else
				{
					//???
					status.AddError( string.Format( "Component: {0}. Error unable to find PathwayComponent for relationship: {1} using pvGUID: {1}.", componentNbr, pathwayComponentRelationship, pcGuid ) );
				}
			}
			//do replace
			if ( !epcmgr.Replace( parentComponentUid, pathwayComponentRelationship, pclist, ref status ) )
			{
				//nothing more to report?
				//status.AddError( string.Format( "Component: {0}, Issue encountered replacing {1} component relationships.", componentNbr, property ));
			}
		}

		private void ReplacePathwayToPathwayComponentRelationships( List<Guid> input, Pathway pathway, int pathwayComponentRelationship, string property, ref SaveStatus status )
		{
			var pclist = new List<PathwayComponent>();
			foreach ( var pcGuid in input )
			{
				//look up component
				var pc = PathwayComponentManager.Get( pcGuid, PathwayComponentManager.componentActionOfNone );
				if ( pc != null && pc.Id > 0 )
				{
					pclist.Add( pc );
				}
				else
				{
					//???
					status.AddError( string.Format( "ReplacePathwayToPathwayComponentRelationships. Error unable to find record for Pathway.Component using Guid: {0}, for relationship: {1}.", pcGuid, pathwayComponentRelationship ) );
				}
			}
			//do replace
			if ( !epcmgr.Replace( pathway.RowId, pathwayComponentRelationship, pclist, ref status ) )
			{
				//status.AddErrorRange( string.Format( "Row: {0}, Issue encountered replacing {1} component relationships.", currentRowNbr, property ), messages );
			}
		}
		private bool DoesComponentExist( PathwayComponent input, string pathwayCTID, ref PathwayComponent record )
		{
			bool isFound = false;

			var exists = PathwayComponentManager.GetByCtid( input.CTID );
			if ( exists != null && exists.Id > 0 )
			{
				record = exists;
				isFound = true;
			}

			return isFound;
		}

		#endregion

		#region Retrievals
		public static ThisEntity GetByCtid( string ctid )
		{
			ThisEntity entity = new ThisEntity();
			if ( string.IsNullOrWhiteSpace( ctid ) )
				return entity;

			return EntityMgr.GetByCtid( ctid );
		}
		public static ThisEntity GetDetailByCtid( string ctid, bool skippingCache = false )
		{
			ThisEntity entity = new ThisEntity();
			if ( string.IsNullOrWhiteSpace( ctid ) )
				return entity;
			var record = EntityMgr.GetByCtid( ctid );

			return GetDetail( record.Id, skippingCache );
		}
		public static ThisEntity GetBasic( int id )
		{
			ThisEntity entity = EntityMgr.GetBasic( id );
			return entity;
		}
		public static ThisEntity GetDetail( int id, bool skippingCache = false )
		{

			ThisEntity entity = EntityMgr.GetDetails( id );


			return entity;
		}

		public static List<CommonSearchSummary> PathwaySearch( MainSearchInput data, ref int pTotalRows )
		{
			if ( UtilityManager.GetAppKeyValue( "usingElasticPathwaySearch", false ) )
			{
				return ElasticServices.PathwaySearch( data, ref pTotalRows );
			}
			else
			{
				List<CommonSearchSummary> results = new List<CommonSearchSummary>();
				var list = DoPathwaySearch( data, ref pTotalRows );
				foreach ( var item in list )
				{
					results.Add( new CommonSearchSummary()
					{
						Id = item.Id,
						Name = item.Name,
						FriendlyName = item.FriendlyName,
						Description = item.Description,
						SubjectWebpage = item.SubjectWebpage,
						PrimaryOrganizationName = item.PrimaryOrganizationName,
						PrimaryOrganizationId = item.OrganizationId,
						CTID = item.CTID,
						EntityTypeId = CodesManager.ENTITY_TYPE_PATHWAY,
						EntityType = "Pathway"
					} );
				}
				return results;
			}

		}//

		private static List<ThisEntity> DoPathwaySearch( MainSearchInput data, ref int totalRows )
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


			LoggingHelper.DoTrace( 5, "PathwayServices.Search(). Filter: " + where );
			return EntityMgr.Search( where, data.SortOrder, data.StartPage, data.PageSize, ref totalRows );
		}
		public static List<ThisEntity> GetPathwaysOwnedByOrg( int orgId, int maxRecords )
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

			LoggingHelper.DoTrace( 5, "PathwayServices.GetPathwaysOwnedByOrg(). Filter: " + where );
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
			string text = " (base.name like '{0}' OR base.SubjectWebpage like '{0}'  OR base.OrganizationName like '{0}'  ) ";
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
		//
		#endregion

		#region ComponentRetrievals
		public static PathwayComponent GetComponentByCtid( string ctid )
		{
			PathwayComponent entity = new PathwayComponent();
			if ( string.IsNullOrWhiteSpace( ctid ) )
				return entity;

			return PathwayComponentManager.GetByCtid( ctid );
		}

		public static PathwayComponent GetComponentBasic( int id )
		{
			PathwayComponent entity = PathwayComponentManager.Get( id );
			return entity;
		}



		#endregion


		#region PathwaySet Retrievals
		public bool PathwaySetImport( PathwaySet entity, ref SaveStatus status )
		{
			//do a get, and add to cache before updating
			if ( entity.Id > 0 )
			{
				//need to force caching here
				//var detail = GetDetail( entity.Id );
			}
			bool isValid = new PathwaySetManager().Save( entity, ref status );
			List<string> messages = new List<string>();
			if ( entity.Id > 0 )
			{
				if ( UtilityManager.GetAppKeyValue( "delayingAllCacheUpdates", false ) == false )
				{
					//no caching at this time
					//new CacheManager().PopulateEntityRelatedCaches( entity.RowId );
					//update Elastic
					if ( UtilityManager.GetAppKeyValue( "usingElasticPathwaySearch", false ) )
						ElasticServices.Pathway_UpdateIndex( entity.Id );
					else
					{
						new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_PATHWAY_SET, entity.Id, 1, ref messages );
						if ( messages.Count > 0 )
							status.AddWarningRange( messages );
					}
				}
				else
				{
					new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_PATHWAY_SET, entity.Id, 1, ref messages );
					new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_ORGANIZATION, entity.OwningOrganizationId, 1, ref messages );
					if ( messages.Count > 0 )
						status.AddWarningRange( messages );
				}
			}

			return isValid;
		}

		public static PathwaySet PathwaySetGetByCtid( string ctid )
		{
			var entity = new PathwaySet();
			if ( string.IsNullOrWhiteSpace( ctid ) )
				return entity;

			return PathwaySetManager.GetByCtid( ctid );
		}
		//public static PathwaySet PathwaySetGetDetailByCtid( string ctid, bool skippingCache = false )
		//{
		//	var entity = new PathwaySet();
		//	if ( string.IsNullOrWhiteSpace( ctid ) )
		//		return entity;
		//	var record = PathwaySetManager.GetByCtid( ctid );

		//	return PathwaySetGetDetail( record.Id, skippingCache );
		//}
		public static PathwaySet PathwaySetGetBasic( int id )
		{
			var entity = PathwaySetManager.Get( id );
			return entity;
		}
		public static PathwaySet PathwaySetGetDetail( int id )
		{

			var entity = PathwaySetManager.Get( id, true );
		
			return entity;
		}

		public static List<PathwaySetSummary> PathwaySetSearch( MainSearchInput data, ref int pTotalRows )
		{
			//if ( UtilityManager.GetAppKeyValue( "usingElasticPathwaySetSearch", false ) )
			//{
			//	return ElasticServices.PathwaySetSearch( data, ref pTotalRows );
			//}
			//else
			{
				//var results = new List<CommonSearchSummary>();
				var list = DoPathwaySetSearch( data, ref pTotalRows );
				//foreach ( var item in list )
				//{
				//	results.Add( new CommonSearchSummary()
				//	{
				//		Id = item.Id,
				//		Name = item.Name,
				//		Description = item.Description,
				//		SubjectWebpage = item.SubjectWebpage,
				//		PrimaryOrganizationName = item.PrimaryOrganizationName,
				//		CTID = item.CTID,
				//		EntityTypeId = CodesManager.ENTITY_TYPE_PATHWAY_SET,
				//		EntityType = "PathwaySet"
				//	} );
				//}
				return list;
			}

		}//
		public static List<CommonSearchSummary> PathwaySetSearch2( MainSearchInput data, ref int pTotalRows )
		{
			if ( UtilityManager.GetAppKeyValue( "usingElasticPathwaySetSearch", false ) )
			{
				return ElasticServices.PathwaySetSearch( data, ref pTotalRows );
			}
			else
			{
				List<CommonSearchSummary> results = new List<CommonSearchSummary>();
				var list = DoPathwaySetSearch( data, ref pTotalRows );
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
						EntityTypeId = CodesManager.ENTITY_TYPE_PATHWAY_SET,
						EntityType = "PathwaySet"
					} );
				}
				return results;
			}

		}//
		public static List<PathwaySetSummary> DoPathwaySetSearch( MainSearchInput data, ref int totalRows )
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


			LoggingHelper.DoTrace( 5, "PathwayServices.DoPathwaySetSearch(). Filter: " + where );
			return PathwaySetManager.Search( where, data.SortOrder, data.StartPage, data.PageSize, ref totalRows );
		}
		public static List<PathwaySetSummary> GetPathwaySetsOwnedByOrg( int orgId, int maxRecords )
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

			LoggingHelper.DoTrace( 5, "PathwayServices.GetPathwaySetsOwnedByOrg(). Filter: " + where );
			return PathwaySetManager.Search( where, "", 1, maxRecords, ref totalRows );
		}
		//
		//
		#endregion

	}
}

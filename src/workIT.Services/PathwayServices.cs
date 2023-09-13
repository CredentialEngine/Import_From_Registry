using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using workIT.Factories;
using workIT.Models;
using workIT.Models.Common;
using workIT.Models.Search;
using workIT.Utilities;

using ElasticHelper = workIT.Services.ElasticServices;
using EntityMgr = workIT.Factories.PathwayManager;
using PathwayComponent = workIT.Models.Common.PathwayComponent;
using PB = workIT.Models.PathwayBuilder;
using ThisEntity = workIT.Models.Common.Pathway;

namespace workIT.Services
{
    public class PathwayServices
	{
		static string thisClassName = "PathwayServices";
		//PathwayComponentConditionManager pccm = new PathwayComponentConditionManager();
		Entity_ComponentConditionManager pccm = new Entity_ComponentConditionManager();
		//
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
                //clear any existing cache
                string key = "pathwayWrapperByApi_" + entity.Id.ToString();
				ServiceHelper.ClearCacheEntity( key );
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
						ElasticHelper.Pathway_UpdateIndex( entity.Id );
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
					new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, entity.OwningOrganizationId, 1, ref messages );
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
				//NEW - trying a delete all first
				new Entity_ComponentConditionManager().DeleteAll( pathway.CTID, ref status );


				//need Entity or at least entity.EntityUid for processing conditions
				var candidates = pathway.HasPart.Where( s => s.HasCondition != null && s.HasCondition.Count() > 0 ).ToList();
				foreach ( var pc in candidates )
				{
					var resSummary = new ResourceSummary()
					{
						CTID = pc.CTID,
						RowId = pc.RowId,
						Name = pc.Name,
						Type = "PathwayComponent"

					};
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
						item.EntityId = component.RelatedEntityId;
						item.ParentIdentifier = component.RowId;
						//var condition = new ComponentCondition();
						//add to pathway component Entity.HasPathwayComponent on conclusion 

						if ( HandleComponentCondition( item, pathway, resSummary, ref status ) < 1 )
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
					var component = PathwayComponentManager.GetByCtid( item.CTID, PathwayComponentManager.ComponentActionOfNone );
					//handle each component
					//add to pathway HasParts on conclusion (with existance checking
					ReplacePathwayComponentRelationships( cntr, component.RowId, item.HasChildList, pathway, PathwayComponent.PathwayComponentRelationship_HasChild, "PathwayComponent.HasChild", ref status );
                    ReplacePathwayComponentRelationships( cntr, component.RowId, item.HasIsChildOfList, pathway, PathwayComponent.PathwayComponentRelationship_IsChildOf, "PathwayComponent.IsChildOf", ref status );
					//
                    ReplacePathwayComponentRelationships( cntr, component.RowId, item.HasPrecededByList, pathway, PathwayComponent.PathwayComponentRelationship_PrecededBy, "PathwayComponent.PrecededBy", ref status );
					ReplacePathwayComponentRelationships( cntr, component.RowId, item.HasPrecedesList, pathway, PathwayComponent.PathwayComponentRelationship_Precedes, "PathwayComponent.Precedes", ref status );
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

				LoggingHelper.LogError( ex, string.Format( thisClassName + ".HandleComponents. Pathway: {0} ({1}) Exception encountered", pathway.Name, pathway.Id ) );
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
				input.RowId = output.RowId;
				recordExists = true;
				//delete any existing conditions
				new Entity_ComponentConditionManager().DeleteAll( input, ref status );
			}
			else
			{
				output.PathwayCTID = pathway.CTID;
				//if ( BaseFactory.IsGuidValid( input.RowId ) )
				//	output.RowId = input.RowId;
			}
			input.PrimaryAgentUID = pathway.PrimaryAgentUID;

			new PathwayComponentManager().Save( input, ref status );


			return input.Id;
		}
		private int HandleComponentCondition( ComponentCondition input, Pathway pathway, ResourceSummary parent, ref SaveStatus status )
		{

			int newId = 0;
			List<string> messages = new List<string>();
			string statusMessage = "";
			//see how ParentComponentId is used, it should no longer be used
			//input.ParentComponentId = component.Id;
			input.ParentIdentifier = parent.RowId;
            //ensure populated!!
            //input.EntityId = component.RelatedEntityId;

            //component.CTID is specific to a component, need to handle where parent is a condition
            //the input.ParentIdentifier should be used, but needs to be passed into this method to be reuseable!
			//ensure Save populates input.RowId (and maybe EntityId?)
            if ( pccm.Save( input, parent.CTID, ref messages ) )
			{
				newId = input.Id;
				activityMgr.SiteActivityAdd( new SiteActivity()
				{
					ActivityType = "PathwayComponent",
					Activity = "Import",
					Event = "Add",
					Comment = string.Format( "Added PathwayComponentCondition via Import: '{0}' for Component: '{1}'", input.Name, parent.Name ),
					ActivityObjectId = newId,

				} );
			}
			else
			{
				status.AddErrorRange( messages );
			}

			if ( newId == 0 || ( !string.IsNullOrWhiteSpace( statusMessage ) && statusMessage != "successful" ) )
			{
				status.AddError( string.Format( "Row: Issue encountered updating pathway ComponentCondition: {0} for Component: '{1}': {2}", input.Name, parent.Name, statusMessage ) );
				return 0;
			}
			//==================================================


			//handle target components - better organization to move this to HandleComponentCondition since all components should now exist
			List<PathwayComponent> profiles = new List<PathwayComponent>();
			messages = new List<string>();
			foreach ( var tc in input.HasTargetComponentList )
			{
				var targetComponent = PathwayComponentManager.Get( tc );
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

            //handle subconditions
            var resSummary = new ResourceSummary()
            {
                RowId = input.RowId,
                Name = input.Name,
				Type = "ComponentCondition"

            };
            messages = new List<string>();
            foreach ( var tc in input.HasCondition )
            {
                var componentEntity = EntityManager.GetEntity( input.RowId, false );

                tc.ParentIdentifier = input.RowId;
				tc.EntityId = componentEntity.Id;
                if ( HandleComponentCondition( tc, pathway, resSummary, ref status ) < 1 )
                {
                    status.RecordsFailed++;
                    //could continue if have an id (i.e. failed after saved)?
                    continue;
                }
            }


			return newId;
		}

		private void ReplacePathwayComponentRelationships( int componentNbr, Guid parentComponentUid, List<Guid> input, Pathway pathway, int pathwayComponentRelationship, string property, ref SaveStatus status )
		{
			var pclist = new List<PathwayComponent>();
			foreach ( var pcGuid in input )
			{
				//look up component
				var pc = PathwayComponentManager.Get( pcGuid, PathwayComponentManager.ComponentActionOfNone );
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
				var pc = PathwayComponentManager.Get( pcGuid, PathwayComponentManager.ComponentActionOfNone );
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
		//

		public static string GetCTIDFromID( int id )
		{
			return EntityMgr.GetCTIDFromID( id );
		}
		//

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
				return ElasticHelper.PathwaySearch( data, ref pTotalRows );
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
						ResultNumber = item.ResultNumber,
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
			//OR base.SubjectWebpage like '{0}' 
			string text = " (base.name like '{0}'  OR base.OrganizationName like '{0}'  ) ";
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
				keywords = SearchServices.SearchifyWord( keywords, false );
			}

			//skip url  OR base.Url like '{0}' 
			if ( isBasic || isCustomSearch )
				where = where + AND + string.Format( " ( " + text + " ) ", keywords );
			else
				where = where + AND + string.Format( " ( " + text + " ) ", keywords );

		}
		//
		#endregion

		#region pathway builder methods

		public static PB.PathwayWrapper PathwayGraphGet( int id, bool skippingCache = false )
		{
			PB.PathwayWrapper output = new PB.PathwayWrapper();

            if ( UsingCache( id, skippingCache, ref output ) )
            {
                return output;
            }
            DateTime start = DateTime.Now;

            var entity = EntityMgr.GetDetails( id );
			if ( entity == null || entity.Id == 0 )
			{
				//or empty might be better
				return output;
			}
			
            DateTime end = DateTime.Now;
            int elasped = ( end - start ).Seconds;
            output = MapToPathwayWrapper( entity );
            if ( elasped > 4 && !skippingCache )
                CacheEntity( output );
            return output;
        }
		//
		public static PB.PathwayWrapper PathwayGraphGetByCtid( string ctid, bool skippingCache = false )
		{
			PB.PathwayWrapper output = new PB.PathwayWrapper();
			if ( string.IsNullOrWhiteSpace( ctid ) )
				return output;

			var pathway = EntityMgr.GetByCtid( ctid );
            if ( pathway == null || pathway.Id == 0 )
            {
                //or empty might be better
                return output;
            }
            if ( UsingCache( pathway.Id, skippingCache, ref output ) )
            {
                return output;
            }
            DateTime start = DateTime.Now;
            var entity = EntityMgr.GetDetails( pathway.Id );
            if ( entity == null || entity.Id == 0 )
            {
                //or empty might be better
                return output;
            }

            DateTime end = DateTime.Now;
            int elasped = ( end - start ).Seconds;
            output = MapToPathwayWrapper( entity );
            if ( elasped > 5 && !skippingCache )
                CacheEntity( output );
            return output;
        }
        private static bool UsingCache( int id, bool skippingCache, ref PB.PathwayWrapper output )
        {
            int cacheMinutes = UtilityManager.GetAppKeyValue( "pathwayCacheMinutes", 0 );
            DateTime maxTime = DateTime.Now.AddMinutes( cacheMinutes * -1 );
            string key = "pathwayWrapperByApi_" + id.ToString();

            if ( !skippingCache && HttpRuntime.Cache[key] != null && cacheMinutes > 0 )
            {
                try
                {
                    var cache = ( CachedPathwayWrapper ) HttpRuntime.Cache[key];
                    if ( cache.LastUpdated > maxTime )
                    {
                        LoggingHelper.DoTrace( BaseFactory.appSectionDurationTraceLevel, string.Format( thisClassName + ".UsingCache === Using cached version of record, Id: {0}, {1}", cache.Item.Pathway.Id, cache.Item.Pathway.Name ) );
                        output = cache.Item;
                        return true;
                    }
                }
                catch ( Exception ex )
                {
                    LoggingHelper.DoTrace( 6, thisClassName + ".UsingCache === exception " + ex.Message );
                }
            }
            else
            {
                LoggingHelper.DoTrace( 8, thisClassName + string.Format( ".UsingCache === Will retrieve full version of record, Id: {0}", id ) );
            }
            return false;
        }
        //
        private static void CacheEntity( PB.PathwayWrapper entity )
        {
            int cacheMinutes = UtilityManager.GetAppKeyValue( "pathwayCacheMinutes", 0 );
            DateTime maxTime = DateTime.Now.AddMinutes( cacheMinutes * -1 );
            string key = "pathwayWrapperByApi_" + entity.Pathway.Id.ToString();

            if ( key.Length > 0 && cacheMinutes > 0 )
            {
                try
                {
                    var newCache = new CachedPathwayWrapper()
                    {
                        Item = entity,
                        LastUpdated = DateTime.Now
                    };
                    if ( HttpContext.Current != null )
                    {
                        if ( HttpContext.Current.Cache[key] != null )
                        {
                            HttpRuntime.Cache.Remove( key );
                            HttpRuntime.Cache.Insert( key, newCache );

                            LoggingHelper.DoTrace( 7, string.Format( thisClassName + ".CacheEntity $$$ Updating cached version of record, Id: {0}, {1}", entity.Pathway.Id, entity.Pathway.Name ) );
                        }
                        else
                        {
                            LoggingHelper.DoTrace( 7, string.Format( thisClassName + ".CacheEntity ****** Inserting new cached version of record, Id: {0}, {1}", entity.Pathway.Id, entity.Pathway.Name ) );

                            System.Web.HttpRuntime.Cache.Insert( key, newCache, null, DateTime.Now.AddMinutes( cacheMinutes ), TimeSpan.Zero );
                        }
                    }
                }
                catch ( Exception ex )
                {
                    LoggingHelper.DoTrace( 6, thisClassName + ".CacheEntity. Updating Cache === exception " + ex.Message );
                }
            }
        }

        //
        public static PB.PathwayWrapper MapToPathwayWrapper( Pathway entity )
		{
			var output = new PB.PathwayWrapper();
			output.Pathway = new PB.Pathway()
			{
				Id = entity.Id,
				RowId = entity.RowId,
				Name = entity.Name,
                FriendlyName = entity.FriendlyName,
                Description = entity.Description,
				CTID = entity.CTID,
				SubjectWebpage = entity.SubjectWebpage,
				Created = entity.Created,
				LastUpdated = entity.LastUpdated

			};
			if ( entity.PrimaryOrganization != null && entity.PrimaryOrganization.Id > 0 )
			{
				output.Pathway.Organization = new ResourceSummary()
				{
					Id = entity.PrimaryOrganization.Id,
					RowId = entity.PrimaryOrganization.RowId,
					Name = entity.PrimaryOrganization.Name,
					CTID = entity.PrimaryOrganization.CTID,
				};
			}

			output.Pathway.Subject = BaseFactory.MapTextValueProfileToString( entity.Subject );
			output.Pathway.Keyword = BaseFactory.MapTextValueProfileToString( entity.Keyword );
			//
			output.Pathway.OccupationType = MapToResourceSummary( entity.OccupationTypes );
			output.Pathway.IndustryType = MapToResourceSummary( entity.IndustryTypes );
			output.Pathway.InstructionalProgramType = MapToResourceSummary( entity.InstructionalProgramTypes);
            //
            if ( entity.HasSupportService != null && entity.HasSupportService.Any() )
            {
                output.Pathway.HasSupportService = MapFromResourceSummaryList( entity.HasSupportService );
            }

            if ( BaseFactory.IsValidCtid( entity.ProgressionModelURI ) )
			{
				output.Pathway.HasProgressionModel.Add( entity.ProgressionModelURI );
				MapProgressionModel( entity.HasProgressionModel, ref output );
			}

			foreach ( var item in entity.HasDestinationComponent )
			{
				output.Pathway.HasDestinationComponent = item.CTID;
				break;
			}

			foreach ( var item in entity.HasPart )
			{
				MapToComponent( output, item );
			}
			return output;
		}
        public static void MapToComponent( PB.PathwayWrapper wrapper, PathwayComponent input )
        {

            var output = new PB.PathwayComponent()
            {
                //TODO - get schema from PathwayComponent and populate Type
                Id = input.Id,
                Type = "ceterms:" + input.PathwayComponentType.Replace( " ", "" ),
                PathwayComponentTypeId = input.ComponentTypeId,
                //we are more concerned with the hasRelations not isPartOf relationships!!
                ComponentRelationshipTypeId = input.ComponentRelationshipTypeId,
                PathwayCTID = input.PathwayCTID,
                RowId = input.RowId,
                Name = input.Name,
                Description = input.Description,
                CTID = input.CTID,
                SubjectWebpage = input.SubjectWebpage,
                Created = input.Created,
                LastUpdated = input.LastUpdated,
                ComponentCategory = input.ComponentCategory,
                CredentialType = input.CredentialType,
                ProgramTerm = input.ProgramTerm,
                ProxyFor = input.ProxyFor,
                RowNumber = input.RowNumber,
                ColumnNumber = input.ColumnNumber,
            };
            var isDestinationComponent = false;
            if ( wrapper.Pathway.HasDestinationComponent == input.CTID )
            {
                isDestinationComponent = true;
            }

            //may want to do this after everything is in the wrapper. For ex, will need to add row/col for a dest comp condition
			//this was a HACK if no data, should skip now
            //if ( input.RowNumber == 0 )
            //{
            //    if ( isDestinationComponent )
            //    {
            //        output.RowNumber = 1;
            //        output.ColumnNumber = 1;
            //    }
            //    output.RowNumber = 1;
            //}
            //if ( input.ColumnNumber == 0 )
            //{
            //    output.ColumnNumber = 1;
            //}
            output.FinderResource = null;

            if ( input.FinderResource != null && !string.IsNullOrWhiteSpace( input.FinderResource.CTID ) )
                output.FinderResource = input.FinderResource;
            //List of ComponentDesignation 
            if ( input.ComponentDesignationList != null && input.ComponentDesignationList.Any() )
                output.ComponentDesignation = input.ComponentDesignationList;

            output.Identifier = input.Identifier;
            output.PointValue = input.PointValue;
            if ( output.PointValue != null )
                if ( output.PointValue.Value == 0 && output.PointValue.MinValue == 0 && output.PointValue.MaxValue == 0 && output.PointValue.Percentage == 0
                && string.IsNullOrWhiteSpace( output.PointValue.Description ) )
                {
                    //OR user may not be able to address this
                    output.PointValue = null;
                }
            var hasConnections = false;
            if ( input.ProgressionLevels != null && input.ProgressionLevels.Any())
                foreach ( var item in input.ProgressionLevels )
                {
                    output.HasProgressionLevel = item.CTID;
                    //only one allowed for now
                    break;
                }
            else if ( input.HasProgressionLevels != null && input.HasProgressionLevels.Any() ) //this handles where source is RegistryResource
                foreach ( var item in input.HasProgressionLevels )
                {
                    output.HasProgressionLevel = item;
                    //only one allowed for now
                    break;
                }
            else if ( !string.IsNullOrWhiteSpace( output.HasProgressionLevel ))
                output.HasProgressionLevel = input.HasProgressionLevel;

            if ( input.HasChild != null )
                foreach ( var item in input.HasChild )
                {
                    if ( BaseFactory.IsValidCtid( item.CTID ) )
                    {
                        output.HasChild.Add( item.CTID );
                        //23-02-23 TODO do we need to populate precededBy as well? Not sure hasChild is handled?
                    }
                    hasConnections = true;
                }

            if ( input.IsChildOf != null )
                foreach ( var item in input.IsChildOf )
                {
                    output.IsChildOf.Add( item.CTID );
                    hasConnections = true;
                }
            if ( input.Precedes != null )
                foreach ( var item in input.Precedes )
                {
                    output.Precedes.Add( item.CTID );
                    hasConnections = true;
                }
            if ( input.PrecededBy != null )
                foreach ( var item in input.PrecededBy )
                {
                    output.PrecededBy.Add( item.CTID );
                    hasConnections = true;
                }
            //
            if ( isDestinationComponent )
            {
                if ( ( output.PrecededBy == null || !output.PrecededBy.Any() )
                    && ( output.HasChild != null && output.HasChild.Any() )
                    )
                {
                   // output.PrecededBy = output.HasChild; // has child is not the same as precededby
                   
                }
            }


            if ( input.CreditValue != null )
                foreach ( var item in input.CreditValue )
                {
                    output.CreditValue.Add( new ValueProfile()
                    {
                        //UnitText = item.UnitText,
                        CreditLevelType = item.CreditLevelType,
                        CreditUnitType = item.CreditUnitType,
                        Description = item.Description,
                        Value = item.Value,
                        MinValue = item.MinValue,
                        MaxValue = item.MaxValue,
                        Percentage = item.Percentage,
                        Subject = item.Subject
                    } );
                }
            if ( input.HasCondition != null )
                foreach ( var cc in input.HasCondition )
                {
                    output.HasCondition.Add( cc.RowId );
                    MapComponentCondition( wrapper, cc );
                    hasConnections = true;
                }
            //TODO - designate a pending component - how to do based on content? Absense of connections?
            //	note : ensure a designation component is not incorrectly sorted!
            if ( !hasConnections )
            {
                if ( isDestinationComponent )
                    hasConnections = true;

                if ( !string.IsNullOrWhiteSpace( output.HasProgressionLevel ) )
                    hasConnections = true;
            }
            output.OccupationType = MapToResourceSummary( input.OccupationTypes );
            output.IndustryType = MapToResourceSummary( input.IndustryTypes );

            //add
            wrapper.PathwayComponents.Add( output );

        }
        //
        public static void MapComponentCondition( PB.PathwayWrapper wrapper, ComponentCondition input )
        {
            var output = new PB.ComponentCondition()
            {
                //
                ParentIdentifier = input.ParentIdentifier,
                Name = input.Name,
                Description = input.Description,
                RowId = input.RowId,
                PathwayCTID = input.PathwayCTID,
                Created = input.Created,
                LastUpdated = input.LastUpdated,
                RequiredNumber = input.RequiredNumber,
                LogicalOperator = input.LogicalOperator,
                RowNumber = input.RowNumber,
                ColumnNumber = input.ColumnNumber,
				HasProgressionLevel= input.HasProgressionLevel,
            };
			//TODO: this was a helper in the publisher. We will need to try to populate in the import?
			//if ( input.ConditionProperties != null )
			//{
			//	output.HasProgressionLevel = input.ConditionProperties.HasProgressionLevel;
			//}

            if ( input.HasConstraint != null && input.HasConstraint.Any() )
            {
                foreach ( var item in input.HasConstraint )
                {
                    if ( item != null )
                    {
                        //ensure we have a ParentIdentifier
                        item.ParentIdentifier = input.ParentIdentifier;
                        wrapper.Constraints.Add( item );
						//constraints will not have a Guid. Import will need to add one
                        output.HasConstraint.Add( item.RowId );
                    }
                }
            }

            if ( input.HasCondition != null && input.HasCondition.Count > 0 )
            {
                foreach ( var cc in input.HasCondition )
                {
                    //output.HasCondition.Add( MapComponentCondition( cc ) );
                    output.HasCondition.Add( cc.RowId );
                    MapComponentCondition( wrapper, cc );
                }
            }
            var defaultProgressionLevel = "";
            foreach ( var item in input.TargetComponent )
            {
                //if ( UsingCTIDsForComponents )
                output.TargetComponent.Add( item.CTID );
                //else
                //	output.TargetComponent.Add( item.RowId.ToString() );
                if ( string.IsNullOrWhiteSpace( defaultProgressionLevel ) && ( item.ProgressionLevels.Any() ) )
                    defaultProgressionLevel = item.ProgressionLevels[0]?.CTID;
            }

			//TODO - can we derive a good progression level if none present?
			//		if no progression model, then no known default. Need to work with Protiviti for a solution
			if ( input.ConditionProperties != null )
			{
				if ( string.IsNullOrWhiteSpace( input.ConditionProperties.HasProgressionLevel ) && wrapper.ProgressionModels.Any() )
				{
					//????
					//put in the same one as targets? 
					//or other than if referenced from a dest component, add to the same progression level?
					if ( !string.IsNullOrWhiteSpace( defaultProgressionLevel ) )
					{
						//output.HasProgressionLevel = defaultProgressionLevel;
					}
					else
					{
						//check parent (could be a condition)
					}
				}
			}
            wrapper.ComponentConditions.Add( output );
          
        }

        //
        public static void MapProgressionModel( ConceptScheme input, ref PB.PathwayWrapper output )
        {
            if ( input == null || string.IsNullOrWhiteSpace( input.Name ) )
                return;

            //can we do this?
            var progressionModel = UtilityManager.SimpleMap<PB.ProgressionModel>( input ) ?? new PB.ProgressionModel();
            //may want to defer this to populate hasTopConcept
            var hasTopConcepts = true;
            if ( progressionModel.HasTopConcept == null || progressionModel.HasTopConcept.Count == 0 )
            {
                hasTopConcepts = false;
                progressionModel.HasTopConcept = new List<string>();
            }
            //output.ProgressionModels.Add( progressionModel );
            //in case we want the progression levels separate
            //TODO - handle broader/narrower
            foreach ( var item in input.HasConcepts )
            {
                var progressionLevel = UtilityManager.SimpleMap<PB.ProgressionLevel>( item ) ?? new PB.ProgressionLevel();
                progressionLevel.Name = item.PrefLabel;
                progressionLevel.InProgressionModel = input.CTID;
                if ( item.IsTopConcept && hasTopConcepts == false )
                {
                    progressionModel.HasTopConcept.Add( progressionLevel.CTID );
                }
                //chg to use the guid
                output.ProgressionLevels.Add( progressionLevel );
            }
            output.ProgressionModels.Add( progressionModel );
        }
        //
        public static List<ResourceSummary> MapFromResourceSummaryList( List<ResourceSummary> input )
        {
            //
            var output = new List<ResourceSummary>();
            var entity = new ResourceSummary();
            if ( input == null || input.Count == 0 )
                return output;

            foreach ( var item in input )
            {
                entity = new ResourceSummary()
                {
                    Type = item.Type,
                    Id = item.Id,
                    Name = item.Name,
                    Description = item.Description,
                    CTID = item.CTID,
                    URI = item.URI,
                    CodedNotation = item.CodedNotation,
                    RowId = item.RowId,
                };
                output.Add( entity );
            }

            return output;
        }
        //
        public static List<ResourceSummary> MapFromResourceSummaryList( List<TopLevelObject> input )
        {
            //
            var output = new List<ResourceSummary>();
            var entity = new ResourceSummary();
            if ( input == null || input.Count == 0 )
                return output;

            foreach ( var item in input )
            {
                entity = new ResourceSummary()
                {
                    Type = item.EntityType,
                    Id = item.Id,
                    Name = item.Name,
                    Description = item.Description,
                    CTID = item.CTID,
                    URI = item.SubjectWebpage,
                    RowId = item.RowId,
                };
                output.Add( entity );
            }

            return output;
        }
        //
        public static List<ResourceSummary> MapResourceSummary( Enumeration input )
        {
            var output = new List<ResourceSummary>();
            if ( input == null || input.Items.Count == 0 )
                return output;

            foreach ( var item in input.Items )
            {
                ResourceSummary resourceSummary = new ResourceSummary()
                {
                    Id = item.Id,
                    Name = item.Name,
                    RowId = item.RowId != null ? Guid.Parse( item.RowId ) : Guid.Empty, //actually no GUID at this time
                    Description = item.Description,
                    CodedNotation = item.Value, //TBD - ensure provided
                };
                output.Add( resourceSummary );
            }

            return output;
        }
        public static List<ResourceSummary> MapToResourceSummary( List<CredentialAlignmentObjectProfile> input )
        {
            var output = new List<ResourceSummary>();
            if ( input == null || input.Count == 0 )
                return output;

            foreach ( var item in input )
            {
                ResourceSummary resourceSummary = new ResourceSummary()
                {
                    Id = item.Id,
                    Name = item.TargetNodeName,
                    RowId = item.RowId != null ? item.RowId : Guid.Empty, //actually no GUID at this time
                    Description = item.Description,
                    CodedNotation = item.CodedNotation,
                };
                output.Add( resourceSummary );
            }

            return output;
        }
        //
        public static List<CredentialAlignmentObjectProfile> MapFromResourceSummary( List<ResourceSummary> input, int categoryId )
        {
            //TODO need a lighter class than  this
            var output = new List<CredentialAlignmentObjectProfile>();
            var entity = new CredentialAlignmentObjectProfile();
            if ( input == null || input.Count == 0 )
                return output;

            foreach ( var item in input )
            {
                entity = new CredentialAlignmentObjectProfile()
                {
                    CategoryId = categoryId,
                    TargetNode = item.URI ?? "",
                    TargetNodeName = item.Name,
                    CodedNotation = item.CodedNotation ?? "",
                    TargetNodeDescription = item.Description
                    //derive this
                    //FrameworkName = item.,
                };
                output.Add( entity );
            }

            return output;
        }

        #region schema methods for pb
        /// <summary>
        /// Get all pathway component concepts
        /// </summary>
        /// <param name="getAll">If true, a component condition will be included.</param>
        /// <returns></returns>
        public static List<PB.Concept> GetPathwayComponentConcepts()
        {
            bool includeComponentCondition = false;
            var enumeration = CodesManager.PathwayComponentTypesAsEnumeration( includeComponentCondition );
            return ConvertEnumerationToConceptList( enumeration );
        }
        //
        /// <summary>
        /// Get all credential type concepts
        /// </summary>
        /// <param name="getAll">true: getAll, false: only get those with totals</param>
        /// <returns></returns>
        public static List<PB.Concept> GetCredentialTypeConcepts( bool getAll = true )
        {
            var enumeration = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_CREDENTIAL_TYPE, true );
            return ConvertEnumerationToConceptList( enumeration );
        }
        //
        public static List<PB.Concept> GetArrayOperationConcepts( bool getAll = true )
        {
            var enumeration = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_ARRAY_OPERATION_CATEGORY, getAll );
            return ConvertEnumerationToConceptList( enumeration );
        }
        //

        public static List<PB.Concept> GetLogicalOperatorConcepts( bool getAll = true )
        {
            var enumeration = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_LOGICAL_OPERATOR_CATEGORY, getAll );
            return ConvertEnumerationToConceptList( enumeration );
        }
        //

        public static List<PB.Concept> GetComparatorConcepts( bool getAll = true )
        {
            var enumeration = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_COMPARATOR_CATEGORY, getAll );
            return ConvertEnumerationToConceptList( enumeration );
        }
        //
        public static List<PB.Concept> GetCreditUnitTypeConcepts( bool getAll = true )
        {
            var enumeration = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_CREDIT_UNIT_TYPE, getAll );
            return ConvertEnumerationToConceptList( enumeration );
        }
        //

        public static List<PB.Concept> GetCreditLevelTypeConcepts( bool getAll = true )
        {
            var enumeration = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL, getAll );
            return ConvertEnumerationToConceptList( enumeration );
        }
        private static List<PB.Concept> ConvertEnumerationToConceptList( Enumeration enumeration )
        {
            return enumeration?.Items?.Select( item =>
            {
                return new PB.Concept()
                {
                    Id = item.Id,
                    Name = item.Name,
                    Description = item.Description,
                    //Any CodedNotation
                    URI = !string.IsNullOrWhiteSpace( item.SchemaName ) ? item.SchemaName : !string.IsNullOrWhiteSpace( item.SchemaUrl ) ? item.SchemaUrl : item.Name,
                    //icon?
                    Icon = !string.IsNullOrWhiteSpace( item.Icon ) ? item.Icon : ""
                };
            } ).ToList() ?? new List<PB.Concept>();
        }
        //
        #endregion

        #endregion

        #region ComponentRetrievals
        public static PathwayComponent GetComponentByCtid( string ctid, int childComponentsAction = 1 )
		{
			PathwayComponent entity = new PathwayComponent();
			if ( string.IsNullOrWhiteSpace( ctid ) )
				return entity;
			//get component with pathway
			return PathwayComponentManager.GetByCtid( ctid, childComponentsAction, true );
		}

		public static PathwayComponent GetComponent( int id, int childComponentsAction = 1 )
		{
            //get component with pathway
            PathwayComponent entity = PathwayComponentManager.Get( id, childComponentsAction, true );
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
						ElasticHelper.Pathway_UpdateIndex( entity.Id );
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
					new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, entity.OwningOrganizationId, 1, ref messages );
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

		public static List<CommonSearchSummary> PathwaySetSearch( MainSearchInput data, ref int pTotalRows )
		{
            if ( UtilityManager.GetAppKeyValue( "usingElasticPathwaySetSearch", false ) )
            {
                return ElasticHelper.PathwaySetSearch( data, ref pTotalRows );
            }
            else
            {
				var results = new List<CommonSearchSummary>();
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
    public class CachedPathwayWrapper
    {
        public CachedPathwayWrapper()
        {
            LastUpdated = DateTime.Now;
        }
        public DateTime LastUpdated { get; set; }
        public PB.PathwayWrapper Item { get; set; }

    }
}

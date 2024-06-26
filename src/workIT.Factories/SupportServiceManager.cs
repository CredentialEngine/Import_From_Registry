using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Newtonsoft.Json;
using workIT.Models;
using workIT.Models.Common;
using workIT.Models.ProfileModels;
using workIT.Utilities;
using DBResource = workIT.Data.Tables.SupportService;
using EntityContext = workIT.Data.Tables.workITEntities;
using ThisResource = workIT.Models.Common.SupportService;

namespace workIT.Factories
{
    public class SupportServiceManager : BaseFactory
    {
        static readonly string thisClassName = "SupportServiceManager";
        static string EntityType = "SupportService";
        static int EntityTypeId = CodesManager.ENTITY_TYPE_SUPPORT_SERVICE;
        static string Entity_Label = "Support Service";
        static string Entities_Label = "Support Services";

        #region SupportService - persistance ==================
        /// <summary>
        /// Update a SupportService
        /// - base only, caller will handle parts?
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public bool Save( ThisResource entity, ref SaveStatus status )
        {
            bool isValid = true;
            int count = 0;
            try
            {
                using ( var context = new EntityContext() )
                {
                    if ( ValidateProfile( entity, ref status ) == false )
                        return false;

                    if ( entity.Id > 0 )
                    {
                        //TODO - consider if necessary, or interferes with anything
                        context.Configuration.LazyLoadingEnabled = false;
                        DBResource efEntity = context.SupportService
                                .SingleOrDefault( s => s.Id == entity.Id );

                        if ( efEntity != null && efEntity.Id > 0 )
                        {
                            //fill in fields that may not be in entity
                            entity.RowId = efEntity.RowId;

                            MapToDB( entity, efEntity );

                            //19-05-21 mp - should add a check for an update where currently is deleted
                            if ( ( efEntity.EntityStateId ) == 0 )
                            {
                                SiteActivity sa = new SiteActivity()
                                {
                                    ActivityType = "SupportService",
                                    Activity = "Import",
                                    Event = "Reactivate",
                                    Comment = string.Format( "SupportService had been marked as deleted, and was reactivted by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ),
                                    ActivityObjectId = entity.Id
                                };
                                new ActivityManager().SiteActivityAdd( sa );
                            }
                            //assume and validate, that if we get here we have a full record
                            if ( efEntity.EntityStateId != 2 )
                                efEntity.EntityStateId = 3;
                            entity.EntityStateId = efEntity.EntityStateId;

                            if ( IsValidDate( status.EnvelopeCreatedDate ) && status.LocalCreatedDate < efEntity.Created )
                            {
                                efEntity.Created = status.LocalCreatedDate;
                            }
                            if ( IsValidDate( status.EnvelopeUpdatedDate ) && status.LocalUpdatedDate != efEntity.LastUpdated )
                            {
                                efEntity.LastUpdated = status.LocalUpdatedDate;
                            }
                            if ( HasStateChanged( context ) )
                            {
                                if ( IsValidDate( status.EnvelopeUpdatedDate ) )
                                    efEntity.LastUpdated = status.LocalUpdatedDate;
                                else
                                    efEntity.LastUpdated = DateTime.Now;
                                //NOTE efEntity.EntityStateId is set to 0 in delete method )
                                count = context.SaveChanges();
                                //can be zero if no data changed
                                if ( count >= 0 )
                                {
                                    entity.LastUpdated = efEntity.LastUpdated;
                                    UpdateEntityCache( entity, ref status );
                                    isValid = true;
                                }
                                else
                                {
                                    //?no info on error
                                    isValid = false;
                                    string message = string.Format( thisClassName + ".Save Failed", "Attempted to update a SupportService. The process appeared to not work, but was not an exception, so we have no message, or no clue. SupportService: {0}, Id: {1}", entity.Name, entity.Id );
                                    status.AddError( "Error - the update was not successful. " + message );
                                    EmailManager.NotifyAdmin( thisClassName + ".Save Failed Failed", message );
                                }

                            }
                            else
                            {
                                //update entity.LastUpdated - assuming there has to have been some change in related data
                                new EntityManager().UpdateModifiedDate( entity.RowId, ref status, efEntity.LastUpdated );
                            }
                            if ( isValid )
                            {
                                if ( !UpdateParts( entity, ref status ) )
                                    isValid = false;

                                SiteActivity sa = new SiteActivity()
                                {
                                    ActivityType = "SupportService",
                                    Activity = "Import",
                                    Event = "Update",
                                    Comment = string.Format( "SupportService was updated by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ),
                                    ActivityObjectId = entity.Id
                                };
                                new ActivityManager().SiteActivityAdd( sa );
                            }
                        }
                        else
                        {
                            status.AddError( "Error - update failed, as record was not found." );
                        }
                    }
                    else
                    {
                        //add
                        int newId = Add( entity, ref status );
                        if ( newId == 0 || status.HasErrors )
                            isValid = false;
                    }
                }
            }
            catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
            {
                string message = HandleDBValidationError( dbex, thisClassName + string.Format( ".Save. id: {0}, Name: {1}", entity.Id, entity.Name ), "SupportService" );
                status.AddError( thisClassName + ".Save(). Error - the save was not successful. " + message );
            }
            catch ( Exception ex )
            {
                string message = FormatExceptions( ex );
                LoggingHelper.LogError( ex, thisClassName + string.Format( ".Save. id: {0}, Name: {1}", entity.Id, entity.Name ) );
                status.AddError( thisClassName + ".Save(). Error - the save was not successful. " + message );
                isValid = false;
            }


            return isValid;
        }

        /// <summary>
        /// add a SupportService
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        private int Add( ThisResource entity, ref SaveStatus status )
        {
            DBResource efEntity = new DBResource();
            using ( var context = new EntityContext() )
            {
                try
                {
                    MapToDB( entity, efEntity );

                    if ( IsValidGuid( entity.RowId ) )
                        efEntity.RowId = entity.RowId;
                    else
                        efEntity.RowId = Guid.NewGuid();
                    efEntity.EntityStateId = entity.EntityStateId = 3;
                    if ( IsValidDate( status.EnvelopeCreatedDate ) )
                    {
                        efEntity.Created = status.LocalCreatedDate;
                        efEntity.LastUpdated = status.LocalCreatedDate;
                    }
                    else
                    {
                        efEntity.Created = System.DateTime.Now;
                        efEntity.LastUpdated = System.DateTime.Now;
                    }
                    context.SupportService.Add( efEntity );

                    // submit the change to database
                    int count = context.SaveChanges();
                    if ( count > 0 )
                    {
                        entity.RowId = efEntity.RowId;
                        entity.Created = efEntity.Created;
                        entity.LastUpdated = efEntity.LastUpdated;
                        entity.Id = efEntity.Id;
                        UpdateEntityCache( entity, ref status );
                        //add log entry
                        //TODO - should be include primary org in the comment, and owningOrgId, and Organization well the latter can be derived
                        SiteActivity sa = new SiteActivity()
                        {
                            ActivityType = EntityType,
                            Activity = "Import",
                            Event = "Add",
                            Comment = $"A SupportService was added by the import. Name: {entity.Name}, Organization: {entity.PrimaryOrganizationName}",
                            ActivityObjectId = entity.Id,
                            OwningOrgId = entity.PrimaryOrganizationId,
                            Organization = entity.PrimaryOrganizationName
                        };
                        new ActivityManager().SiteActivityAdd( sa );
                        if ( UpdateParts( entity, ref status ) == false )
                        {
                        }
                        //fill put the resource for upcoming references
                        entity = GetBasic( efEntity.Id );

                        return efEntity.Id;
                    }
                    else
                    {
                        //?no info on error

                        string message = thisClassName + string.Format( ". Add Failed", "Attempted to add a SupportService. The process appeared to not work, but was not an exception, so we have no message, or no clue. SupportService: {0}, ctid: {1}", entity.Name, entity.CTID );
                        status.AddError( thisClassName + ". Error - the add was not successful. " + message );
                        EmailManager.NotifyAdmin( "SupportServiceManager. Add Failed", message );
                    }
                }
                catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
                {
                    string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", "SupportService" );
                    status.AddError( thisClassName + ".Add(). Error - the save was not successful. " + message );

                    LoggingHelper.LogError( dbex, message );
                }
                catch ( Exception ex )
                {
                    string message = FormatExceptions( ex );
                    LoggingHelper.LogError( ex, thisClassName + string.Format( ".Add(), Name: {0}\r\n", efEntity.Name ) );
                    status.AddError( thisClassName + ".Add(). Error - the save was not successful. \r\n" + message );
                }
            }

            return efEntity.Id;
        }
        public int AddPendingRecord( Guid entityUid, string ctid, string registryAtId, ref SaveStatus status )
        {
            var efEntity = new DBResource();
            try
            {
                using ( var context = new EntityContext() )
                {
                    if ( !IsValidGuid( entityUid ) )
                    {
                        status.AddError( thisClassName + " - A valid GUID must be provided to create a pending entity" );
                        return 0;
                    }
                    //quick check to ensure not existing
                    ThisResource entity = GetByCtid( ctid );
                    if ( entity != null && entity.Id > 0 )
                        return entity.Id;

                    //only add DB required properties
                    //NOTE - an entity will be created via trigger
                    efEntity.Name = "Placeholder until full document is downloaded";
                    efEntity.Description = "Placeholder until full document is downloaded";
                    efEntity.EntityStateId = 1;
                    efEntity.RowId = entityUid;
                    //watch that Ctid can be  updated if not provided now!!
                    efEntity.CTID = ctid;
                    efEntity.SubjectWebpage = registryAtId;

                    efEntity.Created = System.DateTime.Now;
                    efEntity.LastUpdated = System.DateTime.Now;

                    context.SupportService.Add( efEntity );
                    int count = context.SaveChanges();
                    if ( count > 0 )
                    {
                        SiteActivity sa = new SiteActivity()
                        {
                            ActivityType = EntityType,
                            Activity = "Import",
                            Event = string.Format( "Add Pending {0}", EntityType ),
                            Comment = string.Format( "Pending {0} was added by the import. ctid: {1}, registryAtId: {2}", EntityType, ctid, registryAtId ),
                            ActivityObjectId = efEntity.Id
                        };
                        new ActivityManager().SiteActivityAdd( sa );
                        //Question should this be in the EntityCache?
                        entity.Id = efEntity.Id;
                        entity.RowId = efEntity.RowId;
                        entity.CTID = efEntity.CTID;
                        entity.EntityStateId = 1;
                        entity.Name = efEntity.Name;
                        entity.Description = efEntity.Description;
                        entity.SubjectWebpage = efEntity.SubjectWebpage;
                        entity.Created = ( DateTime ) efEntity.Created;
                        entity.LastUpdated = ( DateTime ) efEntity.LastUpdated;
                        UpdateEntityCache( entity, ref status );
                        return efEntity.Id;
                    }

                    status.AddError( thisClassName + " Error - the save was not successful, but no message provided. " );
                }
            }
            catch ( Exception ex )
            {
                string message = FormatExceptions( ex );
                LoggingHelper.LogError( ex, thisClassName + string.Format( ".AddPendingRecord. entityUid:  {0}, ctid: {1}", entityUid, ctid ) );
                status.AddError( thisClassName + " Error - the save was not successful. " + message );

            }
            return 0;
        }

        public void UpdateEntityCache( ThisResource document, ref SaveStatus status )
        {
            EntityCache ec = new EntityCache()
            {
                EntityTypeId = EntityTypeId,
                EntityType = EntityType,
                EntityStateId = document.EntityStateId,
                EntityUid = document.RowId,
                BaseId = document.Id,
                Description = document.Description,
                SubjectWebpage = document.SubjectWebpage,
                CTID = document.CTID,
                Created = document.Created,
                LastUpdated = document.LastUpdated,
                //ImageUrl = document.ImageUrl,
                Name = document.Name,
                OwningAgentUID = document.PrimaryAgentUID,
                OwningOrgId = document.OrganizationId
            };
            var statusMessage = string.Empty;
            if ( new EntityManager().EntityCacheSave( ec, ref statusMessage ) == 0 )
            {
                status.AddError( thisClassName + string.Format( ".UpdateEntityCache for '{0}' ({1}) failed: {2}", document.Name, document.Id, statusMessage ) );
            }
        }
        public bool ValidateProfile( ThisResource profile, ref SaveStatus status )
        {
            status.HasSectionErrors = false;


            if ( string.IsNullOrWhiteSpace( profile.Description ) )
            {
                //status.AddWarning( "An SupportService Description must be entered" );
            }

            var defStatus = CodesManager.GetLifeCycleStatus( CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS_ACTIVE );
            if ( profile.LifeCycleStatusType == null || profile.LifeCycleStatusType.Items == null || profile.LifeCycleStatusType.Items.Count == 0 )
            {
                profile.LifeCycleStatusTypeId = defStatus.Id;
            }
            else
            {
                var schemaName = profile.LifeCycleStatusType.GetFirstItem().SchemaName;
                CodeItem ci = CodesManager.GetLifeCycleStatus( CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, schemaName );
                if ( ci == null || ci.Id < 1 )
                {
                    //while this should never happen, should have a default
                    status.AddError( string.Format( "A valid LifeCycleStatusType must be included. Invalid: {0}", schemaName ) );
                    profile.LifeCycleStatusTypeId = defStatus.Id;
                }
                else
                    profile.LifeCycleStatusTypeId = ci.Id;
            }



            return status.WasSectionValid;
        }


        /// <summary>
        /// Delete an SupportService, and related Entity
        /// </summary>
        /// <param name="ctid"></param>
        /// <param name="statusMessage"></param>
        /// <returns></returns>
        public bool Delete( string ctid, ref List<string> messages )
        {
            bool isValid = true;
            if ( string.IsNullOrWhiteSpace( ctid ) )
            {
                messages.Add( thisClassName + ".Delete() Error - a valid CTID must be provided" );
                return false;
            }
            if ( string.IsNullOrWhiteSpace( ctid ) )
                ctid = "SKIP ME";

            using ( var context = new EntityContext() )
            {
                try
                {
                    context.Configuration.LazyLoadingEnabled = false;
                    DBResource efEntity = context.SupportService
                                .FirstOrDefault( s => ( s.CTID == ctid )
                                );

                    if ( efEntity != null && efEntity.Id > 0 )
                    {
                        Guid rowId = efEntity.RowId;

                        //need to remove from Entity.
                        //-using before delete trigger - verify won't have RI issues
                        string msg = string.Format( " SupportService. Id: {0}, Name: {1}, Ctid: {2}.", efEntity.Id, efEntity.Name, efEntity.CTID );

                        context.SupportService.Remove( efEntity );
                        //efEntity.EntityStateId = 0;
                        //efEntity.LastUpdated = System.DateTime.Now;
                        int count = context.SaveChanges();
                        if ( count > 0 )
                        {
                            new ActivityManager().SiteActivityAdd( new SiteActivity()
                            {
                                ActivityType = "SupportService",
                                Activity = "Import",
                                Event = "Delete",
                                Comment = msg,
                                ActivityObjectId = efEntity.Id
                            } );
                            isValid = true;
                            //delete cache
                            var statusMessage = string.Empty;
                            if ( !new EntityManager().EntityCacheDelete( rowId, ref statusMessage ) )
                            {
                                messages.Add( statusMessage );
                            }
                            //add pending request 
                            new SearchPendingReindexManager().AddDeleteRequest( EntityTypeId, efEntity.Id, ref messages );
                            //mark owning org for updates (actually should be covered by ReindexAgentForDeletedArtifact
                            //new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, orgId, 1, ref messages );

                            //delete all relationships
                            workIT.Models.SaveStatus status = new SaveStatus();
                            Entity_AgentRelationshipManager earmgr = new Entity_AgentRelationshipManager();
                            earmgr.DeleteAll( rowId, ref status );
                            //also check for any relationships
                            //There could be other orgs from relationships to be reindexed as well!

                            //also check for any relationships
                            //new Entity_AgentRelationshipManager().ReindexAgentForDeletedArtifact( orgUid );
                        }
                    }
                    else
                    {
                        messages.Add( thisClassName + ".Delete() Warning No action taken, as the record was not found." );
                    }
                }
                catch ( Exception ex )
                {
                    LoggingHelper.LogError( ex, thisClassName + ".Delete(envelopeId)" );
                    isValid = false;
                    var msg = FormatExceptions( ex );
                    if ( msg.ToLower().IndexOf( "the delete statement conflicted with the reference constraint" ) > -1 )
                    {
                        messages.Add( "Error: this SupportService cannot be deleted as it is being referenced by other items, such as roles or credentials. These associations must be removed before this SupportService can be deleted." );
                    }
                    else
                        messages.Add( msg );
                }
            }
            return isValid;
        }

        #region SupportService properties ===================
        public bool UpdateParts( ThisResource entity, ref SaveStatus status )
        {
            bool isAllValid = true;
            Entity relatedEntity = EntityManager.GetEntity( entity.RowId );
            if ( relatedEntity == null || relatedEntity.Id == 0 )
            {
                status.AddError( "Error - the related Entity was not found." );
                return false;
            }

            if ( UpdateProperties( entity, relatedEntity, ref status ) == false )
            {
                isAllValid = false;
            }
            Entity_AgentRelationshipManager eamgr = new Entity_AgentRelationshipManager();
            eamgr.SaveList( relatedEntity.Id, Entity_AgentRelationshipManager.ROLE_TYPE_OFFERED_BY, entity.OfferedByList, ref status );

            eamgr.SaveList( relatedEntity.Id, Entity_AgentRelationshipManager.ROLE_TYPE_PUBLISHEDBY, entity.PublishedBy, ref status );



            Entity_ReferenceFrameworkManager erfm = new Entity_ReferenceFrameworkManager();
            erfm.DeleteAll( relatedEntity, ref status );

            if ( erfm.SaveList( relatedEntity.Id, CodesManager.PROPERTY_CATEGORY_SOC, entity.OccupationType, ref status ) == false )
                isAllValid = false;

            AddProfiles( entity, relatedEntity, ref status );

            int newId = 0;
            //TBD
            var eIsPartOfSSMgr2 = new Entity_IsPartOfSupportServiceManager();
            eIsPartOfSSMgr2.Update( entity.IsSpecificServiceOfIds, relatedEntity, ref status );

            //
            var ehssMgr = new Entity_HasSupportServiceManager();
            ehssMgr.Update( entity.HasSpecificServiceIds, relatedEntity, ref status );

            return isAllValid;
        }

        public bool UpdateProperties( ThisResource entity, Entity relatedEntity, ref SaveStatus status )
        {

            bool isAllValid = true;

            EntityPropertyManager mgr = new EntityPropertyManager();
            try
            {
                //first clear all propertiesd
                mgr.DeleteAll( relatedEntity, ref status );

                if ( mgr.AddProperties( entity.DeliveryType, entity.RowId, EntityTypeId, CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE, false, ref status ) == false )
                    isAllValid = false;

                if ( mgr.AddProperties( entity.AccommodationType, entity.RowId, EntityTypeId, CodesManager.PROPERTY_CATEGORY_ACCOMMODATION, false, ref status ) == false )
                    isAllValid = false;

                if ( mgr.AddProperties( entity.SupportServiceType, entity.RowId, EntityTypeId, CodesManager.PROPERTY_CATEGORY_SUPPORT_SERVICE_CATEGORY, false, ref status ) == false )
                    isAllValid = false;

            }
            catch ( Exception ex )
            {
                LoggingHelper.DoTrace( 1, $"**EXCEPTION** {thisClassName}.UpdateProperties. Name: {entity.Name}, Id: {entity.Id}, Message: {ex.Message}." );
            }

            return isAllValid;
        }

        public void AddProfiles( ThisResource resource, Entity relatedEntity, ref SaveStatus status )
        {
            LoggingHelper.DoTrace( BaseFactory.appMethodEntryTraceLevel, thisClassName + ".AddProfiles - entered" );

            try
            {
                //ConditionProfile =======================================
                Entity_ConditionProfileManager emanager = new Entity_ConditionProfileManager();
                //emanager.DeleteAll( relatedEntity, ref status );

                emanager.SaveList( resource.SupportServiceCondition, Entity_ConditionProfileManager.ConnectionProfileType_SupportServiceCondition, resource.RowId, ref status );

                //CostProfile
                CostProfileManager cpm = new Factories.CostProfileManager();
                cpm.SaveList( resource.EstimatedCost, resource.RowId, ref status );

                new Entity_FinancialAssistanceProfileManager().SaveList( resource.FinancialAssistance, resource.RowId, ref status );

                new Entity_JurisdictionProfileManager().SaveAssertedInList( resource.RowId, Entity_AgentRelationshipManager.ROLE_TYPE_OFFERED_BY, resource.OfferedIn, ref status );
            }
            catch ( Exception ex )
            {
                LoggingHelper.DoTrace( 1, $"**EXCEPTION** {thisClassName}.AddProfiles. Name: {resource.Name}, Id: {resource.Id}, Message: {ex.Message}." );
            }


            try
            {
                new Entity_AddressManager().SaveList( resource.AvailableAt, resource.RowId, ref status );
                new Entity_CommonConditionManager().SaveList( resource.ConditionManifestIds, resource.RowId, ref status );

                new Entity_CommonCostManager().SaveList( resource.CostManifestIds, resource.RowId, ref status );
            }
            catch ( Exception ex )
            {
                LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".AddProfiles -**EXCEPTION**  3. Name: {0}, Id: {1}, Message: {2}.", resource.Name, resource.Id, ex.Message ) );
            }


            LoggingHelper.DoTrace( BaseFactory.appMethodExitTraceLevel, thisClassName + ".AddProfiles - exited." );

        }

        #endregion

        #endregion

        #region == Retrieval =======================
        public static ThisResource GetMinimumByCtid( string ctid )
        {
            ThisResource entity = new ThisResource();
            using ( var context = new EntityContext() )
            {
                var from = context.SupportService
                        .FirstOrDefault( s => s.CTID.ToLower() == ctid.ToLower() );

                if ( from != null && from.Id > 0 )
                {
                    entity.RowId = from.RowId;
                    entity.Id = from.Id;
                    entity.EntityStateId = from.EntityStateId;
                    entity.Name = from.Name;
                    entity.Description = from.Description;
                    entity.SubjectWebpage = from.SubjectWebpage;
                    entity.CTID = from.CTID;
                }
            }

            return entity;
        }
        //public static ThisResource Get( Guid profileUid )
        //{
        //    ThisResource entity = new ThisResource();
        //    if ( !IsGuidValid( entity.RowId ) )
        //        return null;
        //    try
        //    {
        //        using ( var context = new EntityContext() )
        //        {
        //            DBResource item = context.SupportService
        //                    .SingleOrDefault( s => s.RowId == profileUid );

        //            if ( item != null && item.Id > 0 )
        //            {
        //                MapFromDB( item, entity, false );
        //            }
        //        }
        //    }
        //    catch ( Exception ex )
        //    {
        //        LoggingHelper.LogError( ex, thisClassName + ".Get" );
        //    }
        //    return entity;
        //}//
        public static ThisResource GetBasic( int id )
        {
            ThisResource entity = new ThisResource();
            using ( var context = new EntityContext() )
            {
                var item = context.SupportService
                        .SingleOrDefault( s => s.Id == id );

                if ( item != null && item.Id > 0 )
                {
                    MapFromDB( item, entity, false );
                }
            }

            return entity;
        }

        /// <summary>
        /// Get resource by CTID
        /// </summary>
        /// <param name="ctid"></param>
        /// <param name="includingProperties">Typically false, to get a summary</param>
        /// <returns></returns>
        public static ThisResource GetByCtid( string ctid, bool includingProperties = false )
        {
            ThisResource entity = new ThisResource();
            if ( string.IsNullOrWhiteSpace( ctid ) )
                return entity;
            try
            {
                using ( var context = new EntityContext() )
                {
                    var item = context.SupportService
                            .FirstOrDefault( s => s.CTID.ToLower() == ctid.ToLower() );

                    if ( item != null && item.Id > 0 )
                    {
                        MapFromDB( item, entity, includingProperties );
                    }
                }
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, thisClassName + ".GetByCtid: " + ctid );
            }
            return entity;
        }//

        //public static List<ThisResource> GetAll( ref int totalRecords, int maxRecords = 100 )
        //{
        //	List<ThisResource> list = new List<ThisResource>();
        //	ThisResource entity = new ThisResource();
        //	using ( var context = new EntityContext() )
        //	{
        //		List<DBResource> results = context.SupportService
        //					 .Where( s => s.EntityStateId > 2 )
        //					 .OrderBy( s => s.Name )
        //					 .ToList();
        //		if ( results != null && results.Count > 0 )
        //		{
        //			totalRecords = results.Count();

        //			foreach ( DBResource item in results )
        //			{
        //				entity = new ThisResource();
        //				MapFromDB( item, entity, false );
        //				list.Add( entity );
        //				if ( maxRecords > 0 && list.Count >= maxRecords )
        //					break;
        //			}
        //		}
        //	}

        //	return list;
        //}
        public static ThisResource GetForDetail( int id, bool isAPIRequest = false )
        {
            ThisResource entity = new ThisResource();
            using ( var context = new EntityContext() )
            {
                DBResource item = context.SupportService
                        .SingleOrDefault( s => s.Id == id );

                if ( item != null && item.Id > 0 )
                {
                    //check for virtual deletes
                    if ( item.EntityStateId == 0 )
                    {
                        LoggingHelper.DoTrace( 1, string.Format( thisClassName + " Error: encountered request for detail for a deleted record. Name: {0}, CTID:{1}", item.Name, item.CTID ) );
                        entity.Name = "Record was not found.";
                        entity.CTID = item.CTID;
                        return entity;
                    }

                    MapFromDB( item, entity,
                            true //includingProperties
                            , isAPIRequest
                            );
                }
            }

            return entity;
        }
        public static int Count_ForOwningOrg( Guid orgUid )
        {
            int totalRecords = 0;

            using ( var context = new EntityContext() )
            {
                var results = context.SupportService
                            .Where( s => s.PrimaryAgentUid == orgUid && s.EntityStateId == 3 )
                            .ToList();

                if ( results != null && results.Count > 0 )
                {
                    totalRecords = results.Count();
                }
            }
            return totalRecords;
        }

        public static List<string> Autocomplete( string pFilter, int pageNumber, int pageSize, ref int pTotalRows )
        {
            bool autocomplete = true;
            var results = new List<string>();
            //
            List<ThisResource> list = Search( pFilter, string.Empty, pageNumber, pageSize, ref pTotalRows, autocomplete );
            bool appendingOrgNameToAutocomplete = UtilityManager.GetAppKeyValue( "appendingOrgNameToAutocomplete", false );
            string prevName = string.Empty;
            foreach ( var item in list )
            {
                //note excluding duplicates may have an impact on selected max terms
                if ( string.IsNullOrWhiteSpace( item.OrganizationName ) || !appendingOrgNameToAutocomplete )
                {
                    if ( item.Name.ToLower() != prevName )
                        results.Add( item.Name );
                }
                else
                {
                    results.Add( item.Name + " ('" + item.OrganizationName + "')" );
                }

                prevName = item.Name.ToLower();
            }
            return results;
        }


        public static List<ThisResource> Search( string pFilter, string pOrderBy, int pageNumber, int pageSize, ref int pTotalRows, bool autocomplete = false )
        {
            string connectionString = DBConnectionRO();
            ThisResource item = new ThisResource();
            List<ThisResource> list = new List<ThisResource>();
            var result = new DataTable();
            string temp = string.Empty;
            string org = string.Empty;
            int orgId = 0;

            using ( SqlConnection c = new SqlConnection( connectionString ) )
            {
                c.Open();

                if ( string.IsNullOrEmpty( pFilter ) )
                {
                    pFilter = string.Empty;
                }

                using ( SqlCommand command = new SqlCommand( "[SupportService.ElasticSearch]", c ) )
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add( new SqlParameter( "@Filter", pFilter ) );
                    command.Parameters.Add( new SqlParameter( "@SortOrder", pOrderBy ) );
                    command.Parameters.Add( new SqlParameter( "@StartPageIndex", pageNumber ) );
                    command.Parameters.Add( new SqlParameter( "@PageSize", pageSize ) );

                    SqlParameter totalRows = new SqlParameter( "@TotalRows", pTotalRows );
                    totalRows.Direction = ParameterDirection.Output;
                    command.Parameters.Add( totalRows );
                    try
                    {
                        using ( SqlDataAdapter adapter = new SqlDataAdapter() )
                        {
                            adapter.SelectCommand = command;
                            adapter.Fill( result );
                        }
                        string rows = command.Parameters[command.Parameters.Count - 1].Value.ToString();

                        pTotalRows = Int32.Parse( rows );
                    }
                    catch ( Exception ex )
                    {
                        pTotalRows = 0;
                        LoggingHelper.LogError( ex, thisClassName + string.Format( ".Search() - Execute proc, Message: {0} \r\n Filter: {1} \r\n", ex.Message, pFilter ) );

                        item = new SupportService();
                        item.Name = "Unexpected error encountered. System administration has been notified. Please try again later. ";
                        item.Description = ex.Message;

                        list.Add( item );
                        return list;
                    }
                }

                foreach ( DataRow dr in result.Rows )
                {
                    item = new ThisResource();
                    item.Id = GetRowColumn( dr, "Id", 0 );
                    item.Name = GetRowColumn( dr, "Name", "missing" );
                    item.FriendlyName = FormatFriendlyTitle( item.Name );
                    //for autocomplete, only need name
                    if ( autocomplete )
                    {
                        list.Add( item );
                        continue;
                    }

                    item.Description = GetRowColumn( dr, "Description", string.Empty );
                    string rowId = GetRowColumn( dr, "RowId" );
                    item.RowId = new Guid( rowId );

                    item.SubjectWebpage = GetRowColumn( dr, "SubjectWebpage", string.Empty );
                    item.CTID = GetRowPossibleColumn( dr, "CTID", string.Empty );
                    item.CredentialRegistryId = GetRowPossibleColumn( dr, "CredentialRegistryId", string.Empty );



                    //org = GetRowPossibleColumn( dr, "Organization", string.Empty );
                    //orgId = GetRowPossibleColumn( dr, "OrgId", 0 );
                    //if ( orgId > 0 )
                    //	item.OwningOrganization = new Organization() { Id = orgId, Name = org };

                    //
                    //temp = GetRowColumn( dr, "DateEffective", string.Empty );
                    //if ( IsValidDate( temp ) )
                    //	item.DateEffective = DateTime.Parse( temp ).ToString("yyyy-MM-dd");
                    //else
                    //	item.DateEffective = string.Empty;

                    item.Created = GetRowColumn( dr, "Created", System.DateTime.MinValue );
                    item.LastUpdated = GetRowColumn( dr, "LastUpdated", System.DateTime.MinValue );

                    list.Add( item );
                }

                return list;

            }
        } //

        public static void MapToDB( ThisResource input, DBResource output )
        {

            //want output ensure fields input create are not wiped
            if ( output.Id == 0 )
            {
                output.CTID = input.CTID;
            }
            //if ( !string.IsNullOrWhiteSpace( input.CredentialRegistryId ) )
            //	output.CredentialRegistryId = input.CredentialRegistryId;

            output.Id = input.Id;
            output.Name = GetData( input.Name );
            output.Description = GetData( input.Description );
            output.EntityStateId = input.EntityStateId;
			if ( input.OwnedByList != null && input.OwnedByList.Any() )
			{
				output.PrimaryAgentUid = input.OwnedByList[ 0 ];
			}
			else if ( input.OfferedByList != null && input.OfferedByList.Any() )
			{
				output.PrimaryAgentUid = input.OfferedByList[ 0 ];
			}
			else
			{
				//should not be possible, and is set as not null in db!!
				output.PrimaryAgentUid = Guid.Empty;
			}
			output.LifeCycleStatusTypeId = input.LifeCycleStatusTypeId;


            output.AvailabilityListing = null;
            output.AvailableOnlineAt = null;
            output.AlternateName = null;
            output.Keyword = null;
            if ( input.AvailabilityListing != null && input.AvailabilityListing.Count > 0 )
            {
                output.AvailabilityListing = FormatListAsDelimitedString( input.AvailabilityListing, "|" );
                //output.AvailabilityListing = JsonConvert.SerializeObject( input.AvailabilityListing, JsonHelper.GetJsonSettings(false) );
            }
            if ( input.AvailableOnlineAt != null && input.AvailableOnlineAt.Count > 0 )
            {
                output.AvailableOnlineAt = FormatListAsDelimitedString( input.AvailabilityListing, "|" );
                //output.AvailableOnlineAt = JsonConvert.SerializeObject( input.AvailableOnlineAt, JsonHelper.GetJsonSettings( false ) );
            }
            if ( input.AlternateName != null && input.AlternateName.Count > 0 )
            {
                output.AlternateName = FormatListAsDelimitedString( input.AlternateName, "|" );
                //output.AlternateName = JsonConvert.SerializeObject( input.AlternateName, JsonHelper.GetJsonSettings( false ) );
            }
            if ( input.Keyword != null && input.Keyword.Count > 0 )
            {
                output.Keyword = FormatListAsDelimitedString( input.Keyword, "|" );
            }
            if ( IsValidDate( input.DateEffective ) )
                output.DateEffective = DateTime.Parse( input.DateEffective );
            else
                output.DateEffective = null;
            if ( IsValidDate( input.ExpirationDate ) )
                output.ExpirationDate = DateTime.Parse( input.ExpirationDate );
            else
                output.ExpirationDate = null;
            output.Identifier = input.IdentifierJSON;

            output.SubjectWebpage = GetUrlData( input.SubjectWebpage );
            if ( IsValidDate( input.LastUpdated ) )
                output.LastUpdated = input.LastUpdated;
        }


        public static void MapFromDB_ForSummary( DBResource from, ThisResource to )
        {
            MapFromDB( from, to, false );
        } //

        public static void MapFromDB( DBResource input, ThisResource output, bool includingProperties, bool isAPIRequest = false )
        {
            output.Id = input.Id;
            output.RowId = input.RowId;
            output.EntityStateId = input.EntityStateId;
            //
            output.Name = input.Name;
            output.FriendlyName = FormatFriendlyTitle( input.Name );

            output.Description = input.Description == null ? string.Empty : input.Description;
            output.CTID = input.CTID;
            //primary could be owner or offerer if no ownere
            if ( IsGuidValid( input.PrimaryAgentUid ) )
            {
                output.PrimaryAgentUID = ( Guid ) input.PrimaryAgentUid;
                output.PrimaryOrganization = OrganizationManager.GetBasics( ( Guid ) input.PrimaryAgentUid, false );
            }
            var offeringOrgs = Entity_AgentRelationshipManager.GetAllOfferingOrgs( output.RowId );
            if ( offeringOrgs != null && offeringOrgs.Any() )
            {
                output.PrimaryOrganization = offeringOrgs[0];
                output.OfferedBy = offeringOrgs.ToList().ConvertAll( m =>
                    new ResourceSummary()
                    {
                        Id = m.Id,
                        RowId = m.RowId,
                        Name = m.Name,
                        Description = m.Description,
                        CTID = m.CTID,
                        URI = m.SubjectWebpage,
                    } );

                //TBD on this
                //output.OrganizationRole = new OrganizationRoleManager().GetAllCombinedForTarget( CodesManager.ENTITY_TYPE_SUPPORT_SERVICE, output.Id, output.OwningOrganizationId );
            }
            output.OrganizationRole = Entity_AgentRelationshipManager.AgentEntityRole_GetAll_ToEnumeration( output.RowId, true );

            output.SubjectWebpage = input.SubjectWebpage;
            if ( IsValidDate( input.DateEffective ) )
                output.DateEffective = ( ( DateTime ) input.DateEffective ).ToString( "yyyy-MM-dd" );
            else
                output.DateEffective = string.Empty;
            //
            if ( IsValidDate( input.ExpirationDate ) )
                output.ExpirationDate = ( ( DateTime ) input.ExpirationDate ).ToString( "yyyy-MM-dd" );
            else
                output.ExpirationDate = string.Empty;

            if ( IsValidDate( input.Created ) )
                output.Created = ( DateTime ) input.Created;
            if ( IsValidDate( input.LastUpdated ) )
                output.LastUpdated = ( DateTime ) input.LastUpdated;


            //
            int lifeCycleStatusTypeId = input.LifeCycleStatusTypeId != null ? ( int ) input.LifeCycleStatusTypeId : 0;//get active later
            if ( lifeCycleStatusTypeId > 0 )
            {
                CodeItem ct = CodesManager.GetLifeCycleStatus( lifeCycleStatusTypeId );
                if ( ct != null && ct.Id > 0 )
                {
                    //output.LifeCycleStatus = ct.Title;
                }
                //retain example using an Enumeration for by other related tableS??? - old detail page?
                output.LifeCycleStatusType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS );
                output.LifeCycleStatusType.Items.Add( new EnumeratedItem() { Id = lifeCycleStatusTypeId, Name = ct.Name, SchemaName = ct.SchemaName } );
            }
            else
            {
                //default to active
                output.LifeCycleStatusType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS );
                EnumeratedItem statusItem = output.LifeCycleStatusType.GetFirstItem();
                if ( statusItem != null && statusItem.Id > 0 && statusItem.Name != "Active" )
                {

                }
            }
            //23-07-14 mp - now include concepts for summary requests
            output.AccommodationType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_ACCOMMODATION );
            output.DeliveryType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE );
            output.SupportServiceType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_SUPPORT_SERVICE_CATEGORY );

            //=====
            var relatedEntity = EntityManager.GetEntity( output.RowId, false );
			//NOTE: EntityLastUpdated should really be the last registry update now. Check how LastUpdated is assigned on import
			output.EntityLastUpdated = output.LastUpdated;

			if ( !includingProperties )
                return;
            //
            if ( !string.IsNullOrWhiteSpace( input.AlternateName ) )
                output.AlternateName = SplitDelimitedStringToList( input.AlternateName, '|' );
            output.AvailableAt = Entity_AddressManager.GetAll( output.RowId );
            if ( !string.IsNullOrWhiteSpace( input.AvailabilityListing ) )
                output.AvailabilityListing = SplitDelimitedStringToList( input.AvailabilityListing, '|' );
            if ( !string.IsNullOrWhiteSpace( input.AvailableOnlineAt ) )
                output.AvailableOnlineAt = SplitDelimitedStringToList( input.AvailableOnlineAt, '|' );

            //get condition profiles
			List<ConditionProfile> list = Entity_ConditionProfileManager.GetAll( output.RowId, false );
            if ( list != null && list.Count > 0 )
            {
                foreach ( ConditionProfile item in list )
                {
                    if ( item.ConditionSubTypeId != Entity_ConditionProfileManager.ConditionSubType_Basic )
                    {
                        //should not happen
                    }
                    else if ( item.ConnectionProfileTypeId == Entity_ConditionProfileManager.ConnectionProfileType_SupportServiceCondition )
                        output.SupportServiceCondition.Add( item );

                    else
                    {
                        EmailManager.NotifyAdmin( $"Unexpected Condition Profile for {Entity_Label}", string.Format( "recordId: {0}, ConditionProfileTypeId: {1}", output.Id, item.ConnectionProfileTypeId ) );
                    }
                }
            }
            output.CommonConditions = Entity_CommonConditionManager.GetAll( output.RowId );
            output.CommonCosts = Entity_CommonCostManager.GetAll( output.RowId );

            output.EstimatedCost = CostProfileManager.GetAll( output.RowId );
            output.FinancialAssistance = Entity_FinancialAssistanceProfileManager.GetAll( output.RowId, false );
            if ( !string.IsNullOrWhiteSpace( input.Keyword ) )
                output.Keyword = SplitDelimitedStringToList( input.Keyword, '|' );
            output.OccupationType = Reference_FrameworkItemManager.FillCredentialAlignmentObject( output.RowId, CodesManager.PROPERTY_CATEGORY_SOC );

            output.OfferedIn = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( output.RowId );

            //this is to get all resources (cred, etc) that reference this SS.
            //so should be inverse of HSS
            output.SupportServiceReferencedBy = Entity_HasSupportServiceManager.GetAllTargets( output.Id );
           
            output.IsSpecificServiceOf= Entity_IsPartOfSupportServiceManager.GetAll( output.RowId );
            output.HasSpecificService = Entity_IsPartOfSupportServiceManager.GetHasSpecificService( output.Id );

            try
            {
                if ( !string.IsNullOrEmpty( input.Identifier ) )
                {
                    output.Identifier = JsonConvert.DeserializeObject<List<IdentifierValue>>( input.Identifier );
                }
            }
            catch ( Exception ex )
            {

            }
           
            //Include currencies to fix null errors in various places (detail, compare, publishing) - NA 3/17/2017
            var currencies = CodesManager.GetCurrencies();
            //Include cost types to fix other null errors - NA 3/31/2017
            var costTypes = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_CREDENTIAL_ATTAINMENT_COST );
            if ( output.EstimatedCost != null )
            {
                foreach ( var cost in output.EstimatedCost )
                {
                    cost.CurrencyTypes = currencies;
                    foreach ( var costItem in cost.Items )
                    {
                        costItem.DirectCostType.Items.Add( costTypes.Items.FirstOrDefault( m => m.CodeId == costItem.CostTypeId ) );
                    }
                }
            }

        } //

        #endregion

    }
}

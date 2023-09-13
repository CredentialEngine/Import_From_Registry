using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

using workIT.Models;
using workIT.Models.Common;
using workIT.Utilities;

using DBEntity = workIT.Data.Tables.ScheduledOffering;
using EntityContext = workIT.Data.Tables.workITEntities;
using ThisResource = workIT.Models.Common.ScheduledOffering;

namespace workIT.Factories
{
    public class ScheduledOfferingManager : BaseFactory
    {
        static readonly string thisClassName = "ScheduledOfferingManager";
        static string EntityType = "ScheduledOffering";
        static int EntityTypeId = CodesManager.ENTITY_TYPE_SCHEDULED_OFFERING;
        static string Entity_Label = "Scheduled Offering";
        static string Entities_Label = "Scheduled Offerings";
        EntityManager entityMgr = new EntityManager();

        #region ScheduledOffering - persistance ==================
        /// <summary>
        /// Update a ScheduledOffering
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
                        DBEntity efEntity = context.ScheduledOffering
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
                                    ActivityType = "ScheduledOffering",
                                    Activity = "Import",
                                    Event = "Reactivate",
                                    Comment = string.Format( "ScheduledOffering had been marked as deleted, and was reactivted by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ),
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
                                    string message = string.Format( thisClassName + ".Save Failed", "Attempted to update a ScheduledOffering. The process appeared to not work, but was not an exception, so we have no message, or no clue. ScheduledOffering: {0}, Id: {1}", entity.Name, entity.Id );
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
                                    ActivityType = "ScheduledOffering",
                                    Activity = "Import",
                                    Event = "Update",
                                    Comment = string.Format( "ScheduledOffering was updated by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ),
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
                string message = HandleDBValidationError( dbex, thisClassName + string.Format( ".Save. id: {0}, Name: {1}", entity.Id, entity.Name ), "ScheduledOffering" );
                status.AddError( thisClassName + ".Save(). Error - the save was not successful. " + message );
            }
            catch ( Exception ex )
            {
                string message = FormatExceptions( ex );
                LoggingHelper.LogError( ex, thisClassName + string.Format( ".Save. id: {0}, Name: {1}", entity.Id, entity.Name ), true );
                status.AddError( thisClassName + ".Save(). Error - the save was not successful. " + message );
                isValid = false;
            }


            return isValid;
        }

        /// <summary>
        /// add a ScheduledOffering
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        private int Add( ThisResource entity, ref SaveStatus status )
        {
            DBEntity efEntity = new DBEntity();
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
                    context.ScheduledOffering.Add( efEntity );

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
                        SiteActivity sa = new SiteActivity()
                        {
                            ActivityType = "ScheduledOffering",
                            Activity = "Import",
                            Event = "Add",
                            Comment = string.Format( "Full ScheduledOffering was added by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ),
                            ActivityObjectId = entity.Id
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

                        string message = thisClassName + string.Format( ". Add Failed", "Attempted to add a ScheduledOffering. The process appeared to not work, but was not an exception, so we have no message, or no clue. ScheduledOffering: {0}, ctid: {1}", entity.Name, entity.CTID );
                        status.AddError( thisClassName + ". Error - the add was not successful. " + message );
                        EmailManager.NotifyAdmin( "ScheduledOfferingManager. Add Failed", message );
                    }
                }
                catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
                {
                    string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", "ScheduledOffering" );
                    status.AddError( thisClassName + ".Add(). Error - the save was not successful. " + message );

                    LoggingHelper.LogError( message, true );
                }
                catch ( Exception ex )
                {
                    string message = FormatExceptions( ex );
                    LoggingHelper.LogError( ex, thisClassName + string.Format( ".Add(), Name: {0}\r\n", efEntity.Name ), true );
                    status.AddError( thisClassName + ".Add(). Error - the save was not successful. \r\n" + message );
                }
            }

            return efEntity.Id;
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
            var statusMessage = "";
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
                //status.AddWarning( "An ScheduledOffering Description must be entered" );
            }


            //if ( string.IsNullOrWhiteSpace( profile.SubjectWebpage ) )
            //    status.AddWarning( "Error - A Subject Webpage name must be entered" );

            //else if ( !IsUrlValid( profile.SubjectWebpage, ref commonStatusMessage ) )
            //{
            //    status.AddWarning( "The ScheduledOffering Subject Webpage is invalid. " + commonStatusMessage );
            //}


            return status.WasSectionValid;
        }


        /// <summary>
        /// Delete an ScheduledOffering, and related Entity
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
                    DBEntity efEntity = context.ScheduledOffering
                                .FirstOrDefault( s => ( s.CTID == ctid )
                                );

                    if ( efEntity != null && efEntity.Id > 0 )
                    {
                        Guid rowId = efEntity.RowId;

                        //need to remove from Entity.
                        //-using before delete trigger - verify won't have RI issues
                        string msg = string.Format( " ScheduledOffering. Id: {0}, Name: {1}, Ctid: {2}.", efEntity.Id, efEntity.Name, efEntity.CTID );

                        context.ScheduledOffering.Remove( efEntity );
                        //efEntity.EntityStateId = 0;
                        //efEntity.LastUpdated = System.DateTime.Now;
                        int count = context.SaveChanges();
                        if ( count > 0 )
                        {
                            new ActivityManager().SiteActivityAdd( new SiteActivity()
                            {
                                ActivityType = "ScheduledOffering",
                                Activity = "Import",
                                Event = "Delete",
                                Comment = msg,
                                ActivityObjectId = efEntity.Id
                            } );
                            isValid = true;
                            //delete cache
                            var statusMessage = "";
                            if (!new EntityManager().EntityCacheDelete( rowId, ref statusMessage ))
                            {
                                messages.Add(statusMessage );
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
                        messages.Add( thisClassName + ".Delete() Warning No action taken, as the record was not found.");
                    }
                }
                catch ( Exception ex )
                {
                    LoggingHelper.LogError( ex, thisClassName + ".Delete(envelopeId)" );
                    isValid = false;
                    var msg = FormatExceptions( ex );
                    if ( msg.ToLower().IndexOf( "the delete statement conflicted with the reference constraint" ) > -1 )
                    {
                        messages.Add( "Error: this ScheduledOffering cannot be deleted as it is being referenced by other items, such as roles or credentials. These associations must be removed before this ScheduledOffering can be deleted.");
                    } else 
                        messages.Add( msg );
                }
            }
            return isValid;
        }

        #region ScheduledOffering properties ===================
        public bool UpdateParts( ThisResource resource, ref SaveStatus status )
        {
            bool isAllValid = true;
            Entity relatedEntity = EntityManager.GetEntity( resource.RowId );
            if ( relatedEntity == null || relatedEntity.Id == 0 )
            {
                status.AddError( "Error - the related Entity was not found." );
                return false;
            }

            if ( UpdateProperties( resource, relatedEntity, ref status ) == false )
            {
                isAllValid = false;
            }
            Entity_AgentRelationshipManager eamgr = new Entity_AgentRelationshipManager();
            eamgr.SaveList( relatedEntity.Id, Entity_AgentRelationshipManager.ROLE_TYPE_OFFERED_BY, resource.OfferedByList, ref status );

            eamgr.SaveList( relatedEntity.Id, Entity_AgentRelationshipManager.ROLE_TYPE_PUBLISHEDBY, resource.PublishedBy, ref status );

            AddProfiles( resource, relatedEntity, ref status );

            var ehssMgr = new Entity_HasSupportServiceManager();
            ehssMgr.Update( resource.HasSupportServiceIds, relatedEntity, ref status );

            return isAllValid;
        }

        public bool UpdateProperties( ThisResource entity, Entity relatedEntity, ref SaveStatus status )
        {
            LoggingHelper.DoTrace( 7, thisClassName + ".UpdateProperties - entered" );

            bool isAllValid = true;

            EntityPropertyManager mgr = new EntityPropertyManager();
            try
            {
                //first clear all propertiesd
                mgr.DeleteAll( relatedEntity, ref status );

                if ( mgr.AddProperties( entity.DeliveryType, entity.RowId, EntityTypeId, CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE, false, ref status ) == false )
                    isAllValid = false;

                if ( mgr.AddProperties( entity.OfferFrequencyType, entity.RowId, EntityTypeId, CodesManager.PROPERTY_CATEGORY_OFFER_FREQUENCY, false, ref status ) == false )
                    isAllValid = false;

                if ( mgr.AddProperties( entity.ScheduleFrequencyType, entity.RowId, EntityTypeId, CodesManager.PROPERTY_CATEGORY_SCHEDULE_FREQUENCY, false, ref status ) == false )
                    isAllValid = false;

                if ( mgr.AddProperties( entity.ScheduleTimingType, entity.RowId, EntityTypeId, CodesManager.PROPERTY_CATEGORY_SCHEDULE_TIMING, false, ref status ) == false )
                    isAllValid = false;

            }
            catch ( Exception ex )
            {
                LoggingHelper.DoTrace( 1, string.Format( "**EXCEPTION** LearningOpportunityManager.UpdateProperties. Name: {0}, Id: {1}, Message: {2}.", entity.Name, entity.Id, ex.Message ) );
            }

            LoggingHelper.DoTrace( 7, thisClassName + ".UpdateProperties - exited" );
            return isAllValid;
        }

        public void AddProfiles( ThisResource entity, Entity relatedEntity, ref SaveStatus status )
        {
            LoggingHelper.DoTrace( 7, thisClassName + ".AddProfiles - entered" );

            try
            {
                //DurationProfile
                DurationProfileManager dpm = new Factories.DurationProfileManager();
                dpm.SaveList( entity.EstimatedDuration, entity.RowId, ref status );

                //CostProfile
                CostProfileManager cpm = new Factories.CostProfileManager();
                cpm.SaveList( entity.EstimatedCost, entity.RowId, ref status );
            }
            catch ( Exception ex )
            {
                LoggingHelper.DoTrace( 1, string.Format( "**EXCEPTION** LearningOpportunityManager.AddProfiles - 1. Name: {0}, Id: {1}, Message: {2}.", entity.Name, entity.Id, ex.Message ) );
            }
            

            try
            {
                new Entity_AddressManager().SaveList( entity.AvailableAt, entity.RowId, ref status );

                new Entity_CommonCostManager().SaveList( entity.CostManifestIds, entity.RowId, ref status );
            }
            catch ( Exception ex )
            {
                LoggingHelper.DoTrace( 1, string.Format( thisClassName+ ".AddProfiles -**EXCEPTION**  3. Name: {0}, Id: {1}, Message: {2}.", entity.Name, entity.Id, ex.Message ) );
            }

            var adpm = new Entity_AggregateDataProfileManager();
            if ( adpm.SaveList( entity.AggregateData, relatedEntity, ref status ) == false )
                status.HasSectionErrors = true;

            LoggingHelper.DoTrace( 7, thisClassName + ".AddProfiles - exited." );

        }

        #endregion

        #endregion

        #region == Retrieval =======================
        public static ThisResource GetMinimumByCtid( string ctid )
        {
            ThisResource entity = new ThisResource();
            using ( var context = new EntityContext() )
            {
                DBEntity from = context.ScheduledOffering
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
        //            DBEntity item = context.ScheduledOffering
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
                DBEntity item = context.ScheduledOffering
                        .SingleOrDefault( s => s.Id == id );

                if ( item != null && item.Id > 0 )
                {
                    MapFromDB( item, entity, false );
                }
            }

            return entity;
        }

        //public static List<ThisResource> GetAll( ref int totalRecords, int maxRecords = 100 )
        //{
        //	List<ThisResource> list = new List<ThisResource>();
        //	ThisResource entity = new ThisResource();
        //	using ( var context = new EntityContext() )
        //	{
        //		List<DBEntity> results = context.ScheduledOffering
        //					 .Where( s => s.EntityStateId > 2 )
        //					 .OrderBy( s => s.Name )
        //					 .ToList();
        //		if ( results != null && results.Count > 0 )
        //		{
        //			totalRecords = results.Count();

        //			foreach ( DBEntity item in results )
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
                DBEntity item = context.ScheduledOffering
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

        /// <summary>
        /// Get all VerificationProfile for the parent
        /// Uses the parent Guid to retrieve the related Entity, then uses the EntityId to retrieve the child objects.
        /// </summary>
        /// <param name="orgUid"></param>
        public static List<ThisResource> GetAll( Guid orgUid, bool includingItems = true )
        {
            ThisResource entity = new ThisResource();
            List<ThisResource> list = new List<ThisResource>();
            try
            {
                using ( var context = new EntityContext() )
                {
                    var results = context.ScheduledOffering
                            .Where( s => s.OfferedBy == orgUid )
                            .OrderBy( s => s.Id )
                            .ToList();

                    if ( results != null && results.Count > 0 )
                    {
                        foreach ( var item in results )
                        {
                            entity = new ThisResource();
                            MapFromDB( item, entity, includingItems );
                            list.Add( entity );
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, thisClassName + ".GetAll" );
            }
            return list;
        }//

        public static int Count_ForOwningOrg( Guid orgUid )
        {
            int totalRecords = 0;

            using ( var context = new EntityContext() )
            {
                var results = context.ScheduledOffering
                            .Where( s => s.OfferedBy == orgUid && s.EntityStateId == 3 )
                            .ToList();

                if ( results != null && results.Count > 0 )
                {
                    totalRecords = results.Count();
                }
            }
            return totalRecords;
        }

        public static List<object> Autocomplete( string pFilter, int pageNumber, int pageSize, ref int pTotalRows )
        {
            bool autocomplete = true;
            List<object> results = new List<object>();
            List<string> competencyList = new List<string>();
            //ref competencyList, 
            List<ThisResource> list = Search( pFilter, "", pageNumber, pageSize, ref pTotalRows, autocomplete );
            bool appendingOrgNameToAutocomplete = UtilityManager.GetAppKeyValue( "appendingOrgNameToAutocomplete", false );
            string prevName = "";
            foreach ( ScheduledOffering item in list )
            {
                //note excluding duplicates may have an impact on selected max terms
                if ( string.IsNullOrWhiteSpace( item.OrganizationName )
    || !appendingOrgNameToAutocomplete )
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
            string temp = "";
            string org = "";
            int orgId = 0;

            using ( SqlConnection c = new SqlConnection( connectionString ) )
            {
                c.Open();

                if ( string.IsNullOrEmpty( pFilter ) )
                {
                    pFilter = "";
                }

                using ( SqlCommand command = new SqlCommand( "[ScheduledOffering.ElasticSearch]", c ) )
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

                        item = new ScheduledOffering();
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

                    item.Description = GetRowColumn( dr, "Description", "" );
                    string rowId = GetRowColumn( dr, "RowId" );
                    item.RowId = new Guid( rowId );

                    item.SubjectWebpage = GetRowColumn( dr, "SubjectWebpage", "" );
                    item.CTID = GetRowPossibleColumn( dr, "CTID", "" );
                    item.CredentialRegistryId = GetRowPossibleColumn( dr, "CredentialRegistryId", "" );



                    //org = GetRowPossibleColumn( dr, "Organization", "" );
                    //orgId = GetRowPossibleColumn( dr, "OrgId", 0 );
                    //if ( orgId > 0 )
                    //	item.OwningOrganization = new Organization() { Id = orgId, Name = org };

                    //
                    //temp = GetRowColumn( dr, "DateEffective", "" );
                    //if ( IsValidDate( temp ) )
                    //	item.DateEffective = DateTime.Parse( temp ).ToString("yyyy-MM-dd");
                    //else
                    //	item.DateEffective = "";

                    item.Created = GetRowColumn( dr, "Created", System.DateTime.MinValue );
                    item.LastUpdated = GetRowColumn( dr, "LastUpdated", System.DateTime.MinValue );

                    list.Add( item );
                }

                return list;

            }
        } //

        public static void MapToDB( ThisResource input, DBEntity output )
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
            if ( input.OfferedByList != null && input.OfferedByList.Any() )
            {
                output.OfferedBy = input.OfferedByList[0];
            }
            else
            {
                output.OfferedBy = null;
            }

            output.SubjectWebpage = GetUrlData( input.SubjectWebpage );
            output.AvailabilityListing = null;
            output.AvailableOnlineAt = null;
            output.AlternateName = null;
            if ( input.AvailabilityListing != null && input.AvailabilityListing.Count > 0 )
            {
                output.AvailabilityListing = GetListAsDelimitedString( input.AvailabilityListing, "|" );
                //output.AvailabilityListing = JsonConvert.SerializeObject( input.AvailabilityListing, JsonHelper.GetJsonSettings(false) );
            }
            if ( input.AvailableOnlineAt != null && input.AvailableOnlineAt.Count > 0 )
            {
                output.AvailableOnlineAt = GetListAsDelimitedString( input.AvailabilityListing, "|" );
                //output.AvailableOnlineAt = JsonConvert.SerializeObject( input.AvailableOnlineAt, JsonHelper.GetJsonSettings( false ) );
            }
            if ( input.AlternateName != null && input.AlternateName.Count > 0 )
            {
                output.AlternateName = GetListAsDelimitedString( input.AlternateName, "|" );
                //output.AlternateName = JsonConvert.SerializeObject( input.AlternateName, JsonHelper.GetJsonSettings( false ) );
            }
            output.DeliveryTypeDescription = GetData( input.DeliveryTypeDescription );

            if ( IsValidDate( input.LastUpdated ) )
                output.LastUpdated = input.LastUpdated;
        }

        public static void MapFromDB( DBEntity input, ThisResource output, bool includingProperties, bool isAPIRequest = false )
        {
            output.Id = input.Id;
            output.RowId = input.RowId;
            output.EntityStateId = input.EntityStateId;
            //
            output.Name = input.Name;
            output.FriendlyName = FormatFriendlyTitle( input.Name );

            output.Description = input.Description == null ? "" : input.Description;
            output.CTID = input.CTID;
            //no owner, just offered by 
            if ( IsGuidValid( input.OfferedBy ) )
            {
                output.OfferedByAgentUid = ( Guid ) input.OfferedBy;
                output.PrimaryOrganization = OrganizationManager.GetBasics( ( Guid ) input.OfferedBy );
            }
            var offeringOrgs = Entity_AgentRelationshipManager.GetAllOfferingOrgs( output.RowId);
            if ( offeringOrgs != null && offeringOrgs.Any())
            {
                output.PrimaryOrganization = offeringOrgs[0];
                output.OfferedBy = offeringOrgs.ToList().ConvertAll( m =>
                    new ResourceSummary()
                    {
                        Id = m.Id,
                        RowId = m.RowId,
                        Name = m.Name,
                        Description= m.Description,
                        CTID= m.CTID,
                        URI= m.SubjectWebpage,
                    } );

                //seem to need org roles for later common code
                //var orp = Entity_AgentRelationshipManager.AgentEntityRole_GetAsEnumerationFromCSV( output.RowId, output.OwningOrganization.RowId );
                //output.OrganizationRole = new OrganizationRoleManager().GetAllCombinedForTarget( CodesManager.ENTITY_TYPE_SCHEDULED_OFFERING, output.Id, output.OwningOrganizationId );
            }
            output.OrganizationRole = Entity_AgentRelationshipManager.AgentEntityRole_GetAll_ToEnumeration( output.RowId, true );

            output.SubjectWebpage = input.SubjectWebpage;
            if ( IsValidDate( input.Created ) )
                output.Created = ( DateTime ) input.Created;
            if ( IsValidDate( input.LastUpdated ) )
                output.LastUpdated = ( DateTime ) input.LastUpdated;
            if ( !string.IsNullOrWhiteSpace(input.AvailabilityListing) )
            {
                output.AvailabilityListing = SplitDelimitedStringToList( input.AvailabilityListing, '|' );
                //output.AvailabilityListing = JsonConvert.DeserializeObject<List<string>>( input.AvailabilityListing );
            }
            if ( !string.IsNullOrWhiteSpace( input.AvailableOnlineAt ) )
            {
                output.AvailableOnlineAt = SplitDelimitedStringToList( input.AvailableOnlineAt, '|' );
                //output.AvailableOnlineAt = JsonConvert.DeserializeObject<List<string>>( input.AvailableOnlineAt );
            }
            if ( !string.IsNullOrWhiteSpace( input.AlternateName ) )
            {
                output.AlternateName = SplitDelimitedStringToList( input.AlternateName, '|' );
                //output.AlternateName = JsonConvert.DeserializeObject<List<string>>( input.AlternateName );
            }
            output.AvailableAt = Entity_AddressManager.GetAll( output.RowId );

            //=====
            var relatedEntity = EntityManager.GetEntity( output.RowId, false );
            //if ( relatedEntity != null && relatedEntity.Id > 0 )
            output.EntityLastUpdated = output.LastUpdated;  // relatedEntity.LastUpdated;

            if ( !includingProperties )
                return;
            //
            output.DeliveryType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE );
            output.DeliveryTypeDescription = input.DeliveryTypeDescription;

            output.OfferFrequencyType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_OFFER_FREQUENCY );
            output.ScheduleFrequencyType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_SCHEDULE_FREQUENCY );
            output.ScheduleTimingType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_SCHEDULE_TIMING );
            //
            output.EstimatedCost = CostProfileManager.GetAll( output.RowId );
            output.CommonCosts = Entity_CommonCostManager.GetAll( output.RowId );

            //Include currencies to fix null errors in various places (detail, compare, publishing) - NA 3/17/2017
            var currencies = CodesManager.GetCurrencies();
            //Include cost types to fix other null errors - NA 3/31/2017
            var costTypes = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_CREDENTIAL_ATTAINMENT_COST );
            foreach ( var cost in output.EstimatedCost )
            {
                cost.CurrencyTypes = currencies;

                foreach ( var costItem in cost.Items )
                {
                    costItem.DirectCostType.Items.Add( costTypes.Items.FirstOrDefault( m => m.CodeId == costItem.CostTypeId ) );
                }
            }
            //check if this returns the dsp data. if not should be ignored. 
            output.AggregateData = Entity_AggregateDataProfileManager.GetAll( output.RowId, true, isAPIRequest );

            //
            output.HasSupportService = Entity_HasSupportServiceManager.GetAllAsResourceSummary( relatedEntity );

        } //

        #endregion

    }
}

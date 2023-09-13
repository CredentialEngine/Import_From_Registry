using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

using workIT.Models;
using workIT.Models.ProfileModels;
using workIT.Models.Common;
using workIT.Utilities;

using DBResource = workIT.Data.Tables.VerificationServiceProfile;
using EntityContext = workIT.Data.Tables.workITEntities;
using ThisResource = workIT.Models.ProfileModels.VerificationServiceProfile;

namespace workIT.Factories
{
    public class VerificationServiceProfileManager : BaseFactory
    {
        static readonly string thisClassName = "VerificationServiceProfileManager";
        static string EntityType = "VerificationServiceProfile";
        static int EntityTypeId = CodesManager.ENTITY_TYPE_VERIFICATION_PROFILE;

        EntityManager entityMgr = new EntityManager();

        #region VerificationServiceProfile - persistance ==================
        /// <summary>
        /// Update a VerificationServiceProfile
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
                        DBResource efEntity = context.VerificationServiceProfile
                                .SingleOrDefault( s => s.Id == entity.Id );

                        if ( efEntity != null && efEntity.Id > 0 )
                        {
                            //fill in fields that may not be in entity
                            entity.RowId = efEntity.RowId;

                            MapToDB( entity, efEntity );


                            if ( IsValidDate( status.EnvelopeCreatedDate ) && status.LocalCreatedDate < efEntity.Created )
                            {
                                efEntity.Created = status.LocalCreatedDate;
                            }
                            if ( IsValidDate( status.EnvelopeUpdatedDate ) && status.LocalUpdatedDate != efEntity.LastUpdated )
                            {
                                efEntity.LastUpdated = status.LocalUpdatedDate;
                            }
                            efEntity.EntityStateId = 3;
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
                                    string message = string.Format( thisClassName + ".Save Failed", "Attempted to update a VerificationServiceProfile. The process appeared to not work, but was not an exception, so we have no message, or no clue. VerificationServiceProfile: {0}, Id: {1}", entity.Name, entity.Id );
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
                                    ActivityType = "VerificationServiceProfile",
                                    Activity = "Import",
                                    Event = "Update",
                                    Comment = string.Format( "VerificationServiceProfile was updated by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ),
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
                string message = HandleDBValidationError( dbex, thisClassName + string.Format( ".Save. id: {0}, Name: {1}", entity.Id, entity.Name ), "VerificationServiceProfile" );
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
        /// add a VerificationServiceProfile
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
                    context.VerificationServiceProfile.Add( efEntity );

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
                            ActivityType = "VerificationServiceProfile",
                            Activity = "Import",
                            Event = "Add",
                            Comment = string.Format( "Full VerificationServiceProfile was added by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ),
                            ActivityObjectId = entity.Id
                        };
                        new ActivityManager().SiteActivityAdd( sa );
                        if ( UpdateParts( entity, ref status ) == false )
                        {
                        }

                        return efEntity.Id;
                    }
                    else
                    {
                        //?no info on error

                        string message = thisClassName + string.Format( ". Add Failed", "Attempted to add a VerificationServiceProfile. The process appeared to not work, but was not an exception, so we have no message, or no clue. VerificationServiceProfile: {0}, ctid: {1}", entity.Name, entity.CTID );
                        status.AddError( thisClassName + ". Error - the add was not successful. " + message );
                        EmailManager.NotifyAdmin( "VerificationServiceProfileManager. Add Failed", message );
                    }
                }
                catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
                {
                    string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", "VerificationServiceProfile" );
                    status.AddError( thisClassName + ".Add(). Error - the save was not successful. " + message );

                    LoggingHelper.LogError( message, true );
                }
                catch ( Exception ex )
                {
                    string message = FormatExceptions( ex );
                    LoggingHelper.LogError( ex, thisClassName + string.Format( ".Add(), Name: {0}\r\n", FormatLongLabel(efEntity.Description) ), true );
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
                    //make sure this method allows pending status
                    var entity = GetByCtid( ctid );
                    if ( entity != null && entity.Id > 0 )
                        return entity.Id;

                    //only add DB required properties
                    //NOTE - an entity will be created via trigger
                    efEntity.Description = "Placeholder until full document is downloaded";
                    efEntity.EntityStateId = 1;
                    efEntity.RowId = entityUid;
                    //watch that Ctid can be  updated if not provided now!!
                    efEntity.CTID = ctid;
                    efEntity.SubjectWebpage = registryAtId;

                    efEntity.Created = System.DateTime.Now;
                    efEntity.LastUpdated = System.DateTime.Now;


                    context.VerificationServiceProfile.Add( efEntity );
                    int count = context.SaveChanges();
                    if ( count > 0 )
                    {
                        SiteActivity sa = new SiteActivity()
                        {
                            ActivityType = EntityType,
                            Activity = "Import",
                            Event = string.Format( "Add Pending {0}", EntityType ),
                            Comment = string.Format( "Pending {0} was added by the import. ctid: {1}, registryAtId: {2}", EntityType, ctid, registryAtId ),
                            ActivityObjectCTID = efEntity.CTID,
                            ActivityObjectId = efEntity.Id
                        };
                        new ActivityManager().SiteActivityAdd( sa );
                        //Question should this be in the EntityCache?
                        //SaveStatus status = new SaveStatus();
                        entity.Id = efEntity.Id;
                        entity.RowId = efEntity.RowId;
                        entity.CTID = efEntity.CTID;
                        entity.EntityStateId = 1;
                        entity.Name = "Verification Service Profile";
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
                Name = FormatLongLabel( document.Description ),
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
                //status.AddWarning( "An VerificationServiceProfile Description must be entered" );
            }


            //if ( string.IsNullOrWhiteSpace( profile.SubjectWebpage ) )
            //    status.AddWarning( "Error - A Subject Webpage name must be entered" );

            //else if ( !IsUrlValid( profile.SubjectWebpage, ref commonStatusMessage ) )
            //{
            //    status.AddWarning( "The VerificationServiceProfile Subject Webpage is invalid. " + commonStatusMessage );
            //}


            return status.WasSectionValid;
        }


        /// <summary>
        /// Delete an VerificationServiceProfile, and related Entity
        /// </summary>
        /// <param name="ctid"></param>
        /// <param name="statusMessage"></param>
        /// <returns></returns>
        public bool Delete( string ctid, ref List<string> messages )
        {
            bool isValid = true;
            if ( string.IsNullOrWhiteSpace( ctid ) )
            {
                messages.Add( thisClassName + ".Delete() Error - a valid CTID must be provided");
                return false;
            }
            if ( string.IsNullOrWhiteSpace( ctid ) )
                ctid = "SKIP ME";

            using ( var context = new EntityContext() )
            {
                try
                {
                    context.Configuration.LazyLoadingEnabled = false;
                    DBResource efEntity = context.VerificationServiceProfile
                                .FirstOrDefault( s => ( s.CTID == ctid )
                                );

                    if ( efEntity != null && efEntity.Id > 0 )
                    {
                        Guid rowId = efEntity.RowId;

                        //need to remove Entity - using before delete trigger 
                        string msg = string.Format( " VerificationServiceProfile. Id: {0}, Name: {1}, Ctid: {2}.", efEntity.Id, FormatLongLabel( efEntity.Description ), efEntity.CTID );
                        //need to remove from related entities 
                        if (!new Entity_UsesVerificationServiceManager().DeleteAll( efEntity.Id, ref messages ))
                        {
                            //errors may cause the delete to fail?
                        }
                        if ( !new Entity_HasVerificationServiceManager().DeleteAll( efEntity.Id, ref messages ) )
                        {

                        }
                        //
                        context.VerificationServiceProfile.Remove( efEntity );
                        //efEntity.LastUpdated = System.DateTime.Now;
                        int count = context.SaveChanges();
                        if ( count > 0 )
                        {
                            new ActivityManager().SiteActivityAdd( new SiteActivity()
                            {
                                ActivityType = "VerificationServiceProfile",
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
                    messages.Add( msg);
                    if ( msg.ToLower().IndexOf( "the delete statement conflicted with the reference constraint" ) > -1 )
                    {
                        messages.Add( "Error: this VerificationServiceProfile cannot be deleted as it is being referenced by other items, such as roles or credentials. These associations must be removed before this VerificationServiceProfile can be deleted.");
                    }
                }
            }
            return isValid;
        }

        #region VerificationServiceProfile properties ===================
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

            //CostProfile
            CostProfileManager cpm = new Factories.CostProfileManager();
            cpm.SaveList( entity.EstimatedCost, entity.RowId, ref status );

            int newId = 0;
            Entity_CredentialManager ecm = new Entity_CredentialManager();
            ecm.DeleteAll( relatedEntity, ref status );
            if ( entity.TargetCredentialIds != null && entity.TargetCredentialIds.Count > 0 )
            {
                foreach ( int id in entity.TargetCredentialIds )
                {
                    ecm.Add( entity.RowId, id, BaseFactory.RELATIONSHIP_TYPE_HAS_PART, ref newId, ref status );
                }
            }
            //
            //JurisdictionProfile 
            Entity_JurisdictionProfileManager jpm = new Entity_JurisdictionProfileManager();
            //do deletes - NOTE: other jurisdictions are added in: UpdateAssertedIns
            jpm.DeleteAll( relatedEntity, ref status );
            jpm.SaveList( entity.Jurisdiction, entity.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_SCOPE, ref status );
            jpm.SaveAssertedInList( entity.RowId, Entity_AgentRelationshipManager.ROLE_TYPE_OFFERED_BY, entity.OfferedIn, ref status );


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

                if ( mgr.AddProperties( entity.VerifiedClaimType, entity.RowId, EntityTypeId, CodesManager.PROPERTY_CATEGORY_CLAIM_TYPE, false, ref status ) == false )
                    isAllValid = false;


                isAllValid = false;
            }
            catch ( Exception ex )
            {
                LoggingHelper.DoTrace( 1, string.Format( "**EXCEPTION** {0}.UpdateProperties. Name: {1}, Id: {2}, Message: {3}.", thisClassName, entity.Name, entity.Id, ex.Message ) );
            }

            LoggingHelper.DoTrace( 7, thisClassName + ".UpdateProperties - exited" );
            return isAllValid;
        }


        #endregion

        #endregion


        #region == Retrieval =======================
        /// <summary>
        /// Get short summary by CTID
        /// </summary>
        /// <param name="ctid"></param>
        /// <returns></returns>
        public static ThisResource GetByCtid( string ctid )
        {
            ThisResource entity = new ThisResource();
            using ( var context = new EntityContext() )
            {
                DBResource from = context.VerificationServiceProfile
                        .FirstOrDefault( s => s.CTID.ToLower() == ctid.ToLower() );

                if ( from != null && from.Id > 0 )
                {
                    entity.RowId = from.RowId;
                    entity.Id = from.Id;
                    entity.EntityStateId = from.EntityStateId;
                    entity.Description = from.Description;
                    entity.SubjectWebpage = from.SubjectWebpage;
                    entity.CTID = from.CTID;
                }
            }
            return entity;
        }
        public static ThisResource Get( Guid profileUid )
        {
            ThisResource entity = new ThisResource();
            if ( !IsGuidValid( entity.RowId ) )
                return null;
            try
            {
                using ( var context = new EntityContext() )
                {
                    DBResource item = context.VerificationServiceProfile
                            .SingleOrDefault( s => s.RowId == profileUid );

                    if ( item != null && item.Id > 0 )
                    {
                        MapFromDB( item, entity, false );
                    }
                }
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, thisClassName + ".Get" );
            }
            return entity;
        }//
        public static ThisResource GetBasic( int id )
        {
            ThisResource entity = new ThisResource();
            using ( var context = new EntityContext() )
            {
                DBResource item = context.VerificationServiceProfile
                        .SingleOrDefault( s => s.Id == id );

                if ( item != null && item.Id > 0 )
                {
                    MapFromDB( item, entity, false );
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
                    var results = context.VerificationServiceProfile
                            .Where( s => s.OfferedBy == orgUid && s.EntityStateId == 3 )
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
        public static int GetAllTotal( Guid orgUid )
        {

            try
            {
                using ( var context = new EntityContext() )
                {
                    context.Configuration.LazyLoadingEnabled = false;
                    var results = context.VerificationServiceProfile
                            .Where( s => s.OfferedBy == orgUid && s.EntityStateId == 3 )
                            .OrderBy( s => s.Id )
                            .ToList();
                    if ( results != null && results.Any() )
                        return results.Count;

                }
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, thisClassName + ".GetAllTotal" );
            }
            return 0;
        }//

        public static ThisResource GetForDetail( int id )
        {
            ThisResource entity = new ThisResource();
            using ( var context = new EntityContext() )
            {
                DBResource item = context.VerificationServiceProfile
                        .SingleOrDefault( s => s.Id == id );

                if ( item != null && item.Id > 0 )
                {
                    MapFromDB( item, entity,
                            true //includingProperties
                            );
                }
            }

            return entity;
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
            foreach ( var item in list )
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

                using ( SqlCommand command = new SqlCommand( "[VerificationServiceProfile_Search]", c ) )
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

                        item = new VerificationServiceProfile();
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

        public static void MapToDB( ThisResource input, DBResource output )
        {

            //want output ensure fields input create are not wiped
            if ( output.Id == 0 )
            {
                output.CTID = input.CTID;
            }
            output.Id = input.Id;
            output.Description = GetData( input.Description );
            if ( IsValidDate( input.DateEffective ) )
                output.DateEffective = DateTime.Parse( input.DateEffective );
            else
                output.DateEffective = null;
            output.HolderMustAuthorize = input.HolderMustAuthorize;

            output.SubjectWebpage = GetUrlData( input.SubjectWebpage );

            if ( input.VerificationService != null && input.VerificationService.Count > 0 )
            {
                //json or simple list?
                output.VerificationService = GetListAsDelimitedString( input.VerificationService, "|" );
                //output.VerificationService = JsonConvert.SerializeObject( input.VerificationService, JsonHelper.GetJsonSettings( false ) );
            }
            if ( input.VerificationDirectory != null && input.VerificationDirectory.Count > 0 )
            {
                output.VerificationDirectory = GetListAsDelimitedString( input.VerificationDirectory, "|" );
                //output.VerificationDirectory = JsonConvert.SerializeObject( input.VerificationDirectory, JsonHelper.GetJsonSettings( false ) );
            }
            output.VerificationMethodDescription = input.VerificationMethodDescription;

            if ( input.OfferedByList != null && input.OfferedByList.Any() )
            {
                output.OfferedBy = input.OfferedByList[0];
            }
            else
            {
                output.OfferedBy = null;
            }

            if ( IsValidDate( input.LastUpdated ) )
                output.LastUpdated = input.LastUpdated;
        }

        public static void MapFromDB( DBResource input, ThisResource output, bool includingProperties )
        {
            output.Id = input.Id;
            output.RowId = input.RowId;
            //
            output.Name = FormatLongLabel( input.Description );
            output.CTID = input.CTID;
            output.Description = input.Description == null ? "" : input.Description;

            if ( IsValidDate( input.DateEffective ) )
                output.DateEffective = ( ( DateTime ) input.DateEffective ).ToString( "yyyy-MM-dd" );
            else
                output.DateEffective = null;
            if ( output.HolderMustAuthorize != null )
                input.HolderMustAuthorize = ( bool ) output.HolderMustAuthorize;
            
            if ( IsGuidValid( input.OfferedBy ) )
            {
                output.OfferedByAgentUid = ( Guid ) input.OfferedBy;
                output.OfferedByAgent = OrganizationManager.GetBasics( ( Guid ) input.OfferedBy );
            }
            //no owner, just offered by. just in case, get all 
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

                //seem to need org roles for later common code
                //var orp = Entity_AgentRelationshipManager.AgentEntityRole_GetAsEnumerationFromCSV( output.RowId, output.OwningOrganization.RowId );
                output.OrganizationRole = new OrganizationRoleManager().GetAllCombinedForTarget( CodesManager.ENTITY_TYPE_VERIFICATION_PROFILE, output.Id, output.OwningOrganizationId );
            }
            output.SubjectWebpage = input.SubjectWebpage;
            //output.VerificationDirectoryOLD = input.VerificationDirectory;
            //output.VerificationServiceOLD = input.VerificationService;
            if ( !string.IsNullOrWhiteSpace( input.VerificationDirectory ) )
            {
                output.VerificationDirectory = SplitDelimitedStringToList( input.VerificationDirectory, '|' );
            }
            output.VerificationMethodDescription = input.VerificationMethodDescription;
            if ( !string.IsNullOrWhiteSpace( input.VerificationService ) )
            {
                output.VerificationService = SplitDelimitedStringToList( input.VerificationDirectory, '|' );
            }
            output.VerifiedClaimType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_CLAIM_TYPE );

            if ( IsValidDate( input.Created ) )
                output.Created = ( DateTime ) input.Created;
            if ( IsValidDate( input.LastUpdated ) )
                output.LastUpdated = ( DateTime ) input.LastUpdated;


            //
            if ( !includingProperties )
            {
                return;
            }
            output.EstimatedCost = CostProfileManager.GetAll( output.RowId );

            output.Jurisdiction = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( output.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_SCOPE );


            //output.Region = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( output.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_RESIDENT );
            output.OfferedIn = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( output.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_OFFERREDIN );


            //not sure if doing here? Probably better to have a count?
            bool isForDetailPageCredential = true;
            //make sure this is minimum data
            output.TargetCredential = Entity_CredentialManager.GetAll( output.RowId, BaseFactory.RELATIONSHIP_TYPE_HAS_PART, isForDetailPageCredential );


            //===== not sure we need this, so skip
            //var relatedEntity = EntityManager.GetEntity( output.RowId, false );
            //if ( relatedEntity != null && relatedEntity.Id > 0 )
            //    output.EntityLastUpdated = relatedEntity.LastUpdated;



        } //

        #endregion
    }
}

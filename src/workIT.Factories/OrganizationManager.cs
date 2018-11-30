using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using workIT.Models;
using workIT.Models.Common;
using workIT.Models.Elastic;
using workIT.Models.ProfileModels;
using workIT.Utilities;
using DBEntity = workIT.Data.Tables.Organization;
using EntityContext = workIT.Data.Tables.workITEntities;
using ThisEntity = workIT.Models.Common.Organization;

namespace workIT.Factories
{
    public class OrganizationManager : BaseFactory
    {
        static string thisClassName = "OrganizationManager";
        EntityManager entityMgr = new EntityManager();
        Entity_ReferenceManager erm = new Entity_ReferenceManager();

        string statusMessage = "";

        #region Organization - persistance ==================


        /// <summary>
        /// Save an Organization
        /// - only from import
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public bool Save( ThisEntity entity, ref SaveStatus status )
        {
            bool isValid = true;
            int count = 0;
            try
            {
                using ( var context = new EntityContext() )
                {
                    //note for import, may still do updates?
                    if ( ValidateProfile( entity, ref status ) == false )
                    {
                        return false;
                    }

                    if ( entity.Id > 0 )
                    {
                        DBEntity efEntity = context.Organization
                                .SingleOrDefault( s => s.Id == entity.Id );

                        if ( efEntity != null && efEntity.Id > 0 )
                        {
                            //delete the entity and re-add
                            //Entity e = new Entity()
                            //{
                            //    EntityBaseId = efEntity.Id,
                            //    EntityTypeId = CodesManager.ENTITY_TYPE_ORGANIZATION,
                            //    EntityType = "Organization",
                            //    EntityUid = efEntity.RowId,
                            //    EntityBaseName = efEntity.Name
                            //};
                            //if ( entityMgr.ResetEntity( e, ref statusMessage ) )
                            //{

                            //}


                            //for updates, chances are some fields will not be in interface, don't map these (ie created stuff)
                            //**ensure rowId is passed down for use by profiles, etc
                            entity.RowId = efEntity.RowId;

                            MapToDB( entity, efEntity );
                            //assume and validate, that if we get here we have a full record
                            if ( ( efEntity.EntityStateId ?? 1 ) == 1 )
                            {
                                efEntity.EntityStateId = 3;
                                //identify credentials and other that will need to be reloaded in elasitc and caches
                            }

                            if ( HasStateChanged( context ) )
                            {
                                efEntity.LastUpdated = System.DateTime.Now;
                                //NOTE efEntity.EntityStateId is set to 0 in delete method )
                                count = context.SaveChanges();
                                //can be zero if no data changed
                                if ( count >= 0 )
                                {
                                    isValid = true;

                                }
                                else
                                {
                                    //?no info on error
                                    status.AddError( "Error - the update was not successful. " );
                                    isValid = false;
                                    string message = string.Format( thisClassName + ".Save Failed", "Attempted to update a Organization. The process appeared to not work, but was not an exception, so we have no message, or no clue. Organization: {0}, Id: {1}", entity.Name, entity.Id );
                                    EmailManager.NotifyAdmin( thisClassName + ". Import/Save Failed", message );
                                }
                            }
                            else
                            {
                                //update entity.LastUpdated - assuming there has to have been some change in related data
                                new EntityManager().UpdateModifiedDate( entity.RowId, ref status );
                            }

                            //continue with parts only if valid 
                            if ( isValid )
                            {
                                if ( !UpdateParts( entity, ref status ) )
                                    isValid = false;
                                SiteActivity sa = new SiteActivity()
                                {
                                    ActivityType = "Organization",
                                    Activity = "Import",
                                    Event = "Update",
                                    Comment = string.Format( "Organization was updated by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ),
                                    ActivityObjectId = entity.Id
                                };
                                new ActivityManager().SiteActivityAdd( sa );
                            }

                        }
                        else
                        {
                            status.AddError( string.Format( "Error - Save/Import failed, as record was not found. CredId: {0}", entity.Id ) );
                            isValid = false;
                        }
                    }
                    else
                    {
                        int newId = Add( entity, ref status );
                        if ( newId == 0 || status.HasErrors )
                            isValid = false;
                    }
                }
            }
            catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
            {
                string message = HandleDBValidationError( dbex, thisClassName + string.Format(".Save() id: {0}, Name: {1}", entity.Id, entity.Name), "Organization" );
                status.AddError( "Error - the save was not successful. " + message );
            }
            catch ( Exception ex )
            {
                string message = FormatExceptions( ex );
                LoggingHelper.LogError( ex, thisClassName + string.Format( ".Save. id: {0}, Name: {1}", entity.Id, entity.Name ) );
                status.AddError( thisClassName + " Error - the save was not successful. " + message );
                isValid = false;
            }


            return isValid;
        }

        /// <summary>
        /// add a Organization
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="statusMessage"></param>
        /// <returns></returns>
        private int Add( ThisEntity entity, ref SaveStatus status )
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
                    efEntity.Created = System.DateTime.Now;
                    efEntity.LastUpdated = System.DateTime.Now;
                    efEntity.EntityStateId = 3;

                    context.Organization.Add( efEntity );

                    // submit the change to database
                    int count = context.SaveChanges();
                    if ( count > 0 )
                    {
                        entity.Id = efEntity.Id;
                        entity.RowId = efEntity.RowId;

                        SiteActivity sa = new SiteActivity()
                        {
                            ActivityType = "Organization",
                            Activity = "Import",
                            Event = "Add",
                            Comment = string.Format( "Full Organization was added by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ),
                            ActivityObjectId = entity.Id
                        };
                        new ActivityManager().SiteActivityAdd( sa );
                        UpdateParts( entity, ref status );

                        return efEntity.Id;
                    }
                    else
                    {
                        //?no info on error

                        string message = string.Format( thisClassName + ".Add() Failed", "Attempted to add a Organization. The process appeared to not work, but was not an exception, so we have no message, or no clue. Organization: {0}, ctid: {1}", entity.Name, entity.CTID );
                        status.AddError( "Error - the add was not successful. " + message );
                        EmailManager.NotifyAdmin( "OrganizationManager. Add Failed", message );
                    }
                }
                catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
                {
                    string message = HandleDBValidationError( dbex, thisClassName + ".Save() ", "Organization" );
                    status.AddError( thisClassName + ".Save() Error - the save was not successful. " + message );
                    LoggingHelper.LogError( message, true );
                }
                catch ( Exception ex )
                {
                    string message = FormatExceptions( ex );
                    LoggingHelper.LogError( ex, thisClassName + string.Format( ".Add(), Name: {0}", efEntity.Name ) );
                    status.AddError( thisClassName + ".Save() Error - the save was not successful. " + message );
                }
            }

            return entity.Id;
        }
        public int AddBaseReference( ThisEntity entity, ref SaveStatus status )
        {
            DBEntity efEntity = new DBEntity();
            try
            {
                using ( var context = new EntityContext() )
                {
                    if ( entity == null ||
                        ( string.IsNullOrWhiteSpace( entity.Name ) ||
                        string.IsNullOrWhiteSpace( entity.SubjectWebpage )
                        ) )
                    {
                        status.AddError( thisClassName + string.Format(". AddBaseReference() The organization is incomplete. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage) );
                        return 0;
                    }

                    //only add DB required properties
                    //NOTE - an entity will be created via trigger
                    efEntity.EntityStateId = 2;
                    efEntity.Name = entity.Name;
                    efEntity.Description = entity.Description;
                    efEntity.SubjectWebpage = entity.SubjectWebpage;
                    efEntity.ISQAOrganization = entity.ISQAOrganization;
                    if ( IsValidGuid( entity.RowId ) )
                        efEntity.RowId = entity.RowId;
                    else
                        efEntity.RowId = Guid.NewGuid();
                    //set to return, just in case
                    entity.RowId = efEntity.RowId;
                    efEntity.IsThirdPartyOrganization = true;
                    efEntity.Created = System.DateTime.Now;
                    efEntity.LastUpdated = System.DateTime.Now;

                    context.Organization.Add( efEntity );
                    int count = context.SaveChanges();
                    if ( count > 0 )
                    {
                        SiteActivity sa = new SiteActivity()
                        {
                            ActivityType = "Organization",
                            Activity = "Import",
                            Event = "Add Base Reference",
                            Comment = string.Format( "Pending Organization was added by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ),
                            ActivityObjectId = entity.Id
                        };
                        new ActivityManager().SiteActivityAdd( sa );

                        if ( erm.Add( entity.SocialMediaPages, entity.RowId, CodesManager.ENTITY_TYPE_ORGANIZATION, ref status, CodesManager.PROPERTY_CATEGORY_ORGANIZATION_SOCIAL_MEDIA, true ) == false )
                        {
                            //isAllValid = false;
                        }


                        return efEntity.Id;
                    }

                    status.AddError( thisClassName + ". AddBaseReference() Error - the save was not successful, but no message provided. " );
                }
            }

            catch ( Exception ex )
            {
                string message = FormatExceptions( ex );
                LoggingHelper.LogError( ex, thisClassName + string.Format( ".AddBaseReference. Name:  {0}, SubjectWebpage: {1}", entity.Name, entity.SubjectWebpage ) );
                status.AddError( thisClassName + string.Format( ".AddBaseReference(). Error - the save was not successful. Name:  {0}, SubjectWebpage: {1}, message: ", entity.Name, entity.SubjectWebpage, message ) );

            }
            return 0;
        }
        public int AddPendingRecord( Guid entityUid, string ctid, string registryAtId, ref string status )
        {
            DBEntity efEntity = new DBEntity();
            try
            {
                using ( var context = new EntityContext() )
                {
                    if ( !IsValidGuid( entityUid ) )
                    {
                        status = "A valid GUID must be provided to create a pending entity";
                        return 0;
                    }

                    //quick check to ensure not existing
                    ThisEntity entity = GetByCtid( ctid );
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

                    context.Organization.Add( efEntity );
                    int count = context.SaveChanges();
                    if ( count > 0 )
                    {
                        SiteActivity sa = new SiteActivity()
                        {
                            ActivityType = "Organization",
                            Activity = "Import",
                            Event = "Add Pending Organization",
                            Comment = string.Format( "Pending Organization was added by the import. ctid: {0}, registryAtId: {1}", ctid, registryAtId ),
                            ActivityObjectId = efEntity.Id
                        };
                        new ActivityManager().SiteActivityAdd( sa );
                        return efEntity.Id;
                    }

                    status = thisClassName + ".AddPendingRecord. Error - the save was not successful, but no message provided. ";
                }
            }

            catch ( Exception ex )
            {
                string message = FormatExceptions( ex );
                LoggingHelper.LogError( ex, thisClassName + string.Format( ".AddPendingRecord. entityUid:  {0}, ctid: {1}", entityUid, ctid ) );
                status = thisClassName + ".AddPendingRecord(). Error - the save was not successful. " + message;

            }
            return 0;
        }


        public bool UpdateParts( ThisEntity entity, ref SaveStatus status )
        {

            bool isAllValid = true;

            Entity relatedEntity = EntityManager.GetEntity( entity.RowId );
            if ( relatedEntity == null || relatedEntity.Id == 0 )
            {
                status.AddError( "Error - the related Entity was not found." );
                return false;
            }

            if ( UpdateProperties( entity, relatedEntity, ref status ) == false )
                isAllValid = false;

            //NOTE - in workIT changed to store services as properties
            //if ( !new OrganizationServiceManager().OrganizationService_Update( entity, false, ref statusMessage ) )
            //	isAllValid = false;
            //handle jurisdictions? ==> direct

            //Entity_ReferenceManager erm = new Entity_ReferenceManager();
            erm.DeleteAll( relatedEntity, ref status );

            if ( erm.Add( entity.Keyword, entity.RowId, CodesManager.ENTITY_TYPE_ORGANIZATION, ref status, CodesManager.PROPERTY_CATEGORY_KEYWORD, false ) == false )
                isAllValid = false;

            if ( erm.Add( entity.AlternateNames, entity.RowId, CodesManager.ENTITY_TYPE_ORGANIZATION, ref status, CodesManager.PROPERTY_CATEGORY_ALTERNATE_NAME, false ) == false )
                isAllValid = false;

            if ( erm.Add( entity.SocialMediaPages, entity.RowId, CodesManager.ENTITY_TYPE_ORGANIZATION, ref status, CodesManager.PROPERTY_CATEGORY_ORGANIZATION_SOCIAL_MEDIA, true ) == false )
                isAllValid = false;

            //how to handle notifications on 'other'?
            if ( erm.Add( entity.IdentificationCodes, entity.RowId, CodesManager.ENTITY_TYPE_ORGANIZATION, ref status, CodesManager.PROPERTY_CATEGORY_ORGANIZATION_IDENTIFIERS, true ) == false )
                isAllValid = false;

            if ( erm.Add( entity.Emails, entity.RowId, CodesManager.ENTITY_TYPE_ORGANIZATION, ref status, CodesManager.PROPERTY_CATEGORY_EMAIL_TYPE, true ) == false )
                isAllValid = false;

            Entity_ReferenceFrameworkManager erfm = new Entity_ReferenceFrameworkManager();
            erfm.DeleteAll( relatedEntity, ref status );

            //Entity_FrameworkItemManager efim = new Entity_FrameworkItemManager();
            if ( erfm.SaveList( relatedEntity.Id, CodesManager.PROPERTY_CATEGORY_NAICS, entity.Industries, ref status ) == false )
                isAllValid = false;

            //TODO - handle Naics if provided separately
			//		- note could result in duplicates
            if ( erfm.NaicsSaveList( relatedEntity.Id, CodesManager.PROPERTY_CATEGORY_NAICS, entity.Naics, ref status ) == false )
                isAllValid = false;

            //departments


            //subsiduaries


            AddProfiles( entity, relatedEntity, ref status );

            UpdateAssertedBys( entity, ref status );
            UpdateAssertedIns( entity, ref status );

            UpdateProvidesAssertions( entity, ref status );

            return isAllValid;
        }

        public bool UpdateProperties( ThisEntity entity, Entity relatedEntity, ref SaveStatus status )
        {
            bool isAllValid = true;
            //==== convert to entity properties ========================

            EntityPropertyManager mgr = new EntityPropertyManager();
            //first clear all propertiesd
            mgr.DeleteAll( relatedEntity, ref status );

            if ( mgr.AddProperties( entity.AgentType, entity.RowId, CodesManager.ENTITY_TYPE_ORGANIZATION, CodesManager.PROPERTY_CATEGORY_ORGANIZATION_TYPE, true, ref status ) == false )
                isAllValid = false;

            //TODO - may want a check to toggle the IsQaOrg property. It is used for other checks
            // however this would not be dependable, need to query on MapFromDB

            if ( mgr.AddProperties( entity.OrganizationSectorType, entity.RowId, CodesManager.ENTITY_TYPE_ORGANIZATION, CodesManager.PROPERTY_CATEGORY_ORGANIZATION_SECTORTYPE, false, ref status ) == false )
            {
                isAllValid = false;
            }

            //using Entity.Property in workIT, rather than Organization.Service
            if ( mgr.AddProperties( entity.ServiceType, entity.RowId, CodesManager.ENTITY_TYPE_ORGANIZATION, CodesManager.PROPERTY_CATEGORY_ORG_SERVICE, false, ref status ) == false )
                isAllValid = false;


            //}
            return isAllValid;
        }

        public void AddProfiles( ThisEntity entity, Entity relatedEntity, ref SaveStatus status )
        {
            //AlternativeIdentifier
            new Entity_IdentifierValueManager().SaveList( entity.AlternativeIdentifierList, entity.RowId, Entity_IdentifierValueManager.ORGANIZATION_AlternativeIdentifier, ref status );

            //ProcessProfile
            Entity_ProcessProfileManager ppm = new Factories.Entity_ProcessProfileManager();
            ppm.DeleteAll( relatedEntity, ref status );
            ppm.SaveList( entity.AdministrationProcess, Entity_ProcessProfileManager.ADMIN_PROCESS_TYPE, entity.RowId, ref status );
            ppm.SaveList( entity.AppealProcess, Entity_ProcessProfileManager.APPEAL_PROCESS_TYPE, entity.RowId, ref status );
            ppm.SaveList( entity.ComplaintProcess, Entity_ProcessProfileManager.COMPLAINT_PROCESS_TYPE, entity.RowId, ref status );
            ppm.SaveList( entity.DevelopmentProcess, Entity_ProcessProfileManager.DEV_PROCESS_TYPE, entity.RowId, ref status );
            ppm.SaveList( entity.MaintenanceProcess, Entity_ProcessProfileManager.MTCE_PROCESS_TYPE, entity.RowId, ref status );
            ppm.SaveList( entity.ReviewProcess, Entity_ProcessProfileManager.REVIEW_PROCESS_TYPE, entity.RowId, ref status );
            ppm.SaveList( entity.RevocationProcess, Entity_ProcessProfileManager.REVOKE_PROCESS_TYPE, entity.RowId, ref status );

            //contact points (does delete all - check on implications from address manager)
            new Entity_ContactPointManager().SaveList( entity.ContactPoint, entity.RowId, ref status );

            //addresses
            new Entity_AddressManager().SaveList( entity.Addresses, entity.RowId, ref status );

            //JurisdictionProfile 
            Entity_JurisdictionProfileManager jpm = new Entity_JurisdictionProfileManager();
            //do deletes - NOTE: other jurisdictions are added in: UpdateAssertedIns
            jpm.DeleteAll( relatedEntity, ref status );
            jpm.SaveList( entity.Jurisdiction, entity.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_SCOPE, ref status );

            //the add is done from the CostManifest import, SO DON'T DO HERE
            //new ConditionManifestManager().HasConditionManifest_SaveList( entity.ConditionManifestIds, entity.RowId, ref status );
            //the add is done from the CostManifest import, SO DON'T DO HERE
            //new CostManifestManager().EntityCostManifest_SaveList( entity.CostManifestIds, entity.RowId, ref status );

            new Entity_VerificationProfileManager().SaveList( entity.VerificationServiceProfiles, entity.RowId, ref status );


        }

        /// <summary>
        /// Handle assertions by other entities
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public bool UpdateAssertedBys( ThisEntity entity, ref SaveStatus status )
        {
            bool isAllValid = true;
            Entity_AgentRelationshipManager mgr = new Entity_AgentRelationshipManager();
            Entity parent = EntityManager.GetEntity( entity.RowId );
            if ( parent == null || parent.Id == 0 )
            {
                status.AddError( thisClassName + " - Error - the parent entity was not found." );
                return false;
            }
            //do deletes - should this be done here, should be no other prior updates?
            mgr.DeleteAll( parent, ref status );

            mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_AccreditedBy, entity.AccreditedBy, ref status );
            mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_ApprovedBy, entity.ApprovedBy, ref status );

            mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_RecognizedBy, entity.RecognizedBy, ref status );

            mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_RegulatedBy, entity.RegulatedBy, ref status );

            //including dept and suborg here, but want to move
            mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_DEPARTMENT, entity.Departments, ref status );
            mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_SUBSIDIARY, entity.SubOrganizations, ref status );

            //parent technically uses the same role type, but reversed
            //probably should just check for existance of parent, and add there,
            //or rational for uses Entity.Asserts
            mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_PART_ORG, entity.ParentOrganization, ref status );

            return isAllValid;
        }


        public void UpdateAssertedIns( ThisEntity entity, ref SaveStatus status )
        {
            Entity_JurisdictionProfileManager mgr = new Entity_JurisdictionProfileManager();
            Entity parent = EntityManager.GetEntity( entity.RowId );
            if ( parent == null || parent.Id == 0 )
            {
                status.AddError( thisClassName + " - Error - the parent entity was not found." );
                return;
            }
            //note the deleteAll is done in AddProfiles
            mgr.SaveAssertedInList( entity.RowId, Entity_AgentRelationshipManager.ROLE_TYPE_AccreditedBy, entity.AccreditedIn, ref status );
            mgr.SaveAssertedInList( entity.RowId, Entity_AgentRelationshipManager.ROLE_TYPE_ApprovedBy, entity.ApprovedIn, ref status );
            mgr.SaveAssertedInList( entity.RowId, Entity_AgentRelationshipManager.ROLE_TYPE_RecognizedBy, entity.RecognizedIn, ref status );
            mgr.SaveAssertedInList( entity.RowId, Entity_AgentRelationshipManager.ROLE_TYPE_RegulatedBy, entity.RegulatedIn, ref status );

        } //
          /// <summary>
          /// handle assertions on entities
          /// These need to be matched to Entity.AgentRelationships from the receiving entity!!
          /// </summary>
          /// <param name="entity"></param>
          /// <param name="status"></param>
          /// <returns></returns>
        public bool UpdateProvidesAssertions( ThisEntity entity, ref SaveStatus status )
        {
            bool isAllValid = true;
            Entity_AssertionManager mgr = new Entity_AssertionManager();
            Entity parent = EntityManager.GetEntity( entity.RowId );
            if ( parent == null || parent.Id == 0 )
            {
                status.AddError( thisClassName + " - Error - the parent entity was not found." );
                return false;
            }
            mgr.DeleteAll( parent, ref status );

            mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_Accredits, entity.Accredits, ref status );
            mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_Approves, entity.Approves, ref status );
            mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_Recognizes, entity.Recognizes, ref status );
            mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_Regulates, entity.Regulates, ref status );

            //NOTE: these are not QA, and should not be assertions?
            //      However, if we only show direct assertions, then will be valid
            mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_OFFERS, entity.Offers, ref status );
            mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_OWNS, entity.Owns, ref status );
            mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_Renews, entity.Renews, ref status );
            mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_Revokes, entity.Revokes, ref status );


            return isAllValid;
        } //

        /// <summary>
        /// Delete an Organization, and related Entity
        /// </summary>
        /// <param name="orgId"></param>
        /// <param name="statusMessage"></param>
        /// <returns></returns>
        public bool Delete( int orgId, ref string statusMessage )
        {
            bool isValid = true;
            bool doingVirtualDelete = true;
            if ( orgId == 0 )
            {
                statusMessage = "Error - missing an identifier for the Organization";
                return false;
            }
            using ( var context = new EntityContext() )
            {
                try
                {
                    //ensure exists
                    DBEntity efEntity = context.Organization
                                .FirstOrDefault( s => s.Id == orgId );

                    if ( efEntity != null && efEntity.Id > 0 )
                    {
                        if ( doingVirtualDelete )
                        {
                            statusMessage = string.Format( "Organization: {0}, Id:{1}", efEntity.Name, efEntity.Id );

                            //context.Organization.Remove( efEntity );
                            efEntity.LastUpdated = System.DateTime.Now;

                        }
                        else
                        {
                            Guid rowId = efEntity.RowId;
                            int roleCount = 0;
                            //check for any existing org roles, and reject delete if any found
                            if ( Entity_AgentRelationshipManager.AgentEntityHasRoles( rowId, ref roleCount ) )
                            {
                                statusMessage = string.Format( "Error - this organization cannot be deleted as there are existing roles {0}.", roleCount );
                                return false;
                            }
                            //16-10-19 mp - we have a 'before delete' trigger to remove the Entity
                            //new EntityManager().Delete( rowId, ref statusMessage );

                            context.Organization.Remove( efEntity );
                        }


                        int count = context.SaveChanges();
                        if ( count > 0 )
                        {
                            isValid = true;

                        }
                    }
                    else
                    {
                        statusMessage = "Error - delete failed, as record was not found.";
                    }
                }
                catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
                {
                    string message = HandleDBValidationError( dbex, thisClassName + ".Save() ", "Organization" );
                    statusMessage = "Error - the Delete was not successful. " + message;
                }
                catch ( Exception ex )
                {
                    statusMessage = FormatExceptions( ex );
                    LoggingHelper.LogError( ex, thisClassName + ".Organization_Delete()" );

                    if ( statusMessage.ToLower().IndexOf( "the delete statement conflicted with the reference constraint" ) > -1 )
                    {
                        statusMessage = "Error: this organization cannot be deleted as it is being referenced by other items, such as credentials. These associations must be removed before this organization can be deleted.";
                    }
                }
            }

            return isValid;
        }

        /// <summary>
        /// Delete by envelopeId, or ctid
        /// </summary>
        /// <param name="envelopeId"></param>
        /// <param name="ctid"></param>
        /// <param name="statusMessage"></param>
        /// <returns></returns>
        public bool Delete( string envelopeId, string ctid, ref string statusMessage )
        {
            bool isValid = true;
            if ( (string.IsNullOrWhiteSpace( envelopeId ) || !IsValidGuid( envelopeId ))
                && string.IsNullOrWhiteSpace( ctid ) )
            {
                statusMessage = thisClassName + ".Delete() Error - a valid envelope identifier must be provided - OR  valid CTID";
                return false;
            }
            if ( string.IsNullOrWhiteSpace( ctid ) )
                ctid = "SKIP ME";

            using ( var context = new EntityContext() )
            {
                try
                {
                    context.Configuration.LazyLoadingEnabled = false;
                    DBEntity efEntity = context.Organization
                                .FirstOrDefault( s => s.CredentialRegistryId == envelopeId || ( s.CTID == ctid )
                                );

                    if ( efEntity != null && efEntity.Id > 0 )
                    {
                        Guid rowId = efEntity.RowId;
                        //issue check for ivy tech and others
                        if ( efEntity.CTID.ToLower() == "ce-1abb6c52-0f8c-4b17-9f89-7e9807673106" )
                        {
                            string msg2 = string.Format( "Request to delete Ivy Tech was encountered - ignoring. Organization. Id: {0}, Name: {1}, Ctid: {2}, EnvelopeId: {3}", efEntity.Id, efEntity.Name, efEntity.CTID, envelopeId );
                            EmailManager.NotifyAdmin( "Encountered Request to delete Ivy Tech", msg2 );
                            return true;
                        }
                        if ( efEntity.CTID.ToLower() == "ce-6686c066-0948-4c0b-8fb0-43c6af625a70" )
                        {
                            string msg2 = string.Format( "Request to delete NIMS was encountered - ignoring. Organization. Id: {0}, Name: {1}, Ctid: {2}, EnvelopeId: {3}", efEntity.Id, efEntity.Name, efEntity.CTID, envelopeId );
                            EmailManager.NotifyAdmin( "Encountered Request to delete NIMS", msg2 );
                            return true;
                        }
                        if ( efEntity.CTID.ToLower() == "ce-2ecc2ce8-b134-4a3a-8b17-863aa118f36e" )
                        {
                            string msg2 = string.Format( "Request to delete Ball State University was encountered - ignoring. Organization. Id: {0}, Name: {1}, Ctid: {2}, EnvelopeId: {3}", efEntity.Id, efEntity.Name, efEntity.CTID, envelopeId );
                            EmailManager.NotifyAdmin( "Encountered Request to delete Ball State University", msg2 );
                            return true;
                        }
                        //need to remove from Entity.
                        //-using before delete trigger - verify won't have RI issues
                        string msg = string.Format( " Organization. Id: {0}, Name: {1}, Ctid: {2}, EnvelopeId: {3}", efEntity.Id, efEntity.Name, efEntity.CTID, envelopeId );

                        //18-04-05 mparsons - change to set inactive, and notify - seems to have been some incorrect deletes

                        //context.Organization.Remove( efEntity );
                        efEntity.EntityStateId = 0;
                        efEntity.LastUpdated = System.DateTime.Now;

                        int count = context.SaveChanges();
                        if ( count > 0 )
                        {
                            //LoggingHelper.DoTrace( 2, "Organization virtually deleted: " + msg );
                            new ActivityManager().SiteActivityAdd( new SiteActivity()
                            {
                                ActivityType = "Organization",
                                Activity = "Management",
                                Event = "Delete",
								Comment = msg,
								ActivityObjectId = efEntity.Id
							} );
                            isValid = true;
                            //add pending request 
                            List<String> messages = new List<string>();
                            new SearchPendingReindexManager().AddDeleteRequest( CodesManager.ENTITY_TYPE_ORGANIZATION, efEntity.Id, ref messages );
                        }
                    }
                    else
                    {
                        statusMessage = thisClassName + ".Delete() Warning No action taken, as the record was not found.";
                    }
                }
                catch ( Exception ex )
                {
                    LoggingHelper.LogError( ex, thisClassName + ".Delete(envelopeId)" );
                    statusMessage = FormatExceptions( ex );
                    isValid = false;
                    if ( statusMessage.ToLower().IndexOf( "the delete statement conflicted with the reference constraint" ) > -1 )
                    {
                        statusMessage = thisClassName + "Error: this record cannot be deleted as it is being referenced by other items, such as roles or credentials. These associations must be removed before this assessment can be deleted.";
                    }
                }
            }
            return isValid;
        }
        public bool ValidateProfile( ThisEntity profile, ref SaveStatus status )
        {
            status.HasSectionErrors = false;

            if ( string.IsNullOrWhiteSpace( profile.Name ) )
            {
                status.AddError( "An organization name must be entered" );
            }
            if ( !string.IsNullOrWhiteSpace( profile.DateEffective ) && !IsValidDate( profile.DateEffective ) )
            {
                status.AddWarning( "Please enter a valid effective date" );
            }

            //if ( string.IsNullOrWhiteSpace( profile.SubjectWebpage ) )
            //	status.AddError( "A Subject Webpage must be entered" );

            //else 
            if ( !IsUrlValid( profile.SubjectWebpage, ref commonStatusMessage ) )
                status.AddWarning( "The Subject Webpage Url is invalid. " + commonStatusMessage );

            if ( string.IsNullOrWhiteSpace( profile.AgentDomainType ) )
            {
                status.AddWarning( "The type of this organization is required. It should be either CredentialOrganization, or QAOrganization. Defaulting to CredentialOrganization" );
                profile.AgentDomainType = "CredentialOrganization";
                profile.AgentTypeId = 1;
            }
            else if ( profile.AgentDomainType.IndexOf( "QACredentialOrganization" ) > -1 )
            {
                profile.AgentTypeId = 2;
                profile.ISQAOrganization = true;
            }
            else
                profile.AgentTypeId = 1;


            if ( !IsUrlValid( profile.AgentPurpose, ref commonStatusMessage ) )
                status.AddWarning( "The Agent Purpose Url is invalid. " + commonStatusMessage );

            if ( !IsUrlValid( profile.MissionAndGoalsStatement, ref commonStatusMessage ) )
                status.AddWarning( "The Mission and Goals Statement Url is invalid. " + commonStatusMessage );

            if ( !IsUrlValid( profile.AvailabilityListing, ref commonStatusMessage ) )
                status.AddWarning( "The Availability Listing Url is invalid. " + commonStatusMessage );

            if ( !IsUrlValid( profile.ImageUrl, ref commonStatusMessage ) )
            {
                status.AddWarning( "The Image Url is invalid. " + commonStatusMessage );
            }


            return !status.HasSectionErrors;
        }


        #endregion

        #region Retrieval 
        public static ThisEntity GetDetail( int id )
        {

            bool includeCredentials = true;
            bool includingRoles = true;
            ThisEntity entity = new ThisEntity();

            using ( var context = new Data.Tables.workITEntities() )
            {
                DBEntity item = context.Organization
                        .FirstOrDefault( s => s.Id == id
                        //&& s.StatusId <= CodesManager.ENTITY_STATUS_PUBLISHED 
                        );

                if ( item != null && item.Id > 0 )
                {
                    //check for virtual deletes
                    if (item.EntityStateId == 0)
                        return entity;

                    MapFromDB_ForDetail( item, entity
                        , true      //includingProperties
                        , includeCredentials
                        , includingRoles );
                }
            }

            return entity;
        }
        public static ThisEntity GetByCtid( string ctid )
        {
            ThisEntity to = new ThisEntity();
            using ( var context = new EntityContext() )
            {
                context.Configuration.LazyLoadingEnabled = false;
                DBEntity from = context.Organization
                        .FirstOrDefault( s => s.CTID.ToLower() == ctid.ToLower() );

                if ( from != null && from.Id > 0 )
                {
                    MapFromDB_ForSummary( from, to );
                }
            }
            return to;
        }
        public static ThisEntity GetBySubjectWebpage( string swp )
        {
            ThisEntity to = new ThisEntity();
            using ( var context = new EntityContext() )
            {
                context.Configuration.LazyLoadingEnabled = false;
                DBEntity from = context.Organization
                        .FirstOrDefault( s => s.SubjectWebpage.ToLower() == swp.ToLower() );

                if ( from != null && from.Id > 0 )
                {
                    MapFromDB_ForSummary( from, to );
                }
            }
            return to;
        }
        public static ThisEntity GetByName_SubjectWebpage( string name, string swp )
        {
            ThisEntity to = new ThisEntity();
            using ( var context = new EntityContext() )
            {
                context.Configuration.LazyLoadingEnabled = false;
                DBEntity from = context.Organization
                        .FirstOrDefault( s => s.Name.ToLower() == name.ToLower() && s.SubjectWebpage.ToLower() == swp.ToLower() );

                if ( from != null && from.Id > 0 )
                {
                    MapFromDB_ForSummary( from, to );
                }
            }
            return to;
        }
        /// <summary>
        /// Get a basic org - will not return a pending record
        /// </summary>
        /// <param name="rowId"></param>
        /// <returns></returns>
        public static ThisEntity GetBasics( Guid rowId )
        {
            ThisEntity to = new ThisEntity();
            using ( var context = new EntityContext() )
            {
                context.Configuration.LazyLoadingEnabled = false;
                //need to be careful with the entity state check as may be needed for resolution!
                DBEntity from = context.Organization
                        .FirstOrDefault( s => s.RowId == rowId && (s.EntityStateId ?? 1) > 1 );

                if ( from != null && from.Id > 0 )
                {
                    MapFromDB_ForSummary( from, to );
                }
            }

            return to;
        }

        /// <summary>
        /// Check if org exists in any state
        /// </summary>
        /// <param name="rowId"></param>
        /// <returns></returns>
        public static ThisEntity Exists( Guid rowId )
        {
            ThisEntity to = new ThisEntity();
            using ( var context = new EntityContext() )
            {
                context.Configuration.LazyLoadingEnabled = false;
                //need to be careful with the entity state check as may be needed for resolution!
                DBEntity from = context.Organization
                        .FirstOrDefault( s => s.RowId == rowId );

                if ( from != null && from.Id > 0 )
                {
                    MapFromDB_ForSummary( from, to );
                }
            }

            return to;
        }
        /// <summary>
        /// Get minimum org for display in search results
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static ThisEntity GetForSummary( int id, bool includingRoles = false )
        {

            ThisEntity to = new ThisEntity();

            using ( var context = new EntityContext() )
            {
                context.Configuration.LazyLoadingEnabled = false;
                DBEntity from = context.Organization
                        .SingleOrDefault( s => s.Id == id );

                if ( from != null && from.Id > 0 )
                {
                    MapFromDB_ForSummary( from, to );
                    if ( includingRoles )
                    {
                        GetOrgRoles( from, to );
                    }
                }
            }

            return to;
        }
        public static ThisEntity GetForSummary( Guid id )
        {

            ThisEntity to = new ThisEntity();

            using ( var context = new EntityContext() )
            {
                context.Configuration.LazyLoadingEnabled = false;
                DBEntity from = context.Organization
                        .SingleOrDefault( s => s.RowId == id );

                if ( from != null && from.Id > 0 )
                {
                    //if a placeholder, skip
                    if ( from.EntityStateId < 2 )
                        return to;

                    MapFromDB_ForSummary( from, to );
                }
            }

            return to;
        }

        public static List<string> Autocomplete( string pFilter, int pageNumber, int pageSize, int userId, ref int pTotalRows )
        {
            bool autocomplete = true;
            List<string> results = new List<string>();
            List<OrganizationSummary> list = MainSearch( pFilter, "", pageNumber, pageSize, ref pTotalRows, false, autocomplete );

            string prevName = "";
            foreach ( OrganizationSummary item in list )
            {
                //note excluding duplicates may have an impact on selected max terms
                if ( item.Name.ToLower() != prevName )
                    results.Add( item.Name );

                prevName = item.Name.ToLower();
            }
            return results;
        }

        public static List<ThisEntity> Search( string pFilter, string pOrderBy, int pageNumber, int pageSize, ref int pTotalRows, bool idsOnly = false, bool autocomplete = false )
        {
            string connectionString = DBConnectionRO();
            ThisEntity item = new ThisEntity();
            List<ThisEntity> list = new List<ThisEntity>();
            var result = new DataTable();
            using ( SqlConnection c = new SqlConnection( connectionString ) )
            {
                c.Open();

                if ( string.IsNullOrEmpty( pFilter ) )
                {
                    pFilter = "";
                }

                using ( SqlCommand command = new SqlCommand( "OrganizationSearch", c ) )
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add( new SqlParameter( "@Filter", pFilter ) );
                    command.Parameters.Add( new SqlParameter( "@SortOrder", pOrderBy ) );
                    command.Parameters.Add( new SqlParameter( "@StartPageIndex", pageNumber ) );
                    command.Parameters.Add( new SqlParameter( "@PageSize", pageSize ) );

                    SqlParameter totalRows = new SqlParameter( "@TotalRows", pTotalRows );
                    totalRows.Direction = ParameterDirection.Output;
                    command.Parameters.Add( totalRows );

                    using ( SqlDataAdapter adapter = new SqlDataAdapter() )
                    {
                        adapter.SelectCommand = command;
                        adapter.Fill( result );
                    }
                    string rows = command.Parameters[command.Parameters.Count - 1].Value.ToString();
                    try
                    {
                        pTotalRows = Int32.Parse( rows );
                    }
                    catch
                    {
                        pTotalRows = 0;
                    }
                }

                foreach ( DataRow dr in result.Rows )
                {
                    item = new ThisEntity();
                    item.Id = GetRowColumn( dr, "Id", 0 );
                    item.Name = GetRowColumn( dr, "Name", "missing" );
                    item.FriendlyName = FormatFriendlyTitle( item.Name );

                    if ( idsOnly || autocomplete )
                    {
                        list.Add( item );
                        continue;
                    }
                    item.Description = GetRowColumn( dr, "Description", "" );
                    string rowId = GetRowColumn( dr, "RowId" );
                    item.RowId = new Guid( rowId );
                    item.CTID = GetRowPossibleColumn( dr, "CTID", "" );
                    item.SubjectWebpage = GetRowColumn( dr, "SubjectWebpage", "" );
                    //item.CanEditRecord = GetRowColumn( dr, "CanEditRecord", false );
                    item.CredentialRegistryId = GetRowPossibleColumn( dr, "CredentialRegistryId", "" );

                    item.ImageUrl = GetRowColumn( dr, "ImageUrl", "" );
                    if ( GetRowColumn( dr, "CredentialCount", 0 ) > 0 )
                        item.IsACredentialingOrg = true;
                    item.ISQAOrganization = GetRowColumn( dr, "IsAQAOrganization", false );

                    //item.MainPhoneNumber = PhoneNumber.DisplayPhone( GetRowColumn( dr, "MainPhoneNumber", "" ) );

                    //all addressess
                    //int addressess = GetRowPossibleColumn( dr, "AvailableAddresses", 0 );
                    //if ( addressess > 0 )
                    //{
                    //    //item.Addresses = AddressProfileManager.GetAllOrgAddresses( item.Id );
                    //    item.Addresses = Entity_AddressManager.GetAll( item.RowId );
                    //    //just in case (short term
                    //    if ( item.Addresses.Count > 0 )
                    //        item.Address = item.Addresses[0];
                    //}

                    //Edit - Added to fill out gray boxes in results - NA 5/12/2017
                    item.AgentType = EntityPropertyManager.FillEnumeration( item.RowId, CodesManager.PROPERTY_CATEGORY_ORGANIZATION_TYPE );
                    item.OrganizationSectorType = EntityPropertyManager.FillEnumeration( item.RowId, CodesManager.PROPERTY_CATEGORY_ORGANIZATION_SECTORTYPE );
                    //End Edit

                    list.Add( item );
                }

                return list;

            }
        }

        public static List<OrganizationSummary> MainSearch( string pFilter, string pOrderBy, int pageNumber, int pageSize, ref int pTotalRows, bool idsOnly = false, bool autocomplete = false )
        {
            string connectionString = DBConnectionRO();
            OrganizationSummary item = new OrganizationSummary();
            List<OrganizationSummary> list = new List<OrganizationSummary>();
            var result = new DataTable();
            using ( SqlConnection c = new SqlConnection( connectionString ) )
            {
                c.Open();

                if ( string.IsNullOrEmpty( pFilter ) )
                {
                    pFilter = "";
                }

                using ( SqlCommand command = new SqlCommand( "[OrganizationSearch]", c ) )
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add( new SqlParameter( "@Filter", pFilter ) );
                    command.Parameters.Add( new SqlParameter( "@SortOrder", pOrderBy ) );
                    command.Parameters.Add( new SqlParameter( "@StartPageIndex", pageNumber ) );
                    command.Parameters.Add( new SqlParameter( "@PageSize", pageSize ) );
                    //command.Parameters.Add( new SqlParameter( "@CurrentUserId", userId ) );

                    SqlParameter totalRows = new SqlParameter( "@TotalRows", pTotalRows );
                    totalRows.Direction = ParameterDirection.Output;
                    command.Parameters.Add( totalRows );

                    using ( SqlDataAdapter adapter = new SqlDataAdapter() )
                    {
                        adapter.SelectCommand = command;
                        adapter.Fill( result );
                    }
                    string rows = command.Parameters[command.Parameters.Count - 1].Value.ToString();
                    try
                    {
                        pTotalRows = Int32.Parse( rows );
                    }
                    catch
                    {
                        pTotalRows = 0;
                    }
                }

                foreach ( DataRow dr in result.Rows )
                {
                    item = new OrganizationSummary();
                    item.Id = GetRowColumn( dr, "Id", 0 );
                    item.Name = GetRowColumn( dr, "Name", "missing" );
                    item.FriendlyName = FormatFriendlyTitle( item.Name );

                    if ( idsOnly || autocomplete )
                    {
                        list.Add( item );
                        continue;
                    }
                    item.Description = GetRowColumn( dr, "Description", "" );
                    string rowId = GetRowColumn( dr, "RowId" );
                    item.RowId = new Guid( rowId );
                    item.CTID = GetRowPossibleColumn( dr, "CTID", "" );
                    item.SubjectWebpage = GetRowColumn( dr, "SubjectWebpage", "" );
                    item.CredentialRegistryId = GetRowPossibleColumn( dr, "CredentialRegistryId", "" );

                    item.ImageUrl = GetRowColumn( dr, "ImageUrl", "" );
                    if ( GetRowColumn( dr, "CredentialCount", 0 ) > 0 )
                        item.IsACredentialingOrg = true;
                    item.ISQAOrganization = GetRowColumn( dr, "IsAQAOrganization", false );

                    //item.MainPhoneNumber = PhoneNumber.DisplayPhone( GetRowColumn( dr, "MainPhoneNumber", "" ) );

                    //all addressess
                    int addressess = GetRowPossibleColumn( dr, "AvailableAddresses", 0 );
                    if ( addressess > 0 )
                    {
                        item.Addresses = Entity_AddressManager.GetAll( item.RowId );
                        //just in case (short term
                        //if ( item.Addresses.Count > 0 )
                        //	item.Address = item.Addresses[ 0 ];
                    }

                    //Edit - Added to fill out gray boxes in results - NA 5/12/2017
                    item.AgentType = EntityPropertyManager.FillEnumeration( item.RowId, CodesManager.PROPERTY_CATEGORY_ORGANIZATION_TYPE );
                    item.OrganizationSectorType = EntityPropertyManager.FillEnumeration( item.RowId, CodesManager.PROPERTY_CATEGORY_ORGANIZATION_SECTORTYPE );
                    //End Edit
                    //do we need service type??
                    item.ServiceType = EntityPropertyManager.FillEnumeration( item.RowId, CodesManager.PROPERTY_CATEGORY_ORG_SERVICE );

                    item.NaicsResults = Fill_CodeItemResults( dr, "NaicsList", CodesManager.PROPERTY_CATEGORY_NAICS, false, false );
                    //item.IndustryOtherResults = Fill_CodeItemResults( dr, "OtherIndustriesList", CodesManager.PROPERTY_CATEGORY_NAICS, false, false, false );

                    item.OwnedByResults = Fill_CodeItemResults( dr, "OwnedByList", CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false );

                    item.OfferedByResults = Fill_CodeItemResults( dr, "OfferedByList", CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false );

                    item.AsmtsOwnedByResults = Fill_CodeItemResults( dr, "AsmtsOwnedByList", CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false );

                    item.LoppsOwnedByResults = Fill_CodeItemResults( dr, "LoppsOwnedByList", CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false );

                    item.AccreditedByResults = Fill_CodeItemResults( dr, "AccreditedByList", CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false );

                    item.ApprovedByResults = Fill_CodeItemResults( dr, "ApprovedByList", CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false );
                
                    item.RecognizedByResults = Fill_CodeItemResults( dr, "RecognizedByList", CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false );

                    item.RegulatedByResults = Fill_CodeItemResults( dr, "RegulatedByList", CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false );

                    list.Add( item );
                }


                return list;

            }
        }
        //public static List<OrganizationIndex> GetAllForElastic( string filter = "" )
        //{
        //    string connectionString = DBConnectionRO();
        //    OrganizationIndex item = new OrganizationIndex();
        //    List<OrganizationIndex> list = new List<OrganizationIndex>();
        //    var result = new DataTable();
        //    using ( SqlConnection c = new SqlConnection( connectionString ) )
        //    {
        //        c.Open();

        //        using ( SqlCommand command = new SqlCommand( "[Organization.ElasticSearch]", c ) )
        //        {
        //            command.CommandType = CommandType.StoredProcedure;
        //            command.Parameters.Add( new SqlParameter( "@Filter", filter ) );
        //            command.Parameters.Add( new SqlParameter( "@SortOrder", "" ) );
        //            command.Parameters.Add( new SqlParameter( "@StartPageIndex", "0" ) );
        //            command.Parameters.Add( new SqlParameter( "@PageSize", "0" ) );
        //            //command.Parameters.Add( new SqlParameter( "@CurrentUserId", userId ) );

        //            SqlParameter totalRows = new SqlParameter( "@TotalRows", "0" );
        //            totalRows.Direction = ParameterDirection.Output;
        //            command.Parameters.Add( totalRows );

        //            try
        //            {
        //                using ( SqlDataAdapter adapter = new SqlDataAdapter() )
        //                {
        //                    adapter.SelectCommand = command;
        //                    adapter.Fill( result );
        //                }
        //                string rows = command.Parameters[command.Parameters.Count - 1].Value.ToString();
        //            }
        //            catch ( Exception ex )
        //            {
        //                item = new OrganizationIndex();
        //                item.Name = "EXCEPTION ENCOUNTERED";
        //                item.Description = ex.Message;
        //                list.Add( item );

        //                return list;
        //            }
        //        }

        //        foreach ( DataRow dr in result.Rows )
        //        {
        //            item = new OrganizationIndex();
        //            item.Id = GetRowColumn( dr, "Id", 0 );
        //            item.EntityTypeId = 2;
        //            item.Name = GetRowColumn( dr, "Name", "missing" );
        //            item.FriendlyName = FormatFriendlyTitle( item.Name );

        //            item.Description = GetRowColumn( dr, "Description", "" );
        //            string rowId = GetRowColumn( dr, "RowId" );
        //            item.RowId = new Guid( rowId );
        //            item.CTID = GetRowPossibleColumn( dr, "CTID", "" );
        //            item.SubjectWebpage = GetRowColumn( dr, "SubjectWebpage", "" );

        //            string date = GetRowColumn( dr, "Created", "" );
        //            if ( IsValidDate( date ) )
        //                item.Created = DateTime.Parse( date );
        //            date = GetRowColumn( dr, "LastUpdated", "" );
        //            if ( IsValidDate( date ) )
        //                item.LastUpdated = DateTime.Parse( date );


        //            item.CredentialRegistryId = GetRowPossibleColumn( dr, "CredentialRegistryId", "" );
        //            item.ImageURL = GetRowColumn( dr, "ImageUrl", "" );
        //            if ( GetRowColumn( dr, "CredentialCount", 0 ) > 0 )
        //                item.IsACredentialingOrg = true;
        //            item.ISQAOrganization = GetRowColumn( dr, "IsAQAOrganization", false );

        //            //all addressess
        //            item.Addresses = GetRowPossibleColumn( dr, "AvailableAddresses", 0 );
        //            if ( item.Addresses > 0 )
        //            {
        //                //will handle differently
        //                #region Addresses
        //                var addresses = dr["Addresses"].ToString();
        //                if ( !string.IsNullOrWhiteSpace( addresses ) )
        //                {
        //                    var xDoc = new XDocument();
        //                    xDoc = XDocument.Parse( addresses );
        //                    item.Addresses = xDoc.Root.Elements().Count();

        //                    foreach ( var child in xDoc.Root.Elements() )
        //                        item.AddressLocations.Add( new AddressLocation
        //                        {
        //                            Lat = double.Parse( ( string )child.Attribute( "Latitude" ) ),
        //                            Lon = double.Parse( ( string )child.Attribute( "Longitude" ) )
        //                        } );
        //                }
        //                #endregion
        //            }
        //            //item.AgentType = EntityPropertyManager.FillEnumeration(item.RowId, CodesManager.PROPERTY_CATEGORY_ORGANIZATION_TYPE);
        //            //item.OrganizationSectorType = EntityPropertyManager.FillEnumeration(item.RowId, CodesManager.PROPERTY_CATEGORY_ORGANIZATION_SECTORTYPE);

        //            //
        //            //item.NaicsResults = dr[ "NaicsList" ].ToString();
        //            //item.NaicsResults = Fill_CodeItemResults( dr, "NaicsList", CodesManager.PROPERTY_CATEGORY_NAICS, false, false );


        //            list.Add( item );
        //        }


        //        return list;

        //    }
        //}
        #endregion

        #region helpers
        public static void MapFromDB_ForSummary( DBEntity from, ThisEntity output )
        {
            if ( output == null )
                output = new ThisEntity();
            output.Id = from.Id;
            output.RowId = from.RowId;
            output.EntityStateId = ( int )( from.EntityStateId ?? 1 );

            output.Name = from.Name;
            output.Description = from.Description;
            output.SubjectWebpage = from.SubjectWebpage;
            if ( from.ImageURL != null && from.ImageURL.Trim().Length > 0 )
                output.ImageUrl = from.ImageURL;
            else
                output.ImageUrl = null;
            output.CredentialRegistryId = from.CredentialRegistryId;
            output.CTID = from.CTID;

            if ( IsValidDate( from.Created ) )
                output.Created = ( DateTime )from.Created;

            if ( IsValidDate( from.LastUpdated ) )
                output.LastUpdated = ( DateTime )from.LastUpdated;

            output.ISQAOrganization = from.ISQAOrganization == null ? false : ( bool )from.ISQAOrganization;
        }

        public static void MapFromDB_ForDetail( DBEntity from, Organization to,
                    bool includingProperties,
                    bool includeCredentials,
                    bool includingRoles
            )
        {
            MapFromDB_Common( from, to,
                includingProperties,
                includeCredentials,
                includingRoles,
                true //includingQAWhereUsed
                );
            //17-07-01 currently no difference, moved VerificationStatus here to claify as previously QA only
            //TODO***to.VerificationStatus = Organization_VerificationStatusManager.GetAll( to.Id );
            if ( to.ISQAOrganization )
            {
                //ToMap_QA( from, to, false );
            }

        }

        public static void MapFromDB_Common( DBEntity from, ThisEntity to,
                    bool includingProperties,
                    bool includeCredentials,
                    bool includingRoles,
                    bool includingQAWhereUsed )

        {
            if ( to == null )
                to = new ThisEntity();
            to.Id = from.Id;
            to.RowId = from.RowId;

            to.Name = from.Name;
            to.Description = from.Description;
            to.SubjectWebpage = from.SubjectWebpage;
            to.EntityStateId = from.EntityStateId ?? 1;

            if ( from.ImageURL != null && from.ImageURL.Trim().Length > 0 )
                to.ImageUrl = from.ImageURL;
            else
                to.ImageUrl = null;
            to.CredentialRegistryId = from.CredentialRegistryId;
            to.CTID = from.CTID;
            //TODO
            //to.AlternativeIdentifier = from.AlternativeIdentifier;
            to.AlternativeIdentifierList = Entity_IdentifierValueManager.GetAll( to.RowId, Entity_IdentifierValueManager.ORGANIZATION_AlternativeIdentifier );
            if ( IsValidDate( from.Created ) )
                to.Created = ( DateTime )from.Created;

            if ( IsValidDate( from.LastUpdated ) )
                to.LastUpdated = ( DateTime )from.LastUpdated;

            //need to use the service types as well. See assignment for to.AgentType below
            to.ISQAOrganization = from.ISQAOrganization == null ? false : ( bool )from.ISQAOrganization;

            //to.IsThirdPartyOrganization = from.IsThirdPartyOrganization != null ? ( bool ) from.IsThirdPartyOrganization : false;

            to.AgentType = EntityPropertyManager.FillEnumeration( to.RowId, CodesManager.PROPERTY_CATEGORY_ORGANIZATION_TYPE );
            if ( to.AgentType.HasItems() )
            {
                EnumeratedItem item = to.AgentType.Items.FirstOrDefault( s => s.SchemaName == "orgType:QualityAssurance" );
                if ( item != null && item.Id > 0 )
                    to.ISQAOrganization = true;
            }
            //=========================================================
            to.AgentPurpose = from.AgentPurpose;
            to.AgentPurposeDescription = from.AgentPurposeDescription;
            to.MissionAndGoalsStatement = from.MissionAndGoalsStatement;
            to.MissionAndGoalsStatementDescription = from.MissionAndGoalsStatementDescription;


            to.AvailabilityListing = from.AvailabilityListing;

            //map, although not currently used in interface
            to.FoundingDate = from.FoundingDate;

            to.Keyword = Entity_ReferenceManager.GetAll( to.RowId, CodesManager.PROPERTY_CATEGORY_KEYWORD );
            //TODO: remove this one, or not
            to.AlternateNames = Entity_ReferenceManager.GetAll( to.RowId, CodesManager.PROPERTY_CATEGORY_ALTERNATE_NAME );
            to.AlternateName = Entity_ReferenceManager.GetAllToList( to.RowId, CodesManager.PROPERTY_CATEGORY_ALTERNATE_NAME );
            //properties
            if ( includingProperties )
            {
                List<ContactPoint> orphans = new List<ContactPoint>();
                to.Addresses = Entity_AddressManager.GetAll( to.RowId, ref orphans );
                LoggingHelper.DoTrace( 7, thisClassName + string.Format( ".MapFromDB. OrgId: {0}, Addresses: {1}, orphanContacts: {2}", to.Id, to.Addresses.Count(), orphans.Count ) );
                //these will be mostly (all) under address
                //really should have display to show contact points per address
                //then how to handle CPs without address!
                //any contacts imported with an empty address, would have been added to the org
                to.ContactPoint = Entity_ContactPointManager.GetAll( to.RowId );
                LoggingHelper.DoTrace( 7, thisClassName + string.Format( ".MapFromDB. OrgId: {0}, ContactPoint: {1}", to.Id, to.ContactPoint.Count() ) );
                if ( to.Addresses != null && to.Addresses.Count > 0 )
                {

                }
                if ( orphans.Count > 0)
                {
                    to.ContactPoint.AddRange( orphans );
                }
                //detail page expects social media in contact points
                if ( to.ContactPoint == null )
                    to.ContactPoint = new List<ContactPoint>();
                to.SocialMediaPages = Entity_ReferenceManager.GetAll( to.RowId, CodesManager.PROPERTY_CATEGORY_ORGANIZATION_SOCIAL_MEDIA );
                if ( to.SocialMediaPages != null && to.SocialMediaPages.Count > 0 )
                {
                    LoggingHelper.DoTrace( 7, thisClassName + string.Format( ".MapFromDB. OrgId: {0}, SocialMediaPages: {1}", to.Id, to.SocialMediaPages.Count() ) );
                    ContactPoint cp = new ContactPoint();
                    cp.SocialMedia.AddRange( to.SocialMediaPages );
                    to.ContactPoint.Add( cp );
                }

                //this shouldn't be used anymore
                //to.PhoneNumbers = Entity_ReferenceManager.GetAll( to.RowId, CodesManager.PROPERTY_CATEGORY_PHONE_TYPE );
                //LoggingHelper.DoTrace( 7, thisClassName + string.Format( ".MapFromDB. OrgId: {0}, PhoneNumbers: {1}", to.Id, to.PhoneNumbers.Count() ) );
                to.Emails = Entity_ReferenceManager.GetAll( to.RowId, CodesManager.PROPERTY_CATEGORY_EMAIL_TYPE );
                LoggingHelper.DoTrace( 7, thisClassName + string.Format( ".MapFromDB. OrgId: {0}, Emails: {1}", to.Id, to.Emails.Count() ) );
                //not ideal?
                if ( to.Emails != null && to.Emails.Count > 0 )
                {
                    ContactPoint cp = new ContactPoint();
                    cp.Name = "Email";
                    cp.Email.AddRange( to.Emails );
                    to.ContactPoint.Add( cp );
                }
                //using Entity.Property in workIT, rather than Organization.Service
                //OrganizationServiceManager.FillOrganizationService( from, to );
                to.ServiceType = EntityPropertyManager.FillEnumeration( to.RowId, CodesManager.PROPERTY_CATEGORY_ORG_SERVICE );

                //sector type? - as an enumeration, will be stored in properties
                to.OrganizationSectorType = EntityPropertyManager.FillEnumeration( to.RowId, CodesManager.PROPERTY_CATEGORY_ORGANIZATION_SECTORTYPE );
                to.Jurisdiction = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( to.RowId );

                to.IdentificationCodes = Entity_ReferenceManager.GetAll( to.RowId, CodesManager.PROPERTY_CATEGORY_ORGANIZATION_IDENTIFIERS );
                

                //to.Industry = Entity_FrameworkItemManager.FillEnumeration( to.RowId, CodesManager.PROPERTY_CATEGORY_NAICS );
                to.Industry = Reference_FrameworksManager.FillEnumeration( to.RowId, CodesManager.PROPERTY_CATEGORY_NAICS );
                //to.OtherIndustries = Entity_ReferenceManager.GetAll( to.RowId, CodesManager.PROPERTY_CATEGORY_NAICS );
                //}
            }

            //credentialing?
            if ( includeCredentials )
            {
                to.CreatedCredentials = Entity_AgentRelationshipManager.Credentials_ForOwningOfferingOrg( to.RowId );
                

                //this was preferred as sometimes we lost the relationship. However, now that we need both owns, and offers problably need to go back to the latter
                //to.CreatedCredentials = CredentialManager.GetAllForOwningOrg( to.RowId );
                if ( to.CreatedCredentials != null && to.CreatedCredentials.Count > 0 )
                    to.IsACredentialingOrg = true;
                to.OwnedAssessments = AssessmentManager.GetAllForOwningOrg( to.RowId );
                to.OwnedLearningOpportunities = LearningOpportunityManager.GetAllForOwningOrg( to.RowId );
            }
            else
            {
                //need to distinguish QA from non-QA credentials
                //if ( CountCredentials( from ) > 0 )
                //	to.IsACredentialingOrg = true;
            }

            if ( includingRoles )
            {
                GetOrgRoles( from, to );
            }

            if ( includingQAWhereUsed )
            {
                //to.QualityAssuranceActor = Entity_QualityAssuranceActionManager.QualityAssuranceActionProfile_GetAllForAgent( to.RowId );
            }
            if ( includingProperties )
            {
                to.HasConditionManifest = ConditionManifestManager.GetAll( to.Id, false );
                to.HasCostManifest = CostManifestManager.GetAll( to.Id, false );
            }

            MapProcessProfiles( from, to );

            //need to distiguish between edit, list, and detail
            to.VerificationServiceProfiles = Entity_VerificationProfileManager.GetAll( to.RowId );


        }

        private static void GetOrgRoles( DBEntity from, ThisEntity to )
        {

            //the parent is the entity, and the 
            //to.OrganizationRole_Recipient = Entity_AgentRelationshipManager.AgentEntityRole_GetAll_ToEnumeration( to.RowId, true );
            to.OrganizationRole_Recipient = Entity_AssertionManager.GetAllCombinedForTarget( 2, to.Id );

            //also want the inverses - where this org was providing the QA for asmts, etc. 
            //18-10-08 this is used to display QA performed. 
            //to.OrganizationRole_Actor = Entity_AgentRelationshipManager.GetAll_QATargets_ForAgent( to.RowId );

            //Use a combined view
            to.OrganizationRole_Actor = Entity_AssertionManager.GetAllCombinedForOrganization( to.RowId );
            

            //We need to merge OrganizationRole_Actor with OrganizationAssertions
            //to.OrganizationAssertions = Entity_AssertionManager.GetAll( to.RowId );
            //foreach (var item in to.OrganizationAssertions)
            //{
            //    if (to.OrganizationRole_Actor.Exists(s => s.TargetCredential.Id == item.TargetCredential.Id ))
            //    {
            //        //check roles
            //    }
            //}

            to.JurisdictionAssertions = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( to.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_OFFERREDIN );

            //to.QualityAssuranceAction = Entity_QualityAssuranceActionManager.QualityAssuranceActionProfile_GetAll( to.RowId );

            //dept and subsiduaries ????
            Entity_AgentRelationshipManager.AgentRole_FillAllSubOrganizations( to, 0 );

            //parent org 
            Entity_AgentRelationshipManager.AgentRole_GetParentOrganization( to );

        }
        private static void MapProcessProfiles( DBEntity from, ThisEntity to )
        {
            //get all and then split
            List<ProcessProfile> list = Entity_ProcessProfileManager.GetAll( to.RowId );
            foreach ( ProcessProfile item in list )
            {
                //some default for 1??
                if ( item.ProcessTypeId == Entity_ProcessProfileManager.ADMIN_PROCESS_TYPE )
                    to.AdministrationProcess.Add( item );
                else if ( item.ProcessTypeId == Entity_ProcessProfileManager.DEV_PROCESS_TYPE )
                    to.DevelopmentProcess.Add( item );
                else if ( item.ProcessTypeId == Entity_ProcessProfileManager.MTCE_PROCESS_TYPE )
                    to.MaintenanceProcess.Add( item );

                else if ( item.ProcessTypeId == Entity_ProcessProfileManager.REVIEW_PROCESS_TYPE )
                    to.ReviewProcess.Add( item );
                else if ( item.ProcessTypeId == Entity_ProcessProfileManager.APPEAL_PROCESS_TYPE )
                    to.AppealProcess.Add( item );
                else if ( item.ProcessTypeId == Entity_ProcessProfileManager.COMPLAINT_PROCESS_TYPE )
                    to.ComplaintProcess.Add( item );
                //else if ( item.ProcessTypeId == Entity_ProcessProfileManager.CRITERIA_PROCESS_TYPE )
                //	to.CriteriaProcess.Add( item );
                else if ( item.ProcessTypeId == Entity_ProcessProfileManager.REVOKE_PROCESS_TYPE )
                    to.RevocationProcess.Add( item );
                else
                {
                    //produce warning where not mapped
                    to.ReviewProcess.Add( item );
                    LoggingHelper.LogError( string.Format( "OrganizationManager.MapProcessProfiles Unexpected ProcessProfile. OrgId: {0}, Type: {1} ", from.Id, item.ProcessTypeId ), true );
                }


            }
        }

        public static void MapToDB( ThisEntity from, DBEntity to )
        {
            MapToDB_Base( from, to );

        }
        //public static void MapToDB( QAOrganization from, DBEntity to )
        //{
        //	MapToDB_Base( from, to );
        //	to.ISQAOrganization = true;

        //}
        public static void MapToDB_Base( ThisEntity from, DBEntity to )
        {

            //want to ensure fields from create are not wiped
            if ( to.Id == 0 )
            {
                //need to ensure ctid and registry id are not overridden, if can update outside of the import
                to.CTID = from.CTID;
            }
            if ( !string.IsNullOrWhiteSpace( from.CredentialRegistryId ) )
                to.CredentialRegistryId = from.CredentialRegistryId;

            to.Id = from.Id;
            to.Name = GetData( from.Name );
            to.Description = GetData( from.Description );
            to.AgentPurposeDescription = GetData( from.AgentPurposeDescription );
            to.AgentPurpose = GetUrlData( from.AgentPurpose, null );

            to.SubjectWebpage = GetUrlData( from.SubjectWebpage, null );
            if ( from.AgentDomainType == "ceterms:QACredentialOrganization" )
                to.ISQAOrganization = true;
            else if ( from.AgentTypeId == 2 )
                to.ISQAOrganization = true;

            to.AvailabilityListing = GetUrlData( from.AvailabilityListing, null );
            to.ImageURL = GetUrlData( from.ImageUrl, null );


            //FoundingDate is now a string
            //interface must handle? Or do we have to fix here?
            //depends if just text is passed or separates
            //already validated
            if ( !string.IsNullOrWhiteSpace( from.FoundingDate ) )
                to.FoundingDate = from.FoundingDate;
            else
                to.FoundingDate = null;

            to.MissionAndGoalsStatement = GetUrlData( from.MissionAndGoalsStatement, null );
            to.MissionAndGoalsStatementDescription = GetData( from.MissionAndGoalsStatementDescription );

            //to.ServiceTypeOther = from.ServiceTypeOther;

        }

        public static List<OrganizationSummary> MapFromElasticResults( List<OrganizationIndex> input )
        {
            var list = new List<OrganizationSummary>();
            var output = new OrganizationSummary();
            foreach ( var from in input )
            {
                output = new OrganizationSummary();
                output.Id = from.Id;
                output.RowId = from.RowId;
                //output.EntityStateId = ( int ) ( from.EntityStateId ?? 1 );

                output.Name = from.Name;
                output.FriendlyName = from.FriendlyName;
                output.Description = from.Description;
                output.SubjectWebpage = from.SubjectWebpage;
                if ( from.ImageURL != null && from.ImageURL.Trim().Length > 0 )
                    output.ImageUrl = from.ImageURL;
                else
                    output.ImageUrl = null;

                output.CTID = from.CTID;

                //TODO
                output.NaicsResults = new CodeItemResult();
                output.AgentType = new Enumeration();
                output.OrganizationSectorType = new Enumeration();
                output.AccreditedByResults = new CodeItemResult();
                output.ApprovedByResults = new CodeItemResult();
                output.RecognizedByResults = new CodeItemResult();
                output.RegulatedByResults = new CodeItemResult();
                output.AsmtsOwnedByResults = new CodeItemResult();
                output.LoppsOwnedByResults = new CodeItemResult();
                output.OfferedByResults = new CodeItemResult();
                output.OwnedByResults = new CodeItemResult();

                if ( IsValidDate( from.Created ) )
                    output.Created = ( DateTime )from.Created;

                if ( IsValidDate( from.LastUpdated ) )
                    output.LastUpdated = ( DateTime )from.LastUpdated;

                list.Add( output );
            }

            return list;
        }
        #endregion

    }
}

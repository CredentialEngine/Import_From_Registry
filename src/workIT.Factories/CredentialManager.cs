using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Data.Entity;

using workIT.Models;
using workIT.Models.Common;
using CM = workIT.Models.Common;
using workIT.Models.ProfileModels;
using EM = workIT.Data.Tables;
using workIT.Utilities;

using Views = workIT.Data.Views;

//using CondProfileMgr = workIT.Factories.Entity_ConditionProfileManager;
using ThisEntity = workIT.Models.Common.Credential;
using DBEntity = workIT.Data.Tables.Credential;
using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;
using workIT.Models.Elastic;


namespace workIT.Factories
{
    public class CredentialManager : BaseFactory
    {
        static string thisClassName = "Factories.CredentialManager";
        EntityManager entityMgr = new EntityManager();
        string statusMessage = "";
        //List<string> messages = new List<string>();
        #region Credential - presistance =======================

        /// <summary>
        /// Save a credential - only from import
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public bool Save( ThisEntity entity, ref SaveStatus status )
        {
            bool isValid = true;
            int count = 0;

            //NOTE - need to properly set entity.EntityStateId

            try
            {
                using ( var context = new EntityContext() )
                {
                    //note for import, may still do updates?
                    if ( ValidateProfile( entity, ref status ) == false )
                    {
                        return false;
                    }
                    //getting duplicates somehow
                    //second one seems less full featured, so could compare dates
                    if ( entity.Id > 0 )
                    {
                        DBEntity efEntity = context.Credentials
                                .SingleOrDefault( s => s.Id == entity.Id );
                        if ( efEntity != null && efEntity.Id > 0 )
                        {
                            //delete the entity and re-add
                            Entity e = new Entity()
                            {
                                EntityBaseId = efEntity.Id,
                                EntityTypeId = CodesManager.ENTITY_TYPE_CREDENTIAL,
                                EntityType = "Credential",
                                EntityUid = efEntity.RowId,
                                EntityBaseName = efEntity.Name
                            };
                            if ( entityMgr.ResetEntity( e, ref statusMessage ) )
                            {

                            }

                            //for updates, chances are some fields will not be in interface, don't map these (ie created stuff)
                            //**ensure rowId is passed down for use by profiles, etc
                            entity.RowId = efEntity.RowId;

                            MapToDB( entity, efEntity );
                            //assume and validate, that if we get here we have a full record
                            //not clear if we will want to update a base reference. 
                            //==> should happen automatically if full record matches a SWP?
                            //may be iffy
                            if ( ( efEntity.EntityStateId ?? 1 ) != 2 )
                                efEntity.EntityStateId = 3;

                            if ( HasStateChanged( context ) )
                            {
                                efEntity.LastUpdated = System.DateTime.Now;
                                count = context.SaveChanges();
                                //can be zero if no data changed
                                if ( count >= 0 )
                                {
                                    isValid = true;
                                }
                                else
                                {
                                    //?no info on error
                                    status.AddError( string.Format( "Error - the update was not successful for credential: {0}, Id: {1}. But no reason is present.", entity.Name, entity.Id ) );
                                    isValid = false;
                                    string message = string.Format( thisClassName + ". Save Failed", "Attempted to update a credential. The process appeared to not work, but was not an exception, so we have no message, or no clue. credential: {0}, Id: {1}", entity.Name, entity.Id );
                                    EmailManager.NotifyAdmin( thisClassName + ". Save Failed", message );
                                }
                            }

                            //continue with parts only if valid 
                            if ( isValid )
                            {
                                if ( !UpdateParts( entity, false, ref status ) )
                                    isValid = false;

                                SiteActivity sa = new SiteActivity()
                                {
                                    ActivityType = "Credential",
                                    Activity = "Import",
                                    Event = "Update",
                                    Comment = string.Format( "Credential was updated by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ),
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
                string message = HandleDBValidationError( dbex, thisClassName + ".Save() ", "Credential" );
                status.AddError( thisClassName + ".Save(). Error - the save was not successful. DbEntityValidationException. " + message );
            }
            catch ( Exception ex )
            {
                string message = FormatExceptions( ex );
                LoggingHelper.LogError( ex, thisClassName + string.Format( ".Save. id: {0}", entity.Id ) );
                status.AddError( thisClassName + ".Save(). Error - the save was not successful. " + message );
                isValid = false;
            }


            return isValid;
        }

        /// <summary>
        /// add a credential
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="statusMessage"></param>
        /// <returns></returns>
        private int Add( CM.Credential entity, ref SaveStatus status )
        {
            EM.Credential efEntity = new EM.Credential();
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
                    context.Credentials.Add( efEntity );

                    // submit the change to database
                    int count = context.SaveChanges();
                    if ( count > 0 )
                    {
                        entity.RowId = efEntity.RowId;
                        entity.Id = efEntity.Id;

                        //add log entry
                        SiteActivity sa = new SiteActivity()
                        {
                            ActivityType = "Credential",
                            Activity = "Import",
                            Event = "Add",
                            Comment = string.Format( "Full Credential was added by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ),
                            ActivityObjectId = entity.Id
                        };
                        new ActivityManager().SiteActivityAdd( sa );

                        UpdateParts( entity, true, ref status );

                        return efEntity.Id;
                    }
                    else
                    {
                        //?no info on error
                        status.AddError( "Error - the add was not successful. " );
                        string message = string.Format( "CredentialManager. Add Failed", "Attempted to add a credential. The process appeared to not work, but was not an exception, so we have no message, or no clue.Credential: {0}, createdById: {1}", entity.Name, entity.CreatedById );
                        //EmailManager.NotifyAdmin( "CredentialManager. Credential_Add Failed", message );
                    }
                }
                catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
                {
                    string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", "Credential" );
                    status.AddError( thisClassName + ".Add(). Error - the add was not successful. DbEntityValidationException. " + message );
                }
                catch ( Exception ex )
                {
                    LoggingHelper.LogError( ex, thisClassName + string.Format( ".Add(), Name: {0}", efEntity.Name ) );
                    status.AddError( FormatExceptions( ex ) );
                }
            }

            return efEntity.Id;
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
                        string.IsNullOrWhiteSpace( entity.Description ) ||
                        string.IsNullOrWhiteSpace( entity.SubjectWebpage )
                        ) )
                    {
                        status.AddError( thisClassName + ". AddBaseReference() The credential is incomplete" );
                        return 0;
                    }

                    //only add DB required properties
                    //NOTE - an entity will be created via trigger
                    efEntity.EntityStateId = 2;
                    efEntity.Name = entity.Name;
                    efEntity.Description = entity.Description;
                    efEntity.SubjectWebpage = entity.SubjectWebpage;
                    if ( IsValidGuid( entity.RowId ) )
                        efEntity.RowId = entity.RowId;
                    else
                        efEntity.RowId = Guid.NewGuid();

                    efEntity.Created = System.DateTime.Now;
                    efEntity.LastUpdated = System.DateTime.Now;

                    context.Credentials.Add( efEntity );
                    int count = context.SaveChanges();
                    if ( count > 0 )
                    {
                        SiteActivity sa = new SiteActivity()
                        {
                            ActivityType = "Credential",
                            Activity = "Import",
                            Event = "Add Base Reference",
                            Comment = string.Format( "Pending Credential was added by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ),
                            ActivityObjectId = entity.Id
                        };
                        new ActivityManager().SiteActivityAdd( sa );

                        entity.Id = efEntity.Id;
                        return efEntity.Id;
                    }

                    status.AddError( thisClassName + ". AddBaseReference() Error - the save was not successful, but no message provided. " );
                }
            }

            catch ( Exception ex )
            {
                string message = FormatExceptions( ex );
                LoggingHelper.LogError( ex, thisClassName + string.Format( ".AddBaseReference. Name:  {0}, SubjectWebpage: {1}", entity.Name, entity.SubjectWebpage ) );
                status.AddError( thisClassName + ". AddBaseReference()  Error - the save was not successful. " + message );

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
                        status = thisClassName + " - A valid GUID must be provided to create a pending entity";
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

                    context.Credentials.Add( efEntity );
                    int count = context.SaveChanges();
                    if ( count > 0 )
                    {
                        SiteActivity sa = new SiteActivity()
                        {
                            ActivityType = "Credential",
                            Activity = "Import",
                            Event = "Add Pending Credential",
                            Comment = string.Format( "Pending Credential was added by the import. ctid: {0}, registryAtId: {1}", ctid, registryAtId ),
                            ActivityObjectId = efEntity.Id
                        };
                        new ActivityManager().SiteActivityAdd( sa );
                        return efEntity.Id;
                    }

                    status = thisClassName + " AddPendingRecord(). Error - the save was not successful, but no message provided. ";
                }
            }

            catch ( Exception ex )
            {
                string message = FormatExceptions( ex );
                LoggingHelper.LogError( ex, thisClassName + string.Format( ".AddPendingRecord. entityUid:  {0}, ctid: {1}", entityUid, ctid ) );
                status = thisClassName + "  AddPendingRecord(). Error - the save was not successful. " + message;

            }
            return 0;
        }


        /// <summary>
        /// Update a credential
        /// - base only, caller will handle parts?
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="statusMessage"></param>
        /// <returns></returns>
        //	private bool Update( CM.Credential entity, ref string statusMessage )
        //	{
        //		bool isValid = false;
        //		int count = 0;
        //		using ( var context = new EntityContext() )
        //		{
        //			try
        //			{
        //				//context.Configuration.LazyLoadingEnabled = false;

        //				EM.Credential efEntity = context.Credentials
        //							.SingleOrDefault( s => s.Id == entity.Id );

        //			if ( efEntity != null && efEntity.Id > 0 )
        //			{
        //				if ( ValidateProfile( entity, ref status ) == false )
        //				{
        //					statusMessage = string.Join( "<br/>", messages.ToArray() );
        //					return false;
        //				}
        //				//**ensure rowId is passed down for use by profiles, etc
        //				entity.RowId = efEntity.RowId;
        //				MapToDB( entity, efEntity );

        //				if (context.ChangeTracker.Entries().Any(e => e.State == EntityState.Added
        //                                             || e.State == EntityState.Modified
        //										  || e.State == EntityState.Deleted ) == true)
        //				{
        //					//note: testing - the latter may be true if the child has changed - but shouldn't as the mapping only updates the parent
        //					efEntity.LastUpdated = System.DateTime.Now;
        //					count = context.SaveChanges();
        //				}

        //				//can be zero if no data changed
        //				if ( count >= 0 )
        //				{
        //					//TODO - handle first time owner roles here????
        //					isValid = true;

        //					if ( !UpdateParts( entity, false, ref statusMessage ) )
        //						isValid = false;
        //				}
        //				else
        //				{
        //					//?no info on error
        //					statusMessage = "Error - the update was not successful. ";
        //						string message = string.Format( "CredentialManager. Credential_Update Failed", "Attempted to update a credential. The process appeared to not work, but was not an exception, so we have no message, or no clue. Credential: {0}, Id: {1}, updatedById: {2}", entity.Name, entity.Id, entity.LastUpdatedById );
        //					EmailManager.NotifyAdmin( "CredentialManager. Credential_Update Failed", message );
        //				}
        //			}
        //			else
        //			{
        //				statusMessage = "Error - update failed, as record was not found.";
        //			}
        //		}
        //			catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
        //			{
        //				//LoggingHelper.LogError( dbex, thisClassName + string.Format( ".ContentAdd() DbEntityValidationException, Type:{0}", entity.TypeId ) );
        //				string message = thisClassName + string.Format( ".Credential_Update() DbEntityValidationException, Name: {0}", entity.Name );
        //				foreach ( var eve in dbex.EntityValidationErrors )
        //				{
        //					message += string.Format( "\rEntity of type \"{0}\" in state \"{1}\" has the following validation errors:",
        //						eve.Entry.Entity.GetType().Name, eve.Entry.State );
        //					foreach ( var ve in eve.ValidationErrors )
        //					{
        //						message += string.Format( "- Property: \"{0}\", Error: \"{1}\"",
        //							ve.PropertyName, ve.ErrorMessage );
        //					}

        //					LoggingHelper.LogError( message, true );
        //				}

        //				statusMessage = string.Join( ", ", dbex.EntityValidationErrors.SelectMany( m => m.ValidationErrors.Select( n => n.ErrorMessage ) ).ToList() );
        //			}
        //			catch ( Exception ex )
        //			{
        //				LoggingHelper.LogError( ex, thisClassName + string.Format( ".Credential_Update(), Name: {0}", entity.Name ) );
        //				statusMessage = ex.Message + ( ex.InnerException != null && ex.InnerException.InnerException != null ? " - " + ex.InnerException.InnerException.Message : "" );
        //			}
        //		}

        //		return isValid;
        //}

        public bool ValidateProfile( ThisEntity profile, ref SaveStatus status )
        {

            status.HasSectionErrors = false;
            if ( string.IsNullOrWhiteSpace( profile.Name ) )
            {
                status.AddError( "A credential name must be included" );
            }
            if ( string.IsNullOrWhiteSpace( profile.Description ) )
            {
                status.AddError( "A description must be included" );
            }
            if ( string.IsNullOrWhiteSpace( profile.CredentialTypeSchema ) )
            {
                status.AddError( "A Credential Type must be included" );
            }
            else
            {
                CodeItem ci = CodesManager.Codes_PropertyValue_GetBySchema( CodesManager.PROPERTY_CATEGORY_CREDENTIAL_TYPE, profile.CredentialTypeSchema );
                if ( ci == null || ci.Id < 1 )
                    status.AddError( string.Format( "A valid Credential Type must be included. Invalid: {0}", profile.CredentialTypeSchema ) );
                else
                    profile.CredentialTypeId = ci.Id;
            }

            //if ( string.IsNullOrWhiteSpace( profile.CTID ) )
            //{
            //	status.AddError( "A CTID name must be entered" );
            //}
            if ( !string.IsNullOrWhiteSpace( profile.DateEffective ) && !IsValidDate( profile.DateEffective ) )
            {
                status.AddWarning( "Effective date is invalid" );
            }

            if ( !IsUrlValid( profile.SubjectWebpage, ref commonStatusMessage ) )
            {
                status.AddWarning( "The Subject Webpage Url is invalid. " + commonStatusMessage );
            }

            if ( !IsUrlValid( profile.AvailabilityListing, ref commonStatusMessage ) )
            {
                status.AddWarning( "The 'Availability Listing' Url is invalid. " + commonStatusMessage );
            }
            if ( !IsUrlValid( profile.AvailableOnlineAt, ref commonStatusMessage ) )
            {
                status.AddWarning( "The 'Available Online At' URL format is invalid. " + commonStatusMessage );
                ;
            }
            if ( !IsUrlValid( profile.LatestVersion, ref commonStatusMessage ) )
            {
                status.AddWarning( "The 'Latest Version' URL format is invalid. " + commonStatusMessage );
            }
            if ( !IsUrlValid( profile.PreviousVersion, ref commonStatusMessage ) )
            {
                status.AddWarning( "The 'Replaces Version' URL format is invalid. " + commonStatusMessage );
            }
            if ( !IsUrlValid( profile.ImageUrl, ref commonStatusMessage ) )
            {
                status.AddWarning( "The Image Url is invalid. " + commonStatusMessage );
            }

            return !status.HasSectionErrors;
        }


        public bool UpdateParts( ThisEntity entity, bool isAdd, ref SaveStatus status )
        {
            bool isAllValid = true;
            statusMessage = "";

            Entity relatedEntity = EntityManager.GetEntity( entity.RowId );
            if ( relatedEntity == null || relatedEntity.Id == 0 )
            {
                status.AddError( "Error - the related Entity was not found." );
                return false;
            }

            //OrganizationRoleManager orgMgr = new OrganizationRoleManager();
            if ( AddProperties( entity, ref status ) == false )
            {
                isAllValid = false;
            }

            if ( UpdateReferences( entity, relatedEntity, ref status ) == false )
            {
                isAllValid = false;
            }

            /*
			 * others:
			 * Y	subjects
			 * Y	keywords
			 * occupations
			 * industries
			 * List of strings
			 * AvailableOnlineAt
			 * CodedNotation
			 * Y	DegreeConcentration
			 * Y	DegreeMajor
			 * Y	DegreeMinor
			 * 
			 * Profiles:
			 * EstimatedCost 
			 * Y	ConditonProfiles	
			 * Renewal
			 * ConnectionProfiles (isPreparationFor, etc)
			 * Y	ProcessProfiles	
			 * Y	FinancialAssistance	
			 * Y	CommonConditions?
			 * y	CommonCosts?
			 * y	Revocation
			 * 
			 * y	Jurisdiction
			 * 
			 * Assertions BYs
			 * Assertions (Jurisdiction) INs
			 */


            //not used by import

            //if ( isAdd || ( entity.OwnerRoles != null && entity.OwnerRoles.Items.Count > 0 ) )
            //{
            //    if ( entity.OwnerRoles == null || entity.OwnerRoles.Items.Count == 0 )
            //    {
            //        //status.AddError( "Invalid request, please select one or more roles for the owing agent." );
            //        //isAllValid = false;
            //    }
            //    //the owner role must be selected
            //    else if ( entity.OwnerRoles.GetFirstItemId() != Entity_AgentRelationshipManager.ROLE_TYPE_OWNER )
            //    {
            //        //status.AddError( "Invalid request. The role \"Owned By\" must be one of the roles selected." );
            //        //isAllValid = false;
            //    }
            //    else
            //    {
            //        OrganizationRoleProfile profile = new OrganizationRoleProfile();
            //        profile.ParentUid = entity.RowId;
            //        profile.ActingAgentUid = entity.OwningAgentUid;
            //        profile.AgentRole = entity.OwnerRoles;
            //        //now what
            //    }
            //}


            AddProfiles( entity, relatedEntity, ref status );

            UpdateAssertedBys( entity, ref status );

            UpdateAssertedIns( entity, ref status );

            return isAllValid;
        }
        public void AddProfiles( ThisEntity entity, Entity relatedEntity, ref SaveStatus status )
        {
            //DurationProfile
            DurationProfileManager dpm = new Factories.DurationProfileManager();
            dpm.SaveList( entity.EstimatedDuration, entity.RowId, ref status );

            //VersionIdentifier
            new Entity_IdentifierValueManager().SaveList( entity.VersionIdentifierList, entity.RowId, Entity_IdentifierValueManager.CREDENTIAL_VersionIdentifier, ref status );

            //CostProfile
            CostProfileManager cpm = new Factories.CostProfileManager();
            cpm.SaveList( entity.EstimatedCosts, entity.RowId, ref status );

            //ConditionProfile
            Entity_ConditionProfileManager emanager = new Entity_ConditionProfileManager();
            try
            {
                emanager.SaveList( entity.Requires, Entity_ConditionProfileManager.ConnectionProfileType_Requirement, entity.RowId, ref status );
                emanager.SaveList( entity.Recommends, Entity_ConditionProfileManager.ConnectionProfileType_Recommendation, entity.RowId, ref status );
                emanager.SaveList( entity.Renewal, Entity_ConditionProfileManager.ConnectionProfileType_Renewal, entity.RowId, ref status );
                emanager.SaveList( entity.Corequisite, Entity_ConditionProfileManager.ConnectionProfileType_Renewal, entity.RowId, ref status );

                //Connections
                emanager.SaveList( entity.AdvancedStandingFor, Entity_ConditionProfileManager.ConnectionProfileType_AdvancedStandingFor, entity.RowId, ref status, 2 );
                emanager.SaveList( entity.AdvancedStandingFrom, Entity_ConditionProfileManager.ConnectionProfileType_AdvancedStandingFrom, entity.RowId, ref status, 2 );
                emanager.SaveList( entity.IsPreparationFor, Entity_ConditionProfileManager.ConnectionProfileType_PreparationFor, entity.RowId, ref status, 2 );
                emanager.SaveList( entity.PreparationFrom, Entity_ConditionProfileManager.ConnectionProfileType_PreparationFrom, entity.RowId, ref status, 2 );
                emanager.SaveList( entity.IsRequiredFor, Entity_ConditionProfileManager.ConnectionProfileType_NextIsRequiredFor, entity.RowId, ref status, 2 );
                emanager.SaveList( entity.IsRecommendedFor, Entity_ConditionProfileManager.ConnectionProfileType_NextIsRecommendedFor, entity.RowId, ref status, 2 );
            }
            catch ( Exception ex )
            {
                string message = FormatExceptions( ex );
                LoggingHelper.LogError( ex, thisClassName + string.Format( ".AddProfiles() - ConditionProfiles. id: {0}", entity.Id ) );
                status.AddWarning( thisClassName + ".AddProfiles(). Exceptions encountered handling ConditionProfiles. " + message );
            }


            //ProcessProfile

            Entity_ProcessProfileManager ppm = new Factories.Entity_ProcessProfileManager();
            try
            {
                ppm.SaveList( entity.AdministrationProcess, Entity_ProcessProfileManager.ADMIN_PROCESS_TYPE, entity.RowId, ref status );
                ppm.SaveList( entity.AppealProcess, Entity_ProcessProfileManager.APPEAL_PROCESS_TYPE, entity.RowId, ref status );
                ppm.SaveList( entity.ComplaintProcess, Entity_ProcessProfileManager.COMPLAINT_PROCESS_TYPE, entity.RowId, ref status );
                ppm.SaveList( entity.DevelopmentProcess, Entity_ProcessProfileManager.DEV_PROCESS_TYPE, entity.RowId, ref status );
                ppm.SaveList( entity.MaintenanceProcess, Entity_ProcessProfileManager.MTCE_PROCESS_TYPE, entity.RowId, ref status );
                ppm.SaveList( entity.ReviewProcess, Entity_ProcessProfileManager.REVIEW_PROCESS_TYPE, entity.RowId, ref status );
                ppm.SaveList( entity.RevocationProcess, Entity_ProcessProfileManager.REVOKE_PROCESS_TYPE, entity.RowId, ref status );
            }
            catch ( Exception ex )
            {
                string message = FormatExceptions( ex );
                LoggingHelper.LogError( ex, thisClassName + string.Format( ".AddProfiles() - ProcessProfiles. id: {0}", entity.Id ) );
                status.AddWarning( thisClassName + ".AddProfiles(). Exceptions encountered handling ProcessProfiles. " + message );
            }

            Entity_CredentialManager ecm = new Entity_CredentialManager();
            //has parts
            if ( entity.HasPartIds != null && entity.HasPartIds.Count > 0 )
            {
                ecm.SaveList( entity.HasPartIds, relatedEntity.EntityUid, ref status );
            }
            //isPartOf - have to watch for duplicates here (where the other side added a hasPart
            if ( entity.IsPartOfIds != null && entity.IsPartOfIds.Count > 0 )
            {
                ecm.SaveIsPartOfList( entity.IsPartOfIds, entity.Id, ref status );
            }

            //Financial Alignment 
            Entity_FinancialAlignmentProfileManager fapm = new Factories.Entity_FinancialAlignmentProfileManager();
            fapm.SaveList( entity.FinancialAssistance, entity.RowId, ref status );

            //Revocation Profile
            Entity_RevocationProfileManager rpm = new Entity_RevocationProfileManager();
            rpm.SaveList( entity.Revocation, entity, ref status );

            //addresses
            new Entity_AddressManager().SaveList( entity.Addresses, entity.RowId, ref status );

            //JurisdictionProfile 
            Entity_JurisdictionProfileManager jpm = new Entity_JurisdictionProfileManager();
            jpm.SaveList( entity.Jurisdiction, entity.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_SCOPE, ref status );


            new Entity_CommonConditionManager().SaveList( entity.ConditionManifestIds, entity.RowId, ref status );
            new Entity_CommonCostManager().SaveList( entity.CostManifestIds, entity.RowId, ref status );

        }

        public bool AddProperties( ThisEntity entity, ref SaveStatus status )
        {
            bool isAllValid = true;

            //============================
            EntityPropertyManager mgr = new EntityPropertyManager();

            if ( mgr.AddProperties( entity.AudienceLevelType, entity.RowId, CodesManager.ENTITY_TYPE_CREDENTIAL, CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL, false, ref status ) == false )
                isAllValid = false;


            if ( mgr.AddProperties( entity.CredentialStatusType, entity.RowId, CodesManager.ENTITY_TYPE_CREDENTIAL, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_STATUS_TYPE, false, ref status ) == false )
                isAllValid = false;

            return isAllValid;
        }
        public bool UpdateReferences( ThisEntity entity, Entity relatedEntity, ref SaveStatus status )
        {
            bool isAllValid = true;
            Entity_ReferenceManager erm = new Entity_ReferenceManager();

            Entity_FrameworkItemManager efim = new Entity_FrameworkItemManager();
            Entity_ReferenceFrameworkManager erfm = new Entity_ReferenceFrameworkManager();
            //if ( efim.SaveList( relatedEntity.Id, CodesManager.PROPERTY_CATEGORY_SOC, entity.Occupations, ref status ) == false )
            //             isAllValid = false;
            //         if ( efim.SaveList( relatedEntity.Id, CodesManager.PROPERTY_CATEGORY_NAICS, entity.Industries, ref status ) == false )
            //             isAllValid = false;

            if ( erfm.SaveList( relatedEntity.Id, CodesManager.PROPERTY_CATEGORY_SOC, entity.Occupations, ref status ) == false )
                isAllValid = false;
            if ( erfm.SaveList( relatedEntity.Id, CodesManager.PROPERTY_CATEGORY_NAICS, entity.Industries, ref status ) == false )
                isAllValid = false;

            //TODO - handle Naics if provided separately
            if ( erfm.NaicsSaveList( relatedEntity.Id, CodesManager.PROPERTY_CATEGORY_NAICS, entity.Naics, ref status ) == false )
                isAllValid = false;


            if ( erm.Add( entity.Subject, entity.RowId, CodesManager.ENTITY_TYPE_CREDENTIAL, ref status, CodesManager.PROPERTY_CATEGORY_SUBJECT, false ) == false )
                isAllValid = false;

            if ( erm.Add( entity.Keyword, entity.RowId, CodesManager.ENTITY_TYPE_CREDENTIAL, ref status, CodesManager.PROPERTY_CATEGORY_KEYWORD, false ) == false )
                isAllValid = false;

            if ( erm.Add( entity.AlternateNames, entity.RowId, CodesManager.ENTITY_TYPE_CREDENTIAL, ref status, CodesManager.PROPERTY_CATEGORY_ALTERNATE_NAME, false ) == false )
                isAllValid = false;


            if ( erm.Add( entity.DegreeConcentration, entity.RowId, CodesManager.ENTITY_TYPE_CREDENTIAL, ref status, CodesManager.PROPERTY_CATEGORY_DEGREE_CONCENTRATION, false ) == false )
                isAllValid = false;

            if ( erm.Add( entity.DegreeMajor, entity.RowId, CodesManager.ENTITY_TYPE_CREDENTIAL, ref status, CodesManager.PROPERTY_CATEGORY_DEGREE_MAJOR, false ) == false )
                isAllValid = false;

            if ( erm.Add( entity.DegreeMinor, entity.RowId, CodesManager.ENTITY_TYPE_CREDENTIAL, ref status, CodesManager.PROPERTY_CATEGORY_DEGREE_MINOR, false ) == false )
                isAllValid = false;

            //for language, really want to convert from en to English (en)
            //erm.AddLanguages( entity.InLanguageCodeList, entity.RowId, CodesManager.ENTITY_TYPE_CREDENTIAL, ref status, CodesManager.PROPERTY_CATEGORY_LANGUAGE );
            erm.AddLanguage( entity.InLanguage, relatedEntity.Id, ref status, CodesManager.PROPERTY_CATEGORY_LANGUAGE );

            return isAllValid;
        }

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

            mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_AccreditedBy, entity.AccreditedBy, ref status );
            mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_ApprovedBy, entity.ApprovedBy, ref status );
            mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_OFFERED_BY, entity.OfferedBy, ref status );
            mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_OWNER, entity.OwnedBy, ref status );
            mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_RecognizedBy, entity.RecognizedBy, ref status );
            mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_RegulatedBy, entity.RegulatedBy, ref status );
            mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_RevokedBy, entity.RevokedBy, ref status );
            mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_RenewedBy, entity.RenewedBy, ref status );
            return isAllValid;
        } //


        public void UpdateAssertedIns( ThisEntity entity, ref SaveStatus status )
        {

            Entity_JurisdictionProfileManager mgr = new Entity_JurisdictionProfileManager();
            Entity parent = EntityManager.GetEntity( entity.RowId );
            if ( parent == null || parent.Id == 0 )
            {
                status.AddError( thisClassName + " - Error - the parent entity was not found." );
                return;
            }

            mgr.SaveAssertedInList( entity.RowId, Entity_AgentRelationshipManager.ROLE_TYPE_AccreditedBy, entity.AccreditedIn, ref status );
            mgr.SaveAssertedInList( entity.RowId, Entity_AgentRelationshipManager.ROLE_TYPE_ApprovedBy, entity.ApprovedIn, ref status );
            mgr.SaveAssertedInList( entity.RowId, Entity_AgentRelationshipManager.ROLE_TYPE_OFFERED_BY, entity.OfferedIn, ref status );

            mgr.SaveAssertedInList( entity.RowId, Entity_AgentRelationshipManager.ROLE_TYPE_RecognizedBy, entity.RecognizedIn, ref status );
            mgr.SaveAssertedInList( entity.RowId, Entity_AgentRelationshipManager.ROLE_TYPE_RegulatedBy, entity.RegulatedIn, ref status );
            mgr.SaveAssertedInList( entity.RowId, Entity_AgentRelationshipManager.ROLE_TYPE_RevokedBy, entity.RevokedIn, ref status );
            mgr.SaveAssertedInList( entity.RowId, Entity_AgentRelationshipManager.ROLE_TYPE_RenewedBy, entity.RenewedIn, ref status );


        } //

        /// <summary>
        /// Delete a credential
        /// May be done with each new import?
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="statusMessage"></param>
        /// <returns></returns>
        public bool Delete( int id, int userId, ref string statusMessage )
        {
            bool isValid = false;
            if ( id == 0 )
            {
                statusMessage = "Error - missing an identifier for the Credential";
                return false;
            }
            using ( var context = new EntityContext() )
            {
                EM.Credential efEntity = context.Credentials
                            .SingleOrDefault( s => s.Id == id );

                if ( efEntity != null && efEntity.Id > 0 )
                {
                    statusMessage = string.Format( "Credential: {0}, Id:{1}", efEntity.Name, efEntity.Id );

                    //context.Credential.Remove( efEntity );
                    efEntity.LastUpdated = System.DateTime.Now;

                    int count = context.SaveChanges();
                    if ( count > 0 )
                    {
                        isValid = true;
                        //add pending request 
                        List<String> messages = new List<string>();
                        new SearchPendingReindexManager().AddDeleteRequest( CodesManager.ENTITY_TYPE_CREDENTIAL, efEntity.Id, ref messages );
                    }
                    else
                        statusMessage = "Error - delete failed, but no message was provided.";
                }
                else
                {
                    statusMessage = "Error - delete failed, as record was not found.";
                }
            }

            return isValid;
        }

        /// <summary>
        /// Delete by envelopeId
        /// </summary>
        /// <param name="envelopeId"></param>
        /// <param name="statusMessage"></param>
        /// <returns></returns>
        public bool Delete( string envelopeId, string ctid, ref string statusMessage )
        {
            bool isValid = true;
            if ( ( string.IsNullOrWhiteSpace( envelopeId ) || !IsValidGuid( envelopeId ) )
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
                    DBEntity efEntity = context.Credentials
                                .FirstOrDefault( s => s.CredentialRegistryId == envelopeId
                                || ( s.CTID == ctid )
                                );

                    if ( efEntity != null && efEntity.Id > 0 )
                    {
                        Guid rowId = efEntity.RowId;

                        //need to remove from Entity.
                        //-using before delete trigger - verify won't have RI issues
                        string msg = string.Format( " Credential. Id: {0}, Name: {1}, Ctid: {2}, EnvelopeId: {3}", efEntity.Id, efEntity.Name, efEntity.CTID, envelopeId );
                        //context.Credentials.Remove( efEntity );
                        efEntity.EntityStateId = 0;
                        efEntity.LastUpdated = System.DateTime.Now;

                        int count = context.SaveChanges();
                        if ( count > 0 )
                        {
                            new ActivityManager().SiteActivityAdd( new SiteActivity()
                            {
                                ActivityType = "Credential",
                                Activity = "Management",
                                Event = "Delete",
                                Comment = msg
                            } );
                            isValid = true;
                            //add pending request 
                            List<String> messages = new List<string>();
                            new SearchPendingReindexManager().AddDeleteRequest( CodesManager.ENTITY_TYPE_CREDENTIAL, efEntity.Id, ref messages );
                        }
                    }
                    else
                    {
                        statusMessage = thisClassName + ".Delete() Warning No action taken, as the record was not found.";
                        isValid = false;
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
        #endregion

        #region credential - retrieval ===================
        public static ThisEntity GetByCtid( string ctid )
        {
            ThisEntity entity = new ThisEntity();
            using ( var context = new EntityContext() )
            {
                DBEntity from = context.Credentials
                        .FirstOrDefault( s => s.CTID.ToLower() == ctid.ToLower() );

                if ( from != null && from.Id > 0 )
                {
                    entity.RowId = from.RowId;
                    entity.Id = from.Id;
                    entity.Name = from.Name;
                    entity.EntityStateId = ( int )( from.EntityStateId ?? 1 );
                    entity.Description = from.Description;
                    entity.SubjectWebpage = from.SubjectWebpage;

                    entity.ImageUrl = from.ImageUrl;
                    entity.CTID = from.CTID;
                    entity.CredentialRegistryId = from.CredentialRegistryId;
                }
            }

            return entity;
        }
        public static ThisEntity GetBySubjectWebpage( string swp )
        {
            ThisEntity entity = new ThisEntity();
            using ( var context = new EntityContext() )
            {
                context.Configuration.LazyLoadingEnabled = false;
                DBEntity from = context.Credentials
                        .FirstOrDefault( s => s.SubjectWebpage.ToLower() == swp.ToLower() );

                if ( from != null && from.Id > 0 )
                {
                    entity.RowId = from.RowId;
                    entity.Id = from.Id;
                    entity.Name = from.Name;
                    entity.EntityStateId = ( int )( from.EntityStateId ?? 1 );
                    entity.Description = from.Description;
                    entity.SubjectWebpage = from.SubjectWebpage;

                    entity.ImageUrl = from.ImageUrl;
                    entity.CTID = from.CTID;
                    entity.CredentialRegistryId = from.CredentialRegistryId;
                }
            }
            return entity;
        }
        public static CM.Credential GetForCompare( int id, CredentialRequest cr )
        {
            CM.Credential entity = new CM.Credential();
            if ( id < 1 )
                return entity;
            using ( var context = new EntityContext() )
            {
                //context.Configuration.LazyLoadingEnabled = false;
                EM.Credential item = context.Credentials
                            .SingleOrDefault( s => s.Id == id
                                );

                if ( item != null && item.Id > 0 )
                {
                    MapFromDB( item, entity, cr );
                }
            }

            return entity;
        }
        /// <summary>
        /// Get a credential
        /// ?should we allow get on a 'deleted' cred? Most people wouldn't remember the Id, although could be from a report
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static CM.Credential GetBasic( int id )
        {

            CM.Credential entity = new CM.Credential();
            CredentialRequest cr = new CredentialRequest();
            cr.IsForProfileLinks = true;
            if ( id < 1 )
                return entity;

            using ( var context = new EntityContext() )
            {
                //if ( cr.IsForProfileLinks )
                //	context.Configuration.LazyLoadingEnabled = false;
                EM.Credential item = context.Credentials
                            .SingleOrDefault( s => s.Id == id
                                );

                if ( item != null && item.Id > 0 )
                {
                    MapFromDB( item, entity, cr );

                    //Other parts
                }
            }

            return entity;
        }
        public static string GetCredentialType( int id )
        {

            CM.Credential entity = new CM.Credential();
            CredentialRequest cr = new CredentialRequest();
            cr.IsForProfileLinks = true;
            if ( id < 1 )
                return "";

            using ( var context = new EntityContext() )
            {
                if ( cr.IsForProfileLinks )
                    context.Configuration.LazyLoadingEnabled = false;
                EM.Credential item = context.Credentials
                            .SingleOrDefault( s => s.Id == id
                                );

                if ( item != null && item.Id > 0 )
                {
                    if ( item.CredentialTypeId > 0 )
                    {
                        CodeItem ct = CodesManager.Codes_PropertyValue_Get( ( int )item.CredentialTypeId );
                        if ( ct != null && ct.Id > 0 )
                        {
                            return ct.Title;
                        }
                    }

                }
            }

            return "";
        }

        /// <summary>
        /// Get a basic credentiias 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static CM.Credential GetBasic( Guid id )
        {

            CM.Credential entity = new CM.Credential();
            CredentialRequest cr = new CredentialRequest();
            cr.IsForProfileLinks = true;
            if ( !IsGuidValid( id ) )
                return entity;

            using ( var context = new EntityContext() )
            {
                //if ( cr.IsForProfileLinks )
                //context.Configuration.LazyLoadingEnabled = false;
                EM.Credential item = context.Credentials
                            .SingleOrDefault( s => s.RowId == id
                                );

                if ( item != null && item.Id > 0 && item.EntityStateId > 1 )
                {
                    MapFromDB( item, entity, cr );

                    //Other parts
                }
            }

            return entity;
        }

        public static CM.Credential GetBasicWithConditions( Guid rowId )
        {
            CM.Credential entity = new CM.Credential();
            CredentialRequest cr = new CredentialRequest();
            cr.IsForProfileLinks = true;

            using ( var context = new EntityContext() )
            {
                if ( cr.IsForProfileLinks ) //get minimum
                    context.Configuration.LazyLoadingEnabled = false;

                EM.Credential item = context.Credentials
                            .SingleOrDefault( s => s.RowId == rowId
                                );

                if ( item != null && item.Id > 0 )
                {
                    MapFromDB( item, entity, cr );

                }
            }

            return entity;
        }

        public static CM.Credential GetForDetail( int id, CredentialRequest cr )
        {
            CM.Credential entity = new CM.Credential();

            using ( var context = new EntityContext() )
            {

                //context.Configuration.LazyLoadingEnabled = false;
                EM.Credential item = context.Credentials
                            .SingleOrDefault( s => s.Id == id );
                try
                {
                    if ( item != null && item.Id > 0 )
                    {
                        //check for virtual deletes
                        if (item.EntityStateId == 0)
                            return entity;

                        MapFromDB( item, entity, cr );
                        //get summary for some totals
                        EM.Credential_SummaryCache cache = GetSummary( item.Id );
                        if ( cache != null && cache.BadgeClaimsCount > 0 )
                            entity.HasVerificationType_Badge = true;
                    }
                }
                catch ( Exception ex )
                {
                    LoggingHelper.LogError( ex, thisClassName + string.Format( ".GetForDetail(), Name: {0} ({1})", item.Name, item.Id ) );
                    entity.StatusMessage = FormatExceptions( ex );
                    entity.Id = 0;
                }
            }

            return entity;
        }

        /// <summary>
        /// Get summary view of a credential
        /// Useful for accessing counts
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static EM.Credential_SummaryCache GetSummary( int id )
        {

            EM.Credential_SummaryCache item = new EM.Credential_SummaryCache();
            using ( var context = new EntityContext() )
            {

                item = context.Credential_SummaryCache
                            .SingleOrDefault( s => s.CredentialId == id );

                if ( item != null && item.CredentialId > 0 )
                {

                }
            }

            return item;
        }
        public static List<Credential> GetPending()
        {
            List<ThisEntity> output = new List<ThisEntity>();
            ThisEntity entity = new ThisEntity();
            using ( var context = new EntityContext() )
            {

                List<EM.Credential> list = context.Credentials
                            .Where( s => s.EntityStateId == 1 )
                            .OrderBy( s => s.CTID )
                            .ToList();

                if ( list != null && list.Count > 0 )
                {
                    foreach ( var item in list )
                    {
                        //there is very little data in a pending record
                        entity = new ThisEntity();
                        entity.Id = item.Id;
                        entity.CTID = item.CTID;
                        entity.SubjectWebpage = item.SubjectWebpage;
                        entity.RowId = item.RowId;
                        entity.Created = ( DateTime )item.Created;
                        entity.LastUpdated = ( DateTime )item.LastUpdated;
                        output.Add( entity );
                    }
                }
            }

            return output;
        }

        public static List<string> Autocomplete( string pFilter, int pageNumber, int pageSize, ref int pTotalRows )
        {
            bool autocomplete = true;
            List<string> results = new List<string>();

            List<CM.CredentialSummary> list = Search( pFilter, "", pageNumber, pageSize, ref pTotalRows, autocomplete );
            bool appendingOrgNameToAutocomplete = UtilityManager.GetAppKeyValue( "appendingOrgNameToAutocomplete", false );
            string prevName = "";
            foreach ( CM.CredentialSummary item in list )
            {
                //note excluding duplicates may have an impact on selected max terms
                if ( string.IsNullOrWhiteSpace( item.OwnerOrganizationName )
                    || !appendingOrgNameToAutocomplete )
                {
                    if ( item.Name.ToLower() != prevName )
                        results.Add( item.Name );
                }
                else
                {
                    results.Add( item.Name + " ('" + item.OwnerOrganizationName + "')" );
                }

                prevName = item.Name.ToLower();
            }

            return results;
        }


        public static List<CM.CredentialSummary> Search( string pFilter, string pOrderBy, int pageNumber, int pageSize, ref int pTotalRows, bool autocomplete = false )
        {
            string connectionString = DBConnectionRO();
            CM.CredentialSummary item = new CM.CredentialSummary();
            List<CM.CredentialSummary> list = new List<CM.CredentialSummary>();
            var result = new DataTable();

            bool includingHasPartIsPartWithConnections = UtilityManager.GetAppKeyValue( "includeHasPartIsPartWithConnections", false );

            //int avgMinutes = 0;
            //string orgName = "";
            //int totals = 0;

            using ( SqlConnection c = new SqlConnection( connectionString ) )
            {
                c.Open();

                if ( string.IsNullOrEmpty( pFilter ) )
                {
                    pFilter = "";
                }

                using ( SqlCommand command = new SqlCommand( "[Credential.Search]", c ) )
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

                        item = new CM.CredentialSummary();
                        item.Name = "EXCEPTION ENCOUNTERED";
                        item.Description = ex.Message;
                        item.CredentialTypeSchema = "error";
                        list.Add( item );
                        return list;
                    }
                }

                //Used for costs. Only need to get these once. See below. - NA 5/12/2017
                var currencies = CodesManager.GetCurrencies();
                var costTypes = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_CREDENTIAL_ATTAINMENT_COST );

                foreach ( DataRow dr in result.Rows )
                {
                    //avgMinutes = 0;
                    item = new CM.CredentialSummary();
                    item.Id = GetRowColumn( dr, "Id", 0 );

                    //item.Name = GetRowColumn( dr, "Name", "missing" );
                    item.Name = dr["Name"].ToString();
                    item.FriendlyName = FormatFriendlyTitle( item.Name );
                    item.SubjectWebpage = dr["SubjectWebpage"].ToString();

                    //for autocomplete, only need name
                    if ( autocomplete )
                    {
                        list.Add( item );
                        continue;
                    }
                    //string rowId = GetRowColumn( dr, "RowId" );
                    //string rowId = GetRowColumn( dr, "EntityUid" );
                    string rowId = dr["EntityUid"].ToString();
                    //if ( IsGuidValid( rowId ) )
                    item.RowId = new Guid( rowId );

                    //item.Description = GetRowColumn( dr, "Description", "" );
                    item.Description = dr["Description"].ToString();

                    //item.CanEditRecord = GetRowColumn( dr, "CanEditRecord", false );

                    item.OwnerOrganizationId = GetRowPossibleColumn( dr, "OwningOrganizationId", 0 );
                    item.OwnerOrganizationName = GetRowPossibleColumn( dr, "owningOrganization" );
                    //watch if this is coming from the cache, change!
                    if ( item.OwnerOrganizationName.IndexOf( "Placeholder" ) > -1 )
                        item.OwnerOrganizationName = "";

                    item.CTID = GetRowColumn( dr, "CTID" );

                    item.CredentialRegistryId = dr["CredentialRegistryId"].ToString();

                    string date = GetRowColumn( dr, "EffectiveDate", "" );
                    if ( IsValidDate( date ) )
                        item.DateEffective = ( DateTime.Parse( date ).ToShortDateString() );
                    else
                        item.DateEffective = "";
                    date = GetRowColumn( dr, "Created", "" );
                    if ( IsValidDate( date ) )
                        item.Created = DateTime.Parse( date );
                    date = GetRowColumn( dr, "LastUpdated", "" );
                    if ( IsValidDate( date ) )
                        item.LastUpdated = DateTime.Parse( date );

                    item.Version = GetRowPossibleColumn( dr, "Version", "" );
                    item.LatestVersionUrl = GetRowPossibleColumn( dr, "LatestVersionUrl", "" );
                    item.PreviousVersion = GetRowPossibleColumn( dr, "PreviousVersion", "" );

                    item.CredentialType = GetRowPossibleColumn( dr, "CredentialType", "" );

                    item.CredentialTypeSchema = GetRowPossibleColumn( dr, "CredentialTypeSchema", "" );
                    // item.CredentialTypeSchema = dr["CredentialTypeSchema"].ToString();
                    item.TotalCost = GetRowPossibleColumn( dr, "TotalCost", 0m );
                    //AverageMinutes is a rough approach to sorting. If present, get the duration profiles
                    if ( GetRowPossibleColumn( dr, "AverageMinutes", 0 ) > 0 )
                    {
                        item.EstimatedTimeToEarn = DurationProfileManager.GetAll( item.RowId );
                    }

                    item.IsAQACredential = GetRowPossibleColumn( dr, "IsAQACredential", false );
                    item.HasQualityAssurance = GetRowPossibleColumn( dr, "HasQualityAssurance", false );

                    item.LearningOppsCompetenciesCount = GetRowPossibleColumn( dr, "LearningOppsCompetenciesCount", 0 );
                    item.AssessmentsCompetenciesCount = GetRowPossibleColumn( dr, "AssessmentsCompetenciesCount", 0 );

                    item.QARolesCount = GetRowPossibleColumn( dr, "QARolesCount", 0 );

                    item.HasPartCount = GetRowPossibleColumn( dr, "HasPartCount", 0 );
                    item.IsPartOfCount = GetRowPossibleColumn( dr, "IsPartOfCount", 0 );
                    item.RenewalCount = GetRowPossibleColumn( dr, "RenewalCount", 0 );

                    item.RequiresCount = GetRowPossibleColumn( dr, "RequiresCount", 0 );
                    item.RecommendsCount = GetRowPossibleColumn( dr, "RecommendsCount", 0 );
                    item.RequiredForCount = GetRowPossibleColumn( dr, "IsRequiredForCount", 0 );
                    item.IsRecommendedForCount = GetRowPossibleColumn( dr, "IsRecommendedForCount", 0 );
                    item.IsAdvancedStandingForCount = GetRowPossibleColumn( dr, "IsAdvancedStandingForCount", 0 );
                    item.AdvancedStandingFromCount = GetRowPossibleColumn( dr, "AdvancedStandingFromCount", 0 );
                    item.PreparationForCount = GetRowPossibleColumn( dr, "IsPreparationForCount", 0 );
                    item.PreparationFromCount = GetRowPossibleColumn( dr, "IsPreparationFromCount", 0 );

                    //NAICS CSV
                    //16-09-12 mp - changed to use pipe (|) rather than ; due to conflicts with actual embedded semicolons
                    item.NaicsResults = Fill_CodeItemResults( dr, "NaicsList", CodesManager.PROPERTY_CATEGORY_NAICS, false, false );
                    item.IndustryOtherResults = Fill_CodeItemResults( dr, "OtherIndustriesList", CodesManager.PROPERTY_CATEGORY_NAICS, false, false, false );

                    //OccupationsCSV
                    item.OccupationResults = Fill_CodeItemResults( dr, "OccupationsList", CodesManager.PROPERTY_CATEGORY_SOC, false, false );
                    item.OccupationOtherResults = Fill_CodeItemResults( dr, "OtherOccupationsList", CodesManager.PROPERTY_CATEGORY_SOC, false, false, false );
                    //education levels CSV
                    //16-09-12 mp - changed to use pipe (|) rather than ; due to conflicts with actual embedded semicolons
                    item.LevelsResults = Fill_CodeItemResults( dr, "LevelsList", CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL, false, false );

                    item.QARolesResults = Fill_CodeItemResults( dr, "QARolesList", CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, true );

                    item.Org_QARolesResults = Fill_CodeItemResults( dr, "QAOrgRolesList", CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, true );

                    item.AgentAndRoles = Fill_AgentRelationship( dr, "AgentAndRoles", CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false, true );

                    item.ConnectionsList = Fill_CodeItemResults( dr, "ConnectionsList", CodesManager.PROPERTY_CATEGORY_CONNECTION_PROFILE_TYPE, true, true );
                    if ( includingHasPartIsPartWithConnections )
                    {
                        //manually add other connections
                        if ( item.HasPartCount > 0 )
                        {
                            item.ConnectionsList.Results.Add( new CodeItem() { Id = 0, Title = "Includes", SchemaName = "hasPart", Totals = item.HasPartCount } );
                        }
                        if ( item.IsPartOfCount > 0 )
                        {
                            item.ConnectionsList.Results.Add( new CodeItem() { Id = 0, Title = "Included With", SchemaName = "isPartOf", Totals = item.IsPartOfCount } );
                        }
                    }

                    item.HasPartsList = Fill_CredentialConnectionsResult( dr, "HasPartsList", CodesManager.PROPERTY_CATEGORY_CONNECTION_PROFILE_TYPE );

                    item.IsPartOfList = Fill_CredentialConnectionsResult( dr, "IsPartOfList", CodesManager.PROPERTY_CATEGORY_CONNECTION_PROFILE_TYPE );

                    item.CredentialsList = Fill_CredentialConnectionsResult( dr, "CredentialsList", CodesManager.PROPERTY_CATEGORY_CONNECTION_PROFILE_TYPE );
                    //
                    item.ListTitle = item.Name + " (" + item.OwnerOrganizationName + ")";

                    string subjects = GetRowPossibleColumn( dr, "SubjectsList", "" );

                    if ( !string.IsNullOrWhiteSpace( subjects ) )
                    {
                        var codeGroup = subjects.Split( '|' );
                        foreach ( string codeSet in codeGroup )
                        {
                            var codes = codeSet.Split( '~' );
                            item.Subjects.Add( codes[0].Trim() );
                        }
                    }

                    //addressess
                    int addressess = GetRowPossibleColumn( dr, "AvailableAddresses", 0 );
                    if ( addressess > 0 )
                    {
                        item.Addresses = Entity_AddressManager.GetAll( item.RowId );
                    }

                    //Edit - Estimated Costs - needed for gray buttons in search results. Copied from MapFromDB method, then edited to move database calls outside of foreach loop. - NA 5/12/2017
                    //this only gets for the credential, need to alter to get all - should change to an ajax call
                    /*
					 * - cred
					 *		- conditions
					 *			- asmts
					 *				costs
					 *			- lopp
					 *				costs
					 */

                 //   item.NumberOfCostProfileItems = GetRowColumn( dr, "NumberOfCostProfileItems", 0 );

                    //item.EstimatedCost = CostProfileManager.GetAll( item.RowId, false );
                    //foreach ( var cost in item.EstimatedCost )
                    //{
                    //	cost.CurrencyTypes = currencies;
                    //	foreach ( var costItem in cost.Items )
                    //	{
                    //		costItem.CostType.Items.Add( costTypes.Items.FirstOrDefault( m => m.CodeId == costItem.CostTypeId ) );
                    //	}
                    //}
                    //End edit
                    //badgeClaimsCount
                    if ( GetRowPossibleColumn( dr, "badgeClaimsCount", 0 ) > 0 )
                    {
                        //Edit - Has Badge Verification Service.  Needed in search results. - NA 6/1/2017
                        item.HasVerificationType_Badge = true;  //Update this with appropriate source data
                    }
                    list.Add( item );
                }

                return list;

            }
        }
		
		private static bool ExtractOrg( string data, ref int orgId, ref string orgName )
        {
            var org = data.Split( ',' );
            orgName = org[1].Trim();
            if ( Int32.TryParse( org[0].Trim(), out orgId ) )
                return true;
            else
                return false;


        }

        /// <summary>
        /// Search for credential assets.
        /// At this time the number would seem to be small, so not including paging
        /// </summary>
        /// <param name="credentialId"></param>
        /// <returns></returns>
        public static List<CM.Entity> CredentialAssetsSearch( int credentialId )
        {
            CM.Entity result = new CM.Entity();
            List<CM.Entity> list = new List<CM.Entity>();
            using ( var context = new ViewContext() )
            {
                List<Views.Credential_Assets> results = context.Credential_Assets
                    .Where( s => s.CredentialId == credentialId )
                    .ToList();

                if ( results != null && results.Count > 0 )
                {
                    foreach ( Views.Credential_Assets item in results )
                    {
                        result = new CM.Entity();
                        result.Id = item.AssetEntityId;
                        result.EntityBaseId = item.AssetId;
                        result.EntityUid = item.AssetEntityUid;
                        result.EntityTypeId = item.AssetTypeId;
                        result.EntityType = item.AssetType;
                        result.EntityBaseName = item.Name;

                        list.Add( result );
                    }

                }
            }

            return list;
        }
        public static List<CodeItem> CredentialAssetsSearch2( int credentialId )
        {
            CodeItem result = new CodeItem();
            List<CodeItem> list = new List<CodeItem>();
            using ( var context = new ViewContext() )
            {
                List<Views.Credential_Assets> results = context.Credential_Assets
                    .Where( s => s.CredentialId == credentialId )
                    .ToList();

                if ( results != null && results.Count > 0 )
                {
                    foreach ( Views.Credential_Assets item in results )
                    {
                        result = new CodeItem();
                        result.Id = item.AssetEntityId;
                        result.Title = item.AssetType + " - " + item.Name;

                        list.Add( result );
                    }

                }
            }

            return list;
        }
        /// <summary>
        /// Map properties from the database to the class
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="cr"></param>
        public static void MapFromDB( EM.Credential from, CM.Credential to,
                    CredentialRequest cr )
        {
            to.Id = from.Id;
            to.RowId = from.RowId;
            to.EntityStateId = ( int )( from.EntityStateId ?? 1 );

            to.Name = from.Name;
            to.Description = from.Description;

            to.SubjectWebpage = from.SubjectWebpage != null ? from.SubjectWebpage : "";

            to.CTID = from.CTID;
            to.CredentialRegistryId = from.CredentialRegistryId;
            // 16-06-15 mp - always include credential type
            //can be null for a pending record
            to.CredentialTypeId = ( int )( from.CredentialTypeId ?? 0 );
            if ( to.CredentialTypeId > 0 )
            {
                CodeItem ct = CodesManager.Codes_PropertyValue_Get( to.CredentialTypeId );
                if ( ct != null && ct.Id > 0 )
                {
                    to.CredentialType = ct.Title;
                    to.CredentialTypeSchema = ct.SchemaName;
                }

                to.CredentialTypeEnum = EntityPropertyManager.FillEnumeration( to.RowId, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_TYPE );
                to.CredentialTypeEnum.Items.Add( new EnumeratedItem() { Id = to.CredentialTypeId, Name = ct.Name, SchemaName = ct.SchemaName } );
            }

            if ( from.ImageUrl != null && from.ImageUrl.Trim().Length > 0 )
                to.ImageUrl = from.ImageUrl;
            else
                to.ImageUrl = null;

            if ( IsGuidValid( from.OwningAgentUid ) )
            {
                to.OwningAgentUid = ( Guid )from.OwningAgentUid;
                to.OwningOrganization = OrganizationManager.GetForSummary( to.OwningAgentUid );

                //get roles
                OrganizationRoleProfile orp = Entity_AgentRelationshipManager.AgentEntityRole_GetAsEnumerationFromCSV( to.RowId, to.OwningAgentUid );
                to.OwnerRoles = orp.AgentRole;
            }


            //
            to.OwningOrgDisplay = to.OwningOrganization.Name;

            to.AudienceLevelType = EntityPropertyManager.FillEnumeration( to.RowId, CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL );


            if ( cr.IsForProfileLinks ) //return minimum ===========
                return;
            //===================================================================

            if ( IsGuidValid( from.CopyrightHolder ) )
            {
                to.CopyrightHolder = ( Guid )from.CopyrightHolder;
                //not sure if we need the org for display?
                to.CopyrightHolderOrganization = OrganizationManager.GetForSummary( to.CopyrightHolder );
            }

            //will need to do convert before switching these
            to.AlternateName = Entity_ReferenceManager.GetAllToList( to.RowId, CodesManager.PROPERTY_CATEGORY_ALTERNATE_NAME );
            if ( !string.IsNullOrWhiteSpace( from.AlternateName ) && to.AlternateName.Count == 0 )
                to.AlternateName.Add( from.AlternateName );

            to.CredentialId = from.CredentialId;
            to.CodedNotation = from.CodedNotation;
            to.AvailabilityListing = from.AvailabilityListing;


            if ( IsValidDate( from.EffectiveDate ) )
                to.DateEffective = ( ( DateTime )from.EffectiveDate ).ToShortDateString();
            else
                to.DateEffective = "";

            to.LatestVersion = from.LatestVersionUrl;
            to.PreviousVersion = from.ReplacesVersionUrl;

            to.AvailableOnlineAt = from.AvailableOnlineAt;

            if ( IsValidDate( from.Created ) )
                to.Created = ( DateTime )from.Created;
            if ( IsValidDate( from.LastUpdated ) )
                to.LastUpdated = ( DateTime )from.LastUpdated;

            if ( ( from.InLanguageId ?? 0 ) > 0 )
            {
                to.InLanguageId = ( int )from.InLanguageId;
                EnumeratedItem code = CodesManager.GetLanguage( to.InLanguageId );
                if ( code.Id > 0 )
                {
                    to.InLanguage = code.Name;
                    to.InLanguageCode = code.Value;
                }
            }
            else
            {
                to.InLanguageId = 0;
                to.InLanguage = "";
                to.InLanguageCode = "";
            }
            //multiple languages, now in entity.reference
            to.InLanguageCodeList = Entity_ReferenceManager.GetAll( to.RowId, CodesManager.PROPERTY_CATEGORY_LANGUAGE );
            //short term convenience
            if ( to.InLanguageCodeList != null && to.InLanguageCodeList.Count > 0 )
                to.InLanguage = to.InLanguageCodeList[0].TextValue;

            to.ProcessStandards = from.ProcessStandards ?? "";
            to.ProcessStandardsDescription = from.ProcessStandardsDescription ?? "";

            //properties ===========================================

            //**TODO VersionIdentifier - need to change to a list of IdentifierValue
            to.VersionIdentifier = from.Version;
            //assumes only one identifier type per class
            to.VersionIdentifierList = Entity_IdentifierValueManager.GetAll( to.RowId, Entity_IdentifierValueManager.CREDENTIAL_VersionIdentifier );

            if ( cr.IncludingEstimatedCosts )
            {
                //to.EstimatedCosts = CostProfileManager.GetAll( to.RowId, cr.IsForEditView );
                to.EstimatedCosts = CostProfileManager.GetAll( to.RowId );

                //Include currencies to fix null errors in various places (detail, compare, publishing) - NA 3/17/2017
                var currencies = CodesManager.GetCurrencies();
                //Include cost types to fix other null errors - NA 3/17/2017
                var costTypes = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_CREDENTIAL_ATTAINMENT_COST );
                foreach ( var cost in to.EstimatedCosts )
                {
                    cost.CurrencyTypes = currencies;

                    foreach ( var costItem in cost.Items )
                    {
                        costItem.DirectCostType.Items.Add( costTypes.Items.FirstOrDefault( m => m.CodeId == costItem.CostTypeId ) );
                    }
                }
                //End edits - NA 3/17/2017
            }

            to.CredentialStatusType = EntityPropertyManager.FillEnumeration( to.RowId, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_STATUS_TYPE );

            //just in case
            if ( to.EstimatedCosts == null )
                to.EstimatedCosts = new List<CostProfile>();

            //profiles ==========================================
            to.FinancialAssistance = Entity_FinancialAlignmentProfileManager.GetAll( to.RowId );

            if ( cr.IncludingAddesses )
                to.Addresses = Entity_AddressManager.GetAll( to.RowId );

            if ( cr.IncludingDuration )
                to.EstimatedDuration = DurationProfileManager.GetAll( to.RowId );

            if ( cr.IncludingFrameworkItems )
            {
                //to.Occupation = Entity_FrameworkItemManager.FillEnumeration( to.RowId, CodesManager.PROPERTY_CATEGORY_SOC );
                to.Occupation = Reference_FrameworksManager.FillEnumeration( to.RowId, CodesManager.PROPERTY_CATEGORY_SOC );
                //to.OtherOccupations = Entity_ReferenceManager.GetAll( to.RowId, CodesManager.PROPERTY_CATEGORY_SOC );

                //to.Industry = Entity_FrameworkItemManager.FillEnumeration( to.RowId, CodesManager.PROPERTY_CATEGORY_NAICS );
                to.Industry = Reference_FrameworksManager.FillEnumeration( to.RowId, CodesManager.PROPERTY_CATEGORY_NAICS );
                //to.OtherIndustries = Entity_ReferenceManager.GetAll( to.RowId, CodesManager.PROPERTY_CATEGORY_NAICS );

            }

            if ( cr.IncludingConnectionProfiles )
            {
                //get all associated top level learning opps, and assessments
                //will always be for profile lists - not expected any where else other than edit


                //assessment
                //NOTE: all the target entities will be drawn from conditions!!!
                //to.TargetCredential = Entity_CredentialManager.GetAll( to.RowId );

                //******************get all condition profiles *******************
                //TODO - have custom version of this to only get minimum!!
                //NOTE - the IsForEditView relates to cred, but probably don't want to sent true to the fill
                //re: commonConditions - consider checking if any exist, and if not, don't show

                //need to ensure competencies are bubbled up
                Entity_ConditionProfileManager.FillConditionProfilesForDetailDisplay( to );

                to.CommonConditions = Entity_CommonConditionManager.GetAll( to.RowId );
                to.CommonCosts = Entity_CommonCostManager.GetAll( to.RowId );


            }

            if ( cr.IncludingRevocationProfiles )
            {
                to.Revocation = Entity_RevocationProfileManager.GetAll( to.RowId );
            }

            if ( cr.IncludingJurisdiction )
            {
                to.Jurisdiction = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( to.RowId );
                //to.Region = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( to.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_RESIDENT );
            }
            //TODO - CredentialProcess is used in the detail pages. Should be removed and use individual profiles

            to.CredentialProcess = Entity_ProcessProfileManager.GetAll( to.RowId );
            foreach ( ProcessProfile item in to.CredentialProcess )
            {
                if ( item.ProcessTypeId == Entity_ProcessProfileManager.ADMIN_PROCESS_TYPE )
                    to.AdministrationProcess.Add( item );
                else if ( item.ProcessTypeId == Entity_ProcessProfileManager.DEV_PROCESS_TYPE )
                    to.DevelopmentProcess.Add( item );
                else if ( item.ProcessTypeId == Entity_ProcessProfileManager.MTCE_PROCESS_TYPE )
                    to.MaintenanceProcess.Add( item );
                else if ( item.ProcessTypeId == Entity_ProcessProfileManager.REVIEW_PROCESS_TYPE )
                    to.ReviewProcess.Add( item );
                else if ( item.ProcessTypeId == Entity_ProcessProfileManager.REVOKE_PROCESS_TYPE )
                    to.RevocationProcess.Add( item );
                else if ( item.ProcessTypeId == Entity_ProcessProfileManager.APPEAL_PROCESS_TYPE )
                    to.AppealProcess.Add( item );
                else if ( item.ProcessTypeId == Entity_ProcessProfileManager.COMPLAINT_PROCESS_TYPE )
                    to.ComplaintProcess.Add( item );
                else
                {
                    //unexpected
                }
            }

            if ( cr.IncludingEmbeddedCredentials )
            {
                to.EmbeddedCredentials = Entity_CredentialManager.GetAll( to.RowId );
            }


            //populate is part of - when??
            if ( from.Entity_Credential != null && from.Entity_Credential.Count > 0 )
            {
                foreach ( EM.Entity_Credential ec in from.Entity_Credential )
                {
                    if ( ec.Entity != null )
                    {
                        //This method needs to be enhanced to get enumerations for the credential for display on the detail page - NA 6/2/2017
                        //Need to determine is when non-edit, is actually for the detail reference
                        //only get where parent is a credential, ex not a condition profile
                        if ( ec.Entity.EntityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL )
                            to.IsPartOf.Add( GetBasic( ec.Entity.EntityUid ) );
                    }
                }
            }

            if ( cr.IncludingSubjectsKeywords )
            {
                if ( cr.BubblingUpSubjects )
                    to.Subject = Entity_ReferenceManager.GetAllSubjects( to.RowId );
                else
                    to.Subject = Entity_ReferenceManager.GetAll( to.RowId, CodesManager.PROPERTY_CATEGORY_SUBJECT );

                to.Keyword = Entity_ReferenceManager.GetAll( to.RowId, CodesManager.PROPERTY_CATEGORY_KEYWORD );
            }

            to.DegreeConcentration = Entity_ReferenceManager.GetAll( to.RowId, CodesManager.PROPERTY_CATEGORY_DEGREE_CONCENTRATION );
            to.DegreeMajor = Entity_ReferenceManager.GetAll( to.RowId, CodesManager.PROPERTY_CATEGORY_DEGREE_MAJOR );
            to.DegreeMinor = Entity_ReferenceManager.GetAll( to.RowId, CodesManager.PROPERTY_CATEGORY_DEGREE_MINOR );

            //---------------
            if ( cr.IncludingRolesAndActions )
            {
                to.JurisdictionAssertions = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( to.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_OFFERREDIN );


                //get as ennumerations
                to.OrganizationRole = Entity_AgentRelationshipManager.AgentEntityRole_GetAll_ToEnumeration( to.RowId, true );
            }
        } //


        private static void MapToDB( CM.Credential input, EM.Credential output )
        {
            output.Id = input.Id;
            if ( output.Id < 1 )
            {
                output.CTID = input.CTID;
            }            
            //don't map rowId, ctid, or dates as not on form
            //output.RowId = input.RowId;

            if ( !string.IsNullOrWhiteSpace( input.CredentialRegistryId))
                output.CredentialRegistryId = input.CredentialRegistryId;
            output.Name = GetData( input.Name );
            output.Description = GetData( input.Description );
            output.CredentialTypeId = input.CredentialTypeId;

            //TODO - need output chg output use text value profile
            //import will stop populating this
            if ( input.AlternateName != null && input.AlternateName.Count > 0 )
                output.AlternateName = input.AlternateName[0];

            output.CredentialId = string.IsNullOrWhiteSpace( input.CredentialId ) ? null : input.CredentialId;
            output.CodedNotation = GetData( input.CodedNotation );

            //handle old version setting output zero
            if ( IsGuidValid( input.OwningAgentUid ) )
            {
                if ( output.Id > 0 && output.OwningAgentUid != input.OwningAgentUid )
                {
                    if ( IsGuidValid( output.OwningAgentUid ) )
                    {
                        //need output remove the owner role, or could have been others
                        string statusMessage = "";
                        new Entity_AgentRelationshipManager().Delete( output.RowId, output.OwningAgentUid, Entity_AgentRelationshipManager.ROLE_TYPE_OWNER, ref statusMessage );
                    }
                }
                output.OwningAgentUid = input.OwningAgentUid;
                //get for use to add to elastic pending
                input.OwningOrganization = OrganizationManager.GetForSummary( input.OwningAgentUid );
                
            }
            else
            {
                //always have output have an owner
                output.OwningAgentUid = null;
            }

            if ( input.OwnerOrganizationRoles != null && input.OwnerOrganizationRoles.Count > 0 )
            {
                //may need output do something in case was a change via the roles popup
            }

            //***TODO - replace with list
            output.Version = GetData( input.VersionIdentifier );
            if ( IsValidDate( input.DateEffective ) )
                output.EffectiveDate = DateTime.Parse( input.DateEffective );
            else //handle reset
                output.EffectiveDate = null;

            //output.Url = GetUrlData( input.Url, null );
            //output.SubjectWebpage = GetUrlData( input.SubjectWebpage, null );
            output.SubjectWebpage = GetUrlData( input.SubjectWebpage, null );

            output.LatestVersionUrl = GetUrlData( input.LatestVersion, null );
            output.ReplacesVersionUrl = GetUrlData( input.PreviousVersion, null );
            output.AvailabilityListing = GetUrlData( input.AvailabilityListing, null );
            output.AvailableOnlineAt = GetUrlData( input.AvailableOnlineAt, null );
            output.ImageUrl = GetUrlData( input.ImageUrl, null );

            if ( input.InLanguageId > 0 )
                output.InLanguageId = input.InLanguageId;
            else if ( !string.IsNullOrWhiteSpace( input.InLanguage ) )
            {
                output.InLanguageId = CodesManager.GetLanguageId( input.InLanguage );
            }
            else if ( input.InLanguageCodeList != null && input.InLanguageCodeList.Count > 0 )
            {
                output.InLanguageId = CodesManager.GetLanguageId( input.InLanguageCodeList[0].TextValue );
            }
            else
                output.InLanguageId = null;

            output.ProcessStandards = GetUrlData( input.ProcessStandards, null );
            output.ProcessStandardsDescription = input.ProcessStandardsDescription;

            if ( IsGuidValid( input.CopyrightHolder ) )
                output.CopyrightHolder = input.CopyrightHolder;
            else
                output.CopyrightHolder = null;

        }

        #endregion


    }
    public class CredentialRequest
    {
        public CredentialRequest()
        {
        }
        public void DoCompleteFill()
        {
            IncludingProperties = true;
        }
        public void IsDetailRequest()
        {
            IsForDetailView = true;
            IncludingProperties = true;
            IncludingEstimatedCosts = true;
            IncludingDuration = true;
            IncludingFrameworkItems = true;
            IncludingRolesAndActions = true;

            //add all conditions profiles for now - to get all costs
            IncludingConnectionProfiles = true;
            ConditionProfilesAsList = false;
            IncludingAddesses = true;
            IncludingSubjectsKeywords = true;
            BubblingUpSubjects = true;
            IncludingEmbeddedCredentials = true;

            IncludingJurisdiction = true;
            IncludingRevocationProfiles = true;
        }
        public void IsPublishRequest()
        {
            //check if this is valid for publishing
            IsForPublishRequest = true;
            IsForDetailView = true;
            IncludingProperties = true;
            IncludingEstimatedCosts = true;
            IncludingDuration = true;
            IncludingFrameworkItems = true;
            IncludingRolesAndActions = true;

            //add all conditions profiles for now - to get all costs
            IncludingConnectionProfiles = true;
            ConditionProfilesAsList = false;
            IncludingAddesses = true;
            IncludingSubjectsKeywords = true;
            BubblingUpSubjects = false;
            IncludingEmbeddedCredentials = true;

            IncludingJurisdiction = true;
            IncludingRevocationProfiles = true;
        }
        public void IsCompareRequest()
        {
            IncludingProperties = true;
            IncludingEstimatedCosts = true;
            IncludingDuration = true;
            IncludingFrameworkItems = true;
            IncludingRolesAndActions = true;

            //add all conditions profiles for now - to get all costs
            IncludingConnectionProfiles = true;
        }

        public bool IsForDetailView { get; set; }
        public bool IsForPublishRequest { get; set; }
        public bool IsForProfileLinks { get; set; }
        public bool AllowCaching { get; set; }

        public bool IncludingProperties { get; set; }

        public bool IncludingRolesAndActions { get; set; }
        public bool IncludingConnectionProfiles { get; set; }
        public bool ConditionProfilesAsList { get; set; }
        public bool IncludingRevocationProfiles { get; set; }
        public bool IncludingEstimatedCosts { get; set; }
        public bool IncludingDuration { get; set; }
        public bool IncludingAddesses { get; set; }
        public bool IncludingJurisdiction { get; set; }

        public bool IncludingSubjectsKeywords { get; set; }
        public bool BubblingUpSubjects { get; set; }

        //public bool IncludingKeywords{ get; set; }
        //both occupations and industries, and others for latter
        public bool IncludingFrameworkItems { get; set; }

        public bool IncludingEmbeddedCredentials { get; set; }
    }


}

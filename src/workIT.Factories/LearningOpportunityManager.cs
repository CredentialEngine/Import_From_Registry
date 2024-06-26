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
using CondProfileMgr = workIT.Factories.Entity_ConditionProfileManager;
using DBResource = workIT.Data.Tables.LearningOpportunity;
using EM = workIT.Data.Tables;
using EntityContext = workIT.Data.Tables.workITEntities;
using ReferenceFrameworkItemsManager = workIT.Factories.Reference_FrameworkItemManager;
using ThisResource = workIT.Models.ProfileModels.LearningOpportunityProfile;
//using ReferenceFrameworkItemsManager = workIT.Factories.Reference_FrameworksManager;

namespace workIT.Factories
{
	public class LearningOpportunityManager : BaseFactory
    {
        static string thisClassName = "LearningOpportunityManager";
		static string EntityType = "LearningOpportunity";
		static int EntityTypeId = CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE;
        static string Entity_Label = "Learning Opportunity";
        EntityManager entityMgr = new EntityManager();
        #region LearningOpportunity - persistance ==================
        /// <summary>
        /// Update a LearningOpportunity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public bool Save( ThisResource entity, ref SaveStatus status )
        {
			LoggingHelper.DoTrace( 7, thisClassName + ".Save - entered" );

			bool isValid = true;
            int count = 0;
			DateTime lastUpdated = System.DateTime.Now;
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
                        DBResource efEntity = context.LearningOpportunity
                                    .FirstOrDefault( s => s.Id == entity.Id );

                        if ( efEntity != null && efEntity.Id > 0 )
                        {
                            //fill in fields that may not be in entity
                            entity.RowId = efEntity.RowId;

                            MapToDB( entity, efEntity );

							//19-05-21 mp - should add a check for an update where currently is deleted
							if ( ( efEntity.EntityStateId ?? 0 ) == 0 )
							{
								SiteActivity sa = new SiteActivity()
								{
									ActivityType = entity.LearningEntityType,
									Activity = "Import",
									Event = "Reactivate",
									Comment = string.Format( "LearningOpportunity had been marked as deleted, and was reactivted by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ),
									ActivityObjectId = entity.Id
								};
								new ActivityManager().SiteActivityAdd( sa );
							}
                            //assume and validate, that if we get here we have a full record
                            if ( ( efEntity.EntityStateId ?? 1 ) != 2 )
                                efEntity.EntityStateId = 3;

                            entity.EntityStateId = ( int ) efEntity.EntityStateId;

                            if ( IsValidDate( status.EnvelopeCreatedDate ) && status.LocalCreatedDate < efEntity.Created )
							{
								efEntity.Created = status.LocalCreatedDate;
							}
							if ( IsValidDate( status.EnvelopeUpdatedDate ) && status.LocalUpdatedDate != efEntity.LastUpdated )
							{
								efEntity.LastUpdated = status.LocalUpdatedDate;
								lastUpdated = status.LocalUpdatedDate;
							}
							//has changed?
							if ( HasStateChanged( context ) )
							{
								status.UpdateElasticIndex = true;
								if ( IsValidDate( status.EnvelopeUpdatedDate ) )
									efEntity.LastUpdated = status.LocalUpdatedDate;
								else
									efEntity.LastUpdated = DateTime.Now;
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

                                    string message = thisClassName + string.Format( ". Update Failed", "Attempted to update a LearningOpportunity. The process appeared to not work, but was not an exception, so we have no message, or no clue. LearningOpportunity: {0}, Id: {1}}", entity.Name, entity.Id );
                                    status.AddError( "Error - the update was not successful. " + message );
                                    EmailManager.NotifyAdmin( thisClassName + ". Update Failed", message );
                                }
                            }
                            else
                            {
                                //update entity.LastUpdated - assuming there has to have been some change in related data
                                new EntityManager().UpdateModifiedDate( entity.RowId, ref status );
                            }
							entity.LastUpdated = lastUpdated;
							UpdateEntityCache( entity, ref status );
							if ( isValid )
                            {
                                if ( !UpdateParts( entity, ref status ) )
                                    isValid = false;

                                SiteActivity sa = new SiteActivity()
                                {
                                    ActivityType = entity.LearningEntityType,
                                    Activity = "Import",
                                    Event = "Update",
                                    Comment = string.Format( "LearningOpportunity was updated by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ),
                                    ActivityObjectId = entity.Id
                                };
                                new ActivityManager().SiteActivityAdd( sa );
                            }
                        }
                        else
                        {
                            status.AddError( "Error - update failed, as record was not found.");
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
                string message = HandleDBValidationError( dbex, thisClassName + string.Format( ".Save. id: {0}, Name: {1}", entity.Id, entity.Name ), "LearningOpportunity" );
                status.AddError( thisClassName + ".Save(). Error - the save was not successful. " + message );
            }
            catch ( Exception ex )
            {
                string message = FormatExceptions( ex );
				if ( message.IndexOf( "Execution Timeout Expired." ) > 0 )
				{
					LoggingHelper.DoTrace( 1, thisClassName + string.Format( ".Save. id: {0}, Name: {1}. Execution timeout.*****", entity.Id, entity.Name ) );
					status.AddError( thisClassName + " Error - the save was not successful. " + message );
				}
				else
				{
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Save. id: {0}, Name: {1}", entity.Id, entity.Name ) );
					status.AddError( thisClassName + " Error - the save was not successful. " + message );
				}
                isValid = false;
            }


            return isValid;
        }
        /// <summary>
        /// add a LearningOpportunity
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="statusMessage"></param>
        /// <returns></returns>
        private int Add( ThisResource resource, ref SaveStatus status )
        {
			LoggingHelper.DoTrace( 7, thisClassName + ".Add - entered" );

			DBResource efEntity = new DBResource();
            using ( var context = new EntityContext() )
            {
                try
                {

                    MapToDB( resource, efEntity );
					efEntity.EntityStateId = resource.EntityStateId = 3;
					if ( IsValidGuid( resource.RowId ) )
                        efEntity.RowId = resource.RowId;
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

					context.LearningOpportunity.Add( efEntity );

                    // submit the change to database
                    int count = context.SaveChanges();
                    if ( count > 0 )
                    {
						status.UpdateElasticIndex = true;
						//
						resource.RowId = efEntity.RowId;
						resource.Created = ( DateTime )efEntity.Created;
						resource.LastUpdated = ( DateTime )efEntity.LastUpdated;
						resource.Id = efEntity.Id;
						UpdateEntityCache( resource, ref status );
						//add log entry
						SiteActivity sa = new SiteActivity()
                        {
                            ActivityType = resource.LearningEntityType,
                            Activity = "Import",
                            Event = "Add",
                            Comment = string.Format( "Full {2} was added by the import. Name: {0}, SWP: {1}", resource.Name, resource.SubjectWebpage, resource.LearningEntityType ),
                            ActivityObjectId = resource.Id
                        };
                        new ActivityManager().SiteActivityAdd( sa );


                        if ( UpdateParts( resource, ref status ) == false )
                        {

                        }

                        return resource.Id;
                    }
                    else
                    {
                        //?no info on error
                        string message = thisClassName + string.Format( ". Add Failed", "Attempted to add a Learning Opportunity. The process appeared to not work, but was not an exception, so we have no message, or no clue. LearningOpportunity: {0}, CTID: {1}", resource.Name, resource.CTID );
                        status.AddError( thisClassName + ". Error - the add was not successful. " + message );
                        EmailManager.NotifyAdmin( thisClassName + ".Add Failed", message );
                    }
                }
                catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
                {

                    string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", resource.Name );
                    status.AddError( thisClassName + " - Error - the save was not successful. " + message );
                    LoggingHelper.LogError(dbex, message);

                }
                catch ( Exception ex )
                {
                    string message = FormatExceptions( ex );
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Add(), Name: {0}, CTID: {1}, OwningAgentUid: {2}", efEntity.Name, efEntity.CTID, efEntity.OwningAgentUid ) );

					status.AddError( thisClassName + " - Error - the save was not successful. \r\n" + message );
                }
            }

            return resource.Id;
        }
        public int AddBaseReference( ThisResource entity, ref SaveStatus status )
        {
			//*** need to handle updates, do we?
            DBResource efEntity = new DBResource();
            try
            {
                using ( var context = new EntityContext() )
                {
                    if ( entity == null ||
                        ( string.IsNullOrWhiteSpace( entity.Name )) 
						//||                        string.IsNullOrWhiteSpace( input.SubjectWebpage )) 
						)
                    {
                        status.AddError( thisClassName + ". AddBaseReference() The learning opportunity is incomplete" );
                        return 0;
                    }

					//only add DB required properties
					//NOTE - an input will be created via trigger
					//20-08-27 mp - using full MapToDB to handle any additions that appear in the future
					MapToDB( entity, efEntity );
					efEntity.EntityStateId = entity.EntityStateId = 2;
					//set to active 
					var defStatus = CodesManager.GetLifeCycleStatus( CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS_ACTIVE );
					efEntity.LifeCycleStatusTypeId = defStatus.Id;

					//
					if ( IsValidGuid( entity.RowId ) )
                        efEntity.RowId = entity.RowId;
                    else
                        efEntity.RowId = Guid.NewGuid();
                    //set to return, just in case
                    entity.RowId = efEntity.RowId;
                    efEntity.Created = System.DateTime.Now;
                    efEntity.LastUpdated = System.DateTime.Now;

                    context.LearningOpportunity.Add( efEntity );
                    int count = context.SaveChanges();
                    if ( count > 0 )
                    {
                        entity.Id = efEntity.Id;
						if ( UpdateParts( entity, ref status ) == false )
						{

						}

						SiteActivity sa = new SiteActivity()
						{
							ActivityType = entity.LearningEntityType,
							Activity = "Import",
							Event = "Add Reference",
							Comment = string.Format( "Reference LearningOpportunity was added by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ),
							ActivityObjectId = entity.Id
						};

						entity.Id = efEntity.Id;
						entity.RowId = efEntity.RowId;
						entity.Created = ( DateTime )efEntity.Created;
						entity.LastUpdated = ( DateTime )efEntity.LastUpdated;
						UpdateEntityCache( entity, ref status );
						return efEntity.Id;
                    }

                    status.AddError( thisClassName + ". AddBaseReference() Error - the save was not successful, but no message provided. " );
                }
            }
			catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
			{
				status.AddError( HandleDBValidationError( dbex, thisClassName + ".AddBaseReference() ", "LearningOpportunity" ) );
				LoggingHelper.LogError( dbex, thisClassName + string.Format( ".Add(), Name: {0}, SubjectWebpage: {1}", entity.Name, entity.SubjectWebpage ) );


			}
			catch ( Exception ex )
            {
                string message = FormatExceptions( ex );
                LoggingHelper.LogError( ex, thisClassName + string.Format( ".AddBaseReference. Name:  {0}, SubjectWebpage: {1}", entity.Name, entity.SubjectWebpage ) );
                status.AddError( thisClassName + " Error - the save was not successful. " + message );

            }
            return 0;
        }

        public int AddPendingRecord( Guid entityUid, string ctid, string registryAtId, ref SaveStatus status, int entityTypeId= 7 )
        {
            DBResource efEntity = new DBResource();
            try
            {
                using ( var context = new EntityContext() )
                {
                    if ( !IsValidGuid( entityUid ) )
                    {
                        status.AddError( thisClassName + " - A valid GUID must be provided to create a pending entity");
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
					//default to 7, and can be overridden in the update (validate)
					efEntity.EntityTypeId = entityTypeId;
					efEntity.EntityStateId = 1;
                    efEntity.RowId = entityUid;
                    //watch that Ctid can be  updated if not provided now!!
                    efEntity.CTID = ctid;
                    efEntity.SubjectWebpage = registryAtId;
					//set to active 
					var defStatus = CodesManager.GetLifeCycleStatus( CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS_ACTIVE );
					efEntity.LifeCycleStatusTypeId = defStatus.Id;
					efEntity.Created = System.DateTime.Now;
                    efEntity.LastUpdated = System.DateTime.Now;

                    context.LearningOpportunity.Add( efEntity );
                    int count = context.SaveChanges();
                    if ( count > 0 )
                    {
                        SiteActivity sa = new SiteActivity()
                        {
                            ActivityType = entity.LearningEntityType,
                            Activity = "Import",
                            Event = string.Format("Add Pending {0}", EntityType),
							Comment = string.Format( "Pending {0} was added by the import. ctid: {1}, registryAtId: {2}", EntityType, ctid, registryAtId ),
                            ActivityObjectId = efEntity.Id
                        };
                        new ActivityManager().SiteActivityAdd( sa );
						//Question should this be in the EntityCache?
						//SaveStatus status = new SaveStatus();
						entity.Id = efEntity.Id;
						entity.RowId = efEntity.RowId;
						entity.CTID = efEntity.CTID;
						entity.EntityStateId = 1;
						entity.Name = efEntity.Name;
						entity.Description = efEntity.Description;
						entity.SubjectWebpage = efEntity.SubjectWebpage;
						entity.Created = ( DateTime )efEntity.Created;
						entity.LastUpdated = ( DateTime )efEntity.LastUpdated;
						UpdateEntityCache( entity, ref status );
						return efEntity.Id;
                    }

                    status.AddError( thisClassName + " Error - the save was not successful, but no message provided. ");
                }
            }

            catch ( Exception ex )
            {
                string message = FormatExceptions( ex );
                LoggingHelper.LogError( ex, thisClassName + string.Format( ".AddPendingRecord. entityUid:  {0}, ctid: {1}", entityUid, ctid ) );
                status.AddError( thisClassName + " Error - the save was not successful. " + message);

            }
            return 0;
        }
		public void UpdateEntityCache( ThisResource document, ref SaveStatus status )
		{
			EntityCache ec = new EntityCache()
			{
				EntityTypeId = 7, //document.LearningEntityTypeId,
				//SubclassEntityTypeId = document.LearningEntityTypeId,
				//22-11-13 mp - back to always lopp as can be a problem with searches. Don't know why the sandbox had this (vs with a space)
				EntityType = EntityType, // document.LearningEntityType, // "LearningOpportunity",
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
            //var defStatus = CodesManager.Codes_PropertyValue_GetBySchema( CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS_ACTIVE );
            var ceasedStatus = CodesManager.GetLifeCycleStatus( CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS_CEASED );
            if ( document.LifeCycleStatusTypeId > 0 && document.LifeCycleStatusTypeId == ceasedStatus.Id )
            {
                ec.IsActive = false;
            }
            var statusMessage = string.Empty;
			if ( new EntityManager().EntityCacheSave( ec, ref statusMessage ) == 0 )
			{
				status.AddError( thisClassName + string.Format( ".UpdateEntityCache for '{0}' ({1}) failed: {2}", document.Name, document.Id, statusMessage ) );
			}
		}
		public bool ValidateProfile( ThisResource profile, ref SaveStatus status )
        {
            status.HasSectionErrors = false;

            if ( string.IsNullOrWhiteSpace( profile.Name ) )
            {
                status.AddError( "A Learning Opportunity name must be entered" );
            }
            if ( string.IsNullOrWhiteSpace( profile.Description ) )
            {
                //status.AddWarning( "A Learning Opportunity Description must be entered" );
            }
            if ( !IsValidGuid( profile.PrimaryAgentUID ) )
            {
                //status.AddWarning( "An owning organization is missing" );
            }

			//
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

			if ( !string.IsNullOrWhiteSpace( profile.DateEffective ) && !IsValidDate( profile.DateEffective ) )
            {
                status.AddWarning( "Error the effective date is invalid" );
            }

            //if ( string.IsNullOrWhiteSpace( profile.SubjectWebpage ) )
            //	status.AddWarning( "Error - A Subject Webpage name must be entered" );

            //else 
            if ( !IsUrlValid( profile.SubjectWebpage, ref commonStatusMessage ) )
            {
                status.AddWarning( "The Subject Webpage SubjectWebpage is invalid" + commonStatusMessage );
            }

            if ( !IsUrlValid( profile.AvailableOnlineAt, ref commonStatusMessage ) )
            {
                status.AddWarning( "The Available Online At SubjectWebpage is invalid" + commonStatusMessage );
            }

            if ( !IsUrlValid( profile.AvailabilityListing, ref commonStatusMessage ) )
            {
                status.AddWarning( "The Availability Listing SubjectWebpage is invalid" + commonStatusMessage );
            }

            //if ( profile.CreditHourValue < 0 || profile.CreditHourValue > 10000 )
            //    status.AddWarning( "Error: invalid value for Credit Hour Value. Must be a reasonable decimal value greater than zero." );

            //if ( profile.CreditUnitValue < 0 || profile.CreditUnitValue > 1000 )
            //    status.AddWarning( "Error: invalid value for Credit Unit Value. Must be a reasonable decimal value greater than zero." );


            //can only have credit hours properties, or credit unit properties, not both
            bool hasCreditHourData = false;
            bool hasCreditUnitData = false;
            //if ( profile.CreditHourValue > 0 || ( profile.CreditHourType ?? string.Empty ).Length > 0 )
            //    hasCreditHourData = true;
            if ( profile.CreditUnitTypeId > 0
                || ( profile.CreditUnitTypeDescription ?? string.Empty ).Length > 0
                || profile.CreditUnitValue > 0 )
                hasCreditUnitData = true;

            if ( hasCreditHourData && hasCreditUnitData )
                status.AddWarning( "Error: Data can be entered for Credit Hour related properties or Credit Unit related properties, but not for both." );

			return status.WasSectionValid;
		}


		/// <summary>
		/// Delete by envelopeId
		/// </summary>
		/// <param name="envelopeId"></param>
		/// <param name="ctid"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool Delete( string ctid, ref string statusMessage )
        {
            bool isValid = true;
            if ( string.IsNullOrWhiteSpace( ctid ) )
            {
                statusMessage = thisClassName + ".Delete() Error - a valid CTID must be provided.";
                return false;
            }
            if ( string.IsNullOrWhiteSpace( ctid ) )
                ctid = "SKIP ME";

			int orgId = 0;
			Guid orgUid = new Guid();
			using ( var context = new EntityContext() )
            {
                try
                {
                    context.Configuration.LazyLoadingEnabled = false;
                    DBResource efEntity = context.LearningOpportunity
                                .FirstOrDefault( s => s.CTID == ctid );

                    if ( efEntity != null && efEntity.Id > 0 )
                    {
                        Guid rowId = efEntity.RowId;
						if ( IsValidGuid( efEntity.OwningAgentUid ) )
						{
							Organization org = OrganizationManager.GetBasics( ( Guid )efEntity.OwningAgentUid );
							orgId = org.Id;
							orgUid = org.RowId;
						}
						//need to remove from Entity.
						//-using before delete trigger - verify won't have RI issues
						string msg = string.Format( " Learning Opportunity. Id: {0}, Name: {1}, Ctid: {2}.", efEntity.Id, efEntity.Name, efEntity.CTID );

                        //18-04-05 mparsons - change to set inactive, and notify - seems to have been some incorrect deletes

                        //context.LearningOpportunity.Remove( efEntity );
                        efEntity.EntityStateId = 0;
                        efEntity.LastUpdated = System.DateTime.Now;

                        int count = context.SaveChanges();
                        if ( count > 0 )
                        {
							var entityType = CodesManager.Codes_EntityType_Get( efEntity.EntityTypeId, "LearningOpportunity" );
							isValid = true;
                            //trace is done in import
                            //LoggingHelper.DoTrace( 2, "Learning Opportunity virtually deleted: " + msg );
                            new ActivityManager().SiteActivityAdd( new SiteActivity()
                            {
                                ActivityType = entityType,
                                Activity = "Import",
                                Event = "Delete",
								Comment = msg,
								ActivityObjectId = efEntity.Id,
								ActivityObjectCTID = efEntity.CTID
							} );
							//delete cache
							new EntityManager().EntityCacheDelete( rowId, ref statusMessage );

							//add pending request 
							List<String> messages = new List<string>();
                            new SearchPendingReindexManager().AddDeleteRequest( CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, efEntity.Id, ref messages );
							//mark owning org for updates (actually should be covered by ReindexAgentForDeletedArtifact
							new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, orgId, 1, ref messages );

							//delete all relationships
							workIT.Models.SaveStatus status = new SaveStatus();
							Entity_AgentRelationshipManager earmgr = new Entity_AgentRelationshipManager();
							earmgr.DeleteAll( rowId, ref status );
							//also check for any relationships
							//There could be other orgs from relationships to be reindexed as well!
							new Entity_AgentRelationshipManager().ReindexAgentForDeletedArtifact( orgUid );
						}
                    }
                    else
                    {
                        statusMessage = thisClassName + ".Delete() Warning No action taken, as the record was not found.";
                    }
                }
                catch ( Exception ex )
                {
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Delete(ctid:{0})", ctid ) );
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
        #region LearningOpportunity properties ===================
        public bool UpdateParts( ThisResource entity, ref SaveStatus status )
        {
			LoggingHelper.DoTrace( appMethodEntryTraceLevel, thisClassName + ".UpdateParts - entered" );

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

            Entity_ReferenceFrameworkManager erfm = new Entity_ReferenceFrameworkManager();
            erfm.DeleteAll( relatedEntity, ref status );

			if ( erfm.SaveList( relatedEntity.Id, CodesManager.PROPERTY_CATEGORY_SOC, entity.OccupationTypes, ref status ) == false )
				isAllValid = false;
			if ( erfm.SaveList( relatedEntity.Id, CodesManager.PROPERTY_CATEGORY_NAICS, entity.IndustryTypes, ref status ) == false )
				isAllValid = false;

			//TODO - handle Naics if provided separately
			if ( erfm.NaicsSaveList( relatedEntity.Id, CodesManager.PROPERTY_CATEGORY_NAICS, entity.Naics, ref status ) == false )
				isAllValid = false;
			if ( erfm.SaveList( relatedEntity.Id, CodesManager.PROPERTY_CATEGORY_CIP, entity.InstructionalProgramTypes, ref status ) == false )
                isAllValid = false;

            //not used by import
            //if ( entity.OwnerRoles == null || entity.OwnerRoles.Items.Count == 0 )
            //{
            //    //status.AddWarning( "Invalid request, please select one or more roles for the owing agent." );
            //    //isAllValid = false;
            //}
            //else
            //{
            //    if ( entity.OwnerRoles.GetFirstItemId() != Entity_AgentRelationshipManager.ROLE_TYPE_OWNER )
            //    {
            //        //status.AddWarning( "Invalid request. The role \"Owned By\" must be one of the roles selected." );
            //        //isAllValid = false;
            //    }
            //    else
            //    {
            //        OrganizationRoleProfile profile = new OrganizationRoleProfile();
            //        profile.ParentUid = entity.RowId;
            //        profile.ActingAgentUid = entity.OwningAgentUid;
            //        profile.AgentRole = entity.OwnerRoles;
            //        profile.CreatedById = entity.LastUpdatedById;
            //        profile.LastUpdatedById = entity.LastUpdatedById;

            //        if ( !new Entity_AgentRelationshipManager().Save( profile, Entity_AgentRelationshipManager.VALID_ROLES_OWNER, ref status ) )
            //            isAllValid = false;
            //    }
            //}

            Entity_ReferenceManager erm = new Entity_ReferenceManager();
            erm.DeleteAll( relatedEntity, ref status );
			//if ( erm.Add( entity.OtherInstructionalProgramCategory, entity.RowId, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, ref status, CodesManager.PROPERTY_CATEGORY_CIP, false ) == false )
			//isAllValid = false;            
			if ( erm.Add( entity.DegreeConcentration, entity.RowId, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, ref status, CodesManager.PROPERTY_CATEGORY_DEGREE_CONCENTRATION, false ) == false )
				isAllValid = false;

			if ( erm.Add( entity.Subject, entity.RowId, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, ref status, CodesManager.PROPERTY_CATEGORY_SUBJECT, false ) == false )
                isAllValid = false;

            if ( erm.Add( entity.Keyword, entity.RowId, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, ref status, CodesManager.PROPERTY_CATEGORY_KEYWORD, false ) == false )
                isAllValid = false;

            //for language, really want to convert from en to English (en)
            erm.AddLanguages( entity.InLanguageCodeList, entity.RowId, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, ref status, CodesManager.PROPERTY_CATEGORY_LANGUAGE );
			//
			erm.Add( entity.SameAs, entity.RowId, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, ref status, CodesManager.PROPERTY_CATEGORY_SAME_AS, true );

			if ( erm.Add( entity.AlternateNames, entity.RowId, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, ref status, CodesManager.PROPERTY_CATEGORY_ALTERNATE_NAME, false ) == false )
				isAllValid = false;

			AddProfiles( entity, relatedEntity, ref status );
			//==================	HasPartIds	=============================
			//delete all regardless
			Entity_LearningOpportunityManager elm = new Entity_LearningOpportunityManager();
			elm.DeleteAll( relatedEntity, ref status );
			if ( entity.HasPartIds != null && entity.HasPartIds.Count > 0 )
            {             
                foreach ( var hasPartId in entity.HasPartIds )
                {
                    LoggingHelper.DoTrace( 5, thisClassName + string.Format( ".HandleTargets. entity.ParentId: {0}, processing loppId: {1}, entity.RowId: {2}", entity.RelatedEntityId, entity.Id, entity.RowId.ToString() ) );
                    elm.Add( entity.RowId, hasPartId, BaseFactory.RELATIONSHIP_TYPE_HAS_PART, true, ref status );
                }
            }

			//why is there a separate IsPartOf??????????. Should there be handling of IsPartIds
			foreach ( var isPart in entity.IsPartOf )
                if( isPart.RelatedEntityId == 0)
                    LoggingHelper.DoTrace( 5, thisClassName + string.Format( ".HandleTargets. entity.ParentId: {0}, processing loppId: {1}, entity.RowId: {2}", entity.RelatedEntityId, entity.Id, entity.RowId.ToString() ) );

			if ( entity.IsPartOfIds != null && entity.IsPartOfIds.Count > 0 )
			{
				//elm = new Entity_LearningOpportunityManager();
				//elm.DeleteAll( relatedEntity, ref status );

				foreach ( var partId in entity.IsPartOfIds )
				{
					elm.Add( entity.RowId, partId, BaseFactory.RELATIONSHIP_TYPE_IS_PART_OF, true, ref status );
				}
			}
			//??	TargetLearningOpportunityIds
			//if using Entity_LearningOpportunityManager, will need a specific relationship type
			if ( entity.TargetLearningOpportunityIds != null && entity.TargetLearningOpportunityIds.Count > 0 )
			{
				foreach ( var item in entity.TargetLearningOpportunityIds )
				{
					elm.Add( entity.RowId, item, BaseFactory.RELATIONSHIP_TYPE_TARGET_LOPP, true, ref status );
				}
			}


			var ehssMgr = new Entity_HasSupportServiceManager();
            ehssMgr.Update( entity.HasSupportServiceIds, relatedEntity, ref status );

            //==================================
            if ( entity.PrerequisiteIds != null && entity.PrerequisiteIds.Count > 0 )
			{
				//elm = new Entity_LearningOpportunityManager();
				//elm.DeleteAll( relatedEntity, ref status );

				foreach ( var prereqId in entity.PrerequisiteIds )
				{
					elm.Add( entity.RowId, prereqId, BaseFactory.RELATIONSHIP_TYPE_HAS_PREREQUISITE, true, ref status );
				}
			}

            //==================================
            if ( entity.TargetAssessmentIds != null && entity.TargetAssessmentIds.Count > 0 )
            {
                Entity_AssessmentManager eam = new Entity_AssessmentManager();
                eam.DeleteAll( relatedEntity, ref status );

                foreach ( var item in entity.TargetAssessmentIds )
                {
                    eam.Add( entity.RowId, item, BaseFactory.RELATIONSHIP_TYPE_HAS_TARGET_RESOURCE, true, ref status );
                }
            }
            //
            var ehasOffering = new Entity_HasOfferingManager();
            //destructive? delete all, then add?
            ehasOffering.DeleteAll( relatedEntity, ref status );
            if ( entity.HasOfferingIds != null && entity.HasOfferingIds.Count > 0 )
            {
                ehasOffering.SaveList( entity.HasOfferingIds, relatedEntity, ref status );
            }
            //Entity_HasResource
            var eHasResourcesMgr = new Entity_HasResourceManager();
            eHasResourcesMgr.DeleteAll( relatedEntity, ref status );
            // Transfer Value 
            if ( eHasResourcesMgr.SaveList( relatedEntity, CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, entity.ProvidesTVForIds, ref status, Entity_HasResourceManager.HAS_RESOURCE_TYPE_ProvidesTransferValueFor ) == false )
                isAllValid = false;
            if ( eHasResourcesMgr.SaveList( relatedEntity, CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, entity.ReceivesTVFromIds, ref status, Entity_HasResourceManager.HAS_RESOURCE_TYPE_ReceivesTransferValueFrom ) == false )
                isAllValid = false;
            if ( eHasResourcesMgr.SaveList( relatedEntity, CodesManager.ENTITY_TYPE_CREDENTIALING_ACTION, entity.ObjectOfActionIds, ref status, Entity_HasResourceManager.HAS_RESOURCE_TYPE_HasResource ) == false )
                isAllValid = false;
            //
            if ( eHasResourcesMgr.SaveList( relatedEntity, CodesManager.ENTITY_TYPE_RUBRIC, entity.HasRubricIds, ref status, Entity_HasResourceManager.HAS_RESOURCE_TYPE_HasResource ) == false )
                isAllValid = false;
            UpdateAssertedBys( entity, ref status );

            UpdateAssertedIns( entity, ref status );

			LoggingHelper.DoTrace( appMethodExitTraceLevel, thisClassName + ".UpdateParts - exited" );

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

				if ( mgr.AddProperties( entity.AssessmentMethodType, entity.RowId, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, CodesManager.PROPERTY_CATEGORY_Assessment_Method_Type, false, ref status ) == false )
					isAllValid = false;

				if ( mgr.AddProperties( entity.LearningMethodType, entity.RowId, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, CodesManager.PROPERTY_CATEGORY_Learning_Method_Type, false, ref status ) == false )
					isAllValid = false;


				if ( mgr.AddProperties( entity.DeliveryType, entity.RowId, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE, false, ref status ) == false )
					isAllValid = false;

				if ( mgr.AddProperties( entity.AudienceLevelType, entity.RowId, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL, false, ref status ) == false )
					isAllValid = false;

				if ( mgr.AddProperties( entity.AudienceType, entity.RowId, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE, false, ref status ) == false )
					isAllValid = false;

				if ( mgr.AddProperties( entity.ScheduleFrequencyType, entity.RowId, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, CodesManager.PROPERTY_CATEGORY_SCHEDULE_FREQUENCY, false, ref status ) == false )
					isAllValid = false;

				if ( mgr.AddProperties( entity.ScheduleTimingType, entity.RowId, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, CodesManager.PROPERTY_CATEGORY_SCHEDULE_TIMING, false, ref status ) == false )
					isAllValid = false;

				if ( mgr.AddProperties( entity.OfferFrequencyType, entity.RowId, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, CodesManager.PROPERTY_CATEGORY_OFFER_FREQUENCY, false, ref status ) == false )
					isAllValid = false;

				//TODO - remove after completing direct assignments
				//if ( mgr.AddProperties( entity.LifeCycleStatusType, entity.RowId, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, false, ref status ) == false )
				//	isAllValid = false;
			} catch (Exception ex)
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

				//Identifiers - do delete for first one and then assign
				new Entity_IdentifierValueManager().SaveList( entity.Identifier, entity.RowId, Entity_IdentifierValueManager.IdentifierValue_Identifier, ref status, true );

				//VersionIdentifier
				new Entity_IdentifierValueManager().SaveList( entity.VersionIdentifierList, entity.RowId, Entity_IdentifierValueManager.IdentifierValue_VersionIdentifier, ref status, false );

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
                //ConditionProfile =======================================
                Entity_ConditionProfileManager emanager = new Entity_ConditionProfileManager();
                //emanager.DeleteAll( relatedEntity, ref status );

                emanager.SaveList( entity.Requires, Entity_ConditionProfileManager.ConnectionProfileType_Requirement, entity.RowId, ref status );
                emanager.SaveList( entity.Recommends, Entity_ConditionProfileManager.ConnectionProfileType_Recommendation, entity.RowId, ref status );
                emanager.SaveList( entity.Corequisite, Entity_ConditionProfileManager.ConnectionProfileType_Corequisite, entity.RowId, ref status );
				emanager.SaveList( entity.CoPrerequisite, Entity_ConditionProfileManager.ConnectionProfileType_CoPrerequisite, entity.RowId, ref status );

				emanager.SaveList( entity.EntryCondition, Entity_ConditionProfileManager.ConnectionProfileType_EntryCondition, entity.RowId, ref status );

                //Connections
                emanager.SaveList( entity.IsAdvancedStandingFor, Entity_ConditionProfileManager.ConnectionProfileType_AdvancedStandingFor, entity.RowId, ref status, 4 );
                emanager.SaveList( entity.AdvancedStandingFrom, Entity_ConditionProfileManager.ConnectionProfileType_AdvancedStandingFrom, entity.RowId, ref status, 4 );
                emanager.SaveList( entity.IsPreparationFor, Entity_ConditionProfileManager.ConnectionProfileType_PreparationFor, entity.RowId, ref status, 4 );
                emanager.SaveList( entity.PreparationFrom, Entity_ConditionProfileManager.ConnectionProfileType_PreparationFrom, entity.RowId, ref status, 4 );
                emanager.SaveList( entity.IsRequiredFor, Entity_ConditionProfileManager.ConnectionProfileType_NextIsRequiredFor, entity.RowId, ref status, 4 );
                emanager.SaveList( entity.IsRecommendedFor, Entity_ConditionProfileManager.ConnectionProfileType_NextIsRecommendedFor, entity.RowId, ref status, 4 );
            }
            catch ( Exception ex )
            {
                string message = FormatExceptions( ex );
                LoggingHelper.LogError( ex, thisClassName + string.Format( ".AddProfiles - 2. ConditionProfiles. id: {0}", entity.Id ) );
                status.AddWarning( thisClassName + ".AddProfiles(). Exceptions encountered handling ConditionProfiles. " + message );
            }

			try
			{
				//Financial Alignment 
				//Entity_FinancialAlignmentProfileManager fapm = new Factories.Entity_FinancialAlignmentProfileManager();
				//fapm.SaveList( entity.FinancialAssistanceOLD, entity.RowId, ref status );
				new Entity_FinancialAssistanceProfileManager().SaveList( entity.FinancialAssistance, entity.RowId, ref status );

				//competencies
				Entity_CompetencyManager ecm = new Entity_CompetencyManager();
				//23-01-08 mp - do delete from entity to handle multiple types
				ecm.DeleteAll( relatedEntity, ref status );
				ecm.SaveList( "Teaches", entity.TeachesCompetencies, entity.RowId, ref status );
				//22-11-30 - now will need to distinguish assesses versus teaches
				ecm.SaveList( "Assesses", entity.AssessesCompetencies, entity.RowId, ref status );

				//addresses
				new Entity_AddressManager().SaveList( entity.AvailableAt, entity.RowId, ref status );

				//JurisdictionProfile 
				Entity_JurisdictionProfileManager jpm = new Entity_JurisdictionProfileManager();
				//do deletes - NOTE: other jurisdictions are added in: UpdateAssertedIns
				jpm.DeleteAll( relatedEntity, ref status );

				jpm.SaveList( entity.Jurisdiction, entity.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_SCOPE, ref status );

				new Entity_CommonConditionManager().SaveList( entity.ConditionManifestIds, entity.RowId, ref status );

				new Entity_CommonCostManager().SaveList( entity.CostManifestIds, entity.RowId, ref status );
			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, string.Format( "**EXCEPTION** LearningOpportunityManager.AddProfiles - 3. Name: {0}, Id: {1}, Message: {2}.", entity.Name, entity.Id, ex.Message ) );
			}

			var adpm = new Entity_AggregateDataProfileManager();
			if ( adpm.SaveList( entity.AggregateData, relatedEntity, ref status ) == false )
				status.HasSectionErrors = true;
            //reactivate any related dataSetProfiles
            //not necessary done after e.adp processing
            //if ( entity.DataSetProfileCTIDList!= null && entity.DataSetProfileCTIDList.Count > 0 )
            //{

            //}
			LoggingHelper.DoTrace( LoggingHelper.appMethodExitTraceLevel, thisClassName + ".AddProfiles - exited." );

		}


		public bool UpdateAssertedBys( ThisResource entity, ref SaveStatus status )
        {
			LoggingHelper.DoTrace( 7, thisClassName + ".UpdateAssertedBys - entered" );

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
            mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_OFFERED_BY, entity.OfferedBy, ref status );
            mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_OWNER, entity.OwnedBy, ref status );
            mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_RecognizedBy, entity.RecognizedBy, ref status );
            mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_RegulatedBy, entity.RegulatedBy, ref status );
            //RegisteredBy
            mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_RegisteredBy, entity.RegisteredBy, ref status );


            mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_PUBLISHEDBY, entity.PublishedBy, ref status );
			LoggingHelper.DoTrace( 7, thisClassName + ".UpdateAssertedBys - exited." );

			return isAllValid;
        } //


        public void UpdateAssertedIns( ThisResource entity, ref SaveStatus status )
        {
			LoggingHelper.DoTrace( 7, thisClassName + ".UpdateAssertedIns - entered" );

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
            mgr.SaveAssertedInList( entity.RowId, Entity_AgentRelationshipManager.ROLE_TYPE_OFFERED_BY, entity.OfferedIn, ref status );

            mgr.SaveAssertedInList( entity.RowId, Entity_AgentRelationshipManager.ROLE_TYPE_RecognizedBy, entity.RecognizedIn, ref status );
            mgr.SaveAssertedInList( entity.RowId, Entity_AgentRelationshipManager.ROLE_TYPE_RegulatedBy, entity.RegulatedIn, ref status );

			LoggingHelper.DoTrace( 7, thisClassName + ".UpdateAssertedIns - exited" );

		} //

		#endregion

		#endregion

		#region == Retrieval =======================
		public static int DoesCtidExist( string ctid )
        {
            ThisResource entity = GetByCtid( ctid );
            if ( entity != null && entity.Id > 0 )
                return entity.Id;
            else
                return 0;
        }
        /// <summary>
        /// Get a minimum record by CTID
        /// </summary>
        /// <param name="ctid"></param>
        /// <returns></returns>
        public static ThisResource GetByCtid( string ctid )
        {
            ThisResource output = new ThisResource();
            using ( var context = new EntityContext() )
            {
                context.Configuration.LazyLoadingEnabled = false;
                DBResource input = context.LearningOpportunity
                        .FirstOrDefault( s => s.CTID.ToLower() == ctid.ToLower() );

                if ( input != null && input.Id > 0 )
                {
                    output.RowId = input.RowId;
                    output.Id = input.Id;
                    output.EntityStateId = ( int )( input.EntityStateId ?? 1 );
					output.EntityTypeId = input.EntityTypeId;
					output.Name = input.Name;
					output.LearningEntityTypeId = input.EntityTypeId;
                    output.Description = input.Description;
                    output.SubjectWebpage = input.SubjectWebpage;
                    output.CTID = input.CTID;
                    output.CredentialRegistryId = input.CredentialRegistryId;
					output.LearningEntityTypeId = input.EntityTypeId;
					if ( output.LearningEntityTypeId > 0 )
					{
						CodeItem ct = CodesManager.Codes_EntityType_Get( output.LearningEntityTypeId );
						if ( ct != null && ct.Id > 0 )
						{
							output.LearningEntityType = ct.Title;
							output.CTDLTypeLabel = MapLearningEntityTypeLabel( output.LearningEntityTypeId );
							output.LearningTypeSchema = ct.SchemaName;
						}
						else
						{
							output.LearningEntityType = "Learning Opportunity";
							output.CTDLTypeLabel = "Learning Opportunity";
							output.LearningTypeSchema = "ceterms:LearningOpportunityProfile";
						}
					}
				}
            }

            return output;
        }
		public static ThisResource GetMinimumByGUID( Guid identifier )
		{
			ThisResource output = new ThisResource();
			using ( var context = new EntityContext() )
			{
				context.Configuration.LazyLoadingEnabled = false;
				DBResource input = context.LearningOpportunity
						.FirstOrDefault( s => s.RowId == identifier );

				if ( input != null && input.Id > 0 )
				{
					output.RowId = input.RowId;
					output.Id = input.Id;
					output.EntityStateId = ( int ) ( input.EntityStateId ?? 1 );
					output.EntityTypeId = input.EntityTypeId;
					output.Name = input.Name;
					output.LearningEntityTypeId = input.EntityTypeId;
					output.Description = input.Description;
					output.SubjectWebpage = input.SubjectWebpage;
					output.CTID = input.CTID;
					output.CredentialRegistryId = input.CredentialRegistryId;
					output.LearningEntityTypeId = input.EntityTypeId;
					if ( output.LearningEntityTypeId > 0 )
					{
						CodeItem ct = CodesManager.Codes_EntityType_Get( output.LearningEntityTypeId );
						if ( ct != null && ct.Id > 0 )
						{
							output.LearningEntityType = ct.Title;
							output.CTDLTypeLabel = MapLearningEntityTypeLabel( output.LearningEntityTypeId );
							output.LearningTypeSchema = ct.SchemaName;
						}
						else
						{
							output.LearningEntityType = "Learning Opportunity";
							output.CTDLTypeLabel = "Learning Opportunity";
							output.LearningTypeSchema = "ceterms:LearningOpportunityProfile";
						}
					}
				}
			}

			return output;
		}
		public static ThisResource GetBySubjectWebpage( string swp )
        {
            ThisResource output = new ThisResource();
            using ( var context = new EntityContext() )
            {
                context.Configuration.LazyLoadingEnabled = false;
                DBResource input = context.LearningOpportunity
                        .FirstOrDefault( s => s.SubjectWebpage.ToLower() == swp.ToLower() );

				{
					output.RowId = input.RowId;
					output.Id = input.Id;
					output.EntityStateId = ( int ) ( input.EntityStateId ?? 1 );
					output.EntityTypeId = input.EntityTypeId;
					output.Name = input.Name;
					output.LearningEntityTypeId = input.EntityTypeId;
					output.Description = input.Description;
					output.SubjectWebpage = input.SubjectWebpage;
					output.CTID = input.CTID;
					output.CredentialRegistryId = input.CredentialRegistryId;
					output.LearningEntityTypeId = input.EntityTypeId;
					if ( output.LearningEntityTypeId > 0 )
					{
						CodeItem ct = CodesManager.Codes_EntityType_Get( output.LearningEntityTypeId );
						if ( ct != null && ct.Id > 0 )
						{
							output.LearningEntityType = ct.Title;
							output.CTDLTypeLabel = MapLearningEntityTypeLabel( output.LearningEntityTypeId );
							output.LearningTypeSchema = ct.SchemaName;
						}
						else
						{
							output.LearningEntityType = "Learning Opportunity";
							output.CTDLTypeLabel = "Learning Opportunity";
							output.LearningTypeSchema = "ceterms:LearningOpportunityProfile";
						}
					}
				}
			}
            return output;
        }
		/// <summary>
		/// Used to with blank node references
		/// </summary>
		/// <param name="name"></param>
		/// <param name="swp"></param>
        /// <param name="codedNotation"></param>
        /// <param name="primaryAgentUId"></param>
		/// <returns>A found resource or an initialized resource</returns>
		public static ThisResource GetByName_SubjectWebpage(string name, string swp, string codedNotation, Guid primaryAgentUId )
		{
			ThisResource resource = new ThisResource();
			if (string.IsNullOrWhiteSpace(swp))
				return resource;
			if (swp.IndexOf("//") == -1)
				return resource;
			bool hasHttps = false;
			if (swp.ToLower().IndexOf("https:") > -1)
				hasHttps = true;
            var includingName = true;
			var nameMod = name.Replace( "&amp;", "and" );
			nameMod = nameMod.Replace( " & ", " and " );
			//swp = swp.Substring( swp.IndexOf( "//" ) + 2 );
			//swp = swp.ToLower().TrimEnd( '/' );
			var host = new Uri(swp).Host;
			var domain = host.Substring(host.LastIndexOf('.', host.LastIndexOf('.') - 1) + 1);
			//DBResource from = new DBResource();
			using (var context = new EntityContext())
			{
				//s.Name.ToLower() == name.ToLower() && 
				context.Configuration.LazyLoadingEnabled = false;
				var query = context.LearningOpportunity
					.Where( s => s.SubjectWebpage.ToLower().Contains( domain ) && s.EntityStateId > 1 );
                if ( includingName )
                {
					query = query.Where( s => s.Name.ToLower() == name.ToLower() || s.Name.ToLower() == nameMod.ToLower() );
				}
				if ( IsValidGuid( primaryAgentUId ) )
				{
					query = query.Where( s => s.OwningAgentUid == primaryAgentUId );
				}
				if ( !string.IsNullOrWhiteSpace( codedNotation ) )
				{
					//codedNotation filter should be exact, case insensitive
					query = query.Where( s => s.IdentificationCode.ToLower() == codedNotation.ToLower() );
				}
				var list = query.OrderByDescending( s => s.EntityStateId ).ThenBy( s => s.Name ).ToList();
				//var list = context.LearningOpportunity
				//		.Where(s => s.SubjectWebpage.ToLower().Contains(domain) && s.EntityStateId > 1)
				//		.OrderByDescending(s => s.EntityStateId)
				//		.ThenBy(s => s.Name)
				//		.ToList();
				int cntr = 0;

				ActivityManager amgr = new ActivityManager();
				var includingProperties = true;
                var includingProfiles = true;

                foreach (var from in list)
				{
					cntr++;
					//any way to check further?
					//the full org will be returned first
					//may want a secondary check and send notifications if additional full orgs found, or even if multiples are found.
					if ( from.Name.ToLower().Contains(name.ToLower()) 
                        || name.ToLower().Contains(from.Name.ToLower())
					)
					{
						//OK, take me
						if (cntr == 1 || resource.Id == 0)
						{
							//hmmm if input was https and found http, and a reference, should update to https!
							if (hasHttps && from.SubjectWebpage.StartsWith("http:"))
							{

							}
							//
							MapFromDB(from, resource, includingProperties, includingProfiles, false);
						}
						else
						{
							if (from.EntityStateId == 3)
							{
                                //could log warning conditions to activity log, and then report out at end of an import?
                                amgr.SiteActivityAdd( new SiteActivity()
                                {
                                    ActivityType = "System",
                                    Activity = "Import",
                                    Event = $"{EntityType} Reference Check",
                                    Comment = $"{Entity_Label} Get by Name and subject webpage. Found additional full {EntityType} for name: {name}, swp: {swp}. First {EntityType}: {resource.Name} ({resource.Id})"
                                } );

                            }
                            MapFromDB( from, resource, includingProperties, includingProfiles, false );
                            break;
						}
					}
				}
			}

			return resource;
			//using (var context = new EntityContext())
			//{
			//    context.Configuration.LazyLoadingEnabled = false;
			//    DBResource from = context.LearningOpportunity
			//            .FirstOrDefault(s => s.Name.ToLower() == name.ToLower() && s.SubjectWebpage == swp);

			//    if (from != null && from.Id > 0)
			//    {
			//        //entity.RowId = from.RowId;
			//        //entity.Id = from.Id;
			//        //entity.Name = from.Name;
			//        //entity.EntityStateId = ( int )( from.EntityStateId ?? 1 );
			//        //entity.Description = from.Description;
			//        //entity.SubjectWebpage = from.SubjectWebpage;

			//        //entity.CTID = from.CTID;
			//        //entity.CredentialRegistryId = from.CredentialRegistryId;
			//        MapFromDB(from, entity,
			//            true, //includingProperties
			//            true,
			//            true);
			//    }
			//}
			//return entity;
		}
        /// <summary>
        /// Lopp look up for blank node.
        /// </summary>
        /// <param name="name">Required</param>
        /// <param name="codedNotation">Optional</param>
        /// <param name="primaryAgentUId">Should be required, but may not be available. If not here, probably return?</param>
        /// <returns></returns>
        public static ThisResource FindReferenceResource( string name, string codedNotation, Guid primaryAgentUId )
        {
            ThisResource resource = new ThisResource();
			var nameMod = name.Replace( "&amp;", "and" );
			nameMod = nameMod.Replace( " & ", " and " );
			using ( var context = new EntityContext() )
            {

                context.Configuration.LazyLoadingEnabled = false;

                var query = context.LearningOpportunity
                    .Where( s => s.EntityStateId > 1 && s.OwningAgentUid == primaryAgentUId );
				query = query.Where( s => s.Name.ToLower() == name.ToLower() || s.Name.ToLower() == nameMod.ToLower() );
				if ( !string.IsNullOrWhiteSpace( codedNotation ) )
                {
					//codedNotation filter should be exact, case insensitive
					query = query.Where( s => s.IdentificationCode.ToLower() == codedNotation.ToLower() );
                }
                var list = query.OrderByDescending( s => s.EntityStateId ).ThenBy( s => s.Name ).ToList();
                int cntr = 0;

                ActivityManager amgr = new ActivityManager();
                var includingProperties = true;
                var includingProfiles = true;

                foreach ( var from in list )
                {
                    cntr++;
					//24-01-04 mp - may need to be more precise on name
					if ( from.Name.ToLower().Contains( name.ToLower() ) || name.ToLower().Contains( from.Name.ToLower() ) )
                    {
                        if ( cntr == 1 || resource.Id == 0 )
                        {
                            MapFromDB( from, resource, includingProperties, includingProfiles, false );
                        }
                        else
                        {
                            if ( from.EntityStateId == 3 )
                            {
                                amgr.SiteActivityAdd( new SiteActivity()
                                {
                                    ActivityType = "System",
                                    Activity = "Import",
                                    Event = $"{EntityType} Reference Check",
                                    Comment = $"{Entity_Label} Get by Name and CodedNotation. Found additional full {EntityType} for name: {name}, CodedNotation: {codedNotation}. First {EntityType}: {resource.Name} ({resource.Id})"
                                } );
                            }
                            MapFromDB( from, resource, includingProperties, includingProfiles, false );
                            break;
                        }
                    }
                }
            }

            return resource;
        }

        public static ThisResource GetForDetail( int id, bool isAPIRequest = false, bool formattingFullVersion = true )
        {
            ThisResource entity = new ThisResource();
            var includingProperties = true;
            bool includingProfiles = true;

            using ( var context = new EntityContext() )
            {
                //context.Configuration.LazyLoadingEnabled = false;
                DBResource item = context.LearningOpportunity
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
					//21-07-12 mp - determine if there is a difference between detail and API now
					MapFromDB( item, entity, includingProperties, includingProfiles, isAPIRequest, formattingFullVersion );
                }
            }

            return entity;
        }

        /// <summary>
        /// Get absolute minimum for display in lists, etc
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static ThisResource GetBasic( int id )
        {
            ThisResource entity = new ThisResource();

            using ( var context = new EntityContext() )
            {
                context.Configuration.LazyLoadingEnabled = false;
                DBResource item = context.LearningOpportunity
                        .FirstOrDefault( s => s.Id == id );

                if ( item != null && item.Id > 0 )
                {
                    MapFromDB_Basic( item, entity );
                }
            }

            return entity;
        }

        public static List<ThisResource> GetAllForOwningOrg( Guid owningOrgUid, ref int totalRecords, int maxRecords = 100 )
        {
            List<ThisResource> list = new List<ThisResource>();
            ThisResource entity = new ThisResource();
            using ( var context = new EntityContext() )
            {
                List<DBResource> results = context.LearningOpportunity
                             .Where( s => s.OwningAgentUid == owningOrgUid )
                             .OrderBy( s => s.Name )
                             .ToList();
                if ( results != null && results.Count > 0 )
                {
					totalRecords = results.Count();

					foreach ( DBResource item in results )
                    {
                        entity = new ThisResource();
                        MapFromDB_Basic( item, entity );

                        list.Add( entity );
						if ( maxRecords > 0 && list.Count >= maxRecords )
							break;
                    }
                }
            }

            return list;
        }
		public static List<ThisResource> GetAll( ref int totalRecords, int maxRecords = 100 )
		{
			List<ThisResource> list = new List<ThisResource>();
			ThisResource entity = new ThisResource();
			using ( var context = new EntityContext() )
			{
				List<DBResource> results = context.LearningOpportunity
							 .Where( s => s.EntityStateId > 2 )
							 .OrderBy( s => s.Name )
							 .ToList();
				if ( results != null && results.Count > 0 )
				{
					totalRecords = results.Count();

					foreach ( DBResource item in results )
					{
						entity = new ThisResource();
						MapFromDB_Basic( item, entity );

						list.Add( entity );
						if ( maxRecords > 0 && list.Count >= maxRecords )
							break;
					}
				}
			}

			return list;
		}

		public static ThisResource GetAs_IsPartOf( Guid rowId )
        {
            ThisResource entity = new ThisResource();

            using ( var context = new EntityContext() )
            {
                //	REVIEW	- seems like will need to almost always bubble up costs
                //			- just confirm that this method is to simply list parent Lopps
                context.Configuration.LazyLoadingEnabled = false;

                DBResource item = context.LearningOpportunity
                        .FirstOrDefault( s => s.RowId == rowId );

                if ( item != null && item.Id > 0 )
                {
                    MapFromDB_Basic( item, entity );
                }
            }

            return entity;
        }

		public static List<string> Autocomplete( string pFilter, int pageNumber, int pageSize, ref int pTotalRows )
        {
            bool autocomplete = true;
            var results = new List<string>();
            List<string> competencyList = new List<string>();
            //get minimal entity
            List<ThisResource> list = Search( pFilter, string.Empty, pageNumber, pageSize, ref pTotalRows, ref competencyList, autocomplete );
            bool appendingOrgNameToAutocomplete = UtilityManager.GetAppKeyValue( "appendingOrgNameToAutocomplete", false );
            string prevName = string.Empty;
            foreach ( LearningOpportunityProfile item in list )
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
            List<string> competencyList = new List<string>();
            return Search( pFilter, pOrderBy, pageNumber, pageSize, ref pTotalRows, ref competencyList, autocomplete );
        }
        public static List<ThisResource> Search( string pFilter, string pOrderBy, int pageNumber, int pageSize,
            ref int pTotalRows,
            ref List<string> competencyList,
            bool autocomplete = false )
        {
            string connectionString = DBConnectionRO();
            ThisResource item = new ThisResource();
            List<ThisResource> list = new List<ThisResource>();
            var result = new DataTable();
            string temp = string.Empty;
            string org = string.Empty;
            int orgId = 0;
            int cntr = 0;
            pTotalRows = 0;
            using ( SqlConnection c = new SqlConnection( connectionString ) )
            {
                c.Open();

                if ( string.IsNullOrEmpty( pFilter ) )
                {
                    pFilter = string.Empty;
                }

                using ( SqlCommand command = new SqlCommand( "[LearningOpportunity_Search]", c ) )
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add( new SqlParameter( "@Filter", pFilter ) );
                    command.Parameters.Add( new SqlParameter( "@SortOrder", pOrderBy ) );
                    command.Parameters.Add( new SqlParameter( "@StartPageIndex", pageNumber ) );
                    command.Parameters.Add( new SqlParameter( "@PageSize", pageSize ) );
					command.CommandTimeout = 300;
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
						item = new ThisResource();
						item.Name = "EXCEPTION ENCOUNTERED - " + ex.Message;
						item.Description = ex.Message;
						list.Add( item );
                        return list;
					}
                }
                try
                {
                    
                    foreach ( DataRow dr in result.Rows )
                    {
                        cntr++;
						item = new ThisResource
						{
							Id = GetRowColumn( dr, "Id", 0 ),
							Name = GetRowColumn( dr, "Name", "missing" )
						};
						item.FriendlyName = FormatFriendlyTitle( item.Name );
                        item.CTID = GetRowPossibleColumn( dr, "CTID", string.Empty );
                        item.EntityStateId = GetRowColumn( dr, "EntityStateId", 3 );
                        item.Description = GetRowColumn( dr, "Description", string.Empty );

                        string rowId = GetRowColumn( dr, "RowId" );
                        if ( IsValidGuid( rowId ) )
                        {
                            item.RowId = new Guid( rowId );
                        }
                        item.SubjectWebpage = GetRowColumn( dr, "SubjectWebpage", string.Empty );
                        item.AvailableOnlineAt = GetRowPossibleColumn( dr, "AvailableOnlineAt", string.Empty );

                        //if autocomplete, skip
                        if ( autocomplete )
                        {
                            list.Add( item );
                            continue;
                        }
                        item.CredentialRegistryId = GetRowPossibleColumn( dr, "CredentialRegistryId", string.Empty );

                        item.CodedNotation = GetRowColumn( dr, "IdentificationCode", string.Empty );

                        org = GetRowPossibleColumn( dr, "Organization", string.Empty );
                        orgId = GetRowPossibleColumn( dr, "OrgId", 0 );
                        if ( orgId > 0 )
                        {
                            item.PrimaryOrganization = new Organization() { Id = orgId, Name = org };
                        }
                        item.ListTitle = item.Name + " (" + item.OrganizationName + ")";
                        //
                        temp = GetRowColumn( dr, "DateEffective", string.Empty );
                        if ( IsValidDate( temp ) )
                            item.DateEffective = DateTime.Parse( temp ).ToString( "yyyy-MM-dd" );
                        else
                            item.DateEffective = string.Empty;

                        item.Created = GetRowColumn( dr, "Created", System.DateTime.MinValue );
                        item.LastUpdated = GetRowColumn( dr, "LastUpdated", System.DateTime.MinValue );

                        item.RequiresCount = GetRowColumn( dr, "RequiresCount", 0 );
                        item.RecommendsCount = GetRowColumn( dr, "RecommendsCount", 0 );
                        item.RequiredForCount = GetRowColumn( dr, "IsRequiredForCount", 0 );
                        item.IsRecommendedForCount = GetRowColumn( dr, "IsRecommendedForCount", 0 );
                        item.IsAdvancedStandingForCount = GetRowColumn( dr, "IsAdvancedStandingForCount", 0 );
                        item.AdvancedStandingFromCount = GetRowColumn( dr, "AdvancedStandingFromCount", 0 );
                        item.PreparationForCount = GetRowColumn( dr, "IsPreparationForCount", 0 );
                        item.PreparationFromCount = GetRowPossibleColumn( dr, "isPreparationFromCount", 0 );
                        item.QualityAssurance = Fill_AgentRelationship( dr, "QualityAssurance", CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false, true );

                        //addressess
                        int addressess = GetRowPossibleColumn( dr, "AvailableAddresses", 0 );
                        if ( addressess > 0 )
                        {
                            item.AvailableAt = Entity_AddressManager.GetAll( item.RowId );
                        }
                        //competencies. either arbitrarily get all, or if filters exist, only return matching ones
                        item.CompetenciesCount = GetRowPossibleColumn( dr, "Competencies", 0 );
                        if ( item.CompetenciesCount > 0 )
                        {
                            //handled in search services
                            //FillCompetencies( item, ref competencyList );
                        }
                        var hasAgentRelationships = GetRowPossibleColumn( dr, "HasAgentRelationshipsForEntity", string.Empty );
                        item.HasCachedAgentRelationships = hasAgentRelationships == "yes" ? true : false;
						var hasResourceDetail = GetRowPossibleColumn( dr, "HasResourceDetail", string.Empty );
						item.HasCachedResourceDetail = hasResourceDetail == "yes" ? true : false;

						list.Add( item );
                    }
                }

                catch ( Exception ex )
                {
                    var msg = FormatExceptions( ex );
                    LoggingHelper.DoTrace( 2, string.Format( thisClassName + ".Search. Last Row: {0}, LoppId: {1} Exception: \r\n{2}", cntr, item.Id, msg ) );
                }
                finally
                {

                    LoggingHelper.DoTrace( 2, string.Format( thisClassName + ".Search - Page: {0} Complete loaded {1} records", pageNumber, cntr ) );
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

            if ( !string.IsNullOrWhiteSpace( input.CredentialRegistryId ) )
                output.CredentialRegistryId = input.CredentialRegistryId;
            output.Id = input.Id;
            output.Name = AssignLimitedString( input.Name, 800 );
			//what to do if learning entity type changed?
			output.EntityTypeId = input.LearningEntityTypeId > 0 ? input.LearningEntityTypeId : 7;

			output.Description = GetData( input.Description );
			output.LifeCycleStatusTypeId = input.LifeCycleStatusTypeId;
			if ( IsGuidValid( input.PrimaryAgentUID ) )
            {
                if ( output.Id > 0 && output.OwningAgentUid != input.PrimaryAgentUID )
                {
                    if ( IsGuidValid( output.OwningAgentUid ) )
                    {
                        //need output remove the owner role, or could have been others
                        string statusMessage = string.Empty;
                        new Entity_AgentRelationshipManager().Delete( output.RowId, output.OwningAgentUid, Entity_AgentRelationshipManager.ROLE_TYPE_OWNER, ref statusMessage );
                    }
                }
                output.OwningAgentUid = input.PrimaryAgentUID;
                //get for use to add to elastic pending
                input.PrimaryOrganization = OrganizationManager.GetForSummary( input.PrimaryAgentUID );

            }
            else
            {
                //always have output have an owner
                //output.OwningAgentUid = null;
            }

            output.SubjectWebpage = GetUrlData( input.SubjectWebpage, null );
            output.IdentificationCode = GetData( input.CodedNotation );
			//only true should be published. Ensure the save only saves True
			if ( input.IsNonCredit != null && input.IsNonCredit == true )
				output.IsNonCredit = input.IsNonCredit;
			else
				output.IsNonCredit = null;
			output.SCED = GetData( input.SCED );
			output.AvailableOnlineAt = GetUrlData( input.AvailableOnlineAt, null );
			output.InCatalog = GetUrlData( input.InCatalog );

			output.AvailabilityListing = GetUrlData( input.AvailabilityListing, null );
            output.DeliveryTypeDescription = input.DeliveryTypeDescription;
			//output.VerificationMethodDescription = input.VerificationMethodDescription;
			//
			output.AssessmentMethodDescription = input.AssessmentMethodDescription;
			output.LearningMethodDescription = input.LearningMethodDescription;
			if ( input.TargetLearningResource != null && input.TargetLearningResource.Any() )
			{
				output.TargetLearningResource = JsonConvert.SerializeObject( input.TargetLearningResource, JsonHelper.GetJsonSettings() );
			}
			else
				output.TargetLearningResource = null;
            //
            output.SupersededBy = input.SupersededBy;
            output.Supersedes = input.Supersedes;
            if ( IsValidDate( input.DateEffective ) )
                output.DateEffective = DateTime.Parse( input.DateEffective );
            else
                output.DateEffective = null;
			if ( IsValidDate( input.ExpirationDate ) )
				output.ExpirationDate = DateTime.Parse( input.ExpirationDate );
			else
				output.ExpirationDate = null;
			//if ( input.InLanguageId > 0 )
			//    output.InLanguageId = input.InLanguageId;
			//else if ( !string.IsNullOrWhiteSpace( input.InLanguage ) )
			//{
			//    output.InLanguageId = CodesManager.GetLanguageId( input.InLanguage );
			//}
			//else
			output.InLanguageId = null;

			//======================================================================
			// can have just CreditUnitTypeDescription.Will need a policy if both are found?
			//	-possibly create a second CreditValue?
			output.CreditUnitTypeDescription = null;
			if ( !string.IsNullOrWhiteSpace( input.CreditUnitTypeDescription ) )
				output.CreditUnitTypeDescription = input.CreditUnitTypeDescription;
			//output.CreditValueJson = null;

			//=========================================================
			//21-03-23 - now using ValueProfile

			//***actually may have to fill out credit units etc?
			if ( input.CreditValueJson == "[]" )
				input.CreditValueJson = null;
			output.CreditValue = string.IsNullOrWhiteSpace( input.CreditValueJson ) ? null : input.CreditValueJson;

			if ( IsValidDate( input.LastUpdated ) )
                output.LastUpdated = input.LastUpdated;

        }
		public static void MapFromDB( DBResource input, ThisResource output,
				bool includingProperties = false,
				bool includingProfiles = true,
				bool isAPIRequest = false,
                bool formattingFullVersion = true )
		{

			//TODO add a tomap basic, and handle for lists
			MapFromDB_Basic( input, output, formattingFullVersion );
			//duplicate?
			//var relatedEntity = EntityManager.GetEntity( output.RowId, false );

			output.AvailableAt = Entity_AddressManager.GetAll( output.RowId );

			output.CodedNotation = input.IdentificationCode;
			output.SCED = input.SCED;

			output.DeliveryTypeDescription = input.DeliveryTypeDescription;

			//
			output.AssessmentMethodDescription = input.AssessmentMethodDescription;
			output.LearningMethodDescription = input.LearningMethodDescription;
			if ( !string.IsNullOrWhiteSpace( input.TargetLearningResource ) && input.TargetLearningResource != "null" && input.TargetLearningResource.IndexOf( "\"null") == -1 )
			{
				output.TargetLearningResource = JsonConvert.DeserializeObject<List<string>>( input.TargetLearningResource );
			}
			//=========================================================

			try
			{
				output.DegreeConcentration = Entity_ReferenceManager.GetAll( output.RowId, CodesManager.PROPERTY_CATEGORY_DEGREE_CONCENTRATION );

				output.EstimatedDuration = DurationProfileManager.GetAll( output.RowId );
				//=========================================================
				//21-03-23 - now using ValueProfile

				if ( !string.IsNullOrWhiteSpace( input.CreditValue ) && input.CreditValue!= "[]" )
				{
					output.CreditValue = JsonConvert.DeserializeObject<List<ValueProfile>>( input.CreditValue );
				} 
				output.CreditUnitTypeDescription = input.CreditUnitTypeDescription;

				// Begin edits - Need these output populate Credit Unit Type -  NA 3/31/2017
				//20-11-30 mp - is this still applicable???
				//if ( output.CreditUnitTypeId > 0 )
				//{
				//	output.CreditUnitType = new Enumeration();
				//	var match = CodesManager.GetEnumeration( "creditUnit" ).Items.FirstOrDefault( m => m.CodeId == output.CreditUnitTypeId );
				//	if ( match != null )
				//	{
				//		output.CreditUnitType.Items.Add( match );
				//	}
				//}
				if ( IsValidDate( input.DateEffective ) )
					output.DateEffective = ( ( DateTime )input.DateEffective ).ToString("yyyy-MM-dd");
				else
					output.DateEffective = string.Empty;
				if ( IsValidDate( input.ExpirationDate ) )
					output.ExpirationDate = ( ( DateTime )input.ExpirationDate ).ToString("yyyy-MM-dd");
				else
					output.ExpirationDate = string.Empty;
				if (includingProperties)
				{
					//teaches
                    if ( formattingFullVersion )
					    MapFromDB_Competencies( output );

					//20-10-05 - get as much as possible for a reference esp for TVP case
                    //23-02-15 mp - TODO consider one call to the db and then assign?
					output.AudienceType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE );
					output.AudienceLevelType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL );

					output.DeliveryType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE );
					output.LearningMethodType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_Learning_Method_Type );
					output.AssessmentMethodType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_Assessment_Method_Type );

					output.ScheduleTimingType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_SCHEDULE_TIMING );

					output.ScheduleFrequencyType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_SCHEDULE_FREQUENCY );

					output.OfferFrequencyType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_OFFER_FREQUENCY );

					//23-02-15 mp - TODO consider one call to the db and then assign?
					var allReferences = Entity_ReferenceManager.GetAll( output.RowId );
                    if ( allReferences != null && allReferences.Any()) 
                    {
                        output.Keyword = allReferences.Where( r => r.CategoryId == CodesManager.PROPERTY_CATEGORY_KEYWORD ).OrderBy( s => s.TextValue ).ToList();
                        output.Subject = allReferences.Where( r => r.CategoryId == CodesManager.PROPERTY_CATEGORY_SUBJECT ).OrderBy( s => s.TextValue ).ToList();
                        output.SameAs = allReferences.Where( r => r.CategoryId == CodesManager.PROPERTY_CATEGORY_SAME_AS ).OrderBy( s => s.TextValue ).ToList();
                        output.InLanguageCodeList = allReferences.Where( r => r.CategoryId == CodesManager.PROPERTY_CATEGORY_LANGUAGE ).OrderBy( s => s.TextValue ).ToList();
                    }
                    //               output.Keyword = Entity_ReferenceManager.GetAll( output.RowId, CodesManager.PROPERTY_CATEGORY_KEYWORD );
                    //output.Subject = Entity_ReferenceManager.GetAll( output.RowId, CodesManager.PROPERTY_CATEGORY_SUBJECT );
                    //output.SameAs = Entity_ReferenceManager.GetAll( input.RowId, CodesManager.PROPERTY_CATEGORY_SAME_AS ); 
                    output.SupersededBy = input.SupersededBy;
                    output.Supersedes = input.Supersedes;
				}

				//put outside, just in case
				//Include currencies output fix null errors in various places (detail, compare, publishing) - NA 3/17/2017
				var currencies = CodesManager.GetCurrencies();

				if ( includingProfiles )
				{
                    if ( formattingFullVersion )
					output.Region = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( output.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_RESIDENT );

					//
					//Fix costs
					output.EstimatedCost = CostProfileManager.GetAll( output.RowId );
					output.FinancialAssistance = Entity_FinancialAssistanceProfileManager.GetAll( output.RowId, false );

                    if ( formattingFullVersion )
					    output.Jurisdiction = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( output.RowId );
					
					//Include cost types output fix other null errors - NA 3/31/2017
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

                    output.HasScheduledOffering = Entity_HasOfferingManager.GetAll( output.RowId );


                }

            }
			catch(Exception ex)
			{
				LoggingHelper.LogError( ex, thisClassName + string.Format("MapfromDB(). Section 1. Name: {0}, Id: {1}, CTID: {2}", output.Name, output.Id, output.CTID) );
			}


            output.CredentialRegistryId = input.CredentialRegistryId;
			//====			

			try
			{

                if ( formattingFullVersion )
                {
                    //assumes only one identifier type per class
                    //20-12-01 now two types - will need a property designatiion, or store as json
                    output.VersionIdentifierList = Entity_IdentifierValueManager.GetAll( output.RowId, Entity_IdentifierValueManager.IdentifierValue_VersionIdentifier );

                    output.Identifier = Entity_IdentifierValueManager.GetAll( output.RowId, Entity_IdentifierValueManager.IdentifierValue_Identifier );
                }
				//properties
				output.WhereReferenced = new List<string>();
				if ( includingProfiles )
				{
					//get condition profiles
					List<ConditionProfile> list = new List<ConditionProfile>();
                    if ( formattingFullVersion )
                    {
                        //will need this for compare
                        list = Entity_ConditionProfileManager.GetAll( output.RowId, false );
                        if ( list != null && list.Count > 0 )
                        {
                            foreach ( ConditionProfile item in list )
                            {
                                if ( item.ConditionSubTypeId == Entity_ConditionProfileManager.ConditionSubType_LearningOpportunity )
                                {
                                    output.LearningOppConnections.Add( item );
                                }
                                else if ( item.ConnectionProfileTypeId == Entity_ConditionProfileManager.ConnectionProfileType_Requirement )
                                    output.Requires.Add( item );
                                else if ( item.ConnectionProfileTypeId == Entity_ConditionProfileManager.ConnectionProfileType_Recommendation )
                                    output.Recommends.Add( item );
                                else if ( item.ConnectionProfileTypeId == Entity_ConditionProfileManager.ConnectionProfileType_Corequisite )
                                    output.Corequisite.Add( item );
                                else if ( item.ConnectionProfileTypeId == Entity_ConditionProfileManager.ConnectionProfileType_CoPrerequisite )
                                    output.CoPrerequisite.Add( item );
                                else if ( item.ConnectionProfileTypeId == Entity_ConditionProfileManager.ConnectionProfileType_EntryCondition )
                                    output.EntryCondition.Add( item );
                                else
                                {
                                    EmailManager.NotifyAdmin( "Unexpected Condition Profile for learning opportunity", string.Format( "LearningOppId: {0}, ConditionProfileTypeId: {1}", output.Id, item.ConnectionProfileTypeId ) );

                                    //add output required, for dev only?
                                    if ( IsDevEnv() )
                                    {
                                        item.ProfileName = ( item.ProfileName ?? string.Empty ) + " unexpected condition type of " + item.ConnectionProfileTypeId.ToString();
                                        output.Requires.Add( item );
                                    }
                                }
                            }
                        }
                    }
                    //
                    output.TargetAssessment = Entity_AssessmentManager.GetAll( output.RowId, BaseFactory.RELATIONSHIP_TYPE_HAS_TARGET_RESOURCE, true, true );
                    foreach ( AssessmentProfile ap in output.TargetAssessment )
                    {
                        if ( ap.HasCompetencies || ap.ChildHasCompetencies )
                        {
                            output.ChildHasCompetencies = true;
                            break;
                        }
                    }
                    //

                    if ( input.Entity_LearningOpportunity != null && input.Entity_LearningOpportunity.Count > 0 )
					{
						//the Entity_LearningOpportunity could be for a parent lopp, or a condition profile
						foreach ( EM.Entity_LearningOpportunity item in input.Entity_LearningOpportunity )
						{
							TopLevelObject tlo = new TopLevelObject();
							if ( item.Entity == null )
							{
								//shouldn't happen? - log?
								continue;
							}
							//
							output.WhereReferenced.Add( string.Format( "EntityUid: {0}, Type: {1}", item.Entity.EntityUid, item.Entity.Codes_EntityTypes.Title ) );
							if ( item.Entity.EntityTypeId == CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE )
							{
								//only if downloaded
								//Hmm this can be hasPart, isPartOf, or targetLopp?
								//hasPart is retrieved separately, so should be ignored here or later
								var lo = GetAs_IsPartOf( item.Entity.EntityUid );
								if ( lo.EntityStateId > 1 )
								{
									if ( item.RelationshipTypeId == BaseFactory.RELATIONSHIP_TYPE_IS_PART_OF )
									{
										output.IsPartOf.Add( lo );
									}
									else if ( item.RelationshipTypeId == BaseFactory.RELATIONSHIP_TYPE_HAS_PART )
									{
										//ignore for now
									}
									if ( item.RelationshipTypeId == BaseFactory.RELATIONSHIP_TYPE_TARGET_LOPP )
									{
										//where to put this. not clear if will be used
										output.TargetLearningOpportunity.Add( lo );
									} else
									{
										output.IsPartOf.Add( lo );
									}
								}
							}//or better as ENTITY_TYPE_CONDITION_PROFILE
							else if ( item.Entity.EntityTypeId == CodesManager.ENTITY_TYPE_CONNECTION_PROFILE )
							{
								//21-09-07 mp - may need to pass in the connections so duplicate checks can be done generically.
								ConditionProfile cp = CondProfileMgr.GetAs_IsPartOf( item.Entity.EntityUid, output.Name, output.LearningOppConnections );
								//first check if the parent (ex credential) is already in a condition profile - with inverse relationship type
								if ( isAPIRequest ) //skip if not API context-actually may not be needed at all.
									output.IsPartOfConditionProfile.Add( cp );
								//now done in GetAs_IsPartOf
								//output.LearningOppConnections.Add( cp );
								//need output check cond prof for parent of credential
								//will need output ensure no dups, or realistically, don't do the direct credential check
								//or do this earlier and add to 'output.LearningOppConnections '
								if ( cp.ParentCredential != null && cp.ParentCredential.Id > 0 )
								{
									if ( cp.ParentCredential.EntityStateId > 1 )
									{
										//will not need this for API context
										AddCredentialReference( cp.ParentCredential.Id, output );
									}
								}
								else if ( cp.ParentLearningOpportunity != null && cp.ParentLearningOpportunity.Id > 0 )
								{
									if ( cp.ParentLearningOpportunity.EntityStateId > 1 )
									{
										//should not need to store - but may use to set default content for the condition profile
										var exists = output.IsPartOfLearningOpp.FirstOrDefault( s => s.Id == cp.ParentLearningOpportunity.Id );
										//hmm  would be useful to know how connected for display purposes  -and to format the cp
										if ( exists == null || exists.Id == 0 )
										{
											tlo = GetBasic( cp.ParentLearningOpportunity.Id );
											output.IsPartOfLearningOpp.Add( GetBasic( cp.ParentLearningOpportunity.Id ) );
										}
										//
									}
								}
							}
							else if ( item.Entity.EntityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL )
							{
								//this is not possible in the finder
								//AddCredentialReference( (int)item.Entity.EntityBaseId, output );
							}
							else if ( item.Entity.EntityTypeId == CodesManager.ENTITY_TYPE_DATASET_PROFILE )
							{
								//so should these be kept separate?
								var dsp = DataSetProfileManager.Get( ( int ) item.Entity.EntityBaseId, true, isAPIRequest );
								if ( dsp != null && dsp.Id > 0 && dsp.EntityStateId == 3 )
                                {
									//need to exclude if already part of the aggregateProfile data. 
									var exists = output.AggregateData.Where( s =>
												s.RelevantDataSet.Exists(z => 
													z.CTID == dsp.CTID)).ToList();
									//actually ProPath has lots of dups
									//23-02-10 mp - uncommented the following if to exclude dsps that are in an adp. 
									if (exists == null || exists.Count == 0)
										output.ExternalDataSetProfiles.Add( dsp );
								}

							}
							else if ( item.Entity.EntityTypeId == CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE )
							{
								//so should these be kept separate?
								//get minimum
								var tvp = TransferValueProfileManager.Get( ( int ) item.Entity.EntityBaseId, false );
								if ( tvp != null && tvp.Id > 0 )
									output.HasTransferValueProfile.Add( tvp );

							}
							else if ( item.Entity.EntityTypeId == CodesManager.ENTITY_TYPE_PATHWAY_COMPONENT )
							{
								//need to get the pathway
								var pc = PathwayComponentManager.Get( ( int ) item.Entity.EntityBaseId, 1 );
								if ( pc != null && pc.Id > 0 )
								{
									var pathway = PathwayManager.GetByCtid( pc.PathwayCTID );
									if ( pathway != null && pathway.Id > 0 && pathway.EntityStateId > 2 )
										output.TargetPathway.Add( pathway );
								}

							}
						}
					}

                    //
                    if ( formattingFullVersion )
                    {
                        output.CollectionMembers = CollectionMemberManager.GetMemberOfCollections( output.CTID );
                        if ( output.CollectionMembers != null && output.CollectionMembers.Count > 0 )
                        {
                            //not sure what we will do yet
                            foreach ( var item in output.CollectionMembers )
                            {

                            }
                        }
                    }
                    //
                    if ( formattingFullVersion )
                    {
                        //DisambiguateConditionProfiles actually returns a split object, so why call 6 times
                        var splitConnections = ConditionManifestExpanded.DisambiguateConditionProfiles( output.LearningOppConnections );
                        output.AdvancedStandingFrom = splitConnections.AdvancedStandingFrom;
                        output.IsAdvancedStandingFor = splitConnections.IsAdvancedStandingFor;
                        output.IsRequiredFor = splitConnections.IsRequiredFor;
                        output.IsRecommendedFor = splitConnections.IsRecommendedFor;
                        output.IsPreparationFor = splitConnections.IsPreparationFor;
                        output.PreparationFrom = splitConnections.PreparationFrom;
                    }

                    output.RelatedAction = CredentialingActionManager.GetRelatedActionFromObject( output.RowId );

                    //TODO
                    output.CommonConditions = Entity_CommonConditionManager.GetAll( output.RowId );
					output.CommonCosts = Entity_CommonCostManager.GetAll( output.RowId );

                    //can we get all and then split
                    var rfi = ReferenceFrameworkItemsManager.FillCredentialAlignmentObject( output.RowId );
                    if (rfi != null && rfi.Any()) 
                    {
                        output.OccupationTypes = rfi.Where( r => r.CategoryId == CodesManager.PROPERTY_CATEGORY_SOC ).ToList();
                        output.IndustryTypes = rfi.Where( r => r.CategoryId == CodesManager.PROPERTY_CATEGORY_NAICS ).ToList();
                        output.InstructionalProgramTypes = rfi.Where( r => r.CategoryId == CodesManager.PROPERTY_CATEGORY_CIP ).ToList();
                    }
     //               output.OccupationTypes = ReferenceFrameworkItemsManager.FillCredentialAlignmentObject( output.RowId, CodesManager.PROPERTY_CATEGORY_SOC );
					//output.IndustryTypes = ReferenceFrameworkItemsManager.FillCredentialAlignmentObject( output.RowId, CodesManager.PROPERTY_CATEGORY_NAICS );
					//output.InstructionalProgramTypes = ReferenceFrameworkItemsManager.FillCredentialAlignmentObject( output.RowId, CodesManager.PROPERTY_CATEGORY_CIP );

                    //OLD
                    //output.Occupation = ReferenceFrameworkItemsManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_SOC );		
                    //output.Industry = ReferenceFrameworkItemsManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_NAICS );
                    //output.InstructionalProgramType = ReferenceFrameworkItemsManager.FillEnumeration(output.RowId, CodesManager.PROPERTY_CATEGORY_CIP);
                    //21-07-22 mparsons - TargetPathway is a list of pathways were this record exists anywhere in the pathway
                    //						NO- lopp don't publish has pathay, the relationship comes from the pathway component.
                    //output.TargetPathway = Entity_PathwayManager.GetAll( output.RowId );

                    if ( formattingFullVersion )
                    {
                        output.JurisdictionAssertions = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( output.RowId, 3 );
                        bool forProfilesList = false;
                        MapFromDB_HasPart( output, forProfilesList );

                        MapFromDB_PreRequisite( output, forProfilesList );
                    }
				}

			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + string.Format( "MapfromDB(). Section 2. Name: {0}, Id: {1}, CTID: {2}", output.Name, output.Id, output.CTID ) );
			}



		}

        public static void MapFromDB_Basic( DBResource input, ThisResource output, bool formattingFullVersion = true )
        {
            output.Id = input.Id;
            output.RowId = input.RowId;
            output.CTID = input.CTID;
            output.EntityStateId = ( int ) ( input.EntityStateId ?? 1 );
            //TBD - watch this doesn't get changed somewhere
            //*********** need to update the trigger that creates the Entity ********************
            output.LearningEntityTypeId = input.EntityTypeId;
            if ( output.LearningEntityTypeId > 0 )
            {
                CodeItem ct = CodesManager.Codes_EntityType_Get( output.LearningEntityTypeId );
                if ( ct != null && ct.Id > 0 )
                {
                    output.LearningEntityType = ct.Title;
                    output.CTDLTypeLabel = MapLearningEntityTypeLabel( output.LearningEntityTypeId );
                    output.LearningTypeSchema = ct.SchemaName;
                }
                else
                {
                    output.LearningEntityType = "Learning Opportunity";
                    output.CTDLTypeLabel = "Learning Opportunity";
                    output.LearningTypeSchema = "ceterms:LearningOpportunityProfile";
                }
            }

            output.Name = input.Name;
            output.FriendlyName = FormatFriendlyTitle( input.Name );
			output.NamePlusOrganization = output.Name;
			output.Description = input.Description;
            output.SubjectWebpage = input.SubjectWebpage;
            output.CodedNotation = input.IdentificationCode;
            if ( string.IsNullOrWhiteSpace( output.CTID ) || output.EntityStateId < 3 )
                output.IsReferenceVersion = true;
            //
            bool havePrimaryOrg = false;
            var orgRoleManager = new OrganizationRoleManager();
            if ( IsGuidValid( input.OwningAgentUid ) )
            {
                output.PrimaryAgentUID = ( Guid ) input.OwningAgentUid;
                output.PrimaryOrganization = OrganizationManager.GetForSummary( output.PrimaryAgentUID );
                havePrimaryOrg = true;
				output.NamePlusOrganization = output.Name + $" ( {output.PrimaryOrganization.Name} )";
				//get roles for the primary agent on this record
				OrganizationRoleProfile orp = Entity_AgentRelationshipManager.AgentEntityRole_GetAsEnumerationFromCSV( output.RowId, output.PrimaryAgentUID );
                output.OwnerRoles = orp.AgentRole;
                //
                if ( output.OwningOrganizationId > 0 && !string.IsNullOrWhiteSpace( output.CTID ) && output.EntityStateId == 3 )
                {
                    //new - get owner QA now. only if particular context
                    if ( formattingFullVersion )
                    {
                        //actually don't want this as is slow. Break up into parts
                        //var ownersQAReceived = Entity_AssertionManager.GetAllCombinedForTarget( 2, output.OwningOrganization.Id, output.OwningOrganization.Id );
                        //output.OwningOrganizationQAReceived = Entity_AgentRelationshipManager.GetAllThirdPartyAssertionsForEntity( 2, output.OwningOrganization.RowId, output.OwningOrganization.Id, true );
                        //var orgFirstPartyAssertions = Entity_AssertionManager.GetAllFirstPartyAssertionsForTarget( 2, output.OwningOrganization.RowId, output.OwningOrganization.Id, true );
                        //if ( orgFirstPartyAssertions != null && orgFirstPartyAssertions.Any() )
                        //	output.OwningOrganizationQAReceived.AddRange( orgFirstPartyAssertions );
                        output.OwningOrganizationQAReceived = orgRoleManager.GetAllCombinedForTarget( 2, output.OwningOrganizationId, output.OwningOrganizationId, true );
                    }
                }
            }
            if ( output.OwningOrganizationId > 0 )
            {
                //OLD
                //output.OrganizationRole = Entity_AssertionManager.GetAllCombinedForTarget( 7, output.Id, output.OwningOrganizationId );
                //NEW
                //  these are still assertions by the owning org????
                output.OrganizationRole = orgRoleManager.GetAllCombinedForTarget( 7, output.Id, output.OwningOrganizationId );
                //output.OrganizationRole = Entity_AgentRelationshipManager.GetAllThirdPartyAssertionsForEntity( 7, output.RowId, output.OwningOrganizationId );
                //var firstPartyAssertions = Entity_AssertionManager.GetAllFirstPartyAssertionsForTarget( 7, output.RowId, output.OwningOrganizationId, false );
                //if ( firstPartyAssertions != null && firstPartyAssertions.Any() )
                //	output.OrganizationRole.AddRange( firstPartyAssertions );
            }
            //huh this is done above as well??
            output.OrganizationRole = Entity_AgentRelationshipManager.AgentEntityRole_GetAll_ToEnumeration( output.RowId, true );

            if ( output.EntityStateId == 2 )
            {
                //not sure how this is possible?
                if ( ( output.OrganizationRole == null || !output.OrganizationRole.Any() )
                    && output.OwningOrganizationId > 0 )
                {
                    output.OrganizationRole.Add( new OrganizationRoleProfile()
                    {
                        ActingAgentUid = output.PrimaryAgentUID,
                        ActingAgent = new Organization()
                        {
                            Id = output.OwningOrganizationId,
                            RowId = output.PrimaryAgentUID,
                            Name = output.PrimaryOrganization.Name,
                            SubjectWebpage = output.PrimaryOrganization.SubjectWebpage
                        },
                        AgentRole = new Enumeration()
                        {
                            Items = new List<EnumeratedItem>() { new EnumeratedItem()
                        {
                            Name="Owned By"
                        } }
                        }
                    } );
                }
            }

            //TBD
            if ( !havePrimaryOrg )
            {
                //temp set owning org to first offered by?
                //==> if no primaryOrg, then OrganizationRole is not being set!
                if ( output.OrganizationRole != null && output.OrganizationRole.Any() )
                {
                    int cntr = 0;
                    foreach ( var item in output.OrganizationRole )
                    {
                        var exists = item.AgentRole.Items.Where( x => x.Id == 7 ).ToList();
                        if ( exists != null && exists.Any() )
                        {
                            //may not want to set the owningAgent?
                            //output.OwningAgentUid = item.ActingAgentUid;
                            output.PrimaryOrganization = OrganizationManager.GetForSummary( item.ActingAgentUid );
                            output.OwnerRoles = item.AgentRole;
                            break;
                        }
                        cntr++;
                    }
                }
            }
            //
            //22-07-10 - LifeCycleStatusTypeId is now on the credential directly
            output.LifeCycleStatusTypeId = input.LifeCycleStatusTypeId;
            if ( output.LifeCycleStatusTypeId > 0 )
            {
                CodeItem ct = CodesManager.GetLifeCycleStatus( output.LifeCycleStatusTypeId );
                if ( ct != null && ct.Id > 0 )
                {
                    output.LifeCycleStatus = ct.Title;
                }
                //retain example using an Enumeration for by other related tableS??? - old detail page?
                output.LifeCycleStatusType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS );
                output.LifeCycleStatusType.Items.Add( new EnumeratedItem() { Id = output.LifeCycleStatusTypeId, Name = ct.Name, SchemaName = ct.SchemaName } );
            }
            else
            {
                //OLD
                output.LifeCycleStatusType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS );
                EnumeratedItem statusItem = output.LifeCycleStatusType.GetFirstItem();
                if ( statusItem != null && statusItem.Id > 0 && statusItem.Name != "Active" )
                {

                }
            }

            output.AvailabilityListing = input.AvailabilityListing;
            output.AvailableOnlineAt = input.AvailableOnlineAt;
            output.InCatalog = input.InCatalog;

			//will need output do convert before switching these
			//AlternateName is used by API
			output.AlternateName = Entity_ReferenceManager.GetAllToList( output.RowId, CodesManager.PROPERTY_CATEGORY_ALTERNATE_NAME );
            //output.AlternateNames = Entity_ReferenceManager.GetAll( output.RowId, CodesManager.PROPERTY_CATEGORY_ALTERNATE_NAME );

            //only true should be published. Ensure the save only saves True
            if ( input.IsNonCredit != null && input.IsNonCredit == true )
                output.IsNonCredit = input.IsNonCredit;
            else
                output.IsNonCredit = null;
            if ( IsValidDate( input.Created ) )
                output.Created = ( DateTime ) input.Created;

            if ( IsValidDate( input.LastUpdated ) )
                output.LastUpdated = ( DateTime ) input.LastUpdated;
            //now more for references 
            //23-04-20 HMMM, why was this removed? Performance issue? See CredentialManager.
            //23-07-07 MP - added back
            var relatedEntity = EntityManager.GetEntity( output.RowId, false );
            //if ( relatedEntity != null && relatedEntity.Id > 0 )
            //    output.EntityLastUpdated = relatedEntity.LastUpdated;
            output.EntityLastUpdated = output.LastUpdated;  // relatedEntity.LastUpdated;

            output.HasSupportService = Entity_HasSupportServiceManager.GetAllSummary( relatedEntity );
            var getAll = Entity_HasResourceManager.GetAll( relatedEntity );
            //var getAll = Entity_HasResourceManager.GetAllEntityType( relatedEntity, CodesManager.ENTITY_TYPE_OCCUPATIONS_PROFILE, HasSpecializationRelationshipId );
            if ( getAll != null && getAll.Count > 0 )
            {
                output.HasRubric = getAll.Where( r => r.EntityTypeId == CodesManager.ENTITY_TYPE_RUBRIC ).ToList();
                output.ProvidesTransferValueFor = getAll.Where( r => r.RelationshipTypeId == Entity_HasResourceManager.HAS_RESOURCE_TYPE_ProvidesTransferValueFor && ( r.EntityTypeId == CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE ) ).ToList();
                output.ReceivesTransferValueFrom = getAll.Where( r => r.RelationshipTypeId == Entity_HasResourceManager.HAS_RESOURCE_TYPE_ReceivesTransferValueFrom && ( r.EntityTypeId == CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE ) ).ToList();
                output.ObjectOfAction = getAll.Where( r => r.EntityTypeId == CodesManager.ENTITY_TYPE_CREDENTIALING_ACTION ).ToList();
            }

        }
        public static string MapLearningEntityType( int learningEntityTypeId )
        {
            var learningEntityType = string.Empty;
            switch ( learningEntityTypeId )
            {
                case 7:
                    return "LearningOpportunityProfile";
                case 36:
                    return "LearningProgram";
                case 37:
                    return "Course";

            }
            return learningEntityType;
        }
        public static string MapLearningEntityTypeLabel( int learningEntityTypeId )
        {
            var learningEntityType = string.Empty;
            switch ( learningEntityTypeId )
            {
                case 7:
                    return "Learning Opportunity";
                case 36:
                    return "Learning Program";
                case 37:
                    return "Course";

            }
            return learningEntityType;
        }
        public static void MapFromDB_HasPart( ThisResource to, bool forProfilesList)
        {
            to.HasPart = Entity_LearningOpportunityManager.TargetResource_GetAll( to.RowId, forProfilesList, false, CodesManager.RELATIONSHIP_TYPE_HAS_PART );
			if( to.HasPart != null && to.HasPart.Any())
				foreach ( ThisResource e in to.HasPart )
				{
					if ( e.HasCompetencies || e.ChildHasCompetencies )
					{
						to.ChildHasCompetencies = true;
						break;
					}
				}
        }
		public static void MapFromDB_PreRequisite( ThisResource to, bool forProfilesList )
		{
			to.Prerequisite = Entity_LearningOpportunityManager.TargetResource_GetAll( to.RowId, forProfilesList, false, CodesManager.RELATIONSHIP_TYPE_HAS_PREREQUISITE );
			if ( to.Prerequisite != null && to.Prerequisite.Any() )
				foreach ( ThisResource e in to.Prerequisite )
				{
					if ( e.HasCompetencies || e.ChildHasCompetencies )
					{
						to.ChildHasCompetencies = true;
						break;
					}
				}
		}
		public static void MapFromDB_Competencies( ThisResource output )
		{
            var frameworksList = new Dictionary<string, RegistryImport>();
            //not sure if this one is used anymore!!!!!!!!!!!!!!!!!!
			//20-10-06 - there will be cases, like transfer value that only competencies will be available, with no framework.

            //to.TeachesCompetencies = Entity_CompetencyManager.GetAllAs_CredentialAlignmentObjectProfile( to.RowId, ref frameworksList);
			//to.FrameworkPayloads = frameworksList;
			//if ( to.TeachesCompetencies.Count > 0 )
			//	to.HasCompetencies = true;

			output.TeachesCompetenciesFrameworks = Entity_CompetencyManager.GetAllAs_CAOFramework(output.RowId, "Teaches", ref frameworksList);
            if ( output.TeachesCompetenciesFrameworks.Count > 0 )
            {
                output.HasCompetencies = true;
                output.FrameworkPayloads = frameworksList;
            }

			output.AssessesCompetenciesFrameworks = Entity_CompetencyManager.GetAllAs_CAOFramework( output.RowId, "Assesses", ref frameworksList );
			if ( output.AssessesCompetenciesFrameworks.Count > 0 )
			{
				output.HasCompetencies = true;
				//should be OK. Latter method appends to frameworksList
				output.FrameworkPayloads = frameworksList;
			}

			//these should be under a condition profile???
			//should never get results???
			output.RequiresCompetenciesFrameworks = new List<CredentialAlignmentObjectFrameworkProfile>();
   //         output.RequiresCompetenciesFrameworks = Entity_CompetencyManager.GetAllAs_CAOFramework( output.RowId, "Requires", ref frameworksList );
   //         if ( output.RequiresCompetenciesFrameworks.Count > 0 )
			//{
			//	output.HasCompetencies = true;
			//	//should be OK. Latter method appends to frameworksList
			//	output.FrameworkPayloads = frameworksList;
			//}
		}
		private static void AddCredentialReference( int credentialId, ThisResource to )
		{
			Credential exists = to.IsPartOfCredential.FirstOrDefault( s => s.Id == credentialId );
			//hmm  would be useful to know how connected for display purposes.
			if ( exists == null || exists.Id == 0 )
				to.IsPartOfCredential.Add( CredentialManager.GetBasic( credentialId ) );
		}

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.ProfileModels;
using workIT.Utilities;

using ThisEntity = workIT.Models.ProfileModels.LearningOpportunityProfile;
using DBEntity = workIT.Data.Tables.LearningOpportunity;
using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;
using CondProfileMgr = workIT.Factories.Entity_ConditionProfileManager;

using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;

namespace workIT.Factories
{
    public class LearningOpportunityManager : BaseFactory
    {
        static string thisClassName = "LearningOpportunityManager";
        EntityManager entityMgr = new EntityManager();
        #region LearningOpportunity - persistance ==================
        /// <summary>
        /// Update a LearningOpportunity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public bool Save( ThisEntity entity, ref SaveStatus status )
        {
			LoggingHelper.DoTrace( 7, thisClassName + ".Save - entered" );

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
                        DBEntity efEntity = context.LearningOpportunity
                                    .FirstOrDefault( s => s.Id == entity.Id );

                        if ( efEntity != null && efEntity.Id > 0 )
                        {
                            //delete the entity and re-add
                            //Entity e = new Entity()
                            //{
                            //    EntityBaseId = efEntity.Id,
                            //    EntityTypeId = CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE,
                            //    EntityType = "LearningOpportunity",
                            //    EntityUid = efEntity.RowId,
                            //    EntityBaseName = efEntity.Name
                            //};
                            //if ( entityMgr.ResetEntity( e, ref statusMessage ) )
                            //{

                            //}

                            //fill in fields that may not be in entity
                            entity.RowId = efEntity.RowId;

                            MapToDB( entity, efEntity );

							//19-05-21 mp - should add a check for an update where currently is deleted
							if ( ( efEntity.EntityStateId ?? 0 ) == 0 )
							{
								var url = string.Format( UtilityManager.GetAppKeyValue( "credentialFinderSite" ) + "learningopportunity/{0}", efEntity.Id );
								//notify, and???
								//EmailManager.NotifyAdmin( "Previously Deleted LearningOpportunity has been reactivated", string.Format( "<a href='{2}'>LearningOpportunity: {0} ({1})</a> was deleted and has now been reactivated.", efEntity.Name, efEntity.Id, url ) );
								SiteActivity sa = new SiteActivity()
								{
									ActivityType = "LearningOpportunity",
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

							if ( IsValidDate( status.EnvelopeCreatedDate ) && status.LocalCreatedDate < efEntity.Created )
							{
								efEntity.Created = status.LocalCreatedDate;
							}
							if ( IsValidDate( status.EnvelopeUpdatedDate ) && status.LocalUpdatedDate != efEntity.LastUpdated )
							{
								efEntity.LastUpdated = status.LocalUpdatedDate;
							}
							//has changed?
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

                            if ( isValid )
                            {
                                if ( !UpdateParts( entity, ref status ) )
                                    isValid = false;

                                SiteActivity sa = new SiteActivity()
                                {
                                    ActivityType = "LearningOpportunity",
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
                LoggingHelper.LogError( ex, thisClassName + string.Format( ".Save. id: {0}, Name: {1}", entity.Id, entity.Name ) );
                status.AddError( thisClassName + " Error - the save was not successful. " + message );
                isValid = false;
            }


            return isValid;
        }
        /// <summary>
        /// add a LearningOpportunity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="statusMessage"></param>
        /// <returns></returns>
        private int Add( ThisEntity entity, ref SaveStatus status )
        {
			LoggingHelper.DoTrace( 7, thisClassName + ".Add - entered" );

			DBEntity efEntity = new DBEntity();
            using ( var context = new EntityContext() )
            {
                try
                {

                    MapToDB( entity, efEntity );
                    efEntity.EntityStateId = 3;
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

					context.LearningOpportunity.Add( efEntity );

                    // submit the change to database
                    int count = context.SaveChanges();
                    if ( count > 0 )
                    {
                        entity.Id = efEntity.Id;
                        entity.RowId = efEntity.RowId;
                        //add log entry
                        SiteActivity sa = new SiteActivity()
                        {
                            ActivityType = "LearningOpportunity",
                            Activity = "Import",
                            Event = "Add",
                            Comment = string.Format( "Full LearningOpportunity was added by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ),
                            ActivityObjectId = entity.Id
                        };
                        new ActivityManager().SiteActivityAdd( sa );


                        if ( UpdateParts( entity, ref status ) == false )
                        {

                        }

                        return entity.Id;
                    }
                    else
                    {
                        //?no info on error
                        string message = thisClassName + string.Format( ". Add Failed", "Attempted to add a Learning Opportunity. The process appeared to not work, but was not an exception, so we have no message, or no clue. LearningOpportunity: {0}, CTID: {1}", entity.Name, entity.CTID );
                        status.AddError( thisClassName + ". Error - the add was not successful. " + message );
                        EmailManager.NotifyAdmin( thisClassName + ".Add Failed", message );
                    }
                }
                catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
                {

                    string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", entity.Name );
                    status.AddError( thisClassName + " - Error - the save was not successful. " + message );
                    LoggingHelper.LogError( message, true );

                }
                catch ( Exception ex )
                {
                    string message = FormatExceptions( ex );
                    LoggingHelper.LogError( ex, thisClassName + string.Format( ".Add(), Name: {0} \r\n", entity.Name ) + message );

                    status.AddError( thisClassName + " - Error - the save was not successful. \r\n" + message );
                }
            }

            return entity.Id;
        }
        public int AddBaseReference( ThisEntity input, ref SaveStatus status )
        {
			//*** need to handle updates, do we?
            DBEntity efEntity = new DBEntity();
            try
            {
                using ( var context = new EntityContext() )
                {
                    if ( input == null ||
                        ( string.IsNullOrWhiteSpace( input.Name )) 
						//||                        string.IsNullOrWhiteSpace( input.SubjectWebpage )) 
						)
                    {
                        status.AddError( thisClassName + ". AddBaseReference() The learning opportunity is incomplete" );
                        return 0;
                    }

					//only add DB required properties
					//NOTE - an input will be created via trigger
					//20-08-27 mp - using full MapToDB to handle any additions that appear in the future
					MapToDB( input, efEntity );
					efEntity.EntityStateId = 2;
     //               efEntity.Name = input.Name;
     //               efEntity.Description = input.Description;
     //               efEntity.SubjectWebpage = input.SubjectWebpage;
					////
					//efEntity.AssessmentMethodDescription = input.AssessmentMethodDescription;
					//efEntity.LearningMethodDescription = input.LearningMethodDescription;
					//efEntity.IdentificationCode = input.CodedNotation;
					//
					if ( IsValidGuid( input.RowId ) )
                        efEntity.RowId = input.RowId;
                    else
                        efEntity.RowId = Guid.NewGuid();
                    //set to return, just in case
                    input.RowId = efEntity.RowId;
                    efEntity.Created = System.DateTime.Now;
                    efEntity.LastUpdated = System.DateTime.Now;

                    context.LearningOpportunity.Add( efEntity );
                    int count = context.SaveChanges();
                    if ( count > 0 )
                    {
                        input.Id = efEntity.Id;
						/* handle new parts
						 * AvailableAt
						 * CreditValue
						 * EstimatedDuration
						 * OfferedBy
						 * OwnedBy
						 * teaches
						 */
						if ( UpdateParts( input, ref status ) == false )
						{

						}

						SiteActivity sa = new SiteActivity()
						{
							ActivityType = "LearningOpportunity",
							Activity = "Import",
							Event = "Add Reference",
							Comment = string.Format( "Reference LearningOpportunity was added by the import. Name: {0}, SWP: {1}", input.Name, input.SubjectWebpage ),
							ActivityObjectId = input.Id
						};
						return efEntity.Id;
                    }

                    status.AddError( thisClassName + ". AddBaseReference() Error - the save was not successful, but no message provided. " );
                }
            }
			catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
			{
				status.AddError( HandleDBValidationError( dbex, thisClassName + ".AddBaseReference() ", "LearningOpportunity" ) );
				LoggingHelper.LogError( dbex, thisClassName + string.Format( ".Add(), Name: {0}, UserId: {1}", input.Name, input.CreatedById ) );


			}
			catch ( Exception ex )
            {
                string message = FormatExceptions( ex );
                LoggingHelper.LogError( ex, thisClassName + string.Format( ".AddBaseReference. Name:  {0}, SubjectWebpage: {1}", input.Name, input.SubjectWebpage ), true );
                status.AddError( thisClassName + " Error - the save was not successful. " + message );

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

                    context.LearningOpportunity.Add( efEntity );
                    int count = context.SaveChanges();
                    if ( count > 0 )
                    {
                        SiteActivity sa = new SiteActivity()
                        {
                            ActivityType = "LearningOpportunity",
                            Activity = "Import",
                            Event = "Add Pending LearningOpportunity",
                            Comment = string.Format( "Pending LearningOpportunity was added by the import. ctid: {0}, registryAtId: {1}", ctid, registryAtId ),
                            ActivityObjectId = efEntity.Id
                        };
                        new ActivityManager().SiteActivityAdd( sa );
                        return efEntity.Id;
                    }

                    status = thisClassName + " Error - the save was not successful, but no message provided. ";
                }
            }

            catch ( Exception ex )
            {
                string message = FormatExceptions( ex );
                LoggingHelper.LogError( ex, thisClassName + string.Format( ".AddPendingRecord. entityUid:  {0}, ctid: {1}", entityUid, ctid ) );
                status = thisClassName + " Error - the save was not successful. " + message;

            }
            return 0;
        }

        public bool ValidateProfile( ThisEntity profile, ref SaveStatus status )
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
            if ( !IsValidGuid( profile.OwningAgentUid ) )
            {
                //status.AddWarning( "An owning organization is missing" );
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

            if ( profile.CreditUnitValue < 0 || profile.CreditUnitValue > 1000 )
                status.AddWarning( "Error: invalid value for Credit Unit Value. Must be a reasonable decimal value greater than zero." );


            //can only have credit hours properties, or credit unit properties, not both
            bool hasCreditHourData = false;
            bool hasCreditUnitData = false;
            //if ( profile.CreditHourValue > 0 || ( profile.CreditHourType ?? "" ).Length > 0 )
            //    hasCreditHourData = true;
            if ( profile.CreditUnitTypeId > 0
                || ( profile.CreditUnitTypeDescription ?? "" ).Length > 0
                || profile.CreditUnitValue > 0 )
                hasCreditUnitData = true;

            if ( hasCreditHourData && hasCreditUnitData )
                status.AddWarning( "Error: Data can be entered for Credit Hour related properties or Credit Unit related properties, but not for both." );

			return status.WasSectionValid;
		}


		/// <summary>
		/// Delete a Learning Opportunity, and related Entity - where related entity.LearningOpportunity was deleted
		/// </summary>
		/// <param name="id"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool Delete( int id, ref string statusMessage )
		{
			bool isValid = false;
			if ( id == 0 )
			{
				statusMessage = "Error - missing an identifier for the LearningOpportunity";
				return false;
			}
			int orgId = 0;
			Guid orgUid = Guid.Empty;

			using ( var context = new EntityContext() )
			{
				try
				{
					context.Configuration.LazyLoadingEnabled = false;
					DBEntity efEntity = context.LearningOpportunity
								.FirstOrDefault( s => s.Id == id );

					if ( efEntity != null && efEntity.Id > 0 )
					{
						if ( IsValidGuid( efEntity.OwningAgentUid ) )
						{
							var org = OrganizationManager.GetForSummary( ( Guid )efEntity.OwningAgentUid );
							if ( org != null && org.Id > 0 )
							{
								orgId = org.Id;
								orgUid = org.RowId;
							}
						}
						int count = 0;
						if ( efEntity.EntityStateId == 3 )
						{
							//log with reason
							string msg = string.Format( " Learning Opportunity. Id: {0}, Name: {1}, Ctid: {2}.", efEntity.Id, efEntity.Name, efEntity.CTID );

							//18-04-05 mparsons - change to set inactive, and notify - seems to have been some incorrect deletes
							efEntity.EntityStateId = 0;
							efEntity.LastUpdated = System.DateTime.Now;
							count = context.SaveChanges();
							if ( count > 0 )
							{
								//trace is done in import
								//LoggingHelper.DoTrace( 2, "Learning Opportunity virtually deleted: " + msg );
								new ActivityManager().SiteActivityAdd( new SiteActivity()
								{
									ActivityType = "LearningOpportunity",
									Activity = "Import",
									Event = "Delete From Related",
									Comment = msg,
									ActivityObjectId = efEntity.Id
								} );
							}
						}
						else
						{
							context.LearningOpportunity.Remove( efEntity );
							count = context.SaveChanges();
						}

						if ( count > 0 )
						{
							isValid = true;
							//add pending request 
							List<String> messages = new List<string>();
							new SearchPendingReindexManager().AddDeleteRequest( CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, id, ref messages );

							new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_ORGANIZATION, orgId, 1, ref messages );
							//also check for any relationships
							new Entity_AgentRelationshipManager().ReindexAgentForDeletedArtifact( orgUid );
						}
					}
					else
					{
						statusMessage = "Error - LearningOpportunity delete failed, as record was not found.";
					}
					
				}
				catch ( Exception ex )
				{
					statusMessage = FormatExceptions( ex );
					LoggingHelper.LogError( ex, thisClassName + ".Delete()" );

					if ( statusMessage.ToLower().IndexOf( "the delete statement conflicted with the reference constraint" ) > -1 )
					{
						statusMessage = "Error: this learning opportunity cannot be deleted as it is being referenced by other items, such as roles or credentials. These associations must be removed before this learning opportunity can be deleted.";
					}
				}
			}

			return isValid;
		}


		/// <summary>
		/// Delete by envelopeId
		/// </summary>
		/// <param name="envelopeId"></param>
		/// <param name="ctid"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool Delete( string envelopeId, string ctid, ref string statusMessage )
        {
            bool isValid = true;
            if ( ( string.IsNullOrWhiteSpace( envelopeId ) || !IsValidGuid( envelopeId ) )
                && string.IsNullOrWhiteSpace( ctid ) )
            {
                statusMessage = thisClassName + ".Delete() Error - a valid CTID must be provided - OR  valid envelope identifier.";
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
                    DBEntity efEntity = context.LearningOpportunity
                                .FirstOrDefault( s => s.CredentialRegistryId == envelopeId
                                || ( s.CTID == ctid )
                                );

                    if ( efEntity != null && efEntity.Id > 0 )
                    {
                        Guid rowId = efEntity.RowId;
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
                            isValid = true;
                            //trace is done in import
                            //LoggingHelper.DoTrace( 2, "Learning Opportunity virtually deleted: " + msg );
                            new ActivityManager().SiteActivityAdd( new SiteActivity()
                            {
                                ActivityType = "LearningOpportunity",
                                Activity = "Import",
                                Event = "Delete",
								Comment = msg,
								ActivityObjectId = efEntity.Id
							} );
                            //add pending request 
                            List<String> messages = new List<string>();
                            new SearchPendingReindexManager().AddDeleteRequest( CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, efEntity.Id, ref messages );
							//mark owning org for updates (actually should be covered by ReindexAgentForDeletedArtifact
							new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_ORGANIZATION, orgId, 1, ref messages );

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
        #region LearningOpportunity properties ===================
        public bool UpdateParts( ThisEntity entity, ref SaveStatus status )
        {
			LoggingHelper.DoTrace( 7, thisClassName + ".UpdateParts - entered" );

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

			if ( erfm.SaveList( relatedEntity.Id, CodesManager.PROPERTY_CATEGORY_SOC, entity.Occupations, ref status ) == false )
				isAllValid = false;
			if ( erfm.SaveList( relatedEntity.Id, CodesManager.PROPERTY_CATEGORY_NAICS, entity.Industries, ref status ) == false )
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

            if ( erm.Add( entity.Subject, entity.RowId, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, ref status, CodesManager.PROPERTY_CATEGORY_SUBJECT, false ) == false )
                isAllValid = false;

            if ( erm.Add( entity.Keyword, entity.RowId, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, ref status, CodesManager.PROPERTY_CATEGORY_KEYWORD, false ) == false )
                isAllValid = false;

            //for language, really want to convert from en to English (en)
            erm.AddLanguages( entity.InLanguageCodeList, entity.RowId, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, ref status, CodesManager.PROPERTY_CATEGORY_LANGUAGE );

            AddProfiles( entity, relatedEntity, ref status );
            //foreach ( var hasPart in entity.HasPart )
            //    AddProfiles( hasPart, ref status );
            if ( entity.HasPartIds != null && entity.HasPartIds.Count > 0 )
            {
                Entity_LearningOpportunityManager elm = new Entity_LearningOpportunityManager();
                elm.DeleteAll( relatedEntity, ref status );

                foreach ( var hasPartId in entity.HasPartIds )
                {
                    LoggingHelper.DoTrace( 5, thisClassName + string.Format( ".HandleTargets. entity.ParentId: {0}, processing loppId: {1}, entity.RowId: {2}", entity.ParentId, entity.Id, entity.RowId.ToString() ) );
                    elm.Add( entity.RowId, hasPartId, BaseFactory.RELATIONSHIP_TYPE_HAS_PART, true, ref status );
                }
            }
            foreach ( var isPart in entity.IsPartOf )
                if( isPart.ParentId == 0)
                    LoggingHelper.DoTrace( 5, thisClassName + string.Format( ".HandleTargets. entity.ParentId: {0}, processing loppId: {1}, entity.RowId: {2}", entity.ParentId, entity.Id, entity.RowId.ToString() ) );

            UpdateAssertedBys( entity, ref status );

            UpdateAssertedIns( entity, ref status );


            return isAllValid;
        }

        public bool UpdateProperties( ThisEntity entity, Entity relatedEntity, ref SaveStatus status )
        {
			LoggingHelper.DoTrace( 7, thisClassName + ".UpdateProperties - entered" );

			bool isAllValid = true;

            EntityPropertyManager mgr = new EntityPropertyManager();
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

			if ( mgr.AddProperties( entity.AudienceType,entity.RowId,CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE,CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE,false,ref status ) == false )
                isAllValid = false;
            return isAllValid;
		}
		public void AddProfiles( ThisEntity entity, Entity relatedEntity, ref SaveStatus status )
		{
			LoggingHelper.DoTrace( 7, thisClassName + ".AddProfiles - entered" );

			//DurationProfile
			DurationProfileManager dpm = new Factories.DurationProfileManager();
			dpm.SaveList( entity.EstimatedDuration, entity.RowId, ref status );

			//Identifiers - do delete for first one and then assign
			new Entity_IdentifierValueManager().SaveList( entity.Identifier, entity.RowId, Entity_IdentifierValueManager.LEARNING_OPP_Identifier, ref status, true );

			//VersionIdentifier
			new Entity_IdentifierValueManager().SaveList( entity.VersionIdentifierList, entity.RowId, Entity_IdentifierValueManager.LEARNING_OPP_VersionIdentifier, ref status, false );

            //CostProfile
            CostProfileManager cpm = new Factories.CostProfileManager();
            cpm.SaveList( entity.EstimatedCost, entity.RowId, ref status );

            try
            {
                //ConditionProfile =======================================
                Entity_ConditionProfileManager emanager = new Entity_ConditionProfileManager();
                //emanager.DeleteAll( relatedEntity, ref status );

                emanager.SaveList( entity.Requires, Entity_ConditionProfileManager.ConnectionProfileType_Requirement, entity.RowId, ref status );
                emanager.SaveList( entity.Recommends, Entity_ConditionProfileManager.ConnectionProfileType_Recommendation, entity.RowId, ref status );
                emanager.SaveList( entity.Corequisite, Entity_ConditionProfileManager.ConnectionProfileType_Corequisite, entity.RowId, ref status );
                emanager.SaveList( entity.EntryCondition, Entity_ConditionProfileManager.ConnectionProfileType_EntryCondition, entity.RowId, ref status );

                //Connections
                emanager.SaveList( entity.AdvancedStandingFor, Entity_ConditionProfileManager.ConnectionProfileType_AdvancedStandingFor, entity.RowId, ref status, 4 );
                emanager.SaveList( entity.AdvancedStandingFrom, Entity_ConditionProfileManager.ConnectionProfileType_AdvancedStandingFrom, entity.RowId, ref status, 4 );
                emanager.SaveList( entity.IsPreparationFor, Entity_ConditionProfileManager.ConnectionProfileType_PreparationFor, entity.RowId, ref status, 4 );
                emanager.SaveList( entity.PreparationFrom, Entity_ConditionProfileManager.ConnectionProfileType_PreparationFrom, entity.RowId, ref status, 4 );
                emanager.SaveList( entity.IsRequiredFor, Entity_ConditionProfileManager.ConnectionProfileType_NextIsRequiredFor, entity.RowId, ref status, 4 );
                emanager.SaveList( entity.IsRecommendedFor, Entity_ConditionProfileManager.ConnectionProfileType_NextIsRecommendedFor, entity.RowId, ref status, 4 );
            }
            catch ( Exception ex )
            {
                string message = FormatExceptions( ex );
                LoggingHelper.LogError( ex, thisClassName + string.Format( ".AddProfiles() - ConditionProfiles. id: {0}", entity.Id ) );
                status.AddWarning( thisClassName + ".AddProfiles(). Exceptions encountered handling ConditionProfiles. " + message );
            }

            //Financial Alignment 
            //Entity_FinancialAlignmentProfileManager fapm = new Factories.Entity_FinancialAlignmentProfileManager();
            //fapm.SaveList( entity.FinancialAssistanceOLD, entity.RowId, ref status );
			new Entity_FinancialAssistanceProfileManager().SaveList( entity.FinancialAssistance, entity.RowId, ref status );

			//competencies
			if ( entity.TeachesCompetencies != null && entity.TeachesCompetencies.Count > 0 )
            {
                Entity_CompetencyManager ecm = new Entity_CompetencyManager();
                ecm.SaveList( entity.TeachesCompetencies, entity.RowId, ref status );
            }

            //addresses
            new Entity_AddressManager().SaveList( entity.Addresses, entity.RowId, ref status );

            //JurisdictionProfile 
            Entity_JurisdictionProfileManager jpm = new Entity_JurisdictionProfileManager();
            //do deletes - NOTE: other jurisdictions are added in: UpdateAssertedIns
            jpm.DeleteAll( relatedEntity, ref status );

            jpm.SaveList( entity.Jurisdiction, entity.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_SCOPE, ref status );

            new Entity_CommonConditionManager().SaveList( entity.ConditionManifestIds, entity.RowId, ref status );

            new Entity_CommonCostManager().SaveList( entity.CostManifestIds, entity.RowId, ref status );
        }


        public bool UpdateAssertedBys( ThisEntity entity, ref SaveStatus status )
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
            //note the deleteAll is done in AddProfiles

            mgr.SaveAssertedInList( entity.RowId, Entity_AgentRelationshipManager.ROLE_TYPE_AccreditedBy, entity.AccreditedIn, ref status );
            mgr.SaveAssertedInList( entity.RowId, Entity_AgentRelationshipManager.ROLE_TYPE_ApprovedBy, entity.ApprovedIn, ref status );
            mgr.SaveAssertedInList( entity.RowId, Entity_AgentRelationshipManager.ROLE_TYPE_OFFERED_BY, entity.OfferedIn, ref status );

            mgr.SaveAssertedInList( entity.RowId, Entity_AgentRelationshipManager.ROLE_TYPE_RecognizedBy, entity.RecognizedIn, ref status );
            mgr.SaveAssertedInList( entity.RowId, Entity_AgentRelationshipManager.ROLE_TYPE_RegulatedBy, entity.RegulatedIn, ref status );

        } //

        #endregion

        #endregion

        #region == Retrieval =======================
        public static int DoesCtidExist( string ctid )
        {
            ThisEntity entity = GetByCtid( ctid );
            if ( entity != null && entity.Id > 0 )
                return entity.Id;
            else
                return 0;
        }

        public static ThisEntity GetByCtid( string ctid )
        {
            ThisEntity entity = new ThisEntity();
            using ( var context = new EntityContext() )
            {
                DBEntity from = context.LearningOpportunity
                        .FirstOrDefault( s => s.CTID.ToLower() == ctid.ToLower() );

                if ( from != null && from.Id > 0 )
                {
                    entity.RowId = from.RowId;
                    entity.Id = from.Id;
                    entity.EntityStateId = ( int )( from.EntityStateId ?? 1 );
                    entity.Name = from.Name;
                    entity.Description = from.Description;
                    entity.SubjectWebpage = from.SubjectWebpage;
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
                DBEntity from = context.LearningOpportunity
                        .FirstOrDefault( s => s.SubjectWebpage.ToLower() == swp.ToLower() );

                if ( from != null && from.Id > 0 )
                {
                    entity.RowId = from.RowId;
                    entity.Id = from.Id;
                    entity.Name = from.Name;
                    entity.EntityStateId = ( int )( from.EntityStateId ?? 1 );
                    entity.Description = from.Description;
                    entity.SubjectWebpage = from.SubjectWebpage;

                    entity.CTID = from.CTID;
                    entity.CredentialRegistryId = from.CredentialRegistryId;
                }
            }
            return entity;
        }
        public static ThisEntity GetByName_SubjectWebpage( string name, string swp )
        {
            ThisEntity entity = new ThisEntity();
            using ( var context = new EntityContext() )
            {
                context.Configuration.LazyLoadingEnabled = false;
                DBEntity from = context.LearningOpportunity
                        .FirstOrDefault( s => s.Name.ToLower() == name.ToLower() && s.SubjectWebpage == swp );

                if ( from != null && from.Id > 0 )
                {
                    //entity.RowId = from.RowId;
                    //entity.Id = from.Id;
                    //entity.Name = from.Name;
                    //entity.EntityStateId = ( int )( from.EntityStateId ?? 1 );
                    //entity.Description = from.Description;
                    //entity.SubjectWebpage = from.SubjectWebpage;

                    //entity.CTID = from.CTID;
                    //entity.CredentialRegistryId = from.CredentialRegistryId;
					MapFromDB( from, entity,
						true, //includingProperties
						true,
						true );
				}
            }
            return entity;
        }
        public static ThisEntity GetForDetail( int id )
        {
            ThisEntity entity = new ThisEntity();
            bool includingProfiles = true;

            using ( var context = new EntityContext() )
            {
                //context.Configuration.LazyLoadingEnabled = false;
                DBEntity item = context.LearningOpportunity
                        .SingleOrDefault( s => s.Id == id );

                if ( item != null && item.Id > 0 )
                {
                    //check for virtual deletes
                    if ( item.EntityStateId == 0 )
                        return entity;

                    MapFromDB( item, entity,
                        true, //includingProperties
                        includingProfiles,
                        true );
                }
            }

            return entity;
        }

        /// <summary>
        /// Get absolute minimum for display in lists, etc
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static ThisEntity GetBasic( int id )
        {
            ThisEntity entity = new ThisEntity();

            using ( var context = new EntityContext() )
            {
                context.Configuration.LazyLoadingEnabled = false;
                DBEntity item = context.LearningOpportunity
                        .FirstOrDefault( s => s.Id == id );

                if ( item != null && item.Id > 0 )
                {
                    MapFromDB_Basic( item, entity, false );
                }
            }

            return entity;
        }

        public static List<ThisEntity> GetAllForOwningOrg( Guid owningOrgUid, ref int totalRecords, int maxRecords = 100 )
        {
            List<ThisEntity> list = new List<ThisEntity>();
            ThisEntity entity = new ThisEntity();
            using ( var context = new EntityContext() )
            {
                List<DBEntity> results = context.LearningOpportunity
                             .Where( s => s.OwningAgentUid == owningOrgUid )
                             .OrderBy( s => s.Name )
                             .ToList();
                if ( results != null && results.Count > 0 )
                {
					totalRecords = results.Count();

					foreach ( DBEntity item in results )
                    {
                        entity = new ThisEntity();
                        MapFromDB_Basic( item, entity, false );

                        list.Add( entity );
						if ( maxRecords > 0 && list.Count >= maxRecords )
							break;
                    }
                }
            }

            return list;
        }
		public static List<ThisEntity> GetAll( ref int totalRecords, int maxRecords = 100 )
		{
			List<ThisEntity> list = new List<ThisEntity>();
			ThisEntity entity = new ThisEntity();
			using ( var context = new EntityContext() )
			{
				List<DBEntity> results = context.LearningOpportunity
							 .Where( s => s.EntityStateId > 2 )
							 .OrderBy( s => s.Name )
							 .ToList();
				if ( results != null && results.Count > 0 )
				{
					totalRecords = results.Count();

					foreach ( DBEntity item in results )
					{
						entity = new ThisEntity();
						MapFromDB_Basic( item, entity, false );

						list.Add( entity );
						if ( maxRecords > 0 && list.Count >= maxRecords )
							break;
					}
				}
			}

			return list;
		}

		public static ThisEntity GetAs_IsPartOf( Guid rowId )
        {
            ThisEntity entity = new ThisEntity();

            using ( var context = new EntityContext() )
            {
                //	REVIEW	- seems like will need to almost always bubble up costs
                //			- just confirm that this method is to simply list parent Lopps
                context.Configuration.LazyLoadingEnabled = false;

                DBEntity item = context.LearningOpportunity
                        .FirstOrDefault( s => s.RowId == rowId );

                if ( item != null && item.Id > 0 )
                {
                    MapFromDB_Basic( item, entity, false );
                }
            }

            return entity;
        }
        public static void MapFromDB_Basic( DBEntity from, ThisEntity entity,
                bool includingCosts )
        {
            entity.Id = from.Id;
            entity.RowId = from.RowId;
            entity.EntityStateId = ( int )( from.EntityStateId ?? 1 );
            entity.Name = from.Name;
            entity.Description = from.Description;
            entity.SubjectWebpage = from.SubjectWebpage;
            entity.CTID = from.CTID;

            if ( string.IsNullOrWhiteSpace( from.CTID ) )
                entity.IsReferenceVersion = true;
            if ( IsGuidValid( from.OwningAgentUid ) )
            {
                entity.OwningAgentUid = ( Guid )from.OwningAgentUid;
				entity.OwningOrganization = OrganizationManager.GetForSummary( entity.OwningAgentUid );

				//get roles
				OrganizationRoleProfile orp = Entity_AgentRelationshipManager.AgentEntityRole_GetAsEnumerationFromCSV( entity.RowId, entity.OwningAgentUid );
				entity.OwnerRoles = orp.AgentRole;
			}

			entity.AvailabilityListing = from.AvailabilityListing;
			entity.AvailableOnlineAt = from.AvailableOnlineAt;

			//costs? = shouldn't need, but make optional
			if ( includingCosts )
			{
				entity.EstimatedCost = CostProfileManager.GetAll( entity.RowId );
				entity.CommonCosts = Entity_CommonCostManager.GetAll( entity.RowId );
			}
			if ( IsValidDate( from.Created ) )
				entity.Created = ( DateTime )from.Created;

			if ( IsValidDate( from.LastUpdated ) )
				entity.LastUpdated = ( DateTime )from.LastUpdated;
			var relatedEntity = EntityManager.GetEntity( entity.RowId, false );
			if ( relatedEntity != null && relatedEntity.Id > 0 )
				entity.EntityLastUpdated = relatedEntity.LastUpdated;

		}


        public static List<object> Autocomplete( string pFilter, int pageNumber, int pageSize, ref int pTotalRows )
        {
            bool autocomplete = true;
            List<object> results = new List<object>();
            List<string> competencyList = new List<string>();
            //get minimal entity
            List<ThisEntity> list = Search( pFilter, "", pageNumber, pageSize, ref pTotalRows, ref competencyList, autocomplete );
            bool appendingOrgNameToAutocomplete = UtilityManager.GetAppKeyValue( "appendingOrgNameToAutocomplete", false );
            string prevName = "";
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
        public static List<ThisEntity> Search( string pFilter, string pOrderBy, int pageNumber, int pageSize, ref int pTotalRows )
        {
            List<string> competencyList = new List<string>();
            return Search( pFilter, pOrderBy, pageNumber, pageSize, ref pTotalRows, ref competencyList );
        }
        public static List<ThisEntity> Search( string pFilter, string pOrderBy, int pageNumber, int pageSize,
            ref int pTotalRows,
            ref List<string> competencyList,
            bool autocomplete = false )
        {
            string connectionString = DBConnectionRO();
            ThisEntity item = new ThisEntity();
            List<ThisEntity> list = new List<ThisEntity>();
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

                using ( SqlCommand command = new SqlCommand( "[LearningOpportunity_Search]", c ) )
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
                    item.AvailableOnlineAt = GetRowPossibleColumn( dr, "AvailableOnlineAt", "" );

                    item.CTID = GetRowPossibleColumn( dr, "CTID", "" );
                    item.CredentialRegistryId = GetRowPossibleColumn( dr, "CredentialRegistryId", "" );

                    item.CodedNotation = GetRowColumn( dr, "IdentificationCode", "" );

                    org = GetRowPossibleColumn( dr, "Organization", "" );
                    orgId = GetRowPossibleColumn( dr, "OrgId", 0 );
                    if ( orgId > 0 )
                    {
                        item.OwningOrganization = new Organization() { Id = orgId, Name = org };
                    }
					item.ListTitle = item.Name + " (" + item.OrganizationName + ")";
					//
					temp = GetRowColumn( dr, "DateEffective", "" );
                    if ( IsValidDate( temp ) )
                        item.DateEffective = DateTime.Parse( temp ).ToShortDateString();
                    else
                        item.DateEffective = "";

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
                        item.Addresses = Entity_AddressManager.GetAll( item.RowId );
                    }
                    //competencies. either arbitrarily get all, or if filters exist, only return matching ones
                    item.CompetenciesCount = GetRowPossibleColumn( dr, "Competencies", 0 );
                    if ( item.CompetenciesCount > 0 )
                    {
                        //handled in search services
                        //FillCompetencies( item, ref competencyList );
                    }

                    list.Add( item );
                }

                return list;

            }
        } //



        public static void MapToDB( ThisEntity input, DBEntity output )
        {

            //want output ensure fields input create are not wiped
            if ( output.Id == 0 )
            {
                output.CTID = input.CTID;
            }

            if ( !string.IsNullOrWhiteSpace( input.CredentialRegistryId ) )
                output.CredentialRegistryId = input.CredentialRegistryId;
            output.Id = input.Id;
            output.Name = GetData( input.Name );

            output.Description = GetData( input.Description );

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
                //output.OwningAgentUid = null;
            }

            output.SubjectWebpage = GetUrlData( input.SubjectWebpage, null );
            output.IdentificationCode = GetData( input.CodedNotation );
            output.AvailableOnlineAt = GetUrlData( input.AvailableOnlineAt, null );
            output.AvailabilityListing = GetUrlData( input.AvailabilityListing, null );
            output.DeliveryTypeDescription = input.DeliveryTypeDescription;
			//output.VerificationMethodDescription = input.VerificationMethodDescription;
			//
			output.AssessmentMethodDescription = input.AssessmentMethodDescription;
			output.LearningMethodDescription = input.LearningMethodDescription;
			//
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
			if ( input.CreditValueList != null && input.CreditValueList.Any() )
			{
				//output.CreditValueJson = input.CreditValueJson;
				input.CreditValue = input.CreditValueList[ 0 ];
			}
			if ( input.CreditValue.HasData() )
			{
				if ( input.CreditValue.CreditUnitType != null && input.CreditValue.CreditUnitType.HasItems() )
				{
					//get Id if available
					EnumeratedItem item = input.CreditValue.CreditUnitType.GetFirstItem();
					if ( item != null && item.Id > 0 )
						output.CreditUnitTypeId = item.Id;
					else
					{
						//if not get by schema
						CodeItem code = CodesManager.GetPropertyBySchema( "ceterms:CreditUnit", item.SchemaName );
						output.CreditUnitTypeId = code.Id;
					}
				}
				output.CreditUnitValue = input.CreditValue.Value;
				output.CreditUnitMaxValue = input.CreditValue.MaxValue;
				if ( input.CreditValue.MaxValue > 0 )
					output.CreditUnitValue = input.CreditValue.MinValue;
				if ( !string.IsNullOrWhiteSpace( input.CreditValue.Description ) )
					output.CreditUnitTypeDescription = input.CreditValue.Description;
			}
			//else if ( UtilityManager.GetAppKeyValue( "usingQuantitiveValue", false ) == false )
			//{

			//	//output.CreditHourType = GetData( input.CreditHourType, null );
			//	//output.CreditHourValue = SetData( input.CreditHourValue, 0.5M );
			//	//output.CreditUnitTypeId = SetData( input.CreditUnitTypeId, 1 );
			//	if ( input.CreditUnitType != null && input.CreditUnitType.HasItems() )
			//	{
			//		//get Id if available
			//		EnumeratedItem item = input.CreditUnitType.GetFirstItem();
			//		if ( item != null && item.Id > 0 )
			//			output.CreditUnitTypeId = item.Id;
			//		else
			//		{
			//			//if not get by schema
			//			CodeItem code = CodesManager.GetPropertyBySchema( "ceterms:CreditUnit", item.SchemaName );
			//			output.CreditUnitTypeId = code.Id;
			//		}
			//	}
			//	output.CreditUnitTypeDescription = GetData( input.CreditUnitTypeDescription );
			//	output.CreditUnitValue = SetData( input.CreditUnitValue, 0.5M );
			//}

			if ( IsValidDate( input.LastUpdated ) )
                output.LastUpdated = input.LastUpdated;

        }
        public static void MapFromDB( DBEntity input, ThisEntity output,
                bool includingProperties = false,
                bool includingProfiles = true,
                bool includeWhereUsed = true )
        {

            //TODO add a tomap basic, and handle for lists
            output.Id = input.Id;
            output.RowId = input.RowId;
            output.EntityStateId = ( int )( input.EntityStateId ?? 1 );

            if ( IsGuidValid( input.OwningAgentUid ) )
            {
                output.OwningAgentUid = ( Guid )input.OwningAgentUid;
                output.OwningOrganization = OrganizationManager.GetForSummary( output.OwningAgentUid );

                //get roles
                OrganizationRoleProfile orp = Entity_AgentRelationshipManager.AgentEntityRole_GetAsEnumerationFromCSV( output.RowId, output.OwningAgentUid );
                output.OwnerRoles = orp.AgentRole;
            }
			output.OrganizationRole = Entity_AssertionManager.GetAllCombinedForTarget( 7, output.Id, output.OwningOrganizationId );
			if( output.EntityStateId ==2)
			{
				if (!output.OrganizationRole.Any() && output.OwningOrganizationId > 0)
				{
					output.OrganizationRole.Add( new OrganizationRoleProfile()
					{
						ActingAgentUid = output.OwningAgentUid, 
						ActingAgent = new Organization()
						{
							Id=output.OwningOrganizationId,
							RowId=output.OwningAgentUid,
							Name=output.OwningOrganization.Name,
							SubjectWebpage=output.OwningOrganization.SubjectWebpage
						},
						AgentRole = new Enumeration() { Items = new List<EnumeratedItem>() { new EnumeratedItem()
						{
							Name="Owned By"
						} } }
					} );
				}
			}

			output.Name = input.Name;
            output.Description = input.Description == null ? "" : input.Description;
            output.SubjectWebpage = input.SubjectWebpage;
            if ( IsValidDate( input.Created ) )
                output.Created = ( DateTime )input.Created;

            if ( IsValidDate( input.LastUpdated ) )
                output.LastUpdated = ( DateTime )input.LastUpdated;
            output.CTID = input.CTID;
			//now more for references 
			var relatedEntity = EntityManager.GetEntity( output.RowId, false );
			if ( relatedEntity != null && relatedEntity.Id > 0 )
				output.EntityLastUpdated = relatedEntity.LastUpdated;

			output.Addresses = Entity_AddressManager.GetAll( output.RowId );
			output.AvailabilityListing = input.AvailabilityListing;
			output.CodedNotation = input.IdentificationCode;
			output.AvailableOnlineAt = input.AvailableOnlineAt;
			output.DeliveryTypeDescription = input.DeliveryTypeDescription;
			output.EstimatedDuration = DurationProfileManager.GetAll( output.RowId );
			//
			output.AssessmentMethodDescription = input.AssessmentMethodDescription;
			output.LearningMethodDescription = input.LearningMethodDescription;
			//=========================================================
			//populate QV
			output.CreditValue = FormatQuantitiveValue( input.CreditUnitTypeId, input.CreditUnitValue, input.CreditUnitMaxValue, input.CreditUnitTypeDescription, input.CreditHourType );

			if ( output.CreditValue.HasData() )
			{
				//pending
				output.CreditValueList.Add( output.CreditValue );
				//
				output.CreditUnitType = output.CreditValue.CreditUnitType;
				output.CreditUnitTypeId = ( input.CreditUnitTypeId ?? 0 );
				output.CreditUnitTypeDescription = output.CreditValue.Description;

				output.CreditUnitValue = output.CreditValue.Value;
				output.CreditUnitMaxValue = output.CreditValue.MaxValue;
				if ( output.CreditUnitMaxValue > 0 )
				{
					output.CreditUnitValue = output.CreditValue.MinValue;
					output.CreditUnitMinValue = output.CreditValue.MinValue;
					output.CreditValueIsRange = true;
				}

			}
			else
			{
				output.CreditUnitTypeId = ( input.CreditUnitTypeId ?? 0 );
				output.CreditUnitTypeDescription = input.CreditUnitTypeDescription;
				output.CreditUnitValue = input.CreditUnitValue ?? 0M;
				output.CreditUnitMaxValue = input.CreditUnitMaxValue ?? 0M;
				//temp handling of clock hpurs
				//output.CreditHourType = input.CreditHourType ?? "";
				//output.CreditHourValue = ( input.CreditHourValue ?? 0M );
				//if ( output.CreditHourValue > 0 )
				//{
				//	output.CreditUnitValue = output.CreditHourValue;
				//	output.CreditUnitTypeDescription = output.CreditHourType;
				//}
			}

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
			//teaches
			MapFromDB_Competencies( output );
			//20-10-05 - get as much as possible for a reference esp for TVP case
			output.AudienceType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE );
			output.AudienceLevelType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL );
			if ( IsValidDate( input.DateEffective ) )
				output.DateEffective = ( ( DateTime )input.DateEffective ).ToShortDateString();
			else
				output.DateEffective = "";
			if ( IsValidDate( input.ExpirationDate ) )
				output.ExpirationDate = ( ( DateTime )input.ExpirationDate ).ToShortDateString();
			else
				output.ExpirationDate = "";
			output.DeliveryType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE );

			output.FinancialAssistance = Entity_FinancialAssistanceProfileManager.GetAll( output.RowId, false );

			output.Jurisdiction = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( output.RowId );
			output.Keyword = Entity_ReferenceManager.GetAll( output.RowId, CodesManager.PROPERTY_CATEGORY_KEYWORD );
			output.Identifier = Entity_IdentifierValueManager.GetAll( output.RowId, Entity_IdentifierValueManager.LEARNING_OPP_Identifier );

			output.LearningMethodType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_Learning_Method_Type );
			output.AssessmentMethodType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_Assessment_Method_Type );

			output.Region = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( output.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_RESIDENT );

			output.Subject = Entity_ReferenceManager.GetAll( output.RowId, CodesManager.PROPERTY_CATEGORY_SUBJECT );
			//
			//Fix costs
			output.EstimatedCost = CostProfileManager.GetAll( output.RowId );

			//Include currencies output fix null errors in various places (detail, compare, publishing) - NA 3/17/2017
			var currencies = CodesManager.GetCurrencies();
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

			//----------------------------------------------------------------------------
			if ( string.IsNullOrWhiteSpace( output.CTID ) || output.EntityStateId < 3 )
            {
                output.IsReferenceVersion = true;
                //return;
            }

            output.CredentialRegistryId = input.CredentialRegistryId;
			//====			

			//output.VerificationMethodDescription = input.VerificationMethodDescription;

			//assumes only one identifier type per class
			//20-12-01 now two types - will need a property designatiion, or store as json
			output.VersionIdentifierList = Entity_IdentifierValueManager.GetAll( output.RowId, Entity_IdentifierValueManager.LEARNING_OPP_VersionIdentifier );

			output.Identifier = Entity_IdentifierValueManager.GetAll( output.RowId, Entity_IdentifierValueManager.LEARNING_OPP_Identifier );

			output.InLanguageCodeList = Entity_ReferenceManager.GetAll( output.RowId, CodesManager.PROPERTY_CATEGORY_LANGUAGE );

            //properties

            //get condition profiles
            List<ConditionProfile> list = new List<ConditionProfile>();

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
                    else if ( item.ConnectionProfileTypeId == Entity_ConditionProfileManager.ConnectionProfileType_EntryCondition )
                        output.EntryCondition.Add( item );
                    else
                    {
                        EmailManager.NotifyAdmin( "Unexpected Condition Profile for learning opportunity", string.Format( "LearningOppId: {0}, ConditionProfileTypeId: {1}", output.Id, item.ConnectionProfileTypeId ) );

                        //add output required, for dev only?
                        if ( IsDevEnv() )
                        {
                            item.ProfileName = ( item.ProfileName ?? "" ) + " unexpected condition type of " + item.ConnectionProfileTypeId.ToString();
                            output.Requires.Add( item );
                        }
                    }
                }
            }
            //TODO
            output.CommonConditions = Entity_CommonConditionManager.GetAll( output.RowId );

            output.CommonCosts = Entity_CommonCostManager.GetAll( output.RowId );

			output.Occupation = Reference_FrameworksManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_SOC );

			output.Industry = Reference_FrameworksManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_NAICS );

			output.InstructionalProgramType = Reference_FrameworksManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_CIP );


            output.JurisdictionAssertions = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( output.RowId, 3 );

            //TODO - re: forEditView, not sure about approach for learning opp parts
            //for now getting all, although may only need as links - except may also need output get competencies
            bool forProfilesList = false;

            MapFromDB_HasPart( output, forProfilesList );
            //         output.HasPart = Entity_LearningOpportunityManager.LearningOpps_GetAll( output.RowId, forEditView, forProfilesList );
            //foreach ( ThisEntity e in output.HasPart )
            //{
            //	if ( e.HasCompetencies || e.ChildHasCompetencies )
            //	{
            //		output.ChildHasCompetencies = true;
            //		break;
            //	}
            //}

            //16-09-02 mp - always get for now
            //really only needed for detail view
            //===> need a means output determine request is input microsearch, so only minimal is returned!
            //if ( includeWhereUsed )
            //{
            output.WhereReferenced = new List<string>();
            if ( input.Entity_LearningOpportunity != null && input.Entity_LearningOpportunity.Count > 0 )
            {
                //the Entity_LearningOpportunity could be for a parent lopp, or a condition profile
                foreach ( EM.Entity_LearningOpportunity item in input.Entity_LearningOpportunity )
                {
                    output.WhereReferenced.Add( string.Format( "EntityUid: {0}, Type: {1}", item.Entity.EntityUid, item.Entity.Codes_EntityTypes.Title ) );
                    if ( item.Entity.EntityTypeId == CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE )
                    {
                        //only if downloaded
                        var lo = GetAs_IsPartOf( item.Entity.EntityUid );
                        if ( lo.EntityStateId > 1 )
                            output.IsPartOf.Add( lo );
                    }//or better as ENTITY_TYPE_CONDITION_PROFILE
                    else if ( item.Entity.EntityTypeId == CodesManager.ENTITY_TYPE_CONNECTION_PROFILE )
                    {
                        ConditionProfile cp = CondProfileMgr.GetAs_IsPartOf( item.Entity.EntityUid );
                        output.IsPartOfConditionProfile.Add( cp );
                        //need output check cond prof for parent of credential
                        //will need output ensure no dups, or realistically, don't do the direct credential check
                        if ( cp.ParentCredential != null && cp.ParentCredential.Id > 0 )
                        {
                            if ( cp.ParentCredential.EntityStateId > 1 )
                            {
                                //output.IsPartOfCredential.Add( cp.ParentCredential );
                                AddCredentialReference( cp.ParentCredential.Id, output );
                            }
                        }
                    }
                    else if ( item.Entity.EntityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL )
                    {
                        //this is not possible in the finder
                        //AddCredentialReference( (int)item.Entity.EntityBaseId, output );
                    }
                }
            }

        }
        public static void MapFromDB_HasPart( ThisEntity to, bool forProfilesList)
        {
            to.HasPart = Entity_LearningOpportunityManager.LearningOpps_GetAll( to.RowId, forProfilesList );
            foreach ( ThisEntity e in to.HasPart )
            {
                if ( e.HasCompetencies || e.ChildHasCompetencies )
                {
                    to.ChildHasCompetencies = true;
                    break;
                }
            }
        }
        public static void MapFromDB_Competencies( ThisEntity to )
		{
            var frameworksList = new Dictionary<string, RegistryImport>();
            //not sure if this one is used anymore!!!!!!!!!!!!!!!!!!
			//20-10-06 - there will be cases, like transfer value that only competencies will be available, with no framework.

            //to.TeachesCompetencies = Entity_CompetencyManager.GetAllAs_CredentialAlignmentObjectProfile( to.RowId, ref frameworksList);
			//to.FrameworkPayloads = frameworksList;
			//if ( to.TeachesCompetencies.Count > 0 )
			//	to.HasCompetencies = true;

			to.TeachesCompetenciesFrameworks = Entity_CompetencyManager.GetAllAs_CAOFramework(to.RowId, ref frameworksList);
            if ( to.TeachesCompetenciesFrameworks.Count > 0 )
            {
                to.HasCompetencies = true;
                to.FrameworkPayloads = frameworksList;
            }

            //these should be under a condition profile???
            to.RequiresCompetenciesFrameworks = new List<CredentialAlignmentObjectFrameworkProfile>();
			//to.RequiresCompetenciesFrameworks = Entity_CompetencyManager.GetAllAs_CAOFramework( to.RowId );
			//if ( to.RequiresCompetenciesFrameworks.Count > 0 )
			//	to.HasCompetencies = true;
		}
		private static void AddCredentialReference( int credentialId, ThisEntity to )
		{
			Credential exists = to.IsPartOfCredential.FirstOrDefault( s => s.Id == credentialId );
			if ( exists == null || exists.Id == 0 )
				to.IsPartOfCredential.Add( CredentialManager.GetBasic( ( int ) credentialId ) );
		}

        /// <summary>
        /// Fill actual competencies for entity
        /// </summary>
        /// <param name="item"></param>
        /// <param name="competencyList">Contains any competencies from filters</param>
        //private static void FillCompetencies(ThisEntity item, ref List<string> competencyList)
        //{
        //    item.TeachesCompetenciesFrameworks = new List<CredentialAlignmentObjectFrameworkProfile>();
        //    var frameworksList = new Dictionary<string, RegistryImport>();
        //    //return;
        //    //TODO - not using frameworks, the latter would have flattened items to CredentialAlignmentObjectProfile, which we do have
        //    if ( competencyList.Count == 0 )
        //        item.TeachesCompetenciesFrameworks = Entity_CompetencyManager.GetAllAs_CAOFramework(item.RowId, ref frameworksList);
        //    else
        //    {

        //        List<CredentialAlignmentObjectFrameworkProfile> all = Entity_CompetencyManager.GetAllAs_CAOFramework(item.RowId, ref frameworksList);
        //        foreach ( CredentialAlignmentObjectFrameworkProfile next in all )
        //        {
        //            //just do desc for now
        //            string orig = ( next.Description ?? "" );
        //            foreach ( string filter in competencyList )
        //            {
        //                //not ideal, as would be an exact match
        //                orig = orig.Replace(filter, string.Format("<span class='highlight'>{0}<\\span>", filter));
        //            }
        //            if ( orig != ( next.Description ?? "" ) )
        //            {
        //                next.Description = orig;
        //                item.TeachesCompetenciesFrameworks.Add(next);
        //            }
        //        }
        //    }
        //}
        #endregion
    }
}

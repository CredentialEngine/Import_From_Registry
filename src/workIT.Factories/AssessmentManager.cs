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
using DBResource = workIT.Data.Tables.Assessment;
using EM = workIT.Data.Tables;
using EntityContext = workIT.Data.Tables.workITEntities;
using ThisResource = workIT.Models.ProfileModels.AssessmentProfile;
using ReferenceFrameworkItemsManager = workIT.Factories.Reference_FrameworkItemManager;
//using workIT.Data.Tables;
//using ReferenceFrameworkItemsManager = workIT.Factories.Reference_FrameworksManager;

namespace workIT.Factories
{
	public class AssessmentManager : BaseFactory
    {
        static readonly string thisClassName = "AssessmentManager";
		static string EntityType = CodesManager.ENTITY_TYPE_LABEL_ASSESSMENT_PROFILE;
		static int EntityTypeId = CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE;
		static string Entity_Label = "Assessment Profile";

		EntityManager entityMgr = new EntityManager();

        #region Assessment - persistance ==================
        /// <summary>
        /// Update a Assessment
        /// - base only, caller will handle parts?
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public bool Save( ThisResource entity, ref SaveStatus status )
        {
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
                        DBResource efEntity = context.Assessment
                                .SingleOrDefault( s => s.Id == entity.Id );

                        if ( efEntity != null && efEntity.Id > 0 )
                        {
                            //delete the entity and re-add
                            //Entity e = new Entity()
                            //{
                            //    EntityBaseId = efEntity.Id,
                            //    EntityTypeId = CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE,
                            //    EntityType = "Assessment",
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
								SiteActivity sa = new SiteActivity()
								{
									ActivityType = EntityType,
									Activity = "Import",
									Event = "Reactivate",
									Comment = string.Format( "Assessment had been marked as deleted, and was reactivted by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ),
									ActivityObjectId = entity.Id
								};
								new ActivityManager().SiteActivityAdd( sa );
							}
							//assume and validate, that if we get here we have a full record
							if ( ( efEntity.EntityStateId ?? 1 ) != 2 )
                                efEntity.EntityStateId = 3;

							entity.EntityStateId = (int)efEntity.EntityStateId;

                            if ( IsValidDate( status.EnvelopeCreatedDate ) && status.LocalCreatedDate < efEntity.Created )
							{
								efEntity.Created = status.LocalCreatedDate;
							}
							if ( IsValidDate( status.EnvelopeUpdatedDate ) && status.LocalUpdatedDate != efEntity.LastUpdated )
							{
								efEntity.LastUpdated = status.LocalUpdatedDate;
								lastUpdated = status.LocalUpdatedDate;
							}
							if ( HasStateChanged( context ) )
							{
								
								if ( IsValidDate( status.EnvelopeUpdatedDate ) )
									efEntity.LastUpdated = status.LocalUpdatedDate;
								else
									efEntity.LastUpdated = DateTime.Now;
								//NOTE efEntity.EntityStateId is set to 0 in delete method )
								count = context.SaveChanges();
								status.UpdateElasticIndex = true;
								//can be zero if no data changed
								if ( count >= 0 )
								{
									isValid = true;
								}
								else
								{
									//?no info on error
									
									isValid = false;
									string message = string.Format( thisClassName + ".Save Failed", "Attempted to update a Assessment. The process appeared to not work, but was not an exception, so we have no message, or no clue. Assessment: {0}, Id: {1}", entity.Name, entity.Id);
									status.AddError( "Error - the update was not successful. " + message );
									EmailManager.NotifyAdmin( thisClassName + ".Save Failed Failed", message );
								}
								
							}
                            else
                            {
                                //update entity.LastUpdated - assuming there has to have been some change in related data
                                new EntityManager().UpdateModifiedDate( entity.RowId, ref status, efEntity.LastUpdated );
                            }

							entity.LastUpdated = lastUpdated;
							UpdateEntityCache( entity, ref status );
							if ( isValid )
							{
								if ( !UpdateParts( entity, ref status ) )
									isValid = false;

								SiteActivity sa = new SiteActivity()
								{
									ActivityType = EntityType,
									Activity = "Import",
									Event = "Update",
									Comment = string.Format( "Assessment was updated by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ),
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
				string message = HandleDBValidationError( dbex, thisClassName + string.Format( ".Save. id: {0}, Name: {1}", entity.Id, entity.Name ), "Assessment" );
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
        /// add a Assessment
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
					if ( IsValidDate( status.EnvelopeCreatedDate )  )
					{
						efEntity.Created = status.LocalCreatedDate;
						efEntity.LastUpdated = status.LocalCreatedDate;
					}
					else
					{
						efEntity.Created = System.DateTime.Now;
						efEntity.LastUpdated = System.DateTime.Now;
					}
                    context.Assessment.Add( efEntity );

                    // submit the change to database
                    int count = context.SaveChanges();
                    if ( count > 0 )
                    {
						status.UpdateElasticIndex = true;
						entity.RowId = efEntity.RowId;
						entity.Created = ( DateTime )efEntity.Created;
						entity.LastUpdated = ( DateTime )efEntity.LastUpdated;
						entity.Id = efEntity.Id;
						//
						UpdateEntityCache( entity, ref status );
						//add log entry
						SiteActivity sa = new SiteActivity()
                        {
                            ActivityType = EntityType,
                            Activity = "Import",
                            Event = "Add",
                            Comment = string.Format( "Full Assessment was added by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ),
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

                        string message = thisClassName + string.Format( ". Add Failed", "Attempted to add a Assessment. The process appeared to not work, but was not an exception, so we have no message, or no clue. Assessment: {0}, ctid: {1}", entity.Name, entity.CTID );
                        status.AddError( thisClassName + ". Error - the add was not successful. " + message );
                        EmailManager.NotifyAdmin( "AssessmentManager. Add Failed", message );
                    }
                }
                catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
                {
                    string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", "Assessment" );
                    status.AddError( thisClassName + ".Add(). Error - the save was not successful. " + message );

                    LoggingHelper.LogError( dbex, message );
                }
                catch ( Exception ex )
                {
                    string message = FormatExceptions( ex );
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Add(), Name: {0}, CTID: {1}, OwningAgentUid: {2}", efEntity.Name, efEntity.CTID, efEntity.OwningAgentUid ) );
					status.AddError( thisClassName + ".Add(). Error - the save was not successful. \r\n" + message );
                }
            }

			return efEntity.Id;
		}
		/// <summary>
		/// Add a reference record - where this entity was referenced in a blank node
		/// </summary>
		/// <param name="entity">Reference Assessment</param>
		/// <param name="status"></param>
		/// <returns></returns>
		public int AddReferenceAssessment( ThisResource entity, ref SaveStatus status )
		{
			DBResource efEntity = new DBResource();
			try
			{
				using ( var context = new EntityContext() )
				{
					if ( entity == null ||
						( string.IsNullOrWhiteSpace( entity.Name ) )
						//||                        string.IsNullOrWhiteSpace( entity.SubjectWebpage )) 
						)
					{
						status.AddError( thisClassName + ". AddReferenceAssessment() The assessment is incomplete" );
						return 0;
					}

					//only add DB required properties
					//NOTE - an entity will be created via trigger
					//23-10-23 - ahh - now reference resources can have a lot of info. maybe just use 
					MapToDB( entity, efEntity );

					efEntity.EntityStateId = entity.EntityStateId = 2;
					//efEntity.Name = entity.Name;
					//efEntity.Description = entity.Description;
					//efEntity.SubjectWebpage = entity.SubjectWebpage;
					////
					//efEntity.AssessmentMethodDescription = entity.AssessmentMethodDescription;
					//efEntity.IdentificationCode = entity.CodedNotation;
					//efEntity.LearningMethodDescription = entity.LearningMethodDescription;
					//
					if ( IsValidGuid( entity.RowId ) )
						efEntity.RowId = entity.RowId;
					else
						efEntity.RowId = Guid.NewGuid();
                    //set to return, just in case
                    entity.RowId = efEntity.RowId;
					//set to active 
					var defStatus = CodesManager.GetLifeCycleStatus( CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS_ACTIVE );
					efEntity.LifeCycleStatusTypeId = defStatus.Id;

					//
					efEntity.Created = System.DateTime.Now;
					efEntity.LastUpdated = System.DateTime.Now;

					context.Assessment.Add( efEntity );
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						SiteActivity sa = new SiteActivity()
						{
							ActivityType = EntityType,
							Activity = "Import",
							Event = "Add Base Reference",
							Comment = string.Format( "Reference Assessment was added by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ),
							ActivityObjectId = efEntity.Id
						};
						new ActivityManager().SiteActivityAdd( sa );
						//
						entity.Id = efEntity.Id;
						entity.RowId = efEntity.RowId;
						entity.Created = ( DateTime )efEntity.Created;
						entity.LastUpdated = ( DateTime )efEntity.LastUpdated;
						UpdateEntityCache( entity, ref status );
						//
						if ( UpdateParts( entity, ref status ) == false )
						{

						}
						return efEntity.Id;
					}

					status.AddError( thisClassName + ". AddReferenceAssessment() Error - the save was not successful, but no message provided. " );
				}
			}
			catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
			{
				status.AddError( HandleDBValidationError( dbex, thisClassName + ".AddReferenceAssessment() ", "Assessment" )) ;
				LoggingHelper.LogError( dbex, thisClassName + string.Format( ".Add(), Name: {0}, SWP: '{1}'", entity.Name, entity.SubjectWebpage ) );


			}
			catch ( Exception ex )
			{
				string message = FormatExceptions( ex );
				LoggingHelper.LogError( ex, thisClassName + string.Format( ".AddReferenceAssessment. Name:  {0}, SubjectWebpage: {1}", entity.Name, entity.SubjectWebpage ) );
				status.AddError( thisClassName + ". AddBaseReference() Error - the save was not successful. " + message );

			}
			return 0;
		}

		/// <summary>
		/// Add a pending record - where this entity was referenced with a CTID
		/// </summary>
		/// <param name="entityUid"></param>
		/// <param name="ctid"></param>
		/// <param name="registryAtId"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public int AddPendingRecord( Guid entityUid, string ctid, string registryAtId, ref SaveStatus status )
		{
			DBResource efEntity = new DBResource();
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
					var entity = GetSummaryByCtid( ctid );
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
					//set to active 
					var defStatus = CodesManager.GetLifeCycleStatus( CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS_ACTIVE );
					efEntity.LifeCycleStatusTypeId = defStatus.Id;
					context.Assessment.Add( efEntity );
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
						//not sure pendings should be put in cache yet? 
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
				EntityType = EntityType, //23-04-02 Using AssessmentProfile for consistency with activity log and elsewhere
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
            //var defStatus = CodesManager.GetLifeCycleStatus( CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS_ACTIVE );
            var ceasedStatus = CodesManager.GetLifeCycleStatus( CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS_CEASED );
            if ( document.LifeCycleStatusTypeId > 0 && document.LifeCycleStatusTypeId == ceasedStatus.Id)
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
                status.AddError( "An Assessment name must be entered" );
            }
            if ( string.IsNullOrWhiteSpace( profile.Description ) )
            {
                //status.AddWarning( "An Assessment Description must be entered" );
            }
            if ( !IsValidGuid( profile.PrimaryAgentUID ) )
            {
                //status.AddWarning( "An owning organization must be selected" );
            }
            if ( !string.IsNullOrWhiteSpace( profile.DateEffective ) && !IsValidDate( profile.DateEffective ) )
            {
                //status.AddWarning( "Invalid Assessment effective date" );
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
			//if ( string.IsNullOrWhiteSpace( profile.SubjectWebpage ) )
			//    status.AddWarning( "Error - A Subject Webpage name must be entered" );

			//else if ( !IsUrlValid( profile.SubjectWebpage, ref commonStatusMessage ) )
			//{
			//    //status.AddWarning( "The Assessment Subject Webpage is invalid. " + commonStatusMessage );
			//}

			//if ( !IsUrlValid( profile.AvailableOnlineAt, ref commonStatusMessage ) )
			//{
			//    //status.AddError( "The Available Online At Url is invalid. " + commonStatusMessage );
			//}

			//if ( !IsUrlValid( profile.AvailabilityListing, ref commonStatusMessage ) )
			//{
			//    //status.AddWarning( "The Availability Listing Url is invalid. " + commonStatusMessage );
			//}

			//if ( !IsUrlValid( profile.ExternalResearch, ref commonStatusMessage ) )
			//    status.AddWarning( "The External Research Url is invalid. " + commonStatusMessage );
			//if ( !IsUrlValid( profile.ProcessStandards, ref commonStatusMessage ) )
			//    status.AddWarning( "The Process Standards Url is invalid. " + commonStatusMessage );
			//if ( !IsUrlValid( profile.ScoringMethodExample, ref commonStatusMessage ) )
			//    status.AddWarning( "The Scoring Method Example Url is invalid. " + commonStatusMessage );
			//if ( !IsUrlValid( profile.AssessmentExample, ref commonStatusMessage ) )
			//    status.AddWarning( "The Assessment Example Url is invalid. " + commonStatusMessage );


			//if ( profile.CreditHourValue < 0 || profile.CreditHourValue > 10000 )
			//    status.AddWarning( "Error: invalid value for Credit Hour Value. Must be a reasonable decimal value greater than zero." );

			//if ( profile.CreditUnitValue < 0 || profile.CreditUnitValue > 1000 )
			//    status.AddWarning( "Error: invalid value for Credit Unit Value. Must be a reasonable decimal value greater than zero." );


			//can only have credit hours properties, or credit unit properties, not both
			//bool hasCreditHourData = false;
			//bool hasCreditUnitData = false;
			/*
            if ( profile.CreditUnitTypeId > 0
                || ( profile.CreditUnitTypeDescription ?? string.Empty ).Length > 0
                || profile.CreditUnitValue > 0 )
                hasCreditUnitData = true;

            if ( hasCreditHourData && hasCreditUnitData )
                status.AddWarning( "Error: Data can be entered for Credit Hour related properties or Credit Unit related properties, but not for both." );
			*/

			return status.WasSectionValid;
        }


		/// <summary>
		/// Delete an Assessment, and related Entity
		/// </summary>
		/// <param name="id"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		//public bool Delete( int id, ref string statusMessage )
		//{
		//	bool isValid = false;
		//	if ( id == 0 )
		//	{
		//		statusMessage = "Error - missing an identifier for the Assessment";
		//		return false;
		//	}
		//	int orgId = 0;
		//	Guid orgUid = Guid.Empty;
		//	using ( var context = new EntityContext() )
		//	{
		//		try
		//		{
		//			context.Configuration.LazyLoadingEnabled = false;
		//			DBResource efEntity = context.Assessment
		//						.SingleOrDefault( s => s.Id == id );

		//			if ( efEntity != null && efEntity.Id > 0 )
		//			{
		//				if ( IsValidGuid( efEntity.OwningAgentUid ) )
		//				{
		//					var org = OrganizationManager.GetForSummary( ( Guid )efEntity.OwningAgentUid );
		//					if ( org != null && org.Id > 0 )
		//					{
		//						orgId = org.Id;
		//						orgUid = org.RowId;
		//					}
		//				}
		//				//need to remove from Entity.
		//				//could use a pre-delete trigger?
		//				//what about roles

		//				context.Assessment.Remove( efEntity );
		//				int count = context.SaveChanges();
		//				if ( count > 0 )
		//				{
		//					isValid = true;
		//					//add pending delete request 
		//					List<String> messages = new List<string>();
		//					new SearchPendingReindexManager().AddDeleteRequest( CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, id, ref messages );
		//					//
		//					new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, orgId, 1, ref messages );
		//					//also check for any relationships
		//					new Entity_AgentRelationshipManager().ReindexAgentForDeletedArtifact( orgUid );
		//				}
		//			}
		//			else
		//			{
		//				statusMessage = "Error - Assessment_Delete failed, as record was not found.";
		//			}
		//		}
		//		catch ( Exception ex )
		//		{
		//			statusMessage = FormatExceptions( ex );
		//			LoggingHelper.LogError( ex, thisClassName + string.Format( ".Delete(id: {0})", id ) );

		//			if ( statusMessage.ToLower().IndexOf( "the delete statement conflicted with the reference constraint" ) > -1 )
		//			{
		//				statusMessage = "Error: this assessment cannot be deleted as it is being referenced by other items, such as roles or credentials. These associations must be removed before this assessment can be deleted.";
		//			}
		//		}
		//	}
		//	return isValid;
		//}

		public bool Delete( string ctid, ref string statusMessage )
		{
			bool isValid = true;
            if ( string.IsNullOrWhiteSpace( ctid ) )
            {
                statusMessage = thisClassName + ".Delete() Error - a valid CTID must be provided";
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
					DBResource efEntity = context.Assessment
                                .FirstOrDefault( s =>  s.CTID == ctid );

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
						string msg = string.Format( " Assessment. Id: {0}, Name: {1}, Ctid: {2}.", efEntity.Id, efEntity.Name, efEntity.CTID );
                        //18-04-05 mparsons - change to set inactive, and notify - seems to have been some incorrect deletes
                        //context.Assessment.Remove( efEntity );
                        efEntity.EntityStateId = 0;
                        efEntity.LastUpdated = System.DateTime.Now;
                        int count = context.SaveChanges();
						if ( count > 0 )
						{
                            new ActivityManager().SiteActivityAdd( new SiteActivity()
                            {
                                ActivityType = EntityType,
                                Activity = "Import",
                                Event = "Delete",
								Comment = msg,
								ActivityObjectId = efEntity.Id,
								ActivityObjectCTID = efEntity.CTID
							} );
                            isValid = true;
							//delete cache
							new EntityManager().EntityCacheDelete( rowId, ref statusMessage );

							//add pending request 
							List<String> messages = new List<string>();
                            new SearchPendingReindexManager().AddDeleteRequest( CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, efEntity.Id, ref messages );
							//mark owning org for updates (actually should be covered by ReindexAgentForDeletedArtifact
							new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, orgId, 1, ref messages );

							//delete all relationships
							workIT.Models.SaveStatus status = new SaveStatus();
							Entity_AgentRelationshipManager earmgr = new Entity_AgentRelationshipManager();
							earmgr.DeleteAll( rowId, ref status );
							//also check for any relationships
							//There could be other orgs from relationships to be reindexed as well!

							//also check for any relationships
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
					LoggingHelper.LogError( ex, thisClassName + string.Format(".Delete(ctid:{0})", ctid) );
					isValid = false;
					statusMessage = FormatExceptions( ex );
					if ( statusMessage.ToLower().IndexOf( "the delete statement conflicted with the reference constraint" ) > -1 )
					{
						statusMessage = "Error: this assessment cannot be deleted as it is being referenced by other items, such as roles or credentials. These associations must be removed before this assessment can be deleted.";
					}
				}
			}
			return isValid;
		}

		#region Assessment properties ===================
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

            Entity_ReferenceManager erm = new Entity_ReferenceManager();
            erm.DeleteAll( relatedEntity, ref status );
            if ( erm.Add( entity.Subject, entity.RowId, CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, ref status, CodesManager.PROPERTY_CATEGORY_SUBJECT, false ) == false )
                isAllValid = false;

            if ( erm.Add( entity.Keyword, entity.RowId, CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, ref status, CodesManager.PROPERTY_CATEGORY_KEYWORD, false ) == false )
                isAllValid = false;
			if ( erm.Add( entity.AlternateNames, entity.RowId, CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, ref status, CodesManager.PROPERTY_CATEGORY_ALTERNATE_NAME, false ) == false )
				isAllValid = false;

			erm.AddLanguages( entity.InLanguageCodeList, entity.RowId, CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, ref status, CodesManager.PROPERTY_CATEGORY_LANGUAGE );
			//
			erm.Add( entity.SameAs, entity.RowId, CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, ref status, CodesManager.PROPERTY_CATEGORY_SAME_AS, true );

			AddProfiles( entity, relatedEntity, ref status );
            //
            var ehssMgr = new Entity_HasSupportServiceManager();
            ehssMgr.Update( entity.HasSupportServiceIds, relatedEntity, ref status );


            UpdateAssertedBys( entity, ref status );

			UpdateAssertedIns( entity, ref status );
			//Entity_HasResource
			var eHasResourcesMgr = new Entity_HasResourceManager();
			eHasResourcesMgr.DeleteAll( relatedEntity, ref status );
			// Transfer Value 
			//Hmm - why is being handling like this?
			if ( eHasResourcesMgr.SaveList( relatedEntity, CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, entity.ProvidesTVForIds, ref status, Entity_HasResourceManager.HAS_RESOURCE_TYPE_ProvidesTransferValueFor ) == false )
				isAllValid = false;
			if ( eHasResourcesMgr.SaveList( relatedEntity, CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, entity.ReceivesTVFromIds, ref status, Entity_HasResourceManager.HAS_RESOURCE_TYPE_ReceivesTransferValueFrom ) == false )
				isAllValid = false;
			//
			if ( eHasResourcesMgr.SaveList( relatedEntity, CodesManager.ENTITY_TYPE_RUBRIC, entity.HasRubricIds, ref status, Entity_HasResourceManager.HAS_RESOURCE_TYPE_HasResource ) == false )
				isAllValid = false;
			return isAllValid;
        }

        public bool UpdateProperties( ThisResource entity, Entity relatedEntity, ref SaveStatus status )
        {
            bool isAllValid = true;
            EntityPropertyManager mgr = new EntityPropertyManager();
            //first clear all propertiesd
            mgr.DeleteAll( relatedEntity, ref status );
            Entity_ReferenceManager erm = new Entity_ReferenceManager();

            if ( mgr.AddProperties( entity.AssessmentMethodType, entity.RowId, CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, CodesManager.PROPERTY_CATEGORY_Assessment_Method_Type, false, ref status ) == false )
                isAllValid = false;

            if ( mgr.AddProperties( entity.AssessmentUseType, entity.RowId, CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, CodesManager.PROPERTY_CATEGORY_ASSESSMENT_USE_TYPE, false, ref status ) == false )
                isAllValid = false;

            if ( mgr.AddProperties( entity.DeliveryType, entity.RowId, CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE, false, ref status ) == false )
                isAllValid = false;

            if ( mgr.AddProperties( entity.ScoringMethodType, entity.RowId, CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, CodesManager.PROPERTY_CATEGORY_Scoring_Method, false, ref status ) == false )
                isAllValid = false;

			if ( mgr.AddProperties( entity.AudienceLevelType, entity.RowId, CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL, false, ref status ) == false )
				isAllValid = false;

			if ( mgr.AddProperties( entity.AudienceType, entity.RowId, CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE, false, ref status ) == false )
                isAllValid = false;


			if ( mgr.AddProperties( entity.ScheduleFrequencyType, entity.RowId, CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, CodesManager.PROPERTY_CATEGORY_SCHEDULE_FREQUENCY, false, ref status ) == false )
				isAllValid = false;

			if ( mgr.AddProperties( entity.ScheduleTimingType, entity.RowId, CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, CodesManager.PROPERTY_CATEGORY_SCHEDULE_TIMING, false, ref status ) == false )
				isAllValid = false;

			if ( mgr.AddProperties( entity.OfferFrequencyType, entity.RowId, CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, CodesManager.PROPERTY_CATEGORY_OFFER_FREQUENCY, false, ref status ) == false )
				isAllValid = false;
			return isAllValid;
        }

		public void AddProfiles( ThisResource entity, Entity relatedEntity, ref SaveStatus status )
		{
			//DurationProfile
			DurationProfileManager dpm = new Factories.DurationProfileManager();
			dpm.SaveList( entity.EstimatedDuration, entity.RowId, ref status );

			//Identifiers - do delete for first one and then assign
			new Entity_IdentifierValueManager().SaveList( entity.Identifier, entity.RowId, Entity_IdentifierValueManager.IdentifierValue_Identifier, ref status, true );
			//VersionIdentifier - no delete
			new Entity_IdentifierValueManager().SaveList( entity.VersionIdentifierList, entity.RowId, Entity_IdentifierValueManager.IdentifierValue_VersionIdentifier, ref status, false );

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
				emanager.SaveList( entity.CoPrerequisite, Entity_ConditionProfileManager.ConnectionProfileType_CoPrerequisite, entity.RowId, ref status );

				emanager.SaveList( entity.EntryCondition, Entity_ConditionProfileManager.ConnectionProfileType_EntryCondition, entity.RowId, ref status );

				//Connections
				emanager.SaveList( entity.IsAdvancedStandingFor, Entity_ConditionProfileManager.ConnectionProfileType_AdvancedStandingFor, entity.RowId, ref status, 3 );
				emanager.SaveList( entity.AdvancedStandingFrom, Entity_ConditionProfileManager.ConnectionProfileType_AdvancedStandingFrom, entity.RowId, ref status, 3 );
				emanager.SaveList( entity.IsPreparationFor, Entity_ConditionProfileManager.ConnectionProfileType_PreparationFor, entity.RowId, ref status, 3 );
				emanager.SaveList( entity.PreparationFrom, Entity_ConditionProfileManager.ConnectionProfileType_PreparationFrom, entity.RowId, ref status, 3 );
				emanager.SaveList( entity.IsRequiredFor, Entity_ConditionProfileManager.ConnectionProfileType_NextIsRequiredFor, entity.RowId, ref status, 3 );
				emanager.SaveList( entity.IsRecommendedFor, Entity_ConditionProfileManager.ConnectionProfileType_NextIsRecommendedFor, entity.RowId, ref status, 3 );
			}
			catch ( Exception ex )
			{
				string message = FormatExceptions( ex );
				LoggingHelper.LogError( ex, thisClassName + string.Format( ".AddProfiles() - ConditionProfiles. id: {0}", entity.Id ) );
				status.AddWarning( thisClassName + ".AddProfiles(). Exceptions encountered handling ConditionProfiles. " + message );
			}

			//ProcessProfile =====================================
			Entity_ProcessProfileManager ppm = new Factories.Entity_ProcessProfileManager();
            ppm.DeleteAll( relatedEntity, ref status );

            ppm.SaveList( entity.AdministrationProcess, Entity_ProcessProfileManager.ADMIN_PROCESS_TYPE, entity.RowId, ref status );
			ppm.SaveList( entity.DevelopmentProcess, Entity_ProcessProfileManager.DEV_PROCESS_TYPE, entity.RowId, ref status );
			ppm.SaveList( entity.MaintenanceProcess, Entity_ProcessProfileManager.MTCE_PROCESS_TYPE, entity.RowId, ref status );

			//Financial Alignment 
			//Entity_FinancialAlignmentProfileManager fapm = new Factories.Entity_FinancialAlignmentProfileManager();
			//fapm.SaveList( entity.FinancialAssistanceOLD, entity.RowId, ref status );

			new Entity_FinancialAssistanceProfileManager().SaveList( entity.FinancialAssistance, entity.RowId, ref status );
			//
			Entity_AssessmentManager eam = new Entity_AssessmentManager();
			eam.DeleteAll( relatedEntity, ref status );
			var newId = 0;
			if ( entity.TargetAssessmentIds != null && entity.TargetAssessmentIds.Count > 0 )
			{
				foreach ( int id in entity.TargetAssessmentIds )
				{
					newId = eam.Add( entity.RowId, id, BaseFactory.RELATIONSHIP_TYPE_HAS_TARGET_RESOURCE, true, ref status );
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

            //competencies
            //Note the list could include multiple frameworks
            Entity_CompetencyManager ecm = new Entity_CompetencyManager();

			//23-01-08 mp - do delete from entity to handle multiple types
			ecm.DeleteAll( relatedEntity, ref status );
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

			//eventually
			var adpm = new Entity_AggregateDataProfileManager();
			if ( adpm.SaveList( entity.AggregateData, relatedEntity, ref status ) == false )
				status.HasSectionErrors = true;
		} //

		public bool UpdateAssertedBys( ThisResource entity, ref SaveStatus status )
		{
			bool isAllValid = true;
			Entity_AgentRelationshipManager mgr = new Entity_AgentRelationshipManager();
			Entity relatedEntity = EntityManager.GetEntity( entity.RowId );
			if ( relatedEntity == null || relatedEntity.Id == 0 )
			{
				status.AddError( thisClassName + " - Error - the parent entity was not found." );
				return false;
			}

            //do deletes - should this be done here, should be no other prior updates?
            mgr.DeleteAll( relatedEntity, ref status );

            mgr.SaveList( relatedEntity.Id, Entity_AgentRelationshipManager.ROLE_TYPE_AccreditedBy, entity.AccreditedBy, ref status );
			mgr.SaveList( relatedEntity.Id, Entity_AgentRelationshipManager.ROLE_TYPE_ApprovedBy, entity.ApprovedBy, ref status );
			mgr.SaveList( relatedEntity.Id, Entity_AgentRelationshipManager.ROLE_TYPE_OFFERED_BY, entity.OfferedBy, ref status );
			mgr.SaveList( relatedEntity.Id, Entity_AgentRelationshipManager.ROLE_TYPE_OWNER, entity.OwnedBy, ref status );
			mgr.SaveList( relatedEntity.Id, Entity_AgentRelationshipManager.ROLE_TYPE_RecognizedBy, entity.RecognizedBy, ref status );
            mgr.SaveList( relatedEntity.Id, Entity_AgentRelationshipManager.ROLE_TYPE_RegulatedBy, entity.RegulatedBy, ref status );
			//
			mgr.SaveList( relatedEntity.Id, Entity_AgentRelationshipManager.ROLE_TYPE_PUBLISHEDBY, entity.PublishedBy, ref status );
			return isAllValid;
		} //

		public void UpdateAssertedIns( ThisResource entity, ref SaveStatus status )
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
		//get absolute minimum, typically to get the id for a full get by Id

		public static ThisResource GetSummaryByCtid( string ctid )
        {
            ThisResource entity = new ThisResource();
			if ( string.IsNullOrWhiteSpace( ctid ) )
				return entity;
			using ( var context = new EntityContext() )
            {
                DBResource from = context.Assessment
                        .FirstOrDefault( s => s.CTID.ToLower() == ctid.ToLower().Trim() );

				if ( from != null && from.Id > 0 )
				{
					entity.RowId = from.RowId;
					entity.Id = from.Id;
					entity.EntityStateId = ( int ) ( from.EntityStateId ?? 1 );
					entity.Name = from.Name;
					entity.Description = from.Description;
					entity.SubjectWebpage = from.SubjectWebpage;
					entity.CTID = from.CTID;
					entity.CredentialRegistryId = from.CredentialRegistryId;
				}
			}

			return entity;
		}
		public static ThisResource GetMinimumByGUID( Guid identifier )
		{
			ThisResource entity = new ThisResource();
			using ( var context = new EntityContext() )
			{
				DBResource from = context.Assessment
						.FirstOrDefault( s => s.RowId == identifier);

				if ( from != null && from.Id > 0 )
				{
					entity.RowId = from.RowId;
					entity.Id = from.Id;
					entity.EntityStateId = ( int ) ( from.EntityStateId ?? 1 );
					entity.Name = from.Name;
					entity.Description = from.Description;
					entity.SubjectWebpage = from.SubjectWebpage;
					entity.CTID = from.CTID;
					entity.CredentialRegistryId = from.CredentialRegistryId;
				}
			}

			return entity;
		}
		public static ThisResource GetBySubjectWebpage( string swp )
		{
			ThisResource entity = new ThisResource();
			using ( var context = new EntityContext() )
			{
				context.Configuration.LazyLoadingEnabled = false;
				DBResource from = context.Assessment
						.FirstOrDefault( s => s.SubjectWebpage.ToLower() == swp.ToLower() );

				if ( from != null && from.Id > 0 )
				{
					entity.RowId = from.RowId;
					entity.Id = from.Id;
					entity.Name = from.Name;
					entity.EntityStateId = ( int ) ( from.EntityStateId ?? 1 );
					entity.Description = from.Description;
					entity.SubjectWebpage = from.SubjectWebpage;

					entity.CTID = from.CTID;
					entity.CredentialRegistryId = from.CredentialRegistryId;
				}
			}
			return entity;
		}
		public static ThisResource GetByName_SubjectWebpage(string name, string swp, string codedNotation, Guid primaryAgentUId )
		{
			ThisResource entity = new ThisResource();
			if (string.IsNullOrWhiteSpace(swp))
				return null;
			if (swp.IndexOf("//") == -1)
				return null;
			bool hasHttps = false;
			if (swp.ToLower().IndexOf("https:") > -1)
				hasHttps = true;

			//swp = swp.Substring( swp.IndexOf( "//" ) + 2 );
			//swp = swp.ToLower().TrimEnd( '/' );

			var includingName = true;
			var nameMod = name.Replace( "&amp;", "and" );
			nameMod = nameMod.Replace( " & ", " and " );
			var host = new Uri(swp).Host;
			var domain = host.Substring(host.LastIndexOf('.', host.LastIndexOf('.') - 1) + 1);
			//DBResource from = new DBResource();
			using (var context = new EntityContext())
			{
				//s.Name.ToLower() == name.ToLower() && 
				context.Configuration.LazyLoadingEnabled = false;
				var query = context.Assessment
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
				//var list = context.Assessment
				//		.Where(s => s.SubjectWebpage.ToLower().Contains(domain) && s.EntityStateId > 1)
				//		.OrderByDescending(s => s.EntityStateId)
				//		.ThenBy(s => s.Name)
				//		.ToList();
				int cntr = 0;

				ActivityManager amgr = new ActivityManager();
				foreach (var from in list)
				{
					cntr++;
					//any way to check further?
					//the full org will be returned first
					//may want a secondary check and send notifications if additional full orgs found, or even if multiples are found.
					if (from.Name.ToLower().Contains(name.ToLower())
					|| name.ToLower().Contains(from.Name.ToLower())
					)
					{
						//OK, take me
						if (cntr == 1 || entity.Id == 0)
						{
							//hmmm if input was https and found http, and a reference, should update to https!
							if (hasHttps && from.SubjectWebpage.StartsWith("http:"))
							{

							}
							//
							MapFromDB(from, entity, true, true, true, false);
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
                                    Comment = $"{Entity_Label} Get by Name and subject webpage. Found additional full {EntityType} for name: {name}, swp: {swp}. First {EntityType}: {entity.Name} ({entity.Id})"
                                } );

                            }
							MapFromDB(from, entity, true, true, true, false);
							break;
						}
					}
				}
			}

			return entity;
			//using (var context = new EntityContext())
			//{
			//    context.Configuration.LazyLoadingEnabled = false;
			//    DBResource from = context.Assessment
			//            .FirstOrDefault(s => s.Name.ToLower() == name.ToLower() && s.SubjectWebpage == swp);

			//    if (from != null && from.Id > 0)
			//    {
			//        //    entity.RowId = from.RowId;
			//        //    entity.Id = from.Id;
			//        //    entity.Name = from.Name;
			//        //    entity.EntityStateId = ( int )( from.EntityStateId ?? 1 );
			//        //    entity.Description = from.Description;
			//        //    entity.SubjectWebpage = from.SubjectWebpage;

			//        //    entity.CTID = from.CTID;
			//        //    entity.CredentialRegistryId = from.CredentialRegistryId;
			//        MapFromDB(from, entity,
			//               true, //includingProperties
			//               true, //includingRoles
			//               true,
			//               false);
			//    }
			//}
			//return entity;
		}

		public static ThisResource FindReferenceResource( string name, string description, string codedNotation, Guid primaryorgUId )
		{
			ThisResource entity = new ThisResource();
			using ( var context = new EntityContext() )
			{
				var nameMod = name.Replace( "&amp;", "and" );
				nameMod = nameMod.Replace( " & ", " and " );
				context.Configuration.LazyLoadingEnabled = false;
				var query = context.Assessment
				   .Where( s => s.EntityStateId > 1 
							&& s.OwningAgentUid == primaryorgUId 
							&& ( s.Name.ToLower() == name.ToLower() || s.Name.ToLower() == nameMod.ToLower() )
				   );
				//query = query.Where( s => s.Name.ToLower() == name.ToLower() || s.Name.ToLower() == nameMod.ToLower() );
				if ( !string.IsNullOrWhiteSpace( description ) )
				{
					query = query.Where( s => s.Description.ToLower() == description.ToLower() );
				}
				if ( !string.IsNullOrWhiteSpace( codedNotation ) )
				{
					query = query.Where( s => s.IdentificationCode.ToLower() == codedNotation.ToLower() );
				}
				var list = query.OrderByDescending( s => s.EntityStateId ).ThenBy( s => s.Name ).ToList();
				//var list = context.Assessment
				//		.Where( s => s.EntityStateId > 1 && s.OwningAgentUid == primaryorgUId )
				//		.OrderByDescending( s => s.EntityStateId )
				//		.ThenBy( s => s.Name )
				//		.ToList();
				int cntr = 0;

				ActivityManager amgr = new ActivityManager();
				foreach ( var from in list )
				{
					cntr++;
					//any way to check further?
					//the full org will be returned first
					//may want a secondary check and send notifications if additional full orgs found, or even if multiples are found.
					if ( from.Name.ToLower().Contains( name.ToLower() )
					|| name.ToLower().Contains( from.Name.ToLower() )
					)
					{
						//OK, take me
						if ( cntr == 1 || entity.Id == 0 )
						{
							MapFromDB( from, entity, true, true, true, false );
						}
						else
						{
							if ( from.EntityStateId == 3 )
							{
								//could log warning conditions to activity log, and then report out at end of an import?
								amgr.SiteActivityAdd( new SiteActivity()
								{
									ActivityType = "System",
									Activity = "Import",
									Event = $"{EntityType} Reference Check",
									Comment = $"{Entity_Label} Get by Name and PrimaryOrg. Found additional full {EntityType} for name: {name}, PrimaryAgentUId: {primaryorgUId}. First {EntityType}: {entity.Name} ({entity.Id})"
								} );

							}
							MapFromDB( from, entity, true, true, true, false );
							break;
						}
					}
				}
			}

			return entity;
		}
		public static ThisResource GetBasic( int id, bool includingCompetencies = false )
		{
			ThisResource entity = new ThisResource();
			using ( var context = new EntityContext() )
			{
				DBResource item = context.Assessment
						.SingleOrDefault( s => s.Id == id );

                if ( item != null && item.Id > 0 )
                {
                    MapFromDB_Basic( item, entity, false, false, includingCompetencies );
                }
            }

            return entity;
        }

		public static List<ThisResource> GetAllForLinkChecker( ref int totalRecords, int maxRecords = 100 )
		{
			List<ThisResource> list = new List<ThisResource>();
			ThisResource entity = new ThisResource();
			using ( var context = new EntityContext() )
			{
				List<DBResource> results = context.Assessment
							 .Where( s => s.EntityStateId > 2 )
							 .OrderBy( s => s.Name )
							 .ToList();
				if ( results != null && results.Count > 0 )
				{
					totalRecords = results.Count();

					foreach ( DBResource item in results )
					{
						entity = new ThisResource();
						MapFromDB_Basic( item, entity, true, false, false );
						list.Add( entity );
						if ( maxRecords > 0 && list.Count >= maxRecords )
							break;
					}
				}
			}

			return list;
		}
		public static ThisResource GetForDetail( int id, AssessmentRequest request )
        {
            ThisResource entity = new ThisResource();
            using ( var context = new EntityContext() )
            {
                DBResource item = context.Assessment
                        .SingleOrDefault( s => s.Id == id );

                if ( item != null && item.Id > 0 )
                {
					//check for virtual deletes
					if ( item.EntityStateId == 0 )
					{
						LoggingHelper.DoTrace( 1, string.Format( thisClassName + " Error: encountered request for detail for a deleted record. Name: {0}, CTID:{1}", item.Name, item.CTID ) );
						entity.Name = "Assessment was not found.";
						entity.CTID = item.CTID;
						entity.EntityStateId = 0;
						return entity;
					}

					MapFromDB( item, entity, request);
                }
            }

            return entity;
        }


        public static List<string> Autocomplete( string pFilter, int pageNumber, int pageSize, ref int pTotalRows )
        {
            bool autocomplete = true;
            var results = new List<string>();
            List<string> competencyList = new List<string>();
            //ref competencyList, 
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

                using ( SqlCommand command = new SqlCommand( "[Assessment_Search]", c ) )
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
                    catch (Exception ex)
                    {
                        pTotalRows = 0;
                        LoggingHelper.LogError(ex, thisClassName + string.Format(".Search() - Execute proc, Message: {0} \r\n Filter: {1} \r\n", ex.Message, pFilter));

                        item = new AssessmentProfile();
                        item.Name = "Unexpected error encountered. System administration has been notified. Please try again later. ";
                        item.Description = ex.Message;

                        list.Add(item);
                        return list;
                    }
                }

                foreach ( DataRow dr in result.Rows )
                {
                    item = new ThisResource();
                    item.Id = GetRowColumn( dr, "Id", 0 );
                    item.Name = GetRowColumn( dr, "Name", "missing" );
                    item.FriendlyName = FormatFriendlyTitle( item.Name );
                    item.CTID = GetRowPossibleColumn( dr, "CTID", string.Empty );
                    item.EntityStateId = GetRowColumn( dr, "EntityStateId", 3 );

                    item.Description = GetRowColumn( dr, "Description", string.Empty );
                    string rowId = GetRowColumn( dr, "RowId" );
                    item.RowId = new Guid( rowId );

                    item.SubjectWebpage = GetRowColumn( dr, "SubjectWebpage", string.Empty );
                    item.AvailableOnlineAt = GetRowPossibleColumn( dr, "AvailableOnlineAt", string.Empty );

                    item.CodedNotation = GetRowColumn( dr, "IdentificationCode", string.Empty );
                   
                    item.CredentialRegistryId = GetRowPossibleColumn( dr, "CredentialRegistryId", string.Empty );
                    //if autocomplete, skip rest
                    if ( autocomplete )
                    {
                        list.Add( item );
                        continue;
                    }

                    item.RequiresCount = GetRowPossibleColumn( dr, "RequiresCount", 0 );
                    item.RecommendsCount = GetRowPossibleColumn( dr, "RecommendsCount", 0 );
                    item.RequiredForCount = GetRowPossibleColumn( dr, "IsRequiredForCount", 0 );
                    item.IsRecommendedForCount = GetRowPossibleColumn( dr, "IsRecommendedForCount", 0 );
                    item.IsAdvancedStandingForCount = GetRowPossibleColumn( dr, "IsAdvancedStandingForCount", 0 );
                    item.AdvancedStandingFromCount = GetRowPossibleColumn( dr, "AdvancedStandingFromCount", 0 );
                    item.PreparationForCount = GetRowPossibleColumn( dr, "IsPreparationForCount", 0 );
                    item.PreparationFromCount = GetRowPossibleColumn( dr, "IsPreparationFromCount", 0 );

                    
                    item.QualityAssurance = Fill_AgentRelationship( dr, "QualityAssurance", CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false, true );

                    org = GetRowPossibleColumn( dr, "Organization", string.Empty );
                    orgId = GetRowPossibleColumn( dr, "OrgId", 0 );
                    if ( orgId > 0 )
                        item.PrimaryOrganization = new Organization() { Id = orgId, Name = org };
					item.ListTitle = item.Name + " (" + item.OrganizationName + ")";
					//
					temp = GetRowColumn( dr, "DateEffective", string.Empty );
                    if ( IsValidDate( temp ) )
                        item.DateEffective = DateTime.Parse( temp ).ToString("yyyy-MM-dd");
                    else
                        item.DateEffective = string.Empty;

                    item.Created = GetRowColumn( dr, "Created", System.DateTime.MinValue );
                    item.LastUpdated = GetRowColumn( dr, "LastUpdated", System.DateTime.MinValue );

                    //addressess
                    int addressess = GetRowPossibleColumn( dr, "AvailableAddresses", 0 );
                    if ( addressess > 0 )
                    {
                        item.AvailableAt = Entity_AddressManager.GetAll( item.RowId );
                    }
                    //not used yet
                    item.CompetenciesCount = GetRowPossibleColumn( dr, "Competencies", 0 );
					if ( item.CompetenciesCount > 0 )
					{
						//handled in search services
						//List<string> competencyList = new List<string>();
						//FillCompetencies( item, ref competencyList );
					}
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
            if ( !string.IsNullOrWhiteSpace( input.CredentialRegistryId ) )
                output.CredentialRegistryId = input.CredentialRegistryId;

            output.Id = input.Id;
            output.Name = GetData( input.Name );
            output.Description = GetData( input.Description );
			output.LifeCycleStatusTypeId = input.LifeCycleStatusTypeId;
			output.IdentificationCode = GetData( input.CodedNotation );
            //output.VersionIdentifier = GetData( input.VersionIdentifier );

            //output.OtherAssessmentType = GetData( input.OtherAssessmentType );

            output.SubjectWebpage = GetUrlData( input.SubjectWebpage );
			output.InCatalog = GetUrlData( input.InCatalog );
			output.AvailableOnlineAt = GetUrlData( input.AvailableOnlineAt );
            output.AvailabilityListing = GetUrlData( input.AvailabilityListing );
            output.AssessmentExampleUrl = GetData( input.AssessmentExample );

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
                //input.OwningOrganizationId = org.Id;
            }
            else
            {
                //always have output have an owner
                //output.OwningAgentUid = null;
            }

			//if ( input.InLanguageId > 0 )
			//	output.InLanguageId = input.InLanguageId;
			//else if ( !string.IsNullOrWhiteSpace( input.InLanguage ) )
			//{
			//	output.InLanguageId = CodesManager.GetLanguageId( input.InLanguage );
			//}
			//else
				output.InLanguageId = null;

			//======================================================================
			//can have just CreditUnitTypeDescription. Will need a policy if both are found?
			//	-possibly create a second CreditValue?			
			if ( !string.IsNullOrWhiteSpace( input.CreditUnitTypeDescription ) )
				output.CreditUnitTypeDescription = input.CreditUnitTypeDescription;
			else
				output.CreditUnitTypeDescription = null;

			//=========================================================
			//21-03-23 - now using ValueProfile

			//***actually may have to fill out credit units etc?
			output.CreditValue = string.IsNullOrWhiteSpace(input.CreditValueJson) ? null : input.CreditValueJson;
			//

			//========================================================================
			output.DeliveryTypeDescription = input.DeliveryTypeDescription;
            //output.VerificationMethodDescription = input.VerificationMethodDescription;
            output.AssessmentExampleDescription = input.AssessmentExampleDescription;
            output.AssessmentOutput = input.AssessmentOutput;
            output.ExternalResearch = input.ExternalResearch;
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
            //only true should be published. Ensure the save only saves True
            if ( input.IsNonCredit != null && input.IsNonCredit == true )
				output.IsNonCredit = input.IsNonCredit;
			else
				output.IsNonCredit = null;
			//
			output.HasGroupEvaluation = input.HasGroupEvaluation;
            output.HasGroupParticipation = input.HasGroupParticipation;
            output.IsProctored = input.IsProctored;

            output.ProcessStandards = input.ProcessStandards;
            output.ProcessStandardsDescription = input.ProcessStandardsDescription;

            output.ScoringMethodDescription = input.ScoringMethodDescription;
            output.ScoringMethodExample = input.ScoringMethodExample;
            output.ScoringMethodExampleDescription = input.ScoringMethodExampleDescription;



            if ( IsValidDate( input.DateEffective ) )
                output.DateEffective = DateTime.Parse( input.DateEffective );
            else
                output.DateEffective = null;
			if ( IsValidDate( input.ExpirationDate ) )
				output.ExpirationDate = DateTime.Parse( input.ExpirationDate );
			else
				output.ExpirationDate = null;


			if ( IsValidDate( input.LastUpdated ) )
                output.LastUpdated = input.LastUpdated;
		}

		public static void MapFromDB( DBResource input, ThisResource output,
				bool includingProperties,
				bool includingRoles,
				bool includeWhereUsed,
				bool includingProcessProfiles )
		{
			AssessmentRequest request = new AssessmentRequest()
			{
				IncludingProperties = includingProperties,
				IncludingRolesAndActions = includingRoles,
				IncludingProcessProfiles = includingProcessProfiles,
				IncludeWhereUsed = includeWhereUsed
			};

			MapFromDB( input, output, request );
		}

		public static void MapFromDB( DBResource input, ThisResource output, AssessmentRequest request )
		{
			var isForDetail = (request.IsForAPIRequest || request.IsForDetailView);
			//get basic 
			MapFromDB_Basic( input, output, isForDetail, false, request.IncludingProperties );
			output.CodedNotation = input.IdentificationCode;

			if ( IsValidDate( input.DateEffective ) )
				output.DateEffective = ( ( DateTime ) input.DateEffective ).ToString( "yyyy-MM-dd" );
			else
				output.DateEffective = string.Empty;
			//
			if ( IsValidDate( input.ExpirationDate ) )
				output.ExpirationDate = ( ( DateTime ) input.ExpirationDate ).ToString( "yyyy-MM-dd" );
			else
				output.ExpirationDate = string.Empty;

			output.CreditUnitTypeDescription = input.CreditUnitTypeDescription;
			//=========================================================
			//21-03-23 - now using ValueProfile
			if ( !string.IsNullOrWhiteSpace( input.CreditValue ) && input.CreditValue != "[]" )
			{
				output.CreditValue = JsonConvert.DeserializeObject<List<ValueProfile>>( input.CreditValue );
			}

			//=====
			//23-04-20 HMMM, why was this removed? Performance issue? See CredentialManager.
			var relatedEntity = EntityManager.GetEntity( output.RowId, false );
			//if ( relatedEntity != null && relatedEntity.Id > 0 )
			output.EntityLastUpdated = output.LastUpdated;  // relatedEntity.LastUpdated;
			output.HasSupportService = Entity_HasSupportServiceManager.GetAllSummary( relatedEntity );
			var getAll = Entity_HasResourceManager.GetAll( relatedEntity );
			//var getAll = Entity_HasResourceManager.GetAllEntityType( relatedEntity, CodesManager.ENTITY_TYPE_OCCUPATIONS_PROFILE, HasSpecializationRelationshipId );
			if ( getAll != null && getAll.Count > 0 )
			{
				output.HasRubric = getAll.Where( r => r.EntityTypeId == CodesManager.ENTITY_TYPE_RUBRIC ).ToList();
				output.ProvidesTransferValueFor = getAll.Where( r => r.RelationshipTypeId == Entity_HasResourceManager.HAS_RESOURCE_TYPE_ProvidesTransferValueFor && ( r.EntityTypeId == CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE ) ).ToList();
				output.ReceivesTransferValueFrom = getAll.Where( r => r.RelationshipTypeId == Entity_HasResourceManager.HAS_RESOURCE_TYPE_ReceivesTransferValueFrom && ( r.EntityTypeId == CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE ) ).ToList();
			}

			//
			output.WhereReferenced = new List<string>();
			if ( input.Entity_Assessment != null && input.Entity_Assessment.Count > 0 )
			{
				foreach ( var item in input.Entity_Assessment )
				{
					if ( item.Entity == null )
					{
						//shouldn't happen? - log
						continue;
					}
					if ( item.Entity.EntityTypeId == CodesManager.ENTITY_TYPE_CONNECTION_PROFILE )
					{
						ConditionProfile cp = CondProfileMgr.GetAs_IsPartOf( item.Entity.EntityUid, output.Name, output.AssessmentConnections );
						//first check if the parent (ex credential) is already in a condition profile - with inverse relationship type
						if ( request.IsForAPIRequest )
							output.IsPartOfConditionProfile.Add( cp );
						//now done in GetAs_IsPartOf
						//output.AssessmentConnections.Add( cp );

					}
					else if ( item.Entity.EntityTypeId == CodesManager.ENTITY_TYPE_DATASET_PROFILE )
					{
						//so should these be kept separate?
						var dsp = DataSetProfileManager.Get( ( int ) item.Entity.EntityBaseId, true, request.IsForAPIRequest );
						if ( dsp != null && dsp.Id > 0 && dsp.EntityStateId == 3 )
						{
							//need to exclude if already part of the aggregateProfile data. 
							var exists = output.AggregateData.Where( s =>
										s.RelevantDataSet.Exists( z =>
											z.CTID == dsp.CTID ) ).ToList();
							//actually ProPath has lots of dups
							//23-02-10 mp - uncommented the following if to exclude dsps that are in an adp. 
							if ( exists == null || exists.Count == 0 )
								output.ExternalDataSetProfiles.Add( dsp );
						}

					}
					else if ( item.Entity.EntityTypeId == CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE )
					{
						//so should these be kept separate?
						//this will be replaced by provides and receives TVP
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
			//consider what to skip for references (set in MapFromDB_Basic)
			if ( output.IsReferenceVersion )
			{
				return;
			}
			output.EstimatedDuration = DurationProfileManager.GetAll( output.RowId );
			output.AvailableAt = Entity_AddressManager.GetAll( output.RowId );


			output.AvailabilityListing = input.AvailabilityListing;
			output.AssessmentExample = input.AssessmentExampleUrl;
			output.AvailableOnlineAt = input.AvailableOnlineAt;
			output.ExternalResearch = input.ExternalResearch;

			output.AlternateName = Entity_ReferenceManager.GetAllToList( output.RowId, CodesManager.PROPERTY_CATEGORY_ALTERNATE_NAME );

			output.CredentialRegistryId = input.CredentialRegistryId;

			output.AssessmentExample = input.AssessmentExampleUrl;
			output.AssessmentExampleDescription = input.AssessmentExampleDescription;


			output.AvailableOnlineAt = input.AvailableOnlineAt;
			output.InCatalog = GetUrlData( input.InCatalog );

			//multiple languages, now in entity.reference
			output.InLanguageCodeList = Entity_ReferenceManager.GetAll( output.RowId, CodesManager.PROPERTY_CATEGORY_LANGUAGE );

			//=============================

			output.DeliveryTypeDescription = input.DeliveryTypeDescription;
			//output.VerificationMethodDescription = from.VerificationMethodDescription;
			output.AssessmentOutput = input.AssessmentOutput;
			output.ExternalResearch = input.ExternalResearch;
			if ( input.HasGroupEvaluation != null )
				output.HasGroupEvaluation = ( bool )input.HasGroupEvaluation;
			if ( input.HasGroupParticipation != null )
				output.HasGroupParticipation = ( bool )input.HasGroupParticipation;
			if ( input.IsProctored != null )
				output.IsProctored = ( bool )input.IsProctored;

			output.ProcessStandards = input.ProcessStandards;
			output.ProcessStandardsDescription = input.ProcessStandardsDescription;

			if ( !string.IsNullOrWhiteSpace( input.TargetLearningResource ) && input.TargetLearningResource != "null" && input.TargetLearningResource.IndexOf( "\"null" ) == -1 )
			{
				output.TargetLearningResource = JsonConvert.DeserializeObject<List<string>>( input.TargetLearningResource );
			}
			
            output.ScoringMethodDescription = input.ScoringMethodDescription;
            output.ScoringMethodExample = input.ScoringMethodExample;
            output.ScoringMethodExampleDescription = input.ScoringMethodExampleDescription;
            output.AudienceType = EntityPropertyManager.FillEnumeration( output.RowId,CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE );
			output.AudienceLevelType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL );

			output.Subject = Entity_ReferenceManager.GetAll( output.RowId, CodesManager.PROPERTY_CATEGORY_SUBJECT );
            //
            output.SupersededBy = input.SupersededBy;
            output.Supersedes = input.Supersedes;
            output.Keyword = Entity_ReferenceManager.GetAll( output.RowId, CodesManager.PROPERTY_CATEGORY_KEYWORD );
			output.Identifier = Entity_IdentifierValueManager.GetAll( output.RowId, Entity_IdentifierValueManager.IdentifierValue_Identifier );
			output.SameAs = Entity_ReferenceManager.GetAll( input.RowId, CodesManager.PROPERTY_CATEGORY_SAME_AS ); //  = 76;
			//properties
			try
			{
                if ( request.IncludingProperties )
                {
                    output.AssessmentMethodType = EntityPropertyManager.FillEnumeration(output.RowId, CodesManager.PROPERTY_CATEGORY_Assessment_Method_Type);

                    output.AssessmentUseType = EntityPropertyManager.FillEnumeration(output.RowId, CodesManager.PROPERTY_CATEGORY_ASSESSMENT_USE_TYPE);

                    output.DeliveryType = EntityPropertyManager.FillEnumeration(output.RowId, CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE);

                    output.ScoringMethodType = EntityPropertyManager.FillEnumeration(output.RowId, CodesManager.PROPERTY_CATEGORY_Scoring_Method);

					output.ScheduleTimingType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_SCHEDULE_TIMING );

					output.ScheduleFrequencyType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_SCHEDULE_FREQUENCY );

					output.OfferFrequencyType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_OFFER_FREQUENCY );

					//this is in MapFromDB_Basic
					//output.EstimatedCost = CostProfileManager.GetAll( output.RowId, forEditView );

				}

			}
            catch ( Exception ex )
            {
                LoggingHelper.LogError(ex, thisClassName + string.Format(".MapFromDB_A(), Name: {0} ({1})", output.Name, output.Id));
                output.StatusMessage = FormatExceptions(ex);
            }



			try
			{
				//new
				// get all and then split
				var rfi = ReferenceFrameworkItemsManager.FillCredentialAlignmentObject( output.RowId );
				if ( rfi != null && rfi.Any() )
				{
					output.OccupationTypes = rfi.Where( r => r.CategoryId == CodesManager.PROPERTY_CATEGORY_SOC ).ToList();
					output.IndustryTypes = rfi.Where( r => r.CategoryId == CodesManager.PROPERTY_CATEGORY_NAICS ).ToList();
					output.InstructionalProgramTypes = rfi.Where( r => r.CategoryId == CodesManager.PROPERTY_CATEGORY_CIP ).ToList();
				}
				//output.OccupationTypes = ReferenceFrameworkItemsManager.FillCredentialAlignmentObject( output.RowId, CodesManager.PROPERTY_CATEGORY_SOC );
    //            output.IndustryTypes = ReferenceFrameworkItemsManager.FillCredentialAlignmentObject( output.RowId, CodesManager.PROPERTY_CATEGORY_NAICS );
    //            output.InstructionalProgramTypes = ReferenceFrameworkItemsManager.FillCredentialAlignmentObject( output.RowId, CodesManager.PROPERTY_CATEGORY_CIP );

                //OLD
                //output.Occupation = ReferenceFrameworkItemsManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_SOC );		
                //output.Industry = ReferenceFrameworkItemsManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_NAICS );
                //output.InstructionalProgramType = ReferenceFrameworkItemsManager.FillEnumeration(output.RowId, CodesManager.PROPERTY_CATEGORY_CIP);

                if ( request.IncludingRolesAndActions )
                {

                    //get as ennumerations
                    //output.OrganizationRole = Entity_AgentRelationshipManager.AgentEntityRole_GetAll_ToEnumeration(output.RowId, true);
					//moved to basic
                    //output.OrganizationRole = Entity_AssertionManager.GetAllCombinedForTarget( 3, output.Id, output.OwningOrganizationId );
                    //}
                    //output.QualityAssuranceAction =	Entity_QualityAssuranceActionManager.QualityAssuranceActionProfile_GetAll( output.RowId );


                    output.Jurisdiction = Entity_JurisdictionProfileManager.Jurisdiction_GetAll(output.RowId);
                    output.Region = Entity_JurisdictionProfileManager.Jurisdiction_GetAll(output.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_RESIDENT);
                    output.JurisdictionAssertions = Entity_JurisdictionProfileManager.Jurisdiction_GetAll(output.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_OFFERREDIN);

                }

                //get condition profiles
                List<ConditionProfile> list = new List<ConditionProfile>();
                list = Entity_ConditionProfileManager.GetAll(output.RowId, false);
                if ( list != null && list.Count > 0 )
                {
                    foreach ( ConditionProfile item in list )
                    {
                        if ( item.ConditionSubTypeId == Entity_ConditionProfileManager.ConditionSubType_Assessment )
                        {
                            output.AssessmentConnections.Add(item);
                        }
                        else if ( item.ConnectionProfileTypeId == Entity_ConditionProfileManager.ConnectionProfileType_Requirement )
                            output.Requires.Add(item);
                        else if ( item.ConnectionProfileTypeId == Entity_ConditionProfileManager.ConnectionProfileType_Recommendation )
                            output.Recommends.Add(item);
                        else if ( item.ConnectionProfileTypeId == Entity_ConditionProfileManager.ConnectionProfileType_Corequisite )
                            output.Corequisite.Add(item);
						else if ( item.ConnectionProfileTypeId == Entity_ConditionProfileManager.ConnectionProfileType_CoPrerequisite )
							output.CoPrerequisite.Add( item );
						else if ( item.ConnectionProfileTypeId == Entity_ConditionProfileManager.ConnectionProfileType_EntryCondition )
                            output.EntryCondition.Add(item);
                        else
                        {
                            EmailManager.NotifyAdmin("Unexpected Condition Profile for assessment", string.Format("AssessmentId: {0}, ConditionProfileTypeId: {1}", output.Id, item.ConnectionProfileTypeId));

                            //add output required, for dev only?
                            if ( IsDevEnv() )
                            {
                                item.ProfileName = ( item.ProfileName ?? string.Empty ) + " unexpected condition type of " + item.ConnectionProfileTypeId.ToString();
                                output.Requires.Add(item);
                            }
                        }
                    }
				}
                //
                output.HasScheduledOffering = Entity_HasOfferingManager.GetAll( output.RowId );

				//
				output.CollectionMembers = CollectionMemberManager.GetMemberOfCollections( output.CTID );
				//
				output.AdvancedStandingFrom = ConditionManifestExpanded.DisambiguateConditionProfiles( output.AssessmentConnections ).AdvancedStandingFrom;
				output.IsAdvancedStandingFor = ConditionManifestExpanded.DisambiguateConditionProfiles( output.AssessmentConnections ).IsAdvancedStandingFor;
				output.IsRequiredFor = ConditionManifestExpanded.DisambiguateConditionProfiles( output.AssessmentConnections ).IsRequiredFor;
				output.IsRecommendedFor = ConditionManifestExpanded.DisambiguateConditionProfiles( output.AssessmentConnections ).IsRecommendedFor;
				output.IsPreparationFor = ConditionManifestExpanded.DisambiguateConditionProfiles( output.AssessmentConnections ).IsPreparationFor;
				output.PreparationFrom = ConditionManifestExpanded.DisambiguateConditionProfiles( output.AssessmentConnections ).PreparationFrom;
				output.RelatedAction = CredentialingActionManager.GetRelatedActionFromObject( output.RowId );


			}
            catch ( Exception ex )
            {
                LoggingHelper.LogError(ex, thisClassName + string.Format(".MapFromDB_B(), Name: {0} ({1})", output.Name, output.Id));
                output.StatusMessage = FormatExceptions(ex);
            }

			try
			{
				//**TODO VersionIdentifier - need to change to a list of IdentifierValue
				//to.VersionIdentifier = from.VersionIdentifier;
				//assumes only one identifier type per class
				output.VersionIdentifierList = Entity_IdentifierValueManager.GetAll( output.RowId, Entity_IdentifierValueManager.IdentifierValue_VersionIdentifier );
				output.Identifier = Entity_IdentifierValueManager.GetAll( output.RowId, Entity_IdentifierValueManager.IdentifierValue_Identifier );

				MapCosts( output );

				output.FinancialAssistance = Entity_FinancialAssistanceProfileManager.GetAll( output.RowId, false );
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + string.Format( ".MapFromDB(), Name: {0} ({1})", output.Name, output.Id ) );
				output.StatusMessage = FormatExceptions( ex );
			}


			output.CommonConditions = Entity_CommonConditionManager.GetAll( output.RowId );
			//done in MapFromDB_Basic
			//output.CommonCosts = Entity_CommonCostManager.GetAll( output.RowId );

			//TODO
			if ( request.IncludingProcessProfiles)
			{
				List<ProcessProfile> processes = Entity_ProcessProfileManager.GetAll( output.RowId );
				foreach ( ProcessProfile item in processes )
				{
					if ( item.ProcessTypeId == Entity_ProcessProfileManager.ADMIN_PROCESS_TYPE )
						output.AdministrationProcess.Add( item );
					else if ( item.ProcessTypeId == Entity_ProcessProfileManager.DEV_PROCESS_TYPE )
						output.DevelopmentProcess.Add( item );
					else if ( item.ProcessTypeId == Entity_ProcessProfileManager.MTCE_PROCESS_TYPE )
						output.MaintenanceProcess.Add( item );
					else
					{
						//unexpected
					}
				}
			}
			else
				output.ProcessProfilesSummary = Entity_ProcessProfileManager.GetAllSummary( output.RowId );
		}

		private static void MapCosts( ThisResource output )
		{
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
		} //
		public static void MapFromDB_Basic( DBResource input, ThisResource output, bool isForDetail, bool includingCosts, bool includingCompetencies )
		{
			output.Id = input.Id;
			output.RowId = input.RowId;
			output.CTID = input.CTID;
			output.CTDLTypeLabel = "Assessment Profile";
			output.EntityStateId = ( int ) ( input.EntityStateId ?? 1 );
			var orgRoleManager = new OrganizationRoleManager();
			//
			output.Name = input.Name;
			output.FriendlyName = FormatFriendlyTitle( input.Name );
			output.NamePlusOrganization = output.Name;

			if ( IsGuidValid( input.OwningAgentUid ) )
			{
				output.PrimaryAgentUID = ( Guid )input.OwningAgentUid;
				output.PrimaryOrganization = OrganizationManager.GetForSummary( output.PrimaryAgentUID );
				output.NamePlusOrganization = output.Name + $" ( {output.PrimaryOrganization.Name} )";
				//get all owner roles for this assessment
				OrganizationRoleProfile orp = Entity_AgentRelationshipManager.AgentEntityRole_GetAsEnumerationFromCSV( output.RowId, output.PrimaryAgentUID );
				output.OwnerRoles = orp.AgentRole;
				//
				if ( isForDetail && !string.IsNullOrWhiteSpace( output.CTID ) && output.EntityStateId == 3 && output.OwningOrganizationId > 0 )
				{
					//new - get owner QA now. only if particular context
					//actually don't want this as is slow. Break up into parts
					//var ownersQAReceived = Entity_AssertionManager.GetAllCombinedForTarget( 2, to.OwningOrganization.Id, to.OwningOrganization.Id );
					//output.OwningOrganizationQAReceived = Entity_AgentRelationshipManager.GetAllThirdPartyAssertionsForEntity( 2, output.OwningOrganization.RowId, output.OwningOrganization.Id, true );
					//var orgFirstPartyAssertions = Entity_AssertionManager.GetAllFirstPartyAssertionsForTarget( 2, output.OwningOrganization.RowId, output.OwningOrganization.Id, true );
					//if ( orgFirstPartyAssertions != null && orgFirstPartyAssertions.Any() )
					//{
					//	//foreach( var item in orgFirstPartyAssertions )
					//	//{
					//	//	var exists = output.OwningOrganizationQAReceived.Where( m => m.ActingAgentId == item.ActingAgentId ).ToList();
					//	//}
					//	output.OwningOrganizationQAReceived.AddRange( orgFirstPartyAssertions );
					//}
					output.OwningOrganizationQAReceived = orgRoleManager.GetAllCombinedForTarget( 2, output.OwningOrganizationId, output.OwningOrganizationId, true );
				}
			}

			if ( output.OwningOrganizationId > 0 )
			{
				//OLD
				//output.OrganizationRole = Entity_AssertionManager.GetAllCombinedForTarget( 3, output.Id, output.OwningOrganizationId );
				//NEW
				output.OrganizationRole = orgRoleManager.GetAllCombinedForTarget( 3, output.Id, output.OwningOrganizationId );
				//output.OrganizationRole = Entity_AgentRelationshipManager.GetAllThirdPartyAssertionsForEntity( 3, output.RowId, output.OwningOrganizationId );
				//var firstPartyAssertions = Entity_AssertionManager.GetAllFirstPartyAssertionsForTarget( 3, output.RowId, output.OwningOrganizationId, false );
				//if ( firstPartyAssertions != null && firstPartyAssertions.Any() )
				//	output.OrganizationRole.AddRange( firstPartyAssertions );
			}
			//
			if ( output.EntityStateId == 2 )
			{
				if ( !output.OrganizationRole.Any() && output.OwningOrganizationId > 0 )
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

			output.Description = input.Description == null ? string.Empty : input.Description;

            output.SubjectWebpage = input.SubjectWebpage;
            if (IsValidDate( input.Created ))
                output.Created = ( DateTime ) input.Created;
            if (IsValidDate( input.LastUpdated ))
                output.LastUpdated = ( DateTime ) input.LastUpdated;
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
			//
			//------------------------------------------------------------------------
			if ( string.IsNullOrWhiteSpace( output.CTID ) || output.EntityStateId < 3 )
			{
				output.IsReferenceVersion = true;
				return;
			}
			//
			output.AssessmentMethodDescription = input.AssessmentMethodDescription;
			output.LearningMethodDescription = input.LearningMethodDescription;
			//only true should be published. Ensure the save only saves True
			if ( input.IsNonCredit != null && input.IsNonCredit == true )
				output.IsNonCredit = input.IsNonCredit;
			else
				output.IsNonCredit = null;



			//get competencies - why would competencies be included with basic?
			if ( includingCompetencies )
				MapFromDB_Competencies( output );


			//doesn't make sense to include costs in a basic view?
			if ( includingCosts )
			{
				MapCosts( output );
			}


		} //

		public static void MapFromDB_Competencies( ThisResource output )
        {

            var frameworksList = new Dictionary<string, RegistryImport>();
            //AssessesCompetencies is only used by import
            //to.AssessesCompetencies = Entity_CompetencyManager.GetAllAs_CredentialAlignmentObjectProfile( to.RowId, ref frameworksList);
            //to.FrameworkPayloads = frameworksList;
            //if ( to.AssessesCompetencies.Count > 0 )
            //    to.HasCompetencies = true;

            output.AssessesCompetenciesFrameworks = new List<CredentialAlignmentObjectFrameworkProfile>();
			output.AssessesCompetenciesFrameworks = Entity_CompetencyManager.GetAllAs_CAOFramework(  output.RowId, "Assesses", ref frameworksList);
            //these would be retrieved via condition profiles!
			//23-02-03 mp - it was not being populated elsewhere so re-enabled it here
            output.RequiresCompetenciesFrameworks = Entity_CompetencyManager.GetAllAs_CAOFramework( output.RowId, "Requires", ref frameworksList );
            if ( output.RequiresCompetenciesFrameworks.Count > 0 )
            {
                output.HasCompetencies = true;
                output.FrameworkPayloads = frameworksList;
            }
            //
            if ( output.AssessesCompetenciesFrameworks.Count > 0 )
            {
                output.HasCompetencies = true;
                output.FrameworkPayloads = frameworksList;

				foreach(var item in output.AssessesCompetenciesFrameworks)
				{
					//??????
				}
            }
		}

		/// <summary>
		/// Fill actual competencies for entity
		/// </summary>
		/// <param name="item"></param>
		/// <param name="competencyList">Contains any competencies from filters</param>
		//private static void FillCompetencies(ThisResource item, ref List<string> competencyList)
		//{
		//    var frameworksList = new Dictionary<string, RegistryImport>();
		//    item.AssessesCompetenciesFrameworks = new List<CredentialAlignmentObjectFrameworkProfile>();
		//    //return;
		//    //TODO - not using frameworks, the latter would have flattened items to CredentialAlignmentObjectProfile, which we do have
		//    if ( competencyList.Count == 0 )
		//        item.AssessesCompetenciesFrameworks = Entity_CompetencyManager.GetAllAs_CAOFramework(item.RowId, ref frameworksList);
		//    else
		//    {

		//        List<CredentialAlignmentObjectFrameworkProfile> all = Entity_CompetencyManager.GetAllAs_CAOFramework(item.RowId, ref frameworksList);
		//        foreach ( CredentialAlignmentObjectFrameworkProfile next in all )
		//        {
		//            //just do desc for now
		//            string orig = ( next.Description ?? string.Empty );
		//            foreach ( string filter in competencyList )
		//            {
		//                //not ideal, as would be an exact match
		//                orig = orig.Replace(filter, string.Format("<span class='highlight'>{0}<\\span>", filter));
		//            }
		//            if ( orig != ( next.Description ?? string.Empty ) )
		//            {
		//                next.Description = orig;
		//                item.AssessesCompetenciesFrameworks.Add(next);
		//            }
		//        }
		//    }
		//}
		#endregion
	}
	public class AssessmentRequest
	{
		public AssessmentRequest( int requestTypeId = 1 )
		{
			switch ( requestTypeId )
			{
				case 1:
					IsDetailRequest();
					break;
				case 2:
					IsAPIRequest();
					break;
				case 3:
					IsSummaryRequest();//or minimum
					break;
				default:
					DoCompleteFill();
					break;
			}

		}
		public void DoCompleteFill()
		{
			IncludingProperties = true;
		}
		public void IsDetailRequest()
		{
			IsForDetailView = true;
			//AllowCaching = true;

			IncludingAddresses = true;
			IncludingCosts = true;
			IncludingCompetencies = true;
			IncludingProcessProfiles = true;
			IncludingProperties = true;

			IncludingRolesAndActions = true;
		}
		public void IsAPIRequest()
		{
			//TBD: if API will always want to count the excluded profiles. May want an option with no counts to assess performance
			IsForAPIRequest = true;
			//AllowCaching = true;

			IncludingAddresses = true;
			IncludingCosts = true;
			IncludingCompetencies = true;
			IncludingProperties = true;
			IncludingRolesAndActions = true;

		}
		public void IsSummaryRequest()
		{
			IsForAPIRequest = false;
			AllowCaching = false;
			IncludingAddresses = false;
			IncludingProperties = false;
			IncludingRolesAndActions = false;

		}
		public void IsCompareRequest()
		{
			IncludingProperties = true;
			//AllowCaching = true;

		}

		//not sure we need the 'view' properties
		public bool IsForDetailView { get; set; }
		public bool IsForAPIRequest { get; set; }
		//note this would be handled before we get to the manager
		public bool AllowCaching { get; set; }

		public bool IncludingAddresses { get; set; }
		public bool IncludingCompetencies { get; set; }
		public bool IncludingCosts { get; set; }
		public bool IncludeWhereUsed { get; set; }
		
		public bool IncludingProcessProfiles { get; set; }
		public bool IncludingProperties { get; set; }
		public bool IncludingRolesAndActions { get; set; }
	}


}

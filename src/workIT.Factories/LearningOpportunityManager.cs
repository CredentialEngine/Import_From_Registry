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
		string statusMessage = "";
		EntityManager entityMgr = new EntityManager();
		#region LearningOpportunity - persistance ==================
		/// <summary>
		/// Update a LearningOpportunity
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool Save( ThisEntity entity, ref SaveStatus status )
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
						DBEntity efEntity = context.LearningOpportunities
									.FirstOrDefault( s => s.Id == entity.Id );

						if ( efEntity != null && efEntity.Id > 0 )
						{
							//delete the entity and re-add
							Entity e = new Entity()
							{
								EntityBaseId = efEntity.Id,
								EntityTypeId = CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE,
								EntityType = "LearningOpportunity",
								EntityUid = efEntity.RowId,
								EntityBaseName = efEntity.Name
							};
							if ( entityMgr.ResetEntity( e, ref statusMessage ) )
							{

							}

							//fill in fields that may not be in entity
							entity.RowId = efEntity.RowId;

							MapToDB( entity, efEntity );
							//assume and validate, that if we get here we have a full record
							if ( (efEntity.EntityStateId ?? 1) == 1 )
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
									
									string message = thisClassName + string.Format( ". Update Failed", "Attempted to update a LearningOpportunity. The process appeared to not work, but was not an exception, so we have no message, or no clue. LearningOpportunity: {0}, Id: {1}}", entity.Name, entity.Id );
									status.AddError( "Error - the update was not successful. "  + message );
									EmailManager.NotifyAdmin( thisClassName + ". Update Failed", message );
								}
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
							statusMessage = "Error - update failed, as record was not found.";
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
				string message = HandleDBValidationError( dbex, thisClassName + ".Save() ", "LearningOpportunity" );
				status.AddError( thisClassName + ".Save(). Error - the save was not successful. " + message );
			}
			catch ( Exception ex )
			{
				string message = FormatExceptions( ex );
				LoggingHelper.LogError( ex, thisClassName + string.Format( ".Save. id: {0}", entity.Id ) );
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
					efEntity.Created = System.DateTime.Now;
					efEntity.LastUpdated = System.DateTime.Now;

					context.LearningOpportunities.Add( efEntity );

					// submit the change to database
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						statusMessage = "successful";
						entity.Id = efEntity.Id;
						entity.RowId = efEntity.RowId;
				
						if ( UpdateParts( entity, ref status ) == false )
						{
							
						}

						return entity.Id;
					}
					else
					{
						//?no info on error
						statusMessage = "Error - the add was not successful. ";
						string message = thisClassName + string.Format( ". Add Failed", "Attempted to add a Learning Opportunity. The process appeared to not work, but was not an exception, so we have no message, or no clue. LearningOpportunity: {0}, CTID: {1}", entity.Name, entity.CTID );
						status.AddError( thisClassName + ". Error - the add was not successful. " + message );
						EmailManager.NotifyAdmin( thisClassName + ".Add Failed", message );
					}
				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					
					string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", entity.ProfileName );
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
						status.AddError( thisClassName + ". AddBaseReference() The learning opportunity is incomplete" );
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

					context.LearningOpportunities.Add( efEntity );
					int count = context.SaveChanges();
					if ( count > 0 )
					{
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

					context.LearningOpportunities.Add( efEntity );
					int count = context.SaveChanges();
					if ( count > 0 )
						return efEntity.Id;

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
				status.AddWarning( "A Learning Opportunity Description must be entered" );
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

			if ( profile.CreditHourValue < 0 || profile.CreditHourValue > 10000 )
				status.AddWarning( "Error: invalid value for Credit Hour Value. Must be a reasonable decimal value greater than zero." );

			if ( profile.CreditUnitValue < 0 || profile.CreditUnitValue > 1000 )
				status.AddWarning( "Error: invalid value for Credit Unit Value. Must be a reasonable decimal value greater than zero." );


			//can only have credit hours properties, or credit unit properties, not both
			bool hasCreditHourData = false;
			bool hasCreditUnitData = false;
			if ( profile.CreditHourValue > 0 || ( profile.CreditHourType ?? "" ).Length > 0 )
				hasCreditHourData = true;
			if ( profile.CreditUnitTypeId > 0
				|| ( profile.CreditUnitTypeDescription ?? "" ).Length > 0
				|| profile.CreditUnitValue > 0 )
				hasCreditUnitData = true;

			if ( hasCreditHourData && hasCreditUnitData )
				status.AddWarning( "Error: Data can be entered for Credit Hour related properties or Credit Unit related properties, but not for both." );

			return !status.HasSectionErrors;
		}


		/// <summary>
		/// Delete a Learning Opportunity, and related Entity
		/// </summary>
		/// <param name="Id"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool Delete( int Id, ref string statusMessage )
		{
			bool isValid = false;
			if ( Id == 0 )
			{
				statusMessage = "Error - missing an identifier for the LearningOpportunity";
				return false;
			}
			using ( var context = new EntityContext() )
			{
				try
				{
					context.Configuration.LazyLoadingEnabled = false;
					DBEntity efEntity = context.LearningOpportunities
								.FirstOrDefault( s => s.Id == Id );

					if ( efEntity != null && efEntity.Id > 0 )
					{
						Guid rowId = efEntity.RowId;

						context.LearningOpportunities.Remove( efEntity );
						int count = context.SaveChanges();
						if ( count > 0 )
						{
							isValid = true;
                            //add pending request 
                            List<String> messages = new List<string>();
                            new SearchPendingReindexManager().AddDeleteRequest( CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, efEntity.Id, ref messages );
                        }
					}
					else
					{
						statusMessage = "Error - delete failed, as record was not found.";
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
					DBEntity efEntity = context.LearningOpportunities
                                .FirstOrDefault( s => s.CredentialRegistryId == envelopeId
                                || ( s.CTID == ctid )
                                );

                    if ( efEntity != null && efEntity.Id > 0 )
					{
						Guid rowId = efEntity.RowId;

                        //need to remove from Entity.
                        //-using before delete trigger - verify won't have RI issues
                        string msg = string.Format( " Learning Opportunity. Id: {0}, Name: {1}, Ctid: {2}, EnvelopeId: {3}", efEntity.Id, efEntity.Name, efEntity.CTID, envelopeId );

                        //18-04-05 contactUs - change to set inactive, and notify - seems to have been some incorrect deletes

                        //context.LearningOpportunities.Remove( efEntity );
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
                                ActivityType = "LearningOpportunity", Activity = "Management", Event = "Delete", Comment = msg
                            });
                            //add pending request 
                            List<String> messages = new List<string>();
                            new SearchPendingReindexManager().AddDeleteRequest( CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, efEntity.Id, ref messages );
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
			bool isAllValid = true;
			Entity relatedEntity = EntityManager.GetEntity( entity.RowId );
			if ( relatedEntity == null || relatedEntity.Id == 0 )
			{
				status.AddError( "Error - the related Entity was not found." );
				return false;
			}

			if ( UpdateProperties( entity,  ref status ) == false )
			{
				isAllValid = false;
			}
			Entity_FrameworkItemManager efim = new Entity_FrameworkItemManager();
			if ( efim.SaveList( relatedEntity.Id, CodesManager.PROPERTY_CATEGORY_CIP, entity.InstructionalProgramTypes, ref status ) == false )
				isAllValid = false;


			Entity_ReferenceManager erm = new 
				Entity_ReferenceManager();
			EntityPropertyManager mgr = new EntityPropertyManager();


			if ( entity.OwnerRoles == null || entity.OwnerRoles.Items.Count == 0 )
			{
				//status.AddWarning( "Invalid request, please select one or more roles for the owing agent." );
				//isAllValid = false;
			}
			else
			{

				if ( entity.OwnerRoles.GetFirstItemId() != Entity_AgentRelationshipManager.ROLE_TYPE_OWNER )
				{
					//status.AddWarning( "Invalid request. The role \"Owned By\" must be one of the roles selected." );
					//isAllValid = false;
				}
				else
				{
					OrganizationRoleProfile profile = new OrganizationRoleProfile();
					profile.ParentUid = entity.RowId;
					profile.ActingAgentUid = entity.OwningAgentUid;
					profile.AgentRole = entity.OwnerRoles;
					profile.CreatedById = entity.LastUpdatedById;
					profile.LastUpdatedById = entity.LastUpdatedById;

					if ( !new Entity_AgentRelationshipManager().Save( profile, Entity_AgentRelationshipManager.VALID_ROLES_OWNER, ref status ) )
						isAllValid = false;
				}
			}

			//if ( erm.Add( entity.OtherInstructionalProgramCategory, entity.RowId, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, ref status, CodesManager.PROPERTY_CATEGORY_CIP, false ) == false )
				//isAllValid = false;


			if ( erm.Add( entity.Subject, entity.RowId, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE,  ref status, CodesManager.PROPERTY_CATEGORY_SUBJECT, false ) == false )
				isAllValid = false;

			if ( erm.Add( entity.Keyword, entity.RowId, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE,  ref status, CodesManager.PROPERTY_CATEGORY_KEYWORD, false ) == false )
				isAllValid = false;

            //for language, really want to convert from en to English (en)
            //erm.AddLanguages( entity.InLanguageCodeList, entity.RowId, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, ref status, CodesManager.PROPERTY_CATEGORY_LANGUAGE );
			erm.AddLanguage( entity.InLanguage, relatedEntity.Id, ref status, CodesManager.PROPERTY_CATEGORY_LANGUAGE );

			AddProfiles( entity, ref status );

			UpdateAssertedBys( entity, ref status );

			UpdateAssertedIns( entity, ref status );


			return isAllValid;
		} 

		public bool UpdateProperties( ThisEntity entity, ref SaveStatus status )
		{
			bool isAllValid = true;

			EntityPropertyManager mgr = new EntityPropertyManager();
	

			if ( mgr.AddProperties( entity.LearningMethodType, entity.RowId, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, CodesManager.PROPERTY_CATEGORY_Learning_Method_Type,  false, ref status ) == false )
				isAllValid = false;
		

			if ( mgr.AddProperties( entity.DeliveryType, entity.RowId, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE, false, ref status ) == false )
				isAllValid = false;
			return isAllValid;
		}
		public void AddProfiles( ThisEntity entity, ref SaveStatus status )
		{
			//DurationProfile
			DurationProfileManager dpm = new Factories.DurationProfileManager();
			dpm.SaveList( entity.EstimatedDuration, entity.RowId, ref status );

			//VersionIdentifier
			new Entity_IdentifierValueManager().SaveList( entity.VersionIdentifierList, entity.RowId, Entity_IdentifierValueManager.LEARNING_OPP_VersionIdentifier, ref status );

			//CostProfile
			CostProfileManager cpm = new Factories.CostProfileManager();
			cpm.SaveList( entity.EstimatedCost, entity.RowId, ref status );

			try
			{
				//ConditionProfile =======================================
				Entity_ConditionProfileManager emanager = new Entity_ConditionProfileManager();
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
			Entity_FinancialAlignmentProfileManager fapm = new Factories.Entity_FinancialAlignmentProfileManager();
			fapm.SaveList( entity.FinancialAssistance, entity.RowId, ref status );

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
			jpm.SaveList( entity.Jurisdiction, entity.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_SCOPE, ref status );

			new Entity_CommonConditionManager().SaveList( entity.ConditionManifestIds, entity.RowId, ref status );

			new Entity_CommonCostManager().SaveList( entity.CostManifestIds, entity.RowId, ref status );
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
				DBEntity from = context.LearningOpportunities
						.FirstOrDefault( s => s.CTID.ToLower() == ctid.ToLower() );

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
		public static ThisEntity GetBySubjectWebpage( string swp )
		{
			ThisEntity entity = new ThisEntity();
			using ( var context = new EntityContext() )
			{
				context.Configuration.LazyLoadingEnabled = false;
				DBEntity from = context.LearningOpportunities
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
		public static ThisEntity GetForDetail( int id)
		{
			ThisEntity entity = new ThisEntity();
			bool includingProfiles = true;

			using ( var context = new EntityContext() )
			{
				//context.Configuration.LazyLoadingEnabled = false;
				DBEntity item = context.LearningOpportunities
						.SingleOrDefault( s => s.Id == id );

				if ( item != null && item.Id > 0 )
				{
                    //check for virtual deletes
                    if (item.EntityStateId == 0)
                        return entity;

                    MapFromDB( item, entity,
						true, //includingProperties
						includingProfiles,
						false, //forEditView
						true);
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
				DBEntity item = context.LearningOpportunities
						.FirstOrDefault( s => s.Id == id );

				if ( item != null && item.Id > 0 )
				{
                    MapFromDB_Basic( item, entity, false );
                }
			}

			return entity;
		}

        public static List<ThisEntity> GetAllForOwningOrg( Guid owningOrgUid )
        {
            List<ThisEntity> list = new List<ThisEntity>();
            ThisEntity entity = new ThisEntity();
            using (var context = new EntityContext())
            {
                List<DBEntity> results = context.LearningOpportunities
                             .Where( s => s.OwningAgentUid == owningOrgUid )
                             .OrderBy( s => s.Name )
                             .ToList();
                if (results != null && results.Count > 0)
                {
                    foreach (DBEntity item in results)
                    {
                        entity = new ThisEntity();
                        MapFromDB_Basic( item, entity, false );
                        
                        list.Add( entity );
                    }
                }
            }

            return list;
        }

        public static ThisEntity GetAs_IsPartOf( Guid rowId, bool forEditView )
		{
			ThisEntity entity = new ThisEntity();

			using ( var context = new EntityContext() )
			{
				//	REVIEW	- seems like will need to almost always bubble up costs
				//			- just confirm that this method is to simply list parent Lopps
				context.Configuration.LazyLoadingEnabled = false;

				DBEntity item = context.LearningOpportunities
						.FirstOrDefault( s => s.RowId == rowId );

				if ( item != null && item.Id > 0 )
				{
                    MapFromDB_Basic( item, entity, false );

     //               entity.Id = item.Id;
					//entity.RowId = item.RowId;
					//entity.Name = item.Name;
					//entity.Description = item.Description;
					//entity.SubjectWebpage = item.SubjectWebpage;
					//entity.CTID = item.CTID;
					//if ( IsGuidValid( item.OwningAgentUid ) )
					//{
					//	entity.OwningAgentUid = ( Guid ) item.OwningAgentUid;
					//}
					////costs? = shouldn't need
					//entity.EstimatedCost = CostProfileManager.GetAll( entity.RowId );
				}
			}

			return entity;
		}
        public static void MapFromDB_Basic( DBEntity item, ThisEntity entity,
                bool includingCosts)
        {
            entity.Id = item.Id;
            entity.RowId = item.RowId;
            entity.Name = item.Name;
            entity.Description = item.Description;
            entity.SubjectWebpage = item.SubjectWebpage;
            entity.CTID = item.CTID;
            if (string.IsNullOrWhiteSpace( item.CTID ))
                entity.IsReferenceVersion = true;
            if (IsGuidValid( item.OwningAgentUid ))
            {
                entity.OwningAgentUid = ( Guid ) item.OwningAgentUid;
            }
            //costs? = shouldn't need
            if (includingCosts)
                entity.EstimatedCost = CostProfileManager.GetAll( entity.RowId );

        }


        public static List<string> Autocomplete( string pFilter, int pageNumber, int pageSize, ref int pTotalRows )
		{
			bool autocomplete = true;
			List<string> results = new List<string>();
			List<string> competencyList = new List<string>();
			//get minimal entity
			List<ThisEntity> list = Search( pFilter, "", pageNumber, pageSize,  ref pTotalRows, ref competencyList, autocomplete );
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
			return Search( pFilter, pOrderBy, pageNumber, pageSize,  ref pTotalRows, ref competencyList );
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
					string rows = command.Parameters[ command.Parameters.Count - 1 ].Value.ToString();
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
                    item.PreparationFromCount = GetRowColumn( dr, "IsPreparationFromCount", 0 );
                    item.QualityAssurance = Fill_AgentRelationship( dr, "QualityAssurance", CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false, true );
                    item.ListTitle = item.Name + " (" + item.OwnerOrganizationName + ")";
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

		/// <summary>
		/// Fill actual competencies for entity
		/// </summary>
		/// <param name="item"></param>
		/// <param name="competencyList">Contains any competencies from filters</param>
		private static void FillCompetencies( ThisEntity item, ref List<string> competencyList )
		{
			item.TeachesCompetenciesFrameworks = new List<CredentialAlignmentObjectFrameworkProfile>();
			//return;
			//TODO - not using frameworks, the latter would have flattened items to CredentialAlignmentObjectProfile, which we do have
			if ( competencyList.Count == 0 )
				item.TeachesCompetenciesFrameworks = Entity_CompetencyManager.GetAllAs_CAOFramework( item.RowId );
			else
			{
				
				List<CredentialAlignmentObjectFrameworkProfile> all = Entity_CompetencyManager.GetAllAs_CAOFramework( item.RowId );
				foreach ( CredentialAlignmentObjectFrameworkProfile next in all )
				{
					//just do desc for now
					string orig = ( next.Description ?? "" );
					foreach ( string filter in competencyList )
					{
						//not ideal, as would be an exact match
						orig = orig.Replace( filter, string.Format( "<span class='highlight'>{0}<\\span>", filter ) );
					}
					if ( orig != ( next.Description ?? "" ) )
					{
						next.Description = orig;
						item.TeachesCompetenciesFrameworks.Add( next );
					}
				}
			}
		}

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
			output.Name = GetData(input.Name);
			
			output.Description = GetData(input.Description);

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
                input.OwningOrganization= OrganizationManager.GetForSummary( input.OwningAgentUid );

            }
			else
			{
				//always have output have an owner
				//output.OwningAgentUid = null;
			}

			output.SubjectWebpage = GetUrlData( input.SubjectWebpage, null );
			output.IdentificationCode = GetData(input.CodedNotation);
			output.AvailableOnlineAt = GetUrlData( input.AvailableOnlineAt, null );
			output.AvailabilityListing = GetUrlData( input.AvailabilityListing, null );
			output.DeliveryTypeDescription = input.DeliveryTypeDescription;
			output.VerificationMethodDescription = input.VerificationMethodDescription;

			if ( IsValidDate( input.DateEffective ) )
				output.DateEffective = DateTime.Parse( input.DateEffective );
			else
				output.DateEffective = null;

			if ( input.InLanguageId > 0 )
				output.InLanguageId = input.InLanguageId;
			else if ( !string.IsNullOrWhiteSpace( input.InLanguage ) )
			{
				output.InLanguageId = CodesManager.GetLanguageId( input.InLanguage );
			}
			else
				output.InLanguageId = null;

			output.CreditHourType = GetData( input.CreditHourType );
			output.CreditHourValue = SetData( input.CreditHourValue, 0.5M );
			//output.CreditUnitTypeId = SetData( input.CreditUnitTypeId, 1 );
			if (input.CreditUnitType != null && input.CreditUnitType.HasItems())
			{
				//get Id if available
				EnumeratedItem item = input.CreditUnitType.GetFirstItem();
				if ( item != null && item.Id > 0 )
					output.CreditUnitTypeId = item.Id;
				else
				{ 
				//if not get by schema
					CodeItem code = CodesManager.GetPropertyBySchema( "ceterms:CreditUnit", item.SchemaName );
					output.CreditUnitTypeId = code.Id;
				}
			}
			output.CreditUnitTypeDescription = GetData( input.CreditUnitTypeDescription );
			output.CreditUnitValue = SetData( input.CreditUnitValue, 0.5M );

			if ( IsValidDate( input.LastUpdated ) )
				output.LastUpdated = input.LastUpdated;

		}
		public static void MapFromDB( DBEntity from, ThisEntity to,
				bool includingProperties = false,
				bool includingProfiles = true,
				bool forEditView = true,
				bool includeWhereUsed = true )
		{

			//TODO add a tomap basic, and handle for lists
			to.Id = from.Id;
			to.RowId = from.RowId;
			to.EntityStateId = ( int ) ( from.EntityStateId ?? 1 );

			if ( IsGuidValid( from.OwningAgentUid ) )
			{
				to.OwningAgentUid = ( Guid ) from.OwningAgentUid;
				to.OwningOrganization = OrganizationManager.GetForSummary( to.OwningAgentUid );

				//get roles
				OrganizationRoleProfile orp = Entity_AgentRelationshipManager.AgentEntityRole_GetAsEnumerationFromCSV( to.RowId, to.OwningAgentUid );
				to.OwnerRoles = orp.AgentRole;
			}

			to.Name = from.Name;
			to.Description = from.Description == null ? "" : from.Description;
            to.SubjectWebpage = from.SubjectWebpage;
            if (IsValidDate( from.Created ))
                to.Created = ( DateTime ) from.Created;

            if (IsValidDate( from.LastUpdated ))
                to.LastUpdated = ( DateTime ) from.LastUpdated;
            to.CTID = from.CTID;
            if (string.IsNullOrWhiteSpace( to.CTID ) || to.EntityStateId < 3)
            {
                to.IsReferenceVersion = true;
                return;
            }

            to.CredentialRegistryId = from.CredentialRegistryId;

			to.AvailabilityListing = from.AvailabilityListing;

			
			to.CodedNotation = from.IdentificationCode;
			to.AvailableOnlineAt = from.AvailableOnlineAt;
			to.DeliveryTypeDescription = from.DeliveryTypeDescription;
			to.VerificationMethodDescription = from.VerificationMethodDescription;
			
			//assumes only one identifier type per class
			to.VersionIdentifierList = Entity_IdentifierValueManager.GetAll( to.RowId, Entity_IdentifierValueManager.LEARNING_OPP_VersionIdentifier );

			if ( IsValidDate( from.DateEffective ) )
				to.DateEffective = ( ( DateTime ) from.DateEffective ).ToShortDateString();
			else
				to.DateEffective = "";

			if ( ( from.InLanguageId ?? 0 ) > 0 )
			{
				to.InLanguageId = ( int ) from.InLanguageId;
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
				to.InLanguage = to.InLanguageCodeList[ 0 ].TextValue;

			to.CreditHourType = from.CreditHourType ?? "";
			to.CreditHourValue = ( from.CreditHourValue ?? 0M );
			to.CreditUnitTypeId = ( from.CreditUnitTypeId ?? 0 );
			to.CreditUnitTypeDescription = from.CreditUnitTypeDescription;
			to.CreditUnitValue = from.CreditUnitValue ?? 0M;



			to.Subject = Entity_ReferenceManager.GetAll( to.RowId, CodesManager.PROPERTY_CATEGORY_SUBJECT );

			to.Keyword = Entity_ReferenceManager.GetAll( to.RowId, CodesManager.PROPERTY_CATEGORY_KEYWORD );


			//properties
			if ( includingProperties )
			{
				to.DeliveryType = EntityPropertyManager.FillEnumeration( to.RowId, CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE );
				to.LearningMethodType = EntityPropertyManager.FillEnumeration( to.RowId, CodesManager.PROPERTY_CATEGORY_Learning_Method_Type );

				to.Addresses = Entity_AddressManager.GetAll( to.RowId );

				// Begin edits - Need these to populate Credit Unit Type -  NA 3/31/2017
				if ( to.CreditUnitTypeId > 0 )
				{
					to.CreditUnitType = new Enumeration();
					var match = CodesManager.GetEnumeration( "creditUnit" ).Items.FirstOrDefault( m => m.CodeId == to.CreditUnitTypeId );
					if ( match != null )
					{
						to.CreditUnitType.Items.Add( match );
					}
				}

				//Fix costs
				to.EstimatedCost = CostProfileManager.GetAll( to.RowId );

				to.FinancialAssistance = Entity_FinancialAlignmentProfileManager.GetAll( to.RowId );

				//Include currencies to fix null errors in various places (detail, compare, publishing) - NA 3/17/2017
				var currencies = CodesManager.GetCurrencies();
				//Include cost types to fix other null errors - NA 3/31/2017
				var costTypes = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_CREDENTIAL_ATTAINMENT_COST );
				foreach ( var cost in to.EstimatedCost )
				{
					cost.CurrencyTypes = currencies;

					foreach ( var costItem in cost.Items )
					{
						costItem.DirectCostType.Items.Add( costTypes.Items.FirstOrDefault( m => m.CodeId == costItem.CostTypeId ) );
					}
				}
				//End edits - NA 3/31/2017

			}
			//get condition profiles
			List<ConditionProfile> list = new List<ConditionProfile>();

			list = Entity_ConditionProfileManager.GetAll( to.RowId, false );
			if ( list != null && list.Count > 0 )
			{
				foreach ( ConditionProfile item in list )
				{
					if ( item.ConditionSubTypeId == Entity_ConditionProfileManager.ConditionSubType_LearningOpportunity )
					{
						to.LearningOppConnections.Add( item );
					}
					else if( item.ConnectionProfileTypeId == Entity_ConditionProfileManager.ConnectionProfileType_Requirement )
						to.Requires.Add( item );
					else if ( item.ConnectionProfileTypeId == Entity_ConditionProfileManager.ConnectionProfileType_Recommendation )
						to.Recommends.Add( item );
					else if ( item.ConnectionProfileTypeId == Entity_ConditionProfileManager.ConnectionProfileType_Corequisite )
						to.Corequisite.Add( item );
					else if ( item.ConnectionProfileTypeId == Entity_ConditionProfileManager.ConnectionProfileType_EntryCondition )
						to.EntryCondition.Add( item );
					else
					{
						EmailManager.NotifyAdmin( "Unexpected Condition Profile for learning opportunity", string.Format( "LearningOppId: {0}, ConditionProfileTypeId: {1}", to.Id, item.ConnectionProfileTypeId ) );

						//add to required, for dev only?
						if ( IsDevEnv() )
						{
							item.ProfileName = ( item.ProfileName ?? "" ) + " unexpected condition type of " + item.ConnectionProfileTypeId.ToString();
							to.Requires.Add( item );
						}
					}
				}
			}
			//TODO
			to.CommonConditions = Entity_CommonConditionManager.GetAll( to.RowId );

			to.CommonCosts = Entity_CommonCostManager.GetAll( to.RowId );

			//if ( includingProfiles )
			//{
			//to.InstructionalProgramType = Entity_FrameworkItemManager.FillEnumeration( to.RowId, CodesManager.PROPERTY_CATEGORY_CIP );
			to.InstructionalProgramType = Reference_FrameworksManager.FillEnumeration( to.RowId, CodesManager.PROPERTY_CATEGORY_CIP );
			//to.OtherInstructionalProgramCategory = Entity_ReferenceManager.GetAll( to.RowId, CodesManager.PROPERTY_CATEGORY_CIP );

			//get as ennumerations
			to.OrganizationRole = Entity_AgentRelationshipManager.AgentEntityRole_GetAll_ToEnumeration( to.RowId, true );
			

			//to.QualityAssuranceAction = Entity_QualityAssuranceActionManager.QualityAssuranceActionProfile_GetAll( to.RowId );


			to.EstimatedDuration = DurationProfileManager.GetAll( to.RowId );

			to.Jurisdiction = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( to.RowId );
			to.Region = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( to.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_RESIDENT );

			to.JurisdictionAssertions = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( to.RowId, 3 );

			//TODO - re: forEditView, not sure about approach for learning opp parts
			//for now getting all, although may only need as links - except may also need to get competencies
			bool forProfilesList = false;

            MapFromDB_HasPart( to, forProfilesList, forEditView );
   //         to.HasPart = Entity_LearningOpportunityManager.LearningOpps_GetAll( to.RowId, forEditView, forProfilesList );
			//foreach ( ThisEntity e in to.HasPart )
			//{
			//	if ( e.HasCompetencies || e.ChildHasCompetencies )
			//	{
			//		to.ChildHasCompetencies = true;
			//		break;
			//	}
			//}
			
			MapFromDB_Competencies( to );

				//16-09-02 mp - always get for now
				//really only needed for detail view
				//===> need a means to determine request is from microsearch, so only minimal is returned!
				//if ( includeWhereUsed )
				//{
				to.WhereReferenced = new List<string>();
				if ( from.Entity_LearningOpportunity != null && from.Entity_LearningOpportunity.Count > 0 )
				{
					//the Entity_LearningOpportunity could be for a parent lopp, or a condition profile
					foreach ( EM.Entity_LearningOpportunity item in from.Entity_LearningOpportunity )
					{
						to.WhereReferenced.Add( string.Format( "EntityUid: {0}, Type: {1}", item.Entity.EntityUid, item.Entity.Codes_EntityTypes.Title ) );
						if ( item.Entity.EntityTypeId == CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE )
						{
							to.IsPartOf.Add( GetAs_IsPartOf( item.Entity.EntityUid, forEditView ) );
						}
						else if ( item.Entity.EntityTypeId == CodesManager.ENTITY_TYPE_CONNECTION_PROFILE )
						{
							ConditionProfile cp = CondProfileMgr.GetAs_IsPartOf( item.Entity.EntityUid );
							to.IsPartOfConditionProfile.Add( cp );
							//need to check cond prof for parent of credential
							//will need to ensure no dups, or realistically, don't do the direct credential check
							if ( cp.ParentCredential != null && cp.ParentCredential.Id > 0 )
							{
								//to.IsPartOfCredential.Add( cp.ParentCredential );
								AddCredentialReference( cp.ParentCredential.Id, to );
							}
					}
					else if ( item.Entity.EntityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL )
					{
						//to.IsPartOfCredential.Add( CredentialManager.GetBasic( item.Entity.EntityUid, false ) );
						AddCredentialReference( (int)item.Entity.EntityBaseId, to );
					}
				}
			}

		}
        public static void MapFromDB_HasPart( ThisEntity to, bool forProfilesList, bool forEditView )
        {
            to.HasPart = Entity_LearningOpportunityManager.LearningOpps_GetAll( to.RowId, forEditView, forProfilesList );
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
			//TODO - add competencies
			to.TeachesCompetencies = Entity_CompetencyManager.GetAllAs_CredentialAlignmentObjectProfile( to.RowId);
			if ( to.TeachesCompetencies.Count > 0 )
				to.HasCompetencies = true;

			to.TeachesCompetenciesFrameworks = Entity_CompetencyManager.GetAllAs_CAOFramework( to.RowId );
			if ( to.TeachesCompetenciesFrameworks.Count > 0 )
				to.HasCompetencies = true;

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
		#endregion
	}
}

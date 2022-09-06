using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.InteropServices;

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
		static string EntityType = "Organization";
		static int EntityTypeId = CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION;

		EntityManager entityMgr = new EntityManager();
		Entity_ReferenceManager erm = new Entity_ReferenceManager();

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
			DateTime lastUpdated = System.DateTime.Now;

			try
			{
				using ( var context = new EntityContext() )
				{
					//note for import, may still do updates?
					if ( ValidateProfile( entity, ref status ) == false )
					{
						return false;
					}
					if ( entity?.Id == 0 )
					{
						//just in case
						var exists = GetSummaryByCtid( entity.CTID, true );
						if ( exists?.Id > 0 )
							entity.Id = exists.Id;
					}

					if ( entity.Id > 0 )
					{
						DBEntity efEntity = context.Organization
								.SingleOrDefault( s => s.Id == entity.Id );

						if ( efEntity != null && efEntity.Id > 0 )
						{

							//for updates, chances are some fields will not be in interface, don't map these (ie created stuff)
							//**ensure rowId is passed down for use by profiles, etc
							entity.RowId = efEntity.RowId;

							MapToDB( entity, efEntity );

							//19-05-21 mp - should add a check for an update where currently is deleted
							if ( ( efEntity.EntityStateId ?? 0 ) == 0 )
							{
								SiteActivity sa = new SiteActivity()
								{
									ActivityType = "Organization",
									Activity = "Import",
									Event = "Reactivate",
									Comment = string.Format( "Organization had been marked as deleted, and was reactivted by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ),
									ActivityObjectId = entity.Id
								};
								new ActivityManager().SiteActivityAdd( sa );

								//identify credentials and other that will need to be reloaded in elasitc and caches
								//the AgentRelationships should auto update, same for owner, maybe
							}
							//assume and validate, that if we get here we have a full record
							if ( ( efEntity.EntityStateId ?? 1 ) != 2 )
							{
								efEntity.EntityStateId = entity.EntityStateId = 3;
								
							}

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
								if ( IsValidDate( status.EnvelopeUpdatedDate ) )
									efEntity.LastUpdated = status.LocalUpdatedDate;
								else
									efEntity.LastUpdated = DateTime.Now;
								//NOTE efEntity.EntityStateId is set to 0 in delete method )
								count = context.SaveChanges();
								//can be zero if no data changed
								if ( count >= 0 )
								{
									//
									entity.RowId = efEntity.RowId;
									entity.LastUpdated = ( DateTime )efEntity.LastUpdated;
									entity.Id = efEntity.Id;
									
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
							entity.LastUpdated = lastUpdated;
							UpdateEntityCache( entity, ref status );
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
				string message = HandleDBValidationError( dbex, thisClassName + string.Format( ".Save() id: {0}, Name: {1}", entity.Id, entity.Name ), "Organization" );
				status.AddError( "Error - the save was not successful. " + message );
				LoggingHelper.LogError( dbex, thisClassName + string.Format( ".Save. id: {0}, Name: {1}", entity.Id, entity.Name ) );
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
		/// <param name="status"></param>
		/// <returns></returns>
		private int Add( ThisEntity entity, ref SaveStatus status )
		{
			DBEntity efEntity = new DBEntity();
			using ( var context = new EntityContext() )
			{
				try
				{
					//while should not be necessary, do a quick check to see if CTID exists
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
					efEntity.EntityStateId = entity.EntityStateId = 3;

					context.Organization.Add( efEntity );

					// submit the change to database
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						//
						entity.RowId = efEntity.RowId;
						entity.Created = ( DateTime )efEntity.Created;
						entity.LastUpdated = ( DateTime )efEntity.LastUpdated;
						entity.Id = efEntity.Id;
						//
						UpdateEntityCache( entity, ref status );
						//
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
						status.AddError( thisClassName + "Error - the add was not successful. " + message );
						EmailManager.NotifyAdmin( "OrganizationManager. Add Failed", message );
					}
				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					string message = HandleDBValidationError( dbex, thisClassName + ".Add() Org: " + entity.Name, "Organization" );
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

		/// <summary>
		/// Add an organization blank node/Reference Org
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public int AddReferenceOrganization( ThisEntity entity, ref SaveStatus status )
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
						status.AddError( thisClassName + string.Format( ". AddReferenceOrganization() The organization is incomplete. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ) );
						return 0;
					}

					//only add DB required properties
					//NOTE - an entity will be created via trigger
					efEntity.EntityStateId = 2;
					efEntity.Name = entity.Name.Replace( " &amp; ", " and " ).Replace(" & "," and ");
					efEntity.Description = entity.Description;
					efEntity.SubjectWebpage = entity.SubjectWebpage;
					efEntity.ISQAOrganization = entity.ISQAOrganization;
					if ( entity.ISQAOrganization )
						efEntity.EntityTypeId = CodesManager.ENTITY_TYPE_QAORGANIZATION;
					else
						efEntity.EntityTypeId = CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION;

					//TODO  - this is to be a list now (some time ago)
					//prepart for being JSON, by just storing
					if ( !string.IsNullOrWhiteSpace( entity.AvailabilityListing ) )
						efEntity.AvailabilityListing = entity.AvailabilityListing;
					else
						efEntity.AvailabilityListing = null;

					if ( IsValidGuid( entity.RowId ) )
						efEntity.RowId = entity.RowId;
					else
						efEntity.RowId = Guid.NewGuid();
					//set to return, just in case
					entity.RowId = efEntity.RowId;
					//WHY - could have a reference to a full org - actually full org would always have to exist
					efEntity.IsThirdPartyOrganization = true;
					efEntity.Created = System.DateTime.Now;
					efEntity.LastUpdated = System.DateTime.Now;
					//set to active 
					var defStatus = CodesManager.Codes_PropertyValue_GetBySchema( CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS_ACTIVE );
					efEntity.LifeCycleStatusTypeId = defStatus.Id;

					context.Organization.Add( efEntity );
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						entity.Id = efEntity.Id;
						entity.RowId = efEntity.RowId;
						entity.Created = ( DateTime )efEntity.Created;
						entity.LastUpdated = ( DateTime )efEntity.LastUpdated;
						UpdateEntityCache( entity, ref status );

						if ( entity.ISQAOrganization )
						{
							AddQAOrgTypeProperty( entity, ref status );
						}						

						SiteActivity sa = new SiteActivity()
						{
							ActivityType = "Organization",
							Activity = "Import",
							Event = "Add Reference Organization",
							Comment = string.Format( "A Reference Organization was added by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ),
							ActivityObjectId = entity.Id
						};
						new ActivityManager().SiteActivityAdd( sa );

						//if ( erm.Add( entity.SocialMediaPages, entity.RowId, CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, ref status, CodesManager.PROPERTY_CATEGORY_ORGANIZATION_SOCIAL_MEDIA, true ) == false )
						//{
						//	//isAllValid = false;
						//}
						//call updateParts to handle recent and future additions
						UpdateParts( entity, ref status );
						entity.Id = efEntity.Id;
						return efEntity.Id;
					}

					status.AddError( thisClassName + ". AddReferenceOrganization() Error - the save was not successful, but no message provided. " );
				}
			}

			catch ( Exception ex )
			{
				string message = FormatExceptions( ex );
				LoggingHelper.LogError( ex, thisClassName + string.Format( ".AddReferenceOrganization. Name:  {0}, SubjectWebpage: {1}", entity.Name, entity.SubjectWebpage ) );
				status.AddError( thisClassName + string.Format( ".AddReferenceOrganization(). Error - the save was not successful. Name:  {0}, SubjectWebpage: {1}, message: ", entity.Name, entity.SubjectWebpage, message ) );

			}
			return 0;
		}
		public void UpdateEntityCache( ThisEntity document, ref SaveStatus status )
		{
			EntityCache ec = new EntityCache()
			{
				EntityTypeId = 2, //EntityTypeId,
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
				OwningAgentUID = document.OwningAgentUid,
				OwningOrgId = document.OrganizationId
			};
			var statusMessage = "";
			if ( new EntityManager().EntityCacheSave( ec, ref statusMessage ) == 0 )
			{
				status.AddError( thisClassName + string.Format( ".UpdateEntityCache for '{0}' ({1}) failed: {2}", document.Name, document.Id, statusMessage ) );
			}
		}

		public void AddQAOrgTypeProperty( ThisEntity entity, ref SaveStatus status )
		{
			entity.AgentType = new Enumeration() { Name = "OrgTypes" };
			
			//var ci = CodesManager.GetPropertyBySchema( CodesManager.PROPERTY_CATEGORY_ORGANIZATION_TYPE, "orgType:QualityAssurance" );
			//actually the update method uses SchemaName, not Id
			var item = new EnumeratedItem() { CategoryId = CodesManager.PROPERTY_CATEGORY_ORGANIZATION_TYPE, SchemaName = "orgType:QualityAssurance" };

			entity.AgentType.Items.Add( item );

			EntityPropertyManager mgr = new EntityPropertyManager();
			if ( mgr.AddProperties( entity.AgentType, entity.RowId, CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, CodesManager.PROPERTY_CATEGORY_ORGANIZATION_TYPE, true, ref status ) == false )
			{

			}
		}
		public int AddPendingRecord( Guid entityUid, string ctid, string registryAtId, ref SaveStatus status )
		{
			DBEntity efEntity = new DBEntity();
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
					var entity = GetSummaryByCtid( ctid, true );
					if ( entity != null && entity.Id > 0 )
						return entity.Id;

					//only add DB required properties
					//NOTE - an entity will be created via trigger
					efEntity.Name = "Placeholder until full document is downloaded";
					efEntity.Description = "Placeholder until full document is downloaded";
					efEntity.EntityStateId = 1;
					//not sure if we know the actual entityTypeId at this time
					efEntity.EntityTypeId = entity.EntityTypeId > 0 ? entity.EntityTypeId : 2;
					efEntity.RowId = entityUid;
					//watch that Ctid can be  updated if not provided now!!
					efEntity.CTID = ctid;
					efEntity.SubjectWebpage = registryAtId;

					efEntity.Created = System.DateTime.Now;
					efEntity.LastUpdated = System.DateTime.Now;
					//set to active 
					var defStatus = CodesManager.Codes_PropertyValue_GetBySchema( CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS_ACTIVE );
					efEntity.LifeCycleStatusTypeId = defStatus.Id;

					context.Organization.Add( efEntity );
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

		public bool UpdateJson( int recordId, string json )
		{
			//SaveStatus status
			bool isValid = true;
			int count = 0;
			try
			{
				using ( var context = new EntityContext() )
				{
					DBEntity efEntity = context.Organization
							.SingleOrDefault( s => s.Id == recordId );
					if ( efEntity != null && efEntity.Id > 0 )
					{
						efEntity.JsonProperties = json;

						if ( HasStateChanged( context ) )
						{
							efEntity.LastUpdated = DateTime.Now;

							count = context.SaveChanges();
							//can be zero if no data changed
							if ( count >= 0 )
							{
								isValid = true;
							}
							else
							{
								//?no info on error
								LoggingHelper.LogError( string.Format( "Error - the Json update was not successful for organization: {0}, Id: {1}. But no reason is present.", efEntity.Name, efEntity.Id ), false );
								isValid = false;
							}
						}
					}
					else
					{
						LoggingHelper.LogError( string.Format( "Error - UpdateJson failed, as record was not found. recordId: {0}", recordId ), false );
						isValid = false;
					}
				}

			}
			catch ( Exception ex )
			{
				string message = FormatExceptions( ex );
				LoggingHelper.LogError( thisClassName + ".UpdateJson(). Error - the save was not successful. " + message, false );
				isValid = false;
			}

			return isValid;
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

			if ( erm.Add( entity.Keyword, entity.RowId, CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, ref status, CodesManager.PROPERTY_CATEGORY_KEYWORD, false ) == false )
				isAllValid = false;

			if ( erm.Add( entity.AlternateNames, entity.RowId, CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, ref status, CodesManager.PROPERTY_CATEGORY_ALTERNATE_NAME, false ) == false )
				isAllValid = false;
			//how to handle notifications on 'other'?
			if ( erm.Add( entity.IdentificationCodes, entity.RowId, CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, ref status, CodesManager.PROPERTY_CATEGORY_ORGANIZATION_IDENTIFIERS, true ) == false )
				isAllValid = false;

			if ( erm.Add( entity.SocialMediaPages, entity.RowId, CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, ref status, CodesManager.PROPERTY_CATEGORY_ORGANIZATION_SOCIAL_MEDIA, true ) == false )
				isAllValid = false;

			if ( erm.Add( entity.Emails, entity.RowId, CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, ref status, CodesManager.PROPERTY_CATEGORY_EMAIL_TYPE, true ) == false )
				isAllValid = false;
			//
			if ( erm.Add( entity.SameAs, entity.RowId, CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, ref status, CodesManager.PROPERTY_CATEGORY_SAME_AS, true ) == false )
				isAllValid = false;

			//contact points (does delete all - check on implications from address manager)
			new Entity_ContactPointManager().SaveList( entity.ContactPoint, entity.RowId, ref status );

			//addresses
			new Entity_AddressManager().SaveList( entity.Addresses, entity.RowId, ref status );

			if ( entity.EntityStateId < 3 )
				return isAllValid;

			//==========================================


			Entity_ReferenceFrameworkManager erfm = new Entity_ReferenceFrameworkManager();
			erfm.DeleteAll( relatedEntity, ref status );

			//Entity_FrameworkItemManager efim = new Entity_FrameworkItemManager();
			if ( erfm.SaveList( relatedEntity.Id, CodesManager.PROPERTY_CATEGORY_NAICS, entity.IndustryTypes, ref status ) == false )
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

			if ( mgr.AddProperties( entity.AgentType, entity.RowId, CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, CodesManager.PROPERTY_CATEGORY_ORGANIZATION_TYPE, entity.EntityStateId == 3, ref status ) == false )
				isAllValid = false;

			//TODO - may want a check to toggle the IsQaOrg property. It is used for other checks
			// however this would not be dependable, need to query on MapFromDB

			if ( mgr.AddProperties( entity.AgentSectorType, entity.RowId, CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, CodesManager.PROPERTY_CATEGORY_ORGANIZATION_SECTORTYPE, false, ref status ) == false )
			{
				isAllValid = false;
			}

			//using Entity.Property in workIT, rather than Organization.Service
			if ( mgr.AddProperties( entity.ServiceType, entity.RowId, CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, CodesManager.PROPERTY_CATEGORY_ORG_SERVICE, false, ref status ) == false )
				isAllValid = false;
			//
			if ( mgr.AddProperties( entity.LifeCycleStatusType, entity.RowId, CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, false, ref status ) == false )
				isAllValid = false;

			//}
			return isAllValid;
		}

		public void AddProfiles( ThisEntity entity, Entity relatedEntity, ref SaveStatus status )
		{
			//Identifier
			new Entity_IdentifierValueManager().SaveList( entity.Identifier, entity.RowId, Entity_IdentifierValueManager.ORGANIZATION_Identifier, ref status, true );

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
			//probably should just check for existance of parent, and add there, except we won't know if it is a department or subsidiary relationship!
			//or rational for uses Entity.Asserts
			mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_PARENT_ORG, entity.ParentOrganization, ref status );
			//
			mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_PUBLISHEDBY, entity.PublishedBy, ref status );
			//this will not be part of the org import. Add to the dsp import
			//mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_PROVIDES_OUTCOMES, entity.ProvidesOutcomes, ref status );

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
			//21-05-26 mp - prototyping use EntityAssertion for provides outcomes. If using the latter, cannot delete all assertions, only those for the current context. Alternately, use Entity.AgentRelationship. In this case also cannot arbitrarily delete all relationships
			//this would mean needing to add code when deleting a dataset.
			mgr.DeleteAll( parent, ref status );

			mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_Accredits, entity.Accredits, ref status );
			mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_Approves, entity.Approves, ref status );
			mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_Recognizes, entity.Recognizes, ref status );
			mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_Regulates, entity.Regulates, ref status );
			mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_Renews, entity.Renews, ref status );
			mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_Revokes, entity.Revokes, ref status );

			//NOTE: these are not QA, and should not be assertions?
			//      However, if we only show direct assertions, then will be valid
			//21-07-14 mp - generally should not used EA where the owner and target org are the same. It would seem that it should NEVER be used with OWNS??
			//	OR- note that this is to be balance to entity.AgentRelationship. We don't create an EAR from here but could do a verify. The EAR gets deleted and re-added when a credential is updated.
			//22-03-09 - need to have a means delete any references and their relationships that are not part of this import
			mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_OFFERS, entity.Offers, ref status );
			mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_OWNS, entity.Owns, ref status );	

			return isAllValid;
		} //

		/// <summary>
		/// Delete an Organization, and related Entity
		/// </summary>
		/// <param name="orgId"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		//public bool Delete( int orgId, ref string statusMessage )
		//{
		//    bool isValid = true;
		//    bool doingVirtualDelete = true;
		//    if ( orgId == 0 )
		//    {
		//        statusMessage = "Error - missing an identifier for the Organization";
		//        return false;
		//    }
		//    using ( var context = new EntityContext() )
		//    {
		//        try
		//        {
		//            //ensure exists
		//            DBEntity efEntity = context.Organization
		//                        .FirstOrDefault( s => s.Id == orgId );

		//            if ( efEntity != null && efEntity.Id > 0 )
		//            {
		//                if ( doingVirtualDelete )
		//                {
		//                    statusMessage = string.Format( "Organization: {0}, Id:{1}", efEntity.Name, efEntity.Id );

		//                    //context.Organization.Remove( efEntity );
		//                    efEntity.LastUpdated = System.DateTime.Now;

		//                }
		//                else
		//                {
		//                    Guid rowId = efEntity.RowId;
		//                    int roleCount = 0;
		//                    //check for any existing org roles, and reject delete if any found
		//                    if ( Entity_AgentRelationshipManager.AgentEntityHasRoles( rowId, ref roleCount ) )
		//                    {
		//                        statusMessage = string.Format( "Error - this organization cannot be deleted as there are existing roles {0}.", roleCount );
		//                        return false;
		//                    }
		//                    //16-10-19 mp - we have a 'before delete' trigger to remove the Entity
		//                    //new EntityManager().Delete( rowId, ref statusMessage );

		//                    context.Organization.Remove( efEntity );
		//                }


		//                int count = context.SaveChanges();
		//                if ( count > 0 )
		//                {
		//                    isValid = true;

		//                }
		//            }
		//            else
		//            {
		//                statusMessage = "Error - delete failed, as record was not found.";
		//            }
		//        }
		//        catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
		//        {
		//            string message = HandleDBValidationError( dbex, thisClassName + ".Save() ", "Organization" );
		//            statusMessage = "Error - the Delete was not successful. " + message;
		//        }
		//        catch ( Exception ex )
		//        {
		//            statusMessage = FormatExceptions( ex );
		//            LoggingHelper.LogError( ex, thisClassName + ".Organization_Delete()" );

		//            if ( statusMessage.ToLower().IndexOf( "the delete statement conflicted with the reference constraint" ) > -1 )
		//            {
		//                statusMessage = "Error: this organization cannot be deleted as it is being referenced by other items, such as credentials. These associations must be removed before this organization can be deleted.";
		//            }
		//        }
		//    }

		//    return isValid;
		//}

		/// <summary>
		/// Delete by envelopeId, or ctid
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

			using ( var context = new EntityContext() )
			{
				try
				{
					context.Configuration.LazyLoadingEnabled = false;
					DBEntity efEntity = context.Organization
								.FirstOrDefault( s => s.CTID == ctid );

					if ( efEntity != null && efEntity.Id > 0 )
					{
						Guid rowId = efEntity.RowId;
						//issue check for ivy tech and others
						if ( efEntity.CTID.ToLower() == "ce-1abb6c52-0f8c-4b17-9f89-7e9807673106" )
						{
							string msg2 = string.Format( "Request to delete Ivy Tech was encountered - ignoring. Organization. Id: {0}, Name: {1}, Ctid: {2}", efEntity.Id, efEntity.Name, efEntity.CTID );
							EmailManager.NotifyAdmin( "Encountered Request to delete Ivy Tech", msg2 );
							return true;
						}
						if ( efEntity.CTID.ToLower() == "ce-6686c066-0948-4c0b-8fb0-43c6af625a70" )
						{
							string msg2 = string.Format( "Request to delete NIMS was encountered - ignoring. Organization. Id: {0}, Name: {1}, Ctid: {2}", efEntity.Id, efEntity.Name, efEntity.CTID);
							EmailManager.NotifyAdmin( "Encountered Request to delete NIMS", msg2 );
							return true;
						}
						if ( efEntity.CTID.ToLower() == "ce-2ecc2ce8-b134-4a3a-8b17-863aa118f36e" )
						{
							//string msg2 = string.Format( "Request to delete Ball State University was encountered - ignoring. Organization. Id: {0}, Name: {1}, Ctid: {2}, EnvelopeId: {3}", efEntity.Id, efEntity.Name, efEntity.CTID, envelopeId );
							//EmailManager.NotifyAdmin( "Encountered Request to delete Ball State University", msg2 );
							//return true;
						}
						//need to remove from Entity.
						//-using before delete trigger - verify won't have RI issues
						string msg = string.Format( " Organization. Id: {0}, Name: {1}, Ctid: {2}.", efEntity.Id, efEntity.Name, efEntity.CTID );

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
								Activity = "Import",
								Event = "Delete",
								Comment = msg,
								ActivityObjectId = efEntity.Id
							} );
							//delete cache
							new EntityManager().EntityCacheDelete( 2, efEntity.Id, ref statusMessage );

							isValid = true;
                            //add pending request 
                            List<String> messages = new List<string>();
                            new SearchPendingReindexManager().AddDeleteRequest( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, efEntity.Id, ref messages );
							//also check for any relationships
							new Entity_AgentRelationshipManager().ReindexAgentForDeletedArtifact( efEntity.RowId );
							//what about manifests, will not be able to see these, and realistically should be deleted!
							//which leads to credentials, etc that may be referencing a cost manifest. 

						}
                    }
                    else
                    {
                        statusMessage = thisClassName + ".Delete() Warning No action taken, as the record was not found.";
                    }
                }
                catch ( Exception ex )
                {
                    LoggingHelper.LogError( ex, thisClassName + ".Delete(envelopeId, ctid)" );
                    statusMessage = FormatExceptions( ex );
                    isValid = false;
                    if ( statusMessage.ToLower().IndexOf( "the delete statement conflicted with the reference constraint" ) > -1 )
                    {
                        statusMessage = thisClassName + "Error: this record cannot be deleted as it is being referenced by other items, such as roles or credentials. These associations must be removed before this organization can be deleted.";
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

			//
			var defStatus = CodesManager.Codes_PropertyValue_GetBySchema( CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS_ACTIVE );
			if ( profile.LifeCycleStatusType == null || profile.LifeCycleStatusType.Items == null || profile.LifeCycleStatusType.Items.Count == 0 )
			{
				profile.LifeCycleStatusTypeId = defStatus.Id;
			}
			else
			{
				var schemaName = profile.LifeCycleStatusType.GetFirstItem().SchemaName;
				CodeItem ci = CodesManager.Codes_PropertyValue_GetBySchema( CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, schemaName );
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
			//	status.AddError( "A Subject Webpage must be entered" );

			//else 
			if ( !IsUrlValid( profile.SubjectWebpage, ref commonStatusMessage ) )
				status.AddWarning( "The Subject Webpage Url is invalid. " + commonStatusMessage );

			if ( string.IsNullOrWhiteSpace( profile.AgentDomainType ) )
			{
				status.AddWarning( "The type of this organization is required. It should be either CredentialOrganization, or QACredentialOrganization. Defaulting to CredentialOrganization" );
				profile.AgentDomainType = "CredentialOrganization";
				profile.AgentDomainTypeId = 1;
			}
			else if ( profile.AgentDomainType.IndexOf( "QACredentialOrganization" ) > -1 )
			{
				profile.AgentDomainTypeId = 2;
				profile.ISQAOrganization = true;
			}
			else if ( profile.AgentDomainType.IndexOf( "CredentialOrganization" ) > -1 )
			{
				profile.AgentDomainTypeId = 1;
			}
			else
				profile.AgentDomainTypeId = 3;


			if ( !IsUrlValid( profile.AgentPurpose, ref commonStatusMessage ) )
				status.AddWarning( "The Agent Purpose Url is invalid. " + commonStatusMessage );

			if ( !IsUrlValid( profile.MissionAndGoalsStatement, ref commonStatusMessage ) )
				status.AddWarning( "The Mission and Goals Statement Url is invalid. " + commonStatusMessage );

			//if ( !IsUrlValid( profile.AvailabilityListing, ref commonStatusMessage ) )
			//	status.AddWarning( "The Availability Listing Url is invalid. " + commonStatusMessage );

			if ( !IsUrlValid( profile.Image, ref commonStatusMessage ) )
			{
				status.AddWarning( "The Image Url is invalid. " + commonStatusMessage );
			}


			return status.WasSectionValid;
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
				DBEntity item = context.Organization.FirstOrDefault( s => s.Id == id );

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

					MapFromDB( item, entity
						, true      //includingProperties
						, includeCredentials
						, includingRoles );
				}
			}

			return entity;
		}
		public static ThisEntity GetDetailForAPI( int id, OrganizationRequest request )
		{

			//bool includeCredentials = true;
			//bool includingRoles = true;
			ThisEntity entity = new ThisEntity();
			//var request = new OrganizationRequest( 2 );
			//1=summary, 2=details
			//bool includingProcessProfileDetails = false;
			//bool includingVerificationServicesProfileDetails = false;
			using ( var context = new Data.Tables.workITEntities() )
			{
				DBEntity item = context.Organization.FirstOrDefault( s => s.Id == id );

				if ( item != null && item.Id > 0 )
				{
					//check for virtual deletes
					if ( item.EntityStateId == 0 )
						return entity;

					MapFromDB( item, entity, request );
				}
			}

			return entity;
		}
		public static ThisEntity GetDetailForAPI( string ctid, OrganizationRequest request )
		{

			//bool includeCredentials = true;
			//bool includingRoles = true;
			ThisEntity entity = new ThisEntity();
			if ( string.IsNullOrWhiteSpace( ctid ) )
				return entity;
			//var request = new OrganizationRequest( 2 );
			//1=summary, 2=details
			//bool includingProcessProfileDetails = false;
			//bool includingVerificationServicesProfileDetails = false;
			using ( var context = new Data.Tables.workITEntities() )
			{
				DBEntity item = context.Organization.FirstOrDefault( s => s.CTID == ctid );

				if ( item != null && item.Id > 0 )
				{
					//check for virtual deletes
					if ( item.EntityStateId == 0 )
						return entity;

					MapFromDB( item, entity, request );

				}
			}

			return entity;
		}
		public static ThisEntity GetSummaryByCtid( string ctid, bool acceptingPendingStatus = true )
		{
			ThisEntity to = new ThisEntity();
			if ( string.IsNullOrWhiteSpace( ctid ) )
				return to;
			bool includingExternalData = false;
			using ( var context = new EntityContext() )
			{
				context.Configuration.LazyLoadingEnabled = false;
				DBEntity from = context.Organization
						.FirstOrDefault( s => s.CTID.ToLower() == ctid.ToLower() );

				if ( from != null && from.Id > 0 )
				{
					//if a placeholder, skip - only if properly handled later!					
					if ( from.EntityStateId < 2 && !acceptingPendingStatus )
					{
						//22-04-12 mp - a placeholder is not being handled correctly. May need a different method when called from import
						//22-04-26 mp - making accepting pending as the default as is mostly called from the import to get the publishing org
						return to;
					}
					MapFromDB_ForSummary( from, to, includingExternalData );
				}
			}
			return to;
		}
		public static ThisEntity GetMinimumSummaryByCtid( string ctid, bool fromImport = false )
		{
			ThisEntity to = new ThisEntity();
			if ( string.IsNullOrWhiteSpace( ctid ) )
				return to;

			using ( var context = new EntityContext() )
			{
				context.Configuration.LazyLoadingEnabled = false;
				DBEntity from = context.Organization
						.FirstOrDefault( s => s.CTID.ToLower() == ctid.ToLower() );

				if ( from != null && from.Id > 0 )
				{
					//if a placeholder, skip - only if properly handled later!					
					if ( from.EntityStateId < 2 && !fromImport )
					{
						//22-04-12 mp - a placeholder is not be handled correctly. May need a different method when called from import
						return to;
					}
					MapFromDB_ForSummary( from, to, false );
				}
			}
			return to;
		}

		//
		//public static ThisEntity GetBySubjectWebpage( string swp )
		//{
		//	ThisEntity to = new ThisEntity();
		//	if ( string.IsNullOrWhiteSpace( swp ) )
		//		return to;
		//	using ( var context = new EntityContext() )
		//	{
		//		context.Configuration.LazyLoadingEnabled = false;
		//		DBEntity from = context.Organization
		//				.FirstOrDefault( s => s.SubjectWebpage.ToLower() == swp.ToLower() );

		//		if ( from != null && from.Id > 0 )
		//		{
		//			MapFromDB_ForSummary( from, to );
		//		}
		//	}
		//	return to;
		//}

		/// <summary>
		/// Get by name and swp - usually attempting to resolve a reference org. 
		/// However in the future, may run into multiple concrete orgs?
		/// OR, should we use elastic?
		/// </summary>
		/// <param name="name"></param>
		/// <param name="swp"></param>
		/// <returns></returns>
		public static ThisEntity GetByName_SubjectWebpage(string name, string swp)
		{
			//20-12-17 - other checks are allowing searching with a null swp. As this search does partials, continue to not allowing nulls - will need to revisit
			//21-12-07 - the URL checking should use the domain only, and handle with and without www

			ThisEntity to = new ThisEntity();
			//truncate the protocal identifier
			if (string.IsNullOrWhiteSpace(swp))
				return null;
			if (swp.IndexOf("//") == -1)
				return null;
			bool hasHttps = false;
			if (swp.ToLower().IndexOf("https:") > -1)
				hasHttps = true;
			//
			name = name.Replace( "&amp;", "and" );
			name = name.Replace( " & ", " and " );
			//swp = swp.Substring( swp.IndexOf( "//" ) + 2 );
			swp = swp.ToLower().TrimEnd( '/' );
			var host = new Uri(swp).Host;
			var domain = host.Substring(host.LastIndexOf('.', host.LastIndexOf('.') - 1) + 1);
			//
			using (var context = new EntityContext())
			{
				//s.Name.ToLower() == name.ToLower() && 
				context.Configuration.LazyLoadingEnabled = false;
				var list = context.Organization
						.Where(s => s.SubjectWebpage.ToLower().Contains(domain) && s.EntityStateId > 1)
						.OrderByDescending(s => s.EntityStateId)
						.ThenBy(s => s.Name)
						.ToList();
				int cntr = 0;

				ActivityManager amgr = new ActivityManager();
				if ( list != null && list.Any() )
				{
					foreach ( var from in list )
					{
						cntr++;
						//any clean up
						var nameCheck = from.Name.Replace("&amp;", "and");
						nameCheck = from.Name.Replace( " & ", " and " );
						//any way to check further?
						//the full org will be returned first
						//may want a secondary check and send notifications if additional full orgs found, or even if multiples are found.
						if ( name.ToLower().Contains( nameCheck.ToLower() ) || nameCheck.ToLower().Contains( name.ToLower() ) )
						{
							//OK, take me
							if ( cntr == 1 || to.Id == 0 )
							{
								//hmmm if input was https and found http, and a reference, should update to https!
								if ( hasHttps && from.SubjectWebpage.StartsWith( "http:" ) )
								{

								}
								//
								MapFromDB_ForSummary( from, to );
								//why not breaking here? It appears the next would be arbitrarily taken
								//if ( from.EntityStateId == 3 )
									break;
							}
							//else
							//{
							//	//21-12-20 mp - skip this now based on improved checks and implementing a merge process
							//	if ( from.EntityStateId == 3 )
							//	{
							//		//could log warning conditions to activity log, and then report out at end of an import?
							//		amgr.SiteActivityAdd( new SiteActivity()
							//		{
							//			ActivityType = "System",
							//			Activity = "Import",
							//			Event = "Organization Reference Check",
							//			Comment = string.Format( "Org get by name/swp. Found additional full org for name: {0}, swp: {1}. First org: {2} ({3})", nameCheck, swp, to.Name, to.Id )
							//		} );

							//	}
							//	MapFromDB_ForSummary( from, to );
							//	break;
							//}
						} else
                        {
							//what can be done if the SWP seems promising but a very different name?
							//report?
							//and/or check if there are multiple
                        }
					}
				} else
                {
					//what else? 
					//could plug in alias checking here?
                }
			}

			return to;
		}
		/// <summary>
		/// Get a basic org - will not return a pending record
		/// </summary>
		/// <param name="rowId"></param>
		/// <returns></returns>
		public static ThisEntity GetBasics( Guid rowId, bool includingExternalData = true )
		{
			ThisEntity output = new ThisEntity();
			if ( !IsGuidValid( rowId ) )
				return output;

			using ( var context = new EntityContext() )
			{
				context.Configuration.LazyLoadingEnabled = false;
				//need to be careful with the entity state check as may be needed for resolution!
				DBEntity from = context.Organization
						.FirstOrDefault( s => s.RowId == rowId && ( s.EntityStateId ?? 1 ) > 1 );

				if ( from != null && from.Id > 0 )
				{
					MapFromDB_ForSummary( from, output, includingExternalData );
				}
			}

			return output;
		}

		//21-05-07 mp noticed this is a duplicate of the above method - except doesn't do the entitystateId check
		public static ThisEntity GetForSummary( Guid id, bool includingExternalData = false )
		{

			ThisEntity to = new ThisEntity();
			if ( ( id == null || id == Guid.Empty ) )
				return to;

			using ( var context = new EntityContext() )
			{
				context.Configuration.LazyLoadingEnabled = false;
				DBEntity from = context.Organization
						.FirstOrDefault( s => s.RowId == id );

				if ( from != null && from.Id > 0 )
				{
					//if a placeholder, skip
					if ( from.EntityStateId < 2 )
						return to;

					MapFromDB_ForSummary( from, to, includingExternalData );
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
		/// Get minimum org for link check mostly
		/// </summary>
		/// <param name="includingRoles"></param>
		/// <returns></returns>
		public static List<ThisEntity> GetAll( ref int totalRows, bool includingRoles = false )
		{
			List<ThisEntity> list = new List<ThisEntity>();
			ThisEntity to = new ThisEntity();

			using ( var context = new EntityContext() )
			{
				context.Configuration.LazyLoadingEnabled = false;
				var results = context.Organization
						.Where( s => s.EntityStateId > 1 );
				totalRows = results.Count();
				foreach (var from in results)
				{
					to = new ThisEntity();
					MapFromDB_ForSummary( from, to, true );
					//may want to check org role targets at some point
					if ( includingRoles )
					{
						GetOrgRoles( to );
					}
					list.Add( to );
				}
			}

			return list;
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
					//21-06-01 mparsons added includingRoles to the map call and using false
					//	next rename to includingExternalData
					MapFromDB_ForSummary( from, to, includingRoles );
					if ( includingRoles )
					{
						//GetOrgRoles( from, to );
					}
				}
			}

			return to;
		}

		public static List<string> Autocomplete( string pFilter, int pageNumber, int pageSize, int userId, ref int pTotalRows )
		{
			bool autocomplete = true;
			var results = new List<string>();
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
				try
				{

					foreach ( DataRow dr in result.Rows )
					{
						item = new ThisEntity();
						item.Id = GetRowColumn( dr, "Id", 0 );
						item.Name = GetRowColumn( dr, "Name", "missing" );
						item.FriendlyName = FormatFriendlyTitle( item.Name );
						item.CTID = GetRowPossibleColumn( dr, "CTID", "" );

						if ( idsOnly || autocomplete )
						{
							list.Add( item );
							continue;
						}
						item.Description = GetRowColumn( dr, "Description", "" );
						string rowId = GetRowColumn( dr, "RowId" );
						item.RowId = new Guid( rowId );
						
						item.SubjectWebpage = GetRowColumn( dr, "SubjectWebpage", "" );
						//item.CanEditRecord = GetRowColumn( dr, "CanEditRecord", false );
						item.CredentialRegistryId = GetRowPossibleColumn( dr, "CredentialRegistryId", "" );

						item.Image = GetRowColumn( dr, "ImageUrl", "" );
						if ( GetRowColumn( dr, "CredentialCount", 0 ) > 0 )
							item.IsACredentialingOrg = true;
						item.ISQAOrganization = GetRowColumn( dr, "IsAQAOrganization", false );

						//item.FrameworksOwnedByResults = dr[ "FrameworksOwnedByList" ].ToString();

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
						item.AgentSectorType = EntityPropertyManager.FillEnumeration( item.RowId, CodesManager.PROPERTY_CATEGORY_ORGANIZATION_SECTORTYPE );
						//End Edit

						list.Add( item );
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + ".Search()" );
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

					item.Image = GetRowColumn( dr, "ImageUrl", "" );
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
					item.AgentSectorType = EntityPropertyManager.FillEnumeration( item.RowId, CodesManager.PROPERTY_CATEGORY_ORGANIZATION_SECTORTYPE );
					//End Edit
					//do we need service type??
					item.ServiceType = EntityPropertyManager.FillEnumeration( item.RowId, CodesManager.PROPERTY_CATEGORY_ORG_SERVICE );

					item.IndustryResults = Fill_CodeItemResults( dr, "NaicsList", CodesManager.PROPERTY_CATEGORY_NAICS, false, false );
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
		#endregion

		#region helpers
		public static void MapFromDB_ForSummary( DBEntity input, ThisEntity output, bool includingExternalData = true )
		{
			//if ( output == null )
			//	output = new ThisEntity();
			output.Id = input.Id;
			output.RowId = input.RowId;
			output.EntityStateId = ( int ) ( input.EntityStateId ?? 1 );

			output.Name = input.Name;
			output.FriendlyName = FormatFriendlyTitle( input.Name );

			output.Description = input.Description;
			output.SubjectWebpage = input.SubjectWebpage;
			if ( input.ImageURL != null && input.ImageURL.Trim().Length > 0 )
				output.Image = input.ImageURL;
			else
				output.Image = null;
			output.CredentialRegistryId = input.CredentialRegistryId;
			output.CTID = input.CTID;
			//
			output.LifeCycleStatusTypeId = input.LifeCycleStatusTypeId;
			if ( output.LifeCycleStatusTypeId > 0 )
			{
				CodeItem ct = CodesManager.Codes_PropertyValue_Get( output.LifeCycleStatusTypeId );
				if ( ct != null && ct.Id > 0 )
				{
					output.LifeCycleStatus = ct.Title;
				}
				//retain example using an Enumeration for by other related tableS??? - old detail page?
				output.LifeCycleStatusType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_STATUS_TYPE );
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
			if ( IsValidDate( input.Created ) )
				output.Created = ( DateTime ) input.Created;

			if ( IsValidDate( input.LastUpdated ) )
				output.LastUpdated = ( DateTime ) input.LastUpdated;
			//TODO - this is to be stored as a list.
			//part 1: always map to AvailabilityListings
			if ( !string.IsNullOrWhiteSpace( input.AvailabilityListing ) )
				output.AvailabilityListings = new List<string>() { input.AvailabilityListing };
			//else if ( from.AvailabilityListings?.Count > 0 )
			//	output.AvailabilityListing = from.AvailabilityListings[0];
			else
				output.AvailabilityListings = null;

			output.AgentPurpose = input.AgentPurpose;
			output.MissionAndGoalsStatement = input.MissionAndGoalsStatement;

			output.ISQAOrganization = input.ISQAOrganization == null ? false : ( bool )input.ISQAOrganization;
			if ( output.ISQAOrganization )
				output.AgentDomainType = "QACredentialOrganization";
			else
				output.AgentDomainType = "CredentialOrganization";
			//
			//check if want data from external, though related tables
			if ( includingExternalData )
			{
				List<ContactPoint> orphans = new List<ContactPoint>();
				output.Addresses = Entity_AddressManager.GetAll( input.RowId, ref orphans );

				output.Emails = Entity_ReferenceManager.GetAll( input.RowId, CodesManager.PROPERTY_CATEGORY_EMAIL_TYPE );

				output.SocialMediaPages = Entity_ReferenceManager.GetAll( input.RowId, CodesManager.PROPERTY_CATEGORY_ORGANIZATION_SOCIAL_MEDIA );
				output.SameAs = Entity_ReferenceManager.GetAll( input.RowId, CodesManager.PROPERTY_CATEGORY_SAME_AS ); //76;
																				 
				//21-03-31 - moved here for performance for summary only 
				output.OrganizationRole_Recipient = Entity_AssertionManager.GetAllCombinedForTarget( 2, output.Id, output.Id );
				
			}
		}

		public static void MapFromDB( DBEntity input, Organization output,
					bool includingProperties,
					bool includeCredentials,
					bool includingRoles,
					bool includingProcessProfileDetails = true,
					bool includingVerificationServicesProfileDetails = true,
					bool includingQAWhereUsed = true

			)
		{
			OrganizationRequest request = new OrganizationRequest( 1 )
			{
				IncludingProperties = includingProperties,
				IncludingRolesAndActions = includingRoles,
				IncludingProcessProfiles = includingProcessProfileDetails,
				IncludingVerificationProfiles = includingVerificationServicesProfileDetails,
				IncludingQAWhereUsed = includingQAWhereUsed
			};
			MapFromDB( input, output, request );
		}

		public static void MapFromDB( DBEntity input, Organization output, OrganizationRequest request )
		{

			DateTime overall = DateTime.Now;


			if ( output == null )
				output = new ThisEntity();

			//21-03-05 - assessing performance of returning everything
			var saveDuration = new TimeSpan();
			DateTime started = DateTime.Now;


			output.Id = input.Id;
			output.RowId = input.RowId;

			output.Name = input.Name;
			output.FriendlyName = FormatFriendlyTitle( input.Name );

			output.Description = input.Description;
			output.SubjectWebpage = input.SubjectWebpage;
			output.EntityStateId = input.EntityStateId ?? 1;

			if ( input.ImageURL != null && input.ImageURL.Trim().Length > 0 )
				output.Image = input.ImageURL;
			else
				output.Image = null;
			output.CredentialRegistryId = input.CredentialRegistryId;
			output.CTID = input.CTID;
			
			if ( IsValidDate( input.Created ) )
				output.Created = ( DateTime ) input.Created;

			if ( IsValidDate( input.LastUpdated ) )
				output.LastUpdated = ( DateTime ) input.LastUpdated;

			output.EntityTypeId = input.EntityTypeId;
			if ( output.EntityTypeId == 0 )
				output.EntityTypeId = 2;

				if ( output.EntityTypeId > 0 )
			{
				CodeItem ct = CodesManager.Codes_EntityType_Get( output.EntityTypeId );
				if ( ct != null && ct.Id > 0 )
				{
					output.EntityType = ct.Title;
					output.EntityTypeLabel = MapEntityTypeLabel( output.EntityTypeId );
					output.EntityTypeSchema = ct.SchemaName;
				}
				else
				{
					output.EntityType = "Learning Opportunity";
					output.EntityTypeLabel = "Learning Opportunity";
					output.EntityTypeSchema = "ceterms:LearningOpportunityProfile";
				}
			}
			//need output use the service types as well. See assignment for output.AgentType below
			output.ISQAOrganization = input.ISQAOrganization == null ? false : ( bool ) input.ISQAOrganization;
			//21-04-20 now need to distinguish organization type of organization
			if ( output.ISQAOrganization )
				output.AgentDomainType = "QACredentialOrganization";
			else
				output.AgentDomainType = "CredentialOrganization";
			//output.IsThirdPartyOrganization = input.IsThirdPartyOrganization != null ? ( bool ) input.IsThirdPartyOrganization : false;


			Entity relatedEntity = EntityManager.GetEntity( output.RowId );
			if ( relatedEntity != null && relatedEntity.Id > 0 )
			{
				output.EntityId = relatedEntity.Id;
				output.EntityLastUpdated = relatedEntity.LastUpdated;
			}
			//22-07-10 - LifeCycleStatusTypeId is now on the organization directly
			output.LifeCycleStatusTypeId = input.LifeCycleStatusTypeId;
			if ( output.LifeCycleStatusTypeId > 0 )
			{
				CodeItem ct = CodesManager.Codes_PropertyValue_Get( output.LifeCycleStatusTypeId );
				if ( ct != null && ct.Id > 0 )
				{
					output.LifeCycleStatus = ct.Title;
				}
				//retain example using an Enumeration for by other related tableS??? - old detail page?
				output.LifeCycleStatusType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_STATUS_TYPE );
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

			//--------------------------
			output.AgentPurpose = input.AgentPurpose;
			output.AgentPurposeDescription = input.AgentPurposeDescription;
			//TODO  - this is to be a list now (some time ago)
			//store as JSON
			//part 1: always map to AvailabilityListings
			if ( !string.IsNullOrWhiteSpace( input.AvailabilityListing ) )
				output.AvailabilityListings = new List<string>() { input.AvailabilityListing };
			else
				output.AvailabilityListings = null;

			//if ( !string.IsNullOrWhiteSpace( input.AvailabilityListing ) )
			//	output.AvailabilityListing = input.AvailabilityListing;
			//else if ( from.AvailabilityListings?.Count > 0 )
			//	output.AvailabilityListing = from.AvailabilityListings[0];
			
			output.FoundingDate = input.FoundingDate;

			output.MissionAndGoalsStatement = input.MissionAndGoalsStatement;
			output.MissionAndGoalsStatementDescription = input.MissionAndGoalsStatementDescription;
			output.Supersedes = input.Supersedes;
			output.SupersededBy = input.SupersededBy;
			output.TransferValueStatement = input.TransferValueStatement;
			output.TransferValueStatementDescription = input.TransferValueStatementDescription;

			//
			saveDuration = DateTime.Now.Subtract( started );
			//if ( saveDuration.TotalSeconds > 1 )
			LoggingHelper.DoTrace( 7, string.Format( "         Map Duration: {0:N2} seconds after basic mapping", saveDuration.TotalSeconds ) );
			started = DateTime.Now;

			//TODO: remove this one, or not
			output.AlternateNames = Entity_ReferenceManager.GetAll( output.RowId, CodesManager.PROPERTY_CATEGORY_ALTERNATE_NAME );
			output.AlternateName = output.AlternateNames.Select( m => m.TextValue ).ToList(); ;
			//
			//TODO
			//output.AlternativeIdentifier = input.AlternativeIdentifier;
			output.Identifier = Entity_IdentifierValueManager.GetAll( output.RowId, Entity_IdentifierValueManager.ORGANIZATION_Identifier );

			output.Keyword = Entity_ReferenceManager.GetAll( output.RowId, CodesManager.PROPERTY_CATEGORY_KEYWORD );
			saveDuration = DateTime.Now.Subtract( started );
			//if ( saveDuration.TotalSeconds > 1 )
			LoggingHelper.DoTrace( 7, string.Format( "         Map Duration: {0:N2} seconds after external 1", saveDuration.TotalSeconds ) );
			started = DateTime.Now;
			//properties
			if ( request.IncludingAddresses )
			{
				List<ContactPoint> orphans = new List<ContactPoint>();
				output.Addresses = Entity_AddressManager.GetAll( output.RowId, ref orphans );
				LoggingHelper.DoTrace( 7, thisClassName + string.Format( ".MapFromDB. OrgId: {0}, Addresses: {1}, orphanContacts: {2}", output.Id, output.Addresses.Count(), orphans.Count ) );
				//these will be mostly (all) under address
				//really should have display output show contact points per address
				//then how output handle CPs without address!
				//any contacts imported with an empty address, would have been added output the org
				output.ContactPoint = Entity_ContactPointManager.GetAll( output.RowId );
				LoggingHelper.DoTrace( 7, thisClassName + string.Format( ".MapFromDB. OrgId: {0}, ContactPoint: {1}", output.Id, output.ContactPoint.Count() ) );
				if ( output.Addresses != null && output.Addresses.Count > 0 )
				{

				}
				if ( orphans.Count > 0 )
				{
					output.ContactPoint.AddRange( orphans );
				}
				//detail page expects social media in contact points
				if ( output.ContactPoint == null )
					output.ContactPoint = new List<ContactPoint>();
				output.SocialMediaPages = Entity_ReferenceManager.GetAll( output.RowId, CodesManager.PROPERTY_CATEGORY_ORGANIZATION_SOCIAL_MEDIA );
				if ( output.SocialMediaPages != null && output.SocialMediaPages.Count > 0 )
				{
					//why is this being done?
					//skip for API
					if ( !request.IsForAPIRequest )
					{
						LoggingHelper.DoTrace( 7, thisClassName + string.Format( ".MapFromDB. OrgId: {0}, SocialMediaPages: {1}", output.Id, output.SocialMediaPages.Count() ) );
						ContactPoint cp = new ContactPoint();
						cp.SocialMedia.AddRange( output.SocialMediaPages );
						output.ContactPoint.Add( cp );
					}
				}
			}
			if ( request.IncludingProperties )
			{
				output.AgentType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_ORGANIZATION_TYPE );
				if ( output.AgentType.HasItems() )
				{
					EnumeratedItem item = output.AgentType.Items.FirstOrDefault( s => s.SchemaName == "orgType:QualityAssurance" );
					if ( item != null && item.Id > 0 )
						output.ISQAOrganization = true;
				}
				output.SameAs = Entity_ReferenceManager.GetAll( input.RowId, CodesManager.PROPERTY_CATEGORY_SAME_AS ); //  = 76;

				//this shouldn't be used anymore
				//output.PhoneNumbers = Entity_ReferenceManager.GetAll( output.RowId, CodesManager.PROPERTY_CATEGORY_PHONE_TYPE );
				//LoggingHelper.DoTrace( 7, thisClassName + string.Format( ".MapFromDB. OrgId: {0}, PhoneNumbers: {1}", output.Id, output.PhoneNumbers.Count() ) );
				output.Emails = Entity_ReferenceManager.GetAll( output.RowId, CodesManager.PROPERTY_CATEGORY_EMAIL_TYPE );
				LoggingHelper.DoTrace( 7, thisClassName + string.Format( ".MapFromDB. OrgId: {0}, Emails: {1}", output.Id, output.Emails.Count() ) );
				//not ideal?
				if ( output.Emails != null && output.Emails.Count > 0 )
				{
					ContactPoint cp = new ContactPoint();
					cp.Name = "Email";
					cp.Email.AddRange( output.Emails );
					output.ContactPoint.Add( cp );
				}
				//using Entity.Property in workIT, rather than Organization.Service
				//OrganizationServiceManager.FillOrganizationService( input, output );
				output.ServiceType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_ORG_SERVICE );

				//sector type? - as an enumeration, will be stored in properties
				output.AgentSectorType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_ORGANIZATION_SECTORTYPE );
				output.Jurisdiction = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( output.RowId );

				output.IdentificationCodes = Entity_ReferenceManager.GetAll( output.RowId, CodesManager.PROPERTY_CATEGORY_ORGANIZATION_IDENTIFIERS );


				//
				//output.Industry = Reference_FrameworkItemManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_NAICS );
				output.IndustryTypes = Reference_FrameworkItemManager.FillCredentialAlignmentObject( output.RowId, CodesManager.PROPERTY_CATEGORY_NAICS );

				//}
				//
				saveDuration = DateTime.Now.Subtract( started );
				//if ( saveDuration.TotalSeconds > 1 )
					LoggingHelper.DoTrace( 7, string.Format( "         Map Duration: {0:N2} seconds for properties mapping", saveDuration.TotalSeconds ) );
				started = DateTime.Now;
			}

			if ( request.IncludingEntityCounts )
			{
				//can this be sped up by getting all and then counting?
				output.TotalCredentials = Entity_AgentRelationshipManager.CredentialCount_ForOwningOfferingOrg( output.RowId );
				output.TotalAssessments = Entity_AgentRelationshipManager.AssessmentCount_ForOwningOfferingOrg( output.RowId );
				output.TotalLopps = Entity_AgentRelationshipManager.LoppCount_ForOwningOfferingOrg( output.RowId );
				output.TotalFrameworks = CompetencyFrameworkManager.FrameworkCount_ForOwningOrg( output.CTID );

				output.TotalCollections = CollectionManager.Count_ForOwningOrg( output.RowId );

				output.TotalTransferValueProfiles = TransferValueProfileManager.Count_ForOwningOrg( output.RowId );
				output.TotalPathways = PathwayManager.Count_ForOwningOrg( output.RowId );
				output.TotalPathwaySets = PathwaySetManager.Count_ForOwningOrg( output.RowId );
				output.TotalConceptSchemes = ConceptSchemeManager.CountForOwningOrg( output.Id );
				//to get all dataset profiles, should be able to use dsp.DataProvider as having credentials in dsp is not the same as publishing
				//	although in the proPath example, the dsp is with the cred/ADP, so ????
				//var dsp = DataSetProfileManager.GetAllDataSetCredentials( output.Id, 500 );
				//if ( dsp != null )
				//	output.TotalDataSetProfiles = dsp.Count;
				output.DataSetProfileCount = DataSetProfileManager.GetCountForDataOwner( output.RowId );


				//TODO - would be better to do in one call and split on return
				//21-08-17 mp	- combining now as the new interface combines them
				//				- this method will only find the credentials where the relationship was published with the org, not where there is a Renews condition profile with an asserted by.
				//				- actually EAR is from the perspecive of the credential. Would need to use Entity.Assertion
				output.RevokesCredentials = Entity_AgentRelationshipManager.CredentialCount_ForRevokedByOrg( output.RowId );
				output.RenewsCredentials = Entity_AgentRelationshipManager.CredentialCount_ForRenewedByOrg( output.RowId );
				//Regulates is part of QA
				//output.RegulatesCredentials = Entity_AgentRelationshipManager.CredentialCount_ForRegulatedByOrg( output.RowId );

				//TODO - only do this if a third party publisher - so how to determine this?
				//21-07-15 mp - updated these next properties to use the more general method (EntityCount_ForPublishedByOrg) which uses Entity.Cache - monitor. Could also do credential and compare numbers. Might be a way to warn for an issue
				output.TotalCredentialsPublishedByThirdParty = Entity_AgentRelationshipManager.EntityCount_ForPublishedByOrg( output.RowId, 1 );
				var oldTotalCredentialsPublishedByThirdParty = Entity_AgentRelationshipManager.CredentialCount_ForPublishedByOrg( output.RowId );
				if ( output.TotalCredentialsPublishedByThirdParty != oldTotalCredentialsPublishedByThirdParty )
				{
					LoggingHelper.LogError( "Organization.Get", "TotalCredentialsPublishedByThirdParty", "Conflicting totals returned for total credentials published by third party.", string.Format( "OrgId: {0}, New generic method using Entity.Cache: {1}, old specific method: {2}", output.Id, output.TotalCredentialsPublishedByThirdParty, oldTotalCredentialsPublishedByThirdParty ) );
					//more than likely the old method is accurate and cache needs updating
					if ( oldTotalCredentialsPublishedByThirdParty > output.TotalCredentialsPublishedByThirdParty )
						output.TotalCredentialsPublishedByThirdParty = oldTotalCredentialsPublishedByThirdParty;
				}
				output.TotalOrganizationsPublishedByThirdParty = Entity_AgentRelationshipManager.EntityCount_ForPublishedByOrg( output.RowId, 2 );
				output.TotalLoppsPublishedByThirdParty = Entity_AgentRelationshipManager.EntityCount_ForPublishedByOrg( output.RowId, 7 );
				var oldTotalLoppPublishedByThirdParty = Entity_AgentRelationshipManager.LearningOppCount_ForPublishedByOrg( output.RowId );
				if ( output.TotalLoppsPublishedByThirdParty != oldTotalLoppPublishedByThirdParty )
				{
				}
				output.TotalAssessmentsPublishedByThirdParty = Entity_AgentRelationshipManager.EntityCount_ForPublishedByOrg( output.RowId, 3 );
				//-FrameworkCount_ForOwningOrg
				if ( output.TotalCredentials > 0 )
					output.IsACredentialingOrg = true;

				//
				saveDuration = DateTime.Now.Subtract( started );
				//if ( saveDuration.TotalSeconds > 1 )
					LoggingHelper.DoTrace( 7, string.Format( "         WARNING Map Duration: {0:N2} seconds for credential and counts related mapping", saveDuration.TotalSeconds ) );
				started = DateTime.Now;
			}
            else
            {
                //need output distinguish QA input non-QA credentials
                //if ( CountCredentials( input ) > 0 )
                //	output.IsACredentialingOrg = true;
            }
			
			if ( request.IncludingRolesAndActions )
			{
				//this can be costly
				GetOrgRoles( output );
				//
				saveDuration = DateTime.Now.Subtract( started );
				//if ( saveDuration.TotalSeconds > 1 )
					LoggingHelper.DoTrace( 7, string.Format( "         WARNING Map Duration: {0:N2} seconds for getting roles", saveDuration.TotalSeconds ) );
				started = DateTime.Now;
			}

			if ( request.IncludingQAWhereUsed )
			{
				//output.QualityAssuranceActor = Entity_QualityAssuranceActionManager.QualityAssuranceActionProfile_GetAllForAgent( output.RowId );
			}

			if ( request.IncludingManifests )
			{
				output.HasConditionManifest = ConditionManifestManager.GetAll( output.Id, false );
				output.HasCostManifest = CostManifestManager.GetAll( output.Id, false );
				//
				saveDuration = DateTime.Now.Subtract( started );
				//if ( saveDuration.TotalSeconds > 3 )
					LoggingHelper.DoTrace( 7, string.Format( "         WARNING Map Duration: {0:N2} seconds after manifests", saveDuration.TotalSeconds ) );
				started = DateTime.Now;
			} else
			{
				//counts
				output.HasConditionManifest = ConditionManifestManager.GetAll( output.Id, true );
				output.HasCostManifest = CostManifestManager.GetAll( output.Id, true );
			}
			//or do in here
			MapProcessProfiles( input, output, request.IncludingProcessProfiles );
			//
			saveDuration = DateTime.Now.Subtract( started );
			//if ( saveDuration.TotalSeconds > 3 )
			LoggingHelper.DoTrace( 7, string.Format( "         WARNING Map Duration: {0:N2} seconds for process profiles", saveDuration.TotalSeconds ) );
			started = DateTime.Now;

			//
			if ( request.IncludingVerificationProfiles )
			{
				//need output distiguish between edit, list, and detail
				output.VerificationServiceProfiles = Entity_VerificationProfileManager.GetAll( output.RowId );
				//
				saveDuration = DateTime.Now.Subtract( started );
				//if ( saveDuration.TotalSeconds > 3 )
				LoggingHelper.DoTrace( 7, string.Format( "         WARNING Map Duration: {0:N2} seconds for VerificationServiceProfiles mapping", saveDuration.TotalSeconds ) );
			}
			//else
			{
				output.VerificationServiceProfileCount= Entity_VerificationProfileManager.GetAllTotal( output.RowId );
			}
			//check for owned dataset profiles - realistically need a search or make a postback
			//includingParts = false,
			//isAPIRequest use? - for this context, do we want the data?
			//					- there is no search for dsp. again could use a postback for each	
			if ( request.IncludingDataSetProfiles )
			{
				output.DataSetProfiles = DataSetProfileManager.GetAllForDataOwner( output.RowId, false, request.IsForAPIRequest );
			} else
            {
				//see counts up above
				//output.DataSetProfileCount = DataSetProfileManager.GetCountForDataOwner( output.RowId );
			}
			//
			saveDuration = DateTime.Now.Subtract( overall );
			//if ( saveDuration.TotalSeconds > 3 )
			LoggingHelper.DoTrace( 7, string.Format( "         WARNING Map Duration: {0:N2} seconds for all mapping", saveDuration.TotalSeconds ) );
		}
		public static string MapEntityType( int entityTypeId )
		{
			var entityType = "";
			switch ( entityTypeId )
			{
				case 2:
					return "CredentialOrganization";
				case 13:
					return "QACredentialOrganization";
				case 14:
					return "Organization";

			}
			return entityType;
		}
		public static string MapEntityTypeLabel( int entityTypeId )
		{
			var entityType = "";
			switch ( entityTypeId )
			{
				case 2:
					return "Credential Organization";
				case 13:
					return "Quality Assurance Organization";
				case 14:
					return "Organization";
			}
			return entityType;
		}
		public static int MapEntityTypeId( string ctdlType)
		{
			var entityTypeId = 2;
			switch ( ctdlType )
			{
				case "ceterms:CredentialOrganization":
					return CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION;
				case "ceterms:QACredentialOrganization":
					return CodesManager.ENTITY_TYPE_QAORGANIZATION;
				case "ceterms:Organization":
					return CodesManager.ENTITY_TYPE_PLAIN_ORGANIZATION;

			}
			return entityTypeId;
		}
		private static void GetOrgRoles( ThisEntity output )
		{

			//the parent is the entity, and the 
			//to.OrganizationRole_Recipient = Entity_AgentRelationshipManager.AgentEntityRole_GetAll_ToEnumeration( to.RowId, true );
			//Use a combined view
			//this may be costly, so move OrganizationRole_Recipient out!!!!!!!
			var started = DateTime.Now;
			//this will include QA received
			//output.OrganizationRole_Recipient = Entity_AssertionManager.GetAllCombinedForTarget( 2, output.Id, output.Id );
			//var saveDuration = DateTime.Now.Subtract( started );
			//LoggingHelper.DoTrace( 7, string.Format( "         NOTE Map Duration: {0:N2} seconds for Entity_AssertionManager.GetAllCombinedForTarget mapping", saveDuration.TotalSeconds ) );
			////save to compare
			//var oldMethod = output.OrganizationRole_Recipient;

			//21-07-12 proposed
			started = DateTime.Now;
			output.OrganizationRole_Recipient = new OrganizationRoleManager().GetAllCombinedForTarget( 2, output.Id, output.Id );
			/*
			output.OrganizationRole_Recipient = Entity_AgentRelationshipManager.GetAllThirdPartyAssertionsForEntity( 2, output.RowId, output.Id, true );
			var orgFirstPartyAssertions = Entity_AssertionManager.GetAllFirstPartyAssertionsForTarget( 2, output.RowId, output.Id, true );
			if ( orgFirstPartyAssertions != null && orgFirstPartyAssertions.Any() )
				output.OrganizationRole_Recipient.AddRange( orgFirstPartyAssertions );


			*/
			var saveDuration = DateTime.Now.Subtract( started );
			LoggingHelper.DoTrace( 7, string.Format( "         NOTE Map Duration: {0:N2} seconds for NEW approach for mapping roles. ", saveDuration.TotalSeconds ) );
			//
			//also want the inverses - where this org was providing the QA for asmts, etc. 
			//18-10-08 this is used to display QA performed. 
			//to.OrganizationRole_Actor = Entity_AgentRelationshipManager.GetAll_QATargets_ForAgent( to.RowId );



			int totalRecords = 0;
			//need to keep this call until detail page is changed to only use ActualActorRoleCount
			//to.OrganizationRole_Actor = Entity_AssertionManager.GetAllCombinedForOrganization( to.RowId, ref totalRecords, 10 );
			//if ( totalRecords > to.OrganizationRole_Actor.Count() )
			//	to.ActualActorRoleCount = totalRecords;
			//else
			//	to.ActualActorRoleCount = to.OrganizationRole_Actor.Count();

			//eventually will just use this to get totals
			Entity_AssertionManager.FillCountsForOrganizationQAPerformed( output, ref totalRecords );
			output.ActualActorRoleCount = output.QAPerformedOnCredentialsCount + output.QAPerformedOnOrganizationsCount + output.QAPerformedOnAssessmentsCount + output.QAPerformedOnLoppsCount;

			//We need to merge OrganizationRole_Actor with OrganizationAssertions
			//to.OrganizationAssertions = Entity_AssertionManager.GetAll( to.RowId );
			//foreach (var item in to.OrganizationAssertions)
			//{
			//    if (to.OrganizationRole_Actor.Exists(s => s.TargetCredential.Id == item.TargetCredential.Id ))
			//    {
			//        //check roles
			//    }
			//}

			output.JurisdictionAssertions = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( output.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_OFFERREDIN );

			//to.QualityAssuranceAction = Entity_QualityAssuranceActionManager.QualityAssuranceActionProfile_GetAll( to.RowId );

			//dept and subsiduaries????
			//21-03-12 mparsons - reactivating this for API
			//21-03-20 mparsons - saw WDI in prod that resulted in child orgs appearing twice - both sides of relationship (HasParent and is parent of
			//21-04-23 mparsons - moved code from this to AgentRole_FillAllChildOrganizations, so removing
			//Entity_AgentRelationshipManager.AgentRole_FillAllSubOrganizations( to, 0 );
			//will need to exclude the latter from these in the API
			//		- OrganizationRole_Recipient now contains the suborgs!, the child orgs will get duplicated
			//		- first gets where this org is the acting agent on target with 20,21
			//		** this is slow due to the use of the view: Entity_Relationship_AgentSummary - need to replace
			Entity_AgentRelationshipManager.AgentRole_FillAllChildOrganizations( output );

			//parent org 
			Entity_AgentRelationshipManager.AgentRole_GetParentOrganization( output );

		}
		private static void MapProcessProfiles( DBEntity from, ThisEntity to, bool includingProcessProfileDetails )
		{
			//get all and then split
			if ( includingProcessProfileDetails )
			{
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
			else
			{
				//this would not make sense if trying to avoid the hit
				//actually want counts. Is it one count of one for each type
				to.ProcessProfilesSummary = Entity_ProcessProfileManager.GetAllSummary( to.RowId );
			}
		}

		public static void MapToDB( ThisEntity input, DBEntity output )
		{

			//want output ensure fields input create are not wiped
			if ( output.Id == 0 )
			{
				//need output ensure ctid and registry id are not overridden, if can update outside of the import
				output.CTID = input.CTID;
			}
			if ( !string.IsNullOrWhiteSpace( input.CredentialRegistryId ) )
				output.CredentialRegistryId = input.CredentialRegistryId;

			output.Id = input.Id;
			output.Name = GetData( input.Name );
			output.EntityTypeId = input.EntityTypeId > 0 ? input.EntityTypeId : 2;

			output.Description = GetData( input.Description );
			output.LifeCycleStatusTypeId = input.LifeCycleStatusTypeId;
			output.AgentPurposeDescription = GetData( input.AgentPurposeDescription );
			output.AgentPurpose = GetUrlData( input.AgentPurpose, null );

			output.SubjectWebpage = GetUrlData( input.SubjectWebpage, null );
			if ( input.AgentDomainType == "ceterms:QACredentialOrganization" )
				output.ISQAOrganization = true;
			else if ( input.AgentDomainTypeId == 2 )
				output.ISQAOrganization = true;
			//TODO  - this is to be a list now (some time ago)
			//prepare for being JSON, by just storing
			if ( !string.IsNullOrWhiteSpace( input.AvailabilityListing ))
				output.AvailabilityListing = input.AvailabilityListing;
			else 
				output.AvailabilityListing = null;

			output.ImageURL = GetUrlData( input.Image, null );


			//FoundingDate is now a string
			//interface must handle? Or do we have output fix here?
			//depends if just text is passed or separates
			//already validated
			if ( !string.IsNullOrWhiteSpace( input.FoundingDate ) )
			{
				output.FoundingDate = input.FoundingDate;
				if ( ( output.FoundingDate ?? "" ).Length > 20 )
				{
					//status.AddError( "Organization Founding Date error - exceeds 20 characters: " + output.FoundingDate );
					output.FoundingDate = output.FoundingDate.Substring( 0, 10 );
				}
			}
			else
				output.FoundingDate = null;

			output.MissionAndGoalsStatement = GetUrlData( input.MissionAndGoalsStatement, null );
			output.MissionAndGoalsStatementDescription = GetData( input.MissionAndGoalsStatementDescription );
			output.Supersedes = GetUrlData( input.Supersedes, null );
			output.SupersededBy = GetUrlData( input.SupersededBy, null );
			output.TransferValueStatement = GetUrlData( input.TransferValueStatement, null );
			output.TransferValueStatementDescription = GetData( input.TransferValueStatementDescription );
			//output.ServiceTypeOther = input.ServiceTypeOther;

		}
		#endregion
		public class OrganizationRequest
		{
			public OrganizationRequest( int requestTypeId = 1 )
			{
				switch ( requestTypeId )
				{
					case 1:
						IsDetailRequest();
						break;
					case 2:
						IsAPIRequest();
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
				//IncludingJurisdiction = true;
				IncludingManifests = true;
				IncludingOrgParentChild = true;
				IncludingProcessProfiles = true;
				IncludingProperties = true;

				IncludingRolesAndActions = true;
				IncludingVerificationProfiles = true;
			}
			public void IsAPIRequest()
			{
				//TBD: if API will always want to count the excluded profiles. May want an option with no counts to assess performance
				IsForAPIRequest = true;
				//AllowCaching = true;

				IncludingAddresses = true;
				//IncludingJurisdiction = true;
				IncludingProperties = true;
				IncludingRolesAndActions = true;
				
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
			public bool IncludingDataSetProfiles { get; set; }
			public bool IncludingEntityCounts { get; set; } = true;
			//leaving with properties for now
			//public bool IncludingJurisdiction { get; set; }
			public bool IncludingManifests { get; set; }
			public bool IncludingOrgParentChild { get; set; }
			public bool IncludingProcessProfiles { get; set; }
			public bool IncludingProperties { get; set; }
			public bool IncludingQAWhereUsed { get; set; }
			public bool IncludingRolesAndActions { get; set; }
			public bool IncludingVerificationProfiles { get; set; }
		}
	}

	

}

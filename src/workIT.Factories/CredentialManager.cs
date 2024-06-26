using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;

using workIT.Models;
using workIT.Models.Common;
using workIT.Utilities;
using CM = workIT.Models.Common;
using DBResource = workIT.Data.Tables.Credential;
using EM = workIT.Data.Tables;
using EntityContext = workIT.Data.Tables.workITEntities;
using MPM = workIT.Models.ProfileModels;
using ReferenceFrameworkItemsManager = workIT.Factories.Reference_FrameworkItemManager;
using ThisResource = workIT.Models.Common.Credential;
using ViewContext = workIT.Data.Views.workITViews;
using Views = workIT.Data.Views;

namespace workIT.Factories
{
    public class CredentialManager : BaseFactory
	{
		static string thisClassName = "Factories.CredentialManager";
        static string EntityType = "Credential";
        static int EntityTypeId = CodesManager.ENTITY_TYPE_CREDENTIAL;
        static string Entity_Label = "Credential";

        EntityManager entityMgr = new EntityManager();
		#region Credential - presistance =======================

		/// <summary>
		/// Save a credential - only from import
		/// </summary>
		/// <param name="resource"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public bool Save( ThisResource resource, ref SaveStatus status )
		{
			LoggingHelper.DoTrace( 7, string.Format( "CredentialManager.Save Entered. Name: {0}, CTID: {1}", resource.Name, resource.CTID ) );
			bool isValid = true;
			int count = 0;
			DateTime lastUpdated = System.DateTime.Now;
			//NOTE - need to properly set entity.EntityStateId

			try
			{

				//note for import, may still do updates?
				if ( ValidateProfile( resource, ref status ) == false )
				{//always want to complete import may want to log errors though
				 //return false;
				}
				//getting duplicates somehow
				//second one seems less full featured, so could compare dates
				if ( resource.Id > 0 )
				{
					bool doingUpdateParts = true;

					using ( var context = new EntityContext() )
					{
						DBResource efEntity = context.Credential
								.FirstOrDefault( s => s.Id == resource.Id );
						if ( efEntity != null && efEntity.Id > 0 )
						{

							//for updates, chances are some fields will not be in interface, don't map these (ie created stuff)
							//**ensure rowId is passed down for use by profiles, etc
							resource.RowId = efEntity.RowId;

							MapToDB( resource, efEntity );
							//assume and validate, that if we get here we have a full record
							//not clear if we will want to update a base reference. 
							//==> should happen automatically if full record matches a SWP?
							//may be iffy
							//19-05-21 mp - should add a check for an update where currently is deleted
							if ( ( efEntity.EntityStateId ?? 0 ) == 0 )
							{
								SiteActivity sa = new SiteActivity()
								{
									ActivityType = "Credential",
									Activity = "Import",
									Event = "Reactivate",
									Comment = string.Format( "Credential had been marked as deleted, and was reactivated by the import. Name: {0}, SWP: {1}", resource.Name, resource.SubjectWebpage ),
									ActivityObjectCTID = efEntity.CTID,
									ActivityObjectId = resource.Id
								};
								new ActivityManager().SiteActivityAdd( sa );
							}

                            if ( ( efEntity.EntityStateId ?? 1 ) != 2 )
                                efEntity.EntityStateId = 3;

                            resource.EntityStateId = ( int ) efEntity.EntityStateId;

                            //need to do the date check here, or may not be updated
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
									//should this always be updated?
									//entity.LastUpdated = (DateTime) efEntity.LastUpdated;
									//UpdateEntityCache( entity, ref status );
								}
								else
								{
									//?no info on error
									status.AddError( string.Format( "Error - the update was not successful for credential: {0}, Id: {1}. But no reason is present.", resource.Name, resource.Id ) );
									isValid = false;
									string message = string.Format( thisClassName + ". Save Failed", "Attempted to update a credential. The process appeared to not work, but was not an exception, so we have no message, or no clue. credential: {0}, Id: {1}", resource.Name, resource.Id );
									EmailManager.NotifyAdmin( thisClassName + ". Save Failed", message );
								}
							}
							
							if ( resource.OrganizationId == 0 && resource.PrimaryOrganization != null )
								resource.OrganizationId = resource.PrimaryOrganization.Id;
							//FLAW - we want the entity_cache to reflect last update of any part of a resource
							//		- technically the cache date could tell us that - if it could not be updated when refreshing the whole cache!!
							resource.LastUpdated = lastUpdated;
							UpdateEntityCache( resource, ref status );
						}
						else
						{
							status.AddError( string.Format( "Error - Save/Import failed, as record was not found. CredId: {0}", resource.Id ) );
							return false;
						}//
					}//end context

					//21-04-21 mparsons - end the current context before doing parts
					//continue with parts only if valid 
					//21-04-22 mparsons - actually will always update parts, just in case
					bool partsUpdateIsValid = true;
					if ( isValid || doingUpdateParts )
					{
						if ( !UpdateParts( resource, false, ref status ) )
							partsUpdateIsValid = false;

						SiteActivity sa = new SiteActivity()
						{
							ActivityType = "Credential",
							Activity = "Import",
							Event = "Update",
							Comment = $"Credential was updated by the import. Name: {resource.Name}, SWP: {resource.SubjectWebpage}, CTID: {resource.CTID}",
							ActivityObjectCTID = resource.CTID,
							ActivityObjectId = resource.Id
						};
						new ActivityManager().SiteActivityAdd( sa );
					}

					//this should use the Local date from status
					//if setting Entity.LastUpdated to the registry date, then should remove triggers to update the latter!
					//	???????????????????
					if ( isValid || partsUpdateIsValid )
						new EntityManager().UpdateModifiedDate( resource.RowId, ref status, lastUpdated );

				}
				else
				{
					int newId = Add( resource, ref status );
					if ( newId == 0 || status.HasErrors )
						isValid = false;
				}

			}
			catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
			{
				string message = HandleDBValidationError( dbex, thisClassName + string.Format( ".Save. id: {0}, Name: {1}", resource.Id, resource.Name ), "Credential" );
				LoggingHelper.LogError( dbex, thisClassName, string.Format( "Save for id: {0}, Name: {1}", resource.Id, resource.Name ) );

				status.AddError( thisClassName + ".Save(). Error - the save was not successful. DbEntityValidationException. " + message );
				isValid = false;
			}
			catch ( Exception ex )
			{
				string message = FormatExceptions( ex );
				LoggingHelper.LogError( ex, thisClassName, string.Format( "Save for id: {0}, Name: {1}", resource.Id, resource.Name ) );
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
		private int Add( ThisResource entity, ref SaveStatus status )
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

					//efEntity.Created = System.DateTime.Now;
					//efEntity.LastUpdated = System.DateTime.Now;
					if ( IsValidDate( status.EnvelopeCreatedDate ) )
						efEntity.Created = status.LocalCreatedDate;
					else
						efEntity.Created = DateTime.Now;
					//
					if ( IsValidDate( status.EnvelopeUpdatedDate ) )
						efEntity.LastUpdated = status.LocalUpdatedDate;
					else
						efEntity.LastUpdated = DateTime.Now;

					efEntity.EntityStateId = entity.EntityStateId = 3;
					context.Credential.Add( efEntity );

					// submit the change to database
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						status.UpdateElasticIndex = true;
						//
						entity.RowId = efEntity.RowId;
						entity.Created = ( DateTime )efEntity.Created;
						entity.LastUpdated = ( DateTime )efEntity.LastUpdated;
						entity.Id = efEntity.Id;
						if ( entity.OrganizationId == 0 && entity.PrimaryOrganization != null )
							entity.OrganizationId = entity.PrimaryOrganization.Id;
						UpdateEntityCache( entity, ref status );
						//add log entry
						SiteActivity sa = new SiteActivity()
						{
							ActivityType = "Credential",
							Activity = "Import",
							Event = "Add",
							Comment = $"Full Credential was added by the import. Name: {entity.Name}, SWP: {entity.SubjectWebpage}, CTID: {entity.CTID}",
							ActivityObjectCTID = entity.CTID,
							ActivityObjectId = entity.Id
						};
						new ActivityManager().SiteActivityAdd( sa );

						UpdateParts( entity, true, ref status );
						new EntityManager().UpdateModifiedDate( entity.RowId, ref status, efEntity.LastUpdated );
						return efEntity.Id;
					}
					else
					{
						//?no info on error
						status.AddError( thisClassName + "Error - the add was not successful. " );
						string message = string.Format( "CredentialManager. Add Failed", "Attempted to add a credential. The process appeared to not work, but was not an exception, so we have no message, or no clue.Credential: {0}, SubjectWebpage: {1}", entity.Name, entity.SubjectWebpage );
						//EmailManager.NotifyAdmin( "CredentialManager. Credential_Add Failed", message );
					}
				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					//noticed some duplicate credentials have been created. Have the same ctid, and created dates
					string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", "Credential" );

					status.AddError( thisClassName + string.Format( ".Add(). Error - the add was not successful. DbEntityValidationException. ", entity.Name, entity.PrimaryAgentUID ) + message );
					LoggingHelper.LogError( dbex, thisClassName + string.Format( ".Add(), Name: {0}, CTID: {1}, OwningAgentUid: {2}", efEntity.Name, efEntity.CTID, efEntity.OwningAgentUid ) );
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Add(), Name: {0}, CTID: {1}, OwningAgentUid: {2}", efEntity.Name, efEntity.CTID, efEntity.OwningAgentUid ) );
					status.AddError( FormatExceptions( ex ) );
				}
			}

			return efEntity.Id;
		}

		/// <summary>
		/// Add a base reference for the first time a document was referenced
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public int AddBaseReference( ThisResource entity, ref SaveStatus status )
		{
			DBResource efEntity = new DBResource();
			try
			{
				using ( var context = new EntityContext() )
				{
					if ( entity == null ||
						( string.IsNullOrWhiteSpace( entity.Name ) ||
						string.IsNullOrWhiteSpace( entity.SubjectWebpage )
						) )
					{
						status.AddError( thisClassName + ". AddBaseReference() The credential is incomplete" );
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
					CodeItem ci = CodesManager.Codes_PropertyValue_GetBySchema( CodesManager.PROPERTY_CATEGORY_CREDENTIAL_TYPE, entity.CredentialTypeSchema );
					if ( ci == null || ci.Id < 1 )
					{
						status.AddError( string.Format( "A valid Credential Type must be included. Name: {0}, Invalid: Credential Type: {1}, ", entity.Name, entity.CredentialTypeSchema ) ); //adding anyway
					}
					else
						efEntity.CredentialTypeId = entity.CredentialTypeId = ci.Id;
					//set to active 
					var defStatus = CodesManager.Codes_PropertyValue_GetBySchema( CodesManager.PROPERTY_CATEGORY_CREDENTIAL_STATUS_TYPE, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_STATUS_TYPE_ACTIVE );
					efEntity.CredentialStatusTypeId = defStatus.Id;

					if ( IsValidGuid( entity.RowId ) )
						efEntity.RowId = entity.RowId;
					else
						efEntity.RowId = Guid.NewGuid();
					//set to return, just in case
					entity.RowId = efEntity.RowId;
					efEntity.Created =  System.DateTime.Now;
					efEntity.LastUpdated = System.DateTime.Now;

					context.Credential.Add( efEntity );
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						SiteActivity sa = new SiteActivity()
						{
							ActivityType = "Credential",
							Activity = "Import",
							Event = "Add Base Reference",
							Comment = string.Format( "Pending Credential was added by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ),
							ActivityObjectId = efEntity.Id
						};
						new ActivityManager().SiteActivityAdd( sa );

						entity.Id = efEntity.Id;
						entity.RowId = efEntity.RowId;
						entity.Created = ( DateTime )efEntity.Created;
						entity.LastUpdated = ( DateTime )efEntity.LastUpdated;
						UpdateEntityCache( entity, ref status );
						UpdateParts( entity, true, ref status );
						return efEntity.Id;
					}

					status.AddError( thisClassName + ". AddBaseReference() Error - the save was not successful, but no message provided. " );
				}
			}
			catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
			{
				//noticed some duplicate credentials have been created. Have the same ctid, and created dates
				string message = HandleDBValidationError( dbex, thisClassName + ".AddBaseReference() ", "Credential" );

				status.AddError( thisClassName + string.Format( ".Add(). Error - the add was not successful. DbEntityValidationException. ", entity.Name, entity.PrimaryAgentUID ) + message );
				LoggingHelper.LogError( dbex, thisClassName + string.Format( ".AddBaseReference(), Name: {0}, CTID: {1}, OwningAgentUid: {2}", efEntity.Name, efEntity.CTID, efEntity.OwningAgentUid ) );
			}
			catch ( Exception ex )
			{
				string message = FormatExceptions( ex );
				LoggingHelper.LogError( ex, thisClassName + string.Format( ".AddBaseReference. Name:  {0}, SubjectWebpage: {1}", entity.Name, entity.SubjectWebpage ) );
				status.AddError( thisClassName + ". AddBaseReference()  Error - the save was not successful. " + message );

			}
			return 0;
		}
		public int AddPendingRecord( Guid entityUid, string ctid, string registryAtId, ref string statusMessage )
		{
			DBResource efEntity = new DBResource();
			try
			{
				using ( var context = new EntityContext() )
				{
					if ( !IsValidGuid( entityUid ) )
					{
						statusMessage = thisClassName + " - A valid GUID must be provided to create a pending entity";
						return 0;
					}
					//quick check to ensure not existing
					ThisResource entity = GetMinimumByCtid( ctid );
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
                    //set to active 
                    var defStatus = CodesManager.Codes_PropertyValue_GetBySchema( CodesManager.PROPERTY_CATEGORY_CREDENTIAL_STATUS_TYPE, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_STATUS_TYPE_ACTIVE );
                    efEntity.CredentialStatusTypeId = defStatus.Id;
                    efEntity.Created = System.DateTime.Now;
					efEntity.LastUpdated = System.DateTime.Now;

					context.Credential.Add( efEntity );
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						SiteActivity sa = new SiteActivity()
						{
							ActivityType = "Credential",
							Activity = "Import",
							Event = "Add Pending Credential",
							Comment = string.Format( "Pending Credential was added by the import. ctid: {0}, registryAtId: {1}", ctid, registryAtId ),
							ActivityObjectCTID = efEntity.CTID,
							ActivityObjectId = efEntity.Id
						};
						new ActivityManager().SiteActivityAdd( sa );

						//Question should this be in the EntityCache?
						SaveStatus status = new SaveStatus();
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

					statusMessage = thisClassName + " AddPendingRecord(). Error - the save was not successful, but no message provided. ";
				}
			}

			catch ( Exception ex )
			{
				string message = FormatExceptions( ex );
				LoggingHelper.LogError( ex, thisClassName + string.Format( ".AddPendingRecord. entityUid:  {0}, ctid: {1}", entityUid, ctid ) );
				statusMessage = thisClassName + "  AddPendingRecord(). Error - the save was not successful. " + message;

			}
			return 0;
		}

		public void UpdateEntityCache( ThisResource document, ref SaveStatus status )
		{
			EntityCache ec = new EntityCache()
			{
				EntityTypeId = 1,
				EntityType = "credential",
				EntityStateId = document.EntityStateId,
				EntityUid = document.RowId,
				BaseId = document.Id,
				Description = document.Description,
				SubjectWebpage = document.SubjectWebpage,
				CTID = document.CTID,
				Created = document.Created,
				LastUpdated = document.LastUpdated,
				ImageUrl = document.Image,
				Name = document.Name,
				OwningAgentUID = document.PrimaryAgentUID,
				OwningOrgId = document.OrganizationId,
				PublishedByOrganizationId = document.PublishedByThirdPartyOrganizationId
			};
            //check non-active or just deprecated
            //var defStatus = CodesManager.Codes_PropertyValue_GetBySchema( CodesManager.PROPERTY_CATEGORY_CREDENTIAL_STATUS_TYPE, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_STATUS_TYPE_ACTIVE );
            var deprecatedStatus = CodesManager.Codes_PropertyValue_GetBySchema( CodesManager.PROPERTY_CATEGORY_CREDENTIAL_STATUS_TYPE, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_STATUS_TYPE_DEPRECATED );
            if ( document.CredentialStatusTypeId > 0 && document.CredentialStatusTypeId == deprecatedStatus.Id )
            {
                ec.IsActive = false;
            }
            var statusMessage = string.Empty;
			if (new EntityManager().EntityCacheSave( ec, ref statusMessage ) == 0)
			{
				status.AddError( thisClassName + string.Format(".UpdateEntityCache for '{0}' ({1}) failed: {2}", document.Name, document.Id, statusMessage) );
			}
		}

		public int UpdateBaseReferenceCredentialType( ThisResource entity, ref SaveStatus status )
		{
			DBResource efEntity = new DBResource();
			try
			{
				using ( var context = new EntityContext() )
				{
					if ( entity.Id == 0 )
					{
						//error
						status.AddError( string.Format( "Invalid request to update a Reference Credential: No Id was provided. Name: {0}, SWP: {1}", !string.IsNullOrWhiteSpace( entity.Name ) ? entity.Name : "Missing Name", !string.IsNullOrWhiteSpace( entity.SubjectWebpage ) ? entity.SubjectWebpage : "Missing" ) );
						return 0;
					}
					efEntity = context.Credential
								.SingleOrDefault( s => s.Id == entity.Id );
					if ( efEntity.Id == 0 )
					{
						//error
						status.AddError( string.Format( "Invalid request to update a Reference Credential - NOT FOUND: Id: {0} Name: {1}, SWP: {2}", entity.Id, !string.IsNullOrWhiteSpace( entity.Name ) ? entity.Name : "Missing Name", !string.IsNullOrWhiteSpace( entity.SubjectWebpage ) ? entity.SubjectWebpage : "Missing" ) );
						return 0;
					}
					//make sure this is a reference
					if ( efEntity.EntityStateId != 2 )
					{
						status.AddError( string.Format( "Invalid request to update a Reference Credential, where existing credential is not a reference Type. Id: {0}, Name: {1}", efEntity.Id, efEntity.Name ) );
						return 0;
					}
					//entity was found with name and SWP, and may not have a description, so only update type
					//efEntity.Name = entity.Name;
					//efEntity.Description = entity.Description;
					//efEntity.SubjectWebpage = entity.SubjectWebpage;
					CodeItem ci = CodesManager.Codes_PropertyValue_GetBySchema( CodesManager.PROPERTY_CATEGORY_CREDENTIAL_TYPE, entity.CredentialTypeSchema );
					if ( ci == null || ci.Id < 1 )
					{
						status.AddError( string.Format( "UpdateBaseReferenceCredentialType: A valid Credential Type must be included. Name: {0}, Invalid: Credential Type: {1}, ", entity.Name, entity.CredentialTypeSchema ) );
						return 0;
					}
					else
						efEntity.CredentialTypeId = ci.Id;

					efEntity.LastUpdated = System.DateTime.Now;
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						//no activity for this?
						//SiteActivity sa = new SiteActivity()
						//{
						//    ActivityType = "Credential",
						//    Activity = "Import",
						//    Event = "Add Base Reference",
						//    Comment = string.Format( "Pending Credential was added by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ),
						//    ActivityObjectId = entity.Id
						//};
						//new ActivityManager().SiteActivityAdd( sa );

						entity.Id = efEntity.Id;
						return efEntity.Id;
					}

				}
			}

			catch ( Exception ex )
			{
				string message = FormatExceptions( ex );
				LoggingHelper.LogError( ex, thisClassName + string.Format( ".UpdateBaseReferenceCredentialType. Name:  {0}, SubjectWebpage: {1}", entity.Name, entity.SubjectWebpage ) );
				status.AddError( thisClassName + ". UpdateBaseReferenceCredentialType()  Error - the save was not successful. " + message );

			}
			return 0;
		}

	
		public bool UpdateJson( int credentialId, string json )
		{
			//SaveStatus status
			bool isValid = true;
			int count = 0;
			try
			{
				using ( var context = new EntityContext() )
				{
					DBResource efEntity = context.Credential
							.SingleOrDefault( s => s.Id == credentialId );
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
								LoggingHelper.LogError( string.Format( "Error - the Json update was not successful for credential: {0}, Id: {1}. But no reason is present.", efEntity.Name, efEntity.Id ) );
								isValid = false;
							}
						}
					}
					else
					{
						LoggingHelper.LogError( string.Format( "Error - UpdateJson failed, as record was not found. recordId: {0}", credentialId ) );
						isValid = false;
					}
				}

			}
			catch ( Exception ex )
			{
				string message = FormatExceptions( ex );
				LoggingHelper.LogError( thisClassName + ".UpdateJson(). Error - the save was not successful. " + message );
				isValid = false;
			}

			return isValid;
		}


		/// <summary>
		/// Update a credential
		/// - base only, caller will handle parts?
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		//	private bool Update( ThisResource entity, ref string statusMessage )
		//	{
		//		bool isValid = false;
		//		int count = 0;
		//		using ( var context = new EntityContext() )
		//		{
		//			try
		//			{
		//				//context.Configuration.LazyLoadingEnabled = false;

		//				EM.Credential efEntity = context.Credential
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

		public bool ValidateProfile( ThisResource profile, ref SaveStatus status )
		{

			status.HasSectionErrors = false;
			if ( string.IsNullOrWhiteSpace( profile.Name ) )
			{
				status.AddError( "A credential name must be included" );
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
			//
			var defStatus = CodesManager.Codes_PropertyValue_GetBySchema( CodesManager.PROPERTY_CATEGORY_CREDENTIAL_STATUS_TYPE, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_STATUS_TYPE_ACTIVE );
			if ( profile.CredentialStatusType == null || profile.CredentialStatusType.Items == null || profile.CredentialStatusType.Items.Count == 0 )
			{
				status.AddError( "A CredentialStatusType must be included" );
				//actual should default to active, likely a reference
				profile.CredentialStatusTypeId = defStatus.Id;
			}
			else
			{
				var schemaName = profile.CredentialStatusType.GetFirstItem().SchemaName;
				CodeItem ci = CodesManager.Codes_PropertyValue_GetBySchema( CodesManager.PROPERTY_CATEGORY_CREDENTIAL_STATUS_TYPE, schemaName );
				if ( ci == null || ci.Id < 1 )
				{
					//while this should never happen, should have a default: credentialStat:Active
					status.AddError( string.Format( "A valid CredentialStatusType must be included. Invalid: {0}", profile.CredentialTypeSchema ) );
					profile.CredentialStatusTypeId = defStatus.Id;
				}
				else
					profile.CredentialStatusTypeId = ci.Id;
			}

			//if ( string.IsNullOrWhiteSpace( profile.CTID ) )
			//{
			//	status.AddError( "A CTID name must be entered" );
			//}
			if ( !string.IsNullOrWhiteSpace( profile.DateEffective ) && !IsValidDate( profile.DateEffective ) )
			{
				status.AddWarning( "Effective date is invalid" );
			}


			return status.WasSectionValid;
		}


		public bool UpdateParts( ThisResource entity, bool isAdd, ref SaveStatus status )
		{
			bool isAllValid = true;

			Entity relatedEntity = EntityManager.GetEntity( entity.RowId );
			if ( relatedEntity == null || relatedEntity.Id == 0 )
			{
				status.AddError( "Error - the related Entity was not found." );
				return false;
			}

			//OrganizationRoleManager orgMgr = new OrganizationRoleManager();
			if ( AddProperties( entity, relatedEntity, ref status ) == false )
			{
				isAllValid = false;
			}

			if ( UpdateReferences( entity, relatedEntity, ref status ) == false )
			{
				isAllValid = false;
			}

			//Entity_HasResource
			var eHasResourcesMgr = new Entity_HasResourceManager();
			eHasResourcesMgr.DeleteAll( relatedEntity, ref status );
			// Transfer Value 
			if ( eHasResourcesMgr.SaveList( relatedEntity, CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, entity.ProvidesTVForIds, ref status, Entity_HasResourceManager.HAS_RESOURCE_TYPE_ProvidesTransferValueFor ) == false )
				isAllValid = false;
			if ( eHasResourcesMgr.SaveList( relatedEntity, CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, entity.ReceivesTVFromIds, ref status, Entity_HasResourceManager.HAS_RESOURCE_TYPE_ReceivesTransferValueFrom ) == false )
				isAllValid = false;
			//
			if ( eHasResourcesMgr.SaveList( relatedEntity, CodesManager.ENTITY_TYPE_RUBRIC, entity.HasRubricIds, ref status, Entity_HasResourceManager.HAS_RESOURCE_TYPE_HasResource ) == false )
				isAllValid = false;
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

			if ( entity.IsReferenceEntity == false )
			{
				AddProfiles( entity, relatedEntity, ref status );

				UpdateAssertedBys( entity, ref status );

				UpdateAssertedIns( entity, ref status );
				//outcomes
				HandleOutcomeProfiles( entity, relatedEntity, ref status );
                //
                var ehssMgr = new Entity_HasSupportServiceManager();
                ehssMgr.Update( entity.HasSupportServiceIds, relatedEntity, ref status );
            }

			return isAllValid;
		}
		public void AddProfiles( ThisResource resource, Entity relatedEntity, ref SaveStatus status )
		{


			try
			{
				//DurationProfile (do delete in SaveList)
				DurationProfileManager dpm = new Factories.DurationProfileManager();
				dpm.SaveList( resource.EstimatedDuration, resource.RowId, ref status );
				//rename this method!!!
				dpm.SaveRenewalFrequency( resource.RenewalFrequency, resource.RowId, ref status );
			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, thisClassName + ".UpdateParts(). Exception while processing duration. " + ex.Message );
				status.AddError( ex.Message );
			}
			//
			try
			{
				//Identifiers - do **delete for first one** and then assign
				//VersionIdentifier (do delete in SaveList)
				new Entity_IdentifierValueManager().SaveList( resource.VersionIdentifier, resource.RowId, Entity_IdentifierValueManager.IdentifierValue_VersionIdentifier, ref status, true );
				//skip delete - all the more reason to just store the json
				new Entity_IdentifierValueManager().SaveList( resource.Identifier, resource.RowId, Entity_IdentifierValueManager.IdentifierValue_Identifier, ref status, false );
			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, thisClassName + ".UpdateParts(). Exception while processing identities. " + ex.Message );
				status.AddError( ex.Message );
			}

			//
			try
			{

				//CostProfile (do delete in SaveList)
				CostProfileManager cpm = new Factories.CostProfileManager();
				cpm.SaveList( resource.EstimatedCosts, resource.RowId, ref status );
				new Entity_CommonCostManager().SaveList( resource.CostManifestIds, resource.RowId, ref status );
			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, thisClassName + ".UpdateParts(). Exception while processing costs. " + ex.Message );
				status.AddError( ex.Message );
			}


			//ConditionProfile 
			Entity_ConditionProfileManager emanager = new Entity_ConditionProfileManager();
			//arbitrarily delete all. 
			//20-12-28 mp - there have been deadlock issues 
			//20-12-28 - skip delete all from credential, etc. Rather checking  in save

			//emanager.DeleteAll( relatedEntity, ref status );
			try
			{
				emanager.SaveList( resource.Requires, Entity_ConditionProfileManager.ConnectionProfileType_Requirement, resource.RowId, ref status );
				emanager.SaveList( resource.Recommends, Entity_ConditionProfileManager.ConnectionProfileType_Recommendation, resource.RowId, ref status );
				emanager.SaveList( resource.Renewal, Entity_ConditionProfileManager.ConnectionProfileType_Renewal, resource.RowId, ref status );
				emanager.SaveList( resource.Corequisite, Entity_ConditionProfileManager.ConnectionProfileType_Corequisite, resource.RowId, ref status );
				emanager.SaveList( resource.CoPrerequisite, Entity_ConditionProfileManager.ConnectionProfileType_CoPrerequisite, resource.RowId, ref status );

				//Connections
				emanager.SaveList( resource.IsAdvancedStandingFor, Entity_ConditionProfileManager.ConnectionProfileType_AdvancedStandingFor, resource.RowId, ref status, 2 );
				emanager.SaveList( resource.AdvancedStandingFrom, Entity_ConditionProfileManager.ConnectionProfileType_AdvancedStandingFrom, resource.RowId, ref status, 2 );
				emanager.SaveList( resource.IsPreparationFor, Entity_ConditionProfileManager.ConnectionProfileType_PreparationFor, resource.RowId, ref status, 2 );
				emanager.SaveList( resource.PreparationFrom, Entity_ConditionProfileManager.ConnectionProfileType_PreparationFrom, resource.RowId, ref status, 2 );
				emanager.SaveList( resource.IsRequiredFor, Entity_ConditionProfileManager.ConnectionProfileType_NextIsRequiredFor, resource.RowId, ref status, 2 );
				emanager.SaveList( resource.IsRecommendedFor, Entity_ConditionProfileManager.ConnectionProfileType_NextIsRecommendedFor, resource.RowId, ref status, 2 );


				// (do delete in SaveList)
				new Entity_CommonConditionManager().SaveList( resource.ConditionManifestIds, resource.RowId, ref status );

			}
			catch ( Exception ex )
			{
				string message = FormatExceptions( ex );
				LoggingHelper.LogError( ex, thisClassName + string.Format( ".AddProfiles() - ConditionProfiles. id: {0}", resource.Id ) );
				status.AddWarning( thisClassName + ".AddProfiles(). Exceptions encountered handling ConditionProfiles. " + message );
			}


			//ProcessProfile
			Entity_ProcessProfileManager ppm = new Factories.Entity_ProcessProfileManager();
			ppm.DeleteAll( relatedEntity, ref status );
			try
			{
				ppm.SaveList( resource.AdministrationProcess, Entity_ProcessProfileManager.ADMIN_PROCESS_TYPE, resource.RowId, ref status );
				ppm.SaveList( resource.AppealProcess, Entity_ProcessProfileManager.APPEAL_PROCESS_TYPE, resource.RowId, ref status );
				ppm.SaveList( resource.ComplaintProcess, Entity_ProcessProfileManager.COMPLAINT_PROCESS_TYPE, resource.RowId, ref status );
				ppm.SaveList( resource.DevelopmentProcess, Entity_ProcessProfileManager.DEV_PROCESS_TYPE, resource.RowId, ref status );
				ppm.SaveList( resource.MaintenanceProcess, Entity_ProcessProfileManager.MTCE_PROCESS_TYPE, resource.RowId, ref status );
				ppm.SaveList( resource.ReviewProcess, Entity_ProcessProfileManager.REVIEW_PROCESS_TYPE, resource.RowId, ref status );
				ppm.SaveList( resource.RevocationProcess, Entity_ProcessProfileManager.REVOKE_PROCESS_TYPE, resource.RowId, ref status );
			}
			catch ( Exception ex )
			{
				string message = FormatExceptions( ex );
				LoggingHelper.LogError( ex, thisClassName + string.Format( ".AddProfiles() - ProcessProfiles. id: {0}", resource.Id ) );
				status.AddWarning( thisClassName + ".AddProfiles(). Exceptions encountered handling ProcessProfiles. " + message );
			}

			//
			try
			{
				Entity_CredentialManager ecm = new Entity_CredentialManager();
				//WARNING - CONSIDER CHANGING TO REPLACE, NOW THAT THERE ARE MULTIPLE ROUTES TO Entity.Credential
				ecm.DeleteAll( relatedEntity, ref status );
				//has parts
				if ( resource.HasPartIds != null && resource.HasPartIds.Count > 0 )
				{
					ecm.SaveHasPartList( resource.HasPartIds, relatedEntity.EntityUid, ref status );
				}


				//isPartOf - have to watch for duplicates here (where the other side added a hasPart
				if ( resource.IsPartOfIds != null && resource.IsPartOfIds.Count > 0 )
				{
					ecm.SaveIsPartOfList( resource.IsPartOfIds, resource.Id, ref status );
				}

                //21-07-22 mparsons - TargetPathway should be an inverse relationship and not published with a credential
                //					- so OBSOLETE?
                var epm = new Entity_PathwayManager();
                epm.SavePartList( resource.TargetPathwayIds, relatedEntity.EntityUid, ref status, CodesManager.RELATIONSHIP_TYPE_IS_PART_OF );

                //VSP
                var euvspm = new Entity_UsesVerificationServiceManager();
                euvspm.SaveList( resource.TargetPathwayIds, relatedEntity, ref status );
            }
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, thisClassName + ".UpdateParts(). Exception while processing credential parts. " + ex.Message );
				status.AddError( ex.Message );
			}
			//ETPL
			HandleETPL( resource, relatedEntity, ref status );

			//
			try
			{

				//Financial Alignment  (do delete in SaveList)
				//Entity_FinancialAlignmentProfileManager fapm = new Factories.Entity_FinancialAlignmentProfileManager();
				//fapm.SaveList( entity.FinancialAssistanceOLD, entity.RowId, ref status );

				new Entity_FinancialAssistanceProfileManager().SaveList( resource.FinancialAssistance, resource.RowId, ref status );

				//Revocation Profile (do delete in SaveList)
				Entity_RevocationProfileManager rpm = new Entity_RevocationProfileManager();
				rpm.SaveList( resource.Revocation, resource, ref status );

			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, thisClassName + ".UpdateParts(). Exception while processing financial assistance+. " + ex.Message );
				status.AddError( ex.Message );
			}

			//
			try
			{
				//addresses (do delete in SaveList)
				new Entity_AddressManager().SaveList( resource.Addresses, resource.RowId, ref status );

				//JurisdictionProfile 
				Entity_JurisdictionProfileManager jpm = new Entity_JurisdictionProfileManager();
				//do deletes - NOTE: other jurisdictions are added in: UpdateAssertedIns
				jpm.DeleteAll( relatedEntity, ref status );
				jpm.SaveList( resource.Jurisdiction, resource.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_SCOPE, ref status );

			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, thisClassName + ".UpdateParts(). Exception while processing address/jurisdiction. " + ex.Message );
				status.AddError( ex.Message );
			}


		}

		/// <summary>
		/// for a QA credential - HasETPLResource
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="relatedEntity"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public bool HandleETPL( ThisResource entity, Entity relatedEntity, ref SaveStatus status )
		{
			status.HasSectionErrors = false;
			try
			{
				Entity_CredentialManager ecm = new Entity_CredentialManager();
				ecm.SaveHasPartList( entity.HasETPLCredentialsIds, relatedEntity.EntityUid, ref status, CodesManager.RELATIONSHIP_TYPE_IsETPLResource );

				new Entity_AssessmentManager().SaveList( entity.HasETPLAssessmentsIds, relatedEntity.EntityUid, ref status, CodesManager.RELATIONSHIP_TYPE_IsETPLResource );

				new Entity_LearningOpportunityManager().SaveList( entity.HasETPLLoppsIds, relatedEntity.EntityUid, ref status, CodesManager.RELATIONSHIP_TYPE_IsETPLResource );
			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, thisClassName + ".UpdateParts(). Exception while processing ETPL. " + ex.Message );
				status.AddError( ex.Message );
			}
			//
			return status.WasSectionValid;
		}
		public bool HandleOutcomeProfiles( ThisResource entity, Entity relatedEntity, ref SaveStatus status )
		{
			status.HasSectionErrors = false;

			//Holders
			//HoldersProfileManager ecm = new HoldersProfileManager();
			//if ( ecm.SaveList( entity.Holders, relatedEntity, ref status ) == false )
			//	status.HasSectionErrors = true;
			////Earnings profile
			//var eapMgr = new EarningsProfileManager();
			//if ( eapMgr.SaveList( entity.Earnings, relatedEntity, ref status ) == false )
			//	status.HasSectionErrors = true;
			////Employment outcome
			//var eoMgr = new EmploymentOutcomeProfileManager();
			//if ( eoMgr.SaveList( entity.EmploymentOutcome, relatedEntity, ref status ) == false )
			//	status.HasSectionErrors = true;

			//AggregateData - put after holders for now to prevent delete of shared dataset profile
			var adpm = new Entity_AggregateDataProfileManager();
			if ( adpm.SaveList( entity.AggregateData, relatedEntity, ref status ) == false )
				status.HasSectionErrors = true;

			return status.WasSectionValid;
		}
		public bool AddProperties( ThisResource entity, Entity relatedEntity, ref SaveStatus status )
		{
			bool isAllValid = true;

			//============================
			EntityPropertyManager mgr = new EntityPropertyManager();
			try
			{
				//first clear all properties
				mgr.DeleteAll( relatedEntity, ref status );

				if ( mgr.AddProperties( entity.AudienceLevelType, entity.RowId, CodesManager.ENTITY_TYPE_CREDENTIAL, CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL, false, ref status ) == false )
					isAllValid = false;

				if ( mgr.AddProperties( entity.AudienceType, entity.RowId, CodesManager.ENTITY_TYPE_CREDENTIAL, CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE, false, ref status ) == false )
					isAllValid = false;
				//TODO - remove this once CredentialStatusTypeId is fully implemented. 
				//if ( mgr.AddProperties( entity.CredentialStatusType, entity.RowId, CodesManager.ENTITY_TYPE_CREDENTIAL, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_STATUS_TYPE, false, ref status ) == false )
				//	isAllValid = false;
				//TODO - there can be errors as assessment delivery type uses assessmentDeliveryType:InPerson to allow separate counts from lopp (deliveryType:InPerson)
				if ( mgr.AddProperties( entity.AssessmentDeliveryType, entity.RowId, CodesManager.ENTITY_TYPE_CREDENTIAL, CodesManager.PROPERTY_CATEGORY_ASMT_DELIVERY_TYPE, false, ref status ) == false )
					isAllValid = false;
				if ( mgr.AddProperties( entity.LearningDeliveryType, entity.RowId, CodesManager.ENTITY_TYPE_CREDENTIAL, CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE, false, ref status ) == false )
					isAllValid = false;
			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, thisClassName + ".UpdateParts(). Exception while processing ETPL. " + ex.Message );
				status.AddError( ex.Message );
			}
			//

			//
			return isAllValid;
		}
		public bool UpdateReferences( ThisResource entity, Entity relatedEntity, ref SaveStatus status )
		{
			bool isAllValid = true;
			Entity_ReferenceManager erm = new Entity_ReferenceManager();

			Entity_ReferenceFrameworkManager erfm = new Entity_ReferenceFrameworkManager();
			//
			try
			{
				///do deletes
				erm.DeleteAll( relatedEntity, ref status );
				erfm.DeleteAll( relatedEntity, ref status );

				if ( erfm.SaveList( relatedEntity.Id, CodesManager.PROPERTY_CATEGORY_SOC, entity.OccupationTypes, ref status ) == false )
					isAllValid = false;
				if ( erfm.SaveList( relatedEntity.Id, CodesManager.PROPERTY_CATEGORY_NAICS, entity.IndustryTypes, ref status ) == false )
				{
					isAllValid = false;
				}

				//TODO - handle Naics if provided separately
				if ( erfm.NaicsSaveList( relatedEntity.Id, CodesManager.PROPERTY_CATEGORY_NAICS, entity.Naics, ref status ) == false )
					isAllValid = false;

				if ( erfm.SaveList( relatedEntity.Id, CodesManager.PROPERTY_CATEGORY_CIP, entity.InstructionalProgramTypes, ref status ) == false )
					isAllValid = false;

				if ( erfm.SaveList( relatedEntity.Id, CodesManager.PROPERTY_CATEGORY_NAVY_RATING, entity.NavyRatingType, ref status ) == false )
					isAllValid = false;

				//
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
				erm.AddLanguages( entity.InLanguageCodeList, entity.RowId, CodesManager.ENTITY_TYPE_CREDENTIAL, ref status, CodesManager.PROPERTY_CATEGORY_LANGUAGE );
				//erm.AddLanguage( entity.InLanguage, relatedEntity.Id, ref status, CodesManager.PROPERTY_CATEGORY_LANGUAGE );
				//
				erm.Add( entity.SameAs, entity.RowId, CodesManager.ENTITY_TYPE_CREDENTIAL, ref status, CodesManager.PROPERTY_CATEGORY_SAME_AS, true );
			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, thisClassName + ".UpdateParts.UpdateReferences. Exception while processing ... " + ex.Message );
				status.AddError( ex.Message );
			}
			
				
			return isAllValid;
		}

		public bool UpdateAssertedBys( ThisResource resource, ref SaveStatus status )
		{
			bool isAllValid = true;
			Entity_AgentRelationshipManager mgr = new Entity_AgentRelationshipManager();
			Entity parent = EntityManager.GetEntity( resource.RowId );
			if ( parent == null || parent.Id == 0 )
			{
				status.AddError( thisClassName + " - Error - the parent entity was not found." );
				return false;
			}
			//do deletes - should this be done here, should be no other prior updates?
			mgr.DeleteAll( parent, ref status );

			mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_AccreditedBy, resource.AccreditedBy, ref status );
			mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_ApprovedBy, resource.ApprovedBy, ref status );
			mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_OFFERED_BY, resource.OfferedBy, ref status );
			mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_OWNER, resource.OwnedBy, ref status );
			mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_RecognizedBy, resource.RecognizedBy, ref status );
			mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_RegulatedBy, resource.RegulatedBy, ref status );
			mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_RevokedBy, resource.RevokedBy, ref status );
			mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_RenewedBy, resource.RenewedBy, ref status );
			mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_RegisteredBy, resource.RegisteredBy, ref status );
			//
			mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_PUBLISHEDBY, resource.PublishedBy, ref status );
			return isAllValid;
		} //


		public void UpdateAssertedIns( ThisResource resource, ref SaveStatus status )
		{

			Entity_JurisdictionProfileManager mgr = new Entity_JurisdictionProfileManager();
			Entity parent = EntityManager.GetEntity( resource.RowId );
			if ( parent == null || parent.Id == 0 )
			{
				status.AddError( thisClassName + " - Error - the parent entity was not found." );
				return;
			}
			try
			{
				//note the deleteAll is done in AddProfiles

				mgr.SaveAssertedInList( resource.RowId, Entity_AgentRelationshipManager.ROLE_TYPE_AccreditedBy, resource.AccreditedIn, ref status );
				mgr.SaveAssertedInList( resource.RowId, Entity_AgentRelationshipManager.ROLE_TYPE_ApprovedBy, resource.ApprovedIn, ref status );
				mgr.SaveAssertedInList( resource.RowId, Entity_AgentRelationshipManager.ROLE_TYPE_OFFERED_BY, resource.OfferedIn, ref status );

				mgr.SaveAssertedInList( resource.RowId, Entity_AgentRelationshipManager.ROLE_TYPE_RecognizedBy, resource.RecognizedIn, ref status );
				mgr.SaveAssertedInList( resource.RowId, Entity_AgentRelationshipManager.ROLE_TYPE_RegulatedBy, resource.RegulatedIn, ref status );
				mgr.SaveAssertedInList( resource.RowId, Entity_AgentRelationshipManager.ROLE_TYPE_RevokedBy, resource.RevokedIn, ref status );
				mgr.SaveAssertedInList( resource.RowId, Entity_AgentRelationshipManager.ROLE_TYPE_RenewedBy, resource.RenewedIn, ref status );

			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, thisClassName + ".UpdateParts-UpdateAssertedIns. Exception while processing .... " + ex.Message );
				status.AddError( ex.Message );
			}



		} //

		/// <summary>
		/// Delete a credential
		/// May be done with each new import?
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool Delete( int id, ref string statusMessage )
		{
			bool isValid = false;
			if ( id == 0 )
			{
				statusMessage = "Error - missing an identifier for the Credential";
				return false;
			}
			int orgId = 0;
			Guid orgUid = Guid.Empty;
			using ( var context = new EntityContext() )
			{
				context.Configuration.LazyLoadingEnabled = false;
				EM.Credential efEntity = context.Credential
							.SingleOrDefault( s => s.Id == id );

				if ( efEntity != null && efEntity.Id > 0 )
				{
					var rowId = efEntity.RowId;
					statusMessage = string.Format( "Credential: {0}, Id:{1}", efEntity.Name, efEntity.Id );
					if ( IsValidGuid( efEntity.OwningAgentUid ) )
					{
						var org = OrganizationManager.GetForSummary( ( Guid )efEntity.OwningAgentUid );
						if ( org != null && org.Id > 0 )
						{
							orgId = org.Id;
							orgUid = org.RowId;
						}
					}
					context.Credential.Remove( efEntity );
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						isValid = true;
						//add pending request 
						List<String> messages = new List<string>();
						new SearchPendingReindexManager().AddDeleteRequest( CodesManager.ENTITY_TYPE_CREDENTIAL, id, ref messages );
						new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, orgId, 1, ref messages );
						//also check for any relationships
						new Entity_AgentRelationshipManager().ReindexAgentForDeletedArtifact( orgUid );
						//delete cache
                        new EntityManager().EntityCacheDelete( rowId, ref statusMessage );
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
		public bool Delete( string ctid, ref string statusMessage )
		{
			bool isValid = true;
			if ( string.IsNullOrWhiteSpace( ctid ) )
			{
				statusMessage = thisClassName + ".Delete() Error - a valid CTID must be provided to delete a credential";
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
					DBResource efEntity = context.Credential
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
						if ( efEntity.OwningAgentUid.ToString().ToLower() == "40357b53-9724-4f49-8c72-86dc3a49ec02" )
						{
							//string msg2 = string.Format( "******Request to delete Ball State University credential was encountered - ignoring. Organization. Id: {0}, Name: {1}, Ctid: {2}, EnvelopeId: {3}", efEntity.Id, efEntity.Name, efEntity.CTID, envelopeId );
							//LoggingHelper.DoTrace( 1, msg2 );
							//EmailManager.NotifyAdmin( "Encountered Request to delete Ball State University", msg2 );
							//return true;
						}
						//need to handle entities like HoldersProfile that will not be deleted through RI
						//will need to handle Dataset as well 
						//var holders = Entity_HoldersProfileManager.GetAll( efEntity.RowId, true );
						SaveStatus status = new SaveStatus();
						if ( !new Entity_AggregateDataProfileManager().DeleteAll( efEntity.RowId, ref status ) )
						{

						}
						//if (!new Entity_HoldersProfileManager().DeleteAll( efEntity.RowId, ref status ))
						//{

						//}
						//if ( !new Entity_EarningsProfileManager().DeleteAll( efEntity.RowId, ref status ) )
						//{

						//}
						//if ( !new Entity_EmploymentOutcomeProfileManager().DeleteAll( efEntity.RowId, ref status ) )
						//{

						//}
						//same for earnings and others

						//need to remove from Entity.
						//-using before delete trigger - verify won't have RI issues
						string msg = string.Format( " Credential. Id: {0}, Name: {1}, Ctid: {2}.", efEntity.Id, efEntity.Name, efEntity.CTID );
						int id = efEntity.Id;
						//context.Credential.Remove( efEntity );
						efEntity.EntityStateId = 0;
						efEntity.LastUpdated = System.DateTime.Now;

						int count = context.SaveChanges();
						if ( count > 0 )
						{
							new ActivityManager().SiteActivityAdd( new SiteActivity()
							{
								ActivityType = "Credential",
								Activity = "Import",
								Event = "Delete",
								Comment = msg,
								ActivityObjectCTID = efEntity.CTID,
								//ActivityObjectId = id //although could be good as a reference for db lookup
							} );
							isValid = true;
							//add pending request 
							List<String> messages = new List<string>();
							//delete cache
							new EntityManager().EntityCacheDelete( rowId, ref statusMessage );

							new SearchPendingReindexManager().AddDeleteRequest( CodesManager.ENTITY_TYPE_CREDENTIAL, efEntity.Id, ref messages );
							//mark owning org for updates
							new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, orgId, 1, ref messages );

							//delete all relationships
							
							Entity_AgentRelationshipManager earmgr = new Entity_AgentRelationshipManager();
							earmgr.DeleteAll( rowId, ref status );
							//also check for any relationships
							//There could be other orgs from relationships to be reindexed as well!
							earmgr.ReindexAgentForDeletedArtifact( orgUid );
						}
					}
					else
					{
						statusMessage = thisClassName + $".Delete({ctid}) Warning No action taken, as the record was not found.";
						isValid = false;
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + $".Delete({ctid})" );
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
		//get absolute minimum, typically to get the id for a full get by Id
		//allows pending and even deleted, where could be a reactivate
		public static ThisResource GetMinimumByCtid( string ctid, bool includingOrgRoles = false )
		{
			LoggingHelper.DoTrace( LoggingHelper.appMethodEntryTraceLevel, thisClassName + ".GetMinimumByCtid - entered." );

			ThisResource output = new ThisResource();
			using ( var context = new EntityContext() )
			{
				DBResource input = context.Credential
						.FirstOrDefault( s => s.CTID.ToLower() == ctid.ToLower() );

				if ( input != null && input.Id > 0 )
				{
					output.RowId = input.RowId;
					output.Id = input.Id;
					output.Name = input.Name;
					output.EntityStateId = ( int )( input.EntityStateId ?? 1 );
					output.Description = input.Description;
					output.SubjectWebpage = input.SubjectWebpage;
					output.CredentialTypeId = input.CredentialTypeId ?? 0;

					output.Image = input.ImageUrl;
					output.CTID = input.CTID;
					if ( IsValidDate( input.Created ) )
						output.Created = ( DateTime ) input.Created;
					if ( IsValidDate( input.LastUpdated ) )
						output.LastUpdated = ( DateTime ) input.LastUpdated;
					output.CredentialRegistryId = input.CredentialRegistryId;
					//get this for use by import and preserving published by
					if ( IsGuidValid( input.OwningAgentUid ) )
					{
						output.PrimaryAgentUID = ( Guid )input.OwningAgentUid;
						output.PrimaryOrganization = OrganizationManager.GetForSummary( output.PrimaryAgentUID );

						//get roles - do we need these for minimum?
						MPM.OrganizationRoleProfile orp = Entity_AgentRelationshipManager.AgentEntityRole_GetAsEnumerationFromCSV( output.RowId, output.PrimaryAgentUID );
						output.OwnerRoles = orp.AgentRole;
					}
					//GetAllCombinedForTarget - do we need these for minimum?
					if ( includingOrgRoles )
						output.OrganizationRole = Entity_AssertionManager.GetAllCombinedForTarget( 1, output.Id, output.OwningOrganizationId );

				}
			}
			LoggingHelper.DoTrace( LoggingHelper.appMethodExitTraceLevel, thisClassName + ".GetMinimumByCtid - exit." );

			return output;
		}

		public static ThisResource GetMinimumByGUID( Guid identifier, bool includingOrgRoles = false )
		{
			LoggingHelper.DoTrace( LoggingHelper.appMethodEntryTraceLevel, thisClassName + ".GetMinimumByGUID - entered." );

			ThisResource output = new ThisResource();
			using ( var context = new EntityContext() )
			{
				DBResource input = context.Credential
						.FirstOrDefault( s => s.RowId == identifier);

				if ( input != null && input.Id > 0 )
				{
					output.RowId = input.RowId;
					output.Id = input.Id;
					output.Name = input.Name;
					output.EntityStateId = ( int ) ( input.EntityStateId ?? 1 );
					output.Description = input.Description;
					output.SubjectWebpage = input.SubjectWebpage;
					output.CredentialTypeId = input.CredentialTypeId ?? 0;

					output.Image = input.ImageUrl;
					output.CTID = input.CTID;
					if ( IsValidDate( input.Created ) )
						output.Created = ( DateTime ) input.Created;
					if ( IsValidDate( input.LastUpdated ) )
						output.LastUpdated = ( DateTime ) input.LastUpdated;
					output.CredentialRegistryId = input.CredentialRegistryId;
					//get this for use by import and preserving published by
					if ( IsGuidValid( input.OwningAgentUid ) )
					{
						output.PrimaryAgentUID = ( Guid ) input.OwningAgentUid;
						output.PrimaryOrganization = OrganizationManager.GetForSummary( output.PrimaryAgentUID );

						//get roles - do we need these for minimum?
						MPM.OrganizationRoleProfile orp = Entity_AgentRelationshipManager.AgentEntityRole_GetAsEnumerationFromCSV( output.RowId, output.PrimaryAgentUID );
						output.OwnerRoles = orp.AgentRole;
					}

				}
			}
			LoggingHelper.DoTrace( LoggingHelper.appMethodExitTraceLevel, thisClassName + ".GetMinimumByGUID - exit." );

			return output;
		}

		//public static ThisResource GetBySubjectWebpage( string swp )
		//{
		//	ThisResource entity = new ThisResource();
		//	using ( var context = new EntityContext() )
		//	{
		//		context.Configuration.LazyLoadingEnabled = false;
		//		DBResource from = context.Credential
		//				.FirstOrDefault( s => s.SubjectWebpage.ToLower() == swp.ToLower() );

		//		if ( from != null && from.Id > 0 )
		//		{
		//			entity.RowId = from.RowId;
		//			entity.Id = from.Id;
		//			entity.Name = from.Name;
		//			entity.EntityStateId = ( int )( from.EntityStateId ?? 1 );
		//			entity.Description = from.Description;
		//			entity.SubjectWebpage = from.SubjectWebpage;
		//			entity.CredentialTypeId = from.CredentialTypeId ?? 0;

		//			entity.Image = from.ImageUrl;
		//			entity.CTID = from.CTID;
		//			entity.CredentialRegistryId = from.CredentialRegistryId;
		//		}
		//	}
		//	return entity;
		//}
		public static ThisResource GetByName_SubjectWebpage( string name, string swp )
		{
			ThisResource entity = new ThisResource();
			CredentialRequest cr = new CredentialRequest();
			//getting all, as update will follow
			cr.IsDetailRequest();
			if (string.IsNullOrWhiteSpace(swp))
				return null;
			if (swp.IndexOf("//") == -1)
				return null;
			bool hasHttps = false;
			if (swp.ToLower().IndexOf("https:") > -1)
				hasHttps = true;

			//swp = swp.Substring( swp.IndexOf( "//" ) + 2 );
			//swp = swp.ToLower().TrimEnd( '/' );
			var host = new Uri(swp).Host;
			var domain = host.Substring(host.LastIndexOf('.', host.LastIndexOf('.') - 1) + 1);
			using (var context = new EntityContext())
			{
				//s.Name.ToLower() == name.ToLower() && 
				context.Configuration.LazyLoadingEnabled = false;
				var list = context.Credential
						.Where(s => s.SubjectWebpage.ToLower().Contains(domain) && s.EntityStateId > 1)
						.OrderByDescending(s => s.EntityStateId)
						.ThenBy(s => s.Name)
						.ToList();
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
							MapFromDB(from, entity, cr);
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
							MapFromDB(from, entity, cr);
							break;
						}
					}
				}
			}

			return entity;
			//using ( var context = new EntityContext() )
			//{
			//	context.Configuration.LazyLoadingEnabled = false;
			//	DBResource from = context.Credential
			//			.FirstOrDefault( s => s.Name.ToLower() == name.ToLower() && s.SubjectWebpage == swp );

			//	if ( from != null && from.Id > 0 )
			//	{
			//		MapFromDB( from, entity, cr );
			//		//entity.RowId = from.RowId;
			//		//entity.Id = from.Id;
			//		//entity.Name = from.Name;
			//		//entity.EntityStateId = ( int )( from.EntityStateId ?? 1 );
			//		//entity.Description = from.Description;
			//		//entity.SubjectWebpage = from.SubjectWebpage;
			//		//entity.CredentialTypeId = from.CredentialTypeId ?? 0;

			//		//entity.ImageUrl = from.ImageUrl;
			//		//entity.CTID = from.CTID;
			//		//entity.CredentialRegistryId = from.CredentialRegistryId;
			//	}
			//}

		}

		public static ThisResource GetByName_CodedNotation_PrimaryAgentUId( string name, string codedNotation, Guid PrimaryAgentUId )
		{
			ThisResource entity = new ThisResource();
			CredentialRequest cr = new CredentialRequest();
			//getting all, as update will follow
			cr.IsDetailRequest();
			if ( string.IsNullOrWhiteSpace( codedNotation ) )
				return null;
			using ( var context = new EntityContext() )
			{
				//s.Name.ToLower() == name.ToLower() && 
				context.Configuration.LazyLoadingEnabled = false;
				var list = context.Credential
						.Where( s => s.CodedNotation.ToLower().Contains( codedNotation.ToLower() ) && s.EntityStateId > 1 && s.PrimaryOrganizationUid == PrimaryAgentUId )
						.OrderByDescending( s => s.EntityStateId )
						.ThenBy( s => s.Name )
						.ToList();
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

							MapFromDB( from, entity, cr );
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
									Comment = $"{Entity_Label} Get by Name and Coded Notation. Found additional full {EntityType} for name: {name}, CodedNotation: {codedNotation}. First {EntityType}: {entity.Name} ({entity.Id})"
								} );

							}
							MapFromDB( from, entity, cr );
							break;
						}
					}
				}
			}

			return entity;
		}

		public static ThisResource GetForCompare( int id, CredentialRequest cr )
		{
			ThisResource entity = new ThisResource();
			if ( id < 1 )
				return entity;
			using ( var context = new EntityContext() )
			{
				//context.Configuration.LazyLoadingEnabled = false;
				EM.Credential item = context.Credential
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
		/// Get a credential with minimum properties
		/// ?should we allow get on a 'deleted' cred? Most people wouldn't remember the Id, although could be from a report
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public static ThisResource GetBasic( int id )
		{

			ThisResource entity = new ThisResource();
			var cr = new CredentialRequest();
			cr.IsForProfileLinks = true;
			if ( id < 1 )
				return entity;

			using ( var context = new EntityContext() )
			{
                DBResource item = context.Credential
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

			ThisResource entity = new ThisResource();
			CredentialRequest cr = new CredentialRequest();
			cr.IsForProfileLinks = true;
			if ( id < 1 )
				return "";

			using ( var context = new EntityContext() )
			{
				if ( cr.IsForProfileLinks )
					context.Configuration.LazyLoadingEnabled = false;
				EM.Credential item = context.Credential
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
		public static ThisResource GetBasic( Guid id )
		{

			ThisResource entity = new ThisResource();
			CredentialRequest cr = new CredentialRequest();
			cr.IsForProfileLinks = true;
			if ( !IsGuidValid( id ) )
				return entity;

			using ( var context = new EntityContext() )
			{
				//if ( cr.IsForProfileLinks )
				//context.Configuration.LazyLoadingEnabled = false;
				EM.Credential item = context.Credential
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

		public static ThisResource GetForDetail( int id )
		{
			CredentialRequest cr = new CredentialRequest();
			cr.IsDetailRequest();
			return GetForDetail( id, cr );
		}

		public static ThisResource GetForDetail( int id, CredentialRequest cr )
		{
			ThisResource entity = new ThisResource();

			using ( var context = new EntityContext() )
			{
                DBResource item = context.Credential
							.SingleOrDefault( s => s.Id == id );
				try
				{
					if ( item != null && item.Id > 0 )
					{
						//check for virtual deletes
						if ( item.EntityStateId == 0 )
						{
							LoggingHelper.DoTrace( 1, string.Format( thisClassName + " Error: encountered request for detail for a deleted record. Name: {0}, CTID:{1}", item.Name, item.CTID ) );
							entity.Name = "Credential was not found.";
							entity.CTID = item.CTID;
							return entity;
						}

						MapFromDB( item, entity, cr );
						//TODO - we may have this info from entity.UsesVerificationProfile now
						if ( HasBadgeClaims( item.RowId ) )
							entity.HasVerificationType_Badge = true;
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".GetForDetail(), Name: {0} ({1})", item.Name, item.Id ) );
					entity.StatusMessage = FormatExceptions( ex );
					//entity.Id = 0;
				}
			}

			return entity;
		}
		//public static bool HasBadgeClaimsOLD( int credentialId )
		//{

		//	try
		//	{
		//		using ( var context = new EntityContext() )
		//		{
		//			var claims = ( from a in context.Entity_VerificationProfile
		//						   join c in context.Entity on a.RowId equals c.EntityUid
		//						   join d in context.Entity_Credential on c.Id equals d.EntityId
		//						   join e in context.Entity_Property on c.Id equals e.EntityId
		//						   join f in context.Codes_PropertyValue on e.PropertyValueId equals f.Id
		//						   where f.SchemaName == "claimType:BadgeClaim"
		//						   && d.CredentialId == credentialId
		//						   select d )
		//							.ToList();


		//			if ( claims != null && claims.Count() > 0 )
		//			{
		//				return true;
		//			}
		//		}
		//	}
		//	catch ( Exception ex )
		//	{
		//		LoggingHelper.LogError( ex, thisClassName + ".HasBadgeClaims" );
		//	}
		//	return false;
		//}//
        public static bool HasBadgeClaims( Guid credentialUID )
        {

            try
            {
                using ( var context = new EntityContext() )
                {
                    var claims = ( from a		in context.Entity_UsesVerificationService
                                   join c		in context.Entity on a.EntityId equals c.Id

                                   join d		in context.VerificationServiceProfile on a.VerificationServiceId equals d.Id
                                   join vspE	in context.Entity on d.RowId equals vspE.EntityUid
                                   join e		in context.Entity_Property on vspE.Id equals e.EntityId

                                   join f		in context.Codes_PropertyValue on e.PropertyValueId equals f.Id
                                   where f.SchemaName == "claimType:BadgeClaim"
                                   && c.EntityUid == credentialUID
                                   select d )
                                    .ToList();


                    if ( claims != null && claims.Count() > 0 )
                    {
                        return true;
                    }
                }
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, thisClassName + ".HasBadgeClaims" );
            }
            return false;
        }//
        /// <summary>
        /// Get summary view of a credential
        /// Useful for accessing counts
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // [Obsolete]
        //private static EM.Credential_SummaryCache GetSummary( int id )
        //{

        //    EM.Credential_SummaryCache item = new EM.Credential_SummaryCache();
        //    try
        //    {
        //        using ( var context = new EntityContext() )
        //        {

        //            item = context.Credential_SummaryCache
        //                        .SingleOrDefault( s => s.CredentialId == id );

        //            if ( item != null && item.CredentialId > 0 )
        //            {

        //            }
        //        }
        //    }
        //    catch ( Exception ex )
        //    {
        //        LoggingHelper.LogError( ex, thisClassName + string.Format( ".GetSummary(), Id: {0}", id ) );                
        //    }
        //    return item;
        //}
        public static List<Credential> GetPending()
		{
			List<ThisResource> output = new List<ThisResource>();
			ThisResource entity = new ThisResource();
			using ( var context = new EntityContext() )
			{

				List<EM.Credential> list = context.Credential
							.Where( s => s.EntityStateId == 1 )
							.OrderBy( s => s.CTID )
							.ToList();

				if ( list != null && list.Count > 0 )
				{
					foreach ( var item in list )
					{
						//there is very little data in a pending record
						entity = new ThisResource();
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
		//public static List<Credential> GetAllForOwningOrg( Guid ownedByUid )
		//{
		//	List<ThisResource> output = new List<ThisResource>();
		//	ThisResource entity = new ThisResource();
		//	CredentialRequest cr = new CredentialRequest();
		//	cr.IsForProfileLinks = true;
		//	using ( var context = new EntityContext() )
		//	{

		//		List<EM.Credential> list = context.Credential
		//					.Where( s => s.EntityStateId == 3
		//						&& s.OwningAgentUid == ownedByUid )
		//					.OrderBy( s => s.CTID )
		//					.ToList();

		//		if ( list != null && list.Count > 0 )
		//		{
		//			foreach ( var item in list )
		//			{
		//				//there is very little data in a pending record
		//				entity = new ThisResource();
		//				MapFromDB( item, entity, cr );
		//				output.Add( entity );
		//			}
		//		}
		//	}

		//	return output;
		//}
		public static List<string> AutocompleteInternal( string keyword, int pageNumber, int pageSize, ref int pTotalRows )
		{
			var output = new List<string>();
			keyword = ( keyword ?? string.Empty ).ToLower();
			//want to use a simple search for autocomplete, minimum joins
			bool includeDesc = false;
			try
			{
				using ( var context = new EntityContext() )
				{
					var results = context.Credential
							.Where( s => s.Name.ToLower().Contains( keyword )
							|| ( includeDesc && s.Description.ToLower().Contains( keyword ) )
								)
							//.OrderBy( s => s.Name )
							.ToList();
					if ( results != null && results.Count > 0 )
					{
						output.AddRange( results.Select( m => m.Name ).Distinct().Take( pageSize ).ToList() );
						pTotalRows = output.Count();
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".Autocomplete" );
			}



			return output;
		}

		public static List<string> AutocompleteDB( string pFilter, int pageNumber, int pageSize, ref int pTotalRows )
		{
			bool autocomplete = true;
			var output = new List<string>();

			List<CM.CredentialSummary> list = Search( pFilter, string.Empty, pageNumber, pageSize, ref pTotalRows, autocomplete );
			bool appendingOrgNameToAutocomplete = UtilityManager.GetAppKeyValue( "appendingOrgNameToAutocomplete", false );
			string prevName = string.Empty;
			if ( !appendingOrgNameToAutocomplete )
			{
				//note excluding duplicates may have an impact on selected max terms
				output.AddRange( list.Select( m => m.Name ).Distinct().Take( pageSize ).ToList() );
				pTotalRows = output.Count();
				return output;
			}

			foreach ( CM.CredentialSummary item in list )
			{
				//note excluding duplicates may have an impact on selected max terms
				if ( string.IsNullOrWhiteSpace( item.OwnerOrganizationName )
					|| !appendingOrgNameToAutocomplete )
				{
					if ( item.Name.ToLower() != prevName )
						output.Add( item.Name );
				}
				else
				{
					output.Add( item.Name + " ('" + item.OwnerOrganizationName + "')" );
				}

				prevName = item.Name.ToLower();
			}

			return output;
		}
		/// <summary>
		/// Do an existance search. Typically don't want to return all results, just that there are results
		/// </summary>
		/// <param name="filter"></param>
		/// <param name="orderBy"></param>
		/// <param name="pageNumber"></param>
		/// <param name="pageSize"></param>
		/// <param name="totalRows"></param>
		/// <param name="autocomplete"></param>
		/// <returns></returns>
		public static bool ExistanceSearch( string filter,  ref int totalRows, bool autocomplete = true )
		{
			string orderBy = string.Empty;
			int pageNumber = 1;
			int pageSize = 5;
			var list = Search( filter, orderBy, pageNumber, pageSize, ref totalRows, autocomplete, true );
			
			return totalRows > 0;
		}
		public static List<CM.CredentialSummary> Search( string pFilter, string pOrderBy, int pageNumber, int pageSize, ref int pTotalRows, bool autocomplete = false, bool existanceSearch=false )
        {
            string connectionString = DBConnectionRO();
            CM.CredentialSummary item = new CM.CredentialSummary();
            List<CM.CredentialSummary> list = new List<CM.CredentialSummary>();
            var result = new DataTable();
            LoggingHelper.DoTrace( 6, $"CredentialManager_Search - Page: {pageNumber}. filter\r\n " + pFilter );

            bool includingHasPartIsPartWithConnections = UtilityManager.GetAppKeyValue( "includeHasPartIsPartWithConnections", false );

            //int avgMinutes = 0;
            //string orgName = string.Empty;
            //int totals = 0;

            using ( SqlConnection c = new SqlConnection( connectionString ) )
            {
                c.Open();

                if ( string.IsNullOrEmpty( pFilter ) )
                {
                    pFilter = string.Empty;
                }

                using ( SqlCommand command = new SqlCommand( "[Credential.Search]", c ) )
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
						if ( !existanceSearch )
						{
							item = new CM.CredentialSummary();
                            item.Name = "EXCEPTION ENCOUNTERED - " + ex.Message;
                            item.Description = ex.Message;
							item.CredentialTypeSchema = "error";
							list.Add( item );
						}
                        return list;

                    }
                }

                //Used for costs. Only need to get these once. See below. - NA 5/12/2017
                //var currencies = CodesManager.GetCurrencies();
                //var costTypes = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_CREDENTIAL_ATTAINMENT_COST );
				if ( existanceSearch )
				{
					//just return total rows
					return list;
				}
				int lastId = 0;
				int cntr = 0;
				foreach ( DataRow dr in result.Rows )
                {
					try
					{
						//avgMinutes = 0;
						item = new CM.CredentialSummary();
						item.Id = GetRowColumn( dr, "Id", 0 );
						lastId = item.Id;
                        cntr++;
                        if ( cntr % 200 == 0 )
                            LoggingHelper.DoTrace( 2, string.Format( " Page: {0} - loading record: {1}", pageNumber, cntr ) );

                        //item.Name = GetRowColumn( dr, "Name", "missing" );
                        item.Name = dr[ "Name" ].ToString();
						item.FriendlyName = FormatFriendlyTitle( item.Name );
						item.SubjectWebpage = dr[ "SubjectWebpage" ].ToString();

						item.CTID = GetRowColumn( dr, "CTID" );
						item.PrimaryOrganizationCTID = dr[ "OrganizationCTID" ].ToString();
						item.CredentialRegistryId = dr[ "CredentialRegistryId" ].ToString();
						item.OwnerOrganizationId = GetRowPossibleColumn( dr, "OwningOrganizationId", 0 );
						item.OwnerOrganizationName = GetRowPossibleColumn( dr, "owningOrganization" );
						//watch if this is coming from the cache, change!
						if ( item.OwnerOrganizationName.IndexOf( "Placeholder" ) > -1 )
							item.OwnerOrganizationName = string.Empty;
						item.CredentialType = GetRowPossibleColumn( dr, "CredentialType", string.Empty );

						item.CredentialTypeSchema = GetRowPossibleColumn( dr, "CredentialTypeSchema", string.Empty );
						string date = GetRowColumn( dr, "DateEffective", string.Empty );
						if ( IsValidDate( date ) )
							item.DateEffective = ( DateTime.Parse( date ).ToString("yyyy-MM-dd") );
						else
							item.DateEffective = string.Empty;
						date = GetRowColumn( dr, "Created", string.Empty );
						if ( IsValidDate( date ) )
							item.Created = DateTime.Parse( date );
						date = GetRowColumn( dr, "LastUpdated", string.Empty );
						if ( IsValidDate( date ) )
							item.LastUpdated = DateTime.Parse( date );

						//string rowId = GetRowColumn( dr, "RowId" );
						//string rowId = GetRowColumn( dr, "EntityUid" );
						string rowId = dr[ "EntityUid" ].ToString();
						//if ( IsGuidValid( rowId ) )
						item.RowId = new Guid( rowId );

						//for autocomplete, only need name
						if ( autocomplete )
						{
							list.Add( item );
							continue;
						}
						//item.Description = GetRowColumn( dr, "Description", string.Empty );
						item.Description = dr[ "Description" ].ToString();

						item.Version = GetRowPossibleColumn( dr, "Version", string.Empty );
						//item.LatestVersionUrl = GetRowPossibleColumn( dr, "LatestVersionUrl", string.Empty );
						//item.PreviousVersion = GetRowPossibleColumn( dr, "PreviousVersion", string.Empty );

						item.ProcessStandards = GetRowPossibleColumn( dr, "ProcessStandards", string.Empty );


						// item.CredentialTypeSchema = dr["CredentialTypeSchema"].ToString();
						item.TotalCost = GetRowPossibleColumn( dr, "TotalCost", 0m );
						//AverageMinutes is a rough approach to sorting. If present, get the duration profiles
						if ( GetRowPossibleColumn( dr, "AverageMinutes", 0 ) > 0 )
						{
							item.EstimatedTimeToEarn = DurationProfileManager.GetAll( item.RowId );
						}

						item.IsAQACredential = GetRowPossibleColumn( dr, "IsAQACredential", false );
						item.HasQualityAssurance = GetRowPossibleColumn( dr, "HasQualityAssurance", false );

						item.RequiresCompetenciesCount = GetRowPossibleColumn( dr, "RequiresCompetenciesCount", 0 );
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
						item.IndustryResults = Fill_CodeItemResults( dr, "NaicsList", CodesManager.PROPERTY_CATEGORY_NAICS, false, false );
						item.IndustryOtherResults = Fill_CodeItemResults( dr, "OtherIndustriesList", CodesManager.PROPERTY_CATEGORY_NAICS, false, false, false );

						//OccupationsCSV
						item.OccupationResults = Fill_CodeItemResults( dr, "OccupationsList", CodesManager.PROPERTY_CATEGORY_SOC, false, false );
						item.OccupationOtherResults = Fill_CodeItemResults( dr, "OtherOccupationsList", CodesManager.PROPERTY_CATEGORY_SOC, false, false, false );
						//education levels CSV
						//16-09-12 mp - changed to use pipe (|) rather than ; due to conflicts with actual embedded semicolons
						item.AudienceLevelsResults = Fill_CodeItemResults( dr, "LevelsList", CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL, false, false );

						//item.AudienceTypesResults = Fill_CodeItemResults( dr, "CrendentialProperties", CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE, false, false );

						//item.AssessmentDeliveryType = Fill_CodeItemResults( dr, "CrendentialProperties", CodesManager.PROPERTY_CATEGORY_ASMT_DELIVERY_TYPE, false, false );

						//item.LearningDeliveryType = Fill_CodeItemResults( dr, "CrendentialProperties", CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE, false, false );

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

						string subjects = GetRowPossibleColumn( dr, "SubjectsList", string.Empty );

						if ( !string.IsNullOrWhiteSpace( subjects ) )
						{
							var codeGroup = subjects.Split( '|' );
							foreach ( string codeSet in codeGroup )
							{
								var codes = codeSet.Split( '~' );
								item.Subjects.Add( codes[ 0 ].Trim() );
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
					}catch(Exception ex)
					{
						LoggingHelper.DoTrace( 1, $"Credential.Search. Last Id: {item.Id}" + ex.Message );
					}
					finally
					{
					}
                }
				LoggingHelper.DoTrace( 1, string.Format( "Credential search. Page: {0}, LastId: {1}", pageNumber, lastId ) );

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
		public static void MapFromDB( EM.Credential input, ThisResource output, CredentialRequest cr )
		{
			var duration = new TimeSpan();
			DateTime allStarted = DateTime.Now;
			DateTime started = DateTime.Now;

			output.Id = input.Id;
			output.RowId = input.RowId;
			output.EntityStateId = ( int )( input.EntityStateId ?? 1 );

			output.Name = input.Name;
			output.FriendlyName = FormatFriendlyTitle( input.Name );
			output.NamePlusOrganization = output.Name;
			output.Description = input.Description;

			output.SubjectWebpage = input.SubjectWebpage != null ? input.SubjectWebpage : "";

			output.CTID = input.CTID;
			output.CredentialRegistryId = input.CredentialRegistryId;
			// 16-06-15 mp - always include credential type
			//can be null for a pending record
			output.CredentialTypeId = ( int )( input.CredentialTypeId ?? 0 );
			if ( output.CredentialTypeId > 0 )
			{
				CodeItem ct = CodesManager.Codes_PropertyValue_Get( output.CredentialTypeId );
				if ( ct != null && ct.Id > 0 )
				{
					output.CredentialType = ct.Title;
					output.CTDLTypeLabel = ct.Title;
					output.CredentialTypeSchema = ct.SchemaName;
				}
				//retain example using an Enumeration for by other related tableS???
				//this is no longer valid as there is no data in entity.Propertyy
				output.CredentialTypeEnum = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_TYPE );
				output.CredentialTypeEnum.Items.Add( new EnumeratedItem() { Id = output.CredentialTypeId, Name = ct.Name, SchemaName = ct.SchemaName } );
			}
            //22-05-19 - credentialStatusTypeId is now on the credential directly
            output.CredentialStatusTypeId = input.CredentialStatusTypeId;
			if ( output.CredentialStatusTypeId > 0 )
			{
				CodeItem ct = CodesManager.Codes_PropertyValue_Get( output.CredentialStatusTypeId );
				if ( ct != null && ct.Id > 0 )
				{
					output.CredentialStatus = ct.Title;
					//do we really need this?
					output.CredentialStatusTypeSchema = ct.SchemaName;
				}
				//retain example using an Enumeration for by other related tableS??? - old detail page?
				output.CredentialStatusType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_STATUS_TYPE );
				output.CredentialStatusType.Items.Add( new EnumeratedItem() { Id = output.CredentialStatusTypeId, Name = ct.Name, SchemaName = ct.SchemaName } );
			}
			else
			{
				//OLD
				//ensure only one status. Previous property should have been deleted.
				output.CredentialStatusType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_STATUS_TYPE );
				EnumeratedItem statusItem = output.CredentialStatusType.GetFirstItem();
				if ( statusItem != null && statusItem.Id > 0 && statusItem.Name != "Active" )
				{
					//if ( cr.IsForDetailView && output.Name.IndexOf( statusItem.Name ) == -1 )
					//	output.Name += string.Format( " ({0})", statusItem.Name );
				}
			}
			//
			if ( input.ImageUrl != null && input.ImageUrl.Trim().Length > 0 )
				output.Image = input.ImageUrl;
			else
				output.Image = null;
			var hasPrimaryOrg = false;	
			if ( IsGuidValid( input.OwningAgentUid ) )
			{
				output.PrimaryAgentUID = ( Guid )input.OwningAgentUid;
				output.PrimaryOrganization = OrganizationManager.GetForSummary( output.PrimaryAgentUID, false );
				output.OwningOrgDisplay = output.PrimaryOrganization.Name;
				hasPrimaryOrg = true;
				output.NamePlusOrganization = output.Name + $" ( {output.PrimaryOrganization.Name} )";
				//get roles for owning org
				//will anything be missed that may be published with the org?
				MPM.OrganizationRoleProfile orp = Entity_AgentRelationshipManager.AgentEntityRole_GetAsEnumerationFromCSV( output.RowId, output.PrimaryAgentUID );
				output.OwnerRoles = orp.AgentRole;
			} else
			{
				//need to elevate the offered by and 'tag' as such
			}
            output.OrganizationRole = Entity_AgentRelationshipManager.AgentEntityRole_GetAll_ToEnumeration( output.RowId, true );

			output.JsonProperties = input.JsonProperties;
			//

			if ( cr.IsForProfileLinks )
			{
				duration = DateTime.Now.Subtract( started );
				//if ( saveDuration.TotalSeconds > 1 )
				LoggingHelper.DoTrace( 7, string.Format( "         Map Duration: {0:N2} seconds after get minimum {1}", duration.TotalSeconds, input.Name ) );
				//return minimum ===========
				return;
			}
			//===================================================================


            output.AudienceLevelType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL );
            output.AudienceType = EntityPropertyManager.FillEnumeration( output.RowId,CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE );

			output.AssessmentDeliveryType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_ASMT_DELIVERY_TYPE );
			output.LearningDeliveryType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE );

			
			//populate related stuff:

			if ( IsValidDate( input.Created ) )
				output.Created = ( DateTime )input.Created;
			if ( IsValidDate( input.LastUpdated ) )
				output.LastUpdated = ( DateTime )input.LastUpdated;
			var relatedEntity = EntityManager.GetEntity( output.RowId, false );
			//NOTE: EntityLastUpdated should really be the last registry update now. Check how LastUpdated is assigned on import
			//21-07-06 - lastUpdated is set to the envelope lastUpdated date. The latter should be the visible last updated date.
			//if ( relatedEntity != null && relatedEntity.Id > 0 )
				output.EntityLastUpdated = output.LastUpdated;	// relatedEntity.LastUpdated;

			if ( IsGuidValid( input.CopyrightHolder ) )
            {
                var copyrightHolder = ( Guid )input.CopyrightHolder;
                //not sure if we need the org for display?
                var org = OrganizationManager.GetForSummary( copyrightHolder );
				if ( org != null && org.Id > 0 )
					output.CopyrightHolderOrganization =new List<Organization>() { org };
			}

			//will need output do convert before switching these
			//AlternateName is used by API
			output.AlternateName = Entity_ReferenceManager.GetAllToList( output.RowId, CodesManager.PROPERTY_CATEGORY_ALTERNATE_NAME );
            //output.AlternateNames = Entity_ReferenceManager.GetAll( output.RowId, CodesManager.PROPERTY_CATEGORY_ALTERNATE_NAME );

            output.CredentialId = input.CredentialId;
            //output.CodedNotation = input.CodedNotation;
			output.ISICV4 = input.ISICV4;
			//TODO - should these be suppressed if same as subjectwebpage?
			output.AvailabilityListing = input.AvailabilityListing;
			output.AvailableOnlineAt = input.AvailableOnlineAt;
			output.InCatalog = GetUrlData( input.InCatalog );

			if ( IsValidDate( input.EffectiveDate ) )
                output.DateEffective = ( ( DateTime )input.EffectiveDate ).ToString("yyyy-MM-dd");
            else
                output.DateEffective = string.Empty;
			if ( IsValidDate( input.ExpirationDate ) )
				output.ExpirationDate = ( ( DateTime )input.ExpirationDate ).ToString("yyyy-MM-dd");
			else
				output.ExpirationDate = string.Empty;

			//21-04-05 mp - changing to store the CTID. So will need to retrieve the credential(s)
			//				- actually the url can sometimes be external
			//				- or store as JSON?
			output.LatestVersion = input.LatestVersionUrl;
            output.PreviousVersion = input.ReplacesVersionUrl;
			output.NextVersion = input.NextVersion;
			output.Supersedes = input.Supersedes;
			output.SupersededBy = input.SupersededBy;

			output.Identifier = Entity_IdentifierValueManager.GetAll( output.RowId, Entity_IdentifierValueManager.IdentifierValue_Identifier );
			output.SameAs = Entity_ReferenceManager.GetAll( input.RowId, CodesManager.PROPERTY_CATEGORY_SAME_AS ); //  = 76;


            //multiple languages, now in entity.reference
            output.InLanguageCodeList = Entity_ReferenceManager.GetAll( output.RowId, CodesManager.PROPERTY_CATEGORY_LANGUAGE );

			var getAll = Entity_HasResourceManager.GetAll( relatedEntity );
			//var getAll = Entity_HasResourceManager.GetAllEntityType( relatedEntity, CodesManager.ENTITY_TYPE_OCCUPATIONS_PROFILE, HasSpecializationRelationshipId );
			if ( getAll != null && getAll.Count > 0 )
			{
				output.HasRubric = getAll.Where( r => r.EntityTypeId == CodesManager.ENTITY_TYPE_RUBRIC ).ToList();
				output.ProvidesTransferValueFor = getAll.Where( r => r.RelationshipTypeId == Entity_HasResourceManager.HAS_RESOURCE_TYPE_ProvidesTransferValueFor && ( r.EntityTypeId == CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE ) ).ToList();
				output.ReceivesTransferValueFrom = getAll.Where( r => r.RelationshipTypeId == Entity_HasResourceManager.HAS_RESOURCE_TYPE_ReceivesTransferValueFrom && ( r.EntityTypeId == CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE ) ).ToList();
			}

			output.ProcessStandards = input.ProcessStandards ?? string.Empty;
            output.ProcessStandardsDescription = input.ProcessStandardsDescription ?? string.Empty;

			//
			duration = DateTime.Now.Subtract( started );
			//if ( saveDuration.TotalSeconds > 1 )
			LoggingHelper.DoTrace( 7, string.Format( "         Map Duration: {0:N2} seconds after basic mapping for {1}", duration.TotalSeconds, input.Name ) );
			started = DateTime.Now;
			//---------------
			if ( cr.IncludingRolesAndActions || !hasPrimaryOrg )
			{
				var orgRoleManager = new OrganizationRoleManager();
				output.JurisdictionAssertions = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( output.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_OFFERREDIN );


				//get as ennumerations
				//var oldRoles = Entity_AgentRelationshipManager.AgentEntityRole_GetAll_ToEnumeration( output.RowId, true );
				//this should really be QA only, the latter (AgentEntityRole_GetAll_ToEnumeration) included owns/offers
				if ( output.OwningOrganizationId > 0 )
				{
					//OLD
					//21-07-14 - getting duplicates with new method, resetting to use this for demo
					//output.OrganizationRole = Entity_AssertionManager.GetAllCombinedForTarget( 1, output.Id, output.OwningOrganizationId );
					//NEW. Consider duplicates?
					//how to store first party and third party?
					//compare these before committing to new version
					//TODO - how to control only getting non-QA from EAR
					//23-07-10 - this will only get roles with the owner as the asserter
					output.OrganizationRole = orgRoleManager.GetAllCombinedForTarget( 1, output.Id, output.OwningOrganizationId );
					/*
					var roles = orgRoleManager.GetCombinedRoles( 1, output.RowId, output.OwningOrganizationId );
					output.OrganizationRole = Entity_AgentRelationshipManager.GetAllThirdPartyAssertionsForEntity( 1, output.RowId, output.OwningOrganizationId );
					var firstPartyAssertions = Entity_AssertionManager.GetAllFirstPartyAssertionsForTarget( 1, output.RowId, output.OwningOrganizationId, false );

					if ( firstPartyAssertions != null && firstPartyAssertions.Any() )
					{
						//foreach( var item in orgFirstPartyAssertions )
						//{
						//	var exists = output.OrganizationRole.Where( m => m.ActingAgentId == item.ActingAgentId ).ToList();
						//}
						output.OrganizationRole.AddRange( firstPartyAssertions );
					}
					*/
				}
                //23-07-10 trying this. Can then filter out QA, or owns/offers OR use the latter as needed 
                output.OrganizationRole = Entity_AgentRelationshipManager.AgentEntityRole_GetAll_ToEnumeration( output.RowId, true );


                if ( cr.IsForAPIRequest && !string.IsNullOrWhiteSpace( output.CTID ) && output.EntityStateId == 3 && output.OwningOrganizationId > 0 )
				{
					//new - get owner QA now. only if particular context
					//actually don't want this as is slow. Break up into parts
					//21-07-14 - getting duplicates with new method, resetting to use this for demo
					//compare
					var started2 = DateTime.Now;
					//output.OwningOrganizationQAReceived = Entity_AssertionManager.GetAllCombinedForTarget( 2, output.OwningOrganization.Id, output.OwningOrganization.Id );
					//var saveDuration = DateTime.Now.Subtract( started2 );
					//LoggingHelper.DoTrace( 7, string.Format( "         NOTE Credential Map Duration: {0:N2} seconds for Entity_AssertionManager.GetAllCombinedForTarget mapping", saveDuration.TotalSeconds ) );
					////save to compare
					//var oldMethod = output.OwningOrganizationQAReceived;
					//started2 = DateTime.Now;
					//output.OwningOrganizationQAReceived = orgRoleManager.GetAllCombinedForTarget( 2, output.OwningOrganizationId, output.OwningOrganizationId, true );
					output.OwningOrganizationQAReceived = orgRoleManager.GetAllCombinedForTarget( 2, output.OwningOrganizationId, output.OwningOrganizationId,true  );
					//saveDuration = DateTime.Now.Subtract( started2 );
					//LoggingHelper.DoTrace( 7, string.Format( "         NOTE Map Duration: {0:N2} seconds for NEW approach for mapping roles. ", saveDuration.TotalSeconds ) );

				}

				if ( !hasPrimaryOrg )
				{
					//temp set owning org to first offered by?
					if( output.OrganizationRole != null && output.OrganizationRole.Any())
					{
						int cntr = 0;
						foreach (var item in output.OrganizationRole )
						{
							var exists = item.AgentRole.Items.Where( x => x.Id == 7 ).ToList();
							if (exists != null && exists.Any())
							{
								output.PrimaryAgentUID = item.ActingAgentUid;
								output.PrimaryOrganization = OrganizationManager.GetForSummary( output.PrimaryAgentUID );
								output.OwningOrgDisplay = output.PrimaryOrganization.Name;
								//??
								//output.OwnerRoles = item.AgentRole;
								//output.OrganizationRole.Add( new MPM.OrganizationRoleProfile()
								//{
								//	ActingAgent = item.ActingAgent,
								//	AgentRole = new Enumeration() { Id = 7, SchemaName = "ownedBy", }
								//} );
								break;
							}								
							cntr++;
						}
					}
				}

				//
				duration = DateTime.Now.Subtract( started );
				//if ( saveDuration.TotalSeconds > 1 )
				LoggingHelper.DoTrace( 7, string.Format( "         Map Duration: {0:N2} seconds after roles and actions for {1}", duration.TotalSeconds, input.Name ) );
				started = DateTime.Now;
			}
			//
			//properties ===========================================
			try
            {
                //**TODO VersionIdentifier - need output change output a list of IdentifierValue
                //output.VersionIdentifier = input.Version;
                //assumes only one identifier type per class
                output.VersionIdentifier = Entity_IdentifierValueManager.GetAll( output.RowId, Entity_IdentifierValueManager.IdentifierValue_VersionIdentifier );

                if ( cr.IncludingEstimatedCosts )
                {
                    //output.EstimatedCosts = CostProfileManager.GetAll( output.RowId, cr.IsForEditView );
                    output.EstimatedCosts = CostProfileManager.GetAll( output.RowId );

                    //Include currencies output fix null errors in various places (detail, compare, publishing) - NA 3/17/2017
                    var currencies = CodesManager.GetCurrencies();
                    //Include cost types output fix other null errors - NA 3/17/2017
                    var costTypes = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_CREDENTIAL_ATTAINMENT_COST );
                    foreach ( var cost in output.EstimatedCosts )
                    {
                        cost.CurrencyTypes = currencies;

                        foreach ( var costItem in cost.Items )
                        {
                            costItem.DirectCostType.Items.Add( costTypes.Items.FirstOrDefault( m => m.CodeId == costItem.CostTypeId ) );
                        }
                    }
                    //End edits - NA 3/17/2017
                }
				//TODO - should all nested services be returned? Probably true, as is the intent of the nesting. But should also be able to indicate the hierarchy
				//also should probably be include the accommodations and service types. Or add tags to the TopLevelObject
                output.HasSupportService = Entity_HasSupportServiceManager.GetAllSummary( relatedEntity );


                //just in case
                if ( output.EstimatedCosts == null )
                    output.EstimatedCosts = new List<MPM.CostProfile>();

                //profiles ==========================================
                //output.FinancialAssistanceOLD = Entity_FinancialAlignmentProfileManager.GetAll( output.RowId );
				output.FinancialAssistance = Entity_FinancialAssistanceProfileManager.GetAll( output.RowId, false );

				//
				duration = DateTime.Now.Subtract( started );
				LoggingHelper.DoTrace( 7, string.Format( "         Map Duration: {0:N2} seconds section 3 for {1}", duration.TotalSeconds, input.Name ) );
				started = DateTime.Now;
				if ( cr.IncludingAddresses )
                    output.Addresses = Entity_AddressManager.GetAll( output.RowId );

                if ( cr.IncludingDuration )
                    output.EstimatedDuration = DurationProfileManager.GetAll( output.RowId );

                    output.RenewalFrequency = DurationProfileManager.GetRenewalDuration( output.RowId );
                
                if ( cr.IncludingFrameworkItems )
                {
					//New
					//can we get all and then split
					var rfi = ReferenceFrameworkItemsManager.FillCredentialAlignmentObject( output.RowId );
					if ( rfi != null && rfi.Any() )
					{
						output.OccupationTypes = rfi.Where( r => r.CategoryId == CodesManager.PROPERTY_CATEGORY_SOC ).ToList();
						output.IndustryTypes = rfi.Where( r => r.CategoryId == CodesManager.PROPERTY_CATEGORY_NAICS ).ToList();
						output.InstructionalProgramTypes = rfi.Where( r => r.CategoryId == CodesManager.PROPERTY_CATEGORY_CIP ).ToList();
					}
					//output.OccupationTypes = ReferenceFrameworkItemsManager.FillCredentialAlignmentObject( output.RowId, CodesManager.PROPERTY_CATEGORY_SOC );
					//output.IndustryTypes = ReferenceFrameworkItemsManager.FillCredentialAlignmentObject( output.RowId, CodesManager.PROPERTY_CATEGORY_NAICS );
					//output.InstructionalProgramTypes = ReferenceFrameworkItemsManager.FillCredentialAlignmentObject( output.RowId, CodesManager.PROPERTY_CATEGORY_CIP );

					//Old - only used by old detail page
					output.OccupationType = ReferenceFrameworkItemsManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_SOC );
					output.IndustryType = ReferenceFrameworkItemsManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_NAICS );
					output.InstructionalProgramType = ReferenceFrameworkItemsManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_CIP );

					//output.NavyRating = ReferenceFrameworkItemsManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_NAVY_RATING );
				}
				//
				duration = DateTime.Now.Subtract( started );
				LoggingHelper.DoTrace( 7, string.Format( "         Map Duration: {0:N2} seconds section 4 for {1}", duration.TotalSeconds, input.Name ) );
				started = DateTime.Now;
				if ( cr.IncludingConnectionProfiles )
                {
                    //get all associated top level learning opps, and assessments
                    //will always be for profile lists - not expected any where else other than edit

                    //assessment
                    //NOTE: all the target entities will be drawn input conditions!!!
                    //output.TargetCredential = Entity_CredentialManager.GetAll( output.RowId );

                    //******************get all condition profiles *******************
                    //TODO - have custom version of this output only get minimum!!
                    //NOTE - the IsForEditView relates output cred, but probably don't want output sent true output the fill
                    //re: commonConditions - consider checking if any exist, and if not, don't show

                    //need output ensure competencies are bubbled up
                    Entity_ConditionProfileManager.FillConditionProfilesForDetailDisplay(output);

					//add reversion connections to output.CredentialConnections


					//populate inverse relations - ensure don't already exists
					//21-05-16 - need to update this to handle where the relationship is to a dataset profile
					if ( input.Entity_Credential != null && input.Entity_Credential.Count > 0 )
					{
						foreach ( var ec in input.Entity_Credential )
						{
							if ( ec.Entity != null )
							{
								//This method needs output be enhanced output get enumerations for the credential for display on the detail page - NA 6/2/2017
								//Need output determine is when non-edit, is actually for the detail reference
								//only get where parent is a credential, ex not a condition profile
								if ( ec.Entity.EntityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL )
								{
									//should be checking relationshiptype here - getting 1 and 2, so duplicates
									if ( ec.RelationshipTypeId == BaseFactory.RELATIONSHIP_TYPE_IS_PART_OF )
									{
										var c = GetBasic( ec.Entity.EntityUid );
										//why is this done twice??
										if ( c != null && c.Id > 0 && c.EntityStateId > 1 )
										{
											//output.IsPartOf.Add( GetBasic( ec.Entity.EntityUid ) );
											var exists = output.IsPartOf.Where( s => s.Id == c.Id ).ToList();
											if ( exists == null || !exists.Any() )
												output.IsPartOf.Add( c );
										}
									}
								}
								else if ( ec.Entity.EntityTypeId == CodesManager.ENTITY_TYPE_CONNECTION_PROFILE )
								{
									//may want to first populate all and then do something equivalent to 'ConditionManifestExpanded.DisambiguateConditionProfiles'
									if ( cr.IsForAPIRequest )
									{
										var cp = Entity_ConditionProfileManager.GetAs_IsPartOf( ec.Entity.EntityUid, output.Name, output.CredentialConnections );

										output.IsPartOfConditionProfile.Add( cp );
										//now done in GetAs_IsPartOf
										//output.CredentialConnections.Add( cp );
										//need output check cond prof for parent of credential
										//will need output ensure no dups, or realistically, don't do the direct credential check
										if ( cp.ParentCredential != null && cp.ParentCredential.Id > 0 )
										{
											if ( cp.ParentCredential.EntityStateId > 1 )
											{
												//21-09-08 mp - not clear if this is needed anymore - hold over from publisher?
												//may not need this for API - but could be useful to separate from the condition profile,but would need the connection type
												//AddCredentialReference( cp.ParentCredential.Id, output );
											}
										}
									}
								}
								else if ( ec.Entity.EntityTypeId == CodesManager.ENTITY_TYPE_DATASET_PROFILE )
								{
									//so should these be kept separate? Need a means to not show if already referenced thru an AggregateDataProfile
                                    var dsp = DataSetProfileManager.Get( ( int ) ec.Entity.EntityBaseId, true, cr.IsForAPIRequest );
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
								else if ( ec.Entity.EntityTypeId == CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE )
								{
									//so should these be kept separate?
									var tvp = TransferValueProfileManager.Get( ( int )ec.Entity.EntityBaseId, false );
									if ( tvp != null && tvp.Id > 0 )
										output.HasTransferValueProfile.Add( tvp );

								}
								else if ( ec.Entity.EntityTypeId == CodesManager.ENTITY_TYPE_PATHWAY_COMPONENT )
								{
									//need to get the pathway
									var pc = PathwayComponentManager.Get( ( int )ec.Entity.EntityBaseId, 1 );
									if ( pc != null && pc.Id > 0 )
									{
										var pathway = PathwayManager.GetByCtid( pc.PathwayCTID );
										if ( pathway != null && pathway.Id > 0 && pathway.EntityStateId > 2 )
											output.TargetPathway.Add( pathway );
									}

								}
							}
						}
						if ( output.ExternalDataSetProfiles.Any() )
							LoggingHelper.DoTrace( 6, string.Format( thisClassName + ".MapFromDB. found {0} ExternalDataSetProfiles", output.ExternalDataSetProfiles.Count ) );

						//
						duration = DateTime.Now.Subtract( started );
						LoggingHelper.DoTrace( 7, string.Format( "         Map Duration: {0:N2} seconds after IS PART OF", duration.TotalSeconds ) );
						started = DateTime.Now;
						//
					}
					//TODO prototype use of Entity.HasResource
					//var r = Entity_HasResourceManager.GetAllEntityType( relatedEntity, 1 );

					//
					output.CollectionMembers = CollectionMemberManager.GetMemberOfCollections( output.CTID );
					//split out for API
					//DisambiguateConditionProfiles actually returns a split object, so why call 6 times
					var splitConnections = ConditionManifestExpanded.DisambiguateConditionProfiles( output.CredentialConnections );
					//
					output.AdvancedStandingFrom = splitConnections.AdvancedStandingFrom;
					output.IsAdvancedStandingFor = splitConnections.IsAdvancedStandingFor;
					output.IsRequiredFor = splitConnections.IsRequiredFor;
					output.IsRecommendedFor = splitConnections.IsRecommendedFor;
					output.IsPreparationFor = splitConnections.IsPreparationFor;
					output.PreparationFrom = splitConnections.PreparationFrom;

					//output.AdvancedStandingFrom = ConditionManifestExpanded.DisambiguateConditionProfiles( output.CredentialConnections ).AdvancedStandingFrom;
					//output.IsAdvancedStandingFor = ConditionManifestExpanded.DisambiguateConditionProfiles( output.CredentialConnections ).IsAdvancedStandingFor;
					//output.IsRequiredFor = ConditionManifestExpanded.DisambiguateConditionProfiles( output.CredentialConnections ).IsRequiredFor;
					//output.IsRecommendedFor = ConditionManifestExpanded.DisambiguateConditionProfiles( output.CredentialConnections ).IsRecommendedFor;
					//output.IsPreparationFor = ConditionManifestExpanded.DisambiguateConditionProfiles( output.CredentialConnections ).IsPreparationFor;
					//output.PreparationFrom = ConditionManifestExpanded.DisambiguateConditionProfiles( output.CredentialConnections ).PreparationFrom;

					//only true should be published. Ensure the save only saves True
					if ( input.IsNonCredit != null && input.IsNonCredit == true )
						output.IsNonCredit = input.IsNonCredit;
					else
						output.IsNonCredit = null;
					//
					if ( output.ChildHasCompetencies )
                    {
                        if ( output.Requires != null && output.Requires.Count > 0 )
                        {
                            foreach ( var cp in output.Requires )
                            {
                                if ( cp.HasCompetencies )
                                { }
                                foreach ( var asmt in cp.TargetAssessment )
                                {
                                    foreach ( KeyValuePair<string, RegistryImport> item in asmt.FrameworkPayloads )
                                    {
                                        if ( output.FrameworkPayloads.ContainsKey(item.Key) == false )
                                            output.FrameworkPayloads.Add(item.Key, item.Value);
                                    }
                                }
                                foreach ( var lopp in cp.TargetLearningOpportunity )
                                {
                                    foreach ( KeyValuePair<string, RegistryImport> item in lopp.FrameworkPayloads )
                                    {
                                        if ( output.FrameworkPayloads.ContainsKey(item.Key) == false )
                                            output.FrameworkPayloads.Add(item.Key, item.Value);
                                    }
                                }

                            }
                        }
                        if ( output.Recommends != null && output.Recommends.Count > 0 )
                        {
                            foreach ( var cp in output.Recommends )
                            {
                                if ( cp.HasCompetencies )
                                { }
                                foreach ( var asmt in cp.TargetAssessment )
                                {
                                    foreach ( KeyValuePair<string, RegistryImport> item in asmt.FrameworkPayloads )
                                    {
                                        if ( output.FrameworkPayloads.ContainsKey(item.Key) == false )
                                            output.FrameworkPayloads.Add(item.Key, item.Value);
                                    }
                                }
                                foreach ( var lopp in cp.TargetLearningOpportunity )
                                {
                                    foreach ( KeyValuePair<string, RegistryImport> item in lopp.FrameworkPayloads )
                                    {
                                        if ( output.FrameworkPayloads.ContainsKey(item.Key) == false )
                                            output.FrameworkPayloads.Add(item.Key, item.Value);
                                    }
                                }

                            }
                        }
                    }
					duration = DateTime.Now.Subtract( started );
					if ( duration.TotalSeconds > 1 )
					{
						LoggingHelper.DoTrace( 7, string.Format( "         Map Duration: {0:N2} seconds connection profiles for {1}", duration.TotalSeconds, input.Name ) );
					}
					
					started = DateTime.Now;

					output.CommonConditions = Entity_CommonConditionManager.GetAll(output.RowId);
                    output.CommonCosts = Entity_CommonCostManager.GetAll(output.RowId);
					//add target pathways here
					//23-08-28 ah wiping out code from under Entity_Credential
					//output.TargetPathway = Entity_PathwayManager.GetAll( output.RowId );
                    //
                    output.UsesVerificationService = Entity_UsesVerificationServiceManager.GetAll( relatedEntity );
                    //
                    duration = DateTime.Now.Subtract( started );
					if ( duration.TotalSeconds > 1 )
					{
						LoggingHelper.DoTrace( 7, string.Format( "         Map Duration: {0:N2} seconds common conditions, cost, and target pathway for {1}", duration.TotalSeconds, input.Name ) );
					}
					started = DateTime.Now;
				}
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError(ex, thisClassName + string.Format(".MapFromDB.1(), Name: {0} ({1})", output.Name, output.Id));
                output.StatusMessage = FormatExceptions(ex);
            }

            if ( cr.IncludingRevocationProfiles )
            {
                output.Revocation = Entity_RevocationProfileManager.GetAll( output.RowId );
            }
			//outcomes
			//should be able to get from Entity
			//21-05-26 mp - where a data provider is not present use the credential owner - this should probably be done in the import.
			output.AggregateData = Entity_AggregateDataProfileManager.GetAll( relatedEntity, true, cr.IsForAPIRequest );
			//21-05-10 mparsons need to look for independent dataset profiles 
			//					will use About -> Entity.Credential. See previous handling of input.Entity_Credential


			//output.Holders = Entity_HoldersProfileManager.GetAll( relatedEntity, true );
			//output.Earnings = Entity_EarningsProfileManager.GetAll( relatedEntity, true );
			//output.EmploymentOutcome = Entity_EmploymentOutcomeProfileManager.GetAll( relatedEntity, true );

			//
			duration = DateTime.Now.Subtract( started );
			LoggingHelper.DoTrace( 7, string.Format( "         Map Duration: {0:N2} seconds after outcome data", duration.TotalSeconds ) );
			started = DateTime.Now;
			//
			if ( cr.IncludingJurisdiction )
            {
                output.Jurisdiction = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( output.RowId );
                //output.Region = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( output.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_RESIDENT );
            }

            try
            {
                //TODO - CredentialProcess is used in the detail pages. Should be removed and use individual profiles
				if (cr.IncludingProcessProfiles )
				{
					output.CredentialProcess = Entity_ProcessProfileManager.GetAll( output.RowId );
					foreach ( MPM.ProcessProfile item in output.CredentialProcess )
					{
						if ( item.ProcessTypeId == Entity_ProcessProfileManager.ADMIN_PROCESS_TYPE )
							output.AdministrationProcess.Add( item );
						else if ( item.ProcessTypeId == Entity_ProcessProfileManager.DEV_PROCESS_TYPE )
							output.DevelopmentProcess.Add( item );
						else if ( item.ProcessTypeId == Entity_ProcessProfileManager.MTCE_PROCESS_TYPE )
							output.MaintenanceProcess.Add( item );
						else if ( item.ProcessTypeId == Entity_ProcessProfileManager.REVIEW_PROCESS_TYPE )
							output.ReviewProcess.Add( item );
						else if ( item.ProcessTypeId == Entity_ProcessProfileManager.REVOKE_PROCESS_TYPE )
							output.RevocationProcess.Add( item );
						else if ( item.ProcessTypeId == Entity_ProcessProfileManager.APPEAL_PROCESS_TYPE )
							output.AppealProcess.Add( item );
						else if ( item.ProcessTypeId == Entity_ProcessProfileManager.COMPLAINT_PROCESS_TYPE )
							output.ComplaintProcess.Add( item );
						else
						{
							//unexpected
						}
					}
				} else
					output.ProcessProfilesSummary = Entity_ProcessProfileManager.GetAllSummary( output.RowId );

				
				//
				duration = DateTime.Now.Subtract( started );
				LoggingHelper.DoTrace( 7, string.Format( "         Map Duration: {0:N2} seconds after process profiles", duration.TotalSeconds ) );
				started = DateTime.Now;
				//
				if ( cr.IncludingEmbeddedCredentials )
                {
					//how does this distinguish embedded versus other relationships? Defaults to HasPart - others used different relationship
                    output.EmbeddedCredentials = Entity_CredentialManager.GetAll(output.RowId, BaseFactory.RELATIONSHIP_TYPE_HAS_PART );
                }

				
				//
				if ( output.CredentialTypeSchema == "ceterms:QualityAssuranceCredential" )
				{
					output.ETPLCredentials = Entity_CredentialManager.GetAllSummary( output.RowId, BaseFactory.RELATIONSHIP_TYPE_IsETPLResource );
					output.ETPLAssessments = Entity_AssessmentManager.GetAllSummary( output.RowId, BaseFactory.RELATIONSHIP_TYPE_IsETPLResource );
					output.ETPLLearningOpportunities = Entity_LearningOpportunityManager.GetAllSummary( output.RowId, BaseFactory.RELATIONSHIP_TYPE_IsETPLResource );
					if ( cr.IsForDetailView )
					{
						//may not use this???
						//foreach ( var item in output.ETPLCredentials ) 
						//{
						//	output.HasETPLMembers.Add( new MC.TopLevelObject()
						//	{ Id = item.Id, Name = item.Name, Description = item.Description, SubjectWebpage = item.SubjectWebpage, EntityType = item.CredentialTypeDisplay }
						//	);
						//}
						//foreach ( var item in output.ETPLAssessments )
						//{
						//	output.HasETPLMembers.Add( new MC.TopLevelObject()
						//	{ Id = item.Id, Name = item.Name, Description = item.Description, SubjectWebpage = item.SubjectWebpage, EntityType = "Assessment" }
						//	);
						//}
						//foreach ( var item in output.ETPLLearningOpportunities )
						//{
						//	output.HasETPLMembers.Add( new MC.TopLevelObject()
						//	{ Id = item.Id, Name = item.Name, Description = item.Description, SubjectWebpage = item.SubjectWebpage, EntityType = "LearningOpportunity" }
						//	);
						//}
					}
				}

				//
				if ( cr.IncludingSubjectsKeywords )
                {
                    if ( cr.BubblingUpSubjects )
                        output.Subject = Entity_ReferenceManager.GetAllSubjects(output.RowId);
                    else
                        output.Subject = Entity_ReferenceManager.GetAll(output.RowId, CodesManager.PROPERTY_CATEGORY_SUBJECT);

                    output.Keyword = Entity_ReferenceManager.GetAll(output.RowId, CodesManager.PROPERTY_CATEGORY_KEYWORD);
                }

                output.DegreeConcentration = Entity_ReferenceManager.GetAll(output.RowId, CodesManager.PROPERTY_CATEGORY_DEGREE_CONCENTRATION);
                output.DegreeMajor = Entity_ReferenceManager.GetAll(output.RowId, CodesManager.PROPERTY_CATEGORY_DEGREE_MAJOR);
                output.DegreeMinor = Entity_ReferenceManager.GetAll(output.RowId, CodesManager.PROPERTY_CATEGORY_DEGREE_MINOR);
				// combining actions from both instrument and object for credentials, as Instrument is always a credential
				output.RelatedAction = Entity_CredentialManager.GetRelatedActionFromInstrument( output.Id )
										.Concat( CredentialingActionManager.GetRelatedActionFromObject( output.RowId ))
										.ToList();

				//
				duration = DateTime.Now.Subtract( started );
				LoggingHelper.DoTrace( 7, string.Format( "         Map Duration: {0:N2} seconds LAST SECTION", duration.TotalSeconds ) );
				//
				duration = DateTime.Now.Subtract( allStarted );
				LoggingHelper.DoTrace( 7, string.Format( "         Map Duration: {0:N2} seconds FOR WHOLE GET ===============", duration.TotalSeconds ) );

				//
			}
			catch ( Exception ex )
            {
                LoggingHelper.LogError(ex, thisClassName + string.Format(".MapFromDB.2(), Name: {0} ({1})", output.Name, output.Id));
                output.StatusMessage = FormatExceptions(ex);
            }
        } //
		private static void AddCredentialReference( int credentialId, ThisResource to )
		{
			Credential exists = to.IsPartOfCredential.FirstOrDefault( s => s.Id == credentialId );
			//hmm  would be useful to know how connected for display purposes.
			if ( exists == null || exists.Id == 0 )
				to.IsPartOfCredential.Add( CredentialManager.GetBasic( credentialId ) );
		}

		public static void MapToDB( ThisResource input, EM.Credential output )
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
            output.Name = AssignLimitedString( input.Name, MaxResourceNameLength - 5 );
			
            output.Description = GetData( input.Description );
            output.CredentialTypeId = input.CredentialTypeId;
			output.CredentialStatusTypeId = input.CredentialStatusTypeId;

			//TODO - need output chg output use text value profile
			//import will stop populating this
			//if ( input.AlternateName != null && input.AlternateName.Count > 0 )
			//    output.AlternateName = input.AlternateName[0];

			output.CredentialId = string.IsNullOrWhiteSpace( input.CredentialId ) ? null : input.CredentialId;
            //output.CodedNotation = GetData( input.CodedNotation );
			output.ISICV4 = GetData( input.ISICV4 );

			//handle old version setting output zero
			if ( IsGuidValid( input.PrimaryAgentUID ) )
            {
				//check change of owner
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
                output.OwningAgentUid = null;
            }

			//if ( input.OwnerOrganizationRoles != null && input.OwnerOrganizationRoles.Count > 0 )
			//{
			//    //may need output do something in case was a change via the roles popup
			//}

			//TODO - expect this to have been populated as needed
			output.JsonProperties = input.JsonProperties;

			//
			//***TODO - replace with list
			//21-06-30 - done see: AddProfiles
			//output.Version = GetData( input.VersionIdentifier );
            if ( IsValidDate( input.DateEffective ) )
                output.EffectiveDate = DateTime.Parse( input.DateEffective );
            else //handle reset
                output.EffectiveDate = null;
			if ( IsValidDate( input.ExpirationDate ) )
				output.ExpirationDate = DateTime.Parse( input.ExpirationDate );
			else //handle reset
				output.ExpirationDate = null;

			//output.Url = GetUrlData( input.Url, null );
			//output.SubjectWebpage = GetUrlData( input.SubjectWebpage, null );
			output.SubjectWebpage = GetUrlData( input.SubjectWebpage, null );

            output.LatestVersionUrl = GetUrlData( input.LatestVersion, null );
            output.ReplacesVersionUrl = GetUrlData( input.PreviousVersion, null );
			output.NextVersion = GetUrlData( input.NextVersion, null );
			output.Supersedes = GetUrlData( input.Supersedes, null );
			output.SupersededBy = GetUrlData( input.SupersededBy, null );

			output.AvailabilityListing = GetUrlData( input.AvailabilityListing, null );
            output.AvailableOnlineAt = GetUrlData( input.AvailableOnlineAt, null );
            output.ImageUrl = GetUrlData( input.Image, null );
			output.InCatalog = GetUrlData( input.InCatalog );

			//only true should be published. Ensure the save only saves True
			if ( input.IsNonCredit != null && input.IsNonCredit == true )
				output.IsNonCredit = input.IsNonCredit;
			else
				output.IsNonCredit = null;
			//language is now stored:
			//

			//if ( input.InLanguageId > 0 )
			//    output.InLanguageId = input.InLanguageId;
			//else if ( !string.IsNullOrWhiteSpace( input.InLanguage ) )
			//{
			//    output.InLanguageId = CodesManager.GetLanguageId( input.InLanguage );
			//}
			//else if ( input.InLanguageCodeList != null && input.InLanguageCodeList.Count > 0 )
			//{
			//    output.InLanguageId = CodesManager.GetLanguageId( input.InLanguageCodeList[0].TextValue );
			//}
			//else
			output.InLanguageId = null;

            output.ProcessStandards = GetUrlData( input.ProcessStandards, null );
            output.ProcessStandardsDescription = input.ProcessStandardsDescription;
			if ( input.CopyrightHolders != null && input.CopyrightHolders.Any() )
			{
				if ( IsGuidValid( input.CopyrightHolders[0] ) )
					output.CopyrightHolder = input.CopyrightHolders[0];
				else
					output.CopyrightHolder = null;
			}

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
            IncludingAddresses = true;
            IncludingSubjectsKeywords = true;
            BubblingUpSubjects = true;
            IncludingEmbeddedCredentials = true;

            IncludingJurisdiction = true;
			IncludingProcessProfiles = true;
            IncludingRevocationProfiles = true;
        }
		public void IsAPIRequest()
		{
			IsForAPIRequest = true;
			IncludingProperties = true;
			IncludingEstimatedCosts = true;
			IncludingDuration = true;
			IncludingFrameworkItems = true;
			IncludingRolesAndActions = true;

			//add all conditions profiles for now - to get all costs
			IncludingConnectionProfiles = true;
			ConditionProfilesAsList = false;
			IncludingAddresses = true;
			IncludingSubjectsKeywords = true;
			BubblingUpSubjects = true;
			IncludingEmbeddedCredentials = true;

			IncludingJurisdiction = true;
			//IncludingProcessProfiles = true;
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
			//21-06-04 mp - need to include cost manifests OK ALREADY INCLUDED
            IncludingConnectionProfiles = true;
        }

        public bool IsForDetailView { get; set; }
        public bool IsForAPIRequest { get; set; }
        public bool IsForProfileLinks { get; set; }
        public bool AllowCaching { get; set; }

        public bool IncludingProperties { get; set; }

        public bool IncludingRolesAndActions { get; set; }
        public bool IncludingConnectionProfiles { get; set; }
        public bool ConditionProfilesAsList { get; set; }
		public bool IncludingProcessProfiles { get; set; }
		public bool IncludingRevocationProfiles { get; set; }
        public bool IncludingEstimatedCosts { get; set; }
        public bool IncludingDuration { get; set; }
        public bool IncludingAddresses { get; set; }
        public bool IncludingJurisdiction { get; set; }

        public bool IncludingSubjectsKeywords { get; set; }
        public bool BubblingUpSubjects { get; set; }

        //public bool IncludingKeywords{ get; set; }
        //both occupations and industries, and others for latter
        public bool IncludingFrameworkItems { get; set; }

        public bool IncludingEmbeddedCredentials { get; set; }
    }


}

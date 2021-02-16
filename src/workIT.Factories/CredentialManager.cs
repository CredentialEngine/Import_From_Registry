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
using MPM = workIT.Models.ProfileModels;
using EM = workIT.Data.Tables;
using workIT.Utilities;

using Views = workIT.Data.Views;

//using CondProfileMgr = workIT.Factories.Entity_ConditionProfileManager;
using ThisEntity = workIT.Models.Common.Credential;
using DBEntity = workIT.Data.Tables.Credential;
using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;
using workIT.Models.Elastic;
//using System.Data.Entity.Core.Metadata.Edm;

namespace workIT.Factories
{
	public class CredentialManager : BaseFactory
	{
		static string thisClassName = "Factories.CredentialManager";
		EntityManager entityMgr = new EntityManager();
		#region Credential - presistance =======================

		/// <summary>
		/// Save a credential - only from import
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public bool Save( ThisEntity entity, ref SaveStatus status )
		{
			LoggingHelper.DoTrace( 7, string.Format( "CredentialManager.Save Entered. Name: {0}, CTID: {1}", entity.Name, entity.CTID ) );
			bool isValid = true;
			int count = 0;

			//NOTE - need to properly set entity.EntityStateId

			try
			{
				using ( var context = new EntityContext() )
				{
					//note for import, may still do updates?
					if ( ValidateProfile( entity, ref status ) == false )
					{//always want to complete import may want to log errors though
						//return false;
					}
					//getting duplicates somehow
					//second one seems less full featured, so could compare dates
					if ( entity.Id > 0 )
					{
						DBEntity efEntity = context.Credential
								.FirstOrDefault( s => s.Id == entity.Id );
						if ( efEntity != null && efEntity.Id > 0 )
						{
							//delete the entity and re-add
							//18-09-06 MP   - changing to not do the delete.
							//              - validate, and then replicate to other imports
							//Entity e = new Entity()
							//{
							//    EntityBaseId = efEntity.Id,
							//    EntityTypeId = CodesManager.ENTITY_TYPE_CREDENTIAL,
							//    EntityType = "Credential",
							//    EntityUid = efEntity.RowId,
							//    EntityBaseName = efEntity.Name
							//};
							//if ( !entityMgr.ResetEntity( e, ref statusMessage ) )
							//{
							//    //unexpected issue. We could get duplicate entity children if we continue?
							//}

							//for updates, chances are some fields will not be in interface, don't map these (ie created stuff)
							//**ensure rowId is passed down for use by profiles, etc
							entity.RowId = efEntity.RowId;

							MapToDB( entity, efEntity );
							//assume and validate, that if we get here we have a full record
							//not clear if we will want to update a base reference. 
							//==> should happen automatically if full record matches a SWP?
							//may be iffy
							//19-05-21 mp - should add a check for an update where currently is deleted
							if ( ( efEntity.EntityStateId ?? 0 ) == 0 )
							{
								var url = string.Format( UtilityManager.GetAppKeyValue( "credentialFinderSite" ) + "credential/{0}", efEntity.Id );
								//notify, and???
								//EmailManager.NotifyAdmin( "Previously Deleted Credential has been reactivated", string.Format("<a href='{2}'>Credential: {0} ({1})</a> was deleted and has now been reactivated.", efEntity.Name, efEntity.Id, url ));
								SiteActivity sa = new SiteActivity()
								{
									ActivityType = "Credential",
									Activity = "Import",
									Event = "Reactivate",
									Comment = string.Format( "Credential had been marked as deleted, and was reactivted by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ),
									ActivityObjectId = entity.Id
								};
								new ActivityManager().SiteActivityAdd( sa );
							}

							if ( ( efEntity.EntityStateId ?? 1 ) != 2 )
								efEntity.EntityStateId = 3;

							//need to do the date check here, or may not be updated
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
									status.AddError( string.Format( "Error - the update was not successful for credential: {0}, Id: {1}. But no reason is present.", entity.Name, entity.Id ) );
									isValid = false;
									string message = string.Format( thisClassName + ". Save Failed", "Attempted to update a credential. The process appeared to not work, but was not an exception, so we have no message, or no clue. credential: {0}, Id: {1}", entity.Name, entity.Id );
									EmailManager.NotifyAdmin( thisClassName + ". Save Failed", message );
								}
							}
							else
							{

							}

							//continue with parts only if valid 
							bool partsUpdateIsValid = true;
							if ( isValid )
							{
								if ( !UpdateParts( entity, false, ref status ) )
									partsUpdateIsValid = false;

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

							//this should use the Local date from status
							//if setting Entity.LastUpdated to the registry date, then should remove triggers to update the latter!
							//	AND shouldn't this be: efEntity.LastUpdated
							if ( isValid || partsUpdateIsValid )
								new EntityManager().UpdateModifiedDate( entity.RowId, ref status, efEntity.LastUpdated );
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
				string message = HandleDBValidationError( dbex, thisClassName + string.Format( ".Save. id: {0}, Name: {1}", entity.Id, entity.Name ), "Credential" );
				status.AddError( thisClassName + ".Save(). Error - the save was not successful. DbEntityValidationException. " + message );
				isValid = false;
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

					efEntity.EntityStateId = 3;
					context.Credential.Add( efEntity );

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
						new EntityManager().UpdateModifiedDate( entity.RowId, ref status, efEntity.LastUpdated );
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

					status.AddError( thisClassName + string.Format( ".Add(). Error - the add was not successful. DbEntityValidationException. ", entity.Name, entity.OwningAgentUid ) + message );
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Add(), Name: {0}, OwningAgentUid: {1}", efEntity.Name, efEntity.OwningAgentUid ) );
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
					CodeItem ci = CodesManager.Codes_PropertyValue_GetBySchema( CodesManager.PROPERTY_CATEGORY_CREDENTIAL_TYPE, entity.CredentialTypeSchema );
					if ( ci == null || ci.Id < 1 )
					{
						status.AddError( string.Format( "A valid Credential Type must be included. Name: {0}, Invalid: Credential Type: {1}, ", entity.Name, entity.CredentialTypeSchema ) ); //adding anyway
					}
					else
						efEntity.CredentialTypeId = ci.Id;

					if ( IsValidGuid( entity.RowId ) )
						efEntity.RowId = entity.RowId;
					else
						efEntity.RowId = Guid.NewGuid();
					//set to return, just in case
					entity.RowId = efEntity.RowId;
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

		public int UpdateBaseReferenceCredentialType( ThisEntity entity, ref SaveStatus status )
		{
			DBEntity efEntity = new DBEntity();
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

		public bool UpdateJson( int credentialId, string json )
		{
			//SaveStatus status
			bool isValid = true;
			int count = 0;
			try
			{
				using ( var context = new EntityContext() )
				{
					DBEntity efEntity = context.Credential
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
								LoggingHelper.LogError( string.Format( "Error - the Json update was not successful for credential: {0}, Id: {1}. But no reason is present.", efEntity.Name, efEntity.Id ), false );
								isValid = false;
							}
						}
					}
					else
					{
						LoggingHelper.LogError( string.Format( "Error - UpdateJson failed, as record was not found. recordId: {0}", credentialId ), false );
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
			if ( !IsUrlValid( profile.NextVersion, ref commonStatusMessage ) )
			{
				status.AddWarning( "The 'Next Version' URL format is invalid. " + commonStatusMessage );
			}
			if ( !IsUrlValid( profile.SupersededBy, ref commonStatusMessage ) )
			{
				status.AddWarning( "The 'SupersededBy' URL format is invalid. " + commonStatusMessage );
			}
			if ( !IsUrlValid( profile.Supersedes, ref commonStatusMessage ) )
			{
				status.AddWarning( "The 'Supersedes' URL format is invalid. " + commonStatusMessage );
			}

			if ( !IsUrlValid( profile.ImageUrl, ref commonStatusMessage ) )
			{
				status.AddWarning( "The Image Url is invalid. " + commonStatusMessage );
			}

			return status.WasSectionValid;
		}


		public bool UpdateParts( ThisEntity entity, bool isAdd, ref SaveStatus status )
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

			AddProfiles( entity, relatedEntity, ref status );

			UpdateAssertedBys( entity, ref status );

			UpdateAssertedIns( entity, ref status );
			//outcomes
			HandleOutcomeProfiles( entity, relatedEntity, ref status );

			return isAllValid;
		}
		public void AddProfiles( ThisEntity entity, Entity relatedEntity, ref SaveStatus status )
		{
			//DurationProfile (do delete in SaveList)
			DurationProfileManager dpm = new Factories.DurationProfileManager();
			dpm.SaveList( entity.EstimatedDuration, entity.RowId, ref status );
			//rename this method!!!
			dpm.SaveRenewalFrequency( entity.RenewalFrequency, entity.RowId, ref status );

			//Identifiers - do delete for first one and then assign
			//VersionIdentifier (do delete in SaveList)
			new Entity_IdentifierValueManager().SaveList( entity.VersionIdentifierList, entity.RowId, Entity_IdentifierValueManager.CREDENTIAL_VersionIdentifier, ref status, true );
			//skip delete - all the more reason to just store the json
			new Entity_IdentifierValueManager().SaveList( entity.Identifier, entity.RowId, Entity_IdentifierValueManager.CREDENTIAL_Identifier, ref status, false );

			//CostProfile (do delete in SaveList)
			CostProfileManager cpm = new Factories.CostProfileManager();
			cpm.SaveList( entity.EstimatedCosts, entity.RowId, ref status );

			//ConditionProfile 
			Entity_ConditionProfileManager emanager = new Entity_ConditionProfileManager();
			//arbitrarily delete all. 
			//20-12-28 mp - there have been deadlock issues 
			//20-12-28 - skip delete all from credential, etc. Rather checking  in save

			//emanager.DeleteAll( relatedEntity, ref status );
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
			ppm.DeleteAll( relatedEntity, ref status );
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
			//WARNING - CONSIDER CHANGING TO REPLACE, NOW THAT THERE ARE MULTIPLE ROUTES TO Entity.Credential
			ecm.DeleteAll( relatedEntity, ref status );
			//has parts
			if ( entity.HasPartIds != null && entity.HasPartIds.Count > 0 )
			{
				ecm.SaveHasPartList( entity.HasPartIds, relatedEntity.EntityUid, ref status );
			}
			//isPartOf - have to watch for duplicates here (where the other side added a hasPart
			if ( entity.IsPartOfIds != null && entity.IsPartOfIds.Count > 0 )
			{
				ecm.SaveIsPartOfList( entity.IsPartOfIds, entity.Id, ref status );
			}
			//ETPL
			HandleETPL( entity, relatedEntity, ref status );

			//Financial Alignment  (do delete in SaveList)
			//Entity_FinancialAlignmentProfileManager fapm = new Factories.Entity_FinancialAlignmentProfileManager();
			//fapm.SaveList( entity.FinancialAssistanceOLD, entity.RowId, ref status );

			new Entity_FinancialAssistanceProfileManager().SaveList( entity.FinancialAssistance, entity.RowId, ref status );

			//Revocation Profile (do delete in SaveList)
			Entity_RevocationProfileManager rpm = new Entity_RevocationProfileManager();
			rpm.SaveList( entity.Revocation, entity, ref status );

			//addresses (do delete in SaveList)
			new Entity_AddressManager().SaveList( entity.Addresses, entity.RowId, ref status );

			//JurisdictionProfile 
			Entity_JurisdictionProfileManager jpm = new Entity_JurisdictionProfileManager();
			//do deletes - NOTE: other jurisdictions are added in: UpdateAssertedIns
			jpm.DeleteAll( relatedEntity, ref status );
			jpm.SaveList( entity.Jurisdiction, entity.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_SCOPE, ref status );

			// (do delete in SaveList)
			new Entity_CommonConditionManager().SaveList( entity.ConditionManifestIds, entity.RowId, ref status );
			new Entity_CommonCostManager().SaveList( entity.CostManifestIds, entity.RowId, ref status );
		}

		public bool HandleETPL( ThisEntity entity, Entity relatedEntity, ref SaveStatus status )
		{
			status.HasSectionErrors = false;

			Entity_CredentialManager ecm = new Entity_CredentialManager();
			ecm.SaveHasPartList( entity.HasETPLCredentialsIds, relatedEntity.EntityUid, ref status, CodesManager.RELATIONSHIP_TYPE_IsETPLResource );

			new Entity_AssessmentManager().SaveList( entity.HasETPLAssessmentsIds, relatedEntity.EntityUid, ref status, CodesManager.RELATIONSHIP_TYPE_IsETPLResource );

			new Entity_LearningOpportunityManager().SaveList( entity.HasETPLLoppsIds, relatedEntity.EntityUid, ref status, CodesManager.RELATIONSHIP_TYPE_IsETPLResource );

			return status.WasSectionValid;
		}
		public bool HandleOutcomeProfiles( ThisEntity entity, Entity relatedEntity, ref SaveStatus status )
		{
			status.HasSectionErrors = false;

			HoldersProfileManager ecm = new HoldersProfileManager();
			if ( ecm.SaveList( entity.HoldersProfile, relatedEntity, ref status ) == false )
				status.HasSectionErrors = true;
			//Earnings profile
			var eapMgr = new EarningsProfileManager();
			if ( eapMgr.SaveList( entity.EarningsProfile, relatedEntity, ref status ) == false )
				status.HasSectionErrors = true;
			//.TODO add employment outcome
			var eoMgr = new EmploymentOutcomeProfileManager();
			if ( eoMgr.SaveList( entity.EmploymentOutcomeProfile, relatedEntity, ref status ) == false )
				status.HasSectionErrors = true;

			return status.WasSectionValid;
		}
		public bool AddProperties( ThisEntity entity, Entity relatedEntity, ref SaveStatus status )
		{
			bool isAllValid = true;

			//============================
			EntityPropertyManager mgr = new EntityPropertyManager();
			//first clear all propertiesd
			mgr.DeleteAll( relatedEntity, ref status );

			if ( mgr.AddProperties( entity.AudienceLevelType, entity.RowId, CodesManager.ENTITY_TYPE_CREDENTIAL, CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL, false, ref status ) == false )
				isAllValid = false;

			if ( mgr.AddProperties( entity.AudienceType, entity.RowId, CodesManager.ENTITY_TYPE_CREDENTIAL, CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE, false, ref status ) == false )
				isAllValid = false;

			if ( mgr.AddProperties( entity.CredentialStatusType, entity.RowId, CodesManager.ENTITY_TYPE_CREDENTIAL, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_STATUS_TYPE, false, ref status ) == false )
				isAllValid = false;

			if ( mgr.AddProperties( entity.AssessmentDeliveryType, entity.RowId, CodesManager.ENTITY_TYPE_CREDENTIAL, CodesManager.PROPERTY_CATEGORY_ASMT_DELIVERY_TYPE, false, ref status ) == false )
				isAllValid = false;
			if ( mgr.AddProperties( entity.LearningDeliveryType, entity.RowId, CodesManager.ENTITY_TYPE_CREDENTIAL, CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE, false, ref status ) == false )
				isAllValid = false;
			//
			return isAllValid;
		}
		public bool UpdateReferences( ThisEntity entity, Entity relatedEntity, ref SaveStatus status )
		{
			bool isAllValid = true;
			Entity_ReferenceManager erm = new Entity_ReferenceManager();

			Entity_ReferenceFrameworkManager erfm = new Entity_ReferenceFrameworkManager();

			//do deletes
			erm.DeleteAll( relatedEntity, ref status );
			erfm.DeleteAll( relatedEntity, ref status );

			if ( erfm.SaveList( relatedEntity.Id, CodesManager.PROPERTY_CATEGORY_SOC, entity.Occupations, ref status ) == false )
				isAllValid = false;
			if ( erfm.SaveList( relatedEntity.Id, CodesManager.PROPERTY_CATEGORY_NAICS, entity.Industries, ref status ) == false )
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
			//do deletes - should this be done here, should be no other prior updates?
			mgr.DeleteAll( parent, ref status );

			mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_AccreditedBy, entity.AccreditedBy, ref status );
			mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_ApprovedBy, entity.ApprovedBy, ref status );
			mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_OFFERED_BY, entity.OfferedBy, ref status );
			mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_OWNER, entity.OwnedBy, ref status );
			mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_RecognizedBy, entity.RecognizedBy, ref status );
			mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_RegulatedBy, entity.RegulatedBy, ref status );
			mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_RevokedBy, entity.RevokedBy, ref status );
			mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_RenewedBy, entity.RenewedBy, ref status );
			//
			mgr.SaveList( parent.Id, Entity_AgentRelationshipManager.ROLE_TYPE_PUBLISHEDBY, entity.PublishedBy, ref status );
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
						new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_ORGANIZATION, orgId, 1, ref messages );
						//also check for any relationships
						new Entity_AgentRelationshipManager().ReindexAgentForDeletedArtifact( orgUid );
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
					DBEntity efEntity = context.Credential
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

						//if ( efEntity.OwningAgentUid.ToString().ToLower() == "ce-1abb6c52-0f8c-4b17-9f89-7e9807673106" )
						//{
						//    string msg2 = string.Format( "Request to delete Ivy Tech was encountered - ignoring. Organization. Id: {0}, Name: {1}, Ctid: {2}, EnvelopeId: {3}", efEntity.Id, efEntity.Name, efEntity.CTID, envelopeId );
						//    EmailManager.NotifyAdmin( "Encountered Request to delete Ivy Tech", msg2 );
						//    return true;
						//}
						//if ( efEntity.CTID.ToLower() == "ce-6686c066-0948-4c0b-8fb0-43c6af625a70" )
						//{
						//    string msg2 = string.Format( "Request to delete NIMS was encountered - ignoring. Organization. Id: {0}, Name: {1}, Ctid: {2}, EnvelopeId: {3}", efEntity.Id, efEntity.Name, efEntity.CTID, envelopeId );
						//    EmailManager.NotifyAdmin( "Encountered Request to delete NIMS", msg2 );
						//    return true;
						//}
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
						if (!new Entity_HoldersProfileManager().DeleteAll( efEntity.RowId, ref status ))
						{

						}
						if ( !new Entity_EarningsProfileManager().DeleteAll( efEntity.RowId, ref status ) )
						{

						}
						if ( !new Entity_EmploymentOutcomeProfileManager().DeleteAll( efEntity.RowId, ref status ) )
						{

						}
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
								Comment = msg
								//ActivityObjectId = id //although could be good as a reference for db lookup
							} );
							isValid = true;
							//add pending request 
							List<String> messages = new List<string>();

							new SearchPendingReindexManager().AddDeleteRequest( CodesManager.ENTITY_TYPE_CREDENTIAL, efEntity.Id, ref messages );
							//mark owning org for updates
							new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_ORGANIZATION, orgId, 1, ref messages );

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
			ThisEntity output = new ThisEntity();
			using ( var context = new EntityContext() )
			{
				DBEntity input = context.Credential
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

					output.ImageUrl = input.ImageUrl;
					output.CTID = input.CTID;
					output.CredentialRegistryId = input.CredentialRegistryId;
					//get this for use by import and preserving published by
					if ( IsGuidValid( input.OwningAgentUid ) )
					{
						output.OwningAgentUid = ( Guid )input.OwningAgentUid;
						output.OwningOrganization = OrganizationManager.GetForSummary( output.OwningAgentUid );

						//get roles
						MPM.OrganizationRoleProfile orp = Entity_AgentRelationshipManager.AgentEntityRole_GetAsEnumerationFromCSV( output.RowId, output.OwningAgentUid );
						output.OwnerRoles = orp.AgentRole;
					}
					output.OrganizationRole = Entity_AssertionManager.GetAllCombinedForTarget( 1, output.Id, output.OwningOrganizationId );

				}
			}

			return output;
		}
		public static ThisEntity GetBySubjectWebpage( string swp )
		{
			ThisEntity entity = new ThisEntity();
			using ( var context = new EntityContext() )
			{
				context.Configuration.LazyLoadingEnabled = false;
				DBEntity from = context.Credential
						.FirstOrDefault( s => s.SubjectWebpage.ToLower() == swp.ToLower() );

				if ( from != null && from.Id > 0 )
				{
					entity.RowId = from.RowId;
					entity.Id = from.Id;
					entity.Name = from.Name;
					entity.EntityStateId = ( int )( from.EntityStateId ?? 1 );
					entity.Description = from.Description;
					entity.SubjectWebpage = from.SubjectWebpage;
					entity.CredentialTypeId = from.CredentialTypeId ?? 0;

					entity.ImageUrl = from.ImageUrl;
					entity.CTID = from.CTID;
					entity.CredentialRegistryId = from.CredentialRegistryId;
				}
			}
			return entity;
		}
		public static ThisEntity GetByName_SubjectWebpage( string name, string swp )
		{
			ThisEntity entity = new ThisEntity();
			CredentialRequest cr = new CredentialRequest();
			cr.IsDetailRequest();

			using ( var context = new EntityContext() )
			{
				context.Configuration.LazyLoadingEnabled = false;
				DBEntity from = context.Credential
						.FirstOrDefault( s => s.Name.ToLower() == name.ToLower() && s.SubjectWebpage == swp );

				if ( from != null && from.Id > 0 )
				{
					MapFromDB( from, entity, cr );
					//entity.RowId = from.RowId;
					//entity.Id = from.Id;
					//entity.Name = from.Name;
					//entity.EntityStateId = ( int )( from.EntityStateId ?? 1 );
					//entity.Description = from.Description;
					//entity.SubjectWebpage = from.SubjectWebpage;
					//entity.CredentialTypeId = from.CredentialTypeId ?? 0;

					//entity.ImageUrl = from.ImageUrl;
					//entity.CTID = from.CTID;
					//entity.CredentialRegistryId = from.CredentialRegistryId;
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
				EM.Credential item = context.Credential
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

		public static CM.Credential GetBasicWithConditions( Guid rowId )
		{
			CM.Credential entity = new CM.Credential();
			CredentialRequest cr = new CredentialRequest();
			cr.IsForProfileLinks = true;

			using ( var context = new EntityContext() )
			{
				if ( cr.IsForProfileLinks ) //get minimum
					context.Configuration.LazyLoadingEnabled = false;

				EM.Credential item = context.Credential
							.SingleOrDefault( s => s.RowId == rowId
								);

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity, cr );

				}
			}

			return entity;
		}

		public static CM.Credential GetForDetail( int id )
		{
			CredentialRequest cr = new CredentialRequest();
			cr.IsDetailRequest();
			return GetForDetail( id, cr );
		}

		public static CM.Credential GetForDetail( int id, CredentialRequest cr )
		{
			CM.Credential entity = new CM.Credential();

			using ( var context = new EntityContext() )
			{

				//context.Configuration.LazyLoadingEnabled = false;
				EM.Credential item = context.Credential
							.SingleOrDefault( s => s.Id == id );
				try
				{
					if ( item != null && item.Id > 0 )
					{
						//check for virtual deletes
						if ( item.EntityStateId == 0 )
							return entity;

						MapFromDB( item, entity, cr );
						//get summary for some totals
						//EM.Credential_SummaryCache cache = GetSummary( item.Id );
						//if ( cache != null && cache.BadgeClaimsCount > 0 )
						if ( HasBadgeClaims( item.Id ) )
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
		public static bool HasBadgeClaims( int credentialId )
		{

			try
			{
				using ( var context = new EntityContext() )
				{
					var claims = ( from a in context.Entity_VerificationProfile
								   join c in context.Entity on a.RowId equals c.EntityUid
								   join d in context.Entity_Credential on c.Id equals d.EntityId
								   join e in context.Entity_Property on c.Id equals e.EntityId
								   join f in context.Codes_PropertyValue on e.PropertyValueId equals f.Id
								   where f.SchemaName == "claimType:BadgeClaim"
								   && d.CredentialId == credentialId
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
			List<ThisEntity> output = new List<ThisEntity>();
			ThisEntity entity = new ThisEntity();
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
		public static List<Credential> GetAllForOwningOrg( Guid ownedByUid )
		{
			List<ThisEntity> output = new List<ThisEntity>();
			ThisEntity entity = new ThisEntity();
			CredentialRequest cr = new CredentialRequest();
			cr.IsForProfileLinks = true;
			using ( var context = new EntityContext() )
			{

				List<EM.Credential> list = context.Credential
							.Where( s => s.EntityStateId == 3
								&& s.OwningAgentUid == ownedByUid )
							.OrderBy( s => s.CTID )
							.ToList();

				if ( list != null && list.Count > 0 )
				{
					foreach ( var item in list )
					{
						//there is very little data in a pending record
						entity = new ThisEntity();
						MapFromDB( item, entity, cr );
						output.Add( entity );
					}
				}
			}

			return output;
		}
		public static List<object> AutocompleteInternal( string keyword, int pageNumber, int pageSize, ref int pTotalRows )
		{
			List<object> output = new List<object>();
			keyword = ( keyword ?? "" ).ToLower();
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
						output.AddRange( results.Select( s => s.Name ).Distinct().Take( pageSize ).ToList() );
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

		public static List<object> AutocompleteDB( string pFilter, int pageNumber, int pageSize, ref int pTotalRows )
		{
			bool autocomplete = true;
			List<object> output = new List<object>();

			List<CM.CredentialSummary> list = Search( pFilter, "", pageNumber, pageSize, ref pTotalRows, autocomplete );
			bool appendingOrgNameToAutocomplete = UtilityManager.GetAppKeyValue( "appendingOrgNameToAutocomplete", false );
			string prevName = "";
			if ( !appendingOrgNameToAutocomplete )
			{
				//note excluding duplicates may have an impact on selected max terms
				output.AddRange( list.Select( s => s.Name ).Distinct().Take( pageSize ).ToList() );
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
			string orderBy = "";
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
						if ( !existanceSearch )
						{
							item = new CM.CredentialSummary();
							item.Name = "EXCEPTION ENCOUNTERED";
							item.Description = ex.Message;
							item.CredentialTypeSchema = "error";
							list.Add( item );
						}
                        return list;

                    }
                }

                //Used for costs. Only need to get these once. See below. - NA 5/12/2017
                var currencies = CodesManager.GetCurrencies();
                var costTypes = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_CREDENTIAL_ATTAINMENT_COST );
				if ( existanceSearch )
				{
					//just return total rows
					return list;
				}
				foreach ( DataRow dr in result.Rows )
                {
					try
					{
						//avgMinutes = 0;
						item = new CM.CredentialSummary();
						item.Id = GetRowColumn( dr, "Id", 0 );

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
							item.OwnerOrganizationName = "";
						item.CredentialType = GetRowPossibleColumn( dr, "CredentialType", "" );

						item.CredentialTypeSchema = GetRowPossibleColumn( dr, "CredentialTypeSchema", "" );
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

						//for autocomplete, only need name
						if ( autocomplete )
						{
							list.Add( item );
							continue;
						}
						//string rowId = GetRowColumn( dr, "RowId" );
						//string rowId = GetRowColumn( dr, "EntityUid" );
						string rowId = dr[ "EntityUid" ].ToString();
						//if ( IsGuidValid( rowId ) )
						item.RowId = new Guid( rowId );

						//item.Description = GetRowColumn( dr, "Description", "" );
						item.Description = dr[ "Description" ].ToString();

						item.Version = GetRowPossibleColumn( dr, "Version", "" );
						item.LatestVersionUrl = GetRowPossibleColumn( dr, "LatestVersionUrl", "" );
						item.PreviousVersion = GetRowPossibleColumn( dr, "PreviousVersion", "" );
						item.ProcessStandards = GetRowPossibleColumn( dr, "ProcessStandards", "" );


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

						string subjects = GetRowPossibleColumn( dr, "SubjectsList", "" );

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
						LoggingHelper.DoTrace( 1, "Credential.Search." + ex.Message );
					}
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
        public static void MapFromDB( EM.Credential input, CM.Credential output,
                    CredentialRequest cr )
        {
            output.Id = input.Id;
            output.RowId = input.RowId;
            output.EntityStateId = ( int )( input.EntityStateId ?? 1 );

            output.Name = input.Name;
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
                    output.CredentialTypeSchema = ct.SchemaName;
                }
				//retain example using an Enumeration for by other related tableS???
                output.CredentialTypeEnum = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_TYPE );
                output.CredentialTypeEnum.Items.Add( new EnumeratedItem() { Id = output.CredentialTypeId, Name = ct.Name, SchemaName = ct.SchemaName } );
            }

            if ( input.ImageUrl != null && input.ImageUrl.Trim().Length > 0 )
                output.ImageUrl = input.ImageUrl;
            else
                output.ImageUrl = null;

            if ( IsGuidValid( input.OwningAgentUid ) )
            {
                output.OwningAgentUid = ( Guid )input.OwningAgentUid;
                output.OwningOrganization = OrganizationManager.GetForSummary( output.OwningAgentUid );

				//get roles
				MPM.OrganizationRoleProfile orp = Entity_AgentRelationshipManager.AgentEntityRole_GetAsEnumerationFromCSV( output.RowId, output.OwningAgentUid );
                output.OwnerRoles = orp.AgentRole;
            }


            //
            output.OwningOrgDisplay = output.OwningOrganization.Name;

            output.AudienceLevelType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL );
            output.AudienceType = EntityPropertyManager.FillEnumeration( output.RowId,CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE );

			output.AssessmentDeliveryType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_ASMT_DELIVERY_TYPE );
			output.LearningDeliveryType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE );

			output.JsonProperties = input.JsonProperties;
			//populate related stuff:

			if ( cr.IsForProfileLinks ) //return minimum ===========
                return;
			//===================================================================
			var relatedEntity = EntityManager.GetEntity( output.RowId, false );
			if ( relatedEntity != null && relatedEntity.Id > 0 )
				output.EntityLastUpdated = relatedEntity.LastUpdated;

			if ( IsGuidValid( input.CopyrightHolder ) )
            {
                output.CopyrightHolder = ( Guid )input.CopyrightHolder;
                //not sure if we need the org for display?
                output.CopyrightHolderOrganization = OrganizationManager.GetForSummary( output.CopyrightHolder );
            }

            //will need output do convert before switching these
            output.AlternateName = Entity_ReferenceManager.GetAllToList( output.RowId, CodesManager.PROPERTY_CATEGORY_ALTERNATE_NAME );
            //if ( !string.IsNullOrWhiteSpace( input.AlternateName ) && output.AlternateName.Count == 0 )
            //    output.AlternateName.Add( input.AlternateName );
            output.AlternateNames = Entity_ReferenceManager.GetAll( output.RowId, CodesManager.PROPERTY_CATEGORY_ALTERNATE_NAME );

            output.CredentialId = input.CredentialId;
            output.CodedNotation = input.CodedNotation;
			output.ISICV4 = input.ISICV4;
			//TODO - should these be suppressed if same as subjectwebpage?
			output.AvailabilityListing = input.AvailabilityListing;
			output.AvailableOnlineAt = input.AvailableOnlineAt;

            if ( IsValidDate( input.EffectiveDate ) )
                output.DateEffective = ( ( DateTime )input.EffectiveDate ).ToShortDateString();
            else
                output.DateEffective = "";

            output.LatestVersion = input.LatestVersionUrl;
            output.PreviousVersion = input.ReplacesVersionUrl;
			output.NextVersion = input.NextVersion;
			output.Supersedes = input.Supersedes;
			output.SupersededBy = input.SupersededBy;

			output.Identifier = Entity_IdentifierValueManager.GetAll( output.RowId, Entity_IdentifierValueManager.CREDENTIAL_Identifier );


			if ( IsValidDate( input.Created ) )
                output.Created = ( DateTime )input.Created;
            if ( IsValidDate( input.LastUpdated ) )
                output.LastUpdated = ( DateTime )input.LastUpdated;

            //multiple languages, now in entity.reference
            output.InLanguageCodeList = Entity_ReferenceManager.GetAll( output.RowId, CodesManager.PROPERTY_CATEGORY_LANGUAGE );

            output.ProcessStandards = input.ProcessStandards ?? "";
            output.ProcessStandardsDescription = input.ProcessStandardsDescription ?? "";
            //ensure only one status. Previous property should have been deleted.
            output.CredentialStatusType = EntityPropertyManager.FillEnumeration(output.RowId, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_STATUS_TYPE);
            EnumeratedItem statusItem = output.CredentialStatusType.GetFirstItem();
            if ( statusItem != null && statusItem.Id > 0 && statusItem.Name != "Active" )
            {
				//if ( cr.IsForDetailView && output.Name.IndexOf( statusItem.Name ) == -1 )
				//	output.Name += string.Format( " ({0})", statusItem.Name );
			}
			//---------------
			if ( cr.IncludingRolesAndActions )
			{
				output.JurisdictionAssertions = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( output.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_OFFERREDIN );


				//get as ennumerations
				//var oldRoles = Entity_AgentRelationshipManager.AgentEntityRole_GetAll_ToEnumeration( output.RowId, true );
				//this should really be QA only, the latter (AgentEntityRole_GetAll_ToEnumeration) included owns/offers
				output.OrganizationRole = Entity_AssertionManager.GetAllCombinedForTarget( 1, output.Id, output.OwningOrganizationId );
			}

			//properties ===========================================
			try
            {
                //**TODO VersionIdentifier - need output change output a list of IdentifierValue
                output.VersionIdentifier = input.Version;
                //assumes only one identifier type per class
                output.VersionIdentifierList = Entity_IdentifierValueManager.GetAll( output.RowId, Entity_IdentifierValueManager.CREDENTIAL_VersionIdentifier );

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

            

                //just in case
                if ( output.EstimatedCosts == null )
                    output.EstimatedCosts = new List<MPM.CostProfile>();

                //profiles ==========================================
                //output.FinancialAssistanceOLD = Entity_FinancialAlignmentProfileManager.GetAll( output.RowId );
				output.FinancialAssistance = Entity_FinancialAssistanceProfileManager.GetAll( output.RowId, false );

				if ( cr.IncludingAddresses )
                    output.Addresses = Entity_AddressManager.GetAll( output.RowId );

                if ( cr.IncludingDuration )
                    output.EstimatedDuration = DurationProfileManager.GetAll( output.RowId );

                    output.RenewalFrequency = DurationProfileManager.GetRenewalDuration( output.RowId );
                
                if ( cr.IncludingFrameworkItems )
                {
                    output.Occupation = Reference_FrameworksManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_SOC );

                    output.Industry = Reference_FrameworksManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_NAICS );

					output.InstructionalProgramType = Reference_FrameworksManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_CIP );

					output.NavyRating = Reference_FrameworksManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_NAVY_RATING );
				}

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
                    output.CommonConditions = Entity_CommonConditionManager.GetAll(output.RowId);
                    output.CommonCosts = Entity_CommonCostManager.GetAll(output.RowId);


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
			output.HoldersProfile = Entity_HoldersProfileManager.GetAll( output.RowId, true );
			output.EarningsProfile = Entity_EarningsProfileManager.GetAll( output.RowId, true );
			output.EmploymentOutcomeProfile = Entity_EmploymentOutcomeProfileManager.GetAll( output.RowId, true );

			if ( cr.IncludingJurisdiction )
            {
                output.Jurisdiction = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( output.RowId );
                //output.Region = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( output.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_RESIDENT );
            }

            try
            {
                //TODO - CredentialProcess is used in the detail pages. Should be removed and use individual profiles

                output.CredentialProcess = Entity_ProcessProfileManager.GetAll(output.RowId);
                foreach ( MPM.ProcessProfile item in output.CredentialProcess )
                {
                    if ( item.ProcessTypeId == Entity_ProcessProfileManager.ADMIN_PROCESS_TYPE )
                        output.AdministrationProcess.Add(item);
                    else if ( item.ProcessTypeId == Entity_ProcessProfileManager.DEV_PROCESS_TYPE )
                        output.DevelopmentProcess.Add(item);
                    else if ( item.ProcessTypeId == Entity_ProcessProfileManager.MTCE_PROCESS_TYPE )
                        output.MaintenanceProcess.Add(item);
                    else if ( item.ProcessTypeId == Entity_ProcessProfileManager.REVIEW_PROCESS_TYPE )
                        output.ReviewProcess.Add(item);
                    else if ( item.ProcessTypeId == Entity_ProcessProfileManager.REVOKE_PROCESS_TYPE )
                        output.RevocationProcess.Add(item);
                    else if ( item.ProcessTypeId == Entity_ProcessProfileManager.APPEAL_PROCESS_TYPE )
                        output.AppealProcess.Add(item);
                    else if ( item.ProcessTypeId == Entity_ProcessProfileManager.COMPLAINT_PROCESS_TYPE )
                        output.ComplaintProcess.Add(item);
                    else
                    {
                        //unexpected
                    }
                }

                if ( cr.IncludingEmbeddedCredentials )
                {
                    output.EmbeddedCredentials = Entity_CredentialManager.GetAll(output.RowId);
                }


                //populate is part of - when??
                if ( input.Entity_Credential != null && input.Entity_Credential.Count > 0 )
                {
                    foreach ( EM.Entity_Credential ec in input.Entity_Credential )
                    {
                        if ( ec.Entity != null )
                        {
                            //This method needs output be enhanced output get enumerations for the credential for display on the detail page - NA 6/2/2017
                            //Need output determine is when non-edit, is actually for the detail reference
                            //only get where parent is a credential, ex not a condition profile
                            if ( ec.Entity.EntityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL )
                            {
                                var c = GetBasic(ec.Entity.EntityUid);
                                if ( c != null && c.Id > 0 && c.EntityStateId > 1 )
                                    output.IsPartOf.Add(GetBasic(ec.Entity.EntityUid));
                            }
                        }
                    }
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


            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError(ex, thisClassName + string.Format(".MapFromDB.2(), Name: {0} ({1})", output.Name, output.Id));
                output.StatusMessage = FormatExceptions(ex);
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
            //if ( input.AlternateName != null && input.AlternateName.Count > 0 )
            //    output.AlternateName = input.AlternateName[0];

            output.CredentialId = string.IsNullOrWhiteSpace( input.CredentialId ) ? null : input.CredentialId;
            output.CodedNotation = GetData( input.CodedNotation );
			output.ISICV4 = GetData( input.ISICV4 );

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

			//if ( input.OwnerOrganizationRoles != null && input.OwnerOrganizationRoles.Count > 0 )
			//{
			//    //may need output do something in case was a change via the roles popup
			//}

			//TODO - expect this to have been populated as needed
			output.JsonProperties = input.JsonProperties;

			//
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
			output.NextVersion = GetUrlData( input.NextVersion, null );
			output.Supersedes = GetUrlData( input.Supersedes, null );
			output.SupersededBy = GetUrlData( input.SupersededBy, null );

			output.AvailabilityListing = GetUrlData( input.AvailabilityListing, null );
            output.AvailableOnlineAt = GetUrlData( input.AvailableOnlineAt, null );
            output.ImageUrl = GetUrlData( input.ImageUrl, null );

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
            IncludingAddresses = true;
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
            IncludingAddresses = true;
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

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Newtonsoft.Json;
using workIT.Models;
using workIT.Models.Common;
using workIT.Utilities;
using DBEntity = workIT.Data.Tables.CredentialingAction;
using EntityContext = workIT.Data.Tables.workITEntities;
using ThisResource = workIT.Models.Common.CredentialingAction;

namespace workIT.Factories
{
    public class CredentialingActionManager : BaseFactory
	{
		static readonly string thisClassName = "CredentialingActionManager";
		static string EntityType = "CredentialingAction";
		static int EntityTypeId = CodesManager.ENTITY_TYPE_CREDENTIALING_ACTION;
		static string Entity_Label = "Credentialing Action";
		static string Entities_Label = "Credentialing Actions";
		#region Constants
		//may not store in separate code table? 23-11-09 MP - chg to use Codes.CredentialingActionType
		public static int AccreditActionTypeId = 1;
		public static int AdvancedstandingActionTypeId = 2;
		public static int ApproveActionTypeId = 3;
		public static int CredentialingActionTypeId = 4;
		public static int OfferActionTypeId = 5;
		public static int RecognizeActionTypeId = 6;
		public static int RegulateActionTypeId = 7;
		public static int RenewActionTypeId = 8;
		public static int RevokeActionTypeId = 9;
		public static int RightsActionTypeId = 10;
		public static int WorkForceDemandActionTypeId = 11;
		public static int RegistrationActionTypeId = 12;

		public static string AccreditActionType = "AccreditAction";
		public static string AdvancedstandingActionType = "AdvancedStandingAction";
		public static string ApproveActionType = "ApproveAction";
		public static string CredentialingActionType = "CredentialingAction";
		public static string OfferActionType = "OfferAction";
		public static string RecognizeActionType = "RecognizeAction";
		public static string RegistrationActionType = "RegistrationAction";
		public static string RegulateActionType = "RegulateAction";
		public static string RenewActionType = "RenewAction";
		public static string RevokeActionType = "RevokeAction";
		public static string RightsActionType = "RightsAction";
		public static string WorkForceDemandActionType = "WorkForceDemandAction";

		#endregion

		#region CredentialingAction - persistance ==================
		/// <summary>
		/// Update a CredentialingAction
		/// - base only, caller will handle parts?
		/// </summary>
		/// <param name="resource"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public bool Save( ThisResource resource, ref SaveStatus status )
		{
			bool isValid = true;
			int count = 0;
			try
			{
				using ( var context = new EntityContext() )
				{
					if ( ValidateProfile( resource, ref status ) == false )
						return false;

					if ( resource.Id > 0 )
					{
						//TODO - consider if necessary, or interferes with anything
						context.Configuration.LazyLoadingEnabled = false;
						DBEntity efEntity = context.CredentialingAction
								.SingleOrDefault( s => s.Id == resource.Id );

						if ( efEntity != null && efEntity.Id > 0 )
						{
							//fill in fields that may not be in entity
							resource.RowId = efEntity.RowId;

							MapToDB( resource, efEntity );

							//19-05-21 mp - should add a check for an update where currently is deleted
							if ( efEntity.EntityStateId == 0 )
							{
								SiteActivity sa = new SiteActivity()
								{
									ActivityType = EntityType,
									Activity = "Import",
									Event = "Reactivate",
									Comment = $"{Entity_Label} had been marked as deleted, and was reactivted by the import. Type: {resource.Type}, CTID: {resource.CTID}",
									ActivityObjectId = resource.Id
								};
								new ActivityManager().SiteActivityAdd( sa );
							}
							//assume and validate, that if we get here we have a full record
							if ( efEntity.EntityStateId != 2 )
								efEntity.EntityStateId = 3;
							resource.EntityStateId = efEntity.EntityStateId;

							if ( IsValidDate( status.EnvelopeCreatedDate ) && status.LocalCreatedDate < efEntity.Created )
							{
								efEntity.Created = status.LocalCreatedDate;
							}
							if ( IsValidDate( status.EnvelopeUpdatedDate ) && status.LocalUpdatedDate != efEntity.LastUpdated )
							{
								efEntity.LastUpdated = status.LocalUpdatedDate;
							}
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
									resource.LastUpdated = efEntity.LastUpdated.Value;
									UpdateEntityCache( resource, ref status );
									isValid = true;
								}
								else
								{
									//?no info on error
									isValid = false;
									string message = thisClassName + $".Save Failed. Attempted to update a CredentialingAction. The process appeared to not work, but was not an exception, so we have no message, or no clue. CredentialingAction: {resource.Type}, CTID: {resource.CTID}";
									status.AddError( "Error - the update was not successful. " + message );
									EmailManager.NotifyAdmin( thisClassName + ".Save Failed Failed", message );
								}

							}
							else
							{
								//update entity.LastUpdated - assuming there has to have been some change in related data
								new EntityManager().UpdateModifiedDate( resource.RowId, ref status, efEntity.LastUpdated );
							}
							if ( isValid )
							{
								if ( !UpdateParts( resource, ref status ) )
									isValid = false;

								SiteActivity sa = new SiteActivity()
								{
									ActivityType = EntityType,
									Activity = "Import",
									Event = "Update",
									Comment = string.Format( "CredentialingAction was updated by the import. Name: {0}, SWP: {1}", resource.Name, resource.SubjectWebpage ),
									ActivityObjectId = resource.Id
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
						int newId = Add( resource, ref status );
						if ( newId == 0 || status.HasErrors )
							isValid = false;
					}
				}
			}
			catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
			{
				string message = HandleDBValidationError( dbex, thisClassName + string.Format( ".Save. id: {0}, Name: {1}", resource.Id, resource.Name ), "CredentialingAction" );
				status.AddError( thisClassName + ".Save(). Error - the save was not successful. " + message );
			}
			catch ( Exception ex )
			{
				string message = FormatExceptions( ex );
				LoggingHelper.LogError( ex, thisClassName + string.Format( ".Save. id: {0}, Name: {1}", resource.Id, resource.Name ) );
				status.AddError( thisClassName + ".Save(). Error - the save was not successful. " + message );
				isValid = false;
			}


			return isValid;
		}

		/// <summary>
		/// add a CredentialingAction
		/// </summary>
		/// <param name="resource"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		private int Add( ThisResource resource, ref SaveStatus status )
		{
			DBEntity efEntity = new DBEntity();
			using ( var context = new EntityContext() )
			{
				try
				{
					MapToDB( resource, efEntity );

					if ( IsValidGuid( resource.RowId ) )
						efEntity.RowId = resource.RowId;
					else
						efEntity.RowId = Guid.NewGuid();
					efEntity.EntityStateId = resource.EntityStateId = 3;
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
					context.CredentialingAction.Add( efEntity );

					// submit the change to database
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						resource.RowId = efEntity.RowId;
						resource.Created = efEntity.Created.Value;
						resource.LastUpdated = efEntity.LastUpdated.Value;
						resource.Id = efEntity.Id;
						
						UpdateEntityCache( resource, ref status );
						//add log entry
						SiteActivity sa = new SiteActivity()
						{
							ActivityType = EntityType,
							Activity = "Import",
							Event = "Add",
							Comment = string.Format( "Full CredentialingAction was added by the import. Name: {0}, SWP: {1}", resource.Name, resource.SubjectWebpage ),
							ActivityObjectId = resource.Id
						};
						new ActivityManager().SiteActivityAdd( sa );
						if ( UpdateParts( resource, ref status ) == false )
						{
						}

						return efEntity.Id;
					}
					else
					{
						//?no info on error

						string message = thisClassName + $".Add Failed. Attempted to update a CredentialingAction. The process appeared to not work, but was not an exception, so we have no message, or no clue. CredentialingAction: {resource.Type}, CTID: {resource.CTID}";
						status.AddError( thisClassName + ". Error - the add was not successful. " + message );
						EmailManager.NotifyAdmin( thisClassName + ".Add Failed", message );
					}
				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", "CredentialingAction" );
					status.AddError( thisClassName + ".Add(). Error - the save was not successful. " + message );

					LoggingHelper.LogError( dbex, message );
				}
				catch ( Exception ex )
				{
					string message = FormatExceptions( ex );
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Add(), Name: {0}\r\n", efEntity.Name ) );
					status.AddError( thisClassName + ".Add(). Error - the save was not successful. \r\n" + message );
				}
			}

			return efEntity.Id;
		}
		//not sure if possible
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
					var entity = GetMinimumByCtid( ctid );
					if ( entity != null && entity.Id > 0 )
						return entity.Id;

					//only add DB required properties
					//NOTE - an entity will be created via trigger
					efEntity.Description = "Placeholder until full document is downloaded";
					efEntity.EntityStateId = 1;
					efEntity.RowId = entityUid;
					//watch that Ctid can be  updated if not provided now!!
					efEntity.CTID = ctid;

					efEntity.Created = System.DateTime.Now;
					efEntity.LastUpdated = System.DateTime.Now;

					context.CredentialingAction.Add( efEntity );
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
						entity.Id = efEntity.Id;
						entity.RowId = efEntity.RowId;
						entity.CTID = efEntity.CTID;
						entity.EntityStateId = 1;
						entity.Description = efEntity.Description;
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
				ImageUrl = document.Image,
				Name = "Credentialing Action: " + AssignLimitedString( document.Description, 500),
				OwningAgentUID = document.PrimaryAgentUID,
				OwningOrgId = document.OrganizationId
			};
			var statusMessage = string.Empty;
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
				//status.AddWarning( "An CredentialingAction Description must be entered" );
			}
			//done in import
			//if ( profile.ActionStatusType != null && profile.ActionStatusType.HasItems() )
			//{
			//	profile.ActionStatusTypeId = CodesManager.GetPropertyIdBySchema( CodesManager.PROPERTY_CATEGORY_ACTION_STATUS_TYPE, profile.ActionStatusType.Items[0].SchemaName );
			//}

			return status.WasSectionValid;
		}


		/// <summary>
		/// Delete an CredentialingAction, and related Entity
		/// </summary>
		/// <param name="id"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool Delete( int id, ref string statusMessage )
		{
			bool isValid = false;
			if ( id == 0 )
			{
				statusMessage = "Error - missing an identifier for the CredentialingAction";
				return false;
			}

			using ( var context = new EntityContext() )
			{
				try
				{
					context.Configuration.LazyLoadingEnabled = false;
					DBEntity efEntity = context.CredentialingAction
								.SingleOrDefault( s => s.Id == id );

					if ( efEntity != null && efEntity.Id > 0 )
					{

						//need to remove from Entity.
						//could use a pre-delete trigger?
						//what about roles

						context.CredentialingAction.Remove( efEntity );
						int count = context.SaveChanges();
						if ( count > 0 )
						{
							isValid = true;
							//add pending delete request 
							List<String> messages = new List<string>();
							new SearchPendingReindexManager().AddDeleteRequest( CodesManager.ENTITY_TYPE_OCCUPATIONS_PROFILE, id, ref messages );
							//
							//new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, orgId, 1, ref messages );
							//also check for any relationships
							//new Entity_AgentRelationshipManager().ReindexAgentForDeletedArtifact( orgUid );
						}
					}
					else
					{
						statusMessage = "Error - CredentialingAction_Delete failed, as record was not found.";
					}
				}
				catch ( Exception ex )
				{
					statusMessage = FormatExceptions( ex );
					LoggingHelper.LogError( ex, thisClassName + ".CredentialingAction_Delete()" );

					if ( statusMessage.ToLower().IndexOf( "the delete statement conflicted with the reference constraint" ) > -1 )
					{
						statusMessage = "Error: this CredentialingAction cannot be deleted as it is being referenced by other items, such as roles or credentials. These associations must be removed before this CredentialingAction can be deleted.";
					}
				}
			}
			return isValid;
		}

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

			using ( var context = new EntityContext() )
			{
				try
				{
					context.Configuration.LazyLoadingEnabled = false;
					DBEntity efEntity = context.CredentialingAction
								.FirstOrDefault( s => ( s.CTID == ctid )
								);

					if ( efEntity != null && efEntity.Id > 0 )
					{
						Guid rowId = efEntity.RowId;

						//need to remove from Entity.
						//-using before delete trigger - verify won't have RI issues
						string msg = string.Format( " CredentialingAction. Id: {0}, Name: {1}, Ctid: {2}.", efEntity.Id, efEntity.Name, efEntity.CTID );
						//18-04-05 mparsons - change to set inactive, and notify - seems to have been some incorrect deletes
						//context.CredentialingAction.Remove( efEntity );
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
								ActivityObjectId = efEntity.Id
							} );
							isValid = true;
							//delete cache
							new EntityManager().EntityCacheDelete( rowId, ref statusMessage );
							//add pending request 
							List<String> messages = new List<string>();
							new SearchPendingReindexManager().AddDeleteRequest( CodesManager.ENTITY_TYPE_OCCUPATIONS_PROFILE, efEntity.Id, ref messages );
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
						statusMessage = thisClassName + ".Delete() Warning No action taken, as the record was not found.";
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + ".Delete(envelopeId)" );
					isValid = false;
					statusMessage = FormatExceptions( ex );
					if ( statusMessage.ToLower().IndexOf( "the delete statement conflicted with the reference constraint" ) > -1 )
					{
						statusMessage = "Error: this CredentialingAction cannot be deleted as it is being referenced by other items, such as roles or credentials. These associations must be removed before this CredentialingAction can be deleted.";
					}
				}
			}
			return isValid;
		}

		#region CredentialingAction properties ===================
		public bool UpdateParts( ThisResource resource, ref SaveStatus status )
		{
			bool isAllValid = true;
			Entity relatedEntity = EntityManager.GetEntity( resource.RowId );
			if ( relatedEntity == null || relatedEntity.Id == 0 )
			{
				status.AddError( "Error - the related Entity was not found." );
				return false;
			}

			Entity_AgentRelationshipManager eamgr = new Entity_AgentRelationshipManager();
			//what to do with acting agent? AssertedBy
			eamgr.SaveList( relatedEntity.Id, Entity_AgentRelationshipManager.ROLE_TYPE_AssertedBy, resource.ActingAgentList, ref status ); eamgr.SaveList( relatedEntity.Id, Entity_AgentRelationshipManager.ROLE_TYPE_PUBLISHEDBY, resource.PublishedBy, ref status );
			//EntityProperty concepts
			//EntityPropertyManager epMgr = new EntityPropertyManager();
			////first clear all properties
			//epMgr.DeleteAll( relatedEntity, ref status );

			//if ( epMgr.AddProperties( resource.ActionStatusType, resource.RowId, CodesManager.ENTITY_TYPE_CREDENTIALING_ACTION, CodesManager.PROPERTY_CATEGORY_ACTION_STATUS_TYPE, false, ref status ) == false )
			//	isAllValid = false;

			// handle instrument
			Entity_CredentialManager ecm = new Entity_CredentialManager();
			ecm.DeleteAll( relatedEntity, ref status );
			int newId = 0;
			ecm.Add( resource.RowId, resource.InstrumentIds.FirstOrDefault(), BaseFactory.RELATIONSHIP_TYPE_HAS_PART, ref newId, ref status );

			//Entity_HasResource handling particpants
			var eHasResourcesMgr = new Entity_HasResourceManager();
			//might be used with instrument (although if this is always credential, then use Entity.Credential?
			eHasResourcesMgr.DeleteAll( relatedEntity, ref status );

            if ( eHasResourcesMgr.SaveList( relatedEntity, CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, resource.ParticipantIds, ref status, Entity_HasResourceManager.HAS_RESOURCE_TYPE_HasResource ) == false )
                	isAllValid = false;

			if ( eHasResourcesMgr.SaveList( relatedEntity, CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, resource.ParticipantIds, ref status, Entity_HasResourceManager.HAS_RESOURCE_TYPE_HasResource ) == false )
				isAllValid = false;
			//Object??
			//maybe just save the CTID and get from entity_cache
			//try
			//{
			//	var ecm = new Entity_CredentialManager();
			//	ecm.DeleteAll( relatedEntity, ref status );
			//	if ( resource.InstrumentIds != null && resource.InstrumentIds.Count > 0 )
			//	{
			//		var newId = 0;
			//		foreach ( int id in resource.InstrumentIds )
			//		{
			//			ecm.Add( resource.RowId, id, BaseFactory.RELATIONSHIP_TYPE_HAS_PART, ref newId, ref status );
			//		}
			//	}
			//}
			//catch ( Exception ex )
			//{
			//	LoggingHelper.DoTrace( 1, thisClassName + ".UpdateParts(). Exception while processing InstrumentIds. " + ex.Message );
			//	status.AddError( ex.Message );

			//}

            //JurisdictionProfile 
            Entity_JurisdictionProfileManager jpm = new Entity_JurisdictionProfileManager();
			//do deletes - NOTE: other jurisdictions are added in: UpdateAssertedIns
			jpm.DeleteAll( relatedEntity, ref status );
			jpm.SaveList( resource.Jurisdiction, resource.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_SCOPE, ref status );

			return isAllValid;
		}


		#endregion

		#endregion

		#region == Retrieval =======================
		public static ThisResource GetMinimumByCtid( string ctid )
		{
			ThisResource entity = new ThisResource();
			using ( var context = new EntityContext() )
			{
				DBEntity from = context.CredentialingAction
						.FirstOrDefault( s => s.CTID.ToLower() == ctid.ToLower() );

				if ( from != null && from.Id > 0 )
				{
					entity.RowId = from.RowId;
					entity.Id = from.Id;
					entity.EntityStateId = from.EntityStateId;
					entity.Description = from.Description;
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
					DBEntity item = context.CredentialingAction
							.SingleOrDefault( s => s.RowId == profileUid );

					if ( item != null && item.Id > 0 )
					{
						MapFromDB( item, entity );
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
				DBEntity item = context.CredentialingAction
						.SingleOrDefault( s => s.Id == id );

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity );
				}
			}

			return entity;
		}
		public static ThisResource GetForDetail( int id )
		{
			ThisResource entity = new ThisResource();
			using ( var context = new EntityContext() )
			{
				DBEntity item = context.CredentialingAction
						.SingleOrDefault( s => s.Id == id );

				if ( item != null && item.Id > 0 )
				{
					//check for virtual deletes
					if ( item.EntityStateId == 0 )
					{
						LoggingHelper.DoTrace( 1, string.Format( thisClassName + " Error: encountered request for detail for a deleted record. CTID:{0}, Description: ", item.CTID, AssignLimitedString(item.Description, 200) ) );
						entity.Name = "Record was not found.";
						entity.CTID = item.CTID;
						return entity;
					}

					MapFromDB( item, entity );
				}
			}

			return entity;
		}

		public static List<ResourceSummary> GetRelatedActionFromObject(Guid id )
		{
			List<ResourceSummary> resourceList = new List<ResourceSummary>();
			using ( var context = new EntityContext() )
			{
				var relatedActions = context.CredentialingAction.Where( s => s.Object == id.ToString() ).ToList();
				foreach ( var action in relatedActions )
				{
					if ( action != null && action.Id > 0 )
					{
						var resource = new ResourceSummary();
						resource.Name = action.Name;
						resource.Description = action.Description;
						resource.EntityTypeId = CodesManager.ENTITY_TYPE_CREDENTIALING_ACTION;
						resource.CTID = action.CTID;
						resource.Id = action.Id;
						resourceList.Add( resource );
					}
				}
			}
			return resourceList;
		}

		public static int Count_ForOwningOrg( Guid orgUid )
		{
			int totalRecords = 0;

			using ( var context = new EntityContext() )
			{
				var results = context.CredentialingAction
							.Where( s => s.ActingAgentUid == orgUid && s.EntityStateId == 3 )
							.ToList();

				if ( results != null && results.Count > 0 )
				{
					totalRecords = results.Count();
				}
			}
			return totalRecords;
		}
	//	public static List<object> Autocomplete( string pFilter, int pageNumber, int pageSize, ref int pTotalRows )
	//	{
	//		bool autocomplete = true;
	//		List<object> results = new List<object>();
	//		List<string> competencyList = new List<string>();
	//		//ref competencyList, 
	//		List<ThisResource> list = Search( pFilter, string.Empty, pageNumber, pageSize, ref pTotalRows, autocomplete );
	//		bool appendingOrgNameToAutocomplete = UtilityManager.GetAppKeyValue( "appendingOrgNameToAutocomplete", false );
	//		string prevName = string.Empty;
	//		foreach ( var item in list )
	//		{
	//			//note excluding duplicates may have an impact on selected max terms
	//			if ( string.IsNullOrWhiteSpace( item.OrganizationName )
	//|| !appendingOrgNameToAutocomplete )
	//			{
	//				if ( item.Name.ToLower() != prevName )
	//					results.Add( item.Name );
	//			}
	//			else
	//			{
	//				results.Add( item.Name + " ('" + item.OrganizationName + "')" );
	//			}

	//			prevName = item.Name.ToLower();
	//		}
	//		return results;
	//	}


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

				using ( SqlCommand command = new SqlCommand( "[CredentialingAction_Search]", c ) )
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

						item = new CredentialingAction();
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
					item.Name = "Credential Action"; //TODO - add type
					//for autocomplete, only need name
					if ( autocomplete )
					{
						list.Add( item );
						continue;
					}

					item.Description = GetRowColumn( dr, "Description", string.Empty );
					string rowId = GetRowColumn( dr, "RowId" );
					item.RowId = new Guid( rowId );

					item.CTID = GetRowPossibleColumn( dr, "CTID", string.Empty );
					item.CredentialRegistryId = GetRowPossibleColumn( dr, "CredentialRegistryId", string.Empty );

					//org = GetRowPossibleColumn( dr, "Organization", string.Empty );
					//orgId = GetRowPossibleColumn( dr, "OrgId", 0 );
					//if ( orgId > 0 )
					//	item.OwningOrganization = new Organization() { Id = orgId, Name = org };

					//
					//temp = GetRowColumn( dr, "DateEffective", string.Empty );
					//if ( IsValidDate( temp ) )
					//	item.DateEffective = DateTime.Parse( temp ).ToString("yyyy-MM-dd");
					//else
					//	item.DateEffective = string.Empty;

					item.Created = GetRowColumn( dr, "Created", System.DateTime.MinValue );
					item.LastUpdated = GetRowColumn( dr, "LastUpdated", System.DateTime.MinValue );

					list.Add( item );
				}

				return list;

			}
		} //

		public void MapToDB( ThisResource input, DBEntity output )
		{

			//want output ensure fields input create are not wiped
			if ( output.Id == 0 )
			{
				output.CTID = input.CTID;
			}
			//if ( !string.IsNullOrWhiteSpace( input.CredentialRegistryId ) )
			//	output.CredentialRegistryId = input.CredentialRegistryId;

			output.Id = input.Id;
			output.Name = input.Name;
			output.Description = GetData( input.Description );
			output.EntityStateId = input.EntityStateId;
			output.ActingAgentUid = input.PrimaryAgentUID;
			//change to use codes.CredentialingActionType
			//done in import
			//output.ActionTypeId = CodesManager.GetPropertyIdBySchema( CodesManager.PROPERTY_CATEGORY_ACTION_STATUS_TYPE, input.Type );
			output.ActionTypeId = input.ActionTypeId;
		
			if ( input.ActionStatusTypeId > 0 )
				output.ActionStatusTypeId = input.ActionStatusTypeId;
			else
				output.ActionStatusTypeId = null;

			if ( string.IsNullOrWhiteSpace( input.Name ) )
			{
				//actually do this in the import step
				//input.Name = input.Type;
			}
			if ( !string.IsNullOrWhiteSpace( input.Image ) )
			{
				output.ImageUrl = GetUrlData( input.Image, null );
			}
			output.EvidenceOfAction = input.EvidenceOfAction;
			//??
			if ( IsValidGuid( input.ObjectUid ) )
			{
				output.Object = input.ObjectUid.ToString();
			}
			else
				output.Object = null;
			//
			if ( !string.IsNullOrWhiteSpace( input.StartDate ) )
			{
				output.StartDate = input.StartDate;
				if ( ( output.StartDate ?? string.Empty ).Length > 20 )
				{
					output.StartDate = output.StartDate.Substring( 0, 10 );
				}
			}
			else
				output.StartDate = null;
			//
			if ( !string.IsNullOrWhiteSpace( input.EndDate ) )
			{
				output.EndDate = input.EndDate;
				if ( ( output.EndDate ?? string.Empty ).Length > 20 )
				{
					output.EndDate = output.EndDate.Substring( 0, 10 );
				}
			}
			else
				output.EndDate = null;

			if ( IsValidDate( input.LastUpdated ) )
				output.LastUpdated = input.LastUpdated;
		}

		public static void MapFromDB( DBEntity input, ThisResource output )
		{

			output.Id = input.Id;
			output.RowId = input.RowId;
			output.EntityStateId = input.EntityStateId;
			output.EntityTypeId = CodesManager.ENTITY_TYPE_CREDENTIALING_ACTION;
			//
			output.Name = input.Name;
			output.FriendlyName = FormatFriendlyTitle( input.Name );

			output.Description = input.Description == null ? string.Empty : input.Description;
			output.CTID = input.CTID;
			if ( IsGuidValid( input.ActingAgentUid ) )
			{
				output.PrimaryAgentUID = ( Guid ) input.ActingAgentUid;
				output.PrimaryOrganization = OrganizationManager.GetBasics( ( Guid ) input.ActingAgentUid );
				output.ActingAgent = ConvertToResourceSummaryList( output.PrimaryOrganization, "CredentialingAction.ActingAgent" );
			}
			//this would get offered by. Should exclude the primary or ????
			output.OrganizationRole = Entity_AgentRelationshipManager.AgentEntityRole_GetAll_ToEnumeration( output.RowId, true );

			if ( IsValidDate( input.Created ) )
				output.Created = ( DateTime ) input.Created;
			if ( IsValidDate( input.LastUpdated ) )
				output.LastUpdated = ( DateTime ) input.LastUpdated;

			if ( input.ActionStatusTypeId != null && input.ActionStatusTypeId >0)
            {
				output.ActionStatusTypeId = ( int ) input.ActionStatusTypeId;

				CodeItem ct = CodesManager.Codes_PropertyValue_Get( output.ActionStatusTypeId );
				if ( ct != null && ct.Id > 0 )
				{
					//not sure we need the enumeration
					output.ActionStatusType = new Enumeration()
					{
						Name = "Action Status",
					};
					output.ActionStatusType.Items.Add( new EnumeratedItem() { Id = output.ActionStatusTypeId, Name = ct.Name, SchemaName = ct.SchemaName } );
					//output.ActionStatusType.Name = ct.Title;
					//do we really need this?
					//output.ActionStatusType.SchemaName = ct.SchemaName;
				}

			}
			//
			if ( input.ActionTypeId != null )
			{
				output.ActionTypeId = ( int ) input.ActionTypeId;
				var codeItem = CodesManager.GetCredentialingActionType( output.ActionTypeId );
				if ( codeItem != null && codeItem.Id > 0 )
				{
					output.Type = codeItem.Title;
					output.CTDLTypeLabel = output.EntityTypeLabel = codeItem.Title;
					output.EntityTypeSchema = codeItem.SchemaName;
				}
			}

			output.EvidenceOfAction = input.EvidenceOfAction;
			if ( !string.IsNullOrWhiteSpace( input.StartDate ) )
				output.StartDate = input.StartDate;
			else
				output.StartDate = string.Empty;
			//
			if ( !string.IsNullOrWhiteSpace( input.EndDate ) )
				output.EndDate = input.EndDate;
			else
				output.EndDate = string.Empty;
			//=====

			if( !string.IsNullOrWhiteSpace( input.Object ) && IsValidGuid( input.Object ) )
            {
				//will the object be in the cache
				//TODO - maybe could use ResourceDetail
				var objectref =	EntityManager.EntityCacheGetByGuid( Guid.Parse( input.Object ) );
				 MapEntityCacheToObject( objectref, output );
			}
			if ( input.ImageUrl != null && input.ImageUrl.Trim().Length > 0 )
				output.Image = input.ImageUrl;
			else
				output.Image = null;
			var relatedEntity = EntityManager.GetEntity( output.RowId, false );
			//if ( relatedEntity != null && relatedEntity.Id > 0 )
			output.EntityLastUpdated = output.LastUpdated;  // relatedEntity.LastUpdated;

			//
			//actually more efficient to get all and then split out
			//may only use participant?
			var getAll = Entity_HasResourceManager.GetAll( relatedEntity );
			if ( getAll != null && getAll.Count > 0 )
			{
				output.Participant = getAll.Where( r => r.EntityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION ).ToList();
			}
			//
			//output.Instrument = Entity_CredentialManager.GetAll( output.RowId, BaseFactory.RELATIONSHIP_TYPE_HAS_PART );
			var list = Entity_CredentialManager.GetAll( output.RowId, 0 );
            if ( list.Count > 0 )
            {
				output.Instrument = MapCredentialToResourceSummary( list.FirstOrDefault() );

			}
		} //


		public static int GetActionTypeId( string type )
		{
			if ( string.IsNullOrWhiteSpace( type ) )
				return 1;
			int typeId = 0;

			var codeItem = CodesManager.GetCredentialingActionType(type );
			if (codeItem != null && codeItem.Id > 0)
				typeId = codeItem.Id;
			
			//switch ( type.Replace( "ceterms:", string.Empty ).ToLower() )
			//{
			//	case "accreditaction":
			//		typeId = AccreditActionTypeId;
			//		break;
			//	case "advancedstandingaction":
			//		typeId = AdvancedstandingActionTypeId;
			//		break;
			//	case "approveaction":
			//		typeId = ApproveActionTypeId;
			//		break;
			//	case "credentialingaction": //should not happen!
			//		typeId = CredentialingActionTypeId;
			//		break;
			//	case "offeraction":
			//		typeId = OfferActionTypeId;
			//		break;
			//	case "recognizeaction":
			//		typeId = RecognizeActionTypeId;
			//		break;
			//	case "registrationaction":
			//		typeId = RegistrationActionTypeId;
			//		break;
			//	case "regulateaction":
			//		typeId = RegulateActionTypeId;
			//		break;
			//	case "renewaction":
			//		typeId = RenewActionTypeId;
			//		break;
			//	case "revokeaction":
			//		typeId = RevokeActionTypeId;
			//		break;
			//	case "rightsaction":
			//		typeId = RightsActionTypeId;
			//		break;
			//	case "workforcedemandaction":
			//		typeId = WorkForceDemandActionTypeId;
			//		break;
			//	//
			//	default:
			//		typeId = 0;
			//		break;
			//}

			return typeId;
		}

		public static string GetActionType( int typeId )
		{
			string type = string.Empty;
			switch ( typeId )
			{
				case 1:
					type = AccreditActionType;
					break;
				case 2:
					type = AdvancedstandingActionType;
					break;
				case 3:
					type = ApproveActionType;
					break;
				case 4:
					type = CredentialingActionType;
					break;
				case 5:
					type = OfferActionType;
					break;
				case 6:
					type = RecognizeActionType;
					break;
				case 7:
					type = RegulateActionType;
					break;
				case 8:
					type = RenewActionType;
					break;
				case 9:
					type = RevokeActionType;
					break;
				case 10:
					type = RightsActionType;
					break;
				case 11:
					type = WorkForceDemandActionType;
					break;
				case 12:
					type = RegistrationActionType;
					break;
				//
				default:
					type = "unexpected: " + typeId.ToString();
					break;
			}

			return type;
		}

		private static ResourceSummary MapCredentialToResourceSummary(Credential Credential )
        {
			ResourceSummary resource = new ResourceSummary();
			resource.Name = Credential.Name;
			resource.Description = Credential.Description;
			resource.EntityTypeId = CodesManager.ENTITY_TYPE_CREDENTIAL;
			//CodedNotation is not a credential property
			//resource.CodedNotation = Credential.CodedNotation;
			resource.CTID = Credential.CTID;
			resource.Id = Credential.Id;
			resource.ImageUrl = Credential.Image;
			return resource;
        }

		private static void MapEntityCacheToObject( EntityCache ec, ThisResource output )
		{
			TopLevelObject resource = new TopLevelObject();
			resource.Name = ec.Name;
			resource.EntityType = ec.EntityType;
			resource.EntityTypeId = ec.EntityTypeId;
			resource.Description = ec.Description;
			resource.SubjectWebpage = ec.SubjectWebpage;
			resource.Id = ec.BaseId;
            if ( !string.IsNullOrWhiteSpace( ec.ResourceDetail ) )
            {
				//Object has offered By which is a list, gets the offered by from resource detail
				if ( ec.EntityTypeId == CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE 
					|| ec.EntityTypeId == CodesManager.ENTITY_TYPE_LEARNING_PROGRAM 
					|| ec.EntityTypeId == CodesManager.ENTITY_TYPE_COURSE )
				{
					var detail = JsonConvert.DeserializeObject<Models.API.LearningOpportunityDetail>( ec.ResourceDetail );
					resource.CodedNotation = detail.CodedNotation;
					resource.CTDLTypeLabel = detail.CTDLTypeLabel;
					AssignOfferedBy( detail.OfferedBy, ref resource );
				}
				else if ( ec.EntityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL)
				{
					var detail = JsonConvert.DeserializeObject<Models.API.CredentialDetail>( ec.ResourceDetail );
					resource.CodedNotation = detail.CodedNotation;
					resource.CTDLTypeLabel = detail.CTDLTypeLabel;
					AssignOfferedBy( detail.OfferedBy, ref resource );
				}
				else if ( ec.EntityTypeId == CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE )
				{
					var detail = JsonConvert.DeserializeObject<Models.API.AssessmentDetail>( ec.ResourceDetail );
					resource.CTDLTypeLabel = detail.CTDLTypeLabel;
					AssignOfferedBy( detail.OfferedBy, ref resource );
				}
				else if ( ec.EntityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION 
					||ec.EntityTypeId==CodesManager.ENTITY_TYPE_PLAIN_ORGANIZATION 
					||ec.EntityTypeId==CodesManager.ENTITY_TYPE_QAORGANIZATION )
				{
					var detail = JsonConvert.DeserializeObject<Models.API.OrganizationDetail>( ec.ResourceDetail );
					resource.CTDLTypeLabel = detail.CTDLTypeLabel;
					AssignOfferedBy( detail.OfferedBy, ref resource );
				}
			}
			if ( ec.OwningOrgId > 0 )
            {
				var ownningOrg = OrganizationManager.GetDetail( ec.OwningOrgId );
				resource.PrimaryOrganization = ownningOrg;
            }
			output.Object = resource;
			//resource.ResourcePrimaryOrganizationName=e
   //         if ( ec.EntityTypeId == 7 || ec.EntityTypeId == 36 || ec.EntityTypeId == 37 )
   //         {
			//	output.ObjectLopp.Add( LearningOpportunityManager.GetBasic( ec.BaseId ));
			//}else if(ec.EntityTypeId == 1 )
   //         {
			//	output.ObjectCredential.Add( CredentialManager.GetBasic( ec.BaseId ) );
			//}
			//else if ( ec.EntityTypeId == 3 )
			//{
			//	output.ObjectAsmt.Add( AssessmentManager.GetBasic( ec.BaseId ) );
			//}
			//else if ( ec.EntityTypeId == 2 )
			//{
			//	output.ObjectOrg.Add( OrganizationManager.GetDetail( ec.BaseId ) );
			//}

		}

		private static void AssignOfferedBy(Models.Search.AJAXSettings input, ref TopLevelObject output )
		{
			if ( input != null )
			{
				if ( input.Total > 0 )
				{
					List<Organization> offeredByOrgs = new List<Organization>();
					foreach ( var org in input.Values )
					{
						var offeredBy = JsonConvert.DeserializeObject<Models.API.Outline>( org.ToString() );
						var orgDetail = new Organization()
						{
							Name = offeredBy.Label,
							Description = offeredBy.Description,
							Id = offeredBy.Meta_Id ?? 0,
							CTID = offeredBy.CTID,
						};
						offeredByOrgs.Add( orgDetail );
					}
					output.OfferedBy = offeredByOrgs;
				}
			}
		}

		#endregion

	}
}

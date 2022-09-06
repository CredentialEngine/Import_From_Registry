using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.ProfileModels;

using workIT.Utilities;

using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;
using DBEntity = workIT.Data.Tables.TransferValueProfile;
using ThisEntity = workIT.Models.Common.TransferValueProfile;

using Views = workIT.Data.Views;

using EM = workIT.Data.Tables;
using Newtonsoft.Json;

namespace workIT.Factories
{
	public class TransferValueProfileManager : BaseFactory
	{
		static string thisClassName = "TransferValueProfileManager";
		static string EntityType = "TransferValue";
		#region Persistance ===================


		/// <summary>
		/// Add/Update a TransferValueProfile
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public bool Save( ThisEntity entity, ref SaveStatus status )
		{
			bool isValid = true;
			int count = 0;

			DBEntity efEntity = new DBEntity();
			try
			{
				using ( var context = new EntityContext() )
				{

					if ( ValidateProfile( entity, ref status ) == false )
					{
						return false;
					}

					if ( entity.Id == 0 )
					{
						//add
						efEntity = new DBEntity();
						MapToDB( entity, efEntity );

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

						if ( IsValidGuid( entity.RowId ) )
							efEntity.RowId = entity.RowId;
						else
							efEntity.RowId = Guid.NewGuid();

						context.TransferValueProfile.Add( efEntity );

						count = context.SaveChanges();

						entity.Id = efEntity.Id;
						entity.RowId = efEntity.RowId;
						if ( count == 0 )
						{
							status.AddWarning( string.Format( " Unable to add Profile: {0} <br\\> ", string.IsNullOrWhiteSpace( entity.Name ) ? "no description" : entity.Name ) );
						}
						else
						{
							entity.RowId = efEntity.RowId;
							entity.Created = efEntity.Created.Value;
							entity.LastUpdated = efEntity.LastUpdated.Value;
							entity.Id = efEntity.Id;
							UpdateEntityCache( entity, ref status );

							//add log entry
							SiteActivity sa = new SiteActivity()
							{
								ActivityType = "TransferValueProfile",
								Activity = "Import",
								Event = "Add",
								Comment = string.Format( "New Transfer Value Profile was found by the import. Name: {0}, URI: {1}", entity.Name, entity.SubjectWebpage ),
								ActivityObjectId = entity.Id
							};
							new ActivityManager().SiteActivityAdd( sa );

							if ( !UpdateParts( entity, ref status ) )
								isValid = false;
						}
					}
					else
					{

						efEntity = context.TransferValueProfile.FirstOrDefault( s => s.Id == entity.Id );
						if ( efEntity != null && efEntity.Id > 0 )
						{
							entity.RowId = efEntity.RowId;
							//update
							MapToDB( entity, efEntity );
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

								count = context.SaveChanges();
								//
								entity.LastUpdated = efEntity.LastUpdated.Value;
								UpdateEntityCache( entity, ref status );
								//add log entry
								SiteActivity sa = new SiteActivity()
								{
									ActivityType = "TransferValueProfile",
									Activity = "Import",
									Event = "Update",
									Comment = string.Format( "Updated Transfer Value Profile found by the import. Name: {0}, URI: {1}", entity.Name, entity.SubjectWebpage ),
									ActivityObjectId = entity.Id
								};
								new ActivityManager().SiteActivityAdd( sa );

							}
							if ( !UpdateParts( entity, ref status ) )
								isValid = false;
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "TransferValueProfileManager.Save()" );
			}

			return isValid;
		}

		public bool UpdateParts( ThisEntity entity, ref SaveStatus status )
		{
			bool isAllValid = true;
			Entity_AgentRelationshipManager eamgr = new Entity_AgentRelationshipManager();
			Entity relatedEntity = EntityManager.GetEntity( entity.RowId );
			if ( relatedEntity == null || relatedEntity.Id == 0 )
			{
				status.AddError( "Error - the related Entity was not found." );
				return false;
			}
			eamgr.DeleteAll( relatedEntity, ref status );
			eamgr.SaveList( relatedEntity.Id, Entity_AgentRelationshipManager.ROLE_TYPE_OWNER, entity.OwnedBy, ref status );
			//
			eamgr.SaveList( relatedEntity.Id, Entity_AgentRelationshipManager.ROLE_TYPE_PUBLISHEDBY, entity.PublishedBy, ref status );

			//consider storing the class properties as Json!

			//derived from
			//where to store this? It commonly require Entity.TransferValueProfile
			var etvlMgr = new Entity_TransferValueProfileManager();

			//*****NOTE: API does check to ensure derived from is not same as the current
			etvlMgr.SaveList( entity.DerivedFromForImport, entity.RowId, ref status );

			Entity_AssertionManager eaMgr = new Entity_AssertionManager();
			eaMgr.DeleteAll( relatedEntity, ref status );

			//delete all Entity.Lopp, .Cred, and .Assessment relationships, and then add?
			//would be convenient if a delete wasn't necessary
			//NOTE: this will leave orphan reference objects. Will need to clean up. 
			//could check if target is a reference. If so delete, or check if there are other references
			//NOTE: this should have been done in TransferValueServices.HandlingExistingEntity - when is done corrently, remove this
			Entity_CredentialManager ecm = new Entity_CredentialManager();
			ecm.DeleteAll( relatedEntity, ref status );
			//
			var eam = new Entity_AssessmentManager();
			eam.DeleteAll( relatedEntity, ref status );
			//
			var elom = new Entity_LearningOpportunityManager();
			elom.DeleteAll( relatedEntity, ref status );
			//
			var etvp = new Entity_TransferValueProfileManager();
			etvp.DeleteAll( relatedEntity, ref status );
			//
			foreach ( var item in entity.TransferValueFromImport )
			{
				int newId = 0;
				var from = EntityManager.GetEntity( item, false );
				if ( from == null || from.Id == 0 )
				{
					status.AddError( string.Format( "{0}.UpdateParts - TransferValueFromImport. TVP: {1}. An entity was not found for GUID: {2}", thisClassName, entity.Id, item ) );
					continue;
				}
				if ( from.EntityTypeId == 1 )
				{
					//may need to designate for and from later
					ecm.Add( entity.RowId, from.EntityBaseId, BaseFactory.RELATIONSHIP_TYPE_IS_PART_OF, ref newId, ref status );
					if ( newId > 0 )
					{
						entity.CredentialIds.Add( from.EntityBaseId );
						//eaMgr.Save( relatedEntity.Id, 1, newId, Entity_AgentRelationshipManager.ROLE_TYPE_PROVIDES_OUTCOMES, ref status );
					}
				}
				else if ( from.EntityTypeId == 3 )
				{
					eam.Add( entity.RowId, from.EntityBaseId, BaseFactory.RELATIONSHIP_TYPE_IS_PART_OF, true, ref status );
					if ( newId > 0 )
					{
						entity.AssessmentIds.Add( from.EntityBaseId );
						//eaMgr.Save( relatedEntity.Id, 3, newId, Entity_AgentRelationshipManager.ROLE_TYPE_PROVIDES_OUTCOMES, ref status );
					}
				}
				else if ( from.EntityTypeId == 7 )
				{
					elom.Add( entity.RowId, from.EntityBaseId, BaseFactory.RELATIONSHIP_TYPE_IS_PART_OF, true, ref status );
					if ( newId > 0 )
					{
						entity.LearningOpportunityIds.Add( from.EntityBaseId );
						//eaMgr.Save( relatedEntity.Id, 7, newId, Entity_AgentRelationshipManager.ROLE_TYPE_PROVIDES_OUTCOMES, ref status );
					}
				}
			}

			foreach ( var item in entity.TransferValueForImport )
			{
				int newId = 0;
				var from = EntityManager.GetEntity( item, false );
				if ( from == null || from.Id == 0 )
				{
					//??
					status.AddError( string.Format( "{0}.UpdateParts - TransferValueForImport. TVP: {1}. An entity was not found for GUID: {2}", thisClassName, entity.Id, item ) );
					continue;
				}
				if ( from.EntityTypeId == 1 )
				{
					ecm.Add( entity.RowId, from.EntityBaseId, BaseFactory.RELATIONSHIP_TYPE_HAS_PART, ref newId, ref status );
					if ( newId > 0 )
					{
						entity.CredentialIds.Add( newId );
						//eaMgr.Save( relatedEntity.Id, 1, newId, Entity_AgentRelationshipManager.ROLE_TYPE_PROVIDES_OUTCOMES, ref status );
					}

				}
				else if ( from.EntityTypeId == 3 )
				{
					newId = eam.Add( entity.RowId, from.EntityBaseId, BaseFactory.RELATIONSHIP_TYPE_HAS_PART, true, ref status );
					if ( newId > 0 )
					{
						entity.AssessmentIds.Add( newId );
						//eaMgr.Save( relatedEntity.Id, 3, newId, Entity_AgentRelationshipManager.ROLE_TYPE_PROVIDES_OUTCOMES, ref status );
					}
				}
				else if ( from.EntityTypeId == 7 )
				{
					newId = elom.Add( entity.RowId, from.EntityBaseId, BaseFactory.RELATIONSHIP_TYPE_HAS_PART, true, ref status );
					if ( newId > 0 )
					{
						entity.LearningOpportunityIds.Add( newId );
						//eaMgr.Save( relatedEntity.Id, 7, newId, Entity_AgentRelationshipManager.ROLE_TYPE_PROVIDES_OUTCOMES, ref status );
					}

				}
			}

			foreach ( var item in entity.DerivedFromForImport )
			{
				var from = Get(item );
				if ( from == null || from.Id == 0 )
				{
					//??
					status.AddError( string.Format( "{0}.UpdateParts - DerivedFromForImport. TVP: {1}. A TVP was not found for ID: {2}", thisClassName, entity.Id, item ) );
					continue;
				}
				//check that not the same as current TVP
				if (from.Id == entity.Id)
				{
					status.AddError( string.Format( "{0}.UpdateParts - DerivedFromForImport. TVP: {1}. The DerivedFrom TVP Id ({2}) is the same as the current TVP ID", thisClassName, entity.Id, item ) );
					continue;
				}
				etvp.Add( entity.RowId, item, ref status );
			}

			//ProcessProfile
			Entity_ProcessProfileManager ppm = new Factories.Entity_ProcessProfileManager();
			ppm.DeleteAll( relatedEntity, ref status );
			try
			{
				ppm.SaveList( entity.DevelopmentProcess, Entity_ProcessProfileManager.DEV_PROCESS_TYPE, entity.RowId, ref status );
			}
			catch ( Exception ex )
			{
				string message = FormatExceptions( ex );
				LoggingHelper.LogError( ex, thisClassName + string.Format( ".AddProfiles() - ProcessProfiles. id: {0}", entity.Id ) );
				status.AddWarning( thisClassName + ".AddProfiles(). Exceptions encountered handling ProcessProfiles. " + message );
			}
			return isAllValid;
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

					context.TransferValueProfile.Add( efEntity );
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

		public void UpdateEntityCache( ThisEntity document, ref SaveStatus status )
		{
			EntityCache ec = new EntityCache()
			{
				EntityTypeId = CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE,
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
		/// <summary>
		/// Delete a framework - only if no remaining references!!
		/// MAY NOT expose initially
		/// </summary>
		/// <param name="recordId"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool Delete( int recordId, ref string statusMessage )
		{
			bool isOK = true;
			using ( var context = new EntityContext() )
			{
				DBEntity p = context.TransferValueProfile.FirstOrDefault( s => s.Id == recordId );
				if ( p != null && p.Id > 0 )
				{
					context.TransferValueProfile.Remove( p );
					int count = context.SaveChanges();
				}
				else
				{
					statusMessage = string.Format( "The record was not found: {0}", recordId );
					isOK = false;
				}
			}
			return isOK;

		}

		/// <summary>
		/// Do delete based on import of deleted documents
		/// </summary>
		/// <param name="credentialRegistryId">NOT CURRENTLY HANDLED</param>
		/// <param name="ctid"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool Delete( string ctid, ref string statusMessage )
		{
			bool isValid = true;
			if ( string.IsNullOrWhiteSpace( ctid ) )
			{
				statusMessage = thisClassName + ".Delete() Error - a valid CTID must be provided";
				return false;
			}
			using ( var context = new EntityContext() )
			{
				try
				{
					context.Configuration.LazyLoadingEnabled = false;
					var efEntity = context.TransferValueProfile
								.FirstOrDefault( s => s.CTID == ctid );

					if ( efEntity != null && efEntity.Id > 0 )
					{
						//TODO - may need a check for existing alignments
						Guid rowId = efEntity.RowId;
						var orgUid = efEntity.OwningAgentUid;
						//need to remove from Entity.
						//-using before delete trigger - verify won't have RI issues
						string msg = string.Format( " TransferValueProfile. Id: {0}, Name: {1}, Ctid: {2}", efEntity.Id, efEntity.Name, efEntity.CTID );
						//leaving as virtual?
						//need to check for in use.
						//context.TransferValueProfile.Remove( efEntity );
						efEntity.EntityStateId = 0;
						efEntity.LastUpdated = System.DateTime.Now;

						int count = context.SaveChanges();
						if ( count >= 0 )
						{
							new ActivityManager().SiteActivityAdd( new SiteActivity()
							{
								ActivityType = "TransferValueProfile",
								Activity = "Import",
								Event = "Delete",
								Comment = msg
							} );
							isValid = true;
							//delete cache
							new EntityManager().EntityCacheDelete( CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, efEntity.Id, ref statusMessage );
						}
						if ( orgUid != null )
						{
							List<String> messages = new List<string>();
							//mark owning org for updates 
							//	- nothing yet from frameworks
							var org = OrganizationManager.GetBasics( (Guid)orgUid );
							if ( org != null && org.Id > 0 )
							{
								new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, org.Id, 1, ref messages );
							}
							else
							{
								//issue with org ctid not found
							}
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
				status.AddWarning( "An Transfer Value Profile name must be entered" );
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


			return status.WasSectionValid;
		}

		#endregion
		#region  retrieval ==================

		/// <summary>
		/// Get a competency record
		/// </summary>
		/// <param name="profileId"></param>
		/// <returns></returns>
		public static ThisEntity Get( int profileId, bool gettingAll = true )
		{
			ThisEntity entity = new ThisEntity();
			if ( profileId == 0 )
				return entity;
			try
			{
				using ( var context = new EntityContext() )
				{
					DBEntity item = context.TransferValueProfile
							.SingleOrDefault( s => s.Id == profileId );

					if ( item != null && item.Id > 0 )
					{
						if ( item.EntityStateId == 0 )
						{
							LoggingHelper.DoTrace( 1, string.Format( thisClassName + " Error: encountered request for detail for a deleted record. Name: {0}, CTID:{1}", item.Name, item.CTID ) );
							entity.Name = "Record was not found.";
							entity.CTID = item.CTID;
							return entity;
						}
						MapFromDB( item, entity, gettingAll );
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".Get" );
			}
			return entity;
		}//


		public static ThisEntity GetByUrl( string SubjectWebpage )
		{
			ThisEntity entity = new ThisEntity();
			if ( string.IsNullOrWhiteSpace( SubjectWebpage ) )
				return entity;
			try
			{
				using ( var context = new EntityContext() )
				{
					//lookup by SubjectWebpage, or SourceUrl
					DBEntity item = context.TransferValueProfile
							.FirstOrDefault( s =>
								( s.SubjectWebpage != null && s.SubjectWebpage.ToLower() == SubjectWebpage.ToLower() )
							);

					if ( item != null && item.Id > 0 )
					{
						MapFromDB( item, entity );
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetByUrl" );
			}
			return entity;
		}//

		public static ThisEntity GetByCtid( string ctid, bool gettingAll = false )
		{
			ThisEntity entity = new ThisEntity();
			if ( string.IsNullOrWhiteSpace( ctid ) )
				return entity;
			try
			{
				using ( var context = new EntityContext() )
				{
					//lookup by SubjectWebpage, or SourceUrl
					DBEntity item = context.TransferValueProfile
							.FirstOrDefault( s => s.CTID.ToLower() == ctid.ToLower() );

					if ( item != null && item.Id > 0 )
					{
						MapFromDB( item, entity, gettingAll );
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetByCtid: " + ctid );
			}
			return entity;
		}//
		public static int Count_ForOwningOrg( Guid orgUid )
		{
			int totalRecords = 0;

			using ( var context = new EntityContext() )
			{
				var results = context.TransferValueProfile
							.Where( s => s.OwningAgentUid == orgUid && s.EntityStateId == 3 )
							.ToList();

				if ( results != null && results.Count > 0 )
				{
					totalRecords = results.Count();
				}
			}
			return totalRecords;
		}

		public static List<Credential> GetAllCredentials( int topParentTypeId, int topParentEntityBaseId )
		{
			var list = new List<Credential>();
			Entity parent = EntityManager.GetEntity( topParentTypeId, topParentEntityBaseId );
			//Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				return list;
			}

			try
			{
				//only need minimum
				list = Entity_CredentialManager.GetAll( parent.EntityUid, 0 );
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetAllCredentials" );
			}
			return list;
		}//
		public static List<AssessmentProfile> GetAllAssessments( int topParentTypeId, int topParentEntityBaseId )
		{
			var list = new List<AssessmentProfile>();
			Entity parent = EntityManager.GetEntity( topParentTypeId, topParentEntityBaseId );
			//Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				return list;
			}

			try
			{
				//only need minimum
				list = Entity_AssessmentManager.GetAll( parent.EntityUid, 0 );
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetAllAssessments" );
			}
			return list;
		}//
		public static List<LearningOpportunityProfile> GetAllLearningOpportunities( int topParentTypeId, int topParentEntityBaseId )
		{
			var list = new List<LearningOpportunityProfile>();
			Entity parent = EntityManager.GetEntity( topParentTypeId, topParentEntityBaseId );
			//Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				return list;
			}

			try
			{
				list = Entity_LearningOpportunityManager.LearningOpps_GetAll( parent.EntityUid, true, false,0 );
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetAllLopps" );
			}
			return list;
		}//
		public static void MapToDB( ThisEntity input, DBEntity output )
		{
			//want to ensure fields from create are not wiped
			//to.Id = from.Id;

			output.Name = input.Name;
			//TODO - can't have pending or references
			output.EntityStateId = 3;
			output.Description = input.Description;
			output.CTID = input.CTID;
			output.SubjectWebpage = input.SubjectWebpage ?? "";
			output.CredentialRegistryId = input.CredentialRegistryId ?? "";
			output.OwningAgentUid = input.OwningAgentUid;
			output.LifeCycleStatusTypeId = input.LifeCycleStatusTypeId;
			//need to handle a partial date
			if ( !string.IsNullOrWhiteSpace( input.StartDate ) )
			{
				output.StartDate = input.StartDate;
				if ( ( output.StartDate ?? "" ).Length > 20 )
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
				if ( ( output.EndDate ?? "" ).Length > 20 )
				{
					output.EndDate = output.EndDate.Substring( 0, 10 );
				}
			}
			else
				output.EndDate = null;



			//output.LifecycleStatusType = string.IsNullOrWhiteSpace( input.LifecycleStatusType) ? "lifecycle:Active" : input.LifecycleStatusType;
			//output.CodedNotation = input.CodedNotation;

			//just store the json
			output.IdentifierJson = input.IdentifierJson;
			output.TransferValueJson = input.TransferValueJson;
			output.TransferValueFromJson = input.TransferValueFromJson;
			output.TransferValueForJson = input.TransferValueForJson;

			//output.ProfileGraph = input.ProfileGraph;
			//ensure we don't reset the graph
			//if ( !string.IsNullOrWhiteSpace( input.ProfileGraph ) )
			//	output.ProfileGraph = input.ProfileGraph;
			//else
			//{

			//}
		} //
		//we don't have to store the complete object, such as assessment, lopp, etc.
		public static string TransferValueActionToJson( List<TopLevelObject> input )
		{
			string json = "";


			return json;
		} //

		public static void MapFromDB( DBEntity input, ThisEntity output, bool gettingAll = true )
		{
			output.Id = input.Id;
			output.RowId = input.RowId;
			output.EntityStateId = input.EntityStateId;
			output.Name = input.Name;
			output.FriendlyName = FormatFriendlyTitle( input.Name );

			output.Description = input.Description;
			output.CTID = input.CTID;

			if ( input.Created != null )
				output.Created = ( DateTime )input.Created;
			if ( input.LastUpdated != null )
				output.LastUpdated = ( DateTime )input.LastUpdated;
			if ( IsGuidValid( input.OwningAgentUid ) )
			{
				output.OwningAgentUid = ( Guid )input.OwningAgentUid;
				output.OwningOrganization = OrganizationManager.GetForSummary( output.OwningAgentUid );

				//get roles
				OrganizationRoleProfile orp = Entity_AgentRelationshipManager.AgentEntityRole_GetAsEnumerationFromCSV( output.RowId, output.OwningAgentUid );
				output.OwnerRoles = orp.AgentRole;
			}
			//
			output.OrganizationRole = Entity_AgentRelationshipManager.AgentEntityRole_GetAll_ToEnumeration( output.RowId, true );
			//

			//get related ....
			var relatedEntity = EntityManager.GetEntity( output.RowId, false );
			if ( relatedEntity != null && relatedEntity.Id > 0 )
				output.EntityLastUpdated = relatedEntity.LastUpdated;

			//
			output.SubjectWebpage = input.SubjectWebpage;
			output.CredentialRegistryId = input.CredentialRegistryId ?? "";

			//22-07-10 - LifeCycleStatusTypeId is now on the credential directly
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
			//output.CodedNotation = input.CodedNotation;
			//20-12-16 changed to a string as partial dates are possible
			if ( !string.IsNullOrWhiteSpace( input.StartDate ) )
				output.StartDate = input.StartDate;
			else
				output.StartDate = "";
			//
			if ( !string.IsNullOrWhiteSpace( input.EndDate ) )
				output.EndDate = input.EndDate;
			else
				output.EndDate = "";
	
			//derived from ....

			//get json and expand
			output.IdentifierJson = input.IdentifierJson;
			output.TransferValueJson = input.TransferValueJson;
			output.TransferValueFromJson = input.TransferValueFromJson;
			output.TransferValueForJson = input.TransferValueForJson;
			//
			if ( !string.IsNullOrWhiteSpace( output.IdentifierJson ) )
				output.Identifier = JsonConvert.DeserializeObject<List<Entity_IdentifierValue>>( output.IdentifierJson );
			if ( !string.IsNullOrWhiteSpace( output.TransferValueJson ) )
				output.TransferValue = JsonConvert.DeserializeObject<List<ValueProfile>>( output.TransferValueJson );


			if ( !gettingAll )
				return;

			//the top level object may not be enough. First need to confirm if reference lopps and asmts can have detail pages.
			if ( !string.IsNullOrWhiteSpace( output.TransferValueFromJson ) )
			{
				output.TransferValueFrom = JsonConvert.DeserializeObject<List<TopLevelObject>>( output.TransferValueFromJson );
				output.TransferValueFromLopp = new List<LearningOpportunityProfile>();
				var lopps = output.TransferValueFrom.Where( s => s.EntityTypeId == 7 ).ToList();
				foreach( var item in lopps)
				{
					//should exist, but need to handle where it was deleted
					var lopp =LearningOpportunityManager.GetBasic( item.Id );
					if (lopp != null && lopp.Id > 0)
						output.TransferValueFromLopp.Add( lopp );
				}
				var assmts = output.TransferValueFrom.Where( s => s.EntityTypeId == 3 ).ToList();
				foreach ( var item in assmts )
				{
					var asmt = AssessmentManager.GetBasic( item.Id );
					if ( asmt != null && asmt.Id > 0 )
						output.TransferValueFromAsmt.Add( asmt );
				}

				var creds = output.TransferValueFrom.Where( s => s.EntityTypeId == 1 ).ToList();
				foreach ( var item in creds )
				{
					//no this should not be for detail - especially if full object?
					//the detail was probably getting the TVP = infinate loop
					//output.TransferValueFromCredential.Add( CredentialManager.GetForDetail( item.Id ) );
					var cred = CredentialManager.GetBasic( item.Id );
					if ( cred != null && cred.Id > 0 )
						output.TransferValueFromCredential.Add( cred );
				}
			}
			//
			if ( !string.IsNullOrWhiteSpace( output.TransferValueForJson ) )
			{
				output.TransferValueFor = JsonConvert.DeserializeObject<List<TopLevelObject>>( output.TransferValueForJson );
				var lopps = output.TransferValueFor.Where( s => s.EntityTypeId == 7 ).ToList();
				foreach ( var item in lopps )
				{
					var lopp = LearningOpportunityManager.GetBasic( item.Id );
					if ( lopp != null && lopp.Id > 0 )
						output.TransferValueForLopp.Add( lopp );
				}
				var assmts = output.TransferValueFor.Where( s => s.EntityTypeId == 3 ).ToList();
				foreach ( var item in assmts )
				{
					var asmt = AssessmentManager.GetBasic( item.Id );
					if ( asmt != null && asmt.Id > 0 )
						output.TransferValueForAsmt.Add( asmt );
				}

				var creds = output.TransferValueFor.Where( s => s.EntityTypeId == 1 ).ToList();
				foreach ( var item in creds )
				{
					var cred = CredentialManager.GetBasic( item.Id );
					if ( cred != null && cred.Id > 0 )
						output.TransferValueForCredential.Add( cred );
				}
			}


			//this should be a summary level, not the full TVP
			output.DerivedFrom = Entity_TransferValueProfileManager.GetAll( output.RowId );

			//
			List<ProcessProfile> processes = Entity_ProcessProfileManager.GetAll( output.RowId );
			foreach ( ProcessProfile item in processes )
			{
				if ( item.ProcessTypeId == Entity_ProcessProfileManager.DEV_PROCESS_TYPE )
					output.DevelopmentProcess.Add( item );

				else
				{
					//unexpected
				}
			}


		}

		#endregion

		//public static int Count_ForOwningOrg( string orgCtid )
		//{
		//	int totalRecords = 0;
		//	if ( string.IsNullOrWhiteSpace( orgCtid ) || orgCtid.Trim().Length != 39 )
		//		return totalRecords;

		//	using ( var context = new EntityContext() )
		//	{
		//		var query = ( from entity in context.TransferValueProfile
		//					  join org in context.Organization on entity.OrganizationCTID equals org.CTID
		//					  where entity.OrganizationCTID.ToLower() == orgCtid.ToLower()
		//						   && org.EntityStateId > 1 && entity.EntityStateId == 3
		//					  select new
		//					  {
		//						  entity.CTID
		//					  } );
		//		//until ed frameworks is cleaned up, need to prevent dups != 39
		//		var results = query.Select( s => s.CTID ).Distinct()
		//			.ToList();

		//		if ( results != null && results.Count > 0 )
		//		{
		//			totalRecords = results.Count();

		//		}
		//	}

		//	return totalRecords;
		//}

		public static List<ThisEntity> Search( string pFilter, string pOrderBy, int pageNumber, int pageSize, ref int pTotalRows )
		{
			string connectionString = DBConnectionRO();
			var item = new ThisEntity();
			var list = new List<ThisEntity>();
			var result = new DataTable();

			using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				c.Open();

				if ( string.IsNullOrEmpty( pFilter ) )
				{
					pFilter = "";
				}

				using ( SqlCommand command = new SqlCommand( "[TransferValue.ElasticSearch]", c ) )
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
					string rows = command.Parameters[ 4 ].Value.ToString();
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
					item.CTID = GetRowColumn( dr, "CTID", "" );
					item.Name = GetRowColumn( dr, "Name", "???" );
					item.PrimaryOrganizationName = GetRowColumn( dr, "OrganizationName", "" );
					item.Description = GetRowColumn( dr, "Description", "" );
					item.SubjectWebpage = GetRowColumn( dr, "SubjectWebpage", "" );
					//item.CodedNotation = GetRowColumn( dr, "CodedNotation", "" );
					item.StartDate = GetRowColumn( dr, "StartDate", "" );
					item.EndDate = GetRowColumn( dr, "EndDate", "" );

					item.IdentifierJson = GetRowColumn( dr, "IdentifierJson", "" );
					item.TransferValueJson = GetRowColumn( dr, "TransferValueJson", "" );
					item.TransferValueFromJson = GetRowColumn( dr, "TransferValueFromJson", "" );
					item.TransferValueForJson = GetRowColumn( dr, "TransferValueForJson", "" );
					//
					item.EntityLastUpdated = GetRowColumn( dr, "EntityLastUpdated", System.DateTime.MinValue );
					item.Created = GetRowColumn( dr, "Created", System.DateTime.MinValue );
					//item.LastUpdated = GetRowColumn( dr, "LastUpdated", System.DateTime.MinValue );

					list.Add( item );
				}

				return list;

			}
		} //

		/// <summary>
		/// Search for TransferIntermediary
		/// </summary>
		/// <param name="pFilter"></param>
		/// <param name="pOrderBy"></param>
		/// <param name="pageNumber"></param>
		/// <param name="pageSize"></param>
		/// <param name="pTotalRows"></param>
		/// <returns></returns>
		public static List<ThisEntity> DoTransferIntermediarySearch( string pFilter, string pOrderBy, int pageNumber, int pageSize, ref int pTotalRows )
		{
			string connectionString = DBConnectionRO();
			var item = new ThisEntity();
			var list = new List<ThisEntity>();
			var result = new DataTable();

			using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				c.Open();

				if ( string.IsNullOrEmpty( pFilter ) )
				{
					pFilter = "";
				}

				using ( SqlCommand command = new SqlCommand( "[TransferValue.ElasticSearch]", c ) )
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
					string rows = command.Parameters[4].Value.ToString();
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
					item.CTID = GetRowColumn( dr, "CTID", "" );
					item.Name = GetRowColumn( dr, "Name", "???" );
					item.PrimaryOrganizationName = GetRowColumn( dr, "OrganizationName", "" );
					item.Description = GetRowColumn( dr, "Description", "" );
					item.SubjectWebpage = GetRowColumn( dr, "SubjectWebpage", "" );
					//item.CodedNotation = GetRowColumn( dr, "CodedNotation", "" );
					item.StartDate = GetRowColumn( dr, "StartDate", "" );
					item.EndDate = GetRowColumn( dr, "EndDate", "" );

					item.IdentifierJson = GetRowColumn( dr, "IdentifierJson", "" );
					item.TransferValueJson = GetRowColumn( dr, "TransferValueJson", "" );
					item.TransferValueFromJson = GetRowColumn( dr, "TransferValueFromJson", "" );
					item.TransferValueForJson = GetRowColumn( dr, "TransferValueForJson", "" );
					//
					item.EntityLastUpdated = GetRowColumn( dr, "EntityLastUpdated", System.DateTime.MinValue );
					item.Created = GetRowColumn( dr, "Created", System.DateTime.MinValue );
					//item.LastUpdated = GetRowColumn( dr, "LastUpdated", System.DateTime.MinValue );

					list.Add( item );
				}

				return list;

			}
		} //

	}


}

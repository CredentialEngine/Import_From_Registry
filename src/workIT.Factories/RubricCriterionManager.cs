using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using workIT.Models;
using workIT.Models.Common;
using workIT.Utilities;

using DBResource = workIT.Data.Tables.RubricCriterion;
using EntityContext = workIT.Data.Tables.workITEntities;
using ThisResource = workIT.Models.Common.RubricCriterion;

namespace workIT.Factories
{
    public class RubricCriterionManager : BaseFactory
	{
		static readonly string thisClassName = "RubricCriterionManager";
		static string EntityType = "RubricCriterion";
		static int EntityTypeId = CodesManager.ENTITY_TYPE_RUBRIC_CRITERION;
		static string Entity_Label = "RubricCriterion";
		static string Entities_Label = "RubricCriterions";
		static int HasSpecializationRelationshipId = 1;
		//this is an inverse, so should not be storing the 2, rather looking up reverse using 1??
		static int IsSpecializationOfRelationshipId = 2;

		#region Rubric - persistance ==================
		/// <summary>
		/// Update a Rubric
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
						DBResource efEntity = context.RubricCriterion
								.SingleOrDefault( s => s.Id == resource.Id );

						if ( efEntity != null && efEntity.Id > 0 )
						{
							//fill in fields that may not be in entity
							resource.RowId = efEntity.RowId;

							MapToDB( resource, efEntity );


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
									string message = thisClassName + $".Save Failed. Attempted to update a {EntityType}. The process appeared to not work, but was not an exception, so we have no message, or no clue. Name: {resource.Name}, Id: {resource.Id}.";
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
									Comment = $"{Entity_Label} was updated by the import. Name: {resource.Name}.",
									ActivityObjectId = resource.Id
								};
								//skipping adding to activity
								//new ActivityManager().SiteActivityAdd( sa );
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
				string message = HandleDBValidationError( dbex, thisClassName + string.Format( ".Save. id: {0}, Name: {1}", resource.Id, resource.Name ), EntityType );
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
		/// add a Rubric
		/// </summary>
		/// <param name="resource"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		private int Add( ThisResource resource, ref SaveStatus status )
		{
			DBResource efEntity = new DBResource();
			using ( var context = new EntityContext() )
			{
				try
				{
					MapToDB( resource, efEntity );

					if ( IsValidGuid( resource.RowId ) )
						efEntity.RowId = resource.RowId;
					else
						efEntity.RowId = Guid.NewGuid();
					//efEntity.EntityStateId = entity.EntityStateId = 3;
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
					context.RubricCriterion.Add( efEntity );

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
							Comment = $"{Entity_Label} was added by the import. Name: '{resource.Name}'.",
							ActivityObjectId = resource.Id
						};
						//skipping adding to activity
						//new ActivityManager().SiteActivityAdd( sa );
						if ( UpdateParts( resource, ref status ) == false )
						{
						}

						return efEntity.Id;
					}
					else
					{
						//?no info on error

						string message = $"{thisClassName}.Add Failed. Attempted to add a {Entity_Label}. The process appeared to not work, but was not an exception, so we have no message, or no clue. Name: {resource.Name}, ctid: {resource.CTID}";
						status.AddError( thisClassName + ". Error - the add was not successful. " + message );
						EmailManager.NotifyAdmin( $"{thisClassName}. Add Failed", message );
					}
				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", Entity_Label );
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
		public int AddBaseReference( ThisResource resource, ref SaveStatus status )
		{
			DBResource efEntity = new DBResource();
			try
			{
				using ( var context = new EntityContext() )
				{
					if ( resource == null ||
						( string.IsNullOrWhiteSpace( resource.Name ) )
						//||                        string.IsNullOrWhiteSpace( entity.SubjectWebpage )) 
						)
					{
						status.AddError( thisClassName + ". AddBaseReference() The " + Entity_Label + " is incomplete" );
						return 0;
					}

					//only add DB required properties
					//NOTE - an entity will be created via trigger
					//efEntity.EntityStateId = resource.EntityStateId = 2;
					efEntity.Name = resource.Name;
					efEntity.Description = resource.Description;
					//efEntity.SubjectWebpage = resource.SubjectWebpage;

					//
					if ( IsValidGuid( resource.RowId ) )
						efEntity.RowId = resource.RowId;
					else
						efEntity.RowId = Guid.NewGuid();
					//set to return, just in case
					resource.RowId = efEntity.RowId;
					//
					efEntity.Created = System.DateTime.Now;
					efEntity.LastUpdated = System.DateTime.Now;

					context.RubricCriterion.Add( efEntity );
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						resource.Id = efEntity.Id;
						resource.RowId = efEntity.RowId;
						resource.Created = efEntity.Created.Value;
						resource.LastUpdated = efEntity.LastUpdated.Value;
						UpdateEntityCache( resource, ref status );
						UpdateParts( resource, ref status );
						if ( UpdateParts( resource, ref status ) == false )
						{

						}
						return efEntity.Id;
					}

					status.AddError( thisClassName + ". AddBaseReference() Error - the save was not successful, but no message provided. " );
				}
			}
			catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
			{
				status.AddError( HandleDBValidationError( dbex, thisClassName + ".AddBaseReference() ", "Rubric" ) );
				LoggingHelper.LogError( dbex, thisClassName + string.Format( ".Add(), Name: {0}, SubjectWebpage: {1}", resource.Name, resource.SubjectWebpage ) );


			}
			catch ( Exception ex )
			{
				string message = FormatExceptions( ex );
				LoggingHelper.LogError( ex, thisClassName + string.Format( ".AddBaseReference(), Name: {0}\r\n", efEntity.Name ) );
				status.AddError( thisClassName + ". AddBaseReference() Error - the save was not successful. " + message );

			}
			return 0;
		}
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
					var entity = GetMinimumByCtid( ctid );
					if ( entity != null && entity.Id > 0 )
						return entity.Id;

					//only add DB required properties
					//NOTE - an entity will be created via trigger
					efEntity.Name = "Placeholder until full document is downloaded";
					efEntity.Description = "Placeholder until full document is downloaded";
					//efEntity.EntityStateId = 1;
					efEntity.RowId = entityUid;
					//watch that Ctid can be  updated if not provided now!!
					efEntity.CTID = ctid;
					//efEntity.SubjectWebpage = registryAtId;

					efEntity.Created = System.DateTime.Now;
					efEntity.LastUpdated = System.DateTime.Now;

					context.RubricCriterion.Add( efEntity );
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
						//entity.SubjectWebpage = efEntity.SubjectWebpage;
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
				EntityStateId = 3,			// document.EntityStateId,
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
				//status.AddWarning( "An Rubric Description must be entered" );
			}


			return status.WasSectionValid;
		}


		/// <summary>
		/// Delete an Rubric, and related Entity
		/// </summary>
		/// <param name="id"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool Delete( int id, ref string statusMessage )
		{
			bool isValid = false;
			if ( id == 0 )
			{
				statusMessage = $"Error - missing an identifier for the {EntityType}.";
				return false;
			}

			using ( var context = new EntityContext() )
			{
				try
				{
					context.Configuration.LazyLoadingEnabled = false;
					DBResource efEntity = context.RubricCriterion
								.SingleOrDefault( s => s.Id == id );

					if ( efEntity != null && efEntity.Id > 0 )
					{

						//need to remove from Entity.
						//could use a pre-delete trigger?
						//what about roles

						context.RubricCriterion.Remove( efEntity );
						int count = context.SaveChanges();
						if ( count > 0 )
						{
							isValid = true;
							//add pending delete request 
							List<String> messages = new List<string>();
							new SearchPendingReindexManager().AddDeleteRequest( CodesManager.ENTITY_TYPE_RUBRIC_CRITERION, id, ref messages );
							//
							//new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, orgId, 1, ref messages );
							//also check for any relationships
							//new Entity_AgentRelationshipManager().ReindexAgentForDeletedArtifact( orgUid );
						}
					}
					else
					{
						statusMessage = "Error - Rubric_Delete failed, as record was not found.";
					}
				}
				catch ( Exception ex )
				{
					statusMessage = FormatExceptions( ex );
					LoggingHelper.LogError( ex, thisClassName + ".Delete()" );

					if ( statusMessage.ToLower().IndexOf( "the delete statement conflicted with the reference constraint" ) > -1 )
					{
						statusMessage = $"Error: this {EntityType} cannot be deleted as it is being referenced by other items, such as roles or credentials. These associations must be removed before this Rubric can be deleted.";
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
					DBResource efEntity = context.RubricCriterion
								.FirstOrDefault( s => ( s.CTID == ctid )
								);

					if ( efEntity != null && efEntity.Id > 0 )
					{
						Guid rowId = efEntity.RowId;

						//need to remove from Entity.
						//-using before delete trigger - verify won't have RI issues
						string msg = string.Format( " Rubric. Id: {0}, Name: {1}, Ctid: {2}.", efEntity.Id, efEntity.Name, efEntity.CTID );
						//18-04-05 mparsons - change to set inactive, and notify - seems to have been some incorrect deletes
						//context.Rubric.Remove( efEntity );
						//efEntity.EntityStateId = 0;
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
							new SearchPendingReindexManager().AddDeleteRequest( CodesManager.ENTITY_TYPE_RUBRIC_CRITERION, efEntity.Id, ref messages );
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
						statusMessage = "Error: this Rubric cannot be deleted as it is being referenced by other items, such as roles or credentials. These associations must be removed before this Rubric can be deleted.";
					}
				}
			}
			return isValid;
		}
		public bool DeleteAllForRubric( int parentId, ref List<string> messages )
		{
			bool isValid = true;
			if ( parentId == 0 )
			{
				messages.Add( "Error - RubricCriterionManager.DeleteAll: The parent entity was not found: " + parentId.ToString() );
				return false;
			}
			using ( var context = new EntityContext() )
			{
				context.RubricCriterion.RemoveRange( context.RubricCriterion.Where( s => s.RubricId == parentId ) );
				int count = context.SaveChanges();
				if ( count > 0 )
				{
					isValid = true;
					messages.Add( string.Format( "removed {0} related relationships.", count ) );
				}
			}
			return isValid;

		}

		#region  properties ===================
		public bool UpdateParts( ThisResource resource, ref SaveStatus status )
		{
			bool isAllValid = true;
			Entity relatedEntity = EntityManager.GetEntity( resource.RowId );
			if ( relatedEntity == null || relatedEntity.Id == 0 )
			{
				status.AddError( "Error - the related Entity was not found." );
				return false;
			}

			var eHasResourcesMgr = new Entity_HasResourceManager();
			eHasResourcesMgr.DeleteAll( relatedEntity, ref status );

			if ( eHasResourcesMgr.SaveList( relatedEntity, CodesManager.ENTITY_TYPE_TASK_PROFILE, resource.TargetTaskIds, ref status, Entity_HasResourceManager.HAS_RESOURCE_TYPE_HasResource ) == false )
				isAllValid = false;



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
				DBResource from = context.RubricCriterion
						.FirstOrDefault( s => s.CTID.ToLower() == ctid.ToLower() );

				if ( from != null && from.Id > 0 )
				{
					entity.RowId = from.RowId;
					entity.Id = from.Id;
					entity.Name = from.Name;
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
					DBResource item = context.RubricCriterion
							.SingleOrDefault( s => s.RowId == profileUid );

					if ( item != null && item.Id > 0 )
					{
						MapFromDB( item, entity, false );
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
				DBResource item = context.RubricCriterion
						.SingleOrDefault( s => s.Id == id );

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity, false );
				}
			}

			return entity;
		}

		public static ThisResource GetByCTID( string ctid )
		{
			var entity = new ThisResource();
			using ( var context = new EntityContext() )
			{
				//s.Name.ToLower() == name.ToLower() && 
				context.Configuration.LazyLoadingEnabled = false;
				var list = context.RubricCriterion
						.Where( s => s.CTID == ctid )
						.ToList();
				int cntr = 0;
				foreach ( var from in list )
				{
					cntr++;
					//just take first one
					MapFromDB( from, entity, false );

					break;
				}
			}

			return entity;
		}


		public static ThisResource GetForDetail( int id )
		{
			ThisResource entity = new ThisResource();
			using ( var context = new EntityContext() )
			{
				DBResource item = context.RubricCriterion
						.SingleOrDefault( s => s.Id == id );

				if ( item != null && item.Id > 0 )
				{
					////check for virtual deletes
					//if ( item.EntityStateId == 0 )
					//{
					//	LoggingHelper.DoTrace( 1, string.Format( thisClassName + " Error: encountered request for detail for a deleted record. Name: {0}, CTID:{1}", item.Name, item.CTID ) );
					//	entity.Name = "Record was not found.";
					//	entity.CTID = item.CTID;
					//	return entity;
					//}

					MapFromDB( item, entity,
							true //includingProperties
							);
				}
			}

			return entity;
		}

		public static List<ThisResource> GetAllForRubric( int rubricId )
		{
			var entity = new List<ThisResource>();
			using ( var context = new EntityContext() )
			{
				context.Configuration.LazyLoadingEnabled = false;
				var list = context.RubricCriterion
						.Where( s => s.RubricId==rubricId)
						.ToList();
				foreach ( var from in list )
				{
					var to = new ThisResource();
					MapFromDB( from, to, false );
					entity.Add( to );
				}
			}

			return entity;
		}

		public static void MapToDB( ThisResource input, DBResource output )
		{

			if ( output.Id == 0 )
			{
				output.CTID = input.CTID;
			}
			output.Id = input.Id;
			output.Name = GetData( input.Name );
			output.Description = GetData( input.Description );
			output.ListID = input.ListID;
			output.Weight = Convert.ToDecimal( input.Weight );
			output.CodedNotation = GetData( input.CodedNotation );
			output.TargetCompetency = FormatCAOListAsDelimitedString( input.TargetCompetency, "|" );
			output.HasProgressionLevel = input.HasProgressionLevelCTID;
			output.RubricId = input.RubricId;
			if ( IsValidDate( input.LastUpdated ) )
				output.LastUpdated = input.LastUpdated;
		}

		public static void MapFromDB( DBResource input, ThisResource output, bool includingProperties )
		{
			output.Id = input.Id;
			output.RubricId = input.RubricId;
			output.RowId = input.RowId;
			output.CTID = input.CTID;
			output.Name = input.Name;
			output.Description = input.Description;
			output.CodedNotation = input.CodedNotation;
			output.ListID = input.ListID;
			output.Weight = input.Weight;
			if ( !string.IsNullOrWhiteSpace( input.TargetCompetency ) )
			{
				output.TargetCompetency = GetTargetCompetency( SplitDelimitedStringToList( input.TargetCompetency, '|' ) );
			}
			if ( IsValidDate( input.Created ) )
				output.Created = ( DateTime ) input.Created;
			if ( IsValidDate( input.LastUpdated ) )
				output.LastUpdated = ( DateTime ) input.LastUpdated;


			output.HasProgressionLevel = MapPLToResourceSummary( input.HasProgressionLevel);

			var relatedEntity = EntityManager.GetEntity( output.RowId, false );
			output.EntityLastUpdated = relatedEntity.LastUpdated;
			output.HasCriterionLevel =Entity_HasCriterionLevelManager.GetAll( relatedEntity.Id );
			var getAll = Entity_HasResourceManager.GetAll( relatedEntity );
			if ( getAll != null && getAll.Count > 0 )
			{
				output.TargetTask = getAll.Where( r => r.EntityTypeId == CodesManager.ENTITY_TYPE_TASK_PROFILE ).ToList();
			}

		} //
	
		#endregion
	}
}


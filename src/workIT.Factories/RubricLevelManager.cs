using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using workIT.Models;
using workIT.Models.Common;
using workIT.Utilities;

using DBResource = workIT.Data.Tables.RubricLevel;
using EntityContext = workIT.Data.Tables.workITEntities;
using ThisResource = workIT.Models.Common.RubricLevel;

namespace workIT.Factories
{
	public class RubricLevelManager : BaseFactory
	{
		static readonly string thisClassName = "RubricLevelManager";
		static string EntityType = "RubricLevel";
		static int EntityTypeId = CodesManager.ENTITY_TYPE_RUBRIC_LEVEL;
		static string Entity_Label = "Rubric Level";
		static string Entities_Label = "RubricLevel";
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
						context.Configuration.LazyLoadingEnabled = false;
						DBResource efEntity = context.RubricLevel
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
									string message = string.Format( thisClassName + ".Save Failed", "Attempted to update a Rubric Level. The process appeared to not work, but was not an exception, so we have no message, or no clue. Rubric Level: {0}, Id: {1}", resource.Name, resource.Id );
									status.AddError( "Error - the update was not successful. " + message );
									EmailManager.NotifyAdmin( thisClassName + ".Save Failed Failed", message );
								}

							}
							else
							{
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
				string message = HandleDBValidationError( dbex, thisClassName + string.Format( ".Save. id: {0}, Name: {1}", resource.Id, resource.Name ), "Rubric Level" );
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
					resource.EntityStateId = 3;
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
					context.RubricLevel.Add( efEntity );

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

						string message = thisClassName + string.Format( ". Add Failed", "Attempted to add a Rubric Level. The process appeared to not work, but was not an exception, so we have no message, or no clue. Rubric Level: {0}", resource.Name );
						status.AddError( thisClassName + ". Error - the add was not successful. " + message );
						EmailManager.NotifyAdmin( "RubricLevelManager. Add Failed", message );
					}
				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", "Rubric Level" );
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
						status.AddError( thisClassName + ". AddBaseReference() The Rubric Level is incomplete" );
						return 0;
					}

					//only add DB required properties
					//NOTE - an entity will be created via trigger
					//efEntity.EntityStateId = resource.EntityStateId = 2;
					efEntity.Name = resource.Name;
					efEntity.Description = resource.Description;

					//
					if ( IsValidGuid( resource.RowId ) )
						efEntity.RowId = resource.RowId;
					else
						efEntity.RowId = Guid.NewGuid();
					//set to return, just in case
					resource.RowId = efEntity.RowId;
					//

					//
					efEntity.Created = System.DateTime.Now;
					efEntity.LastUpdated = System.DateTime.Now;

					context.RubricLevel.Add( efEntity );
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
				status.AddError( HandleDBValidationError( dbex, thisClassName + ".AddBaseReference() ", "Rubric Level" ) );
				LoggingHelper.LogError( dbex, thisClassName + string.Format( ".Add(), Name: {0}", resource.Name ) );


			}
			catch ( Exception ex )
			{
				string message = FormatExceptions( ex );
				LoggingHelper.LogError( ex, thisClassName + string.Format( ".AddBaseReference. Name:  {0}", resource.Name) );
				status.AddError( thisClassName + ". AddBaseReference() Error - the save was not successful. " + message );

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
					DBResource efEntity = context.RubricLevel
								.SingleOrDefault( s => s.Id == id );

					if ( efEntity != null && efEntity.Id > 0 )
					{

						//need to remove from Entity.
						//could use a pre-delete trigger?
						//what about roles

						context.RubricLevel.Remove( efEntity );
						int count = context.SaveChanges();
						if ( count > 0 )
						{
							isValid = true;
							//add pending delete request 
							List<String> messages = new List<string>();
							new SearchPendingReindexManager().AddDeleteRequest( CodesManager.ENTITY_TYPE_RUBRIC_LEVEL, id, ref messages );
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
					LoggingHelper.LogError( ex, thisClassName + ".Rubric Level_Delete()" );

					if ( statusMessage.ToLower().IndexOf( "the delete statement conflicted with the reference constraint" ) > -1 )
					{
						statusMessage = "Error: this Rubric Level cannot be deleted as it is being referenced by other items, such as roles or credentials. These associations must be removed before this Rubric can be deleted.";
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
				context.RubricLevel.RemoveRange( context.RubricLevel.Where( s => s.RubricId == parentId ) );
				int count = context.SaveChanges();
				if ( count > 0 )
				{
					isValid = true;
					messages.Add( string.Format( "removed {0} related relationships.", count ) );
				}
			}
			return isValid;

		}

		#region Rubric Level properties ===================
		public bool UpdateParts( ThisResource resource, ref SaveStatus status )
		{
			bool isAllValid = true;
			Entity relatedEntity = EntityManager.GetEntity( resource.RowId );
			if ( relatedEntity == null || relatedEntity.Id == 0 )
			{
				status.AddError( "Error - the related Entity was not found." );
				return false;
			}

			if ( UpdateProperties( resource, relatedEntity, ref status ) == false )
			{
				isAllValid = false;
			}
			return isAllValid;
		}

		public bool UpdateProperties( ThisResource entity, Entity relatedEntity, ref SaveStatus status )
		{
			bool isAllValid = true;

			//if ( mgr.AddProperties( entity.EvaluatorType, entity.RowId, CodesManager.ENTITY_TYPE_RUBRIC, CodesManager.PROPERTY_CATEGORY_EVALUATOR_CATEGORY, false, ref status ) == false )
			//	isAllValid = false;
			//first clear all propertiesd
			//mgr.DeleteAll( relatedEntity, ref status );
			//Entity_ReferenceManager erm = new Entity_ReferenceManager();
			//already did a deleteAll in UpdateParts

			return isAllValid;
		}


		#endregion

		#endregion

		#region == Retrieval =======================
		public static ThisResource Get( Guid profileUid )
		{
			ThisResource entity = new ThisResource();
			if ( !IsGuidValid( profileUid ))
				return null;
			try
			{
				using ( var context = new EntityContext() )
				{
					DBResource item = context.RubricLevel
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
				DBResource item = context.RubricLevel
						.SingleOrDefault( s => s.Id == id );

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity, false );
				}
			}

			return entity;
		}
		public static ThisResource GetByNameAndDescription( string name, string description )
		{
			var entity = new ThisResource();
			using ( var context = new EntityContext() )
			{
				context.Configuration.LazyLoadingEnabled = false;
				var list = context.RubricLevel
					.Where( s => s.Name.ToLower() == name.ToLower() && s.Description != null )
					.OrderBy( s => s.Name )
					.ToList();
				int cntr = 0;
				foreach ( var from in list )
				{
					cntr++;
					//if only one take it. 
					if ( list.Count == 1 )
					{
						MapFromDB( from, entity, false );
						break;
					}
					//just start with an exact match on the desc. The key is having one
					if ( from.Description.ToLower() == description.ToLower() )
					{
						MapFromDB( from, entity, false );
						break;
					}
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
				var list = context.RubricLevel
						.Where( s => s.RubricId == rubricId )
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

		public static ThisResource GetForDetail( int id )
		{
			ThisResource entity = new ThisResource();
			using ( var context = new EntityContext() )
			{
				DBResource item = context.RubricLevel
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

		public static void MapToDB( ThisResource input, DBResource output )
		{

			//if ( output.Id == 0 )
			//{
			//	output.CTID = input.CTID;
			//}
			output.Id = input.Id;
			output.Name = GetData( input.Name );
			output.Description = GetData( input.Description );
			output.ListID = input.ListID;
			output.HasProgressionLevel =input.HasProgressionLevelCTID;
			output.CodedNotation = GetData( input.CodedNotation );
			output.RubricId = input.RubricId;
			if ( IsValidDate( input.LastUpdated ) )
				output.LastUpdated = input.LastUpdated;
		}

		public static void MapFromDB( DBResource input, ThisResource output, bool includingProperties )
		{
			output.Id = input.Id;
			output.RubricId = input.RubricId;
			output.RowId = input.RowId;
			output.Name = input.Name;
			output.Description = input.Description;
			output.CodedNotation = input.CodedNotation;
			output.ListID = input.ListID;
			if ( IsValidDate( input.Created ) )
				output.Created = ( DateTime ) input.Created;
			if ( IsValidDate( input.LastUpdated ) )
				output.LastUpdated = ( DateTime ) input.LastUpdated;
			output.HasProgressionLevel = MapPLToResourceSummary( input.HasProgressionLevel );
			var relatedEntity = EntityManager.GetEntity( output.RowId, false );
			output.EntityLastUpdated = relatedEntity.LastUpdated;
			
			output.HasCriterionLevel = Entity_HasCriterionLevelManager.GetAll( relatedEntity.Id );

		} //
	
		#endregion

	}
}


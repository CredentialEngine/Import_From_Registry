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

using ThisEntity = workIT.Models.Common.ConditionManifest;
using DBEntity = workIT.Data.Tables.ConditionManifest;
using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;

using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;

namespace workIT.Factories
{
	public class ConditionManifestManager : BaseFactory
	{
		static string thisClassName = "Factories.ConditionManifestManager";
		static string EntityType = "ConditionManifest";
		static int EntityTypeId = CodesManager.ENTITY_TYPE_CONDITION_MANIFEST;

		EntityManager entityMgr = new EntityManager();

		#region === Persistance ==================
		/// <summary>
		/// Persist ConditionManifest
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="parentUid"></param>
		/// <param name="userId"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public bool Save( ThisEntity entity,  ref SaveStatus status )
		{
			bool isValid = true;
			//will not have a parent Guid - should be the entity.OwningAgentUid
			//Guid parentUid = entity.OwningAgentUid;

			if ( !IsValidGuid( entity.PrimaryAgentUID ) )
			{
				status.AddError( "Error: the parent identifier was not provided." );
				return false;
			}
			
			int count = 0;
			DateTime lastUpdated = System.DateTime.Now;
			DBEntity efEntity = new DBEntity();
			int parentOrgId = 0;

			Guid condtionManifestParentUid = new Guid();
			Entity parent = EntityManager.GetEntity( entity.PrimaryAgentUID );
			if ( parent == null || parent.Id == 0 )
			{
				status.AddError( "Error - the entity for the parent organization was not found." );
				return false;
			}
			if ( parent.EntityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION )
			{
				parentOrgId = parent.EntityBaseId;
				condtionManifestParentUid = parent.EntityUid;
				//no common condition in this context
			}
			
			
			using ( var context = new EntityContext() )
			{
				try
				{
					bool isEmpty = false;

					if ( ValidateProfile( entity, ref isEmpty, ref status ) == false )
					{
						return false;
					}
					if ( isEmpty )
					{
						status.AddWarning( "The Condition Manifest Profile is empty. " );
						return false;
					}

					if ( entity.Id == 0 )
					{
						//add
						efEntity = new DBEntity();
						MapToDB( entity, efEntity );
						
						efEntity.OrganizationId = parentOrgId;
						efEntity.EntityStateId = entity.EntityStateId = 3;
						if ( IsValidDate( status.EnvelopeCreatedDate ) )
							efEntity.Created = status.LocalCreatedDate;
						else
							efEntity.Created = DateTime.Now;
						//
						if ( IsValidDate( status.EnvelopeUpdatedDate ) && status.LocalUpdatedDate != efEntity.LastUpdated )
						{
							efEntity.LastUpdated = status.LocalUpdatedDate;
							lastUpdated = status.LocalUpdatedDate;
						}

						if ( IsValidGuid( entity.RowId ) )
							efEntity.RowId = entity.RowId;
						else
							efEntity.RowId = Guid.NewGuid();

						context.ConditionManifest.Add( efEntity );
						count = context.SaveChanges();

						entity.Id = efEntity.Id;
						entity.RowId = efEntity.RowId;
						if ( count == 0 )
						{
							status.AddError( " Unable to add Condition Manifest Profile" );
						}
						else
						{
							entity.RowId = efEntity.RowId;
							entity.Created = efEntity.Created.Value;
							entity.LastUpdated = efEntity.LastUpdated.Value;
							entity.Id = efEntity.Id;
							UpdateEntityCache( entity, ref status );
							//create the Entity.ConditionManifest
							//ensure to handle this properly when adding a commonCondition CM to a CM
							Entity_HasConditionManifest_Add( condtionManifestParentUid, efEntity.Id, ref status );

							if ( !UpdateParts( entity, ref status ) )
									isValid = false;

							SiteActivity sa = new SiteActivity()
							{
								ActivityType = "ConditionManifest",
								Activity = "Import",
								Event = "Add",
								Comment = string.Format( "ConditionManifest was added by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ),
								ActivityObjectId = entity.Id
							};
							new ActivityManager().SiteActivityAdd( sa );
							// a trigger is used to create the entity Object. 

						}
					}
					else
					{

						efEntity = context.ConditionManifest.SingleOrDefault( s => s.Id == entity.Id );
						if ( efEntity != null && efEntity.Id > 0 )
						{
							//delete the entity and re-add
							//Entity e = new Entity()
							//{
							//	EntityBaseId = efEntity.Id,
							//	EntityTypeId = CodesManager.ENTITY_TYPE_CONDITION_MANIFEST,
							//	EntityType = "ConditionManifest",
							//	EntityUid = efEntity.RowId,
							//	EntityBaseName = efEntity.Name
							//};
							//if ( entityMgr.ResetEntity( e, ref statusMessage ) )
							//{

							//}

							entity.RowId = efEntity.RowId;
							//update
							MapToDB( entity, efEntity );
                            //assume and validate, that if we get here we have a full record
                            if ( ( efEntity.EntityStateId ?? 1 ) != 2 )
                                efEntity.EntityStateId = 3;

                            entity.EntityStateId = ( int ) efEntity.EntityStateId;
                            //if started as a placeholder, may not have the org
                            efEntity.OrganizationId = parentOrgId;
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

								count = context.SaveChanges();
							}
                            else
                            {
                                //update entity.LastUpdated - assuming there has to have been some change in related data
                                //new EntityManager().UpdateModifiedDate( entity.RowId, ref status, efEntity.LastUpdated );
                            }
							entity.LastUpdated = lastUpdated;
							UpdateEntityCache( entity, ref status );

							//the Entity.ConditionManifest should exist, try to be sure
							Entity_HasConditionManifest_Add( condtionManifestParentUid, efEntity.Id, ref status );
							//
							if ( !UpdateParts( entity, ref status ) )
								isValid = false;

							SiteActivity sa = new SiteActivity()
							{
								ActivityType = "ConditionManifest",
								Activity = "Import",
								Event = "Update",
								Comment = string.Format( "ConditionManifest was updated by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ),
								ActivityObjectId = entity.Id
							};
							new ActivityManager().SiteActivityAdd( sa );

							//if ( isValid || partsUpdateIsValid )
								new EntityManager().UpdateModifiedDate( entity.RowId, ref status, efEntity.LastUpdated );
						}
					}
				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					string message = HandleDBValidationError( dbex, thisClassName + ".Save() ", "ConditionManifest" );
					status.AddError( "Error - the save was not successful. " + message );
					LoggingHelper.LogError( dbex, thisClassName + string.Format( ".Save(), Parent: {0} ({1})", parent.EntityBaseName, parent.EntityBaseId ) );
					isValid = false;
				}
				catch ( Exception ex )
				{
					string message = FormatExceptions( ex );
					status.AddError( "Error - the save was not successful. " + message );
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Save(), Parent: {0} ({1})", parent.EntityBaseName, parent.EntityBaseId ) );
					isValid = false;
				}

			}

		


			return isValid;
		}
		public void UpdateEntityCache( ThisEntity document, ref SaveStatus status )
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
		public bool UpdateParts( ThisEntity entity, ref SaveStatus status )
		{
			status.HasSectionErrors = false;
            Entity relatedEntity = EntityManager.GetEntity( entity.RowId );
            if ( relatedEntity == null || relatedEntity.Id == 0 )
            {
                status.AddError( "Error - the related Entity was not found." );
                return false;
            }

            //ConditionProfile
            Entity_ConditionProfileManager emanager = new Entity_ConditionProfileManager();
			//deleteall is handled in SaveList
            //emanager.DeleteAll( relatedEntity, ref status );

            emanager.SaveList( entity.Requires, Entity_ConditionProfileManager.ConnectionProfileType_Requirement, entity.RowId, ref status );
			emanager.SaveList( entity.Recommends, Entity_ConditionProfileManager.ConnectionProfileType_Recommendation, entity.RowId, ref status );
			emanager.SaveList( entity.Corequisite, Entity_ConditionProfileManager.ConnectionProfileType_Corequisite, entity.RowId, ref status );
			emanager.SaveList( entity.CoPrerequisite, Entity_ConditionProfileManager.ConnectionProfileType_CoPrerequisite, entity.RowId, ref status );

			emanager.SaveList( entity.EntryCondition, Entity_ConditionProfileManager.ConnectionProfileType_EntryCondition, entity.RowId, ref status );
			emanager.SaveList( entity.Renewal, Entity_ConditionProfileManager.ConnectionProfileType_Renewal, entity.RowId, ref status );

			return status.WasSectionValid;
		} //


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
					var entity = GetByCtid( ctid );
					if ( entity != null && entity.Id > 0 )
					{
						return entity.Id;
					}
					efEntity.Name = "Placeholder until full document is downloaded";
					efEntity.Description = "Placeholder until full document is downloaded";
					efEntity.EntityStateId = 1;
					efEntity.RowId = entityUid;
					efEntity.CTID = ctid;
					efEntity.SubjectWebpage = registryAtId;
					efEntity.Created = DateTime.Now;
					efEntity.LastUpdated = DateTime.Now;
					context.ConditionManifest.Add( efEntity );
					if ( context.SaveChanges() > 0 )
					{
						entity.Id = efEntity.Id;
						entity.RowId = efEntity.RowId;
						entity.CTID = efEntity.CTID;
						entity.EntityStateId = 1;
						entity.Name = efEntity.Name;
						entity.Description = efEntity.Description;
						entity.SubjectWebpage = efEntity.SubjectWebpage;
						entity.Created = efEntity.Created.Value;
						entity.LastUpdated = efEntity.LastUpdated.Value;
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
		

		/// <summary>
		/// Delete a Condition Manifest, and related Entity
		/// </summary>
		/// <param name="Id"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		//public bool Delete( int Id, ref string statusMessage )
		//{
		//	bool isValid = false;
		//	if ( Id == 0 )
		//	{
		//		statusMessage = "Error - missing an identifier for the ConditionManifest";
		//		return false;
		//	}
		//	using ( var context = new EntityContext() )
		//	{
		//		try
		//		{
		//			DBEntity efEntity = context.ConditionManifest
		//						.SingleOrDefault( s => s.Id == Id );

		//			if ( efEntity != null && efEntity.Id > 0 )
		//			{
		//				Guid rowId = efEntity.RowId;

		//				context.ConditionManifest.Remove( efEntity );
		//				int count = context.SaveChanges();
		//				if ( count > 0 )
		//				{
		//					isValid = true;
		//					//do with trigger now
		//					//new EntityManager().Delete( rowId, ref statusMessage );
		//				}
		//			}
		//			else
		//			{
		//				statusMessage = "Error - delete failed, as record was not found.";
		//			}
		//		}
		//		catch ( Exception ex )
		//		{
		//			statusMessage = FormatExceptions( ex );
		//			LoggingHelper.LogError( ex, thisClassName + ".Delete()" );
					
		//			if ( statusMessage.ToLower().IndexOf( "the delete statement conflicted with the reference constraint" ) > -1 )
		//			{
		//				statusMessage = "Error: this Condition Manifest cannot be deleted as it is being referenced by other items, such as roles or credentials. These associations must be removed before this Condition Manifest can be deleted.";
		//			}
		//		}
		//	}

		//	return isValid;
		//}
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
                statusMessage = thisClassName + ".Delete() Error - a valid CTID must be provided.";
                return false;
            }

            using ( var context = new EntityContext() )
			{
				try
				{
					context.Configuration.LazyLoadingEnabled = false;
					DBEntity efEntity = context.ConditionManifest
								.FirstOrDefault( s => s.CTID == ctid );

                    if ( efEntity != null && efEntity.Id > 0 )
					{
						Guid rowId = efEntity.RowId;
						int orgId = efEntity.OrganizationId ?? 0;
                        //need to remove from Entity.
                        //-using before delete trigger - verify won't have RI issues
                        string msg = string.Format( " ConditionManifest. Id: {0}, Name: {1}, Ctid: {2}.", efEntity.Id, efEntity.Name, efEntity.CTID );
                        //leaving as a delete
                        context.ConditionManifest.Remove( efEntity );
                        //efEntity.EntityStateId = 0;
                        //efEntity.LastUpdated = System.DateTime.Now;

                        int count = context.SaveChanges();
						if ( count >= 0 )
						{
                            new ActivityManager().SiteActivityAdd( new SiteActivity()
                            {
                                ActivityType = "ConditionManifest",
                                Activity = "Import",
                                Event = "Delete",
                                Comment = msg
                            } );
                            isValid = true;
							//delete cache
							new EntityManager().EntityCacheDelete( rowId, ref statusMessage );
						}
						List<String> messages = new List<string>();
						//mark owning org for updates 
						new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, orgId, 1, ref messages );
					}
					else
					{
						statusMessage = thisClassName + ".Delete() Warning No action taken, as the record was not found.";
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + ".Delete(ctid)" );
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
		public bool ValidateProfile( ThisEntity profile, ref bool isEmpty, ref SaveStatus status )
		{
			status.HasSectionErrors = false;

			isEmpty = false;
			//check if empty

			//&& ( profile.EstimatedCost == null || profile.EstimatedCost.Count == 0 )
			if ( string.IsNullOrWhiteSpace( profile.Name ) )
			{
				//actually optional
				status.AddError( "A Condition Manifest name must be entered" );
			}
			if ( string.IsNullOrWhiteSpace( profile.Description ) )
			{
				status.AddWarning( "A Condition Manifest Description must be entered" );
			}

			//not sure if this will be selected, or by context
			if ( !IsValidGuid( profile.PrimaryAgentUID ) )
			{
				status.AddError( "An owning organization must be selected" );
			}

			if ( !IsUrlValid( profile.SubjectWebpage, ref commonStatusMessage ) )
			{
				status.AddWarning( "The Subject Webpage Url is invalid " + commonStatusMessage );
			}

			return status.WasSectionValid;
		}

		#endregion

		#region == Retrieval =======================
		public static ThisEntity GetByCtid( string ctid )
		{
			ThisEntity entity = new ThisEntity();
			using ( var context = new EntityContext() )
			{
				DBEntity from = context.ConditionManifest
						.SingleOrDefault( s => s.CTID == ctid );

				if ( from != null && from.Id > 0 )
				{
					MapFromDB( from, entity );
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
				DBEntity from = context.ConditionManifest
						.FirstOrDefault( s => s.SubjectWebpage.ToLower() == swp.ToLower() );

				if ( from != null && from.Id > 0 )
				{
					MapFromDB( from, entity );
				}
			}
			return entity;
		}
		public static ThisEntity Get( int id)
		{
			ThisEntity entity = new ThisEntity();

			using ( var context = new EntityContext() )
			{
			
				DBEntity item = context.ConditionManifest
						.SingleOrDefault( s => s.Id == id );

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity );
				}
			}

			return entity;
		}

		/// <summary>
		/// Get absolute minimum for display as profile link
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public static ThisEntity GetBasic( int id )
		{
			ThisEntity entity = new ThisEntity();

			using ( var context = new EntityContext() )
			{
				//want to get org, deal with others
				//context.Configuration.LazyLoadingEnabled = false;

				DBEntity item = context.ConditionManifest
						.SingleOrDefault( s => s.Id == id );

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity );
				}
			}

			return entity;
		}


		/// <summary>
		/// Get all the condition manifests for the parent organization
		/// </summary>
		/// <param name="orgId"></param>
		/// <returns></returns>
		public static List<ThisEntity> GetAll( int orgId, bool isForLinks )
		{
			ThisEntity to = new ThisEntity();
			List<ThisEntity> list = new List<ThisEntity>();

			try
			{
				using ( var context = new EntityContext() )
				{
					var results = context.ConditionManifest
							.Where( s => s.OrganizationId == orgId )
							.OrderBy( s => s.Created )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( var from in results )
						{
							to = new ThisEntity();
							if ( isForLinks )
							{
								to.Id = from.Id;
								to.RowId = from.RowId;

								to.OrganizationId = (int)from.OrganizationId;
								//to.OwningAgentUid = from.Entity.EntityUid;
								//
								to.Name = from.Name;
								to.Description = from.Description;
							} else
							{
								MapFromDB( from, to );
							}
							list.Add( to );
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetAll(int orgId)" );
			}
			return list;
		}//
		public static List<ThisEntity> GetAllOLD( int orgId, bool isForLinks )
		{
			ThisEntity to = new ThisEntity();
			List<ThisEntity> list = new List<ThisEntity>();

			Entity parent = EntityManager.GetEntity( 2, orgId );
			if ( parent == null || parent.Id == 0 )
			{
				return list;
			}
			try
			{
				using ( var context = new EntityContext() )
				{
					List<EM.Entity_ConditionManifest> results = context.Entity_ConditionManifest
							.Where( s => s.EntityId == parent.Id )
							.OrderBy( s => s.Created )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( EM.Entity_ConditionManifest from in results )
						{
							to = new ThisEntity();
							if ( isForLinks )
							{
								to.Id = from.ConditionManifestId;
								to.RowId = from.ConditionManifest.RowId;

								to.OrganizationId = ( int )from.Entity.EntityBaseId;
								to.PrimaryAgentUID = from.Entity.EntityUid;
								//
								to.Name = from.ConditionManifest.Name;
							}
							else
							{
								MapFromDB( from.ConditionManifest, to );
							}
							list.Add( to );
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetAll(int orgId)" );
			}
			return list;
		}//
		/// <summary>
		/// Get all the condition manifests for the parent entity (ex a credential)
		/// </summary>
		/// <param name="parentUid"></param>
		/// <returns></returns>
		//public static List<ThisEntity> GetAll( Guid parentUid, bool isForLinks )
		//{
		//	ThisEntity to = new ThisEntity();
		//	List<ThisEntity> list = new List<ThisEntity>();
		//	Entity parent = EntityManager.GetEntity( parentUid );
		//	if ( parent == null || parent.Id == 0 )
		//	{
		//		return list;
		//	}

		//	try
		//	{
		//		using ( var context = new EntityContext() )
		//		{
		//			//context.Configuration.LazyLoadingEnabled = false;

		//			List<EM.Entity_ConditionManifest> results = context.Entity_ConditionManifest
		//					.Where( s => s.EntityId == parent.Id )
		//					.OrderBy( s => s.Created )
		//					.ToList();

		//			if ( results != null && results.Count > 0 )
		//			{
		//				foreach ( EM.Entity_ConditionManifest from in results )
		//				{
		//					to = new ThisEntity();
		//					if ( isForLinks )
		//					{
		//						to.Id = from.Id;
		//						to.RowId = from.ConditionManifest.RowId;

		//						to.OrganizationId = ( int ) from.Entity.EntityBaseId;
		//						to.OwningAgentUid = from.Entity.EntityUid;
		//						to.ProfileName = from.ConditionManifest.Name;
		//					}
		//					else
		//					{
		//						MapFromDB( from.ConditionManifest, to );
		//					}
		//					list.Add( to );
		//				}
		//			}
		//		}
		//	}
		//	catch ( Exception ex )
		//	{
		//		LoggingHelper.LogError( ex, thisClassName + ".GetAll (Guid parentUid)" );
		//	}
		//	return list;
		//}//

		public static List<ThisEntity> Search( int orgId, int pageNumber, int pageSize, ref int pTotalRows )
		{
			ThisEntity to = new ThisEntity();
			List<ThisEntity> list = new List<ThisEntity>();
			Entity parent = EntityManager.GetEntity( 2, orgId );
			if ( parent == null || parent.Id == 0 )
			{
				return list;
			}
			try
			{
				using ( var context = new EntityContext() )
				{
					List<EM.Entity_ConditionManifest> results = context.Entity_ConditionManifest
							.Where( s => s.EntityId == parent.Id )
							.OrderBy( s => s.Created )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( EM.Entity_ConditionManifest from in results )
						{
							to = new ThisEntity();
							
							to.Id = from.ConditionManifestId;
							to.RowId = from.ConditionManifest.RowId;
							to.Description = from.ConditionManifest.Description;
							to.OrganizationId = ( int ) from.Entity.EntityBaseId;
							to.PrimaryAgentUID = from.Entity.EntityUid;
							to.Name = from.ConditionManifest.Name;
							
							list.Add( to );
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".Search()" );
			}

			return list;
		} //

		  /// <summary>
		  /// Search for a condition manifest.
		  /// Currently should only allow where owned by the same org as the owning org of the current context
		  /// </summary>
		  /// <param name="pFilter"></param>
		  /// <param name="pOrderBy"></param>
		  /// <param name="pageNumber"></param>
		  /// <param name="pageSize"></param>
		  /// <param name="userId"></param>
		  /// <param name="pTotalRows"></param>
		  /// <returns></returns>
		public static List<ThisEntity> MainSearch( string pFilter, string pOrderBy, int pageNumber, int pageSize, int userId, ref int pTotalRows )
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
					pFilter = string.Empty;
				}

				using ( SqlCommand command = new SqlCommand( "[ConditionManifest_Search]", c ) )
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
					item.OrganizationId = GetRowColumn( dr, "OrganizationId", 0 );
					item.Name = GetRowColumn( dr, "Name", "missing" );
					
					item.Description = GetRowColumn( dr, "Description", string.Empty );

					string rowId = GetRowColumn( dr, "RowId" );
					item.RowId = new Guid( rowId );
					item.CTID = GetRowColumn( dr, "CTID" );
					item.SubjectWebpage = GetRowColumn( dr, "CostDetails", string.Empty );
					

					list.Add( item );
				}

				return list;

			}
		} //


		public static void MapToDB( ThisEntity from, DBEntity to )
		{

			//want to ensure fields from create are not wiped
			if ( to.Id == 0 )
			{
				to.CTID = from.CTID;
			}

            if ( !string.IsNullOrWhiteSpace( from.CredentialRegistryId ) )
                to.CredentialRegistryId = from.CredentialRegistryId;
            //don't map rowId, ctid, or dates as not on form

            to.Id = from.Id;
			//Dont do here, do in caller
			//to.OrganizationId = from.OrganizationId;
			to.Name = GetData( from.Name );
			to.Description = GetData( from.Description );
			to.SubjectWebpage = GetUrlData( from.SubjectWebpage, null );

			

		}
		public static void MapFromDB( DBEntity input, ThisEntity output )
		{
			output.Id = input.Id;
			output.RowId = input.RowId;
			output.EntityStateId = ( int ) ( input.EntityStateId ?? 1 );

			output.OrganizationId = (int) (input.OrganizationId ?? 0);

			if ( output.OrganizationId > 0 )
			{
				output.PrimaryOrganization = OrganizationManager.GetForSummary( output.OrganizationId );
				if ( output.PrimaryOrganization != null && output.PrimaryOrganization.Id > 0 )
					output.PrimaryAgentUID = output.PrimaryOrganization.RowId;
			}

			output.Name = input.Name;
			output.FriendlyName = FormatFriendlyTitle( input.Name );

			output.Description = input.Description == null ? string.Empty : input.Description;
			
			output.CTID = input.CTID;
			output.CredentialRegistryId = input.CredentialRegistryId;

			output.SubjectWebpage = input.SubjectWebpage;
			
			if ( IsValidDate( input.Created ) )
				output.Created = ( DateTime ) input.Created;
		
			if ( IsValidDate( input.LastUpdated ) )
				output.LastUpdated = ( DateTime ) input.LastUpdated;
			//=====
			var relatedEntity = EntityManager.GetEntity( output.RowId, false );
			if ( relatedEntity != null && relatedEntity.Id > 0 )
				output.EntityLastUpdated = relatedEntity.LastUpdated;

			//NOTE: EntityLastUpdated should really be the last registry update now. Check how LastUpdated is assigned on import
			output.EntityLastUpdated = output.LastUpdated;
			//get common conditions
			//TODO - determine what to return for edit vs non-edit states
			//if ( forEditView )
			//	to.CommonConditions = Entity_CommonConditionManager.GetAll( to.RowId, forEditView );
			//else
			//	to.CommonConditions = Entity_CommonConditionManager.GetAll( to.RowId, forEditView );

			//get entry conditions
			List<ConditionProfile> list = Entity_ConditionProfileManager.GetAll( output.RowId, true );

			//??actions
			if ( list != null && list.Count > 0 )
			{
				foreach ( ConditionProfile item in list )
				{
					output.ConditionProfiles.Add(item);

					if ( item.ConnectionProfileTypeId == Entity_ConditionProfileManager.ConnectionProfileType_Requirement )
						output.Requires.Add( item );
					else if ( item.ConnectionProfileTypeId == Entity_ConditionProfileManager.ConnectionProfileType_Recommendation )
						output.Recommends.Add( item );
					else if ( item.ConnectionProfileTypeId == Entity_ConditionProfileManager.ConnectionProfileType_EntryCondition )
						output.EntryCondition.Add( item );
					else if ( item.ConnectionProfileTypeId == Entity_ConditionProfileManager.ConnectionProfileType_Corequisite )
						output.Corequisite.Add( item );
					else if ( item.ConnectionProfileTypeId == Entity_ConditionProfileManager.ConnectionProfileType_CoPrerequisite )
						output.CoPrerequisite.Add( item );
					else if ( item.ConnectionProfileTypeId == Entity_ConditionProfileManager.ConnectionProfileType_Renewal )
						output.Renewal.Add( item );
					else
					{
						EmailManager.NotifyAdmin( "Unexpected Condition Profile for Condition Manifest", string.Format( "conditionManifestId: {0}, ConditionProfileTypeId: {1}", output.Id, item.ConnectionProfileTypeId ) );
					}
				}
				//LoggingHelper.DoTrace( 5, "Unexpected Condition Profiles found for Condition Manifest. " + string.Format( "conditionManifestId: {0}, Count: {1}", to.Id, list.Count ) );
			}


		}

		#endregion

		#region === Entity_HasConditionManifest ================
		public bool HasConditionManifest_SaveList( List<int> list, Guid parentUid, ref SaveStatus status )
		{
			if ( list == null || list.Count == 0 )
				return true;

			bool isAllValid = true;
			foreach ( int profileId in list )
			{
				Entity_HasConditionManifest_Add( parentUid, profileId, ref status );
			}

			return isAllValid;
		}
		/// <summary>
		/// Add an Entity_CommonCondition
		/// </summary>
		/// <param name="parentUid"></param>
		/// <param name="profileId"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public int Entity_HasConditionManifest_Add( Guid parentUid,
					int profileId,
					ref SaveStatus status )
		{
			int id = 0;
			int count = 0;
			if ( profileId == 0 )
			{
				status.AddError( string.Format( "A valid ConditionManifest identifier was not provided to the {0}.Add method.", thisClassName ) );
				return 0;
			}
		
			Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				status.AddError( "Error - the parent entity was not found." );
				return 0;
			}
			using ( var context = new EntityContext() )
			{
				EM.Entity_ConditionManifest efEntity = new EM.Entity_ConditionManifest();
				try
				{
					//first check for duplicates
					efEntity = context.Entity_ConditionManifest
							.SingleOrDefault( s => s.EntityId == parent.Id
							&& s.ConditionManifestId == profileId );

					if ( efEntity != null && efEntity.Id > 0 )
					{
						//status.AddError( string.Format( "Error - this ConditionManifest has already been added to this profile.", thisClassName ) );
						return 0;
					}

					efEntity = new EM.Entity_ConditionManifest();
					efEntity.EntityId = parent.Id;
					efEntity.ConditionManifestId = profileId;

					efEntity.Created = System.DateTime.Now;

					context.Entity_ConditionManifest.Add( efEntity );

					// submit the change to database
					count = context.SaveChanges();
					if ( count > 0 )
					{
						id = efEntity.Id;
						return efEntity.Id;
					}
					else
					{
						//?no info on error
						status.AddError( thisClassName + "Error - the add was not successful." );
						string message = thisClassName + string.Format( ".Add Failed", "Attempted to add a ConditionManifest for a profile. The process appeared to not work, but there was no exception, so we have no message, or no clue. Parent Profile: {0}, Type: {1}, learningOppId: {2}", parentUid, parent.EntityType, profileId );
						EmailManager.NotifyAdmin( thisClassName + ".Add Failed", message );
					}
				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", "Entity_CommonCondition" );
					status.AddError( "Error - the save was not successful. " + message );
					LoggingHelper.LogError( dbex, thisClassName + string.Format( ".Save(), Parent: {0} ({1})", parent.EntityBaseName, parent.EntityBaseId ) );

				}
				catch ( Exception ex )
				{
					string message = FormatExceptions( ex );
					status.AddError( "Error - the save was not successful. " + message );
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Save(), Parent: {0} ({1})", parent.EntityBaseName, parent.EntityBaseId ) );

				}
			}
			return id;
		}
		public bool Delete_EntityConditionManifest( Guid parentUid, int profileId, ref string statusMessage )
		{
			bool isValid = false;
			if ( profileId == 0 )
			{
				statusMessage = "Error - missing an identifier for the Assessment to remove";
				return false;
			}
			//need to get Entity.Id 
			Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				statusMessage = "Error - the parent entity was not found.";
				return false;
			}

			using ( var context = new EntityContext() )
			{
				EM.Entity_ConditionManifest efEntity = context.Entity_ConditionManifest
								.SingleOrDefault( s => s.EntityId == parent.Id && s.ConditionManifestId == profileId );

				if ( efEntity != null && efEntity.Id > 0 )
				{
					context.Entity_ConditionManifest.Remove( efEntity );
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						isValid = true;
					}
				}
				else
				{
					statusMessage = "Warning - the record was not found - probably because the target had been previously deleted";
					isValid = true;
				}
			}

			return isValid;
		}

		public static Entity_ConditionManifest EntityConditionManifest_Get( int parentId, int profileId )
		{
			Entity_ConditionManifest entity = new Entity_ConditionManifest();
			if ( parentId < 1 || profileId < 1 )
			{
				return entity;
			}
			try
			{
				using ( var context = new EntityContext() )
				{
					EM.Entity_ConditionManifest from = context.Entity_ConditionManifest
							.SingleOrDefault( s => s.ConditionManifestId == profileId && s.EntityId == parentId );

					if ( from != null && from.Id > 0 )
					{
						entity.Id = from.Id;
						entity.ConditionManifestId = from.ConditionManifestId;
						entity.EntityId = from.EntityId;
						//entity.ConditionManifest = ConditionManifestManager.GetBasic( from.ConditionManifestId );
						entity.ProfileSummary = entity.ConditionManifest.Name;

						//entity.ConditionManifest = from.ConditionManifest;
						if ( IsValidDate( from.Created ) )
							entity.Created = ( DateTime ) from.Created;
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".EntityConditionManifest_Get" );
			}
			return entity;
		}//
		public static List<ThisEntity> GetAllManifests( Guid parentUid, bool forEditView )
		{
			List< ThisEntity> list = new List<ThisEntity>();
			ThisEntity entity = new ThisEntity();
			Entity parent = EntityManager.GetEntity( parentUid );

			try
			{
				using ( var context = new EntityContext() )
				{
					List<EM.Entity_ConditionManifest> results = context.Entity_ConditionManifest
							.Where( s => s.EntityId == parent.Id )
							.OrderBy( s => s.ConditionManifestId )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( EM.Entity_ConditionManifest item in results )
						{
							//TODO - optimize the appropriate MapFromDB methods
							entity = new ThisEntity();
							MapFromDB(item.ConditionManifest, entity);

							list.Add( entity );
						}
					}
					return list;
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetAllManifests" );
			}
			return list;
		}
		#endregion


	}
}

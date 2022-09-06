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

using ThisEntity = workIT.Models.Common.CostManifest;
using DBEntity = workIT.Data.Tables.CostManifest;
using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;

using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;
namespace workIT.Factories
{
	public class CostManifestManager : BaseFactory
	{
		static string thisClassName = "Factories.CostManifestManager";
		static string EntityType = "CostManifest";
		static int EntityTypeId = CodesManager.ENTITY_TYPE_COST_MANIFEST;
		EntityManager entityMgr = new EntityManager();

		#region === -Persistance ==================
		/// <summary>
		/// Persist CostManifest
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="parentUid"></param>
		/// <param name="userId"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public bool Save( ThisEntity entity,  ref SaveStatus status )
		{
			bool isValid = true;
			//will not have a parent Guid
			Guid parentUid = entity.OwningAgentUid;

			if ( !IsValidGuid( entity.OwningAgentUid ) )
			{
				status.AddError( "Error: the parent identifier was not provided." );
				return false;
			}

			int count = 0;
			DateTime lastUpdated = System.DateTime.Now;
			DBEntity efEntity = new DBEntity();
			int parentOrgId = 0;
			
			Guid conditionManifestParentUid = new Guid();
			Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				status.AddError( "Error - the parent entity was not found." );
				return false;
			}
			if ( parent.EntityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION )
			{
				parentOrgId = parent.EntityBaseId;
				conditionManifestParentUid = parent.EntityUid;
				//no common Cost in this context
			}

			else
			{
				//should not happen - error Cost
				status.AddError( "Error: the parent for a Cost Manifest must be an organization." );
				return false;
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
						status.AddWarning( "The Cost Manifest Profile is empty. " );
						return false;
					}

					if ( entity.Id == 0 )
					{
						//add
						efEntity = new DBEntity();
						MapToDB( entity, efEntity );
						efEntity.OrganizationId = parentOrgId;
						efEntity.EntityStateId = 3;
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

						context.CostManifest.Add( efEntity );
						count = context.SaveChanges();

						entity.Id = efEntity.Id;
						entity.RowId = efEntity.RowId;
						if ( count == 0 )
						{
							status.AddError( " Unable to add Cost Manifest Profile" );
						}
						else
						{
							entity.RowId = efEntity.RowId;
							entity.Created = efEntity.Created.Value;
							entity.LastUpdated = efEntity.LastUpdated.Value;
							entity.Id = efEntity.Id;
							UpdateEntityCache( entity, ref status );
							//
							if ( !UpdateParts( entity, ref status ) )
								isValid = false;

							SiteActivity sa = new SiteActivity()
							{
								ActivityType = "CostManifest",
								Activity = "Import",
								Event = "Add",
								Comment = string.Format( "CostManifest was added by the import. Name: {0}, SWP: {1}", entity.Name, entity.CostDetails ),
								ActivityObjectId = entity.Id
							};
							new ActivityManager().SiteActivityAdd( sa );

							//create the Entity.CostManifest
							//ensure to handle this properly when adding a commonCost CM to a CM
                            //18-10-10 mp - the HasCostManifest on org is a reverse property that should probably be ignored. This entry point should check if exists, if so, it is OK, with no message
							EntityCostManifest_Add( conditionManifestParentUid, efEntity.Id, ref status );

							// a trigger is used to create the entity Object. 
							
						}
					}
					else
					{

						efEntity = context.CostManifest.SingleOrDefault( s => s.Id == entity.Id );
						if ( efEntity != null && efEntity.Id > 0 )
						{
							//delete the entity and re-add
							//Entity e = new Entity()
							//{
							//	EntityBaseId = efEntity.Id,
							//	EntityTypeId = CodesManager.ENTITY_TYPE_COST_MANIFEST,
							//	EntityType = "CostManifest",
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
							if ( (efEntity.EntityStateId ?? 1) == 1 )
								efEntity.EntityStateId = 3;
                            //if started as a placeholder, may not have the org
                            efEntity.OrganizationId = parentOrgId;

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
							}
                            else
                            {
                                //update entity.LastUpdated - assuming there has to have been some change in related data
                                //new EntityManager().UpdateModifiedDate( entity.RowId, ref status, efEntity.LastUpdated );
                            }
							lastUpdated = status.LocalUpdatedDate;
							UpdateEntityCache( entity, ref status );
							//just in case
							EntityCostManifest_Add( conditionManifestParentUid, efEntity.Id, ref status );

							if ( !UpdateParts( entity, ref status ) )
								isValid = false;

							SiteActivity sa = new SiteActivity()
							{
								ActivityType = "CostManifest",
								Activity = "Import",
								Event = "Update",
								Comment = string.Format( "CostManifest was updated by the import. Name: {0}, SWP: {1}", entity.Name, entity.CostDetails ),
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

					string message = HandleDBValidationError( dbex, thisClassName + ".Save() ", "CostManifest" );
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

		public bool UpdateParts( ThisEntity entity, ref SaveStatus status )
		{
			status.HasSectionErrors = false;

			//CostProfile
			CostProfileManager cpm = new Factories.CostProfileManager();
			cpm.SaveList( entity.EstimatedCost, entity.RowId, ref status );

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
					efEntity.CostDetails = registryAtId;
					efEntity.Created = DateTime.Now;
					efEntity.LastUpdated = DateTime.Now;
					//
					context.CostManifest.Add( efEntity );
					//
					if ( context.SaveChanges() > 0 )
					{
						entity.Id = efEntity.Id;
						entity.RowId = efEntity.RowId;
						entity.CTID = efEntity.CTID;
						entity.EntityStateId = 1;
						entity.Name = efEntity.Name;
						entity.Description = efEntity.Description;
						entity.SubjectWebpage = efEntity.CostDetails;
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

		public void UpdateEntityCache( CostManifest document, ref SaveStatus status )
		{
			EntityCache ec = new EntityCache
			{
				EntityTypeId = CodesManager.ENTITY_TYPE_COST_MANIFEST,
				EntityType = EntityType,
				EntityStateId = document.EntityStateId,
				EntityUid = document.RowId,
				BaseId = document.Id,
				Description = document.Description,
				SubjectWebpage = document.SubjectWebpage,
				CTID = document.CTID,
				Created = document.Created,
				LastUpdated = document.LastUpdated,
				Name = document.Name,
				OwningAgentUID = document.OwningAgentUid,
				OwningOrgId = document.OrganizationId
			};
			string statusMessage = "";
			if ( new EntityManager().EntityCacheSave( ec, ref statusMessage ) == 0 )
			{
				status.AddError( thisClassName + string.Format( ".UpdateEntityCache for '{0}' ({1}) failed: {2}", document.Name, document.Id, statusMessage ) );
			}
		}

		/// <summary>
		/// Delete a Cost Manifest, and related Entity
		/// </summary>
		/// <param name="Id"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool Delete( int Id, ref string statusMessage )
		{
			bool isValid = false;
			if ( Id == 0 )
			{
				statusMessage = "Error - missing an identifier for the CostManifest";
				return false;
			}
			using ( var context = new EntityContext() )
			{
				try
				{
					DBEntity efEntity = context.CostManifest
								.SingleOrDefault( s => s.Id == Id );

					if ( efEntity != null && efEntity.Id > 0 )
					{
						Guid rowId = efEntity.RowId;

						context.CostManifest.Remove( efEntity );
						int count = context.SaveChanges();
						if ( count > 0 )
						{
							isValid = true;
							//do with trigger now
							//new EntityManager().Delete( rowId, ref statusMessage );
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
						statusMessage = "Error: this Cost Manifest cannot be deleted as it is being referenced by other items, such as roles or credentials. These associations must be removed before this Cost Manifest can be deleted.";
					}
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
				statusMessage = thisClassName + ".Delete() Error - a valid CTID must be provided";
				return false;
			}
		
            using ( var context = new EntityContext() )
			{
				try
				{
					context.Configuration.LazyLoadingEnabled = false;
					DBEntity efEntity = context.CostManifest
                                .FirstOrDefault( s => s.CTID == ctid );

                    if ( efEntity != null && efEntity.Id > 0 )
					{
						Guid rowId = efEntity.RowId;
						int orgId = efEntity.OrganizationId ?? 0;
						//need to remove from Entity.
						//-using before delete trigger - verify won't have RI issues
						string msg = string.Format( " CostManifest. Id: {0}, Name: {1}, Ctid: {2}.", efEntity.Id, efEntity.Name, efEntity.CTID );
                        //leaving as a delete
                        context.CostManifest.Remove( efEntity );
                        //efEntity.EntityStateId = 0;
                        //efEntity.LastUpdated = System.DateTime.Now;

                        int count = context.SaveChanges();
						if ( count >= 0 )
						{
                            new ActivityManager().SiteActivityAdd( new SiteActivity()
                            {
                                ActivityType = "CostManifest",
                                Activity = "Import",
                                Event = "Delete",
                                Comment = msg
                            } );
                            isValid = true;
							//delete cache
							new EntityManager().EntityCacheDelete( CodesManager.ENTITY_TYPE_COST_MANIFEST, efEntity.Id, ref statusMessage );
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
					LoggingHelper.LogError( ex, thisClassName + ".Delete()" );
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
			if ( string.IsNullOrWhiteSpace( profile.Name )
				&& string.IsNullOrWhiteSpace( profile.Description )
				&& string.IsNullOrWhiteSpace( profile.CostDetails)

				)
			{
				//isEmpty = true;
				//return isValid;
			}
			//&& ( profile.EstimatedCost == null || profile.EstimatedCost.Count == 0 )
			if ( string.IsNullOrWhiteSpace( profile.Name ) )
			{
				status.AddError( "A Cost Manifest name must be entered" );
			}
			if ( string.IsNullOrWhiteSpace( profile.Description ) )
			{
				status.AddError( "A Cost Manifest Description must be entered" );
			}


			if ( !IsUrlValid( profile.CostDetails, ref commonStatusMessage ) )
			{
				status.AddError( "The Cost Detail Url is invalid " + commonStatusMessage );
			}
			DateTime startDate = DateTime.Now;
			DateTime endDate = DateTime.Now;
			if ( !string.IsNullOrWhiteSpace( profile.StartDate ) )
			{
				if ( !IsValidDate( profile.StartDate ) )
					status.AddError( "Please enter a valid start date" );
				else
				{
					DateTime.TryParse( profile.StartDate, out startDate );
				}
			}
			if ( !string.IsNullOrWhiteSpace( profile.EndDate ) )
			{
				if ( !IsValidDate( profile.EndDate ) )
					status.AddError( "Please enter a valid end date" );
				else
				{
					DateTime.TryParse( profile.EndDate, out endDate );
					if ( IsValidDate( profile.StartDate )
						&& startDate > endDate )
						status.AddError( "The end date must be greater than the start date." );
				}
			}
		
			return status.WasSectionValid;
		}

		#endregion

		#region == Retrieval =======================
		public static ThisEntity GetByCtid( string ctid )
		{
			ThisEntity entity = new ThisEntity();
			if ( string.IsNullOrWhiteSpace( ctid ) )
				return entity;
			using ( var context = new EntityContext() )
			{
				DBEntity from = context.CostManifest
						.FirstOrDefault( s => s.CTID.ToLower() == ctid.ToLower().Trim() );

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
				DBEntity from = context.CostManifest
						.FirstOrDefault( s => s.CostDetails.ToLower() == swp.ToLower() );

				if ( from != null && from.Id > 0 )
				{
					MapFromDB( from, entity );
				}
			}
			return entity;
		}
		public static ThisEntity Get( int id )
		{
			ThisEntity entity = new ThisEntity();

			using ( var context = new EntityContext() )
			{
				DBEntity item = context.CostManifest
						.SingleOrDefault( s => s.Id == id );

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity);
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

				DBEntity item = context.CostManifest
						.SingleOrDefault( s => s.Id == id );

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity );
				}
			}

			return entity;
		}


		/// <summary>
		/// Get all the Cost manifests for the parent organization
		/// </summary>
		/// <param name="orgId"></param>
		/// <returns></returns>
		public static List<ThisEntity> GetAll( int orgId, bool isForLinks )
		{
			ThisEntity to = new ThisEntity();
			List<ThisEntity> list = new List<ThisEntity>();

			//Entity parent = EntityManager.GetEntity( 2, orgId );
			//if ( parent == null || parent.Id == 0 )
			//{
			//	return list;
			//}
			try
			{
				using ( var context = new EntityContext() )
				{
					var results = context.CostManifest
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
								to.OrganizationId = ( int ) from.OrganizationId;
								//to.OwningAgentUid = from.Entity.EntityUid;
								//
								to.Name = from.Name;
								to.Description = from.Description;
							}
							else
							{
								MapFromDB( from, to);
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
		/// Get all the Cost manifests for the parent entity (ex a credential)
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

		//			List<EM.Entity_CostManifest> results = context.Entity_CostManifest
		//					.Where( s => s.EntityId == parent.Id )
		//					.OrderBy( s => s.Created )
		//					.ToList();

		//			if ( results != null && results.Count > 0 )
		//			{
		//				foreach ( EM.Entity_CostManifest from in results )
		//				{
		//					to = new ThisEntity();
		//					if ( isForLinks )
		//					{
		//						to.Id = from.Id;
		//						to.RowId = from.CostManifest.RowId;

		//						to.OrganizationId = ( int ) from.Entity.EntityBaseId;
		//						to.OwningAgentUid = from.Entity.EntityUid;
		//						to.Name = from.CostManifest.Name;
		//					}
		//					else
		//					{
		//						MapFromDB( from.CostManifest, to );
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
					List<EM.Entity_CostManifest> results = context.Entity_CostManifest
							.Where( s => s.EntityId == parent.Id )
							.OrderBy( s => s.Created )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( EM.Entity_CostManifest from in results )
						{
							to = new ThisEntity();

							to.Id = from.CostManifestId;
							to.RowId = from.CostManifest.RowId;
							to.Description = from.CostManifest.Description;
							to.OrganizationId = ( int ) from.Entity.EntityBaseId;
							to.OwningAgentUid = from.Entity.EntityUid;
							to.Name = from.CostManifest.Name;

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
		/// Search for a cost manifest.
		/// Currently should only allow where owned by the same org as the owning org of the current context. Will be used by batch processes, such as publishing
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

				using ( SqlCommand command = new SqlCommand( "[CostManifest_Search]", c ) )
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

					item.Description = GetRowColumn( dr, "Description", "" );

					string rowId = GetRowColumn( dr, "RowId" );
					item.RowId = new Guid( rowId );
					item.CTID = GetRowColumn( dr, "CTID" );
					item.CostDetails = GetRowColumn( dr, "CostDetails", "" );


					list.Add( item );
				}

				return list;

			}
		} //

		public static void MapToDB( ThisEntity input, DBEntity output )
		{

			//want output ensure fields input create are not wiped
			if ( output.Id == 0 )
			{
				output.CTID = input.CTID;;
			}

            if ( !string.IsNullOrWhiteSpace( input.CredentialRegistryId ) )
                output.CredentialRegistryId = input.CredentialRegistryId;
            output.Id = input.Id;
			//Dont do here, do in caller
			//output.OrganizationId = input.OrganizationId;
			output.Name = GetData( input.Name );
			output.Description = GetData( input.Description );
			output.CostDetails = GetUrlData( input.CostDetails, null );
			if ( IsValidDate( input.StartDate ) )
				output.StartDate = DateTime.Parse( input.StartDate );
			else
				output.StartDate = null;

			if ( IsValidDate( input.EndDate ) )
				output.EndDate = DateTime.Parse( input.EndDate );
			else
				output.EndDate = null;


		}
		public static void MapFromDB( DBEntity from, ThisEntity to )
		{
			to.Id = from.Id;
			to.RowId = from.RowId;
			to.EntityStateId = ( int ) ( from.EntityStateId ?? 1 );

			to.OrganizationId =(int) (from.OrganizationId ?? 0);

			if ( to.OrganizationId > 0 )
			{
				to.OwningOrganization = OrganizationManager.GetForSummary( to.OrganizationId );
				if ( to.OwningOrganization != null && to.OwningOrganization.Id > 0 )
					to.OwningAgentUid = to.OwningOrganization.RowId;
			}

			to.Name = from.Name;
			to.FriendlyName = FormatFriendlyTitle( from.Name );

			to.Description = from.Description == null ? "" : from.Description;

			to.CTID = from.CTID;
			to.CredentialRegistryId = from.CredentialRegistryId;

			to.CostDetails = from.CostDetails;

			if ( IsValidDate( from.Created ) )
				to.Created = ( DateTime ) from.Created;
			
			if ( IsValidDate( from.LastUpdated ) )
				to.LastUpdated = ( DateTime ) from.LastUpdated;
			//=====
			var relatedEntity = EntityManager.GetEntity( to.RowId, false );
			if ( relatedEntity != null && relatedEntity.Id > 0 )
				to.EntityLastUpdated = relatedEntity.LastUpdated;
			if ( IsValidDate( from.StartDate ) )
				to.StartDate = ( ( DateTime ) from.StartDate ).ToString("yyyy-MM-dd");
			else
				to.StartDate = "";

			if ( IsValidDate( from.EndDate ) )
				to.EndDate = ( ( DateTime ) from.EndDate ).ToString("yyyy-MM-dd");
			else
				to.EndDate = "";
			//get common Costs
			//TODO - determine what to return for edit vs non-edit states
			//if ( forEditView )
			//	to.CommonCosts = Entity_CommonCostManager.GetAll( to.RowId, forEditView );
			//else
			//	to.CommonCosts = Entity_CommonCostManager.GetAll( to.RowId, forEditView );

			//get Costs
			//List<CostProfile> list = new List<CostProfile>();
			to.EstimatedCost = CostProfileManager.GetAll( to.RowId );


		}

		#endregion

		#region === Entity_HasCostManifest ================
		//**** see Entity_CommonCostManager ***
		public bool EntityCostManifest_SaveList( List<int> list, Guid parentUid, ref SaveStatus status )
		{
            Entity parent = EntityManager.GetEntity( parentUid );
            if ( parent == null || parent.Id == 0 )
            {
                status.AddError( "Error - the parent entity was not found." );
                return false;
            }
            //Entity_CostManifest is most likely added from CostManifest manager, don't deleted, in fact don't even call me!!!!
            //DeleteAll( parent, ref status );

            if ( list == null || list.Count == 0 )
				return true;

			bool isAllValid = true;
			foreach ( int profileId in list )
			{
				EntityCostManifest_Add( parent, profileId, ref status );
			}

			return isAllValid;
        }
        /// <summary>
        /// Add an Entity_CommonCost
        /// </summary>
        /// <param name="parentUid"></param>
        /// <param name="profileId"></param>
        /// <param name="messages"></param>
        /// <returns></returns>
        public int EntityCostManifest_Add( Guid parentUid,
                    int profileId,
                    ref SaveStatus status )
        {
            Entity parent = EntityManager.GetEntity( parentUid );
            if ( parent == null || parent.Id == 0 )
            {
                status.AddError( "Error - the parent entity was not found." );
                return 0;
            }
            //currently called from con
            return EntityCostManifest_Add( parent, profileId, ref status ); 
        }
		/// <summary>
		/// Add an Entity_CommonCost
		/// </summary>
		/// <param name="parentUid"></param>
		/// <param name="profileId"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public int EntityCostManifest_Add( Entity parent,
					int profileId,
					ref SaveStatus status )
		{
			int id = 0;
			int count = 0;
			if ( profileId == 0 )
			{
				status.AddError( string.Format( "A valid CostManifest identifier was not provided to the {0}.Add method.", thisClassName ) );
				return 0;
			}
			

			using ( var context = new EntityContext() )
			{
				EM.Entity_CostManifest efEntity = new EM.Entity_CostManifest();
				try
				{
					//first check for duplicates
					efEntity = context.Entity_CostManifest
							.SingleOrDefault( s => s.EntityId == parent.Id
							&& s.CostManifestId == profileId );

					if ( efEntity != null && efEntity.Id > 0 )
					{
						//status.AddError( string.Format( "Error - this CostManifest has already been added to this profile.", thisClassName ) );
						return 0;
					}

					efEntity = new EM.Entity_CostManifest();
					efEntity.EntityId = parent.Id;
					efEntity.CostManifestId = profileId;

					efEntity.Created = System.DateTime.Now;

					context.Entity_CostManifest.Add( efEntity );

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
						string message = thisClassName + string.Format( ".Add Failed", "Attempted to add a CostManifest for a profile. The process appeared to not work, but there was no exception, so we have no message, or no clue. Parent Profile: {0}, Type: {1}, learningOppId: {2}", parent.EntityUid, parent.EntityType, profileId );
						EmailManager.NotifyAdmin( thisClassName + ".Add Failed", message );
					}
				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", "Entity_CommonCost" );
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
        public bool DeleteAll( Entity parent, ref SaveStatus status )
        {
            bool isValid = true;
            //Entity parent = EntityManager.GetEntity( parentUid );
            if ( parent == null || parent.Id == 0 )
            {
                status.AddError( thisClassName + ". Error - the provided target parent entity was not provided." );
                return false;
            }
            using ( var context = new EntityContext() )
            {
                context.Entity_CostManifest.RemoveRange( context.Entity_CostManifest.Where( s => s.EntityId == parent.Id ) );
                int count = context.SaveChanges();
                if ( count > 0 )
                {
                    isValid = true;
                }
                else
                {
                    //if doing a delete on spec, may not have been any properties
                }
            }

            return isValid;
        }

        public bool Delete_EntityCostManifest( Guid parentUid, int profileId, ref string statusMessage )
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
				EM.Entity_CostManifest efEntity = context.Entity_CostManifest
								.SingleOrDefault( s => s.EntityId == parent.Id && s.CostManifestId == profileId );

				if ( efEntity != null && efEntity.Id > 0 )
				{
					context.Entity_CostManifest.Remove( efEntity );
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

		public static Entity_CostManifest EntityCostManifest_Get( int parentId, int profileId )
		{
			Entity_CostManifest entity = new Entity_CostManifest();
			if ( parentId < 1 || profileId < 1 )
			{
				return entity;
			}
			try
			{
				using ( var context = new EntityContext() )
				{
					EM.Entity_CostManifest from = context.Entity_CostManifest
							.SingleOrDefault( s => s.CostManifestId == profileId && s.EntityId == parentId );

					if ( from != null && from.Id > 0 )
					{
						entity.Id = from.Id;
						entity.CostManifestId = from.CostManifestId;
						entity.EntityId = from.EntityId;
						//entity.CostManifest = CostManifestManager.GetBasic( from.CostManifestId );
						entity.ProfileSummary = entity.CostManifest.Name;

						//entity.CostManifest = from.CostManifest;
						if ( IsValidDate( from.Created ) )
							entity.Created = ( DateTime ) from.Created;
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".EntityCostManifest_Get" );
			}
			return entity;
		}//

		#endregion


	}
}

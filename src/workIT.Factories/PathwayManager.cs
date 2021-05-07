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

using ThisEntity = workIT.Models.Common.Pathway;
using DBEntity = workIT.Data.Tables.Pathway;
using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;

using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;


namespace workIT.Factories
{
	public class PathwayManager : BaseFactory
	{
		static readonly string thisClassName = "PathwayManager";

		#region Pathway - persistance ==================
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
						DBEntity efEntity = context.Pathway
								.SingleOrDefault( s => s.Id == entity.Id );

						if ( efEntity != null && efEntity.Id > 0 )
						{
							//fill in fields that may not be in entity
							entity.RowId = efEntity.RowId;

							MapToDB( entity, efEntity );

							//19-05-21 mp - should add a check for an update where currently is deleted
							if ( ( efEntity.EntityStateId ?? 0 ) == 0 )
							{
								var url = string.Format( UtilityManager.GetAppKeyValue( "credentialFinderSite" ) + "pathway/{0}", efEntity.Id );
								//notify, and???
								//EmailManager.NotifyAdmin( "Previously Deleted Pathway has been reactivated", string.Format( "<a href='{2}'>Pathway: {0} ({1})</a> was deleted and has now been reactivated.", efEntity.Name, efEntity.Id, url ) );
								SiteActivity sa = new SiteActivity()
								{
									ActivityType = "Pathway",
									Activity = "Import",
									Event = "Reactivate",
									Comment = string.Format( "Pathway had been marked as deleted, and was reactivted by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ),
									ActivityObjectId = entity.Id
								};
								new ActivityManager().SiteActivityAdd( sa );
							}
							//assume and validate, that if we get here we have a full record
							if ( ( efEntity.EntityStateId ?? 1 ) != 2 )
								efEntity.EntityStateId = 3;

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

									isValid = false;
									string message = string.Format( thisClassName + ".Save Failed", "Attempted to update a Pathway. The process appeared to not work, but was not an exception, so we have no message, or no clue. Pathway: {0}, Id: {1}", entity.Name, entity.Id );
									status.AddError( "Error - the update was not successful. " + message );
									EmailManager.NotifyAdmin( thisClassName + ".Save Failed Failed", message );
								}

							}
							else
							{
								//update entity.LastUpdated - assuming there has to have been some change in related data
								//new EntityManager().UpdateModifiedDate( entity.RowId, ref status );
							}
							if ( isValid )
							{
								if ( !UpdateParts( entity, ref status ) )
									isValid = false;

								SiteActivity sa = new SiteActivity()
								{
									ActivityType = "Pathway",
									Activity = "Import",
									Event = "Update",
									Comment = string.Format( "Pathway was updated by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ),
									ActivityObjectId = entity.Id
								};
								new ActivityManager().SiteActivityAdd( sa );

								//if ( isValid || partsUpdateIsValid )
								new EntityManager().UpdateModifiedDate( entity.RowId, ref status, efEntity.LastUpdated );
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
						int newId = Add( entity, ref status );
						if ( newId == 0 || status.HasErrors )
							isValid = false;
					}
				}
			}
			catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
			{
				string message = HandleDBValidationError( dbex, thisClassName + string.Format( ".Save. id: {0}, Name: {1}", entity.Id, entity.Name ), "Pathway" );
				status.AddError( thisClassName + ".Save(). Error - the save was not successful. " + message );
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
		/// add a Pathway
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
					MapToDB( entity, efEntity );

					if ( IsValidGuid( entity.RowId ) )
						efEntity.RowId = entity.RowId;
					else
						efEntity.RowId = Guid.NewGuid();
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

					context.Pathway.Add( efEntity );

					// submit the change to database
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						entity.Id = efEntity.Id;
						entity.RowId = efEntity.RowId;
						//add log entry
						SiteActivity sa = new SiteActivity()
						{
							ActivityType = "Pathway",
							Activity = "Import",
							Event = "Add",
							Comment = string.Format( "Full Pathway was added by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ),
							ActivityObjectId = entity.Id
						};
						new ActivityManager().SiteActivityAdd( sa );

						if ( UpdateParts( entity, ref status ) == false )
						{
						}

						return efEntity.Id;
					}
					else
					{
						//?no info on error

						string message = thisClassName + string.Format( ". Add Failed", "Attempted to add a Pathway. The process appeared to not work, but was not an exception, so we have no message, or no clue. Pathway: {0}, ctid: {1}", entity.Name, entity.CTID );
						status.AddError( thisClassName + ". Error - the add was not successful. " + message );
						EmailManager.NotifyAdmin( "PathwayManager. Add Failed", message );
					}
				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", "Pathway" );
					status.AddError( thisClassName + ".Add(). Error - the save was not successful. " + message );

					LoggingHelper.LogError( message, true );
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

					context.Pathway.Add( efEntity );
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
				status.AddError( "An Pathway name must be entered" );
			}
			if ( string.IsNullOrWhiteSpace( profile.Description ) )
			{
				status.AddWarning( "An Pathway Description must be entered" );
			}
			if ( !IsValidGuid( profile.OwningAgentUid ) )
			{
				status.AddWarning( "An owning organization must be selected" );
			}

			if ( string.IsNullOrWhiteSpace( profile.SubjectWebpage ) )
				status.AddWarning( "Error - A Subject Webpage name must be entered" );

			else if ( !IsUrlValid( profile.SubjectWebpage, ref commonStatusMessage ) )
			{
				status.AddWarning( "The Pathway Subject Webpage is invalid. " + commonStatusMessage );
			}

			return status.WasSectionValid;
		}
		public bool Delete( string envelopeId, string ctid, ref string statusMessage )
		{
			bool isValid = true;
			if ( ( string.IsNullOrWhiteSpace( envelopeId ) || !IsValidGuid( envelopeId ) )
				&& string.IsNullOrWhiteSpace( ctid ) )
			{
				statusMessage = thisClassName + ".Delete() Error - a valid envelope identifier must be provided - OR  valid CTID";
				return false;
			}
			if ( string.IsNullOrWhiteSpace( envelopeId ) )
				envelopeId = "SKIP ME";
			if ( string.IsNullOrWhiteSpace( ctid ) )
				ctid = "SKIP ME";
			int orgId = 0;
			Guid orgUid = new Guid();
			using ( var context = new EntityContext() )
			{
				try
				{
					context.Configuration.LazyLoadingEnabled = false;
					DBEntity efEntity = context.Pathway
								.FirstOrDefault( s => s.CredentialRegistryId == envelopeId
								|| ( s.CTID == ctid )
								);

					if ( efEntity != null && efEntity.Id > 0 )
					{
						Guid rowId = efEntity.RowId;
						if ( IsValidGuid( efEntity.OwningAgentUid ) )
						{
							Organization org = OrganizationManager.GetBasics( ( Guid )efEntity.OwningAgentUid );
							orgId = org.Id;
							orgUid = org.RowId;
						}
						//need to remove from Entity.
						//-using before delete trigger - verify won't have RI issues
						string msg = string.Format( " Pathway. Id: {0}, Name: {1}, Ctid: {2}.", efEntity.Id, efEntity.Name, efEntity.CTID );
						//18-04-05 mparsons - change to set inactive, and notify - seems to have been some incorrect deletes
						//context.Pathway.Remove( efEntity );
						efEntity.EntityStateId = 0;
						efEntity.LastUpdated = System.DateTime.Now;
						int count = context.SaveChanges();
						if ( count > 0 )
						{
							new ActivityManager().SiteActivityAdd( new SiteActivity()
							{
								ActivityType = "Pathway",
								Activity = "Import",
								Event = "Delete",
								Comment = msg,
								ActivityObjectId = efEntity.Id
							} );
							isValid = true;
							//add pending request 
							List<String> messages = new List<string>();
							new SearchPendingReindexManager().AddDeleteRequest( CodesManager.ENTITY_TYPE_PATHWAY, efEntity.Id, ref messages );
							//mark owning org for updates (actually should be covered by ReindexAgentForDeletedArtifact
							new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_ORGANIZATION, orgId, 1, ref messages );

							//delete all relationships
							workIT.Models.SaveStatus status = new SaveStatus();
							Entity_AgentRelationshipManager earmgr = new Entity_AgentRelationshipManager();
							earmgr.DeleteAll( rowId, ref status );
							//also check for any relationships
							//There could be other orgs from relationships to be reindexed as well!

							//also check for any relationships
							new Entity_AgentRelationshipManager().ReindexAgentForDeletedArtifact( orgUid );
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
						statusMessage = "Error: this Pathway cannot be deleted as it is being referenced by other items, such as roles or credentials. These associations must be removed before this Pathway can be deleted.";
					}
				}
			}
			return isValid;
		}


		public bool UpdateParts( ThisEntity entity, ref SaveStatus status )
		{
			bool isAllValid = true;
			Entity_AgentRelationshipManager mgr = new Entity_AgentRelationshipManager();
			Entity relatedEntity = EntityManager.GetEntity( entity.RowId );
			if ( relatedEntity == null || relatedEntity.Id == 0 )
			{
				status.AddError( "Error - the related Entity was not found." );
				return false;
			}
			mgr.DeleteAll( relatedEntity, ref status );

			mgr.SaveList( relatedEntity.Id, Entity_AgentRelationshipManager.ROLE_TYPE_OWNER, entity.OwnedBy, ref status );

			//initial plan is store whole payload as json

			Entity_ReferenceFrameworkManager erfm = new Entity_ReferenceFrameworkManager();
			erfm.DeleteAll( relatedEntity, ref status );

			if ( erfm.SaveList( relatedEntity.Id, CodesManager.PROPERTY_CATEGORY_SOC, entity.Occupations, ref status ) == false )
				isAllValid = false;
			if ( erfm.SaveList( relatedEntity.Id, CodesManager.PROPERTY_CATEGORY_NAICS, entity.Industries, ref status ) == false )
				isAllValid = false;

			//
			Entity_ReferenceManager erm = new Entity_ReferenceManager();
			erm.DeleteAll( relatedEntity, ref status );
			if ( erm.Add( entity.Subject, entity.RowId, CodesManager.ENTITY_TYPE_PATHWAY, ref status, CodesManager.PROPERTY_CATEGORY_SUBJECT, false ) == false )
				isAllValid = false;

			if ( erm.Add( entity.Keyword, entity.RowId, CodesManager.ENTITY_TYPE_PATHWAY, ref status, CodesManager.PROPERTY_CATEGORY_KEYWORD, false ) == false )
				isAllValid = false;

			return isAllValid;
		}

		#endregion



		#region == Retrieval =======================
		public static ThisEntity GetByCtid( string ctid )
		{
			ThisEntity entity = new ThisEntity();
			using ( var context = new EntityContext() )
			{
				DBEntity from = context.Pathway
						.FirstOrDefault( s => s.CTID.ToLower() == ctid.ToLower() );

				if ( from != null && from.Id > 0 )
				{
					entity.RowId = from.RowId;
					entity.Id = from.Id;
					entity.EntityStateId = ( int )( from.EntityStateId ?? 1 );
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
				DBEntity from = context.Pathway
						.FirstOrDefault( s => s.SubjectWebpage.ToLower() == swp.ToLower() );

				if ( from != null && from.Id > 0 )
				{
					entity.RowId = from.RowId;
					entity.Id = from.Id;
					entity.Name = from.Name;
					entity.EntityStateId = ( int )( from.EntityStateId ?? 1 );
					entity.Description = from.Description;
					entity.SubjectWebpage = from.SubjectWebpage;

					entity.CTID = from.CTID;
					entity.CredentialRegistryId = from.CredentialRegistryId;
				}
			}
			return entity;
		}
		public static ThisEntity GetByName_SubjectWebpage( string name, string swp )
		{
			ThisEntity entity = new ThisEntity();
			using ( var context = new EntityContext() )
			{
				context.Configuration.LazyLoadingEnabled = false;
				DBEntity from = context.Pathway
						.FirstOrDefault( s => s.Name.ToLower() == name.ToLower() && s.SubjectWebpage.ToLower() == swp.ToLower() );

				if ( from != null && from.Id > 0 )
				{
					entity.RowId = from.RowId;
					entity.Id = from.Id;
					entity.Name = from.Name;
					entity.EntityStateId = ( int )( from.EntityStateId ?? 1 );
					entity.Description = from.Description;
					entity.SubjectWebpage = from.SubjectWebpage;

					entity.CTID = from.CTID;
					entity.CredentialRegistryId = from.CredentialRegistryId;
				}
			}
			return entity;
		}
		public static ThisEntity GetBasic( int id )
		{
			ThisEntity entity = new ThisEntity();
			using ( var context = new EntityContext() )
			{
				DBEntity item = context.Pathway
						.SingleOrDefault( s => s.Id == id );

				if ( item != null && item.Id > 0 )
				{
					MapFromDB_Basic( item, entity, false );
				}
			}

			return entity;
		}
		public static ThisEntity GetBasic( Guid guid )
		{
			ThisEntity entity = new ThisEntity();
			//Guid guid = new Guid( id );
			using ( var context = new EntityContext() )
			{
				DBEntity item = context.Pathway
						.SingleOrDefault( s => s.RowId == guid );

				if ( item != null && item.Id > 0 )
				{
					MapFromDB_Basic( item, entity, false );
				}
			}

			return entity;
		}
		public static List<ThisEntity> GetAllForOwningOrg( Guid owningOrgUid, ref int totalRecords, int maxRecords = 100 )
		{
			List<ThisEntity> list = new List<ThisEntity>();
			ThisEntity entity = new ThisEntity();
			using ( var context = new EntityContext() )
			{
				List<DBEntity> results = context.Pathway
							 .Where( s => s.OwningAgentUid == owningOrgUid )
							 .OrderBy( s => s.Name )
							 .ToList();
				if ( results != null && results.Count > 0 )
				{
					totalRecords = results.Count();

					foreach ( DBEntity item in results )
					{
						entity = new ThisEntity();
						MapFromDB_Basic( item, entity, false );
						list.Add( entity );
						if ( maxRecords > 0 && list.Count >= maxRecords )
							break;
					}
				}
			}

			return list;
		}
		public static int Count_ForOwningOrg( Guid orgUid )
		{
			int totalRecords = 0;

			using ( var context = new EntityContext() )
			{
				var results = context.Pathway
							.Where( s => s.OwningAgentUid == orgUid && s.EntityStateId == 3 )
							.ToList();

				if ( results != null && results.Count > 0 )
				{
					totalRecords = results.Count();
				}
			}
			return totalRecords;
		}
		public static List<ThisEntity> GetAll( ref int totalRecords, int maxRecords = 100 )
		{
			List<ThisEntity> list = new List<ThisEntity>();
			ThisEntity entity = new ThisEntity();
			using ( var context = new EntityContext() )
			{
				List<DBEntity> results = context.Pathway
							 .Where( s => s.EntityStateId > 2 )
							 .OrderBy( s => s.Name )
							 .ToList();
				if ( results != null && results.Count > 0 )
				{
					totalRecords = results.Count();

					foreach ( DBEntity item in results )
					{
						entity = new ThisEntity();
						MapFromDB_Basic( item, entity, false );
						list.Add( entity );
						if ( maxRecords > 0 && list.Count >= maxRecords )
							break;
					}
				}
			}

			return list;
		}
		//
		public static List<ThisEntity> GetAllForProgressionModel( string progressionModelCTID )
		{

			Pathway entity = new Pathway();
			var output = new List<Pathway>();

			using ( var context = new EntityContext() )
			{
				context.Configuration.LazyLoadingEnabled = false;
				var list = context.Pathway
							.Where( s => s.HasProgressionModel == progressionModelCTID )
							.ToList();
				foreach ( var item in list )
				{
					entity = new Pathway();
					//
					MapFromDB( item, entity, false );
					output.Add( entity );
				}
			}

			return output;
		}
		//
		public static ThisEntity GetDetails( int id )
		{
			ThisEntity entity = new ThisEntity();
			using ( var context = new EntityContext() )
			{
				DBEntity item = context.Pathway
						.SingleOrDefault( s => s.Id == id );

				if ( item != null && item.Id > 0 )
				{
					//check for virtual deletes
					if ( item.EntityStateId == 0 )
						return entity;

					MapFromDB( item, entity,true );
				}
			}

			return entity;
		}


		public static List<object> Autocomplete( string pFilter, int pageNumber, int pageSize, ref int pTotalRows )
		{
			bool autocomplete = true;
			List<object> results = new List<object>();
			List<string> competencyList = new List<string>();
			//ref competencyList, 
			List<ThisEntity> list = Search( pFilter, "", pageNumber, pageSize, ref pTotalRows, autocomplete );
			bool appendingOrgNameToAutocomplete = UtilityManager.GetAppKeyValue( "appendingOrgNameToAutocomplete", false );
			string prevName = "";
			foreach ( Pathway item in list )
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
		/// <summary>
		/// Search for Pathways
		/// </summary>
		/// <returns></returns>
		//public static List<ThisEntity> QuickSearch( int userId, string keyword, int pageNumber, int pageSize, ref int pTotalRows )
		//{
		//	List<ThisEntity> list = new List<ThisEntity>();
		//	ThisEntity entity = new ThisEntity();
		//	keyword = string.IsNullOrWhiteSpace( keyword ) ? "" : keyword.Trim();
		//	if ( pageSize == 0 )
		//		pageSize = 500;
		//	int skip = 0;
		//	if ( pageNumber > 1 )
		//		skip = ( pageNumber - 1 ) * pageSize;

		//	using ( var context = new EntityContext() )
		//	{
		//		var Query = from Results in context.Pathway
		//				.Where( s => keyword == "" || s.Name.Contains( keyword ) )
		//				.OrderBy( s => s.Name )
		//				select Results;
		//		pTotalRows = Query.Count();
		//		var results = Query.Skip(skip).Take( pageSize )
		//			.ToList();

		//		//List<DBEntity> results = context.Pathway
		//		//	.Where( s => keyword == "" || s.Name.Contains( keyword ) )
		//		//	.Take( pageSize )
		//		//	.OrderBy( s => s.Name )
		//		//	.ToList();

		//		if ( results != null && results.Count > 0 )
		//		{
		//			foreach ( DBEntity item in results )
		//			{
		//				entity = new ThisEntity();
		//				MapFromDB( item, entity,
		//						false, //includingProperties
		//						false, //includingRoles
		//						false //includeWhereUsed
		//						 );
		//				list.Add( entity );
		//			}

		//			//Other parts
		//		}
		//	}

		//	return list;
		//}

		public static List<ThisEntity> Search( string pFilter, string pOrderBy, int pageNumber, int pageSize, ref int pTotalRows, bool autocomplete = false )
		{
			string connectionString = DBConnectionRO();
			ThisEntity item = new ThisEntity();
			List<ThisEntity> list = new List<ThisEntity>();
			var result = new DataTable();
			string org = "";
			int orgId = 0;
			int rowNbr = ( pageNumber - 1 ) * pageSize;

			using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				c.Open();

				if ( string.IsNullOrEmpty( pFilter ) )
				{
					pFilter = "";
				}

				using ( SqlCommand command = new SqlCommand( "[Pathway.ElasticSearch]", c ) )
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
						string rows = command.Parameters[ command.Parameters.Count - 1 ].Value.ToString();

						pTotalRows = Int32.Parse( rows );
					}
					catch ( Exception ex )
					{
						pTotalRows = 0;
						LoggingHelper.LogError( ex, thisClassName + string.Format( ".Search() - Execute proc, Message: {0} \r\n Filter: {1} \r\n", ex.Message, pFilter ) );

						item = new Pathway();
						item.Name = "Unexpected error encountered. System administration has been notified. Please try again later. ";
						item.Description = ex.Message;

						list.Add( item );
						return list;
					}
				}
				//					ResultNumber = rowNbr,

				foreach ( DataRow dr in result.Rows )
				{
					rowNbr++;

					item = new ThisEntity();
					item.ResultNumber = rowNbr;

					item.Id = GetRowColumn( dr, "Id", 0 );
					item.Name = GetRowColumn( dr, "Name", "missing" );
					item.FriendlyName = FormatFriendlyTitle( item.Name );
					item.PrimaryOrganizationName = GetRowColumn( dr, "OrganizationName", "" );
					item.OrganizationId = GetRowColumn( dr, "Id", 0 );
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
					item.CTID = GetRowPossibleColumn( dr, "CTID", "" );
					item.CredentialRegistryId = GetRowPossibleColumn( dr, "CredentialRegistryId", "" );

					//
					//item.Subjects = GetRowColumn( dr, "SubjectAreas", "" );

					org = GetRowPossibleColumn( dr, "Organization", "" );
					orgId = GetRowPossibleColumn( dr, "OrgId", 0 );


					if ( orgId > 0 )
						item.OwningOrganization = new Organization() { Id = orgId, Name = org };

					item.Created = GetRowColumn( dr, "Created", System.DateTime.MinValue );
					item.LastUpdated = GetRowColumn( dr, "LastUpdated", System.DateTime.MinValue );

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
				output.CTID = input.CTID;
			}
			if ( !string.IsNullOrWhiteSpace( input.CredentialRegistryId ) )
				output.CredentialRegistryId = input.CredentialRegistryId;

			output.Id = input.Id;
			output.Name = GetData( input.Name );
			output.Description = GetData( input.Description );
			output.SubjectWebpage = GetUrlData( input.SubjectWebpage );
			output.HasProgressionModel = input.ProgressionModelURI;

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
				//input.OwningOrganizationId = org.Id;
			}
			else
			{
				//always have output have an owner
				//output.OwningAgentUid = null;
			}

			//if ( input.InLanguageId > 0 )
			//	output.InLanguageId = input.InLanguageId;
			//else if ( !string.IsNullOrWhiteSpace( input.InLanguage ) )
			//{
			//	output.InLanguageId = CodesManager.GetLanguageId( input.InLanguage );
			//}
			//else


			//======================================================================

		}

		public static void MapFromDB( DBEntity from, ThisEntity to, bool includingComponents, bool includingExtra = true)
		{
			MapFromDB_Basic( from, to, includingComponents );

			to.CredentialRegistryId = from.CredentialRegistryId;

			//=============================
			if ( includingExtra )
			{
				to.Subject = Entity_ReferenceManager.GetAll( to.RowId, CodesManager.PROPERTY_CATEGORY_SUBJECT );

				to.Keyword = Entity_ReferenceManager.GetAll( to.RowId, CodesManager.PROPERTY_CATEGORY_KEYWORD );
				//properties

				try
				{

					to.OccupationType = Reference_FrameworksManager.FillEnumeration( to.RowId, CodesManager.PROPERTY_CATEGORY_SOC );
					to.AlternativeOccupations = Entity_ReferenceManager.GetAll( to.RowId, CodesManager.PROPERTY_CATEGORY_SOC );

					to.IndustryType = Reference_FrameworksManager.FillEnumeration( to.RowId, CodesManager.PROPERTY_CATEGORY_NAICS );
					to.AlternativeIndustries = Entity_ReferenceManager.GetAll( to.RowId, CodesManager.PROPERTY_CATEGORY_NAICS );

				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".MapFromDB_B(), Name: {0} ({1})", to.Name, to.Id ) );
					to.StatusMessage = FormatExceptions( ex );
				}
			}

		}


		public static void MapFromDB_Basic( DBEntity input, ThisEntity output, bool includingComponents )
		{
			output.Id = input.Id;
			output.RowId = input.RowId;
			output.EntityStateId = ( int )( input.EntityStateId ?? 1 );
			output.Name = input.Name;
			output.Description = input.Description == null ? "" : input.Description;
			output.CTID = input.CTID;
			if ( IsGuidValid( input.OwningAgentUid ) )
			{
				output.OwningAgentUid = ( Guid )input.OwningAgentUid;
				output.OwningOrganization = OrganizationManager.GetForSummary( output.OwningAgentUid );
				output.OrganizationId = output.OwningOrganization.Id;
				//get roles
				OrganizationRoleProfile orp = Entity_AgentRelationshipManager.AgentEntityRole_GetAsEnumerationFromCSV( output.RowId, output.OwningAgentUid );
				output.OwnerRoles = orp.AgentRole;
			}
			//
			output.OrganizationRole = Entity_AgentRelationshipManager.AgentEntityRole_GetAll_ToEnumeration( output.RowId, true );
			//
			output.SubjectWebpage = input.SubjectWebpage;
			if ( IsValidDate( input.Created ) )
				output.Created = ( DateTime )input.Created;
			if ( IsValidDate( input.LastUpdated ) )
				output.LastUpdated = ( DateTime )input.LastUpdated;



			if ( string.IsNullOrWhiteSpace( output.CTID ) || output.EntityStateId < 3 )
			{
				output.IsReferenceVersion = true;
				return;
			}
			//=====
			var relatedEntity = EntityManager.GetEntity( output.RowId, false );
			if ( relatedEntity != null && relatedEntity.Id > 0 )
				output.EntityLastUpdated = relatedEntity.LastUpdated;

			//components
			if ( includingComponents )
			{
				//
				output.ProgressionModelURI = input.HasProgressionModel;
				if ( !string.IsNullOrWhiteSpace( output.ProgressionModelURI ) && includingComponents )
				{
					//ensure this is not called always from ProgressionModel/CS or will get a stack overflow
					output.HasProgressionModel = ConceptSchemeManager.GetByCtid( output.ProgressionModelURI );
				}

				//include conditions
				//to.HasPart = PathwayComponentManager.GetAllForPathway( to.CTID, PathwayComponentManager.componentActionOfDeep );
				//actual may be better to do through Entity_PathwayComponent
				//	but only destination component is under pathway
				//will there be an issue with recursion?
				//compare
				//one less that parts???
				//var parts1 = PathwayComponentManager.GetAllForPathway( to.CTID, PathwayComponentManager.componentActionOfDeep );
				//and
				var parts = Entity_PathwayComponentManager.GetAll( output.RowId, PathwayComponentManager.componentActionOfDeep );
				//may want to split out here, do in context

				foreach ( var item in parts )
				{
					if ( item.ComponentRelationshipTypeId == PathwayComponent.PathwayComponentRelationship_HasDestinationComponent )
						output.HasDestinationComponent.Add( item );
					else if ( item.ComponentRelationshipTypeId == PathwayComponent.PathwayComponentRelationship_HasChild )
						output.HasChild.Add( item );

				}
				//now get a unique list 
				//var parts = to.HasPart;
				output.HasPart = new List<PathwayComponent>();
				foreach ( var item in parts )
				{
					int index = output.HasPart.FindIndex( a => a.CTID == item.CTID );
					if ( index == -1 )
					{
						output.HasPart.Add( item );
					}
				}



			}
		} //


		#endregion


	}
}

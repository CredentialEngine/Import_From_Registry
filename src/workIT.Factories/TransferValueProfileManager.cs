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
		#region --- TransferValueProfileManager ---
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
			Entity_AgentRelationshipManager mgr = new Entity_AgentRelationshipManager();
			Entity relatedEntity = EntityManager.GetEntity( entity.RowId );
			if ( relatedEntity == null || relatedEntity.Id == 0 )
			{
				status.AddError( "Error - the related Entity was not found." );
				return false;
			}
			mgr.DeleteAll( relatedEntity, ref status );
			mgr.SaveList( relatedEntity.Id, Entity_AgentRelationshipManager.ROLE_TYPE_OWNER, entity.OwnedBy, ref status );
			//consider storing the class properties as Json!

			//delete all Entity.Lopp, .Cred, and .Assessment relationships, and then add?
			//would be convenient if a delete wasn't necessary
			//NOTE: this will leave orphan reference objects. Will need to clean up. 
			//could check if target is a reference. If so delete, or check if there are other references
			//NOTE: this should have been done in TransferValueServices.HandlingExistingEntity - is done corrently, remove this
			Entity_CredentialManager ecm = new Entity_CredentialManager();
			ecm.DeleteAll( relatedEntity, ref status );
			//
			var eam = new Entity_AssessmentManager();
			eam.DeleteAll( relatedEntity, ref status );
			//
			var elom = new Entity_LearningOpportunityManager();
			elom.DeleteAll( relatedEntity, ref status );
			//
			foreach ( var item in entity.TransferValueFromImport )
			{
				int newId = 0;
				var from = EntityManager.GetEntity( item, false );
				if ( from == null || from.Id == 0 )
				{
					//??
					continue;
				}
				if ( from.EntityTypeId == 1 )
				{
					ecm.Add( entity.RowId, from.EntityBaseId, BaseFactory.RELATIONSHIP_TYPE_IS_PART_OF, ref newId, ref status );
				}
				else if ( from.EntityTypeId == 3 )
				{
					eam.Add( entity.RowId, from.EntityBaseId, BaseFactory.RELATIONSHIP_TYPE_IS_PART_OF, false, ref status );
				}
				else if ( from.EntityTypeId == 7 )
				{
					elom.Add( entity.RowId, from.EntityBaseId, BaseFactory.RELATIONSHIP_TYPE_IS_PART_OF, false, ref status );
				}
			}

			foreach ( var item in entity.TransferValueForImport )
			{
				int newId = 0;
				var from = EntityManager.GetEntity( item, false );
				if ( from == null || from.Id == 0 )
				{
					//??
					continue;
				}
				if ( from.EntityTypeId == 1 )
				{
					ecm.Add( entity.RowId, from.EntityBaseId, BaseFactory.RELATIONSHIP_TYPE_HAS_PART, ref newId, ref status );
				}
				else if ( from.EntityTypeId == 3 )
				{
					eam.Add( entity.RowId, from.EntityBaseId, BaseFactory.RELATIONSHIP_TYPE_HAS_PART, false, ref status );
				}
				else if ( from.EntityTypeId == 7 )
				{
					elom.Add( entity.RowId, from.EntityBaseId, BaseFactory.RELATIONSHIP_TYPE_HAS_PART, false, ref status );
				}
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
					efEntity.CTID = ctid;
					efEntity.SubjectWebpage = registryAtId;

					efEntity.Created = System.DateTime.Now;
					efEntity.LastUpdated = System.DateTime.Now;

					context.TransferValueProfile.Add( efEntity );
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
		public bool Delete( string credentialRegistryId, string ctid, ref string statusMessage )
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
						}
						if ( orgUid != null )
						{
							List<String> messages = new List<string>();
							//mark owning org for updates 
							//	- nothing yet from frameworks
							var org = OrganizationManager.GetBasics( (Guid)orgUid );
							if ( org != null && org.Id > 0 )
							{
								new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_ORGANIZATION, org.Id, 1, ref messages );
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

			//if we don't require url, we can't resolve potentially duplicate framework names
			

			return status.WasSectionValid;
		}

		#endregion
		#region  retrieval ==================

		/// <summary>
		/// Get a competency record
		/// </summary>
		/// <param name="profileId"></param>
		/// <returns></returns>
		public static ThisEntity Get( int profileId )
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

		public static ThisEntity GetByCtid( string ctid )
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
						MapFromDB( item, entity );
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



			output.LifecycleStatusType = string.IsNullOrWhiteSpace( input.LifecycleStatusType) ? "lifecycle:Active" : input.LifecycleStatusType;
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

		public static void MapFromDB( DBEntity input, ThisEntity output )
		{
			output.Id = input.Id;
			output.RowId = input.RowId;
			output.EntityStateId = input.EntityStateId;
			output.Name = input.Name;
			output.Description = input.Description;
			output.CTID = input.CTID;
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

			output.LifecycleStatusType = string.IsNullOrWhiteSpace( input.LifecycleStatusType ) ? "lifecycle:Active" : input.LifecycleStatusType;
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

			//the top level object may not be enough. First need to confirm if reference lopps and asmts can have detail pages.
			if ( !string.IsNullOrWhiteSpace( output.TransferValueFromJson ) )
			{
				output.TransferValueFrom = JsonConvert.DeserializeObject<List<TopLevelObject>>( output.TransferValueFromJson );
				var lopps = output.TransferValueFrom.Where( s => s.EntityTypeId == 7 ).ToList();
				foreach( var item in lopps)
				{
					output.TransferValueFromLopp.Add( LearningOpportunityManager.GetForDetail( item.Id ) );
				}
				var assmts = output.TransferValueFrom.Where( s => s.EntityTypeId == 3 ).ToList();
				foreach ( var item in assmts )
				{
					output.TransferValueFromAsmt.Add( AssessmentManager.GetForDetail( item.Id ) );
				}

				var creds = output.TransferValueFrom.Where( s => s.EntityTypeId == 1 ).ToList();
				foreach ( var item in creds )
				{
					output.TransferValueFromCredential.Add( CredentialManager.GetForDetail( item.Id ) );
				}
			}
			//
			if ( !string.IsNullOrWhiteSpace( output.TransferValueForJson ) )
			{
				output.TransferValueFor = JsonConvert.DeserializeObject<List<TopLevelObject>>( output.TransferValueForJson );
				var lopps = output.TransferValueFor.Where( s => s.EntityTypeId == 7 ).ToList();
				foreach ( var item in lopps )
				{
					output.TransferValueForLopp.Add( LearningOpportunityManager.GetForDetail( item.Id ) );
				}
				var assmts = output.TransferValueFor.Where( s => s.EntityTypeId == 3 ).ToList();
				foreach ( var item in assmts )
				{
					output.TransferValueForAsmt.Add( AssessmentManager.GetForDetail( item.Id ) );
				}

				var creds = output.TransferValueFor.Where( s => s.EntityTypeId == 1 ).ToList();
				foreach ( var item in creds )
				{
					output.TransferValueForCredential.Add( CredentialManager.GetForDetail( item.Id ) );
				}

			}

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

			if ( input.Created != null )
				output.Created = ( DateTime )input.Created;
			if ( input.LastUpdated != null )
				output.LastUpdated = ( DateTime )input.LastUpdated;

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
		#endregion
	}


}

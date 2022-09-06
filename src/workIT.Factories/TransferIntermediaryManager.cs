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
using DBEntity = workIT.Data.Tables.TransferIntermediary;
using ThisEntity = workIT.Models.Common.TransferIntermediary;

using Views = workIT.Data.Views;

using EM = workIT.Data.Tables;
using Newtonsoft.Json;

namespace workIT.Factories
{
	public class TransferIntermediaryManager : BaseFactory
	{
		static string thisClassName = "TransferIntermediaryManager";
		static string EntityType = "TransferIntermediary";
		static int EntitTypeId = CodesManager.ENTITY_TYPE_TRANSFER_INTERMEDIARY;
		#region Persistance ===================


		/// <summary>
		/// Add/Update a TransferIntermediary
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

						context.TransferIntermediary.Add( efEntity );

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
								ActivityType = "TransferIntermediary",
								Activity = "Import",
								Event = "Add",
								Comment = string.Format( "New Transfer Intermediary was found by the import. Name: {0}, URI: {1}", entity.Name, entity.SubjectWebpage ),
								ActivityObjectId = entity.Id
							};
							new ActivityManager().SiteActivityAdd( sa );

							if ( !UpdateParts( entity, ref status ) )
								isValid = false;
						}
					}
					else
					{

						efEntity = context.TransferIntermediary.FirstOrDefault( s => s.Id == entity.Id );
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
									ActivityType = "TransferIntermediary",
									Activity = "Import",
									Event = "Update",
									Comment = string.Format( "Updated Transfer Intermediary found by the import. Name: {0}, URI: {1}", entity.Name, entity.SubjectWebpage ),
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
				LoggingHelper.LogError( ex, "TransferIntermediaryManager.Save()" );
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

			//
			foreach ( var item in entity.IntermediaryFor )
			{
				//save in TransferIntermediary.TransferValue


			}
			//
			var ecpmanager = new Entity_ConditionProfileManager();
			//ecpmanager.DeleteAll( relatedEntity, ref status );
			ecpmanager.SaveList( entity.Requires, Entity_ConditionProfileManager.ConnectionProfileType_Requirement, entity.RowId, ref status );
			//
			Entity_ReferenceManager erm = new Entity_ReferenceManager();
			erm.DeleteAll( relatedEntity, ref status );
			if ( erm.Add( entity.Subject, entity.RowId, EntitTypeId, ref status, CodesManager.PROPERTY_CATEGORY_SUBJECT, false ) == false )
				isAllValid = false;
			return isAllValid;
		}


		public void UpdateEntityCache( ThisEntity document, ref SaveStatus status )
		{
			EntityCache ec = new EntityCache()
			{
				EntityTypeId = EntitTypeId,
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
			var statusMessage = "";
			if ( new EntityManager().EntityCacheSave( ec, ref statusMessage ) == 0 )
			{
				status.AddError( thisClassName + string.Format( ".UpdateEntityCache for '{0}' ({1}) failed: {2}", document.Name, document.Id, statusMessage ) );
			}
		}
		/// <summary>
		/// Delete 
		/// DOES NOT MEAN TO DELETE ALL TVPs though
		/// </summary>
		/// <param name="recordId"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool Delete( int recordId, ref string statusMessage )
		{
			bool isOK = true;
			using ( var context = new EntityContext() )
			{
				DBEntity p = context.TransferIntermediary.FirstOrDefault( s => s.Id == recordId );
				if ( p != null && p.Id > 0 )
				{
					context.TransferIntermediary.Remove( p );
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
					var efEntity = context.TransferIntermediary
								.FirstOrDefault( s => s.CTID == ctid );

					if ( efEntity != null && efEntity.Id > 0 )
					{
						//TODO - may need a check for existing alignments
						Guid rowId = efEntity.RowId;
						var orgUid = efEntity.OwningAgentUid;
						//need to remove from Entity.
						//-using before delete trigger - verify won't have RI issues
						string msg = string.Format( " TransferIntermediary. Id: {0}, Name: {1}, Ctid: {2}", efEntity.Id, efEntity.Name, efEntity.CTID );
						//leaving as virtual?
						//need to check for in use.
						//context.TransferIntermediary.Remove( efEntity );
						efEntity.EntityStateId = 0;
						efEntity.LastUpdated = System.DateTime.Now;

						int count = context.SaveChanges();
						if ( count >= 0 )
						{
							new ActivityManager().SiteActivityAdd( new SiteActivity()
							{
								ActivityType = "TransferIntermediary",
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
							var org = OrganizationManager.GetBasics( ( Guid ) orgUid );
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
				status.AddWarning( "An Transfer Intermediary name must be entered" );
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
		public static ThisEntity Get( int profileId, bool gettingAll = true )
		{
			ThisEntity entity = new ThisEntity();
			if ( profileId == 0 )
				return entity;
			try
			{
				using ( var context = new EntityContext() )
				{
					DBEntity item = context.TransferIntermediary
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
					DBEntity item = context.TransferIntermediary
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
					DBEntity item = context.TransferIntermediary
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
				var results = context.TransferIntermediary
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
				list = Entity_LearningOpportunityManager.LearningOpps_GetAll( parent.EntityUid, true, false, 0 );
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
			//
			output.CodedNotation = input.CodedNotation;
			output.CreditValueJson = input.CreditValueJson;
			output.IntermediaryForJson = input.IntermediaryForJson;
			//


		} //
		  //we don't have to store the complete object, such as assessment, lopp, etc.
		public static string TransferIntermediaryActionToJson( List<TopLevelObject> input )
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
				output.Created = ( DateTime ) input.Created;
			if ( input.LastUpdated != null )
				output.LastUpdated = ( DateTime ) input.LastUpdated;
			if ( IsGuidValid( input.OwningAgentUid ) )
			{
				output.OwningAgentUid = ( Guid ) input.OwningAgentUid;
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
			output.CodedNotation = input.CodedNotation;
			output.SubjectWebpage = input.SubjectWebpage;
			output.CredentialRegistryId = input.CredentialRegistryId ?? "";

			//get json and expand
			output.CreditValueJson = input.CreditValueJson;
			if ( !string.IsNullOrWhiteSpace( output.CreditValueJson ) )
				output.CreditValue = JsonConvert.DeserializeObject<List<ValueProfile>>( output.CreditValueJson );
			//get json and expand
			output.IntermediaryForJson = input.IntermediaryForJson;

			//
			output.Subject = Entity_ReferenceManager.GetAll( output.RowId, CodesManager.PROPERTY_CATEGORY_SUBJECT );		



			if ( !gettingAll )
				return;

			//get condition profiles
			List<ConditionProfile> list = new List<ConditionProfile>();
			list = Entity_ConditionProfileManager.GetAll( output.RowId, false );
			if ( list != null && list.Count > 0 )
			{
				foreach ( ConditionProfile item in list )
				{
					if ( item.ConnectionProfileTypeId == Entity_ConditionProfileManager.ConnectionProfileType_Requirement )
						output.Requires.Add( item );
					else
					{
						//NOT possible - could log just in case.
						//add output required, for dev only?
						//if ( IsDevEnv() )
						//{
						//	item.ProfileName = ( item.ProfileName ?? "" ) + " unexpected condition type of " + item.ConnectionProfileTypeId.ToString();
						//	output.Requires.Add( item );
						//}
					}
				}
			}
			//


		}

		#endregion

		//public static int Count_ForOwningOrg( string orgCtid )
		//{
		//	int totalRecords = 0;
		//	if ( string.IsNullOrWhiteSpace( orgCtid ) || orgCtid.Trim().Length != 39 )
		//		return totalRecords;

		//	using ( var context = new EntityContext() )
		//	{
		//		var query = ( from entity in context.TransferIntermediary
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

				using ( SqlCommand command = new SqlCommand( "[TransferIntermediary.ElasticSearch]", c ) )
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
					item.CodedNotation = GetRowColumn( dr, "CodedNotation", "" );
					//Subject

					item.CreditValueJson = GetRowColumn( dr, "CreditValueJson", "" );
					item.IntermediaryForJson = GetRowColumn( dr, "IntermediaryForJson", "" );
					
					//
					item.EntityLastUpdated = item.LastUpdated = GetRowColumn( dr, "LastUpdated", System.DateTime.MinValue );
					item.Created = GetRowColumn( dr, "Created", System.DateTime.MinValue );
					//
					item.HasTransferValueProfiles = GetRowColumn( dr, "HasTransferValueProfiles", 0 );
					list.Add( item );
				}

				return list;

			}
		} //


	}


}

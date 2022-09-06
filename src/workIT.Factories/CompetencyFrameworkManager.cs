using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

using Newtonsoft.Json;

using workIT.Models;
using workIT.Models.Common;
using workIT.Utilities;
//using workIT.Models.Helpers.Cass;
using ApiFramework = workIT.Models.API.CompetencyFramework;
using DBEntity = workIT.Data.Tables.CompetencyFramework;
using EntityContext = workIT.Data.Tables.workITEntities;
using ThisEntity = workIT.Models.ProfileModels.CompetencyFramework;
using ThisEntityItem = workIT.Models.Common.CredentialAlignmentObjectItem;

namespace workIT.Factories
{
	public class CompetencyFrameworkManager : BaseFactory
	{
		static string thisClassName = "CompetencyFrameworkManager";
		#region --- CompetencyFrameworkManager ---
		#region Persistance ===================


		/// <summary>
		/// Add/Update a CompetencyFramework
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public bool Save(ThisEntity entity,
				ref SaveStatus status, bool addingActivity = false)
		{
			bool isValid = true;
			int count = 0;
			DateTime lastUpdated = System.DateTime.Now;
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
							efEntity.Created = status.LocalCreatedDate;
						else
							efEntity.Created = DateTime.Now;
						//
						if ( IsValidDate( status.EnvelopeUpdatedDate ) )
							efEntity.LastUpdated = status.LocalUpdatedDate;
						else
							efEntity.LastUpdated = DateTime.Now;

						if ( IsValidGuid( entity.RowId ) )
							efEntity.RowId = entity.RowId;
						else
							efEntity.RowId = Guid.NewGuid();

						context.CompetencyFramework.Add( efEntity );

						count = context.SaveChanges();

						entity.Id = efEntity.Id;
						entity.RowId = efEntity.RowId;
						if ( count == 0 )
						{
							status.AddWarning( string.Format( " Unable to add Profile: {0} <br\\> ", string.IsNullOrWhiteSpace( entity.Name ) ? "no description" : entity.Name ) );

							entity.LastUpdated = ( DateTime ) efEntity.LastUpdated;
							
						}
						else
						{
							if ( addingActivity )
							{
								//add log entry
								SiteActivity sa = new SiteActivity()
								{
									ActivityType = "CompetencyFramework",
									Activity = "Import",
									Event = "Add",
									Comment = string.Format( "New Competency Framework was found by the import. Name: {0}, URI: {1}", entity.Name, entity.FrameworkUri ),
									ActivityObjectId = entity.Id
								};
								new ActivityManager().SiteActivityAdd( sa );
							}
							UpdateEntityCache( entity, ref status );
							//update competencies
							new CompetencyFrameworkCompetencyManager().SaveList( entity.Id, entity.ImportCompetencies, ref status );

							if ( !UpdateParts( entity, ref status ) )
								isValid = false;
						}
					}
					else
					{

						efEntity = context.CompetencyFramework.FirstOrDefault( s => s.Id == entity.Id );
						if ( efEntity != null && efEntity.Id > 0 )
						{
							entity.RowId = efEntity.RowId;
							//update
							MapToDB( entity, efEntity );

							//need to do the date check here, or may not be updated
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
								if ( addingActivity )
								{
									//add log entry
									SiteActivity sa = new SiteActivity()
									{
										ActivityType = "CompetencyFramework",
										Activity = "Import",
										Event = "Update",
										Comment = string.Format( "Updated Competency Framework found by the import. Name: {0}, URI: {1}", entity.Name, entity.FrameworkUri ),
										ActivityObjectId = entity.Id
									};
									new ActivityManager().SiteActivityAdd( sa );
								}
							}

							entity.LastUpdated = lastUpdated;
							UpdateEntityCache( entity, ref status );
							//update competencies regardless
							new CompetencyFrameworkCompetencyManager().SaveList( entity.Id, entity.ImportCompetencies, ref status );
							if ( !UpdateParts( entity, ref status ) )
								isValid = false;
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "CompetencyFrameworkManager.Save()" );
			}

			return isValid;
		}
		public int AddPendingRecord( Guid entityUid, string ctid, string registryAtId, ref string status )
		{
			DBEntity efEntity = new DBEntity();
			try
			{
				//var pathwayCTIDTemp = "ce-abcb5fe0-8fde-4f06-9d70-860cd5bdc763";
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
					
					//realitically the component should be added in the same workflow
					efEntity.EntityStateId = 1;
					efEntity.RowId = entityUid;
					//watch that Ctid can be  updated if not provided now!!
					efEntity.CTID = ctid;
					efEntity.FrameworkUri = registryAtId;

					efEntity.Created = System.DateTime.Now;
					efEntity.LastUpdated = System.DateTime.Now;

					context.CompetencyFramework.Add( efEntity );
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
		public void UpdateEntityCache( ThisEntity document, ref SaveStatus status )
		{
			var ec = new EntityCache()
			{
				EntityTypeId = 10,
				EntityType = "CompetencyFramework",
				EntityStateId = document.EntityStateId,
				EntityUid = document.RowId,
				BaseId = document.Id,
				Description = document.Description,
				SubjectWebpage = document.SubjectWebpage,
				CTID = document.CTID,
				Created = document.Created,
				LastUpdated = document.LastUpdated,
				ImageUrl = document.Image,
				Name = document.Name,
				OwningAgentUID = document.OwningAgentUid,
				OwningOrgId = document.OrganizationId,
				PublishedByOrganizationId = document.PublishedByThirdPartyOrganizationId
			};
			var statusMessage = "";
			if ( new EntityManager().EntityCacheSave( ec, ref statusMessage ) == 0 )
			{
				status.AddError( thisClassName + string.Format( ".UpdateEntityCache for '{0}' ({1}) failed: {2}", document.Name, document.Id, statusMessage ) );
			}
		}

		public bool UpdateParts( ThisEntity entity, ref SaveStatus status )
		{
			bool isAllValid = true;
			Entity relatedEntity = EntityManager.GetEntity( entity.RowId );
			if ( relatedEntity == null || relatedEntity.Id == 0 )
			{
				status.AddError( "Error - the related Entity was not found." );
				return false;
			}
			Entity_AgentRelationshipManager mgr = new Entity_AgentRelationshipManager();

			mgr.SaveList( relatedEntity.Id, Entity_AgentRelationshipManager.ROLE_TYPE_PUBLISHEDBY, entity.PublishedBy, ref status );


			return isAllValid;
		}

		/// <summary>
		/// Delete a framework - only if no remaining references!!
		/// MAY NOT expose initially
		/// </summary>
		/// <param name="recordId"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool Delete(int recordId, ref string statusMessage)
		{
			bool isOK = true;
			using ( var context = new EntityContext() )
			{
				DBEntity p = context.CompetencyFramework.FirstOrDefault( s => s.Id == recordId );
				if ( p != null && p.Id > 0 )
				{
					context.CompetencyFramework.Remove( p );
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
		public bool Delete( string ctid, ref string statusMessage)
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
					var efEntity = context.CompetencyFramework
								.FirstOrDefault( s => s.CTID == ctid );

					if ( efEntity != null && efEntity.Id > 0 )
					{
						//TODO - may need a check for existing alignments
						Guid rowId = efEntity.RowId;
						var orgCtid = efEntity.OrganizationCTID ?? "";
						//need to remove from Entity.
						//-using before delete trigger - verify won't have RI issues
						string msg = string.Format( " CompetencyFramework Id: {0}, Name: {1}, Ctid: {2}", efEntity.Id, efEntity.Name, efEntity.CTID );
						//leaving as virtual?
						//need to check for in use.
						//context.CompetencyFramework.Remove( efEntity );
						efEntity.EntityStateId = 0;
						efEntity.LastUpdated = System.DateTime.Now;

						int count = context.SaveChanges();
						if ( count >= 0 )
						{
							new ActivityManager().SiteActivityAdd( new SiteActivity()
							{
								ActivityType = "CompetencyFramework",
								Activity = "Import",
								Event = "Delete",
								Comment = msg
							} );
							isValid = true;
							//delete cache
							new EntityManager().EntityCacheDelete( CodesManager.ENTITY_TYPE_COMPETENCY_FRAMEWORK, efEntity.Id, ref statusMessage );
							//add pending request 
							List<String> messages = new List<string>();
							new SearchPendingReindexManager().AddDeleteRequest( CodesManager.ENTITY_TYPE_COMPETENCY_FRAMEWORK, efEntity.Id, ref messages );

							//delete all relationships
							workIT.Models.SaveStatus status = new SaveStatus();
							Entity_AgentRelationshipManager earmgr = new Entity_AgentRelationshipManager();
							earmgr.DeleteAll( rowId, ref status );
							//also check for any relationships
							//There could be other orgs from relationships to be reindexed as well!

							
						}
						if ( !string.IsNullOrWhiteSpace( orgCtid ) )
						{
							List<String> messages = new List<string>();
							//mark owning org for updates 
							//	- nothing yet from frameworks
							var org = OrganizationManager.GetSummaryByCtid( orgCtid );
							if ( org != null && org.Id > 0 )
							{
								new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, org.Id, 1, ref messages );

								//also check for any relationships
								new Entity_AgentRelationshipManager().ReindexAgentForDeletedArtifact( org.RowId );
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
		public bool ValidateProfile(ThisEntity profile, ref SaveStatus status)
		{
			status.HasSectionErrors = false;

			if ( string.IsNullOrWhiteSpace( profile.Name ) )
			{
				status.AddWarning( "An competency framework name must be entered" );
			}

			//if we don't require url, we can't resolve potentially duplicate framework names


			return status.WasSectionValid;
		}

		#endregion
		#region  retrieval ==================

		/// <summary>
		/// Get a competency framework record
		/// </summary>
		/// <param name="profileId"></param>
		/// <returns></returns>
		public static ThisEntity Get(int profileId)
		{
			ThisEntity entity = new ThisEntity();
			if ( profileId == 0 )
				return entity;
			try
			{
				using ( var context = new EntityContext() )
				{
					DBEntity item = context.CompetencyFramework
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

		public int Lookup_OR_Add(string frameworkUri, string frameworkName)
		{
			int frameworkId = 0;
			if ( string.IsNullOrWhiteSpace( frameworkUri ) )
				return 0;

			//*** no data for frameworkURL, just frameworkUri or sourceUrl
			ThisEntity entity = GetByUrl( frameworkUri );
			if ( entity != null && entity.Id > 0 )
				return entity.Id;
			//skip if no name
			if ( string.IsNullOrWhiteSpace( frameworkName ) )
				return 0;
			SaveStatus status = new SaveStatus();
			entity.Name = frameworkName;
			//this could an external Url, or a registry Uri
			if ( frameworkUri.ToLower().IndexOf( "credentialengineregistry.org/resources/" ) > -1
					|| frameworkUri.ToLower().IndexOf( "credentialengineregistry.org/graph/" ) > -1 )
				entity.FrameworkUri = frameworkUri;
			else
				entity.Source = frameworkUri;
			Save( entity, ref status, true );
			if ( entity.Id > 0 )
				return entity.Id;

			return frameworkId;
		}//

		public static ThisEntity GetByUrl(string frameworkUri)
		{
			ThisEntity entity = new ThisEntity();
			if ( string.IsNullOrWhiteSpace( frameworkUri ) )
				return entity;
			try
			{
				using ( var context = new EntityContext() )
				{
					//lookup by frameworkUri, or SourceUrl
					DBEntity item = context.CompetencyFramework
							.FirstOrDefault( s => 
								( s.FrameworkUri != null && s.FrameworkUri.ToLower() == frameworkUri.ToLower())
							||	(s.SourceUrl != null && s.SourceUrl.ToLower() == frameworkUri.ToLower())
							);

					if ( item != null && item.Id > 0 )
					{
						MapFromDB( item, entity );
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetByUrl: " + frameworkUri );
			}
			return entity;
		}//

		public static ThisEntity GetByCtid(string ctid)
		{
			ThisEntity entity = new ThisEntity();
			if ( string.IsNullOrWhiteSpace( ctid ) )
				return entity;
			try
			{
				using ( var context = new EntityContext() )
				{
					//lookup by frameworkUri, or SourceUrl
					DBEntity item = context.CompetencyFramework
							.FirstOrDefault( s => s.CTID.ToLower() == ctid.ToLower() );

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
		public static void MapToDB(ThisEntity from, DBEntity to)
		{
			//want to ensure fields from create are not wiped
			//to.Id = from.Id;

			to.Name = from.Name;
			to.Description = from.Description;
			to.SourceUrl = from.Source ?? "";
			to.FrameworkUri = from.FrameworkUri ?? "";
			to.CredentialRegistryId = from.CredentialRegistryId ?? "";
			//will want to extract from FrameworkUri (for now)
			if ( !string.IsNullOrWhiteSpace( from.CTID ) && from.CTID.Length == 39 )
				to.CTID = from.CTID;
			else
			{
				if ( to.FrameworkUri.ToLower().IndexOf( "credentialengineregistry.org/resources/ce-" ) > -1
					|| to.FrameworkUri.ToLower().IndexOf( "credentialengineregistry.org/graph/ce-" ) > -1 )
				{
					to.CTID = from.FrameworkUri.Substring( from.FrameworkUri.IndexOf( "/ce-" ) + 1 );

				}
				//else if ( from.FrameworkUri.ToLower().IndexOf("credentialengineregistry.org/resources/ce-") > -1 )
				//{
				//    to.CTID = from.FrameworkUri.Substring(from.FrameworkUri.IndexOf("/ce-") + 1);
				//}
			}
			if ( !string.IsNullOrWhiteSpace( from.OrganizationCTID ) && from.OrganizationCTID.Length == 39 )
				to.OrganizationCTID = from.OrganizationCTID;


			//TODO - have to be consistent in having this data
			//this may done separately. At very least setting false will be done separately
			//actually the presence of a ctid should only be for registry denizen
			if ( from.ExistsInRegistry 
				|| (!string.IsNullOrWhiteSpace(from.CTID) && from.CTID.Length == 39)
				|| ( !string.IsNullOrWhiteSpace( to.CredentialRegistryId ) && to.CredentialRegistryId.Length == 36 ) 
				)
			{
				to.EntityStateId = 3;
				to.ExistsInRegistry = from.ExistsInRegistry;
			}
			else
			{
				//dont think there is a case to set to 1
				to.EntityStateId = 2;
				to.ExistsInRegistry = false;
			}
			to.TotalCompetencies = from.TotalCompetencies;
			if ( !string.IsNullOrWhiteSpace( from.ElasticCompentenciesStore ) )
				to.CompetenciesStore = from.ElasticCompentenciesStore;
			else
			{
				//ensure we don't reset the store
				to.CompetenciesStore = null;
			}
			if ( !string.IsNullOrWhiteSpace( from.APIFramework ) )
				to.CompetencyFrameworkHierarchy = from.APIFramework;
			else
			{
				//ensure we don't reset the property
				to.CompetencyFrameworkHierarchy = null;
			}
			//20-07-02 mp - just store the (index ready) competencies json, not the whole graph
			//				- may stop saving this for now?
			if ( !string.IsNullOrWhiteSpace( from.CompetencyFrameworkGraph ) )
				to.CompetencyFrameworkGraph = from.CompetencyFrameworkGraph;
			else
			{
				//ensure we don't reset the graph
			}

		} //

		public static void MapFromDB(DBEntity from, ThisEntity to)
		{
			to.Id = from.Id;
			to.RowId = from.RowId;
			to.EntityStateId = from.EntityStateId;
			to.Name = from.Name;
			to.FriendlyName = FormatFriendlyTitle( from.Name );

			to.Description = from.Description;
			to.CTID = from.CTID;
			to.OrganizationCTID = from.OrganizationCTID ?? "";
			to.Source = from.SourceUrl;
			to.FrameworkUri = from.FrameworkUri;
			to.CredentialRegistryId = from.CredentialRegistryId ?? "";

			to.TotalCompetencies = from.TotalCompetencies;
			to.ElasticCompentenciesStore = from.CompetenciesStore;
			to.CompetencyFrameworkGraph = from.CompetencyFrameworkGraph;


			//this should be replace by presence of CredentialRegistryId
			if ( from.ExistsInRegistry != null )
				to.ExistsInRegistry = ( bool )from.ExistsInRegistry;
			if ( from.Created != null )
				to.Created = ( DateTime )from.Created;
			if ( from.LastUpdated != null )
				to.LastUpdated = ( DateTime )from.LastUpdated;

			to.APIFramework = from.CompetencyFrameworkHierarchy;
			try
			{
				if ( !string.IsNullOrWhiteSpace( to.APIFramework ) )
				{
					//ApiFramework
					to.ApiFramework = JsonConvert.DeserializeObject<ApiFramework>( to.APIFramework );
				}
			} catch (Exception ex)
            {
				LoggingHelper.LogError( ex, thisClassName + string.Format( ".MapFromDB. Name: '{0}', CTID: {1} ", to.Name, to.CTID ));

			}
			//soon to be obsolete
			//to.FrameworkUrl = from.FrameworkUrl;
		}

		#endregion
		public static int FrameworkCount_InRegistry()
		{
			int totalRecords = 0;

			using ( var context = new EntityContext() )
			{
				var results = context.CompetencyFramework.Where( s => s.CredentialRegistryId != null && s.CredentialRegistryId.Length == 36).ToList();

				if ( results != null && results.Count > 0 )
				{
					totalRecords = results.Count();

				}
			}

			return totalRecords;
		}
		public static int FrameworkCount_ForOwningOrg(string orgCtid)
		{
			int totalRecords = 0;
			if ( string.IsNullOrWhiteSpace( orgCtid ) || orgCtid.Trim().Length != 39 )
				return totalRecords;

			using ( var context = new EntityContext() )
			{
				var query = ( from entity in context.CompetencyFramework
							  join org in context.Organization on entity.OrganizationCTID equals org.CTID
							  where entity.OrganizationCTID.ToLower() == orgCtid.ToLower()
								   && org.EntityStateId > 1 && entity.EntityStateId == 3
							  select new
							  {
								  entity.CTID
							  } );
				//until ed frameworks is cleaned up, need to prevent dups != 39
				var results = query.Select( s => s.CTID ).Distinct()
					.ToList();

				if ( results != null && results.Count > 0 )
				{
					totalRecords = results.Count();

				}
			}

			return totalRecords;
		}

		/// <summary>
		/// Search for competencies (not CompetencyFrameworks!)
		/// Currently uses: ConditionProfile_Competencies_cache
		/// 21-07-23 mparsons - need to review this process!
		/// </summary>
		/// <param name="pFilter"></param>
		/// <param name="pOrderBy"></param>
		/// <param name="pageNumber"></param>
		/// <param name="pageSize"></param>
		/// <param name="pTotalRows"></param>
		/// <returns></returns>
		public static List<ThisEntityItem> Search(string pFilter, string pOrderBy, int pageNumber, int pageSize, ref int pTotalRows)
		{
			string connectionString = DBConnectionRO();
			ThisEntityItem item = new ThisEntityItem();
			List<ThisEntityItem> list = new List<ThisEntityItem>();
			var result = new DataTable();

			using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				c.Open();

				if ( string.IsNullOrEmpty( pFilter ) )
				{
					pFilter = "";
				}

				using ( SqlCommand command = new SqlCommand( "[Competencies_search]", c ) )
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
					item = new ThisEntityItem();
					item.Id = GetRowColumn( dr, "CompetencyFrameworkItemId", 0 );
					item.TargetNodeName = GetRowColumn( dr, "Competency", "???" );
					//item.ProfileName = GetRowPossibleColumn( dr, "Competency2", "???" );
					item.Description = GetRowColumn( dr, "Description", "" );

					//don't include credentialId, as will work with source of the search will often be for a credential./ Same for condition profiles for now. 
					item.SourceParentId = GetRowColumn( dr, "SourceId", 0 );
					item.SourceEntityTypeId = GetRowColumn( dr, "SourceEntityTypeId", 0 );
					//item.AlignmentTypeId = GetRowColumn( dr, "AlignmentTypeId", 0 );
					//item.AlignmentType = GetRowColumn( dr, "AlignmentType", "" );
					//Although the condition profile type may be significant?
					item.ConnectionTypeId = GetRowColumn( dr, "ConnectionTypeId", 0 );
					//=== NOTE created and lastUpdated are not relevent here
					//string date = GetRowPossibleColumn( dr, "Created", "" );
					//if ( DateTime.TryParse( date, out DateTime testdate ) )
					//	item.Created = testdate;

					//date = GetRowPossibleColumn( dr, "LastUpdated", "" );
					//if ( DateTime.TryParse( date, out testdate ) )
					//	item.LastUpdated = item.EntityLastUpdated = testdate;

					list.Add( item );
				}

				return list;

			}
		} //
		#endregion
	}
}

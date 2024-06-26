﻿using System;
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
using DBResource = workIT.Data.Tables.CompetencyFramework;
using EntityContext = workIT.Data.Tables.workITEntities;
using ThisResource = workIT.Models.ProfileModels.CompetencyFramework;
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
		public bool Save(ThisResource entity,
				ref SaveStatus status, bool addingActivity = false)
		{
			bool isValid = true;
			int count = 0;
			DateTime lastUpdated = System.DateTime.Now;
			DBResource efEntity = new DBResource();
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
						efEntity = new DBResource();
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
							entity.Created = ( DateTime ) efEntity.Created;
							entity.LastUpdated = ( DateTime ) efEntity.LastUpdated;
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
							//24-03-14 mp - why is this being set to something other than that done in the MapToDB?
							entity.EntityStateId = 3;
							UpdateEntityCache( entity, ref status );

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
							//24-03-14 mp - chg to check for a ctid? It assumes that the value 2 was set correctly?
                            if ( efEntity.EntityStateId != 2 || IsValidCtid( entity.CTID) )
                                efEntity.EntityStateId = 3;
                            entity.EntityStateId = efEntity.EntityStateId;
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
							entity.Created = ( DateTime ) efEntity.Created;

							UpdateEntityCache( entity, ref status );

							if ( !UpdateParts( entity, ref status ) )
								isValid = false;
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, $"{thisClassName}.Save(ctid: {entity.CTID})" );
			}

			return isValid;
		}
		public int AddPendingRecord( Guid entityUid, string ctid, string registryAtId, ref string status )
		{
			DBResource efEntity = new DBResource();
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
					ThisResource entity = GetByCtid( ctid );
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
		public void UpdateEntityCache( ThisResource document, ref SaveStatus status )
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
				ImageUrl = "",
				Name = document.Name,
				OwningAgentUID = document.PrimaryAgentUID,
				OwningOrgId = document.OrganizationId,
				PublishedByOrganizationId = document.PublishedByThirdPartyOrganizationId
			};
			var statusMessage = string.Empty;
			if ( new EntityManager().EntityCacheSave( ec, ref statusMessage ) == 0 )
			{
				status.AddError( thisClassName + string.Format( ".UpdateEntityCache for '{0}' ({1}) failed: {2}", document.Name, document.Id, statusMessage ) );
			}
		}

		public bool UpdateParts( ThisResource resource, ref SaveStatus status )
		{
			bool isAllValid = true;
			Entity relatedEntity = EntityManager.GetEntity( resource.RowId );
			if ( relatedEntity == null || relatedEntity.Id == 0 )
			{
				status.AddError( "Error - the related Entity was not found." );
				return false;
			}
			resource.RelatedEntityId= relatedEntity.Id;
			Entity_AgentRelationshipManager mgr = new Entity_AgentRelationshipManager();
			//do deletes
			mgr.DeleteAll( relatedEntity, ref status );
			mgr.SaveList( relatedEntity.Id, Entity_AgentRelationshipManager.ROLE_TYPE_OWNER, resource.OwnedByIds, ref status );

			mgr.SaveList( relatedEntity.Id, Entity_AgentRelationshipManager.ROLE_TYPE_PUBLISHEDBY, resource.PublishedBy, ref status );
			//
			new Entity_IdentifierValueManager().SaveList( resource.VersionIdentifier, resource.RowId, Entity_IdentifierValueManager.IdentifierValue_VersionIdentifier, ref status, true );

			//update competencies regardless
			new CompetencyFrameworkCompetencyManager().SaveList( resource, resource.ImportCompetencies, ref status );

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
				DBResource p = context.CompetencyFramework.FirstOrDefault( s => s.Id == recordId );
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
						var orgCtid = efEntity.OrganizationCTID ?? string.Empty;
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
							new EntityManager().EntityCacheDelete( rowId, ref statusMessage );
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
		public bool ValidateProfile(ThisResource profile, ref SaveStatus status)
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
		public static ThisResource Get(int profileId, bool gettingAllData = true )
		{
			ThisResource entity = new ThisResource();
			if ( profileId == 0 )
				return entity;
			try
			{
				using ( var context = new EntityContext() )
				{
					DBResource item = context.CompetencyFramework
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
			ThisResource entity = GetByUrl( frameworkUri );
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

		public static ThisResource GetByUrl(string frameworkUri)
		{
			ThisResource entity = new ThisResource();
			if ( string.IsNullOrWhiteSpace( frameworkUri ) )
				return entity;
			try
			{
				using ( var context = new EntityContext() )
				{
					//lookup by frameworkUri, or SourceUrl
					DBResource item = context.CompetencyFramework
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

		public static ThisResource GetByCtid(string ctid)
		{
			ThisResource entity = new ThisResource();
			if ( string.IsNullOrWhiteSpace( ctid ) )
				return entity;
			try
			{
				using ( var context = new EntityContext() )
				{
					//lookup by frameworkUri, or SourceUrl
					DBResource item = context.CompetencyFramework
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
		}
		//

		public static string GetCTIDFromID( int id )
		{
			using ( var context = new EntityContext() )
			{
				var item = context.CompetencyFramework.FirstOrDefault( s => s.Id == id );
				if ( item != null && item.Id > 0 )
				{
					return item.CTID;
				}
			}

			return null;
		}
		//

		public static string GetCompetencyCTIDFromCompetencyID( int id )
		{
			using(var context = new EntityContext() )
			{
				var item = context.CompetencyFramework_Competency.FirstOrDefault( s => s.Id == id );
				if( item != null && item.Id > 0 )
				{
					return item.CTID;
				}
			}

			return null;
		}
		//

		public static void MapToDB(ThisResource input, DBResource output)
		{
			//want to ensure fields from create are not wiped
			//to.Id = from.Id;

			output.Name = input.Name;
			output.Description = input.Description;
			output.SourceUrl = input.Source ?? string.Empty;
			output.FrameworkUri = input.FrameworkUri ?? string.Empty;
			output.CredentialRegistryId = input.CredentialRegistryId ?? string.Empty;
			//will want to extract from FrameworkUri (for now)
			if ( !string.IsNullOrWhiteSpace( input.CTID ) && input.CTID.Length == 39 )
				output.CTID = input.CTID;
			else
			{
				if ( output.FrameworkUri.ToLower().IndexOf( "credentialengineregistry.org/resources/ce-" ) > -1
					|| output.FrameworkUri.ToLower().IndexOf( "credentialengineregistry.org/graph/ce-" ) > -1 )
				{
					output.CTID = input.FrameworkUri.Substring( input.FrameworkUri.IndexOf( "/ce-" ) + 1 );

				}
				//else if ( from.FrameworkUri.ToLower().IndexOf("credentialengineregistry.org/resources/ce-") > -1 )
				//{
				//    to.CTID = from.FrameworkUri.Substring(from.FrameworkUri.IndexOf("/ce-") + 1);
				//}
			}
			if ( !string.IsNullOrWhiteSpace( input.OrganizationCTID ) && input.OrganizationCTID.Length == 39 )
				output.OrganizationCTID = input.OrganizationCTID;

            output.LatestVersion = GetUrlData( input.LatestVersion, null );
            output.PreviousVersion = GetUrlData( input.PreviousVersion, null );
            output.NextVersion = GetUrlData( input.NextVersion, null );

            //TODO - have to be consistent in having this data
            //this may done separately. At very least setting false will be done separately
            //actually the presence of a ctid should only be for registry denizen
            if ( input.ExistsInRegistry 
				|| (!string.IsNullOrWhiteSpace(input.CTID) && input.CTID.Length == 39)
				|| ( !string.IsNullOrWhiteSpace( output.CredentialRegistryId ) && output.CredentialRegistryId.Length == 36 ) 
				)
			{
				output.EntityStateId = 3;
				output.ExistsInRegistry = input.ExistsInRegistry;
			}
			else
			{
				//dont think there is a case to set to 1
				output.EntityStateId = 2;
				output.ExistsInRegistry = false;
			}
			output.TotalCompetencies = input.TotalCompetencies;
			if ( !string.IsNullOrWhiteSpace( input.ElasticCompentenciesStore ) )
				output.CompetenciesStore = input.ElasticCompentenciesStore;
			else
			{
				//ensure we don't reset the store
				output.CompetenciesStore = null;
			}
			if ( !string.IsNullOrWhiteSpace( input.APIFramework ) )
				output.CompetencyFrameworkHierarchy = input.APIFramework;
			else
			{
				//ensure we don't reset the property
				output.CompetencyFrameworkHierarchy = null;
			}
			//20-07-02 mp - just store the (index ready) competencies json, not the whole graph
			//				- may stop saving this for now?
			if ( !string.IsNullOrWhiteSpace( input.CompetencyFrameworkGraph ) )
				output.CompetencyFrameworkGraph = input.CompetencyFrameworkGraph;
			else
			{
				//ensure we don't reset the graph
			}

		} //

		public static void MapFromDB(DBResource input, ThisResource output, bool gettingAllData = true )
		{
			output.Id = input.Id;
			output.RowId = input.RowId;
			output.EntityStateId = input.EntityStateId;
			output.Name = input.Name;
			output.FriendlyName = FormatFriendlyTitle( input.Name );

			output.Description = input.Description;
			output.CTID = input.CTID;
			output.OrganizationCTID = input.OrganizationCTID ?? string.Empty;
			output.Source = input.SourceUrl;
			output.FrameworkUri = input.FrameworkUri;
			output.CredentialRegistryId = input.CredentialRegistryId ?? string.Empty;

            output.VersionIdentifier = Entity_IdentifierValueManager.GetAll( output.RowId, Entity_IdentifierValueManager.IdentifierValue_VersionIdentifier );
            output.LatestVersion = input.LatestVersion;
            output.PreviousVersion = input.PreviousVersion;
            output.NextVersion = input.NextVersion;

            output.TotalCompetencies = input.TotalCompetencies;
			output.ElasticCompentenciesStore = input.CompetenciesStore;
			output.CompetencyFrameworkGraph = input.CompetencyFrameworkGraph;


			//this should be replace by presence of CredentialRegistryId or realistically a CTID
			if ( input.ExistsInRegistry != null )
				output.ExistsInRegistry = ( bool )input.ExistsInRegistry;
			if ( input.Created != null )
				output.Created = ( DateTime )input.Created;
			if ( input.LastUpdated != null )
				output.LastUpdated = ( DateTime )input.LastUpdated;

			if (!gettingAllData )
				return;

			//
			output.APIFramework = input.CompetencyFrameworkHierarchy;
			try
			{
				if ( !string.IsNullOrWhiteSpace( output.APIFramework ) )
				{
					//Obolete (no longer populated: 22-05-11)
					output.ApiFramework = JsonConvert.DeserializeObject<ApiFramework>( output.APIFramework );
				}
			} catch (Exception ex)
            {
				LoggingHelper.LogError( ex, thisClassName + string.Format( ".MapFromDB. Name: '{0}', CTID: {1} ", output.Name, output.CTID ));

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
				//24-03-14 mp - was going to change to include frameworks with an entityStateId of 2: there is some inconsistency
				//	However these are frameworks not the registry, so the framework search would not work, also typically the org CTID is not present
				var query = ( from entity in context.CompetencyFramework
							  join org in context.Organization on entity.OrganizationCTID equals org.CTID
							  where entity.OrganizationCTID.ToLower() == orgCtid.ToLower()
								   && org.EntityStateId > 1 && entity.EntityStateId > 2
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
					pFilter = string.Empty;
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
					item.Description = GetRowColumn( dr, "Description", string.Empty );

					//don't include credentialId, as will work with source of the search will often be for a credential./ Same for condition profiles for now. 
					item.SourceParentId = GetRowColumn( dr, "SourceId", 0 );
					item.SourceEntityTypeId = GetRowColumn( dr, "SourceEntityTypeId", 0 );
					//item.AlignmentTypeId = GetRowColumn( dr, "AlignmentTypeId", 0 );
					//item.AlignmentType = GetRowColumn( dr, "AlignmentType", string.Empty );
					//Although the condition profile type may be significant?
					item.ConnectionTypeId = GetRowColumn( dr, "ConnectionTypeId", 0 );
					//=== NOTE created and lastUpdated are not relevent here
					//string date = GetRowPossibleColumn( dr, "Created", string.Empty );
					//if ( DateTime.TryParse( date, out DateTime testdate ) )
					//	item.Created = testdate;

					//date = GetRowPossibleColumn( dr, "LastUpdated", string.Empty );
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

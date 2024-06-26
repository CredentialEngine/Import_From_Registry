using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.ProfileModels;
using workIT.Utilities;

using ThisEntity = workIT.Models.Common.PathwaySet;
using DBEntity = workIT.Data.Tables.PathwaySet;
using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;

using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;

namespace workIT.Factories
{
	public class PathwaySetManager : BaseFactory
	{
		static string thisClassName = "PathwaySetManager";
		static string EntityType = "PathwaySet";
		static int EntityTypeId = CodesManager.ENTITY_TYPE_PATHWAY_SET;

		#region persistance ==================

		/// <summary>
		/// add a PathwaySet
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool Save( ThisEntity entity, ref SaveStatus status )
		{
			bool isValid = true;
			var efEntity = new DBEntity();
			using ( var context = new EntityContext() )
			{
				try
				{
					//messages = new List<string>();
					if ( ValidateProfile( entity, ref status ) == false )
					{
						return false;
					}


					if ( entity.Id == 0 )
					{
						//entity.StatusId = 1;
						MapToDB( entity, efEntity );

						if ( entity.RowId == null || entity.RowId == Guid.Empty )
							efEntity.RowId = entity.RowId = Guid.NewGuid();
						else
							efEntity.RowId = entity.RowId;
						efEntity.EntityStateId = entity.EntityStateId = 3;

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

						context.PathwaySet.Add( efEntity );

						// submit the change to database
						int count = context.SaveChanges();
						if ( count > 0 )
						{
							entity.Id = efEntity.Id;
							entity.RowId = efEntity.RowId;
							entity.Created = efEntity.Created.Value;
							entity.LastUpdated = efEntity.LastUpdated.Value;
							UpdateEntityCache( entity, ref status );
							//add log entry
							SiteActivity sa = new SiteActivity()
							{
								ActivityType = "PathwaySet",
								Activity = "Import",
								Event = "Add",
								Comment = string.Format( "Full PathwaySet was added by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage ),
								ActivityObjectId = entity.Id
							};
							new ActivityManager().SiteActivityAdd( sa );
							UpdateParts( entity, ref status );

							return true;
						}
						else
						{
							//?no info on error
							status.AddError( "Error - the profile was not saved. " );
							string message = string.Format( "PathwayManager.Add Failed. Attempted to add a PathwaySet. The process appeared to not work, but was not an exception, so we have no message, or no clue.PathwaySet. PathwaySet: {0}, SubjectWebpage: {1}", entity.Name, entity.SubjectWebpage );
							EmailManager.NotifyAdmin( thisClassName + ".Add Failed", message );
						}
					}
					else
					{
						efEntity = context.PathwaySet
								.SingleOrDefault( s => s.Id == entity.Id );

						if ( efEntity != null && efEntity.Id > 0 )
						{
							//for updates, chances are some fields will not be in interface, don't map these (ie created stuff)
							MapToDB( entity, efEntity );
							if ( IsValidDate( status.EnvelopeCreatedDate ) && status.LocalCreatedDate < efEntity.Created )
							{
								efEntity.Created = status.LocalCreatedDate;
							}
							if ( IsValidDate( status.EnvelopeUpdatedDate ) && status.LocalUpdatedDate != efEntity.LastUpdated )
							{
								efEntity.LastUpdated = status.LocalUpdatedDate;
							}
                            if ( ( efEntity.EntityStateId ?? 1 ) != 2 )
                                efEntity.EntityStateId = 3;

                            entity.EntityStateId = ( int ) efEntity.EntityStateId;
                            //has changed?
                            if ( HasStateChanged( context ) )
							{
								if ( IsValidDate( status.EnvelopeUpdatedDate ) )
									efEntity.LastUpdated = status.LocalUpdatedDate;
								else
									efEntity.LastUpdated = DateTime.Now;
								int count = context.SaveChanges();
								//can be zero if no data changed
								if ( count >= 0 )
								{
									entity.LastUpdated = efEntity.LastUpdated.Value;
									UpdateEntityCache( entity, ref status );
									isValid = true;
								}
								else
								{
									//?no info on error
									status.AddError( "Error - the update was not successful. " );
									string message = string.Format( thisClassName + ".Save Failed", "Attempted to update a PathwaySet. The process appeared to not work, but was not an exception, so we have no message, or no clue. PathwayId: {0}, Id: {1}.", entity.Id, entity.Id );
									EmailManager.NotifyAdmin( thisClassName + ". Pathway_Update Failed", message );
								}
							}
							//continue with parts regardless
							UpdateParts( entity, ref status );
						}
						else
						{
							status.AddError( "Error - update failed, as record was not found." );
						}
					}

				}
				//catch ( System.Data.Entity.Validation.DBEntityValidationException dbex )
				//{
				//	//LoggingHelper.LogError( dbex, thisClassName + string.Format( ".ContentAdd() DBEntityValidationException, Type:{0}", entity.TypeId ) );
				//	string message = thisClassName + string.Format( ".Pathway_Add() DBEntityValidationException, PathwayId: {0}", PathwaySet.Id );
				//	foreach ( var eve in dbex.EntityValidationErrors )
				//	{
				//		message += string.Format( "\rEntity of type \"{0}\" in state \"{1}\" has the following validation errors:",
				//			eve.Entry.Entity.GetType().Name, eve.Entry.State );
				//		foreach ( var ve in eve.ValidationErrors )
				//		{
				//			message += string.Format( "- Property: \"{0}\", Error: \"{1}\"",
				//				ve.PropertyName, ve.ErrorMessage );
				//		}

				//		LoggingHelper.LogError( message, true );
				//	}
				//}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Save(), PathwaySet: '{0}'", entity.Name ) );
					status.AddError( string.Format( "PathwayManager.Save Failed. PathwaySet: {0}, SubjectWebpage: {1}, Error: {2}", entity.Name, entity.SubjectWebpage, ex.Message ) );
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
			bool isAllValid = true;
			Entity_AgentRelationshipManager eamgr = new Entity_AgentRelationshipManager();
			Entity relatedEntity = EntityManager.GetEntity( entity.RowId );
			if ( relatedEntity == null || relatedEntity.Id == 0 )
			{
				status.AddError( thisClassName + " - Error - the parent entity was not found." );
				return false;
			}
			eamgr.DeleteAll( relatedEntity, ref status );

			eamgr.SaveList( relatedEntity.Id, Entity_AgentRelationshipManager.ROLE_TYPE_OFFERED_BY, entity.OfferedBy, ref status );
			eamgr.SaveList( relatedEntity.Id, Entity_AgentRelationshipManager.ROLE_TYPE_OWNER, entity.OwnedBy, ref status );
			eamgr.SaveList( relatedEntity.Id, Entity_AgentRelationshipManager.ROLE_TYPE_PUBLISHEDBY, entity.PublishedBy, ref status );

			var epmgr = new Entity_PathwayManager();
			//handle pathways - using replace
			//actually just use typical pattern of delete all and then add
			//could be extreme
			//epmgr.DeleteAll( parent.EntityUid, ref status );
			var list = new List<int>();
			//check if we should get the list of ids easier
			foreach( var item in entity.HasPathwayList)
			{
				var p = PathwayManager.GetBasic( item );
				if ( p != null && p.Id > 0 )
					list.Add( p.Id );
				else
				{
					//??
					status.AddError( thisClassName + string.Format( " - Error - the pathway using Guid: {0} entity was not found.", item.ToString()) );
				}
			}
			if ( !new Entity_PathwayManager().Replace( entity.RowId, 1, list, ref status ) )
			{
				isAllValid = false;
			}

			return isAllValid;
		}
		public bool Delete( string ctid, ref string statusMessage )
		{
			bool isValid = true;
			if ( string.IsNullOrWhiteSpace( ctid ) )
			{
				statusMessage = thisClassName + ".Delete() Error - a valid CTID must be provided";
				return false;
			}
			int orgId = 0;
			Guid orgUid = new Guid();
			using ( var context = new EntityContext() )
			{
				try
				{
					context.Configuration.LazyLoadingEnabled = false;
					DBEntity efEntity = context.PathwaySet
								.FirstOrDefault( s => s.CTID == ctid );

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
						string msg = string.Format( " PathwaySet. Id: {0}, Name: {1}, Ctid: {2}.", efEntity.Id, efEntity.Name, efEntity.CTID );
						//18-04-05 mparsons - change to set inactive, and notify - seems to have been some incorrect deletes
						//context.Pathway.Remove( efEntity );
						efEntity.EntityStateId = 0;
						efEntity.LastUpdated = System.DateTime.Now;
						int count = context.SaveChanges();
						if ( count > 0 )
						{
							new ActivityManager().SiteActivityAdd( new SiteActivity()
							{
								ActivityType = "PathwaySet",
								Activity = "Import",
								Event = "Delete",
								Comment = msg,
								ActivityObjectId = efEntity.Id
							} );
							//delete cache
							new EntityManager().EntityCacheDelete( rowId, ref statusMessage );
							isValid = true;
							//add pending request 
							List<String> messages = new List<string>();
							new SearchPendingReindexManager().AddDeleteRequest( CodesManager.ENTITY_TYPE_PATHWAY_SET, efEntity.Id, ref messages );
							//mark owning org for updates (actually should be covered by ReindexAgentForDeletedArtifact
							new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, orgId, 1, ref messages );

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
						statusMessage = "Error: this PathwaySet cannot be deleted as it is being referenced by other items, such as roles or credentials. These associations must be removed before this PathwaySet can be deleted.";
					}
				}
			}
			return isValid;
		}
		public bool Delete( int Id, ref string statusMessage )
		{
			bool isValid = false;
			if ( Id == 0 )
			{
				statusMessage = "Error - missing an identifier for the PathwaySet";
				return false;
			}
			using ( var context = new EntityContext() )
			{
				DBEntity efEntity = context.PathwaySet
							.SingleOrDefault( s => s.Id == Id );

				if ( efEntity != null && efEntity.Id > 0 )
				{
					context.PathwaySet.Remove( efEntity );
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						isValid = true;
					}
				}
				else
				{
					statusMessage = "Error - delete failed, as record was not found.";
				}
			}

			return isValid;
		}


		private bool ValidateProfile( PathwaySet profile, ref SaveStatus status, bool validatingUrls = true )
		{
			status.HasSectionErrors = false;

			if ( string.IsNullOrWhiteSpace( profile.Name ) )
			{
				status.AddError( "Error: A PathwaySet Name is required." );
			}
			if ( !IsGuidValid( profile.PrimaryAgentUID ) )
			{
				//first determine if this is populated in edit mode
				status.AddError( "An owning organization must be provided." );
			}

			if ( string.IsNullOrWhiteSpace( profile.Description ) )
			{
				status.AddWarning( "A PathwaySet Description must be entered" );
			}
			if ( string.IsNullOrWhiteSpace( profile.SubjectWebpage ) )
				status.AddError( "A Subject Webpage name must be entered" );

			else if ( validatingUrls && !IsUrlValid( profile.SubjectWebpage, ref commonStatusMessage ) )
			{
				status.AddError( "The PathwaySet Subject Webpage is invalid. " + commonStatusMessage );
			}


			return status.WasSectionValid;
		}
		#endregion

		#region == Retrieval =======================

		/// <summary>
		/// Get a  pathway set
		/// if includingPathways is false, only a list of pathway ids is returned
		/// </summary>
		/// <param name="id"></param>
		/// <param name="includingPathways"></param>
		/// <returns></returns>
		public static ThisEntity Get( int id, bool includingPathways = true )
		{
			ThisEntity entity = new ThisEntity();
			using ( var context = new EntityContext() )
			{
				DBEntity item = context.PathwaySet
						.SingleOrDefault( s => s.Id == id );

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity, includingPathways );
				}
			}

			return entity;
		}
		/// <summary>
		/// Get a basic PathwaySet by CTID
		/// </summary>
		/// <param name="ctid"></param>
		/// <returns></returns>
		public static ThisEntity GetByCtid( string ctid )
		{

			PathwaySet entity = new PathwaySet();
			if ( string.IsNullOrWhiteSpace( ctid ) )
				return entity;

			using ( var context = new EntityContext() )
			{
				context.Configuration.LazyLoadingEnabled = false;
				EM.PathwaySet item = context.PathwaySet
							.FirstOrDefault( s => s.CTID.ToLower() == ctid.ToLower()
								&& s.EntityStateId > 1
								);

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity );
				}
			}

			return entity;
		}

		public static int Count_ForOwningOrg( Guid orgUid )
		{
			int totalRecords = 0;

			using ( var context = new EntityContext() )
			{
				var results = context.PathwaySet
							.Where( s => s.OwningAgentUid == orgUid && s.EntityStateId == 3 )
							.ToList();

				if ( results != null && results.Count > 0 )
				{
					totalRecords = results.Count();
				}
			}
			return totalRecords;
		}

		//public static List<PathwaySetSummary> SearchByUrl( string subjectWebpage, string pOrderBy, int pageNumber, int pageSize, ref int pTotalRows, int userId = 0, bool autocomplete = false )
		//{
		//	string url = NormalizeUrlData( subjectWebpage );
		//	//skip if an example url
		//	string filter = string.Format( " ( base.Id in (Select Id from PathwaySet where (SubjectWebpage like '{0}%') )) ", url );
		//	int ptotalRows = 0;
		//	var exists = Search( filter, string.Empty, 1, 100, ref ptotalRows );
		//	return exists;
		//}

		//public static List<PathwaySetSummary> Search( BaseSearchModel bsm, ref int pTotalRows )
		//{
		//	return Search( bsm.Filter, bsm.OrderBy, bsm.PageNumber, bsm.PageSize, ref pTotalRows, bsm.UserId, bsm.IsAutocomplete );

		//}
		public static List<PathwaySetSummary> Search( string pFilter, string pOrderBy, int pageNumber, int pageSize, ref int pTotalRows, bool autocomplete = false )
		{
			string connectionString = DBConnectionRO();
			var item = new PathwaySetSummary();
			var list = new List<PathwaySetSummary>();
			var result = new DataTable();

			using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				c.Open();

				if ( string.IsNullOrEmpty( pFilter ) )
				{
					pFilter = string.Empty;
				}

				using ( SqlCommand command = new SqlCommand( "[PathwaySet.ElasticSearch]", c ) )
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
						//string rows = command.Parameters[ 5 ].Value.ToString();
						string rows = command.Parameters[ command.Parameters.Count - 1 ].Value.ToString();
						pTotalRows = Int32.Parse( rows );
						if ( pTotalRows > 0 && result.Rows.Count == 0 )
						{
							//actual this can be a credential.Cache sync issue
							item = new PathwaySetSummary();
							item.Name = "Error: invalid page number. Or this could mean a record is not in the credential cache. ";
							item.Description = "Error: invalid page number. Select displayed page button only.";

							list.Add( item );
							return list;
						}
					}
					catch ( Exception ex )
					{
						pTotalRows = 0;
						LoggingHelper.LogError( ex, thisClassName + string.Format( ".Search() - Execute proc, Message: {0} \r\n Filter: {1} \r\n", ex.Message, pFilter ) );

						item = new PathwaySetSummary();
						item.Name = "Unexpected error encountered. System administration has been notified. Please try again later. ";
						item.Description = ex.Message;

						list.Add( item );
						return list;
					}
				}

				//Used for costs. Only need to get these once. See below. - NA 5/12/2017
				//var currencies = CodesManager.GetCurrencies();
				//var costTypes = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_CREDENTIAL_ATTAINMENT_COST );
				int cntr = 0;
				try
				{
					foreach ( DataRow dr in result.Rows )
					{
						cntr++;
						if ( cntr == 10 )
						{

						}
						//avgMinutes = 0;
						item = new PathwaySetSummary();
						item.SearchRowNumber = GetRowColumn( dr, "RowNumber", 0 );
						item.Id = GetRowColumn( dr, "Id", 0 );

						//item.Name = GetRowColumn( dr, "Name", "missing" );
						item.Name = dr[ "Name" ].ToString();
						item.FriendlyName = FormatFriendlyTitle( item.Name );
						item.CTID = GetRowColumn( dr, "CTID" );
						if ( string.IsNullOrWhiteSpace( item.CTID ) )
							item.IsReferenceVersion = true;

						var owningOrganizationId = GetRowPossibleColumn( dr, "OrganizationId", 0 );
						var owningOrganizationName = GetRowPossibleColumn( dr, "OrganizationName" );
						item.PrimaryOrganizationCTID = GetRowPossibleColumn( dr, "OrganizationCTID" );
						if ( owningOrganizationId > 0 )
						{
							item.PrimaryOrganization = new Organization()
							{
								Id = owningOrganizationId,
								Name = owningOrganizationName,
								CTID = item.PrimaryOrganizationCTID
							};
						}
						var agentUid = GetRowColumn( dr, "OwningAgentUid" );
						if ( Guid.TryParse( agentUid, out Guid aUid ) )
						{
							item.PrimaryOrganization.RowId = aUid;
						}
						//for autocomplete, only need name
						if ( autocomplete )
						{
							list.Add( item );
							continue;
						}

						string rowId = GetRowColumn( dr, "RowId" );
						item.RowId = new Guid( rowId );

						item.Description = dr[ "Description" ].ToString();
						item.SubjectWebpage = dr[ "SubjectWebpage" ].ToString();
						//
						string relatedItems = GetRowColumn( dr, "HasPathways" );
						//if using Stuff
						//string[] array = relatedItems.Split( '|' );
						//if ( array.Count() > 0 )
						//	foreach ( var i in array )
						//	{
						//		if ( !string.IsNullOrWhiteSpace( i ) )
						//			item.HasPathway.Add( i.ToLower() );
						//	}
						//or
						if ( !string.IsNullOrWhiteSpace( relatedItems ) )
						{
							Pathway pw = new Pathway();
							var xDoc = XDocument.Parse( relatedItems );
							foreach ( var child in xDoc.Root.Elements() )
							{
								pw = new Pathway();
								pw.Name = ( string )child.Attribute( "Pathway" ) ?? string.Empty;
								pw.Id= int.Parse( child.Attribute( "PathwayId" ).Value );

								item.Pathways.Add( pw );
							}
						}

						//

						DateTime testdate;
						//=====================================
						string date = GetRowPossibleColumn( dr, "EntityLastUpdated", string.Empty );
						if ( DateTime.TryParse( date, out testdate ) )
							item.EntityLastUpdated = testdate;

						item.CredentialRegistryId = dr[ "CredentialRegistryId" ].ToString();
	
						//=====================================================================


						date = GetRowColumn( dr, "Created", string.Empty );
						if ( IsValidDate( date ) )
							item.Created = DateTime.Parse( date );
						date = GetRowColumn( dr, "LastUpdated", string.Empty );
						if ( IsValidDate( date ) )
							item.LastUpdated = DateTime.Parse( date );

						list.Add( item );
					}

					return list;
				}
				catch ( Exception ex )
				{
					pTotalRows = 0;
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".BasicSearch() - Execute proc, Row: {0}, Message: {1} \r\n Filter: {2} \r\n", cntr, ex.Message, pFilter ) );

					item = new PathwaySetSummary();
					item.Name = "Unexpected error encountered. System administration has been notified. Please try again later. ";
					item.Description = ex.Message;
					list.Add( item );
					return list;
				}
			}
		}

		public static void MapFromDB( DBEntity input, ThisEntity output, bool includingPathways = false )
		{

			output.Id = input.Id;
			output.RowId = input.RowId;
			output.CTID = input.CTID;
			output.Name = input.Name;
			output.FriendlyName = FormatFriendlyTitle( input.Name );

			output.Description = input.Description;
			output.SubjectWebpage = input.SubjectWebpage;
			output.EntityStateId = (int)(input.EntityStateId ?? 2);
			//TODO - get pathways
			if ( includingPathways )
			{
				output.Pathways = Entity_PathwayManager.GetAll( output.RowId, true );
				output.HasPathway = output.Pathways.Select( m => m.CTID ).ToList();
			}
			else
			{
				//really only need the pathway ids for publishing - also ctid
				//might just get the lite versions of a pathway?
				output.Pathways = Entity_PathwayManager.GetAll( output.RowId, false );
				//always collect for now
				output.HasPathway = output.Pathways.Select( m => m.CTID ).ToList();
			}

			if ( IsGuidValid( input.OwningAgentUid ) )
			{
				output.PrimaryAgentUID = ( Guid )input.OwningAgentUid;
				output.PrimaryOrganization = OrganizationManager.GetForSummary( output.PrimaryAgentUID );

				//get roles
				OrganizationRoleProfile orp = Entity_AgentRelationshipManager.AgentEntityRole_GetAsEnumerationFromCSV( output.RowId, output.PrimaryAgentUID );
				output.OwnerRoles = orp.AgentRole;
			}
			//
			output.OrganizationRole = Entity_AgentRelationshipManager.AgentEntityRole_GetAll_ToEnumeration( output.RowId, true );

			//confustion over OrganizationRole and OwnerRoles (enum)!!!
			//to.OrganizationRole = Entity_AgentRelationshipManager.AgentEntityRole_GetAll_ToEnumeration( to.RowId, true );
			output.CredentialRegistryId = input.CredentialRegistryId;

			if ( IsValidDate( input.Created ) )
				output.Created = ( DateTime )input.Created;
			if ( IsValidDate( input.LastUpdated ) )
				output.LastUpdated = ( DateTime )input.LastUpdated;

			//NOTE: EntityLastUpdated should really be the last registry update now. Check how LastUpdated is assigned on import
			output.EntityLastUpdated = output.LastUpdated;

		}
		public static void MapToDB( ThisEntity from, DBEntity to )
		{

			to.Id = from.Id;
			if ( to.Id < 1 )
			{
				
			}
			else
			{

			}
			//don't map rowId, ctid, or dates as not on form
			//to.RowId = from.RowId;
			to.Name = from.Name;
			to.CTID = from.CTID;
			if ( !string.IsNullOrWhiteSpace(from.CredentialRegistryId )) 
			{
				//this may not exist if added as pending?
				to.CredentialRegistryId = from.CredentialRegistryId ?? string.Empty;
			}
			to.EntityStateId = from.EntityStateId > 0 ? from.EntityStateId : 3;
			to.Description = from.Description;
			to.SubjectWebpage = from.SubjectWebpage;

			if ( from.PrimaryAgentUID != null )
				to.OwningAgentUid = ( Guid )from.PrimaryAgentUID;



		}

		public static List<Dictionary<string, object>> GetAllForExport_DictionaryList( string owningOrgUid, bool includingConditionProfile = true )
		{
			//
			var result = new List<Dictionary<string, object>>();
			var table = GetAllForExport_DataTable( owningOrgUid, includingConditionProfile );

			foreach ( DataRow dr in table.Rows )
			{
				var rowData = new Dictionary<string, object>();
				for ( var i = 0; i < dr.ItemArray.Count(); i++ )
				{
					rowData[ table.Columns[ i ].ColumnName ] = dr.ItemArray[ i ];
				}
				result.Add( rowData );
			}
			return result;
		}
		//
		public static DataTable GetAllForExport_DataTable( string owningOrgUid, bool includingConditionProfile )
		{
			var result = new DataTable();
			string connectionString = DBConnectionRO();
			//
			using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				c.Open();
				using ( SqlCommand command = new SqlCommand( "[Pathways_Export]", c ) )
				{
					command.CommandType = CommandType.StoredProcedure;
					command.Parameters.Add( new SqlParameter( "@OwningOrgUid", owningOrgUid ) );

					try
					{
						using ( SqlDataAdapter adapter = new SqlDataAdapter() )
						{
							adapter.SelectCommand = command;
							adapter.Fill( result );
						}

					}
					catch ( Exception ex )
					{
						LoggingHelper.LogError( ex, thisClassName + string.Format( ".GetAllForExport_DataTable() - Execute proc, Message: {0} \r\n owningOrgUid: {1} ", ex.Message, owningOrgUid ) );
					}
				}
			}
			return result;
		}

		#endregion

	}
}

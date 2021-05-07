using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

using workIT.Models;
using workIT.Models.Common;
using workIT.Utilities;
using DBEntity = workIT.Data.Tables.ConceptScheme;

using EntityContext = workIT.Data.Tables.workITEntities;
using EM = workIT.Data.Tables;
using ThisEntity = workIT.Models.Common.ConceptScheme;
using ThisEntityItem = workIT.Models.Common.CredentialAlignmentObjectItem;


namespace workIT.Factories
{
	public class ConceptSchemeManager : BaseFactory
	{
		static string thisClassName = "ConceptSchemeManager";

		#region persistance ==================

		/// <summary>
		/// Add/Update a ConceptScheme
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public bool Save( ThisEntity entity,
				ref SaveStatus status, bool addingActivity = false )
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
						entity.EntityStateId = 3;
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

						context.ConceptScheme.Add( efEntity );

						count = context.SaveChanges();

						entity.Id = efEntity.Id;
						entity.RowId = efEntity.RowId;
						if ( count == 0 )
						{
							status.AddWarning( string.Format( " Unable to add Profile: {0} <br\\> ", string.IsNullOrWhiteSpace( entity.Name ) ? "no description" : entity.Name ) );
						}
						else
						{
							if ( addingActivity )
							{
								//add log entry
								SiteActivity sa = new SiteActivity()
								{
									ActivityType = "ConceptScheme",
									Activity = "Import",
									Event = "Add",
									Comment = string.Format( "New ConceptScheme was found by the import. Name: {0}, URI: {1}", entity.Name, entity.Source ),
									ActivityObjectId = entity.Id
								};
								new ActivityManager().SiteActivityAdd( sa );
							}

							UpdateParts( entity, ref status );
							//
							HandleConceptsSimple( entity, ref status );
						}
					}
					else
					{

						efEntity = context.ConceptScheme.FirstOrDefault( s => s.Id == entity.Id );
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
								//NOTE UTC vs central
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
										ActivityType = "ConceptScheme",
										Activity = "Import",
										Event = "Update",
										Comment = string.Format( "Updated ConceptScheme found by the import. Name: {0}, URI: {1}", entity.Name, entity.Source ),
										ActivityObjectId = entity.Id
									};
									new ActivityManager().SiteActivityAdd( sa );
								}
								
							}
							UpdateParts( entity, ref status );
							//
							HandleConceptsSimple( entity, ref status );
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "ConceptSchemeManager.Save()" );
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

			return isAllValid;
		}
		/// <summary>
		/// TBD-may just store the concepts as Json on concept scheme
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="userId"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public bool HandleConceptsSimple( ThisEntity entity, ref SaveStatus status )
		{
			bool isValid = true;
			//first phase: delete all or do a replace
			var efEntity = new EM.ConceptScheme_Concept();
			if ( !entity.HasConcepts.Any() )
			{
				status.AddError( "HandleConceptsSimple - a list of concepts/progression level mames are required for this method." );
				return false;
			}
			using ( var context = new EntityContext() )
			{
				try
				{
					var existing = context.ConceptScheme_Concept.Where( s => s.ConceptSchemeId == entity.Id ).ToList();
					var incomingCTIDs = entity.HasConcepts.Select( x => x.CTID ).ToList();
					//delete records which are not selected 
					var notExisting = existing.Where( x => !incomingCTIDs.Contains( x.CTID ) ).ToList();
					foreach ( var item in notExisting )
					{
						context.ConceptScheme_Concept.Remove( item );
						context.SaveChanges();
					}
					//only get profiles where not existing
					var existingCTIDs = existing.Select( x => x.CTID ).ToList();
					var newConcepts = entity.HasConcepts.Where( y => !existingCTIDs.Contains( y.CTID ) ).ToList();
					var existingConcepts = entity.HasConcepts.Where( y => existingCTIDs.Contains( y.CTID ) ).ToList();
					//
					
					if ( existing != null && existing.Count() > 0 && entity.HasConcepts.Count() > 0 )
					{
						//LoggingHelper.DoTrace( 6, string.Format( thisClassName + ".Replace. Existing: {0}, input: {1}, Not existing(to delete): {2}, newProfiles: {3}", existing.Count(), entity.HasConcepts.Count(), notExisting.Count(), newProfiles.Count() ) );

						if ( existing.Count() != entity.HasConcepts.Count() )
						{

						}
					}
					//
					//**need to handle updates
					foreach ( var item in existingConcepts )
					{
						var updateConcept = entity.HasConcepts.FirstOrDefault( s => s.CTID == item.CTID );
						item.PrefLabel = updateConcept.PrefLabel;
						item.Definition = updateConcept.Definition;
						item.Note = updateConcept.Note;
						item.LastUpdated = System.DateTime.Now;
						context.SaveChanges();
					}
					//
					foreach ( var item in newConcepts )
					{
						//if there are no existing, optimize by not doing check. What about duplicates?
						efEntity = new EM.ConceptScheme_Concept
						{
							ConceptSchemeId = entity.Id,
							PrefLabel = item.PrefLabel,
							Definition = item.Definition,
							Note = item.Note,
							RowId = Guid.NewGuid(),
							CTID = item.CTID,
							IsTopConcept = true,
							Created = DateTime.Now,
							LastUpdated = DateTime.Now
						};
						context.ConceptScheme_Concept.Add( efEntity );
						var count = context.SaveChanges();

					} //foreach


				}

				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".HandleConceptsSimple(), ConceptScheme: {0}", entity.Name ) );
					status.AddError( thisClassName + string.Format( ".HandleConceptsSimple(), ConceptScheme: {0}. Message: ", entity.Name ) + ex.Message );
					isValid = false;
				}
			}
			return isValid;
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
					DBEntity efEntity = context.ConceptScheme
								.FirstOrDefault( s => s.CredentialRegistryId == envelopeId
								|| ( s.CTID == ctid )
								);

					if ( efEntity != null && efEntity.Id > 0 )
					{
						Guid rowId = efEntity.RowId;
						if ( efEntity.OrgId > 0 )
						{
							Organization org = OrganizationManager.GetForSummary( efEntity.OrgId );
							orgId = org.Id;
							orgUid = org.RowId;
						}
						//need to remove from Entity.
						//-using before delete trigger - verify won't have RI issues
						string msg = string.Format( " ConceptScheme. Id: {0}, Name: {1}, Ctid: {2}.", efEntity.Id, efEntity.Name, efEntity.CTID );
						//18-04-05 mparsons - change to set inactive, and notify - seems to have been some incorrect deletes
						//context.ConceptScheme.Remove( efEntity );
						efEntity.EntityStateId = 0;
						efEntity.LastUpdated = System.DateTime.Now;
						int count = context.SaveChanges();
						if ( count > 0 )
						{
							new ActivityManager().SiteActivityAdd( new SiteActivity()
							{
								ActivityType = "ConceptSchemeProfile",
								Activity = "Import",
								Event = "Delete",
								Comment = msg,
								ActivityObjectId = efEntity.Id
							} );
							isValid = true;
							//add pending request 
							List<String> messages = new List<string>();
							new SearchPendingReindexManager().AddDeleteRequest( CodesManager.ENTITY_TYPE_CONCEPT_SCHEME, efEntity.Id, ref messages );
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
						statusMessage = "Error: this ConceptScheme cannot be deleted as it is being referenced by other items. These associations must be removed before this ConceptScheme can be deleted.";
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
				status.AddWarning( "An competency framework name must be entered" );
			}

			//if we don't require url, we can't resolve potentially duplicate framework names


			return status.WasSectionValid;
		}

		#endregion

		#region == Retrieval =======================
		public static ThisEntity GetByCtid( string ctid, bool includingComponents = true )
		{
			ThisEntity entity = new ThisEntity();
			if ( string.IsNullOrWhiteSpace( ctid ) )
				return entity;
			ctid = ctid.ToLower();
			using ( var context = new EntityContext() )
			{
				DBEntity item = context.ConceptScheme
						.FirstOrDefault( s => s.CTID.ToLower() == ctid );
				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity, includingComponents );
				}
			}

			return entity;
		}


		public static ThisEntity Get( int id, bool includingComponents = true )
		{
			ThisEntity entity = new ThisEntity();
			using ( var context = new EntityContext() )
			{
				DBEntity item = context.ConceptScheme
						.FirstOrDefault( s => s.Id == id );

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity, includingComponents );

				}
			}
			return entity;
		}

		public static void MapToDB( ThisEntity from, DBEntity to )
		{

			//want to ensure fields from create are not wiped
			if ( to.Id < 1 )
			{
				if ( IsValidDate( from.Created ) )
					to.Created = from.Created;
			}
			//from may not have these values
			//to.Id = from.Id;
			//to.RowId = from.RowId;
			to.OrgId = from.OrganizationId;
			to.Name = from.Name;
			to.EntityStateId = from.EntityStateId;
			to.Description = from.Description;
			to.IsProgressionModel = from.IsProgressionModel;
			to.CTID = from.CTID;
			//make sure not overwritten
			if ( !string.IsNullOrWhiteSpace( from.CredentialRegistryId ) )
				to.CredentialRegistryId = from.CredentialRegistryId;

			//watch for overwriting these new properties.
			to.Source = from.Source;
			to.PublicationStatusType = from.PublicationStatusType;
			

			//using last updated date from interface, as we don't have all data here. 
			if ( IsValidDate( from.LastUpdated ) )
				to.LastUpdated = from.LastUpdated;


		}

		public static void MapFromDB( DBEntity from, ThisEntity to, bool includingComponents = true )
		{
			to.Id = from.Id;
			to.RowId = from.RowId;
			to.EntityStateId = from.EntityStateId;
			to.OrganizationId = from.OrgId;
			to.Name = from.Name;
			to.Description = from.Description;

			to.IsProgressionModel = from.IsProgressionModel == null ? false : (bool)from.IsProgressionModel;
			to.CTID = from.CTID.ToLower();
			to.Source = from.Source;
		
			to.PublicationStatusType = from.PublicationStatusType;
			to.CredentialRegistryId = from.CredentialRegistryId;
			if ( to.OrganizationId > 0 )
			{
				to.OwningOrganization = OrganizationManager.GetForSummary( to.OrganizationId );

				to.OwningAgentUid = to.OwningOrganization.RowId;
				//get roles- not sure. Can have own and offer
				//OrganizationRoleProfile orp = Entity_AgentRelationshipManager.AgentEntityRole_GetAsEnumerationFromCSV( to.RowId, to.OwningAgentUid );
				//to.OwnerRoles = orp.AgentRole;
				//if ( to.OwnerRoles.HasItems() == false )
				//{
				//	EnumeratedItem ei = Entity_AgentRelationshipManager.GetAgentRole( "Owned By" );
				//	if ( ei == null || ei.Id == 0 )
				//	{
				//		//messages.Add( string.Format( "The organization role: {0} is not valid", "OwnedBy" ) );
				//	}
				//	else
				//	{
				//		to.OwnerRoles.Items.Add( ei );
				//	}
				//}
			}
			//
			to.OrganizationRole = Entity_AgentRelationshipManager.AgentEntityRole_GetAll_ToEnumeration( to.RowId, true );
			//
			if ( from.ConceptScheme_Concept != null && from.ConceptScheme_Concept.Any() )
			{
				foreach ( var item in from.ConceptScheme_Concept )
				{
					to.HasConcepts.Add( new Models.Common.Concept()
					{
						Id = item.Id,
						CTID = item.CTID,
						PrefLabel = item.PrefLabel,
						Definition = item.Definition,
						IsTopConcept = item.IsTopConcept ?? false
					} );
				}
			}
			//
			if ( includingComponents )
			{
				//how to know if this is a progression model? Or just do get anyway
				to.Pathways = PathwayManager.GetAllForProgressionModel( to.CTID );
				to.HasPathway = to.Pathways.Select( m => m.CTID ).ToList();
			}
			//
			if ( IsValidDate( from.Created ) )
				to.Created = ( DateTime )from.Created;
			if ( IsValidDate( from.LastUpdated ) )
				to.LastUpdated = ( DateTime )from.LastUpdated;

		}

		public static List<ConceptSchemeSummary> Search( string pFilter, string pOrderBy, int pageNumber, int pageSize, ref int pTotalRows )
		{
			string connectionString = DBConnectionRO();
			var item = new ConceptSchemeSummary();
			var list = new List<ConceptSchemeSummary>();
			var result = new DataTable();

			using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				c.Open();

				if ( string.IsNullOrEmpty( pFilter ) )
				{
					pFilter = "";
				}

				using ( SqlCommand command = new SqlCommand( "[ConceptScheme_Search]", c ) )
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
						LoggingHelper.DoTrace( 6, thisClassName + string.Format( ".Search() EXCEPTION. \r\n{0}", ex.Message ) );
						LoggingHelper.LogError( ex, thisClassName + string.Format( ".Search() - Execute proc, Message: {0} \r\n Filter: {1} \r\n", ex.Message, pFilter ) );
						item = new ConceptSchemeSummary();
						item.Name = "Unexpected error encountered. System administration has been notified. Please try again later. ";
						item.Description = ex.Message;
						list.Add( item );
						return list;
					}
				}

				foreach ( DataRow dr in result.Rows )
				{
					item = new ConceptSchemeSummary();
					item.Id = GetRowColumn( dr, "Id", 0 );
					item.OrganizationId = GetRowColumn( dr, "OrganizationId", 0 );
					var organizationName = GetRowColumn( dr, "OrganizationName", "" );
					item.PrimaryOrganizationCTID = GetRowColumn( dr, "OrganizationCTID" );
					if ( item.OrganizationId > 0 )
					{
						item.OwningOrganization = new Organization() { Id = item.OrganizationId, Name = organizationName, CTID = item.PrimaryOrganizationCTID };
					}
					item.Name = GetRowColumn( dr, "Name", "???" ); 
					item.FriendlyName = FormatFriendlyTitle( item.Name );
					item.Description = GetRowColumn( dr, "Description" );

					item.CTID = GetRowColumn( dr, "CTID", "" );
					item.IsProgressionModel = GetRowColumn( dr, "IsProgressionModel", false );
					item.CredentialRegistryId = GetRowColumn( dr, "CredentialRegistryId" );
					item.Source = GetRowColumn( dr, "Source" );
					//=====================================
					string date = GetRowPossibleColumn( dr, "Created", "" );
					if ( DateTime.TryParse( date, out DateTime testdate ) )
						item.Created = testdate;

					date = GetRowPossibleColumn( dr, "LastUpdated", "" );
					if ( DateTime.TryParse( date, out testdate ) )
						item.LastUpdated = item.EntityLastUpdated = testdate;

					list.Add( item );
				}

				return list;

			}
		} //


		public static int CountForOwningOrg( int orgId )
		{
			int totalRecords = 0;
			if ( orgId < 1 )
				return totalRecords;

			using ( var context = new EntityContext() )
			{
				var results = context.ConceptScheme.Where( s => s.OrgId == orgId ).ToList();
				if ( results != null && results.Count > 0 )
				{
					totalRecords = results.Count();
				}
			}

			return totalRecords;
		}

		#endregion

		#region Concepts
		public static Concept GetByConceptCtid( string ctid )
		{
			var entity = new Concept();
			if ( string.IsNullOrWhiteSpace( ctid ) )
				return entity;
			ctid = ctid.ToLower();
			using ( var context = new EntityContext() )
			{
				var item = context.ConceptScheme_Concept
						.FirstOrDefault( s => s.CTID.ToLower() == ctid );
				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity );
				}
			}

			return entity;
		}
		public static void MapFromDB( EM.ConceptScheme_Concept from, Concept to )
		{
			to.Id = from.Id;
			to.RowId = from.RowId;
			to.PrefLabel = from.PrefLabel;
			to.CTID = from.CTID.ToLower();
			to.Definition = from.Definition;
			to.Note = from.Note;
			to.IsTopConcept = from.IsTopConcept ?? true;

			//
			if ( IsValidDate( from.Created ) )
				to.Created = ( DateTime )from.Created;
			to.CreatedById = from.CreatedById;
			if ( IsValidDate( from.LastUpdated ) )
				to.LastUpdated = ( DateTime )from.LastUpdated;
			to.LastUpdatedById = from.LastUpdatedById;


			//TODO
			//to.CreatedBy = SetLastActionBy( to.CreatedById, from.AccountCreatedBy );
			//to.LastUpdatedBy = SetLastActionBy( to.LastUpdatedById, from.AccountLastUpdatedBy );

		}
		#endregion
	}
}

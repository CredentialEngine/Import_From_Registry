using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data.Entity;

using workIT.Models;
using ThisEntity = workIT.Models.Common.Collection;
using DBEntity = workIT.Data.Tables.Collection;
using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;

using CM = workIT.Models.Common;
using MPM = workIT.Models.ProfileModels;
using EM = workIT.Data.Tables;
using workIT.Utilities;
using Views = workIT.Data.Views;
using workIT.Models.Common;
using System.Data;
using System.Data.SqlClient;

namespace workIT.Factories
{
	public class CollectionManager : BaseFactory
	{
		#region Persistance
		static readonly string thisClassName = "CollectionManager";
		static string EntityType = "Collection";
		static int EntityTypeId = CodesManager.ENTITY_TYPE_COLLECTION;
		public bool Save(ThisEntity entity, ref SaveStatus status)
		{
			bool isValid = true;
			int count = 0;
			DateTime lastUpdated = System.DateTime.Now;
			try
			{
				using ( var context = new EntityContext() )
				{
					if ( ValidateProfile( entity, ref status ) == false )
					{
						return false;
					}
					if (entity.Id > 0)
					{
						//TODO - consider if necessary, or interferes with anything
						context.Configuration.LazyLoadingEnabled = false;
						DBEntity efEntity = context.Collection
								.SingleOrDefault(s => s.Id == entity.Id);

						if (efEntity != null && efEntity.Id > 0)
						{

							//fill in fields that may not be in entity
							entity.RowId = efEntity.RowId;

							MapToDB(entity, efEntity);

							if (efEntity.EntityStateId == 0)
							{
								SiteActivity sa = new SiteActivity()
								{
									ActivityType = "Collection",
									Activity = "Import",
									Event = "Reactivate",
									Comment = string.Format("Collection had been marked as deleted, and was reactivted by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage),
									ActivityObjectId = entity.Id
								};
								new ActivityManager().SiteActivityAdd(sa);
							}
							//assume and validate, that if we get here we have a full record
							if (efEntity.EntityStateId != 2)
								efEntity.EntityStateId = 3;

							if (IsValidDate(status.EnvelopeCreatedDate) && status.LocalCreatedDate < efEntity.Created)
							{
								efEntity.Created = status.LocalCreatedDate;
							}
							if (IsValidDate(status.EnvelopeUpdatedDate) && status.LocalUpdatedDate != efEntity.LastUpdated)
							{
								efEntity.LastUpdated = status.LocalUpdatedDate;
								lastUpdated = status.LocalUpdatedDate;
							}
							if (HasStateChanged(context))
							{

								if (IsValidDate(status.EnvelopeUpdatedDate))
									efEntity.LastUpdated = status.LocalUpdatedDate;
								else
									efEntity.LastUpdated = DateTime.Now;
								//NOTE efEntity.EntityStateId is set to 0 in delete method )
								count = context.SaveChanges();
								//can be zero if no data changed
								if (count >= 0)
								{
									isValid = true;
								}
								else
								{
									//?no info on error

									isValid = false;
									string message = string.Format(thisClassName + ".Save Failed", "Attempted to update a Collection. The process appeared to not work, but was not an exception, so we have no message, or no clue. Collection: {0}, Id: {1}", entity.Name, entity.Id);
									status.AddError("Error - the update was not successful. " + message);
									EmailManager.NotifyAdmin(thisClassName + ".Save Failed Failed", message);
								}

							}
							else
							{
								//update entity.LastUpdated - assuming there has to have been some change in related data
								new EntityManager().UpdateModifiedDate(entity.RowId, ref status, efEntity.LastUpdated);
							}
							entity.LastUpdated = lastUpdated;
							UpdateEntityCache( entity, ref status );
							if (isValid)
							{
								if (!UpdateParts(entity, ref status))
									isValid = false;

								SiteActivity sa = new SiteActivity()
								{
									ActivityType = "Collection",
									Activity = "Import",
									Event = "Update",
									Comment = string.Format("Collection was updated by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage),
									ActivityObjectId = entity.Id
								};
								new ActivityManager().SiteActivityAdd(sa);
							}
						}
						else
						{
							status.AddError("Error - update failed, as record was not found.");
						}
					}
					else
					{
						//add
						int newId = Add(entity, ref status);
						if (newId == 0 || status.HasErrors)
							isValid = false;
					}
				}
			}
			catch (System.Data.Entity.Validation.DbEntityValidationException dbex)
			{
				string message = HandleDBValidationError(dbex, thisClassName + string.Format(".Save. id: {0}, Name: {1}", entity.Id, entity.Name), "Collection");
				status.AddError(thisClassName + ".Save(). Error - the save was not successful. " + message);
			}
			catch (Exception ex)
			{
				string message = FormatExceptions(ex);
				LoggingHelper.LogError(ex, thisClassName + string.Format(".Save. id: {0}, Name: {1}", entity.Id, entity.Name), true);
				status.AddError(thisClassName + ".Save(). Error - the save was not successful. " + message);
				isValid = false;
			}


			return isValid;
		}


		public bool UpdateParts(ThisEntity entity, ref SaveStatus status)
		{
			bool isAllValid = true;
			var relatedEntity = EntityManager.GetEntity(entity.RowId);
			if (relatedEntity == null || relatedEntity.Id == 0)
			{
				status.AddError("Error - the related Entity was not found.");
				return false;
			}
			Entity_AgentRelationshipManager eamgr = new Entity_AgentRelationshipManager();

			//do deletes - should this be done here, should be no other prior updates?
			eamgr.DeleteAll(relatedEntity, ref status);
			eamgr.SaveList(relatedEntity.Id, Entity_AgentRelationshipManager.ROLE_TYPE_OWNER, entity.OwnedBy, ref status);
			eamgr.SaveList( relatedEntity.Id, Entity_AgentRelationshipManager.ROLE_TYPE_PUBLISHEDBY, entity.PublishedBy, ref status );


			EntityPropertyManager mgr = new EntityPropertyManager();
			//first clear all properties
			mgr.DeleteAll(relatedEntity, ref status);
			//TODO - delete this once fully implemented 
			if (mgr.AddProperties(entity.LifeCycleStatusType, entity.RowId, CodesManager.ENTITY_TYPE_COLLECTION, CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, false, ref status) == false)
				isAllValid = false;

			if (mgr.AddProperties(entity.CollectionType, entity.RowId, CodesManager.ENTITY_TYPE_COLLECTION, CodesManager.PROPERTY_CATEGORY_COLLECTION_CATEGORY, false, ref status) == false)
				isAllValid = false;

			//clear all
			Entity_ReferenceFrameworkManager erfm = new Entity_ReferenceFrameworkManager();
			erfm.DeleteAll(relatedEntity, ref status);

			if (erfm.SaveList(relatedEntity.Id, CodesManager.PROPERTY_CATEGORY_SOC, entity.OccupationType, ref status) == false)
				isAllValid = false;
			if (erfm.SaveList(relatedEntity.Id, CodesManager.PROPERTY_CATEGORY_NAICS, entity.IndustryType, ref status) == false)
				isAllValid = false;

			if (erfm.SaveList(relatedEntity.Id, CodesManager.PROPERTY_CATEGORY_CIP, entity.InstructionalProgramType, ref status) == false)
				isAllValid = false;
			//
			Entity_ReferenceManager erm = new Entity_ReferenceManager();
			erm.DeleteAll(relatedEntity, ref status);
			if (erm.Add(entity.Subject, entity.RowId, CodesManager.ENTITY_TYPE_COLLECTION, ref status, CodesManager.PROPERTY_CATEGORY_SUBJECT, false) == false)
				isAllValid = false;

			if (erm.Add(entity.Keyword, entity.RowId, CodesManager.ENTITY_TYPE_COLLECTION, ref status, CodesManager.PROPERTY_CATEGORY_KEYWORD, false) == false)
				isAllValid = false;

			erm.AddLanguages(entity.InLanguageCodeList, entity.RowId, CodesManager.ENTITY_TYPE_COLLECTION, ref status, CodesManager.PROPERTY_CATEGORY_LANGUAGE);
			//
			//ConditionProfile =======================================
			Entity_ConditionProfileManager emanager = new Entity_ConditionProfileManager();
			//emanager.DeleteAll( relatedEntity, ref status );

			emanager.SaveList(entity.MembershipCondition, Entity_ConditionProfileManager.ConnectionProfileType_Requirement, entity.RowId, ref status);

			//TBD
			//Classification

			//collectionMember

			//HasMember

			//competencies
			//these were gathered separately in the import, but also part of HasMember????
			//update competencies
			//22-06-07 mp NOTE: plan to use Entity.Competency instead of cloning CompetencyFrameworkCompetencyManager.
			new CollectionCompetencyManager().SaveList( entity.Id, entity.ImportCompetencies, ref status );



			return isAllValid;
		}

		public static void MapToDB(ThisEntity input, DBEntity output)
		{

			//want output ensure fields input create are not wiped
			if (output.Id == 0)
			{
				output.CTID = input.CTID;
			}
			if (!string.IsNullOrWhiteSpace(input.CredentialRegistryId))
				output.CredentialRegistryId = input.CredentialRegistryId;

			output.Id = input.Id;
			output.Name = GetData(input.Name);
			output.EntityStateId = input.EntityStateId;
			output.CodedNotation = GetData(input.CodedNotation);
			output.CollectionGraph = input.CollectionGraph;
			output.CredentialRegistryId = input.CredentialRegistryId;
			output.Description = GetData( input.Description );
			output.License = input.License;
			output.LifeCycleStatusTypeId = input.LifeCycleStatusTypeId;
			output.SubjectWebpage = input.SubjectWebpage;

			if (IsGuidValid(input.OwningAgentUid))
			{
				if (output.Id > 0 && output.OwningAgentUid != input.OwningAgentUid)
				{
					if (IsGuidValid(output.OwningAgentUid))
					{
						//need output remove the owner role, or could have been others
						string statusMessage = "";
						new Entity_AgentRelationshipManager().Delete(output.RowId, output.OwningAgentUid, Entity_AgentRelationshipManager.ROLE_TYPE_OWNER, ref statusMessage);
					}
				}
				output.OwningAgentUid = input.OwningAgentUid;
				//get for use to add to elastic pending
				input.OwningOrganization = OrganizationManager.GetForSummary(input.OwningAgentUid);
				//input.OwningOrganizationId = org.Id;
			}
			else
			{
				//always have output have an owner
				//output.OwningAgentUid = null;
			}

			//======================================================================



			if (IsValidDate(input.DateEffective))
				output.DateEffective = DateTime.Parse(input.DateEffective);
			else
				output.DateEffective = null;
			if (IsValidDate(input.ExpirationDate))
				output.ExpirationDate = DateTime.Parse(input.ExpirationDate);
			else
				output.ExpirationDate = null;


			if (IsValidDate(input.LastUpdated))
				output.LastUpdated = input.LastUpdated;
		}
		public int Lookup_OR_Add( string frameworkUri, string frameworkName )
		{
			if ( string.IsNullOrWhiteSpace( frameworkUri ) )
				return 0;

			var ctid = ExtractCtid( frameworkUri );
			//*** no data for frameworkURL, just frameworkUri or sourceUrl
			ThisEntity entity = GetByCTID( ctid );
			if ( entity != null && entity.Id > 0 )
				return entity.Id;
			//skip if no name
			if ( string.IsNullOrWhiteSpace( frameworkName ) )
				return 0;
			SaveStatus status = new SaveStatus();
			entity.Name = frameworkName;
			//
			Save( entity, ref status );
			if ( entity.Id > 0 )
				return entity.Id;

			return 0;
		}//
		

		private int Add(ThisEntity entity, ref SaveStatus status)
		{
			DBEntity efEntity = new DBEntity();
			using (var context = new EntityContext())
			{
				try
				{

					MapToDB(entity, efEntity);

					if (IsValidGuid(entity.RowId))
						efEntity.RowId = entity.RowId;
					else
						efEntity.RowId = Guid.NewGuid();
					efEntity.EntityStateId = 3;
					if (IsValidDate(status.EnvelopeCreatedDate))
					{
						efEntity.Created = status.LocalCreatedDate;
						efEntity.LastUpdated = status.LocalCreatedDate;
					}
					else
					{
						efEntity.Created = System.DateTime.Now;
						efEntity.LastUpdated = System.DateTime.Now;
					}
					context.Collection.Add(efEntity);

					// submit the change to database
					int count = context.SaveChanges();
					if (count > 0)
					{
						entity.RowId = efEntity.RowId;
						entity.Created = (DateTime)efEntity.Created;
						entity.LastUpdated = (DateTime)efEntity.LastUpdated;
						entity.Id = efEntity.Id;
						//
						UpdateEntityCache(entity, ref status);
						//add log entry
						SiteActivity sa = new SiteActivity()
						{
							ActivityType = "Collection",
							Activity = "Import",
							Event = "Add",
							Comment = string.Format("Full Collection was added by the import. Name: {0}, SWP: {1}", entity.Name, entity.SubjectWebpage),
							ActivityObjectId = entity.Id
						};
						new ActivityManager().SiteActivityAdd(sa);

						if (UpdateParts(entity, ref status) == false)
						{
						}

						return efEntity.Id;
					}
					else
					{
						//?no info on error

						string message = thisClassName + string.Format(". Add Failed", "Attempted to add a Collection. The process appeared to not work, but was not an exception, so we have no message, or no clue. Collection: {0}, ctid: {1}", entity.Name, entity.CTID);
						status.AddError(thisClassName + ". Error - the add was not successful. " + message);
						EmailManager.NotifyAdmin("CollectionManager. Add Failed", message);
					}
				}
				catch (System.Data.Entity.Validation.DbEntityValidationException dbex)
				{
					string message = HandleDBValidationError(dbex, thisClassName + ".Add() ", "Collection");
					status.AddError(thisClassName + ".Add(). Error - the save was not successful. " + message);

					LoggingHelper.LogError(message, true);
				}
				catch (Exception ex)
				{
					string message = FormatExceptions(ex);
					LoggingHelper.LogError(ex, thisClassName + string.Format(".Add(), Name: {0}, CTID: {1}, OwningAgentUid: {2}", efEntity.Name, efEntity.CTID, efEntity.OwningAgentUid));
					status.AddError(thisClassName + ".Add(). Error - the save was not successful. \r\n" + message);
				}
			}

			return efEntity.Id;
		}
		public void UpdateEntityCache(ThisEntity document, ref SaveStatus status)
		{
			EntityCache ec = new EntityCache()
			{
				EntityTypeId = EntityTypeId,
				EntityType = EntityType,
				EntityStateId = document.EntityStateId,
				EntityUid = document.RowId,
				BaseId = document.Id,
				Description = document.Description,
				//a list
				//SubjectWebpage = document.SubjectWebpage,
				CTID = document.CTID,
				Created = document.Created,
				LastUpdated = document.LastUpdated,
				Name = document.Name,
				OwningAgentUID = document.OwningAgentUid,
				OwningOrgId = document.OrganizationId
			};
			var statusMessage = "";
			if (new EntityManager().EntityCacheSave(ec, ref statusMessage) == 0)
			{
				status.AddError(thisClassName + string.Format(".UpdateEntityCache for '{0}' ({1}) failed: {2}", document.Name, document.Id, statusMessage));
			}
		}
		public static void MapFromDB(DBEntity input, ThisEntity output)
		{
			//var isForDetail = (request.IsForAPIRequest || request.IsForDetailView);

			output.Name = input.Name;
			output.CTID = input.CTID;
			output.Id = input.Id;
			output.EntityStateId = input.EntityStateId;
			output.FriendlyName = FormatFriendlyTitle(input.Name);
			output.Description = input.Description == null ? "" : input.Description;
			//
			output.SubjectWebpage = input.SubjectWebpage;

			if (IsValidDate(input.Created))
				output.Created = (DateTime)input.Created;
			if (IsValidDate(input.LastUpdated))
				output.LastUpdated = (DateTime)input.LastUpdated;

			output.CollectionType = EntityPropertyManager.FillEnumeration(output.RowId, CodesManager.PROPERTY_CATEGORY_COLLECTION_CATEGORY);
			//22-07-10 - LifeCycleStatusTypeId is now on the credential directly
			output.LifeCycleStatusTypeId = input.LifeCycleStatusTypeId;
			if ( output.LifeCycleStatusTypeId > 0 )
			{
				CodeItem ct = CodesManager.Codes_PropertyValue_Get( output.LifeCycleStatusTypeId );
				if ( ct != null && ct.Id > 0 )
				{
					output.LifeCycleStatus = ct.Title;
				}
				//retain example using an Enumeration for by other related tableS??? - old detail page?
				output.LifeCycleStatusType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_STATUS_TYPE );
				output.LifeCycleStatusType.Items.Add( new EnumeratedItem() { Id = output.LifeCycleStatusTypeId, Name = ct.Name, SchemaName = ct.SchemaName } );
			}
			else
			{
				//OLD
				output.LifeCycleStatusType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS );
				EnumeratedItem statusItem = output.LifeCycleStatusType.GetFirstItem();
				if ( statusItem != null && statusItem.Id > 0 && statusItem.Name != "Active" )
				{

				}
			}
			

			output.CodedNotation = input.CodedNotation;
			output.CollectionGraph = input.CollectionGraph;
			output.CredentialRegistryId = input.CredentialRegistryId;
			//New
			output.OccupationType = Reference_FrameworkItemManager.FillCredentialAlignmentObject( output.RowId, CodesManager.PROPERTY_CATEGORY_SOC );
			output.IndustryType = Reference_FrameworkItemManager.FillCredentialAlignmentObject( output.RowId, CodesManager.PROPERTY_CATEGORY_NAICS );
			output.InstructionalProgramType = Reference_FrameworkItemManager.FillCredentialAlignmentObject( output.RowId, CodesManager.PROPERTY_CATEGORY_CIP );
			//Old
			//output.Occupation = Reference_FrameworkItemManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_SOC );
			//output.Industry = Reference_FrameworkItemManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_NAICS );
			//output.InstructionalProgramTypes = Reference_FrameworkItemManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_CIP );

			output.License = input.License;

			//------------------------------------------------------------------------

			if (IsValidDate(input.DateEffective))
				output.DateEffective = ((DateTime)input.DateEffective).ToString("yyyy-MM-dd");
			else
				output.DateEffective = "";
			//
			if (IsValidDate(input.ExpirationDate))
				output.ExpirationDate = ((DateTime)input.ExpirationDate).ToString("yyyy-MM-dd");
			else
				output.ExpirationDate = "";


			//multiple languages, now in entity.reference
			output.InLanguageCodeList = Entity_ReferenceManager.GetAll(output.RowId, CodesManager.PROPERTY_CATEGORY_LANGUAGE);

			output.Subject = Entity_ReferenceManager.GetAll(output.RowId, CodesManager.PROPERTY_CATEGORY_SUBJECT);

			output.Keyword = Entity_ReferenceManager.GetAll(output.RowId, CodesManager.PROPERTY_CATEGORY_KEYWORD);

			try
			{
				//get condition profiles
				output.MembershipCondition = Entity_ConditionProfileManager.GetAll(output.RowId, false);

				//get members
			}
			catch (Exception ex)
			{
				LoggingHelper.LogError(ex, thisClassName + string.Format(".MapFromDB(), Name: {0} ({1})", output.Name, output.Id));
				output.StatusMessage = FormatExceptions(ex);
			}

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
					DBEntity efEntity = context.Collection
								.FirstOrDefault( s => s.CTID == ctid );

					if ( efEntity != null && efEntity.Id > 0 )
					{
						Guid rowId = efEntity.RowId;
						if ( IsValidGuid( efEntity.OwningAgentUid ) )
						{
							Organization org = OrganizationManager.GetBasics( ( Guid ) efEntity.OwningAgentUid );
							orgId = org.Id;
							orgUid = org.RowId;
						}
						//need to remove from Entity.
						//-using before delete trigger - verify won't have RI issues
						string msg = string.Format( " Collection. Id: {0}, Name: {1}, Ctid: {2}.", efEntity.Id, efEntity.Name, efEntity.CTID );
						efEntity.EntityStateId = 0;
						efEntity.LastUpdated = System.DateTime.Now;
						int count = context.SaveChanges();
						if ( count > 0 )
						{
							new ActivityManager().SiteActivityAdd( new SiteActivity()
							{
								ActivityType = "Collection",
								Activity = "Import",
								Event = "Delete",
								Comment = msg,
								ActivityObjectId = efEntity.Id
							} );
							//delete cache
							new EntityManager().EntityCacheDelete( CodesManager.ENTITY_TYPE_COLLECTION, efEntity.Id, ref statusMessage );
							isValid = true;
							//add pending request 
							List<String> messages = new List<string>();
							new SearchPendingReindexManager().AddDeleteRequest( CodesManager.ENTITY_TYPE_COLLECTION, efEntity.Id, ref messages );
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
						statusMessage = "Error: this Collection cannot be deleted as it is being referenced by other items, such as roles or credentials. These associations must be removed before this Collection can be deleted.";
					}
				}
			}
			return isValid;
		}

		#endregion

		#region Retrievals

		public static ThisEntity Get(int id)
		{

			var entity = new ThisEntity();
			if (id < 1)
				return entity;

			using (var context = new EntityContext())
			{
				//if ( cr.IsForProfileLinks )
				//	context.Configuration.LazyLoadingEnabled = false;
				EM.Collection item = context.Collection
							.SingleOrDefault(s => s.Id == id
								);

				if (item != null && item.Id > 0)
				{
					MapFromDB(item, entity);

					//Other parts
				}
			}

			return entity;
		}
		public static ThisEntity GetByCTID(string ctid)
		{

			var entity = new ThisEntity();
			if (string.IsNullOrWhiteSpace(ctid))
				return entity;

			using (var context = new EntityContext())
			{
				//if ( cr.IsForProfileLinks )
				//	context.Configuration.LazyLoadingEnabled = false;
				EM.Collection item = context.Collection
							.FirstOrDefault(s => s.CTID.ToLower() == ctid.ToLower());

				if (item != null && item.Id > 0)
				{
					MapFromDB(item, entity);

					//Other parts
				}
			}

			return entity;
		}

		public static int Count_ForOwningOrg( Guid orgUID )
		{
			int totalRecords = 0;
			if ( !IsGuidValid( orgUID))
				return totalRecords;

			using ( var context = new EntityContext() )
			{
				var query = ( from entity in context.Collection
							  join org in context.Organization on entity.OwningAgentUid equals org.RowId
							  where entity.OwningAgentUid == orgUID
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

		public static List<ThisEntity> Search( string pFilter, string pOrderBy, int pageNumber, int pageSize, ref int pTotalRows )
		{
			string connectionString = DBConnectionRO();
			var output = new ThisEntity();
			var list = new List<ThisEntity>();
			var result = new DataTable();

			using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				c.Open();

				if ( string.IsNullOrEmpty( pFilter ) )
				{
					pFilter = "";
				}

				using ( SqlCommand command = new SqlCommand( "[Collection.ElasticSearch]", c ) )
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
						string rows = command.Parameters[4].Value.ToString();
						pTotalRows = Int32.Parse( rows );
					}
					catch ( Exception ex )
					{
						LoggingHelper.DoTrace( 2, "Search - Exception:\r\n " + ex.Message );
						LoggingHelper.LogError( ex, "Search", true, thisClassName + ".Search Error" );
						output = new ThisEntity();
						output.Name = "EXCEPTION ENCOUNTERED";
						output.Description = ex.Message;
						list.Add( output );
						pTotalRows = -1;
						return list;
					}
				}
				int rowNbr = ( pageNumber - 1 ) * pageSize;
				foreach ( DataRow dr in result.Rows )
				{
					rowNbr++;
					output = new ThisEntity();
					output.ResultNumber = rowNbr;
					output.Id = GetRowColumn( dr, "Id", 0 );
					output.CTID = GetRowColumn( dr, "CTID", "" );
					output.Name = GetRowColumn( dr, "Name", "???" );
					//
					var organizationEntityStateId = GetRowColumn( dr, "OrganizationEntityStateId", 0 );
					if ( organizationEntityStateId > 1 )
					{
						output.PrimaryOrganizationName = GetRowColumn( dr, "OrganizationName", "" );
						output.OrganizationId = GetRowColumn( dr, "OrganizationId", 0 );
						output.PrimaryOrganizationCTID = GetRowColumn( dr, "OrganizationCTID", "" );
					}
					output.Description = GetRowColumn( dr, "Description", "" );
					//needs to be a list. Just store first one
					var subjectWebpage = GetRowColumn( dr, "SubjectWebpage", "" );
					//if ( !string.IsNullOrWhiteSpace( subjectWebpage ) )
					//{
					//	//do a split
					//	string[] swps = subjectWebpage.Split( '|' );
					//	foreach ( var item in swps )
					//	{
					//		output.SubjectWebpage.Add(item.Trim());
					//	}
					//}
					output.CodedNotation = GetRowColumn( dr, "CodedNotation", "" );
					output.DateEffective = GetRowColumn( dr, "DateEffective", "" );
					output.ExpirationDate = GetRowColumn( dr, "ExpirationDate", "" );


					//Actuallu LastUpdated is the correct last updated
					output.EntityLastUpdated = GetRowColumn( dr, "LastUpdated", System.DateTime.MinValue );
					output.Created = GetRowColumn( dr, "Created", System.DateTime.MinValue );
					output.LastUpdated = GetRowColumn( dr, "LastUpdated", System.DateTime.MinValue );

					list.Add( output );
				}

				return list;

			}
		} //
		#endregion

		public bool ValidateProfile( ThisEntity profile, ref SaveStatus status )
		{
			status.HasSectionErrors = false;

			if ( string.IsNullOrWhiteSpace( profile.Name ) )
			{
				status.AddError( "A Collection name must be entered" );
			}
			if ( string.IsNullOrWhiteSpace( profile.Description ) )
			{
				//status.AddWarning( "An Assessment Description must be entered" );
			}
			if ( !IsValidGuid( profile.OwningAgentUid ) )
			{
				//status.AddWarning( "An owning organization must be selected" );
			}
			if ( !string.IsNullOrWhiteSpace( profile.DateEffective ) && !IsValidDate( profile.DateEffective ) )
			{
				//status.AddWarning( "Invalid Assessment effective date" );
			}

			//
			var defStatus = CodesManager.Codes_PropertyValue_GetBySchema( CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS_ACTIVE );
			if ( profile.LifeCycleStatusType == null || profile.LifeCycleStatusType.Items == null || profile.LifeCycleStatusType.Items.Count == 0 )
			{
				profile.LifeCycleStatusTypeId = defStatus.Id;
			}
			else
			{
				var schemaName = profile.LifeCycleStatusType.GetFirstItem().SchemaName;
				CodeItem ci = CodesManager.Codes_PropertyValue_GetBySchema( CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, schemaName );
				if ( ci == null || ci.Id < 1 )
				{
					//while this should never happen, should have a default
					status.AddError( string.Format( "A valid LifeCycleStatusType must be included. Invalid: {0}", schemaName ) );
					profile.LifeCycleStatusTypeId = defStatus.Id;
				}
				else
					profile.LifeCycleStatusTypeId = ci.Id;
			}
			
			return status.WasSectionValid;
		}

	}
}

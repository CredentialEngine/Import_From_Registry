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
//using workIT.Models.Helpers.Cass;

using workIT.Utilities;

using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;
using DBEntity = workIT.Data.Tables.EducationFramework;
using ThisEntity = workIT.Models.ProfileModels.EducationFramework;
using ThisEntityItem = workIT.Models.Common.CredentialAlignmentObjectItem;
using Views = workIT.Data.Views;

using EM = workIT.Data.Tables;


namespace workIT.Factories
{
	public class EducationFrameworkManager : BaseFactory
	{
		static string thisClassName = "EducationFrameworkManager";
		#region --- EducationFrameworkManager ---
		#region Persistance ===================
		/// <summary>
		/// Check if the provided framework has already been sync'd. 
		/// If not, it will be added. 
		/// </summary>
		/// <param name="request"></param>
		/// <param name="userId"></param>
		/// <param name="messages"></param>
		/// <param name="frameworkId"></param>
		/// <returns></returns>
		//public bool HandleFrameworkRequest( CassFramework request,
		//		int userId,
		//		ref SaveStatus status,
		//		ref int frameworkId )
		//{
		//	bool isValid = true;
		//	if ( request == null || string.IsNullOrWhiteSpace(request._IdAndVersion) )
		//	{
		//		status.AddWarning( "The Cass Request doesn't contain a valid Cass Framework class." );
		//		return false;
		//	}
		//	ThisEntity item = Get( request._IdAndVersion );
		//	if (item != null && item.Id > 0)
		//	{
		//		//TODO - do we want to attempt an update - if changed
		//		//		- if we plan to implement a batch refresh of sync'd content, then not necessary
		//		frameworkId = item.Id;
		//		return true;
		//	}
		//	//add the framework...
		//	ThisEntity entity = new ThisEntity();
		//	entity.Name = request.Name;
		//	entity.Description = request.Description;
		//	entity.FrameworkUrl = request.Url;
		//	entity.RepositoryUri = request._IdAndVersion;

		//	//TDO - need owning org - BUT, first person to reference a framework is not necessarily the owner!!!!!
		//	//actually, we may not care here. Eventually get a ctid from CASS
		//	//entity.OwningOrganizationId = 0;

		//	isValid = Save( entity, userId, ref status );
		//	frameworkId = entity.Id;
		//	return isValid;
		//}

		
		//actually not likely to have a separate list of frameworks
		//public bool SaveList( List<ThisEntity> list, ref SaveStatus status )
		//{
		//	if ( list == null || list.Count == 0 )
		//		return true;

		//	bool isAllValid = true;
		//	foreach ( ThisEntity item in list )
		//	{
		//		Save( item, ref status );
		//	}

		//	return isAllValid;
		//}

		/// <summary>
		/// Add/Update a EducationFramework
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
						MapToDB( entity, efEntity );

						efEntity.Created = efEntity.LastUpdated = DateTime.Now;
						
						if ( IsValidGuid( entity.RowId ) )
							efEntity.RowId = entity.RowId;
						else
							efEntity.RowId = Guid.NewGuid();

						context.EducationFramework.Add( efEntity );

						count = context.SaveChanges();

						entity.Id = efEntity.Id;
						entity.RowId = efEntity.RowId;
						if ( count == 0 )
						{
							status.AddWarning( string.Format( " Unable to add Profile: {0} <br\\> ", string.IsNullOrWhiteSpace( entity.FrameworkName ) ? "no description" : entity.FrameworkName ) );
						} else
						{
							if ( addingActivity )
							{
								//add log entry
								SiteActivity sa = new SiteActivity()
								{
									ActivityType = "CompetencyFramework",
									Activity = "Import",
									Event = "Add",
									Comment = string.Format( "New Competency Framework was found by the import. Name: {0}, URI: {1}", entity.FrameworkName, entity.FrameworkUri ),
									ActivityObjectId = entity.Id
								};
								new ActivityManager().SiteActivityAdd( sa );
							}
						}
					}
					else
					{

						efEntity = context.EducationFramework.FirstOrDefault( s => s.Id == entity.Id );
						if ( efEntity != null && efEntity.Id > 0 )
						{
							entity.RowId = efEntity.RowId;
							//update
							MapToDB( entity, efEntity );
							//has changed?
							if ( HasStateChanged( context ) )
							{
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
										Comment = string.Format( "Updated Competency Framework found by the import. Name: {0}, URI: {1}", entity.FrameworkName, entity.FrameworkUri ),
										ActivityObjectId = entity.Id
									};
									new ActivityManager().SiteActivityAdd( sa );
								}
							}
						}
					}
				}
			}catch (Exception ex)
			{
				LoggingHelper.LogError( ex, "EducationFrameworkManager.Save()" );
			}

			return isValid;
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
				DBEntity p = context.EducationFramework.FirstOrDefault( s => s.Id == recordId );
				if ( p != null && p.Id > 0 )
				{
					context.EducationFramework.Remove( p );
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
					var efEntity = context.EducationFramework
								.FirstOrDefault( s => s.CTID == ctid );

					if ( efEntity != null && efEntity.Id > 0 )
					{
						//TODO - may need a check for existing alignments
						Guid rowId = efEntity.RowId;
						var orgCtid = efEntity.OrganizationCTID ?? "";
						//need to remove from Entity.
						//-using before delete trigger - verify won't have RI issues
						string msg = string.Format( " CompetencyFramework. Id: {0}, Name: {1}, Ctid: {2}", efEntity.Id, efEntity.FrameworkName, efEntity.CTID );
						//leaving as virtual?
						//need to check for in use.
						//context.EducationFramework.Remove( efEntity );
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
						}
						if ( !string.IsNullOrWhiteSpace( orgCtid ) )
						{
							List<String> messages = new List<string>();
							//mark owning org for updates 
							//	- nothing yet from frameworks
							var org = OrganizationManager.GetByCtid( orgCtid );
							if ( org != null && org.Id > 0 )
							{
								new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_ORGANIZATION, org.Id, 1, ref messages );
							} else
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

			if ( string.IsNullOrWhiteSpace( profile.FrameworkName ) )
			{
				status.AddWarning( "An educational framework name must be entered" );
			}
		
			//if we don't require url, we can't resolve potentially duplicate framework names


			return !status.HasSectionErrors;
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
					DBEntity item = context.EducationFramework
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

		public int Lookup_OR_Add( string frameworkUri, string frameworkName )
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
			entity.FrameworkName = frameworkName;
			//this could an external Url, or a registry Uri
			if ( frameworkUri.ToLower().IndexOf( "credentialengineregistry.org/resources/" ) > -1
					|| frameworkUri.ToLower().IndexOf( "credentialengineregistry.org/graph/" ) > -1 )
			    entity.FrameworkUri = frameworkUri;
            else
                entity.SourceUrl = frameworkUri;
            Save( entity, ref status );
			if (entity.Id > 0)
				return entity.Id;

			return frameworkId;
		}//

		public static ThisEntity GetByUrl( string frameworkUri )
		{
			ThisEntity entity = new ThisEntity();
			if ( string.IsNullOrWhiteSpace( frameworkUri ))
				return entity;
			try
			{
				using ( var context = new EntityContext() )
				{
                    //lookup by frameworkUri, or SourceUrl
					DBEntity item = context.EducationFramework
							.FirstOrDefault( s => s.FrameworkUri.ToLower() == frameworkUri.ToLower()
                            || s.SourceUrl.ToLower() == frameworkUri.ToLower()
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
					//lookup by frameworkUri, or SourceUrl
					DBEntity item = context.EducationFramework
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
		public static void MapToDB( ThisEntity from, DBEntity to )
		{
			//want to ensure fields from create are not wiped
			//to.Id = from.Id;

			to.FrameworkName = from.FrameworkName;
			to.SourceUrl = from.SourceUrl ?? "";
			to.FrameworkUri = from.FrameworkUri ?? "";
			to.CredentialRegistryId = from.CredentialRegistryId ?? "";
			//will want to extract from FrameworkUri (for now)
			if (!string.IsNullOrWhiteSpace(from.CTID) && from.CTID.Length == 39 )
                to.CTID = from.CTID;
            else
            {
                if ( to.FrameworkUri.ToLower().IndexOf("credentialengineregistry.org/resources/ce-") > -1 
					|| to.FrameworkUri.ToLower().IndexOf( "credentialengineregistry.org/graph/ce-" ) > -1 )
                {
                    to.CTID = from.FrameworkUri.Substring(from.FrameworkUri.IndexOf("/ce-") + 1);

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
			if ( from.ExistsInRegistry || ( !string.IsNullOrWhiteSpace( to.CredentialRegistryId ) && to.CredentialRegistryId.Length == 36 ) )
			{
				to.EntityStateId = 3;
				to.ExistsInRegistry = from.ExistsInRegistry;
			} else
			{
				//dont think there is a case to set to 1
				to.EntityStateId = 2;
			}

		} //

		public static void MapFromDB( DBEntity from, ThisEntity to)
		{
			to.Id = from.Id;
			to.RowId = from.RowId;
			to.EntityStateId = from.EntityStateId;
			to.FrameworkName = from.FrameworkName;
            to.CTID = from.CTID;
			to.OrganizationCTID = from.OrganizationCTID ?? "";
			to.SourceUrl = from.SourceUrl;
            to.FrameworkUri = from.FrameworkUri;
			to.CredentialRegistryId = from.CredentialRegistryId ?? "";
			//this should be replace by presence of CredentialRegistryId
			to.ExistsInRegistry = (bool)from.ExistsInRegistry;
			if ( from.Created != null )
				to.Created = ( DateTime )from.Created;
			if ( from.LastUpdated != null )
				to.LastUpdated = (DateTime) from.LastUpdated;

            //soon to be obsolete
			//to.FrameworkUrl = from.FrameworkUrl;
		}

		#endregion

		public static int FrameworkCount_ForOwningOrg( string orgCtid )
		{
			int totalRecords = 0;
			if ( string.IsNullOrWhiteSpace( orgCtid ) || orgCtid.Trim().Length != 39 )
				return totalRecords;

			using ( var context = new EntityContext() )
			{
				var query = ( from entity in context.EducationFramework
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

		public static List<ThisEntityItem> Search( string pFilter, string pOrderBy, int pageNumber, int pageSize, ref int pTotalRows )
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

					list.Add( item );
				}

				return list;

			}
		} //
		#endregion
	}
}

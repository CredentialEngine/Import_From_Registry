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
				ref SaveStatus status )
		{
			bool isValid = true;
			int count = 0;

			DBEntity efEntity = new DBEntity();
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

					efEntity.Created = DateTime.Now;
					efEntity.RowId = Guid.NewGuid();

					context.EducationFramework.Add( efEntity );

					count = context.SaveChanges();

					entity.Id = efEntity.Id;
					entity.RowId = efEntity.RowId;
					if ( count == 0 )
					{
						status.AddWarning( string.Format( " Unable to add Profile: {0} <br\\> ", string.IsNullOrWhiteSpace( entity.FrameworkName ) ? "no description" : entity.FrameworkName ) );
					}
				}
				else
				{

					efEntity = context.EducationFramework.SingleOrDefault( s => s.Id == entity.Id );
					if ( efEntity != null && efEntity.Id > 0 )
					{
						entity.RowId = efEntity.RowId;
						//update
						MapToDB( entity, efEntity );
						//has changed?
						if ( HasStateChanged( context ) )
						{
							count = context.SaveChanges();
						}
					}
				}
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

		public int Lookup_OR_Add( string frameworkUrl, string frameworkName )
		{
			int frameworkId = 0;
			if ( string.IsNullOrWhiteSpace( frameworkUrl ) )
				return 0;

			ThisEntity entity = GetByUrl( frameworkUrl );
			if ( entity != null && entity.Id > 0 )
				return entity.Id;
			//skip if no name
			if ( string.IsNullOrWhiteSpace( frameworkName ) )
				return 0;
			SaveStatus status = new SaveStatus();
			entity.FrameworkName = frameworkName;
            //this could an external Url, or a registry Uri
            if (frameworkUrl.ToLower().IndexOf("credentialengineregistry.org/resources/") > -1 )
			    entity.FrameworkUri = frameworkUrl;
            else
                entity.SourceUrl = frameworkUrl;
            Save( entity, ref status );
			if (entity.Id > 0)
				return entity.Id;

			return frameworkId;
		}//

		public static ThisEntity GetByUrl( string frameworkUrl )
		{
			ThisEntity entity = new ThisEntity();
			if ( string.IsNullOrWhiteSpace( frameworkUrl ))
				return entity;
			try
			{
				using ( var context = new EntityContext() )
				{
                    //lookup by frameworkUri, or SourceUrl
					DBEntity item = context.EducationFramework
							.FirstOrDefault( s => s.FrameworkUri.ToLower() == frameworkUrl.ToLower()
                            || s.SourceUrl.ToLower() == frameworkUrl.ToLower()
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

		public static void MapToDB( ThisEntity from, DBEntity to )
		{
			//want to ensure fields from create are not wiped
			//to.Id = from.Id;

			to.FrameworkName = from.FrameworkName;
            //will want to extract from FrameworkUri (for now)
            if (!string.IsNullOrWhiteSpace(from.CTID) && from.CTID.Length == 39 )
                to.CTID = from.CTID;
            else
            {
                if ( from.FrameworkUri.ToLower().IndexOf("credentialengineregistry.org/resources/ce-") > -1 )
                {
                    to.CTID = from.FrameworkUri.Substring(from.FrameworkUri.IndexOf("/ce-") + 1);

                }
                //else if ( from.FrameworkUri.ToLower().IndexOf("credentialengineregistry.org/resources/ce-") > -1 )
                //{
                //    to.CTID = from.FrameworkUri.Substring(from.FrameworkUri.IndexOf("/ce-") + 1);
                //}
            }
            to.SourceUrl = from.SourceUrl;
            to.FrameworkUri = from.FrameworkUri;

            //soon to be obsolete
            //to.FrameworkUrl = from.FrameworkUrl;
		} //

		public static void MapFromDB( DBEntity from, ThisEntity to)
		{
			to.Id = from.Id;
			to.RowId = from.RowId;
			to.FrameworkName = from.FrameworkName;
            to.CTID = from.CTID;
            to.SourceUrl = from.SourceUrl;
            to.FrameworkUri = from.FrameworkUri;
            //soon to be obsolete
			//to.FrameworkUrl = from.FrameworkUrl;
		}

		#endregion


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

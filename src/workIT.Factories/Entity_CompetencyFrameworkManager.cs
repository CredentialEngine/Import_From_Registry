using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.ProfileModels;
//using MN = workIT.Models.Node;
using workIT.Utilities;

using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;
using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;

using DBEntity = workIT.Data.Tables.Entity_CompetencyFramework;
using DBBntityItem = workIT.Data.Tables.Entity_CompetencyFrameworkItem;
using ThisEntity = workIT.Models.Common.CredentialAlignmentObjectFrameworkProfile;
using ThisEntityItem = workIT.Models.Common.CredentialAlignmentObjectItem;

namespace workIT.Factories
{
	public class Entity_CompetencyFrameworkManager : BaseFactory
	{
		static string thisClassName = "Entity_CompetencyFrameworkManager";
		#region --- Entity_CompetencyFramework ---
		#region Persistance ===================
		public bool SaveList( List<ThisEntity> list, Guid parentUid, ref SaveStatus status )
		{
			if ( list == null || list.Count == 0 )
				return true;

			bool isAllValid = true;
			foreach ( ThisEntity item in list )
			{
				Save( item, parentUid, ref status );
			}

			return isAllValid;
		}

		/// <summary>
		/// Add/Update a competency
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="parentUid"></param>
		/// <param name="userId"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public bool Save( ThisEntity entity,
				Guid parentUid,
				ref SaveStatus status )
		{
			bool isValid = true;

			if ( !IsValidGuid( parentUid ) )
			{
				status.AddError( thisClassName + "Save(). Error: the parent identifier was not provided." );
				return false;
			}

			int count = 0;

			DBEntity efEntity = new DBEntity();
			Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				status.AddError( thisClassName + string.Format( "Save(). Error - the parent entity (parentUid: {0}) was not found.", parentUid) );
				return false;
			}

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
					efEntity.EntityId = parent.Id;

					efEntity.Created = efEntity.LastUpdated = DateTime.Now;
					efEntity.RowId = Guid.NewGuid();

					context.Entity_CompetencyFramework.Add( efEntity );

					count = context.SaveChanges();

					entity.Id = efEntity.Id;
					entity.ParentId = parent.Id;
					entity.RowId = efEntity.RowId;
					if ( count == 0 )
					{
						status.AddWarning( thisClassName + string.Format( "Save(). Unable to add Profile: {0} <br\\> ", string.IsNullOrWhiteSpace( entity.FrameworkName ) ? "no description" : entity.FrameworkName ) );
					}
				}
				else
				{
					entity.ParentId = parent.Id;

					efEntity = context.Entity_CompetencyFramework.SingleOrDefault( s => s.Id == entity.Id );
					if ( efEntity != null && efEntity.Id > 0 )
					{
						entity.RowId = efEntity.RowId;
						//update
						MapToDB( entity, efEntity );
						//has changed?
						if ( HasStateChanged( context ) )
						{
							efEntity.LastUpdated = System.DateTime.Now;
							count = context.SaveChanges();
						}
					}
				}
			}

			return isValid;
		}

		/// <summary>
		/// Delete a competency
		/// </summary>
		/// <param name="recordId"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool Delete( int recordId, ref string statusMessage )
		{
			bool isOK = true;
			using ( var context = new EntityContext() )
			{
				DBEntity p = context.Entity_CompetencyFramework.FirstOrDefault( s => s.Id == recordId );
				if ( p != null && p.Id > 0 )
				{
					context.Entity_CompetencyFramework.Remove( p );
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

			//could check for alignment type, but this is typically set by context, and not entered.
			return !status.HasSectionErrors;
		}

		#endregion
		#region  retrieval ==================

		/// <summary>
		/// Get all profiles for the parent
		/// Uses the parent Guid to retrieve the related ThisEntity, then uses the EntityId to retrieve the child objects.
		/// </summary>
		/// <param name="parentUid"></param>
		/// <param name="alignmentType">If blank, get all types.</param>
		public static List<ThisEntity> GetAll( Guid parentUid, string alignmentType )
		{
			ThisEntity entity = new ThisEntity();
			List<ThisEntity> list = new List<ThisEntity>();
			//Views.Entity_Summary parent = EntityManager.GetDBEntity( parentUid );
			Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				return list;
			}
			try
			{
				using ( var context = new EntityContext() )
				{
					List<DBEntity> results = context.Entity_CompetencyFramework
							.Where( s => s.EntityId == parent.Id
							&& ( alignmentType == "" || s.AlignmentType == alignmentType ) )
							.OrderBy( s => s.EducationalFrameworkName )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( DBEntity item in results )
						{
							entity = new ThisEntity();
							MapFromDB( item, entity, true );
							list.Add( entity );
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetAll" );
			}
			return list;
		}//

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
					DBEntity item = context.Entity_CompetencyFramework
							.SingleOrDefault( s => s.Id == profileId );

					if ( item != null && item.Id > 0 )
					{
						MapFromDB( item, entity, true );
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".Get" );
			}
			return entity;
		}//


		public static void MapToDB( ThisEntity from, DBEntity to )
		{
			//want to ensure fields from create are not wiped
			//to.Id = from.Id;

			to.EducationalFrameworkName = from.EducationalFrameworkName;
			int pos2 = from.EducationalFrameworkName.ToLower().IndexOf( "jquery" );
			if ( pos2 > 1 )
			{
				from.EducationalFrameworkName = from.EducationalFrameworkName.Substring( 0, pos2 );
			}
			to.EducationalFrameworkUrl = from.EducationalFrameworkUrl;

			//TODO work to eliminate
			to.AlignmentType = from.AlignmentType;
			if ( from.AlignmentTypeId > 0 )
				to.AlignmentTypeId = from.AlignmentTypeId;
			else if ( !string.IsNullOrWhiteSpace( to.AlignmentType ) )
			{
				CodeItem item = CodesManager.Codes_PropertyValue_Get( CodesManager.PROPERTY_CATEGORY_ALIGNMENT_TYPE, to.AlignmentType );
				if ( item != null && item.Id > 0 )
					to.AlignmentTypeId = item.Id;
			}

		}
		public static void MapFromDB( DBEntity from, ThisEntity to, bool includingItems = true )
		{
			to.Id = from.Id;
			to.RowId = from.RowId;
			to.ParentId = from.EntityId;

			to.ProfileName = from.EducationalFrameworkName;
			to.EducationalFrameworkName = from.EducationalFrameworkName;

			to.EducationalFrameworkUrl = from.EducationalFrameworkUrl;

			to.AlignmentTypeId = from.AlignmentTypeId ?? 0;
			to.AlignmentType = from.AlignmentType;

			if ( IsValidDate( from.Created ) )
				to.Created = ( DateTime ) from.Created;
			to.CreatedById = from.CreatedById == null ? 0 : ( int ) from.CreatedById;
			if ( IsValidDate( from.LastUpdated ) )
				to.LastUpdated = ( DateTime ) from.LastUpdated;
			to.LastUpdatedById = from.LastUpdatedById == null ? 0 : ( int ) from.LastUpdatedById;
			to.LastUpdatedBy = SetLastUpdatedBy( to.LastUpdatedById, from.Account_Modifier );

			ThisEntityItem ip = new ThisEntityItem();
			//get all competencies - as profile links?
			foreach (DBEntityItem item in from.Entity_CompetencyFrameworkItem) 
			{
				ip = new ThisEntityItem();
				ip.Id = item.Id;
				ip.ParentId = item.EntityFrameworkId;

				ip.Name = item.Name;
				ip.Description = item.Description;
				ip.TargetName  = item.TargetName;
				ip.TargetDescription = item.TargetDescription;
				ip.TargetUrl = item.TargetUrl;
				ip.CodedNotation = item.CodedNotation;

				to.Items.Add(ip);
			}

		}

		#endregion
		#endregion


		#region --- Entity_CompetencyFrameworkItem ---

		#region Persistance ===================

		/// <summary>
		/// Add/Update a competency
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="parentUid"></param>
		/// <param name="userId"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public bool Entity_Competency_Save( ThisEntityItem entity,
				int userId,
				ref SaveStatus status )
		{
			bool isValid = true;
			int intialCount = messages.Count;

			if ( entity.ParentId == 0 )
			{
				status.AddWarning( "Error: the parent identifier was not provided." );
			}

			if ( messages.Count > intialCount )
				return false;

			int count = 0;

			DBEntityItem efEntity = new DBEntityItem();

			using ( var context = new EntityContext() )
			{

				bool isEmpty = false;

				if ( ValidateProfile( entity, ref isEmpty, ref status ) == false )
				{
					return false;
				}
				if ( isEmpty )
				{
					status.AddWarning( "The Competency Profile is empty. " );
					return false;
				}

				if ( entity.Id == 0 )
				{
					//add
					efEntity = new DBEntityItem();
					MapToDB( entity, efEntity );

					efEntity.Created = efEntity.LastUpdated = DateTime.Now;
					efEntity.CreatedById = efEntity.LastUpdatedById = userId;
					efEntity.RowId = Guid.NewGuid();

					context.Entity_CompetencyFrameworkItem.Add( efEntity );

					count = context.SaveChanges();

					entity.Id = efEntity.Id;
					entity.RowId = efEntity.RowId;
					if ( count == 0 )
					{
						status.AddWarning( string.Format( " Unable to add Profile: {0} <br\\> ", string.IsNullOrWhiteSpace( entity.Name ) ? "no description" : entity.Description ) );
					}

				}
				else
				{

					efEntity = context.Entity_CompetencyFrameworkItem.SingleOrDefault( s => s.Id == entity.Id );
					if ( efEntity != null && efEntity.Id > 0 )
					{
						entity.RowId = efEntity.RowId;
						//update
						MapToDB( entity, efEntity );
						//has changed?
						if ( HasStateChanged( context ) )
						{
							efEntity.LastUpdated = System.DateTime.Now;
							efEntity.LastUpdatedById = userId;

							count = context.SaveChanges();
						}
					}
				}
			}

			return isValid;
		}

		/// <summary>
		/// Delete a competency
		/// </summary>
		/// <param name="recordId"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool Entity_Competency_Delete( int recordId, ref string statusMessage )
		{
			bool isOK = true;
			using ( var context = new EntityContext() )
			{
				DBEntityItem p = context.Entity_CompetencyFrameworkItem.FirstOrDefault( s => s.Id == recordId );
				if ( p != null && p.Id > 0 )
				{
					context.Entity_CompetencyFrameworkItem.Remove( p );
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


		public bool ValidateProfile( ThisEntityItem profile, ref bool isEmpty, ref SaveStatus status )
		{
			bool isValid = true;

			isEmpty = false;
			//check if empty
			if ( string.IsNullOrWhiteSpace( profile.Name )
				&& string.IsNullOrWhiteSpace( profile.Description )
				&& string.IsNullOrWhiteSpace( profile.TargetName )
				&& string.IsNullOrWhiteSpace( profile.TargetDescription )
				)
			{
				status.AddWarning( "Please enter, at minimum, a competency name." );
				//isEmpty = true;
				return false;
			}

			if ( string.IsNullOrWhiteSpace( profile.Name ) )
			{
				status.AddWarning( "A competency name must be entered" );
				isValid = false;
			}

			if ( !string.IsNullOrWhiteSpace( profile.AlignmentDate )
	&& !IsValidDate( profile.AlignmentDate ) )
			{
				status.AddWarning( "Please enter a valid alignment date" );
				isValid = false;
			}

			//if ( string.IsNullOrWhiteSpace( profile.Description ) )
			//{
			//	status.AddWarning( "A competency Description must be entered" );
			//	isValid = false;
			//}

			return isValid;
		}

		#endregion
		#region  retrieval ==================

		/// <summary>
		/// Get all records for the parent
		/// Uses the parent Guid to retrieve the related ThisEntityItem, then uses the EntityId to retrieve the child objects.
		/// </summary>
		/// <param name="parentUid"></param>
		/// <param name="alignmentType">If blank, get all types</param>
		public static List<ThisEntityItem> Entity_Competency_GetAll( int frameworkId )
		{
			ThisEntityItem entity = new ThisEntityItem();
			List<ThisEntityItem> list = new List<ThisEntityItem>();

			try
			{
				using ( var context = new EntityContext() )
				{
					List<DBEntityItem> results = context.Entity_CompetencyFrameworkItem
							.Where( s => s.EntityFrameworkId == frameworkId )
							.OrderBy( s => s.Id )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( DBEntityItem item in results )
						{
							entity = new ThisEntityItem();
							MapFromDB( item, entity, true );
							list.Add( entity );
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".Entity_Competency_GetAll" );
			}
			return list;
		}//

		/// <summary>
		/// Get a competency record
		/// </summary>
		/// <param name="profileId"></param>
		/// <returns></returns>
		public static ThisEntityItem Entity_Competency_Get( int profileId )
		{
			ThisEntityItem entity = new ThisEntityItem();
			if ( profileId == 0 )
				return entity;
			try
			{
				using ( var context = new EntityContext() )
				{
					DBEntityItem item = context.Entity_CompetencyFrameworkItem
							.SingleOrDefault( s => s.Id == profileId );

					if ( item != null && item.Id > 0 )
					{
						MapFromDB( item, entity, true );
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".Entity_Competency_Get" );
			}
			return entity;
		}//

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
					item.Name = GetRowColumn( dr, "Competency", "???" );
					item.ProfileName = GetRowPossibleColumn( dr, "Competency2", "???" );
					item.Description = GetRowColumn( dr, "Description", "" );

					//don't include credentialId, as will work with source of the search will often be for a credential./ Same for condition profiles for now. 
					item.SourceParentId = GetRowColumn( dr, "SourceId", 0 );
					item.SourceEntityTypeId = GetRowColumn( dr, "SourceEntityTypeId", 0 );
					item.AlignmentTypeId = GetRowColumn( dr, "AlignmentTypeId", 0 );
					item.AlignmentType = GetRowColumn( dr, "AlignmentType", "" );
					//Although the condition profile type may be significant?
					item.ConnectionTypeId = GetRowColumn( dr, "ConnectionTypeId", 0 );
					
					list.Add( item );
				}

				return list;

			}
		} //
		
		public static void MapToDB( ThisEntityItem from, DBEntityItem to )
		{
			//want to ensure fields from create are not wiped
			//to.Id = from.Id;
			to.EntityFrameworkId = from.ParentId;
			to.Name = StripJqueryTag( from.Name );
			to.Description = from.Description;
			to.TargetName = from.TargetName;
			to.TargetDescription = from.TargetDescription;
			to.TargetUrl = from.TargetUrl;
			to.CodedNotation = from.CodedNotation;

			if ( IsValidDate( from.AlignmentDate ) )
				to.AlignmentDate = DateTime.Parse( from.AlignmentDate );
			else
				to.AlignmentDate = null;

		}
		public static void MapFromDB( DBEntityItem from, ThisEntityItem to, bool includingItems = true )
		{
			to.Id = from.Id;
			to.ParentId = from.EntityFrameworkId;
			to.RowId = from.RowId;

			to.Name = StripJqueryTag( from.Name );
			to.Description = from.Description;
			to.TargetName = from.TargetName;
			to.TargetDescription = from.TargetDescription;
			to.TargetUrl = from.TargetUrl;
			to.CodedNotation = from.CodedNotation;

			if ( IsValidDate( from.AlignmentDate ) )
				to.AlignmentDate = ( ( DateTime ) from.AlignmentDate ).ToShortDateString();
			else
				to.AlignmentDate = "";

			if ( IsValidDate( from.Created ) )
				to.Created = ( DateTime ) from.Created;
			to.CreatedById = from.CreatedById == null ? 0 : ( int ) from.CreatedById;
			if ( IsValidDate( from.LastUpdated ) )
				to.LastUpdated = ( DateTime ) from.LastUpdated;
			to.LastUpdatedById = from.LastUpdatedById == null ? 0 : ( int ) from.LastUpdatedById;
			to.LastUpdatedBy = SetLastUpdatedBy( to.LastUpdatedById, from.Account_Modifier );
		}

		#endregion

		#endregion
	}
}

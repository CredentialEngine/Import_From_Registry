using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.ProfileModels;
using workIT.Utilities;

using ThisEntity = workIT.Models.SearchPendingReindex;
using DBEntity = workIT.Data.Tables.SearchPendingReindex;
using EntityContext = workIT.Data.Tables.workITEntities;

using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;

namespace workIT.Factories
{
	/// <summary>
	/// Instructions:
	/// - for a top level profile table -with base add, etc
	/// - create a new manager class, and then copy and paste this class into new class
	/// - change all Credential_ConnectionProfile to the entity frameworks entity name
	/// - change all ThisEntity to a custom profile class
	/// - change  to the proper class name
	/// - update the to and from map methods
	/// </summary>
	public class _TemplateManager : BaseFactory
	{
		static string thisClassName = "_TemplateManager";


        #region persistance ==================

        /// <summary>
        /// add a ThisEntity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="statusMessage"></param>
        /// <returns></returns>
        public int Add( ThisEntity entity, ref List<String> messages )
		{
			DBEntity efEntity = new DBEntity();
			entity.ParentId = credential.Id;
            using ( var context = new EntityContext() )
            {
				try
				{
                    if ( !IsValid( entity, ref messages ) )
                        return 0;
                    MapToDB( entity, efEntity );

					efEntity.CredentialId = credential.Id;
					if ( efEntity.RowId == null || efEntity.RowId.ToString() == DEFAULT_GUID )
						efEntity.RowId = Guid.NewGuid();
					efEntity.CreatedById = credential.LastUpdatedById;
					efEntity.Created = System.DateTime.Now;
					efEntity.LastUpdatedById = credential.LastUpdatedById;
					efEntity.LastUpdated = System.DateTime.Now;

					context.Credential_ConnectionProfile.Add( efEntity );

					// submit the change to database
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						entity.Id = efEntity.Id;

						//opMgr.ConditionProfile_UpdateParts( entity, true, ref statusMessage );

						return efEntity.Id;
					}
					else
					{
						//?no info on error
						messages.Add( "Error - the profile was not saved. " );
						string message = string.Format( "ConditionProfileManager. ConditionProfile_Add Failed", "Attempted to add a ThisEntity. The process appeared to not work, but was not an exception, so we have no message, or no clue.ThisEntity. CredentialId: {0}, createdById: {1}", entity.ParentId, entity.CreatedById );
						EmailManager.NotifyAdmin( thisClassName + ". ConditionProfile_Add Failed", message );
					}
				}
				catch ( System.Data.ThisEntity.Validation.DbEntityValidationException dbex )
				{
					//LoggingHelper.LogError( dbex, thisClassName + string.Format( ".ContentAdd() DbEntityValidationException, Type:{0}", entity.TypeId ) );
					string message = thisClassName + string.Format( ".ConditionProfile_Add() DbEntityValidationException, CredentialId: {0}", credential.Id );
					foreach ( var eve in dbex.EntityValidationErrors )
					{
						message += string.Format( "\rEntity of type \"{0}\" in state \"{1}\" has the following validation errors:",
							eve.Entry.ThisEntity.GetType().Name, eve.Entry.State );
						foreach ( var ve in eve.ValidationErrors )
						{
							message += string.Format( "- Property: \"{0}\", Error: \"{1}\"",
								ve.PropertyName, ve.ErrorMessage );
						}

						LoggingHelper.LogError( message, true );
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Add(), parentId: {0}", entity.ParentId ) );
				}
			}

			return efEntity.Id;
		}
		/// <summary>
		/// Update a ThisEntity
		/// - base only, caller will handle parts?
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool Update( ThisEntity entity, ref string statusMessage )
		{
			bool isValid = false;
			int count = 0;
			try
			{
				using ( var context = new EntityContext() )
				{
                    if ( !IsValid( entity, ref messages ) )
                        return 0;

                    DBEntity efEntity = context.Credential_ConnectionProfile
								.SingleOrDefault( s => s.Id == entity.Id );

					if ( efEntity != null && efEntity.Id > 0 )
					{
						//for updates, chances are some fields will not be in interface, don't map these (ie created stuff)
						MapToDB( entity, efEntity );
						if ( HasStateChanged( context ) )
						{
							efEntity.LastUpdated = System.DateTime.Now;
							efEntity.LastUpdatedById = entity.LastUpdatedById;
							count = context.SaveChanges();
							//can be zero if no data changed
							if ( count >= 0 )
							{
								isValid = true;
							}
							else
							{
								//?no info on error
								statusMessage = "Error - the update was not successful. ";
								string message = string.Format( thisClassName + ".ConditionProfile_Update Failed", "Attempted to update a ThisEntity. The process appeared to not work, but was not an exception, so we have no message, or no clue. CredentialId: {0}, Id: {1}, updatedById: {2}", entity.ParentId, entity.Id, entity.LastUpdatedById );
								EmailManager.NotifyAdmin( thisClassName + ". ConditionProfile_Update Failed", message );
							}
						}
						//continue with parts regardless
						//opMgr.ConditionProfile_UpdateParts( entity, false, ref statusMessage );
					}
					else
					{
						statusMessage = "Error - update failed, as record was not found.";
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + string.Format( ".Update. id: {0}", entity.Id ) );
			}


			return isValid;
		}

		public bool Delete( int Id, ref string statusMessage )
		{
			bool isValid = false;
			if ( Id == 0 )
			{
				statusMessage = "Error - missing an identifier for the ThisEntity";
				return false;
			}
			using ( var context = new EntityContext() )
			{
				DBEntity efEntity = context.Credential_ConnectionProfile
							.SingleOrDefault( s => s.Id == Id );

				if ( efEntity != null && efEntity.Id > 0 )
				{
					context.Credential_ConnectionProfile.Remove( efEntity );
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

		private bool IsValid( ThisEntity item, ref List<string> messages )
		{
			bool isValid = true;
			
			if ( string.IsNullOrWhiteSpace( item.ProfileName ) )
                messages.Add( "Error: missing profile name" );

            if ( messages.Count > 0 )
                isValid = false;
            return isValid;
		}
		#endregion

		#region == Retrieval =======================

		public static ThisEntity Get( int id, bool includeProperties = false )
		{
			ThisEntity entity = new ThisEntity();
			using ( var context = new EntityContext() )
			{

				DBEntity item = context.Credential_ConnectionProfile
						.SingleOrDefault( s => s.Id == id );

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity, true );
					if ( includeProperties )
					{
						//TBD
					}
				}
			}

			return entity;
		}

		public static void MapToDB( ThisEntity fromEntity, DBEntity to )
		{

			//want to ensure fields from create are not wiped
			if ( to.Id < 1 )
			{
				if ( IsValidDate( fromEntity.Created ) )
					to.Created = fromEntity.Created;
				to.CreatedById = fromEntity.CreatedById;
			}

			to.Id = fromEntity.Id;
			//to.Name = fromEntity.Name;
			to.Description = fromEntity.Description;
			to.CredentialId = fromEntity.ParentId;

			
			

			if ( IsValidDate( fromEntity.LastUpdated ) )
				to.LastUpdated = fromEntity.LastUpdated;
			to.LastUpdatedById = fromEntity.LastUpdatedById;
		}
		public static void MapFromDB( DBEntity fromEntity, ThisEntity to, bool includingProperties = false )
		{
			to.Id = fromEntity.Id;
			to.RowId = fromEntity.RowId;
			to.ParentId = fromEntity.CredentialId;
			to.ProfileName = fromEntity.Name;
			to.Description = fromEntity.Description;
			
			//....

			if ( IsValidDate( fromEntity.Created ) )
				to.Created = ( DateTime ) fromEntity.Created;
			to.CreatedById = fromEntity.CreatedById == null ? 0 : ( int ) fromEntity.CreatedById;
			if ( IsValidDate( fromEntity.LastUpdated ) )
				to.LastUpdated = ( DateTime ) fromEntity.LastUpdated;
			to.LastUpdatedById = fromEntity.LastUpdatedById == null ? 0 : ( int ) fromEntity.LastUpdatedById;

		}

		#endregion


	}
}

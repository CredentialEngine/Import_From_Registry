﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;
using ThisEntity = workIT.Models.ProfileModels.RevocationProfile;
using DBEntity = workIT.Data.Tables.Entity_RevocationProfile;
using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;

using workIT.Utilities;
using CM = workIT.Models.Common;

using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;
namespace workIT.Factories
{
	public class Entity_RevocationProfileManager : BaseFactory
	{
		static string thisClassName = "Entity_RevocationProfileManager";
		#region Entity Persistance ===================

		public bool SaveList( List<ThisEntity> list, Credential credential, ref SaveStatus status )
		{

            if ( credential == null || credential.Id < 1 )
            {
                status.AddError( "Error: the credential identifier was not provided." );
                return false;
            }

            DBEntity efEntity = new DBEntity();
            Entity parent = EntityManager.GetEntity( credential.RowId );
            if ( parent == null || parent.Id == 0 )
            {
                status.AddError( "Error - the parent entity was not found." );
                return false;
            }
            DeleteAll( parent, ref status );

            if ( list == null || list.Count == 0 )
				return true;

			status.HasSectionErrors = false;
			foreach ( ThisEntity item in list )
			{
				Save( item, parent, ref status );
			}

			return status.WasSectionValid;
		} //

		/// <summary>
		/// Persist Revocation Profiles
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="parent"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
		private bool Save( ThisEntity entity, Entity parent, ref SaveStatus status )
		{
			bool isValid = true;

			DBEntity efEntity = new DBEntity();
			//Entity parent = EntityManager.GetEntity( credential.RowId );
			if ( parent == null || parent.Id == 0 )
			{
				status.AddError( "Error - the parent entity was not found." );
				return false;
			}
			using ( var context = new EntityContext() )
			{

				bool isEmpty = false;
				int profNbr = 0;

				profNbr++;
				if ( ValidateProfile( entity, ref isEmpty, ref  status ) == false )
				{
					return false;
				}
				if ( isEmpty ) //skip
				{
					status.AddWarning( "Revocation Profile was empty. " );
					return false;
				}

				if ( entity.Id == 0 )
				{
					//add
					efEntity = new DBEntity();
					MapToDB( entity, efEntity );
					efEntity.EntityId = parent.Id;

					efEntity.Created = efEntity.LastUpdated = DateTime.Now;
                    if ( IsValidGuid( entity.RowId ) )
                        efEntity.RowId = entity.RowId;
                    else
                        efEntity.RowId = Guid.NewGuid();

                    context.Entity_RevocationProfile.Add( efEntity );
					int count = context.SaveChanges();
					//update profile record so doesn't get deleted
					entity.Id = efEntity.Id;
					entity.RelatedEntityId = parent.Id;
					entity.RowId = efEntity.RowId;
					if ( count == 0 )
					{
						status.AddError( string.Format( " Unable to add Profile: {0} ", string.IsNullOrWhiteSpace( entity.ProfileName ) ? "no description" : entity.ProfileName ) );
					}
					UpdateParts( entity, ref status );
				}
				else
				{
					entity.RelatedEntityId = parent.Id;

					efEntity = context.Entity_RevocationProfile.SingleOrDefault( s => s.Id == entity.Id );
					if ( efEntity != null && efEntity.Id > 0 )
					{
						entity.RowId = efEntity.RowId;
						//update
						MapToDB( entity, efEntity );
						//has changed?
						if ( HasStateChanged( context ) )
						{
							efEntity.LastUpdated = System.DateTime.Now;
							

							int count = context.SaveChanges();
						}
						UpdateParts( entity, ref status );
					}
				}
			}

			return isValid;
		}
		private bool UpdateParts( ThisEntity entity, ref SaveStatus status )
		{
			bool isAllValid = true;
			Entity relatedEntity = EntityManager.GetEntity( entity.RowId );
			if ( relatedEntity == null || relatedEntity.Id == 0 )
			{
				status.AddError( "Error - the related Entity was not found." );
				return false;
			}

			//
			//JurisdictionProfile 
			Entity_JurisdictionProfileManager jpm = new Entity_JurisdictionProfileManager();
			//do deletes 
			jpm.DeleteAll( relatedEntity, ref status );
			jpm.SaveList( entity.Jurisdiction, entity.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_SCOPE, ref status );
			//
			return isAllValid;
		}
		public bool DeleteAll( Entity parent, ref SaveStatus status )
        {
            bool isValid = true;
            //Entity parent = EntityManager.GetEntity( parentUid );
            if ( parent == null || parent.Id == 0 )
            {
                status.AddError( thisClassName + ". Error - the provided target parent entity was not provided." );
                return false;
            }
			try
			{
				using ( var context = new EntityContext() )
				{
					var results = context.Entity_RevocationProfile.Where( s => s.EntityId == parent.Id )
					.ToList();
					if ( results == null || results.Count == 0 )
						return true;
					//
					foreach ( var item in results )
					{
						//21-03-31 mp - just removing the profile will not remove its entity and the latter,s children!
						string statusMessage = string.Empty;
						new EntityManager().Delete( item.RowId, string.Format( "RevocationProfile: {0} for EntityType: {1} ({2})", item.Id, parent.EntityTypeId, parent.EntityBaseId ), ref statusMessage );

						context.Entity_RevocationProfile.Remove( item );
						var count = context.SaveChanges();
						if ( count > 0 )
						{

						}
					}
					//context.Entity_RevocationProfile.RemoveRange( context.Entity_RevocationProfile.Where( s => s.EntityId == parent.Id ) );
					//int count = context.SaveChanges();
					//if ( count > 0 )
					//{
					//	isValid = true;
					//}
					//else
					//{
					//	//if doing a delete on spec, may not have been any properties
					//}
				}
			}
			catch ( Exception ex )
			{
				var msg = BaseFactory.FormatExceptions( ex );
				LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DeleteAll. ParentType: {0}, baseId: {1}, exception: {2}", parent.EntityType, parent.EntityBaseId, msg ) );
			}

            return isValid;
        }
        public bool ValidateProfile( ThisEntity profile, ref bool isEmpty, ref SaveStatus status )
		{
			status.HasSectionErrors = false;

			//check if empty
			if ( string.IsNullOrWhiteSpace( profile.ProfileName) 
				&& string.IsNullOrWhiteSpace( profile.Description )
				&& string.IsNullOrWhiteSpace( profile.RevocationCriteriaUrl )
				&& string.IsNullOrWhiteSpace( profile.RevocationCriteriaDescription )
				&& string.IsNullOrWhiteSpace( profile.DateEffective )
				&& ( profile.Jurisdiction == null || profile.Jurisdiction.Count == 0 )
				)
			{
				isEmpty = true;
				return false;
			}

			//date check, can this be in the future?
			if ( !string.IsNullOrWhiteSpace( profile.DateEffective )
				&& !IsValidDate( profile.DateEffective ) )
			{
				status.AddWarning( "Please enter a valid effective date" );
			}
			if ( !IsUrlValid( profile.RevocationCriteriaUrl, ref commonStatusMessage ) )
			{
				status.AddWarning( "The 'Revocation Criteria Url' format is invalid. " + commonStatusMessage );
			}

			return status.WasSectionValid;
		}

		#endregion
		#region  retrieval ==================

		/// <summary>
		/// Get all Revocation Profiles for the parent
		/// Uses the parent Guid to retrieve the related Entity, then uses the EntityId to retrieve the child objects.
		/// </summary>
		/// <param name="parentUid"></param>
		public static List<ThisEntity> GetAll( Guid parentUid )
		{
			ThisEntity entity = new ThisEntity();
			List<ThisEntity> list = new List<ThisEntity>();
			Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				return list;
			}

			try
			{
				using ( var context = new EntityContext() )
				{
					List<DBEntity> results = context.Entity_RevocationProfile
							.Where( s => s.EntityId == parent.Id )
							.OrderBy( s => s.Id )
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

		public static ThisEntity Get( int profileId )
		{
			ThisEntity entity = new ThisEntity();

			try
			{
				using ( var context = new EntityContext() )
				{
					DBEntity item = context.Entity_RevocationProfile
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

		public static void MapToDB( ThisEntity from, DBEntity to )
		{
			//want to ensure fields from create are not wiped

			to.Id = from.Id;
			to.ProfileName = from.ProfileName;
			to.Description = from.Description;
			to.RevocationCriteriaUrl = from.RevocationCriteriaUrl;
			to.RevocationCriteriaDescription = from.RevocationCriteriaDescription;
			
			if ( IsValidDate( from.DateEffective ) )
				to.DateEffective = DateTime.Parse( from.DateEffective );
			else
				to.DateEffective = null;
			
		}
		public static void MapFromDB( DBEntity from, ThisEntity to, bool includingItems = true )
		{
			to.Id = from.Id;
			to.RowId = from.RowId;
			to.RelatedEntityId = from.EntityId;

			to.Description = from.Description;

			to.RevocationCriteriaDescription = from.RevocationCriteriaDescription;
			to.RevocationCriteriaUrl = from.RevocationCriteriaUrl;

			if ( IsValidDate( from.DateEffective ) )
				to.DateEffective = ( ( DateTime ) from.DateEffective ).ToString("yyyy-MM-dd");
			else
				to.DateEffective = string.Empty;

			to.RevocationCriteriaUrl = from.RevocationCriteriaUrl;
			if ( ( from.Entity.EntityBaseName ?? string.Empty ).Length > 3 )
				to.ParentSummary = from.Entity.EntityBaseName;
			//not used:
			to.ProfileSummary = SetEntitySummary( to );
			//no longer using name, but need for the editor list
			to.ProfileName = to.ProfileSummary;
			

			if ( IsValidDate( from.Created ) )
				to.Created = ( DateTime ) from.Created;
			if ( IsValidDate( from.LastUpdated ) )
				to.LastUpdated = ( DateTime ) from.LastUpdated;

			if ( includingItems )
			{
				to.CredentialProfiled = Entity_CredentialManager.GetAll( to.RowId, BaseFactory.RELATIONSHIP_TYPE_HAS_PART );
				//
				to.Jurisdiction = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( to.RowId );
				to.Region = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( to.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_RESIDENT );
			}
		}
		static string SetEntitySummary( ThisEntity to )
		{
			string summary = "Revocation Profile ";

			if ( !string.IsNullOrWhiteSpace( to.ParentSummary ) )
			{
				summary += " for " + to.ParentSummary;
			}

			return summary;

		}
		#endregion

	}
}

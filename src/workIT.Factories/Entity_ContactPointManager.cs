using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.ProfileModels;
using EM = workIT.Data;
using workIT.Utilities;

using Views = workIT.Data.Views;

using ThisEntity = workIT.Models.Common.ContactPoint;
using DBEntity = workIT.Data.Tables.Entity_ContactPoint;
using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Tables.workITEntities;

namespace workIT.Factories
{
	public class Entity_ContactPointManager : BaseFactory
	{
		static string thisClassName = "Entity_ContactPointManager";
		#region Entity Persistance ===================
		public bool SaveList( List<ThisEntity> list, Guid parentUid, ref SaveStatus status, bool doingDelete = true )
		{
            if ( !IsValidGuid( parentUid ) )
            {
                status.AddWarning( "Error: the parent identifier was not provided." );
                return false;
            }

            Entity parent = EntityManager.GetEntity( parentUid );
            if ( parent == null || parent.Id == 0 )
            {
                status.AddWarning( "Error - the parent entity was not found." );
                return false;
            }
            if ( doingDelete )
                DeleteAll( parent, ref status );

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
		/// Persist ContactPoint
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="parentUid"></param>
		/// <param name="userId"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public bool Save( ThisEntity entity, Guid parentUid, ref SaveStatus status )
		{
			bool isValid = true;
            int count = 0;
            DBEntity efEntity = new DBEntity();

            if ( !IsValidGuid( parentUid ) )
			{
				status.AddWarning( "Error: the parent identifier was not provided." );
				return false;
			}

			Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				status.AddWarning( "Error - the parent entity was not found." );
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
					efEntity.ParentEntityId = parent.Id;

					efEntity.Created = efEntity.LastUpdated = DateTime.Now;
					efEntity.RowId = Guid.NewGuid();

					context.Entity_ContactPoint.Add( efEntity );
					count = context.SaveChanges();
					//update profile record so doesn't get deleted
					entity.Id = efEntity.Id;
					entity.ParentId = parent.Id;
					entity.RowId = efEntity.RowId;
					if ( count == 0 )
					{
						status.AddWarning( string.Format( " Unable to add Contact Point: {0} <br\\> ", string.IsNullOrWhiteSpace( entity.ProfileName ) ? "no description" : entity.ProfileName ) );
					}
					else
					{
						UpdateParts( entity, ref status );
					}
				}
				else
				{
					entity.ParentId = parent.Id;

					efEntity = context.Entity_ContactPoint.SingleOrDefault( s => s.Id == entity.Id );
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
						//always check parts
						UpdateParts( entity, ref status );
					}
				}
			}

			return isValid;
		}

		private bool UpdateParts( ThisEntity entity,  ref SaveStatus status )
		{
			bool isAllValid = true;

			//EntityPropertyManager mgr = new EntityPropertyManager();
			Entity_ReferenceManager erm = new Entity_ReferenceManager();

			if ( erm.AddTextValue( entity.SocialMediaPages, entity.RowId, 
				ref status, CodesManager.PROPERTY_CATEGORY_ORGANIZATION_SOCIAL_MEDIA ) == false )
				isAllValid = false;


			if ( erm.AddTextValue( entity.Emails, entity.RowId, ref status, CodesManager.PROPERTY_CATEGORY_EMAIL_TYPE ) == false )
				isAllValid = false;

			if ( erm.AddTextValue( entity.PhoneNumbers, entity.RowId, 
				ref status, CodesManager.PROPERTY_CATEGORY_PHONE_TYPE ) == false )
				isAllValid = false;

			return isAllValid;
		}

		public bool Delete( int recordId, ref string statusMessage )
		{
			bool isOK = true;
			using ( var context = new EntityContext() )
			{
				DBEntity p = context.Entity_ContactPoint.FirstOrDefault( s => s.Id == recordId );
				if ( p != null && p.Id > 0 )
				{
					context.Entity_ContactPoint.Remove( p );
					int count = context.SaveChanges();
				}
				else
				{
					statusMessage = string.Format( "Contact Point record was not found: {0}", recordId );
					isOK = false;
				}
			}
			return isOK;

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
            using ( var context = new EntityContext() )
            {
                context.Entity_ContactPoint.RemoveRange( context.Entity_ContactPoint.Where( s => s.ParentEntityId == parent.Id ) );
                int count = context.SaveChanges();
                if ( count > 0 )
                {
                    isValid = true;
                }
                else
                {
                    //if doing a delete on spec, may not have been any properties
                }
            }

            return isValid;
        }
        public bool ValidateProfile( ThisEntity profile, ref SaveStatus status )
		{
			status.HasSectionErrors = false;

			if ( string.IsNullOrWhiteSpace( profile.ProfileName ) )
			{
				//status.AddWarning( "A contact point name must be entered" );
			}
			//not sure if should be required?
			if ( string.IsNullOrWhiteSpace( profile.ContactType ) )
			{
				//status.AddWarning( "A contact point type must be entered" );
			}
			if (profile.SocialMediaPages.Count > 0)
			{
				//probable will not initially validate
			}
		

			//make this a method
			//IsPhoneValid( profile.Telephone, "phone", ref status );
			//IsPhoneValid( profile.FaxNumber, "fax", ref status );

			//string phoneNbr = PhoneNumber.StripPhone( GetData( profile.Telephone ) );

			//if ( !string.IsNullOrWhiteSpace( phoneNbr ) && phoneNbr.Length < 10 )
			//{
			//	status.AddWarning( string.Format( "Error - A phone number ({0}) must have at least 10 numbers.", profile.Telephone ) );
			//}
			//phoneNbr = PhoneNumber.StripPhone( GetData( profile.FaxNumber ) );

			//if ( !string.IsNullOrWhiteSpace( phoneNbr ) && phoneNbr.Length < 10 )
			//{
			//	status.AddWarning( string.Format( "Error - A Fax number ({0}) must have at least 10 numbers.", profile.FaxNumber ) );
			//}

			//needs to be one of email, phone, fax, or list
			//will be this or a check of the lists
			bool hasContent = false;
			//if ( !string.IsNullOrWhiteSpace( profile.Email )
			//	|| !string.IsNullOrWhiteSpace( profile.FaxNumber )
			//	|| !string.IsNullOrWhiteSpace( profile.Telephone )
			//	|| !string.IsNullOrWhiteSpace( profile.SocialMedia )
			//	)
			//{
			//	hasContent = true;
			//}

			if ( profile.PhoneNumbers.Count > 0 ||
				profile.Emails.Count > 0 ||
				profile.SocialMediaPages.Count > 0 )
			{
				hasContent = true;
			}
			if ( !hasContent )
				status.AddWarning( "A contact point must have at least one phone, email, or URL" );

			
			return !status.HasSectionErrors;
		}

		#endregion
		#region  retrieval ==================

		/// <summary>
		/// Get all ContactPoint for the parent
		/// Uses the parent Guid to retrieve the related Entity, then uses the ParentEntityId to retrieve the child objects.
		/// </summary>
		/// <param name="parentUid"></param>
		public static List<ThisEntity> GetAll( Guid parentUid )
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
					List<DBEntity> results = context.Entity_ContactPoint
							.Where( s => s.ParentEntityId == parent.Id )
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
					DBEntity item = context.Entity_ContactPoint
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
			if ( to.Id == 0 )
			{
				if ( IsValidDate( from.Created ) )
					to.Created = from.Created;
			}
			to.Id = from.Id;
			to.Name = from.ProfileName;

			to.ContactType = from.ContactType;
			
			//to.ContactOption = from.ContactOption;

			//to.Email = from.Email;
			//to.Telephone = from.Telephone;
			//to.Fax = from.FaxNumber;
			//to.SocialMedia = from.SocialMedia;


		}
		public static void MapFromDB( DBEntity from, ThisEntity to, bool includingItems = true )
		{
			to.Id = from.Id;
			to.RowId = from.RowId;
			to.ParentId = from.ParentEntityId;
			if ( from.Entity != null )
				to.ParentRowId = from.Entity.EntityUid;
			to.ProfileName = from.Name;

			to.ContactType = from.ContactType;
            //to.ContactOption = from.ContactOption;

            string summary = "Contact Point ";
            if ( string.IsNullOrWhiteSpace( to.ProfileName ) )
            {
                if ( !string.IsNullOrWhiteSpace( to.ContactType ) )
                {
                    to.ProfileName = to.ContactType;
                    to.ContactType = "";
                }
            }
             
            if ( string.IsNullOrWhiteSpace( to.ProfileName ) )
            {
                if ( string.IsNullOrWhiteSpace( to.ProfileName ) && from.Entity != null )
                {
                    to.ProfileName = from.Entity.EntityBaseName ?? "Contact Point";
                }
                else
                    to.ProfileName = summary;
            }

			if ( includingItems )
			{
				to.SocialMedia = Entity_ReferenceManager.GetAll( to.RowId, CodesManager.PROPERTY_CATEGORY_ORGANIZATION_SOCIAL_MEDIA );
				to.PhoneNumber = Entity_ReferenceManager.GetAll( to.RowId, CodesManager.PROPERTY_CATEGORY_PHONE_TYPE );
				to.Email = Entity_ReferenceManager.GetAll( to.RowId, CodesManager.PROPERTY_CATEGORY_EMAIL_TYPE );
			}


			if ( IsValidDate( from.Created ) )
				to.Created = ( DateTime ) from.Created;
			
			if ( IsValidDate( from.LastUpdated ) )
				to.LastUpdated = ( DateTime ) from.LastUpdated;
			
		}
		static string SetEntitySummary( ThisEntity to )
		{
			string summary = "Contact Point ";
			if ( !string.IsNullOrWhiteSpace( to.ProfileName ) )
			{
				return to.ProfileName;
			}
			else if ( !string.IsNullOrWhiteSpace( to.ContactType ) )
			{
				return to.ContactType;
			}
			//else if ( !string.IsNullOrWhiteSpace( to.ContactOption ) )
			//{
			//	return to.ContactOption;
			//}
			//else if ( !string.IsNullOrWhiteSpace( to.ContactOption ) )
			//{
			//	return to.ContactOption;
			//}
			//else if ( !string.IsNullOrWhiteSpace( to.Telephone ) )
			//{
			//	return "Telephone: " + to.Telephone;
			//}
			//else if ( !string.IsNullOrWhiteSpace( to.FaxNumber ) )
			//{
			//	return "Fax: " + to.FaxNumber;
			//}
			//else if ( !string.IsNullOrWhiteSpace( to.Email ) )
			//{
			//	return "Email: " + to.Email;
			//}
			//if ( to.Id > 1 )
			//{
			//	summary += to.Id.ToString();
			//}
			return summary;

		}
		#endregion

	}
}

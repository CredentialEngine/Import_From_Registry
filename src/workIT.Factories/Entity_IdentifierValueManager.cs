using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;
using ThisEntity = workIT.Models.ProfileModels.Entity_IdentifierValue;
using DBEntity = workIT.Data.Tables.Entity_IdentifierValue;
using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;

using workIT.Utilities;
using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;

namespace workIT.Factories
{
	public class Entity_IdentifierValueManager : BaseFactory
	{
		static string thisClassName = "Entity_IdentifierValueManager";
		public static int CREDENTIAL_VersionIdentifier = 1;
		public static int ORGANIZATION_AlternativeIdentifier = 2; //OBSOLETE
		public static int ASSESSMENT_VersionIdentifier = 3;
		public static int LEARNING_OPP_VersionIdentifier = 4;

		public static int CREDENTIAL_Identifier = 11;
		public static int ORGANIZATION_Identifier = 12;
		public static int ASSESSMENT_Identifier = 13;
		public static int LEARNING_OPP_Identifier = 14;

		#region === Persistance ===================
		public bool SaveList( List<ThisEntity> list, Guid parentUid, int IdentityValueTypeId, ref SaveStatus status, bool doingDelete )
		{
            if ( !IsValidGuid( parentUid ) )
            {
                status.AddError( string.Format( "A valid parent identifier was not provided to the {0}.Add method.", thisClassName ) );
                return false;
            }


            Entity parent = EntityManager.GetEntity( parentUid );
            if ( parent == null || parent.Id == 0 )
            {
                status.AddError( "Error - the parent entity was not found." );
                return false;
            }
			if ( doingDelete )
				DeleteAll( parent, ref status );

            if ( list == null || list.Count == 0 )
				return true;

			bool isAllValid = true;
			foreach ( ThisEntity item in list )
			{
				item.IdentityValueTypeId = IdentityValueTypeId;
				Add( parent, item, ref status );
			}

			return isAllValid;
		}

		/// <summary>
		/// Add an Entity_IdentifierValueManager
		/// </summary>
		/// <param name="parentUid"></param>
		/// <param name="entity"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
		private int Add( Entity parent,
					ThisEntity entity,
					ref SaveStatus status )
		{
			int id = 0;
			int count = 0;

			//Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				status.AddError( "Error - the parent entity was not found." );
				return 0;
			}
			using ( var context = new EntityContext() )
			{
				DBEntity efEntity = new DBEntity();
				try
				{

					efEntity = new DBEntity();

					MapToDB( entity, efEntity );
					efEntity.EntityId = parent.Id;
					efEntity.Created = System.DateTime.Now;

					context.Entity_IdentifierValue.Add( efEntity );

					// submit the change to database
					count = context.SaveChanges();
					if ( count > 0 )
					{
						id = efEntity.Id;
						return efEntity.Id;
					}
					else
					{
						//?no info on error
						status.AddError( "Error - the add was not successful." );
						string message = thisClassName + string.Format( ".Add Failed", "Attempted to add a Entity_IdentifierValue for a profile. The process appeared to not work, but there was no exception, so we have no message, or no clue. Parent Profile: {0}, Type: {1}, learningOppId: {2}, createdById: {3}", parent.EntityUid, parent.EntityType, entity.IdentifierType );
						EmailManager.NotifyAdmin( thisClassName + ".Add Failed", message );
					}
				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", "Entity_Entity_IdentifierValue" );
					status.AddError( "Error - the save was not successful. " + message );
					LoggingHelper.LogError( dbex, thisClassName + string.Format( ".Save(), Parent: {0} ({1})", parent.EntityBaseName, parent.EntityBaseId ) );

				}
				catch ( Exception ex )
				{
					string message = FormatExceptions( ex );
					status.AddError( "Error - the save was not successful. " + message );
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Save(), Parent: {0} ({1})", parent.EntityBaseName, parent.EntityBaseId ) );
				}


			}
			return id;
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
                context.Entity_IdentifierValue.RemoveRange( context.Entity_IdentifierValue.Where( s => s.EntityId == parent.Id ) );
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
        #endregion


        /// <summary>
        /// Get all assessments for the provided entity
        /// The returned entities are just the base
        /// </summary>
        /// <param name="parentUid"></param>
        /// <returns></returnsThisEntity
        public static List<ThisEntity> GetAll( Guid parentUid, int identityValueTypeId )
		{
			List<ThisEntity> list = new List<ThisEntity>();
			ThisEntity entity = new ThisEntity();

			Entity parent = EntityManager.GetEntity( parentUid );
			LoggingHelper.DoTrace( 7, string.Format( thisClassName + ".GetAll: parentUid:{0} entityId:{1}, e.EntityTypeId:{2}", parentUid, parent.Id, parent.EntityTypeId ) );

			try
			{
				using ( var context = new EntityContext() )
				{
					List<DBEntity> results = context.Entity_IdentifierValue
							.Where( s => s.EntityId == parent.Id && s.IdentityValueTypeId == identityValueTypeId )
							.OrderBy( s => s.Name )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( DBEntity item in results )
						{
							entity = new ThisEntity();
							MapFromDB( item, entity );
							list.Add( entity );
						}
					}
					return list;
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetAll" );
			}
			return list;
		}
		public static ThisEntity Get( int parentId, int recordId )
		{
			ThisEntity entity = new ThisEntity();
			if ( parentId < 1 || recordId < 1 )
			{
				return entity;
			}
			try
			{
				using ( var context = new EntityContext() )
				{
					DBEntity from = context.Entity_IdentifierValue
							.SingleOrDefault( s => s.Id == recordId && s.EntityId == parentId );

					if ( from != null && from.Id > 0 )
					{
						MapFromDB( from, entity );
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".Get" );
			}
			return entity;
		}//

		public static void MapFromDB( DBEntity from, ThisEntity to )
		{
			to.Id = from.Id;
			to.EntityId = from.EntityId;
			to.IdentityValueTypeId = from.IdentityValueTypeId;
			to.Name = from.Name;
			//to.Description = from.Description;
			to.IdentifierType = from.IdentifierType;
			to.IdentifierValueCode = from.IdentifierValueCode;
			to.Created = ( DateTime ) from.Created;
		}
		public static void MapToDB( ThisEntity from, DBEntity to )
		{
			if ( from.Id == 0)
			{
				to.Id = from.Id;
				to.EntityId = from.EntityId;
			}
			
			to.IdentityValueTypeId = from.IdentityValueTypeId;
			to.Name = from.Name;
			//to.Description = from.Description;
			to.IdentifierType = from.IdentifierType;
			to.IdentifierValueCode = from.IdentifierValueCode;
			
		}
	}
}

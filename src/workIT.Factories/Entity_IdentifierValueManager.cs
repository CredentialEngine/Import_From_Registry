﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;
using ThisResource = workIT.Models.ProfileModels.Entity_IdentifierValue;
using DBResource = workIT.Data.Tables.Entity_IdentifierValue;
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
		//21-10-21 mp - no need to have unique identifier values by entity, as will be under a specific entity type
		public static int IdentifierValue_VersionIdentifier = 1;
		public static int IdentifierValue_Identifier = 2;
		//
		//public static int CREDENTIAL_VersionIdentifier = 1;
		//public static int ORGANIZATION_AlternativeIdentifier = 2; //OBSOLETE
		//public static int ASSESSMENT_VersionIdentifier = 1;//3;
		//public static int LEARNING_OPP_VersionIdentifier = 1;//4;

		//public static int CREDENTIAL_Identifier = 2;//11;
		//public static int ORGANIZATION_Identifier = 2;//12;
		//public static int ASSESSMENT_Identifier = 2;//13;
		//public static int LEARNING_OPP_Identifier = 2;//14;
		//NOTE: for pathwayComponent, Idenifier is stored in the JsonProperties
		#region === Persistance ===================
		public bool SaveList( List<ThisResource> list, Guid parentUid, int IdentityValueTypeId, ref SaveStatus status, bool doingDelete )
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
			//delete is dependent on caller context. Should only do a delete here if no other identifiers
			if ( doingDelete )
				DeleteAll( parent, ref status );

            if ( list == null || list.Count == 0 )
				return true;

			bool isAllValid = true;
			foreach ( ThisResource item in list )
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
		private int Add( Entity parent, ThisResource entity, ref SaveStatus status )
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
				DBResource efEntity = new DBResource();
				try
				{
					efEntity = new DBResource();

					MapToDB( entity, efEntity );
					efEntity.EntityId = parent.Id;
					if ( entity.Created == DateTime.MinValue )
						efEntity.Created = System.DateTime.Now;
					else 
						efEntity.Created = entity.Created;

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
						status.AddError( thisClassName + "Error - the add was not successful." );
						string message = thisClassName + string.Format( ".Add Failed", "Attempted to add a Entity_IdentifierValue for a profile. The process appeared to not work, but there was no exception, so we have no message, or no clue. parent.EntityUid,: {0}, Type: {1}, IdentifierType: {2}", parent.EntityUid, parent.EntityType, entity.IdentifierType );
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
			try
			{
				using ( var context = new EntityContext() )
				{
					var results = context.Entity_IdentifierValue.Where( s => s.EntityId == parent.Id )
						.ToList();
					if ( results == null || results.Count == 0 )
						return true;

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
			}
			catch ( Exception ex )
			{
				var msg = BaseFactory.FormatExceptions( ex );
				LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DeleteAll. ParentType: {0}, baseId: {1}, exception: {2}", parent.EntityType, parent.EntityBaseId, msg ) );
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
        public static List<ThisResource> GetAll( Guid parentUid, int identityValueTypeId )
		{
			List<ThisResource> list = new List<ThisResource>();
			ThisResource entity = new ThisResource();

			Entity parent = EntityManager.GetEntity( parentUid );
			LoggingHelper.DoTrace( 7, string.Format( thisClassName + ".GetAll: parentUid:{0} entityId:{1}, e.EntityTypeId:{2}", parentUid, parent.Id, parent.EntityTypeId ) );

			try
			{
				using ( var context = new EntityContext() )
				{
					List<DBResource> results = context.Entity_IdentifierValue
							.Where( s => s.EntityId == parent.Id && s.IdentityValueTypeId == identityValueTypeId )
								.OrderBy( s => s.IdentifierType ) //nulls?
								.ThenBy( s => s.Created )    //may want created order
								.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( DBResource item in results )
						{
							entity = new ThisResource();
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
		public static ThisResource Get( int parentId, int recordId )
		{
			ThisResource entity = new ThisResource();
			if ( parentId < 1 || recordId < 1 )
			{
				return entity;
			}
			try
			{
				using ( var context = new EntityContext() )
				{
					DBResource from = context.Entity_IdentifierValue
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

		public static void MapFromDB( DBResource from, ThisResource to )
		{
			to.Id = from.Id;
			to.EntityId = from.EntityId;
			to.IdentityValueTypeId = from.IdentityValueTypeId;
			to.IdentifierTypeName = from.Name;
			to.IdentifierType = from.IdentifierType;
			to.IdentifierValueCode = from.IdentifierValueCode;
			to.Created = ( DateTime ) from.Created;
		}
		public static void MapToDB( ThisResource from, DBResource to )
		{
			if ( from.Id == 0)
			{
				to.Id = from.Id;
				to.EntityId = from.EntityId;
			}
			
			to.IdentityValueTypeId = from.IdentityValueTypeId;
			to.Name = from.IdentifierTypeName;
			to.IdentifierType = from.IdentifierType;
			to.IdentifierValueCode = from.IdentifierValueCode;
			
		}
	}
}

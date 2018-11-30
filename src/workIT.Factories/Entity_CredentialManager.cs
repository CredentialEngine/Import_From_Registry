﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;
using ThisEntity = workIT.Models.ProfileModels.Entity_Credential;
using DBEntity = workIT.Data.Tables.Entity_Credential;
using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;

using workIT.Utilities;
using CM = workIT.Models.Common;

using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;
namespace workIT.Factories
{
	public class Entity_CredentialManager : BaseFactory
	{
		static string thisClassName = "Entity_CredentialManager";

		#region Entity Persistance ===================
		public bool SaveList( List<int> list, Guid parentUid, ref SaveStatus status )
		{
			if ( list == null || list.Count == 0 )
				return true;
			int newId = 0;

			bool isAllValid = true;
			foreach ( int item in list )
			{
				Add( parentUid, item, ref newId, ref status );
				if ( newId == 0 )
					isAllValid = false;
			}

			return isAllValid;
		}

		/// <summary>
		/// For isPartOf, the list is the entity where the credential is a part of. 
		/// Get the entity, 
		/// </summary>
		/// <param name="parentList">List of credential Ids for the parent where is part of</param>
		/// <param name="parentUid"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public bool SaveIsPartOfList( List<int> parentList, int credentialId, ref SaveStatus status )
		{
			if ( parentList == null || parentList.Count == 0 )
				return true;
			int newId = 0;
			bool isAllValid = true;

			foreach ( int parentCredentialId in parentList )
			{
				Entity partOfEntity = EntityManager.GetEntity( 1, parentCredentialId );
				if ( partOfEntity == null || partOfEntity.Id == 0 )
				{
					status.AddError( string.Format( thisClassName + ".SaveIsPartOfList(). Error - the related part Of (parent) credential was not found. parentCredentialId: {0}, credentialId: {1}", parentCredentialId, credentialId ) );
					continue;
				}

				Add( partOfEntity, credentialId,  ref newId, ref status );
				if ( newId == 0 )
					isAllValid = false;
			}

			return isAllValid;
		}

		/// <summary>
		/// Persist Entity Credential
		/// </summary>
		/// <param name="credentialId"></param>
		/// <param name="parentUid"></param>
		/// <param name="newId">Return record id of the new record</param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public bool Add( Guid parentUid, int credentialId,
			ref int newId,
			ref SaveStatus status )
		{

			if ( !IsValidGuid( parentUid ) )
			{
				status.AddError( "Error: the parent identifier was not provided." );
			}

			if ( credentialId < 1 )
			{
				status.AddError( "Error: a valid credential was not provided." );
				return false;
			}

			Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				//status.AddError( "Error - the parent entity was not found." );
				return false;
			}

			return Add( parent, credentialId, ref newId, ref status );
		}

        private bool Add( Entity parent, int credentialId,
                ref int newId, ref SaveStatus status )
        {
            bool isValid = true;
            int count = 0;
            newId = 0;


            if ( parent == null || parent.Id == 0 )
            {
                status.AddWarning( string.Format( thisClassName + ".Add() Error: a valid parent entity was not provided for credential: {0}.", credentialId ) );
                return false;
            }

            if ( credentialId < 1 )
            {
                status.AddWarning( thisClassName + ".Add() Error: a valid credential was not provided." );
                return false;
            }


            DBEntity efEntity = new DBEntity();
            try
            {
                using ( var context = new EntityContext() )
                {
                    //first check for duplicates
                    efEntity = context.Entity_Credential
                            .FirstOrDefault( s => s.EntityId == parent.Id && s.CredentialId == credentialId );
                    if ( efEntity != null && efEntity.Id > 0 )
                    {
                        newId = efEntity.Id;
                        //just let it go - expected for an update
                        LoggingHelper.DoTrace( 6, string.Format( thisClassName + ".Add() the credential is already part of this profile. credential: {0} to parent entityId: :{1}  ", credentialId, parent.Id ) );
                        return true;
                    }

                    //add
                    efEntity = new DBEntity();
                    efEntity.CredentialId = credentialId;
                    efEntity.EntityId = parent.Id;
                    efEntity.Created = DateTime.Now;

                    context.Entity_Credential.Add( efEntity );
                    count = context.SaveChanges();

                    newId = efEntity.Id;

                    if ( count == 0 )
                    {
                        status.AddError( string.Format( thisClassName + ".Add() Unable to add the related credential: {0} to parent entityId: {1} ", credentialId, parent.Id ) );
                        isValid = false;
                    }
                }
            }
            catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
            {
                string message = HandleDBValidationError( dbex, thisClassName + ".Add()", string.Format( "CredentialId: {0} to parent entityId: {1} ", credentialId, parent.Id ) );
                status.AddError( message );
            }
            catch ( Exception ex )
            {
                string message = thisClassName + string.Format( ".Add(), CredentialId: {0} to parent entityId: {1} ", credentialId, parent.Id );
                LoggingHelper.LogError( ex, message );
                status.AddError( message );
            }
            return isValid;
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
                context.Entity_Credential.RemoveRange( context.Entity_Credential.Where( s => s.EntityId == parent.Id ) );
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

        #region  retrieval ==================

        /// <summary>
        /// get all the base credentials for an EntityCredential
        /// Uses the parent Guid to retrieve the related Entity, then uses the EntityId to retrieve the child objects.
        /// </summary>
        /// <param name="parentUid"></param>
        /// <returns></returns>
        public static List<Credential> GetAll( Guid parentUid, bool isForDetailPageCondition = false )
		{
			ThisEntity entity = new ThisEntity();
			List<Credential> list = new List<Credential>();
			Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				return list;
			}
			try
			{
				using ( var context = new EntityContext() )
				{
					//commented out in order to get more data for detail page
					//context.Configuration.LazyLoadingEnabled = false;

					List<DBEntity> results = context.Entity_Credential
							.Where( s => s.EntityId == parent.Id)
							.OrderBy( s => s.Id )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( DBEntity item in results )
						{
							entity = new ThisEntity();
							if ( item.Credential != null 
                                && item.Credential.Id > 0 
                                && item.Credential.EntityStateId > 1 )
							{
								MapFromDB( item, entity, isForDetailPageCondition );

								list.Add( entity.Credential );
							}
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

		public static void MapToDB( ThisEntity from, DBEntity to )
		{
			//want to ensure fields from create are not wiped
			if ( to.Id == 0 )
			{
				if ( IsValidDate( from.Created ) )
					to.Created = from.Created;
			}
			to.Id = from.Id;
			to.CredentialId = from.CredentialId;
			to.EntityId = from.ParentId;
			
		}
		public static void MapFromDB( DBEntity from, ThisEntity to, bool isForDetailPageCondition = false )
		{
			to.Id = from.Id;
			to.CredentialId = from.CredentialId;
			to.ParentId = from.EntityId;
            
			
			//to.Credential = from.Credential;
			to.Credential = new Credential();
			if ( from.Credential != null && from.Credential.Id > 0 )
			{
				to.ProfileSummary = from.Credential.Name;
				CredentialMinimumMap( from.Credential, to.Credential );
			}
			else
			{
				to.Credential = CredentialManager.GetBasic( to.CredentialId );
				if ( to.Credential != null && to.Credential.Id > 0 )
				{
					to.ProfileSummary = to.Credential.Name;
					//CredentialMinimumMap( from.Credential, to.Credential );
				}
				else
				{
					to.ProfileSummary = string.Format( "Credential ({0}) has not been downloaded", to.CredentialId );
					LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".MapFromDB() Credential ({0}) has not been downloaded. ParentEntityId: {1}, Entity.CredentialId: {2}", to.CredentialId, to.ParentId, to.Id ) );
				}
			}

			if ( IsValidDate( from.Created ) )
				to.Created = ( DateTime ) from.Created;
		}

		public static void CredentialMinimumMap( EM.Credential from, Credential to )
		{
			CredentialRequest cr = new CredentialRequest();
			//probably too much
			cr.IsDetailRequest();

			to.Id = from.Id;
			to.RowId = from.RowId;
			
			to.Name = from.Name;
			to.Description = from.Description;

			to.SubjectWebpage = from.SubjectWebpage;
			to.CTID = from.CTID;
            to.EntityStateId = (int)from.EntityStateId;
			// 16-06-15 mp - always include credential type
			//can be null for a pending record
			to.CredentialTypeId = ( int ) ( from.CredentialTypeId ?? 0 );
			if ( to.CredentialTypeId  > 0)
			{
				CodeItem ct = CodesManager.Codes_PropertyValue_Get( to.CredentialTypeId );
				if ( ct != null && ct.Id > 0 )
				{
					to.CredentialType = ct.Title;
					to.CredentialTypeSchema = ct.SchemaName;
				}

                to.CredentialTypeEnum = EntityPropertyManager.FillEnumeration( to.RowId, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_TYPE );
                to.CredentialTypeEnum.Items.Add( new EnumeratedItem() { Id = to.CredentialTypeId, Name = ct.Name, SchemaName = ct.SchemaName } );
            }

			if ( from.ImageUrl != null && from.ImageUrl.Trim().Length > 0 )
				to.ImageUrl = from.ImageUrl;
			else
				to.ImageUrl = null;

			if ( IsValidDate( from.Created ) )
				to.Created = ( DateTime ) from.Created;

			to.AudienceLevelType = EntityPropertyManager.FillEnumeration( to.RowId, CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL );

			//to.Occupation = Entity_FrameworkItemManager.FillEnumeration( to.RowId, CodesManager.PROPERTY_CATEGORY_SOC );
            to.Occupation = Reference_FrameworksManager.FillEnumeration( to.RowId, CodesManager.PROPERTY_CATEGORY_SOC );
            to.OtherOccupations = Entity_ReferenceManager.GetAll( to.RowId, CodesManager.PROPERTY_CATEGORY_SOC );

            //to.Industry = Entity_FrameworkItemManager.FillEnumeration( to.RowId, CodesManager.PROPERTY_CATEGORY_NAICS );
            to.Industry = Reference_FrameworksManager.FillEnumeration( to.RowId, CodesManager.PROPERTY_CATEGORY_NAICS );
            to.OtherIndustries = Entity_ReferenceManager.GetAll( to.RowId, CodesManager.PROPERTY_CATEGORY_NAICS );

			to.Subject = Entity_ReferenceManager.GetAllSubjects( to.RowId );

			to.Keyword = Entity_ReferenceManager.GetAll( to.RowId, CodesManager.PROPERTY_CATEGORY_KEYWORD );

			//Added these because they were needed on the detail page - NA 6/1/2017
			to.OwningAgentUid = from.OwningAgentUid ?? Guid.Empty;
			to.OwningOrganization = OrganizationManager.GetForSummary( to.OwningAgentUid );

		}


		#endregion

	}
}

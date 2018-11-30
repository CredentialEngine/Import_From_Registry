using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.ProfileModels;
using workIT.Utilities;

using ThisEntity = workIT.Models.Common.FinancialAlignmentObject;
using DBEntity = workIT.Data.Tables.Entity_FinancialAlignmentProfile;
using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;

using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;

namespace workIT.Factories
{
	public class Entity_FinancialAlignmentProfileManager : BaseFactory
	{
		static string thisClassName = "Entity_FinancialAlignmentProfileManager";


		#region === -Persistance ==================
		public bool SaveList( List<ThisEntity> list, Guid parentUid, ref SaveStatus status )
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
            DeleteAll( parent, ref status );

            if ( list == null || list.Count == 0 )
				return true;

			bool isAllValid = true;
			foreach ( ThisEntity item in list )
			{
				Save( item, parent, ref status );
			}

			return isAllValid;
		}

		/// <summary>
		/// Persist FinancialAlignmentProfile
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="parentUid"></param>
		/// <param name="userId"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public bool Save( ThisEntity entity, Entity parent, ref SaveStatus status )
		{
			bool isValid = true;
			int count = 0;

			DBEntity efEntity = new DBEntity();

			//Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				status.AddError( "Error - the parent entity was not found." );
				return false;
			}
			using ( var context = new EntityContext() )
			{
				try
				{

					if ( ValidateProfile( entity, ref status ) == false )
					{
						return false;
					}

					if ( entity.Id == 0 )
					{
						//add
						efEntity = new DBEntity();
						FromMap( entity, efEntity );
						efEntity.EntityId = parent.Id;
						efEntity.Created = efEntity.LastUpdated = DateTime.Now;
                        if ( IsValidGuid( entity.RowId ) )
                            efEntity.RowId = entity.RowId;
                        else
                            efEntity.RowId = Guid.NewGuid();

                        context.Entity_FinancialAlignmentProfile.Add( efEntity );
						count = context.SaveChanges();

						entity.Id = efEntity.Id;
						entity.RowId = efEntity.RowId;
						if ( count == 0 )
						{
							status.AddError( thisClassName + " - Unable to add Financial Alignment Profile" );
						}
						
					}
					else
					{

						efEntity = context.Entity_FinancialAlignmentProfile.SingleOrDefault( s => s.Id == entity.Id );
						if ( efEntity != null && efEntity.Id > 0 )
						{
							entity.RowId = efEntity.RowId;
							//update
							FromMap( entity, efEntity );
							//has changed?
							if ( HasStateChanged( context ) )
							{
								efEntity.LastUpdated = System.DateTime.Now;
								count = context.SaveChanges();
							}
					
						}
					}
				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{

					string message = HandleDBValidationError( dbex, thisClassName + ".Save() ", "FinancialAlignmentProfile" );
					status.AddWarning( thisClassName +  " - Error - the save was not successful. " + message );
					LoggingHelper.LogError( dbex, thisClassName + string.Format( ".Save(), Parent: {0} ({1})", parent.EntityBaseName, parent.EntityBaseId ) );
					isValid = false;
				}
				catch ( Exception ex )
				{
					string message = FormatExceptions( ex );
					status.AddError( thisClassName + " - Error - the save was not successful. " + message );
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Save(), Parent: {0} ({1}), UserId: {2}", parent.EntityBaseName, parent.EntityBaseId ) );
					isValid = false;
				}

			}

			return isValid;
		}


		/// <summary>
		/// Delete a Financial Alignment profile, and related Entity
		/// </summary>
		/// <param name="Id"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool Delete( int Id, ref string statusMessage )
		{
			bool isValid = false;
			if ( Id == 0 )
			{
				statusMessage = "Error - missing an identifier for the FinancialAlignmentProfile";
				return false;
			}
			using ( var context = new EntityContext() )
			{
				try
				{
					DBEntity efEntity = context.Entity_FinancialAlignmentProfile
								.SingleOrDefault( s => s.Id == Id );

					if ( efEntity != null && efEntity.Id > 0 )
					{
						Guid rowId = efEntity.RowId;

						context.Entity_FinancialAlignmentProfile.Remove( efEntity );
						int count = context.SaveChanges();
						if ( count > 0 )
						{
							isValid = true;
							//do with trigger now
							//new EntityManager().Delete( rowId, ref statusMessage );
						}
					}
					else
					{
						statusMessage = "Error - delete failed, as record was not found.";
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + ".Delete()" );

					if ( ex.InnerException != null && ex.InnerException.Message != null )
					{
						statusMessage = ex.InnerException.Message;

						if ( ex.InnerException.InnerException != null && ex.InnerException.InnerException.Message != null )
							statusMessage = ex.InnerException.InnerException.Message;
					}
					if ( statusMessage.ToLower().IndexOf( "the delete statement conflicted with the reference constraint" ) > -1 )
					{
						statusMessage = "Error: this Financial Alignment cannot be deleted as it is being referenced by other items, such as roles or credentials. These associations must be removed before this Financial Alignment can be deleted.";
					}
				}
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
                context.Entity_FinancialAlignmentProfile.RemoveRange( context.Entity_FinancialAlignmentProfile.Where( s => s.EntityId == parent.Id ) );
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

			//
			if ( string.IsNullOrWhiteSpace( profile.FrameworkName ) )
			{
				status.AddWarning( thisClassName + " - A Framework name must be entered" );
			}

			if ( !IsUrlValid( profile.Framework, ref commonStatusMessage ) )
			{
				status.AddWarning( thisClassName + " - The Framework Url is invalid " + commonStatusMessage );
			}

			if ( !IsUrlValid( profile.TargetNode, ref commonStatusMessage ) )
			{
				status.AddWarning( thisClassName + " - The TargetNode Url is invalid " + commonStatusMessage );
			}
			if ( !string.IsNullOrWhiteSpace( profile.AlignmentDate ) 
			  && IsValidDate(profile.AlignmentDate) ==  false)
				status.AddWarning( thisClassName + " - The Alignment Date is invalid " );

			return !status.HasSectionErrors;
		}

		#endregion

		#region == Retrieval =======================
		public static ThisEntity Get( int id,
			bool forEditView = false )
		{
			ThisEntity entity = new ThisEntity();
			bool includingProfiles = false;
			if ( forEditView )
				includingProfiles = true;

			using ( var context = new EntityContext() )
			{

				DBEntity item = context.Entity_FinancialAlignmentProfile
						.SingleOrDefault( s => s.Id == id );

				if ( item != null && item.Id > 0 )
				{
					ToMap( item, entity,
						true, //includingProperties
						includingProfiles);
				}
			}

			return entity;
		}



		 /// <summary>
		 /// Get all the Financial Alignments for the parent entity (ex a credential)
		 /// </summary>
		 /// <param name="parentUid"></param>
		 /// <returns></returns>
		public static List<ThisEntity> GetAll( Guid parentUid )
		{
			ThisEntity to = new ThisEntity();
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
					//context.Configuration.LazyLoadingEnabled = false;

					List<EM.Entity_FinancialAlignmentProfile> results = context.Entity_FinancialAlignmentProfile
							.Where( s => s.EntityId == parent.Id )
							.OrderBy( s => s.FrameworkName)
							.ThenBy( s => s.Created )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( EM.Entity_FinancialAlignmentProfile from in results )
						{
							to = new ThisEntity();
							ToMap( from, to, true, true);
							list.Add( to );
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetAll (Guid parentUid)" );
			}
			return list;
		}//
		 

		public static void FromMap( ThisEntity from, DBEntity to )
		{

			//want to ensure fields from create are not wiped
			if ( to.Id == 0 )
			{
				
			}


			to.Id = from.Id;
			to.Framework = GetData( from.Framework );
			to.FrameworkName = GetData( from.FrameworkName );
			to.TargetNodeName = GetData( from.TargetNodeName );
			to.TargetNode = GetData( from.TargetNode );
			to.TargetNodeDescription = GetData( from.TargetNodeDescription );
			to.CodedNotation = from.CodedNotation;
			to.AlignmentDate = SetDate(from.AlignmentDate);
			//to.AlignmentType = from.AlignmentType;
			//if ( from.AlignmentTypeId > 0 )
			//	to.AlignmentTypeId = from.AlignmentTypeId;
			//else
				to.AlignmentTypeId = null;

			to.Weight = SetData( from.Weight, 0.01M );

		}
		public static void ToMap( DBEntity from, ThisEntity to,
				bool includingProperties = false,
				bool includingProfiles = true )
		{
			to.Id = from.Id;
			to.RowId = from.RowId;
			to.ParentId = from.EntityId;
			to.Framework = GetData( from.Framework );
			to.FrameworkName = GetData( from.FrameworkName );
			to.ProfileName = to.FrameworkName;
			to.TargetNodeName = GetData( from.TargetNodeName );
			to.TargetNode = GetData( from.TargetNode );
			to.TargetNodeDescription = GetData( from.TargetNodeDescription );
			to.CodedNotation = from.CodedNotation;
			
			if ( IsValidDate( from.AlignmentDate ) )
				to.AlignmentDate = ( ( DateTime ) from.AlignmentDate ).ToShortDateString();
			else
				to.AlignmentDate = "";

			to.AlignmentTypeId = from.AlignmentTypeId == null ? 0 : (int) from.AlignmentTypeId;
			//if ( to.AlignmentTypeId > 0 && from.Codes_PropertyValue != null)
			//{
			//	to.AlignmentType = from.Codes_PropertyValue.Title;
			//}

			to.Weight = from.Weight != null ? (decimal) from.Weight : 0;

			if ( IsValidDate( from.Created ) )
				to.Created = ( DateTime ) from.Created;
			if ( IsValidDate( from.LastUpdated ) )
				to.LastUpdated = ( DateTime ) from.LastUpdated;
		}

		#endregion
	}
}

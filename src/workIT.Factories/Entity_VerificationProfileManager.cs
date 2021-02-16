using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;
using ThisEntity = workIT.Models.ProfileModels.VerificationServiceProfile;
using DBEntity = workIT.Data.Tables.Entity_VerificationProfile;
using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;

using workIT.Utilities;
using CM = workIT.Models.Common;

using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;
namespace workIT.Factories
{
	public class Entity_VerificationProfileManager : BaseFactory
	{
		static string thisClassName = "Entity_VerificationProfileManager";
		#region Entity Persistance ===================
		public bool SaveList( List<ThisEntity> list, Guid parentUid, ref SaveStatus status )
		{
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
		 /// Persist VerificationProfile
		 /// </summary>
		 /// <param name="entity"></param>
		 /// <param name="parent"></param>
		 /// <param name="userId"></param>
		 /// <param name="messages"></param>
		 /// <returns></returns>
		public bool Save( ThisEntity entity, Entity parent, ref SaveStatus status )
		{
			bool isValid = true;
			int count = 0;
			DBEntity efEntity = new DBEntity();
			using ( var context = new EntityContext() )
			{
				try
				{
					bool isEmpty = false;

					if ( ValidateProfile( entity, ref isEmpty, ref status ) == false )
					{
						return false;
					}
					if ( isEmpty )
					{
						status.AddWarning( "The Verification Profile is empty. " + SetEntitySummary( entity ) );
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

                        context.Entity_VerificationProfile.Add( efEntity );
						count = context.SaveChanges();
						//update profile record so doesn't get deleted
						entity.Id = efEntity.Id;
						entity.ParentId = parent.Id;
						entity.RowId = efEntity.RowId;
						if ( count == 0 )
						{
							status.AddError( " Unable to add Verification Service Profile"  );
						}
						else
						{
							//other entity components use a trigger to create the entity Object. If a trigger is not created, then child adds will fail (as typically use entity_summary to get the parent. As the latter is easy, make the direct call?
							//string statusMessage = "";
							//int entityId = new EntityManager().Add( efEntity.RowId, entity.Id, 	CodesManager.ENTITY_TYPE_VERIFICATION_PROFILE, ref  statusMessage );

							UpdateParts( entity, ref status );
						}
					}
					else
					{
						entity.ParentId = parent.Id;

						efEntity = context.Entity_VerificationProfile.SingleOrDefault( s => s.Id == entity.Id );
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
				catch ( Exception ex )
				{
					string message = FormatExceptions( ex);
					status.AddError( "Error - the save was not successful. " + message );
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Save(), Parent: {0} ({1})", parent.EntityBaseName, parent.EntityBaseId) );
				}

			}

			return isValid;
		}

		private bool UpdateParts( ThisEntity entity, ref SaveStatus status )
		{
			bool isAllValid = true;

			EntityPropertyManager mgr = new EntityPropertyManager();

			if ( mgr.AddProperties( entity.ClaimType, entity.RowId, CodesManager.ENTITY_TYPE_PROCESS_PROFILE, CodesManager.PROPERTY_CATEGORY_CLAIM_TYPE, false, ref status ) == false )
				isAllValid = false;

			//CostProfile
			CostProfileManager cpm = new Factories.CostProfileManager();
			cpm.SaveList( entity.EstimatedCost, entity.RowId, ref status );

			int newId = 0;
			if ( entity.TargetCredentialIds != null && entity.TargetCredentialIds.Count > 0 )
			{
				Entity_CredentialManager ecm = new Entity_CredentialManager();
				foreach ( int id in entity.TargetCredentialIds )
				{
					ecm.Add( entity.RowId, id, BaseFactory.RELATIONSHIP_TYPE_HAS_PART, ref newId, ref status );
				}
			}

			return isAllValid;
		}

		public bool Delete( int recordId, ref string statusMessage )
		{
			bool isOK = true;
			using ( var context = new EntityContext() )
			{
				DBEntity p = context.Entity_VerificationProfile.FirstOrDefault( s => s.Id == recordId );
				if ( p != null && p.Id > 0 )
				{
					context.Entity_VerificationProfile.Remove( p );
					int count = context.SaveChanges();
				}
				else
				{
					statusMessage = string.Format( "Verification Profile record was not found: {0}", recordId );
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
			try
			{
				using ( var context = new EntityContext() )
				{
					var results = context.Entity_VerificationProfile.Where( s => s.EntityId == parent.Id )
					.ToList();
					if ( results == null || results.Count == 0 )
						return true;

					context.Entity_VerificationProfile.RemoveRange( context.Entity_VerificationProfile.Where( s => s.EntityId == parent.Id ) );
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

        public bool ValidateProfile( ThisEntity profile, ref bool isEmpty, ref SaveStatus status )
		{
			status.HasSectionErrors = false;

			isEmpty = false;

			if ( string.IsNullOrWhiteSpace( profile.Description ) )
			{
				status.AddWarning( "A profile description must be entered" );
			}
			
			if ( !IsUrlValid( profile.SubjectWebpage, ref commonStatusMessage ) )
			{
				status.AddWarning( "The Subject Webpage Url is invalid. " + commonStatusMessage );
			}
			if ( !IsUrlValid( profile.VerificationServiceUrl, ref commonStatusMessage ) )
			{
				status.AddWarning( "The Verification Service Url is invalid. " + commonStatusMessage );
			}
			if ( !IsUrlValid( profile.VerificationDirectory, ref commonStatusMessage ) )
			{
				status.AddWarning( "The Verification Directory Url is invalid. " + commonStatusMessage );
			}

			return status.WasSectionValid;
		}

		#endregion
		#region  retrieval ==================

		/// <summary>
		/// Get all VerificationProfile for the parent
		/// Uses the parent Guid to retrieve the related Entity, then uses the EntityId to retrieve the child objects.
		/// </summary>
		/// <param name="parentUid"></param>
		public static List<ThisEntity> GetAll( Guid parentUid, bool includingItems = true )
		{
			ThisEntity entity = new ThisEntity();
			List<ThisEntity> list = new List<ThisEntity>();
			Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				return list;
			}
			//bool includingItems = true;
			try
			{
				using ( var context = new EntityContext() )
				{
					List<DBEntity> results = context.Entity_VerificationProfile
							.Where( s => s.EntityId == parent.Id )
							.OrderBy( s => s.Id )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( DBEntity item in results )
						{
							entity = new ThisEntity();
							MapFromDB( item, entity, includingItems );


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
					DBEntity item = context.Entity_VerificationProfile
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

			}
			to.Id = from.Id;
			to.Description = from.Description;
			to.HolderMustAuthorize = from.HolderMustAuthorize;

			to.SubjectWebpage = GetUrlData( from.SubjectWebpage );

			to.VerificationService = from.VerificationServiceUrl;
			to.VerificationDirectory = from.VerificationDirectory;
			to.VerificationMethodDescription = from.VerificationMethodDescription;

			if ( IsValidDate( from.DateEffective ) )
				to.DateEffective = DateTime.Parse( from.DateEffective );
			else
				to.DateEffective = null;
			
			if ( IsGuidValid( from.OfferedByAgentUid ) )
			{
				to.OfferedByAgentUid = from.OfferedByAgentUid;
			}
			else
			{
				to.OfferedByAgentUid = null;
			}

		}
		public static void MapFromDB( DBEntity from, ThisEntity to, 
				bool includingItems
			)
		{
            //TODO - add option for get during import to get less data
			to.Id = from.Id;
			to.RowId = from.RowId;

			to.Description = ( from.Description ?? "" );
			//ProfileName is for display purposes
			to.ProfileName = to.Description.Length < 80 ? to.Description : to.Description.Substring(0, 79) + " ...";

			if ( from.HolderMustAuthorize != null )
				to.HolderMustAuthorize = ( bool ) from.HolderMustAuthorize;
			
			if ( IsValidDate( from.DateEffective ) )
				to.DateEffective = ( ( DateTime ) from.DateEffective ).ToShortDateString();
			else
				to.DateEffective = "";

			to.SubjectWebpage = from.SubjectWebpage;
			to.VerificationServiceUrl = from.VerificationService;
			to.VerificationDirectory = from.VerificationDirectory;
			to.VerificationMethodDescription = from.VerificationMethodDescription;
			
			if ( IsGuidValid( from.OfferedByAgentUid ) )
			{
				to.OfferedByAgentUid = ( Guid ) from.OfferedByAgentUid;
				to.OfferedByAgent = OrganizationManager.GetBasics( to.OfferedByAgentUid );
			}

			if ( includingItems )
			{
				//TODO 170803- need to chg to a list
				//only get if:
				//edit - get profile list
				//detail - get basic
				bool isForDetailPageCredential = true;
				
				to.TargetCredential = Entity_CredentialManager.GetAll( to.RowId, BaseFactory.RELATIONSHIP_TYPE_HAS_PART, isForDetailPageCredential );

				to.EstimatedCost = CostProfileManager.GetAll( to.RowId );

				to.ClaimType = EntityPropertyManager.FillEnumeration( to.RowId, CodesManager.PROPERTY_CATEGORY_CLAIM_TYPE );

				to.Jurisdiction = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( to.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_SCOPE );

				to.Region = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( to.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_RESIDENT );
				to.JurisdictionAssertions = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( to.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_OFFERREDIN );

				//to.VerificationStatus = Entity_VerificationStatusManager.GetAll( to.Id );
			}


			if ( IsValidDate( from.Created ) )
				to.Created = ( DateTime ) from.Created;
			if ( IsValidDate( from.LastUpdated ) )
				to.LastUpdated = ( DateTime ) from.LastUpdated;
		}
		static string SetEntitySummary( ThisEntity to )
		{
			string summary = "Verification Profile ";
			if ( !string.IsNullOrWhiteSpace( to.Description ) )
			{
				return to.Description.Length < 80 ? to.Description : to.Description.Substring( 0, 79 ) + " ...";
			}

			if ( to.Id > 1 )
			{
				summary += to.Id.ToString();
			}
			return summary;

		}
		#endregion

	}
}

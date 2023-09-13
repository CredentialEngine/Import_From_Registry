using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;
using ThisResource = workIT.Models.ProfileModels.VerificationServiceProfile;
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
		public bool SaveList( List<ThisResource> list, Guid parentUid, ref SaveStatus status )
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
			foreach ( ThisResource item in list )
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
		private bool Save( ThisResource entity, Entity parent, ref SaveStatus status )
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
						entity.RelatedEntityId = parent.Id;
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
						entity.RelatedEntityId = parent.Id;

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
					status.AddError( $"Error - the save for Entity.VerificationService for Parent: {parent.EntityBaseName} ({parent.EntityBaseId}) was not successful. " + message );
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Save(), Parent: {0} ({1})", parent.EntityBaseName, parent.EntityBaseId) );
				}

			}

			return isValid;
		}

		private bool UpdateParts( ThisResource entity, ref SaveStatus status )
		{
			bool isAllValid = true;
			Entity relatedEntity = EntityManager.GetEntity( entity.RowId );
			if ( relatedEntity == null || relatedEntity.Id == 0 )
			{
				status.AddError( "Error - the related Entity was not found." );
				return false;
			}
			EntityPropertyManager mgr = new EntityPropertyManager();
			//first clear all properties
			mgr.DeleteAll( relatedEntity, ref status );
			//
			if ( mgr.AddProperties( entity.VerifiedClaimType, entity.RowId, CodesManager.ENTITY_TYPE_PROCESS_PROFILE, CodesManager.PROPERTY_CATEGORY_CLAIM_TYPE, false, ref status ) == false )
				isAllValid = false;

			//CostProfile
			CostProfileManager cpm = new Factories.CostProfileManager();
			cpm.SaveList( entity.EstimatedCost, entity.RowId, ref status );

			int newId = 0;
			Entity_CredentialManager ecm = new Entity_CredentialManager();
			ecm.DeleteAll( relatedEntity, ref status );
			if ( entity.TargetCredentialIds != null && entity.TargetCredentialIds.Count > 0 )
			{
				foreach ( int id in entity.TargetCredentialIds )
				{
					ecm.Add( entity.RowId, id, BaseFactory.RELATIONSHIP_TYPE_HAS_PART, ref newId, ref status );
				}
			}
			//
			//JurisdictionProfile 
			Entity_JurisdictionProfileManager jpm = new Entity_JurisdictionProfileManager();
			//do deletes - NOTE: other jurisdictions are added in: UpdateAssertedIns
			jpm.DeleteAll( relatedEntity, ref status );
			jpm.SaveList( entity.Jurisdiction, entity.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_SCOPE, ref status );
			jpm.SaveAssertedInList( entity.RowId, Entity_AgentRelationshipManager.ROLE_TYPE_OFFERED_BY, entity.OfferedIn, ref status );
			
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
					//
					foreach ( var item in results )
					{
						//21-03-31 mp - just removing the profile will not remove its entity and the latter's children!
						string statusMessage = "";
						new EntityManager().Delete( item.RowId, string.Format( "VerificationProfile: {0} for EntityType: {1} ({2})", item.Id, parent.EntityTypeId, parent.EntityBaseId ), ref statusMessage );

						context.Entity_VerificationProfile.Remove( item );
						var count = context.SaveChanges();
						if ( count > 0 )
						{

						}
					}
					//context.Entity_VerificationProfile.RemoveRange( context.Entity_VerificationProfile.Where( s => s.EntityId == parent.Id ) );
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

        public bool ValidateProfile( ThisResource profile, ref bool isEmpty, ref SaveStatus status )
		{
			status.HasSectionErrors = false;

			isEmpty = false;

			if ( string.IsNullOrWhiteSpace( profile.Description ) )
			{
				status.AddWarning( "A verification service profile description must be entered." );
			}
			
			if ( !IsUrlValid( profile.SubjectWebpage, ref commonStatusMessage ) )
			{
				status.AddWarning( "The verification service profile Subject Webpage Url is invalid. " + commonStatusMessage );
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
		public static List<ThisResource> GetAll( Guid parentUid, bool includingItems = true )
		{
			ThisResource entity = new ThisResource();
			List<ThisResource> list = new List<ThisResource>();
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
							entity = new ThisResource();
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

		public static int GetAllTotal( Guid parentUid )
		{

			Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				return 0;
			}
			//bool includingItems = true;
			try
			{
				using ( var context = new EntityContext() )
				{
					context.Configuration.LazyLoadingEnabled = false;

					List<DBEntity> results = context.Entity_VerificationProfile
							.Where( s => s.EntityId == parent.Id )
							.OrderBy( s => s.Id )
							.ToList();
					if ( results != null && results.Any() )
						return results.Count;
					
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetAllTotal" );
			}
			return 0;
		}//

		//public static ThisResource Get( int profileId )
		//{
		//	ThisResource entity = new ThisResource();

		//	try
		//	{
		//		using ( var context = new EntityContext() )
		//		{
		//			DBEntity item = context.Entity_VerificationProfile
		//					.SingleOrDefault( s => s.Id == profileId );

		//			if ( item != null && item.Id > 0 )
		//			{
		//				MapFromDB( item, entity, true );
		//			}
		//		}
		//	}
		//	catch ( Exception ex )
		//	{
		//		LoggingHelper.LogError( ex, thisClassName + ".Get" );
		//	}
		//	return entity;
		//}//

        

        public static void MapToDB( ThisResource input, DBEntity output )
		{
			//want output ensure fields from create are not wiped
			if ( output.Id == 0 )
			{

			}
			output.Id = input.Id;
			output.Description = input.Description;
            if ( IsValidDate( input.DateEffective ) )
                output.DateEffective = DateTime.Parse( input.DateEffective );
            else
                output.DateEffective = null;
            output.HolderMustAuthorize = input.HolderMustAuthorize;

			output.SubjectWebpage = GetUrlData( input.SubjectWebpage );

			output.VerificationService = GetListAsDelimitedString(input.VerificationService, "|");
			output.VerificationDirectory = GetListAsDelimitedString( input.VerificationDirectory, "|");
			output.VerificationMethodDescription = input.VerificationMethodDescription;
			
			if ( IsGuidValid( input.OfferedByAgentUid ) )
			{
				output.OfferedByAgentUid = input.OfferedByAgentUid;
			}
			else
			{
				output.OfferedByAgentUid = null;
			}

		}
		public static void MapFromDB( DBEntity input, ThisResource output, 
				bool includingItems
			)
		{
            //TODO - add option for get during import output get less data
			output.Id = input.Id;
			output.RowId = input.RowId;

			output.Description = ( input.Description ?? "" );
            if ( IsValidDate( input.DateEffective ) )
                output.DateEffective = (( DateTime) input.DateEffective ).ToString("yyyy-MM-dd");
            else
                output.DateEffective = null;
            if ( input.HolderMustAuthorize != null )
				output.HolderMustAuthorize = ( bool ) input.HolderMustAuthorize;
			


			output.SubjectWebpage = input.SubjectWebpage;
            //output.VerificationDirectoryOLD = input.VerificationDirectory;
            //output.VerificationServiceOLD = input.VerificationService;
            if ( !string.IsNullOrWhiteSpace( input.VerificationDirectory ) )
            {
                output.VerificationDirectory = SplitDelimitedStringToList( input.VerificationDirectory, '|' );
            }
            output.VerificationMethodDescription = input.VerificationMethodDescription;
            if ( !string.IsNullOrWhiteSpace( input.VerificationService ) )
            {
                output.VerificationService = SplitDelimitedStringToList( input.VerificationDirectory, '|' );
            }
            output.VerificationMethodDescription = input.VerificationMethodDescription;
			
			if ( IsGuidValid( input.OfferedByAgentUid ) )
			{
				output.OfferedByAgentUid = ( Guid ) input.OfferedByAgentUid;
				output.OfferedByAgent = OrganizationManager.GetBasics( output.OfferedByAgentUid );
			}

			if ( includingItems )
			{
				//TODO 170803- need output chg output a list
				//only get if:
				//edit - get profile list
				//detail - get basic
				bool isForDetailPageCredential = true;
				//make sure this is minimum data
				output.TargetCredential = Entity_CredentialManager.GetAll( output.RowId, BaseFactory.RELATIONSHIP_TYPE_HAS_PART, isForDetailPageCredential );

				output.EstimatedCost = CostProfileManager.GetAll( output.RowId );

				output.VerifiedClaimType = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_CLAIM_TYPE );

				output.Jurisdiction = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( output.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_SCOPE );

				//output.Region = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( output.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_RESIDENT );
				output.OfferedIn = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( output.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_OFFERREDIN );

				//output.VerificationStatus = Entity_VerificationStatusManager.GetAll( output.Id );
			}


			if ( IsValidDate( input.Created ) )
				output.Created = ( DateTime ) input.Created;
			if ( IsValidDate( input.LastUpdated ) )
				output.LastUpdated = ( DateTime ) input.LastUpdated;
		}
		static string SetEntitySummary( ThisResource to )
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

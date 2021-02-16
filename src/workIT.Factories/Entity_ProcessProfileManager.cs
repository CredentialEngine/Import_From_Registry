using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using workIT.Models;
using workIT.Models.Common;
using workIT.Models.ProfileModels;
using workIT.Utilities;

using ThisEntity = workIT.Models.ProfileModels.ProcessProfile;
using DBEntity = workIT.Data.Tables.Entity_ProcessProfile;
using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;

using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;

namespace workIT.Factories
{
	public class Entity_ProcessProfileManager : BaseFactory
	{
		static string thisClassName = "Entity_ProcessProfileManager";


		public static int DEFAULT_PROCESS_TYPE = 1;
		public static int ADMIN_PROCESS_TYPE = 1;
		public static int DEV_PROCESS_TYPE = 2; //convert to 2 from 7
		public static int MTCE_PROCESS_TYPE = 3; //convert to 3 from 8

		public static int APPEAL_PROCESS_TYPE = 4;	//to 4 from 2
		public static int COMPLAINT_PROCESS_TYPE = 5;	//to 5 from 3

		[Obsolete]
		public static int CRITERIA_PROCESS_TYPE = 6;
		public static int REVIEW_PROCESS_TYPE = 7;
		public static int REVOKE_PROCESS_TYPE = 8;




		#region Entity Persistance ===================
		public bool SaveList( List<ThisEntity> list, int processProfileTypeId, Guid parentUid, ref SaveStatus status )
		{
			if ( list == null || list.Count == 0 )
				return true;

			bool isAllValid = true;
			foreach ( ThisEntity item in list )
			{
				item.ProcessTypeId = processProfileTypeId;
				Save( item, parentUid, ref status );
			}

			return isAllValid;
		}

		/// <summary>
		/// Persist ProcessProfile
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="parentUid"></param>
		/// <param name="userId"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public bool Save( ThisEntity entity, Guid parentUid, ref SaveStatus status )
		{
			status.HasSectionErrors = false;

			if ( !IsValidGuid( parentUid ) )
			{
				status.AddError( "Error: the parent identifier was not provided." );
				return false;
			}


			int count = 0;

			DBEntity efEntity = new DBEntity();

			Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				status.AddError( "Error - the parent entity was not found." );
				return false;
			}


			//determine type
			int profileTypeId = 0;
			if ( entity.ProcessTypeId > 0 )
				profileTypeId = entity.ProcessTypeId;
			else
			{
				//
				switch ( entity.ProcessProfileType )
				{
					case "AppealProcess":
						entity.ProcessTypeId = APPEAL_PROCESS_TYPE;
						break;
					case "ComplaintProcess":
						entity.ProcessTypeId = COMPLAINT_PROCESS_TYPE;
						break;
					//case "CriteriaProcess":
					//	entity.ProcessTypeId = CRITERIA_PROCESS_TYPE;
					//	break;

					case "ReviewProcess":
						entity.ProcessTypeId = REVIEW_PROCESS_TYPE;
						break;

					case "RevocationProcess":
						entity.ProcessTypeId = REVOKE_PROCESS_TYPE;
						break;

					case "ProcessProfile":
						entity.ProcessTypeId = DEFAULT_PROCESS_TYPE;
						break;

					case "CredentialProcess":
						entity.ProcessTypeId = DEFAULT_PROCESS_TYPE;
						break;

					case "MaintenanceProcess":
						entity.ProcessTypeId = MTCE_PROCESS_TYPE;
						break;

					case "AdministrationProcess":
						entity.ProcessTypeId = ADMIN_PROCESS_TYPE;
						break;

					case "DevelopmentProcess":
						entity.ProcessTypeId = DEV_PROCESS_TYPE;
						break;
					//
					default:
						entity.ProcessTypeId = 1;
						status.AddError( string.Format( "Error: Unexpected profile type of {0} was encountered.", entity.ProcessProfileType ) );
						return false;
				}
			}
			using ( var context = new EntityContext() )
			{


				if ( ValidateProfile( entity,  ref  status ) == false )
				{
					status.AddError( "Process Profile was invalid. " + SetEntitySummary( entity ) );
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

					context.Entity_ProcessProfile.Add( efEntity );
					count = context.SaveChanges();
					//update profile record so doesn't get deleted
					entity.Id = efEntity.Id;
					entity.ParentId = parent.Id;
					entity.RowId = efEntity.RowId;
					if ( count == 0 )
					{
						status.AddError( string.Format( " Unable to add Profile: {0} <br\\> ", string.IsNullOrWhiteSpace( entity.ProfileName ) ? "no description" : entity.ProfileName ) );
					}
					else
					{
						//other entity components use a trigger to create the entity Object. If a trigger is not created, then child adds will fail (as typically use entity_summary to get the parent. As the latter is easy, make the direct call?

						UpdateParts( entity, ref status );
					}
				}
				else
				{
					entity.ParentId = parent.Id;

					efEntity = context.Entity_ProcessProfile.SingleOrDefault( s => s.Id == entity.Id );
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

			return status.WasSectionValid;
		}

		private bool UpdateParts( ThisEntity entity, ref SaveStatus status )
		{
			bool isAllValid = true;

			EntityPropertyManager mgr = new EntityPropertyManager();
			if ( mgr.AddProperties( entity.ExternalInput, entity.RowId, CodesManager.ENTITY_TYPE_PROCESS_PROFILE, CodesManager.PROPERTY_CATEGORY_EXTERNAL_INPUT_TYPE, false, ref status ) == false )
				isAllValid = false;

			if ( HandleTargets( entity, ref status ) == false )
				isAllValid = false;
			//
			return isAllValid;
		}
		private bool HandleTargets( ThisEntity entity, ref SaveStatus status )
		{
			status.HasSectionErrors = false;
			int newId = 0;
			if ( entity.TargetCredentialIds != null && entity.TargetCredentialIds.Count > 0 )
			{
				Entity_CredentialManager ecm = new Entity_CredentialManager();
				foreach ( int id in entity.TargetCredentialIds )
				{
					ecm.Add( entity.RowId, id, BaseFactory.RELATIONSHIP_TYPE_HAS_PART, ref newId, ref status );
				}
			}

			if ( entity.TargetAssessmentIds != null && entity.TargetAssessmentIds.Count > 0 )
			{
				Entity_AssessmentManager eam = new Entity_AssessmentManager();
				foreach ( int id in entity.TargetAssessmentIds )
				{
					newId = eam.Add( entity.RowId, id, BaseFactory.RELATIONSHIP_TYPE_HAS_PART, true, ref status );
				}
			}

			if ( entity.TargetLearningOpportunityIds != null && entity.TargetLearningOpportunityIds.Count > 0 )
			{
				Entity_LearningOpportunityManager elm = new Entity_LearningOpportunityManager();
				foreach ( int id in entity.TargetLearningOpportunityIds )
				{
					newId = elm.Add( entity.RowId, id, BaseFactory.RELATIONSHIP_TYPE_HAS_PART, true, ref status );
				}
			}

			return status.WasSectionValid;
		}
		public bool Delete( int recordId, ref string statusMessage )
		{
			bool isOK = true;
			using ( var context = new EntityContext() )
			{
				DBEntity p = context.Entity_ProcessProfile.FirstOrDefault( s => s.Id == recordId );
				if ( p != null && p.Id > 0 )
				{
					context.Entity_ProcessProfile.Remove( p );
					int count = context.SaveChanges();
				}
				else
				{
					statusMessage = string.Format( "Process Profile record was not found: {0}", recordId );
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
					var results = context.Entity_ProcessProfile.Where( s => s.EntityId == parent.Id )
					.ToList();
					if ( results == null || results.Count == 0 )
						return true;

					context.Entity_ProcessProfile.RemoveRange( context.Entity_ProcessProfile.Where( s => s.EntityId == parent.Id ) );
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

        public bool ValidateProfile( ThisEntity profile, ref SaveStatus status )
		{
			status.HasSectionErrors = false;	

			if ( string.IsNullOrWhiteSpace( profile.ProfileName ) )
			{
				//status.AddError( "A profile name must be entered" );
			}
			//should be something else
			if ( !IsUrlValid( profile.ProcessMethod, ref commonStatusMessage ) )
			{
				status.AddWarning( "The Process Method Url is invalid. " + commonStatusMessage );
			}
			if ( !IsUrlValid( profile.ProcessStandards, ref commonStatusMessage ) )
			{
				status.AddWarning( "The Process Standards Url is invalid. " + commonStatusMessage );
			}
			if ( !IsUrlValid( profile.ScoringMethodExample, ref commonStatusMessage ) )
			{
				status.AddWarning( "The Scoring Method Example Url is invalid. " + commonStatusMessage );
			}
			if ( !IsUrlValid( profile.SubjectWebpage, ref commonStatusMessage ) )
				status.AddWarning( "The Subject Webpage is invalid. " + commonStatusMessage );
			

			return status.WasSectionValid;
		}
		
		#endregion
		#region  retrieval ==================

		/// <summary>
		/// Get all ProcessProfile for the parent
		/// Uses the parent Guid to retrieve the related Entity, then uses the EntityId to retrieve the child objects.
		/// </summary>
		/// <param name="parentUid"></param>
		public static List<ThisEntity> GetAll( Guid parentUid, bool getForList = false)
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
					List<DBEntity> results = context.Entity_ProcessProfile
							.Where( s => s.EntityId == parent.Id )
							.OrderBy( s => s.Id )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( DBEntity item in results )
						{
							entity = new ThisEntity();
							MapFromDB( item, entity, true, getForList );
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
					DBEntity item = context.Entity_ProcessProfile
							.SingleOrDefault( s => s.Id == profileId );

					if ( item != null && item.Id > 0 )
					{
						MapFromDB( item, entity, true, false );
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
				to.ProcessTypeId = from.ProcessTypeId;
			}
			to.Id = from.Id;
			
			//to.ProfileName = from.ProcessProfileType;
			to.Description = from.Description;
			if ( IsValidDate( from.DateEffective ) )
				to.DateEffective = DateTime.Parse( from.DateEffective );
			else //handle reset
				to.DateEffective = null;

            if ( from.ProcessingAgentUid == null || from.ProcessingAgentUid.ToString() == DEFAULT_GUID )
			{
				to.ProcessingAgentUid = null;//			
			}
			else
			{
				to.ProcessingAgentUid = from.ProcessingAgentUid;
			}

			to.SubjectWebpage = GetUrlData( from.SubjectWebpage );

			to.ProcessFrequency = from.ProcessFrequency;
			//to.TargetCompetencyFramework = from.TargetCompetencyFramework;

			to.ProcessMethod = from.ProcessMethod;
			to.ProcessMethodDescription = from.ProcessMethodDescription;

			to.ProcessStandards = from.ProcessStandards;
			to.ProcessStandardsDescription = from.ProcessStandardsDescription;

			to.ScoringMethodDescription = from.ScoringMethodDescription;
			to.ScoringMethodExample = from.ScoringMethodExample;
			to.ScoringMethodExampleDescription = from.ScoringMethodExampleDescription;
			to.VerificationMethodDescription = from.VerificationMethodDescription;




		}
		public static void MapFromDB( DBEntity from, ThisEntity to, bool includingItems, bool getForList )
		{
			to.Id = from.Id;
			to.RowId = from.RowId;
			//HANDLE PROCESS TYPES
			if ( from.ProcessTypeId != null && ( int ) from.ProcessTypeId > 0)
				to.ProcessTypeId = ( int ) from.ProcessTypeId;
			else
				to.ProcessTypeId = 1;

			to.ProfileName = to.ProcessType;
            //need to distinguish if for detail
            to.ProcessProfileType = GetProfileType( to.ProcessTypeId );

            to.Description = from.Description;
			if ( ( to.Description ?? "" ).Length > 5 )
			{
                //this should just be the type now
                to.ProfileName = GetProfileType( to.ProcessTypeId );

                //to.ProfileName = to.Description.Length > 100 ? to.Description.Substring(0,100) + " . . ." : to.Description;
			}

			if ( from.Entity != null )
				to.ParentId = from.Entity.Id;

			//- provide minimum option, for lists
			if ( getForList )
				return;

			if ( IsGuidValid( from.ProcessingAgentUid ) )
			{
				to.ProcessingAgentUid = ( Guid ) from.ProcessingAgentUid;

				to.ProcessingAgent = OrganizationManager.GetBasics( to.ProcessingAgentUid );
			}

			if ( IsValidDate( from.DateEffective ) )
				to.DateEffective = ( ( DateTime ) from.DateEffective ).ToShortDateString();
			else
				to.DateEffective = "";
			to.SubjectWebpage = from.SubjectWebpage;

			to.ProcessFrequency = from.ProcessFrequency;
			//to.TargetCompetencyFramework = from.TargetCompetencyFramework;
			//to.RequiresCompetenciesFrameworks = Entity_CompetencyFrameworkManager.GetAll( to.RowId, "requires" );

			to.ProcessMethod = from.ProcessMethod;
			to.ProcessMethodDescription = from.ProcessMethodDescription;

			to.ProcessStandards = from.ProcessStandards;
			to.ProcessStandardsDescription = from.ProcessStandardsDescription;

			to.ScoringMethodDescription = from.ScoringMethodDescription;
			to.ScoringMethodExample = from.ScoringMethodExample;
			to.ScoringMethodExampleDescription = from.ScoringMethodExampleDescription;
			to.VerificationMethodDescription = from.VerificationMethodDescription;

			if ( IsValidDate( from.DateEffective ) )
				to.DateEffective = ( ( DateTime ) from.DateEffective ).ToShortDateString();
			else
				to.DateEffective = "";

			//enumerations
			to.ExternalInput = EntityPropertyManager.FillEnumeration( to.RowId, CodesManager.PROPERTY_CATEGORY_EXTERNAL_INPUT_TYPE );
			//to.StaffEvaluationMethod = EntityPropertyManager.FillEnumeration( to.RowId, CodesManager.PROPERTY_CATEGORY_STAFF_EVALUATION_METHOD );
			//to.ProcessMethod = EntityPropertyManager.FillEnumeration( to.RowId, CodesManager.PROPERTY_CATEGORY_PROCESS_METHOD );

			to.ProfileSummary = SetEntitySummary( to );


			if ( includingItems )
			{
				
				to.Jurisdiction = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( to.RowId );
				//will only be one, but could model with multiple
				to.TargetCredential = Entity_CredentialManager.GetAll( to.RowId, BaseFactory.RELATIONSHIP_TYPE_HAS_PART );
				to.TargetAssessment = Entity_AssessmentManager.GetAll( to.RowId, BaseFactory.RELATIONSHIP_TYPE_HAS_PART );

				to.TargetLearningOpportunity = Entity_LearningOpportunityManager.LearningOpps_GetAll( to.RowId, true );
			}


			if ( IsValidDate( from.Created ) )
				to.Created = ( DateTime ) from.Created;
			
			if ( IsValidDate( from.LastUpdated ) )
				to.LastUpdated = ( DateTime ) from.LastUpdated;
		
		}
		static string SetEntitySummary( ThisEntity to )
		{
			string summary = "Process Profile ";
			summary = to.ProcessType + ( string.IsNullOrWhiteSpace( to.ProfileName ) ? "Process Profile" : to.ProfileName );

			if ( !string.IsNullOrWhiteSpace( to.ProfileName ) )
			{
				return to.ProfileName;
			}

			if ( to.Id > 1 )
			{
				summary += to.Id.ToString();
			}
			return summary;

		}
        static string GetProfileType( int processTypeId, bool returningLabeFormat = false )
        {
            switch (processTypeId)
            {
                case 1:
                    return returningLabeFormat ? "AdministrationProcess" : "Administration Process";
                case 2:
                    return returningLabeFormat ? "DevelopmentProcess" : "Development Process";
                case 3:
                    return returningLabeFormat ? "MaintenanceProcess" : "Maintenance Process";
                case 4:
                    return returningLabeFormat ? "AppealProcess" : "Appeal Process";
                case 5:
                    return returningLabeFormat ? "ComplaintProcess" : "Complaint Process";
                case 7:
                    return returningLabeFormat ? "ReviewProcess" : "Review Process";
                case 8:
                    return returningLabeFormat ? "RevocationProcess" : "Revocation Process";

                default:
                    return returningLabeFormat ? "ProcessProfile" : "Process Process";
            }
        }
        #endregion

        #region  validations
        public static bool IsParentBeingAddedAsChildToItself( int profileId, int childId, int childEntityTypeId )
		{
			bool isOk = false;
			using ( var context = new EntityContext() )
			{
				//get the profile that is the parent of the child
				DBEntity efEntity = context.Entity_ProcessProfile
						.SingleOrDefault( s => s.Id == profileId );

				if ( efEntity != null 
					&& efEntity.Id > 0 
					&& efEntity.Entity != null )
				{
					//check if the parent entity is the same one being added as a child
					if ( efEntity.Entity.EntityTypeId == childEntityTypeId
						&& efEntity.Entity.EntityBaseId == childId )
					{
						return true;
					}
				}
			}

			return isOk;
		}

		#endregion
	}
}

﻿using System;
using System.Collections.Generic;
using System.Linq;

using workIT.Models;
using workIT.Models.Common;
using ThisResource = workIT.Models.ProfileModels.ConditionProfile;
using DBEntity = workIT.Data.Tables.Entity_ConditionProfile;
using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;

using workIT.Utilities;
using CM = workIT.Models.Common;
using workIT.Models.ProfileModels;
using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;
using Newtonsoft.Json;
using System.Data.Entity.Validation;

namespace workIT.Factories
{
	public class Entity_ConditionProfileManager : BaseFactory
	{
		static string thisClassName = "Entity_ConditionProfileManager";
		#region constants
		public static int ConnectionProfileType_Requirement = 1;
		public static int ConnectionProfileType_Recommendation = 2;
		public static int ConnectionProfileType_NextIsRequiredFor = 3;
		public static int ConnectionProfileType_NextIsRecommendedFor = 4;
		public static int ConnectionProfileType_Renewal = 5;
		public static int ConnectionProfileType_AdvancedStandingFor = 6;
		public static int ConnectionProfileType_AdvancedStandingFrom = 7;
		public static int ConnectionProfileType_PreparationFor = 8;
		public static int ConnectionProfileType_PreparationFrom = 9;
		public static int ConnectionProfileType_Corequisite = 10;
		public static int ConnectionProfileType_EntryCondition = 11;

		public static int ConnectionProfileType_Membership = 14;
		public static int ConnectionProfileType_CoPrerequisite = 15;
		public static int ConnectionProfileType_SupportServiceCondition = 16;

		public static int ConditionSubType_Basic = 1;
		public static int ConditionSubType_CredentialConnection = 2;
		public static int ConditionSubType_Assessment = 3;
		public static int ConditionSubType_LearningOpportunity = 4;
		public static int ConditionSubType_Alternative = 5;
		public static int ConditionSubType_Additional = 6;
		#endregion

		#region persistance ==================

		public bool SaveList( List<ThisResource> list, int conditionTypeId, Guid parentUid, ref SaveStatus status, int subConnectionTypeId = 0 )
		{
			//a delete ALL is no longer being done before entering here so need to check for deletes in method
			if ( !IsValidGuid( parentUid ) )
			{
				status.AddError( "Error: the parent identifier was not provided." );
			}
			Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				status.AddError( "Error - the parent entity was not found." );
				return false;
			}
			//20-12-28 - skip delete all from credential, etc. Rather checking  in save

			//now be sure to still do a delete all until implementing a balance line
			//could set date and delete all before this date!
			DateTime updateDate = DateTime.Now;
			if ( IsValidDate( status.EnvelopeUpdatedDate ) )
			{
				updateDate = status.LocalUpdatedDate;
			}

			var currentConditions = GetAllForConditionType( parent, conditionTypeId, subConnectionTypeId );
			bool isAllValid = true;
			if ( list == null || list.Count == 0 )
			{
				if ( currentConditions != null && currentConditions.Any() )
				{
					//no input, and existing conditions, delete all
					DeleteAllForConditionType( parent, conditionTypeId, subConnectionTypeId, ref status );
				}
				return true;
			}
			//may not need this if the new list version works
			else if ( list.Count == 1 && currentConditions.Count == 1 )
			{
				//One of each, just do update of one
				//NO - can miss changes to targets? OR can get duplicates for alternate conditions!
				var existingConditionProfile = currentConditions[ 0 ];
				var entity = list[ 0 ];
				entity.Id = existingConditionProfile.Id;
				entity.ConnectionProfileTypeId = conditionTypeId;
				entity.ConditionSubTypeId = subConnectionTypeId;
				if ( existingConditionProfile.AlternativeCondition != null && existingConditionProfile.AlternativeCondition.Any())
				{
					DeleteAllAlternativeConditions( existingConditionProfile.RowId, ref status );
				}
				Save( entity, parent, updateDate, ref status );
			}
			else
			{
				//may need to delete all here, as cannot easily/dependably look up a condition profile
				if ( currentConditions.Count > 0)
					DeleteAllForConditionType( parent, conditionTypeId, subConnectionTypeId, ref status );
				foreach ( ThisResource item in list )
				{
					item.ConnectionProfileTypeId = conditionTypeId;
					item.ConditionSubTypeId = subConnectionTypeId;
					Save( item, parent, updateDate, ref status );
				}
				//delete any entities with last updated less than updateDate
				//DeleteAll( parent, ref status, updateDate );
			}
			//bool isAllValid = true;
			//foreach ( ThisResource item in list )
			//{
			//	item.ConnectionProfileTypeId = conditionTypeId;
			//	item.ConditionSubTypeId = subConnectionTypeId;
			//	Save( item, parent, ref status );
			//}

			return isAllValid;
		}

		private bool Save( ThisResource entity, Entity parent, DateTime updateDate, ref SaveStatus status  )
		{
			bool isValid = true;
			
			entity.RelatedEntityId = parent.Id;

			int profileTypeId = 0;
			
			if ( entity.ConnectionProfileTypeId > 0 )
				profileTypeId = entity.ConnectionProfileTypeId;
			else
			{
				profileTypeId = GetConditionTypeId( entity.ConnectionProfileType );
			}
			bool doingUpdateParts = true;
			try
			{

				if ( !ValidateProfile( entity, ref status ) )
				{
					//return false;
				}

				entity.ConnectionProfileTypeId = profileTypeId;
				//should always be add if always resetting the entity
				if ( entity.Id > 0 )
				{
					using ( var context = new EntityContext() )
					{

						DBEntity p = context.Entity_ConditionProfile
							.FirstOrDefault( s => s.Id == entity.Id );
						if ( p != null && p.Id > 0 )
						{
							entity.RowId = p.RowId;
							entity.RelatedEntityId = p.EntityId;
							MapToDB( entity, p );

							if ( HasStateChanged( context ) )
							{
								p.LastUpdated = System.DateTime.Now;
								context.SaveChanges();
							}
						}
						else
						{
							doingUpdateParts = false;
							//error should have been found
							isValid = false;
							status.AddWarning( string.Format( "Error: the requested record was not found: recordId: {0}", entity.Id ) );
						}
					}
					//regardless, check parts
					if ( doingUpdateParts )
						isValid = UpdateParts( entity, updateDate, ref status );
				}
				else
				{
					int newId = Add( entity, updateDate, ref status );
					if ( newId == 0 || status.HasErrors )
						isValid = false;
				}

			}
			catch ( DbEntityValidationException dbex )
			{
				string message = HandleDBValidationError( dbex, thisClassName + ".Save()", string.Format( "EntityId: {0} , ConnectionProfileTypeId: {1}  ", entity.RelatedEntityId, entity.ConnectionProfileTypeId ) );
				status.AddWarning( message );
				LoggingHelper.LogError( dbex, thisClassName + $".Save()-DbEntityValidationException, Parent: {parent.EntityBaseName} (type: {parent.EntityTypeId}, Id: {parent.EntityBaseId})" );
				return isValid;
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + $".Save(), EntityId: {entity.RelatedEntityId}" );
				return isValid;
			}
			return isValid;
		}


		/// <summary>
		/// add a ConditionProfile
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		private int Add(ThisResource entity, DateTime updateDate, ref SaveStatus status)
		{
			DBEntity efEntity = new DBEntity();
			bool doingUpdateParts = true;
			int newId = 0;
			try
			{
				using ( var context = new EntityContext() )
				{
					MapToDB( entity, efEntity );

					efEntity.EntityId = entity.RelatedEntityId;
					if ( IsValidGuid( entity.RowId ) )
						efEntity.RowId = entity.RowId;
					else
						efEntity.RowId = Guid.NewGuid();
					efEntity.Created = efEntity.LastUpdated = updateDate;

					context.Entity_ConditionProfile.Add( efEntity );

					// submit the change to database
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						newId = entity.Id = efEntity.Id;
						entity.RowId = efEntity.RowId;					

						//return efEntity.Id;
					}
					else
					{
						doingUpdateParts = false;
						//?no info on error
						status.AddWarning( "Error - the profile was not saved. " );
						string message = string.Format( "{0}.Add() Failed", "Attempted to add a ConditionProfile. The process appeared to not work, but was not an exception, so we have no message, or no clue. ConditionProfile. EntityId: {1}, ConnectionProfileTypeId: {2}", thisClassName, entity.RelatedEntityId, entity.ConnectionProfileTypeId );
						EmailManager.NotifyAdmin( thisClassName + ".Add() Failed", message );
					}
				}
				//21-04-21 mparsons - end the current context before doing parts
				if ( doingUpdateParts )
				{
					UpdateParts( entity, updateDate, ref status );
				}
			}
			catch (System.Data.Entity.Validation.DbEntityValidationException dbex)
			{
				string message = HandleDBValidationError( dbex, "Entity_ConditionProfileManager.Add()", string.Format( "EntityId: 0 , ConnectionProfileTypeId: {1}  ", entity.RelatedEntityId, entity.ConnectionProfileTypeId ) );
				status.AddWarning( message );
					
			}
			catch (Exception ex)
			{
				LoggingHelper.LogError(ex, thisClassName + string.Format(".Add(), EntityId: {0}", entity.RelatedEntityId));
			}
			

			return newId;
		}
		public bool DeleteAllAlternativeConditions( Guid parentUID, ref SaveStatus status )
		{
			bool isValid = true;
			try
			{
				//NOTE: could get the entityId and do an RemoveAll
				using ( var context = new EntityContext() )
				{
					var results = ( from entity in context.Entity
								  join alternateCP in context.Entity_ConditionProfile on entity.Id equals alternateCP.EntityId
								  where entity.EntityUid == parentUID
									   && alternateCP.ConditionSubTypeId == 5
								  select new Entity()
								  {
									  Id=alternateCP.Id
								  } ).ToList();
					if ( results == null || results.Count == 0 )
						return true;

					foreach ( var item in results )
					{
						DBEntity efEntity = context.Entity_ConditionProfile
							.SingleOrDefault( s => s.Id == item.Id );

						if ( efEntity != null && efEntity.Id > 0 )
						{
							context.Entity_ConditionProfile.Remove( efEntity );
							int count = context.SaveChanges();
							if ( count > 0 )
							{
								isValid = true;
								//16-10-19 mp - create 'After Delete' triggers to delete the Entity
								//new EntityManager().Delete(rowId, ref statusMessage);
							}
						}
						else
						{
							status.AddError( string.Format( "DeleteAllAlternativeConditions. Error - delete was not possible, as record was not found. parentUID: {0} item.Id: {1}", parentUID, item.Id) );
						}
					}
				}
			}
			catch ( Exception ex )
			{
				var msg = BaseFactory.FormatExceptions( ex );
				LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DeleteAllAlternativeConditions. parentUID: {0}, exception: {1}", parentUID, msg ) );
			}

			return isValid;
		}
        public bool DeleteAll( Entity parent, ref SaveStatus status, DateTime? lastUpdated = null )
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
					//20-12-03 mp - getting deadlock errors. 
					//			- trying this here and individual deletes for addresses to assess
					//20-12-09 mp - didn't help, try using loop
					//context.Database.ExecuteSqlCommand( "SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;" );
					//context.Entity_ConditionProfile.RemoveRange( context.Entity_ConditionProfile.Where( s => s.EntityId == parent.Id ) );
		//            int count = context.SaveChanges();
		//            if ( count > 0 )
		//            {
		//                isValid = true;
		//            }


					var results = context.Entity_ConditionProfile.Where( s => s.EntityId == parent.Id && ( lastUpdated == null || s.LastUpdated < lastUpdated ) ).ToList();
					if ( results == null || results.Count == 0 )
						return true;

					foreach ( var item in results )
					{
						//21-03-31 mp - just removing the profile will not remove its entity and the latter's children!
						//NO - handled by trigger - i.e. trgConnectionProfileAfterDelete - or is explicit better?
						//string statusMessage = string.Empty;
						//we have a trigger for this
						//new EntityManager().Delete( item.RowId, string.Format( "ConditionProfile: {0} for EntityType: {1} ({2})", item.Id, parent.EntityTypeId, parent.EntityBaseId ), ref statusMessage );

						context.Entity_ConditionProfile.Remove( item );
						var count = context.SaveChanges();
						if ( count > 0 )
						{

						}
					}
				}
		
			} catch (Exception ex)
			{
				var msg = BaseFactory.FormatExceptions( ex );
		LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DeleteAll. ParentType: {0}, baseId: {1}, exception: {2}", parent.EntityType, parent.EntityBaseId, msg ) );
			}
            return isValid;
        }

		public bool DeleteAllForConditionType( Entity parent, int conditionTypeId, int subConditionTypeId, ref SaveStatus status )
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
					var results = context.Entity_ConditionProfile
								.Where( s => s.EntityId == parent.Id && s.ConnectionTypeId == conditionTypeId && ( subConditionTypeId == 0 || s.ConditionSubTypeId == subConditionTypeId ) )
								.ToList();
					if ( results == null || results.Count == 0 )
						return true;

					foreach ( var item in results )
					{
						//check for alternative conditions
						DeleteAllAlternativeConditions( item.RowId, ref status );

						context.Entity_ConditionProfile.Remove( item );
						var count = context.SaveChanges();
						if ( count > 0 )
						{

						}
					}
				}
			}
			catch ( Exception ex )
			{
				var msg = BaseFactory.FormatExceptions( ex );
				LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DeleteAllForConditionType. ParentType: {0}, baseId: {1}, exception: {2}", parent.EntityType, parent.EntityBaseId, msg ) );
			}
			return isValid;
		}

		public bool UpdateParts(ThisResource entity, DateTime updateDate, ref SaveStatus status)
		{
			bool isAllValid = true;
			Entity relatedEntity = EntityManager.GetEntity( entity.RowId );
			if ( relatedEntity == null || relatedEntity.Id == 0 )
			{
				status.AddError( "Error - the related Entity was not found." );
				return false;
			}
			//Alternative conditions
			//TODO - what is necessary for deleting existing????
			try
			{
				if ( entity.AlternativeCondition != null && entity.AlternativeCondition.Count > 0 )
				{
					Entity parent = EntityManager.GetEntity( entity.RowId );
					foreach ( ThisResource item in entity.AlternativeCondition )
					{
						item.ConnectionProfileTypeId = ConnectionProfileType_Requirement;
						item.ConditionSubTypeId = ConditionSubType_Alternative;
						Save( item, parent, updateDate, ref status );
					}
				}
			} catch( Exception ex)
			{
				LoggingHelper.DoTrace( 1, thisClassName + ".UpdateParts(). Exception while processing AlternativeCondition. " + ex.Message );
				status.AddError( ex.Message );
			}

			try
			{
				//CostProfile
				//no deletes necessary
				CostProfileManager cpm = new Factories.CostProfileManager();
				cpm.SaveList( entity.EstimatedCosts, entity.RowId, ref status );
				//
				new Entity_CommonCostManager().SaveList( entity.CostManifestIds, entity.RowId, ref status );
			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, thisClassName + ".UpdateParts(). Exception while processing estimated and common costs. " + ex.Message );
			}
			//
			try
			{
				Entity_JurisdictionProfileManager jpm = new Entity_JurisdictionProfileManager();
				//do deletes - NOTE: other jurisdictions are added in: UpdateAssertedIns
				jpm.DeleteAll( relatedEntity, ref status );
				jpm.SaveList( entity.Jurisdiction, entity.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_SCOPE, ref status );
				jpm.SaveList( entity.ResidentOf, entity.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_RESIDENT, ref status );

			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, thisClassName + ".UpdateParts(). Exception while processing jurisdictions. " + ex.Message );
				status.AddError( ex.Message );

			}
			//
			try
			{
				EntityPropertyManager mgr = new EntityPropertyManager();
				//first clear all propertiesd
				mgr.DeleteAll( relatedEntity, ref status );
				if ( mgr.AddProperties( entity.AudienceLevel, entity.RowId, CodesManager.ENTITY_TYPE_CONDITION_PROFILE, CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL, false, ref status ) == false )
					isAllValid = false;

				if ( mgr.AddProperties( entity.ApplicableAudienceType, entity.RowId, CodesManager.ENTITY_TYPE_CONDITION_PROFILE, CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE, false, ref status ) == false )
					isAllValid = false;

			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, thisClassName + ".UpdateParts(). Exception while processing properties. " + ex.Message );
				status.AddError( ex.Message );

			}


			try
			{
				Entity_ReferenceManager erm = new Entity_ReferenceManager();
				//21-03-24 deletes were not being done!!!!
				//		-what about other properties????
				erm.DeleteAll( relatedEntity, ref status );

				if ( erm.Add( entity.Condition, entity.RowId, CodesManager.ENTITY_TYPE_CONDITION_PROFILE, ref status, CodesManager.PROPERTY_CATEGORY_CONDITION_ITEM, false ) == false )
					isAllValid = false;

				if ( erm.Add( entity.SubmissionOf, entity.RowId, CodesManager.ENTITY_TYPE_CONDITION_PROFILE, ref status, CodesManager.PROPERTY_CATEGORY_SUBMISSION_ITEM, false ) == false )
					isAllValid = false;

			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, thisClassName + ".UpdateParts(). Exception while processing references, etc. " + ex.Message );
				status.AddError( ex.Message );

			}

			//
			if ( HandleTargets( entity, relatedEntity, ref status ) == false )
				isAllValid = false;

			//JurisdictionProfile 
			try
			{
				Entity_JurisdictionProfileManager jpm = new Entity_JurisdictionProfileManager();
				//do deletes - NOTE: other jurisdictions are added in: UpdateAssertedIns
				jpm.DeleteAll( relatedEntity, ref status );
				jpm.SaveList( entity.Jurisdiction, entity.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_SCOPE, ref status );
				jpm.SaveList( entity.ResidentOf, entity.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_RESIDENT, ref status );

			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, thisClassName + ".UpdateParts(). Exception while processing jurisdictions. " + ex.Message );
				status.AddError( ex.Message );

			}

			//
			return isAllValid;
		}
		private bool HandleTargets( ThisResource entity, Entity relatedEntity, ref SaveStatus status )
		{
			status.HasSectionErrors = false;
			int newId = 0;
			try
			{
				Entity_CredentialManager ecm = new Entity_CredentialManager();
				ecm.DeleteAll( relatedEntity, ref status );
				if ( entity.TargetCredentialIds != null && entity.TargetCredentialIds.Count > 0 )
				{

					foreach ( int id in entity.TargetCredentialIds )
					{
						ecm.Add( entity.RowId, id, BaseFactory.RELATIONSHIP_TYPE_HAS_PART, ref newId, ref status );
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, thisClassName + ".HandleTargets(). Exception while processing TargetCredentials. " + ex.Message );
				status.AddError( ex.Message );

			}

			try
			{
				Entity_AssessmentManager eam = new Entity_AssessmentManager();
				eam.DeleteAll( relatedEntity, ref status );
				if ( entity.TargetAssessmentIds != null && entity.TargetAssessmentIds.Count > 0 )
				{

					foreach ( int id in entity.TargetAssessmentIds )
					{
						newId = eam.Add( entity.RowId, id, BaseFactory.RELATIONSHIP_TYPE_HAS_PART, true, ref status );
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, thisClassName + ".HandleTargets(). Exception while processing TargetAssessmentIds. " + ex.Message );
				status.AddError( ex.Message );

			}

			try
			{
				Entity_LearningOpportunityManager elm = new Entity_LearningOpportunityManager();
				elm.DeleteAll( relatedEntity, ref status );
				if ( entity.TargetLearningOpportunityIds != null && entity.TargetLearningOpportunityIds.Count > 0 )
				{

					foreach ( int id in entity.TargetLearningOpportunityIds )
					{
						LoggingHelper.DoTrace( 7, thisClassName + string.Format( ".HandleTargets. entity.ParentId: {0}, processing loppId: {1}, entity.RowId: {2}", entity.RelatedEntityId, id, entity.RowId.ToString() ) );
						//20-12-28 assuming adds here. OK - method checks for existing
						newId = elm.Add( entity.RowId, id, BaseFactory.RELATIONSHIP_TYPE_HAS_PART, true, ref status, false );
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, thisClassName + ".HandleTargets(). Exception while processing TargetLearningOpportunityIds. " + ex.Message );
				status.AddError( ex.Message );

			}
            //occ

            try
            {
                var occMgr = new Entity_OccupationManager();
                occMgr.DeleteAll( relatedEntity, ref status );
                if ( entity.TargetOccupationIds != null && entity.TargetOccupationIds.Count > 0 )
                {

                    foreach ( int id in entity.TargetOccupationIds )
                    {
                        LoggingHelper.DoTrace( 7, thisClassName + string.Format( ".HandleTargets. entity.ParentId: {0}, processing loppId: {1}, entity.RowId: {2}", entity.RelatedEntityId, id, entity.RowId.ToString() ) );
                        //20-12-28 assuming adds here. OK - method checks for existing
                        newId = occMgr.Add( entity.RowId, id, BaseFactory.RELATIONSHIP_TYPE_HAS_PART, true, ref status, false );
                    }
                }
            }
            catch ( Exception ex )
            {
                LoggingHelper.DoTrace( 1, thisClassName + ".HandleTargets(). Exception while processing TargetOccupationIds. " + ex.Message );
                status.AddError( ex.Message );
            }
			try
			{
				var jobMgr = new Entity_JobManager();
				jobMgr.DeleteAll( relatedEntity, ref status );
				if ( entity.TargetJobIds != null && entity.TargetJobIds.Count > 0 )
				{

					foreach ( int id in entity.TargetJobIds )
					{
						LoggingHelper.DoTrace( 7, thisClassName + string.Format( ".HandleTargets. entity.ParentId: {0}, processing loppId: {1}, entity.RowId: {2}", entity.RelatedEntityId, id, entity.RowId.ToString() ) );
						//20-12-28 assuming adds here. OK - method checks for existing
						newId = jobMgr.Add( entity.RowId, id, BaseFactory.RELATIONSHIP_TYPE_HAS_PART, true, ref status, false );
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, thisClassName + ".HandleTargets(). Exception while processing TargetJobIds. " + ex.Message );
				status.AddError( ex.Message );
			}

			try
			{
				if ( entity.TargetCompetency != null && entity.TargetCompetency.Count > 0 )
				{
					Entity_CompetencyManager ecm = new Entity_CompetencyManager();
					//23-01-08 mp - do delete from entity to handle multiple types
					//24-01-01 mp - there seems to be issues with timeouts. Look into doing replaces.
					ecm.DeleteAll( relatedEntity, ref status );
					ecm.SaveList("Requires", entity.TargetCompetency, entity.RowId, ref status );

				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, thisClassName + ".HandleTargets(). Exception while processing TargetCompetencies. " + ex.Message );
				status.AddError( ex.Message );

			}

			return status.WasSectionValid;
		}

		/// <summary>
		/// Delete a Condition Profile, and related Entity
		/// </summary>
		/// <param name="Id"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		//public bool Delete( int profileId, ref string statusMessage )
		//{
		//	bool isValid = false;
		//	if ( profileId == 0 )
		//	{
		//		statusMessage = "Error - missing an identifier for the ConditionProfile";
		//		return false;
		//	}
		//	using (var context = new EntityContext())
		//	{
		//		DBEntity efEntity = context.Entity_ConditionProfile
		//					.SingleOrDefault( s => s.Id == profileId );

		//		if (efEntity != null && efEntity.Id > 0)
		//		{
		//			Guid rowId = efEntity.RowId;
		//			context.Entity_ConditionProfile.Remove(efEntity);
		//			int count = context.SaveChanges();
		//			if (count > 0)
		//			{
		//				isValid = true;
		//				//16-10-19 mp - create 'After Delete' triggers to delete the Entity
		//				//new EntityManager().Delete(rowId, ref statusMessage);
		//			}
		//		}
		//		else
		//		{
		//			statusMessage = "Error - delete was not possible, as record was not found.";
		//		}
		//	}

		//	return isValid;
		//}

		private bool ValidateProfile( ThisResource item, ref SaveStatus status)
		{
			status.HasSectionErrors = false;
			bool isNameRequired = true;
			
			string firstEntityName = string.Empty;

			//TODO - treat connections separately!
			if ( item.ConnectionProfileType == "AssessmentConnections" )
			{
				isNameRequired = false;
			}
			else if ( item.ConnectionProfileType == "LearningOppConnections" )
			{
				isNameRequired = false;
				//can't fully edit this. On initial create, no lopp, and the TargetLopp is not returned, as these are immediate saves.
				//would have to determine if not initial, and then do a check for existing (as for to.TargetLearningOpportunity in toMap)

			}
			else
			if ( item.ConnectionProfileType == "CredentialConnections" 
				|| item.ConnectionProfileType == "Corequisite" 
				)
			{
				isNameRequired = false;
				firstEntityName = item.ConnectionProfileType;
				//List<Credential> list = Entity_CredentialManager.GetAll( item.RowId );
				//if ( item.Id > 0 && list.Count == 0 )
				//{
				//	status.AddWarning( "Error: a credential must be selected" );
				//	firstEntityName = "Credential Connection";
				//}
				//else
				//{
				//	if ( item.Id > 0)
				//	 firstEntityName = list[ 0 ].Name;
				//}
				item.ProfileSummary = firstEntityName;
			}

			//will use whatever is provided in ProfileName/Name
			//if ( string.IsNullOrWhiteSpace( item.ProfileSummary) )
			//{
			//	item.ProfileName = firstEntityName;
			//} else
			//	item.ProfileName = item.ProfileSummary;


			if ( !IsUrlValid( item.SubjectWebpage, ref commonStatusMessage ) )
			{
				status.AddWarning( "The condition Subject Webpage is invalid" + commonStatusMessage );
			}
			if ( item.MinimumAge < 0 || item.MinimumAge > 100 )
			{
				status.AddWarning( "Error: invalid value for minimum age" );
			}
			if ( item.YearsOfExperience < 0 || item.YearsOfExperience > 50 )
			{
				status.AddWarning( "Error: invalid value for years of experience" );
			}

			if ( item.Weight < 0 || item.Weight > 1 )
				status.AddWarning( "Error: invalid value for Weight. Must be a decimal value between 0 and 1." );
			if ( item.ConnectionProfileType == "LearningOppConnections" && item.TargetLearningOpportunity.Count == 0 )
			{
				//the list may always be empty!!
				//status.AddWarning( "Error: At least one Learning Opportunity must be added to this condition profile." );
			}

			//if ( item.CreditHourValue < 0 || item.CreditHourValue > 10000 )
			//	status.AddWarning( "Error: invalid value for Credit Hour Value. Must be a reasonable decimal value greater than zero." );

			if ( item.CreditUnitValue < 0 || item.CreditUnitValue > 1000 )
				status.AddWarning( "Error: invalid value for Credit Unit Value. Must be a reasonable decimal value greater than zero." );


			//can only have credit hours properties, or credit unit properties, not both
			bool hasCreditHourData = false;
			bool hasCreditUnitData = false;
			//if ( item.CreditHourValue > 0 || ( item.CreditHourType ?? string.Empty ).Length > 0 )
			//	hasCreditHourData = true;
			if (  item.CreditUnitTypeId > 0
				|| (item.CreditUnitTypeDescription ?? string.Empty).Length > 0
				|| item.CreditUnitValue > 0)
				hasCreditUnitData = true;

			if ( hasCreditHourData && hasCreditUnitData )
				status.AddWarning( "Error: Data can be entered for Credit Hour related properties or Credit Unit related properties, but not for both." );

			return status.WasSectionValid;
		}
		#endregion

		#region == Retrieval =======================

		

		/// <summary>
		///Get all condition profiles for parent 
		/// For this method, the parent is resonsible for assigning to the proper condition profile types, if more than one expected.
		/// </summary>
		/// <param name="parentUid"></param>
		/// <returns></returns>
		public static List<ThisResource> GetAll( Guid parentUid, bool isForCredentialDetail, bool getMinimumOnly = false )
		{
			ThisResource entity = new ThisResource();
			List<ThisResource> list = new List<ThisResource>();
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

					List<DBEntity> results = context.Entity_ConditionProfile
							.Where( s => s.EntityId == parent.Id )
							.OrderBy( s => s.ConnectionTypeId )
							.ThenBy( s => s.Created )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( DBEntity item in results )
						{
							entity = new ThisResource();
							MapFromDB( item, entity, true, true, isForCredentialDetail, getMinimumOnly );

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

		public static List<ThisResource> GetAllForConditionType( Entity parent, int conditionTypeId, int subConnectionTypeId = 0 )
		{
			ThisResource entity = new ThisResource();
			List<ThisResource> list = new List<ThisResource>();
			//Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				return list;
			}

			try
			{
				using ( var context = new EntityContext() )
				{
					//context.Configuration.LazyLoadingEnabled = false;

					List<DBEntity> results = context.Entity_ConditionProfile
							.Where( s => s.EntityId == parent.Id && s.ConnectionTypeId == conditionTypeId && ( subConnectionTypeId == 0 || s.ConditionSubTypeId == subConnectionTypeId ) )
							.OrderBy( s => s.ConnectionTypeId )
							.ThenBy( s => s.Created )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( DBEntity item in results )
						{
							entity = new ThisResource();
							//??do we need all data? It will be replaced. The main issue will be references to lopps, asmts, etc. 
							MapFromDB( item, entity, true, true, false, false );
							list.Add( entity );
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetAllForConditionType" );
			}
			return list;
		}//
		public static List<Credential> GetAllCredentails( int topParentTypeId, int topParentEntityBaseId )
		{
			var list = new List<Credential>();
			Entity parent = EntityManager.GetEntity( topParentTypeId, topParentEntityBaseId );
			//Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				return list;
			}

			try
			{
				using ( var context = new EntityContext() )
				{
					List<DBEntity> results = context.Entity_ConditionProfile
							.Where( s => s.EntityId == parent.Id )
							.OrderBy( s => s.ConnectionTypeId )
							.ThenBy( s => s.Created )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( DBEntity item in results )
						{
							list.AddRange( Entity_CredentialManager.GetAll( item.RowId, BaseFactory.RELATIONSHIP_TYPE_HAS_PART ) );
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetAllCredentails" );
			}
			return list;
		}//
		/// <summary>
		/// get all assessments related to condition profiles for the parent entity
		/// Current use is for onclick of a gray box in the search results
		/// </summary>
		/// <param name="parentUid"></param>
		/// <returns></returns>
		public static List<AssessmentProfile> GetAllAssessments( int topParentTypeId, int topParentEntityBaseId )
		{
			var list = new List<AssessmentProfile>();
			Entity parent = EntityManager.GetEntity( topParentTypeId, topParentEntityBaseId );
			//Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				return list;
			}

			try
			{
				using ( var context = new EntityContext() )
				{
					List<DBEntity> results = context.Entity_ConditionProfile
							.Where( s => s.EntityId == parent.Id )
							.OrderBy( s => s.ConnectionTypeId )
							.ThenBy( s => s.Created )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( DBEntity item in results )
						{
							if ( item.ConnectionTypeId < 3 )
							{
								list.AddRange( Entity_AssessmentManager.GetAll( item.RowId, BaseFactory.RELATIONSHIP_TYPE_HAS_PART ) );
							}
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetAllAssessments" );
			}
			return list;
		}//
		/// <summary>
		/// get all LearningOpportunities related to condition profiles for the parent entity
		/// Current use is for onclick of a gray box in the search results
		/// </summary>
		/// <param name="parentUid"></param>
		/// <returns></returns>
		public static List<LearningOpportunityProfile> GetAllLearningOpportunities( int topParentTypeId, int topParentEntityBaseId )
		{
			var list = new List<LearningOpportunityProfile>();
			Entity parent = EntityManager.GetEntity( topParentTypeId, topParentEntityBaseId );
			//Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				return list;
			}

			try
			{
				using ( var context = new EntityContext() )
				{
					List<DBEntity> results = context.Entity_ConditionProfile
							.Where( s => s.EntityId == parent.Id )
							.OrderBy( s => s.ConnectionTypeId )
							.ThenBy( s => s.Created )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( DBEntity item in results )
						{
							list.AddRange( Entity_LearningOpportunityManager.TargetResource_GetAll( item.RowId, true ) );
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetAllLearningOpportunities" );
			}
			return list;
		}//

		/// <summary>
		/// Will only return the base condition. Targets are not retrieved as this method is typically called from a target, and don't want infinite loop.
		/// Setting the ConditionType to be the reverse, and set default name/desc.
		/// Will also check if the reverse entity is already in the connections
		/// </summary>
		/// <param name="rowId"></param>
		/// <returns></returns>
		public static ThisResource GetAs_IsPartOf(Guid rowId, string currentParentName, List<ThisResource> existingConnections )
		{
			ThisResource entity = new ThisResource();
            using (var context = new EntityContext())
            {
                DBEntity efEntity = context.Entity_ConditionProfile
                        .FirstOrDefault( s => s.RowId == rowId );

                if (efEntity != null && efEntity.Id > 0)
                {
					bool addToConnections = true;

					MapFromDB_Basics( efEntity, entity, true );
					var relatedName = string.Empty;
					var relatedEntityType = string.Empty;
					if ( efEntity.Entity != null )
					{
						if ( efEntity.Entity.EntityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL )
						{
							entity.ParentCredential = CredentialManager.GetBasic( ( int )efEntity.Entity.EntityBaseId );
							relatedName = entity.ParentCredential.Name;
							entity.TargetCredential.Add( entity.ParentCredential );
							relatedEntityType = " credential ";
							foreach ( var item in existingConnections )
							{
								var exists = item.TargetCredential.Where( s => s.Id == entity.ParentCredential.Id ).ToList();
								if ( exists != null && exists.Any() )
								{
									addToConnections = false;
									break;
								}
							}
						}
						else if ( efEntity.Entity.EntityTypeId == CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE )
						{
							entity.ParentAssessment = AssessmentManager.GetBasic( ( int )efEntity.Entity.EntityBaseId );
							relatedName = entity.ParentAssessment.Name;
							entity.TargetAssessment.Add( entity.ParentAssessment );
							relatedEntityType = " assessment ";
							foreach ( var item in existingConnections )
							{
								var exists = item.TargetAssessment.Where( s => s.Id == entity.ParentAssessment.Id ).ToList();
								if ( exists != null && exists.Any() )
								{
									addToConnections = false;
									break;
								}
							}
						}
						else if ( efEntity.Entity.EntityTypeId == CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE )
						{
							entity.ParentLearningOpportunity = LearningOpportunityManager.GetBasic( ( int )efEntity.Entity.EntityBaseId );
							relatedName = entity.ParentLearningOpportunity.Name;
							entity.TargetLearningOpportunity.Add( entity.ParentLearningOpportunity );
							relatedEntityType = " learning opportunity ";
							foreach ( var item in existingConnections )
							{
								var exists = item.TargetLearningOpportunity.Where( s => s.Id == entity.ParentLearningOpportunity.Id ).ToList();
								if ( exists != null && exists.Any() )
								{
									addToConnections = false;
									break;
								}
							}
						}
						else if ( efEntity.Entity.EntityTypeId == CodesManager.ENTITY_TYPE_CONDITION_MANIFEST )
						{
							entity.ParentConditionManifest = ConditionManifestManager.GetBasic( ( int )efEntity.Entity.EntityBaseId );
							relatedName = entity.ParentConditionManifest.Name;
							relatedEntityType = "ConditionManifest";
						}
						//generally may not care about target resources, as getting this because caller is a target resource - could do a duration check and get anyway.
						//IncludeTargetResources( efEntity, entity, false, true );
						//
						
						SetInversionConditionType( entity, currentParentName, relatedName, relatedEntityType );
						if ( addToConnections )
							existingConnections.Add( entity );
					}
				}
			}         

			return entity;
		}
		/// <summary>
		/// Set inverse condition profile
		/// Example From:
		/// - Credential requires Lopp
		/// To
		/// - Lopp is required for Credential
		/// </summary>
		/// <param name="input">Condition Profile</param>
		/// <param name="relatedName"></param>
		/// <param name="relatedEntityType"></param>
		private static void SetInversionConditionType( ThisResource input, string currentParentName, string relatedName, string relatedEntityType )
		{
			switch ( input.ConnectionProfileTypeId)
			{
				case 0:
					input.ConnectionProfileTypeId = 1;
					break;
				case 1:
					input.ConnectionProfileTypeId = ConnectionProfileType_NextIsRequiredFor;//3
					break;
				case 3:
					input.ConnectionProfileTypeId = ConnectionProfileType_Requirement;//1
					break;
				case 2:
					input.ConnectionProfileTypeId = ConnectionProfileType_NextIsRecommendedFor;//4
					break;
				case 4:
					input.ConnectionProfileTypeId = ConnectionProfileType_Recommendation;//2
					break;
				case 6:
					input.ConnectionProfileTypeId = ConnectionProfileType_AdvancedStandingFrom;
					break;
				case 7:
					input.ConnectionProfileTypeId = ConnectionProfileType_AdvancedStandingFor;
					break;
				case 8:
					input.ConnectionProfileTypeId = ConnectionProfileType_PreparationFrom;//9
					break;
				case 9:
					input.ConnectionProfileTypeId = ConnectionProfileType_PreparationFor;//8
					break;
			}
			input.ConnectionProfileType=GetConditionType( input.ConnectionProfileTypeId );
			//input.Name = !string.IsNullOrWhiteSpace( input.Name ) ? input.Name : string.Format("{0} '{1}'", input.ConnectionProfileType, relatedName);
			input.Name = string.Format( "Other requirements for '{0}'.", currentParentName );
			input.Description = string.Format( "'{0}' '{1}' {2} '{3}'", currentParentName, input.ConnectionProfileType, relatedEntityType, relatedName );
		}
		private static void MapToDB( ThisResource input, DBEntity output )
		{

			//want output ensure fields input create are not wiped
			if ( output.Id < 1 )
			{
				output.ConnectionTypeId = input.ConnectionProfileTypeId;
				//we may not get the subtype back on update, so only set if > 0, otherwise leave as is???
				if ( input.ConditionSubTypeId > 0 )
				{
					output.ConditionSubTypeId = input.ConditionSubTypeId;
				}
				else
				{
					if ( ( output.ConditionSubTypeId ?? 0 ) == 0 )
					{
						//not sure we need specific conditionSub types for cred, asmt, and lopp as we know the parent
						if ( input.ConnectionProfileType == "CredentialConnections" )
							output.ConditionSubTypeId = ConditionSubType_CredentialConnection;
						else if ( input.ConnectionProfileType == "AssessmentsConnections" )
							output.ConditionSubTypeId = ConditionSubType_Assessment;
						else if ( input.ConnectionProfileType == "LearningOppConnections" )
							output.ConditionSubTypeId = ConditionSubType_LearningOpportunity;
						else if ( input.ConnectionProfileType == "AlternativeCondition" )
							output.ConditionSubTypeId = ConditionSubType_Alternative;
						else if ( input.ConnectionProfileType == "AdditionalCondition" )
							output.ConditionSubTypeId = ConditionSubType_Additional;
						else
							output.ConditionSubTypeId = 1;
					}
				}

				output.EntityId = input.RelatedEntityId;
			} else
			{
				if ( input.ConnectionProfileTypeId > 0 )
					output.ConnectionTypeId = input.ConnectionProfileTypeId;
				else if ( output.ConnectionTypeId < 1 )
					output.ConnectionTypeId = 1;

				//ConditionSubTypeId should be left as is input ADD
			}

			output.Id = input.Id;


			//170316 mparsons - ProfileSummary is used in the edit interface for Name
			if ( string.IsNullOrWhiteSpace( input.ProfileName ) )
				input.ProfileName = input.ProfileSummary ?? string.Empty;

			//check for wierd jquery addition
			int pos2 = input.ProfileName.ToLower().IndexOf( "jquery" );
			if ( pos2 > 1 )
			{
				input.ProfileName = input.ProfileName.Substring( 0, pos2 );
			}

			//check for <span class=
			pos2 = input.ProfileName.ToLower().IndexOf( "</span>" );
			if ( input.ProfileName.ToLower().IndexOf( "</span>" ) > -1 )
			{
				input.ProfileName = input.ProfileName.Substring( pos2 + 7 );
			}

			output.Name = GetData( input.ProfileName );
			output.Description = GetData( input.Description );
			output.SubmissionOfDescription = GetData( input.SubmissionOfDescription );

			//if ( input.AssertedByAgent != null && input.AssertedByAgent .Count > 0)
   //         {
			//	//just handling one
   //         }
			if ( input.AssertedByAgentUid == null || input.AssertedByAgentUid.ToString() == DEFAULT_GUID )
			{
				output.AgentUid = null;//			
			}
			else
			{
				output.AgentUid = input.AssertedByAgentUid;
			}

			output.Experience = GetData( input.Experience );
			output.SubjectWebpage = input.SubjectWebpage;

			if ( input.MinimumAge > 0 )
				output.MinimumAge = input.MinimumAge;
			else
				output.MinimumAge = null;
			if ( input.YearsOfExperience > 0 )
				output.YearsOfExperience = input.YearsOfExperience;
			else
				output.YearsOfExperience = null;
			if ( input.Weight > 0 )
				output.Weight = input.Weight;
			else
				output.Weight = null;
			//======================================================================
			// can have just CreditUnitTypeDescription.Will need a policy if both are found?
			//	-possibly create a second CreditValue?
			output.CreditUnitTypeDescription = null;
			if ( !string.IsNullOrWhiteSpace( input.CreditUnitTypeDescription ) )
				output.CreditUnitTypeDescription = input.CreditUnitTypeDescription;

			//21-03-23 - now using ValueProfile
			output.CreditValue = string.IsNullOrWhiteSpace( input.CreditValueJson ) ? null : input.CreditValueJson;

			

			//output.CreditValueJson = null;

			//else if ( UtilityManager.GetAppKeyValue( "usingQuantitiveValue", false ) == false )
			//{

			//	//output.CreditHourType = GetData( input.CreditHourType, null );
			//	//output.CreditHourValue = SetData( input.CreditHourValue, 0.5M );
			//	//output.CreditUnitTypeId = SetData( input.CreditUnitTypeId, 1 );
			//	if ( input.CreditUnitType != null && input.CreditUnitType.HasItems() )
			//	{
			//		//get Id if available
			//		EnumeratedItem item = input.CreditUnitType.GetFirstItem();
			//		if ( item != null && item.Id > 0 )
			//			output.CreditUnitTypeId = item.Id;
			//		else
			//		{
			//			//if not get by schema
			//			CodeItem code = CodesManager.GetPropertyBySchema( "ceterms:CreditUnit", item.SchemaName );
			//			output.CreditUnitTypeId = code.Id;
			//		}
			//	}
			//	output.CreditUnitTypeDescription = GetData( input.CreditUnitTypeDescription );
			//	output.CreditUnitValue = SetData( input.CreditUnitValue, 0.5M );
			//}

			if (IsValidDate(input.DateEffective))
				output.DateEffective = DateTime.Parse(input.DateEffective);
			else
				output.DateEffective = null;

		}

		public static void MapFromDB(DBEntity input, ThisResource output
				, bool includingProperties
				, bool incudingResources
				, bool isForCredentialDetails
				, bool getMinimumOnly = false //will be true for link checker
			) 
		{
			MapFromDB_Basics( input, output, isForCredentialDetails );
			output.EstimatedCosts = CostProfileManager.GetAll( output.RowId );

			if ( getMinimumOnly )
				return;
			//
			output.CommonCosts = Entity_CommonCostManager.GetAll( output.RowId );
			
			//========================================================
			//TODO - determine what is really needed for the detail page for conditions

			output.Experience = input.Experience;
			output.MinimumAge = GetField(input.MinimumAge, 0);
			output.YearsOfExperience = GetField(input.YearsOfExperience, 0m);
			output.Weight = GetField( input.Weight, 0m );

			//=========================================================
			//populate CreditValue
			//TODO - chg to use JSON


			if ( !string.IsNullOrWhiteSpace( input.CreditValue ) && input.CreditValue != "[]" )
			{
				output.CreditValueList = JsonConvert.DeserializeObject<List<ValueProfile>>( input.CreditValue );

				if ( output.CreditValueList != null && output.CreditValueList.Any() )
				{
					/* AVOID THIS HACK MOVING FORWARD, USE THE LIST*/

				}
			} 

			output.CreditUnitTypeDescription = input.CreditUnitTypeDescription;

			//======================================================================

			if ( IsValidDate(input.DateEffective))
				output.DateEffective = ((DateTime)input.DateEffective).ToString("yyyy-MM-dd");
			else
				output.DateEffective = string.Empty;
			

			output.Condition = Entity_ReferenceManager.GetAll(output.RowId, CodesManager.PROPERTY_CATEGORY_CONDITION_ITEM);

			output.SubmissionOf = Entity_ReferenceManager.GetAll( output.RowId, CodesManager.PROPERTY_CATEGORY_SUBMISSION_ITEM );
			output.SubmissionOfDescription = input.SubmissionOfDescription;
			//output.RequiresCompetenciesFrameworks = Entity_CompetencyFrameworkManager.GetAll( output.RowId, "requires" );
			var frameworksList = new Dictionary<string, RegistryImport>();
            output.RequiresCompetenciesFrameworks = Entity_CompetencyManager.GetAllAs_CAOFramework( output.RowId, "Requires", ref frameworksList);
            if ( output.RequiresCompetenciesFrameworks.Count > 0 )
            {
                output.HasCompetencies = true;
                output.FrameworkPayloads = frameworksList;
            }            

			if (includingProperties)
			{
				output.AudienceLevel = EntityPropertyManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL );
				output.ApplicableAudienceType = EntityPropertyManager.FillEnumeration(output.RowId, CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE);
				//
				output.Jurisdiction = Entity_JurisdictionProfileManager.Jurisdiction_GetAll(output.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_SCOPE );

				output.ResidentOf = Entity_JurisdictionProfileManager.Jurisdiction_GetAll(output.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_RESIDENT);

			}

			if (incudingResources)
			{
				//if for the detail page, want output include more info, but not all
				output.TargetCredential = Entity_CredentialManager.GetAll( output.RowId, BaseFactory.RELATIONSHIP_TYPE_HAS_PART, isForCredentialDetails );

				//assessment
				//for entity.condition(ec) - entity = ec.rowId
				output.TargetAssessment = Entity_AssessmentManager.GetAll(output.RowId, BaseFactory.RELATIONSHIP_TYPE_HAS_PART, true, true );
				foreach ( AssessmentProfile ap in output.TargetAssessment )
					
				{
					if ( ap.HasCompetencies || ap.ChildHasCompetencies )
					{
						output.ChildHasCompetencies = true;
						break;
					}
				}	

				output.TargetLearningOpportunity = Entity_LearningOpportunityManager.TargetResource_GetAll( output.RowId, false, isForCredentialDetails );
				foreach (LearningOpportunityProfile e in output.TargetLearningOpportunity)
				{
					if (e.HasCompetencies || e.ChildHasCompetencies)
					{
						output.ChildHasCompetencies = true;
						break;
					}
				}

                output.TargetOccupation = Entity_OccupationManager.TargetResource_GetAll( output.RowId, false );

                output.TargetJob = Entity_JobManager.TargetResource_GetAll( output.RowId, false );

            }
        }
		
		public static void MapFromDB_Basics( DBEntity from, ThisResource to, bool isForCredentialDetails )
		{
			to.Id = from.Id;
			to.RowId = from.RowId;
			to.Description = from.Description;
			if ( IsValidDate( from.Created ) )
				to.Created = ( DateTime ) from.Created;
			
			if ( IsValidDate( from.LastUpdated ) )
				to.LastUpdated = ( DateTime ) from.LastUpdated;

			to.RelatedEntityId = from.EntityId;
			if ( IsGuidValid( from.AgentUid ) )
			{
				to.AssertedByAgentUid = ( Guid ) from.AgentUid;
			}
			else
			{
				//attempt to get from parent?
				//22-08-30 - NO, only show what was published
				//if ( from.Entity != null  )
				//{
				//	if ( from.Entity.EntityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL )
				//	{
				//		Credential cred = CredentialManager.GetBasic( from.Entity.EntityUid );
				//		to.AssertedByAgentUid = cred.OwningAgentUid;
				//	}
				//	//else if ( from.Entity.EntityTypeId == CodesManager.ENTITY_TYPE_CONDITION_PROFILE )
				//	//{
				//	//	MN.ProfileLink cp = GetProfileLink( from.Entity.EntityUid );
				//	//	to.AssertedByAgentUid = cp.OwningAgentUid;
				//	//}
				//}
			}
			if ( IsGuidValid( to.AssertedByAgentUid ) )
			{
				//TODO - get org and then create profile link
				//to.AssertedByOrgProfileLink = OrganizationManager.Agent_GetProfileLink( to.AssertedByAgentUid );

				to.AssertedBy = OrganizationManager.GetBasics( to.AssertedByAgentUid );

				//to.AssertedByOrgProfileLink = new Models.Node.ProfileLink()
				//{
				//	RowId = to.AssertedBy.RowId,
				//	Id = to.AssertedBy.Id,
				//	Name = to.AssertedBy.Name,
				//	Type = typeof( Models.Node.Organization )
				//};

			}

			to.ConnectionProfileTypeId = ( int ) from.ConnectionTypeId;
			to.ConditionSubTypeId = GetField( from.ConditionSubTypeId, 1 );
			//todo reset to.ConnectionProfileTypeId if after a starter profile
			if ( to.ConditionSubTypeId >= ConditionSubType_CredentialConnection )
			{
				//if ( to.Created == to.LastUpdated )
				//{
				//	//reset as was an auto created, so allow use to set type
				//	to.ConnectionProfileTypeId = 0;
				//}
			}

			to.SubjectWebpage = from.SubjectWebpage;

			string parentName = string.Empty;
			if ( from.Entity != null && from.Entity.EntityTypeId == 1 )
				parentName = from.Entity.EntityBaseName;
			if ( to.ConnectionProfileTypeId > 0)
				to.ConnectionProfileType = GetConditionType(to.ConnectionProfileTypeId);

			//TODO - need to have a default for a missing name
			//17-03-16 mparsons - using ProfileName for the list view, and ProfileSummary for the edit view
			if ( ( from.Name ?? string.Empty ).Length > 0 )
			{
				//note could have previously had a name, and no longer shown!
				to.ProfileName = from.Name;
			}
			else 
			{
				to.ProfileName = parentName ;
			}

			to.ProfileSummary = to.ProfileName;

			if ( to.ConditionSubTypeId == ConditionSubType_CredentialConnection )
			{
				List<Credential> list = Entity_CredentialManager.GetAll( to.RowId, BaseFactory.RELATIONSHIP_TYPE_HAS_PART );
				if ( list.Count > 0 )
				{
					to.ProfileName = list[ 0 ].Name;
					if ( list.Count > 1 )
					{
						to.ProfileName += string.Format(" [plus {0} other(s)] ", list.Count-1);
					}
					//if ( to.AssertedByOrgProfileLink != null && !string.IsNullOrWhiteSpace(to.AssertedByOrgProfileLink.Name ) )
					//{
					//	to.ProfileName += " ( " + to.AssertedByOrgProfileLink.Name + " ) ";
					//}
				}
			}

			//if ( to.ConditionSubTypeId == ConditionSubType_Alternative
			//	&& forEditView 
			//	&& to.ProfileName.ToLower().IndexOf( "<span class=" ) == -1 )
			//	to.ProfileName = string.Format( "<span class='alternativeCondition'>ALTERNATIVE&nbsp;</span>{0}", to.ProfileName );
			//else if ( to.ConditionSubTypeId == ConditionSubType_Additional
			//	&& forEditView 
			//	&& to.ProfileName.ToLower().IndexOf( "<span class=" ) == -1 )
			//	to.ProfileName = string.Format( "<span class='additionalCondition'>ADDITIONAL&nbsp;</span>{0}", to.ProfileName );


			PopulateSubconditions( to, isForCredentialDetails );
		} //
	

		public int GetConditionTypeId( string conditionType )
		{
			int conditionTypeId = 0;
			switch ( conditionType.ToLower() )
			{
				case "requires":
					conditionTypeId = ConnectionProfileType_Requirement;
					break;
			
				case "recommends":
					conditionTypeId = ConnectionProfileType_Recommendation;
					break;

				case "isrequiredfor":
					conditionTypeId = ConnectionProfileType_NextIsRequiredFor;
					break;

				case "isrecommendedfor":
					conditionTypeId = ConnectionProfileType_NextIsRecommendedFor;
					break;

				case "renewal":
					conditionTypeId = ConnectionProfileType_Renewal;
					break;
				case "advancedstandingfor":
					conditionTypeId = ConnectionProfileType_AdvancedStandingFor;
					break;
				case "advancedstandingfrom":
					conditionTypeId = ConnectionProfileType_AdvancedStandingFrom;
					break;
				case "ispreparationfor":
					conditionTypeId = ConnectionProfileType_PreparationFor;
					break;
				case "preparationfrom":
					conditionTypeId = ConnectionProfileType_PreparationFrom;
					break;
				case "corequisite":
					conditionTypeId = Entity_ConditionProfileManager.ConnectionProfileType_Corequisite;
					break;
				case "entrycondition":
					conditionTypeId = Entity_ConditionProfileManager.ConnectionProfileType_EntryCondition;
					break;
				case "coprerequisite":
					conditionTypeId = Entity_ConditionProfileManager.ConnectionProfileType_CoPrerequisite;
					break;
                case "supportservicecondition":
                case "supportservice":
                    conditionTypeId = ConnectionProfileType_SupportServiceCondition;
                    break;
                //
                default:
					conditionTypeId = 1;
					break;
			}
			return conditionTypeId;
		} //
		public static string GetConditionType(int conditionTypeId)
		{

			string ctype = string.Empty;
			switch ( conditionTypeId )
			{
				case 1:
					ctype= "Requires";
					break;
				case 2:
					ctype = "Recommendation";
					break;
				case 3:
					ctype = "Is Required For";
					break;
				case 4:
					ctype = "Is Recommended For";
					break;
				case 5:
					ctype = "Renewal Requirements";
					break;
				case 6:
					ctype = "Is Advanced Standing For";
					break;
				case 7:
					ctype = "Advanced Standing From";
					break;
				case 8:
					ctype = "Is Preparation For";
					break;
				case 9:
					ctype = "Is Preparation From";
					break;
				case 10:
					ctype = "Corequisite";
					break;
				case 11:
					ctype = "Entry Condition";
					break;
				case 15:
					ctype = "CoPrerequisite";
					break;
                case 16:
                    ctype = "Support Service Condition";
                    break;
                //CredentialConnections
                default:
					conditionTypeId = 0;
					break;
			}

			return ctype;
		}
		private static void PopulateSubconditions( ThisResource to, bool isForCredentialDetails )
		{
			//alternative conditions
			//all required at this time!
			List<ConditionProfile> cpList = new List<ConditionProfile>();
			//this is wrong need to differentiate edit of condition profile versus edit view of credential
			cpList = Entity_ConditionProfileManager.GetAll( to.RowId, isForCredentialDetails );

			if ( cpList != null && cpList.Count > 0 )
			{
				foreach ( ConditionProfile item in cpList )
				{
					if ( item.ConditionSubTypeId == Entity_ConditionProfileManager.ConditionSubType_Alternative )
					{
						to.AlternativeCondition.Add( item );
					}
					//else if ( item.ConditionSubTypeId == Entity_ConditionProfileManager.ConditionSubType_Additional )
					//	to.AdditionalCondition.Add( item );
					else
					{
						EmailManager.NotifyAdmin( "Unexpected Alternative Condition Profile for a condition profile", string.Format( "ConditionProfileId: {0}, ConditionProfileTypeId: {1}, ConditionSubTypeId: {2}", to.Id, item.ConnectionProfileTypeId, item.ConditionSubTypeId ) );
						//to.AlternativeCondition.Add( item );
					}
				}
			}
		}


		/// <summary>
		/// Get all condition profiles for a credential for use on detail page. 
		/// Will need to ensure any target entities return all the necessary (but pointless) extra data.
		/// </summary>
		/// <param name="to"></param>
		public static void FillConditionProfilesForDetailDisplay( Credential to )
		{
			bool forEditView = false;

			//get entity for credential
			using ( var context = new EntityContext() )
			{
				EM.Entity dbEntity = context.Entity
						.Include( "Entity_ConditionProfile" )
						.AsNoTracking()
						.SingleOrDefault( s => s.EntityUid == to.RowId );

				if ( dbEntity != null && dbEntity.Id > 0 )
				{
					if ( dbEntity.Entity_ConditionProfile != null
				&& dbEntity.Entity_ConditionProfile.Count > 0 )
					{
						ConditionProfile entity = new ConditionProfile();
						//could use this, but need to do mapping get related data


						//to.Requires = dbEntity.Entity_ConditionProfile
						//			.Where( x => x.ConnectionTypeId == ConnectionProfileType_Requirement ) 
						//			as List<ConditionProfile>;

						var creditUnitTypeCodes = CodesManager.GetEnumeration( "creditUnit" ); //Get code table one time - NA 3/17/2017

                        //get directly, so can sort
                        List<EM.Entity_ConditionProfile> list = context.Entity_ConditionProfile
                                .Where(s => s.EntityId == dbEntity.Id)
                                .OrderBy(s => s.ConnectionTypeId).ThenBy(s => s.Name).ThenBy(s => s.Created)
                                .ToList();
                        //foreach ( EM.Entity_ConditionProfile item in dbEntity.Entity_ConditionProfile )
                        foreach (EM.Entity_ConditionProfile item in list)
                        {
							entity = new ConditionProfile();
							MapFromDB( item, entity, true, true, true );

							//Add the credit unit type enumeration with the selected item, to fix null error in publishing and probably detail - NA 3/17/2017
							entity.CreditUnitType = new Enumeration()
							{
								Items = new List<EnumeratedItem>()
							};
							entity.CreditUnitType.Items.Add( creditUnitTypeCodes.Items.FirstOrDefault( m => m.CodeId == entity.CreditUnitTypeId ) );
							//End edits - NA 3/17/2017

							if ( entity.HasCompetencies || entity.ChildHasCompetencies )
								to.ChildHasCompetencies = true;

							if ( entity.ConditionSubTypeId == ConditionSubType_CredentialConnection )
							{
								to.CredentialConnections.Add( entity );
							}
							else
							{
								//eventually will only be required or recommends here
								//may want to add logging, and notification - but should be covered via conversion
								if ( entity.ConnectionProfileTypeId == ConnectionProfileType_Requirement )
								{
									//to.Requires.Add( entity );
									//to.Requires = new List<ThisResource>();
									to.Requires= HandleSubConditions( to.Requires, entity, forEditView );
								}
								else if ( entity.ConnectionProfileTypeId == ConnectionProfileType_Recommendation )
								{ 
									//to.Recommends.Add( entity );
									to.Recommends = HandleSubConditions( to.Recommends, entity, forEditView );
								}
								else if ( entity.ConnectionProfileTypeId == ConnectionProfileType_Renewal )
								{
									to.Renewal = HandleSubConditions( to.Renewal, entity, forEditView );
								}
								else if ( entity.ConnectionProfileTypeId == ConnectionProfileType_Corequisite )
								{
									to.Corequisite.Add( entity );
								}
								else if ( entity.ConnectionProfileTypeId == ConnectionProfileType_CoPrerequisite )
								{
									to.CoPrerequisite.Add( entity );
								}
								else
								{
									EmailManager.NotifyAdmin( thisClassName + ".FillConditionProfiles. Unhandled connection type", string.Format( "Unhandled connection type of {0} was encountered for CredentialId: {1}", entity.ConnectionProfileTypeId, to.Id ) );
								}
							}
							
						}
					}
				}
				
			}
				
		}//

//		public static void FillConditionProfilesForList( Credential to, bool forEditView )
//		{
//			//get entity for credential
//			using ( var context = new EntityContext() )
//			{
//				EM.Entity dbEntity = context.Entity
//						.Include( "Entity_ConditionProfile" )
//						.AsNoTracking()
//						.SingleOrDefault( s => s.EntityUid == to.RowId );

//				if ( dbEntity != null && dbEntity.Id > 0 )
//				{
//					if ( dbEntity.Entity_ConditionProfile != null
//				&& dbEntity.Entity_ConditionProfile.Count > 0 )
//					{
//						ConditionProfile entity = new ConditionProfile();

//						foreach ( EM.Entity_ConditionProfile item in dbEntity.Entity_ConditionProfile )
//						{
//							entity = new ConditionProfile();
//							//this method is called from the edit view of credential, but here we want to set editView to true?
//							MapFromDB_Basics( item, entity, false);						

//							if ( entity.ConditionSubTypeId == ConditionSubType_CredentialConnection )
//							{
//								to.CredentialConnections.Add( entity );
//							}
//							else
//							{

//								//eventually will only be required or recommends here
//								//may want to add logging, and notification - but should be covered via conversion
//								if ( entity.ConnectionProfileTypeId == ConnectionProfileType_Requirement )
//								{
//									//to.Requires.Add( entity );
//									to.Requires= HandleSubConditions( to.Requires, entity, forEditView );
//								}
//								else if ( entity.ConnectionProfileTypeId == ConnectionProfileType_Recommendation )
//								{
//									//to.Recommends.Add( entity );
//									to.Recommends = HandleSubConditions( to.Recommends, entity, forEditView );
//								}
//								else if ( entity.ConnectionProfileTypeId == ConnectionProfileType_Renewal )
//								{
//									to.Renewal = HandleSubConditions( to.Renewal, entity, forEditView );
//								}
//								else if ( entity.ConnectionProfileTypeId == ConnectionProfileType_Corequisite )
//								{
//									to.Corequisite.Add( entity );
//								}
//								else
//								{
//									EmailManager.NotifyAdmin( thisClassName + ".FillConditionProfilesForList. Unhandled connection type", string.Format( "Unhandled connection type of {0} was encountered", entity.ConnectionProfileTypeId ) );
//									//add to required, for dev only?
//									if (IsDevEnv())
//									{
//										entity.ProfileName = ( entity.ProfileName ?? string.Empty ) + " unexpected condition type of " + entity.ConnectionProfileTypeId.ToString();
//										to.Requires.Add( entity );
//									}
//								}
//							}

//						}
//					}
//				}

//			}
//}//

		/// <summary>
		/// 21-05-19 - not sure if this is working properly. Why are alternative conditions being added to the list.
		/// Might have been useful in publisher!
		/// </summary>
		/// <param name="profiles"></param>
		/// <param name="entity"></param>
		/// <param name="forEditView"></param>
		/// <returns></returns>
		private static List<ConditionProfile> HandleSubConditions( List<ConditionProfile> profiles, ThisResource entity, bool forEditView )
		{
			profiles.Add( entity );
			//21-05-19 mp - skip the rest for now and evaluate
			return profiles;
			//List<ConditionProfile> list = new List<ThisResource>();
			//list.AddRange( profiles );

			//foreach ( ConditionProfile item in entity.AlternativeCondition )
			//{
			//	if ( IsGuidValid( entity.AssertedByAgentUid ) && !IsGuidValid( item.AssertedByAgentUid ) )
			//		item.AssertedByAgentUid = entity.AssertedByAgentUid;

			//	//if ( forEditView && entity.ProfileName.ToLower().IndexOf( "<span class=" ) == -1 )
			//	//{
			//	//	item.ProfileName = string.Format( "<span class='alternativeCondition'>ALTERNATIVE&nbsp;</span>{0}", item.ProfileName );

			//	//}

				
			//	list.Add( item );
			//}

			//return list;

		}//


		#endregion


		#region  validations
		public static bool IsParentBeingAddedAsChildToItself( int condProfId, int childId, int childEntityTypeId )
		{
			bool isOk = false;
			using ( var context = new EntityContext() )
			{

				DBEntity efEntity = context.Entity_ConditionProfile
						.SingleOrDefault( s => s.Id == condProfId );

				if ( efEntity != null && efEntity.Id > 0 && efEntity.Entity != null )
				{
					if (efEntity.Entity.EntityTypeId == childEntityTypeId
						&& efEntity.Entity.EntityBaseId == childId)
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

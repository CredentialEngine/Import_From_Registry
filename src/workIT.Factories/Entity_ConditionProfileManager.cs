using System;
using System.Collections.Generic;
using System.Linq;

using workIT.Models;
using workIT.Models.Common;
using ThisEntity = workIT.Models.ProfileModels.ConditionProfile;
using DBEntity = workIT.Data.Tables.Entity_ConditionProfile;
using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;

using workIT.Utilities;
using CM = workIT.Models.Common;
using workIT.Models.ProfileModels;
using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;

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

		public static int ConditionSubType_Basic = 1;
		public static int ConditionSubType_CredentialConnection = 2;
		public static int ConditionSubType_Assessment = 3;
		public static int ConditionSubType_LearningOpportunity = 4;
		public static int ConditionSubType_Alternative = 5;
		public static int ConditionSubType_Additional = 6;
		#endregion

		#region persistance ==================

		public bool SaveList( List<ThisEntity> list, int conditionTypeId, Guid parentUid, ref SaveStatus status, int subConnectionTypeId = 0 )
		{
			if ( list == null || list.Count == 0 )
				return true;
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

			bool isAllValid = true;
			foreach ( ThisEntity item in list )
			{
				item.ConnectionProfileTypeId = conditionTypeId;
				item.ConditionSubTypeId = subConnectionTypeId;
				Save( item, parent, ref status );
			}

			return isAllValid;
		}

		private bool Save( ThisEntity item, Entity parent,  ref SaveStatus status  )
		{
			bool isValid = true;
			
			item.ParentId = parent.Id;

			int profileTypeId = 0;
			
			if ( item.ConnectionProfileTypeId > 0 )
				profileTypeId = item.ConnectionProfileTypeId;
			else
			{
				profileTypeId = GetConditionTypeId( item.ConnectionProfileType );
			}

			using (var context = new EntityContext())
			{
				if (!ValidateProfile(item, ref status))
				{
					return false;
				}

				item.ConnectionProfileTypeId = profileTypeId;
				//should always be add if always resetting the entity
				if (item.Id > 0)
				{
					DBEntity p = context.Entity_ConditionProfile
							.FirstOrDefault(s => s.Id == item.Id);
					if (p != null && p.Id > 0)
					{
						item.RowId = p.RowId;
						item.ParentId = p.EntityId;
						MapToDB(item, p);

						if (HasStateChanged(context))
						{
							p.LastUpdated = System.DateTime.Now;
							context.SaveChanges();
						}
						//regardless, check parts
						isValid = UpdateParts( item, ref status );
					}
					else
					{
						//error should have been found
						isValid = false;
						status.AddWarning(string.Format("Error: the requested record was not found: recordId: {0}", item.Id));
					}
				}
				else
				{
					int newId = Add( item, ref status );
					if ( newId == 0 || status.HasErrors )
						isValid = false;
				}
			}
			return isValid;
		}


		/// <summary>
		/// add a ConditionProfile
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		private int Add(ThisEntity entity, ref SaveStatus status)
		{
			DBEntity efEntity = new DBEntity();
			using (var context = new EntityContext())
			{
				try
				{
					MapToDB(entity, efEntity);

					efEntity.EntityId = entity.ParentId;
                    if ( IsValidGuid( entity.RowId ) )
                        efEntity.RowId = entity.RowId;
                    else
                        efEntity.RowId = Guid.NewGuid();
                    efEntity.Created = efEntity.LastUpdated = System.DateTime.Now;
					
					context.Entity_ConditionProfile.Add(efEntity);

					// submit the change to database
					int count = context.SaveChanges();
					if (count > 0)
					{
						entity.Id = efEntity.Id;
						entity.RowId = efEntity.RowId;

						UpdateParts(entity, ref status);

						return efEntity.Id;
					}
					else
					{
						//?no info on error
						status.AddWarning("Error - the profile was not saved. ");
						string message = string.Format("{0}.Add() Failed", "Attempted to add a ConditionProfile. The process appeared to not work, but was not an exception, so we have no message, or no clue. ConditionProfile. EntityId: {1}, createdById: {2}", thisClassName, entity.ParentId, entity.CreatedById);
						EmailManager.NotifyAdmin(thisClassName + ".Add() Failed", message);
					}
				}
				catch (System.Data.Entity.Validation.DbEntityValidationException dbex)
				{
					string message = HandleDBValidationError( dbex, "Entity_ConditionProfileManager.Add()", string.Format( "EntityId: 0 , ConnectionProfileTypeId: {1}  ", entity.ParentId, entity.ConnectionProfileTypeId ) );
					status.AddWarning( message );
					
				}
				catch (Exception ex)
				{
					LoggingHelper.LogError(ex, thisClassName + string.Format(".Add(), EntityId: {0}", entity.ParentId));
				}
			}

			return efEntity.Id;
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
                context.Entity_ConditionProfile.RemoveRange( context.Entity_ConditionProfile.Where( s => s.EntityId == parent.Id ) );
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
        public bool UpdateParts(ThisEntity entity, ref SaveStatus status)
		{
			bool isAllValid = true;

			//Alternative conditions
			if (entity.AlternativeCondition != null && entity.AlternativeCondition.Count > 0)
			{
				Entity parent = EntityManager.GetEntity( entity.RowId );
				foreach ( ThisEntity item in entity.AlternativeCondition )
				{
					item.ConnectionProfileTypeId = ConnectionProfileType_Requirement;
					item.ConditionSubTypeId = ConditionSubType_Alternative;
					Save( item, parent, ref status );
				}
			}

			//CostProfile
			CostProfileManager cpm = new Factories.CostProfileManager();
			cpm.SaveList( entity.EstimatedCosts, entity.RowId, ref status );

			EntityPropertyManager mgr = new EntityPropertyManager();
			if ( mgr.AddProperties( entity.AudienceLevel, entity.RowId, CodesManager.ENTITY_TYPE_CONDITION_PROFILE, CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL, false, ref status ) == false )
				isAllValid = false;

			if ( mgr.AddProperties( entity.ApplicableAudienceType, entity.RowId, CodesManager.ENTITY_TYPE_CONDITION_PROFILE, CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE, false, ref status ) == false )
				isAllValid = false;


			Entity_ReferenceManager erm = new Entity_ReferenceManager();


			if (erm.Add(entity.Condition, entity.RowId, CodesManager.ENTITY_TYPE_CONDITION_PROFILE,  ref status, CodesManager.PROPERTY_CATEGORY_CONDITION_ITEM, false) == false)
				isAllValid = false;

			if ( erm.Add( entity.SubmissionOf, entity.RowId, CodesManager.ENTITY_TYPE_CONDITION_PROFILE,  ref status, CodesManager.PROPERTY_CATEGORY_SUBMISSION_ITEM, false ) == false )
				isAllValid = false;
			//
			if ( HandleTargets( entity, ref status ) == false )
				isAllValid = false;

			//JurisdictionProfile 
			Entity_JurisdictionProfileManager jpm = new Entity_JurisdictionProfileManager();
			jpm.SaveList( entity.Jurisdiction, entity.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_SCOPE, ref status );

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
					ecm.Add( entity.RowId, id, ref newId, ref status );
				}
			}

			if ( entity.TargetAssessmentIds != null && entity.TargetAssessmentIds.Count > 0)
			{
				Entity_AssessmentManager eam = new Entity_AssessmentManager();
				foreach ( int id in entity.TargetAssessmentIds )
				{
					newId = eam.Add( entity.RowId, id, true, ref status );
				}
			}

			if ( entity.TargetLearningOpportunityIds != null && entity.TargetLearningOpportunityIds.Count > 0 )
			{
				Entity_LearningOpportunityManager elm = new Entity_LearningOpportunityManager();
				foreach ( int id in entity.TargetLearningOpportunityIds )
				{
					LoggingHelper.DoTrace( 6, thisClassName + string.Format( ".HandleTargets. entity.ParentId: {0}, processing loppId: {1}, entity.RowId: {2}", entity.ParentId,  id, entity.RowId.ToString()) );
					newId = elm.Add( entity.RowId, id, true, ref status );
				}
			}

			if ( entity.TargetCompetencies != null && entity.TargetCompetencies.Count > 0 )
			{
				Entity_CompetencyManager ecm = new Entity_CompetencyManager();
				ecm.SaveList( entity.TargetCompetencies, entity.RowId, ref status );
				//foreach ( int id in entity.TargetCompetencies )
				//{
				//	newId = ecm.Add( entity.RowId, id, true, ref status );
				//}
			}
			return !status.HasSectionErrors;
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

		private bool ValidateProfile( ThisEntity item, ref SaveStatus status)
		{
			status.HasSectionErrors = false;
			bool isNameRequired = true;
			
			string firstEntityName = "";

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
			//if ( item.CreditHourValue > 0 || ( item.CreditHourType ?? "" ).Length > 0 )
			//	hasCreditHourData = true;
			if (  item.CreditUnitTypeId > 0
				|| (item.CreditUnitTypeDescription ?? "").Length > 0
				|| item.CreditUnitValue > 0)
				hasCreditUnitData = true;

			if ( hasCreditHourData && hasCreditUnitData )
				status.AddWarning( "Error: Data can be entered for Credit Hour related properties or Credit Unit related properties, but not for both." );

			return !status.HasSectionErrors;
		}
		#endregion

		#region == Retrieval =======================

		

		/// <summary>
		///Get all condition profiles for parent 
		/// For this method, the parent is resonsible for assigning to the proper condition profile types, if more than one expected.
		/// </summary>
		/// <param name="parentUid"></param>
		/// <returns></returns>
		public static List<ThisEntity> GetAll( Guid parentUid, bool isForCredentialDetail, bool getMinimumOnly = false )
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
							entity = new ThisEntity();
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
		

		
		public static ThisEntity GetAs_IsPartOf(Guid rowId)
		{
			ThisEntity entity = new ThisEntity();
            using (var context = new EntityContext())
            {

                DBEntity efEntity = context.Entity_ConditionProfile
                        .FirstOrDefault( s => s.RowId == rowId );

                if (efEntity != null && efEntity.Id > 0)
                {
                    MapFromDB_Basics( efEntity, entity, true );
                    if (efEntity.Entity.EntityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL)
                    {
                        entity.ParentCredential = CredentialManager.GetBasic( ( int ) efEntity.Entity.EntityBaseId );
                    }
                    else if (efEntity.Entity.EntityTypeId == CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE)
                    {
                        entity.ParentAssessment = AssessmentManager.GetBasic( ( int ) efEntity.Entity.EntityBaseId );

                    }
                    else if (efEntity.Entity.EntityTypeId == CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE)
                    {
                        entity.ParentLearningOpportunity = LearningOpportunityManager.GetBasic( ( int ) efEntity.Entity.EntityBaseId );
                    }
                    else if (efEntity.Entity.EntityTypeId == CodesManager.ENTITY_TYPE_CONDITION_MANIFEST)
                    {
                        entity.ParentConditionManifest = ConditionManifestManager.GetBasic( ( int ) efEntity.Entity.EntityBaseId );
                    }
                    //generally may not care about targert resources, as getting this because caller is a target resource - could do a duration check and get anyway.
                    //IncludeTargetResources( efEntity, entity, false, true );
                }
            }
         

			return entity;
		}
		private static void MapToDB(ThisEntity input, DBEntity output)
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
					if ( (output.ConditionSubTypeId ?? 0) == 0 )
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

				output.EntityId = input.ParentId;
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
				input.ProfileName = input.ProfileSummary ?? "";
			
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

			if (input.AssertedByAgentUid == null || input.AssertedByAgentUid.ToString() == DEFAULT_GUID)
			{
				output.AgentUid = null;//			
			}
			else
			{
				output.AgentUid = input.AssertedByAgentUid;
			}

			output.Experience = GetData(input.Experience);
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
			if ( input.CreditValue.HasData() )
			{
				if ( input.CreditValue.CreditUnitType != null && input.CreditValue.CreditUnitType.HasItems() )
				{
					//get Id if available
					EnumeratedItem item = input.CreditValue.CreditUnitType.GetFirstItem();
					if ( item != null && item.Id > 0 )
						output.CreditUnitTypeId = item.Id;
					else
					{
						//if not get by schema
						CodeItem code = CodesManager.GetPropertyBySchema( "ceterms:CreditUnit", item.SchemaName );
						output.CreditUnitTypeId = code.Id;
					}
				}
				output.CreditUnitValue = input.CreditValue.Value;
				output.CreditUnitMaxValue = input.CreditValue.MaxValue;
				if ( input.CreditValue.MaxValue > 0 )
					output.CreditUnitValue = input.CreditValue.MinValue;
				output.CreditUnitTypeDescription = input.CreditValue.Description;
			}
			else if ( UtilityManager.GetAppKeyValue( "usingQuantitiveValue", false ) == false )
			{

				//output.CreditHourType = GetData( input.CreditHourType, null );
				//output.CreditHourValue = SetData( input.CreditHourValue, 0.5M );
				//output.CreditUnitTypeId = SetData( input.CreditUnitTypeId, 1 );
				if ( input.CreditUnitType != null && input.CreditUnitType.HasItems() )
				{
					//get Id if available
					EnumeratedItem item = input.CreditUnitType.GetFirstItem();
					if ( item != null && item.Id > 0 )
						output.CreditUnitTypeId = item.Id;
					else
					{
						//if not get by schema
						CodeItem code = CodesManager.GetPropertyBySchema( "ceterms:CreditUnit", item.SchemaName );
						output.CreditUnitTypeId = code.Id;
					}
				}
				output.CreditUnitTypeDescription = GetData( input.CreditUnitTypeDescription );
				output.CreditUnitValue = SetData( input.CreditUnitValue, 0.5M );
			}

			if (IsValidDate(input.DateEffective))
				output.DateEffective = DateTime.Parse(input.DateEffective);
			else
				output.DateEffective = null;

		}

		public static void MapFromDB(DBEntity from, ThisEntity to
				, bool includingProperties
				, bool incudingResources
				, bool isForCredentialDetails
				, bool getMinimumOnly = false //will be true for link checker
			) 
		{
			MapFromDB_Basics( from, to, isForCredentialDetails );
			to.EstimatedCosts = CostProfileManager.GetAll( to.RowId );

			if ( getMinimumOnly )
				return;
			//========================================================
			//TODO - determine what is really needed for the detail page for conditions

			to.Experience = from.Experience;
			to.MinimumAge = GetField(from.MinimumAge, 0);
			to.YearsOfExperience = GetField(from.YearsOfExperience, 0m);
			to.Weight = GetField( from.Weight, 0m );

			//=========================================================
			//populate QV
			to.CreditValue = FormatQuantitiveValue( to.CreditUnitTypeId, to.CreditUnitValue, to.CreditUnitMaxValue, to.CreditUnitTypeDescription );
			if ( to.CreditValue.HasData() )
			{
				to.CreditUnitType = to.CreditValue.CreditUnitType;
				to.CreditUnitTypeId = ( from.CreditUnitTypeId ?? 0 );
				to.CreditUnitTypeDescription = to.CreditValue.Description;

				to.CreditUnitValue = to.CreditValue.Value;
				to.CreditUnitMaxValue = to.CreditValue.MaxValue;
			}
			else
			{
				//check for old
				to.CreditUnitTypeId = ( from.CreditUnitTypeId ?? 0 );
				to.CreditUnitTypeDescription = from.CreditUnitTypeDescription;
				to.CreditUnitValue = from.CreditUnitValue ?? 0M;
				to.CreditUnitMaxValue = from.CreditUnitMaxValue ?? 0M;
				//temp handling of clock hpurs
				//to.CreditHourType = from.CreditHourType ?? "";
				//to.CreditHourValue = ( from.CreditHourValue ?? 0M );
				//if ( to.CreditHourValue > 0 )
				//{
				//	to.CreditUnitValue = to.CreditHourValue;
				//	to.CreditUnitTypeDescription = to.CreditHourType;
				//}
			}

			//======================================================================

			if ( IsValidDate(from.DateEffective))
				to.DateEffective = ((DateTime)from.DateEffective).ToShortDateString();
			else
				to.DateEffective = "";
			

			to.Condition = Entity_ReferenceManager.GetAll(to.RowId, CodesManager.PROPERTY_CATEGORY_CONDITION_ITEM);

			to.SubmissionOf = Entity_ReferenceManager.GetAll( to.RowId, CodesManager.PROPERTY_CATEGORY_SUBMISSION_ITEM );
			to.SubmissionOfDescription = from.SubmissionOfDescription;
			//to.RequiresCompetenciesFrameworks = Entity_CompetencyFrameworkManager.GetAll( to.RowId, "requires" );
			var frameworksList = new Dictionary<string, RegistryImport>();
            to.RequiresCompetenciesFrameworks = Entity_CompetencyManager.GetAllAs_CAOFramework( to.RowId, ref frameworksList);
            if ( to.RequiresCompetenciesFrameworks.Count > 0 )
            {
                to.HasCompetencies = true;
                to.FrameworkPayloads = frameworksList;
            }

            

			if (includingProperties)
			{
				to.AudienceLevel = EntityPropertyManager.FillEnumeration( to.RowId, CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL );
				to.ApplicableAudienceType = EntityPropertyManager.FillEnumeration(to.RowId, CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE);

				to.Jurisdiction = Entity_JurisdictionProfileManager.Jurisdiction_GetAll(to.RowId);

				to.ResidentOf = Entity_JurisdictionProfileManager.Jurisdiction_GetAll(to.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_RESIDENT);

			}


			if (incudingResources)
			{
				//if for the detail page, want to include more info, but not all
				to.TargetCredential = Entity_CredentialManager.GetAll( to.RowId, isForCredentialDetails );

				//assessment
				//for entity.condition(ec) - entity = ec.rowId
				to.TargetAssessment = Entity_AssessmentManager.GetAll(to.RowId );
				foreach ( AssessmentProfile ap in to.TargetAssessment )
					
				{
					if ( ap.HasCompetencies || ap.ChildHasCompetencies )
					{
						to.ChildHasCompetencies = true;
						break;
					}
				}	

				to.TargetLearningOpportunity = Entity_LearningOpportunityManager.LearningOpps_GetAll( to.RowId, false, isForCredentialDetails );
				foreach (LearningOpportunityProfile e in to.TargetLearningOpportunity)
				{
					if (e.HasCompetencies || e.ChildHasCompetencies)
					{
						to.ChildHasCompetencies = true;
						break;
					}
				}
				
			}
		}
		
		public static void MapFromDB_Basics( DBEntity from, ThisEntity to, bool isForCredentialDetails )
		{
			to.Id = from.Id;
			to.RowId = from.RowId;
			to.Description = from.Description;
			if ( IsValidDate( from.Created ) )
				to.Created = ( DateTime ) from.Created;
			
			if ( IsValidDate( from.LastUpdated ) )
				to.LastUpdated = ( DateTime ) from.LastUpdated;

			to.ParentId = from.EntityId;
			if ( IsGuidValid( from.AgentUid ) )
			{
				to.AssertedByAgentUid = ( Guid ) from.AgentUid;
			}
			else
			{
				//attempt to get from parent?
				if ( from.Entity != null  )
				{
					if ( from.Entity.EntityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL )
					{
						Credential cred = CredentialManager.GetBasic( from.Entity.EntityUid );
						to.AssertedByAgentUid = cred.OwningAgentUid;
					}
					//else if ( from.Entity.EntityTypeId == CodesManager.ENTITY_TYPE_CONDITION_PROFILE )
					//{
					//	MN.ProfileLink cp = GetProfileLink( from.Entity.EntityUid );
					//	to.AssertedByAgentUid = cp.OwningAgentUid;
					//}
				}
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

			string parentName = "";
			string conditionType = "";
			if ( from.Entity != null && from.Entity.EntityTypeId == 1 )
				parentName = from.Entity.EntityBaseName;
			if ( to.ConnectionProfileTypeId > 0)
				conditionType = GetConditionType(to.ConnectionProfileTypeId);

			//TODO - need to have a default for a missing name
			//17-03-16 mparsons - using ProfileName for the list view, and ProfileSummary for the edit view
			if ( ( from.Name ?? "" ).Length > 0 )
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
				List<Credential> list = Entity_CredentialManager.GetAll( to.RowId );
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
		public int GetSubConditionTypeId( string conditionType )
		{
			int conditionSubTypeId = 0;


			return conditionSubTypeId;
		} //

		public int GetConditionTypeId( string conditionType )
		{
			int profileTypeId = 0;
			switch ( conditionType.ToLower() )
			{
				case "requires":
					profileTypeId = ConnectionProfileType_Requirement;
					break;
			
				case "recommends":
					profileTypeId = ConnectionProfileType_Recommendation;
					break;

				case "isrequiredfor":
					profileTypeId = ConnectionProfileType_NextIsRequiredFor;
					break;

				case "isrecommendedfor":
					profileTypeId = ConnectionProfileType_NextIsRecommendedFor;
					break;

				case "renewal":
					profileTypeId = ConnectionProfileType_Renewal;
					break;
				case "advancedstandingfor":
					profileTypeId = ConnectionProfileType_AdvancedStandingFor;
					break;
				case "advancedstandingfrom":
					profileTypeId = ConnectionProfileType_AdvancedStandingFrom;
					break;
				case "ispreparationfor":
					profileTypeId = ConnectionProfileType_PreparationFor;
					break;
				case "preparationfrom":
					profileTypeId = ConnectionProfileType_PreparationFrom;
					break;
				case "corequisite":
					profileTypeId = Entity_ConditionProfileManager.ConnectionProfileType_Corequisite;
					break;
				case "entrycondition":
					profileTypeId = Entity_ConditionProfileManager.ConnectionProfileType_EntryCondition;
					break;
				//
				default:
					profileTypeId = 1;
					break;
			}
			return profileTypeId;
		} //
		public static string GetConditionType(int conditionTypeId)
		{

			string ctype = "";
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
				//CredentialConnections
				default:
					conditionTypeId = 0;
					break;
			}

			return ctype;
		}
		private static void PopulateSubconditions( ThisEntity to, bool isForCredentialDetails )
		{
			//alternative/additional conditions
			//all required at this time!
			List<ConditionProfile> cpList = new List<ConditionProfile>();
			//this is wrong need to differentiate edit of condition profile versus edit view of credential
			cpList = Entity_ConditionProfileManager.GetAll( to.RowId, isForCredentialDetails );

			if ( cpList != null && cpList.Count > 0 )
			{
				foreach ( ConditionProfile item in cpList )
				{
					if ( item.ConditionSubTypeId == Entity_ConditionProfileManager.ConditionSubType_Alternative )
						to.AlternativeCondition.Add( item );
					//else if ( item.ConditionSubTypeId == Entity_ConditionProfileManager.ConditionSubType_Additional )
					//	to.AdditionalCondition.Add( item );
					else
					{
						EmailManager.NotifyAdmin( "Unexpected Alternative/Additional Condition Profile for a condition profile", string.Format( "ConditionProfileId: {0}, ConditionProfileTypeId: {1}, ConditionSubTypeId: {2}", to.Id, item.ConnectionProfileTypeId, item.ConditionSubTypeId ) );
						to.AlternativeCondition.Add( item );
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
//										entity.ProfileName = ( entity.ProfileName ?? "" ) + " unexpected condition type of " + entity.ConnectionProfileTypeId.ToString();
//										to.Requires.Add( entity );
//									}
//								}
//							}

//						}
//					}
//				}

//			}
//}//

		private static List<ConditionProfile> HandleSubConditions( List<ConditionProfile> profiles, ThisEntity entity, bool forEditView )
		{
			profiles.Add( entity );
			List<ConditionProfile> list = profiles;

			foreach ( ConditionProfile item in entity.AlternativeCondition )
			{
				if ( IsGuidValid( entity.AssertedByAgentUid ) && !IsGuidValid( item.AssertedByAgentUid ) )
					item.AssertedByAgentUid = entity.AssertedByAgentUid;

				if ( forEditView && entity.ProfileName.ToLower().IndexOf( "<span class=" ) == -1 )
				{
					item.ProfileName = string.Format( "<span class='alternativeCondition'>ALTERNATIVE&nbsp;</span>{0}", item.ProfileName );

				}

				
				list.Add( item );
			}

			//foreach ( ConditionProfile item in entity.AdditionalCondition )
			//{
			//	if ( forEditView && entity.ProfileName.ToLower().IndexOf( "<span class=" ) == -1 )
			//	{
			//		item.ProfileName = string.Format( "<span class='additionalCondition'>ADDITIONAL&nbsp;</span>{0}", item.ProfileName );
			//	}
			//	list.Add( item );
			//}
			return list;

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

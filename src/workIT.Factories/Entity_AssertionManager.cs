﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.ProfileModels;

using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;
using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;

using workIT.Utilities;

using DBEntity = workIT.Data.Tables.Entity_Assertion;
//using DBEntitySummary = workIT.Data.Views.Entity_Assertion_Summary;
using ThisEntity = workIT.Models.Common.OrganizationAssertion;

namespace workIT.Factories
{
    public class Entity_AssertionManager : BaseFactory
    {
        static string thisClassName = "Entity_AssertionManager";

		#region Persistance
		/// <summary>
		/// Save list of assertions
		/// TODO - need to handle change of owner. The old owner is not being deleted. 
		/// </summary>
		/// <param name="parentId"></param>
		/// <param name="roleId"></param>
		/// <param name="targetUids"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public bool SaveList( int parentId, int roleId, List<Guid> targetUids, ref SaveStatus status )
        {

            if ( targetUids == null || targetUids.Count == 0 || roleId < 1 )
                return true;
            Entity parentEntity = EntityManager.GetEntity( parentId );
            Entity_AgentRelationshipManager emgr = new Entity_AgentRelationshipManager();
			var searchPendingReindexManager = new SearchPendingReindexManager();
			var messages = new List<string>();
			bool isAllValid = true;
            foreach ( Guid targetUid in targetUids )
            {
                Entity targetEntity = EntityManager.GetEntity( targetUid );

                Save( parentId, targetUid, roleId, ref status );

				//check for add to AgentRelationship, if present
				//we don't know what the target is, so can't create a pending record!!!
				//NO SHOULD NOT DO THIS, OTHERWISE HAVE DIRECT AND INDIRECT FROM ONE TRANSACTION
				//20-11-20 mp	- scenario is that QA org adds assertion, which may not exist from target perspecitive
				//				- so this does need to happen. What are the cautions?
				//				- or don't do explicitly, and have the search handle both sides!
				//21-02-02 mp - need to add all targets to be reindexed.
				if ( targetEntity.Id > 0 )
				{
					emgr.Save( targetEntity.Id, parentEntity.EntityUid, roleId, ref status );

					searchPendingReindexManager.Add( targetEntity.EntityTypeId, targetEntity.EntityBaseId, 1, ref messages );
				};
            }
            return isAllValid;
		} //

		public int Save( int parentEntityId, int entityTypeId, int targetId, int roleId, ref SaveStatus status )
		{
			//look up target Guid - need entity type
			var entity = EntityManager.GetEntity( entityTypeId, targetId );
			if ( entity != null && entity.Id > 0 )
			{
				return Save( parentEntityId, entity.EntityUid, roleId, ref status );
			} else
			{
				status.AddError( thisClassName + string.Format( ".Save() Error: invalid request - could not find entity. Please provide a valid parentEntityId: {0}, entityTypeId:{1}, targetId: {2}, and RoleId: {3}.", parentEntityId, entityTypeId, targetId, roleId ) );
				return 0;
			}
		} //

        public int Save( int entityId, Guid targetUid, int roleId, ref SaveStatus status )
        {
            int newId = 0;
            //assume if all empty, then ignore
            if ( entityId == 0 || !IsValidGuid( targetUid ) || roleId < 1 )
            {
                status.AddError( thisClassName + string.Format( ".Save() Error: invalid request, please provide a valid entityId: {0}, RoleId: {1}, and AgenUtid: {2}.", entityId, roleId, targetUid ) );
                return newId;
            }

            //TODO - update this method
            //a role could exist
            if ( EntityAssertionRoleExists( entityId, targetUid, roleId, ref newId ) )
            {
                //status.AddError( "Error: the selected relationship already exists!" );
                return newId;
            }

            EM.Entity_Assertion efEntity = new EM.Entity_Assertion();

            //TODO - need to get entity and type, otherwise set pending
            Entity targetEntity = EntityManager.GetEntity( targetUid );
            if ( targetEntity == null || targetEntity.Id == 0 )
            {
                //don't show in messages, no action possible
                //status.AddWarning( thisClassName + ".Save() Warning: the selected entity was not found yet. Need to check later. Setting to pending" );
                efEntity.IsPending = true;
                //may want to log an activity - would allow for queries, or the equivalent to the search reindex
                LoggingHelper.DoTrace( 5, thisClassName + string.Format( ".Save. The target Entity was not found, for entityId: {0}, targetUid:{1}, RoleId: {2}", entityId, targetUid, roleId ) );
                return 0;
            }
            else
            {
                //efEntity.TargetEntityTypeId = targetEntity.EntityTypeId;
				//TODO - definition of IsPending
                efEntity.IsPending = false;
            }

            using ( var context = new EntityContext() )
            {
                //add

                efEntity.EntityId = entityId;
                efEntity.TargetEntityUid = targetUid;
				efEntity.TargetEntityTypeId = targetEntity.EntityTypeId;
                efEntity.AssertionTypeId = roleId;
                efEntity.IsInverseRole = false; //should remove this, as never inverse
                efEntity.Created = System.DateTime.Now;

                context.Entity_Assertion.Add( efEntity );

                // submit the change to database
                int count = context.SaveChanges();
                newId = efEntity.Id;
				//assertions are by the current org, so a separate request to reindex should not be necessary!
            }

            return newId;
        }


        private static bool EntityAssertionRoleExists( int entityId, Guid targetEntityUid, int roleId, ref int recordId )
        {
            //EntityAgentRelationship item = new EntityAgentRelationship();
            using ( var context = new EntityContext() )
            {
                EM.Entity_Assertion entity = context.Entity_Assertion.FirstOrDefault( s => s.EntityId == entityId
                        && s.TargetEntityUid == targetEntityUid
                        && s.AssertionTypeId == roleId );
                if ( entity != null && entity.Id > 0 )
                {
					recordId = entity.Id;
                    return true;
                }
            }
            return false;
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
					var results = context.Entity_Assertion.Where( s => s.EntityId == parent.Id )
					.ToList();
					if ( results == null || results.Count == 0 )
						return true;
					foreach ( var item in results )
					{
						context.Entity_Assertion.Remove( item );
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
					//context.Entity_Assertion.RemoveRange( context.Entity_Assertion.Where( s => s.EntityId == parent.Id ) );
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
				LoggingHelper.LogError( ex, thisClassName + ".DeleteAll( Entity parent, ref SaveStatus status )" );
			}
			return isValid;
        }
        #endregion

        #region Retrieval 
        //public static List<OrganizationRoleProfile> GetAll( Guid agentUid )
        //{
        //    OrganizationRoleProfile orp = new OrganizationRoleProfile();
        //    List<OrganizationRoleProfile> list = new List<OrganizationRoleProfile>();
        //    EnumeratedItem eitem = new EnumeratedItem();

        //    Guid prevTargetUid = new Guid();
        //    Entity agentEntity = EntityManager.GetEntity( agentUid );

        //    using ( var context = new ViewContext() )
        //    {
        //        List<DBEntitySummary> agentRoles = context.Entity_Assertion_Summary
        //            .Where( s => s.AgentUid == agentUid
        //                 && s.IsQARole == true
        //                 && s.TargetEntityStateId > 1 )
        //                 .OrderBy( s => s.TargetEntityTypeId )
        //                 .ThenBy( s => s.TargetEntityName )
        //                 .ThenBy( s => s.AgentToSourceRelationship )
        //            .ToList();

        //        foreach ( DBEntitySummary entity in agentRoles )
        //        {
        //            //loop until change in entity type?
        //            if ( prevTargetUid != entity.TargetEntityUid )
        //            {
        //                //handle previous fill
        //                if ( IsGuidValid( prevTargetUid ) && orp.AgentRole.Items.Count > 0 )
        //                    list.Add( orp );

        //                prevTargetUid = entity.TargetEntityUid;

        //                orp = new OrganizationRoleProfile();
        //                orp.Id = 0;
        //                orp.ParentId = agentEntity.EntityBaseId;
        //                orp.ParentTypeId = agentEntity.EntityTypeId;

        //                orp.ProfileSummary = entity.TargetEntityName;

        //                orp.AgentRole = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_ENTITY_AGENT_ROLE );
        //                orp.AgentRole.ParentId = entity.OrgId;

        //                orp.AgentRole.Items = new List<EnumeratedItem>();
        //                orp.SourceEntityType = entity.TargetEntityType;

        //                if ( entity.TargetEntityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL )
        //                {
        //                    //17-08-27 mp - just get the basic for each entity!
        //                    orp.TargetCredential = CredentialManager.GetBasic( entity.TargetEntityBaseId ?? 0 );

        //                }
        //                else if ( entity.TargetEntityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION )
        //                {
        //                    orp.TargetOrganization.Id = entity.TargetEntityBaseId ?? 0;
        //                    orp.TargetOrganization.RowId = entity.TargetEntityUid;
        //                    orp.TargetOrganization.Name = entity.TargetEntityName;

        //                    orp.TargetOrganization.Description = entity.TargetEntityDescription;
        //                    orp.TargetOrganization.SubjectWebpage = entity.TargetEntitySubjectWebpage;
        //                    orp.TargetOrganization.ImageUrl = entity.TargetEntityImageUrl;
        //                }
        //                else if ( entity.TargetEntityTypeId == CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE )
        //                {
        //                    orp.TargetAssessment = AssessmentManager.GetBasic( entity.TargetEntityBaseId ?? 0 );
        //                }
        //                else if ( entity.TargetEntityTypeId == CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE )
        //                {
        //                    orp.TargetLearningOpportunity = LearningOpportunityManager.GetBasic( entity.TargetEntityBaseId ?? 0 );
        //                }
        //            }

        //            //add relationship
        //            eitem = new EnumeratedItem();
        //            //??
        //            eitem.Id = entity.AssertionTypeId;
        //            //eitem.RowId = entity.RowId.ToString();
        //            //not used here
        //            eitem.RecordId = entity.AssertionTypeId;
        //            eitem.CodeId = entity.AssertionTypeId;

        //            eitem.Name = entity.AgentToSourceRelationship;
        //            eitem.SchemaName = entity.ReverseSchemaTag;
        //            //eitem.Selected = true;
        //            //if ( ( bool )entity.IsQARole )
        //            //{
        //            //    eitem.IsSpecialValue = true;
        //            //    if ( IsDevEnv() )
        //            //        eitem.Name += " (QA)";
        //            //}

        //            orp.AgentRole.Items.Add( eitem );

        //        }
        //        //check for remaining
        //        if ( IsGuidValid( prevTargetUid ) && orp.AgentRole.Items.Count > 0 )
        //            list.Add( orp );

        //    }
        //    return list;

        //} //

        //
        
		public static List<OrganizationAssertion> GetAllQAPerformedCombined( int orgId, int maxRecords = 10 )
        {
            //Organization org = OrganizationManager.GetForSummary( orgId, true );
            //return GetAllCombined( org.RowId, maxRecords );
            OrganizationAssertion orp = new OrganizationAssertion();
            List<OrganizationAssertion> list = new List<OrganizationAssertion>();
            EnumeratedItem eitem = new EnumeratedItem();
            int records = maxRecords * 2;
            Guid prevTargetUid = new Guid();

            string prevRoleSource = string.Empty;
            //string currRoleSource = string.Empty;
            int prevRoleTypeId = 0;
            Entity agentEntity = EntityManager.GetEntity( 2, orgId );

            using ( var context = new ViewContext() )
            {
                List<Views.Organization_CombinedQAPerformed> agentRoles = context.Organization_CombinedQAPerformed
                    .Where( s => s.OrgUid == agentEntity.EntityUid
                         && s.IsQARole == true
                         && s.TargetEntityStateId > 1 )
                         .OrderBy( s => s.TargetEntityTypeId )
                         .ThenBy( s => s.TargetEntityBaseId )
                         .ThenBy( s => s.TargetEntityName )
                         .ThenBy( s => s.AgentToSourceRelationship )
                         .ThenBy( s => s.roleSource )
                    .Take( records )
                    .ToList();

                foreach ( var entity in agentRoles )
                {
                    if ( entity.TargetEntityUid == prevTargetUid && entity.RelationshipTypeId == prevRoleTypeId )
                    {
                        continue;
                    }
                    //loop until change in entity type?
                    //if ( prevTargetUid != entity.TargetEntityUid )

                    //handle previous fill
                    //if ( IsGuidValid( prevTargetUid ) && orp.AgentAssertion.Items.Count > 0 )
                    //	list.Add( orp );

                    prevTargetUid = entity.TargetEntityUid;
                    prevRoleSource = entity.roleSource;
                    prevRoleTypeId = entity.RelationshipTypeId;

                    orp = new OrganizationAssertion();
                    orp.Id = 0;
                    orp.ParentEntityId = agentEntity.EntityBaseId;


                    orp.AgentAssertion = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_ENTITY_AGENT_ROLE );
                    orp.AgentAssertion.ParentId = entity.OrgId;

                    orp.AgentAssertion.Items = new List<EnumeratedItem>();
                    orp.TargetEntityType = entity.TargetEntityType;
                    orp.TargetEntityBaseId = ( int )entity.TargetEntityBaseId;
                    orp.TargetEntityName = entity.TargetEntityName;
                    orp.TargetEntitySubjectWebpage = entity.TargetEntitySubjectWebpage;
                    orp.TargetEntityStateId = ( int )entity.TargetEntityStateId;
                    orp.AgentToSourceRelationship = entity.AgentToSourceRelationship;
					//20-10-10 - targetCTID was not being populated
					orp.TargetCTID = entity.AgentCTID;
                    if ( list.Count() < maxRecords )
                        list.Add( orp );


                    //check for a change in roleSource
                    //if ( prevRoleSource != entity.roleSource )
                    //{
                    //    if ( prevRoleTypeId == entity.RelationshipTypeId )
                    //    {
                    //        //add as matched assertion
                    //        //may want to delay addding enumeration item
                    //        continue;
                    //    }
                    //}


                }
                //check for remaining
                //if ( IsGuidValid( prevTargetUid ) && orp.AgentAssertion.Items.Count > 0 )
                //    list.Add( orp );

            }
            return list;
        }
        public static List<OrganizationAssertion> GetAllCombined2( int orgId, int maxRecords = 10 )
        {
            //Organization org = OrganizationManager.GetForSummary( orgId, true );
            //return GetAllCombined( org.RowId, maxRecords );
            OrganizationAssertion orp = new OrganizationAssertion();
            List<OrganizationAssertion> list = new List<OrganizationAssertion>();
            EnumeratedItem eitem = new EnumeratedItem();

            Guid prevTargetUid = new Guid();
            string prevRoleSource = string.Empty;
            int prevRoleTypeId = 0;
            Entity agentEntity = EntityManager.GetEntity( 2, orgId );

            using ( var context = new ViewContext() )
            {
                List<Views.Organization_CombinedQAPerformed> agentRoles = context.Organization_CombinedQAPerformed
                    .Where( s => s.OrgUid == agentEntity.EntityUid
                         && s.IsQARole == true
                         && s.TargetEntityStateId > 1 )
                         .OrderBy( s => s.TargetEntityTypeId )
                         .ThenBy( s => s.TargetEntityBaseId )
                         .ThenBy( s => s.TargetEntityName )
                         .ThenBy( s => s.AgentToSourceRelationship )
                         .ThenBy( s => s.roleSource )
                    .Take( maxRecords )
                    .ToList();

                foreach ( var entity in agentRoles )
                {
                    //loop until change in entity type?
                    if ( prevTargetUid != entity.TargetEntityUid )
                    {
                        //handle previous fill
                        if ( IsGuidValid( prevTargetUid ) && orp.AgentAssertion.Items.Count > 0 )
                            list.Add( orp );

                        prevTargetUid = entity.TargetEntityUid;
                        prevRoleSource = entity.roleSource;
                        prevRoleTypeId = entity.RelationshipTypeId;

                        orp = new OrganizationAssertion();
                        orp.Id = 0;
                        orp.ParentEntityId = agentEntity.EntityBaseId;


                        orp.AgentAssertion = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_ENTITY_AGENT_ROLE );
                        orp.AgentAssertion.ParentId = entity.OrgId;

                        orp.AgentAssertion.Items = new List<EnumeratedItem>();
                        orp.TargetEntityType = entity.TargetEntityType;
                        orp.TargetEntityBaseId = ( int )entity.TargetEntityBaseId;
                        orp.TargetEntityName = entity.TargetEntityName;
                        orp.TargetEntitySubjectWebpage = entity.TargetEntitySubjectWebpage;
                        orp.TargetEntityStateId = ( int )entity.TargetEntityStateId;
                        orp.AgentToSourceRelationship = entity.AgentToSourceRelationship;
                        //if ( entity.TargetEntityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL )
                        //{
                        //	//17-08-27 mp - just get the basic for each entity!
                        //	orp.TargetCredential = CredentialManager.GetBasic( entity.TargetEntityBaseId ?? 0 );

                        //}
                        //else if ( entity.TargetEntityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION )
                        //{
                        //	//orp.TargetOrganization = OrganizationManager.GetBasics( entity.TargetEntityUid );
                        //	orp.TargetOrganization.Id = entity.TargetEntityBaseId ?? 0;
                        //	orp.TargetOrganization.RowId = entity.TargetEntityUid;
                        //	orp.TargetOrganization.Name = entity.TargetEntityName;

                        //	orp.TargetOrganization.Description = entity.TargetEntityDescription;
                        //	orp.TargetOrganization.EntityStateId = entity.TargetEntityStateId ?? 2;
                        //	orp.TargetOrganization.SubjectWebpage = entity.TargetEntitySubjectWebpage;
                        //	orp.TargetOrganization.ImageUrl = entity.TargetEntityImageUrl;
                        //}
                        //else if ( entity.TargetEntityTypeId == CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE )
                        //{
                        //	orp.TargetAssessment = AssessmentManager.GetBasic( entity.TargetEntityBaseId ?? 0 );
                        //}
                        //else if ( entity.TargetEntityTypeId == CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE )
                        //{
                        //	orp.TargetLearningOpportunity = LearningOpportunityManager.GetBasic( entity.TargetEntityBaseId ?? 0 );
                        //}
                    }

                    //check for a change in roleSource
                    if ( prevRoleSource != entity.roleSource )
                    {
                        if ( prevRoleTypeId == entity.RelationshipTypeId )
                        {
                            //add as matched assertion
                            //may want to delay addding enumeration item
                            continue;
                        }
                    }
                    //add relationship
                    eitem = new EnumeratedItem();
                    //??
                    eitem.Id = entity.RelationshipTypeId;

                    //eitem.RowId = entity.RowId.ToString();
                    //not used here
                    eitem.RecordId = entity.RelationshipTypeId;
                    //eitem.CodeId = entity.RelationshipTypeId;

                    prevRoleTypeId = entity.RelationshipTypeId;
                    prevRoleSource = entity.roleSource;

                    eitem.Name = entity.AgentToSourceRelationship;
                    eitem.SchemaName = entity.ReverseSchemaTag;

                    orp.AgentAssertion.Items.Add( eitem );

                }
                //check for remaining
                if ( IsGuidValid( prevTargetUid ) && orp.AgentAssertion.Items.Count > 0 )
                    list.Add( orp );

            }
            return list;
        }

		public static void FillCountsForOrganizationQAPerformed( Organization org, ref int totalRecords )
		{

			Entity agentEntity = EntityManager.GetEntity( org.RowId );
			using ( var context = new ViewContext() )
			{
				//first check how long this step takes
				DateTime start = DateTime.Now;
				LoggingHelper.DoTrace( 8, "FillCountsForOrganizationQAPerformed start" );
				
				var query = from qa in context.Organization_CombinedQAPerformed
					.Where( s => s.OrgUid == org.RowId
						 && s.IsQARole == true
						 && s.TargetEntityStateId > 1 )
						 .OrderBy( s => s.TargetEntityTypeId )
						 .ThenBy( s => s.TargetOwningOrganizationName )
						 .ThenBy( s => s.TargetEntityName )
						 .ThenBy( s => s.AgentToSourceRelationship )
						 .ThenBy( s => s.roleSource )
							select new
							{
								qa.TargetEntityTypeId,
								qa.TargetEntityBaseId,
								qa.TargetOwningOrganizationId
							};
				DateTime end = DateTime.Now;
				var elasped = end.Subtract( start ).TotalSeconds;
				LoggingHelper.DoTrace( 8, string.Format( "FillCountsForOrganizationQAPerformed retrieve seconds: {0}", elasped ) );

				var results = query.Distinct().ToList();
				if ( results != null && results.Count() > 0 )
				{
					//
					totalRecords = results.Count();
					//may want a fudge factor?
					org.QAPerformedOnCredentialsCount = results.Where( s => s.TargetEntityTypeId == 1 && s.TargetOwningOrganizationId != org.Id ).Distinct().Count();
					org.QAPerformedOnOrganizationsCount = results.Where( s => s.TargetEntityTypeId == 2 ).Distinct().Count();
					org.QAPerformedOnAssessmentsCount = results.Where( s => s.TargetEntityTypeId == 3 && s.TargetOwningOrganizationId != org.Id ).Distinct().Count();
					org.QAPerformedOnLoppsCount = results.Where( s => s.TargetEntityTypeId == 7 && s.TargetOwningOrganizationId != org.Id ).Distinct().Count();
					org.QAPerformedOnTransferValuesCount = results.Where( s => s.TargetEntityTypeId == CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE && s.TargetOwningOrganizationId != org.Id ).Distinct().Count();

					DateTime listEnd = DateTime.Now;
					elasped = listEnd.Subtract( end ).TotalSeconds;
					LoggingHelper.DoTrace( 8, string.Format( "FillCountsForOrganizationQAPerformed loaded list seconds: {0}", elasped ) );
				}
			}


		} //

		//public static List<OrganizationRoleProfile> GetAllCombinedForOrganization( Guid agentUid, ref int totalRecords, int maxRecords = 25 )
		//      {
		//          OrganizationRoleProfile orp = new OrganizationRoleProfile();
		//          List<OrganizationRoleProfile> list = new List<OrganizationRoleProfile>();
		//          EnumeratedItem eitem = new EnumeratedItem();

		//          Guid prevTargetUid = new Guid();
		//          string prevRoleSource = string.Empty;
		//          int prevRoleTypeId = 0;
		//          Entity agentEntity = EntityManager.GetEntity( agentUid );
		//	if ( UtilityManager.GetAppKeyValue( "environment" ) == "production" )
		//	{
		//		//show all for now in production
		//		//maxRecords = 0;
		//	}
		//          using ( var context = new ViewContext() )
		//          {
		//		//first check how long this step takes
		//		DateTime start = DateTime.Now;
		//		LoggingHelper.DoTrace( 4, "GetAllCombinedForOrganization start" );
		//              List<Views.Organization_CombinedQAPerformed> agentRoles = context.Organization_CombinedQAPerformed
		//                  .Where( s => s.OrgUid == agentUid
		//                       && s.IsQARole == true
		//                       && s.TargetEntityStateId > 1 )
		//                       .OrderBy( s => s.TargetEntityTypeId )
		//                       .ThenBy( s => s.TargetOwningOrganizationName )
		//                       .ThenBy( s => s.TargetEntityName )
		//                       .ThenBy( s => s.AgentToSourceRelationship )
		//                       .ThenBy( s => s.roleSource )
		//                  .ToList();

		//		DateTime end = DateTime.Now;
		//		var elasped = end.Subtract( start ).TotalSeconds;
		//		LoggingHelper.DoTrace( 4, string.Format("GetAllCombinedForOrganization retrieve seconds: {0}", elasped) );

		//		if (agentRoles != null && agentRoles.Count() > 0)
		//		{
		//			//
		//			totalRecords = agentRoles.Count();
		//			//may want a fudge factor?

		//		}
		//		int cntr = 0;
		//              foreach ( var entity in agentRoles )
		//              {
		//			cntr++;
		//                  //loop until change in entity type?
		//                  if ( prevTargetUid != entity.TargetEntityUid )
		//                  {
		//                      //handle previous fill
		//                      if ( IsGuidValid( prevTargetUid ) && prevRoleTypeId > 0 )
		//                      {
		//                          orp.AgentRole.Items.Add( eitem );
		//                          list.Add( orp );
		//					if ( maxRecords > 0 && cntr >= maxRecords )
		//					{
		//						break;
		//					}
		//				}

		//                      prevTargetUid = entity.TargetEntityUid;
		//                      prevRoleSource = entity.roleSource;
		//                      prevRoleTypeId = entity.RelationshipTypeId;

		//                      orp = new OrganizationRoleProfile
		//                      {
		//                          Id = 0,
		//                          ParentId = agentEntity.EntityBaseId,
		//                          ParentTypeId = agentEntity.EntityTypeId,
		//                          ProfileSummary = entity.TargetEntityName,

		//                          AgentRole = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_ENTITY_AGENT_ROLE )
		//                      };
		//                      orp.AgentRole.ParentId = entity.OrgId;

		//                      orp.AgentRole.Items = new List<EnumeratedItem>();
		//                      orp.SourceEntityType = entity.TargetEntityType;

		//                      if ( entity.TargetEntityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL )
		//                      {
		//                          //17-08-27 mp - just get the basic for each entity!
		//                          orp.TargetCredential = CredentialManager.GetBasic( entity.TargetEntityBaseId ?? 0 );

		//                      }
		//                      else if ( entity.TargetEntityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION )
		//                      {
		//                          //orp.TargetOrganization = OrganizationManager.GetBasics( entity.TargetEntityUid );
		//                          orp.TargetOrganization.Id = entity.TargetEntityBaseId ?? 0;
		//                          orp.TargetOrganization.RowId = entity.TargetEntityUid;
		//                          orp.TargetOrganization.Name = entity.TargetEntityName;

		//                          orp.TargetOrganization.Description = entity.TargetEntityDescription;
		//                          orp.TargetOrganization.EntityStateId = entity.TargetEntityStateId ?? 2;
		//                          orp.TargetOrganization.SubjectWebpage = entity.TargetEntitySubjectWebpage;
		//                          orp.TargetOrganization.Image = entity.TargetEntityImageUrl;
		//                      }
		//                      else if ( entity.TargetEntityTypeId == CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE )
		//                      {
		//                          orp.TargetAssessment = AssessmentManager.GetBasic( entity.TargetEntityBaseId ?? 0 );
		//                      }
		//                      else if ( entity.TargetEntityTypeId == CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE )
		//                      {
		//                          orp.TargetLearningOpportunity = LearningOpportunityManager.GetBasic( entity.TargetEntityBaseId ?? 0 );
		//                      }
		//                  }

		//                  /* either first one for new target
		//			 * or change in relationship
		//			 * or change in role source
		//			 */

		//                  if ( prevRoleTypeId == entity.RelationshipTypeId )
		//                  {
		//                      if ( prevRoleSource != entity.roleSource )
		//                      {
		//                          if ( entity.roleSource == "DirectAssertion" )
		//                              eitem.IsDirectAssertion = true;
		//                          else
		//                              eitem.IsIndirectAssertion = true;

		//                          //add previous
		//                          //could get a dup if there is an immediate chg in target, 
		//                          //orp.AgentRole.Items.Add( eitem );
		//                          prevRoleSource = entity.roleSource;

		//                          continue;
		//                      }

		//                  }
		//                  else
		//                  {
		//                      //if not equal, add previous, and initialize next one (fall thru)
		//                      orp.AgentRole.Items.Add( eitem );
		//                  }

		//			//add relationship
		//			eitem = new EnumeratedItem
		//			{
		//				Id = entity.RelationshipTypeId,
		//				Name = entity.AgentToSourceRelationship,
		//				SchemaName = entity.ReverseSchemaTag,
		//				IsQAValue = true
		//                  };
		//                  //eitem.CodeId = entity.RelationshipTypeId;

		//                  prevRoleTypeId = entity.RelationshipTypeId;
		//                  prevRoleSource = entity.roleSource;
		//                  if ( entity.roleSource == "DirectAssertion" )
		//                      eitem.IsDirectAssertion = true;
		//                  else
		//                      eitem.IsIndirectAssertion = true;

		//                  //eitem.Name = entity.AgentToSourceRelationship;
		//                  //               eitem.SchemaName = entity.ReverseSchemaTag;

		//                  //               orp.AgentRole.Items.Add( eitem );

		//              } //end
		//              //check for remaining
		//              if ( IsGuidValid( prevTargetUid ) && orp.AgentRole.Items.Count > 0 )
		//              {
		//                  orp.AgentRole.Items.Add( eitem );
		//                  list.Add( orp );
		//              }

		//		DateTime listEnd = DateTime.Now;
		//		elasped = listEnd.Subtract( end ).TotalSeconds;
		//		LoggingHelper.DoTrace( 4, string.Format( "GetAllCombinedForOrganization loaded list seconds: {0}", elasped ) );
		//	}
		//          return list;

		//      } //

		/// <summary>
		/// Get roles
		/// 21-06-01 this method seems to be called excessively. For a TVP detail - currently 9 times. Seems wrong
		/// WARNING/TODO - this is very slow and will just get worse with more volume. Consider creating a cache table?
		///			- the view: Organization_CombinedConnections does a UNION of Entity.AgentRelationships and Entity.Assertions
		/// </summary>
		/// <param name="targetEntityTypeId"></param>
		/// <param name="recordId"></param>
		/// <param name="owningOrgId"></param>
		/// <returns></returns>
		public static List<OrganizationRoleProfile> GetAllCombinedForTarget( int targetEntityTypeId, int recordId, int owningOrgId )
		{
			LoggingHelper.DoTrace( LoggingHelper.appMethodEntryTraceLevel, thisClassName + ".GetAllCombinedForTarget - entered." );

			OrganizationRoleProfile orp = new OrganizationRoleProfile();
			List<OrganizationRoleProfile> list = new List<OrganizationRoleProfile>();
			//21-03-22 remove requirement for owningOrgId
			//|| owningOrgId == 0
			if ( targetEntityTypeId == 0 || recordId == 0  )
				return list;
			LoggingHelper.DoTrace( 8, string.Format(  "@@@@@ Entity_AssertionManager.GetAllCombinedForTarget targetEntityTypeId:{0}, recordId:{1}, owningOrgId: {2}", targetEntityTypeId, recordId, owningOrgId ) );
			EnumeratedItem eitem = new EnumeratedItem();

			Guid prevOrgUid = new Guid();
			string prevRoleSource = string.Empty;
			int prevRoleTypeId = 0;
			bool includingPublishedBy = false;
			using ( var context = new ViewContext() )
			{
				List<Views.Organization_CombinedConnections> agentRoles = context.Organization_CombinedConnections
					.Where( s => s.TargetEntityTypeId == targetEntityTypeId 
							&& s.TargetEntityBaseId == recordId
							//&& s.IsQARole == true
							&& s.TargetEntityStateId > 1 )
							.OrderBy( s => s.Organization )
							.ThenBy( s => s.RelationshipTypeId )
							.ThenBy( s => s.roleSource )
						.ToList();
				if ( agentRoles != null && agentRoles.Any() )
				{
					//for this view, we want to retrieve the QA organization info, we already have the target (ie. that is the current context).
					foreach ( var entity in agentRoles )
					{
						if(!includingPublishedBy && entity.RelationshipTypeId == 30)
						{
							continue;
						}
						//loop until change in entity type?
						if ( prevOrgUid != entity.OrgUid )
						{
							//handle previous fill
							if ( IsGuidValid( prevOrgUid ) && prevRoleTypeId > 0 )
							{
								if( eitem.Id > 0)
									orp.AgentRole.Items.Add( eitem );
								if (orp.AgentRole.Items.Any())
									list.Add( orp );
							}

							prevOrgUid = entity.OrgUid;
							prevRoleSource = entity.roleSource;
							prevRoleTypeId = entity.RelationshipTypeId;

							//not sure if pertinent
							orp = new OrganizationRoleProfile
							{
								Id = 0,
								RelatedEntityId = entity.OrgId,
								ParentTypeId = 2,
								ProfileSummary = entity.Organization,

								AgentRole = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_ENTITY_AGENT_ROLE )
							};
							orp.AgentRole.ParentId = entity.OrgId;

							orp.ActingAgent = new Organization();

							//or should it be TargetOrganization - check how currently used
							//compare: Entity_AgentRelationshipManager.AgentEntityRole_GetAll_ToEnumeration
							orp.ActingAgentUid = entity.OrgUid;
							orp.ActingAgentId = entity.OrgId;
							orp.ActingAgent = new Organization()
							{
								Id = entity.OrgId,
								RowId = entity.OrgUid,
								Name = entity.Organization,
								SubjectWebpage = entity.AgentSubjectWebpage,
								Description = entity.AgentDescription,
								Image = entity.AgentImageUrl,
								EntityStateId = entity.AgentEntityStateId ?? 1,
								CTID = entity.AgentCTID
							};
							orp.AgentRole.Items = new List<EnumeratedItem>();
						}

						/* either first one for new target
						 * or change in relationship
						 * or change in role source
						 * NOTE: skip the Direct checks if not QA, or at least if only owns/offers
						 */

						if ( prevRoleTypeId == entity.RelationshipTypeId )
						{
							if ( prevRoleSource != entity.roleSource )
							{
								//TBD
								if ( entity.IsQARole ?? false )
								{
									if ( entity.roleSource == "QAOrganization" || entity.OrgId == owningOrgId )
										eitem.IsDirectAssertion = true;
									else
										eitem.IsIndirectAssertion = true;
								}
								//add previous
								//could get a dup if there is an immediate chg in target, 
								//orp.AgentRole.Items.Add( eitem );
								prevRoleSource = entity.roleSource;
								continue;
							}
						}
						else
						{
							//if not equal, add previous, and initialize next one (fall thru)
							orp.AgentRole.Items.Add( eitem );
						}

						//add relationship
						eitem = new EnumeratedItem()
						{
							Id = entity.RelationshipTypeId,
							Name = entity.SourceToAgentRelationship,
							SchemaName = entity.SchemaTag,
							IsQAValue = ( entity.IsQARole ?? false )
						};

						//21-06-30 Huh - both sides of if just set the same as above
						/*
						if ( ( entity.IsQARole ?? false ) && entity.OrgId != owningOrgId )
						{
							//not sure this is correct. For QA received, it should be BYs
							//eitem.Name = entity.AgentToSourceRelationship;
							eitem.Name = entity.SourceToAgentRelationship;
							eitem.SchemaName = entity.SchemaTag;
							//eitem.SchemaName = entity.ReverseSchemaTag;
						}
						else
						{
							if ( entity.RelationshipTypeId != 6 && entity.RelationshipTypeId != 7)
							{
								eitem.Name = entity.SourceToAgentRelationship;
								eitem.SchemaName = entity.SchemaTag;
							}
							//eitem.Name = entity.SourceToAgentRelationship;
							//eitem.SchemaName = entity.SchemaTag;
						}
						*/
						//eitem.CodeId = entity.RelationshipTypeId;

						prevRoleTypeId = entity.RelationshipTypeId;
						prevRoleSource = entity.roleSource;
						//**need additional check if from the owning org!
						if ( entity.IsQARole ?? false )
						{
							if ( entity.roleSource == "QAOrganization" || entity.OrgId == owningOrgId )
								eitem.IsDirectAssertion = true;
							else
								eitem.IsIndirectAssertion = true;
						}
					} //

					//check for remaining
					if ( IsGuidValid( prevOrgUid ) && prevRoleTypeId > 0 )
					{
						if ( eitem.Id > 0 )
							orp.AgentRole.Items.Add( eitem );
						if ( orp.AgentRole.Items.Any() )
							list.Add( orp );
					}
				}
			}
			return list;

		} //

		/// <summary>
		/// Get assertions made by the organization rather than via the entity
		/// </summary>
		/// <param name="targetEntityTypeId"></param>
		/// <param name="targetEntityUid"></param>
		/// <param name="owningOrgId"></param>
		/// <param name="onlyQAAssertions"></param>
		/// <returns></returns>
		public static List<OrganizationRoleProfile> GetAllFirstPartyAssertionsForTarget( int targetEntityTypeId, Guid targetEntityUid, int owningOrgId, bool onlyQAAssertions = false )
		{
			OrganizationRoleProfile orp = new OrganizationRoleProfile();
			List<OrganizationRoleProfile> list = new List<OrganizationRoleProfile>();
			//21-03-22 remove requirement for owningOrgId
			//|| owningOrgId == 0
			if ( targetEntityTypeId == 0 || targetEntityUid== null || !BaseFactory.IsGuidValid( targetEntityUid) )
				return list;
			LoggingHelper.DoTrace( 5, string.Format( "@@@@@ GetAllFirstPartyAssertionsForTarget.GetAllCombinedForTarget targetEntityTypeId:{0}, targetEntityUid:{1}, owningOrgId: {2}", targetEntityTypeId, targetEntityUid, owningOrgId ) );
			EnumeratedItem eitem = new EnumeratedItem();

			Guid prevOrgUid = new Guid();
			string prevRoleSource = string.Empty;
			int prevRoleTypeId = 0;
			bool includingPublishedBy = false;
			using ( var context = new EntityContext() )
			{
				var list1 = from item in context.Entity_Assertion
							join entity in context.Entity on item.EntityId equals entity.Id
							join org in context.Organization on entity.EntityUid equals org.RowId
							join codes in context.Codes_CredentialAgentRelationship on item.AssertionTypeId equals codes.Id
							where item.TargetEntityTypeId == targetEntityTypeId && item.TargetEntityUid == targetEntityUid
								//&& agent.RelationshipTypeId == 22 //parent org relationship
								&& org.EntityStateId > 1

							select new
							{
								RelationshipTypeId=item.AssertionTypeId,
								item.IsInverseRole,
								RelationshipType = codes.Title,
								codes.ReverseRelation,
								codes.SchemaTag,
								
								codes.ReverseSchemaTag,
								codes.IsQARole,
								OrgId=org.Id,
								OrgRowId = org.RowId,
								OrganizationName=org.Name,
								org.SubjectWebpage,
								org.Description,
								org.CTID,
								org.ImageURL,
								org.EntityStateId,
								org.ISQAOrganization
							};
				var agentRoles = list1.OrderBy( m => m.OrganizationName ).ThenBy( m => m.RelationshipTypeId ).ToList();
				//proposal to reuse method
				if ( onlyQAAssertions )
				{
					agentRoles = agentRoles.Where( m => m.RelationshipTypeId == 1 || m.RelationshipTypeId == 2 || m.RelationshipTypeId == 10 || m.RelationshipTypeId == 12 )
						.OrderBy( m => m.OrganizationName ).ThenBy( m => m.RelationshipTypeId ).ToList();
				}
				if ( agentRoles != null && agentRoles.Any() )
				{
					//for this view, we want to retrieve the QA organization info, we already have the target (ie. that is the current context).
					foreach ( var entity in agentRoles )
					{
						if ( !includingPublishedBy && entity.RelationshipTypeId == 30 )
						{
							continue;
						}
						//loop until change in entity type?
						if ( prevOrgUid != entity.OrgRowId )
						{
							//handle previous fill
							if ( IsGuidValid( prevOrgUid ) && prevRoleTypeId > 0 )
							{
								if ( eitem.Id > 0 )
									orp.AgentRole.Items.Add( eitem );
								if ( orp.AgentRole.Items.Any() )
									list.Add( orp );
							}

							prevOrgUid = entity.OrgRowId;
							//prevRoleSource = entity.roleSource;
							prevRoleTypeId = entity.RelationshipTypeId;

							//not sure if pertinent
							orp = new OrganizationRoleProfile
							{
								Id = 0,
								RelatedEntityId = entity.OrgId,
								ParentTypeId = 2,
								ProfileSummary = entity.OrganizationName,
								AgentRole = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_ENTITY_AGENT_ROLE ),
								AssertionType="First Party", //
								IsDirectAssertion = true
							};
							orp.AgentRole.ParentId = entity.OrgId;

							orp.ActingAgent = new Organization();

							//or should it be TargetOrganization - check how currently used
							//compare: Entity_AgentRelationshipManager.AgentEntityRole_GetAll_ToEnumeration
							orp.ActingAgentUid = entity.OrgRowId;
							orp.ActingAgentId = entity.OrgId;
							orp.ActingAgent = new Organization()
							{
								Id = entity.OrgId,
								RowId = entity.OrgRowId,
								Name = entity.OrganizationName,
								SubjectWebpage = entity.SubjectWebpage,
								Description = entity.Description,
								Image = entity.ImageURL,
								EntityStateId = entity.EntityStateId ?? 1,
								CTID = entity.CTID
							};
							orp.AgentRole.Items = new List<EnumeratedItem>();
						}

						/* either first one for new target
						 * or change in relationship
						 * or change in role source
						 * NOTE: skip the Direct checks if not QA, or at least if only owns/offers
						 */

						//21-07-12 - this code results in skipping the proper creation of eitem. This for the first relationship
						if ( prevRoleTypeId == entity.RelationshipTypeId )
						{
							if ( entity.IsQARole ?? false )
								{
								if ( ( entity.ISQAOrganization != null && ( bool )entity.ISQAOrganization ) || entity.OrgId == owningOrgId )
									eitem.IsDirectAssertion = true;
									else
										eitem.IsIndirectAssertion = true;
								}
								//add previous
								//could get a dup if there is an immediate chg in target, 
								//orp.AgentRole.Items.Add( eitem );
								//prevRoleSource = entity.roleSource;
								//continue;
							//}
						}
						else
						{
							//if not equal, add previous, and initialize next one (fall thru)
							orp.AgentRole.Items.Add( eitem );
						}

						//add relationship
						eitem = new EnumeratedItem
						{
							Id = entity.RelationshipTypeId,
							Name = entity.RelationshipType,
							SchemaName = entity.SchemaTag,
							IsQAValue = ( entity.IsQARole ?? false )
						};


						//eitem.CodeId = entity.RelationshipTypeId;

						prevRoleTypeId = entity.RelationshipTypeId;
						//prevRoleSource = entity.roleSource;
						//**need additional check if from the owning org!
						if ( entity.IsQARole ?? false )
						{
							if ( ( entity.ISQAOrganization != null && ( bool )entity.ISQAOrganization ) || entity.OrgId == owningOrgId )
								eitem.IsDirectAssertion = true;
							else
								eitem.IsIndirectAssertion = true;
						}
					} //

					//check for remaining
					if ( IsGuidValid( prevOrgUid ) && prevRoleTypeId > 0 )
					{
						if ( eitem.Id > 0 )
							orp.AgentRole.Items.Add( eitem );
						if ( orp.AgentRole.Items.Any() )
							list.Add( orp );
					}
				}
			}
			return list;

		} //

		/// <summary>
		/// Get list of assertions for an entity made by the actual organization (first party)
		///
		/// </summary>
		/// <param name="targetEntityTypeId"></param>
		/// <param name="targetEntityUid">UID for entity receiving the assertion</param>
		/// <param name="owningOrgId"></param>
		/// <returns></returns>
		public static List<OrganizationRoleProfile> GetAllFirstPartyQAAssertionsForTarget( int targetEntityTypeId, Guid targetEntityUid, int owningOrgId, bool onlyQAAssertions = true )
		{
			OrganizationRoleProfile orp = new OrganizationRoleProfile();
			List<OrganizationRoleProfile> list = new List<OrganizationRoleProfile>();
			//21-03-22 remove requirement for owningOrgId
			//|| owningOrgId == 0
			if ( targetEntityTypeId == 0 || targetEntityUid == null || !BaseFactory.IsGuidValid( targetEntityUid ) )
				return list;
			LoggingHelper.DoTrace( 8, string.Format( "@@@@@ Entity_AssertionManager.GetAllFirstPartyQAAssertionsForTarget targetEntityTypeId:{0}, targetEntityUid:{1}, owningOrgId: {2}", targetEntityTypeId, targetEntityUid, owningOrgId ) );
			EnumeratedItem eitem = new EnumeratedItem();

			Guid prevOrgUid = new Guid();
			string prevRoleSource = string.Empty;
			int prevRoleTypeId = 0;
			bool includingPublishedBy = false;
			using ( var context = new EntityContext() )
			{
				//Entity.Assertions is always an org making an assertion for another entity
				var list1 = from item in context.Entity_Assertion
							join entity in context.Entity on item.EntityId equals entity.Id
							join org in context.Organization on entity.EntityUid equals org.RowId
							join codes in context.Codes_CredentialAgentRelationship on item.AssertionTypeId equals codes.Id
							where item.TargetEntityTypeId == targetEntityTypeId && item.TargetEntityUid == targetEntityUid
								&& org.EntityStateId > 1    //the org can be a reference 
								&& (item.AssertionTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_AccreditedBy 
								|| item.AssertionTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_ApprovedBy 
								|| item.AssertionTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_RegulatedBy 
								|| item.AssertionTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_RecognizedBy )								

							select new
							{
								RelationshipTypeId = item.AssertionTypeId,
								item.IsInverseRole,
								RelationshipType = codes.Title,
								codes.ReverseRelation,
								codes.SchemaTag,
								codes.ReverseSchemaTag,
								codes.IsQARole,
								OrgId = org.Id,
								OrgRowId = org.RowId,
								OrganizationName = org.Name,
								org.SubjectWebpage,
								org.Description,
								org.CTID,
								org.ImageURL,
								org.EntityStateId,
								org.ISQAOrganization
							};
				var agentRoles = list1.OrderBy( m => m.OrganizationName ).ThenBy( m => m.RelationshipTypeId ).ToList();

				if ( agentRoles != null && agentRoles.Any() )
				{
					//for this view, we want to retrieve the QA organization info, we already have the target (ie. that is the current context).???
					foreach ( var entity in agentRoles )
					{
						if ( !includingPublishedBy && entity.RelationshipTypeId == 30 )
						{
							continue;
						}
						//loop until change in entity type?
						if ( prevOrgUid != entity.OrgRowId )
						{
							//handle previous fill
							if ( IsGuidValid( prevOrgUid ) && prevRoleTypeId > 0 )
							{
								orp.AgentRole.Items.Add( eitem );
								list.Add( orp );
							}

							prevOrgUid = entity.OrgRowId;
							//prevRoleSource = entity.roleSource;
							prevRoleTypeId = entity.RelationshipTypeId;

							//not sure if pertinent
							orp = new OrganizationRoleProfile
							{
								Id = 0,
								RelatedEntityId = entity.OrgId,
								ParentTypeId = 2,
								ProfileSummary = entity.OrganizationName,

								AgentRole = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_ENTITY_AGENT_ROLE )
							};
							orp.AgentRole.ParentId = entity.OrgId;

							//or should it be TargetOrganization - check how currently used
							//compare: Entity_AgentRelationshipManager.AgentEntityRole_GetAll_ToEnumeration
							orp.ActingAgentUid = entity.OrgRowId;
							orp.ActingAgentId = entity.OrgId;
							orp.ActingAgent = new Organization()
							{
								Id = entity.OrgId,
								RowId = entity.OrgRowId,
								Name = entity.OrganizationName,
								SubjectWebpage = entity.SubjectWebpage,
								Description = entity.Description,
								Image = entity.ImageURL,
								EntityStateId = entity.EntityStateId ?? 1,
								CTID = entity.CTID
							};
							orp.AgentRole.Items = new List<EnumeratedItem>();
						}

						/* either first one for new target
						 * or change in relationship
						 * or change in role source
						 * NOTE: skip the Direct checks if not QA, or at least if only owns/offers
						 */

						if ( prevRoleTypeId == entity.RelationshipTypeId )
						{
							//if ( prevRoleSource != entity.roleSource )
							//{
							//TBD
							if ( entity.IsQARole ?? false )
							{
								if ( ( bool )entity.ISQAOrganization || entity.OrgId == owningOrgId )
									eitem.IsDirectAssertion = true;
								else
									eitem.IsIndirectAssertion = true;
							}
							//add previous
							//could get a dup if there is an immediate chg in target, 
							//orp.AgentRole.Items.Add( eitem );
							//prevRoleSource = entity.roleSource;
							continue;
							//}
						}
						else
						{
							//if not equal, add previous, and initialize next one (fall thru)
							orp.AgentRole.Items.Add( eitem );
						}

						//add relationship
						eitem = new EnumeratedItem
						{
							Id = entity.RelationshipTypeId,
							Name = entity.RelationshipType,
							SchemaName = entity.SchemaTag,
							IsQAValue = ( entity.IsQARole ?? false )
						};


						//eitem.CodeId = entity.RelationshipTypeId;

						prevRoleTypeId = entity.RelationshipTypeId;
						//prevRoleSource = entity.roleSource;
						//**need additional check if from the owning org!
						if ( entity.IsQARole ?? false )
						{
							if ( ( bool )entity.ISQAOrganization || entity.OrgId == owningOrgId )
								eitem.IsDirectAssertion = true;
							else
								eitem.IsIndirectAssertion = true;
						}
					} //

					//check for remaining
					if ( IsGuidValid( prevOrgUid ) && prevRoleTypeId > 0 )
					{
						orp.AgentRole.Items.Add( eitem );
						list.Add( orp );
					}
				}
			}
			return list;

		} //


		public static int CredentialCount_ForRenewedByOrg( Guid orgUid )
		{
			int totalRecords = 0;
			using ( var context = new EntityContext() )
			{
				var query = ( from entity		in context.Entity
							  join assertion	in context.Entity_Assertion on entity.Id equals assertion.EntityId
							  join org			in context.Organization		on entity.EntityUid equals org.RowId
							  join artifact		in context.Credential on entity.EntityUid equals artifact.RowId

							  join codes in context.Codes_CredentialAgentRelationship on assertion.AssertionTypeId equals codes.Id
							  where org.RowId == orgUid
								   && entity.EntityTypeId == 1
								   && artifact.EntityStateId > 1
								   && org.EntityStateId > 1
								  && ( assertion.AssertionTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_RenewedBy )

							  select new
							  {
								  entity.EntityBaseId
							  } );

				var results = query.Select( s => s.EntityBaseId ).Distinct()
					.ToList();

				if ( results != null && results.Count > 0 )
				{
					totalRecords = results.Count();

				}
			}
			return totalRecords;
		}
		#endregion
	}
}

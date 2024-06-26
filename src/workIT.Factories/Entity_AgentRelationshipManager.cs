﻿using System;
using System.Collections.Generic;
using System.Linq;
using workIT.Models;
using workIT.Models.Common;
using workIT.Models.ProfileModels;
using workIT.Utilities;
using DBEntity = workIT.Data.Views.Entity_AgentRelationshipIdCSV;
using DBentitySummary = workIT.Data.Views.Entity_Relationship_AgentSummary;
using EM = workIT.Data.Tables;
//using MN = workIT.Models.Node;

using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;
using Views = workIT.Data.Views;

namespace workIT.Factories
{
    public class Entity_AgentRelationshipManager : BaseFactory
    {
        /// <summary>
        /// Entity_AgentRelationshipManager
        /// The entity is acted upon by the agent. ex
        /// Credential accredited by an agent
        ///		Entity: credential ??? by Agent: org
        ///	Org accredits another org (entity)
        ///		Entity: target org ?? by Agent: current org
        ///	Org is accredited by another org
        ///		Entity: current org ?? by Agent: entered org
        /// </summary>
        static string thisClassName = "Entity_AgentRelationshipManager";

        #region role type constants
        public static int ROLE_TYPE_AccreditedBy = 1;
        public static int ROLE_TYPE_Accredits = 1;
        public static int ROLE_TYPE_ApprovedBy = 2;
        public static int ROLE_TYPE_Approves = 2;
		public static int ROLE_TYPE_AssertedBy = 3;
		public static int ROLE_TYPE_OWNER = 6;
        public static int ROLE_TYPE_OWNS = 6;
        public static int ROLE_TYPE_OFFERED_BY = 7;
        public static int ROLE_TYPE_OFFERS = 7;
		public static int ROLE_TYPE_PROVIDED_BY = 8;
		public static int ROLE_TYPE_RecognizedBy = 10;
        public static int ROLE_TYPE_Recognizes = 10;
        public static int ROLE_TYPE_RevokedBy = 11;
        public static int ROLE_TYPE_Revokes = 11;
        public static int ROLE_TYPE_RegulatedBy = 12;
        public static int ROLE_TYPE_Regulates = 12;
        public static int ROLE_TYPE_RenewedBy = 13;
        public static int ROLE_TYPE_Renews = 13;
        public static int ROLE_TYPE_RegisteredBy = 14;

        public static int ROLE_TYPE_DEPARTMENT = 20;
        public static int ROLE_TYPE_SUBSIDIARY = 21;
        public static int ROLE_TYPE_PARENT_ORG = 22;
		public static int ROLE_TYPE_PUBLISHEDBY = 30;
		public static int ROLE_TYPE_PROVIDES_OUTCOMES = 31;
		#endregion


		#region context valid roles constants
		//todo make table driven
		public static string VALID_ROLES_OWNER = "2,6,7,10,11,13,";
        public static string VALID_ROLES_QA = "1,2,10,11,";
        public static string VALID_ROLES_ORG_QA = "1,2,10,";
        public static string VALID_ROLES_OFFERED_BY = "7,";
        //get owns/offers roles
        public static List<string> GetOwnsOffersRoles()
        {
            return new List<string>() { "6", "7" };
        }
        #endregion

        #region roles persistance ==================
        public bool SaveList( int parentId, int roleId, List<Guid> agentUids, ref SaveStatus status )
        {

            if ( agentUids == null || agentUids.Count == 0 || roleId < 1 )
                return true;

            bool isAllValid = true;
            foreach ( Guid agentUid in agentUids )
            {
                Save( parentId, agentUid, roleId, ref status );
            }
            return isAllValid;
        } //

        public int Save( int parentId, Guid agentUid, int roleId, ref SaveStatus status )
        {
            int newId = 0;
            //assume if all empty, then ignore
            if ( parentId == 0 || !IsValidGuid( agentUid ) || roleId < 1 )
            {
                status.AddError( thisClassName + string.Format( ".Save() Error: invalid request, please provide a valid entityId: {0}, RoleId: {1}, and AgenUtid: {2}.", parentId, roleId, agentUid ) );
                return newId;
            }
            try
            {
                //TODO - update this method - can't exist, as all are deleted
				//20-11-20 mp - where assertion comes from the QA org, then will not have been deleted - but should it?
				//				- here we need to distinguish direction
                if ( AgentEntityRoleExists( parentId, agentUid, roleId ) )
                {
                    //status.AddError( "Error: the selected relationship already exists!" );
                    return 0;
                }
                //TODO - need to handle agent
                Organization org = OrganizationManager.Exists( agentUid );
                if ( org == null || org.Id == 0 )
                {
                    status.AddWarning( string.Format( "AgentRelationship: The agent was not found, for entityId: {0}, AgentId: {1}, RoleId: {2}", parentId, agentUid, roleId ) );
                    LoggingHelper.DoTrace( 6, thisClassName + string.Format( ".Entity_AgentRole_Add the agent was not found, for entityId: {0}, AgentId:{1}, RoleId: {2}", parentId, agentUid, roleId ) );
                    return 0;
                }

                using ( var context = new EntityContext() )
                {
                    //add
                    EM.Entity_AgentRelationship car = new EM.Entity_AgentRelationship();

                    car.EntityId = parentId;
                    car.AgentUid = agentUid;
                    car.RelationshipTypeId = roleId;
                    car.IsInverseRole = true;

                    car.Created = System.DateTime.Now;
                    car.LastUpdated = System.DateTime.Now;
                    car.RowId = Guid.NewGuid();
                    context.Entity_AgentRelationship.Add( car );

                    // submit the change to database
                    int count = context.SaveChanges();
                    newId = car.Id;

					//add to reindex
					List<string> messages = new List<string>();
					new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, org.Id, 1, ref messages );
				}
            }
            catch ( Exception ex )
            {
                string message = FormatExceptions( ex );
                LoggingHelper.LogError( ex, string.Format( thisClassName + ".Save(). entityId: {0}, agentUid: {1}, roleId: {2} ", parentId, agentUid, roleId ) );
                status.AddError( thisClassName + string.Format( ".Save() Exception:  entityId: {0}, RoleId: {1}, and AgenUtid: {2}. Message: {3}", parentId, roleId, agentUid, message ) );
            }
            return newId;
		}


		/// <summary>
		/// Delete all Entity_AgentRelationship for parent 
		/// </summary>
		/// <param name="parentUid"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public bool DeleteAll( Guid parentUid, ref SaveStatus status )
		{
//			SaveStatus status = new SaveStatus();
			Entity_AgentRelationshipManager mgr = new Entity_AgentRelationshipManager();
			Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				status.AddError( thisClassName + " - Error - the parent entity was not found." );
				return false;
			}
			//do deletes - should this be done here, should be no other prior updates?
			return DeleteAll( parent, ref status );
		}


		/// <summary>
		/// Delete all Entity_AgentRelationship for parent (in preparation for import)
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
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
					var results = context.Entity_AgentRelationship.Where( s => s.EntityId == parent.Id )
					.ToList();
					if ( results == null || results.Count == 0 )
						return true;
					foreach ( var item in results )
					{
						context.Entity_AgentRelationship.Remove( item );
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
					//THIS
					//context.Entity_AgentRelationship.RemoveRange( results );

					//OR THIS??
					//context.Entity_AgentRelationship.RemoveRange( context.Entity_AgentRelationship.Where( s => s.EntityId == parent.Id ) );
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
			} catch( Exception ex)
			{
				var msg = BaseFactory.FormatExceptions( ex );
				LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DeleteAll. ParentType: {0}, baseId: {1}, exception: {2}", parent.EntityType, parent.EntityBaseId, msg ) );
			}
            return isValid;
        }


     

        /// <summary>
        /// Delete a single row for a parent entity and agent
        /// </summary>
        /// <param name="parentUid"></param>
        /// <param name="agentUid">Handles a nullable Guid (ex for an owing organzation)</param>
        /// <param name="roleId"></param>
        /// <param name="statusMessage"></param>
        /// <returns></returns>
        public bool Delete( Guid parentUid, Guid? agentUid, int roleId, ref string statusMessage )
        {
            Guid current = ( Guid )agentUid;
            return Delete( parentUid, current, roleId, ref statusMessage );
        }
        /// <summary>
        /// Delete a single row for a parent entity and agent
        /// </summary>
        /// <param name="parentUid"></param>
        /// <param name="agentUid"></param>
        /// <param name="roleId"></param>
        /// <param name="statusMessage"></param>
        /// <returns></returns>
        public bool Delete( Guid parentUid, Guid agentUid, int roleId, ref string statusMessage )
        {
            bool isValid = false;
            Entity parent = EntityManager.GetEntity( parentUid );

            using ( var context = new EntityContext() )
            {
                if ( roleId == 0 || !IsValidGuid( parentUid ) || !IsValidGuid( agentUid ) )
                {
                    statusMessage = "Error - missing identifiers, please provide proper keys.";
                    return false;
                }

                EM.Entity_AgentRelationship efEntity =
                    context.Entity_AgentRelationship.FirstOrDefault( s => s.EntityId == parent.Id
                        && s.AgentUid == agentUid
                        && s.RelationshipTypeId == roleId );
                if ( efEntity != null && efEntity.Id > 0 )
                {
                    statusMessage = string.Format( "Removed Role of {0} from {1} #{2}", efEntity.RelationshipTypeId, efEntity.Entity.EntityTypeId, efEntity.Entity.EntityBaseId );

                    context.Entity_AgentRelationship.Remove( efEntity );

                    int count = context.SaveChanges();
                    if ( count > 0 )
                    {
                        isValid = true;
                    }
                }
                else
                {
                    statusMessage = string.Format( "Agent role record was not found: {0}", roleId );
                    isValid = false;
                }
            }

            return isValid;
        }


		/// <summary>
		/// this seems incorrect!!!!
		/// </summary>
		/// <param name="pParentUid"></param>
		public void ReindexAgentForDeletedArtifact( Guid pParentUid )
		{
			List<String> messages = new List<string>();
			List<Views.Entity_Relationship_AgentSummary> roles = new List<Views.Entity_Relationship_AgentSummary>();
			var reindexMgr = new SearchPendingReindexManager();
			using ( var context = new ViewContext() )
			{
				roles = context.Entity_Relationship_AgentSummary
						.Where( s => s.SourceEntityUid == pParentUid )
						.ToList();

				foreach ( Views.Entity_Relationship_AgentSummary entity in roles )
				{
					//just QA or all? - should be all
					//if ( entity.IsQARole ?? false )
					//{
						//mark agent org for index updates
						reindexMgr.Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, entity.AgentRelativeId, 1, ref messages );
					//}
				}

			}


		} //
        #endregion


        #region == retrieval NEW ==================
        public static List<Organization> GetAllOfferingOrgs( Guid parentUid )
        {

            Organization org = new Organization();
            List<Organization> list = new List<Organization>();

            using ( var context = new EntityContext() )
            {
                var list1 = from item	in context.Entity_AgentRelationship
                            join entity in context.Entity on item.EntityId equals entity.Id
                            join agent	in context.Organization on item.AgentUid equals agent.RowId      
                            //join codes in context.Codes_CredentialAgentRelationship on item.RelationshipTypeId equals codes.Id
                            where entity.EntityUid == parentUid
                                && agent.EntityStateId > 1    //the org can be a reference 
								&& item.RelationshipTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_OFFERED_BY

                            select new
                            {
                                agent.Id,
                                agent.RowId,
                                agent.Name,
                                agent.SubjectWebpage,
                                agent.Description,
                                agent.CTID,
                                agent.EntityStateId,
                                agent.ISQAOrganization
                            };
                var orgList = list1.OrderBy( m => m.Name ).ToList();


                foreach ( var item in orgList )
                {
                    if ( item.EntityStateId < 2 )
                        continue;
                    org = new Organization
                    {
                        Id = item.Id,
                        Name = item.Name,
                        RowId = item.RowId,
                        Description = item.Description,
                        CTID = item.CTID,
						SubjectWebpage = item.SubjectWebpage,
						EntityStateId = (int)item.EntityStateId
                    };
                    list.Add( org );
                }
                return list;
            }
        } //


        #endregion


        #region == retrieval - UNDER REVIEW ==================
        /// <summary>
        /// QA assertions from third party - typically the owning org of the entity 
        /// That is:
        ///		Credential - Entity - Entity.AgentRelationship - RelationshipTypeId by AgentUid 
        ///		
        /// OR have option to get all
        /// </summary>
        /// <param name="targetEntityTypeId"></param>
        /// <param name="targetEntityUid"></param>
        /// <param name="owningOrgId"></param>
        /// <returns></returns>
        public static List<OrganizationRoleProfile> GetAllThirdPartyAssertionsForEntity( int targetEntityTypeId, Guid targetEntityUid, int owningOrgId, bool onlyQAAssertions = false )
		{
			OrganizationRoleProfile orp = new OrganizationRoleProfile();
			List<OrganizationRoleProfile> list = new List<OrganizationRoleProfile>();
			//21-03-22 remove requirement for owningOrgId
			//|| owningOrgId == 0
			if ( targetEntityTypeId == 0 || targetEntityUid == null || !BaseFactory.IsGuidValid( targetEntityUid ) )
				return list;
			LoggingHelper.DoTrace( 5, string.Format( "@@@@@ Entity_AgentRelationshipManager.GetAllQAAssertionsForTarget targetEntityTypeId:{0}, targetEntityUid:{1}, owningOrgId: {2}", targetEntityTypeId, targetEntityUid, owningOrgId ) );
			EnumeratedItem eitem = new EnumeratedItem();

			Guid prevOrgUid = new Guid();
			string prevRoleSource = string.Empty;
			int prevRoleTypeId = 0;
			bool includingPublishedBy = false;
			using ( var context = new EntityContext() )
			{
				//Entity_AgentRelationship is always the AgentUID making an assertion for the related EntityId
				var list1 = from item in context.Entity_AgentRelationship
							join entity in context.Entity on item.EntityId equals entity.Id
							join org in context.Organization on item.AgentUid equals org.RowId		// assertion attributed to org by entity
							join codes in context.Codes_CredentialAgentRelationship on item.RelationshipTypeId equals codes.Id
							where entity.EntityTypeId == targetEntityTypeId && entity.EntityUid == targetEntityUid
								&& org.EntityStateId > 1    //the org can be a reference 
								//&& ( item.RelationshipTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_AccreditedBy 
								//	|| item.RelationshipTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_ApprovedBy 
								//	|| item.RelationshipTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_RegulatedBy 
								//	|| item.RelationshipTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_RecognizedBy 
								//	)

							select new
							{
								item.RelationshipTypeId,
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
								//get list of valid roles - why? Should it just be QA roles
								//	there are no Codes.PropertyValue] for categoryId=13? This just established the Enumeration. The Items are added later. 
								AgentRole = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_ENTITY_AGENT_ROLE ),
								AssertionType = "Third Party",
								IsDirectAssertion =false
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
								CTID = entity.CTID??""
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
							
							//TBD
							//21-07-12 - this code results in skipping the proper creation of eitem. This for the first relationship
							if ( entity.IsQARole ?? false )
							{
								//???
								if (eitem.Id == 0)
								{
									eitem = new EnumeratedItem
									{
										Id = entity.RelationshipTypeId,
										Name = entity.RelationshipType,
										SchemaName = entity.SchemaTag,
										IsQAValue = ( entity.IsQARole ?? false )
									};
								}
								if ( (entity.ISQAOrganization != null && ( bool )entity.ISQAOrganization) || entity.OrgId == owningOrgId )
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

		private static bool AgentEntityRoleExists( int entityId, Guid agentUid, int roleId )
        {
            EntityAgentRelationship item = new EntityAgentRelationship();
            using ( var context = new EntityContext() )
            {
                EM.Entity_AgentRelationship entity = context.Entity_AgentRelationship.FirstOrDefault( s => s.EntityId == entityId
                        && s.AgentUid == agentUid
                        && s.RelationshipTypeId == roleId );
                if ( entity != null && entity.Id > 0 )
                {
                    return true;
                }
            }
            return false;
        }
        //private static bool AgentEntityRoleExists( Guid pParentUid, Guid agentUid, int roleId )
        //{
        //    EntityAgentRelationship item = new EntityAgentRelationship();
        //    Entity parent = EntityManager.GetEntity( pParentUid );
        //    using ( var context = new EntityContext() )
        //    {
        //        EM.Entity_AgentRelationship entity = context.Entity_AgentRelationship.FirstOrDefault( s => s.EntityId == parent.Id
        //                && s.AgentUid == agentUid
        //                && s.RelationshipTypeId == roleId );
        //        if ( entity != null && entity.Id > 0 )
        //        {
        //            return true;
        //        }
        //    }
        //    return false;
        //}


        /// <summary>
        /// Get summary version of roles (using CSV) - for use in lists
        /// It will not return org to org roles like departments, and subsiduaries
        /// </summary>
        /// <param name="pParentUid"></param>
        /// <param name="isParentActor"></param>
        /// <returns></returns>
        //public static List<OrganizationRoleProfile> AgentEntityRole_GetAllSummary( Guid pParentUid, bool isParentActor = false )
        //{
        //    //If parent is actor, then this is a direct role. 
        //    //for ex. if called from assessments, then it is inverse, as the parent is the assessment, and the relate org is the actor
        //    bool isInverseRole = !isParentActor;

        //    OrganizationRoleProfile orp = new OrganizationRoleProfile();
        //    List<OrganizationRoleProfile> list = new List<OrganizationRoleProfile>();
        //    Entity parent = EntityManager.GetEntity( pParentUid );
        //    using ( var context = new ViewContext() )
        //    {
        //        List<DBEntity> agentRoles = context.Entity_AgentRelationshipIdCSV
        //            .Where( s => s.EntityId == parent.Id
        //                 && s.IsInverseRole == isInverseRole )
        //            .ToList();
        //        foreach ( DBEntity entity in agentRoles )
        //        {

        //            orp = new OrganizationRoleProfile();

        //            //warning for purposes of the editor, need to set the agent/object id to the orgId, and rowId from the org
        //            orp.Id = entity.AgentRelativeId;
        //            orp.RowId = ( Guid )entity.AgentUid;

        //            //parent, ex credential, assessment, or org in org-to-org
        //            //Hmm should this be the entityId - need to be consistant
        //            orp.ParentId = entity.EntityId;
        //            //orp.ParentUid = entity.ParentUid;  //this the entityUid
        //            //orp.ParentTypeId = entity.ParentTypeId; //this is wrong, it is the parent of the entity

        //            //useful for compare when doing deletes, and New checks
        //            orp.ActingAgentUid = entity.AgentUid;
        //            orp.ActingAgentId = entity.AgentRelativeId;
        //            orp.ProfileName = entity.AgentName;

        //            //may be included now, but with addition of person, and use of agent, it won't

        //            orp.ActingAgent = new Organization()
        //            {
        //                Id = entity.AgentRelativeId,
        //                RowId = orp.ActingAgentUid,
        //                Name = entity.AgentName,
        //                SubjectWebpage = entity.AgentUrl,
        //                Description = entity.AgentDescription,
        //                ImageUrl = entity.AgentImageUrl
        //            };

        //            //don't need actual roles for summary, but including
        //            orp.AllRoleIds = entity.RoleIds;
        //            orp.AllRoles = entity.Roles.TrimEnd( ',', ' ', '\n' );
        //            //could include roles in profile summary??, particularly if small)

        //            orp.ProfileSummary = entity.AgentName + " {" + orp.AllRoles + "}";
        //            list.Add( orp );
        //        }

        //        if ( list.Count > 0 )
        //        {
        //            var Query = ( from roles in list.OrderBy( p => p.ProfileSummary )
        //                          select roles ).ToList();
        //            list = Query;
        //        }
        //    }
        //    return list;

        //} //

        /// <summary>
        /// Get All AgentEntity roles for the target entity - except where agent is the owner for the entity.
        /// Each OrganizationRoleProfile has a list of the roles, not one role per profile
        /// </summary>
        /// <param name="pParentUid"></param>
        /// <param name="owningAgentUid"></param>
        /// <param name="excludingIfOfferedOnly">If true, and the only role is offeredBy, do not include</param>
        /// <param name="isParentActor"></param>
        /// <returns>Summary list of agents, with list of roles in entity</returns>
        //public static List<OrganizationRoleProfile> AgentEntityRole_GetAllExceptOwnerSummary( Guid pParentUid, Guid owningAgentUid, bool excludingIfOfferedOnly, bool isParentActor = false )
        //{
        //    //If parent is actor, then this is a direct role. 
        //    //for ex. if called from assessments, then it is inverse, as the parent is the assessment, and the relate org is the actor
        //    bool isInverseRole = !isParentActor;

        //    OrganizationRoleProfile orp = new OrganizationRoleProfile();
        //    List<OrganizationRoleProfile> list = new List<OrganizationRoleProfile>();
        //    Entity parent = EntityManager.GetEntity( pParentUid );

        //    using ( var context = new ViewContext() )
        //    {
        //        List<DBEntity> agentRoles = context.Entity_AgentRelationshipIdCSV
        //            .Where( s => s.EntityId == parent.Id
        //                 && s.AgentUid != owningAgentUid
        //                 && s.IsInverseRole == isInverseRole )
        //            .ToList();
        //        foreach ( DBEntity entity in agentRoles )
        //        {

        //            orp = new OrganizationRoleProfile();

        //            //warning for purposes of the editor, need to set the agent/object id to the orgId, and rowId from the org
        //            orp.Id = entity.AgentRelativeId;
        //            orp.RowId = ( Guid )entity.AgentUid;

        //            //parent, ex credential, assessment, or org in org-to-org
        //            //Hmm should this be the entityId - need to be consistant
        //            orp.ParentId = entity.EntityId;

        //            //useful for compare when doing deletes, and New checks
        //            orp.ActingAgentUid = entity.AgentUid;
        //            orp.ActingAgentId = entity.AgentRelativeId;
        //            orp.ProfileName = entity.AgentName;

        //            //may be included now, but with addition of person, and use of agent, it won't

        //            orp.ActingAgent = new Organization()
        //            {
        //                Id = entity.AgentRelativeId,
        //                RowId = orp.ActingAgentUid,
        //                Name = entity.AgentName,
        //                SubjectWebpage = entity.AgentUrl,
        //                Description = entity.AgentDescription,
        //                ImageUrl = entity.AgentImageUrl
        //            };

        //            //don't need actual roles for summary, but including
        //            //skip if only offered by
        //            if ( excludingIfOfferedOnly == false || entity.RoleIds != "7" )
        //            {
        //                orp.AllRoleIds = entity.RoleIds;
        //                orp.AllRoles = entity.Roles;
        //                //could include roles in profile summary??, particularly if small)

        //                orp.ProfileSummary = entity.AgentName;
        //                list.Add( orp );
        //            }

        //        }

        //        if ( list.Count > 0 )
        //        {
        //            var Query = ( from roles in list.OrderBy( p => p.ProfileSummary )
        //                          select roles ).ToList();
        //            list = Query;
        //            //var Query = from roles in credential.OrganizationRole select roles;
        //            //Query = Query.OrderBy( p => p.ProfileSummary );
        //            //credential.OrganizationRole = Query.ToList();
        //        }
        //    }
        //    return list;

        //} //


        /// <summary>
        /// Get All QA Roles for all assets for a credential
        /// </summary>
        /// <param name="pParentUid"></param>
        /// <returns></returns>
        //public static List<ThisEntity> CredentialAssets_GetAllQARoles( int credentialId )
        //{
        //    OrganizationRoleProfile orp = new OrganizationRoleProfile();
        //    List<OrganizationRoleProfile> list = new List<OrganizationRoleProfile>();

        //    using ( var context = new ViewContext() )
        //    {

        //        List<Views.Credential_Assets_AgentRelationship_Totals> agentRoles = context.Credential_Assets_AgentRelationship_Totals
        //            .Where( s => s.CredentialId == credentialId && s.QaCount > 0 )
        //            .ToList();

        //        return CredentialAssets_AgentRelationship_Fill( agentRoles );
        //    }

        //} //

        /// <summary>
        /// Get offered by roles for the credential, and all related assets.
        /// </summary>
        /// <param name="credentialId"></param>
        /// <returns></returns>
        //public static List<OrganizationRoleProfile> CredentialAssets_GetAllOfferedByRoles( int credentialId )
        //{

        //    OrganizationRoleProfile orp = new OrganizationRoleProfile();
        //    List<OrganizationRoleProfile> list = new List<OrganizationRoleProfile>();

        //    using ( var context = new ViewContext() )
        //    {
        //        List<Views.Credential_Assets_AgentRelationship_Totals> agentRoles = context.Credential_Assets_AgentRelationship_Totals
        //            .Where( s => s.CredentialId == credentialId && s.OfferedCount > 0 )
        //            .ToList();

        //        return CredentialAssets_AgentRelationship_Fill( agentRoles );
        //    }
        //} //

        /// <summary>
        /// Get offered by roles for the entity.
        /// TODO - DEVELOP ALTERNATIVE THAT DOES NOT USE Entity_Relationship_AgentSummary
        /// </summary>
        /// <param name="credentialId"></param>
        /// <returns></returns>
        public static List<Organization> GetAllOfferingOrgsOLD( Guid parentUid )
		{

			Organization org = new Organization();
			List<Organization> list = new List<Organization>();

			using ( var context = new ViewContext() )
			{
				List<Views.Entity_Relationship_AgentSummary> agentRoles = context.Entity_Relationship_AgentSummary
					.Where( s => s.SourceEntityUid == parentUid
					&& s.RelationshipTypeId == ROLE_TYPE_OFFERED_BY )
					.ToList();
				foreach ( Views.Entity_Relationship_AgentSummary item in agentRoles )
				{
					if ( item.EntityStateId < 2 )
						continue;
					org = new Organization
					{
						Id = item.AgentRelativeId,
						Name = item.AgentName,
						RowId = item.ActingAgentUid,
						Description = item.AgentDescription,
						CTID = item.CTID
					};
					list.Add( org );
				}
				return list;
			}
		} //



		//private static List<OrganizationRoleProfile> CredentialAssets_AgentRelationship_Fill( List<Views.Credential_Assets_AgentRelationship_Totals> agentRoles )
		//{
		//    List<OrganizationRoleProfile> list = new List<OrganizationRoleProfile>();
		//    OrganizationRoleProfile orp = new OrganizationRoleProfile();
		//    foreach ( Views.Credential_Assets_AgentRelationship_Totals entity in agentRoles )
		//    {

		//        orp = new OrganizationRoleProfile();

		//        //WARNING for purposes of the editor, need to set the agent/object id to the orgId, and rowId from the org
		//        orp.Id = entity.AgentRelativeId;
		//        orp.RowId = entity.AgentUid;

		//        //parent, ex credential, assessment, or org in org-to-org
		//        //Hmm should this be the entityId - need to be consistant
		//        orp.ParentId = entity.AssetEntityId;
		//        orp.ParentType = entity.EntityType;
		//        orp.ParentName = entity.EntityBaseName;
		//        orp.ProfileName = entity.AgentName; //agent name

		//        orp.ActedUponEntityUid = entity.EntityUid;
		//        orp.ActedUponEntityId = entity.AssetEntityId;
		//        orp.ActedUponEntity = new Entity()
		//        {
		//            Id = entity.AssetEntityId,
		//            RowId = entity.EntityUid,
		//            EntityBaseName = entity.EntityBaseName
		//        };

		//        //useful for compare when doing deletes, and New checks
		//        orp.ActingAgentUid = entity.AgentUid;
		//        orp.ActingAgentId = entity.AgentRelativeId;
		//        orp.Description = entity.Description;

		//        //may be included now, but with addition of person, and use of agent, it won't

		//        orp.ActingAgent = new Organization()
		//        {
		//            Id = entity.AgentRelativeId,
		//            RowId = orp.ActingAgentUid,
		//            Name = entity.AgentName//,
		//                                   //SubjectWebpage = entity.AgentUrl
		//        };

		//        orp.ProfileName = orp.ProfileSummary = entity.EntityType + " - " + entity.AgentName;
		//        list.Add( orp );
		//    }
		//    if ( list.Count > 0 )
		//    {
		//        var Query = ( from roles in list.OrderBy( p => p.ProfileSummary )
		//                      select roles ).ToList();
		//        list = Query;
		//        //var Query = from roles in credential.OrganizationRole select roles;
		//        //Query = Query.OrderBy( p => p.ProfileSummary );
		//        //credential.OrganizationRole = Query.ToList();
		//    }

		//    return list;
		//}
		//public static List<OrganizationRoleProfile> AgentEntityRole_GetOwnerSummary( Guid pParentUid, Guid owningAgentUid, bool isParentActor = false )
		//{
		//    //If parent is actor, then this is a direct role. 
		//    //for ex. if called from assessments, then it is inverse, as the parent is the assessment, and the relate org is the actor
		//    bool isInverseRole = !isParentActor;

		//    OrganizationRoleProfile orp = new OrganizationRoleProfile();
		//    List<OrganizationRoleProfile> list = new List<OrganizationRoleProfile>();
		//    Entity parent = EntityManager.GetEntity( pParentUid );

		//    using ( var context = new ViewContext() )
		//    {
		//        List<DBEntity> agentRoles = context.Entity_AgentRelationshipIdCSV
		//            .Where( s => s.EntityId == parent.Id
		//                    && s.AgentUid == owningAgentUid
		//                    && s.IsInverseRole == isInverseRole )
		//            .ToList();
		//        foreach ( DBEntity entity in agentRoles )
		//        {

		//            orp = new OrganizationRoleProfile();

		//            //warning for purposes of the editor, need to set the agent/object id to the orgId, and rowId from the org
		//            orp.Id = entity.AgentRelativeId;
		//            orp.RowId = ( Guid )entity.AgentUid;

		//            //parent, ex credential, assessment, or org in org-to-org
		//            //Hmm should this be the entityId - need to be consistant
		//            orp.ParentId = entity.EntityId;
		//            //orp.ParentUid = entity.ParentUid;  //this the entityUid
		//            //orp.ParentTypeId = entity.ParentTypeId; //this is wrong, it is the parent of the entity

		//            //useful for compare when doing deletes, and New checks
		//            orp.ActingAgentUid = entity.AgentUid;
		//            orp.ActingAgentId = entity.AgentRelativeId;
		//            orp.ProfileName = entity.AgentName;

		//            //may be included now, but with addition of person, and use of agent, it won't

		//            orp.ActingAgent = new Organization()
		//            {
		//                Id = entity.AgentRelativeId,
		//                RowId = orp.ActingAgentUid,
		//                Name = entity.AgentName,
		//                SubjectWebpage = entity.AgentUrl,
		//                Description = entity.AgentDescription,
		//                ImageUrl = entity.AgentImageUrl
		//            };

		//            //don't need actual roles for summary, but including
		//            orp.AllRoleIds = entity.RoleIds;
		//            orp.AllRoles = entity.Roles;
		//            //could include roles in profile summary??, particularly if small)

		//            orp.ProfileSummary = entity.AgentName;
		//            list.Add( orp );
		//        }

		//        if ( list.Count > 0 )
		//        {
		//            var Query = ( from roles in list.OrderBy( p => p.ProfileSummary )
		//                          select roles ).ToList();
		//            list = Query;
		//            //var Query = from roles in credential.OrganizationRole select roles;
		//            //Query = Query.OrderBy( p => p.ProfileSummary );
		//            //credential.OrganizationRole = Query.ToList();
		//        }
		//    }
		//    return list;

		//} //



		public static OrganizationRoleProfile AgentEntityRole_GetAsEnumerationFromCSV( Guid pParentUid, Guid agentUid, bool isInverseRole = true )
        {
			LoggingHelper.DoTrace( LoggingHelper.appMethodEntryTraceLevel, thisClassName + ".AgentEntityRole_GetAsEnumerationFromCSV - entered." );

			OrganizationRoleProfile orp = new OrganizationRoleProfile();

            Entity parent = EntityManager.GetEntity( pParentUid );

            using ( var context = new ViewContext() )
            {
                //there can be inconsistancies, resulting in more than one.
                //So use a list, and log/send email
                List<DBEntity> agentRoles = context.Entity_AgentRelationshipIdCSV
                    .Where( s => s.EntityId == parent.Id
                         && s.AgentUid == agentUid )
                    .ToList();
				if ( agentRoles != null && agentRoles.Any() )
				{
					DBEntity_Fill( orp, agentRoles, true );
				}
            }
            return orp;

        } //

        private static void DBEntity_Fill( OrganizationRoleProfile orp, List<DBEntity> agentRoles, bool fillingEnumerations = true )
        {
            List<OrganizationRoleProfile> list = new List<OrganizationRoleProfile>();
            EnumeratedItem eitem = new EnumeratedItem();
            foreach ( DBEntity entity in agentRoles )
            {

                //warning for purposes of the editor, need to set the agent/object id to the orgId, and rowId from the org
                orp.Id = entity.AgentRelativeId;
                orp.RowId = ( Guid )entity.AgentUid;

                //parent, ex credential, assessment, or org in org-to-org
                orp.RelatedEntityId = entity.EntityId;
                //orp.ParentUid = entity.ParentUid;
                //orp.ParentTypeId = parent.EntityTypeId;

                orp.ActedUponEntityUid = entity.EntityUid;
                orp.ActedUponEntityId = entity.EntityId;
                orp.ActedUponEntity = new Entity()
                {
                    Id = entity.EntityId,
                    RowId = entity.EntityUid,
                    EntityBaseName = entity.EntityBaseName
                };


                orp.ActingAgentUid = entity.AgentUid;
                orp.ActingAgentId = entity.AgentRelativeId;

                //TODO - do we still need this ==> YES
                orp.ActingAgent = new Organization()
                {
                    Id = entity.AgentRelativeId,
                    RowId = orp.ActingAgentUid,
                    Name = entity.AgentName,
                    SubjectWebpage = entity.AgentUrl,
                    Description = entity.AgentDescription,
                    Image = entity.AgentImageUrl
                };

                orp.ProfileSummary = entity.AgentName;
                orp.ProfileName = entity.AgentName;

                orp.AgentRole = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_ENTITY_AGENT_ROLE );
                orp.AgentRole.ParentId = ( int )entity.EntityBaseId;


                orp.AgentRole.Items = new List<EnumeratedItem>();
                if ( fillingEnumerations )
                {
                    if ( entity.RoleIds != null && entity.RoleIds.Length > 0 )
                    {
                        string[] roles = entity.RoleIds.Split( ',' );

                        foreach ( string role in roles )
                        {
                            eitem = new EnumeratedItem();
                            //??
                            eitem.Id = int.Parse( role );
                            //not used here
                            eitem.RecordId = int.Parse( role );
                            eitem.CodeId = int.Parse( role );
                            eitem.Value = role.Trim();

                            eitem.Selected = true;
                            orp.AgentRole.Items.Add( eitem );
                        }
                    }
                }

                //}
                if ( agentRoles.Count > 1 )
                {
                    //log an exception
                    //==>NO, there can be multiples with the new format, until stabalized. ex. Owned by, offered by, a QA role
                    LoggingHelper.LogError( string.Format( "Entity_AgentRelationshipManager.AgentEntityRole_GetAsEnumeration. Multiple records found where one expected. entity.BaseId: {0}, entity.ParentTypeId: {1}, entity.AgentRelativeId: {2}", entity.EntityBaseId, entity.EntityTypeId, entity.AgentRelativeId ) );
                }
                //break;
            }
        }


        /// <summary>
        /// Get all roles for an entity. 
        /// The flat roles (one entity - role - agent per record) are read and returned as enumerations - fully filled out
		/// ****TODO - this view is too general, includes many left joins
        /// </summary>
        /// <param name="pParentUid"></param>
        /// <param name="isInverseRole"></param>
        /// <returns></returns>
        public static List<OrganizationRoleProfile> AgentEntityRole_GetAll_ToEnumeration( Guid pParentUid, bool isInverseRole )
        {
            OrganizationRoleProfile orp = new OrganizationRoleProfile();
            List<OrganizationRoleProfile> list = new List<OrganizationRoleProfile>();
            EnumeratedItem eitem = new EnumeratedItem();
            int prevAgentId = 0;
            Entity parent = EntityManager.GetEntity( pParentUid );

            using ( var context = new ViewContext() )
            {
                //TODO - this view is too general, includes many left joins
				//23-07-10 mp - updated the view: using Entity_Cache, so removed the several left joins
                List<DBentitySummary> agentRoles = context.Entity_Relationship_AgentSummary
                    .Where( s => s.EntityId == parent.Id
                         && s.IsInverseRole == isInverseRole )
                         .OrderBy( s => s.ActingAgentEntityType )
                         .ThenBy( s => s.AgentName ).ThenBy( s => s.AgentRelativeId )
                         .ThenBy( s => s.SourceToAgentRelationship )
                    .ToList();

                foreach ( DBentitySummary entity in agentRoles )
                {
                    if ( entity.EntityStateId < 2 )
                        continue;
                    //loop until change in agent
                    if ( prevAgentId != entity.AgentRelativeId )
                    {
                        //handle previous fill
                        if ( prevAgentId > 0 )
                            list.Add( orp );

                        prevAgentId = entity.AgentRelativeId;

                        orp = new OrganizationRoleProfile();
                        orp.Id = 0;
                        orp.RelatedEntityId = entity.EntityId;
                        //orp.ParentUid = entity.SourceEntityUid;
                        orp.ParentTypeId = parent.EntityTypeId;

                        orp.ActingAgentUid = entity.ActingAgentUid;
                        orp.ActingAgentId = entity.AgentRelativeId;
                        orp.ActingAgent = new Organization()
                        {
                            Id = entity.AgentRelativeId,
                            RowId = entity.ActingAgentUid,
                            Name = entity.AgentName,
                            SubjectWebpage = entity.AgentUrl,
                            Description = entity.AgentDescription,
                            Image = entity.AgentImageUrl,
                            EntityStateId = entity.EntityStateId,
                            CTID = entity.CTID
                        };

                        orp.ProfileSummary = entity.AgentName;

                        orp.AgentRole = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_ENTITY_AGENT_ROLE );
                        orp.AgentRole.ParentId = entity.AgentRelativeId;

                        orp.AgentRole.Items = new List<EnumeratedItem>();
                    }


                    eitem = new EnumeratedItem();
                    //??always confusion as to whether this should be the Id or RelationshipTypeId
                    eitem.Id = entity.EntityAgentRelationshipId;
                    eitem.CodeId = entity.RelationshipTypeId;

                    eitem.RowId = entity.RowId.ToString();
                    //not used here
                    eitem.RecordId = entity.EntityAgentRelationshipId;
                    
                    //???
                    //eitem.Value = entity.RelationshipTypeId.ToString();
                    //WARNING - the code table uses Accredited by as the title and the latter is actually the reverse (using our common context), so we need to reverse the returned values here 
                    if ( !isInverseRole )
                    {
                        eitem.Name = entity.AgentToSourceRelationship;
                        eitem.SchemaName = entity.ReverseSchemaTag;

                        eitem.ReverseTitle = entity.SourceToAgentRelationship;
                        eitem.ReverseSchemaName = entity.SchemaTag;
                    }
                    else
                    {
                        eitem.Name = entity.SourceToAgentRelationship;
                        eitem.SchemaName = entity.SchemaTag;

                        eitem.ReverseTitle = entity.AgentToSourceRelationship;
                        eitem.ReverseSchemaName = entity.ReverseSchemaTag;
                    }
                    //TODO - if needed	
                    //eitem.Description = entity.RelationshipDescription;

                    eitem.Selected = true;
                    if ( ( bool )entity.IsQARole )
                    {
                        eitem.IsQAValue = true;
                        if ( IsDevEnv() )
                            eitem.Name += " (QA)";
                    }

                    orp.AgentRole.Items.Add( eitem );

                }
                //check for remaining
                if ( prevAgentId > 0 )
                    list.Add( orp );

                if ( list.Count > 0 )
                {
                    var Query = ( from items in list.OrderBy( p => p.ProfileSummary )
                                  select items ).ToList();
                    list = Query;
                }

            }
            return list;

        } //



		/// <summary>
		/// Get all child relationships for an org.
		/// </summary>
		/// <param name="thisOrg"></param>
		/// <returns></returns>
		public static void AgentRole_FillAllChildOrganizations( Organization thisOrg )
		{
			OrganizationRoleProfile p = new OrganizationRoleProfile();
			List<OrganizationRoleProfile> list = new List<OrganizationRoleProfile>();
			thisOrg.OrganizationRole_Dept = new List<OrganizationRoleProfile>();
			thisOrg.OrganizationRole_Subsidiary = new List<OrganizationRoleProfile>();
			//***** TODO - stop using the view: Entity_Relationship_AgentSummary
			List<Views.Entity_Relationship_AgentSummary> roles = new List<DBentitySummary>();
			List<Views.Entity_Relationship_AgentSummary> inverseRoles = new List<DBentitySummary>();
			EnumeratedItem eitem = new EnumeratedItem();
			using ( var context = new EntityContext() )
			{
				//get where thisOrg is the acting agent in EAR, so children are from entity
				var list1 = from org in context.Organization
							join entity in context.Entity					on org.RowId equals entity.EntityUid
							join agent in context.Entity_AgentRelationship	on entity.Id equals agent.EntityId
							join codes in context.Codes_CredentialAgentRelationship on agent.RelationshipTypeId equals codes.Id
							where agent.AgentUid == thisOrg.RowId 
								&& agent.RelationshipTypeId == 22 //parent org relationship
								&& org.EntityStateId > 1
							select new
							{
								agent.RelationshipTypeId,
								agent.IsInverseRole,
								RelationshipType = codes.Title,
								codes.ReverseRelation,
								codes.SchemaTag,
								codes.ReverseSchemaTag,
								org.Id,
								RowId = entity.EntityUid,
								org.Name,
								org.SubjectWebpage,
								org.Description,
								org.CTID,
								org.ImageURL,
								org.EntityStateId
							} ;
				var results = list1.ToList();
				//get where thisOrg is the parent, and child orgs are the EAR.AgentUID
				var list2 = from org in context.Organization
							join agent in context.Entity_AgentRelationship on org.RowId equals agent.AgentUid
							join codes in context.Codes_CredentialAgentRelationship on agent.RelationshipTypeId equals codes.Id
							join entity in context.Entity on agent.EntityId equals entity.Id
							where entity.EntityUid == thisOrg.RowId 
								&& (agent.RelationshipTypeId == 20 || agent.RelationshipTypeId == 21 ) 
								&& org.EntityStateId > 1
							select new
							{
								agent.RelationshipTypeId,
								agent.IsInverseRole,
								RelationshipType = codes.Title,
								codes.ReverseRelation,
								codes.SchemaTag,
								codes.ReverseSchemaTag,
								org.Id,
								RowId = agent.AgentUid,   //should this be agent.AgentUid, entity.EntityUid is the parent org rowID
								org.Name,
								org.SubjectWebpage,
								org.Description,
								org.CTID,
								org.ImageURL,
								org.EntityStateId
							};
				var results2 = list2.ToList();

				if ( results2 != null && results2.Count > 0 )
				{
					//21-05-10 mp this approach will miss an org that has a parent in results and a dept
					//var newItems = results2.Where( x => !results.Any( y => x.Id == y.Id ) );
					//foreach ( var item in newItems )
					//{
					//	results.Add( item );
					//}

					foreach ( var item in results2 )
					{
						var exists = results.Where( x => x.Id == item.Id && x.RelationshipTypeId == item.RelationshipTypeId ).ToList();
						if( exists == null || !exists.Any())
							results.Add( item );
					}
				}

				//need to exclude orgs that are already in OrganizationRole_Recipient, especially if RelationshipTypeId in (20,21)
				foreach ( var entity in results )
				{
					p = new OrganizationRoleProfile
					{
						Id = entity.RelationshipTypeId,
						RoleTypeId = entity.RelationshipTypeId
					};
					string relation = "Is Parent Of";
					//this should control which relationship label is displayed (has dept or is dept of)
					p.IsInverseRole = (bool)entity.IsInverseRole;

					p.RelatedEntityId = thisOrg.Id;
					p.ParentTypeId = 2;

					p.ActingAgentUid = thisOrg.RowId;

					//this the current org being displayed
					p.ActingAgent = new Organization()
					{
						Id = thisOrg.Id,
						CTID=thisOrg.CTID,
						RowId = thisOrg.RowId,
						Name = thisOrg.Name,
						SubjectWebpage = thisOrg.SubjectWebpage,
						Description = thisOrg.Description,
						Image = thisOrg.Image
					};
					//this is the related org
					p.ParticipantAgent = new Organization()
					{
						Id = entity.Id,
						CTID=entity.CTID,
						RowId = entity.RowId,
						Name = entity.Name,
						SubjectWebpage = entity.SubjectWebpage,
						Description = entity.Description,
						Image = entity.ImageURL
					};
					p.ProfileSummary = string.Format( "{0} {1} {2}", thisOrg.Name, relation, entity.Name );

					//???????????????
					p.AgentRole = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_ENTITY_AGENT_ROLE );
					p.AgentRole.ParentId = thisOrg.Id;

					p.AgentRole.Items = new List<EnumeratedItem>();
					eitem = new EnumeratedItem
					{
						Id = entity.RelationshipTypeId,
						RowId = entity.RowId.ToString(),
						//not used here
						//eitem.RecordId = entity.RelationshipTypeId;
						//eitem.CodeId = entity.RelationshipTypeId;
						Name = p.IsInverseRole ? entity.RelationshipType : entity.ReverseRelation,
						SchemaName = entity.SchemaTag,
						ReverseSchemaName = entity.ReverseSchemaTag
					};
					// && ( s.RoleTypeId == 20 || s.RoleTypeId == 21 ) )
					//Note: AgentRole_FillAllSubOrganizations adds entries with relationshiptype of 22
					//21-04-23 mp - will look to eliminate use of the latter, seems wrong
					//				- also consider separating with a new/specific property 
					//		**		- may still want to do this for the current detail page
					var exists = thisOrg.OrganizationRole_Recipient
							.Where( s => s.ActingAgentUid == thisOrg.RowId
									&& s.ParticipantAgent.RowId == entity.RowId )
							.ToList();
					if ( exists == null || !exists.Any() )
					{
						p.AgentRole.Items.Add( eitem );
						thisOrg.OrganizationRole_Recipient.Add( p );
					}
					else
					{
						//doing extra check for the relationshiptypeid
						foreach ( var item in exists )
						{

						}
					}
					//start using this, will need a duplicate check
					//for these, don't need p.AgentRole.Items, as only one role
					if ( entity.RelationshipTypeId == ROLE_TYPE_DEPARTMENT )
					{
						var exists2 = thisOrg.OrganizationRole_Dept
									.Where( s => s.ActingAgentUid == thisOrg.RowId
									&& s.ParticipantAgent.RowId == entity.RowId ).ToList();
						if ( exists2 == null || !exists2.Any() )
						{
							if ( !p.AgentRole.Items.Any() )
								p.AgentRole.Items.Add( eitem );

							thisOrg.OrganizationRole_Dept.Add( p );
						}
					}
					else if ( entity.RelationshipTypeId == ROLE_TYPE_SUBSIDIARY )
					{
						var exists2 = thisOrg.OrganizationRole_Subsidiary
									.Where( s => s.ActingAgentUid == thisOrg.RowId
									&& s.ParticipantAgent.RowId == entity.RowId ).ToList();
						if ( exists2 == null || !exists2.Any() )
						{
							if ( !p.AgentRole.Items.Any() )
								p.AgentRole.Items.Add( eitem );

							thisOrg.OrganizationRole_Subsidiary.Add( p );
						}
					}
				}
			}


		} //

		/*
		/// <summary>
		/// Get all departments and subsiduaries for the parent org
		/// NOTE: the parent org is the agent in the relationships. The parent adds the child to the relationship, so the child is the entity, and the parent is the agent
		/// 21-03-12 mparsons - reactivating this for API
		/// </summary>
		/// <param name="pParentUid"></param>
		/// <param name="roleTypeId">If zero, get both otherwise get specific roles</param>
		/// <returns></returns>
		public static void AgentRole_FillAllSubOrganizations( Organization parent, int roleTypeId )
        {
            OrganizationRoleProfile p = new OrganizationRoleProfile();
            List<OrganizationRoleProfile> list = new List<OrganizationRoleProfile>();
            parent.OrganizationRole_Dept = new List<OrganizationRoleProfile>();
            parent.OrganizationRole_Subsidiary = new List<OrganizationRoleProfile>();
			parent.ParentOrganizations = new List<ThisEntity>();
            List<Views.Entity_Relationship_AgentSummary> roles = new List<DBentitySummary>();
            List<Views.Entity_Relationship_AgentSummary> inverseRoles = new List<DBentitySummary>();
            EnumeratedItem eitem = new EnumeratedItem();
            using ( var context = new ViewContext() )
            {

                {
					//19-05-07 mp - shouldn't this be using ActingAgentUid

					roles = context.Entity_Relationship_AgentSummary
                        .Where( s => s.ActingAgentUid == parent.RowId
                             && (
                                    ( roleTypeId == 0 && (s.RelationshipTypeId == ROLE_TYPE_DEPARTMENT || s.RelationshipTypeId == ROLE_TYPE_SUBSIDIARY))
                                || ( s.RelationshipTypeId == roleTypeId )
                                )
                             )
                             .OrderBy( s => s.RelationshipTypeId ).ThenBy( s => s.AgentName )
                        .ToList();

                }

                foreach ( Views.Entity_Relationship_AgentSummary entity in roles )
                {
                    if ( entity.EntityStateId < 2 )
                        continue;

                    p = new OrganizationRoleProfile();
                    p.Id = entity.EntityAgentRelationshipId;

                    p.RoleTypeId = entity.RelationshipTypeId;
                    string relation = string.Empty;
                    if ( entity.SourceToAgentRelationship != null )
                    {
                        relation = entity.AgentToSourceRelationship;
                    }
                    p.IsInverseRole = entity.IsInverseRole ?? false;
                    //if ( p.IsInverseRole )
                    //    relation = entity.SourceToAgentRelationship;

                    //HACK ALERT
                    //reversing the parent and agent for display
                    //16-10-31 mp - works for display, but still wrong for edit.
     
                    p.RelatedEntityId = entity.EntityId;
                    //p.ParentUid = entity.SourceEntityUid;
                    p.ParentTypeId = entity.SourceEntityTypeId;

                    p.ActingAgentUid = entity.ActingAgentUid;
                    p.ActingAgent = new Organization()
                    {
                        Id = entity.AgentRelativeId,
                        RowId = entity.ActingAgentUid,
                        Name = entity.AgentName,
                        SubjectWebpage = entity.AgentUrl,
                        Description = entity.AgentDescription,
                        Image = entity.AgentImageUrl
                    };
					//actually want participant here - although we don't know actual acting agent
					p.ParticipantAgent = new Organization()
					{
						Id = entity.SourceEntityBaseId,
						RowId = entity.SourceEntityUid,
						Name = entity.SourceEntityName,
						SubjectWebpage = entity.SourceEntityUrl,
						Description = entity.SourceEntityDescription,
						Image = entity.SourceEntityImageUrl
					};

					p.ProfileSummary = string.Format( "{0} {1} {2}", entity.AgentName, relation, entity.SourceEntityName );
            

                    //if ( entity.RelationshipTypeId == ROLE_TYPE_DEPARTMENT )
                    //{
                    //    parent.OrganizationRole_Dept.Add( p );
                    //}
                    //else 
					if ( entity.RelationshipTypeId == ROLE_TYPE_DEPARTMENT )
					{
                        parent.OrganizationRole_Dept.Add( p );
                    } else if ( entity.RelationshipTypeId == ROLE_TYPE_SUBSIDIARY  )
					{
						parent.OrganizationRole_Subsidiary.Add( p );
					}
					else if (  entity.RelationshipTypeId == ROLE_TYPE_PARENT_ORG )
					{
						parent.ParentOrganizations.Add( p );
					}


					//OR
					p.AgentRole = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_ENTITY_AGENT_ROLE );
                    p.AgentRole.ParentId = entity.AgentRelativeId;

                    p.AgentRole.Items = new List<EnumeratedItem>();
					eitem = new EnumeratedItem
					{
						Id = entity.EntityAgentRelationshipId,
						RowId = entity.RowId.ToString(),
						//not used here
						RecordId = entity.EntityAgentRelationshipId,
						CodeId = entity.RelationshipTypeId
					};
					if ( p.IsInverseRole )
                    {
                        eitem.Name = entity.AgentToSourceRelationship;
                        eitem.SchemaName = entity.ReverseSchemaTag;
                    }
                    else
                    {
                        eitem.Name = entity.SourceToAgentRelationship;
                        eitem.SchemaName = entity.SchemaTag;
                    }
                    //TODO - if needed	
                    //eitem.Description = entity.RelationshipDescription;

                    eitem.Selected = true;
                    if ( ( bool )entity.IsQARole )
                    {
                        eitem.IsQAValue = true;
                        if ( IsDevEnv() )
                            eitem.Name += " (QA)";
                    }

                    p.AgentRole.Items.Add( eitem );

                    parent.OrganizationRole_Recipient.Add( p );
                    
                    //list.Add( p );
                }

            }
            //return list;

        } //
*/
        /// <summary>
        /// Get Parent organization
        /// The dept/subsiduaries are handled by roles. 
        /// Whereever the interface creates a relationship, the current context (ex credential) is the parent, or source, and the selected agent is the acting agent. The opposite is actually true for depts/subs, but the same appoach was used. 
        /// Any code doing retrieval must accomodate this condition
        /// Use case: a parent org was part of import for this org, but parent org didn't import with the child relationship
        /// </summary>
        /// <param name="thisOrg">Child org</param>
        /// <param name="forEditView"></param>
        public static void AgentRole_GetParentOrganization( Organization thisOrg)
        {
            OrganizationRoleProfile p = new OrganizationRoleProfile();
            List<OrganizationRoleProfile> list = new List<OrganizationRoleProfile>();

            thisOrg.ParentOrganizations = new List<OrganizationRoleProfile>();

            EnumeratedItem eitem = new EnumeratedItem();
            using ( var context = new ViewContext() )
            {
				var childRoles = context.Entity_Relationship_AgentSummary
						.Where( (s => (s.ActingAgentUid == thisOrg.RowId
							 && ( s.RelationshipTypeId == ROLE_TYPE_DEPARTMENT || s.RelationshipTypeId == ROLE_TYPE_SUBSIDIARY ) ) 
							// || ( s.EntityId == child.EntityId && s.RelationshipTypeId == ROLE_TYPE_PARENT_ORG ) 
							 )
							
							 ).Distinct()	//this is probably useless here?
							 .OrderBy( s => s.RelationshipTypeId ).ThenBy( s => s.AgentName )
						.ToList();

				var hasParentRoles = context.Entity_Relationship_AgentSummary
					.Where( ( s => s.EntityId == thisOrg.EntityId && s.RelationshipTypeId == ROLE_TYPE_PARENT_ORG ) )
					.OrderBy( s => s.RelationshipTypeId ).ThenBy( s => s.AgentName )
					.ToList();
				if (hasParentRoles != null && hasParentRoles.Count() > 0)
				{
					var newItems = hasParentRoles.Where( x => !childRoles.Any( y => x.AgentRelativeId == y.SourceEntityBaseId ) );
					foreach ( var item in newItems )
					{
						childRoles.Add( item );
					}
					//childRoles.AddRange( hasParentRoles );
				}

				foreach ( Views.Entity_Relationship_AgentSummary entity in childRoles )
                {
                    if ( entity.EntityStateId < 2 )
                        continue;

                    p = new OrganizationRoleProfile
                    {
                        Id = entity.EntityAgentRelationshipId,
                        RoleTypeId = entity.RelationshipTypeId
                    };
                    string relation = string.Empty;
                    //Department of 
                    if ( entity.SourceToAgentRelationship != null )
                    {
                        relation = entity.SourceToAgentRelationship;
                    }
                    p.IsInverseRole = entity.IsInverseRole ?? false;

                    p.RelatedEntityId = entity.EntityId;
                    p.ParentTypeId = entity.SourceEntityTypeId;

                    //ActingAgent is the parent org, but comes from the source
                    p.ActingAgentUid = entity.ActingAgentUid;
					if ( entity.RelationshipTypeId == 22 )
					{
						p.ActingAgent = new Organization()
						{
							Id = entity.AgentRelativeId,
							RowId = entity.ActingAgentUid,
							Name = entity.AgentName,
							CTID = entity.CTID,
							SubjectWebpage = entity.AgentUrl,
							Description = entity.AgentDescription,
							Image = entity.AgentImageUrl
						};
						//note this is not always formated properly
						p.ProfileSummary = string.Format( "{0} {1} {2}", p.ActingAgent.Name, relation, entity.AgentName );
					}
					else
					{
						p.ActingAgent = new Organization()
						{
							Id = entity.SourceEntityBaseId,
							RowId = entity.SourceEntityUid,
							Name = entity.SourceEntityName,
							CTID = entity.CTID,
							SubjectWebpage = entity.SourceEntityUrl,
							Description = entity.SourceEntityDescription,
							Image = entity.SourceEntityImageUrl
						};
						p.ProfileSummary = string.Format( "{0} is {1} {2}", entity.AgentName, relation, p.ActingAgent.Name );
					}
					//add if not present
					//if (child.se)
					thisOrg.ParentOrganizations.Add( p );

                }
				thisOrg.ParentOrganizations = thisOrg.ParentOrganizations.Distinct().ToList();

			}

        } //

		#endregion

		#region OBSOLETE CREDENTIAL relationships
		//      /// <summary>
		//      /// Get total count of credentials where the provided org is the creator
		//      /// </summary>
		//      /// <param name="orgUid"></param>
		//      /// <returns></returns>
		//      public static int CredentialCount_ForOwningOrg( Guid orgUid )
		//      {
		//          int count = 0;
		//          using ( var context = new EntityContext() )
		//          {
		//              var creds = from cred in context.Credential
		//                          join entity in context.Entity
		//                          on cred.RowId equals entity.EntityUid
		//                          join agent in context.Entity_AgentRelationship
		//                          on entity.Id equals agent.EntityId
		//                          where agent.AgentUid == orgUid
		//                              && agent.RelationshipTypeId == 6
		//                          select cred;
		//              var results = creds.ToList();

		//              if ( results != null && results.Count > 0 )
		//              {
		//                  count = results.Count;
		//              }
		//          }

		//          return count;
		//      }


		//      /// <summary>
		//      /// Get all credentials for the organization, and relationship
		//      /// 17-03-24 mp -	TODO while this should still be valid, there is now a direct relationship. We hesitate as 
		//      ///                 the question has again been raised whether there can be multiple owners.
		//      /// </summary>
		//      /// <param name="orgUid"></param>
		//      /// <returns></returns>
		//      public static List<Credential> Credentials_ForOwningOfferingOrg( Guid orgUid, ref int totalRecords, int maxRecords = 100 )
		//      {
		//          EnumeratedItem eitem = new EnumeratedItem();
		//          List<Credential> list = new List<Credential>();
		//          Credential credential = new Credential();
		//	if ( UtilityManager.GetAppKeyValue( "environment" ) == "production" )
		//	{
		//		//show all for now in production
		//		maxRecords = 0;
		//	}

		//	using ( var context = new EntityContext() )
		//          {
		//              var query = ( from cred in context.Credential
		//                            join entity in context.Entity on cred.RowId equals entity.EntityUid
		//                            join agent in context.Entity_AgentRelationship on entity.Id equals agent.EntityId
		//                            join codes in context.Codes_CredentialAgentRelationship on agent.RelationshipTypeId equals codes.Id
		//                            where agent.AgentUid == orgUid
		//                                && ( agent.RelationshipTypeId == ROLE_TYPE_OWNER || agent.RelationshipTypeId == ROLE_TYPE_OFFERED_BY )


		//                            select new
		//                            {
		//                                RowId = entity.EntityUid,
		//                                Id = cred.Id,
		//                                RelationshipTypeId = agent.RelationshipTypeId,
		//                                RelationshipType = codes.Title,
		//                                ReverseRelation = codes.ReverseRelation,
		//                                Name = cred.Name,
		//                                //AlternateName = cred.AlternateName, OBSOLETE, now from a list
		//                                SubjectWebpage = cred.SubjectWebpage,
		//                                Description = cred.Description,
		//                                CTID = cred.CTID,
		//                                ImageUrl = cred.ImageUrl,
		//                                CredentialTypeId = cred.CredentialTypeId,
		//                                EntityStateId = cred.EntityStateId
		//                            } );
		//              var results = query
		//			.OrderBy( s => s.Name).ThenBy(s => s.Id)
		//			.ToList();

		//		//future will only need this query - should create a separate method
		//		var query2 = ( from cred in context.Credential
		//					  join entity in context.Entity on cred.RowId equals entity.EntityUid
		//					  join agent in context.Entity_AgentRelationship on entity.Id equals agent.EntityId
		//					  join codes in context.Codes_CredentialAgentRelationship on agent.RelationshipTypeId equals codes.Id
		//					  where agent.AgentUid == orgUid
		//						  && ( agent.RelationshipTypeId == ROLE_TYPE_OWNER || agent.RelationshipTypeId == ROLE_TYPE_OFFERED_BY )


		//					  select new
		//					  {
		//						  CTID = cred.CTID
		//					  } );

		//		var results2 = query2.Select(s => s.CTID).Distinct()
		//			.ToList();

		//		if ( results != null && results.Count > 0 )
		//              {
		//			//this total is wrong, as will include owns and offers
		//			totalRecords = results.Count();

		//			totalRecords = results2.Count();

		//			int previousId = 0;
		//                  foreach ( var item in results )
		//                  {
		//				if ( ( int )item.EntityStateId < 2 )
		//					continue;

		//                      if ( previousId != item.Id )
		//                      {
		//                          if ( previousId > 0 )
		//                          {
		//                              list.Add( credential );
		//						if ( maxRecords > 0 && list.Count >= maxRecords )
		//						{
		//							previousId = 0;
		//							break;
		//						}
		//					}
		//                          previousId = item.Id;
		//                          credential = new Credential();

		//                          credential.Id = item.Id;
		//                          credential.RowId = item.RowId;
		//                          credential.Name = item.Name;
		//                          credential.EntityStateId =  (int) item.EntityStateId;
		//                          //TODO - do we need alt name here? Will need to get from references
		//                          //if ( !string.IsNullOrWhiteSpace( item.AlternateName ) )
		//                          //    credential.AlternateName.Add( item.AlternateName );
		//                          credential.SubjectWebpage = item.SubjectWebpage;
		//                          credential.Description = item.Description;
		//                          credential.CTID = item.CTID;
		//                          credential.CredentialType = CredentialManager.GetCredentialType( item.Id );
		//                          credential.Image = item.ImageUrl;
		//                          //do we really need this?
		//                          credential.AudienceLevelType = EntityPropertyManager.FillEnumeration( credential.RowId, CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL );

		//                          credential.CredentialTypeEnum = EntityPropertyManager.FillEnumeration( item.RowId, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_TYPE );

		//                          if ( item.CredentialTypeId.HasValue )
		//                          {
		//                              CodeItem ct = CodesManager.Codes_PropertyValue_Get( item.CredentialTypeId.Value );
		//                              credential.CredentialTypeEnum.Items.Add( new EnumeratedItem() { Id = item.CredentialTypeId.Value, Name = ct.Name, SchemaName = ct.SchemaName } );
		//                          }
		//                          if ( string.IsNullOrWhiteSpace( credential.CTID ) )
		//                          {
		//                              credential.IsReferenceVersion = true;
		//                          }
		//                      }
		//                      eitem = new EnumeratedItem
		//                      {
		//                          Id = item.RelationshipTypeId,
		//                          Name = item.ReverseRelation
		//                          //SchemaName = item.ReverseSchemaTag
		//                      };
		//                      credential.OwnerRoles.Items.Add( eitem );
		//                  }
		//                  if ( previousId > 0 )
		//                  {
		//                      list.Add( credential );
		//                  }
		//              }
		//          }

		//          return list;
		//      }

		////may need the equivalent for assessments and lopps
		//public static int EntityCount_ForOwningOfferingOrg( Guid orgUid, int targetEntityTypeId )
		//{
		//	EnumeratedItem eitem = new EnumeratedItem();
		//	List<AssessmentProfile> list = new List<AssessmentProfile>();
		//	AssessmentProfile ap = new AssessmentProfile();
		//	int totalRecords = 0;
		//	using ( var context = new EntityContext() )
		//	{
		//		var query = ( from entity in context.Entity
		//					  join agent in context.Entity_AgentRelationship on entity.Id equals agent.EntityId
		//					  join org in context.Organization on agent.AgentUid equals org.RowId
		//					  join codes in context.Codes_CredentialAgentRelationship on agent.RelationshipTypeId equals codes.Id
		//					   where agent.AgentUid == orgUid
		//							&& entity.EntityTypeId == targetEntityTypeId
		//							&& org.EntityStateId > 1
		//						   && ( agent.RelationshipTypeId == ROLE_TYPE_OWNER || agent.RelationshipTypeId == ROLE_TYPE_OFFERED_BY )

		//					   select new
		//					   {
		//						   EntityBaseId = entity.EntityBaseId
		//					   } );

		//		var results = query.Select( s => s.EntityBaseId ).Distinct()
		//			.ToList();

		//		if ( results != null && results.Count > 0 )
		//		{
		//			totalRecords = results.Count();

		//		}
		//	}

		//	return totalRecords;
		//}
		#endregion

		#region Counts for relationships
		public static int CredentialCount_ForOwningOfferingOrg( Guid orgUid )
		{
			int totalRecords = 0;
			using ( var context = new EntityContext() )
			{
				var query = ( from entity	in context.Entity
							  join ear		in context.Entity_AgentRelationship on entity.Id equals ear.EntityId
							  join artifact in context.Credential on entity.EntityUid equals artifact.RowId
							  join org		in context.Organization on ear.AgentUid equals org.RowId
							  join codes	in context.Codes_CredentialAgentRelationship on ear.RelationshipTypeId equals codes.Id
							  where ear.AgentUid == orgUid
								   && entity.EntityTypeId == 1
								   && artifact.EntityStateId > 1
								   && org.EntityStateId > 1
								   && ( ear.RelationshipTypeId == ROLE_TYPE_OWNER || ear.RelationshipTypeId == ROLE_TYPE_OFFERED_BY )

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
		//tried more generic, but need actual record to get entityStateId
		//if entity.cache was dependible, could use it
		public static int EntityCount_ForPublishedByOrg( Guid orgUid, int entityTypeId )
		{
			int totalRecords = 0;
			using ( var context = new EntityContext() )
			{
				var query = ( from entity	in context.Entity
							  join ear		in context.Entity_AgentRelationship on entity.Id equals ear.EntityId
							  join artifact in context.Entity_Cache on entity.Id equals artifact.Id
							  join org		in context.Organization on ear.AgentUid equals org.RowId
							  join codes	in context.Codes_CredentialAgentRelationship on ear.RelationshipTypeId equals codes.Id
							  where ear.AgentUid == orgUid
								   && entity.EntityTypeId == entityTypeId
								   && artifact.EntityStateId == 3
								   && org.EntityStateId == 3
								  && ( ear.RelationshipTypeId == ROLE_TYPE_PUBLISHEDBY )

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
		//
		public static int AssessmentCount_ForOwningOfferingOrg( Guid orgUid)
		{
			EnumeratedItem eitem = new EnumeratedItem();
			List<AssessmentProfile> list = new List<AssessmentProfile>();
			AssessmentProfile ap = new AssessmentProfile();
			int totalRecords = 0;
			using ( var context = new EntityContext() )
			{
				var query = ( from entity in context.Entity
							  join agent in context.Entity_AgentRelationship on entity.Id equals agent.EntityId
							  join artifact in context.Assessment on entity.EntityUid equals artifact.RowId
							  join org in context.Organization on agent.AgentUid equals org.RowId
							  join codes in context.Codes_CredentialAgentRelationship on agent.RelationshipTypeId equals codes.Id
							  where agent.AgentUid == orgUid
								   && entity.EntityTypeId == 3
								   && artifact.EntityStateId > 1
								   && org.EntityStateId > 1
								  && ( agent.RelationshipTypeId == ROLE_TYPE_OWNER || agent.RelationshipTypeId == ROLE_TYPE_OFFERED_BY )

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
		public static int LoppCount_ForOwningOfferingOrg( Guid orgUid )
		{
			int totalRecords = 0;
			using ( var context = new EntityContext() )
			{
				var query = ( from loppEntity	in context.Entity
							  join ear			in context.Entity_AgentRelationship on loppEntity.Id equals ear.EntityId
							  join artifact		in context.LearningOpportunity on loppEntity.EntityUid equals artifact.RowId
							  join org			in context.Organization on ear.AgentUid equals org.RowId
							  join codes		in context.Codes_CredentialAgentRelationship on ear.RelationshipTypeId equals codes.Id
							  where ear.AgentUid == orgUid
								   && loppEntity.EntityTypeId == 7
								   && artifact.EntityStateId > 1
								   && org.EntityStateId > 1
								  && ( ear.RelationshipTypeId == ROLE_TYPE_OWNER || ear.RelationshipTypeId == ROLE_TYPE_OFFERED_BY )

							  select new
							  {
								  loppEntity.EntityBaseId
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
		public static int CredentialCount_ForRevokedByOrg( Guid orgUid )
		{
			int totalRecords = 0;
			using ( var context = new EntityContext() )
			{
				var query = ( from entity in context.Entity
							  join agent in context.Entity_AgentRelationship on entity.Id equals agent.EntityId
							  join artifact in context.Credential on entity.EntityUid equals artifact.RowId
							  join org in context.Organization on agent.AgentUid equals org.RowId
							  join codes in context.Codes_CredentialAgentRelationship on agent.RelationshipTypeId equals codes.Id
							  where agent.AgentUid == orgUid
								   && entity.EntityTypeId == 1
								   && artifact.EntityStateId > 1
								   && org.EntityStateId > 1
								  && ( agent.RelationshipTypeId == ROLE_TYPE_RevokedBy )

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
		public static int CredentialCount_ForRenewedByOrg( Guid orgUid )
		{
			int totalRecords = 0;
			using ( var context = new EntityContext() )
			{
				var query = ( from entity in context.Entity
							  join agent in context.Entity_AgentRelationship on entity.Id equals agent.EntityId
							  join artifact in context.Credential on entity.EntityUid equals artifact.RowId
							  join org in context.Organization on agent.AgentUid equals org.RowId
							  join codes in context.Codes_CredentialAgentRelationship on agent.RelationshipTypeId equals codes.Id
							  where agent.AgentUid == orgUid
								   && entity.EntityTypeId == 1
								   && artifact.EntityStateId > 1
								   && org.EntityStateId > 1
								  && ( agent.RelationshipTypeId == ROLE_TYPE_RenewedBy )

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
		public static int CredentialCount_ForRegulatedByOrg( Guid orgUid )
		{
			int totalRecords = 0;
			using ( var context = new EntityContext() )
			{
				var query = ( from entity in context.Entity
							  join agent in context.Entity_AgentRelationship on entity.Id equals agent.EntityId
							  join artifact in context.Credential on entity.EntityUid equals artifact.RowId
							  join org in context.Organization on agent.AgentUid equals org.RowId
							  join codes in context.Codes_CredentialAgentRelationship on agent.RelationshipTypeId equals codes.Id
							  where agent.AgentUid == orgUid
								   && entity.EntityTypeId == 1
								   && artifact.EntityStateId > 1
								   && org.EntityStateId > 1
								  && ( agent.RelationshipTypeId == ROLE_TYPE_RegulatedBy )

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

		#region role codes retrieval ==================
		
        /// <summary>
		/// Get QA roles plus owned and offered
		/// </summary>
		/// <param name="isInverseRole"></param>
		/// <returns></returns>
		public static Enumeration GetCommonPlusQAAgentRoles( bool isInverseRole = true, bool includingPublishedBy = false )
        {
            Enumeration entity = new Enumeration();
			//21-05-10 - removed 30 - published by 
			//21-05-20 - mparsons - oops need 30 for widgets!!!
			var extraRoles = "6 7 ";
			if ( includingPublishedBy )
				extraRoles += " 30";
			using ( var context = new EntityContext() )
            {
                EM.Codes_PropertyCategory category = context.Codes_PropertyCategory
                            .SingleOrDefault( s => s.Id == CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE );

                if ( category != null && category.Id > 0 )
                {
                    entity.Id = category.Id;
                    entity.Name = category.Title;
                    entity.SchemaName = category.SchemaName;
                    entity.Url = category.SchemaUrl;
                    entity.Items = new List<EnumeratedItem>();

                    EnumeratedItem val = new EnumeratedItem();
                    var Query = from P in context.Codes_CredentialAgentRelationship
                        .Where( s => s.IsActive == true
                        && ( s.IsQARole == true || ( extraRoles ).IndexOf( s.Id.ToString() ) > -1 ) )
                                select P;

                    Query = Query.OrderBy( p => p.Title );
                    var results = Query.ToList();

                    foreach ( EM.Codes_CredentialAgentRelationship item in results )
                    {
                        val = new EnumeratedItem();
                        ToMap( item, val, isInverseRole );
                        entity.Items.Add( val );
                    }

                }
            }

            return entity;
        } 

        private static void ToMap( EM.Codes_CredentialAgentRelationship from, EnumeratedItem to, bool isInverseRole = true )
        {
            to.Id = from.Id;
            to.CodeId = from.Id;
            to.Value = from.Id.ToString();//????
            to.Description = from.Description;
            to.SchemaName = from.SchemaTag;

            if ( isInverseRole )
            {
                to.Name = from.ReverseRelation;
            }
            else
            {
                to.Name = from.Title;
                //val.Description = string.Format( "{0} is {1} by this Organization ", entityType, item.Title );
            }

            if ( ( bool )from.IsQARole )
            {
                to.IsQAValue = true;
                if ( IsDevEnv() )
                    to.Name += " (QA)";
            }
        }
        #endregion


    }
}

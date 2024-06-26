using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.ProfileModels;
using workIT.Models.Common;
//using DBEntity = workIT.Data.Views.Credential_AgentRoleIdCSV;
using ThisEntity = workIT.Models.ProfileModels.OrganizationRoleProfile;
using EM = workIT.Data;
using Views = workIT.Data.Views;
using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;
using workIT.Utilities;

namespace workIT.Factories
{
	/// <summary>
	/// THIS IS CONFUSING AS THE TABLE IS NOT Organization.Role, but Entity.AgentRelationship
	/// </summary>
	public class OrganizationRoleManager : BaseFactory
	{
		public static int CredentialToOrgRole_AccreditedBy = 1;
		public static int CredentialToOrgRole_ApprovedBy = 2;
		public static int CredentialToOrgRole_QualityAssuredBy = 3;
		public static int CredentialToOrgRole_ConferredBy = 4;
		public static int CredentialToOrgRole_CreatedBy = 5;
		public static int CredentialToOrgRole_OwnedBy = 6;
		public static int CredentialToOrgRole_OfferedBy = 7;
		public static int CredentialToOrgRole_EndorsedBy = 8;
		public static int CredentialToOrgRole_AssessedBy = 9;
		public static int CredentialToOrgRole_RecognizedBy = 10;
		public static int CredentialToOrgRole_RevokedBy = 11;
		public static int CredentialToOrgRole_RegulatedBy = 12;
		public static int CredentialToOrgRole_RenewalsBy = 13;
		public static int CredentialToOrgRole_UpdatedVersionBy = 14;


		public static int CredentialToOrgRole_MonitoredBy = 15;
		public static int CredentialToOrgRole_VerifiedBy = 16;
		public static int CredentialToOrgRole_ValidatedBy = 17;
		public static int CredentialToOrgRole_Contributor = 18;
		public static int CredentialToOrgRole_WIOAApproved = 19;
        #region
        //public List<OrganizationRoleProfile> GetCombinedRoles( int targetEntityTypeId, Guid entityRowId, int owningOrganizationId, bool onlyQAAssertions = false )
        //{
        //	/*
        //	 * change to get a flat list from each method to enable merging here into OrganizationRole
        //	 * 
        //	 */


        //	//get assertions made via the entity owner (ownedBy, approvedBy) 
        //	var roles = Entity_AgentRelationshipManager.GetAllThirdPartyAssertionsForEntity( targetEntityTypeId, entityRowId, owningOrganizationId, onlyQAAssertions );
        //	//get assertions made by an organization (accredits, approves)
        //	var orgFirstPartyAssertions = Entity_AssertionManager.GetAllFirstPartyAssertionsForTarget( targetEntityTypeId, entityRowId, owningOrganizationId, onlyQAAssertions );
        //	if ( orgFirstPartyAssertions != null && orgFirstPartyAssertions.Any() )
        //	{
        //		//what to do with duplicates?
        //		//foreach ( var item in orgFirstPartyAssertions )
        //		//{
        //		//	//check for same org 
        //		//	var exists = roles.Where( m => m.ActingAgentId == item.ActingAgentId ).ToList();
        //		//}
        //		///OR just add and then loop thru and adjust like old method
        //		roles.AddRange( orgFirstPartyAssertions );
        //	} else
        //	{
        //		//if no first party, just return roles
        //		return roles;
        //	}
        //	if ( roles == null || !roles.Any() )
        //		return roles;
        //	//merge roles
        //	var mergedRoles = roles.OrderBy( m => m.ActingAgent.Id ).ToList();


        //	return roles;
        //}

        /// <summary>
        /// THIS IS CONFUSING AS THE TABLE IS NOT Organization.Role, but Entity.AgentRelationship
        /// </summary>
        /// <param name="targetEntityTypeId"></param>
        /// <param name="targetBaseId"></param>
        /// <param name="assertingAgentId"></param>
        /// <param name="onlyQAAssertions"></param>
        /// <returns></returns>
        public List<OrganizationRoleProfile> GetAllCombinedForTarget( int targetEntityTypeId, int targetBaseId, int assertingAgentId, bool onlyQAAssertions = false )
		{
			OrganizationRoleProfile orp = new OrganizationRoleProfile();
			List<OrganizationRoleProfile> list = new List<OrganizationRoleProfile>();
			//21-03-22 remove requirement for assertingAgent
			//21-07-15 - revisit this?
			//|| assertingAgentId == 0
			if ( targetEntityTypeId == 0 || targetBaseId == 0 )
				return list;
			LoggingHelper.DoTrace( 7, string.Format( "@@@@@ OrganizationRoleManager.GetAllCombinedForTarget targetEntityTypeId:{0}, targetBaseId:{1}, assertingAgent: {2}", targetEntityTypeId, targetBaseId, assertingAgentId ) );
			EnumeratedItem eitem = new EnumeratedItem();

			int prevActingAgentId = 0;
			string prevRoleSource = string.Empty;
			int prevRoleTypeId = 0;
			//would like to enable this in the sandbox
			bool includingPublishedBy = UtilityManager.GetAppKeyValue( "displayingPublishedBy", false );
			
			using ( var context = new EntityContext() )
			{
				//Entity_AgentRelationship is always the AgentUID making an assertion for the related EntityId

				//getting relationships for the target
				var list1 = from item			in context.Entity_AgentRelationship
							join entity			in context.Entity on item.EntityId equals entity.Id
							join actingAgent	in context.Organization on item.AgentUid equals actingAgent.RowId      // assertion attributed to actingAgent by entity
							join codes			in context.Codes_CredentialAgentRelationship on item.RelationshipTypeId equals codes.Id
							where	entity.EntityTypeId == targetEntityTypeId 
								&&	entity.EntityBaseId == targetBaseId
								&&	actingAgent.EntityStateId > 1    //the actingAgent can be a reference 

							select new OrganizationRole
							{
								RoleSource="EntityPublisher", //from POV of entity, accredited by the agent
								RelationshipTypeId=item.RelationshipTypeId,
								RelationshipType = codes.Title,
								IsInverseRole = item.IsInverseRole ?? false, //not sure needed.
								ReverseRelation=codes.ReverseRelation,
								SchemaTag=codes.SchemaTag,
								ReverseSchemaTag=codes.ReverseSchemaTag,
								IsQARole=codes.IsQARole ?? false,

                                //21-09-24 mp - this seems wrong, it is the actingAgent
                                //AssertingOrganizationId = assertingAgentId,
                                AssertingOrganizationId = actingAgent.Id,
                                ActingAgentId = actingAgent.Id,
								ActingAgentUid = actingAgent.RowId,
								ActingAgentName = actingAgent.Name,
								ActingAgentSubjectWebpage=actingAgent.SubjectWebpage,
								ActingAgentDescription = actingAgent.Description,
								ActingAgentCTID=actingAgent.CTID,
								ActingAgentImage=actingAgent.ImageURL,
								ActingAgentEntityStateId=actingAgent.EntityStateId ?? 0,
								ISQAOrganization=actingAgent.ISQAOrganization ?? false
							};

				//get external assertions from Entity_Assertion
				var list2 = from item			in context.Entity_Assertion
							join entity			in context.Entity on item.EntityId equals entity.Id
							join targetEntity	in context.Entity on item.TargetEntityUid equals targetEntity.EntityUid							
							join actingAgent	in context.Organization on entity.EntityUid equals actingAgent.RowId
							join codes			in context.Codes_CredentialAgentRelationship on item.AssertionTypeId equals codes.Id
							where item.TargetEntityTypeId == targetEntityTypeId
								&& targetEntity.EntityBaseId == targetBaseId
								//&& item.TargetEntityUid == targetEntityUid
								&& actingAgent.EntityStateId > 1

							select new OrganizationRole
							{
								RoleSource = "OrganizationPublisher", //from POV of organization (accredits the target entity)
								RelationshipTypeId = item.AssertionTypeId,
								RelationshipType = codes.Title,
								IsInverseRole = item.IsInverseRole, //not sure needed.
								ReverseRelation = codes.ReverseRelation,
								SchemaTag = codes.SchemaTag,
								ReverseSchemaTag = codes.ReverseSchemaTag,
								IsQARole = codes.IsQARole ?? false,
								//21-09-24 mp - this seems wrong, it is the actingAgent
								//AssertingOrganizationId = assertingAgentId,
								AssertingOrganizationId = actingAgent.Id,

								ActingAgentId = actingAgent.Id,
								ActingAgentUid = actingAgent.RowId,
								ActingAgentName = actingAgent.Name,
								ActingAgentSubjectWebpage = actingAgent.SubjectWebpage,
								ActingAgentDescription = actingAgent.Description,
								ActingAgentCTID = actingAgent.CTID,
								ActingAgentImage = actingAgent.ImageURL,
								ActingAgentEntityStateId = actingAgent.EntityStateId ?? 0,
								ISQAOrganization = actingAgent.ISQAOrganization ?? false
							};

				//var agentRoles1 = list1.Concat(list2);
				//sort so org, then relationship, then roleSource, so the entity is first, then the org
				var agentRoles = list1.Concat( list2 ).OrderBy( m => m.ActingAgentName ).ThenBy( m => m.RelationshipTypeId ).ThenBy(x => x.RoleSource ).ToList();


				if ( onlyQAAssertions )
				{
					agentRoles = agentRoles.Where( m => m.RelationshipTypeId == 1 || m.RelationshipTypeId == 2 || m.RelationshipTypeId == 10 || m.RelationshipTypeId == 12 )
						.OrderBy( m => m.ActingAgentName ).ThenBy( m => m.RelationshipTypeId ).ToList();
				}
				//=====================================================
				if ( agentRoles != null && agentRoles.Any() )
				{
					//for this view, we want to retrieve the QA organization info, we already have the target (ie. that is the current context).
					foreach ( var entity in agentRoles )
					{
						if ( !includingPublishedBy && entity.RelationshipTypeId == 30 )
						{
							continue;
						}
						if ( entity.RelationshipTypeId  == 11 )
						{

						}

						//loop until change in entity type?
						if ( prevActingAgentId != entity.ActingAgentId )
						{
							//handle previous fill
							if ( prevActingAgentId > 0 && prevRoleTypeId > 0 )
							{
								if ( eitem.Id > 0 )
									orp.AgentRole.Items.Add( eitem );
								if ( orp.AgentRole.Items.Any() )
									list.Add( orp );
							}

							prevActingAgentId = entity.ActingAgentId;
							prevRoleSource = entity.RoleSource;
							prevRoleTypeId = entity.RelationshipTypeId;

							//set up ORP
							orp = new OrganizationRoleProfile
							{
								Id = 0,
								RelatedEntityId = entity.ActingAgentId,
								ParentTypeId = 2,
								ProfileSummary = entity.ActingAgentName,
								FriendlyName = FormatFriendlyTitle(entity.ActingAgentName),
								//get list of valid roles - why? Should it just be QA roles
								//	there are no Codes.PropertyValue] for categoryId=13? This just established the Enumeration. The Items are added later. 
								AgentRole = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_ENTITY_AGENT_ROLE ),
								AssertionType = entity.RoleSource,// "Third Party", //??don't know yet
								IsDirectAssertion = false
							};
							orp.AgentRole.ParentId = entity.ActingAgentId;

							orp.ActingAgentUid = entity.ActingAgentUid;
							orp.ActingAgentId = entity.ActingAgentId;
							orp.ActingAgent = new Organization()
							{
								Id = entity.ActingAgentId,
								RowId = entity.ActingAgentUid,
								Name = entity.ActingAgentName,
								FriendlyName = FormatFriendlyTitle( entity.ActingAgentName ),
								SubjectWebpage = entity.ActingAgentSubjectWebpage,
								Description = entity.ActingAgentDescription,
								Image = entity.ActingAgentImage,
								EntityStateId = entity.ActingAgentEntityStateId,
								CTID = entity.ActingAgentCTID ?? string.Empty
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
							if ( prevRoleSource != entity.RoleSource )
							{
								//TBD
								if ( entity.IsQARole )
								{
									if ( entity.RoleSource == "OrganizationPublisher" || entity.ActingAgentId == assertingAgentId )
										eitem.IsDirectAssertion = true;
									else
										eitem.IsIndirectAssertion = true;
								}
								//
								prevRoleSource = entity.RoleSource;
								continue;
							}
													
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
							IsQAValue = entity.IsQARole
						};

						prevRoleTypeId = entity.RelationshipTypeId;
						prevRoleSource = entity.RoleSource;
						//**need additional check if from the owning actingAgent!
						if ( entity.IsQARole )
						{
							if ( ( entity.ISQAOrganization ) || entity.ActingAgentId == assertingAgentId )
								eitem.IsDirectAssertion = true;
							else
								eitem.IsIndirectAssertion = true;
						}
					} //

					//check for remaining
					if ( prevActingAgentId > 0 && prevRoleTypeId > 0 )
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

        #endregion
        #region role codes retrieval for enumerations ==================
        /// <summary>
        /// need to dump this and use Codes.CredentialAgentRelationship
        /// </summary>
        /// <param name="isOrgToCredentialRole"></param>
        /// <param name="parentEntityTypeId"></param>
        /// <param name="getAll"></param>
        /// <returns></returns>
        [Obsolete]	
		public static Enumeration GetEntityAgentQAActions( bool isOrgToCredentialRole, int parentEntityTypeId, bool getAll = true )
        {
            return GetEntityToOrgQARolesCodes( isOrgToCredentialRole, 1, getAll, parentEntityTypeId );

        }


		/// <summary>
		/// Get available QA roles for filtering
		/// </summary>
		/// <param name="isInverseRole"></param>
		/// <param name="qaRoleState"></param>
		/// <param name="getAll"></param>
		/// <param name="parentEntityTypeId"></param>
		/// <returns></returns>
		public static Enumeration GetEntityToOrgQARolesCodes( bool isInverseRole,
                    int qaRoleState,
                    bool getAll,
                    int parentEntityTypeId )
        {
            Enumeration entity = new Enumeration();

            using ( var context = new EntityContext() )
            {
                EM.Tables.Codes_PropertyCategory category = context.Codes_PropertyCategory
                            .SingleOrDefault( s => s.Id == CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE );

                if ( category != null && category.Id > 0 )
                {
                    entity.Id = category.Id;
                    entity.Name = category.Title;
                    entity.SchemaName = category.SchemaName;
                    entity.Url = category.SchemaUrl;
                    entity.Items = new List<EnumeratedItem>();

                    EnumeratedItem val = new EnumeratedItem();
                    //18-02-07 all entities have the same QA roles at this time
                    //( parentEntityTypeId == 1 && s.IsCredentialsConnectionType == true ) ||
                    //( parentEntityTypeId == 3 && s.IsAssessmentAgentRole == true ) ||
                    //( parentEntityTypeId == 7 && s.IsLearningOppAgentRole == true )

                    var results = context.Codes_CredentialAgentRelationship
                            .Where( s => s.IsActive == true && (bool)s.IsQARole == true )     
                            .OrderBy( p => p.Title )
                            .ToList();

                    foreach ( EM.Tables.Codes_CredentialAgentRelationship item in results )
                    {
                        val = new EnumeratedItem();
                        val.Id = item.Id;
                        val.CodeId = item.Id;
                        val.Value = item.Id.ToString();//????
                        val.Description = item.Description;

                        if ( isInverseRole )
                        {
                            val.Name = item.ReverseRelation;
                        }
                        else
                        {
                            val.Name = item.Title;
                            //val.Description = string.Format( "{0} is {1} by this Organization ", entityType, item.Title );
                        }

                        if ( ( bool ) item.IsQARole )
                        {
                            val.IsQAValue = true;
                            
                        }
                        if ( parentEntityTypeId == 3 )
                            val.Totals = item.AssessmentTotals ?? 0;
                        else if ( parentEntityTypeId == 2 )
                            val.Totals = item.OrganizationTotals ?? 0;
                        else if ( parentEntityTypeId == 7 )
                            val.Totals = item.LoppTotals ?? 0;
                        else if ( parentEntityTypeId == 1 )
                            val.Totals = item.CredentialTotals ?? 0;
                        if ( IsDevEnv() )
                            val.Name += string.Format(" ({0})", val.Totals);

                        if ( getAll || val.Totals > 0 )
                            entity.Items.Add( val );
                    }

                }
            }

            return entity;
        }

		public static Enumeration GetOrgEntityToNONQARoleCodes( bool isInverseRole, int parentEntityTypeId, bool getAll )
		{
			Enumeration entity = new Enumeration();

			using ( var context = new EntityContext() )
			{
				EM.Tables.Codes_PropertyCategory category = context.Codes_PropertyCategory
							.SingleOrDefault( s => s.Id == CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE );

				if ( category != null && category.Id > 0 )
				{
					entity.Id = category.Id;
					entity.Name = category.Title;
					entity.SchemaName = category.SchemaName;
					entity.Url = category.SchemaUrl;
					entity.Items = new List<EnumeratedItem>();

					EnumeratedItem val = new EnumeratedItem();
					var results = context.Codes_CredentialAgentRelationship
							.Where( s => s.IsActive == true && ( bool )s.IsQARole == false && (bool)s.IsEntityToAgentRole == true )
							.OrderBy( p => p.Title )
							.ToList();

					foreach ( EM.Tables.Codes_CredentialAgentRelationship item in results )
					{
						val = new EnumeratedItem();
						val.Id = item.Id;
						val.CodeId = item.Id;
						val.Value = item.Id.ToString();//????
						val.Description = item.Description;

						if ( isInverseRole )
						{
							val.Name = item.ReverseRelation;
						}
						else
						{
							val.Name = item.Title;
							//val.Description = string.Format( "{0} is {1} by this Organization ", entityType, item.Title );
						}

						if ( ( bool )item.IsQARole )
						{
							val.IsQAValue = true;

						}
						if ( parentEntityTypeId == 3 )
							val.Totals = item.AssessmentTotals ?? 0;
						else if ( parentEntityTypeId == 2 )
							val.Totals = item.OrganizationTotals ?? 0;
						else if ( parentEntityTypeId == 7 )
							val.Totals = item.LoppTotals ?? 0;
						else if ( parentEntityTypeId == 1 )
							val.Totals = item.CredentialTotals ?? 0;
						if ( IsDevEnv() )
							val.Name += string.Format( " ({0})", val.Totals );

						if ( getAll || val.Totals > 0 )
							entity.Items.Add( val );
					}

				}
			}

			return entity;
		}

		/// <summary>
		/// Get roles as enumeration for edit view
		/// </summary>
		/// <param name="isOrgToCredentialRole"></param>
		/// <param name="entityType"></param>
		/// <returns></returns>
		public static Enumeration GetCredentialOrg_AllRoles( bool isInverseRole = true, string entityType = "Credential" )
		{
			return GetEntityToOrgRolesCodes( isInverseRole, 0, true, entityType );
		}
		private static Enumeration GetEntityToOrgRolesCodes( bool isInverseRole,
                    int qaRoleState,
                    bool getAll,
                    string entityType )
        {
            Enumeration entity = new Enumeration();

            using ( var context = new EntityContext() )
            {
                EM.Tables.Codes_PropertyCategory category = context.Codes_PropertyCategory
                            .SingleOrDefault( s => s.Id == CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE );

                if (category != null && category.Id > 0)
                {
                    entity.Id = category.Id;
                    entity.Name = category.Title;
                    entity.SchemaName = category.SchemaName;
                    entity.Url = category.SchemaUrl;
                    entity.Items = new List<EnumeratedItem>();

                    EnumeratedItem val = new EnumeratedItem();
                    //var sortedList = context.Codes_CredentialAgentRelationship
                    //		.Where( s => s.IsActive == true && ( qaOnlyRoles == false || s.IsQARole == true) )
                    //		.OrderBy( x => x.Title )
                    //		.ToList();
                    
                        var Query = from P in context.Codes_CredentialAgentRelationship
                            .Where(s => s.IsActive == true)
                                    select P;
                        if (qaRoleState == 1) //qa only
                        {
                            Query = Query.Where(p => p.IsQARole == true);
                        }
                        else if (qaRoleState == 2)
                        {
                            //this is state is for showing org roles for a credential.
                            //16-06-01 mp - for now show qa and no qa, just skip agent to agent which for now is dept and Subsidiary
                            if (entityType.ToLower() == "credential")
                                Query = Query.Where(p => p.IsEntityToAgentRole == true);
                            else
                                Query = Query.Where(p => p.IsQARole == false && p.IsEntityToAgentRole == true);
                        }
                        else //all
                        {

                        }
                        Query = Query.OrderBy(p => p.Title);
                        var results = Query.ToList();

                        //add Select option
                        //need to only do if for a dropdown, not a checkbox list
                        if (qaRoleState == 1)
                        {
                            //val = new EnumeratedItem();
                            //val.Id = 0;
                            //val.CodeId = val.Id;
                            //val.Name = "Select an Action";
                            //val.Description = string.Empty;
                            //val.SortOrder = 0;
                            //val.Value = val.Id.ToString();
                            //entity.Items.Add( val );
                        }


                        //foreach ( Codes_PropertyValue item in category.Codes_PropertyValue )
                        foreach (EM.Tables.Codes_CredentialAgentRelationship item in results)
                        {
                            val = new EnumeratedItem();
                            val.Id = item.Id;
                            val.CodeId = item.Id;
                            val.Value = item.Id.ToString();//????
                            val.Description = item.Description;

                            if (isInverseRole)
                            {
                                val.Name = item.ReverseRelation;
                                //if ( string.IsNullOrWhiteSpace( entityType ) )
                                //{
                                //	//may not matter
                                //	val.Description = string.Format( "Organization has {0} service.", item.ReverseRelation );
                                //}
                                //else
                                //{
                                //	val.Description = string.Format( "Organization {0} this {1}", item.ReverseRelation, entityType );
                                //}
                            }
                            else
                            {
                                val.Name = item.Title;
                                //val.Description = string.Format( "{0} is {1} by this Organization ", entityType, item.Title );
                            }

                            if ((bool)item.IsQARole)
                            {
                                val.IsQAValue = true;
                                if (IsDevEnv())
                                    val.Name += " (QA)";
                            }

                            entity.Items.Add(val);
                        }

                }
            }

            return entity;
        }
		public static Enumeration GetAgentToAgentRolesCodes( bool isInverseRole = true )
		{
			Enumeration entity = new Enumeration();

			using ( var context = new EntityContext() )
			{
				EM.Tables.Codes_PropertyCategory category = context.Codes_PropertyCategory
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
                        .Where(s => s.IsActive == true && s.IsAgentToAgentRole == true)
                                select P;

                    Query = Query.OrderBy(p => p.Title);
                    var results = Query.ToList();

                    foreach (EM.Tables.Codes_CredentialAgentRelationship item in results)
                    {
                        val = new EnumeratedItem();
                        val.Id = item.Id;
                        val.CodeId = item.Id;
                        val.Value = item.Id.ToString();//????
                        val.Description = item.Description;

                        if (isInverseRole)
                        {
                            val.Name = item.ReverseRelation;

                        }
                        else
                        {
                            val.Name = item.Title;

                        }

                        if ((bool)item.IsQARole)
                        {
                            val.IsQAValue = true;
                            if (IsDevEnv())
                                val.Name += " (QA)";
                        }

                        entity.Items.Add(val);
                    }
                    
				}
			}

			return entity;
		}


		#endregion

	}
	public class OrganizationRole 
	{
		public OrganizationRole()
		{
		}

		//parent had been an entity like credential. this may now be the context, and 
		//will use ActedUponEntityUid separately as the target entity
		public Guid ParentUid { get; set; }

		public Guid ActedUponEntityUid { get; set; }
		public Entity ActedUponEntity { get; set; }
		public int ActedUponEntityId { get; set; }

		public int ParentTypeId { get; set; }

		//agent that states the assertion
		public int AssertingOrganizationId { get; set; }


		//agent that does the assertion
		public int ActingAgentId { get; set; }
		public int ActingAgentEntityStateId { get; set; }
		public Guid ActingAgentUid { get; set; }
		public string ActingAgentName { get; set; }
		public string ActingAgentDescription { get; set; }
		public string ActingAgentCTID { get; set; }
		public string ActingAgentSubjectWebpage { get; set; }
		public string ActingAgentImage { get; set; }
		public bool ISQAOrganization { get; set; }
		/// <summary>
		/// Acting agent is actually for the org making the assertion
		/// </summary>
		//public Organization ActingAgent { get; set; } = new Organization();


		/// <summary>
		/// IsDirect. True - first party (by QA org), false third party (by owning org)
		/// May need a label for both
		/// </summary>
		public bool IsDirectAssertion { get; set; }
		public string AssertionType { get; set; }
		//public bool IsQAActionRole { get; set; }

		public string RoleSource { get; set; }
		public int RelationshipTypeId { get; set; }
		public string RelationshipType { get; set; }
		public string ReverseRelation { get; set; }
		public bool IsInverseRole { get; set; }
		public bool IsQARole { get; set; }
		public string SourceEntityType { get; set; }
		public int SourceEntityStateId { get; set; }
		public string SchemaTag { get; set; }
		public string ReverseSchemaTag { get; set; }


	}
}

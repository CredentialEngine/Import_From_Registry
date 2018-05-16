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

        #region role codes retrieval ==================

        public static Enumeration GetEntityAgentQAActions( bool isOrgToCredentialRole, int parentEntityTypeId, bool getAll = true )
        {
            return GetEntityToOrgQARolesCodes( isOrgToCredentialRole, 1, getAll, parentEntityTypeId );

        }
        private static Enumeration GetEntityToOrgQARolesCodes( bool isInverseRole,
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
                            val.IsSpecialValue = true;
                            
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
                            //val.Description = "";
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
                                val.IsSpecialValue = true;
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
                            val.IsSpecialValue = true;
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


		#region OBSOLETE
		//private static void MapAgentToOrgRole( Credential credential, EM.Credential_AgentRelationship entity )
		//{
		//	ThisEntity p = new ThisEntity();
		//	p.Id = entity.Id;
		//	p.ParentId = entity.CredentialId;
		//	p.Url = entity.URL;
		//	p.Description = entity.Description;

		//	p.ActingAgentId = entity.OrgId;
		//	if ( entity.AgentUid != null )
		//		p.ActingAgentUid = ( Guid ) entity.AgentUid;
		//	p.RoleTypeId = entity.RelationshipTypeId;
		//	string relation = "";
		//	if ( entity.Codes_CredentialAgentRelationship != null )
		//	{
		//		relation = entity.Codes_CredentialAgentRelationship.ReverseRelation;
		//	}

		//	//may be included now, but with addition of person, and use of agent, it won't
		//	if ( entity.Organization != null )
		//	{
		//		OrganizationManager.Organization_ToMap( entity.Organization, p.TargetOrganization );
		//	}
		//	else
		//	{
		//		//get basic?
		//		p.TargetOrganization = OrganizationManager.Organization_Get( entity.OrgId );
		//	}

		//	p.ProfileSummary = string.Format( "{0} {1} this credential", entity.Organization.Name, relation );
		//	if ( IsValidDate( entity.EffectiveDate ) )
		//		p.DateEffective = ( ( DateTime ) entity.EffectiveDate ).ToShortDateString();
		//	else
		//		p.DateEffective = "";

		//	if ( IsValidDate( entity.Created ) )
		//		p.Created = ( DateTime ) entity.Created;
		//	p.CreatedById = entity.CreatedById == null ? 0 : ( int ) entity.CreatedById;
		//	if ( IsValidDate( entity.LastUpdated ) )
		//		p.LastUpdated = ( DateTime ) entity.LastUpdated;
		//	p.LastUpdatedById = entity.LastUpdatedById == null ? 0 : ( int ) entity.LastUpdatedById;

		//	credential.OrganizationRole.Add(p);
		//}		//private bool CredentialOrgRole_Update( int recordId, int agentId, int roleId, int userId, ref string status )
		//{
		//	bool isValid = true;
		//	if ( recordId == 0 )
		//	{
		//		status = "Error: invalid request, please ensure a valid record has been selected.";
		//		return false;
		//	}

		//	//TODO - need to handle agent
		//	Organization org = OrganizationManager.Organization_Get( agentId, false );
		//	if ( org == null || org.Id == 0 )
		//	{
		//		status = "Error: the selected organization was not found!";
		//		LoggingHelper.DoTrace( 5, string.Format( "OrganizationRoleManager.CredentialOrgRole_Update the organization was not found, for credential: {0}, AgentId:{1}, RoleId: {2}", recordId, agentId, roleId ) );
		//		return false;
		//	}

		//	using ( var context = new EntityContext() )
		//	{
		//		EM.Credential_AgentRelationship car = context.Credential_AgentRelationship.FirstOrDefault( s => s.Id == recordId );
		//		if ( car != null && car.Id > 0 )
		//		{
		//			status = "Error: the selected relationship was not found!";
		//			return false;
		//		}

		//		//assign, then check if there were any actual updates
		//		//this credential centric, so leave alone
		//		car.OrgId = agentId;
		//		car.AgentUid = org.RowId;
		//		car.RelationshipTypeId = roleId;

		//		if ( HasStateChanged( context ) )
		//		{
		//			car.LastUpdated = System.DateTime.Now;
		//			car.LastUpdatedById = userId;

		//			// submit the change to database
		//			int count = context.SaveChanges();
		//		}
		//	}

		//	return isValid;
		//}
		/// <summary>
		/// Persist all credential - org relationships
		/// ==> note will want to watch for creator and owner, and ignore as FOR NOW should not be handled here!
		/// </summary>
		/// <param name="credential"></param>
		/// <returns></returns>
		//public bool CredentialAgentRoles_Update( Credential credential, ref string status, ref int count )
		//{
		//	bool isValid = true;
		//	count = 0;
		//	int count1 = 0;
		//	string status1 = "";
		//	isValid = Credential_UpdateOrgRoles( credential, ref status1, ref count1 );
		//	count = count1;
		//	status = status1;
		//	if ( Credential_UpdateQAActions( credential, ref status, ref count1 ) )
		//	{
		//		isValid = false;
		//	}
		//	count += count1;
		//	status += status1;
		//	//List<string> messages = new List<string>();

		//	//if ( credential.OrganizationRole == null )
		//	//	credential.OrganizationRole = new List<ThisEntity>();

		//	//if ( credential.QualityAssuranceAction == null )
		//	//	credential.QualityAssuranceAction = new List<QualityAssuranceActionProfile>();

		//	//using ( var context = new EntityContext() )
		//	//{
		//	//	//loop thru input, check for changes to existing, and for adds
		//	//	foreach ( ThisEntity item in credential.OrganizationRole )
		//	//	{
		//	//		int codeId = CodesManager.GetEnumerationSelection( item.RoleType );
		//	//		if ( codeId == 0 )
		//	//		{
		//	//			isValid = false;
		//	//			messages.Add( string.Format( "Error: a role was not entered. Select a role and try again. AgentId: {0}", item.ActingAgentId ) );
		//	//			continue;
		//	//		}

		//	//		if ( item.Id > 0 )
		//	//		{
		//	//			EM.Credential_AgentRelationship p = context.Credential_AgentRelationship.FirstOrDefault( s => s.Id == item.Id);
		//	//			if ( p != null && p.Id > 0 )
		//	//			{
		//	//				p.CredentialId = credential.Id;
		//	//				//int itemId = CodesManager.GetEnumerationSelection( item.RoleType );
		//	//				//if ( itemId == 0 )
		//	//				//{
		//	//				//	isValid = false;
		//	//				//	messages.Add( string.Format( "Error: a role was not found: {0}", item.ActingAgentId ) );
		//	//				//	continue;
		//	//				//}
		//	//				p.RelationshipTypeId = codeId;
		//	//				//actually need to get the rowId!
		//	//				if ( p.OrgId != item.ActingAgentId )
		//	//				{
		//	//					//NOTE - need to handle agent!!!
		//	//					Organization org = OrganizationManager.Organization_Get( item.OrganizationId, false );
		//	//					if ( org == null || org.Id == 0 )
		//	//					{
		//	//						isValid = false;
		//	//						messages.Add( string.Format( "Error: the selected organization was not found: {0}", item.ActingAgentId ) );
		//	//						continue;
		//	//					}
		//	//					p.AgentUid = org.RowId;
		//	//				}
		//	//				p.OrgId = item.ActingAgentId;
		//	//				if ( HasStateChanged( context ) )
		//	//				{
		//	//					p.LastUpdated = System.DateTime.Now;
		//	//					p.LastUpdatedById = credential.LastUpdatedById;
		//	//					count = context.SaveChanges();
		//	//				}
		//	//			}
		//	//			else
		//	//			{
		//	//				//error should have been found
		//	//				isValid = false;
		//	//				messages.Add( string.Format("Error: the requested role was not found: recordId: {0}", item.Id ));
		//	//			}
		//	//		}
		//	//		else
		//	//		{
		//	//			CredentialOrgRole_Add( credential.Id, item.ActingAgentId, codeId, credential.LastUpdatedById, ref status );
		//	//		}
		//	//	}

		//	//}

		//	return isValid;
		//}

		//public static void FillAllOrgToOrgRoles( EM.Credential fromCredential, Credential credential )
		//{
		//	//start by assuming all roles have been read
		//	if ( fromCredential.Credential_AgentRelationship == null || fromCredential.Credential_AgentRelationship.Count == 0 )
		//	{
		//		return;
		//	}

		//	credential.OrganizationRole = new List<ThisEntity>();
		//	credential.QualityAssuranceAction = new List<QualityAssuranceActionProfile>();

		//	foreach ( EM.Credential_AgentRelationship item in fromCredential.Credential_AgentRelationship )
		//	{
		//		bool isActionType = item.IsActionType == null ? false : ( bool ) item.IsActionType;

		//		if ( item.TargetCredentialId > 0 || isActionType )
		//		{
		//			MapAgentToQAAction( credential, item );
		//		}
		//		else
		//		{
		//			//MapAgentToOrgRole( credential, item );
		//			credential.OrganizationRole.Add( MapAgentToOrgRole( item, "credential" ) );
		//		}


		//		if ( item.RelationshipTypeId == CredentialToOrgRole_CreatedBy )
		//			credential.CreatorOrganizationId = item.OrgId;

		//		if ( item.RelationshipTypeId == CredentialToOrgRole_OwnedBy )
		//			credential.OwnerOrganizationId = item.OrgId;


		//	}


		//}
		// item
		//public static CredentialAgentRelationship AgentCredentialRoleGet( int recordId )
		//{
		//	CredentialAgentRelationship item = new CredentialAgentRelationship();
		//	using ( var context = new EntityContext() )
		//	{
		//		EM.Credential_AgentRelationship entity = context.Credential_AgentRelationship.FirstOrDefault( s => s.Id == recordId );
		//		if ( entity != null && entity.Id > 0 )
		//		{
		//			item.Id = entity.Id;
		//			item.ParentId = entity.CredentialId;
		//			item.OrganizationId = entity.OrgId;
		//			item.RelationshipId = entity.RelationshipTypeId;
		//			item.AgentUid = entity.AgentUid != null ? ( Guid ) entity.AgentUid : Guid.Empty;

		//			//item.TargetOrganization = entity.Organization;
		//		}
		//		else
		//		{
		//			item.Id = 0;
		//		}

		//	}
		//	return item;

		//}

		/// <summary>
		/// Retrieve and fill org roles of creator or owner only
		/// </summary>
		/// <param name="credential"></param>
		//public static void FillOwnerOrgRolesForCredential( Credential credential )
		//{
		//	EnumeratedItem row = new EnumeratedItem();
		//	using ( var context = new EntityContext() )
		//	{
		//		List<Views.CredentialAgentRelationships_Summary> results = context.CredentialAgentRelationships_Summary
		//				.Where( s => s.CredentialId == credential.Id
		//				&& ( s.RelationshipTypeId == CredentialToOrgRole_CreatedBy || s.RelationshipTypeId == CredentialToOrgRole_OwnedBy ) )
		//				.OrderBy( s => s.RelationshipTypeId )
		//				.ToList();

		//		if ( results != null && results.Count > 0 )
		//		{
		//			foreach ( Views.CredentialAgentRelationships_Summary item in results )
		//			{
		//				if ( item.RelationshipTypeId == CredentialToOrgRole_CreatedBy )
		//				{
		//					//create an enumeration, but can be minimal
		//					//credential.CreatorUrl = new Enumeration();
		//					//credential.CreatorUrl.Name = "creatorUrl";
		//					//credential.CreatorUrl.SchemaName = "creatorUrl";
		//					//credential.CreatorUrl.ParentId = credential.Id;
		//					//credential.CreatorUrl.Id = item.OrgId;

		//					credential.CreatorOrganizationId = item.OrgId;

		//					//row = new EnumeratedItem()
		//					//{
		//					//	Id = item.OrgId,
		//					//	Name = item.OrganizationName,
		//					//	Description = item.RelationshipType,
		//					//	Selected = true, 
		//					//	Value = item.OrgId.ToString(),
		//					//	Created = item.Created ?? DateTime.Now,
		//					//	CreatedById = item.CreatedById ?? 0
		//					//};
		//					//credential.CreatorUrl.Items.Add( row );
		//				}
		//				else if ( item.RelationshipTypeId == CredentialToOrgRole_OwnedBy )
		//				{
		//					//credential.OwnerUrl = new Enumeration();
		//					//credential.OwnerUrl.Name = "ownerUrl";
		//					//credential.OwnerUrl.SchemaName = "ownerUrl";
		//					//credential.OwnerUrl.ParentId = credential.Id;
		//					//credential.OwnerUrl.Id = item.OrgId;

		//					credential.OwnerOrganizationId = item.OrgId;

		//					//row = new EnumeratedItem()
		//					//{
		//					//	Id = item.OrgId,
		//					//	Name = item.OrganizationName,
		//					//	Description = item.RelationshipType,
		//					//	Selected = true,
		//					//	Value = item.OrgId.ToString(),
		//					//	Created = item.Created ?? DateTime.Now,
		//					//	CreatedById = item.CreatedById ?? 0
		//					//};
		//					//credential.OwnerUrl.Items.Add( row );
		//				}
		//			}
		//		}
		//	}

		//}//

		#endregion
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.Common;

namespace workIT.Models.ProfileModels
{

	//Note - referred to as AgentRoleProfile in the spreadsheet
	[Serializable]
	public class OrganizationRoleProfile : BaseProfile
	{
		public OrganizationRoleProfile()
		{
			AgentRole = new Enumeration();
			TargetOrganization = new Organization();
			//TargetCredential = new Credential();
			//TargetAssessment = new AssessmentProfile();
			//TargetLearningOpportunity = new LearningOpportunityProfile();
		}

		//parent had been an entity like credential. this may now be the context, and 
		//will use ActedUponEntityUid separately as the target entity
		public Guid ParentUid { get; set; }

		public Guid ActedUponEntityUid { get; set; }
		public Entity ActedUponEntity { get; set; }
		public int ActedUponEntityId { get; set; }

		public int ParentTypeId { get; set; }

		/// <summary>
		/// see if we can eliminate this property?
		/// </summary>
		public int ActingAgentId { get; set; }
		public Guid ActingAgentUid { get; set; }
		/// <summary>
		/// Acting agent is actually for the current context, i.e. the org being displayed
		/// </summary>
		public Organization ActingAgent { get; set; } = new Organization();


		//????how is participant different from acting
		/*
		 * Context should be from perspective of this org (ActingAgent)
		 * Role can be asserted from either side. We want to format the relationships from the POV of thisOrg
		 *	This org has dept
		 *	This org is dept of 
		 * 
		 */
		//public Guid ParticipantAgentUid { get; set; }
		public Organization ParticipantAgent { get; set; }
		/// <summary>
		/// IsDirect. True - first party (by QA org), false third party (by owning org)
		/// May need a label for both
		/// </summary>
		public bool IsDirectAssertion { get; set; }
		public string AssertionType { get; set; }
		//public bool IsQAActionRole { get; set; }
		public Enumeration AgentRole { get; set; }
		//public Enumeration RoleType 
		//{ 
		//	get { return AgentRole; }
		//	set { AgentRole = value; } 
		//}
		public int RoleTypeId { get; set; }
		//public string AllRoleIds { get; set; }
		//public string AllRoles { get; set; }
		public bool IsInverseRole { get; set; }
        public string SourceEntityType { get; set; }
        public int SourceEntityStateId { get; set; }
        //public string SchemaTag { get; set; }
        //public string ReverseSchemaTag { get; set; }

        #region === Targets - where acted upon by the agent ======================

        /// <summary>
        /// TargetCredentialId is the parentId in credential to org roles
        /// The credential acted upon by the agent
        /// </summary>
        public Credential TargetCredential { get; set; }

		/// <summary>
		/// If referenced, indicates that the TargetOrganizationId is the parent in the action - again acted upon by the agent
		/// </summary>
		public Organization TargetOrganization { get; set; }


		/// <summary>
		/// If referenced, indicates that the TargetAssessment is the parent in the action
		/// </summary>
		public AssessmentProfile TargetAssessment { get; set; }

		public LearningOpportunityProfile TargetLearningOpportunity { get; set; }

		#endregion
	}
	//
	//public class OrganizationRoleImport
	//{

	//	public int RoleTypeId { get; set; }
	//	public Guid OrganizationUid { get; set; }

	//}
}

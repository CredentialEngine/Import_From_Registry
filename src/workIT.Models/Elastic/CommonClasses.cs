using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Elastic
{
    //public class AddressLocation
    //{
    //    public double Lat { get; set; }
    //    public double Lon { get; set; }
    //}
    //public class Location
    //{
    //    public double Lat { get; set; }
    //    public double Lon { get; set; }
    //}
    public class IndexReferenceFramework
    {
        public int CategoryId { get; set; }
        //
        public int ReferenceFrameworkItemId { get; set; }
        public string Name { get; set; }
        public string SchemaName { get; set; }
        public string CodeGroup { get; set; }
        public string CodedNotation { get; set; }
        public string CodeTitle
        {
            get
            {
                var codeTitle = Name;
                if ( !string.IsNullOrEmpty( CodedNotation ) )
                    codeTitle = string.Format( "{0} ({1})", Name, CodedNotation );
                return codeTitle;
            }
        }
    }
    public class IndexCompetency
    {
        public string Name { get; set; }
		public string CTID { get; set; }
		public string Description { get; set; }
		//public string CompetencyLabel { get; set; }
		//public string CompetencyText { get; set; }
		//public string Comment { get; set; }
		///competencyCategory
	}
	public class IndexProperty
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public string SchemaName { get; set; }
        //public string Description { get; set; }
    }
    public class IndexSubject
    {
        public string Name { get; set; }
        public string Source { get; set; }
        public int EntityTypeId { get; set; }
        public int ReferenceBaseId { get; set; }
    }
    public class IndexText
    {
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public string Source { get; set; }
    }
	//consider changing to organization connection
    public class IndexQualityAssurance
    {
        public int RelationshipTypeId { get; set; }
        public string SourceToAgentRelationship { get; set; }
        public string AgentToSourceRelationship { get; set; }
        public string AgentUrl { get; set; }
        public int AgentRelativeId { get; set; }
        public string AgentName { get; set; }
        public bool IsQARole { get; set; }
        public int EntityStateId { get; set; }

    }
	public class EntityReference
	{
		public int EntityStateId { get; set; }
		public int EntityTypeId { get; set; }
		public string EntityType { get; set; }
		public int EntityBaseId { get; set; }
		public string EntityName { get; set; }

	}
	public class AgentRelationshipForEntity
	{
		public int OrgId { get; set; }
		public string AgentName { get; set; }
		public string AgentUrl { get; set; }
		public int EntityStateId { get; set; }
		public bool IsDirectAssertion { get; set; }
		//includes all roles QA: 1,2,10,12, others: 6,7,11,13,20,21
		public List<int> RelationshipTypeIds { get; set; } = new List<int>();
		//relationships from the context of the parent entity. For example Accredited By
		public List<string> Relationships { get; set; } = new List<string>();
		//relationships from the context of the agent. For example Accredits
		public List<string> AgentRelationships { get; set; } = new List<string>();
	}

	//public class IndexQualityAssurancePerformed
 //   {
 //       public int AssertionTypeId { get; set; }
 //       public string SourceToAgentRelationship { get; set; }
 //       public string AgentToSourceRelationship { get; set; }
 //       public int TargetEntityTypeId { get; set; }
 //       public int TargetEntityBaseId { get; set; }
 //       public string TargetEntityName { get; set; }
 //       public string TargetEntitySubjectWebpage { get; set; }
 //       public bool IsQARole { get; set; }
 //       public int EntityStateId { get; set; }
 //       public string RoleSource { get; set; }
 //   }
	public class QualityAssurancePerformed
	{
		public int TargetEntityStateId { get; set; }
		public int TargetEntityTypeId { get; set; }
		public int TargetEntityBaseId { get; set; }
		public string TargetEntityName { get; set; }
		public List<int> AssertionTypeIds { get; set; } = new List<int>();

	}
	public class OrganizationRole
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }
        public int RoleId { get; set; }

    }
	public class IndexWidgetTag
	{
		//may make this an integer
		//provider, QA, entity list
		public string Purpose { get; set; }
		public int WidgetId { get; set; }
	}
	public class Connection
    {
        public int Id { get; set; }
        //public string Name { get; set; }
        public string ConnectionType { get; set; }
        public int ConnectionTypeId { get; set; }
        //not sure if will need a list of these
        public int CredentialId { get; set; }
        public string Credential { get; set; }
        public int CredentialOrgId { get; set; }
        public string CredentialOrganization { get; set; }

        public int AssessmentId { get; set; }
        public string Assessment { get; set; }
        public int AssessmentOrganizationId { get; set; }
        public string AssessmentOrganization { get; set; }
        public int LoppId { get; set; }
        public string LearningOpportunity { get; set; }
        public int LoppOrganizationId { get; set; }
        public string LearningOpportunityOrganization { get; set; }

    }
    public class CostItem
    {
        //??
        public int Id { get; set; }

        /// <summary>
        /// Direct, Assessment, Lopp, 
        /// </summary>
        public string Source { get; set; }
        public string DirectCostType { get; set; }
        public decimal Price { get; set; }

    }
}


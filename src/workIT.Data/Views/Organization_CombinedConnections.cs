//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace workIT.Data.Views
{
    using System;
    using System.Collections.Generic;
    
    public partial class Organization_CombinedConnections
    {
        public string roleSource { get; set; }
        public System.Guid OrgUid { get; set; }
        public string Organization { get; set; }
        public int OrgId { get; set; }
        public string AgentDescription { get; set; }
        public string AgentSubjectWebpage { get; set; }
        public string AgentImageUrl { get; set; }
        public string AgentCTID { get; set; }
        public Nullable<int> AgentEntityStateId { get; set; }
        public int EntityId { get; set; }
        public Nullable<int> TargetEntityTypeId { get; set; }
        public System.Guid TargetEntityUid { get; set; }
        public string TargetEntityType { get; set; }
        public Nullable<int> TargetEntityBaseId { get; set; }
        public string TargetEntityName { get; set; }
        public string TargetEntityDescription { get; set; }
        public string TargetEntitySubjectWebpage { get; set; }
        public string TargetEntityImageUrl { get; set; }
        public Nullable<int> TargetEntityStateId { get; set; }
        public int RelationshipTypeId { get; set; }
        public string SourceToAgentRelationship { get; set; }
        public string AgentToSourceRelationship { get; set; }
        public string RelationshipDescription { get; set; }
        public string SchemaTag { get; set; }
        public string ReverseSchemaTag { get; set; }
        public Nullable<bool> IsQARole { get; set; }
        public Nullable<bool> IsOwnerAgentRole { get; set; }
    }
}

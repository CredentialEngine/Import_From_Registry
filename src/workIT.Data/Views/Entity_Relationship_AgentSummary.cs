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
    
    public partial class Entity_Relationship_AgentSummary
    {
        public int EntityAgentRelationshipId { get; set; }
        public System.Guid RowId { get; set; }
        public int EntityId { get; set; }
        public System.Guid SourceEntityUid { get; set; }
        public int SourceEntityTypeId { get; set; }
        public string SourceEntityType { get; set; }
        public int SourceEntityBaseId { get; set; }
        public string SourceEntityName { get; set; }
        public string SourceEntityDescription { get; set; }
        public string SourceEntityUrl { get; set; }
        public string SourceEntityImageUrl { get; set; }
        public int RelationshipTypeId { get; set; }
        public Nullable<bool> IsInverseRole { get; set; }
        public string SourceToAgentRelationship { get; set; }
        public string AgentToSourceRelationship { get; set; }
        public string RelationshipDescription { get; set; }
        public string SchemaTag { get; set; }
        public string ReverseSchemaTag { get; set; }
        public Nullable<bool> IsQARole { get; set; }
        public Nullable<bool> IsOwnerAgentRole { get; set; }
        public System.Guid ActingAgentUid { get; set; }
        public int ActingAgentTypeId { get; set; }
        public string ActingAgentEntityType { get; set; }
        public string AgentName { get; set; }
        public int AgentRelativeId { get; set; }
        public string AgentDescription { get; set; }
        public string AgentUrl { get; set; }
        public string AgentImageUrl { get; set; }
        public string CTID { get; set; }
        public Nullable<System.DateTime> Created { get; set; }
        public Nullable<System.DateTime> LastUpdated { get; set; }
        public int EntityStateId { get; set; }
    }
}

//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace workIT.Data.Tables
{
    using System;
    using System.Collections.Generic;
    
    public partial class Entity_ComponentCondition
    {
        public int Id { get; set; }
        public System.Guid RowId { get; set; }
        public int EntityId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Nullable<int> RequiredNumber { get; set; }
        public string HasConstraint { get; set; }
        public string LogicalOperator { get; set; }
        public string PathwayCTID { get; set; }
        public Nullable<System.DateTime> Created { get; set; }
        public Nullable<System.DateTime> LastUpdated { get; set; }
        public System.Guid SourceRowId { get; set; }
        public string ConditionProperties { get; set; }
        public Nullable<int> RequiredConstraints { get; set; }
    
        public virtual Entity Entity { get; set; }
    }
}

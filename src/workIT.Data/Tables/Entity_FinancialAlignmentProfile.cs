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
    
    public partial class Entity_FinancialAlignmentProfile
    {
        public int Id { get; set; }
        public int EntityId { get; set; }
        public string FrameworkName { get; set; }
        public string Framework { get; set; }
        public string TargetNodeName { get; set; }
        public string TargetNodeDescription { get; set; }
        public string TargetNode { get; set; }
        public string CodedNotation { get; set; }
        public Nullable<System.DateTime> AlignmentDate { get; set; }
        public Nullable<int> AlignmentTypeId { get; set; }
        public Nullable<decimal> Weight { get; set; }
        public Nullable<System.DateTime> Created { get; set; }
        public Nullable<System.DateTime> LastUpdated { get; set; }
        public System.Guid RowId { get; set; }
        public string AlignmentType { get; set; }
    
        public virtual Entity Entity { get; set; }
    }
}

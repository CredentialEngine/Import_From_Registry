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
    
    public partial class Entity_Job
    {
        public int Id { get; set; }
        public int EntityId { get; set; }
        public int JobId { get; set; }
        public Nullable<System.DateTime> Created { get; set; }
        public int RelationshipTypeId { get; set; }
    
        public virtual Entity Entity { get; set; }
        public virtual JobProfile JobProfile { get; set; }
    }
}

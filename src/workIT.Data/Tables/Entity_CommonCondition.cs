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
    
    public partial class Entity_CommonCondition
    {
        public int Id { get; set; }
        public int EntityId { get; set; }
        public int ConditionManifestId { get; set; }
        public Nullable<System.DateTime> Created { get; set; }
    
        public virtual Entity Entity { get; set; }
        public virtual ConditionManifest ConditionManifest { get; set; }
    }
}

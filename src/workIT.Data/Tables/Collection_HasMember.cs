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
    
    public partial class Collection_HasMember
    {
        public int Id { get; set; }
        public int CollectionId { get; set; }
        public int EntityTypeId { get; set; }
        public System.Guid MemberUID { get; set; }
        public Nullable<System.DateTime> Created { get; set; }
    
        public virtual Codes_EntityTypes Codes_EntityTypes { get; set; }
        public virtual Collection Collection { get; set; }
    }
}

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
    
    public partial class Import_Message
    {
        public int Id { get; set; }
        public int ParentId { get; set; }
        public Nullable<System.DateTime> Created { get; set; }
        public Nullable<int> Severity { get; set; }
        public string Message { get; set; }
    
        public virtual Import_Staging Import_Staging { get; set; }
    }
}

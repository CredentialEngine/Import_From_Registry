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
    
    public partial class ProgressionModel_ProgressionLevel
    {
        public int Id { get; set; }
        public System.Guid RowId { get; set; }
        public int ProgressionModelId { get; set; }
        public string PrefLabel { get; set; }
        public string Definition { get; set; }
        public string CTID { get; set; }
        public Nullable<bool> IsTopConcept { get; set; }
        public string Notation { get; set; }
        public string Note { get; set; }
        public string PrecededBy { get; set; }
        public string Precedes { get; set; }
        public Nullable<int> Broader { get; set; }
        public System.DateTime Created { get; set; }
        public System.DateTime LastUpdated { get; set; }
        public string Properties { get; set; }
    
        public virtual ProgressionModel ProgressionModel { get; set; }
    }
}
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
    
    public partial class CompetencyFramework_Competency
    {
        public int Id { get; set; }
        public int CompetencyFrameworkId { get; set; }
        public string CompetencyText { get; set; }
        public string CTID { get; set; }
        public string Comment { get; set; }
        public string CompetencyCategory { get; set; }
        public string CompetencyLabel { get; set; }
        public string CodedNotation { get; set; }
        public string ListID { get; set; }
        public Nullable<decimal> Weight { get; set; }
        public string CredentialRegistryURI { get; set; }
        public Nullable<System.DateTime> DateModified { get; set; }
        public Nullable<System.DateTime> Created { get; set; }
        public System.Guid RowId { get; set; }
    
        public virtual CompetencyFramework CompetencyFramework { get; set; }
    }
}
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
    
    public partial class Collection_Competency
    {
        public int Id { get; set; }
        public int CollectionId { get; set; }
        public string CompetencyText { get; set; }
        public string CTID { get; set; }
        public string CompetencyCategory { get; set; }
        public string CompetencyLabel { get; set; }
        public string CredentialRegistryURI { get; set; }
        public Nullable<System.DateTime> Created { get; set; }
        public System.Guid RowId { get; set; }
        public Nullable<System.DateTime> LastUpdated { get; set; }
        public string CompetencyDetailJson { get; set; }
    
        public virtual Collection Collection { get; set; }
    }
}

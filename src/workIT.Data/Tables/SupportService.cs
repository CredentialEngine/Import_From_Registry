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
    
    public partial class SupportService
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public SupportService()
        {
            this.Entity_HasSupportService = new HashSet<Entity_HasSupportService>();
            this.Entity_IsPartOfSupportService = new HashSet<Entity_IsPartOfSupportService>();
        }
    
        public int Id { get; set; }
        public System.Guid RowId { get; set; }
        public string CTID { get; set; }
        public int EntityStateId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public System.Guid PrimaryAgentUid { get; set; }
        public string SubjectWebpage { get; set; }
        public string AvailableOnlineAt { get; set; }
        public string AvailabilityListing { get; set; }
        public string AlternateName { get; set; }
        public Nullable<System.DateTime> DateEffective { get; set; }
        public Nullable<System.DateTime> ExpirationDate { get; set; }
        public string Identifier { get; set; }
        public Nullable<int> LifeCycleStatusTypeId { get; set; }
        public System.DateTime Created { get; set; }
        public System.DateTime LastUpdated { get; set; }
        public string Keyword { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Entity_HasSupportService> Entity_HasSupportService { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Entity_IsPartOfSupportService> Entity_IsPartOfSupportService { get; set; }
    }
}

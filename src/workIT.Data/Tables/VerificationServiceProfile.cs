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
    
    public partial class VerificationServiceProfile
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public VerificationServiceProfile()
        {
            this.Entity_HasVerificationService = new HashSet<Entity_HasVerificationService>();
            this.Entity_UsesVerificationService = new HashSet<Entity_UsesVerificationService>();
        }
    
        public int Id { get; set; }
        public System.Guid RowId { get; set; }
        public string CTID { get; set; }
        public string Description { get; set; }
        public Nullable<System.DateTime> DateEffective { get; set; }
        public Nullable<bool> HolderMustAuthorize { get; set; }
        public string SubjectWebpage { get; set; }
        public string VerificationDirectory { get; set; }
        public string VerificationMethodDescription { get; set; }
        public string VerificationService { get; set; }
        public Nullable<System.Guid> OfferedBy { get; set; }
        public System.DateTime Created { get; set; }
        public System.DateTime LastUpdated { get; set; }
        public int EntityStateId { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Entity_HasVerificationService> Entity_HasVerificationService { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Entity_UsesVerificationService> Entity_UsesVerificationService { get; set; }
    }
}

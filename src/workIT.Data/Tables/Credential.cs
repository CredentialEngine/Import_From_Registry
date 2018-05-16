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
    
    public partial class Credential
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Credential()
        {
            this.Credential_SummaryCache = new HashSet<Credential_SummaryCache>();
            this.Entity_Credential = new HashSet<Entity_Credential>();
        }
    
        public int Id { get; set; }
        public System.Guid RowId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }
        public Nullable<System.DateTime> EffectiveDate { get; set; }
        public string SubjectWebpage { get; set; }
        public string LatestVersionUrl { get; set; }
        public string ReplacesVersionUrl { get; set; }
        public string ImageUrl { get; set; }
        public Nullable<System.DateTime> Created { get; set; }
        public Nullable<System.DateTime> LastUpdated { get; set; }
        public string CredentialRegistryId { get; set; }
        public string AlternateName { get; set; }
        public string CTID { get; set; }
        public string AvailableOnlineAt { get; set; }
        public string CredentialId { get; set; }
        public string CodedNotation { get; set; }
        public string AvailabilityListing { get; set; }
        public Nullable<System.Guid> OwningAgentUid { get; set; }
        public string ProcessStandards { get; set; }
        public string ProcessStandardsDescription { get; set; }
        public Nullable<System.Guid> CopyrightHolder { get; set; }
        public Nullable<int> InLanguageId { get; set; }
        public Nullable<int> CredentialTypeId { get; set; }
        public Nullable<int> EntityStateId { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Credential_SummaryCache> Credential_SummaryCache { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Entity_Credential> Entity_Credential { get; set; }
    }
}

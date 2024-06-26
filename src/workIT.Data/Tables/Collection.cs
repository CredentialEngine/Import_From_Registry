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
    
    public partial class Collection
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Collection()
        {
            this.Collection_CollectionMember = new HashSet<Collection_CollectionMember>();
            this.Collection_Competency = new HashSet<Collection_Competency>();
            this.Collection_HasMember = new HashSet<Collection_HasMember>();
            this.Entity_Competency = new HashSet<Entity_Competency>();
        }
    
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int EntityStateId { get; set; }
        public string CTID { get; set; }
        public System.Guid OwningAgentUid { get; set; }
        public string SubjectWebpage { get; set; }
        public string CredentialRegistryId { get; set; }
        public Nullable<System.DateTime> Created { get; set; }
        public Nullable<System.DateTime> LastUpdated { get; set; }
        public System.Guid RowId { get; set; }
        public string CollectionGraph { get; set; }
        public string CodedNotation { get; set; }
        public string License { get; set; }
        public Nullable<System.DateTime> DateEffective { get; set; }
        public Nullable<System.DateTime> ExpirationDate { get; set; }
        public int LifeCycleStatusTypeId { get; set; }
        public string InCatalog { get; set; }
        public string LatestVersion { get; set; }
        public string PreviousVersion { get; set; }
        public string NextVersion { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Collection_CollectionMember> Collection_CollectionMember { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Collection_Competency> Collection_Competency { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Collection_HasMember> Collection_HasMember { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Entity_Competency> Entity_Competency { get; set; }
    }
}

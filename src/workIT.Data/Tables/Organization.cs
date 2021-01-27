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
    
    public partial class Organization
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Organization()
        {
            this.Entity_Organization = new HashSet<Entity_Organization>();
            this.ConceptScheme = new HashSet<ConceptScheme>();
        }
    
        public int Id { get; set; }
        public System.Guid RowId { get; set; }
        public Nullable<int> EntityStateId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string SubjectWebpage { get; set; }
        public string AgentPurpose { get; set; }
        public string AgentPurposeDescription { get; set; }
        public string ImageURL { get; set; }
        public string FoundingDate { get; set; }
        public string CTID { get; set; }
        public string CredentialRegistryId { get; set; }
        public string AvailabilityListing { get; set; }
        public string MissionAndGoalsStatement { get; set; }
        public string MissionAndGoalsStatementDescription { get; set; }
        public Nullable<bool> ISQAOrganization { get; set; }
        public Nullable<bool> IsThirdPartyOrganization { get; set; }
        public Nullable<System.DateTime> Created { get; set; }
        public Nullable<System.DateTime> LastUpdated { get; set; }
        public string JsonProperties { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Entity_Organization> Entity_Organization { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ConceptScheme> ConceptScheme { get; set; }
    }
}

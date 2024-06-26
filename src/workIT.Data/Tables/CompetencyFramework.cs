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
    
    public partial class CompetencyFramework
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public CompetencyFramework()
        {
            this.CompetencyFramework_Competency = new HashSet<CompetencyFramework_Competency>();
            this.Entity_Competency = new HashSet<Entity_Competency>();
            this.Entity_CompetencyFramework = new HashSet<Entity_CompetencyFramework>();
        }
    
        public int Id { get; set; }
        public string Name { get; set; }
        public int EntityStateId { get; set; }
        public string CTID { get; set; }
        public string OrganizationCTID { get; set; }
        public string FrameworkUri { get; set; }
        public string SourceUrl { get; set; }
        public Nullable<bool> ExistsInRegistry { get; set; }
        public string CredentialRegistryId { get; set; }
        public Nullable<System.DateTime> Created { get; set; }
        public Nullable<System.DateTime> LastUpdated { get; set; }
        public System.Guid RowId { get; set; }
        public string Description { get; set; }
        public string CompetencyFrameworkGraph { get; set; }
        public string CompetenciesStore { get; set; }
        public int TotalCompetencies { get; set; }
        public string CompetencyFrameworkHierarchy { get; set; }
        public string LatestVersion { get; set; }
        public string PreviousVersion { get; set; }
        public string NextVersion { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<CompetencyFramework_Competency> CompetencyFramework_Competency { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Entity_Competency> Entity_Competency { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Entity_CompetencyFramework> Entity_CompetencyFramework { get; set; }
    }
}

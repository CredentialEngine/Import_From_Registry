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
    
    public partial class PathwayComponent
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public PathwayComponent()
        {
            this.Entity_HasPathwayComponent = new HashSet<Entity_HasPathwayComponent>();
            this.Pathway_ComponentCondition = new HashSet<Pathway_ComponentCondition>();
        }
    
        public int Id { get; set; }
        public System.Guid RowId { get; set; }
        public Nullable<int> ComponentTypeId { get; set; }
        public string CTID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string SubjectWebpage { get; set; }
        public string SourceData { get; set; }
        public string CodedNotation { get; set; }
        public string CredentialType { get; set; }
        public string ComponentCategory { get; set; }
        public string ProgramTerm { get; set; }
        public string HasProgressionLevel { get; set; }
        public string ExternalIdentifier { get; set; }
        public string PathwayCTID { get; set; }
        public Nullable<System.DateTime> Created { get; set; }
        public Nullable<int> CreatedById { get; set; }
        public Nullable<System.DateTime> LastUpdated { get; set; }
        public Nullable<int> LastUpdatedById { get; set; }
        public string Properties { get; set; }
        public int EntityStateId { get; set; }
    
        public virtual Codes_PathwayComponentType Codes_PathwayComponentType { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Entity_HasPathwayComponent> Entity_HasPathwayComponent { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Pathway_ComponentCondition> Pathway_ComponentCondition { get; set; }
    }
}

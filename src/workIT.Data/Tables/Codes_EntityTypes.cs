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
    
    public partial class Codes_EntityTypes
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Codes_EntityTypes()
        {
            this.Entity = new HashSet<Entity>();
            this.Counts_EntityStatistic = new HashSet<Counts_EntityStatistic>();
        }
    
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public Nullable<bool> IsActive { get; set; }
        public string SchemaName { get; set; }
        public Nullable<System.DateTime> Created { get; set; }
        public Nullable<int> Totals { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Entity> Entity { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Counts_EntityStatistic> Counts_EntityStatistic { get; set; }
    }
}

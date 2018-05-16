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
    
    public partial class Codes_PropertyValue
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Codes_PropertyValue()
        {
            this.Entity_Reference = new HashSet<Entity_Reference>();
        }
    
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public Nullable<int> SortOrder { get; set; }
        public Nullable<bool> IsActive { get; set; }
        public string SchemaName { get; set; }
        public string SchemaUrl { get; set; }
        public string ParentSchemaName { get; set; }
        public Nullable<System.DateTime> Created { get; set; }
        public Nullable<int> Totals { get; set; }
        public Nullable<bool> IsSubType1 { get; set; }
    
        public virtual Codes_PropertyCategory Codes_PropertyCategory { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Entity_Reference> Entity_Reference { get; set; }
    }
}

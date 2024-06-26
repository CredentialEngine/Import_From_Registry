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
    
    public partial class Codes_PropertyCategory
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Codes_PropertyCategory()
        {
            this.Codes_PropertyValue = new HashSet<Codes_PropertyValue>();
            this.Reference_FrameworkItem = new HashSet<Reference_FrameworkItem>();
        }
    
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public string SchemaName { get; set; }
        public string SchemaUrl { get; set; }
        public Nullable<System.DateTime> Created { get; set; }
        public string CodeName { get; set; }
        public Nullable<int> InterfaceType { get; set; }
        public string PropertyTableName { get; set; }
        public Nullable<bool> IsConceptScheme { get; set; }
        public string SchemeFor { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Codes_PropertyValue> Codes_PropertyValue { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Reference_FrameworkItem> Reference_FrameworkItem { get; set; }
    }
}

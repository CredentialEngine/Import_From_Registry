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
    
    public partial class Reference_Frameworks
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Reference_Frameworks()
        {
            this.Entity_ReferenceFramework = new HashSet<Entity_ReferenceFramework>();
        }
    
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string CodeGroup { get; set; }
        public string CodedNotation { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string TargetNode { get; set; }
        public Nullable<System.DateTime> Created { get; set; }
        public Nullable<int> ExternalFrameworkId { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Entity_ReferenceFramework> Entity_ReferenceFramework { get; set; }
    }
}

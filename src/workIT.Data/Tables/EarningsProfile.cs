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
    
    public partial class EarningsProfile
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public EarningsProfile()
        {
            this.Entity_EarningsProfile = new HashSet<Entity_EarningsProfile>();
        }
    
        public int Id { get; set; }
        public System.Guid RowId { get; set; }
        public int EntityStateId { get; set; }
        public string CTID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Source { get; set; }
        public Nullable<int> LowEarnings { get; set; }
        public Nullable<int> MedianEarnings { get; set; }
        public Nullable<int> HighEarnings { get; set; }
        public Nullable<int> PostReceiptMonths { get; set; }
        public Nullable<System.DateTime> Created { get; set; }
        public Nullable<System.DateTime> LastUpdated { get; set; }
        public Nullable<System.DateTime> DateEffective { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Entity_EarningsProfile> Entity_EarningsProfile { get; set; }
    }
}
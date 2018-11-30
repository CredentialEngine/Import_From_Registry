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
    
    public partial class Entity_JurisdictionProfile
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Entity_JurisdictionProfile()
        {
            this.GeoCoordinate = new HashSet<GeoCoordinate>();
        }
    
        public int Id { get; set; }
        public int EntityId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Nullable<bool> IsOnlineJurisdiction { get; set; }
        public Nullable<bool> IsGlobalJurisdiction { get; set; }
        public Nullable<int> JProfilePurposeId { get; set; }
        public Nullable<System.DateTime> Created { get; set; }
        public Nullable<System.DateTime> LastUpdated { get; set; }
        public System.Guid RowId { get; set; }
        public Nullable<System.Guid> AssertedByAgentUid { get; set; }
        public Nullable<int> AssertedInTypeId { get; set; }
    
        public virtual Codes_AssertionType Codes_AssertionType { get; set; }
        public virtual Entity Entity { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<GeoCoordinate> GeoCoordinate { get; set; }
    }
}

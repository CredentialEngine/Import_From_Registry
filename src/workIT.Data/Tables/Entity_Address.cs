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
    
    public partial class Entity_Address
    {
        public int Id { get; set; }
        public int EntityId { get; set; }
        public string Name { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string PostOfficeBoxNumber { get; set; }
        public string City { get; set; }
        public string Region { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public Nullable<bool> IsPrimaryAddress { get; set; }
        public Nullable<double> Latitude { get; set; }
        public Nullable<double> Longitude { get; set; }
        public System.Guid RowId { get; set; }
        public System.DateTime Created { get; set; }
        public System.DateTime LastUpdated { get; set; }
        public Nullable<int> CountryId { get; set; }
        public string SubRegion { get; set; }
        public string Description { get; set; }
        public string IdentifierJson { get; set; }
    
        public virtual Entity Entity { get; set; }
    }
}

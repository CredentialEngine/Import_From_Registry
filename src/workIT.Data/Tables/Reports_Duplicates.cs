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
    
    public partial class Reports_Duplicates
    {
        public int Id { get; set; }
        public string ReportType { get; set; }
        public Nullable<System.DateTime> ReportCreationDate { get; set; }
        public string Publisher { get; set; }
        public string PublisherCTID { get; set; }
        public string Organization { get; set; }
        public string OrganizationCTID { get; set; }
        public string EntityType { get; set; }
        public Nullable<int> EntityTypeId { get; set; }
        public string Category { get; set; }
        public string Name { get; set; }
        public string CTID { get; set; }
        public Nullable<int> RecordId { get; set; }
        public string SubjectWebpage { get; set; }
        public string Description { get; set; }
        public Nullable<System.DateTime> DateCreated { get; set; }
        public Nullable<System.DateTime> LastUpdated { get; set; }
        public Nullable<bool> IsProcessed { get; set; }
        public Nullable<bool> ExistsInPublisher { get; set; }
    }
}

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
    
    public partial class Reports_Summary
    {
        public int Id { get; set; }
        public string ReportType { get; set; }
        public Nullable<System.DateTime> NotificationDate { get; set; }
        public string Publisher { get; set; }
        public string PublisherCTID { get; set; }
        public string Organization { get; set; }
        public string OrganizationCTID { get; set; }
        public string EntityType { get; set; }
        public Nullable<int> Totals { get; set; }
    }
}

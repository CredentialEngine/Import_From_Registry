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
    
    public partial class Import_PendingRequest
    {
        public int Id { get; set; }
        public System.DateTime Created { get; set; }
        public bool WasProcessed { get; set; }
        public string Environment { get; set; }
        public Nullable<System.DateTime> EnvelopeLastUpdated { get; set; }
        public Nullable<bool> WasChanged { get; set; }
        public string EnvelopeId { get; set; }
        public string DataOwnerCTID { get; set; }
        public string PublisherCTID { get; set; }
        public string PublishMethodURI { get; set; }
        public string PublishingEntityType { get; set; }
        public string EntityCtid { get; set; }
        public string EntityName { get; set; }
        public Nullable<System.DateTime> ImportedDate { get; set; }
        public Nullable<bool> ImportWasSuccessful { get; set; }
    }
}

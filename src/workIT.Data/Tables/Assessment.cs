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
    
    public partial class Assessment
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Assessment()
        {
            this.Entity_Assessment = new HashSet<Entity_Assessment>();
        }
    
        public int Id { get; set; }
        public System.Guid RowId { get; set; }
        public Nullable<int> EntityStateId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Nullable<System.DateTime> DateEffective { get; set; }
        public string CTID { get; set; }
        public string AssessmentExampleUrl { get; set; }
        public Nullable<int> OrgId { get; set; }
        public Nullable<System.Guid> OwningAgentUid { get; set; }
        public string SubjectWebpage { get; set; }
        public Nullable<System.DateTime> Created { get; set; }
        public Nullable<System.DateTime> LastUpdated { get; set; }
        public string IdentificationCode { get; set; }
        public string AvailableOnlineAt { get; set; }
        public string AvailabilityListing { get; set; }
        public string CredentialRegistryId { get; set; }
        public string CreditHourType { get; set; }
        public Nullable<decimal> CreditHourValue { get; set; }
        public Nullable<int> CreditUnitTypeId { get; set; }
        public string CreditUnitTypeDescription { get; set; }
        public Nullable<decimal> CreditUnitValue { get; set; }
        public string DeliveryTypeDescription { get; set; }
        public string VerificationMethodDescription { get; set; }
        public string AssessmentExampleDescription { get; set; }
        public string AssessmentOutput { get; set; }
        public string ExternalResearch { get; set; }
        public Nullable<bool> HasGroupEvaluation { get; set; }
        public Nullable<bool> HasGroupParticipation { get; set; }
        public Nullable<bool> IsProctored { get; set; }
        public string ProcessStandards { get; set; }
        public string ProcessStandardsDescription { get; set; }
        public string ScoringMethodDescription { get; set; }
        public string ScoringMethodExample { get; set; }
        public string ScoringMethodExampleDescription { get; set; }
        public Nullable<int> InLanguageId { get; set; }
        public string VersionIdentifier { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Entity_Assessment> Entity_Assessment { get; set; }
    }
}

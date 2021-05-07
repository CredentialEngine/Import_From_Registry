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
    
    public partial class Entity_ConditionProfile
    {
        public int Id { get; set; }
        public int EntityId { get; set; }
        public int ConnectionTypeId { get; set; }
        public Nullable<int> ConditionSubTypeId { get; set; }
        public Nullable<System.Guid> AgentUid { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string SubjectWebpage { get; set; }
        public string Experience { get; set; }
        public Nullable<int> MinimumAge { get; set; }
        public Nullable<decimal> YearsOfExperience { get; set; }
        public Nullable<System.DateTime> DateEffective { get; set; }
        public Nullable<System.DateTime> Created { get; set; }
        public Nullable<System.DateTime> LastUpdated { get; set; }
        public System.Guid RowId { get; set; }
        public Nullable<decimal> Weight { get; set; }
        public string CreditHourType { get; set; }
        public Nullable<decimal> CreditHourValue { get; set; }
        public Nullable<int> CreditUnitTypeId { get; set; }
        public string CreditUnitTypeDescription { get; set; }
        public Nullable<decimal> CreditUnitValue { get; set; }
        public Nullable<decimal> CreditUnitMaxValue { get; set; }
        public string SubmissionOfDescription { get; set; }
        public string CreditValue { get; set; }
    
        public virtual Entity Entity { get; set; }
    }
}

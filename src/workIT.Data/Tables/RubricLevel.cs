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
    
    public partial class RubricLevel
    {
        public int Id { get; set; }
        public System.Guid RowId { get; set; }
        public int RubricId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ListID { get; set; }
        public string CodedNotation { get; set; }
        public string HasProgressionLevel { get; set; }
        public Nullable<System.DateTime> Created { get; set; }
        public Nullable<System.DateTime> LastUpdated { get; set; }
    
        public virtual Rubric Rubric { get; set; }
    }
}
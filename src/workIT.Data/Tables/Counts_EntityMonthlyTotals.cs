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
    
    public partial class Counts_EntityMonthlyTotals
    {
        public int Id { get; set; }
        public System.DateTime Period { get; set; }
        public int EntityTypeId { get; set; }
        public int CreatedTotal { get; set; }
        public int UpdatedTotal { get; set; }
        public int DeletedTotal { get; set; }
    }
}

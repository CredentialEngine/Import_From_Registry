//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace workIT.Data.Views
{
    using System;
    using System.Collections.Generic;
    
    public partial class NAIC
    {
        public int Id { get; set; }
        public string NaicsCode { get; set; }
        public string NaicsTitle { get; set; }
        public string Description { get; set; }
        public Nullable<short> CodeLength { get; set; }
        public Nullable<short> NaicsGroup { get; set; }
        public Nullable<int> NacisNumber { get; set; }
        public string URL { get; set; }
        public Nullable<System.DateTime> EffectiveDate { get; set; }
        public Nullable<int> Totals { get; set; }
    }
}

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
    
    public partial class Widget
    {
        public int Id { get; set; }
        public string OrgCTID { get; set; }
        public string OrganizationName { get; set; }
        public string Name { get; set; }
        public string WidgetAlias { get; set; }
        public Nullable<System.DateTime> Created { get; set; }
        public Nullable<System.DateTime> LastUpdated { get; set; }
        public Nullable<int> CreatedById { get; set; }
        public Nullable<int> LastUpdatedById { get; set; }
        public string CustomStyles { get; set; }
        public string LogoUrl { get; set; }
        public string CustomStylesFileName { get; set; }
        public string WidgetStylesUrl { get; set; }
        public string SearchFilters { get; set; }
        public string OwningOrganizationIds { get; set; }
        public string CountryFilters { get; set; }
        public string RegionFilters { get; set; }
        public string CityFilters { get; set; }
        public Nullable<bool> IncludeIfAvailableOnline { get; set; }
    }
}

﻿//------------------------------------------------------------------------------
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
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class workITViews : DbContext
    {
        public workITViews()
            : base("name=workITViews")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<Codes_Currency> Codes_Currency { get; set; }
        public virtual DbSet<NAIC> NAICS { get; set; }
        public virtual DbSet<NAICS_NaicsGroup> NAICS_NaicsGroup { get; set; }
        public virtual DbSet<ONET_SOC> ONET_SOC { get; set; }
        public virtual DbSet<ONET_SOC_JobFamily> ONET_SOC_JobFamily { get; set; }
        public virtual DbSet<CIPCode2010> CIPCode2010 { get; set; }
        public virtual DbSet<CIPCode2010_JobFamily> CIPCode2010_JobFamily { get; set; }
        public virtual DbSet<Codes_Countries> Codes_Countries { get; set; }
        public virtual DbSet<Codes_Language> Codes_Language { get; set; }
        public virtual DbSet<Activity_Summary> Activity_Summary { get; set; }
        public virtual DbSet<EntityProperty_Summary> EntityProperty_Summary { get; set; }
        public virtual DbSet<Entity_Subjects> Entity_Subjects { get; set; }
        public virtual DbSet<Entity_Reference_Summary> Entity_Reference_Summary { get; set; }
        public virtual DbSet<Credential_Assets> Credential_Assets { get; set; }
        public virtual DbSet<Assessment_Summary> Assessment_Summary { get; set; }
        public virtual DbSet<Entity_LearningOpportunity_IsPartOfSummary> Entity_LearningOpportunity_IsPartOfSummary { get; set; }
        public virtual DbSet<CostProfile_SummaryForSearch> CostProfile_SummaryForSearch { get; set; }
        public virtual DbSet<Agent_Summary> Agent_Summary { get; set; }
        public virtual DbSet<Entity_Relationship_AgentSummary> Entity_Relationship_AgentSummary { get; set; }
        public virtual DbSet<Credential_Assets_AgentRelationship_Totals> Credential_Assets_AgentRelationship_Totals { get; set; }
        public virtual DbSet<Entity_AgentRelationshipIdCSV> Entity_AgentRelationshipIdCSV { get; set; }
        public virtual DbSet<Entity_FrameworkItemSummary> Entity_FrameworkItemSummary { get; set; }
        public virtual DbSet<Entity_FrameworkCIPCodeSummary> Entity_FrameworkCIPCodeSummary { get; set; }
        public virtual DbSet<Entity_FrameworkIndustryCodeSummary> Entity_FrameworkIndustryCodeSummary { get; set; }
        public virtual DbSet<Entity_ReferenceFramework_Summary> Entity_ReferenceFramework_Summary { get; set; }
        public virtual DbSet<Entity_ReferenceFramework_Totals> Entity_ReferenceFramework_Totals { get; set; }
        public virtual DbSet<SiteTotalsSummary> SiteTotalsSummaries { get; set; }
        public virtual DbSet<CodesProperty_Counts_ByEntity> CodesProperty_Counts_ByEntity { get; set; }
        public virtual DbSet<CodesProperty_Summary> CodesProperty_Summary { get; set; }
        public virtual DbSet<ExistingCountries_list> ExistingCountries_list { get; set; }
        public virtual DbSet<ExistingCountryRegions_list> ExistingCountryRegions_list { get; set; }
        public virtual DbSet<EntityCompetencyFramework_Items_Summary> EntityCompetencyFramework_Items_Summary { get; set; }
        public virtual DbSet<ExistingRegionCities_list> ExistingRegionCities_list { get; set; }
        public virtual DbSet<Entity_Assertion_Summary> Entity_Assertion_Summary { get; set; }
        public virtual DbSet<Organization_CombinedQAPerformed> Organization_CombinedQAPerformed { get; set; }
        public virtual DbSet<Query_IndianapolisCredentials> Query_IndianapolisCredentials { get; set; }
        public virtual DbSet<Query_IndianaCredentials> Query_IndianaCredentials { get; set; }
        public virtual DbSet<Organization_CombinedConnections> Organization_CombinedConnections { get; set; }
        public virtual DbSet<Organization_Summary> Organization_Summary { get; set; }
    }
}

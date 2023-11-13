
Use credFinder_github
go

/*
**************************************************************************************************************
NOTE: THIS SCRIPT DOESN'T WORK PROPERTLY WITH A 2022 DB - THE INSTEAD TRIGGERS FAIL!!!!

**************************************************************************************************************

*/


/*


--run proc to list tables and rows
exec aspAllDatabaseTableCounts 'credFinder_github', 'base table', 10

-- then do Tasks -> Shrink -> Files for both data log

go
*/

/* 
	Generally:
	- leave Account and AspNet.... tables alone
	- NEVER clear any Codes tables

*/

--clear activity log
truncate table credFinder_github.dbo.ActivityLog
--clear message log
DELETE FROM credFinder_github.dbo.[MessageLog]



DELETE FROM credFinder_github.dbo.Assessment
----------------------------------------------------
truncate table credFinder_github.dbo.[Cache.Organization_ActorRoles] 

--this has be be done after entity.CompetencyCollection
--DELETE FROM credFinder_github.dbo.[Collection.CollectionMember]  
--DELETE FROM credFinder_github.dbo.Collection   

DELETE FROM credFinder_github.dbo.[CompetencyFramework.Competency]
DELETE FROM credFinder_github.dbo.CompetencyFramework   
--
DELETE FROM credFinder_github.dbo.[ConceptScheme.Concept] 
DELETE FROM credFinder_github.dbo.ConceptScheme   
--
DELETE FROM credFinder_github.dbo.ConditionManifest

truncate table credFinder_github.dbo.[ConditionProfile_Competencies_cache]

DELETE FROM credFinder_github.dbo.[CostManifest]

--========================================================================
--
-- set counts properties to zero, need to retain all rows
Update credFinder_github.dbo.[Counts.Assessment_Property] set Total = 0
Update credFinder_github.dbo.[Counts.BenchmarkProperty] set Total = 0
Update credFinder_github.dbo.[Counts.CompetencyFramework_Property] set Total = 0
Update credFinder_github.dbo.[Counts.ConditionManifest_Property] set Total = 0
Update credFinder_github.dbo.[Counts.CostManifest_Property] set Total = 0
Update credFinder_github.dbo.[Counts.Credential_Property] set Total = 0
Update credFinder_github.dbo.[Counts.LearningOpportunity_Property] set Total = 0
Update credFinder_github.dbo.[Counts.Organization_Property] set Total = 0
Update credFinder_github.dbo.[Counts.Pathway_Property] set Total = 0
Update credFinder_github.dbo.[Counts.TransferValue_Property] set Total = 0

Update credFinder_github.dbo.[Counts.EntityMonthlyTotals] set CreatedTotal = 0, UpdatedTotal = 0, DeletedTotal = 0
update credFinder_github.dbo.[Counts.EntityStatistic] set Totals = 0
update credFinder_github.dbo.[Counts.MonthlySiteTotals] set Totals = 0
Update credFinder_github.dbo.[Counts.RegionTotals] Set Totals = 0
update credFinder_github.dbo.[Counts.SiteTotals] set Totals = 0
update credFinder_github.dbo.[Counts.SiteTotals_Staging] set Totals = 0
--========================================================================
--NOTE: need to drop  relationship: FK_Entity.HasResource_Entity_Cache
--		and then re-add
--		23-11-13 - no longer using the latter FK, so don't re add it!
BEGIN TRANSACTION
GO
ALTER TABLE dbo.[Entity.HasResource]
	DROP CONSTRAINT [FK_Entity.HasResource_Entity_Cache]
GO
ALTER TABLE dbo.Entity_Cache SET (LOCK_ESCALATION = TABLE)
GO
COMMIT
GO
--========================================================================

DELETE FROM credFinder_github.dbo.credential

--where EntityStateId < 3
truncate table credFinder_github.dbo.[Credential.SummaryCache]
--

DELETE FROM credFinder_github.dbo.DataProfile
DELETE FROM credFinder_github.dbo.DataSetProfile
DELETE FROM credFinder_github.dbo.DataSetTimeFrame
--
--TODO PLAN TO DROP THESE
DELETE FROM credFinder_github.dbo.EarningsProfile
DELETE FROM credFinder_github.dbo.EmploymentOutcomeProfile
DELETE FROM credFinder_github.dbo.HoldersProfile

--need to delete competency before deleting education framework
DELETE FROM credFinder_github.dbo.[Entity.Competency]
DELETE FROM credFinder_github.dbo.EducationFramework
--======================
--delete entity and via cascade, all child tables
DELETE FROM credFinder_github.dbo.[Entity]

DELETE FROM credFinder_github.dbo.Entity_Cache
--truncate table credFinder_github.dbo.Entity_Cache

--not actually used
DELETE FROM [dbo].[EntityLanguageMaps]

--this has be be done after entity.CompetencyCollection
DELETE FROM credFinder_github.dbo.[Collection.CollectionMember]  
-- NOTE: not being used, should be removed at some point
DELETE FROM credFinder_github.dbo.[Collection.HasMember]  
DELETE FROM credFinder_github.dbo.[Collection.Competency]
DELETE FROM credFinder_github.dbo.Collection   
-- ================================
DELETE FROM credFinder_github.dbo.[GeoCoordinate]

-- 
truncate table credFinder_github.dbo.[Import.EntityResolution]
truncate table credFinder_github.dbo.[Import.Message]
truncate table credFinder_github.dbo.[Import.PendingRequest]
/*
this can be big, so steps may be necessary
-- list by month
select convert(varchar(07), [DownloadDate], 120), count(*) as ttl from credFinder_github.dbo.[Import.Staging]
group by convert(varchar(07), [DownloadDate], 120)  Order by 1

*/
-- then either do by dates, or all at once
DELETE FROM credFinder_github.dbo.[Import.Staging]
where [DownloadDate] < '2021-01-01'
DELETE FROM credFinder_github.dbo.[Import.Staging]
where [DownloadDate] < '2022-01-01'
DELETE FROM credFinder_github.dbo.[Import.Staging]
where [DownloadDate] < '2023-01-01'
--delete remaining 
DELETE FROM credFinder_github.dbo.[Import.Staging]
--
DELETE FROM credFinder_github.dbo.JobProfile
DELETE FROM credFinder_github.dbo.LearningOpportunity
-- a helper table, not needed in this context, so remove if found
--DELETE FROM credFinder_github.dbo.LearningOpportunity_IndexBuild
DROP TABLE credFinder_github.dbo.LearningOpportunity_IndexBuild
--
DELETE FROM credFinder_github.dbo.OccupationProfile
DELETE FROM credFinder_github.dbo.[ProgressionModel.ProgressionLevel]
DELETE FROM credFinder_github.dbo.[ProgressionModel]

--need to clear all databases with a reference (and are not nullable) to an organization first

DELETE FROM credFinder_github.dbo.VerificationServiceProfile

--now clear the org
DELETE FROM credFinder_github.dbo.Organization

--OBSOLETE
--DELETE FROM credFinder_github.dbo.[Pathway.ComponentCondition]
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Pathway.ComponentCondition]') AND type in (N'U'))
DROP TABLE [dbo].[Pathway.ComponentCondition]
GO

DELETE FROM credFinder_github.dbo.[PathwayComponent]
DELETE FROM credFinder_github.dbo.Pathway
DELETE FROM credFinder_github.dbo.PathwaySet

--mostly obsolete
--DELETE FROM  credFinder_github.dbo.[Reference.Frameworks]

/****** Object:  Table [dbo].[Reference.Frameworks]    Script Date: 8/8/2023 3:56:38 PM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Reference.Frameworks]') AND type in (N'U'))
DROP TABLE [dbo].[Reference.Frameworks]
GO


DELETE FROM  credFinder_github.dbo.[Reference.FrameworkItem]
--Last ==> or leave alone!!
--DELETE FROM  credFinder_github.dbo.[Reference.Framework]
/* *****		dev/github only

DELETE FROM  credFinder_github.dbo.[Reference.Occupation]
*/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Reference.Occupation]') AND type in (N'U'))
DROP TABLE [dbo].[Reference.Occupation]
GO

DELETE FROM  credFinder_github.dbo.[Reports.Duplicates]
DELETE FROM  credFinder_github.dbo.[Reports.Summary]
DELETE FROM  credFinder_github.dbo.[Reports.SummaryHistory]
--
DELETE FROM credFinder_github.dbo.[ScheduledOffering]
DELETE FROM credFinder_github.dbo.SearchPendingReindex
truncate table credFinder_github.dbo.ServerDatabaseTables
DELETE FROM credFinder_github.dbo.SupportService

DELETE FROM credFinder_github.dbo.TaskProfile
DELETE FROM credFinder_github.dbo.[TransferIntermediary.TransferValue]

DELETE FROM credFinder_github.dbo.TransferIntermediary
DELETE FROM credFinder_github.dbo.TransferValueProfile

---
DELETE FROM credFinder_github.dbo.[Widget.Selection]
DELETE FROM credFinder_github.dbo.Widget
DELETE FROM credFinder_github.dbo.[Work.Query]
DELETE FROM credFinder_github.dbo.WorkRole


use credFinder_github
go

--reset identity ids to 0
DBCC CHECKIDENT ('[ActivityLog]', RESEED, 0);
DBCC CHECKIDENT ('[Assessment]', RESEED, 0);

DBCC CHECKIDENT ('[Collection]', RESEED, 0);
DBCC CHECKIDENT ('[Collection.CollectionMember]', RESEED, 0);
DBCC CHECKIDENT ('[Collection.Competency]', RESEED, 0);
DBCC CHECKIDENT ('[Collection.HasMember]', RESEED, 0);

DBCC CHECKIDENT ('[CompetencyFramework]', RESEED, 0);
DBCC CHECKIDENT ('[CompetencyFramework.Competency]', RESEED, 0);

DBCC CHECKIDENT ('[ConceptScheme]', RESEED, 0);
DBCC CHECKIDENT ('[ConceptScheme.Concept]', RESEED, 0);

DBCC CHECKIDENT ('[ConditionManifest]', RESEED, 0);
DBCC CHECKIDENT ('[ConditionProfile_Competencies_cache]', RESEED, 0);
DBCC CHECKIDENT ('[CostManifest]', RESEED, 0);

DBCC CHECKIDENT ('[credential]', RESEED, 0);
DBCC CHECKIDENT ('[Credential.SummaryCache]', RESEED, 0)
--
DBCC CHECKIDENT ('[DataProfile]', RESEED, 0);
DBCC CHECKIDENT ('[DataSetProfile]', RESEED, 0);
DBCC CHECKIDENT ('[DataSetTimeFrame]', RESEED, 0);;
--
DBCC CHECKIDENT ('[EarningsProfile]', RESEED, 0);
DBCC CHECKIDENT ('[EducationFramework]', RESEED, 0);
DBCC CHECKIDENT ('[EmploymentOutcomeProfile]', RESEED, 0);

DBCC CHECKIDENT ('[Entity]', RESEED, 0);
--NOTE - no identity column
---DBCC CHECKIDENT ('[Entity_Cache]', RESEED, 0);


DBCC CHECKIDENT ('[Entity.Address]', RESEED, 0);
DBCC CHECKIDENT ('[Entity.AgentRelationship]', RESEED, 0);
DBCC CHECKIDENT ('[Entity.AggregateDataProfile]', RESEED, 0);

DBCC CHECKIDENT ('[Entity.Assertion]', RESEED, 0);
DBCC CHECKIDENT ('[Entity.Assessment]', RESEED, 0);

DBCC CHECKIDENT ('[Entity.CommonCondition]', RESEED, 0);
DBCC CHECKIDENT ('[Entity.CommonCost]', RESEED, 0);
DBCC CHECKIDENT ('[Entity.Competency]', RESEED, 0);

DBCC CHECKIDENT ('[Entity.ConditionManifest]', RESEED, 0);
DBCC CHECKIDENT ('[Entity.ComponentCondition]', RESEED, 0);
DBCC CHECKIDENT ('[Entity.ConditionManifest]', RESEED, 0);
DBCC CHECKIDENT ('[Entity.ConditionProfile]', RESEED, 0);
DBCC CHECKIDENT ('[Entity.ContactPoint]', RESEED, 0);
DBCC CHECKIDENT ('[Entity.CostManifest]', RESEED, 0);
DBCC CHECKIDENT ('[Entity.CostProfile]', RESEED, 0);
DBCC CHECKIDENT ('[Entity.CostProfileItem]', RESEED, 0);
DBCC CHECKIDENT ('[Entity.Credential]', RESEED, 0);
--
DBCC CHECKIDENT ('[Entity.DataSetProfile]', RESEED, 0);
DBCC CHECKIDENT ('[Entity.DurationProfile]', RESEED, 0);
--
DBCC CHECKIDENT ('[Entity.EarningsProfile]', RESEED, 0);
DBCC CHECKIDENT ('[Entity.EmploymentOutcomeProfile]', RESEED, 0);
--
DBCC CHECKIDENT ('[Entity.FinancialAssistanceProfile]', RESEED, 0);
DBCC CHECKIDENT ('[Entity.FrameworkItem]', RESEED, 0);
DBCC CHECKIDENT ('[Entity.FrameworkItemOther]', RESEED, 0);
--
DBCC CHECKIDENT ('[Entity.HasOffering]', RESEED, 0);
DBCC CHECKIDENT ('[Entity.HasPathway]', RESEED, 0);
DBCC CHECKIDENT ('[Entity.HasPathwayComponent]', RESEED, 0);
DBCC CHECKIDENT ('[Entity.HasResource]', RESEED, 0);
DBCC CHECKIDENT ('[Entity.HasSupportService]', RESEED, 0);

--OBSOLETE
DBCC CHECKIDENT ('[Entity.HoldersProfile]', RESEED, 0);

DBCC CHECKIDENT ('[Entity.IdentifierValue]', RESEED, 0);
DBCC CHECKIDENT ('[Entity.IsPartOfSupportService]', RESEED, 0);

DBCC CHECKIDENT ('[Entity.Job]', RESEED, 0);
DBCC CHECKIDENT ('[Entity.JurisdictionProfile]', RESEED, 0);

DBCC CHECKIDENT ('[Entity.Language]', RESEED, 0);
----dev only
--DBCC CHECKIDENT ('[Entity.LanguageMaps]', RESEED, 0);
DBCC CHECKIDENT ('[Entity.LearningOpportunity]', RESEED, 0);
DBCC CHECKIDENT ('[Entity.Occupation]', RESEED, 0);
DBCC CHECKIDENT ('[Entity.Organization]', RESEED, 0);
--a prototype not really used yet
--DBCC CHECKIDENT ('[Entity.OrganizationReference]', RESEED, 0);
DBCC CHECKIDENT ('[Entity.ProcessProfile]', RESEED, 0);
DBCC CHECKIDENT ('[Entity.Property]', RESEED, 0);

DBCC CHECKIDENT ('[Entity.Reference]', RESEED, 0);
DBCC CHECKIDENT ('[Entity.ReferenceConnection]', RESEED, 0);
DBCC CHECKIDENT ('[Entity.ReferenceFramework]', RESEED, 0);
DBCC CHECKIDENT ('[Entity.RevocationProfile]', RESEED, 0);

DBCC CHECKIDENT ('[Entity.SearchIndex]', RESEED, 0);

DBCC CHECKIDENT ('[Entity.TransferValueProfile]', RESEED, 0);
DBCC CHECKIDENT ('[Entity.UsesVerificationService]', RESEED, 0);
--OBSOLETE
DBCC CHECKIDENT ('[Entity.VerificationProfile]', RESEED, 0);
--OBSOLETE
DBCC CHECKIDENT ('[Entity.VerificationStatus]', RESEED, 0);
--
DBCC CHECKIDENT ('[GeoCoordinate]', RESEED, 0);
DBCC CHECKIDENT ('[HoldersProfile]', RESEED, 0);
--
DBCC CHECKIDENT ('[Import.EntityResolution]', RESEED, 0);
DBCC CHECKIDENT ('[Import.Message]', RESEED, 0);
DBCC CHECKIDENT ('[Import.PendingRequest]', RESEED, 0);
DBCC CHECKIDENT ('[Import.Staging]', RESEED, 0);
--
DBCC CHECKIDENT ('[JobProfile]', RESEED, 0);
DBCC CHECKIDENT ('[LearningOpportunity]', RESEED, 0);
DBCC CHECKIDENT ('[MessageLog]', RESEED, 0);

DBCC CHECKIDENT ('[OccupationProfile]', RESEED, 0);
DBCC CHECKIDENT ('[Organization]', RESEED, 0);

DBCC CHECKIDENT ('[Pathway]', RESEED, 0);
DBCC CHECKIDENT ('[PathwayComponent]', RESEED, 0);
-- --OBSOLETE
--DBCC CHECKIDENT ('[Pathway.ComponentCondition]', RESEED, 0);
DBCC CHECKIDENT ('[PathwaySet]', RESEED, 0);
--
DBCC CHECKIDENT ('[ProgressionModel]', RESEED, 0);
DBCC CHECKIDENT ('[ProgressionModel.ProgressionLevel]', RESEED, 0);

--OBSOLETE
--DBCC CHECKIDENT ('[Reference.Frameworks]', RESEED, 0);
DBCC CHECKIDENT ('[Reference.FrameworkItem]', RESEED, 0);
--no longer clearing
--DBCC CHECKIDENT ('[Reference.Framework]', RESEED, 0);
--
DBCC CHECKIDENT ('[Reports.Duplicates]', RESEED, 0);
DBCC CHECKIDENT ('[Reports.Summary]', RESEED, 0);
DBCC CHECKIDENT ('[Reports.SummaryHistory]', RESEED, 0);


DBCC CHECKIDENT ('[ScheduledOffering]', RESEED, 0);
DBCC CHECKIDENT ('[SearchPendingReindex]', RESEED, 0);
DBCC CHECKIDENT ('[SupportService]', RESEED, 0);

DBCC CHECKIDENT ('[TaskProfile]', RESEED, 0);
DBCC CHECKIDENT ('[TransferIntermediary]', RESEED, 0);
DBCC CHECKIDENT ('[TransferIntermediary.TransferValue]', RESEED, 0);
DBCC CHECKIDENT ('[TransferValueProfile]', RESEED, 0);

DBCC CHECKIDENT ('[VerificationServiceProfile]', RESEED, 0);
--
DBCC CHECKIDENT ('[Widget]', RESEED, 0);
DBCC CHECKIDENT ('[Widget.Selection]', RESEED, 0);

DBCC CHECKIDENT ('[Work.Query]', RESEED, 0);
DBCC CHECKIDENT ('[WorkRole]', RESEED, 0);

--END
GO


--re-add FK_Entity.HasResource_Entity_Cache
ALTER TABLE [dbo].[Entity.HasResource]  WITH CHECK ADD  CONSTRAINT [FK_Entity.HasResource_Entity_Cache] FOREIGN KEY([EntityTypeId], [ResourceId])
REFERENCES [dbo].[Entity_Cache] ([EntityTypeId], [BaseId])
GO

ALTER TABLE [dbo].[Entity.HasResource] CHECK CONSTRAINT [FK_Entity.HasResource_Entity_Cache]
GO



--check to ensure all applicable tables have been reset

exec aspAllDatabaseTableCounts 'credFinder_github', 'base table', 10
go
--populate the dictionary tables
aspGenerateColumnDef @TableFilter = NULL, @TypeFilter='table'


--Now shrink the database

--exec aspAllDatabaseTableCounts 'credFinder_github', 'base table', 10
--go
     


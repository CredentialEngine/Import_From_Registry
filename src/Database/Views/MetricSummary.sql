Use credFinder
go
use sandbox_credFinder
go

--use staging_credFinder	
--go
--use flstaging_credFinder	
--go
--use txlibrary_credFinder	
--go
/****** Object:  View [dbo].[MetricSummary]   Script Date: 7/29/2020 11:13:19 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


/*
USE [credFinder]
GO
USE [sandbox_credFinder]
GO

SELECT [Id]
      ,[RowId]
      ,[EntityId]
      ,[Name]
      ,[Description]
      ,[EntityStateId]
      ,[CTID]
      ,[PrimaryAgentUid]
      ,[PrimaryOrganizationId]
      ,[PrimaryOrganizationName]
      ,[PrimaryOrganizationCtid]
      ,[RecordTypeId]
      ,[DerivedFrom]
      ,[EarningDefinition]
      ,[EarningsThreshold]
      ,[IncomeDeterminationId]
      ,[MetricTypeId]
      ,[SameAs]
      ,[Created]
      ,[LastUpdated]
  FROM [dbo].[MetricSummary]

GO


*/
/*
MetricSummary
Notes
- 
Mods
24-10-10 mparsons - new

*/
Alter VIEW [dbo].[MetricSummary]
AS
 
SELECT base.[Id]
		,base.[RowId]
		,b.Id as EntityId
		,base.[Name]
		,base.[Description]
		,base.[EntityStateId]
		,base.[CTID]
		,base.[PrimaryAgentUid]
		,isnull(primaryOrg.Id,0)	as PrimaryOrganizationId
		,isnull(primaryOrg.Name,'') as PrimaryOrganizationName
		,isnull(primaryOrg.CTID,'') as PrimaryOrganizationCtid

		,base.[RecordTypeId]
		,base.DerivedFromId

		,base.[EarningsDefinition]
		,base.[EarningsThreshold]
		,base.IncomeDeterminationTypeId

		,base.[SameAs]

		,base.[Created]
		,base.[LastUpdated]

-- select *
  FROM [dbo].[Metric] base

INNER JOIN dbo.Entity AS b ON base.RowId = b.EntityUid 
-- join for primary
Left join Organization primaryOrg on base.[PrimaryAgentUid] = primaryOrg.RowId and primaryOrg.EntityStateId > 1


where base.EntityStateId > 1



GO

grant select on [MetricSummary] to public
go



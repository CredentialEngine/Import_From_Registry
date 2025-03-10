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
/****** Object:  View [dbo].[IndustrySummary]    Script Date: 7/29/2020 11:13:19 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


/*
USE [credFinder]
GO
USE [sandbox_credFinder]
GO

SELECT base.[Id]
      ,base.[RowId]
      ,base.[Name]
      ,base.[Description]
      ,base.[EntityStateId]
      ,base.[CTID]
	        ,base.[PrimaryAgentUid]
      ,base.[PrimaryOrganizationId]
      ,base.[PrimaryOrganizationName]
      ,base.[PrimaryOrganizationCtid]
      ,base.[AbilityEmbodied]
      ,base.[Classification]
      ,base.[CodedNotation]
      ,base.[Comment]
      ,base.[Identifier]
      ,base.[KnowledgeEmbodied]
      ,base.[SkillEmbodied]
   
      ,base.[VersionIdentifier]
      ,base.[JsonProperties]
      ,base.[Created]
      ,base.[LastUpdated]
  FROM [dbo].[IndustrySummary] a

*/
/*
IndustrySummary
Notes
- 
Mods
22-11-08 mparsons - new

*/
Create VIEW [dbo].[IndustrySummary]
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

		,base.AlternateName
		,base.[Classification]
		,base.[CodedNotation]
		,base.[Comment]
		,base.[Identifier]
		,base.InCatalog
		,base.Keyword
		,base.[LifeCycleStatusTypeId]
		,cpv.Title as LifeCycleStatusType
		,base.SameAs
		,base.[VersionIdentifier]

		,base.[Created]
		,base.[LastUpdated]

		,(SELECT ehrs.[Name], ehrs.[Description] FROM [dbo].[Entity.HasResourceSummary] ehrs 
		where ehrs.EntityId = b.Id  and ehrs.EntityTypeId=17
		FOR XML RAW, ROOT('Competencies')) Competencies

-- select *
  FROM [dbo].[Industry] base

INNER JOIN dbo.Entity AS b ON base.RowId = b.EntityUid 
-- join for primary
	Left join Organization primaryOrg on base.[PrimaryAgentUid] = primaryOrg.RowId and primaryOrg.EntityStateId > 1
	Left Join [Codes.PropertyValue] cpv on base.LifeCycleStatusTypeId = cpv.Id

where base.EntityStateId > 1



GO

grant select on [IndustrySummary] to public
go



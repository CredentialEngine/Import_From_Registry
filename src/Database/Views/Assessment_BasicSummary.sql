use credFinder
GO

use sandbox_credFinder
go


/****** Object:  View [dbo].[Assessment_BasicSummary]    Script Date: 8/16/2017 9:27:56 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

/*

SELECT [Id]
      ,[Name]
      ,[Description]
      ,[OrgId]
      ,[Organization]
      ,[DateEffective]
      ,[SubjectWebpage]
      ,[EntityStateId]
      ,[CTID]
      ,[CredentialRegistryId]
      ,[availableOnlineAt]
      ,[AvailabilityListing]
      ,[AssessmentExampleUrl]
      ,[ProcessStandards]
      ,[ScoringMethodExample]
      ,[ExternalResearch]


	        ,[Created]
      ,[LastUpdated]
      ,[RowId]
  FROM [dbo].[Assessment_BasicSummary]






*/

/*
Modifications
25-01-15 - removed IdentificationCode as no longer used (or as codedNotation)

*/
Alter VIEW [dbo].[Assessment_BasicSummary]
AS

SELECT base.[Id]
	,base.[Name]
	,base.[Description]
	
	--owning org
	,isnull(owningOrg.Id,0) as OrgId
	,isnull(owningOrg.Name,'') as OwningOrganization
	,base.OwningAgentUid
	,[DateEffective]
	-- ,[OrgId]      ,base.[AgentUid]
	,base.SubjectWebpage 
	,base.EntityStateId
	,isnull(base.CTID,'') As CTID 
	,base.CredentialRegistryId   

	,base.availableOnlineAt
	,base.AvailabilityListing
	,base.AssessmentExampleUrl
	,base.ProcessStandards
	,base.ScoringMethodExample
	,base.ExternalResearch
	,base.IsNonCredit
	,base.[Created]
	,base.[LastUpdated]
	,base.RowId

  FROM [dbo].[Assessment] base
-- join for owner
	Left join Organization owningOrg on base.OwningAgentUid = owningOrg.RowId and owningOrg.EntityStateId > 1

where base.EntityStateId >= 2

go
grant select on [Assessment_BasicSummary] to public

GO



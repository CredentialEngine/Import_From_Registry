USE [credFinder]
GO

use sandbox_credFinder
go

--use staging_credFinder
--go
/****** Object:  View [dbo].[Assessment_Summary]    Script Date: 5/31/2020 9:44:06 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


/*

USE [credFinder]
GO

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
      ,[cerEnvelopeUrl]

      ,[availableOnlineAt]
      ,[AvailabilityListing]
      ,[AssessmentExampleUrl]
      ,[ProcessStandards]
      ,[ScoringMethodExample]
      ,[ExternalResearch]

      ,[RequiresCount]
      ,[RecommendsCount]
      ,[isRequiredForCount]
      ,[IsRecommendedForCount]
      ,[IsAdvancedStandingForCount]
      ,[AdvancedStandingFromCount]
      ,[isPreparationForCount]
      ,[PreparationFromCount]

      --,[ConnectionsList]
      --,[CredentialsList]
	  ,Org_QAAgentAndRoles
	        ,[Created]
      ,[LastUpdated]
      ,[RowId]
  FROM [dbo].[Assessment_Summary]
  where EntityStateId= 2

  and len(Org_QAAgentAndRoles) > 0
where IsAdvancedStandingForCount> 0





*/
/*
Modifications
25-01-15 - removed IdentificationCode as no longer used (or as codedNotation)
*/
ALTER VIEW [dbo].[Assessment_Summary]
AS

SELECT base.[Id]
	,base.[Name]
	,base.[Description]
	,base.EntityStateId
	,isnull(base.CTID,'') As CTID 	
	--owning org
	,isnull(owningOrg.Id,0) as OrgId
	,isnull(owningOrg.Name,'') as Organization
	,isnull(owningOrg.CTID,'') as OwningOrganizationCtid
	,base.OwningAgentUid
	,[DateEffective]
	-- ,[OrgId]      ,base.[AgentUid]
	,base.SubjectWebpage 

	,base.CredentialRegistryId   
	--,case when len(isnull(base.CredentialRegistryId,'')) = 36 then
	--'<a href="http://lr-staging.learningtapestry.com/ce-registry/envelopes/' + base.CredentialRegistryId + '" target="_blank">cerEnvelope</a>'
	--else '' End As cerEnvelopeUrl  
	,'' As cerEnvelopeUrl 
	,case when IsNull(base.LifeCycleStatusTypeId,0) > 0 then base.LifeCycleStatusTypeId
	else 0 end As LifeCycleStatusTypeId --default to production value for now

	,case when IsNull(base.LifeCycleStatusTypeId,0) > 0 then cpv.Title
	else '' end As LifeCycleStatusType 

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

	,IsNull(c1.Nbr,0) As RequiresCount
	,IsNull(c2.Nbr,0) As RecommendsCount
	,IsNull(c3.Nbr,0) As isRequiredForCount
	,IsNull(c4.Nbr,0) As IsRecommendedForCount
	,IsNull(c6.Nbr,0) As IsAdvancedStandingForCount
	,IsNull(c7.Nbr,0) As AdvancedStandingFromCount
	,IsNull(c8.Nbr,0) As isPreparationForCount
	,IsNull(c9.Nbr,0) As PreparationFromCount
    
		--actual connection type (no credential info)
	,isnull(connectionsCsv.Profiles,'') As ConnectionsList	
	--connection type, plus Id, and name of credential - need to handle other entities		
	,isnull(connectionsCsv.CredentialsList,'') As CredentialsList	

--	24-02-22 - not much of a difference after removing these
	--,0 As RequiresCount
	--,0 As RecommendsCount
	--,0 As isRequiredForCount
	--,0 As IsRecommendedForCount
	--,0 As IsAdvancedStandingForCount
	--,0 As AdvancedStandingFromCount
	--,0 As isPreparationForCount
	--,0 As PreparationFromCount
	--,'' as ConnectionsList


	--,isnull(qaRoles.Org_QAAgentAndRoles,'') As Org_QAAgentAndRoles
	,'' as Org_QAAgentAndRoles
  FROM [dbo].[Assessment] base
  left join [codes.PropertyValue] cpv on base.LifeCycleStatusTypeId = cpv.Id
-- join for owner
	Left join Organization owningOrg on base.OwningAgentUid = owningOrg.RowId and owningOrg.EntityStateId > 1
	--24-02-18 mp - this is wrong, LifeCycleStatusTypeId is on base. It was a fall back
	--Left Join EntityProperty_Summary	statusProperty on base.RowId = statusProperty.EntityUid and statusProperty.CategoryId = 84		--LifeCycleStatus


	-- ===================== condition profiles ====================================
	--conditionProfiles - Post-Award Connections (Requirements)\
	left join (
		Select ParentId, Count(*) As Nbr from Entity_ConditionProfileTargetsSummary 
		where ParentEntityTypeId= 3 
		AND ConnectionTypeId = 1 and isnull(ConditionSubTypeId, 1) = 2
		group by ParentId

	) c1 on base.Id = c1.ParentId
--conditionProfiles - Attainment Recommendations
left join (
	Select ParentId, Count(*) As Nbr from Entity_ConditionProfileTargetsSummary where ParentEntityTypeId= 3 
	AND ConnectionTypeId = 2 and isnull(ConditionSubTypeId, 1) = 2
	group by ParentId
	) c2						on base.Id = c2.ParentId
	left join (
	Select ParentId, Count(*) As Nbr from Entity_ConditionProfileTargetsSummary where ParentEntityTypeId= 3 
	AND ConnectionTypeId = 3 group by ParentId
	) c3						on base.Id = c3.ParentId
		
	--conditionProfiles - Post-Award Connections (Recommendations)
	left join (
	Select ParentId, Count(*) As Nbr from Entity_ConditionProfileTargetsSummary where ParentEntityTypeId= 3 
	AND ConnectionTypeId = 4 group by ParentId
	) c4						on base.Id = c4.ParentId

	--conditionProfiles - Advanced Standing For
	left join (
	Select ParentId, Count(*) As Nbr from Entity_ConditionProfileTargetsSummary where ParentEntityTypeId= 3 
	AND ConnectionTypeId = 6 group by ParentId
	) c6						on base.Id = c6.ParentId
								
		
	--conditionProfiles - Advanced Standing From
	left join (
	Select ParentId, Count(*) As Nbr from Entity_ConditionProfileTargetsSummary where ParentEntityTypeId= 3 
	AND ConnectionTypeId = 7 group by ParentId
	) c7						on base.Id = c7.ParentId
		
		
	--======== connection Profiles  Preparation For=======
	left join (
	Select ParentId, Count(*) As Nbr from Entity_ConditionProfileTargetsSummary where ParentEntityTypeId= 3 
	AND ConnectionTypeId = 8 group by ParentId
	) c8						on base.Id = c8.ParentId

	--conditionProfiles - Preparation From
	left join (
	Select ParentId, Count(*) As Nbr from Entity_ConditionProfileTargetsSummary where ParentEntityTypeId= 3 
	AND ConnectionTypeId = 9 group by ParentId
	) c9						on base.Id = c9.ParentId		

	--connections ? how different from the above
	left join Entity_ConditionProfilesConnectionsCSV connectionsCsv 
				on connectionsCsv.ParentEntityTypeId = 3 
				AND base.id = connectionsCsv.ParentId

--TODO - Entity_QARolesCSV uses entity_summary (many unions), need to change this
	--left join [Entity_QARolesCSV] qaRoles 
	--			on qaRoles.EntityTypeId = 3 
	--			AND base.id = qaRoles.BaseId
where base.EntityStateId >= 2

GO


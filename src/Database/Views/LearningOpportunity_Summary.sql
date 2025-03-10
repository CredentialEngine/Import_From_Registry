use credFinder
GO
--use sandbox_credFinder
--go


--use staging_credFinder
--go

/****** Object:  View [dbo].[LearningOpportunity_Summary]    Script Date: 8/16/2017 10:06:44 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


/*


SELECT top (1000)
[Id]
      ,[RowId]
      ,[Name]
      ,[Description]
      ,[OrgId]
      ,[Organization]
      ,[SubjectWebpage]
      ,[DateEffective]
      ,[EntityStateId]
      ,[CTID]
      ,[CredentialRegistryId]
      ,[cerEnvelopeUrl]
      ,[Created]
      ,[LastUpdated]
      ,[CodedNotation]
      ,[availableOnlineAt]
      ,[AvailabilityListing]
      ,[RequiresCount]
      ,[RecommendsCount]
      ,[isRequiredForCount]
      ,[IsRecommendedForCount]
      ,[IsAdvancedStandingForCount]
      ,[AdvancedStandingFromCount]
      ,[isPreparationForCount]
      ,[PreparationFromCount]
      ,[ConnectionsList]
      ,[CredentialsList]
      ,[Org_QAAgentAndRoles]
  FROM [dbo].[LearningOpportunity_Summary] base
  where base.id > 34000 and base.id < 36000
  order by base.Id

  where len(Org_QAAgentAndRoles) > 0

where 
 ( base.RowId in ( SELECT  b.EntityUid FROM [dbo].[Entity.Address] a inner join Entity b on base.EntityId = b.Id    where [Longitude] < -75.69581015624999 and [Longitude] > -144.60206015625 and [Latitude] < 55.563635054119175 and [Latitude] > 7.772301325445006 ) ) 

 
*/

/*
Learning opportunity summary
NOT used in code, only by search, elastic search, and other views

Do not bulk up if not necessary

Modifications
25-01-15 - renamed IdentificationCode to codedNotation

*/
Alter VIEW [dbo].[LearningOpportunity_Summary]
AS

SELECT 
base.[Id]
	,base.RowId 
	,base.EntityTypeId as LearningEntityTypeId
	,case when base.EntityTypeId= 36 then 'Learning Program'
		when base.EntityTypeId = 37 then 'Course'
		else 'Learning Opportunity' end as LearningEntityType
	,e.Id as EntityId
	,base.EntityStateId
	,isnull(base.CTID,'') As CTID 
	,base.[Name]
	,base.[Description]
	,IsNull(base.LifeCycleStatusTypeId,0)	as LifeCycleStatusTypeId
	,cpv.Title		as LifeCycleStatusType
	--owning org
	,isnull(owningOrg.Id,0) as OrgId
	,isnull(owningOrg.Name,'') as Organization
	,isnull(owningOrg.CTID,'') as OwningOrganizationCtid
	,base.OwningAgentUid
	,base.SubjectWebpage
	,base.[DateEffective]

	,base.CredentialRegistryId
	,case when len(isnull(base.CredentialRegistryId,'')) = 36 then
	'<a href="http://lr-staging.learningtapestry.com/ce-registry/envelopes/' + base.CredentialRegistryId + '" target="_blank">cerEnvelope</a>'
	else '' End As cerEnvelopeUrl
	
	,base.[Created]
	,base.[LastUpdated]

	--else isnull(statusProperty.Property,'') end As LifeCycleStatusType --
	,base.CodedNotation
	,base.SCED
	,base.availableOnlineAt
	,base.AvailabilityListing

	,base.IsNonCredit

	,IsNull(c1.Nbr,0) As RequiresCount
	,IsNull(c2.Nbr,0) As RecommendsCount
	----don't add these until a use case for elastic
	--,IsNull(Corequisite.Nbr,0) As CorequisiteConditionCount
	--,IsNull(EntryCondition.Nbr,0) As EntryConditionConditionCount

	,IsNull(c3.Nbr,0) As isRequiredForCount
	,IsNull(c4.Nbr,0) As IsRecommendedForCount
	,IsNull(c6.Nbr,0) As IsAdvancedStandingForCount
	,IsNull(c7.Nbr,0) As AdvancedStandingFromCount
	,IsNull(c8.Nbr,0) As isPreparationForCount
	,IsNull(c9.Nbr,0) As PreparationFromCount

	--actual connection type (no credential info)
	,isnull(connectionsCsv.Profiles,'') As ConnectionsList			
	,isnull(connectionsCsv.CredentialsList,'') As CredentialsList	--connection type, plus Id, and name of credential
	,isnull(qaRoles.Org_QAAgentAndRoles,'') As Org_QAAgentAndRoles

	--,skip for now, a little heavy?
	--,isnull(e.AgentRelationshipsForEntity,'') as AgentRelationshipsForEntity
	--,isnull(e.ResourceDetail,'') as ResourceDetail

  FROM [dbo].[LearningOpportunity] base
    Inner Join Entity_Cache e on base.RowId = e.EntityUid

  left join [codes.PropertyValue] cpv on base.LifeCycleStatusTypeId = cpv.Id
 --   Left Join Organization managingOrg on base.ManagingOrgId = managingOrg.Id
 -- join for owner - note may be changing
	Left join Organization owningOrg on base.OwningAgentUid = owningOrg.RowId and owningOrg.EntityStateId > 1
--	Left Join EntityProperty_Summary	statusProperty on base.RowId = statusProperty.EntityUid and statusProperty.CategoryId = 84

	--connections/conditionProfiles  - Post-Award Connections (Requirements)
left join (
	Select ParentId, Count(*) As Nbr from Entity_ConditionProfileTargetsSummary where ParentEntityTypeId= 7 AND ConnectionTypeId = 1 
	and isnull(ConditionSubTypeId, 1) in (1,2)
	--AND HasTargetCredential= 1 --don't think applicable
	group by ParentId
	) c1 on base.Id = c1.ParentId
--connections/conditionProfiles - Attainment Recommendations
left join (
	Select ParentId, Count(*) As Nbr from Entity_ConditionProfileTargetsSummary where ParentEntityTypeId= 7 AND ConnectionTypeId = 2 
	and isnull(ConditionSubTypeId, 1) in (1,2)
	--AND HasTargetCredential= 1 
	group by ParentId
	) c2						on base.Id = c2.ParentId
--Corequisite
left join (
	Select ParentId, Count(*) As Nbr from Entity_ConditionProfileTargetsSummary where ParentEntityTypeId= 7 AND ConnectionTypeId = 10 
	and isnull(ConditionSubTypeId, 1) in (1,2)
	--AND HasTargetCredential= 1 
	group by ParentId
	) Corequisite						on base.Id = Corequisite.ParentId

--Entry Condition
left join (
	Select ParentId, Count(*) As Nbr from Entity_ConditionProfileTargetsSummary where ParentEntityTypeId= 7 AND ConnectionTypeId = 11 
	and isnull(ConditionSubTypeId, 1) in (1,2)
	--AND HasTargetCredential= 1 
	group by ParentId
	) EntryCondition						on base.Id = EntryCondition.ParentId
	--============================================================================
	--Post-Award Connections (Is Required For)
	left join (
	Select ParentId, Count(*) As Nbr from Entity_ConditionProfileTargetsSummary where ParentEntityTypeId= 7 AND ConnectionTypeId = 3 
	group by ParentId
	--AND HasTargetCredential = 1 
	) c3						on base.Id = c3.ParentId
		
	--connections/conditionProfiles - Post-Award Connections (Is Recommended For)
	left join (
	Select ParentId, Count(*) As Nbr from Entity_ConditionProfileTargetsSummary where ParentEntityTypeId= 7 AND ConnectionTypeId = 4
	--AND HasTargetCredential = 1 
	 group by ParentId
	) c4						on base.Id = c4.ParentId

	--connections/conditionProfiles - Advanced Standing For
	left join (
	Select ParentId, Count(*) As Nbr from Entity_ConditionProfileTargetsSummary where ParentEntityTypeId= 7 AND ConnectionTypeId = 6 
	--AND HasTargetCredential= 1 
	group by ParentId
	) c6						on base.Id = c6.ParentId
								
		
	--connections/conditionProfiles - Advanced Standing From
	left join (
	Select ParentId, Count(*) As Nbr from Entity_ConditionProfileTargetsSummary where ParentEntityTypeId= 7 AND ConnectionTypeId = 7 
	--AND HasTargetCredential= 1 
	group by ParentId
	) c7						on base.Id = c7.ParentId
		
		
	--connections/conditionProfiles - Preparation For
	left join (
	Select ParentId, Count(*) As Nbr from Entity_ConditionProfileTargetsSummary where ParentEntityTypeId= 7 AND ConnectionTypeId = 8 
	--AND HasTargetCredential= 1 
	group by ParentId
	) c8						on base.Id = c8.ParentId

	--connections/conditionProfiles - Preparation From
	left join (
	Select ParentId, Count(*) As Nbr from Entity_ConditionProfileTargetsSummary where ParentEntityTypeId= 7 AND ConnectionTypeId = 9
	AND HasTargetCredential= 1 
	group by ParentId
	) c9						on base.Id = c9.ParentId	

	--why do we have this after the latter?
	left join Entity_ConditionProfilesConnectionsCSV connectionsCsv 
				on connectionsCsv.ParentEntityTypeId = 7 
				AND base.id = connectionsCsv.ParentId
--TODO - Entity_QARolesCSV uses entity_summary (many unions), need to change this
	left join [Entity_QARolesCSV] qaRoles 
				on qaRoles.EntityTypeId = 7 
				AND base.id = qaRoles.BaseId

where base.EntityStateId >= 2
GO
grant select on LearningOpportunity_Summary to public
go


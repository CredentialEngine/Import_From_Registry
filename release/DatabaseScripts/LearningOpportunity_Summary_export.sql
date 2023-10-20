use credfinder_github
go

/****** Object:  View [dbo].[LearningOpportunity_Summary_Export]    Script Date: 8/16/2017 10:06:44 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


/*

USE [credFinder]
GO
USE [credFinder]
GO

SELECT top 1000
a.[Id]
      ,a.[RowId]
      ,a.[LearningEntityTypeId]
	  ,a.LearningClassType
      ,a.[EntityId]
      ,a.[EntityStateId]
      ,a.[CTID]
      ,a.[Name]
      ,a.[Description]
      ,a.[LifeCycleStatusTypeId]
      ,a.[LifeCycleStatusType]
      ,a.[OrgId]
      ,a.[Organization]
      ,a.[OwningOrganizationCtid]
      ,a.[SubjectWebpage]
      ,a.[DateEffective]
      ,a.[Created]
      ,a.[LastUpdated]
      ,a.[CodedNotation]
      ,a.[SCED]
      ,a.[availableOnlineAt]
      ,a.[AvailabilityListing]
      ,a.[IsNonCredit]
      ,a.[CreditValue]
      ,a.[CreditUnitTypeDescription]
      ,a.[ConnectionsList]
      ,a.[CredentialsList]
      ,a.[DeliveryType]
      ,a.[AudienceLevel]
      ,a.[AudienceType]
      ,a.[LearningMethodType]
      ,a.[AssessmentMethodType]
      ,a.[IndustryType]
      ,a.[OccupationType]

   --   ,b.[TypeId]
	  --,b.[FromDuration]
   --   ,b.[ToDuration]
   --   ,b.[FromYears]
   --   ,b.[FromMonths]
   --   ,b.[FromWeeks]
   --   ,b.[FromDays]
   --   ,b.[FromHours]
   --   ,b.[FromMinutes]
   --   ,b.[ToYears]
   --   ,b.[ToMonths]
   --   ,b.[ToWeeks]
   --   ,b.[ToDays]
   --   ,b.[ToHours]
   --   ,b.[ToMinutes]
   --   ,b.[DurationComment]
   --   ,b.[DurationSummary]


  FROM [dbo].[LearningOpportunity_Summary_Export] a
  --Left join Entity_DurationProfileSummary b on a.Id = b.EntityBaseId and b.EntityTypeId = 7
  where a.[CreditValue]is not null 



--  where 
--  LearningEntityTypeId = 37
--  Len(CodedNotation) > 1
--  Len(Isnull(IndustryType,'')) > 5 
--	OR DeliveryType is not null
--	OR AudienceType is not null
GO


  where base.id > 34000 and base.id < 36000
  order by base.Id

  where len(Org_QAAgentAndRoles) > 0

where 
 ( base.RowId in ( SELECT  b.EntityUid FROM [dbo].[Entity.Address] a inner join Entity b on base.EntityId = b.Id    where [Longitude] < -75.69581015624999 and [Longitude] > -144.60206015625 and [Latitude] < 55.563635054119175 and [Latitude] > 7.772301325445006 ) ) 

 SELECT ISJSON(CreditValue) FROM [LearningOpportunity_Summary_Export] -- Check JSON ok
 
*/

/*
Learning opportunity summary for OECD/exporting

*/
Create VIEW [dbo].[LearningOpportunity_Summary_Export]
AS

SELECT 
base.[Id]
	,base.RowId 
	,base.EntityTypeId as LearningEntityTypeId
	,cet.Title as LearningClassType
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
	--,base.OwningAgentUid
	,base.SubjectWebpage
	,base.[DateEffective]

	--,base.CredentialRegistryId
	--,case when len(isnull(base.CredentialRegistryId,'')) = 36 then
	--'<a href="http://lr-staging.learningtapestry.com/ce-registry/envelopes/' + base.CredentialRegistryId + '" target="_blank">cerEnvelope</a>'
	--else '' End As cerEnvelopeUrl
	
	,base.[Created]	,base.[LastUpdated]

	,base.[IdentificationCode] as CodedNotation
	,base.SCED
	,base.availableOnlineAt
	,base.AvailabilityListing

	,base.IsNonCredit
	,base.[CreditValue]
	,base.[CreditUnitTypeDescription]
	--assuming one entry
	,credValueTranslate.Value as cvValue
	,credValueTranslate.Description as cvDescription

	--,IsNull(c1.Nbr,0) As RequiresCount
	--,IsNull(c2.Nbr,0) As RecommendsCount
	------don't add these until a use case for elastic
	----,IsNull(Corequisite.Nbr,0) As CorequisiteConditionCount
	----,IsNull(EntryCondition.Nbr,0) As EntryConditionConditionCount

	--,IsNull(c3.Nbr,0) As isRequiredForCount
	--,IsNull(c4.Nbr,0) As IsRecommendedForCount
	--,IsNull(c6.Nbr,0) As IsAdvancedStandingForCount
	--,IsNull(c7.Nbr,0) As AdvancedStandingFromCount
	--,IsNull(c8.Nbr,0) As isPreparationForCount
	--,IsNull(c9.Nbr,0) As PreparationFromCount

	--actual connection type (no credential info)
	,isnull(connectionsCsv.Profiles,'') As ConnectionsList			
	,isnull(connectionsCsv.CredentialsList,'') As CredentialsList	--connection type, plus Id, and name of credential
	--,isnull(qaRoles.Org_QAAgentAndRoles,'') As Org_QAAgentAndRoles
	--==================================
    ,CASE
          WHEN DeliveryType IS NULL THEN ''
          WHEN len(DeliveryType) = 0 THEN ''
          ELSE left(DeliveryType,len(DeliveryType)-1)
    END AS DeliveryType
	,CASE
          WHEN AudienceLevel IS NULL THEN ''
          WHEN len(AudienceLevel) = 0 THEN ''
          ELSE left(AudienceLevel,len(AudienceLevel)-1)
    END AS AudienceLevel
    ,CASE
          WHEN AudienceType IS NULL THEN ''
          WHEN len(AudienceType) = 0 THEN ''
          ELSE left(AudienceType,len(AudienceType)-1)
    END AS AudienceType
	,CASE
          WHEN LearningMethodType IS NULL THEN ''
          WHEN len(LearningMethodType) = 0 THEN ''
          ELSE left(LearningMethodType,len(LearningMethodType)-1)
    END AS LearningMethodType
	,CASE
          WHEN AssessmentMethodType IS NULL THEN ''
          WHEN len(AssessmentMethodType) = 0 THEN ''
          ELSE left(AssessmentMethodType,len(AssessmentMethodType)-1)
    END AS AssessmentMethodType
	--
--probably don't want XML
	--,(SELECT a.[CategoryId], a.[ReferenceFrameworkItemId], a.[Name], a.[SchemaName], ISNULL(NULLIF(a.[CodeGroup], ''), NULL) CodeGroup, a.[CodedNotation] FROM [dbo].[Entity_ReferenceFramework_Summary] a inner join Entity b on a.EntityId = b.Id inner join LearningOpportunity c on b.EntityUid = c.RowId where a.[CategoryId] = 10 AND c.[Id] = base.[Id] FOR XML RAW, ROOT('IndustryType')) IndustryType 
    ,CASE
          WHEN IndustryType IS NULL THEN ''
          WHEN len(IndustryType) = 0 THEN ''
          ELSE left(IndustryType,len(IndustryType)-1)
    END AS IndustryType
	,CASE
          WHEN OccupationType IS NULL THEN ''
          WHEN len(OccupationType) = 0 THEN ''
          ELSE left(OccupationType,len(OccupationType)-1)
    END AS OccupationType


  FROM [dbo].[LearningOpportunity] base
    Inner Join Entity e on base.RowId = e.EntityUid
	inner join [Codes.EntityTypes] cet on base.EntityTypeId = cet.Id
  left join [codes.PropertyValue] cpv on base.LifeCycleStatusTypeId = cpv.Id
 --   Left Join Organization managingOrg on base.ManagingOrgId = managingOrg.Id
 -- join for owner - note may be changing
	Left join Organization owningOrg on base.OwningAgentUid = owningOrg.RowId and owningOrg.EntityStateId > 1
--	Left Join EntityProperty_Summary	statusProperty on base.RowId = statusProperty.EntityUid and statusProperty.CategoryId = 84
--=======================================



--=================================
-- condition profiles
-- requires
--left join (
--	Select ParentId, Count(*) As Nbr from Entity_ConditionProfileTargetsSummary 
--	where ParentEntityTypeId= 7 AND ConnectionTypeId = 1 and isnull(ConditionSubTypeId, 0) = 1
--	--AND HasTargetCredential= 1	--does this matter? - think not
--	group by ParentId
--	) cp1 on base.Id = cp1.ParentId
---- recommends
--left join (
--	Select ParentId, Count(*) As Nbr from Entity_ConditionProfileTargetsSummary 
--	where ParentEntityTypeId= 7 AND ConnectionTypeId = 2 and isnull(ConditionSubTypeId, 0) = 1
--	group by ParentId
--	) cp2 on base.Id = cp2.ParentId

--	--connections/conditionProfiles  - Post-Award Connections (Requirements)
--left join (
--	Select ParentId, Count(*) As Nbr from Entity_ConditionProfileTargetsSummary where ParentEntityTypeId= 7 AND ConnectionTypeId = 1 
--	and isnull(ConditionSubTypeId, 1) in (1,2)
--	--AND HasTargetCredential= 1 --don't think applicable
--	group by ParentId
--	) c1 on base.Id = c1.ParentId
----connections/conditionProfiles - Attainment Recommendations
--left join (
--	Select ParentId, Count(*) As Nbr from Entity_ConditionProfileTargetsSummary where ParentEntityTypeId= 7 AND ConnectionTypeId = 2 
--	and isnull(ConditionSubTypeId, 1) in (1,2)
--	--AND HasTargetCredential= 1 
--	group by ParentId
--	) c2						on base.Id = c2.ParentId
----Corequisite
--left join (
--	Select ParentId, Count(*) As Nbr from Entity_ConditionProfileTargetsSummary where ParentEntityTypeId= 7 AND ConnectionTypeId = 10 
--	and isnull(ConditionSubTypeId, 1) in (1,2)
--	--AND HasTargetCredential= 1 
--	group by ParentId
--	) Corequisite						on base.Id = Corequisite.ParentId

----Entry Condition
--left join (
--	Select ParentId, Count(*) As Nbr from Entity_ConditionProfileTargetsSummary where ParentEntityTypeId= 7 AND ConnectionTypeId = 11 
--	and isnull(ConditionSubTypeId, 1) in (1,2)
--	--AND HasTargetCredential= 1 
--	group by ParentId
--	) EntryCondition						on base.Id = EntryCondition.ParentId
--	--============================================================================
--	--Post-Award Connections (Is Required For)
--	left join (
--	Select ParentId, Count(*) As Nbr from Entity_ConditionProfileTargetsSummary where ParentEntityTypeId= 7 AND ConnectionTypeId = 3 
--	group by ParentId
--	--AND HasTargetCredential = 1 
--	) c3						on base.Id = c3.ParentId
		
--	--connections/conditionProfiles - Post-Award Connections (Is Recommended For)
--	left join (
--	Select ParentId, Count(*) As Nbr from Entity_ConditionProfileTargetsSummary where ParentEntityTypeId= 7 AND ConnectionTypeId = 4
--	--AND HasTargetCredential = 1 
--	 group by ParentId
--	) c4						on base.Id = c4.ParentId

--	--connections/conditionProfiles - Advanced Standing For
--	left join (
--	Select ParentId, Count(*) As Nbr from Entity_ConditionProfileTargetsSummary where ParentEntityTypeId= 7 AND ConnectionTypeId = 6 
--	--AND HasTargetCredential= 1 
--	group by ParentId
--	) c6						on base.Id = c6.ParentId
								
		
--	--connections/conditionProfiles - Advanced Standing From
--	left join (
--	Select ParentId, Count(*) As Nbr from Entity_ConditionProfileTargetsSummary where ParentEntityTypeId= 7 AND ConnectionTypeId = 7 
--	--AND HasTargetCredential= 1 
--	group by ParentId
--	) c7						on base.Id = c7.ParentId
		
		
--	--connections/conditionProfiles - Preparation For
--	left join (
--	Select ParentId, Count(*) As Nbr from Entity_ConditionProfileTargetsSummary where ParentEntityTypeId= 7 AND ConnectionTypeId = 8 
--	--AND HasTargetCredential= 1 
--	group by ParentId
--	) c8						on base.Id = c8.ParentId

--	--connections/conditionProfiles - Preparation From
--	left join (
--	Select ParentId, Count(*) As Nbr from Entity_ConditionProfileTargetsSummary where ParentEntityTypeId= 7 AND ConnectionTypeId = 9
--	AND HasTargetCredential= 1 
--	group by ParentId
--	) c9						on base.Id = c9.ParentId	

	--why do we have this after the latter?
	left join Entity_ConditionProfilesConnectionsCSV connectionsCsv 
				on connectionsCsv.ParentEntityTypeId = 7 
				AND base.id = connectionsCsv.ParentId
--TODO - Entity_QARolesCSV uses entity_summary (many unions), need to change this
	--left join [Entity_QARolesCSV] qaRoles 
	--			on qaRoles.EntityTypeId = 7 
	--			AND base.id = qaRoles.BaseId

--=================================
--convert(varchar,cp.PropertyValueId)  + '~' + 
CROSS APPLY (
    SELECT cp.Property + '| '
    FROM dbo.EntityProperty_Summary cp  
    WHERE cp.CategoryId = 21
	and e.Id = cp.EntityId
	--Order by cp.PropertyValueId
    FOR XML Path('') 
	) D (DeliveryType)
--
CROSS APPLY (
    SELECT cp.Property + '| '
    FROM dbo.EntityProperty_Summary cp  
    WHERE cp.CategoryId = 4
	and e.Id = cp.EntityId
	--Order by cp.PropertyValueId
    FOR XML Path('') 
	) AL (AudienceLevel)
	--
CROSS APPLY (
    SELECT cp.Property + '| '
    FROM dbo.EntityProperty_Summary cp  
    WHERE cp.CategoryId = 14
	and e.Id = cp.EntityId
	--Order by cp.PropertyValueId
    FOR XML Path('') 
	) AT (AudienceType)
	--
CROSS APPLY (
    SELECT cp.Property + '| '
    FROM dbo.EntityProperty_Summary cp  
    WHERE cp.CategoryId = 53
	and e.Id = cp.EntityId
	--Order by cp.PropertyValueId
    FOR XML Path('') 
	) LMT (LearningMethodType)
	--
CROSS APPLY (
    SELECT cp.Property + '| '
    FROM dbo.EntityProperty_Summary cp  
    WHERE cp.CategoryId = 56
	and e.Id = cp.EntityId
	--Order by cp.PropertyValueId
    FOR XML Path('') 
	) AMT (AssessmentMethodType)
--===============================
CROSS APPLY (
    SELECT convert(varchar,cp.CodedNotation)  + '~' + cp.Name + '| '
    FROM dbo.[Entity_ReferenceFramework_Summary] cp  
    WHERE cp.CategoryId = 10
	and e.Id = cp.EntityId
    FOR XML Path('') ) IT (IndustryType)
CROSS APPLY (
    SELECT convert(varchar,cp.CodedNotation)  + '~' + cp.Name + '| '
    FROM dbo.[Entity_ReferenceFramework_Summary] cp  
    WHERE cp.CategoryId = 11
	and e.Id = cp.EntityId
    FOR XML Path('') ) OT (OccupationType)

CROSS APPLY 
	OPENJSON (base.[CreditValue])
		WITH (
			Value		decimal(8,2) N'$.Value',
			Description	VARCHAR(MAX) N'$.Description'
		) as credValueTranslate

/*
FROM dbo.[Entity.ReferenceFramework]		a
Inner Join Entity							e on a.EntityId = e.Id
INNER JOIN dbo.[Reference.FrameworkItem]	b ON a.ReferenceFrameworkItemId = b.Id
*/
where base.EntityStateId >= 2
GO
grant select on LearningOpportunity_Summary_Export to public
go


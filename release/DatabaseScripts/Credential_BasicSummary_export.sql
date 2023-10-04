USE [credFinder]
GO
use sandbox_credfinder
go
--use credfinder_github
--go
/****** Object:  View [dbo].[Credential_BasicSummary_Export]    Script Date: 9/5/2023 5:51:54 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

/*
USE [credFinder]
GO

SELECT top (1000)
a.[Id] as FinderId
	--  ,a.[EntityId]
	--   ,a.[RowId]
	,a.[CTID]
	,a.[Name]
	--    ,a.[AlternateName]
	,a.[Description]
	,a.[EntityStateId]
	--   ,a.[CredentialTypeId]
	--  ,a.[CredentialType]
	,a.[CredentialTypeSchema]
	--    ,a.[IsAQACredential]
	--     ,a.[CredentialStatusTypeId]
	,a.[CredentialStatus]
	--    ,a.[CredentialStatusId]
	--    ,a.[OwningAgentUid]
	--   ,a.[OrgEntityStateId]
	,a.[OwningOrganizationId]
	,a.[OwningOrganization]
	,a.[OwningOrganizationCTID]
	--    ,a.[PrimaryOrganizationUid]
	,a.[SubjectWebpage]
	--    ,a.[ImageUrl]
	,a.[DateEffective]
	,a.[ExpirationDate]
	,a.[availableOnlineAt]
	,a.[AvailabilityListing]
	--   ,a.[Version]
	--  ,a.[LatestVersionUrl]
	--  ,a.[PreviousVersion]
	--   ,a.[NextVersion]
	--   ,a.[Supersedes]
	--   ,a.[SupersededBy]
	--   ,a.[CopyrightHolder]
	,a.[CredentialId]
	,a.[ISICV4]
	,a.[ProcessStandards]
	,a.[ProcessStandardsDescription]
	,a.[Created]
	,a.[LastUpdated]
	,a.[EntityLastUpdated]
	,a.[DeliveryType]
	,a.[AudienceLevel]
	,a.[AudienceType]
	,a.[IndustryType]
	,a.[OccupationType]


      ,b.[TypeId]
	  ,b.[FromDuration]
      ,b.[ToDuration]
      ,b.[FromYears]
      ,b.[FromMonths]
      ,b.[FromWeeks]
      ,b.[FromDays]
      ,b.[FromHours]
      ,b.[FromMinutes]
      ,b.[ToYears]
      ,b.[ToMonths]
      ,b.[ToWeeks]
      ,b.[ToDays]
      ,b.[ToHours]
      ,b.[ToMinutes]
      ,b.[DurationComment]
      ,b.[DurationSummary]
  FROM [dbo].[Credential_BasicSummary_Export] a
  Left join Entity_DurationProfileSummary b on a.Id = b.EntityBaseId and b.EntityTypeId = 1

  --can be slow with order by
  --order by a.Name
GO






*/

/*
Summary view for credentials

-- =========================================================
23-09-04 mparsons - created a version for OECD
*/
Create VIEW [dbo].[Credential_BasicSummary_Export]
AS

SELECT  Distinct   
-- === IDs ===
	base.Id, 
	ec.Id as EntityId,
	base.RowId,
	isnull(base.CTID,'') As CTID, 
	-- common data 
	base.Name, 
	isnull(base.AlternateName,'') As AlternateName, 
	isnull(base.Description,'') As [Description], 
	base.EntityStateId,
	--
	base.CredentialTypeId,
	--friendly label for a credential type
	isnull(credTypeProperty.Title,'') As CredentialType,
	-- ctdl schema name
	isnull(credTypeProperty.SchemaName,'') As CredentialTypeSchema,
	--added virtual property, as the name can change and don't want buried in code. Added here for consistency, though will always be zero based on the where clause
	case when isnull(credTypeProperty.SchemaName,'') = 'qualityAssuranceCredential' then 1 else 0 end As IsAQACredential,
	-- new
	base.CredentialStatusTypeId,
	credStatus.Title as CredentialStatus,
	--retain temporarily
	base.CredentialStatusTypeId as CredentialStatusId,
	--
	--isnull(statusProperty.Property,'') As CredentialStatus,
	--isnull(statusProperty.PropertyValueId,'') As CredentialStatusId,

	-- ==== owning org =================================
	base.OwningAgentUid,
	owningOrg.EntityStateId as OrgEntityStateId,
	owningOrg.Id as OwningOrganizationId,
	owningOrg.Name as OwningOrganization,
	isnull(owningOrg.CTID,'') as OwningOrganizationCTID,
		--OR????
	base.[PrimaryOrganizationUid],
	-- =====================================

	-- =====================================
	isnull(base.SubjectWebpage,'') As SubjectWebpage, 

	base.ImageUrl,
	--base.EffectiveDate, 
	base.EffectiveDate AS 	DateEffective, 
	base.ExpirationDate,

	isnull(base.availableOnlineAt,'') As availableOnlineAt, 
	isnull(base.AvailabilityListing,'') As AvailabilityListing, 

	isnull(base.Version,'') As Version, 
	isnull(base.LatestVersionUrl,'') As LatestVersionUrl, 
	isnull(base.ReplacesVersionUrl,'') As PreviousVersion
	,base.[NextVersion]
	,base.[Supersedes]
	,base.[SupersededBy]


	,base.[CopyrightHolder]
	,base.[CredentialId]
	,base.[ISICV4]
	,base.[ProcessStandards]
	,base.[ProcessStandardsDescription]



	  ,
	base.Created, 	
	base.LastUpdated, 
	ec.LastUpdated as EntityLastUpdated


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
          WHEN IndustryType IS NULL THEN ''
          WHEN len(IndustryType) = 0 THEN ''
          ELSE left(IndustryType,len(IndustryType)-1)
    END AS IndustryType
	,CASE
          WHEN OccupationType IS NULL THEN ''
          WHEN len(OccupationType) = 0 THEN ''
          ELSE left(OccupationType,len(OccupationType)-1)
    END AS OccupationType

FROM       dbo.Credential base 
Inner Join Entity ec on base.RowId = ec.EntityUid
left join [Codes.PropertyValue] credTypeProperty on base.CredentialTypeId = credTypeProperty.Id 
Left Join [Codes.PropertyValue] credStatus on base.CredentialStatusTypeId = credStatus.Id

--Left Join EntityProperty_Summary statusProperty on base.RowId = statusProperty.EntityUid and statusProperty.CategoryId = 39
Left Join Organization					owningOrg on base.OwningAgentUid = owningOrg.RowId
--=================================
--convert(varchar,cp.PropertyValueId)  + '~' + 
CROSS APPLY (
    SELECT cp.Property + '| '
    FROM dbo.EntityProperty_Summary cp  
    WHERE cp.CategoryId = 21
	and ec.Id = cp.EntityId
	--Order by cp.PropertyValueId
    FOR XML Path('') 
	) D (DeliveryType)
--
CROSS APPLY (
    SELECT cp.Property + '| '
    FROM dbo.EntityProperty_Summary cp  
    WHERE cp.CategoryId = 4
	and ec.Id = cp.EntityId
	--Order by cp.PropertyValueId
    FOR XML Path('') 
	) AL (AudienceLevel)
--
CROSS APPLY (
    SELECT cp.Property + '| '
    FROM dbo.EntityProperty_Summary cp  
    WHERE cp.CategoryId = 14
	and ec.Id = cp.EntityId
	--Order by cp.PropertyValueId
    FOR XML Path('') 
	) AT (AudienceType)
--===============================
CROSS APPLY (
    SELECT convert(varchar,cp.CodedNotation)  + '~' + cp.Name + '| '
    FROM dbo.[Entity_ReferenceFramework_Summary] cp  
    WHERE cp.CategoryId = 10
	and ec.Id = cp.EntityId
    FOR XML Path('') ) IT (IndustryType)

CROSS APPLY (
    SELECT convert(varchar,cp.CodedNotation)  + '~' + cp.Name + '| '
    FROM dbo.[Entity_ReferenceFramework_Summary] cp  
    WHERE cp.CategoryId = 11
	and ec.Id = cp.EntityId
    FOR XML Path('') ) OT (OccupationType)

WHERE base.EntityStateId = 3 -- 


GO



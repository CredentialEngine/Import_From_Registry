USE [credFinder]
GO

--use sandbox_credFinder
--go

--use staging_credFinder
--go
/****** Object:  View [dbo].[SupportServiceSummary]    Script Date: 5/31/2020 9:44:06 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


/*




*/
Alter VIEW [dbo].[SupportServiceSummary]
AS

SELECT base.[Id]
      ,base.[RowId]
      ,base.[CTID]
      ,base.[EntityStateId]
      ,base.[Name]
      ,base.[Description]
      ,base.[PrimaryAgentUid]
	,isnull(owningOrg.Id,0) as OrganizationId
	,isnull(owningOrg.Name,'') as OrganizationName
	,isnull(owningOrg.CTID,'') as OrganizationCtid

      ,base.[SubjectWebpage]
      ,base.[AvailableOnlineAt]
      ,base.[AvailabilityListing]
      ,base.[AlternateName]
      ,base.[DateEffective]
      ,base.[ExpirationDate]
      ,base.[Identifier]
	,case when IsNull(base.LifeCycleStatusTypeId,0) > 0 then base.LifeCycleStatusTypeId
	else isnull(statusProperty.PropertyValueId,2648) end As LifeCycleStatusTypeId --default to production value for now
	,case when IsNull(base.LifeCycleStatusTypeId,0) > 0 then cpv.Title
	else isnull(statusProperty.Property,'') end As LifeCycleStatusType --

      ,base.[Keyword]

      ,base.[Created]      ,base.[LastUpdated]

	,IsNull(c1.Nbr,0) As SupportServiceConditionCount


  FROM [dbo].SupportService base
  left join [codes.PropertyValue] cpv on base.LifeCycleStatusTypeId = cpv.Id
-- join for owner
	Left join Organization owningOrg on base.[PrimaryAgentUid] = owningOrg.RowId and owningOrg.EntityStateId > 1
	Left Join EntityProperty_Summary	statusProperty on base.RowId = statusProperty.EntityUid and statusProperty.CategoryId = 84

	--support condition
	left join (
		Select b.EntityBaseId, Count(*) As Nbr from [Entity.ConditionProfile] a
		inner join Entity b on a.EntityId = b.Id
		where b.EntityTypeId= 38 
		AND ConnectionTypeId = 16 and isnull(ConditionSubTypeId, 1) = 1
		group by b.EntityBaseId
	) c1 on base.Id = c1.EntityBaseId

where base.EntityStateId >= 2

GO
grant select on [SupportServiceSummary] to public
go


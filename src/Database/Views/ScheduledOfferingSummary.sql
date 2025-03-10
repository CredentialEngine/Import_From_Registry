Use credFinder
go
--use sandbox_credFinder
--go
--use staging_credFinder	
--go
--use flstaging_credFinder	
--go


/****** Object:  View [dbo].[ScheduledOfferingSummary]    Script Date: 7/29/2020 11:13:19 AM ******/
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
      ,[EntityStateId]
      ,[Name]
      ,[Description]
      ,[SubjectWebpage]
      ,[CTID]
      ,[OwningAgentUid]
      ,[OrganizationCTID]
      ,[OrganizationId]
      ,[OrganizationName]
      ,[HasTransferValueProfiles]
      ,[CredentialRegistryId]
      ,[CodedNotation]
      ,[CreditValueJson]
      ,[IntermediaryForJson]
      ,[Subject]
      ,[Created]
      ,[LastUpdated]
  FROM [dbo].[ScheduledOfferingSummary]

GO



GO


*/
/*
ScheduledOfferingSummary
Notes
- 
Mods
23-04-05 mparsons - new


*/
Alter VIEW [dbo].[ScheduledOfferingSummary]
AS


SELECT  base.[Id]
		,e.Id as EntityId
		,base.[RowId]
		,base.[CTID]
		,base.[EntityStateId]
		,base.[Name]
		,base.[Description]
		,base.[OfferedBy]
		,base.[SubjectWebpage]
		,base.[DeliveryTypeDescription]
		,base.[AvailableOnlineAt]
		,base.[AvailabilityListing]
		,base.[AlternateName]
		,base.[Created]
		,base.[LastUpdated]

		,isnull(b.ctid,'')  as OrganizationCTID
		,b.Id as OrganizationId
		,b.Name as OrganizationName
	--
		,IsNull(aggregateDataProfile.Nbr,0) As AggregateDataProfileCount
--
  FROM [dbo].[ScheduledOffering] base

INNER JOIN dbo.Entity AS e ON base.RowId = e.EntityUid 
LEFT  JOIN dbo.Organization AS b ON base.[OfferedBy] = b.RowId

--=========
left join (
		Select b.EntityBaseId, COUNT(*)  As Nbr from [Entity.AggregateDataProfile] a Inner join Entity b ON a.EntityId = b.Id Where b.EntityTypeId = 15  Group By b.EntityBaseId
		) aggregateDataProfile	on base.Id = aggregateDataProfile.EntityBaseId 
where base.EntityStateId > 1
GO

grant select on [ScheduledOfferingSummary] to public
go



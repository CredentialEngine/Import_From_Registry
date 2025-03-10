use credFinder
go

--use credFinder_prod
--go

use sandbox_credFinder
go

--use staging_credFinder
--go


/****** Object:  View [dbo].[Entity_Summary]    Script Date: 7/26/2017 10:52:55 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

/*

SELECT [Id]
      ,[EntityTypeId]      ,[EntityType]
	  ,EntitySubTypeId
      ,[EntityUid]
      ,[parentEntityId]
      ,[parentEntityUid]
      ,[parentEntityType]
      ,[parentEntityTypeId]
      ,[BaseId]
      ,[Name]
      ,[Description]
      ,[Created]
      ,[LastUpdated]
      ,[OwningOrgId]
      ,[OwningOrganization]
      ,[SubjectWebpage]
      ,[ImageUrl]
      ,[CTID]
      ,[EntityStateId]
  FROM [dbo].[Entity_Summary]
  where EntityTypeId= 48
  or EntitySubTypeId= 37
GO




where id = 334


 -- where parententityuid = 'C10E0233-16E9-4790-9F77-AC4C0A33E901'
order by 
      [EntityType]
      ,[Name]


*/
/*
Modifications
17-08-22 mparsons - added to workIT
20-04-20 mparsons - replace EducationFrameworks with CompetencyFrameworks
21-05-29 mparsons - add OccupationProfile, JobProfile
21-11-24 mparsons - updated to use specific entityTypeIds for lopp classes - need to test implications
22-02-23 mparsons - Added collection. For credential, removed the inner join to credential type. 
22-03-01 mparsons - Added transfer intermediary
22-06-01 mparsons - now excluding non-top level resources
22-06-14 mparsons - added dataSet profile
22-07-07 mparsons - need to distinguish between the base type and a subtype. Use the base type (i.e. 7 for lopps) for main reporting
***** trying to move away from using this. Though may be useful for a full build ****
***** can we get rid of the non top level resources (ex. condition profiles, addresses)
24-02-15 mparsons - changed to just entity_cache
*/
Alter VIEW [dbo].[Entity_Summary]
AS
SELECT a.[Id]
      ,a.[EntityTypeId]
      ,a.[EntityType]
	  --TBD
	  ,a.EntityTypeId as EntitySubTypeId
      ,a.[EntityUid]
      ,a.[EntityStateId]
      ,a.[CTID]
      ,a.[parentEntityId]
      ,a.[parentEntityUid]
      ,a.[parentEntityType]
      ,a.[parentEntityTypeId]
      ,a.[BaseId]
      ,a.[Name]
      ,a.[Description]
      ,a.[SubjectWebpage]
      ,a.[OwningOrgId]
	  ,isnull(org.Name,'') as OwningOrganization --should be primaryOrg
      ,a.[ImageUrl]
      ,a.[Created]
      ,a.[LastUpdated]
      ,a.[CacheDate]
      ,a.[PublishedByOrgId]
      --,a.[ResourceDetail]
      --,a.[AgentRelationshipsForEntity]
      ,a.[IsActive]
  FROM [dbo].[Entity_Cache] a
  Left Join Organization org on a.OwningOrgId = org.Id
GO
grant select on [Entity_Summary] to public
go


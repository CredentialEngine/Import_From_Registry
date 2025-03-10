use credFinder
GO

use sandbox_credFinder
go

--use credFinder_prod
--go

/****** Object:  View [dbo].[Activity_Summary]    Script Date: 8/16/2017 2:05:02 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


/*
USE [credFinder]
GO

use credFinder_prod
go

SELECT top 1000
 [Id]
      ,[CreatedDate]
      ,[ActivityType]
      ,[EntityTypeId]
      ,[Activity]
      ,[Event]
      ,[Comment]
      --,[TargetUserId]
      ,[ActionByUserId], [ActionByUser]
      
      ,[ActivityObjectId], EntityName, EntityStateId, EntityCTID
      ,[ObjectRelatedId]
      ,[OwningOrgId]  ,[Organization], OrganizationEntityStateId
    
    --  ,[RelatedTargetUrl]
      ,[TargetObjectId]
      ,[SessionId]
      ,[IPAddress]
      ,[Referrer]
      ,[IsBot]
  FROM [dbo].[Activity_Summary]

  where 
  [ActivityType] like 'assess%'
 -- EntityTypeId in (1,3,7)
  and activity in ('view', 'import')
  and EntityStateId = 3
 -- event like '%view%'
order by id desc



*/
Alter  VIEW [dbo].[Activity_Summary]
AS

SELECT a.[Id]
      ,a.[CreatedDate]
      ,a.[ActivityType], d.Id as EntityTypeId
      ,[Activity]
      ,[Event]
      ,[Comment]
      ,[TargetUserId]
	  ,0 as ActionByUserId
	  ,'' as ActionByUser
     -- ,[ActionByUserId]
	 --, b.FirstName + ' ' + b.LastName as ActionByUser
      ,[ActivityObjectId]
	  ,IsNull(ec.Name,'') as EntityName, IsNull(ec.CTID,'') as EntityCTID
	  , isnull(ec.EntityStateId,0) as EntityStateId
      ,[ObjectRelatedId]
     ,ec.OwningOrgId, o.Name as Organization
	 , isnull(o.EntityStateId,0) as OrganizationEntityStateId
      ,[RelatedTargetUrl]
      ,[TargetObjectId]
      ,[SessionId]
      ,[IPAddress]
      ,[Referrer]
      ,[IsBot]

  FROM [dbo].[ActivityLog] a
  --left join Account b on a.ActionByUserId = b.Id
	 Left Join [Codes.EntityTypes] d on a.ActivityType = d.Title
	 Left Join Entity_Cache ec on a.ActivityObjectId = ec.BaseId AND d.Id = ec.EntityTypeId 
	 Left Join Organization o on ec.OwningOrgId = o.Id
	--where Activity <> 'session'
	--and convert(varchar(10),CreatedDate,120) = convert(varchar(10),getDate(),120)
	--order by createddate desc


GO



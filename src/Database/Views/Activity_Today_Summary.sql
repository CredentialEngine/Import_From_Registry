USE credFinder
GO

/****** Object:  View [dbo].[Activity_Today_Summary]    Script Date: 2/1/2018 1:44:47 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



Create  VIEW [dbo].[Activity_Today_Summary]
AS

SELECT top 100 percent 
a.[Id]
      ,[CreatedDate]
      ,a.[ActivityType]
      ,[Activity]
      ,[Event]
      ,[Comment]
      ,[TargetUserId]
      ,[ActionByUserId], b.FirstName + ' ' + b.LastName as ActionByUser
      ,[ActivityObjectId]
      ,[ObjectRelatedId]
      ,[RelatedTargetUrl]
      ,[TargetObjectId]
      ,[SessionId]
      ,[IPAddress]
      ,[Referrer]
      ,[IsBot]
  FROM [dbo].[ActivityLog] a
  left join Account b on a.ActionByUserId = b.Id
	where Activity <> 'session'
	and convert(varchar(10),CreatedDate,120) = convert(varchar(10),getDate(),120)
	--order by createddate desc
go
grant select on [Activity_Today_Summary] to public

GO



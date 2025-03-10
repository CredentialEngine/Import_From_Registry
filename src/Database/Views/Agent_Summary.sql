use credFinder
GO

/****** Object:  View [dbo].[Agent_Summary]    Script Date: 8/27/2017 10:24:49 PM ******/

/*

truncate table Agent_Cache
go
INSERT INTO [dbo].[Agent_Cache]
           ([AgentRowId]           ,[AgentTypeId]           ,[AgentType]
           ,[AgentRelativeId]
           ,[AgentName]
           ,[Summary]
           ,[City]           ,[Region]           ,[Country]           ,[Email]
           ,[SortOrder]
		   ,Created
		,LastUpdated)

SELECT [AgentRowId]
      ,[AgentTypeId]
      ,[AgentType]
      ,[AgentRelativeId]
      ,[AgentName]
      ,[Summary]
      ,[City]
      ,[Region]
      ,[Country]
      ,[Email]
      ,[SortOrder]
	  ,Created
		,LastUpdated
  FROM [dbo].[Agent_Summary]

-- ==================================================

SELECT [AgentRowId]
      ,[AgentTypeId]
      ,[AgentType]
      ,[AgentRelativeId]
      ,[AgentName]
      ,[Summary]
      ,[City]
      ,[Region]
      ,[Country]
      ,[Email]
      ,[SortOrder]
      ,[Description]
      ,[URL]
      ,[ImageURL]
      ,[Created]
      ,[LastUpdated]
  FROM [dbo].[Agent_Summary]
order by AgentRelativeId



*/
/*
Modifications
17-07-26 mparsons - removed union to person
									- removed joins to address - slows the query - need alternative
17-12-01 mparsons - added EntityStateId
*/
Alter VIEW [dbo].[Agent_Summary]
AS
SELECT        
	org.RowId AS AgentRowId, 
	2 as AgentTypeId,
	'Organization' As AgentType, 
	org.Id AS AgentRelativeId, 
	org.Name AS AgentName, 

	org.Name + ' (Organization) '  as  Summary, 
	isnull(org.CTID, '') As CTID, 
	isnull(org.EntityStateId, 0) As EntityStateId, 
	'Unavailable' As City, 
	'Unavailable' As Region, 
	'Unavailable' As Country, 
  '' As Email 
	,org.Name AS SortOrder
	,org.Description
	,org.SubjectWebpage, org.ImageURL
	,org.Created
	,org.LastUpdated
FROM dbo.Organization org


GO
grant select on [Agent_Summary] to public
go



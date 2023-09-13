
use credFinder
GO
use sandbox_credfinder
go

--use staging_credFinder
--go
--populate [Entity.SearchIndex]
-- ********* ACTUALLY SHOULD POPULATE SPECIFIC TO THE ENTITY
--
/*
truncate table [Entity.SearchIndex]

exec [Populate_Entity_SearchIndex] 0

*/

/*
Entity.SearchIndex - is populated with properties that will be used with TextValues in elastic search


22-11-18 mparsons - review if this is actually used anymore?
				- Yes, is used by the various ElasticSearch procs.

*/
Alter  Procedure [dbo].[Populate_Entity_SearchIndex]
	@EntityId int
	
AS
if @EntityId = 0 begin
	truncate table [Entity.SearchIndex]
	end
else begin
	DELETE FROM [dbo].[Entity.SearchIndex]
  WHERE [EntityId] = @EntityId
	end

-- framework items (occupations, etc)
INSERT INTO [dbo].[Entity.SearchIndex]
           ([EntityId]
           ,[CategoryId]
           ,[CodedNotation]
           ,TextValue
           ,[Desciption]
           --,[Created]
		   )

SELECT [EntityId]    ,[CategoryId] ,CodedNotation
       ,case when len(Name) > 200 then SUBSTRING(Name, 1,200) else Name end as Name
	   ,case when len(IsNull([Description],'')) > 800 then SUBSTRING([Description], 1,800) else [Description] end as [Description]
  FROM dbo.Entity_ReferenceFramework_Summary
where (EntityId = @EntityId OR @EntityId = 0)


--subjects, keywords, degree major, degree minor
INSERT INTO [dbo].[Entity.SearchIndex]
           ([EntityId]
           ,[CategoryId]
           ,[CodedNotation]
           ,TextValue
           ,[Desciption]
           ,[Created]
		   )

SELECT [EntityId] 
	,[CategoryId]
	,''
	,case when len([TextValue]) > 200 then SUBSTRING([TextValue], 1,200) else [TextValue] end as [TextValue]
	,''
    ,[Created]
 
  FROM dbo.[Entity.Reference] a
where CategoryId in (34,35, 63, 64)
and (a.EntityId = @EntityId OR @EntityId = 0)


--competencies
INSERT INTO [dbo].[Entity.SearchIndex]
           ([EntityId]
           ,[CategoryId]
           ,[CodedNotation]
           ,TextValue
           ,[Desciption]
           ,[Created]
		   )

SELECT 
	[EntityId]
	,29	--actually listed as competency items for condition profile
	,isnull(a.CodedNotation,'')
	,case when len(a.TargetNodeName) > 200 then SUBSTRING(TargetNodeName,1,200) else TargetNodeName end
	,case when len(isnull(TargetNodeDescription,'')) > 800 then SUBSTRING(TargetNodeDescription,1,800) else TargetNodeDescription end	
	,a.[Created]
 
  FROM [dbo].[Entity.Competency] a
	where
	(a.EntityId = @EntityId OR @EntityId = 0)

-- properties
INSERT INTO [dbo].[Entity.SearchIndex]
           ([EntityId]
           ,[CategoryId]
           ,[CodedNotation]
           ,TextValue
           ,[Desciption]
           ,[Created]
		   )

SELECT [EntityId]
       ,[CategoryId]
     -- ,[EntityBaseId]
	 ,''
	  ,case when len([Property]) > 200 then SUBSTRING([Property], 1,200) else [Property] end as [Property]
	  ,''
      ,[Created]
  FROM [dbo].[EntityProperty_Summary]

where isactive = 1
and [CategoryId] in (2,4,39)
AND (EntityId = @EntityId OR @EntityId = 0)


-- regions for main entity
INSERT INTO [dbo].[Entity.SearchIndex]
           ([EntityId]
           ,[CategoryId]
           ,[CodedNotation]
           ,TextValue
           ,[Desciption]
           ,[Created]
		   )

SELECT distinct  [EntityId]
       ,99 as [CategoryId]
	 ,''
	 --,Region
      ,case when b.StateCode is not null then b.State else  Region end as Region2
	 -- ,b.StateCode, b.State
	  ,''
	  ,getdate()
      --,max([Created])
  FROM [dbo].[Entity.Address] a
  Left Join [dbo].[Codes.State] b on a.Region = b.StateCode

where (EntityId = @EntityId OR @EntityId = 0)
group by EntityId, Region, b.StateCode, b.State


-- regions for owning org of credentials
INSERT INTO [dbo].[Entity.SearchIndex]
           ([EntityId]
           ,[CategoryId]
           ,[CodedNotation]
           ,TextValue
           ,[Desciption]
           ,[Created]
		   )

SELECT distinct  ce.Id AS [EntityId]
       ,99 as [CategoryId]
	 ,''
	 --,Region
      ,case when cs.StateCode is not null then cs.State else  Region end as Region2
	 -- ,b.StateCode, b.State
	  ,''
	  ,getdate()
      --,max([Created])
  FROM Credential c
  inner join Entity ce on c.RowId = ce.EntityUid
  inner join Entity b on c.OwningAgentUid = b.EntityUid
  inner join [dbo].[Entity.Address] a on b.Id = a.EntityId
  Left Join [dbo].[Codes.State] cs on a.Region = cs.StateCode
  left join [Entity.SearchIndex] esi on ce.Id = esi.EntityId

where esi.Id is null
AND (ce.Id = @EntityId OR @EntityId = 0) 

group by ce.Id, Region, cs.StateCode, cs.State

-- regions for owning org of assessments
INSERT INTO [dbo].[Entity.SearchIndex]
           ([EntityId]
           ,[CategoryId]
           ,[CodedNotation]
           ,TextValue
           ,[Desciption]
           ,[Created]
		   )

SELECT distinct  ce.Id AS [EntityId]
       ,99 as [CategoryId]
	 ,''
	 --,Region
      ,case when cs.StateCode is not null then cs.State else  Region end as Region2
	 -- ,b.StateCode, b.State
	  ,''
	  ,getdate()
      --,max([Created])
  FROM Assessment c
  inner join Entity ce on c.RowId = ce.EntityUid
  inner join Entity b on c.OwningAgentUid = b.EntityUid
  inner join [dbo].[Entity.Address] a on b.Id = a.EntityId
  Inner Join [dbo].[Codes.State] cs on a.Region = cs.StateCode
  left join [Entity.SearchIndex] esi on ce.Id = esi.EntityId

where esi.Id is null
AND (ce.Id = @EntityId OR @EntityId = 0) 

group by ce.Id, Region, cs.StateCode, cs.State


-- regions for owning org of lopps
INSERT INTO [dbo].[Entity.SearchIndex]
           ([EntityId]
           ,[CategoryId]
           ,[CodedNotation]
           ,TextValue
           ,[Desciption]
           ,[Created]
		   )

SELECT distinct  ce.Id AS [EntityId]
       ,99 as [CategoryId]
	 ,''
	 --,Region
      ,case when cs.StateCode is not null then cs.State else  Region end as Region2
	 -- ,b.StateCode, b.State
	  ,''
	  ,getdate()
      --,max([Created])
  FROM LearningOpportunity c
  inner join Entity ce on c.RowId = ce.EntityUid
  inner join Entity b on c.OwningAgentUid = b.EntityUid
  inner join [dbo].[Entity.Address] a on b.Id = a.EntityId
  Left Join [dbo].[Codes.State] cs on a.Region = cs.StateCode
  left join [Entity.SearchIndex] esi on ce.Id = esi.EntityId

where esi.Id is null
AND (ce.Id = @EntityId OR @EntityId = 0) 

group by ce.Id, Region, cs.StateCode, cs.State

go

grant execute on [Populate_Entity_SearchIndex] to public
go


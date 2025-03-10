USE [credFinder]
GO

use sandbox_credFinder
go
--USE staging_credFinder
--GO


--use flstaging_credFinder
--go

--use txlibrary_credFinder	
--go
--use chaffey_credFinder	
--go

/****** Object:  View [dbo].[LearningOpportunity_Competency_Summary]    Script Date: 10/6/2017 4:16:47 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

/*


SELECT [EntityId]
      ,[LearningOpportunity]
      ,[LearningOpportunityId]
      ,[Name]
      ,[Description]
      ,[TargetName]
      ,[TargetDescription]
      ,[TargetUrl]
      ,[CodedNotation]
      ,[AlignmentType]
  FROM [dbo].[LearningOpportunity_Competency_Summary]
GO
distinct
select * from LearningOpportunity_summary base
where 
( base.Id in (SELECT  LearningOpportunityId FROM [dbo].LearningOpportunity_Competency_Summary  where AlignmentType = 'teaches' AND (([Name] like '%design%' OR [Description] like '%design%')) ) )

*/
/*
LearningOpportunity_Competency_Summary
- list of teaches competencies for a learning opportunity
Modifications
21-05-02 mparsons - stop using: EntityCompetencyFramework_Items_Summary
					- too many joins result

*/
Alter VIEW [dbo].[LearningOpportunity_Competency_Summary]
AS
  /*
     entity.Competency
			 entity (for parent LO)
				LearningOpportunity parent

    			entity.LearningOpp (under the LearningOpportunity entity - for 
    				entity (parent LO)
							LearningOpportunity (rowId)

	*/

SELECT 
	---comp.EntityCompetencyFrameworkItemId as Id,
	IsNUll(b.Id,0) AS Id
	,comp.[EntityId]
	,lopp.Name as LearningOpportunity
	,lopp.Id as LearningOpportunityId
	,lopp.CTID as LearningOpportunityCTID
	,comp.[FrameworkName]
	,comp.TargetNodeName AS Competency
	,comp.TargetNodeName AS Name 
	--,comp.Competency
	--,comp.Competency As Name
	,comp.TargetNodeDescription
	,comp.TargetNodeDescription as Description
	,comp.TargetNode
	,comp.[CodedNotation]
	,comp.Alignment as AlignmentType
	,comp.Created  as CompetencyCreated
	--  ,'teaches' as [AlignmentType]
	--,AlignmentTypeId
     
	,parentLopp.Id as ParentLearningOpportunityId
	,parentLopp.Name as ParentLearningOpportunity
	  --,parentLopp.Organization as ParentOrganization
--	select *
  --FROM  [EntityCompetencyFramework_Items_Summary] a
FROM       dbo.[Entity.Competency] comp
--may not have a framework. Why? Collections. 
Left JOIN dbo.CompetencyFramework b ON comp.CompetencyFrameworkId = b.id
inner join Entity e			on comp.EntityId = e.Id
inner join LearningOpportunity lopp on e.EntityUid = lopp.RowId

  --get related Entity.LearningOpportunity to get a parent lopp if present. 
  --	NOT SURE OF VALUE VS PERFORMANCE
  LEFT join [Entity.LearningOpportunity] entityLopp on lopp.Id = entityLopp.LearningOpportunityId
  --get parent (most likely comp is at a embedded Lopp level)
  left join Entity pe on entityLopp.EntityId = pe.Id
  left join LearningOpportunity parentLopp on pe.EntityUid = parentLopp.RowId

  where e.EntityTypeId = 7


GO



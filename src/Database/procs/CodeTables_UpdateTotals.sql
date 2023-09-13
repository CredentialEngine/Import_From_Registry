USE credFinder
GO

--use credFinder_prod
--go

--use staging_credFinder
--go

--use sandbox_credFinder
--go

--use snhu_credFinder
--go

/****** Object:  StoredProcedure [dbo].[CodeTables_UpdateTotals]    Script Date: 1/24/2018 11:28:09 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


/*
SELECT tags.EntityTypeId,count(*) AS Totals
  FROM [dbo].[Entity] tags
  --inner join Entity_summary e on tags.Id = e.Id
  --where e.StatusId <= 3   --@StatusId
  group by tags.EntityTypeId

use sandbox_credFinder
go
exec [CodeTables_UpdateTotals]

select * from [Counts.SiteTotals]

*/

/*
Modifications
16-09-02 mparsons - added totals for costTypeId, connection profile type
18-03-28 mparsons - added 
20-04-06 mparsons - made copy of this proc and saved as CodeTables_UpdateTotalsV2. 
					The non-Vnn version will always be the current one
20-04-06 mparsons - changed to not set major totals to zero at start of process:
					- Counts.EntityStatistic
					- Codes.EntityTypes
					- Codes.ConditionProfileType
					- Codes.CredentialAgEntityStatisticentRelationship
21-02-17 mparsons	- added SchemaName to Counts.SiteTotals
21-02-22 mparsons	- consider including all properties (from codes.PropertyValue) in Counts.SiteTotals - in order to be able to see all (optionally) in filter lists
					- can't since this table includes entity typeId.
					  Would have to do a separate populate for each entity type
21-05-14 mparsons	- removed competency frameworks (10) when updating Codes.EntityTypes
21-06-21 mparsons	- add transfer value entity stats
23-08-10 mparsons	- noticed the LifeCycleStatusType (84) was still assumed part of entity.property, and it is always on the base table now.
					- also credentialStatus?
*/
ALTER  Procedure [dbo].[CodeTables_UpdateTotals]
    @debugLevel int = 0

AS
declare @StatusId int
set @StatusId = 1

-- first reset (to handle where all instances of a property have been deleted)
UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = 0

--NEW ***
-- will replace the reports portion of [Codes.PropertyValue]
-- the competency framework related counts are done dynamically, so do NOT reset here
--UPDATE [dbo].[Counts.EntityStatistic]
--   SET [Totals] = 0
--where EntityTypeId in (1,2,3,7)

-- site totals
-- will replace the reports portion of [Codes.PropertyValue]
truncate table [dbo].[Counts.SiteTotals]
--
truncate table [Counts.RegionTotals] 

--UPDATE [dbo].[Codes.ConditionProfileType]
--   SET [Totals] = 0
--   , CredentialTotals = 0
--   , AssessmentTotals = 0
--   , LoppTotals = 0

--UPDATE [dbo].[Codes.CredentialAgentRelationship]
--   SET [Totals] = 0
--   , CredentialTotals = 0
--   , OrganizationTotals = 0
--   , AssessmentTotals = 0
--   , LoppTotals = 0

--actually don't reset to work around where the tables are not repopulated
--or at least major ones?
--UPDATE [dbo].[Codes.EntityTypes]
--   SET [Totals] = 0
--where id <> 10

--UPDATE [dbo].NAICS
--   SET [Totals] = 0
--UPDATE [dbo].ONET_SOC
--   SET [Totals] = 0
--UPDATE [dbo].[ONET_SOC.JobFamily]
--   SET [Totals] = 0

--UPDATE [dbo].CIPCode2010
--   SET [Totals] = 0

UPDATE [dbo].[Codes.PathwayComponentType]
   SET [Totals] = 0

-- ==========================================================
print 'updating all [Codes.PropertyValue] from [Entity.Property] ...'
--only include properties that related a single entity type, otherwise will use [Counts.SiteTotals]
--note that  'qualityAssuranceCredential' will be ignored, which affects counts
UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [Codes.PropertyValue] codes
Inner join ( 
SELECT [PropertyValueId],count(*) AS Totals
  FROM [dbo].[Entity.Property] tags
  inner join Entity_summary e on tags.EntityId = e.Id
  group by [PropertyValueId]
  --order by [PropertyValueId]
    ) base on codes.Id = base.[PropertyValueId]

where codes.IsActive = 1 
and codes.CategoryId NOT in (4, 14,18,21, 39, 84) --cred stat, and lifeCycle are on the base
--		see below

-- ====================================================
-- CredentialStatusTypeId
UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [Codes.PropertyValue] codes
Inner join ( 
  select CredentialStatusTypeId, count(*) as  Totals
  from Credential a
  inner join  [Codes.PropertyValue] codes on a.CredentialStatusTypeId = codes.Id
  where EntityStateId = 3
  group by CredentialStatusTypeId
) base on codes.Id = base.CredentialStatusTypeId

where codes.IsActive = 1 
and codes.CategoryId = 39
/*
select Id, title, Totals from  [dbo].[Codes.PropertyValue] where CategoryId=39
*/
--		


-- =======================
-- use Codes.SiteTotals
-- Audience Level 4
-- audience type 14
-- delivery type 18,21
-- lifeCycleStatus 84 -no removed as not part of Entity.Property

INSERT INTO [dbo].[Counts.SiteTotals]
           ([CategoryId]
           ,[EntityTypeId]
           ,[CodeId]
           ,[Title],SchemaName
           ,[Totals])

SELECT codes.CategoryId, base.EntityTypeId, base.[PropertyValueId], isnull(codes.title,''), codes.SchemaName, base.Totals
from [Codes.PropertyValue] codes 
Inner Join (
SELECT e.EntityTypeId, tags.[PropertyValueId], Isnull(count(*),0) AS Totals
  FROM [dbo].[Entity.Property] tags
  inner join Entity_summary e on tags.EntityId = e.Id
  Inner Join [Codes.PropertyValue] codes on tags.PropertyValueId = codes.id 

  where codes.IsActive = 1 
	and codes.CategoryId	in (4,14,18,21) --???
	and e.EntityTypeId		in (1,2,3,7,8,26,36, 37, 38)
	And e.EntityStateId = 3
  group by e.EntityTypeId, tags.[PropertyValueId]
  --order by e.EntityTypeId, tags.[PropertyValueId]
) base On codes.id = base.PropertyValueId
order by codes.CategoryId, base.EntityTypeId, base.[PropertyValueId], codes.title 
--

-- =======================
-- LifeCycleStatusTypeId 
-- use Codes.SiteTotals
-- categoryId = 84

--LOPP
  INSERT INTO [dbo].[Counts.SiteTotals]
           ([CategoryId]
           ,[EntityTypeId]
		   ,[CodeId]
           ,[Title]
           ,[Totals])

select 84, 7, LifeCycleStatusTypeId, cpv.title, count(*) as  Totals
  from LearningOpportunity a
 inner join  [Codes.PropertyValue] cpv on a.LifeCycleStatusTypeId = cpv.Id
  where a.EntityStateId = 3 AND  cpv.IsActive = 1
  group by LifeCycleStatusTypeId, cpv.title


--Asmt
  INSERT INTO [dbo].[Counts.SiteTotals]
           ([CategoryId]
           ,[EntityTypeId]
		   ,[CodeId]
           ,[Title]
           ,[Totals])

select 84, 3, LifeCycleStatusTypeId, cpv.title, count(*) as  Totals
  from Assessment a
 inner join  [Codes.PropertyValue] cpv on a.LifeCycleStatusTypeId = cpv.Id
  where a.EntityStateId = 3 AND  cpv.IsActive = 1
  group by LifeCycleStatusTypeId, cpv.title


--Org
  INSERT INTO [dbo].[Counts.SiteTotals]
           ([CategoryId]
           ,[EntityTypeId]
		   ,[CodeId]
           ,[Title]
           ,[Totals])

select 84, 2, LifeCycleStatusTypeId, cpv.title, count(*) as  Totals
  from Organization a
 inner join  [Codes.PropertyValue] cpv on a.LifeCycleStatusTypeId = cpv.Id
  where a.EntityStateId = 3 AND  cpv.IsActive = 1
  group by LifeCycleStatusTypeId, cpv.title

--collection
  INSERT INTO [dbo].[Counts.SiteTotals]
           ([CategoryId]
           ,[EntityTypeId]
		   ,[CodeId]
           ,[Title]
           ,[Totals])

select 84, 9, LifeCycleStatusTypeId, cpv.title, count(*) as  Totals
  from Collection a
 inner join  [Codes.PropertyValue] cpv on a.LifeCycleStatusTypeId = cpv.Id
  where a.EntityStateId = 3 AND  cpv.IsActive = 1
  group by LifeCycleStatusTypeId, cpv.title
  
--transfer value
  INSERT INTO [dbo].[Counts.SiteTotals]
           ([CategoryId]
           ,[EntityTypeId]
		   ,[CodeId]
           ,[Title]
           ,[Totals])

select 84, 26, LifeCycleStatusTypeId, cpv.title, count(*) as  Totals
  from TransferValueProfile a
 inner join  [Codes.PropertyValue] cpv on a.LifeCycleStatusTypeId = cpv.Id
  where a.EntityStateId = 3 AND  cpv.IsActive = 1
  group by LifeCycleStatusTypeId, cpv.title
  
--supportService
  INSERT INTO [dbo].[Counts.SiteTotals]
           ([CategoryId]
           ,[EntityTypeId]
		   ,[CodeId]
           ,[Title]
           ,[Totals])

select 84, 38, LifeCycleStatusTypeId, cpv.title, count(*) as  Totals
  from supportService a
 inner join  [Codes.PropertyValue] cpv on a.LifeCycleStatusTypeId = cpv.Id
  where a.EntityStateId = 3 AND  cpv.IsActive = 1
  group by LifeCycleStatusTypeId, cpv.title


-- =======================
-- Language 
-- use Codes.SiteTotals
-- categoryId = 65

  INSERT INTO [dbo].[Counts.SiteTotals]
           ([CategoryId]
           ,[EntityTypeId]
		   ,[CodeId]
           ,[Title]
           ,[Totals])

SELECT distinct 65, e.EntityTypeId, 0, RTrim(LTrim(isnull(tags.title,''))), count(*) AS Totals
  FROM [dbo].[Entity.Reference] tags
  inner join Entity e on tags.EntityId = e.Id
  where tags.CategoryId = 65
  AND e.EntityTypeId in  (1,2,3,7,8,26,36, 37, 38)
  group by e.EntityTypeId, tags.Title

order by 1,2,3


-- ==========================================================
print 'updating all [Codes.PropertyValue] credential type from credential ...'

UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [Codes.PropertyValue] codes
Inner join ( 
SELECT CredentialTypeId, count(*) AS Totals
  FROM [dbo].Credential tags
  where tags.EntityStateId = 3
  group by CredentialTypeId
    ) base on codes.Id = base.CredentialTypeId
where codes.IsActive = 1
  -- ==========================================================
print 'updating all [Codes.PropertyValue] from [Entity.Reference] ...'

UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [Codes.PropertyValue] codes
Inner join ( 
SELECT [PropertyValueId],count(*) AS Totals
  FROM [dbo].[Entity.Reference] tags
  inner join Entity_summary e on tags.EntityId = e.Id
  where e.EntityStateId = 3
  group by [PropertyValueId]
    ) base on codes.Id = base.[PropertyValueId]
where codes.IsActive = 1

-- ==========================================================
print 'updating all [Codes.PropertyValue] from [Entity.CostProfileItem] ...'

UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [Codes.PropertyValue] codes
Inner join ( 
SELECT [CostTypeId],count(*) AS Totals
  FROM [dbo].[Entity.CostProfileItem] tags
	inner join [Entity.CostProfile] cp on tags.CostProfileId = tags.Id
	inner join Entity_summary e on cp.EntityId = e.Id
	where e.EntityStateId = 3
  group by [CostTypeId]
    ) base on codes.Id = base.[CostTypeId]
where codes.IsActive = 1
	
-- ==========================================================
print 'updating all [Codes.PropertyValue] from [Entity.ConditionProfile] ...'

UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [Codes.PropertyValue] codes
Inner join ( 
SELECT ConnectionTypeId,count(*) AS Totals
  FROM [dbo].[Entity.ConditionProfile] tags
	inner join Entity_summary e on tags.EntityId = e.Id
	where e.EntityStateId = 3
  group by ConnectionTypeId
    ) base on codes.Id = base.ConnectionTypeId
where codes.IsActive = 1


-- ==========================================================
print 'updating all [Codes.ConditionProfileType] from [Entity.ConditionProfile]  credential ...'
--==> CREDENTIAL CONNECTIONS ONLY
UPDATE [dbo].[Codes.ConditionProfileType]
   SET CredentialTotals = isnull(base.Totals,0)
from [Codes.ConditionProfileType] codes
Inner join ( 
	SELECT ttls.ConnectionTypeId, count(*) AS Totals
	  FROM  (
		SELECT ConnectionTypeId, e.EntityBaseId, count(*) AS Totals
		  FROM [dbo].[Entity.ConditionProfile] tags
			inner join Entity e on tags.EntityId = e.Id
			Inner Join Credential c on e.EntityUid = c.RowId
			where c.EntityStateId=3
			and IsNull(tags.ConditionSubTypeId,1) = 2
		  group by ConnectionTypeId, EntityBaseId
		 -- order by ConnectionTypeId,e.EntityBaseId
	  ) ttls 
		group by ttls.ConnectionTypeId
    ) base on codes.Id = base.ConnectionTypeId
where codes.IsActive = 1

print ' ----- assessments ...'
--==> CREDENTIAL CONNECTIONS ONLY ???
UPDATE [dbo].[Codes.ConditionProfileType]
   SET AssessmentTotals = isnull(base.Totals,0)
from [Codes.ConditionProfileType] codes
Inner join ( 
	SELECT ttls.ConnectionTypeId, count(*) AS Totals
	  FROM  (
		SELECT ConnectionTypeId, e.EntityBaseId, count(*) AS Totals
		  FROM [dbo].[Entity.ConditionProfile] tags
			inner join Entity e on tags.EntityId = e.Id
			Inner Join Assessment c on e.EntityUid = c.RowId
			where c.EntityStateId=3
			and IsNull(tags.ConditionSubTypeId,1) = 3
			--where ConnectionTypeId = 7
		  group by ConnectionTypeId, EntityBaseId
		 -- order by ConnectionTypeId,e.EntityBaseId
	  ) ttls 
		group by ttls.ConnectionTypeId
    ) base on codes.Id = base.ConnectionTypeId
where codes.IsActive = 1

print ' ----- lopp ...'
UPDATE [dbo].[Codes.ConditionProfileType]
   SET LoppTotals = isnull(base.Totals,0)
from [Codes.ConditionProfileType] codes
Inner join ( 
	SELECT ttls.ConnectionTypeId, count(*) AS Totals
	  FROM  (
		SELECT ConnectionTypeId, e.EntityBaseId, count(*) AS Totals
		  FROM [dbo].[Entity.ConditionProfile] tags
			inner join Entity e on tags.EntityId = e.Id
			Inner Join LearningOpportunity c on e.EntityUid = c.RowId
			where c.EntityStateId=3
			and IsNull(tags.ConditionSubTypeId,1) = 4
			--where ConnectionTypeId = 7
		  group by ConnectionTypeId, EntityBaseId
		 -- order by ConnectionTypeId,e.EntityBaseId
	  ) ttls 
		group by ttls.ConnectionTypeId
    ) base on codes.Id = base.ConnectionTypeId
where codes.IsActive = 1

print ' ----- isPartOf ...'
UPDATE [dbo].[Codes.ConditionProfileType]
   SET CredentialTotals = isnull(base.Totals,0)
from [Codes.ConditionProfileType] codes
Inner join ( 
SELECT count(*) AS Totals
  FROM  (
	SELECT tags.CredentialEntityId, count(*) AS Totals
	  FROM [dbo].[Credential_EmbeddedCredentials_Summary] tags
	  Inner Join Credential c on tags.ParentCredentialId = c.Id
	  where c.EntityStateId=3
	  group by tags.CredentialEntityId
	  ) ttls 
    ) base on codes.Id = 2293
where codes.IsActive = 1
--140

-- ==========================================================

if @debugLevel > 8  begin  
	SELECT [PK]
		  ,[CategoryId]
		  ,[Category]
		  ,[CategorySchemaName]
		  ,[CategorySchemaUrl]
		  ,[PropertyId]
		  ,[Property]
		  ,[SortOrder]
		  --,[PropertySchemaName]
		  --,[PropertySchemaUrl]
	  FROM [dbo].[CodesProperty.Summary]
	  order by categoryId, [SortOrder]
  end

-- ===========================================================
print '[Codes.CredentialAgentRelationship] ...'
--this either to be split into 4 or use entity_cache
-- TODO - handling of lopp subtypes?
INSERT INTO [dbo].[Counts.SiteTotals]
			([CategoryId]
           ,[EntityTypeId]
		   ,[CodeId]
           ,[Title]
           ,[Totals]) 

SELECT 13, ttls.EntityTypeId, ttls.RelationshipTypeId, isnull(codes.title,''), IsNull(count(*),0) AS Totals
FROM  (
	SELECT b.EntityTypeId, RelationshipTypeId, tags.EntityId
		,count(*) AS Totals
		  FROM [dbo].[Entity.AgentRelationship] tags
		  inner join Entity b on tags.EntityId = b.Id
		  inner join [Entity_Cache] ec on b.Id = ec.Id
		  where ec.EntityStateId = 3
		  group by b.EntityTypeId, RelationshipTypeId, tags.EntityId
		  --order by RelationshipTypeId
) ttls 
Inner Join [Codes.CredentialAgentRelationship] codes on ttls.RelationshipTypeId = codes.Id
where codes.IsActive = 1
group by ttls.EntityTypeId, ttls.RelationshipTypeId, codes.Title

print 'NEW [Counts.SiteTotals] QAPerformed ...'
--should this include references???
INSERT INTO [dbo].[Counts.SiteTotals]
			([CategoryId]
           ,[EntityTypeId]
		   ,[CodeId]
           ,[Title]
           ,[Totals]) 

SELECT 130, ttls.EntityTypeId, ttls.AssertionTypeId, IsNuLL(codes.ReverseRelation,''), IsNull(count(*),0) AS Totals
FROM  (
	SELECT b.EntityTypeId, AssertionTypeId, tags.EntityId
		,count(*) AS Totals
		  FROM [dbo].[Entity.Assertion] tags
		  inner join Entity b on tags.EntityId = b.Id
		  --this
		  where b.EntityTypeId = 2 
		  and AssertionTypeId in (1,2,10,12)
		  --OR
		  --inner join Organization c on b.EntityUid = c.RowId
		  --where c.EntityStateId = 3
		  --and AssertionTypeId in (1,2,10,12)

		  group by b.EntityTypeId, AssertionTypeId, tags.EntityId
) ttls 
Inner Join [Codes.CredentialAgentRelationship] codes on ttls.AssertionTypeId = codes.Id
where codes.IsActive = 1
group by ttls.EntityTypeId, ttls.AssertionTypeId, codes.ReverseRelation

-- ====================================================================
-- OLD
UPDATE [dbo].[Codes.CredentialAgentRelationship]
   SET CredentialTotals = isnull(base.Totals,0)
from [Codes.CredentialAgentRelationship] codes
Inner join ( 
	SELECT ttls.RelationshipTypeId, count(*) AS Totals
	  FROM  (
		SELECT  RelationshipTypeId, tags.EntityId
		,count(*) AS Totals
		  FROM [dbo].[Entity.AgentRelationship] tags
		  inner join Entity b on tags.EntityId = b.Id
		  Inner Join Credential c on b.EntityUid = c.RowId
		  where c.EntityStateId = 3
		  group by RelationshipTypeId, tags.EntityId
		  --order by RelationshipTypeId
	  ) ttls 
	group by ttls.RelationshipTypeId
    ) base on codes.Id = base.RelationshipTypeId
where codes.IsActive = 1

-- -----
print '[Codes.CredentialAgentRelationship] organization ...'
UPDATE [dbo].[Codes.CredentialAgentRelationship]
   SET OrganizationTotals = isnull(base.Totals,0)
from [Codes.CredentialAgentRelationship] codes
Inner join ( 
	SELECT ttls.RelationshipTypeId, count(*) AS Totals
	  FROM  (
		SELECT  RelationshipTypeId, tags.EntityId
		,count(*) AS Totals
		  FROM [dbo].[Entity.AgentRelationship] tags
		  inner join Entity b on tags.EntityId = b.Id
		  Inner Join Organization c on b.EntityUid = c.RowId
		  where c.EntityStateId = 3
		  group by RelationshipTypeId, tags.EntityId
		  --order by RelationshipTypeId
	  ) ttls 
	group by ttls.RelationshipTypeId
    ) base on codes.Id = base.RelationshipTypeId
where codes.IsActive = 1


print '[Codes.CredentialAgentRelationship] Assessment ...'
UPDATE [dbo].[Codes.CredentialAgentRelationship]
   SET AssessmentTotals = isnull(base.Totals,0)
from [Codes.CredentialAgentRelationship] codes
Inner join ( 
	SELECT ttls.RelationshipTypeId, count(*) AS Totals
	  FROM  (
		SELECT  RelationshipTypeId, tags.EntityId
		,count(*) AS Totals
		  FROM [dbo].[Entity.AgentRelationship] tags
		  inner join Entity b on tags.EntityId = b.Id
		  Inner Join Assessment c on b.EntityUid = c.RowId
		  where c.EntityStateId = 3
		  group by RelationshipTypeId, tags.EntityId
		  --order by RelationshipTypeId
	  ) ttls 
	group by ttls.RelationshipTypeId
    ) base on codes.Id = base.RelationshipTypeId
where codes.IsActive = 1

print '[Codes.CredentialAgentRelationship] LearningOpportunity ...'
UPDATE [dbo].[Codes.CredentialAgentRelationship]
   SET LoppTotals = isnull(base.Totals,0)
from [Codes.CredentialAgentRelationship] codes
Inner join ( 
	SELECT ttls.RelationshipTypeId, count(*) AS Totals
	  FROM  (
		SELECT  RelationshipTypeId, tags.EntityId
		,count(*) AS Totals
		  FROM [dbo].[Entity.AgentRelationship] tags
		  inner join Entity b on tags.EntityId = b.Id
		  Inner Join LearningOpportunity c on b.EntityUid = c.RowId
		  where c.EntityStateId = 3
		  group by RelationshipTypeId, tags.EntityId
		  --order by RelationshipTypeId
	  ) ttls 
	group by ttls.RelationshipTypeId
    ) base on codes.Id = base.RelationshipTypeId
where codes.IsActive = 1


-- ===========================================================
--print 'updating all [Codes.EntityTypes] (except competency frameworks-10) ...'
print 'moved updates of Codes.EntityTypes to [CodesEntityTypes_UpdateTotals]'
--top level


--UPDATE [dbo].[Codes.EntityTypes]
--   SET [Totals] = isnull(base.Totals,0)
--from [Codes.EntityTypes] codes
--Inner join ( 
--SELECT tags.EntityTypeId,count(*) AS Totals
--  FROM [dbo].[Entity] tags
--  inner join Entity_summary e on tags.Id = e.Id
--	  and e.EntityTypeId in (1,2,3,7,8,9,11,19,20,23,24, 26, 32,33,34,35) --update Entity_Summary to include 27, or use alternate (better) means to get total 
--	  and e.EntityStateId > 2
--	  and Len(isnull(e.ctid,'')) > 10
--  group by tags.EntityTypeId
--    ) base on codes.Id = base.EntityTypeId
--where codes.IsActive = 1

-- ==== updated comp frameworks?????

/*
declare @dpCount int
print 'updating comp frameworks ...'

Select @dpCount = count(*) FROM [CompetencyFramework] tags
  where tags.EntityStateId = 3 and tags.ExistsInRegistry = 1
print 'comp frameworks = ' + convert(varchar,@dpCount)
-- 
UPDATE [dbo].[Codes.EntityTypes]
   SET [Totals] = isnull(@dpCount,0)
from [Codes.EntityTypes] codes
where codes.Id = 10
*/

--select id, totals FROM [credFinder].[dbo].[Codes.EntityTypes] where id = 10

-- =====
print 'updating all [Codes.EntityTypes] for indirect:[Entity.DurationProfile] ...'
declare @dpCount int
Select @dpCount = count(*) FROM [dbo].[Entity.DurationProfile] tags
  inner join Entity_summary e on tags.EntityId = e.Id
-- 
UPDATE [dbo].[Codes.EntityTypes]
   SET [Totals] = isnull(@dpCount,0)
from [Codes.EntityTypes] codes
where codes.Id = 22

print 'updating all [Codes.EntityTypes] for indirect:[Entity.ContactPoint] ...'
declare @cpCount int
Select @cpCount = count(*) FROM [dbo].[Entity.ContactPoint] tags
  inner join Entity_summary e on tags.Id = e.Id

UPDATE [dbo].[Codes.EntityTypes]
   SET [Totals] = isnull(@cpCount,0)
from [Codes.EntityTypes] codes
where codes.Id = 15


print 'updating all [Codes.EntityTypes] for indirect:[Entity.Address] ...'
declare @adrCount int
Select @adrCount = count(*) FROM [dbo].[Entity.Address] tags
  inner join Entity_summary e on tags.Id = e.Id


UPDATE [dbo].[Codes.EntityTypes]
   SET [Totals] = isnull(@adrCount,0)
from [Codes.EntityTypes] codes
where codes.Id = 16

-- ===========================================================
print 'updating all [NAICS] ...'
-- need to get a unique name for the groups
INSERT INTO [dbo].[Counts.SiteTotals]
			([CategoryId]
           ,[EntityTypeId]
		   ,[CodeId]
           ,[Title]
           ,[Totals]) 
SELECT a.[CategoryId], c.EntityTypeId
	  ,[CodeGroup]  
      ,d.NaicsTitle
      , COUNT (*) as totals
  FROM [dbo].[Reference.FrameworkItem] a 
  INNER JOIN [Entity.ReferenceFramework] b ON a.Id = b.ReferenceFrameworkItemId
  INNER JOIN Entity c ON b.EntityId = c.Id 
  Inner Join [NAICS.NaicsGroup] d on a.CodeGroup = d.NaicsGroup
  Inner join entity_cache ec on c.Id = ec.Id
  where a.CategoryId = 10 AND CodeGroup IS NOT NULL 
  and ec.EntityStateId = 3
  --and EntityTypeId = 1
  GROUP by a.CategoryId, c.EntityTypeId, CodeGroup, NaicsTitle

print 'updating all [ONET_SOC] ...'
INSERT INTO [dbo].[Counts.SiteTotals]
			([CategoryId]
           ,[EntityTypeId]
		   ,[CodeId]
           ,[Title]
           ,[Totals]) 
SELECT a.[CategoryId], c.EntityTypeId
	  ,[CodeGroup]  
      ,d.Description
      , COUNT (*) as totals
  FROM [dbo].[Reference.FrameworkItem] a 
  INNER JOIN [Entity.ReferenceFramework] b ON a.Id = b.ReferenceFrameworkItemId
  INNER JOIN Entity c ON b.EntityId = c.Id 
  Inner Join [ONET_SOC.JobFamily] d on a.CodeGroup = d.JobFamilyId
  Inner join entity_cache ec on c.Id = ec.Id
  where a.CategoryId = 11 AND CodeGroup IS NOT NULL 
  and ec.EntityStateId = 3
  GROUP by a.CategoryId, c.EntityTypeId, CodeGroup, d.[Description]

print 'updating all [CIP] ...'
INSERT INTO [dbo].[Counts.SiteTotals]
			([CategoryId]
           ,[EntityTypeId]
		   ,[CodeId]
           ,[Title]
           ,[Totals]) 
SELECT a.[CategoryId], c.EntityTypeId
	  ,[CodeGroup]  
      ,d.CIPTitle
      , COUNT (*) as totals
  FROM [dbo].[Reference.FrameworkItem] a 
  INNER JOIN [Entity.ReferenceFramework] b ON a.Id = b.ReferenceFrameworkItemId
  INNER JOIN Entity c ON b.EntityId = c.Id 
  Inner Join [CIPCode2010.JobFamily] d on a.CodeGroup = d.CIPFamily
  Inner join entity_cache ec on c.Id = ec.Id
  where a.CategoryId = 23 AND CodeGroup IS NOT NULL 
  and ec.EntityStateId = 3
  GROUP by a.CategoryId, c.EntityTypeId, CodeGroup, d.CIPTitle


-- ============ one offs ==========================================
/*
change to use [Counts.EntityStatistic]
*/

-- $$$$ credentials $$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
--	cost profiles
-- need to do two parts, so to not count multiple per entity
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue]
Inner join ( 
select count(*) as Totals 
from (
	SELECT e.EntityBaseId,count(*) AS Totals
	  FROM [dbo].[Entity.CostProfile] tags
	  inner join Entity e on tags.EntityId = e.Id 
	  --and e.EntityTypeId = 1
	  inner Join Credential c on e.EntityUid = c.RowId
	  where c.EntityStateId = 3
	  group by EntityBaseId
	  ) source
	) base on codes.SchemaName = 'credReport:HasCostProfile'
where codes.IsActive = 1

--	reference common cost MANIFESTS
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue]
Inner join ( 
select count(*) as Totals 
from (
	SELECT e.EntityBaseId,count(*) AS Totals
	  FROM [dbo].[Entity.CommonCost] tags
	  inner join Entity e on tags.EntityId = e.Id 
	  --and e.EntityTypeId = 1
	  inner Join Credential c on e.EntityUid = c.RowId
	  where c.EntityStateId = 3
	  group by EntityBaseId
	  ) source
    ) base on codes.SchemaName = 'credReport:ReferencesCommonCosts'
where codes.IsActive = 1


--	reference common condition manifests
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue]
Inner join ( 
select count(*) as Totals 
from (
	SELECT e.EntityBaseId,count(*) AS Totals
	  FROM [dbo].[Entity.CommonCondition] tags
	  inner join Entity e on tags.EntityId = e.Id 
	  --and e.EntityTypeId = 1
	  inner Join Credential c on e.EntityUid = c.RowId
	  where c.EntityStateId = 3
	  group by EntityBaseId
	  ) source
    ) base on codes.SchemaName = 'credReport:ReferencesCommonConditions'
where codes.IsActive = 1

--	reference financial
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT e.EntityBaseId,count(*) AS Totals
	  FROM [dbo].[Entity.FinancialAssistanceProfile] tags
	  inner join Entity e on tags.EntityId = e.Id 
	  --and e.EntityTypeId = 1
	  inner Join Credential c on e.EntityUid = c.RowId
	  where c.EntityStateId = 3
	  group by EntityBaseId
	  ) source
    ) base on codes.SchemaName = 'credReport:FinancialAid'
where codes.IsActive = 1

--	process profiles
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT e.EntityBaseId,count(*) AS Totals
	  FROM [dbo].[Entity.ProcessProfile] tags
	  inner join Entity e on tags.EntityId = e.Id 
	  --and e.EntityTypeId = 1
	  inner Join Credential c on e.EntityUid = c.RowId
	  where c.EntityStateId = 3
	  group by EntityBaseId
	  ) source
    ) base on codes.SchemaName = 'credReport:HasProcessProfile'
where codes.IsActive = 1

--
--	jurisdiction profiles
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT e.EntityBaseId,count(*) AS Totals
	  FROM [dbo].[Entity.JurisdictionProfile] tags
	  inner join Entity e on tags.EntityId = e.Id 
	  Inner Join Credential c on e.EntityUid = c.RowId
	  where tags.JProfilePurposeId =1 AND c.EntityStateId=3
	  group by EntityBaseId
	  ) source
    ) base on codes.SchemaName = 'credReport:HasJurisdictionProfile'
where codes.IsActive = 1
--

--
--	revocation profiles
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT e.EntityBaseId,count(*) AS Totals
	  FROM [dbo].[Entity.RevocationProfile] tags
	  inner join Entity e on tags.EntityId = e.Id 
	  --and e.EntityTypeId = 1
	  inner Join Credential c on e.EntityUid = c.RowId
	  where c.EntityStateId = 3
	  group by EntityBaseId
	  ) source
    ) base on codes.SchemaName = 'credReport:HasRevocation'
where codes.IsActive = 1

--	renwal profiles -------------------------------------
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT e.EntityBaseId,count(*) AS Totals
	  FROM [dbo].[Entity.ConditionProfile] tags
	  inner join Entity e on tags.EntityId = e.Id 
	  --and e.EntityTypeId = 1
	  inner Join Credential c on e.EntityUid = c.RowId
	  where c.EntityStateId = 3
	  and tags.ConnectionTypeId = 5 and isnull(ConditionSubTypeId,0) = 1
	  group by EntityBaseId
	  ) source
    ) base on codes.SchemaName = 'credReport:HasRenewal'
where codes.IsActive = 1
	
--	has occupations
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT e.EntityBaseId,count(*) AS Totals
	  FROM [dbo].[Entity.ReferenceFramework] tags
	  inner join Entity e on tags.EntityId = e.Id 
	  --and e.EntityTypeId = 1
	  inner Join Credential c on e.EntityUid = c.RowId
	  where c.EntityStateId = 3
	  And tags.CategoryId =  11
	  group by EntityBaseId
	  ) source
    ) base on codes.SchemaName = 'credReport:HasOccupations'
where codes.IsActive = 1

--	has industries
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT e.EntityBaseId,count(*) AS Totals
	  FROM [dbo].[Entity.ReferenceFramework] tags
	  inner join Entity e on tags.EntityId = e.Id 
	  --and e.EntityTypeId = 1
	  inner Join Credential c on e.EntityUid = c.RowId
	  where c.EntityStateId = 3
	  And tags.CategoryId =  10
	  group by EntityBaseId
	  ) source
    ) base on codes.SchemaName = 'credReport:HasIndustries'
where codes.IsActive = 1

-- Has CIP
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT e.EntityBaseId,count(*) AS Totals
	  FROM [dbo].[Entity.ReferenceFramework] tags
	  inner join Entity e on tags.EntityId = e.Id 
	  --and e.EntityTypeId = 1
	  inner Join Credential c on e.EntityUid = c.RowId
	  where c.EntityStateId = 3
	  And tags.CategoryId =  23
	  group by EntityBaseId
	  ) source
    ) base on codes.SchemaName = 'credReport:HasCIP'
where codes.IsActive = 1


--	has condition profile 
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT e.EntityBaseId,count(*) AS Totals
	  FROM [dbo].[Entity.ConditionProfile] tags
	  inner join Entity e on tags.EntityId = e.Id 
	  --and e.EntityTypeId = 1
	  inner Join Credential c on e.EntityUid = c.RowId
	  where c.EntityStateId = 3
	  group by EntityBaseId
	  ) source
    ) base on codes.SchemaName = 'credReport:HasConditionProfile'
where codes.IsActive = 1
	
--	By available online -----------------------------------------------
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
SELECT count(*) AS Totals
  FROM [dbo].Credential c
  where c.EntityStateId=3 and len(isnull(c.AvailableOnlineAt,'')) > 10
  --group by EntityType
    ) base on codes.SchemaName = 'credReport:AvailableOnline'
where codes.IsActive = 1
	

--	By requires competencies -----------------------------------------------
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT distinct a.[EntityTypeId], a.BaseId, Count(*) as nbr
	FROM [dbo].[Counts.ConditionProfileChildEntities] a
	inner join Credential c on a.BaseId = c.Id
	where c.EntityStateId=3 and  a.EntityTypeId = 1 and RequiredCompetencies> 0
	group by [EntityTypeId], baseId
	) source

) base on codes.SchemaName = 'credReport:RequiresCompetencies'
where codes.IsActive = 1

--	By has competencies -----------------------------------------------
-- any of requires, or asmt -> assess or lopp -> teaches
--=. DEPENDENT ON credential_SummaryCache being updated first 
--20-04-15 NEED TO DUMP THE CACHE
--UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
--   SET [Totals] = isnull(base.Totals,0)
--from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
--Inner join ( 
--select count(*) as Totals 
--from  [Credential.SummaryCache] a 
--where a.EntityStateId=3
--and (a.AssessmentsCompetenciesCount > 0 OR a.LearningOppsCompetenciesCount > 0 OR a.RequiresCompetenciesCount > 0 )

--) base on codes.SchemaName = 'credReport:HasCompetencies'
--where codes.IsActive = 1
--

UPDATE [dbo].[Counts.EntityStatistic]  
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes 
Inner join ( 
	-- ==
	select count(*) as Totals 
	--Select base.Id 	,IsNull(f.Nbr,0) As LearningOppsCompetenciesCount	,IsNull(g.Nbr,0) As AssessmentsCompetenciesCount	,IsNull(h.Nbr,0) As RequiresCompetenciesCount
	from credential base
	--competencies from learning opps
	left join (
		Select CredentialId, Count(*) As Nbr from [ConditionProfile_LearningOpp_Competencies_Summary] group by CredentialId
		) f							on base.Id = f.CredentialId

	--competencies from assessments
	left join (
		Select CredentialId, Count(*) As Nbr from ConditionProfile_Assessments_Competencies_Summary group by CredentialId
		) g							on base.Id = g.CredentialId
	--requires competencies from conditions
	left join (
		Select a.EntityBaseId As CredentialId, Count(*) As Nbr from Entity a		-- entity for credential
				Inner Join [Entity.ConditionProfile] b on a.Id = b.EntityId		-- condition profiles for credential
				Inner Join Entity cpEntity on b.RowId = cpEntity.EntityUid		-- entity for condition profile
				Inner Join [Entity.Competency] c on cpEntity.id = c.EntityId
				where b.ConnectionTypeId = 1
				group by a.EntityBaseId
		) h on base.Id = h.CredentialId

	where base.EntityStateId=3
	and (IsNull(f.Nbr,0) > 0 OR IsNull(g.Nbr,0) > 0 OR IsNull(h.Nbr,0) > 0 )
	-- ===
) base on codes.SchemaName = 'credReport:HasCompetencies'
where codes.IsActive = 1

-- ====================================================================

--	By requires credential -----------------------------------------------
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals from (
SELECT distinct a.[EntityTypeId], a.BaseId
      ,Count(*) as nbr
  FROM [dbo].[Counts.ConditionProfileChildEntities] a
  inner join Credential c on a.BaseId = c.Id
	where c.EntityStateId=3 And a.EntityTypeId = 1 and a.TargetCredentials> 0
	and a.ConnectionTypeId = 1
	group by a.[EntityTypeId], a.BaseId
	) source

) base on codes.SchemaName = 'credReport:RequiresCredential'
where codes.IsActive = 1

--	By recommends credential -----------------------------------------------
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals from (
SELECT distinct a.[EntityTypeId], a.BaseId
      ,Count(*) as nbr
  FROM [dbo].[Counts.ConditionProfileChildEntities] a
  inner join Credential c on a.BaseId = c.Id
	where c.EntityStateId=3 And a.EntityTypeId = 1 
	and a.TargetCredentials > 0
	and a.ConnectionTypeId = 2
	group by [EntityTypeId], baseId
	) source

) base on codes.SchemaName = 'credReport:RecommendsCredential'
where codes.IsActive = 1

--	By requires assessment -----------------------------------------------
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals from (
SELECT distinct a.[EntityTypeId], a.BaseId
      ,Count(*) as nbr
  FROM [dbo].[Counts.ConditionProfileChildEntities] a
  inner join Credential c on a.BaseId = c.Id
	where c.EntityStateId=3 And a.EntityTypeId = 1 
	and a.TargetAssessments> 0
	and a.ConnectionTypeId = 1
	group by a.[EntityTypeId], a.BaseId
	) source

) base on codes.SchemaName = 'credReport:RequiresAssessment'
where codes.IsActive = 1

--	By recommends assessment -----------------------------------------------
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals from (
SELECT distinct a.[EntityTypeId], a.BaseId
      ,Count(*) as nbr
  FROM [dbo].[Counts.ConditionProfileChildEntities] a
  inner join Credential c on a.BaseId = c.Id
	where c.EntityStateId=3 And a.EntityTypeId = 1 
	and a.TargetAssessments> 0
	and a.ConnectionTypeId = 2
	group by a.[EntityTypeId], a.BaseId
	) source

) base on codes.SchemaName = 'credReport:RecommendsAssessment'
where codes.IsActive = 1


--	By requires LearningOpportunitiess -----------------------------------------------
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals from (
SELECT distinct a.[EntityTypeId], a.BaseId
      ,Count(*) as nbr
  FROM [dbo].[Counts.ConditionProfileChildEntities] a
  inner join Credential c on a.BaseId = c.Id
	where c.EntityStateId=3 And a.EntityTypeId = 1 
	and a.TargetLearningOpportunities > 0
	and a.ConnectionTypeId = 1
	group by a.[EntityTypeId], a.BaseId
	) source

) base on codes.SchemaName = 'credReport:RequiresLearningOpportunity'
where codes.IsActive = 1

--	By recommends LearningOpportunities -----------------------------------------------
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals from (
SELECT distinct a.[EntityTypeId], a.BaseId
      ,Count(*) as nbr
  FROM [dbo].[Counts.ConditionProfileChildEntities] a
  inner join Credential c on a.BaseId = c.Id
	where c.EntityStateId=3 And a.EntityTypeId = 1 
	and a.TargetLearningOpportunities > 0
	and ConnectionTypeId = 2
	group by [EntityTypeId], baseId
	) source

) base on codes.SchemaName = 'credReport:RecommendsLearningOpportunity'
where codes.IsActive = 1

-- conditions
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals from (
	SELECT distinct [EntityTypeId], b.EntityBaseId
      ,Count(*) as nbr
	FROM [dbo].[Entity.ConditionProfile] a Inner Join Entity b on a.EntityId = b.id 
	inner join Credential c on b.EntityUid = c.RowId
	where c.EntityStateId = 3
	group by [EntityTypeId], b.EntityBaseId
	) source

) base on codes.SchemaName = 'credReport:HasConditionProfile'
where codes.IsActive = 1	

--	embedded credentials -----------------------------------------------
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals from (
	SELECT distinct [EntityTypeId], b.EntityBaseId
      ,Count(*) as nbr
	FROM [dbo].[Entity.Credential] a Inner Join Entity b on a.EntityId = b.id 
	where b.EntityTypeId = 1 
	group by [EntityTypeId], b.EntityBaseId
	) source

) base on codes.SchemaName = 'credReport:HasEmbeddedCredentials'
where codes.IsActive = 1	
-- =================== OUTCOME DATA	==========================================
-- Has AggregateData	======================
UPDATE [dbo].[Counts.EntityStatistic] 
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes 
Inner join ( 
	--select count(*) as Totals 
	--from (	)
		select count(*) as Totals from 
		(
			SELECT distinct [EntityTypeId], b.EntityBaseId
			  ,Count(*) as nbr
			FROM [dbo].[Entity.AggregateDataProfile] a 
			Inner Join Entity b on a.EntityId = b.id 
			inner join Credential c on b.EntityUid = c.RowId
			where c.EntityStateId = 3
			group by [EntityTypeId], b.EntityBaseId
		) source

) base on codes.SchemaName = 'credReport:HasAggregateDataProfile'
	where codes.IsActive = 1

-- Has HasDataSetProfile	======================
--CRED
UPDATE [dbo].[Counts.EntityStatistic] 
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes 
Inner join ( 
	--select count(*) as Totals 
	--from (	)
		select count(*) as Totals from 
		(
			SELECT distinct [EntityTypeId], b.EntityBaseId
			  ,Count(*) as nbr
			FROM [dbo].[Entity.Credential] a 
			Inner Join Entity b on a.EntityId = b.id 
			inner join Credential c on a.CredentialId = c.Id
			where b.EntityTypeId = 31 --where a DataSetProfile.Entity points to a credential
			AND c.EntityStateId = 3	--make sure still active
			group by [EntityTypeId], b.EntityBaseId
		) source

) base on codes.SchemaName = 'credReport:HasDataSetProfile'
	where codes.IsActive = 1
-- Has OutcomeData	======================
--want a distinct count of either ADP or DSP
--this has to count dataSetProfile and aggregateDataProfile
--or after the fact add the latter totals
UPDATE [dbo].[Counts.EntityStatistic] 
   SET [Totals] = isnull(base.CombinedTotals,0)
from [dbo].[Counts.EntityStatistic] codes 
Inner join ( 
		SELECT  SUM(totals) as CombinedTotals
		FROM [Counts.EntityStatistic]
		where [SchemaName] in ('credReport:HasAggregateDataProfile','credReport:HasDataSetProfile')

) base on codes.SchemaName = 'credReport:HasOutcomeData'
	where codes.IsActive = 1


-- --------------------
-- LOPP
--don't want to include EntityTypeId as there are 3 subclasses
--MIGHT be OK, as the group by then results in one total
UPDATE [dbo].[Counts.EntityStatistic] 
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes 
Inner join ( 
	--select count(*) as Totals 
	--from (	)
		select count(*) as Totals from 
		(
			SELECT distinct c.[EntityTypeId], b.EntityBaseId
			  ,Count(*) as nbr
			FROM [dbo].[Entity.AggregateDataProfile] a 
			Inner Join Entity b on a.EntityId = b.id 
			inner join LearningOpportunity c on b.EntityUid = c.RowId
			where c.EntityStateId = 3
			group by c.[EntityTypeId], b.EntityBaseId
		) source

) base on codes.SchemaName = 'loppReport:HasAggregateDataProfile'
	where codes.IsActive = 1


--LOPP
UPDATE [dbo].[Counts.EntityStatistic] 
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes 
Inner join ( 
	--select count(*) as Totals 
	--from (	)
		select count(*) as Totals from 
		(
			SELECT distinct c.[EntityTypeId], b.EntityBaseId
			  ,Count(*) as nbr
			FROM [dbo].[Entity.LearningOpportunity] a 
			Inner Join Entity b on a.EntityId = b.id 
			inner join LearningOpportunity c on a.LearningOpportunityId = c.Id
			where b.EntityTypeId = 31 --where a DataSetProfile.Entity points to an lopp
			AND c.EntityStateId = 3	--make sure still active
			group by c.[EntityTypeId], b.EntityBaseId
		) source

) base on codes.SchemaName = 'loppReport:HasDataSetProfile'
	where codes.IsActive = 1


-- Lopp
UPDATE [dbo].[Counts.EntityStatistic] 
   SET [Totals] = isnull(base.CombinedTotals,0)
from [dbo].[Counts.EntityStatistic] codes 
Inner join ( 
		SELECT  SUM(totals) as CombinedTotals
		FROM [Counts.EntityStatistic]
		where [SchemaName] in ('loppReport:HasAggregateDataProfile','loppReport:HasDataSetProfile')

) base on codes.SchemaName = 'loppReport:HasOutcomeData'
	where codes.IsActive = 1

-- Has HoldersProfiles	======================
UPDATE [dbo].[Counts.EntityStatistic] 
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes 
Inner join ( 
	--select count(*) as Totals 
	--from (	)
		select count(*) as Totals from 
		(
			SELECT distinct [EntityTypeId], b.EntityBaseId
			  ,Count(*) as nbr
			FROM [dbo].[Entity.HoldersProfile] a Inner Join Entity b on a.EntityId = b.id 
			inner join Credential c on b.EntityUid = c.RowId
			where c.EntityStateId = 3
			group by [EntityTypeId], b.EntityBaseId
		) source

) base on codes.SchemaName = 'credReport:HasHoldersProfile'
	where codes.IsActive = 1
		
-- Has EarningsProfiles	======================
UPDATE [dbo].[Counts.EntityStatistic] 
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes 
Inner join ( 
	select count(*) as Totals from 
	(
		SELECT distinct [EntityTypeId], b.EntityBaseId
		  ,Count(*) as nbr
		FROM [dbo].[Entity.EarningsProfile] a Inner Join Entity b on a.EntityId = b.id 
		inner join Credential c on b.EntityUid = c.RowId
		where c.EntityStateId = 3
		group by [EntityTypeId], b.EntityBaseId
	) source

) base on codes.SchemaName = 'credReport:HasEarningsProfile'
	where codes.IsActive = 1
		
-- Has EmploymentOutcomeProfiles	======================
UPDATE [dbo].[Counts.EntityStatistic] 
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes 
Inner join ( 
	select count(*) as Totals from 
	(
		SELECT distinct [EntityTypeId], b.EntityBaseId
		  ,Count(*) as nbr
		FROM [dbo].[Entity.EmploymentOutcomeProfile] a Inner Join Entity b on a.EntityId = b.id 
		inner join Credential c on b.EntityUid = c.RowId
		where c.EntityStateId = 3
		group by [EntityTypeId], b.EntityBaseId
	) source
) base on codes.SchemaName = 'credReport:HasEmploymentOutcomeProfile'
	where codes.IsActive = 1

--	IsPartOf credentials -----------------------------------------------
-- credential (part of) -> Entity.Credential ->  Entity - Credential (parent)
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals from (
	SELECT c.Id
      ,Count(*) as nbr
	FROM Credential c 
	Inner Join [dbo].[Entity.Credential] a on c.Id = a.CredentialId 
	Inner Join Entity b on a.EntityId = b.id 
	--Inner Join Credential d on b.EntityUid = d.RowId
	where c.EntityStateId > 2 and b.EntityTypeId = 1
	group by c.Id
	) source

) base on codes.SchemaName = 'credReport:IsPartOfCredential'
where codes.IsActive = 1	
			


--	By Has Verification Badge(s) -----------------------------------------------
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
--select codes.CategoryId, codes.EntityTypeId, base2.Totals
from [dbo].[Counts.EntityStatistic] codes 
Inner join ( 
	select count(*) as Totals from (
	--			
		select		 base.Id,
		isnull(badgeClaims.Total, 0) as badgeClaimsCount
		FROM [dbo].Credential base
			Inner Join (
				SELECT ec.CredentialId, count(*) as Total
				FROM [dbo].[Entity.VerificationProfile] a
				inner join entity vpEntity on a.RowId = vpEntity.EntityUid
				Inner join [Entity.Credential] ec on vpEntity.Id = ec.EntityId
				Inner join Credential c on  ec.CredentialId = c.Id
				inner join  [dbo].[Entity.Property] ep  on vpEntity.Id = ep.EntityId
				inner join [Codes.PropertyValue] b on ep.PropertyValueId = b.Id
				where c.EntityStateId = 3
				And	b.SchemaName = 'claimType:BadgeClaim'
				group by ec.CredentialId
		) badgeClaims on base.Id = badgeClaims.CredentialId
	--
	) source
) base on codes.SchemaName = 'credReport:HasVerificationBadges'
where codes.IsActive = 1

-- ====================================================================		
		
--	Duration profiles
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
	select count(*) as Totals 
	from (
		SELECT e.EntityBaseId,count(*) AS Totals
		  FROM [dbo].[Entity.DurationProfile] tags
		  inner join Entity e on tags.EntityId = e.Id and e.EntityTypeId = 1
		  Inner Join Credential c on e.EntityUid = c.RowId
		  where c.EntityStateId=3
		  group by EntityBaseId
		  ) source
	) base on codes.SchemaName = 'credReport:HasDurationProfile'
where codes.IsActive = 1
				

-- @@@@@@@  organization @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
--  partner orgs
-- reference orgs
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 

	SELECT count(*) AS Totals
		FROM [dbo].Organization tags
		where tags.EntityStateId = 2

) base on codes.SchemaName = 'orgReport:IsReferenceOrg'
	where codes.IsActive = 1

--
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 

	SELECT count(*) AS Totals
		FROM [dbo].Organization tags
		where tags.EntityStateId = 3

) base on codes.SchemaName = 'orgReport:IsRegisteredOrg'
	where codes.IsActive = 1

--===========================
-- Has credentials
UPDATE [dbo].[Counts.EntityStatistic] 
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes 
Inner join ( 
select count(*) as Totals 
from (
	SELECT distinct tags.AgentUid
		From Credential o 
		inner join Entity e on o.RowId = e.EntityUid
		Left Join [dbo].[Entity.AgentRelationship] tags on e.id = tags.EntityId
		where o.EntityStateId = 3 and tags.RelationshipTypeId in (6,7)
		
		) source
) base on codes.SchemaName = 'orgReport:HasCredentials'
	where codes.IsActive = 1
-- Has Assessments
UPDATE [dbo].[Counts.EntityStatistic] 
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes 
Inner join ( 
select count(*) as Totals 
from (
	SELECT distinct tags.AgentUid
		From Assessment o 
		inner join Entity e on o.RowId = e.EntityUid
		Left Join [dbo].[Entity.AgentRelationship] tags on e.id = tags.EntityId
		where o.EntityStateId = 3 and tags.RelationshipTypeId in (6,7)
		
		) source
) base on codes.SchemaName = 'orgReport:HasAssessments'
	where codes.IsActive = 1

-- Has LearningOpportunity
UPDATE [dbo].[Counts.EntityStatistic] 
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes 
Inner join ( 
select count(*) as Totals 
from (
	SELECT distinct tags.AgentUid
		From LearningOpportunity o 
		inner join Entity e on o.RowId = e.EntityUid
		Left Join [dbo].[Entity.AgentRelationship] tags on e.id = tags.EntityId
		where o.EntityStateId = 3 and tags.RelationshipTypeId in (6,7)
		
		) source
) base on codes.SchemaName = 'orgReport:HasLearningOpportunities'
	where codes.IsActive = 1

-- HasCompetencyFrameworks
UPDATE [dbo].[Counts.EntityStatistic] 
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes 
Inner join ( 
select count(*) as Totals 
from (
	SELECT distinct b.Id
		From CompetencyFramework o 
		inner join Organization b on o.OrganizationCTID = b.CTID
		where o.EntityStateId = 3 and o.EntityStateId = 3
		
		) source
) base on codes.SchemaName = 'orgReport:HasCompetencyFrameworks'
	where codes.IsActive = 1

-- Has Pathways
UPDATE [dbo].[Counts.EntityStatistic] 
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes 
Inner join ( 
select count(*) as Totals 
from (
	SELECT distinct tags.AgentUid
		From Pathway o 
		inner join Entity e on o.RowId = e.EntityUid
		Left Join [dbo].[Entity.AgentRelationship] tags on e.id = tags.EntityId
		where o.EntityStateId = 3 and tags.RelationshipTypeId in (6,7)
		
		) source
) base on codes.SchemaName = 'orgReport:HasPathways'
	where codes.IsActive = 1
--
--	process profiles
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT e.EntityBaseId,count(*) AS Totals
	  FROM [dbo].[Entity.ProcessProfile] tags
	  inner join Entity e on tags.EntityId = e.Id 
	  Inner Join Organization c on e.EntityUid = c.RowId
	  where c.EntityStateId=3
	  group by EntityBaseId
	  ) source
    ) base on codes.SchemaName = 'orgReport:HasProcessProfile'
where codes.IsActive = 1

--
--	jurisdiction profiles
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT e.EntityBaseId,count(*) AS Totals
	  FROM [dbo].[Entity.JurisdictionProfile] tags
	  inner join Entity e on tags.EntityId = e.Id 
	  Inner Join Organization c on e.EntityUid = c.RowId
	  where tags.JProfilePurposeId =1 AND c.EntityStateId=3
	  group by EntityBaseId
	  ) source
    ) base on codes.SchemaName = 'orgReport:HasJurisdictionProfile'
where codes.IsActive = 1

-- Has PathwaySets	======================
UPDATE [dbo].[Counts.EntityStatistic] 
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes 
Inner join ( 
select count(*) as Totals 
from (
	SELECT distinct tags.AgentUid
		From PathwaySet o 
		inner join Entity e on o.RowId = e.EntityUid
		Left Join [dbo].[Entity.AgentRelationship] tags on e.id = tags.EntityId
		where o.EntityStateId = 3 and tags.RelationshipTypeId in (6,7)
		
		) source
) base on codes.SchemaName = 'orgReport:HasPathwaySets'
	where codes.IsActive = 1

--=== PathwayComponent ===========================
UPDATE [dbo].[Codes.PathwayComponentType]
   SET [Totals] = pcCounts.Total
 from [Codes.PathwayComponentType] a 
 inner join ( SELECT [ComponentTypeId] ,count(*) as Total
  FROM [dbo].[PathwayComponent]
  where [EntityStateId] = 3
  group by ComponentTypeId
  ) pcCounts on a.id = pcCounts.ComponentTypeId


  
-- Has orgReport:HasOutcomeProfiles	======================
UPDATE [dbo].[Counts.EntityStatistic] 
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes 
Inner join ( 
select count(*) as Totals 
from (
	SELECT distinct o.DataProviderUID
		From DataSetProfileSummary o 
		inner join Entity e on o.RowId = e.EntityUid
		where o.EntityStateId = 3 AND o.InternalDSPEntityId is null 
		
		) source
) base on codes.SchemaName = 'orgReport:HasOutcomeProfiles'
	where codes.IsActive = 1

-- Has TransferValues	======================
UPDATE [dbo].[Counts.EntityStatistic] 
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes 
Inner join ( 
select count(*) as Totals 
from (
	SELECT distinct tags.AgentUid
		From TransferValueProfile o 
		inner join Entity e on o.RowId = e.EntityUid
		Left Join [dbo].[Entity.AgentRelationship] tags on e.id = tags.EntityId
		where o.EntityStateId = 3 and tags.RelationshipTypeId in (6,7)
		
		) source
) base on codes.SchemaName = 'orgReport:HasTransferValueProfiles'
	where codes.IsActive = 1


-- Cred Has TransferValues	======================
UPDATE [dbo].[Counts.EntityStatistic] 
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes 
Inner join ( 
select count(*) as Totals 
from (
	SELECT distinct tags.CredentialId
		From TransferValueProfile o 
		inner join Entity e on o.RowId = e.EntityUid
		Left Join [dbo].[Entity.Credential] tags on e.id = tags.EntityId
		where o.EntityStateId = 3  AND tags.CredentialId is not null
		
		) source
) base on codes.SchemaName = 'credReport:HasTransferValues'
	where codes.IsActive = 1

-- Lopp Has TransferValues	======================
UPDATE [dbo].[Counts.EntityStatistic] 
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes 
Inner join ( 
select count(*) as Totals 
from (
	SELECT distinct tags.LearningOpportunityId
		From TransferValueProfile o 
		inner join Entity e on o.RowId = e.EntityUid
		Left Join [dbo].[Entity.LearningOpportunity] tags on e.id = tags.EntityId
		where o.EntityStateId = 3 AND tags.LearningOpportunityId is not null
		
		) source
) base on codes.SchemaName = 'loppReport:HasTransferValues'
	where codes.IsActive = 1

-- Asmt Has TransferValues	======================
UPDATE [dbo].[Counts.EntityStatistic] 
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes 
Inner join ( 
select count(*) as Totals 
from (
	SELECT distinct tags.AssessmentId
		From TransferValueProfile o 
		inner join Entity e on o.RowId = e.EntityUid
		Left Join [dbo].[Entity.Assessment] tags on e.id = tags.EntityId
		where o.EntityStateId = 3  AND tags.AssessmentId is not null
		
		) source
) base on codes.SchemaName = 'asmtReport:HasTransferValues'
	where codes.IsActive = 1
--	manifests =============================================================
--seems wrong
--UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
--   SET [Totals] = isnull(base.Totals,0)
--from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
--Inner join ( 
--select count(*) as Totals 
--from (
--	SELECT distinct tags.EntityId, count(*) AS Totals
--		FROM [dbo].[Entity.CostManifest] tags
--		inner join Entity e on tags.EntityId = e.Id and e.EntityTypeId = 2
--		Inner Join Organization o on e.EntityUid = o.RowId
--		where Len(Isnull(o.ctid,'')) > 20
--		and o.EntityStateId =3
--		group by tags.EntityId
--		) source
--) base on codes.SchemaName = 'orgReport:HasCostManifest'
--	where codes.IsActive = 1
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT distinct tags.EntityId, count(*) AS Totals
		FROM [dbo].[Entity.CostManifest] tags
		inner join Entity e on tags.EntityId = e.Id and e.EntityTypeId = 2
		Inner Join Organization o on e.EntityUid = o.RowId
		where Len(Isnull(o.ctid,'')) > 20
		and o.EntityStateId =3
		group by tags.EntityId
		) source
) base on codes.SchemaName = 'orgReport:HasCostManifest'
	where codes.IsActive = 1

----------
UPDATE [dbo].[Counts.EntityStatistic]
   SET [Totals] = isnull(base.Totals,0)
from [Counts.EntityStatistic] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT distinct tags.EntityId, count(*) AS Totals
		FROM [dbo].[Entity.ConditionManifest] tags
		inner join Entity e on tags.EntityId = e.Id and e.EntityTypeId = 2
		Inner Join Organization o on e.EntityUid = o.RowId
		where o.EntityStateId = 3
		group by tags.EntityId
		) source
) base on codes.SchemaName = 'orgReport:HasConditionManifest'
	where codes.IsActive = 1

UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT distinct o.Id
		From Organization o 
		inner join Entity e on o.RowId = e.EntityUid
		Left Join [dbo].[Entity.CostManifest] tags on e.id = tags.EntityId
		where o.EntityStateId = 3
		and tags.Id is null
		) source
) base on codes.SchemaName = 'orgReport:HasNoCostManifests'
	where codes.IsActive = 1
-- ======================================================================================
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT distinct o.Id
		From Organization o 
		inner join Entity e on o.RowId = e.EntityUid
		Left Join [dbo].[Entity.ConditionManifest] tags on e.id = tags.EntityId
		where o.EntityStateId = 3
		and tags.Id is null
		) source
) base on codes.SchemaName = 'orgReport:HasNoConditionManifests'
	where codes.IsActive = 1


--	verification service =====================================================
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT distinct tags.EntityId, count(*) AS Totals
		FROM [dbo].[Entity.VerificationProfile] tags
		inner join Entity e on tags.EntityId = e.Id and e.EntityTypeId = 2
		Inner Join Organization o on e.EntityUid = o.RowId
		--inner join Entity_Cache e on tags.EntityId = e.Id and e.EntityTypeId = 2
		where o.EntityStateId = 3
		group by tags.EntityId
		) source
) base on codes.SchemaName = 'orgReport:HasVerificationService'
	where codes.IsActive = 1



-- NO	verification service
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT distinct o.Id
		From Organization o 
		inner join Entity e on o.RowId = e.EntityUid
		Left Join [dbo].[Entity.VerificationProfile] tags on e.id = tags.EntityId
		where o.EntityStateId = 3 and tags.Id is null
		
		) source
) base on codes.SchemaName = 'orgReport:HasNoVerificationService'
	where codes.IsActive = 1

--	Verification Claim 
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select source.PropertyValueId, count(*) as Totals 
from (
	SELECT orgentity.EntityBaseId, tags.PropertyValueId, count(*) AS Totals
	  FROM [dbo].[Entity.Property] tags
	  inner join Entity e on tags.EntityId = e.Id 
	  inner join [Entity.VerificationProfile] evp on e.EntityUid = evp.RowId
	  --
	  inner join [Entity] orgentity on evp.EntityId = orgentity.Id
	  inner join Organization c on orgentity.EntityUid = c.RowId
	  inner join [Codes.PropertyValue] cpv on tags.PropertyValueId = cpv.Id
	  where cpv.CategoryId = 41 
	  and c.EntityStateId = 3
	  group by orgentity.EntityBaseId, tags.PropertyValueId
	  ) source
	  group by source.PropertyValueId
) base on codes.Id = base.PropertyValueId

-- Has Departments
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT distinct o.Id
		From Organization o 
		inner join Entity e on o.RowId = e.EntityUid
		Left Join [dbo].[Entity.AgentRelationship] tags on e.id = tags.EntityId
		where o.EntityStateId = 3 and tags.RelationshipTypeId = 20
		
		) source
) base on codes.SchemaName = 'orgReport:HasDepartment'
	where codes.IsActive = 1


-- Has Subsidiary
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT distinct o.Id
		From Organization o 
		inner join Entity e on o.RowId = e.EntityUid
		Left Join [dbo].[Entity.AgentRelationship] tags on e.id = tags.EntityId
		where o.EntityStateId = 3 and tags.RelationshipTypeId = 21
		
		) source
) base on codes.SchemaName = 'orgReport:HasSubsidiary'
	where codes.IsActive = 1

--	has industries
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT distinct e.EntityBaseId,count(*) AS Totals
	  FROM [dbo].[Entity.ReferenceFramework] tags
	  inner join Entity e on tags.EntityId = e.Id and e.EntityTypeId = 2
	  Inner Join Organization o on e.EntityUid = o.RowId
	  where tags.CategoryId =  10 and o.EntityStateId=3
	  And o.EntityStateId =3
	  group by EntityBaseId
	  ) source
    ) base on codes.SchemaName = 'orgReport:HasIndustries'
where codes.IsActive = 1
	

-- AAAAAAAA  asmt AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
-- conditions
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals from (
	SELECT distinct [EntityTypeId], b.EntityBaseId
      ,Count(*) as nbr
	FROM [dbo].[Entity.ConditionProfile] a Inner Join Entity b on a.EntityId = b.id 
	inner join Assessment c on b.EntityUid = c.RowId
	where c.EntityStateId = 3
		group by [EntityTypeId], b.EntityBaseId
	) source

) base on codes.SchemaName = 'asmtReport:HasConditionProfile'
where codes.IsActive = 1	

--	cost profiles
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT e.EntityBaseId,count(*) AS Totals
	  FROM [dbo].[Entity.CostProfile] tags
	  inner join Entity e on tags.EntityId = e.Id 
	  inner join Assessment c on e.EntityUid = c.RowId
	where c.EntityStateId = 3
	  group by EntityBaseId
	  ) source
	) base on codes.SchemaName = 'asmtReport:HasCostProfile'
where codes.IsActive = 1

--	Duration profiles
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT e.EntityBaseId,count(*) AS Totals
	  FROM [dbo].[Entity.DurationProfile] tags
	  inner join Entity e on tags.EntityId = e.Id 
	  --and e.EntityTypeId = 3
	  Inner Join Assessment c on e.EntityUid = c.RowId
	  where c.EntityStateId=3
	  group by EntityBaseId
	  ) source
	) base on codes.SchemaName = 'asmtReport:HasDurationProfile'
where codes.IsActive = 1


--	reference common cost profiles
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT e.EntityBaseId,count(*) AS Totals
	  FROM [dbo].[Entity.CommonCost] tags
	  inner join Entity e on tags.EntityId = e.Id 
	  inner join Assessment c on e.EntityUid = c.RowId
		where c.EntityStateId = 3
	  group by EntityBaseId
	  ) source
    ) base on codes.SchemaName = 'asmtReport:ReferencesCommonCosts'
where codes.IsActive = 1


--	reference common condition manifests
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT e.EntityBaseId,count(*) AS Totals
	  FROM [dbo].[Entity.CommonCondition] tags
	  inner join Entity e on tags.EntityId = e.Id 
	  inner join Assessment c on e.EntityUid = c.RowId
		where c.EntityStateId = 3
	  group by EntityBaseId
	  ) source
    ) base on codes.SchemaName = 'asmtReport:ReferencesCommonConditions'
where codes.IsActive = 1

--	reference financial
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT e.EntityBaseId,count(*) AS Totals
	  FROM [dbo].[Entity.FinancialAssistanceProfile] tags
	  inner join Entity e on tags.EntityId = e.Id 
	  inner join Assessment c on e.EntityUid = c.RowId
		where c.EntityStateId = 3
	  group by EntityBaseId
	  ) source
    ) base on codes.SchemaName = 'asmtReport:FinancialAid'
where codes.IsActive = 1


--	process profiles
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT e.EntityBaseId,count(*) AS Totals
	  FROM [dbo].[Entity.ProcessProfile] tags
	  inner join Entity e on tags.EntityId = e.Id 
	  inner join Assessment c on e.EntityUid = c.RowId
		where c.EntityStateId = 3
	  group by EntityBaseId
	  ) source
    ) base on codes.SchemaName = 'asmtReport:HasProcessProfile'
where codes.IsActive = 1
--	jurisdiction profiles-asmt
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT e.EntityBaseId,count(*) AS Totals
	  FROM [dbo].[Entity.JurisdictionProfile] tags
	  inner join Entity e on tags.EntityId = e.Id 
	  Inner Join Assessment c on e.EntityUid = c.RowId
	  where tags.JProfilePurposeId =1 AND c.EntityStateId=3
	  group by EntityBaseId
	  ) source
    ) base on codes.SchemaName = 'asmtReport:HasJurisdictionProfile'
where codes.IsActive = 1
--

--	has occupations
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT e.EntityBaseId,count(*) AS Totals
	  FROM [dbo].[Entity.ReferenceFramework] tags
	  inner join Entity e on tags.EntityId = e.Id 
	  inner join Assessment c on e.EntityUid = c.RowId
	  where c.EntityStateId = 3
	  And tags.CategoryId =  11
	  group by EntityBaseId
	  ) source
    ) base on codes.SchemaName = 'asmtReport:HasOccupations'
where codes.IsActive = 1

--	has industries
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT e.EntityBaseId,count(*) AS Totals
	  FROM [dbo].[Entity.ReferenceFramework] tags
	  inner join Entity e on tags.EntityId = e.Id 
	  inner join Assessment c on e.EntityUid = c.RowId
	  where c.EntityStateId = 3
	  and tags.CategoryId =  10
	  group by EntityBaseId
	  ) source
    ) base on codes.SchemaName = 'asmtReport:HasIndustries'
where codes.IsActive = 1

-- Has CIP
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT e.EntityBaseId,count(*) AS Totals
	  FROM [dbo].[Entity.ReferenceFramework] tags
	  inner join Entity e on tags.EntityId = e.Id 
	  inner join Assessment c on e.EntityUid = c.RowId
		Where c.EntityStateId = 3
	    and tags.CategoryId =  23
	  group by EntityBaseId
	  ) source
    ) base on codes.SchemaName = 'asmtReport:HasCIP'
where codes.IsActive = 1

--	By available online -----------------------------------------------
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
SELECT count(*) AS Totals
  FROM [dbo].Assessment c
  where c.EntityStateId = 3
  and Len(Isnull(ctid,'')) > 20
	and len(isnull(c.AvailableOnlineAt,'')) > 10
  --group by EntityType
    ) base on codes.SchemaName = 'asmtReport:AvailableOnline'
where codes.IsActive = 1			


--	asmt By requires competencies -----------------------------------------------
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT distinct ParentEntityTypeId, a.ParentEntityId, Count(*) as nbr
	FROM [dbo].ConditionProfile_RequiredCompetencies a 
	Inner Join Assessment c on a.ParentEntityUid = c.RowId
	where c.EntityStateId =3
	group by ParentEntityTypeId, a.ParentEntityId
	) source

) base on codes.SchemaName = 'asmtReport:RequiresCompetencies'
where codes.IsActive = 1

--	By assesses competencies -----------------------------------------------
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT distinct [EntityTypeId], a.EntityId, Count(*) as nbr
	FROM [dbo].[Entity.Competency] a
	Inner Join [Entity] b on a.EntityId = b.Id 
	Inner Join Assessment c on b.EntityUid = c.RowId
	where c.EntityStateId =3
	group by [EntityTypeId], a.EntityId
	) source

) base on codes.SchemaName = 'asmtReport:AssessesCompetencies'
where codes.IsActive = 1

-- LLLLLLLLLLLLLLLLL lopp LLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLL
--	cost profiles
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT e.EntityBaseId,count(*) AS Totals
	  FROM [dbo].[Entity.CostProfile] tags
	  inner join Entity e on tags.EntityId = e.Id 
	  inner join LearningOpportunity c on e.EntityUid = c.RowId
	  where c.EntityStateId = 3
	  group by EntityBaseId
	  ) source
	) base on codes.SchemaName = 'loppReport:HasCostProfile'
where codes.IsActive = 1

UPDATE [dbo].[Counts.EntityStatistic]  -- 
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals from (
	SELECT distinct b.[EntityTypeId], b.EntityBaseId
      ,Count(*) as nbr
	FROM [dbo].[Entity.ConditionProfile] a Inner Join Entity b on a.EntityId = b.id 
	inner join LearningOpportunity c on b.EntityUid = c.RowId
	where c.EntityStateId = 3
	group by b.[EntityTypeId], b.EntityBaseId
	) source

) base on codes.SchemaName = 'loppReport:HasConditionProfile'
where codes.IsActive = 1	

--	Duration profiles
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT e.EntityBaseId,count(*) AS Totals
	  FROM [dbo].[Entity.DurationProfile] tags
	  inner join Entity e on tags.EntityId = e.Id 
	  Inner Join LearningOpportunity c on e.EntityUid = c.RowId
	  where c.EntityStateId=3
	  group by EntityBaseId
	  ) source
	) base on codes.SchemaName = 'loppReport:HasDurationProfile'
where codes.IsActive = 1


--	reference common cost profiles
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT e.EntityBaseId,count(*) AS Totals
	  FROM [dbo].[Entity.CommonCost] tags
	  inner join Entity e on tags.EntityId = e.Id 
	  Inner Join LearningOpportunity c on e.EntityUid = c.RowId
	  where c.EntityStateId=3
	  group by EntityBaseId
	  ) source
    ) base on codes.SchemaName = 'loppReport:ReferencesCommonCosts'
where codes.IsActive = 1

--	reference common condition manifests
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT e.EntityBaseId,count(*) AS Totals
	  FROM [dbo].[Entity.CommonCondition] tags
	  inner join Entity e on tags.EntityId = e.Id 
	  Inner Join LearningOpportunity c on e.EntityUid = c.RowId
	  where c.EntityStateId=3
	  group by EntityBaseId
	  ) source
    ) base on codes.SchemaName = 'loppReport:ReferencesCommonConditions'
where codes.IsActive = 1

--	reference financial
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT e.EntityBaseId,count(*) AS Totals
	  FROM [dbo].[Entity.FinancialAssistanceProfile] tags
	  inner join Entity e on tags.EntityId = e.Id 
	  Inner Join LearningOpportunity c on e.EntityUid = c.RowId
	  where c.EntityStateId=3
	  group by EntityBaseId
	  ) source
    ) base on codes.SchemaName = 'loppReport:FinancialAid'
where codes.IsActive = 1
--
--	jurisdiction profiles
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT e.EntityBaseId,count(*) AS Totals
	  FROM [dbo].[Entity.JurisdictionProfile] tags
	  inner join Entity e on tags.EntityId = e.Id 
	  Inner Join LearningOpportunity c on e.EntityUid = c.RowId
	  where tags.JProfilePurposeId =1 AND c.EntityStateId=3
	  group by EntityBaseId
	  ) source
    ) base on codes.SchemaName = 'loppReport:HasJurisdictionProfile'
where codes.IsActive = 1
--	has occupations
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT e.EntityBaseId,count(*) AS Totals
	  FROM [dbo].[Entity.ReferenceFramework] tags
	  inner join Entity e on tags.EntityId = e.Id 
	  Inner Join LearningOpportunity c on e.EntityUid = c.RowId
	  where c.EntityStateId=3
	  And tags.CategoryId =  11
	  group by EntityBaseId
	  ) source
    ) base on codes.SchemaName = 'loppReport:HasOccupations'
where codes.IsActive = 1

--	has industries
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT e.EntityBaseId,count(*) AS Totals
	  FROM [dbo].[Entity.ReferenceFramework] tags
	  inner join Entity e on tags.EntityId = e.Id 
	  Inner Join LearningOpportunity c on e.EntityUid = c.RowId
	  where c.EntityStateId=3
	  And tags.CategoryId =  10
	  group by EntityBaseId
	  ) source
    ) base on codes.SchemaName = 'loppReport:HasIndustries'
where codes.IsActive = 1
-- Has CIP
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT e.EntityBaseId,count(*) AS Totals
	  FROM [dbo].[Entity.ReferenceFramework] tags
	  inner join Entity e on tags.EntityId = e.Id 
	  Inner Join LearningOpportunity c on e.EntityUid = c.RowId
	  where c.EntityStateId=3
	  And tags.CategoryId =  23
	  group by EntityBaseId
	  ) source
    ) base on codes.SchemaName = 'loppReport:HasCIP'
where codes.IsActive = 1



--	By available online -----------------------------------------------
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
SELECT count(*) AS Totals
  FROM [dbo].LearningOpportunity c
  where c.EntityStateId=3
  and  Len(Isnull(CTID,'')) > 20
	and len(isnull(c.AvailableOnlineAt,'')) > 10
  --group by EntityType
    ) base on codes.SchemaName = 'loppReport:AvailableOnline'
where codes.IsActive = 1	

-- RequiresCompetencies
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT distinct ParentEntityTypeId, a.ParentEntityId, Count(*) as nbr
	FROM [dbo].ConditionProfile_RequiredCompetencies a 
	Inner Join LearningOpportunity c on a.ParentEntityUid = c.RowId
	where c.EntityStateId =3
	group by ParentEntityTypeId, a.ParentEntityId
	) source

) base on codes.SchemaName = 'loppReport:RequiresCompetencies'
where codes.IsActive = 1

--	By teaches competencies -----------------------------------------------
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT distinct b.[EntityTypeId], a.EntityId, Count(*) as nbr
	FROM [dbo].[Entity.Competency] a
	Inner Join [Entity] b on a.EntityId = b.Id 
	Inner Join LearningOpportunity c on b.EntityUid = c.RowId
	where c.EntityStateId =3
	group by b.[EntityTypeId], a.EntityId
	) source

) base on codes.SchemaName = 'loppReport:TeachesCompetencies'
where codes.IsActive = 1						

-- =====================================================================

UPDATE [dbo].[Counts.EntityStatistic]  
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes 
Inner join ( 
select count(*) as Totals 
from (
	SELECT caecFor.EntityId, COUNT(*) as total
	FROM dbo.[Entity.Credential] caecFor
	INNER JOIN dbo.Entity caecForEntity ON caecFor.EntityId = caecForEntity.Id 
	WHERE caecForEntity.EntityTypeId = 26
	AND caecFor.RelationshipTypeId = 1 
	group by caecFor.EntityId
	) source

) base on codes.SchemaName = 'tvpReport:HasTransferValueForCredentials'
where codes.IsActive = 1						


UPDATE [dbo].[Counts.EntityStatistic]  
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes 
Inner join ( 
select count(*) as Totals 
from (
	SELECT caecFor.EntityId, COUNT(*) as total
	FROM dbo.[Entity.Credential] caecFor
	INNER JOIN dbo.Entity caecForEntity ON caecFor.EntityId = caecForEntity.Id 
	WHERE caecForEntity.EntityTypeId = 26
	AND caecFor.RelationshipTypeId = 2 
	group by caecFor.EntityId
	) source

) base on codes.SchemaName = 'tvpReport:HasTransferValueFromCredentials'
where codes.IsActive = 1	

------ assessments

UPDATE [dbo].[Counts.EntityStatistic]  
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes 
Inner join ( 
select count(*) as Totals 
from (
	SELECT caecFor.EntityId, COUNT(*) as total
	FROM dbo.[Entity.Assessment] caecFor
	INNER JOIN dbo.Entity caecForEntity ON caecFor.EntityId = caecForEntity.Id 
	WHERE caecForEntity.EntityTypeId = 26
	AND caecFor.RelationshipTypeId = 1 
	group by caecFor.EntityId
	) source

) base on codes.SchemaName = 'tvpReport:HasTransferValueForAssessments'
where codes.IsActive = 1						
--
UPDATE [dbo].[Counts.EntityStatistic]  
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes 
Inner join ( 
select count(*) as Totals 
from (
	SELECT caecFor.EntityId, COUNT(*) as total
	FROM dbo.[Entity.Assessment] caecFor
	INNER JOIN dbo.Entity caecForEntity ON caecFor.EntityId = caecForEntity.Id 
	WHERE caecForEntity.EntityTypeId = 26
	AND caecFor.RelationshipTypeId = 2
	group by caecFor.EntityId
	) source

) base on codes.SchemaName = 'tvpReport:HasTransferValueFromAssessments'
where codes.IsActive = 1

------ lopps

UPDATE [dbo].[Counts.EntityStatistic]  
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes 
Inner join ( 
select count(*) as Totals 
from (
	SELECT caecFor.EntityId, COUNT(*) as total
	FROM dbo.[Entity.LearningOpportunity] caecFor
	INNER JOIN dbo.Entity caecForEntity ON caecFor.EntityId = caecForEntity.Id 
	WHERE caecForEntity.EntityTypeId = 26
	AND caecFor.RelationshipTypeId = 1 
	group by caecFor.EntityId
	) source

) base on codes.SchemaName = 'tvpReport:HasTransferValueForLopps'
where codes.IsActive = 1	
--
UPDATE [dbo].[Counts.EntityStatistic]  
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes 
Inner join ( 
select count(*) as Totals 
from (
	SELECT caecFor.EntityId, COUNT(*) as total
	FROM dbo.[Entity.LearningOpportunity] caecFor
	INNER JOIN dbo.Entity caecForEntity ON caecFor.EntityId = caecForEntity.Id 
	WHERE caecForEntity.EntityTypeId = 26
	AND caecFor.RelationshipTypeId = 2 
	group by caecFor.EntityId
	) source

) base on codes.SchemaName = 'tvpReport:HasTransferValueFromLopps'
where codes.IsActive = 1	
--
UPDATE [dbo].[Counts.EntityStatistic]  
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes 
Inner join ( 
select count(*) as Totals 
from (
	SELECT epp.EntityId, COUNT(*) as total
	FROM dbo.[Entity.ProcessProfile] epp
	INNER JOIN dbo.Entity e ON epp.EntityId = e.Id 
	WHERE e.EntityTypeId = 26
	AND epp.ProcessTypeId = 4
	group by epp.EntityId
	) source

) base on codes.SchemaName = 'tvpReport:TransferValueHasDevProcess'
where codes.IsActive = 1	
-- =====================================================================


-- expand regions as needed.
UPDATE [dbo].[Entity.Address]
   SET [Region] = b.state
-- select a.Region, b.StateCode, b.State
  FROM [dbo].[Entity.Address] a
  Left Join [dbo].[Codes.State] b on Rtrim(LTrim(a.Region)) = b.StateCode
  where b.StateCode is not null
  and a.region <> b.State

-- handle null countries for US
UPDATE [dbo].[Entity.Address]
   SET Country = 'United States'
-- select a.Country, a.Region, b.StateCode, b.State
  FROM [dbo].[Entity.Address] a
  Left Join [dbo].[Codes.State] b on a.Region = b.State
  where isnull(a.Country,'')= ''
  and b.StateCode is not null

-- ===================
-- using organization addresses rather than depending on addresses being included with the credential
INSERT INTO [dbo].[Counts.RegionTotals]
           ([EntityTypeId]
           ,[Country]
           ,[Region]
           ,[Totals])

Select 1, regions.Country, regions.Region, count(*) as cnt 
from (

	SELECT DISTINCT dbo.Credential.Id, dbo.Organization.Name, dbo.[Entity.Address].Country, dbo.[Entity.Address].Region
	FROM            dbo.[Entity.Address] 
	INNER JOIN dbo.Entity ON dbo.[Entity.Address].EntityId = dbo.Entity.Id 
	INNER JOIN dbo.Credential 
		INNER JOIN dbo.Organization 
			ON dbo.Credential.OwningAgentUid = dbo.Organization.RowId 
			ON dbo.Entity.EntityUid = dbo.Organization.RowId
	WHERE        (dbo.Credential.EntityStateId = 3)
--and dbo.[Entity.Address].Region = 'Ohio'
) regions
group by regions.Country,regions.Region
order by 1,2

go
grant execute on [CodeTables_UpdateTotals] to public
go
USE credFinder
GO

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


exec [CodeTables_UpdateTotals]

*/

/*
Modifications
16-09-02 mparsons - added totals for costTypeId, connection profile type
18-03-28 mparsons - added 
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
UPDATE [dbo].[Counts.EntityStatistic]
   SET [Totals] = 0
where EntityTypeId in (1,2,3,7)

-- site totals
-- will replace the reports portion of [Codes.PropertyValue]
truncate table [dbo].[Counts.SiteTotals]
	

UPDATE [dbo].[Codes.ConditionProfileType]
   SET [Totals] = 0
   , CredentialTotals = 0
   , AssessmentTotals = 0
   , LoppTotals = 0

UPDATE [dbo].[Codes.CredentialAgentRelationship]
   SET [Totals] = 0
   , CredentialTotals = 0
   , OrganizationTotals = 0
   , AssessmentTotals = 0
   , LoppTotals = 0

UPDATE [dbo].[Codes.EntityTypes]
   SET [Totals] = 0
where id <> 10

--UPDATE [dbo].NAICS
--   SET [Totals] = 0
--UPDATE [dbo].ONET_SOC
--   SET [Totals] = 0
--UPDATE [dbo].[ONET_SOC.JobFamily]
--   SET [Totals] = 0

--UPDATE [dbo].CIPCode2010
--   SET [Totals] = 0



-- ==========================================================
print 'updating all [Codes.PropertyValue] from [Entity.Property] ...'

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
and codes.CategoryId NOT in (14,18,21, 41) --???
--and codes.CategoryId <> 41
--and codes.CategoryId <> 14

-- =======================
-- use Codes.SiteTotals
-- audience type 14
-- delivery type 18,21

INSERT INTO [dbo].[Counts.SiteTotals]
           ([CategoryId]
           ,[EntityTypeId]
           ,[CodeId]
           ,[Title]
           ,[Totals])

SELECT codes.CategoryId, base.EntityTypeId, base.[PropertyValueId], codes.title, base.Totals
from [Codes.PropertyValue] codes 
Inner Join (
SELECT e.EntityTypeId, tags.[PropertyValueId], count(*) AS Totals
  FROM [dbo].[Entity.Property] tags
  inner join Entity_summary e on tags.EntityId = e.Id
  Inner Join [Codes.PropertyValue] codes on tags.PropertyValueId = codes.Id
  where codes.IsActive = 1 
	and codes.CategoryId in (4,14,18,21) --???
	and e.EntityTypeId in (1,3,7)
  group by e.EntityTypeId, tags.[PropertyValueId]
  --order by e.EntityTypeId, tags.[PropertyValueId]
) base On codes.id = base.PropertyValueId
order by codes.CategoryId, base.EntityTypeId, base.[PropertyValueId], codes.title 

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

SELECT distinct 65, e.EntityTypeId, 0, RTrim(LTrim(tags.[TextValue])), count(*) AS Totals
  FROM [dbo].[Entity.Reference] tags
  inner join Entity e on tags.EntityId = e.Id
  where tags.CategoryId = 65
  AND e.EntityTypeId in (1,3,7)
  group by e.EntityTypeId, tags.[TextValue]

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
			and e.EntityTypeId = 1 AND IsNull(tags.ConditionSubTypeId,1) = 2
			--where ConnectionTypeId = 7
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
			and e.EntityTypeId = 3 AND IsNull(tags.ConditionSubTypeId,1) = 3
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
			and e.EntityTypeId = 7 AND IsNull(tags.ConditionSubTypeId,1) = 4
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
	  group by tags.CredentialEntityId
	  ) ttls 
    ) base on codes.Id = 2293
where codes.IsActive = 1


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
print '[Codes.CredentialAgentRelationship] credential ...'
print 'TODO - replace with Counts.SiteTotals'
INSERT INTO [dbo].[Counts.SiteTotals]
			([CategoryId]
           ,[EntityTypeId]
		   ,[CodeId]
           ,[Title]
           ,[Totals]) 

SELECT 13, ttls.EntityTypeId, ttls.RelationshipTypeId, codes.Title, IsNull(count(*),0) AS Totals
FROM  (
	SELECT b.EntityTypeId, RelationshipTypeId, tags.EntityId
		,count(*) AS Totals
		  FROM [dbo].[Entity.AgentRelationship] tags
		  inner join Entity b on tags.EntityId = b.Id
		  --where b.EntityTypeId = 1
		  group by b.EntityTypeId, RelationshipTypeId, tags.EntityId
		  --order by RelationshipTypeId
) ttls 
Inner Join [Codes.CredentialAgentRelationship] codes on ttls.RelationshipTypeId = codes.Id
where codes.IsActive = 1
group by ttls.EntityTypeId, ttls.RelationshipTypeId, codes.Title

print 'NEW [Counts.SiteTotals] QAPerformed ...'
INSERT INTO [dbo].[Counts.SiteTotals]
			([CategoryId]
           ,[EntityTypeId]
		   ,[CodeId]
           ,[Title]
           ,[Totals]) 

SELECT 130, ttls.EntityTypeId, ttls.AssertionTypeId, codes.ReverseRelation, IsNull(count(*),0) AS Totals
FROM  (
	SELECT b.EntityTypeId, AssertionTypeId, tags.EntityId
		,count(*) AS Totals
		  FROM [dbo].[Entity.Assertion] tags
		  inner join Entity b on tags.EntityId = b.Id
		  where b.EntityTypeId = 2 
		  and AssertionTypeId in (1,2,10,12)
		  group by b.EntityTypeId, AssertionTypeId, tags.EntityId

) ttls 
Inner Join [Codes.CredentialAgentRelationship] codes on ttls.AssertionTypeId = codes.Id
where codes.IsActive = 1
group by ttls.EntityTypeId, ttls.AssertionTypeId, codes.ReverseRelation


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
		  where b.EntityTypeId = 1
		  group by RelationshipTypeId, tags.EntityId
		  --order by RelationshipTypeId
	  ) ttls 
	group by ttls.RelationshipTypeId
    ) base on codes.Id = base.RelationshipTypeId
where codes.IsActive = 1


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
		  where b.EntityTypeId = 2
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
		  where b.EntityTypeId = 3
		  group by RelationshipTypeId, tags.EntityId
		  --order by RelationshipTypeId
	  ) ttls 
	group by ttls.RelationshipTypeId
    ) base on codes.Id = base.RelationshipTypeId
where codes.IsActive = 1

print '[Codes.CredentialAgentRelationship] Assessment ...'
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
		  where b.EntityTypeId = 7
		  group by RelationshipTypeId, tags.EntityId
		  --order by RelationshipTypeId
	  ) ttls 
	group by ttls.RelationshipTypeId
    ) base on codes.Id = base.RelationshipTypeId
where codes.IsActive = 1


-- ===========================================================
print 'updating all [Codes.EntityTypes] ...'
--top level


UPDATE [dbo].[Codes.EntityTypes]
   SET [Totals] = isnull(base.Totals,0)
from [Codes.EntityTypes] codes
Inner join ( 
SELECT tags.EntityTypeId,count(*) AS Totals
  FROM [dbo].[Entity] tags
  inner join Entity_summary e on tags.Id = e.Id
	  and e.EntityTypeId in (1,2,3,7,19,20)
	  and e.EntityStateId > 2
	  and Len(isnull(e.ctid,'')) > 10
  group by tags.EntityTypeId
    ) base on codes.Id = base.EntityTypeId
where codes.IsActive = 1

-- =====
print 'updating all [Codes.EntityTypes] for indirect:[Entity.DurationProfile] ...'
declare @dpCount int
Select @dpCount = count(*) FROM [dbo].[Entity.DurationProfile] tags
  inner join Entity_summary e on tags.Id = e.Id

-- ????????????????????????????????????
UPDATE [dbo].[Codes.EntityTypes]
   SET [Totals] = isnull(@dpCount,0)
from [Codes.EntityTypes] codes
where codes.Id = 21

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
SELECT a.[CategoryId], EntityTypeId
	  ,[CodeGroup]  
      ,d.NaicsTitle
      , COUNT (*) as totals
  FROM [dbo].[Reference.Frameworks] a 
  INNER JOIN [Entity.ReferenceFramework] b ON a.Id = b.ReferenceFrameworkId
  INNER JOIN Entity c ON b.EntityId = c.Id 
  Inner Join [NAICS.NaicsGroup] d on a.CodeGroup = d.NaicsGroup
  where a.CategoryId = 10 AND CodeGroup IS NOT NULL 
  --and EntityTypeId = 1
  GROUP by a.CategoryId, EntityTypeId, CodeGroup, NaicsTitle

print 'updating all [ONET_SOC] ...'
INSERT INTO [dbo].[Counts.SiteTotals]
			([CategoryId]
           ,[EntityTypeId]
		   ,[CodeId]
           ,[Title]
           ,[Totals]) 
SELECT a.[CategoryId], EntityTypeId
	  ,[CodeGroup]  
      ,d.[Description]
      , COUNT (*) as totals
  FROM [dbo].[Reference.Frameworks] a 
  INNER JOIN [Entity.ReferenceFramework] b ON a.Id = b.ReferenceFrameworkId
  INNER JOIN Entity c ON b.EntityId = c.Id 
  Inner Join [ONET_SOC.JobFamily] d on a.CodeGroup = d.JobFamilyId
  where a.CategoryId = 11 AND CodeGroup IS NOT NULL 
  --and EntityTypeId = 1
  GROUP by a.CategoryId, EntityTypeId, CodeGroup, d.[Description]

print 'updating all [CIP] ...'
INSERT INTO [dbo].[Counts.SiteTotals]
			([CategoryId]
           ,[EntityTypeId]
		   ,[CodeId]
           ,[Title]
           ,[Totals]) 
SELECT a.[CategoryId], EntityTypeId
	  ,[CodeGroup]  
      ,d.CIPTitle
      , COUNT (*) as totals
  FROM dbo.[Reference.Frameworks] a 
  INNER JOIN [Entity.ReferenceFramework] b ON a.Id = b.ReferenceFrameworkId
  INNER JOIN Entity c ON b.EntityId = c.Id 
  Inner Join [CIPCode2010.JobFamily] d on a.CodeGroup = d.CIPFamily
  where a.CategoryId = 23 AND CodeGroup IS NOT NULL 
  --and EntityTypeId = 3
  GROUP by a.CategoryId, EntityTypeId, CodeGroup, d.CIPTitle


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
	  inner join Entity e on tags.EntityId = e.Id and e.EntityTypeId = 1
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
	  inner join Entity e on tags.EntityId = e.Id and e.EntityTypeId = 1
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
	  inner join Entity e on tags.EntityId = e.Id and e.EntityTypeId = 1
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
	  FROM [dbo].[Entity.FinancialAlignmentProfile] tags
	  inner join Entity e on tags.EntityId = e.Id and e.EntityTypeId = 1
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
	  inner join Entity e on tags.EntityId = e.Id and e.EntityTypeId = 1
	  group by EntityBaseId
	  ) source
    ) base on codes.SchemaName = 'credReport:HasProcessProfile'
where codes.IsActive = 1

	
--	revocation profiles
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from (
	SELECT e.EntityBaseId,count(*) AS Totals
	  FROM [dbo].[Entity.RevocationProfile] tags
	  inner join Entity e on tags.EntityId = e.Id and e.EntityTypeId = 1
	  group by EntityBaseId
	  ) source
    ) base on codes.SchemaName = 'credReport:HasRevocation'
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
	  inner join Entity e on tags.EntityId = e.Id and e.EntityTypeId = 1
	  where tags.CategoryId =  11
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
	  inner join Entity e on tags.EntityId = e.Id and e.EntityTypeId = 1
	  where tags.CategoryId =  10
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
	  inner join Entity e on tags.EntityId = e.Id and e.EntityTypeId = 1
	  where tags.CategoryId =  23
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
	  inner join Entity e on tags.EntityId = e.Id and e.EntityTypeId = 1
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
  FROM [dbo].Credential source
  where len(isnull(source.AvailableOnlineAt,'')) > 10
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
	SELECT distinct [EntityTypeId], baseId,Count(*) as nbr
	FROM [dbo].[Counts.ConditionProfileChildEntities]
	where EntityTypeId = 1 and RequiredCompetencies> 0
	group by [EntityTypeId], baseId
	) source

) base on codes.SchemaName = 'credReport:RequiresCompetencies'
where codes.IsActive = 1

--	By has competencies -----------------------------------------------
-- any of requires, or asmt -> assess or lopp -> teaches
--=. DEPENDENT ON credential_SummaryCache being updated first
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals 
from  [Credential.SummaryCache] a 
where a.AssessmentsCompetenciesCount > 0 OR a.LearningOppsCompetenciesCount > 0 OR a.RequiresCompetenciesCount > 0 

) base on codes.SchemaName = 'credReport:HasCompetencies'
where codes.IsActive = 1

--	By requires credential -----------------------------------------------
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals from (
SELECT distinct [EntityTypeId], baseId
      ,Count(*) as nbr
  FROM [dbo].[Counts.ConditionProfileChildEntities]
	where EntityTypeId = 1 and TargetCredentials> 0
	and ConnectionTypeId = 1
	group by [EntityTypeId], baseId
	) source

) base on codes.SchemaName = 'credReport:RequiresCredential'
where codes.IsActive = 1

--	By recommends credential -----------------------------------------------
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals from (
SELECT distinct [EntityTypeId], baseId
      ,Count(*) as nbr
  FROM [dbo].[Counts.ConditionProfileChildEntities]
	where EntityTypeId = 1 and TargetCredentials> 0
	and ConnectionTypeId = 2
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
SELECT [EntityTypeId], baseId
      ,Count(*) as nbr
  FROM [dbo].[Counts.ConditionProfileChildEntities]
	where EntityTypeId = 1 and TargetAssessments> 0
	and ConnectionTypeId = 1
	group by [EntityTypeId], baseId
	) source

) base on codes.SchemaName = 'credReport:RequiresAssessment'
where codes.IsActive = 1

--	By recommends assessment -----------------------------------------------
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals from (
SELECT [EntityTypeId], baseId
      ,Count(*) as nbr
  FROM [dbo].[Counts.ConditionProfileChildEntities]
	where EntityTypeId = 1 and TargetAssessments> 0
	and ConnectionTypeId = 2
	group by [EntityTypeId], baseId
	) source

) base on codes.SchemaName = 'credReport:RecommendsAssessment'
where codes.IsActive = 1


--	By requires LearningOpportunitiess -----------------------------------------------
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals from (
SELECT distinct [EntityTypeId], baseId
      ,Count(*) as nbr
  FROM [dbo].[Counts.ConditionProfileChildEntities]
	where EntityTypeId = 1 and TargetLearningOpportunities > 0
	and ConnectionTypeId = 1
	group by [EntityTypeId], baseId
	) source

) base on codes.SchemaName = 'credReport:RequiresLearningOpportunity'
where codes.IsActive = 1

--	By recommends LearningOpportunities -----------------------------------------------
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals from (
SELECT distinct [EntityTypeId], baseId
      ,Count(*) as nbr
  FROM [dbo].[Counts.ConditionProfileChildEntities]
	where EntityTypeId = 1 and TargetLearningOpportunities > 0
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
	where b.EntityTypeId = 1 
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
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
select count(*) as Totals from (
SELECT distinct CredentialId
      ,Count(*) as nbr
  FROM [dbo].[Credential.SummaryCache] where isnull(BadgeClaimsCount,0) > 0
	group by CredentialId
	) source

) base on codes.SchemaName = 'credReport:HasVerificationBadges'
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
	  inner join Entity e on tags.EntityId = e.Id and e.EntityTypeId = 1
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


--	manifests =============================================================
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
		group by tags.EntityId
		) source
) base on codes.SchemaName = 'orgReport:HasCostManifest'
	where codes.IsActive = 1

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
	  inner join [Entity] orgentity on evp.EntityId = orgentity.Id
	  inner join [Codes.PropertyValue] cpv on tags.PropertyValueId = cpv.Id
	  where cpv.CategoryId = 41
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
	  where tags.CategoryId =  10
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
	where b.EntityTypeId = 3
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
	  inner join Entity e on tags.EntityId = e.Id and e.EntityTypeId = 3
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
	  inner join Entity e on tags.EntityId = e.Id and e.EntityTypeId = 3
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
	  inner join Entity e on tags.EntityId = e.Id and e.EntityTypeId = 3
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
	  inner join Entity e on tags.EntityId = e.Id and e.EntityTypeId = 3
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
	  FROM [dbo].[Entity.FinancialAlignmentProfile] tags
	  inner join Entity e on tags.EntityId = e.Id and e.EntityTypeId = 3
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
	  inner join Entity e on tags.EntityId = e.Id and e.EntityTypeId = 3
	  group by EntityBaseId
	  ) source
    ) base on codes.SchemaName = 'asmtReport:HasProcessProfile'
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
	  inner join Entity e on tags.EntityId = e.Id and e.EntityTypeId = 3
	  where tags.CategoryId =  11
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
	  inner join Entity e on tags.EntityId = e.Id and e.EntityTypeId = 3
	  where tags.CategoryId =  10
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
	  inner join Entity e on tags.EntityId = e.Id and e.EntityTypeId = 3
	    where tags.CategoryId =  23
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
  FROM [dbo].Assessment source
  where Len(Isnull(ctid,'')) > 20
	and len(isnull(source.AvailableOnlineAt,'')) > 10
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
	  inner join Entity e on tags.EntityId = e.Id and e.EntityTypeId = 7
	  group by EntityBaseId
	  ) source
	) base on codes.SchemaName = 'loppReport:HasCostProfile'
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
	  inner join Entity e on tags.EntityId = e.Id and e.EntityTypeId = 7
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
	  inner join Entity e on tags.EntityId = e.Id and e.EntityTypeId = 7
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
	  inner join Entity e on tags.EntityId = e.Id and e.EntityTypeId = 7
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
	  FROM [dbo].[Entity.FinancialAlignmentProfile] tags
	  inner join Entity e on tags.EntityId = e.Id and e.EntityTypeId = 7
	  group by EntityBaseId
	  ) source
    ) base on codes.SchemaName = 'loppReport:FinancialAid'
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
	  inner join Entity e on tags.EntityId = e.Id and e.EntityTypeId = 7
	  where tags.CategoryId =  11
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
	  inner join Entity e on tags.EntityId = e.Id and e.EntityTypeId = 7
	  where tags.CategoryId =  10
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
	  inner join Entity e on tags.EntityId = e.Id and e.EntityTypeId = 7
	  where tags.CategoryId =  23
	  group by EntityBaseId
	  ) source
    ) base on codes.SchemaName = 'loppReport:HasCIP'
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
	  inner join Entity e on tags.EntityId = e.Id and e.EntityTypeId = 7
	  group by EntityBaseId
	  ) source
    ) base on codes.SchemaName = 'loppReport:HasProcessProfile'
where codes.IsActive = 1

--	By available online -----------------------------------------------
UPDATE [dbo].[Counts.EntityStatistic]  -- UPDATE [dbo].[Codes.PropertyValue]
   SET [Totals] = isnull(base.Totals,0)
from [dbo].[Counts.EntityStatistic] codes -- from [dbo].[Codes.PropertyValue] codes
Inner join ( 
SELECT count(*) AS Totals
  FROM [dbo].LearningOpportunity source
  where Len(Isnull(CTID,'')) > 20
	and len(isnull(source.AvailableOnlineAt,'')) > 10
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
	SELECT distinct [EntityTypeId], a.EntityId, Count(*) as nbr
	FROM [dbo].[Entity.Competency] a
	Inner Join [Entity] b on a.EntityId = b.Id 
	Inner Join LearningOpportunity c on b.EntityUid = c.RowId
	group by [EntityTypeId], a.EntityId
	) source

) base on codes.SchemaName = 'loppReport:TeachesCompetencies'
where codes.IsActive = 1						

-- =====================================================================
truncate table [Counts.RegionTotals] 
-- expand regions as needed.
UPDATE credFinder.[dbo].[Entity.Address]
   SET [Region] = b.state
-- select a.Region, b.StateCode, b.State
  FROM credFinder.[dbo].[Entity.Address] a
  Left Join credFinder.[dbo].[Codes.State] b on Rtrim(LTrim(a.Region)) = b.StateCode
  where b.StateCode is not null
  and a.region <> b.State

-- handle null countries for US
UPDATE credFinder.[dbo].[Entity.Address]
   SET Country = 'United States'
-- select a.Country, a.Region, b.StateCode, b.State
  FROM credFinder.[dbo].[Entity.Address] a
  Left Join credFinder.[dbo].[Codes.State] b on a.Region = b.State
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

) regions
group by regions.Country,regions.Region
order by 1,2

go
grant execute on [CodeTables_UpdateTotals] to public
go
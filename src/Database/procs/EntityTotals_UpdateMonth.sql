use credFinder
go
--USE staging_credFinder
--GO

/*
handle historic data
See: Populate historical monthly entity history.sql

--add new entities

INSERT INTO [dbo].[Counts.EntityMonthlyTotals]
           ([Period]
           ,[EntityTypeId]
           ,[CreatedTotal]
           ,[UpdatedTotal], [DeletedTotal])

SELECT  convert(varchar(8),Created,120) + '01', 26, count(*) as [CreatedTotal],0 [UpdatedTotal],0 [DeletedTotal]
  FROM [dbo].TransferValueProfile 
  where [EntityStateId] = 3
 -- and created < '2020-03-01'
  group by convert(varchar(8),Created,120)
 -- AND convert(varchar(8),Created,120) + '01' = @RangeStart

 =======================================================================================

SELECT [Id]
      ,[Period]
      ,[EntityTypeId]
      ,[CreatedTotal]
      ,[UpdatedTotal]
	  ,DeletedTotal
  FROM [dbo].[Counts.EntityMonthlyTotals]
order by 2,3

exec EntityMonthlyTotals_Update

*/

/*
EntityMonthlyTotals_Update - update entity totals for current period

NOTE:
Need to ensure pending entities are handled properly. 
Also have to consider impact of mass re-importing data - where potentially the data didn't change. 
This should be OK, as the update methods first check for changes to the record. 
*/
Alter  Procedure [dbo].[EntityMonthlyTotals_Update]
    @debugLevel int = 0

AS
declare @RangeStart datetime, @RangeEnd datetime, @EntityTypeId int
declare @UpdateCount int
--set @RangeStart = '2018-08-01'
set @RangeStart = convert(varchar(8),getdate(),120) + '01'
--set @RangeEnd = DATEADD(Month, 1, @rangeStart)
--select @RangeStart, @RangeEnd
print 'updates for period ' + convert(varchar(8),getdate(),120) + '01'
--select getdate()
--don't want to delete always. First check if exists
--DELETE FROM [dbo].[Counts.EntityMonthlyTotals]
--      WHERE convert(varchar(10),Period,120) = @RangeStart

IF EXISTS (SELECT * FROM [Counts.EntityMonthlyTotals] WHERE convert(varchar(10),Period,120) = @RangeStart)
BEGIN
	print 'Records found for period, resetting ...'
    UPDATE [dbo].[Counts.EntityMonthlyTotals]
	   SET [UpdatedTotal] = 0, [CreatedTotal] = 0, DeletedTotal = 0
	--	Select a.Period, a.EntityTypeId, updates.UpdatedTotal
	 From [Counts.EntityMonthlyTotals] a
	 WHERE convert(varchar(10),Period,120) = @RangeStart
	END
ELSE BEGIN
   --***may need to create template rows for the month, and do updates
   print 'NO records found for period, creating ...'
	INSERT INTO [dbo].[Counts.EntityMonthlyTotals]
			   ([Period] ,[EntityTypeId] ,[CreatedTotal] ,[UpdatedTotal], DeletedTotal)
		 VALUES (@RangeStart,1,0,0,0)
	INSERT INTO [dbo].[Counts.EntityMonthlyTotals]
			   ([Period] ,[EntityTypeId] ,[CreatedTotal] ,[UpdatedTotal], DeletedTotal)
		 VALUES (@RangeStart,2,0,0,0)

	INSERT INTO [dbo].[Counts.EntityMonthlyTotals]
			   ([Period] ,[EntityTypeId] ,[CreatedTotal] ,[UpdatedTotal], DeletedTotal)
		 VALUES (@RangeStart,3,0,0,0)
	INSERT INTO [dbo].[Counts.EntityMonthlyTotals]
			   ([Period] ,[EntityTypeId] ,[CreatedTotal] ,[UpdatedTotal], DeletedTotal)
		 VALUES (@RangeStart,7,0,0,0)

	INSERT INTO [dbo].[Counts.EntityMonthlyTotals]
			   ([Period] ,[EntityTypeId] ,[CreatedTotal] ,[UpdatedTotal], DeletedTotal)
		 VALUES (@RangeStart,8,0,0,0)
	INSERT INTO [dbo].[Counts.EntityMonthlyTotals]
			   ([Period] ,[EntityTypeId] ,[CreatedTotal] ,[UpdatedTotal], DeletedTotal)
		 VALUES (@RangeStart,10,0,0,0)
	INSERT INTO [dbo].[Counts.EntityMonthlyTotals]
		([Period] ,[EntityTypeId] ,[CreatedTotal] ,[UpdatedTotal], DeletedTotal)
		VALUES (@RangeStart,26,0,0,0)
	END
-- === Created ===========================================
-- == the activity log approach is not dependable as sometimes the org gets added as pending
--UPDATE [dbo].[Counts.EntityMonthlyTotals]
--   SET [CreatedTotal] = adds.AddsTotal
----	Select a.Period, a.EntityTypeId, updates.UpdatedTotal
-- From [Counts.EntityMonthlyTotals] a
-- Inner Join (
-- -- OK to include where created and updated in same period 
-- SELECT 
--convert(varchar(8),CreatedDate,120) + '01' as Period
--     -- ,[ActivityType]
--	  ,b.Id as EntityTypeId
--      ,count(*) as AddsTotal

--  FROM [dbo].[Activity_Summary] a
--  Inner Join [Codes.EntityTypes] b on a.ActivityType = b.Title
--  where [Activity] = 'Import'
--  and [Event] = 'Add'
--  and b.Id in (1,2,3,7)
--group by 
--convert(varchar(8),CreatedDate,120) + '01'
--,b.Id
----order by 1,2
--) adds on a.EntityTypeId = adds.EntityTypeId
--		AND a.Period = adds.Period

UPDATE [dbo].[Counts.EntityMonthlyTotals]
   SET [CreatedTotal] = adds.Totals
--	Select a.Period, a.EntityTypeId, updates.UpdatedTotal
 From [Counts.EntityMonthlyTotals] a
 Inner Join (
SELECT  count(*) as totals
  FROM [dbo].[Credential] 
  where [EntityStateId] = 3
  AND convert(varchar(8),Created,120) + '01' = @RangeStart
  ) adds on a.Period = @RangeStart and a.EntityTypeId = 1


UPDATE [dbo].[Counts.EntityMonthlyTotals]
   SET [CreatedTotal] = adds.Totals
--	Select a.Period, a.EntityTypeId, updates.UpdatedTotal
 From [Counts.EntityMonthlyTotals] a
 Inner Join (
SELECT count(*) as totals
  FROM [dbo].Organization 
  where [EntityStateId] = 3
  AND convert(varchar(8),Created,120) + '01' = @RangeStart
  ) adds on a.Period = @RangeStart and a.EntityTypeId = 2

UPDATE [dbo].[Counts.EntityMonthlyTotals]
   SET [CreatedTotal] = adds.Totals
--	Select a.Period, a.EntityTypeId, updates.UpdatedTotal
 From [Counts.EntityMonthlyTotals] a
 Inner Join (
SELECT count(*) as totals
  FROM [dbo].Assessment 
  where [EntityStateId] = 3
  AND convert(varchar(8),Created,120) + '01' = @RangeStart
  ) adds on a.Period = @RangeStart and a.EntityTypeId = 3
-- ============== LOPP ====================================
UPDATE [dbo].[Counts.EntityMonthlyTotals]
   SET [CreatedTotal] = adds.Totals
--	Select a.Period, a.EntityTypeId, updates.UpdatedTotal
 From [Counts.EntityMonthlyTotals] a
 Inner Join (
SELECT count(*) as totals
  FROM [dbo].LearningOpportunity 
  where [EntityStateId] = 3
  AND convert(varchar(8),Created,120) + '01' = @RangeStart
  ) adds on a.Period = @RangeStart and a.EntityTypeId = 7

-- ============== Pathway ====================================
UPDATE [dbo].[Counts.EntityMonthlyTotals]
   SET [CreatedTotal] = adds.Totals
--	Select a.Period, a.EntityTypeId, updates.UpdatedTotal
 From [Counts.EntityMonthlyTotals] a
 Inner Join (
SELECT count(*) as totals
  FROM [dbo].Pathway 
  where [EntityStateId] = 3
  AND convert(varchar(8),Created,120) + '01' = @RangeStart
  ) adds on a.Period = @RangeStart and a.EntityTypeId = 7

-- ============== frameworks ====================================
UPDATE [dbo].[Counts.EntityMonthlyTotals]
   SET [CreatedTotal] = adds.Totals
--	Select a.Period, a.EntityTypeId, updates.UpdatedTotal
 From [Counts.EntityMonthlyTotals] a
 Inner Join (
SELECT count(*) as totals
  FROM [dbo].CompetencyFramework 
  where [EntityStateId] = 3
  AND convert(varchar(8),Created,120) + '01' = @RangeStart
  ) adds on a.Period = @RangeStart and a.EntityTypeId = 7


-- ============== transfer values ====================================
UPDATE [dbo].[Counts.EntityMonthlyTotals]
   SET [CreatedTotal] = adds.Totals
--	Select a.Period, a.EntityTypeId, updates.UpdatedTotal
 From [Counts.EntityMonthlyTotals] a
 Inner Join (
SELECT count(*) as totals
  FROM [dbo].TransferValueProfile 
  where [EntityStateId] = 3
  AND convert(varchar(8),Created,120) + '01' = @RangeStart
  ) adds on a.Period = @RangeStart and a.EntityTypeId = 7

-- === Updates ===========================================

--declare @RangeStart2 datetime
--set @RangeStart2 = '2019-02-01'

UPDATE [dbo].[Counts.EntityMonthlyTotals]
   SET [UpdatedTotal] = updates.UpdatedTotal
--	Select a.Period, a.EntityTypeId, updates.UpdatedTotal
 From [Counts.EntityMonthlyTotals] a
 Inner Join (
 -- OK to include where created and updated in same period 
 SELECT 
convert(varchar(8),CreatedDate,120) + '01' as Period
     -- ,[ActivityType]
	  ,b.Id as EntityTypeId
      ,count(*) as UpdatedTotal

  FROM [dbo].[Activity_Summary] a
  Inner Join [Codes.EntityTypes] b on a.ActivityType = b.Title
  where [Activity] = 'Import'
  and [Event] = 'Update'
  --and b.Id in (1,2,3,7,8,10,11,19,20,23,26,27,31)
  and b.Id in (1,2,3,7,8,10,26)
  AND convert(varchar(8),a.CreatedDate,120) + '01' = @RangeStart
group by 
convert(varchar(8),CreatedDate,120) + '01'
,b.Id
--order by 1,2
) updates on a.EntityTypeId = updates.EntityTypeId
		AND a.Period = updates.Period


-- === Deletes ===========================================
--declare @RangeStart2 datetime
--set @RangeStart2 = '2019-02-01'

UPDATE [dbo].[Counts.EntityMonthlyTotals]
   SET DeletedTotal = updates.DeletedTotal
--	Select a.Period, a.EntityTypeId, updates.UpdatedTotal
 From [Counts.EntityMonthlyTotals] a
 Inner Join (
 -- OK to include where created and updated in same period 
 SELECT 
convert(varchar(8),CreatedDate,120) + '01' as Period
     -- ,[ActivityType]
	  ,b.Id as EntityTypeId
      ,count(*) as DeletedTotal

  FROM [dbo].[Activity_Summary] a
  Inner Join [Codes.EntityTypes] b on a.ActivityType = b.Title
  where [Activity] = 'Import'
  and [Event] = 'Delete'
    --and b.Id in (1,2,3,7,8,10,11,19,20,23,26,27,31)
  and b.Id in (1,2,3,7,8,10,26)
  AND convert(varchar(8),a.CreatedDate,120) + '01' = @RangeStart
group by 
convert(varchar(8),CreatedDate,120) + '01'
,b.Id
--order by 1,2
) updates on a.EntityTypeId = updates.EntityTypeId
		AND a.Period = updates.Period
-- ?????????????
-- ============= credentials  =============================
--set @EntityTypeId= 1
--INSERT INTO [dbo].[Counts.EntityMonthlyTotals]
--           ([Period]
--           ,[EntityTypeId]
--           ,[CreatedTotal]
--           ,[UpdatedTotal])

----could just use activity log???
--SELECT @RangeStart, @EntityTypeId, count(*) as totals, 0
--  FROM [dbo].[Credential] 
--  where [EntityStateId] = 3
--  --and Convert(varchar(10),Created, 120) >= @RangeStart
--  --and Convert(varchar(10),Created, 120) < @RangeEnd
--  --OR
--  AND convert(varchar(8),Created,120) + '01' = @RangeStart

----**this doesn't handle multiple updates for the same entity
--Select @UpdateCount = count(*) FROM [dbo].[Credential] 
--  where [EntityStateId] = 3
--  and created < @RangeStart
--  and Convert(varchar(10),LastUpdated, 120) >= @RangeStart
--  and Convert(varchar(10),LastUpdated, 120) < @RangeEnd

--UPDATE [dbo].[Counts.EntityMonthlyTotals] 
--   SET [UpdatedTotal] = @UpdateCount
--from [dbo].[Counts.EntityMonthlyTotals]  a
--WHERE [Period] = @RangeStart and [EntityTypeId]= @EntityTypeId


---- ============= organization  =============================
--set @EntityTypeId= 2
--INSERT INTO [dbo].[Counts.EntityMonthlyTotals]
--           ([Period]
--           ,[EntityTypeId]
--           ,[CreatedTotal]
--           ,[UpdatedTotal])

--SELECT @RangeStart, @EntityTypeId, count(*) as totals, 0
--  FROM [dbo].Organization 
--  where [EntityStateId] = 3
--  and Convert(varchar(10),Created, 120) >= @RangeStart
--  and Convert(varchar(10),Created, 120) < @RangeEnd

--Select @UpdateCount = count(*) FROM [dbo].Organization 
--  where [EntityStateId] = 3
--  and created < @RangeStart
--  and Convert(varchar(10),LastUpdated, 120) >= @RangeStart
--  and Convert(varchar(10),LastUpdated, 120) < @RangeEnd

--UPDATE [dbo].[Counts.EntityMonthlyTotals] 
--   SET [UpdatedTotal] = @UpdateCount
--from [dbo].[Counts.EntityMonthlyTotals]  a
--WHERE [Period] = @RangeStart and [EntityTypeId]= @EntityTypeId


---- ============= assessment  =============================
--set @EntityTypeId= 3
--INSERT INTO [dbo].[Counts.EntityMonthlyTotals]
--           ([Period]
--           ,[EntityTypeId]
--           ,[CreatedTotal]
--           ,[UpdatedTotal])
--
--SELECT @RangeStart, @EntityTypeId, count(*) as totals, 0
--  FROM [dbo].Assessment 
--  where [EntityStateId] = 3
--  and Convert(varchar(10),Created, 120) >= @RangeStart
--  and Convert(varchar(10),Created, 120) < @RangeEnd

--Select @UpdateCount = count(*) FROM [dbo].Assessment 
--  where [EntityStateId] = 3
--  and created < @RangeStart
--  and Convert(varchar(10),LastUpdated, 120) >= @RangeStart
--  and Convert(varchar(10),LastUpdated, 120) < @RangeEnd

--UPDATE [dbo].[Counts.EntityMonthlyTotals] 
--   SET [UpdatedTotal] = @UpdateCount
--from [dbo].[Counts.EntityMonthlyTotals]  a
--WHERE [Period] = @RangeStart and [EntityTypeId]= @EntityTypeId

---- ============= lopp  =============================
--set @EntityTypeId= 7
--INSERT INTO [dbo].[Counts.EntityMonthlyTotals]
--           ([Period]
--           ,[EntityTypeId]
--           ,[CreatedTotal]
--           ,[UpdatedTotal])

--SELECT @RangeStart, @EntityTypeId, count(*) as totals, 0
--  FROM [dbo].LearningOpportunity 
--  where [EntityStateId] = 3
--  and Convert(varchar(10),Created, 120) >= @RangeStart
--  and Convert(varchar(10),Created, 120) < @RangeEnd

--Select @UpdateCount = count(*) FROM [dbo].LearningOpportunity 
--  where [EntityStateId] = 3
--  and created < @RangeStart
--  and Convert(varchar(10),LastUpdated, 120) >= @RangeStart
--  and Convert(varchar(10),LastUpdated, 120) < @RangeEnd

--UPDATE [dbo].[Counts.EntityMonthlyTotals] 
--   SET [UpdatedTotal] = @UpdateCount
--from [dbo].[Counts.EntityMonthlyTotals]  a
--WHERE [Period] = @RangeStart and [EntityTypeId]= @EntityTypeId


--TODO - will want to do something for competency frameworks.
GO
grant execute on [EntityMonthlyTotals_Update] to public
go


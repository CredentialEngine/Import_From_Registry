USE [credFinder]
GO
--drop  Procedure [dbo].[PopulateMonthlySiteTotals]
--go


/*
select * 
from [Counts.MonthlySiteTotals]


Roll over the site totals FOR PROPERTIES for the current month to [Counts.MonthlySiteTotals]

NOT CURRENTLY IN USE, AND MAY NOT BE

*/
Alter  Procedure [dbo].[PopulateSiteMonthlyTotals]
    @debugLevel int = 0

AS
Declare @period datetime
select convert(varchar(10), GETDATE(), 120)
set @period = convert(varchar(10), GETDATE(), 120)
select @period
Begin
-- individual codes are rolled over based on when related parent was new or updated
--INSERT INTO [dbo].[Counts.MonthlySiteTotals]
--           ([Period]
--           ,[CategoryId]
--           ,[EntityTypeId]
--           ,[CodeId]
--           ,[Title]
--           ,[Totals])

--SELECT @period
--      ,[CategoryId]
--      ,[EntityTypeId]
--      ,[CodeId]
--      ,[Title]
--      ,[Totals]
--  FROM [dbo].[Counts.SiteTotals]

  -- entity specific
  INSERT INTO [dbo].[Counts.MonthlySiteTotals]
           ([Period]
           ,[CategoryId]
           ,[EntityTypeId]
           ,[CodeId]
           ,[Title]
           ,[Totals])

SELECT 
--@period, 
codes.CategoryId, e.EntityTypeId, 
 [PropertyValueId], codes.Title, count(*) AS Totals
  FROM [dbo].[Entity.Property] tags
  Inner Join [Codes.PropertyValue] codes on tags.PropertyValueId = codes.Id
  inner join Entity_summary e on tags.EntityId = e.Id

  
where codes.IsActive = 1 
and e.EntityTypeId in (1,2,3,7)
and codes.CategoryId <> 41
and codes.CategoryId <> 14
  group by codes.CategoryId,e.EntityTypeId, [PropertyValueId], codes.Title
  order by codes.CategoryId,e.EntityTypeId, [PropertyValueId], codes.Title



end
go




USE credFinder
GO

--use credFinder_prod
--go


--use sandbox_credFinder
--go

--use staging_credFinder
--go


/****** Object:  StoredProcedure [dbo].[Activity_Search]    Script Date: 2/1/2018 1:41:34 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


/*
USE [CTI]
GO

SELECT [Id]
      ,[UserName]
      ,[FirstName]
      ,[LastName]
      ,[FullName]
      ,[SortName]
      ,[Email]
      ,[IsActive]
      ,[Created]
      ,[LastUpdated]
      ,[LastUpdatedById]
      ,[RowId]
      ,[AspNetId]
      ,[Roles]
  FROM [dbo].[Account_Summary]
GO



--=====================================================

DECLARE @RC int,@SortOrder varchar(100),@Filter varchar(5000)
DECLARE @StartPageIndex int, @PageSize int, @TotalRows int,@CurrentUserId	int
--
set @CurrentUserId = 0
set @SortOrder = ''


set @Filter = ' event = ''View'' '
set @Filter = ' convert(varchar(10),createdDate,120) = ''2020-10-26'''
set @Filter = ' (ActivityType LIKE ''%asses%'') AND (Activity LIKE ''%view%'')'
--set @Filter = ''	-- blind search 
set @StartPageIndex = 4
set @PageSize = 55
--set statistics time on       
EXECUTE @RC = [Activity_Search]
     @Filter,@SortOrder  ,@StartPageIndex  ,@PageSize, @TotalRows OUTPUT

select 'total rows = ' + convert(varchar,@TotalRows)

--set statistics time off       


*/


/* =============================================
Description:      Activity search
Options:

  @StartPageIndex - starting page number. If interface is at 20 when next
page is requested, this would be set to 21?
  @PageSize - number of records on a page
  @TotalRows OUTPUT - total available rows. Used by interface to build a
custom pager
  ------------------------------------------------------
Modifications
20-12-17 mparsons - new version that is much more performant
21-01-18 mparsons - changed back to use a temp table for target set due to issues with sort order
*/

Alter PROCEDURE [dbo].[Activity_Search]
		@Filter           varchar(5000)
		,@SortOrder       varchar(100)
		,@StartPageIndex  int
		,@PageSize        int
		,@TotalRows       int OUTPUT

As

SET NOCOUNT ON;
-- paging
DECLARE
      @first_id               int
      ,@startRow        int
	  ,@lastRow int
      ,@debugLevel      int
      ,@SQL             varchar(5000)
      ,@OrderBy         varchar(100)


-- =================================

--set @CurrentUserId = 24
Set @debugLevel = 4

if len(@SortOrder) > 0
      set @OrderBy = ' Order by ' + @SortOrder
else
      set @OrderBy = ' Order by base.CreatedDate desc '

--===================================================
-- Calculate the range
--===================================================
IF @StartPageIndex < 1			SET @StartPageIndex = 1
SET @StartPageIndex =  ((@StartPageIndex - 1)  * @PageSize) + 1
SET @lastRow =  (@StartPageIndex + @PageSize) - 1
PRINT '@StartPageIndex = ' + convert(varchar,@StartPageIndex) +  ' @lastRow = ' + convert(varchar,@lastRow)

-- =================================
--IDENTITY(1,1)
CREATE TABLE #tempWorkTable(
      RowNumber         int PRIMARY KEY  NOT NULL,
      Id int,
      Title             varchar(max)
)
  CREATE TABLE #tempQueryTotalTable(
      TotalRows int
)

-- =================================

  if len(@Filter) > 0 begin
     if charindex( 'where', @Filter ) = 0 OR charindex( 'where',  @Filter ) > 10
        set @Filter =     ' where ' + @Filter
     end

  print '@Filter len: '  +  convert(varchar,len(@Filter))
  -- ================================= 
set @SQL = '   SELECT count(*) as TotalRows  FROM [dbo].Activity_Summary base  '  + @Filter 
INSERT INTO #tempQueryTotalTable (TotalRows)
exec (@SQL)
--select * from #tempQueryTotalTable
select top 1  @TotalRows= TotalRows from #tempQueryTotalTable
--select @TotalRows
--====
  set @SQL = ' 
  SELECT        
		DerivedTable.RowNumber, 
		 base.Id, base.Comment
From ( SELECT 
         ROW_NUMBER() OVER(' + @OrderBy + ') as RowNumber,
           base.Id, base.Comment
		from [Activity_Summary] base  ' 
        + @Filter + ' 
   ) as DerivedTable
       Inner join [dbo].[Activity_Summary] base on DerivedTable.Id = base.Id
WHERE RowNumber BETWEEN ' + convert(varchar,@StartPageIndex) + ' AND ' + convert(varchar,@lastRow) + ' '  

  print '@SQL len: '  +  convert(varchar,len(@SQL))
  print @SQL
  INSERT INTO #tempWorkTable (RowNumber, Id, Title)
  exec (@SQL)
  
-- =================================
  --set @SQL = 'SELECT  base.Id, base.Comment 
  --      from [Activity_Summary] base   '
  --      + @Filter
        
  --if charindex( 'order by', lower(@Filter) ) = 0
  --  set @SQL = @SQL + ' ' + @OrderBy

  --print '@SQL len: '  +  convert(varchar,len(@SQL))
  --print @SQL

  --INSERT INTO #tempWorkTable (Id, Title)
  --exec (@SQL)
  ----print 'rows: ' + convert(varchar, @@ROWCOUNT)
  --SELECT @TotalRows = @@ROWCOUNT
-- =================================

--print 'added to temp table: ' + convert(varchar,@TotalRows)
--if @debugLevel > 7 begin
--  select * from #tempWorkTable
--  end

-- Calculate the range
--===================================================
--SET ROWCOUNT @StartPageIndex
--SELECT @first_id = @StartPageIndex
--PRINT '@first_id = ' + convert(varchar,@first_id)

--if @first_id = 1 set @first_id = 0
--set max to return
--SET ROWCOUNT @PageSize


-- ================================= 
--  set @SQL = '  
--SELECT        
--		DerivedTable.RowNumber, 
--		base.Id
--		,base.[CreatedDate]
--		,base.[ActivityType]
--		,base.[Activity]
--		,base.[Event]
--		,base.[Comment]
--		,base.[TargetUserId]
--		--,base.[ActionByUserId]
--		--,base.[ActionByUser]
--		,base.[ActivityObjectId]
--		,base.[ObjectRelatedId]
--		,base.[RelatedTargetUrl]
--		,base.[TargetObjectId]
--		,base.[SessionId]
--		,base.[IPAddress]
--		,base.[Referrer]
--		,base.[IsBot]
--		,base.EntityTypeId
--	 ,IsNull(base.OwningOrgId,0) As OwningOrgId
--	  ,IsNull(base.Organization,'''') As Organization
--From 
--   (SELECT 
--         ROW_NUMBER() OVER(' + @OrderBy + ') as RowNumber,
--         base.Id, base.ActivityType
--        FROM [dbo].Activity_Summary base 
--		  ' 
--        + @Filter + ' 
--   ) as DerivedTable
--       Inner join [dbo].[Activity_Summary] base on DerivedTable.Id = base.Id

--WHERE RowNumber BETWEEN ' + convert(varchar,@StartPageIndex) + ' AND ' + convert(varchar,@lastRow) + ' ' 

--  print '@SQL len: '  +  convert(varchar,len(@SQL))
--  print @SQL
--  exec (@SQL)
SELECT        
		RowNumber, 
		base.Id
		,base.[CreatedDate]
		,base.[ActivityType]
		,base.[Activity]
		,base.[Event]
		,base.[Comment]

		,base.[ActivityObjectId]
		,base.EntityName, base.EntityStateId, base.EntityCTID, base.EntityTypeId
		,base.[ObjectRelatedId]
		,base.[RelatedTargetUrl]
		,base.[TargetObjectId]
		,base.[SessionId]
		,base.[TargetUserId]
		--,base.[ActionByUserId]
		--,base.[ActionByUser]
		,0 as [ActionByUserId]
		,'' as [ActionByUser]
		,base.[IPAddress]
		,base.[Referrer]
		,base.[IsBot]
		,base.EntityTypeId
		,IsNull(base.OwningOrgId,0) As OwningOrgId
		,IsNull(base.Organization,'') As Organization
		,base.OrganizationEntityStateId
From #tempWorkTable work
	Inner join Activity_Summary base on work.Id = base.Id

--WHERE RowNumber > @first_id
order by RowNumber 
go
grant execute on [Activity_Search] to public


GO



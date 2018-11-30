USE credFinder
GO

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

-- blind search 

set @Filter = ''
set @StartPageIndex = 1
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
16-04-19 mparsons - new

*/

CREATE PROCEDURE [dbo].[Activity_Search]
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
SET @StartPageIndex =  (@StartPageIndex - 1)  * @PageSize
IF @StartPageIndex < 1        SET @StartPageIndex = 1

-- =================================
CREATE TABLE #tempWorkTable(
      RowNumber         int PRIMARY KEY IDENTITY(1,1) NOT NULL,
      Id int,
      Title             varchar(max)
)
-- =================================

  if len(@Filter) > 0 begin
     if charindex( 'where', @Filter ) = 0 OR charindex( 'where',  @Filter ) > 10
        set @Filter =     ' where ' + @Filter
     end

  print '@Filter len: '  +  convert(varchar,len(@Filter))
  set @SQL = 'SELECT  base.Id, base.Comment 
        from [Activity_Summary] base   '
        + @Filter
        
  if charindex( 'order by', lower(@Filter) ) = 0
    set @SQL = @SQL + ' ' + @OrderBy

  print '@SQL len: '  +  convert(varchar,len(@SQL))
  print @SQL

  INSERT INTO #tempWorkTable (Id, Title)
  exec (@SQL)
  --print 'rows: ' + convert(varchar, @@ROWCOUNT)
  SELECT @TotalRows = @@ROWCOUNT
-- =================================

print 'added to temp table: ' + convert(varchar,@TotalRows)
if @debugLevel > 7 begin
  select * from #tempWorkTable
  end

-- Calculate the range
--===================================================
PRINT '@StartPageIndex = ' + convert(varchar,@StartPageIndex)

SET ROWCOUNT @StartPageIndex
SELECT @first_id = @StartPageIndex
PRINT '@first_id = ' + convert(varchar,@first_id)

if @first_id = 1 set @first_id = 0
--set max to return
SET ROWCOUNT @PageSize

-- ================================= 
SELECT        
    RowNumber, 
		base.Id
    ,base.[CreatedDate]
      ,base.[ActivityType]
      ,base.[Activity]
      ,base.[Event]
      ,base.[Comment]
      ,base.[TargetUserId]
      ,base.[ActionByUserId]
      ,base.[ActionByUser]
      ,base.[ActivityObjectId]
      ,base.[ObjectRelatedId]
      ,base.[RelatedTargetUrl]
      ,base.[TargetObjectId]
      ,base.[SessionId]
      ,base.[IPAddress]
      ,base.[Referrer]
      ,base.[IsBot]

From #tempWorkTable work
	Inner join Activity_Summary base on work.Id = base.Id

WHERE RowNumber > @first_id
order by RowNumber 
go
grant execute on [Activity_Search] to public


GO



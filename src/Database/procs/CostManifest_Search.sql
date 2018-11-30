use credFinder
GO

/****** Object:  StoredProcedure [dbo].[CostManifest_Search]    Script Date: 10/21/2017 3:41:07 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


/*
use credFinder
GO

--=====================================================

DECLARE @RC int,@SortOrder varchar(100),@Filter varchar(5000)
DECLARE @StartPageIndex int, @PageSize int, @TotalRows int
--
set @SortOrder = ''

set @Filter = ''
set @StartPageIndex = 1
set @PageSize = 15
--set statistics time on       
EXECUTE @RC = [CostManifest_Search]
     @Filter,@SortOrder  ,@StartPageIndex  ,@PageSize, @TotalRows OUTPUT

select 'total rows = ' + convert(varchar,@TotalRows)

set statistics time off       


*/


/* =============================================
Description:      CostManifests search
Options:

  @StartPageIndex - starting page number. 
  @PageSize - number of records on a page
  @TotalRows OUTPUT - total available rows. Used by interface to build a
custom pager
  ------------------------------------------------------
Modifications
16-09-02 mparsons - new

*/

CREATE PROCEDURE [dbo].[CostManifest_Search]
		@Filter           varchar(5000)
		,@SortOrder       varchar(100)
		,@StartPageIndex  int
		,@PageSize        int
		,@TotalRows       int OUTPUT

As

SET NOCOUNT ON;
-- paging
DECLARE
	@first_id         int
	,@startRow        int
	,@debugLevel      int
	,@SQL             varchar(5000)
	,@OrderBy         varchar(100)
	,@HasSitePrivileges bit

-- =================================

Set @debugLevel = 4
set @HasSitePrivileges= 0

if len(@SortOrder) > 0 AND @SortOrder = 'newest'
      set @OrderBy = ' Order by Id Desc'

--else if len(@SortOrder) > 0 AND @SortOrder <> 'relevance'
--      set @OrderBy = ' Order by ' + @SortOrder
else
      set @OrderBy = ' Order by base.Name '

--===================================================
-- Calculate the range
--===================================================
SET @StartPageIndex =  (@StartPageIndex - 1)  * @PageSize
IF @StartPageIndex < 1        SET @StartPageIndex = 1

-- =================================
CREATE TABLE #tempWorkTable(
      RowNumber         int PRIMARY KEY IDENTITY(1,1) NOT NULL,
      Id int,
      Title             varchar(500)
)

-- =================================

  if len(@Filter) > 0 begin
     if charindex( 'where', @Filter ) = 0 OR charindex( 'where',  @Filter ) > 10
        set @Filter =     ' where ' + @Filter
     end

  print '@Filter len: '  +  convert(varchar,len(@Filter))
  set @SQL = 'SELECT  base.Id, base.Name 
        from [CostManifest] base   '
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
--SELECT @first_id = RowNumber FROM #tempWorkTable   ORDER BY RowNumber
SELECT @first_id = @StartPageIndex
PRINT '@first_id = ' + convert(varchar,@first_id)

if @first_id = 1 set @first_id = 0
--set max to return
SET ROWCOUNT @PageSize

-- ================================= 
SELECT        
	RowNumber 
	,base.Id
	,base.Name
	,base.OrganizationId
	,base.Description
	,base.CostDetails
	,base.CTID
	,base.StartDate, base.EndDate
	,base.CredentialRegistryId
	,base.Created
	,base.LastUpdated
	,base.RowId

From #tempWorkTable work
	Inner join [CostManifest] base on work.Id = base.Id

WHERE RowNumber > @first_id
order by RowNumber 
go
grant execute on CostManifest_Search to public


GO



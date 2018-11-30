use credFinder
GO

/****** Object:  StoredProcedure [dbo].[Competencies_search]    Script Date: 10/6/2017 4:08:13 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


/*

--=====================================================

DECLARE @RC int,@SortOrder varchar(100),@Filter varchar(5000)
DECLARE @StartPageIndex int, @PageSize int, @TotalRows int
--
set @SortOrder = ''
set @SortOrder = 'base.Competency'

set @Filter = '  (AlignmentType in (''Assesses'', ''Teaches'') ) AND  (credentialId =1  ) '

set @Filter = ' (credentialId = 52  )'

--set @Filter = ' (SourceEntityTypeId = 3 AND [SourceId] = 17 )'

--set @Filter = ''
set @StartPageIndex = 1
set @PageSize = 300
--set statistics time on       
EXECUTE @RC = [Competencies_search]
     @Filter,@SortOrder  ,@StartPageIndex  ,@PageSize, @TotalRows OUTPUT

select 'total rows = ' + convert(varchar,@TotalRows)

set statistics time off       


*/


/* =============================================
Description:      Competencies search
Options:

  @StartPageIndex - starting page number. 
  @PageSize - number of records on a page
  @TotalRows OUTPUT - total available rows. Used by interface to build a
custom pager
  ------------------------------------------------------
Modifications
16-09-02 mparsons - new

*/

Alter PROCEDURE [dbo].[Competencies_search]
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

if len(@SortOrder) > 0 AND @SortOrder <> 'relevance'
      set @OrderBy = ' Order by ' + @SortOrder
else
      set @OrderBy = ' Order by base.SourceName, base.Competency '

--===================================================
-- Calculate the range
--===================================================
SET @StartPageIndex =  (@StartPageIndex - 1)  * @PageSize
IF @StartPageIndex < 1        SET @StartPageIndex = 1

-- =================================
CREATE TABLE #tempWorkTable(
      RowNumber         int PRIMARY KEY IDENTITY(1,1) NOT NULL,
      Id int,
      Title             varchar(500),
	  Source             varchar(500),
)

-- =================================

  if len(@Filter) > 0 begin
     if charindex( 'where', @Filter ) = 0 OR charindex( 'where',  @Filter ) > 10
        set @Filter =     ' where ' + @Filter
     end

  print '@Filter len: '  +  convert(varchar,len(@Filter))
  set @SQL = 'SELECT   base.Id, base.Competency, base.SourceName 
        from [ConditionProfile_Competencies_cache] base   '
        + @Filter
        
  if charindex( 'order by', lower(@Filter) ) = 0
    set @SQL = @SQL + ' ' + @OrderBy

  print '@SQL len: '  +  convert(varchar,len(@SQL))
  print @SQL

  INSERT INTO #tempWorkTable (Id, Title, Source)
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
	,[CredentialId]
	,[ConnectionTypeId]
	,[EntityConditionProfileRowId]
	,[nodeLevel]
	,[SourceEntityTypeId]
	,[SourceId]
	,[SourceName]
	,CompetencyFrameworkItemId
	,case when len(Isnull(Description,'')) > 5 then [Competency] + ' -- ' + Description
	else [Competency] end as [Competency1]
	,[Competency] As Competency
	,Description
	--,[AlignmentType]
	--,[AlignmentTypeId]

From #tempWorkTable work
	Inner join [ConditionProfile_Competencies_cache] base on work.Id = base.Id

WHERE RowNumber > @first_id
order by RowNumber 
go
grant execute on [Competencies_search] to public


GO



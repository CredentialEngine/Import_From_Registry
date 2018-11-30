use credFinder
GO

/****** Object:  StoredProcedure [dbo].[CostProfileItems_search]    Script Date: 10/6/2017 5:31:33 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


/*


--=====================================================

DECLARE @RC int,@SortOrder varchar(100),@Filter varchar(5000)
,@condProfParentEntityTypeId	int
    ,@condProfParentEntityBaseId int
DECLARE @StartPageIndex int, @PageSize int, @TotalRows int
--
set @SortOrder = ''

set @Filter = ' ( condProfParentEntityTypeId = 1 and condProfParentEntityBaseId =1141)'

set @condProfParentEntityTypeId = 1 
set @condProfParentEntityBaseId = 1141
set @Filter = ''
set @StartPageIndex = 1
set @PageSize = 15
--set statistics time on       
EXECUTE @RC = [CostProfileItems_search]
     @condProfParentEntityTypeId, @condProfParentEntityBaseId, @Filter, @SortOrder  ,@StartPageIndex  ,@PageSize, @TotalRows OUTPUT

select 'total rows = ' + convert(varchar,@TotalRows)

set statistics time off       


*/


/* =============================================
Description:      CostProfileItems search - to bubble up all costs related to the entity
Options:

  @StartPageIndex - starting page number. 
  @PageSize - number of records on a page
  @TotalRows OUTPUT - total available rows. Used by interface to build a
custom pager
  ------------------------------------------------------
Modifications
17-06-20 mparsons - new

*/

CREATE PROCEDURE [dbo].[CostProfileItems_search]
		@condProfParentEntityTypeId	int
    ,@condProfParentEntityBaseId int
		,@Filter					varchar(5000)
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
	,@MainFilter					varchar(1000)

-- =================================

Set @debugLevel = 4
set @HasSitePrivileges= 0

if len(@SortOrder) > 0 AND @SortOrder <> 'relevance'
      set @OrderBy = ' Order by ' + @SortOrder
else
      set @OrderBy = ' Order by base.EntityType, base.CostProfileName '

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
set @Filter = ' ( condProfParentEntityTypeId = ' + convert(varchar, @condProfParentEntityTypeId) 
							+ ' and condProfParentEntityBaseId = '+ convert(varchar, @condProfParentEntityBaseId) 
							+ ' )'

  if len(@Filter) > 0 begin
     if charindex( 'where', @Filter ) = 0 OR charindex( 'where',  @Filter ) > 10
        set @Filter =     ' where ' + @Filter
     end

  print '@Filter len: '  +  convert(varchar,len(@Filter))
  set @SQL = 'SELECT  base.Entity_CostProfileId, base.CostProfileName 
        from [CostProfile_SummaryForSearch] base   
				'
        + @Filter
        
--inner join [Entity.CostProfileItem] b on base.Entity_CostProfileId = b.CostProfileId
--	inner join [Codes.PropertyValue] c on b.CostTypeId = c.Id 

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
SELECT    distinct  
	RowNumber 
	,base.Entity_CostProfileId

			,[EntityBaseId]
      ,[EntityTypeId]
      ,[EntityType]
      ,[EntityId]
      ,[ParentName]
      ,[OwningAgentUid]
			,[condProfParentEntityTypeId]
      ,[condProfParentEntityBaseId]

      ,[CostProfileName]
      ,[CostProfileRowId]
      ,[CostDescription]
			,Currency, base.CurrencySymbol
      ,b.CostTypeId, c.Title as CostType
			,b.Price
From #tempWorkTable work
	Inner join [CostProfile_SummaryForSearch] base on work.Id = base.Entity_CostProfileId

	inner join [Entity.CostProfileItem] b on work.Id = b.CostProfileId
	inner join [Codes.PropertyValue] c		on b.CostTypeId = c.Id


WHERE RowNumber > @first_id
and base.condProfParentEntityTypeId= @condProfParentEntityTypeId
and base.condProfParentEntityBaseId= @condProfParentEntityBaseId
order by RowNumber 
--,b.CostTypeId
go
grant execute on CostProfileItems_search to public


GO



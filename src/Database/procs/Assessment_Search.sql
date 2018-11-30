use credFinder
GO

/****** Object:  StoredProcedure [dbo].[Assessment_Search]    Script Date: 8/16/2017 9:26:00 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



/*
use credFinder
GO

	SELECT distinct c.Id, Count(*) as nbr
  FROM [dbo].[Entity.CompetencyFramework] a
	Inner Join [Entity] b on a.EntityId = b.Id 
	Inner Join Assessment c on b.EntityUid = c.RowId
	where a.AlignmentType = 'Requires'
	group by c.Id
SELECT [Id]
      ,[Name]
      ,[Description]
      ,[Address1]      ,[Address2]
      ,[City]      ,[Region]      ,[PostalCode]      ,[Country]
      ,[Email]
      ,[MainPhoneNumber]
      ,[URL]
      ,[UniqueURI]      ,[ImageURL]
      ,[RowId]
      ,[CredentialCount]
      ,[IsAQAOrganization]
     -- ,[Created]  ,[LastUpdated]    ,[CreatedById]  ,[LastUpdatedById]

  FROM [dbo].[Organization_Summary]
GO

--=====================================================

DECLARE @RC int,@SortOrder varchar(100),@Filter varchar(5000)
DECLARE @StartPageIndex int, @PageSize int, @TotalRows int
--

set @SortOrder = ''
set @SortOrder = 'lastupdated'
set @SortOrder = 'cost_highest'
set @SortOrder = 'org_alpha'
--set @SortOrder = 'cost_lowest'
--set @SortOrder = 'duration_shortest'
--set @SortOrder = 'duration_longest'
-- blind search 
set @Filter = ' ( base.Id in (SELECT  OrgId FROM [Organization.Member] where UserId = 2) ) '


set @Filter = ' (len(isnull(base.AvailableOnlineAt,'''')) > 10) '

set @Filter = ' ( base.Id in (SELECT distinct c.Id FROM [dbo].[Entity.CompetencyFramework] a Inner Join [Entity] b on a.EntityId = b.Id Inner Join Assessment c on b.EntityUid = c.RowId where a.AlignmentType = ''Requires''  group by c.Id having count(*) > 0 ))  '

	set @Filter = '  (base.Id in (SELECT c.id FROM [dbo].[Entity.FrameworkItemSummary] a inner join Entity b on a.EntityId = b.Id inner join Assessment c on b.EntityUid = c.RowId where [CategoryId] = 23 and ([CodeGroup] in ('''')  OR ([CodeId] in ('''') ) ) ) )  '

set @Filter = ''

set @StartPageIndex = 1
set @PageSize = 100
--set statistics time on       
EXECUTE @RC = [Assessment_Search]
     @Filter,@SortOrder  ,@StartPageIndex  ,@PageSize, @TotalRows OUTPUT

select 'total rows = ' + convert(varchar,@TotalRows)

--set statistics time off       


*/


/* =============================================
Description:      Assessments search
Options:

  @StartPageIndex - starting page number. If interface is at 20 when next
page is requested, this would be set to 21?
  @PageSize - number of records on a page
  @TotalRows OUTPUT - total available rows. Used by interface to build a
custom pager
  ------------------------------------------------------
Modifications
16-06-16 mparsons - new

*/

Alter PROCEDURE [dbo].[Assessment_Search]
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

Set @debugLevel = 4


if @SortOrder = 'relevance' set @SortOrder = 'base.Name '
else if @SortOrder = 'alpha' set @SortOrder = 'base.Name '
else if @SortOrder = 'org_alpha' set @SortOrder = 'Organization, base.Name '
else if @SortOrder = 'newest' set @SortOrder = 'base.lastUpdated Desc '
else if @SortOrder = 'cost_highest' set @SortOrder = 'costs.TotalCost DESC'
else if @SortOrder = 'cost_lowest' set @SortOrder = 'costs.TotalCost'
else set @SortOrder = 'base.Name '

if len(@SortOrder) > 0 
      set @OrderBy = ' Order by ' + @SortOrder
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
      Title             varchar(200)
			,OwningOrganization varchar(300)
)

-- =================================

  if len(@Filter) > 0 begin
     if charindex( 'where', @Filter ) = 0 OR charindex( 'where',  @Filter ) > 10
        set @Filter =     ' where ' + @Filter
     end

  print '@Filter len: '  +  convert(varchar,len(@Filter))
  set @SQL = 'SELECT  base.Id, base.Name, Organization 
        from [Assessment_Summary] base 
				--not ideal, but doing a total
					left join (
					Select ParentEntityUid, sum(isnull(TotalCost, 0)) As TotalCost from Entity_CostProfileTotal group by ParentEntityUid
					) costs	on base.RowId = costs.ParentEntityUid				'
        + @Filter
        
  if charindex( 'order by', lower(@Filter) ) = 0
    set @SQL = @SQL + ' ' + @OrderBy

  print '@SQL len: '  +  convert(varchar,len(@SQL))
  print @SQL

  INSERT INTO #tempWorkTable (Id, Title, OwningOrganization)
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
	RowNumber, 
	base.id, 
	base.Name, 
	isnull(base.Description,'') As Description,

	isnull(base.SubjectWebpage,'') As SubjectWebpage

	,[DateEffective]
    ,isnull(base.[IdentificationCode],'') As [IdentificationCode]

	--,[SourceToAgentRelationship]
	--,[OrgId]
	--,[Organization]
	,case when Charindex('Placeholder', Isnull([Organization], '')) = 1 then 0
	else [OrgId] end  as [OrgId]
	,case when Charindex('Placeholder', Isnull([Organization], '')) = 1 then ''
	else [Organization] end  as [Organization]

	,base.Created, 
	base.LastUpdated 
	,base.availableOnlineAt
	,base.RowId

	,base.RequiresCount
	,base.RecommendsCount
	,base.isRequiredForCount
	,base.IsRecommendedForCount
	,base.IsAdvancedStandingForCount
	,base.AdvancedStandingFromCount
	,base.isPreparationForCount
	,base.PreparationFromCount
	,base.PreparationFromCount as IsPreparationFromCount

	,'' as QualityAssurance

	,isnull(costs.totalCost,0) As TotalCost
	,ea.Nbr as AvailableAddresses
	,base.CTID
	,base.CredentialRegistryId
	--,isnull(comps.Nbr,0) as Competencies
	,0 as Competencies
From #tempWorkTable work
Inner join Assessment_Summary base on work.Id = base.Id

  Inner Join Entity e on base.RowId = e.EntityUid
  left Join (select EntityId, count(*) as nbr from [Entity.Address] group by EntityId ) ea on e.Id = ea.EntityId
	--not ideal, but doing a total
	left join (
					Select ParentEntityUid, sum(isnull(TotalCost, 0)) As TotalCost from Entity_CostProfileTotal group by ParentEntityUid
					) costs	on base.RowId = costs.ParentEntityUid       
	--left Join (
	--	select EntityId, count(*) as nbr from Assessment_Competency_Summary group by EntityId 
	--	) comps on e.Id = comps.EntityId  

WHERE RowNumber > @first_id
order by RowNumber 


GO
grant execute on [Assessment_Search] to public
go


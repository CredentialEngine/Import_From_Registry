USE credFinder
GO
--use sandbox_credFinder
--go
--use staging_credFinder
--go

/****** Object:  StoredProcedure [dbo].[LearningOpportunity_Search]    Script Date: 3/8/2018 7:29:22 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/*
use credFinder
GO

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
     -- ,[Created]  ,[LastUpdated]    

  FROM [dbo].[Organization_Summary]
GO

--=====================================================

DECLARE @RC int,@SortOrder varchar(100),@Filter varchar(5000)
DECLARE @StartPageIndex int, @PageSize int, @TotalRows int
--

set @SortOrder = 'newest'
set @SortOrder = 'cost_highest'
set @SortOrder = 'cost_lowest'
set @SortOrder = 'org_alpha'
set @SortOrder = 'alpha'
--set @SortOrder = 'duration_shortest'
--set @SortOrder = 'duration_longest'

-- blind search 

set @Filter = '  (base.name like ''%western gov%'' OR base.Description like ''%western gov%''  OR base.Organization like ''%western gov%''   OR base.Url like ''%western gov%'') '

set @Filter = '  ( base.EntityStateId = 3 ) AND ( base.CTID in (Select distinct CTID from [Entity_Cache]  where EntityTypeId=7 AND  IsNull(ResourceDetail,'''') = '''' ) ) '
	
--set @Filter = '  ( CTID in (Select distinct EntityCtid from [Import.PendingRequest] where PublisherCTID = ''ce-89bd0d16-6492-4ae1-8059-deabc3d89c15''  )) and EntityStateId=3 '

set @Filter = ' base.EntityStateId = 2 '

set @StartPageIndex = 4
set @PageSize = 75
--set statistics time on       
EXECUTE @RC = [LearningOpportunity_Search]
     @Filter,@SortOrder  ,@StartPageIndex  ,@PageSize, @TotalRows OUTPUT

select 'total rows = ' + convert(varchar,@TotalRows)

--set statistics time off       


*/


/* =============================================
Description:      LearningOpportunity search
Options:

  @StartPageIndex - starting page number. 
  @PageSize - number of records on a page
  @TotalRows OUTPUT - total available rows. Used by interface to build a
custom pager
  ------------------------------------------------------
Modifications
16-06-16 mparsons - new

*/

ALTER PROCEDURE [dbo].[LearningOpportunity_Search]
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

-- =================================

Set @debugLevel = 4

print '@SortOrder ' + @SortOrder
if @SortOrder = 'relevance' set @SortOrder = 'base.Name '
else if @SortOrder = 'alpha' set @SortOrder = 'base.Name '
else if @SortOrder = 'org_alpha' set @SortOrder = 'base.Organization, base.Name '
else if @SortOrder = 'newest' set @SortOrder = 'base.lastUpdated Desc '
else if @SortOrder = 'oldest' set @SortOrder = 'base.Id '
--else if @SortOrder = 'cost_highest' set @SortOrder = 'costs.TotalCost DESC'
--else if @SortOrder = 'cost_lowest' set @SortOrder = 'costs.TotalCost'
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
--TODO - just remove title and organization
CREATE TABLE #tempWorkTable(
      RowNumber         int PRIMARY KEY IDENTITY(1,1) NOT NULL,
      Id int,
      Title             varchar(1000)
			,Organization varchar(1000)
)

-- =================================

  if len(@Filter) > 0 begin
     if charindex( 'where', @Filter ) = 0 OR charindex( 'where',  @Filter ) > 10
        set @Filter =     ' where ' + @Filter
     end

  print '@Filter len: '  +  convert(varchar,len(@Filter))
  set @SQL = 'SELECT  base.Id, base.Name , base.Organization  
        from [LearningOpportunity_Summary] base 	'
        + @Filter
        
  if charindex( 'order by', lower(@Filter) ) = 0
    set @SQL = @SQL + ' ' + @OrderBy

  print '@SQL len: '  +  convert(varchar,len(@SQL))
  print @SQL

  INSERT INTO #tempWorkTable (Id, Title, Organization)
  exec (@SQL)
  --print 'rows: ' + convert(varchar, @@ROWCOUNT)
  SELECT @TotalRows = @@ROWCOUNT
-- =================================

print 'added to temp table: ' + convert(varchar,@TotalRows)
if @debugLevel > 7 begin
  select a.*, base.ctid from #tempWorkTable a
  	Inner join LearningOpportunity_Summary base on a.Id = base.Id
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
	base.CTID,
	base.EntityStateId,
	base.Name, 
	isnull(base.Description,'') As Description
	--[OrgId],base.Organization,
	,case when Charindex('Placeholder', Isnull(base.Organization, '')) = 1 then 0
	else [OrgId] end  as [OrgId]
	,case when Charindex('Placeholder', Isnull(base.Organization, '')) = 1 then ''
	else base.Organization end  as [Organization]
	
	 
	,isnull(base.SubjectWebpage,'') As SubjectWebpage

	,[DateEffective]
    ,isnull(base.[IdentificationCode],'') As [IdentificationCode]

	,base.availableOnlineAt
	,base.Created
	,base.LastUpdated 
	,base.RowId

	--,isnull(costs.totalCost,0) As TotalCost
	--,base.RowId as EntityUid
	,ea.Nbr as AvailableAddresses
	
	--,isnull(comps.Nbr,0) as Competencies
	,0 as Competencies

	,base.CredentialRegistryId

	,base.RequiresCount
	,base.RecommendsCount
	,base.isRequiredForCount
	,base.IsRecommendedForCount
	,base.IsAdvancedStandingForCount
	,base.AdvancedStandingFromCount
	,base.isPreparationForCount
	,base.PreparationFromCount as isPreparationFromCount
	,'' as QualityAssurance
	,case when isnull(e.AgentRelationshipsForEntity,'') = '' then '' else 'yes' end as  HasAgentRelationshipsForEntity
	,case when isnull(e.ResourceDetail,'') = '' then '' else 'yes' end as  HasResourceDetail

From #tempWorkTable work
	Inner join LearningOpportunity_Summary base on work.Id = base.Id
	Left Join Entity_Cache e on base.RowId = e.EntityUid
	left Join (select EntityId, count(*) as nbr from [Entity.Address] group by EntityId ) ea on e.Id = ea.EntityId

	--left Join (
	--	select EntityId, count(*) as nbr from LearningOpportunity_Competency_Summary group by EntityId 
	--) comps on e.Id = comps.EntityId  

	--left join Entity_CostProfileTotal costs on base.RowId = costs.ParentEntityUid
	--not ideal, but doing a total
	--left join (
	--Select ParentEntityUid, sum(isnull(TotalCost, 0)) As TotalCost from Entity_CostProfileTotal group by ParentEntityUid
	--) costs	on base.RowId = costs.ParentEntityUid 

WHERE RowNumber > @first_id
order by RowNumber 
GO

grant execute on LearningOpportunity_Search to public
go


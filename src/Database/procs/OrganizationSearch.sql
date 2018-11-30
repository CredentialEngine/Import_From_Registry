use credFinder
GO

/****** Object:  StoredProcedure [dbo].[OrganizationSearch]    Script Date: 8/16/2017 9:26:12 AM ******/
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
     -- ,[Created]  ,[LastUpdated]    ,[CreatedById]  ,[LastUpdatedById]

  FROM [dbo].[Organization_Summary]
GO

--=====================================================

DECLARE @RC int,@SortOrder varchar(100),@Filter varchar(5000)
DECLARE @StartPageIndex int, @PageSize int, @TotalRows int
--

set @SortOrder = ''
set @SortOrder = 'base.Name'
-- blind search 
set @Filter = ' ( base.Id in (SELECT  OrgId FROM [Organization.Member] where UserId = 2) ) '
set @Filter = ' (OrgTypeId in (1,6)) '

set @Filter = ' ( base.Id in (SELECT  OrgId FROM [OrganizationMember.RoleIdCSV] where ([IsAdmin] = 1 or [IsAccountAdmin] = 1) AND  UserId = 2) ) '
set @Filter = ' [IsAQAOrganization] = 1 '
set @Filter = ' [CredentialCount] > 0 '


set @Filter = ' ( base.Id in ( SELECT  [OrganizationId] FROM [dbo].[EntityProperty.Summary] where [PropertyValueId] in (54,66,65))) '

set @Filter = '(base.Id in (SELECT ParentOrgId FROM workIT.[dbo].[Organization.Member] where userId = 17) ) '
set @Filter = '  RowId = ''BF5D5693-2706-467C-8081-46EE8A0EF578'' '

set @Filter = '   ( base.Id in ( 	SELECT b.EntityBaseId FROM [dbo].[Entity.VerificationProfile] a inner join entity b on a.EntityId = b.Id  ) )  '

set @Filter = '  (base.Id in (SELECT c.id FROM [dbo].[Entity.FrameworkItemSummary] a inner join Entity b on a.EntityId = b.Id inner join Organization c on b.EntityUid = c.RowId where [CategoryId] = 10  ) )  '
--and ([CodeGroup] in (1817)  OR ([CodeId] in (1817) ) )

set @Filter = ' ( base.Id in (SELECT b.EntityBaseId FROM [dbo].[Entity.ConditionManifest] a inner join entity b on a.EntityId = b.Id  ) )  '

set @Filter = ' ( Name = ''American Welding Society'' ) '

set @Filter = ''
set @StartPageIndex = 1
set @PageSize = 55
--set statistics time on       
EXECUTE @RC = [OrganizationSearch]
     @Filter,@SortOrder  ,@StartPageIndex  ,@PageSize,  @TotalRows OUTPUT

select 'total rows = ' + convert(varchar,@TotalRows)

--set statistics time off       


*/


/* =============================================
Description:      Org search
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

Alter PROCEDURE [dbo].[OrganizationSearch]
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
	  ,@HasSitePrivileges bit

-- =================================

--set @CurrentUserId = 24
Set @debugLevel = 4
set @HasSitePrivileges= 0

if @SortOrder = 'relevance' set @SortOrder = 'base.Name '
else if @SortOrder = 'alpha' set @SortOrder = 'base.Name '
else if @SortOrder = 'newest' set @SortOrder = 'base.lastUpdated Desc '
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
)

-- =================================

  if len(@Filter) > 0 begin
     if charindex( 'where', @Filter ) = 0 OR charindex( 'where',  @Filter ) > 10
        set @Filter =     ' where ' + @Filter
     end

  print '@Filter len: '  +  convert(varchar,len(@Filter))
  set @SQL = 'SELECT  base.Id, base.Name 
        from [Organization_Summary] base   '
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
	RowNumber, 
	base.id, 
	base.Name, 
	base.Description, base.SubjectWebpage,
	--base.MainPhoneNumber, 
	--base.Email, 
	base.Address1,  base.Address2,  base.City, base.Region,		base.PostalCode, base.Country, 
	base.Latitude, base.Longitude,
	base.Created, 
	base.LastUpdated,  
	base.RowId, base.ImageURL
	,base.CTID
	,base.CredentialRegistryId
	,base.CredentialCount
	,base.IsAQAOrganization

	,oa.Nbr as AvailableAddresses

	,isnull(naicsCsv.naics,'') As NaicsList
	,isnull(naicsCsv.Others,'') As OtherIndustriesList

,isnull(actorRolesCsv.OwnedBy,'') As OwnedByList
,isnull(actorRolesCsv.OfferedBy,'') As OfferedByList
,isnull(actorRolesCsv.AsmtsOwnedBy,'') As AsmtsOwnedByList
,isnull(actorRolesCsv.LoppsOwnedBy,'') As LoppsOwnedByList

,isnull(recipRolesCsv.AccreditedBy,'') As AccreditedByList
,isnull(recipRolesCsv.ApprovedBy,'') As ApprovedByList

-- ======================================================
	-- For ElasticSearch
	-- Depends on Entity.SearchIndex, so the latter must be up to date!
	--, STUFF((SELECT '|' + ISNULL(NULLIF(a.TextValue, ''), NULL) AS [text()] FROM [dbo].[Entity] e INNER JOIN [dbo].[Entity.SearchIndex] a ON a.EntityId = e.Id where base.RowId = e.EntityUid AND e.EntityTypeId = 2 FOR XML Path('')), 1,1,'') TextValues

	--, STUFF((SELECT '|' + ISNULL(NULLIF(a.CodedNotation, ''), NULL) AS [text()] FROM [dbo].[Entity] e INNER JOIN [dbo].[Entity.SearchIndex] a ON a.EntityId = e.Id where base.RowId = e.EntityUid AND e.EntityTypeId = 2 FOR XML Path('')), 1,1,'') CodedNotation

	--, STUFF((SELECT '|' + ISNULL(NULLIF(CAST(eps.PropertyValueId AS NVARCHAR(MAX)), ''), NULL) AS [text()] FROM [dbo].[EntityProperty_Summary] eps where eps.EntityTypeId = 2 AND eps.EntityBaseId = base.Id FOR XML Path('')), 1,1,'') PropertyValues
 
 --, STUFF((SELECT '|' + ISNULL(NULLIF(CAST(a.[RelationshipTypeId] AS NVARCHAR(MAX)), ''), NULL) AS [text()] FROM [dbo].[Entity.AgentRelationship] a inner join Entity b on a.EntityId = b.Id WHERE b.EntityUid = base.RowId FOR XML Path('')), 1,1,'') RelationshipTypes
 
 -- [Entity_Subjects] is a union of direct and indirect subjects
 --, (SELECT a.[Subject], a.[Source], a.EntityTypeId, a.ReferenceBaseId FROM [dbo].[Entity_Subjects] a WHERE a.EntityTypeId = 1 AND a.EntityUid = base.RowId FOR XML RAW, ROOT('Subjects')) Subjects
 
 --	, (SELECT a.Latitude, a.Longitude, a.Region, a.Country FROM [dbo].[Entity.Address] a inner join Entity b on a.EntityId = b.Id WHERE a.Latitude <> 0 AND b.EntityTypeId = 2 AND b.EntityUid = base.RowId FOR XML RAW, ROOT('Addresses')) Addresses


From #tempWorkTable work
	Inner join Organization_Summary base on work.Id = base.Id

   left Join (select EntityBaseId, count(*) as nbr from [Entity_AddressSummary] where EntityTypeId = 2 group by EntityBaseId ) oa on base.Id = oa.EntityBaseId

	--18-06-21 mp - to avoid issues where cache update fails, using view directly
	--left join [Cache.Organization_ActorRoles] actorRolesCsv on work.id = actorRolesCsv.OrganizationId
	left join [Organization_ActorRolesCSV] actorRolesCsv on work.id = actorRolesCsv.OrganizationId
	
	left join Organization_RecipientRolesCSV recipRolesCsv on work.id = recipRolesCsv.OrganizationId

  left join [Entity.NaicsCSV] naicsCsv	on base.EntityId = naicsCsv.EntityId



	--
WHERE RowNumber > @first_id
order by RowNumber 


GO
grant execute on OrganizationSearch to public
go



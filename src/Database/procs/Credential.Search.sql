use credFinder
GO
/****** Object:  StoredProcedure [dbo].[Credential.Search]    Script Date: 8/22/2017 7:07:17 PM ******/
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
	set @SortOrder = 'duration_shortest'
	set @SortOrder = 'duration_longest'
	set @SortOrder = 'newest'
	set @SortOrder = 'relevance'
	
	--set @SortOrder = 'cost_highest'
	--set @SortOrder = 'cost_lowest'
		--set @SortOrder = 'cost_highest'
	set @SortOrder = 'org_alpha'

	-- blind search 

	set @Filter = ' ( organizationName like ''%depaul%'')'

	
	set @Filter = '    ( id in (SELECT [CredentialId] FROM [dbo].[CredentialAgentRelationships_Summary] where RelationshipTypeId = 1 and OrgId = 64)) '

	set @Filter = '  (base.Id in (SELECT c.id FROM [dbo].[Entity.FrameworkItemSummary] a inner join Entity b on a.EntityId = b.Id inner join Credential c on b.EntityUid = c.RowId where [CategoryId] = 10 and ([CodeGroup] in (71,32)  OR ([CodeId] in ('''') ) ) ) )  '
--
 set @Filter = '  [SubjectWebpage] like ''http://lr-staging.learningtapestry.com/resources/ce-%'' ' 


	set @Filter = '  ( base.Id in (SELECT b.EntityBaseId FROM [dbo].[Entity.Address] a inner join entity b on a.EntityId = b.Id   AND b.EntityTypeId = 1 ) )  '
	--set @Filter = ''

	set @StartPageIndex = 1
	set @PageSize = 99
	--set statistics IO on       
	EXECUTE @RC = [Credential.Search]
			 @Filter,@SortOrder  ,@StartPageIndex  ,@PageSize, @TotalRows OUTPUT

	select 'total rows = ' + convert(varchar,@TotalRows)

	set statistics IO off       


*/


/* =============================================
Description:      Credential search
Options:

  @StartPageIndex - starting page number. If interface is at 20 when next
page is requested, this would be set to 21?
  @PageSize - number of records on a page
  @TotalRows OUTPUT - total available rows. Used by interface to build a
custom pager
  ------------------------------------------------------
Modifications
17-08-01 mparsons - workIT version
*/

			

ALTER PROCEDURE [dbo].[Credential.Search]
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
		,@UsingSummaryCache bit


-- =================================
--set @CurrentUserId = 21
Set @debugLevel = 4
set @HasSitePrivileges= 0

set @UsingSummaryCache = 0

if @SortOrder = 'relevance' set @SortOrder = 'base.Name '
else if @SortOrder = 'alpha' set @SortOrder = 'base.Name '
else if @SortOrder = 'org_alpha' set @SortOrder = 'OwningOrganization, base.Name '
--else if @SortOrder = 'cost_highest' set @SortOrder = 'costs.TotalCost DESC'
--else if @SortOrder = 'cost_lowest' set @SortOrder = 'costs.TotalCost'

else if @SortOrder = 'duration_shortest' set @SortOrder = 'duration.AverageMinutes '
else if @SortOrder = 'duration_longest' set @SortOrder = 'duration.AverageMinutes DESC'

else if @SortOrder = 'newest' set @SortOrder = 'base.lastUpdated Desc '
else set @SortOrder = 'base.Name '

if len(@SortOrder) > 0 
      set @OrderBy = ' Order by ' + @SortOrder
else
      set @OrderBy = ' Order by base.Name '

--check for site privileges
--if (exists (
--select RoleId from [dbo].[AspNetUserRoles_Summary] where id = @CurrentUserId And RoleId in (1,2,3)
--))
--	set @HasSitePrivileges = 1

--print '@HasSitePrivileges: ' + convert(varchar, @HasSitePrivileges)
--===================================================
-- Calculate the range
--===================================================
SET @StartPageIndex =  (@StartPageIndex - 1)  * @PageSize
IF @StartPageIndex < 1        SET @StartPageIndex = 1

 
-- =================================
CREATE TABLE #tempWorkTable(
      RowNumber         int PRIMARY KEY IDENTITY(1,1) NOT NULL,
      Id int,
      Title             varchar(200),
			LastUpdated			datetime
			,TotalCost [decimal](9, 2) 
			,AverageDuration int
			,OwningOrganization varchar(300)
			--,RowId	 uniqueidentifier
)

-- =================================

  if len(@Filter) > 0 begin
     if charindex( 'where', @Filter ) = 0 OR charindex( 'where',  @Filter ) > 10
        set @Filter =     ' where ' + @Filter
     end

  print '@Filter len: '  +  convert(varchar,len(@Filter))
	if @UsingSummaryCache = 1 begin
		set @SQL = 'SELECT distinct base.Id, base.Name, base.OwningOrgs as OwningOrganization, base.lastUpdated, costs.TotalCost , duration.AverageMinutes
from [Credential.SummaryCache] b  
Inner join credential base on b.CredentialId = base.Id
--not ideal, but doing a total
--left join (
--					Select ParentEntityUid, sum(isnull(TotalCost, 0)) As TotalCost from Entity_CostProfileTotal group by ParentEntityUid
--					) costs	on base.RowId = costs.ParentEntityUid 

--left join (SELECT [ParentEntityUid] ,sum([AverageMinutes]) as [AverageMinutes] 
--		FROM [dbo].[Entity_Duration_EntityAverage] group by [ParentEntityUid])  duration on base.RowId = duration.ParentEntityUid  
	'
					+ @Filter
				end
	else begin
		set @SQL = 'SELECT distinct base.Id, base.Name, base.OwningOrganization, base.lastUpdated, 0,0 
		--costs.TotalCost , duration.AverageMinutes
					from [Credential_Summary] base  
					--left join Entity_CostProfileTotal costs on base.EntityUid = costs.ParentEntityUid 
					--not ideal, but doing a total
					left join (
					Select ParentEntityUid, sum(isnull(TotalCost, 0)) As TotalCost from Entity_CostProfileTotal group by ParentEntityUid
					) costs	on base.EntityUid = costs.ParentEntityUid

					left join (SELECT [ParentEntityUid] ,sum([AverageMinutes]) as [AverageMinutes] 
		FROM [dbo].[Entity_Duration_EntityAverage] group by [ParentEntityUid])  duration on base.EntityUid = duration.ParentEntityUid 
	'
        + @Filter
		end
--, AverageMinutes 
----left join [Entity_Duration_EntityAverage] duration on base.EntityUid = duration.ParentEntityUid 
--				
        
  if charindex( 'order by', lower(@Filter) ) = 0
    set @SQL = @SQL + ' ' + @OrderBy

  print '@SQL len: '  +  convert(varchar,len(@SQL))
  print @SQL

  INSERT INTO #tempWorkTable (Id, Title, OwningOrganization, LastUpdated, TotalCost, AverageDuration)
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
SELECT        Distinct
	RowNumber, 
	work.Id, 
	cred.Name, 
	base.EntityUid,
	cred.Description, cred.SubjectWebpage,
	base.OwningAgentUid,
	--base.OwningOrganizationId,
	--if still a placeholder, skip
	case when Charindex('Placeholder', Isnull(org.Name, '')) = 1 then 0
	else base.OwningOrganizationId end  as OwningOrganizationId,
	case when Charindex('Placeholder', Isnull(org.Name, '')) = 1 then ''
	else org.Name end  as OwningOrganization,
	

	cred.EffectiveDate,
	cred.Version,
	cred.LatestVersionUrl,

	cred.ReplacesVersionUrl As PreviousVersion,

	cred.CTID, 
	cred.CredentialRegistryId,
	cred.availableOnlineAt,
	cred.CredentialId,
	cred.Created, 
  cred.LastUpdated, 
	-- ======================================================
	base.EntityId,
	base.CredentialType,
	base.CredentialTypeId,
	base.CredentialTypeSchema,
	base.IsAQACredential,
	base.HasQualityAssurance,

		--creator org
	--base.CreatorOrgs,
	'' as CreatorOrgs,
	--base.OwningOrgs
	'' as OwningOrgs
	,base.OfferingOrgs

	,base.AssessmentsCompetenciesCount
	,base.LearningOppsCompetenciesCount
	,base.QARolesCount

	,base.HasPartCount
	,base.IsPartOfCount
	,0 as RenewalCount
	,base.entryConditionCount
	--credential connection types/counts
	,base.RequiresCount			--messy as could be condition or connection!--	
	,base.RecommendsCount		--messy as could be condition or connection!--
		
	,base.RequiredForCount			as isRequiredForCount
	,base.IsRecommendedForCount	
	,base.IsAdvancedStandingForCount
	,base.AdvancedStandingFromCount
	,base.PreparationForCount		as isPreparationForCount
	,base.PreparationFromCount	as isPreparationFromCount


	--== candidates
	,isnull(base.totalCost,0) As TotalCost,
	--isnull(costProfiles.Total,0) as NumberOfCostProfiles,
	base.NumberOfCostProfileItems,
	base.AverageMinutes,
	

	--isnull(props.properties,'') As Properties,
	isnull(base.NaicsList,'') As NaicsList,
	'' As OtherIndustriesList,

	isnull(	base.LevelsList,'') As LevelsList,

	isnull(base.OccupationsList,'') As OccupationsList,
	'' As OtherOccupationsList,

	--17-05-04 mp - these were added to Credential.SummaryCache and joined in summary 
	isnull(base.QARolesList,'') As QARolesList,
	isnull(base.QAOrgRolesList,'') As QAOrgRolesList,
		isnull(base.QAAgentAndRoles,'') As QAAgentAndRoles,
	isnull(base.AgentAndRoles,'') As AgentAndRoles,

	isnull(base.SubjectsList,'') As SubjectsList,
	--this may be obsolete now - that is it should be moved to the summaries or cache?
	isnull(base.ConnectionsList,'') As ConnectionsList,
	isnull(base.CredentialsList,'') As CredentialsList,
	isnull(badgeClaimsCount, 0) as badgeClaimsCount
	,base.AvailableAddresses
	,base.HasPartList as HasPartsList
	,base.IsPartOfList
	-- ======================================================


From #tempWorkTable work
	--Inner join Credential_Summary base on work.Id = base.Id
	Inner join [Credential.SummaryCache] base on work.Id = base.CredentialId
		Inner join Credential cred on work.Id = cred.Id
	  left join [Organization] org on base.OwningAgentUid = org.RowId

  --Inner Join Entity e on base.EntityUid = e.EntityUid

 -- left Join (select EntityId, count(*) as nbr from [Entity.Address] group by EntityId ) ea on base.EntityId = ea.EntityId

 -- left join [Entity.NaicsCSV] naicsCsv					on base.EntityId = naicsCsv.EntityId
	--left join [Entity.OccupationsCSV] occsCsv			on base.EntityId = occsCsv.EntityId
 -- left join EntityProperty_EducationLevelCSV levelsCsv on base.EntityId = levelsCsv.EntityId
--17-05-04 mp - these were added to Credential.SummaryCache and joined in summary 
	--left join [Credential.QARolesCSV] qaRolesCsv	on work.id = qaRolesCsv.CredentialId

	--left join Credential_ConditionProfilesCSV connectionsCsv on work.id = connectionsCsv.CredentialId
	--left join [Entity_SubjectsCSV] subjectsCsv		on base.EntityUid = subjectsCsv.EntityUid

	-- ========== check for a verifiable badge claim ========== 
	--Left Join (SELECT c.CredentialId, count(*) as Total
	--	FROM [Entity.VerificationProfile] a
	--	inner join entity vpEntity							on a.RowId = vpEntity.EntityUid
	--	inner join  [dbo].[Entity.Credential] c on vpEntity.Id = c.EntityId
	--	inner join  [dbo].[Entity.Property] ep  on vpEntity.Id = ep.EntityId
	--	inner join [Codes.PropertyValue] b			on ep.PropertyValueId = b.Id
	--	where 	b.SchemaName = 'claimType:BadgeClaim'
	--	group by c.CredentialId

	--) badgeClaims on base.Id = badgeClaims.CredentialId

	--not ideal, but doing a total
	--left join (
	--Select ParentEntityUid, sum(isnull(TotalCost, 0)) As TotalCost from Entity_CostProfileTotal group by ParentEntityUid
	--) costs				
	--	on base.EntityUid = costs.ParentEntityUid

-- ========== total cost items - just for credential, no child items ========== 
	--left join (
	--				Select ParentEntityUid, Count(*) As Total from Entity_CostProfileTotal group by ParentEntityUid
	--				) costProfiles	on base.EntityUid = costProfiles.ParentEntityUid

-- ========== total cost items - just for credential, AND child items ========== 
	--left join (
	--				Select condProfParentEntityBaseId, Sum(TotalCostItems) As Total from [CostProfile_SummaryForSearch] 
	--				where condProfParentEntityTypeId =1  and TotalCostItems > 0
	--				group by condProfParentEntityBaseId
	--				) allCostProfiles	on base.Id = allCostProfiles.condProfParentEntityBaseId

-- =======================================================
--left join (SELECT [ParentEntityUid] ,sum([AverageMinutes]) as [AverageMinutes] 
--  FROM [dbo].[Entity_Duration_EntityAverage] group by [ParentEntityUid])  duration on base.EntityUid = duration.ParentEntityUid 

-- =========================================================
WHERE RowNumber > @first_id

order by RowNumber 

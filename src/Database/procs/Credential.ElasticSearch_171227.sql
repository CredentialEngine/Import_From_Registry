USE [credFinder]
GO
/****** Object:  StoredProcedure [dbo].[Credential.ElasticSearch]    Script Date: 1/2/2018 7:28:05 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


ALTER PROCEDURE [dbo].[Credential.ElasticSearch] 
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
	  
--set @CurrentUserId = 21
Set @debugLevel = 4
set @HasSitePrivileges= 0
-- probably will never use cache for a build
--unless we should always ensure cache sources are updated before a build???
set @UsingSummaryCache = 0

if @SortOrder = 'relevance' set @SortOrder = 'base.Name '
else if @SortOrder = 'alpha' set @SortOrder = 'base.Name '
else if @SortOrder = 'id' set @SortOrder = 'base.Id '
else if @SortOrder = 'cost_highest' set @SortOrder = 'costs.TotalCost DESC'
else if @SortOrder = 'cost_lowest' set @SortOrder = 'costs.TotalCost'
else if @SortOrder = 'duration_shortest' set @SortOrder = 'duration.AverageMinutes '
else if @SortOrder = 'duration_longest' set @SortOrder = 'duration.AverageMinutes DESC'
else if @SortOrder = 'newest' set @SortOrder = 'base.lastUpdated Desc '
else set @SortOrder = 'base.Name '

if len(@SortOrder) > 0 
      set @OrderBy = ' Order by ' + @SortOrder
else
      set @OrderBy = ' Order by base.Name '

SET @StartPageIndex =  (@StartPageIndex - 1)  * @PageSize
IF @StartPageIndex < 1        SET @StartPageIndex = 1

CREATE TABLE #tempWorkTable(
	RowNumber         int PRIMARY KEY IDENTITY(1,1) NOT NULL,
	Id int,
	Title             varchar(200),
	LastUpdated			datetime,
	TotalCost [decimal](9, 2) ,
	AverageDuration int
			--,RowId	 uniqueidentifier
)

  if len(@Filter) > 0 begin
     if charindex( 'where', @Filter ) = 0 OR charindex( 'where',  @Filter ) > 10
        set @Filter =     ' where ' + @Filter
     end
  

		if @UsingSummaryCache = 1 begin
		set @SQL = 'SELECT distinct base.Id, base.Name, base.lastUpdated, costs.TotalCost , duration.AverageMinutes
from [Credential.SummaryCache] b  
Inner join credential base on b.CredentialId = base.Id
inner join [Credential_Summary] cs  on cs.Id = base.Id
--not ideal, but doing a total
left join (
					Select ParentEntityUid, sum(isnull(TotalCost, 0)) As TotalCost from Entity_CostProfileTotal group by ParentEntityUid
					) costs	on base.RowId = costs.ParentEntityUid 

left join (SELECT [ParentEntityUid] ,sum([AverageMinutes]) as [AverageMinutes] 
		FROM [dbo].[Entity_Duration_EntityAverage] group by [ParentEntityUid])  duration on base.RowId = duration.ParentEntityUid  
	'
					+ @Filter
				end
	else begin
		set @SQL = 'SELECT distinct base.Id, base.Name, base.lastUpdated, costs.TotalCost , duration.AverageMinutes
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

  INSERT INTO #tempWorkTable (Id, Title, LastUpdated, TotalCost, AverageDuration)
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
	base.Name, 
	base.AlternateName,
	base.EntityUid,
	base.Description, base.SubjectWebpage,
	base.OwningAgentUid,
		base.OwningOrganizationId,
	base.OwningOrganization,
	--base.ManagingOrgId, managingOrg.Name as ManagingOrganization,
	base.EntityStateId,
	base.EffectiveDate,
	base.Version,
	base.LatestVersionUrl,
	
	base.PreviousVersion,	
	base.CTID, 
	base.CredentialRegistryId,
	base.availableOnlineAt,
	base.Created, 
  base.LastUpdated, 
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
	,base.AssessmentsCompetenciesCount
	,base.LearningOppsCompetenciesCount
	,base.QARolesCount

	,base.HasPartCount
	,base.IsPartOfCount
	,base.RequiresCount
	,base.RecommendsCount
	,base.isRequiredForCount
	,base.IsRecommendedForCount
	,0 as RenewalCount
	,base.IsAdvancedStandingForCount
	,base.AdvancedStandingFromCount
	,base.isPreparationForCount
	,base.isPreparationFromCount
	,base.entryConditionCount,

	--== candidates
	isnull(costs.totalCost,0) As TotalCost,
	--isnull(costProfiles.Total,0) as NumberOfCostProfiles,
	isnull(allCostProfiles.Total,0) as NumberOfCostProfileItems,
	duration.AverageMinutes,
	--isnull(duration.AverageMinutes,0) as AverageMinutes,
	--isnull(duration.FromDuration,'') as FromDuration,
	--isnull(duration.ToDuration,'') as ToDuration,

	--isnull(props.properties,'') As Properties,
	isnull(naicsCsv.naics,'') As NaicsList,
	isnull(naicsCsv.Others,'') As OtherIndustriesList,

	isnull(levelsCsv.Properties,'') As LevelsList,

	isnull(occsCsv.Occupations,'') As OccupationsList,
	isnull(occsCsv.Others,'') As OtherOccupationsList,

	--17-05-04 mp - these were added to Credential.SummaryCache and joined in summary 
	isnull(base.QARolesList,'') As QARolesList,
	isnull(base.QAOrgRolesList,'') As QAOrgRolesList,
	isnull(base.AgentAndRoles,'') As AgentAndRoles,

	--isnull(subjectsCsv.Subjects,'') As SubjectsList,
	--this may be obsolete now - that is it should be moved to the summaries or cache?
	isnull(connectionsCsv.Profiles,'') As ConnectionsList,
	isnull(connectionsCsv.CredentialsList,'') As CredentialsList,
	isnull(badgeClaims.Total, 0) as badgeClaimsCount
	,ea.Nbr as AvailableAddresses
	,base.HasPartList as HasPartsList
	,base.IsPartOfList
	-- ======================================================
	-- For ElasticSearch
	, STUFF((SELECT '|' + ISNULL(NULLIF(a.TextValue, ''), NULL) AS [text()] FROM [dbo].[Entity] e INNER JOIN [dbo].[Entity.SearchIndex] a ON a.EntityId = e.Id where base.RowId = e.EntityUid AND e.EntityTypeId = 1 FOR XML Path('')), 1,1,'') TextValues

	, STUFF((SELECT '|' + ISNULL(NULLIF(a.CodedNotation, ''), NULL) AS [text()] FROM [dbo].[Entity] e INNER JOIN [dbo].[Entity.SearchIndex] a ON a.EntityId = e.Id where base.RowId = e.EntityUid AND e.EntityTypeId = 1 FOR XML Path('')), 1,1,'') CodedNotation

	, STUFF((SELECT '|' + ISNULL(NULLIF(CAST(eps.PropertyValueId AS NVARCHAR(MAX)), ''), NULL) AS [text()] FROM [dbo].[EntityProperty_Summary] eps where eps.EntityTypeId = 1 AND eps.EntityBaseId = base.Id FOR XML Path('')), 1,1,'') PropertyValues
 
	, STUFF((SELECT '|' + ISNULL(NULLIF(CAST(a.[RelationshipTypeId] AS NVARCHAR(MAX)), ''), NULL) AS [text()] FROM [dbo].[Entity.AgentRelationship] a inner join Entity b on a.EntityId = b.Id WHERE b.EntityUid = base.RowId FOR XML Path('')), 1,1,'') RelationshipTypes
	-- Entity_Subjects includes the bubbled up subjects
	, (SELECT a.[Subject], a.[Source], a.EntityTypeId, a.ReferenceBaseId FROM [dbo].[Entity_Subjects] a WHERE a.EntityTypeId = 1 AND a.EntityUid = base.RowId FOR XML RAW, ROOT('Subjects')) Subjects
	--
	, (SELECT a.Latitude, a.Longitude, a.Region, a.Country FROM [dbo].[Entity.Address] a inner join Entity b on a.EntityId = b.Id WHERE a.Latitude <> 0 AND b.EntityTypeId = 1 AND b.EntityUid = base.RowId FOR XML RAW, ROOT('Addresses')) Addresses

	, (SELECT ccs.[Name], ccs.[TargetNodeDescription] [Description] FROM [dbo].[ConditionProfile_Competencies_Summary] ccs where ccs.CredentialId = base.Id FOR XML RAW, ROOT('Competencies')) Competencies

	,(SELECT a.[CategoryId], a.[ReferenceFrameworkId], a.[Name], a.[SchemaName], ISNULL(NULLIF(a.[CodeGroup], ''), NULL) CodeGroup FROM [dbo].[Entity_ReferenceFramework_Summary] a where a.[CategoryId] IN (10, 11) AND a.[EntityId] = base.[EntityId] FOR XML RAW, ROOT('Frameworks')) Frameworks
	
	--,(SELECT a.[CodeGroup], 'a' as [CodedNotation] FROM [dbo].[Entity.FrameworkItemSummary] a where a.[CategoryId] = 10 AND a.EntityId = base.EntityId FOR XML RAW, ROOT('Industries')) Industries

  --( base.EntityStateId = 3 )  AND  (   (base.RowId in (SELECT EntityUid FROM [Entity_Subjects] a where EntityTypeId = 1 AND  (a.Subject like '%PHY%221	UNIVERSITY%PHYSICS%I	%4%sh%')  )) )
 
From #tempWorkTable work
	Inner join Credential_Summary base on work.Id = base.Id
	--Inner join Credential_Summary_Cache base on work.Id = base.Id

	left Join (select EntityId, count(*) as nbr from [Entity.Address] group by EntityId ) ea on base.EntityId = ea.EntityId

	left join [Entity.NaicsCSV] naicsCsv					on base.EntityId = naicsCsv.EntityId
	left join [Entity.OccupationsCSV] occsCsv			on base.EntityId = occsCsv.EntityId
	left join EntityProperty_EducationLevelCSV levelsCsv	on base.EntityId = levelsCsv.EntityId
--17-05-04 mp - these were added to Credential.SummaryCache and joined in summary 
	--left join [Credential.QARolesCSV] qaRolesCsv	on work.id = qaRolesCsv.CredentialId

	left join Credential_ConditionProfilesCSV connectionsCsv on work.id = connectionsCsv.CredentialId
	--left join [Entity_SubjectsCSV] subjectsCsv		on base.EntityUid = subjectsCsv.EntityUid

	-- ========== check for a verifiable badge claim ========== 
	Left Join (SELECT c.CredentialId, count(*) as Total
		FROM [Entity.VerificationProfile] a
		inner join entity vpEntity							on a.RowId = vpEntity.EntityUid
		inner join  [dbo].[Entity.Credential] c on vpEntity.Id = c.EntityId
		inner join  [dbo].[Entity.Property] ep  on vpEntity.Id = ep.EntityId
		inner join [Codes.PropertyValue] b			on ep.PropertyValueId = b.Id
		where 	b.SchemaName = 'claimType:BadgeClaim'
		group by c.CredentialId

	) badgeClaims on base.Id = badgeClaims.CredentialId

	--not ideal, but doing a total
	left join (
	Select ParentEntityUid, sum(isnull(TotalCost, 0)) As TotalCost from Entity_CostProfileTotal group by ParentEntityUid
	) costs				
		on base.EntityUid = costs.ParentEntityUid

-- ========== total cost items - just for credential, no child items ========== 
	--left join (
	--				Select ParentEntityUid, Count(*) As Total from Entity_CostProfileTotal group by ParentEntityUid
	--				) costProfiles	on base.EntityUid = costProfiles.ParentEntityUid

-- ========== total cost items - just for credential, AND child items ========== 
	left join (
					Select condProfParentEntityBaseId, Sum(TotalCostItems) As Total from [CostProfile_SummaryForSearch] 
					where condProfParentEntityTypeId =1  and TotalCostItems > 0
					group by condProfParentEntityBaseId
					) allCostProfiles	on base.Id = allCostProfiles.condProfParentEntityBaseId

-- =======================================================
left join (SELECT [ParentEntityUid] ,sum([AverageMinutes]) as [AverageMinutes] 
  FROM [dbo].[Entity_Duration_EntityAverage] group by [ParentEntityUid])  duration on base.EntityUid = duration.ParentEntityUid 

-- =========================================================
WHERE RowNumber > @first_id 
--and (base.RequiresCount  > 0 or base.RecommendsCount > 0)

order by RowNumber 

/*
exec [dbo].[Credential.ElasticSearch] ' ( base.EntityStateId = 3 ) ','',0,0,0
*/


USE [credFinder]
GO

--use flstaging_credfinder
--go
--use staging_credFinder
--go
use sandbox_credFinder
go

/****** Object:  StoredProcedure [dbo].[LearningOpportunity.ElasticSearch]    Script Date: 1/22/2018 4:26:03 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/*

SELECT [Id]
      ,[RowId]
      ,[Name]
      ,[Description]
      ,[OrgId]
      ,[Organization]
      ,[OwningOrganizationCtid]
      ,[OwningAgentUid]
      ,[SubjectWebpage]
      ,[DateEffective]
      ,[EntityStateId]
      ,[CTID]
      ,[CredentialRegistryId]
      ,[cerEnvelopeUrl]
      ,[Created]
      ,[LastUpdated]
      ,[IdentificationCode]
      ,[availableOnlineAt]
      ,[AvailabilityListing]
      ,[RequiresCount]
      ,[RecommendsCount]
      ,[isRequiredForCount]
      ,[IsRecommendedForCount]
      ,[IsAdvancedStandingForCount]
      ,[AdvancedStandingFromCount]
      ,[isPreparationForCount]
      ,[PreparationFromCount]
      ,[ConnectionsList]
      ,[CredentialsList]
      ,[Org_QAAgentAndRoles]
  FROM [dbo].[LearningOpportunity_Summary]
GO




--=====================================================

DECLARE @RC int,@SortOrder varchar(100),@Filter varchar(5000)
DECLARE @StartPageIndex int, @PageSize int, @TotalRows int
--

set @SortOrder = 'newest'
set @SortOrder = 'cost_highest'
set @SortOrder = 'cost_lowest'
set @SortOrder = 'alpha'
--set @SortOrder = 'duration_shortest'
--set @SortOrder = 'duration_longest'

-- blind search 

--set @Filter = '  (base.name like ''%western gov%'' OR base.Description like ''%western gov%''  OR base.Organization like ''%western gov%''   OR base.Url like ''%western gov%'') '

--set @Filter = ' (base.name like ''%western%'' OR base.Description like ''%western%''  OR base.Organization like ''%western%''  )'
	
--set @Filter = '  (base.Id in (SELECT c.id FROM [dbo].[Entity.FrameworkItemSummary] a inner join Entity b on a.EntityId = b.Id inner join LearningOpportunity c on b.EntityUid = c.RowId where [CategoryId] = 23 and ([CodeGroup] in ('''')  OR ([CodeId] in ('''') ) ) ) )  '
set @Filter = '  (base.Id = 14856) '

set @Filter = '  (base.Id in  (select baseId from  [dbo].[Entity_Cache] where OwningOrgId = 34 and EntityTypeId = 7 AND LifeCycleStatusType =''Ceased'')) '

--drop table LearningOpportunity_IndexBuild

set @Filter = ' EntityStateId > 1'
set @StartPageIndex = 1
set @PageSize = 500
--set statistics time on       
EXECUTE @RC = [LearningOpportunity.ElasticSearch]
     @Filter,@SortOrder  ,@StartPageIndex  ,@PageSize, @TotalRows OUTPUT

select 'total rows = ' + convert(varchar,@TotalRows)

--set statistics time off       


<QualityAssurance><row RelationshipTypeId="2" SourceToAgentRelationship="Approved By" AgentToSourceRelationship="Approves" AgentRelativeId="209" AgentName="TESTING_(ISC)²" EntityStateId="3"/><row RelationshipTypeId="7" SourceToAgentRelationship="Offered By" AgentToSourceRelationship="Offers" AgentRelativeId="209" AgentName="TESTING_(ISC)²" EntityStateId="3"/></QualityAssurance>

<Connections><row ConnectionTypeId="6" ConnectionType="Advanced Standing For" CredentialId="1027" CredentialName="TESTING_Bachelor of Science in Nursing" AssessmentId="0" LearningOpportunityId="0"/></Connections>
*/


/* =============================================
Description:      LearningOpportunity search for elastic load
Options:

  @StartPageIndex - starting page number. 
  @PageSize - number of records on a page
  @TotalRows OUTPUT - total available rows. Used by interface to build a
custom pager
  ------------------------------------------------------
Modifications
18-01-22 mparsons - created from existing search
18-10-06 mparsons - removed [IsQARole]= 1 from QualityAssurance, so that owned and offered by can be filtered. The actual property name should also be changed now! 
21-01-04 mparsons - change the workflow to use a temp table for target data and then do the joins
21-05-26 mparsons - this proc is very slow to start from elastic build (full). Why?
					- maybe try: SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED
					- this seemed to help. The lopp build in prod was stuck for 20+ minutes. 
					  It started almost immediately after add the latter!
21-10-14 mparsons - what was the purpose of LearningOpportunity_IndexBuild - not fully implemented??
			- a possible rational is to build the latter prior to calling a rebuild. Sometimes the console app just sits with no response where the proc will run , albeit slowly in 20 sec. 
			*****
			Approach is to uncomment line below
			--into LearningOpportunity_IndexBuild
			--drop table LearningOpportunity_IndexBuild (since recreated above)
23-05-16 mparsons - we are experiencing terrible performance for the property: AgentRelationshipsForEntity
			- changed the approach to:
				- on import call a method that will get the AgentRelationshipsForEntity and cache it in Entity_cache.AgentRelationshipsForEntity
				- then the lopp elastic search can get it by joining on entity cache, saving a huge hit
			- planning on doing the same for credentials, even though we don't see as huge a hit!
			-hmm maybe a custom version of Entity.AgentRelationshipIdCSV would help
24-03-07 sneha - Provides TransferValue For and Receives TransferValue From Tags to search 
======================================
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LearningOpportunity_IndexBuild]') AND type in (N'U'))
DROP TABLE [dbo].[LearningOpportunity_IndexBuild]
GO


			 to populate using:
				set @StartPageIndex = 1
				set @PageSize = 10000
			 Then run [LearningOpportunity.ElasticSearchV2]
*/

--exec [dbo].[LearningOpportunity.ElasticSearch] '', '', 0,0,0
ALTER PROCEDURE [dbo].[LearningOpportunity.ElasticSearch]
		@Filter           varchar(5000)
		,@SortOrder       varchar(100)
		,@StartPageIndex  int
		,@PageSize        int
		,@TotalRows       int OUTPUT

As

SET NOCOUNT ON;
-- paging
DECLARE
	@first_id  int
	,@startRow int
	,@lastRow int
	,@debugLevel      int
	,@SQL             varchar(5000)
	,@OrderBy         varchar(100)
	,@HasSitePrivileges bit

-- =================================

Set @debugLevel = 4
set @HasSitePrivileges= 0

print '@SortOrder ' + @SortOrder
if @SortOrder = 'relevance' set @SortOrder = 'base.Name '
else if @SortOrder = 'alpha' set @SortOrder = 'base.Name '
else if @SortOrder = 'org_alpha' set @SortOrder = 'base.Organization, base.Name '
else if @SortOrder = 'oldest' set @SortOrder = 'base.Id'
else if @SortOrder = 'newest' set @SortOrder = 'base.lastUpdated Desc '
--else if @SortOrder = 'cost_highest' set @SortOrder = 'costs.TotalCost DESC'
--else if @SortOrder = 'cost_lowest' set @SortOrder = 'costs.TotalCost'
else set @SortOrder = 'base.Name '

if len(@SortOrder) > 0 
      set @OrderBy = ' Order by ' + @SortOrder
else
      set @OrderBy = ' Order by base.lastUpdated Desc '

--TODO maybe just default to the latest, with full first
--set @OrderBy = 'base.EntityStateId desc , base.lastUpdated Desc '

-- nolock testing @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED

--===================================================
-- Calculate the range
--===================================================
if @PageSize < 1				set @PageSize = 1000
if @PageSize < 1				set @PageSize = 1000
IF @StartPageIndex < 1			SET @StartPageIndex = 1
SET @StartPageIndex =  ((@StartPageIndex - 1)  * @PageSize) + 1
SET @lastRow =  (@StartPageIndex + @PageSize) - 1
PRINT '@StartPageIndex = ' + convert(varchar,@StartPageIndex) +  ' @lastRow = ' + convert(varchar,@lastRow)
 
-- =================================
CREATE TABLE #tempWorkTable(
      RowNumber         int PRIMARY KEY  NOT NULL,
      Id int,
      Name             varchar(500)
	,Organization varchar(500)
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
set @SQL = '   SELECT count(*) as TotalRows  FROM [dbo].LearningOpportunity_Summary base  '  + @Filter 
INSERT INTO #tempQueryTotalTable (TotalRows)
exec (@SQL)
--select * from #tempQueryTotalTable
select top 1  @TotalRows= TotalRows from #tempQueryTotalTable
  print '@TotalRows '  +  convert(varchar,@TotalRows)
--====
  set @SQL = ' 
  SELECT        
		DerivedTable.RowNumber, 
		base.Id
		,base.[Name], base.Organization
From ( SELECT 
         ROW_NUMBER() OVER(' + @OrderBy + ') as RowNumber,
          base.Id, base.Name, base.Organization
		from [LearningOpportunity_Summary] base  ' 
        + @Filter + ' 
   ) as DerivedTable
       Inner join [dbo].[LearningOpportunity_Summary] base on DerivedTable.Id = base.Id
WHERE RowNumber BETWEEN ' + convert(varchar,@StartPageIndex) + ' AND ' + convert(varchar,@lastRow) + ' '  

  print '@SQL len: '  +  convert(varchar,len(@SQL))
  print @SQL
  INSERT INTO #tempWorkTable (RowNumber, Id, Name, Organization)
  exec (@SQL)

--select * from #tempWorkTable

--=====old ===============================================
/*
  set @SQL = 'SELECT  base.Id, base.Name , base.Organization  
        from [LearningOpportunity_Summary] base 
				--left join Entity_CostProfileTotal costs on base.RowId = costs.ParentEntityUid   
				--not ideal, but doing a total
					--left join (
					--Select ParentEntityUid, sum(isnull(TotalCost, 0)) As TotalCost from Entity_CostProfileTotal group by ParentEntityUid
					--) costs	on base.RowId = costs.ParentEntityUid 
				'
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
*/
-- ================================= 
SELECT        
	RowNumber, 
	base.LearningEntityTypeId,

	base.id, 
	base.EntityId,
	base.CTID,
	base.RowId,
	base.EntityStateId,
	base.Name, 
	isnull(base.Description,'') As Description
	--[OrgId],base.Organization,
	,case when Charindex('Placeholder', Isnull(base.Organization, '')) = 1 then 0
	else [OrgId] end  as [OrgId]
	,case when Charindex('Placeholder', Isnull(base.Organization, '')) = 1 then ''
	else base.Organization end  as [Organization]
	,base.OwningOrganizationCtid
	 
	,isnull(base.SubjectWebpage,'') As SubjectWebpage

	,[DateEffective]
    ,isnull(base.[IdentificationCode],'') As [IdentificationCode]

	,base.availableOnlineAt
	,base.AvailabilityListing
	,base.Created
	,base.LastUpdated 
	--,e.LastUpdated As EntityLastUpdated
	,ec.CacheDate As EntityLastUpdated
	,ec.ResourceDetail
	--23-05-16 start getting from entity_cache. May require a process to ensure all the cache for all lopp has been populate
	,ec.AgentRelationshipsForEntity

	,base.LifeCycleStatusType
	,base.LifeCycleStatusTypeId
	,base.ConnectionsList			--actual connection type (no credential info)
	,base.CredentialsList	--connection type, plus Id, and name of credential
	,base.IsNonCredit
	-- ====================================
	,base.RequiresCount
	,base.RecommendsCount
	,base.isRequiredForCount
	,base.IsRecommendedForCount
	,base.IsAdvancedStandingForCount
	,base.AdvancedStandingFromCount
	,base.isPreparationForCount
	,base.PreparationFromCount


	-- 20-10-10 mp - added number of cost profile items. Monitoring performance.	
	,Isnull(allCostProfiles.Total,0) as NumberOfCostProfileItems
	,IsNULL(costProfiles.Nbr, 0) As CostProfilesCount
	,IsNULL(conditionProfiles.Nbr, 0) As ConditionProfilesCount
	--remove these
	--,IsNULL(costs.TotalCost, 0) As TotalCostCount
	,0 As TotalCostCount
	,IsNull(CommonCost.Nbr,0) As CommonCostsCount
	,IsNull(CommonCondition.Nbr,0) As CommonConditionsCount
	,IsNULL(FinancialAid.Nbr, 0) As FinancialAidCount
	--lopp doesn't have opp
	--,IsNULL(processProfiles.Nbr, 0) As ProcessProfilesCount
	--TBD remove later
	,0 as ProcessProfilesCount
	--,0 As AggregateDataProfileCount
	,IsNull(aggregateDataProfile.Nbr,0) As AggregateDataProfileCount
	,IsNull(dataSetProfile.Nbr,0)		As DataSetProfileCount
	,IsNull(tvProfile.Nbr,0)			As HasTransferValueProfileCount
	--	,0 As HasTransferValueProfileCount
	
	,IsNULL(HasCIP.Nbr, 0) As HasCIPCount
	,IsNULL(HasDuration.Nbr, 0) As HasDurationCount	
	
	,(SELECT DISTINCT ConnectionTypeId ,ConnectionType  ,AssessmentId, isnull(AssessmentName,'') As AssessmentName,   CredentialId, IsNUll(CredentialName,'') As CredentialName, LearningOpportunityId, Isnull(LearningOpportunityName,'') As LearningOpportunityName, credOrgid,credOrganization, asmtOrgid, asmtOrganization, loppOrgid, loppOrganization  
		FROM [dbo].[Entity_ConditionProfilesConnectionsSummary]  WHERE EntityTypeId = 7
		AND EntityBaseId = base.id 
		FOR XML RAW, ROOT('Connections')) LoppConnections

	--,isnull(comps.Nbr,0) as Competencies
	,0 as CompetenciesCount
	,base.CredentialRegistryId

	--23-05-17: adds 2 sec. 
	--just keywords and codedNotation?
	 ,(SELECT ISNULL(NULLIF(a.TextValue, ''), NULL) TextValue, a.[CodedNotation], a.CategoryId FROM [dbo].[Entity.SearchIndex] a 
		inner join Entity b on a.EntityId = b.Id 
		inner join LearningOpportunity c on b.EntityUid = c.RowId 
		WHERE b.EntityTypeId = 7 AND c.Id = base.Id FOR XML RAW, ROOT('TextValues')) TextValues

	--23-05-17: the following three properties have no impact on length of query
	,(SELECT DISTINCT lcs.[Name], lcs.[Description] FROM [dbo].LearningOpportunity_Competency_Summary lcs  where lcs.LearningOpportunityId = base.Id AND lcs.AlignmentType = 'teaches' FOR XML RAW, ROOT('TeachesCompetencies')) TeachesCompetencies

	,(SELECT DISTINCT crc.TargetNodeName As Name FROM [dbo].[ConditionProfile_RequiredCompetencies] crc  where crc.ParentEntityBaseId = base.Id AND crc.ParentEntityTypeId = 7 FOR XML RAW, ROOT('RequiresCompetencies')) RequiresCompetencies
	
	,(SELECT DISTINCT lcs.[Name], lcs.[Description] FROM [dbo].LearningOpportunity_Competency_Summary lcs  
		where lcs.LearningOpportunityId = base.Id AND lcs.AlignmentType = 'assesses' FOR XML RAW, ROOT('AssessesCompetencies')) 
		AssessesCompetencies

--23-05-17: no impact
	,(SELECT DISTINCT a.[Subject] FROM [Entity_Subjects] a where EntityTypeId = 7 AND a.EntityUid = base.RowId FOR XML RAW, ROOT('SubjectAreas')) SubjectAreas

	,(SELECT a.[CategoryId], a.[ReferenceFrameworkItemId], a.[Name], a.[SchemaName], ISNULL(NULLIF(a.[CodeGroup], ''), NULL) CodeGroup, a.[CodedNotation] FROM [dbo].[Entity_ReferenceFramework_Summary] a inner join Entity b on a.EntityId = b.Id inner join LearningOpportunity c on b.EntityUid = c.RowId where a.[CategoryId] = 23 AND c.[Id] = base.[Id] FOR XML RAW, ROOT('Classifications')) Classifications 


	,(SELECT a.[CategoryId], a.[ReferenceFrameworkItemId], a.[Name], a.[SchemaName], ISNULL(NULLIF(a.[CodeGroup], ''), NULL) CodeGroup, a.[CodedNotation] FROM [dbo].[Entity_ReferenceFramework_Summary] a inner join Entity b on a.EntityId = b.Id inner join LearningOpportunity c on b.EntityUid = c.RowId where a.[CategoryId] IN (10, 11, 23) AND c.[Id] = base.[Id] FOR XML RAW, ROOT('Frameworks')) Frameworks 

--23-05-17: no impact
	 , ( SELECT DISTINCT a.CategoryId, a.[PropertyValueId], a.Property, PropertySchemaName  
			FROM [dbo].[EntityProperty_Summary] a 
			where EntityTypeId = 7 
			AND CategoryId IN ( 4, 14, 21, 53) AND base.Id = [EntityBaseId] 
		FOR XML RAW, ROOT('LoppProperties')) LoppProperties

 	--, ( SELECT DISTINCT a.CategoryId, a.TextValue FROM [dbo].[Entity.Reference] a inner join [Entity] b on a.EntityId = b.Id where b.EntityTypeId= 7 AND a.CategoryId = 65 AND base.Id = b.[EntityBaseId] FOR XML RAW, ROOT('Languages')) Languages
	,isnull(Languages.Languages,'') As Languages	
	--widget selection
 	, (SELECT a.WidgetId, a.WidgetSection  FROM [dbo].[Widget.Selection] a where a.EntityTypeId = 7 AND a.RecordId = base.[Id] 
		FOR XML RAW, ROOT('WidgetTags')
	) ResourceForWidget

	----transfer value member
	----24-02-18 mp - noted that this is  not being populated. Plus seems duplicate from use of tvProfile
 --	, (SELECT a.TransferValueProfileId, c.Name as TransferValueProfile  FROM [dbo].[Entity.TransferValueProfile] a Inner Join Entity b on a.EntityId = b.Id and b.EntityUid = base.RowId inner join TransferValueProfile c on a.TransferValueProfileId = c.Id where b.entityTypeId=7 and b.entityUID = base.RowId
	--	FOR XML RAW, ROOT('TransferValueReference')
	--) TransferValueReference
	,'' as TransferValueReference

	--collection member
 	, (SELECT a.CollectionId, b.Name as Collection  FROM [dbo].[Collection.CollectionMember] a Inner Join Collection b on a.CollectionId = b.Id where a.ProxyFor = base.CTID
		FOR XML RAW, ROOT('CollectionMembers')
	) CollectionMembers
		-----------------================================TRANSFERVALUES-===================================== Using resource detail
	--,(SELECT ehrs.Name,ehrs.ResourceId,e.Id  FROM [Entity.HasResourceSummary] ehrs Inner Join Entity e	on base.RowId = e.EntityUid
	--WHERE ehrs.[EntityTypeId] = 26 AND ehrs.EntityId = e.Id and [RelationshipTypeId]=15
	--	FOR XML RAW, ROOT('ProvidesTransferValueFor')
	--	) ProvidesTransferValueFor,

 --   (SELECT ehrs.Name,ehrs.ResourceId FROM [Entity.HasResourceSummary] ehrs Inner join Entity b ON base.RowId = b.EntityUid
	--WHERE ehrs.[EntityTypeId] = 26 AND ehrs.EntityId = b.Id and [RelationshipTypeId]=16
	--FOR XML RAW, ROOT('ReceivesTransferValueFrom')) ReceivesTransferValueFrom
	--	-----====================================ACTION
	--   , (SELECT ehrs.Name,ehrs.ResourceId FROM [Entity.HasResourceSummary] ehrs Inner join Entity b ON base.RowId = b.EntityUid
	--WHERE ehrs.[EntityTypeId] = 22 AND ehrs.EntityId = b.Id and [RelationshipTypeId]=1
	--FOR XML RAW, ROOT('ObjectOfAction')) ObjectOfAction
	--=== QA ==============================
	,base.Org_QAAgentAndRoles
	--this is incorrect, it is all relationships should use RelationshipTypes for consistency
	--renamed from QualityAssurances
	--this should be obsolete
	--,( SELECT DISTINCT a.[RelationshipTypeId] FROM [dbo].[Entity.AgentRelationship] a inner join Entity b on a.EntityId = b.Id where base.RowId = b.EntityUid FOR XML RAW, ROOT('RelationshipTypes')) AgentRelationships  
	--now obsolete
	,'' as AgentRelationships
	,'' as QualityAssurances

--23-05-17: BIG impact +16 seconds, which seems wrong. the view is fast by itself
--	start using from Entity_Cache
/*	*/
	--all entity to organization relationships with org information. 
	 --,(SELECT DISTINCT ear.AgentRelativeId As OrgId, ear.AgentName, ear.AgentUrl, ear.EntityStateId, ear.RoleIds as RelationshipTypeIds,  ear.Roles as Relationships, ear.AgentContextRoles FROM [dbo].[Entity.AgentRelationshipIdCSV] ear
		--	WHERE ear.EntityTypeId = 7 AND ear.EntityBaseId = base.id 
		--	FOR XML RAW, ROOT('AgentRelationshipsForEntity')) 
		--AgentRelationshipsForEntity

	
	--** QA asserted by third part, not owner
	 ,(SELECT DISTINCT OrgId, Organization as AgentName, TargetEntityStateId as EntityStateId, Assertions as RelationshipTypeIds FROM [dbo].Organization_QAPerformedCSV  WHERE [TargetEntityTypeId] = 7 AND  TargetEntityBaseId = base.id 
		FOR XML RAW, ROOT('ThirdPartyQualityAssuranceReceived')) ThirdPartyQualityAssuranceReceived

--23-05-17: about 2 sec.
--	neither addresses 7 sec. Just org addresses 9 sec, just Addresses: 8 sec, both addresses: 6 sec?
	--[IsQARole]= 1 and 
	--21-02-25 mp - removed:	a.EntityBaseName,
	,(SELECT b.RowId, b.Id, b.EntityId, a.EntityUid, a.EntityTypeId, a.EntityBaseId,  b.Id AS EntityAddressId, b.Name, b.IsPrimaryAddress, b.Address1, b.Address2, b.City, b.Region, b.SubRegion, b.PostOfficeBoxNumber, b.PostalCode, b.Country, b.Latitude, b.Longitude, b.IdentifierJson, b.Created, b.LastUpdated FROM dbo.Entity AS a INNER JOIN dbo.[Entity.Address] AS b ON a.Id = b.EntityId where a.[EntityUid] = base.[RowId] FOR XML RAW, ROOT('Addresses')) Addresses

--23-05-17: no impact.why? When Addresses is removed, then
	-- addresses for owning org - will only be used if there is no address for the credential
	, (SELECT b.RowId, b.Id, b.EntityId, a.EntityUid, a.EntityTypeId, a.EntityBaseId, a.EntityBaseName, b.Id AS EntityAddressId, b.Name, b.IsPrimaryAddress, b.Address1, b.Address2, b.City, b.Region, b.SubRegion, b.PostOfficeBoxNumber, b.PostalCode, b.Country, b.Latitude, b.Longitude, b.IdentifierJson, b.Created, b.LastUpdated FROM dbo.Entity AS a INNER JOIN dbo.[Entity.Address] AS b ON a.Id = b.EntityId where a.[EntityUid] = base.OwningAgentUid
		FOR XML RAW, ROOT('OrgAddresses')) OrgAddresses
/*			*/
--into LearningOpportunity_IndexBuild
From #tempWorkTable work
	Inner join LearningOpportunity_Summary base on work.Id = base.Id
	--Inner Join Entity e on base.RowId = e.EntityUid
	left join Entity_Cache ec on base.RowId = ec.EntityUid
	--another option ==> EVEN SLOWER. but accurate
	--left join 
	--( 
	--	select Id, entityStateId, ctid, LastUpdated 
 --  			,(SELECT DISTINCT ear.AgentRelativeId As OrgId, ear.AgentName, ear.AgentUrl, ear.EntityStateId, ear.RoleIds as RelationshipTypeIds,  ear.Roles as Relationships, ear.AgentContextRoles FROM [dbo].[Entity.AgentRelationshipIdCSV] ear
	--			WHERE ear.EntityTypeId= 7 AND ear.EntityBaseId = base.Id 
	--			FOR XML RAW, ROOT('AgentRelationshipsForEntity')
	--		) AgentRelationshipsForEntity
	--	from LearningOpportunity base 
	--) resource on ec.CTID = resource.CTID


	left join (
	Select b.EntityBaseId, COUNT(*)  As Nbr from [Entity.ConditionProfile] a Inner join Entity b ON a.EntityId = b.Id Where b.EntityTypeId = 7  Group By b.EntityBaseId
	) conditionProfiles	on base.Id = conditionProfiles.EntityBaseId  
	left join (
		Select b.EntityBaseId, COUNT(*)  As Nbr from [Entity.CostProfile] a Inner join Entity b ON a.EntityId = b.Id Where b.EntityTypeId = 7  Group By b.EntityBaseId
	) costProfiles	on base.Id = costProfiles.EntityBaseId  
/* 20-10-10 mp - added number of cost profile items. Monitoring performance.	*/
	left join (
		Select condProfParentEntityBaseId, Sum(TotalCostItems) As Total from [CostProfile_SummaryForSearch] 
		where condProfParentEntityTypeId =7  and TotalCostItems > 0
		group by condProfParentEntityBaseId 
		) allCostProfiles	on work.Id = allCostProfiles.condProfParentEntityBaseId
	--
	left join (
	Select b.EntityBaseId, COUNT(*)  As Nbr from [Entity.CommonCost] a Inner join Entity b ON a.EntityId = b.Id Where b.EntityTypeId = 7  Group By b.EntityBaseId
	) CommonCost	on base.Id = CommonCost.EntityBaseId     
	left join (
	Select b.EntityBaseId, COUNT(*)  As Nbr from [Entity.CommonCondition] a Inner join Entity b ON a.EntityId = b.Id Where b.EntityTypeId = 7  Group By b.EntityBaseId
	) CommonCondition	on base.Id = CommonCondition.EntityBaseId  
	--left join (
	--Select b.EntityBaseId, COUNT(*)  As Nbr from [Entity.FinancialAlignmentProfile] a Inner join Entity b ON a.EntityId = b.Id Where b.EntityTypeId = 7 Group By b.EntityBaseId 
	--) FinancialAid	on base.Id = FinancialAid.EntityBaseId
	left join (
	Select b.EntityBaseId, COUNT(*)  As Nbr from [Entity.FinancialAssistanceProfile] a Inner join Entity b ON a.EntityId = b.Id Where b.EntityTypeId = 7  Group By b.EntityBaseId 
	) FinancialAid	on base.Id = FinancialAid.EntityBaseId  

	--left join (
	--Select b.EntityBaseId, COUNT(*)  As Nbr from [Entity.ProcessProfile] a Inner join Entity b ON a.EntityId = b.Id Where b.EntityTypeId = 7  Group By b.EntityBaseId
	--) processProfiles	on base.Id = processProfiles.EntityBaseId 
	
	left join (
	Select b.EntityBaseId, COUNT(*)  As Nbr from [Entity.ReferenceFramework] a Inner join Entity b ON a.EntityId = b.Id Where b.EntityTypeId = 7  Group By b.EntityBaseId 
	) HasCIP	on base.Id = HasCIP.EntityBaseId  
	left join (
	Select b.EntityBaseId, COUNT(*)  As Nbr from [Entity.DurationProfile] a Inner join Entity b ON a.EntityId = b.Id Where b.EntityTypeId = 7  Group By b.EntityBaseId 
	) HasDuration	on base.Id = HasDuration.EntityBaseId  
	--left join EntityProperty_AudienceTypeCSV typesCsv on base.Id = typesCsv.EntityId
	left Join ( 
		SELECT     distinct base.Id, 
		CASE WHEN Languages IS NULL THEN '' WHEN len(Languages) = 0 THEN '' ELSE left(Languages,len(Languages)-1) END AS Languages
		From dbo.LearningOpportunity base
		CROSS APPLY ( SELECT a.Title + '| ' + a.TextValue + '| '
			FROM [dbo].[Entity.Reference] a inner join [Entity] b on a.EntityId = b.Id 
			where b.EntityTypeId= 7 AND a.CategoryId = 65
			and base.Id = b.EntityBaseId FOR XML Path('')  ) D (Languages)
		where Languages is not null
	) Languages on base.Id = Languages.Id
--=========
left join (
		Select b.EntityBaseId, COUNT(*)  As Nbr from [Entity.AggregateDataProfile] a Inner join Entity b ON a.EntityId = b.Id Where b.EntityTypeId = 7  Group By b.EntityBaseId
		) aggregateDataProfile	on work.Id = aggregateDataProfile.EntityBaseId 
left join (
		Select c.LearningOpportunityId, COUNT(*)  As Nbr 
		from [DataSetProfile] a 
		inner join entity						b on a.RowId = b.EntityUid 
		inner join [Entity.LearningOpportunity] c ON b.Id = c.EntityId 
		Left Join [entity.DataSetProfile]		d on a.Id = d.DataSetProfileId 
		where a.EntityStateId = 3
		--exclude where connected to (likely) an entity.AggregateDataProfile
		AND d.Id is null 
		Group By c.LearningOpportunityId
		) dataSetProfile	on work.Id = dataSetProfile.LearningOpportunityId 
left join (
		Select c.LearningOpportunityId, COUNT(*)  As Nbr from [TransferValueProfile] a inner join entity b on a.RowId = b.EntityUid inner join [Entity.LearningOpportunity] c ON b.Id = c.EntityId Where a.EntityStateId = 3  Group By c.LearningOpportunityId
		) tvProfile	on work.Id = tvProfile.LearningOpportunityId 
--WHERE RowNumber > @first_id
order by RowNumber 
go

grant execute on [LearningOpportunity.ElasticSearch] to public
go

USE [credFinder]
GO

--use flstaging_credFinder
--go

--use staging_credFinder
--go

use sandbox_credFinder
go

/****** Object:  StoredProcedure [dbo].[Assessment.ElasticSearch]    Script Date: 1/19/2018 9:50:09 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


/*

[dbo].[Assessment.ElasticSearch] '', '', 0, 500, NULL

SLOW COMPARED TO ORG AND CREDENTIAL???????????????????????????0
=> 30 sec
- reduced to 8 sec if addresses removed
--try just asmt address=> 9 sec
--OK  -then added orgAddresses back, and also 9 sec????
--===================================================== 

DECLARE @RC int,@SortOrder varchar(100),@Filter varchar(5000)
DECLARE @StartPageIndex int, @PageSize int, @TotalRows int
--
set @SortOrder = ''
set @SortOrder = 'lastupdated'
--set @SortOrder = 'cost_highest'
set @SortOrder = 'org_alpha'
--set @SortOrder = 'cost_lowest'
--set @SortOrder = 'duration_shortest'
--set @SortOrder = 'duration_longest'
-- blind search 

set @Filter = ' ( base.EntityStateId > 1 )'
--set @Filter = ''

set @StartPageIndex = 1
set @PageSize = 500
--set statistics time on       
EXECUTE @RC = [Assessment.ElasticSearch]
     @Filter,@SortOrder  ,@StartPageIndex  ,@PageSize, @TotalRows OUTPUT

select 'total rows = ' + convert(varchar,@TotalRows)

--set statistics time off     


<QualityAssurance><row SourceEntityBaseId="192" RelationshipTypeId="10" SourceToAgentRelationship="Recognized By" AgentToSourceRelationship="Recognizes" AgentRelativeId="3" AgentName="Elon University"/></QualityAssurance>
*/
--EXEC [dbo].[Assessment.ElasticSearch] '', '', 0, 0, 0

/* =============================================
Description:      Assessment search for elastic load
Options:

  @StartPageIndex - starting page number. 
  @PageSize - number of records on a page
  @TotalRows OUTPUT - total available rows. Used by interface to build a
custom pager
  ------------------------------------------------------
Modifications
18-01-22 mparsons - created from existing search
18-10-06 mparsons - removed [IsQARole]= 1 from QualityAssurance, so that owned and offered by can be filtered. The actual property name should also be changed now! 
20-12-30 mparsons - changed to use improved approach
23-02-23 mparsons - assessing performance issues. 
			- found there was little concern with the Joins after the where clause
			- commented the XML related properties and selectively re added them
			- the AgentRelationshipsForEntity was the culprit
			- added orgAddresses back in and it was not a problem
			- so why is this an issue for assessments, but not lopps?
24-02-18 mparsons - making assessment_summary as light as possible by moving join stuff here
24-03-07 sneha - Provides TransferValue For and Receives TransferValue From Tags to search 
*/
ALTER PROCEDURE [dbo].[Assessment.ElasticSearch]
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

if @SortOrder = 'relevance' set @SortOrder = 'base.Name '
else if @SortOrder = 'alpha' set @SortOrder = 'base.Name '
else if @SortOrder = 'org_alpha' set @SortOrder = 'Organization, base.Name '
else if @SortOrder = 'oldest' set @SortOrder = 'base.Id'
else if @SortOrder = 'newest' set @SortOrder = 'base.lastUpdated Desc '
--else if @SortOrder = 'cost_highest' set @SortOrder = 'costs.TotalCost DESC'
--else if @SortOrder = 'cost_lowest' set @SortOrder = 'costs.TotalCost'
else set @SortOrder = 'base.Name '

if len(@SortOrder) > 0 
      set @OrderBy = ' Order by ' + @SortOrder
else
      set @OrderBy = ' Order by base.Name '

-- nolock testing @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED

--===================================================
-- Calculate the range
--===================================================
if @PageSize < 1				set @PageSize = 1000
IF @StartPageIndex < 1			SET @StartPageIndex = 1
SET @StartPageIndex =  ((@StartPageIndex - 1)  * @PageSize) + 1
SET @lastRow =  (@StartPageIndex + @PageSize) - 1
PRINT '@StartPageIndex = ' + convert(varchar,@StartPageIndex) +  ' @lastRow = ' + convert(varchar,@lastRow)
 
-- =================================
CREATE TABLE #tempWorkTable(
	RowNumber         int PRIMARY KEY NOT NULL,
	Id int,
	Name             varchar(500),
	OwningOrganization varchar(500),
	LastUpdated			datetime
)
  CREATE TABLE #tempQueryTotalTable(
      TotalRows int
)

CREATE TABLE [dbo].[#asmtTempTable](
	[EntityBaseId] [int] NOT NULL,
	[EntityUID] uniqueidentifier NOT NULL,
	[OrgId] [int] NOT NULL,
	[AgentName] [varchar](500) NOT NULL,
	[AgentUrl] [varchar](600) NULL,
	[EntityStateId] [int] NULL,
	[RelationshipTypeIds] [nvarchar](100) NULL,
	[Relationships] [nvarchar](1000) NULL,
	[AgentContextRoles] [varchar](500) NOT NULL
) 

-- =================================

  if len(@Filter) > 0 begin
     if charindex( 'where', @Filter ) = 0 OR charindex( 'where',  @Filter ) > 10
        set @Filter =     ' where ' + @Filter
     end

  print '@Filter len: '  +  convert(varchar,len(@Filter))
-- =================================
set @SQL = '   SELECT count(*) as TotalRows  FROM [dbo].Assessment_Summary base  '  + @Filter 
INSERT INTO #tempQueryTotalTable (TotalRows)
exec (@SQL)
--select * from #tempQueryTotalTable
select top 1  @TotalRows= TotalRows from #tempQueryTotalTable
--====
  set @SQL = ' 
  SELECT        
		DerivedTable.RowNumber, 
		base.Id
		,base.[Name], base.Organization
		,base.[lastUpdated]
From ( SELECT 
         ROW_NUMBER() OVER(' + @OrderBy + ') as RowNumber,
          base.Id, base.Name, base.Organization, base.lastUpdated
		from [Assessment_Summary] base  ' 
        + @Filter + ' 
   ) as DerivedTable
       Inner join [dbo].[Assessment_Summary] base on DerivedTable.Id = base.Id
WHERE RowNumber BETWEEN ' + convert(varchar,@StartPageIndex) + ' AND ' + convert(varchar,@lastRow) + ' '  

  print '@SQL len: '  +  convert(varchar,len(@SQL))
  print @SQL
  print 'populating #tempWorkTable ...' + convert(varchar(20),getdate(),120)

  INSERT INTO #tempWorkTable (RowNumber, Id, Name, OwningOrganization, LastUpdated)
  exec (@SQL)

--select * from #tempWorkTable

-- ==================================================================
--24-02-18 - this was done to be more performant, confirm it is
print 'populating #asmtTempTable started ...' + convert(varchar(20),getdate(),120)

INSERT INTO #asmtTempTable
           ([EntityBaseId], [EntityUID]
           ,[OrgId]           ,[AgentName]           ,[AgentUrl]
           ,[EntityStateId]
           ,[RelationshipTypeIds] ,[Relationships] ,[AgentContextRoles])
SELECT DISTINCT ear.EntityBaseId, ear.EntityUid, ear.AgentRelativeId As OrgId, ear.AgentName, ear.AgentUrl, ear.EntityStateId, ear.RoleIds as RelationshipTypeIds,  ear.Roles as Relationships, ear.AgentContextRoles
FROM [dbo].[Entity.AgentRelationshipIdCSV] ear
inner join #tempWorkTable t on ear.EntityBaseId = t.Id
where ear.EntityTypeId = 3
print 'populating #asmtTempTable completed ...' + convert(varchar(20),getdate(),120)

if @debugLevel> 7 begin
	select * from #asmtTempTable
end
-- ================================================================== 
SELECT        
	RowNumber
	,base.Id
	,base.RowId
	,base.EntityStateId
	--,ea.Nbr as AvailableAddresses
	,base.CTID
	,base.Name
	,isnull(base.[Description], '') As [Description]
	,isnull(base.SubjectWebpage, '') As SubjectWebpage
	,base.[DateEffective]
    ,isnull(base.[IdentificationCode],'') As [IdentificationCode]
	--,[SourceToAgentRelationship]
	--,[OrgId]
	--,[Organization]
	,case when Charindex('Placeholder', Isnull([Organization], '')) = 1 then 0
	else base.[OrgId] end  as [OrgId]
	,case when Charindex('Placeholder', Isnull([Organization], '')) = 1 then ''
	else [Organization] end  as [Organization]
	,base.OwningOrganizationCtid
	,base.Created, 
	base.LastUpdated 
	--,e.LastUpdated As EntityLastUpdated
	,ec.CacheDate As EntityLastUpdated
	,ec.ResourceDetail

	,base.LifeCycleStatusType
	,base.LifeCycleStatusTypeId
	,base.availableOnlineAt
	,base.AvailabilityListing
	,base.AssessmentExampleUrl
	,base.ProcessStandards
	,base.ScoringMethodExample
	,base.ExternalResearch

	,base.CredentialRegistryId
	,base.IsNonCredit
	--ex:  8~Is Preparation For~ceterms:isPreparationFor~2
	,base.ConnectionsList			--actual connection type (no credential info)
	--ex: 8~Is Preparation For~136~MSSC Certified Production Technician (CPT©)~| 8~Is Preparation For~272~MSSC Certified Logistics Technician (CLT©)~
	,base.CredentialsList	--connection type, plus Id, and name of credential

	-- === Connecitions ===================
	,base.RequiresCount
	,base.RecommendsCount
	,base.isRequiredForCount
	,base.IsRecommendedForCount
	,base.IsAdvancedStandingForCount
	,base.AdvancedStandingFromCount
	,base.isPreparationForCount
	,base.PreparationFromCount

	--==================================
	,IsNull(CommonCost.Nbr,0) As CommonCostsCount
	,IsNull(CommonCondition.Nbr,0) As CommonConditionsCount
	/* 20-10-10 mp - added number of cost profile items. Monitoring performance.	*/
	,Isnull(allCostProfiles.Total,0) as NumberOfCostProfileItems
	,IsNULL(costProfiles.Nbr, 0) As CostProfilesCount
	,IsNULL(conditionProfiles.Nbr, 0) As ConditionProfilesCount
	--
	,0 As TotalCostCount
	,IsNULL(FinancialAid.Nbr, 0) As FinancialAidCount
	,IsNULL(processProfiles.Nbr, 0) As ProcessProfilesCount
	--
	,0 As AggregateDataProfileCount
	,IsNull(dataSetProfile.Nbr,0)		As DataSetProfileCount
	,IsNull(tvProfile.Nbr,0)			As HasTransferValueProfileCount
	--,0 As HasTransferValueProfileCount
	,IsNULL(HasCIP.Nbr, 0) As HasCIPCount
	,IsNULL(HasDuration.Nbr, 0) As HasDurationCount

	,isnull(Languages.Languages,'') As Languages


	---***************** testing for bottle necks

	 ,(SELECT ISNULL(NULLIF(a.TextValue, ''), NULL) TextValue, a.[CodedNotation], a.CategoryId FROM [dbo].[Entity.SearchIndex] a inner join Entity b on a.EntityId = b.Id inner join Assessment c on b.EntityUid = c.RowId WHERE b.EntityTypeId = 3 AND c.Id = base.Id FOR XML RAW, ROOT('TextValues')
	 ) TextValues

	,(	SELECT acs.[Name], acs.[Description] FROM [dbo].Assessment_Competency_Summary acs  
		where acs.AssessmentId = base.Id AND acs.AlignmentType = 'assesses' 
		FOR XML RAW, ROOT('AssessesCompetencies')
	) AssessesCompetencies
	
	--,(SELECT DISTINCT crc.TargetNodeName As Name FROM [dbo].[ConditionProfile_RequiredCompetencies] crc  where crc.ParentEntityBaseId = base.Id AND crc.ParentEntityTypeId = 3 
	--	FOR XML RAW, ROOT('RequiresCompetencies')
	--) RequiresCompetencies
	,'' as RequiresCompetencies
	-- [Entity_Subjects] is a union of direct and indirect subjects	: not really applicable to asmts, and lopps, so just the text is used.
	,(SELECT DISTINCT a.[Subject] FROM [Entity_Subjects] a where EntityTypeId = 3 AND a.EntityUid = base.RowId 
		FOR XML RAW, ROOT('SubjectAreas')
	) SubjectAreas


	, ( SELECT DISTINCT a.CategoryId, a.[PropertyValueId], a.Property, PropertySchemaName  FROM [dbo].[EntityProperty_Summary] a where EntityTypeId= 3 AND CategoryId IN (4, 14, 21, 37, 54, 56) AND base.Id = [EntityBaseId] FOR XML RAW, ROOT('AssessmentProperties')
	) AssessmentProperties
	
	,(SELECT a.[CategoryId], a.[ReferenceFrameworkItemId], a.[Name], a.[SchemaName], ISNULL(NULLIF(a.[CodeGroup], ''), NULL) CodeGroup, a.[CodedNotation] FROM [dbo].[Entity_ReferenceFramework_Summary] a inner join Entity b on a.EntityId = b.Id inner join Assessment c on b.EntityUid = c.RowId where a.[CategoryId] IN (10, 11, 23) AND c.[Id] = base.[Id] 
		FOR XML RAW, ROOT('Frameworks')
	) Frameworks 

	--widget selection
 	, (SELECT a.WidgetId, a.WidgetSection  FROM [dbo].[Widget.Selection] a where a.EntityTypeId = 3 AND a.RecordId = base.[Id] 
		FOR XML RAW, ROOT('WidgetTags')
	) ResourceForWidget

	----transfer value member
	----24-02-18 mp - noted that this is  not being populated. Plus seems duplicate from use of tvProfile
 --	, (SELECT a.TransferValueProfileId, c.Name as TransferValueProfile  FROM [dbo].[Entity.TransferValueProfile] a Inner Join Entity b on a.EntityId = b.Id and b.EntityUid = base.RowId inner join TransferValueProfile c on a.TransferValueProfileId = c.Id where b.entityTypeId=3 and b.entityUID = base.RowId
	--	FOR XML RAW, ROOT('TransferValueReference')
	--) TransferValueReference
	,'' as TransferValueReference

	--collection member
 	, (SELECT a.CollectionId, b.Name as Collection  FROM [dbo].[Collection.CollectionMember] a Inner Join Collection b on a.CollectionId = b.Id where a.ProxyFor = base.CTID
		FOR XML RAW, ROOT('CollectionMembers')
	) CollectionMembers
	--		-----------------================================TRANSFERVALUES-=====================================--using resource detail
	--,(SELECT DISTINCT ehrs.Name,ehrs.ResourceId,e.Id  FROM [Entity.HasResourceSummary] ehrs Inner Join Entity e	on base.RowId = e.EntityUid
	--WHERE ehrs.[EntityTypeId] = 26 AND ehrs.EntityId = e.Id and [RelationshipTypeId]=15
	--FOR XML RAW, ROOT('ProvidesTransferValueFor')) ProvidesTransferValueFor,
 --     (SELECT DISTINCT ehrs.Name,ehrs.ResourceId FROM [Entity.HasResourceSummary] ehrs Inner join Entity b ON base.RowId = b.EntityUid
	--WHERE ehrs.[EntityTypeId] = 26 AND ehrs.EntityId = b.Id and [RelationshipTypeId]=16
	--FOR XML RAW, ROOT('ReceivesTransferValueFrom')) ReceivesTransferValueFrom
	--=== QA ==============================
	--,0 as QARolesCount -- is this needed?????
	,base.Org_QAAgentAndRoles
	--this is incorrect, it is all relationships should use RelationshipTypes for consistency
	--renamed from QualityAssurances
	,( SELECT DISTINCT a.[RelationshipTypeId] FROM [dbo].[Entity.AgentRelationship] a inner join Entity b on a.EntityId = b.Id 
		where b.EntityTypeId = 3 AND base.RowId = b.EntityUid FOR XML RAW, ROOT('RelationshipTypes')) 
		RelationshipTypes  
	,'' as QualityAssurances


	--now obsolete
	,'' as AgentRelationships

	--** QA asserted by third part, not owner
	 --,(SELECT DISTINCT OrgId, Organization as AgentName, TargetEntityStateId as EntityStateId, Assertions as RelationshipTypeIds FROM [dbo].Organization_QAPerformedCSV  WHERE [TargetEntityTypeId]=3 AND  TargetEntityBaseId = base.id 
		--FOR XML RAW, ROOT('ThirdPartyQualityAssuranceReceived')
		--) ThirdPartyQualityAssuranceReceived
		,'' as ThirdPartyQualityAssuranceReceived
	,(SELECT b.RowId, b.Id, b.EntityId, a.EntityUid, a.EntityTypeId, a.EntityBaseId, a.EntityBaseName, b.Id AS EntityAddressId, b.Name, b.IsPrimaryAddress, b.Address1, b.City, b.Region, b.SubRegion, b.PostOfficeBoxNumber, b.PostalCode, b.Country, b.Latitude, b.Longitude, b.IdentifierJson, b.Created, b.LastUpdated 
		FROM dbo.[Entity.Address] AS b
		INNER JOIN dbo.Entity AS a   ON a.Id = b.EntityId 
		where a.EntityTypeId = 3 AND a.[EntityUid] = base.[RowId] FOR XML RAW, ROOT('Addresses')
		) Addresses


	--	-- addresses for owning org - will only be used if there is no address for the assessment
	-- addresses for owning org - will only be used if there is no address for the credential
	, (SELECT b.RowId, b.Id, b.EntityId, a.EntityUid, a.EntityTypeId, a.EntityBaseId, a.EntityBaseName, b.Id AS EntityAddressId, b.Name, b.IsPrimaryAddress, b.Address1, b.Address2, b.City, b.Region, b.SubRegion, b.PostOfficeBoxNumber, b.PostalCode, b.Country, b.Latitude, b.Longitude, b.IdentifierJson, b.Created, b.LastUpdated FROM dbo.Entity AS a INNER JOIN dbo.[Entity.Address] AS b ON a.Id = b.EntityId where a.[EntityUid] = base.OwningAgentUid
		FOR XML RAW, ROOT('OrgAddresses')) OrgAddresses
	--,'' as OrgAddresses

	--all entity to organization relationships with org information. 
	--23-02-23 HUGE hit, this section resulted in the hanging. Same code is in lopp?
	--		no help on remove of AgentContextRoles
	 --	,(SELECT DISTINCT ear.AgentRelativeId As OrgId, ear.AgentName, ear.AgentUrl, ear.EntityStateId, ear.RoleIds as RelationshipTypeIds,  ear.Roles as Relationships, ear.AgentContextRoles FROM [dbo].[Entity.AgentRelationshipIdCSV] ear
		----inner join Assessment a on ear.entityBaseId = base.id 
		--inner join Assessment a on ear.EntityUid = asmt.RowId 
		--	--WHERE ear.EntityTypeId= 3 
		--	--AND ear.EntityBaseId = base.id 
		--FOR XML RAW, ROOT('AgentRelationshipsForEntity')) AgentRelationshipsForEntity

		--alt 1
		--24-02-19 mp - this will soon be replaced by using content from entity_cache
		--		 May require a process to ensure all the cache for all asmts has been populate
		--,ec.AgentRelationshipsForEntity
		,(SELECT ear.OrgId, ear.AgentName, ear.AgentUrl, ear.EntityStateId, ear.RelationshipTypeIds,  ear.Relationships, ear.AgentContextRoles 
			FROM #asmtTempTable ear
				WHERE ear.EntityBaseId = base.id 
		FOR XML RAW, ROOT('AgentRelationshipsForEntity')) AgentRelationshipsForEntity
		/*
		,'' as AgentRelationshipsForEntity2

	*/
	-- ==========================================================================
	From #tempWorkTable work 
	--Inner join Assessment asmt on work.Id = asmt.Id
	Inner join Assessment_Summary base on work.Id = base.Id

	--left join Entity e on work.Id = e.EntityBaseId and e.EntityTypeId = 1
	left join Entity_Cache ec on base.RowId = ec.EntityUid
   
	left join (
	Select b.EntityBaseId, COUNT(*)  As Nbr from [Entity.ConditionProfile] a Inner join Entity b ON a.EntityId = b.Id Where b.EntityTypeId = 3  Group By b.EntityBaseId
	) conditionProfiles	on base.Id = conditionProfiles.EntityBaseId  

	left join (
	Select b.EntityBaseId, COUNT(*)  As Nbr from [Entity.CostProfile] a Inner join Entity b ON a.EntityId = b.Id Where b.EntityTypeId = 3  Group By b.EntityBaseId
	) costProfiles	on base.Id = costProfiles.EntityBaseId 
/* 20-10-10 mp - added number of cost profile items. Monitoring performance.	*/
	left join (
		Select condProfParentEntityBaseId, Sum(TotalCostItems) As Total from [CostProfile_SummaryForSearch] 
		where condProfParentEntityTypeId =3  and TotalCostItems > 0
		group by condProfParentEntityBaseId 
		) allCostProfiles	on work.Id = allCostProfiles.condProfParentEntityBaseId
	--
	
	left join (
	Select b.EntityBaseId, COUNT(*)  As Nbr from [Entity.CommonCost] a Inner join Entity b ON a.EntityId = b.Id Where b.EntityTypeId = 3  Group By b.EntityBaseId
	) CommonCost	on base.Id = CommonCost.EntityBaseId     
	left join (
	Select b.EntityBaseId, COUNT(*)  As Nbr from [Entity.CommonCondition] a Inner join Entity b ON a.EntityId = b.Id Where b.EntityTypeId = 3  Group By b.EntityBaseId
	) CommonCondition	on base.Id = CommonCondition.EntityBaseId     
	--==================================
	left join (
	Select b.EntityBaseId, COUNT(*)  As Nbr from [Entity.FinancialAssistanceProfile] a Inner join Entity b ON a.EntityId = b.Id Where b.EntityTypeId = 3  Group By b.EntityBaseId 
	) FinancialAid	on base.Id = FinancialAid.EntityBaseId  
	--==================================
	left join (
	Select b.EntityBaseId, COUNT(*)  As Nbr from [Entity.ProcessProfile] a Inner join Entity b ON a.EntityId = b.Id Where b.EntityTypeId = 3  Group By b.EntityBaseId
	) processProfiles	on base.Id = processProfiles.EntityBaseId      
	--==================================
	left join (
	Select b.EntityBaseId, COUNT(*)  As Nbr from [Entity.ReferenceFramework] a Inner join Entity b ON a.EntityId = b.Id Where b.EntityTypeId = 3  Group By b.EntityBaseId 
	) HasCIP	on base.Id = HasCIP.EntityBaseId  
	--==================================
	left join (
	Select b.EntityBaseId, COUNT(*)  As Nbr from [Entity.DurationProfile] a Inner join Entity b ON a.EntityId = b.Id Where b.EntityTypeId = 3  Group By b.EntityBaseId 
	) HasDuration	on base.Id = HasDuration.EntityBaseId  
	--==================================
	left Join ( 
		SELECT     distinct base.Id, 
		CASE WHEN Languages IS NULL THEN ''           WHEN len(Languages) = 0 THEN ''          ELSE left(Languages,len(Languages)-1)     END AS Languages
		From dbo.Assessment base
		CROSS APPLY ( SELECT a.Title + '| ' + a.TextValue + '| '
			FROM [dbo].[Entity.Reference] a inner join [Entity] b on a.EntityId = b.Id 
			where b.EntityTypeId= 3 AND a.CategoryId = 65
			and base.Id = b.EntityBaseId FOR XML Path('')  ) D (Languages)
		where Languages is not null
	) Languages on base.Id = Languages.Id
	--==================================
left join (
		Select c.AssessmentId, COUNT(*)  As Nbr from [DataSetProfile] a inner join entity b on a.RowId = b.EntityUid inner join [Entity.Assessment] c ON b.Id = c.EntityId Left Join [entity.DataSetProfile] d on a.Id = d.DataSetProfileId where d.Id is null Group By c.AssessmentId
		) dataSetProfile	on work.Id = dataSetProfile.AssessmentId 
	--==================================
left join (
		Select c.AssessmentId, COUNT(*)  As Nbr from [TransferValueProfile] a inner join entity b on a.RowId = b.EntityUid inner join [Entity.Assessment] c ON b.Id = c.EntityId where a.entityStateId = 3 Group By c.AssessmentId
		) tvProfile	on work.Id = tvProfile.AssessmentId 
	--left Join (
	--	select EntityId, count(*) as nbr from Assessment_Competency_Summary group by EntityId 
	--	) comps on e.Id = comps.EntityId  
	--left join EntityProperty_AudienceTypeCSV typesCsv on base.Id = typesCsv.EntityId
--WHERE RowNumber > @first_id

order by RowNumber 


go
grant execute on [Assessment.ElasticSearch] to public

go

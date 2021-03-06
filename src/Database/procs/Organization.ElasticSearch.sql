USE [credFinder]
GO
--USE staging_credFinder
--GO

/****** Object:  StoredProcedure [dbo].[Organization.ElasticSearch]    Script Date: 4/20/2018 11:40:06 AM ******/
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
--set @Filter = ' ( base.Id in (SELECT  OrgId FROM [Organization.Member] where UserId = 2) ) '
--set @Filter = ' (OrgTypeId in (1,6)) '


--set @Filter = ' ( base.Id in (SELECT b.EntityBaseId FROM [dbo].[Entity.ConditionManifest] a inner join entity b on a.EntityId = b.Id  ) )  '

--set @Filter = ' ( Name = ''American Welding Society'' ) '

set @Filter = ' id = 424'
set @StartPageIndex = 1
set @PageSize = 200
--set statistics time on       
EXECUTE @RC = [Organization.ElasticSearch]
     @Filter,@SortOrder  ,@StartPageIndex  ,@PageSize,  @TotalRows OUTPUT

select 'total rows = ' + convert(varchar,@TotalRows)

--set statistics time off       

<AlternateIdentifiers><row Title="DUNS" TextValue="789797920"/><row Title="FEIN" TextValue="04-3064434"/></AlternateIdentifiers>

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
18-06-21 mparsons - re: Cache.Organization_ActorRoles. there seems to be issues where the cache has not been updated before this proc runs, changed to use view directly.
*/
--[dbo].[Organization.ElasticSearch] '', '', 0, 20, NULL

ALTER PROCEDURE [dbo].[Organization.ElasticSearch] 
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
	base.EntityStateId,
	base.Name, 
	base.Description,
	base.SubjectWebpage,
	--base.MainPhoneNumber,
	--base.Email, 
	--base.Address1,  base.Address2,  base.City, base.Region,		base.PostalCode, base.Country, 
	base.Latitude, base.Longitude,
	base.Created, 
	base.LastUpdated,  
	e.LastUpdated As EntityLastUpdated,
	base.RowId, base.ImageURL
	,base.CTID
	,base.CredentialRegistryId
	,base.CredentialCount
	,base.IsAQAOrganization
	,base.AvailabilityListing
	--,oa.Nbr as AvailableAddresses

	--,isnull(naicsCsv.naics,'') As NaicsList
	--,isnull(naicsCsv.Others,'') As OtherIndustriesList

,isnull(actorRolesCsv.OwnedBy,'') As OwnedByList
,isnull(actorRolesCsv.OfferedBy,'') As OfferedByList
,isnull(actorRolesCsv.AsmtsOwnedBy,'') As AsmtsOwnedByList
,isnull(actorRolesCsv.LoppsOwnedBy,'') As LoppsOwnedByList
,isnull(actorRolesCsv.FrameworksOwnedBy,'') As FrameworksOwnedByList

,isnull(recipRolesCsv.AccreditedBy,'') As AccreditedByList
,isnull(recipRolesCsv.ApprovedBy,'') As ApprovedByList
,isnull(recipRolesCsv.RecognizedBy,'') As RecognizedByList
,isnull(recipRolesCsv.RegulatedBy,'') As RegulatedByList

,isnull(verificationProfiles.Nbr,0) As VerificationProfilesCount
,isnull(costManifests.Nbr,0) As CostManifestsCount
,isnull(conditionManifests.Nbr,0) As ConditionManifestsCount

,isnull(Subsidiaries.Nbr,0) As SubsidiariesCount
,isnull(Departments.Nbr,0) As DepartmentsCount

,isnull(HasIndustries.Nbr, 0) As HasIndustriesCount

-- ======================================================
	-- For ElasticSearch
	-- Depends on Entity.SearchIndex, so the latter must be up to date!

	, (SELECT ISNULL(NULLIF(a.TextValue, ''), NULL) TextValue, a.CategoryId FROM [dbo].[Entity] e INNER JOIN [dbo].[Entity.SearchIndex] a ON a.EntityId = e.Id where e.EntityTypeId = 2 AND base.RowId = e.EntityUid FOR XML RAW, ROOT('TextValues')) TextValues

	, STUFF((SELECT '|' + ISNULL(NULLIF(a.TextValue, ''), NULL) AS [text()] FROM [dbo].[Organization_AlternatesNames] a   where base.Id = a.EntityBaseId FOR XML Path('')), 1,1,'') AlternatesNames

	-- change to not use view to reduce dependency on entity_cache
	--, (SELECT DISTINCT Title, TextValue FROM [dbo].Entity_Reference_Summary where EntityTypeId= 2 AND CategoryId = 9 AND [EntityBaseId] = base.Id FOR XML RAW, ROOT('AlternateIdentifiers2')) AlternateIdentifiers2
	
	,(SELECT a.Title, a.TextValue FROM [dbo].[Entity.Reference] a inner join Entity b on a.EntityId = b.Id 
	--inner join Organization c on b.EntityUid = c.RowId 
		where a.[CategoryId] = 9 AND b.EntityTypeId= 2 AND b.EntityBaseId = base.[Id] FOR XML RAW, ROOT('AlternateIdentifiers')) AlternateIdentifiers 

 	--all three of these end up in the same property, so could be combined,especially for performance reasons
	--ACTUALLY, these are all in the latter PropertyValues - so do we need these????
	, (SELECT DISTINCT CategoryId, [PropertyValueId], Property, PropertySchemaName FROM [dbo].[EntityProperty_Summary] where (EntityTypeId= 2 AND CategoryId IN (6, 7, 30)) AND [EntityBaseId] = base.Id FOR XML RAW, ROOT('PropertyValues')) PropertyValues

	--,(SELECT DISTINCT eps.CategoryId, eps.[PropertyValueId], eps.Property, eps.PropertySchemaName FROM dbo.Entity e INNER JOIN dbo.[Entity.VerificationProfile] evp ON e.Id = evp.EntityId INNER JOIN dbo.EntityProperty_Summary eps ON evp.RowId = eps.EntityUid WHERE e.EntityTypeId = 2 AND e.EntityBaseId = base.Id FOR XML RAW, ROOT('VerificationProfiles')) VerificationProfiles

	 , (SELECT DISTINCT CategoryId, [PropertyValueId], Property, PropertySchemaName FROM [dbo].[EntityProperty_Summary] a
                     inner join [Entity.VerificationProfile] b on a.EntityUid = b.RowId
                     inner join Entity orgEntity on b.entityId = orgEntity.Id 
                     where orgEntity.EntityTypeId= 2 AND CategoryId = 41
                     AND orgEntity.EntityBaseId = base.Id
                     FOR XML RAW, ROOT('ClaimTypes') ) ClaimTypes


	--contains industry. This is slow, consider options
	,(SELECT a.[CategoryId], a.[ReferenceFrameworkId], a.[Name], a.[SchemaName], ISNULL(NULLIF(a.[CodeGroup], ''), NULL) CodeGroup , ISNULL(a.CodedNotation, '') CodedNotation 
		FROM [dbo].[Entity_ReferenceFramework_Summary] a where a.EntityTypeId = 2 AND a.CategoryId = 10 AND a.[EntityId] = base.[EntityId] FOR XML RAW, ROOT('Frameworks')) Industries

	--contains unique list of all relationships
	--, (SELECT distinct a.RelationshipTypeId FROM [dbo].[Entity.AgentRelationship] a 
	--	inner join Entity b on a.EntityId = b.Id 
	--	WHERE b.EntityUid = base.RowId 
	--	FOR XML RAW, ROOT('AgentRelationships')) AgentRelationships
	--now obsolete
	,'' as AgentRelationships

	--all entity to organization relationships with org information.
	--Roles is from entity context, AgentContextRoles is from the agent context. Accredited By vs Accredits 
	 ,(SELECT DISTINCT AgentRelativeId As OrgId, AgentName, AgentUrl, EntityStateId, RoleIds as RelationshipTypeIds,  Roles as Relationships, AgentContextRoles FROM [dbo].[Entity.AgentRelationshipIdCSV]
			WHERE EntityTypeId= 2 AND EntityBaseId = base.id 
			FOR XML RAW, ROOT('AgentRelationshipsForEntity')) AgentRelationshipsForEntity

	--
	-- remove this once completely implemented QualityAssuranceCsv or AgentRelationshipsForEntity
	,(SELECT DISTINCT [RelationshipTypeId] ,[SourceToAgentRelationship]   ,[AgentToSourceRelationship]  ,[AgentRelativeId], AgentName, AgentUrl, IsQARole, [EntityStateId] FROM [dbo].[Entity_Relationship_AgentSummary]  WHERE [SourceEntityTypeId] = 2 AND [SourceEntityBaseId] = base.id FOR XML RAW, ROOT('QualityAssurance')) QualityAssurance

	-- just QA roles with organization
	 --,(SELECT DISTINCT AgentRelativeId , AgentName, RoleIds as RelationshipTypeIds FROM [dbo].[Entity.Agent_QARelationshipCSV] 
		--	WHERE EntityTypeId= 2 AND EntityBaseId = base.id 
		--	FOR XML RAW, ROOT('QualityAssuranceCsv')) QualityAssuranceCsv

	--,(SELECT DISTINCT AssertionTypeId ,[SourceToAgentRelationship]   ,[AgentToSourceRelationship]  ,TargetEntityBaseId, TargetEntityName, TargetEntitySubjectWebpage, [TargetEntityStateId] FROM [dbo].[Entity_Assertion_Summary]  WHERE  [IsQARole]= 1 and OrgId = base.id FOR XML RAW, ROOT('QualityAssurancePerformed')) QualityAssurancePerformed

	--QualityAssuranceCombined will be replaced by QualityAssurancePerformedCSV
	--,(SELECT DISTINCT roleSource, [TargetEntityTypeId], TargetEntityBaseId, [RelationshipTypeId] , [SourceToAgentRelationship]   ,[AgentToSourceRelationship]  ,TargetEntityName, TargetEntitySubjectWebpage, [TargetEntityStateId] FROM [dbo].Organization_CombinedQAPerformed  WHERE  [IsQARole]= 1 and OrgId = base.Id FOR XML RAW, ROOT('QualityAssuranceCombined')) QualityAssuranceCombined

	 ,(SELECT DISTINCT TargetEntityStateId, [TargetEntityTypeId],TargetEntityBaseId , TargetEntityName, Assertions FROM [dbo].Organization_QAPerformedCSV  WHERE  OrgId = base.id FOR XML RAW, ROOT('QualityAssurancePerformedCSV')) QualityAssurancePerformedCSV

	-- ( base.EntityStateId = 3 )  AND  ( base.RowId in ( SELECT distinct b.EntityUid FROM [dbo].[Entity.AgentRelationship] a inner join Entity b on a.EntityId = b.Id   where [RelationshipTypeId] in (1,2)   ) ) 
	,(SELECT b.RowId, b.Id, b.EntityId, a.EntityUid, a.EntityTypeId, a.EntityBaseId, a.EntityBaseName, b.Id AS EntityAddressId, b.Name, b.IsPrimaryAddress, b.Address1, b.Address2, b.City, b.Region, b.PostOfficeBoxNumber, b.PostalCode, b.Country, b.Latitude, b.Longitude, b.Created, b.LastUpdated FROM dbo.Entity AS a INNER JOIN dbo.[Entity.Address] AS b ON a.Id = b.EntityId where a.[EntityUid] = base.[RowId] FOR XML RAW, ROOT('Addresses')) Addresses

From #tempWorkTable work
Inner join Organization_Summary base on work.Id = base.Id
Inner join Entity e on work.Id = e.EntityBaseId and e.EntityTypeId = 2
--18-06-21 mp - to avoid issues where cache update fails, using view directly
--left join [Cache.Organization_ActorRoles] actorRolesCsv on work.id = actorRolesCsv.OrganizationId
left join [Organization_ActorRolesCSV] actorRolesCsv on work.id = actorRolesCsv.OrganizationId
	
left join Organization_RecipientRolesCSV recipRolesCsv on work.id = recipRolesCsv.OrganizationId
--left join Organization_QAPerformedCSV performedRolesCsv on work.id = performedRolesCsv.OrgId

left join (
	SELECT a.Id As OrganizationId, count(*) as Nbr  FROM Organization a inner join entity b on a.RowId = b.EntityUid  inner join  [dbo].[Entity.VerificationProfile] c on b.id = c.EntityId group by a.Id
) verificationProfiles on base.id = verificationProfiles.OrganizationId 

left join (
	SELECT a.Id As OrganizationId, count(*) as Nbr  FROM Organization a inner join entity b on a.RowId = b.EntityUid  inner join  [dbo].[Entity.CostManifest] c on b.id = c.EntityId group by a.Id
) costManifests on base.id = costManifests.OrganizationId

left join (
	SELECT a.Id As OrganizationId, count(*) as Nbr  FROM Organization a inner join entity b on a.RowId = b.EntityUid  inner join  [dbo].[Entity.ConditionManifest] c on b.id = c.EntityId group by a.Id
) conditionManifests on base.id = conditionManifests.OrganizationId

left join ( 
	SELECT a.EntityBaseId As OrganizationId, count(*) as Nbr  FROM entity a inner join  [dbo].[Entity.AgentRelationship] b on a.id = b.EntityId where RelationshipTypeId = 21 group by a.EntityBaseId
) Subsidiaries on base.id = Subsidiaries.OrganizationId

left join (
	SELECT a.EntityBaseId As OrganizationId, count(*) as Nbr  FROM entity a inner join  [dbo].[Entity.AgentRelationship] b on a.id = b.EntityId where RelationshipTypeId = 20 group by a.EntityBaseId
) Departments on base.id = Departments.OrganizationId
left join (
	SELECT a.Id As OrganizationId, count(*) as Nbr  FROM Organization a inner join entity b on a.RowId = b.EntityUid  inner join  [dbo].[Entity.ReferenceFramework] c on b.id = c.EntityId where CategoryId = 10 group by a.Id
) HasIndustries on base.id = HasIndustries.OrganizationId


WHERE RowNumber > @first_id
order by RowNumber 


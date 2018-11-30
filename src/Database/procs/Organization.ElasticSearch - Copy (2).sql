USE [credFinder]
GO
/****** Object:  StoredProcedure [dbo].[Organization.ElasticSearch]    Script Date: 2/6/2018 8:46:27 PM ******/
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


set @Filter = ' ( base.Id in (SELECT b.EntityBaseId FROM [dbo].[Entity.ConditionManifest] a inner join entity b on a.EntityId = b.Id  ) )  '

set @Filter = ' ( Name = ''American Welding Society'' ) '

set @Filter = ''
set @StartPageIndex = 1
set @PageSize = 100
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
	base.Name, 
	base.Description,
	 base.SubjectWebpage,
	--base.MainPhoneNumber,
	--base.Email, 
	--base.Address1,  base.Address2,  base.City, base.Region,		base.PostalCode, base.Country, 
	base.Latitude, base.Longitude,
	base.Created, 
	base.LastUpdated,  
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
		, STUFF((SELECT '|' + ISNULL(NULLIF(a.TextValue, ''), NULL) AS [text()] FROM [dbo].[Entity] e INNER JOIN [dbo].[Entity.SearchIndex] a ON e.Id = a.EntityId  where base.RowId = e.EntityUid AND e.EntityTypeId = 2 FOR XML Path('')), 1,1,'') TextValues

		, STUFF((SELECT '|' + ISNULL(NULLIF(a.TextValue, ''), NULL) AS [text()] FROM [dbo].[Organization_AlternatesNames] a   where base.Id = a.EntityBaseId FOR XML Path('')), 1,1,'') AlternatesNames

	--, (SELECT a.TextValue FROM [dbo].[Organization_AlternatesNames] a 		WHERE a.EntityBaseId = base.Id 		FOR XML RAW, ROOT('AlternatesNames')) AlternatesNames

	--, STUFF((SELECT '|' + ISNULL(NULLIF(a.CodedNotation, ''), NULL) AS [text()] FROM [dbo].[Entity] e INNER JOIN [dbo].[Entity.SearchIndex] a ON a.EntityId = e.Id where base.RowId = e.EntityUid AND e.EntityTypeId = 2 FOR XML Path('')), 1,1,'') CodedNotation

	--, STUFF((SELECT '|' + ISNULL(NULLIF(CAST(eps.PropertyValueId AS NVARCHAR(MAX)), ''), NULL) AS [text()] 
	--	FROM [dbo].[EntityProperty_Summary] eps 
	--	where eps.EntityTypeId = 2 AND eps.EntityBaseId = base.Id 
	--	FOR XML Path('')), 1,1,'') PropertyValues

	, (SELECT DISTINCT Title, TextValue FROM [dbo].Entity_Reference_Summary where EntityTypeId= 2 AND CategoryId = 9 AND [EntityBaseId] = base.Id FOR XML RAW, ROOT('AlternateIdentifiers')) AlternateIdentifiers

 	--all three of these end up in the same property, so could be combined,especially for performance reasons
	--ACTUALLY, these are all in the latter PropertyValues - so do we need these????
	, (SELECT DISTINCT CategoryId, [PropertyValueId], Property FROM [dbo].[EntityProperty_Summary] where EntityTypeId= 2 AND CategoryId IN (6, 7, 30) AND [EntityBaseId] = base.Id FOR XML RAW, ROOT('PropertyValues')) PropertyValues

	--the entity in [EntityProperty_Summary] will be for the Entity.VerificationProfile, we need to tie this to the org 
	, (SELECT DISTINCT CategoryId, [PropertyValueId], Property  FROM [dbo].[EntityProperty_Summary] a 
			inner join [Entity.VerificationProfile] b on a.EntityUid = b.RowId 
			inner join Entity orgEntity on b.entityId = orgEntity.Id  
			where orgEntity.EntityTypeId= 2 AND CategoryId = 41 
			AND orgEntity.EntityBaseId = base.Id 
			FOR XML RAW, ROOT('ClaimTypes') ) ClaimTypes
	--actually only one
	--, (SELECT DISTINCT CategoryId, [PropertyValueId], Property  FROM [dbo].[EntityProperty_Summary] where EntityTypeId= 2 AND CategoryId = 30 AND [EntityBaseId] = base.Id FOR XML RAW, ROOT('SectorTypes')) SectorTypes
	
 --naics is replaced by Entity_ReferenceFramework_
 --left join [Entity.NaicsCSV] naicsCsv	on base.EntityId = naicsCsv.EntityId
--, STUFF((SELECT '|' + ISNULL(NULLIF(CAST(a.[ReferenceFrameworkId] AS NVARCHAR(MAX)), ''), NULL) AS [text()] 
--	FROM [dbo].[Entity_ReferenceFramework_Summary] a 
--	inner join Entity b on a.EntityId = b.Id AND a.EntityTypeId = 2 AND a.CategoryId = 10
--	WHERE b.EntityUid = base.RowId 
--	FOR XML Path('')), 1,1,'') ReferenceFrameworks

	--contains industry. This is slow, consider options
	,(SELECT a.[CategoryId], a.[ReferenceFrameworkId], a.[Name], a.[SchemaName], ISNULL(NULLIF(a.[CodeGroup], ''), NULL) CodeGroup , ISNULL(a.CodedNotation, '') CodedNotation 
	FROM [dbo].[Entity_ReferenceFramework_Summary] a where a.EntityTypeId = 2 AND a.CategoryId = 10 AND a.[EntityId] = base.[EntityId] FOR XML RAW, ROOT('Frameworks')) Industries

	--, (SELECT a.[CodeId] FROM [dbo].[Organization.ServiceSummary] a 
	--	WHERE a.OrganizationId = base.Id 
	--	FOR XML RAW, ROOT('ServiceCodes')) ServiceCodes

	, (SELECT distinct a.RelationshipTypeId FROM [dbo].[Entity.AgentRelationship] a inner join Entity b on a.EntityId = b.Id 
	WHERE b.EntityUid = base.RowId 
	FOR XML RAW, ROOT('AgentRelationships')) AgentRelationships

	,(SELECT DISTINCT [RelationshipTypeId] ,[SourceToAgentRelationship]   ,[AgentToSourceRelationship]  ,[AgentRelativeId], AgentName, [EntityStateId] FROM [dbo].[Entity_Relationship_AgentSummary]  WHERE [IsQARole]= 1 and [SourceEntityTypeId] = 2 AND [SourceEntityBaseId] = base.id FOR XML RAW, ROOT('QualityAssurance')) QualityAssurance

	,(SELECT b.RowId, b.Id, b.EntityId, a.EntityUid, a.EntityTypeId, a.EntityBaseId, a.EntityBaseName, b.Id AS EntityAddressId, b.Name, b.IsPrimaryAddress, b.Address1, b.Address2, b.City, b.Region, b.PostOfficeBoxNumber, b.PostalCode, b.Country, b.Latitude, b.Longitude, b.Created, b.LastUpdated FROM dbo.Entity AS a INNER JOIN dbo.[Entity.Address] AS b ON a.Id = b.EntityId where a.[EntityUid] = base.[RowId] FOR XML RAW, ROOT('Addresses')) Addresses

From #tempWorkTable work
Inner join Organization_Summary base on work.Id = base.Id

left join [Cache.Organization_ActorRoles] actorRolesCsv on work.id = actorRolesCsv.OrganizationId
	
left join Organization_RecipientRolesCSV recipRolesCsv on work.id = recipRolesCsv.OrganizationId

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
	SELECT a.Id As OrganizationId, count(*) as Nbr  FROM Organization a inner join entity b on a.RowId = b.EntityUid  inner join  [dbo].[Entity.AgentRelationshipIdCSV] c on b.id = c.EntityId where (',' + RTRIM(c.RoleIds) + ',')  LIKE '%,20,%' group by a.Id
) Subsidiaries on base.id = Subsidiaries.OrganizationId

left join (
	SELECT a.Id As OrganizationId, count(*) as Nbr  FROM Organization a inner join entity b on a.RowId = b.EntityUid  inner join  [dbo].[Entity.AgentRelationshipIdCSV] c on b.id = c.EntityId where (',' + RTRIM(c.RoleIds) + ',') LIKE '%,21,%' group by a.Id
) Departments on base.id = Departments.OrganizationId
left join (
	SELECT a.Id As OrganizationId, count(*) as Nbr  FROM Organization a inner join entity b on a.RowId = b.EntityUid  inner join  [dbo].[Entity.ReferenceFramework] c on b.id = c.EntityId where CategoryId = 10 group by a.Id
) HasIndustries on base.id = HasIndustries.OrganizationId


WHERE RowNumber > @first_id
order by RowNumber 

go
grant execute on [Organization.ElasticSearch] to public
go
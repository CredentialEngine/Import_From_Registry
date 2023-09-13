use credFinder
GO

use sandbox_credFinder
go

--use staging_credFinder
--go
--use snhu_credFinder
--go

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

/*
select entityStateId, count(*) as nbr from [Credential.SummaryCache]
group by entityStateId

select top 100 *  from [Credential.SummaryCache]
order by lastSyncDate desc 


[Populate_Credential_SummaryCache] -1

[Populate_Credential_SummaryCache] 0

[Populate_Credential_SummaryCache] 2799

*/


/* =============================================
Description:      Populate_Credential_SummaryCache
									NOTE: issue with  duplicates if have more than one owner or creater org
Options:

  @CredentialId - optional credentialId. 
				If non-zero will only replace the related row, 
				If -1, will use contents of SearchPendingReindex. This means it has to run before doing a reindex
					23-01-27 updated to compare last sync date to lastUpdatedDate of the related Entity.
				otherwise replace all

  ------------------------------------------------------
Modifications
16-10-27 mparsons - new version to only cache the credential parts properties
17-02-28 mparsons - added OwningAgentUid
17-09-14 mparssons - added HasPartList, and IsPartOfList
17-10-10 mparsons - added owningOrganization and OwningOrganizationId 
17-11-16 mparsons - added entityUid
18-02-26 mparsons - added processing from SearchPendingReindex 
23-01-27 mparsons - removed use of SearchPendingReindex as this may not be run before the handle pending indexing runs
*/

Alter PROCEDURE [dbo].[Populate_Credential_SummaryCache]
		@CredentialId	int = 0
As

SET NOCOUNT ON;

DECLARE
	@debugLevel      int

Set @debugLevel = 4

-- =================================
if @CredentialId > 0 begin
	print 'deleting credential ' + convert(varchar,@CredentialId)
	DELETE FROM [dbo].[Credential.SummaryCache]
					WHERE CredentialId = @CredentialId

	print 'deleted credential ' + convert(varchar, @@ROWCOUNT)
	end
else if @CredentialId = -1 begin
	print 'using pending reindex'
	-- 'duh the SearchPendingReindex.StatusId is set to 2 after update. This would have to run before the reindex???'
	-- changed to check for updates
	--DELETE D 
	--	FROM [dbo].[Credential.SummaryCache] D
	--	inner join SearchPendingReindex b on d.CredentialId = b.RecordId And b.EntityTypeId = 1 and b.StatusId = 1
	DELETE D 
	-- Select D.CredentialId
		FROM [dbo].[Credential.SummaryCache] D
		Inner Join [entity] e on D.EntityId = e.Id
		WHERE e.LastUpdated > D.LastSyncDate

		if @@ROWCOUNT > 0 begin
			print 'deleted credential. Count: ' + convert(varchar, @@ROWCOUNT)
		end
		else begin
			--exit
			print 'no target data FOR @CredentialId = -1 - exiting'
			return -1;
		end

	end

else begin
		print 'truncating table'
		truncate table [Credential.SummaryCache]
	end

	
INSERT INTO [dbo].[Credential.SummaryCache]
           ([CredentialId]
					 ,EntityStateId
					,EntityId
					,EntityUid
					,[LastSyncDate]
					,[CredentialType]
					,[CredentialTypeSchema]
					,[CredentialTypeId]

					,OwningAgentUid
					,OwningOrganizationId
					--,OwningOrganization
					,[IsAQACredential]
					,[HasQualityAssurance]
					,OwningOrgs
					,OfferingOrgs
					,[LearningOppsCompetenciesCount]
					,[AssessmentsCompetenciesCount]
					,[RequiresCompetenciesCount]
					,[QARolesCount]

					,[HasPartCount]
					,[IsPartOfCount]
					,HasPartList
					,IsPartOfList

					,[RequiresCount]
					,[RecommendsCount]
					,[RequiredForCount]
					,[IsRecommendedForCount]
					,[IsAdvancedStandingForCount]
					,[AdvancedStandingFromCount]
					,[PreparationForCount]
					,[PreparationFromCount]

					,EntryConditionCount
					,CorequisiteConditionCount

					,QARolesList
					,AgentAndRoles
					,QAOrgRolesList
					,QAAgentAndRoles	-- new
					
					,BadgeClaimsCount
					
					,AvailableAddresses
					,NaicsList
					,LevelsList
					,OccupationsList
					,SubjectsList
					,ConnectionsList
					,CredentialsList
					,TotalCost
					,NumberOfCostProfileItems
					,AverageMinutes
					)
    
SELECT Distinct
	base.Id
	,isnull(base.EntityStateId,1)	as EntityStateId
	,base.EntityId
	,base.EntityUid
	,getdate()		as [LastSyncDate]
	,base.[CredentialType]
	,base.[CredentialTypeSchema]
	,base.[CredentialTypeId]

	,base.OwningAgentUid
	,owningOrg.Id as OwningOrganizationId
	--	,owningOrg.Name as OwningOrganization

	,base.[IsAQACredential]
	,base.[HasQualityAssurance]
	,base.OwningOrgs
	,base.OfferingOrgs
        
	,base.[LearningOppsCompetenciesCount]
	,base.[AssessmentsCompetenciesCount]
	,base.[RequiresCompetenciesCount]

	,base.[QARolesCount]
	,base.[HasPartCount]
	,base.[IsPartOfCount]
	,base.HasPartList
	,base.IsPartOfList

	,base.[RequiresCount]
	,base.[RecommendsCount]
	,base.[RequiredForCount]
	,base.[IsRecommendedForCount]
	-- ,[RenewalCount]
	,base.[IsAdvancedStandingForCount]
	,base.[AdvancedStandingFromCount]
	,base.[PreparationForCount]
	,base.[PreparationFromCount]

	,base.EntryConditionCount
	,base.CorequisiteConditionCount

	,base.QARolesList
	,base.AgentAndRoles
	,base.Org_QARolesList
	,base.Org_QAAgentAndRoles
			
	,isnull(badgeClaims.Total, 0) as badgeClaimsCount
	--====
	,ea.Nbr as AvailableAddresses
	,isnull(naicsCsv.naics,'') As NaicsList
	,isnull(levelsCsv.Properties,'') As LevelsList

	,isnull(occsCsv.Occupations,'') As OccupationsList
	--====
	,isnull(subjectsCsv.Subjects,'') As SubjectsList

	--this may be obsolete now - that is it should be moved to the summaries or cache?
	--18-04-11 mp - still in use
	,isnull(connectionsCsv.Profiles,'') As ConnectionsList
	,isnull(connectionsCsv.CredentialsList,'') As CredentialsList
	--====
	--,isnull(costs.totalCost,0)				As TotalCost
	,0 as TotalCost
	,isnull(allCostProfiles.Total,0)	as NumberOfCostProfileItems
	--duration.AverageMinutes
	,0 as AverageMinutes
  FROM [dbo].Credential_PartsSummary base
	Left Join Organization						owningOrg on base.OwningAgentUid = owningOrg.RowId
	-- =====
	left Join (select EntityId, count(*) as nbr from [Entity.Address] group by EntityId ) ea on base.EntityId = ea.EntityId
	-- =====
	left join [Entity.NaicsCSV] naicsCsv			  on base.EntityId = naicsCsv.EntityId
	left join [Entity.OccupationsCSV] occsCsv			on base.EntityId = occsCsv.EntityId
	left join EntityProperty_EducationLevelCSV levelsCsv on base.EntityId = levelsCsv.EntityId
	-- ===
	left join [Entity_SubjectsCSV] subjectsCsv		on base.EntityUid = subjectsCsv.EntityUid
	left join Credential_ConditionProfilesCSV connectionsCsv on base.id = connectionsCsv.CredentialId

-- ===
	--not ideal, but doing a total
	--left join (
	--Select ParentEntityUid, sum(isnull(TotalCost, 0)) As TotalCost from Entity_CostProfileTotal group by ParentEntityUid
	--) costs				
	--	on base.EntityUid = costs.ParentEntityUid
		-- ========== total cost items - just for credential, AND child items ========== 
	left join (
		Select condProfParentEntityBaseId, Sum(TotalCostItems) As Total from [CostProfile_SummaryForSearch] 
		where condProfParentEntityTypeId =1  and TotalCostItems > 0
		group by condProfParentEntityBaseId
	) allCostProfiles	on base.Id = allCostProfiles.condProfParentEntityBaseId
	-- =======================================================
	--23-01-27 dump this
	--left join (SELECT [ParentEntityUid] ,sum([AverageMinutes]) as [AverageMinutes] 
	--FROM [dbo].[Entity_Duration_EntityAverage] group by [ParentEntityUid]
	--)  duration on base.EntityUid = duration.ParentEntityUid 

		-- ========== check for a verifiable badge claim ========== 
	Left Join (
		SELECT c.CredentialId, count(*) as Total
		FROM [dbo].[Entity.VerificationProfile] a
		inner join entity vpEntity on a.RowId = vpEntity.EntityUid
		Inner join [Entity.Credential] c on vpEntity.Id = c.EntityId
		inner join  [dbo].[Entity.Property] ep  on vpEntity.Id = ep.EntityId
		inner join [Codes.PropertyValue] b on ep.PropertyValueId = b.Id
		where 	b.SchemaName = 'claimType:BadgeClaim'
		group by c.CredentialId

	) badgeClaims on base.Id = badgeClaims.CredentialId

	--===================================================
	--left join SearchPendingReindex pending on base.Id = pending.RecordId And pending.EntityTypeId = 1 and pending.StatusId = 1
	Left Join [Credential.SummaryCache] cache on base.Id = cache.CredentialId

	where isnull(base.EntityStateId, 2) > 1
	--AND 
	--(
	--		@CredentialId = 0 
	--	OR  base.Id = @CredentialId
	--	OR (@CredentialId = -1 AND pending.RecordId = base.Id)
	--)
	AND 
	(	
			(@CredentialId = 0 )
		OR	(@CredentialId > 0 AND base.[Id] = @CredentialId AND cache.CredentialId Is null )
		OR	(@CredentialId = -1 AND cache.CredentialId Is null )
	)

	print 'added credentials ' + convert(varchar, @@ROWCOUNT)

GO
grant execute on [Populate_Credential_SummaryCache] to public
go


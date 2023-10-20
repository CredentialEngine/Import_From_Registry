use credfinder_github
go

/****** Object:  View [dbo].[Organization_Summary_export]    Script Date: 8/16/2017 9:51:51 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

/*


SELECT top(1000)
[Id] as FinderId

	,[EntityId]
	,[CTID]
	,[EntityStateId]
	,[Name]
	,[Description]
	,OrganizationClass
	-- ,[EntityTypeId]

	--  ,[LifeCycleStatusTypeId]
	,[LifeCycleStatusType]
	,[OrganizationType]
	,[SectorType]
	,[Address1]
	-- ,[Address2]
	,[City]
	,[Region]
	,[PostalCode]
	,[Country]
	,[Latitude]
	,[Longitude]
	,[SubjectWebpage]
	,[ImageURL]
	--  ,[RowId]
	--  ,[cerEnvelopeUrl]
	,[AvailabilityListing]
	-- ,[IsAQAOrganization]
	,[Created]
	,[LastUpdated]
	,[JsonProperties]

  FROM [dbo].[Organization_Summary_export]

  order by name
GO


GO


where CredentialCount> 0

*/

/*
	Organization Summary

	Mods
	23-09-02 mparsons - new version for exporting
*/
create VIEW [dbo].[Organization_Summary_export]
AS
SELECT        
	isnull(o.Id, -10) As Id, 
	e.Id as EntityId,
	isnull(o.CTID,'') As CTID, 
	o.Name, o.Description
	,o.EntityTypeId
	,cet.Title as OrganizationClass
	,o.EntityStateId
	,case when IsNull(o.LifeCycleStatusTypeId,0) > 0 then o.LifeCycleStatusTypeId
	else 2648 end As LifeCycleStatusTypeId --default to production value for now

	,case when IsNull(o.LifeCycleStatusTypeId,0) > 0 then cpv.Title
	else 'Active' end As LifeCycleStatusType --
	--Address1, Address2, City, Region, PostalCode, Country, o.Latitude, o.Longitude,
	,oa.Address1,oa.City, oa.Region, oa.PostalCode, oa.Country, oa.Latitude, oa.Longitude,
	--need to get from Entity.Reference now!
	--'' as Email, 

	--'' As MainPhoneNumber, 
	o.SubjectWebpage
	, ImageURL
	, o.RowId,

	--o.CredentialRegistryId,

	isnull(o.AvailabilityListing,'') As AvailabilityListing, 

	--isnull(orgTypesQA.ServiceCount, 0) As QAServiceCount,
	--case when isnull(orgTypesQA.ServiceCount, 0) > 0 then 1 else 0 end As IsAQAOrganization,
	case 
		when isnull(ISQAOrganization,0) =1 then 1
		when isnull(orgTypesQA.propertyCount, 0) > 0 then 1 
		else 0 end As IsAQAOrganization,

	--isnull(orgMbrs.Total,0) as OrgMbrsCount,
	o.Created, o.LastUpdated
 -- o.CreatedById, o.LastUpdatedById
 	--json stuff in progress
	,o.JsonProperties

 	--counts
	--,isnull(creds.Owns, 0) As CredentialCount
	--,isnull(asmts.Owns, 0) As AssessmentCount
	--,isnull(lopps.Owns, 0) As LearningOpportunityCount
	--,isnull(frameworks.Owns, 0) As FrameworkCount

	--,isnull(pathways.Owns, 0) As PathwayCount
	--,isnull(pathwaySets.Owns, 0) As PathwaySetCount
	--,isnull(tvps.Owns, 0) As TransferValueCount

	,CASE
          WHEN OrgType IS NULL THEN ''
          WHEN len(OrgType) = 0 THEN ''
          ELSE left(OrgType,len(OrgType)-1)
    END AS OrganizationType
    ,CASE
          WHEN SectorType IS NULL THEN ''
          WHEN len(SectorType) = 0 THEN ''
          ELSE left(SectorType,len(SectorType)-1)
    END AS SectorType

FROM            dbo.Organization o
Inner join Entity e on o.RowId = e.EntityUid
left join [codes.PropertyValue] cpv on o.LifeCycleStatusTypeId = cpv.Id
--Left Join EntityProperty_Summary	statusProperty on o.RowId = statusProperty.EntityUid and statusProperty.CategoryId = 84
left join [Codes.EntityTypes] cet on o.EntityTypeId = cet.Id
-- 17-10-02 mparsons - may be better to get a count, and retrieve server side as needed?
--20-07-17 mparsons - or store as json on organization and expand as needed for elastic
left join [Entity_AddressSummary] oa on oa.EntityAddressId = (
    select top 1 eas.EntityAddressId from [Entity_AddressSummary] eas
    where eas.EntityBaseId = o.Id and eas.EntityTypeId = 2
    order by eas.Created desc
    --limit 1
)
Left Join (
	select OwningAgentUid, count(*) As Owns from [Credential] 
	where EntityStateId = 3
	group by OwningAgentUid 
	) creds on o.RowId = creds.OwningAgentUid
--Left Join (
--	select AgentUid, count(*) As CredentialCount from [CredentialAgentRelationships_Summary] 
--	where RelationshipTypeId in ( 6)
--	group by AgentUid 
--	) creds on o.RowId = creds.AgentUid

left join (
	select EntityBaseId, count(*) As propertyCount from [dbo].[EntityProperty_Summary] eps 
	where [CategoryId]= 7 and [PropertySchemaName]= 'orgType:QualityAssurance' group by EntityBaseId
	) orgTypesQA on o.Id = orgTypesQA.EntityBaseId
-- ==================================================	
-- counts 
----Left Join (
----	select OwningAgentUid, count(*) As Owns from Assessment 
----	where EntityStateId = 3
----	group by OwningAgentUid 
----	) asmts on o.RowId = asmts.OwningAgentUid
------	
------21-04-30 chg to include offers - or do we really need this here? Not used in elastic
----Left Join (

----	select c.AgentUid,  count(*) As Owns from LearningOpportunity a
----	inner join Entity b on a.RowId = b.EntityUid
----	inner join [Entity.AgentRelationship] c on b.id = c.EntityId
----	--inner join Organization d on c.AgentUid = d.RowId
----	where a.EntityStateId >= 2 and c.RelationshipTypeId in (6,7)
----	--and a.id in (754,756, 757, 687,781)
----	group by c.AgentUid 
----) lopps on  o.RowId = lopps.AgentUid
------	
------	
----Left Join (
----	select OrganizationCTID, count(*) As Owns from CompetencyFramework 
----	where EntityStateId = 3
----	group by OrganizationCTID 
----	) frameworks on o.CTID = frameworks.OrganizationCTID
------	
----Left Join (
----	select OwningAgentUid, count(*) As Owns from Pathway 
----	where EntityStateId = 3
----	group by OwningAgentUid 
----	) pathways on o.RowId = pathways.OwningAgentUid
------	
----Left Join (
----	select OwningAgentUid, count(*) As Owns from PathwaySet 
----	where EntityStateId = 3
----	group by OwningAgentUid 
----	) pathwaySets on o.RowId = pathwaySets.OwningAgentUid
------	
----Left Join (
----	select OwningAgentUid, count(*) As Owns from TransferValueProfile 
----	where EntityStateId = 3
----	group by OwningAgentUid 
----	) tvps on o.RowId = tvps.OwningAgentUid


--Left Join (
--	select ParentOrgId, count(*) As Total from [Organization.Member] 
--	group by ParentOrgId 
--	) orgMbrs on o.Id = orgMbrs.ParentOrgId
--
CROSS APPLY (
    SELECT cp.Property + '| '
    FROM dbo.EntityProperty_Summary cp  
    WHERE cp.CategoryId = 7
	and e.Id = cp.EntityId
	--Order by cp.PropertyValueId
    FOR XML Path('') 
	) OT (OrgType)
	--
CROSS APPLY (
    SELECT cp.Property + '| '
    FROM dbo.EntityProperty_Summary cp  
    WHERE cp.CategoryId = 30
	and e.Id = cp.EntityId
	--Order by cp.PropertyValueId
    FOR XML Path('') 
	) ST (SectorType)

where o.EntityStateId >= 2

GO
grant select on [Organization_Summary_export] to public
go



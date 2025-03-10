use credFinder
go

USE [sandbox_credFinder]
GO
--use staging_credFinder
--go

/*


SELECT a.[Id]
      ,a.[EntityId]
      ,a.[EntityTypeId]
      ,a.[ResourceId]
      ,a.[RelationshipTypeId], a.RelationshipType
      ,a.[Created]
      ,a.[EntityType]
      ,a.[Name]
      ,a.[Description]
      ,a.[CTID]
      ,a.[SubjectWebpage]
      ,a.[EntityStateId]
      ,a.[EntityUid]
      ,a.[ResourceOwningOrgId]
      ,a.[OwningOrgId]
      ,a.[ResourceOrganizationName]
      ,a.[PublishedByOrgId]
      ,a.[ParentEntityTypeId]
      ,a.[ParentEntityStateId]
      ,a.[ParentEntityId]
      ,a.[ParentName]
      ,a.[ParentCTID]
      ,a.[ParentDescription]
      ,a.[ParentPrimaryOrganizationId]
      ,a.[ParentPrimaryOrgEntityStateId]
      ,a.[ParentPrimaryOrganizationName]
      ,a.[Organization]
      ,a.[ParentPrimaryOrganizationDesc]
      ,a.[EntityParentUid]
  FROM [dbo].[Entity.HasResourceSummary] a
--  inner join LearningOpportunity lopp on a.EntityUID = lopp.RowId
  --inner join transferValueProfile tv on a.EntityUid = tv.RowId

  where a.[RelationshipTypeId]in (17,18)
GO





*/
ALTER VIEW [dbo].[Entity.HasResourceSummary] 
AS
SELECT a.[Id]
	,a.[EntityId]
	,a.[EntityTypeId]
	,a.[ResourceId]
	,a.RelationshipTypeId
	,codesHRRT.Name AS RelationshipType

	,a.[Created]
	-- NOTE: target resource
	,ec.EntityType
	,ec.Name
	,ec.Description
	,ec.CTID
	,ec.SubjectWebpage
	,ec.EntityStateId
	,ec.EntityUid
	,ec.OwningOrgId		as ResourceOwningOrgId
	--TEMP
	,ec.OwningOrgId		as OwningOrgId
	,IsNull(c.Name,'')	as ResourceOrganizationName
	,ec.PublishedByOrgId	as PublishedByOrgId 
	   
	--NOTE: this the parent of Entity.HasResource
	,parentCache.EntityTypeId as ParentEntityTypeId
	,parentCache.EntityStateId as ParentEntityStateId
	,parentCache.Id		as ParentEntityId
	,parentCache.Name		as ParentName
	,parentCache.CTID		as ParentCTID
	,parentCache.Description	as ParentDescription
	  
	--
	,parentOrg.Id				As ParentPrimaryOrganizationId
	,parentOrg.EntityStateId	as ParentPrimaryOrgEntityStateId
	,parentOrg.[Name]			As ParentPrimaryOrganizationName 
	--TEMP
	,parentOrg.[Name] As Organization 
	,parentOrg.Description	As ParentPrimaryOrganizationDesc
	,ec.parentEntityUid as EntityParentUid --to get the UId of the parent in case of competencies and concepts

  FROM [dbo].[Entity.HasResource] a 
  --inner join Entity e on a.EntityId = e.Id
  --entity_cache for the destination resource
  inner join Entity_Cache ec on a.EntityTypeId = ec.EntityTypeId and a.ResourceId = ec.BaseId
  Left Join Organization c on ec.OwningOrgId = c.Id 

  --should we do the joins for parent here or have a separate process
  inner join Entity_Cache parentCache on a.EntityId = parentCache.Id
  Left Join Organization parentOrg on parentCache.OwningOrgId = parentOrg.Id 
  Left Join [Codes.HasResourceRelationshipType] codesHRRT on a.RelationshipTypeId = codesHRRT.Id

  where ec.EntityStateId > 1


GO
grant select on [Entity.HasResourceSummary] to public 
go



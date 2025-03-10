use credFinder
GO
--USE staging_credFinder
--GO
use sandbox_credfinder
go

/****** Object:  View [dbo].[Entity_Relationship_AgentSummary]    Script Date: 8/27/2017 10:24:10 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


/*

-- by entity
SELECT [EntityAgentRelationshipId]
      ,[RowId]
      ,[EntityId]
      ,[SourceEntityUid]
      ,[SourceEntityTypeId]
      ,[SourceEntityType]
      ,[SourceEntityBaseId]
      ,[SourceEntityName]
			,SourceEntityDescription
			,SourceEntityUrl, SourceEntityImageUrl
      ,[RelationshipTypeId]
      ,[IsInverseRole]
      ,[SourceToAgentRelationship]
      ,[AgentToSourceRelationship]
      ,[ActingAgentUid]
      ,[ActingAgentTypeId]
      ,[ActingAgentEntityType]
      ,[AgentName]
      ,[AgentRelativeId]
      ,[AgentDescription]
      ,[AgentUrl]
      ,[AgentImageUrl]
      ,[Created]      ,[LastUpdated]
  FROM [dbo].[Entity_Relationship_AgentSummary]
	where RelationshipTypeId in (20,21)
	and ActingAgentUid = 'bf5d5693-2706-467c-8081-46ee8a0ef578'


*/
/*
Modifications
17-07-26 mparsons - removed union to person
									- removed joins to Entity for agent (and related code table)
17-12-01 mparsons - added entitystateId for agent. May want to always restrict this to only values of 2 or 3 (no pending)
									- would need audits to report on missing downloads though
18-09-11 mparsons - remove use of Entity_Cache
23-07-11 mparsons - added Entity_Cache back to save the many left joins and improve flexibility
24-12-16 mparsons - Saved a Join by removing the Entity join.
25-02-25 mparsons - Just a noted that there was an issue and the '24-12-16' update addressed it, but was not updated in production!
25-03-02 mparsons - changed to use Org directly rather than through a 'very' old view
*/
Alter VIEW [dbo].[Entity_Relationship_AgentSummary]
AS
SELECT        
		IsNull(base.Id, -1) AS EntityAgentRelationshipId, 
		base.RowId,
		base.EntityId, 

		es.EntityUid	AS SourceEntityUid, 
		es.EntityTypeId	AS SourceEntityTypeId, 
		es.EntityType	AS SourceEntityType, 
		es.BaseId		AS SourceEntityBaseId,
		es.Name			AS SourceEntityName,

		es.description as SourceEntityDescription,
		es.SubjectWebpage as SourceEntityUrl,
		isnull(es.ImageUrl,'') as SourceEntityImageUrl,
		es.EntityStateId as SourceEntityStateId,
		owingOrg.Name as SourceOwningOrganizationName,
		es.owningOrgId as SourceOwningOrganizationId, 

		base.RelationshipTypeId, 
		base.IsInverseRole,
		codes.Title			AS SourceToAgentRelationship, 
		codes.ReverseRelation AS AgentToSourceRelationship, 
		codes.[Description] as RelationshipDescription,
		codes.SchemaTag,
		codes.ReverseSchemaTag,
		codes.IsQARole,
		codes.IsOwnerAgentRole,

		base.AgentUid	AS ActingAgentUid, 
		2				AS ActingAgentTypeId, 
		--ActingAgentEntityType.Title AS ActingAgentEntityType,
		'Organization'	as ActingAgentEntityType, 
		agent.Name		as AgentName
		,agent.Id		as AgentRelativeId
		,agent.Description As AgentDescription
		,agent.SubjectWebpage As AgentUrl
		,agent.ImageURL as AgentImageUrl
		,agent.CTID
		,agent.EntityStateId
		,base.Created
		,base.LastUpdated

FROM            
	dbo.[Entity.AgentRelationship] base
INNER JOIN dbo.Entity_Cache es			ON base.EntityId = es.Id 
Left Join Organization owingOrg			on es.OwningOrgId = owingOrg.Id
INNER JOIN dbo.Organization agent		ON base.AgentUid = agent.RowId 
INNER JOIN dbo.[Codes.CredentialAgentRelationship]	codes	ON base.RelationshipTypeId = codes.Id

where codes.IsActive = 1
and IsNull(agent.EntityStateId, 1) > 1

go
grant select on Entity_Relationship_AgentSummary to public


GO

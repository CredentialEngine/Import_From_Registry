USE [credFinder]
GO

/*

SELECT [Id]
      ,[EntityTypeId]
      ,[EntityBaseId]
      ,[EntityBaseName]
      ,[ConnectionType]
      ,[ConnectionTypeId]
      ,[ConditionSubTypeId]
      ,[LearningOpportunityId]
      ,[LearningOpportunityName]
      ,[lname]
      ,[LoppEntityStateId]
      ,[loppOrganization]
      ,[loppOrgid]
      ,[AssessmentId]
      ,[AssessmentName2]
      ,[AssesmentEntityStateId]
      ,[AssessmentName]
      ,[asmtOrganization]
      ,[asmtOrgid]
      ,[CredentialId]
      ,[CredentialName2]
      ,[CredentialEntityStateId]
      ,[CredentialName]
      ,[credOrganization]
      ,[credOrgid]
  FROM [dbo].[Entity_ConditionProfilesConnectionsSummary]
where EntityTypeId= 3
  order by 2,3



*/
/*
Used in the assessment.elastic, credential.elastic, and lopp.elastic searches
**** While the performance seems OK, this view is quite complicated  ****

*/
Alter VIEW [dbo].[Entity_ConditionProfilesConnectionsSummary]
AS

SELECT DISTINCT 
TOP (100) PERCENT 
	connectionParentEntity.Id, connectionParentEntity.EntityTypeId, connectionParentEntity.EntityBaseId, connectionParentEntity.EntityBaseName
	, ccpt.Title AS ConnectionType
	, ecp.ConnectionTypeId
	, isnull(ecp.ConditionSubTypeId,1) as ConditionSubTypeId
	, Isnull(elo.LearningOpportunityId, 0) As LearningOpportunityId
	, Case when Isnull(elo.LearningOpportunityId, 0) > 0 AND Len(Isnull(lopp.Organization,'')) > 0 then lopp.Name + ' [ ' + lopp.Organization + ' ] ' else lopp.Name end as LearningOpportunityName
	, lopp.Name AS lname
	, lopp.EntityStateId AS LoppEntityStateId
	, lopp.Organization as loppOrganization, IsNull(lopp.OrgId,0) As loppOrgid

	, IsNull(eas.AssessmentId, 0) As AssessmentId, asmt.Name AS AssessmentName2, asmt.EntityStateId AS AssesmentEntityStateId
	, Case when Isnull(eas.AssessmentId, 0) > 0 AND Len(Isnull(asmt.Organization,'')) > 0 then asmt.Name + ' [ ' + asmt.Organization  + ' ] ' else asmt.Name end as AssessmentName
	, asmt.Organization as asmtOrganization, IsNull(asmt.OrgId,0) As asmtOrgid

	, IsNull(ecr.CredentialId, 0) As CredentialId, cred.Name AS CredentialName2, cred.EntityStateId AS CredentialEntityStateId
	, Case when Isnull(ecr.CredentialId, 0) > 0 AND Len(Isnull(cred.OwningOrganization,'')) > 0 then cred.Name + ' [ ' + cred.OwningOrganization  + ' ] ' else cred.Name end as CredentialName
	, cred.OwningOrganization as credOrganization, IsNull(cred.OwningOrganizationId,0) As credOrgid

FROM dbo.[Codes.ConditionProfileType] ccpt
INNER JOIN dbo.Entity	connectionParentEntity
INNER JOIN dbo.[Entity.ConditionProfile] ecp ON connectionParentEntity.Id = ecp.EntityId 
INNER JOIN dbo.Entity AS ConditionProfileEntity 
					ON ecp.RowId = ConditionProfileEntity.EntityUid 
					ON ccpt.Id = ecp.ConnectionTypeId 
	LEFT OUTER JOIN dbo.LearningOpportunity_Summary lopp
		INNER JOIN dbo.[Entity.LearningOpportunity] elo
					ON lopp.Id = elo.LearningOpportunityId  And lopp.EntityStateId > 1
					ON ConditionProfileEntity.Id = elo.EntityId 
	LEFT OUTER JOIN dbo.Credential_Summary cred
		INNER JOIN dbo.[Entity.Credential] ecr 
					ON cred.Id = ecr.CredentialId And cred.EntityStateId > 1
					ON ConditionProfileEntity.Id = ecr.EntityId 
	LEFT OUTER JOIN dbo.Assessment_Summary asmt  
		INNER JOIN dbo.[Entity.Assessment] eas 
					ON asmt.Id = eas.AssessmentId And asmt.EntityStateId > 1
					ON ConditionProfileEntity.Id = eas.EntityId

WHERE     
	(ecp.ConditionSubTypeId IN (2, 3, 4) )   
	 
AND	( 
		(ISNULL(eas.AssessmentId, 0) > 0 AND asmt.EntityStateId > 1 ) 
 OR		(ISNULL(elo.LearningOpportunityId, 0) > 0 AND lopp.EntityStateId > 1 ) 
 OR		(ISNULL(ecr.CredentialId, 0) > 0 AND cred.EntityStateId > 1 )
 )

ORDER BY 
	connectionParentEntity.EntityTypeId, 
	connectionParentEntity.EntityBaseId, 
	connectionParentEntity.EntityBaseName, 
	ConnectionType

Go


grant select on [Entity_ConditionProfilesConnectionsSummary] to public
go
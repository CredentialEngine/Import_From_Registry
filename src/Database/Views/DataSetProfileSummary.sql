Use credFinder
go
use sandbox_credFinder
go
--use staging_credFinder	
--go
--use flstaging_credFinder	
--go

/****** Object:  View [dbo].[DataSetProfileSummary]    Script Date: 5/31/2021 2:14:30 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
--Drop VIEW [dbo].[DataSetProfileSummary]
/*
USE [sandbox_credFinder]
GO



SELECT top (500)
[Id]
      ,[RowId]
      ,[EntityStateId]
      ,[CTID]
      ,[Name]
      ,[Description]
      ,[Source]
      ,[DataProviderUID]
      ,[DataProviderName]
      ,[DataProviderId]
      ,[DataProviderCTID]
      ,[EntityId]
      ,[Created]
      ,[LastUpdated]
      ,[InternalDSPEntityId]
      ,[DataSuppressionPolicy]

      ,[SubjectIdentification]
      ,[DistributionFile]
      ,[CredentialId]
      ,[AssessmentId]
      ,[LearningOpportunityId]
      ,[Credentials]
      ,[LearningOpportunities]
	  ,DataSetTimePeriodJson
  FROM [dbo].[DataSetProfileSummary]
  where DataSetTimePeriodJson like '%employment%'
GO


  where CredentialId is not null
GO





*/
Alter VIEW [dbo].[DataSetProfileSummary]
AS
SELECT        
	base.Id, base.RowId, base.EntityStateId, 
	base.CTID, 
	base.Name, 
	base.Description, 
	base.Source, 
	base.DataProviderUID, 
	isnull(d.Name,'') AS DataProviderName, 
	d.Id AS DataProviderId, 
	d.CTID as DataProviderCTID,
	b.Id as EntityId, 

	base.Created, base.LastUpdated,
	-- if is not null, then an internal DSP
	--	and what does this mean?
	edsp.EntityId as InternalDSPEntityId,
	base.DataSuppressionPolicy,
	base.SubjectIdentification,
	base.DistributionFile,
	base.[DateEffective],
	base.DataSetTimePeriodJson,
	base.[PublicationStatusType],
	--
	ec.CacheDate As EntityLastUpdated,
	isnull(ec.ResourceDetail,'') as ResourceDetail,
	-- ============================
	c.CredentialId,
	ea.AssessmentId,
	el.LearningOpportunityId
	--typically only one
	, CASE
			WHEN Credentials IS NULL THEN ''
			WHEN len(Credentials) = 0 THEN ''
			ELSE left(Credentials,len(Credentials)-1)
		END AS Credentials
	, CASE
			WHEN LearningOpportunities IS NULL THEN ''
			WHEN len(LearningOpportunities) = 0 THEN ''
			ELSE left(LearningOpportunities,len(LearningOpportunities)-1)
		END AS LearningOpportunities


FROM dbo.DataSetProfile base  
INNER JOIN  dbo.Entity b on base.RowId = b.EntityUid
Inner Join [Entity_Cache] ec on base.RowId = ec.EntityUID
Left Join Organization d on base.DataProviderUID = d.RowId
-- use a left join on [Entity.DataSetProfile] to control getting external only or all, where eDSP.id is not null
Left JOIN dbo.[Entity.DataSetProfile] AS edsp  ON base.Id = edsp.DataSetProfileId


--
Left JOIN dbo.[Entity.Credential] AS c  ON b.Id = c.EntityId 
Left JOIN dbo.[Entity.Assessment] AS ea  ON b.Id = ea.EntityId 
Left JOIN dbo.[Entity.LearningOpportunity] AS el  ON b.Id = el.EntityId 

--

CROSS APPLY (
	SELECT 
		convert(varchar,caec.CredentialId) + '~ ' + convert(varchar,cac.Name) + ', '
	FROM dbo.[Entity.Credential] caec
	Inner Join Credential cac on caec.credentialId = cac.Id
	INNER JOIN dbo.Entity cae ON caec.EntityId = cae.Id 
	WHERE (base.EntityStateId = 3) 
	AND b.Id = caec.EntityId
	FOR XML Path('') 
) creds (Credentials)


CROSS APPLY (
	SELECT 
		convert(varchar,caec.LearningOpportunityId) + '~ ' + convert(varchar,cac.Name) + ', '
	FROM dbo.[Entity.LearningOpportunity] caec
	Inner Join LearningOpportunity cac on caec.LearningOpportunityId = cac.Id
	INNER JOIN dbo.Entity cae ON caec.EntityId = cae.Id 
	WHERE (base.EntityStateId = 3) 
	AND b.Id = caec.EntityId
	FOR XML Path('') 
) lopps (LearningOpportunities)

where base.EntityStateId = 3
GO
grant select on [DataSetProfileSummary] to public
go


/*
14459~ base.S. in Nursing


*/
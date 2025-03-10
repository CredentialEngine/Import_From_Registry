--USE credFinder
--GO

use sandbox_credFinder
go
--use staging_credFinder
--go

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



/*

USE credFinder
GO


SELECT 
[Id],
    --  ,[RowId]
      [Name]
    --  ,[ProfileName]
      ,[CTID]
      --,[FEIN]
      --,[DUNS]
      --,[OPEID]
      ,[Webpage]
	  ,Description
      ,[PrimaryEmail]
     -- ,[OrganizationSectorUri]

      --,[StatusUri]
      --,[Created]

      --,[LastUpdated]

      --,[IsActive]
      --,[ExternalIdentifier]
    
  FROM [dbo].[OrganizationSummary]
GO

*/

/*
Organization Summary from related Accounts database
- Primarily used in joins for reports so that each doesn't have to have the accounts org with 
*/
Create View [dbo].[Accounts.OrganizationSummary]
as 

SELECT a.[Id]
      ,a.[RowId]
      ,a.[Name]
      ,a.[ProfileName]
      ,a.[CTID]
      ,a.[FEIN]
      ,a.[DUNS]
      ,a.[OPEID]
      ,a.[Url] as Webpage
	  ,a.Description
      ,a.[PrimaryPhoneNumber]
      ,a.[PrimaryPhoneExtension]
      ,a.[SecondaryPhoneNumber]
      ,a.[SecondaryPhoneExtension]
      ,a.[PrimaryEmail]
      ,a.[OrganizationSectorUri]
	  --tempting to include this?
      --,a.[PublishingApiKey]
      ,a.[StatusUri]
      ,a.[Created]
     -- ,a.[CreatedById]
      ,a.[LastUpdated]
    --  ,a.[LastUpdatedById]
      ,a.[IsActive]

  --FROM [CE_Accounts].[dbo].[Organization] a
    FROM [sandbox_CE_Accounts].[dbo].[Organization] a
  --	FROM [staging_CE_Accounts].[dbo].[Organization] a

  --confirm that IsActive is better than StatusUri (where a change could result in being reset to submitted.
  --22-05-01 does seem inconsistent
  --			may want to get all. If exists in finder, need to match one in accounts
  Where 
	--a.IsActive = 1
	a.StatusUri = 'status:Active'

GO
grant select on [Accounts.OrganizationSummary] to public
go

 
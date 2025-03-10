USE [credFinder]
GO

use ctdlEditor
go

/****** Object:  View [dbo].[CIPCodeSummary]    Script Date: 5/26/2020 3:40:50 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

--
--view to the current CIPCode table in CE_ExternalData
Create View [dbo].[CIPCodeSummary] 
As
SELECT  [Id]
      ,[CIPFamily]
      ,[CIPCode]
      ,[CIPTitle]
      ,[CIPDefinition]
      ,[CrossReferences]
      ,[Examples]
      ,[Url]
  FROM CE_ExternalData.[dbo].CIPCodeSummary
GO
grant select on [CIPCodeSummary] to public
go




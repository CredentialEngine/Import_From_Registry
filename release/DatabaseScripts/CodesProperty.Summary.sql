use credfinder_github
go
/****** Object:  View [dbo].[CodesProperty.Summary]    Script Date: 1/24/2018 2:34:52 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

/*
USE [CTI]
GO

SELECT [CategoryId]
      ,[Category]
      ,[CategorySchemaName]
      ,[CategorySchemaUrl]
      ,PropertyId,[Property],Totals, PropertyDescription, SortOrder
      ,[PropertySchemaName],ParentSchemaName
      ,[PropertySchemaUrl]
  FROM [dbo].[CodesProperty.Summary]
  where CategoryIsActive = 1 
	and CategoryId = 2
order by Category, SortOrder, Property


*/
Alter VIEW [dbo].[CodesProperty.Summary]
AS
SELECT       
--workaround to get entity frameworks to recognize it as a unique key for the view
	isnull(prop.Id, -1) As PK, 
	propCat.Id as CategoryId, 
	propCat.Title AS Category, 
	propCat.SchemaName AS CategorySchemaName, 
	propCat.SchemaUrl AS CategorySchemaUrl, 
	propCat.IsActive As CategoryIsActive,
	prop.Id As PropertyId, 
	prop.Title AS Property, 
	prop.Description as PropertyDescription,
	prop.SortOrder,
	prop.SchemaName AS PropertySchemaName, 
	prop.ParentSchemaName,
	prop.SchemaUrl AS PropertySchemaUrl, 
	isnull(prop.Totals,0) As Totals
FROM dbo.[Codes.PropertyCategory] propCat
Left JOIN dbo.[Codes.PropertyValue] prop ON propCat.Id = prop.CategoryId
where propCat.IsActive = 1
AND prop.IsActive = 1
AND prop.CategoryId <> 6 AND prop.CategoryId <> 15

UNION 

Select
isnull(prop.Id, -1) As PK,  --this result in dups
	6 as CategoryId, 
	'Agent Role Type' AS Category, 	
	'serviceType' AS CategorySchemaName, 
	'' AS CategorySchemaUrl, 
	convert(bit,1) As CategoryIsActive,
	prop.Id As PropertyId, 
	prop.Title AS Property, 
	prop.Description as PropertyDescription,
	0 as SortOrder,
	prop.SchemaTag AS PropertySchemaName, 
	'' As ParentSchemaName,
	'' AS PropertySchemaUrl, 
	isnull(prop.Totals,0) As Totals
from [Codes.CredentialAgentRelationship] prop
where prop.IsActive = 1
And prop.id < 20

UNION 

Select
isnull(prop.Id, -1) As PK, --this should be OK
	15 as CategoryId, 
	'Condition Profile' AS Category, 	
	'conditionProfileType' AS CategorySchemaName, 
	'' AS CategorySchemaUrl, 
	convert(bit,1) As CategoryIsActive,
	prop.Id As PropertyId, 
	prop.Title AS Property, 
	prop.Description as PropertyDescription,
	0 as SortOrder,
	prop.[SchemaName] AS PropertySchemaName, 
	'' As ParentSchemaName,
	'' AS PropertySchemaUrl, 
	isnull(prop.Totals,0) As Totals
from [Codes.ConditionProfileType] prop
where prop.IsActive = 1


GO


grant select on [CodesProperty.Summary] to public
go
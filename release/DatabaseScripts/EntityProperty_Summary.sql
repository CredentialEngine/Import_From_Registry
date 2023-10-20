use credfinder_github
go
/****** Object:  View [dbo].[EntityProperty_Summary]    Script Date: 8/16/2017 9:53:33 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

/*

SELECT [EntityId], EntityUid, EntityBaseId
      ,[EntityTypeId]
    --  ,[ParentTypeId]
      ,[Title]
      ,[PropertyValueId]
	  ,EntityPropertyId
      ,[Property], PropertySchemaName, ParentSchemaName
      ,[CategoryId]
      ,[Category]
      ,[Created]
      ,[Description]
  FROM [dbo].[EntityProperty_Summary]
  where CategoryId = 4
   AND EntityTypeId in (3,7)
order by 1, title


*/
Alter VIEW [dbo].[EntityProperty_Summary]
AS
SELECT   
	e.Id As EntityId, e.EntityUid, e.EntityTypeId, e.EntityBaseId, e.EntityBaseName,  
	cet.Title, 

	base.PropertyValueId, 
	base.Id as EntityPropertyId,
	cpv.Title AS Property, 
	cpv.Description, 
	cpv.SchemaName AS PropertySchemaName,
	cpv.ParentSchemaName, 
	cpv.CategoryId, 
	cpv.SortOrder,
	cpc.Title AS Category, 
	base.Created, 

	cpc.Description As CategoryDescription
	,cpv.IsActive
	,cpv.IsSubType1

FROM dbo.[Entity.Property] base
inner join dbo.Entity e on base.EntityId = e.Id
left JOIN dbo.[Codes.EntityTypes] cet ON e.EntityTypeId = cet.Id 
INNER JOIN dbo.[Codes.PropertyValue] cpv ON base.PropertyValueId = cpv.Id 
INNER JOIN dbo.[Codes.PropertyCategory] cpc ON cpv.CategoryId = cpc.Id
where cpv.IsActive = 1

GO
grant select on [EntityProperty_Summary] to public
go



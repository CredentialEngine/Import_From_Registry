use credfinder_github
go
/****** Object:  View [dbo].[Entity_Reference_Summary]    Script Date: 8/16/2017 4:32:45 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

/*
SELECT TOP 1000 
	[EntityId]
	,[EntityTypeId]
	,[EntityType]
	,[EntityUid]
	,[EntityBaseId]
	,[EntityName]
	,EntityReferenceId
	,CategoryId

	,[Title]
		,PropertyValueId, PropertyValue
	,[TextValue]
  FROM [dbo].[Entity_Reference_Summary]
  where  TextValue = '178317905'
	or [EntityId] = 4686
  order by EntityReferenceId,3, EntityName

*/
/*
--modifications
17-08-15 mparsons -replace entity_summary with Entity_cache
18-09-11 mparsons - remove use of Entity_Cache
*/
Alter VIEW [dbo].[Entity_Reference_Summary]
AS
SELECT 
	isnull(er.id, -10) as PK,       
	er.EntityId, 

	--base.Id, 
	base.EntityTypeId, 			-- not used by mapping
	SourceEntityType.Title As EntityType, 			-- not used by mapping
	base.EntityUid,				-- not used by mapping
	base.EntityBaseName as EntityName,	-- not used by mapping
	base.EntityBaseId, 

	er.Id As EntityReferenceId,
	er.CategoryId, 
	er.PropertyValueId,
	er.Title, 
	er.TextValue,
	case when Isnull(er.PropertyValueId,0) > 0 then codes.Title 
		else '' end As PropertyValue,
	case when Isnull(er.PropertyValueId,0) > 0 then codes.SchemaName 
		else '' end As PropertySchema

FROM dbo.Entity base
INNER JOIN dbo.[Entity.Reference] er			ON base.Id = er.EntityId
Left Join dbo.[Codes.PropertyValue] codes on er.PropertyValueId = codes.Id
INNER JOIN dbo.[Codes.EntityTypes] AS SourceEntityType		ON base.EntityTypeId = SourceEntityType.Id 
go

grant select on Entity_Reference_Summary to public
go



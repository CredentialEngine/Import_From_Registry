use credfinder_github
go
/****** Object:  View [dbo].[Entity.ProgramsCSV]    Script Date: 7/3/2018 5:24:47 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


/*
modifications

*/
Create VIEW [dbo].[Entity.ProgramsCSV]
AS

SELECT     distinct
base.EntityId, 
    CASE
          WHEN Programs IS NULL THEN ''
          WHEN len(Programs) = 0 THEN ''
          ELSE left(Programs,len(Programs)-1)
    END AS Programs,
    CASE
          WHEN CipList IS NULL THEN ''
          WHEN len(CipList) = 0 THEN ''
          ELSE left(CipList,len(CipList)-1)
    END AS CipList,
	CASE
          WHEN Others IS NULL THEN ''
          WHEN len(Others) = 0 THEN ''
          ELSE left(Others,len(Others)-1)
    END AS Others
FROM [dbo].[Entity_ReferenceFramework_Summary] base

CROSS APPLY (
--SELECT cp.FrameworkCode  + '~' + cp.Title + '| '
    SELECT convert(varchar, cp.CodedNotation) + '~' + cp.Name  + '| '
	 FROM dbo.[Entity_ReferenceFramework_Summary] cp 
 
    WHERE cp.CategoryId = 23
	and base.EntityId = cp.EntityId
    FOR XML Path('') ) D (Programs)
--' ( ' + cp.FrameworkCode + ' )'  +

CROSS APPLY (
    SELECT cp.CodedNotation  + '| '
    FROM dbo.[Entity_ReferenceFramework_Summary] cp 
    WHERE cp.CategoryId = 23
	and base.EntityId = cp.EntityId
	and isnull(cp.CodedNotation,'') <> ''

    FOR XML Path('') ) codes (CipList)


CROSS APPLY (
    SELECT cp.Name  + '| '
    FROM dbo.[Entity_ReferenceFramework_Summary] cp 

    WHERE cp.CategoryId = 23
	and base.EntityId = cp.EntityId
	and isnull(cp.CodedNotation,'') = ''

    FOR XML Path('') ) er (Others)

WHERE base.CategoryId = 23
and base.Id is not null
AND (Programs is not null Or Others is not null)

GO

grant select on [Entity.ProgramsCSV] to public
go

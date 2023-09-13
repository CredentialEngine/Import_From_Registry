USE [credFinder]
GO

--use staging_credFinder
--go

--use sandbox_credFinder
--go


/****** Object:  StoredProcedure [dbo].[Import.EntityResolution_SetResolved]    Script Date: 6/21/2018 3:58:41 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

/* 
SELECT a.[Id]

      ,[ReferencedId]
      ,[ReferencedCtid]
      ,[ReferencedEntityTypeId]
      ,[EntityUid]
      ,[IsResolved]
      ,[EntityBaseId], b.Name as credential 
  FROM [credFinder].[dbo].[Import.EntityResolution] a
  inner join credential b on a.EntityBaseId = b.id 
  where a.ReferencedEntityTypeId = 1 
  and b.EntityStateId = 3
  and a.[IsResolved] = 0
go

*/
ALTER  Procedure [dbo].[Import.EntityResolution_SetResolved]

AS
UPDATE [dbo].[Import.EntityResolution]
   SET [IsResolved] = 1
  FROM [dbo].[Import.EntityResolution] a
  inner join credential b on a.EntityBaseId = b.id 
  where a.ReferencedEntityTypeId = 1 
  and b.EntityStateId = 3
  and a.[IsResolved] = 0

  
UPDATE [dbo].[Import.EntityResolution]
   SET [IsResolved] = 1
  FROM [dbo].[Import.EntityResolution] a
  inner join Assessment b on a.EntityBaseId = b.id 
  where a.ReferencedEntityTypeId = 3  and b.EntityStateId = 3
  and a.[IsResolved] = 0

    
UPDATE [dbo].[Import.EntityResolution]
   SET [IsResolved] = 1
  FROM [dbo].[Import.EntityResolution] a
  inner join Organization b on a.EntityBaseId = b.id 
  where a.ReferencedEntityTypeId = 2  and b.EntityStateId = 3
  and a.[IsResolved] = 0

    
UPDATE [dbo].[Import.EntityResolution]
   SET [IsResolved] = 1
  FROM [dbo].[Import.EntityResolution] a
  inner join LearningOpportunity b on a.EntityBaseId = b.id 
  where a.ReferencedEntityTypeId = 7  and b.EntityStateId = 3
  and a.[IsResolved] = 0

UPDATE [dbo].[Import.EntityResolution]
   SET [IsResolved] = 1
  FROM [dbo].[Import.EntityResolution] a
  inner join ConditionManifest b on a.EntityBaseId = b.id 
  where a.ReferencedEntityTypeId = 19  and b.EntityStateId = 3
  and a.[IsResolved] = 0    

UPDATE [dbo].[Import.EntityResolution]
   SET [IsResolved] = 1
  FROM [dbo].[Import.EntityResolution] a
  inner join [CostManifest] b on a.EntityBaseId = b.id 
  where a.ReferencedEntityTypeId = 20  and b.EntityStateId = 3
  and a.[IsResolved] = 0
--
UPDATE [dbo].[Import.EntityResolution]
   SET [IsResolved] = 1
  FROM [dbo].[Import.EntityResolution] a
  inner join CompetencyFramework b on a.EntityBaseId = b.id 
  where a.ReferencedEntityTypeId = 10  and b.EntityStateId = 3
  and a.[IsResolved] = 0
--
UPDATE [dbo].[Import.EntityResolution]
   SET [IsResolved] = 1
  FROM [dbo].[Import.EntityResolution] a
  inner join Pathway b on a.EntityBaseId = b.id 
  where a.ReferencedEntityTypeId = 8  and b.EntityStateId = 3
  and a.[IsResolved] = 0
--
UPDATE [dbo].[Import.EntityResolution]
   SET [IsResolved] = 1
  FROM [dbo].[Import.EntityResolution] a
  inner join TransferValueProfile b on a.EntityBaseId = b.id 
  where a.ReferencedEntityTypeId = 26  and b.EntityStateId = 3
  and a.[IsResolved] = 0
GO

grant execute on [Import.EntityResolution_SetResolved] to public
go



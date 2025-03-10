

USE [sandbox_credFinder]
GO

/****** Object:  View [dbo].[DatasetProfile.HasMetric]  Script Date: 10/19/2024 4:29:48 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


Create View [dbo].[DatasetProfile.HasMetric] 
As

SELECT distinct 

      a.[DataSetProfileId]
      ,a.[IsObservationOf]
	  ,m.Name, m.Description, m.CTID, m.RowId, m.MetricTypeId, cpv.Title as MetricType
  FROM [sandbox_credFinder].[dbo].[DataSetProfile.Observation] a
  inner join Metric m on a.IsObservationOf = m.Id
  inner join [Codes.PropertyValue] cpv on m.MetricTypeId = cpv.Id

GO
grant select on [DatasetProfile.HasMetric] to public
go


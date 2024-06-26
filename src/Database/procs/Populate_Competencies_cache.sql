use credFinder
go

USE [sandbox_credFinder]
GO
--use staging_credFinder
--go

/****** Object:  StoredProcedure [dbo].[Populate_Competencies_cache]    Script Date: 8/3/2023 11:24:13 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/*
select *
from [ConditionProfile_Competencies_cache]


[Populate_Competencies_cache] 0

[Populate_Competencies_cache] -1

*/


/* =============================================
Description:      Populate_Competencies_cache
Options:

  @CredentialId - optional credentialId. 
				If non-zero will only replace the related row, otherwise replace all

  ------------------------------------------------------
Modifications
16-10-13 mparsons - new
21-08-10 mparsons - added the missing required competencies
22-11-18 mparsons - NOTE Referenced in CompetencyFrameworkManager.Search
				- Used in TagSet micro searches
23-07-11 mparsons - need to handle updates only. Starting by allowing -1, but treating as a full build
23-08-03 mparsons - added check for competency text length greater than 2000
*/

ALTER PROCEDURE [dbo].[Populate_Competencies_cache]
		@CredentialId	int = 0
As

SET NOCOUNT ON;

DECLARE
	@debugLevel      int

Set @debugLevel = 4

-- =================================


if @CredentialId > 0 begin
	print 'deleting credential ' + convert(varchar,@CredentialId)
	DELETE FROM [dbo].[ConditionProfile_Competencies_cache]
					WHERE [CredentialId] = @CredentialId

	print 'deleted competencies ' + convert(varchar, @@ROWCOUNT)
	end
else if @CredentialId = -1 begin
	print 'eventually want to able to do recent updates only. Added entity.competency Created, now what to delete? ' 
	print 'truncating ConditionProfile_Competencies_cache'
	truncate table ConditionProfile_Competencies_cache
		set @CredentialId= 0
	--OR
	--	DELETE D 
	---- Select D.CredentialId
	--	FROM [dbo].[ConditionProfile_Competencies_cache] D
	--	Inner Join [ConditionProfile_Assessments_Competencies_Summary] e on D.[CredentialId] = e.[CredentialId]
	--	WHERE e.[CompetencyCreated] > D.LastSyncDate

	end
else begin
		print 'truncating ConditionProfile_Competencies_cache'
		truncate table ConditionProfile_Competencies_cache
	end

	--add assessment related
	INSERT INTO [dbo].[ConditionProfile_Competencies_cache]
							([CredentialId]
							--,[ConnectionTypeId]
							--,[EntityConditionProfileRowId]
							,[nodeLevel]
							,SourceEntityTypeId
							,[SourceId]
							,[SourceName]
							,CompetencyFrameworkItemId
							,[Competency]
							,[Description])

	SELECT DISTINCT base.[CredentialId]
				--,0 as [ConnectionTypeId]
				--,'00000000-0000-0000-0000-000000000000'				As EntityConditionProfileRowId
				,base.[nodeLevel]
				,3
				,base.[AssessmentId] As SourceId
				,base.[Assessment]		As SourceName
				,base.CompetencyFrameworkItemId
				--,base.[Competency]
				,case when len(base.Competency) > 2000 then SUBSTRING(base.Competency,1,2000) else base.Competency end as Competency
				,base.TargetNodeDescription
			
		FROM [dbo].[ConditionProfile_Assessments_Competencies_Summary] base
		Left Join [ConditionProfile_Competencies_cache] cache on base.CredentialId = cache.CredentialId
	--where [CredentialId]= 62
	--where [AssessmentId]= 17
		where (@CredentialId = 0) 
		OR  (base.[CredentialId] = @CredentialId)
		OR	(@CredentialId = -1 AND cache.CredentialId Is null )


	print 'added assessment competencies: ' + convert(varchar, @@ROWCOUNT)

BEGIN TRY  

	-- add learning opp related
	INSERT INTO [dbo].[ConditionProfile_Competencies_cache]
							([CredentialId]
							--,[ConnectionTypeId]
							--,[EntityConditionProfileRowId]
							,[nodeLevel]
							,SourceEntityTypeId
							,[SourceId]
							,[SourceName]
							,CompetencyFrameworkItemId
							,[Competency]
							,[Description])


	SELECT DISTINCT [CredentialId]
				--,[ConnectionTypeId]
				--,[RowId]						As EntityConditionProfileRowId
				,[learningOppNode]	as [nodeLevel]
				,7
				,[LearningOpportunityId]	As SourceId
				,[LearningOpportunity]		As SourceName
				,CompetencyFrameworkItemId
				--,[Competency]
				,case when len(Competency) > 2000 then SUBSTRING(Competency,1,2000) else Competency end as Competency
				,TargetNodeDescription
--	select * 			
	FROM [dbo].[ConditionProfile_LearningOpp_Competencies_Summary]
	where (@CredentialId = 0) OR  ([CredentialId] = @CredentialId)
--if @@ROWCOUNT > 0 begin end
	print 'added learning opp competencies ' + convert(varchar, @@ROWCOUNT)

END TRY  
BEGIN CATCH  
     print 'Errors encountered in Populate_Competencies_cache ' 
	   --SELECT
    --ERROR_NUMBER() AS ErrorNumber,
    --ERROR_STATE() AS ErrorState,
    --ERROR_SEVERITY() AS ErrorSeverity,
    --ERROR_PROCEDURE() AS ErrorProcedure,
    --ERROR_LINE() AS ErrorLine,
    --ERROR_MESSAGE() AS ErrorMessage;

INSERT INTO [dbo].[MessageLog]
           ([Created],[Application],[Activity]
           ,[MessageType],[Message],[Description]
           ,[ActionByUserId],[ActivityObjectId]
           ,[RelatedUrl],[SessionId],[IPAddress],[Tags])
     VALUES
           (getdate(), 'CredentialFinder', 'Populate_Competencies_cache'
           ,'Error'
           ,ERROR_MESSAGE()
           ,'Errors encountered in Populate_Competencies_cache for ConditionProfile_LearningOpp_Competencies_Summary'  
           ,0
           ,0
           ,NULL,NULL,NULL,NULL)

END CATCH 
		

BEGIN TRY		
	-- add required competencies
	INSERT INTO [dbo].[ConditionProfile_Competencies_cache]
							([CredentialId]
							,[nodeLevel]
							,SourceEntityTypeId
							,[SourceId]
							,[SourceName]
							,CompetencyFrameworkItemId
							,[Competency]
							,[Description])

	SELECT DISTINCT [ParentEntityBaseId] as [CredentialId],
		'level1'--[nodeLevel]??
      ,1		--??
      ,[ParentEntityBaseId]	--SourceEntityTypeId??
      ,ParentEntityName		--??
      ,[CompetencyFrameworkId]
	  ,case when len([TargetNodeName]) > 2000 then SUBSTRING(TargetNodeName,1,2000) else TargetNodeName end as Competency
      --,[TargetNodeName]
	--  ,len([TargetNodeName])as cl
	  , '' as Description
  FROM [dbo].[ConditionProfile_RequiredCompetencies]
		where ParentEntityTypeId= 1
	--	and len([TargetNodeName]) > 1990
		AND (@CredentialId = 0 OR  [ParentEntityBaseId] = @CredentialId)

if @@ROWCOUNT > 0 begin
	print 'added required competencies ' + convert(varchar, @@ROWCOUNT)
end

END TRY  
BEGIN CATCH  
     print 'Errors encountered in Populate_Competencies_cache ' 
	   --SELECT
    --ERROR_NUMBER() AS ErrorNumber,
    --ERROR_STATE() AS ErrorState,
    --ERROR_SEVERITY() AS ErrorSeverity,
    --ERROR_PROCEDURE() AS ErrorProcedure,
    --ERROR_LINE() AS ErrorLine,
    --ERROR_MESSAGE() AS ErrorMessage;

INSERT INTO [dbo].[MessageLog]
           ([Created],[Application],[Activity]
           ,[MessageType],[Message],[Description]
           ,[ActionByUserId],[ActivityObjectId]
           ,[RelatedUrl],[SessionId],[IPAddress],[Tags])
     VALUES
           (getdate(), 'CredentialFinder', 'Populate_Competencies_cache'
           ,'Error'
           ,ERROR_MESSAGE()
           ,'Errors encountered in Populate_Competencies_cache for ConditionProfile_RequiredCompetencies'  
           ,0
           ,0
           ,NULL,NULL,NULL,NULL)

END CATCH 
			


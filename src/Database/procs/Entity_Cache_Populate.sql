use credFinder
go

--use sandbox_credFinder
--go

--use staging_credFinder
--go

--use credFinder_Prod
--GO

/****** Object:  StoredProcedure [dbo].[Entity_Cache_Populate]    Script Date: 9/27/2016 6:37:21 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/*
exec Entity_Cache_Populate 607

exec Entity_Cache_Populate 0

==================================
[Entity_Cache_Populate]
@EntityId	
-	>0, update a specific entity
-	-1 - use pending updates from SearchPendingReindex (so must run this before running the SearchPendingReindex)
-	 0 - truncate and rebuild

NOTES:
****This is mostly obsolete now. ****
Each resource type will do direct calls to update the entity cache. 
If this has be run or parts of it for some reason, be aware that the following are not populated (and cannot be)
[ResourceDetail]
[AgentRelationshipsForEntity]

For entities that do not use the latter, a modified version could be run.
*/
Alter  Procedure [dbo].[Entity_Cache_Populate]
	@EntityId	int = 0
AS

if @EntityId > 0 begin
	print 'deleting Entity ' + convert(varchar,@EntityId)
	DELETE FROM [dbo].[Entity_Cache]
					WHERE Id = @EntityId

	print 'deleted Entity ' + convert(varchar, @@ROWCOUNT)

	INSERT INTO [dbo].[Entity_Cache]
           ([Id]
           ,[EntityTypeId]
           ,[EntityType]
           ,[EntityUid]
           ,[parentEntityId]
           ,[parentEntityUid]
           ,[parentEntityType]
           ,[parentEntityTypeId]
           ,[BaseId]
           ,[Name]
           ,[Description]
		   ,OwningOrgId		-- this is now owning org
			,SubjectWebpage
			,CTID
			,ImageUrl
			,EntityStateId
           ,[Created]
           ,[LastUpdated]
		   ---,PublishedByOrgId
		   )

SELECT a.[Id]
		,a.[EntityTypeId]
		,a.[EntityType]
		,a.[EntityUid]
		,a.[parentEntityId]
		,a.[parentEntityUid]
		,a.[parentEntityType]
		,a.[parentEntityTypeId]
		,a.[BaseId]
		,a.[Name]
		,a.[Description]
		,a.OwningOrgId
		,a.SubjectWebpage
		,a.CTID
		,a.ImageUrl
		,a.EntityStateId
		,a.[Created]
		,a.[LastUpdated]
		--,a.PublishedByOrgId
		
  FROM [dbo].[Entity_Summary] a
 	left join [Entity_Cache] b on a.Id = b.Id
	where (a.[Id] = @EntityId)
	end
else if @EntityId = -1 begin
	--use pending
	--clear existing
		print 'using pending reindex'
	DELETE D 
	FROM [dbo].[Entity_Cache] D
	inner join SearchPendingReindex b on d.[BaseId] = b.RecordId 
		AND b.EntityTypeId = d.[EntityTypeId] And  b.StatusId = 1

	--insert
	INSERT INTO [dbo].[Entity_Cache]
           ([Id]
           ,[EntityTypeId]
           ,[EntityType]
           ,[EntityUid]
           ,[parentEntityId]
           ,[parentEntityUid]
           ,[parentEntityType]
           ,[parentEntityTypeId]
           ,[BaseId]
           ,[Name]
           ,[Description]
		   ,OwningOrgId
			,SubjectWebpage
			,CTID
			,ImageUrl
			,EntityStateId
           ,[Created]
           ,[LastUpdated]
		   --,PublishedByOrgId
		   )

SELECT a.[Id]
		,a.[EntityTypeId]
		,a.[EntityType]
		,a.[EntityUid]
		,a.[parentEntityId]
		,a.[parentEntityUid]
		,a.[parentEntityType]
		,a.[parentEntityTypeId]
		,a.[BaseId]
		,a.[Name]
		,a.[Description]
		,a.OwningOrgId
		,a.SubjectWebpage
		,a.CTID
		,a.ImageUrl
		,a.EntityStateId
		,a.[Created] ,a.[LastUpdated]
		--,a.PublishedByOrgId

  FROM [dbo].[Entity_Summary] a
  inner join SearchPendingReindex b on a.[BaseId] = b.RecordId 
		AND b.EntityTypeId = a.[EntityTypeId] And  b.StatusId = 1
	Order by Id

	end
else Begin
	print 'truncating table'
	truncate table [Entity_Cache]

	INSERT INTO [dbo].[Entity_Cache]
           ([Id]
           ,[EntityTypeId]
           ,[EntityType]
           ,[EntityUid]
           ,[parentEntityId]
           ,[parentEntityUid]
           ,[parentEntityType]
           ,[parentEntityTypeId]
           ,[BaseId]
           ,[Name]
           ,[Description]
		   ,OwningOrgId
			,SubjectWebpage
			,CTID
			,ImageUrl
			,EntityStateId
           ,[Created]
           ,[LastUpdated]
		   --,PublishedByOrgId
		   )

SELECT a.[Id]
		,a.[EntityTypeId]
		,a.[EntityType]
		,a.[EntityUid]
		,a.[parentEntityId]
		,a.[parentEntityUid]
		,a.[parentEntityType]
		,a.[parentEntityTypeId]
		,a.[BaseId]
		,a.[Name]
		,a.[Description]
		,a.OwningOrgId
		,a.SubjectWebpage
		,a.CTID
		,a.ImageUrl
		,a.EntityStateId
		,a.[Created] ,a.[LastUpdated]
		--,a.PublishedByOrgId
  FROM [dbo].[Entity_Summary] a
 --	left join [Entity_Cache] b on a.Id = b.Id
 --where b.Id is null
	Order by Id
	End

go

grant execute on [Entity_Cache_Populate] to public
go
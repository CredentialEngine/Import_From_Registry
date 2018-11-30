use credFinder
GO

/****** Object:  StoredProcedure [dbo].[Populate_Cache.Organization_ActorRoles]    Script Date: 9/11/2017 12:24:58 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


/*
exec [Populate_Cache.Organization_ActorRoles] 0

exec [Populate_Cache.Organization_ActorRoles] -1

*/
Alter  Procedure [dbo].[Populate_Cache.Organization_ActorRoles]
	@OrganizationId	int = 0
AS

-- =================================
if @OrganizationId > 0 begin
	print 'deleting Organization ' + convert(varchar,@OrganizationId)
	DELETE FROM [dbo].[Cache.Organization_ActorRoles]
					WHERE OrganizationId = @OrganizationId
	print 'deleted Organization roles # ' + convert(varchar, @@ROWCOUNT)
	end
else if @OrganizationId = -1 begin
	print 'using pending reindex'
	DELETE D 
	FROM [dbo].[Cache.Organization_ActorRoles] D
	inner join SearchPendingReindex b on d.OrganizationId = b.RecordId And b.EntityTypeId = 2 and b.StatusId = 1
	
	INSERT INTO [dbo].[Cache.Organization_ActorRoles]
						([OrganizationId]
						,[OwnedBy]
						,[OfferedBy]
						,[AsmtsOwnedBy]
						,[LoppsOwnedBy]
						,[LastCacheDate])
    
	SELECT [OrganizationId]
				,[OwnedBy]
				,[OfferedBy]
				,[AsmtsOwnedBy]
				,[LoppsOwnedBy]
				,getDate() as LastCacheDate
		FROM [dbo].[Organization_ActorRolesCSV] d
	inner join SearchPendingReindex b on d.OrganizationId = b.RecordId And b.EntityTypeId = 2 and b.StatusId = 1
	end

else begin
		print 'truncating table'
		truncate table [Cache.Organization_ActorRoles]
	end


	INSERT INTO [dbo].[Cache.Organization_ActorRoles]
						 ([OrganizationId]
						 ,[OwnedBy]
						 ,[OfferedBy]
						 ,[AsmtsOwnedBy]
						 ,[LoppsOwnedBy]
						 ,[LastCacheDate])
    
	SELECT [OrganizationId]
				,[OwnedBy]
				,[OfferedBy]
				,[AsmtsOwnedBy]
				,[LoppsOwnedBy]
				,getDate() as LastCacheDate
		FROM [dbo].[Organization_ActorRolesCSV]
	where (@OrganizationId = 0 OR  [OrganizationId] = @OrganizationId)


GO
grant execute on [Populate_Cache.Organization_ActorRoles] to public
go


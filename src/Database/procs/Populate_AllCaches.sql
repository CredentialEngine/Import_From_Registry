use credFinder
go
--use credFinder_ProdSync
--GO

/****** Object:  StoredProcedure [dbo].[Populate_Cache.Organization_ActorRoles]    Script Date: 9/11/2017 12:24:58 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


/*
exec [Populate_AllCaches] 0

exec [Populate_AllCaches] -1


--
Could customize to only do entities in the reindex table
*/
ALTER  Procedure [dbo].[Populate_AllCaches]
	@PopulateType int = -1
AS

-- =================================
	--18-08-22 mp - there is a trigger to automatically populate Entity_Cache, so do we really need this?
	--				As well, work to remove dependence on this!
	exec Entity_Cache_Populate @PopulateType

	print '[Populate_Cache.Organization_ActorRoles] - TBD - may be able to eliminate this one!'
	exec [Populate_Cache.Organization_ActorRoles]  @PopulateType

	print 'Populate_Credential_SummaryCache'
	exec Populate_Credential_SummaryCache @PopulateType

	--doing all, does bubbling up, so could have unrelated entities affected
	--doesn't handle -1
	print '[Entity_ReferenceConnection_Populate]'
	exec [Entity_ReferenceConnection_Populate] 0

	--doing all, this will probably change
	--doesn't handle -1
	print '[Populate_Competencies_cache]'
	exec [Populate_Competencies_cache] 0

	--for now doing all
	--doesn't handle -1
	print '[Populate_Entity_SearchIndex]'
	exec [Populate_Entity_SearchIndex] 0


GO
grant execute on [Populate_AllCaches] to public
go


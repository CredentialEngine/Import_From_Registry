use credFinder
go

use sandbox_credfinder
go


--use staging_credFinder
--go


SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


/*
exec [Populate_AllCaches] 0

exec [Populate_AllCaches] -1


--
Could customize to only do entities in the reindex table

22-11-17 mparsons Can we skip Populate_Credential_SummaryCache? It is always called after update of a credential.
23-07-11 mparsons - NO cannot skip Populate_Credential_SummaryCache, or Populate_Competencies_cache
					The credential cache was not being called anywhere else in the import except after a round of imports that included any elastic updates 
*/
ALTER  Procedure [dbo].[Populate_AllCaches]
	@PopulateType int = -1
AS

-- =================================
	--18-08-22 mp - there is a trigger to automatically populate Entity_Cache, so do we really need this?
	--				As well, work to remove dependence on this!
	--this should not be done arbitrarily now as majority of applicable tables update cache directly
	--print '[Entity_Cache_Populate] - SKIPPING'
	--exec Entity_Cache_Populate @PopulateType

	--print '[Populate_Cache.Organization_ActorRoles] - SKIPPING - ORG SEARCHES USE A VIEW DUE TO TIMING ISSUES. '
	---exec [Populate_Cache.Organization_ActorRoles]  @PopulateType

	--these should probably be separate so a failure in one doesn't affect the others
	print 'Populate_Credential_SummaryCache'
	--OR IS IT? THE IMPORT CALLS A METHOD TO POPULATE THE CACHES
	exec Populate_Credential_SummaryCache @PopulateType

	--doing all, does bubbling up, so could have unrelated entities affected
	--doesn't handle -1
	print '[Entity_ReferenceConnection_Populate]'
	exec [Entity_ReferenceConnection_Populate] 0

	--doing all, this will probably change
	--doesn't handle -1
	print '[Populate_Competencies_cache] '
	exec [Populate_Competencies_cache] @PopulateType

	--for now doing all
	--doesn't handle -1
	--still used by all elasticSearch procs
	print '[Populate_Entity_SearchIndex]'
	exec [Populate_Entity_SearchIndex] 0


GO
grant execute on [Populate_AllCaches] to public
go


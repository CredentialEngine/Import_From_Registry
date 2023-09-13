use credFinder
GO

/****** Object:  StoredProcedure [dbo].[Entity_ReferenceConnection_Populate]    Script Date: 8/22/2017 5:24:53 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


/*
use [ctdlEditor]
GO

exec [Entity_ReferenceConnection_Populate] 6777
truncate table [Entity.ReferenceConnection]
go
exec [Entity_ReferenceConnection_Populate] 0

exec [Entity_ReferenceConnection_Populate] 6882

*/


/* 
==================================================
Description:      populate Entity.ReferenceConnection
Key purpose to enable bubbling up of assets like a subject to a top level parent. 
ex: for a subject added for a learning opportunity, add a connection to the top level credential
For parent, find related connections like a parent learning opp, credential, 
and add an Entity.ReferenceConnection

CREDENTIAL
Entity.Reference (subject)
	Entity (ex for a learning opp) er.EntityId = a.Id
		Entity.LearningOpp (where latter is LOPP, get any child ref)
			Entity		parent entity for the latter (cond profile)
				Entity.ConditionProfile 
					Entity (parent credential)

-----------------------------------------------------
Modifications
16-09-02 mparsons - new

*/

CREATE PROCEDURE [dbo].[Entity_ReferenceConnection_Populate]
		@EntityReferenceId int -- FK to EntityReference just added

As
declare @LoppInserted int ,  
@AsmtInserted int,  
@CredInserted int,  
@LoppCredInserted int,	--where added to a lopp under a credential (not under context of cond profile - which could be later)
@AsmtCredInserted int	
set @LoppInserted = 0
set @LoppCredInserted= 0
SET NOCOUNT ON;

-- ==================================================
print ' first for credentials connected to a lopp'
INSERT INTO [dbo].[Entity.ReferenceConnection]
           (EntityReferenceId
           ,[EntityUid])

--@EntityReferenceId int
--set @EntityReferenceId = 6813

SELECT DISTINCT 
	er.Id as EntitySubjectId
	,credEntity.EntityUid

	--,er.[TextValue] As Subject
--	select *
FROM            
	dbo.Entity credEntity  
	
	INNER JOIN dbo.[Entity.LearningOpportunity] b ON credEntity.Id = b.EntityId 
	INNER JOIN dbo.LearningOpportunity c		ON b.LearningOpportunityId = c.Id 
	inner join Entity loppEntity				on c.RowId = loppEntity.EntityUid

	inner join [dbo].[Entity.Reference] er		on loppEntity.Id = er.EntityId
	left join [Entity.ReferenceConnection] d	on er.Id = d.EntityReferenceId
where credEntity.EntityTypeId = 1 
AND er.[CategoryId]= 34
and (er.Id = @EntityReferenceId OR @EntityReferenceId = 0)
and d.EntityUid is null 

set @LoppCredInserted = @@rowcount 


-- ==================================================
print ' next for credentials connected to an asmt '
INSERT INTO [dbo].[Entity.ReferenceConnection]
           (EntityReferenceId
           ,[EntityUid])

--@EntityReferenceId int
--set @EntityReferenceId = 6813

SELECT DISTINCT 
	er.Id as EntitySubjectId
	,credEntity.EntityUid

	--,er.[TextValue] As Subject
--	select *
FROM            
	dbo.Entity credEntity  
	
	INNER JOIN dbo.[Entity.Assessment] b ON credEntity.Id = b.EntityId 
	INNER JOIN dbo.Assessment c			ON b.AssessmentId = c.Id 
	inner join Entity loppEntity on c.RowId = loppEntity.EntityUid

	inner join [dbo].[Entity.Reference] er	on loppEntity.Id = er.EntityId
left join [Entity.ReferenceConnection] d	on er.Id = d.EntityReferenceId
where credEntity.EntityTypeId = 1 
AND er.[CategoryId]= 34
and (er.Id = @EntityReferenceId OR @EntityReferenceId = 0)
and d.EntityUid is null 

set @AsmtCredInserted = @@rowcount 

-- ==================================================
print 'next for credentials thru a condition profile '
INSERT INTO [dbo].[Entity.ReferenceConnection]
           (EntityReferenceId
           ,[EntityUid])

--@EntityReferenceId int
--set @EntityReferenceId = 6813

SELECT DISTINCT 
	er.Id as EntitySubjectId
	,base.CredentialRowId

	--,er.[TextValue] As Subject
	
--	select *
FROM            
	dbo.Credential_ConditionProfile AS base  
	
	INNER JOIN dbo.[Entity.LearningOpportunity] b ON base.EntityId = b.EntityId 
	INNER JOIN dbo.LearningOpportunity c			ON b.LearningOpportunityId = c.Id 
	inner join Entity loppEntity on c.RowId = loppEntity.EntityUid

	inner join [dbo].[Entity.Reference] er	on loppEntity.Id = er.EntityId
left join [Entity.ReferenceConnection] d	on er.Id = d.EntityReferenceId
where er.[CategoryId]= 34
and (er.Id = @EntityReferenceId OR @EntityReferenceId = 0)
and d.EntityUid is null 
----------------------------------
--for clarity, the following could be used
----------------------------------
/*
INSERT INTO [dbo].[Entity.ReferenceConnection]
           (EntityReferenceId
           ,[EntityUid])
SELECT DISTINCT 
	er.Id as EntitySubjectId
	,e.EntityUid

	----,s.[Subject]
	--,er.[TextValue] As Subject
	
--	select *
FROM   [dbo].[Entity.Reference]  er
	Inner join Entity a on er.EntityId = a.Id		--parent of subject (LOPP for now)
		Inner join [Entity.LearningOpportunity] b on a.EntityBaseId = b.LearningOpportunityId 
			Inner join Entity c on b.EntityId = c.Id	--condition profile containing latter 
				Inner join [Entity.ConditionProfile] d on c.EntityUid = d.RowId
					Inner Join Entity e on d.EntityId = e.Id	-- entity of a cred

	left join [Entity.ReferenceConnection] f	on er.Id = f.EntityReferenceId

where er.[CategoryId]= 34
and er.Id = @EntityReferenceId
and f.EntityUid is null 
*/

set @LoppInserted = @@rowcount 


print ' top level asmts '
INSERT INTO [dbo].[Entity.ReferenceConnection]
           (EntityReferenceId
           ,[EntityUid])

SELECT DISTINCT 
	er.Id as EntitySubjectId
	,base.CredentialRowId
	--,base.CredentialId 
 -- ,base.[EntityTypeId]

	--,er.CategoryId
	--,er.TextValue as[Subject]

FROM            
	dbo.Credential_ConditionProfile AS base  
	INNER JOIN dbo.[Entity.Assessment] b ON base.EntityId = b.EntityId 
	INNER JOIN dbo.Assessment c			ON b.AssessmentId = c.Id 
	inner join Entity a_e on c.RowId = a_e.EntityUid

	inner join [dbo].[Entity.Reference] er on a_e.Id = er.EntityId
	left join [Entity.ReferenceConnection] d on er.Id = d.EntityReferenceId

where er.[CategoryId]= 34
and (er.Id = @EntityReferenceId OR @EntityReferenceId = 0)
and d.EntityUid is null 

set @AsmtInserted = @@ROWCOUNT

print ' populate for credentials '
INSERT INTO [dbo].[Entity.ReferenceConnection]
           (EntityReferenceId
           ,[EntityUid])

SELECT DISTINCT 
	er.Id as EntitySubjectId
	,base.EntityUid

	--,er.CategoryId
	--,er.TextValue as[Subject]

FROM Entity base		--Entity for a credenial with embedded credentials
		inner join dbo.[Entity.Credential] b ON base.Id = b.EntityId 
		INNER JOIN dbo.Credential c			ON b.CredentialId = c.Id --embedded cred
		inner join Entity a_e on c.RowId = a_e.EntityUid	--entity for an embedded cred
		inner join [dbo].[Entity.Reference] er on a_e.Id = er.EntityId -- for ref under latter
		left join [Entity.ReferenceConnection] d on er.Id = d.EntityReferenceId

where base.EntityTypeId = 1
and er.[CategoryId]= 34
and (er.Id = @EntityReferenceId OR @EntityReferenceId = 0)
and d.EntityUid is null 

set @CredInserted = @@ROWCOUNT

print '@loppInserted: ' + convert(varchar,@loppInserted)
	+  ', @AsmtInserted: ' + convert(varchar,@AsmtInserted)
	+  ', @CredInserted: ' + convert(varchar,@CredInserted)
	+  ', @LoppCredInserted: ' + convert(varchar,@LoppCredInserted)
	+  ', @AsmtCredInserted: ' + convert(varchar,@AsmtCredInserted)

	

GO
grant execute on Entity_ReferenceConnection_Populate to public
go


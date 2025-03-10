USE credFinder
GO

/****** Object:  View [dbo].[Account_Summary]    Script Date: 3/28/2016 11:01:58 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

/*

SELECT [Id]
      --,[UserName]
      ,[FirstName]      ,[LastName]
     -- ,[FullName]      ,[SortName]
      ,[Email]
			    ,[Roles]
			, OrgMbrs
			,convert(varchar(10),lastLogon,120) As LastLoginDate
   --   ,[IsActive]
   --   ,[Created]      ,[LastUpdated],LastUpdatedById
	  --,[AspNetId]
  
  FROM [dbo].[Account_Summary]
order by sortName


*/
/* ==========================================================
   Modifications
   19-02-28 mparsons - added to the finder for us by stand alone projects
  				 - there is no organzation support, so returning blank for orgMbrs
				 - there could be a conflict here with entity frameworks from ctdlEditor
-- ==========================================================
*/
Create  VIEW [dbo].[Account_Summary]
AS

SELECT  ISNULL( base.[Id],-1) As Id
		,[UserName]
		,[FirstName] ,[LastName]
		,[FirstName] + ' ' + [LastName] As FullName
		,[LastName] + ', ' + [FirstName] As SortName
		,[Email]
		,[IsActive]
		,[Created],[LastUpdated], base.LastUpdatedById
		,base.CEAccountIdentifier
		,base.RowId
		,base.[AspNetId]
		
		, CASE
			WHEN Roles IS NULL THEN ''
			WHEN len(Roles) = 0 THEN ''
			ELSE left(Roles,len(Roles)-1)
		END AS Roles
		--, CASE
		--		WHEN OrgMbr IS NULL THEN ''
		--		WHEN len(OrgMbr) = 0 THEN ''
		--		ELSE left(OrgMbr,len(OrgMbr)-1)
		--END AS OrgMbrs
		,'' as OrgMbrs
		, LoginHistory.lastLogon
	--, CASE
	--         WHEN RoleIds IS NULL THEN ''
	--         WHEN len(RoleIds) = 0 THEN ''
	--         ELSE left(RoleIds,len(RoleIds)-1)
	--   END AS RoleIds
	FROM [dbo].[Account] base
	left join (SELECT [TargetUserId] ,max([CreatedDate]) As lastLogon
		FROM [dbo].[Activity_Summary]
		where [Activity] = 'account'
		and ( [event] = 'Authentication: login' or [event] = 'Authentication: Google' or [event] = 'Authentication: Admin PASSKEY Login ' ) 
		group by [TargetUserId]
		) as LoginHistory on base.Id = LoginHistory.TargetUserId


CROSS APPLY (
	SELECT 
		convert(varchar,ar.Name) + ', '
	-- '''' + convert(varchar,ar.Name) + ''', '
	FROM dbo.AspNetRoles ar
	INNER JOIN dbo.AspNetUserRoles aur ON ar.Id = aur.RoleId  
	WHERE (base.IsActive = 1) 
	AND base.[AspNetId] = aur.UserId
	FOR XML Path('') 
) D (Roles)


--CROSS APPLY (
--    SELECT o.Name + ' ( ' + convert(varchar, o.Id) + ' ); '
--   -- ,rsub.ResourceId
--    FROM dbo.Organization o
--		INNER JOIN dbo.[Organization.Member] om ON o.Id = om.ParentOrgId
--    WHERE (o.StatusId <= 3) 
--		AND base.Id = om.UserId
--    FOR XML Path('') 
--) R (OrgMbr)

--CROSS APPLY (
--    SELECT convert(varchar, o.Id) + '~' + o.Name + '~' + o.Ctid + '| '
--   -- ,rsub.ResourceId
--    FROM dbo.Organization o
--		INNER JOIN dbo.[Organization.Member] om ON o.Id = om.ParentOrgId
--    WHERE (o.StatusId <= 3) 
--		AND base.Id = om.UserId
--    FOR XML Path('') 
--) R (OrgMbr)

GO

grant select on [Account_Summary] to public
go

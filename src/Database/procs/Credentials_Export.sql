USE credFinder
GO

use sandbox_credFinder
go

/****** Object:  StoredProcedure [dbo].[Credentials_Export]    Script Date: 5/21/2018 1:01:56 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


/*


exec [Credentials_Export] 'dab7aeae-d843-4c46-a013-9cbdc8e1cc44'

--Scrum Alliance
exec [Credentials_Export] 'A85C81BC-DFE1-498F-AC50-7F97B84EC8BA'

--participate
exec [Credentials_Export] '91D58CC1-BE37-42F5-BB5C-88913B13B3FA'

--SAP
exec [Credentials_Export] '071ae23c-00a1-4467-97f0-a5ce7b59765e'

--SAP SE
exec [Credentials_Export] '07B4A948-2305-440D-AC16-9FA8084A53C5'


-- local ===============================================

--Credential Engine Sandbox Administration
select * from organization where name like 'Credential Engine%'
-- ce-8DDBF031-BAD6-4B19-B726-066D5F71AD01
exec [Credentials_Export] 'C8D7E881-FF03-489F-A6C7-2F1673B4646D'

-- microsoft: 
select * from organization where name = 'microsoft'
exec [Credentials_Export] '4D07FBE3-13B4-4A84-80F4-FB63CE78C23D'

-- noct: 
select * from organization where name like '%nocti%'
exec [Credentials_Export] '6EC5F5B9-542D-4073-8C03-A2B9B49286A8'




select * from organization where name like '%hutch%'
exec [Credentials_Export] '5C501AA2-FFFB-412D-93D3-27BA4D58EFE7'


*/


/* =============================================
Description:      [Credentials_Export]
Options:
- Consider option to exclude condition profile
 
------------------------------------------------------
Modifications


*/

Alter PROCEDURE [dbo].[Credentials_Export]
		@OwningOrgUid	varchar(50),
		@IncludingConditionProfile bit = 1

As

SET NOCOUNT ON;



select distinct
	'' as [External Identifier]
	,a.CTID
	,b.ctid as 'Owned By'
	,a.Id as CredentialId
	,a.Name As 'Credential Name'
	,a.Description
	--,replace(a.Description, '"','`') as Description

	--,'Roles'
	,replace(a .CredentialTypeSchema, 'ceterms:','') as 'Credential Type'
	,cstat.Property as  'Credential Status'
	, a.SubjectWebpage as 'Webpage'
	, isnull(c.AvailableOnlineAt,'') 'Available Online At'
	, isnull(c.AvailabilityListing,'') 'Availability Listing'
	, isnull(c.AlternateName,'') 'Alternate Name'
	, isnull(c.CodedNotation,'') 'Coded Notation'
	, isnull(c.CredentialId,'')  'Credential Id'
	, isnull(c.ImageUrl,'') 'Credential Image'
	-- ============= languages ================
	, IsNull(STUFF(
	(	
		SELECT '|' +  Replace([TextValue], 'English (en)', 'en') AS [text()] 
		FROM [dbo].[Entity.Reference] els
		where CategoryId = 65 AND els.EntityId = a.EntityId 
		FOR XML Path('')
		), 1,1,''
	),'') as 'Language'

	-- ===========================================
	, case when c.EffectiveDate < '1950-01-01' then ''
		when c.EffectiveDate is null then ''
		else  convert(varchar(10),c.EffectiveDate,120)  end  'Date Effective'
	, IsNull(STUFF(
		(	
		SELECT '|' + ISNULL([TextValue], '') AS [text()]  FROM [dbo].[Entity.Reference] keywords 
		where [CategoryId]= 35  and keywords.EntityId = a.EntityId
		FOR XML Path('') ), 1,1,''
	),'') as 'Keywords'


	, IsNull(esub.DirectSubjects,'') 'Subjects'

	, IsNull(STUFF(
		(	
		SELECT '|' + Replace(ISNULL(eps.PropertySchemaName, ''),'audLevel:','') AS [text()] 
		FROM [dbo].[EntityProperty_Summary] eps 
		where eps.EntityTypeId = 1 AND eps.CategoryId = 4 AND eps.EntityId = a.EntityId 
		FOR XML Path('')
		), 1,1,''
	),'') as 'Audience Level Type'

	,IsNull(enaics.Others,'')		'Industry Type'
	,IsNull(enaics.NaicsList,'')	'NAICS List'
	,IsNull(eoccupations.Others,'')    'Occupation Type'
	,IsNull(eoccupations.OnetList,'')  'O*NET List'
	,IsNull(STUFF(
		(	
		SELECT '|' + convert(varchar,addresses.Id) AS [text()]  FROM [dbo].[Entity.Address] addresses 
		where addresses.EntityId = a.EntityId
		FOR XML Path('')
		), 1,1,''
	),'') as 'Available At'
	, IsNull(STUFF(
		(	
		SELECT '|' + ISNULL([TextValue], '') AS [text()]  FROM [dbo].[Entity.Reference] keywords 
		where [CategoryId]= 63  and keywords.EntityId = a.EntityId
		FOR XML Path('') ), 1,1,''
	),'') as 'Degree Major'
	

	--consider combining roles into one column with a new pattern
	--user could be confused by the use of CTID
	--SELECT '|' + convert(varchar(50),earOrg.Ctid) AS [text()]  FROM [dbo].[Entity.AgentRelationship] ear
	, IsNull(STUFF(
		(	
		SELECT '|' + case when isnull(earOrg.CTID,'') = '' then earOrg.Name + '~' + earOrg.SubjectWebpage else earOrg.CTID end AS [text()] FROM [dbo].[Entity.AgentRelationship] ear
		Inner Join Organization earOrg on ear.agentUid = earOrg.rowId 
		where ear.EntityId = a.EntityId AND ear.RelationshipTypeId = 7
		--and earorg.Id = 4652
		FOR XML Path('')
		), 1,1,''
	),'') as 'Offered By'
	, IsNull(STUFF(
		(	
		SELECT '|' + case when isnull(earOrg.CTID,'') = '' then earOrg.Name + '~' + earOrg.SubjectWebpage else earOrg.CTID end AS [text()] FROM [dbo].[Entity.AgentRelationship] ear
		Inner Join Organization earOrg on ear.agentUid = earOrg.rowId 
		where ear.EntityId = a.EntityId AND ear.RelationshipTypeId = 1
		FOR XML Path('')
		), 1,1,''
	),'') as 'Accredited By'
	, IsNull(STUFF(
		(	
		SELECT '|' + case when isnull(earOrg.CTID,'') = '' then earOrg.Name + '~' + earOrg.SubjectWebpage else earOrg.CTID end AS [text()] FROM [dbo].[Entity.AgentRelationship] ear
		Inner Join Organization earOrg on ear.agentUid = earOrg.rowId 
		where ear.EntityId = a.EntityId AND ear.RelationshipTypeId = 2
		FOR XML Path('')
		), 1,1,''
	),'') as 'Approved By'
	, IsNull(STUFF(
		(	
		SELECT '|' + case when isnull(earOrg.CTID,'') = '' then earOrg.Name + '~' + earOrg.SubjectWebpage else earOrg.CTID end AS [text()] FROM [dbo].[Entity.AgentRelationship] ear
		Inner Join Organization earOrg on ear.agentUid = earOrg.rowId 
		where ear.EntityId = a.EntityId AND ear.RelationshipTypeId = 10
		FOR XML Path('')
		), 1,1,''
	),'') as 'Recognized By'

	,isnull(cpr.ctid,'') 'Copyright Holder'
	,IsNull(c.ProcessStandards,'') 'Process Standards'
	,Isnull(c.ProcessStandardsDescription,'') 'Process Standards Description'

	--should be single at this time
	--, STUFF(
	--	(	
	--	SELECT '|' + ISNULL([FromDuration], '') AS [text()]  FROM [dbo].[Entity.DurationProfile] duration 
	--	where duration.EntityId = a.EntityId
	--	FOR XML Path('')
	--	), 1,1,''
	--) as 'Estimated Duration'
	,case 
		when dp.FromYears > 0 then convert(varchar, dp.FromYears) + ' Years'
		when dp.FromMonths > 0 then convert(varchar, dp.FromMonths) + ' Months'
		when dp.FromWeeks > 0 then convert(varchar, dp.FromWeeks) + ' Weeks'
		when dp.FromDays > 0 then convert(varchar, dp.FromDays) + ' Days'
		when dp.FromHours > 0 then convert(varchar, dp.fromhours) + ' Hours'
		when dp.FromMinutes > 0 then convert(varchar, dp.FromMinutes) + ' Minutes'
		else ISNULL([FromDuration], '') end as 'Estimated Duration'

	-- === single cost ===========================================================================
	, case when costs.RowId is null then '' else convert(varchar(50),costs.RowId) end as 'Cost: Internal Identifier'
	, ''  as 'Cost: External Identifier'
	, IsNUll(costs.ProfileName,'') as 'Cost: Name'
	, IsNUll(costs.Description,'') as 'Cost: Description'
	, IsNUll(costs.DetailsUrl,'') as 'Cost: Details Url'
	, IsNUll(currencies.AlphabeticCode,'') as 'Cost: Currency Type'
	, IsNull(STUFF(
	(	
		SELECT '|' + costType.Title + '~' + Convert(varchar(25),ISNULL(cpi.price,0)) AS [text()]  FROM [dbo].[Entity.CostProfileItem] cpi 
		inner Join [Codes.PropertyValue] costType on cpi.CostTypeId = costType.Id
		where costs.Id = cpi.CostProfileId
		FOR XML Path('') 	), 1,1,'' 	),'') as 'Cost: Types List'
	--,costs.Created as CostProfileCreated
	-- ============= condition ====================================
	--will need to distinguish between condition types. Should only allow requires now - and not a connection!
	--, ecp.ConnectionTypeId as 'ConditionProfile: ConditionTypeId'
	, replace(cpType.SchemaName,'ceterms:','') as 'ConditionProfile: Condition Type'
	, case when ecp.RowId is null then '' else convert(varchar(50),ecp.RowId) end as 'ConditionProfile: Internal Identifier'
	, ''  as 'ConditionProfile: External Identifier'
	, IsNull(ecp.Name,'') as 'ConditionProfile: Name'
	, IsNull(ecp.Description,'') as 'ConditionProfile: Description'
	, IsNull(ecp.SubjectWebpage,'') as 'ConditionProfile: Subject Webpage'

	, IsNull(STUFF(
	(	
	SELECT '|' + eref.TextValue AS [text()]  FROM [dbo].[Entity.Reference] eref
	where eref.CategoryId = 57
	AND eref.EntityId = cpEntity.Id 
	FOR XML Path('')
	), 1,1,'' ),'') as 'ConditionProfile: Submission Of Items'
			
	, IsNull(STUFF(
	(	
	SELECT '|' + eref.TextValue AS [text()]  FROM [dbo].[Entity.Reference] eref
	where eref.CategoryId = 28
	AND eref.EntityId = cpEntity.Id 
	FOR XML Path('')
	), 1,1,'' 	),'') as 'ConditionProfile: Condition Items'
	, IsNull(ecp.Experience,'')  as 'ConditionProfile: Experience'
	, IsNUll(ecp.YearsOfExperience,0)  as 'ConditionProfile: Years Of Experience'
	--,ecp.Created as ConditionProfileCreated

	--TODO - how to include targets (asmts, etc)

	, IsNUll(ecp.CreditHourType,0)  as 'ConditionProfile: CreditHourType'
	, IsNUll(ecp.CreditHourValue,0)  as 'ConditionProfile: CreditHourValue'
	, IsNUll(creditUnitType.Title,'')  as 'ConditionProfile: CreditUnitType'
	, IsNUll(ecp.CreditUnitValue,0)  as 'ConditionProfile: CreditUnitValue'
	, IsNUll(ecp.CreditUnitTypeDescription,0)  as 'ConditionProfile: CreditUnitTypeDescription'


from Credential_Summary a 
inner join Organization b			on a.owningOrganizationId = b.Id 
inner join credential c				on a.Id = c.id
Inner Join [EntityProperty_Summary] cstat	on a.EntityId = cstat.EntityId and cstat.CategoryId = 39
Left join Organization cpr			on c.CopyrightHolder = cpr.RowId 
left join [Entity.NaicsCSV] enaics	on a.EntityId = enaics.EntityId
left join [Entity.OccupationsCSV] eoccupations on a.EntityId = eoccupations.EntityId
--Left Join [Codes.Language] lang		on c.InLanguageId = lang.Id
Left Join [Entity_SubjectsCSV] esub on a.EntityId = esub.EntityId

Left Join [Entity.ConditionProfile] ecp on a.EntityId = ecp.EntityId 
		and ecp.ConnectionTypeId in ( 1,2,5)
		and IsNull(ecp.ConditionSubTypeId,1) = 1
	--	AND @IncludingConditionProfile = 1
Left Join Entity cpEntity on ecp.RowId = cpEntity.EntityUid
Left Join [Codes.ConditionProfileType] cpType		on ecp.ConnectionTypeId = cpType.Id
--Left Join  [Import.IdentifierToObjectXref] cpsxr	on ecp.RowId = cpsxr.TargetRowId
Left Join [Codes.PropertyValue] creditUnitType on ecp.CreditUnitTypeId = creditUnitType.Id
Left Join [Entity.DurationProfile] dp on a.EntityId = dp.EntityId

-- cost
Left Join [Entity.CostProfile] costs on a.EntityId = costs.EntityId 
--Left Join  [Import.IdentifierToObjectXref] costsxr on costs.RowId = costsxr.TargetRowId
Left Join [Codes.Currency] currencies on costs.CurrencyTypeId = currencies.NumericCode

where c.OwningAgentUid = @OwningOrgUid

Order by a.Name
--,ecp.Id
--,costs.Id
-- =================================

GO

grant execute on Credentials_Export to public
go
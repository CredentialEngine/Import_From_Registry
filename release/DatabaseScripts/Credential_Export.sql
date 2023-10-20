use credfinder_github
go

/****** Object:  View [dbo].[Credential_Export]    Script Date: 10/4/2023 2:51:53 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

/*

SELECT top 500
[CTID]
      ,[Owned By]
      ,[CredentialId]
      ,[Credential Name]
      ,[Description]
      ,[Credential Type]
      ,[Credential Status]
      ,[Webpage]
      ,[Available Online At]
      ,[Availability Listing]
      ,[Alternate Name]
      ,[Coded Notation]
      ,[Credential Id]
      ,[Credential Image]
      ,[Language]
      ,[Date Effective]
      ,[Keywords]
      ,[Subjects]
      ,[Audience Level Type]
      ,[Industry Type]
      ,[NAICS List]
      ,[Occupation Type]
      ,[O*NET List]
      ,[Available At]
      ,[Degree Major]
      ,[Offered By]
      ,[Accredited By]
      ,[Approved By]
      ,[Recognized By]
      ,[Copyright Holder]
      ,[Process Standards]
      ,[Process Standards Description]
      ,[Estimated Duration]
      ,[Cost: Internal Identifier]
      ,[Cost: External Identifier]
      ,[Cost: Name]
      ,[Cost: Description]
      ,[Cost: Details Url]
      ,[Cost: Currency Type]
      ,[Cost: Types List]
      ,[ConditionProfile: Condition Type]
      ,[ConditionProfile: Internal Identifier]
      ,[ConditionProfile: External Identifier]
      ,[ConditionProfile: Name]
      ,[ConditionProfile: Description]
      ,[ConditionProfile: Subject Webpage]
      ,[ConditionProfile: Submission Of Items]
      ,[ConditionProfile: Condition Items]
      ,[ConditionProfile: Experience]
      ,[ConditionProfile: Years Of Experience]
      ,[ConditionProfile: CreditHourType]
      ,[ConditionProfile: CreditHourValue]
      ,[ConditionProfile: CreditUnitType]
      ,[ConditionProfile: CreditUnitValue]
      ,[ConditionProfile: CreditUnitTypeDescription]
  FROM [dbo].[Credential_Export]
  order by [Credential Name]
GO



*/

/*
Summary view for credentials

-- =========================================================
18-09-20 mparsons - created a lite version of the summary
*/
Alter VIEW [dbo].[Credential_Export]
AS

select distinct
	a.CTID
	,b.ctid as 'OwnedBy'
	,a.Id as CredentialRecordId
	,a.Name As 'CredentialName'
	,a.Description
	--,replace(a.Description, '"','`') as Description

	--,'Roles'
	,replace(a .CredentialTypeSchema, 'ceterms:','') as 'CredentialType'
	,cstat.Property as  'CredentialStatus'
	, a.SubjectWebpage as 'Webpage'
	, isnull(c.AvailableOnlineAt,'') 'AvailableOnlineAt'
	, isnull(c.AvailabilityListing,'') 'AvailabilityListing'
	, isnull(c.AlternateName,'') 'AlternateName'
	, isnull(c.CodedNotation,'') 'CodedNotation'
	, isnull(c.CredentialId,'')  'CredentialId'
	, isnull(c.ImageUrl,'') 'CredentialImage'
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
		else  convert(varchar(10),c.EffectiveDate,120)  end  'DateEffective'
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
	),'') as 'AudienceLevelType'

	,IsNull(enaics.Naics,'')		'IndustryType'
	,IsNull(enaics.NaicsList,'')	'NAICS List'
	,IsNull(eoccupations.Occupations,'')    'OccupationType'
	,IsNull(eoccupations.OnetList,'')  'O*NET List'
		,isnull(ePrograms.Programs,'')    'InstructionalProgramType'
	,isnull(ePrograms.CipList,'')  'CIP List'
	,IsNull(STUFF(
		(	
		SELECT '|' + convert(varchar,addresses.Id) AS [text()]  FROM [dbo].[Entity.Address] addresses 
		where addresses.EntityId = a.EntityId
		FOR XML Path('')
		), 1,1,''
	),'') as 'AvailableAt'
	, IsNull(STUFF(
		(	
		SELECT '|' + ISNULL([TextValue], '') AS [text()]  FROM [dbo].[Entity.Reference] keywords 
		where [CategoryId]= 63  and keywords.EntityId = a.EntityId
		FOR XML Path('') ), 1,1,''
	),'') as 'DegreeMajor'
	

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
	),'') as 'OfferedBy'
	, IsNull(STUFF(
		(	
		SELECT '|' + case when isnull(earOrg.CTID,'') = '' then earOrg.Name + '~' + earOrg.SubjectWebpage else earOrg.CTID end AS [text()] FROM [dbo].[Entity.AgentRelationship] ear
		Inner Join Organization earOrg on ear.agentUid = earOrg.rowId 
		where ear.EntityId = a.EntityId AND ear.RelationshipTypeId = 1
		FOR XML Path('')
		), 1,1,''
	),'') as 'AccreditedBy'
	, IsNull(STUFF(
		(	
		SELECT '|' + case when isnull(earOrg.CTID,'') = '' then earOrg.Name + '~' + earOrg.SubjectWebpage else earOrg.CTID end AS [text()] FROM [dbo].[Entity.AgentRelationship] ear
		Inner Join Organization earOrg on ear.agentUid = earOrg.rowId 
		where ear.EntityId = a.EntityId AND ear.RelationshipTypeId = 2
		FOR XML Path('')
		), 1,1,''
	),'') as 'ApprovedBy'
	, IsNull(STUFF(
		(	
		SELECT '|' + case when isnull(earOrg.CTID,'') = '' then earOrg.Name + '~' + earOrg.SubjectWebpage else earOrg.CTID end AS [text()] FROM [dbo].[Entity.AgentRelationship] ear
		Inner Join Organization earOrg on ear.agentUid = earOrg.rowId 
		where ear.EntityId = a.EntityId AND ear.RelationshipTypeId = 10
		FOR XML Path('')
		), 1,1,''
	),'') as 'RecognizedBy'

	,isnull(cpr.ctid,'') 'CopyrightHolder'
	,IsNull(c.ProcessStandards,'') 'ProcessStandards'
	,Isnull(c.ProcessStandardsDescription,'') 'ProcessStandards Description'

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
left join [Entity.ProgramsCSV] ePrograms on a.EntityId = ePrograms.EntityId
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

where a.EntityStateId > 1

--Order by a.Name


GO



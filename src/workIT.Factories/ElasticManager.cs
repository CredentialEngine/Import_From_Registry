using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

using workIT.Models;
using CM = workIT.Models.Common;
using PM = workIT.Models.ProfileModels;
using workIT.Models.Elastic;
using workIT.Utilities;
namespace workIT.Factories
{
	public class ElasticManager : BaseFactory
	{
		#region Common Elastic Index

		#endregion

		#region Credential Elastic Index
		public static List<CredentialIndex> Credential_SearchForElastic( string filter, int pageSize, int pageNumber, ref int pTotalRows )
		{
			string connectionString = DBConnectionRO();
			var index = new CredentialIndex();
			var list = new List<CredentialIndex>();
			var result = new DataTable();
			LoggingHelper.DoTrace( 2, "Credential_SearchForElastic - Starting. filter\r\n " + filter );
			bool includingHasPartIsPartWithConnections = UtilityManager.GetAppKeyValue( "includeHasPartIsPartWithConnections", false );
			bool usingEntityLastUpdatedDate = UtilityManager.GetAppKeyValue( "usingEntityLastUpdatedDateForIndexLastUpdated", true );
			int cntr = 0;
			DateTime started = DateTime.Now;

			using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				c.Open();

				using ( SqlCommand command = new SqlCommand( "[Credential.ElasticSearch]", c ) )
				{
					command.CommandType = CommandType.StoredProcedure;
					command.Parameters.Add( new SqlParameter( "@Filter", filter ) );
					command.Parameters.Add( new SqlParameter( "@SortOrder", "alpha" ) );
					command.Parameters.Add( new SqlParameter( "@StartPageIndex", pageNumber ) );
					command.Parameters.Add( new SqlParameter( "@PageSize", pageSize ) );
					//command.Parameters.Add( new SqlParameter( "@CurrentUserId", userId ) );

					command.CommandTimeout = 600;

					SqlParameter totalRows = new SqlParameter( "@TotalRows", pTotalRows );
					totalRows.Direction = ParameterDirection.Output;
					command.Parameters.Add( totalRows );

					try
					{
						using ( SqlDataAdapter adapter = new SqlDataAdapter() )
						{
							adapter.SelectCommand = command;
							adapter.Fill( result );
						}
						string rows = command.Parameters[ command.Parameters.Count - 1 ].Value.ToString();
						pTotalRows = Int32.Parse( rows );
					}
					catch ( Exception ex )
					{
						LoggingHelper.DoTrace( 2, "Credential_SearchForElastic - Exception:\r\n " + ex.Message );
						LoggingHelper.LogError( ex, "Credential_SearchForElastic", true, "Credential Search For Elastic Error" );
						index = new CredentialIndex();
						index.Name = "EXCEPTION ENCOUNTERED";
						index.Description = ex.Message;
						//index.CredentialTypeSchema = "error";
						list.Add( index );
						pTotalRows = -1;
						return list;
					}
				}

				//Used for costs. Only need to get these once. See below. - NA 5/12/2017
				//var currencies = CodesManager.GetCurrencies();
				//var costTypes = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_CREDENTIAL_ATTAINMENT_COST );
				int costProfilesCount = 0;
				string qualityAssurance = "";

				LoggingHelper.DoTrace( 2, string.Format( "Credential_SearchForElastic - loading {0} rows ", result.Rows.Count ) );
				try
				{

					foreach ( DataRow dr in result.Rows )
					{
						cntr++;
						if ( cntr % 200 == 0 )
							LoggingHelper.DoTrace( 2, string.Format( " loading record: {0}", cntr ) );
						//avgMinutes = 0;
						index = new CredentialIndex
						{
							EntityTypeId = 1,
							Id = GetRowColumn( dr, "Id", 0 ),
							//fine for a full reindex, need algorithm for small updates - could add to sql table
							NameIndex = cntr * 1000,

							//only full entities (3) will be in the index
							EntityStateId = GetRowPossibleColumn( dr, "EntityStateId", 0 ),
							Name = dr[ "Name" ].ToString()
						};
						if ( index.Id == 342 )
						{

						}
						if ( !string.IsNullOrWhiteSpace( dr[ "AlternateName" ].ToString() ) )
							index.AlternateNames.Add( dr[ "AlternateName" ].ToString() );

						index.FriendlyName = FormatFriendlyTitle( index.Name );

						index.SubjectWebpage = dr[ "SubjectWebpage" ].ToString();
						index.ImageURL = GetRowColumn( dr, "ImageUrl", "" );

						string rowId = dr[ "EntityUid" ].ToString();
						index.RowId = new Guid( rowId );

						index.Description = dr[ "Description" ].ToString();

						//index.OwnerOrganizationId = GetRowPossibleColumn( dr, "OwningOrganizationId", 0 );
						index.OwnerOrganizationId = Int32.Parse( dr[ "OwningOrganizationId" ].ToString() );

						index.OwnerOrganizationName = dr[ "OwningOrganization" ].ToString();
						if ( index.OwnerOrganizationName.Length > 0 )
							index.ListTitle = index.Name + " (" + index.OwnerOrganizationName + ")";
						else
							index.ListTitle = index.Name;
						//add helpers
						index.PrimaryOrganizationCTID = dr[ "OwningOrganizationCtid" ].ToString();
						//index.PrimaryOrganizationId = index.OwnerOrganizationId;
						//index.PrimaryOrganizationName = index.OwnerOrganizationName;

						index.CTID = dr[ "CTID" ].ToString();
						//
						index.CredentialType = dr[ "CredentialType" ].ToString();

						index.CredentialTypeSchema = dr[ "CredentialTypeSchema" ].ToString();
						index.CredentialStatus = dr[ "CredentialStatus" ].ToString();
						if ( !string.IsNullOrWhiteSpace( index.CredentialStatus )
							&& index.CredentialStatus != "Active" && index.Name.IndexOf( index.CredentialStatus ) == -1 )
						{
							index.Name += string.Format( " ({0})", index.CredentialStatus );
						}
						index.CredentialStatusId = GetRowColumn( dr, "CredentialStatusId", 0 );

						index.CredentialTypeId = Int32.Parse( dr[ "CredentialTypeId" ].ToString() );
						index.CredentialRegistryId = dr[ "CredentialRegistryId" ].ToString();

						string date = GetRowColumn( dr, "EffectiveDate", "" );
						if ( IsValidDate( date ) )
							index.DateEffective = ( DateTime.Parse( date ).ToShortDateString() );
						else
							index.DateEffective = "";
						date = GetRowColumn( dr, "Created", "" );
						if ( IsValidDate( date ) )
							index.Created = DateTime.Parse( date );
						date = GetRowColumn( dr, "LastUpdated", "" );
						if ( IsValidDate( date ) )
							index.LastUpdated = DateTime.Parse( date );
						//define LastUpdated to be EntityLastUpdated
						//TODO - add means to skip this for mass updates
						date = GetRowColumn( dr, "EntityLastUpdated", "" );
						if ( IsValidDate( date ) )
							index.LastUpdated = DateTime.Parse( date );
						
						index.AvailableOnlineAt = dr[ "AvailableOnlineAt" ].ToString();

						// index.NumberOfCostProfileItems = GetRowColumn( dr, "NumberOfCostProfileItems", 0 );
						index.TotalCost = GetRowPossibleColumn( dr, "TotalCost", 0 );
						//AverageMinutes is a rough approach to sorting. If present, get the duration profiles

						index.EstimatedTimeToEarn = GetRowPossibleColumn( dr, "AverageMinutes", 0 );
						//index.EstimatedTimeToEarn = Int32.Parse( dr[ "AverageMinutes" ].ToString() );
						index.IsAQACredential = GetRowColumn( dr, "IsAQACredential", false );

						index.RequiresCompetenciesCount = Int32.Parse( dr[ "RequiresCompetenciesCount" ].ToString() );
						index.LearningOppsCompetenciesCount = Int32.Parse( dr[ "LearningOppsCompetenciesCount" ].ToString() );
						index.AssessmentsCompetenciesCount = Int32.Parse( dr[ "AssessmentsCompetenciesCount" ].ToString() );
						index.HasPartCount = Int32.Parse( dr[ "HasPartCount" ].ToString() );
						index.IsPartOfCount = Int32.Parse( dr[ "IsPartOfCount" ].ToString() );
						index.RequiresCount = Int32.Parse( dr[ "RequiresCount" ].ToString() );
						index.RecommendsCount = Int32.Parse( dr[ "RecommendsCount" ].ToString() );
						index.EntryConditionCount = Int32.Parse( dr[ "EntryConditionCount" ].ToString() );

						index.RequiredForCount = Int32.Parse( dr[ "isRequiredForCount" ].ToString() );
						index.IsRecommendedForCount = Int32.Parse( dr[ "IsRecommendedForCount" ].ToString() );
						index.RenewalCount = Int32.Parse( dr[ "RenewalCount" ].ToString() );
						index.IsAdvancedStandingForCount = Int32.Parse( dr[ "IsAdvancedStandingForCount" ].ToString() );
						index.AdvancedStandingFromCount = Int32.Parse( dr[ "AdvancedStandingFromCount" ].ToString() );
						index.PreparationForCount = Int32.Parse( dr[ "isPreparationForCount" ].ToString() );

						index.PreparationFromCount = Int32.Parse( dr[ "isPreparationFromCount" ].ToString() );

						costProfilesCount = Int32.Parse( dr[ "costProfilesCount" ].ToString() );
					
						index.CommonConditionsCount = Int32.Parse( dr[ "CommonConditionsCount" ].ToString() );
						index.CommonCostsCount = Int32.Parse( dr[ "CommonCostsCount" ].ToString() );
						index.FinancialAidCount = Int32.Parse( dr[ "FinancialAidCount" ].ToString() );
						index.EmbeddedCredentialsCount = Int32.Parse( dr[ "EmbeddedCredentialsCount" ].ToString() );

						index.RequiredAssessmentsCount = Int32.Parse( dr[ "RequiredAssessmentsCount" ].ToString() );
						index.RequiredCredentialsCount = Int32.Parse( dr[ "RequiredCredentialsCount" ].ToString() );
						index.RequiredLoppCount = Int32.Parse( dr[ "RequiredLoppCount" ].ToString() );

						index.RecommendedAssessmentsCount = Int32.Parse( dr[ "RecommendedAssessmentsCount" ].ToString() );
						index.RecommendedCredentialsCount = Int32.Parse( dr[ "RecommendedCredentialsCount" ].ToString() );
						index.RecommendedLoppCount = Int32.Parse( dr[ "RecommendedLoppCount" ].ToString() );

						index.BadgeClaimsCount = Int32.Parse( dr[ "badgeClaimsCount" ].ToString() );
						index.RevocationProfilesCount = Int32.Parse( dr[ "RevocationProfilesCount" ].ToString() );
						index.ProcessProfilesCount = Int32.Parse( dr[ "ProcessProfilesCount" ].ToString() );
						//index.HasOccupationsCount = Int32.Parse( dr[ "HasOccupationsCount" ].ToString() );
						//index.HasIndustriesCount = Int32.Parse( dr[ "HasIndustriesCount" ].ToString() );
						//-actual connection type (no credential info), with the schema name, and number of connections
						// 8~Is Preparation For~ceterms:isPreparationFor~2
						index.ConnectionsList = dr[ "ConnectionsList" ].ToString();

						//connection type, plus Id, and name of credential
						//8~Is Preparation For~136~MSSC Certified Production Technician (CPT©)~| 8~Is Preparation For~272~MSSC Certified Logistics Technician (CLT©)~
						index.CredentialsList = dr[ "CredentialsList" ].ToString();
						index.IsPartOfList = dr[ "IsPartOfList" ].ToString();
						index.HasPartsList = dr[ "HasPartsList" ].ToString();
						if ( includingHasPartIsPartWithConnections )
						{
							index.CredentialsList += index.IsPartOfList;
							index.CredentialsList += index.HasPartsList;
						}
						string credentialConnections = dr[ "CredentialConnections" ].ToString();
						if ( !string.IsNullOrWhiteSpace( credentialConnections ) )
						{
							Connection conn = new Connection();
							var xDoc = XDocument.Parse( credentialConnections );
							foreach ( var child in xDoc.Root.Elements() )
							{
								conn = new Connection();
								conn.ConnectionType = ( string ) child.Attribute( "ConnectionType" ) ?? "";
								conn.ConnectionTypeId = int.Parse( child.Attribute( "ConnectionTypeId" ).Value );

								//do something with counts for this type

								conn.CredentialId = int.Parse( child.Attribute( "CredentialId" ).Value );
								if ( conn.CredentialId > 0 )
								{
									//add credential
									conn.Credential = ( string ) child.Attribute( "CredentialName" ) ?? "";
									//??????
									conn.CredentialOrgId = int.Parse( child.Attribute( "credOrgid" ).Value );
									conn.CredentialOrganization = ( string ) child.Attribute( "credOrganization" ) ?? "";
								}
								conn.AssessmentId = int.Parse( child.Attribute( "AssessmentId" ).Value );
								if ( conn.AssessmentId > 0 )
								{
									conn.Assessment = ( string ) child.Attribute( "AssessmentName" ) ?? "";
									conn.AssessmentOrganizationId = int.Parse( child.Attribute( "asmtOrgid" ).Value );
									conn.AssessmentOrganization = ( string ) child.Attribute( "asmtOrganization" ) ?? "";
								}
								conn.LoppId = int.Parse( child.Attribute( "LearningOpportunityId" ).Value );
								if ( conn.LoppId > 0 )
								{
									conn.LearningOpportunity = ( string ) child.Attribute( "LearningOpportunityName" ) ?? "";
									conn.LoppOrganizationId = int.Parse( child.Attribute( "loppOrgid" ).Value );
									conn.LearningOpportunityOrganization = ( string ) child.Attribute( "loppOrganization" ) ?? "";
								}

								index.Connections.Add( conn );
							}
						}


						#region QualityAssurance

						HandleAgentRelationshipsForEntity( dr, index );

						//2|7|6|10|11
						//string relationshipTypes = dr[ "RelationshipTypes" ].ToString();
						//if ( !string.IsNullOrWhiteSpace( relationshipTypes ) )
						//{
						//	foreach ( var name in relationshipTypes.Split( '|' ) )
						//	{
						//		if ( !string.IsNullOrWhiteSpace( name ) )
						//			index.AgentRelationships.Add( int.Parse( name ) );
						//	}
						//}

						//16-09-12 mp - changed to use pipe (|) rather than ; due to conflicts with actual embedded semicolons
						//QARolesList: includes roleId, roleName
						//1~Accredited~1~Credential| 2~Approved~1~Credential
						//index.QARolesResults = dr[ "QARolesList" ].ToString();

						//AgentAndRoles: includes roleId, roleName, OrgId, and orgName
						//1~Accredited~3~National Commission for Certifying Agencies (NCCA) [ reference ]~Credential
						index.AgentAndRoles = dr[ "AgentAndRoles" ].ToString();

						//QA on owning org
						//This should be combined with the credential QA - see publisher
						//this may not being used properly - no data other than blank and "not applicable" (remove this text). Should be:
						//QAOrgRolesList: includes roleId, roleName, number of organizations in role
						//10~Recognized~2~Organization| 12~Regulated~2~Organization
						//replacing the source
						//index.Org_QARolesList = GetRowPossibleColumn( dr, "QAOrgRolesList" );
						index.Org_QARolesList = dr[ "Org_QARolesList" ].ToString();

						//QAAgentAndRoles - List actual orgIds and names for roles
						//10~Owning Org is Recognized~1105~Accrediting Commission for Community and Junior Colleges - updated~Organization| 10~Owning Org is Recognized~64~AdvancED~Organization| 12~Owning Org is Regulated~55~TESTING_American National Standards Institute (ANSI)~Organization| 12~Owning Org is Regulated~64~AdvancED~Organization
						index.Org_QAAgentAndRoles = dr[ "Org_QAAgentAndRoles" ].ToString();

						#endregion

						#region Subjects
						var subjects = dr[ "Subjects" ].ToString();
						if ( !string.IsNullOrWhiteSpace( subjects ) )
						{
							var xDoc = new XDocument();
							xDoc = XDocument.Parse( subjects );
							foreach ( var child in xDoc.Root.Elements() )
							{
								var cs = new IndexSubject();
								var subject = child.Attribute( "Subject" );
								if ( subject != null )
								{
									cs.Name = subject.Value;

									//source is just direct/indirect, more want the sourceEntityType
									var source = child.Attribute( "Source" );
									if ( source != null )
										cs.Source = source.Value;

									int outputId = 0;
									var entityTypeId = child.Attribute( "EntityTypeId" );
									if ( entityTypeId != null )
										if ( Int32.TryParse( entityTypeId.Value, out outputId ) )
											cs.EntityTypeId = outputId;

									var referenceBaseId = child.Attribute( "ReferenceBaseId" );
									if ( referenceBaseId != null )
										if ( Int32.TryParse( referenceBaseId.Value, out outputId ) )
											cs.ReferenceBaseId = outputId;
									//may dump Subjects, for consistency
									index.Subjects.Add( cs );
									index.SubjectAreas.Add( cs.Name );
								}
							}
							if ( index.Subjects.Count() > 0 )
								index.HasSubjects = true;
						}
						#endregion

						#region Addresses
						string prevRegion = "";

						var addresses = dr[ "Addresses" ].ToString();
						if ( !string.IsNullOrWhiteSpace( addresses ) )
						{
							var xDoc = new XDocument();
							xDoc = XDocument.Parse( addresses );
							foreach ( var child in xDoc.Root.Elements() )
							{
								string region = ( string ) child.Attribute( "Region" ) ?? "";
								string city = ( string ) child.Attribute( "City" ) ?? "";
								index.Addresses.Add( new Address
								{
									Latitude = double.Parse( ( string ) child.Attribute( "Latitude" ) ),
									Longitude = double.Parse( ( string ) child.Attribute( "Longitude" ) ),
									Address1 = ( string ) child.Attribute( "Address1" ) ?? "",
									Address2 = ( string ) child.Attribute( "Address2" ) ?? "",
									City = ( string ) child.Attribute( "City" ) ?? "",
									AddressRegion = ( string ) child.Attribute( "Region" ) ?? "",
									PostalCode = ( string ) child.Attribute( "PostalCode" ) ?? "",
									Country = ( string ) child.Attribute( "Country" ) ?? ""
								} );
								AddTextValue( index, city );
								AddTextValue( index, region );
							}
						}
						if ( index.Addresses.Count == 0 )
						{
							//prototype: if no cred addresses, and one org address, then add to index (not detail page)
							var orgAddresses = dr[ "OrgAddresses" ].ToString();
							try
							{
								if ( !string.IsNullOrWhiteSpace( orgAddresses ) )
								{
									var xDoc = new XDocument();
									xDoc = XDocument.Parse( orgAddresses );
									//actually will only focus on regions

									//if ( xDoc.Root.Elements().Count() == 1)
									//{
									foreach ( var child in xDoc.Root.Elements() )
									{
										string region = ( string ) child.Attribute( "Region" ) ?? "";
										string city = ( string ) child.Attribute( "City" ) ?? "";
										if ( prevRegion != region )
										{
											index.Addresses.Add( new Address
											{
												Latitude = double.Parse( ( string ) child.Attribute( "Latitude" ) ),
												Longitude = double.Parse( ( string ) child.Attribute( "Longitude" ) ),
												Address1 = ( string ) child.Attribute( "Address1" ) ?? "",
												Address2 = ( string ) child.Attribute( "Address2" ) ?? "",
												//City = ( string )child.Attribute( "City" ) ?? "",
												AddressRegion = ( string ) child.Attribute( "Region" ) ?? "",
												//PostalCode = ( string )child.Attribute( "PostalCode" ) ?? "",
												Country = ( string ) child.Attribute( "Country" ) ?? ""
											} );
											prevRegion = region;
											AddTextValue( index, city );
											AddTextValue( index, region );
										}
										//should only be one, just in case some change in future
										//break;
									}
								}
								//}
							}
							catch ( Exception ex )
							{
								LoggingHelper.DoTrace( 2, string.Format( " # {0}. Exception on OrgAddresses id: {1}; \r\n{2}", cntr, index.Id, ex.Message ) );
							}
						}
						#endregion

						if ( index.BadgeClaimsCount > 0 )
							index.HasVerificationType_Badge = true;  //Update this with appropriate source data

						#region CrendentialProperties
						try
						{
							var credentialProperties = dr[ "CredentialProperties" ].ToString();
							if ( !string.IsNullOrEmpty( credentialProperties ) )
							{
								credentialProperties = credentialProperties.Replace( "&", " " );
								var xDoc = XDocument.Parse( credentialProperties );
								foreach ( var child in xDoc.Root.Elements() )
								{
									var categoryId = int.Parse( child.Attribute( "CategoryId" ).Value );
									var propertyValueId = int.Parse( child.Attribute( "PropertyValueId" ).Value );
									var property = child.Attribute( "Property" ).Value;
									var schemaName = ( string )child.Attribute( "PropertySchemaName" );

									index.CredentialProperties.Add( new IndexProperty {
										CategoryId = categoryId,
										Id = propertyValueId,
										Name = property, 
										SchemaName = schemaName
									} );
									if ( categoryId == (int)CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE )
										index.LearningDeliveryMethodTypeIds.Add( propertyValueId );
									if ( categoryId == (int)CodesManager.PROPERTY_CATEGORY_ASMT_DELIVERY_TYPE )
										index.AsmntDeliveryMethodTypeIds.Add( propertyValueId );
									if ( categoryId == ( int ) CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE )
										index.AudienceTypeIds.Add( propertyValueId );
									if ( categoryId == ( int ) CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL )
										index.AudienceLevelTypeIds.Add( propertyValueId );

									if ( !string.IsNullOrWhiteSpace( ( string )child.Attribute( "PropertySchemaName" ) ) )
										AddTextValue( index, ( string )child.Attribute( "PropertySchemaName" ) );
									//	index.TextValues.Add( ( string )child.Attribute( "PropertySchemaName" ) );
								}
							}
						}
						catch ( Exception ex )
						{
							LoggingHelper.DoTrace( 2, string.Format( " # {0}. Exception on CrendentialProperties id: {1}; \r\n{2}", cntr, index.Id, ex.Message ) );
						}
						#endregion

						#region TextValues, CodedNotation
						try
						{
							var textValues = dr[ "TextValues" ].ToString();
							if ( !string.IsNullOrWhiteSpace( textValues ) )
							{
								textValues = textValues.Replace( "&", " " );
								var xDoc = new XDocument();
								xDoc = XDocument.Parse( textValues );
								foreach ( var child in xDoc.Root.Elements() )
								{
									var categoryId = int.Parse( child.Attribute( "CategoryId" ).Value );
									var textValue = child.Attribute( "TextValue" );
									if ( textValue != null && !string.IsNullOrWhiteSpace( textValue.Value ) )
									{

										AddTextValue( index, textValue.Value );
										if ( textValue.Value.IndexOf( "-" ) > -1 )
											AddTextValue( index, textValue.Value.Replace( "-", "" ) );

										if ( categoryId == 35 ) //
											index.Keyword.Add( textValue.Value );
									}
									//source is just direct/indirect, more want the sourceEntityType
									var codeNotation = child.Attribute( "CodedNotation" );
									if ( codeNotation != null && !string.IsNullOrWhiteSpace( codeNotation.Value ) )
									{
										AddTextValue( index, codeNotation.Value );
										if ( codeNotation.Value.IndexOf( "-" ) > -1 )
											AddTextValue( index, codeNotation.Value.Replace( "-", "" ) );
										//index.TextValues.Add( codeNotation.Value.Replace( "-", "" ) );
									}
								}
							}


							if ( !string.IsNullOrWhiteSpace( index.AvailableOnlineAt ) )
								AddTextValue( index, index.AvailableOnlineAt );

							AddTextValue( index, index.CredentialType );
							//properties to add to textvalues

							string url = dr[ "AvailabilityListing" ].ToString();
							if ( !string.IsNullOrWhiteSpace( url ) )
								AddTextValue( index, url );
							//	index.TextValues.Add( url );

							if ( !string.IsNullOrWhiteSpace( index.CredentialRegistryId ) )
								index.TextValues.Add( index.CredentialRegistryId );
							string indexField = dr[ "CredentialId" ].ToString();
							if ( !string.IsNullOrWhiteSpace( indexField ) )
								AddTextValue( index, indexField );
							//index.TextValues.Add( indexField );

							AddTextValue( index, "credential " + index.Id.ToString() );
							AddTextValue( index, index.CTID );

						}
						catch ( Exception ex )
						{
							LoggingHelper.DoTrace( 2, string.Format( " # {0}. Exception on TextValues, CodedNotation id: {1}; \r\n{2}", cntr, index.Id, ex.Message ) );
						}
						#endregion


						#region Languages
						index.InLanguage = GetLanguages( dr );
						#endregion

						#region Competencies
						//these are competencies from condition profiles
						//not sure we want these in the index
						string requiresCompetencies = dr[ "Competencies" ].ToString();
						if ( !string.IsNullOrWhiteSpace( requiresCompetencies ) )
						{
							if ( ContainsUnicodeCharacter( requiresCompetencies ) )
							{
								requiresCompetencies = Regex.Replace( requiresCompetencies, @"[^\u0000-\u007F]+", string.Empty );
							}
							requiresCompetencies = requiresCompetencies.Replace( "&", " " );
							var xDoc = new XDocument();
							xDoc = XDocument.Parse( requiresCompetencies );
							foreach ( var child in xDoc.Root.Elements() )
							{
								var competency = new IndexCompetency
								{
									Name = ( string ) child.Attribute( "Name" ) ?? "",
									Description = ( string ) child.Attribute( "Description" ) ?? ""
								};

								index.Competencies.Add( competency );
							}
						}
						#endregion

						#region Reference Frameworks - industries, occupations and classifications
						string frameworks = dr[ "Frameworks" ].ToString();
						if ( !string.IsNullOrWhiteSpace( frameworks ) )
						{
							var xDoc = new XDocument();
							xDoc = XDocument.Parse( frameworks );
							foreach ( var child in xDoc.Root.Elements() )
							{
								var framework = new IndexReferenceFramework
								{
									CategoryId = int.Parse( child.Attribute( "CategoryId" ).Value ),
									ReferenceFrameworkId = int.Parse( child.Attribute( "ReferenceFrameworkId" ).Value ),
									Name = ( string ) child.Attribute( "Name" ) ?? "",
									CodeGroup = ( string ) child.Attribute( "CodeGroup" ) ?? "",
									SchemaName = ( string ) child.Attribute( "SchemaName" ) ?? "",
									CodedNotation = ( string ) child.Attribute( "CodedNotation" ) ?? "",
								};

								//index.Frameworks.Add( framework );
								if ( framework.CategoryId == 11 )
								{
									index.Occupations.Add( framework );
									AddTextValue( index, "occupation " + framework.Name );
								}
								else if ( framework.CategoryId == 10 )
								{
									index.Industries.Add( framework );
									AddTextValue( index, "industry " + framework.Name );
								}
								else if ( framework.CategoryId == 23 )
								{
									index.Classifications.Add( framework );
									AddTextValue( index, "program " + framework.Name );
								}
								
							}//
							if( index.Occupations.Count > 0)
								index.HasOccupations = true;
							if ( index.Industries.Count > 0 )
								index.HasIndustries = true;
							if ( index.Classifications.Count > 0 )
								index.HasInstructionalPrograms = true;

						}
						
						#endregion

						#region Custom Reports
						int propertyId = 0;

						AddReportProperty( index, index.AvailableOnlineAt, 58, "Available Online", "credReport:AvailableOnline" );
						//if ( !string.IsNullOrWhiteSpace( index.AvailableOnlineAt ) )
						//	if ( GetPropertyId( 58, "credReport:AvailableOnline", ref propertyId ) )
						//		index.ReportFilters.Add( propertyId );

						AddReportProperty( index, index.EmbeddedCredentialsCount, 58, "Has Embedded Credentials", "credReport:HasEmbeddedCredentials" );
						//if ( index.EmbeddedCredentialsCount > 0 )
						//	if ( GetPropertyId( 58, "credReport:HasEmbeddedCredentials", ref propertyId ) )
						//		index.ReportFilters.Add( propertyId );
						//
						AddReportProperty( index, costProfilesCount, 58, "Has cost profile", "credReport:HasCostProfile" );
						//if ( costProfilesCount > 0 )
						//	if ( GetPropertyId( 58, "credReport:HasCostProfile", ref propertyId ) )
						//		index.ReportFilters.Add( propertyId );
						AddReportProperty( index, index.CommonConditionsCount, 58, "References Common Conditions", "credReport:ReferencesCommonConditions" );
						//if ( index.CommonConditionsCount > 0 )
						//	if ( GetPropertyId( 58, "credReport:ReferencesCommonConditions", ref propertyId ) )
						//		index.ReportFilters.Add( propertyId );
						AddReportProperty( index, index.CommonCostsCount, 58, "References Common Costs", "credReport:ReferencesCommonCosts" );
						//if ( index.CommonCostsCount > 0 )
						//	if ( GetPropertyId( 58, "credReport:ReferencesCommonCosts", ref propertyId ) )
						//		index.ReportFilters.Add( propertyId );
						AddReportProperty( index, index.FinancialAidCount, 58, "Has Financial Aid", "credReport:FinancialAid" );
						//if ( index.FinancialAidCount > 0 )
						//	if ( GetPropertyId( 58, "credReport:FinancialAid", ref propertyId ) )
						//		index.ReportFilters.Add( propertyId );
						AddReportProperty( index, index.RequiredAssessmentsCount, 58, "Requires Assessment", "credReport:RequiresAssessment" );
						//if ( index.RequiredAssessmentsCount > 0 )
						//	if ( GetPropertyId( 58, "credReport:RequiresAssessment", ref propertyId ) )
						//		index.ReportFilters.Add( propertyId );
						AddReportProperty( index, index.RequiredCredentialsCount, 58, "Requires Credential", "credReport:RequiresCredential" );
						//if ( index.RequiredCredentialsCount > 0 )
						//	if ( GetPropertyId( 58, "credReport:RequiresCredential", ref propertyId ) )
						//		index.ReportFilters.Add( propertyId );
						AddReportProperty( index, index.RequiredLoppCount, 58, "Requires Learning Opportunity", "credReport:RequiresLearningOpportunity" );
						//if ( index.RequiredLoppCount > 0 )
						//	if ( GetPropertyId( 58, "credReport:RequiresLearningOpportunity", ref propertyId ) )
						//		index.ReportFilters.Add( propertyId );
						AddReportProperty( index, index.RecommendedAssessmentsCount, 58, "Recommends Assessment", "credReport:RecommendsAssessment" );
						//if ( index.RecommendedAssessmentsCount > 0 )
						//	if ( GetPropertyId( 58, "credReport:RecommendsAssessment", ref propertyId ) )
						//		index.ReportFilters.Add( propertyId );
						AddReportProperty( index, index.RecommendedCredentialsCount, 58, "Recommends Credential", "credReport:RecommendsCredential" );
						//if ( index.RecommendedCredentialsCount > 0 )
						//	if ( GetPropertyId( 58, "credReport:RecommendsCredential", ref propertyId ) )
						//		index.ReportFilters.Add( propertyId );
						AddReportProperty( index, index.RecommendedLoppCount, 58, "Recommends Learning Opportunity", "credReport:RecommendsLearningOpportunity" );
						//if ( index.RecommendedLoppCount > 0 )
						//	if ( GetPropertyId( 58, "credReport:RecommendsLearningOpportunity", ref propertyId ) )
						//		index.ReportFilters.Add( propertyId );
						AddReportProperty( index, index.BadgeClaimsCount, 58, "Has Verification Badges", "credReport:HasVerificationBadges" );
						//if ( index.BadgeClaimsCount > 0 )
						//	if ( GetPropertyId( 58, "credReport:HasVerificationBadges", ref propertyId ) )
						//		index.ReportFilters.Add( propertyId );
						AddReportProperty( index, index.RevocationProfilesCount, 58, "Has Revocation", "credReport:HasRevocation" );
						//if ( index.RevocationProfilesCount > 0 )
						//	if ( GetPropertyId( 58, "credReport:HasRevocation", ref propertyId ) )
						//		index.ReportFilters.Add( propertyId );
						AddReportProperty( index, index.ProcessProfilesCount, 58, "Has Process Profile", "credReport:HasProcessProfile" );
						//if ( index.ProcessProfilesCount > 0 )
						//	if ( GetPropertyId( 58, "credReport:HasProcessProfile", ref propertyId ) )
						//		index.ReportFilters.Add( propertyId );
						AddReportProperty( index, index.HasOccupations, 58, "Has Occupations", "credReport:HasOccupations" );
						//if ( index.HasOccupations  )
						//	if ( GetPropertyId( 58, "credReport:HasOccupations", ref propertyId ) )
						//		index.ReportFilters.Add( propertyId );
						AddReportProperty( index, index.HasIndustries, 58, "Has Industries", "credReport:HasIndustries" );
						//if ( index.HasIndustries  )
						//	if ( GetPropertyId( 58, "credReport:HasIndustries", ref propertyId ) )
						//		index.ReportFilters.Add( propertyId );
						AddReportProperty( index, index.HasInstructionalPrograms, 58, "Has Programs/CIP", "credReport:HasCIP" );
						//if ( index.HasInstructionalPrograms )
						//	if ( GetPropertyId( 58, "credReport:HasCIP", ref propertyId ) )
						//		index.ReportFilters.Add( propertyId );
						AddReportProperty( index, index.IsPartOfCount, 58, "Has Is Part Of Credential", "credReport:IsPartOfCredential" );
						//if ( index.IsPartOfCount > 0 )
						//	if ( GetPropertyId( 58, "credReport:IsPartOfCredential", ref propertyId ) )
						//		index.ReportFilters.Add( propertyId );
						
						AddReportProperty( index, index.Addresses.Count, 58, "Has Addresses", "credReport:HasAddresses" );
						//if ( index.Addresses.Count > 0 )
						//	if ( GetPropertyId( 58, "credReport:HasAddresses", ref propertyId ) )
						//		index.ReportFilters.Add( propertyId );

						AddReportProperty( index, index.RequiresCompetenciesCount, 58, "Requires Competencies", "credReport:RequiresCompetencies" );
						//if ( index.RequiresCompetenciesCount > 0 )
						//{
						//	if ( GetPropertyId( 58, "credReport:RequiresCompetencies", ref propertyId ) )
						//		index.ReportFilters.Add( propertyId );
						//}
						AddReportProperty( index, ( index.RequiresCompetenciesCount > 0 || index.AssessmentsCompetenciesCount > 0 || index.LearningOppsCompetenciesCount > 0 ), 58, "Has Competencies", "credReport:HasCompetencies" );
						//if ( index.RequiresCompetenciesCount > 0 || index.AssessmentsCompetenciesCount > 0 || index.LearningOppsCompetenciesCount > 0 )
						//{
						//	if ( GetPropertyId( 58, "credReport:HasCompetencies", ref propertyId ) )
						//		index.ReportFilters.Add( propertyId );
						//}
						//
						var HasConditionProfileCount = GetRowPossibleColumn( dr, "HasConditionProfileCount", 0 );
						AddReportProperty( index, HasConditionProfileCount, 58, "Has Condition Profile", "credReport:HasConditionProfile" );
						//if ( HasConditionProfileCount > 0 )
						//{
						//	if ( GetPropertyId( 58, "credReport:HasConditionProfile", ref propertyId ) )
						//		index.ReportFilters.Add( propertyId );
						//}
						//
						
						var DurationProfileCount = GetRowPossibleColumn( dr, "HasDurationCount", 0 );
						AddReportProperty( index, index.Addresses.Count, 58, "Has Duration Profile", "credReport:HasDurationProfile" );
						//if ( DurationProfileCount > 0 )
						//{

						//	if ( GetPropertyId( 58, "credReport:HasDurationProfile", ref propertyId ) )
						//		index.ReportFilters.Add( propertyId );
						//}
						#endregion

						list.Add( index );
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.DoTrace( 2, string.Format( "Credential_SearchForElastic. Last Row: {0}, CredentialId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
				}
				finally
				{
					DateTime completed = DateTime.Now;
					var duration = completed.Subtract( started ).TotalSeconds;

					LoggingHelper.DoTrace( 2, string.Format( "Credential_SearchForElastic - Completed. loaded {0} records, in {1} seconds", cntr, duration ) );
				}

				return list;
			}
		}
		public static void AddTextValue( IIndex index, string input, bool addingToPremium = false )
		{
			if ( string.IsNullOrWhiteSpace( input ) )
				return;
			//check if exists
			if ( index.TextValues.FindIndex( a => a == input ) < 0 )
				index.TextValues.Add( input.Trim() );
			if ( addingToPremium )
			{
				if ( index.PremiumValues.FindIndex( a => a == input ) < 0 )
					index.PremiumValues.Add( input.Trim() );
			}
		}
		public static void AddReportProperty( IIndex index, int count, int reportCategoryId, string reportTextName, string reportSchemaname )
		{
			int propertyId = 0;
			if ( count > 0 )
				if ( GetPropertyId( reportCategoryId, reportSchemaname, ref propertyId ) )
				{
					index.ReportFilters.Add( propertyId );
					AddTextValue( index, reportTextName );
				}
		}
		public static void AddReportProperty( IIndex index, bool hasData, int reportCategoryId, string reportTextName, string reportSchemaname )
		{
			int propertyId = 0;
			if ( hasData )
				if ( GetPropertyId( reportCategoryId, reportSchemaname, ref propertyId ) )
				{
					index.ReportFilters.Add( propertyId );
					AddTextValue( index, reportTextName );
				}
		}
		public static void AddReportProperty( IIndex index, string input, int reportCategoryId, string reportTextName, string reportSchemaname )
		{
			int propertyId = 0;
			if ( !string.IsNullOrWhiteSpace( input ) )
				if ( GetPropertyId( reportCategoryId, reportSchemaname, ref propertyId ) )
				{
					index.ReportFilters.Add( propertyId );
					AddTextValue( index, reportTextName );
				}
		}

		public static bool ContainsUnicodeCharacter( string input )
		{
			const int MaxAnsiCode = 255;

			return input.Any( c => c > MaxAnsiCode );
		}
		public static List<CM.CredentialSummary> Credential_MapFromElastic( List<CredentialIndex> credentials )
		{
			var list = new List<CM.CredentialSummary>();

			var currencies = CodesManager.GetCurrencies();
			var costTypes = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_CREDENTIAL_ATTAINMENT_COST );
			bool includingHasPartIsPartWithConnections = UtilityManager.GetAppKeyValue( "includeHasPartIsPartWithConnections", false );

			foreach ( var ci in credentials )
			{
				var index = new CM.CredentialSummary();

				//avgMinutes = 0;
				index = new CM.CredentialSummary
				{
					Id = ci.Id,
					Name = ci.Name,
					FriendlyName = ci.FriendlyName,
					SubjectWebpage = ci.SubjectWebpage,
					EntityStateId = ci.EntityStateId,
					RowId = ci.RowId,
					Description = ci.Description,
					OwnerOrganizationId = ci.OwnerOrganizationId,
					OwnerOrganizationName = ci.OwnerOrganizationName,
					CTID = ci.CTID,
					PrimaryOrganizationCTID = ci.PrimaryOrganizationCTID,
					CredentialRegistryId = ci.CredentialRegistryId,
					DateEffective = ci.DateEffective,
					Created = ci.Created,
					//define LastUpdated to be EntityLastUpdated
					LastUpdated = ci.LastUpdated,
					CredentialType = ci.CredentialType,
					CredentialTypeSchema = ci.CredentialTypeSchema,
					TotalCost = ci.TotalCost,
					IsAQACredential = ci.IsAQACredential,
					LearningOppsCompetenciesCount = ci.LearningOppsCompetenciesCount,
					AssessmentsCompetenciesCount = ci.AssessmentsCompetenciesCount,
					//QARolesCount = ci.QARolesCount,
					HasPartCount = ci.HasPartCount,
					IsPartOfCount = ci.IsPartOfCount,
					RequiresCount = ci.RequiresCount,
					RecommendsCount = ci.RecommendsCount,
					RequiredForCount = ci.RequiredForCount,
					IsRecommendedForCount = ci.IsRecommendedForCount,
					RenewalCount = ci.RenewalCount,
					IsAdvancedStandingForCount = ci.IsAdvancedStandingForCount,
					AdvancedStandingFromCount = ci.AdvancedStandingFromCount,
					PreparationForCount = ci.PreparationForCount,
					PreparationFromCount = ci.PreparationFromCount,
					//NumberOfCostProfileItems = ci.NumberOfCostProfileItems,
					HasVerificationType_Badge = ci.HasVerificationType_Badge,
					//TotalCostCount = ci.TotalCostCount,
					CommonCostsCount = ci.CommonCostsCount,
					CommonConditionsCount = ci.CommonConditionsCount,
					FinancialAidCount = ci.FinancialAidCount

				};
				if ( ci.ImageURL != null && ci.ImageURL.Trim().Length > 0 )
					index.ImageUrl = ci.ImageURL;
				else
					index.ImageUrl = null;
				//AverageMinutes is a rough approach to sorting. If present, get the duration profiles
				//if ( ci.EstimatedTimeToEarn > 0 )
				index.EstimatedTimeToEarn = DurationProfileManager.GetAll( index.RowId );

				if ( ci.Industries != null && ci.Industries.Count > 0 )
					index.IndustryResults = Fill_CodeItemResults( ci.Industries.Where( x => x.CategoryId == 10 ).ToList(), CodesManager.PROPERTY_CATEGORY_NAICS, false, false );

				if ( ci.Occupations != null && ci.Occupations.Count > 0 )
					index.OccupationResults = Fill_CodeItemResults( ci.Occupations.Where( x => x.CategoryId == 11 ).ToList(), CodesManager.PROPERTY_CATEGORY_SOC, false, false );

				if ( ci.Classifications != null && ci.Classifications.Count > 0 )
					index.InstructionalProgramClassification = Fill_CodeItemResults( ci.Classifications.Where( x => x.CategoryId == 23 ).ToList(), CodesManager.PROPERTY_CATEGORY_CIP, false, false );
			
				index.Org_QARolesResults = Fill_CodeItemResults( ci.Org_QARolesList, 130, false, true );
				index.Org_QAAgentAndRoles = Fill_AgentRelationship( ci.Org_QAAgentAndRoles, 130, false, false, true, "Organization" );
				index.AgentAndRoles = Fill_AgentRelationship( ci.AgentAndRoles, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false, true, "Credential" );

				index.ConnectionsList = Fill_CodeItemResults( ci.ConnectionsList, CodesManager.PROPERTY_CATEGORY_CONNECTION_PROFILE_TYPE, true, true );

				if ( includingHasPartIsPartWithConnections )
				{
					//manually add other connections
					if ( ci.HasPartCount > 0 )
					{
						index.ConnectionsList.Results.Add( new CodeItem() { Id = 0, Title = "Includes", SchemaName = "hasPart", Totals = ci.HasPartCount } );
					}
					if ( ci.IsPartOfCount > 0 )
					{
						index.ConnectionsList.Results.Add( new CodeItem() { Id = 0, Title = "Included With", SchemaName = "isPartOf", Totals = ci.IsPartOfCount } );
					}
				}

				index.HasPartsList = Fill_CredentialConnectionsResult( ci.HasPartsList, CodesManager.PROPERTY_CATEGORY_CONNECTION_PROFILE_TYPE );

				index.IsPartOfList = Fill_CredentialConnectionsResult( ci.IsPartOfList, CodesManager.PROPERTY_CATEGORY_CONNECTION_PROFILE_TYPE );

				index.CredentialsList = Fill_CredentialConnectionsResult( ci.CredentialsList, CodesManager.PROPERTY_CATEGORY_CONNECTION_PROFILE_TYPE );
				index.ListTitle = ci.ListTitle;
				index.Subjects = ci.Subjects.Select( x => x.Name ).Distinct().ToList();

				index.AssessmentDeliveryType = Fill_CodeItemResults( ci.CredentialProperties.Where(x=> x.CategoryId == (int) CodesManager.PROPERTY_CATEGORY_ASMT_DELIVERY_TYPE).ToList(), CodesManager.PROPERTY_CATEGORY_ASMT_DELIVERY_TYPE, false, false );
				index.LearningDeliveryType = Fill_CodeItemResults( ci.CredentialProperties.Where( x => x.CategoryId == ( int ) CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE ).ToList(), CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE, false, false );

				//16-09-12 mp - changed to use pipe (|) rather than ; due to conflicts with actual embedded semicolons
				index.AudienceLevelsResults = Fill_CodeItemResults( ci.CredentialProperties.Where( x => x.CategoryId == ( int ) CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL ).ToList(), CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL, false, false );
				index.AudienceTypesResults = Fill_CodeItemResults( ci.CredentialProperties.Where( x => x.CategoryId == ( int ) CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE ).ToList(), CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE, false, false );

				//addressess                

				index.Addresses = ci.Addresses.Select( x => new CM.Address
				{
					Latitude = x.Latitude,
					Longitude = x.Longitude,
					Address1 = x.Address1,
					Address2 = x.Address2,
					City = x.City,
					AddressRegion = x.AddressRegion,
					PostalCode = x.PostalCode,
					Country = x.Country
				} ).ToList();

				list.Add( index );
			}

			return list;
		}


		#endregion

		#region Organization Elastic Index
		public static List<OrganizationIndex> Organization_SearchForElastic( string filter )
		{
			string connectionString = DBConnectionRO();
			var index = new OrganizationIndex();
			var list = new List<OrganizationIndex>();
			var result = new DataTable();
			LoggingHelper.DoTrace( 2, "GetAllForElastic - Starting. filter\r\n " + filter );
			int cntr = 0;
			using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				c.Open();

				using ( SqlCommand command = new SqlCommand( "[Organization.ElasticSearch]", c ) )
				{
					command.CommandType = CommandType.StoredProcedure;
					command.Parameters.Add( new SqlParameter( "@Filter", filter ) );
					command.Parameters.Add( new SqlParameter( "@SortOrder", "alpha" ) );
					command.Parameters.Add( new SqlParameter( "@StartPageIndex", "0" ) );
					command.Parameters.Add( new SqlParameter( "@PageSize", "0" ) );
					//command.Parameters.Add( new SqlParameter( "@CurrentUserId", userId ) );
					command.CommandTimeout = 300;

					SqlParameter totalRows = new SqlParameter( "@TotalRows", 0 );
					totalRows.Direction = ParameterDirection.Output;
					command.Parameters.Add( totalRows );

					try
					{
						using ( SqlDataAdapter adapter = new SqlDataAdapter() )
						{
							adapter.SelectCommand = command;
							adapter.Fill( result );
						}
						string rows = command.Parameters[ command.Parameters.Count - 1 ].Value.ToString();
					}
					catch ( Exception ex )
					{
						index = new OrganizationIndex();
						index.Name = "EXCEPTION ENCOUNTERED";
						index.Description = ex.Message;
						list.Add( index );

						return list;
					}
				}
				try
				{


					foreach ( DataRow dr in result.Rows )
					{
						cntr++;
						if ( cntr % 100 == 0 )
							LoggingHelper.DoTrace( 2, string.Format( " Organization loading record: {0}", cntr ) );
						if ( cntr == 33 )
						{

						}

						index = new OrganizationIndex();
						index.EntityTypeId = 2;
						index.Id = GetRowColumn( dr, "Id", 0 );
						index.EntityStateId = GetRowColumn( dr, "EntityStateId", 1 );
						index.NameIndex = cntr * 1000;
						index.Name = GetRowColumn( dr, "Name", "missing" );
						index.FriendlyName = FormatFriendlyTitle( index.Name );
						index.SubjectWebpage = GetRowColumn( dr, "SubjectWebpage", "" );

						string rowId = GetRowColumn( dr, "RowId" );
						if ( IsValidGuid( rowId ) )
							index.RowId = new Guid( rowId );

						index.Description = GetRowColumn( dr, "Description", "" );
						index.CTID = GetRowPossibleColumn( dr, "CTID", "" );
						//add helpers
						index.PrimaryOrganizationCTID = index.CTID;
						index.PrimaryOrganizationId = index.Id;
						index.PrimaryOrganizationName = index.Name;
						index.CredentialRegistryId = GetRowPossibleColumn( dr, "CredentialRegistryId", "" );

						string date = GetRowColumn( dr, "Created", "" );
						if ( IsValidDate( date ) )
							index.Created = DateTime.Parse( date );
						date = GetRowColumn( dr, "LastUpdated", "" );
						if ( IsValidDate( date ) )
							index.LastUpdated = DateTime.Parse( date );
						//define LastUpdated to be EntityLastUpdated
						date = GetRowColumn( dr, "EntityLastUpdated", "" );
						if ( IsValidDate( date ) )
							index.LastUpdated = DateTime.Parse( date );

						index.ImageURL = GetRowColumn( dr, "ImageUrl", "" );
						if ( GetRowColumn( dr, "CredentialCount", 0 ) > 0 )
							index.IsACredentialingOrg = true;
						index.ISQAOrganization = GetRowColumn( dr, "IsAQAOrganization", false );
						//index.MainPhoneNumber = CM.PhoneNumber.DisplayPhone( GetRowColumn( dr, "MainPhoneNumber", "" ) );

						index.OwnedByResults = dr[ "OwnedByList" ].ToString();

						index.OfferedByResults = dr[ "OfferedByList" ].ToString();

						index.AsmtsOwnedByResults = dr[ "AsmtsOwnedByList" ].ToString();
						index.LoppsOwnedByResults = dr[ "LoppsOwnedByList" ].ToString();
						index.FrameworksOwnedByResults = dr[ "FrameworksOwnedByList" ].ToString();
						index.ApprovedByResults = dr[ "ApprovedByList" ].ToString();
						index.AccreditedByResults = dr[ "AccreditedByList" ].ToString();
						index.RecognizedByResults = dr[ "RecognizedByList" ].ToString();
						index.RegulatedByResults = dr[ "RegulatedByList" ].ToString();
						index.VerificationProfilesCount = GetRowPossibleColumn( dr, "VerificationProfilesCount", 0 );
						index.CostManifestsCount = GetRowPossibleColumn( dr, "CostManifestsCount", 0 );
						index.ConditionManifestsCount = GetRowPossibleColumn( dr, "ConditionManifestsCount", 0 );
						index.SubsidiariesCount = GetRowPossibleColumn( dr, "SubsidiariesCount", 0 );
						index.DepartmentsCount = GetRowPossibleColumn( dr, "DepartmentsCount", 0 );
						index.HasIndustriesCount = GetRowPossibleColumn( dr, "HasIndustriesCount", 0 );

						#region Addresses
						var addresses = dr[ "Addresses" ].ToString();
						if ( !string.IsNullOrWhiteSpace( addresses ) )
						{
							//index.Addresses = addresses;
							var xDoc = new XDocument();
							//index.AddressesCount = xDoc.Root.Elements().Count();
							xDoc = XDocument.Parse( addresses );

							foreach ( var child in xDoc.Root.Elements() )
							{
								string region = ( string ) child.Attribute( "Region" ) ?? "";
								string city = ( string ) child.Attribute( "City" ) ?? "";
								index.Addresses.Add( new Address
								{
									Latitude = double.Parse( ( string ) child.Attribute( "Latitude" ) ),
									Longitude = double.Parse( ( string ) child.Attribute( "Longitude" ) ),
									Address1 = ( string ) child.Attribute( "Address1" ) ?? "",
									Address2 = ( string ) child.Attribute( "Address2" ) ?? "",
									City = ( string ) child.Attribute( "City" ) ?? "",
									AddressRegion = ( string ) child.Attribute( "Region" ) ?? "",
									PostalCode = ( string ) child.Attribute( "PostalCode" ) ?? "",
									Country = ( string ) child.Attribute( "Country" ) ?? ""
								} );
								AddTextValue( index, city );
								AddTextValue( index, region );
							}

						}
						#endregion


						#region TextValues - alternate name + ?

						//string textValues = GetRowPossibleColumn( dr, "TextValues" );
						//if ( !string.IsNullOrWhiteSpace( textValues ) )
						//{
						//    foreach ( var text in textValues.Split( '|' ) )
						//    {
						//        if ( !string.IsNullOrWhiteSpace( text ) )
						//            index.TextValues.Add( text );
						//    }
						//}
						var textValues = dr[ "TextValues" ].ToString();
						if ( !string.IsNullOrWhiteSpace( textValues ) )
						{
							textValues = textValues.Replace( "&", " " );

							var xDoc = new XDocument();
							xDoc = XDocument.Parse( textValues );
							//index.Addresses = xDoc.Root.Elements().Count();
							foreach ( var child in xDoc.Root.Elements() )
							{
								var categoryId = int.Parse( child.Attribute( "CategoryId" ).Value );
								var textValue = child.Attribute( "TextValue" );
								if ( textValue != null )
								{
									index.TextValues.Add( textValue.Value );

									if ( textValue.Value.IndexOf( "-" ) > -1 )
										index.TextValues.Add( textValue.Value.Replace( "-", "" ) );

									if ( categoryId == 35 )
										index.Keyword.Add( textValue.Value );
								}
							}
						}

						string alternatesNames = GetRowPossibleColumn( dr, "AlternatesNames" );
						if ( !string.IsNullOrWhiteSpace( alternatesNames ) )
						{
							var xDoc = XDocument.Parse( alternatesNames );
							foreach ( var child in xDoc.Root.Elements() )
							{
								string textValue = ( string ) ( child.Attribute( "TextValue" ) ) ?? "";
								if ( !string.IsNullOrWhiteSpace( textValue ) )
									index.TextValues.Add( textValue );
							}
						}

						string identifiers = GetRowPossibleColumn( dr, "AlternateIdentifiers" );
						if ( !string.IsNullOrWhiteSpace( identifiers ) )
						{
							var xDoc = XDocument.Parse( identifiers );
							foreach ( var child in xDoc.Root.Elements() )
							{
								string title = ( string ) ( child.Attribute( "Title" ) ) ?? "";
								string textValue = ( string ) ( child.Attribute( "TextValue" ) ) ?? "";
								if ( !string.IsNullOrWhiteSpace( title ) && !string.IsNullOrWhiteSpace( textValue ) )
									index.TextValues.Add( title + " " + textValue.Replace( "-", "" ) );
							}
						}

						////properties to add to textvalues
						string url = GetRowPossibleColumn( dr, "AvailabilityListing", "" );
						if ( !string.IsNullOrWhiteSpace( url ) )
							index.TextValues.Add( url );
						if ( !string.IsNullOrWhiteSpace( index.CredentialRegistryId ) )
							index.TextValues.Add( index.CredentialRegistryId );
						index.TextValues.Add( index.Id.ToString() );
						if ( !string.IsNullOrWhiteSpace( index.CTID ) )
							index.TextValues.Add( index.CTID );

						#endregion

						#region PropertyValues  - ServiceTypes, OrgTypes, SectorTypes 
						//THESE ARE THE SAME AS FOR PropertyValues, DO WE NEED THEM
						string propertyValues = GetRowPossibleColumn( dr, "PropertyValues" );
						if ( !string.IsNullOrWhiteSpace( propertyValues ) )
						{
							var xDoc = XDocument.Parse( propertyValues );
							foreach ( var child in xDoc.Root.Elements() )
							{
								var prop = new IndexProperty
								{
									CategoryId = int.Parse( child.Attribute( "CategoryId" ).Value ),
									Id = int.Parse( child.Attribute( "PropertyValueId" ).Value ),
									Name = ( string ) child.Attribute( "Property" ),
									SchemaName = ( string ) child.Attribute( "PropertySchemaName" )
								};
								if ( prop.CategoryId == 6 )
								{
									index.OrganizationServiceTypes.Add( prop );
									index.OrganizationServiceTypeIds.Add( prop.Id );
									//AddTextValue( index, prop.SchemaName );
								} else if ( prop.CategoryId == 7 )
								{
									index.OrganizationTypes.Add( prop );
									index.OrganizationTypeIds.Add( prop.Id );
									//AddTextValue( index, prop.SchemaName );
								}else if ( prop.CategoryId == 30 )
								{
									index.OrganizationSectorTypes.Add( prop );
									index.OrganizationSectorTypeIds.Add( prop.Id );
								}
								//claim types are handled separately now
								//else if ( prop.CategoryId == 41 )
								//{
								//	index.OrganizationClaimTypes.Add( prop );
								//	index.OrganizationClaimTypeIds.Add( prop.Id );
								//}
								if ( !string.IsNullOrWhiteSpace( ( string ) child.Attribute( "Property" ) ) )
									index.TextValues.Add( ( string ) child.Attribute( "Property" ) );
								if ( !string.IsNullOrWhiteSpace( ( string )child.Attribute( "PropertySchemaName" ) ) )
									index.TextValues.Add( ( string )child.Attribute( "PropertySchemaName" ) );
							}
						}

						string claimTypes = GetRowPossibleColumn( dr, "ClaimTypes" );
						if ( !string.IsNullOrWhiteSpace( claimTypes ) )
						{
							var xDoc = XDocument.Parse( claimTypes );
							foreach ( var child in xDoc.Root.Elements() )
							{
								var prop = new IndexProperty
								{
									CategoryId = int.Parse( child.Attribute( "CategoryId" ).Value ),
									Id = int.Parse( child.Attribute( "PropertyValueId" ).Value ),
									Name = ( string ) child.Attribute( "Property" ),
									SchemaName = ( string ) child.Attribute( "PropertySchemaName" )
								};

								index.OrganizationClaimTypes.Add( prop );
								index.OrganizationClaimTypeIds.Add( prop.Id );

								if ( !string.IsNullOrWhiteSpace( ( string ) child.Attribute( "Property" ) ) )
									index.TextValues.Add( ( string ) child.Attribute( "Property" ) );
							}
						}
						#endregion

						#region ReferenceFrameworks
						/*not used - need to use:
                        * Reference.Frameworks
                        * Entity.ReferenceFramework
                        * Entity_ReferenceFramework_Summary
                       */

						string Industries = GetRowPossibleColumn( dr, "Industries" );
						if ( !string.IsNullOrWhiteSpace( Industries ) )
						{
							var xDoc = XDocument.Parse( GetRowPossibleColumn( dr, "Industries" ) );
							foreach ( var child in xDoc.Root.Elements() )
							{
								var framework = new IndexReferenceFramework
								{
									CategoryId = int.Parse( child.Attribute( "CategoryId" ).Value ),
									ReferenceFrameworkId = int.Parse( child.Attribute( "ReferenceFrameworkId" ).Value ),
									Name = ( string ) child.Attribute( "Name" ) ?? "",
									CodeGroup = ( string ) child.Attribute( "CodeGroup" ) ?? "",
									SchemaName = ( string ) child.Attribute( "SchemaName" ) ?? "",
									CodedNotation = ( string ) child.Attribute( "CodedNotation" ) ?? ""
								};

								if ( framework.CategoryId == 10 )
									index.Industries.Add( framework );
							}
						}
						#endregion

						#region AgentRelationships
						//string agentRelations = GetRowPossibleColumn( dr, "AgentRelationships" );
						//if ( !string.IsNullOrWhiteSpace( agentRelations ) )
						//{
						//	var xDoc = new XDocument();
						//	xDoc = XDocument.Parse( agentRelations );
						//	foreach ( var child in xDoc.Root.Elements() )
						//		index.AgentRelationships.Add( int.Parse( ( string ) child.Attribute( "RelationshipTypeId" ) ) );
						//}
						#endregion

						#region AgentRelationshipsForEntity
						//TODO replacing: QualityAssurance with AgentRelationshipsForEntity
						HandleAgentRelationshipsForEntity( dr, index );

					
						//}
						#endregion

						#region QualityAssurancePerformed
						string qAPerformed = GetRowPossibleColumn( dr, "QualityAssurancePerformedCSV" );
						if ( !string.IsNullOrWhiteSpace( qAPerformed ) )
						{
							if ( ContainsUnicodeCharacter( qAPerformed ) )
							{
								qAPerformed = Regex.Replace( qAPerformed, @"[^\u0000-\u007F]+", string.Empty );
							}
							qAPerformed = qAPerformed.Replace( "&", " " );
							var xDoc = XDocument.Parse( qAPerformed );

							foreach ( var child in xDoc.Root.Elements() )
							{
								string targetName = ( string ) child.Attribute( "TargetEntityName" ) ?? "";
								string entityStatId = ( string ) child.Attribute( "TargetEntityStateId" ) ?? "";
								var entityStateId = 0;
								int.TryParse( entityStatId, out entityStateId );
								if ( entityStateId > 1 )
								{
									var assertions = new List<int>();
									foreach(var s in child.Attribute( "Assertions" ).Value.Split( new char[] { ',' } ) )
									{
										assertions.Add( int.Parse( s.Trim() ) );
									}
									index.QualityAssurancePerformed.Add( new QualityAssurancePerformed
									{
										TargetEntityBaseId = int.Parse( child.Attribute( "TargetEntityBaseId" ).Value ),
										TargetEntityTypeId = int.Parse( child.Attribute( "TargetEntityTypeId" ).Value ),
										AssertionTypeIds = assertions,
										TargetEntityName = ( string ) child.Attribute( "TargetEntityName" ) ?? "",
										TargetEntityStateId = entityStateId,
									} );
								}
								if( index.QualityAssurancePerformed.Count() > 0 )
								{
									index.HasQualityAssurancePerformed = true;
									if ( index.QualityAssurancePerformed.Where ( s => s.TargetEntityTypeId == 1).Count() > 0)
										index.HasCredentialsQAPerformed = true;
									if ( index.QualityAssurancePerformed.Where( s => s.TargetEntityTypeId == 2 ).Count() > 0 )
										index.HasOrganizationsQAPerformed = true;
									if ( index.QualityAssurancePerformed.Where( s => s.TargetEntityTypeId == 3 ).Count() > 0 )
										index.HasAssessmentsQAPerformed = true;
									if ( index.QualityAssurancePerformed.Where( s => s.TargetEntityTypeId == 7 ).Count() > 0 )
										index.HasLoppsQAPerformed = true;
								}
							}
							index.HasQualityAssurancePerformed = index.QualityAssurancePerformed.Count() > 0 ? true : false;
						}
						#endregion

						#region Custom Reports
						int propertyId = 0;

						//TODO - could replace this approach and just add the schema text the text index
						if ( index.VerificationProfilesCount > 0 )
						{
							index.TextValues.Add( "orgReport:HasVerificationService" );
							if ( GetPropertyId( 59, "orgReport:HasVerificationService", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						}
						else
						{
							index.TextValues.Add( "orgReport:HasNoVerificationService" );
							if ( GetPropertyId( 59, "orgReport:HasNoVerificationService", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						}
						if ( index.CostManifestsCount > 0 )
						{
							if ( GetPropertyId( 59, "orgReport:HasCostManifest", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						}
						else
						{
							if ( GetPropertyId( 59, "orgReport:HasNoCostManifests", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						}
						if ( index.ConditionManifestsCount > 0 )
						{
							if ( GetPropertyId( 59, "orgReport:HasConditionManifest", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						}
						else
						{
							if ( GetPropertyId( 59, "orgReport:HasNoConditionManifests", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						}
						if ( index.SubsidiariesCount > 0 )
						{
							if ( GetPropertyId( 59, "orgReport:HasSubsidiary", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						}
						if ( index.DepartmentsCount > 0 )
						{
							if ( GetPropertyId( 59, "orgReport:HasDepartment", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						}
						if ( index.HasIndustriesCount > 0 )
							if ( GetPropertyId( 59, "orgReport:HasIndustries", ref propertyId ) )
								index.ReportFilters.Add( propertyId );

						if ( index.Addresses.Count > 0 )
							if ( GetPropertyId( 60, "orgReport:HasAddresses", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						#endregion

						list.Add( index );
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.DoTrace( 2, string.Format( "Organization_SearchForElastic. Last Row: {0}, OrgId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
				}
				finally
				{
					LoggingHelper.DoTrace( 2, string.Format( "Organization_SearchForElastic - Complete loaded {0} records", cntr ) );
				}


				return list;
			}
		}

		//public static bool GetPropertyId( int itemCount, int categoryId, string reportSchemaName, ref int propertyId )
		//{
		//    if (itemCount > 0
		//        && GetPropertyId( categoryId, reportSchemaName, ref propertyId ))
		//        return true;
		//    else
		//        return false;

		//}

		public static List<CM.OrganizationSummary> Organization_MapFromElastic( List<OrganizationIndex> organizations )
		{
			var list = new List<CM.OrganizationSummary>();

			var currencies = CodesManager.GetCurrencies();
			//have to be changed
			var costTypes = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_CREDENTIAL_ATTAINMENT_COST );


			foreach ( var oi in organizations )
			{
				var index = new CM.OrganizationSummary
				{
					Id = oi.Id,
					Name = oi.Name,
					FriendlyName = oi.FriendlyName,
					SubjectWebpage = oi.SubjectWebpage,
					RowId = oi.RowId,
					Description = oi.Description,
					CTID = oi.CTID,
					CredentialRegistryId = oi.CredentialRegistryId,
					EntityStateId = oi.EntityStateId,
					//DateEffective = ci.DateEffective,
					Created = oi.Created,
					LastUpdated = oi.LastUpdated,
				};

				if (index.EntityStateId == 2 )
				{
					index.Name += " [reference]";
				}
				if ( oi.ImageURL != null && oi.ImageURL.Trim().Length > 0 )
					index.ImageUrl = oi.ImageURL;
				else
					index.ImageUrl = null;

				if ( IsValidDate( oi.Created ) )
					index.Created = oi.Created;

				if ( IsValidDate( oi.LastUpdated ) )
					index.LastUpdated = oi.LastUpdated;

				index.IsACredentialingOrg = oi.IsACredentialingOrg;

				//addressess                

				index.Addresses = oi.Addresses.Select( x => new CM.Address
				{
					Latitude = x.Latitude,
					Longitude = x.Longitude,
					Address1 = x.Address1,
					Address2 = x.Address2,
					City = x.City,
					AddressRegion = x.AddressRegion,
					PostalCode = x.PostalCode,
					Country = x.Country
				} ).ToList();

				//these should be derived from the codes property
				index.AgentType = EntityPropertyManager.FillEnumeration( oi.RowId, CodesManager.PROPERTY_CATEGORY_ORGANIZATION_TYPE );
				index.OrganizationSectorType = EntityPropertyManager.FillEnumeration( oi.RowId, CodesManager.PROPERTY_CATEGORY_ORGANIZATION_SECTORTYPE );
				//index.OrganizationClaimType = EntityPropertyManager.FillEnumeration( oi.RowId, CodesManager.PROPERTY_CATEGORY_CLAIM_TYPE );
				index.ServiceType = EntityPropertyManager.FillEnumeration( index.RowId, CodesManager.PROPERTY_CATEGORY_ORG_SERVICE );

				if ( oi.Industries != null && oi.Industries.Count > 0 )
				{
					index.IndustryResults = Fill_CodeItemResults( oi.Industries.Where( x => x.CategoryId == 10 ).ToList(), CodesManager.PROPERTY_CATEGORY_NAICS, false, false );
				}
				index.OwnedByResults = Fill_CodeItemResults( oi.OwnedByResults, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false );

				index.OfferedByResults = Fill_CodeItemResults( oi.OfferedByResults, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false );

				index.AsmtsOwnedByResults = Fill_CodeItemResults( oi.AsmtsOwnedByResults, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false );

				index.LoppsOwnedByResults = Fill_CodeItemResults( oi.LoppsOwnedByResults, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false );
				index.FrameworksOwnedByResults = Fill_CodeItemResults( oi.FrameworksOwnedByResults, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false );

				index.AccreditedByResults = Fill_CodeItemResults( oi.AccreditedByResults, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false );

				index.ApprovedByResults = Fill_CodeItemResults( oi.ApprovedByResults, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false );

				index.RecognizedByResults = Fill_CodeItemResults( oi.RecognizedByResults, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false );

				index.RegulatedByResults = Fill_CodeItemResults( oi.RegulatedByResults, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false );

				index.QualityAssurance = Fill_AgentRelationship( oi.AgentRelationshipsForEntity, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, "Organization" );

				index.QualityAssurancePerformed = Fill_TargetQaAssertion( oi.QualityAssurancePerformed, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, "" );
				var results = oi.QualityAssurancePerformed.GroupBy( a => new
				{
					a.TargetEntityTypeId,
					a.TargetEntityBaseId,
					a.AssertionTypeIds
				} )
					.Select( g => new QualityAssurancePerformed
					{
						TargetEntityTypeId = g.Key.TargetEntityTypeId,
						TargetEntityBaseId = g.Key.TargetEntityBaseId,
						AssertionTypeIds = g.Key.AssertionTypeIds
					} )
					.OrderBy( a => a.TargetEntityTypeId ).ThenBy( s => s.TargetEntityBaseId ).ThenBy( s => s.AssertionTypeIds )
					.ToList();

				//results = results.OrderBy(s => s.TargetEntityTypeId).ThenBy(s => s.TargetEntityBaseId).ThenBy(s => s.AssertionTypeId).Distinct().ToList();

				index.QualityAssuranceCombinedTotal = results.Count();

				list.Add( index );
			}

			return list;
		}
		#endregion

		#region Assessment Elastic Index 
		public static List<AssessmentIndex> Assessment_SearchForElastic( string filter )
		{
			string connectionString = DBConnectionRO();
			var index = new AssessmentIndex();
			var list = new List<AssessmentIndex>();
			var result = new DataTable();
			LoggingHelper.DoTrace( 2, "GetAllForElastic - Starting. filter\r\n " + filter );
			int cntr = 0;
			using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				c.Open();
				using ( SqlCommand command = new SqlCommand( "[Assessment.ElasticSearch]", c ) )
				{
					command.CommandType = CommandType.StoredProcedure;
					command.Parameters.Add( new SqlParameter( "@Filter", filter ) );
					command.Parameters.Add( new SqlParameter( "@SortOrder", "alpha" ) );
					command.Parameters.Add( new SqlParameter( "@StartPageIndex", "0" ) );
					command.Parameters.Add( new SqlParameter( "@PageSize", "0" ) );
					command.CommandTimeout = 300;

					SqlParameter totalRows = new SqlParameter( "@TotalRows", 0 );
					totalRows.Direction = ParameterDirection.Output;
					command.Parameters.Add( totalRows );

					try
					{
						using ( SqlDataAdapter adapter = new SqlDataAdapter() )
						{
							adapter.SelectCommand = command;
							adapter.Fill( result );
						}
						string rows = command.Parameters[ command.Parameters.Count - 1 ].Value.ToString();
					}
					catch ( Exception ex )
					{
						index = new AssessmentIndex();
						index.Name = "EXCEPTION ENCOUNTERED";
						index.Description = ex.Message;
						//index.CredentialTypeSchema = "error";
						list.Add( index );
						return list;
					}
				}
				int costProfilesCount = 0;
				int conditionProfilesCount = 0;
				string assessesCompetencies = "";
				try
				{

					foreach ( DataRow dr in result.Rows )
					{
						cntr++;
						if ( cntr % 100 == 0 )
							LoggingHelper.DoTrace( 2, string.Format( " loading record: {0}", cntr ) );

						index = new AssessmentIndex();
						index.Id = GetRowColumn( dr, "Id", 0 );
						index.NameIndex = cntr * 1000;
						if ( index.Id == 415 )
						{
							//415,289,406,280 - had reference error in connections
						}
						index.Name = dr[ "Name" ].ToString();
						index.EntityStateId = GetRowPossibleColumn( dr, "EntityStateId", 0 );
						index.FriendlyName = FormatFriendlyTitle( index.Name );
						index.Description = dr[ "Description" ].ToString();
						string rowId = dr[ "RowId" ].ToString();
						index.RowId = new Guid( rowId );

						index.SubjectWebpage = GetRowColumn( dr, "SubjectWebpage", "" );

						index.CodedNotation = GetRowColumn( dr, "IdentificationCode", "" );
						index.CTID = GetRowPossibleColumn( dr, "CTID", "" );
						index.CredentialRegistryId = GetRowPossibleColumn( dr, "CredentialRegistryId", "" );

						index.OwnerOrganizationName = GetRowPossibleColumn( dr, "Organization", "" );
						index.OwnerOrganizationId = GetRowPossibleColumn( dr, "OrgId", 0 );

						if ( index.OwnerOrganizationName.Length > 0 )
							index.ListTitle = index.Name + " (" + index.OwnerOrganizationName + ")";
						else
							index.ListTitle = index.Name;
						//add helpers
						index.PrimaryOrganizationCTID = dr[ "OwningOrganizationCtid" ].ToString();

						var date = GetRowColumn( dr, "DateEffective", "" );
						if ( IsValidDate( date ) )
							index.DateEffective = ( DateTime.Parse( date ).ToShortDateString() );
						else
							index.DateEffective = "";
						date = GetRowColumn( dr, "Created", "" );
						if ( IsValidDate( date ) )
							index.Created = DateTime.Parse( date );
						date = GetRowColumn( dr, "LastUpdated", "" );
						if ( IsValidDate( date ) )
							index.LastUpdated = DateTime.Parse( date );
						//define LastUpdated to be EntityLastUpdated
						date = GetRowColumn( dr, "EntityLastUpdated", "" );
						if ( IsValidDate( date ) )
							index.LastUpdated = DateTime.Parse( date );

						//don't thinks this is necessary!
						//index.QARolesCount = GetRowColumn( dr, "QARolesCount", 0 );

						index.RequiresCount = GetRowColumn( dr, "RequiresCount", 0 );
						index.RecommendsCount = GetRowColumn( dr, "RecommendsCount", 0 );
						index.IsRequiredForCount = GetRowColumn( dr, "IsRequiredForCount", 0 );
						index.IsRecommendedForCount = GetRowColumn( dr, "IsRecommendedForCount", 0 );
						index.IsAdvancedStandingForCount = GetRowColumn( dr, "IsAdvancedStandingForCount", 0 );
						index.AdvancedStandingFromCount = GetRowColumn( dr, "AdvancedStandingFromCount", 0 );
						index.IsPreparationForCount = GetRowColumn( dr, "IsPreparationForCount", 0 );
						index.PreparationFromCount = GetRowColumn( dr, "PreparationFromCount", 0 );
						//index.TotalCostCount = GetRowPossibleColumn( dr, "TotalCostCount", 0M );
						costProfilesCount = GetRowPossibleColumn( dr, "CostProfilesCount", 0 );
						conditionProfilesCount = GetRowPossibleColumn( dr, "conditionProfilesCount", 0 );
						//
						index.CommonConditionsCount = GetRowPossibleColumn( dr, "CommonConditionsCount", 0 );
						index.CommonCostsCount = GetRowPossibleColumn( dr, "CommonCostsCount", 0 );
						index.FinancialAidCount = GetRowPossibleColumn( dr, "FinancialAidCount", 0 );
						index.ProcessProfilesCount = GetRowPossibleColumn( dr, "ProcessProfilesCount", 0 );
						//index.HasInstructionalPrograms = GetRowPossibleColumn( dr, "HasCIPCount", 0 );
						//-actual connection type (no credential info)
						index.ConnectionsList = dr[ "ConnectionsList" ].ToString();
						//connection type, plus Id, and name of credential
						index.CredentialsList = dr[ "CredentialsList" ].ToString();
						//change to use Connections
						try
						{
							string credentialConnections = GetRowPossibleColumn( dr, "AssessmentConnections" );
							if ( !string.IsNullOrWhiteSpace( credentialConnections ) )
							{
								Connection conn = new Connection();
								var xDoc = XDocument.Parse( credentialConnections );
								foreach ( var child in xDoc.Root.Elements() )
								{
									conn = new Connection();
									conn.ConnectionType = ( string ) child.Attribute( "ConnectionType" ) ?? "";
									conn.ConnectionTypeId = int.Parse( child.Attribute( "ConnectionTypeId" ).Value );

									//do something with counts for this type

									conn.CredentialId = int.Parse( child.Attribute( "CredentialId" ).Value );
									if ( conn.CredentialId > 0 )
									{
										//add credential
										conn.Credential = ( string ) child.Attribute( "CredentialName" ) ?? "";
										//??????
										conn.CredentialOrgId = int.Parse( child.Attribute( "credOrgid" ).Value );
										conn.CredentialOrganization = ( string ) child.Attribute( "credOrganization" ) ?? "";
									}
									conn.AssessmentId = int.Parse( child.Attribute( "AssessmentId" ).Value );
									if ( conn.AssessmentId > 0 )
									{
										conn.Assessment = ( string ) child.Attribute( "AssessmentName" ) ?? "";
										conn.AssessmentOrganizationId = int.Parse( child.Attribute( "asmtOrgid" ).Value );
										conn.AssessmentOrganization = ( string ) child.Attribute( "asmtOrganization" ) ?? "";
									}
									conn.LoppId = int.Parse( child.Attribute( "LearningOpportunityId" ).Value );
									if ( conn.LoppId > 0 )
									{
										conn.LearningOpportunity = ( string ) child.Attribute( "LearningOpportunityName" ) ?? "";
										conn.LoppOrganizationId = int.Parse( child.Attribute( "loppOrgid" ).Value );
										conn.LearningOpportunityOrganization = ( string ) child.Attribute( "loppOrganization" ) ?? "";
									}

									index.Connections.Add( conn );
								}
							}
						}
						catch ( Exception ex )
						{
							LoggingHelper.DoTrace( 2, string.Format( " # {0}. Exception on Assessment Connections id: {1}; \r\n{2}", cntr, index.Id, ex.Message ) );
						}

						#region AssessesCompetencies

						assessesCompetencies = dr[ "AssessesCompetencies" ].ToString();
						if ( !string.IsNullOrWhiteSpace( assessesCompetencies ) )
						{
							try
							{
								if ( ContainsUnicodeCharacter( assessesCompetencies ) )
								{
									assessesCompetencies = Regex.Replace( assessesCompetencies, @"[^\u0000-\u007F]+", string.Empty );

								}
								assessesCompetencies = assessesCompetencies.Replace( "&", " " );
								var xDoc = new XDocument();
								xDoc = XDocument.Parse( assessesCompetencies );
								foreach ( var child in xDoc.Root.Elements() )
								{
									var competency = new IndexCompetency
									{
										Name = ( string ) child.Attribute( "Name" ) ?? "",
										Description = ( string ) child.Attribute( "Description" ) ?? ""
									};

									index.AssessesCompetencies.Add( competency );
								}
							}
							catch ( Exception ex )
							{
								LoggingHelper.DoTrace( 2, string.Format( " # {0}. Exception on Assessment AssessesCompetencies id: {1}; \r\n{2}", cntr, index.Id, ex.Message ) );
							}
						}

						#endregion

						#region RequiresCompetencies

						string requiresCompetencies = dr[ "RequiresCompetencies" ].ToString();
						if ( !string.IsNullOrWhiteSpace( requiresCompetencies ) )
						{
							if ( ContainsUnicodeCharacter( requiresCompetencies ) )
							{
								requiresCompetencies = Regex.Replace( requiresCompetencies, @"[^\u0000-\u007F]+", string.Empty );
							}
							requiresCompetencies = requiresCompetencies.Replace( "&", " " );
							try
							{
								var xDoc = new XDocument();
								xDoc = XDocument.Parse( requiresCompetencies );
								foreach ( var child in xDoc.Root.Elements() )
								{
									var competency = new IndexCompetency
									{
										Name = ( string ) child.Attribute( "Name" ) ?? "",
										Description = ( string ) child.Attribute( "Description" ) ?? ""
									};

									index.RequiresCompetencies.Add( competency );
								}
							}
							catch ( Exception ex )
							{
								LoggingHelper.DoTrace( 2, string.Format( " # {0}. Exception on Assessment RequiresCompetencies id: {1}; \r\n{2}", cntr, index.Id, ex.Message ) );
							}
						}

						#endregion

						#region TextValues

						var textValues = dr[ "TextValues" ].ToString();
						if ( !string.IsNullOrWhiteSpace( textValues ) )
						{
							textValues = textValues.Replace( "&", " " );
							var xDoc = new XDocument();
							xDoc = XDocument.Parse( textValues );
							//index.Addresses = xDoc.Root.Elements().Count();
							foreach ( var child in xDoc.Root.Elements() )
							{
								var categoryId = int.Parse( child.Attribute( "CategoryId" ).Value );
								//index.TextValues.Add( ( string )child.Attribute( "TextValue" ) ?? "" );
								var textValue = child.Attribute( "TextValue" );
								if ( textValue != null && !string.IsNullOrWhiteSpace( textValue.Value ) )
								{
									index.TextValues.Add( textValue.Value );

									if ( textValue.Value.IndexOf( "-" ) > -1 )
										index.TextValues.Add( textValue.Value.Replace( "-", "" ) );

									if ( categoryId == 35 )
										index.Keyword.Add( textValue.Value );
								}
								//source is just direct/indirect, more want the sourceEntityType
								var codeNotation = child.Attribute( "CodedNotation" );
								if ( codeNotation != null && !string.IsNullOrWhiteSpace( codeNotation.Value ) )
								{
									index.TextValues.Add( codeNotation.Value );
									if ( codeNotation.Value.IndexOf( "-" ) > -1 )
										index.TextValues.Add( codeNotation.Value.Replace( "-", "" ) );
								}
							}
						}
						//properties to add to textvalues
						index.AvailableOnlineAt = GetRowPossibleColumn( dr, "AvailableOnlineAt", "" );
						if ( !string.IsNullOrWhiteSpace( index.AvailableOnlineAt ) )
							index.TextValues.Add( index.AvailableOnlineAt );
						index.AvailabilityListing = GetRowPossibleColumn( dr, "AvailabilityListing", "" );
						if ( !string.IsNullOrWhiteSpace( index.AvailabilityListing ) )
							index.TextValues.Add( index.AvailabilityListing );
						string url = GetRowPossibleColumn( dr, "AssessmentExampleUrl", "" );
						if ( !string.IsNullOrWhiteSpace( url ) )
							index.TextValues.Add( url );
						url = GetRowPossibleColumn( dr, "ProcessStandards", "" );
						if ( !string.IsNullOrWhiteSpace( url ) )
							index.TextValues.Add( url );
						url = GetRowPossibleColumn( dr, "ScoringMethodExample", "" );
						if ( !string.IsNullOrWhiteSpace( url ) )
							index.TextValues.Add( url );
						url = GetRowPossibleColumn( dr, "ExternalResearch", "" );
						if ( !string.IsNullOrWhiteSpace( url ) )
							index.TextValues.Add( url );
						//	,base.
						if ( !string.IsNullOrWhiteSpace( index.CredentialRegistryId ) )
							index.TextValues.Add( index.CredentialRegistryId );
						index.TextValues.Add( index.Id.ToString() );
						index.TextValues.Add( index.CTID );
						if ( !string.IsNullOrWhiteSpace( index.CodedNotation ) )
							index.TextValues.Add( index.CodedNotation );
						#endregion

						#region SubjectAreas

						var subjectAreas = dr[ "SubjectAreas" ].ToString();
						if ( !string.IsNullOrWhiteSpace( subjectAreas ) )
						{
							var xDoc = XDocument.Parse( subjectAreas );
							foreach ( var child in xDoc.Root.Elements() )
								index.SubjectAreas.Add( child.Attribute( "Subject" ).Value );

							if ( index.SubjectAreas.Count() > 0 )
								index.HasSubjects = true;
						}
						
						#endregion


						#region AssessmentProperties
						try
						{
							var assessmentProperties = dr[ "AssessmentProperties" ].ToString();
							if ( !string.IsNullOrEmpty( assessmentProperties ) )
							{
								assessmentProperties = assessmentProperties.Replace( "&", " " );
								var xDoc = XDocument.Parse( assessmentProperties );
								foreach ( var child in xDoc.Root.Elements() )
								{
									var categoryId = int.Parse( child.Attribute( "CategoryId" ).Value );
									var propertyValueId = int.Parse( child.Attribute( "PropertyValueId" ).Value );
									var property = child.Attribute( "Property" ).Value;
									var schemaName = ( string )child.Attribute( "PropertySchemaName" );

									index.AssessmentProperties.Add( new IndexProperty
									{
										CategoryId = categoryId,
										Id = propertyValueId,
										Name = property,
										SchemaName = schemaName
									} );
									if ( categoryId == ( int ) CodesManager.PROPERTY_CATEGORY_Assessment_Method_Type )
										index.AssessmentMethodTypeIds.Add( propertyValueId );
									if ( categoryId == ( int ) CodesManager.PROPERTY_CATEGORY_ASSESSMENT_USE_TYPE )
										index.AssessmentUseTypeIds.Add( propertyValueId );
									if ( categoryId == ( int ) CodesManager.PROPERTY_CATEGORY_Scoring_Method )
										index.ScoringMethodTypeIds.Add( propertyValueId );
									if ( categoryId == ( int ) CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE )
										index.DeliveryMethodTypeIds.Add( propertyValueId );
									if ( categoryId == ( int ) CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE )
										index.AudienceTypeIds.Add( propertyValueId );
									else if ( categoryId == ( int )CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL )
										index.AudienceLevelTypeIds.Add( propertyValueId );

									if ( !string.IsNullOrWhiteSpace( ( string )child.Attribute( "PropertySchemaName" ) ) )
										index.TextValues.Add( ( string )child.Attribute( "PropertySchemaName" ) );
								}
							}
						}
						catch ( Exception ex )
						{
							LoggingHelper.DoTrace( 2, string.Format( " # {0}. Exception on AssessmentProperties id: {1}; \r\n{2}", cntr, index.Id, ex.Message ) );
						}
						#endregion

						#region Reference Frameworks - industries, occupations and classifications
						string frameworks = dr[ "Frameworks" ].ToString();
						if ( !string.IsNullOrWhiteSpace( frameworks ) )
						{
							var xDoc = new XDocument();
							xDoc = XDocument.Parse( frameworks );
							foreach ( var child in xDoc.Root.Elements() )
							{
								var framework = new IndexReferenceFramework
								{
									CategoryId = int.Parse( child.Attribute( "CategoryId" ).Value ),
									ReferenceFrameworkId = int.Parse( child.Attribute( "ReferenceFrameworkId" ).Value ),
									Name = ( string ) child.Attribute( "Name" ) ?? "",
									CodeGroup = ( string ) child.Attribute( "CodeGroup" ) ?? "",
									SchemaName = ( string ) child.Attribute( "SchemaName" ) ?? "",
									CodedNotation = ( string ) child.Attribute( "CodedNotation" ) ?? "",
								};

								if ( framework.CategoryId == 11 )
									index.Occupations.Add( framework );
								if ( framework.CategoryId == 10 )
									index.Industries.Add( framework );
								if ( framework.CategoryId == 23 )
									index.Classifications.Add( framework );
							}
							if ( index.Occupations.Count > 0 )
								index.HasOccupations = true;
							if ( index.Industries.Count > 0 )
								index.HasIndustries = true;
							if ( index.Classifications.Count > 0 )
								index.HasInstructionalPrograms = true;
						}
				
						#endregion

						#region QualityAssurance
						index.Org_QAAgentAndRoles = GetRowPossibleColumn( dr, "Org_QAAgentAndRoles" );

						HandleAgentRelationshipsForEntity( dr, index );

					   #endregion

						#region Addresses
						var addresses = dr[ "Addresses" ].ToString();
						if ( !string.IsNullOrWhiteSpace( addresses ) )
						{
							//index.Addresses = addresses;
							var xDoc = new XDocument();
							xDoc = XDocument.Parse( addresses );
							//index.AvailableAddresses = xDoc.Root.Elements().Count();
							foreach ( var child in xDoc.Root.Elements() )
							{
								string region = ( string ) child.Attribute( "Region" ) ?? "";
								string city = ( string ) child.Attribute( "City" ) ?? "";
								index.Addresses.Add( new Address
								{
									Latitude = double.Parse( ( string ) child.Attribute( "Latitude" ) ),
									Longitude = double.Parse( ( string ) child.Attribute( "Longitude" ) ),
									Address1 = ( string ) child.Attribute( "Address1" ) ?? "",
									Address2 = ( string ) child.Attribute( "Address2" ) ?? "",
									City = ( string ) child.Attribute( "City" ) ?? "",
									AddressRegion = ( string ) child.Attribute( "Region" ) ?? "",
									PostalCode = ( string ) child.Attribute( "PostalCode" ) ?? "",
									Country = ( string ) child.Attribute( "Country" ) ?? ""
								} );
								AddTextValue( index, city );
								AddTextValue( index, region );
							}
						}
						if ( index.Addresses.Count == 0 )
						{
							//prototype: if no cred addresses, and one org address, then add to index (not detail page)
							var orgAddresses = GetRowPossibleColumn( dr, "OrgAddresses" );
							if ( !string.IsNullOrWhiteSpace( orgAddresses ) )
							{
								try
								{
									var xDoc = new XDocument();
									xDoc = XDocument.Parse( orgAddresses );
									//actually will only focus on regions
									string prevRegion = "";
									string prevCountry = "";
									//if ( xDoc.Root.Elements().Count() == 1)
									//{
									foreach ( var child in xDoc.Root.Elements() )
									{
										string region = ( string ) child.Attribute( "Region" ) ?? "";
										string city = ( string ) child.Attribute( "City" ) ?? "";
										if ( prevRegion != region )
										{
											index.Addresses.Add( new Address
											{
												Latitude = double.Parse( ( string ) child.Attribute( "Latitude" ) ),
												Longitude = double.Parse( ( string ) child.Attribute( "Longitude" ) ),
												Address1 = ( string ) child.Attribute( "Address1" ) ?? "",
												Address2 = ( string ) child.Attribute( "Address2" ) ?? "",
												//City = ( string )child.Attribute( "City" ) ?? "",
												AddressRegion = ( string ) child.Attribute( "Region" ) ?? "",
												//PostalCode = ( string )child.Attribute( "PostalCode" ) ?? "",
												Country = ( string ) child.Attribute( "Country" ) ?? ""
											} );
											prevRegion = region;
											AddTextValue( index, city );
											AddTextValue( index, region );
										}
										//should only be one, just in case some change in future
										//break;
									}
									//}
								}
								catch ( Exception ex )
								{
									LoggingHelper.DoTrace( 2, string.Format( " # {0}. Exception on OrgAddresses id: {1}; \r\n{2}", cntr, index.Id, ex.Message ) );
								}
							}
						}
						#endregion

						#region language
						index.InLanguage = GetLanguages( dr );
						#endregion

						#region custom reports

						int propertyId = 0;
						if ( !string.IsNullOrWhiteSpace( index.AvailableOnlineAt ) )
							if ( GetPropertyId( 60, "asmtReport:AvailableOnline", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( index.AssessesCompetencies.Count > 0 )
							if ( GetPropertyId( 60, "asmtReport:AssessesCompetencies", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( index.RequiresCompetencies.Count > 0 )
							if ( GetPropertyId( 60, "asmtReport:RequiresCompetencies", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( costProfilesCount > 0 )
							if ( GetPropertyId( 60, "asmtReport:HasCostProfile", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( conditionProfilesCount > 0 )
							if ( GetPropertyId( 60, "asmtReport:HasConditionProfile", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						//
						var DurationProfileCount = GetRowPossibleColumn( dr, "HasDurationCount", 0 );
						if ( DurationProfileCount > 0 )
						{
							if ( GetPropertyId( 60, "asmtReport:HasDurationProfile", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						}

						if ( index.CommonConditionsCount > 0 )
							if ( GetPropertyId( 60, "asmtReport:ReferencesCommonConditions", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( index.CommonCostsCount > 0 )
							if ( GetPropertyId( 60, "asmtReport:ReferencesCommonCosts", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( index.FinancialAidCount > 0 )
							if ( GetPropertyId( 60, "asmtReport:FinancialAid", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( index.ProcessProfilesCount > 0 )
							if ( GetPropertyId( 60, "asmtReport:HasProcessProfile", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( index.HasOccupations )
							if ( GetPropertyId( 60, "asmtReport:HasOccupations", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( index.HasIndustries )
							if ( GetPropertyId( 60, "asmtReport:HasIndustries", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( index.HasInstructionalPrograms )
							if ( GetPropertyId( 60, "asmtReport:HasCIP", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( index.Addresses.Count > 0 )
							if ( GetPropertyId( 60, "asmtReport:HasAddresses", ref propertyId ) )
								index.ReportFilters.Add( propertyId );

						list.Add( index );
					}
					#endregion

				}
				catch ( Exception ex )
				{
					LoggingHelper.DoTrace( 2, string.Format( "Assessment_SearchForElastic. Last Row: {0}, asmtId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
				}
				finally
				{
					LoggingHelper.DoTrace( 2, string.Format( "Assessment_SearchForElastic - Complete loaded {0} records", cntr ) );
				}
				return list;
			}
		}
		private static List<int> GetIntegerList( DataRow dr, string propertyName )
		{
			string delimitedList = GetRowPossibleColumn( dr, propertyName );
			List<int> list = new List<int>();
			if ( !string.IsNullOrWhiteSpace( delimitedList ) )
			{
				foreach ( var index in delimitedList.Split( '|' ) )
				{
					list.Add( int.Parse( index ) );
				}
			}
			return list;
		}
		private static List<string> GetLanguages( DataRow dr )
		{
			string languages = GetRowPossibleColumn( dr, "Languages" );
			List<string> list = new List<string>();
			if ( !string.IsNullOrWhiteSpace( languages ) )
			{
				foreach ( var language in languages.Split( '|' ) )
				{
					if ( !string.IsNullOrWhiteSpace( language ) )
					{
						if ( language.IndexOf( "(" ) > 1 )
							list.Add( language.Substring( 0, language.IndexOf( "(" ) ).Trim() );
						else
							list.Add( language );
					}
				}
			}
			return list;
		}

		/// <summary>
		/// Look up property directly in code table
		/// OR - create a cache list of codes to save many lookups
		/// </summary>
		/// <param name="categoryId"></param>
		/// <param name="schemaName"></param>
		/// <param name="propertyId"></param>
		/// <returns></returns>
		public static bool GetPropertyId( int categoryId, string schemaName, ref int propertyId )
		{
			CodeItem index = CodesManager.GetEntityStatisticBySchema( categoryId, schemaName );
			if ( index != null && index.Id > 0 )
			{
				propertyId = index.Id;
				return true;
			}
			return false;
		}

		public static List<PM.AssessmentProfile> Assessment_MapFromElastic( List<AssessmentIndex> assessments )
		{
			var list = new List<PM.AssessmentProfile>();

			foreach ( var ai in assessments )
			{
				var index = new PM.AssessmentProfile
				{
					Id = ai.Id,
					Name = ai.Name,
					FriendlyName = ai.FriendlyName,
					Description = ai.Description,
					EntityStateId = ai.EntityStateId,
					RowId = ai.RowId,
					SubjectWebpage = ai.SubjectWebpage,
					AvailableOnlineAt = ai.AvailableOnlineAt,
					CodedNotation = ai.IdentificationCode,
					CTID = ai.CTID,
					PrimaryOrganizationCTID = ai.PrimaryOrganizationCTID,
					CredentialRegistryId = ai.CredentialRegistryId,
					DateEffective = ai.DateEffective,
					Created = ai.Created,
					//define LastUpdated to be EntityLastUpdated
					LastUpdated = ai.LastUpdated,
					RequiresCount = ai.RequiresCount,
					RecommendsCount = ai.RecommendsCount,
					RequiredForCount = ai.IsRequiredForCount,
					IsRecommendedForCount = ai.IsRecommendedForCount,
					IsAdvancedStandingForCount = ai.IsAdvancedStandingForCount,
					AdvancedStandingFromCount = ai.AdvancedStandingFromCount,
					PreparationForCount = ai.IsPreparationForCount,
					PreparationFromCount = ai.PreparationFromCount,
					CompetenciesCount = ai.AssessesCompetencies.Count,
					//TotalCostCount = ai.TotalCostCount,
					CommonCostsCount = ai.CommonCostsCount,
					CommonConditionsCount = ai.CommonConditionsCount,
					FinancialAidCount = ai.FinancialAidCount
				};

				if ( ai.OwnerOrganizationId > 0 )
					index.OwningOrganization = new CM.Organization() { Id = ai.OwnerOrganizationId, Name = ai.OwnerOrganizationName };
				//addresses
				index.Addresses = ai.Addresses.Select( x => new CM.Address
				{
					Latitude = x.Latitude,
					Longitude = x.Longitude,
					Address1 = x.Address1,
					Address2 = x.Address2,
					City = x.City,
					AddressRegion = x.AddressRegion,
					PostalCode = x.PostalCode,
					Country = x.Country
				} ).ToList();

				index.EstimatedDuration = DurationProfileManager.GetAll( index.RowId );
				index.QualityAssurance = Fill_AgentRelationship( ai.AgentRelationshipsForEntity, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, "Assessment" );
				index.Org_QAAgentAndRoles = Fill_AgentRelationship( ai.Org_QAAgentAndRoles, 130, false, false, true, "Organization" );

				index.AssessmentMethodTypes = Fill_CodeItemResults( ai.AssessmentProperties.Where( x => x.CategoryId == ( int ) CodesManager.PROPERTY_CATEGORY_Assessment_Method_Type ).ToList(), CodesManager.PROPERTY_CATEGORY_Assessment_Method_Type, false, false );
				index.AssessmentUseTypes = Fill_CodeItemResults( ai.AssessmentProperties.Where( x => x.CategoryId == ( int ) CodesManager.PROPERTY_CATEGORY_ASSESSMENT_USE_TYPE ).ToList(), CodesManager.PROPERTY_CATEGORY_ASSESSMENT_USE_TYPE, false, false );
				index.ScoringMethodTypes = Fill_CodeItemResults( ai.AssessmentProperties.Where( x => x.CategoryId == ( int ) CodesManager.PROPERTY_CATEGORY_Scoring_Method ).ToList(), CodesManager.PROPERTY_CATEGORY_Scoring_Method, false, false );
				index.DeliveryMethodTypes = Fill_CodeItemResults( ai.AssessmentProperties.Where( x => x.CategoryId == ( int ) CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE ).ToList(), CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE, false, false );
				index.AudienceTypes = Fill_CodeItemResults( ai.AssessmentProperties.Where( x => x.CategoryId == ( int ) CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE ).ToList(),CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE, false, false );

				if ( ai.Industries != null && ai.Industries.Count > 0 )
					index.IndustryResults = Fill_CodeItemResults( ai.Industries.Where( x => x.CategoryId == 10 ).ToList(), CodesManager.PROPERTY_CATEGORY_NAICS, false, false );

				if ( ai.Occupations != null && ai.Occupations.Count > 0 )
					index.OccupationResults = Fill_CodeItemResults( ai.Occupations.Where( x => x.CategoryId == 11 ).ToList(), CodesManager.PROPERTY_CATEGORY_SOC, false, false );

				if ( ai.Classifications != null && ai.Classifications.Count > 0 )
					index.InstructionalProgramClassification = Fill_CodeItemResults( ai.Classifications.Where( x => x.CategoryId == 23 ).ToList(), CodesManager.PROPERTY_CATEGORY_CIP, false, false );

				index.CredentialsList = Fill_CredentialConnectionsResult( ai.CredentialsList, CodesManager.PROPERTY_CATEGORY_CONNECTION_PROFILE_TYPE );
				index.ListTitle = ai.ListTitle;
				index.Subjects = ai.SubjectAreas;
				list.Add( index );
			}

			return list;
		}

		#endregion

		#region LearningOpp Elastic Index 

		public static List<LearningOppIndex> LearningOpp_SearchForElastic( string filter )
		{
			string connectionString = DBConnectionRO();
			var index = new LearningOppIndex();
			var list = new List<LearningOppIndex>();
			var result = new DataTable();
			LoggingHelper.DoTrace( 2, "LearningOpp_SearchForElastic - Starting. filter\r\n " + filter );
			int cntr = 0;
			using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				c.Open();
				using ( SqlCommand command = new SqlCommand( "[LearningOpportunity.ElasticSearch]", c ) )
				{
					command.CommandType = CommandType.StoredProcedure;
					command.Parameters.Add( new SqlParameter( "@Filter", filter ) );
					command.Parameters.Add( new SqlParameter( "@SortOrder", "alpha" ) );
					command.Parameters.Add( new SqlParameter( "@StartPageIndex", "0" ) );
					command.Parameters.Add( new SqlParameter( "@PageSize", "0" ) );
					command.CommandTimeout = 300;

					SqlParameter totalRows = new SqlParameter( "@TotalRows", 0 );
					totalRows.Direction = ParameterDirection.Output;
					command.Parameters.Add( totalRows );

					try
					{
						using ( SqlDataAdapter adapter = new SqlDataAdapter() )
						{
							adapter.SelectCommand = command;
							adapter.Fill( result );
						}
						string rows = command.Parameters[ command.Parameters.Count - 1 ].Value.ToString();
					}
					catch ( Exception ex )
					{
						index = new LearningOppIndex();
						index.Name = "EXCEPTION ENCOUNTERED";
						index.Description = ex.Message;
						//index.CredentialTypeSchema = "error";
						list.Add( index );
						return list;
					}
				}
				int costProfilesCount = 0;
				try
				{

					foreach ( DataRow dr in result.Rows )
					{
						cntr++;
						if ( cntr % 100 == 0 )
							LoggingHelper.DoTrace( 2, string.Format( " loading record: {0}", cntr ) );

						index = new LearningOppIndex();
						index.Id = GetRowColumn( dr, "Id", 0 );
						index.NameIndex = cntr * 1000;
						index.Name = GetRowColumn( dr, "Name", "missing" );
						index.FriendlyName = FormatFriendlyTitle( index.Name );
						index.Description = GetRowColumn( dr, "Description", "" );
						if ( index.Id == 209 )
						{

						}
						string rowId = GetRowColumn( dr, "RowId" );
						index.RowId = new Guid( rowId );
						index.EntityStateId = GetRowPossibleColumn( dr, "EntityStateId", 0 );
						index.SubjectWebpage = GetRowColumn( dr, "SubjectWebpage", "" );


						index.CTID = GetRowPossibleColumn( dr, "CTID", "" );
						index.CredentialRegistryId = GetRowPossibleColumn( dr, "CredentialRegistryId", "" );


						index.OwnerOrganizationName = GetRowPossibleColumn( dr, "Organization", "" );
						index.OwnerOrganizationId = GetRowPossibleColumn( dr, "OrgId", 0 );
						if ( index.OwnerOrganizationName.Length > 0 )
							index.ListTitle = index.Name + " (" + index.OwnerOrganizationName + ")";
						else
							index.ListTitle = index.Name;
						//add helpers
						index.PrimaryOrganizationCTID = dr[ "OwningOrganizationCtid" ].ToString();

						var date = GetRowColumn( dr, "DateEffective", "" );
						if ( IsValidDate( date ) )
							index.DateEffective = DateTime.Parse( date ).ToShortDateString();
						else
							index.DateEffective = "";

						index.Created = GetRowColumn( dr, "Created", System.DateTime.MinValue );
						index.LastUpdated = GetRowColumn( dr, "LastUpdated", System.DateTime.MinValue );
						//define LastUpdated to be EntityLastUpdated
						date = GetRowColumn( dr, "EntityLastUpdated", "" );
						if ( IsValidDate( date ) )
							index.LastUpdated = DateTime.Parse( date );

						//competencies. either arbitrarily get all, or if filters exist, only return matching ones
						index.CompetenciesCount = GetRowPossibleColumn( dr, "CompetenciesCount", 0 );

						//connections not condition profiles
						index.RequiresCount = GetRowColumn( dr, "RequiresCount", 0 );
						index.RecommendsCount = GetRowColumn( dr, "RecommendsCount", 0 );
						index.IsRequiredForCount = GetRowColumn( dr, "IsRequiredForCount", 0 );
						index.IsRecommendedForCount = GetRowColumn( dr, "IsRecommendedForCount", 0 );
						index.IsAdvancedStandingForCount = GetRowColumn( dr, "IsAdvancedStandingForCount", 0 );
						index.AdvancedStandingFromCount = GetRowColumn( dr, "AdvancedStandingFromCount", 0 );
						index.IsPreparationForCount = GetRowColumn( dr, "IsPreparationForCount", 0 );
						index.PreparationFromCount = GetRowColumn( dr, "PreparationFromCount", 0 );
						//index.TotalCostCount = GetRowPossibleColumn( dr, "TotalCostCount", 0M );
						costProfilesCount = GetRowPossibleColumn( dr, "CostProfilesCount", 0 );
						index.CommonConditionsCount = GetRowPossibleColumn( dr, "CommonConditionsCount", 0 );
						index.CommonCostsCount = GetRowPossibleColumn( dr, "CommonCostsCount", 0 );
						index.FinancialAidCount = GetRowPossibleColumn( dr, "FinancialAidCount", 0 );
						index.ProcessProfilesCount = GetRowPossibleColumn( dr, "ProcessProfilesCount", 0 );
						//-actual connection type (no credential info)
						index.ConnectionsList = dr[ "ConnectionsList" ].ToString();
						//connection type, plus Id, and name of credential
						index.CredentialsList = dr[ "CredentialsList" ].ToString();
						//change to use Connections
						string credentialConnections = GetRowPossibleColumn( dr, "LoppConnections" );
						if ( !string.IsNullOrWhiteSpace( credentialConnections ) )
						{
							Connection conn = new Connection();
							var xDoc = XDocument.Parse( credentialConnections );
							foreach ( var child in xDoc.Root.Elements() )
							{
								conn = new Connection();
								conn.ConnectionType = ( string ) child.Attribute( "ConnectionType" ) ?? "";
								conn.ConnectionTypeId = int.Parse( child.Attribute( "ConnectionTypeId" ).Value );

								//do something with counts for this type

								conn.CredentialId = int.Parse( child.Attribute( "CredentialId" ).Value );
								if ( conn.CredentialId > 0 )
								{
									//add credential
									conn.Credential = ( string ) child.Attribute( "CredentialName" ) ?? "";
									//??????
									conn.CredentialOrgId = int.Parse( child.Attribute( "credOrgid" ).Value );
									conn.CredentialOrganization = ( string ) child.Attribute( "credOrganization" ) ?? "";
								}
								conn.AssessmentId = int.Parse( child.Attribute( "AssessmentId" ).Value );
								if ( conn.AssessmentId > 0 )
								{
									conn.Assessment = ( string ) child.Attribute( "AssessmentName" ) ?? "";
									conn.AssessmentOrganizationId = int.Parse( child.Attribute( "asmtOrgid" ).Value );
									conn.AssessmentOrganization = ( string ) child.Attribute( "asmtOrganization" ) ?? "";
								}
								conn.LoppId = int.Parse( child.Attribute( "LearningOpportunityId" ).Value );
								if ( conn.LoppId > 0 )
								{
									conn.LearningOpportunity = ( string ) child.Attribute( "LearningOpportunityName" ) ?? "";
									conn.LoppOrganizationId = int.Parse( child.Attribute( "loppOrgid" ).Value );
									conn.LearningOpportunityOrganization = ( string ) child.Attribute( "loppOrganization" ) ?? "";
								}

								index.Connections.Add( conn );
							}
						}

						#region TeachesCompetencies

						string teachesCompetencies = dr[ "TeachesCompetencies" ].ToString();
						if ( !string.IsNullOrWhiteSpace( teachesCompetencies ) )
						{
							if ( ContainsUnicodeCharacter( teachesCompetencies ) )
							{
								teachesCompetencies = Regex.Replace( teachesCompetencies, @"[^\u0000-\u007F]+", string.Empty );
							}
							teachesCompetencies = teachesCompetencies.Replace( "&", " " );
							var xDoc = new XDocument();
							xDoc = XDocument.Parse( teachesCompetencies );
							foreach ( var child in xDoc.Root.Elements() )
							{
								var competency = new IndexCompetency
								{
									Name = ( string ) child.Attribute( "Name" ) ?? "",
									Description = ( string ) child.Attribute( "Description" ) ?? ""
								};

								index.TeachesCompetencies.Add( competency );
							}
						}

						#endregion

						#region RequiresCompetencies

						string requiresCompetencies = dr[ "RequiresCompetencies" ].ToString();
						if ( !string.IsNullOrWhiteSpace( requiresCompetencies ) )
						{
							if ( ContainsUnicodeCharacter( requiresCompetencies ) )
							{
								requiresCompetencies = Regex.Replace( requiresCompetencies, @"[^\u0000-\u007F]+", string.Empty );
							}
							requiresCompetencies = requiresCompetencies.Replace( "&", " " );
							var xDoc = new XDocument();
							xDoc = XDocument.Parse( requiresCompetencies );
							foreach ( var child in xDoc.Root.Elements() )
							{
								var competency = new IndexCompetency
								{
									Name = ( string ) child.Attribute( "Name" ) ?? "",
									Description = ( string ) child.Attribute( "Description" ) ?? ""
								};

								index.RequiresCompetencies.Add( competency );
							}
						}

						#endregion

						#region SubjectAreas

						var subjectAreas = dr[ "SubjectAreas" ].ToString();
						if ( !string.IsNullOrWhiteSpace( subjectAreas ) )
						{
							var xDoc = XDocument.Parse( subjectAreas );
							foreach ( var child in xDoc.Root.Elements() )
								index.SubjectAreas.Add( child.Attribute( "Subject" ).Value );

							if ( index.SubjectAreas.Count() > 0 )
								index.HasSubjects = true;
						}

						#endregion

						#region TextValues

						var textValues = dr[ "TextValues" ].ToString();
						if ( !string.IsNullOrWhiteSpace( textValues ) )
						{
							textValues = textValues.Replace( "&", " " );
							var xDoc = new XDocument();
							xDoc = XDocument.Parse( textValues );
							//index.Addresses = xDoc.Root.Elements().Count();
							foreach ( var child in xDoc.Root.Elements() )
							{
								var categoryId = int.Parse( child.Attribute( "CategoryId" ).Value );
								var textValue = child.Attribute( "TextValue" );
								if ( textValue != null && !string.IsNullOrWhiteSpace( textValue.Value ) )
								{
									index.TextValues.Add( textValue.Value );

									if ( textValue.Value.IndexOf( "-" ) > -1 )
										index.TextValues.Add( textValue.Value.Replace( "-", "" ) );

									if ( categoryId == 35 )
										index.Keyword.Add( textValue.Value );
								}
								//source is just direct/indirect, more want the sourceEntityType
								var codeNotation = child.Attribute( "CodedNotation" );
								if ( codeNotation != null && !string.IsNullOrWhiteSpace( codeNotation.Value ) )
								{
									index.TextValues.Add( codeNotation.Value );
									if ( codeNotation.Value.IndexOf( "-" ) > -1 )
										index.TextValues.Add( codeNotation.Value.Replace( "-", "" ) );
								}
							}
						}
						//properties to add to textvalues
						index.AvailableOnlineAt = GetRowPossibleColumn( dr, "AvailableOnlineAt", "" );
						AddTextValue( index, index.AvailableOnlineAt );

						string url = GetRowPossibleColumn( dr, "AvailabilityListing", "" );
						AddTextValue( index, url );
						index.CodedNotation = GetRowColumn( dr, "IdentificationCode", "" );
						AddTextValue( index, index.CodedNotation );

						AddTextValue( index, index.CredentialRegistryId );
						AddTextValue( index, index.Id.ToString() );
						AddTextValue( index, index.CTID, true );


						#endregion

						#region LoppProperties
						try
						{
							var loppProperties = dr[ "LoppProperties" ].ToString();
							if ( !string.IsNullOrEmpty( loppProperties ) )
							{
								loppProperties = loppProperties.Replace( "&", " " );
								var xDoc = XDocument.Parse( loppProperties );
								foreach ( var child in xDoc.Root.Elements() )
								{
									var categoryId = int.Parse( child.Attribute( "CategoryId" ).Value );
									var propertyValueId = int.Parse( child.Attribute( "PropertyValueId" ).Value );
									var property = child.Attribute( "Property" ).Value;

									index.LoppProperties.Add( new IndexProperty
									{
										CategoryId = categoryId,
										Id = propertyValueId,
										Name = property
									} );
									if ( categoryId == ( int ) CodesManager.PROPERTY_CATEGORY_Learning_Method_Type )
										index.LearningMethodTypeIds.Add( propertyValueId );
									if ( categoryId == ( int ) CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE )
										index.DeliveryMethodTypeIds.Add( propertyValueId );
									if ( categoryId == ( int ) CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE )
										index.AudienceTypeIds.Add( propertyValueId );
									else if ( categoryId == ( int )CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL )
										index.AudienceLevelTypeIds.Add( propertyValueId );

									if ( !string.IsNullOrWhiteSpace( ( string )child.Attribute( "PropertySchemaName" ) ) )
										index.TextValues.Add( ( string )child.Attribute( "PropertySchemaName" ) );
								}
							}
						}
						catch ( Exception ex )
						{
							LoggingHelper.DoTrace( 2, string.Format( " # {0}. Exception on LoppProperties id: {1}; \r\n{2}", cntr, index.Id, ex.Message ) );
						}
						#endregion

						#region Reference Frameworks - industries, occupations and classifications
						string frameworks = dr[ "Frameworks" ].ToString();
						if ( !string.IsNullOrWhiteSpace( frameworks ) )
						{
							var xDoc = new XDocument();
							xDoc = XDocument.Parse( frameworks );
							foreach ( var child in xDoc.Root.Elements() )
							{
								var framework = new IndexReferenceFramework
								{
									CategoryId = int.Parse( child.Attribute( "CategoryId" ).Value ),
									ReferenceFrameworkId = int.Parse( child.Attribute( "ReferenceFrameworkId" ).Value ),
									Name = ( string ) child.Attribute( "Name" ) ?? "",
									CodeGroup = ( string ) child.Attribute( "CodeGroup" ) ?? "",
									SchemaName = ( string ) child.Attribute( "SchemaName" ) ?? "",
									CodedNotation = ( string ) child.Attribute( "CodedNotation" ) ?? "",
								};

								if ( framework.CategoryId == 11 )
									index.Occupations.Add( framework );
								if ( framework.CategoryId == 10 )
									index.Industries.Add( framework );
								if ( framework.CategoryId == 23 )
									index.Classifications.Add( framework );
							}
							if ( index.Occupations.Count > 0 )
								index.HasOccupations = true;
							if ( index.Industries.Count > 0 )
								index.HasIndustries = true;
							if ( index.Classifications.Count > 0 )
								index.HasInstructionalPrograms = true;
						}

				
						#endregion

						#region QualityAssurance
						index.Org_QAAgentAndRoles = GetRowPossibleColumn( dr, "Org_QAAgentAndRoles" );

						HandleAgentRelationshipsForEntity( dr, index );
						

						#endregion

						#region Addresses
						var addresses = dr[ "Addresses" ].ToString();
						if ( !string.IsNullOrWhiteSpace( addresses ) )
						{
							//index.Addresses = addresses;
							var xDoc = new XDocument();
							xDoc = XDocument.Parse( addresses );
							//index.AvailableAddresses = xDoc.Root.Elements().Count();
							foreach ( var child in xDoc.Root.Elements() )
							{
								string region = ( string ) child.Attribute( "Region" ) ?? "";
								string city = ( string ) child.Attribute( "City" ) ?? "";
								index.Addresses.Add( new Address
								{
									Latitude = double.Parse( ( string ) child.Attribute( "Latitude" ) ),
									Longitude = double.Parse( ( string ) child.Attribute( "Longitude" ) ),
									Address1 = ( string ) child.Attribute( "Address1" ) ?? "",
									Address2 = ( string ) child.Attribute( "Address2" ) ?? "",
									City = ( string ) child.Attribute( "City" ) ?? "",
									AddressRegion = ( string ) child.Attribute( "Region" ) ?? "",
									PostalCode = ( string ) child.Attribute( "PostalCode" ) ?? "",
									Country = ( string ) child.Attribute( "Country" ) ?? ""
								} );
								AddTextValue( index, city );
								AddTextValue( index, region );
							}
						}

						if ( index.Addresses.Count == 0 )
						{
							//prototype: if no cred addresses, and one org address, then add to index (not detail page)
							var orgAddresses = GetRowPossibleColumn( dr, "OrgAddresses" );
							if ( !string.IsNullOrWhiteSpace( orgAddresses ) )
							{
								try
								{
									var xDoc = new XDocument();
									xDoc = XDocument.Parse( orgAddresses );
									//actually will only focus on regions
									string prevRegion = "";
									string prevCountry = "";
									//if ( xDoc.Root.Elements().Count() == 1)
									//{
									foreach ( var child in xDoc.Root.Elements() )
									{
										string region = ( string ) child.Attribute( "Region" ) ?? "";
										string city = ( string ) child.Attribute( "City" ) ?? "";
										if ( prevRegion != region )
										{
											index.Addresses.Add( new Address
											{
												Latitude = double.Parse( ( string ) child.Attribute( "Latitude" ) ),
												Longitude = double.Parse( ( string ) child.Attribute( "Longitude" ) ),
												Address1 = ( string ) child.Attribute( "Address1" ) ?? "",
												Address2 = ( string ) child.Attribute( "Address2" ) ?? "",
												//City = ( string )child.Attribute( "City" ) ?? "",
												AddressRegion = ( string ) child.Attribute( "Region" ) ?? "",
												//PostalCode = ( string )child.Attribute( "PostalCode" ) ?? "",
												Country = ( string ) child.Attribute( "Country" ) ?? ""
											} );
											prevRegion = region;
											AddTextValue( index, city );
											AddTextValue( index, region );
										}
										//}
									}
								}
								catch ( Exception ex )
								{
									LoggingHelper.DoTrace( 2, string.Format( " # {0}. Exception on OrgAddresses id: {1}; \r\n{2}", cntr, index.Id, ex.Message ) );
								}
							}
						}
						#endregion

						#region Languages
						index.InLanguage = GetLanguages( dr );
						#endregion

						#region custom reports

						int propertyId = 0;
						if ( !string.IsNullOrWhiteSpace( index.AvailableOnlineAt ) )
							if ( GetPropertyId( 61, "loppReport:AvailableOnline", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( index.TeachesCompetencies.Count > 0 )
							if ( GetPropertyId( 61, "loppReport:TeachesCompetencies", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( index.RequiresCompetencies.Count > 0 )
							if ( GetPropertyId( 61, "loppReport:RequiresCompetencies", ref propertyId ) )
								index.ReportFilters.Add( propertyId );

						var DurationProfileCount = GetRowPossibleColumn( dr, "HasDurationCount", 0 );
						if ( DurationProfileCount > 0 )
						{
							if ( GetPropertyId( 61, "loppReport:HasDurationProfile", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						}

						if ( costProfilesCount > 0 )
							if ( GetPropertyId( 61, "loppReport:HasCostProfile", ref propertyId ) )
								index.ReportFilters.Add( propertyId );

						if ( index.CommonConditionsCount > 0 )
							if ( GetPropertyId( 61, "loppReport:ReferencesCommonConditions", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( index.CommonCostsCount > 0 )
							if ( GetPropertyId( 61, "loppReport:ReferencesCommonCosts", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( index.FinancialAidCount > 0 )
							if ( GetPropertyId( 61, "loppReport:FinancialAid", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( index.ProcessProfilesCount > 0 )
							if ( GetPropertyId( 61, "loppReport:HasProcessProfile", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( index.HasOccupations )
							if ( GetPropertyId( 61, "loppReport:HasOccupations", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( index.HasIndustries )
							if ( GetPropertyId( 61, "loppReport:HasIndustries", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( index.HasInstructionalPrograms )
							if ( GetPropertyId( 61, "loppReport:HasCIP", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( index.Addresses.Count > 0 )
							if ( GetPropertyId( 61, "loppReport:HasAddresses", ref propertyId ) )
								index.ReportFilters.Add( propertyId );

						list.Add( index );
					}
					#endregion

				}
				catch ( Exception ex )
				{
					LoggingHelper.DoTrace( 2, string.Format( "LearningOpp_SearchForElastic. Last Row: {0}, LoppId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
				}
				finally
				{
					LoggingHelper.DoTrace( 2, string.Format( "LearningOpp_SearchForElastic - Complete loaded {0} records", cntr ) );
				}
				return list;
			}
		}

		public static List<PM.LearningOpportunityProfile> LearningOpp_MapFromElastic( List<LearningOppIndex> learningOpps )
		{
			var list = new List<PM.LearningOpportunityProfile>();

			foreach ( var li in learningOpps )
			{
				var index = new PM.LearningOpportunityProfile
				{
					Id = li.Id,
					Name = li.Name,
					FriendlyName = li.FriendlyName,
					Description = li.Description,
					EntityStateId = li.EntityStateId,
					RowId = li.RowId,
					SubjectWebpage = li.SubjectWebpage,
					AvailableOnlineAt = li.AvailableOnlineAt,
					CodedNotation = li.IdentificationCode,
					CTID = li.CTID,
					PrimaryOrganizationCTID = li.PrimaryOrganizationCTID,
					CredentialRegistryId = li.CredentialRegistryId,
					DateEffective = li.DateEffective,
					Created = li.Created,
					LastUpdated = li.LastUpdated,
					RequiresCount = li.RequiresCount,
					RecommendsCount = li.RecommendsCount,
					RequiredForCount = li.IsRequiredForCount,
					IsRecommendedForCount = li.IsRecommendedForCount,
					IsAdvancedStandingForCount = li.IsAdvancedStandingForCount,
					AdvancedStandingFromCount = li.AdvancedStandingFromCount,
					PreparationForCount = li.IsPreparationForCount,
					PreparationFromCount = li.PreparationFromCount,
					CompetenciesCount = li.TeachesCompetencies.Count,
					//TotalCostCount = li.TotalCostCount,
					CommonCostsCount = li.CommonCostsCount,
					CommonConditionsCount = li.CommonConditionsCount,
					FinancialAidCount = li.FinancialAidCount
				};

				if ( li.OwnerOrganizationId > 0 )
					index.OwningOrganization = new CM.Organization() { Id = li.OwnerOrganizationId, Name = li.OwnerOrganizationName };

				//addressess                

				index.Addresses = li.Addresses.Select( x => new CM.Address
				{
					Latitude = x.Latitude,
					Longitude = x.Longitude,
					Address1 = x.Address1,
					Address2 = x.Address2,
					City = x.City,
					AddressRegion = x.AddressRegion,
					PostalCode = x.PostalCode,
					Country = x.Country
				} ).ToList();
				index.EstimatedDuration = DurationProfileManager.GetAll( index.RowId );
				index.Org_QAAgentAndRoles = Fill_AgentRelationship( li.Org_QAAgentAndRoles, 130, false, false, true, "Organization" );
				index.QualityAssurance = Fill_AgentRelationship( li.AgentRelationshipsForEntity, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, "LearningOpportunity" );
				index.CredentialsList = Fill_CredentialConnectionsResult( li.CredentialsList, CodesManager.PROPERTY_CATEGORY_CONNECTION_PROFILE_TYPE );
				index.Subjects = li.SubjectAreas;

				if ( li.Industries != null && li.Industries.Count > 0 )
					index.IndustryResults = Fill_CodeItemResults( li.Industries.Where( x => x.CategoryId == 10 ).ToList(), CodesManager.PROPERTY_CATEGORY_NAICS, false, false );

				if ( li.Occupations != null && li.Occupations.Count > 0 )
					index.OccupationResults = Fill_CodeItemResults( li.Occupations.Where( x => x.CategoryId == 11 ).ToList(), CodesManager.PROPERTY_CATEGORY_SOC, false, false );

				if ( li.Classifications != null && li.Classifications.Count > 0 )
					index.InstructionalProgramClassification = Fill_CodeItemResults( li.Classifications.Where( x => x.CategoryId == 23 ).ToList(), CodesManager.PROPERTY_CATEGORY_CIP, false, false );

				index.LearningMethodTypes = Fill_CodeItemResults( li.LoppProperties.Where( x => x.CategoryId == ( int ) CodesManager.PROPERTY_CATEGORY_Learning_Method_Type ).ToList(), CodesManager.PROPERTY_CATEGORY_Learning_Method_Type, false, false );
				index.DeliveryMethodTypes = Fill_CodeItemResults( li.LoppProperties.Where( x => x.CategoryId == ( int ) CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE ).ToList(), CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE, false, false );
				index.AudienceTypes = Fill_CodeItemResults( li.LoppProperties.Where( x => x.CategoryId == ( int ) CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE ).ToList(), CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE, false, false );
				index.ListTitle = li.ListTitle;
				list.Add( index );
			}

			return list;
		}
		#endregion

		public static void HandleAgentRelationshipsForEntity( DataRow dr, BaseIndex index)
		{
			//we may be able to remove this property
			string agentRelations = GetRowPossibleColumn( dr, "AgentRelationships" );
			if ( !string.IsNullOrWhiteSpace( agentRelations ) )
			{
				var xDoc = new XDocument();
				xDoc = XDocument.Parse( agentRelations );
				foreach ( var child in xDoc.Root.Elements() )
					index.AgentRelationships.Add( int.Parse( ( string )child.Attribute( "RelationshipTypeId" ) ) );
			}

			string agentRelationshipsForEntity = GetRowPossibleColumn( dr, "AgentRelationshipsForEntity" );
			if ( !string.IsNullOrWhiteSpace( agentRelationshipsForEntity ) )
			{
				if ( ContainsUnicodeCharacter( agentRelationshipsForEntity ) )
				{
					agentRelationshipsForEntity = Regex.Replace( agentRelationshipsForEntity, @"[^\u0000-\u007F]+", string.Empty );
				}
				agentRelationshipsForEntity = agentRelationshipsForEntity.Replace( "&", " " );
				var xDoc = XDocument.Parse( agentRelationshipsForEntity );
				foreach ( var child in xDoc.Root.Elements() )
				{
					string agentName = ( string )child.Attribute( "AgentName" ) ?? "";
					string relationshipTypeIds = ( string )child.Attribute( "RelationshipTypeIds" ) ?? "";
					string relationships = ( string )child.Attribute( "Relationships" ) ?? "";
					string agentContextRoles = ( string )child.Attribute( "AgentContextRoles" ) ?? "";

					if ( !string.IsNullOrWhiteSpace( agentName ) && !string.IsNullOrWhiteSpace( relationshipTypeIds ) )
					{
						var relationshipIds = new List<int>();
						foreach ( var s in child.Attribute( "RelationshipTypeIds" ).Value.Split( new char[] { ',' } ) )
						{
							relationshipIds.Add( int.Parse( s.Trim() ) );
						}
						var relationshipsList = new List<string>();
						foreach ( var s in child.Attribute( "Relationships" ).Value.Split( new char[] { ',' } ) )
						{
							if (!string.IsNullOrWhiteSpace(s))
								relationshipsList.Add( s.Trim() );
						}
						var agentRelationshipsList = new List<string>();
						foreach ( var s in child.Attribute( "AgentContextRoles" ).Value.Split( new char[] { ',' } ) )
						{
							if ( !string.IsNullOrWhiteSpace( s ) )
								agentRelationshipsList.Add( s.Trim() );
						}
						index.AgentRelationshipsForEntity.Add( new AgentRelationshipForEntity
						{
							OrgId = int.Parse( child.Attribute( "OrgId" ).Value ),
							//todo get/split list of ids
							//RelationshipTypeId = int.Parse( child.Attribute( "RelationshipTypeId" ).Value ),
							RelationshipTypeIds = relationshipIds,
							AgentUrl = ( string )child.Attribute( "AgentUrl" ) ?? "",
							AgentName = ( string )child.Attribute( "AgentName" ) ?? "",
							EntityStateId = int.Parse( child.Attribute( "EntityStateId" ).Value ),
							Relationships = relationshipsList,
							AgentRelationships = agentRelationshipsList
						} );
						//add phrase. ex Accredited by microsoft
						if ( !string.IsNullOrWhiteSpace( relationships ) && !string.IsNullOrWhiteSpace( agentName ) )
							index.TextValues.Add( string.Format( "{0} {1}", relationships, agentName ) );
					}
				}
			}
		}
	}
}
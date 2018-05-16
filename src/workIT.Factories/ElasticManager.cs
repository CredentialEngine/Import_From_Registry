using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
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
        public static List<CredentialIndex> Credential_SearchForElastic( string filter, int pageSize = 0, int pageNumber = 1 )
        {
            string connectionString = DBConnectionRO();
            var item = new CredentialIndex();
            var list = new List<CredentialIndex>();
            var result = new DataTable();
            LoggingHelper.DoTrace( 2, "Credential_SearchForElastic - Starting. filter\r\n " + filter );
            bool includingHasPartIsPartWithConnections = UtilityManager.GetAppKeyValue( "includeHasPartIsPartWithConnections", false );
            int cntr = 0;
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
                        string rows = command.Parameters[command.Parameters.Count - 1].Value.ToString();
                        //pTotalRows = Int32.Parse( rows );
                    }
                    catch ( Exception ex )
                    {
                        item = new CredentialIndex();
                        item.Name = "EXCEPTION ENCOUNTERED";
                        item.Description = ex.Message;
                        //item.CredentialTypeSchema = "error";
                        list.Add( item );

                        return list;
                    }
                }

                //Used for costs. Only need to get these once. See below. - NA 5/12/2017
                //var currencies = CodesManager.GetCurrencies();
                //var costTypes = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_CREDENTIAL_ATTAINMENT_COST );
                int costProfilesCount = 0;
                foreach ( DataRow dr in result.Rows )
                {
                    cntr++;
                    if ( cntr % 100 == 0 )
                        LoggingHelper.DoTrace( 2, string.Format( " loading record: {0}", cntr ) );
                    //avgMinutes = 0;
                    item = new CredentialIndex();
                    item.EntityTypeId = 1;
                    item.Id = GetRowColumn( dr, "Id", 0 );
                    //fine for a full reindex, need algorithm for small updates - could add to sql table
                    item.NameIndex = cntr * 1000;

                    //only full entities (3) will be in the index
                    item.EntityStateId = GetRowPossibleColumn( dr, "EntityStateId", 0 );
                    item.Name = dr["Name"].ToString();
                    if ( !string.IsNullOrWhiteSpace( GetRowPossibleColumn( dr, "AlternateName", "" ) ) )
                        item.AlternateNames.Add( GetRowPossibleColumn( dr, "AlternateName", "" ) );

                    item.FriendlyName = FormatFriendlyTitle( item.Name );

                    item.SubjectWebpage = dr["SubjectWebpage"].ToString();

                    string rowId = dr["EntityUid"].ToString();
                    item.RowId = new Guid( rowId );

                    item.Description = dr["Description"].ToString();

                    item.OwnerOrganizationId = GetRowPossibleColumn( dr, "OwningOrganizationId", 0 );
                    item.OwnerOrganizationName = GetRowPossibleColumn( dr, "OwningOrganization" );
                    if ( item.OwnerOrganizationName.Length > 0 )
                        item.ListTitle = item.Name + " (" + item.OwnerOrganizationName + ")";
                    else
                        item.ListTitle = item.Name;

                    item.CTID = GetRowColumn( dr, "CTID" );

                    item.CredentialRegistryId = dr["CredentialRegistryId"].ToString();

                    string date = GetRowColumn( dr, "EffectiveDate", "" );
                    if ( IsValidDate( date ) )
                        item.DateEffective = ( DateTime.Parse( date ).ToShortDateString() );
                    else
                        item.DateEffective = "";
                    date = GetRowColumn( dr, "Created", "" );
                    if ( IsValidDate( date ) )
                        item.Created = DateTime.Parse( date );
                    date = GetRowColumn( dr, "LastUpdated", "" );
                    if ( IsValidDate( date ) )
                        item.LastUpdated = DateTime.Parse( date );

                    item.CredentialType = GetRowPossibleColumn( dr, "CredentialType", "" );

                    item.CredentialTypeSchema = GetRowPossibleColumn( dr, "CredentialTypeSchema", "" );
                    item.CredentialTypeId = GetRowPossibleColumn( dr, "CredentialTypeId", 0 );

                    // item.NumberOfCostProfileItems = GetRowColumn( dr, "NumberOfCostProfileItems", 0 );
                    item.TotalCost = GetRowPossibleColumn( dr, "TotalCost", 0 );
                    //AverageMinutes is a rough approach to sorting. If present, get the duration profiles

                    item.EstimatedTimeToEarn = GetRowPossibleColumn( dr, "AverageMinutes", 0 );

                    item.IsAQACredential = GetRowColumn( dr, "IsAQACredential", false );
                    item.LearningOppsCompetenciesCount = GetRowColumn( dr, "LearningOppsCompetenciesCount", 0 );
                    item.AssessmentsCompetenciesCount = GetRowColumn( dr, "AssessmentsCompetenciesCount", 0 );

                    item.HasPartCount = GetRowColumn( dr, "HasPartCount", 0 );
                    item.IsPartOfCount = GetRowColumn( dr, "IsPartOfCount", 0 );
                    item.RequiresCount = GetRowColumn( dr, "RequiresCount", 0 );
                    item.RecommendsCount = GetRowColumn( dr, "RecommendsCount", 0 );
                    item.RequiredForCount = GetRowColumn( dr, "IsRequiredForCount", 0 );
                    item.IsRecommendedForCount = GetRowColumn( dr, "IsRecommendedForCount", 0 );
                    item.RenewalCount = GetRowColumn( dr, "RenewalCount", 0 );
                    item.IsAdvancedStandingForCount = GetRowColumn( dr, "IsAdvancedStandingForCount", 0 );
                    item.AdvancedStandingFromCount = GetRowColumn( dr, "AdvancedStandingFromCount", 0 );
                    item.PreparationForCount = GetRowColumn( dr, "IsPreparationForCount", 0 );
                    item.PreparationFromCount = GetRowColumn( dr, "IsPreparationFromCount", 0 );
                    item.EntryConditionCount = GetRowColumn( dr, "entryConditionCount", 0 );

                    //item.TotalCostCount = GetRowPossibleColumn( dr, "TotalCostCount", 0M );
                    costProfilesCount = GetRowPossibleColumn( dr, "CostProfilesCount", 0 );

                    item.CommonConditionsCount = GetRowPossibleColumn( dr, "CommonConditionsCount", 0 );
                    item.CommonCostsCount = GetRowPossibleColumn( dr, "CommonCostsCount", 0 );
                    item.FinancialAidCount = GetRowPossibleColumn( dr, "FinancialAidCount", 0 );

                    item.EmbeddedCredentialsCount = GetRowPossibleColumn( dr, "EmbeddedCredentialsCount", 0 );
                    item.RequiredAssessmentsCount = GetRowPossibleColumn( dr, "RequiredAssessmentsCount", 0 );
                    item.RequiredCredentialsCount = GetRowPossibleColumn( dr, "RequiredCredentialsCount", 0 );
                    item.RequiredLoppCount = GetRowPossibleColumn( dr, "RequiredLoppCount", 0 );
                    item.RecommendedAssessmentsCount = GetRowPossibleColumn( dr, "RecommendedAssessmentsCount", 0 );
                    item.RecommendedCredentialsCount = GetRowPossibleColumn( dr, "RecommendedCredentialsCount", 0 );
                    item.RecommendedLoppCount = GetRowPossibleColumn( dr, "RecommendedLoppCount", 0 );
                    item.BadgeClaimsCount = GetRowPossibleColumn( dr, "badgeClaimsCount", 0 );
                    item.RevocationProfilesCount = GetRowPossibleColumn( dr, "RevocationProfilesCount", 0 );
                    item.ProcessProfilesCount = GetRowPossibleColumn( dr, "ProcessProfilesCount", 0 );
                    item.HasOccupationsCount = GetRowPossibleColumn( dr, "HasOccupationsCount", 0 );
                    item.HasIndustriesCount = GetRowPossibleColumn( dr, "HasIndustriesCount", 0 );

                    //-actual connection type (no credential info), with the schema name, and number of connections
                    // 8~Is Preparation For~ceterms:isPreparationFor~2
                    item.ConnectionsList = dr["ConnectionsList"].ToString();

                    //connection type, plus Id, and name of credential
                    //8~Is Preparation For~136~MSSC Certified Production Technician (CPT©)~| 8~Is Preparation For~272~MSSC Certified Logistics Technician (CLT©)~
                    item.CredentialsList = dr["CredentialsList"].ToString();
                    if ( includingHasPartIsPartWithConnections ) item.CredentialsList += item.IsPartOfList;

                    try
                    {
                        string credentialConnections = GetRowPossibleColumn( dr, "CredentialConnections" );
                        if ( !string.IsNullOrWhiteSpace( credentialConnections ) )
                        {
                            Connection conn = new Connection();
                            var xDoc = XDocument.Parse( credentialConnections );
                            foreach ( var child in xDoc.Root.Elements() )
                            {
                                conn = new Connection();
                                conn.ConnectionType = ( string )child.Attribute( "ConnectionType" ) ?? "";
                                conn.ConnectionTypeId = int.Parse( child.Attribute( "ConnectionTypeId" ).Value );

                                //do something with counts for this type

                                conn.CredentialId = int.Parse( child.Attribute( "CredentialId" ).Value );
                                if ( conn.CredentialId > 0 )
                                {
                                    //add credential
                                    conn.Credential = ( string )child.Attribute( "CredentialName" ) ?? "";
                                    //??????
                                    conn.CredentialOrgId = int.Parse( child.Attribute( "credOrgid" ).Value );
                                    conn.CredentialOrganization = ( string )child.Attribute( "credOrganization" ) ?? "";
                                }
                                conn.AssessmentId = int.Parse( child.Attribute( "AssessmentId" ).Value );
                                if ( conn.AssessmentId > 0 )
                                {
                                    conn.Assessment = ( string )child.Attribute( "AssessmentName" ) ?? "";
                                    conn.AssessmentOrganizationId = int.Parse( child.Attribute( "asmtOrgid" ).Value );
                                    conn.AssessmentOrganization = ( string )child.Attribute( "asmtOrganization" ) ?? "";
                                }
                                conn.LoppId = int.Parse( child.Attribute( "LearningOpportunityId" ).Value );
                                if ( conn.LoppId > 0 )
                                {
                                    conn.LearningOpportunity = ( string )child.Attribute( "LearningOpportunityName" ) ?? "";
                                    conn.LoppOrganizationId = int.Parse( child.Attribute( "loppOrgid" ).Value );
                                    conn.LearningOpportunityOrganization = ( string )child.Attribute( "loppOrganization" ) ?? "";
                                }

                                item.Connections.Add( conn );
                            }
                        }
                    }
                    catch ( Exception ex )
                    {
                        LoggingHelper.DoTrace( 2, string.Format( " # {0}. Exception on CredentialConnections id: {1}; \r\n{2}", cntr, item.Id, ex.Message ) );
                    }


                    #region QualityAssurance
                    try
                    {
                        //2|7|6|10|11
                        string relationshipTypes = GetRowPossibleColumn( dr, "RelationshipTypes" );
                        if ( !string.IsNullOrWhiteSpace( relationshipTypes ) )
                        {
                            foreach ( var name in relationshipTypes.Split( '|' ) )
                            {
                                if ( !string.IsNullOrWhiteSpace( name ) )
                                    item.RelationshipTypes.Add( int.Parse( name ) );
                            }
                        }

                        //16-09-12 mp - changed to use pipe (|) rather than ; due to conflicts with actual embedded semicolons
                        //QARolesList: includes roleId, roleName
                        //1~Accredited~1~Credential| 2~Approved~1~Credential
                        //item.QARolesResults = dr[ "QARolesList" ].ToString();

                        //AgentAndRoles: includes roleId, roleName, OrgId, and orgName
                        //1~Accredited~3~National Commission for Certifying Agencies (NCCA) [ reference ]~Credential
                        item.AgentAndRoles = dr["AgentAndRoles"].ToString();

                        //QA on owning org
                        //This should be combined with the credential QA - see publisher
                        //this may not being used properly - no data other than blank and "not applicable" (remove this text). Should be:
                        //QAOrgRolesList: includes roleId, roleName, number of organizations in role
                        //10~Recognized~2~Organization| 12~Regulated~2~Organization
                        //replacing the source
                        //item.Org_QARolesList = GetRowPossibleColumn( dr, "QAOrgRolesList" );
                        item.Org_QARolesList = GetRowPossibleColumn( dr, "Org_QARolesList" );

                        //QAAgentAndRoles - List actual orgIds and names for roles
                        //10~Owning Org is Recognized~1105~Accrediting Commission for Community and Junior Colleges - updated~Organization| 10~Owning Org is Recognized~64~AdvancED~Organization| 12~Owning Org is Regulated~55~TESTING_American National Standards Institute (ANSI)~Organization| 12~Owning Org is Regulated~64~AdvancED~Organization
                        item.Org_QAAgentAndRoles = GetRowPossibleColumn( dr, "Org_QAAgentAndRoles" );

                        string qualityAssurance = GetRowPossibleColumn( dr, "QualityAssurance" );
                        if ( !string.IsNullOrWhiteSpace( qualityAssurance ) )
                        {
                            var xDoc = XDocument.Parse( qualityAssurance );
                            foreach ( var child in xDoc.Root.Elements() )
                            {
                                string agentName = ( string )child.Attribute( "AgentName" ) ?? "";
                                string relationship = ( string )child.Attribute( "SourceToAgentRelationship" ) ?? "";

                                item.QualityAssurance.Add( new IndexQualityAssurance
                                {
                                    AgentRelativeId = int.Parse( child.Attribute( "AgentRelativeId" ).Value ),
                                    RelationshipTypeId = int.Parse( child.Attribute( "RelationshipTypeId" ).Value ),
                                    SourceToAgentRelationship = ( string )child.Attribute( "SourceToAgentRelationship" ) ?? "",
                                    AgentToSourceRelationship = ( string )child.Attribute( "AgentToSourceRelationship" ) ?? "",
                                    AgentUrl = ( string )child.Attribute( "AgentUrl" ) ?? "",
                                    AgentName = ( string )child.Attribute( "AgentName" ) ?? "",
                                    EntityStateId = int.Parse( child.Attribute( "EntityStateId" ).Value ),
                                } );
                                //add phrase. ex Accredited by microsoft
                                if ( !string.IsNullOrWhiteSpace( relationship ) && !string.IsNullOrWhiteSpace( agentName ) )
                                    item.TextValues.Add( string.Format( "{0} {1}", relationship, agentName ) );
                            }
                        }
                    }
                    catch ( Exception ex )
                    {
                        LoggingHelper.DoTrace( 2, string.Format( " # {0}. Exception on QualityAssurance id: {1}; \r\n{2}", cntr, item.Id, ex.Message ) );
                    }
                    #endregion

                    #region Subjects
                    var subjects = GetRowPossibleColumn( dr, "Subjects" );
                    if ( !string.IsNullOrWhiteSpace( subjects ) )
                    {
                        var xDoc = new XDocument();
                        xDoc = XDocument.Parse( subjects );
                        foreach ( var child in xDoc.Root.Elements() )
                        {
                            var cs = new IndexSubject();
                            var subject = child.Attribute( "Subject" );
                            if ( subject != null ) cs.Name = subject.Value;

                            //source is just direct/indirect, more want the sourceEntityType
                            var source = child.Attribute( "Source" );
                            if ( source != null ) cs.Source = source.Value;

                            int outputId = 0;
                            var entityTypeId = child.Attribute( "EntityTypeId" );
                            if ( entityTypeId != null )
                                if ( Int32.TryParse( entityTypeId.Value, out outputId ) )
                                    cs.EntityTypeId = outputId;

                            var referenceBaseId = child.Attribute( "ReferenceBaseId" );
                            if ( referenceBaseId != null )
                                if ( Int32.TryParse( referenceBaseId.Value, out outputId ) )
                                    cs.ReferenceBaseId = outputId;

                            item.Subjects.Add( cs );
                        }
                        //item.Subject = string.Join( "|", item.Subjects.Select( x => x.Name ) );
                    }
                    #endregion

                    #region Addresses
                    var addresses = GetRowPossibleColumn( dr, "Addresses" );
                    if ( !string.IsNullOrWhiteSpace( addresses ) )
                    {
                        //item.Addresses = addresses;
                        var xDoc = new XDocument();
                        xDoc = XDocument.Parse( addresses );

                        foreach ( var child in xDoc.Root.Elements() )
                            item.Addresses.Add( new Address
                            {
                                Latitude = double.Parse( ( string )child.Attribute( "Latitude" ) ),
                                Longitude = double.Parse( ( string )child.Attribute( "Longitude" ) ),
                                Address1 = ( string )child.Attribute( "Address1" ) ?? "",
                                Address2 = ( string )child.Attribute( "Address2" ) ?? "",
                                City = ( string )child.Attribute( "City" ) ?? "",
                                AddressRegion = ( string )child.Attribute( "Region" ) ?? "",
                                PostalCode = ( string )child.Attribute( "PostalCode" ) ?? "",
                                Country = ( string )child.Attribute( "Country" ) ?? ""
                            } );
                    }
                    #endregion

                    if ( GetRowPossibleColumn( dr, "badgeClaimsCount", 0 ) > 0 )
                        item.HasVerificationType_Badge = true;  //Update this with appropriate source data

                    #region TextValues, CodedNotation
                    try
                    {
                        var textValues = GetRowPossibleColumn( dr, "TextValues" );
                        if ( !string.IsNullOrWhiteSpace( textValues ) )
                        {
                            var xDoc = new XDocument();
                            xDoc = XDocument.Parse( textValues );
                            foreach ( var child in xDoc.Root.Elements() )
                            {
                                var textValue = child.Attribute( "TextValue" );
                                if ( textValue != null )
                                {
                                    item.TextValues.Add( textValue.Value );

                                    if ( textValue.Value.IndexOf( "-" ) > -1 )
                                        item.TextValues.Add( textValue.Value.Replace( "-", "" ) );
                                }
                                //source is just direct/indirect, more want the sourceEntityType
                                var codeNotation = child.Attribute( "CodedNotation" );
                                if ( codeNotation != null )
                                {
                                    item.TextValues.Add( codeNotation.Value );
                                    if ( codeNotation.Value.IndexOf( "-" ) > -1 )
                                        item.TextValues.Add( codeNotation.Value.Replace( "-", "" ) );
                                }
                            }
                        }


                        item.TextValues.Add( item.CredentialType );
                        //properties to add to textvalues
                        string url = GetRowPossibleColumn( dr, "AvailableOnlineAt", "" );
                        if ( !string.IsNullOrWhiteSpace( url ) )
                            item.TextValues.Add( url );
                        url = GetRowPossibleColumn( dr, "AvailabilityListing", "" );
                        if ( !string.IsNullOrWhiteSpace( url ) )
                            item.TextValues.Add( url );

                        if ( !string.IsNullOrWhiteSpace( item.CredentialRegistryId ) )
                            item.TextValues.Add( item.CredentialRegistryId );
                        string indexField = GetRowPossibleColumn( dr, "CredentialId", "" );
                        if ( !string.IsNullOrWhiteSpace( indexField ) )
                            item.TextValues.Add( indexField );

                        item.TextValues.Add( item.Id.ToString() );
                        item.TextValues.Add( item.CTID );

                        //not sure we need level list with AudienceLevelTypeIds?
                        //37~Masters Degree Level| 38~Doctoral Degree Level
                        item.LevelsResults = GetRowPossibleColumn( dr, "LevelsList" );
                        //37|38
                        string propertyValues = GetRowPossibleColumn( dr, "AudienceLevelTypeIds" );
                        if ( !string.IsNullOrWhiteSpace( propertyValues ) )
                        {
                            foreach ( var propertyValueId in propertyValues.Split( '|' ) )
                            {
                                item.AudienceLevelTypeIds.Add( int.Parse( propertyValueId ) );
                            }
                        }
                        if ( !string.IsNullOrWhiteSpace( item.LevelsResults ) )
                            item.TextValues.Add( item.LevelsResults );

                    }
                    catch ( Exception ex )
                    {
                        LoggingHelper.DoTrace( 2, string.Format( " # {0}. Exception on TextValues, CodedNotation id: {1}; \r\n{2}", cntr, item.Id, ex.Message ) );
                    }
                    #endregion

                    #region Competencies
                    //these are competencies from condition profiles
                    //not sure we want these in the index
                    string competencies = GetRowPossibleColumn( dr, "Competencies" );
                    if ( !string.IsNullOrWhiteSpace( competencies ) )
                    {
                        var xDoc = new XDocument();
                        xDoc = XDocument.Parse( competencies );
                        foreach ( var child in xDoc.Root.Elements() )
                        {
                            var competency = new IndexCompetency
                            {
                                Name = ( string )child.Attribute( "Name" ) ?? "",
                                Description = ( string )child.Attribute( "Description" ) ?? ""
                            };

                            item.Competencies.Add( competency );
                        }
                    }
                    #endregion

                    #region Reference Frameworks - industries,and occupations
                    string frameworks = GetRowPossibleColumn( dr, "Frameworks" );
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
                                Name = ( string )child.Attribute( "Name" ) ?? "",
                                CodeGroup = ( string )child.Attribute( "CodeGroup" ) ?? "",
                                SchemaName = ( string )child.Attribute( "SchemaName" ) ?? "",
                                CodedNotation = ( string )child.Attribute( "CodedNotation" ) ?? "",
                            };

                            //item.Frameworks.Add( framework );
                            if ( framework.CategoryId == 11 ) item.Occupations.Add( framework );
                            if ( framework.CategoryId == 10 ) item.Industries.Add( framework );
                        }
                    }
                    #endregion

                    #region Custom Reports
                    int propertyId = 0;

                    string availableOnlineAt = GetRowPossibleColumn( dr, "AvailableOnlineAt", "" );
                    if ( !string.IsNullOrWhiteSpace( availableOnlineAt ) )
                        if ( GetPropertyId( 58, "credReport:AvailableOnline", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    //var EmbeddedCredentialsCount = GetRowPossibleColumn( dr, "EmbeddedCredentialsCount", 0 );
                    //if ( EmbeddedCredentialsCount > 0 )
                    //    if ( GetPropertyId( 58, "credReport:HasEmbeddedCredentials", ref propertyId ) )
                    //        item.ReportFilters.Add( propertyId );
                    if ( item.EmbeddedCredentialsCount > 0 )
                        if ( GetPropertyId( 58, "credReport:HasEmbeddedCredentials", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    if ( costProfilesCount > 0 )
                        if ( GetPropertyId( 58, "credReport:HasCostProfile", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    if ( item.CommonConditionsCount > 0 )
                        if ( GetPropertyId( 58, "credReport:ReferencesCommonConditions", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    if ( item.CommonCostsCount > 0 )
                        if ( GetPropertyId( 58, "credReport:ReferencesCommonCosts", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    if ( item.FinancialAidCount > 0 )
                        if ( GetPropertyId( 58, "credReport:FinancialAid", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    if ( item.RequiredAssessmentsCount > 0 )
                        if ( GetPropertyId( 58, "credReport:RequiresAssessment", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    if ( item.RequiredCredentialsCount > 0 )
                        if ( GetPropertyId( 58, "credReport:RequiresCredential", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    if ( item.RequiredLoppCount > 0 )
                        if ( GetPropertyId( 58, "credReport:RequiresLearningOpportunity", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    if ( item.RecommendedAssessmentsCount > 0 )
                        if ( GetPropertyId( 58, "credReport:RecommendsAssessment", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    if ( item.RecommendedCredentialsCount > 0 )
                        if ( GetPropertyId( 58, "credReport:RecommendsCredential", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    if ( item.RecommendedLoppCount > 0 )
                        if ( GetPropertyId( 58, "credReport:RecommendsLearningOpportunity", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    if ( item.BadgeClaimsCount > 0 )
                        if ( GetPropertyId( 58, "credReport:HasVerificationBadges", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    if ( item.RevocationProfilesCount > 0 )
                        if ( GetPropertyId( 58, "credReport:HasRevocation", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    if ( item.ProcessProfilesCount > 0 )
                        if ( GetPropertyId( 58, "credReport:HasProcessProfile", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    if ( item.HasOccupationsCount > 0 )
                        if ( GetPropertyId( 58, "credReport:HasOccupations", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    if ( item.HasIndustriesCount > 0 )
                        if ( GetPropertyId( 58, "credReport:HasIndustries", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    if ( item.IsPartOfCount > 0 )
                        if ( GetPropertyId( 58, "credReport:IsPartOfCredential", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    if ( item.HasPartCount > 0 )
                        if ( GetPropertyId( 58, "credReport:HasEmbeddedCredentials", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );

                    var RequiresCompetenciesCount = GetRowPossibleColumn( dr, "RequiresCompetenciesCount", 0 );
                    var AssessmentsCompetenciesCount = GetRowPossibleColumn( dr, "AssessmentsCompetenciesCount", 0 );
                    var LearningOppsCompetenciesCount = GetRowPossibleColumn( dr, "LearningOppsCompetenciesCount", 0 );

                    if ( RequiresCompetenciesCount > 0 )
                    {
                        if ( GetPropertyId( 58, "credReport:RequiresCompetencies", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    }
                    if ( RequiresCompetenciesCount > 0 || AssessmentsCompetenciesCount > 0 || LearningOppsCompetenciesCount > 0 )
                    {
                        if ( GetPropertyId( 58, "credReport:HasCompetencies", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    }
                    var HasConditionProfileCount = GetRowPossibleColumn( dr, "HasConditionProfileCount", 0 );
                    if ( HasConditionProfileCount > 0 )
                    {
                        if ( GetPropertyId( 58, "credReport:HasConditionProfile", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    }
                    var DurationProfileCount = GetRowPossibleColumn( dr, "HasDurationCount", 0 );
                    if ( DurationProfileCount > 0 )
                    {

                        if ( GetPropertyId( 58, "credReport:HasDurationProfile", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    }
                    #endregion

                    list.Add( item );
                }

                LoggingHelper.DoTrace( 2, string.Format( "Credential_SearchForElastic - Complete loaded {0} records", cntr ) );

                return list;
            }
        }

        public static List<CM.CredentialSummary> Credential_MapFromElastic( List<CredentialIndex> credentials )
        {
            var list = new List<CM.CredentialSummary>();

            var currencies = CodesManager.GetCurrencies();
            var costTypes = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_CREDENTIAL_ATTAINMENT_COST );
            bool includingHasPartIsPartWithConnections = UtilityManager.GetAppKeyValue( "includeHasPartIsPartWithConnections", false );

            foreach ( var ci in credentials )
            {
                var item = new CM.CredentialSummary();

                //avgMinutes = 0;
                item = new CM.CredentialSummary
                {
                    Id = ci.Id,
                    Name = ci.Name,
                    FriendlyName = ci.FriendlyName,
                    SubjectWebpage = ci.SubjectWebpage,
                    RowId = ci.RowId,
                    Description = ci.Description,
                    OwnerOrganizationId = ci.OwnerOrganizationId,
                    OwnerOrganizationName = ci.OwnerOrganizationName,
                    CTID = ci.CTID,
                    CredentialRegistryId = ci.CredentialRegistryId,
                    DateEffective = ci.DateEffective,
                    Created = ci.Created,
                    LastUpdated = ci.LastUpdated,
                    //Version = ci.Version,
                    //LatestVersionUrl = ci.LatestVersionUrl,
                    //PreviousVersion = ci.PreviousVersion,
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

                //AverageMinutes is a rough approach to sorting. If present, get the duration profiles
                if ( ci.EstimatedTimeToEarn > 0 )
                    item.EstimatedTimeToEarn = DurationProfileManager.GetAll( item.RowId );

                if ( ci.Industries != null && ci.Industries.Count > 0 )
                    item.NaicsResults = Fill_CodeItemResults( ci.Industries.Where( x => x.CategoryId == 10 ).ToList(), CodesManager.PROPERTY_CATEGORY_NAICS, false, false );

                if ( ci.Occupations != null && ci.Occupations.Count > 0 )
                    item.OccupationResults = Fill_CodeItemResults( ci.Occupations.Where( x => x.CategoryId == 11 ).ToList(), CodesManager.PROPERTY_CATEGORY_SOC, false, false );

                //16-09-12 mp - changed to use pipe (|) rather than ; due to conflicts with actual embedded semicolons
                item.LevelsResults = Fill_CodeItemResults( ci.LevelsResults, CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL, false, false );

                //   item.QARolesResults = Fill_CodeItemResults( ci.QARolesResults, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, true );
                //  item.AgentAndRoles = Fill_AgentRelationship( ci.AgentAndRoles, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false, true );

                item.Org_QARolesResults = Fill_CodeItemResults( ci.Org_QARolesList, 130, false, true );
                item.Org_QAAgentAndRoles = Fill_AgentRelationship( ci.Org_QAAgentAndRoles, 130, false, false, true, "Organization" );
                item.AgentAndRoles = Fill_AgentRelationship( ci.QualityAssurance, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false, true, "Credential" );

                item.ConnectionsList = Fill_CodeItemResults( ci.ConnectionsList, CodesManager.PROPERTY_CATEGORY_CONNECTION_PROFILE_TYPE, true, true );

                if ( includingHasPartIsPartWithConnections )
                {
                    //manually add other connections
                    if ( ci.HasPartCount > 0 )
                    {
                        item.ConnectionsList.Results.Add( new CodeItem() { Id = 0, Title = "Includes", SchemaName = "hasPart", Totals = ci.HasPartCount } );
                    }
                    if ( ci.IsPartOfCount > 0 )
                    {
                        item.ConnectionsList.Results.Add( new CodeItem() { Id = 0, Title = "Included With", SchemaName = "isPartOf", Totals = ci.IsPartOfCount } );
                    }
                }

                item.HasPartsList = Fill_CredentialConnectionsResult( ci.HasPartsList, CodesManager.PROPERTY_CATEGORY_CONNECTION_PROFILE_TYPE );

                item.IsPartOfList = Fill_CredentialConnectionsResult( ci.IsPartOfList, CodesManager.PROPERTY_CATEGORY_CONNECTION_PROFILE_TYPE );

                item.CredentialsList = Fill_CredentialConnectionsResult( ci.CredentialsList, CodesManager.PROPERTY_CATEGORY_CONNECTION_PROFILE_TYPE );
                item.ListTitle = ci.ListTitle;
                item.Subjects = ci.Subjects.Select( x => x.Name ).Distinct().ToList();

                //addressess                

                item.Addresses = ci.Addresses.Select( x => new CM.Address
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

                list.Add( item );
            }

            return list;
        }


        #endregion

        #region Organization Elastic Index
        public static List<OrganizationIndex> Organization_SearchForElastic( string filter )
        {
            string connectionString = DBConnectionRO();
            var item = new OrganizationIndex();
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
                        string rows = command.Parameters[command.Parameters.Count - 1].Value.ToString();
                    }
                    catch ( Exception ex )
                    {
                        item = new OrganizationIndex();
                        item.Name = "EXCEPTION ENCOUNTERED";
                        item.Description = ex.Message;
                        list.Add( item );

                        return list;
                    }
                }

                foreach ( DataRow dr in result.Rows )
                {
                    cntr++;
                    if ( cntr % 50 == 0 )
                        LoggingHelper.DoTrace( 2, string.Format( " Organization loading record: {0}", cntr ) );

                    item = new OrganizationIndex();
                    item.EntityTypeId = 2;
                    item.Id = GetRowColumn( dr, "Id", 0 );
                    item.NameIndex = cntr * 1000;
                    item.Name = GetRowColumn( dr, "Name", "missing" );
                    item.FriendlyName = FormatFriendlyTitle( item.Name );
                    item.SubjectWebpage = GetRowColumn( dr, "SubjectWebpage", "" );

                    string rowId = GetRowColumn( dr, "RowId" );
                    if ( IsValidGuid( rowId ) )
                        item.RowId = new Guid( rowId );

                    item.Description = GetRowColumn( dr, "Description", "" );
                    item.CTID = GetRowPossibleColumn( dr, "CTID", "" );
                    item.CredentialRegistryId = GetRowPossibleColumn( dr, "CredentialRegistryId", "" );

                    string date = GetRowColumn( dr, "Created", "" );
                    if ( IsValidDate( date ) )
                        item.Created = DateTime.Parse( date );
                    date = GetRowColumn( dr, "LastUpdated", "" );
                    if ( IsValidDate( date ) )
                        item.LastUpdated = DateTime.Parse( date );

                    item.ImageURL = GetRowColumn( dr, "ImageUrl", "" );
                    if ( GetRowColumn( dr, "CredentialCount", 0 ) > 0 )
                        item.IsACredentialingOrg = true;
                    item.ISQAOrganization = GetRowColumn( dr, "IsAQAOrganization", false );
                    //item.MainPhoneNumber = CM.PhoneNumber.DisplayPhone( GetRowColumn( dr, "MainPhoneNumber", "" ) );

                    item.OwnedByResults = dr["OwnedByList"].ToString();

                    item.OfferedByResults = dr["OfferedByList"].ToString();

                    item.AsmtsOwnedByResults = dr["AsmtsOwnedByList"].ToString();
                    item.LoppsOwnedByResults = dr["LoppsOwnedByList"].ToString();
                    item.ApprovedByResults = dr["ApprovedByList"].ToString();
                    item.AccreditedByResults = dr["AccreditedByList"].ToString();
                    item.RecognizedByResults = dr["RecognizedByList"].ToString();
                    item.RegulatedByResults = dr["RegulatedByList"].ToString();
                    item.VerificationProfilesCount = GetRowPossibleColumn( dr, "VerificationProfilesCount", 0 );
                    item.CostManifestsCount = GetRowPossibleColumn( dr, "CostManifestsCount", 0 );
                    item.ConditionManifestsCount = GetRowPossibleColumn( dr, "ConditionManifestsCount", 0 );
                    item.SubsidiariesCount = GetRowPossibleColumn( dr, "SubsidiariesCount", 0 );
                    item.DepartmentsCount = GetRowPossibleColumn( dr, "DepartmentsCount", 0 );
                    item.HasIndustriesCount = GetRowPossibleColumn( dr, "HasIndustriesCount", 0 );

                    #region Addresses
                    var addresses = dr["Addresses"].ToString();
                    if ( !string.IsNullOrWhiteSpace( addresses ) )
                    {
                        //item.Addresses = addresses;
                        var xDoc = new XDocument();
                        //item.AddressesCount = xDoc.Root.Elements().Count();
                        xDoc = XDocument.Parse( addresses );

                        foreach ( var child in xDoc.Root.Elements() )
                            item.Addresses.Add( new Address
                            {
                                Latitude = double.Parse( ( string )child.Attribute( "Latitude" ) ),
                                Longitude = double.Parse( ( string )child.Attribute( "Longitude" ) ),
                                Address1 = ( string )child.Attribute( "Address1" ) ?? "",
                                Address2 = ( string )child.Attribute( "Address2" ) ?? "",
                                City = ( string )child.Attribute( "City" ) ?? "",
                                AddressRegion = ( string )child.Attribute( "Region" ) ?? "",
                                PostalCode = ( string )child.Attribute( "PostalCode" ) ?? "",
                                Country = ( string )child.Attribute( "Country" ) ?? ""
                            } );
                    }
                    #endregion


                    #region TextValues - alternate name + ?
                    string textValues = GetRowPossibleColumn( dr, "TextValues" );
                    if ( !string.IsNullOrWhiteSpace( textValues ) )
                    {
                        foreach ( var text in textValues.Split( '|' ) )
                        {
                            if ( !string.IsNullOrWhiteSpace( text ) )
                                item.TextValues.Add( text );
                        }
                    }

                    string alternatesNames = GetRowPossibleColumn( dr, "AlternatesNames" );
                    if ( !string.IsNullOrWhiteSpace( alternatesNames ) )
                    {
                        var xDoc = XDocument.Parse( alternatesNames );
                        foreach ( var child in xDoc.Root.Elements() )
                        {
                            string textValue = ( string )( child.Attribute( "TextValue" ) ) ?? "";
                            if ( !string.IsNullOrWhiteSpace( textValue ) )
                                item.TextValues.Add( textValue );
                        }
                    }

                    string identifiers = GetRowPossibleColumn( dr, "AlternateIdentifiers" );
                    if ( !string.IsNullOrWhiteSpace( identifiers ) )
                    {
                        var xDoc = XDocument.Parse( identifiers );
                        foreach ( var child in xDoc.Root.Elements() )
                        {
                            string title = ( string )( child.Attribute( "Title" ) ) ?? "";
                            string textValue = ( string )( child.Attribute( "TextValue" ) ) ?? "";
                            if ( !string.IsNullOrWhiteSpace( title ) && !string.IsNullOrWhiteSpace( textValue ) )
                                item.TextValues.Add( title + " " + textValue.Replace( "-", "" ) );
                        }
                    }

                    ////properties to add to textvalues
                    string url = GetRowPossibleColumn( dr, "AvailabilityListing", "" );
                    if ( !string.IsNullOrWhiteSpace( url ) )
                        item.TextValues.Add( url );
                    if ( !string.IsNullOrWhiteSpace( item.CredentialRegistryId ) )
                        item.TextValues.Add( item.CredentialRegistryId );
                    item.TextValues.Add( item.Id.ToString() );
                    if ( !string.IsNullOrWhiteSpace( item.CTID ) )
                        item.TextValues.Add( item.CTID );

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
                                Name = ( string )child.Attribute( "Property" ),
                                SchemaName = ( string )child.Attribute( "PropertySchemaName" )
                            };
                            if ( prop.CategoryId == 6 )
                            {
                                item.OrganizationServiceTypes.Add( prop );
                                item.OrganizationServiceTypeIds.Add( prop.Id );
                            }
                            if ( prop.CategoryId == 7 )
                            {
                                item.OrganizationTypes.Add( prop );
                                item.OrganizationTypeIds.Add( prop.Id );
                            }
                            if ( prop.CategoryId == 30 )
                            {
                                item.OrganizationSectorTypes.Add( prop );
                                item.OrganizationSectorTypeIds.Add( prop.Id );
                            }
                            if ( prop.CategoryId == 41 )
                            {
                                item.OrganizationClaimTypes.Add( prop );
                                item.OrganizationClaimTypeIds.Add( prop.Id );
                            }
                            if ( !string.IsNullOrWhiteSpace( ( string )child.Attribute( "Property" ) ) )
                                item.TextValues.Add( ( string )child.Attribute( "Property" ) );
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
                                Name = ( string )child.Attribute( "Property" ),
                                SchemaName = ( string )child.Attribute( "PropertySchemaName" )
                            };

                            item.OrganizationClaimTypes.Add( prop );
                            item.OrganizationClaimTypeIds.Add( prop.Id );

                            if ( !string.IsNullOrWhiteSpace( ( string )child.Attribute( "Property" ) ) )
                                item.TextValues.Add( ( string )child.Attribute( "Property" ) );
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
                                Name = ( string )child.Attribute( "Name" ) ?? "",
                                CodeGroup = ( string )child.Attribute( "CodeGroup" ) ?? "",
                                SchemaName = ( string )child.Attribute( "SchemaName" ) ?? "",
                                CodedNotation = ( string )child.Attribute( "CodedNotation" ) ?? ""
                            };

                            if ( framework.CategoryId == 10 ) item.ReferenceFrameworks.Add( framework );
                        }
                    }
                    #endregion

                    #region AgentRelationships
                    string agentRelations = GetRowPossibleColumn( dr, "AgentRelationships" );
                    if ( !string.IsNullOrWhiteSpace( agentRelations ) )
                    {
                        var xDoc = new XDocument();
                        xDoc = XDocument.Parse( agentRelations );
                        foreach ( var child in xDoc.Root.Elements() )
                            item.AgentRelationships.Add( int.Parse( ( string )child.Attribute( "RelationshipTypeId" ) ) );
                    }
                    #endregion

                    #region QualityAssurance

                    string qualityAssurance = GetRowPossibleColumn( dr, "QualityAssurance" );
                    if ( !string.IsNullOrWhiteSpace( qualityAssurance ) )
                    {
                        var xDoc = XDocument.Parse( qualityAssurance );
                        foreach ( var child in xDoc.Root.Elements() )
                        {
                            string agentName = ( string )child.Attribute( "AgentName" ) ?? "";
                            string relationship = ( string )child.Attribute( "SourceToAgentRelationship" ) ?? "";
                            item.QualityAssurance.Add( new IndexQualityAssurance
                            {
                                AgentRelativeId = int.Parse( child.Attribute( "AgentRelativeId" ).Value ),
                                RelationshipTypeId = int.Parse( child.Attribute( "RelationshipTypeId" ).Value ),
                                SourceToAgentRelationship = ( string )child.Attribute( "SourceToAgentRelationship" ) ?? "",
                                AgentToSourceRelationship = ( string )child.Attribute( "AgentToSourceRelationship" ) ?? "",
                                AgentUrl = ( string )child.Attribute( "AgentUrl" ) ?? "",
                                AgentName = ( string )child.Attribute( "AgentName" ) ?? "",
                                EntityStateId = int.Parse( child.Attribute( "EntityStateId" ).Value ),
                            } );
                            //add phrase. ex Accredited by microsoft
                            if ( !string.IsNullOrWhiteSpace( relationship ) && !string.IsNullOrWhiteSpace( agentName ) )
                                item.TextValues.Add( string.Format( "{0} {1}", relationship, agentName ) );
                        }
                    }
                    #endregion

                    #region Custom Reports
                    int propertyId = 0;
                    if ( item.VerificationProfilesCount > 0 )
                    {
                        if ( GetPropertyId( 59, "orgReport:HasVerificationService", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    }
                    else
                    {
                        if ( GetPropertyId( 59, "orgReport:HasNoVerificationService", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    }
                    if ( item.CostManifestsCount > 0 )
                    {
                        if ( GetPropertyId( 59, "orgReport:HasCostManifest", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    }
                    else
                    {
                        if ( GetPropertyId( 59, "orgReport:HasNoCostManifests", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    }
                    if ( item.ConditionManifestsCount > 0 )
                    {
                        if ( GetPropertyId( 59, "orgReport:HasConditionManifest", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    }
                    else
                    {
                        if ( GetPropertyId( 59, "orgReport:HasNoConditionManifests", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    }
                    if ( item.SubsidiariesCount > 0 )
                    {
                        if ( GetPropertyId( 59, "orgReport:HasSubsidiary", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    }
                    if ( item.DepartmentsCount > 0 )
                    {
                        if ( GetPropertyId( 59, "orgReport:HasDepartment", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    }
                    if ( item.HasIndustriesCount > 0 )
                        if ( GetPropertyId( 59, "orgReport:HasIndustries", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    #endregion

                    list.Add( item );
                }

                LoggingHelper.DoTrace( 2, string.Format( "Organization_SearchForElastic - Complete loaded {0} records", cntr ) );

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
                var item = new CM.OrganizationSummary
                {
                    Id = oi.Id,
                    Name = oi.Name,
                    FriendlyName = oi.FriendlyName,
                    SubjectWebpage = oi.SubjectWebpage,
                    RowId = oi.RowId,
                    Description = oi.Description,
                    CTID = oi.CTID,
                    CredentialRegistryId = oi.CredentialRegistryId,
                    //DateEffective = ci.DateEffective,
                    Created = oi.Created,
                    LastUpdated = oi.LastUpdated,
                };

                if ( oi.ImageURL != null && oi.ImageURL.Trim().Length > 0 )
                    item.ImageUrl = oi.ImageURL;
                else
                    item.ImageUrl = null;

                if ( IsValidDate( oi.Created ) )
                    item.Created = oi.Created;

                if ( IsValidDate( oi.LastUpdated ) )
                    item.LastUpdated = oi.LastUpdated;

                item.IsACredentialingOrg = oi.IsACredentialingOrg;

                //addressess                

                item.Addresses = oi.Addresses.Select( x => new CM.Address
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
                item.AgentType = EntityPropertyManager.FillEnumeration( oi.RowId, CodesManager.PROPERTY_CATEGORY_ORGANIZATION_TYPE );
                item.OrganizationSectorType = EntityPropertyManager.FillEnumeration( oi.RowId, CodesManager.PROPERTY_CATEGORY_ORGANIZATION_SECTORTYPE );
                //item.OrganizationClaimType = EntityPropertyManager.FillEnumeration( oi.RowId, CodesManager.PROPERTY_CATEGORY_CLAIM_TYPE );
                item.ServiceType = EntityPropertyManager.FillEnumeration( item.RowId, CodesManager.PROPERTY_CATEGORY_ORG_SERVICE );

                if ( oi.ReferenceFrameworks != null && oi.ReferenceFrameworks.Count > 0 )
                {
                    item.NaicsResults = Fill_CodeItemResults( oi.ReferenceFrameworks.Where( x => x.CategoryId == 10 ).ToList(), CodesManager.PROPERTY_CATEGORY_NAICS, false, false );
                }
                item.OwnedByResults = Fill_CodeItemResults( oi.OwnedByResults, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false );

                item.OfferedByResults = Fill_CodeItemResults( oi.OfferedByResults, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false );

                item.AsmtsOwnedByResults = Fill_CodeItemResults( oi.AsmtsOwnedByResults, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false );

                item.LoppsOwnedByResults = Fill_CodeItemResults( oi.LoppsOwnedByResults, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false );

                item.AccreditedByResults = Fill_CodeItemResults( oi.AccreditedByResults, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false );

                item.ApprovedByResults = Fill_CodeItemResults( oi.ApprovedByResults, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false );

                item.RecognizedByResults = Fill_CodeItemResults( oi.RecognizedByResults, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false );

                item.RegulatedByResults = Fill_CodeItemResults( oi.RegulatedByResults, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false );

                item.QualityAssurance = Fill_AgentRelationship( oi.QualityAssurance, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false, true );
                list.Add( item );
            }

            return list;
        }
        #endregion

        #region Assessment Elastic Index 
        public static List<AssessmentIndex> Assessment_SearchForElastic( string filter )
        {
            string connectionString = DBConnectionRO();
            var item = new AssessmentIndex();
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
                        string rows = command.Parameters[command.Parameters.Count - 1].Value.ToString();
                    }
                    catch ( Exception ex )
                    {
                        item = new AssessmentIndex();
                        item.Name = "EXCEPTION ENCOUNTERED";
                        item.Description = ex.Message;
                        //item.CredentialTypeSchema = "error";
                        list.Add( item );
                        return list;
                    }
                }
                int costProfilesCount = 0;
                foreach ( DataRow dr in result.Rows )
                {
                    cntr++;
                    if ( cntr % 50 == 0 )
                        LoggingHelper.DoTrace( 2, string.Format( " loading record: {0}", cntr ) );

                    item = new AssessmentIndex();
                    item.Id = GetRowColumn( dr, "Id", 0 );
                    item.NameIndex = cntr * 1000;
                    if ( item.Id == 259 )
                    {

                    }
                    item.Name = GetRowColumn( dr, "Name", "missing" );
                    item.FriendlyName = FormatFriendlyTitle( item.Name );
                    item.Description = GetRowColumn( dr, "Description", "" );
                    string rowId = GetRowColumn( dr, "RowId" );
                    item.RowId = new Guid( rowId );

                    item.SubjectWebpage = GetRowColumn( dr, "SubjectWebpage", "" );

                    item.CodedNotation = GetRowColumn( dr, "IdentificationCode", "" );
                    item.CTID = GetRowPossibleColumn( dr, "CTID", "" );
                    item.CredentialRegistryId = GetRowPossibleColumn( dr, "CredentialRegistryId", "" );

                    item.Organization = GetRowPossibleColumn( dr, "Organization", "" );
                    item.OrgId = GetRowPossibleColumn( dr, "OrgId", 0 );

                    if ( item.Organization.Length > 0 )
                        item.ListTitle = item.Name + " (" + item.Organization + ")";
                    else
                        item.ListTitle = item.Name;

                    var date = GetRowColumn( dr, "DateEffective", "" );
                    if ( IsValidDate( date ) )
                        item.DateEffective = ( DateTime.Parse( date ).ToShortDateString() );
                    else
                        item.DateEffective = "";
                    date = GetRowColumn( dr, "Created", "" );
                    if ( IsValidDate( date ) )
                        item.Created = DateTime.Parse( date );
                    date = GetRowColumn( dr, "LastUpdated", "" );
                    if ( IsValidDate( date ) )
                        item.LastUpdated = DateTime.Parse( date );

                    //don't thinks this is necessary!
                    //item.QARolesCount = GetRowColumn( dr, "QARolesCount", 0 );

                    item.RequiresCount = GetRowColumn( dr, "RequiresCount", 0 );
                    item.RecommendsCount = GetRowColumn( dr, "RecommendsCount", 0 );
                    item.IsRequiredForCount = GetRowColumn( dr, "IsRequiredForCount", 0 );
                    item.IsRecommendedForCount = GetRowColumn( dr, "IsRecommendedForCount", 0 );
                    item.IsAdvancedStandingForCount = GetRowColumn( dr, "IsAdvancedStandingForCount", 0 );
                    item.AdvancedStandingFromCount = GetRowColumn( dr, "AdvancedStandingFromCount", 0 );
                    item.IsPreparationForCount = GetRowColumn( dr, "IsPreparationForCount", 0 );
                    item.PreparationFromCount = GetRowColumn( dr, "PreparationFromCount", 0 );
                    //item.TotalCostCount = GetRowPossibleColumn( dr, "TotalCostCount", 0M );
                    costProfilesCount = GetRowPossibleColumn( dr, "CostProfilesCount", 0 );
                    item.CommonConditionsCount = GetRowPossibleColumn( dr, "CommonConditionsCount", 0 );
                    item.CommonCostsCount = GetRowPossibleColumn( dr, "CommonCostsCount", 0 );
                    item.FinancialAidCount = GetRowPossibleColumn( dr, "FinancialAidCount", 0 );
                    item.ProcessProfilesCount = GetRowPossibleColumn( dr, "ProcessProfilesCount", 0 );
                    item.HasCIP = GetRowPossibleColumn( dr, "HasCIPCount", 0 );
                    //-actual connection type (no credential info)
                    item.ConnectionsList = dr["ConnectionsList"].ToString();
                    //connection type, plus Id, and name of credential
                    item.CredentialsList = dr["CredentialsList"].ToString();
                    //change to use Connections
                    try
                    {
                        string credentialConnections = GetRowPossibleColumn( dr, "Connections" );
                        if ( !string.IsNullOrWhiteSpace( credentialConnections ) )
                        {
                            Connection conn = new Connection();
                            var xDoc = XDocument.Parse( credentialConnections );
                            foreach ( var child in xDoc.Root.Elements() )
                            {
                                conn = new Connection();
                                conn.ConnectionType = ( string )child.Attribute( "ConnectionType" ) ?? "";
                                conn.ConnectionTypeId = int.Parse( child.Attribute( "ConnectionTypeId" ).Value );

                                //do something with counts for this type

                                conn.CredentialId = int.Parse( child.Attribute( "CredentialId" ).Value );
                                if ( conn.CredentialId > 0 )
                                {
                                    //add credential
                                    conn.Credential = ( string )child.Attribute( "CredentialName" ) ?? "";
                                    //??????
                                    conn.CredentialOrgId = int.Parse( child.Attribute( "credOrgid" ).Value );
                                    conn.CredentialOrganization = ( string )child.Attribute( "credOrganization" ) ?? "";
                                }
                                conn.AssessmentId = int.Parse( child.Attribute( "AssessmentId" ).Value );
                                if ( conn.AssessmentId > 0 )
                                {
                                    conn.Assessment = ( string )child.Attribute( "AssessmentName" ) ?? "";
                                    conn.AssessmentOrganizationId = int.Parse( child.Attribute( "asmtOrgid" ).Value );
                                    conn.AssessmentOrganization = ( string )child.Attribute( "asmtOrganization" ) ?? "";
                                }
                                conn.LoppId = int.Parse( child.Attribute( "LearningOpportunityId" ).Value );
                                if ( conn.LoppId > 0 )
                                {
                                    conn.LearningOpportunity = ( string )child.Attribute( "LearningOpportunityName" ) ?? "";
                                    conn.LoppOrganizationId = int.Parse( child.Attribute( "loppOrgid" ).Value );
                                    conn.LearningOpportunityOrganization = ( string )child.Attribute( "loppOrganization" ) ?? "";
                                }

                                item.Connections.Add( conn );
                            }
                        }
                    }
                    catch ( Exception ex )
                    {
                        LoggingHelper.DoTrace( 2, string.Format( " # {0}. Exception on Assessment Connections id: {1}; \r\n{2}", cntr, item.Id, ex.Message ) );
                    }

                    #region AssessesCompetencies

                    string AssessesCompetencies = dr["AssessesCompetencies"].ToString();
                    if ( !string.IsNullOrWhiteSpace( AssessesCompetencies ) )
                    {
                        var xDoc = new XDocument();
                        xDoc = XDocument.Parse( AssessesCompetencies );
                        foreach ( var child in xDoc.Root.Elements() )
                        {
                            var competency = new IndexCompetency
                            {
                                Name = ( string )child.Attribute( "Name" ) ?? "",
                                Description = ( string )child.Attribute( "Description" ) ?? ""
                            };

                            item.AssessesCompetencies.Add( competency );
                        }
                    }

                    #endregion                    

                    #region RequiresCompetencies

                    string RequiresCompetencies = dr["RequiresCompetencies"].ToString();
                    if ( !string.IsNullOrWhiteSpace( RequiresCompetencies ) )
                    {
                        var xDoc = new XDocument();
                        xDoc = XDocument.Parse( RequiresCompetencies );
                        foreach ( var child in xDoc.Root.Elements() )
                        {
                            var competency = new IndexCompetency
                            {
                                Name = ( string )child.Attribute( "Name" ) ?? "",
                                Description = ( string )child.Attribute( "Description" ) ?? ""
                            };

                            item.RequiresCompetencies.Add( competency );
                        }
                    }

                    #endregion                    

                    #region TextValues

                    var textValues = dr["TextValues"].ToString();
                    if ( !string.IsNullOrWhiteSpace( textValues ) )
                    {
                        var xDoc = new XDocument();
                        xDoc = XDocument.Parse( textValues );
                        //item.Addresses = xDoc.Root.Elements().Count();
                        foreach ( var child in xDoc.Root.Elements() )
                        {
                            item.TextValues.Add( ( string )child.Attribute( "TextValue" ) ?? "" );
                        }
                    }
                    //properties to add to textvalues
                    item.AvailableOnlineAt = GetRowPossibleColumn( dr, "AvailableOnlineAt", "" );
                    if ( !string.IsNullOrWhiteSpace( item.AvailableOnlineAt ) )
                        item.TextValues.Add( item.AvailableOnlineAt );
                    item.AvailabilityListing = GetRowPossibleColumn( dr, "AvailabilityListing", "" );
                    if ( !string.IsNullOrWhiteSpace( item.AvailabilityListing ) )
                        item.TextValues.Add( item.AvailabilityListing );
                    string url = GetRowPossibleColumn( dr, "AssessmentExampleUrl", "" );
                    if ( !string.IsNullOrWhiteSpace( url ) )
                        item.TextValues.Add( url );
                    url = GetRowPossibleColumn( dr, "ProcessStandards", "" );
                    if ( !string.IsNullOrWhiteSpace( url ) )
                        item.TextValues.Add( url );
                    url = GetRowPossibleColumn( dr, "ScoringMethodExample", "" );
                    if ( !string.IsNullOrWhiteSpace( url ) )
                        item.TextValues.Add( url );
                    url = GetRowPossibleColumn( dr, "ExternalResearch", "" );
                    if ( !string.IsNullOrWhiteSpace( url ) )
                        item.TextValues.Add( url );
                    //	,base.
                    if ( !string.IsNullOrWhiteSpace( item.CredentialRegistryId ) )
                        item.TextValues.Add( item.CredentialRegistryId );
                    item.TextValues.Add( item.Id.ToString() );
                    item.TextValues.Add( item.CTID );
                    if ( !string.IsNullOrWhiteSpace( item.CodedNotation ) )
                        item.TextValues.Add( item.CodedNotation );
                    #endregion

                    #region SubjectAreas

                    var subjectAreas = dr["SubjectAreas"].ToString();
                    if ( !string.IsNullOrWhiteSpace( subjectAreas ) )
                    {
                        var xDoc = XDocument.Parse( subjectAreas );
                        foreach ( var child in xDoc.Root.Elements() )
                            item.SubjectAreas.Add( child.Attribute( "Subject" ).Value );
                    }

                    #endregion

                    #region Properties
                    //TODO - change this to same as others
                    var methodTypes = dr["AssessmentMethodTypes"].ToString();
                    if ( !string.IsNullOrWhiteSpace( methodTypes ) )
                    {
                        var xDoc = XDocument.Parse( methodTypes );
                        //foreach (var child in xDoc.Root.Elements())
                        //    item.AssessmentMethodTypes.Add(int.Parse((string)child.Attribute("PropertyValueId").Value));
                        foreach ( var child in xDoc.Root.Elements() )
                        {
                            var prop = new IndexProperty
                            {
                                CategoryId = int.Parse( child.Attribute( "CategoryId" ).Value ),
                                Id = int.Parse( child.Attribute( "PropertyValueId" ).Value ),
                                Name = ( string )child.Attribute( "Property" )
                            };
                            item.AssessmentMethodTypes.Add( prop );
                            item.AssessmentMethodTypeIds.Add( prop.Id );
                            if ( !string.IsNullOrWhiteSpace( ( string )child.Attribute( "Property" ) ) )
                                item.TextValues.Add( ( string )child.Attribute( "Property" ) );
                        }
                    }

                    var assessmentUseTypes = dr["AssessmentUseTypes"].ToString();
                    if ( !string.IsNullOrWhiteSpace( assessmentUseTypes ) )
                    {
                        var xDoc = XDocument.Parse( assessmentUseTypes );
                        foreach ( var child in xDoc.Root.Elements() )
                        {
                            var prop = new IndexProperty
                            {
                                CategoryId = int.Parse( child.Attribute( "CategoryId" ).Value ),
                                Id = int.Parse( child.Attribute( "PropertyValueId" ).Value ),
                                Name = ( string )child.Attribute( "Property" )
                            };
                            item.AssessmentUseTypes.Add( prop );
                            item.AssessmentUseTypeIds.Add( prop.Id );
                            if ( !string.IsNullOrWhiteSpace( ( string )child.Attribute( "Property" ) ) )
                                item.TextValues.Add( ( string )child.Attribute( "Property" ) );
                        }
                    }

                    var scoringMethodTypes = dr["ScoringMethodTypes"].ToString();
                    if ( !string.IsNullOrWhiteSpace( scoringMethodTypes ) )
                    {
                        var xDoc = XDocument.Parse( scoringMethodTypes );
                        foreach ( var child in xDoc.Root.Elements() )
                        {
                            var prop = new IndexProperty
                            {
                                CategoryId = int.Parse( child.Attribute( "CategoryId" ).Value ),
                                Id = int.Parse( child.Attribute( "PropertyValueId" ).Value ),
                                Name = ( string )child.Attribute( "Property" )
                            };
                            item.ScoringMethodTypes.Add( prop );
                            item.ScoringMethodTypeIds.Add( prop.Id );
                            if ( !string.IsNullOrWhiteSpace( ( string )child.Attribute( "Property" ) ) )
                                item.TextValues.Add( ( string )child.Attribute( "Property" ) );
                        }
                    }

                    var deliveryMethodTypes = dr["DeliveryMethodTypes"].ToString();
                    if ( !string.IsNullOrWhiteSpace( deliveryMethodTypes ) )
                    {
                        var xDoc = XDocument.Parse( deliveryMethodTypes );
                        foreach ( var child in xDoc.Root.Elements() )
                        {
                            var prop = new IndexProperty
                            {
                                CategoryId = int.Parse( child.Attribute( "CategoryId" ).Value ),
                                Id = int.Parse( child.Attribute( "PropertyValueId" ).Value ),
                                Name = ( string )child.Attribute( "Property" )
                            };
                            item.DeliveryMethodTypes.Add( prop );
                            item.DeliveryMethodTypeIds.Add( prop.Id );
                            if ( !string.IsNullOrWhiteSpace( ( string )child.Attribute( "Property" ) ) )
                                item.TextValues.Add( ( string )child.Attribute( "Property" ) );
                        }
                    }

                    #endregion

                    #region Classifications

                    string classifications = GetRowPossibleColumn( dr, "Classifications" );
                    if ( !string.IsNullOrWhiteSpace( classifications ) )
                    {
                        var xDoc = XDocument.Parse( classifications );
                        foreach ( var child in xDoc.Root.Elements() )
                            item.Classifications.Add( new IndexReferenceFramework
                            {
                                CategoryId = int.Parse( child.Attribute( "CategoryId" ).Value ),
                                ReferenceFrameworkId = int.Parse( child.Attribute( "ReferenceFrameworkId" ).Value ),
                                Name = ( string )child.Attribute( "Name" ) ?? "",
                                CodeGroup = ( string )child.Attribute( "CodeGroup" ) ?? "",
                                SchemaName = ( string )child.Attribute( "SchemaName" ) ?? "",
                                CodedNotation = ( string )child.Attribute( "CodedNotation" ) ?? "",
                            } );
                        //if( classifications.CategoryId == 23 ) item.Classifications.Add( classifications );
                    }

                    #endregion

                    #region QualityAssurance
                    item.Org_QAAgentAndRoles = GetRowPossibleColumn( dr, "Org_QAAgentAndRoles" );
                    string qualityAssurances = GetRowPossibleColumn( dr, "QualityAssurances" );
                    if ( !string.IsNullOrWhiteSpace( qualityAssurances ) )
                    {
                        var xDoc = XDocument.Parse( qualityAssurances );
                        foreach ( var child in xDoc.Root.Elements() )
                            item.QualityAssurances.Add( int.Parse( ( string )child.Attribute( "RelationshipTypeId" ) ) );
                    }

                    string qualityAssurance = GetRowPossibleColumn( dr, "QualityAssurance" );
                    if ( !string.IsNullOrWhiteSpace( qualityAssurance ) )
                    {
                        var xDoc = XDocument.Parse( qualityAssurance );
                        foreach ( var child in xDoc.Root.Elements() )
                        {
                            string agentName = ( string )child.Attribute( "AgentName" ) ?? "";
                            string relationship = ( string )child.Attribute( "SourceToAgentRelationship" ) ?? "";

                            item.QualityAssurance.Add( new IndexQualityAssurance
                            {
                                AgentRelativeId = int.Parse( child.Attribute( "AgentRelativeId" ).Value ),
                                RelationshipTypeId = int.Parse( child.Attribute( "RelationshipTypeId" ).Value ),
                                SourceToAgentRelationship = ( string )child.Attribute( "SourceToAgentRelationship" ) ?? "",
                                AgentToSourceRelationship = ( string )child.Attribute( "AgentToSourceRelationship" ) ?? "",
                                AgentUrl = ( string )child.Attribute( "AgentUrl" ) ?? "",
                                AgentName = ( string )child.Attribute( "AgentName" ) ?? "",
                                EntityStateId = int.Parse( child.Attribute( "EntityStateId" ).Value ),
                            } );
                            //add phrase. ex Accredited by microsoft
                            if ( !string.IsNullOrWhiteSpace( relationship ) && !string.IsNullOrWhiteSpace( agentName ) )
                                item.TextValues.Add( string.Format( "{0} {1}", relationship, agentName ) );
                        }
                    }

                    #endregion

                    #region Addresses
                    var addresses = dr["Addresses"].ToString();
                    if ( !string.IsNullOrWhiteSpace( addresses ) )
                    {
                        //item.Addresses = addresses;
                        var xDoc = new XDocument();
                        xDoc = XDocument.Parse( addresses );
                        //item.AvailableAddresses = xDoc.Root.Elements().Count();
                        foreach ( var child in xDoc.Root.Elements() )
                            item.Addresses.Add( new Address
                            {
                                Latitude = double.Parse( ( string )child.Attribute( "Latitude" ) ),
                                Longitude = double.Parse( ( string )child.Attribute( "Longitude" ) ),
                                Address1 = ( string )child.Attribute( "Address1" ) ?? "",
                                Address2 = ( string )child.Attribute( "Address2" ) ?? "",
                                City = ( string )child.Attribute( "City" ) ?? "",
                                AddressRegion = ( string )child.Attribute( "Region" ) ?? "",
                                PostalCode = ( string )child.Attribute( "PostalCode" ) ?? "",
                                Country = ( string )child.Attribute( "Country" ) ?? ""
                            } );
                    }
                    #endregion

                    #region custom reports

                    int propertyId = 0;
                    if ( !string.IsNullOrWhiteSpace( item.AvailableOnlineAt ) )
                        if ( GetPropertyId( 60, "asmtReport:AvailableOnline", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    if ( item.AssessesCompetencies.Count > 0 )
                        if ( GetPropertyId( 60, "asmtReport:AssessesCompetencies", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    if ( item.RequiresCompetencies.Count > 0 )
                        if ( GetPropertyId( 60, "asmtReport:RequiresCompetencies", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    if ( costProfilesCount > 0 )
                        if ( GetPropertyId( 60, "asmtReport:HasCostProfile", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );

                    var DurationProfileCount = GetRowPossibleColumn( dr, "HasDurationCount", 0 );
                    if ( DurationProfileCount > 0 )
                    {
                        if ( GetPropertyId( 60, "asmtReport:HasDurationProfile", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    }

                    if ( item.CommonConditionsCount > 0 )
                        if ( GetPropertyId( 60, "asmtReport:ReferencesCommonConditions", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    if ( item.CommonCostsCount > 0 )
                        if ( GetPropertyId( 60, "asmtReport:ReferencesCommonCosts", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    if ( item.FinancialAidCount > 0 )
                        if ( GetPropertyId( 60, "asmtReport:FinancialAid", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    if ( item.ProcessProfilesCount > 0 )
                        if ( GetPropertyId( 60, "asmtReport:HasProcessProfile", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    if ( item.HasCIP > 0 )
                        if ( GetPropertyId( 60, "asmtReport:HasCIP", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    list.Add( item );
                }
                #endregion
                LoggingHelper.DoTrace( 2, string.Format( "Assessment_SearchForElastic - Complete loaded {0} records", cntr ) );
                return list;
            }
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
            CodeItem item = CodesManager.GetPropertyBySchema( categoryId, schemaName );
            if ( item != null && item.Id > 0 )
            {
                propertyId = item.Id;
                return true;
            }
            return false;
        }

        public static List<PM.AssessmentProfile> Assessment_MapFromElastic( List<AssessmentIndex> assessments )
        {
            var list = new List<PM.AssessmentProfile>();

            foreach ( var ai in assessments )
            {
                var item = new PM.AssessmentProfile
                {
                    Id = ai.Id,
                    Name = ai.Name,
                    FriendlyName = ai.FriendlyName,
                    Description = ai.Description,
                    RowId = ai.RowId,
                    SubjectWebpage = ai.SubjectWebpage,
                    AvailableOnlineAt = ai.AvailableOnlineAt,
                    CodedNotation = ai.IdentificationCode,
                    CTID = ai.CTID,
                    CredentialRegistryId = ai.CredentialRegistryId,
                    DateEffective = ai.DateEffective,
                    Created = ai.Created,
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

                if ( ai.OrgId > 0 )
                    item.OwningOrganization = new CM.Organization() { Id = ai.OrgId, Name = ai.Organization };
                //addresses
                item.Addresses = ai.Addresses.Select( x => new CM.Address
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


                item.QualityAssurance = Fill_AgentRelationship( ai.QualityAssurance, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false, true, "Assessment" );
                item.Org_QAAgentAndRoles = Fill_AgentRelationship( ai.Org_QAAgentAndRoles, 130, false, false, true, "Organization" );
                item.AssessmentMethodTypes = Fill_CodeItemResults( ai.AssessmentMethodTypes, CodesManager.PROPERTY_CATEGORY_Assessment_Method_Type, false, false );

                item.AssessmentUseTypes = Fill_CodeItemResults( ai.AssessmentUseTypes, CodesManager.PROPERTY_CATEGORY_ASSESSMENT_USE_TYPE, false, false );
                item.ScoringMethodTypes = Fill_CodeItemResults( ai.ScoringMethodTypes, CodesManager.PROPERTY_CATEGORY_Scoring_Method, false, false );
                item.DeliveryMethodTypes = Fill_CodeItemResults( ai.DeliveryMethodTypes, CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE, false, false );
                if ( ai.Classifications != null && ai.Classifications.Count > 0 )
                    item.InstructionalProgramClassification = Fill_CodeItemResults( ai.Classifications.Where( x => x.CategoryId == 23 ).ToList(), CodesManager.PROPERTY_CATEGORY_CIP, false, false );

                item.CredentialsList = Fill_CredentialConnectionsResult( ai.CredentialsList, CodesManager.PROPERTY_CATEGORY_CONNECTION_PROFILE_TYPE );
                item.ListTitle = ai.ListTitle;
                item.Subjects = ai.SubjectAreas;
                list.Add( item );
            }

            return list;
        }

        #endregion

        #region LearningOpp Elastic Index 

        public static List<LearningOppIndex> LearningOpp_SearchForElastic( string filter )
        {
            string connectionString = DBConnectionRO();
            var item = new LearningOppIndex();
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
                        string rows = command.Parameters[command.Parameters.Count - 1].Value.ToString();
                    }
                    catch ( Exception ex )
                    {
                        item = new LearningOppIndex();
                        item.Name = "EXCEPTION ENCOUNTERED";
                        item.Description = ex.Message;
                        //item.CredentialTypeSchema = "error";
                        list.Add( item );
                        return list;
                    }
                }
                int costProfilesCount = 0;
                foreach ( DataRow dr in result.Rows )
                {
                    cntr++;
                    if ( cntr % 100 == 0 )
                        LoggingHelper.DoTrace( 2, string.Format( " loading record: {0}", cntr ) );

                    item = new LearningOppIndex();
                    item.Id = GetRowColumn( dr, "Id", 0 );
                    item.NameIndex = cntr * 1000;
                    item.Name = GetRowColumn( dr, "Name", "missing" );
                    item.FriendlyName = FormatFriendlyTitle( item.Name );
                    item.Description = GetRowColumn( dr, "Description", "" );

                    string rowId = GetRowColumn( dr, "RowId" );
                    item.RowId = new Guid( rowId );

                    item.SubjectWebpage = GetRowColumn( dr, "SubjectWebpage", "" );


                    item.CTID = GetRowPossibleColumn( dr, "CTID", "" );
                    item.CredentialRegistryId = GetRowPossibleColumn( dr, "CredentialRegistryId", "" );


                    item.Organization = GetRowPossibleColumn( dr, "Organization", "" );
                    item.OrgId = GetRowPossibleColumn( dr, "OrgId", 0 );
                    if ( item.Organization.Length > 0 )
                        item.ListTitle = item.Name + " (" + item.Organization + ")";
                    else
                        item.ListTitle = item.Name;

                    var temp = GetRowColumn( dr, "DateEffective", "" );
                    if ( IsValidDate( temp ) )
                        item.DateEffective = DateTime.Parse( temp ).ToShortDateString();
                    else
                        item.DateEffective = "";

                    item.Created = GetRowColumn( dr, "Created", System.DateTime.MinValue );
                    item.LastUpdated = GetRowColumn( dr, "LastUpdated", System.DateTime.MinValue );

                    //competencies. either arbitrarily get all, or if filters exist, only return matching ones
                    item.CompetenciesCount = GetRowPossibleColumn( dr, "CompetenciesCount", 0 );

                    item.RequiresCount = GetRowColumn( dr, "RequiresCount", 0 );
                    item.RecommendsCount = GetRowColumn( dr, "RecommendsCount", 0 );
                    item.IsRequiredForCount = GetRowColumn( dr, "IsRequiredForCount", 0 );
                    item.IsRecommendedForCount = GetRowColumn( dr, "IsRecommendedForCount", 0 );
                    item.IsAdvancedStandingForCount = GetRowColumn( dr, "IsAdvancedStandingForCount", 0 );
                    item.AdvancedStandingFromCount = GetRowColumn( dr, "AdvancedStandingFromCount", 0 );
                    item.IsPreparationForCount = GetRowColumn( dr, "IsPreparationForCount", 0 );
                    item.PreparationFromCount = GetRowColumn( dr, "PreparationFromCount", 0 );
                    //item.TotalCostCount = GetRowPossibleColumn( dr, "TotalCostCount", 0M );
                    costProfilesCount = GetRowPossibleColumn( dr, "CostProfilesCount", 0 );
                    item.CommonConditionsCount = GetRowPossibleColumn( dr, "CommonConditionsCount", 0 );
                    item.CommonCostsCount = GetRowPossibleColumn( dr, "CommonCostsCount", 0 );
                    item.FinancialAidCount = GetRowPossibleColumn( dr, "FinancialAidCount", 0 );
                    item.ProcessProfilesCount = GetRowPossibleColumn( dr, "ProcessProfilesCount", 0 );
                    //-actual connection type (no credential info)
                    item.ConnectionsList = dr["ConnectionsList"].ToString();
                    //connection type, plus Id, and name of credential
                    item.CredentialsList = dr["CredentialsList"].ToString();

                    //item.EntryConditionCount = GetRowColumn(dr, "entryConditionCount", 0);

                    #region TeachesCompetencies

                    string TeachesCompetencies = dr["TeachesCompetencies"].ToString();
                    if ( !string.IsNullOrWhiteSpace( TeachesCompetencies ) )
                    {
                        var xDoc = new XDocument();
                        xDoc = XDocument.Parse( TeachesCompetencies );
                        foreach ( var child in xDoc.Root.Elements() )
                        {
                            var competency = new IndexCompetency
                            {
                                Name = ( string )child.Attribute( "Name" ) ?? "",
                                Description = ( string )child.Attribute( "Description" ) ?? ""
                            };

                            item.TeachesCompetencies.Add( competency );
                        }
                    }

                    #endregion                    

                    #region RequiresCompetencies

                    string RequiresCompetencies = dr["RequiresCompetencies"].ToString();
                    if ( !string.IsNullOrWhiteSpace( RequiresCompetencies ) )
                    {
                        var xDoc = new XDocument();
                        xDoc = XDocument.Parse( RequiresCompetencies );
                        foreach ( var child in xDoc.Root.Elements() )
                        {
                            var competency = new IndexCompetency
                            {
                                Name = ( string )child.Attribute( "Name" ) ?? "",
                                Description = ( string )child.Attribute( "Description" ) ?? ""
                            };

                            item.RequiresCompetencies.Add( competency );
                        }
                    }

                    #endregion                    

                    #region SubjectAreas

                    var subjectAreas = dr["SubjectAreas"].ToString();
                    if ( !string.IsNullOrWhiteSpace( subjectAreas ) )
                    {
                        var xDoc = XDocument.Parse( subjectAreas );
                        foreach ( var child in xDoc.Root.Elements() )
                            item.SubjectAreas.Add( child.Attribute( "Subject" ).Value );
                    }

                    #endregion

                    #region TextValues

                    var textValues = dr["TextValues"].ToString();
                    if ( !string.IsNullOrWhiteSpace( textValues ) )
                    {
                        var xDoc = new XDocument();
                        xDoc = XDocument.Parse( textValues );
                        //item.Addresses = xDoc.Root.Elements().Count();
                        foreach ( var child in xDoc.Root.Elements() )
                        {
                            item.TextValues.Add( ( string )child.Attribute( "TextValue" ) ?? "" );
                        }
                    }
                    //properties to add to textvalues
                    item.AvailableOnlineAt = GetRowPossibleColumn( dr, "AvailableOnlineAt", "" );
                    if ( !string.IsNullOrWhiteSpace( item.AvailableOnlineAt ) )
                        item.TextValues.Add( item.AvailableOnlineAt );
                    string url = GetRowPossibleColumn( dr, "AvailabilityListing", "" );
                    if ( !string.IsNullOrWhiteSpace( url ) )
                        item.TextValues.Add( url );
                    item.CodedNotation = GetRowColumn( dr, "IdentificationCode", "" );
                    if ( !string.IsNullOrWhiteSpace( item.CodedNotation ) )
                        item.TextValues.Add( item.CodedNotation );

                    if ( !string.IsNullOrWhiteSpace( item.CredentialRegistryId ) )
                        item.TextValues.Add( item.CredentialRegistryId );
                    item.TextValues.Add( item.Id.ToString() );
                    item.TextValues.Add( item.CTID );

                    #endregion

                    #region properties

                    var deliveryMethodTypes = dr["DeliveryMethodTypes"].ToString();
                    if ( !string.IsNullOrWhiteSpace( deliveryMethodTypes ) )
                    {
                        var xDoc = XDocument.Parse( deliveryMethodTypes );
                        foreach ( var child in xDoc.Root.Elements() )
                        {
                            var prop = new IndexProperty
                            {
                                CategoryId = int.Parse( child.Attribute( "CategoryId" ).Value ),
                                Id = int.Parse( child.Attribute( "PropertyValueId" ).Value ),
                                Name = ( string )child.Attribute( "Property" )
                            };
                            item.DeliveryMethodTypes.Add( prop );
                            item.DeliveryMethodTypeIds.Add( prop.Id );

                            if ( !string.IsNullOrWhiteSpace( ( string )child.Attribute( "Property" ) ) )
                                item.TextValues.Add( ( string )child.Attribute( "Property" ) );
                        }
                    }
                    var methodTypes = dr["LearningMethodTypes"].ToString();
                    if ( !string.IsNullOrWhiteSpace( methodTypes ) )
                    {
                        var xDoc = XDocument.Parse( methodTypes );
                        foreach ( var child in xDoc.Root.Elements() )
                        {
                            var prop = new IndexProperty
                            {
                                CategoryId = int.Parse( child.Attribute( "CategoryId" ).Value ),
                                Id = int.Parse( child.Attribute( "PropertyValueId" ).Value ),
                                Name = ( string )child.Attribute( "Property" )
                            };
                            item.LearningMethodTypes.Add( prop );
                            item.LearningMethodTypeIds.Add( prop.Id );

                            if ( !string.IsNullOrWhiteSpace( ( string )child.Attribute( "Property" ) ) )
                                item.TextValues.Add( ( string )child.Attribute( "Property" ) );
                        }
                    }
                    #endregion

                    #region Classifications

                    string classifications = GetRowPossibleColumn( dr, "Classifications" );
                    if ( !string.IsNullOrWhiteSpace( classifications ) )
                    {
                        var xDoc = XDocument.Parse( classifications );
                        foreach ( var child in xDoc.Root.Elements() ) item.Classifications.Add( new IndexReferenceFramework
                        {
                            CategoryId = int.Parse( child.Attribute( "CategoryId" ).Value ),
                            ReferenceFrameworkId = int.Parse( child.Attribute( "ReferenceFrameworkId" ).Value ),
                            Name = ( string )child.Attribute( "Name" ) ?? "",
                            CodeGroup = ( string )child.Attribute( "CodeGroup" ) ?? "",
                            SchemaName = ( string )child.Attribute( "SchemaName" ) ?? "",
                            CodedNotation = ( string )child.Attribute( "CodedNotation" ) ?? "",
                        } );
                    }

                    #endregion

                    #region QualityAssurance
                    item.Org_QAAgentAndRoles = GetRowPossibleColumn( dr, "Org_QAAgentAndRoles" );
                    string qualityAssurances = GetRowPossibleColumn( dr, "QualityAssurances" );
                    if ( !string.IsNullOrWhiteSpace( qualityAssurances ) )
                    {
                        var xDoc = XDocument.Parse( qualityAssurances );
                        foreach ( var child in xDoc.Root.Elements() )
                            item.QualityAssurances.Add( int.Parse( ( string )child.Attribute( "RelationshipTypeId" ) ) );
                    }


                    string qualityAssurance = GetRowPossibleColumn( dr, "QualityAssurance" );
                    if ( !string.IsNullOrWhiteSpace( qualityAssurance ) )
                    {
                        var xDoc = XDocument.Parse( qualityAssurance );
                        foreach ( var child in xDoc.Root.Elements() )
                        {
                            string agentName = ( string )child.Attribute( "AgentName" ) ?? "";
                            string relationship = ( string )child.Attribute( "SourceToAgentRelationship" ) ?? "";
                            item.QualityAssurance.Add( new IndexQualityAssurance
                            {
                                AgentRelativeId = int.Parse( child.Attribute( "AgentRelativeId" ).Value ),
                                RelationshipTypeId = int.Parse( child.Attribute( "RelationshipTypeId" ).Value ),
                                SourceToAgentRelationship = ( string )child.Attribute( "SourceToAgentRelationship" ) ?? "",
                                AgentToSourceRelationship = ( string )child.Attribute( "AgentToSourceRelationship" ) ?? "",
                                AgentUrl = ( string )child.Attribute( "AgentUrl" ) ?? "",
                                AgentName = ( string )child.Attribute( "AgentName" ) ?? "",
                                EntityStateId = int.Parse( child.Attribute( "EntityStateId" ).Value ),
                            } );
                            //add phrase. ex Accredited by microsoft
                            if ( !string.IsNullOrWhiteSpace( relationship ) && !string.IsNullOrWhiteSpace( agentName ) )
                                item.TextValues.Add( string.Format( "{0} {1}", relationship, agentName ) );
                        }
                    }

                    #endregion

                    #region Addresses
                    var addresses = dr["Addresses"].ToString();
                    if ( !string.IsNullOrWhiteSpace( addresses ) )
                    {
                        //item.Addresses = addresses;
                        var xDoc = new XDocument();
                        xDoc = XDocument.Parse( addresses );
                        //item.AvailableAddresses = xDoc.Root.Elements().Count();
                        foreach ( var child in xDoc.Root.Elements() )
                            item.Addresses.Add( new Address
                            {
                                Latitude = double.Parse( ( string )child.Attribute( "Latitude" ) ),
                                Longitude = double.Parse( ( string )child.Attribute( "Longitude" ) ),
                                Address1 = ( string )child.Attribute( "Address1" ) ?? "",
                                Address2 = ( string )child.Attribute( "Address2" ) ?? "",
                                City = ( string )child.Attribute( "City" ) ?? "",
                                AddressRegion = ( string )child.Attribute( "Region" ) ?? "",
                                PostalCode = ( string )child.Attribute( "PostalCode" ) ?? "",
                                Country = ( string )child.Attribute( "Country" ) ?? ""
                            } );
                    }
                    #endregion

                    #region custom reports

                    int propertyId = 0;
                    if ( !string.IsNullOrWhiteSpace( item.AvailableOnlineAt ) )
                        if ( GetPropertyId( 61, "loppReport:AvailableOnline", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    if ( item.TeachesCompetencies.Count > 0 )
                        if ( GetPropertyId( 61, "loppReport:TeachesCompetencies", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    if ( item.RequiresCompetencies.Count > 0 )
                        if ( GetPropertyId( 61, "loppReport:RequiresCompetencies", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );

                    var DurationProfileCount = GetRowPossibleColumn( dr, "HasDurationCount", 0 );
                    if ( DurationProfileCount > 0 )
                    {
                        if ( GetPropertyId( 61, "loppReport:HasDurationProfile", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    }

                    if ( costProfilesCount > 0 )
                        if ( GetPropertyId( 61, "loppReport:HasCostProfile", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );

                    if ( item.CommonConditionsCount > 0 )
                        if ( GetPropertyId( 61, "loppReport:ReferencesCommonConditions", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    if ( item.CommonCostsCount > 0 )
                        if ( GetPropertyId( 61, "loppReport:ReferencesCommonCosts", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    if ( item.FinancialAidCount > 0 )
                        if ( GetPropertyId( 61, "loppReport:FinancialAid", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    if ( item.ProcessProfilesCount > 0 )
                        if ( GetPropertyId( 61, "loppReport:HasProcessProfile", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );

                    var HasCIPCount = GetRowPossibleColumn( dr, "HasCIPCount", 0 );
                    if ( HasCIPCount > 0 )
                    {
                        if ( GetPropertyId( 61, "loppReport:HasCIP", ref propertyId ) )
                            item.ReportFilters.Add( propertyId );
                    }

                    list.Add( item );
                }
                #endregion
                LoggingHelper.DoTrace( 2, string.Format( "LearningOpp_SearchForElastic - Complete loaded {0} records", cntr ) );
                return list;
            }
        }

        public static List<PM.LearningOpportunityProfile> LearningOpp_MapFromElastic( List<LearningOppIndex> learningOpps )
        {
            var list = new List<PM.LearningOpportunityProfile>();

            foreach ( var li in learningOpps )
            {
                var item = new PM.LearningOpportunityProfile
                {
                    Id = li.Id,
                    Name = li.Name,
                    FriendlyName = li.FriendlyName,
                    Description = li.Description,
                    RowId = li.RowId,
                    SubjectWebpage = li.SubjectWebpage,
                    AvailableOnlineAt = li.AvailableOnlineAt,
                    CodedNotation = li.IdentificationCode,
                    CTID = li.CTID,
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

                if ( li.OrgId > 0 )
                    item.OwningOrganization = new CM.Organization() { Id = li.OrgId, Name = li.Organization };

                //addressess                

                item.Addresses = li.Addresses.Select( x => new CM.Address
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

                item.Org_QAAgentAndRoles = Fill_AgentRelationship( li.Org_QAAgentAndRoles, 130, false, false, true, "Organization" );
                item.QualityAssurance = Fill_AgentRelationship( li.QualityAssurance, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false, true, "LearningOpportunity" );
                item.CredentialsList = Fill_CredentialConnectionsResult( li.CredentialsList, CodesManager.PROPERTY_CATEGORY_CONNECTION_PROFILE_TYPE );
                item.Subjects = li.SubjectAreas;

                if ( li.Classifications != null && li.Classifications.Count > 0 )
                    item.InstructionalProgramClassification = Fill_CodeItemResults( li.Classifications.Where( x => x.CategoryId == 23 ).ToList(), CodesManager.PROPERTY_CATEGORY_CIP, false, false );

                item.LearningMethodTypes = Fill_CodeItemResults( li.LearningMethodTypes, CodesManager.PROPERTY_CATEGORY_Learning_Method_Type, false, false );
                item.DeliveryMethodTypes = Fill_CodeItemResults( li.DeliveryMethodTypes, CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE, false, false );
                item.ListTitle = li.ListTitle;
                list.Add( item );
            }

            return list;
        }
        #endregion
    }
}
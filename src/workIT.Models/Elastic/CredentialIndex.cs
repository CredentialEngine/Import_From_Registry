using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using workIT.Models.ProfileModels;
using Nest;

namespace workIT.Models.Elastic
{
    public class CredentialIndex
    {
        public CredentialIndex()
        {
			EntityTypeId = 1;
            CompetenciesName = new List<string>();
            CompetenciesTargetNode = new List<string>();
            RelationshipTypes = new List<int>();
            Subjects = new List<IndexSubject>();            
            Competencies = new List<IndexCompetency>();
            Addresses = new List<Address>();
            OccupationCodeGroups = new List<string>();
            OccupationCodeNotations = new List<string>();
            IndustryCodeGroups = new List<string>();
            IndustryCodeNotations = new List<string>();
            QualityAssurance = new List<IndexQualityAssurance>();

        }
        public int EntityTypeId { get; set; } = 1;
        public int Id { get; set; }
        public int NameIndex { get; set; }
        /// <summary>
        /// Friendly name is used for urls
        /// </summary>
		public string FriendlyName { get; set; }

		public string Name { get; set; }
		public string Description { get; set; }
		

        public Guid RowId { get; set; }
        
        public int EntityStateId { get; set; }

        public int OwnerOrganizationId { get; set; }
        public string OwnerOrganizationName { get; set; }

        public string DateEffective { get; set; }

        public DateTime Created { get; set; }

        public DateTime LastUpdated { get; set; }

        public string CredentialType { get; set; }

        public int CredentialTypeId { get; set; }

        public string CredentialTypeSchema { get; set; }

        public decimal TotalCost { get; set; }

        public string CTID { get; set; }

        public string CredentialRegistryId { get; set; }

        public string SubjectWebpage { get; set; }

        //  public string ImageUrl { get; set; } //image URL

       // public string CredentialStatusType { get; set; }

        public List<string> Keyword { get; set; }
        //Keep either Connectionslist or this

        public int EstimatedTimeToEarn { get; set; }

        public bool IsAQACredential { get; set; }

        //public bool HasQualityAssurance { get; set; }

        public int LearningOppsCompetenciesCount { get; set; }

        public int AssessmentsCompetenciesCount { get; set; }

        public List<IndexQualityAssurance> QualityAssurance { get; set; } = new List<IndexQualityAssurance>();
       // public string QARolesResults { get; set; }
        public string AgentAndRoles { get; set; }

        //QA on owning org
        public string Org_QARolesList { get; set; }

        //QAAgentAndRoles - List actual orgIds and names for roles
        public string Org_QAAgentAndRoles { get; set; }

        public List<int> RelationshipTypes { get; set; }

        #region counts
        //public int QARolesCount { get; set; }

        public int HasPartCount { get; set; }

        public int IsPartOfCount { get; set; }

        public int RequiresCount { get; set; }

        public int RecommendsCount { get; set; }

        public int RequiredForCount { get; set; }

        public int IsRecommendedForCount { get; set; }

        public int RenewalCount { get; set; }

        public int IsAdvancedStandingForCount { get; set; }

        public int AdvancedStandingFromCount { get; set; }

        public int PreparationForCount { get; set; }

        public int PreparationFromCount { get; set; }

        public int EntryConditionCount { get; set; }
        public int CommonCostsCount { get; set; }
        public int CommonConditionsCount { get; set; }
        //public decimal TotalCostCount { get; set; }
        public int FinancialAidCount { get; set; }
        public int EmbeddedCredentialsCount { get; set; }

        public int RequiredAssessmentsCount { get; set; }
        public int RequiredCredentialsCount { get; set; }
        public int RequiredLoppCount { get; set; }
        public int RecommendedAssessmentsCount { get; set; }
        public int RecommendedCredentialsCount { get; set; }
        public int RecommendedLoppCount { get; set; }
        public int BadgeClaimsCount { get; set; }
        public int ProcessProfilesCount { get; set; }
        public int RevocationProfilesCount { get; set; }

        public int HasOccupationsCount { get; set; }
        public int HasIndustriesCount { get; set; }

        #endregion
        public List<Connection> Connections { get; set; } = new List<Connection>();

        public string LevelsResults { get; set; }



        //-actual connection type (no credential info)
        public string ConnectionsList { get; set; }
        //connection type, plus Id, and name of credential
        public string CredentialsList { get; set; }

        public string HasPartsList { get; set; }

        public string IsPartOfList { get; set; }

        //credential name plus organization
        public string ListTitle { get; set; }

        //public string Subject { get; set; }
        public List<IndexSubject> Subjects { get; set; }

        //public string NaicsResults { get; set; }

        //public string IndustryOtherResults { get; set; }

        //public string OccupationResults { get; set; }
        //public string OccupationOtherResults { get; set; }

        public List<IndexReferenceFramework> Industries { get; set; } = new List<IndexReferenceFramework>();

        public List<IndexReferenceFramework> Occupations { get; set; }= new List<IndexReferenceFramework>();

        public List<IndexCompetency> Competencies { get; set; }
        public List<string> CompetenciesName { get; set; }
        public List<string> CompetenciesTargetNode { get; set; }

        //public Address Addresses { get; set; }
        public List<Address> Addresses { get; set; }
        public List<string> OccupationCodeGroups { get; set; }
        public List<string> OccupationCodeNotations { get; set; }
        public List<string> IndustryCodeGroups { get; set; }
        public List<string> IndustryCodeNotations { get; set; }

        public List<string> InLanguage { get; set; } = new List<string>();

        public int NumberOfCostProfileItems { get; set; }

        public bool HasVerificationType_Badge { get; set; }
        //public string AlternateName { get; set; }
		public List<string> AlternateNames { get; set; } = new List<string>();

        public List<string> TextValues { get; set; } = new List<string>();
        public List<string> CodedNotation { get; set; } = new List<string>();

        //public List<int> PropertyValues { get; set; }
        public List<int> AudienceLevelTypeIds { get; set; } = new List<int>();
        public List<int> ReportFilters { get; set; } = new List<int>();
        //public List<IndexProperty> AudienceLevelTypes { get; set; } = new List<IndexProperty>();


    }

    }


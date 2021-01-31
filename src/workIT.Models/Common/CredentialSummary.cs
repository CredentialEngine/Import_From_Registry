using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.ProfileModels;

namespace workIT.Models.Common
{
	public class CredentialSummary : BaseObject
	{
		public CredentialSummary()
		{
			AgentAndRoles = new AgentRelationshipResult();
			CredentialTypeSchema = "";
			IndustryResults = new CodeItemResult();
			IndustryOtherResults = new CodeItemResult();
			OccupationResults = new CodeItemResult();
			OccupationOtherResults = new CodeItemResult();
			AudienceLevelsResults = new CodeItemResult();
			AudienceTypesResults = new CodeItemResult();
			QARolesResults = new CodeItemResult();
			ConnectionsList = new CodeItemResult();
			CredentialsList = new CredentialConnectionsResult();
			HasPartsList = new CredentialConnectionsResult();
			IsPartOfList = new CredentialConnectionsResult();
			Addresses = new List<Address>();
			EstimatedTimeToEarn = new List<DurationProfile>();
			EstimatedCost = new List<CostProfile>();
			Subjects = new List<string>();

		}
		public string Name { get; set; }
		public string FriendlyName { get; set; }
		public int EntityStateId { get; set; }
		public string ListTitle { get; set; }
		//public int ResultNumber { get; set; }
		
		public string Description { get; set; }
		public string Version { get; set; }
		public string LatestVersionUrl { get; set; }
		public string PreviousVersion { get; set; }
		public string ProcessStandards { get; set; }
		public string AvailableOnlineAt { get; set; }
		
		public string SubjectWebpage { get; set; }
		public string ImageUrl { get; set; }

		public string CredentialType { get; set; }
		public string CredentialTypeSchema { get; set; }
		public string CTID { get; set; }
		public decimal TotalCost { get; set; }
		public int CostProfileCount { get; set; }
		public string CredentialRegistryId { get; set; }
		//public int ManagingOrgId { get; set; }
		//public string ManagingOrganization { get; set; }

		public int OwnerOrganizationId { get; set; }
		public string OwnerOrganizationName { get; set; }
		public string OrganizationName
		{
			get
			{
				if ( OwnerOrganizationName != null  )
					return OwnerOrganizationName;
				else
					return "";
			}
		}
		public string PrimaryOrganizationCTID { get; set; }
		public int LearningOppsCompetenciesCount { get; set; }
		public int AssessmentsCompetenciesCount { get; set; }
		public int QARolesCount { get; set; }

		//credential connections
		public CredentialConnectionsResult HasPartsList { get; set; }
		public CredentialConnectionsResult IsPartOfList { get; set; }

		//should just have one total?
		public int RequiredAssessmentsCount { get; set; }
		public int RecommendedAssessmentsCount { get; set; }
		public int RequiredLoppCount { get; set; }
		public int RecommendedLoppCount { get; set; }
		//
		public int HoldersProfileCount { get; set; }
		public string HoldersProfileSummary { get; set; }
		public int EarningsProfileCount { get; set; }
		public string EarningsProfileSummary { get; set; }
		public int EmploymentOutcomeProfileCount { get; set; }
		public string EmploymentOutcomeProfileSummary { get; set; }

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
        public int CommonCostsCount { get; set; }
        public int CommonConditionsCount { get; set; }
       // public decimal TotalCostCount { get; set; }
        public int FinancialAidCount { get; set; }
        public List<string> Subjects { get; set; }
		public List<string> DegreeConcentration { get; set; } = new List<string>();
		public string HasDegreeConcentation
		{
			get
			{
				if ( DegreeConcentration == null || DegreeConcentration.Count == 0 )
					return "";
				else
				{
					var concentration = String.Join(";", DegreeConcentration);

					return concentration;
				}
			}
		}
		public CodeItemResult IndustryResults { get; set; }
		public CodeItemResult IndustryOtherResults { get; set; }
		public CodeItemResult OccupationResults { get; set; }
		public CodeItemResult OccupationOtherResults { get; set; }
		public CodeItemResult InstructionalProgramClassification { get; set; } = new CodeItemResult();
		public CodeItemResult AudienceLevelsResults { get; set; }
		public CodeItemResult AudienceTypesResults { get; set; }
        //QA roles on credential
		public CodeItemResult QARolesResults { get; set; }
        public AgentRelationshipResult AgentAndRoles { get; set; }
		public CodeItemResult AssessmentDeliveryType { get; set; } = new CodeItemResult();
		public CodeItemResult LearningDeliveryType { get; set; } = new CodeItemResult();
		//QA roles on owing org
		public CodeItemResult Org_QARolesResults { get; set; }
        //actual roles and orgs with QA on owning org
        public AgentRelationshipResult Org_QAAgentAndRoles { get; set; } = new AgentRelationshipResult();
		
		public CodeItemResult ConnectionsList { get; set; }
		public CredentialConnectionsResult CredentialsList { get; set; }

		public List<Address> Addresses { get; set; }
		public bool IsAQACredential { get; set; }
		public bool HasQualityAssurance { get; set; }

		public List<DurationProfile> EstimatedTimeToEarn { get; set; }

		public int NumberOfCostProfileItems { get; set; }
		public List<CostProfile> EstimatedCost { get; set; }
		public bool HasVerificationType_Badge { get; set; }
		
	}
	public class CodeItemResult
	{
		public CodeItemResult()
		{
			HasAnIdentifer = true;
			Results = new List<CodeItem>();
		}
		public int CategoryId { get; set; }
		public bool HasAnIdentifer { get; set; }
		public bool UsingCodedNotation { get; set; }

		public List<CodeItem> Results { get; set; }
	}
	public class CredentialConnectionsResult
	{
		public CredentialConnectionsResult()
		{
			Results = new List<CredentialConnectionItem>();
		}
		public int CategoryId { get; set; }

		public List<CredentialConnectionItem> Results { get; set; }
	}
	public class CredentialConnectionItem
	{
		public int ConnectionId { get; set; }

		public string Connection { get; set; }

		public int CredentialId { get; set; }

		public string Credential { get; set; }
        public string CredentialOwningOrg { get; set; }
        public int CredentialOwningOrgId { get; set; }


    }
    public class AgentRelationshipResult
	{
		public AgentRelationshipResult()
		{
			HasAnIdentifer = true;
			Results = new List<AgentRelationship>();
		}
		public int CategoryId { get; set; }
		public bool HasAnIdentifer { get; set; }

		public List<AgentRelationship> Results { get; set; }
	} 
    public class TargetAssertionResult
    {
        public TargetAssertionResult()
        {
            HasAnIdentifer = true;
            Results = new List<TargetAssertion>();
        }
        public int CategoryId { get; set; }
        public bool HasAnIdentifer { get; set; }

        public List<TargetAssertion> Results { get; set; }
    }
}

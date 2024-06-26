using System.Collections.Generic;

namespace workIT.Models.Elastic
{
	public class CredentialIndex : BaseIndex, IIndex
	{
		public CredentialIndex()
		{
			EntityTypeId = 1;
			
			Subjects = new List<IndexSubject>();
			Competencies = new List<IndexCompetency>();
			//Addresses = new List<Address>();
			//QualityAssurance = new List<IndexQualityAssurance>();

		}
		//      public int EntityTypeId { get; set; } = 1;
		//      public int Id { get; set; }
		//public Guid RowId { get; set; }
		//public string CTID { get; set; }
		//public int EntityStateId { get; set; }
		//public DateTime IndexLastUpdated { get; set; } = DateTime.Now;

		//public string Name { get; set; }
		public int NameIndex { get; set; }
		/// <summary>
		/// Friendly name is used for urls
		/// </summary>
		//public string FriendlyName { get; set; }

		//public string Description { get; set; }

		//public string SubjectWebpage { get; set; }

		public int OwnerOrganizationId
		{
			get { return base.PrimaryOrganizationId; }
			set { this.PrimaryOrganizationId = value; }
		}
		public string OwnerOrganizationName
		{
			get { return base.PrimaryOrganizationName; }
			set { this.PrimaryOrganizationName = value; }
		}
		public string OwnerOrganizationCTID
		{
			get { return PrimaryOrganizationCTID; }
			set { this.PrimaryOrganizationCTID = value; }
		}

		//MP Created a separate common class called BaseIndex
		//public string CredentialRegistryId { get; set; }
		//public List<string> InLanguage { get; set; } = new List<string>();
		//public DateTime Created { get; set; }

		//      //define this to be EntityLastUpdated
		//      public DateTime LastUpdated { get; set; }
		//public DateTime EntityLastUpdated { get; set; }

		public string DateEffective { get; set; }
		public string CredentialType { get; set; }

		public int CredentialTypeId { get; set; }

		public string CredentialTypeSchema { get; set; }
		public string CredentialStatus { get; set; }
		public int CredentialStatusId { get; set; }
		public string ImageURL { get; set; }
		public decimal TotalCost { get; set; }
		public int CostProfileCount { get; set; }

		public int NumberOfCostProfileItems { get; set; }
		public string AvailableOnlineAt { get; set; }
		public bool IsAvailableOnline
		{
			get
			{
				return !string.IsNullOrWhiteSpace( AvailableOnlineAt ) && AvailableOnlineAt.Length > 10;
			}
		}

		//public List<string> Keyword { get; set; } = new List<string>();
		//Keep either Connectionslist or this

		public int EstimatedTimeToEarn { get; set; }

		public bool IsAQACredential { get; set; }

		public bool IsNonCredit { get; set; }

		//public bool HasQualityAssurance { get; set; }
		public int AssessmentsCompetenciesCount { get; set; }
		public int LearningOppsCompetenciesCount { get; set; }

		public int RequiresCompetenciesCount { get; set; }

		//public List<AgentRelationshipForEntity> AgentRelationshipsForEntity { get; set; } = new List<AgentRelationshipForEntity>();
		//public List<IndexQualityAssurance> QualityAssurance { get; set; } = new List<IndexQualityAssurance>();
		//public List<QualityAssurancePerformed> QualityAssurancePerformed { get; set; } = new List<QualityAssurancePerformed>();
		// public string QARolesResults { get; set; }
		public string AgentAndRoles { get; set; }

		//QA on owning org
		public string Org_QARolesList { get; set; }

		//QAAgentAndRoles - List actual orgIds and names for roles
		public string Org_QAAgentAndRoles { get; set; }

		//public List<int> RelationshipTypes { get; set; } = new List<int>();

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

		public int AggregateDataProfileCount { get; set; }
		public string AggregateDataProfileSummary { get; set; }
		public int DataSetProfileCount { get; set; }
		public int JurisdictionProfilesCount { get; set; }
		
		//
		public int HoldersProfileCount { get; set; }
		public string HoldersProfileSummary { get; set; }
		public int EarningsProfileCount { get; set; }
		public string EarningsProfileSummary { get; set; }
		public int EmploymentOutcomeProfileCount { get; set; }
		public string EmploymentOutcomeProfileSummary { get; set; }
		//
		public int RevocationProfilesCount { get; set; }
		public bool HasOccupations { get; set; }
		public bool HasIndustries { get; set; }
		public bool HasInstructionalPrograms { get; set; }
		public int HasTransferValueProfilesCount { get; set; }
		#endregion
		public List<Connection> Connections { get; set; } = new List<Connection>();

		//public string LevelsResults { get; set; }
		//public string TypesResults { get; set; }


		//-actual connection type (no credential info)
		public string ConnectionsList { get; set; }
		//connection type, plus Id, and name of credential
		public string CredentialsList { get; set; }

		public string HasPartsList { get; set; }

		public string IsPartOfList { get; set; }

		//credential name plus organization
		//public string ListTitle { get; set; }

		public bool HasSubjects { get; set; }
		public List<IndexSubject> Subjects { get; set; }
		//public List<string> SubjectAreas { get; set; }

		//public List<IndexReferenceFramework> Industries { get; set; } = new List<IndexReferenceFramework>();

		//public List<IndexReferenceFramework> Occupations { get; set; } = new List<IndexReferenceFramework>();
		//public List<IndexReferenceFramework> InstructionalPrograms { get; set; } = new List<IndexReferenceFramework>();

		//public List<Address> Addresses { get; set; }

		//public List<string> AddressLocations { get { return Addresses.Select( x => x.AddressRegion ).ToList(); } }

		//public List<string> OccupationCodeGroups { get; set; }
		//public List<string> OccupationCodeNotations { get; set; }
		//public List<string> IndustryCodeGroups { get; set; }
		//public List<string> IndustryCodeNotations { get; set; }


		public bool HasVerificationType_Badge { get; set; }
		//public string AlternateName { get; set; }
		public List<string> AlternateNames { get; set; } = new List<string>();


		/// <summary>
		/// Source will be Entity.SearchIndex, including:
		/// Credential Type, Audience Level Type, NAICS        
		/// ONET Occupation Codes
		/// Classification of Instructional Programs( CIP)
		/// Competency Item
		/// Subject
		/// Keyword
		/// Credential Status Type
		/// PLUS:
		/// - Alternate name
		/// - AlternateIdentifiers
		/// - Id
		/// - CredentialRegistryId
		/// - CTID
		/// </summary>
		//public List<string> TextValues { get; set; } = new List<string>();
		/// <summary>
		/// Use for specialilzed queries, where want to limit to 'premium' text values, rather than all
		/// The question remains how to take advantage of these in searches? Perhaps giving an higher wieght.
		/// </summary>
		public List<string> PremiumValues { get; set; } = new List<string>();
		

		//public List<int> PropertyValues { get; set; }
		public List<int> AudienceLevelTypeIds { get; set; } = new List<int>();
		public List<int> AudienceTypeIds { get; set; } = new List<int>();
		public List<int> AsmntDeliveryMethodTypeIds { get; set; } = new List<int>();
		public List<int>LearningDeliveryMethodTypeIds { get; set; } = new List<int>();
		public List<IndexProperty> CredentialProperties { get; set; } = new List<IndexProperty>();
	}

}


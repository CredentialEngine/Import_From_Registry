using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Elastic
{
    public class LearningOppIndex: BaseIndex, IIndex
	{
        public LearningOppIndex()
        {
            TeachesCompetencies = new List<IndexCompetency>();
            RequiresCompetencies = new List<IndexCompetency>();
            RelationshipTypes = new List<int>();
            SubjectAreas = new List<string>();
            Classifications = new List<IndexReferenceFramework>();
            Addresses = new List<Address>();
        }
        //public int Id { get; set; }
        //public DateTime IndexLastUpdated { get; set; } = DateTime.Now;
        //public string Name { get; set; }
        //public string FriendlyName { get; set; }
        //public int EntityStateId { get; set; }
        public string DateEffective { get; set; }

        //public Guid RowId { get; set; }
        //public string CTID { get; set; }
        public List<IndexCompetency> TeachesCompetencies { get; set; }
        public List<IndexCompetency> RequiresCompetencies { get; set; }
        //public List<string> InLanguage { get; set; } = new List<string>();

        // public string AssessmentMethodType { get; set; }
        public string ProcessStandards { get; set; }
        public string ProcessStandardsDescription { get; set; }
         public int QARolesCount { get; set; }

        public int HasPartCount { get; set; }

        //public string SubjectWebpage { get; set; }
        public List<int> RelationshipTypes { get; set; }
        //public List<JurisdictionProfile> JurisdictionAssertions { get; set; }


        //public string Description { get; set; }

        public int NameIndex { get; set; }
        public string IdentificationCode { get; set; }
		public int OwnerOrganizationId
		{
			get { return PrimaryOrganizationId; }
			set { this.PrimaryOrganizationId = value; }
		}
		public string OwnerOrganizationName
		{
			get { return PrimaryOrganizationName; }
			set { this.PrimaryOrganizationName = value; }
		}
		public string OwnerOrganizationCTID
		{
			get { return PrimaryOrganizationCTID; }
			set { this.PrimaryOrganizationCTID = value; }
		}

		//public DateTime Created { get; set; }
		//public DateTime LastUpdated { get; set; }
		public string AvailableOnlineAt { get; set; }
		public bool IsAvailableOnline
		{
			get
			{
				if ( !string.IsNullOrWhiteSpace( AvailableOnlineAt ) && AvailableOnlineAt.Length > 10 )
					return true;
				else
					return false;
			}
		}
		public decimal TotalCost { get; set; }
        //public string CredentialRegistryId { get; set; }
		/// <summary>
		/// Source will be Entity.SearchIndex:
		/// Audience Level Type,         
		/// ONET Occupation Codes
		/// Classification of Instructional Programs( CIP)
		/// Competency Item
		/// Subject
		/// Keyword
		/// </summary>
		//public List<string> TextValues { get; set; } = new List<string>();
		public List<string> PremiumValues { get; set; } = new List<string>();
		public bool HasSubjects { get; set; }
		//public new List<string> SubjectAreas { get; set; }

		public List<int> LearningMethodTypeIds { get; set; } = new List<int>();
        public List<int> DeliveryMethodTypeIds { get; set; } = new List<int>();
        //public List<IndexProperty> LearningMethodTypes { get; set; } = new List<IndexProperty>();
        //public List<IndexProperty> DeliveryMethodTypes { get; set; } = new List<IndexProperty>();
		public List<IndexProperty> LoppProperties { get; set; } = new List<IndexProperty>();
		public List<IndexReferenceFramework> Industries { get; set; } = new List<IndexReferenceFramework>();

		public List<IndexReferenceFramework> Occupations { get; set; } = new List<IndexReferenceFramework>();
		public List<IndexReferenceFramework> Classifications { get; set; }
        //QAAgentAndRoles - List actual orgIds and names for roles
        public string Org_QAAgentAndRoles { get; set; }
        //public List<int> QualityAssurances { get; set; } = new List<int>();
        //public List<IndexQualityAssurance> QualityAssurance { get; set; }

		//public List<AgentRelationshipForEntity> AgentRelationshipsForEntity { get; set; } = new List<AgentRelationshipForEntity>();
		public string CodedNotation { get; set; }
     
        public int CompetenciesCount { get; set; }
        public string ListTitle { get; set; }
        public List<int> AudienceTypeIds { get; set; } = new List<int>();
		public List<int> AudienceLevelTypeIds { get; set; } = new List<int>();
		public List<int> ReportFilters { get; set; } = new List<int>();

		//-actual connection type (no credential info)
		public string ConnectionsList { get; set; }
		//replace CredentialsList with Connection class - handle all types
		public List<Connection> Connections { get; set; } = new List<Connection>();
		//connection type, plus Id, and name of credential
		public string CredentialsList { get; set; }
		public List<QualityAssurancePerformed> QualityAssurancePerformed { get; set; } = new List<QualityAssurancePerformed>();

		//condition profiles - future

		// public int EntryConditionCount { get; set; }

		//  public int AvailableAddresses { get; set; }
		//public int AddressesCount { get; set; }

		public List<Address> Addresses { get; set; } = new List<Elastic.Address>();
		public string TypesResults { get; set; }
		public List<string> Keyword { get; set; } = new List<string>();
		public bool HasOccupations { get; set; }
		public bool HasIndustries { get; set; }
		public bool HasInstructionalPrograms { get; set; }
		#region counts
		//connections not condition profiles
		public int RequiresCount { get; set; }

        public int RecommendsCount { get; set; }
        public int AdvancedStandingFromCount { get; set; }
        public int IsAdvancedStandingForCount { get; set; }
        public int IsPreparationForCount { get; set; }
        public int IsRecommendedForCount { get; set; }
        public int IsRequiredForCount { get; set; }
        public int PreparationFromCount { get; set; }
        public int CommonCostsCount { get; set; }
        public int CommonConditionsCount { get; set; }
        //public decimal TotalCostCount { get; set; }
        public int FinancialAidCount { get; set; }
        public int ProcessProfilesCount { get; set; }


        #endregion

    }
}

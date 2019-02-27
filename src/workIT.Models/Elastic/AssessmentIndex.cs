using System.Collections.Generic;

namespace workIT.Models.Elastic
{
	public class AssessmentIndex: BaseIndex, IIndex
    {
        public AssessmentIndex()
        {
            AssessesCompetencies = new List<IndexCompetency>();
            RequiresCompetencies = new List<IndexCompetency>();
              
            RelationshipTypes = new List<int>();
            SubjectAreas = new List<string>();
           // SubjectAreas1 = new List<string>();
            
            QualityAssurance = new List<IndexQualityAssurance>();
            Addresses = new List<Address>();
        }

		public int NameIndex { get; set; }
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
		//public int EntityTypeId { get; set; } = 3;
		//public int Id { get; set; }
		//public Guid RowId { get; set; }
		//public string CTID { get; set; }
		//public int EntityStateId { get; set; }
		//public DateTime IndexLastUpdated { get; set; } = DateTime.Now;

		// public string Name { get; set; }

		//public string FriendlyName { get; set; }
		//public string Description { get; set; }
		//public string SubjectWebpage { get; set; }
		//public string CredentialRegistryId { get; set; }

		//public DateTime Created { get; set; }
		//public DateTime LastUpdated { get; set; }

		public string DateEffective { get; set; }
        
        
        public List<IndexCompetency> AssessesCompetencies { get; set; }
        public List<IndexCompetency> RequiresCompetencies { get; set; }


        public string AssessmentUseType { get; set; }
        // public string AssessmentMethodType { get; set; }
        public string ProcessStandards { get; set; }
        public string ProcessStandardsDescription { get; set; }

        //public string ScoringMethodType { get; set; }
        public string ScoringMethodDescription { get; set; }
        public string ScoringMethodExample { get; set; }
        public string ScoringMethodExampleDescription { get; set; }

        public List<int> RelationshipTypes { get; set; }
        //public List<JurisdictionProfile> JurisdictionAssertions { get; set; }

        public string IdentificationCode { get; set; }


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
        public string AvailabilityListing { get; set; }
       
        

		/// <summary>
		/// Source will be Entity.SearchIndex
		/// Audience Level Type,        
		/// Classification of Instructional Programs( CIP)
		/// Competency Item
		/// Subject
		/// Keyword
		/// </summary>
		//public List<string> TextValues { get; set; } = new List<string>();
		public List<string> PremiumValues { get; set; } = new List<string>();
		public List<int> ReportFilters { get; set; } = new List<int>();
        //public string Subject { get; set; }
        public List<string> SubjectAreas { get; set; }
        //SubjectAreas1 was the same as Classifications
        // public List<string> SubjectAreas1 { get; set; }
        
        public List<int> AssessmentMethodTypeIds { get; set; } = new List<int>();
        public List<int> AssessmentUseTypeIds { get; set; } = new List<int>();
        public List<int> ScoringMethodTypeIds { get; set; } = new List<int>();
        public List<int> AudienceTypeIds { get; set; } = new List<int>();
		public List<int> DeliveryMethodTypeIds { get; set; } = new List<int>();

  //      public List<IndexProperty> AssessmentMethodTypes { get; set; } = new List<IndexProperty>();
  //      public List<IndexProperty> AssessmentUseTypes { get; set; } = new List<IndexProperty>();
  //      public List<IndexProperty> ScoringMethodTypes { get; set; } = new List<IndexProperty>();
  //      public List<IndexProperty> DeliveryMethodTypes { get; set; } = new List<IndexProperty>();
		//public List<IndexProperty> AudienceTypes { get; set; } = new List<IndexProperty>();
		public List<IndexProperty> AssessmentProperties { get; set; } = new List<IndexProperty>();

		public List<IndexReferenceFramework> Industries { get; set; } = new List<IndexReferenceFramework>();

		public List<IndexReferenceFramework> Occupations { get; set; } = new List<IndexReferenceFramework>();
		public List<IndexReferenceFramework> Classifications { get; set; } = new List<IndexReferenceFramework>();

        //QAAgentAndRoles - List actual orgIds and names for roles
        public string Org_QAAgentAndRoles { get; set; }
        //public List<int> QualityAssurances { get; set; } = new List<int>();

		//public List<AgentRelationshipForEntity> AgentRelationshipsForEntity { get; set; } = new List<AgentRelationshipForEntity>();
		public List<IndexQualityAssurance> QualityAssurance { get; set; }
       // public string Org_QAAgentAndRoles { get; set; }

        public string CodedNotation { get; set; }
        
        //public int AddressesCount { get; set; }
        public string ListTitle { get; set; }
        
        public List<Address> Addresses { get; set; } = new List<Elastic.Address>();

        #region counts
        //connections
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
		public bool HasOccupations { get; set; }
		public bool HasIndustries { get; set; }
		public bool HasInstructionalPrograms { get; set; }

        //-actual connection type (no credential info)
        public string ConnectionsList { get; set; }
        //connection type, plus Id, and name of credential
        public string CredentialsList { get; set; }
        //replace CredentialsList with Connection class - handle all types
        public List<Connection> Connections { get; set; } = new List<Connection>();

        //public int QARolesCount { get; set; }


        //condition profiles - future

        public int EntryConditionCount { get; set; }
        public string TypesResults { get; set; }

        public List<string> Keyword { get; set; } = new List<string>();
        #endregion

    }
}

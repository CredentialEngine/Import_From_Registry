using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.Common;

namespace workIT.Models.ProfileModels
{
    [Serializable]
    public class LearningOpportunityProfile : BaseProfile
	{
		public LearningOpportunityProfile()
		{
			OwningOrganization = new Organization();

			EstimatedCost = new List<CostProfile>();
			FinancialAssistanceOLD = new List<FinancialAlignmentObject>();
			EstimatedDuration = new List<DurationProfile>();
			DeliveryType = new Enumeration();
            InstructionalProgramType = new Enumeration();
			HasPart = new List<LearningOpportunityProfile>();
			IsPartOf = new List<LearningOpportunityProfile>();
			//IsPartOfConditionProfile = new List<ConditionProfile>();
			OrganizationRole = new List<OrganizationRoleProfile>();
			//QualityAssuranceAction = new List<QualityAssuranceActionProfile>();
			WhereReferenced = new List<string>();
			//LearningCompetencies = new List<TextValueProfile>();
			Subject = new List<TextValueProfile>();
			Keyword = new List<TextValueProfile>();
			Addresses = new List<Address>();
			CommonCosts = new List<CostManifest>();
			CommonConditions = new List<ConditionManifest>();
			Requires = new List<ConditionProfile>();
			Recommends = new List<ConditionProfile>();
			Corequisite = new List<ConditionProfile>();
			EntryCondition = new List<ConditionProfile>();
			LearningOppConnections = new List<ConditionProfile>();
			IsPartOfConditionProfile = new List<ConditionProfile>();
			IsPartOfCredential = new List<Credential>();
			TeachesCompetenciesFrameworks = new List<CredentialAlignmentObjectFrameworkProfile>();
			RequiresCompetenciesFrameworks = new List<CredentialAlignmentObjectFrameworkProfile>();

			TeachesCompetencies = new List<CredentialAlignmentObjectProfile>();
			//RequiresCompetencies = new List<CredentialAlignmentObjectProfile>();
			//EmbeddedAssessment = new List<AssessmentProfile>();
			Occupation = new Enumeration();
			OccupationType = new Enumeration();
			Industry = new Enumeration();
			IndustryType = new Enumeration();
			OtherIndustries = new List<TextValueProfile>();
			OtherOccupations = new List<TextValueProfile>();
			Region = new List<JurisdictionProfile>();
			JurisdictionAssertions = new List<JurisdictionProfile>();
			LearningMethodType = new Enumeration();
			OwnerRoles = new Enumeration();
            QualityAssurance = new AgentRelationshipResult();
            InLanguageCodeList = new List<TextValueProfile>();
			VersionIdentifierList = new List<Entity_IdentifierValue>();
			CredentialsList = new CredentialConnectionsResult();
		}

		public string Name { get; set; }
		public string FriendlyName { get; set; }
		public string SubjectWebpage { get; set; }

		public int EntityStateId { get; set; }
		public string CredentialRegistryId { get; set; }
		public string CTID { get; set; }

		/// <summary>
		/// Single is the primary for now
		/// </summary>
		public string VersionIdentifier { get; set; }
		/// <summary>
		/// Also doing import of list
		/// </summary>
		public List<Entity_IdentifierValue> VersionIdentifierList { get; set; }

		public string AvailableOnlineAt { get; set; }
		//public List<TextValueProfile> Auto_AvailableOnlineAt { get
		//	{
		//		var result = new List<TextValueProfile>();
		//		if ( !string.IsNullOrWhiteSpace( AvailableOnlineAt ) )
		//		{
		//			result.Add( new TextValueProfile() { TextValue = AvailableOnlineAt } );
		//		}
		//		return result;
		//	} }
		//public List<GeoCoordinates> AvailableAt
		//{
		//	get
		//	{
		//		return Addresses.ConvertAll( m => new GeoCoordinates()
		//		{
		//			Address = m,
		//			Latitude = m.Latitude,
		//			Longitude = m.Longitude,
		//			Name = m.Name
		//			//Url = ???
		//		} ).ToList();
		//	}
		//	set
		//	{
		//		Addresses = value.ConvertAll( m => new Address()
		//		{
		//			GeoCoordinates = m,
		//			Latitude = m.Latitude,
		//			Longitude = m.Longitude,
		//			Name = m.Name
		//			//??? = m.Url
		//		} ).ToList();
		//	}
		//} //Alias used for publishing
		

		/// <summary>
		/// OwningAgentUid
		///  (Nov2016)
		/// </summary>
		public Guid OwningAgentUid { get; set; }
		/// <summary>
		/// Inflate OwningAgentUid for display 
		/// </summary>
		public Organization OwningOrganization { get; set; }
		public string OrganizationName
		{
			get
			{
				if ( OwningOrganization != null && OwningOrganization.Id > 0 )
					return OwningOrganization.Name;
				else
					return "";
			}
		}
		public int OwningOrganizationId
		{
			get
			{
				if ( OwningOrganization != null && OwningOrganization.Id > 0 )
					return OwningOrganization.Id;
				else
					return 0;
			}
        }
        public string OwnerOrganizationName { get; set; }
		public string PrimaryOrganizationCTID { get; set; }
		public Enumeration OwnerRoles { get; set; }
		//public List<OrganizationRoleProfile> OwnerOrganizationRoles { get; set; }

		/// <summary>
		/// CodedNotation replaces IdentificationCode
		/// </summary>
		public string CodedNotation { get; set; }

		//public int InLanguageId { get; set; }
		//public string InLanguage { get; set; }
		//public string InLanguageCode { get; set; }
		public List<TextValueProfile> InLanguageCodeList { get; set; }

		//not sure if will use this?
		public QuantitativeValue CreditValue { get; set; } = new QuantitativeValue();
		[Obsolete]
		public string CreditHourType { get; set; }
		[Obsolete]
		public decimal CreditHourValue { get; set; }
		public Enumeration CreditUnitType { get; set; } //Used for publishing
		public int CreditUnitTypeId { get; set; }
		public string CreditUnitTypeDescription { get; set; }
		public decimal CreditUnitValue { get; set; }
		public decimal CreditUnitMaxValue { get; set; }
		public bool CreditValueIsRange { get; set; }

		public List<DurationProfile> EstimatedDuration { get; set; }
		
		public Enumeration DeliveryType { get; set; }
        public Enumeration AudienceType { get; set; } = new Enumeration();
		public CodeItemResult AudienceTypes { get; set; } = new CodeItemResult();
		public Enumeration AudienceLevelType { get; set; } = new Enumeration();
		//
		public CodeItemResult DeliveryMethodTypes { get; set; } = new CodeItemResult();
        public string DeliveryTypeDescription { get; set; }
		//public string VerificationMethodDescription { get; set; }

		public Enumeration Industry { get; set; }
		public Enumeration IndustryType
		{
			get
			{
				return new Enumeration()
				{
					Items = new List<EnumeratedItem>()
					.Concat( Industry.Items )
					//.Concat( OtherIndustries.ConvertAll( m => new EnumeratedItem() { Name = m.TextTitle, Description = m.TextValue } ) ).ToList()
					//.Concat( OtherIndustries.ConvertAll( m => new EnumeratedItem() { Name = m.TextValue } ) )
					.ToList()
				};
			}
			set { Industry = value; }
		} //Used for publishing
		public List<TextValueProfile> OtherIndustries { get; set; }
		public Enumeration Occupation { get; set; }
		public Enumeration OccupationType
		{
			get
			{
				return new Enumeration()
				{
					Items = new List<EnumeratedItem>()
					.Concat( Occupation.Items )
					//.Concat( OtherOccupations.ConvertAll( m => new EnumeratedItem() { Name = m.TextTitle, Description = m.TextValue } ) ).ToList()
					//.Concat( OtherOccupations.ConvertAll( m => new EnumeratedItem() { Name = m.TextValue } ) )
					.ToList()
				};
			}
			set { Occupation = value; }
		} //Used for publishing
		public List<TextValueProfile> OtherOccupations { get; set; }

		//used for import only
		public List<CredentialAlignmentObjectProfile> Occupations { get; set; } = new List<CredentialAlignmentObjectProfile>();
		public List<CredentialAlignmentObjectProfile> Industries { get; set; } = new List<CredentialAlignmentObjectProfile>();
		public List<string> Naics { get; set; } = new List<string>();
		public List<CredentialAlignmentObjectProfile> InstructionalProgramTypes { get; set; }
		public Enumeration InstructionalProgramType { get; set; }
		//used for search attributes
		public CodeItemResult IndustryResults { get; set; } = new CodeItemResult();
		public CodeItemResult OccupationResults { get; set; } = new CodeItemResult();
		public CodeItemResult InstructionalProgramClassification { get; set; } = new CodeItemResult();
        public List<LearningOpportunityProfile> HasPart { get; set; }
		public List<LearningOpportunityProfile> IsPartOf { get; set; }

		public List<OrganizationRoleProfile> OrganizationRole { get; set; }

		public List<TextValueProfile> Keyword { get; set; }
		public List<TextValueProfile> Subject { get; set; }
        public List<string> Subjects { get; set; } = new List<string>();
        public List<string> WhereReferenced { get; set; }
		public List<Address> Addresses { get; set; }
		public string AvailabilityListing { get; set; }

		public List<ConditionProfile> IsPartOfConditionProfile { get; set; }
		public List<Credential> IsPartOfCredential { get; set; }

		public int CompetenciesCount { get; set; }
		public List<CredentialAlignmentObjectProfile> TeachesCompetencies { get; set; }
        public Dictionary<string, RegistryImport> FrameworkPayloads = new Dictionary<string, RegistryImport>();

        public List<CredentialAlignmentObjectFrameworkProfile> TeachesCompetenciesFrameworks { get; set; }
		public List<CredentialAlignmentObjectFrameworkProfile> RequiresCompetenciesFrameworks { get; set; }
				

		public List<JurisdictionProfile> Region { get; set; }
		public List<JurisdictionProfile> JurisdictionAssertions { get; set; }

		public Enumeration LearningMethodType { get; set; }
        public CodeItemResult LearningMethodTypes { get; set; } = new CodeItemResult();
        public List<CostProfile> EstimatedCost { get; set; }

		public List<FinancialAlignmentObject> FinancialAssistanceOLD { get; set; }
		public List<FinancialAssistanceProfile> FinancialAssistance { get; set; } = new List<FinancialAssistanceProfile>();

		public string ListTitle { get; set; }

        #region import 
        public List<int> HasPartIds { get; set; }
        public List<int> IsPartOfIds { get; set; }
        
        //CostManifestId
        //hmm, need to create a placeholder CMs
        public List<int> CostManifestIds { get; set; }
		public List<int> ConditionManifestIds { get; set; }
		public List<Guid> AccreditedBy { get; set; }
		public List<Guid> OwnedBy { get; set; }

		public List<Guid> ApprovedBy { get; set; }

		public List<Guid> OfferedBy { get; set; }

		public List<Guid> RecognizedBy { get; set; }

		public List<Guid> RegulatedBy { get; set; }

		//INs
		public List<JurisdictionProfile> AccreditedIn { get; set; }

		public List<JurisdictionProfile> ApprovedIn { get; set; }

		public List<JurisdictionProfile> OfferedIn { get; set; }

		public List<JurisdictionProfile> RecognizedIn { get; set; }

		public List<JurisdictionProfile> RegulatedIn { get; set; }
		#endregion
		#region Output for detail
		public List<CostManifest> CommonCosts { get; set; }
		public List<ConditionManifest> CommonConditions { get; set; }
        #endregion

        public AgentRelationshipResult QualityAssurance { get; set; }
        public AgentRelationshipResult Org_QAAgentAndRoles { get; set; } = new AgentRelationshipResult();

        #region CONDITION PROFILES
        public List<ConditionProfile> Requires { get; set; }
		public List<ConditionProfile> Recommends { get; set; }

        public List<ConditionProfile> AdvancedStandingFrom { get; set; }
        public List<ConditionProfile> AdvancedStandingFor { get; set; }
        public List<ConditionProfile> PreparationFrom { get; set; }
        public List<ConditionProfile> IsPreparationFor { get; set; }
        public List<ConditionProfile> IsRequiredFor { get; set; }
        public List<ConditionProfile> IsRecommendedFor { get; set; }

        public int RequiresCount { get; set; }
        public int RecommendsCount { get; set; }
        public int RequiredForCount { get; set; }
        public int IsRecommendedForCount { get; set; }
        public int IsAdvancedStandingForCount { get; set; }
        public int AdvancedStandingFromCount { get; set; }
        public int PreparationForCount { get; set; }
        public int PreparationFromCount { get; set; }
        public int CommonCostsCount { get; set; }
        public int CommonConditionsCount { get; set; }
        //public decimal TotalCostCount { get; set; }
        public int FinancialAidCount { get; set; }

        /// <summary>
        /// The resource being referenced must be pursued concurrently with the resource being described.
        /// </summary>
        public List<ConditionProfile> Corequisite { get; set; }

		/// <summary>
		/// The prerequisites for entry into the resource being described.
		/// Comment:
		/// Such requirements might include transcripts, previous experience, lower-level learning opportunities, etc.
		/// </summary>
		public List<ConditionProfile> EntryCondition { get; set; }

		public List<ConditionProfile> LearningOppConnections { get; set; }
        public CredentialConnectionsResult CredentialsList { get; set; }
		#endregion
	}
	//

}

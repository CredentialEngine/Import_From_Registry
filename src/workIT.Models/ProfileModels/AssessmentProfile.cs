using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.Common;

using ApiEntity = workIT.Models.API.CompetencyFramework;


namespace workIT.Models.ProfileModels
{
    [Serializable]
    public class AssessmentProfile : TopLevelObject, IBaseObject
	{
		public AssessmentProfile()
		{
			//Publish_Type = "ceterms:AssessmentProfile";
			EntityTypeId = 3;
			AssessmentMethodType = new Enumeration();
			AssessmentUseType = new Enumeration();
			DeliveryType = new Enumeration();
			OrganizationRole = new List<OrganizationRoleProfile>();
		
			EstimatedDuration = new List<DurationProfile>();
			WhereReferenced = new List<string>();
			Subject = new List<TextValueProfile>();
			Keyword = new List<TextValueProfile>();
			Addresses = new List<Address>();
			CommonCosts = new List<CostManifest>();
			EstimatedCost = new List<CostProfile>();
			//FinancialAssistanceOLD = new List<FinancialAlignmentObject>();
			CommonConditions = new List<ConditionManifest>();

			Requires = new List<ConditionProfile>();
			Recommends = new List<ConditionProfile>();
			Corequisite = new List<ConditionProfile>();
			EntryCondition = new List<ConditionProfile>();
			AssessmentConnections = new List<ConditionProfile>();
			AdministrationProcess = new List<ProcessProfile>();
			DevelopmentProcess = new List<ProcessProfile>();
			MaintenanceProcess = new List<ProcessProfile>();

			IsPartOfConditionProfile = new List<ConditionProfile>();
			IsPartOfCredential = new List<Credential>();
			IsPartOfLearningOpp = new List<LearningOpportunityProfile>();
            QualityAssurance = new AgentRelationshipResult();
            AssessesCompetenciesFrameworks = new List<CredentialAlignmentObjectFrameworkProfile>();

			AssessesCompetencies = new List<CredentialAlignmentObjectProfile>();
			RequiresCompetenciesFrameworks = new List<CredentialAlignmentObjectFrameworkProfile>();
			Occupation = new Enumeration();
			OccupationType = new Enumeration();
			OtherIndustries = new List<TextValueProfile>();
			OtherOccupations = new List<TextValueProfile>();
			Industry = new Enumeration();
			IndustryType = new Enumeration();
			InstructionalProgramType = new Enumeration();
			Region = new List<JurisdictionProfile>();
			JurisdictionAssertions = new List<JurisdictionProfile>();
			ScoringMethodType = new Enumeration();
			OwnerRoles = new Enumeration();
			//to delete
			CredentialsList = new CredentialConnectionsResult();
			InLanguageCodeList = new List<TextValueProfile>();
			VersionIdentifierList = new List<Entity_IdentifierValue>();
		}
		//public string Name { get; set; }
		//public string FriendlyName { get; set; }
		//public int EntityStateId { get; set; }
		//public string CTID { get; set; }
		//public string CredentialRegistryId { get; set; }
		//public System.Guid OwningAgentUid { get; set; }
		//public string SubjectWebpage { get; set; }
		/// <summary>
		/// Inflate OwningAgentUid for display 
		/// </summary>
		//public Organization OwningOrganization { get; set; }
		//public string OwnerOrganizationName { get; set; }
		//public string OrganizationName
		//{
		//	get
		//	{
		//		if ( OwningOrganization != null && OwningOrganization.Id > 0 )
		//			return OwningOrganization.Name;
		//		else
		//			return "";
		//	}
		//}
		//public int OwningOrganizationId
		//{
		//	get
		//	{
		//		if ( OwningOrganization != null && OwningOrganization.Id > 0 )
		//			return OwningOrganization.Id;
		//		else
		//			return 0;
		//	}
  //      }
		public Enumeration OwnerRoles { get; set; }
		//public List<OrganizationRoleProfile> OwnerOrganizationRoles { get; set; }
		//public string PrimaryOrganizationCTID { get; set; }


		//public int InLanguageId { get; set; }
		//public string InLanguage { get; set; }
		//public string InLanguageCode { get; set; }
		public List<TextValueProfile> InLanguageCodeList { get; set; }

		//not sure if will use this?
		//public QuantitativeValue QVCreditValue { get; set; } = new QuantitativeValue();
		//public ValueProfile CreditValue { get; set; } = new ValueProfile();
		//20-07-24 updating to handle a list
		//public List<QuantitativeValue> QVCreditValueList { get; set; } = new List<QuantitativeValue>();
		public List<ValueProfile> CreditValue { get; set; } = new List<ValueProfile>();
		public string CreditValueJson { get; set; }

		//[Obsolete]
		//public string CreditHourType { get; set; }
		//[Obsolete]
		//public decimal CreditHourValue { get; set; }

		public Enumeration CreditUnitType { get; set; } //Used for publishing
		public int CreditUnitTypeId { get; set; }
		public string CreditUnitTypeDescription { get; set; }
		public decimal CreditUnitValue { get; set; }
		public decimal CreditUnitMinValue { get; set; }
		public decimal CreditUnitMaxValue { get; set; }
		public bool CreditValueIsRange { get; set; }
		//=======================================

		public Enumeration AssessmentUseType { get; set; }
		public Enumeration DeliveryType { get; set; }
		public string DeliveryTypeDescription { get; set; }
		//public string VerificationMethodDescription { get; set; }
		public string AssessmentMethodDescription { get; set; }
		public string LearningMethodDescription { get; set; }

		public List<OrganizationRoleProfile> OrganizationRole { get; set; }
		//
		public List<CodeItem> ProcessProfilesSummary { get; set; } = new List<CodeItem>();
		public List<ProcessProfile> AdministrationProcess { get; set; }
		public List<ProcessProfile> DevelopmentProcess { get; set; }
		public List<ProcessProfile> MaintenanceProcess { get; set; }

		public List<CostProfile> EstimatedCost { get; set; }
		//public string ExpirationDate { get; set; }
		//public List<FinancialAlignmentObject> FinancialAssistanceOLD { get; set; }
		public List<FinancialAssistanceProfile> FinancialAssistance { get; set; } = new List<FinancialAssistanceProfile>();
		public string FinancialAssistanceJson { get; set; }

		public List<DurationProfile> EstimatedDuration { get; set; }
        //public List<TextValueProfile> ResourceUrl { get; set; } = new List<TextValueProfile>();
		public string AssessmentExample { get; set; }
		public string AssessmentExampleDescription { get; set; }

		public List<TextValueProfile> Subject { get; set; }
        public List<string> Subjects { get; set; } = new List<string>();
        public List<TextValueProfile> Keyword { get; set; }
		public List<TextValueProfile> SameAs { get; set; } = new List<TextValueProfile>();


		//used by import, NOT the detail page
		public List<CredentialAlignmentObjectProfile> AssessesCompetencies { get; set; }
		public int CompetenciesCount { get; set; }
		public List<int> TargetPathwayIds { get; set; } = new List<int>();
		public List<int> TargetAssessmentIds { get; set; } = new List<int>();
		public List<string> TargetLearningResource { get; set; } = new List<string>();

		public Dictionary<string, RegistryImport> FrameworkPayloads = new Dictionary<string, RegistryImport>();

        //used by detail page, not the import
        public List<CredentialAlignmentObjectFrameworkProfile> AssessesCompetenciesFrameworks { get; set; }
		public List<CredentialAlignmentObjectFrameworkProfile> RequiresCompetenciesFrameworks { get; set; }
		//used by finderAPI
		public List<ApiEntity> AssessesFrameworks { get; set; }
		public List<ApiEntity> RequiresFrameworks { get; set; }


		public string AvailableOnlineAt { get; set; }

		/// <summary>
		/// Single is the primary for now
		/// </summary>
		public string VersionIdentifier { get; set; }
		/// <summary>
		/// Also doing import of list
		/// </summary>
		public List<Entity_IdentifierValue> VersionIdentifierList { get; set; }

		public string CodedNotation { get; set; }
		/// <summary>
		/// Identifier
		/// Definition:	Alphanumeric token that identifies this resource and information about the token's originating context or scheme.
		/// </summary>	
		public List<Entity_IdentifierValue> Identifier { get; set; } = new List<Entity_IdentifierValue>();
		//or could store this as json
		public string IdentifierJson { get; set; }
		public List<string> WhereReferenced { get; set; }
		public List<ConditionProfile> IsPartOfConditionProfile { get; set; }
		public List<Credential> IsPartOfCredential { get; set; }
		public List<LearningOpportunityProfile> IsPartOfLearningOpp { get; set; }

		/// <summary>
		/// List of ETPL Credentials where is a member
		/// </summary>
		public List<Credential> IsResourceOnETPL { get; set; } = new List<Credential>();
		public List<Address> Addresses { get; set; }
		public string AvailabilityListing { get; set; }

		#region import 
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



		public List<ConditionProfile> Requires { get; set; }
		public List<ConditionProfile> Recommends { get; set; }
		public List<ConditionProfile> Corequisite { get; set; }
		/// <summary>
		/// The prerequisites for entry into the resource being described.
		/// Comment:
		/// Such requirements might include transcripts, previous experience, lower-level learning opportunities, etc.
		/// </summary>
		public List<ConditionProfile> EntryCondition { get; set; }

        /// <summary>
        /// The resource being referenced must be pursued concurrently with the resource being described.
        /// </summary>

        public List<ConditionProfile> AdvancedStandingFrom { get; set; }
        public List<ConditionProfile> IsAdvancedStandingFor { get; set; }
        public List<ConditionProfile> PreparationFrom { get; set; }
        public List<ConditionProfile> IsPreparationFor { get; set; }
        public List<ConditionProfile> IsRequiredFor { get; set; }
        public List<ConditionProfile> IsRecommendedFor { get; set; }
        public List<ConditionProfile> AssessmentConnections { get; set; }

        public int RequiresCount { get; set; }
        public int RecommendsCount { get; set; }
        public int RequiredForCount { get; set; }
        public int IsRecommendedForCount { get; set; }
        public int IsAdvancedStandingForCount { get; set; }
        public int AdvancedStandingFromCount { get; set; }
        public int PreparationForCount { get; set; }
        public int PreparationFromCount { get; set; }
		public int CostProfilesCount { get; set; }
		public int NumberOfCostProfileItems { get; set; }

		public int CommonCostsCount { get; set; }
        public int CommonConditionsCount { get; set; }
        //public decimal TotalCostCount { get; set; }
        public int FinancialAidCount { get; set; }
        public string ListTitle { get; set; }
        public CredentialConnectionsResult CredentialsList { get; set; }
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
		public List<CredentialAlignmentObjectProfile> Occupations { get; set; }
		public List<CredentialAlignmentObjectProfile> Industries { get; set; }
		public List<string> Naics { get; set; }
		public List<CredentialAlignmentObjectProfile> InstructionalProgramTypes { get; set; }
		public Enumeration InstructionalProgramType { get; set; }
		public CodeItemResult IndustryResults { get; set; } = new CodeItemResult();
		public CodeItemResult OccupationResults { get; set; } = new CodeItemResult();
		public CodeItemResult InstructionalProgramClassification { get; set; } = new CodeItemResult();
        public Enumeration AssessmentMethodType { get; set; }
		public string AssessmentOutput { get; set; }
        public Enumeration AudienceType { get; set; } = new Enumeration();
		public Enumeration AudienceLevelType { get; set; } = new Enumeration();
		public string ExternalResearch { get; set; }
		//public List<TextValueProfile> Auto_ExternalResearch
		//{
		//	get
		//	{
		//		var result = new List<TextValueProfile>();
		//		if ( !string.IsNullOrWhiteSpace( ExternalResearch ) )
		//		{
		//			result.Add( new TextValueProfile() { TextValue = ExternalResearch } );
		//		}
		//		return result;
		//	}
		//}
		public bool? HasGroupEvaluation { get; set; }
		public bool? HasGroupParticipation { get; set; }
		public bool? IsProctored { get; set; }

		public string ProcessStandards { get; set; }
		public string ProcessStandardsDescription { get; set; }


		public Enumeration ScoringMethodType { get; set; }
		public string ScoringMethodDescription { get; set; }
		public string ScoringMethodExample { get; set; }
		public string ScoringMethodExampleDescription { get; set; }

		public List<JurisdictionProfile> Region { get; set; }
		public List<JurisdictionProfile> JurisdictionAssertions { get; set; }

        public CodeItemResult AssessmentMethodTypes { get; set; } = new CodeItemResult();
        public CodeItemResult AssessmentUseTypes { get; set; } = new CodeItemResult();
        public CodeItemResult ScoringMethodTypes { get; set; } = new CodeItemResult();
        public CodeItemResult DeliveryMethodTypes { get; set; } = new CodeItemResult();
		public CodeItemResult AudienceTypes { get; set; } = new CodeItemResult();
		public AgentRelationshipResult QualityAssurance { get; set; }
        public AgentRelationshipResult Org_QAAgentAndRoles { get; set; } = new AgentRelationshipResult();
    }
	//

}

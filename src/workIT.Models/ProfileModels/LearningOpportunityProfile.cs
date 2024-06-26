﻿using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;

using workIT.Models.Common;

namespace workIT.Models.ProfileModels
{
	[Serializable] 
    public class LearningOpportunityProfile : TopLevelObject, IBaseObject
	{
		public LearningOpportunityProfile()
		{
			//OwningOrganization = new Organization();
			EntityTypeId = 7;
			EstimatedCost = new List<CostProfile>();
			//FinancialAssistanceOLD = new List<FinancialAlignmentObject>();
			EstimatedDuration = new List<DurationProfile>();
			DeliveryType = new Enumeration();
            InstructionalProgramType = new Enumeration();
			HasPart = new List<LearningOpportunityProfile>();
			//IsPartOfConditionProfile = new List<ConditionProfile>();
			OrganizationRole = new List<OrganizationRoleProfile>();
			//QualityAssuranceAction = new List<QualityAssuranceActionProfile>();
			WhereReferenced = new List<string>();
			//LearningCompetencies = new List<TextValueProfile>();
			Subject = new List<TextValueProfile>();
			Keyword = new List<TextValueProfile>();
			AvailableAt = new List<Address>();
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
			IndustryType = new Enumeration();
			//IndustryType = new Enumeration();
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



		public int LearningEntityTypeId { get; set; } = 7;
		public string LearningEntityType { get; set; }
		//public string CTDLTypeLabel { get; set; }
		public string LearningTypeSchema { get; set; }


		public string AvailableOnlineAt { get; set; }

		public Enumeration OwnerRoles { get; set; }
		//public List<OrganizationRoleProfile> OwnerOrganizationRoles { get; set; }

		/// <summary>
		/// CodedNotation replaces IdentificationCode
		/// </summary>
		public string CodedNotation { get; set; }

		/// <summary>
		/// Identifier
		/// Definition:	Alphanumeric token that identifies this resource and information about the token's originating context or scheme.
		/// </summary>	
		public List<Entity_IdentifierValue> Identifier { get; set; } = new List<Entity_IdentifierValue>();
		public List<IdentifierValue> IdentifierNew { get; set; } = new List<IdentifierValue>();
		//or could store this as json
		public string IdentifierJSON { get; set; }

		/// <summary>
		/// Also doing import of list
		/// </summary>
		public List<Entity_IdentifierValue> VersionIdentifierList { get; set; }
		public List<IdentifierValue> VersionIdentifierNew { get; set; }
		public string VersionIdentifierJSON { get; set; }

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
		//public decimal CreditUnitMinValue { get; set; }

		//public decimal CreditUnitMaxValue { get; set; }
		public bool CreditValueIsRange { get; set; }
		public List<TextValueProfile> DegreeConcentration { get; set; } = new List<TextValueProfile>();

		public List<DurationProfile> EstimatedDuration { get; set; }
		
		public Enumeration DeliveryType { get; set; }
        public Enumeration AudienceType { get; set; } 
		public CodeItemResult AudienceTypes { get; set; }
		public Enumeration AudienceLevelType { get; set; } 
		public CodeItemResult AudienceLevelTypes { get; set; } 
		public CodeItemResult AssessmentMethodTypes { get; set; }
		public CodeItemResult ProvidesTransferValueForElastic { get; set; } = new CodeItemResult();
		public CodeItemResult ReceivesTransferValueFromElastic { get; set; } = new CodeItemResult();
		public CodeItemResult ObjectOfActionElastic { get; set; } = new CodeItemResult();
		//
		public CodeItemResult DeliveryMethodTypes { get; set; } 
        public string DeliveryTypeDescription { get; set; }
		//public string VerificationMethodDescription { get; set; }

		public Enumeration IndustryType { get; set; }
		//public Enumeration IndustryType
		//{
		//	get
		//	{
		//		return new Enumeration()
		//		{
		//			Items = new List<EnumeratedItem>()
		//			.Concat( Industry.Items )
		//			//.Concat( OtherIndustries.ConvertAll( m => new EnumeratedItem() { Name = m.TextTitle, Description = m.TextValue } ) ).ToList()
		//			//.Concat( OtherIndustries.ConvertAll( m => new EnumeratedItem() { Name = m.TextValue } ) )
		//			.ToList()
		//		};
		//	}
		//	set { Industry = value; }
		//} //
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

		//used for import only.
		//22-05-17 and now for API output
		public List<CredentialAlignmentObjectProfile> OccupationTypes { get; set; } = new List<CredentialAlignmentObjectProfile>();
		public List<CredentialAlignmentObjectProfile> IndustryTypes { get; set; } = new List<CredentialAlignmentObjectProfile>();
		public List<string> Naics { get; set; } = new List<string>();
		public List<CredentialAlignmentObjectProfile> InstructionalProgramTypes { get; set; }
		public Enumeration InstructionalProgramType { get; set; }
		//used for search attributes
		public CodeItemResult IndustryResults { get; set; } = new CodeItemResult();
		public CodeItemResult OccupationResults { get; set; } = new CodeItemResult();
		public CodeItemResult InstructionalProgramClassification { get; set; } = new CodeItemResult();

		public List<CollectionMember> CollectionMembers { get; set; } = new List<CollectionMember>();
		public int InCollectionCount { get; set; }	= 0;
		/// <summary>
		/// Is Non-Credit
		/// Resource carries or confers no official academic credit towards a program or a credential.
		/// </summary>
		public bool? IsNonCredit { get; set; }

		public List<LearningOpportunityProfile> HasPart { get; set; }
		public List<LearningOpportunityProfile> IsPartOf { get; set; } = new List<LearningOpportunityProfile>();
		public List<LearningOpportunityProfile> Prerequisite { get; set; }
		public List<OrganizationRoleProfile> OrganizationRole { get; set; }
		public List<OrganizationRoleProfile> OwningOrganizationQAReceived { get; set; }

		//public List<Organization> OfferedByOrganization { get; set; } = new List<Organization>();

		/// <summary>
		/// List of ETPL Credentials where is a member
		/// </summary>
		public List<Credential> IsResourceOnETPL { get; set; } = new List<Credential>();
		public List<TextValueProfile> Keyword { get; set; }


		public List<AssessmentProfile> TargetAssessment { get; set; } 
		//inverse property not applicable, or is it?
		public List<LearningOpportunityProfile> TargetLearningOpportunity { get; set; } 
		public List<Pathway> TargetPathway { get; set; } = new List<Pathway>();
		public List<ResourceSummary> RelatedAction { get; set; } = new List<ResourceSummary>();

		public List<TextValueProfile> SameAs { get; set; } = new List<TextValueProfile>();
		/// <summary>
		/// School Courses for the Exchange of Data code for a course.
		/// It is preferable to record the whole 12 character alphanumeric code, however it is also valid to record just the five digit subject code + course number.
		/// Minimum of 5 characters and maximum of 14 characters for now. 
		/// COURSE ONLY
		/// </summary>
		public string SCED { get; set; }

        public List<ScheduledOffering> HasScheduledOffering { get; set; }
        public List<TopLevelObject> HasSupportService { get; set; } = new List<TopLevelObject>();
		public List<ResourceSummary> ProvidesTransferValueFor { get; set; } = new List<ResourceSummary>();
		public List<ResourceSummary> ReceivesTransferValueFrom { get; set; } = new List<ResourceSummary>();
		public List<ResourceSummary> HasRubric { get; set; } = new List<ResourceSummary>();
		/// <summary>
		/// Action related to this resource.
		/// List of URIs for an existing Action
		/// ceterms:targetAction
		/// </summary>
		public List<ResourceSummary> ObjectOfAction { get; set; } = new List<ResourceSummary>();

		public List<TextValueProfile> Subject { get; set; }
        public List<string> Subjects { get; set; } = new List<string>();
		public string Supersedes { get; set; }
		public string SupersededBy { get; set; }

		public List<string> WhereReferenced { get; set; }
		public List<Address> AvailableAt { get; set; }

		public List<Address> Addresses
		{
			get {
				if ( AvailableAt == null )
					return null;
				else
					return AvailableAt;
			}
		}
		

		public string AvailabilityListing { get; set; }
		public string InCatalog { get; set; }
		public List<ConditionProfile> IsPartOfConditionProfile { get; set; }
		public List<Credential> IsPartOfCredential { get; set; }
		public List<AssessmentProfile> IsPartOfAssessment { get; set; } = new List<AssessmentProfile>();

		public List<LearningOpportunityProfile> IsPartOfLearningOpp { get; set; } = new List<LearningOpportunityProfile>();

		public int CompetenciesCount { get; set; }
		public List<CredentialAlignmentObjectProfile> AssessesCompetencies { get; set; }

		public List<CredentialAlignmentObjectProfile> TeachesCompetencies { get; set; }

		/// <summary>
		/// A prototype to send all frameworks to UI.
		/// Not implemented.
		/// </summary>
        public Dictionary<string, RegistryImport> FrameworkPayloads = new Dictionary<string, RegistryImport>();

		public List<CredentialAlignmentObjectFrameworkProfile> AssessesCompetenciesFrameworks { get; set; }

		public List<CredentialAlignmentObjectFrameworkProfile> TeachesCompetenciesFrameworks { get; set; }
		public List<CredentialAlignmentObjectFrameworkProfile> RequiresCompetenciesFrameworks { get; set; }
				

		public List<JurisdictionProfile> Region { get; set; }
		public List<JurisdictionProfile> JurisdictionAssertions { get; set; }
		public Enumeration AssessmentMethodType { get; set; }

		public string AssessmentMethodDescription { get; set; }
		public string LearningMethodDescription { get; set; }
		public Enumeration LifeCycleStatusType { get; set; }
		public string LifeCycleStatus { get; set; }
		public int LifeCycleStatusTypeId { get; set; }

		public Enumeration LearningMethodType { get; set; }
        public CodeItemResult LearningMethodTypes { get; set; } = new CodeItemResult();
		public List<string> TargetLearningResource { get; set; } = new List<string>();

		public List<CostProfile> EstimatedCost { get; set; }
		//public string ExpirationDate { get; set; }

		//public List<FinancialAlignmentObject> FinancialAssistanceOLD { get; set; }
		public List<FinancialAssistanceProfile> FinancialAssistance { get; set; } = new List<FinancialAssistanceProfile>();
		public string FinancialAssistanceJson { get; set; }

		public string ListTitle { get; set; }
		public Enumeration ScheduleTimingType { get; set; }
		public Enumeration ScheduleFrequencyType { get; set; }
		public Enumeration OfferFrequencyType { get; set; }
		#region import 
		public List<int> HasOfferingIds { get; set; } = new List<int>();
		public List<int> HasRubricIds { get; set; } = new List<int>();

		public List<int> HasSupportServiceIds { get; set; } = new List<int>();

        public List<int> HasPartIds { get; set; }
        public List<int> IsPartOfIds { get; set; }
		public List<int> PrerequisiteIds { get; set; }
		public List<int> TargetAssessmentIds { get; set; } = new List<int>();
		public List<int> TargetLearningOpportunityIds { get; set; } = new List<int>();
		public List<int> ProvidesTVForIds { get; set; }
		public List<int> ReceivesTVFromIds { get; set; }
		public List<int> ObjectOfActionIds { get; set; }


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
        public List<Guid> RegisteredBy { get; set; }
        //

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

		//no this would mostly be one off
		public bool HasCachedAgentRelationships { get; set; }
		public bool HasCachedResourceDetail { get; set; }

		#region CONDITION PROFILES
		public List<ConditionProfile> Requires { get; set; }
		public List<ConditionProfile> Recommends { get; set; }

        public List<ConditionProfile> AdvancedStandingFrom { get; set; }
        public List<ConditionProfile> IsAdvancedStandingFor { get; set; }
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
		public int CostProfilesCount { get; set; }
		public int NumberOfCostProfileItems { get; set; }

		public int CommonCostsCount { get; set; }
        public int CommonConditionsCount { get; set; }
        //public decimal TotalCostCount { get; set; }
        public int FinancialAidCount { get; set; }
		public int AggregateDataProfileCount { get; set; }
		public string AggregateDataProfileSummary { get; set; }
		public List<AggregateDataProfile> AggregateData { get; set; } = new List<AggregateDataProfile>();

		/// <summary>
		/// Use alternate name for display
		/// </summary>
		public List<string> AlternateName { get; set; }
		//use for import and detail, so maybe don't need AlternateName. Need for API
		public List<TextValueProfile> AlternateNames { get; set; } = new List<TextValueProfile>();

		public int DataSetProfileCount { get; set; }
		public List<QData.DataSetProfile> ExternalDataSetProfiles { get; set; } = new List<QData.DataSetProfile>();

		public int TransferValueCount { get; set; }
		public List<TransferValueProfile> HasTransferValueProfile { get; set; } = new List<TransferValueProfile>();


		/// <summary>
		/// The resource being referenced must be pursued concurrently with the resource being described.
		/// </summary>
		public List<ConditionProfile> Corequisite { get; set; }
		public List<ConditionProfile> CoPrerequisite { get; set; } = new List<ConditionProfile>();

		/// <summary>
		/// The prerequisites for entry into the resource being described.
		/// Comment:
		/// Such requirements might include transcripts, previous experience, lower-level learning opportunities, etc.
		/// </summary>
		public List<ConditionProfile> EntryCondition { get; set; }

		public List<ConditionProfile> LearningOppConnections { get; set; }
        public CredentialConnectionsResult CredentialsList { get; set; }
        #endregion

        public JObject ResourceDetail { get; set; }

        #region 
		public List<string> DataSetProfileCTIDList { get;set; }
        #endregion
    }
    //

}

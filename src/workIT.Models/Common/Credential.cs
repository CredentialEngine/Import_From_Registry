using System;
using System.Collections.Generic;
using System.Linq;

using workIT.Models.ProfileModels;

namespace workIT.Models.Common
{
    [Serializable]
    public class Credential : TopLevelObject, IBaseObject
	{
		public Credential()
		{
			EntityTypeId = 1;
			Addresses = new List<Address>();

			AlternateName = new List<string>();
			AudienceLevelType = new Enumeration();
			CredentialStatusType = new Enumeration();
			//CredentialType = new Enumeration();
			
			EmbeddedCredentials = new List<Credential>();

			EstimatedCosts = new List<CostProfile>();
		    //FinancialAssistanceOLD = new List<FinancialAlignmentObject>();
			EstimatedDuration = new List<DurationProfile>();
            RenewalFrequency = new DurationItem();
			HasPartIds = new List<int>();
			
			Id = 0;
			Industry = new Enumeration();
			IsPartOf = new List<Credential>();
			IsPartOfIds = new List<int>();

			Jurisdiction = new List<JurisdictionProfile>();
			//Region = new List<JurisdictionProfile>();
			JurisdictionAssertions = new List<JurisdictionProfile>();
			Keyword = new List<TextValueProfile>();
			MilitaryOccupation = new Enumeration();
			Name = "";
			Occupation = new Enumeration();
			OrganizationRole = new List<OrganizationRoleProfile>();
			OfferedByOrganizationRole = new List<OrganizationRoleProfile>();
			OfferedByOrganization = new List<Organization>();

			//QualityAssuranceAction = new List<QualityAssuranceActionProfile>();
			CommonCosts = new List<CostManifest>(); 
			CommonConditions = new List<ConditionManifest>();

			AllConditions = new List<ConditionProfile>();
			CredentialConnections = new List<ConditionProfile>();
			CommonConditions = new List<ConditionManifest>();

			Recommends = new List<ConditionProfile>();
			Requires = new List<ConditionProfile>();
			Renewal = new List<ConditionProfile>();
			Corequisite = new List<ConditionProfile>();

			//RequiresCompetencies = new List<CredentialAlignmentObjectProfile>();
			Revocation = new List<RevocationProfile>();
			
			Subject = new List<TextValueProfile>();
			DegreeConcentration = new List<TextValueProfile>();
			DegreeMajor = new List<TextValueProfile>();
			DegreeMinor = new List<TextValueProfile>();

			PrimaryOrganization = new Organization();
			OwnerRoles = new Enumeration();

			//TargetCredential = new List<Credential>();
			TargetAssessment = new List<AssessmentProfile>();
			TargetLearningOpportunity = new List<LearningOpportunityProfile>();
			AssessmentEstimatedCosts = new List<CostProfile>();
			LearningOpportunityEstimatedCosts = new List<CostProfile>();

			Naics = new List<string>();
			OtherIndustries = new List<TextValueProfile>();
			OtherOccupations = new List<TextValueProfile>();

			CredentialProcess = new List<ProcessProfile>();
			AdministrationProcess = new List<ProcessProfile>();
			DevelopmentProcess = new List<ProcessProfile>();
			MaintenanceProcess = new List<ProcessProfile>();
            ComplaintProcess = new List<ProcessProfile>();
            AppealProcess = new List<ProcessProfile>();
            ReviewProcess = new List<ProcessProfile>();
            RevocationProcess = new List<ProcessProfile>();

            InLanguageCodeList = new List<TextValueProfile>();

			VersionIdentifierList = new List<Entity_IdentifierValue>();
		}

		//public DateTime RegistryLastUpdated { get; set; }

		public CredentialExternalProperties CredentialExternalProperties { get; set; } = new CredentialExternalProperties();
		//propsal used JsonProperties to store serialized version of CredentialExternalProperties
		public string JsonProperties { get; set; }
		public Enumeration OwnerRoles { get; set; }

		public List<TextValueProfile> InLanguageCodeList { get; set; }

		//public Guid CopyrightHolder { get; set; }
		public List<Guid> CopyrightHolders { get; set; } = new List<Guid>();
		public List<Organization> CopyrightHolderOrganization { get; set; }

        /// <summary>
        /// Use alternate name for display
        /// </summary>
		public List<string> AlternateName { get; set; }
        //use for import and detail, so maybe don't need AlternateName. Need for API
        public List<TextValueProfile> AlternateNames { get; set; } = new List<TextValueProfile>();

		public string LatestVersion { get; set; }
		public string PreviousVersion { get; set; }
		public string NextVersion { get; set; } //URL
		public string SupersededBy { get; set; } //URL
		public string Supersedes { get; set; } //URL
		//NEW
		//public int LatestVersionId { get; set; }
		//public int PreviousVersionId { get; set; }
		//public int NextVersionId { get; set; } 
		//public int SupersededById { get; set; } 
		//public int SupersedesId { get; set; } 

		//public string SubjectWebpage { get; set; }



		public string AvailableOnlineAt { get; set; }
		
		public string AvailabilityListing { get; set; }
		
		//public string Image { get; set; } //image URL
		/// <summary>
		/// ISIC Revision 4 Code
		/// </summary>
		public string ISICV4 { get; set; }
		public List<Address> Addresses { get; set; }
		

		//public List<JurisdictionProfile> Jurisdiction { get; set; }
		//public List<JurisdictionProfile> Region { get; set; }
		public List<JurisdictionProfile> JurisdictionAssertions { get; set; }

		public List<DurationProfile> EstimatedDuration { get; set; }
        //always single, 
        public DurationItem RenewalFrequency { get; set; }

		public Enumeration CredentialTypeEnum { get; set; }
	
		public int CredentialTypeId { get; set; }
		public string CredentialType { get; set; }
		public string CredentialTypeSchema{ get; set; }
		public string OwningOrgDisplay { get; set; }


		public Enumeration AudienceLevelType { get; set; }
        public Enumeration AudienceType { get; set; } = new Enumeration();
		public Enumeration AssessmentDeliveryType { get; set; } = new Enumeration();
		public Enumeration LearningDeliveryType { get; set; } = new Enumeration();

		public int CredentialStatusTypeId { get; set; }
		public Enumeration CredentialStatusType { get; set; }
		public string CredentialStatus { get; set; }
		public string CredentialStatusTypeSchema { get; set; }
		public List<Credential> EmbeddedCredentials { get; set; } //bundled/sub-credentials
		/// <summary>
		/// Is Non-Credit
		/// Resource carries or confers no official academic credit towards a program or a credential.
		/// </summary>
		public bool? IsNonCredit { get; set; }

		public List<Credential> IsPartOf { get; set; } //pseudo-"parent" credentials that this credential is a part of or included with (could be multiple)
		public List<int> HasPartIds { get; set; }
		public List<int> IsPartOfIds { get; set; }
		public List<ConditionProfile> IsPartOfConditionProfile { get; set; } = new List<ConditionProfile>();
		//21-09-08 mp - not clear if this is needed anymore - hold over from publisher?
		public List<Credential> IsPartOfCredential { get; set; }

		// ==================================================================
		public List<Credential> ETPLCredentials { get; set; } = new List<Credential>();
		public List<AssessmentProfile> ETPLAssessments { get; set; } = new List<AssessmentProfile>();
		public List<LearningOpportunityProfile> ETPLLearningOpportunities { get; set; } = new List<LearningOpportunityProfile>();
		//for detail page, use a generic class
		public List<TopLevelObject> HasETPLMembers { get; set; } = new List<TopLevelObject>();
		/// <summary>
		/// List of ETPL Credentials where is a member
		/// </summary>
		public List<Credential> IsResourceOnETPL { get; set; } = new List<Credential>();

        public List<TopLevelObject> HasSupportService { get; set; } = new List<TopLevelObject>();
        public List<SupportService> HasSupportService2 { get; set; } = new List<SupportService>();

        public List<TransferValueProfile> HasTransferValueProfile { get; set; } = new List<TransferValueProfile>();
		// ==================================================================

		/// <summary>
		/// placeholder for all Processes
		/// </summary>
		public List<CodeItem> ProcessProfilesSummary { get; set; } = new List<CodeItem>();
		public List<ProcessProfile> CredentialProcess { get; set; }
		public List<ProcessProfile> AdministrationProcess { get; set; }
		public List<ProcessProfile> AppealProcess { get; set; }
		public List<ProcessProfile> ComplaintProcess { get; set; }
		public List<ProcessProfile> DevelopmentProcess { get; set; }
		public List<ProcessProfile> MaintenanceProcess { get; set; }
		public List<ProcessProfile> ReviewProcess { get; set; }
		public List<ProcessProfile> RevocationProcess { get; set; }
        

        //public List<EarningsProfile> Earnings { get; set; }
        //public List<EmploymentOutcomeProfile> EmploymentOutcome { get; set; }
        //public List<HoldersProfile> Holders { get; set; }

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
					.Concat( OtherIndustries.ConvertAll( m => new EnumeratedItem() { Name = m.TextValue } ) ).ToList()
				};
			}
			set { Industry = value; }
		} //used by detail page
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
					.Concat( OtherOccupations.ConvertAll( m => new EnumeratedItem() { Name = m.TextValue } ) ).ToList()
				};
			}
			set { Occupation = value; }
		} //Used for publishing
		public List<TextValueProfile> OtherOccupations { get; set; }

		public Enumeration InstructionalProgramType { get; set; } = new Enumeration();
		//only used for display
		public Enumeration NavyRating { get; set; } = new Enumeration();
		//only used by import
		public List<CredentialAlignmentObjectProfile> NavyRatingType { get; set; } = new List<CredentialAlignmentObjectProfile>();
		//confirm if necessary
		//public CodeItemResult InstructionalProgramClassification { get; set; } = new CodeItemResult();
		public Enumeration MilitaryOccupation { get; set; }

		public List<OrganizationRoleProfile> OrganizationRole { get; set; }
		public List<OrganizationRoleProfile> OwningOrganizationQAReceived { get; set; }

		public List<OrganizationRoleProfile> OfferedByOrganizationRole { get; set; }
		public List<Organization> OfferedByOrganization { get; set; }

		public List<TextValueProfile> SameAs { get; set; } = new List<TextValueProfile>();

		public List<VerificationServiceProfile> UsesVerificationService { get; set; } = new List<VerificationServiceProfile>();
        #region Import Profiles
        public List<CredentialAlignmentObjectProfile> OccupationTypes { get; set; }
		public List<CredentialAlignmentObjectProfile> IndustryTypes { get; set; }
		public List<string> Naics { get; set; }
		public List<CredentialAlignmentObjectProfile> InstructionalProgramTypes { get; set; } = new List<CredentialAlignmentObjectProfile>();
		public List<Guid> AccreditedBy { get; set; }
		public List<Guid> OwnedBy { get; set; }

		public List<Guid> ApprovedBy { get; set; }

		public List<Guid> OfferedBy { get; set; }

		public List<Guid> RecognizedBy { get; set; }

		public List<Guid> RegulatedBy { get; set; }

		public List<Guid> RevokedBy { get; set; }

		public List<Guid> RenewedBy { get; set; }
		//public List<Guid> PublishedBy { get; set; }
		//INs
		public List<JurisdictionProfile> AccreditedIn { get; set; }

		public List<JurisdictionProfile> ApprovedIn { get; set; }

		public List<JurisdictionProfile> OfferedIn { get; set; }

		public List<JurisdictionProfile> RecognizedIn { get; set; }

		public List<JurisdictionProfile> RegulatedIn { get; set; }

		public List<JurisdictionProfile> RevokedIn { get; set; }

		public List<JurisdictionProfile> RenewedIn { get; set; }
		#endregion
		//public List<QualityAssuranceActionProfile> QualityAssuranceAction { get; set; }

		#region Condition Profiles

		//actually, get all conditions and then split out to other types
		public List<ConditionProfile> AllConditions { get; set; }
		public List<ConditionProfile> CredentialConnections{ get; set; }
		//public List<ConditionProfile> AssessmentConnections { get; set; }
		//public List<ConditionProfile> LearningOppConnections { get; set; }


		public List<CollectionMember> CollectionMembers { get; set; } = new List<CollectionMember>();

		#region Output for detail
		public List<CostManifest> CommonCosts { get; set; }
		public List<ConditionManifest> CommonConditions { get; set; }
		#endregion

		public List<ConditionProfile> Requires { get; set; }
		public List<ConditionProfile> Recommends { get; set; }
		public List<ConditionProfile> Renewal { get; set; }
		public List<ConditionProfile> Corequisite { get; set; }
		public List<ConditionProfile> CoPrerequisite { get; set; } = new List<ConditionProfile>();


		//Use the DisambiguateConditionProfiles for the API?
		//public List<ConditionProfile> PreparationFrom
		//{
		//	get { return ConditionManifestExpanded.DisambiguateConditionProfiles( CredentialConnections ).PreparationFrom; }
		//}
		//public List<ConditionProfile> AdvancedStandingFrom { get { return ConditionManifestExpanded.DisambiguateConditionProfiles( CredentialConnections ).AdvancedStandingFrom; } }
		//public List<ConditionProfile> IsRequiredFor { get { return ConditionManifestExpanded.DisambiguateConditionProfiles( CredentialConnections ).IsRequiredFor; } }
		//public List<ConditionProfile> IsRecommendedFor { get { return ConditionManifestExpanded.DisambiguateConditionProfiles( CredentialConnections ).IsRecommendedFor; } }
		//public List<ConditionProfile> IsAdvancedStandingFor { get { return ConditionManifestExpanded.DisambiguateConditionProfiles( CredentialConnections ).IsAdvancedStandingFor; } }
		//public List<ConditionProfile> IsPreparationFor { get { return ConditionManifestExpanded.DisambiguateConditionProfiles( CredentialConnections ).IsPreparationFor; } }


		/// <summary>
		/// The resource being referenced must be pursued concurrently with the resource being described.
		/// </summary>

		public List<ConditionProfile> AdvancedStandingFrom { get; set; }
		public List<ConditionProfile> IsAdvancedStandingFor { get; set; }
		public List<ConditionProfile> PreparationFrom { get; set; }
		public List<ConditionProfile> IsPreparationFor { get; set; }
		public List<ConditionProfile> IsRequiredFor { get; set; }
		public List<ConditionProfile> IsRecommendedFor { get; set; }


		#endregion

		public List<RevocationProfile> Revocation { get; set; }
		
		//public int OwnerOrganizationId { get; set; }
		//public int ManagingOrgId { get; set; }
		public string CredentialId { get; set; }
		public string CodedNotation { get; set; }

		/// <summary>
		/// Identifier
		/// Definition:	Alphanumeric token that identifies this resource and information about the token's originating context or scheme.
		/// </summary>	
		public List<Entity_IdentifierValue> Identifier { get; set; } = new List<Entity_IdentifierValue>();
		//not stored yet
		public List<IdentifierValue> IdentifierNew { get; set; } = new List<IdentifierValue>();
		//or could store this as json
		//not stored yet
		public string IdentifierJSON { get; set; }

		/// <summary>
		/// Also doing import of list
		/// </summary>
		public List<Entity_IdentifierValue> VersionIdentifierList { get; set; }
		public List<IdentifierValue> VersionIdentifierNew { get; set; }
		//not stored yet
		public string VersionIdentifierJSON { get; set; }

		public List<TextValueProfile> Keyword { get; set; }
		public List<TextValueProfile> Subject { get; set; }


		public List<TextValueProfile> DegreeConcentration { get; set; }
		public List<TextValueProfile> DegreeMajor{ get; set; }
		public List<TextValueProfile> DegreeMinor{ get; set; }

		/// <summary>
		/// Only publish the related property, if the credential's type is a degree or a subclass of degree
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		//private List<CredentialAlignmentObject> IsValidDegreeType( List<TextValueProfile> data )
		//{
		//	try
		//	{
		//		var validDegreeTypes = new List<string>() { "ceterms:AssociateDegree", "ceterms:BachelorDegree", "ceterms:Degree", "ceterms:DoctoralDegree", "ceterms:MasterDegree" };
		//		if ( CredentialType.Items.Where( m => validDegreeTypes.Contains( m.SchemaName ) ).Count() > 0 )
		//		{
		//			return data.ConvertAll( m => new CredentialAlignmentObject()
		//			{
		//				TargetNodeName = m.TextValue
		//			} ).ToList();
		//		}
		//		return new List<CredentialAlignmentObject>();
		//	}
		//	catch
		//	{
		//		return new List<CredentialAlignmentObject>();
		//	}
		//}


		public List<CostProfile> EstimatedCosts { get; set; }
		public List<CostProfile> AssessmentEstimatedCosts { get; set; }
		public List<CostProfile> LearningOpportunityEstimatedCosts { get; set; }
		/// <summary>
		/// Alias used for publishing
		/// </summary>
		public List<CostProfile> EstimatedCost {
			get { return EstimatedCosts; }
			set { EstimatedCosts = value; }
		}

		/// <summary>
		/// Used for publishing
		/// </summary>
		//public List<CostProfileMerged> EstimatedCost_Merged {
		//	get { return CostProfileMerged.FlattenCosts( EstimatedCosts ); }
		//} //

		//public List<FinancialAlignmentObject> FinancialAssistanceOLD { get; set; }
		public List<FinancialAssistanceProfile> FinancialAssistance { get; set; } = new List<FinancialAssistanceProfile>();
		public string FinancialAssistanceJson { get; set; }
		//public List<CredentialAlignmentObjectProfile> RequiresCompetencies { get; set; }

		public List<AggregateDataProfile> AggregateData { get; set; } = new List<AggregateDataProfile>();
		public List<QData.DataSetProfile> ExternalDataSetProfiles { get; set; } = new List<QData.DataSetProfile>();
		public List<EarningsProfile> Earnings { get; set; } = new List<EarningsProfile>();

		public List<HoldersProfile> Holders { get; set; } = new List<HoldersProfile>();
		public List<EmploymentOutcomeProfile> EmploymentOutcome { get; set; } = new List<EmploymentOutcomeProfile>();

		/// <summary>
		/// Credentials related by a condition profile
		/// - hold, see if needed - may not as the condition types are very specific
		/// </summary>
		//public List<Credential> TargetCredential { get; set; } 
		/// <summary>
		/// Assessments related by a condition profile
		/// </summary>
		public List<AssessmentProfile> TargetAssessment { get; set; }
		/// <summary>
		/// Learning Opportunities related by a condition profile
		/// </summary>
		public List<LearningOpportunityProfile> TargetLearningOpportunity { get; set; }

		public List<Pathway> TargetPathway { get; set; } = new List<Pathway>();


		public Dictionary<string, RegistryImport> FrameworkPayloads = new Dictionary<string, RegistryImport>();
        /// <summary>
        /// processStandards (Nov2016)
        /// URL
        /// </summary>
        public string ProcessStandards { get; set; }
		/// <summary>
		/// ProcessStandardsDescription (Nov2016)
		/// </summary>
		public string ProcessStandardsDescription { get; set; }

		public List<VerificationServiceProfile> VerificationServiceProfiles { get; set; } = new List<VerificationServiceProfile>();

        public bool HasVerificationType_Badge { get; set; }

        #region import 
        //used by import
        public List<Guid> HasETPLResourceUids { get; set; } = new List<Guid>();
        //public List<TopLevelObject> HasETPLAssessments { get; set; } = new List<TopLevelObject>();
        public List<int> HasETPLAssessmentsIds { get; set; } = new List<int>();
        //public List<TopLevelObject> HasETPLCredentials{ get; set; } = new List<TopLevelObject>();
        public List<int> HasETPLCredentialsIds { get; set; } = new List<int>();
        //public List<TopLevelObject> HasETPLLopps { get; set; } = new List<TopLevelObject>();
        public List<int> HasETPLLoppsIds { get; set; } = new List<int>();
        //CostManifestId
        //hmm, need to create a placeholder CMs - try to use ints

        public List<int> CostManifestIds { get; set; }
        public List<int> ConditionManifestIds { get; set; }
        public List<int> HasSupportServiceIds { get; set; } = new List<int>();
        public List<int> TargetPathwayIds { get; set; } = new List<int>();

        public List<int> VerificationServiceProfileIds { get; set; }
        #endregion
    }
    [Serializable]
	public class CredentialExternalProperties
	{
		public List<Address> Addresses { get; set; } = new List<Address>();
	}
}

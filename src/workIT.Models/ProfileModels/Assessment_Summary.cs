﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.Common;
namespace workIT.Models.ProfileModels
{
    public class Assessment_Summary : BaseProfile
    {
        public Assessment_Summary()
        {

            AssessmentMethodType = new Enumeration();
            AssessmentUseType = new Enumeration();
            DeliveryType = new Enumeration();
            OrganizationRole = new List<OrganizationRoleProfile>();

            EstimatedDuration = new List<DurationProfile>();
            ResourceUrl = new List<TextValueProfile>();
            WhereReferenced = new List<string>();
            Subject = new List<TextValueProfile>();
            Keyword = new List<TextValueProfile>();
            Addresses = new List<Address>();
            CommonCosts = new List<CostManifest>();
            EstimatedCost = new List<CostProfile>();
            //FinancialAssistance = new List<FinancialAlignmentObject>();
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

            AssessesCompetenciesFrameworks = new List<CredentialAlignmentObjectFrameworkProfile>();

            AssessesCompetencies = new List<CredentialAlignmentObjectProfile>();
            RequiresCompetenciesFrameworks = new List<CredentialAlignmentObjectFrameworkProfile>();
            InstructionalProgramType = new Enumeration();
            Region = new List<JurisdictionProfile>();
            JurisdictionAssertions = new List<JurisdictionProfile>();
            ScoringMethodType = new Enumeration();
            OwnerRoles = new Enumeration();
            //to delete

            InLanguageCodeList = new List<TextValueProfile>();
            VersionIdentifierList = new List<Entity_IdentifierValue>();
        }
        public string Name { get; set; }
        //public string FriendlyName { get; set; }
        public int EntityStateId { get; set; }

        public System.Guid OwningAgentUid { get; set; }
        /// <summary>
        /// Inflate OwningAgentUid for display 
        /// </summary>
        public Organization OwningOrganization { get; set; }
        public string OrganizationName
        {
            get
            {
                if (OwningOrganization != null && OwningOrganization.Id > 0)
                    return OwningOrganization.Name;
                else
                    return "";
            }
        }
        public int OwningOrganizationId
        {
            get
            {
                if (OwningOrganization != null && OwningOrganization.Id > 0)
                    return OwningOrganization.Id;
                else
                    return 0;
            }
        }
        public Enumeration OwnerRoles { get; set; }
        //public List<OrganizationRoleProfile> OwnerOrganizationRoles { get; set; }

        public string CTID { get; set; }
        public string CredentialRegistryId { get; set; }
        public int InLanguageId { get; set; }
        public string InLanguage { get; set; }
        public string InLanguageCode { get; set; }
        public List<TextValueProfile> InLanguageCodeList { get; set; }

        public string CreditHourType { get; set; }
        public decimal CreditHourValue { get; set; }
        public Enumeration CreditUnitType { get; set; } //Used for publishing
        public int CreditUnitTypeId { get; set; }
        public string CreditUnitTypeDescription { get; set; }
        public decimal CreditUnitValue { get; set; }

        //=======================================

        public Enumeration AssessmentUseType { get; set; }
        public Enumeration DeliveryType { get; set; }
        public string DeliveryTypeDescription { get; set; }
        public string VerificationMethodDescription { get; set; }

        public List<OrganizationRoleProfile> OrganizationRole { get; set; }
        public List<ProcessProfile> AdministrationProcess { get; set; }
        public List<ProcessProfile> DevelopmentProcess { get; set; }
        public List<ProcessProfile> MaintenanceProcess { get; set; }

        public List<CostProfile> EstimatedCost { get; set; }

        //public List<FinancialAlignmentObject> FinancialAssistance { get; set; }

        public List<DurationProfile> EstimatedDuration { get; set; }
        public List<TextValueProfile> ResourceUrl { get; set; }
        public string AssessmentExample { get; set; }
        public string AssessmentExampleDescription { get; set; }

        public List<TextValueProfile> Subject { get; set; }
        public List<TextValueProfile> Keyword { get; set; }

        public List<CredentialAlignmentObjectProfile> AssessesCompetencies { get; set; }
        public int CompetenciesCount { get; set; }

        public List<CredentialAlignmentObjectFrameworkProfile> AssessesCompetenciesFrameworks { get; set; }
        public List<CredentialAlignmentObjectFrameworkProfile> RequiresCompetenciesFrameworks { get; set; }

        public string SubjectWebpage { get; set; }

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

        public List<string> WhereReferenced { get; set; }
        public List<ConditionProfile> IsPartOfConditionProfile { get; set; }
        public List<Credential> IsPartOfCredential { get; set; }
        public List<LearningOpportunityProfile> IsPartOfLearningOpp { get; set; }
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
        public List<ConditionProfile> AdvancedStandingFor { get; set; }
        public List<ConditionProfile> PreparationFrom { get; set; }
        public List<ConditionProfile> IsPreparationFor { get; set; }
        public List<ConditionProfile> IsRequiredFor { get; set; }
        public List<ConditionProfile> IsRecommendedFor { get; set; }
        public List<ConditionProfile> AssessmentConnections { get; set; }

        public List<CredentialAlignmentObjectProfile> InstructionalProgramTypes { get; set; }
        public Enumeration InstructionalProgramType { get; set; }

        public Enumeration AssessmentMethodType { get; set; }
        public string AssessmentOutput { get; set; }
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

        //Publishing properties
        //public override Dictionary<string, object> Publish_GetPublishableVersion()
        //{
        //	var data = base.Publish_GetPublishableVersion();

        //	return data;
        //}
    }
}

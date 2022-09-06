using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http.Headers;

using workIT.Models.ProfileModels;
using workIT.Models.QData;
namespace workIT.Models.Common
{
    [Serializable]
    public class Organization : TopLevelObject
	{
		public Organization()
		{
			EntityTypeId = 2;
			//AgentDomainType = "CredentialOrganization";
			AgentDomainTypeId = 1;

			//Address = new Address(); //see Agent
			AgentType = new Enumeration();
			ServiceType = new Enumeration();
	
			AgentSectorType = new Enumeration();

			OrganizationRole_Dept = new List<OrganizationRoleProfile>();
			OrganizationRole_Subsidiary = new List<OrganizationRoleProfile>();
			ParentOrganizations = new List<OrganizationRoleProfile>();

			OrganizationRole_Actor = new List<OrganizationRoleProfile>();
			OrganizationRole_Recipient = new List<OrganizationRoleProfile>();

			Identifiers = new Enumeration();
			VerificationServiceProfiles = new List<VerificationServiceProfile>();
			

			CreatedCredentials = new List<Credential>();
			QACredentials = new List<Credential>();

			ISQAOrganization = false;
			IsACredentialingOrg = false;
			FoundingDate = "";
			//FoundingYear = "";
			//FoundingMonth = "";
			//FoundingDay = "";
			JurisdictionAssertions = new List<JurisdictionProfile>();
			Jurisdiction = new List<JurisdictionProfile>();
			//QA
			AgentProcess = new List<ProcessProfile>();
            ReviewProcess = new List<ProcessProfile>();

			RevocationProcess = new List<ProcessProfile>();
			AppealProcess = new List<ProcessProfile>();
			ComplaintProcess = new List<ProcessProfile>();

			DevelopmentProcess = new List<ProcessProfile>();
			AdministrationProcess = new List<ProcessProfile>();
			MaintenanceProcess = new List<ProcessProfile>();

			//VerificationStatus = new List<VerificationStatus>();

			Naics = new List<string>();
			Industry = new Enumeration();
			OtherIndustries = new List<TextValueProfile>();

			HasConditionManifest = new List<ConditionManifest>();
			HasCostManifest = new List<CostManifest>();

			UserAccountRolesForThisOrganization = new List<string>();
		}


		/// <summary>
		/// Organization, CredentialOrganization or QAOrganization
		/// </summary>
		public string AgentDomainType { get; set; }
		/// <summary>
		/// 1-CredentialOrganization; 2-QAOrganization; 3-Organization
		/// </summary>
		public int AgentDomainTypeId { get; set; }

		//public int EntityStateId { get; set; }

		public string FoundingDate { get; set; }

		public int EntityId
		{
			get { return this.ParentId; }
			set { this.ParentId = value; }
		}
		//public string Image { get; set; }

		/// <summary>
		/// AgentType - (OrganizationType)
		/// </summary>
		public Enumeration AgentType { get; set; } = new Enumeration();

		public Enumeration AgentSectorType { get; set; } = new Enumeration();
		//public Enumeration AgentSectorType { get { return OrganizationSectorType; } set { OrganizationSectorType = value; } } //use for import only
		public List<TextValueProfile> AlternateNames { get; set; } = new List<TextValueProfile>();
		public List<string> AlternateName { get; set; } = new List<string>();

		///// <summary>
		///// Why do we still have a single addres in use?
		///// </summary>
		//public Address Address { get; set; } = new Address();

		public List<Address> Addresses { get; set; } = new List<Address>();
		public string AddressesJson { get; set; }
		public bool HasAnyShortRegions
		{
			get
			{
				if ( Addresses.Count == 0 )
					return false;
				else
				{
					var exists = Addresses.FirstOrDefault( a => a.HasShortRegion == true );
					if ( exists != null && exists.AddressRegion.Length > 0 )
						return true;
					else
						return false;
				}
			}
		}


		//public OrganizationExternalProperties OrganizationExternalProperties { get; set; } = new OrganizationExternalProperties();
		//public string JsonProperties { get; set; }

		/// <summary>
		/// Identifier
		/// Definition:	Alphanumeric token that identifies this resource and information about the token's originating context or scheme.
		/// </summary>	
		public List<Entity_IdentifierValue> Identifier { get; set; } = new List<Entity_IdentifierValue>();
		//or could store this as json
		public string IdentifierJson { get; set; }
		public Enumeration LifeCycleStatusType { get; set; } = new Enumeration();
		public string LifeCycleStatus { get; set; }
		public int LifeCycleStatusTypeId { get; set; }	
		/// <summary>
		/// Listing of online and/or physical locations where a credential can be pursued.
		/// </summary>
		public string AvailabilityListing { get; set; }
		public List<string> AvailabilityListings { get; set; } = new List<string>();
		//
		//public Enumeration OrganizationClaimType { get; set; }
        public Enumeration ServiceType { get; set; }

		//public string ServiceTypeOther { get; set; }

		//public bool IsThirdPartyOrganization { get; set; }

		public bool IsACredentialingOrg { get; set; }

		/// <summary>
		/// True if org does QA
		/// </summary>
		public bool ISQAOrganization { get; set; }

		#region import 
		//CostManifestId
		//hmm, need to create a placeholder CMs - try to use ints

		public List<int> CostManifestIds { get; set; }
		public List<int> ConditionManifestIds { get; set; }

		public List<Guid> ApprovedBy { get; set; }

		public List<Guid> AccreditedBy { get; set; }

		public List<Guid> RecognizedBy { get; set; }

		public List<Guid> RegulatedBy { get; set; }

		//INs
		public List<JurisdictionProfile> AccreditedIn { get; set; }

		public List<JurisdictionProfile> ApprovedIn { get; set; }


		public List<JurisdictionProfile> RecognizedIn { get; set; }

		public List<JurisdictionProfile> RegulatedIn { get; set; }

        public List<Guid> Accredits { get; set; } = new List<Guid>();
        public List<Guid> Approves { get; set; }

		public List<Guid> Offers { get; set; }
		public List<Guid> Owns { get; set; }
		public List<Guid> Recognizes { get; set; }
        public List<Guid> Regulates { get; set; } = new List<Guid>();
        public List<Guid> Renews { get; set; }

		public List<Guid> Revokes { get; set; }

		public List<Guid> ParentOrganization { get; set; }
		public List<Guid> Departments { get; set; }
		public List<Guid> SubOrganizations { get; set; }
		public List<Guid> ProvidesOutcomes { get; set; }
		#endregion
		#region Output for detail
		public List<ConditionManifest> HasConditionManifest { get; set; }
		public List<CostManifest> HasCostManifest { get; set; }
		#endregion

		public string MissionAndGoalsStatement { get; set; }
		public string MissionAndGoalsStatementDescription { get; set; }

		/// <summary>
		/// Should only be one parent, but using list for consistancy
		/// </summary>
		public List<OrganizationRoleProfile> ParentOrganizations { get; set; }
		public List<OrganizationRoleProfile> OrganizationRole_Dept { get; set; }
		//public List<TextValueProfile> Auto_OrganizationRole_Dept { get
		//	{
		//		return OrganizationRole_Dept.ConvertAll( m => new TextValueProfile() { TextValue = m.ActingAgent.CTID } );
		//	} }
		public List<OrganizationRoleProfile> OrganizationRole_Subsidiary{ get; set; }
		//public List<TextValueProfile> Auto_OrganizationRole_SubOrganization { get
		//	{
		//		return OrganizationRole_Subsidiary.ConvertAll( m => new TextValueProfile() { TextValue = m.ActingAgent.CTID } );
		//	} }

		/// <summary>
		/// roles where org is the actor, ie Accredits something
        /// this would be a third party assertion
		/// </summary>
		public List<OrganizationRoleProfile> OrganizationRole_Actor { get; set; }
		public int ActualActorRoleCount { get; set; }
		public int QAPerformedOnAssessmentsCount { get; set; }
		public int QAPerformedOnCredentialsCount { get; set; }
		public int QAPerformedOnLoppsCount { get; set; }
		public int QAPerformedOnOrganizationsCount { get; set; }

		public List<OrganizationRoleProfile> OrganizationAssertions { get; set; } = new List<OrganizationRoleProfile>();


		/// <summary>
		/// Roles where org was acted upon - that is accrdedited by another agent
		/// </summary>
		public List<OrganizationRoleProfile> OrganizationRole_Recipient { get; set; }

		//Identifiers is saved as an OrganizationProperty
		public Enumeration Identifiers { get; set; }

		public int TotalCredentials { get; set; }
		public int TotalAssessments { get; set; }
		public int TotalLopps{ get; set; }
		public int TotalCollections { get; set; }
		public int TotalFrameworks { get; set; }
		public int TotalConceptSchemes { get; set; }
		public int RenewsCredentials { get; set; }
		public int RevokesCredentials { get; set; }
		public int RegulatesCredentials { get; set; }
		//
		public int TotalTransferValueProfiles { get; set; }
		

		public int TotalPathways{ get; set; }
		public int TotalPathwaySets { get; set; }
		public int TotalCredentialsPublishedByThirdParty { get; set; }
		public int TotalOrganizationsPublishedByThirdParty { get; set; }
		public int TotalAssessmentsPublishedByThirdParty { get; set; }
		public int TotalLoppsPublishedByThirdParty { get; set; }
		public List<Credential> CreatedCredentials { get; set; }
		public List<Credential> QACredentials { get; set; }
        public List<AssessmentProfile> OwnedAssessments { get; set; } = new List<AssessmentProfile>();
        public List<LearningOpportunityProfile> OwnedLearningOpportunities { get; set; } = new List<LearningOpportunityProfile>();

        /// <summary>
        /// URL
        /// </summary>
        public string AgentPurpose { get; set; }
		public string AgentPurposeDescription { get; set; }

		//public List<JurisdictionProfile> Jurisdiction { get; set; }
		public List<JurisdictionProfile> JurisdictionAssertions { get; set; }
		public List<ProcessProfile> AgentProcess { get; set; }

		private static string GetListSpaced(string input)
		{
			return string.IsNullOrWhiteSpace( input ) ? "" : input + " ";
		}

		public List<string> Naics { get; set; }
		public List<CredentialAlignmentObjectProfile> IndustryTypes { get; set; }
		public Enumeration Industry { get; set; }
		public Enumeration IndustryType
		{
			get
			{
				return new Enumeration()
				{
					Items = new List<EnumeratedItem>()
					.Concat( Industry.Items )
					.Concat( OtherIndustries.ConvertAll( m => new EnumeratedItem() { Name = m.TextTitle, Description = m.TextValue } ) ).ToList()
				};
			}
			set { Industry = value; }
		} //Used for publishing
		public List<TextValueProfile> OtherIndustries { get; set; }
		public List<TextValueProfile> Emails { get; set; } = new List<TextValueProfile>();
		//public List<TextValueProfile> Auto_Email
		//{
		//	get
		//	{
		//		var result = new List<TextValueProfile>()
		//			.Concat( Emails ).ToList();

		//		return result;
		//	}
		//}

		public List<TextValueProfile> Keyword { get; set; } = new List<TextValueProfile>();

		public List<ContactPoint> ContactPoint { get; set; } = new List<ContactPoint>();

		public List<TextValueProfile> SameAs { get; set; } = new List<TextValueProfile>();
		//public List<TextValueProfile> Auto_SameAs { get { return SameAs; } set { SameAs = value; } } //Alias used for publishing
		public List<TextValueProfile> IdentificationCodes { get; set; } = new List<TextValueProfile>();
		public string ID_DUNS { get { return IdentificationCodes.FirstOrDefault( m => m.CodeSchema == "ceterms:duns" )?.TextValue; } }
		public string ID_FEIN { get { return IdentificationCodes.FirstOrDefault( m => m.CodeSchema == "ceterms:fein" )?.TextValue; } }
		public string ID_IPEDSID { get { return IdentificationCodes.FirstOrDefault( m => m.CodeSchema == "ceterms:ipedsID" )?.TextValue; } }
		public string ID_OPEID { get { return IdentificationCodes.FirstOrDefault( m => m.CodeSchema == "ceterms:opeID" )?.TextValue; } }
		public string ID_LEICode { get { return IdentificationCodes.FirstOrDefault( m => m.CodeSchema == "ceterms:leiCode" )?.TextValue; } }
		public string ID_ISICV4 { get { return IdentificationCodes.FirstOrDefault( m => m.CodeSchema == "ceterms:isicv4" )?.TextValue; } }
		public string ID_NECS { get { return IdentificationCodes.FirstOrDefault( m => m.CodeSchema == "ceterms:ceterms:ncesID" )?.TextValue; } }

		//used in detail page
		//21-11-23 mp this not in ctdl
		//public List<IdentifierValue> ID_AlternativeIdentifier
		//{
		//	get
		//	{
		//		return IdentificationCodes.Where( m => m.CodeSchema == "ceterms:alternativeIdentifier" ).ToList().ConvertAll( m =>
		//		new IdentifierValue()
		//		{
		//			IdentifierType = m.TextTitle,
		//			IdentifierValueCode = m.TextValue
		//		} );
		//	}
		//}

		public List<TextValueProfile> SocialMediaPages { get; set; } = new List<TextValueProfile>();

		public string SupersededBy { get; set; } //URL
		public string Supersedes { get; set; } //URL

		/// <summary>
		/// Webpage or online document that defines or explains the nature of transfer value handled by the organization.
		/// URI
		/// </summary>
		public string TransferValueStatement { get; set; }
		/// <summary>
		/// Description of the nature of transfer value handled by the organization.
		/// </summary>
		public string TransferValueStatementDescription { get; set; }

		//shouldn't need TotalDataSetProfiles and DataSetProfileCount
		public int DataSetProfileCount { get; set; }
		public List<DataSetProfile> DataSetProfiles { get; set; }
		#region Process Profiles

		public List<CodeItem> ProcessProfilesSummary { get; set; } = new List<CodeItem>();
		public List<ProcessProfile> AdministrationProcess { get; set; }
		public List<ProcessProfile> AppealProcess { get; set; }
		public List<ProcessProfile> ComplaintProcess { get; set; }
		public List<ProcessProfile> DevelopmentProcess { get; set; }
		public List<ProcessProfile> MaintenanceProcess { get; set; }
		public List<ProcessProfile> ReviewProcess { get; set; }
		public List<ProcessProfile> RevocationProcess { get; set; }

		
		
		#endregion
		//public List<VerificationStatus> VerificationStatus { get; set; }
		public int VerificationServiceProfileCount { get; set; }
		public List<VerificationServiceProfile> VerificationServiceProfiles { get; set; }

		/// <summary>
		/// Populated via Accounts System
		/// </summary>
		public List<string> UserAccountRolesForThisOrganization { get; set; }
		public bool CanCreateWidgets { get; set; }

	}
	[Serializable]
	public class OrganizationExternalProperties
	{
		public List<Address> Addresses { get; set; } = new List<Address>();
	}
}

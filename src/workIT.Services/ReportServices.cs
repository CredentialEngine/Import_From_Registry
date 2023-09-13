using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using workIT.Factories;
using workIT.Models;
using workIT.Models.Common;
using workIT.Models.Helpers.Reports;
using workIT.Utilities;

namespace workIT.Services
{
	public class ReportServices
	{
		public static ApiStatisticsSummary APISiteTotals()
		{
			//TODO - add caching
			var totals = new ApiStatisticsSummary();

			var Model = ReportServices.SiteTotals();
			totals.CommonTotals = Model;

			var reportsAreCredentialStatesSearchable = UtilityManager.GetAppKeyValue( "reportsAreCredentialStatesSearchable", false );
			//var searchURL = "http://localhost:3000/search?searchType=";
			//From https://docs.google.com/spreadsheets/d/1RaqRY_s9evMdlXUKVLh1xSNGJgqVig3Jnn1H5vlhRwg/edit#gid=0
			//var totalOrgs = Model.TotalOrganizations;
			//var totalCreds = Model.TotalCredentialsAtCurrentCtdl;
			var totalOrgs = Model.MainEntityTotals.FirstOrDefault( m => m.Id == 2 ).Totals;
			//var totalReferenceOrgs = Model.MainEntityTotals.FirstOrDefault( m => m.Name == "Organization" ).Totals;
			var totalCreds = Model.MainEntityTotals.FirstOrDefault( m => m.Id == 1 ).Totals;
			var totalAsmts = Model.MainEntityTotals.FirstOrDefault( m => m.Id == 3 ).Totals;
			var totalLopps = Model.MainEntityTotals.FirstOrDefault( m => m.Id == 7 ).Totals;
			var totalComps = Model.MainEntityTotals.FirstOrDefault( m => m.Id == 10 ).Totals;
			var totalPathways = Model.MainEntityTotals.FirstOrDefault( m => m.Id == 8 ).Totals;
			var totalTVPs = Model.MainEntityTotals.FirstOrDefault( m => m.Id == 26 ).Totals;
			var enumServices = new EnumerationServices();
			var rand = new Random();
			var states = new List<string>() { "Alabama", "Alaska", "Arizona", "Arkansas", "California", "Colorado", "Connecticut", "Delaware", "Florida", "Georgia", "Hawaii", "Idaho", "Illinois", "Indiana", "Iowa", "Kansas", "Kentucky", "Louisiana", "Maine", "Maryland", "Massachusetts", "Michigan", "Minnesota", "Mississippi", "Missouri", "Montana", "Nebraska", "Nevada", "New Hampshire", "New Jersey", "New Mexico", "New York", "North Carolina", "North Dakota", "Ohio", "Oklahoma", "Oregon", "Pennsylvania", "Rhode Island", "South Carolina", "South Dakota", "Tennessee", "Texas", "Utah", "Vermont", "Virginia", "Washington", "West Virginia", "Wisconsin", "Wyoming" };
			//

			#region collect stats
			var stats = new List<Statistic>()
			{
				#region Organizations
				/*
				* new Statistic( "Partner Organizations", "Organizations recognized as Credential Engine Partners", 98, "organizations_partners", new List<string>() { "organizations", "organizations_partners", "general" } ) { CategoryId = "7", CodeId = "2375", IsSearchabilityAllowed = true },
				* */
				new Statistic( "Total PUBLISHED Organizations", "All published organizations in the system. Reference organizations are not included.", totalOrgs, "organizations_total", new List<string>() { "organizations", "grandTotal" } ),
				//new Statistic( "Additional Reference Organizations", "An organization that is referenced by another document (for example accredited by) but not actually published is referred to as a reference organization.", totalReferenceOrgs, "organizations_total", new List<string>() { "organizations", "grandTotal" },"",2,"", false ),
				Model.GetSingleStatistic( "orgReport:IsReferenceOrg", "IsReferenceOrg", new List<string>() { "organizations", "grandTotal", "endTopSection" }, false, "Additional Reference Organizations", "An organization that is referenced by another document (for example accredited by) but not actually published, is referred to as a reference organization.", false, 5 ),
				//Model.GetSingleStatistic( "orgReport:IsReferenceOrg", "IsReferenceOrg", new List<string>() { "organizations", "general" }, false, "Reference Organizations", "An organization that is referenced by another document (for example accredited by) but not actually published is referred to as a reference organization." ),
				Model.GetSingleStatistic( "orgReport:HasCostManifest", "hasCostManifest", new List<string>() { "organizations", "general" }, true, "Has Cost Manifest", "Organizations who have cost manifest(s)" ),
				Model.GetSingleStatistic( "orgReport:HasNoCostManifests", "hasNoCostManifests", new List<string>() { "organizations", "general" }, true, "Has No Cost Manifests", "Organizations who do not have cost manifest(s)" ),
				Model.GetSingleStatistic( "orgReport:HasConditionManifest", "hasConditionManifest", new List<string>() { "organizations", "general" }, true, "Has Condition Manifest", "Organizations who have condition manifest(s)" ),
				Model.GetSingleStatistic( "orgReport:HasNoConditionManifests", "hasNoConditionManifests", new List<string>() { "organizations", "general" }, true, "Has No Condition Manifests", "Organizations who do not have condition manifest(s)" ),
				Model.GetSingleStatistic( "orgReport:HasVerificationService", "hasVerificationService", new List<string>() { "organizations", "general" }, true, "Has Verification Service", "Organizations that offer a verification service for their credentials" ),
				Model.GetSingleStatistic( "orgReport:HasNoVerificationService", "hasNoVerificationService", new List<string>() { "organizations", "general" }, true, "Has No Verification Service", "Organizations that DO NOT offer a verification service for their credentials" ),
				Model.GetSingleStatistic( "orgReport:HasSubsidiary", "hasSubsidiary", new List<string>() { "organizations", "general" }, true, "Has Subsidiary", "Organizations that have subsidiaries", true ),
				Model.GetSingleStatistic( "orgReport:HasDepartment", "hasDepartment", new List<string>() { "organizations", "general" }, true, "Has Department", "Organizations that have departments", true ),
				Model.GetSingleStatistic( "orgReport:HasIndustries", "hasIndustries", new List<string>() { "organizations", "general" }, true, "Has Industries", "Organizations that have Industries.", true ),
				#endregion

				#region Credentials
				// (including Deprecated which are excluded in the main search)
				new Statistic( "Total Credentials", "All Credentials in the system.", totalCreds, "credentials_total", new List<string>() { "credentials", "grandTotal", "endTopSection" }),
				Model.GetSingleStatistic( "credReport:AvailableOnline", "availableOnline", new List<string>() { "credentials", "general" }, true, "Available Online", "Credentials that are available online" ),
				Model.GetSingleStatistic( "credReport:HasProcessProfile", "hasProcess", new List<string>() { "credentials", "general" }, true, "Has Process Information", "Credentials that have process-related information" ),
				Model.GetSingleStatistic( "credReport:HasRevocation", "hasRevocation", new List<string>() { "credentials", "general" }, true, "Has Revocation Information", "Credentials that have revocation-related information" ),
				Model.GetSingleStatistic( "credReport:HasRenewal", "hasRenewal", new List<string>() { "credentials", "general" }, true, "Has Renewal Information", "Credentials that have renewal-related information" ),
				Model.GetSingleStatistic( "credReport:HasVerificationBadges", "hasVerificationBadges", new List<string>() { "credentials", "general" }, true, "Has Verification Badges", "Credentials that have verification badges." ),
				Model.GetSingleStatistic( "credReport:HasEmbeddedCredentials", "hasEmbeddedCredentials", new List<string>() { "credentials", "general" }, true, "Has Embedded Credentials", "Credentials that have Embedded Credentials." ),
				Model.GetSingleStatistic( "credReport:HasOccupations", "hasOccupations", new List<string>() { "credentials", "general" }, true, "Has Occupations", "Credentials that have Occupations." , true),
				Model.GetSingleStatistic( "credReport:HasIndustries", "hasIndustries", new List<string>() { "credentials", "general" }, true, "Has Industries", "Credentials that have Industries.", true ),
				Model.GetSingleStatistic( "credReport:HasCIP", "hasCIP", new List<string>() { "credentials", "general" }, true, "Has Instructional Programs", "Credentials that reference instructional Programs.", true ),
				Model.GetSingleStatistic( "credReport:HasCompetencies", "hasCompetencies", new List<string>() { "credentials", "general" }, true, "Has Competencies", "Credentials that are have any competencies." , true),
				Model.GetSingleStatistic( "credReport:IsPartOfCredential", "IsPartOfCredential", new List<string>() { "credentials", "credentialConnectionType" }, true, "Is PartOf Credential", "Credentials that are Is Part Of Other Credentials." , true ),
				Model.GetSingleStatistic( "credReport:HasConditionProfile", "HasConditionProfile", new List<string>() { "credentials", "general" }, true, "Has Condition Profile", "Credentials that have Condition Profiles.", true ),
				Model.GetSingleStatistic( "credReport:HasDurationProfile", "HasDurationProfile", new List<string>() { "credentials", "general" }, true, "Has Duration Profile", "Credentials that have Duration Profiles.", true ),
				//Requires
				Model.GetSingleStatistic( "credReport:RequiresCredential", "requiresCredential", new List<string>() { "credentials", "directConnection" }, true, "Requires Credentials", "Credentials that require at least one credential" ),
				Model.GetSingleStatistic( "credReport:RequiresCompetencies", "requiresCompetency", new List<string>() { "credentials", "directConnection" }, true , "Requires Competencies", "Credentials that require at least one competency" ),
				Model.GetSingleStatistic( "credReport:RequiresAssessment", "requiresAssessment", new List<string>() { "credentials", "directConnection" }, true, "Requires Assessment", "Credentials that require at least one assessment" ),
				Model.GetSingleStatistic( "credReport:RequiresLearningOpportunity", "requiresLearningOpportunity", new List<string>() { "credentials", "directConnection" }, true, "Requires Learning Opportunity", "Credentials that require at least one learning opportunity" ),
				//Costs
				Model.GetSingleStatistic( "credReport:HasCostProfile", "credentialHasCostProfile", new List<string>() { "credentials", "general" }, true, "Has Cost Profile", "Credentials that have Cost Profile(s)" ),
				Model.GetSingleStatistic( "credReport:ReferencesCommonConditions", "credentialReferencesCommonConditions", new List<string>() { "credentials", "general" }, true, "References Common Conditions", "Credentials that reference Common Conditions" ),
				Model.GetSingleStatistic( "credReport:ReferencesCommonCosts", "credentialReferencesCommonCosts", new List<string>() { "credentials", "general" }, true, "References Common Costs", "Credentials that reference Common Costs" ),
				Model.GetSingleStatistic( "credReport:FinancialAid", "credentialtFinancialAid", new List<string>() { "credentials", "general" }, true, "Financial Aid", "Credentials that have Financial Assistance" ),
				//Recommends
				Model.GetSingleStatistic( "credReport:RecommendsCredential", "recommendsCredential", new List<string>() { "credentials", "directConnection" }, true, "Recommends Credentials", "Credentials that recommend at least one credential" ),
				Model.GetSingleStatistic( "credReport:RecommendsAssessment", "recommendsAssessment", new List<string>() { "credentials", "directConnection" }, true, "Recommends Assessment", "Credentials that recommend at least one assessment" ),
				Model.GetSingleStatistic( "credReport:RecommendsLearningOpportunity", "recommendsLearningOpportunity", new List<string>() { "credentials", "directConnection" }, true, "Recommends Learning Opportunity", "Credentials that recommend at least one learning opportunity" ),
				#endregion

				#region Assessments
				//
				new Statistic("Total Assessments", "All Assessments in the system.", totalAsmts, "assessments_total", new List<string>() { "assessments", "grandTotal", "general", "endTopSection" }),
				Model.GetSingleStatistic( "asmtReport:AvailableOnline", "assessmentAvailableOnline", new List<string>() { "assessments", "general" }, true, "Available Online", "Assessments that are available online" ),
				Model.GetSingleStatistic( "asmtReport:RequiresCompetencies", "assessmentRequiresCompetencies", new List<string>() { "assessments", "general" }, false, "Requires Competencies", "Assessments that require competencies" ),
				Model.GetSingleStatistic( "asmtReport:AssessesCompetencies", "assessmentAssessesCompetencies", new List<string>() { "assessments", "general" }, true, "Assesses Competencies", "Assessments that assess competencies" ),
				Model.GetSingleStatistic( "asmtReport:HasCostProfile", "assessmentHasCostProfile", new List<string>() { "assessments", "general" }, true, "Has Cost Profile", "Assessments that have Cost Profile(s)" ),
				Model.GetSingleStatistic( "asmtReport:HasDurationProfile", "assessmentHasDurationProfile", new List<string>() { "assessments", "general" }, true, "Has Duration Profile", "Assessments that have Duration Profile(s)" , true),
				Model.GetSingleStatistic( "asmtReport:ReferencesCommonConditions", "assessmentReferencesCommonConditions", new List<string>() { "assessments", "general" }, true, "References Common Conditions", "Assessments that have Common Conditions" ),
				Model.GetSingleStatistic( "asmtReport:ReferencesCommonCosts", "assessmentReferencesCommonCosts", new List<string>() { "assessments", "general" }, true, "References Common Costs", "Assessments that have  Common Costs" ),
				Model.GetSingleStatistic( "asmtReport:FinancialAid", "assessmentFinancialAid", new List<string>() { "assessments", "general" }, true, "Financial Aid", "Assessments that have Financial Assistance" ),
				Model.GetSingleStatistic( "asmtReport:HasProcessProfile", "assessmenthasProcess", new List<string>() { "assessments", "general" }, true, "Has Process Information", "Assessments that have process-related information" ),
				Model.GetSingleStatistic( "asmtReport:HasOccupations", "assessmentHasOccupations", new List<string>() { "assessments", "general" }, true, "Has Occupations", "Assessments that have Occupations." , true),
				Model.GetSingleStatistic( "asmtReport:HasIndustries", "assessmentHasIndustries", new List<string>() { "assessments", "general" }, true, "Has Industries", "Assessments that have Industries.", true ),
				Model.GetSingleStatistic( "asmtReport:HasCIP", "assessmentHasCIP", new List<string>() { "assessments", "general" }, true, "Has Instructional Programs", "Assessments that reference Instructional Programs" ),
				#endregion

				#region LearningOpportunities
				//
				new Statistic("Total Learning Opportunities", "All Learning Opportunities in the system.", totalLopps, "learningOpportunities_total", new List<string>() { "learningOpportunities", "grandTotal", "general", "endTopSection" }),
				Model.GetSingleStatistic( "loppReport:AvailableOnline", "loppAvailableOnline", new List<string>() { "learningOpportunities", "general" }, true, "Available Online", "Learning Opportunities that are available online" ),
				Model.GetSingleStatistic( "loppReport:RequiresCompetencies", "loppRequiresCompetencies", new List<string>() { "learningOpportunities", "general" }, false, "Requires Competencies", "Learning Opportunities that require competencies" ),
				Model.GetSingleStatistic( "loppReport:TeachesCompetencies", "loppTeachesCompetencies", new List<string>() { "learningOpportunities", "general" }, true, "Teaches Competencies", "Learning Opportunities that teach competencies" ),
				Model.GetSingleStatistic( "loppReport:HasCostProfile", "loppHasCostProfile", new List<string>() { "learningOpportunities", "general" }, true, "Has Cost Profile", "Learning Opportunities that have Cost Profile(s)" ),
				Model.GetSingleStatistic( "loppReport:HasDurationProfile", "loppHasDurationProfile", new List<string>() { "learningOpportunities", "general" }, true, "Has Duration Profile", "Learning Opportunities that have Duration Profile(s)", true ),
				Model.GetSingleStatistic( "loppReport:ReferencesCommonConditions", "loppReferencesCommonConditions", new List<string>() { "learningOpportunities", "general" }, true, "References Common Conditions", "Learning Opportunities that have Common Conditions" ),
				Model.GetSingleStatistic( "loppReport:ReferencesCommonCosts", "loppReferencesCommonCosts", new List<string>() { "learningOpportunities", "general" }, true, "References Common Costs", "Learning Opportunities that have  Common Costs" ),
				Model.GetSingleStatistic( "loppReport:FinancialAid", "loppFinancialAid", new List<string>() { "learningOpportunities", "general" }, true, "Financial Aid", "Learning Opportunities that have Financial Aid" ),
				Model.GetSingleStatistic( "loppReport:HasProcessProfile", "loppHasProcess", new List<string>() { "learningOpportunities", "general" }, true, "Has Process Information", "Learning Opportunities that have process-related information" ),
				Model.GetSingleStatistic( "loppReport:HasOccupations", "loppHasOccupations", new List<string>() { "learningOpportunities", "general" }, true, "Has Occupations", "Learning Opportunities that have Occupations." , true),
				Model.GetSingleStatistic( "loppReport:HasIndustries", "loppHasIndustries", new List<string>() { "learningOpportunities", "general" }, true, "Has Industries", "Learning Opportunities that have Industries.", true ),
				Model.GetSingleStatistic( "loppReport:HasCIP", "loppHasCIP", new List<string>() { "learningOpportunities", "general" }, true, "Has Instructional Programs", "Learning Opportunities that reference Instructional Programs" ),
				#endregion

				#region Competency Framework
				//
				new Statistic("Total Competency Frameworks", "All Competency Frameworks in the system.", totalComps, "competencyFrameworks_total", new List<string>() { "competencyFrameworks", "grandTotal", "general","endTopSection" }),
				Model.GetSingleStatistic( "frameworkReport:Competencies", "competencyFrameworkCompetencies", new List<string>() { "competencyFrameworks", "general" }, false, "Total Competencies", "Total Competencies for all Competency Frameworks" ),
				Model.GetSingleStatistic( "frameworkReport:HasEducationLevels", "competencyFrameworkHasEducationLevels", new List<string>() { "competencyFrameworks", "frameworkAlignment" }, false, "Has Education Levels", "Competency Frameworks that have Education Levels" ),
				Model.GetSingleStatistic( "frameworkReport:HasBroadAlignment", "competencyFrameworkHasBroadAlignment", new List<string>() { "competencyFrameworks", "frameworkAlignment" }, false, "Has Broad Alignment", "Competency Frameworks with Competencies that have any Broad Alignment(s)" ),
				Model.GetSingleStatistic( "frameworkReport:HasExactAlignment", "HasExactAlignment", new List<string>() { "competencyFrameworks", "frameworkAlignment" }, false, "Has Exact Alignment", "Competency Frameworks with Competencies that have any Exact Alignment(s)" ),
				Model.GetSingleStatistic( "frameworkReport:HasMajorAlignment", "HasMajorAlignment", new List<string>() { "competencyFrameworks", "v" }, false, "Has Major Alignment", "Competency Frameworks with Competencies that have any Major Alignment(s)" ),
				Model.GetSingleStatistic( "frameworkReport:HasMinorAlignment", "HasMinorAlignment", new List<string>() { "competencyFrameworks", "frameworkAlignment" }, false, "Has Minor Alignment", "Competency Frameworks with Competencies that have any Minor Alignment(s)" ),
				Model.GetSingleStatistic( "frameworkReport:HasNarrowAlignment", "HasNarrowAlignment", new List<string>() { "competencyFrameworks", "frameworkAlignment" }, false, "Has Narrow Alignment", "Competency Frameworks with Competencies that have any Narrow Alignment(s)" ),
				Model.GetSingleStatistic( "frameworkReport:HasPrerequisiteAlignment", "HasPrerequisiteAlignment", new List<string>() { "competencyFrameworks", "frameworkAlignment" }, false, "Has Prerequisite Alignment", "Competency Frameworks with Competencies that have any Prerequisite Alignment(s)" ),
				Model.GetSingleStatistic( "frameworkReport:HasAlignFrom", "HasAlignFrom", new List<string>() { "competencyFrameworks", "frameworkAlignment" }, false, "Has 'AlignFrom' Alignments", "Competency Frameworks with Competencies that have any 'AlignFrom' Alignment(s)" ),
				Model.GetSingleStatistic( "frameworkReport:HasAlignTo", "HasAlignTo", new List<string>() { "competencyFrameworks", "frameworkAlignment" }, false, "Has 'AlignTo' Alignments", "Competency Frameworks with Competencies that have any 'AlignTo' Alignment(s)" ),
				#endregion

				#region Pathway
					//
				new Statistic("Total Pathways", "All Pathways in the system.", totalPathways, "pathways_total", new List<string>() { "pathways", "grandTotal", "general","endTopSection" }),
				Model.GetSingleStatistic( "pathwayReport:HasOccupations", "pathwayHasOccupations", new List<string>() { "pathways", "general" }, true, "Has Occupations", "Pathways that have Occupations." , true),
				Model.GetSingleStatistic( "pathwayReport:HasIndustries", "pathwayHasIndustries", new List<string>() { "pathways", "general" }, true, "Has Industries", "Pathways that have Industries.", true ),
				//no point for total, instead use indv totals
				//Model.GetSingleStatistic( "frameworkReport:PathwayComponents", "pathwayPathwayComponents", new List<string>() { "pathways", "general" }, false, "Total Pathway Components", "Total Pathway Components for all Pathways" ),
				#endregion

				#region transfervalues

				//
				new Statistic("Total Transfer Values", "All Transfer Values in the system.", totalTVPs, "Transfer Values", new List<string>() { "transfervalues", "grandTotal", "general","endTopSection" }),

				Model.GetSingleStatistic( "tvpReport:HasTransferValueForCredentials", "HasTransferValueForCredentials", new List<string>() { "transfervalues", "general" }, true, "Has Transfer Value For Credentials", "Has Transfer Value For Credentials." , false),
				Model.GetSingleStatistic( "tvpReport:HasTransferValueFromCredentials", "HasTransferValueFromCredentials", new List<string>() { "transfervalues", "general" }, true, "Has Transfer Value From Credentials", "Has Transfer Value From Credentials." , false),

				Model.GetSingleStatistic( "tvpReport:HasTransferValueForAssessments", "HasTransferValueForAssessments", new List<string>() { "transfervalues", "general" }, true, "Has Transfer Value For Assessments", "Has Transfer Value For Assessments." , false),
				Model.GetSingleStatistic( "tvpReport:HasTransferValueFromAssessments", "HasTransferValueFromAssessments", new List<string>() { "transfervalues", "general" }, true, "Has Transfer Value From Assessments", "Has Transfer Value From Assessments." , false),

				Model.GetSingleStatistic( "tvpReport:HasTransferValueForLopps", "HasTransferValueForLopps", new List<string>() { "transfervalues", "general" }, true, "Has Transfer Value For Learning Opportunities", "Has Transfer Value For Learning Opportunities." , false),
				Model.GetSingleStatistic( "tvpReport:HasTransferValueFromLopps", "HasTransferValueFromLopps", new List<string>() { "transfervalues", "general" }, true, "Has Transfer Value From Learning Opportunities", "Has Transfer Value From Learning Opportunities." , false),
				Model.GetSingleStatistic( "tvpReport:TransferValueHasDevProcess", "TransferValueHasDevProcess", new List<string>() { "transfervalues", "general" }, true, "Transfer Value Has Development Process", "Transfer Value Has Development Process." , false),
				#endregion
			};
			//Placeholder
			var statsToDevelop = new List<Statistic>
			()
			{
					new Statistic("Organizations with Quality Assurance", "Organizations which receive Quality Assurance", Ceiling( totalOrgs * 0.75 ), "organizations_withQA", new List<string>
						() { "organizations", "organizations_withQA", "general" } ),
					new Statistic("Organizations available online", "Organizations which have a significant online presence", Ceiling( totalOrgs * 0.8 ), "organizations_online", new List<string>
						() { "organizations", "organizations_online", "general" } ),
					new Statistic("Organizations with unique identifier", "Organizations which have a unique identifier such as IPEDS, OPE ID, FEIN, etc.", Ceiling( totalOrgs * 0.65 ), "organizations_uniqueID", new List<string>
						() { "organizations", "organizations_uniqueID", "general" } ),
					new Statistic("Credentials with Quality Assurance", "Credentials which receive Quality Assurance", Ceiling( totalCreds * 0.3 ), "credentials_withQA", new List<string>
						() { "credentials", "credentials_withQA", "general" }),
				};


			//Organizations by role
			stats = stats.Concat( Model.GetStatisticsByEntity( "Organization", "agentRole", "organizationAgentRole", new List<string>
				() { "organizations", "organizationAgentRole" }, false, true ) ).ToList();


			//Organizations by type
			stats = stats.Concat( Model.GetStatistics( "ceterms:OrganizationType", "orgType", new List<string>
				() { "organizations", "orgType" } ) ).ToList();

			//Organizations by sector
			stats = stats.Concat( Model.GetStatistics( "ceterms:AgentSector", "organizationSector", new List<string>
				() { "organizations", "organizationSector" } ) ).ToList();

			//Organizations by service type
			stats = stats.Concat( Model.GetStatistics( "ceterms:AgentServiceType", "organizationService", new List<string>
				() { "organizations", "organizationService" } ) ).ToList();

			//Organizations by identity type
			stats = stats.Concat( Model.GetStatistics( "identityType", "organizationIdentity", new List<string>
				() { "organizations", "organizationIdentity" }, false, false ) ).ToList();

			//Organizations by industry group
			stats = stats.Concat( Model.GetStatisticsByEntity( "Organization", "ceterms:industryType", "organizationIndustryGroup", new List<string>
				() { "organizations", "organizationIndustryGroup" }, false, false ) ).ToList();

			//Organizations by History
			//stats = stats.Concat( Model.GetStatistics( "history", "organizationHistory", new List<string>	() { "organizations", "organizationHistory" }, false, false ) ).ToList();

			//Organizations by social media type - codes are not used,
			//stats = stats.Concat( Model.GetStatistics( "sameAs", "organizationSocialMedia", new List<string>	() { "organizations", "organizationSocialMedia" }, false, false ) ).ToList();

			//Organizations by verification profile
			stats = stats.Concat( Model.GetStatisticsByEntity( "VerificationProfile", "ctdl:ClaimType", "organizationVerifyType", new List<string>
				() { "organizations", "organizationVerifyType" }, true, true ) ).ToList();

			//Organizations by state
			foreach ( var state in states )
			{
				statsToDevelop.Add( new Statistic( state, "Organizations that have a significant presence in " + state, rand.Next( totalOrgs ), "organizationState_" + state.Replace( " ", "_" ).ToLower(), new List<string>
					() { "organizations", "organizationState" } ) );
			}

			//Credentials by role
			stats = stats.Concat( Model.GetStatisticsByEntity( 1, "agentRole", "credentialAgentRole", new List<string>
				() { "credentials", "credentialAgentRole" }, false, true ) ).ToList();

			//Credentials by type - this will be different than for the publisher, not in entity.property
			stats = stats.Concat( Model.GetStatistics( "ceterms:credentialType", "credentialType", new List<string>
				() { "credentials", "credentialType" } ) ).ToList();

			//Credentials by audience type (Note - combining all instances; Note - Credential does not have audience type - are these supposed to come from condition profile (presumably only "requires")?)
			stats = stats.Concat( Model.GetStatisticsByEntity( "Entity. ConditionProfile", "ceterms:Audience", "credentialAudience", new List<string>
				() { "credentials", "credentialAudienceType" } ) ).ToList();

			//Credentials by cost type (Note - combining all cost instances; Note - not supposed to include this by type)
			//stats = stats.Concat( Model.GetStatistics( "ceterms:CostType", "costType", new List<string>	() { "credentials", "credentialCostType" } ) ).ToList();

			//Credentials by audience level
			stats = stats.Concat( Model.GetStatisticsByEntity( 1, "ceterms:AudienceLevel", "audienceLevel", new List<string>
				() { "credentials", "credentialAudienceLevel" } ) ).ToList();
			stats = stats.Concat( Model.GetStatisticsByEntity( 1, "ceterms:Audience", "credentialAudienceType", new List<string>
				() { "credentials", "credentialAudienceType" } ) ).ToList();
			stats = stats.Concat( Model.GetStatisticsByEntity( 1, "ceterms:AssessmentDelivery", "credentialAssessmentDelivery", new List<string>
				() { "credentials", "credentialAssessmentDelivery" } ) ).ToList();
			stats = stats.Concat( Model.GetStatisticsByEntity( 1, "ceterms:Delivery", "credentialLearningDelivery", new List<string>
				() { "credentials", "credentialLearningDelivery" } ) ).ToList();
			//Credentials by state
			foreach ( var state in states )
			{
				statsToDevelop.Add( new Statistic( state, "Credentials recognized in " + state, rand.Next( totalCreds ), "credentialState_" + state.Replace( " ", "_" ).ToLower(), new List<string>
					() { "credentials", "credentialState" } ) );
			}

			//Credentials by connection type
			var connStats = Model.GetStatisticsByEntity( 1, "conditionProfile", "credentialConnectionType", new List<string>
				() { "credentials", "credentialConnectionType" }, false );
			foreach ( var conn in connStats )
			{
				stats.Add( new Statistic( conn.Title, "Credentials with a \"" + conn.Title + "\" connection to other entities", conn.Value, conn.Id, conn.Tags, conn.CategoryId, conn.CodeId, true ) );
			}

			//Credentials by status type
			stats = stats.Concat( Model.GetStatistics( "ceterms:CredentialStatus", "credentialStatusType", new List<string>
				() { "credentials", "credentialStatusType" } ) ).ToList();

			//Credentials by SOC and NAICS
			//stats = stats.Concat( Model.GetStatistics( "ceterms:occupationType", "credentialSocGroup", new List<string>	() { "credentials", "credentialSocGroup" }, false, false ) ).ToList();
			//stats = stats.Concat( Model.GetStatistics( "ceterms:industryType", "credentialIndustryGroup", new List<string>	() { "credentials", "credentialIndustryGroup" }, false, false ) ).ToList();

			stats = stats.Concat( Model.GetStatisticsByEntity( 1, "ceterms:occupationType", "credentialSocGroup", new List<string>
				() { "credentials", "credentialSocGroup" }, false, false ) ).ToList();
			stats = stats.Concat( Model.GetStatisticsByEntity( 1, "ceterms:industryType", "credentialIndustryGroup", new List<string>
				() { "credentials", "credentialIndustryGroup" }, false, false ) ).ToList();
			stats = stats.Concat( Model.GetStatisticsByEntity( 1, "ceterms:instructionalProgramType", "credentialCIPGroup", new List<string>
				() { "credentials", "credentialCIPGroup" }, false, false ) ).ToList();
			//this country, region could be confusing
			stats = stats.Concat( Model.GetStatisticsByEntityRegion( 1, "United States", "credentialRegions", new List<string>
				() { "credentials", "credentialRegions" }, false, reportsAreCredentialStatesSearchable ) ).ToList();
			//
			//stats = stats.Concat( Model.GetStatistics( "ctdl:NaicsGroup", "organizationIndustryGroup", new List<string>	() { "credentials", "organizationIndustryGroup" }, false, false ) ).ToList();
			//stats = stats.Concat( Model.GetStatistics( "ceterms:industryType", "credentialIndustryGroup", new List<string>	() { "credentials", "credentialIndustryGroup" }, false, false ) ).ToList();
			//stats = stats.Concat( Model.GetStatisticsByEntity( 1, "ceterms:industryType", "credentialIndustryGroup", new List<string>	() { "credentials", "credentialIndustryGroup" }, false, false ) ).ToList();

			//Assessments by role
			stats = stats.Concat( Model.GetStatisticsByEntity( 3, "agentRole", "assessmentAgentRole", new List<string>
		() { "assessments", "assessmentAgentRole" }, false, true ) ).ToList();

			//Assessments by method
			stats = stats.Concat( Model.GetStatisticsByEntity( 3, "ceterms:AssessmentMethod", "assessmentMethod", new List<string>
				() { "assessments", "assessmentMethodType" } ) ).ToList();

			//Assessments by use
			stats = stats.Concat( Model.GetStatisticsByEntity( 3, "ceterms:AssessmentUse", "assessmentUse", new List<string>
				() { "assessments", "assessmentUseType" } ) ).ToList();

			//Assessments by delivery type
			stats = stats.Concat( Model.GetStatisticsByEntity( 3, "ceterms:Delivery", "assessmentDelivery", new List<string>
				() { "assessments", "assessmentDelivery" } ) ).ToList();

			//Assessments by scoring method
			stats = stats.Concat( Model.GetStatisticsByEntity( 3, "ceterms:ScoringMethod", "assessmentScoringMethod", new List<string>
				() { "assessments", "assessmentScoringMethodType" } ) ).ToList();

			//Assessments by audience
			stats = stats.Concat( Model.GetStatisticsByEntity( 3, "ceterms:Audience", "assessmentAudienceType", new List<string>
				() { "assessments", "assessmentAudienceType" } ) ).ToList();

			//Assessment Occupation and Industry groups
			stats = stats.Concat( Model.GetStatisticsByEntity( 3, "ceterms:occupationType", "assessmentSocGroup", new List<string>
				() { "assessments", "assessmentSocGroup" }, false, false ) ).ToList();
			stats = stats.Concat( Model.GetStatisticsByEntity( 3, "ceterms:industryType", "assessmentIndustryGroup", new List<string>
				() { "assessments", "assessmentIndustryGroup" }, false, false ) ).ToList();

			//stats = stats.Concat( Model.GetStatisticsByEntity( 3, "ceterms:instructionalProgramType", "assessmentCIPGroup", new List<string>	() { "assessments", "assessmentCIPGroup" }, false, false ) ).ToList();
			//stats = stats.Concat( Model.GetStatistics( "ceterms:instructionalProgramType", "assessmentCIPGroup", new List<string>	() { "assessments", "assessmentCIPGroup" }, false, false ) ).ToList();
			stats = stats.Concat( Model.GetStatisticsByEntity( 3, "ceterms:instructionalProgramType", "assessmentCIPGroup", new List<string>
				() { "assessments", "assessmentCIPGroup" }, false, false ) ).ToList();

			//Learning Opportunities by role
			stats = stats.Concat( Model.GetStatisticsByEntity( 7, "agentRole", "learningOpportunityAgentRole", new List<string>
				() { "learningOpportunities", "learningOpportunityAgentRole" }, false, true ) ).ToList();

			//Learning Opportunities by method (Note - erroneous \t character in schema)
			stats = stats.Concat( Model.GetStatistics( "\tceterms:LearningMethod", "learningMethodType", new List<string>
				() { "learningOpportunities", "learningMethodType" } ) ).ToList();
			stats = stats.Concat( Model.GetStatistics( "ceterms:LearningMethod", "learningMethodType", new List<string>
				() { "learningOpportunities", "learningMethodType" } ) ).ToList();

			//Learning Opportunities by audience
			stats = stats.Concat( Model.GetStatisticsByEntity( 7, "ceterms:Audience", "learningOpportunityAudienceType", new List<string>
				() { "learningOpportunities", "learningOpportunityAudienceType" } ) ).ToList();

			stats = stats.Concat( Model.GetStatisticsByEntity( 7, "ceterms:AudienceLevel", "learningOpportunityAudienceLevel", new List<string>
			() { "learningOpportunities", "learningOpportunityAudienceLevel" } ) ).ToList();

			//Learning Opportunities by delivery method
			stats = stats.Concat( Model.GetStatisticsByEntity( 7, "ceterms:Delivery", "deliveryType", new List<string>
				() { "learningOpportunities", "deliveryType" } ) ).ToList();

			//Learning Occupation and Industry group
			stats = stats.Concat( Model.GetStatisticsByEntity( 7, "ceterms:occupationType", "learningOpportunitySocGroup", new List<string>
				() { "learningOpportunities", "learningOpportunitySocGroup" }, false, false ) ).ToList();
			stats = stats.Concat( Model.GetStatisticsByEntity( 7, "ceterms:industryType", "learningOpportunityIndustryGroup", new List<string>
				() { "learningOpportunities", "learningOpportunityIndustryGroup" }, false, false ) ).ToList();

			//stats = stats.Concat( Model.GetStatisticsByEntity( 7, "ceterms:instructionalProgramType", "learningOpportunityCIPGroup", new List<string>	() { "learningOpportunities", "learningOpportunityCIPGroup" }, false, false ) ).ToList();
			//stats = stats.Concat( Model.GetStatistics( "ceterms:instructionalProgramType", "learningOpportunityCIPGroup", new List<string>	() { "learningOpportunities", "learningOpportunityCIPGroup" }, false, false ) ).ToList();
			stats = stats.Concat( Model.GetStatisticsByEntity( 7, "ceterms:instructionalProgramType", "learningOpportunityCIPGroup", new List<string>
				() { "learningOpportunities", "learningOpportunityCIPGroup" }, false, false ) ).ToList();
			//TODO: add the rest of the organization, credential, assessment, and learning opportunity related items

			//PathwayComponents by type
			stats = stats.Concat( Model.GetStatistics( "ceterms:PathwayComponentType", "pathwayComponentType", new List<string>
				() { "pathways", "pathwayComponentType" }, false, false ) ).ToList();
			//Assessment Occupation and Industry groups
			stats = stats.Concat( Model.GetStatisticsByEntity( 3, "ceterms:occupationType", "assessmentSocGroup", new List<string>
				() { "pathways", "assessmentSocGroup" }, false, false ) ).ToList();
			stats = stats.Concat( Model.GetStatisticsByEntity( 3, "ceterms:industryType", "assessmentIndustryGroup", new List<string>
				() { "pathways", "assessmentIndustryGroup" }, false, false ) ).ToList();
			#endregion

			//filter out empty stats
			stats = stats.Where( s => s.Title != null ).ToList();

			totals.EntitySummary = new List<EntitySummary>()
			{
				new EntitySummary()
				{
					ReportType="Credential",
					Statistics = stats.Where( m => m.Tags.Contains( "credentials" )).ToList()
				},
				new EntitySummary()
				{
					ReportType="Organization",
					Statistics = stats.Where( m => m.Tags.Contains( "organizations" )).ToList()
				},
				new EntitySummary()
				{
					ReportType="Assessments",
					Statistics = stats.Where( m => m.Tags.Contains( "assessments" )).ToList()
				},
				new EntitySummary()
				{
					ReportType="Learning Opportunities",
					Statistics = stats.Where( m => m.Tags.Contains( "learningOpportunities" )).ToList()
				},
				new EntitySummary()
				{
					ReportType="Competency Frameworks",
					Statistics = stats.Where( m => m.Tags.Contains( "competencyframeworks" )).ToList()
				},
				new EntitySummary()
				{
					ReportType="Pathways",
					Statistics = stats.Where( m => m.Tags.Contains( "pathways" )).ToList()
				},
				new EntitySummary()
				{
					ReportType="Transfer Values",
					Statistics = stats.Where( m => m.Tags.Contains( "transfervalues" )).ToList()
				}
			};

			//???????????????
			return totals;


		}

		public static List<BenchmarkPropertyTotal> APIBenchmarksSummary( BenchmarkQuery request)
		{
			//TODO	- add caching as only changes daily - especially if not exposing filtering.
			//		- OR get all and cache

			bool IsDescending = false;
			var filter = "";
			string OR = "";

			if (!string.IsNullOrWhiteSpace(request.LabelFilter))
			{
				if ( request.LabelFilter.IndexOf( "!" ) == 0 )
					filter += OR + string.Format( "({0} NOT LIKE '%{1}%')", "Label", request.LabelFilter.Trim() );
				else
					filter += OR + string.Format( "({0} LIKE '%{1}%')", "Label", request.LabelFilter.Trim() );
			}
			if ( !string.IsNullOrWhiteSpace( request.PolicyFilter ) )
			{
				if ( request.PolicyFilter.IndexOf( "!" ) == 0 )
					filter += OR + string.Format( "({0} NOT LIKE '%{1}%')", "Policy", request.PolicyFilter.Trim() );
				else
					filter += OR + string.Format( "({0} LIKE '%{1}%')", "Policy", request.PolicyFilter.Trim() );
			}
			var totalRecords = 0;
			var list = ReportServices.Search( request.SearchType, filter, request.SortOrder, request.IsDescending, request.PageNumber, request.PageSize, ref totalRecords );

			return list;
		}
		public static int Ceiling( double d )
		{
			return ( int )Math.Ceiling( ( decimal )d );
		}

		public static string Identify( string prefix, EnumeratedItem item )
		{
			return prefix + "_" + ( item.SchemaName ?? "" ).Replace( ":", "_" );
		}

		public static string Identify( string prefix, CodeItem item )
		{
			return prefix + "_" + ( item.SchemaName ?? "" ).Replace( ":", "_" );
		}
		public static CommonTotals SiteTotals()
		{
            var currentDate = DateTime.Now;
            currentDate = currentDate.AddDays( -2 );
			//
			CommonTotals totals = new CommonTotals();// ActivityManager.SiteTotals_Get();
			totals.MainEntityTotals = MainEntityTotals();
            totals.CredentialHistory = HistoryReports( 1);
            totals.OrganizationHistory = HistoryReports( 2 );
            totals.AssessmentHistory = HistoryReports( 3 );
            totals.LearningOpportunityHistory = ReportServices.HistoryReports( 7 );
			totals.PathwayHistory = ReportServices.HistoryReports( 8 );
			totals.CompetencyFrameworkHistory = ReportServices.HistoryReports( 10 );
			totals.TransferValueHistory = ReportServices.HistoryReports( CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE );

			totals.EntityRegionTotals = CodesManager.GetEntityRegionTotals( 1, "United States");
			//vm.TotalDirectCredentials = list.FirstOrDefault( x => x.Id == 1 ).Totals;
			//vm.TotalOrganizations = list.FirstOrDefault( x => x.Id == 2 ).Totals;
			//vm.TotalQAOrganizations = list.FirstOrDefault( x => x.Id == 99 ).Totals;
			//21-07-30 mp - why is AgentServices type added separately? It could be due to use outside of Codes.PropertyValue.
			totals.AgentServiceTypes = new EnumerationServices().GetOrganizationServices(EnumerationType.MULTI_SELECT, false);

			totals.PropertiesTotals = PropertyTotals();
			//
			//get totals from view: CodesProperty_Counts_ByEntity.
			//	the latter has a union with Counts.SiteTotals
			totals.PropertiesTotalsByEntity = CodesManager.Property_GetTotalsByEntity();
            totals.PropertiesTotals.AddRange( CodesManager.GetAllEntityStatistics());
			totals.PropertiesTotals.AddRange( CodesManager.GetAllPathwayComponentStatistics() );

			
			//using counts.SiteTotals - so based on the above, this should not be needed???
			//var allSiteTotals = CodesManager.CodeEntity_GetCountsSiteTotals();
			//totals.SOC_Groups = allSiteTotals.Where( s => s.EntityTypeId == 1 && s.CategoryId == 11 ).ToList();
			//totals.CredentialIndustry_Groups = allSiteTotals.Where( s => s.EntityTypeId == 1 && s.CategoryId == 10 ).ToList();
			//totals.CredentialCIP_Groups = allSiteTotals.Where( s => s.EntityTypeId == 3 && s.CategoryId == 23 ).ToList();
			//totals.OrgIndustry_Groups = allSiteTotals.Where( s => s.EntityTypeId == 2 && s.CategoryId == 10 ).ToList();
			//totals.AssessmentCIP_Groups = allSiteTotals.Where( s => s.EntityTypeId == 3 && s.CategoryId == 23 ).ToList();
			//totals.LoppCIP_Groups = allSiteTotals.Where( s => s.EntityTypeId == 7 && s.CategoryId == 23 ).ToList();

			return totals;
		}

        public static List<HistoryTotal> HistoryReports( int entityTypeId )
        {
            var result = CodesManager.GetHistoryTotal( entityTypeId );
            return result;


        }

		/// <summary>
		/// Get Entity Codes with totals for top level entities like: Credential, Organization, assessments, and learning opp
		/// </summary>
		/// <returns></returns>
		public static List<CodeItem> MainEntityTotals( bool gettingAll = true, string onlyTheseEntities = "")
		{
			List<CodeItem> list = CodesManager.CodeEntity_GetTopLevelEntity( gettingAll, onlyTheseEntities );

			return list;
		}

		/// <summary>
		/// Get property totals, by category or all active properties
		/// </summary>
		/// <param name="categoryId"></param>
		/// <returns></returns>
		public static List<CodeItem> PropertyTotals( int categoryId = 0)
		{
			List<CodeItem> list = CodesManager.Property_GetSummaryTotals( categoryId );

			return list;
		}

		public static List<BenchmarkPropertyTotal> Search( string classType, string pFilter, string pOrderBy, bool IsDescending, int pageNumber, int pageSize, ref int pTotalRows )
		{

			//probably should validate valid order by - or do in proc
			if ( string.IsNullOrWhiteSpace( pOrderBy ) )
			{
				//not handling desc yet				
				//parms.IsDescending = true;
				pOrderBy = "Id";
			}
			else
			{
				if ( pOrderBy == "Order" )
					pOrderBy = "Id";
				else if ( pOrderBy == "Title" )
					pOrderBy = "Label";
				else if ( pOrderBy == "CodeTitle" )
					pOrderBy = "Policy";
				else if ( pOrderBy == "Group" )
					pOrderBy = "PropertyGroup";
				else if ( pOrderBy == "Totals" )
					pOrderBy = "Total";

				if ( "id label policy propertygroup total".IndexOf( pOrderBy.ToLower() ) == -1 )
				{
					pOrderBy = "Id";
				}
			}
			if ( IsDescending )
				pOrderBy += " DESC";

			var list = ReportsManager.Search( classType, pFilter, pOrderBy, pageNumber, pageSize, ref pTotalRows );
			return list;
		}

		public static void PopulateDuplicatesReports()
		{
			new QueryManager().PopulateReportsDuplicates();
			
		}

	}
}

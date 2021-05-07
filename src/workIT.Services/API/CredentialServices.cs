using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using workIT.Models;
using workIT.Models.Common;
using WMP= workIT.Models.ProfileModels;
using WMA = workIT.Models.API;
using workIT.Models.Search;
using workIT.Factories;
using workIT.Utilities;
using ElasticHelper = workIT.Services.ElasticServices;
using EntityHelper = workIT.Services.CredentialServices;
using WorkITSearchServices = workIT.Services.SearchServices;

using ThisEntity = workIT.Models.Common.Credential;
using ThisEntityDetail = workIT.Models.API.CredentialDetail;
using ThisSearchEntity = workIT.Models.Common.CredentialSummary;
using EntityMgr = workIT.Factories.CredentialManager;



namespace workIT.Services.API
{
	public class CredentialServices
	{
		static string thisClassName = "API.CredentialServices";
		public static string externalFinderSiteURL = UtilityManager.GetAppKeyValue( "externalFinderSiteURL" );
		public static string searchType = "credential";

		public static WMA.CredentialDetail GetDetailForAPI( int id, bool skippingCache = false )
		{
			CredentialRequest cr = new CredentialRequest();
			cr.IsDetailRequest();
			cr.IncludingProcessProfiles = false;
			var entity = EntityHelper.GetDetail( id, cr, skippingCache );
			return MapToAPI( entity );

		}
		public static WMA.CredentialDetail GetDetailByCtidForAPI( string ctid, bool skippingCache = false )
		{
			var credential = EntityMgr.GetMinimumByCtid( ctid );
			return GetDetailForAPI( credential.Id, skippingCache );
			//CredentialRequest cr = new CredentialRequest();
			//cr.IsDetailRequest();
			//cr.IncludingProcessProfiles = false;
			//var entity = EntityHelper.GetDetail( credential.Id, skippingCache );

			//return MapToAPI( entity );
		}
		private static WMA.CredentialDetail MapToAPI( ThisEntity input )
		{

			var output = new WMA.CredentialDetail()
			{
				Meta_Id = input.Id,
				CTID = input.CTID,
				Name = input.Name,
				Description = input.Description,
				SubjectWebpage = input.SubjectWebpage,
				EntityTypeId = 1,
				CTDLTypeLabel = input.CredentialType,
				CTDLType = input.CredentialTypeSchema,
				CredentialRegistryURL = RegistryServices.GetResourceUrl(input.CTID),
				RegistryData = ServiceHelper.FillRegistryData( input.CTID )

			};
			//
			//output.CTDLType = record.CredentialType; ;
			//output.AgentSectorType = ServiceHelper.MapPropertyLabelLinks( org.AgentSectorType, "organization" );
			output.FriendlyName = HttpUtility.UrlPathEncode( input.Name );
			output.AlternateName = input.AlternateName;

			//owned by and offered by 
			//need a label link for header
			if ( input.OwningOrganizationId > 0 )
			{
				output.OwnedByLabel = ServiceHelper.MapDetailLink( "Organization", input.OrganizationName, input.OwningOrganizationId );
			}
			var work = ServiceHelper.MapOrganizationRoleProfileToOutline( input.OrganizationRole, Entity_AgentRelationshipManager.ROLE_TYPE_OWNER );
			output.OwnedBy = ServiceHelper.MapOutlineToAJAX( work, "" );
			//
			work = ServiceHelper.MapOrganizationRoleProfileToOutline( input.OrganizationRole, Entity_AgentRelationshipManager.ROLE_TYPE_OFFERED_BY );
			output.OfferedBy = ServiceHelper.MapOutlineToAJAX( work, "Offered by {0} Organization(s)" );
			//

			//QA for owner,not offerer
			if ( input.OwningOrganization != null && input.OwningOrganization.Id > 0 )
			{
				if ( input.OwningOrganization.OrganizationRole_Recipient != null && input.OwningOrganization.OrganizationRole_Recipient.Any() )
				{
					output.OwnerQAReceived = ServiceHelper.MapQAReceived( input.OwningOrganization.OrganizationRole_Recipient, searchType );
				}
				//var inheritedRoles = SetupRoles( roleSet.ActingAgent.OrganizationRole_Recipient, loadedAgentIDs );
				//wrapper.QAFromOwner = inheritedRoles.QADirect;
			}
			//
			output.Meta_LastUpdated = input.EntityLastUpdated;
			output.Meta_StateId = input.EntityStateId;
			output.EntityTypeId = input.EntityTypeId;
			if ( input.InLanguageCodeList != null && input.InLanguageCodeList.Any() )
			{
				//output.Meta_Language = input.InLanguageCodeList[ 0 ].TextTitle;
				output.InLanguage = new List<string>();
				foreach ( var item in input.InLanguageCodeList )
				{
					output.InLanguage.Add( item.TextTitle );
				}
			}
			try
			{
				if ( input.HasVerificationType_Badge )
					output.CTDLTypeLabel += " + Badge Issued";
				output.Image = input.Image;
				//
				if ( !string.IsNullOrWhiteSpace( input.AvailabilityListing ) )
					output.AvailabilityListing = new List<string>() { input.AvailabilityListing };
				if ( !string.IsNullOrWhiteSpace( input.AvailableOnlineAt ) )
					output.AvailableOnlineAt = new List<string>() { input.AvailableOnlineAt };

				if ( input.CopyrightHolderOrganization != null && input.CopyrightHolderOrganization.Any() )
				{
					output.CopyrightHolder = new List<WMA.Outline>();
					//output.CopyrightHolder2 = new List<WMA.LabelLink>();
					foreach ( var target in input.CopyrightHolderOrganization )
					{
						if ( target != null && target.Id > 0 )
						{
							//TODO - add overload to only get minimum data - like Link
							output.CopyrightHolder.Add( ServiceHelper.MapToOutline( target, "organization" ) );

							//var link = ServiceHelper.MapDetailLink( "organization", target.Name, target.Id );
							//output.CopyrightHolder2.Add( link );
						}
					}
					//or Link objects

				}

				output.CredentialStatusType = ServiceHelper.MapPropertyLabelLink( input.CredentialStatusType, searchType );
				output.CredentialType = ServiceHelper.MapPropertyLabelLink( input.CredentialTypeEnum, searchType );

				output.DateEffective = input.DateEffective;
				output.ExpirationDate = input.ExpirationDate;
				output.EstimatedDuration = ServiceHelper.MapDurationProfiles( input.EstimatedDuration );
				output.RenewalFrequency = ServiceHelper.MapDurationItem( input.RenewalFrequency );
				//
				if ( input.DegreeConcentration != null && input.DegreeConcentration.Any() )
					output.DegreeConcentration = ServiceHelper.MapPropertyLabelLinks( input.DegreeConcentration, searchType );
				if ( input.DegreeMajor != null && input.DegreeMajor.Any() )
					output.DegreeMajor = ServiceHelper.MapPropertyLabelLinks( input.DegreeMajor, searchType );
				if ( input.DegreeMinor != null && input.DegreeMinor.Any() )
					output.DegreeMinor = ServiceHelper.MapPropertyLabelLinks( input.DegreeMinor, searchType );
				//
				if ( input.EmbeddedCredentials != null && input.EmbeddedCredentials.Any() )
				{
					output.HasPart2 = new List<WMA.Outline>();
					foreach ( var target in input.EmbeddedCredentials )
					{
						if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
							output.HasPart2.Add( ServiceHelper.MapToOutline( target, searchType ) );
					}
					output.HasPart = ServiceHelper.MapOutlineToAJAX( output.HasPart2, "Includes {0} Credential(s)" );
					output.HasPart2 = null;

				}
				//
				if ( input.IsPartOf != null && input.IsPartOf.Any() )
				{
					output.IsPartOf2 = new List<WMA.Outline>();
					foreach ( var target in input.IsPartOf )
					{
						if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
							output.IsPartOf2.Add( ServiceHelper.MapToOutline( target, searchType ) );
					}
					output.IsPartOf = ServiceHelper.MapOutlineToAJAX( output.IsPartOf2, "Is Part of {0} Credential(s)" );
					output.IsPartOf2 = null;
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( thisClassName + ".MapToAPI. Section 1, Name: {0}, Id: [1}", input.Name, input.Id ) );

			}

			//addresses
			//MapAddress( input, ref output );
			output.AvailableAt = ServiceHelper.MapAddress( input.Addresses );

			//
			output.Image = input.Image;
			if(!string.IsNullOrWhiteSpace( input.CredentialTypeSchema ) )
				output.Meta_Icon = WorkITSearchServices.GetCredentialIcon( input.CredentialTypeSchema.ToLower() );
			//
			output.IndustryType = ServiceHelper.MapReferenceFrameworkLabelLink( input.IndustryType, searchType, CodesManager.PROPERTY_CATEGORY_NAICS );
			output.OccupationType = ServiceHelper.MapReferenceFrameworkLabelLink( input.OccupationType, searchType, CodesManager.PROPERTY_CATEGORY_SOC );
			output.InstructionalProgramType = ServiceHelper.MapReferenceFrameworkLabelLink( input.InstructionalProgramType, searchType, CodesManager.PROPERTY_CATEGORY_CIP );
			//
			output.IsReferenceVersion = input.IsReferenceVersion;
			//
			if ( input.Keyword != null && input.Keyword.Any() )
				output.Keyword = ServiceHelper.MapPropertyLabelLinks( input.Keyword, searchType );
			if ( input.Subject != null && input.Subject.Any() )
				output.Subject = ServiceHelper.MapPropertyLabelLinks( input.Subject, searchType );
			//
			output.AssessmentDeliveryType = ServiceHelper.MapPropertyLabelLinks( input.AssessmentDeliveryType, searchType );
			output.AudienceLevelType = ServiceHelper.MapPropertyLabelLinks( input.AudienceLevelType, searchType );
			output.AudienceType = ServiceHelper.MapPropertyLabelLinks( input.AudienceType, searchType );
			output.LearningDeliveryType = ServiceHelper.MapPropertyLabelLinks( input.LearningDeliveryType, searchType );

			//
			//condition profiles
			try
			{
				output.Corequisite = ServiceHelper.MapToConditionProfiles( input.Corequisite, searchType );
				output.Recommends = ServiceHelper.MapToConditionProfiles( input.Recommends, searchType );
				output.Renewal = ServiceHelper.MapToConditionProfiles( input.Renewal, searchType );
				output.Requires = ServiceHelper.MapToConditionProfiles( input.Requires, searchType );
				if ( input.CommonConditions != null && input.CommonConditions.Any() )
				{
					//these will likely just be mapped to specific conditions
					output.CommonConditions = ServiceHelper.MapConditionManifests( input.CommonConditions, searchType );
					if ( output.CommonConditions != null && output.CommonConditions.Any() )
					{
						foreach ( var item in output.CommonConditions )
						{
							if ( item.Requires != null && item.Requires.Any() )
							{
								output.Requires = AppendConditions( item.Requires, output.Requires );
							}
							if ( item.Recommends != null && item.Recommends.Any() )
							{
								output.Recommends = AppendConditions( item.Recommends, output.Recommends );
							}
							if ( item.Corequisite != null && item.Corequisite.Any() )
							{
								output.Corequisite = AppendConditions( item.Requires, output.Corequisite );
							}
							if ( item.Renewal != null && item.Renewal.Any() )
							{
								output.Renewal = AppendConditions( item.Renewal, output.Renewal );
							}
						}
					}
				}
				//connection profiles
				if ( input.CredentialConnections != null && input.CredentialConnections.Any() )
				{
					//var list = input.CredentialConnections.Where( s => s.ConditionSubTypeId == 2 && s.ConnectionProfileTypeId == Entity_ConditionProfileManager.ConnectionProfileType_AdvancedStandingFor ).ToList();
					//foreach ( var item in input.CredentialConnections )
					//{
					//	//some default for 1??
					//	if ( item.ConditionSubTypeId == 2 && item.ConnectionProfileTypeId == Entity_ConditionProfileManager.ConnectionProfileType_AdvancedStandingFor )
					//		input.IsAdvancedStandingFor.Add( item );
					//	else if ( item.ConditionSubTypeId == 2 && item.ConnectionProfileTypeId == Entity_ConditionProfileManager.ConnectionProfileType_AdvancedStandingFrom )
					//		input.AdvancedStandingFrom.Add( item );
					//	else if ( item.ConditionSubTypeId == 2 && item.ConnectionProfileTypeId == Entity_ConditionProfileManager.ConnectionProfileType_PreparationFor )
					//		input.IsPreparationFor.Add( item );
					//	else if ( item.ConditionSubTypeId == 2 && item.ConnectionProfileTypeId == Entity_ConditionProfileManager.ConnectionProfileType_PreparationFrom )
					//		input.PreparationFrom.Add( item );
					//	{
					//		//????;
					//	}


					//}
				}
				output.AdvancedStandingFrom = ServiceHelper.MapToConditionProfiles( input.AdvancedStandingFrom, searchType );
				output.IsAdvancedStandingFor = ServiceHelper.MapToConditionProfiles( input.IsAdvancedStandingFor, searchType );
				//
				output.PreparationFrom = ServiceHelper.MapToConditionProfiles( input.PreparationFrom, searchType );
				output.IsPreparationFor = ServiceHelper.MapToConditionProfiles( input.IsPreparationFor, searchType );
				//
				output.IsRequiredFor = ServiceHelper.MapToConditionProfiles( input.IsRequiredFor, searchType );
				output.IsRecommendedFor = ServiceHelper.MapToConditionProfiles( input.IsRecommendedFor, searchType );

			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( thisClassName + ".MapToAPI. Section 2, Name: {0}, Id: [1}", input.Name, input.Id ) );

			}
			//====================================
			//the following can be made a common method
			var dataMergedRequirements = new MergedConditions();
			var dataMergedRecommendations = new MergedConditions();
			var dataConnections = new ConnectionData();
			ServiceHelper.GetAllChildren( dataMergedRequirements, dataMergedRecommendations, dataConnections, input, null, null );
			//now pull out estimated durations
			if (dataMergedRequirements.TargetCredential != null && dataMergedRequirements.TargetCredential.Any() )
			{
				output.CredentialEstimatedDuration = ServiceHelper.GetAllDurations( dataMergedRequirements.CredentialsSansSelf(input.Id), "Estimated Time to Complete Required Embedded Credentials" );
			}
			if ( dataMergedRequirements.TargetAssessment != null && dataMergedRequirements.TargetAssessment.Any() )
			{
				//output.AssessmentEstimatedDuration = ServiceHelper.GetAllDurationsOLD( dataMergedRequirements.TargetAssessment, "Estimated Time to Complete Required Assessments" );
				output.AssessmentEstimatedDuration = ServiceHelper.GetAllDurations( dataMergedRequirements.TargetAssessment, "Estimated Time to Complete Required Assessments" );
			}
			if ( dataMergedRequirements.TargetLearningOpportunity != null && dataMergedRequirements.TargetLearningOpportunity.Any() )
			{
				output.LearningOpportunityEstimatedDuration = ServiceHelper.GetAllDurations( dataMergedRequirements.TargetLearningOpportunity, "Estimated Time to Complete Required Learning Opportunities" );
			}
			

			//competencies
			var dataAllCompetencies = ServiceHelper.GetAllCompetencies( dataMergedRequirements );

			var allCompetencies = dataAllCompetencies.RequiresByFramework
							.Concat( dataAllCompetencies.AssessesByFramework )
							.Concat( dataAllCompetencies.TeachesByFramework )
							.ToList();
			var allFrameWorks = new Dictionary<string, List<string>>();
			foreach ( var frameWork in allCompetencies )
			{
				if ( !string.IsNullOrWhiteSpace( frameWork.CaSSViewerUrl ) && !allFrameWorks.ContainsKey( frameWork.CaSSViewerUrl ?? "" ) )
				{
					allFrameWorks.Add( frameWork.CaSSViewerUrl, frameWork.Items.Select( m => m.TargetNode ).ToList() );
				}
			}
			var frameworkGraphs = new List<string>();
			
			foreach ( var framework in allCompetencies )
			{
				var uri = ( framework.FrameworkUri ?? "" ).Replace( "/resources/", "/graph/" );
				if ( framework.IsARegistryFrameworkUrl && framework.ExistsInRegistry )
				{

					var frameworkData = RegistryServices.GetRegistryData( "", uri );
					if ( !string.IsNullOrWhiteSpace( frameworkData ) && frameworkData.IndexOf( "<" ) != 0 ) //Avoid empty results and results like "<h2>Incomplete response received from application</h2>"
					{
						frameworkGraphs.Add( frameworkData );
					}
				}
			}

			if ( dataAllCompetencies.RequiresByFramework.Count() > 0 )
			{

			}
			if ( dataAllCompetencies.AssessesByFramework.Count() > 0 )
			{

			}
			if ( dataAllCompetencies.TeachesByFramework.Count() > 0 )
			{

			}
			//=======================================
			try
			{
				//
				if ( input.CommonCosts != null && input.CommonCosts.Any() )
				{
					output.CommonCosts = ServiceHelper.MapCostManifests( input.CommonCosts, searchType );
					output.EstimatedCost = new List<Models.Elastic.CostProfile>();
					foreach ( var item in output.CommonCosts )
					{
						output.EstimatedCost.AddRange( item.EstimatedCost );
					}
					output.CommonCosts = null;
				}

				if ( input.EstimatedCost != null && input.EstimatedCost.Any() )
				{
					if ( output.EstimatedCost == null )
						output.EstimatedCost = new List<Models.Elastic.CostProfile>();

					var estimatedCost = ServiceHelper.MapCostProfiles( input.EstimatedCost, searchType );
					if ( estimatedCost != null && estimatedCost.Any() )
						output.EstimatedCost.AddRange( estimatedCost );
				}
				//loop costs
				if ( input.Requires.SelectMany( x => x.TargetLearningOpportunity.Where( y => y.EstimatedCost.Count() + y.CommonCosts.Count() > 0 ) ).Count() > 0 )
				{
					var list = input.Requires.SelectMany( x => x.TargetLearningOpportunity ).ToList();
					foreach ( var item in list )
					{
						if ( item.CommonCosts.Any() || item.EstimatedCost.Any() )
						{
							var commonCosts = ServiceHelper.MapCostManifests( item.CommonCosts, searchType );
							output.LearningOpportunityCost = new List<Models.Elastic.CostProfile>();
							if ( commonCosts != null && commonCosts.Any() )
							{
								foreach ( var cc in commonCosts )
								{
									output.LearningOpportunityCost.AddRange( cc.EstimatedCost );
								}
							}
						}
						//
						if ( item.EstimatedCost != null && item.EstimatedCost.Any() )
						{
							if ( output.LearningOpportunityCost == null )
								output.LearningOpportunityCost = new List<Models.Elastic.CostProfile>();

							var estimatedCost = ServiceHelper.MapCostProfiles( item.EstimatedCost, searchType );
							if ( estimatedCost != null && estimatedCost.Any() )
								output.LearningOpportunityCost.AddRange( estimatedCost );
						}
					}
				}
				//asmt costs
				if ( input.Requires.SelectMany( x => x.TargetAssessment.Where( y => y.EstimatedCost.Count() + y.CommonCosts.Count() > 0 ) ).Count() > 0 )
				{
					var list = input.Requires.SelectMany( x => x.TargetAssessment ).ToList();
					foreach ( var item in list )
					{
						if ( item.CommonCosts.Any() || item.EstimatedCost.Any() )
						{
							var commonCosts = ServiceHelper.MapCostManifests( item.CommonCosts, searchType );
							output.AssessmentCost = new List<Models.Elastic.CostProfile>();
							if ( commonCosts != null && commonCosts.Any() )
							{
								foreach ( var cc in commonCosts )
								{
									output.AssessmentCost.AddRange( cc.EstimatedCost );
								}
							}
						}
						//
						if ( item.EstimatedCost.Any() || item.EstimatedCost.Any() )
						{
							if ( output.AssessmentCost == null )
								output.AssessmentCost = new List<Models.Elastic.CostProfile>();

							var estimatedCost = ServiceHelper.MapCostProfiles( item.EstimatedCost, searchType );
							if ( estimatedCost != null && estimatedCost.Any() )
								output.AssessmentCost.AddRange( estimatedCost );
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( thisClassName + ".MapToAPI. Section 3, Name: {0}, Id: [1}", input.Name, input.Id ) );

			}


			//
			if ( input.FinancialAssistance != null && input.FinancialAssistance.Any() )
			{
				output.FinancialAssistance = ServiceHelper.MapFinancialAssistanceProfiles( input.FinancialAssistance, searchType );
			}
			//
			output.CredentialId = input.CredentialId;
			output.ISICV4 = input.ISICV4;
			//InLanguage

			//
			output.Identifier = ServiceHelper.MapIdentifierValue( input.Identifier );
			//
			MapProcessProfiles( input, ref output );
			output.SameAs = ServiceHelper.MapTextValueProfileTextValue( input.SameAs );
			//
			//output.ProcessStandards = input.ProcessStandards;
			//output.ProcessStandardsDescription = input.ProcessStandardsDescription;
			output.ProcessStandards = ServiceHelper.MapPropertyLabelLink( input.ProcessStandards, "Process Standards", input.ProcessStandardsDescription );

			//
			output.Revocation = ServiceHelper.MapRevocationProfile( searchType, input.Revocation );

			//these are can be links to existing credentials, likely to be in finder
			output.LatestVersion = ServiceHelper.MapPropertyLabelLink( input.LatestVersion, "Latest Version" );
			output.NextVersion = ServiceHelper.MapPropertyLabelLink( input.NextVersion, "Next Version" );
			output.PreviousVersion = ServiceHelper.MapPropertyLabelLink( input.PreviousVersion, "Previous Version" );
			output.Supersedes = ServiceHelper.MapPropertyLabelLink( input.Supersedes, "Supersedes" );
			output.SupersededBy = ServiceHelper.MapPropertyLabelLink( input.SupersededBy, "Superseded By" );
			//
			output.TargetPathway = ServiceHelper.MapPathwayToAJAXSettings( input.TargetPathway, "Has {0} Target Pathway(s)" );

			//
			output.VersionIdentifier = ServiceHelper.MapIdentifierValue( input.VersionIdentifierList, "Version Identifier" );
			try
			{
				MapJurisdictions( input, ref output );
				//
				//QA received
				//==> need to exclude 30-published by 
				if ( input.OrganizationRole.Any() )
				{
					output.QAReceived = ServiceHelper.MapQAReceived( input.OrganizationRole, searchType );
					//old
					var renewedBy2 = ServiceHelper.MapRoleReceived( input.OrganizationRole, searchType, Entity_AgentRelationshipManager.ROLE_TYPE_RenewedBy );
					var revokedBy2 = ServiceHelper.MapRoleReceived( input.OrganizationRole, searchType, Entity_AgentRelationshipManager.ROLE_TYPE_RevokedBy );
					//new
					output.RenewedBy = ServiceHelper.MapOutlineToAJAX( renewedBy2, "Renewed by {0} Organization(s)" );
					output.RevokedBy = ServiceHelper.MapOutlineToAJAX( revokedBy2, "Revoked by {0} Organization(s)" );


				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( thisClassName + ".MapToAPI. Section 4, Name: {0}, Id: [1}", input.Name, input.Id ) );
			}
			//



			//
			return output;
		}

		private static List<WMA.ConditionProfile> AppendConditions( List<WMA.ConditionProfile> input, List<WMA.ConditionProfile> existing )
		{
			if ( input != null && input.Any() )
			{
				if ( existing == null )
					existing = new List<WMA.ConditionProfile>();
				existing.AddRange( input );
			}

			return existing;
		}

		private static void MapJurisdictions( ThisEntity input, ref ThisEntityDetail output )
		{
			if ( input.Jurisdiction != null && input.Jurisdiction.Any() )
			{
				output.Jurisdiction = ServiceHelper.MapJurisdiction( input.Jurisdiction );

			}
			//return if no assertions
			if ( input.JurisdictionAssertions == null || !input.JurisdictionAssertions.Any() )
			{
				return;
			}
			//TODO - return all in a group or individual?
			//output.JurisdictionAssertion = ServiceHelper.MapJurisdiction( input.JurisdictionAssertions, "OfferedIn" );
			//OR

			var assertedIn = input.JurisdictionAssertions.Where( s => s.AssertedInTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_AccreditedBy ).ToList();
			if ( assertedIn != null && assertedIn.Any() )
			{
				output.AccreditedIn = ServiceHelper.MapJurisdiction( assertedIn, "AccreditedIn" );
			}
			//
			assertedIn = input.JurisdictionAssertions.Where( s => s.AssertedInTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_ApprovedBy ).ToList();
			if ( assertedIn != null && assertedIn.Any() )
				output.ApprovedIn = ServiceHelper.MapJurisdiction( assertedIn, "ApprovedIn" );
			//
			assertedIn = input.JurisdictionAssertions.Where( s => s.AssertedInTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_OFFERED_BY ).ToList();
			if ( assertedIn != null && assertedIn.Any() )
				output.OfferedIn = ServiceHelper.MapJurisdiction( assertedIn, "OfferedIn" );
			//
			assertedIn = input.JurisdictionAssertions.Where( s => s.AssertedInTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_RecognizedBy ).ToList();
			if ( assertedIn != null && assertedIn.Any() )
				output.RecognizedIn = ServiceHelper.MapJurisdiction( assertedIn, "RecognizedIn" );
			//
			assertedIn = input.JurisdictionAssertions.Where( s => s.AssertedInTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_RegulatedBy ).ToList();
			if ( assertedIn != null && assertedIn.Any() )
				output.RegulatedIn = ServiceHelper.MapJurisdiction( assertedIn, "RegulatedIn" );
			//
			assertedIn = input.JurisdictionAssertions.Where( s => s.AssertedInTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_RenewedBy ).ToList();
			if ( assertedIn != null && assertedIn.Any() )
				output.RenewedIn = ServiceHelper.MapJurisdiction( assertedIn, "RenewedIn" );
			//
			assertedIn = input.JurisdictionAssertions.Where( s => s.AssertedInTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_RevokedBy ).ToList();
			if ( assertedIn != null && assertedIn.Any() )
				output.RevokedIn = ServiceHelper.MapJurisdiction( assertedIn, "RevokedIn" );
		}
		private static void MapProcessProfiles( ThisEntity input, ref ThisEntityDetail output )
		{
			if ( input.ProcessProfilesSummary != null && input.ProcessProfilesSummary.Any() )
			{
				var url = string.Format( "detail/ProcessProfile/{0}/", input.RowId.ToString() );
				output.ProcessProfiles = new List<AJAXSettings>();
				foreach ( var item in input.ProcessProfilesSummary )
				{
					var ajax = new AJAXSettings()
					{
						Label = item.Name,
						Description = "",
						Total = item.Totals,
						URL = externalFinderSiteURL + url + item.Id.ToString(),
						TestURL = ServiceHelper.finderApiSiteURL + url + item.Id.ToString(),
					};
					//not sure we need this as part of the URL
					var qd = new ProcessProfileAjax()
					{
						Id = input.RowId.ToString(),
						ProcessTypeId = item.Id,
						//EndPoint = externalFinderSiteURL + url + item.Id.ToString()
					};
					ajax.QueryData = qd;
					/*need to know
					 * endpoint: detail/processprofile
					 * id:		 input.RowId
					 * processProfileTypeId: item.Id?
					 * 
					 */
					//var filter = ( new MD.LabelLink()
					//{
					//	Label = item.Name,
					//	Count = item.Totals,
					//	URL = externalFinderSiteURL + url + item.Id.ToString()
					//} );


					output.ProcessProfiles.Add( ajax );
				}

				return;
			}

			//process profiles
			if ( input.AdministrationProcess.Any() )
			{
				output.AdministrationProcess = ServiceHelper.MapAJAXProcessProfile( "Administration Process", "", input.AdministrationProcess );
			}
			if ( input.AppealProcess.Any() )
			{
				output.AppealProcess = ServiceHelper.MapAJAXProcessProfile( "Appeal Process", "", input.AppealProcess );
			}
			if ( input.ComplaintProcess.Any() )
			{
				output.ComplaintProcess = ServiceHelper.MapAJAXProcessProfile( "Complaint Process", "", input.ComplaintProcess );
			}
			if ( input.DevelopmentProcess.Any() )
			{
				output.DevelopmentProcess = ServiceHelper.MapAJAXProcessProfile( "Development Process", "", input.DevelopmentProcess );
			}
			if ( input.MaintenanceProcess.Any() )
			{
				output.MaintenanceProcess = ServiceHelper.MapAJAXProcessProfile( "Maintenance Process", "", input.MaintenanceProcess );
			}
			if ( input.ReviewProcess.Any() )
			{
				output.ReviewProcess = ServiceHelper.MapAJAXProcessProfile( "Review Process", "", input.ReviewProcess );
			}
			if ( input.RevocationProcess.Any() )
			{
				output.RevocationProcess = ServiceHelper.MapAJAXProcessProfile( "Revocation Process", "", input.RevocationProcess );
			}
		}

		//

		//
		/*

		public static CompetencyWrapper GetAllCompetencies( WMP.ConditionProfile container )
		{
			var wrapper = new CompetencyWrapper();

			//Data by framework is reliably populated
			//.Concat( container.TargetAssessment.SelectMany( m => m.RequiresCompetenciesFrameworks ) )
			wrapper.RequiresByFramework = container.TargetCredential.SelectMany( m => m.Requires ).SelectMany( m => m.RequiresCompetenciesFrameworks )
				.Concat( container.TargetLearningOpportunity.SelectMany( m => m.RequiresCompetenciesFrameworks ) )
				.Where( m => m != null )
				.ToList();
			wrapper.AssessesByFramework = container.TargetAssessment.SelectMany( m => m.AssessesCompetenciesFrameworks ).Where( m => m != null ).ToList();
			wrapper.TeachesByFramework = container.TargetLearningOpportunity.SelectMany( m => m.TeachesCompetenciesFrameworks ).Where( m => m != null ).ToList();

			//Data by competency is not reliably populated, so, instead get it from the frameworks
			wrapper.Requires = CredentialAlignmentObjectFrameworkProfile.FlattenAlignmentObjects( wrapper.RequiresByFramework );
			wrapper.Assesses = CredentialAlignmentObjectFrameworkProfile.FlattenAlignmentObjects( wrapper.AssessesByFramework );
			wrapper.Teaches = CredentialAlignmentObjectFrameworkProfile.FlattenAlignmentObjects( wrapper.TeachesByFramework );

			return wrapper;
		}
		//
		//

		public static void GetChildLearningOpps( List<WMP.LearningOpportunityProfile> learningOpportunities, List<WMP.LearningOpportunityProfile> runningTotal )
		{
			foreach ( var lopp in learningOpportunities )
			{
				if ( runningTotal.Where( m => m.Id == lopp.Id ).Count() == 0 )
				{
					runningTotal.Add( lopp );
					GetChildLearningOpps( lopp.HasPart, runningTotal );
				}
			}
		}
		//

		public static void GetChildCredentials( List<Credential> credentials, List<Credential> runningCredTotal, List<WMP.AssessmentProfile> runningAssessmentTotal, List<WMP.LearningOpportunityProfile> runningLoppTotal )
		{
			foreach ( var cred in credentials )
			{
				if ( runningCredTotal.Where( m => m.Id == cred.Id ).Count() == 0 )
				{
					runningCredTotal.Add( cred );
					//GetChildCredentials( cred.EmbeddedCredentials, runningCredTotal, runningAssessmentTotal, runningLoppTotal );
					GetChildCredentials( cred.Requires.SelectMany( m => m.TargetCredential ).ToList(), runningCredTotal, runningAssessmentTotal, runningLoppTotal );

					foreach ( var assessment in cred.Requires.SelectMany( m => m.TargetAssessment ) )
					{
						if ( runningAssessmentTotal.Where( m => m.Id == assessment.Id ).Count() == 0 )
						{
							runningAssessmentTotal.Add( assessment );
						}
					}

					foreach ( var lopp in cred.Requires.SelectMany( m => m.TargetLearningOpportunity ) )
					{
						if ( runningLoppTotal.Where( m => m.Id == lopp.Id ).Count() == 0 )
						{
							runningLoppTotal.Add( lopp );
						}
					}
				}
			}
		}*/
	}
	
	//
	/*
	public class MergedConditions : WMP.ConditionProfile
	{
		public MergedConditions()
		{
			TopLevelCredentials = new List<Credential>();
			TopLevelAssessments = new List<WMP.AssessmentProfile>();
			TopLevelLearningOpportunities = new List<WMP.LearningOpportunityProfile>();
		}

		public List<Credential> CredentialsSansSelf( int id )
		{
			return TargetCredential.Where( m => m.Id != id ).ToList();
		}
		public List<WMP.AssessmentProfile> AssessmentsSansSelf( int id )
		{
			return TargetAssessment.Where( m => m.Id != id ).ToList();
		}
		public List<WMP.LearningOpportunityProfile> LearningOpportunitiesSansSelf( int id )
		{
			return TargetLearningOpportunity.Where( m => m.Id != id ).ToList();
		}

		public List<Credential> TopLevelCredentials { get; set; }
		public List<WMP.AssessmentProfile> TopLevelAssessments { get; set; }
		public List<WMP.LearningOpportunityProfile> TopLevelLearningOpportunities { get; set; }
	}
	//
	public class CompetencyWrapper
	{
		public CompetencyWrapper()
		{
			Requires = new List<CredentialAlignmentObjectProfile>();
			Teaches = new List<CredentialAlignmentObjectProfile>();
			Assesses = new List<CredentialAlignmentObjectProfile>();
			RequiresByFramework = new List<CredentialAlignmentObjectFrameworkProfile>();
			AssessesByFramework = new List<CredentialAlignmentObjectFrameworkProfile>();
			TeachesByFramework = new List<CredentialAlignmentObjectFrameworkProfile>();
		}
		public List<CredentialAlignmentObjectProfile> Requires { get; set; }
		public List<CredentialAlignmentObjectProfile> Teaches { get; set; }
		public List<CredentialAlignmentObjectProfile> Assesses { get; set; }
		public List<CredentialAlignmentObjectProfile> Concatenated { get { return Requires.Concat( Teaches ).Concat( Assesses ).ToList(); } }
		public int Total { get { return Concatenated.Count(); } }

		public List<CredentialAlignmentObjectFrameworkProfile> RequiresByFramework { get; set; }
		public List<CredentialAlignmentObjectFrameworkProfile> AssessesByFramework { get; set; }
		public List<CredentialAlignmentObjectFrameworkProfile> TeachesByFramework { get; set; }
		public List<CredentialAlignmentObjectFrameworkProfile> ConcatenatedFrameworks { get { return RequiresByFramework.Concat( TeachesByFramework ).Concat( AssessesByFramework ).ToList(); } }
		// Will be checked later
		public List<CredentialAlignmentObjectItem> ConcatenatedCompetenciesFromFrameworks { get { return ConcatenatedFrameworks.SelectMany( m => m.Items ).ToList(); } }
		public int TotalFrameworks { get { return ConcatenatedFrameworks.Count(); } }
		public int TotalCompetenciesWithinFrameworks { get { return ConcatenatedCompetenciesFromFrameworks.Count(); } }
	}
	//

	public class ConnectionData
	{
		public ConnectionData()
		{
			foreach ( var item in this.GetType().GetProperties().Where( m => m.PropertyType == typeof( List<WMP.ConditionProfile> ) ) )
			{
				item.SetValue( this, new List<WMP.ConditionProfile>() );
			}
		}
		public static ConnectionData Process( List<WMP.ConditionProfile> connections, ConnectionData existing, List<ConditionManifest> commonConditions )
		{
			var result = new ConnectionData();
			connections = connections ?? new List<WMP.ConditionProfile>();
			existing = existing ?? new ConnectionData();
			//Handle common conditions
			var manifests = ConditionManifestExpanded.ExpandConditionManifestList( commonConditions ?? new List<ConditionManifest>() );
			//Handle condition profiles
			var conditions = ConditionManifestExpanded.DisambiguateConditionProfiles( connections );
			result.Requires = existing.Requires
				.Concat( conditions.Requires )
				.Concat( manifests.SelectMany( m => m.Requires ) )
				.ToList();
			result.Recommends = existing.Recommends
				.Concat( conditions.Recommends )
				.Concat( manifests.SelectMany( m => m.Recommends ) )
				.ToList();
			result.PreparationFrom = existing.PreparationFrom
				.Concat( conditions.PreparationFrom )
				.Concat( manifests.SelectMany( m => m.PreparationFrom ) )
				.ToList();
			result.AdvancedStandingFrom = existing.AdvancedStandingFrom
				.Concat( conditions.AdvancedStandingFrom )
				.Concat( manifests.SelectMany( m => m.AdvancedStandingFrom ) )
				.ToList();
			result.IsRequiredFor = existing.IsRequiredFor
				.Concat( conditions.IsRequiredFor )
				.Concat( manifests.SelectMany( m => m.IsRequiredFor ) )
				.ToList();
			result.IsRecommendedFor = existing.IsRecommendedFor
				.Concat( conditions.IsRecommendedFor )
				.Concat( manifests.SelectMany( m => m.IsRecommendedFor ) )
				.ToList();
			result.IsAdvancedStandingFor = existing.IsAdvancedStandingFor
				.Concat( conditions.IsAdvancedStandingFor )
				.Concat( manifests.SelectMany( m => m.IsAdvancedStandingFor ) )
				.ToList();
			result.IsPreparationFor = existing.IsPreparationFor
				.Concat( conditions.IsPreparationFor )
				.Concat( manifests.SelectMany( m => m.IsPreparationFor ) )
				.ToList();
			result.Corequisite = existing.Corequisite
				.Concat( conditions.Corequisite )
				.Concat( manifests.SelectMany( m => m.Corequisite ) )
				.ToList();
			result.EntryCondition = existing.EntryCondition
				.Concat( conditions.EntryCondition )
				.Concat( manifests.SelectMany( m => m.EntryCondition ) )
				.ToList();
			result.Renewal = existing.Renewal
				.Concat( conditions.Renewal )
				.Concat( manifests.SelectMany( m => m.Renewal ) )
				.ToList();

			return result;
		}
		public List<WMP.ConditionProfile> Requires { get; set; }
		public List<WMP.ConditionProfile> Recommends { get; set; }
		public List<WMP.ConditionProfile> PreparationFrom { get; set; }
		public List<WMP.ConditionProfile> AdvancedStandingFrom { get; set; }
		public List<WMP.ConditionProfile> IsRequiredFor { get; set; }
		public List<WMP.ConditionProfile> IsRecommendedFor { get; set; }
		public List<WMP.ConditionProfile> IsAdvancedStandingFor { get; set; }
		public List<WMP.ConditionProfile> IsPreparationFor { get; set; }
		public List<WMP.ConditionProfile> Corequisite { get; set; }
		public List<WMP.ConditionProfile> EntryCondition { get; set; }
		public List<WMP.ConditionProfile> Renewal { get; set; }
	}
	*/
	//
}

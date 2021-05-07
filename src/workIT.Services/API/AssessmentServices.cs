using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Web;

using workIT.Models;
using workIT.Models.Common;
using MCD = workIT.Models.API;
using workIT.Models.Search;
using workIT.Factories;

using workIT.Utilities;
using ElasticHelper = workIT.Services.ElasticServices;
using EntityHelper = workIT.Services.AssessmentServices;

using ThisEntity = workIT.Models.ProfileModels.AssessmentProfile;
using ThisEntityDetail = workIT.Models.API.AssessmentDetail;


namespace workIT.Services.API
{
	public class AssessmentServices
	{
		public static string externalFinderSiteURL = UtilityManager.GetAppKeyValue( "externalFinderSiteURL" );

	
		public static ThisEntityDetail GetDetailForAPI( int id, bool skippingCache = false )
		{
			var output = EntityHelper.GetDetail( id, skippingCache );
			return MapToAPI( output );

		}
		public static ThisEntityDetail GetDetailByCtidForAPI( string ctid, bool skippingCache = false )
		{
			var output = EntityHelper.GetDetailByCtid( ctid, skippingCache );
			return MapToAPI( output );
		}
		private static ThisEntityDetail MapToAPI( ThisEntity input )
		{
			var searchType = "assessment";

			var output = new MCD.AssessmentDetail()
			{
				Meta_Id = input.Id,
				CTID = input.CTID,
				Name = input.Name,
				FriendlyName = HttpUtility.UrlPathEncode ( input.Name ),
				Description = input.Description,
				SubjectWebpage = input.SubjectWebpage,
				EntityTypeId = 3,
				CredentialRegistryURL = RegistryServices.GetResourceUrl( input.CTID ),
				RegistryData = ServiceHelper.FillRegistryData( input.CTID )

			};
			//
			//output.CTDLType = input.assessmentType; ;
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
			//output.OwnedBy2 = ServiceHelper.MapRoleReceived( input.OrganizationRole, searchType, Entity_AgentRelationshipManager.ROLE_TYPE_OWNER );
			//output.OwnedBy = ServiceHelper.MapOutlineToAJAX( output.OwnedBy2, "" );
			//output.OfferedBy2 = ServiceHelper.MapRoleReceived( input.OrganizationRole, searchType, Entity_AgentRelationshipManager.ROLE_TYPE_OFFERED_BY );
			//output.OfferedBy = ServiceHelper.MapOutlineToAJAX( output.OfferedBy2, "Offered by {0} Organization(s)" );
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
			if ( input.InLanguageCodeList != null && input.InLanguageCodeList.Any() )
			{
				//output.Meta_Language = input.InLanguageCodeList[ 0 ].TextTitle;
				output.InLanguage = new List<string>();
				foreach ( var item in input.InLanguageCodeList )
				{
					output.InLanguage.Add( item.TextTitle );
				}
			}
			//
			//output.AssessmentExample = input.AssessmentExample;
			//output.AssessmentExampleDescription = input.AssessmentExampleDescription;
			output.AssessmentExample = ServiceHelper.MapPropertyLabelLink( input.AssessmentExample, "Assessment Example", input.AssessmentExampleDescription );

			//
			output.AssessmentMethodDescription = input.AssessmentMethodDescription;
			output.AssessmentMethodType = ServiceHelper.MapPropertyLabelLinks( input.AssessmentMethodType, searchType );
			output.AssessmentOutput = input.AssessmentOutput;
			output.AssessmentUseType = ServiceHelper.MapPropertyLabelLinks( input.AssessmentUseType, searchType );

			if ( !string.IsNullOrWhiteSpace( input.AvailabilityListing ) )
				output.AvailabilityListing = new List<string>() { input.AvailabilityListing };
			if ( !string.IsNullOrWhiteSpace( input.AvailableOnlineAt ) )
				output.AvailableOnlineAt = new List<string>() { input.AvailableOnlineAt };
			//addresses
			//
			//MapAddress( input, ref output );
			output.AvailableAt = ServiceHelper.MapAddress( input.Addresses );

			//
			output.AudienceLevelType = ServiceHelper.MapPropertyLabelLinks( input.AudienceLevelType, searchType );
			output.AudienceType = ServiceHelper.MapPropertyLabelLinks( input.AudienceType, searchType );
			output.CreditUnitTypeDescription = input.CreditUnitTypeDescription;
			output.CreditValue = ServiceHelper.MapValueProfile( input.CreditValue, searchType );
			//
			output.DateEffective = input.DateEffective;
			output.ExpirationDate = input.ExpirationDate;
			output.ExternalResearch = input.ExternalResearch;
			output.DeliveryType = ServiceHelper.MapPropertyLabelLinks( input.DeliveryType, searchType );
			output.DeliveryTypeDescription = input.DeliveryTypeDescription;
			output.EstimatedDuration = ServiceHelper.MapDurationProfiles( input.EstimatedDuration );
			//
			output.AdvancedStandingFrom = ServiceHelper.MapToConditionProfiles( input.AdvancedStandingFrom, searchType );
			output.IsAdvancedStandingFor = ServiceHelper.MapToConditionProfiles( input.IsAdvancedStandingFor, searchType );
			//
			output.PreparationFrom = ServiceHelper.MapToConditionProfiles( input.PreparationFrom, searchType );
			output.IsPreparationFor = ServiceHelper.MapToConditionProfiles( input.IsPreparationFor, searchType );
			//
			output.IsRequiredFor = ServiceHelper.MapToConditionProfiles( input.IsRequiredFor, searchType );
			output.IsRecommendedFor = ServiceHelper.MapToConditionProfiles( input.IsRecommendedFor, searchType );
			//
			//CostProfiles
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
			//
			if ( input.FinancialAssistance != null && input.FinancialAssistance.Any() )
			{
				output.FinancialAssistance = ServiceHelper.MapFinancialAssistanceProfiles( input.FinancialAssistance, searchType );
			}
			
			//condition profiles
			output.Corequisite = ServiceHelper.MapToConditionProfiles( input.Corequisite, searchType );
			output.EntryCondition = ServiceHelper.MapToConditionProfiles( input.EntryCondition, searchType );
			output.Recommends = ServiceHelper.MapToConditionProfiles( input.Recommends, searchType );
			output.EntryCondition = ServiceHelper.MapToConditionProfiles( input.EntryCondition, searchType );
			output.Requires = ServiceHelper.MapToConditionProfiles( input.Requires, searchType );
			//
			if ( input.CommonConditions != null && input.CommonConditions.Any() )
			{
				output.CommonConditions = ServiceHelper.MapConditionManifests( input.CommonConditions, searchType );
				if ( output.CommonConditions != null && output.CommonConditions.Any() )
				{
					foreach ( var item in output.CommonConditions )
					{
						if ( item.Requires != null && item.Requires.Any() )
						{
							output.Requires = ServiceHelper.AppendConditions( item.Requires, output.Requires );
						}
						if ( item.Recommends != null && item.Recommends.Any() )
						{
							output.Recommends = ServiceHelper.AppendConditions( item.Recommends, output.Recommends );
						}
						if ( item.Corequisite != null && item.Corequisite.Any() )
						{
							output.Corequisite = ServiceHelper.AppendConditions( item.Corequisite, output.Corequisite );
						}
						if ( item.EntryCondition != null && item.EntryCondition.Any() )
						{
							output.EntryCondition = ServiceHelper.AppendConditions( item.EntryCondition, output.EntryCondition );
						}
					}
				}
			}
			//
			output.IndustryType = ServiceHelper.MapReferenceFrameworkLabelLink( input.IndustryType, searchType, CodesManager.PROPERTY_CATEGORY_NAICS );
			output.OccupationType = ServiceHelper.MapReferenceFrameworkLabelLink( input.OccupationType, searchType, CodesManager.PROPERTY_CATEGORY_SOC );
			output.InstructionalProgramType = ServiceHelper.MapReferenceFrameworkLabelLink( input.InstructionalProgramType, searchType, CodesManager.PROPERTY_CATEGORY_CIP );
			output.IsReferenceVersion = input.IsReferenceVersion;
			//
			if ( input.Keyword != null && input.Keyword.Any() )
				output.Keyword = ServiceHelper.MapPropertyLabelLinks( input.Keyword, searchType );
			if ( input.Subject != null && input.Subject.Any() )
				output.Subject = ServiceHelper.MapPropertyLabelLinks( input.Subject, searchType );
			//

			//
			output.HasGroupEvaluation = input.HasGroupEvaluation;
			output.HasGroupParticipation = input.HasGroupParticipation;
			output.IsProctored = input.IsProctored;
			//InLanguage

			//
			output.Identifier = ServiceHelper.MapIdentifierValue( input.Identifier );
			output.LearningMethodDescription = input.LearningMethodDescription;
			//
			MapProcessProfiles( input, ref output );
			//
			//output.ProcessStandards = input.ProcessStandards;
			//output.ProcessStandardsDescription = input.ProcessStandardsDescription;
			output.ProcessStandards = ServiceHelper.MapPropertyLabelLink( input.ProcessStandards, "Process Standards", input.ProcessStandardsDescription );


			output.ScoringMethodDescription = input.ScoringMethodDescription;
			//output.ScoringMethodExample = input.ScoringMethodExample;
			//output.ScoringMethodExampleDescription = input.ScoringMethodExampleDescription;
			output.ScoringMethodExample = ServiceHelper.MapPropertyLabelLink( input.ScoringMethodExample, "Scoring Method Example", input.ScoringMethodExampleDescription );

			output.ScoringMethodType = ServiceHelper.MapPropertyLabelLinks( input.ScoringMethodType, searchType );

			output.SameAs = ServiceHelper.MapTextValueProfileTextValue( input.SameAs );

			output.VersionIdentifier = ServiceHelper.MapIdentifierValue( input.VersionIdentifierList, "Version Identifier" );
			//
			MapJurisdictions( input, ref output );
			//
			//QA received
			//==> need to exclude 30-published by 
			if ( input.OrganizationRole.Any() )
			{
				output.QAReceived = ServiceHelper.MapQAReceived( input.OrganizationRole, searchType );
				
			}

			//
			return output;
		}
		private static void MapAddress( ThisEntity input, ref MCD.AssessmentDetail output )
		{
			//addresses
			//if ( input.Addresses.Any() )
			//{
			//	foreach ( var item in input.Addresses )
			//	{
			//		var address = new MCD.Address()
			//		{
			//			StreetAddress = item.Address1,
			//			PostOfficeBoxNumber = item.PostOfficeBoxNumber,
			//			AddressLocality = item.AddressLocality,
			//			SubRegion = item.SubRegion ?? "",
			//			AddressRegion = item.AddressRegion,
			//			PostalCode = item.PostalCode,
			//			AddressCountry = item.AddressCountry,
			//			Latitude = item.Latitude,
			//			Longitude = item.Longitude
			//		};
			//		if ( item.HasContactPoints() )
			//		{
			//			//???
			//			//output.ContactPoint = new List<MCD.ContactPoint>();
			//			address.TargetContactPoint = new List<MCD.ContactPoint>();
			//			foreach ( var cp in item.ContactPoint )
			//			{
			//				var cpOutput = new MCD.ContactPoint()
			//				{
			//					ContactType = cp.ContactType,
			//					Email = cp.Emails,
			//					Telephone = cp.PhoneNumbers,
			//					SocialMedia = cp.SocialMediaPages
			//				};
			//				address.TargetContactPoint.Add( cpOutput );
			//			}
			//		}
			//		output.AvailableAt.Add( address );
			//	}

			//}

		}

		private static void MapJurisdictions( ThisEntity org, ref MCD.AssessmentDetail output )
		{
			if ( org.Jurisdiction != null && org.Jurisdiction.Any() )
			{
				output.Jurisdiction = ServiceHelper.MapJurisdiction( org.Jurisdiction );

			}
			//return if no assertions
			if ( org.JurisdictionAssertions == null || !org.JurisdictionAssertions.Any() )
			{
				return;
			}
			////TODO - return all in a group or individual?
			//output.JurisdictionAssertion = ServiceHelper.MapJurisdiction( org.JurisdictionAssertions, "OfferedIn" );
			////OR

			var assertedIn = org.JurisdictionAssertions.Where( s => s.AssertedInTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_AccreditedBy ).ToList();
			if ( assertedIn != null && assertedIn.Any() )
			{
				output.AccreditedIn = ServiceHelper.MapJurisdiction( assertedIn, "AccreditedIn" );
			}
			//
			assertedIn = org.JurisdictionAssertions.Where( s => s.AssertedInTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_ApprovedBy ).ToList();
			if ( assertedIn != null && assertedIn.Any() )
				output.ApprovedIn = ServiceHelper.MapJurisdiction( assertedIn, "ApprovedIn" );
			//
			assertedIn = org.JurisdictionAssertions.Where( s => s.AssertedInTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_OFFERED_BY ).ToList();
			if ( assertedIn != null && assertedIn.Any() )
				output.OfferedIn = ServiceHelper.MapJurisdiction( assertedIn, "OfferedIn" );
			//
			assertedIn = org.JurisdictionAssertions.Where( s => s.AssertedInTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_RecognizedBy ).ToList();
			if ( assertedIn != null && assertedIn.Any() )
				output.RecognizedIn = ServiceHelper.MapJurisdiction( assertedIn, "RecognizedIn" );
			//
			assertedIn = org.JurisdictionAssertions.Where( s => s.AssertedInTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_RegulatedBy ).ToList();
			if ( assertedIn != null && assertedIn.Any() )
				output.RegulatedIn = ServiceHelper.MapJurisdiction( assertedIn, "RegulatedIn" );
		}
		private static void MapProcessProfiles( ThisEntity input, ref MCD.AssessmentDetail output )
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
					output.ProcessProfiles.Add( ajax );
				}

				return;
			}

			//process profiles
			if ( input.AdministrationProcess.Any() )
			{
				output.AdministrationProcess = ServiceHelper.MapAJAXProcessProfile( "Administration Process", "", input.AdministrationProcess );
			}
			if ( input.DevelopmentProcess.Any() )
			{
				output.DevelopmentProcess = ServiceHelper.MapAJAXProcessProfile( "Development Process", "", input.DevelopmentProcess );
			}
			if ( input.MaintenanceProcess.Any() )
			{
				output.MaintenanceProcess = ServiceHelper.MapAJAXProcessProfile( "Maintenance Process", "", input.MaintenanceProcess );
			}

		}
		
	}
}

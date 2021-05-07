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
using EntityHelper = workIT.Services.LearningOpportunityServices;
using ThisEntity = workIT.Models.ProfileModels.LearningOpportunityProfile;
using ThisEntityDetail = workIT.Models.API.LearningOpportunityDetail;

namespace workIT.Services.API
{
	public class LearningOpportunityServices
	{
		public static string externalFinderSiteURL = UtilityManager.GetAppKeyValue( "externalFinderSiteURL" );


		public static MCD.LearningOpportunityDetail GetDetailForAPI( int id, bool skippingCache = false )
		{
			var record = EntityHelper.GetDetail( id, skippingCache );
			return MapToAPI( record );

		}
		public static MCD.LearningOpportunityDetail GetDetailByCtidForAPI( string ctid, bool skippingCache = false )
		{
			var record = EntityHelper.GetDetailByCtid( ctid, skippingCache );
			return MapToAPI( record );
		}
		private static MCD.LearningOpportunityDetail MapToAPI( ThisEntity input )
		{
			var searchType = "learningopportunity";
			var output = new MCD.LearningOpportunityDetail()
			{
				Meta_Id = input.Id,
				CTID=input.CTID,
				Name = input.Name,
				FriendlyName = HttpUtility.UrlPathEncode ( input.Name ),
				Description = input.Description,
				SubjectWebpage = input.SubjectWebpage,
				EntityTypeId = 7,
				CredentialRegistryURL = RegistryServices.GetResourceUrl( input.CTID ),
				RegistryData = ServiceHelper.FillRegistryData( input.CTID )
			};
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
			//
			//QA for owner,not offerer
			if ( input.OwningOrganization != null && input.OwningOrganization.Id > 0 )
			{
				if ( input.OwningOrganization.OrganizationRole_Recipient != null && input.OwningOrganization.OrganizationRole_Recipient.Any() )
				{
					output.OwnerQAReceived = ServiceHelper.MapQAReceived( input.OwningOrganization.OrganizationRole_Recipient, searchType );
				}
			}
			//
			output.AssessmentMethodType = ServiceHelper.MapPropertyLabelLinks( input.AssessmentMethodType, searchType );
			output.AssessmentMethodDescription = input.AssessmentMethodDescription;

			if ( !string.IsNullOrWhiteSpace( input.AvailabilityListing ) )
				output.AvailabilityListing = new List<string>() { input.AvailabilityListing };
			if ( !string.IsNullOrWhiteSpace( input.AvailableOnlineAt ) )
				output.AvailableOnlineAt = new List<string>() { input.AvailableOnlineAt };
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
			output.DeliveryType = ServiceHelper.MapPropertyLabelLinks( input.DeliveryType, searchType );
			output.DeliveryTypeDescription = input.DeliveryTypeDescription;
			output.EstimatedDuration = ServiceHelper.MapDurationProfiles( input.EstimatedDuration );
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
							output.Corequisite = ServiceHelper.AppendConditions( item.Requires, output.Corequisite );
						}
						if ( item.EntryCondition != null && item.EntryCondition.Any() )
						{
							output.EntryCondition = ServiceHelper.AppendConditions( item.EntryCondition, output.EntryCondition );
						}
					}
				}
			}
			//
			//connection profiles
			output.AdvancedStandingFrom = ServiceHelper.MapToConditionProfiles( input.AdvancedStandingFrom, searchType );
			output.IsAdvancedStandingFor = ServiceHelper.MapToConditionProfiles( input.IsAdvancedStandingFor, searchType );
			//
			output.PreparationFrom = ServiceHelper.MapToConditionProfiles( input.PreparationFrom, searchType );
			output.IsPreparationFor = ServiceHelper.MapToConditionProfiles( input.IsPreparationFor, searchType );
			//
			output.IsRequiredFor = ServiceHelper.MapToConditionProfiles( input.IsRequiredFor, searchType );
			output.IsRecommendedFor = ServiceHelper.MapToConditionProfiles( input.IsRecommendedFor, searchType );
			//
			//
			if ( input.HasPart != null && input.HasPart.Any() )
			{
				output.HasPart = new List<MCD.Outline>();
				foreach ( var target in input.HasPart )
				{
					if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
						output.HasPart.Add( ServiceHelper.MapToOutline( target, searchType ) );
				}
			}
			//
			if ( input.IsPartOf != null && input.IsPartOf.Any() )
			{
				output.IsPartOf = new List<MCD.Outline>();
				foreach ( var target in input.IsPartOf )
				{
					if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
						output.IsPartOf.Add( ServiceHelper.MapToOutline( target, searchType ) );
				}
			}
			//
			output.Identifier = ServiceHelper.MapIdentifierValue( input.Identifier );
			//
			output.IndustryType = ServiceHelper.MapReferenceFrameworkLabelLink( input.IndustryType, searchType, CodesManager.PROPERTY_CATEGORY_NAICS );
			output.OccupationType = ServiceHelper.MapReferenceFrameworkLabelLink( input.OccupationType, searchType, CodesManager.PROPERTY_CATEGORY_SOC );
			output.InstructionalProgramType = ServiceHelper.MapReferenceFrameworkLabelLink( input.InstructionalProgramType, searchType, CodesManager.PROPERTY_CATEGORY_CIP );
			output.IsReferenceVersion = input.IsReferenceVersion;
			//
			MapJurisdictions( input, ref output );

			//
			if ( input.Keyword != null && input.Keyword.Any() )
				output.Keyword = ServiceHelper.MapPropertyLabelLinks( input.Keyword, searchType );
			if ( input.Subject != null && input.Subject.Any() )
				output.Subject = ServiceHelper.MapPropertyLabelLinks( input.Subject, searchType );
			//
			output.LearningMethodType = ServiceHelper.MapPropertyLabelLinks( input.LearningMethodType, searchType );
			output.LearningMethodDescription = input.LearningMethodDescription;
			//
			//none yet, leave here for likely additions
			//MapProcessProfiles( input, ref output );
			//
			output.SameAs = ServiceHelper.MapTextValueProfileTextValue( input.SameAs );
			//
			output.VersionIdentifier = ServiceHelper.MapIdentifierValue( input.VersionIdentifierList, "Version Identifier" );
			//QA received
			//==> need to exclude 30-published by 
			if ( input.OrganizationRole.Any() )
			{
				output.QAReceived = ServiceHelper.MapQAReceived( input.OrganizationRole, searchType );
				
			}

			return output;
		}
		//
		

		

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
			assertedIn = input.JurisdictionAssertions.Where( s => s.AssertedInTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_RecognizedBy ).ToList();
			if ( assertedIn != null && assertedIn.Any() )
				output.RecognizedIn = ServiceHelper.MapJurisdiction( assertedIn, "RecognizedIn" );
			//
			assertedIn = input.JurisdictionAssertions.Where( s => s.AssertedInTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_RegulatedBy ).ToList();
			if ( assertedIn != null && assertedIn.Any() )
				output.RegulatedIn = ServiceHelper.MapJurisdiction( assertedIn, "RegulatedIn" );
		}

		private static void MapProcessProfiles( ThisEntity input, ref MCD.LearningOpportunityDetail output )
		{
			//process profiles
			//if ( input.AdministrationProcess.Any() )
			//{
			//	output.AdministrationProcess = ServiceHelper.MapProcessProfile( "assessment", input.AdministrationProcess );
			//}

			//if ( input.DevelopmentProcess.Any() )
			//{
			//	output.DevelopmentProcess = ServiceHelper.MapProcessProfile( "assessment", input.DevelopmentProcess );
			//}
			//if ( input.MaintenanceProcess.Any() )
			//{
			//	output.MaintenanceProcess = ServiceHelper.MapProcessProfile( "assessment", input.MaintenanceProcess );
			//}

		}
	}
}

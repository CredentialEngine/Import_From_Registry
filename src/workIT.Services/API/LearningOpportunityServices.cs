using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using workIT.Models;
using workIT.Models.Common;
using WMA = workIT.Models.API;
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
		public static WMA.LearningOpportunityDetail GetDetailForAPI( int id, bool skippingCache = false )
		{
			var record = EntityHelper.GetDetail( id, skippingCache, true );
			return MapToAPI( record );
		}
		public static WMA.LearningOpportunityDetail GetDetailForAPI( WMA.DetailRequest request )
		{
			var record = EntityHelper.GetDetail( request.Id, request.SkippingCache, request.IsAPIRequest );
			return MapToAPI( record, request.WidgetId );
		}
		public static WMA.LearningOpportunityDetail GetDetailByCtidForAPI( string ctid, bool skippingCache = false )
		{
			//get minimum
			var learningOpportunity = LearningOpportunityManager.GetByCtid( ctid );
			return GetDetailForAPI( learningOpportunity.Id, skippingCache );
		}
		private static WMA.LearningOpportunityDetail MapToAPI( ThisEntity input, int widgetId = 0 )
		{
			var searchType = "learningopportunity";
			var output = new WMA.LearningOpportunityDetail()
			{
				Meta_Id = input.Id,
				CTID = input.CTID,
				Name = input.Name,
				Meta_FriendlyName = HttpUtility.UrlPathEncode( input.Name ),
				Description = input.Description,
				SubjectWebpage = input.SubjectWebpage,
				EntityTypeId = input.LearningEntityTypeId,
				//TBD
				LearningEntityTypeId = input.LearningEntityTypeId,
				//BroadType = (input.LearningEntityType??"").Replace(" ",""),	//always LearningOpportunity
				CTDLTypeLabel = input.LearningEntityTypeLabel,
				CTDLType = input.LearningTypeSchema,
				CredentialRegistryURL = RegistryServices.GetResourceUrl( input.CTID ),
				RegistryData = ServiceHelper.FillRegistryData( input.CTID, "Learning Opportunity" )
			};
			output.EntityLastUpdated = input.EntityLastUpdated;
			if ( input.EntityStateId == 0 )
				return output;
			//experimental - not used in UI yet
			output.RegistryDataList.Add( output.RegistryData );

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
				output.OwnedByLabel = ServiceHelper.MapDetailLink( "Organization", input.OrganizationName, input.OwningOrganizationId, input.OwningOrganization.FriendlyName );
			}
			//
			var ownedBy = ServiceHelper.MapOrganizationRoleProfileToOutline( input.OrganizationRole, Entity_AgentRelationshipManager.ROLE_TYPE_OWNER );
			var offeredBy = ServiceHelper.MapOrganizationRoleProfileToOutline( input.OrganizationRole, Entity_AgentRelationshipManager.ROLE_TYPE_OFFERED_BY );
			//if there is just one org, and has both owned and offered by
			if ( ownedBy != null && offeredBy != null && ownedBy.Count == 1 && offeredBy.Count == 1
				&& ownedBy[0].Meta_Id == offeredBy[0].Meta_Id )
			{
				output.OwnedOfferedBy = ServiceHelper.MapOutlineToAJAX( ownedBy, "" );
			}
			else
			{
				output.OwnedBy = ServiceHelper.MapOutlineToAJAX( ownedBy, "" );
				//
				output.OfferedBy = ServiceHelper.MapOutlineToAJAX( offeredBy, "Offered by {0} Organization(s)" );
			}

			//
			//output.Publisher = ServiceHelper.MapOutlineToAJAX( ServiceHelper.MapOrganizationRoleProfileToOutline( input.OrganizationRole, Entity_AgentRelationshipManager.ROLE_TYPE_PUBLISHEDBY ), "Published By" );
			//
			//QA for owner,not offerer
			if ( input.OwningOrganizationQAReceived != null && input.OwningOrganizationQAReceived.Any() )
			{
				output.OwnerQAReceived = ServiceHelper.MapQAReceived( input.OwningOrganizationQAReceived, searchType );
			}
			//
			//output.AlternateName = input.AlternateName;

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
			output.CodedNotation = input.CodedNotation;
			output.SCED = input.SCED;
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
				//foreach ( var item in output.CommonCosts )
				//{
				//	output.EstimatedCost.AddRange( item.EstimatedCost );
				//}
				//output.CommonCosts = null;
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
			if ( input.IsNonCredit != null && input.IsNonCredit == true )
				output.IsNonCredit = input.IsNonCredit;
			//
			if ( input.FinancialAssistance != null && input.FinancialAssistance.Any() )
			{
				output.FinancialAssistance = ServiceHelper.MapFinancialAssistanceProfiles( input.FinancialAssistance, searchType );
			}
			//
			output.AggregateData = ServiceHelper.MapToAggregateDataProfile( input.AggregateData, searchType );
			if ( output.AggregateData != null )
			{
				//hmm check for dataSetProfile to add to RegistryDataList.
				//Might be better to do this in the managers
			}
			//
			output.ExternalDataSetProfiles = ServiceHelper.MapToDatasetProfile( input.ExternalDataSetProfiles, searchType );
			//could add these to RegistryDataList??
			if ( output.ExternalDataSetProfiles != null && output.ExternalDataSetProfiles.Any() )
			{
				foreach ( var item in output.ExternalDataSetProfiles )
				{
					var regData = ServiceHelper.FillRegistryData( item.CTID, searchType );
					output.RegistryDataList.Add( regData );
				}
			}
			//condition profiles
			output.Corequisite = ServiceHelper.MapToConditionProfiles( input.Corequisite, searchType );
			output.EntryCondition = ServiceHelper.MapToConditionProfiles( input.EntryCondition, searchType );
			output.Recommends = ServiceHelper.MapToConditionProfiles( input.Recommends, searchType );
			output.Requires = ServiceHelper.MapToConditionProfiles( input.Requires, searchType );
			//
			if ( input.CommonConditions != null && input.CommonConditions.Any() )
			{
				output.CommonConditions = ServiceHelper.MapConditionManifests( input.CommonConditions, searchType );
				if ( output.CommonConditions != null && output.CommonConditions.Any() )
				{
					//foreach ( var item in output.CommonConditions )
					//{
					//	if ( item.Requires != null && item.Requires.Any() )
					//	{
					//		output.Requires = ServiceHelper.AppendConditions( item.Requires, output.Requires );
					//	}
					//	if ( item.Recommends != null && item.Recommends.Any() )
					//	{
					//		output.Recommends = ServiceHelper.AppendConditions( item.Recommends, output.Recommends );
					//	}
					//	if ( item.Corequisite != null && item.Corequisite.Any() )
					//	{
					//		output.Corequisite = ServiceHelper.AppendConditions( item.Requires, output.Corequisite );
					//	}
					//	if ( item.EntryCondition != null && item.EntryCondition.Any() )
					//	{
					//		output.EntryCondition = ServiceHelper.AppendConditions( item.EntryCondition, output.EntryCondition );
					//	}
					//}
				}
				//output.CommonConditions = null;
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
			//if ( input.Prerequisite != null && input.Prerequisite.Any() )
			//{
			//	var prerequisite = new List<WMA.Outline>();
			//	foreach ( var target in input.Prerequisite )
			//	{
			//		if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
			//			prerequisite.Add( ServiceHelper.MapToOutline( target, searchType ) );
			//	}
			//	output.Prerequisite = ServiceHelper.MapOutlineToAJAX( prerequisite, prerequisite.Count > 1 ? "Includes {0} Prerequisites" : "Includes {0} Prerequisite" );
			//}
			//
			if ( input.HasPart != null && input.HasPart.Any() )
			{
				var hasPart = new List<WMA.Outline>();
				foreach ( var target in input.HasPart )
				{
					if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
						hasPart.Add( ServiceHelper.MapToOutline( target, searchType ) );
				}
				output.HasPart = ServiceHelper.MapOutlineToAJAX( hasPart, hasPart.Count > 1 ? "Includes {0} Learning Opportunities" : "Includes {0} Learning Opportunity" );

			}
			//
			if ( input.IsPartOf != null && input.IsPartOf.Any() )
			{
				var isPartOf = new List<WMA.Outline>();
				foreach ( var target in input.IsPartOf )
				{
					if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
						isPartOf.Add( ServiceHelper.MapToOutline( target, searchType ) );
				}
				output.IsPartOf = ServiceHelper.MapOutlineToAJAX( isPartOf, isPartOf.Count > 1 ? "Is Part Of {0} Learning Opportunities" : "Is Part Of {0} Learning Opportunity" );
			}
			//
			//
			output.Identifier = ServiceHelper.MapIdentifierValue( input.Identifier );
			//new 
			output.OccupationType = ServiceHelper.MapReferenceFramework( input.OccupationTypes, searchType, CodesManager.PROPERTY_CATEGORY_SOC, widgetId );
			output.IndustryType = ServiceHelper.MapReferenceFramework( input.IndustryTypes, searchType, CodesManager.PROPERTY_CATEGORY_NAICS );
			output.InstructionalProgramType = ServiceHelper.MapReferenceFramework( input.InstructionalProgramTypes, searchType, CodesManager.PROPERTY_CATEGORY_CIP );

			//old
			//output.OccupationTypeOld = ServiceHelper.MapReferenceFrameworkLabelLink( input.OccupationType, searchType, CodesManager.PROPERTY_CATEGORY_SOC );
			//output.IndustryType = ServiceHelper.MapReferenceFrameworkLabelLink( input.IndustryType, searchType, CodesManager.PROPERTY_CATEGORY_NAICS );

			//output.InstructionalProgramType = ServiceHelper.MapReferenceFrameworkLabelLink( input.InstructionalProgramTypes, searchType, CodesManager.PROPERTY_CATEGORY_CIP );
			//
			//if ( input.CollectionMembers != null && input.CollectionMembers.Count > 0 )
			//{
			//	output.Collections = ServiceHelper.MapCollectionMemberToOutline( input.CollectionMembers );
			//}
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
			output.LifeCycleStatusType = ServiceHelper.MapPropertyLabelLink( input.LifeCycleStatusType, searchType );

			//
			//none yet, leave here for likely additions
			//MapProcessProfiles( input, ref output );
			//
			output.SameAs = ServiceHelper.MapTextValueProfileTextValue( input.SameAs );
			//
			output.TargetAssessment = ServiceHelper.MapAssessmentToAJAXSettings( input.TargetAssessment, "Has {0} Target Assessments(s)" );
			output.TargetLearningOpportunity = ServiceHelper.MapLearningOppToAJAXSettings( input.TargetLearningOpportunity, "Has {0} Target Learning Opportunit(ies)" );
			output.TargetPathway = ServiceHelper.MapPathwayToAJAXSettings( input.TargetPathway, "Has {0} Target Pathway(s)" );
			//
			output.TargetLearningResource = input.TargetLearningResource;
			//
			output.VersionIdentifier = ServiceHelper.MapIdentifierValue( input.VersionIdentifierList, "Version Identifier" );
			//QA received
			//==> need to exclude 30-published by 
			if ( input.OrganizationRole.Any() )
			{
				output.QAReceived = ServiceHelper.MapQAReceived( input.OrganizationRole, searchType );

			}

			//Competencies
			output.TeachesCompetencies = API.CompetencyFrameworkServices.ConvertCredentialAlignmentObjectFrameworkProfileToAJAXSettingsForDetail( "Teaches {#} Competenc{ies}", input.TeachesCompetenciesFrameworks );

			output.ExternalDataSetProfiles = ServiceHelper.MapToDatasetProfile( input.ExternalDataSetProfiles, searchType );

			//

			if ( input.HasTransferValueProfile != null && input.HasTransferValueProfile.Any() )
			{
				var work = new List<WMA.Outline>();
				foreach ( var target in input.HasTransferValueProfile )
				{
					if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
						work.Add( ServiceHelper.MapToOutline( target, "transfervalue" ) );
				}
				//
				output.HasTransferValue = new AJAXSettings()
				{
					Label = string.Format( "Has {0} Transfer Values", input.HasTransferValueProfile.Count ),
					Total = input.HasTransferValueProfile.Count
				};
				List<object> obj = work.Select( f => ( object ) f ).ToList();
				output.HasTransferValue.Values = obj;
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

		private static void MapProcessProfiles( ThisEntity input, ref WMA.LearningOpportunityDetail output )
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

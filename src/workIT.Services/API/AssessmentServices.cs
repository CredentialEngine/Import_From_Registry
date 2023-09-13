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
using ResourceManager = workIT.Factories.AssessmentManager;
using ResourceHelper = workIT.Services.AssessmentServices;
using ThisResource = workIT.Models.ProfileModels.AssessmentProfile;
using OutputResource = workIT.Models.API.AssessmentDetail;


namespace workIT.Services.API
{
	public class AssessmentServices
	{
		public static string searchType = "Assessment";

		public static OutputResource GetDetailForAPI( int id, bool skippingCache = false )
		{
			var request = new AssessmentRequest( 2 );
			request.IncludingProcessProfiles = UtilityManager.GetAppKeyValue( "includeProcessProfileDetails", true );
			request.AllowCaching = !skippingCache;
			//
			var output = ResourceHelper.GetDetail( id, request );
			return MapToAPI( output );

		}
		public static OutputResource GetDetailByCtidForAPI( string ctid, bool skippingCache = false )
		{
			var record = ResourceManager.GetSummaryByCtid( ctid );
			return GetDetailForAPI( record.Id, skippingCache );
		}
        public static OutputResource GetDetailForElastic( int id, bool skippingCache )
        {
            var record = ResourceHelper.GetDetail( id, skippingCache );
            return MapToAPI( record );
        }
        private static OutputResource MapToAPI( ThisResource input )
		{

			var output = new WMA.AssessmentDetail()
			{
				Meta_Id = input.Id,
				CTID = input.CTID,
				Name = input.Name,
				Meta_FriendlyName = HttpUtility.UrlPathEncode ( input.Name ),
				Description = input.Description,
				SubjectWebpage = input.SubjectWebpage,
				EntityTypeId = 3,
			};
			if ( input.EntityStateId == 0 )
				return output;

            if ( !string.IsNullOrWhiteSpace( input.CTID ) )
            {
                output.CredentialRegistryURL = RegistryServices.GetResourceUrl( input.CTID );

                output.RegistryData = ServiceHelper.FillRegistryData( input.CTID, searchType );
                //experimental - not used in UI yet
                output.RegistryDataList.Add( output.RegistryData );
            }
            
			//
			//output.CTDLType = input.assessmentType; ;
			//need a label link for header
			if ( input.OwningOrganizationId > 0 )
			{
				output.OwnedByLabel = ServiceHelper.MapDetailLink( "Organization", input.OrganizationName, input.OwningOrganizationId, input.PrimaryOrganization.FriendlyName );
			}
			//
			var ownedBy = ServiceHelper.MapOrganizationRoleProfileToOutline( input.OrganizationRole, Entity_AgentRelationshipManager.ROLE_TYPE_OWNER );
			var offeredBy = ServiceHelper.MapOrganizationRoleProfileToOutline( input.OrganizationRole, Entity_AgentRelationshipManager.ROLE_TYPE_OFFERED_BY );
			//if there is just one org, and has both owned and offered by
			if ( ownedBy != null && offeredBy != null && ownedBy.Count == 1 && offeredBy.Count == 1
				&& ownedBy[ 0 ].Meta_Id == offeredBy[ 0 ].Meta_Id )
			{
				output.OwnedOfferedBy = ServiceHelper.MapOutlineToAJAX( ownedBy, "" );
			}
			else
			{
				output.OwnedBy = ServiceHelper.MapOutlineToAJAX( ownedBy, "" );
				//
				output.OfferedBy = ServiceHelper.MapOutlineToAJAX( offeredBy, "Offered by {0} Organization(s)" );
			}
			output.Publisher = ServiceHelper.MapOutlineToAJAX( ServiceHelper.MapOrganizationRoleProfileToOutline( input.OrganizationRole, Entity_AgentRelationshipManager.ROLE_TYPE_PUBLISHEDBY ), "Published By" );
			//
			//output.OwnedBy2 = ServiceHelper.MapRoleReceived( input.OrganizationRole, searchType, Entity_AgentRelationshipManager.ROLE_TYPE_OWNER );
			//output.OwnedBy = ServiceHelper.MapOutlineToAJAX( output.OwnedBy2, "" );
			//output.OfferedBy2 = ServiceHelper.MapRoleReceived( input.OrganizationRole, searchType, Entity_AgentRelationshipManager.ROLE_TYPE_OFFERED_BY );
			//output.OfferedBy = ServiceHelper.MapOutlineToAJAX( output.OfferedBy2, "Offered by {0} Organization(s)" );
			//QA for owner,not offerer
			if ( input.OwningOrganizationQAReceived != null && input.OwningOrganizationQAReceived.Any() )
			{
				output.OwnerQAReceived = ServiceHelper.MapQAReceived( input.OwningOrganizationQAReceived, searchType );
			}
			//
			output.EntityLastUpdated = input.EntityLastUpdated;
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
			output.AlternateName = input.AlternateName;

			//
			//output.AssessmentExample = input.AssessmentExample;
			//output.AssessmentExampleDescription = input.AssessmentExampleDescription;
			output.AssessmentExample = ServiceHelper.MapPropertyLabelLink( input.AssessmentExample, "Example Data", input.AssessmentExampleDescription );

			//
			output.AssessmentMethodDescription = input.AssessmentMethodDescription;
			output.AssessmentMethodType = ServiceHelper.MapPropertyLabelLinks( input.AssessmentMethodType, searchType );
			output.AssessmentOutput = input.AssessmentOutput;
			//output.AssessmentOutput = ServiceHelper.MapPropertyLabelLink( input.AssessmentOutput, "Assessment Output", "" );

			output.AssessmentUseType = ServiceHelper.MapPropertyLabelLinks( input.AssessmentUseType, searchType );

			if ( !string.IsNullOrWhiteSpace( input.AvailabilityListing ) )
				output.AvailabilityListing = new List<string>() { input.AvailabilityListing };
			if ( !string.IsNullOrWhiteSpace( input.AvailableOnlineAt ) )
				output.AvailableOnlineAt = new List<string>() { input.AvailableOnlineAt };
			//addresses
			//
			//MapAddress( input, ref output );
			output.AvailableAt = ServiceHelper.MapAddress( input.AvailableAt );

			//
			output.AudienceLevelType = ServiceHelper.MapPropertyLabelLinks( input.AudienceLevelType, searchType, false );
			output.AudienceType = ServiceHelper.MapPropertyLabelLinks( input.AudienceType, searchType, false );

			output.ScheduleTimingType = ServiceHelper.MapPropertyLabelLinks( input.ScheduleTimingType, searchType, false );
			output.ScheduleFrequencyType = ServiceHelper.MapPropertyLabelLinks( input.ScheduleFrequencyType, searchType, false );
			output.OfferFrequencyType = ServiceHelper.MapPropertyLabelLinks( input.OfferFrequencyType, searchType, false );


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
				//output.EstimatedCost = new List<Models.Elastic.CostProfile>();
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
			if ( input.FinancialAssistance != null && input.FinancialAssistance.Any() )
			{
				output.FinancialAssistance = ServiceHelper.MapFinancialAssistanceProfiles( input.FinancialAssistance, searchType );
			}
			
			//condition profiles
			output.Corequisite = ServiceHelper.MapToConditionProfiles( input.Corequisite, searchType );
			output.CoPrerequisite = ServiceHelper.MapToConditionProfiles( input.CoPrerequisite, searchType );

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
					//		output.Corequisite = ServiceHelper.AppendConditions( item.Corequisite, output.Corequisite );
					//	}
					//	if ( item.EntryCondition != null && item.EntryCondition.Any() )
					//	{
					//		output.EntryCondition = ServiceHelper.AppendConditions( item.EntryCondition, output.EntryCondition );
					//	}
					//}
				}
			}
			//new 
			output.OccupationType = ServiceHelper.MapReferenceFramework( input.OccupationTypes, searchType, CodesManager.PROPERTY_CATEGORY_SOC );
			output.IndustryType = ServiceHelper.MapReferenceFramework( input.IndustryTypes, searchType, CodesManager.PROPERTY_CATEGORY_NAICS );
			output.InstructionalProgramType = ServiceHelper.MapReferenceFramework( input.InstructionalProgramTypes, searchType, CodesManager.PROPERTY_CATEGORY_CIP );

			//old
			//output.OccupationTypeOld = ServiceHelper.MapReferenceFrameworkLabelLink( input.OccupationType, searchType, CodesManager.PROPERTY_CATEGORY_SOC );
			//output.IndustryType = ServiceHelper.MapReferenceFrameworkLabelLink( input.IndustryType, searchType, CodesManager.PROPERTY_CATEGORY_NAICS );

			//output.InstructionalProgramType = ServiceHelper.MapReferenceFrameworkLabelLink( input.InstructionalProgramTypes, searchType, CodesManager.PROPERTY_CATEGORY_CIP );
			//
			if ( input.CollectionMembers != null && input.CollectionMembers.Count > 0 )
			{
				output.Collections = ServiceHelper.MapCollectionMemberToOutline( input.CollectionMembers );
			}
			output.IsReferenceVersion = input.IsReferenceVersion;
			//
			if ( input.Keyword != null && input.Keyword.Any() )
				output.Keyword = ServiceHelper.MapPropertyLabelLinks( input.Keyword, searchType );
			if ( input.Subject != null && input.Subject.Any() )
				output.Subject = ServiceHelper.MapPropertyLabelLinks( input.Subject, searchType );
            //
            output.SupersededBy = input.SupersededBy;
            output.Supersedes = input.Supersedes;
            //
            if ( input.IsNonCredit != null && input.IsNonCredit == true )
				output.IsNonCredit = input.IsNonCredit;
			//
			output.HasGroupEvaluation = input.HasGroupEvaluation;
			output.HasGroupParticipation = input.HasGroupParticipation;
			output.IsProctored = input.IsProctored;
			//InLanguage

			//
			output.Identifier = ServiceHelper.MapIdentifierValue( input.Identifier );
			output.LearningMethodDescription = input.LearningMethodDescription;
			output.LifeCycleStatusType = ServiceHelper.MapPropertyLabelLink( input.LifeCycleStatusType, searchType );

			//
			MapProcessProfiles( input, ref output );
			//
			//output.ProcessStandards = input.ProcessStandards;
			//output.ProcessStandardsDescription = input.ProcessStandardsDescription;
			output.ProcessStandards = ServiceHelper.MapPropertyLabelLink( input.ProcessStandards, "Process Standards", input.ProcessStandardsDescription );


			output.ScoringMethodDescription = input.ScoringMethodDescription;
			//output.ScoringMethodExample = input.ScoringMethodExample;
			//output.ScoringMethodExampleDescription = input.ScoringMethodExampleDescription;
			output.ScoringMethodExample = ServiceHelper.MapPropertyLabelLink( input.ScoringMethodExample, "Scoring Method Data", input.ScoringMethodExampleDescription );

			output.ScoringMethodType = ServiceHelper.MapPropertyLabelLinks( input.ScoringMethodType, searchType );

			output.SameAs = ServiceHelper.MapTextValueProfileTextValue( input.SameAs );
			output.TargetPathway = ServiceHelper.MapPathwayToAJAXSettings( input.TargetPathway, "Has {0} Target Pathway(s)" );
			//
			output.TargetLearningResource = input.TargetLearningResource;
			//
			output.AggregateData = ServiceHelper.MapToAggregateDataProfile( input.AggregateData, searchType );

			output.ExternalDataSetProfiles = ServiceHelper.MapToDatasetProfileList( input.ExternalDataSetProfiles, searchType );

			//
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

			//Competencies
			//21-08-24 mp - will now need to collect the framework CTIDs and add to RegistryDataList
			output.AssessesCompetencies = API.CompetencyFrameworkServices.ConvertCredentialAlignmentObjectFrameworkProfileToAJAXSettingsForDetail( "Assesses {#} Competenc{ies}", input.AssessesCompetenciesFrameworks );
			output.RequiresCompetencies = API.CompetencyFrameworkServices.ConvertCredentialAlignmentObjectFrameworkProfileToAJAXSettingsForDetail( "Requires {#} Competenc{ies}", input.RequiresCompetenciesFrameworks );

            //
            var links = new List<WMA.LabelLink>();
            output.Connections = null;

            var work = new List<WMA.Outline>();
			if ( input.HasTransferValueProfile != null && input.HasTransferValueProfile.Any() )
			{
                work = new List<WMA.Outline>();
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
				List<object> obj = work.Select( f => ( object )f ).ToList();
				output.HasTransferValue.Values = obj;
			}
            //
            if ( input.HasSupportService?.Count > 0 )
            {
                work = new List<WMA.Outline>();
                foreach ( var target in input.HasSupportService )
                {
                    if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
                        work.Add( ServiceHelper.MapToOutline( target, target.EntityType ) );
                }
                output.HasSupportService = ServiceHelper.MapOutlineToAJAX( work, "Has {0} Support Services" );

                //ServiceHelper.MapSupportServiceSearchLink( input.Id, input.Name, input.HasSupportService.Count, "Has {0} Support Services", "supportservice", ref links );

                //output.Connections = links;
            }
            //
            return output;
		}
		private static void MapAddress( ThisResource input, ref WMA.AssessmentDetail output )
		{
			//addresses
			//if ( input.Addresses.Any() )
			//{
			//	foreach ( var item in input.Addresses )
			//	{
			//		var address = new WMA.Address()
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
			//			//output.ContactPoint = new List<WMA.ContactPoint>();
			//			address.TargetContactPoint = new List<WMA.ContactPoint>();
			//			foreach ( var cp in item.ContactPoint )
			//			{
			//				var cpOutput = new WMA.ContactPoint()
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

		private static void MapJurisdictions( ThisResource org, ref WMA.AssessmentDetail output )
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
		private static void MapProcessProfiles( ThisResource input, ref WMA.AssessmentDetail output )
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
						//
						URL = ServiceHelper.finderApiSiteURL + url + item.Id.ToString(),
						TestURL = ServiceHelper.finderApiSiteURL + url + item.Id.ToString(),
					};
					//not sure we need this as part of the URL
					var qd = new ProcessProfileAjax()
					{
						Id = input.RowId.ToString(),
						ProcessTypeId = item.Id,
						
					};
					ajax.QueryData = qd;
					var processType = item.Name.Replace( " ", "" );
					switch ( processType )
					{
						case "AdministrationProcess":
							output.AdministrationProcess = ajax;
							break;
						case "DevelopmentProcess":
							output.DevelopmentProcess = ajax;
							break;
						case "MaintenanceProcess":
							output.MaintenanceProcess = ajax;
							break;
						
						default:
							break;
					}
					//output.ProcessProfiles.Add( ajax );
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

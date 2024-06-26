using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using workIT.Models.Common;
using MSR = workIT.Models.Search;
using workIT.Models.Search;
using workIT.Utilities;
using workIT.Factories;
using System.Web;

namespace workIT.Services.API
{
	//Search Services dealing with Filters
	public partial class SearchServices
	{
		#region FILTERS FOR NEW FINDER
		static bool includingHistoryFilters = false;



		public static FilterResponse GetCredentialFilters( bool getAll = false )
		{
			EnumerationServices enumServices = new EnumerationServices();
			int entityTypeId = 1;
			string searchType = "credential";
			string label = "credential";
			string labelPlural = "credentials";
			string key = searchType + "_filters";


			FilterResponse filters = new FilterResponse( searchType );

			if ( IsFilterAvailableFromCache( searchType, ref filters ) )
			{
				return filters;
			}
			var filter = new MSR.Filter();
			filters.Filters = new List<Filter>();
			try
			{
				var credentialTypes = enumServices.GetCredentialType( EnumerationType.MULTI_SELECT, getAll );
				if ( credentialTypes == null || credentialTypes.Items.Count == 0 )
				{
					//just in case
					getAll = true;
					credentialTypes = enumServices.GetCredentialType( EnumerationType.MULTI_SELECT, getAll );
				}
				var audienceTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, 14, entityTypeId, getAll );
				var audienceLevelTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, 4, entityTypeId, getAll );

				var credStatusTypes = enumServices.GetCredentialStatusType( workIT.Models.Common.EnumerationType.MULTI_SELECT, getAll );
				var credAsmtDeliveryTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, 18, entityTypeId, getAll );
				var credLoppDeliveryTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, 21, entityTypeId, getAll );
				var connections = enumServices.GetCredentialConnectionsFilters( EnumerationType.MULTI_SELECT, getAll );
				var languages = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, 65, entityTypeId, getAll );
				var qaReceived = enumServices.GetEntityAgentQAActions( EnumerationType.MULTI_SELECT, entityTypeId, getAll );
				//var nonQARoles = enumServices.GetEntityAgentNONQAActions( EnumerationType.MULTI_SELECT, entityTypeId, getAll );

				var otherFilters = enumServices.EntityStatisticGetEnumeration( entityTypeId, EnumerationType.MULTI_SELECT, getAll );
				//LWIA/address region filter
				//LWIAs proposed. If to be included, will be first
				if ( UtilityManager.GetAppKeyValue( "includingIllinoisCredentialLWIAFilters", false ) )
				{
					var lwiaFilters = CodesManager.GetAddressRegionsAsEnumeration( 1, "LWIA" );
					if ( lwiaFilters != null && lwiaFilters.Items.Any() )
					{
						filter = ConvertEnumeration( "Illinois LWIA", "lwiaType", lwiaFilters, "Select the LWIA(s)." );
						filters.Filters.Add( filter );
					}
				}
				/* TODO
				 * handle history
				 */

				if ( includingHistoryFilters )
					filters.Filters.Add( GetHistoryFilters( labelPlural ) );

				//

				if ( credentialTypes != null && credentialTypes.Items.Any() )
				{
					filter = ConvertEnumeration( "Credential Type", "credentialType", credentialTypes );
					filters.Filters.Add( filter );
				}
				//owns/offers
				filters.Filters.Add( GetOwnerFilters( "Owned/Offered By" ) );

				//=====QA
				if ( qaReceived != null && qaReceived.Items.Any() )
				{
					filter = ConvertEnumeration( "Quality Assurance", "qualityAssurance", qaReceived, string.Format( "Select the type(s) of quality assurance to filter relevant {0}.", labelPlural ) );
					//just in case
					filter.Label = "Quality Assurance";
					filter.URI = "filter:QAReceived";
					filter.Items.Add( new MSR.FilterItem()
					{
						Label = string.Format( "Optionally, find and select one or more organizations that perform {0} Quality Assurance.", label ),
						URI = "interfaceType:TextValue",
						InterfaceType = APIFilter.InterfaceType_Autocomplete
					} );
					filters.Filters.Add( filter );
				}

				//
				#region Properties
				if ( audienceLevelTypes != null && audienceLevelTypes.Items.Any() )
				{
					filter = ConvertEnumeration( "Audience Level Type", "audienceLevelType", audienceLevelTypes, "Select the type of level(s) indicating a point in a progression through an educational or training context." );
					filters.Filters.Add( filter );
				}

				if ( audienceTypes != null && audienceTypes.Items.Any() )
				{
					filter = ConvertEnumeration( "Audience Type", "audienceType", audienceTypes, string.Format( "Select the applicable audience types that are the target of {0}. Note that many {0} will not have specific audience types.", labelPlural, labelPlural ) );
					filters.Filters.Add( filter );
				}
				//
				if ( connections != null && connections.Items.Any() )
				{
					filter = ConvertEnumeration( "Connections", "credentialConnection", connections, string.Format( "Select the connection type(s) to filter {0}:", labelPlural ) );
					filters.Filters.Add( filter );
				}
				#endregion
				#region competencies don't have an autocomplete
				var competencies = new MSR.Filter( "Competencies" )
				{
					Label = "Competencies",
					Id = CodesManager.PROPERTY_CATEGORY_COMPETENCY,
					Description = string.Format( "Select 'Has Competencies' to search for {0} with any competencies required by a credential.", labelPlural ),
					//MicroSearchGuidance = string.Format( "Enter a term(s) to show {0} with relevant competencies and click Add.", labelPlural ),
					//HasAny = new FilterItem()
					//{
					//	Label = "Has Competencies (any type)",
					//	Value = "credReport:HasCompetencies"
					//}
				};
				competencies.Items.Add( new MSR.FilterItem()
				{
					//Description = string.Format( "Select 'Has Competencies' to search for {0} with any competencies.", labelPlural ),
					Label = "Has Competencies (any type)",
					URI = "credReport:HasCompetencies",
					InterfaceType = APIFilter.InterfaceType_Checkbox
				} );
				competencies.Items.Add( new MSR.FilterItem()
				{
					Label = string.Format( "Enter a term(s) to show {0} with relevant competencies and click Add.", labelPlural ),
					URI = "interfaceType:TextValue",
					InterfaceType = APIFilter.InterfaceType_Text
				} );
				filters.Filters.Add( competencies );
				#endregion
				//======================================
				//filter = new MSR.Filter( "Subjects" )
				//{
				//	Id = CodesManager.PROPERTY_CATEGORY_SUBJECT,
				//	Label = "Subject Areas",
				//	Description = "",
				//};
				//filter.Items.Add( new MSR.FilterItem()
				//{
				//	Description = string.Format( "Enter a term(s) to show {0} with relevant subjects.", labelPlural ),
				//	URI = APIFilter.FilterItem_TextValue,
				//	InterfaceType = APIFilter.InterfaceType_Autocomplete
				//} );
				filters.Filters.Add( GetSubjectFilters( labelPlural, "credReport:HasSubjects" ) );
				//check for any collections with creds. 
				//first check if there are any. This will be important initially. 
				if ( CollectionMemberManager.HasAnyForEntityType( 1 ) )
					filters.Filters.Add( GetInCollectionFilters( labelPlural, "credReport:IsPartOfCollection" ) );
				//
				//======================================
				#region Industries, occupations, and programs
				filters.Filters.Add( GetIndustryFilters( labelPlural, "credReport:HasIndustries" ) );

				filters.Filters.Add( GetOccupationFilters( labelPlural, "credReport:HasOccupations" ) );

				filters.Filters.Add( GetInstructionalProgramFilters( labelPlural, "credReport:HasCIP" ) );
				/*
				//filter = new MSR.Filter()
				//{
				//	Label = "Occupations",
				//	URI = "filter:OccupationType",
				//	Description = string.Format( "Select 'Has Occupations' to search for {0} with any occupations.", labelPlural ),
				//};
				//filter.Items.Add( new MSR.FilterItem()
				//{
				//	Label = "Has Occupations",
				//	URI = "credReport:HasOccupations",
				//	InterfaceType = APIFilter.InterfaceType_Checkbox
				//} );
				//filter.Items.Add( new MSR.FilterItem()
				//{
				//	Label = string.Format( "Find and select the occupations to filter relevant {0}.", labelPlural ),
				//	URI = "interfaceType:TextValue",
				//	InterfaceType = APIFilter.InterfaceType_Autocomplete
				//} );
				//filters.Filters.Add( filter );

				//filter = new MSR.Filter()
				//{
				//	Label = "Industries",
				//	URI = "filter:IndustryType",
				//	Description = string.Format( "Select 'Has Industries' to search for {0} with any industries.", labelPlural ),
				//};
				//filter.Items.Add( new MSR.FilterItem()
				//{
				//	Label = "Has Industries",
				//	URI = "credReport:HasIndustries",
				//	InterfaceType = APIFilter.InterfaceType_Checkbox
				//} );
				//filter.Items.Add( new MSR.FilterItem()
				//{
				//	Label = string.Format( "Find and select the industries to filter relevant {0}.", labelPlural ),
				//	URI = "interfaceType:TextValue",
				//	InterfaceType = APIFilter.InterfaceType_Autocomplete
				//} );

				//======================================
				filter = new MSR.Filter()
				{
					Label = "Instructional Programs",
					URI = "filter:InstructionalProgramType",
					Description = string.Format( "Select 'Has Instructional Programs' to search for {0} with any instructional program classifications.", labelPlural ),
				};
				filter.Items.Add( new MSR.FilterItem()
				{
					Label = "Has Instructional Programs",
					URI = "credReport:HasCIP",
					InterfaceType = APIFilter.InterfaceType_Checkbox
				} );
				filter.Items.Add( new MSR.FilterItem()
				{
					Label = string.Format( "Find and select the instructional programs to filter relevant {0}.", labelPlural ),
					URI = "interfaceType:TextValue",
					InterfaceType = APIFilter.InterfaceType_Autocomplete
				} );
				filters.Filters.Add( filter );
				*/
				#endregion

				//
				if ( credAsmtDeliveryTypes != null && credAsmtDeliveryTypes.Items.Any() )
				{
					filter = ConvertEnumeration( "Assessment Delivery Type", "assessmentDeliveryType", credAsmtDeliveryTypes, "Select the type of Assessment delivery method(s)." );
					filters.Filters.Add( filter );
				}
				if ( credLoppDeliveryTypes != null && credLoppDeliveryTypes.Items.Any() )
				{
					filter = ConvertEnumeration( "Delivery Method Types", "learningDeliveryType", credLoppDeliveryTypes, "Select the type of Learning Opportunity delivery method(s)." );
					filters.Filters.Add( filter );
				}

				//
				if ( credStatusTypes != null && credStatusTypes.Items.Any() )
				{
					filter = ConvertEnumeration( "Credential Status", "credentialStatusType", credStatusTypes, "Select one or more status to display credentials for those Credential Status" );
					filters.Filters.Add( filter );
				}

				//
				if ( languages != null && languages.Items.Count > 0 )
				{
					//filter = ConvertEnumeration( "InLanguage", languages, string.Format( "Select one or more languages to display {0} for those languages.", labelPlural ) );
					filters.Filters.Add( GetLanguageFilters( labelPlural, languages ) );
				}


				//
				if ( otherFilters != null && otherFilters.Items.Any() )
				{
					//filter = ConvertEnumeration( "OtherFilters", otherFilters, string.Format( "Select one of the 'Other' filters that are available. Note these filters are independent (ORs). For example selecting 'Has Cost Profile(s)' and 'Has Financial Aid' will show {0} that have cost profile(s) OR financial assistance.", labelPlural ) );
					filters.Filters.Add( GetTheOtherFilters( labelPlural, otherFilters ) );

				}
				//
				AddFilterToCache( filters );
			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, string.Format( "GetCredentialFilters. {0}", ex.Message ) );
			}
			return filters;
		}
		public static FilterResponse GetOrganizationFilters( bool getAll = false )
		{
			EnumerationServices enumServices = new EnumerationServices();
			int entityTypeId = 2;
			string searchType = "organization";
			string label = "organization";
			string labelPlural = "organizations";
			FilterResponse filters = new FilterResponse( searchType );
			//FilterResponse filters = new FilterResponse( searchType );

			try
			{
				var orgTypes = enumServices.GetOrganizationType( EnumerationType.MULTI_SELECT, getAll );
				if ( orgTypes == null || orgTypes.Items.Count == 0 )
				{
					//just in case
					getAll = true;
					orgTypes = enumServices.GetOrganizationType( EnumerationType.MULTI_SELECT, getAll );
				}
				var orgServiceTypes = enumServices.GetOrganizationServices( EnumerationType.MULTI_SELECT, getAll );
				var orgSectorTypes = enumServices.GetEnumeration( "agentSector", EnumerationType.SINGLE_SELECT, getAll );
				var claimTypes = enumServices.GetEnumeration( "claimType", EnumerationType.SINGLE_SELECT, getAll );
				var lifeCycleTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, entityTypeId, getAll );
				//
				var entityTypes = enumServices.GetOrgSubclasses( EnumerationType.SINGLE_SELECT, getAll );
				//var entityTypes = enumServices.GetOrgSubclasses( Models.Common.EnumerationType.MULTI_SELECT, false );


				var qaReceived = enumServices.GetEntityAgentQAActions( EnumerationType.MULTI_SELECT, entityTypeId, getAll );
				var qaPerformed = enumServices.GetEntityAgentQAActions( EnumerationType.MULTI_SELECT, entityTypeId, getAll, true );
				var otherFilters = enumServices.EntityStatisticGetEnumeration( entityTypeId, EnumerationType.MULTI_SELECT, getAll );
				//==================================

				var filter = new MSR.Filter();
				filters.Filters = new List<Filter>();
				//LWIA/address region filter
				//LWIAs proposed. If to be included, will be first
				if ( UtilityManager.GetAppKeyValue( "includingIllinoisOrganizationLWIAFilters", false ) )
				{
					var lwiaFilters = CodesManager.GetAddressRegionsAsEnumeration( 1, "LWIA" );
					if ( lwiaFilters != null && lwiaFilters.Items.Any() )
					{
						filter = ConvertEnumeration( "Illinois LWIA", "lwiaType", lwiaFilters, "Select the LWIA(s)." );
						filters.Filters.Add( filter );
					}
				}
				if ( includingHistoryFilters )
					filters.Filters.Add( GetHistoryFilters( labelPlural ) );
				//
				if ( entityTypes != null && entityTypes.Items.Count > 1 )
				{
					filter = ConvertEnumeration( "Organization Classes", "organizationSubtype", entityTypes, "Select the type(s) of Organization Classes." );
					filters.Filters.Add( filter );
				}
				//

				if ( lifeCycleTypes != null && lifeCycleTypes.Items.Count > 1 )
				{
					filter = ConvertEnumeration( "Life Cycle Status Types", "lifeCycleTypes", lifeCycleTypes, "Select the type(s) of Life Cycle Status." );
					filters.Filters.Add( filter );
				}
				//
				if ( orgTypes != null && orgTypes.Items.Any() )
				{
					filter = ConvertEnumeration( "Organization Types", "orgType", orgTypes, "Select the organization type(s)" );
					filters.Filters.Add( filter );
				}
				//
				//filter = new MSR.Filter()
				//{
				//	Label = "Industries",
				//	URI = "filter:IndustryType",
				//	Description = string.Format( "Select 'Has Industries' to search for {0} with any industries.", labelPlural ),
				//};
				//filter.Items.Add( new MSR.FilterItem()
				//{
				//	Label = "Has Industries",
				//	URI = "orgReport:HasIndustries",
				//	InterfaceType = APIFilter.InterfaceType_Checkbox
				//} );
				//filter.Items.Add( new MSR.FilterItem()
				//{
				//	Label = string.Format( "Find and select the industries to filter relevant {0}.", labelPlural ),
				//	URI = "interfaceType:TextValue",
				//	InterfaceType = APIFilter.InterfaceType_Autocomplete
				//} );
				filters.Filters.Add( GetIndustryFilters( labelPlural, "orgReport:HasIndustries" ) );

				//
				if ( orgServiceTypes != null && orgServiceTypes.Items.Any() )
				{
					filter = ConvertEnumeration( "Service Types", "serviceType", orgServiceTypes, "Select a service(s) offered by an organization." );
					filters.Filters.Add( filter );
				}
				//
				if ( orgSectorTypes != null && orgSectorTypes.Items.Any() )
				{
					filter = ConvertEnumeration( "Sector Types", "sectorType", orgSectorTypes, "Select the type of sector for an organization." );
					filters.Filters.Add( filter );
				}
				if ( claimTypes != null && claimTypes.Items.Any() )
				{
					filter = ConvertEnumeration( "Claim Types", "claimType", claimTypes, "Select the type of claim for an organization." );
					filters.Filters.Add( filter );
				}
				//
				//check for any collections with orgs
				if ( CollectionMemberManager.HasAnyForEntityType( 2 ) )
					filters.Filters.Add( GetInCollectionFilters( labelPlural, "orgfReport:IsPartOfCollection" ) );
				//
				//
				if ( qaReceived != null && qaReceived.Items.Any() )
				{
					filter = ConvertEnumeration( "Quality Assurance", "qualityAssurance", qaReceived, string.Format( "Select the type(s) of quality assurance to filter relevant {0}.", labelPlural ) );
					//just in case
					filter.Label = "Quality Assurance";
					filter.URI = "filter:QAReceived";
					filter.Items.Add( new MSR.FilterItem()
					{
						Label = string.Format( "Optionally, find and select one or more organizations that perform {0} Quality Assurance.", label ),
						URI = "interfaceType:TextValue",
						InterfaceType = APIFilter.InterfaceType_Autocomplete
					} );
					filters.Filters.Add( filter );
				}

				if ( qaPerformed != null && qaPerformed.Items.Any() )
				{
					var f = new MSR.Filter()
					{
						Label = "Quality Assurance Performed",
						URI = "filter:QAPerformed",
						//CategoryId = qaPerformed.Id,
						Description = "Select one or more types of quality assurance to display organizations that have performed the selected types of assurance.",
					};
					filter = ConvertEnumeration( f, qaPerformed );
					filter.Id = CodesManager.PROPERTY_CATEGORY_QA_PERFORMED;
					filters.Filters.Add( filter );
				}
				//
				if ( otherFilters != null && otherFilters.Items.Any() )
				{
					//filter = ConvertEnumeration( "OtherFilters", otherFilters, string.Format( "Select one of the 'Other' filters that are available. Note these filters are independent (ORs). For example selecting 'Has Credential(s)' and 'Has Assessments' will show {0} that have credential(s) OR assesments.", labelPlural ) );
					filters.Filters.Add( GetTheOtherFilters( labelPlural, otherFilters, true ) );

				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, string.Format( "GetOrganizationFilters. {0}", ex.Message ) );
			}
			return filters;
		}
		public static FilterResponse GetAssessmentFilters( bool getAll = false )
		{
			EnumerationServices enumServices = new EnumerationServices();
			int entityTypeId = 3;
			string searchType = "assessment";
			string label = "assessment";
			string labelPlural = "assessments";
			FilterResponse filters = new FilterResponse( searchType );

			try
			{
				var audienceLevelTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, 4, entityTypeId, getAll );
				if ( audienceLevelTypes == null || audienceLevelTypes.Items.Count == 0 )
				{
					//just in case
					getAll = true;
					audienceLevelTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, 4, entityTypeId, getAll );
				}
				var asmtMethodTypes = enumServices.GetEnumeration( CodesManager.PROPERTY_CATEGORY_Assessment_Method_Type, EnumerationType.MULTI_SELECT, false, getAll );
				var asmtUseTypes = enumServices.GetEnumeration( CodesManager.PROPERTY_CATEGORY_ASSESSMENT_USE_TYPE, EnumerationType.MULTI_SELECT, false, getAll );

				var audienceTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, 14, entityTypeId, getAll );
				var connections = enumServices.GetAssessmentsConditionProfileTypes( EnumerationType.MULTI_SELECT, getAll );
				//
				var lifeCycleTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, entityTypeId, getAll );
				//
				var deliveryTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE, entityTypeId, getAll );
				var languages = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, 65, entityTypeId, getAll );
				var qaReceived = enumServices.GetEntityAgentQAActions( EnumerationType.MULTI_SELECT, entityTypeId, getAll );
				var otherFilters = enumServices.EntityStatisticGetEnumeration( entityTypeId, EnumerationType.MULTI_SELECT, getAll );

				//
				var scoringMethodTypes = enumServices.GetEnumeration( CodesManager.PROPERTY_CATEGORY_Scoring_Method, EnumerationType.MULTI_SELECT, false, getAll );
				/* TODO
				 */
				var filter = new MSR.Filter();
				filters.Filters = new List<Filter>();
				//
				if ( includingHistoryFilters )
					filters.Filters.Add( GetHistoryFilters( labelPlural ) );

				filters.Filters.Add( GetOwnerFilters( "Owned/Offered By" ) );

				#region competencies don't have an autocomplete
				var competencies = new MSR.Filter( "Competencies" )
				{
					Label = "Competencies",
					Id = CodesManager.PROPERTY_CATEGORY_COMPETENCY,
					Description = string.Format( "Select 'Has Competencies' to search for {0} with any competencies assessed by an Assessment.", labelPlural ),
				};
				competencies.Items.Add( new MSR.FilterItem()
				{
					Label = "Has Competencies (any type)",
					URI = "asmtReport:HasCompetencies",
					InterfaceType = APIFilter.InterfaceType_Checkbox
				} );
				competencies.Items.Add( new MSR.FilterItem()
				{
					Label = string.Format( "Enter a term(s) to show {0} with relevant competencies and click Add.", labelPlural ),
					URI = "interfaceType:TextValue",
					InterfaceType = APIFilter.InterfaceType_Text
				} );
				filters.Filters.Add( competencies );
				#endregion
				//======================================
				//filter = new MSR.Filter( "Subjects" )
				//{
				//	Label = "Subject Areas",
				//	Description = "",
				//};
				//filter.Items.Add( new MSR.FilterItem()
				//{
				//	Description = string.Format( "Enter a term(s) to show {0} with relevant subjects.", labelPlural ),
				//	URI = APIFilter.FilterItem_TextValue,
				//	InterfaceType = APIFilter.InterfaceType_Autocomplete
				//} );
				filters.Filters.Add( GetSubjectFilters( labelPlural, "asmtReport:HasSubjects" ) );
				//
				if ( connections != null && connections.Items.Any() )
				{
					filter = ConvertEnumeration( "Connections", "assessmentConnection", connections, string.Format( "Select the connection type(s) to filter {0}:", labelPlural ) );
					filters.Filters.Add( filter );
				}

				if ( asmtMethodTypes != null && asmtMethodTypes.Items.Any() )
				{
					filter = ConvertEnumeration( "Assessment Method Type", "assessmentMethodType", asmtMethodTypes, "Select the assessment method(s) type." );
					filters.Filters.Add( filter );
				}
				if ( asmtUseTypes != null && asmtUseTypes.Items.Any() )
				{
					filter = ConvertEnumeration( "Assessment Use", "assessmentUse", asmtUseTypes, "Select the type(s) of assessment uses." );
					filters.Filters.Add( filter );
				}
				if ( deliveryTypes != null && deliveryTypes.Items.Any() )
				{
					filter = ConvertEnumeration( "Assessment Delivery Type", "assessmentdeliverytype", deliveryTypes, "Select the type of delivery method(s)." );
					filters.Filters.Add( filter );
				}
				//
				if ( audienceLevelTypes != null && audienceLevelTypes.Items.Any() )
				{
					filter = ConvertEnumeration( "Audience Level Type", "audienceleveltype", audienceLevelTypes, "Select the type of level(s) indicating a point in a progression through an educational or training context." );
					filters.Filters.Add( filter );
				}
				if ( audienceTypes != null && audienceTypes.Items.Any() )
				{
					filter = ConvertEnumeration( "Audience Type", "audiencetype", audienceTypes, "Select the applicable audience types that are the target of assessments. Note that many assessments will not have specific audience types." );
					filters.Filters.Add( filter );
				}
				if ( lifeCycleTypes != null && lifeCycleTypes.Items.Count > 1 )
				{
					filter = ConvertEnumeration( "Life Cycle Status Types", "lifeCycleTypes", lifeCycleTypes, "Select the type(s) of Life Cycle Status." );
					filters.Filters.Add( filter );
				}
				//
				if ( scoringMethodTypes != null && scoringMethodTypes.Items.Any() )
				{
					filter = ConvertEnumeration( "Scoring Method", "scoringMethod", scoringMethodTypes, "Select the type of scoring method(s)." );
					filters.Filters.Add( filter );
				}
				//check for any collections with asmts.
				//first check if there are any. This will be important initially. 
				if ( CollectionMemberManager.HasAnyForEntityType( 3 ) )
					filters.Filters.Add( GetInCollectionFilters( labelPlural, "asmtReport:IsPartOfCollection" ) );
				//
				//======================================
				#region Industries, occupations, and programs
				filters.Filters.Add( GetIndustryFilters( labelPlural, "asmtReport:HasIndustries" ) );

				filters.Filters.Add( GetOccupationFilters( labelPlural, "asmtReport:HasOccupations" ) );

				filters.Filters.Add( GetInstructionalProgramFilters( labelPlural, "asmtReport:HasCIP" ) );
				#endregion
				//
				//=====QA
				if ( qaReceived != null && qaReceived.Items.Any() )
				{
					filter = ConvertEnumeration( "Quality Assurance", "qualityAssurance", qaReceived, string.Format( "Select the type(s) of quality assurance to filter relevant {0}.", labelPlural ) );
					//just in case
					filter.Label = "Quality Assurance";
					filter.URI = "filter:QAReceived";
					filter.Items.Add( new MSR.FilterItem()
					{
						Label = string.Format( "Optionally, find and select one or more organizations that perform {0} Quality Assurance.", label ),
						URI = "interfaceType:TextValue",
						InterfaceType = APIFilter.InterfaceType_Autocomplete
					} );
					filters.Filters.Add( filter );
				}

				//
				if ( languages != null && languages.Items.Count > 0 )
				{
					//filter = ConvertEnumeration( "languages", languages, string.Format( "Select one or more languages to display {0} for those languages.", labelPlural ) );
					filters.Filters.Add( GetLanguageFilters( labelPlural, languages ) );
				}

				//
				if ( otherFilters != null && otherFilters.Items.Any() )
				{
					//filter = ConvertEnumeration( "otherFilters", otherFilters, string.Format( "Select one of the 'Other' filters that are available. Note these filters are independent (ORs). For example selecting 'Has Cost Profile(s)' and 'Has Financial Aid' will show {0} that have cost profile(s) OR financial assistance.", labelPlural ) );
					filters.Filters.Add( GetTheOtherFilters( labelPlural, otherFilters, true ) );
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, string.Format( "GetAssessmentFilters. {0}", ex.Message ) );
			}
			return filters;
		}

		public static FilterResponse GetLearningOppFilters( bool getAll = false, string widgetId = "" )
		{
			EnumerationServices enumServices = new EnumerationServices();
			int entityTypeId = 7;
			string searchType = "learningopportunity";
			string label = "learning opportunity";
			string labelPlural = "learning opportunities";
			FilterResponse filters = new FilterResponse( searchType );
			var filter = new MSR.Filter();
			filters.Filters = new List<Filter>();

			try
			{
				var audienceLevelTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, 4, entityTypeId, getAll );
				if ( audienceLevelTypes == null || audienceLevelTypes.Items.Count == 0 )
				{
					//just in case
					getAll = true;
					audienceLevelTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, 4, entityTypeId, getAll );
				}
				//TODO - if counts are based on entity typeId, could eventually have a problem where a property was used for a course but not an lopp!
				var asmtMethodTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, CodesManager.PROPERTY_CATEGORY_Assessment_Method_Type, entityTypeId, getAll );

				var audienceTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, 14, entityTypeId, getAll );
				var connections = enumServices.GetLearningOppsConditionProfileTypes( EnumerationType.MULTI_SELECT, getAll );

				//
				var deliveryTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE, entityTypeId, getAll );
				var loppAsmtDeliveryTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, CodesManager.PROPERTY_CATEGORY_ASMT_DELIVERY_TYPE, entityTypeId, getAll );
				//this should use PropertyValue, not SiteTotal
				//var learningMethodTypes2 = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, CodesManager.PROPERTY_CATEGORY_Learning_Method_Type, entityTypeId, getAll );
				var learningMethodTypes = enumServices.GetEnumeration( "learnMethod", EnumerationType.MULTI_SELECT, false );
				var learningObjectTypes = enumServices.GetLearningObjectType( Models.Common.EnumerationType.MULTI_SELECT, false );

				var languages = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, 65, entityTypeId, getAll );

				var lifeCycleTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, entityTypeId, getAll );

				var qaReceived = enumServices.GetEntityAgentQAActions( EnumerationType.MULTI_SELECT, entityTypeId, getAll );
				var otherFilters = enumServices.EntityStatisticGetEnumeration( entityTypeId, EnumerationType.MULTI_SELECT, getAll );

				//
				//LWIA/address region filter
				//LWIAs proposed. If to be included, will be first
				if ( UtilityManager.GetAppKeyValue( "includingIllinoisLoppLWIAFilters", false ) )
				{
					//****HACK **** 
					if ( UtilityManager.GetAppKeyValue( "proPathWidgetId" ) == widgetId || UtilityManager.GetAppKeyValue( "proPathWidgetName" ).ToLower() == widgetId.ToLower() )
					{
						var lwiaFilters = CodesManager.GetAddressRegionsAsEnumeration( 1, "LWIA" );
						if ( lwiaFilters != null && lwiaFilters.Items.Any() )
						{
							filter = ConvertEnumeration( "Illinois LWIA", "lwiaType", lwiaFilters, "Select the LWIA(s)." );
							filters.Filters.Add( filter );
						}

						if ( UtilityManager.GetAppKeyValue( "environment" ) != "production" )
						{
							var edrFilters = CodesManager.GetAddressRegionsAsEnumeration( 2, "EDR" );
							if ( edrFilters != null && edrFilters.Items.Any() )
							{
								filter = ConvertEnumeration( "Illinois EDR", "edrType", edrFilters, "Select the EDR(s)." );
								filters.Filters.Add( filter );
							}
						}
					}
				}
				//
				if ( includingHistoryFilters )
					filters.Filters.Add( GetHistoryFilters( labelPlural ) );
				//
				if ( learningObjectTypes != null && learningObjectTypes.Items.Count > 1 )
				{
					filter = ConvertEnumeration( "Learning Types", "learningObjectTypes", learningObjectTypes, "Select the type(s) of Learning Objects." );
					filters.Filters.Add( filter );
				}
				//
				if ( lifeCycleTypes != null && lifeCycleTypes.Items.Count > 1 )
				{
					filter = ConvertEnumeration( "Life Cycle Status Types", "lifeCycleTypes", lifeCycleTypes, "Select the type(s) of Life Cycle Status." );
					filters.Filters.Add( filter );
				}
				filters.Filters.Add( GetOwnerFilters( "Owned/Offered By" ) );
				//
				#region competencies don't have an autocomplete
				var competencies = new MSR.Filter( "Competencies" )
				{
					Label = "Competencies",
					Id = CodesManager.PROPERTY_CATEGORY_COMPETENCY,
					Description = string.Format( "Select 'Has Competencies' to search for {0} with any competencies taught by a Learning Opportuntiy.", labelPlural ),
				};
				competencies.Items.Add( new MSR.FilterItem()
				{
					//Description = string.Format( "Select 'Has Competencies' to search for {0} with any competencies.", labelPlural ),
					Label = "Has Competencies (any type)",
					URI = "loppReport:HasCompetencies",
					InterfaceType = APIFilter.InterfaceType_Checkbox
				} );
				competencies.Items.Add( new MSR.FilterItem()
				{
					Label = string.Format( "Enter a term(s) to show {0} with relevant competencies and click Add.", labelPlural ),
					URI = "interfaceType:TextValue",
					InterfaceType = APIFilter.InterfaceType_Text
				} );
				filters.Filters.Add( competencies );
				#endregion
				//
				filters.Filters.Add( GetSubjectFilters( labelPlural, "loppReport:HasSubjects" ) );
				//
				if ( connections != null && connections.Items.Any() )
				{
					filter = ConvertEnumeration( "Connections", "loppConnections", connections, string.Format( "Select the connection type(s) to filter {0}:", labelPlural ) );
					filters.Filters.Add( filter );
				}
				/* TODO
				 * QA received accredited, approved, recognized, regulated 
				 * other filters
				 * Has any?
				 *	industry, occupation, 
				 * Connections
				 * - ispartof, is prep, etc
				 */

				if ( asmtMethodTypes != null && asmtMethodTypes.Items.Any() )
				{
					filter = ConvertEnumeration( "Assessment Method Type", "assessmentMethodType", asmtMethodTypes );
					filters.Filters.Add( filter );
				}
				//
				if ( audienceLevelTypes != null && audienceLevelTypes.Items.Any() )
				{
					filter = ConvertEnumeration( "Audience Level Type", "audienceLevelType", audienceLevelTypes, "Select the type of level(s) indicating a point in a progression through an educational or training context." );
					filters.Filters.Add( filter );
				}
				if ( audienceTypes != null && audienceTypes.Items.Any() )
				{
					filter = ConvertEnumeration( "Audience Type", "audienceType", audienceTypes, string.Format( "Select the applicable audience types that are the target of {0}. Note that many {0} will not have specific audience types.", labelPlural, labelPlural ) );
					filters.Filters.Add( filter );
				}

				//
				if ( deliveryTypes != null && deliveryTypes.Items.Any() )
				{
					filter = ConvertEnumeration( "Delivery Method Types", "learningDeliveryType", deliveryTypes, "Select the type(s) of delivery method(s)." );
					filters.Filters.Add( filter );
				}

				if ( loppAsmtDeliveryTypes != null && loppAsmtDeliveryTypes.Items.Any() )
				{
					filter = ConvertEnumeration( "Assessment Delivery Type", "assessmentDeliveryType", loppAsmtDeliveryTypes, "Select the type(s) of assessment delivery method(s)." );
					filters.Filters.Add( filter );
				}

				//
				if ( learningMethodTypes != null && learningMethodTypes.Items.Any() )
				{
					//does this have to match the value used by the enumServices.Get? "learningMethodType" vs"learnMethod"
					//the option to hide for widgets is not working. trying "learnMethod". No, try back to learningMethodType (used in SiteWidgetizer)
					filter = ConvertEnumeration( "Learning Method Types", "learningMethodType", learningMethodTypes, "Select the type(s) of learning method." );
					filters.Filters.Add( filter );
				}
				//check for any collections with lopps. use Occupation as a model
				//first check if there are any. This will be important initially. 
				if ( CollectionMemberManager.HasAnyForEntityType( 7 ) )
				{
					filters.Filters.Add( GetInCollectionFilters( labelPlural, "loppReport:IsPartOfCollection" ) );
				}
				//
				#region Industries, occupations, and programs
				filters.Filters.Add( GetIndustryFilters( labelPlural, "loppReport:HasIndustries" ) );

				filters.Filters.Add( GetOccupationFilters( labelPlural, "loppReport:HasOccupations" ) );

				filters.Filters.Add( GetInstructionalProgramFilters( labelPlural, "loppReport:HasCIP" ) );

				#endregion
				//
				//=====QA
				if ( qaReceived != null && qaReceived.Items.Any() )
				{
					filter = ConvertEnumeration( "Quality Assurance", "qualityAssurance", qaReceived, string.Format( "Select the type(s) of quality assurance to filter relevant {0}.", labelPlural ) );
					//just in case
					filter.URI = "filter:QAReceived";
					filter.Items.Add( new MSR.FilterItem()
					{
						Label = string.Format( "Optionally, find and select one or more organizations that perform {0} Quality Assurance.", label ),
						URI = "interfaceType:TextValue",
						InterfaceType = APIFilter.InterfaceType_Autocomplete
					} );
					filters.Filters.Add( filter );
				}
				if ( languages != null && languages.Items.Count > 0 )
				{
					//filter = ConvertEnumeration( "languages", languages, string.Format( "Select one or more languages to display {0} for those languages.", labelPlural ) );
					filters.Filters.Add( GetLanguageFilters( labelPlural, languages ) );
				}
				//
				if ( otherFilters != null && otherFilters.Items.Any() )
				{
					//filter = ConvertEnumeration( "otherFilters", otherFilters, string.Format( "Select one of the 'Other' filters that are available. Note these filters are independent (ORs). For example selecting 'Has Cost Profile(s)' and 'Has Financial Aid' will show {0} that have cost profile(s) OR financial assistance.", labelPlural ) );
					filters.Filters.Add( GetTheOtherFilters( labelPlural, otherFilters ) );
				}

			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, string.Format( "GetLearningOppFilters. {0}", ex.Message ) );
			}
			return filters;
		}
		public static FilterResponse GetCollectionFilters( bool getAll = false, string widgetId = "" )
		{
			EnumerationServices enumServices = new EnumerationServices();
			int entityTypeId = 9;
			string searchType = "collection";
			string labelPlural = "collections";
			FilterResponse filters = new FilterResponse( searchType );

			try
			{
				//
				var collectionTypes = enumServices.GetEnumeration( "collectionCategory", EnumerationType.MULTI_SELECT, false );
				var lifeCycleTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, entityTypeId, getAll );

				var otherFilters = enumServices.EntityStatisticGetEnumeration( entityTypeId, EnumerationType.MULTI_SELECT, getAll );
				//
				var filter = new MSR.Filter();
				filters.Filters = new List<Filter>();
				if ( includingHistoryFilters )
					filters.Filters.Add( GetHistoryFilters( labelPlural ) );
				//
				if ( collectionTypes != null && collectionTypes.Items.Count > 1 )
				{
					filter = ConvertEnumeration( "Collection Types", "collectionTypes", collectionTypes, "Select the type(s) of Collection." );
					filters.Filters.Add( filter );
				}
				//owns/offers
				filters.Filters.Add( GetOwnerFilters( "Owned By" ) );
				//
				if ( lifeCycleTypes != null && lifeCycleTypes.Items.Count > 1 )
				{
					filter = ConvertEnumeration( "Life Cycle Status Types", "lifeCycleTypes", lifeCycleTypes, "Select the type(s) of Life Cycle Status." );
					filters.Filters.Add( filter );
				}
				//
				if ( otherFilters != null && otherFilters.Items.Any() )
				{
					//filter = ConvertEnumeration( "otherFilters", otherFilters, string.Format( "Select one of the 'Other' filters that are available. Note these filters are independent (ORs). For example selecting 'Has Cost Profile(s)' and 'Has Financial Aid' will show {0} that have cost profile(s) OR financial assistance.", labelPlural ) );
					filters.Filters.Add( GetTheOtherFilters( labelPlural, otherFilters ) );

				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, string.Format( "GetCollectionFilters. {0}", ex.Message ) );
			}
			return filters;
		}
		//

		public static FilterResponse GetCompetencyFrameworkFilters( bool getAll = false )
		{
			EnumerationServices enumServices = new EnumerationServices();
			int entityTypeId = 10;
			string searchType = "CompetencyFramework";
			string label = "Competency Framework";
			string labelPlural = "Competency Frameworks";
			FilterResponse filters = new FilterResponse( searchType );
			var filter = new MSR.Filter();
			filters.Filters = new List<Filter>();
			var competencyFrameworkUsingRegistrySearch = UtilityManager.GetAppKeyValue( "competencyFrameworkUsingRegistrySearch", true );

			try
			{
				/*
				response.Filters.Add( new MSR.Filter()
				{
					Label = "Competency Frameworks",
					URI = "competencyFrameworks",
					Description = string.Format( "There are no filters currently available for Competency Frameworks...", labelPlural ),
				} );
				*/
				filters.Filters.Add( new MSR.Filter()
				{
					Label = "Competency Text",
					URI = "filter:HasCompetencyWithText",
					Id = 10,
					Description = "Find frameworks with competencies that contain this text",
					Items = new List<FilterItem>()
					{
						new FilterItem()
						{
							Label = "Find Frameworks with Competencies which contain this text:",
							InterfaceType = APIFilter.InterfaceType_Text,
							URI = "filterItem:CompetencyWithText"
						}
					}
				} );
				if ( competencyFrameworkUsingRegistrySearch )
				{
					filters.Filters.Add( new MSR.Filter()
					{
						Label = "Creator/Publisher",
						URI = "filter:Provider",
						Id = 20,
						Description = "Find Frameworks based on the Framework's Creator or Publisher",
						Items = new List<FilterItem>()
					{
						new FilterItem()
						{
							Label = "Name or CTID for the Framework's Creator or Publisher:",
							InterfaceType = APIFilter.InterfaceType_Autocomplete,
							URI = "filterItem:Provider"
						}
					}
					} );
				} else
				{
					//owns/offers
					filters.Filters.Add( GetOwnerFilters( "Creator/Publisher" ) );
				}
				
				
			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, string.Format( "GetPathwayFilters. {0}", ex.Message ) );
			}

			return filters;
		}
		//
		public static FilterResponse GetPathwayFilters( bool getAll = false )
		{
			EnumerationServices enumServices = new EnumerationServices();
			int entityTypeId = 8;
			string searchType = "pathway";
			string labelPlural = "pathways";
			FilterResponse filters = new FilterResponse( searchType );

			try
			{
				var lifeCycleTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, entityTypeId, getAll );
				var otherFilters = enumServices.EntityStatisticGetEnumeration( entityTypeId, EnumerationType.MULTI_SELECT, getAll );
				//
				var filter = new MSR.Filter();
				filters.Filters = new List<Filter>();
				if ( includingHistoryFilters )
					filters.Filters.Add( GetHistoryFilters( labelPlural ) );

				//======================================
				//owns/offers - not working
				//filters.Filters.Add( GetOwnerFilters( "Owned By" ) );


				filters.Filters.Add( GetSubjectFilters( labelPlural, "pathwayReport:HasSubjects" ) );
				//

				if ( lifeCycleTypes != null && lifeCycleTypes.Items.Count > 1 )
				{
					filter = ConvertEnumeration( "Life Cycle Status Types", "lifeCycleTypes", lifeCycleTypes, "Select the type(s) of Life Cycle Status." );
					filters.Filters.Add( filter );
				}
				//
				#region Industries, occupations, and programs
				filters.Filters.Add( GetIndustryFilters( labelPlural, "pathwayReport:HasIndustries" ) );

				filters.Filters.Add( GetOccupationFilters( labelPlural, "pathwayReport:HasOccupations" ) );

				#endregion

				//
				if ( otherFilters != null && otherFilters.Items.Any() )
				{
					//filter = ConvertEnumeration( "otherFilters", otherFilters, string.Format( "Select one of the 'Other' filters that are available. Note these filters are independent (ORs). For example selecting 'Has Cost Profile(s)' and 'Has Financial Aid' will show {0} that have cost profile(s) OR financial assistance.", labelPlural ) );
					filters.Filters.Add( GetTheOtherFilters( labelPlural, otherFilters ) );

				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, string.Format( "GetPathwayFilters. {0}", ex.Message ) );
			}
			return filters;
		}
		//
		public static FilterResponse GetOwnerOnlyFilters( int entityTypeId, string searchType, string labelPlural, bool getAll = false )
		{
			EnumerationServices enumServices = new EnumerationServices();
			FilterResponse filters = new FilterResponse( searchType );

			try
			{
				var otherFilters = enumServices.EntityStatisticGetEnumeration( entityTypeId, EnumerationType.MULTI_SELECT, getAll );


				var filter = new MSR.Filter();
				filters.Filters = new List<Filter>();
				//owns/offers
				filters.Filters.Add( GetOwnerFilters( "Owned By" ) );
				//
				if ( otherFilters != null && otherFilters.Items.Any() )
				{
					//filter = ConvertEnumeration( "otherFilters", otherFilters, string.Format( "Select one of the 'Other' filters that are available. Note these filters are independent (ORs). For example selecting 'Has Cost Profile(s)' and 'Has Financial Aid' will show {0} that have cost profile(s) OR financial assistance.", labelPlural ) );
					filters.Filters.Add( GetTheOtherFilters( labelPlural, otherFilters ) );

				}

			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, string.Format( "GetPathwayFilters. {0}", ex.Message ) );
			}
			return filters;
		}

		//
		public static FilterResponse GetScheduledOfferingFilters( bool getAll = false )
		{
			EnumerationServices enumServices = new EnumerationServices();
			int entityTypeId = CodesManager.ENTITY_TYPE_SCHEDULED_OFFERING;
			string searchType = "ScheduledOffering";
			string label = "Scheduled Offering";
			string labelPlural = "Scheduled Offerings";
			FilterResponse filters = new FilterResponse( searchType );

			try
			{
				var offerFrequencyType = enumServices.GetEnumeration( CodesManager.PROPERTY_CATEGORY_OFFER_FREQUENCY, EnumerationType.MULTI_SELECT, false, getAll );
				//will need to ensure stored separately 
				var scheduleFrequencyType = enumServices.GetEnumeration( CodesManager.PROPERTY_CATEGORY_SCHEDULE_FREQUENCY, EnumerationType.MULTI_SELECT, false, getAll );
				var scheduleTiming = enumServices.GetEnumeration( CodesManager.PROPERTY_CATEGORY_SCHEDULE_TIMING, EnumerationType.MULTI_SELECT, false, getAll );
				//

				var otherFilters = enumServices.EntityStatisticGetEnumeration( entityTypeId, EnumerationType.MULTI_SELECT, getAll );

				/* TODO
				 */
				var filter = new MSR.Filter();
				filters.Filters = new List<Filter>();
				//
				if ( includingHistoryFilters )
					filters.Filters.Add( GetHistoryFilters( labelPlural ) );

				//owns/offers
				filters.Filters.Add( GetOwnerFilters( "Offered By" ) );
				//
				if ( offerFrequencyType != null && offerFrequencyType.Items.Any() )
				{
					filter = ConvertEnumeration( "Offer Frequency Type", "offerFrequencyType", offerFrequencyType, "Select the type(s) of offer frequency." );
					filters.Filters.Add( filter );
				}
				if ( scheduleTiming != null && scheduleTiming.Items.Any() )
				{
					filter = ConvertEnumeration( "Schedule Timing", "scheduleTiming", scheduleTiming, "Select the type(s) of schedule timing." );
					filters.Filters.Add( filter );
				}
				if ( scheduleFrequencyType != null && scheduleFrequencyType.Items.Any() )
				{
					filter = ConvertEnumeration( "Schedule Frequency Type", "scheduleFrequencyType", scheduleFrequencyType, "Select the type(s) of schedule frequency." );
					filters.Filters.Add( filter );
				}

				//
				//======================================

				//
				if ( otherFilters != null && otherFilters.Items.Any() )
				{
					//filter = ConvertEnumeration( "otherFilters", otherFilters, string.Format( "Select one of the 'Other' filters that are available. Note these filters are independent (ORs). For example selecting 'Has Cost Profile(s)' and 'Has Financial Aid' will show {0} that have cost profile(s) OR financial assistance.", labelPlural ) );
					filters.Filters.Add( GetTheOtherFilters( labelPlural, otherFilters, true ) );
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, string.Format( "GetSupportServiceFilters. {0}", ex.Message ) );
			}
			return filters;
		}
		//
		public static FilterResponse GetSupportServiceFilters( bool getAll = false )
		{
			EnumerationServices enumServices = new EnumerationServices();
			int entityTypeId = CodesManager.ENTITY_TYPE_SUPPORT_SERVICE;
			string searchType = "SupportService";
			string label = "Support Service";
			string labelPlural = "Support Services";
			FilterResponse filters = new FilterResponse( searchType );

			try
			{
				var accommodationTypes = enumServices.GetEnumeration( CodesManager.PROPERTY_CATEGORY_ACCOMMODATION, EnumerationType.MULTI_SELECT, false, getAll );
				var supportSrvcTypes = enumServices.GetEnumeration( CodesManager.PROPERTY_CATEGORY_SUPPORT_SERVICE_CATEGORY, EnumerationType.MULTI_SELECT, false, getAll );
				//
				var lifeCycleTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, entityTypeId, getAll );
				//do we need a custom 
				var deliveryTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE, entityTypeId, getAll );
				var languages = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, 65, entityTypeId, getAll );
				var otherFilters = enumServices.EntityStatisticGetEnumeration( entityTypeId, EnumerationType.MULTI_SELECT, getAll );

				/* TODO
				 */
				var filter = new MSR.Filter();
				filters.Filters = new List<Filter>();
				//
				if ( includingHistoryFilters )
					filters.Filters.Add( GetHistoryFilters( labelPlural ) );

				//owns/offers
				filters.Filters.Add( GetOwnerFilters( "Offered By" ) );

				if ( accommodationTypes != null && accommodationTypes.Items.Any() )
				{
					filter = ConvertEnumeration( "Accommodation Type", "accommodationType", accommodationTypes, "Select the type(s) accommodation." );
					filters.Filters.Add( filter );
				}
				if ( supportSrvcTypes != null && supportSrvcTypes.Items.Any() )
				{
					filter = ConvertEnumeration( "Support Service Category", "supportServiceCategory", supportSrvcTypes, "Select the type(s) of support service." );
					filters.Filters.Add( filter );
				}
				if ( deliveryTypes != null && deliveryTypes.Items.Any() )
				{
					filter = ConvertEnumeration( "Delivery Type", "deliverytype", deliveryTypes, "Select the type(s) of delivery method." );
					filters.Filters.Add( filter );
				}
				if ( lifeCycleTypes != null && lifeCycleTypes.Items.Count > 1 )
				{
					filter = ConvertEnumeration( "Life Cycle Status Types", "lifeCycleTypes", lifeCycleTypes, "Select the type(s) of Life Cycle Status." );
					filters.Filters.Add( filter );
				}
				//
				//======================================
				#region Industries, occupations, and programs
				filters.Filters.Add( GetOccupationFilters( labelPlural, "asmtReport:HasOccupations" ) );
				#endregion
				//

				//
				if ( languages != null && languages.Items.Count > 0 )
				{
					//filter = ConvertEnumeration( "languages", languages, string.Format( "Select one or more languages to display {0} for those languages.", labelPlural ) );
					filters.Filters.Add( GetLanguageFilters( labelPlural, languages ) );
				}

				//
				if ( otherFilters != null && otherFilters.Items.Any() )
				{
					//filter = ConvertEnumeration( "otherFilters", otherFilters, string.Format( "Select one of the 'Other' filters that are available. Note these filters are independent (ORs). For example selecting 'Has Cost Profile(s)' and 'Has Financial Aid' will show {0} that have cost profile(s) OR financial assistance.", labelPlural ) );
					filters.Filters.Add( GetTheOtherFilters( labelPlural, otherFilters, true ) );
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, string.Format( "GetSupportServiceFilters. {0}", ex.Message ) );
			}
			return filters;
		}


		public static FilterResponse GetTransferValueFilters( bool getAll = false )
		{
			EnumerationServices enumServices = new EnumerationServices();
			int entityTypeId = 26;
			string searchType = "transfervalue";
			string labelPlural = "transfer values";
			FilterResponse filters = new FilterResponse( searchType );

			try
			{
				var lifeCycleTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, entityTypeId, getAll );
				var otherFilters = enumServices.EntityStatisticGetEnumeration( entityTypeId, EnumerationType.MULTI_SELECT, getAll );
				//this wouldn't work directly with TVP, as table only has 1,3,7 and we need totals, and only want owns. So maybe don't need check boxes
				var nonQARoles = enumServices.GetEntityAgentNONQAActions( EnumerationType.MULTI_SELECT, entityTypeId, getAll );

				//
				var filter = new MSR.Filter();
				filters.Filters = new List<Filter>();
				if ( includingHistoryFilters )
					filters.Filters.Add( GetHistoryFilters( labelPlural ) );
				//
				if ( lifeCycleTypes != null && lifeCycleTypes.Items.Count > 1 )
				{
					filter = ConvertEnumeration( "Life Cycle Status Types", "lifeCycleTypes", lifeCycleTypes, "Select the type(s) of Life Cycle Status." );
					filters.Filters.Add( filter );
				}
				//
				//owns/offers
				filters.Filters.Add( GetOwnerFilters( "Provides Transfer Value" ) );
				//
				if ( UtilityManager.GetAppKeyValue( "searchFilters:IncludingTVPFromForFilters", false ) )
				{	
					filters.Filters.Add( GetOwnerFilters( "Has transfer values For", CodesManager.PROPERTY_CATEGORY_ORG_HAS_FOR_TVPS, "filter:transferValueFor", "Search for and select one or more organizations with resources \"For Transfer Values\"." ) );
					filters.Filters.Add( GetOwnerFilters( "Has transfer values From", CodesManager.PROPERTY_CATEGORY_ORG_HAS_FROM_TVPS, "filter:transferValueFrom", "Search for and select one or more organizations with resources \"From Transfer Values\"." ) );
				}
				//
				if ( lifeCycleTypes != null && lifeCycleTypes.Items.Count > 1 )
				{
					filter = ConvertEnumeration( "Life Cycle Status Types", "lifeCycleTypes", lifeCycleTypes, "Select the type(s) of Life Cycle Status." );
					filters.Filters.Add( filter );
				}
				//
				if ( otherFilters != null && otherFilters.Items.Any() )
				{
					//filter = ConvertEnumeration( "otherFilters", otherFilters, string.Format( "Select one of the 'Other' filters that are available. Note these filters are independent (ORs). For example selecting 'Has Cost Profile(s)' and 'Has Financial Aid' will show {0} that have cost profile(s) OR financial assistance.", labelPlural ) );
					filters.Filters.Add( GetTheOtherFilters( labelPlural, otherFilters ) );

				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, string.Format( "GetPathwayFilters. {0}", ex.Message ) );
			}
			return filters;
		}


		//GetOutcomeDataFilters
		public static FilterResponse GetOutcomeDataFilters( bool getAll = false )
		{
			EnumerationServices enumServices = new EnumerationServices();
			int entityTypeId = CodesManager.ENTITY_TYPE_DATASET_PROFILE;
			string searchType = "Outcome Data";
			string label = "Outcome Data";
			string labelPlural = "Outcome Data";
			FilterResponse filters = new FilterResponse( searchType );

			try
			{
				var offerFrequencyType = enumServices.GetEnumeration( CodesManager.PROPERTY_CATEGORY_OFFER_FREQUENCY, EnumerationType.MULTI_SELECT, false, getAll );
				//will need to ensure stored separately 

				//

				var otherFilters = enumServices.EntityStatisticGetEnumeration( entityTypeId, EnumerationType.MULTI_SELECT, getAll );

				/* TODO
				 */
				var filter = new MSR.Filter();
				filters.Filters = new List<Filter>();
				//
				if ( includingHistoryFilters )
					filters.Filters.Add( GetHistoryFilters( labelPlural ) );
				//owns/offers - not working
				//24-03-20 mp - seems OK now. The main issue is after removing a org filter, it is still "remembered"
				if ( UtilityManager.GetAppKeyValue( "searchFilters:IncludingOutcomeOwnerFilter", false ) )
				{
					filters.Filters.Add( GetOwnerFilters( "Provided By" ) );
				}

				//======================================

				//
				if ( otherFilters != null && otherFilters.Items.Any() )
				{
					//filter = ConvertEnumeration( "otherFilters", otherFilters, string.Format( "Select one of the 'Other' filters that are available. Note these filters are independent (ORs). For example selecting 'Has Cost Profile(s)' and 'Has Financial Aid' will show {0} that have cost profile(s) OR financial assistance.", labelPlural ) );
					filters.Filters.Add( GetTheOtherFilters( labelPlural, otherFilters, true ) );
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, string.Format( "GetSupportServiceFilters. {0}", ex.Message ) );
			}
			return filters;
		}
		public static FilterResponse GetNoFilters( string label )
		{
			FilterResponse filters = new FilterResponse( label );

			try
			{
				var filter = new MSR.Filter();
				filters.Filters = new List<Filter>();

				filter = new MSR.Filter()
				{
					Label = label,
					URI = label.ToLower().Replace( " ", "" ),
					Description = string.Format( "There are no filters currently available for {0}.", label ),
				};
				filters.Filters.Add( filter );


			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, string.Format( "GetPathwayFilters. {0}", ex.Message ) );
			}
			return filters;
		}
		private static MSR.Filter GetInCollectionFilters( string labelPlural, string hasAnyURI )
		{

			var filter = new MSR.Filter()
			{
				Id = CodesManager.PROPERTY_CATEGORY_COLLECTION_CATEGORY,
				Label = "Collections",
				URI = "filter:InCollection",
				Description = string.Format( "Select 'Has Collections' to search for {0} that are part of any collections.", labelPlural ),
			};
			filter.Items.Add( new MSR.FilterItem()
			{
				Label = "In Collection(s)",
				URI = hasAnyURI,
				InterfaceType = APIFilter.InterfaceType_Checkbox
			} );
			filter.Items.Add( new MSR.FilterItem()
			{
				Label = string.Format( "Find and select the collections to filter relevant {0}. Enter the name of a collection, such as \"ETPL\" or \"Illinois\"", labelPlural ),
				URI = "interfaceType:TextValue",
				InterfaceType = APIFilter.InterfaceType_Autocomplete
			} );

			return filter;
		}


		public static FilterResponse GetJobFilters ( bool getAll = false )
		{
			EnumerationServices enumServices = new EnumerationServices();
			int entityTypeId = 32;
			string searchType = "job";
			string labelPlural = "jobs";
			FilterResponse filters = new FilterResponse( searchType );

			try
			{
				//
				var filter = new MSR.Filter();
				filters.Filters = new List<Filter>();
				//owns/offers
				filters.Filters.Add( GetOwnerFilters( "Offered By" ) );
				//
				var lifeCycleTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, entityTypeId, getAll );
				if ( lifeCycleTypes != null && lifeCycleTypes.Items.Count >= 1 )
				{
					filter = ConvertEnumeration( "Life Cycle Status Types", "lifeCycleTypes", lifeCycleTypes, "Select the type(s) of Life Cycle Status." );
					filters.Filters.Add( filter );
				}
				#region competencies don't have an autocomplete
				var competencies = new MSR.Filter( "Competencies" )
				{
					Label = "Competencies",
					Id = CodesManager.PROPERTY_CATEGORY_COMPETENCY,
					Description = string.Format( "Select 'Has Competencies' to search for {0} with any competencies required by a job.", labelPlural ),
				};
				competencies.Items.Add( new MSR.FilterItem()
				{
					Label = "Has Competencies (any type)",
					URI = "jobReport:HasCompetencies",
					InterfaceType = APIFilter.InterfaceType_Checkbox
				} );
				competencies.Items.Add( new MSR.FilterItem()
				{
					Label = string.Format( "Enter a term(s) to show {0} with relevant competencies and click Add.", labelPlural ),
					URI = "interfaceType:TextValue",
					InterfaceType = APIFilter.InterfaceType_Text
				} );
				filters.Filters.Add( competencies );
				#endregion

			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, string.Format( "GetJobFilters. {0}", ex.Message ) );
			}
			return filters;
		}

		public static FilterResponse GetWorkClassFilters( int entityTypeId, string searchType, bool getAll = false)
		{
			EnumerationServices enumServices = new EnumerationServices();
			string labelPlural = searchType+"s";
			FilterResponse filters = new FilterResponse( searchType );

			try
			{
				//
				var filter = new MSR.Filter();
				filters.Filters = new List<Filter>();
				//owns/offers
				filters.Filters.Add( GetOwnerFilters( "Asserted By" ) );
				//
				var lifeCycleTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, CodesManager.PROPERTY_CATEGORY_LIFE_CYCLE_STATUS, entityTypeId, getAll );
				if ( lifeCycleTypes != null && lifeCycleTypes.Items.Count >= 1 )
				{
					filter = ConvertEnumeration( "Life Cycle Status Types", "lifeCycleTypes", lifeCycleTypes, "Select the type(s) of Life Cycle Status." );
					filters.Filters.Add( filter );
				}
				#region competencies don't have an autocomplete
				var competencies = new MSR.Filter( "Competencies" )
				{
					Label = "Competencies",
					Id = CodesManager.PROPERTY_CATEGORY_COMPETENCY,
					Description = string.Format( "Select 'Has Competencies' to search for {0} with any competencies required by a job.", labelPlural ),
				};
				competencies.Items.Add( new MSR.FilterItem()
				{
					Label = "Has Competencies (any type)",
					URI = searchType+"Report:HasCompetencies",
					InterfaceType = APIFilter.InterfaceType_Checkbox
				} );
				competencies.Items.Add( new MSR.FilterItem()
				{
					Label = string.Format( "Enter a term(s) to show {0} with relevant competencies and click Add.", labelPlural ),
					URI = "interfaceType:TextValue",
					InterfaceType = APIFilter.InterfaceType_Text
				} );
				filters.Filters.Add( competencies );
				#endregion

			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, string.Format( "GetWorkClassesFilters. {0}", ex.Message ) );
			}
			return filters;
		}
		#endregion
		#region filter helpers
		private static MSR.Filter GetOwnerFilters( string label, string filterType = "filter:organizationnonqaroles" )
		{
			int categoryId = CodesManager.PROPERTY_CATEGORY_ORG_OWNS_OFFERS;
			return GetOwnerFilters( label, categoryId, filterType );
		}
		private static MSR.Filter GetOwnerFilters( string label, int categoryId, string filterType = "filter:organizationnonqaroles" ,string customGuidance = "" )
		{
			var guidance = string.Format( "Search for and select one or more organizations that perform '{0}' relationships.", label );
			if (!string.IsNullOrWhiteSpace(customGuidance))
				guidance = customGuidance;

			//may want to use:	organizationnonqaroles
			var filter = new MSR.Filter()
			{
				//need a unique category id for each different filter type
				Id = categoryId,
				Label = label,
				URI = filterType,       //TBD
										//Description = string.Format( "Search for and select one or more organizations that perform '{0}' relationships.", label ),
			};
			filter.Items.Add( new MSR.FilterItem()
			{
				Label = guidance,
				URI = "interfaceType:TextValue",
				InterfaceType = APIFilter.InterfaceType_Autocomplete
			} );

			return filter;
		}
		private static MSR.Filter GetIndustryFilters( string labelPlural, string hasAnyURI )
		{
			var filter = new MSR.Filter()
			{
				Label = "Industries",
				Id = CodesManager.PROPERTY_CATEGORY_NAICS,
				URI = "filter:IndustryType",
				Description = string.Format( "Select 'Has Industries' to search for {0} with any industries.", labelPlural ),
			};
			filter.Items.Add( new MSR.FilterItem()
			{
				Label = "Has Industries",
				URI = hasAnyURI,
				InterfaceType = APIFilter.InterfaceType_Checkbox
			} );
			filter.Items.Add( new MSR.FilterItem()
			{
				//Label = string.Format( "Find and select the industries to filter relevant {0}. You must enter at least 3 characters. For example text or an industry code such as 541512, or 541.", labelPlural ),
				Label = string.Format( "Find and select the industries to filter relevant {0}.  Enter the name or code of an industry such as \"Manufacturing\" or \"3325\". ", labelPlural ),
				URI = "interfaceType:TextValue",
				InterfaceType = APIFilter.InterfaceType_Autocomplete
			} );

			return filter;
		}
		private static MSR.Filter GetOccupationFilters( string labelPlural, string hasAnyURI )
		{
			var filter = new MSR.Filter()
			{
				Id = CodesManager.PROPERTY_CATEGORY_SOC,
				Label = "Occupations",
				URI = "filter:OccupationType",
				Description = string.Format( "Select 'Has Occupations' to search for {0} with any occupations.", labelPlural ),
			};
			filter.Items.Add( new MSR.FilterItem()
			{
				Label = "Has Occupations",
				URI = hasAnyURI,
				InterfaceType = APIFilter.InterfaceType_Checkbox
			} );
			filter.Items.Add( new MSR.FilterItem()
			{
				Label = string.Format( "Find and select the occupations to filter relevant {0}. Enter the name or code of an occupation, such as \"welding\" or \"23-2011\"", labelPlural ),
				URI = "interfaceType:TextValue",
				InterfaceType = APIFilter.InterfaceType_Autocomplete
			} );

			return filter;
		}
		private static MSR.Filter GetInstructionalProgramFilters( string labelPlural, string hasAnyURI )
		{
			var filter = new MSR.Filter()
			{
				Id = CodesManager.PROPERTY_CATEGORY_CIP,
				Label = "Instructional Programs",
				URI = "filter:InstructionalProgramType",
				Description = string.Format( "Select 'Has Instructional Programs' to search for {0} with any instructional program classifications.", labelPlural ),
			};
			filter.Items.Add( new MSR.FilterItem()
			{
				Label = "Has Instructional Programs",
				URI = hasAnyURI,
				InterfaceType = APIFilter.InterfaceType_Checkbox
			} );
			filter.Items.Add( new MSR.FilterItem()
			{
				Label = string.Format( "Find and select the instructional programs to filter relevant {0}.  Enter the name or code of an instructional program such as \"Computer Programming\" or \"11.0201\". ", labelPlural ),
				URI = "interfaceType:TextValue",
				InterfaceType = APIFilter.InterfaceType_Autocomplete
			} );

			return filter;
		}
		private static MSR.Filter GetHistoryFilters( string labelPlural )
		{
			var history = new MSR.Filter( "History", "history" )
			{
				Id = CodesManager.PROPERTY_CATEGORY_HISTORY, //need a fake Id for the client. Should be OK to use the same for each type
				Label = "History",
				Description = string.Format( "Use Last Updated and Created dates to search for {0}.", labelPlural )
			};
			history.Items.Add( new MSR.FilterItem()
			{
				Description = "Last Updated",
				Label = "From",
				URI = "lastUpdatedFrom",
				InterfaceType = APIFilter.FilterItem_TextValue
			} );
			history.Items.Add( new MSR.FilterItem()
			{
				Label = "To",
				URI = "lastUpdatedTo",
				InterfaceType = APIFilter.FilterItem_TextValue
			} );
			history.Items.Add( new MSR.FilterItem()
			{
				Description = "Created",
				Label = "From",
				URI = "createdFrom",
				InterfaceType = APIFilter.FilterItem_TextValue
			} );
			history.Items.Add( new MSR.FilterItem()
			{
				Label = "To",
				URI = "createdTo",
				InterfaceType = APIFilter.FilterItem_TextValue
			} );
			return history;

		}
		private static MSR.Filter GetLanguageFilters( string labelPlural, Enumeration languages )
		{

			var filter = ConvertEnumeration( "In Language", "inLanguage", languages, string.Format( "Select one or more languages to display {0} for those languages.", labelPlural ) );
			filter.Id = CodesManager.PROPERTY_CATEGORY_LANGUAGE;

			return filter;
		}
		private static MSR.Filter GetSubjectFilters( string labelPlural, string hasAnyURI )
		{

			var filter = new MSR.Filter( "Subjects" )
			{
				Id = CodesManager.PROPERTY_CATEGORY_SUBJECT,
				Label = "Subject Areas",
				Description = "",
			};
			filter.Items.Add( new MSR.FilterItem()
			{
				Label = "Has Subjects",
				URI = hasAnyURI,
				InterfaceType = APIFilter.InterfaceType_Checkbox
			} );
			filter.Items.Add( new MSR.FilterItem()
			{
				Label = string.Format( "Enter a term(s) to show {0} with relevant subjects.", labelPlural ),
				URI = APIFilter.InterfaceType_Text,
				InterfaceType = APIFilter.InterfaceType_Autocomplete
			} );

			return filter;
		}
		private static MSR.Filter GetTheOtherFilters( string labelPlural, Enumeration otherFilters, bool usingOrgExample = false )
		{
			var example = "Select one of the 'Other' filters that are available. Note these filters are independent (ORs). For example selecting 'Has Cost Profile(s)' and 'Has Financial Aid' will show {0} that have cost profile(s) OR financial assistance.";
			var orgExample = "Select one of the 'Other' filters that are available. Note these filters are independent (ORs). For example selecting 'Has Credential(s)' and 'Has Assessments' will show {0} that have credential(s) OR assesments";
			if ( usingOrgExample )
				example = orgExample;

			var filter = new MSR.Filter();
			filter = ConvertEnumeration( "Other Filters", "otherFilters", otherFilters, string.Format( example, labelPlural ) );
			filter.Id = CodesManager.PROPERTY_CATEGORY_OTHER_FILTERS;


			return filter;
		}

		public static MSR.Filter ConvertEnumeration( MSR.Filter output, Enumeration e )
		{

			foreach ( var item in e.Items )
			{
				output.Items.Add( new MSR.FilterItem()
				{
					Id = item.Id,
					Label = item.Name,
					//Schema = item.SchemaName,
					URI = item.SchemaName,
					Description = item.Description,
					InterfaceType = MSR.APIFilter.InterfaceType_Checkbox

				} );
			}

			return output;
		}
		public static MSR.Filter ConvertEnumeration( string filterName, Enumeration e, string guidance = "" )
		{
			var filterURI = filterName.Replace( " ", "" );

			return ConvertEnumeration( filterName, filterURI, e, guidance ); ;
		}
		public static MSR.Filter ConvertEnumeration( string filterName, string filterURI, Enumeration e, string guidance = "" )
		{
			var output = new MSR.Filter( filterName )
			{
				//URI = filterName,
				Id = e.Id,
				Label = filterName,
				Description = guidance
			};
			//test
			if ( !string.IsNullOrWhiteSpace( e.SchemaName ) && e.SchemaName.IndexOf( "ceterms:" ) == 0 )
			{

			}
			foreach ( var item in e.Items )
			{
				output.Items.Add( new MSR.FilterItem()
				{
					Id = item.Id,
					Label = item.Name,
					URI = item.SchemaName,
					Description = item.Description,
					InterfaceType = MSR.APIFilter.InterfaceType_Checkbox
				} );
			}

			return output;
		}

		public static bool IsFilterAvailableFromCache( string searchType, ref FilterResponse output )
		{
			int cacheMinutes = 120;
			DateTime maxTime = DateTime.Now.AddMinutes( cacheMinutes * -1 );
			string key = searchType + "_queryFilter";

			if ( HttpRuntime.Cache[key] != null && cacheMinutes > 0 )
			{
				var cache = new CachedFilter();
				try
				{
					cache = ( CachedFilter ) HttpRuntime.Cache[key];
					if ( cache.lastUpdated > maxTime )
					{
						LoggingHelper.DoTrace( 7, string.Format( "===SearchServices.IsFilterAvailableFromCache === Using cached version of FilterQuery, SearchType: {0}.", cache.Item.SearchType ) );
						output = cache.Item;
						return true;
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.DoTrace( 5, "SearchServices.IsFilterAvailableFromCache. === exception " + ex.Message );
				}
			}

			return false;
		}
		public static void AddFilterToCache( FilterResponse entity )
		{
			int cacheMinutes = 60;

			string key = entity.SearchType + "_queryFilter";

			if ( cacheMinutes > 0 )
			{
				var newCache = new CachedFilter()
				{
					Item = entity,
					lastUpdated = DateTime.Now
				};
				if ( HttpContext.Current != null )
				{
					if ( HttpContext.Current.Cache[key] != null )
					{
						HttpRuntime.Cache.Remove( key );
						HttpRuntime.Cache.Insert( key, newCache );
						LoggingHelper.DoTrace( 7, string.Format( "SearchServices.AddFilterToCache $$$ Updating cached version of FilterQuery, SearchType: {0}", entity.SearchType ) );

					}
					else
					{
						LoggingHelper.DoTrace( 7, string.Format( "SearchServices.AddFilterToCache ****** Inserting new cached version of SearchType, SearchType: {0}", entity.SearchType ) );

						System.Web.HttpRuntime.Cache.Insert( key, newCache, null, DateTime.Now.AddHours( cacheMinutes ), TimeSpan.Zero );
					}
				}
			}

			//return entity;
		}


		#endregion
		public class CachedFilter
		{
			public CachedFilter()
			{
				lastUpdated = DateTime.Now;
			}
			public DateTime lastUpdated { get; set; }
			public FilterResponse Item { get; set; }

		}
	}
}

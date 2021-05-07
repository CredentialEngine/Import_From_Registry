using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.Caching;
using System.Web;

using Newtonsoft.Json.Linq;

using workIT.Factories;
using workIT.Models;
using workIT.Models.Common;
using ME = workIT.Models.Elastic;
using MSR = workIT.Models.Search;

using workIT.Models.ProfileModels;
using workIT.Models.Search;
using workIT.Utilities;
using ElasticHelper = workIT.Services.ElasticServices;
//using ElasticHelper7 = workIT.Services.ElasticServices7;
using CF = workIT.Factories;
using workIT.Models.Helpers;

namespace workIT.Services.API
{
	//General Search Services stuff
	public partial class SearchServices
	{
		//Shouldn't these go in the workit.Services/API/SearchServices_Filters.cs file instead?
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
				//return filters;
			}
			try
			{
				var audienceTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, 14, entityTypeId, getAll );
				var audienceLevelTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, 4, entityTypeId, getAll );
				var credentialTypes = enumServices.GetCredentialType( workIT.Models.Common.EnumerationType.MULTI_SELECT, getAll );
				var credStatusTypes = enumServices.GetCredentialStatusType( workIT.Models.Common.EnumerationType.MULTI_SELECT, getAll );
				var credAsmtDeliveryTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, 18, entityTypeId, getAll );
				var credLoppDeliveryTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, 21, entityTypeId, getAll );
				var connections = enumServices.GetCredentialConnectionsFilters( EnumerationType.MULTI_SELECT, getAll );
				var languages = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, 65, entityTypeId, getAll );
				var qaReceived = enumServices.GetEntityAgentQAActions( EnumerationType.MULTI_SELECT, entityTypeId, getAll );
				var otherFilters = enumServices.EntityStatisticGetEnumeration( entityTypeId, EnumerationType.MULTI_SELECT, getAll );
				//frameworks


				/* TODO
				 * handle history
				 */
				var filter = new MSR.Filter();
				filters.Filters = new List<Filter>();
				if( includingHistoryFilters )
					filters.Filters.Add( GetHistoryFilters( labelPlural ) );

				//
				
				if ( credentialTypes != null && credentialTypes.Items.Any() )
				{
					filter = ConvertEnumeration( "Credential Type", "credentialType", credentialTypes );
					filters.Filters.Add( filter );
				}
				//=====QA
				if ( qaReceived != null && qaReceived.Items.Any() )
				{
					filter = ConvertEnumeration( "Quality Assurance", "qualityAssurance", qaReceived, string.Format( "Select the type(s) of quality assurance to filter relevant {0}.", labelPlural ) );
					//just in case
					filter.Label = "Quality Assurance";
					filter.URI = "filter:Organization";
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
					filter = ConvertEnumeration( "Credential Connection", "credentialConnection", connections, string.Format( "Select the connection type(s) to filter {0}:", labelPlural ) );
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
				filters.Filters.Add( GetSubjectFilters( labelPlural ) );

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
					filter = ConvertEnumeration( "Learning Delivery Type", "learningDeliveryType", credLoppDeliveryTypes, "Select the type of Learning Opportunity delivery method(s)." );
					filters.Filters.Add( filter );
				}

				//
				if ( credStatusTypes != null && credStatusTypes.Items.Any() )
				{
					filter = ConvertEnumeration( "Credential Status", "credentialStatusType", credStatusTypes, "Select one or more status to display credentials for those Credential Status" );
					filters.Filters.Add( filter );
				}

				//
				if ( languages != null && languages.Items.Count > 0)
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
				var orgServiceTypes = enumServices.GetOrganizationServices( EnumerationType.MULTI_SELECT, getAll );
				var orgTypes = enumServices.GetOrganizationType( EnumerationType.MULTI_SELECT, getAll );
				var orgSectorTypes = enumServices.GetEnumeration( "orgSectorType", EnumerationType.SINGLE_SELECT, getAll );
				var claimTypes = enumServices.GetEnumeration( "claimType", EnumerationType.SINGLE_SELECT, getAll );

				var qaReceived = enumServices.GetEntityAgentQAActions( EnumerationType.MULTI_SELECT, entityTypeId, getAll );
				var qaPerformed = enumServices.GetEntityAgentQAActions( EnumerationType.MULTI_SELECT, entityTypeId, getAll, true );
				var otherFilters = enumServices.EntityStatisticGetEnumeration( entityTypeId, EnumerationType.MULTI_SELECT, getAll );
				//==================================

				var filter = new MSR.Filter();
				filters.Filters = new List<Filter>();
				if ( includingHistoryFilters )
					filters.Filters.Add( GetHistoryFilters( labelPlural ) );
				//


				if ( orgTypes != null && orgTypes.Items.Any() )
				{
					filter = ConvertEnumeration( "Organization Types", "organizationType", orgTypes, "Select the organization type(s)" );
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

				//
				if ( qaReceived != null && qaReceived.Items.Any() )
				{
					filter = ConvertEnumeration( "Quality Assurance", "qualityAssurance", qaReceived, string.Format( "Select the type(s) of quality assurance to filter relevant {0}.", labelPlural ) );
					//just in case
					filter.Label = "Quality Assurance";
					filter.URI = "filter:Organization";
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
						URI = "qualityAssurancePerformed",
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
				var asmtMethodTypes = enumServices.GetEnumeration( "assessmentMethodType", EnumerationType.MULTI_SELECT, false, getAll );
				var asmtUseTypes = enumServices.GetEnumeration( "assessmentUse", EnumerationType.MULTI_SELECT, false, getAll );
				var audienceLevelTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, 4, entityTypeId, getAll );
				var audienceTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, 14, entityTypeId, getAll );
				var connections = enumServices.GetAssessmentsConditionProfileTypes( EnumerationType.MULTI_SELECT, getAll );

				//
				var deliveryTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE, entityTypeId, getAll );
				var languages = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, 65, entityTypeId, getAll );
				var qaReceived = enumServices.GetEntityAgentQAActions( EnumerationType.MULTI_SELECT, entityTypeId, getAll );
				var otherFilters = enumServices.EntityStatisticGetEnumeration( entityTypeId, EnumerationType.MULTI_SELECT, getAll );

				//
				var scoringMethodTypes = enumServices.GetEnumeration( "scoringMethod", EnumerationType.MULTI_SELECT, false, getAll );
				/* TODO
				 */
				var filter = new MSR.Filter();
				filters.Filters = new List<Filter>();
				//
				if ( includingHistoryFilters )
					filters.Filters.Add( GetHistoryFilters( labelPlural ) );

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
				filters.Filters.Add( GetSubjectFilters( labelPlural ) );
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
				if ( asmtUseTypes != null && asmtUseTypes.Items.Any() ) { 
					filter = ConvertEnumeration( "Assessment Use", "assessmentUse", asmtUseTypes, "Select the type(s) of assessment uses." );
					filters.Filters.Add( filter );
				}
				if ( deliveryTypes != null && deliveryTypes.Items.Any() ) { 
					filter = ConvertEnumeration( "Assessment Delivery Type", "assessmentdeliverytype", deliveryTypes, "Select the type of delivery method(s)." );
					filters.Filters.Add( filter );
				}
				//
				if ( audienceLevelTypes != null && audienceLevelTypes.Items.Any() ) { 
					filter = ConvertEnumeration( "Audience Level Type", "audienceleveltype", audienceLevelTypes, "Select the type of level(s) indicating a point in a progression through an educational or training context." );
					filters.Filters.Add( filter );
				}
				if ( audienceTypes != null && audienceTypes.Items.Any() ) { 
					filter = ConvertEnumeration( "Audience Type", "audiencetype", audienceTypes, "Select the applicable audience types that are the target of assessments. Note that many assessments will not have specific audience types." );
					filters.Filters.Add( filter );
				}
				
				if ( scoringMethodTypes != null && scoringMethodTypes.Items.Any() )
					filter = ConvertEnumeration( "Scoring Method", "scoringMethod", scoringMethodTypes, "Select the type of scoring method(s)." );

				//======================================
				#region Industries, occupations, and programs
				filters.Filters.Add( GetIndustryFilters( labelPlural, "asmtReport:HasIndustries" ) );

				filters.Filters.Add( GetOccupationFilters( labelPlural, "asmtReport:HasOccupations" ) );

				filters.Filters.Add( GetInstructionalProgramFilters( labelPlural, "asmtReport:HasCIP" ) );
				/*
				filter = new MSR.Filter()
				{
					Label = "Occupations",
					URI = "filter:OccupationType",
					Description = string.Format( "Select 'Has Occupations' to search for {0} with any occupations.", labelPlural ),
				};
				filter.Items.Add( new MSR.FilterItem()
				{
					Label = "Has Occupations",
					URI = "asmtReport:HasOccupations",
					InterfaceType = APIFilter.InterfaceType_Checkbox
				} );
				filter.Items.Add( new MSR.FilterItem()
				{
					Label = string.Format( "Find and select the occupations to filter relevant {0}.", labelPlural ),
					URI = "interfaceType:TextValue",
					InterfaceType = APIFilter.InterfaceType_Autocomplete
				} );
				filters.Filters.Add( filter );

				//filter = new MSR.Filter()
				//{
				//	Label = "Industries",
				//	URI = "filter:IndustryType",
				//	Description = string.Format( "Select 'Has Industries' to search for {0} with any industries.", labelPlural ),
				//};
				//filter.Items.Add( new MSR.FilterItem()
				//{
				//	Label = "Has Industries",
				//	URI = "asmtReport:HasIndustries",
				//	InterfaceType = APIFilter.InterfaceType_Checkbox
				//} );
				//filter.Items.Add( new MSR.FilterItem()
				//{
				//	Label = string.Format( "Find and select the industries to filter relevant {0}.", labelPlural ),
				//	URI = "interfaceType:TextValue",
				//	InterfaceType = APIFilter.InterfaceType_Autocomplete
				//} );
				filters.Filters.Add( GetIndustryFilters( labelPlural, "asmtReport:HasIndustries" ) );
				

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
					URI = "asmtReport:HasCIP",
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
				//=====QA
				if ( qaReceived != null && qaReceived.Items.Any() )
				{
					filter = ConvertEnumeration( "Quality Assurance", "qualityAssurance", qaReceived, string.Format( "Select the type(s) of quality assurance to filter relevant {0}.", labelPlural ) );
					//just in case
					filter.Label = "Quality Assurance";
					filter.URI = "filter:Organization";
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
				if ( otherFilters != null && otherFilters.Items.Any() ) { 
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

		public static FilterResponse GetLearningOppFilters( bool getAll = false )
		{
			EnumerationServices enumServices = new EnumerationServices();
			int entityTypeId = 7;
			string searchType = "learningopportunity";
			string label = "learning opportunity";
			string labelPlural = "learningopportunities";
			FilterResponse filters = new FilterResponse( searchType );
			var filter = new MSR.Filter();
			filters.Filters = new List<Filter>();

			try
			{
				//
				var asmtMethodTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, CodesManager.PROPERTY_CATEGORY_Assessment_Method_Type, entityTypeId, getAll );
				var audienceLevelTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, 4, entityTypeId, getAll );
				var audienceTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, 14, entityTypeId, getAll );
				var connections = enumServices.GetLearningOppsConditionProfileTypes( EnumerationType.MULTI_SELECT, getAll );

				//
				var deliveryTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE, entityTypeId, getAll );
				var loppAsmtDeliveryTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, CodesManager.PROPERTY_CATEGORY_ASMT_DELIVERY_TYPE, entityTypeId, getAll );
				var learningMethodTypes = enumServices.GetEnumeration( "learningMethodType", EnumerationType.MULTI_SELECT, false, getAll );
				var languages = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, 65, entityTypeId, getAll );
				var qaReceived = enumServices.GetEntityAgentQAActions( EnumerationType.MULTI_SELECT, entityTypeId, getAll );
				var otherFilters = enumServices.EntityStatisticGetEnumeration( entityTypeId, EnumerationType.MULTI_SELECT, getAll );

				//
				if ( includingHistoryFilters )
					filters.Filters.Add( GetHistoryFilters( labelPlural ) );

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
				filters.Filters.Add( GetSubjectFilters( labelPlural ) );
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
				if ( audienceLevelTypes != null && audienceLevelTypes.Items.Any() ) { 
					filter = ConvertEnumeration( "Audience Level", "audienceLevelType", audienceLevelTypes, "Select the type of level(s) indicating a point in a progression through an educational or training context." );
					filters.Filters.Add( filter );
				}
				if ( audienceTypes != null && audienceTypes.Items.Any() ) { 
					filter = ConvertEnumeration( "Audience Type", "audienceType", audienceTypes, string.Format( "Select the applicable audience types that are the target of {0}. Note that many {0} will not have specific audience types.", labelPlural, labelPlural ) );
					filters.Filters.Add( filter );
				}

				//
				if ( deliveryTypes != null && deliveryTypes.Items.Any() ) { 
					filter = ConvertEnumeration( "Learning Delivery Type", "learningDeliveryType", deliveryTypes, "Select the type(s) of delivery method(s)." );
					filters.Filters.Add( filter );
				}

				if ( loppAsmtDeliveryTypes != null && loppAsmtDeliveryTypes.Items.Any() ) { 
					filter = ConvertEnumeration( "Assessment Delivery Type", "assessmentDeliveryType", loppAsmtDeliveryTypes, "Select the type(s) of assessment delivery method(s)." );
					filters.Filters.Add( filter );
				}

				//
				if ( learningMethodTypes != null && learningMethodTypes.Items.Any() ) { 
					filter = ConvertEnumeration( "Learning Method Types", "learningMethodType", learningMethodTypes, "Select the type(s) of learning method." );
					filters.Filters.Add( filter );
				}
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
					filter.URI = "filter:Organization";
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
				if ( otherFilters != null && otherFilters.Items.Any() ) { 
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

		public static FilterResponse GetPathwayFilters( bool getAll = false )
		{
			EnumerationServices enumServices = new EnumerationServices();
			int entityTypeId = 8;
			string searchType = "pathway";
			string label = "Pathway";
			string labelPlural = "pathways";
			FilterResponse filters = new FilterResponse( searchType );

			try
			{

				var otherFilters = enumServices.EntityStatisticGetEnumeration( entityTypeId, EnumerationType.MULTI_SELECT, getAll );
				//
				var filter = new MSR.Filter();
				filters.Filters = new List<Filter>();
				if ( includingHistoryFilters )
					filters.Filters.Add( GetHistoryFilters( labelPlural ) );

				//======================================
				filters.Filters.Add( GetSubjectFilters( labelPlural ) );
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

			try
			{
				filter = new MSR.Filter()
				{
					Label = "Competency Frameworks",
					URI = "competencyFrameworks",
					Description = string.Format( "There are no filters currently available for Competency Frameworks.", labelPlural ),
				};
				filters.Filters.Add( filter );

			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, string.Format( "GetPathwayFilters. {0}", ex.Message ) );
			}
			return filters;
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
				Label = string.Format( "Find and select the industries to filter relevant {0}.", labelPlural ),
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
				Label = string.Format( "Find and select the occupations to filter relevant {0}.", labelPlural ),
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
				Label = string.Format( "Find and select the instructional programs to filter relevant {0}.", labelPlural ),
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
		private static MSR.Filter GetSubjectFilters( string labelPlural)
		{

			var filter = new MSR.Filter( "Subjects" )
			{
				Id = CodesManager.PROPERTY_CATEGORY_SUBJECT,
				Label = "Subject Areas",
				Description = "",
			};
			filter.Items.Add( new MSR.FilterItem()
			{
				Description = string.Format( "Enter a term(s) to show {0} with relevant subjects.", labelPlural ),
				URI = APIFilter.FilterItem_TextValue,
				InterfaceType = APIFilter.InterfaceType_Autocomplete
			} );

			return filter;
		}
		private static MSR.Filter GetTheOtherFilters( string labelPlural, Enumeration otherFilters, bool usingOrgExample = false)
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

			if ( HttpRuntime.Cache[ key ] != null && cacheMinutes > 0 )
			{
				var cache = new CachedFilter();
				try
				{
					cache = ( CachedFilter )HttpRuntime.Cache[ key ];
					if ( cache.lastUpdated > maxTime )
					{
						LoggingHelper.DoTrace( 6, string.Format( "===SearchServices.IsFilterAvailableFromCache === Using cached version of FilterQuery, SearchType: {0}.", cache.Item.SearchType ) );
						output = cache.Item;
						return true;
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.DoTrace( 6, "SearchServices.IsFilterAvailableFromCache. === exception " + ex.Message );
				}
			}

			return false;
		}
		public static void AddFilterToCache( FilterResponse entity )
		{
			int cacheMinutes = 120;

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
					if ( HttpContext.Current.Cache[ key ] != null )
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
	}

	//Shouldn't this go in the workit.Services/API/SearchServices_Filters.cs file instead?
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.Caching;
using System.Web;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using workIT.Factories;
using workIT.Models;
using workIT.Models.Common;
using ME = workIT.Models.Elastic;
using MSR = workIT.Models.Search;

using workIT.Models.ProfileModels;
using workIT.Models.QData;
using workIT.Models.Search;
using workIT.Utilities;
using ElasticHelper = workIT.Services.ElasticServices;
//using ElasticHelper7 = workIT.Services.ElasticServices7;
using CF = workIT.Factories;
using workIT.Models.Helpers;
using workIT.Models.API.RegistrySearchAPI;
using System.Text.RegularExpressions;

namespace workIT.Services
{
	public class SearchServices
	{
		public static string thisClassName = "workIT.Services.SearchServices";
		//private static readonly object LoggingHelper;
		DateTime checkDate = new DateTime( 2016, 1, 1 );
		public MainSearchResults MainSearch( MainSearchInput data, ref bool valid, ref string status, JObject debug = null )
		{
			debug = debug ?? new JObject();

			LoggingHelper.DoTrace( 6, "SearchServices.MainSearch - entered" );
			//Sanitize input
			data.Keywords = string.IsNullOrWhiteSpace( data.Keywords ) ? "" : data.Keywords;
			data.Keywords = ServiceHelper.CleanText( data.Keywords );
			data.Keywords = ServiceHelper.HandleApostrophes( data.Keywords );
			data.Keywords = data.Keywords.Trim();

			//Sanitize input
			//need to translate sort order - should already be done, but not working in AB dev.
			//sortOrder:MostRelevant, sortOrder:Oldest, sortOrder:Newest, sortOrder:AtoZ, sortOrder:ZtoA
			if (!string.IsNullOrWhiteSpace( data.SortOrder ) && data.SortOrder.IndexOf("sortOrder:") == 0 )
			{
				switch ( data.SortOrder )
				{
					case "sortOrder:MostRelevant":
						data.SortOrder = "relevance";
						break;
					case "sortOrder:Oldest":
						data.SortOrder = "oldest";
						break;	
					case "sortOrder:Newest":
						data.SortOrder = "newest";
						break;
					case "sortOrder:AtoZ":
						data.SortOrder = "alpha";
						break;
					case "sortOrder:ZtoA":
						data.SortOrder = "zalpha";
						break;
					default:
						data.SortOrder = "newest";
						break;

				}
			}
			var validSortOrders = new List<string>() { "newest", "oldest", "relevance", "alpha", "cost_lowest", "cost_highest", "duration_shortest", "duration_longest", "org_alpha", "zalpha" };
			if ( !validSortOrders.Contains( data.SortOrder ) )
			{
				data.SortOrder = validSortOrders.First();
			}

			//Default blind searches to "newest" when "relevance" is selected
			if( string.IsNullOrWhiteSpace( data.Keywords ) && data.FiltersV2.Count() == 0 && data.SortOrder == "relevance" )
			{
				data.SortOrder = "newest";
			}
			if ( data.PageSize < 10 || data.PageSize > 100 )
				data.PageSize = 25;

			//Determine search type
			var searchType = data.SearchType;
			if ( string.IsNullOrWhiteSpace( searchType ) )
			{
				valid = false;
				status = "Unable to determine search mode";
				return null;
			}

			//Testing
			debug.Add( "Raw Query", JObject.FromObject( data ) );
			/*
			var finderAPITest = new JObject();
			try
			{
				//this was already done in the API search controller!
				var converted = Services.API.SearchServices.TranslateMainSearchInputToMainQuery( data );
				finderAPITest.Add( "MainQuery", JObject.FromObject( converted, new JsonSerializer() { NullValueHandling = NullValueHandling.Ignore } ) );

				var roundTrip = Services.API.SearchServices.TranslateMainQueryToMainSearchInput( converted );
				finderAPITest.Add( "MainSearchInput", JObject.FromObject( roundTrip, new JsonSerializer() { NullValueHandling = NullValueHandling.Ignore } ) );
			}
			catch ( Exception ex )
			{
				finderAPITest.Add( "Error Converting MainSearchInput to MainQuery", ex.Message );
			}
			try
			{
				debug.Add( "Finder API Test", finderAPITest );
			}
			catch { }
			*/

			//Do the search
			LoggingHelper.DoTrace( 7, thisClassName + ".MainSearch. Calling: " + searchType );
			var competencyFrameworkUsingRegistrySearch = UtilityManager.GetAppKeyValue( "competencyFrameworkUsingRegistrySearch", true );
			var totalResults = 0;
			switch ( searchType.ToLower() )
			{
				case "credential":
				{
					if ( data.UseSimpleSearch )
					{
						var results = ElasticHelper.CredentialSimpleSearch( data, ref totalResults );
						return ConvertCredentialResults( results, totalResults, searchType );
					}
					else
					{
						var results = CredentialServices.Search( data, ref totalResults );
						return ConvertCredentialResults( results, totalResults, searchType );
					}


				}
				case "organization":
				{
					if ( data.UseSimpleSearch )
					{
						var results = ElasticHelper.OrganizationSimpleSearch( data, ref totalResults );
						return ConvertOrganizationResults( results, totalResults, searchType );
					}
					else
					{
						var results = OrganizationServices.Search( data, ref totalResults );
						return ConvertOrganizationResults( results, totalResults, searchType );
					}
				}
				case "assessment":
				{
					if ( data.UseSimpleSearch )
					{
						var results = ElasticHelper.AssessmentSimpleSearch( data, ref totalResults );
						return ConvertAssessmentResults( results, totalResults, searchType );
					}
					else
					{
						var results = AssessmentServices.Search( data, ref totalResults );
						return ConvertAssessmentResults( results, totalResults, searchType );
					}
				}
				case "learningopportunity":
				{
					if ( data.UseSimpleSearch )
					{
						var results = ElasticHelper.LearningOppSimpleSearch( data, ref totalResults );
						return ConvertLearningOpportunityResults( results, totalResults, searchType );
					}
					else
					{
						var results = LearningOpportunityServices.Search( data, ref totalResults );
						return ConvertLearningOpportunityResults( results, totalResults, searchType );
					}
				}
				case "cf":
				{
					var results = ElasticHelper.CompetencyFrameworkSearch( data, ref totalResults );
					return ConvertCompetencyFrameworkResults2( results, totalResults, searchType );
				}
				case "competencyframeworkold":
				{
					var results = CompetencyFrameworkServices.SearchViaRegistry( data, false );
					return ConvertCompetencyFrameworkResults( results, searchType );
				}
				case "competencyframework":
				case "competencyframeworks":
				case "competencyframeworkdsp":
				{
					if ( competencyFrameworkUsingRegistrySearch )
					{
						var results = CompetencyFrameworkServicesV2.SearchViaRegistry( data, true );
						//debug.Add( "Raw Results Data", JObject.FromObject( results ) ); //Temporary
						return ConvertCompetencyFrameworkResultsV2( results );
					}
					else
                    {
						var results = ElasticHelper.CompetencyFrameworkSearch( data, ref totalResults );
						return ConvertCompetencyFrameworkResults2( results, totalResults, searchType );
					}
				}
				case "conceptscheme":
				{
					var results = ConceptSchemeServices.Search( data, ref totalResults );
					return ConvertConceptSchemeResults( results, totalResults, searchType );
				}
				case "pathway":
				{
					var results = PathwayServices.PathwaySearch( data, ref totalResults );
					return ConvertGeneralIndexResults( results, totalResults, searchType, "Pathway" );
				}
				case "pathwayset":
				{
					var results = PathwayServices.PathwaySetSearch( data, ref totalResults );
					//return ConvertGeneralIndexResults( results, totalResults, searchType );	"Pathway Set"
					//return ConvertPathwaySetResults( results, totalResults, searchType );
					return ConvertGeneralIndexResults( results, totalResults, searchType, "Pathway Set" );

					//
				}
				case "transfervalue":
				{
					var results = TransferValueServices.Search( data, ref totalResults );
					//var results2 = ElasticHelper.GeneralSearch( CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, data, ref totalResults );
					//return ConvertTransferValueResults( results, totalResults, searchType );
					return ConvertGeneralIndexResults( results, totalResults, searchType, "Transfer Value Profile" );
				}
				case "transferintermediary":
					{
						var results = TransferIntermediaryServices.Search( data, ref totalResults );
						return ConvertGeneralIndexResults( results, totalResults, searchType, "Transfer Intermediary" );
					}
				case "datasetprofile":
				{
					var results = DataSetProfileServices.Search( data, ref totalResults );
					return ConvertDataSetProfileResults( results, totalResults, searchType );
				}
				case "collection":
				{
					var results = CollectionServices.CollectionSearch( data, ref totalResults );
					return ConvertCollectionResults( results, totalResults, searchType );
				}
				default:
				{
					valid = false;
					status = "Unknown search mode: " + searchType;
					return null;
				}
			}
		}

		//Do an autocomplete
		public static List<FilterItem> DoAutoComplete( MainSearchInput data, ref bool valid, ref string status )
		{
			var results = new List<FilterItem>();
			int totalRows = 0;
			var textValues = new List<string>();
			data.PageSize = UtilityManager.GetAppKeyValue( "autocompleteTerms", 15 );
			//Sanitize input
			data.Keywords = string.IsNullOrWhiteSpace( data.Keywords ) ? "" : data.Keywords;
			data.Keywords = ServiceHelper.CleanText( data.Keywords );
			data.Keywords = ServiceHelper.HandleApostrophes( data.Keywords );
			data.Keywords = data.Keywords.Trim();
			//
			data.SortOrder = "relevance";
			// Default blind searches to "newest" when "relevance" is selected - should not be possible.
			if ( string.IsNullOrWhiteSpace( data.Keywords ) && data.FiltersV2.Count() == 0 && data.SortOrder == "relevance" )
			{
				data.SortOrder = "newest";
			}
			//Determine search type
			var searchType = data.SearchType;
			if ( string.IsNullOrWhiteSpace( searchType ) )
			{
				valid = false;
				status = "Unable to determine search mode";
				return null;
			}
			var context = data.AutocompleteContext;
			if ( string.IsNullOrWhiteSpace( context ) )
			{
				valid = false;
				status = "Unable to determine the autocomplete context.";
				return null;
			}
			//temp
			var text = data.Keywords;
			int widgetId = 0; //obsolete
			switch ( searchType.ToLower() )
			{
				case "credential":
					{
						switch ( context.ToLower() )
						{
							//case "mainsearch": return CredentialServices.Autocomplete( text, data.PageSize ).Select( m => m.Name ).ToList();
							case "mainsearch":
								textValues = CredentialServices.Autocomplete( data, data.PageSize/*, widgetId*/ ); break; //why is widgetId commented out here?
							//case "competencies":
							//	return CredentialServices.AutocompleteCompetencies( text, data.PageSize );
							case "subjects":
							case "filter:subjects":
								textValues = Autocomplete_Subjects( data, CF.CodesManager.ENTITY_TYPE_CREDENTIAL, CF.CodesManager.PROPERTY_CATEGORY_SUBJECT, text, data.PageSize ); break;
							case "occupations"://
							case "filter:occupationtype":
								textValues = Autocomplete_Occupations( data, CF.CodesManager.ENTITY_TYPE_CREDENTIAL, text, data.PageSize ); break;
							case "industries":
							case "filter:industrytype":
								textValues = Autocomplete_Industries( data, CF.CodesManager.ENTITY_TYPE_CREDENTIAL, text, data.PageSize ); break;
							case "instructionalprogramtypes":
							case "filter:instructionalprogramtype":
								textValues = Autocomplete_Cip( data, CF.CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, text, data.PageSize ); break;
							case "organizations":
							case "filter:organization":
							case "filter:qareceived":
								//why was maxTerms set to 1 here? 22-05-06 mp - set to default
								return ElasticHelper.OrganizationQAAutoComplete( text, data.PageSize );
							case "organizationnonqaroles":
							case "filter:organizationnonqaroles":
								//why was maxTerms set to 1 here? 22-05-06 mp - set to default
								return ElasticHelper.OrganizationAutoComplete( data, data.PageSize, ref totalRows );
							case "filter:location":
							case "location":
								var total = 0;
								var geodata = new ThirdPartyApiServices().GeoNamesSearch( text, 1, data.PageSize, null, ref total, true );
								textValues = geodata.Select( f => f.LocationFormatted ).ToList();
								break;
							default:
								break;
						}
						break;
					}
				case "organization":
					{
						switch ( context.ToLower() )
						{
							case "mainsearch":
								textValues = OrganizationServices.Autocomplete( data, data.PageSize, widgetId ); break;
							case "industries":
							case "filter:industrytype":
								textValues = Autocomplete_Industries( data, CF.CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, text, data.PageSize ); break;
							case "organizations":
							case "filter:organization":
							case "filter:qareceived":
								//why was maxTerms set to 2 here? 22-05-06 mp - set to default
								return ElasticHelper.OrganizationQAAutoComplete( text, data.PageSize );
							default:
								break;
						}
						break;
					}
				case "assessment":
					{
						switch ( context.ToLower() )
						{
							case "mainsearch":
								textValues = AssessmentServices.Autocomplete( data, data.PageSize/*, widgetId */); break;
							//case "competencies":
							//	return AssessmentServices.Autocomplete( text, "competencies", data.PageSize );
							case "subjects":
							case "filter:subjects":
								textValues = Autocomplete_Subjects( data, CF.CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, CF.CodesManager.PROPERTY_CATEGORY_SUBJECT, text, data.PageSize ); break;
							case "occupations"://
							case "filter:occupationtype":
								textValues = Autocomplete_Occupations( data, CF.CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, text, data.PageSize ); break;
							case "industries":
							case "filter:industrytype":
								textValues = Autocomplete_Industries( data, CF.CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, text, data.PageSize ); break;
							case "instructionalprogramtypes":
							case "filter:instructionalprogramtype":
								textValues = Autocomplete_Cip( data, CF.CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, text, data.PageSize ); break;
							case "organizations":
							case "filter:organization":
							case "filter:qareceived":
								return ElasticHelper.OrganizationQAAutoComplete( text, 3 );
							default:
								break;
						}
						break;
					}
				case "learningopportunity":
					{
						if ( context == null )
						{
							//temp. best guess
							context = "filter:occupationtype";
						}
						switch ( context.ToLower() )
						{
							case "mainsearch":
								textValues = LearningOpportunityServices.Autocomplete( data, widgetId );
								//textValues = LearningOpportunityServices.Autocomplete( data, text, 15, widgetId );
								break;
							//case "competencies":
							//	return LearningOpportunityServices.Autocomplete( text, "competencies", data.PageSize );
							case "subjects":
							case "filter:subjects":
								textValues = Autocomplete_Subjects( data, CF.CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, CF.CodesManager.PROPERTY_CATEGORY_SUBJECT, text, data.PageSize ); break;
							case "occupations"://
							case "filter:occupationtype":
								textValues = Autocomplete_Occupations( data, CF.CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, text, data.PageSize ); break;
							//
							case "industries":
							case "filter:industrytype":
								textValues = Autocomplete_Industries( data, CF.CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, text, data.PageSize ); break;
							case "instructionalprogramtypes":
							case "filter:instructionalprogramtype":
								textValues = Autocomplete_Cip( data, CF.CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, text, data.PageSize ); break;
							case "organizations":
							case "filter:organization":
							case "filter:qareceived":
								return ElasticHelper.OrganizationQAAutoComplete( text, data.PageSize );
							default:
								break;
						}
						break;
					}
				case "cf":
					{
						switch ( context.ToLower() )
						{
							case "mainsearch":
								textValues = ElasticHelper.CompetencyFrameworkAutoComplete( text, data.PageSize, ref totalRows ); break;
							//case "competencies":
							//	return LearningOpportunityServices.Autocomplete( text, "competencies", data.PageSize );
							//case "subjects":
							//	return Autocomplete_Subjects( CF.CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, CF.CodesManager.PROPERTY_CATEGORY_SUBJECT, text, data.PageSize );
							default:
								break;
						}
						break;
					}
				case "conceptscheme":
					{
						switch ( context.ToLower() )
						{
							case "mainsearch":
								var sdata = ConceptSchemeServices.Autocomplete( text, data.PageSize, ref totalRows );
								textValues = sdata.Select( f => f.Name ).ToList();
								break;
							default:
								break;
						}
						break;
					}
				case "pathway":
					{
						switch ( context.ToLower() )
						{
							case "mainsearch":
								textValues = ElasticServices.PathwayAutoComplete( data, data.PageSize, ref totalRows );
								break;
							default:
								break;
						}
						break;
					}
				case "location": //or is this a context under each top level?
					{
						switch ( context.ToLower() )
						{
							case "mainsearch":
								var total = 0;
								var sdata = new ThirdPartyApiServices().GeoNamesSearch( text, 1, data.PageSize, null, ref total, true );
								textValues = sdata.Select( f => f.LocationFormatted ).ToList();
								break;
							default:
								break;
						}
						break;
					}
				default:
					break;
			}

			results = textValues != null && textValues.Count() > 0 ? textValues.Select( m => new FilterItem() { Label = m, Text = m } ).ToList() : results;

			return results;
		}

		public static List<FilterItem> DoAutoCompleteOLD( string searchType, string context, string text, int widgetId = 0 )
	{
			var results = new List<FilterItem>();
			int totalRows = 0;
			var textValues = new List<string>();
			MainSearchInput query = new MainSearchInput()
			{
				SearchType = searchType,
				Keywords = text,
				AutocompleteContext = context
			};
			switch ( searchType.ToLower() )
			{
				case "credential":
				{
					switch ( context.ToLower() )
					{
						//case "mainsearch": return CredentialServices.Autocomplete( text, 10 ).Select( m => m.Name ).ToList();
						case "mainsearch":
							textValues = CredentialServices.Autocomplete( query, 15/*, widgetId*/ ); break; //why is widgetId commented out here?
						//case "competencies":
						//	return CredentialServices.AutocompleteCompetencies( text, 10 );
						case "subjects":
						case "filter:subjects":
							textValues = Autocomplete_Subjects( query, CF.CodesManager.ENTITY_TYPE_CREDENTIAL, CF.CodesManager.PROPERTY_CATEGORY_SUBJECT, text, 10 ); break;
						case "occupations"://
						case "filter:occupationtype":
							textValues = Autocomplete_Occupations( query, CF.CodesManager.ENTITY_TYPE_CREDENTIAL, text, 10 ); 
								break;
						case "industries":
						case "filter:industrytype":
							textValues = Autocomplete_Industries( query, CF.CodesManager.ENTITY_TYPE_CREDENTIAL, text, 10 ); 
								break;
						case "instructionalprogramtypes":
						case "filter:instructionalprogramtype":
							textValues = Autocomplete_Cip( query, CF.CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, text, 10 ); 
								break;
						case "organizations":
						case "filter:organization":
						case "filter:qareceived":
							//why was maxTerms set to 1 here? 22-05-06 mp - set to 10
							return ElasticHelper.OrganizationQAAutoComplete( text, 10 );
						case "organizationnonqaroles":
						case "filter:organizationnonqaroles":
							//why was maxTerms set to 1 here? 22-05-06 mp - set to 10
							return ElasticHelper.OrganizationAutoComplete( query, 10, ref totalRows ); 
						case "filter:location":
						case "location":
							var total = 0;
							var data = new ThirdPartyApiServices().GeoNamesSearch( text, 1, 15, null, ref total, true );
							textValues = data.Select( f => f.LocationFormatted ).ToList();
							break;
						default:
							break;
					}
					break;
				}
				case "organization":
				{
					switch ( context.ToLower() )
					{
						case "mainsearch":
							textValues = OrganizationServices.Autocomplete( query, 10, widgetId ); break;
						case "industries":
						case "filter:industrytype":
							textValues = Autocomplete_Industries( query, CF.CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, text, 10 ); break;
						case "organizations":
						case "filter:organization":
						case "filter:qareceived":
							return ElasticHelper.OrganizationQAAutoComplete( text, 2 );
						default:
							break;
					}
					break;
				}
				case "assessment":
				{
					switch ( context.ToLower() )
					{
						case "mainsearch":
							textValues = AssessmentServices.Autocomplete( query, 15/*, widgetId */); break;
						//case "competencies":
						//	return AssessmentServices.Autocomplete( text, "competencies", 10 );
						case "subjects":
						case "filter:subjects":
							textValues = Autocomplete_Subjects( query, CF.CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, CF.CodesManager.PROPERTY_CATEGORY_SUBJECT, text, 10 ); break;
						case "occupations"://
						case "filter:occupationtype":
							textValues = Autocomplete_Occupations( query, CF.CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, text, 10 ); break;
						case "industries":
						case "filter:industrytype":
							textValues = Autocomplete_Industries( query, CF.CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, text, 10 ); break;
						case "instructionalprogramtypes":
						case "filter:instructionalprogramtype":
							textValues = Autocomplete_Cip( query, CF.CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, text, 10 ); break;
						case "organizations":
						case "filter:organization":
						case "filter:qareceived":
							return ElasticHelper.OrganizationQAAutoComplete( text, 3 );
						default:
							break;
					}
					break;
				}
				case "learningopportunity":
				{
						if ( context  == null)
                        {
							//temp. best guess
							context = "filter:occupationtype";
						}
					switch ( context.ToLower() )
					{
						case "mainsearch":
							textValues = LearningOpportunityServices.AutocompleteOld( text, 15, widgetId );
							//textValues = LearningOpportunityServices.Autocomplete( text, 15, widgetId );
							break;
						//case "competencies":
						//	return LearningOpportunityServices.Autocomplete( text, "competencies", 10 );
						case "subjects":
						case "filter:subjects":
							textValues = Autocomplete_Subjects( query, CF.CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, CF.CodesManager.PROPERTY_CATEGORY_SUBJECT, text, 10 ); break;
						case "occupations"://
						case "filter:occupationtype":
							textValues = Autocomplete_Occupations( query, CF.CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, text, 10 ); break;
								//
						case "industries":
						case "filter:industrytype":
							textValues = Autocomplete_Industries( query, CF.CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, text, 10 ); break;
						case "instructionalprogramtypes":
						case "filter:instructionalprogramtype":
							textValues = Autocomplete_Cip( query, CF.CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, text, 10 ); break;
						case "organizations":
						case "filter:organization":
						case "filter:qareceived":
							return ElasticHelper.OrganizationQAAutoComplete( text, 7 );
						default:
							break;
					}
					break;
				}
				case "cf":
				{
					switch ( context.ToLower() )
					{
						case "mainsearch":
							textValues = ElasticHelper.CompetencyFrameworkAutoComplete( text, 15, ref totalRows ); break;
						//case "competencies":
						//	return LearningOpportunityServices.Autocomplete( text, "competencies", 10 );
						//case "subjects":
						//	return Autocomplete_Subjects( CF.CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, CF.CodesManager.PROPERTY_CATEGORY_SUBJECT, text, 10 );
						default:
							break;
					}
					break;
				}
				case "conceptscheme":
				{
					switch ( context.ToLower() )
					{
						case "mainsearch":
							var data = ConceptSchemeServices.Autocomplete( text, 15, ref totalRows ); 
							textValues = data.Select( f => f.Name ).ToList();
							break;
						default:
							break;
					}
					break;
				}
				case "location": //or is this a context under each top level?
				{
					switch ( context.ToLower() )
					{
						case "mainsearch":
							var total = 0;
							var data = new ThirdPartyApiServices().GeoNamesSearch( text, 1, 15, null, ref total, true );
							textValues = data.Select( f => f.LocationFormatted ).ToList();
							break;
						default:
							break;
					}
					break;
				}
				default:
					break;
			}

			results = textValues != null && textValues.Count() > 0 ? textValues.Select( m => new FilterItem() { Label = m, Text = m } ).ToList() : results;

			return results;
		}
		//
		public static List<Credential> EntityCredentialsList( string searchType, int recordId, int maxRecords = 10 )
		{
			var results = new List<Credential>();
			switch ( searchType.ToLower() )
			{
				case "credential":
				{
					return CF.Entity_ConditionProfileManager.GetAllCredentails( 1, recordId );
				}
				case "learningopportunity":
				{
					return CF.Entity_ConditionProfileManager.GetAllCredentails( 7, recordId );
				}
				case "transfervalue":
				{
					return CF.TransferValueProfileManager.GetAllCredentials( CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, recordId );

				}
				default:
					break;
			}

			return results;
		}
		//
		public static List<AssessmentProfile> EntityAssesmentsList( string searchType, int recordId, int maxRecords = 10 )
		{
			var results = new List<AssessmentProfile>();
			switch ( searchType.ToLower() )
			{
				case "credential":
				{
					return CF.Entity_ConditionProfileManager.GetAllAssessments( 1, recordId );
				}
				case "learningopportunity":
				{
					return CF.Entity_ConditionProfileManager.GetAllAssessments( 7, recordId );
				}
				case "transfervalue":
				{
					return CF.TransferValueProfileManager.GetAllAssessments( CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, recordId );

				}
				default:
					break;
			}

			return results;
		}
		//
		public static List<LearningOpportunityProfile> EntityLoppsList( string searchType, int recordId, int maxRecords = 10 )
		{
			var results = new List<LearningOpportunityProfile>();
			switch ( searchType.ToLower() )
			{
				case "credential":
				{
					return CF.Entity_ConditionProfileManager.GetAllLearningOpportunities( 1, recordId );
				}
				case "learningopportunity":
				{
					return CF.Entity_ConditionProfileManager.GetAllLearningOpportunities( 7, recordId );
				}
				case "transfervalue":
				{
					return CF.TransferValueProfileManager.GetAllLearningOpportunities( CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, recordId );

				}
				default:
					break;
			}

			return results;
		}
		//
		public static List<CredentialAlignmentObjectItem> EntityCompetenciesList(string searchType, int artifactId, int maxRecords = 10)
		{
			var results = new List<CredentialAlignmentObjectItem>();
			string filter = "";
			int pTotalRows = 0;
			switch ( searchType.ToLower() )
			{
				case "credential":
				{
					//not sure if will be necessary to include alignment type (ie teaches, and assesses, but not required)
					filter = string.Format( "(CredentialId = {0})", artifactId );
					return CF.CompetencyFrameworkManager.Search( filter, "", 1, maxRecords, ref pTotalRows );
				}
				case "assessment":
				{
					return CF.Entity_CompetencyManager.GetAll( 3, artifactId, maxRecords );
					//filter = string.Format( "(SourceEntityTypeId = 3 AND [SourceId] = {0})", artifactId );
					//return CF.CompetencyFrameworkManager.Search( filter, "", 1, maxRecords, ref pTotalRows );
				}
				case "learningopportunity":
				{
					return CF.Entity_CompetencyManager.GetAll( 7, artifactId, maxRecords );
					//filter = string.Format( "(SourceEntityTypeId = 7 AND [SourceId] = {0})", artifactId );
					//return CF.CompetencyFrameworkManager.Search( filter, "", 1, maxRecords, ref pTotalRows );
				}
				default:
					break;
			}

			return results;
		}

		public static List<CostProfileItem> EntityCostsList(string searchType, int entityId, int maxRecords = 10)
		{
			var results = new List<CostProfileItem>();
			string filter = "";
			int pTotalRows = 0;

			switch ( searchType.ToLower() )
			{
				case "credential":
				{
					filter = "";
					return CF.CostProfileItemManager.Search( 1, entityId, filter, "", 1, maxRecords, ref pTotalRows );
				}
				case "assessment":
				{
					filter = "";
					return CF.CostProfileItemManager.Search( 3, entityId, filter, "", 1, maxRecords, ref pTotalRows );
				}
				case "learningopportunity":
				{
					filter = "";
					return CF.CostProfileItemManager.Search( 7, entityId, filter, "", 1, maxRecords, ref pTotalRows );
				}
				default:
					break;
			}

			return results;
		}
		public static List<FinancialAssistanceProfile> EntityFinancialAssistanceList( string searchType, int entityId, int maxRecords = 10 )
		{
			var results = new List<FinancialAssistanceProfile>();

			switch ( searchType.ToLower() )
			{
				case "credential":
				{
					return CF.Entity_FinancialAssistanceProfileManager.Search( 1, entityId );
				}
				case "assessment":
				{
					return CF.Entity_FinancialAssistanceProfileManager.Search( 3, entityId );
				}
				case "learningopportunity":
				{
					return CF.Entity_FinancialAssistanceProfileManager.Search( 7, entityId );
				}
				default:
					break;
			}

			return results;
		}
		public static List<CredentialAlignmentObjectItem> EntityQARolesList(string searchType, int entityId, int maxRecords = 10)
		{
			var results = new List<CredentialAlignmentObjectItem>();
			string filter = "";
			int pTotalRows = 0;
			switch ( searchType.ToLower() )
			{
				case "credential":
				{
					//not sure if will be necessary to include alignment type (ie teaches, and assesses, but not required)
					filter = string.Format( "(CredentialId = {0})", entityId );
					return CF.CompetencyFrameworkManager.Search( filter, "", 1, maxRecords, ref pTotalRows );
				}
				case "assessment":
				{
					filter = string.Format( "(SourceEntityTypeId = 3 AND [SourceId] = {0})", entityId );
					return CF.CompetencyFrameworkManager.Search( filter, "", 1, maxRecords, ref pTotalRows );
				}
				case "learningopportunity":
				{
					filter = string.Format( "(SourceEntityTypeId = 7 AND [SourceId] = {0})", entityId );
					return CF.CompetencyFrameworkManager.Search( filter, "", 1, maxRecords, ref pTotalRows );
				}
				default:
					break;
			}

			return results;
		}
		public static List<OrganizationAssertion> QAPerformedList(int orgId, int maxRecords = 10)
		{
			//try to use combined!
			return CF.Entity_AssertionManager.GetAllQAPerformedCombined( orgId, maxRecords );
		}

		//Convenience method to handle location data
		//For convenience, check boundaries.IsDefined to see if a boundary is defined
		public static BoundingBox GetBoundaries(MainSearchInput data, string name)
		{
			var boundaries = new BoundingBox();
			if ( data.FiltersV2 == null || !data.FiltersV2.Any() )
				return boundaries;
			try
			{
				var target = data.FiltersV2.FirstOrDefault( m => m.Name == name );
				if (target != null )
					boundaries = target.AsBoundaries();
				//boundaries = data.FiltersV2.FirstOrDefault( m => m.Name == name ).AsBoundaries();
			}
			catch { }

			return boundaries;
		}
		//
		/// <summary>
		/// explain relationships
		/// anything added to ButtonCategoryTypes, add to here as all upper case.
		/// In SearchV2.cshtml apparantly need a translation in method: handler_GetRelatedItemsViaAJAX
		/// </summary>
		public enum TagTypes { CONNECTIONS, QUALITY, ASSESSMENT, HAS_ASSESSMENT, AUDIENCE_LEVEL, AUDIENCE_TYPE, LEARNINGOPPORTUNITY, OCCUPATIONS, INDUSTRIES, SUBJECTS, HAS_CREDENTIAL, COMPETENCIES, TIME, COST, ORGANIZATIONTYPE, ORGANIZATIONSECTORTYPE, ORG_SERVICE_TYPE, OWNED_BY, OFFERED_BY, ASMTS_OWNED_BY, HAS_LOPP, LOPPS_OWNED_BY, FINANCIAL, FRAMEWORKS_OWNED_BY, ASMNT_DELIVER_METHODS, DELIVER_METHODS, ASSESSMENT_USE_TYPES, ASSESSMENT_METHOD_TYPES, LEARNING_METHODS, INSTRUCTIONAL_PROGRAM, OWNS_CREDENTIAL, OFFERS_CREDENTIAL, QAPERFORMED, SCORING_METHODS, REFERENCED_BY_CREDENTIAL, REFERENCED_BY_ASSESSMENT, REFERENCED_BY_LOPP, TVPHASCREDENTIAL, TVPHASASSESSMENT, TVPHASLOPP, TRANSFERVALUE, HASTRANSFERVALUE, PATHWAY, PATHWAYSET, DATASETPROFILE, DATASETPROFILE_CREDENTIAL, TRANSFERINTERMEDIARY }

		public enum ButtonSearchTypes { Organization, Credential, AssessmentProfile, LearningOpportunityProfile, CompetencyFramework, Pathway, PathwaySet }
		/// <summary>
		/// how do ButtonCategoryTypes relate to TagTypes  
		/// </summary>
		public enum ButtonCategoryTypes { QualityAssuranceReceived, QualityAssurancePerformed, OrganizationOwns, OrganizationOffers, Connection, Credential, AssessmentProfile, LearningOpportunityProfile, OccupationType, IndustryType, InstructionalProgramType, Subject, Competency, EstimatedDuration, EstimatedCost, FinancialAssistance, AudienceLevelType, AudienceType, LearningDeliveryType, AssessmentDeliveryType, OrganizationType, SectorType, ServiceType, AssessmentUseType, AssessmentMethodType, ScoringMethodType, LearningMethodType, TransferValue, HasTransferValue, TVPHasCredential, TVPHasAssessment, TVPHasLopp, Pathway, PathwaySet, OutcomeData, HoldersProfile, EarningsProfile, EmploymentOutcomeProfile, DataSetProfile } //, OtherTags
																																																																																																																																																																																						 //
		public enum ButtonHandlerTypes { handler_RenderDetailPageLink, handler_RenderQualityAssurance, handler_RenderConnection, handler_RenderCheckboxFilter, handler_RenderExternalCodeFilter, handler_GetRelatedItemsViaAJAX } //use a string that is the function name itself for custom handlers
		public enum ButtonTargetEntityTypes { Credential, Assessment, LearningOpportunity, CompetencyFramework, Competency, EstimatedCost, FinancialAssistance, QualityAssurancePerformed, TransferValue, TVPHasCredential, TVPHasAssessment, TVPHasLearningOpportunity, Pathway, PathwaySet, DataSetProfile }

		public MainSearchResults ConvertCredentialResults(List<CredentialSummary> results, int totalResults, string searchType)
		{
			var output = new MainSearchResults() { TotalResults = totalResults, SearchType = searchType };
			if ( results == null || results.Count == 0 )
				return output;
			int cntr = 0;
			foreach ( var item in results )
			{
				cntr++;
				if (item.Id == 40682 )
                {

                }
				try
				{
					var mergedCosts = item.NumberOfCostProfileItems; // CostProfileMerged.FlattenCosts( item.EstimatedCost );
					var subjects = Deduplicate( item.Subjects );
					var mergedQA = item.AgentAndRoles.Results.Concat( item.Org_QAAgentAndRoles.Results ).ToList();
					var mergedConnections = item.CredentialsList.Results.ToList();
					var iconUrl = GetCredentialIcon( item.CredentialTypeSchema.ToLower() );
					var statusLabel = "";
					if ( UtilityManager.GetAppKeyValue( "showingNonActiveStatusLabel", false ) )
					{
						if ( !string.IsNullOrWhiteSpace( item.CredentialStatus ) && item.CredentialStatus != "Active" )
							statusLabel = " [** " + item.CredentialStatus.ToUpper() + " **]";
					}
					//.Concat( item.IsPartOfList.Results ).Concat( item.HasPartsList.Results ).ToList();
					output.Results.Add( Result( item.Name, item.FriendlyName, item.Description, item.Id,
						new Dictionary<string, object>()
						{
						{ "Name", item.Name },
						{ "Description", item.Description },
						{ "Type", item.CredentialType },
						{ "Owner", item.OwnerOrganizationName },
						{ "OwnerId", item.OwnerOrganizationId },
						{ "OwnerCTID", item.PrimaryOrganizationCTID },
						//{ "CanEditRecord", item.CanEditRecord },
						{ "TypeSchema", item.CredentialTypeSchema.ToLower()},
						{ "AvailableAt", new { Type = "locations", Data = ConvertAddresses( item.Addresses ) } },
						{ "Locations", ConvertAddresses( item.Addresses ) },
							{ "CredentialStatus", item.CredentialStatus },
						{ "SearchType", searchType },
						{ "RecordId", item.Id },
						{ "ResultNumber", item.ResultNumber },
						{ "ctid", item.CTID },
						{ "UrlTitle", item.FriendlyName },
						{ "OrganizationUrlTitle", item.PrimaryOrganizationFriendlyName },
						{ "ResultImageUrl", item.ImageUrl ?? "" },
						{ "ResultIconUrl", iconUrl ?? "" },
						{ "HasDegreeConcentation", item.HasDegreeConcentation ?? "" },
						{ "HasBadge", item.HasVerificationType_Badge }, //Indicate existence of badge here
                        { "Created", item.Created.ToShortDateString() },
						{ "LastUpdated", item.LastUpdated.ToShortDateString() },
						{ "T_LastUpdated", item.LastUpdated.ToString("yyyy-MM-dd HH:mm:ss") },
						{ "T_StateId", item.EntityStateId },
						{ "T_BroadType", "Credential" },
						{ "T_CTDLType", item.CredentialTypeSchema },
						{ "T_CTDLTypeLabel", item.CredentialType },
						{ "T_Locations", ConvertAddressesToJArray(item.Addresses) },
						{ "T_ImageURL", item.ImageUrl }

						},
						new List<TagSet>(),
						//SearchTag is OBSOLETE, use SearchResultButton
						new List<Models.Helpers.SearchTag>(),

						new List<SearchResultButton>()
						{
						//Credential Quality Assurance
						new SearchResultButton()
						{
							CategoryLabel = "Quality Assurance",
							CategoryType = ButtonCategoryTypes.QualityAssuranceReceived.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderQualityAssurance.ToString(),
							TotalItems = mergedQA.Count(),
							Items = mergedQA.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( m ) ).ToList()
						},
						//Connections
						new SearchResultButton()
						{
							CategoryLabel = mergedConnections.Count() == 1 ? "Connection" : "Connections",
							CategoryType = ButtonCategoryTypes.Connection.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderConnection.ToString(),
							TotalItems = mergedConnections.Count(),
							Items = mergedConnections.Take(10).ToList().ConvertAll( m => JObject.FromObject( new SearchResultButton.Helpers.DetailPageLink()
							{
								ConnectionLabel = m.Connection,
								ConnectionType = m.ConnectionId.ToString(),
								TargetLabel = m.Credential,
								TargetType = ButtonSearchTypes.Credential.ToString(),
								TargetId = m.CredentialId
							} ) )
						},
						//Financial Assistance
						new SearchResultButton()
						{
							CategoryLabel = "Financial Assistance",
							CategoryType = ButtonCategoryTypes.FinancialAssistance.ToString(),
							HandlerType = ButtonHandlerTypes.handler_GetRelatedItemsViaAJAX.ToString(),
							TotalItems = item.FinancialAidCount,
							RenderData = JObject.FromObject(new SearchResultButton.Helpers.AjaxDataForResult()
							{
								AjaxQueryName = "GetFinancialAssistance",
								TargetEntityType = ButtonTargetEntityTypes.FinancialAssistance.ToString()
							} )
						},
						//Related Assessments
						new SearchResultButton()
						{
							CategoryLabel = item.RequiredAssessmentsCount + item.RecommendedAssessmentsCount == 1 ? "Related Assessment" : "Related Assessments",
							CategoryType = ButtonCategoryTypes.AssessmentProfile.ToString(),
							HandlerType = ButtonHandlerTypes.handler_GetRelatedItemsViaAJAX.ToString(),
							TotalItems = item.RequiredAssessmentsCount + item.RecommendedAssessmentsCount,
							RenderData = JObject.FromObject( new SearchResultButton.Helpers.AjaxDataForResult()
							{
								AjaxQueryName = "GetSearchResultAsmt",
								TargetEntityType = ButtonTargetEntityTypes.Assessment.ToString()
							} )
						},
						//Related Learning Opportunities
						new SearchResultButton()
						{
							CategoryLabel = item.RequiredLoppCount + item.RecommendedLoppCount == 1 ? "Related Learning Opportunity" : "Related Learning Opportunities",
							CategoryType = ButtonCategoryTypes.LearningOpportunityProfile.ToString(),
							HandlerType = ButtonHandlerTypes.handler_GetRelatedItemsViaAJAX.ToString(),
							TotalItems = item.RequiredLoppCount + item.RecommendedLoppCount,
							RenderData = JObject.FromObject( new SearchResultButton.Helpers.AjaxDataForResult()
							{
								AjaxQueryName = "GetSearchResultLopp",
								TargetEntityType = ButtonTargetEntityTypes.LearningOpportunity.ToString()
							} )
						},
						//Audience Level Type
						new SearchResultButton()
						{
							CategoryLabel = item.AudienceLevelsResults.Results.Count() == 1 ? "Audience Level" : "Audience Levels",
							CategoryType = ButtonCategoryTypes.AudienceLevelType.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderCheckboxFilter.ToString(),
							TotalItems = item.AudienceLevelsResults.Results.Count(),
							Items = item.AudienceLevelsResults.Results.ConvertAll( m => JObject.FromObject( GetFilterItem( m, item.AudienceLevelsResults.CategoryId ) ) )
						},
						//Audience Type
						new SearchResultButton()
						{
							CategoryLabel = item.AudienceTypesResults.Results.Count() == 1 ? "Audience Type" : "Audience Types",
							CategoryType = ButtonCategoryTypes.AudienceType.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderCheckboxFilter.ToString(),
							TotalItems = item.AudienceTypesResults.Results.Count(),
							Items = item.AudienceTypesResults.Results.ConvertAll( m => JObject.FromObject( GetFilterItem( m, item.AudienceTypesResults.CategoryId ) ) )
						},
						//Occupations
						new SearchResultButton()
						{
							CategoryLabel = item.OccupationResults.Results.Count() == 1 ? "Occupation" : "Occupations",
							CategoryType = ButtonCategoryTypes.OccupationType.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderExternalCodeFilter.ToString(),
							TotalItems = item.OccupationResults.Results.Count(),
							Items = item.OccupationResults.Results.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( GetFilterItem( m, item.OccupationResults.CategoryId ) ) )
						},
						//Industries
						new SearchResultButton()
						{
							CategoryLabel = item.IndustryResults.Results.Count() == 1 ? "Industry" : "Industries",
							CategoryType = ButtonCategoryTypes.IndustryType.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderExternalCodeFilter.ToString(),
							TotalItems = item.IndustryResults.Results.Count(),
							Items = item.IndustryResults.Results.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( GetFilterItem( m, item.IndustryResults.CategoryId ) ) )
						},
						//Instructional Program Classifications
						new SearchResultButton()
						{
							CategoryLabel = item.InstructionalProgramClassification.Results.Count() == 1 ? "Instructional Program Type" : "Instructional Program Types",
							CategoryType = ButtonCategoryTypes.InstructionalProgramType.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderExternalCodeFilter.ToString(),
							TotalItems = item.InstructionalProgramClassification.Results.Count(),
							Items = item.InstructionalProgramClassification.Results.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( GetFilterItem( m, item.InstructionalProgramClassification.CategoryId ) ) )
						},
						//Assessment Delivery Type
						new SearchResultButton()
						{
							CategoryLabel = item.AssessmentDeliveryType.Results.Count() == 1 ? "Assessment Delivery Type" : "Assessment Delivery Types",
							CategoryType = ButtonCategoryTypes.AssessmentDeliveryType.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderCheckboxFilter.ToString(),
							TotalItems = item.AssessmentDeliveryType.Results.Count(),
							Items = item.AssessmentDeliveryType.Results.ConvertAll( m => JObject.FromObject( GetFilterItem( m, item.AssessmentDeliveryType.CategoryId ) ) )
						},
						//Learning Delivery Type
						new SearchResultButton()
						{
							CategoryLabel = item.LearningDeliveryType.Results.Count() == 1 ? "Learning Delivery Type" : "Learning Delivery Types",
							CategoryType = ButtonCategoryTypes.LearningDeliveryType.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderCheckboxFilter.ToString(),
							TotalItems = item.LearningDeliveryType.Results.Count(),
							Items = item.LearningDeliveryType.Results.ConvertAll( m => JObject.FromObject( GetFilterItem( m, item.LearningDeliveryType.CategoryId ) ) )
						},
						//Subjects
						new SearchResultButton()
						{
							CategoryLabel = subjects.Count() == 1 ? "Subject" : "Subjects",
							CategoryType = ButtonCategoryTypes.Subject.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderDetailPageLink.ToString(),
							TotalItems = subjects.Count(),
							Items = item.Subjects.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( new SearchResultButton.Helpers.DetailPageLink()
							{
								TargetLabel = m,
								TargetType = ButtonSearchTypes.Credential.ToString(),
								TargetId = item.Id
							} ) )
						},
						//OutcomeProfiles
						new SearchResultButton()
						{//note item.AggregateDataProfileCount + item.DataSetProfileCount were already combined in Credential_MapFromElastic
							CategoryLabel = item.AggregateDataProfileCount == 1 ? "Outcome Data" : "Outcome Data",
							CategoryType = ButtonCategoryTypes.OutcomeData.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderDetailPageLink.ToString(),
							TotalItems = item.AggregateDataProfileCount,
							Items = new List<string>() { item.AggregateDataProfileSummary }.ToList().ConvertAll( m => JObject.FromObject( new SearchResultButton.Helpers.DetailPageLink()
							{
								ConnectionLabel = "Outcome Data",
								TargetLabel = m,
								TargetType = ButtonSearchTypes.Credential.ToString(),
								TargetId = item.Id
							} ) )
						},
						//Related TVP
						new SearchResultButton()
						{
							CategoryLabel = item.TransferValueCount == 1 ? "Has Transfer Value" : "Has Transfer Values",
							CategoryType = ButtonCategoryTypes.HasTransferValue.ToString(),
							HandlerType = ButtonHandlerTypes.handler_GetRelatedItemsViaAJAX.ToString(),
							TotalItems = item.TransferValueCount,
							RenderData = JObject.FromObject( new SearchResultButton.Helpers.AjaxDataForResult()
							{
								AjaxQueryName = "GetSearchResultTVP",
								TargetEntityType = ButtonTargetEntityTypes.TransferValue.ToString()
							} )
						},
						//EarningsProfiles
						//new SearchResultButton()
						//{
						//	CategoryLabel = item.EarningsProfileCount == 1 ? "Earnings Profile" : "Earnings Profiles",
						//	CategoryType = ButtonCategoryTypes.EarningsProfile.ToString(),
						//	HandlerType = ButtonHandlerTypes.handler_RenderDetailPageLink.ToString(),
						//	TotalItems = item.EarningsProfileCount,
						//	Items = new List<string>() { item.EarningsProfileSummary }.ToList().ConvertAll( m => JObject.FromObject( new SearchResultButton.Helpers.DetailPageLink()
						//	{
						//		ConnectionLabel = "Earnings Profile",
						//		TargetLabel = m,
						//		TargetType = ButtonSearchTypes.Credential.ToString(),
						//		TargetId = item.Id
						//	} ) )
						//},
						//EmploymentOutcomeProfiles
						//new SearchResultButton()
						//{
						//	CategoryLabel = item.EmploymentOutcomeProfileCount == 1 ? "EmploymentOutcome Profile" : "EmploymentOutcome Profiles",
						//	CategoryType = ButtonCategoryTypes.EmploymentOutcomeProfile.ToString(),
						//	HandlerType = ButtonHandlerTypes.handler_RenderDetailPageLink.ToString(),
						//	TotalItems = item.EmploymentOutcomeProfileCount,
						//	Items = new List<string>() { item.EmploymentOutcomeProfileSummary }.ToList().ConvertAll( m => JObject.FromObject( new SearchResultButton.Helpers.DetailPageLink()
						//	{
						//		ConnectionLabel = "Employment Outcome Profile",
						//		TargetLabel = m,
						//		TargetType = ButtonSearchTypes.Credential.ToString(),
						//		TargetId = item.Id
						//	} ) )
						//},
						//Competencies
						new SearchResultButton()
						{
							CategoryLabel = item.AssessmentsCompetenciesCount + item.LearningOppsCompetenciesCount + item.RequiresCompetenciesCount == 1 ? "Competency" : "Competencies",
							CategoryType = ButtonCategoryTypes.Competency.ToString(),
							HandlerType = ButtonHandlerTypes.handler_GetRelatedItemsViaAJAX.ToString(),
							TotalItems = item.AssessmentsCompetenciesCount + item.LearningOppsCompetenciesCount + item.RequiresCompetenciesCount,
							RenderData = JObject.FromObject( new SearchResultButton.Helpers.AjaxDataForResult()
							{
								AjaxQueryName = "GetSearchResultCompetencies",
								TargetEntityType = ButtonTargetEntityTypes.Competency.ToString()
							} )
						},
						//Durations
						new SearchResultButton()
						{
							CategoryLabel = item.EstimatedTimeToEarn.Count() == 1 ? "Time Estimate" : "Time Estimates",
							CategoryType = ButtonCategoryTypes.EstimatedDuration.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderDetailPageLink.ToString(),
							TotalItems = item.EstimatedTimeToEarn.Count(),
							Items = item.EstimatedTimeToEarn.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( new SearchResultButton.Helpers.DetailPageLink()
							{
								TargetLabel = m.IsRange ? m.MinimumDuration.Print() + " - " + m.MaximumDuration.Print() : string.IsNullOrEmpty(m.ExactDuration.Print()) ? m.Conditions :  m.ExactDuration.Print(),
								TargetType = ButtonTargetEntityTypes.Credential.ToString(),
								TargetId = item.Id
							} ) )
						},
						//Costs
						new SearchResultButton()
						{
							CategoryLabel = item.NumberOfCostProfileItems == 1 ? "Estimated Cost" : "Estimated Costs",
							CategoryType = ButtonCategoryTypes.EstimatedCost.ToString(),
							HandlerType = ButtonHandlerTypes.handler_GetRelatedItemsViaAJAX.ToString(),
							TotalItems = item.NumberOfCostProfileItems,
							RenderData = JObject.FromObject( new SearchResultButton.Helpers.AjaxDataForResult()
							{
								AjaxQueryName = "GetSearchResultCosts",
								TargetEntityType = ButtonTargetEntityTypes.EstimatedCost.ToString()
							} )
						}
						}
					) );
				} catch ( Exception ex )
                {
					var msg = BaseFactory.FormatExceptions( ex );
					var jitem = JsonConvert.SerializeObject( item );
					LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".ConvertCredentialResults #{0} Credential: {1} (Id:{2}), Exception: {3}, for:\r\n{4)", cntr, item.Name, item.Id, msg, jitem ) );
                }
			}
			return output;
			/*
					new List<Models.Helpers.SearchTag>()
					{
						//Credential Quality Assurance
						new Models.Helpers.SearchTag()
						{
							CategoryName = "Quality Assurance",
							DisplayTemplate = "{#} Quality Assurance",
							Name = "organizationroles", //Using the "quality" enum breaks this filter since it tries to find the matching item in the checkbox list and it doesn't exist
							TotalItems = mergedQA.Count(),
							SearchQueryType = "merged", //Change this to "custom", or back to detail
							//Something like this...
							//QAOrgRolesResults is a list of 1 role and 1 org (org repeating for each relevant role)
							//e.g. [Accredited By] [Organization 1], [Approved By] [Organization 1], [Accredited By] [Organization 2], etc.
							Items = mergedQA.Take(10).ToList().ConvertAll( m => new Models.Helpers.SearchTagItem() {
								Display = "<b>" + m.Relationship + "</b>" + " by " + m.Agent, //[Accredited By] [Organization 1]
								QueryValues = new Dictionary<string, object>() {
									{ "Relationship", m.Relationship },
									{ "TextValue", m.Agent },
									{ "RelationshipId", m.RelationshipId },
                                     //  { "CodeId", m.AgentId },
                                    { "IsThirdPartyOrganization", m.IsThirdPartyOrganization },
									{ "AgentId", m.AgentId }, //AgentId?
									{ "TargetType", m.EntityType }, //Probably okay to hard code this for now
                                    { "AgentUrl", m.AgentUrl},
									{ "EntityStateId", m.EntityStateId }
									//{ "ConnectionTypeId", m.ConnectionId }, //Connection type
                                }
							} ).ToList()
							
							//Items = GetSearchTagItems_Filter( item.QARolesResults.Results, "{Name} by Quality Assurance Organization(s)", item.QARolesResults.CategoryId )
						},
                       //Connections
						new Models.Helpers.SearchTag()
						{
							CategoryName = "Connections",
							DisplayTemplate = "{#} Connection{s}",
							Name = TagTypes.CONNECTIONS.ToString().ToLower(),
                            //TotalItems = item.CredentialsList.Results.Count(),
                            TotalItems = mergedConnections.Count(),
							SearchQueryType = "link",
							//Items = GetSearchTagItems_Filter( item.ConnectionsList.Results, "{Name} Credential(s)", item.ConnectionsList.CategoryId )
							//Something like this...
							
							Items = mergedConnections.Take(10).ToList().ConvertAll(m => new Models.Helpers.SearchTagItem()
							{
								Display = "<b>" + m.Connection + "</b>" + " " + m.Credential, //[Is Preparation For] [Some Credential Name] 
								QueryValues = new Dictionary<string, object>()
								{
									{ "TargetId", m.CredentialId }, //AgentId?
									{ "TargetType", "credential" }, //Probably okay to hard code this for now
									{ "ConnectionTypeId", m.ConnectionId }, //Connection type
									{ "IsReference", "false" },
								}
							} ).ToList()
						},
						//financial assistance
						new Models.Helpers.SearchTag()
						{
							CategoryName = "FinancialAssistance",
							DisplayTemplate = "{#} Financial Assistance",
							Name = "financialAssistance", //The CSS on the search page will look for an icon associated with this
							TotalItems = item.FinancialAidCount, 
							SearchQueryType = "text",
							IsAjaxQuery = true,
							AjaxQueryName = "GetFinancialAssistance", //Query to call to get the related assessments (reference "Costs" below for usage)
							AjaxQueryValues = new Dictionary<string, object>() //Values to pass to the above query. Probably need to change what's in here to make it work.
							{
								{ "SearchType", "credential" },
								{ "RecordId", item.Id },
								{ "TargetEntityType", "financial" }
							}
						},
						//Related Assessments
						new Models.Helpers.SearchTag()
						{
							CategoryName = "Assessments",
							DisplayTemplate = "{#} Assessment{s}",
							Name = "assessments", //The CSS on the search page will look for an icon associated with this
							TotalItems = item.RequiredAssessmentsCount + item.RecommendedAssessmentsCount, //Replace this with the count
							SearchQueryType = "link",
							IsAjaxQuery = true,
							AjaxQueryName = "GetSearchResultAsmt", //Query to call to get the related assessments (reference "Costs" below for usage)
							AjaxQueryValues = new Dictionary<string, object>() //Values to pass to the above query. Probably need to change what's in here to make it work.
							{
								{ "SearchType", "credential" },
								{ "RecordId", item.Id },
								{ "TargetEntityType", "assessment" }
							}
						},
						//Related Learning Opportunities
						new Models.Helpers.SearchTag()
						{
							CategoryName = "LearningOpportunities",
							DisplayTemplate = "{#} Learning Opportunit{ies}",
							Name = "learningOpportunities", //The CSS on the search page will look for an icon associated with this
							TotalItems = item.RequiredLoppCount + item.RecommendedLoppCount, //Replace this with the count
							SearchQueryType = "link",
							IsAjaxQuery = true,
							AjaxQueryName = "GetSearchResultLopp", //Query to call to get the related learning opportunities 
							AjaxQueryValues = new Dictionary<string, object>() 
							{
								{ "SearchType", "credential" },
								{ "RecordId", item.Id },
								{ "TargetEntityType", "learningopportunity" }
							}
						},
						//Audience Level Type
						new Models.Helpers.SearchTag()
						{
							CategoryName = "Levels",
							DisplayTemplate = "{#} Audience Level{s}",
							Name = TagTypes.AUDIENCE_LEVEL.ToString().ToLower(),
							TotalItems = item.AudienceLevelsResults.Results.Count(),
							SearchQueryType = "code",
							Items = GetSearchTagItems_Filter( item.AudienceLevelsResults.Results, "{Name}", item.AudienceLevelsResults.CategoryId )
						},
						//Audience Type
						new Models.Helpers.SearchTag()
						{
							CategoryName = "Types",
							DisplayTemplate = "{#} Audience Type(s)",
							Name = TagTypes.AUDIENCE_TYPE.ToString().ToLower(),
							TotalItems = item.AudienceTypesResults.Results.Count(),
							SearchQueryType = "code",
							Items = GetSearchTagItems_Filter( item.AudienceTypesResults.Results, "{Name}", item.AudienceTypesResults.CategoryId )
						},
						//Occupations
						new Models.Helpers.SearchTag()
						{
							CategoryName = "Occupations",
							DisplayTemplate = "{#} Occupation{s}",
							Name = TagTypes.OCCUPATIONS.ToString().ToLower(),
							TotalItems = item.OccupationResults.Results.Count(),
                            //SearchQueryType = "framework",
                            SearchQueryType = "text",
							Items = item.OccupationResults.Results.Take(10).ToList().ConvertAll( m => new Models.Helpers.SearchTagItem() { Display = m.CodeTitle, QueryValues = new Dictionary<string, object>() { { "TextValue", m.CodeTitle } } } )
                            //Items = GetSearchTagItems_Filter( item.OccupationResults.Results, "{Name}", item.OccupationResults.CategoryId )
                        },
						//Industries
						new Models.Helpers.SearchTag()
						{
							CategoryName = "Industries",
							DisplayTemplate = "{#} Industr{ies}",
							Name = TagTypes.INDUSTRIES.ToString().ToLower(),
							TotalItems = item.IndustryResults.Results.Count(),
                            //SearchQueryType = "framework",
                            SearchQueryType = "text",
                            //Items = GetSearchTagItems_Filter( item.NaicsResults.Results, "{Name}", item.NaicsResults.CategoryId )
                            Items = item.IndustryResults.Results.Take(10).ToList().ConvertAll( m => new Models.Helpers.SearchTagItem() { Display = m.CodeTitle, QueryValues = new Dictionary<string, object>() { { "TextValue", m.CodeTitle } } } )
						},
						//Instructional Program Classfication
						new Models.Helpers.SearchTag()
						{
							CategoryName = "instructionalprogramtypes",
							CategoryLabel = "Instructional Program Type",
							DisplayTemplate = "{#} Instructional Program{s}",
							Name = "instructionalprogramtypes",
							TotalItems = item.InstructionalProgramClassification.Results.Count(),
							SearchQueryType = "text",
                           //Items = GetSearchTagItems_Filter( item.InstructionalProgramClassification.Results, "{Name}", item.InstructionalProgramClassification.CategoryId )
                            Items = item.InstructionalProgramClassification.Results.Take(10).ToList().ConvertAll( m => new Models.Helpers.SearchTagItem() { Display = m.CodeTitle, QueryValues = new Dictionary<string, object>() { { "TextValue", m.CodeTitle } } } )
						},

						//Asmnt Delivery Method Type
                        new Models.Helpers.SearchTag()
						{
							CategoryName = "AssessmentDeliveryTypes",
							CategoryLabel = "Assessment Delivery Types",
							DisplayTemplate = "{#} Assessment DeliveryType{s}",
							Name = TagTypes.ASMNT_DELIVER_METHODS.ToString().ToLower(),
							TotalItems = item.AssessmentDeliveryType.Results.Count(),
							SearchQueryType = "code",
							Items = GetSearchTagItems_Filter(item.AssessmentDeliveryType.Results, "{Name}", item.AssessmentDeliveryType.CategoryId)
						},

						//Learning Delivery Method Type
                        new Models.Helpers.SearchTag()
						{
							CategoryName = "LearningDeliveryTypes",
							CategoryLabel = "Learning Delivery Types",
							DisplayTemplate = "{#} Learning Delivery Type(s)",
							Name = TagTypes.DELIVER_METHODS.ToString().ToLower(),
							TotalItems = item.LearningDeliveryType.Results.Count(),
							SearchQueryType = "code",
							Items = GetSearchTagItems_Filter(item.LearningDeliveryType.Results, "{Name}", item.LearningDeliveryType.CategoryId)
						},
						//Subjects
						new Models.Helpers.SearchTag()
						{
							CategoryName = "Subjects",
							DisplayTemplate = "{#} Subject{s}",
							Name = TagTypes.SUBJECTS.ToString().ToLower(),
							TotalItems = subjects.Count(), //Returns a count of the de-duplicated items
							SearchQueryType = "text",
							Items = subjects.Take(10).ToList().ConvertAll( m => new Models.Helpers.SearchTagItem() { Display = m, QueryValues = new Dictionary<string, object>() { { "TextValue", m } } } )
						},
						//Competencies
						new Models.Helpers.SearchTag()
						{
							CategoryName = "Competencies",
							DisplayTemplate = "{#} Competenc{ies}",
							Name = TagTypes.COMPETENCIES.ToString().ToLower(),
							TotalItems = item.AssessmentsCompetenciesCount + item.LearningOppsCompetenciesCount,
							SearchQueryType = "text",
							IsAjaxQuery = true,
							AjaxQueryName = "GetSearchResultCompetencies",
							AjaxQueryValues = new Dictionary<string, object>()
							{
								{ "SearchType", "credential" },
								{ "RecordId", item.Id },
								{ "TargetEntityType", "competencies" }
							}
						},
						//Durations
						new Models.Helpers.SearchTag()
						{
							CategoryName = "Time Estimates",
							DisplayTemplate = " Time Estimate{s}",
							Name = TagTypes.TIME.ToString().ToLower(),
							TotalItems = item.EstimatedTimeToEarn.Count(), //# of duration profiles
							SearchQueryType = "detail", //Not sure how this could be any kind of search query
							Items = item.EstimatedTimeToEarn.Take(10).ToList().ConvertAll(m => new Models.Helpers.SearchTagItem()
							{
								Display = m.IsRange ? m.MinimumDuration.Print() + " - " + m.MaximumDuration.Print() : string.IsNullOrEmpty(m.ExactDuration.Print()) ? m.Conditions :  m.ExactDuration.Print(),

								QueryValues = new Dictionary<string, object>()
								{
									{ "ExactDuration", m.ExactDuration },
									{ "MinimumDuration", m.MinimumDuration },
									{ "MaximumDuration", m.MaximumDuration },
									{ "Conditions", m.Conditions}
								}
							} ).ToList()
						},
						//Costs
						new Models.Helpers.SearchTag()
						{
							CategoryName = "Costs",
							DisplayTemplate = " Cost{s}",
							Name = TagTypes.COST.ToString().ToLower(),
							TotalItems = item.NumberOfCostProfileItems > 0 ? item.NumberOfCostProfileItems : item.CostProfileCount, //# of cost profiles items
							SearchQueryType = "text",
							IsAjaxQuery = true,
							AjaxQueryName = "GetSearchResultCosts",
							AjaxQueryValues = new Dictionary<string, object>()
							{
								{ "SearchType", "credential" },
								{ "RecordId", item.Id },
								{ "TargetEntityType", "cost" }
							}
						},
						//Partial Loader
						new Models.Helpers.SearchTag()
						{
							CategoryName = "Test Partial Category", //Change this
							DisplayTemplate = "Test Partial Template", //Change this
							Name = "TestPartialName", //Change this (no spaces)
							TotalItems = 0, //Change this
							SearchQueryType = "partial", //Don't change this
							IsAjaxQuery = true, //Don't change this
							AjaxQueryName = "TestPanel", //This should be the name of the partial that lives in /Views/Search/ResultPanels, sans .cshtml
							AjaxQueryValues = new Dictionary<string, object>() //Make sure your partial uses an @model of Dictionary<string, object>
							{
								{ "ThisResultID", item.Id },
								{ "ThisResultCTID", item.CTID },
								{ "Parameter1ToSendToPartial", "valueToSend" },
								{ "Parameter2", 99 },
								{ "Parameter3", true },
								{ "Parameter4", "another value" }
							}
						}
					},
					*/
		}
		//

		public static string GetCredentialIcon( string credentiaType )
		{
			//defaults
			credentiaType = credentiaType.Replace( "ceterms:", "" );
			string url = string.Format( "images/icons/flat_{0}_cropped.png", credentiaType);
			//exceptions
			if (credentiaType.IndexOf("badge") > -1)
				url = "~/images/icons/flat_badge_cropped.png";
			else if ( credentiaType.IndexOf( "certificate" ) > -1 )
				url = "~/images/icons/flat_certificate_cropped.png";
			else if ( credentiaType.IndexOf( "degree" ) > -1 
				|| credentiaType.IndexOf( "master" ) > -1
				|| credentiaType.IndexOf( "doctorate" ) > -1 )
				url = "~/images/icons/flat_degree_cropped.png";
			else if ( credentiaType.IndexOf( "diploma" ) > -1
				|| credentiaType.IndexOf( "generaleducat" ) > -1 )
				url = "~/images/icons/flat_diploma_cropped.png";
			else if ( credentiaType.IndexOf( "apprentice" ) > -1 )
				url = "~/images/icons/flat_apprentice_cropped.png";

			//need to use this as also called from finderAPI
			string oldCredentialFinderSite = UtilityManager.GetAppKeyValue( "oldCredentialFinderSite" );
			url= oldCredentialFinderSite + url.Replace("~/","");
			return url;
		}

		//

		public List<string> Deduplicate(List<string> items)
		{
			var added = new List<string>();
			var result = new List<string>();
			if ( items == null || items.Count == 0 )
				return result;

			foreach ( var item in items )
			{
				var text = item.ToLower().Trim();
				if ( !added.Contains( text ) )
				{
					added.Add( text );
					result.Add( item.Trim() );
				}
			}
			return result;
		}
		//List<> 
		public List<string> Deduplicate( List<ME.IndexSubject> items )
		{
			var added = new List<string>();
			var result = new List<string>();
			if ( items == null || items.Count == 0 )
				return result;

			foreach ( var item in items )
			{
				var text = item.Name.ToLower().Trim();
				if ( !added.Contains( text ) )
				{
					added.Add( text );
					result.Add( item.Name.Trim() );
				}
			}
			return result;
		}
		public List<Models.Helpers.SearchTagItem> GetSearchTagItems_Filter(List<CodeItem> items, string displayTemplate, int categoryID)
		{
			if ( "10 11 23".IndexOf( categoryID.ToString() ) > -1 )
			{
				return items.ConvertAll( m => new Models.Helpers.SearchTagItem()
				{
					Display = Models.Helpers.SearchTagHelper.Count( displayTemplate.Replace( "{Name}", m.Name ), 1 ),
					QueryValues = new Dictionary<string, object>()
				{
					{ "CategoryId", categoryID },
					{ "Code", m.Code },
					{ "SchemaName", m.SchemaName },
					{ "Name", m.Name },
					{ "CodeId", m.Id }
				}
				} );
			}
			else
			{
				return items.ConvertAll( m => new Models.Helpers.SearchTagItem()
				{
					Display = Models.Helpers.SearchTagHelper.Count( displayTemplate.Replace( "{Name}", m.Name ), 1 ),
					QueryValues = new Dictionary<string, object>()
				{
					{ "CategoryId", categoryID },
					{ "CodeId", m.Id },
					{ "SchemaName", m.SchemaName }
				}
				} );
			}

		}
		//

		public List<Models.Helpers.SearchTagItem> GetSearchTagItems_Filter(List<EnumeratedItem> items, string displayTemplate, int categoryID)
		{
			return items.ConvertAll( m => new Models.Helpers.SearchTagItem()
			{
				Display = Models.Helpers.SearchTagHelper.Count( displayTemplate.Replace( "{Name}", m.Name ), 1 ),
				QueryValues = new Dictionary<string, object>()
				{
					{ "CategoryId", categoryID },
					{ "CodeId", m.Id },
					{ "SchemaName", m.SchemaName }
				}
			} );
		}
		//

		public SearchResultButton.Helpers.FilterItem GetFilterItem( CodeItem sourceItem, int categoryID )
		{
			return new SearchResultButton.Helpers.FilterItem()
			{
				CategoryId = categoryID,
				ItemCodeId = sourceItem.Id,
				SchemaURI = sourceItem.SchemaName,
				ItemCode = sourceItem.Code,
				ItemLabel = sourceItem.Name,
				ItemCodeTitle = sourceItem.CodeTitle
			};
		}
		//

		public SearchResultButton.Helpers.FilterItem GetFilterItem( EnumeratedItem sourceItem, int categoryID )
		{
			return new SearchResultButton.Helpers.FilterItem()
			{
				CategoryId = categoryID,
				ItemCodeId = sourceItem.Id,
				SchemaURI = sourceItem.SchemaName,
				ItemLabel = sourceItem.Name
			};
		}
		//





		/// <summary>
		/// Search for requested tag set
		/// </summary>
		/// <param name="searchType"></param>
		/// <param name="entityType"></param>
		/// <param name="recordID"></param>
		/// <param name="maxRecords"></param>
		/// <returns></returns>
		public static TagSet GetTagSet(string searchType, TagTypes entityType, int recordID, int maxRecords = 10)
		{
			var result = new TagSet();
			switch ( entityType ) //Match "Schema" in ConvertCredentialResults() method above
			{
				case TagTypes.ASSESSMENT:
				{
					var data = SearchServices.EntityAssesmentsList( searchType, recordID, maxRecords );
					result = new TagSet()
					{
						Schema = TagTypes.ASSESSMENT.ToString().ToLower(),
						Label = "Assessments",
						Method = "link",
						EntityTagItems = data.ConvertAll( m => new EntityTagItem() { TargetEntityBaseId = m.Id, TargetEntityType="Assessment", TargetEntityTypeId=3, TargetEntityName = m.Name, TargetEntitySubjectWebpage = m.SubjectWebpage, IsReference = string.IsNullOrWhiteSpace(m.CTID), TargetFriendlyName=m.FriendlyName } ).Take( 10 ).ToList()
					};
					break;
				}
				case TagTypes.LEARNINGOPPORTUNITY:
				{
					var data = SearchServices.EntityLoppsList( searchType, recordID, maxRecords );
					result = new TagSet()
					{
						Schema = TagTypes.LEARNINGOPPORTUNITY.ToString().ToLower(),
						Label = "LearningOpportunities",
						Method = "link",
						EntityTagItems = data.ConvertAll( m => new EntityTagItem() { TargetEntityBaseId = m.Id, TargetEntityType = "LearningOpportunity", TargetEntityTypeId = 7, TargetEntityName = m.Name, TargetEntitySubjectWebpage = m.SubjectWebpage, IsReference = string.IsNullOrWhiteSpace( m.CTID ), TargetFriendlyName = m.FriendlyName } ).Take( 10 ).ToList()
					};
					break;
				}
				case TagTypes.OWNS_CREDENTIAL:
				{
					//will need versions for owns, and offers, also QA on
					//20-12-15 - is this correct, using EntityLoppsList, or never implemented?
					var data = SearchServices.EntityLoppsList( searchType, recordID, maxRecords );
					result = new TagSet()
					{
						Schema = TagTypes.OWNS_CREDENTIAL.ToString().ToLower(),
						Label = "Credentials",
						Method = "link",
						EntityTagItems = data.ConvertAll( m => new EntityTagItem() { TargetEntityBaseId = m.Id, TargetEntityType = "Credential", TargetEntityTypeId = 7, TargetEntityName = m.Name, TargetEntitySubjectWebpage = m.SubjectWebpage, IsReference = string.IsNullOrWhiteSpace( m.CTID ), TargetFriendlyName = m.FriendlyName } ).Take( 10 ).ToList()
					};
					break;
				}
				case TagTypes.COMPETENCIES:
				{
					var data = SearchServices.EntityCompetenciesList( searchType, recordID, maxRecords );
					result = new TagSet()
					{
						Schema = TagTypes.COMPETENCIES.ToString().ToLower(),
						Label = "Competencies",
						Method = "direct",
						Items = data.ConvertAll( m => new TagItem() { CodeId = m.Id, Label = m.TargetNodeName, Description = m.Description } ).Take( 10 ).ToList()
					};
					break;
				}
				case TagTypes.REFERENCED_BY_ASSESSMENT:
				{
					var data = SearchServices.EntityCompetenciesList( searchType, recordID, maxRecords );
					result = new TagSet()
					{
						Schema = TagTypes.COMPETENCIES.ToString().ToLower(),
						Label = "Assessments",
						Method = "direct",
						Items = data.ConvertAll( m => new TagItem() { CodeId = m.Id, Label = m.TargetNodeName, Description = m.Description } ).Take( 10 ).ToList()
					};
					break;
				}
				case TagTypes.COST:
				{
					//future
					var data = SearchServices.EntityCostsList( searchType, recordID, maxRecords );
					result = new TagSet()
					{
						Schema = TagTypes.COST.ToString().ToLower(),
						Label = "Costs",
						Method = "direct",
						CostItems = data.ConvertAll( c => new CostTagItem()
						{
							CodeId = c.CostProfileId,
							Price = c.Price,
							CostType = c.Price > 0 ? c.CostTypeName : c.CostDescription,
							CurrencySymbol = c.CurrencySymbol == "&#36;" ? "$" : c.CurrencySymbol,
							SourceEntity = c.ParentEntityType.ToLower().IndexOf( searchType.ToLower()) == 0 ? "direct" : c.ParentEntityType
						} ),	//	doing sort in search proc instead of here .OrderBy( s => s.Price).ToList(),
						//Items are used for translation in new Search API
						Items = data.ConvertAll( 
							m => new TagItem() 
							{ 
								CodeId = m.Id, 
								Label = m.CostTypeName + ( m.Price > 0 ? " - " + ( m.CurrencySymbol == "&#36;" ? "$" : m.CurrencySymbol ) + Regex.Replace( m.Price.ToString(), @"\B(?=(\d{3})+(?!\d))", "," ) : "" ), 
								Description = "" 
							} 
						)
					};
					break;
				}
				case TagTypes.FINANCIAL:
				{
					var data = SearchServices.EntityFinancialAssistanceList( searchType, recordID, maxRecords );
					result = new TagSet()
					{
						Schema = TagTypes.FINANCIAL.ToString().ToLower(),
						Label = "Financial Assistance",
						Method = "direct",
						FinancialItems = data.ConvertAll( c => new FinancialTagItem()
						{
							CodeId = c.Id,
							Label = c.Name,
							AssistanceTypes = c.FinancialAssistanceType.Items.Any() ? "Types: " + string.Join( ",", c.FinancialAssistanceType.Items.ToArray().Select( m => m.Name ) ) : "",
							Description = string.Join(";",c.FinancialAssistanceValueSummary.ToArray())
						} ),
						Items = data.ConvertAll( m => new TagItem() { CodeId = m.Id, Label = m.Name, Description = m.Description } )
					};
					break;
				}
				case TagTypes.QAPERFORMED:
				{
					var data = SearchServices.QAPerformedList( recordID, maxRecords );
					result = new TagSet()
					{
						Schema = TagTypes.QAPERFORMED.ToString().ToLower(),
						Label = "Quality Assurance Performed",
						Method = "qaperformed",
						QAItems = data.ConvertAll( q => new QAPerformedTagItem()
						{
							TargetEntityTypeId = q.TargetEntityTypeId,
							TargetEntityBaseId = q.TargetEntityBaseId,
							AssertionTypeId = q.AssertionTypeId,
							TargetEntityName = q.TargetEntityName,
							TargetEntityType = q.TargetEntityType,
							TargetEntitySubjectWebpage = q.TargetEntitySubjectWebpage,
							AgentToTargetRelationship = q.AgentToSourceRelationship,
							IsReference = string.IsNullOrEmpty( q.TargetCTID )
						} )
					};
					break;
				}//
				case TagTypes.TRANSFERINTERMEDIARY:    //org transfer value profiles
					{
						if ( searchType == "organization" )
						{
							var data = TransferIntermediaryServices.GetOwnedByOrg( recordID, maxRecords );
							result = new TagSet()
							{
								Schema = TagTypes.TRANSFERVALUE.ToString().ToLower(),
								Label = "Transfer Intermediaries",
								Method = "link",
								EntityTagItems = data.ConvertAll( m => new EntityTagItem() { TargetEntityBaseId = m.Id, TargetEntityType = "TransferIntermediary", TargetEntityTypeId = CodesManager.ENTITY_TYPE_TRANSFER_INTERMEDIARY, TargetEntityName = m.Name, TargetEntitySubjectWebpage = m.SubjectWebpage, IsReference = string.IsNullOrWhiteSpace( m.CTID ), TargetFriendlyName = m.FriendlyName } ).Take( 10 ).ToList()
							};
						}
						else
						{
							var data = TransferIntermediaryServices.GetEntityHasTransferIntermediary( searchType, recordID, maxRecords );
							result = new TagSet()
							{
								Schema = TagTypes.TRANSFERINTERMEDIARY.ToString().ToLower(),
								Label = "Transfer Intermediaries",
								Method = "link",
								EntityTagItems = data.ConvertAll( m => new EntityTagItem() { TargetEntityBaseId = m.Id, TargetEntityType = "TransferIntermediary", TargetEntityTypeId = CodesManager.ENTITY_TYPE_TRANSFER_INTERMEDIARY, TargetEntityName = m.Name, TargetEntitySubjectWebpage = m.SubjectWebpage, IsReference = string.IsNullOrWhiteSpace( m.CTID ), TargetFriendlyName = m.FriendlyName } ).Take( 10 ).ToList()
							};
						}

						break;
					}//
				case TagTypes.TRANSFERVALUE:	//org transfer value profiles
				{
					if ( searchType == "organization")
					{
						var data = TransferValueServices.GetTVPOwnedByOrg( recordID, maxRecords );
						result = new TagSet()
						{
							Schema = TagTypes.TRANSFERVALUE.ToString().ToLower(),
							Label = "TransferValues",
							Method = "link",
							EntityTagItems = data.ConvertAll( m => new EntityTagItem() { TargetEntityBaseId = m.Id, TargetEntityType = "TransferValueProfile", TargetEntityTypeId = 26, TargetEntityName = m.Name, TargetEntitySubjectWebpage = m.SubjectWebpage, IsReference = string.IsNullOrWhiteSpace( m.CTID ), TargetFriendlyName = m.FriendlyName } ).Take( 10 ).ToList()
						};
					} else
					{
						var data = TransferValueServices.GetEntityHasTVP( searchType, recordID, maxRecords );
						result = new TagSet()
						{
							Schema = TagTypes.TRANSFERVALUE.ToString().ToLower(),
							Label = "TransferValues",
							Method = "link",
							EntityTagItems = data.ConvertAll( m => new EntityTagItem() { TargetEntityBaseId = m.Id, TargetEntityType = "TransferValueProfile", TargetEntityTypeId = 26, TargetEntityName = m.Name, TargetEntitySubjectWebpage = m.SubjectWebpage, IsReference = string.IsNullOrWhiteSpace( m.CTID ), TargetFriendlyName = m.FriendlyName } ).Take( 10 ).ToList()
						};
					}
					
					break;
				}
				case TagTypes.HASTRANSFERVALUE: //credential etc, has transfer value profiles
				{
					//get any TVP for 
					var data = TransferValueServices.GetEntityHasTVP( searchType, recordID, maxRecords );
					result = new TagSet()
					{
						Schema = TagTypes.HASTRANSFERVALUE.ToString().ToLower(),
						Label = "Has Transfer Values",
						Method = "link",
						EntityTagItems = data.ConvertAll( m => new EntityTagItem() { TargetEntityBaseId = m.Id, TargetEntityType = "TransferValueProfile", TargetEntityTypeId = 26, TargetEntityName = m.Name, TargetEntitySubjectWebpage = m.SubjectWebpage, IsReference = string.IsNullOrWhiteSpace( m.CTID ), TargetFriendlyName = m.FriendlyName } ).Take( 10 ).ToList()
					};
					break;
				}
				case TagTypes.TVPHASCREDENTIAL: //credentials for a transfer value profile
				{
					//get any TVP for 
					//var data = TransferValueServices.GetEntityHasTVP( searchType, recordID, maxRecords );
					var data = TransferValueProfileManager.GetAllCredentials( CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, recordID );

					result = new TagSet()
					{
						Schema = TagTypes.HASTRANSFERVALUE.ToString().ToLower(),
						Label = "Credentials",
						Method = "link",
						EntityTagItems = data.ConvertAll( m => new EntityTagItem() { TargetEntityBaseId = m.Id, TargetEntityType = "Credential", TargetEntityTypeId = 1, TargetEntityName = m.Name, TargetEntitySubjectWebpage = m.SubjectWebpage, IsReference = string.IsNullOrWhiteSpace( m.CTID ), TargetFriendlyName = m.FriendlyName } ).Take( 10 ).ToList()
					};
					break;
				}
				case TagTypes.TVPHASASSESSMENT: // for a transfer value profile
				{
					//get any TVP for 
					var data = TransferValueProfileManager.GetAllAssessments( CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, recordID );

					result = new TagSet()
					{
						Schema = TagTypes.HASTRANSFERVALUE.ToString().ToLower(),
						Label = "Assessments",
						Method = "link",
						EntityTagItems = data.ConvertAll( m => new EntityTagItem() { TargetEntityBaseId = m.Id, TargetEntityType = "Assessment", TargetEntityTypeId = 3, TargetEntityName = m.Name, TargetEntitySubjectWebpage = m.SubjectWebpage, IsReference = string.IsNullOrWhiteSpace( m.CTID ), TargetFriendlyName = m.FriendlyName } ).Take( 10 ).ToList()
					};
					break;
				}
				case TagTypes.TVPHASLOPP: // for a transfer value profile
				{
					//get any TVP for 
					var data = TransferValueProfileManager.GetAllLearningOpportunities( CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, recordID );

					result = new TagSet()
					{
						Schema = TagTypes.HASTRANSFERVALUE.ToString().ToLower(),
						Label = "Learning Opportunities",
						Method = "link",
						EntityTagItems = data.ConvertAll( m => new EntityTagItem() { TargetEntityBaseId = m.Id, TargetEntityType = "LearningOpportunity", TargetEntityTypeId = 7, TargetEntityName = m.Name, TargetEntitySubjectWebpage = m.SubjectWebpage, IsReference = string.IsNullOrWhiteSpace( m.CTID ), TargetFriendlyName = m.FriendlyName } ).Take( 10 ).ToList()
					};
					break;
				}
				case TagTypes.PATHWAY:
				{
					var data = PathwayServices.GetPathwaysOwnedByOrg( recordID, maxRecords );
					result = new TagSet()
					{
						Schema = TagTypes.PATHWAY.ToString().ToLower(),
						Label = "Pathway",
						Method = "link",
						EntityTagItems = data.ConvertAll( m => new EntityTagItem() { TargetEntityBaseId = m.Id, TargetEntityType = "Pathway", TargetEntityTypeId = 8, TargetEntityName = m.Name, TargetEntitySubjectWebpage = m.SubjectWebpage, IsReference = string.IsNullOrWhiteSpace( m.CTID ), TargetFriendlyName = m.FriendlyName } ).Take( 10 ).ToList()
					};
					break;
				}
				case TagTypes.PATHWAYSET:
				{
					var data = PathwayServices.GetPathwaySetsOwnedByOrg( recordID, maxRecords );
					result = new TagSet()
					{
						Schema = TagTypes.PATHWAYSET.ToString().ToLower(),
						Label = "Pathway Set",
						Method = "link",
						EntityTagItems = data.ConvertAll( m => new EntityTagItem() { TargetEntityBaseId = m.Id, TargetEntityType = "PathwaySet", TargetEntityTypeId = 23, TargetEntityName = m.Name, TargetEntitySubjectWebpage = m.SubjectWebpage, IsReference = string.IsNullOrWhiteSpace( m.CTID ), TargetFriendlyName = m.FriendlyName } ).Take( 10 ).ToList()
					};
					break;
				}
				case TagTypes.DATASETPROFILE_CREDENTIAL:
				{
					//21-05-26 - get all credentials for an org's dataset profiles
					var data = DataSetProfileServices.GetDSPCredentialsForOrg( recordID, maxRecords );
					result = new TagSet()
					{
						Schema = TagTypes.DATASETPROFILE_CREDENTIAL.ToString().ToLower(),
						Label = "Credentials",
						Method = "link",
						EntityTagItems = data.ConvertAll( m => new EntityTagItem() { TargetEntityBaseId = m.Id, TargetEntityType = "Credential", TargetEntityTypeId = 1, TargetEntityName = m.Name, TargetEntitySubjectWebpage = m.SubjectWebpage, IsReference = string.IsNullOrWhiteSpace( m.CTID ), TargetFriendlyName = m.FriendlyName } ).Take( 10 ).ToList()
					};
					break;
				}
				default:
					break;
			}
			return result;
		}
		//

		/*
		private List<TagItem> ConvertCodeItemsToTagItems( List<CodeItem> input )
		{
			var result = new List<TagItem>();
			foreach ( var item in input )
			{
				if ( result.FirstOrDefault( m => m.CodeId == item.Id ) == null ) //Prevent duplicates
				{
					result.Add( new TagItem() { CodeId = item.Id, Schema = item.SchemaName, Label = item.Name } );
				}
			}
			result = result.OrderBy( m => m.Label ).ToList();
			return result;
		}
		//
		*/
		public MainSearchResults ConvertOrganizationResults(List<OrganizationSummary> results, int totalResults, string searchType)
		 {
			var output = new MainSearchResults() { TotalResults = totalResults, SearchType = searchType };
			foreach ( var item in results )
			{
				//probably need to do this for all?
				List<CodeItem> ownsOffers = new List<CodeItem>();
				if ( item.OwnedByResults.Results.Count() > item.OfferedByResults.Results.Count() )
				{
					ownsOffers = item.OwnedByResults.Results;
				} else
				{
					ownsOffers = item.OfferedByResults.Results;
				}
				var totalOwnsOffers = item.OwnedByResults.Results.Count() > item.OfferedByResults.Results.Count() ? item.OwnedByResults.Results.Count() : item.OfferedByResults.Results.Count();
				var statusLabel = "";
				if ( UtilityManager.GetAppKeyValue( "showingNonActiveStatusLabel", false ) )
				{
					if ( !string.IsNullOrWhiteSpace( item.LifeCycleStatus ) && item.LifeCycleStatus != "Active" )
						statusLabel = " [** " + item.LifeCycleStatus.ToUpper() + " **]";
				}
				//21-11-23 mp - stop using single Address, only Addresses is populated
				//set first one to a default address
				Address defaultAddress = new Address();
				if ( item.Addresses != null && item.Addresses.Any() ) 
					defaultAddress = item.Addresses[ 0 ];
				output.Results.Add( Result( item.Name + statusLabel, item.FriendlyName, item.Description, item.Id,
					new Dictionary<string, object>()
					{
						{ "Name", item.Name + statusLabel},
						{ "Description", item.Description },
						{ "OwnerId", 0 },
						//{ "CanEditRecord", item.CanEditRecord },
						//{ "AvailableAt", new { Type = "locations", Data = ConvertAddresses( new List<Address>() { item.Address } ) } },
						{ "AvailableAt", new { Type = "locations", Data = ConvertAddresses( item.Addresses ) } },
						{ "Locations", ConvertAddresses( item.Addresses ) }, //old item.Auto_Address
							{ "LifeCycleStatus", item.LifeCycleStatus },
						{ "SearchType", searchType },
						{ "RecordId", item.Id },
						{ "ResultNumber", item.ResultNumber },
						{ "ctid", item.CTID },
						{ "UrlTitle", item.FriendlyName },
						{ "Logo", item.Image },
						{ "ResultImageUrl", item.Image ?? "" },
						{ "Location", defaultAddress.AddressCountry ?? "" + ( string.IsNullOrWhiteSpace( defaultAddress.AddressCountry ) ? "" : " - " ) + defaultAddress.AddressLocality + ( string.IsNullOrWhiteSpace( defaultAddress.AddressLocality ) ? "" : ", " ) + defaultAddress.AddressRegion },
						 { "Created", item.Created.ToShortDateString() },
						{ "LastUpdated", item.LastUpdated.ToShortDateString() },
						{ "Coordinates", new { Type = "coordinates", Data = new { Latitude = defaultAddress.Latitude, Longitude = defaultAddress.Longitude } } },
						{ "IsQA", item.ISQAOrganization ? "true" : "false" },
						{ "T_LastUpdated", item.LastUpdated.ToString("yyyy-MM-dd HH:mm:ss") },
						{ "T_StateId", item.EntityStateId },
						{ "T_BroadType", "Organization" },
						{ "T_CTDLType", item.ISQAOrganization ? "ceterms:QACredentialOrganization" : item.IsACredentialingOrg ? "ceterms:CredentialOrganization" : "ceterms:Organization" }, //Hack
						{ "T_CTDLTypeLabel", item.ISQAOrganization ? "QA Credential Organization" : item.IsACredentialingOrg ? "Credential Organization" : "Organization" }, //Also a hack
						{ "T_Locations", ConvertAddressesToJArray(item.Addresses) }
					},
					null,
					new List<Models.Helpers.SearchTag>(),//obsolete					
					new List<SearchResultButton>()
					{
						//Quality Assurance Received
						new SearchResultButton()
						{
							CategoryLabel = "Quality Assurance",
							CategoryType = ButtonCategoryTypes.QualityAssuranceReceived.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderQualityAssurance.ToString(),
							TotalItems = item.QualityAssurance.Results.Count(),
							Items = item.QualityAssurance.Results.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( m ) )
						},
						//Quality Assurance Performed
						new SearchResultButton()
						{
							CategoryLabel = item.QualityAssuranceCombinedTotal == 1 ? "Quality Assurance Performance Identified" : "Quality Assurance Performances Identified",
							CategoryType = ButtonCategoryTypes.QualityAssurancePerformed.ToString(),
							HandlerType = ButtonHandlerTypes.handler_GetRelatedItemsViaAJAX.ToString(),
							TotalItems = item.QualityAssuranceCombinedTotal,
							RenderData = JObject.FromObject(new SearchResultButton.Helpers.AjaxDataForResult()
							{
								AjaxQueryName = "GetSearchResultPerformed",
								TargetEntityType = ButtonTargetEntityTypes.QualityAssurancePerformed.ToString()
							} )
						},
						//TVP owned
						new SearchResultButton()
						{
							CategoryLabel = "Transfer Value Profiles",
							CategoryType = ButtonCategoryTypes.TransferValue.ToString(),
							HandlerType = ButtonHandlerTypes.handler_GetRelatedItemsViaAJAX.ToString(),
							TotalItems = item.TotalTransferValueProfiles,
							RenderData = JObject.FromObject(new SearchResultButton.Helpers.AjaxDataForResult()
							{
								AjaxQueryName = "GetSearchResultTVP",
								TargetEntityType = ButtonTargetEntityTypes.TransferValue.ToString()
							} )
						},
						//Pathway owned
						new SearchResultButton()
						{
							CategoryLabel = "Pathways",
							CategoryType = ButtonCategoryTypes.Pathway.ToString(),
							HandlerType = ButtonHandlerTypes.handler_GetRelatedItemsViaAJAX.ToString(),
							TotalItems = item.TotalPathways,
							RenderData = JObject.FromObject(new SearchResultButton.Helpers.AjaxDataForResult()
							{
								AjaxQueryName = "GetSearchResultPathway",
								TargetEntityType = ButtonTargetEntityTypes.Pathway.ToString()
							} )
						},
						new SearchResultButton()
						{
							CategoryLabel = "Pathway Sets",
							CategoryType = ButtonCategoryTypes.PathwaySet.ToString(),
							HandlerType = ButtonHandlerTypes.handler_GetRelatedItemsViaAJAX.ToString(),
							TotalItems = item.TotalPathwaySets,
							RenderData = JObject.FromObject(new SearchResultButton.Helpers.AjaxDataForResult()
							{
								AjaxQueryName = "GetSearchResultPathwaySet",
								TargetEntityType = ButtonTargetEntityTypes.PathwaySet.ToString()
							} )
						},
						//dataset profiles owned
						//new SearchResultButton()
						//{
						//	CategoryLabel = "Outcome Data",
						//	CategoryType = ButtonCategoryTypes.DataSetProfile.ToString(),
						//	HandlerType = ButtonHandlerTypes.handler_GetRelatedItemsViaAJAX.ToString(),
						//	TotalItems = item.DataSetProfileCount,
						//	RenderData = JObject.FromObject(new SearchResultButton.Helpers.AjaxDataForResult()
						//	{
						//		//we don't distinguish what is in the datasetprofiles, so what to show?
						//		AjaxQueryName = "GetSearchResultDataSetProfilesCredentials",
						//		TargetEntityType = ButtonTargetEntityTypes.DataSetProfile.ToString()
						//	} )
						//},
						//new SearchResultButton()
						//{
						//	CategoryLabel = item.TotalDataSetProfiles == 1 ? "Outcome Data" : "Outcome Data",
						//	CategoryType = ButtonCategoryTypes.DataSetProfile.ToString(),
						//	HandlerType = ButtonHandlerTypes.handler_RenderDetailPageLink.ToString(),
						//	TotalItems = item.TotalDataSetProfiles,
						//	Items = new List<string>() { item.AggregateDataProfileSummary }.ToList().ConvertAll( m => JObject.FromObject( new SearchResultButton.Helpers.DetailPageLink()
						//	{
						//		ConnectionLabel = "Outcome Data",
						//		TargetLabel = m,
						//		TargetType = ButtonSearchTypes.Credential.ToString(),
						//		TargetId = item.Id
						//	} ) )
						//},
						//Organization Type
						new SearchResultButton()
						{
							CategoryLabel = item.AgentType.Items.Count() == 1 ? "Organization Type" : "Organization Types",
							CategoryType = ButtonCategoryTypes.OrganizationType.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderCheckboxFilter.ToString(),
							TotalItems = item.AgentType.Items.Count(),
							Items = item.AgentType.Items.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( GetFilterItem( m, item.AgentType.Id ) ) )
						},
						//Organization Sector Type
						new SearchResultButton()
						{
							CategoryLabel = item.AgentSectorType.Items.Count() == 1 ? "Organization Sector Type" : "Organization Sector Types",
							CategoryType = ButtonCategoryTypes.SectorType.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderCheckboxFilter.ToString(),
							TotalItems = item.AgentSectorType.Items.Count(),
							Items = item.AgentSectorType.Items.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( GetFilterItem( m, item.AgentSectorType.Id ) ) )
						},
						//Organization Service Type
						new SearchResultButton()
						{
							CategoryLabel = item.ServiceType.Items.Count() == 1 ? "Organization Service Type" : "Organization Service Types",
							CategoryType = ButtonCategoryTypes.ServiceType.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderCheckboxFilter.ToString(),
							TotalItems = item.ServiceType.Items.Count(),
							Items = item.ServiceType.Items.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( GetFilterItem( m, item.ServiceType.Id ) ) )
						},
						//Owns Credentials
						new SearchResultButton()
						{
							//the new UI places the total on the left, so skip here
							//CategoryLabel = "Owns/Offers " + ( totalOwnsOffers == 1 ? " 1 Credential" : totalOwnsOffers + " Credentials" ),
							CategoryLabel = "Owns/Offers " + ( totalOwnsOffers == 1 ? " Credential" : " Credentials" ),
							CategoryType = ButtonCategoryTypes.OrganizationOwns.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderDetailPageLink.ToString(),
							TotalItems = totalOwnsOffers,
							Items = ownsOffers.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( new SearchResultButton.Helpers.DetailPageLink()
							{
								TargetLabel = m.Title,
								TargetId = m.Id,
								TargetType = ButtonTargetEntityTypes.Credential.ToString()
							} ) )
						},
						//Offers Credentials
						//new SearchResultButton()
						//{
						//	CategoryLabel = "Offers " + ( item.OfferedByResults.Results.Count() == 1 ? " 1 Credential" : item.OfferedByResults.Results.Count() + " Credentials" ),
						//	CategoryType = ButtonCategoryTypes.OrganizationOffers.ToString(),
						//	HandlerType = ButtonHandlerTypes.handler_RenderDetailPageLink.ToString(),
						//	TotalItems = item.OfferedByResults.Results.Count(),
						//	Items = item.OfferedByResults.Results.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( new SearchResultButton.Helpers.DetailPageLink()
						//	{
						//		TargetLabel = m.Title,
						//		TargetId = m.Id,
						//		TargetType = ButtonTargetEntityTypes.Credential.ToString()
						//	} ) )
						//},
						//Owns Assessments
						new SearchResultButton()
						{
							//CategoryLabel = "Owns/Offers " + ( item.AsmtsOwnedByResults.Results.Count() == 1 ? " 1 Assessment" : item.AsmtsOwnedByResults.Results.Count() + " Assessments" ),
							CategoryLabel = "Owns/Offers " + ( item.AsmtsOwnedByResults.Results.Count() == 1 ? " Assessment" : " Assessments" ),
							CategoryType = ButtonCategoryTypes.OrganizationOwns.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderDetailPageLink.ToString(),
							TotalItems = item.AsmtsOwnedByResults.Results.Count(),
							Items = item.AsmtsOwnedByResults.Results.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( new SearchResultButton.Helpers.DetailPageLink()
							{
								TargetLabel = m.Title,
								TargetId = m.Id,
								TargetType = ButtonTargetEntityTypes.Assessment.ToString()
							} ) )
						},
						//Owns Learning Opportunities
						new SearchResultButton()
						{
							//CategoryLabel = "Owns/Offers " + ( item.LoppsOwnedByResults.Results.Count() == 1 ? " 1 Learning Opportunity" : item.LoppsOwnedByResults.Results.Count() + " Learning Opportunities" ),
							CategoryLabel = "Owns/Offers " + ( item.LoppsOwnedByResults.Results.Count() == 1 ? " Learning Opportunity" : " Learning Opportunities" ),
							CategoryType = ButtonCategoryTypes.OrganizationOwns.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderDetailPageLink.ToString(),
							TotalItems = item.LoppsOwnedByResults.Results.Count(),
							Items = item.LoppsOwnedByResults.Results.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( new SearchResultButton.Helpers.DetailPageLink()
							{
								TargetLabel = m.Title,
								TargetId = m.Id,
								TargetType = ButtonTargetEntityTypes.LearningOpportunity.ToString()
							} ) )
						},
						//Owns Competency Frameworks
						new SearchResultButton()
						{
							//CategoryLabel = "Owns " + ( item.FrameworksOwnedByResults.Results.Count() == 1 ? " 1 Competency Framework" : item.FrameworksOwnedByResults.Results.Count() + " Competency Frameworks" ),
							CategoryLabel = "Owns " + ( item.FrameworksOwnedByResults.Results.Count() == 1 ? " Competency Framework" : " Competency Frameworks" ),
							CategoryType = ButtonCategoryTypes.OrganizationOwns.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderDetailPageLink.ToString(),
							TotalItems = item.FrameworksOwnedByResults.Results.Count(),
							Items = item.FrameworksOwnedByResults.Results.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( new SearchResultButton.Helpers.DetailPageLink()
							{
								TargetLabel = m.Title,
								TargetId = m.Id,
								TargetType = ButtonTargetEntityTypes.CompetencyFramework.ToString()
							} ) )
						},
						//Industry Type
						new SearchResultButton()
						{
							CategoryLabel = item.IndustryResults.Results.Count() == 1 ? "Industry Type" : "Industry Types",
							CategoryType = ButtonCategoryTypes.IndustryType.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderExternalCodeFilter.ToString(),
							TotalItems = item.IndustryResults.Results.Count(),
							Items = item.IndustryResults.Results.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( GetFilterItem( m, item.IndustryType.Id ) ) )
						}
					}
				) );
			}
			return output;
			//new List<Models.Helpers.SearchTag>()
			//{
			//                    //Quality Assurance
			//                    new Models.Helpers.SearchTag()
			//	{
			//		CategoryName = "Quality Assurance",
			//		DisplayTemplate = "{#} Quality Assurance",
			//		Name = "organizationroles", //Using the "quality" enum breaks this filter since it tries to find the matching item in the checkbox list and it doesn't exist
			//		TotalItems = item.QualityAssurance.Results.Count(),
			//		SearchQueryType = "merged", //Change this to "custom", or back to detail
			//		Items = item.QualityAssurance.Results.Take(10).ToList().ConvertAll( m => new Models.Helpers.SearchTagItem() {
			//			Display = "<b>" + m.Relationship + "</b>" + " by " + m.Agent, //[Accredited By] [Organization 1]
			//			QueryValues = new Dictionary<string, object>() {
			//					{ "Relationship", m.Relationship },
			//					{ "TextValue", m.Agent },
			//					{ "RelationshipId", m.RelationshipId },
			//					{ "AgentId", m.AgentId },
			//					{ "TargetType", "organization" },
			//					{ "AgentUrl", m.AgentUrl},
			//					{ "EntityStateId", m.EntityStateId },
			//					{ "IsThirdPartyOrganization", m.IsThirdPartyOrganization },
			//				}
			//		} ).ToList()
			//	},
			//                    //Quality Assurance Performed
			//                    new Models.Helpers.SearchTag()
			//	{
			//		CategoryName = "Quality Assurance Performed",
			//		DisplayTemplate = "{#} Quality Assurance Performed",
			//		Name = "qualityassuranceperformed",
			//		TotalItems = item.QualityAssuranceCombinedTotal,

			//                       //Items
			//                       SearchQueryType = "qaperformed",
			//		IsAjaxQuery = true,
			//		AjaxQueryName = "GetSearchResultPerformed",
			//		AjaxQueryValues = new Dictionary<string, object>()
			//		{
			//			{ "SearchType", "organization" },
			//			{ "RecordId", item.Id },
			//			{ "TargetEntityType", "QAPERFORMED" }
			//		}
			//	},

			//	//Organization Type
			//	new Models.Helpers.SearchTag()
			//	{
			//		CategoryName = "OrganizationType",
			//		CategoryLabel = "Organization Type",
			//		DisplayTemplate = "{#} Organization Type(s)",
			//		Name = TagTypes.ORGANIZATIONTYPE.ToString().ToLower(),
			//		TotalItems = item.AgentType.Items.Count(),
			//		SearchQueryType = "code",
			//		Items = GetSearchTagItems_Filter( item.AgentType.Items.Take(10).ToList(), "{Name}", item.AgentType.Id )
			//	},
			//	//Organization Sector Type
			//	new Models.Helpers.SearchTag()
			//	{
			//		CategoryName = "OrganizationSector",
			//		CategoryLabel = "Sector Type",
			//		DisplayTemplate = "{#} Sector{s}",
			//		Name = TagTypes.ORGANIZATIONSECTORTYPE.ToString().ToLower(),
			//		TotalItems = item.AgentSectorType.Items.Count(),
			//		SearchQueryType = "code",
			//		Items = GetSearchTagItems_Filter( item.AgentSectorType.Items.Take(10).ToList(), "{Name}", item.AgentSectorType.Id )
			//	},
			//                   //Organization Service Type
			//	new Models.Helpers.SearchTag()
			//	{
			//		CategoryName = "OrganizationService",
			//		CategoryLabel = "Service Type",
			//		DisplayTemplate = "{#} Service Type(s)",
			//		Name = TagTypes.ORG_SERVICE_TYPE.ToString().ToLower(),
			//		TotalItems = item.ServiceType.Items.Count(),
			//		SearchQueryType = "code",
			//		Items = GetSearchTagItems_Filter( item.ServiceType.Items, "{Name}", item.ServiceType.Id )
			//	},
			//	//owns
			//	new Models.Helpers.SearchTag()
			//	{
			//		CategoryName = "OwnsCredentials",
			//		CategoryLabel = "Owns Credentials",
			//		DisplayTemplate = "Owns {#} Credential(s)",
			//		Name = TagTypes.OWNED_BY.ToString().ToLower(),
			//		TotalItems = item.OwnedByResults.Results.Count(),
			//		SearchQueryType = "link",
			//		Items = item.OwnedByResults.Results.Take(10).ToList().ConvertAll(m => new Models.Helpers.SearchTagItem()
			//		{
			//			Display = m.Title,
			//			QueryValues = new Dictionary<string, object>()
			//			{
			//				{ "TargetId", m.Id }, //Credential ID
			//				{ "TargetType", "credential" },
			//				{ "IsReference", "false" },
			//			}
			//		} ).ToList()
			//	},
			//	//offers
			//	new Models.Helpers.SearchTag()
			//	{
			//		CategoryName = "OffersCredentials",
			//		CategoryLabel = "Offers Credentials",
			//		DisplayTemplate = "Offers {#} Credential(s)",
			//		Name = TagTypes.OFFERED_BY.ToString().ToLower(),
			//		TotalItems = item.OfferedByResults.Results.Count(),
			//		SearchQueryType = "link",
			//		Items = item.OfferedByResults.Results.Take(10).ToList().ConvertAll(m => new Models.Helpers.SearchTagItem()
			//		{
			//			Display = m.Title,
			//			QueryValues = new Dictionary<string, object>()
			//			{
			//				{ "TargetId", m.Id }, //Credential ID
			//				{ "TargetType", "credential" },
			//				{ "IsReference", "false" },
			//			}
			//		} ).ToList()
			//	},
			//	//asmts owned by
			//	new Models.Helpers.SearchTag()
			//	{
			//		CategoryName = "OwnsAssessments",
			//		CategoryLabel = "Owns Assessments",
			//		DisplayTemplate = "Owns {#} Assessment{s}",
			//		Name = TagTypes.ASMTS_OWNED_BY.ToString().ToLower(),
			//		TotalItems = item.AsmtsOwnedByResults.Results.Count(),
			//		SearchQueryType = "link",
			//		Items = item.AsmtsOwnedByResults.Results.Take(10).ToList().ConvertAll(m => new Models.Helpers.SearchTagItem()
			//		{
			//			Display = m.Title,
			//			QueryValues = new Dictionary<string, object>()
			//			{
			//				{ "TargetId", m.Id }, //asmt ID
			//				{ "TargetType", "assessment" },
			//				{ "IsReference", "false" },
			//			}
			//		} ).ToList()
			//	},
			//	//lopps owned by
			//	new Models.Helpers.SearchTag()
			//	{
			//		CategoryName = "OwnsLearningOpportunity",
			//		CategoryLabel = "Owns Learning Opportunities",
			//		DisplayTemplate = "Owns {#} Learning Opportunit{ies}",
			//		Name = TagTypes.LOPPS_OWNED_BY.ToString().ToLower(),
			//		TotalItems = item.LoppsOwnedByResults.Results.Count(),
			//		SearchQueryType = "link",
			//		Items = item.LoppsOwnedByResults.Results.Take(10).ToList().ConvertAll(m => new Models.Helpers.SearchTagItem()
			//		{
			//			Display = m.Title,
			//			QueryValues = new Dictionary<string, object>()
			//			{
			//				{ "TargetId", m.Id }, //lopp ID
			//				{ "TargetType", "learningopportunity" }, //??
			//				{ "IsReference", "false" },
			//			}
			//		} ).ToList()
			//	},
			//	new Models.Helpers.SearchTag()
			//	{
			//		CategoryName = "OwnsFrameworks",
			//		CategoryLabel = "Owns Competency Frameworks",
			//		DisplayTemplate = "Owns {#} Competency Framework{s}",
			//		Name = TagTypes.FRAMEWORKS_OWNED_BY.ToString().ToLower(),
			//		TotalItems = item.FrameworksOwnedByResults.Results.Count(),
			//		SearchQueryType = "link",
			//		Items = item.FrameworksOwnedByResults.Results.Take(10).ToList().ConvertAll(m => new Models.Helpers.SearchTagItem()
			//		{
			//			Display = m.Title,
			//			QueryValues = new Dictionary<string, object>()
			//			{
			//				{ "TargetId", m.Id }, //f ID
			//				{ "TargetType", "competencyframework" }, //??
			//			}
			//		} ).ToList()
			//	},

			//                 new Models.Helpers.SearchTag()
			//                 {
			//                     CategoryName = "Quality Assurance",
			//                     DisplayTemplate = "{#} Quality Assurance",
			//                     Name = "qualityAssuranceBy", //Using the "quality" enum breaks this filter since it tries to find the matching item in the checkbox list and it doesn't exist
			//TotalItems = item.QualityAssurance.Results.Count(),
			//                     SearchQueryType = "link", //Change this to "custom", or back to detail
			////Something like this...
			////QAOrgRolesResults is a list of 1 role and 1 org (org repeating for each relevant role)
			////e.g. [Accredited By] [Organization 1], [Approved By] [Organization 1], [Accredited By] [Organization 2], etc.
			//Items = item.QualityAssurance.Results.Take(10).ToList().ConvertAll( m => new Models.Helpers.SearchTagItem() {
			//                     Display = "<b>" + m.Relationship + "</b>" + "  " + m.Agent, //[Accredited By] [Organization 1]
			//QueryValues = new Dictionary<string, object>() {
			//                             { "TargetType", "organization" },
			//                             { "TargetId", m.AgentId },
			//                             { "IsThirdPartyOrganization", m.IsThirdPartyOrganization },
			//                         }
			//                     } ).ToList()
			//                 },

			//Industries
			//	new Models.Helpers.SearchTag()
			//	{
			//		CategoryName = "Industries",
			//		DisplayTemplate = "{#} Industr{ies}",
			//		Name = TagTypes.INDUSTRIES.ToString().ToLower(),
			//		TotalItems = item.IndustryResults.Results.Count(),
			//                       //SearchQueryType = "framework",
			//                       SearchQueryType = "text",
			//                       //Items = GetSearchTagItems_Filter( item.NaicsResults.Results, "{Name}", item.NaicsResults.CategoryId )
			//                       Items = item.IndustryResults.Results.Take(10).ToList().ConvertAll( m => new Models.Helpers.SearchTagItem() { Display = m.CodeTitle, QueryValues = new Dictionary<string, object>() { { "TextValue", m.CodeTitle } } } )
			//	},
			//},
		}
		/*
		public MainSearchResults ConvertOrganizationResultsOLD(List<Organization> results, int totalResults, string searchType)
		{
			var output = new MainSearchResults() { TotalResults = totalResults, SearchType = searchType };
			foreach ( var item in results )
			{
				output.Results.Add( Result( item.Name, item.FriendlyName, item.Description, item.Id,
					new Dictionary<string, object>()
					{
						{ "Name", item.Name },
						{ "Description", item.Description },
						{ "OwnerId", 0 },
						//{ "CanEditRecord", item.CanEditRecord },
						{ "AvailableAt", new { Type = "locations", Data = ConvertAddresses( new List<Address>() { item.Address } ) } },
						{ "Locations", ConvertAddresses( new List<Address>() { item.Address } ) },
						{ "SearchType", searchType },
						{ "RecordId", item.Id },
						{ "UrlTitle", item.FriendlyName },
						{ "Logo", item.Image },
						{ "ResultImageUrl", item.Image ?? "" },
						{ "Location", item.Address.AddressCountry + ( string.IsNullOrWhiteSpace( item.Address.AddressCountry ) ? "" : " - " ) + item.Address.AddressLocality + ( string.IsNullOrWhiteSpace( item.Address.AddressLocality ) ? "" : ", " ) + item.Address.AddressRegion },

						{ "Coordinates", new { Type = "coordinates", Data = new { Latitude = item.Address.Latitude, Longitude = item.Address.Longitude } } },
						{ "IsQA", item.ISQAOrganization ? "true" : "false" },
						//{ "AvailableAt", new { Type = "locations", Data = ConvertAddresses( item.Addresses ) } },

					},
					null,
					new List<Models.Helpers.SearchTag>()
					{
						//Organization Type
						new Models.Helpers.SearchTag()
						{
							CategoryName = "OrganizationType",
							CategoryLabel = "Organization Type",
							DisplayTemplate = "{#} Organization Type(s)",
							Name = TagTypes.ORGANIZATIONTYPE.ToString().ToLower(),
							TotalItems = item.AgentType.Items.Count(),
							SearchQueryType = "code",
							Items = GetSearchTagItems_Filter( item.AgentType.Items, "{Name}", item.AgentType.Id )
						},
						//Organization Sector Type
						new Models.Helpers.SearchTag()
						{
							CategoryName = "OrganizationSector",
							CategoryLabel = "Sector Type",
							DisplayTemplate = "{#} Economic Sector{s}",
							Name = TagTypes.ORGANIZATIONSECTORTYPE.ToString().ToLower(),
							TotalItems = item.AgentSectorType.Items.Count(),
							SearchQueryType = "code",
							Items = GetSearchTagItems_Filter( item.AgentSectorType.Items, "{Name}", item.AgentSectorType.Id )
						},
					}
				) );
			}
			return output;
		}
		*/

		public MainSearchResults ConvertAssessmentResults(List<AssessmentProfile> results, int totalResults, string searchType)
		{
			var output = new MainSearchResults() { TotalResults = totalResults, SearchType = searchType };
			foreach ( var item in results )
			{
				var subjects = Deduplicate( item.Subjects );
				var mergedQA = item.QualityAssurance.Results.Concat( item.Org_QAAgentAndRoles.Results ).ToList();

				output.Results.Add( Result( item.Name, item.FriendlyName, item.Description, item.Id,
					new Dictionary<string, object>()
					{
						{ "Name", item.Name },
						{ "Description", item.Description },
						{ "Owner", string.IsNullOrWhiteSpace( item.OrganizationName ) ? "" : item.OrganizationName },
						{ "OwnerId", item.OwningOrganizationId },
						{ "OwnerCTID", item.PrimaryOrganizationCTID },
						{ "ctid", item.CTID },
						{ "AvailableAt", new { Type = "locations", Data = ConvertAddresses( item.Addresses ) } },
						{ "Locations", ConvertAddresses( item.Addresses ) },
							{ "LifeCycleStatus", item.LifeCycleStatus },
						{ "SearchType", searchType },
						{ "RecordId", item.Id },
						{ "ResultNumber", item.ResultNumber },
						{ "UrlTitle", item.FriendlyName },
						{ "OrganizationUrlTitle", item.PrimaryOrganizationFriendlyName },
						{ "Created", item.Created.ToShortDateString() },
						{ "LastUpdated", item.LastUpdated.ToShortDateString() },
						{ "T_LastUpdated", item.LastUpdated.ToString("yyyy-MM-dd HH:mm:ss") },
						{ "T_StateId", item.EntityStateId },
						{ "T_BroadType", "Assessment" },
						{ "T_CTDLType", "ceterms:AssessmentProfile" },
						{ "T_CTDLTypeLabel", "Assessment Profile" },
						{ "T_Locations", ConvertAddressesToJArray(item.Addresses) }
					},
					null,
					null,
					new List<SearchResultButton>()
					{
						//Assessment Quality Assurance
						new SearchResultButton()
						{
							CategoryLabel = "Quality Assurance",
							CategoryType = ButtonCategoryTypes.QualityAssuranceReceived.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderQualityAssurance.ToString(),
							TotalItems = mergedQA.Count(),
							Items = mergedQA.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( m ) ).ToList()
						},
						//Financial Assistance
						new SearchResultButton()
						{
							CategoryLabel = "Financial Assistance",
							CategoryType = ButtonCategoryTypes.FinancialAssistance.ToString(),
							HandlerType = ButtonHandlerTypes.handler_GetRelatedItemsViaAJAX.ToString(),
							TotalItems = item.FinancialAidCount,
							RenderData = JObject.FromObject(new SearchResultButton.Helpers.AjaxDataForResult()
							{
								AjaxQueryName = "GetFinancialAssistance",
								TargetEntityType = ButtonTargetEntityTypes.FinancialAssistance.ToString()
							} )
						},
						//Connections
						new SearchResultButton()
						{
							CategoryLabel = item.CredentialsList.Results.Count() == 1 ? "Connection" : "Connections",
							CategoryType = ButtonCategoryTypes.Connection.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderConnection.ToString(),
							TotalItems = item.CredentialsList.Results.Count(),
							Items = item.CredentialsList.Results.Take(10).ToList().ConvertAll( m => JObject.FromObject( new SearchResultButton.Helpers.DetailPageLink()
							{
								ConnectionLabel = m.Connection,
								ConnectionType = m.ConnectionId.ToString(),
								TargetLabel = m.Credential,
								TargetType = ButtonSearchTypes.Credential.ToString(),
								TargetId = m.CredentialId
							} ) )
						},
						//Subjects
						new SearchResultButton()
						{
							CategoryLabel = subjects.Count() == 1 ? "Subject" : "Subjects",
							CategoryType = ButtonCategoryTypes.Subject.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderDetailPageLink.ToString(),
							TotalItems = subjects.Count(),
							Items = subjects.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( new SearchResultButton.Helpers.DetailPageLink()
							{
								TargetLabel = m,
								TargetType = ButtonSearchTypes.Credential.ToString(),
								TargetId = item.Id
							} ) )
						},
						//Assessment Use Type
						new SearchResultButton()
						{
							CategoryLabel = item.AssessmentUseTypes.Results.Count() == 1 ? "Assessment Use Type" : "Assessment Use Types",
							CategoryType = ButtonCategoryTypes.AssessmentUseType.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderCheckboxFilter.ToString(),
							TotalItems = item.AssessmentUseTypes.Results.Count(),
							Items = item.AssessmentUseTypes.Results.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( GetFilterItem( m, item.AssessmentUseTypes.CategoryId ) ) )
						},
						//Assessment Method Type
						new SearchResultButton()
						{
							CategoryLabel = item.AssessmentMethodTypes.Results.Count() == 1 ? "Assessment Method Type" : "Assessment Method Types",
							CategoryType = ButtonCategoryTypes.AssessmentMethodType.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderCheckboxFilter.ToString(),
							TotalItems = item.AssessmentMethodTypes.Results.Count(),
							Items = item.AssessmentMethodTypes.Results.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( GetFilterItem( m, item.AssessmentMethodTypes.CategoryId ) ) )
						},
						//Scoring Method Type
						new SearchResultButton()
						{
							CategoryLabel = item.ScoringMethodTypes.Results.Count() == 1 ? "Scoring Method Type" : "Scoring Method Types",
							CategoryType = ButtonCategoryTypes.ScoringMethodType.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderCheckboxFilter.ToString(),
							TotalItems = item.ScoringMethodTypes.Results.Count(),
							Items = item.ScoringMethodTypes.Results.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( GetFilterItem( m, item.ScoringMethodTypes.CategoryId ) ) )
						},
						//Delivery Method Type
						new SearchResultButton()
						{
							CategoryLabel = item.DeliveryMethodTypes.Results.Count() == 1 ? "Delivery Method Type" : "Delivery Method Types",
							CategoryType = ButtonCategoryTypes.AssessmentDeliveryType.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderCheckboxFilter.ToString(),
							TotalItems = item.DeliveryMethodTypes.Results.Count(),
							Items = item.DeliveryMethodTypes.Results.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( GetFilterItem( m, item.DeliveryMethodTypes.CategoryId ) ) )
						},
						//Audience Type
						new SearchResultButton()
						{
							CategoryLabel = item.AudienceTypes.Results.Count() == 1 ? "Audience Type" : "Audience Types",
							CategoryType = ButtonCategoryTypes.AudienceType.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderCheckboxFilter.ToString(),
							TotalItems = item.AudienceTypes.Results.Count(),
							Items = item.AudienceTypes.Results.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( GetFilterItem( m, item.AudienceTypes.CategoryId ) ) )
						},
						//Audience Level Type
						new SearchResultButton()
						{
							CategoryLabel = item.AudienceLevelTypes.Results.Count() == 1 ? "Audience Level Type" : "Audience Level Types",
							CategoryType = ButtonCategoryTypes.AudienceLevelType.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderCheckboxFilter.ToString(),
							TotalItems = item.AudienceLevelTypes.Results.Count(),
							Items = item.AudienceLevelTypes.Results.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( GetFilterItem( m, item.AudienceLevelTypes.CategoryId ) ) )
						},
						//Durations
						new SearchResultButton()
						{
							CategoryLabel = item.EstimatedDuration.Count() == 1 ? "Time Estimate" : "Time Estimates",
							CategoryType = ButtonCategoryTypes.EstimatedDuration.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderDetailPageLink.ToString(),
							TotalItems = item.EstimatedDuration.Count(),
							Items = item.EstimatedDuration.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( new SearchResultButton.Helpers.DetailPageLink()
							{
								TargetLabel = m.IsRange ? m.MinimumDuration.Print() + " - " + m.MaximumDuration.Print() : string.IsNullOrEmpty( m.ExactDuration.Print() ) ? m.Conditions :  m.ExactDuration.Print(),
								TargetType = ButtonTargetEntityTypes.Assessment.ToString(),
								TargetId = item.Id
							} ) )
						},
						//Occupations
						new SearchResultButton()
						{
							CategoryLabel = item.OccupationResults.Results.Count() == 1 ? "Occupation" : "Occupations",
							CategoryType = ButtonCategoryTypes.OccupationType.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderExternalCodeFilter.ToString(),
							TotalItems = item.OccupationResults.Results.Count(),
							Items = item.OccupationResults.Results.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( GetFilterItem( m, item.OccupationResults.CategoryId ) ) )
						},
						//Industries
						new SearchResultButton()
						{
							CategoryLabel = item.IndustryResults.Results.Count() == 1 ? "Industry" : "Industries",
							CategoryType = ButtonCategoryTypes.IndustryType.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderExternalCodeFilter.ToString(),
							TotalItems = item.IndustryResults.Results.Count(),
							Items = item.IndustryResults.Results.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( GetFilterItem( m, item.IndustryResults.CategoryId ) ) )
						},
						//Instructional Program Classifications
						new SearchResultButton()
						{
							CategoryLabel = item.InstructionalProgramClassification.Results.Count() == 1 ? "Instructional Program Type" : "Instructional Program Types",
							CategoryType = ButtonCategoryTypes.InstructionalProgramType.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderExternalCodeFilter.ToString(),
							TotalItems = item.InstructionalProgramClassification.Results.Count(),
							Items = item.InstructionalProgramClassification.Results.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( GetFilterItem( m, item.InstructionalProgramClassification.CategoryId ) ) )
						},
						//Competencies
						new SearchResultButton()
						{
							CategoryLabel = "Assesses " + ( item.CompetenciesCount == 1 ? " 1 Competency" : item.CompetenciesCount + " Competencies"),
							CategoryType = ButtonCategoryTypes.Competency.ToString(),
							HandlerType = ButtonHandlerTypes.handler_GetRelatedItemsViaAJAX.ToString(),
							TotalItems = item.CompetenciesCount,
							RenderData = JObject.FromObject( new SearchResultButton.Helpers.AjaxDataForResult()
							{
								AjaxQueryName = "GetSearchResultCompetencies",
								TargetEntityType = ButtonTargetEntityTypes.Competency.ToString()
							} )
						},//Costs
						new SearchResultButton()
						{
							CategoryLabel = item.NumberOfCostProfileItems == 1 ? "Estimated Cost" : "Estimated Costs",
							CategoryType = ButtonCategoryTypes.EstimatedCost.ToString(),
							HandlerType = ButtonHandlerTypes.handler_GetRelatedItemsViaAJAX.ToString(),
							TotalItems = item.NumberOfCostProfileItems,
							RenderData = JObject.FromObject( new SearchResultButton.Helpers.AjaxDataForResult()
							{
								AjaxQueryName = "GetSearchResultCosts",
								TargetEntityType = ButtonTargetEntityTypes.EstimatedCost.ToString()
							} )
						},
						//OutcomeProfiles
						new SearchResultButton()
						{
							CategoryLabel = item.AggregateDataProfileCount + item.DataSetProfileCount == 1 ? "Outcome Data" : "Outcome Data",
							CategoryType = ButtonCategoryTypes.OutcomeData.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderDetailPageLink.ToString(),
							TotalItems = item.AggregateDataProfileCount + item.DataSetProfileCount,
							Items = new List<string>() { item.AggregateDataProfileSummary }.ToList().ConvertAll( m => JObject.FromObject( new SearchResultButton.Helpers.DetailPageLink()
							{
								ConnectionLabel = "Outcome Data",
								TargetLabel = m,
								TargetType = ButtonSearchTypes.AssessmentProfile.ToString(),
								TargetId = item.Id
							} ) )
						},
						//Related TVP
						new SearchResultButton()
						{
							CategoryLabel = item.TransferValueCount == 1 ? "Has Transfer Value" : "Has Transfer Values",
							CategoryType = ButtonCategoryTypes.HasTransferValue.ToString(),
							HandlerType = ButtonHandlerTypes.handler_GetRelatedItemsViaAJAX.ToString(),
							TotalItems = item.TransferValueCount,
							RenderData = JObject.FromObject( new SearchResultButton.Helpers.AjaxDataForResult()
							{
								AjaxQueryName = "GetSearchResultTVP",
								TargetEntityType = ButtonTargetEntityTypes.TransferValue.ToString()	//this appears wrong results in an org TransverValue search
							} )
						},
					}
				) );
			}
			return output;
		}
		//

		public MainSearchResults ConvertLearningOpportunityResults(List<LearningOpportunityProfile> results, int totalResults, string searchType)
		{
			var output = new MainSearchResults() { TotalResults = totalResults, SearchType = searchType };
			foreach ( var item in results )
			{
				var subjects = Deduplicate( item.Subjects );
				var mergedQA = item.QualityAssurance.Results.Concat( item.Org_QAAgentAndRoles.Results ).ToList();
				var statusLabel = "";
				if ( UtilityManager.GetAppKeyValue( "showingNonActiveStatusLabel", false ) )
				{
					if ( !string.IsNullOrWhiteSpace( item.LifeCycleStatus ) && item.LifeCycleStatus != "Active" )
						statusLabel = " [** " + item.LifeCycleStatus.ToUpper() + " **]";
				}
				output.Results.Add( Result( item.Name + statusLabel, item.FriendlyName, item.Description, item.Id,
					new Dictionary<string, object>()
					{
						{ "Name", item.Name + statusLabel},
						{ "Description", item.Description },
						{ "Owner", string.IsNullOrWhiteSpace( item.OrganizationName ) ? "" : item.OrganizationName },
						{ "OwnerId", item.OwningOrganizationId },
						{ "OwnerCTID", item.PrimaryOrganizationCTID },
						{ "AvailableAt", new { Type = "locations", Data = ConvertAddresses( item.Addresses ) } },
						{ "Locations", ConvertAddresses( item.Addresses ) },
						{ "LifeCycleStatus", item.LifeCycleStatus },
						{ "SearchType", searchType },
						{ "RecordId", item.Id },
						{ "ResultNumber", item.ResultNumber },
						{ "ctid", item.CTID },
						{ "UrlTitle", item.FriendlyName },
						{ "OrganizationUrlTitle", item.PrimaryOrganizationFriendlyName },
						{ "Created", item.Created.ToShortDateString() },
						{ "LastUpdated", item.LastUpdated.ToShortDateString() },
						{ "T_LastUpdated", item.LastUpdated.ToString("yyyy-MM-dd HH:mm:ss") },
						{ "EntityTypeId", item.LearningEntityTypeId },
						{ "T_StateId", item.EntityStateId },
						{ "T_BroadType", "LearningOpportunity" },
						{ "T_CTDLType", item.LearningTypeSchema },
						{ "T_CTDLTypeLabel", item.LearningEntityTypeLabel },
						{ "T_Locations", ConvertAddressesToJArray(item.Addresses) }
					},
					null,
					null,
					new List<SearchResultButton>()
					{
						//Learning Opportunity Quality Assurance
						new SearchResultButton()
						{
							CategoryLabel = "Quality Assurance",
							CategoryType = ButtonCategoryTypes.QualityAssuranceReceived.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderQualityAssurance.ToString(),
							TotalItems = mergedQA.Count(),
							Items = mergedQA.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( m ) ).ToList()
						},
						//Subjects
						new SearchResultButton()
						{
							CategoryLabel = subjects.Count() == 1 ? "Subject" : "Subjects",
							CategoryType = ButtonCategoryTypes.Subject.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderDetailPageLink.ToString(),
							TotalItems = subjects.Count(),
							Items = item.Subjects.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( new SearchResultButton.Helpers.DetailPageLink()
							{
								TargetLabel = m,
								TargetType = ButtonSearchTypes.Credential.ToString(),
								TargetId = item.Id
							} ) )
						},
						//Financial Assistance
						new SearchResultButton()
						{
							CategoryLabel = "Financial Assistance",
							CategoryType = ButtonCategoryTypes.FinancialAssistance.ToString(),
							HandlerType = ButtonHandlerTypes.handler_GetRelatedItemsViaAJAX.ToString(),
							TotalItems = item.FinancialAidCount,
							RenderData = JObject.FromObject(new SearchResultButton.Helpers.AjaxDataForResult()
							{
								AjaxQueryName = "GetFinancialAssistance",
								TargetEntityType = ButtonTargetEntityTypes.FinancialAssistance.ToString()
							} )
						},
						//Connections
						new SearchResultButton()
						{
							CategoryLabel = item.CredentialsList.Results.Count() == 1 ? "Connection" : "Connections",
							CategoryType = ButtonCategoryTypes.Connection.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderConnection.ToString(),
							TotalItems = item.CredentialsList.Results.Count(),
							Items = item.CredentialsList.Results.Take(10).ToList().ConvertAll( m => JObject.FromObject( new SearchResultButton.Helpers.DetailPageLink()
							{
								ConnectionLabel = m.Connection,
								ConnectionType = m.ConnectionId.ToString(),
								TargetLabel = m.Credential,
								TargetType = ButtonSearchTypes.Credential.ToString(),
								TargetId = m.CredentialId
							} ) )
						},
						//Delivery Method Type
						new SearchResultButton()
						{
							CategoryLabel = item.DeliveryMethodTypes.Results.Count() == 1 ? "Delivery Method Type" : "Delivery Method Types",
							CategoryType = ButtonCategoryTypes.LearningDeliveryType.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderCheckboxFilter.ToString(),
							TotalItems = item.DeliveryMethodTypes.Results.Count(),
							Items = item.DeliveryMethodTypes.Results.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( GetFilterItem( m, item.DeliveryMethodTypes.CategoryId ) ) )
						},
						//Learning Method Type
						new SearchResultButton()
						{
							CategoryLabel = item.LearningMethodTypes.Results.Count() == 1 ? "Learning Method Type" : "Learning Method Types",
							CategoryType = ButtonCategoryTypes.LearningMethodType.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderCheckboxFilter.ToString(),
							TotalItems = item.LearningMethodTypes.Results.Count(),
							Items = item.LearningMethodTypes.Results.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( GetFilterItem( m, item.LearningMethodTypes.CategoryId ) ) )
						},
						//Audience Type
						new SearchResultButton()
						{
							CategoryLabel = item.AudienceTypes.Results.Count() == 1 ? "Audience Type" : "Audience Types",
							CategoryType = ButtonCategoryTypes.AudienceType.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderCheckboxFilter.ToString(),
							TotalItems = item.AudienceTypes.Results.Count(),
							Items = item.AudienceTypes.Results.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( GetFilterItem( m, item.AudienceTypes.CategoryId ) ) )
						},
						//Audience Level Type
						new SearchResultButton()
						{
							CategoryLabel = item.AudienceLevelTypes.Results.Count() == 1 ? "Audience Level Type" : "Audience Level Types",
							CategoryType = ButtonCategoryTypes.AudienceLevelType.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderCheckboxFilter.ToString(),
							TotalItems = item.AudienceLevelTypes.Results.Count(),
							Items = item.AudienceLevelTypes.Results.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( GetFilterItem( m, item.AudienceLevelTypes.CategoryId ) ) )
						},
						//Durations
						new SearchResultButton()
						{
							CategoryLabel = item.EstimatedDuration.Count() == 1 ? "Time Estimate" : "Time Estimates",
							CategoryType = ButtonCategoryTypes.EstimatedDuration.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderDetailPageLink.ToString(),
							TotalItems = item.EstimatedDuration.Count(),
							Items = item.EstimatedDuration.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( new SearchResultButton.Helpers.DetailPageLink()
							{
								TargetLabel = m.IsRange ? m.MinimumDuration.Print() + " - " + m.MaximumDuration.Print() : string.IsNullOrEmpty( m.ExactDuration.Print() ) ? m.Conditions :  m.ExactDuration.Print(),
								TargetType = ButtonTargetEntityTypes.LearningOpportunity.ToString(),
								TargetId = item.Id
							} ) )
						},
						//Occupations
						new SearchResultButton()
						{
							CategoryLabel = item.OccupationResults.Results.Count() == 1 ? "Occupation" : "Occupations",
							CategoryType = ButtonCategoryTypes.OccupationType.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderExternalCodeFilter.ToString(),
							TotalItems = item.OccupationResults.Results.Count(),
							Items = item.OccupationResults.Results.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( GetFilterItem( m, item.OccupationResults.CategoryId ) ) )
						},
						//Industries
						new SearchResultButton()
						{
							CategoryLabel = item.IndustryResults.Results.Count() == 1 ? "Industry" : "Industries",
							CategoryType = ButtonCategoryTypes.IndustryType.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderExternalCodeFilter.ToString(),
							TotalItems = item.IndustryResults.Results.Count(),
							Items = item.IndustryResults.Results.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( GetFilterItem( m, item.IndustryResults.CategoryId ) ) )
						},
						//Instructional Program Classifications
						new SearchResultButton()
						{
							CategoryLabel = item.InstructionalProgramClassification.Results.Count() == 1 ? "Instructional Program Type" : "Instructional Program Types",
							CategoryType = ButtonCategoryTypes.InstructionalProgramType.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderExternalCodeFilter.ToString(),
							TotalItems = item.InstructionalProgramClassification.Results.Count(),
							Items = item.InstructionalProgramClassification.Results.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( GetFilterItem( m, item.InstructionalProgramClassification.CategoryId ) ) )
						},
						//Competencies
						new SearchResultButton()
						{
							CategoryLabel = "Teaches " + ( item.CompetenciesCount == 1 ? " 1 Competency" : item.CompetenciesCount + " Competencies"),
							CategoryType = ButtonCategoryTypes.Competency.ToString(),
							HandlerType = ButtonHandlerTypes.handler_GetRelatedItemsViaAJAX.ToString(),
							TotalItems = item.CompetenciesCount,
							RenderData = JObject.FromObject( new SearchResultButton.Helpers.AjaxDataForResult()
							{
								AjaxQueryName = "GetSearchResultCompetencies",
								TargetEntityType = ButtonTargetEntityTypes.Competency.ToString()
							} )
						},
						//Costs
						new SearchResultButton()
						{
							CategoryLabel = item.NumberOfCostProfileItems == 1 ? "Estimated Cost" : "Estimated Costs",
							CategoryType = ButtonCategoryTypes.EstimatedCost.ToString(),
							HandlerType = ButtonHandlerTypes.handler_GetRelatedItemsViaAJAX.ToString(),
							TotalItems = item.NumberOfCostProfileItems,
							RenderData = JObject.FromObject( new SearchResultButton.Helpers.AjaxDataForResult()
							{
								AjaxQueryName = "GetSearchResultCosts",
								TargetEntityType = ButtonTargetEntityTypes.EstimatedCost.ToString()
							} )
						},
						//OutcomeProfiles
						new SearchResultButton()
						{
							CategoryLabel = item.AggregateDataProfileCount + item.DataSetProfileCount == 1 ? "Outcome Data" : "Outcome Data",
							CategoryType = ButtonCategoryTypes.OutcomeData.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderDetailPageLink.ToString(),
							TotalItems = item.AggregateDataProfileCount + item.DataSetProfileCount,
							Items = new List<string>() { item.AggregateDataProfileSummary }.ToList().ConvertAll( m => JObject.FromObject( new SearchResultButton.Helpers.DetailPageLink()
							{
								ConnectionLabel = "Outcome Data",
								TargetLabel = m,
								TargetType = ButtonSearchTypes.LearningOpportunityProfile.ToString(),
								TargetId = item.Id
							} ) )
						},
						//Related TVP
						new SearchResultButton()
						{
							CategoryLabel = item.TransferValueCount == 1 ? "Has Transfer Value" : "Has Transfer Values",
							CategoryType = ButtonCategoryTypes.HasTransferValue.ToString(),
							HandlerType = ButtonHandlerTypes.handler_GetRelatedItemsViaAJAX.ToString(),
							TotalItems = item.TransferValueCount,
							RenderData = JObject.FromObject( new SearchResultButton.Helpers.AjaxDataForResult()
							{
								AjaxQueryName = "GetSearchResultTVP",
								TargetEntityType = ButtonTargetEntityTypes.TransferValue.ToString()
							} )
						},
						//LWIAs
						//new SearchResultButton()
						//{
						//	CategoryLabel = item.LearningMethodTypes.Results.Count() == 1 ? "Other Tags" : "Other Tags",
						//	CategoryType = ButtonCategoryTypes.OtherTags.ToString(),
						//	HandlerType = ButtonHandlerTypes.handler_RenderCheckboxFilter.ToString(),
						//	TotalItems = item.LearningMethodTypes.Results.Count(),
						//	Items = item.LearningMethodTypes.Results.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( GetFilterItem( m, item.LearningMethodTypes.CategoryId ) ) )
						//},
					}
				) );
			}
			return output;
		}
		//

		private JObject LoadResourceData( string uri, JArray relatedItems, JArray errors )
		{
			var httpData = "";
			var matchData = new JObject();
			try
			{
				if ( string.IsNullOrWhiteSpace( uri ) )
				{
					return null;
				}

				//Should always be found here...
				var match = ( JObject ) relatedItems.FirstOrDefault( m => m[ "@id" ] != null && m[ "@id" ].ToString() == uri );
				if ( match != null )
				{
					matchData = match;
					return match;
				}

				//Shouldn't happen, but just in case...
				var cacheID = "rawdata_" + uri;
				var data = ( string ) MemoryCache.Default[ cacheID ];
				if ( string.IsNullOrWhiteSpace( data ) )
				{
					var client = new HttpClient();
					client.DefaultRequestHeaders.TryAddWithoutValidation( "Accept", "application/json" );
					data = client.GetAsync( uri ).Result.Content.ReadAsStringAsync().Result ?? "";
					httpData = data;
					MemoryCache.Default.Remove( cacheID );
					MemoryCache.Default.Add( cacheID, data, DateTime.Now.AddMinutes( 15 ) );
				}

				//Handle @graph data
				var json = JObject.Parse( data );
				if( json["@graph"] != null )
				{
					var graph = ( JArray ) json[ "@graph" ];
					if( graph.Count() > 0 )
					{
						return ( JObject ) ( graph.FirstOrDefault( m => m[ "@id" ].ToString() == uri ) ?? graph.FirstOrDefault() ?? json );
					}
				}

				json[ "RequestURI" ] = uri;
				return json;
			}
			catch ( Exception ex )
			{
				errors.Add( new JObject()
				{
					{ "URI", uri },
					{ "ErrorMessage", ex.Message },
					{ "RawMatchData", matchData },
					{ "RawHTTPData", httpData }
				} );

				return new JObject()
				{
					{ "RequestURI", uri }
				};
			}
		}
		private List<JObject> GetTopRelatedItems( List<string> uris, JArray relatedItems, JArray errors, int take = 10 )
		{
			return uris.Take( take ).ToList().Select( m => LoadResourceData( m, relatedItems, errors ) ).ToList();
		}

		private void Log( string text )
		{
			try
			{
				//System.IO.File.AppendAllText( "C:/@logs/finderuhoh.txt", text + System.Environment.NewLine );
			}
			catch( Exception ex )
			{

			}
		}
		
		//Use the new ComposedSearchResultSet structure
		public MainSearchResults ConvertCompetencyFrameworkResultsV2( ComposedSearchResultSet results )
		{
			//Hold the results
			var output = new MainSearchResults()
			{
				TotalResults = results.TotalResults,
				SearchType = "competencyframework",
				RelatedItems = JArray.FromObject( results.RelatedItems ?? new List<JObject>() ),
				Debug = new JObject()
				{
					{ "Composed Results Debug", results.DebugInfo }
				}
			};

			try
			{
				//Convert the results to the old format
				foreach ( var result in results.Results )
				{
					output.Debug[ "Farthest Step" ] = "Getting Result URI";
					var resultURI = result.Data[ "@id" ].ToString();

					output.Debug[ "Farthest Step" ] = "Looking up Framework ID by its CTID";
					var ctid = result.Data[ "ceterms:ctid" ].ToString();
					var frameworkID = CompetencyFrameworkManager.GetByCtid( ctid )?.Id ?? -1;

					//Owner
					output.Debug[ "Farthest Step" ] = "Processing Creator/Owner/Publisher";
					var creators = results.GetRelatedItems( result.GetURIsForPathRegex( "^> ceasn:creator > ceterms:Agent$" ) );
					var publishers = results.GetRelatedItems( result.GetURIsForPathRegex( "^> ceasn:publisher > ceterms:Agent$" ) );
					var owner = creators.Concat( publishers ).FirstOrDefault( m => m != null ) ?? new JObject() { { "ceterms:name", new JObject() { { "en", "Unknown Organization" } } } };

					//Process Dates
					output.Debug[ "Farthest Step" ] = "Processing Other Fields";
					var name = CompetencyFrameworkServicesV2.GetEnglishString( result.Data[ "ceasn:name" ], "Unknown Name" );
					var description = CompetencyFrameworkServicesV2.GetEnglishString( result.Data[ "ceasn:description" ], "Unknown Description" );
					//var dateCreated = result.Data[ "ceasn:dateCreated" ] == null ? "Unknown" : result.Data[ "ceasn:dateCreated" ].ToString().Split( new string[] { "T" }, StringSplitOptions.RemoveEmptyEntries ).FirstOrDefault() ?? "Unknown";
					//var dateModified = result.Data[ "ceasn:dateModified" ] == null ? "Unknown" : result.Data[ "ceasn:dateModified" ].ToString().Split( new string[] { "T" }, StringSplitOptions.RemoveEmptyEntries ).FirstOrDefault() ?? "Unknown";
					var dateCreated = result.Metadata?[ "RecordCreated" ] == null ? "Unknown" : result.Metadata[ "RecordCreated" ].ToString().Split( new string[] { "T" }, StringSplitOptions.RemoveEmptyEntries ).FirstOrDefault() ?? "Unknown";
					var dateModified = result.Metadata?[ "RecordUpdated" ] == null ? "Unknown" : result.Metadata[ "RecordUpdated" ].ToString().Split( new string[] { "T" }, StringSplitOptions.RemoveEmptyEntries ).FirstOrDefault() ?? "Unknown";
					var friendlyName = CF.BaseFactory.FormatFriendlyTitle( name );

					//Process the result core
					output.Debug[ "Farthest Step" ] = "Processing Core Result";
					var coreProperties = new Dictionary<string, object>()
					{
						{ "CTID", ctid },
						{ "ctid", ctid }, //Not sure why we have both
						{ "Locations", new List<object>() { } },
						{ "DateCreated", dateCreated },
						{ "DateModified", dateModified },
						{ "LastUpdated", dateModified }, //Not sure why we have both
						{ "RawData", result.Data },
						{ "Name", name },
						{ "UrlTitle", friendlyName },
						{ "FriendlyName", friendlyName },
						{ "Description", description },
						{ "Owner", owner },
						{ "SearchType", "competencyframework" },
						{ "RecordId", ctid },
						{ "Errors", new JArray() },
						{ "T_LastUpdated", dateModified },
						{ "T_StateId", 3 },
						{ "T_BroadType", "CompetencyFramework" },
						{ "T_CTDLType", "ceasn:CompetencyFramework" },
						{ "T_CTDLTypeLabel", "Competency Framework" }
					};

					//Process gray buttons
					output.Debug[ "Farthest Step" ] = "Processing Gray Buttons";
					var buttons = new List<Models.Helpers.SearchResultButton>();

					//Competencies in this Framework
					output.Debug[ "Farthest Step" ] = "Processing Competencies";
					var competencyData = results.GetComposedPathSetWithResources( result.GetPathsForPathRegex( "^< ceasn:isPartOf < ceasn:Competency$" ) );
					buttons.Add( new SearchResultButton()
					{
						CategoryLabel = competencyData.TotalURIs > 1 ? "Competencies" : "Competency",
						CategoryType = ButtonCategoryTypes.Competency.ToString(),
						HandlerType = ButtonHandlerTypes.handler_RenderDetailPageLink.ToString(),
						TotalItems = competencyData.TotalURIs,
						Items = competencyData.RelatedItems.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( new SearchResultButton.Helpers.DetailPageLink()
						{
							TargetLabel = CompetencyFrameworkServicesV2.GetEnglishString( m[ "ceasn:competencyText" ], "Unknown Competency" ),
							TargetType = ButtonSearchTypes.CompetencyFramework.ToString(),
							TargetCTID = ctid //Framework CTID
						} ) )
					} );

					//Credentials
					output.Debug[ "Farthest Step" ] = "Processing Credentials";
					var credentialData = results.GetComposedPathSetWithResources( result.GetPathsForPathRegex( "ceterms:Credential$" ) );
					buttons.Add( new SearchResultButton()
					{
						CategoryLabel = credentialData.TotalURIs > 1 ? "Related Credentials" : "Related Credential",
						CategoryType = ButtonCategoryTypes.Credential.ToString(),
						HandlerType = ButtonHandlerTypes.handler_RenderDetailPageLink.ToString(),
						TotalItems = credentialData.TotalURIs,
						Items = credentialData.RelatedItems.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( new SearchResultButton.Helpers.DetailPageLink()
						{
							TargetLabel = CompetencyFrameworkServicesV2.GetEnglishString( m[ "ceterms:name" ], "Unknown Credential" ),
							TargetType = ButtonSearchTypes.Credential.ToString(),
							TargetCTID = m[ "ceterms:ctid" ]?.ToString() ?? ""
						} ) )
					} );

					//Assessments
					output.Debug[ "Farthest Step" ] = "Processing Assessments";
					var assessmentData = results.GetComposedPathSetWithResources( result.GetPathsForPathRegex( "ceterms:AssessmentProfile$" ) );
					buttons.Add( new SearchResultButton()
					{
						CategoryLabel = assessmentData.TotalURIs > 1 ? "Related Assessments" : "Related Assessment",
						CategoryType = ButtonCategoryTypes.AssessmentProfile.ToString(),
						HandlerType = ButtonHandlerTypes.handler_RenderDetailPageLink.ToString(),
						TotalItems = assessmentData.TotalURIs,
						Items = assessmentData.RelatedItems.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( new SearchResultButton.Helpers.DetailPageLink()
						{
							TargetLabel = CompetencyFrameworkServicesV2.GetEnglishString( m[ "ceterms:name" ], "Unknown Assessment" ),
							TargetType = ButtonSearchTypes.AssessmentProfile.ToString(),
							TargetCTID = m[ "ceterms:ctid" ]?.ToString() ?? ""
						} ) )
					} );

					//Learning Opportunities
					output.Debug[ "Farthest Step" ] = "Processing Learning Opportunities";
					var learningOpportunityData = results.GetComposedPathSetWithResources( result.GetPathsForPathRegex( "ceterms:LearningOpportunityProfile$" ) );
					buttons.Add( new SearchResultButton()
					{
						CategoryLabel = learningOpportunityData.TotalURIs > 1 ? "Related Learning Opportunities" : "Related Learning Opportunity",
						CategoryType = ButtonCategoryTypes.LearningOpportunityProfile.ToString(),
						HandlerType = ButtonHandlerTypes.handler_RenderDetailPageLink.ToString(),
						TotalItems = learningOpportunityData.TotalURIs,
						Items = learningOpportunityData.RelatedItems.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( new SearchResultButton.Helpers.DetailPageLink()
						{
							TargetLabel = CompetencyFrameworkServicesV2.GetEnglishString( m[ "ceterms:name" ], "Unknown Learning Opportunity" ),
							TargetType = ButtonSearchTypes.LearningOpportunityProfile.ToString(),
							TargetCTID = m[ "ceterms:ctid" ]?.ToString() ?? ""
						} ) )
					} );

					//Aligned Frameworks
					output.Debug[ "Farthest Step" ] = "Processing Aligned Frameworks";
					var alignedFrameworkData = results.GetComposedPathSetWithResources( result.GetPathsForPathRegex( @"^(?>>|<) ceasn:align\S* (?>>|<) ceasn:CompetencyFramework$" ) );
					buttons.Add( new SearchResultButton()
					{
						CategoryLabel = alignedFrameworkData.TotalURIs > 1 ? "Framework Alignments" : "Framework Alignment",
						CategoryType = ButtonCategoryTypes.Connection.ToString(),
						HandlerType = ButtonHandlerTypes.handler_RenderDetailPageLink.ToString(),
						TotalItems = alignedFrameworkData.TotalURIs,
						Items = alignedFrameworkData.RelatedItems.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( new SearchResultButton.Helpers.DetailPageLink()
						{
							TargetLabel = CompetencyFrameworkServicesV2.GetEnglishString( m[ "ceasn:name" ], "Unknown Framework" ),
							TargetType = ButtonSearchTypes.CompetencyFramework.ToString(),
							TargetCTID = m[ "ceterms:ctid" ]?.ToString() ?? ""
						} ) )
					} );

					//Aligned Competencies
					output.Debug[ "Farthest Step" ] = "Processing Aligned Competencies";
					var alignedCompetencyData = results.GetComposedPathSetWithResources( result.GetPathsForPathRegex( @"^< ceasn:isPartOf < ceasn:Competency (?>>|<) ceasn:\S*Alignment (?>>|<) ceasn:Competency$" ) );
					buttons.Add( new SearchResultButton()
					{
						CategoryLabel = alignedCompetencyData.TotalURIs > 1 ? "Competency Alignments" : "Competency Alignment",
						CategoryType = ButtonCategoryTypes.Connection.ToString(),
						HandlerType = ButtonHandlerTypes.handler_RenderDetailPageLink.ToString(),
						TotalItems = alignedCompetencyData.TotalURIs,
						Items = alignedCompetencyData.RelatedItems.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( new SearchResultButton.Helpers.DetailPageLink()
						{
							TargetLabel = CompetencyFrameworkServicesV2.GetEnglishString( m[ "ceasn:competencyText" ], "Unknown Competency" ),
							TargetType = ButtonSearchTypes.CompetencyFramework.ToString(),
							TargetCTID = m[ "ceasn:isPartOf" ]?.ToString()?.Split( new string[] { "/ce-" }, StringSplitOptions.RemoveEmptyEntries ).Select( n => "ce-" + n ).Last() ?? ""
						} ) )
					} );

					//Education Level Alignments
					//Note - Since these are combined from alignments at both the framework and competency level, TotalURIs reflects the total number of alignments, 
					//NOT the total number of concepts (even though the data provides the first 10 concepts from those alignments that were returned by the data)
					output.Debug[ "Farthest Step" ] = "Processing Education Level Alignments";
					var educationLevelData = results.GetComposedPathSetWithResources( result.GetPathsForPathRegex( "ceasn:educationLevelType > skos:Concept$" ) );
					buttons.Add( new SearchResultButton()
					{
						CategoryLabel = educationLevelData.TotalURIs > 1 ? "Education Levels" : "Education Level",
						CategoryType = ButtonCategoryTypes.Connection.ToString(),
						HandlerType = ButtonHandlerTypes.handler_RenderDetailPageLink.ToString(),
						TotalItems = educationLevelData.TotalURIs,
						Items = educationLevelData.RelatedItems.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( new SearchResultButton.Helpers.DetailPageLink()
						{
							TargetLabel = CompetencyFrameworkServicesV2.GetEnglishString( m[ "skos:prefLabel" ], "Unknown Level" ),
							TargetType = ButtonSearchTypes.CompetencyFramework.ToString(),
							TargetCTID = ctid //Framework CTID
						} ) )
					} );

					//Complexity Level Alignments
					//See note above, this works the same way
					output.Debug[ "Farthest Step" ] = "Processing Complexity Level Alignments";
					var complexityLevelData = results.GetComposedPathSetWithResources( result.GetPathsForPathRegex( "ceasn:complexityLevel > skos:Concept$" ) );
					buttons.Add( new SearchResultButton()
					{
						CategoryLabel = complexityLevelData.TotalURIs > 1 ? "Complexity Levels" : "Complexity Level",
						CategoryType = ButtonCategoryTypes.Connection.ToString(),
						HandlerType = ButtonHandlerTypes.handler_RenderDetailPageLink.ToString(),
						TotalItems = complexityLevelData.TotalURIs,
						Items = complexityLevelData.RelatedItems.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( new SearchResultButton.Helpers.DetailPageLink()
						{
							TargetLabel = CompetencyFrameworkServicesV2.GetEnglishString( m[ "skos:prefLabel" ], "Unknown Level" ),
							TargetType = ButtonSearchTypes.CompetencyFramework.ToString(),
							TargetCTID = ctid //Framework CTID
						} ) )
					} );

					//Related Concept Schemes (Combined)
					//See note above, this works the same way
					output.Debug[ "Farthest Step" ] = "Processing Concept Schemes";
					var conceptSchemeData = results.GetComposedPathSetWithResources( result.GetPathsForPathRegex( "skos:ConceptScheme$" ) );
					buttons.Add( new SearchResultButton()
					{
						CategoryLabel = conceptSchemeData.TotalURIs > 1 ? "Related Concept Schemes" : "Related Concept Scheme",
						CategoryType = ButtonCategoryTypes.Connection.ToString(),
						HandlerType = ButtonHandlerTypes.handler_RenderDetailPageLink.ToString(),
						TotalItems = conceptSchemeData.TotalURIs,
						Items = conceptSchemeData.RelatedItems.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( new SearchResultButton.Helpers.DetailPageLink()
						{
							TargetLabel = CompetencyFrameworkServicesV2.GetEnglishString( m[ "ceasn:name" ], "Unknown Concept Scheme" ),
							TargetType = ButtonSearchTypes.CompetencyFramework.ToString(),
							TargetCTID = ctid //Framework CTID
						} ) )
					} );

					//Add the result
					output.Results.Add( Result( name, friendlyName, description, frameworkID, coreProperties, null, new List<SearchTag>(), buttons ) );
				}
			}
			catch( Exception ex )
			{
				output.Debug.Add( "Error Converting Results", new JObject()
				{
					{ "Message", ex.Message },
					{ "Inner Exception", ex.InnerException?.Message ?? "(None)" }
				} );
			}

			return output;
		}
		//

		public MainSearchResults ConvertCompetencyFrameworkResults( CTDLAPICompetencyFrameworkResultSet results, string searchType, bool useSPARQL = false )
		{
			//var output = new MainSearchResults() { TotalResults = results.TotalResults, SearchType = searchType, RelatedItems = results.RelatedItems };
			var output = new MainSearchResults();
			output.Debug = new JObject();

			try
			{
				output = new MainSearchResults() { TotalResults = results.TotalResults, SearchType = searchType, RelatedItems = new JArray() };
				output.Debug[ "Farthest Step" ] = "Initialization Complete";

				Log( "Output != null: " + ( output != null ? "true" : "false" ) );
				Log( "Total: " + output.TotalResults );
				//Get related triples
				if ( useSPARQL )
				{
					output.Debug[ "Using SPARQL" ] = true;
					var resultDebugs = new List<JObject>();
					if ( results.RelatedItemsMap != null )
					{
						output.RelatedItemsMap = results.RelatedItemsMap;
						output.Debug = results.Debug ?? new JObject();
					}

					foreach ( var result in results.Results )
					{
						var resultDebug = new JObject();
						var parsedResult = new JObject();
						var resultURI = "";

						try
						{
							parsedResult = JObject.Parse( result.RawData );

							resultURI = parsedResult[ "@id" ].ToString();
							resultDebug[ "Result URI" ] = resultURI;
							resultDebug[ "Farthest Step" ] = "Processing Item Map";

							var itemMap = new CTDLAPICompetencyFrameworkResultSetRelatedItemsMapItem();
							var errors = new JArray();
							var mapForResult = results.RelatedItemsMap.FirstOrDefault( m => m[ "ResourceURI" ].ToString() == resultURI );
							if ( mapForResult != null )
							{
								itemMap = mapForResult.ToObject<CTDLAPICompetencyFrameworkResultSetRelatedItemsMapItem>();
							}

							//Owner
							resultDebug[ "Farthest Step" ] = "Processing Creator/Owner/Publisher";
							var creator = LoadResourceData( itemMap.GetRelatedItemsForPath_ExactMatch( "> ceasn:creator > ceterms:Agent" ).URIs.FirstOrDefault() ?? "", results.RelatedItems, errors );
							var publisher = LoadResourceData( itemMap.GetRelatedItemsForPath_ExactMatch( "> ceasn:publisher > ceterms:Agent" ).URIs.FirstOrDefault() ?? "", results.RelatedItems, errors );
							var owner = new List<JObject>() { creator, publisher }.FirstOrDefault( m => m != null );

							//Competencies
							resultDebug[ "Farthest Step" ] = "Processing Competencies";
							var competencyList = itemMap.GetRelatedItemsForPath_ExactMatch( "< ceasn:isPartOf < ceasn:Competency" );
							var competencyURIs = competencyList.URIs;
							var competencyData = GetTopRelatedItems( competencyURIs, results.RelatedItems, errors );
							var competencyTotal = competencyList.TotalURIs;

							//Credentials
							resultDebug[ "Farthest Step" ] = "Processing Credentials";
							var credentialList = itemMap.GetRelatedItemsForPath_EndsWithMatch( "ceterms:Credential" );
							var credentialURIs = credentialList.SelectMany( m => m.URIs ).Distinct().ToList();
							var credentialData = GetTopRelatedItems( credentialURIs, results.RelatedItems, errors );
							var credentialTotal = credentialList.Sum( m => m.TotalURIs );

							//Learning Opportunities
							resultDebug[ "Farthest Step" ] = "Processing Learning Opportunities";
							var learningOpportunityList = itemMap.GetRelatedItemsForPath_EndsWithMatch( "ceterms:LearningOpportunityProfile" );
							var learningOpportunityURIs = learningOpportunityList.SelectMany( m => m.URIs ).Distinct().ToList();
							var learningOpportunityData = GetTopRelatedItems( learningOpportunityURIs, results.RelatedItems, errors );
							var learningOpportunityTotal = learningOpportunityList.Sum( m => m.TotalURIs );

							//Assessments
							resultDebug[ "Farthest Step" ] = "Processing Assessments";
							var assessmentList = itemMap.GetRelatedItemsForPath_EndsWithMatch( "ceterms:AssessmentProfile" );
							var assessmentURIs = assessmentList.SelectMany( m => m.URIs ).Distinct().ToList();
							var assessmentData = GetTopRelatedItems( assessmentURIs, results.RelatedItems, errors );
							var assessmentTotal = assessmentList.Sum( m => m.TotalURIs );

							//Aligned Frameworks
							resultDebug[ "Farthest Step" ] = "Processing Aligned Frameworks";
							var alignedFrameworkList = itemMap.GetRelatedItemsForPath_EndsWithMatch( "ceasn:CompetencyFramework" );
							var alignedFrameworkURIs = alignedFrameworkList.SelectMany( m => m.URIs ).Distinct().ToList();
							var alignedFrameworkData = GetTopRelatedItems( alignedFrameworkURIs, results.RelatedItems, errors );
							var alignedFrameworkTotal = alignedFrameworkList.Sum( m => m.TotalURIs );

							//Aligned Competencies
							resultDebug[ "Farthest Step" ] = "Processing Aligned Competencies";
							var alignedCompetencyList = itemMap.GetRelatedItemsForPath_EndsWithMatch( "ceasn:Competency" ).Where( m => m.Path.ToLower().Contains( "alignment >" ) || m.Path.ToLower().Contains( "alignment <" ) ).ToList();
							var alignedCompetencyURIs = alignedCompetencyList.SelectMany( m => m.URIs ).Distinct().ToList();
							var alignedCompetencyData = GetTopRelatedItems( alignedCompetencyURIs, results.RelatedItems, errors );
							var alignedCompetencyTotal = alignedCompetencyList.Sum( m => m.TotalURIs );

							//Concepts
							resultDebug[ "Farthest Step" ] = "Processing Concepts";
							var conceptList = itemMap.GetRelatedItemsForPath_EndsWithMatch( "skos:Concept" );
							var conceptURIs = conceptList.SelectMany( m => m.URIs ).Distinct().ToList();
							var conceptData = GetTopRelatedItems( conceptURIs, results.RelatedItems, errors );
							var conceptTotal = conceptList.Sum( m => m.TotalURIs );

							//Concept Schemes
							resultDebug[ "Farthest Step" ] = "Processing Concept Schemes";
							var conceptSchemeList = itemMap.GetRelatedItemsForPath_EndsWithMatch( "skos:ConceptScheme" );
							var conceptSchemeURIs = conceptSchemeList.SelectMany( m => m.URIs ).Distinct().ToList();
							var conceptSchemeData = GetTopRelatedItems( conceptSchemeURIs, results.RelatedItems, errors );
							var conceptSchemeTotal = conceptSchemeList.Sum( m => m.TotalURIs );

							resultDebug[ "Farthest Step" ] = "Processing Gray Buttons";

							output.Results.Add( Result( result.Name.ToString(), result.Description.ToString(), -1,
								new Dictionary<string, object>()
								{
									{ "CTID", result.CTID ?? "" },
									//TEMP//{ "CreatorCTID", result.Creator == null ? "" : (result.Creator.FirstOrDefault() ?? "").Split('/').ToList().Last() },
									{ "Locations", new List<object>() { } },
									{ "DateCreated", (result.DateCreated == null || result.DateCreated == DateTime.MinValue) ? "Unknown" : result.DateCreated.ToString("yyyy-MM-dd") },
									{ "DateModified", (result.DateModified == null || result.DateModified == DateTime.MinValue) ? "Unknown" : result.DateModified.ToString("yyyy-MM-dd") },
									//TODO: add framework and competency data to supply ajax calls after rendering
									{ "RawData", result.RawData },
									//{ "PerResultRelatedItems", results.PerResultRelatedItems.FirstOrDefault( m => m.RelatedItemsForCTID == result.CTID ) },
									//{ "Debug", results.Debug },

									//Compatibility with SearchV2
									{ "Name", result.Name.ToString() },
									{ "ctid", result.CTID ?? "" },
									{ "Description", result.Description.ToString() },
									{ "Owner", owner },
									//TEMP//{ "OwnerId", result.Creator == null ? "" : (result.Creator.FirstOrDefault() ?? "").Split('/').ToList().Last() },
									{ "LastUpdated", (result.DateModified == null || result.DateModified == DateTime.MinValue) ? "Unknown" : result.DateModified.ToString("yyyy-MM-dd") },
									{ "SearchType", "competencyframework" },
									{ "RecordId", result.CTID ?? "" },
									{ "UrlTitle", "" },
									{ "Errors", errors }
								},
								null,
								new List<Models.Helpers.SearchTag>()
								{
									//Add related items so they don't have to be calculated client-side
									new Models.Helpers.SearchTag()
									{
										CategoryName = "Competencies",
										CategoryLabel = "Competencies",
										DisplayTemplate = "{#} Competenc{ies}",
										Name = TagTypes.COMPETENCIES.ToString().ToLower(),
										TotalItems = competencyTotal,
										SearchQueryType = "detail",
										Items = competencyData.ConvertAll( m => new Models.Helpers.SearchTagItem()
										{
											Display = CompetencyFrameworkServices.GetEnglish( m["ceasn:competencyText"] ),
											QueryValues = new Dictionary<string, object>() { { "TextValue", CompetencyFrameworkServices.GetEnglish( m[ "ceasn:competencyText" ] ) } }
										} ).ToList()
									},
									new Models.Helpers.SearchTag()
									{
										CategoryName = "Credentials",
										CategoryLabel = "Credentials",
										DisplayTemplate = "{#} Related Credential(s)",
										Name = "credentials",
										TotalItems = credentialTotal,
										SearchQueryType = "link",
										Items = credentialData.ConvertAll( m => new Models.Helpers.SearchTagItem()
										{
											Display = CompetencyFrameworkServices.GetEnglish( m["ceterms:name"] ),
											QueryValues = new Dictionary<string, object>()
											{
												{ "TargetId", m["ceterms:ctid"] }, //Credential ID
												{ "TargetType", "credential" },
												{ "IsReference", false },
											}
										} ).ToList()
									},
									new Models.Helpers.SearchTag()
									{
										CategoryName = "Assessments",
										CategoryLabel = "Assessments",
										DisplayTemplate = "{#} Related Assessment{s}",
										Name = "assessments",
										TotalItems = assessmentTotal,
										SearchQueryType = "link",
										Items = assessmentData.ConvertAll( m => new Models.Helpers.SearchTagItem()
										{
											Display = CompetencyFrameworkServices.GetEnglish( m["ceterms:name"] ),
											QueryValues = new Dictionary<string, object>()
											{
												{ "TargetId", m["ceterms:ctid"] }, //Assessment ID
												{ "TargetType", "assessment" },
												{ "IsReference", false },
											}
										} ).ToList()
									},
									new Models.Helpers.SearchTag()
									{
										CategoryName = "LearningOpportunities",
										CategoryLabel = "Learning Opportunities",
										DisplayTemplate = "{#} Related Learning Opportunit{ies}",
										Name = "learningopportunities",
										TotalItems = learningOpportunityTotal,
										SearchQueryType = "link",
										Items = learningOpportunityData.ConvertAll( m => new Models.Helpers.SearchTagItem()
										{
											Display = CompetencyFrameworkServices.GetEnglish( m["ceterms:name"] ),
											QueryValues = new Dictionary<string, object>()
											{
												{ "TargetId", m["ceterms:ctid"] }, //Learning Opportunity ID
												{ "TargetType", "learningopportunity" },
												{ "IsReference", false },
											}
										} ).ToList()
									},
									new Models.Helpers.SearchTag()
									{
										CategoryName = "AlignedFrameworks",
										CategoryLabel = "Competency Framework Alignments",
										DisplayTemplate = "{#} Competency Framework Alignment{s}",
										Name = "competencyframeworkalignments",
										TotalItems = alignedFrameworkTotal,
										SearchQueryType = "link",
										Items = alignedFrameworkData.ConvertAll( m => new Models.Helpers.SearchTagItem()
										{
											Display = CompetencyFrameworkServices.GetEnglish( m["ceasn:name"] ),
											QueryValues = new Dictionary<string, object>()
											{
												{ "TargetId", m["ceterms:ctid"] }, //Framework ID
												{ "TargetType", "competencyframework" },
												{ "IsReference", false },
											}
										} ).ToList()
									},
									new Models.Helpers.SearchTag()
									{
										CategoryName = "AlignedCompetencies",
										CategoryLabel = "Competency Alignments",
										DisplayTemplate = "{#} Competency Alignment{s}",
										Name = "competencyalignments",
										TotalItems = alignedCompetencyTotal,
										SearchQueryType = "link",
										Items = alignedCompetencyData.ConvertAll( m => new Models.Helpers.SearchTagItem()
										{
											Display = CompetencyFrameworkServices.GetEnglish( m["ceasn:competencyText"] ),
											QueryValues = new Dictionary<string, object>()
											{
												//{ "TargetId", m["ceasn:isPartOf"].FirstOrDefault() }, //Framework ID
												{ "TargetId", "" }, //Framework ID
												{ "TargetType", "competencyframework" },
												{ "IsReference", false },
											}
										} ).ToList()
									},
									new Models.Helpers.SearchTag()
									{
										CategoryName = "ConceptSchemes",
										CategoryLabel = "Concept Schemes",
										DisplayTemplate = "{#} Related Concept Scheme{s}",
										Name = "relatedconceptschemes",
										TotalItems = conceptSchemeTotal,
										SearchQueryType = "detail",
										Items = conceptSchemeData.ConvertAll( m => new Models.Helpers.SearchTagItem()
										{
											Display = CompetencyFrameworkServices.GetEnglish( m["ceasn:name"] ),
											QueryValues = new Dictionary<string, object>() { { "TextValue", m } }
										} ).ToList()
									},
									new Models.Helpers.SearchTag()
									{
										CategoryName = "Concepts",
										CategoryLabel = "Concepts",
										DisplayTemplate = "{#} Related Concept{s}",
										Name = "relatedconcepts",
										TotalItems = conceptTotal,
										SearchQueryType = "detail",
										Items = conceptData.ConvertAll( m => new Models.Helpers.SearchTagItem()
										{
											Display = CompetencyFrameworkServices.GetEnglish( m["skos:prefLabel"] ),
											QueryValues = new Dictionary<string, object>() { { "TextValue", m }, { "RequestURI", m[ "RequestURI" ] } }
										} ).ToList()
									}
								},
								new List<SearchResultButton>() 
								{ 
									new SearchResultButton()
									{
										CategoryLabel = competencyTotal > 1 ? "Competencies" : "Competency",
										CategoryType = ButtonCategoryTypes.Competency.ToString(),
										HandlerType = ButtonHandlerTypes.handler_RenderDetailPageLink.ToString(),
										TotalItems = competencyTotal,
										Items = competencyData.Take(10).ToList().ConvertAll( m => JObject.FromObject( new SearchResultButton.Helpers.DetailPageLink()
										{
											TargetLabel = CompetencyFrameworkServices.GetEnglish( m["ceasn:competencyText"] ),
											TargetType = ButtonSearchTypes.CompetencyFramework.ToString(),
											TargetCTID = result.CTID
										} ) )
									},
									new SearchResultButton()
									{
										CategoryLabel = credentialTotal > 1 ? "Related Credentials" : "Related Credential",
										CategoryType = ButtonCategoryTypes.Credential.ToString(),
										HandlerType = ButtonHandlerTypes.handler_RenderDetailPageLink.ToString(),
										TotalItems = credentialTotal,
										Items = credentialData.Take(10).ToList().ConvertAll( m => JObject.FromObject(new SearchResultButton.Helpers.DetailPageLink()
										{
											TargetLabel = CompetencyFrameworkServices.GetEnglish( m[ "ceterms:name" ] ),
											TargetType = ButtonSearchTypes.Credential.ToString(),
											TargetCTID = (string) m[ "ceterms:ctid" ] ?? ""
										} ) )
									},
									new SearchResultButton()
									{
										CategoryLabel = assessmentTotal > 1 ? "Related Assessments" : "Related Assessment",
										CategoryType = ButtonCategoryTypes.AssessmentProfile.ToString(),
										HandlerType = ButtonHandlerTypes.handler_RenderDetailPageLink.ToString(),
										TotalItems = assessmentTotal,
										Items = assessmentData.Take(10).ToList().ConvertAll( m => JObject.FromObject(new SearchResultButton.Helpers.DetailPageLink()
										{
											TargetLabel = CompetencyFrameworkServices.GetEnglish( m[ "ceterms:name" ] ),
											TargetType = ButtonSearchTypes.AssessmentProfile.ToString(),
											TargetCTID = (string) m[ "ceterms:ctid" ] ?? ""
										} ) )
									},
									new SearchResultButton()
									{
										CategoryLabel = learningOpportunityTotal > 1 ? "Related Learning Opportunities" : "Related Learning Opportunity",
										CategoryType = ButtonCategoryTypes.LearningOpportunityProfile.ToString(),
										HandlerType = ButtonHandlerTypes.handler_RenderDetailPageLink.ToString(),
										TotalItems = learningOpportunityTotal,
										Items = learningOpportunityData.Take(10).ToList().ConvertAll( m => JObject.FromObject(new SearchResultButton.Helpers.DetailPageLink()
										{
											TargetLabel = CompetencyFrameworkServices.GetEnglish( m[ "ceterms:name" ] ),
											TargetType = ButtonSearchTypes.LearningOpportunityProfile.ToString(),
											TargetCTID = (string) m[ "ceterms:ctid" ] ?? ""
										} ) )
									},
									new SearchResultButton()
									{
										CategoryLabel = alignedFrameworkTotal > 1 ? "Framework Alignments" : "Framework Alignment",
										CategoryType = ButtonCategoryTypes.Connection.ToString(),
										HandlerType = ButtonHandlerTypes.handler_RenderDetailPageLink.ToString(),
										TotalItems = alignedFrameworkTotal,
										Items = alignedFrameworkData.Take(10).ToList().ConvertAll( m => JObject.FromObject(new SearchResultButton.Helpers.DetailPageLink()
										{
											TargetLabel = CompetencyFrameworkServices.GetEnglish( m[ "ceasn:name" ] ),
											TargetType = ButtonSearchTypes.CompetencyFramework.ToString(),
											TargetCTID = (string) m[ "ceterms:ctid" ] ?? ""
										} ) )
									},
									new SearchResultButton()
									{
										CategoryLabel = alignedCompetencyTotal > 1 ? "Competency Alignments" : "Competency Alignment",
										CategoryType = ButtonCategoryTypes.Connection.ToString(),
										HandlerType = ButtonHandlerTypes.handler_RenderDetailPageLink.ToString(),
										TotalItems = alignedCompetencyTotal,
										Items = alignedCompetencyData.Take(10).ToList().ConvertAll( m => JObject.FromObject(new SearchResultButton.Helpers.DetailPageLink()
										{
											TargetLabel = CompetencyFrameworkServices.GetEnglish( m[ "ceasn:competencyText" ] ),
											TargetType = ButtonSearchTypes.CompetencyFramework.ToString(),
											TargetCTID = (string) m[ "ceasn:isPartOf" ] ?? ""
										} ) )
									},
									/*
									*/
									new SearchResultButton()
									{
										CategoryLabel = conceptSchemeTotal > 1 ? "Related Concept Schemes" : "Related Concept Scheme",
										CategoryType = ButtonCategoryTypes.Connection.ToString(),
										HandlerType = ButtonHandlerTypes.handler_RenderDetailPageLink.ToString(),
										TotalItems = conceptSchemeTotal,
										Items = conceptSchemeData.Take(10).ToList().ConvertAll( m => JObject.FromObject(new SearchResultButton.Helpers.DetailPageLink()
										{
											TargetLabel = CompetencyFrameworkServices.GetEnglish( m[ "ceasn:name" ] ),
											TargetType = ButtonSearchTypes.CompetencyFramework.ToString(),
											TargetCTID = result.CTID
										} ) )
									},
									new SearchResultButton()
									{
										CategoryLabel = conceptSchemeTotal > 1 ? "Related Concepts" : "Related Concept",
										CategoryType = ButtonCategoryTypes.Connection.ToString(),
										HandlerType = ButtonHandlerTypes.handler_RenderDetailPageLink.ToString(),
										TotalItems = conceptSchemeTotal,
										Items = conceptSchemeData.Take(10).ToList().ConvertAll( m => JObject.FromObject(new SearchResultButton.Helpers.DetailPageLink()
										{
											TargetLabel = CompetencyFrameworkServices.GetEnglish( m[ "skos:prefLabel" ] ),
											TargetType = ButtonSearchTypes.CompetencyFramework.ToString(),
											TargetCTID = result.CTID
										} ) )
									}
								}
							) );
						}
						catch ( Exception ex )
						{
							resultDebugs.Add( resultDebug );
							resultDebug[ "Single Framework Result Conversion Error" ] = new JObject()
							{
								{ "Error", ex.Message },
								{ "URI", resultURI },
								{ "Raw Data", JObject.FromObject( result ) },
								{ "Parsed Result", parsedResult }
							};
						}

					}

					output.Debug = output.Debug ?? new JObject();
					output.Debug[ "Result Debug List" ] = JArray.FromObject( resultDebugs );

				}
				else
				{
					/*
					Log( "Loading gremlin stuff" );
					foreach ( var result in results.Results )
					{
						Log( "Result != null: " + ( result != null ? "true" : "false" ) );
						Log( "PerResultRelatedItems != null: " + ( results.PerResultRelatedItems != null ? "true" : "false" ) );
						Log( "PerResultRelatedItems Count: " + results.PerResultRelatedItems.Count() );
						var relatedItemsForResult = results.PerResultRelatedItems.FirstOrDefault( m => m.RelatedItemsForCTID == result.CTID );
						output.Results.Add( Result( result.Name.ToString(), result.Description.ToString(), -1,
							new Dictionary<string, object>()
							{
						{ "CTID", result.CTID ?? "" },
						{ "CreatorCTID", result.Creator == null ? "" : (result.Creator.FirstOrDefault() ?? "").Split('/').ToList().Last() },
						{ "Locations", new List<object>() { } },
						{ "DateCreated", result.DateCreated == null || result.DateCreated == DateTime.MinValue ? "Unknown" : result.DateCreated.ToString("yyyy-MM-dd") },
						{ "DateModified", result.DateModified == null || result.DateModified == DateTime.MinValue ? "Unknown" : result.DateModified.ToString("yyyy-MM-dd") },
						//TODO: add framework and competency data to supply ajax calls after rendering
						{ "RawData", result.RawData },
						{ "PerResultRelatedItems", results.PerResultRelatedItems.FirstOrDefault( m => m.RelatedItemsForCTID == result.CTID ) },
						{ "Debug", results.Debug },

						//Compatibility with SearchV2
						{ "Name", result.Name.ToString() },
						{ "ctid", result.CTID ?? "" },
						{ "Description", result.Description.ToString() },
						{ "Owner", relatedItemsForResult.Owners },
						{ "OwnerId", result.Creator == null ? "" : (result.Creator.FirstOrDefault() ?? "").Split('/').ToList().Last() },
						{ "LastUpdated", result.DateModified == null || result.DateModified == DateTime.MinValue ? "Unknown" : "Unknown; Last Updated: " + result.DateModified.ToString("yyyy-MM-dd") },
						{ "SearchType", "competencyframework" },
						{ "RecordId", result.CTID ?? "" },
						{ "UrlTitle", "" }
							},
							null,
							new List<Models.Helpers.SearchTag>()
							{
						//Add related items so they don't have to be calculated client-side
						new Models.Helpers.SearchTag()
						{
							CategoryName = "Competencies",
							CategoryLabel = "Competencies",
							DisplayTemplate = "{#} Competenc{ies}",
							Name = TagTypes.COMPETENCIES.ToString().ToLower(),
							TotalItems = relatedItemsForResult.Competencies.TotalItems,
							SearchQueryType = "detail",
							Items = relatedItemsForResult.Competencies.Samples.ConvertAll( m => new Models.Helpers.SearchTagItem()
							{
								Display = CompetencyFrameworkServices.GetEnglish( m["ceasn:competencyText"] ),
								QueryValues = new Dictionary<string, object>() { { "TextValue", m } }
							} ).ToList()
						},
						new Models.Helpers.SearchTag()
						{
							CategoryName = "Credentials",
							CategoryLabel = "Credentials",
							DisplayTemplate = "{#} Related Credential(s)",
							Name = "credentials",
							TotalItems = relatedItemsForResult.Credentials.TotalItems,
							SearchQueryType = "link",
							Items = relatedItemsForResult.Credentials.Samples.ConvertAll( m => new Models.Helpers.SearchTagItem()
							{
								Display = CompetencyFrameworkServices.GetEnglish( m["ceterms:name"] ),
								QueryValues = new Dictionary<string, object>()
								{
									{ "TargetId", m["ceterms:ctid"] }, //Credential ID
									{ "TargetType", "credential" },
									{ "IsReference", "false" },
								}
							} ).ToList()
						},
						new Models.Helpers.SearchTag()
						{
							CategoryName = "Assessments",
							CategoryLabel = "Assessments",
							DisplayTemplate = "{#} Related Assessment{s}",
							Name = "assessments",
							TotalItems = relatedItemsForResult.Assessments.TotalItems,
							SearchQueryType = "link",
							Items = relatedItemsForResult.Assessments.Samples.ConvertAll( m => new Models.Helpers.SearchTagItem()
							{
								Display = CompetencyFrameworkServices.GetEnglish( m["ceterms:name"] ),
								QueryValues = new Dictionary<string, object>()
								{
									{ "TargetId", m["ceterms:ctid"] }, //Assessment ID
									{ "TargetType", "assessment" },
									{ "IsReference", "false" },
								}
							} ).ToList()
						},
						new Models.Helpers.SearchTag()
						{
							CategoryName = "LearningOpportunities",
							CategoryLabel = "Learning Opportunities",
							DisplayTemplate = "{#} Related Learning Opportunit{ies}",
							Name = "learningopportunities",
							TotalItems = relatedItemsForResult.LearningOpportunities.TotalItems,
							SearchQueryType = "link",
							Items = relatedItemsForResult.LearningOpportunities.Samples.ConvertAll( m => new Models.Helpers.SearchTagItem()
							{
								Display = CompetencyFrameworkServices.GetEnglish( m["ceterms:name"] ),
								QueryValues = new Dictionary<string, object>()
								{
									{ "TargetId", m["ceterms:ctid"] }, //Learning Opportunity ID
									{ "TargetType", "learningopportunity" },
									{ "IsReference", "false" },
								}
							} ).ToList()
						},
						new Models.Helpers.SearchTag()
						{
							CategoryName = "AlignedFrameworks",
							CategoryLabel = "Competency Framework Alignments",
							DisplayTemplate = "{#} Competency Framework Alignment{s}",
							Name = TagTypes.CONNECTIONS.ToString().ToLower(),
							TotalItems = relatedItemsForResult.AlignedFrameworks.TotalItems,
							SearchQueryType = "link",
							Items = relatedItemsForResult.AlignedFrameworks.Samples.ConvertAll( m => new Models.Helpers.SearchTagItem()
							{
								Display = CompetencyFrameworkServices.GetEnglish( m["ceasn:name"] ),
								QueryValues = new Dictionary<string, object>()
								{
									{ "TargetId", m["ceterms:ctid"] }, //Framework ID
									{ "TargetType", "competencyframework" },
									{ "IsReference", "false" },
								}
							} ).ToList()
						},
						new Models.Helpers.SearchTag()
						{
							CategoryName = "AlignedCompetencies",
							CategoryLabel = "Competency Alignments",
							DisplayTemplate = "{#} Competency Alignment{s}",
							Name = TagTypes.CONNECTIONS.ToString().ToLower(),
							TotalItems = relatedItemsForResult.AlignedCompetencies.TotalItems,
							SearchQueryType = "link",
							Items = relatedItemsForResult.AlignedCompetencies.Samples.ConvertAll( m => new Models.Helpers.SearchTagItem()
							{
								Display = CompetencyFrameworkServices.GetEnglish( m["ceasn:competencyText"] ),
								QueryValues = new Dictionary<string, object>()
								{
									{ "TargetId", m["ceasn:isPartOf"].FirstOrDefault() }, //Framework ID
									{ "TargetType", "competencyframework" },
									{ "IsReference", "false" },
								}
							} ).ToList()
						},
						new Models.Helpers.SearchTag()
						{
							CategoryName = "ConceptSchemes",
							CategoryLabel = "Concept Schemes",
							DisplayTemplate = "{#} Related Concept Scheme{s}",
							Name = TagTypes.CONNECTIONS.ToString().ToLower(),
							TotalItems = relatedItemsForResult.ConceptSchemes.TotalItems,
							SearchQueryType = "detail",
							Items = relatedItemsForResult.ConceptSchemes.Samples.ConvertAll( m => new Models.Helpers.SearchTagItem()
							{
								Display = CompetencyFrameworkServices.GetEnglish( m["ceasn:name"] ),
								QueryValues = new Dictionary<string, object>() { { "TextValue", m } }
							} ).ToList()
						},
						new Models.Helpers.SearchTag()
						{
							CategoryName = "Concepts",
							CategoryLabel = "Concepts",
							DisplayTemplate = "{#} Related Concept{s}",
							Name = TagTypes.CONNECTIONS.ToString().ToLower(),
							TotalItems = relatedItemsForResult.Concepts.TotalItems,
							SearchQueryType = "detail",
							Items = relatedItemsForResult.Concepts.Samples.ConvertAll( m => new Models.Helpers.SearchTagItem()
							{
								Display = CompetencyFrameworkServices.GetEnglish( m["skos:prefLabel"] ),
								QueryValues = new Dictionary<string, object>() { { "TextValue", m } }
							} ).ToList()
						}
							}
						) );
					}
					*/
				}

			}
			catch ( Exception ex )
			{
				output.Debug[ "Error Converting Competency Framework Results" ] = ex.Message;
			}


			return output;
		}
		//

		public MainSearchResults ConvertCompetencyFrameworkResults2( List<CompetencyFrameworkSummary> results, int totalResults, string searchType )
		{
			var output = new MainSearchResults() { 
				TotalResults = totalResults, 
				SearchType = searchType
			};
			try
			{
				foreach ( var item in results )
				{
					if ( item.TotalCompetencies > 0 )
					{

					}
					output.Results.Add( Result( item.Name, item.FriendlyName, item.Description, item.Id,
						new Dictionary<string, object>()
						{
						{ "Name", item.Name },
						{ "Description", item.Description },
						{ "Owner", string.IsNullOrWhiteSpace( item.OrganizationName ) ? "" : item.OrganizationName },
						{ "OwnerId", item.OrganizationId },
						{ "OwnerCTID", item.OrganizationCTID },
						{ "SearchType", searchType },
						{ "RecordId", item.Id },
						{ "ctid", item.CTID },
						{ "UrlTitle", item.FriendlyName },
						{ "ResultNumber", item.ResultNumber },
						{ "Created", item.Created.ToShortDateString() },
						{ "LastUpdated", item.LastUpdated.ToShortDateString() }
						},
						null,
						new List<Models.Helpers.SearchTag>()
						{
							//Competencies - will be contained in CF
							new Models.Helpers.SearchTag()
							{
								CategoryName = "Competencies",
								CategoryLabel = "Competency",
								DisplayTemplate = "Has {#} Competenc{ies}",
								Name = TagTypes.COMPETENCIES.ToString().ToLower(),
								TotalItems = item.TotalCompetencies,
								SearchQueryType = "text",
								Items = item.Competencies.Take(10).ToList().ConvertAll( m => new Models.Helpers.SearchTagItem() { Display = m.Name, QueryValues = new Dictionary<string, object>() { { "TextValue", m.Name } } } )
							},
							//Related credentials
							//new Models.Helpers.SearchTag()
							//{
							//	CategoryName = "Credentials",
							//	DisplayTemplate = "{#} Credential(s)",
							//	Name = "credentials", //The CSS on the search page will look for an icon associated with this
							//	TotalItems = item.ReferencedByCredentials, //Replace this with the count
							//	SearchQueryType = "text",
							//	IsAjaxQuery = true,
							//	AjaxQueryName = "", //Query to call to get the related assessments (reference "Costs" below for usage)
							//	AjaxQueryValues = new Dictionary<string, object>() //Values to pass to the above query. Probably need to change what's in here to make it work.
							//	{
							//		{ "SearchType", "cf" },
							//		{ "RecordId", item.Id },
							//		{ "TargetEntityType", "credentials" }
							//	}
							//},
							//Related Assessments
							//new Models.Helpers.SearchTag()
							//{
							//	CategoryName = "Assessments",
							//	DisplayTemplate = "{#} Assessment{s}",
							//	Name = "assessments", //The CSS on the search page will look for an icon associated with this
							//	TotalItems = item.ReferencedByAssessments, //Replace this with the count
							//	SearchQueryType = "text",
							//	IsAjaxQuery = true,
							//	AjaxQueryName = "", //Query to call to get the related assessments (reference "Costs" below for usage)
							//	AjaxQueryValues = new Dictionary<string, object>() //Values to pass to the above query. Probably need to change what's in here to make it work.
							//	{
							//		{ "SearchType", "cf" },
							//		{ "RecordId", item.Id },
							//		{ "TargetEntityType", "assessments" }
							//	}
							//},
							//Related Learning Opportunities
							//new Models.Helpers.SearchTag()
							//{
							//	CategoryName = "LearningOpportunities",
							//	DisplayTemplate = "{#} Learning Opportunit{ies}",
							//	Name = "learningOpportunities", //The CSS on the search page will look for an icon associated with this
							//	TotalItems = item.ReferencedByLearningOpportunities, //Replace this with the count
							//	SearchQueryType = "text",
							//	IsAjaxQuery = true,
							//	AjaxQueryName = "", //Query to call to get the related learning opportunities (reference "Costs" below for usage)
							//	AjaxQueryValues = new Dictionary<string, object>() //Values to pass to the above query. Probably need to change what's in here to make it work.
							//	{
							//		{ "SearchType", "credential" },
							//		{ "RecordId", item.Id },
							//		{ "TargetEntityType", "learningOpportunities" }
							//	}
							//}
						}
					) );
				}
			} catch (Exception ex)
			{
				LoggingHelper.DoTrace( 1, "SearchServices. ConvertCompetencyFrameworkResults2" + ex.Message );
			}
			return output;
		}
		//
		public MainSearchResults ConvertConceptSchemeResults( List<ConceptSchemeSummary> results, int totalResults, string searchType )
		{
			var output = new MainSearchResults() { TotalResults = totalResults, SearchType = searchType };
			foreach ( var item in results )
			{

				string entityLastUpdated = "";
				if ( item.EntityLastUpdated > checkDate )
					entityLastUpdated = item.EntityLastUpdated.ToString( "yyyy-MM-dd HH:mm" );

				output.Results.Add( Result( item.Name, item.FriendlyName, item.Description, item.Id,
					new Dictionary<string, object>()
					{
							{ "Name", item.Name },
							{ "Description", item.Description ?? "" },
							{ "Owner", string.IsNullOrWhiteSpace( item.OrganizationName ) ? "" : item.OrganizationName },
							{ "OwnerId", item.OwningOrganizationId },
							{ "OwnerCTID", item.PrimaryOrganizationCTID },
							{ "ctid", item.CTID },
							{ "SearchType", searchType },
							{ "RecordId", item.Id },
							{ "ResultNumber", item.ResultNumber },
							{ "UrlTitle", item.FriendlyName },
							{ "Created", item.Created.ToShortDateString() },
							{ "LastUpdated", entityLastUpdated }
					},
						null,

						null

				) );
			}
			return output;
		}
		//

		//
		public MainSearchResults ConvertGeneralIndexResults( List<CommonSearchSummary> results, int totalResults, string searchType, string ctdlTypeLabel )
		{
			var broadType = ctdlTypeLabel.Replace( " ", "" );
			var ctdlType = "ceterms:" + broadType;
			var output = new MainSearchResults() { TotalResults = totalResults, SearchType = searchType };
			foreach ( var item in results )
			{
				var subjects = Deduplicate( item.Subjects );

				output.Results.Add( Result( item.Name, item.FriendlyName, item.Description, item.Id,
					new Dictionary<string, object>()
					{
						{ "Name", item.Name },
						{ "Description", item.Description },
						{ "Owner", string.IsNullOrWhiteSpace( item.PrimaryOrganizationName ) ? "" : item.PrimaryOrganizationName },
						{ "OwnerId", item.PrimaryOrganizationId },
						{ "OwnerCTID", item.PrimaryOrganizationCTID },
						{ "SearchType", searchType },
						{ "RecordId", item.Id },
						{ "ResultNumber", item.ResultNumber },
						{ "ctid", item.CTID },
						{ "UrlTitle", item.FriendlyName },
						{ "OrganizationUrlTitle", item.PrimaryOrganizationFriendlyName },
						{ "Created", item.Created.ToShortDateString() },
						{ "LastUpdated", item.LastUpdated.ToShortDateString() },
						{ "T_LastUpdated", item.LastUpdated.ToString("yyyy-MM-dd HH:mm:ss") },
						{ "T_StateId", item.EntityStateId },
						{ "T_BroadType", broadType },
						{ "T_CTDLType", ctdlType },
						{ "T_CTDLTypeLabel", ctdlTypeLabel }
					},
					null,
					null,
					new List<SearchResultButton>()
					{
						//TVP
						//related creds
						new SearchResultButton()
						{
							CategoryLabel = item.TransferValueForCredentialsCount + item.TransferValueFromCredentialsCount == 1 ? "Related Credential" : "Related Credentials",
							CategoryType = ButtonCategoryTypes.TVPHasCredential.ToString(),
							HandlerType = ButtonHandlerTypes.handler_GetRelatedItemsViaAJAX.ToString(),
							TotalItems = item.TransferValueForCredentialsCount + item.TransferValueFromCredentialsCount,
							RenderData = JObject.FromObject( new SearchResultButton.Helpers.AjaxDataForResult()
							{
								AjaxQueryName = "GetSearchResultCredential",
								TargetEntityType = ButtonTargetEntityTypes.TVPHasCredential.ToString()
							} )
						},
						//related asmts
						new SearchResultButton()
						{
							CategoryLabel = item.TransferValueForAssessmentsCount + item.TransferValueFromAssessmentsCount == 1 ? "Related Assessment" : "Related Assessments",
							CategoryType = ButtonCategoryTypes.TVPHasAssessment.ToString(),
							HandlerType = ButtonHandlerTypes.handler_GetRelatedItemsViaAJAX.ToString(),
							TotalItems = item.TransferValueForAssessmentsCount + item.TransferValueFromAssessmentsCount,
							RenderData = JObject.FromObject( new SearchResultButton.Helpers.AjaxDataForResult()
							{
								AjaxQueryName = "GetSearchResultAsmt",
								TargetEntityType = ButtonTargetEntityTypes.TVPHasAssessment.ToString()
							} )
						},
						//related lopps
						new SearchResultButton()
						{
							CategoryLabel = item.TransferValueForLoppsCount + item.TransferValueFromLoppsCount == 1 ? "Related Learning Opportunity" : "Related Learning Opportunities",
							CategoryType = ButtonCategoryTypes.TVPHasLopp.ToString(),
							HandlerType = ButtonHandlerTypes.handler_GetRelatedItemsViaAJAX.ToString(),
							TotalItems = item.TransferValueForLoppsCount + item.TransferValueFromLoppsCount,
							RenderData = JObject.FromObject( new SearchResultButton.Helpers.AjaxDataForResult()
							{
								AjaxQueryName = "GetSearchResultLopp",
								TargetEntityType = ButtonTargetEntityTypes.TVPHasLearningOpportunity.ToString()
							} )
						},
						//Subjects
						new SearchResultButton()
						{
							CategoryLabel = subjects.Count() == 1 ? "Subject" : "Subjects",
							CategoryType = ButtonCategoryTypes.Subject.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderDetailPageLink.ToString(),
							TotalItems = subjects.Count(),
							Items = subjects.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( new SearchResultButton.Helpers.DetailPageLink()
							{
								TargetLabel = m,
								TargetType = ButtonSearchTypes.Pathway.ToString(),
								TargetId = item.Id
							} ) )
						},

						//Occupations
						new SearchResultButton()
						{
							CategoryLabel = item.OccupationResults.Results.Count() == 1 ? "Occupation" : "Occupations",
							CategoryType = ButtonCategoryTypes.OccupationType.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderExternalCodeFilter.ToString(),
							TotalItems = item.OccupationResults.Results.Count(),
							Items = item.OccupationResults.Results.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( GetFilterItem( m, item.OccupationResults.CategoryId ) ) )
						},
						//Industries
						new SearchResultButton()
						{
							CategoryLabel = item.IndustryResults.Results.Count() == 1 ? "Industry" : "Industries",
							CategoryType = ButtonCategoryTypes.IndustryType.ToString(),
							HandlerType = ButtonHandlerTypes.handler_RenderExternalCodeFilter.ToString(),
							TotalItems = item.IndustryResults.Results.Count(),
							Items = item.IndustryResults.Results.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( GetFilterItem( m, item.IndustryResults.CategoryId ) ) )
						},
						new SearchResultButton()
						{
							CategoryLabel = "Pathways",
							CategoryType = ButtonCategoryTypes.Pathway.ToString(),
							HandlerType = ButtonHandlerTypes.handler_GetRelatedItemsViaAJAX.ToString(),
							TotalItems = item.PathwaysCount,
							RenderData = JObject.FromObject( new SearchResultButton.Helpers.AjaxDataForResult()
							{
								AjaxQueryName = "GetSearchResultPathway",
								TargetEntityType = ButtonTargetEntityTypes.Pathway.ToString()
							} )
						},
					}
				) );
			}
			return output;
		}
		//
		//public MainSearchResults ConvertPathwaySetResults( List<PathwaySetSummary> results, int totalResults, string searchType )
		//{
		//	var output = new MainSearchResults() { TotalResults = totalResults, SearchType = searchType };
		//	foreach ( var item in results )
		//	{

		//		output.Results.Add( Result( item.Name, item.Description, item.Id,
		//			new Dictionary<string, object>()
		//			{
		//				{ "Name", item.Name },
		//				{ "Description", item.Description },
		//				{ "Owner", string.IsNullOrWhiteSpace( item.OrganizationName ) ? "" : item.OrganizationName },
		//				{ "OwnerId", item.OwningOrganizationId },
		//				{ "OwnerCTID", item.PrimaryOrganizationCTID },
		//				{ "SearchType", searchType },
		//				{ "RecordId", item.Id },
		//				{ "ResultNumber", item.ResultNumber },
		//				{ "ctid", item.CTID },
		//				{ "UrlTitle", item.FriendlyName },
		//				 { "Created", item.Created.ToShortDateString() },
		//				{ "LastUpdated", item.LastUpdated.ToShortDateString() },
		//				{ "T_LastUpdated", item.LastUpdated.ToString("yyyy-MM-dd HH:mm:ss") },
		//				{ "T_StateId", item.EntityStateId },
		//				{ "T_BroadType", "PathwaySet" },
		//				{ "T_CTDLType", "ceterms:PathwaySet" },
		//				{ "T_CTDLTypeLabel", "Pathway Set" }
		//			},
		//			null, null,
		//			new List<Models.Helpers.SearchResultButton>()
		//			{
		//				new SearchResultButton()
		//				{
		//					CategoryLabel = "Pathways",
		//					CategoryType = ButtonCategoryTypes.Pathway.ToString(),
		//					HandlerType = ButtonHandlerTypes.handler_GetRelatedItemsViaAJAX.ToString(),
		//					TotalItems = item.Pathways.Count,
		//					RenderData = JObject.FromObject( new SearchResultButton.Helpers.AjaxDataForResult()
		//					{
		//						AjaxQueryName = "GetSearchResultPathway",
		//						TargetEntityType = ButtonTargetEntityTypes.Pathway.ToString()
		//					} )
		//				},
		//				//new Models.Helpers.SearchResultButton()
		//				//{
		//				//	CategoryLabel = "Pathways",
		//				//	DisplayTemplate = "{#} Pathway{s}",
		//				//	Name = "pathways", //The CSS on the search page will look for an icon associated with this
		//				//	TotalItems = item.Pathways.Count(),
		//				//	SearchQueryType = "link",
		//				//	Items = item.Pathways.ConvertAll( m => new Models.Helpers.SearchTagItem()
		//				//		{
		//				//			Display = m.Name,
		//				//			QueryValues = new Dictionary<string, object>()
		//				//			{
		//				//				{ "TargetId", m.Id }, 
		//				//				{ "TargetType", "pathway" },
		//				//				{ "IsReference", "false" },
		//				//			}
		//				//		} ).ToList()
		//				//}
		//			}
		//		) );
		//	}
		//	return output;
		//}
		//use ConvertGeneralIndexResults
		//public MainSearchResults ConvertTransferValueResults( List<CommonSearchSummary> results, int totalResults, string searchType )
		//{
		//	var output = new MainSearchResults() { TotalResults = totalResults, SearchType = searchType };
		//	foreach ( var item in results )
		//	{

		//		output.Results.Add( Result( item.Name, item.Description, item.Id,
		//			new Dictionary<string, object>()
		//			{
		//				{ "Name", item.Name },
		//				{ "Description", item.Description },
		//				{ "Owner", string.IsNullOrWhiteSpace( item.PrimaryOrganizationName ) ? "" : item.PrimaryOrganizationName },
		//				{ "OwnerId", item.PrimaryOrganizationId },
		//				{ "OwnerCTID", item.PrimaryOrganizationCTID },
		//				{ "SearchType", searchType },
		//				{ "RecordId", item.Id },
		//				{ "ResultNumber", item.ResultNumber },
		//				{ "ctid", item.CTID },
		//				{ "UrlTitle", item.FriendlyName },
		//				 { "Created", item.Created.ToShortDateString() },
		//				{ "LastUpdated", item.LastUpdated.ToShortDateString() },
		//				{ "T_LastUpdated", item.LastUpdated.ToString("yyyy-MM-dd HH:mm:ss") },
		//				{ "T_StateId", item.EntityStateId },
		//				{ "T_BroadType", "TransferValueProfile" },
		//				{ "T_CTDLType", "ceterms:TransferValueProfile" },
		//				{ "T_CTDLTypeLabel", "Transfer Value Profile" }
		//			},
		//			null,null,
		//			new List<SearchResultButton>()
		//			{
		//				//TVP
		//				//related creds
		//				new SearchResultButton()
		//				{
		//					CategoryLabel = item.TransferValueForCredentialsCount + item.TransferValueFromCredentialsCount == 1 ? "Related Credential" : "Related Credentials",
		//					CategoryType = ButtonCategoryTypes.Credential.ToString(),
		//					HandlerType = ButtonHandlerTypes.handler_GetRelatedItemsViaAJAX.ToString(),
		//					TotalItems = item.TransferValueForCredentialsCount + item.TransferValueFromCredentialsCount,
		//					RenderData = JObject.FromObject( new SearchResultButton.Helpers.AjaxDataForResult()
		//					{
		//						AjaxQueryName = "GetSearchResultCredential",
		//						TargetEntityType = ButtonTargetEntityTypes.Credential.ToString()
		//					} )
		//				},
		//				//related asmts
		//				new SearchResultButton()
		//				{
		//					CategoryLabel = item.TransferValueForAssessmentsCount + item.TransferValueFromAssessmentsCount == 1 ? "Related Assessment" : "Related Assessments",
		//					CategoryType = ButtonCategoryTypes.AssessmentProfile.ToString(),
		//					HandlerType = ButtonHandlerTypes.handler_GetRelatedItemsViaAJAX.ToString(),
		//					TotalItems = item.TransferValueForAssessmentsCount + item.TransferValueFromAssessmentsCount,
		//					RenderData = JObject.FromObject( new SearchResultButton.Helpers.AjaxDataForResult()
		//					{
		//						AjaxQueryName = "GetSearchResultAsmt",
		//						TargetEntityType = ButtonTargetEntityTypes.Assessment.ToString()
		//					} )
		//				},
		//				//related lopps
		//				new SearchResultButton()
		//				{
		//					CategoryLabel = item.TransferValueForLoppsCount + item.TransferValueFromLoppsCount == 1 ? "Related Learning Opportunity" : "Learning Opportunities",
		//					CategoryType = ButtonCategoryTypes.LearningOpportunityProfile.ToString(),
		//					HandlerType = ButtonHandlerTypes.handler_GetRelatedItemsViaAJAX.ToString(),
		//					TotalItems = item.TransferValueForLoppsCount + item.TransferValueFromLoppsCount,
		//					RenderData = JObject.FromObject( new SearchResultButton.Helpers.AjaxDataForResult()
		//					{
		//						AjaxQueryName = "GetSearchResultLopp",
		//						TargetEntityType = ButtonTargetEntityTypes.LearningOpportunity.ToString()
		//					} )
		//				},

		//				//Occupations
		//				new SearchResultButton()
		//				{
		//					CategoryLabel = item.OccupationResults.Results.Count() == 1 ? "Occupation" : "Occupations",
		//					CategoryType = ButtonCategoryTypes.OccupationType.ToString(),
		//					HandlerType = ButtonHandlerTypes.handler_RenderExternalCodeFilter.ToString(),
		//					TotalItems = item.OccupationResults.Results.Count(),
		//					Items = item.OccupationResults.Results.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( GetFilterItem( m, item.OccupationResults.CategoryId ) ) )
		//				},
		//				//Industries
		//				new SearchResultButton()
		//				{
		//					CategoryLabel = item.IndustryResults.Results.Count() == 1 ? "Industry" : "Industries",
		//					CategoryType = ButtonCategoryTypes.IndustryType.ToString(),
		//					HandlerType = ButtonHandlerTypes.handler_RenderExternalCodeFilter.ToString(),
		//					TotalItems = item.IndustryResults.Results.Count(),
		//					Items = item.IndustryResults.Results.Take( 10 ).ToList().ConvertAll( m => JObject.FromObject( GetFilterItem( m, item.IndustryResults.CategoryId ) ) )
		//				}
		//			}
		//		) );
		//	}
		//	return output;
		//}
		//
		public MainSearchResults ConvertDataSetProfileResults( List<DataSetProfile> results, int totalResults, string searchType )
		{
			var output = new MainSearchResults() { TotalResults = totalResults, SearchType = searchType };
			foreach ( var item in results )
			{

				output.Results.Add( Result( item.Name, item.Description, item.Id,
					new Dictionary<string, object>()
					{
						{ "Name", item.Name },
						{ "Description", item.Description },
						{ "Owner", string.IsNullOrWhiteSpace( item.DataProviderName ) ? "" : item.DataProviderName },
						{ "OwnerId", item.DataProviderId },
						{ "OwnerCTID", item.DataProviderCTID },
						{ "SearchType", searchType },
						{ "RecordId", item.Id },
						{ "ResultNumber", item.ResultNumber },
						{ "ctid", item.CTID },
						//{ "UrlTitle", item.FriendlyName },
						 { "Created", item.Created.ToShortDateString() },
						{ "LastUpdated", item.LastUpdated.ToShortDateString() },
						{ "T_LastUpdated", item.LastUpdated.ToString("yyyy-MM-dd HH:mm:ss") },
						{ "T_StateId", item.EntityStateId },
						{ "T_BroadType", "DataSetProfile" },
						{ "T_CTDLType", "qdata:DataSetProfile" },
						{ "T_CTDLTypeLabel", "DataSet Profile" }
					},
					null,
					null
				) );
			}
			return output;
		}
		//

		public MainSearchResults ConvertCollectionResults( List<CommonSearchSummary> results, int totalResults, string searchType )
		{
			var broadType = "Collection";
			var ctdlTypeLabel = "Collection";
			var ctdlType = "ceterms:Collection";
			var output = new MainSearchResults() { TotalResults = totalResults, SearchType = searchType };
			foreach( var item in results )
			{
				output.Results.Add( Result( item.Name, item.Description, item.Id, 
					new Dictionary<string, object>()
					{
						{ "Name", item.Name },
						{ "Description", item.Description },
						{ "Owner", string.IsNullOrWhiteSpace( item.PrimaryOrganizationName ) ? "" : item.PrimaryOrganizationName },
						{ "OwnerId", item.PrimaryOrganizationId },
						{ "OwnerCTID", item.PrimaryOrganizationCTID },
						{ "SearchType", searchType },
						{ "RecordId", item.Id },
						{ "ResultNumber", item.ResultNumber },
						{ "ctid", item.CTID },
						{ "UrlTitle", item.FriendlyName },
						{ "OrganizationUrlTitle", item.PrimaryOrganizationFriendlyName },
						{ "Created", item.Created.ToShortDateString() },
						{ "LastUpdated", item.LastUpdated.ToShortDateString() },
						{ "T_LastUpdated", item.LastUpdated.ToString("yyyy-MM-dd HH:mm:ss") },
						{ "T_StateId", item.EntityStateId },
						{ "T_BroadType", broadType },
						{ "T_CTDLType", ctdlType },
						{ "T_CTDLTypeLabel", ctdlTypeLabel }
					},
					null,
					null
				) );
			}

			return output;
		}
		//

		public static Entity GetEntityByCTID(string ctid)
		{
			var entityCache = CF.EntityManager.EntityCacheGetByCTID( ctid );

			var entity = new Entity();
			if ( entityCache?.Id > 0 )
            {
				entity = new Entity()
				{
					Id = entityCache.Id,
					EntityType = entityCache.EntityType,
					EntityTypeId = entityCache.EntityTypeId,
					EntityUid = entityCache.EntityUid,
					EntityBaseId = entityCache.BaseId,
					EntityBaseName = entityCache.Name,
				};
				return entity;
            }
			//otherwise check individually
			//TODO - need to improve use of entity_cache. HINT: just add/update as part of the import!
			//	Others: 
			//	- CF Competency
			//	- Collection Competency
			//TEMP, try credential
			var cred = CredentialManager.GetMinimumByCtid( ctid );
			if ( cred != null && cred.Id > 0 )
			{
				entity = CF.EntityManager.GetEntity( cred.RowId, false );
				return entity;
			}
			//	- CF Competency
			var cfCompetency = CompetencyFrameworkCompetencyManager.GetByCtid( ctid );
			if ( cfCompetency != null && cfCompetency.Id > 0 )
			{
				//no entity for competency
				//entity = CF.EntityManager.GetEntity( cfCompetency.RowId, false );
				entity = new Entity()
				{
					Id = cfCompetency.Id,
					EntityType = "Competency",
					EntityTypeId = 0,// entityCache.EntityTypeId, //none at this time
					EntityUid = cfCompetency.RowId,
					EntityBaseId = cfCompetency.Id,
					EntityBaseName = cfCompetency.CompetencyText,
				};
				return entity;
			}
			var org = OrganizationManager.GetSummaryByCtid( ctid );
			if ( org != null && org.Id > 0 )
			{
				entity = CF.EntityManager.GetEntity( org.RowId, false );
				return entity;
			}
		
			var asmt = AssessmentManager.GetSummaryByCtid( ctid );
			if ( asmt != null && asmt.Id > 0 )
			{
				entity = CF.EntityManager.GetEntity( asmt.RowId, false );
				return entity;
			}
				var lopp = LearningOpportunityManager.GetByCtid( ctid );
			if ( lopp != null && lopp.Id > 0 )
			{
				entity = EntityManager.GetEntity( lopp.RowId, false );
				return entity;
			}
			var cf = CompetencyFrameworkManager.GetByCtid( ctid );
			if ( cf != null && cf.Id > 0 )
			{
				entity = CF.EntityManager.GetEntity( cf.RowId, false );
				return entity;
			}


			return entity;
		}
	


		public Dictionary<string, string> ConvertCompetenciesToDictionary(List<CredentialAlignmentObjectProfile> input)
		{
			var result = new Dictionary<string, string>();
			if (input != null)
			{
				foreach (var item in input)
				{
					try
					{
						result.Add(item.Id.ToString(), item.Description);
					}
					catch { }
				}
			}
			return result;
		}
		public MainSearchResult Result(string name, string description, int recordID, Dictionary<string, object> properties, List<TagSet> tags, List<Models.Helpers.SearchTag> tagsV2 = null, List<Models.Helpers.SearchResultButton> buttons = null )
		{
			return new MainSearchResult()
			{
				Name = string.IsNullOrWhiteSpace(name) ? "No name" : name,
				Description = string.IsNullOrWhiteSpace(description) ? "No description" : description,
				RecordId = recordID,
				Properties = properties == null ? new Dictionary<string, object>() : properties,
				Tags = tags == null ? new List<TagSet>() : tags,
				TagsV2 = tagsV2 ?? new List<Models.Helpers.SearchTag>(),
				Buttons = (buttons ?? new List<Models.Helpers.SearchResultButton>() ).Where( m => m.TotalItems > 0 ).ToList()
			};
		}
		//
		public MainSearchResult Result(string name, string friendlyName, string description, int recordID, Dictionary<string, object> properties, List<TagSet> tags, List<Models.Helpers.SearchTag> tagsV2 = null, List<Models.Helpers.SearchResultButton> buttons = null )
		{
			return new MainSearchResult()
			{
				Name = string.IsNullOrWhiteSpace(name) ? "No name" : name,
				FriendlyName = string.IsNullOrWhiteSpace(friendlyName) ? "Record" : friendlyName,
				Description = string.IsNullOrWhiteSpace(description) ? "No description" : description,
				RecordId = recordID,
				Properties = properties == null ? new Dictionary<string, object>() : properties,
				Tags = tags == null ? new List<TagSet>() : tags,
				TagsV2 = tagsV2 ?? new List<Models.Helpers.SearchTag>(),
				Buttons = ( buttons ?? new List<Models.Helpers.SearchResultButton>() ).Where( m => m.TotalItems > 0 ).ToList()
			};
		}
		//
		public Dictionary<string, string> ConvertCodeItemsToDictionary(List<CodeItem> input)
		{
			var result = new Dictionary<string, string>();
			foreach (var item in input)
			{
				try
				{
					result.Add(item.Code, item.Name);
				}
				catch { }
			}
			return result;
		}
		//

		public List<Dictionary<string, object>> ConvertAddresses(List<Address> input)
		{
			var result = new List<Dictionary<string, object>>();
			foreach (var item in input)
			{
				try
				{
					var data = new Dictionary<string, object>()
					{
						{ "Latitude", item.Latitude },
						{ "Longitude", item.Longitude },
						{ "Address", item.DisplayAddress() }
					};
					result.Add(data);
				}
				catch { }
			}
			return result;
		}
		//

		public JArray ConvertAddressesToJArray(List<Address> input )
		{
			try
			{
				return JArray.FromObject( input );
			}
			catch
			{
				return new JArray();
			}
		}
		//

		public static List<string> Autocomplete_Subjects( MainSearchInput query, int entityTypeId, int categoryId, string keyword, int maxTerms = 25)
		{
			var list = new List<string>();
			list = CF.Entity_ReferenceManager.QuickSearch_Subjects( query, entityTypeId, keyword, maxTerms);
			return list;
		}

		public static List<string> Autocomplete_Occupations( MainSearchInput query, int entityTypeId, string keyword, int maxTerms = 25)
		{

			return CF.Entity_ReferenceManager.QuickSearch_ReferenceFrameworks( query, entityTypeId, 11, "", keyword, maxTerms);
		}
		public static List<string> Autocomplete_OccupationsOLD( MainSearchInput query, int entityTypeId, string keyword, int maxTerms = 25 )
		{
			return CF.Entity_ReferenceManager.QuickSearch_ReferenceFrameworks( query, entityTypeId, 11, "", keyword, maxTerms );
		}
		public static List<string> Autocomplete_Industries( MainSearchInput query, int entityTypeId, string keyword, int maxTerms = 25)
		{
			return CF.Entity_ReferenceManager.QuickSearch_ReferenceFrameworks( query, entityTypeId, 10, "", keyword, maxTerms);
		}
		public static List<string> Autocomplete_Cip( MainSearchInput query, int entityTypeId, string keyword, int maxTerms = 25)
		{
			return CF.Entity_ReferenceManager.QuickSearch_ReferenceFrameworks( query, entityTypeId, 23, "", keyword, maxTerms);
		}


		#region Common filters
		public static void HandleCustomFilters(MainSearchInput data, int searchCategory, ref string where)
		{
			string AND = "";
			//may want custom category for each one, to prevent requests that don't match the current search

			string sql = "";

			//Updated to use FilterV2
			if (where.Length > 0)
			{
				AND = " AND ";
			}
			foreach (var filter in data.FiltersV2.Where(m => m.Type == MainSearchFilterV2Types.CODE).ToList())
			{
				var item = filter.AsCodeItem();
				if (item.CategoryId != searchCategory)
				{
					continue;
				}

				sql = GetPropertySql(item.Id);
				if (string.IsNullOrWhiteSpace(sql) == false)
				{
					where = where + AND + sql;
					AND = " AND ";
				}
			}
			if (sql.Length > 0)
			{
				LoggingHelper.DoTrace(6, "SearchServices.HandleCustomFilters. result: \r\n" + where);
			}

			/* //Retained for reference
			foreach ( MainSearchFilter filter in data.Filters.Where( s => s.CategoryId == searchCategory ) )
			{
				//each item will be a custom sql 
				//the propertyId will differ in env, so can't use it for lookup in web.config. Could get from db, and cache
				if ( where.Length > 0 )
					AND = " AND ";
				int id = 0;
				foreach ( string item in filter.Items )
				{
					if (Int32.TryParse(item, out id)) 
					{
						sql = GetPropertySql( id );
						if ( string.IsNullOrWhiteSpace( sql ) == false )
						{
							where = where + AND + sql;
							AND = " AND ";
						}
					}
				}
				
			}
			if ( sql.Length > 0 )
			{
				LoggingHelper.DoTrace( 6, "SearchServices.HandleCustomFilters. result: \r\n" + where );
			}
			*/
		}
		public static string GetPropertySql(int id)
		{
			string sql = "";
			string key = "propertySql_" + id.ToString();
			//check cache for vocabulary
			if (HttpRuntime.Cache[key] != null)
			{
				sql = (string)HttpRuntime.Cache[key];
				return sql;
			}

			CodeItem item = CF.CodesManager.Codes_PropertyValue_Get(id);
			if (item != null && (item.Description ?? "").Length > 5)
			{
				sql = item.Description;
				HttpRuntime.Cache.Insert(key, sql);
			}

			return sql;
		}

		public static void SetSubjectsFilter(MainSearchInput data, int entityTypeId, ref string where)
		{
			string subjects = "  (base.RowId in (SELECT EntityUid FROM [Entity_Subjects] a where EntityTypeId = {0} AND {1} )) ";
			//RowId is same as EntityUid
			//if ( data.SearchType == "credential" )
			//    subjects = subjects.Replace( "base.RowId", "base.EntityUid" );

			string frameworkItems = " OR (RowId in (SELECT EntityUid FROM [dbo].[Entity_Reference_Summary] a where CategoryId= 23 AND {0} ) ) ";

			string phraseTemplate = " (a.Subject like '{0}') ";
			string titleTemplate = " (a.TextValue like '{0}') ";

			string AND = "";
			string OR = "";
			// string keyword = "";

			//Updated to use FilterV2
			string next = "";
			string fnext = "";
			if (where.Length > 0)
			{
				AND = " AND ";
			}

			foreach (var filter in data.FiltersV2.Where(m => m.Name == "subjects"))
			{
				var text = ServiceHelper.HandleApostrophes(filter.AsText());
				if (string.IsNullOrWhiteSpace(text))
				{
					continue;
				}

				next += OR + string.Format(phraseTemplate, SearchifyWord(text));
				fnext += OR + string.Format(titleTemplate, SearchifyWord(text));
				OR = " OR ";
			}
			string fsubject = "";
			if (!string.IsNullOrWhiteSpace(fnext)
				&& (entityTypeId == 3 || entityTypeId == 7))
			{
				fsubject = string.Format(frameworkItems, fnext);
			}
			if (!string.IsNullOrWhiteSpace(next))
			{
				where = where + AND + " ( " + string.Format(subjects, entityTypeId, next) + fsubject + ")";
			}

			/* //Retained for reference
			foreach ( MainSearchFilter filter in data.Filters.Where( s => s.Name == "subjects" ) )
			{
				string next = "";
				if ( where.Length > 0 )
					AND = " AND ";
				foreach ( string item in filter.Texts )
				{
					keyword = ServiceHelper.HandleApostrophes( item );
					if ( keyword.IndexOf( ";" ) > -1 )
					{
						var words = keyword.Split( ';' );
						foreach ( string word in words )
						{
							next += OR + string.Format( phraseTemplate, PrepWord( word) );
							OR = " OR ";
						}
					}
					else
					{
						next = string.Format( phraseTemplate, PrepWord( keyword ) );
					}
					//next += keyword;	//					+",";
					//just handle one for now
					break;
				}
				//next = next.Trim( ',' );
				if ( !string.IsNullOrWhiteSpace( next ) )
					where = where + AND + string.Format( subjects, entityTypeId, next );

				break;
			}
			*/
		}
		/// <summary>
		/// May want to make configurable, in case don't want to always perform check.
		/// </summary>
		/// <param name="word"></param>
		/// <returns></returns>
		public static string SearchifyWord(string word, bool forElasticSearch = true)
		{
			string keyword = word.Trim() + "^";


			//if ( keyword.ToLower().LastIndexOf( "es^" ) > 4 )
			//{
			//	//may be too loose
			//	//keyword = keyword.Substring( 0, keyword.ToLower().LastIndexOf( "es" ) );
			//}
			//else 
			if (keyword.ToLower().LastIndexOf("s^") > 4)
			{
				keyword = keyword.Substring(0, keyword.ToLower().LastIndexOf("s"));
			}

			if (keyword.ToLower().LastIndexOf("ing^") > 3)
			{
				keyword = keyword.Substring(0, keyword.ToLower().LastIndexOf("ing^"));
			}
			else if (keyword.ToLower().LastIndexOf("ed^") > 4)
			{
				keyword = keyword.Substring(0, keyword.ToLower().LastIndexOf("ed^"));
			}
			else if (keyword.ToLower().LastIndexOf("ion^") > 3)
			{
				keyword = keyword.Substring(0, keyword.ToLower().LastIndexOf("ion^"));
			}
			else if (keyword.ToLower().LastIndexOf("ive^") > 3)
			{
				keyword = keyword.Substring(0, keyword.ToLower().LastIndexOf("ive^"));
			}

			//Ohhh BAD - assumes all or none
			if ( forElasticSearch && UtilityManager.GetAppKeyValue("usingElasticCredentialSearch", false))
			{
				var env = UtilityManager.GetAppKeyValue("environment");
				//not sure of this
				if (env != "production" && keyword.IndexOf("*") == -1)
				{
					//keyword = "*" + keyword.Trim() + "*";
					//keyword = keyword.Replace( "&", "*" ).Replace( " and ", "*" ).Replace( " in ", "*" ).Replace( " of ", "*" ).Replace( " for ", "*" ).Replace( " with ", "*" );
					//keyword = keyword.Replace( " from ", "*" );
					//keyword = keyword.Replace( " a ", "*" );
					//keyword = keyword.Replace( " - ", "*" );
					//keyword = keyword.Replace( " * ", "*" );

					////just replace all spaces with *?
					//keyword = keyword.Replace( "  ", "*" );
					//keyword = keyword.Replace( " ", "*" );
					//keyword = keyword.Replace( "**", "*" );
				}
			}
			else if (keyword.IndexOf("%") == -1)
			{
				keyword = "%" + keyword.Trim() + "%";
				keyword = keyword.Replace("&", "%").Replace(" and ", "%").Replace(" in ", "%").Replace(" of ", "%").Replace(" for ", "%").Replace(" with ", "%");
				keyword = keyword.Replace(" from ", "%");
				keyword = keyword.Replace(" a ", "%");
				keyword = keyword.Replace(" - ", "%");
				keyword = keyword.Replace(" % ", "%");

				//just replace all spaces with %?
				keyword = keyword.Replace("  ", "%");
				keyword = keyword.Replace(" ", "%");
				keyword = keyword.Replace("%%", "%");
			}


			keyword = keyword.Replace("^", "");
			return keyword;
		}

		public static void SetPropertiesFilter(MainSearchInput data, int entityTypeId, string searchCategories, ref string where)
		{
			string AND = "";
			string template = " ( base.Id in ( SELECT  [EntityBaseId] FROM [dbo].[EntityProperty_Summary] where EntityTypeId= {0} AND [PropertyValueId] in ({1}))) ";
			int prevCategoryId = 0;

			//Updated to use FiltersV2
			string next = "";
			if (where.Length > 0)
				AND = " AND ";
			foreach (var filter in data.FiltersV2.Where(m => m.Type == MainSearchFilterV2Types.CODE).ToList())
			{
				var item = filter.AsCodeItem();
				if (searchCategories.Contains(item.CategoryId.ToString()))
				{
					if (item.CategoryId != prevCategoryId)
					{
						if (prevCategoryId > 0)
						{
							next = next.Trim(',');
							where = where + AND + string.Format(template, entityTypeId, next);
							AND = " AND ";
						}
						prevCategoryId = item.CategoryId;
						next = "";
					}
					next += item.Id + ",";
				}
			}
			next = next.Trim(',');
			if (!string.IsNullOrWhiteSpace(next))
			{
				where = where + AND + string.Format(template, entityTypeId, next);
			}

		}
		public static void SetBoundariesFilter(MainSearchInput data, ref string where)
		{
			string AND = "";
			if (where.Length > 0)
				AND = " AND ";
			string template = " ( base.RowId in ( SELECT  b.EntityUid FROM [dbo].[Entity.Address] a inner join Entity b on a.EntityId = b.Id    where [Longitude] < {0} and [Longitude] > {1} and [Latitude] < {2} and [Latitude] > {3} ) ) ";
			if (data.SearchType == "credential")
				template = template.Replace("base.RowId", "base.EntityUid");

			var boundaries = SearchServices.GetBoundaries(data, "bounds");
			if (boundaries.IsDefined)
			{
				where = where + AND + string.Format(template, boundaries.East, boundaries.West, boundaries.North, boundaries.South);
			}
		}
		//
		public static void SetRolesFilter(MainSearchInput data, ref string where)
		{
			string AND = "";
			if (where.Length > 0)
				AND = " AND ";
			string template = " ( base.RowId in ( SELECT distinct b.EntityUid FROM [dbo].[Entity.AgentRelationship] a inner join Entity b on a.EntityId = b.Id   where [RelationshipTypeId] in ({0})   ) ) ";

			//if ( data.SearchType == "credential" )
			//    template = template.Replace( "base.RowId", "base.EntityUid" );

			//Updated to use FiltersV2
			string next = "";
			if (where.Length > 0)
				AND = " AND ";
			if ( data.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CUSTOM ) )
			{
				var roles = new List<CodeItem>();
				//will only be one owner
				var orgId = 0;
				string template2 = " ( base.RowId in ( SELECT distinct b.EntityUid FROM [dbo].[Entity.AgentRelationship] a inner join Entity b on a.EntityId = b.Id inner join Organization c on a.AgentUid = c.RowId  where c.Id = {0} AND [RelationshipTypeId] in ({1})   ) ) ";
				foreach ( var filter in data.FiltersV2.Where( m => m.Name == "organizationroles" ).ToList() )
				{
					var cc = filter.AsOrgRolesItem();
					orgId = cc.Id;
					roles.Add( filter.AsOrgRolesItem() );
					break;
				}
				if ( orgId > 0 && roles != null && roles.Count == 1 )
				{
					var relationnships = roles.Select( s => s.Id ).ToList();
					var r2 = roles[ 0 ].IdsList;

					next = String.Join( ", ", r2.ToArray() );
					next = next.Trim( ',' );
					if ( !string.IsNullOrWhiteSpace( next ) )
					{
						where = where + AND + string.Format( template2, orgId, next );
					}
				}
			} else if ( data.FiltersV2.Any( x => x.Type == MainSearchFilterV2Types.CODE ) )
			{
				foreach ( var filter in data.FiltersV2.Where( m => m.Type == MainSearchFilterV2Types.CODE ) )
				{
					var item = filter.AsCodeItem();
					if ( item.CategoryId == 13 )
					{
						next += item.Id + ",";
					}
				}
				next = next.Trim( ',' );
				if ( !string.IsNullOrWhiteSpace( next ) )
				{
					where = where + AND + string.Format( template, next );
				}
			}
			


			/* //Retained for reference
			foreach ( MainSearchFilter filter in data.Filters.Where( s => s.CategoryId == 13  ) )
			{
				string next = "";
				if ( where.Length > 0 )
					AND = " AND ";
				foreach ( string item in filter.Items )
				{
					next += item + ",";
				}
				next = next.Trim( ',' );
				where = where + AND + string.Format( template, next );
			}
			*/
		}


		public static void SetOrgRolesFilter(MainSearchInput data, ref string where)
		{
			string AND = "";
			if (where.Length > 0)
				AND = " AND ";
			string template = " ( base.RowId in ( SELECT distinct EntityUid FROM [dbo].[Entity.AgentRelationship] a inner join Entity b on a.EntityId = b.Id   where [RelationshipTypeId] in ({0})   ) ) ";

			foreach (MainSearchFilter filter in data.Filters.Where(s => s.CategoryId == 13))
			{
				string next = "";
				if (where.Length > 0)
					AND = " AND ";
				foreach (string item in filter.Items)
				{
					next += item + ",";
				}
				next = next.Trim(',');
				where = where + AND + string.Format(template, next);
			}
		}


		#endregion

		#region FILTERS FOR NEW FINDER
		//public static FilterResponse GetCredentialFilters( bool getAll = false )
		//{
		//	EnumerationServices enumServices = new EnumerationServices();
		//	int entityTypeId = 1;
		//	string searchType = "credential";
		//	string label = "credential";
		//	string labelPlural= "credentials";
		//	string key = searchType + "_filters";

		//	FilterResponse filters = new FilterResponse( searchType );
		//	if ( IsFilterAvailableFromCache( searchType, ref filters ) )
		//	{
		//		//return filters;
		//	}
		//	try
		//	{
		//		var audienceTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, 14, entityTypeId, getAll );
		//		var audienceLevelTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, 4, entityTypeId, getAll );
		//		var credentialTypes = enumServices.GetCredentialType( workIT.Models.Common.EnumerationType.MULTI_SELECT, getAll );
		//		var credStatusTypes = enumServices.GetCredentialStatusType( workIT.Models.Common.EnumerationType.MULTI_SELECT, getAll );
		//		var credAsmtDeliveryTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, 18, entityTypeId, getAll );
		//		var credLoppDeliveryTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, 21, entityTypeId, getAll );
		//		var connections = enumServices.GetCredentialConnectionsFilters( EnumerationType.MULTI_SELECT, getAll );
		//		var languages = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, 65, entityTypeId, getAll );
		//		var qaReceived = enumServices.GetEntityAgentQAActions( EnumerationType.MULTI_SELECT, entityTypeId, getAll );
		//		var otherFilters = enumServices.EntityStatisticGetEnumeration( entityTypeId, EnumerationType.MULTI_SELECT, getAll );
		//		//frameworks


		//		/* TODO
		//		 * handle history
		//		 */
		//		var filter = new MSR.Filter();
		//		filters.Filters = new List<Filter>();

		//		//
		//		//competencies don't have an autocomplete
		//		var history = new MSR.Filter( "History" )
		//		{
		//			Label = "History",
		//			Description = string.Format( "Use Last Updated and Created dates to search for {0}.", labelPlural )
		//		};
		//		history.Items.Add( new MSR.FilterItem()
		//		{
		//			Description = "Last Updated",
		//			Label = "From",
		//			URI = "lastUpdatedFrom",
		//			InterfaceType = APIFilter.FilterItem_TextValue
		//		} );
		//		history.Items.Add( new MSR.FilterItem()
		//		{
		//			Label = "To",
		//			URI = "lastUpdatedTo",
		//			InterfaceType = APIFilter.FilterItem_TextValue
		//		} );
		//		history.Items.Add( new MSR.FilterItem()
		//		{
		//			Description = "Created",
		//			Label = "From",
		//			URI = "createdFrom",
		//			InterfaceType = APIFilter.FilterItem_TextValue
		//		} );
		//		history.Items.Add( new MSR.FilterItem()
		//		{
		//			Label = "To",
		//			URI = "createdTo",
		//			InterfaceType = APIFilter.FilterItem_TextValue
		//		} );
		//		filters.Filters.Add( history );


		//		//
		//		if ( credentialTypes != null && credentialTypes.Items.Any() )
		//		{
		//			filter = ConvertEnumeration( "CredentialType", credentialTypes );
		//			filters.Filters.Add( filter );
		//		}
		//		//=====QA
		//		if ( qaReceived != null && qaReceived.Items.Any() )
		//		{
		//			filter = ConvertEnumeration( "Quality Assurance", qaReceived, string.Format( "Select the type(s) of quality assurance to filter relevant {0}.", labelPlural ) );
		//			//just in case
		//			filter.Label = "Quality Assurance";
		//			filter.URI = "filter:Organization";
		//			//filter = new MSR.Filter()
		//			//{
		//			//	Label = "Quality Assurance",
		//			//	URI = "filter:Organization",
		//			//	//CategoryId = qaReceived.Id,
		//			//	Description = string.Format("Select the type(s) of quality assurance to filter relevant {0}.", labelPlural),
		//			//	MicroSearchGuidance = string.Format("Optionally, find and select one or more organizations that perform {0} Quality Assurance.", label),
		//			//	SearchType = searchType,
		//			//	SearchTypeContext = "organizations",
		//			//};
		//			filter.Items.Add( new MSR.FilterItem()
		//			{
		//				Label = string.Format( "Optionally, find and select one or more organizations that perform {0} Quality Assurance.", label ),
		//				URI = "interfaceType:TextValue",
		//				InterfaceType = APIFilter.InterfaceType_Autocomplete
		//			} );
		//			//filters.Filters.Add( ConvertEnumeration( filter, qaReceived ) );
		//			filters.Filters.Add( filter );

		//		}

		//		//
		//		if ( audienceLevelTypes != null && audienceLevelTypes.Items.Any() )
		//		{
		//			filter = ConvertEnumeration( "AudienceLevelType", audienceLevelTypes, "Select the type of level(s) indicating a point in a progression through an educational or training context." );
		//			filters.Filters.Add( filter );
		//		}

		//		if ( audienceTypes != null && audienceTypes.Items.Any() )
		//		{
		//			filter = ConvertEnumeration( "AudienceType", audienceTypes, string.Format( "Select the applicable audience types that are the target of {0}. Note that many {0} will not have specific audience types.", labelPlural, labelPlural ) );
		//			filters.Filters.Add( filter );
		//		}
		//		//
		//		if ( connections != null && connections.Items.Any() )
		//		{
		//			filter = ConvertEnumeration( "CredentialConnection", connections, string.Format( "Select the connection type(s) to filter {0}:", labelPlural ) );
		//			filters.Filters.Add( filter );
		//		}
		//		//competencies don't have an autocomplete
		//		var competencies = new MSR.Filter( "Competencies" )
		//		{
		//			Label = "Competencies",
		//			Description = string.Format( "Select 'Has Competencies' to search for {0} with any competencies.", labelPlural ),
		//			//MicroSearchGuidance = string.Format( "Enter a term(s) to show {0} with relevant competencies and click Add.", labelPlural ),
		//			//HasAny = new FilterItem()
		//			//{
		//			//	Label = "Has Competencies (any type)",
		//			//	Value = "credReport:HasCompetencies"
		//			//}
		//		};
		//		competencies.Items.Add( new MSR.FilterItem()
		//		{
		//			//Description = string.Format( "Select 'Has Competencies' to search for {0} with any competencies.", labelPlural ),
		//			Label = "Has Competencies (any type)",
		//			URI = "credReport:HasCompetencies",
		//			InterfaceType = APIFilter.InterfaceType_Checkbox
		//		} );
		//		competencies.Items.Add( new MSR.FilterItem()
		//		{
		//			Label = string.Format( "Enter a term(s) to show {0} with relevant competencies and click Add.", labelPlural ),
		//			URI = "interfaceType:TextValue",
		//			InterfaceType = APIFilter.InterfaceType_Text
		//		} );
		//		filters.Filters.Add( competencies );

		//		//======================================
		//		filter = new MSR.Filter( "Subjects" )
		//		{
		//			Label = "Subject Areas",
		//			Description = "",
		//		};
		//		filter.Items.Add( new MSR.FilterItem()
		//		{
		//			Description = string.Format( "Enter a term(s) to show {0} with relevant subjects.", labelPlural ),
		//			URI = APIFilter.FilterItem_TextValue,
		//			InterfaceType = APIFilter.InterfaceType_Autocomplete
		//		} );
		//		filters.Filters.Add( filter );

		//		//======================================
		//		filter = new MSR.Filter()
		//		{
		//			Label = "Occupations",
		//			URI = "filter:OccupationType",
		//			Description = string.Format( "Select 'Has Occupations' to search for {0} with any occupations.", labelPlural ),
		//		};
		//		filter.Items.Add( new MSR.FilterItem()
		//		{
		//			Label = "Has Occupations",
		//			URI = "credReport:HasOccupations",
		//			InterfaceType = APIFilter.InterfaceType_Checkbox
		//		} );
		//		filter.Items.Add( new MSR.FilterItem()
		//		{
		//			Label = string.Format( "Find and select the occupations to filter relevant {0}.", labelPlural ),
		//			URI = "interfaceType:TextValue",
		//			InterfaceType = APIFilter.InterfaceType_Autocomplete
		//		} );
		//		filters.Filters.Add( filter );

		//		//======================================
		//		//
		//		filter = new MSR.Filter()
		//		{
		//			Label = "Industries",
		//			URI = "filter:IndustryType",
		//			Description = string.Format( "Select 'Has Industries' to search for {0} with any industries.", labelPlural ),
		//		};
		//		filter.Items.Add( new MSR.FilterItem()
		//		{
		//			Label = "Has Industries",
		//			URI = "credReport:HasIndustries",
		//			InterfaceType = APIFilter.InterfaceType_Checkbox
		//		} );
		//		filter.Items.Add( new MSR.FilterItem()
		//		{
		//			Label = string.Format( "Find and select the industries to filter relevant {0}.", labelPlural ),
		//			URI = "interfaceType:TextValue",
		//			InterfaceType = APIFilter.InterfaceType_Autocomplete
		//		} );
		//		filters.Filters.Add( filter );

		//		//======================================
		//		filter = new MSR.Filter()
		//		{
		//			Label = "Instructional Programs",
		//			URI = "filter:InstructionalProgramType",
		//			Description = string.Format( "Select 'Has Instructional Programs' to search for {0} with any instructional program classifications.", labelPlural ),
		//		};
		//		filter.Items.Add( new MSR.FilterItem()
		//		{
		//			Label = "Has Instructional Programs",
		//			URI = "credReport:HasCIP",
		//			InterfaceType = APIFilter.InterfaceType_Checkbox
		//		} );
		//		filter.Items.Add( new MSR.FilterItem()
		//		{
		//			Label = string.Format( "Find and select the instructional programs to filter relevant {0}.", labelPlural ),
		//			URI = "interfaceType:TextValue",
		//			InterfaceType = APIFilter.InterfaceType_Autocomplete
		//		} );
		//		filters.Filters.Add( filter );


		//		//
		//		if ( credAsmtDeliveryTypes != null && credAsmtDeliveryTypes.Items.Any() )
		//		{
		//			filter = ConvertEnumeration( "AssessmentDeliveryType", credAsmtDeliveryTypes, "Select the type of Assessment delivery method(s)." );
		//			filters.Filters.Add( filter );
		//		}
		//		if ( credLoppDeliveryTypes != null && credLoppDeliveryTypes.Items.Any() ) 
		//		{
		//			filter = ConvertEnumeration( "LearningDeliveryType", credLoppDeliveryTypes, "Select the type of Learning Opportunity delivery method(s)." );
		//			filters.Filters.Add( filter );
		//		}

		//		//
		//		if ( credStatusTypes != null && credStatusTypes.Items.Any() )
		//		{
		//			filter = ConvertEnumeration( "CredentialStatusType", credStatusTypes, "Select one or more status to display credentials for those Credential Status" );
		//			filters.Filters.Add( filter );
		//		}

		//		//
		//		if ( languages != null && languages.Items.Any() )
		//		{
		//			filter = ConvertEnumeration( "InLanguage", languages, string.Format( "Select one or more languages to display {0} for those languages.", labelPlural ) );
		//			filters.Filters.Add( filter );
		//		}


		//		//
		//		if ( otherFilters != null && otherFilters.Items.Any() )
		//		{
		//			filter = ConvertEnumeration( "OtherFilters", otherFilters, string.Format( "Select one of the 'Other' filters that are available. Note these filters are independent (ORs). For example selecting 'Has Cost Profile(s)' and 'Has Financial Aid' will show {0} that have cost profile(s) OR financial assistance.", labelPlural ) );
		//			filters.Filters.Add( filter );
		//		}
		//		//
		//		AddFilterToCache( filters );
		//	} catch(Exception ex)
		//	{
		//		LoggingHelper.DoTrace( 1, string.Format( "GetCredentialFilters. {0}", ex.Message ) );
		//	}
		//	return filters;
		//}
		//public static FilterQueryOld GetOrganizationFilters( bool getAll = false )
		//{
		//	EnumerationServices enumServices = new EnumerationServices();
		//	int entityTypeId = 2;
		//	string searchType = "organization";
		//	string label = "organization";
		//	string labelPlural = "organizations";
		//	FilterQueryOld filters = new FilterQueryOld( searchType );
		//	//FilterResponse filters = new FilterResponse( searchType );

		//	try
		//	{
		//		var orgServiceTypes = enumServices.GetOrganizationServices( EnumerationType.MULTI_SELECT, getAll );
		//		var orgTypes = enumServices.GetOrganizationServices( EnumerationType.MULTI_SELECT, getAll );
		//		var orgSectorTypes = enumServices.GetOrganizationServices( EnumerationType.MULTI_SELECT, getAll );
		//		var claimTypes = enumServices.GetEnumeration( "claimType", EnumerationType.SINGLE_SELECT, getAll );

		//		var qaReceived = enumServices.GetEntityAgentQAActions( EnumerationType.MULTI_SELECT, entityTypeId, getAll );
		//		var qaPerformed = enumServices.GetEntityAgentQAActions( EnumerationType.MULTI_SELECT, entityTypeId, getAll, true );
		//		var otherFilters = enumServices.EntityStatisticGetEnumeration( entityTypeId, EnumerationType.MULTI_SELECT, getAll );


		//		if ( orgServiceTypes != null && orgServiceTypes.Items.Any() )
		//			filters.OrganizationServiceTypes = ConvertEnumeration( "servicetypes", orgServiceTypes, "Select a service(s) offered by an organization." );
		//		//
		//		if ( orgTypes != null && orgTypes.Items.Any() )
		//			filters.OrganizationTypes = ConvertEnumeration( "organizationtypes", orgTypes, "Select the organization type(s)" );
		//		if ( orgSectorTypes != null && orgSectorTypes.Items.Any() )
		//			filters.OrganizationSectorTypes = ConvertEnumeration( "sectortypes", orgSectorTypes, "Select the type of sector for an organization." );
		//		if ( claimTypes != null && claimTypes.Items.Any() )
		//			filters.VerificationClaimTypes = ConvertEnumeration( "claimTypes", claimTypes, "Select the type of claim for an organization." );
		//		//
		//		//
		//		filters.Industries = new MSR.Filter()
		//		{
		//			Label = "Industries",
		//			URI = "industries",
		//			HasAnyGuidance = string.Format( "Select 'Has Industries' to search for {0} with any industries.", labelPlural ),
		//			MicroSearchGuidance = string.Format( "Find and select the industries to filter relevant {0}.", labelPlural ),
		//			SearchType = searchType,
		//			SearchTypeContext = "industries",
		//			HasAny = new FilterItem()
		//			{
		//				Label = "Has Industries",
		//				URI = "orgReport:HasIndustries"
		//			}
		//		};
		//		//
		//		if ( qaReceived != null && qaReceived.Items.Any() )
		//		{
		//			var f = new MSR.Filter()
		//			{
		//				Label = "Quality Assurance",
		//				URI = "qualityAssurance",
		//				//CategoryId = qaReceived.Id,
		//				Description = "Select the type(s) of quality assurance to filter relevant organizations.",
		//				MicroSearchGuidance = "Optionally, find and select one or more organizations that perform organization Quality Assurance."
		//			};
		//			filters.QualityAssurance = ConvertEnumeration( f, qaReceived );
		//		}

		//		if ( qaPerformed != null && qaPerformed.Items.Any() )
		//		{
		//			var f = new MSR.Filter()
		//			{
		//				Label = "Quality Assurance Performed",
		//				URI = "qualityAssurancePerformed",
		//				//CategoryId = qaPerformed.Id,
		//				Description = "Select one or more types of quality assurance to display organizations that have performed the selected types of assurance.",
		//			};
		//			filters.QualityAssurancePerformed = ConvertEnumeration( f, qaPerformed );
		//		}
		//	}
		//	catch ( Exception ex )
		//	{
		//		LoggingHelper.DoTrace( 1, string.Format( "GetOrganizationFilters. {0}", ex.Message ) );
		//	}
		//	return filters;
		//}
		//public static FilterQueryOld GetAssessmentFilters( bool getAll = false )
		//{
		//	EnumerationServices enumServices = new EnumerationServices();
		//	int entityTypeId = 3;
		//	string searchType = "assessment";
		//	string label = "assessment";
		//	string labelPlural = "assessments";
		//	FilterQueryOld filters = new FilterQueryOld( searchType );

		//	try
		//	{
		//		var asmtMethodTypes = enumServices.GetEnumeration( "assessmentMethodType", EnumerationType.MULTI_SELECT, false, getAll );
		//		var asmtUseTypes = enumServices.GetEnumeration( "assessmentUse", EnumerationType.MULTI_SELECT, false, getAll );
		//		var audienceLevelTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, 4, entityTypeId, getAll );
		//		var audienceTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, 14, entityTypeId, getAll );
		//		var connections = enumServices.GetAssessmentsConditionProfileTypes( EnumerationType.MULTI_SELECT, getAll );

		//		//
		//		var deliveryTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE, entityTypeId, getAll );
		//		var languages = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, 65, entityTypeId, getAll );
		//		var qaReceived = enumServices.GetEntityAgentQAActions( EnumerationType.MULTI_SELECT, entityTypeId, getAll );
		//		var otherFilters = enumServices.EntityStatisticGetEnumeration( entityTypeId, EnumerationType.MULTI_SELECT, getAll );

		//		//
		//		var scoringMethodTypes = enumServices.GetEnumeration( "scoringMethod", EnumerationType.MULTI_SELECT, false, getAll );
		//		/* TODO
		//		 * 
		//		 * QA received accredited, approved, recognized, regulated 
		//		 * other filters
		//		 * Has any?
		//		 *	industry, occupation, 
		//		 * Connections
		//		 * - ispartof, is prep, etc
		//		 */

		//		if ( asmtMethodTypes != null && asmtMethodTypes.Items.Any() )
		//			filters.AssessmentMethodTypes = ConvertEnumeration( "assessmentMethodType", asmtMethodTypes, "Select the assessment method(s) type." );
		//		if ( asmtUseTypes != null && asmtUseTypes.Items.Any() )
		//			filters.AssessmentUseTypes = ConvertEnumeration( "assessmentUse", asmtUseTypes, "Select the type(s) of assessment uses." );
		//		if ( deliveryTypes != null && deliveryTypes.Items.Any() )
		//			filters.AssessmentDeliveryTypes = ConvertEnumeration( "assessmentdeliverytypes", deliveryTypes, "Select the type of delivery method(s)." );

		//		//
		//		if ( audienceLevelTypes != null && audienceLevelTypes.Items.Any() )
		//			filters.AudienceLevels = ConvertEnumeration( "audienceleveltypes", audienceLevelTypes, "Select the type of level(s) indicating a point in a progression through an educational or training context." );
		//		if ( audienceTypes != null && audienceTypes.Items.Any() )
		//			filters.AudienceTypes = ConvertEnumeration( "Audiencetypes", audienceTypes, "Select the applicable audience types that are the target of assessments. Note that many assessments will not have specific audience types." );
		//		//
		//		if ( connections != null && connections.Items.Any() )
		//			filters.Connections = ConvertEnumeration( "connections", connections, string.Format( "Select the connection type(s) to filter {0}:", labelPlural ) );
		//		//
		//		if ( languages != null && languages.Items.Any() )
		//			filters.Languages = ConvertEnumeration( "languages", languages, string.Format( "Select one or more languages to display {0} for those languages.", labelPlural ) );
		//		if ( scoringMethodTypes != null && scoringMethodTypes.Items.Any() )
		//			filters.ScoringMethodTypes = ConvertEnumeration( "scoringMethod", scoringMethodTypes, "Select the type of scoring method(s)." );
		//		//
		//		if ( otherFilters != null && otherFilters.Items.Any() )
		//			filters.OtherFilters = ConvertEnumeration( "otherFilters", otherFilters, string.Format( "Select one of the 'Other' filters that are available. Note these filters are independent (ORs). For example selecting 'Has Cost Profile(s)' and 'Has Financial Aid' will show {0} that have cost profile(s) OR financial assistance.", labelPlural ) );
		//		//
		//		//competencies don't have an autocomplete
		//		filters.Competencies = new MSR.Filter()
		//		{
		//			Label = "Competencies",
		//			URI = "competencies",
		//			HasAnyGuidance = string.Format( "Select 'Has Assesses Competencies' to search for {0} with any competencies.", labelPlural ),
		//			MicroSearchGuidance = string.Format( "Enter a term(s) to show {0} with relevant competencies and click Add.", labelPlural ),
		//			HasAny = new FilterItem()
		//			{
		//				Label = "Has Assesses Competencies",
		//				URI = "asmtReport:HasCompetencies"
		//			}
		//		};
		//		filters.SubjectAreas = new MSR.Filter()
		//		{
		//			Label = "Subject Areas",
		//			URI = "subjectAreas",
		//			Description = "",
		//			MicroSearchGuidance = string.Format( "Enter a term(s) to show {0} with relevant subjects.", labelPlural ),
		//			SearchType = searchType,
		//			SearchTypeContext = "subjects"
		//		};
		//		//
		//		filters.Occupations = new MSR.Filter()
		//		{
		//			Label = "Occupations",
		//			URI = "occupations",
		//			HasAnyGuidance = string.Format( "Select 'Has Occupations' to search for {0} with any occupations.", labelPlural ),
		//			MicroSearchGuidance = string.Format( "Find and select the occupation(s) to filter relevant {0}.", labelPlural ),
		//			SearchType = searchType,
		//			SearchTypeContext = "occupations",
		//			HasAny = new FilterItem()
		//			{
		//				Label = "Has Occupations",
		//				URI = "asmtReport:HasOccupations"
		//			}
		//		};
		//		//
		//		filters.Industries = new MSR.Filter()
		//		{
		//			Label = "Industries",
		//			URI = "industries",
		//			HasAnyGuidance = string.Format( "Select 'Has Industries' to search for {0} with any industries.", labelPlural ),
		//			MicroSearchGuidance = string.Format( "Find and select the industries to filter relevant {0}.", labelPlural ),
		//			SearchType = searchType,
		//			SearchTypeContext = "industries",
		//			HasAny = new FilterItem()
		//			{
		//				Label = "Has Industries",
		//				URI = "asmtReport:HasIndustries"
		//			}
		//		};
		//		//
		//		filters.InstructionalPrograms = new MSR.Filter()
		//		{
		//			Label = "Instructional Programs",
		//			URI = "instructionalPrograms",
		//			HasAnyGuidance = string.Format( "Select 'Has Instructional Programs' to search for {0} with any instructional program classifications.", labelPlural ),
		//			MicroSearchGuidance = string.Format( "Find and select instructional programs to filter {0}.", labelPlural ),
		//			SearchType = searchType,
		//			SearchTypeContext = "instructionalprogramtypes",
		//			HasAny = new FilterItem()
		//			{
		//				Label = "Has InstructionalPrograms",
		//				URI = "asmtReport:HasCIP"
		//			}
		//		};
		//		//
		//		if ( qaReceived != null && qaReceived.Items.Any() )
		//		{
		//			var f = new MSR.Filter()
		//			{
		//				Label = "Quality Assurance",
		//				URI = "qualityAssurance",
		//				//CategoryId = qaReceived.Id,
		//				Description = string.Format( "Select the type(s) of quality assurance to filter relevant {0}.", labelPlural ),
		//				MicroSearchGuidance = string.Format( "Optionally, find and select one or more organizations that perform {0} Quality Assurance.", label ),
		//				SearchType = searchType,
		//				SearchTypeContext = "organizations",
		//			};
		//			filters.QualityAssurance = ConvertEnumeration( f, qaReceived );
		//		}
		//	}
		//	catch ( Exception ex )
		//	{
		//		LoggingHelper.DoTrace( 1, string.Format( "GetAssessmentFilters. {0}", ex.Message ) );
		//	}
		//	return filters;
		//}

		//public static FilterQueryOld GetLearningOppFilters( bool getAll = false )
		//{
		//	EnumerationServices enumServices = new EnumerationServices();
		//	int entityTypeId = 7;
		//	string searchType = "learningopportunity";
		//	string label = "learning opportunity";
		//	string labelPlural = "learningopportunities";
		//	FilterQueryOld filters = new FilterQueryOld( searchType );

		//	try
		//	{
		//		var asmtMethodTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, CodesManager.PROPERTY_CATEGORY_Assessment_Method_Type, entityTypeId, getAll );
		//		var audienceLevelTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, 4, entityTypeId, getAll );
		//		var audienceTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, 14, entityTypeId, getAll );
		//		var connections = enumServices.GetLearningOppsConditionProfileTypes( EnumerationType.MULTI_SELECT, getAll );

		//		//
		//		var deliveryTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE, entityTypeId, getAll );
		//		var loppAsmtDeliveryTypes = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, CodesManager.PROPERTY_CATEGORY_ASMT_DELIVERY_TYPE, entityTypeId, getAll );
		//		var learningMethodTypes = enumServices.GetEnumeration( "learningMethodType", EnumerationType.MULTI_SELECT, false , getAll );
		//		var languages = enumServices.GetSiteTotals( EnumerationType.MULTI_SELECT, 65, entityTypeId, getAll );
		//		var qaReceived = enumServices.GetEntityAgentQAActions( EnumerationType.MULTI_SELECT, entityTypeId, getAll );
		//		var otherFilters = enumServices.EntityStatisticGetEnumeration( entityTypeId, EnumerationType.MULTI_SELECT, getAll );

		//		/* TODO
		//		 * QA received accredited, approved, recognized, regulated 
		//		 * other filters
		//		 * Has any?
		//		 *	industry, occupation, 
		//		 * Connections
		//		 * - ispartof, is prep, etc
		//		 */

		//		if ( asmtMethodTypes != null && asmtMethodTypes.Items.Any() )
		//			filters.AssessmentMethodTypes = ConvertEnumeration( "assessmentmethodtypes", asmtMethodTypes );
		//		//
		//		if ( audienceLevelTypes != null && audienceLevelTypes.Items.Any() )
		//			filters.AudienceLevels = ConvertEnumeration( "audienceleveltypes", audienceLevelTypes, "Select the type of level(s) indicating a point in a progression through an educational or training context.." );
		//		if ( audienceTypes != null && audienceTypes.Items.Any() )
		//			filters.AudienceTypes = ConvertEnumeration( "Audiencetypes", audienceTypes, string.Format( "Select the applicable audience types that are the target of {0}. Note that many {0} will not have specific audience types.", labelPlural, labelPlural ) );
		//		//
		//		if ( connections != null && connections.Items.Any() )
		//			filters.Connections = ConvertEnumeration( "loppConnections", connections, string.Format( "Select the connection type(s) to filter {0}:", labelPlural ) );
		//		//
		//		if ( deliveryTypes != null && deliveryTypes.Items.Any() )
		//			filters.LearningDeliveryTypes = ConvertEnumeration( "learningdeliverytypes", deliveryTypes, "Select the type(s) of delivery method(s)." );

		//		if ( loppAsmtDeliveryTypes != null && loppAsmtDeliveryTypes.Items.Any() )
		//			filters.AssessmentDeliveryTypes = ConvertEnumeration( "assessmentdeliverytypes", loppAsmtDeliveryTypes, "Select the type(s) of assessment delivery method(s)." );
		//		//
		//		if ( languages != null && languages.Items.Any() )
		//			filters.Languages = ConvertEnumeration( "languages", languages, string.Format( "Select one or more languages to display {0} for those languages.", labelPlural ) );
		//		//
		//		if ( learningMethodTypes != null && learningMethodTypes.Items.Any() )
		//			filters.LearningMethodTypes = ConvertEnumeration( "learningmethodtypes", learningMethodTypes, "Select the type(s) of learning method." );
		//		//
		//		if ( otherFilters != null && otherFilters.Items.Any() )
		//			filters.OtherFilters = ConvertEnumeration( "otherFilters", otherFilters, string.Format( "Select one of the 'Other' filters that are available. Note these filters are independent (ORs). For example selecting 'Has Cost Profile(s)' and 'Has Financial Aid' will show {0} that have cost profile(s) OR financial assistance.", labelPlural ) );
		//		//
		//		//competencies don't have an autocomplete
		//		filters.Competencies = new MSR.Filter()
		//		{
		//			Label = "Competencies",
		//			URI = "competencies",
		//			HasAnyGuidance = string.Format( "Select 'Has Teaches Competencies' to search for {0} with any competencies.", labelPlural ),
		//			MicroSearchGuidance = string.Format( "Enter a term(s) to show {0} with relevant competencies and click Add.", labelPlural ),
		//			HasAny = new FilterItem()
		//			{
		//				Label = "Has Teaches Competencies",
		//				URI = "loppReport:HasCompetencies"
		//			}
		//		};
		//		//
		//		filters.SubjectAreas = new MSR.Filter()
		//		{
		//			Label = "Subject Areas",
		//			URI = "subjectAreas",
		//			Description = "",
		//			MicroSearchGuidance = string.Format( "Enter a term(s) to show {0} with relevant subjects.", labelPlural ),
		//			SearchType = searchType,
		//			SearchTypeContext = "subjects"
		//		};
		//		//
		//		filters.Occupations = new MSR.Filter()
		//		{
		//			Label = "Occupations",
		//			URI = "occupations",
		//			HasAnyGuidance = string.Format( "Select 'Has Occupations' to search for {0} with any occupations.", labelPlural ),
		//			MicroSearchGuidance = string.Format( "Find and select the occupation(s) to filter relevant {0}.", labelPlural ),
		//			SearchType = searchType,
		//			SearchTypeContext = "occupations",
		//			HasAny = new FilterItem()
		//			{
		//				Label = "Has Occupations",
		//				URI = "loppReport:HasOccupations"
		//			}
		//		};
		//		//
		//		filters.Industries = new MSR.Filter()
		//		{
		//			Label = "Industries",
		//			URI = "industries",
		//			HasAnyGuidance = string.Format( "Select 'Has Industries' to search for {0} with any industries.", labelPlural ),
		//			MicroSearchGuidance = string.Format( "Find and select the industries to filter relevant {0}.", labelPlural ),
		//			SearchType = searchType,
		//			SearchTypeContext = "industries",
		//			HasAny = new FilterItem()
		//			{
		//				Label = "Has Industries",
		//				URI = "loppReport:HasIndustries"
		//			}
		//		};
		//		//
		//		filters.InstructionalPrograms = new MSR.Filter()
		//		{
		//			Label = "Instructional Programs",
		//			URI = "instructionalPrograms",
		//			HasAnyGuidance = string.Format( "Select 'Has Instructional Programs' to search for {0} with any instructional program classifications.", labelPlural ),
		//			MicroSearchGuidance = string.Format( "Find and select instructional programs to filter {0}.", labelPlural ),
		//			SearchType = searchType,
		//			SearchTypeContext = "instructionalprogramtypes",
		//			HasAny = new FilterItem()
		//			{
		//				Label = "Has InstructionalPrograms",
		//				URI = "loppReport:HasCIP"
		//			}
		//		};
		//		//
		//		if ( qaReceived != null && qaReceived.Items.Any() )
		//		{
		//			var f = new MSR.Filter()
		//			{
		//				Label = "Quality Assurance",
		//				URI = "qualityAssurance",
		//				//CategoryId = qaReceived.Id,
		//				Description = string.Format( "Select the type(s) of quality assurance to filter relevant {0}.", labelPlural ),
		//				MicroSearchGuidance = string.Format( "Optionally, find and select one or more organizations that perform {0} Quality Assurance.", label ),
		//				SearchType = searchType,
		//				SearchTypeContext = "organizations",
		//			};
		//			filters.QualityAssurance = ConvertEnumeration( f, qaReceived );
		//		}
		//	}
		//	catch ( Exception ex )
		//	{
		//		LoggingHelper.DoTrace( 1, string.Format( "GetLearningOppFilters. {0}", ex.Message ) );
		//	}
		//	return filters;
		//}

		//public static FilterQueryOld GetPathwayFilters( bool getAll = false )
		//{
		//	EnumerationServices enumServices = new EnumerationServices();
		//	int entityTypeId = 8;
		//	string searchType = "pathway";
		//	string label = "Pathway";
		//	string labelPlural = "pathways";
		//	FilterQueryOld filters = new FilterQueryOld( searchType );

		//	try
		//	{

		//		var otherFilters = enumServices.EntityStatisticGetEnumeration( entityTypeId, EnumerationType.MULTI_SELECT, getAll );

		//		//
		//		if ( otherFilters != null && otherFilters.Items.Any() )
		//			filters.OtherFilters = ConvertEnumeration( "otherFilters", otherFilters, string.Format( "Select one of the 'Other' filters that are available. Note these filters are independent (ORs). For example selecting 'Has Cost Profile(s)' and 'Has Financial Aid' will show {0} that have cost profile(s) OR financial assistance.", labelPlural ) );
		//		//
		//		filters.SubjectAreas = new MSR.Filter()
		//		{
		//			Label = "Subject Areas",
		//			URI = "subjectAreas",
		//			Description = "",
		//			MicroSearchGuidance = string.Format( "Enter a term(s) to show {0} with relevant subjects.", labelPlural ),
		//			SearchType = searchType,
		//			SearchTypeContext = "subjects"
		//		};
		//		//
		//		filters.Occupations = new MSR.Filter()
		//		{
		//			Label = "Occupations",
		//			URI = "occupations",
		//			HasAnyGuidance = string.Format( "Select 'Has Occupations' to search for {0} with any occupations.", labelPlural ),
		//			MicroSearchGuidance = string.Format( "Find and select the occupation(s) to filter relevant {0}.", labelPlural ),
		//			SearchType = searchType,
		//			SearchTypeContext = "occupations",
		//			HasAny = new FilterItem()
		//			{
		//				Label = "Has Occupations",
		//				URI = "pathwayReport:HasOccupations"
		//			}
		//		};
		//		//
		//		filters.Industries = new MSR.Filter()
		//		{
		//			Label = "Industries",
		//			URI = "industries",
		//			HasAnyGuidance = string.Format( "Select 'Has Industries' to search for {0} with any industries.", labelPlural ),
		//			MicroSearchGuidance = string.Format( "Find and select the industries to filter relevant {0}.", labelPlural ),
		//			SearchType = searchType,
		//			SearchTypeContext = "industries",
		//			HasAny = new FilterItem()
		//			{
		//				Label = "Has Industries",
		//				URI = "pathwayReport:HasIndustries"
		//			}
		//		};

		//	}
		//	catch ( Exception ex )
		//	{
		//		LoggingHelper.DoTrace( 1, string.Format( "GetPathwayFilters. {0}", ex.Message ) );
		//	}
		//	return filters;
		//}
		//public static FilterQueryOld GetNoFilters( string label )
		//{
		//	FilterQueryOld filters = new FilterQueryOld(label);

		//	try
		//	{
		//		filters.Connections = new MSR.Filter()
		//		{
		//			Label = label,
		//			URI = label.ToLower().Replace(" ",""),
		//			Description = string.Format( "There are no filters currently available for {0}.", label ),
		//		};

		//	}
		//	catch ( Exception ex )
		//	{
		//		LoggingHelper.DoTrace( 1, string.Format( "GetPathwayFilters. {0}", ex.Message ) );
		//	}
		//	return filters;
		//}
		//public static FilterQueryOld GetCompetencyFrameworkFilters( bool getAll = false )
		//{
		//	EnumerationServices enumServices = new EnumerationServices();
		//	int entityTypeId = 10;
		//	string searchType = "CompetencyFramework";
		//	string label = "Competency Framework";
		//	string labelPlural = "Competency Frameworks";
		//	FilterQueryOld filters = new FilterQueryOld( searchType );

		//	try
		//	{
		//		filters.Competencies = new MSR.Filter()
		//		{
		//			Label = "Competency Frameworks",
		//			URI = "competencyFrameworks",
		//			Description = string.Format( "There are no filters currently available for Competency Frameworks.", labelPlural ),

		//		};

		//	}
		//	catch ( Exception ex )
		//	{
		//		LoggingHelper.DoTrace( 1, string.Format( "GetPathwayFilters. {0}", ex.Message ) );
		//	}
		//	return filters;
		//}

		//public static MSR.Filter ConvertEnumeration( MSR.Filter output, Enumeration e )
		//{

		//	foreach ( var item in e.Items )
		//	{
		//		output.Items.Add( new MSR.FilterItem()
		//		{
		//			Id = item.Id,
		//			Label = item.Name,
		//			//Schema = item.SchemaName,
		//			URI = item.SchemaName,
		//			Description = item.Description,
		//			InterfaceType = "filterType:CheckBox"

		//		} );
		//	}

		//	return output;
		//}
		public static MSR.Filter ConvertEnumeration( string filterName, Enumeration e, string guidance = "" )
		{
			var output = new MSR.Filter( filterName )
			{
				//URI = filterName,
				//CategoryId = e.Id,
				Label = filterName,
				Description = guidance
			};
			//test
			if (!string.IsNullOrWhiteSpace(e.SchemaName) && e.SchemaName.IndexOf("ceterms:")==0 )
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

		//public static bool IsFilterAvailableFromCache( string searchType, ref FilterResponse output )
		//{
		//	int cacheMinutes = 120;
		//	DateTime maxTime = DateTime.Now.AddMinutes( cacheMinutes * -1 );
		//	string key = searchType + "_queryFilter";

		//	if ( HttpRuntime.Cache[ key ] != null && cacheMinutes > 0 )
		//	{
		//		var cache = new CachedFilter();
		//		try
		//		{
		//			cache = ( CachedFilter )HttpRuntime.Cache[ key ];
		//			if ( cache.lastUpdated > maxTime )
		//			{
		//				LoggingHelper.DoTrace( 6, string.Format( "===SearchServices.IsFilterAvailableFromCache === Using cached version of FilterQuery, SearchType: {0}.", cache.Item.SearchType ) );
		//				output= cache.Item;
		//				return true;
		//			}
		//		}
		//		catch ( Exception ex )
		//		{
		//			LoggingHelper.DoTrace( 6, "SearchServices.IsFilterAvailableFromCache. === exception " + ex.Message );
		//		}
		//	}
			
		//	return false;
		//}
		//public static void AddFilterToCache( FilterResponse entity )
		//{
		//	int cacheMinutes = 120;

		//	string key = entity.SearchType + "_queryFilter";

		//	if ( cacheMinutes > 0 )
		//	{
		//		var newCache = new CachedFilter()
		//		{
		//			Item = entity,
		//			lastUpdated = DateTime.Now
		//		};
		//		if ( HttpContext.Current != null )
		//		{
		//			if ( HttpContext.Current.Cache[ key ] != null )
		//			{
		//				HttpRuntime.Cache.Remove( key );
		//				HttpRuntime.Cache.Insert( key, newCache );
		//				LoggingHelper.DoTrace( 7, string.Format( "SearchServices.AddFilterToCache $$$ Updating cached version of FilterQuery, SearchType: {0}", entity.SearchType ) );

		//			}
		//			else
		//			{
		//				LoggingHelper.DoTrace( 7, string.Format( "SearchServices.AddFilterToCache ****** Inserting new cached version of SearchType, SearchType: {0}", entity.SearchType ) );

		//				System.Web.HttpRuntime.Cache.Insert( key, newCache, null, DateTime.Now.AddHours( cacheMinutes ), TimeSpan.Zero );
		//			}
		//		}
		//	}

		//	//return entity;
		//}

		#endregion
	}
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

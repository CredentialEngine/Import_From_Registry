using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using workIT.Models.Search;

namespace workIT.Services.API
{
	//Search Services dealing with Queries
	public partial class SearchServices
	{
		//TODO: Update this to accept a debug JObject
		public static MainSearchInput TranslateMainQueryToMainSearchInput( MainQuery query, JObject debug = null )
		{
			var translated = new MainSearchInput();
			debug = debug ?? new JObject();

			translated.SearchType = query.SearchType;
			//translated.AutoCompleteContext = query.AutocompleteContext;
			translated.Keywords = query.Keywords;
			translated.StartPage = query.SkipPages + 1;
			translated.PageSize = query.PageSize;
			translated.SortOrder = TranslateMainQuerySortOrderToMainSearchInputSortOrder( query.SortOrder );
			translated.FiltersV2 = TranslateMainQueryFiltersToMainSearchInputFilters( query.MainFilters, query.MapFilter, query.SearchType, debug );
			translated.AutocompleteContext = query.AutocompleteContext;

			//translated.WidgetId = query.WidgetId;
			//translated.MustHaveWidget = ? //TBD
			//translated.MustNotHaveWidget = ? //TBD

			translated.UseSimpleSearch = false; //Probably always going to be false in this context?
			//translated.IncludingReferenceObjects = ? //TBD. Should probably be handled by a Filter instead.
			//translated.HasCredentialPotentialResults = ? //TBD
			//translated.CustomSearchInFields = ? //TBD

			//translated.CompetenciesKeywords = ? //TBD. Currently has 0 references in the MainSearch.cs file, so we may not need this.
			//translated.ElasticConfigs = ? //Only used with the development-only interface. No need to translate it.
			//translated.UseSPARQL = ? //Not used anymore. No need to translate it.
			//translated.Filters = ? //Not used anymore. No need to translate it.

			return translated;
		}
		//

		public static string TranslateMainQuerySortOrderToMainSearchInputSortOrder( string mainQuerySortOrder )
		{
			mainQuerySortOrder = mainQuerySortOrder ?? "";
			return MainQuerySortOrderToMainSearchInputSortOrder.FirstOrDefault( m => m.NewValue == mainQuerySortOrder )?.OldValue ?? "relevance";
		}
		//

		public static string TranslateMainQueryFilterURIToMainSearchInputFilterName( string mainFilterURI, string searchType )
		{
			mainFilterURI = mainFilterURI ?? "";
			return MainQueryFilterURIToSearchInputFilterName.Where( m => m.SearchType == searchType.ToLower() || m.SearchType == null ).FirstOrDefault( m => m.NewValue == mainFilterURI )?.OldValue ?? mainFilterURI.Replace( "originalFilterName:", "" );
		}
		//

		public static List<MainSearchFilterV2> TranslateMainQueryFiltersToMainSearchInputFilters( List<Filter> mainFilters, MapFilter mapFilter, string searchType, JObject debug = null )
		{
			mainFilters = mainFilters ?? new List<Filter>();
			var translated = new List<MainSearchFilterV2>();
			debug = debug ?? new JObject();
			
			//Expand each MainFilter out into its Filters and merge their data into a list of MainSearchFilterV2
			foreach( var mainFilter in mainFilters )
			{
				//Handle "CUSTOM" filters
				if( mainFilter.Parameters != null && mainFilter.Parameters.Properties().Count() > 0 )
				{
					var translatedItem = new MainSearchFilterV2();
					translatedItem.Type = MainSearchFilterV2Types.CUSTOM;
					translatedItem.Name = mainFilter.Parameters[ "n" ]?.ToString() ?? "";
					switch ( translatedItem.Name )
					{
						case "organizationroles": //Seems to be the only custom filter?
						if( searchType == "competencyframework" )
						{
							//Get the data
							var agentID = int.Parse( mainFilter.Parameters[ "aid" ]?.ToString() ?? "0" );
							var relationshipIDs = ( ( JArray ) mainFilter.Parameters[ "rid" ] ?? new JArray() ).Select( m => int.Parse( m?.ToString() ?? "0" ) ).ToList();
							
							//Lookup the agent ID
							var agentCTID = OrganizationServices.GetForSummary( agentID )?.CTID ?? "";
							if ( string.IsNullOrWhiteSpace( agentCTID ) )
							{
								break;
							}

							//Hack - Assume that 6 and/or 7 represents an owns/offers relationship
							//Additional Hack - CTDL-ASN doesn't have/use ownedBy or offeredBy, it uses publisher/creator, and those connections are reversed compared to CTDL anyway
							//So assume that if 6 and/or 7 are present, we want to do a reverse publisher/creator lookup
							if( relationshipIDs.Contains(6) || relationshipIDs.Contains( 7 ) )
							{
								translatedItem.TranslationHelper = new JObject()
								{
									{ "> ceasn:publisher > ceterms:ctid", agentCTID },
									{ "> ceasn:creator > ceterms:ctid", agentCTID }
								};
							}
						}
						else
						{
							/* Doing this the right way breaks code later on that is expecting it to be done the wrong way
							translatedItem.Values = new Dictionary<string, object>()
							{
								{ "AgentId", int.Parse( mainFilter.Parameters["aid"]?.ToString() ?? "0" ) },
								{ "RelationshipId", ( (JArray) mainFilter.Parameters["rid"] ?? new JArray() ).Select( m => int.Parse( m?.ToString() ?? "0" ) ).ToList() }
							};
							*/
							//So hack in the wrong conversion behavior to make the code later on work as expected
							translatedItem.Values = new Dictionary<string, object>();
							translatedItem.Values.Add( "AgentId", int.Parse( mainFilter.Parameters[ "aid" ]?.ToString() ?? "0" ) );
							var counter = 0;
							foreach ( var id in ( ( JArray ) mainFilter.Parameters[ "rid" ] ?? new JArray() ).Select( m => int.Parse( m?.ToString() ?? "0" ) ).ToList() )
							{
								translatedItem.Values.Add( "RelationshipId[" + counter + "]", id );
								counter++;
							}
						}
						break;

						case "potentialresults":
						{
							translatedItem.CustomJSON = ( ( JArray ) mainFilter.Parameters[ "ids" ] )?.ToString( Formatting.None );
						}
						break;

						default:
						translatedItem.Values = TranslateJObjectToDictionaryStringObject( mainFilter.Parameters );
						break;
					}
					translated.Add( translatedItem );
				}
				//Handle all the other kinds of filters
				else
				{
					//Hacky duct tape Badge behavior
					if ( mainFilter.Items.Any( m => m.URI == "ceterms:Badge" ) )
					{
						var filters = ConvertEnumeration( "Credential Type", "credentialType", new EnumerationServices().GetCredentialType( workIT.Models.Common.EnumerationType.MULTI_SELECT, true, true  ) );
						mainFilter.Items.Add( filters.Items.FirstOrDefault( m => m.URI == "ceterms:DigitalBadge" ) );
						mainFilter.Items.Add( filters.Items.FirstOrDefault( m => m.URI == "ceterms:OpenBadge" ) );
					}

					//Process filters
					foreach ( var filterItem in mainFilter.Items )
					{
						var translatedItem = new MainSearchFilterV2();
						if ( filterItem != null )
						{
							//Bypass conversion for widget locationset filter
							if ( filterItem.Type?.ToLower() == "locationset" || filterItem.URI?.ToLower() == "locationset" )
							{
								//Not sure which of these is correct
								translated.Add( new MainSearchFilterV2()
								{
									Name = "LOCATIONSET",
									Type = MainSearchFilterV2Types.CODE,
									Values = filterItem.Values
								} );

								translated.Add( new MainSearchFilterV2()
								{
									Name = "bounds",
									Type = MainSearchFilterV2Types.MAP,
									Map_PositionType = "positionType:In",
									Map_Country = filterItem.Values.ContainsKey( "Countries" ) ? string.Join( ", ", ( ( JArray ) filterItem.Values[ "Countries" ] )?.Select( m => m?.ToString() ).Where( m => !string.IsNullOrWhiteSpace( m ) ).ToList() ) : null,
									Map_Region = filterItem.Values.ContainsKey( "Regions" ) ? string.Join( ", ", ( ( JArray ) filterItem.Values[ "Regions" ] )?.Select( m => m?.ToString() ).Where( m => !string.IsNullOrWhiteSpace( m ) ).ToList() ) : null,
									Map_Locality = filterItem.Values.ContainsKey( "Cities" ) ? string.Join( ", ", ( ( JArray ) filterItem.Values[ "Cities" ] )?.Select( m => m?.ToString() ).Where( m => !string.IsNullOrWhiteSpace( m ) ).ToList() ) : null
								} );
							}
							//Handle other filters
							else
							{
								switch ( filterItem.InterfaceType )
								{
									case "interfaceType:CheckBox": translatedItem.Type = MainSearchFilterV2Types.CODE; break;
									case "interfaceType:Text": translatedItem.Type = MainSearchFilterV2Types.TEXT; break;
									//Note: Date range filters would (apparently?) use the default of "CODE" since there is no "DATE" in MainSearchFilterV2Types
									default: break;
								}

								translatedItem.Name = TranslateMainQueryFilterURIToMainSearchInputFilterName( mainFilter.URI, searchType );

								translatedItem.Values = new Dictionary<string, object>();
								if ( mainFilter.Id > 0 )
								{
									translatedItem.Values.Add( "CategoryId", mainFilter.Id );
								}
								if ( filterItem.Id > 0 )
								{
									translatedItem.Values.Add( "CodeId", filterItem.Id );
									translatedItem.Values.Add( "CodeText", !string.IsNullOrWhiteSpace( filterItem.Text ) ? filterItem.Text : !string.IsNullOrWhiteSpace( filterItem.Label ) ? filterItem.Label : filterItem.Id.ToString() );
								}
								if ( !string.IsNullOrWhiteSpace( filterItem.Text ) )
								{
									translatedItem.Values.Add( "TextValue", filterItem.Text );
								}
								if ( !string.IsNullOrWhiteSpace( filterItem.URI ) )
								{
									if ( mainFilter.URI == "filter:Custom_DateRange" ) //Kind of a kludge, but only the date range filters use PropertyName instead of SchemaName
									{
										translatedItem.Values.Add( "PropertyName", filterItem.URI.Replace( "originalFilterItemName:", "" ) );
									}
									else
									{
										translatedItem.Values.Add( "SchemaName", filterItem.URI );
									}
								}

								translated.Add( translatedItem );
							}
						}
					}
				}
			}

			if( mapFilter != null )
			{
				var translatedMapFilter = new MainSearchFilterV2()
				{
					Type = MainSearchFilterV2Types.MAP,
					Name = "bounds",
					Map_PositionType = mapFilter.PositionType,
					Map_Country = mapFilter.Country,
					Map_Region = mapFilter.Region,
					Map_Locality = mapFilter.Region == mapFilter.Label ? null : mapFilter.Label
				};

				//If all of the bounding box values are non-zero, use them directly
				//We'll need to improve this to use something other than a box and/or factor in the an irregularly-shaped geographic region like a state
				if ( mapFilter.PositionType == "positionType:In" && new List<double>() { mapFilter.BBoxNorth, mapFilter.BBoxEast, mapFilter.BBoxSouth, mapFilter.BBoxWest }.All( m => m != 0 ) )
				{
					translatedMapFilter.Values = new Dictionary<string, object>()
					{
						{ "north", (decimal) mapFilter.BBoxNorth },
						{ "east", (decimal) mapFilter.BBoxEast },
						{ "south", (decimal) mapFilter.BBoxSouth },
						{ "west", (decimal) mapFilter.BBoxWest },
					};

					translated.Add( translatedMapFilter );
				}
				//Otherwise, create a box with a roughly 50 mile radius (0.75 degrees = 51.75 miles) from the center point
				//Once the interface has a drop-down box for selecting the radius, this will need to be adjusted
				else if( mapFilter.BBoxCenterLatitude != 0 && mapFilter.BBoxCenterLongitude != 0 )
				{
					translatedMapFilter.Values = new Dictionary<string, object>()
					{
						{ "north", (decimal) mapFilter.BBoxCenterLatitude + 0.75m },
						{ "east", (decimal) mapFilter.BBoxCenterLongitude + 0.75m },
						{ "south", (decimal) mapFilter.BBoxCenterLatitude + -0.75m },
						{ "west", (decimal) mapFilter.BBoxCenterLongitude + -0.75m },
					};

					translated.Add( translatedMapFilter );
				}

				//Otherwise, don't include anything

			}

			return translated;
		}
		//

		public static Dictionary<string, object> TranslateJObjectToDictionaryStringObject( JObject input )
		{
			var result = new Dictionary<string, object>();

			foreach( var property in input.Properties() )
			{
				result.Add( property.Name, TranslateJTokenToObject( property.Value ) );
			}

			return result;
		}
		private static object TranslateJTokenToObject( JToken value )
		{
			object result = null;

			if( value.Type == JTokenType.Array )
			{
				var holder = new List<object>();
				foreach ( var item in ( JArray ) value )
				{
					holder.Add( TranslateJTokenToObject( item ) );
				}
				result = holder;
			}
			else if( value.Type == JTokenType.Object )
			{
				result = TranslateJObjectToDictionaryStringObject( (JObject) value );
			}
			else
			{
				result = JsonConvert.DeserializeObject( value.ToString( Formatting.None ) );
			}

			return result;
		}
		//

		#region Old to New

		public static MainQuery TranslateMainSearchInputToMainQuery( MainSearchInput query )
		{
			var translated = new MainQuery();
			translated.SearchType = query.SearchType;

			translated.SearchType = query.SearchType;
			translated.Keywords = query.Keywords;
			translated.SkipPages = query.StartPage - 1;
			translated.PageSize = query.PageSize;
			translated.SortOrder = TranslateMainSearchInputSortOrderToMainQuerySortOrder( query.SortOrder );
			translated.MainFilters = TranslateMainSearchInputFiltersToMainQueryFilters( query.FiltersV2, query.SearchType );
			//translated.WidgetId = query.WidgetId;
			translated.MapFilter = TranslateMainSearchMapFilterToMainQueryMapFilter( query.FiltersV2, query.SearchType );
			translated.AutocompleteContext = query.AutocompleteContext;

			return translated;
		}
		//

		public static string TranslateMainSearchInputSortOrderToMainQuerySortOrder( string mainSearchInputSortOrder )
		{
			mainSearchInputSortOrder = mainSearchInputSortOrder ?? "";
			return MainQuerySortOrderToMainSearchInputSortOrder.FirstOrDefault( m => m.OldValue == mainSearchInputSortOrder )?.NewValue ?? "sortOrder:MostRelevant";
		}
		//

		public static string TranslateMainSearchInputFilterNameToMainQueryFilterURI( string mainFilterName, string searchType )
		{
			mainFilterName = mainFilterName ?? "";
			return MainQueryFilterURIToSearchInputFilterName.Where( m => m.SearchType == searchType.ToLower() || m.SearchType == null ).FirstOrDefault( m => m.OldValue == mainFilterName )?.NewValue ?? "originalFilterName:" + mainFilterName;
		}
		//

		public static List<Filter> TranslateMainSearchInputFiltersToMainQueryFilters( List<MainSearchFilterV2> mainFilters, string searchType )
		{
			var translated = new List<Filter>();

			foreach( var filter in mainFilters.Where( m => m.Type != MainSearchFilterV2Types.MAP ).ToList() )
			{
				var filterURI = TranslateMainSearchInputFilterNameToMainQueryFilterURI( filter.Name, searchType );
				var matchingFilter = translated.FirstOrDefault( m => m.URI == filterURI );
				if( matchingFilter == null )
				{
					matchingFilter = new Filter()
					{
						URI = filterURI
					};
					try
					{
						matchingFilter.Id = int.Parse( filter.Values[ "CategoryId" ].ToString() );
					}
					catch { }
					translated.Add( matchingFilter );
				}

				if( filter.Type == MainSearchFilterV2Types.CUSTOM )
				{
					matchingFilter.URI = TranslateMainSearchInputFilterNameToMainQueryFilterURI( filter.Name, searchType );
					matchingFilter.Parameters = JObject.FromObject( filter.Values );
				}
				else
				{
					var item = new FilterItem();
					switch ( filter.Type )
					{
						case MainSearchFilterV2Types.TEXT: item.InterfaceType = "interfaceType:Text"; break;
						case MainSearchFilterV2Types.CODE: item.InterfaceType = "interfaceType:CheckBox"; break;
						default: item.Type = "filterItem:CheckBox"; break;
					}

					try { 
						item.URI = 
							filter.Values.ContainsKey( "SchemaName" ) ? filter.Values[ "SchemaName" ].ToString() : //Most filters use this
							filter.Values.ContainsKey( "PropertyName" ) ? "originalFilterItemName:" + filter.Values[ "PropertyName" ].ToString() : //Date Range filters use this
							"TBD"; //Unknown
					} catch { }
					try { item.Text = filter.Values.ContainsKey( "TextValue" ) ? filter.Values[ "TextValue" ].ToString() : null; } catch { }
					try { item.Id = filter.Values.ContainsKey( "CodeId" ) ? int.Parse( filter.Values[ "CodeId" ].ToString() ) : 0; } catch { }

					matchingFilter.Items = matchingFilter.Items ?? new List<FilterItem>();
					matchingFilter.Items.Add( item );
				}
			}

			//Hacky duct tape Badge behavior
			translated = translated.Where( m => m.URI != "ceterms:DigitalBadge" && m.URI != "ceterms:OpenBadge" ).ToList();

			return translated;
		}
		//

		public static MapFilter TranslateMainSearchMapFilterToMainQueryMapFilter( List<MainSearchFilterV2> mainFilters, string searchType )
		{
			var mapFilter = mainFilters.FirstOrDefault( m => m.Type == MainSearchFilterV2Types.MAP );
			if( mapFilter != null )
			{
				try
				{
					var map = new MapFilter();
					map.BBoxNorth = double.Parse( mapFilter.Values[ "north" ]?.ToString() ?? "0.0" );
					map.BBoxEast = double.Parse( mapFilter.Values[ "east" ]?.ToString() ?? "0.0" );
					map.BBoxSouth = double.Parse( mapFilter.Values[ "south" ]?.ToString() ?? "0.0" );
					map.BBoxWest = double.Parse( mapFilter.Values[ "west" ]?.ToString() ?? "0.0" );
					map.BBoxCenterLatitude = ( map.BBoxNorth + map.BBoxSouth ) / 2;
					map.BBoxCenterLongitude = ( map.BBoxEast + map.BBoxWest ) / 2;
					//Average out the vertical and horizontal radii and multiply by 69 to convert degrees to miles
					map.RadiusMiles = (int) Math.Ceiling( ( ( Math.Abs( map.BBoxNorth - map.BBoxCenterLatitude ) + Math.Abs( map.BBoxEast - map.BBoxCenterLongitude ) ) / 2 ) * 69 );
					return map;
				}
				catch { }
			}

			return null;
		}
		//

		#endregion

		#region Mappings
		private class Mapping
		{
			public Mapping( string newValue, string oldValue, string searchType = null )
			{
				NewValue = newValue;
				OldValue = oldValue;
				SearchType = searchType;
			}

			public string NewValue { get; set; }
			public string OldValue { get; set; }
			public string SearchType { get; set; }
		}

		private static List<Mapping> MainQueryFilterURIToSearchInputFilterName
		{
			get
			{
				return new List<Mapping>()
				{
					//Common
					new Mapping( "filter:QAReceived", "qualityassurance" ),
					new Mapping( "filter:AudienceLevelType", "audienceleveltypes" ), //Learning Opportunities use filter:AudienceLevel
					new Mapping( "filter:AudienceType", "audiencetypes" ),
					new Mapping( "filter:Competencies", "competencies" ),
					new Mapping( "filter:Subjects", "subjects" ),
					new Mapping( "filter:IndustryType", "industries" ),
					new Mapping( "filter:OccupationType", "occupations" ),
					new Mapping( "filter:InstructionalProgramType", "instructionalprogramtypes" ),
					new Mapping( "filter:InLanguage", "languages" ),
					new Mapping( "filter:OtherFilters", "reports" ),

					//Organization-Specific
					new Mapping( "filter:OrganizationTypes", "organizationtypes", "organization" ),
					new Mapping( "filter:QAPerformed", "qaperformed", "organization" ),
					new Mapping( "filter:ServiceTypes", "servicetypes", "organization" ),
					new Mapping( "filter:SectorTypes", "sectortypes", "organization" ),
					new Mapping( "filter:ClaimTypes", "claimtypes", "organization" ),

					//Credential-Specific
					new Mapping( "filter:CredentialType", "credentialtypes", "credential" ),
					new Mapping( "filter:CredentialConnection", "credentialconnections", "credential" ),
					new Mapping( "filter:AssessmentDeliveryType", "assessmentdeliverytypes", "credential" ),
					new Mapping( "filter:LearningDeliveryType", "learningdeliverytypes", "credential" ),
					new Mapping( "filter:CredentialStatus", "credentialstatustypes", "credential" ),

					//Assessment-Specific
					new Mapping( "filter:Connections", "assessmentconnections", "assessment" ),
					new Mapping( "filter:AssessmentMethodType", "assessmentmethodtypes", "assessment" ),
					new Mapping( "filter:AssessmentUse", "assessmentusetypes", "assessment" ),
					new Mapping( "filter:AssessmentDeliveryType", "deliverymethodtypes", "assessment" ),

					//LearningOpportunity-Specific
					new Mapping( "filter:Connections", "learningopportunityconnections", "learningopportunity" ),
					new Mapping( "filter:AudienceLevel", "audienceleveltypes", "learningopportunity" ), //Should probably use filter:AudienceLevelType (see "Common" section above)
					new Mapping( "filter:LearningDeliveryType", "deliverymethodtypes", "learningopportunity" ),
					new Mapping( "filter:LearningMethodTypes", "learningmethodtypes", "learningopportunity" )
				};
			}
		}

		private static List<Mapping> MainQuerySortOrderToMainSearchInputSortOrder
		{
			get
			{
				return new List<Mapping>()
				{
					new Mapping( "sortOrder:MostRelevant", "relevance" ),
					new Mapping( "sortOrder:Newest", "newest" ),
					new Mapping( "sortOrder:AtoZ", "alpha" ),
					new Mapping( "sortOrder:Oldest", "oldest" ),
					new Mapping( "sortOrder:ZtoA", "zalpha" )
				};
			}
		}
		#endregion
	}
}

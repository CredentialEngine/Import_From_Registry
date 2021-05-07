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
		public static MainSearchInput TranslateMainQueryToMainSearchInput( MainQuery query )
		{
			var translated = new MainSearchInput();

			translated.SearchType = query.SearchType;
			translated.Keywords = query.Keywords;
			translated.StartPage = query.SkipPages + 1;
			translated.PageSize = query.PageSize;
			translated.SortOrder = TranslateMainQuerySortOrderToMainSearchInputSortOrder( query.SortOrder );
			translated.FiltersV2 = TranslateMainQueryFiltersToMainSearchInputFilters( query.MainFilters, query.MapFilter );

			translated.WidgetId = query.WidgetFilter?.WidgetId ?? 0;
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
			switch ( mainQuerySortOrder ?? "" )
			{
				case "sortOrder:MostRelevant": return "relevance";
				case "sortOrder:Newest": return "newest";
				case "sortOrder:AtoZ": return "alpha";
				case "sortOrder:Oldest": return "oldest";
				case "sortOrder:ZtoA": return "zalpha"; //Z to A is not supported
				default: return "relevance";
			}
		}
		//

		public static string TranslateMainQueryFilterURIToMainSearchInputFilterName( string mainFilterURI )
		{
			switch( mainFilterURI ?? "" )
			{
				case "filter:CredentialType": return "credentialtypes";
				case "filter:Custom_DateRange": return "history";
				case "filter:Custom_OrganizationRoles": return "organizationroles";
				case "filter:Custom_QualityAssurance": return "qualityassurance";
				default: return ( mainFilterURI ?? "" ).Replace( "originalFilterName:", "" );
			}
		}
		//

		public static List<MainSearchFilterV2> TranslateMainQueryFiltersToMainSearchInputFilters( List<Filter> mainFilters, MapFilter mapFilter )
		{
			mainFilters = mainFilters ?? new List<Filter>();
			var translated = new List<MainSearchFilterV2>();
			
			//Expand each MainFilter out into its Filters and merge their data into a list of MainSearchFilterV2
			foreach( var mainFilter in mainFilters )
			{
				//Handle "CUSTOM" filters
				if( mainFilter.Parameters != null )
				{
					var translatedItem = new MainSearchFilterV2();
					translatedItem.Type = MainSearchFilterV2Types.CUSTOM;
					translatedItem.Values = TranslateJObjectToDictionaryStringObject( mainFilter.Parameters );
					translatedItem.Name = new List<string>()
					{
						TranslateMainQueryFilterURIToMainSearchInputFilterName( translatedItem.Values.ContainsKey("n") ? (string) translatedItem.Values["n"] : "" ),
						TranslateMainQueryFilterURIToMainSearchInputFilterName( mainFilter.URI ),
						TranslateMainQueryFilterURIToMainSearchInputFilterName( mainFilter.Id.ToString() )
					}.FirstOrDefault( m => m.Length > 0 );
					translated.Add( translatedItem );
				}
				//Handle all the other kinds of filters
				else
				{
					foreach ( var filterItem in mainFilter.Items )
					{
						var translatedItem = new MainSearchFilterV2();
						switch ( filterItem.InterfaceType )
						{
							case "interfaceType:CheckBox": translatedItem.Type = MainSearchFilterV2Types.CODE; break;
							case "interfaceType:Text": translatedItem.Type = MainSearchFilterV2Types.TEXT; break;
							//Note: Date range filters would (apparently?) use the default of "CODE" since there is no "DATE" in MainSearchFilterV2Types
							default: break;
						}

						translatedItem.Name = TranslateMainQueryFilterURIToMainSearchInputFilterName( mainFilter.URI );

						translatedItem.Values = new Dictionary<string, object>();
						if( mainFilter.Id > 0 )
						{
							translatedItem.Values.Add( "CategoryId", mainFilter.Id );
						}
						if( filterItem.Id > 0 )
						{
							translatedItem.Values.Add( "CodeId", filterItem.Id );
							translatedItem.Values.Add( "CodeText", filterItem.Id.ToString() );
						}
						if ( !string.IsNullOrWhiteSpace( filterItem.Text ) )
						{
							translatedItem.Values.Add( "TextValue", filterItem.Text );
						}
						if ( !string.IsNullOrWhiteSpace( filterItem.URI ) )
						{
							if( mainFilter.URI == "filter:Custom_DateRange" ) //Kind of a kludge, but only the date range filters use PropertyName instead of SchemaName
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

			if( mapFilter != null )
			{
				var translatedMapFilter = new MainSearchFilterV2()
				{
					Type = MainSearchFilterV2Types.MAP,
					Name = "bounds",
					Values = new Dictionary<string, object>()
					{
						{ "north", mapFilter.BBoxTopLeft?.Latitude ?? 0 },
						{ "east", mapFilter.BBoxBottomRight?.Longitude ?? 0 },
						{ "south", mapFilter.BBoxBottomRight?.Latitude ?? 0 },
						{ "west", mapFilter.BBoxTopLeft?.Longitude ?? 0 },
					}
				};
				translated.Add( translatedMapFilter );
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
			translated.MainFilters = TranslateMainSearchInputFiltersToMainQueryFilters( query.FiltersV2 );
			translated.WidgetFilter = query.WidgetId > 0 ? new WidgetFilter() { WidgetId = query.WidgetId } : null;

			return translated;
		}
		//

		public static string TranslateMainSearchInputSortOrderToMainQuerySortOrder( string mainSearchInputSortOrder )
		{
			switch ( mainSearchInputSortOrder ?? "" )
			{
				case "relevance": return "sortOrder:MostRelevant";
				case "newest": return "sortOrder:Newest";
				case "alpha": return "sortOrder:AtoZ"; //Z to A is not supported
				case "oldest": return "sortOrder:Oldest";
				default: return "sortOrder:MostRelevant";
			}
		}
		//

		public static string TranslateMainSearchInputFilterNameToMainQueryFilterURI( string mainFilterName )
		{
			switch ( mainFilterName ?? "" )
			{
				case "credentialtypes": return "filter:CredentialType";
				case "history": return "filter:Custom_DateRange";
				case "organizationroles": return "filter:Custom_OrganizationRoles";
				case "qualityassurance": return "filter:Custom_QualityAssurance";
				default: return "originalFilterName:" + mainFilterName;
			}
		}
		//

		public static List<Filter> TranslateMainSearchInputFiltersToMainQueryFilters( List<MainSearchFilterV2> mainFilters )
		{
			var translated = new List<Filter>();

			foreach( var filter in mainFilters.Where( m => m.Type != MainSearchFilterV2Types.MAP ).ToList() )
			{
				var filterURI = TranslateMainSearchInputFilterNameToMainQueryFilterURI( filter.Name );
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
					matchingFilter.URI = TranslateMainSearchInputFilterNameToMainQueryFilterURI( filter.Name );
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
			foreach( var filter in mainFilters.Where( m => m.Type == MainSearchFilterV2Types.MAP ).ToList() )
			{
				try
				{
					var map = new MapFilter();
					map.BBoxTopLeft = new Coordinates() { Latitude = float.Parse( filter.Values[ "north" ].ToString() ), Longitude = float.Parse( filter.Values[ "west" ].ToString() ) };
					map.BBoxBottomRight = new Coordinates() { Latitude = float.Parse( filter.Values[ "south" ].ToString() ), Longitude = float.Parse( filter.Values[ "east" ].ToString() ) };
				}
				catch { }
			}

			return translated;
		}
		//

		#endregion
	}
}

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json.Linq;

namespace workIT.Models.Search
{
	public class MainSearchInput
	{
		public MainSearchInput()
		{
			SearchType = "credential";
			StartPage = 1;
			PageSize = 50;
			Keywords = "";
			SortOrder = "relevance";
			Filters = new List<MainSearchFilter>();
			FiltersV2 = new List<MainSearchFilterV2>();
			ElasticConfigs = new List<ElasticSearchFilterConfig>();
		}
		public int WidgetId { get; set; }
		public bool MustHaveWidget { get; set; }
		public bool MustNotHaveWidget { get; set; }
		//TBD - value in passing the widget - or an object with properties significant for search filters
		public string SearchType { get; set; }
		public int StartPage { get; set; }
		public int PageSize { get; set; }
		public bool IncludingReferenceObjects { get; set; }
		public string Keywords { get; set; }
		public string CompetenciesKeywords { get; set; }
		public string SortOrder { get; set; }
		public bool UseSimpleSearch { get; set; }
		public bool UseSPARQL { get; set; }
		public List<MainSearchFilter> Filters { get; set; }
		public List<MainSearchFilterV2> FiltersV2 { get; set; }
		//yes, need to make this more general
		public bool HasCredentialPotentialResults { get; set; }
		/// <summary>
		/// Currently will only be used for the search related to the widget potential results.
		/// The search method can only handle 'known' filter types. At least one must be present, otherwise ignored
		/// </summary>
		public List<string> CustomSearchInFields { get; set; } = new List<string>();
		public List<ElasticSearchFilterConfig> ElasticConfigs { get; set; } //Used to tweak elasticsearch filters/boosting configs. Development only.

		public List<string> GetFilterValues_Strings( string name )
		{
			try
			{
				return Filters.FirstOrDefault( m => m.Name.ToLower() == name.ToLower() ).Items ?? new List<string>();
			}
			catch
			{
				return new List<string>();
			}
		}
		public List<int> GetFilterValues_Ints( string name )
		{
			try
			{
				return GetFilterValues_Strings( name ).Select( int.Parse ).ToList();
			}
			catch
			{
				return new List<int>();
			}
		}
		public List<string> GetFilterValues_Strings( int categoryID )
		{
			try
			{
				return Filters.FirstOrDefault( m => m.CategoryId == categoryID ).Items ?? new List<string>();
			}
			catch
			{
				return new List<string>();
			}
		}
		public List<int> GetFilterValues_Ints( int categoryID )
		{
			try
			{
				return GetFilterValues_Strings( categoryID ).Select( int.Parse ).ToList();
			}
			catch
			{
				return new List<int>();
			}
		}

		public bool Elastic { get; set; }
		public bool LoggingActivity { get; set; } = true;

		public int Results { get; set; }
	}
	//

	public enum MainSearchFilterV2Types { CODE, TEXT, FRAMEWORK, MAP, CUSTOM }
	public class MainSearchFilterV2
	{
		public MainSearchFilterV2()
		{
			Type = MainSearchFilterV2Types.CODE;
			Values = new Dictionary<string, object>();
		}
		[JsonConverter( typeof( StringEnumConverter ) )]
		public MainSearchFilterV2Types Type { get; set; }
		public string Name { get; set; }
		public Dictionary<string, object> Values { get; set; }
		public string CustomJSON { get; set; }

		//Convenience Methods
		public string GetValueOrDefault( string key, string defaultValue = "" )
		{
			try
			{
				return Values[ key ].ToString();
			}
			catch
			{
				return defaultValue;
			}

		}
		public int GetValueOrDefault( string key, int defaultValue = 0 )
		{
			try
			{
				return int.Parse( ( string ) Values[ key ] );
			}
			catch { }
			try
			{
				//for some reason, when called from the finder API, the integer like CategoryId are not recognized
				var v = Values[ key ];
				if ( v != null && int.TryParse( v.ToString(), out int goodInt ) )
				{
					return goodInt;
				}
				return ( int ) Values[ key ];
			}
			catch { }
			return defaultValue;
		}

		public bool GetBoolen( string key, bool defaultValue = false )
		{
			try
			{
				return bool.Parse( Values[ key ].ToString());
			}
			catch
			{
				return defaultValue;
			}
		}
		public List<int> GetListOrDefault( string key )
		{
			List<int> items = new List<int>();
			try
			{
				foreach ( var aa in Values.Keys )
				{
					if ( aa.Contains( key ) )
						items.Add( int.Parse( Values[ aa ].ToString() ) );
				}
				//items = ( List<int> ) Values[ key ];

				//var list = JsonConvert.DeserializeObject<List<int>>( item );
				return items;
			}
			catch ( Exception ex )
			{
			}

			return items;
		}
		public decimal GetValueOrDefault( string key, decimal defaultValue = 0m )
		{
			try
			{
				return decimal.Parse( ( string ) Values[ key ] );
			}
			catch { }
			try
			{
				return ( decimal ) Values[ key ];
			}
			catch { }
			return defaultValue;
		}
		public T GetValueOrDefault<T>( string key, T defaultValue = default( T ) )
		{
			try
			{
				return ( T ) Values[ key ];
			}
			catch
			{
				return defaultValue;
			}
		}
		public string AsText()
		{
			try
			{
				//char leveltype = '"';
				//var text = GetValueOrDefault( "TextValue", "" ).Replace("\"", "");
				return GetValueOrDefault( "TextValue", "" );
			}
			catch
			{
				return "";
			}
		}
		public CodeItem AsDateItem()
		{
			try
			{
				return new CodeItem()
				{
					Name = GetValueOrDefault( "PropertyName", "" ),
					Code = GetValueOrDefault( "TextValue", "" )
				};
			}
			catch
			{
				return new CodeItem();
			}
		}
		public CodeItem AsQaText()
		{
			try
			{
				return new CodeItem()
				{					
					Id = GetValueOrDefault( "Id", 0 ),
					Name = GetValueOrDefault( "TextValue", "" )
				};
			}
			catch
			{
				return new CodeItem();
			}
		}
		public CodeItem AsCodeItem()
		{
			try
			{
				return new CodeItem()
				{
					RelationshipId = GetValueOrDefault( "RelationshipId", 0 ),
					AssertionId = GetValueOrDefault( "AssertionId", 0 ),
					CategoryId = GetValueOrDefault( "CategoryId", 0 ),
					Id = GetValueOrDefault( "CodeId", 0 ),
					SchemaName = GetValueOrDefault( "SchemaName", "" ),
					Name = GetValueOrDefault( "Name", "" )
				};
			}
			catch
			{
				return new CodeItem();
			}
		}
		public CodeItem HasAnyValue()
		{
			try
			{
				return new CodeItem()
				{
					AnyValue = GetBoolen( "AnyValue", false ),
					SchemaName = GetValueOrDefault( "SchemaName", "" )
				};
			}
			catch
			{
				return new CodeItem();
			}
		}
		public Common.WidgetV2.LocationSet AsLocationSet()
		{
			var locationSet = new Common.WidgetV2.LocationSet();
			try
			{
				//
				//19-11-01 mp current approach doesn't work - only returns first value from a list
				string checkemptyCountries = GetValueOrDefault( "Countries[0]", "" );
				string checkemptyRegions = GetValueOrDefault( "Regions[0]", "" );
				string checkemptyCities = GetValueOrDefault( "Cities[0]", "" );

				if ( checkemptyCountries != "" )
				{
					locationSet.Countries = GetValueOrDefault( "Countries[0]", "" ).Split( ',' ).ToList();
					var next = "1";
					int cntr = 0;
					//ugly approach but is a quick fix not knowing the rational behind the input format
					do
					{
						cntr++;
						next = GetValueOrDefault( string.Format( "Countries[{0}]", cntr ), "" );
						if ( !string.IsNullOrWhiteSpace( next ) )
							locationSet.Countries.Add( next );

					} while ( next != "" );
				}
				if ( checkemptyRegions != "" )
				{
					locationSet.Regions = GetValueOrDefault( "Regions[0]", "" ).Split( ',' ).ToList();
					var next = "1";
					int cntr = 0;
					do
					{
						cntr++;
						next = GetValueOrDefault( string.Format( "Regions[{0}]", cntr ), "" );
						if ( !string.IsNullOrWhiteSpace( next ) )
							locationSet.Regions.Add( next );

					} while ( next != "" );
				}
				if ( checkemptyCities != "" )
				{
					//this only gets one value from a list, not all!
					locationSet.Cities = GetValueOrDefault( "Cities[0]", "" ).Split( ',' ).ToList();
					var next = "1";
					int cntr = 0;
					do
					{
						cntr++;
						next = GetValueOrDefault( string.Format( "Cities[{0}]", cntr ), "" );
						if ( !string.IsNullOrWhiteSpace( next ) )
							locationSet.Cities.Add( next );

					} while ( next != "" );

				}

				locationSet.IsAvailableOnline = GetValueOrDefault( "IsAvailableOnline", false );

				return locationSet;
			}
			catch
			{
				return locationSet;
			}
		}

		public CodeItem AsQaItem()
		{
			try
			{
				return new CodeItem()
				{
					RelationshipId = GetValueOrDefault( "RelationshipId", 0 ),
					Id = GetValueOrDefault( "AgentId", 0 ),
				};
			}
			catch
			{
				return new CodeItem();
			}
		}
		public CodeItem AsQapItem()
		{
			try
			{
				return new CodeItem()
				{
					AssertionId = GetValueOrDefault( "AssertionId", 0 ),
					Id = GetValueOrDefault( "AgentId", 0 ),
				};
			}
			catch
			{
				return new CodeItem();
			}
		}

		//extract a single orgId and one or more roles
		public CodeItem AsOrgRolesItem()
		{
			try
			{
				var item = new CodeItem()

				{
					IdsList = GetListOrDefault( "RelationshipId" ),

					Id = GetValueOrDefault( "AgentId", 0 )
				};
				item.ItemList = "[" + String.Join( ", ", item.IdsList.ToArray() ) + "]";
				return item;
			}
			catch
			{
				return new CodeItem();
			}
		}
		//string key
		//public bool AsBoolean(  )
		//{
		//	try
		//	{
		//		return GetBoolen( "AnyValue", false );
		//	}
		//	catch
		//	{
		//		return false;
		//	}
		//}
		public Models.Common.BoundingBox AsBoundaries()
		{
			try
			{
				return new Common.BoundingBox()
				{
					//String names are lowercase here, because they are taken directly from the Google Maps API
					North = GetValueOrDefault( "north", 0m ),
					East = GetValueOrDefault( "east", 0m ),
					South = GetValueOrDefault( "south", 0m ),
					West = GetValueOrDefault( "west", 0m )
				};
			}
			catch
			{
				return new Common.BoundingBox();
			}
		}
	}
	//


	public class MainSearchFilter
	{
		public MainSearchFilter()
		{
			Name = "";
			Items = new List<string>();
			Schemas = new List<string>();
			Texts = new List<string>();
			Data = new Dictionary<string, object>();
			Boundaries = new Common.BoundingBox();
		}
		public string Name { get; set; }
		public int CategoryId { get; set; }
		public List<string> Items { get; set; } //IDs
		public List<string> Schemas { get; set; } //SchemaNames
		public List<string> Texts { get; set; } //Labels and text strings
		public Dictionary<string, object> Data { get; set; } //Other types of data
		public Common.BoundingBox Boundaries { get; set; }
	}
	//

	public class MainSearchResults
	{
		public MainSearchResults()
		{
			TotalResults = 0;
			Results = new List<MainSearchResult>();
			Debug = new JObject();
			RelatedItems = new JArray();
			RelatedItemsMap = new JArray();
		}

		public string SearchType { get; set; }
		public int TotalResults { get; set; }
		public List<MainSearchResult> Results { get; set; }
		public JArray RelatedItems { get; set; }
		public JArray RelatedItemsMap { get; set; }
		public JObject Debug { get; set; }
	}
	//

	public class MainSearchResult
	{
		public MainSearchResult()
		{
			Properties = new Dictionary<string, object>();
			Tags = new List<TagSet>();
			TagsV2 = new List<Helpers.SearchTag>();
			Buttons = new List<Helpers.SearchResultButton>();
		}
		public string Name { get; set; }
		public string FriendlyName { get; set; }
		public string Description { get; set; }
		public int RecordId { get; set; }
		public Dictionary<string, object> Properties { get; set; }
		public List<TagSet> Tags { get; set; }
		public List<Models.Helpers.SearchTag> TagsV2 { get; set; } //In development
		public List<Models.Helpers.SearchResultButton> Buttons { get; set; }
	}
	//

	public class TagSet
	{
		public TagSet()
		{
			Items = new List<TagItem>();
			CostItems = new List<CostTagItem>();
		}
		public string Schema { get; set; } //industry, occupation, etc
		public string Method { get; set; } //embedded, ajax, link
		public string Label { get; set; }
		public int CategoryId { get; set; }
		public List<TagItem> Items { get; set; }
		public List<CostTagItem> CostItems { get; set; }
		public List<FinancialTagItem> FinancialItems { get; set; } = new List<FinancialTagItem>();
		public List<EntityTagItem> EntityTagItems { get; set; } = new List<EntityTagItem>();
		public List<QAPerformedTagItem> QAItems { get; set; } = new List<QAPerformedTagItem>();
		public int Count { get; set; }
	}
	//

	public class TagItem
	{
		public int CodeId { get; set; } //Should be the record integer ID from the code table itself
		public string Schema { get; set; }  //Used when CodeId is not viable
		public string Label { get; set; } //Used when all else fails
		public string Description { get; set; }
	}
	//
	public class CostTagItem
	{
		public int CodeId { get; set; } //CostProfileId
		public decimal Price { get; set; }
		public string CostType { get; set; }
		public string CurrencySymbol { get; set; }
		public string SourceEntity { get; set; }
	}
	public class FinancialTagItem
	{
		public int CodeId { get; set; } //FinancialAssistanceId
		public string Label { get; set; } //Used when all else fails
		public string AssistanceTypes{ get; set; }

		public string Description { get; set; }
	}
	public class EntityTagItem
	{
		public int TargetEntityBaseId { get; set; }
		public string TargetEntityType { get; set; }
		public string TargetEntityName { get; set; }
		public string TargetEntitySubjectWebpage { get; set; }
		public bool IsReference { get; set; }
		public int TargetEntityTypeId { get; set; }
	}
	public class QAPerformedTagItem
	{
		public int TargetEntityBaseId { get; set; }
		public int AssertionTypeId { get; set; }
		public string AgentToTargetRelationship { get; set; }
		public string TargetEntityType { get; set; }
		public string TargetEntityName { get; set; }
		public string TargetEntitySubjectWebpage { get; set; }
		public bool IsReference { get; set; }
		public int TargetEntityTypeId { get; set; }
	}
	//
	//Used by EnumerationFilter partial
	public class HtmlEnumerationFilterSettings
	{
		public HtmlEnumerationFilterSettings()
		{
			FilterSchema = "";
			CssClasses = new List<string>();
			Enumeration = new Common.Enumeration();
			PreselectedFilters = new Dictionary<int, List<int>>();
		}

		public List<string> CssClasses { get; set; }
		public string SearchType { get; set; }
		public string FilterName { get; set; }
		public string FilterLabel { get; set; }
		public int CategoryId { get; set; }
		public Models.Common.Enumeration Enumeration { get; set; }
		public Dictionary<int, List<int>> PreselectedFilters { get; set; }
		public string Guidance { get; set; }
		public string FilterSchema { get; set; }
		public bool ShowDescriptions { get; set; }
	}
	//

	//Used for micro searches that are used as filters on the search page
	public class MicroSearchFilterSettings
	{
		public MicroSearchFilterSettings()
		{
			InputTitle = "";
			SelectedTitle = "";
			PageSize = 5;
			IncludeKeywords = true;
			ParentSearchType = "";
			FilterName = "";
			MicroSearchType = "";
			FilterSchema = "";
			Filters = new List<MicroSearchSettings_FilterV2>();
			PreselectedFilters = new Dictionary<int, List<int>>();
		}

		public string InputTitle { get; set; }
		public string SelectedTitle { get; set; }
		public bool IncludeKeywords { get; set; }
		public int PageSize { get; set; }
		public string ParentSearchType { get; set; }
		public string FilterName { get; set; }
		public int CategoryId { get; set; }
		public string MicroSearchType { get; set; }
		public List<MicroSearchSettings_FilterV2> Filters { get; set; }
		public Dictionary<int, List<int>> PreselectedFilters { get; set; }
		public string Guidance { get; set; }
		public string TagTitle { get; set; }
		public string FilterSchema { get; set; }
	}
	//

	public class TextFilterSettings
	{
		public TextFilterSettings()
		{
			InputTitle = "";
			Guidance = "";
			SearchType = "";
			FilterName = "";
			Fields = new List<string>();
			Placeholder = "Search...";
			FilterSchema = "";
		}

		public string InputTitle { get; set; }
		public string TagTitle { get; set; }
		public string Guidance { get; set; }
		public string SearchType { get; set; }
		public string FilterName { get; set; }
		public string CategoryId { get; set; }
		public List<string> Fields { get; set; }
		public string Placeholder { get; set; }
		public string FilterSchema { get; set; }
	}
	//

	public class ElasticSearchFilterConfig
	{
		public string SearchType { get; set; }
		public string QueryType { get; set; }
		public string QuerySubType { get; set; }
		public string FieldName { get; set; }
		public int Boost { get; set; }
	}
	//
}

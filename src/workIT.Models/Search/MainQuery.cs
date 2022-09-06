using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace workIT.Models.Search
{

	public class MainQuery
	{
		public string Keywords { get; set; }
		public List<Filter> MainFilters { get; set; }
		public MapFilter MapFilter { get; set; }
		public string SearchType { get; set; }
		public int SkipPages { get; set; }
		public int PageSize { get; set; }
		public string SortOrder { get; set; }
		public WidgetFilter WidgetFilter { get; set; }
		public string AutocompleteContext { get; set; }
	}

	public class AutoCompleteQuery
	{
		[JsonProperty( "@type" )]
		public string Type { get; set; } = "AutoCompleteQuery";

		/// <summary>
		/// Example: credential
		/// </summary>
		public string SearchType { get; set; }

		/// <summary>
		/// URI for the associated Filter. Provided by the Filter that contains the autocomplete text box. (Filter.URI)
		/// example:filter:OccupationType
		/// </summary>
		public string FilterURI { get; set; }
		/// <summary>
		/// URI for the associated FilterItem. Provided by the FilterItem that was used to render this autocomplete text box. (FilterItem.URI)
		/// example: interfaceType:TextValue
		/// </summary>
		public string FilterItemURI { get; set; }

		public string Text { get; set; }
		public int WidgetId { get; set; }
	}
	public class AutoCompleteResponse
	{
		public List<FilterItem> Items { get; set; }
	}

	//
	/// <summary>
	/// AJAXSettings
	/// </summary>
	public class AJAXSettings
	{
		//[JsonProperty( "@type" )]
		//public string Type { get; set; } = "AjaxQueryQuery";

		/// <summary>
		/// Example: credential
		/// May not be necessary if URL has the necessary info
		/// </summary>
		public string SearchType { get; set; }

		public string Label { get; set; }
		public string URL { get; set; }
		public string TestURL { get; set; }
		public string Description { get; set; }
		public int? Total { get; set; }
		public List<object> Values { get; set; }
		public object QueryData { get; set; }

		/* - won't be used if using QueryData
		/// <summary>
		/// URI for the associated Filter. Provided by the Filter that contains the autocomplete text box. (Filter.URI)
		/// example:filter:OccupationType
		/// </summary>
		public string FilterURI { get; set; }
		/// <summary>
		/// URI for the associated FilterItem. Provided by the FilterItem that was used to render this autocomplete text box. (FilterItem.URI)
		/// example:filterItem:TextValue
		/// </summary>
		public string FilterItemURI { get; set; }

		public string Text { get; set; }
		public int? WidgetId { get; set; }
		*/
	}
	//
	public class MapQuery
	{
		public string Text { get; set; }
	}
	//
	public class MapResponse
	{
		public List<MapFilter> Items { get; set; }
	}
	//
	public class MapFilter
	{
		public double BBoxCenterLatitude { get; set; }
		public double BBoxCenterLongitude { get; set; }
		public double BBoxNorth { get; set; }
		public double BBoxEast { get; set; }
		public double BBoxSouth { get; set; }
		public double BBoxWest { get; set; }
		public string Label { get; set; } //Will be a city, state, or some other equivalent name of the result
		public string Region { get; set; }
		public string Country { get; set; }
		public string PositionType { get; set; }
		public int RadiusMiles { get; set; }
	}
	//

	public class WidgetFilter
	{
		public int WidgetId { get; set; }
	}
	//
	public class MainQueryResponse
	{
		public MainQueryResponse()
		{
			Results = new List<JObject>();
		}
		public int TotalResults { get; set; }
		public List<JObject> Results { get; set; }
		public JObject Debug { get; set; }
	}
}

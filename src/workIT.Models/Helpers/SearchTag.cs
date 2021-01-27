using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace workIT.Models.Helpers
{
	//The gray box things used on search results
	public class SearchTag
	{
		public SearchTag()
		{
			Items = new List<SearchTagItem>();
			AjaxQueryValues = new Dictionary<string, object>();
		}
		public string Name { get; set; }
		public string CategoryName { get; set; }
		public string CategoryLabel { get; set; }
		public string DisplayTemplate { get; set; }
		public int TotalItems { get; set; }
		public List<SearchTagItem> Items { get; set; }
		public string SearchQueryType { get; set; }
		public string Display { get { return SearchTagHelper.Count( DisplayTemplate, TotalItems ); } }
		public bool IsAjaxQuery { get; set; }
		public string AjaxQueryName { get; set; }
		public Dictionary<string, object> AjaxQueryValues { get; set; }
	}
	//

	public class SearchTagItem
	{
		public SearchTagItem()
		{
			QueryValues = new Dictionary<string, object>();
		}
		public string Display { get; set; }
		public Dictionary<string, object> QueryValues { get; set; }
	}
	//

	public static class SearchTagHelper
	{
		public static string Count( string template, int count )
		{
			return template
				.Replace( "{#}", count.ToString() )
				.Replace( "{ies}", count == 1 ? "y" : "ies" )
				.Replace( "{s}", count == 1 ? "" : "s" );
		}
	}
	//

	//aka SearchTagV3
	public class SearchResultButton 
	{
		public string HandlerType { get; set; } //Tells the client which rendering method to use to handle this Button
		public string CategoryType { get; set; } //Enables the search to identify what this Button is, which is useful for some filter stuff
		public string CategoryLabel { get; set; } //Text on the button that displayed to the user (unless overridden in the RenderHandler)
		public int TotalItems { get; set; } //Count of total items for the button (i.e. the "35" in "teaches 35 competencies" even if only the first 10 are included)
		public JObject RenderData { get; set; } //Data, if any, to pass to the client-side javascript that renders this item (for use with ajax queries and whatnot)
		public List<JObject> Items { get; set; } //Items, if any, that are immediately passed along with this Search Result Button 


		//Helpers for common types of rendering
		public class Helpers
		{
			public class BasicItem
			{
				public string ItemLabel { get; set; }
			}
			//

			public class DetailPageLink //e.g. Detail Page, either for this item directly or for a related item
			{
				public string ConnectionLabel { get; set; } //e.g. Is Preparation For
				public string ConnectionType { get; set; } //Internal use, e.g. 123
				public string TargetLabel { get; set; } //e.g. Bachelor Degree of ABC
				public string TargetType { get; set; } //e.g. credential
				public int TargetId { get; set; } //e.g. 123
				public string TargetCTID { get; set; } //e.g. ce-...
				public string TargetFriendlyURL { get; set; } //e.g. bachelor_degree_of_abc
			}
			//

			public class FilterItem : BasicItem //e.g. Audience Type
			{
				public int CategoryId { get; set; } //e.g. 123
				public int ItemCodeId { get; set; } //e.g. 123
				public string SchemaURI { get; set; } //e.g. audLevel:Beginner
				public string ItemCode { get; set; } //e.g. 49-9041.00
				public string ItemCodeTitle { get; set; } //e.g. Industrial Machinery Mechanics (49-9041.00)
			}
			//

			public class AjaxDataForResult //e.g. Related things for some origin, which is probably always the search result itself
			{
				public string AjaxQueryName { get; set; } //The name of the query to do to get the data
				public string SearchType { get; set; } //The SearchType of the origin item (e.g. the "Credential" in "Credential requires x Competencies"). This defaults to the search type of the main item.
				public int RecordId { get; set; } //ID of the origin item. This defaults to the ID of the main item.
				public string TargetEntityType { get; set; } //The type of item to query for related to the main item (e.g. the "Competencies" in "Credential requires x Competencies")
			}
			//

		}
		//
	}
	//


}

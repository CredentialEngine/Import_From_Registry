using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}

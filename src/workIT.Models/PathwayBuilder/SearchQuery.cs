using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
 * probably don't need this
 * */
namespace workIT.Models.PathwayBuilder
{
    public class SearchQuery
    {
		public SearchQuery()
		{
			Filters = new List<SearchFilter>();
			Skip = 0;
			Take = 15;
		}

		public string Keywords { get; set; }
		public List<SearchFilter> Filters { get; set; }
		public int Skip { get; set; }
		public int Take { get; set; }
		public string Sort { get; set; }
		public string ContextURI { get; set; }

		public List<string> GetFilterItemTexts( string filterURI )
		{
			return Filters?.FirstOrDefault( filter => filter.URI?.ToLower() == filterURI?.ToLower() )?.ItemTexts;
		}

		public List<int> GetFilterItemIDs( string filterURI )
		{
			return Filters?.FirstOrDefault( filter => filter.URI?.ToLower() == filterURI?.ToLower() )?.ItemIDs;
		}
	}
	//

	public class SearchFilter
	{
		public SearchFilter()
		{
			ItemTexts = new List<string>();
			ItemIDs = new List<int>();
		}

		public string URI { get; set; }
		public List<string> ItemTexts { get; set; }
		public List<int> ItemIDs { get; set; }
	}
	//

	public class SearchResponse<T>
	{
		public SearchResponse() {
			Results = new List<T>();
			Debug = new JObject();
		}

		public int TotalResults { get; set; }
		public List<T> Results { get; set; }
		public List<JObject> RelatedResources { get; set; }
		public JObject Debug { get; set; }
	}
	//

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace workIT.Models.API
{
	public class SearchResult
	{
		//TBD
	}
	//

	[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
	public class TagSet
	{
		public int Total { get; set; }
		public string Icon { get; set; }
		public string Label { get; set; }
		public string TagItemType { get; set; }
		public List<TagItem> Values { get; set; }
		public string URL { get; set; }
		public TagSetRequest QueryData { get; set; }
	}
	//

	[JsonObject( ItemNullValueHandling = NullValueHandling.Ignore )]
	public class TagItem
	{
		public string Label { get; set; }
		public string URL { get; set; }
		public int? FilterID { get; set; }
		public int? FilterItemID { get; set; }
		public string FilterItemText { get; set; }
	}
	//

	public class TagSetRequest
	{
		public string TargetType { get; set; }
		public string SearchType { get; set; }
		public int RecordId { get; set; }
	}
	//

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace workIT.Models.DescriptionSet
{
	public class DescriptionSetResult
	{
		public DescriptionSetResult()
		{
			RelatedItems = new List<JObject>();
			RelatedItemsMap = new List<RelatedItemsMapItem>();
			DebugInfo = new JObject();
		}
		public List<JObject> RelatedItems { get; set; }
		public List<RelatedItemsMapItem> RelatedItemsMap { get; set; }
		public JObject DebugInfo { get; set; }
	}
	//

	public class RelatedItemsMapItem
	{
		public RelatedItemsMapItem()
		{
			RelatedItems = new List<RelatedItemsMapItemPathAndURIs>();
		}
		public string ResourceURI { get; set; }
		public List<RelatedItemsMapItemPathAndURIs> RelatedItems { get; set; }
	}
	//

	public class RelatedItemsMapItemPathAndURIs
	{
		public RelatedItemsMapItemPathAndURIs()
		{
			URIs = new List<string>();
		}
		public string Path { get; set; }
		public int TotalURIs { get; set; }
		public List<string> URIs { get; set; }
	}
	//
}

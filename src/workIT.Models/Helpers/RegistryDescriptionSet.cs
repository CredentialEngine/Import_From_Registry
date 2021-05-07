using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace workIT.Models.Helpers
{
	public class RegistryDescriptionSetResponse
	{
		public bool Valid { get; set; }
		public List<string> Messages { get; set; } = new List<string>();
		public JObject Debug { get; set; } = new JObject();
		public RegistryDescriptionSet Data { get; set; } = new RegistryDescriptionSet();
	}
	//

	public class RegistryDescriptionSet
	{
		[JsonIgnore] //The Results need to be extracted programmatically from the raw data
		public List<JObject> Results { get; set; } = new List<JObject>();

		public int TotalResults { get; set; }

		[JsonProperty( PropertyName = "description_sets" )]
		public List<JObject> DescriptionSets { get; set; } = new List<JObject>();

		[JsonProperty( PropertyName = "description_set_resources" )]
		public List<JObject> DescriptionSetResources { get; set; } = new List<JObject>();
	}
	//
}

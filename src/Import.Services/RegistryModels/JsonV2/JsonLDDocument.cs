using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
		public class JsonLDDocument 
	{   
		public JsonLDDocument()
		{
			Context = "https://credreg.net/ctdl/schema/context/json";
		}
        [JsonIgnore]
        [JsonProperty( "@context" )]
		public string Context { get; set; }
		//public Dictionary<string, object> Context { get; set; }

		[JsonProperty( PropertyName = "ceterms:dateModified" )]
		public string LastUpdated { get; set; } = null;
	}
}

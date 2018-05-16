using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace RA.Models.Json
{
		public class JsonLDDocument 
	{   
		public JsonLDDocument()
		{
			Context = "http://credreg.net/ctdl/schema/context/json";
		}
		[JsonProperty( "@context" )]
		public string Context { get; set; }
		//public Dictionary<string, object> Context { get; set; }
	}
}

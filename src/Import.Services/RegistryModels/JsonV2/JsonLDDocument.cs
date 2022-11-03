using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
	/// <summary>
	/// Base resource document
	/// </summary>
	public class BaseResourceDocument 
	{   
		/// <summary>
		/// constructor
		/// </summary>
		public BaseResourceDocument()
		{
			Context = "https://credreg.net/ctdl/schema/context/json";
		}
        [JsonIgnore]
        [JsonProperty( "@context" )]
		public string Context { get; set; }
        //public Dictionary<string, object> Context { get; set; }

        [JsonProperty( "schema:datePublished" )]
        public string DatePublished { get; set; } = null;
    }
}

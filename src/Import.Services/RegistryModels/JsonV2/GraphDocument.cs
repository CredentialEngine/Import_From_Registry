using System.Collections.Generic;
using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
	public class GraphDocument
	{

		public GraphDocument()
		{
			//needs to be set by caller! or overridden
			//Context = "https://credreg.net/ctdlasn/schema/context/json";
		}
		[JsonProperty( "@context" )]
		public string Context { get; set; }

		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }

		/// <summary>
		/// Main graph object
		/// </summary>
		[JsonProperty( "@graph" )]
		public object Graph { get; set; }

	}
}

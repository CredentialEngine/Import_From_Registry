using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace workIT.Models.API
{
	[JsonObject( ItemNullValueHandling = NullValueHandling.Ignore )]
	public class ConceptScheme : BaseAPIType
	{
		public ConceptScheme()
		{
			EntityTypeId = 11;
			BroadType = "ConceptScheme";
			CTDLType = "ceasn:ConceptScheme";
			CTDLTypeLabel = "Concept Scheme";
		}
		public string Source { get; set; }
	}
	public class Concept 
	{
		/// <summary>
		/// CTID - identifier for concept. 
		/// Format: ce-UUID (lowercase)
		/// example: ce-a044dbd5-12ec-4747-97bd-a8311eb0a042
		/// </summary>
		public string CTID { get; set; }

		/// <summary>
		/// Concept 
		/// Required
		/// </summary>
		public string PrefLabel { get; set; }

		/// <summary>
		/// Concetpt description 
		/// Required
		/// </summary>
		public string Definition { get; set; }

		public bool IsTopConcept { get; set; }

	}
}

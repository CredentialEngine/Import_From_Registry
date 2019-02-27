using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
	public class CostManifest : JsonLDDocument
	{
		[JsonIgnore]
		public static string classType = "ceterms:CostManifest";
		public CostManifest()
		{
			Type = "ceterms:CostManifest";
		}

		/// <summary>
		/// entity type
		/// </summary>
		[JsonProperty( "@type" )]
		public string Type { get; set; }

		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }

		[JsonProperty( PropertyName = "ceterms:ctid" )]
		public string Ctid { get; set; }

		[JsonProperty( PropertyName = "ceterms:name" )]
		public LanguageMap Name { get; set; }

		[JsonProperty( PropertyName = "ceterms:description" )]
		public LanguageMap Description { get; set; }

		[JsonProperty( PropertyName = "ceterms:costManifestOf" )]
		public List<string> CostManifestOf { get; set; }

		[JsonProperty( PropertyName = "ceterms:costDetails" )]
		public string CostDetails { get; set; } //URL

		[JsonProperty( PropertyName = "ceterms:startDate" )]
		public string StartDate { get; set; }

		[JsonProperty( PropertyName = "ceterms:endDate" )]
		public string EndDate { get; set; }

		[JsonProperty( PropertyName = "ceterms:estimatedCost" )]
		public List<CostProfile> EstimatedCost { get; set; }
	}
}

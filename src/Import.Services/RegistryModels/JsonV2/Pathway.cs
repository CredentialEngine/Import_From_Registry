using System.Collections.Generic;

using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
	public class Pathway : JsonLDDocument
	{
		public Pathway()
		{
			Type = "ceterms:Pathway";
		}

		[JsonProperty( "@type" )]
		public string Type { get; set; }

		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }

		[JsonProperty( PropertyName = "ceterms:ctid" )]
		public string CTID { get; set; }

		[JsonProperty( PropertyName = "ceterms:name" )]
		public LanguageMap Name { get; set; }

		[JsonProperty( PropertyName = "ceterms:description" )]
		public LanguageMap Description { get; set; }

		/// <summary>
		/// This property identifies a child pathwayComponent(s) in the downward path.
		/// </summary>
		[JsonProperty( PropertyName = "ceasn:hasChild" )]
		public List<string> HasChild { get; set; } 

		/// <summary>
		/// Goal or destination node of the pathway. 
		/// URI for a ceterms:PathwayComponent
		/// Multipicity: Single??
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:hasDestinationComponent" )]
		public List<string> HasDestinationComponent { get; set; }


		/// <summary>
		/// This property identifies all pathway components for a pathway
		/// </summary>
		[JsonProperty( PropertyName = "ceasn:hasPart" )]
		public List<string> HasPart { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "asn:hasProgressionModel" )]
		public string HasProgressionModel{ get; set; }

		[JsonProperty( PropertyName = "ceterms:industryType" )]
		public List<CredentialAlignmentObject> IndustryType { get; set; } = new List<CredentialAlignmentObject>();

		//
		[JsonProperty( PropertyName = "ceterms:keyword" )]
		public LanguageMapList Keyword { get; set; }

		[JsonProperty( PropertyName = "ceterms:occupationType" )]
		public List<CredentialAlignmentObject> OccupationType { get; set; } = new List<CredentialAlignmentObject>();

		[JsonProperty( PropertyName = "ceterms:ownedBy" )]
		public List<string> OwnedBy { get; set; }

		[JsonProperty( PropertyName = "ceterms:offeredBy" )]
		public List<string> OfferedBy { get; set; }

		//
		[JsonProperty( PropertyName = "ceterms:subject" )]
		public List<CredentialAlignmentObject> Subject { get; set; }

		/// <summary>
		/// The webpage that describes this pathway.
		/// URL
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:subjectWebpage" )]
		public string SubjectWebpage { get; set; }

	}

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using MC = workIT.Models.Common;
using WMS = workIT.Models.Search;

namespace workIT.Models.API
{
	[JsonObject( ItemNullValueHandling = NullValueHandling.Ignore )]

	public class Pathway : BaseAPIType
	{
		public static int PathwayRelationship_IsDestinationComponentOf = 1;
		public static int PathwayRelationship_HasChild = 2;
		public static int PathwayRelationship_IsPartOf = 3;

		public enum PathwayRelationships
		{
			UNKNOWN = 0,
			IsDestinationComponentOf = 1,
			HasChild = 2
			//IsPartOf = 3
		}
		public Pathway()
		{
			EntityTypeId = 8;
			CTDLTypeLabel = "Pathway";
			CTDLType = "ceterms:Pathway";
			BroadType = "Pathway";
		}
		//public string CTDLTypeLabel { get; set; }
		public int PathwayRelationshipTypeId { get; set; }

		//public PathwaySet PathwaySet { get; set; } = new PathwaySet();
		////public string ExternalIdenifier { get; set; }
		////list of all component
		//would it make sense to just use outline for HasChild and destinationComponent. The details would be found in Meta_HasPart. Although is the latter less accessible from AjaxSettings?
		public WMS.AJAXSettings HasChild { get; set; }
		public WMS.AJAXSettings HasDestinationComponent { get; set; } 
		/// <summary>
		/// ConceptScheme or a ProgressionModel (TBD)
		/// URI
		/// </summary>
		public ConceptScheme HasProgressionModel { get; set; }
		public string ProgressionModelURI { get; set; }
		//??

		public List<ReferenceFramework> OccupationType { get; set; } = new List<ReferenceFramework>();

		public WMS.AJAXSettings Meta_HasPart { get; set; }
		public List<ReferenceFramework> IndustryType { get; set; } = new List<ReferenceFramework>();
		public List<LabelLink> Keyword { get; set; } = new List<LabelLink>();

		public List<LabelLink> Subject { get; set; } = new List<LabelLink>();


	}
	public class PathwayComponent : MC.PathwayComponent
	{
	}
}

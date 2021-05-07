using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.API
{
	public class Pathway : BaseDisplay
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
		}
		//public string CTDLTypeLabel { get; set; }
		public int PathwayRelationshipTypeId { get; set; }

		//public PathwaySet PathwaySet { get; set; } = new PathwaySet();
		////public string ExternalIdenifier { get; set; }
		////list of all component
		//public List<PathwayComponent> HasPart { get; set; } = new List<PathwayComponent>();
		//public List<PathwayComponent> HasChild { get; set; } = new List<PathwayComponent>();
		//public List<PathwayComponent> HasDestinationComponent { get; set; } = new List<PathwayComponent>();
		///// <summary>
		///// ConceptScheme or a ProgressionModel (TBD)
		///// URI
		///// </summary>
		//public ConceptScheme HasProgressionModel { get; set; } = new ConceptScheme();
		public string ProgressionModelURI { get; set; }
		//??

		public List<LabelLink> OccupationType { get; set; } = new List<LabelLink>();
		public List<LabelLink> InstructionalProgramType { get; set; } = new List<LabelLink>();
		public List<LabelLink> Keyword { get; set; } = new List<LabelLink>();

		public List<LabelLink> Subject { get; set; } = new List<LabelLink>();


	}


}

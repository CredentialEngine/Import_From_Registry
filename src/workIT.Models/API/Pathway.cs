using Newtonsoft.Json;

using System.Collections.Generic;

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
		/// <summary>
		/// If true, show link for pathway display, else not
		/// </summary>
        public bool AllowUseOfPathwayDisplay { get; set; }

        public int PathwayRelationshipTypeId { get; set; }

		//public PathwaySet PathwaySet { get; set; } = new PathwaySet();
		////public string ExternalIdenifier { get; set; }
		////list of all component
		//would it make sense to just use outline for HasChild and destinationComponent. The details would be found in Meta_HasPart. Although is the latter less accessible from AjaxSettings?
		public WMS.AJAXSettings HasChild { get; set; }
		public WMS.AJAXSettings HasDestinationComponent { get; set; }
        //not sure of approach 
        public WMS.AJAXSettings HasSupportService { get; set; }

        /// <summary>
        /// ConceptScheme or a ProgressionModel (TBD)
        /// URI
        /// </summary>
        public ConceptScheme HasProgressionModel { get; set; }
		public string ProgressionModelURI { get; set; }
		//??

		public List<ReferenceFramework> OccupationType { get; set; } = new List<ReferenceFramework>();
		public List<ReferenceFramework> InstructionalProgramType { get; set; } = new List<ReferenceFramework>();

		public WMS.AJAXSettings Meta_HasPart { get; set; }
		public List<ReferenceFramework> IndustryType { get; set; } = new List<ReferenceFramework>();
		public List<LabelLink> Keyword { get; set; } = new List<LabelLink>();

		public List<LabelLink> Subject { get; set; } = new List<LabelLink>();

		public LabelLink LatestVersion { get; set; }
		public LabelLink NextVersion { get; set; } //URL
		public LabelLink PreviousVersion { get; set; }
		public List<IdentifierValue> VersionIdentifier { get; set; }

	}
	public class PathwayComponent : BaseAPIType
    {
        public PathwayComponent()
        {
            EntityTypeId = 24;
            CTDLTypeLabel = "PathwayComponent";
            CTDLType = "ceterms:PathwayComponent";
            BroadType = "PathwayComponent";
        }
        //public string BroadType { get; set; }
        //public string CTDLType { get; set; }
        //public string CTDLTypeLabel { get; set; }

        /// <summary>
        /// If true, show link for pathway display, else not
        /// </summary>
        public bool AllowUseOfPathwayDisplay { get; set; }
        public string CodedNotation { get; set; }
        public List<string> ComponentDesignationList { get; set; } = new List<string>();
        public string PathwayCTID { get; set; }
        public string PathwayComponentType { get; set; }

        //      public int? Meta_Id { get; set; }
        //      public string Meta_FriendlyName { get; set; }
        //      public int? Meta_StateId { get; set; }
        //      public string Meta_Language { get; set; } = "en";
        public WMS.AJAXSettings Pathway { get; set; }
        //the main pathway page expects this to the registry URL for the pathway
        public string IsDestinationComponentOf { get; set; }

        //      public string CredentialRegistryURL { get; set; }
        //      //TBD - replace with a list
        //      public RegistryData RegistryData { get; set; } = new RegistryData();

        public string ComponentCategory { get; set; }
        public string CredentialType { get; set; }
        public List<ValueProfile> CreditValue { get; set; } = new List<ValueProfile>();
        public List<IdentifierValue> Identifier { get; set; } = new List<IdentifierValue>();
        public QuantitativeValue PointValue { get; set; } = new QuantitativeValue();

        // May just be a resourceSummary?
        public List<PathwayComponent> PrecededBy { get; set; } = new List<PathwayComponent>();

        /// <summary>
        /// May just be a resourceSummary?
        /// </summary>
        public List<PathwayComponent> Precedes { get; set; } = new List<PathwayComponent>();
        public string ProgramTerm { get; set; }
        public WMS.AJAXSettings ProxyFor { get; set; }

        public List<ReferenceFramework> OccupationType { get; set; } = new List<ReferenceFramework>();

        public List<ReferenceFramework> IndustryType { get; set; } = new List<ReferenceFramework>();

    }
}

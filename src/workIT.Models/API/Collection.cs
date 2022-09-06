using System.Collections.Generic;

using Newtonsoft.Json;

using WMA = workIT.Models.API;
using WMS = workIT.Models.Search;

namespace workIT.Models.API
{
	[JsonObject( ItemNullValueHandling = NullValueHandling.Ignore )]
	public class Collection : BaseAPIType
	{
		public Collection()
		{
			EntityTypeId = 9;
			BroadType = "Collection";
			CTDLType = "ceasn:Collection";
			CTDLTypeLabel = "Collection";
			//???
			Meta_HasPart = new List<Competency>();
		}
		public List<Competency> Meta_HasPart { get; set; }
		//public WMS.AJAXSettings OwnedBy { get; set; }

		/// <summary>
		/// Category or classification of this resource.
		/// List of URIs that point to a concept
		/// </summary>
		public List<string> Classification { get; set; } = new List<string>();

		public string CodedNotation { get; set; }
		public List<LabelLink> CollectionType { get; set; }
		public string DateEffective { get; set; }
		public string ExpirationDate { get; set; }
		public int HasMemberCount { get; set; }
		
		public List<LabelLink> Keyword { get; set; } = new List<LabelLink>();
		public string License { get; set; }
		public LabelLink LifeCycleStatusType { get; set; }
		public List<WMA.ConditionProfile> MembershipCondition { get; set; }
		//
		public List<ReferenceFramework> InstructionalProgramType { get; set; } = new List<ReferenceFramework>();
		public List<ReferenceFramework> IndustryType { get; set; } = new List<ReferenceFramework>();
		public List<ReferenceFramework> OccupationType { get; set; } = new List<ReferenceFramework>();
		
	
	}


}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WMS = workIT.Models.Search;

namespace workIT.Models.API
{
	[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
	public class CompetencyFramework : BaseAPIType
	{
		public CompetencyFramework()
		{
			EntityTypeId = 10;
			BroadType = "CompetencyFramework";
			CTDLType = "ceasn:CompetencyFramework";
			CTDLTypeLabel = "Competency Framework";
			Meta_HasPart = new List<Competency>();
		}
		public string Source { get; set; }

		public List<string> HasTopChild { get; set; }
		public List<Competency> Meta_HasPart { get; set; }
		public WMS.AJAXSettings Creator { get; set; }
		public WMS.AJAXSettings Publisher { get; set; }
		public WMS.AJAXSettings RightsHolder { get; set; }
	}

	//don't inherit from BaseDisplay for cleaner use in display
	public class Competency		//: BaseDisplay
	{
		public Competency()
		{
			//BroadType = "Competency";
			//CTDLType = "ceasn:Competency";
			//CTDLTypeLabel = "Competency";
			HasChild = new List<string>();
		}
		//public string BroadType { get; set; }
		//public string CTDLType { get; set; }
		//public string CTDLTypeLabel { get; set; }

		public string CredentialRegistryURL { get; set; }
		public string CTID { get; set; }
		public string CompetencyLabel { get; set; }
		public string CompetencyText { get; set; }
		public string Comment { get; set; }
		public string CodedNotation { get; set; }
		public string CompetencyCategory { get; set; }
		public string ListID { get; set; }

		public List<string> HasChild { get; set; } = new List<string>();
		public List<Competency> Meta_HasChild { get; set; } = new List<Competency>();
		public bool? Meta_IsReferenced { get; set; }
		public RegistryData RegistryData { get; set; } = new RegistryData();

	}

	public class CompetencyAlignmentSet
	{
		public List<CompetencyFramework> RequiresCompetencies { get; set; }
		public List<CompetencyFramework> AssessesCompetencies { get; set; }
		public List<CompetencyFramework> TeachesCompetencies { get; set; }
		public List<JObject> Debug { get; set; }
	}
}

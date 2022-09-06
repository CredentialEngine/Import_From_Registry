using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.API
{
	public class ConditionManifest : BaseAPIType
	{
		public ConditionManifest()
		{
			CTDLTypeLabel = "Condition Manifest";
			EntityTypeId = 19;
		}
		//public string CTDLTypeLabel { get; set; }
		//public int Id { get; set; }

		//public string Name { get; set; }

		//public string Description { get; set; }

		//public string CTID { get; set; }

		////URL
		//public string SubjectWebpage { get; set; }

		public List<ConditionProfile> Corequisite { get; set; } = new List<ConditionProfile>();
		public List<ConditionProfile> EntryCondition { get; set; }
		public List<ConditionProfile> Recommends { get; set; }
		public List<ConditionProfile> Renewal { get; set; }
		public List<ConditionProfile> Requires { get; set; }

		public bool DisplayAdditionalInformation { get; set; }
	}
}

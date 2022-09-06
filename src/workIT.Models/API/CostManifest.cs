using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ME = workIT.Models.Elastic;

namespace workIT.Models.API
{
	[Serializable]
	public class CostManifest : BaseAPIType
	{

		public CostManifest()
		{
			EntityTypeId = 20;
			CTDLTypeLabel = "Cost Manifest";
			EstimatedCost = new List<ME.CostProfile>();
		}

		//URL
		public string CostDetails { get; set; }
		public string StartDate { get; set; }
		public string EndDate { get; set; }

		public List<ME.CostProfile> EstimatedCost { get; set; }

		public bool DisplayAdditionalInformation { get; set; }

	}
}

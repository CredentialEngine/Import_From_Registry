using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Elastic
{
	[Serializable]
	public class CostManifest 
	{
		public CostManifest()
		{
			EstimatedCost = new List<CostProfile>();
		}
		//public int Id { get; set; }

		public string Name { get; set; }

		public string Description { get; set; }

		public string CTID { get; set; }

		//URL
		public string CostDetails { get; set; }
		public string StartDate { get; set; }
		public string EndDate { get; set; }

		public List<CostProfile> EstimatedCost { get; set; }
	}
}

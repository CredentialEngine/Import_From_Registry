using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MD = workIT.Models.API;


namespace workIT.Models.Elastic
{
	public class CostProfile 
	{
		public CostProfile()
		{
			CTDLTypeLabel = "Cost Profile";
		}
		public string CTDLTypeLabel { get; set; }
		public string Name { get; set; }
		//required
		//URL
		public string CostDetails { get; set; }
		//required
		public string Description { get; set; }
		public string Currency { get; set; }
		public string CurrencySymbol { get; set; }
		public List<string> Condition { get; set; }
		public string StartDate { get; set; }
		public string EndDate { get; set; }

		public List<JurisdictionProfile> Jurisdiction { get; set; }

		public List<CostProfileItem> CostItems { get; set; } = new List<CostProfileItem>();
	}
	//

	public class CostProfileItem 
	{
		public CostProfileItem()
		{
		}

		public MD.LabelLink DirectCostType { get; set; }
		public decimal Price { get; set; }
		public string PaymentPattern { get; set; }
		public List<MD.LabelLink> AudienceType { get; set; }
		public List<MD.LabelLink> ResidencyType { get; set; }

	}

}

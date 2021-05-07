using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.Common;

namespace workIT.Models.API
{
	public class FinancialAssistanceProfile 
	{
		public FinancialAssistanceProfile() 
		{
			CTDLTypeLabel = "Financial Assistance Profile";
		}
		public string CTDLTypeLabel { get; set; }

		public int? Id { get; set; }
		/// <summary>
		/// name
		/// </summary>
		public string Name { get; set; }
		public string Description { get; set; }

		/// <summary>
		/// SubjectWebpage - URI
		/// </summary>
		public string SubjectWebpage { get; set; }

		public List<LabelLink> FinancialAssistanceType { get; set; } = new List<LabelLink>();

		public List<string> FinancialAssistanceValue { get; set; } 
		public List<QuantitativeValue> FinancialAssistanceValue2 { get; set; }
	}
}

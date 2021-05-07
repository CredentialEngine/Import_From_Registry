using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Common
{
	public class HoldersProfile : OutcomesBaseObject
	{
		/// <summary>
		/// DemographicInformation
		/// Aggregate data or summaries of statistical data relating to the population of credential holders including data about gender, geopolitical regions, age, education levels, and other categories of interest.
		/// </summary>
		public string DemographicInformation { get; set; }

		/// <summary>
		///  Number of credentials awarded.
		/// </summary>
		public int NumberAwarded { get; set; }

	}

	public class Entity_HoldersProfile
	{
		public int Id { get; set; }
		public int EntityId { get; set; }
		public int HoldersProfileId { get; set; }
		public System.DateTime Created { get; set; }
		//public HoldersProfile HoldersProfile { get; set; } = new HoldersProfile();
	}
}

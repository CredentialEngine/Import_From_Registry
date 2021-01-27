using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Common
{
	public class EmploymentOutcomeProfile : OutcomesBaseObject
	{

		/// <summary>
		///  Number of jobs obtained in the region during a given timeframe.
		///  ceterms:jobsObtained
		/// </summary>
		public int JobsObtained { get; set; }

	}

	public class Entity_EmploymentOutcomeProfile
	{
		public int Id { get; set; }
		public int EntityId { get; set; }
		public int EmploymentOutcomeProfileId { get; set; }
		public System.DateTime Created { get; set; }
		public EmploymentOutcomeProfile EmploymentOutcomeProfile { get; set; } = new EmploymentOutcomeProfile();
	}
}

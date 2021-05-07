using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.ProfileModels
{
	public class Entity_CompetencyFramework
	{
		public int Id { get; set; }
		public int EntityId { get; set; }
		public int CompetencyFrameworkId { get; set; }
		public System.DateTime Created { get; set; }

		public string ProfileSummary { get; set; }

		public CompetencyFramework CompetencyFramework { get; set; } = new CompetencyFramework();
	}
}
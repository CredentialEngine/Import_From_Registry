using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.ProfileModels
{
	public class Entity_Assessment
	{
		public int Id { get; set; }
		public int EntityId { get; set; }
		public int AssessmentId { get; set; }
		public System.DateTime Created { get; set; }
		public int CreatedById { get; set; }

		public string ProfileSummary { get; set; }

		public AssessmentProfile Assessment { get; set; }
	}
}

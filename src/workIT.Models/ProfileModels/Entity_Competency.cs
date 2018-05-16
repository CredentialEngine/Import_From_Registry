using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.ProfileModels
{
	public partial class Entity_Competency
	{
		public Entity_Competency()
		{
			
		}
		public int Id { get; set; }
		public int EntityId { get; set; }
		public int EducationFrameworkId { get; set; }

		public string CodedNotation { get; set; }
		public string TargetNodeName { get; set; }
		public string TargetNodeDescription { get; set; }
		public string TargetNode { get; set; }

		public decimal Weight { get; set; }

		public DateTime Created { get; set; }

		#region External Properties

		public string FrameworkName { get; set; }
		public string FrameworkUrl { get; set; }
		#endregion
	}
}

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
		public int CompetencyFrameworkId { get; set; }
		public int CollectionId { get; set; }
		public string CodedNotation { get; set; }
		public string TargetNodeName { get; set; }
		public string TargetNodeDescription { get; set; }
		public string TargetNode { get; set; }
		public string TargetNodeCTID { get; set; }
		

		public decimal Weight { get; set; }

		public DateTime Created { get; set; }

		#region External Properties

		public string FrameworkName { get; set; }
        //can be source url or framework Uri
		public string Framework { get; set; }
		#endregion
	}
}

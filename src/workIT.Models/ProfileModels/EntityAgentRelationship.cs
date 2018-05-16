using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.Common;

namespace workIT.Models.ProfileModels
{
	public class EntityAgentRelationship : BaseProfile
	{
		public EntityAgentRelationship()
		{
			Relationship = "";
			ProfileSummary = "";
			Description = "";
			URL = "";
			//ActingAgent = new Organization();
		}
		public System.Guid ParentUid { get; set; }
		public int ParentTypeId { get; set; }
		public System.Guid AgentUid { get; set; }
		public int AgentId { get; set; }
		//not clear if necessay here
		//public Organization ActingAgent { get; set; }
		public int RelationshipTypeId { get; set; }
		public string Relationship { get; set; } 
		public string URL { get; set; }

		
	}
}

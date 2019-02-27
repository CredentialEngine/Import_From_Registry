using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models
{
    public class AgentRelationship
    {
        public AgentRelationship()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public int RelationshipId { get; set; }

        public string Relationship { get; set; }

		//sometimes will have all of the relationships for an organization in this class.
		//public List<int> RelationshipTypeIds { get; set; } = new List<int>();
		//public List<string> Relationships { get; set; } = new List<string>();
		//public List<string> AgentRelationships { get; set; } = new List<string>();

		public int AgentId { get; set; }
        public System.Guid AgentUid { get; set; }
        public string Agent { get; set; }
        public string AgentUrl { get; set; }
        public bool IsThirdPartyOrganization { get; set; }
        public int EntityStateId { get; set; }
        public string EntityType { get; set; }
    }
    public class TargetAssertion
    {
        public TargetAssertion()
        {
        }
        public int AssertionId { get; set; }
        public string Assertion { get; set; }
        public string AgentToSourceRelationship { get; set; }
        public int TargetId { get; set; }
        public System.Guid TargetUid { get; set; }
        public string Target { get; set; }
        public string TargetEntitySubjectWebpage { get; set; }
        public bool IsThirdPartyOrganization { get; set; }
        public int EntityStateId { get; set; }
        public string EntityType { get; set; }
    }
}

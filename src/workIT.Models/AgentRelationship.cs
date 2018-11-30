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

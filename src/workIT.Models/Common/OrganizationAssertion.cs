using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Common
{
    [Serializable]
    public class OrganizationAssertion : BaseObject
	{
		public OrganizationAssertion()
		{
			Organization = new Organization();
		}
		public int ParentEntityId { get; set; }

		public Guid ParentUid { get; set; }

		public int AssertionTypeId { get; set; }
        public int TargetEntityBaseId { get; set; }
        public string TargetEntityName { get; set; }
        public string TargetEntityType { get; set; }
        public string TargetEntitySubjectWebpage { get; set; }
        public int TargetEntityStateId { get; set; }
        public string AgentToSourceRelationship { get; set; }
        public string TargetCTID { get; set; }
        public bool IsReference
        {
            get
            {
                if ( ( int )TargetEntityStateId < 3 )
                    return true;
                else
                {
                    return false;
                }
            }
        }



        public Enumeration AgentAssertion { get; set; } = new Enumeration();

		public Guid TargetEntityUid { get; set; }
		public Entity TargetEntity { get; set; }
		public int TargetEntityTypeId { get; set; }

		//derived

		public int OrganizationId { get; set; }
		public Organization Organization { get; set; }

	}
}

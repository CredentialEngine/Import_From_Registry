using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Common
{
	public class OrganizationAssertion : BaseObject
	{
		public OrganizationAssertion()
		{
			Organization = new Organization();
		}
		public int ParentEntityId { get; set; }

		public Guid ParentUid { get; set; }

		public int AssertionTypeId { get; set; }
		public Guid TargetEntityUid { get; set; }
		public Entity TargetEntity { get; set; }
		public int TargetEntityTypeId { get; set; }

		//derived

		public int OrganizationId { get; set; }
		public Organization Organization { get; set; }

	}
}

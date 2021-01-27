using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.ProfileModels;

namespace workIT.Models.Common
{
    public class PathwaySet : TopLevelObject
    {

		public List<Pathway> Pathways { get; set; } = new List<Pathway>();
		public List<string> HasPathway { get; set; } = new List<string>();

        public List<Organization> OwnedByOrganization { get; set; } = new List<Organization>();

        public List<Organization> OfferedByOrganization { get; set; } = new List<Organization>();

		public Enumeration OwnerRoles { get; set; } = new Enumeration();
		//??
		public List<OrganizationRoleProfile> OrganizationRole { get; set; } = new List<OrganizationRoleProfile>();
		public int ResultNumber { get; set; }

		#region Import
		public List<Guid> HasPathwayList { get; set; } = new List<Guid>();
		public List<Guid> OwnedBy { get; set; } = new List<Guid>();
		public List<Guid> OfferedBy { get; set; } = new List<Guid>();

		#endregion
	}

	public class PathwaySetSummary : PathwaySet
	{
		public PathwaySetSummary()
		{
			//make sure no issue with initializing here
			OwningOrganization = new Organization();
		}

		public int SearchRowNumber { get; set; }
		//public string OwningAgentUid { get; set; }

	}
}

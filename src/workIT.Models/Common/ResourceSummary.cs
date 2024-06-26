using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Common
{
    public class ResourceSummary
    {
        public int EntityTypeId { get; set; }
        public int Id { get; set; }
        public Guid RowId { get; set; }
        public string CodedNotation { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string CTID { get; set; }
        public string URI { get; set; }
        public string Type { get; set; }
        public int? RelationshipTypeId { get; set; }
        public string ImageUrl { get; set; }

		public int ResourcePrimaryOrgId { get; set; }
		public string ResourcePrimaryOrganizationName { get; set; }
	}
}

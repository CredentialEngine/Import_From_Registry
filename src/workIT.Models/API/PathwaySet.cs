using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WMS = workIT.Models.Search;


namespace workIT.Models.API
{
	public class PathwaySet : BaseDisplay
	{
		public PathwaySet()
		{
			EntityTypeId = 23;
			CTDLTypeLabel = "Pathway Set";
		}

		public List<Pathway> Pathways { get; set; } = new List<Pathway>();
		public List<Outline> HasPathways { get; set; } = new List<Outline>();

		public WMS.AJAXSettings HasPathway { get; set; }

	}
}

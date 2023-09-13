using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WMS = workIT.Models.Search;


namespace workIT.Models.API
{
	public class PathwaySet : BaseAPIType
	{
		public PathwaySet()
		{
			EntityTypeId = 23;
			CTDLTypeLabel = "Pathway Set";
		}

		//public List<Pathway> Pathways { get; set; } = new List<Pathway>();

		public WMS.AJAXSettings HasPathway { get; set; }

	}
}

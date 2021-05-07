using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.API
{
	public class CompetencyFramework //: BaseDisplay
	{
		public CompetencyFramework()
		{
			EntityTypeId = 10;
			BroadType = "CompetencyFramework";
			CTDLType = "ceasn:CompetencyFramework";
			CTDLTypeLabel = "Competency Framework";
		}
		public string BroadType { get; set; }
		public string CTDLType { get; set; }
		public string CTDLTypeLabel { get; set; }
		public int EntityTypeId { get; set; }
		public string Meta_Language { get; set; }
		public DateTime Meta_LastUpdated { get; set; }

		public string Name { get; set; }
		public string CTID { get; set; }
		public string Description { get; set; }
		public string Source { get; set; }

		public List<string> HasTopChild { get; set; }
		public List<Competency> Meta_HasPart { get; set; } = new List<Competency>();
	}

	//don't inherit from BaseDisplay for cleaner use in display
	public class Competency		//: BaseDisplay
	{
		public Competency()
		{
			//EntityTypeId = 11;
			//BroadType = "Competency";
			//CTDLType = "ceasn:Competency";
			//CTDLTypeLabel = "Competency";
		}
		//public string BroadType { get; set; }
		//public string CTDLType { get; set; }
		//public string CTDLTypeLabel { get; set; }
		//public int EntityTypeId { get; set; }

		public string CTID { get; set; }


		public string CompetencyLabel { get; set; }
		public string CompetencyText { get; set; }
		//public string CTID { get; set; }
		public string Description { get; set; }
		public string Comment { get; set; }

		public string CodedNotation { get; set; }
		public string CompetencyCategory { get; set; }
		public List<string> HasChildId { get; set; } = new List<string>();
		//or store hierarchy for future display
		public List<Competency> HasChild { get; set; } = new List<Competency>();
		public string ListID { get; set; }

		
		/*
		 * CompetencyLabel
		 * 
		 * 
		 * 
		 * */

	}
}

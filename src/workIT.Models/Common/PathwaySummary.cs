using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PM = workIT.Models.ProfileModels;
namespace workIT.Models.Common
{
	public class PathwaySummary : Pathway
	{
		public PathwaySummary ()
		{
			//make sure no issue with initializing here
			OwningOrganization = new Organization();
		}
		
		//		public string Name { get; set; }
		//public LanguageMap Name_Map { get; set; }
		//public string FriendlyName { get; set; }

		//public string Description { get; set; }
		//public LanguageMap Description_Map { get; set; }

		//public string CTID { get; set; }
		//public string SubjectWebpage { get; set; }
		public int SearchRowNumber { get; set; }
		//public string OwningAgentUid { get; set; }


		//public List<PM.TextValueProfile> Subject { get; set; }
		public List<string> Subjects { get; set; } = new List<string>();

		public CodeItemResult OccupationResults { get; set; } = new CodeItemResult();
		public CodeItemResult OccupationOtherResults { get; set; } = new CodeItemResult();

		public CodeItemResult IndustryResults { get; set; } = new CodeItemResult();
		public CodeItemResult IndustryOtherResults { get; set; } = new CodeItemResult();
	}
}

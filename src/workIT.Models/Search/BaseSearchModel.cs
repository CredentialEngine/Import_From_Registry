using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Search
{
	public class BaseSearchModel
	{
		public BaseSearchModel()
		{
			StartDate = new DateTime( 2015, 1, 1 );
			EndDate = DateTime.Now;
			OrderBy = "CreatedDate";
			IsDescending = true;
			PageNumber = 1;
			PageSize = 25;
			TotalRows = 0;
			Filter = "";
		}
		public string Filter { get; set; }
		public string Keyword { get; set; }
		public DateTime StartDate { get; set; }
		public DateTime EndDate { get; set; }
		public int UserId { get; set; }

		public string OrderBy { get; set; }
		public bool IsDescending { get; set; }

		public int PageNumber { get; set; }
		public int PageSize { get; set; }

		public int TotalRows { get; set; }

	}
}

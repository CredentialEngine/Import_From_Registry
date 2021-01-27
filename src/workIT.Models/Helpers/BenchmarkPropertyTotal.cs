using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Helpers.Reports
{
	public class BenchmarkPropertyTotal
	{
		public BenchmarkPropertyTotal()
		{
		}

		public int DefaultOrder { get; set; }

		public string Property { get; set; }
		public string Label { get; set; }
		public string Policy { get; set; }

		public string PropertyGroup { get; set; }

		public int Total { get; set; }
		public decimal PercentOfOverallTotal { get; set; }

	}
}

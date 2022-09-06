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

	public class BenchmarkQuery
	{
		public string SearchType { get; set; }
		public string LabelFilter { get; set; }
		public string PolicyFilter { get; set; }

		public int PageNumber { get; set; }
		public int PageSize { get; set; }
		public string SortOrder { get; set; }
		public bool IsDescending { get; set; }
	}
	public class BenchmarkQueryResult
	{
		public string BenchmarkType { get; set; }
		public List<BenchmarkPropertyTotal> Benchmarks { get; set; } = new List<BenchmarkPropertyTotal>();
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Common
{
	public class CurrencyCode
	{
		public int NumericCode { get; set; }
		public string AlphabeticCode { get; set; }
		public string Currency { get; set; }
		//public Nullable<int> SortOrder { get; set; }
		public string UnicodeDecimal { get; set; }
		public string UnicodeHex { get; set; }
		public string HtmlCodes { get; set; }
	}
}

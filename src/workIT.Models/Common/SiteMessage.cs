using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Common
{
    [Serializable]
    public class SiteMessage
	{
		public string Title { get; set; }
		public string Message { get; set; }
		public string MessageType { get; set; }
	}
}

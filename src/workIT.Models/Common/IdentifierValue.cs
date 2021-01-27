using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Common
{
	//TBD - should this be replaced by PM.Entity_IdentifierValue
	//		- 21-01-07 mparsons - No. Will be used when storing the JSON in a sql property
	[Serializable]
    public class IdentifierValue 
	{
		public string IdentifierTypeName { get; set; }
		//public string Description { get; set; }
		public string IdentifierType { get; set; }
		public string IdentifierValueCode { get; set; }
	}
}

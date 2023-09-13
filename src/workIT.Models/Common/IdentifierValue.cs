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
		/// <summary>
		/// Framework, scheme, type, or other organizing principle of this identifier.
		/// URI
		/// </summary>
		public string IdentifierType { get; set; }
		//22-08-03 MP - NOTE: IS there existing data with Name? May only have been in Address that uses Entity_IdentifierValue
		//public string Name { get; set; }

		/// <summary>
		/// Formal name or acronym of the framework, scheme, type, or other organizing principle of this identifier, such as ISBN or ISSN.
		/// </summary>
		public string IdentifierTypeName { get; set; }


		public string IdentifierValueCode { get; set; }
	}
}

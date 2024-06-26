using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Common
{
	//TBD - should this be replaced by PM.Entity_IdentifierValue
	//		- 21-01-07 mparsons - No. Will be used when storing the JSON in a sql property
	//		- 23-12-17 mparsons - However the latter should be avoided as lacks ability to customize?
	//							- may be useful to included date created to ensure sorted by the latter?
	[Serializable]
    public class IdentifierValue 
	{
		/// <summary>
		/// Framework, scheme, type, or other organizing principle of this identifier.
		/// URI
		/// </summary>
		public string IdentifierType { get; set; }

		/// <summary>
		/// Formal name or acronym of the framework, scheme, type, or other organizing principle of this identifier, such as ISBN or ISSN.
		/// </summary>
		public string IdentifierTypeName { get; set; }


		public string IdentifierValueCode { get; set; }
	}
}

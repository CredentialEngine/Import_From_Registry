using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.API
{
	public class IdentifierValue
	{
		public string IdentifierTypeName { get; set; }
		public string IdentifierType { get; set; }
		public string IdentifierValueCode { get; set; }

		public bool HasData()
		{
			if ( !string.IsNullOrWhiteSpace( IdentifierValueCode ) )
			{
				return true;
			}

			return false;
		}
	}
}

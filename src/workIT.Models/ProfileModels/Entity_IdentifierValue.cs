using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.ProfileModels
{
    [Serializable]
    public class Entity_IdentifierValue
	{
		public int Id { get; set; }
		
		public int EntityId { get; set; }

		/// <summary>
		/// See constants in Entity.IdentifierValue
		/// Example for VersionIdentifier, or Identifier, etc. 
		/// </summary>
		public int IdentityValueTypeId { get; set; }
		/// <summary>
		/// Not sure of edits.
		/// Should sort by this to keep related identifiers together
		/// URI
		/// </summary>
		public string IdentifierType { get; set; } = "";

		public string IdentifierTypeName { get; set; }

		public string IdentifierValueCode { get; set; }
		public System.DateTime Created { get; set; }

		public bool HasData()
		{
			if ( IdentityValueTypeId > 0 
				|| ( !string.IsNullOrWhiteSpace( IdentifierTypeName ) )
				|| ( !string.IsNullOrWhiteSpace( IdentifierValueCode ))
				)
			{
				return true;
			}

			return false;
		}
	}
}

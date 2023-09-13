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
		public string IdentifierTypeName { get; set; }
		//22-07-28 commented Name. Moving to use of Common.IdentifierValue (at least for Address, related data)
		//22-08-03 MP - NOTE: there is existing data with Name. Need to add this back as an alias pending a full re-index. 
		///// <summary>
		///// DO not reference directly - alias only for JSON deserializing
		///// </summary>
		//public string Name
  //      {
  //          get { return IdentifierTypeName; }
  //      }

        /// <summary>
        /// Not sure of edits
        /// URL
        /// </summary>
        public string IdentifierType { get; set; }
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

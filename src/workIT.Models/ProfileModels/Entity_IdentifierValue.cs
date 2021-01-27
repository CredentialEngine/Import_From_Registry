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
		/// </summary>
		public int IdentityValueTypeId { get; set; }
		public string Name { get; set; }
		//public string Description { get; set; }
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
				|| ( !string.IsNullOrWhiteSpace( Name ) )
				|| ( !string.IsNullOrWhiteSpace( IdentifierValueCode ))
				)
			{
				return true;
			}

			return false;
		}
	}
}

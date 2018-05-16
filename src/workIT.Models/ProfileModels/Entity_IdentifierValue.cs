using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.ProfileModels
{
	public class Entity_IdentifierValue
	{
		public int Id { get; set; }
		
		public int EntityId { get; set; }

		/// <summary>
		/// Primarily a placeholder, should ever have more than one property in a class of type IdentifierValue
		/// </summary>
		public int IdentityValueTypeId { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public string IdentifierType { get; set; }
		public string IdentifierValueCode { get; set; }
		public System.DateTime Created { get; set; }
	}
}

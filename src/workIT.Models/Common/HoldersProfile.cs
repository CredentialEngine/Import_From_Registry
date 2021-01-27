using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Common
{
	public class HoldersProfile : OutcomesBaseObject
	{

		/// <summary>
		///  Number of credentials awarded.
		/// </summary>
		public int NumberAwarded { get; set; }

	}

	public class Entity_HoldersProfile
	{
		public int Id { get; set; }
		public int EntityId { get; set; }
		public int HoldersProfileId { get; set; }
		public System.DateTime Created { get; set; }
		//public HoldersProfile HoldersProfile { get; set; } = new HoldersProfile();
	}
}

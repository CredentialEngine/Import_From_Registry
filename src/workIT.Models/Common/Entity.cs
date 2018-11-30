using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Common
{
    [Serializable]
    public class Entity : BaseObject
	{
		public Entity ()
		{
			//can't initialize here, as will cause infinite loop
			ParentEntity = null;	//			new Entity();
		}
		public System.Guid EntityUid { get; set; }
		public int EntityTypeId { get; set; }
		public string EntityType { get; set; }

		public int EntityBaseId { get; set; }
		public string EntityBaseName { get; set; }

		public Entity ParentEntity { get; set; }
	}
}

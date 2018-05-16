using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using System.Web.Script.Serialization;

namespace workIT.Models.Node
{
	public class ProfileLink
	{
		public ProfileLink()
		{
			Type = this.GetType();
			RowId = new Guid(); //All zeroes
			OwningAgentUid = new Guid(); //All zeroes
		}

		public int Id { get; set; }
		public Guid RowId { get; set; }
		public string Name { get; set; }
		public string Property { get; set; }
		public string TypeName 
		{ 
			get
			{
				if ( Type != null )
					return Type.Name;
				else
					return "";
			} 
			set { this.Type = Type.GetType( "Models.Node." + value ); } 
		}

		public Guid ParentEntityRowId { get; set; }
		public int ParentEntityTypeId { get; set; }

		public Guid OwningAgentUid { get; set; }

		public bool IsReferenceEntity { get; set; }

		[JsonIgnore][ScriptIgnore]
		public Type Type { get; set; }
	}

}

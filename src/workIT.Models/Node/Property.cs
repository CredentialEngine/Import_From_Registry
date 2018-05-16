using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Node
{

	//Attribute to make conversion easier
	[AttributeUsage( AttributeTargets.Property )]
	public class Property : Attribute
	{
		public Property()
		{
			Type = typeof( ProfileLink );
			DBType = typeof( string );
		}
		public Type Type { get; set; }
		public Type DBType { get; set; }
		public string DBName { get; set; }
		public bool SaveAsProfile { get; set; } //Indicates whether or not to initialize a new profile during saving - used with micro searches that do not do direct saves
		public string SchemaName { get; set; }
		public string LoadMethod { get; set; }
		public string SaveMethod { get; set; }
	}
	//
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Common
{
	public class ReferenceFrameworkItem
	{

		public int Id { get; set; }
		public int CategoryId { get; set; }
		public string CodeGroup { get; set; }
		public string CodedNotation { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public string TargetNode { get; set; }
		public Nullable<System.DateTime> Created { get; set; }
		public int ReferenceFrameworkId { get; set; }

		//helpers - or ensure ExternalFrameworkId gets set
		public string Framework { get; set; }
		public string FrameworkName { get; set; }
	}
	public class ReferenceFramework
	{
		public int Id { get; set; }
		public int CategoryId { get; set; }
		public string Name { get; set; }
		public string Framework { get; set; }
		public string Description { get; set; }
		public System.DateTime Created { get; set; }
		
	}

	public partial class Entity_ReferenceFramework
	{
		public int Id { get; set; }
		public int EntityId { get; set; }
		public int ReferenceFrameworkId { get; set; }
		//necessary
		public int CategoryId { get; set; }
		public System.DateTime Created { get; set; }

		//public virtual Entity Entity { get; set; }
		//public virtual Reference_Frameworks Reference_Frameworks { get; set; }
	}

	//public class ReferenceFrameworks
	//{

	//	public int Id { get; set; }
	//	public int CategoryId { get; set; }
	//	public string CodeGroup { get; set; }
	//	public string CodedNotation { get; set; }
	//	public string Name { get; set; }
	//	public string TargetNode { get; set; }
	//	public string Framework { get; set; }
	//	public string Description { get; set; }
	//	public Nullable<System.DateTime> Created { get; set; }
	//	public int ExternalFrameworkId { get; set; }

	//}
}

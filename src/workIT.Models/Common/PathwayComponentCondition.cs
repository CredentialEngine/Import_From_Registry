using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Common
{
	/// <summary>
	/// Describes what must be done to complete one PathwayComponent (or part thereof) as determined by the issuer of the Pathway
	/// </summary>
	public class PathwayComponentCondition : BaseObject
	{

		public int ParentComponentId { get; set; }
		/// <summary>
		/// PathwayComponent Description 
		/// Required
		/// </summary>
		public string Description { get; set; }


		/// <summary>
		/// Name
		/// </summary>
		public string Name { get; set; }

		public int RequiredNumber { get; set; }

		public List<PathwayComponent> TargetComponent { get; set; } = new List<PathwayComponent>();


		public string PathwayCTID { get; set; }

		public System.Guid PathwayIdentifier { get; set; }

		//****NOTE: will get stack overflow initializing here. 
		//current code will always check for nulls
		public Entity RelatedEntity { get; set; }

		#region Import
		public List<Guid> HasTargetComponentList { get; set; } = new List<Guid>();


		#endregion
	}
}

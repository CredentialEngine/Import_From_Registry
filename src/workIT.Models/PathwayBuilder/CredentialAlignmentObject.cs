using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.PathwayBuilder
{
    public class CredentialAlignmentObject
    {
		public int Id { get; set; }

		/// <summary>
		/// FK to the framework
		/// </summary>
		public int ReferenceFrameworkId { get; set; }
		/// <summary>
		/// Optional
		/// Typically would be read from the 
		/// URL
		/// </summary>
		public string Framework { get; set; }
		/// <summary>
		/// Framework name
		/// Optional
		/// </summary>
		public string FrameworkName { get; set; }

		//TBD, 10-industry, 11 occupation, 23 cip 
		public int CategoryId { get; set; }

		/// <summary>
		/// Coded notation form a framework. Likely should be empty if no framework
		/// </summary>
		public string CodedNotation { get; set; }
		/// <summary>
		/// URL
		/// </summary>
		public string TargetNode { get; set; }
		public string TargetNodeName { get; set; }
		public string TargetNodeDescription { get; set; }

		public DateTime Created { get; set; }




	}
}

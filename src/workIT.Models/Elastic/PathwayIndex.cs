using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Elastic
{
	public class PathwayIndex: BaseIndex, IIndex
	{

		//public List<IndexReferenceFramework> Industries { get; set; } = new List<IndexReferenceFramework>();
		public List<string> Keyword { get; set; } = new List<string>();
		//public List<IndexReferenceFramework> Occupations { get; set; } = new List<IndexReferenceFramework>();

		public int OwnerOrganizationId
		{
			get { return PrimaryOrganizationId; }
			set { this.PrimaryOrganizationId = value; }
		}
		/// <summary>
		/// Source will be Entity.SearchIndex
		/// Audience Level Type,        
		/// Classification of Instructional Programs( CIP)
		/// Competency Item
		/// Subject
		/// Keyword
		/// </summary>
		public List<string> PremiumValues { get; set; } = new List<string>();

		//public List<int> ReportFilters { get; set; } = new List<int>();
		//need to clarify where used vs SubjectArea
		public List<IndexSubject> Subjects { get; set; }

		//place holders required for IIndex
		//public List<Address> Addresses { get; set; } = null;
		//public List<IndexReferenceFramework> InstructionalPrograms { get; set; } = null;
		public bool IsAvailableOnline { get; set; } = false;

	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Elastic
{
	public class GenericIndex : BaseIndex, IIndex
	{

		//public List<IndexReferenceFramework> Industries { get; set; } = new List<IndexReferenceFramework>();
		//public List<string> Keyword { get; set; } = new List<string>();
		//public List<IndexReferenceFramework> Occupations { get; set; } = new List<IndexReferenceFramework>();
		public string OwnerOrganizationName { get; set; }

		public int OwnerOrganizationId
		{
			get { return PrimaryOrganizationId; }
			set { this.PrimaryOrganizationId = value; }
		}

		public int? LifeCycleStatusTypeId { get; set; }
		public string LifeCycleStatusType { get; set; }
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
		public List<IndexSubject> Subjects { get; set; } = new List<IndexSubject>();

		//place holders required for IIndex
		//public List<Address> Addresses { get; set; } = null;
		//public List<IndexReferenceFramework> InstructionalPrograms { get; set; } = null;
		public bool IsAvailableOnline { get; set; } = false;

		public bool HasOccupations { get; set; }
		public bool HasIndustries { get; set; }

		public List<EntityReference> Pathways { get; set; } = null;
		public bool HasPathwaysCount { get; set; }
		public int ResultNumber { get; set; }

		#region TVP counts
		public int TransferValueForCredentialsCount { get; set; }

		public int TransferValueFromCredentialsCount { get; set; }

		public int TransferValueForAssessmentsCount { get; set; }

		public int TransferValueFromAssessmentsCount { get; set; }

		public int TransferValueForLoppsCount { get; set; }

		public int TransferValueFromLoppsCount { get; set; }
		public int TransferValueHasDevProcessCount { get; set; }

		#endregion

	}
}

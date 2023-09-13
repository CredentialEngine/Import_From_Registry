using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Elastic
{
    /// <summary>
    /// Common index currently used by:
    /// - Transfer value
    /// - Transfer Intermediary
    /// - Collection
    /// - Occupation, Job, Task and workRole
    /// - Concept scheme and progression model
    ///			Currently mostly N/A as search is done thru the registry
	///			Progression model should be split out
    /// </summary>
    public class GenericIndex : BaseIndex, IIndex
	{

		//public List<IndexReferenceFramework> Industries { get; set; } = new List<IndexReferenceFramework>();
		//public List<string> Keyword { get; set; } = new List<string>();
		//public List<IndexReferenceFramework> Occupations { get; set; } = new List<IndexReferenceFramework>();
		//public string PrimaryOrganizationName { get; set; }

		//public int OwnerOrganizationId
		//{
		//	get { return PrimaryOrganizationId; }
		//	set { this.PrimaryOrganizationId = value; }
		//}

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
        public string DateEffective { get; set; }
        public string ExpirationDate { get; set; }
        public bool HasOccupations { get; set; }
		public bool HasIndustries { get; set; }

		public List<IndexProperty> Properties { get; set; } = new List<IndexProperty>();
		public List<int> CollectionCategoryTypeIds { get; set; } = new List<int>();

		//public List<EntityReference> Pathways { get; set; } = null;
		//public bool HasPathwaysCount { get; set; }
		public int ResultNumber { get; set; }

        #region Transfer Value specific
        /// <summary>
        /// List of TVP ids part of a transfer intermediary
        /// </summary>
        public List<int> TransferIntermediariesFor { get; set; } = new List<int>();

        /// <summary>
        /// List of transfer intermediary ids a TVP is part of 
        /// </summary>
        public List<int> HasTransferIntermediary { get; set; } = new List<int>();


        public int TransferValueForCredentialsCount { get; set; }

		public int TransferValueFromCredentialsCount { get; set; }

		public int TransferValueForAssessmentsCount { get; set; }

		public int TransferValueFromAssessmentsCount { get; set; }

		public int TransferValueForLoppsCount { get; set; }

		public int TransferValueFromLoppsCount { get; set; }
		public int TransferValueHasDevProcessCount { get; set; }
		//
		public int HasTransferValueProfiles { get; set; }
        #endregion


        public List<int> AccommodationTypeIds { get; set; } = new List<int>();
        public List<int> SupportServiceCategoryIds { get; set; } = new List<int>();
        public List<int> DeliveryMethodTypeIds { get; set; } = new List<int>();

        public int CostProfilesCount { get; set; }

        public int CommonCostsCount { get; set; }
        public int CommonConditionsCount { get; set; }
        public int FinancialAidCount { get; set; }

		#region DataSetProfile
		public JObject DataSetTimePeriods { get; set; }
		public string AboutCredentialList { get; set; }
		public string AboutLoppList { get; set; }
		#endregion
	}
}

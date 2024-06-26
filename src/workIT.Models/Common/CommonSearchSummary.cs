using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace workIT.Models.Common
{
	public class CommonSearchSummary
	{
		public int ResultNumber { get; set; }
		public int EntityTypeId { get; set; }
		public string EntityType { get; set; }
		public int Id { get; set; }
		public Guid RowId { get; set; }
		public string CTID { get; set; }
		public int EntityStateId { get; set; }
		public string CredentialRegistryId { get; set; }

		public string Name { get; set; }
		//TBD
		public string NameAlphanumericOnly { get; set; }
		//
		public string FriendlyName { get; set; }
		public string Description { get; set; }
		public string SubjectWebpage { get; set; }

		public DateTime Created { get; set; }
		public DateTime LastUpdated { get; set; }
		//===================================
		public int PrimaryOrganizationId { get; set; }

		public string PrimaryOrganizationName { get; set; }
		public string PrimaryOrganizationFriendlyName { get; set; }

		public string PrimaryOrganizationCTID { get; set; }
		public List<int> AgentRelationships { get; set; } = new List<int>();
		//public List<AgentRelationshipForEntity> AgentRelationshipsForEntity { get; set; } = new List<AgentRelationshipForEntity>();
		//=======================================
		//
		//
		public bool HasOccupations { get; set; }
		public bool HasIndustries { get; set; }
		public CodeItemResult IndustryResults { get; set; } = new CodeItemResult();
		public CodeItemResult OccupationResults { get; set; } = new CodeItemResult();
		public CodeItemResult InstructionalProgramClassification { get; set; } = new CodeItemResult();

		//public List<IndexReferenceFramework> Industries { get; set; } = new List<IndexReferenceFramework>();
		//public List<IndexReferenceFramework> Occupations { get; set; } = new List<IndexReferenceFramework>();
		//public List<IndexReferenceFramework> InstructionalPrograms { get; set; } = new List<IndexReferenceFramework>();
		public List<string> Industry { get; set; } = new List<string>();
		public List<string> Occupation { get; set; } = new List<string>();
		public List<string> InstructionalProgram { get; set; } = new List<string>();

        public string DateEffective { get; set; }
        public string ExpirationDate { get; set; }
		public string StartDate { get; set; }
		public string EndDate { get; set; }
		public List<string> Keyword { get; set; } = new List<string>();
        public Enumeration LifeCycleStatusType { get; set; } = new Enumeration();
        public string LifeCycleStatus { get; set; }
        public int LifeCycleStatusTypeId { get; set; }
        public List<string> Subjects { get; set; } = new List<string>();
		//public List<ResourceSummary> AboutResources { get; set; } = new List<ResourceSummary>();
		public CodeItemResult AboutCredentials { get; set; } = new CodeItemResult();
		public CodeItemResult AboutLearningOpportunities { get; set; } = new CodeItemResult();
		public CodeItemResult ProvidesTransferValueFor { get; set; } = new CodeItemResult();
		public CodeItemResult ReceivesTransferValueFrom { get; set; } = new CodeItemResult();

		//
		public List<int> ReportFilters { get; set; } = new List<int>();
		//will need more, for each of the types of list:
		//provider, QA, entity list
		//although could just handle lists for now
		public List<int> ResourceForWidget { get; set; } = new List<int>();
		//OR
		//public List<IndexWidgetTag> WidgetTags { get; set; } = new List<IndexWidgetTag>();

		#region TVP counts
		public int TransferValueForCredentialsCount { get; set; }

		public int PathwaysCount { get; set; }
		public int TransferValueFromCredentialsCount { get; set; }
		public int TransferValueForAssessmentsCount { get; set; }
		public int TransferValueFromAssessmentsCount { get; set; }
		public int TransferValueForLoppsCount { get; set; }
		public int TransferValueFromLoppsCount { get; set; }

		//
		public int TransferIntermediariesFor { get; set; }

        #endregion
        public List<Address> AvailableAt { get; set; }
		public CodeItemResult AccommodationTypes { get; set; } = new CodeItemResult();

		public CodeItemResult SupportServiceCategories { get; set; } = new CodeItemResult();
        public CodeItemResult DeliveryMethodTypes { get; set; } = new CodeItemResult();

        public CodeItemResult OfferFrequencyType { get; set; } = new CodeItemResult();
        public CodeItemResult ScheduleFrequencyType { get; set; } = new CodeItemResult();
        public CodeItemResult ScheduleTiming { get; set; } = new CodeItemResult();

        public int CostProfilesCount { get; set; }
        public int NumberOfCostProfileItems { get; set; }
        public int CommonCostsCount { get; set; }
        public int CommonConditionsCount { get; set; }
        public int FinancialAidCount { get; set; }
        public int HasSupportServiceCount { get; set; }
        public int IsPartOfSupportServiceCount { get; set; }
        public JObject ResourceDetail { get; set; }

	}
}

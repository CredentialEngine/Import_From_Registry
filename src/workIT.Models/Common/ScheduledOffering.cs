using System;
using System.Collections.Generic;

using workIT.Models.ProfileModels;

namespace workIT.Models.Common
{
	public class ScheduledOffering : TopLevelObject
    {
        public string Type { get; set; } = "ceterms:ScheduledOffering";


        //public string CTID { get; set; }

        //public string Name { get; set; }
        public Guid OfferedByAgentUid { get; set; }
        public List<ResourceSummary> OfferedBy { get; set; } = new List<ResourceSummary>();
        public List<OrganizationRoleProfile> OrganizationRole { get; set; }

        public List<string> AlternateName { get; set; } = new List<string>();

        //public string SubjectWebpage { get; set; } //URL

        public List<AggregateDataProfile> AggregateData { get; set; }

        public List<string> AvailabilityListing { get; set; }
        public List<Address> AvailableAt { get; set; }

        public List<string> AvailableOnlineAt { get; set; }


        public List<CostManifest> CommonCosts { get; set; }


        public Enumeration DeliveryType { get; set; }

        public string DeliveryTypeDescription { get; set; }

        public List<CostProfile> EstimatedCost { get; set; }

        public List<DurationProfile> EstimatedDuration { get; set; }

        public List<ResourceSummary> HasSupportService { get; set; } = new List<ResourceSummary>();

        /// <summary>
        /// Offer Frequency Type
        /// ConceptScheme: ceterms:ScheduleFrequency
        /// </summary>
        public Enumeration OfferFrequencyType { get; set; }

        /// <summary>
        /// Schedule Frequency Type
        /// ConceptScheme: ceterms:ScheduleFrequency
        /// </summary>
        public Enumeration ScheduleFrequencyType { get; set; }

        /// <summary>
        /// Schedule Timing Type
        /// Type of time at which events typically occur; select from an existing enumeration of such types.
        /// ConceptScheme: ceterms:ScheduleTiming
        /// </summary>
        public Enumeration ScheduleTimingType { get; set; }

        public int DataSetProfileCount { get; set; }
        public List<QData.DataSetProfile> ExternalDataSetProfiles { get; set; } = new List<QData.DataSetProfile>();


        #region Import 
        public List<int> CostManifestIds { get; set; }
        public List<int> HasSupportServiceIds { get; set; } = new List<int>();

        public List<Guid> OfferedByList { get; set; }
        public List<string> DataSetProfileCTIDList { get; set; }
        #endregion


    }

    //
    public class Entity_HasOffering
    {
        public int Id { get; set; }
        public int EntityId { get; set; }
        public int ScheduledOfferingId { get; set; }
        public System.DateTime Created { get; set; }

        //TBD if to be used
        public ScheduledOffering ScheduledOffering { get; set; }

    }
}

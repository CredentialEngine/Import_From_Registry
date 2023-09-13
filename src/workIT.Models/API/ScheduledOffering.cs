using System.Collections.Generic;

using ME = workIT.Models.Elastic;
using WMS = workIT.Models.Search;

namespace workIT.Models.API
{
    public class ScheduledOffering : BaseAPIType
    {
        public ScheduledOffering()
        {
            EntityTypeId = 15;
            BroadType = "ScheduledOffering";
            CTDLType = "ceterms:ScheduledOffering";
            CTDLTypeLabel = "Scheduled Offering";
        }

        public List<string> AlternateName { get; set; } = new List<string>();


        public List<AggregateDataProfile> AggregateData { get; set; }

        public List<string> AvailabilityListing { get; set; }
        public List<Address> AvailableAt { get; set; }

        public List<string> AvailableOnlineAt { get; set; }


        public List<CostManifest> CommonCosts { get; set; }


        public List<LabelLink> DeliveryType { get; set; } = new List<LabelLink>();

        public string DeliveryTypeDescription { get; set; }

        public List<ME.CostProfile> EstimatedCost { get; set; }

        public List<DurationProfile> EstimatedDuration { get; set; }

        public List<DataSetProfile> ExternalDataSetProfiles { get; set; }

        public WMS.AJAXSettings HasSupportService { get; set; }

        /// <summary>
        /// Offer Frequency Type
        /// ConceptScheme: ceterms:ScheduleFrequency
        /// </summary>
        public List<LabelLink> OfferFrequencyType { get; set; }

        /// <summary>
        /// Schedule Frequency Type
        /// ConceptScheme: ceterms:ScheduleFrequency
        /// </summary>
        public List<LabelLink> ScheduleFrequencyType { get; set; }

        /// <summary>
        /// Schedule Timing Type
        /// Type of time at which events typically occur; select from an existing enumeration of such types.
        /// ConceptScheme: ceterms:ScheduleTiming
        /// </summary>
        public List<LabelLink> ScheduleTimingType { get; set; }


    }
}

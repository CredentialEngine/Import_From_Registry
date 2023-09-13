using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MC = workIT.Models.Common;
using WMA = workIT.Models.API;
using ME = workIT.Models.Elastic;
using WMS = workIT.Models.Search;

namespace workIT.Models.API
{
    public class SupportService : BaseAPIType
    {
        public SupportService()
        {
            EntityTypeId = 38;
            BroadType = "SupportService";
            CTDLType = "ceterms:SupportService";
            CTDLTypeLabel = "Support Service";
        }

        public List<LabelLink> AccommodationType { get; set; } = new List<LabelLink>();
        public List<string> AlternateName { get; set; } = new List<string>();

        public List<string> AvailabilityListing { get; set; }
        public List<Address> AvailableAt { get; set; }

        public List<string> AvailableOnlineAt { get; set; }

        public List<ConditionManifest> CommonConditions { get; set; }
        public List<CostManifest> CommonCosts { get; set; }

        /// <summary>
        /// Start Date of this resource
        /// </summary>
        public string DateEffective { get; set; }

        public List<LabelLink> DeliveryType { get; set; } = new List<LabelLink>();

        /// <summary>
        /// End date of the learning opportunity if applicable
        /// </summary>
        public string ExpirationDate { get; set; }

        public List<ME.CostProfile> EstimatedCost { get; set; }

        public List<FinancialAssistanceProfile> FinancialAssistance { get; set; }
        public List<IdentifierValue> Identifier { get; set; }
        public List<LabelLink> Keyword { get; set; }

        public List<ReferenceFramework> OccupationType { get; set; }

        public List<ME.JurisdictionProfile> OfferedIn { get; set; }

        /// <summary>
        /// Qualifying requirements for receiving a support service.
        /// </summary>
        public List<ConditionProfile> SupportServiceConditon { get; set; }

        /// <summary>
        /// Resource to which this support service is applicable.
        /// Likely will be a link to a search. However may need to handle multiple types of searches
        /// </summary>
        public List<string> SupportServiceFor { get; set; }

        //public string SubjectWebpage { get; set; } //URL
        public List<LabelLink> SupportServiceType { get; set; }

        public WMS.AJAXSettings HasSpecificService { get; set; }
        public WMS.AJAXSettings IsSpecificServiceOf { get; set; }
        //retain temporarily7
        public WMS.AJAXSettings SupportServiceReferencedBy { get; set; }
        //
        public List<LabelLink> Connections { get; set; } = new List<LabelLink>();


    }
}

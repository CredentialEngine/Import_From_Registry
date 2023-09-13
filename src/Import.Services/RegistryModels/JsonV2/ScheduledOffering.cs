using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
	public class ScheduledOffering : BaseResourceDocument
	{
		[JsonProperty( "@type" )]
		public string Type { get; set; } = "ceterms:ScheduledOffering";

		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }  //		/resource

		[JsonProperty( "ceterms:ctid" )]
		public string CTID { get; set; }

		[JsonProperty( PropertyName = "ceterms:name" )]
		public LanguageMap Name { get; set; }

		[JsonProperty( PropertyName = "ceterms:description" )]
		public LanguageMap Description { get; set; }

		[JsonProperty( PropertyName = "ceterms:alternateName" )]
		public LanguageMapList AlternateName { get; set; }

		[JsonProperty( PropertyName = "ceterms:subjectWebpage" )]
		public string SubjectWebpage { get; set; } //URL

		[JsonProperty( PropertyName = "ceterms:aggregateData" )]
		public List<AggregateDataProfile> AggregateData { get; set; }

		[JsonProperty( PropertyName = "ceterms:availabilityListing" )]
		public List<string> AvailabilityListing { get; set; }
		[JsonProperty( PropertyName = "ceterms:availableAt" )]
		public List<Place> AvailableAt { get; set; }

		[JsonProperty( PropertyName = "ceterms:availableOnlineAt" )]
		public List<string> AvailableOnlineAt { get; set; }


		[JsonProperty( PropertyName = "ceterms:commonCosts" )]
		public List<string> CommonCosts { get; set; }


		[JsonProperty( PropertyName = "ceterms:deliveryType" )]
		public List<CredentialAlignmentObject> DeliveryType { get; set; }

		[JsonProperty( PropertyName = "ceterms:deliveryTypeDescription" )]
		public LanguageMap DeliveryTypeDescription { get; set; }

		[JsonProperty( PropertyName = "ceterms:estimatedCost" )]
		public List<CostProfile> EstimatedCost { get; set; }

		[JsonProperty( PropertyName = "ceterms:estimatedDuration" )]
		public List<DurationProfile> EstimatedDuration { get; set; }

        /// <summary>
        /// Reference to a relevant support service available for this resource.
        /// </summary>
        [JsonProperty( PropertyName = "ceterms:hasSupportService" )]

        public List<string> HasSupportService { get; set; }
        [JsonProperty( PropertyName = "ceterms:offeredBy" )]
		public List<string> OfferedBy { get; set; }

		[JsonProperty( PropertyName = "ceterms:offerFrequencyType" )]
		public List<CredentialAlignmentObject> OfferFrequencyType { get; set; }

		[JsonProperty( PropertyName = "ceterms:scheduleFrequencyType" )]
		public List<CredentialAlignmentObject> ScheduleFrequencyType { get; set; }

		[JsonProperty( PropertyName = "ceterms:scheduleTimingType" )]
		public List<CredentialAlignmentObject> ScheduleTimingType { get; set; }

	}
}

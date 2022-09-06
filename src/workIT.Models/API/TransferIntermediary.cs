using System.Collections.Generic;

using Newtonsoft.Json;

using WMA = workIT.Models.API;
using WMS = workIT.Models.Search;

namespace workIT.Models.API
{
    [JsonObject( ItemNullValueHandling = NullValueHandling.Ignore )]

	public class TransferIntermediary : BaseAPIType
	{
		public TransferIntermediary()
		{
			EntityTypeId = 28;
			BroadType = "TransferIntermediary";
			CTDLType = "ceterms:TransferIntermediary";
			CTDLTypeLabel = "Transfer Intermediary";
		}

		public string CodedNotation { get; set; }

		public List<ValueProfile> CreditValue { get; set; } = new List<ValueProfile>();

		public WMS.AJAXSettings IntermediaryFor { get; set; }
		//TBD
		public string LifecycleStatusType { get; set; }

		public List<WMA.ConditionProfile> Requires { get; set; }

		public List<LabelLink> Subject { get; set; } = new List<LabelLink>();
		public int HasTransferValueProfiles { get; set; }


	}
}

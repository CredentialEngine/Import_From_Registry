using System.Collections.Generic;

using Newtonsoft.Json;

using workIT.Models.Common;

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
        public List<string> AlternateName { get; set; }
        public string CodedNotation { get; set; }
        public List<LabelLink> Connections { get; set; } = new List<LabelLink>();
        public List<ValueProfile> CreditValue { get; set; } = new List<ValueProfile>();

		public WMS.AJAXSettings IntermediaryFor { get; set; }

		public List<WMA.ConditionProfile> Requires { get; set; }
		public WMS.AJAXSettings RequiresCompetencies { get; set; }
		public List<CredentialAlignmentObjectFrameworkProfile> RequiresCompetenciesFrameworks { get; set; } = new List<CredentialAlignmentObjectFrameworkProfile>();


		public List<LabelLink> Subject { get; set; } = new List<LabelLink>();


	}
}

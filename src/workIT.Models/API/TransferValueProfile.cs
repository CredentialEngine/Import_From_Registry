using System.Collections.Generic;

using Newtonsoft.Json;

using WMS = workIT.Models.Search;

namespace workIT.Models.API
{
    [JsonObject( ItemNullValueHandling = NullValueHandling.Ignore )]
	public class TransferValueProfile : BaseAPIType
	{
		public TransferValueProfile()
		{
			EntityTypeId = 26;
			BroadType = "TransferValueProfile";
			CTDLType = "ceterms:TransferValueProfile";
			CTDLTypeLabel = "Transfer Value Profile";
		}

		/// <summary>
		/// Identifier
		/// Definition:	Alphanumeric token that identifies this resource and information about the token's originating context or scheme.
		/// </summary>	
		public List<IdentifierValue> Identifier { get; set; } = new List<IdentifierValue>();

		public WMS.AJAXSettings DerivedFrom { get; set; }

		public WMS.AJAXSettings RelatedAssessment { get; set; }
		public WMS.AJAXSettings RelatedLearningOpp { get; set; }
		public WMS.AJAXSettings RelatedCredential { get; set; }
		public WMS.AJAXSettings RelatedJob { get; set; }
		public WMS.AJAXSettings RelatedOccupation { get; set; }

		//TVP doesn't have AdministrationProcess
		//public WMS.AJAXSettings AdministrationProcess { get; set; }

		public WMS.AJAXSettings DevelopmentProcess { get; set; } 

		/// <summary>
		/// Date the validity or usefulness of the information in this resource begins.
		/// </summary>
		public string StartDate { get; set; }

		/// <summary>
		/// Date this assertion ends.
		/// </summary>
		public string EndDate { get; set; }

        public string Supersedes { get; set; }
        public string SupersededBy { get; set; }
        /// <summary>
        /// A suggested or articulated credit- or point-related transfer value.
        /// </summary>
        public List<ValueProfile> TransferValue { get; set; } = new List<ValueProfile>();
		/// <summary>
		///  Resource that provides the transfer value described by this resource, according to the entity providing this resource.
		/// </summary>
		public WMS.AJAXSettings TransferValueFrom { get; set; }

		/// <summary>
		///  Resource that accepts the transfer value described by this resource, according to the entity providing this resource.
		/// </summary>
		public WMS.AJAXSettings TransferValueFor { get; set; }

		public WMS.AJAXSettings HasIntermediaryFor { get; set; }

		public LabelLink LatestVersion { get; set; }
		public LabelLink NextVersion { get; set; } //URL
		public LabelLink PreviousVersion { get; set; }
		public List<IdentifierValue> VersionIdentifier { get; set; }
		public string InCatalog { get; set; }
	}
}

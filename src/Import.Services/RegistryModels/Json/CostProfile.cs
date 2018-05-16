using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using RA.Models.Json;


namespace RA.Models.Json
{
	public class CostProfile
	{
		public CostProfile()
		{
            Type = "ceterms:CostProfile";
            Jurisdiction = new List<JurisdictionProfile>();
			//Region = null;
            DirectCostType = new List<CredentialAlignmentObject>();
            AudienceType = new List<CredentialAlignmentObject>();
            ResidencyType = new List<CredentialAlignmentObject>();
			Condition = new List<string>();

		}

        [JsonProperty( "@type" )]
        public string Type { get; set; }

        [JsonProperty( PropertyName = "ceterms:costDetails" )]
		public string CostDetails { get; set; }

		[JsonProperty( PropertyName = "ceterms:currency" )]
		public string Currency { get; set; }

		[JsonProperty( PropertyName = "ceterms:description" )]
		public string Description { get; set; }

		[JsonProperty( PropertyName = "ceterms:name" )]
		public string Name { get; set; }

		[JsonProperty( PropertyName = "ceterms:endDate" )]
		public string EndDate { get; set; }

		[JsonProperty( PropertyName = "ceterms:startDate" )]
		public string StartDate { get; set; }


		[JsonProperty( PropertyName = "ceterms:condition" )]
		public List<string> Condition { get; set; }


		[JsonProperty( PropertyName = "ceterms:jurisdiction" )]
		public List<JurisdictionProfile> Jurisdiction { get; set; }

		[JsonProperty( PropertyName = "ceterms:directCostType" )]
		public List<CredentialAlignmentObject> DirectCostType { get; set; }

		[JsonProperty( PropertyName = "ceterms:residencyType" )]
		public List<CredentialAlignmentObject> ResidencyType { get; set; }

		[JsonProperty( PropertyName = "ceterms:audienceType" )]
		public List<CredentialAlignmentObject> AudienceType { get; set; }

		[JsonProperty( PropertyName = "ceterms:price" )]
		public decimal Price { get; set; }

		[JsonProperty( PropertyName = "ceterms:paymentPattern" )]
		public string PaymentPattern { get; set; }


		//[JsonProperty( PropertyName = "ceterms:region" )]
		//public List<GeoCoordinates> Region { get; set; }
	}
}

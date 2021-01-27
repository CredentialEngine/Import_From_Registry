using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
	public class FinancialAssistanceProfile
	{
		public FinancialAssistanceProfile()
		{
			Type = "ceterms:FinancialAssistanceProfile";
		}
		[JsonProperty( "@type" )]
		public string Type { get; set; }

		[JsonProperty( PropertyName = "ceterms:name" )]
		public LanguageMap Name { get; set; }

		[JsonProperty( PropertyName = "ceterms:description" )]
		public LanguageMap Description { get; set; }

		[JsonProperty( PropertyName = "ceterms:subjectWebpage" )]
		public string SubjectWebpage { get; set; } //URL

		[JsonProperty(PropertyName = "ceterms:financialAssistanceType ")]
      public List<CredentialAlignmentObject> FinancialAssistanceType { get; set; }

		//
		[JsonProperty( PropertyName = "ceterms:financialAssistanceValue" )]
		public List<QuantitativeValue> FinancialAssistanceValue { get; set; } = null;

	}
}

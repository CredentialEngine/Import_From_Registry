using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
	/// <summary>
	/// Will there be a separate class for this, or uses CredentialingAction.
	/// Will it be a top level object with a CTID?
	/// </summary>
	public class WorkforceDemandAction : CredentialingAction
	{
		/// <summary>
		/// type
		/// </summary>
		public WorkforceDemandAction()
		{
			Type = "ceterms:WorkforceDemandAction";
		}

		//[JsonProperty( "@type" )]
		//public string Type { get; set; }

		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }

		#region Required 

		//As this action can be referred to by a competency, implies need for a CTID
		[JsonProperty( PropertyName = "ceterms:ctid" )]
		public string CTID { get; set; }


		//[JsonProperty( PropertyName = "ceterms:actingAgent" )]
		//public string ActingAgent { get; set; }

		//[JsonProperty( PropertyName = "ceterms:description" )]
		//public LanguageMap Description { get; set; }


		#endregion

		///// <summary>
		///// Date this assertion ends.
		///// </summary>
		//[JsonProperty( PropertyName = "ceterms:endDate" )]
		//public string EndDate { get; set; }

		//[JsonProperty( PropertyName = "ceterms:instrument " )]
		//public List<string> Instrument { get; set; }


		//[JsonProperty( PropertyName = "ceterms:jurisdiction" )]
		//public List<JurisdictionProfile> Jurisdiction { get; set; }

		///// <summary>
		///// Object
		///// List of URIs 
		///// </summary>
		//[JsonProperty( PropertyName = "ceterms:object" )]
		//public List<string> Object { get; set; }

		/// <summary>
		/// List of URIs to concept(s)
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:result" )]
		public List<string> Result { get; set; }

	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
	public class BaseEmploymentToWorkObject : BaseResourceDocument
	{

		/// <summary>
		/// Type of official status of the resource; 
		/// URI to a concept
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:lifeCycleStatusType" )]
		public CredentialAlignmentObject LifeCycleStatusType { get; set; }

		/// <summary>
		/// An inventory or listing of resources that includes this resource.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:inCatalog" )]
		public string InCatalog { get; set; }

		/// <summary>
		/// A competency relevant to the resource being described.
		/// </summary>
		//[JsonProperty( PropertyName = "ceterms:targetCompetency" )]
		//public List<string> TargetCompetencyOld { get; set; }


		[JsonProperty( PropertyName = "ceterms:targetCompetency" )]
		public List<CredentialAlignmentObject> TargetCompetency { get; set; }
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
	/// <summary>
	/// History
	/// 21-01-13 Added DevelopementProcess
	/// </summary>
	public class TransferValueProfile
	{
		public TransferValueProfile()
		{
			Type = "ceterms:TransferValueProfile";
		}

		[JsonProperty( "@type" )]
		public string Type { get; set; }

		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }

		[JsonProperty( PropertyName = "ceterms:ctid" )]
		public string CTID { get; set; }
		#region Required 

		[JsonProperty( PropertyName = "ceterms:name" )]
		public LanguageMap Name { get; set; }

		[JsonProperty( PropertyName = "ceterms:description" )]
		public LanguageMap Description { get; set; }

		/// <summary>
		/// A third party version of the entity being referenced that has been modified in meaning through editing, extension or refinement.
		/// </summary>
		[JsonProperty( PropertyName = "ceasn:derivedFrom" )]
		public List<string> DerivedFrom { get; set; }

		[JsonProperty( PropertyName = "ceterms:developmentProcess" )]
		public List<ProcessProfile> DevelopmentProcess { get; set; }

		/// <summary>
		/// The webpage that describes this TransferValueProfile.
		/// URL
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:subjectWebpage" )]
		public string SubjectWebpage { get; set; }

		[JsonProperty( PropertyName = "ceterms:ownedBy" )]
		public List<string> OwnedBy { get; set; }

		#endregion

		/// <summary>
		/// Date this assertion ends.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:endDate" )]
		public string EndDate { get; set; }

		/// <summary>
		/// May be replace by Identifier
		/// </summary>
		//[JsonProperty( PropertyName = "ceterms:codedNotation" )]
		//public string CodedNotation { get; set; }

		/// <summary>
		/// Identifier
		/// Definition:	Alphanumeric Identifier value.
		/// List of URIs 
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:identifierValue" )]
		public List<IdentifierValue> Identifier { get; set; }


		/// <summary>
		/// Date the validity or usefulness of the information in this resource begins.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:startDate" )]
		public string StartDate { get; set; }

		/// <summary>
		/// Type of official status of the TransferProfile; select from an enumeration of such types.
		/// TBD: string or URI to a concept
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:lifecycleStatusType" )]
		public CredentialAlignmentObject LifecycleStatusType { get; set; }


		[JsonProperty( PropertyName = "ceterms:transferValue" )]
		public List<ValueProfile> TransferValue { get; set; } = null;
		//public List<QuantitativeValue> TransferValue { get; set; } = null;


		[JsonProperty( PropertyName = "ceterms:transferValueFor" )]
		public List<string> TransferValueFor { get; set; }

		[JsonProperty( PropertyName = "ceterms:transferValueFrom" )]
		public List<string> TransferValueFrom { get; set; }

		/// <summary>
		/// 
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:transferValueType" )]
		public string TransferValueType { get; set; }

		/*
		 * 
		 * 
		[JsonProperty( PropertyName = "ceterms:instructionalProgramType" )]
		public List<CredentialAlignmentObject> InstructionalProgramType { get; set; } = new List<CredentialAlignmentObject>();
		//

		[JsonProperty( PropertyName = "ceterms:subject" )]
		public List<CredentialAlignmentObject> Subject { get; set; }

		[JsonProperty( PropertyName = "ceterms:targetAssessment" )]
		public List<string> TargetAssessment { get; set; }



		/// <summary>
		///  Learning opportunity that is the focus of a condition, process or another learning opportunity.
		///  List of EntityReferences. 
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:targetLearningOpportunity" )]
		public List<string> TargetLearningOpportunity { get; set; }

		/// <summary>
		/// Provider for the resource that is the focus or target of this resource.
		/// Registry URI, or CTID
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:targetProvidedBy" )]
		public List<string> TargetProvidedBy { get; set; } 

		[JsonProperty( PropertyName = "ceterms:teaches" )]
		public List<CredentialAlignmentObject> Teaches { get; set; }


	*/
	}
}

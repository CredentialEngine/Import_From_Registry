using System.Collections.Generic;

using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
	/// <summary>
	/// History
	/// 21-01-06 remove CodedNotation
	/// </summary>
	public class PathwayComponent
	{
		public PathwayComponent() { }

		/// <summary>
		/// Need a custom mapping to @type based on input value
		/// </summary>
		[JsonProperty( "@type" )]
		public string PathwayComponentType { get; set; }

		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }

		[JsonProperty( PropertyName = "ceterms:ctid" )]
		public string CTID { get; set; }

		//[JsonProperty( PropertyName = "ceasn:codedNotation" )]
		//public string CodedNotation { get; set; }

		//
		[JsonProperty( PropertyName = "ceterms:componentCategory" )]
		public LanguageMap ComponentCategory { get; set; }

		//ceterms:componentDesignation
		[JsonProperty( PropertyName = "ceterms:componentDesignation" )]
		public List<CredentialAlignmentObject> ComponentDesignation { get; set; } = new List<CredentialAlignmentObject>();


		[JsonProperty( PropertyName = "ceterms:description" )]
		public LanguageMap Description { get; set; }


		/// <summary>
		/// This property identifies a child pathwayComponent(s) in the downward path.
		/// ceterms:PathwayComponent
		/// Could use blank nodes. That is a blank node URI to a PathwayComponent
		/// </summary>
		[JsonProperty( PropertyName = "ceasn:hasChild" )]
		public List<string> HasChild { get; set; } = new List<string>();

		/// <summary>
		/// An indicator whether or not the pathway component being described is required for successful completion of its parent component
		/// </summary>
		//[JsonProperty( PropertyName = "ceterms:requiredForParentCompletion" )]
		//public bool RequiredForParentCompletion { get; set; }

		/// <summary>
		/// Resource(s) that describes what must be done to complete a PathwayComponent, or part thereof, as determined by the issuer of the Pathway.
		/// ceterms:ComponentCondition
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:hasCondition" )]
		public List<ComponentCondition> HasCondition { get; set; }


		[JsonProperty( PropertyName = "ceterms:identifierValue" )]
		public List<IdentifierValue> Identifier { get; set; }

		/// <summary>
		/// The referenced resource is higher in some arbitrary hierarchy than this resource.
		/// ceterms:PathwayComponent
		/// </summary>
		[JsonProperty( PropertyName = "ceasn:isChildOf" )]
		public List<string> IsChildOf { get; set; }


		/// <summary>
		/// Pathway for which this resource is the goal or destination.
		/// Like IsTopChildOf
		/// ceterms:Pathway
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:isDestinationComponentOf" )]
		public List<string> IsDestinationComponentOf { get; set; }

		[JsonProperty( PropertyName = "ceasn:isPartOf" )]
		public List<string> IsPartOf { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "asn:hasProgressionLevel" )]
		public List<string> HasProgressionLevel { get; set; }


		[JsonProperty( PropertyName = "ceterms:name" )]
		public LanguageMap Name { get; set; } = new LanguageMap();


		[JsonProperty( PropertyName = "ceterms:credentialType" )]
		public string CredentialType { get; set; }

		/// <summary>
		/// Points associated with this resource, or points possible.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:pointValue" )]
		public QuantitativeValue PointValue { get; set; } = null;
		//
		/// <summary>
		/// Credit Value
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:creditValue" )]
		public List<ValueProfile> CreditValue { get; set; } = null;

		/// <summary>
		/// Resource that logically comes after this resource.
		/// This property indicates a simple or suggested ordering of resources; if a required ordering is intended, use ceterms:prerequisite instead.
		/// ceterms:ComponentCondition
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:preceeds" )]
		public List<string> Preceeds { get; set; }

		/// <summary>
		/// Resource(s) that is required as a prior condition to this resource.
		/// ceterms:ComponentCondition
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:prerequisite" )]
		public List<string> Prerequisite { get; set; }

		/// <summary>
		/// Program term
		/// For course component only 
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:programTerm" )]
		public LanguageMap ProgramTerm { get; set; }

		/// <summary>
		/// URL to structured data representing the resource.
		/// The preferred data serialization is JSON-LD or some other serialization of RDF.
		/// URL
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:sourceData" )]
		public string SourceData { get; set; }


		/// <summary>
		/// The webpage that describes this entity.
		/// URL
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:subjectWebpage" )]
		public string SubjectWebpage { get; set; }


	}

	public class ComponentCondition
	{
		public ComponentCondition()
		{
			Type = "ceterms:ComponentCondition";
		}

		[JsonProperty( "@type" )]
		public string Type { get; set; }

		//[JsonProperty( "@id" )]
		//public string CtdlId { get; set; }

		//[JsonProperty( PropertyName = "ceterms:ctid" )]
		//public string CTID { get; set; }

		[JsonProperty( PropertyName = "ceterms:description" )]
		public LanguageMap Description { get; set; } = new LanguageMap();

		[JsonProperty( PropertyName = "ceterms:name" )]
		public LanguageMap Name { get; set; } = new LanguageMap();

		/// <summary>
		/// Number of targetComponent resources that must be fulfilled in order to satisfy the ComponentCondition.
		/// Integer
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:requiredNumber" )]
		public int RequiredNumber { get; set; }

		/// <summary>
		/// Candidate PathwayComponent for the ComponentCondition as determined by applying the RuleSet.
		/// ceterms:PathwayComponent
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:targetComponent" )]
		public List<string> TargetComponent { get; set; } = new List<string>();
	}
}

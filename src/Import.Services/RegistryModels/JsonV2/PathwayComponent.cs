using System.Collections.Generic;

using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
    /// <summary>
    /// History
    /// 21-01-06 remove CodedNotation
    /// 23-04-30 Add CodedNotation for competencyComponent only
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

        //23-05-26 banished
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

		///// <summary>
		///// An indicator whether or not the pathway component being described is required for successful completion of its parent component
		///// </summary>
		//[JsonProperty( PropertyName = "ceterms:requiredForParentCompletion" )]
		//public bool RequiredForParentCompletion { get; set; }

		/// <summary>
		/// Resource(s) that describes what must be done to complete a PathwayComponent, or part thereof, as determined by the issuer of the Pathway.
		/// ceterms:ComponentCondition
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:hasCondition" )]
		public List<ComponentCondition> HasCondition { get; set; }


		[JsonProperty( PropertyName = "ceterms:identifier" )]
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

		[JsonProperty( PropertyName = "ceterms:alternateName" )]
		public LanguageMapList AlternateName { get; set; }

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
		[JsonProperty( PropertyName = "ceterms:precedes" )]
		public List<string> Precedes { get; set; }

		/// <summary>
		/// Component is preceded by the referenced components
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:precededBy" )]
		public List<string> PrecededBy { get; set; }

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
		/// Indicates the resource for which a pathway component or similar proxy resource is a stand-in.
		/// URI
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:proxyFor" )]
		public string ProxyFor { get; set; }

		[JsonProperty( PropertyName = "ceterms:proxyForList" )]
		public List<string> ProxyForList { get; set; }

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


		#region For JobComponent only
		/// <summary>
		/// IndustryType
		/// Type of industry; select from an existing enumeration of such types such as the SIC, NAICS, and ISIC classifications.
		/// Best practice in identifying industries for U.S. credentials is to provide the NAICS code using the ceterms:naics property. 
		/// Other credentials may use the ceterms:industrytype property and any framework of the class ceterms:IndustryClassification.
		/// ceterms:industryType
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:industryType" )]
		public List<CredentialAlignmentObject> IndustryType { get; set; }

		/// <summary>
		/// OccupationType
		/// Type of occupation; select from an existing enumeration of such types.
		///  For U.S. credentials, best practice is to identify an occupation using a framework such as the O*Net. 
		///  Other credentials may use any framework of the class ceterms:OccupationClassification, such as the EU's ESCO, ISCO-08, and SOC 2010.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:occupationType" )]
		public List<CredentialAlignmentObject> OccupationType { get; set; }
		#endregion

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

		/// <summary>
		/// URI to Concept
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:logicalOperator" )]
		public string LogicalOperator { get; set; }

		/// <summary>
		/// Resource(s) that describes what must be done to complete a PathwayComponent, or part thereof, as determined by the issuer of the Pathway.
		/// ceterms:ComponentCondition
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:hasCondition" )]
		public List<ComponentCondition> HasCondition { get; set; }

        /// <summary>
        /// Number of hasConstraint objects that must be fulfilled in order to satisfy the ComponentCondition.
        /// </summary>
        [JsonProperty( PropertyName = "ceterms:requiredConstraints" )]
        public int RequiredConstraints { get; set; }
        /// <summary>
        /// Resource(s) that describes what must be done to complete a PathwayComponent, or part thereof, as determined by the issuer of the Pathway.
        /// ceterms:ComponentCondition
        /// </summary>
        [JsonProperty( PropertyName = "ceterms:hasConstraint" )]
		public List<Constraint> HasConstraint{ get; set; }


		[JsonProperty( PropertyName = "ceterms:alternateName" )]
		public LanguageMapList AlternateName { get; set; }
	}

	public class Constraint
	{
		public Constraint()
		{
			Type = "ceterms:Constraint";
		}

		[JsonProperty( "@type" )]
		public string Type { get; set; }

		/// <summary>
		/// Constraint Name
		/// Optional
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:name" )]
		public LanguageMap Name { get; set; } = null;


		/// <summary>
		/// Constraint Description 
		/// Optional
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:description" )]
		public LanguageMap Description { get; set; }

		/// <summary>
		/// Type of symbol that denotes an operator in a constraint expression such as "gteq" (greater than or equal to), "eq" (equal to), "lt" (less than), "isAllOf" (is all of), "isAnyOf" (is any of); 
		/// URI to Concept
		/// ceterms:Concept 
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:comparator" )]
		public string Comparator { get; set; }

		/// <summary>
		/// Left hand parameter of a constraint.
		/// Range: rdf:Property, skos:Concept 
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:leftSource" )]

		public List<string> LeftSource { get; set; }

		/// <summary>
		/// Action performed on the left constraint; select from an existing enumeration of such types.
		/// URI to Concept
		/// Range: ceterms:Concept 
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:leftAction" )]

		public string LeftAction { get; set; }

		/// <summary>
		/// Right hand parameter of a constraint.
		/// Range: rdf:Property, skos:Concept 
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:rightSource" )]

		public List<string> RightSource { get; set; }

		/// <summary>
		/// Action performed on the right constraint; select from an existing enumeration of such types.
		/// URI to Concept
		/// Range: ceterms:Concept
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:rightAction" )]
		public string RightAction { get; set; }

		[JsonProperty( PropertyName = "ceterms:alternateName" )]
		public LanguageMapList AlternateName { get; set; }

	}
}

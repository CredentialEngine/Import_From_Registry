using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using workIT.Models.ProfileModels;
using static workIT.Models.Common.CoreObject;
using WMP = workIT.Models.ProfileModels;
namespace workIT.Models.Common
{
	public class Rubric : TopLevelObject
	{
		public Rubric()
		{
		}

		//[JsonProperty( "@type" )]
		public string Type { get; set; } = "ceasn:Rubric";

		//[JsonProperty( "@id" )]
		public string CtdlId { get; set; }  //resource

		//[JsonProperty( "ceterms:ctid" )]
		//public string CTID { get; set; }

		public List<WMP.OrganizationRoleProfile> OrganizationRole { get; set; }
		/// <summary>
		/// A name given to the resource.
		/// </summary>
		////[JsonProperty( "ceasn:name" )]
		//public LanguageMap Name { get; set; }

		///// <summary>
		///// An account of the resource.
		///// </summary>
		////[JsonProperty( "ceasn:description" )]
		//public LanguageMap Description { get; set; }

		/// <summary>
		/// Entity describing the process by which this resource, are administered.
		/// ceterms:administrationProcess
		/// </summary>
		//[JsonProperty( PropertyName = "ceterms:administrationProcess" )]
		public List<WMP.ProcessProfile> AdministrationProcess { get; set; }

		/// <summary>
		/// Alternative Coded Notation
		/// An alphanumeric notation or ID code identifying this competency in common use among end-users.
		/// </summary>
		//[JsonProperty( "ceasn:altCodedNotation" )]
		public List<string> AltCodedNotation { get; set; }

		//[JsonProperty( PropertyName = "ceterms:audienceLevelType" )]
		public Enumeration AudienceLevelType { get; set; }

		//[JsonProperty( PropertyName = "ceterms:audienceType" )]
		public Enumeration AudienceType { get; set; }

		//[JsonProperty( "ceasn:author" )]
		public string Author { get; set; }

		/// <summary>
		/// Category or classification of this resource.
		/// List of URIs that point to a concept
		/// </summary>
		//[JsonProperty( "ceterms:classification" )]
		public List<ResourceSummary> Classification { get; set; } = new List<ResourceSummary>();

		//[JsonProperty( PropertyName = "ceasn:codedNotation" )]
		public string CodedNotation { get; set; }

		//[JsonProperty( "ceasn:creator" )]
		public List<Guid> Creator { get; set; }
		public List<ResourceSummary> CreatorList { get; set; } = new List<ResourceSummary>();

		//[JsonProperty( "ceasn:dateCopyrighted" )]
		public string DateCopyrighted { get; set; }


		/// <summary>
		/// Only allow date (yyyy-mm-dd), no time
		/// xsd:date
		/// </summary>
		//[JsonProperty( "ceasn:dateCreated" )]
		public string DateCreated { get; set; }

		/// <summary>
		/// Originally only allowing date (yyyy-mm-dd), no time. 
		/// However, this is defined as: xsd:dateTime. So consumers like the credential registry search, expect a datetime format.
		/// </summary>
		//[JsonProperty( "ceasn:dateModified" )]
		public string DateModified { get; set; }

		/// <summary>
		/// xsd:dateTime
		/// </summary>
		//[JsonProperty( "ceasn:dateValidFrom" )]
		public string DateValidFrom { get; set; }

		/// <summary>
		/// xsd:dateTime
		/// </summary>
		//[JsonProperty( "ceasn:dateValidUntil" )]
		public string DateValidUntil { get; set; }

		//[JsonProperty( PropertyName = "ceterms:deliveryType" )]
		public Enumeration DeliveryType { get; set; }

		/// <summary>
		/// Derived From
		/// A third party version of the entity being reference that has been modified in meaning through editing, extension or refinement.
		/// List of URIs to frameworks
		/// ceasn:derivedFrom
		/// </summary>
		public List<string> DerivedFrom { get; set; }


		/// <summary>
		/// A general statement describing the education or training context. Alternatively, a more specific statement of the location of the audience in terms of its progression through an education or training context.
		/// ConceptScheme: ceterms:AudienceLevel
		/// </summary>
		public Enumeration EducationLevelType { get; set; }



		/// <summary>
		/// Type of evaluator; select from an existing enumeration of such types.
		/// ConceptScheme: ceasn:EvaluatorCategory
		/// </summary>
		public Enumeration EvaluatorType { get; set; }

		//????these are URIs - could imply RubricCriterion is to be a top level class
		// RubricCriterian referenced defines a principle or standard to be met that demonstrates quality in performance of a task or obtaining an objective.
		/// List of CTIDs/URIs to a RubricCriterion
		/// </summary>
		public List<string> HasRubricCriterion { get; set; }

		/// <summary>
		/// List of blank node identifiers that refer to an entry in request.RubricLevel
		/// </summary>
		public List<string> HasRubricLevel { get; set; }

		/// <summary>
		/// Has Criterion Category Set
		/// Indicates the Concept Scheme for clustering logical sets of Rubric Criteria.
		/// </summary>
		public ResourceSummary HasCriterionCategorySet { get; set; }
		public Guid HasCriterionCategorySetUid { get; set; }

		/// <summary>
		/// Reference to a progression model used.
		/// </summary>
		//[JsonProperty( "asn:hasProgressionModel" )]
		public ResourceSummary HasProgressionModel { get; set; }
		public Guid HasProgressionModelUid { get; set; }

		//[JsonProperty( "asn:hasProgressionLevel" )]
		public ResourceSummary HasProgressionLevel { get; set; }
		public string HasProgressionLevelCTID { get; set; }

		/// <summary>
		/// Description of what the rubric's creator intended to assess or evaluate.
		/// </summary>
		//[JsonProperty( "asn:hasScope" )]
		public LanguageMap HasScopeMap { get; set; }

		public string HasScope { get; set; }
		public string InCatalog { get; set; }

		/// <summary>
		/// An unambiguous reference to the resource within a given context.
		/// Recommended practice is to identify the resource by means of a string conforming to an identification system. Examples include International Standard Book Number (ISBN), Digital Object Identifier (DOI), and Uniform Resource Name (URN). Persistent identifiers should be provided as HTTP URIs.
		/// </summary>
		//[JsonProperty( PropertyName = "ceterms:identifier" )]
		public List<IdentifierValue> Identifier { get; set; }
		public string IdentifierJson { get; set; }

		//[JsonProperty( "ceasn:inLanguage" )]
		public List<string> InLanguage { get; set; }
		public List<TextValueProfile> InLanguageCodeList { get; set; }

		//[JsonProperty( PropertyName = "ceterms:conceptKeyword" )]
		public List<string> ConceptKeyword { get; set; }

		/// <summary>
		/// A legal document giving official permission to do something with this resource.
		/// </summary>
		//[JsonProperty( "ceasn:license" )]
		public string License { get; set; }

		/// <summary>
		/// Type of official status of the TransferProfile; select from an enumeration of such types.
		/// The default is Active. 
		/// ConceptScheme: ceterms:LifeCycleStatus
		/// </summary>
		//[JsonProperty( PropertyName = "ceterms:lifeCycleStatusType" )]
		public Enumeration LifeCycleStatusType { get; set; }
		public string LifeCycleStatus { get; set; }
		public int LifeCycleStatusTypeId { get; set; }


		//[JsonProperty( PropertyName = "ceterms:latestVersion" )]
		public string LatestVersion { get; set; } //URL

		//[JsonProperty( PropertyName = "ceterms:nextVersion" )]
		public string NextVersion { get; set; } //URL

		//[JsonProperty( PropertyName = "ceterms:previousVersion " )]
		public string PreviousVersion { get; set; } //URL


		//[JsonProperty( PropertyName = "ceterms:offeredIn" )]
		public List<JurisdictionProfile> OfferedIn { get; set; }

		//[JsonProperty( "ceasn:publicationStatusType" )]
		public string PublicationStatusType { get; set; }

		/// <summary>
		/// An agent responsible for making this entity available.
		/// Also referred to as the promulgating agency of the entity.
		/// List of URIs, for example to a ceterms:CredentialOrganization
		/// Or provide a list of CTIDs and the Assistant API will format the proper URL for the environment.
		/// Required
		/// </summary>
		public List<Guid> Publisher { get; set; }
		public List<ResourceSummary> PublisherList { get; set; } = new List<ResourceSummary>();

		/// <summary>
		/// Name of an agent responsible for making this entity available.
		/// </summary>
		public List<string> PublisherName { get; set; }
		//
		/// <summary>
		/// Information about rights held in and over this resource.
		/// ceasn:rights
		/// </summary>
		//[JsonProperty( "ceasn:rights" )]
		public string Rights { get; set; }

		/// <summary>
		/// Original resource on which this resource is based or derived from.
		/// </summary>
		//[JsonProperty( "ceterms:source" )]  //??? 
		public string Source { get; set; }  //URI

		/// <summary>
		/// Words or brief phrases describing the topicality of the entity; select subject terms from an existing enumeration of such terms.
		/// </summary>
		public List<TextValueProfile> Subject { get; set; } = new List<TextValueProfile>();

		////[JsonProperty( "ceterms:subjectWebpage" )]  //??? 
		//public string SubjectWebpage { get; set; }  //URI

		#region Occupation, Industry, Program
		public List<CredentialAlignmentObjectProfile> OccupationTypes { get; set; } = new List<CredentialAlignmentObjectProfile>();
		public List<CredentialAlignmentObjectProfile> IndustryTypes { get; set; } = new List<CredentialAlignmentObjectProfile>();
		public List<CredentialAlignmentObjectProfile> InstructionalProgramTypes { get; set; } = new List<CredentialAlignmentObjectProfile>();
		#endregion


		/// <summary>
		/// Occupation that is the focus or target of this resource.
		/// List of CTIDs, URIs or blank nodes
		/// </summary>
		public List<ResourceSummary> TargetOccupation { get; set; } = new List<ResourceSummary>();

		/// <summary>
		/// VersionIdentifier
		/// Alphanumeric identifier of the version of the credential that is unique within the organizational context of its owner.
		/// The credential version captured here is any local identifier used by the credential owner to identify the version of the credential in the its local system.
		/// </summary>
		public List<IdentifierValue> VersionIdentifier { get; set; }
		public string VersionIdentifierJson { get; set; }


		public List<RubricCriterion> RubricCriterion { get; set; } = new List<RubricCriterion>();
        public List<RubricLevel> RubricLevel { get; set; } = new List<RubricLevel>();
        public List<CriterionLevel> CriterionLevel { get; set; } = new List<CriterionLevel>();
		public ProgressionModel ProgressionModel { get; set; } 

		public List<int> DerivedFromForImport { get; set; }
		public List<int> TargetOccupationIds { get; set; }
	}

	public class RubricCriterion: TopLevelObject
	{
		public RubricCriterion()
		{
		}

		#region Base properties
	
		//[JsonProperty( "@type" )]
		public string Type { get; set; } = "ceasn:RubricCriterion";

		//[JsonProperty( "@id" )]
		public string CtdlId { get; set; }  //resource

        public int RubricId { get; set; }

        //[JsonProperty( "ceterms:ctid" )]
        //public string CTID { get; set; }

        //[JsonProperty( "ceasn:codedNotation" )]
        public string CodedNotation { get; set; }

		/// <summary>
		/// An account of the resource.
		/// </summary>
		//[JsonProperty( "ceasn:description" )]
		//public string Description { get; set; }

		////[JsonProperty( "dcterms:Language" )]
		//public List<string> Language { get; set; }

		//[JsonProperty( PropertyName = "asn:hasProgressionLevel" )]
		public ResourceSummary HasProgressionLevel { get; set; }
		public string HasProgressionLevelCTID { get; set; }

		/// <summary>
		/// Numeric value representing the resource's position in a list (array) of resources.
		/// </summary>
		//[JsonProperty( "ceasn:listID" )]
		public string ListID { get; set; }


		/// <summary>
		/// A name given to the resource.
		/// </summary>
		//[JsonProperty( "ceasn:name" )]
		//public string Name { get; set; }

		//[JsonProperty( "ceasn:weight" )]
		public decimal? Weight { get; set; }
		#endregion

		#region relationship properties

		/// <summary>
		/// Indicates a Concept for clustering logical sets of Rubric Criteria.
		/// </summary>
		//[JsonProperty( "ceasn:hasCriterionCategory" )]
		public List<string> HasCriterionCategory { get; set; }

		/// <summary>
		/// Criterion Level for this resource.
		/// </summary>
		//[JsonProperty( "ceasn:hasCriterionLevel" )]
		public List<CriterionLevel> HasCriterionLevel { get; set; }
		public List<String> HasCriterionLevelUids { get; set; }
		#endregion`


		/// <summary>
		/// Task that is the focus or target of this resource.
		/// </summary>
		//[JsonProperty( PropertyName = "ceterms:targetTask" )]
		public List<ResourceSummary> TargetTask { get; set; }

		//[JsonProperty( PropertyName = "ceterms:targetCompetency" )]
		public List<CredentialAlignmentObjectProfile> TargetCompetency { get; set; } = new List<CredentialAlignmentObjectProfile>();

		public List<int> TargetTaskIds { get; set; }

	}

	public class RubricLevel:TopLevelObject
	{
		/*
asn:hasProgressionLevel		 * 
d	ceasn:codedNotation
d	ceasn:description
ceasn:hasCriterionLevel
ceasn:listID
d	ceasn:name


		*/
		public RubricLevel()
		{
		}
		//[JsonProperty( "@type" )]
		public string Type { get; set; } = "ceasn:RubricLevel";

		/// <summary>
		/// The identifier for a Rubric level.
		/// Must be a valid blank node identifier: _:UUID
		/// example:		_:9c09016c-907e-46ab-8d63-c1cda5474836
		/// Classes with the property hasRubricLevel would a list of strings (this bnode identifier)
		/// </summary>
		//[JsonProperty( "@id" )]
		public string CtdlId { get; set; }  //resource

        public int RubricId { get; set; }

        /// <summary>
        /// A name given to the resource.
        /// </summary>
        //[JsonProperty( "ceasn:name" )]
        //	public string Name { get; set; }

        /// <summary>
        /// An account of the resource.
        /// </summary>
        //[JsonProperty( "ceasn:description" )]
        //public string Description { get; set; }


        //[JsonProperty( PropertyName = "ceasn:codedNotation" )]
        public string CodedNotation { get; set; }


		/// <summary>
		/// RubricCriterian referenced defines a principle or standard to be met that demonstrates quality in performance of a task or obtaining an objective.
		/// </summary>
		//[JsonProperty( "ceasn:hasCriterionLevel" )]
		public List<CriterionLevel> HasCriterionLevel { get; set; }
		public List<String> HasCriterionLevelUids { get; set; }

		/// <summary>
		/// Reference to a progression model used.
		/// **** should this be single???????????
		/// </summary>
		//[JsonProperty( "asn:hasProgressionLevel" )]
		public ResourceSummary HasProgressionLevel { get; set; }
		public string HasProgressionLevelCTID { get; set; }

		/// <summary>
		/// Numeric value representing the resource's position in a list (array) of resources.
		/// </summary>
		//[JsonProperty( "ceasn:listID" )]
		public string ListID { get; set; }

    }


	public class CriterionLevel: TopLevelObject
	{
		public CriterionLevel()
		{
		}

		//[JsonProperty( "@type" )]
		public string Type { get; set; } = "ceasn:CriterionLevel";
        public int RubricId { get; set; }
        /// <summary>
        /// The identifier for a Rubric level.
        /// Must be a valid blank node identifier: _:UUID
        /// example:		_:9c09016c-907e-46ab-8d63-c1cda5474836
        /// Classes with the property hasCriterioniLevel would a list of strings (this bnode identifier)
        /// </summary>
        //[JsonProperty( "@id" )]
        public string CtdlId { get; set; }  //resource

		#region base properties


		////[JsonProperty( "@id" )]
		//public string CtdlId { get; set; }  //resource

		////[JsonProperty( "ceterms:ctid" )]
		//public string CTID { get; set; }

		/// <summary>
		/// Label for the level achieved as defined by the Rubric Criterion.
		/// </summary>
		//[JsonProperty( "ceasn:benchmarkLabel" )]
		public string BenchmarkLabel { get; set; } //??

		/// <summary>
		/// Description of the level achieved as defined by the Rubric Criterion.
		/// </summary>
		//[JsonProperty( "ceasn:BenchmarkText" )]
		public string BenchmarkText { get; set; }

		/// <summary>
		/// An alphanumeric notation or ID code as defined by the promulgating body to identify this resource.
		/// </summary>
		//[JsonProperty( "ceasn:codedNotation" )]
		public string CodedNotation { get; set; }

		/// <summary>
		/// Indicates whether the criterion level is evaluated as having been met or not.
		/// </summary>
		//[JsonProperty( "ceasn:isBinaryEvaluation" )]
		//public bool? IsBinaryEvaluation { get; set; } = null;

		//[JsonProperty( "ceasn:listID" )]
		public string ListID { get; set; }


		/// <summary>
		/// Qualitative description of the degree of achievement used for a column header in a tabular rubric.
		/// </summary>
		//[JsonProperty( "ceasn:feedback" )]
		public string Feedback { get; set; }

		/// <summary>
		/// Points to be awarded for achieving this level for a RubricCriterion.
		/// </summary>
		//[JsonProperty( "schema:value" )]
		public decimal? Value { get; set; } = null;

		//[JsonProperty( "schema:minValue" )]
		public decimal? MinValue { get; set; } = null;

		//[JsonProperty( "schema:maxValue" )]
		public decimal? MaxValue { get; set; } = null;

		//[JsonProperty( "qdata:percentage" )]
		public decimal? Percentage { get; set; } = null;

		//[JsonProperty( "qdata:minPercentage" )]
		public decimal? MinPercentage { get; set; } = null;

		//[JsonProperty( "qdata:maxPercentage" )]
		public decimal? MaxPercentage { get; set; } = null;


		#endregion

		#region relationship properties

		/// <summary>
		/// Reference to the RubricCriterion to which the CriterionLevel being described belongs.
		/// </summary>
		//[JsonProperty( "ceasn:hasCriterionLevel" )]
		public List<string> HasCriterionLevel { get; set; }

		/// <summary>
		/// Reference to a progression model used.
		/// **** should this be single???????????
		/// </summary>
		//[JsonProperty( "asn:hasProgressionLevel" )]
		public List<string> HasProgressionLevel { get; set; }

		#endregion
	}
	public class Entity_HasCriterionLevel
	{
		public int Id { get; set; }
		public int EntityId { get; set; }
		public int CriterionLevelId { get; set; }
		public System.DateTime Created { get; set; }
		public int CreatedById { get; set; }
		public virtual Entity Entity { get; set; }
		public virtual CriterionLevel Pathway { get; set; }
	}
}

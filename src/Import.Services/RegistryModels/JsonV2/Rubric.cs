using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
	public class Rubric : BaseResourceDocument
	{
		public Rubric()
		{
		}

		[JsonProperty( "@type" )]
		public string Type { get; set; } = "ceasn:Rubric";

		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }  //resource

		[JsonProperty( "ceterms:ctid" )]
		public string CTID { get; set; }


        /// <summary>
        /// A name given to the resource.
        /// </summary>
        [JsonProperty( "ceasn:name" )]
        public LanguageMap Name { get; set; }

        /// <summary>
        /// An account of the resource.
        /// </summary>
        [JsonProperty( "ceasn:description" )]
		public LanguageMap Description { get; set; }

        /// <summary>
        /// Entity describing the process by which this resource, are administered.
        /// ceterms:administrationProcess
        /// </summary>
        [JsonProperty( PropertyName = "ceterms:administrationProcess" )]
        public List<ProcessProfile> AdministrationProcess { get; set; }

        /// <summary>
        /// Alternative Coded Notation
        /// An alphanumeric notation or ID code identifying this competency in common use among end-users.
        /// </summary>
        [JsonProperty( "ceasn:altCodedNotation" )]
        public List<string> AltCodedNotation { get; set; }

        [JsonProperty( PropertyName = "ceterms:audienceLevelType" )]
        public List<CredentialAlignmentObject> AudienceLevelType { get; set; }

        [JsonProperty( PropertyName = "ceterms:audienceType" )]
        public List<CredentialAlignmentObject> AudienceType { get; set; }

		/// <summary>
		/// Category or classification of this resource.
		/// List of URIs that point to a concept
		/// </summary>
		[ JsonProperty( "ceterms:classification" )]
        public List<string> Classification { get; set; }

        [JsonProperty( PropertyName = "ceasn:codedNotation" )]
        public string CodedNotation { get; set; }

        [JsonProperty( "ceasn:creator" )]
        public List<string> Creator { get; set; }

        [JsonProperty( "ceasn:dateCopyrighted" )]
        public string DateCopyrighted { get; set; }


        /// <summary>
        /// Only allow date (yyyy-mm-dd), no time
        /// xsd:date
        /// </summary>
        [JsonProperty( "ceasn:dateCreated" )]
        public string DateCreated { get; set; }

        ///// <summary>
        ///// Only allow date (yyyy-mm-dd), no time
        ///// xsd:date
        ///// </summary>
        //[JsonProperty( "ceterms:dateEffective" )]
        //public string DateEffective{ get; set; }

        /// <summary>
        /// Originally only allowing date (yyyy-mm-dd), no time. 
        /// However, this is defined as: xsd:dateTime. So consumers like the credential registry search, expect a datetime format.
        /// </summary>
        [JsonProperty( "ceasn:dateModified" )]
        public string DateModified { get; set; }

        /// <summary>
        /// xsd:dateTime
        /// </summary>
        [JsonProperty( "ceasn:dateValidFrom" )]
        public string DateValidFrom { get; set; }

        /// <summary>
        /// xsd:dateTime
        /// </summary>
        [JsonProperty( "ceasn:dateValidUntil" )]
        public string DateValidUntil { get; set; }
		
		[ JsonProperty( PropertyName = "ceterms:deliveryType" ) ]
		public List<CredentialAlignmentObject> DeliveryType { get; set; }

		[ JsonProperty( "ceasn:derivedFrom" )]
        public List<string> DerivedFrom { get; set; }


		[JsonProperty( PropertyName = "ceterms:educationLevelType" )]
		public List<CredentialAlignmentObject> EducationLevelType { get; set; }


		/// <summary>
		/// Evaluator Type
		/// Type of evaluator; select from an existing enumeration of such types.
		/// </summary>
		[JsonProperty( PropertyName = "ceasn:evaluatorType" )]
		public List<string> EvaluatorType { get; set; }

		//
		///// <summary>
		///// Date beyond which the resource is no longer offered or available.
		///// Only allowing date (yyyy-mm-dd), no time. 
		///// xsd:date
		///// ceterms:expirationDate
		///// </summary>
		//[JsonProperty( "ceterms:expirationDate" )]
		//public string ExpirationDate { get; set; }

		//????these are URIs - could imply RubricCriterion is to be a top level class
		/// <summary>
		/// RubricCriterian referenced defines a principle or standard to be met that demonstrates quality in performance of a task or obtaining an objective.
		/// </summary>
		[JsonProperty( "ceasn:hasRubricCriterion" )]
		public List<string> HasRubricCriterion { get; set; }

		[JsonProperty( "ceasn:hasRubricLevel" )]
		public List<string> HasRubricLevel { get; set; }

		///// <summary>
		///// Has Criterion Category
		///// Resource referenced by the Rubric that defines categories for clustering logical sets of RubricCriterion.
		///// </summary>
		//[JsonProperty( "asn:hasCriterionCategory" )]
		//public List<string> HasCriterionCategory { get; set; }

		/// <summary>
		/// Has Criterion Category Set
		/// Indicates the Concept Scheme for clustering logical sets of Rubric Criteria.
		/// </summary>
		[JsonProperty( "asn:hasCriterionCategorySet" )]
		public string HasCriterionCategorySet { get; set; }

		/// <summary>
		/// Reference to a progression model used.
		/// </summary>
		[JsonProperty( "asn:hasProgressionModel" )]  
		public string HasProgressionModel { get; set; }

        [JsonProperty( "asn:hasProgressionLevel" )]
        public string HasProgressionLevel{ get; set; }

		/// <summary>
		/// Description of what the rubric's creator intended to assess or evaluate.
		/// </summary>
		[JsonProperty( "asn:hasScope" )]
		public LanguageMap HasScope { get; set; }

		/// <summary>
		/// An unambiguous reference to the resource within a given context.
		/// Recommended practice is to identify the resource by means of a string conforming to an identification system. Examples include International Standard Book Number (ISBN), Digital Object Identifier (DOI), and Uniform Resource Name (URN). Persistent identifiers should be provided as HTTP URIs.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:identifier" )]
        public List<IdentifierValue> Identifier { get; set; }

		/// <summary>
		/// An inventory or listing of resources that includes this resource.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:inCatalog" )]
		public string InCatalog { get; set; }

		[JsonProperty( "ceasn:inLanguage" )]
		public List<string> InLanguage { get; set; }

		[JsonProperty( PropertyName = "ceterms:conceptKeyword" )]
		public LanguageMapList ConceptKeyword { get; set; }

		/// <summary>
		/// A legal document giving official permission to do something with this resource.
		/// </summary>
		[JsonProperty( "ceasn:license" )]
        public string License { get; set; }

		/// <summary>
		/// Type of official status of the TransferProfile; select from an enumeration of such types.
		/// The default is Active. 
		/// ConceptScheme: ceterms:LifeCycleStatus
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:lifeCycleStatusType" )]
		public CredentialAlignmentObject LifeCycleStatusType { get; set; }


		[JsonProperty( PropertyName = "ceterms:latestVersion" )]
        public string LatestVersion { get; set; } //URL

        [JsonProperty( PropertyName = "ceterms:nextVersion" )]
        public string NextVersion { get; set; } //URL

        [JsonProperty( PropertyName = "ceterms:previousVersion " )]
        public string PreviousVersion { get; set; } //URL


        [JsonProperty( PropertyName = "ceterms:offeredIn" )]
        public List<JurisdictionProfile> OfferedIn { get; set; }

        [JsonProperty( "ceasn:publicationStatusType" )]
        public string PublicationStatusType { get; set; }

        //
        [JsonProperty( "ceasn:publisher" )]
        public List<string> Publisher { get; set; }

        [JsonProperty( "ceasn:publisherName" )]
        public LanguageMapList PublisherName { get; set; }
        //
        /// <summary>
        /// Information about rights held in and over this resource.
        /// ceasn:rights
        /// </summary>
        [JsonProperty( "ceasn:rights" )]
        public LanguageMap Rights { get; set; }


        [JsonProperty( PropertyName = "ceterms:subject" )]
        public List<CredentialAlignmentObject> Subject { get; set; }

		/// <summary>
		/// Webpage that describes this entity.
		/// </summary>
		[JsonProperty( "ceterms:subjectWebpage" )]  
        public string SubjectWebpage { get; set; }  //URI

        #region Occupation, Industry, Program
        [JsonProperty( PropertyName = "ceterms:occupationType" )]
        public List<CredentialAlignmentObject> OccupationType { get; set; } = new List<CredentialAlignmentObject>();

        [JsonProperty( PropertyName = "ceterms:industryType" )]
        public List<CredentialAlignmentObject> IndustryType { get; set; } = new List<CredentialAlignmentObject>();


        [JsonProperty( PropertyName = "ceterms:instructionalProgramType" )]
        public List<CredentialAlignmentObject> InstructionalProgramType { get; set; } = new List<CredentialAlignmentObject>();
		#endregion


		[JsonProperty( PropertyName = "ceterms:targetOccupation" )]
		public List<string> TargetOccupation { get; set; }

		[JsonProperty( PropertyName = "ceterms:versionIdentifier" )]
        public List<IdentifierValue> VersionIdentifier { get; set; }
    }

    public class RubricCriterion
	{
		public RubricCriterion()
		{
		}

		#region Base properties
		[JsonProperty( "@type" )]
		public string Type { get; set; } = "ceasn:RubricCriterion";

		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }  //resource

		[JsonProperty( "ceterms:ctid" )]
		public string CTID { get; set; }

		[JsonProperty( "ceasn:codedNotation" )]
		public string CodedNotation { get; set; }

		/// <summary>
		/// An account of the resource.
		/// </summary>
		[JsonProperty( "ceasn:description" )]
		public LanguageMap Description { get; set; }

		//[JsonProperty( "dcterms:Language" )]
		//public List<string> Language { get; set; }

		[JsonProperty( PropertyName = "asn:hasProgressionLevel" )]
		public List<string> HasProgressionLevel { get; set; }

		/// <summary>
		/// Numeric value representing the resource's position in a list (array) of resources.
		/// </summary>
		[JsonProperty( "ceasn:listID" )]
		public string ListID { get; set; }


		/// <summary>
		/// A name given to the resource.
		/// </summary>
		[JsonProperty( "ceasn:name" )]
		public LanguageMap Name { get; set; }

		[JsonProperty( "ceasn:weight" )]
		public decimal? Weight { get; set; }
		#endregion

		#region relationship properties

		/// <summary>
		/// Indicates a Concept for clustering logical sets of Rubric Criteria.
		/// </summary>
		[JsonProperty( "ceasn:hasCriterionCategory" )]
		public List<string> HasCriterionCategory { get; set; }

		/// <summary>
		/// Criterion Level for this resource.
		/// </summary>
		[JsonProperty( "ceasn:hasCriterionLevel" )]
		public List<string> HasCriterionLevel { get; set; }
		#endregion`


		/// <summary>
		/// Task that is the focus or target of this resource.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:targetTask" )]
		public List<string> TargetTask { get; set; }

		[JsonProperty( PropertyName = "ceterms:targetCompetency" )]
		public List<CredentialAlignmentObject> TargetCompetency { get; set; }

	}

	public class RubricLevel
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
		[JsonProperty( "@type" )]
		public string Type { get; set; } = "ceasn:RubricLevel";

		/// <summary>
		/// The identifier for a Rubric level.
		/// Must be a valid blank node identifier: _:UUID
		/// example:		_:9c09016c-907e-46ab-8d63-c1cda5474836
		/// Classes with the property hasRubricLevel would a list of strings (this bnode identifier)
		/// </summary>
		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }  //resource

		/// <summary>
		/// A name given to the resource.
		/// </summary>
		[JsonProperty( "ceasn:name" )]
		public LanguageMap Name { get; set; }

		/// <summary>
		/// An account of the resource.
		/// </summary>
		[JsonProperty( "ceasn:description" )]
		public LanguageMap Description { get; set; }


		[JsonProperty( PropertyName = "ceasn:codedNotation" )]
		public string CodedNotation { get; set; }


		/// <summary>
		/// RubricCriterian referenced defines a principle or standard to be met that demonstrates quality in performance of a task or obtaining an objective.
		/// </summary>
		[JsonProperty( "ceasn:hasCriterionLevel" )]
		public List<string> HasCriterionLevel { get; set; }

		/// <summary>
		/// Reference to a progression model used.
		/// **** should this be single???????????
		/// </summary>
		[JsonProperty( "asn:hasProgressionLevel" )]
		public List<string> HasProgressionLevel { get; set; }

		/// <summary>
		/// Numeric value representing the resource's position in a list (array) of resources.
		/// </summary>
		[JsonProperty( "ceasn:listID" )]
		public string ListID { get; set; }

	}


	public class CriterionLevel
	{
		public CriterionLevel()
		{
		}

		[JsonProperty( "@type" )]
		public string Type { get; set; } = "ceasn:CriterionLevel";

		/// <summary>
		/// The identifier for a Rubric level.
		/// Must be a valid blank node identifier: _:UUID
		/// example:		_:9c09016c-907e-46ab-8d63-c1cda5474836
		/// Classes with the property hasCriterioniLevel would a list of strings (this bnode identifier)
		/// </summary>
		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }  //resource

		#region base properties


		//[JsonProperty( "@id" )]
		//public string CtdlId { get; set; }  //resource

		//[JsonProperty( "ceterms:ctid" )]
		//public string CTID { get; set; }

		/// <summary>
		/// Label for the level achieved as defined by the Rubric Criterion.
		/// </summary>
		[JsonProperty( "ceasn:benchmarkLabel" )]
		public LanguageMap BenchmarkLabel { get; set; } //??

		/// <summary>
		/// Description of the level achieved as defined by the Rubric Criterion.
		/// </summary>
		[JsonProperty( "ceasn:BenchmarkText" )]
		public LanguageMap BenchmarkText { get; set; }

		/// <summary>
		/// An alphanumeric notation or ID code as defined by the promulgating body to identify this resource.
		/// </summary>
		[JsonProperty( "ceasn:codedNotation" )]
		public string CodedNotation { get; set; }

		/// <summary>
		/// Indicates whether the criterion level is evaluated as having been met or not.
		/// </summary>
		[JsonProperty( "ceasn:isBinaryEvaluation" )]
		public bool? IsBinaryEvaluation { get; set; } = null;

		[JsonProperty( "ceasn:listID" )]
		public string ListID { get; set; }


		/// <summary>
		/// Qualitative description of the degree of achievement used for a column header in a tabular rubric.
		/// </summary>
		[JsonProperty( "ceasn:feedback" )]
		public LanguageMap Feedback { get; set; }

		/// <summary>
		/// Points to be awarded for achieving this level for a RubricCriterion.
		/// </summary>
		[JsonProperty( "schema:value" )]
		public decimal? Value { get; set; } = null;

		[JsonProperty( "schema:minValue" )]
		public decimal? MinValue { get; set; } = null;

		[JsonProperty( "schema:maxValue" )]
		public decimal? MaxValue { get; set; } = null;

		[JsonProperty( "qdata:percentage" )]
		public decimal? Percentage { get; set; } = null;

		[JsonProperty( "qdata:minPercentage" )]
		public decimal? MinPercentage { get; set; } = null;

		[JsonProperty( "qdata:maxPercentage" )]
		public decimal? MaxPercentage { get; set; } = null;


		#endregion

		#region relationship properties

		/// <summary>
		/// Reference to the RubricCriterion to which the CriterionLevel being described belongs.
		/// </summary>
		[JsonProperty( "ceasn:hasCriterionLevel" )]
		public List<string> HasCriterionLevel { get; set; }

		/// <summary>
		/// Reference to a progression model used.
		/// **** should this be single???????????
		/// </summary>
		[JsonProperty( "asn:hasProgressionLevel" )]
		public List<string> HasProgressionLevel { get; set; }

		#endregion
	}

	/*
	public class CriterionCategory
	{
		public CriterionCategory()
		{
		}

		[JsonProperty( "@type" )]
		public string Type { get; set; } = "asn:CriterionCategory";

		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }  //resource

		[JsonProperty( "ceterms:ctid" )]
		public string CTID { get; set; }


		/// <summary>
		/// An account of the resource.
		/// </summary>
		[JsonProperty( "dcterms:description" )]
		public LanguageMap Description { get; set; }

		[JsonProperty( "dcterms:Language" )]
		public List<string> Language { get; set; }

		/// <summary>
		/// A name given to the resource.
		/// </summary>
		[JsonProperty( "dcterms:title" )]
		public LanguageMap Title { get; set; }

		/// <summary>
		/// Resource referenced is a Rubric to which this CriterionCategory belongs.
		/// </summary>
		[JsonProperty( "asn:criterionCategoryOf" )]
		public List<string> CriterionCategoryOf { get; set; }

		/// <summary>
		/// RubricCriterian referenced defines a principle or standard to be met that demonstrates quality in performance of a task or obtaining an objective.
		/// </summary>
		[JsonProperty( "asn:hasCriterion" )]
		public List<string> HasCriterion { get; set; }

	}

	*/

}

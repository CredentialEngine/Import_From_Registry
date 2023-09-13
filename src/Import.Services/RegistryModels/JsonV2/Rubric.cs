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

        /// <summary>
        /// List of Alternate Names for this learning opportunity
        /// </summary>
        [JsonProperty( PropertyName = "ceterms:alternateName" )]
        public LanguageMapList AlternateName { get; set; }


        [JsonProperty( PropertyName = "ceterms:audienceLevelType" )]
        public List<CredentialAlignmentObject> AudienceLevelType { get; set; }

        [JsonProperty( PropertyName = "ceterms:audienceType" )]
        public List<CredentialAlignmentObject> AudienceType { get; set; }

		[JsonProperty( "ceasn:author" )]
		public string author { get; set; }

		/// <summary>
		/// Category or classification of this resource.
		/// List of URIs that point to a concept
		/// </summary>
		[JsonProperty( "ceterms:classification" )]
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

        /// <summary>
        /// Only allow date (yyyy-mm-dd), no time
        /// xsd:date
        /// </summary>
        [JsonProperty( "ceterms:dateEffective" )]
        public string DateEffective{ get; set; }

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

        [JsonProperty( "ceasn:derivedFrom" )]
        public List<string> DerivedFrom { get; set; }

        /// <summary>
        /// Date beyond which the resource is no longer offered or available.
        /// Only allowing date (yyyy-mm-dd), no time. 
        /// xsd:date
        /// ceterms:expirationDate
        /// </summary>
        [JsonProperty( "ceterms:expirationDate" )]
        public string ExpirationDate { get; set; }
        
        //????these are URIs - could imply RubricCriterion is to be a top level class
        /// <summary>
        /// RubricCriterian referenced defines a principle or standard to be met that demonstrates quality in performance of a task or obtaining an objective.
        /// </summary>
        [JsonProperty( "ceasn:hasRubricCriterion" )]
		public List<string> HasRubricCriterion { get; set; }

		/// <summary>
		/// Has Criterion Category
		/// Resource referenced by the Rubric that defines categories for clustering logical sets of RubricCriterion.
		/// </summary>
		[JsonProperty( "asn:hasCriterionCategory" )]
		public List<string> HasCriterionCategory { get; set; }

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

        [JsonProperty( "ceasn:inLanguage" )]
		public List<string> InLanguage { get; set; }

        [JsonProperty( PropertyName = "ceterms:keyword" )]
        public LanguageMapList Keyword { get; set; }

        /// <summary>
        /// A legal document giving official permission to do something with this resource.
        /// </summary>
        [JsonProperty( "ceasn:license" )]
        public string License { get; set; }

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

        /// <summary>
        /// Original resource on which this resource is based or derived from.
        /// </summary>
        [JsonProperty( "ceterms:source" )]  //??? 
		public string Source { get; set; }  //URI

        [JsonProperty( PropertyName = "ceterms:subject" )]
        public List<CredentialAlignmentObject> Subject { get; set; }

        [JsonProperty( "ceterms:subjectWebpage" )]  //??? 
        public string SubjectWebpage { get; set; }  //URI

        #region Occupation, Industry, Program
        [JsonProperty( PropertyName = "ceterms:occupationType" )]
        public List<CredentialAlignmentObject> OccupationType { get; set; } = new List<CredentialAlignmentObject>();

        [JsonProperty( PropertyName = "ceterms:industryType" )]
        public List<CredentialAlignmentObject> IndustryType { get; set; } = new List<CredentialAlignmentObject>();

        [JsonProperty( PropertyName = "ceterms:naics" )]
        public List<string> Naics { get; set; } = new List<string>();

        [JsonProperty( PropertyName = "ceterms:instructionalProgramType" )]
        public List<CredentialAlignmentObject> InstructionalProgramType { get; set; } = new List<CredentialAlignmentObject>();
        #endregion


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
		public string Type { get; set; } = "asn:RubricCriterion";

		//[JsonProperty( "@id" )]
		//public string CtdlId { get; set; }  //resource

		/// <summary>
		/// An account of the resource.
		/// </summary>
		[JsonProperty( "dcterms:description" )]
		public LanguageMap Description { get; set; }

		[JsonProperty( "dcterms:Language" )]
		public List<string> Language { get; set; }

		/// <summary>
		/// Numeric value representing the resource's position in a list (array) of resources.
		/// </summary>
		[JsonProperty( "asn:sequence" )]
		public int Sequence { get; set; }

		/// <summary>
		/// A name given to the resource.
		/// </summary>
		[JsonProperty( "dcterms:title" )]
		public LanguageMap Title { get; set; }

		#endregion

		#region relationship properties

		/// <summary>
		/// Reference to the Rubric to which the RubricCriteria being described belongs.
		/// </summary>
		[JsonProperty( "asn:criterionFor" )]
		public List<string> CriterionFor { get; set; }

		/// <summary>
		/// Resource description of a level of performance based on a RubricCriterion.
		/// </summary>
		[JsonProperty( "asn:hasLevel" )]
		public List<string> HasLevel { get; set; }
		#endregion`


	}


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



	public class CriterionLevel
	{
		public CriterionLevel()
		{
		}

		#region base properties
		[JsonProperty( "@type" )]
		public string Type { get; set; } = "asn:CriterionLevel";

		//[JsonProperty( "@id" )]
		//public string CtdlId { get; set; }  //resource

		//[JsonProperty( "ceterms:ctid" )]
		//public string CTID { get; set; }

		/// <summary>
		/// Description of a level of achievement in performance of a task defined by the RubricCriterion.
		/// </summary>
		[JsonProperty( "asn:benchmark" )]
		public LanguageMap Benchmark { get; set; } //??

		[JsonProperty( "dcterms:description" )]
		public LanguageMap Description { get; set; }

		[JsonProperty( "dcterms:Language" )]
		public List<string> Language { get; set; }

		/// <summary>
		/// Qualitative description of the degree of achievement used for a column header in a tabular rubric.
		/// </summary>
		[JsonProperty( "asn:qualityLabel" )]
		public LanguageMap QualityLabel { get; set; }

		/// <summary>
		/// Points to be awarded for achieving this level for a RubricCriterion.
		/// </summary>
		[JsonProperty( "asn:score" )]
		public decimal? Score { get; set; }

		/// <summary>
		/// Numeric value representing the resource's position in a list (array) of resources.
		/// </summary>
		[JsonProperty( "asn:sequence" )]
		public int Sequence { get; set; }

		#endregion

		#region relationship properties

		/// <summary>
		/// Reference to the RubricCriterion to which the CriterionLevel being described belongs.
		/// </summary>
		[JsonProperty( "asn:levelFor" )]
		public List<string> LevelFor { get; set; }

		/// <summary>
		/// Reference to a progression model used.
		/// **** should this be single???????????
		/// </summary>
		[JsonProperty( "asn:hasProgressionLevel" )]
		public List<string> HasProgressionLevel { get; set; }

		#endregion
	}
}

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
	public class Rubric
	{
		public Rubric()
		{
		}

		[JsonProperty( "@type" )]
		public string Type { get; set; } = "asn:Rubric";

		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }  //resource

		[JsonProperty( "ceterms:ctid" )]
		public string CTID { get; set; }

		/// <summary>
		/// An account of the resource.
		/// </summary>
		[JsonProperty( "dcterms:description" )]
		public LanguageMap Description { get; set; }

		//????these are URIs - could imply RubricCriterion is to be a top level class
		/// <summary>
		/// RubricCriterian referenced defines a principle or standard to be met that demonstrates quality in performance of a task or obtaining an objective.
		/// </summary>
		[JsonProperty( "asn:hasCriterion" )]
		public List<string> HasCriterion { get; set; }

		/// <summary>
		/// Has Criterion Category
		/// Resource referenced by the Rubric that defines categories for clustering logical sets of RubricCriterion.
		/// </summary>
		[JsonProperty( "asn:hasCriterionCategory" )]
		public List<string> HasCriterionCategory { get; set; }

		/// <summary>
		/// Reference to a progression model used.
		/// </summary>
		[JsonProperty( "asn:hasProgressionModel" )]  //??? asn example has no namespace
		public string HasProgressionModel { get; set; }

		/// <summary>
		/// Description of what the rubric's creator intended to assess or evaluate.
		/// </summary>
		[JsonProperty( "asn:hasScope" )]
		public LanguageMap HasScope { get; set; }

		/// <summary>
		/// An unambiguous reference to the resource within a given context.
		/// Recommended practice is to identify the resource by means of a string conforming to an identification system. Examples include International Standard Book Number (ISBN), Digital Object Identifier (DOI), and Uniform Resource Name (URN). Persistent identifiers should be provided as HTTP URIs.
		/// </summary>
		[JsonProperty( "dcterms:identifier" )]  
		public string Identifier { get; set; }

		[JsonProperty( "dcterms:Language" )]
		public List<string> Language { get; set; }

		/// <summary>
		/// Original resource on which this resource is based or derived from.
		/// </summary>
		[JsonProperty( "dcterms:source" )]  //??? 
		public string Source { get; set; }  //URI

		/// <summary>
		/// A name given to the resource.
		/// </summary>
		[JsonProperty( "dcterms:title" )]
		public LanguageMap Title { get; set; }

	}

	public class RubricCriterion
	{
		public RubricCriterion()
		{
		}

		#region Base properties
		[JsonProperty( "@type" )]
		public string Type { get; set; } = "asn:RubricCriterion";

		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }  //resource

		[JsonProperty( "ceterms:ctid" )]
		public string CTID { get; set; }

		/// <summary>
		/// An account of the resource.
		/// </summary>
		[JsonProperty( "dcterms:description" )]
		public LanguageMap Description { get; set; }

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
		public string Type { get; set; } = "asn:RubricCriterion";

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
		public string Type { get; set; } = "asn:RubricCriterion";

		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }  //resource

		[JsonProperty( "ceterms:ctid" )]
		public string CTID { get; set; }

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
		public decimal Score { get; set; }

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

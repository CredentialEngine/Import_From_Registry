using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RA.Models.JsonV3
{
	public class CompetencyFrameworksGraph
	{
		[JsonIgnore]
		public static string classType = "ceasn:CompetencyFramework";
		public CompetencyFrameworksGraph()
		{
			Type = classType;
			Context = "http://credreg.net/ctdlasn/schema/context/json";
		}
		[JsonProperty( "@context" )]
		public string Context { get; set; }

		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }

		/// <summary>
		/// Main graph object
		/// </summary>
		[JsonProperty( "@graph" )]
		public object Graph { get; set; }
		//public object Graph { get; set;  }


		[JsonIgnore]
		[JsonProperty( "@type" )]
		public string Type { get; set; }

		[JsonIgnore]
		[JsonProperty( "ceterms:ctid" )]
		public string CTID { get; set; }

	}
	public class CompetencyFramework : JsonLDDocument
	{
		[JsonIgnore]
		public static string classType = "ceasn:CompetencyFramework";
		public CompetencyFramework()
		{
			Type = classType;
		}
		[JsonProperty( "@type" )]
		public string Type { get; set; }

		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }

		[JsonProperty( PropertyName = "ceterms:ctid" )]
		public string Ctid { get; set; }



		[JsonProperty( PropertyName = "ceasn:alignFrom" )]
		public List<string> alignFrom { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "ceasn:alignTo" )]
		public List<string> alignTo { get; set; } = new List<string>();

		[JsonProperty( "@author" )]
		public List<string> author { get; set; }


		[JsonProperty( PropertyName = "ceasn:conceptKeyword" )]
		public LanguageMapList conceptKeyword { get; set; }

		[JsonProperty( PropertyName = "ceasn:conceptTerm" )]
		public List<string> conceptTerm { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "ceasn:creator" )]
		public List<string> creator { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "ceasn:dateCopyrighted" )]
		public string dateCopyrighted { get; set; }

		[JsonProperty( PropertyName = "ceasn:dateCreated" )]
		public string dateCreated { get; set; }


		[JsonProperty( PropertyName = "ceasn:dateValidFrom" )]
		public string dateValidFrom { get; set; }

		[JsonProperty( PropertyName = "ceasn:dateValidUntil" )]
		public string dateValidUntil { get; set; }

		//single per https://github.com/CredentialEngine/CompetencyFrameworks/issues/66
		[JsonProperty( PropertyName = "ceasn:derivedFrom" )]
		public string derivedFrom { get; set; }

		//???language map??
		[JsonProperty( PropertyName = "ceasn:description" )]
		public LanguageMap description { get; set; } = new LanguageMap();

		[JsonProperty( PropertyName = "ceasn:educationLevelType" )]
		public List<string> educationLevelType { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "ceasn:hasTopChild" )]
		public List<string> hasTopChild { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "ceasn:identifier" )]
		public List<string> identifier { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "ceasn:inLanguage" )]
		public List<string> inLanguage { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "ceasn:license" )]
		public string license { get; set; }

		[JsonProperty( PropertyName = "ceasn:localSubject" )]
		public LanguageMapList localSubject { get; set; } = new LanguageMapList();


		[JsonProperty( PropertyName = "ceasn:name" )]
		public LanguageMap name { get; set; } = new LanguageMap();

		[JsonProperty( PropertyName = "ceasn:publicationStatusType" )]
		public string publicationStatusType { get; set; }// = new List<IdProperty>();

		[JsonProperty( PropertyName = "ceasn:publisher" )]
		public List<string> publisher { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "ceasn:publisherName" )]
		public LanguageMapList publisherName { get; set; } = new LanguageMapList();
		//

		[JsonProperty( PropertyName = "ceasn:repositoryDate" )]
		public string repositoryDate { get; set; }

		[JsonProperty( PropertyName = "ceasn:rights" )]
		//public string rights { get; set; }
		public List<string> rights { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "ceasn:rightsHolder" )]
		public string rightsHolder { get; set; }

		[JsonProperty( PropertyName = "ceasn:source" )]
		public List<string> source { get; set; } = new List<string>();

		//
		[JsonProperty( PropertyName = "ceasn:tableOfContents" )]
		public LanguageMap tableOfContents { get; set; } = new LanguageMap();

		[JsonProperty( PropertyName = "ceasn:competency" )]
		public List<Competency> competency { get; set; } = new List<Competency>();
	}

	public class CompetencyFrameworkInput
	{
		[JsonIgnore]
		public static string classType = "ceasn:CompetencyFramework";
		public CompetencyFrameworkInput()
		{
			Type = classType;
		}
		[JsonProperty( "@type" )]
		public string Type { get; set; }

		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }

		[JsonProperty( PropertyName = "ceterms:ctid" )]
		public string CTID { get; set; }



		[JsonProperty( PropertyName = "ceasn:alignFrom" )]
		public List<string> alignFrom { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "ceasn:alignTo" )]
		public List<string> alignTo { get; set; } = new List<string>();

		/// <summary>
		/// A person or organization chiefly responsible for the intellectual or artistic content of this competency framework or competency.
		/// </summary>
		[JsonProperty( "ceasn:author" )]
		public List<string> author { get; set; }


		/// <summary>
		/// A word or phrase used by the promulgating agency to refine and differentiate individual competencies contextually.
		/// </summary>
		[JsonProperty( PropertyName = "ceasn:conceptKeyword" )]
		//public object conceptKeyword { get; set; }
		public LanguageMapList conceptKeyword { get; set; }

		/// <summary>
		/// A term drawn from a controlled vocabulary used by the promulgating agency to refine and differentiate individual competencies contextually.
		/// </summary>
		[JsonProperty( PropertyName = "ceasn:conceptTerm" )]
		public List<string> conceptTerm { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "ceasn:creator" )]
		public List<string> creator { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "ceasn:dateCopyrighted" )]
		public string dateCopyrighted { get; set; }

		[JsonProperty( PropertyName = "ceasn:dateCreated" )]
		public string dateCreated { get; set; }


		[JsonProperty( PropertyName = "ceasn:dateValidFrom" )]
		public string dateValidFrom { get; set; }

		[JsonProperty( PropertyName = "ceasn:dateValidUntil" )]
		public string dateValidUntil { get; set; }

		[JsonProperty( PropertyName = "ceasn:derivedFrom" )]
		public string derivedFrom { get; set; }

		//???language map??
		[JsonProperty( PropertyName = "ceasn:description" )]
		public LanguageMap description { get; set; } = new LanguageMap();

		[JsonProperty( PropertyName = "ceasn:educationLevelType" )]
		public List<string> educationLevelType { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "ceasn:hasTopChild" )]
		public List<string> hasTopChild { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "ceasn:identifier" )]
		public List<string> identifier { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "ceasn:inLanguage" )]
		public List<string> inLanguage { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "ceasn:license" )]
		public string license { get; set; }

		[JsonProperty( PropertyName = "ceasn:localSubject" )]
		public LanguageMapList localSubject { get; set; } = new LanguageMapList();


		[JsonProperty( PropertyName = "ceasn:name" )]
		public LanguageMap name { get; set; } = new LanguageMap();

		[JsonProperty( PropertyName = "ceasn:publicationStatusType" )]
		public string publicationStatusType { get; set; }

		[JsonProperty( PropertyName = "ceasn:publisher" )]
		public List<string> publisher { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "ceasn:publisherName" )]
		//public object publisherName { get; set; }
		public LanguageMapList publisherName { get; set; } = new LanguageMapList();

		[JsonProperty( PropertyName = "ceasn:repositoryDate" )]
		public string repositoryDate { get; set; }

		[JsonProperty( PropertyName = "ceasn:rights" )]
		//public string rights { get; set; }
		public List<string> rights { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "ceasn:rightsHolder" )]
		public string rightsHolder { get; set; }

		[JsonProperty( PropertyName = "ceasn:source" )]
		public List<string> source { get; set; } = new List<string>();

		//
		[JsonProperty( PropertyName = "ceasn:tableOfContents" )]
		public LanguageMap tableOfContents { get; set; } = new LanguageMap();
	}
}

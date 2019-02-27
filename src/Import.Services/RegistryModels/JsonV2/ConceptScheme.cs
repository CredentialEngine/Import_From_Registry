using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
	public class ConceptSchemeGraph
	{
		[JsonIgnore]
		public static string classType = "skos:ConceptScheme";
		public ConceptSchemeGraph()
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

		[JsonIgnore]
		[JsonProperty( "@type" )]
		public string Type { get; set; }

		[JsonIgnore]
		[JsonProperty( "ceterms:ctid" )]
		public string CTID { get; set; }
	}
	public class ConceptScheme : JsonLDDocument
	{
		[JsonIgnore]
		public static string classType = "skos:CompetencyFramework";
		public ConceptScheme()
		{
			Type = classType;
		}
		[JsonProperty( "@type" )]
		public string Type { get; set; }

		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }

		[JsonProperty( PropertyName = "ceterms:ctid" )]
		public string CTID { get; set; }

		[JsonProperty( PropertyName = "dcterms:title" )]
		public object title { get; set; }
		//public LanguageMap title { get; set; } = new LanguageMap();

		[JsonProperty( PropertyName = "skos:description" )]
		public object description { get; set; } 
		//public LanguageMap description { get; set; } = new LanguageMap();

		[JsonProperty( PropertyName = "skos:creator" )]
		public List<string> creator { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "dcterms:language" )]
		public List<string> language { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "skos:publicationStatusType" )]
		public string publicationStatusType { get; set; }

		[JsonProperty( PropertyName = "skos:publisher" )]
		public List<string> publisher { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "schema:dateCreated" )]
		public string dateCreated { get; set; }

		[JsonProperty( PropertyName = "schema:dateModified" )]
		public string dateModified { get; set; }

		[JsonProperty( PropertyName = "skos:hasTopConcept" )]
		public List<string> hasTopConcept { get; set; } = new List<string>();
	}

	public class Concept : JsonLDDocument
	{

		[JsonIgnore]
		public static string classType = "Concept";
		public Concept()
		{
			Type = classType;
		}

		[JsonProperty( "@type" )]
		public string Type { get; set; }

		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }

		//[JsonProperty( PropertyName = "ceterms:ctid" )]
		//public string Ctid { get; set; }
		[JsonProperty( PropertyName = "skos:definition" )]
		public object definition { get; set; } 

		[JsonProperty( PropertyName = "skos:prefLabel" )]
		public object prefLabel { get; set; } 

		[JsonProperty( PropertyName = "skos:topConceptOf" )]
		public string topConceptOf { get; set; }

		[JsonProperty( PropertyName = "skos:inScheme" )]
		public string inScheme { get; set; }

		[JsonProperty( PropertyName = "schema:dateModified" )]
		public string dateModified { get; set; }
	}
}

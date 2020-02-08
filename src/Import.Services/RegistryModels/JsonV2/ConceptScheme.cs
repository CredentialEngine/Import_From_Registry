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
			Context = "https://credreg.net/ctdlasn/schema/context/json";
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
		public static string classType = "skos:ConceptScheme";
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


		[JsonProperty( PropertyName = "ceasn:altIdentifier" )]
		public List<string> altIdentifier { get; set; }

		[JsonProperty( PropertyName = "skos:changeNote" )]
		public LanguageMapList ChangeNote { get; set; }

		[JsonProperty( PropertyName = "ceasn:conceptKeyword" )]
		public LanguageMapList ConceptKeyword { get; set; }

		[JsonProperty( PropertyName = "ceasn:conceptTerm" )]
		public List<string> ConceptTerm { get; set; }

		[JsonProperty( PropertyName = "ceasn:creator" )]
		public List<string> Creator { get; set; }

		[JsonProperty( PropertyName = "ceasn:dateCopyrighted" )]
		public string DateCopyrighted{ get; set; }

		[JsonProperty( PropertyName = "ceasn:dateCreated" )]
		public string DateCreated { get; set; }

		[JsonProperty( PropertyName = "ceasn:dateModified" )]
		public string DateModified { get; set; }

		[JsonProperty( PropertyName = "ceasn:description" )]
		public LanguageMap Description { get; set; } = new LanguageMap();

		[JsonProperty( PropertyName = "skos:hasTopConcept" )]
		public List<string> HasTopConcept { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "skos:historyNote" )]
		public LanguageMap HistoryNote { get; set; }

		//[JsonProperty( PropertyName = "ceasn:inLanguage" )]
		//public string InLanguage { get; set; } 
		//public List<string> InLanguage { get; set; } = new List<string>();


		[JsonProperty( PropertyName = "ceasn:inLanguage" )]
		public List<string> InLanguage { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "ceasn:license" )]
		public string License { get; set; }

		[JsonProperty( PropertyName = "ceasn:name" )]
		public LanguageMap Name { get; set; } = new LanguageMap();

		[JsonProperty( PropertyName = "ceasn:publicationStatusType" )]
		public string PublicationStatusType { get; set; }

		[JsonProperty( PropertyName = "ceasn:publisher" )]
		public string Publisher { get; set; }

		[JsonProperty( PropertyName = "ceasn:publisherName" )]
		public LanguageMap PublisherName { get; set; }

		[JsonProperty( PropertyName = "ceasn:rights" )]
		public LanguageMap Rights { get; set; }

		[JsonProperty( PropertyName = "ceasn:rightsHolder" )]
		public string RightsHolder { get; set; }


		[JsonProperty( PropertyName = "ceasn:source" )]
		public string Source { get; set; }

	}

	/// <summary>
	/// Concept
	/// </summary>
	public class Concept : JsonLDDocument
	{

		[JsonIgnore]
		public static string classType = "skos:Concept";
		public Concept()
		{
			Type = classType;
		}

		[JsonProperty( "@type" )]
		public string Type { get; set; }

		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }

		[JsonProperty( PropertyName = "ceterms:ctid" )]
		public string CTID { get; set; }

		[JsonProperty( PropertyName = "skos:altLabel" )]
		public LanguageMapList AltLabel { get; set; }

		[JsonProperty( PropertyName = "skos:broader" )]
		public string Broader { get; set; }

		[JsonProperty( PropertyName = "skos:broadMatch" )]
		public List<string> BroadMatch { get; set; }

		[JsonProperty( PropertyName = "skos:changeNote" )]
		public LanguageMapList ChangeNote { get; set; }

		[JsonProperty( PropertyName = "skos:closeMatch" )]
		public List<string> CloseMatch { get; set; }

		//[JsonProperty( PropertyName = "skos:dateModified" )]
		//public string DateModified { get; set; }

		[JsonProperty( PropertyName = "skos:definition" )]
		public LanguageMap Definition { get; set; }

		[JsonProperty( PropertyName = "skos:exactMatch" )]
		public List<string> ExactMatch { get; set; }

		[JsonProperty( PropertyName = "skos:hiddenLabel" )]
		public LanguageMapList HiddenLabel { get; set; }

		[JsonProperty( PropertyName = "skos:inScheme" )]
		public string InScheme { get; set; }

		[JsonProperty( PropertyName = "ceasn:inLanguage" )]
		public List<string> InLanguage { get; set; }

		[JsonProperty( PropertyName = "skos:narrower" )]
		public List<string> Narrower { get; set; }

		[JsonProperty( PropertyName = "skos:narrowMatch" )]
		public List<string> NarrowMatch { get; set; }

		[JsonProperty( PropertyName = "skos:notation" )]
		public string Notation { get; set; }

		[JsonProperty( PropertyName = "skos:note" )]
		public LanguageMapList Note { get; set; }


		[JsonProperty( PropertyName = "skos:prefLabel" )]
		public LanguageMap PrefLabel { get; set; }

		[JsonProperty( PropertyName = "skos:related" )]
		public List<string> Related{ get; set; }

		[JsonProperty( PropertyName = "skos:relatedMatch" )]
		public List<string> RelatedMatch { get; set; }

		[JsonProperty( PropertyName = "meta:supersededBy" )]
		public string SupersededBy { get; set; }

		[JsonProperty( PropertyName = "skos:topConceptOf" )]
		public string TopConceptOf { get; set; }

		//TBD....................

		[JsonProperty( PropertyName = "navy:codeNEC" )]
		public string CodeNEC { get; set; }

		[JsonProperty( PropertyName = "navy:legacyCodeNEC" )]
		public string LegacyCodeNEC { get; set; }

		[JsonProperty( PropertyName = "navy:SourceCareerFieldCode" )]
		public List<string> SourceCareerFieldCode { get; set; } 

	}

	#region Plain graph
	public class ConceptSchemePlain : JsonLDDocument
	{
		[JsonIgnore]
		public static string classType = "skos:ConceptScheme";
		public ConceptSchemePlain()
		{
			Type = classType;
		}
		[JsonProperty( "@type" )]
		public string Type { get; set; }

		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }

		[JsonProperty( PropertyName = "ceterms:ctid" )]
		public string CTID { get; set; }


		[JsonProperty( PropertyName = "ceasn:altIdentifier" )]
		public List<string> altIdentifier { get; set; }

		[JsonProperty( PropertyName = "skos:changeNote" )]
		public List<string> ChangeNote { get; set; }

		[JsonProperty( PropertyName = "ceasn:conceptKeyword" )]
		public List<string> ConceptKeyword { get; set; }

		[JsonProperty( PropertyName = "ceasn:conceptTerm" )]
		public List<string> ConceptTerm { get; set; }

		[JsonProperty( PropertyName = "ceasn:creator" )]
		public List<string> Creator { get; set; }

		[JsonProperty( PropertyName = "ceasn:dateCopyrighted" )]
		public string DateCopyrighted { get; set; }

		[JsonProperty( PropertyName = "ceasn:dateCreated" )]
		public string DateCreated { get; set; }

		[JsonProperty( PropertyName = "ceasn:dateModified" )]
		public string DateModified { get; set; }

		[JsonProperty( PropertyName = "ceasn:description" )]
		public string Description { get; set; }

		[JsonProperty( PropertyName = "skos:hasTopConcept" )]
		public List<string> HasTopConcept { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "skos:historyNote" )]
		public string HistoryNote { get; set; }

		[JsonProperty( PropertyName = "ceasn:inLanguage" )]
		public List<string> InLanguage { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "ceasn:license" )]
		public string License { get; set; }

		[JsonProperty( PropertyName = "ceasn:name" )]
		public string Name { get; set; } 

		[JsonProperty( PropertyName = "ceasn:publicationStatusType" )]
		public string PublicationStatusType { get; set; }

		[JsonProperty( PropertyName = "ceasn:publisher" )]
		public string Publisher { get; set; }

		[JsonProperty( PropertyName = "ceasn:publisherName" )]
		public string PublisherName { get; set; }

		[JsonProperty( PropertyName = "ceasn:rights" )]
		public string Rights { get; set; }

		[JsonProperty( PropertyName = "ceasn:rightsHolder" )]
		public string RightsHolder { get; set; }


		[JsonProperty( PropertyName = "ceasn:source" )]
		public string Source { get; set; }

	}

	/// <summary>
	/// Concept
	/// </summary>
	public class ConceptPlain
	{

		[JsonIgnore]
		public static string classType = "skos:Concept";
		public ConceptPlain()
		{
			Type = classType;
		}

		[JsonProperty( "@type" )]
		public string Type { get; set; }

		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }

		[JsonProperty( PropertyName = "ceterms:ctid" )]
		public string CTID { get; set; }

		[JsonProperty( PropertyName = "skos:altLabel" )]
		public object AltLabel { get; set; }

		[JsonProperty( PropertyName = "skos:broader" )]
		public string Broader { get; set; }

		[JsonProperty( PropertyName = "skos:broadMatch" )]
		public List<string> BroadMatch { get; set; }

		[JsonProperty( PropertyName = "skos:changeNote" )]
		public List<string> ChangeNote { get; set; }

		[JsonProperty( PropertyName = "skos:closeMatch" )]
		public List<string> CloseMatch { get; set; }

		//[JsonProperty( PropertyName = "skos:dateModified" )]
		//public string DateModified { get; set; }

		[JsonProperty( PropertyName = "skos:definition" )]
		public string Definition { get; set; }

		[JsonProperty( PropertyName = "skos:exactMatch" )]
		public List<string> ExactMatch { get; set; }

		[JsonProperty( PropertyName = "skos:hiddenLabel" )]
		public List<string> HiddenLabel { get; set; }

		[JsonProperty( PropertyName = "skos:inScheme" )]
		public List<string> InScheme { get; set; }

		[JsonProperty( PropertyName = "skos:inSchemeList" )]
		public List<string> InSchemeList { get; set; }

		[JsonProperty( PropertyName = "ceasn:inLanguage" )]
		public List<string> InLanguage { get; set; }

		[JsonProperty( PropertyName = "skos:narrower" )]
		public List<string> Narrower { get; set; }

		[JsonProperty( PropertyName = "skos:narrowMatch" )]
		public List<string> NarrowMatch { get; set; }

		[JsonProperty( PropertyName = "skos:notation" )]
		public string Notation { get; set; }

		[JsonProperty( PropertyName = "skos:note" )]
		public List<string> Note { get; set; }


		[JsonProperty( PropertyName = "skos:prefLabel" )]
		public string PrefLabel { get; set; }

		[JsonProperty( PropertyName = "skos:related" )]
		public List<string> Related { get; set; }

		[JsonProperty( PropertyName = "skos:relatedMatch" )]
		public List<string> RelatedMatch { get; set; }

		[JsonProperty( PropertyName = "meta:supersededBy" )]
		public string SupersededBy { get; set; }

		[JsonProperty( PropertyName = "skos:topConceptOfTest" )]
		public object TopConceptOfTest { get; set; }

		[JsonProperty( PropertyName = "skos:topConceptOfTest2" )]
		public object TopConceptOfTest2 { get; set; }

		[JsonProperty( PropertyName = "skos:topConceptOf2" )]
		public string TopConceptOf2 { get; set; }

		[JsonProperty( PropertyName = "skos:topConceptOf" )]
		public List<string> TopConceptOf { get; set; }

		//TBD....................

		//[JsonProperty( PropertyName = "navy:codeNEC" )]
		//public string CodeNEC { get; set; }

		//[JsonProperty( PropertyName = "navy:legacyCodeNEC" )]
		//public string LegacyCodeNEC { get; set; }

		//[JsonProperty( PropertyName = "navy:SourceCareerFieldCode" )]
		//public List<string> SourceCareerFieldCode { get; set; }

	}
	#endregion
}

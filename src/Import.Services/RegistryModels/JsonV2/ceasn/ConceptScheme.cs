using System;
using System.Collections.Generic;
using Newtonsoft.Json;

using RA.Models.Input;

namespace RA.Models.JsonV2
{
	public class ConceptSchemeGraph
	{
		//[JsonIgnore]
		//public static string classType = "skos:ConceptScheme";
		public ConceptSchemeGraph()
		{
			//Type = classType;
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
		public string Type { get; set; } = "skos:ConceptScheme";

		[JsonIgnore]
		[JsonProperty( "ceterms:ctid" )]
		public string CTID { get; set; }
	}
	public class ConceptScheme //: JsonLDDocument
	{
	
		/// <summary>
		/// constructor
		/// </summary>
		public ConceptScheme()
		{
		}
		/// <summary>
		/// Type
		/// </summary>
		[JsonProperty( "@type" )]
		public string Type { get; set; } = "skos:ConceptScheme";

		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }

		[JsonProperty( PropertyName = "ceterms:ctid" )]
		public string CTID { get; set; }

		//20-08-05 not sure was ever valid?
		//[JsonProperty( PropertyName = "ceasn:altIdentifier" )]
		//public List<string> AltIdentifier { get; set; }

		//
		//
		/// <summary>
		/// Text describing a significant change to the concept.
		/// 20-08-05 no longer on credReg.net
		/// 22-03-30 apparantly back
		/// </summary>
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
		public LanguageMap Description { get; set; } 

		[JsonProperty( PropertyName = "skos:hasTopConcept" )]
		public List<string> HasTopConcept { get; set; }

		///// <summary>
		///// obsolete
		///// </summary>
  //      [JsonProperty( PropertyName = "skos:historyNote" )]
  //      public LanguageMap HistoryNote { get; set; }

        [JsonProperty( PropertyName = "ceasn:inLanguage" )]
		public List<string> InLanguage { get; set; } 

		[JsonProperty( PropertyName = "ceasn:license" )]
		public string License { get; set; }

		[JsonProperty( PropertyName = "ceasn:name" )]
		public LanguageMap Name { get; set; } 

		[JsonProperty( PropertyName = "ceasn:publicationStatusType" )]
		public string PublicationStatusType { get; set; }

		/// <summary>
		/// This defined as an object to handle old data from CaSS.
		/// The Finder import and link checker can define this as a list.
		/// </summary>
		[JsonProperty( PropertyName = "ceasn:publisher" )]
		public object Publisher { get; set; }
		//public List<string> Publisher { get; set; }

		[JsonProperty( PropertyName = "ceasn:publisherName" )]
		public object PublisherName { get; set; }
		//public LanguageMap PublisherName { get; set; }

		[JsonProperty( PropertyName = "ceasn:rights" )]
		public LanguageMap Rights { get; set; }

		[JsonProperty( PropertyName = "ceasn:rightsHolder" )]
		public List<string> RightsHolder { get; set; }


		[JsonProperty( PropertyName = "ceasn:source" )]
        //public object Source { get; set; }
        public List<string> Source { get; set; }

        [JsonProperty( PropertyName = "ceterms:supersededBy" )]
        public string SupersededBy { get; set; } //URL
    }

	/// <summary>
	/// Concept
	/// </summary>
	public class Concept //: JsonLDDocument
	{
		/// <summary>
		/// Constructor
		/// </summary>
		public Concept()
		{
			//Type = classType;
		}

		[JsonProperty( "@type" )]
		public string Type { get; set; } = "skos:Concept";

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
		public object InScheme { get; set; }
		//public string InScheme { get; set; }

		//[JsonProperty( PropertyName = "skos:inSchemeList" )]
		//public List<string> InSchemeList { get; set; }

		//[JsonProperty( PropertyName = "ceasn:inLanguage" )]
		//public List<string> InLanguage { get; set; }

		[JsonProperty( PropertyName = "skos:narrower" )]
		public List<string> Narrower { get; set; }

		[JsonProperty( PropertyName = "skos:narrowMatch" )]
		public List<string> NarrowMatch { get; set; }

		/// <summary>
		/// Alphanumeric notation or ID code as defined by the promulgating body to identify this resource.
		/// </summary>
		[JsonProperty( PropertyName = "skos:notation" )]
		public string Notation { get; set; }

		/// <summary>
		///  Annotations to the concept for purposes of general documentation.
		/// </summary>
		[JsonProperty( PropertyName = "skos:note" )]
		public LanguageMapList Note { get; set; }

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
		///  Preferred language-tagged label representing this concept.
		/// </summary>
		[JsonProperty( PropertyName = "skos:prefLabel" )]
		public LanguageMap PrefLabel { get; set; }

		[JsonProperty( PropertyName = "skos:related" )]
		public List<string> Related{ get; set; }

		//[JsonProperty( PropertyName = "skos:relatedMatch" )]
		//public List<string> RelatedMatch { get; set; }

		[JsonProperty( PropertyName = "ceterms:subjectWebpage" )]
		public string SubjectWebpage { get; set; }

		[JsonProperty( PropertyName = "meta:supersededBy" )]
		public string SupersededBy { get; set; }

		[JsonProperty( PropertyName = "skos:topConceptOf" )]
		public string TopConceptOf { get; set; }

		//TBD....................
		//Navy
		//[JsonProperty( PropertyName = "navy:codeNEC" )]
		//public string CodeNEC { get; set; }

		//[JsonProperty( PropertyName = "navy:legacyCodeNEC" )]
		//public string LegacyCodeNEC { get; set; }

		//[JsonProperty( PropertyName = "navy:SourceCareerFieldCode" )]
		//public List<string> SourceCareerFieldCode { get; set; } 

	}

	#region Plain graph
	public class ConceptSchemePlain //: JsonLDDocument
	{
		//[JsonIgnore]
		//public static string classType = "skos:ConceptScheme";
		public ConceptSchemePlain()
		{
			//Type = classType;
		}
		[JsonProperty( "@type" )]
		public string Type { get; set; } = "skos:ConceptScheme";

		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }

		[JsonProperty( PropertyName = "ceterms:ctid" )]
		public string CTID { get; set; }


		//[JsonProperty( PropertyName = "ceasn:altIdentifier" )]
		//public List<string> altIdentifier { get; set; }

		//[JsonProperty( PropertyName = "skos:changeNote" )]
		//public List<string> ChangeNote { get; set; }

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

		//[JsonProperty( PropertyName = "skos:historyNote" )]
		//public string HistoryNote { get; set; }

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
		public List<string> PublisherName { get; set; }

		[JsonProperty( PropertyName = "ceasn:rights" )]
		public string Rights { get; set; }

		/// <summary>
		/// In this concept, the content is a URL for an organization in the registry.
		/// Not sure if needed, could switch to organizationReference?
		/// Actually this context is a formed, but plain graph with URIs.
		/// </summary>
		[JsonProperty( PropertyName = "ceasn:rightsHolder" )]
		public List<string> RightsHolder { get; set; }
        //public string RightsHolder { get; set; } 


        [JsonProperty( PropertyName = "ceasn:source" )]
		public List<string> Source { get; set; }

	}

	/// <summary>
	/// Concept
	/// </summary>
	public class ConceptPlain : Concept
	{

		[JsonIgnore]
		public static string classType = "skos:Concept";
		public ConceptPlain()
		{
			Type = classType;
		}

		//[JsonProperty( "@type" )]
		//public string Type { get; set; }

		//[JsonProperty( "@id" )]
		//public string CtdlId { get; set; }

		//[JsonProperty( PropertyName = "ceterms:ctid" )]
		//public string CTID { get; set; }

		[JsonProperty( PropertyName = "skos:altLabel" )]
		public new object AltLabel { get; set; }

		//[JsonProperty( PropertyName = "skos:broader" )]
		//public string Broader { get; set; }

		//[JsonProperty( PropertyName = "skos:broadMatch" )]
		//public List<string> BroadMatch { get; set; }

		[JsonProperty( PropertyName = "skos:changeNote" )]
		public new List<string> ChangeNote { get; set; }

		[JsonProperty( PropertyName = "skos:closeMatch" )]
		public new List<string> CloseMatch { get; set; }

		//[JsonProperty( PropertyName = "skos:dateModified" )]
		//public string DateModified { get; set; }

		[JsonProperty( PropertyName = "skos:definition" )]
		public new string Definition { get; set; }

		//[JsonProperty( PropertyName = "skos:exactMatch" )]
		//public List<string> ExactMatch { get; set; }

		[JsonProperty( PropertyName = "skos:hiddenLabel" )]
		public new List<string> HiddenLabel { get; set; }

		//[JsonProperty( PropertyName = "skos:inScheme" )]
		//public List<string> InScheme { get; set; }

		//[JsonProperty( PropertyName = "skos:inSchemeList" )]
		//public List<string> InSchemeList { get; set; }

		//[JsonProperty( PropertyName = "ceasn:inLanguage" )]
		//public List<string> InLanguage { get; set; }

		//[JsonProperty( PropertyName = "skos:narrower" )]
		//public List<string> Narrower { get; set; }

		//[JsonProperty( PropertyName = "skos:narrowMatch" )]
		//public List<string> NarrowMatch { get; set; }

		//[JsonProperty( PropertyName = "skos:notation" )]
		//public string Notation { get; set; }

		[JsonProperty( PropertyName = "skos:note" )]
		public new List<string> Note { get; set; }


		[JsonProperty( PropertyName = "skos:prefLabel" )]
		public new string PrefLabel { get; set; }

		//[JsonProperty( PropertyName = "skos:related" )]
		//public List<string> Related { get; set; }

		//[JsonProperty( PropertyName = "skos:relatedMatch" )]
		//public List<string> RelatedMatch { get; set; }

		//[JsonProperty( PropertyName = "meta:supersededBy" )]
		//public string SupersededBy { get; set; }

		[JsonProperty( PropertyName = "skos:topConceptOf" )]
		public new object TopConceptOf { get; set; }


	}
	#endregion
}

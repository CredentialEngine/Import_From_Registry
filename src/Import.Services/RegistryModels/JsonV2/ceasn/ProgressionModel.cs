using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
    /// <summary>
    /// ProgressionModel
	/// 22-12-15 Changed to not inherit from ConceptScheme
    /// </summary>
    public class ProgressionModel 
    {
        public ProgressionModel()
        {
        }
		/// <summary>
		/// Type
		/// </summary>
		[JsonProperty( "@type" )]
		public string Type { get; set; } = "asn:ProgressionModel";

		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }

		[JsonProperty( PropertyName = "ceterms:ctid" )]
		public string CTID { get; set; }
		//

		[JsonProperty( PropertyName = "ceterms:alternateName" )]
		public LanguageMapList AlternateName { get; set; }
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
		public string DateCopyrighted { get; set; }

		[JsonProperty( PropertyName = "ceasn:dateCreated" )]
		public string DateCreated { get; set; }

		[JsonProperty( PropertyName = "ceasn:dateModified" )]
		public string DateModified { get; set; }

		[JsonProperty( PropertyName = "ceasn:description" )]
		public LanguageMap Description { get; set; }

		[JsonProperty( PropertyName = "skos:hasTopConcept" )]
		public List<string> HasTopConcept { get; set; }

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
		public string RightsHolder { get; set; }


		[JsonProperty( PropertyName = "ceasn:source" )]
		public object Source { get; set; }

	}

    /// <summary>
    /// ProgressionLevel
    /// </summary>
    public class ProgressionLevel 
    {
        /// <summary>
        /// constructor
        /// </summary>
        public ProgressionLevel()
        {
            Type = "asn:ProgressionLevel";
        }

        //the following are STILL used
        //TopConceptOf



		[JsonProperty( "@type" )]
		public string Type { get; set; } = "asn:ProgressionLevel";

		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }

		[JsonProperty( PropertyName = "ceterms:ctid" )]
		public string CTID { get; set; }

		//[JsonProperty( PropertyName = "skos:altLabel" )]
		//public LanguageMapList AltLabel { get; set; }

		[JsonProperty( PropertyName = "skos:broader" )]
		public string Broader { get; set; }

		//[JsonProperty( PropertyName = "skos:broadMatch" )]
		//public List<string> BroadMatch { get; set; }

		//[JsonProperty( PropertyName = "skos:changeNote" )]
		//public LanguageMapList ChangeNote { get; set; }

		//[JsonProperty( PropertyName = "skos:closeMatch" )]
		//public List<string> CloseMatch { get; set; }

		//[JsonProperty( PropertyName = "skos:dateModified" )]
		//public string DateModified { get; set; }

		[JsonProperty( PropertyName = "skos:definition" )]
		public LanguageMap Definition { get; set; }

		//[JsonProperty( PropertyName = "skos:exactMatch" )]
		//public List<string> ExactMatch { get; set; }

		//[JsonProperty( PropertyName = "skos:hiddenLabel" )]
		//public LanguageMapList HiddenLabel { get; set; }

		/// <summary>
		/// Progression Model to which this Progression Level belongs.
		/// </summary>
		[JsonProperty( "ceasn:inProgressionModel" )]
		public string InProgressionModel { get; set; }

		[JsonProperty( PropertyName = "skos:narrower" )]
		public List<string> Narrower { get; set; }

		//[JsonProperty( PropertyName = "skos:narrowMatch" )]
		//public List<string> NarrowMatch { get; set; }

		[JsonProperty( PropertyName = "skos:notation" )]
		public string Notation { get; set; }

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

		[JsonProperty( PropertyName = "skos:prefLabel" )]
		public LanguageMap PrefLabel { get; set; }

		//[JsonProperty( PropertyName = "skos:related" )]
		//public List<string> Related { get; set; }


		//[JsonProperty( PropertyName = "meta:supersededBy" )]
		//public string SupersededBy { get; set; }

		[JsonProperty( PropertyName = "skos:topConceptOf" )]
		public string TopConceptOf { get; set; }

	}

	public class ProgressionModelPlain 
	{
		//[JsonIgnore]
		//public static string classType = "skos:ConceptScheme";
		public ProgressionModelPlain()
		{
			//Type = classType;
		}
		[JsonProperty( "@type" )]
		public string Type { get; set; } = "asn:ProgressionModel";

		[JsonProperty( PropertyName = "ceterms:ctid" )]
		public string CTID { get; set; }

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
		public string RightsHolder { get; set; }

		[JsonProperty( PropertyName = "ceasn:source" )]
		public string Source { get; set; }

	}

	/// <summary>
	/// Progression Level
	/// </summary>
	public class ProgressionLevelPlain : ProgressionLevel
	{

		[JsonIgnore]
		public static string classType = "asn:ProgressionLevel";
		public ProgressionLevelPlain()
		{
			Type = classType;
		}


		[JsonProperty( PropertyName = "skos:definition" )]
		public new string Definition { get; set; }

		[JsonProperty( PropertyName = "skos:note" )]
		public new List<string> Note { get; set; }

		[JsonProperty( PropertyName = "skos:prefLabel" )]
		public new string PrefLabel { get; set; }

		[JsonProperty( PropertyName = "skos:topConceptOf" )]
		public new object TopConceptOf { get; set; }


	}
}

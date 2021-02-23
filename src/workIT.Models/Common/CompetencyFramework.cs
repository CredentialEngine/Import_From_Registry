using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace workIT.Models.Common
{
    public class CompetencyFrameworksGraph
    {
        public CompetencyFrameworksGraph()
        {
        }
        public string Context { get; set; }


        public string CtdlId { get; set; }

        /// <summary>
        /// Main graph object
        /// </summary>
        public object Graph { get; set; }

        public string Type { get; set; }

        public string CTID { get; set; }
    }

	/// <summary>
	/// CompetencyFramework used by the graph search
	/// ****21-02-22 THIS IS NOT USED BY IMPORT **************
	/// </summary>
	public class CompetencyFramework : BaseObject
    {

        public CompetencyFramework()
        {
        }
        //won't be entered, only one type
        //public string Type { get; set; }

        //public int Id { get; set; }
        //public Guid RowId { get; set; }
        //required
        public string CtdlId { get; set; }

        //required
        public string CTID { get; set; }

		public int TotalCompetencies { get; set; }
        public List<string> alignFrom { get; set; } = new List<string>();

        public List<string> alignTo { get; set; } = new List<string>();

        public List<string> author { get; set; } = new List<string>();


        public List<LanguageItem> conceptKeyword { get; set; } = new List<LanguageItem>();

        public List<string> conceptTerm { get; set; } = new List<string>();

        public List<string> creator { get; set; } = new List<string>();

        public string dateCopyrighted { get; set; }

        public string dateCreated { get; set; }


        public string dateValidFrom { get; set; }

        public string dateValidUntil { get; set; }

        public List<string> derivedFrom { get; set; } = new List<string>();

        //???language map??
        public LanguageItem description { get; set; } = new LanguageItem();

        public List<string> educationLevelType { get; set; } = new List<string>();

        public List<string> hasTopChild { get; set; } = new List<string>();

        public List<string> identifier { get; set; } = new List<string>();

        public List<string> inLanguage { get; set; } = new List<string>();

        public string license { get; set; }

        public List<LanguageItem> localSubject { get; set; } = new List<LanguageItem>();

        public LanguageItem Name { get; set; } = new LanguageItem();

        public List<string> publicationStatusType { get; set; } = new List<string>();

        public List<string> publisher { get; set; } = new List<string>();

        public List<LanguageItem> publisherName { get; set; } = new List<LanguageItem>();
        //

        public string repositoryDate { get; set; }

        public string rights { get; set; }

        public string rightsHolder { get; set; }

        public List<string> source { get; set; } = new List<string>();

        //
        public List<LanguageItem> tableOfContents { get; set; } = new List<LanguageItem>();


    }

    public class Competency
    {
        //required": [ "@type", "@id", "ceasn:competencyText", "ceasn:inLanguage", "ceasn:isPartOf", "ceterms:ctid" ]

        public Competency()
        {
        }

        public string Type { get; set; }

        public string CtdlId { get; set; }

        public string Ctid { get; set; }

        public List<string> alignFrom { get; set; } = new List<string>();

        public List<string> alignTo { get; set; } = new List<string>();

        public List<string> altCodedNotation { get; set; } = new List<string>();

        public List<string> author { get; set; } = new List<string>();

        public List<string> broadAlignment { get; set; } = new List<string>();

        public string codedNotation { get; set; }


        public LanguageMap comment { get; set; }


        public List<LanguageMap> competencyCategory { get; set; }



        public LanguageMap competencyText { get; set; }

        ///ProficiencyScale??
        public List<string> complexityLevel { get; set; } = new List<string>();

        public List<string> comprisedOf { get; set; } = new List<string>();

        public List<LanguageMap> conceptKeyword { get; set; }

        public List<string> conceptTerm { get; set; } = new List<string>();



        public List<string> creator { get; set; } = new List<string>();

        public List<string> crossSubjectReference { get; set; } = new List<string>();

        public string dateCreated { get; set; }

        public List<string> derivedFrom { get; set; } = new List<string>();

        public List<string> educationLevelType { get; set; } = new List<string>();

        public List<string> exactAlignment { get; set; } = new List<string>();


        public List<Competency> hasChild { get; set; } = new List<Competency>();

        public List<string> identifier { get; set; } = new List<string>();

        public List<string> inLanguage { get; set; } = new List<string>();

        public List<string> isChildOf { get; set; } = new List<string>();

        public List<string> isPartOf { get; set; } = new List<string>();

        public string isVersionOf { get; set; }

        public string listID { get; set; }

        public List<LanguageMap> localSubject { get; set; }

        public List<string> majorAlignment { get; set; } = new List<string>();

        public List<string> minorAlignment { get; set; } = new List<string>();

        public List<string> narrowAlignment { get; set; } = new List<string>();

        public List<string> prerequisiteAlignment { get; set; } = new List<string>();

        public List<string> skillEmbodied { get; set; } = new List<string>();


        public string weight { get; set; }

    }
    //

    public class CTDLAPICompetencyFrameworkResultSet
    {
        public List<CTDLAPICompetencyFrameworkResult> Results { get; set; }
        public JArray RelatedItems { get; set; }
        public JArray RelatedItemsMap { get; set; }
        public int TotalResults { get; set; }
        public List<CTDLAPICompetencyFrameworkRelatedItemSetForSearchResult> PerResultRelatedItems { get; set; }
        public JObject Debug { get; set; }
    }
    public class CTDLAPICompetencyFrameworkResultSetRelatedItemsMapItem
    {
        public CTDLAPICompetencyFrameworkResultSetRelatedItemsMapItem()
        {
			RelatedItems = new List<CTDLAPICompetencyFrameworkResultSetRelatedItemsMapItemListItem>();
        }

        public string ResourceURI { get; set; }
        public JObject DebugInfo { get; set; }
        public List<CTDLAPICompetencyFrameworkResultSetRelatedItemsMapItemListItem> RelatedItems { get; set; }

        public CTDLAPICompetencyFrameworkResultSetRelatedItemsMapItemListItem GetRelatedItemsForPath_ExactMatch( string path )
        {
            return RelatedItems.FirstOrDefault( m => m.Path.ToLower() == path.ToLower() ) ?? new CTDLAPICompetencyFrameworkResultSetRelatedItemsMapItemListItem();
        }
        public List<CTDLAPICompetencyFrameworkResultSetRelatedItemsMapItemListItem> GetRelatedItemsForPath_PartialMatch( string path )
        {
            return RelatedItems.Where( m => m.Path.ToLower().Contains( path.ToLower() ) ).ToList();
        }
        public List<CTDLAPICompetencyFrameworkResultSetRelatedItemsMapItemListItem> GetRelatedItemsForPath_StartsWithMatch( string path )
        {
            return RelatedItems.Where( m => m.Path.ToLower().IndexOf( path.ToLower() ) == 0 ).ToList();
        }
        public List<CTDLAPICompetencyFrameworkResultSetRelatedItemsMapItemListItem> GetRelatedItemsForPath_EndsWithMatch( string path )
        {
            return RelatedItems.Where( m => System.Text.RegularExpressions.Regex.IsMatch( m.Path, path + "$", System.Text.RegularExpressions.RegexOptions.IgnoreCase ) ).ToList();
        }
        public List<CTDLAPICompetencyFrameworkResultSetRelatedItemsMapItemListItem> GetRelatedItemsForPath_RegexMatch( string pathRegex )
        {
            return RelatedItems.Where( m => System.Text.RegularExpressions.Regex.IsMatch( m.Path, pathRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase ) ).ToList();
        }
    }
    public class CTDLAPICompetencyFrameworkResultSetRelatedItemsMapItemListItem
    {
        public CTDLAPICompetencyFrameworkResultSetRelatedItemsMapItemListItem()
        {
            URIs = new List<string>();
        }
        public string Path { get; set; }
        public List<string> URIs { get; set; }
		public int TotalURIs { get; set; }
    }
    //

    public class CTDLAPICompetencyFrameworkResult
    {
        [JsonProperty(PropertyName = "@id")]
        public string _ID { get; set; }
        [JsonProperty(PropertyName = "@type")]
        public string _Type { get; set; }
        [JsonProperty(PropertyName = "ceasn:name")]
        public LanguageMap Name { get; set; }
        [JsonProperty(PropertyName = "ceasn:description")]
        public LanguageMap Description { get; set; }
        [JsonProperty(PropertyName = "ceterms:ctid")]
        public string CTID { get; set; }
        [JsonProperty(PropertyName = "ceasn:creator")]
        public List<string> Creator { get; set; }
        [JsonProperty(PropertyName = "ceasn:dateModified")]
        public DateTime DateModified { get; set; }
        [JsonProperty(PropertyName = "ceasn:dateCreated")]
        public DateTime DateCreated { get; set; }
        public string RawData { get; set; }
    }
    //

    public class CTDLAPICompetencyFrameworkRelatedItemSetForSearchResult
    {
        public CTDLAPICompetencyFrameworkRelatedItemSetForSearchResult()
        {
            foreach (var property in this.GetType().GetProperties().Where(m => m.PropertyType == typeof(CTDLAPIRelatedItemForSearchResult)).ToList())
            {
                property.SetValue(this, new CTDLAPIRelatedItemForSearchResult());
            }
            Triples = new List<JObject>();
        }

        public string Logging { get; set; }
        public string RelatedItemsForCTID { get; set; }
        public CTDLAPIRelatedItemForSearchResult Publishers { get; set; }
        public CTDLAPIRelatedItemForSearchResult Creators { get; set; }
        public CTDLAPIRelatedItemForSearchResult Owners { get; set; }
        public CTDLAPIRelatedItemForSearchResult Competencies { get; set; }
        public CTDLAPIRelatedItemForSearchResult Credentials { get; set; }
        public CTDLAPIRelatedItemForSearchResult Assessments { get; set; }
        public CTDLAPIRelatedItemForSearchResult LearningOpportunities { get; set; }
        public CTDLAPIRelatedItemForSearchResult ConceptSchemes { get; set; }
        public CTDLAPIRelatedItemForSearchResult Concepts { get; set; }
        public CTDLAPIRelatedItemForSearchResult AlignedFrameworks { get; set; }
        public CTDLAPIRelatedItemForSearchResult AlignedCompetencies { get; set; }

        public List<JObject> Triples { get; set; }
    }
    //

    public class CTDLAPIRelatedItemForSearchResult
    {
        public CTDLAPIRelatedItemForSearchResult()
        {
            Samples = new List<JObject>();
        }
        public CTDLAPIRelatedItemForSearchResult(string name, string label, List<JObject> allSamples, int sampleSize = 10)
        {
            Name = name;
            TotalItems = allSamples.Count();
            Label = label.Replace("#", TotalItems.ToString());
            Samples = allSamples.Take(sampleSize).ToList();
        }

        public string Name { get; set; }
        public string Label { get; set; }
        public int TotalItems { get; set; }
        public List<JObject> Samples { get; set; }
    }
    //
}

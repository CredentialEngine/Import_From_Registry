using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public class CompetencyFramework
    {

        public CompetencyFramework()
        {
        }
        //won't be entered, only one type
        //public string Type { get; set; }

        //required
        public string CtdlId { get; set; }

        //required
        public string Ctid { get; set; }

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

        public LanguageItem name { get; set; } = new LanguageItem();

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

        public List<Competency> Competencies { get; set; } = new List<Competency>();
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
    /// <summary>
    /// Language Map is an object holds multiple 
    /// </summary>
    public class LanguageMap
    {
        public string English { get; set; }
        public string English_US { get; set; }

        public string English_GB { get; set; }

        public string Spanish { get; set; }
        public string Polish { get; set; }
        public string German { get; set; }
        public string Russian { get; set; }
    }
    public class LanguageItem
    {
        public string Language { get; set; }
        public string Text { get; set; }
    }

}

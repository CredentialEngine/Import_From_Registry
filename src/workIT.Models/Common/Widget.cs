using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Common
{
    [Serializable]
    public class Widget : BaseObject
    {
        public Widget()
        {
            OwningOrganizationIdsList = new List<int>();
        }
        //  public int Id { get; set; }

        public string OrgCTID { get; set; }
        public string OrganizationName { get; set; }

        public string Name { get; set; }

        public string WidgetAlias { get; set; }
        public string CustomURL { get; set; }

        /// <summary>
        /// JSON version of WidgetFilters class serialized to DB
        /// </summary>
        public string SearchFilters { get; set; }
        public WidgetFilters WidgetFilters { get; set; } = new WidgetFilters();
        
        #region Style related 
        public string WidgetStylesUrl { get; set; }
        public string LogoUrl { get; set; }
        public string CustomStylesFileName { get; set; }
        public string CustomStylesURL { get; set; }
        public WidgetStyles WidgetStyles { get; set; } = new WidgetStyles();

        /// <summary>
        /// JSON version of WidgetStyles to store in database
        /// </summary>
        public string CustomStyles { get; set; }
        #endregion

        #region filters
        public bool IncludeIfAvailableOnline { get; set; }

        public List<int> OwningOrganizationIdsList { get; set; }
        public string OwningOrganizationIds { get; set; }
        public string CountryFilters { get; set; }
        public string CityFilters { get; set; }
        public string RegionFilters { get; set; }
        public List<string> CountriesList { get; set; }

        public List<string> CitiesList { get; set; }
        public List<string> RegionsList { get; set; }
        #endregion

        //will probably need a separate table for all the options, although could be a JSON blob
        //OR just use styles
    }
    [Serializable]
    public class WidgetFilters
    {

        //temp for prototyping
        public bool HideCredentialTypesFilter { get; set; }
        public CredentialFilters CredFilters { get; set; } = new CredentialFilters();
        public OrganizationFilters OrganizationFilters { get; set; } = new OrganizationFilters();
        public AssessmentFilters AssessmentFilters { get; set; } = new AssessmentFilters();
        public LearningOpportunityFilters LoppFilters { get; set; } = new LearningOpportunityFilters();

        public SearchFilters SearchFilters { get; set; } = new SearchFilters();
    }
    [Serializable]
    public class WidgetStyles
    {
        readonly string MainSiteHeaderBackground_Default = "#FFFFFF";
        readonly string MainSiteLogo_Default = "/Images/Common/logo_stacked.png";
        readonly string SearchButton1_Default = "#0F3E63";
        readonly string FilterButton1_Default = "#3b7741";
        readonly string ResetButton1_Default = "#B55130";

        public WidgetStyles()
        {
            this.MainSiteHeader = MainSiteHeaderBackground_Default;
            this.MainSiteLogo = MainSiteLogo_Default;
            this.SearchButton1 = SearchButton1_Default;
            this.FilterButton1 = FilterButton1_Default;
            this.ResetButton1 = ResetButton1_Default;
        }

    
        /// <summary>
        /// MainSiteBackgroundColor
        /// #mainSiteHeader
        /// </summary>
        //public CredFilters CredFilters { get; set; }
        public string MainSiteHeader { get; set; } 
        public string MainSiteLogo { get; set; }

        /// <summary>
        /// For: ???
        /// </summary>
        public string BackgroundColor1 { get; set; }
        public string ForegroundColor1 { get; set; }
        /// <summary>
        /// For: ???
        /// </summary>
        public string BackgroundColor2 { get; set; }
        public string ForegroundColor2 { get; set; }
        /// <summary>
        /// For: ???
        /// </summary>
        public string BackgroundColor3 { get; set; }
        public string ForegroundColor3 { get; set; }

        //interface to have a seperate section to handle all the buttons

        public string SearchButton1 { get; set; } 
        public string FilterButton1 { get; set; }
        public string ResetButton1 { get; set; }

        public bool HasChanged()
        {
            if ( (MainSiteHeader != null && MainSiteHeader != MainSiteHeaderBackground_Default) ||
                ( SearchButton1 != null && SearchButton1 != SearchButton1_Default) ||
                ( FilterButton1 != null && FilterButton1 != FilterButton1_Default ) ||
                ( ResetButton1 != null && ResetButton1 != ResetButton1_Default ) ||
                ( MainSiteLogo != null && MainSiteLogo != MainSiteLogo_Default ) 
                )
                return true;
            else
                return false;
        }
    }

    [Serializable]
    public class SearchFilters
    {
        //general
        public bool HideDescriptions { get; set; }
        public bool HideGrayButtons { get; set; }
    }
    [Serializable]
    public class CredentialFilters
    {
        public bool HideCredentialTypes { get; set; }
        public bool HideAudienceLevelTypes { get; set; }
        public bool HideApplicableAudienceTypes { get; set; }
        public bool HideCredentialConnections { get; set; }
        public bool HideCompetencies { get; set; }
        public bool HideSubjectArea { get; set; }
        public bool HideOccupations { get; set; }
        public bool HideIndustries { get; set; }
        public bool HideQualityAssuarance { get; set; }
        public bool HideOtherFilters { get; set; } = true;
        public string Keywords { get; set; }

    }
    [Serializable]
    public class OrganizationFilters
    {

        public string Keywords { get; set; }

    }
    [Serializable]
    public class AssessmentFilters
    {

        public string Keywords { get; set; }

    }
    [Serializable]
    public class LearningOpportunityFilters
    {

        public string Keywords { get; set; }

    }
}

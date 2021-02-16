using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Elastic
{
	public class CompetencyFrameworkIndex : BaseIndex
	{
		public CompetencyFrameworkIndex()
		{

		}

		public int OwnerOrganizationId
		{
			get { return PrimaryOrganizationId; }
			set { this.PrimaryOrganizationId = value; }
		}
		public string OwnerOrganizationName
		{
			get { return PrimaryOrganizationName; }
			set { this.PrimaryOrganizationName = value; }
		}
		public string OwnerOrganizationCTID
		{
			get { return PrimaryOrganizationCTID; }
			set { this.PrimaryOrganizationCTID = value; }
		}

		public List<string> Author { get; set; } = new List<string>();
		public List<string> Creator { get; set; } = new List<string>();
		public string SourceUrl { get; set; }
		public List<string> EducationLevelType { get; set; } = new List<string>();

		public List<string> Identifier { get; set; } = new List<string>();

		//public List<string> Keyword { get; set; } = new List<string>();

		//optional
		public string CompetencyFrameworkGraph { get; set; }

		public List<string> TopChildCompetencies { get; set; } = new List<string>();
		//add all competencies
		public int TotalCompetencies { get; set; }
		public List<IndexCompetency> Competencies { get; set; } = new List<IndexCompetency>();


		// set count for references by other entities
		//Or maybe just a list of assessment ids
		public int ReferencedByAssessments { get; set; }
		public int ReferencedByCredentials { get; set; }
		public int ReferencedByLearningOpportunities { get; set; }

		public List<BaseIndex> RelatedCredentials { get; set; } = new List<BaseIndex>();
		public List<BaseIndex> RelatedAssessment { get; set; } = new List<BaseIndex>();
		public List<BaseIndex> RelatedLearningOpportunities { get; set; } = new List<BaseIndex>();

		//public List<int> ReportFilters { get; set; } = new List<int>();


		//extras for IIndex??
	}
}

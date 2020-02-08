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

		public List<string> EducationLevelType { get; set; } = new List<string>();

		public List<string> Identifier { get; set; } = new List<string>();

		public List<string> Keyword { get; set; } = new List<string>();
		//add all competencies
		public List<string> Competencies { get; set; } = new List<string>();


		// set true if is referenced by any assessments
		//Or maybe just a list of assessment ids
		public bool ReferencedByAssessments { get; set; }
		public bool ReferencedByCredentials { get; set; }
		public bool ReferencedByLearningOpportunities { get; set; }
	}
}

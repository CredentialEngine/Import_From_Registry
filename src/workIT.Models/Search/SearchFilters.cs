using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Search
{
	/// <summary>
	/// Change to be generic. Setting unused to null will hide
	/// </summary>
	public class FilterQuery
	{
		public FilterQuery( string searchType )
		{
			SearchType = searchType;
		}
		public string SearchType { get; set; }
		//property searches
		public Filter AssessmentDeliveryTypes { get; set; }
		public Filter AssessmentMethodTypes { get; set; }
		public Filter AssessmentUseTypes { get; set; }
		public Filter AudienceLevels { get; set; } 
		public Filter AudienceTypes { get; set; } 
		public Filter CredentialStatusTypes { get; set; } 
		public Filter CredentialTypes { get; set; }
		//public Filter CredentialConnections { get; set; }
		public Filter LearningDeliveryTypes { get; set; }
		public Filter LearningMethodTypes { get; set; }

		public Filter OrganizationSectorTypes { get; set; } 
		public Filter OrganizationServiceTypes { get; set; } 
		public Filter OrganizationTypes { get; set; }
		public Filter OtherFilters { get; set; }
		public Filter ScoringMethodTypes { get; set; }
		public Filter VerificationClaimTypes { get; set; }

		//has any
		public Filter Competencies { get; set; } 
		public Filter Industries { get; set; } 
		public Filter Occupations { get; set; } 
		public Filter InstructionalPrograms { get; set; } //= new Filter();
		//connections. filterName=credentialconnections
		public Filter Connections { get; set; }
		//connections. filterName=qualityassurance, categoryId=13
		public Filter QualityAssurance { get; set; }
		public Filter QualityAssurancePerformed { get; set; }
		//languages. CategoryId=65
		public Filter Languages { get; set; } //= new Filter( "languages", 65 );
		//subjects
		public Filter SubjectAreas { get; set; } //= new Filter( "subjects", 0 );
		//
	}

	public class Filter
	{
		public Filter()
		{

		}
		public Filter( string filterName, int categoryId )
		{
			FilterName = filterName;
			CategoryId = categoryId;
		}
		public string FilterName { get; set; }
		public int CategoryId { get; set; }
		public string Label { get; set; }
		public string Description { get; set; }
		//use relative URL?
		//or equivalent of the Custom or Code searches from current

		public string HasAnyLabel { get; set; }
		public string HasAnyGuidance { get; set; }
		public FilterItem HasAny{ get; set; } 
		public List<FilterItem> Items { get; set; } = new List<FilterItem>();

		public string MicroSearchGuidance { get; set; }
		public string SearchType { get; set; }
		public string SearchTypeContext { get; set; }
		public string MicroSearchEndpoint { get; set; }
	}
	public class CheckboxListTextFilter
	{
		public string FilterName { get; set; }
		public int CategoryId { get; set; }
		public string Label { get; set; }
		public string Guidance { get; set; }
		//use relative URL?
		public string MicroSearchEndpoint { get; set; }
		public List<FilterItem> Options { get; set; } = new List<FilterItem>();
	}

	public class FilterItem
	{
		public int Id { get; set; }
		public string Label { get; set; }
		public string Value { get; set; }
		public string Schema { get; set; }
		//tooltip
		public string Description { get; set; }
	}
}

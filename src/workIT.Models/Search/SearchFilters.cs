using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace workIT.Models.Search
{

	public class APIFilter
	{
		public static string InterfaceType_Autocomplete = "interfaceType:Autocomplete";
		public static string InterfaceType_Checkbox = "interfaceType:CheckBox";
		public static string InterfaceType_Text = "interfaceType:Text";

		public static string FilterItem_HasAnyValue = "filterItem:HasAnyValue";
		public static string FilterItem_TextValue = "filterItem:TextValue";
		//"interfaceType:"

	}
	/// <summary>
	/// Change to be generic. Setting unused to null will hide
	/// </summary>

	public class FilterQuery
	{
		public string SearchType { get; set; }
	}
	public class FilterResponse
	{
		public FilterResponse( string searchType )
		{
			SearchType = searchType;
		}
		[JsonProperty( "@type" )]
		public string Type { get; set; } = "FilterResponse";
		public string SearchType { get; set; }

		public List<Filter> Filters { get; set; } = new List<Filter>();
	}
	/*
	public class FilterQueryOld
	{
		public FilterQueryOld( string searchType )
		{
			SearchType = searchType;
		}

		public string SearchType { get; set; }

		public List<Filter> Filters { get; set; }
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
*/

	public class Filter
	{
		public Filter()
		{

		}
		public Filter( string filterName )
		{
			URI = filterName.Replace(" ","");
			if ( URI.IndexOf( "filter:" ) == -1 )
				URI = "filter:" + URI;
		}
		public Filter( string filterName, string filterURI )
		{
			Label = filterName;
			URI = filterURI.Replace( " ", "" );
			if ( URI.IndexOf( "filter:" ) == -1 )
				URI = "filter:" + URI;
		}
		//public Filter( string filterName, int categoryId )
		//{
		//	URI = filterName;
		//	if ( URI.IndexOf( "filter:" ) == -1 )
		//		URI = "filter:" + URI;
		//	CategoryId = categoryId;
		//}
		[JsonProperty( "@type" )]
		public string Type { get; set; } = "Filter";

		public string URI { get; set; }
		public int Id { get; set; }
		public string Label { get; set; }
		public string Description { get; set; }
		public List<FilterItem> Items { get; set; } = new List<FilterItem>();

		//use relative URL?
		//or equivalent of the Custom or Code searches from current

		//public string HasAnyLabel { get; set; }
		//public string HasAnyGuidance { get; set; }
		//public FilterItem HasAny{ get; set; } 
		//public string MicroSearchGuidance { get; set; }
		public string SearchType { get; set; }
		public string SearchTypeContext { get; set; }
		//public string MicroSearchEndpoint { get; set; }
		public JObject Parameters { get; set; }
	}


	public class FilterItem
	{
		[JsonProperty( "@type" )]
		public string Type { get; set; } = "FilterItem";
		public int? Id { get; set; }
		public string Label { get; set; }
		public string Description { get; set; }
		public string URI { get; set; }
		public string InterfaceType { get; set; } 
		public string Text { get; set; }

		/// <summary>
		/// Only used to pass the Location Set filter from the widget\n
		/// Do not use this for anything else, as it is a terrible hack
		/// </summary>
		public Dictionary<string, object> Values { get; set; }
	}
}

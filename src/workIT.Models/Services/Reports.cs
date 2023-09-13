using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Services.Reports
{
	public class Report<T> where T : BasicRow
	{
		public Report()
		{
			Generated = DateTime.Now.ToString();
			Rows = new List<T>();
		}

		public string Generated { get; set; }
		public List<T> Rows { get; set; }
		public int TotalRows { get; set; }
	}
	//

	public class DuplicatesSummaryReport : Report<DuplicatesSummaryRow> { }
	public class DuplicatesResourceReport : Report<DuplicatesResourceRow> { }
	public class CurrencySummaryReport : Report<CurrencySummaryRow> { }
	public class CurrencyResourceReport : Report<CurrencyResourceRow> { }
	public class LinksSummaryReport : Report<LinksSummaryRow> { }
	public class LinksResourceReport: Report<LinksResourceRow> { }
	public class CredentialTypeResourceReport : Report<CredentialTypeResourceRow> { }
	public class DataQualityReport : Report<CredentialTypeResourceRow> { }
	//

	public class BasicRow
	{
		public string Name { get; set; }
		public string CTID { get; set; }
		public string LastUpdated { get; set; }
	}
	//

	public class SummaryRow : BasicRow
	{
		public int TotalItems { get; set; }
	}
	//

	public class ResourceRow : BasicRow
	{
		public string TypeLabel { get; set; }
		public string Publisher { get; set; }
		public string PublisherCTID { get; set; }
		public string DataOwner { get; set; }
		public string DataOwnerCTID { get; set; }
        public bool IsInPublisher { get; set; }
    }
	//

	public class DuplicatesSummaryRow : SummaryRow { }
	public class DuplicatesResourceRow : ResourceRow { }
	//

	public class CurrencySummaryRow : SummaryRow { }
	public class CurrencyResourceRow : ResourceRow
	{
		public int StatusTypeId { get; set; }
	}
	//

	public class LinksSummaryRow : SummaryRow
	{
		public int TotalRegistryLinks { get; set; }
		public int TotalContentLinks { get; set; }
	}
	public class LinksResourceRow : ResourceRow
	{
		public string Path { get; set; }
		public string Status { get; set; }
		public string URL { get; set; }

		/// <summary>
		/// Indicates the type of link<br />
		/// Use "linkCategory:Registry" or "linkCategory:Content"
		/// </summary>
		public string LinkType { get; set; }
		//Date Link checker last ran
		public string LastChecked { get; set; }
		//Reference OrgId
		public int ReferenceOrgId { get; set; }
	}
	//

	public class CredentialTypeResourceRow : ResourceRow
	{
		public string Description { get; set; }
		public string SubjectWebPage { get; set; }
		public string Type { get; set; }//for the reference report publisher, to get the resource name
	}
	//

	public class Query
	{
		public string Keywords { get; set; }
		public string EntityType { get; set; }
		//public string PublishingOrganizationCTID { get; set; }
		//public string OrganizationCTID { get; set; }
		//public string OwnerCTID { get; set; }
		public int Skip { get; set; }
		public int Take { get; set; }
		public string OrderBy { get; set; }
		public int TotalRows { get; set; }
        public bool IsSummary { get; set; }
        public List<Filter> Filters { get; set; }

		//Helper methods
		//Get filters, values, etc., based on filter URI
		public Filter GetFilter( string filterURI )
		{
			return Filters?.FirstOrDefault( m => m.FilterURI == filterURI );
		}
		public List<string> GetFilterValues( string filterURI, List<string> returnValuesIfNull = null )
		{
			return GetFilter( filterURI )?.Values ?? returnValuesIfNull;
		}
		public string GetFilterValue( string filterURI, string returnValueIfNull = null )
		{
			return GetFilterValues( filterURI )?.FirstOrDefault() ?? returnValueIfNull;
		}
		public List<int> GetFilterIntValues( string filterURI, List<int> returnValuesIfNull = null )
		{
			return GetIntValuesFromStringValues( GetFilter( filterURI )?.Values ) ?? returnValuesIfNull;
		}
		public int GetFilterIntValue( string filterURI, int returnValueIfNull = 0 )
		{
			return GetFilterIntValues( filterURI )?.FirstOrDefault() ?? returnValueIfNull;
		}

		//Get filters, values, etc., based on filter ID
		public Filter GetFilter( int filterID )
		{
			return Filters?.FirstOrDefault( m => m.FilterId == filterID );
		}
		public List<string> GetFilterValues( int filterID, List<string> returnValuesIfNull = null )
		{
			return GetFilter( filterID )?.Values ?? returnValuesIfNull;
		}
		public string GetFilterValue( int filterID, string returnValueIfNull = null )
		{
			return GetFilterValues( filterID )?.FirstOrDefault() ?? returnValueIfNull;
		}
		public List<int> GetFilterIntValues( int filterID, List<int> returnValuesIfNull = null )
		{
			return GetIntValuesFromStringValues( GetFilter( filterID )?.Values ) ?? returnValuesIfNull;
		}
		public int GetFilterIntValue( int filterID, int returnValueIfNull = 0 )
		{
			return GetFilterIntValues( filterID )?.FirstOrDefault() ?? returnValueIfNull;
		}

		//Integer parsing helper
		public List<int> GetIntValuesFromStringValues( List<string> values, List<int> returnValuesIfNull = null )
		{
			var temp = 0;
			return values?.Where( m => int.TryParse( m, out temp ) )?.Select( m => int.Parse( m ) )?.ToList() ??
			returnValuesIfNull;
		}
	}
	//

	public class Filter
	{
		public string FilterURI { get; set; }
		public int FilterId { get; set; }
		public List<string> Values { get; set; }
	}
	//

	public class Concept
	{
		public Concept() { }
		public Concept( string value, string name )
		{
			Value = value;
			Name = name;
		}

		public string Value { get; set; }
		public string Name { get; set; }
	}
	//
	public class APIRequestValidationResponse
	{
		public APIRequestValidationResponse()
		{
			Messages = new List<string>();
		}

		/// True if action was successfull, otherwise false
		public bool Successful { get; set; }
		public string RegistryAuthorizationToken { get; set; }
		public string PublishingOrganization { get; set; }
		public string PublishingOrganizationRegistryIdentifier { get; set; }
		public string PublishingOrganizationCTID { get; set; }
		public bool OwningOrganizationExists { get; set; }
		public string OwningOrganization { get; set; }
		public string OwningOrganizationRegistryIdentifier { get; set; }
		public bool IsSuperPublisher { get; set; }
		public bool PublisherIsTrustedPartner { get; set; }
		public bool PublisherCanPublishTheOrganization { get; set; }
		/// <summary>
		/// List of error or warning messages
		/// </summary>
		public List<string> Messages { get; set; }

	}
	public class NamedValue<T>
	{
		public NamedValue() { }
		public NamedValue( string name, T value ) {
			Name = name;
			Value = value;
		}

		public string Name { get; set; }
		public T Value { get; set; }
	}
	//

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;

namespace workIT.Models.API.RegistrySearchAPI
{
	//Resource and DSP data for use with a single Resource, for a Detail Page
	public class DetailPageDescriptionSet
	{
		public DetailPageDescriptionSet()
		{
			RelatedItemsMap = new List<RelatedItemsPath>();
			DebugInfo = new JObject();
		}
		public JObject Resource { get; set; }
		public JObject Metadata { get; set; }
		public List<RelatedItemsPath> RelatedItemsMap { get; set; }
		public List<JObject> RelatedItems { get; set; }
		public JObject DebugInfo { get; set; }
	}
	//

	//Used by other areas in the code
	public class ComposedSearchResultSet
	{
		public ComposedSearchResultSet()
		{
			Results = new List<ComposedSearchResult>();
			RelatedItems = new List<JObject>();
			DebugInfo = new JObject();
		}
		public int TotalResults { get; set; }
		public List<ComposedSearchResult> Results { get; set; }
		public List<JObject> RelatedItems { get; set; }
		public JObject DebugInfo { get; set; }

		public List<JObject> GetRelatedItems( List<string> uris )
		{
			return ( RelatedItems ?? new List<JObject>() ).Where( m => m[ "@id" ] != null && uris.Contains( m[ "@id" ].ToString() ) ).ToList();
		}
		public ComposedPathSetWithResources GetComposedPathSetWithResources( List<RelatedItemsPath> paths )
		{
			return new ComposedPathSetWithResources()
			{
				Paths = paths.Select( m => m.Path ).ToList(),
				TotalURIs = paths.Sum( m => m.TotalURIs ),
				RelatedItems = ( RelatedItems ?? new List<JObject>() ).Where( m => m[ "@id" ] != null && paths.SelectMany( n => n.URIs ).Distinct().ToList().Contains( m[ "@id" ].ToString() ) ).ToList()
			};
		}
	}
	//

	public class ComposedSearchResult
	{
		public JObject Data { get; set; }
		public List<RelatedItemsPath> RelatedItemsMap { get; set; }
		public JObject Metadata { get; set; }

		public List<RelatedItemsPath> GetPathsForPathRegex( string pathRegex )
		{
			return ( RelatedItemsMap ?? new List<RelatedItemsPath>() ).Where( m => new Regex( pathRegex ).IsMatch( m.Path ) ).ToList();
		}

		public List<string> GetURIsForPathRegex( string pathRegex )
		{
			return GetPathsForPathRegex( pathRegex ).SelectMany( m => m.URIs ).ToList();
		}
	}
	//

	public class ComposedPathSetWithResources
	{
		public ComposedPathSetWithResources()
		{
			Paths = new List<string>();
			RelatedItems = new List<JObject>();
		}
		public List<string> Paths { get; set; }
		public int TotalURIs { get; set; }
		public List<JObject> RelatedItems { get; set; }
	}
	//

	//Represents a single key-value pair from a language map
	public class LanguageMapValue
	{
		public LanguageMapValue()
		{
			MultiValue = new List<string>();
		}

		public string LanguageCode { get; set; }
		public string SingleValue { get; set; }
		public List<string> MultiValue { get; set; }
	}
	//

	//Used to make requests to the API
	public class SearchQuery
	{
		public enum DescriptionSetTypes { Resource, Resource_RelatedURIs, Resource_RelatedURIs_RelatedData }
		public SearchQuery()
		{
			Query = new JObject();
			DescriptionSetType = DescriptionSetTypes.Resource;
		}
		public JObject Query { get; set; }
		public int Skip { get; set; }
		public int Take { get; set; }
		public string Sort { get; set; }
		[JsonConverter( typeof( StringEnumConverter ) )]
		public DescriptionSetTypes DescriptionSetType { get; set; }
		public int DescriptionSetRelatedURIsLimit { get; set; }
		public bool IncludeDebugInfo { get; set; }
		public bool IncludeResultsMetadata { get; set; }
		public string Community { get; set; }
		public bool SkipLogging { get; set; }
		public JObject ExtraLoggingInfo { get; set; }
		public bool UseBetaAPI { get; set; }
	}
	//

	//Format the API sends back responses in
	public class SearchResponse
	{
		public List<JObject> data { get; set; }
		public bool valid { get; set; }
		public string status { get; set; }
		public SearchResponseExtra extra { get; set; }
	}
	public class SearchResponseExtra
	{
		public int TotalResults { get; set; }
		public List<RelatedItemsWrapper> RelatedItemsMap { get; set; }
		public List<JObject> RelatedItems { get; set; }
		public List<JObject> ResultsMetadata { get; set; }
		public JObject DebugInfo { get; set; }
	}
	public class RelatedItemsWrapper
	{
		public RelatedItemsWrapper()
		{
			RelatedItems = new List<RelatedItemsPath>();
		}

		public string ResourceURI { get; set; }
		public List<RelatedItemsPath> RelatedItems { get; set; }
	}
	public class RelatedItemsPath
	{
		public RelatedItemsPath()
		{
			URIs = new List<string>();
		}

		public string Path { get; set; }
		public int TotalURIs { get; set; }
		public List<string> URIs { get; set; }
	}
	//

	//Classes for direct interaction with the Registry
	public class RawRegistryResponse
	{
		public RawRegistryResponse()
		{
			DebugInfo = new JObject();
		}

		public bool Successful { get; set; }
		public string RawContent { get; set; }
		public JToken RawData {get; set;}
		public JObject DebugInfo { get; set; }
	}
	//

}

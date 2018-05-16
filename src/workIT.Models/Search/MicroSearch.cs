using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace workIT.Models.Search
{

	public class MicroSearchInputV2 : MicroSearchInput { }

	public class MicroSearchInput
	{
		public MicroSearchInput()
		{
			Filters = new List<MicroSearchFilter>();
			PageNumber = 1;
			PageSize = 50;
			IncludeAllCodes = true;
		}
		public string SearchType { get; set; } //IndustrySearch, OccupationSearch, etc.
		public string ParentSearchType { get; set; } //credential, organization, etc.
		/// <summary>
		/// ex. CredentialId
		/// </summary>
		public int ParentId { get; set; }
		public int PageNumber { get; set; }
		public int PageSize { get; set; }
		public string PageContext { get; set; } //Either "MainSiteSearch" or "MainSiteEditor" for now
		public bool IncludeAllCodes { get; set; }
		public List<MicroSearchFilter> Filters { get; set; }

		public string GetFilterValueString( string name )
		{
			try
			{
				return Filters.FirstOrDefault( m => m.Name.ToLower() == name.ToLower() ).Value ?? "";
			}
			catch
			{
				return "";
			}
		}
		public int GetFilterValueInt( string name )
		{
			try
			{
				return int.Parse( GetFilterValueString( name ) );
			}
			catch
			{
				return 0;
			}
		}

	}
	//

	public class MicroSearchFilter 
	{
		public string Name { get; set; }
		public string Value { get; set; }

		public int GetInt()
		{
			try
			{
				return int.Parse( Value );
			}
			catch
			{
				return 0;
			}
		}

	}
	//

	public class MicroSearchResults
	{
		public MicroSearchResults()
		{
			Results = new List<MicroSearchResult>();
		}
		public int TotalResults { get; set; }
		public List<MicroSearchResult> Results { get; set; }
		public object RawData { get; set; } //Some API searches return a string
	}
	//

	public class MicroSearchResult
	{
		public MicroSearchResult()
		{
			Properties = new Dictionary<string, object>();
		}
		public int CodeId { get; set; }
		public string Title { get; set; }
		public string Description { get; set; }
		public Dictionary<string, object> Properties { get; set; }
	}
	//

	public class MicroSearchSelection
	{
		public int ParentId { get; set; }
		public string ParentType { get; set; }
		public string SearchType { get; set; }
		public Dictionary<string, string> Values { get; set; }

		public string GetValueString( string name )
		{
			try
			{
				return Values.FirstOrDefault( m => m.Key.ToLower() == name.ToLower() ).Value ?? "";
			}
			catch
			{
				return "";
			}
		}

		public int GetValueInt( string name )
		{
			try
			{
				return int.Parse( GetValueString( name ) );
			}
			catch
			{
				return 0;
			}
		}
		
	}
	//

	//Passed by the interface to a partial
	public class MicroSearchSettings
	{
		public MicroSearchSettings()
		{
			Attributes = new Dictionary<string, string>();
			Filters = new List<MicroSearchSettings_Filter>();
			PageSize = 10;
			PageNumber = 1;
		}
		public string ParentId { get; set; } //Database ID (may be int or guid, not sure yet)
		public string ParentType { get; set; }
		public string Property { get; set; }
		public string SearchId { get; set; } //Unique ID of the search on the page
		public string SearchType { get; set; }
		public string Previous { get; set; }
		public int PageSize { get; set; }
		public int PageNumber { get; set; }
		public Dictionary<string, string> Attributes { get; set; }
		public string ResultRenderMethod { get; set; }
		public string SavedRenderMethod { get; set; }
		public bool UsesRecursiveSave { get; set; }
		public string ExtractionMethod { get; set; }
		public string ResultTemplate { get; set; }
		public string SavedTemplate { get; set; }
		public string SearchHeader { get; set; }
		public string SavedItemsHeader { get; set; }
		public List<MicroSearchSettings_Filter> Filters { get; set; }
	}
	//
	public class MicroSearchSettings_Filter
	{
		public MicroSearchSettings_Filter()
		{
			Attributes = new Dictionary<string, string>();
			Items = new Dictionary<string, string>();
		}
		public string Type { get; set; }
		public string FilterName { get; set; }
		public string Placeholder { get; set; }
		public Dictionary<string, string> Attributes { get; set; }
		public Dictionary<string, string> Items { get; set; }

	}
	//

	public class MicroSearchSettings_FilterV2 : MicroSearchSettings_Filter
	{
		public MicroSearchSettings_FilterV2()
		{
			IncludeDefaultItem = true;
			DefaultItemTitle = "";
			EnumerationData = new List<Common.EnumeratedItem>();
			CodeData = new List<CodeItem>();
		}

		public void SynthesizeItems()
		{

			try
			{
				foreach ( var item in EnumerationData )
				{
					Items.Add( item.Id.ToString(), item.Name );
				}
			}
			catch { }

			try
			{
				foreach ( var item in CodeData )
				{
					Items.Add( item.Id.ToString(), item.Name );
				}
			}
			catch { }

		}

		public bool IncludeDefaultItem { get; set; }
		public string DefaultItemTitle { get; set; }
		public List<Models.Common.EnumeratedItem> EnumerationData { get; set; }
		public List<Models.CodeItem> CodeData { get; set; }
	}
	//

}

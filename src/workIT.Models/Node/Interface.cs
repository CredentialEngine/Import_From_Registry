using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.Node;
using workIT.Models.Common;

namespace workIT.Models.Node.Interface
{
	//DTO for pointer data
	public class ProfileContext
	{
		public ProfileLink Main { get; set; } //Top-level main profile (e.g., Credential, Organization, etc)
		public ProfileLink Parent { get; set; } //Parent of the profile being worked on (e.g., Condition Profile that "owns" the Jurisdiction Profile being worked on)
		public ProfileLink Profile { get; set; } //Profile being targeted/worked on directly
		public bool IsTopLevel { get; set; } //Indicates whether or not Profile is also Main
	}
	//

	//Data for configuring the editor
	//public class EditorSettings
	//{
	//	public EditorSettings()
	//	{
	//		MainProfile = new ProfileLink();
	//		Editor = EditorSettings.EditorType.CREDENTIAL;
	//		Data = new BaseProfile();
	//		ParentRequestType = "";
	//		LastProfileType = "";
	//		LastProfileRowId = "";
	//		UserOrganizations = new List<CodeItem>();
	//	}
	//	public enum EditorType { CREDENTIAL, ORGANIZATION, ASSESSMENT, LEARNINGOPPORTUNITY, QACREDENTIAL, QA_ORGANIZATION }

	//	public ProfileLink MainProfile { get; set; }
	//	public EditorType Editor { get; set; }
	//	public BaseProfile Data { get; set; }
	//	/// <summary>
	//	/// Preloaded profile data - used for micro searches
	//	/// </summary>
	//	public List<BaseProfile> Profiles { get; set; }
	//	/// <summary>
	//	/// List of organizations for current user
	//	/// </summary>
	//	//public List<ProfileLink> UserOrganizations { get; set; }
	//	public List<CodeItem> UserOrganizations { get; set; }

	//	public string ParentRequestType { get; set; }
	//	public string LastProfileType { get; set; }
	//	public string LastProfileRowId { get; set; }
	//}
	//

	//Base settings for various types of editor elements
	public class BaseSettings
	{
		public BaseSettings()
		{
			ExtraClasses = new List<string>();
			UseSmallLabel = true;
			ShowTooltip = true;
			PropertySchema = "";
		}

		public string Property { get; set; }
		/// <summary>
		/// PropertySchema - used as 'fix' where the property name is not the valid ctdl property name. Set to {none} to prevent add of tooltip
		/// </summary>
		public string PropertySchema { get; set; }
		public string Label { get; set; }
		public string Guidance { get; set; }
		public List<string> ExtraClasses { get; set; } //Extra CSS classes
		public bool UseSmallLabel { get; set; }
		public bool RequireValue { get; set; }
		public bool ShowTooltip { get; set; }
	}
	//

	//Text input settings
	public class TextInputSettings : BaseSettings
	{
		public TextInputSettings()
		{
			Type = InputType.TEXT;
		}

		public enum InputType { TEXT, DATE, URL, NUMBER, TEXTAREA, HIDDEN, LABEL, NULLABLE_BOOLEAN }

		public InputType Type { get; set; }
		public string Placeholder { get; set; }
		public int MinimumLength { get; set; }

		public string NullableBooleanNullText { get; set; }
		public string NullableBooleanTrueText { get; set; }
		public string NullableBooleanFalseText { get; set; }
	}
	//

	//List input settings
	public class ListInputSettings : BaseSettings
	{
		public ListInputSettings()
		{
			Type = InterfaceType.CHECKBOX_LIST;
			CodeItems = new List<CodeItem>();
			EnumItems = new List<EnumeratedItem>();
			StringItems = new Dictionary<string, string>();
			Attributes = new Dictionary<string, string>();
			IncludeDefaultItem = true;
			EnableUncheck = true;
			PreSelectedItem = -1;
			LabelForNoneOption = "";
		}

		public enum InterfaceType { DROPDOWN_LIST, CHECKBOX_LIST, BOOLEAN_CHECKBOX_LIST, RADIO_LIST, BOOLEAN_RADIO_LIST }

		public InterfaceType Type { get; set; }
		public bool HasOtherBox { get; set; }
		public List<CodeItem> CodeItems { get; set; }
		public List<EnumeratedItem> EnumItems { get; set; }
		public Dictionary<string, string> StringItems { get; set; }
		public Dictionary<string, string> Attributes { get; set; }
		/// <summary>
		/// Inserts a default option of "Select..." with a value of 0 to drop-down lists
		/// </summary>
		public bool IncludeDefaultItem { get; set; } 
		public bool AddNoneOption { get; set; }
		public string LabelForNoneOption { get; set; }

		public bool EnableUncheck { get; set; } //Disables unselecting a checkbox if it is loaded as checked - only applies to BOOLEAN_CHECKBOX_LIST
		/// <summary>
		/// PreSelectItem - optional. Allow system to preselect an item (typically for a radio list
		/// </summary>
		public int PreSelectedItem { get; set; }
	}
	//

	//Profile settings
	public class ProfileSettings : BaseSettings
	{
		public ProfileSettings()
		{
			Type = ModelType.LIST;
			TabItems = new Dictionary<string, string>();
			IncludeName = true;
			ParentRepeaterId = "{repeaterID}";
			CopyText = "";
		}

		public enum ModelType { LIST, WRAPPER_START, WRAPPER_END, WRAPPER_CLOSE }

		public ModelType Type { get; set; }
		public bool IncludeName { get; set; }
		public string Profile { get; set; }
		public string ProfileType { get; set; }
		public string AddText { get; set; }
		public string CopyText { get; set; }
		//public string ParentEditorName { get; set; }
		public string ParentRepeaterId { get; set; }
		public string Filter { get; set; }
		public bool HasTabs { get; set; }

		public Dictionary<string, string> TabItems { get; set; }
	}
	//

	//Micro Search settings
	public class MicroSearchSettings : BaseSettings
	{
		public MicroSearchSettings()
		{
			AllowingSearch = true;
			AllowingStarterCreate = false;
			AllowingEntityReferenceCreate = false;
			AllowingPopupCreate = true;
			PageSize = 10;
			PageNumber = 1;
			Previous = "";
			Filters = new List<MicroSearchFilter>();
			HiddenFilters = new List<MicroSearchFilter>();
			HiddenValue = "";
			StaticSelectorValues = new Dictionary<string, object>();
			HasKeywords = true;
			AllowMultipleSavedItems = true;
			ProfileTemplate = "MicroProfile";
			ParentRepeaterId = "{repeaterID}";
			DoAjaxSave = true;
			SavedItemsHeader = "Saved Items";
			CreateProfileTitle = "Item";
			ProfileType = "";
			AutoPropertyRefresh = new List<string>();

		}

		/// <summary>
		/// The common case is to allow a search. 
		/// There may be conditions, where we want to just add entities, and show the saved list
		/// </summary>
		public bool AllowingSearch { get; set; }
		public bool AllowingEntityReferenceCreate { get; set; }
		public bool AllowingStarterCreate { get; set; }
		public bool AllowingPopupCreate { get; set; }
		public bool AllowingAddProfileOption { get; set; }
		public bool HasKeywords { get; set; }
		/// <summary>
		/// If false, only one entity can be saved. New selections will replace any currently saved/selected item
		/// </summary>
		public bool AllowMultipleSavedItems { get; set; }
		/// <summary>
		/// If the parent profile is new and hasn't been saved yet, save it before trying to save the selected MicroProfile
		/// </summary>
		public bool AutoSaveNewParentProfile { get; set; } 
		/// <summary>
		/// Determines whether or not the search does an immediate save on selection of a result
		/// </summary>
		public bool DoAjaxSave { get; set; }
		/// <summary>
		/// When true, system will allow creating a new entity even if parent/container has not been saved as yet.
		/// Note: should only be used where:
		/// - DoAjaxSave is false
		/// - AllowMultipleSavedItems is false
		/// </summary>
		public bool AllowCreateWithoutParentExisting { get; set; } 
		public string ParentRepeaterId { get; set; }
		public string ProfileTemplate { get; set; }
		public string Previous { get; set; }
		public string SearchType { get; set; }
		public int PageSize { get; set; }
		public int PageNumber { get; set; }
		public List<MicroSearchFilter> Filters { get; set; }
		public List<MicroSearchFilter> HiddenFilters { get; set; }
		public string HiddenValue { get; set; }
		public Dictionary<string, object> StaticSelectorValues { get; set; }
		public string SavedItemsHeader { get; set; }
		/// <summary>
		/// Determines whether or not to show an "Edit Profile" link on results
		/// </summary>
		public bool HasEditProfile { get; set; } 
		/// <summary>
		/// Determines whether or not to show a "Create New" button
		/// </summary>
		public bool HasCreateProfile { get; set; } 
		/// <summary>
		/// Indicates which profile to load when "Create New" is clicked 
		/// </summary>
		public string ProfileType { get; set; } 
		/// <summary>
		/// Text to show after "Create New" on the Create New button
		/// </summary>
		public string CreateProfileTitle { get; set; } 
		/// <summary>
		/// The search result's title will be a link
		/// </summary>
		public bool HasResultLink { get; set; } 
		/// <summary>
		/// Automatically refresh all microsearches anywhere in the editor for the listed properties when a result is saved or removed from this microsearch
		/// </summary>
		public List<string> AutoPropertyRefresh { get; set; }
	}
	//

	//Micro Search Filter
	public class MicroSearchFilter
	{
		public MicroSearchFilter()
		{
			Attributes = new Dictionary<string, string>();
			Items = new Dictionary<string, string>();
			HiddenValue = "";
		}
		public string Type { get; set; }
		public string FilterName { get; set; }
		//value to use for a hidden filter of type text
		public string HiddenValue { get; set; }
		public string Placeholder { get; set; }
		public Dictionary<string, string> Attributes { get; set; }
		public Dictionary<string, string> Items { get; set; }
	}
	//

	//Text Value Settings
	/*public class TextValueSettings : BaseSettings
	{
		public TextValueSettings()
		{
			CodeItems = new List<CodeItem>();
			EnumItems = new List<EnumeratedItem>();
			ValueType = TextInputSettings.InputType.TEXT;
			IncludeSelector = true;
			IncludeOtherBox = true;
		}

		public List<CodeItem> CodeItems { get; set; }
		public List<EnumeratedItem> EnumItems { get; set; }
		public bool IncludeSelector { get; set; }
		public bool IncludeOtherBox { get; set; }
		public string ValueLabel { get; set; }
		public TextInputSettings.InputType ValueType { get; set; }
		public string ValueGuidance { get; set; }
	}
	//
	*/

	//Text Value Editor Settings
	public class TextValueEditorSettings : BaseSettings
	{
		public TextValueEditorSettings()
		{
			AddText = "Add New";
			ParentRepeaterId = "{repeaterID}";
			CodeItems = new List<CodeItem>();
			EnumItems = new List<EnumeratedItem>();
			ValueType = TextInputSettings.InputType.TEXT;
			ValuePlaceholder = "Value...";
			OtherPlaceholder = "Other...";
		}

		public string AddText { get; set; }
		public string ParentRepeaterId { get; set; }
		public List<CodeItem> CodeItems { get; set; }
		public List<EnumeratedItem> EnumItems { get; set; }
		public bool HasSelector { get; set; }
		public bool HasOther { get; set; }
		public TextInputSettings.InputType ValueType { get; set; }
		public string ValuePlaceholder { get; set; }
		public string OtherPlaceholder { get; set; }
		public bool RequireOther { get; set; }
		public int CategoryId { get; set; }
	}
	//

}

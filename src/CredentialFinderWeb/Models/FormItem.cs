using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CredentialFinderWeb.Models
{
	public class FormItem
	{
		public enum InterfaceTypes { Text, TextArea, Select, Boolean, CheckBoxList, TextList, Hidden, DisplayOnly, SummerNote, FileReference, FileReferenceList }
		public FormItem() { }
		public FormItem( string label, string property, InterfaceTypes interfaceType = InterfaceTypes.Text, List<ValueItem> items = null, bool isRequired = false, string helpText = "", string extraCSSClasses = "", int tagId = 0 )
		{
			Label = label;
			Property = property;
			InterfaceType = interfaceType;
			IsRequired = isRequired;
			Items = items;
			HelpText = helpText;
			ExtraCssClasses = extraCSSClasses;
			TagId = tagId;
		}
		public string Label { get; set; }
		public string Property { get; set; }
		public InterfaceTypes InterfaceType { get; set; }
		public bool IsRequired { get; set; }
		public List<ValueItem> Items { get; set; }
		public string HelpText { get; set; }
		public string ExtraCssClasses { get; set; }
		public int TagId { get; set; }

		public string InterfaceTypeString { get { return InterfaceType.ToString().ToLower(); } }
	}

	public class ValueItem
	{
		public ValueItem() { }
		public ValueItem( string label, string value )
		{
			Label = label;
			Value = value;
			HelpText = "";
		}
		public ValueItem( string label, string value, string helpText )
		{
			Label = label;
			Value = value;
			HelpText = helpText;
		}
		public ValueItem( string label, string value, int tagId )
		{
			Label = label;
			Value = value;
			TagId = tagId;
		}
		public string Label { get; set; }
		public string Value { get; set; }
		public string HelpText { get; set; }
		public int TagId { get; set; }

	}

}
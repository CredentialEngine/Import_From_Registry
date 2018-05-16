using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Common
{
	public enum EnumerationType
	{
		MULTI_SELECT,
		SINGLE_SELECT,
		FRAMEWORK_SELECT,
		SINGLE_SELECT_ID_ONLY,
		MULTI_SELECT_ID_ONLY,
		CUSTOM
	}

	public class Enumeration
	{
		public Enumeration()
		{
			Items = new List<EnumeratedItem>();
			OtherValue = "";
		}
		public string Name { get; set; }
		public string SchemaName { get; set; }
		public string Description { get; set; }
		public int ParentId { get; set; }
		public string Url { get; set; }
		public string FrameworkVersion { get; set; }
		public List<EnumeratedItem> Items { get; set; }
		public EnumerationType InterfaceType { get; set; }
		public bool ShowOtherValue { get; set; }

		/// <summary>
		/// CategoryId from Codes.PropertyCategory
		/// </summary>
		public int Id { get; set; }

		/// <summary>
		/// If one of the selected items is other, the related other value is stored here!
		/// </summary>
		public string OtherValue { get; set; }

		/// <summary>
		/// Return true if any Items exist
		/// note that for an update, a check may have to be done, in case any previous items were deleted
		/// </summary>
		/// <returns></returns>
		public bool HasItems() 
		{
			return !( Items == null || Items.Count == 0 );
		}

		public EnumeratedItem GetFirstItem() 
		{
			EnumeratedItem firstItem = new EnumeratedItem();
			if ( HasItems() )
			{
				foreach ( EnumeratedItem item in Items )
				{
					firstItem = item;
					break;
				}
			}
			return firstItem;
		}
		public int GetFirstItemId()
		{
			int id = 0;
			EnumeratedItem firstItem = new EnumeratedItem();
			if ( HasItems() )
			{
				foreach ( EnumeratedItem item in Items )
				{
					id = item.Id;
					break;
				}
			}
			return id;
		}
		//public List<CredentialAlignmentObjectProfile> ItemsAsAlignmentObjects
		//{
		//	get
		//	{
		//		return ConvertItemsToAlignmentObjects( this );
		//	}
		//	set
		//	{
		//		ConvertAlignmentObjectsToEnumeration( this, value );
		//	}
		//}

		//public static List<CredentialAlignmentObjectProfile> ConvertItemsToAlignmentObjects( Enumeration data )
		//{
		//	var result = new List<CredentialAlignmentObjectProfile>();
		//	foreach ( var item in data.Items )
		//	{
		//		result.Add( new CredentialAlignmentObjectProfile()
		//		{
		//			FrameworkName = data.Name,
		//			FrameworkUrl = data.Url,
		//			EducationalFramework = data.Name,
		//			Name = item.Name,
		//			TargetName = item.Name,
		//			Description = item.Description,
		//			TargetDescription = item.Description,
		//			TargetUrl = string.IsNullOrWhiteSpace( item.URL ) ? ( string.IsNullOrWhiteSpace( item.SchemaName ) ? "" : item.SchemaName.Contains( ":" ) ? item.SchemaName : "missingPrefix:" + item.SchemaName ) : item.URL,
		//			CodedNotation = item.Value
		//		} );
		//	}//,CodedNotation = item.Value
		//	return result;
		//}
		//

		//public static void ConvertAlignmentObjectsToEnumeration( Enumeration target, List<CredentialAlignmentObjectProfile> data )
		//{
		//	//TODO: flesh this out
		//	var result = new List<EnumeratedItem>();
		//	foreach( var item in data )
		//	{
		//		result.Add( new EnumeratedItem()
		//		{
		//			Name = item.TargetNodeName,
		//			Description = item.TargetNodeDescription,
		//			SchemaName = item.TargetNode.IndexOf("http") == 0 ? "" : item.TargetNode,
		//			SchemaUrl = item.TargetNode.IndexOf("http") == 0 ? item.TargetNode : "",
		//			Value = item.CodedNotation
		//		} );
		//	}
		//	target.Items = result;
		//}
		//
	}
	//

	public class EnumeratedItem
	{
		public EnumeratedItem() {
			IsSpecialValue = false;
		}
		/// <summary>
		/// Database unique ID. 
		/// </summary>
		public int Id { get; set; } 
		public string RowId { get; set; }
		public int ParentId { get; set; } //parent ID. 
		public int CodeId { get; set; } //code table ID. 

		/// <summary>
		/// PK of parent table storing code  - clarify if different than ParentId
		/// </summary>
		public int RecordId { get; set; } 
		/// <summary>
		/// Displayed name
		/// </summary>
		public string Name { get; set; }
 		/// <summary>
		/// Url - optional
 		/// </summary>
		public string URL { get; set; }  
		/// <summary>
		/// Description (if applicable)
		/// </summary>
		public string Description { get; set; } 
		/// <summary>
		/// Schema-based name. Should not contain spaces. May not be necessary.
		/// </summary>
		public string SchemaName { get; set; }
		/// <summary>
		/// Schema-based name of the parent code, where present.
		/// </summary>
		public string ParentSchemaName { get; set; } 

		/// <summary>
		/// Value referenced in "value" property of HTML objects
		/// </summary>
		public string Value { get; set; }
		public bool IsSpecialValue { get; set; }

		/// <summary>
		/// URL to schema descriptor - can probably delete this
		/// </summary>
		public string SchemaUrl { get; set; } 
		public int SortOrder { get; set; } //Sort Order
		/// <summary>
		/// Indicates whether or not the item is selected
		/// </summary>
		public bool Selected { get; set; }
		public int Totals { get; set; }
		/// <summary>
		/// Datetime this item was created
		/// </summary>
		public DateTime Created { get; set; }
 		/// <summary>
		/// ID of the user that created this item
 		/// </summary>
		public int CreatedById { get; set; }
		public string ItemSummary { get; set; }

		public string ReverseTitle { get; set; }
		public string ReverseDescription { get; set; }
		public string ReverseSchemaName { get; set; }
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Download.Models
{
	[Serializable]
	public class CodeItem
	{
		public CodeItem()
		{
			SortOrder = 10;
			IsActive = true;
			CodeGroup = "";
		}

		//the code PK is either Id or Code - the caller will know the context
		public int Id { get; set; }
		/// <summary>
		/// Code is a convenience property to handle where a code item has a character key, or where need to use a non-integer display - rare
		/// </summary>
		public string Code { get; set; }
		public string CodeGroup { get; set; }
		public string Name
		{
			get
			{
				return Title;
			}
			set
			{
				this.Title = value.Trim();
			}
		}
		public string Title { get; set; }

		public string Description { get; set; }
		public string URL { get; set; }
		public string SchemaName { get; set; }
		public string ParentSchemaName { get; set; }
		public bool IsActive { get; set; }
		public bool AnyValue { get; set; }
		public int SortOrder { get; set; }
		public int Totals { get; set; }
		public int CategoryId { get; set; }
		public string Category { get; set; }
		public string CategorySchema { get; set; }
		public string EntityType { get; set; }
		public int EntityTypeId { get; set; }
		public string ReverseTitle { get; set; }
		public string ReverseDescription { get; set; }
		public string ReverseSchemaName { get; set; }
		public int RelationshipId { get; internal set; }
		public int AssertionId { get; internal set; }
		public string CodeTitle { get; set; }
		public List<int> IdsList { get; set; } = new List<int>();
		public string ItemList { get; set; }

	}
}

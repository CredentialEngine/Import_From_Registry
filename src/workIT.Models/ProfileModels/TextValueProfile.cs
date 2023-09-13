using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.ProfileModels
{
    [Serializable]
    public class TextValueProfile : BaseProfile
	{
		//public string ProfileSummary { get; set; }

		/// <summary>
		/// Id of the category (typically in Codes.PropertyCategory)
		/// </summary>
		public int CategoryId { get; set; }

		/// <summary>
		/// The parentId is typically the EntityId, not the container profile. The latter id is the BaseId
		/// </summary>
		public int EntityId
		{
			get { return this.RelatedEntityId; }
			set { this.RelatedEntityId = value; }
		}

		/// <summary>
		/// Actually container primary key (as opposed to EntityId)
		/// </summary>
		public int EntityBaseId { get; set; }
        /// <summary>
        /// Actual parent type
        /// </summary>
        public int EntityTypeId { get; set; }
        
        /// <summary>
        /// Title of the category (e.g. "Reference Url")
        /// </summary>
        public string CategoryTitle { get; set; }
 
		/// <summary>
		/// Unique Id of the code (typically in Codes.PropertyValue)
		/// </summary>
		public int PropertyValueId { get; set; }
		public int CodeId {
			get { return PropertyValueId; }
			set { PropertyValueId = value; }
		}

		/// <summary>
		/// Value from the code table (e.g. "Dun and Bradstreet DUNS Number")
		/// </summary>
		public string CodeTitle { get; set; } 

		/// <summary>
		/// Schema name for a property, where present
		/// </summary>
		public string CodeSchema{ get; set; } 

		/// <summary>
		/// User-defined arbitrary title for the value (e.g., "Assessment handbook PDF Link")
		/// </summary>
		public string TextTitle { get; set; } 
		/// <summary>
		/// Value from the end user (e.g. an actual DUNS number)
		/// </summary>
		public string TextValue { get; set; }
        public string TextValue_Map { get; set; }
    }

}

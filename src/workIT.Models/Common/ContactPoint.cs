using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.ProfileModels;

namespace workIT.Models.Common
{
    /// <summary>
    /// Contact Point
    /// </summary>
    [Serializable]
    public class ContactPoint : BaseObject
	{
		public ContactPoint()
		{
			PhoneNumbers = new List<string>();
			Emails = new List<string>();
			SocialMediaPages = new List<string>();

			PhoneNumber = new List<TextValueProfile>();
			Email = new List<TextValueProfile>();
			SocialMedia = new List<TextValueProfile>();

			//ContactOption = new List<string>();
		}

		public Guid ParentRowId { get; set; }

		public string ProfileName { get; set; }
        public string Name_Map { get; set; }
        public string Name {  get { return ProfileName; } set { ProfileName = value; } } //Alias used for publishing
		/// <summary>
		/// Specification of the type of contact.
		/// </summary>
		public string ContactType { get; set; }
        public string ContactType_Map { get; set; }
        /// <summary>
        /// An option available on this contact point.
        /// For example, a toll-free number or support for hearing-impaired callers.
        /// </summary>
        //public string ContactOption { get; set; }
        //public List<string> ContactOption { get; set; }

        #region Used by Import
        public List<string> PhoneNumbers { get; set; }
		public List<string> FaxNumber { get; set; } = new List<string>();

		//
		public List<string> Emails { get; set; }
		/// <summary>
		/// A social media resource for the resource being described.
		/// </summary>
		public List<string> SocialMediaPages { get; set; }

		#endregion

		#region Used by display
		//this was the editor approach, here we are using contact points only
		//although, the detail page expects these?
		public List<TextValueProfile> PhoneNumber { get; set; }
		public List<TextValueProfile> Email { get; set; }
		public List<TextValueProfile> SocialMedia { get; set; }

		#endregion 

	}
}

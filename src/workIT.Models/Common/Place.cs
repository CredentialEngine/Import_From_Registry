using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Common
{
	/// <summary>
	/// why so we have a Place and an Address class
	/// </summary>
    [Serializable]
    public class Place
	{
		public Place()
		{
			ContactPoint = new List<ContactPoint>();
		}

		public string Name { get; set; }

		public string Description { get; set; }

		public string Address1 { get; set; }
		public string Address2 { get; set; }

		public string PostOfficeBoxNumber { get; set; }

		public string City { get; set; }

		public string AddressRegion { get; set; }
		public string PostalCode { get; set; }

		public string Country { get; set; }

		public double Latitude { get; set; }

		public double Longitude { get; set; }

		public string GeoURI { get; set; }
		public string IdentifierJson { get; set; }
		public List<ContactPoint> ContactPoint { get; set; }
	}

	//public class ContactPoint
	//{
	//	public ContactPoint()
	//	{
	//		PhoneNumbers = new List<string>();
	//		Emails = new List<string>();
	//		SocialMediaPages = new List<string>();
	//		ContactOption = new List<string>();
	//	}

	//	public string Name { get; set; }

	//	/// <summary>
	//	/// Specification of the type of contact
	//	/// </summary>
	//	public string ContactType { get; set; }

	//	/// <summary>
	//	/// An option available on this contact point.
	//	/// For example, a toll-free number or support for hearing-impaired callers.
	//	/// </summary>
	//	public List<string> ContactOption { get; set; }

	//	public List<string> PhoneNumbers { get; set; }
	//	public List<string> Emails { get; set; }
	//	public List<string> SocialMediaPages { get; set; }


	//}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Elastic
{
	public class Address
	{
		//public int Id { get; set; }
		public string Name { get; set; }
		public string Address1 { get; set; }
		public string PostOfficeBoxNumber { get; set; }
		public string City { get; set; }

		public string SubRegion { get; set; }

		public string AddressRegion { get; set; }
		public string PostalCode { get; set; }
		public string Country { get; set; }
		public double Latitude { get; set; }
		public double Longitude { get; set; }
		//contactPoints
		//public List<ContactPoint> ContactPoint { get; set; }

	}

	//public class ContactPoint
	//{
	//	public ContactPoint()
	//	{
	//		PhoneNumber = new List<string>();
	//		Email = new List<string>();
	//		SocialMediaPage = new List<string>();
	//	}

	//	public string Name { get; set; }
	//	/// <summary>
	//	/// Specification of the type of contact.
	//	/// </summary>
	//	public string ContactType { get; set; }

	//	public List<string> PhoneNumber { get; set; }
	//	public List<string> Email { get; set; }
	//	/// <summary>
	//	/// A social media resource for the resource being described.
	//	/// </summary>
	//	public List<string> SocialMediaPage { get; set; }
	//}
}
          
        

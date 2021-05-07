using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Elastic
{
	[Serializable]

	public class Address
	{
		//public int Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public string StreetAddress { get; set; }
		public string PostOfficeBoxNumber { get; set; }
		public string AddressLocality { get; set; }

		public string SubRegion { get; set; }

		public string AddressRegion { get; set; }
		public string PostalCode { get; set; }
		public string AddressCountry { get; set; }
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
          
        
